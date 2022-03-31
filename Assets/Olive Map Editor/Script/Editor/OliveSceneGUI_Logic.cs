namespace OliveMapEditor {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;
	using System.Linq;


	// === Logic ===
	public partial class OliveSceneGUI {

		// Dirty
		private static void SetAllRoomLayerItemDirty () {
			NeedFixAllRoomLayerItem = true;
		}

		private static void SetAutoTileDirty (bool forceFix = false, SelectionMode selectionMode = SelectionMode.CurrentLayer, bool allRoom = false) {
			SetAutoTileDirty(-1, forceFix ? -3 : -2, selectionMode, allRoom);
		}

		private static void SetAutoTileDirty (long paletteID, int nfatIndex, SelectionMode selectionMode, bool allRoom) {
			NeedFixAutoTile = nfatIndex;
			NeedFixAutoTileID = paletteID;
			FixAutoTileSelectionMode = selectionMode;
			FixAutoTileForAllRooms = allRoom;
		}

		private static void SetOverlapDirty () {
			NeedDeleteOverlap = true;
		}

		private static void SetRepaintDirty () {
			NeedRepaintSceneView = true;
		}

		// System
		private static void RefreshAllSceneMaps (OliveMap map) {
			EditingIndexInParentMaps = -1;
			AllMapsInScene.Clear();
			var allMaps = Object.FindObjectsOfType<OliveMap>();
			for (int i = 0; i < allMaps.Length; i++) {
				var m = allMaps[i];
				if (m) {
					AllMapsInScene.Add(m);
				}
			}
			AllMapsInScene.Sort(new MapSorter());
			SameParentMapNames = new string[AllMapsInScene.Count];
			for (int i = 0; i < AllMapsInScene.Count; i++) {
				SameParentMapNames[i] = AllMapsInScene[i].name;
				if (map && AllMapsInScene[i] == map) {
					EditingIndexInParentMaps = i;
				}
			}
		}

		private static float ScrollGUI (float scrollValue, Rect viewRect, float contentHeight, float panelHeight) {

			// Scroll Bar
			var newScrollValue = GUI.VerticalScrollbar(
				new Rect(viewRect) { width = SCROLL_WIDTH, x = PANEL_WIDTH - SCROLL_WIDTH },
				scrollValue,
				Mathf.Clamp01(panelHeight / contentHeight),
				0, 1
			);

			// Logic
			switch (Event.current.type) {
				case EventType.ScrollWheel:
					if (viewRect.Contains(Event.current.mousePosition)) {
						newScrollValue += Event.current.delta.y * (panelHeight / contentHeight) * 0.06f;
						Event.current.Use();
					}
					break;
			}

			return newScrollValue;
		}

		private static void ClearSelection () {
			if (Selection.activeGameObject) {
				Selection.activeGameObject = null;
			}
			_SelectionWorldRect = null;
		}

		private static void LoadSettings () {
			PaletteSizeIndex.Load();
			ShowHierarchyIcon.Load();
			ShowHightlightCursor.Load();
			ColorPickingModeIndex.Load();
			FocusOnOpeningRoom.Load();
			OliveUndoable.Load();
			SelectionModeIndex.Load();
			ToolIndex.Load();
			GizmosUIIndex.Load();
			ShowGizmosLabel.Load();
			ShowPaintingItem.Load();
			PaintingTint.Load();
			AllRoomForReplaceTile.Load();
			AllLayerForReplaceTile.Load();
			ShowCollider.Load();
			ShowExTools.Load();
		}

		private static void DeleteSettings () {
			EditorPrefs.DeleteKey(PaletteSizeIndex.Key);
			EditorPrefs.DeleteKey(ShowHierarchyIcon.Key);
			EditorPrefs.DeleteKey(ShowHightlightCursor.Key);
			EditorPrefs.DeleteKey(ColorPickingModeIndex.Key);
			EditorPrefs.DeleteKey(FocusOnOpeningRoom.Key);
			EditorPrefs.DeleteKey(OliveUndoable.Key);
			EditorPrefs.DeleteKey(SelectionModeIndex.Key);
			EditorPrefs.DeleteKey(ToolIndex.Key);
			EditorPrefs.DeleteKey(GizmosUIIndex.Key);
			EditorPrefs.DeleteKey(ShowGizmosLabel.Key);
			EditorPrefs.DeleteKey(ShowPaintingItem.Key);
			EditorPrefs.DeleteKey(PaintingTint.Key);
			EditorPrefs.DeleteKey(AllRoomForReplaceTile.Key);
			EditorPrefs.DeleteKey(AllLayerForReplaceTile.Key);
			EditorPrefs.DeleteKey(ShowCollider.Key);
		}

		private static void SelectAllLogic () {
			SelectionCacheList.Clear();
			ForTriggeringLayer(LayerSelectionMode, (LayerTF) => {
				int count = LayerTF.childCount;
				for (int i = 0; i < count; i++) {
					SelectionCacheList.Add(LayerTF.GetChild(i).gameObject);
				}
				return count > 0;
			});
			Selection.objects = SelectionCacheList.ToArray();
		}


		// Preview
		private static void RemovePreviewFor (OliveMap.PaletteItem item) {
			for (int i = 0; i < item.ItemCount; i++) {
				RemovePreviewFor(item, i);
			}
		}


		private static void RemovePreviewFor (OliveMap.PaletteItem item, int detailIndex) {
			var indexedItem = new IndexedPaletteItem(item, detailIndex);
			if (PalettePreviewMap.ContainsKey(indexedItem)) {
				PalettePreviewMap.Remove(indexedItem);
			}
		}


		// Palette
		private static void PaletteRandomNumberGUI (Rect rect, OliveMap.PaletteItem item, int _fontSize, float scale = 0.5f, bool showLabel = false) {
			if (item && item.IsMuti) {
				var r = new Rect(
					rect.x + rect.width * (1f - scale) + 1,
					rect.y + rect.height * (1f - scale) + 1,
					rect.width * scale,
					rect.height * scale
				);
				ColorBlock(r, PAINTING_ITEM_NUMBER_BG_COLOR);
				GUI.Label(r, item.ItemCount.ToString(), new GUIStyle(GUI.skin.label) {
					fontSize = _fontSize,
					alignment = TextAnchor.MiddleCenter,
				});
				if (showLabel) {
					r.width = 52;
					r.x -= r.width;
					ColorBlock(r, PAINTING_ITEM_NUMBER_BG_COLOR);
					GUI.Label(r, item.Type == OliveMap.PaletteItemTyle.AutoTile ? "AutoTile" : "random", new GUIStyle(GUI.skin.label) {
						fontSize = _fontSize - 6,
						alignment = TextAnchor.MiddleRight,
					});
				}
			}
		}


		private static void PaletteDragAndDropGUI (Rect rect, OliveMap.PaletteItem item = null, DragAndDropVisualMode visual = DragAndDropVisualMode.Copy) {
			switch (Event.current.type) {
				case EventType.DragUpdated:
					if (DragAndDrop.objectReferences.Length > 0 && rect.Contains(Event.current.mousePosition)) {
						DragAndDrop.visualMode = visual;
						Event.current.Use();
					}
					break;
				case EventType.DragPerform:
					if (DragAndDrop.objectReferences.Length > 0 && rect.Contains(Event.current.mousePosition)) {
						var list = new List<Object>();
						foreach (Object obj in DragAndDrop.objectReferences) {
							if (obj is GameObject) {
								var prefab = PrefabUtility.GetCorrespondingObjectFromSource(obj as GameObject);
								if (prefab && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(prefab))) {
									list.Add(prefab);
								} else if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj))) {
									list.Add(obj);
								}
							} else if (obj is Sprite) {
								list.Add(obj);
							} else if (obj is Texture2D) {
								var path = AssetDatabase.GetAssetPath(obj);
								if (!string.IsNullOrEmpty(path)) {
									var sps = AssetDatabase.LoadAllAssetsAtPath(path);
									for (int i = 0; i < sps.Length; i++) {
										if (sps[i] is Sprite) {
											list.Add(sps[i]);
										}
									}
								}
							}
						}
						TryAddPaletteItem(list, false, item);
						DragAndDrop.AcceptDrag();
						Event.current.Use();
					}
					break;
			}
		}

		private static void TryDeletePaletteItem (int index) {
			if (!EditingMap || index < 0 || index >= EditingMap.Palette.Count) { 
				return;
			}
			if (Dialog("Confirm", "Delete this palette item?", "Delete", "Cancel")) {
				RegistUndo("Delete Palette Item");
				EditingMap.RemovePaletteItemAt(index);
				index = Mathf.Clamp(index, 0, Mathf.Max(0, EditingMap.Palette.Count - 1));
				SetSelectingPaletteItem(index);
				SetAllRoomLayerItemDirty();
			}
		}

		private static void TryAddPaletteItem () {
			TryAddPaletteItem(null);
		}

		private static void TryAddPaletteItem (List<Object> objs, bool addEmpty = true, OliveMap.PaletteItem pal = null) {
			if (!EditingMap || pal == EMPTY_PALETTE_ITEM) { return; }
			bool paletteAdd = false;
			bool detailAdd = false;
			if (objs != null && objs.Count > 0) {
				var pList = new List<GameObject>();
				var sList = new List<Sprite>();
				foreach (Object obj in objs) {
					if (!obj) { continue; }
					if (obj is GameObject) {
						pList.Add(obj as GameObject);
					} else if (obj is Sprite) {
						sList.Add(obj as Sprite);
					}
				}
				if (pList.Count + sList.Count > 0) {
					RegistUndo("Add Palette Item");
				}
				if (pList.Count > 0) {
					if (!pal) {
						pal = new OliveMap.PaletteItem(OliveMap.PaletteItemTyle.Random, OliveMap.PaletteAssetType.Prefab);
						EditingMap.AddPaletteItem(pal);
					}
					foreach (var p in pList) {
						pal.Items.Add(new OliveMap.PaletteItem.ItemData() { Prefab = p });
					}
					if (pal.AssetType != OliveMap.PaletteAssetType.Prefab) {
						pal.AssetType = OliveMap.PaletteAssetType.Prefab;
						RemovePreviewFor(pal);
					}
					detailAdd = true;
					DetailScrollValue = 0.5f;
				}
				if (sList.Count > 0) {
					if (!pal) {
						pal = new OliveMap.PaletteItem(OliveMap.PaletteItemTyle.Random, OliveMap.PaletteAssetType.Sprite);
						EditingMap.AddPaletteItem(pal);
					}
					foreach (var s in sList) {
						pal.Items.Add(new OliveMap.PaletteItem.ItemData() { Sprite = s });
					}
					if (pal.AssetType != OliveMap.PaletteAssetType.Sprite) {
						pal.AssetType = OliveMap.PaletteAssetType.Sprite;
						RemovePreviewFor(pal);
					}
					detailAdd = true;
					DetailScrollValue = 0.5f;
				}
			} else if (addEmpty) {
				if (pal) {
					RegistUndo("Add Palette Item");
					pal.Items.Add(new OliveMap.PaletteItem.ItemData());
					detailAdd = true;
				} else {
					RegistUndo("Add Palette Item");
					pal = new OliveMap.PaletteItem(OliveMap.PaletteItemTyle.Random, OliveMap.PaletteAssetType.Sprite);
					pal.Items.Add(new OliveMap.PaletteItem.ItemData());
					EditingMap.AddPaletteItem(pal);
					paletteAdd = true;
				}
			}
			// End
			if (paletteAdd) {
				PaletteScrollValue = 1f;
				if (EditingMap.Palette.Count > 0) {
					SetSelectingPaletteItem(EditingMap.Palette.Count - 1);
				}
				SortPaletteItems();
			} else if (detailAdd) {
				// End
				DetailScrollValue = 1f;
				SetSelectingPaletteItem(SelectingPaletteIndex, true);
			}
			// Num Fix 99
			if (pal && pal.TryFixItemMaxCount()) {
				Dialog("", "It can be up to 99 items only.", "OK");
			}
			if (pal && pal.Type == OliveMap.PaletteItemTyle.AutoTile) {
				SetAutoTileDirty(true, SelectionMode.AllLayers);
			}
		}


		private static void TryAddPaletteDetailItemColor (OliveMap.PaletteItem item, Color newColor) {
			if (!item || item.AssetType != OliveMap.PaletteAssetType.Color) { return; }
			RegistUndo("Add Palette Item");
			item.Items.Add(new OliveMap.PaletteItem.ItemData() { Color = newColor });
			DetailScrollValue = 1f;
			SetSelectingPaletteItem(SelectingPaletteIndex, true);
		}

		private static void TryDuplicatePaletteItem (OliveMap.PaletteItem item) {
			if (!EditingMap || !item) { return; }
			var newItem = new OliveMap.PaletteItem(item);
			int oldIndex = EditingMap.Palette.IndexOf(item);
			int newIndex = Mathf.Clamp(oldIndex + 1, 0, EditingMap.Palette.Count);
			EditingMap.Palette.Insert(newIndex, newItem);
			SetSelectingPaletteItem(newIndex);
			Selection.objects = null;
		}


		private static void SortPaletteItems () {
			if (!EditingMap) { return; }
			var mode = EditingMap.PaletteSortMode;
			if (mode != OliveMap.OlivePaletteSortMode.Unsorted) {
				EditingMap.Palette.Sort(new PaletteSorter() { Mode = mode });

			}
		}

		private static void TryMovePaletteItemAt (int index, bool left) {
			if (!EditingMap) { return; }
			int indexAlt = left ? index - 1 : index + 1;
			if (index >= 0 && index < EditingMap.Palette.Count && indexAlt >= 0 && indexAlt < EditingMap.Palette.Count) {
				RegistUndo("Move Palette");
				var temp = EditingMap.Palette[index];
				EditingMap.Palette[index] = EditingMap.Palette[indexAlt];
				EditingMap.Palette[indexAlt] = temp;
				SelectingPaletteIndex = indexAlt;
			}
		}

		private static void SetSelectingPaletteItem (int index, bool forceReset = false) {
			if (SelectingPaletteIndex != index || forceReset) {
				SelectingPaletteIndex = index;
				ResetSelectingPaletteItemRandomIndex();
				ResetEditingHighlight();
				ClearSelection();
			}
			// Replace Tile
			var rePal = SelectingPaletteItem;
			if (!rePal || rePal.IsEmpty || rePal.AssetType == OliveMap.PaletteAssetType.Color) {
				rePal = null;
			}
			if (PickingReplaceTileFrom) {
				ReplaceTileFrom = rePal;
				PickingReplaceTileFrom = false;
			}
			if (PickingReplaceTileTo) {
				ReplaceTileTo = rePal;
				PickingReplaceTileTo = false;
			}
		}


		private static void ResetSelectingPaletteItemRandomIndex () {
			PaletteRandomIndex = 
				SelectingPaletteItem && SelectingPaletteItem.IsRandom ?
				(int)Random.Range(0f, SelectingPaletteItem.ItemCount - 0.001f) : 0;
		}


		private static void ResetEditingHighlight () {
			if (EditingHighlight) {
				if (EditingHighlight.childCount > 0) {
					Object.DestroyImmediate(EditingHighlight.GetChild(0).gameObject, false);
				}
				var pal = SelectingPaletteItem;
				if (pal) {
					Transform highlight = null;
					switch (pal.AssetType) {
						case OliveMap.PaletteAssetType.Prefab: {
								var itemData = PaletteRandomIndex >= 0 && PaletteRandomIndex < pal.ItemCount ? pal.Items[PaletteRandomIndex] : null;
								if (!itemData || !itemData.Prefab) { break; }
								highlight = Object.Instantiate(itemData.Prefab, EditingHighlight).transform;
								highlight.localScale = Vector3.one * EditingMap.GridSize * pal.Scale;
								break;
							}
						case OliveMap.PaletteAssetType.Sprite: {
								int index = PaletteRandomIndex;
								if (pal.Type == OliveMap.PaletteItemTyle.AutoTile) {
									index = 0;
									for (int i = 0; i < pal.ItemCount; i++) {
										var iData = pal.Items[i];
										if (!iData) { continue; }
										if (CheckAutoTileRotation(PaintingRotation, iData.Rotation)) {
											index = i;
											break;
										}
									}
								}
								var itemData = index >= 0 && index < pal.ItemCount ? pal.Items[index] : null;
								if (!itemData) { break; }
								float gridSize = EditingMap ? EditingMap.GridSize : 1f;
								highlight = new GameObject(pal.ID.ToString(), typeof(SpriteRenderer)).transform;
								highlight.SetParent(EditingHighlight);
								var texture = GetPalettePreview(pal, PaletteRandomIndex);
								var sprite = itemData.Sprite;
								if (texture && sprite) {
									var sr = highlight.GetComponent<SpriteRenderer>();
									sr.sprite = sprite;
									sr.color = pal.Tint * PaintingTint;
									float w = Mathf.Max(1, texture.width);
									float h = Mathf.Max(1, texture.height);
									highlight.localScale = new Vector3(
										sprite.pixelsPerUnit * pal.Scale * gridSize / w,
										sprite.pixelsPerUnit * pal.Scale * gridSize / h,
										1f
									);
								}
								break;
							}
						default:
						case OliveMap.PaletteAssetType.Color:
							break;
					}
					if (highlight) {
						highlight.localPosition = new Vector3(0, 0, -0.1f);
						highlight.localRotation = pal.UseRotation ? Quaternion.Euler(0, 0, (int)PaintingRotation * 90f) : Quaternion.identity;
					}
				}
			}
		}


		private static void ShowPaletteItemMenu (int index) {
			if (!EditingMap || EditingMap.Palette == null || index < 0 || index >= EditingMap.Palette.Count) { return; }
			var pal = EditingMap.Palette[index];
			if (!pal) { return; }
			var menu = new GenericMenu() { allowDuplicateNames = false };
			menu.AddItem(new GUIContent("Delete"), false, () => {
				TryDeletePaletteItem(index);
			});
			menu.AddItem(new GUIContent("Duplicate"), false, () => {
				TryDuplicatePaletteItem(pal);
			});
			menu.ShowAsContext();
		}


		private static void ShowPaletteMenu () {
			if (!EditingMap || EditingMap.Palette == null) { return; }
			var menu = new GenericMenu() { allowDuplicateNames = false };
			menu.AddItem(new GUIContent("New Item"), false, TryAddPaletteItem);
			menu.ShowAsContext();
		}


		// Room
		private static void TryAddRoom () {
			if (!EditingMap) { return; }
			RegistUndo("Add Room");
			EditingMap.SpawnRoom("Room " + EditingMap.RoomCount);
			SetOpeningRoomIndex(EditingMap.RoomCount - 1);
		}


		private static void TryDeleteRoomAt (int index) {
			if (!EditingMap) { return; }
			if (index >= 0 && index < EditingMap.RoomCount) {
				if (EditingMap.RoomCount > 1) {
					if (Dialog("Confirm", "Delete room at " + index + "?", "Delete", "Cancel")) {
						RegistUndo("Delete Room");
						RemoveRoomAt(index);
						SetOpeningRoomIndex(Mathf.Clamp(OpeningRoomIndex, 0, EditingMap.RoomCount - 1));
					}
				} else {
					Debug.Log("[Olive] You can't delete last room.");
				}
			}
		}


		private static void MoveRoom (OliveMap map, int index, bool up) {
			if (!map) { return; }
			map.MoveRoom(index, up);
			SetAllRoomLayerItemDirty();
		}


		private static void ClearRoomMinMax () {
			_RoomMinMax = null;
		}


		private static void SetOpeningRoomIndex (int newIndex) {
			OpeningRoomIndex = newIndex;
			SetAllRoomLayerItemDirty();
			ClearRoomMinMax();
			ClearSelection();
			SetOverlapDirty();
			if (FocusOnOpeningRoom) {
				FocusCurrentRoom();
			}
		}


		private static void RemoveRoomAt (int roomIndex) {
			if (!EditingMap) { return; }
			Transform roomTF = EditingMap.GetRoomTF(roomIndex);
			if (roomTF) {
				DestroyObject(roomTF.gameObject);
			}
		}


		// Layer
		private static void TryAddLayer () {
			if (!EditingMap) { return; }
			RegistUndo("Add Layer");
			EditingMap.SpawnLayer("Layer " + EditingMap.GetLayerCount(OpeningRoomIndex), OpeningRoomIndex);
			SetSelectingLayerIndex(EditingMap.GetLayerCount(OpeningRoomIndex) - 1);
			SetAllRoomLayerItemDirty();
		}


		private static void TryDeleteLayerAt (int index) {
			if (!EditingMap) { return; }
			int layerCount = EditingMap.GetLayerCount(OpeningRoomIndex);
			if (index >= 0 && index < layerCount) {
				if (layerCount > 1) {
					if (Dialog("Confirm", "Delete layer at " + index + "?", "Delete", "Cancel")) {
						RegistUndo("Delete Layer");
						RemoveLayerAt(OpeningRoomIndex, index);
						SetSelectingLayerIndex(Mathf.Clamp(SelectingLayerIndex, 0, EditingMap.GetLayerCount(OpeningRoomIndex)));
						SetAllRoomLayerItemDirty();
					}
				} else {
					Debug.Log("[Olive] You can't delete last layer.");
				}
			}
		}


		private static void MoveLayer (OliveMap map, int index, bool up) {
			if (!map) { return; }
			var layerTF = map.GetLayerTF(OpeningRoomIndex, index);
			if (!layerTF) { return; }
			int altIndex = index + (up ? -1 : 1);
			if (altIndex < 0 || altIndex >= map.GetLayerCount(OpeningRoomIndex)) { return; }
			layerTF.SetSiblingIndex(altIndex);
			SetAllRoomLayerItemDirty();
		}


		private static void SetSelectingLayerIndex (int newIndex) {
			if (newIndex != SelectingLayerIndex) {
				SelectingLayerIndex = newIndex;
				ClearSelection();
				SetOverlapDirty();
			}
		}


		private static void ForTriggeringLayer (SelectionMode mode, System.Func<Transform, bool> func, bool allRoom = false) {
			ForTriggeringLayer(mode, (layerTF, index) => func.Invoke(layerTF), allRoom);
		}


		private static void ForTriggeringLayer (SelectionMode mode, System.Func<Transform, int, bool> func, bool allRoom = false) {
			ForTriggeringLayer(mode, (layerTF, roomIndex, layerIndex) => func.Invoke(layerTF, layerIndex), allRoom);
		}


		private static void ForTriggeringLayer (SelectionMode mode, System.Func<Transform, int, int, bool> func, bool allRoom = false) {
			bool oneOnly, hasItemTriggered = false;
			int startRoomIndex = allRoom ? 0 : OpeningRoomIndex;
			int endRoomIndex = allRoom ? EditingMap.RoomCount : OpeningRoomIndex + 1;
			for (int roomIndex = startRoomIndex; roomIndex < endRoomIndex; roomIndex++) {
				int startIndex = 0;
				int endIndex = EditingMap.GetLayerCount(OpeningRoomIndex);
				switch (mode) {
					default:
					case SelectionMode.CurrentLayer:
						startIndex = SelectingLayerIndex;
						endIndex = SelectingLayerIndex + 1;
						oneOnly = true;
						break;
					case SelectionMode.TopLayer:
						oneOnly = true;
						break;
					case SelectionMode.AllLayers:
						oneOnly = false;
						break;
				}
				for (int layerIndex = startIndex; layerIndex < endIndex; layerIndex++) {
					var layerTF = EditingMap.GetLayerTF(roomIndex, layerIndex);
					if (!layerTF) { continue; }
					try {
						hasItemTriggered = func.Invoke(layerTF, roomIndex, layerIndex) || hasItemTriggered;
					} catch {
						EditorUtility.ClearProgressBar();
					}
					if (hasItemTriggered && oneOnly) { break; }
				}
				if (hasItemTriggered && oneOnly) { break; }
			}
		}


		private static void RemoveLayerAt (int roomIndex, int layerIndex) {
			if (!EditingMap) { return; }
			var layerTF = EditingMap.GetLayerTF(roomIndex, layerIndex);
			if (layerTF) {
				DestroyObject(layerTF.gameObject);
			}
		}


		// Misc
		private static void RegistUndo (string title) {
			if (EditingMap && OliveUndoable) {
				Undo.RegisterCompleteObjectUndo(EditingMap, "[Olive] " + title);
			}
		}


		private static void RegistObjUndo (Object obj, string title) {
			if (obj && OliveUndoable) {
				Undo.RegisterCompleteObjectUndo(obj, "[Olive] " + title);
			}
		}


		private static void RegistCreateUndo (Object obj, string title) {
			if (OliveUndoable) {
				Undo.RegisterCreatedObjectUndo(obj, "[Olive] " + title);
			}
		}


		private static void DestroyObject (GameObject obj) {
			if (OliveUndoable) {
				Undo.DestroyObjectImmediate(obj);
			} else {
				Object.DestroyImmediate(obj, false);
			}
		}


		private static Texture2D GetOliveIconTexture (OliveMap data) {
			if (!_OliveIconTexture) {
				try {
					_OliveIconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CombinePaths(GetOliveRootPath(data), "Image", "Olive Icon.psd"));
				} catch { }
			}
			if (!_OliveIconTexture) {
				_OliveIconTexture = Texture2D.blackTexture;
			}
			return _OliveIconTexture;
		}


		private static void HideMapRoot (GameObject g) {
			var map = g.GetComponent<OliveMap>();
			if (map) {
				map.HideRoot();
			}
		}


		private static bool ForSelectingItems (System.Action<Transform> action) {
			bool hasValue = false;
			if (!EditingMap || !Selection.activeGameObject) { return hasValue; }
			var roomTF = EditingMap.GetRoomTF(OpeningRoomIndex);
			foreach (var g in Selection.gameObjects) {
				if (!g.transform.parent || !g.transform.parent.parent || g.transform.parent.parent != roomTF) { continue; }
				action(g.transform);
				hasValue = true;
			}
			return hasValue;
		}


		private static void DeleteAllOverlap (int roomIndex, int layerIndex) {
			if (!EditingMap) { return; }
			var layerTF = EditingMap.GetLayerTF(roomIndex, layerIndex);
			if (!layerTF) { return; }
			OverlapCacheMap.Clear();
			for (int i = layerTF.childCount - 1; i >= 0; i--) {
				var itemTF = layerTF.GetChild(i);
				var localPos = itemTF.localPosition;
				var pos = new Int2(
					Mathf.RoundToInt(localPos.x / EditingMap.GridSize),
					Mathf.RoundToInt(localPos.y / EditingMap.GridSize)
				);
				if (!OverlapCacheMap.ContainsKey(pos)) {
					OverlapCacheMap.Add(pos, itemTF);
				} else {
					DestroyObject(itemTF.gameObject);
				}
			}
		}


		private static void SetPaintingRotation (OliveRotationType rot) {
			if (PaintingRotation != rot) {
				PaintingRotation = rot;
				SetSelectingPaletteItem(SelectingPaletteIndex, true);
			}
		}


		private static bool CopySelectingItems (bool cut) {
			CopyCacheList.Clear();
			var hasItem = ForSelectingItems((itemTF) => {
				var pal = EditingMap.GetPaletteItem(OliveMap.GetItemID(itemTF));
				int index = OliveMap.GetDetailIndex(itemTF);
				if (pal) {
					var pos = itemTF.localPosition;
					var data = new LongInt4(
						pal.ID, index,
						Mathf.RoundToInt(pos.x / EditingMap.GridSize),
						Mathf.RoundToInt(pos.y / EditingMap.GridSize),
						OliveMap.GetItemRotation(itemTF)
					);
					if (!cut) {
						data.x++;
						data.y++;
					}
					CopyCacheList.Add(data);
				}
				if (cut) {
					DestroyObject(itemTF.gameObject);
				}
			});
			if (hasItem) {
				if (cut) {
					SetAutoTileDirty(false, SelectionMode.AllLayers);
				}
			}
			return hasItem;
		}


		private static bool PasteFromCopyedData () {
			if (CopyCacheList.Count > 0) {
				SelectionCacheList.Clear();
				var layerTF = EditingMap.GetLayerTF(OpeningRoomIndex, SelectingLayerIndex);
				if (!layerTF) { return false; }
				for (int i = 0; i < CopyCacheList.Count; i++) {
					var data = CopyCacheList[i];
					var pal = EditingMap.GetPaletteItem(data.a);
					if (!pal) { continue; }
					var itemTF = EditingMap.SpawnItem(pal, layerTF, data.x, data.y, data.z, PaintingTint, data.index, ShowCollider);
					data.x++;
					data.y++;
					CopyCacheList[i] = data;
					if (itemTF) {
						RegistCreateUndo(itemTF.gameObject, "Spawn Item");
						SelectionCacheList.Add(itemTF.gameObject);
					}
				}
				return true;
			}
			return false;
		}


		private static void TryReplaceTile () {
			if (!EditingMap || !ReplaceTileFrom || ReplaceTileFrom.IsEmpty || ReplaceTileFrom.AssetType == OliveMap.PaletteAssetType.Color) { return; }
			long fromID = ReplaceTileFrom.ID;
			long toID = ReplaceTileTo && !ReplaceTileTo.IsEmpty && ReplaceTileTo.AssetType != OliveMap.PaletteAssetType.Color ? ReplaceTileTo.ID : -1;
			var itemList = new List<Transform>();
			var layerMode = AllRoomForReplaceTile || AllLayerForReplaceTile ? SelectionMode.AllLayers : SelectionMode.CurrentLayer;
			ForTriggeringLayer(layerMode, (layerTF) => {
				bool triggered = false;
				int itemCount = layerTF.childCount;
				for (int i = 0; i < itemCount; i++) {
					var itemTF = layerTF.GetChild(i);
					long id = OliveMap.GetItemID(itemTF);
					if (id == fromID) {
						itemList.Add(itemTF);
					}
				}
				return triggered;
			}, AllRoomForReplaceTile);
			float gridSize = EditingMap.GridSize;
			for (int i = 0; i < itemList.Count; i++) {
				var itemTF = itemList[i];
				try {
					if (!itemTF || !itemTF.parent) { continue; }
					if (toID >= 0) {
						// Spawn To
						var pos = itemTF.localPosition;
						int rot = OliveMap.GetItemRotation(itemTF);
						int dIndex = OliveMap.GetDetailIndex(itemTF);
						var newItem = EditingMap.SpawnItem(
							ReplaceTileTo, itemTF.parent,
							Mathf.RoundToInt(pos.x / gridSize),
							Mathf.RoundToInt(pos.y / gridSize),
							rot, Color.white, dIndex, ShowCollider
						);
						if (newItem) {
							RegistCreateUndo(newItem.gameObject, "Spawn Item");
						}
					}
					// Delete From
					DestroyObject(itemTF.gameObject);
					// Progress
					EditorUtility.DisplayProgressBar("Replace Tile", string.Format("Replacing...{0}/{1}", i, itemList.Count), (float)i / itemList.Count);
				} catch { }
			}
			EditorUtility.ClearProgressBar();
			ShowReplaceTileGUI = false;
			PickingReplaceTileFrom = false;
			PickingReplaceTileTo = false;
			SetAutoTileDirty(false, layerMode, AllRoomForReplaceTile);
		}


		private static void AddDefaultRoomLayerIfEmpty () {
			if (!EditingMap) { return; }
			// Room Fix
			if (EditingMap.RoomCount == 0) {
				EditingMap.SpawnRoom("Room");
			}
			var newRoomIndex = Mathf.Clamp(OpeningRoomIndex, 0, EditingMap.RoomCount - 1);
			if (OpeningRoomIndex != newRoomIndex) {
				SetOpeningRoomIndex(newRoomIndex);
			}

			// Layer Fix
			if (EditingMap.GetLayerCount(OpeningRoomIndex) == 0) {
				EditingMap.SpawnLayer("Layer", OpeningRoomIndex);
				SetAllRoomLayerItemDirty();
			}
			int layerCount = EditingMap.GetLayerCount(OpeningRoomIndex);
			SetSelectingLayerIndex(Mathf.Clamp(SelectingLayerIndex, 0, layerCount - 1));
		}


		// Auto Tile
		private static void FixAllAutoTileItems () {
			if (!EditingMap) { return; }
			PosItemMapForAutoTile.Clear();
			ForTriggeringLayer(FixAutoTileSelectionMode, (layerTF, roomIndex, layerIndex) => {
				int itemCount = layerTF.childCount;
				float gridSize = EditingMap.GridSize;
				Int4 indexPos;
				Transform itemTF;
				for (int i = 0; i < itemCount; i++) {
					itemTF = layerTF.GetChild(i);
					long id = OliveMap.GetItemID(itemTF);
					int index = OliveMap.GetDetailIndex(itemTF);
					if (id < 0) { continue; }
					var pal = EditingMap.GetPaletteItem(id);
					if (!pal || pal.Type != OliveMap.PaletteItemTyle.AutoTile || pal.ItemCount <= 1) { continue; }
					var localPos = itemTF.localPosition;
					indexPos = new Int4(roomIndex, layerIndex, Mathf.RoundToInt(localPos.x / gridSize), Mathf.RoundToInt(localPos.y / gridSize));
					if (!PosItemMapForAutoTile.ContainsKey(indexPos)) {
						PosItemMapForAutoTile.Add(indexPos, new TransformLongInt(itemTF, id, index));
					}
				}
				return true;
			}, FixAutoTileForAllRooms);

			// Fix
			bool forceFix = NeedFixAutoTile == -3;
			bool fixSinglePalette = NeedFixAutoTileID >= 0 && NeedFixAutoTile >= 0;
			foreach (var pair in PosItemMapForAutoTile) {
				var indexPos = pair.Key;
				var itemTF = pair.Value.transform;
				if (!itemTF) { continue; }
				long id = pair.Value.a;
				int oldIndex = pair.Value.b;
				int newIndex = 0;
				var pal = EditingMap.GetPaletteItem(id);
				if (!pal) { continue; }
				for (int i = 0; i < pal.ItemCount; i++) {
					var itemData = pal.Items[i];
					if (!itemData) { continue; }
					if (CheckAutoTileRotation(
						(OliveRotationType)OliveMap.GetItemRotation(itemTF),
						itemData.Rotation
					) && OliveMap.PaletteItem.CheckAutoTile(
						itemData.AutoTileData,
						PosItemMapForAutoTile.ContainsKey(new Int4(indexPos.x0, indexPos.y0, indexPos.x1, indexPos.y1 + 1)),
						PosItemMapForAutoTile.ContainsKey(new Int4(indexPos.x0, indexPos.y0, indexPos.x1, indexPos.y1 - 1)),
						PosItemMapForAutoTile.ContainsKey(new Int4(indexPos.x0, indexPos.y0, indexPos.x1 - 1, indexPos.y1)),
						PosItemMapForAutoTile.ContainsKey(new Int4(indexPos.x0, indexPos.y0, indexPos.x1 + 1, indexPos.y1)),
						PosItemMapForAutoTile.ContainsKey(new Int4(indexPos.x0, indexPos.y0, indexPos.x1 - 1, indexPos.y1 + 1)),
						PosItemMapForAutoTile.ContainsKey(new Int4(indexPos.x0, indexPos.y0, indexPos.x1 + 1, indexPos.y1 + 1)),
						PosItemMapForAutoTile.ContainsKey(new Int4(indexPos.x0, indexPos.y0, indexPos.x1 - 1, indexPos.y1 - 1)),
						PosItemMapForAutoTile.ContainsKey(new Int4(indexPos.x0, indexPos.y0, indexPos.x1 + 1, indexPos.y1 - 1))
					)) {
						if (i == oldIndex) {
							newIndex = oldIndex;
						} else {
							int finalIndex = pal.GetAutoTileFinalIndex(i);
							if (finalIndex == i) {
								newIndex = i;
							} else if (oldIndex >= i && oldIndex <= finalIndex) {
								newIndex = oldIndex;
							} else {
								newIndex = (int)Random.Range(i, finalIndex + 0.999f);
							}
						}
						break;
					}
				}

				// Set
				if (
					newIndex != oldIndex || forceFix ||
					(fixSinglePalette && NeedFixAutoTileID == id && oldIndex == NeedFixAutoTile)
				) {
					ChangeAutoTileDetailIndex(pal, itemTF, newIndex);
				}
			}
			PosItemMapForAutoTile.Clear();
		}


		private static void ChangeAutoTileDetailIndex (OliveMap.PaletteItem pal, Transform itemTF, int newIndex) {
			if (!EditingMap || !pal || !itemTF || pal.Type != OliveMap.PaletteItemTyle.AutoTile || pal.IsEmpty) { return; }
			switch (pal.AssetType) {
				case OliveMap.PaletteAssetType.Sprite:
					if (itemTF.childCount == 0) { break; }
					var spriteTF = itemTF.GetChild(0);
					var sr = spriteTF.GetComponent<SpriteRenderer>();
					if (!sr) { return; }
					newIndex = Mathf.Clamp(newIndex, 0, pal.ItemCount - 1);
					var sp = pal.Items[newIndex].Sprite;
					sr.sprite = sp;
					spriteTF.localScale = OliveMap.GetSpriteScale(sp);
					var rot = OliveMap.GetItemRotation(itemTF);
					var col = OliveMap.GetItemColliderType(itemTF);
					itemTF.name = OliveMap.GetItemName(pal.ID, newIndex, rot, pal.AssetType, pal.ColliderType);
					if (col == OliveMap.OliveColliderType.PhysicsShapeCollider) {
						EditingMap.RefreshColliderDisplayer(itemTF, col, sp, ShowCollider);
					}
					break;
				case OliveMap.PaletteAssetType.Prefab:
					int childCount = itemTF.childCount;
					Vector3 oldScale = Vector3.one;
					for (int i = 0; i < childCount; i++) {
						var oldTF = itemTF.GetChild(0);
						oldScale = oldTF.localScale;
						Object.DestroyImmediate(oldTF.gameObject, false);
					}
					newIndex = Mathf.Clamp(newIndex, 0, pal.ItemCount - 1);
					var p = pal.Items[newIndex].Prefab;
					if (p) {
						var _p = Object.Instantiate(p, itemTF);
						_p.transform.localPosition = Vector3.zero;
						_p.transform.localRotation = Quaternion.identity;
						_p.transform.localScale = oldScale;
					}
					break;
				default:
				case OliveMap.PaletteAssetType.Color:
					break;
			}
		}


		private static bool CheckAutoTileRotation (OliveRotationType itemRotation, OliveMap.PaletteItemRotationType autoRotation) {
			switch (autoRotation) {
				default:
				case OliveMap.PaletteItemRotationType.AllRotation:
					return true;
				case OliveMap.PaletteItemRotationType.Horizontal:
					return itemRotation == OliveRotationType.Left || itemRotation == OliveRotationType.Right;
				case OliveMap.PaletteItemRotationType.Vertical:
					return itemRotation == OliveRotationType.Up || itemRotation == OliveRotationType.Down;
				case OliveMap.PaletteItemRotationType.Up:
					return itemRotation == OliveRotationType.Up;
				case OliveMap.PaletteItemRotationType.Left:
					return itemRotation == OliveRotationType.Left;
				case OliveMap.PaletteItemRotationType.Down:
					return itemRotation == OliveRotationType.Down;
				case OliveMap.PaletteItemRotationType.Right:
					return itemRotation == OliveRotationType.Right;
			}
		}


		// Edit
		private static void RespawnEditingStuff () {
			EditingRoot = null;
			if (EditingMap) {
				if (EditingMap.MapRoot) {
					// Create Root 
				}
				int deleteIndex = 0;
				int childCount = EditingMap.transform.childCount;
				for (int i = 0; i < childCount; i++) {
					var tf = EditingMap.transform.GetChild(deleteIndex);
					if (tf != EditingMap.MapRoot) {
						Object.DestroyImmediate(tf.gameObject, false);
					} else {
						deleteIndex++;
					}
				}
				// Editing Root
				EditingRoot = new GameObject("Editing Root") {
					hideFlags = HideFlags.HideAndDontSave,
				}.transform;
				EditingRoot.SetParent(EditingMap.transform);
				EditingRoot.SetAsLastSibling();
				EditingRoot.localPosition = Vector3.zero;
				EditingRoot.localRotation = Quaternion.identity;
				EditingRoot.localScale = Vector3.one;
				// Highlight
				EditingHighlight = new GameObject("High Light").transform;
				EditingHighlight.SetParent(EditingRoot);
				EditingHighlight.SetAsLastSibling();
				EditingHighlight.localPosition = Vector3.zero;
				EditingHighlight.localRotation = Quaternion.identity;
				EditingHighlight.localScale = Vector3.one;
				// Reselect
				SetSelectingPaletteItem(SelectingPaletteIndex, true);
			} else {
				EditingRoot = null;
				EditingHighlight = null;
			}
		}


	}
}