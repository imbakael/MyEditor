namespace OliveMapEditor {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;
	using System.Linq;


	// === Paint ===
	public partial class OliveSceneGUI {




		#region --- VAR ---


		// Const
		private const int PAINTING_LIMIT = 64;



		// Short
		private static bool IgnoreGUIPaint {
			get {
				return MouseInGUI.a || PickingReplaceTileFrom || PickingReplaceTileTo;
			}
		}


		private static bool IgnoreGUIPick {
			get {
				return MouseInGUI.a;
			}
		}


		// Data
		private static readonly Vector3[] LINE_RECT_CACHE = new Vector3[8] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, };
		private static Vector3 PrevSnapedMouseWorldPos = Vector3.zero;
		private static Vector3? MouseDownSnapedWorldPos = null;
		private static Rect? DraggingWorldRect = null;
		private static bool DragInSelection = false;
		private static bool MouseInSelection = false;
		private static bool HideHighlightTemp = false;



		#endregion




		#region --- GUI ---


		private static void GizmosGUI (SceneView scene) {

			if (!EditingMap) { return; }


			// Hide Built-in Grid
			if (ToggleGridUtility.ShowGrid) {
				ToggleGridUtility.ShowGrid = false;
				Debug.Log("[Olive] Hide built-in grid during editing.");
			}

			var oldC = Handles.color;
			bool alt = Event.current.alt;
			bool ctrl = Event.current.control;
			bool shift = Event.current.shift;
			float gridSize = EditingMap.GridSize;
			int roomCount = EditingMap.RoomCount;

			// Room BG
			if (!EditingMap.OnlyShowSelectingRoom) {
				for (int i = 0; i < roomCount; i++) {
					if (i == OpeningRoomIndex) { continue; }
					var _roomTF = EditingMap.GetRoomTF(i);
					if (!_roomTF) { continue; }
					var range = EditingMap.GetRoomRange(i);
					var _roomPos = _roomTF.position;
					var roomRect = Rect.MinMaxRect(
						_roomPos.x + range.x * gridSize - 0.5f * gridSize,
						_roomPos.y + range.y * gridSize - 0.5f * gridSize,
						_roomPos.x + range.z * gridSize + 0.5f * gridSize,
						_roomPos.y + range.w * gridSize + 0.5f * gridSize
					);

					// BG
					Handles.color = Color.white;
					Handles.DrawSolidRectangleWithOutline(roomRect, Color.black * 0.618f, Color.clear);
				}
			}


			// Selecting Room
			{
				var roomTF = EditingMap.GetRoomTF(OpeningRoomIndex);
				float bold = gridSize * GIZMOS_UI_WIDTH_MUTI[GizmosUIIndex];
				Vector2 roomPos = roomTF.position;
				Vector2 gridMin = new Vector2(
					RoomMinMax.x - gridSize * 0.5f,
					RoomMinMax.y - gridSize * 0.5f
				) + roomPos;
				Vector2 gridMax = new Vector2(
					RoomMinMax.z + gridSize * 0.5001f,
					RoomMinMax.w + gridSize * 0.5001f
				) + roomPos;

				// Grid
				Handles.color = Color.black * 0.6f;

				// ||| 
				for (float x = gridMin.x; x <= gridMax.x; x += gridSize) {
					Handles.DrawLine(new Vector2(x, gridMin.y), new Vector2(x, gridMax.y));
				}

				// ≡
				for (float y = gridMin.y; y <= gridMax.y; y += gridSize) {
					Handles.DrawLine(new Vector2(gridMin.x, y), new Vector2(gridMax.x, y));
				}

				// Coord
				Handles.color = Color.black;
				SetLineRectCache(new Rect(roomPos.x - gridSize * 0.5f, gridMin.y, 0, gridMax.y - gridMin.y));
				Handles.DrawAAPolyLine(8f, LINE_RECT_CACHE);
				SetLineRectCache(new Rect(gridMin.x, roomPos.y - gridSize * 0.5f, gridMax.x - gridMin.x, 0));
				Handles.DrawAAPolyLine(8f, LINE_RECT_CACHE);

				// Outline
				switch (GizmosUI) {
					case GizmosUIType.Normal:
						Handles.DrawSolidRectangleWithOutline(
							Rect.MinMaxRect(gridMin.x, gridMin.y, gridMax.x, gridMax.y),
							Color.clear, Color.black
						);
						break;
					case GizmosUIType.Bold:
					case GizmosUIType.Extra_Bold:
						var rect = Rect.MinMaxRect(gridMin.x - bold, gridMin.y - bold, gridMax.x + bold, gridMin.y);
						SetLineRectCache(rect);
						Handles.DrawAAConvexPolygon(LINE_RECT_CACHE);
						rect.y = gridMax.y;
						SetLineRectCache(rect);
						Handles.DrawAAConvexPolygon(LINE_RECT_CACHE);
						rect = Rect.MinMaxRect(gridMin.x - bold, gridMin.y - bold, gridMin.x, gridMax.y + bold);
						SetLineRectCache(rect);
						Handles.DrawAAConvexPolygon(LINE_RECT_CACHE);
						rect.x = gridMax.x;
						SetLineRectCache(rect);
						Handles.DrawAAConvexPolygon(LINE_RECT_CACHE);
						break;
				}


				if (ShowGizmosLabel) {
					// Label
					var labelStr = GetDisplayString(roomTF.name, 12);
					Vector2 roomLocalPos = roomTF.localPosition;
					labelStr += string.Format(
						"  ({0}, {1})  {2}×{3}",
						Mathf.RoundToInt(roomLocalPos.x / gridSize),
						Mathf.RoundToInt(roomLocalPos.y / gridSize),
						Mathf.RoundToInt((RoomMinMax.z - RoomMinMax.x) / gridSize),
						Mathf.RoundToInt((RoomMinMax.w - RoomMinMax.y) / gridSize)
					);
					Handles.color = Color.white;
					Handles.Label(
						new Vector3(gridMin.x - bold, gridMax.y + bold, 0),
						labelStr,
						BlackBGLabel
					);
				}
			}


			// Move Room Handles
			{
				for (int i = 0; i < roomCount; i++) {
					if (EditingMap.OnlyShowSelectingRoom && i != OpeningRoomIndex) { continue; }
					var _roomTF = EditingMap.GetRoomTF(i);
					if (!_roomTF) { continue; }
					var range = EditingMap.GetRoomRange(i);
					// Drag to Move
					MoveRoomHandle(_roomTF, gridSize * 0.5f, gridSize,
						new Vector3(
							Mathf.Min(range.x, 0f) * gridSize + (i == OpeningRoomIndex ? -gridSize : 0),
							Mathf.Min(range.y, 0f) * gridSize
						)
					);
				}
			}


			// Open Room Button
			if (!EditingMap.OnlyShowSelectingRoom) {
				bool mouseInLeft = new Rect(0, 0, PANEL_WIDTH, scene.position.height).Contains(Event.current.mousePosition);
				for (int i = 0; i < roomCount; i++) {
					if (i == OpeningRoomIndex) { continue; }
					var _roomTF = EditingMap.GetRoomTF(i);
					if (!_roomTF) { continue; }
					var range = EditingMap.GetRoomRange(i);
					var pos = _roomTF.position + new Vector3(range.x * gridSize, range.w * gridSize);
					Handles.color = Color.black;
					float handleSize = gridSize * 0.618f;
					if (Handles.Button(pos, Quaternion.identity, handleSize, gridSize * 0.5f, Handles.CubeHandleCap) && !mouseInLeft) {
						SetOpeningRoomIndex(i);
						Event.current.Use();
					}
					Handles.color = Color.white;
					Handles.DrawSolidRectangleWithOutline(new Rect(pos.x - 0.5f * handleSize, pos.y - 0.5f * handleSize, handleSize, handleSize), Color.clear, Color.white);
					if (ShowGizmosLabel) {
						// Other Room Label
						Handles.color = Color.white;
						Handles.Label(
							new Vector3(pos.x + handleSize * 0.618f, pos.y + handleSize * 0.5f, -0.1f),
							GetDisplayString(_roomTF.name, 12), BlackBGLabel
						);
					}
				}
			}


			// Selection GUI
			MouseInSelection = false;
			if (SelectionWorldRect.HasValue) {
				var rect = SelectionWorldRect.Value;
				SetLineRectCache(rect);
				Handles.color = Color.white;
				Handles.DrawAAPolyLine(
					Mathf.Max(2f, GIZMOS_UI_WIDTH_MUTI[GizmosUIIndex] * 8f),
					LINE_RECT_CACHE
				);
				var roomTF = EditingMap.GetRoomTF(OpeningRoomIndex);
				var roomPos = Vector2.zero;
				if (roomTF) {
					roomPos = roomTF.position;
				}
				ForSelectingItems((itemTF) => {
					Handles.DrawSolidRectangleWithOutline(
						new Rect(
							(Vector2)itemTF.localPosition + roomPos - Vector2.one * gridSize * 0.5f,
							Vector2.one * gridSize
						),
						new Color(0, 0, 0, 0.6f), Color.white
					);
				});
				// Mouse In Selection
				var guiMin = HandleUtility.WorldToGUIPoint(rect.min);
				var guiMax = HandleUtility.WorldToGUIPoint(rect.max);
				var guiRect = Rect.MinMaxRect(guiMin.x, guiMax.y, guiMax.x, guiMin.y);
				MouseInSelection = guiRect.Contains(Event.current.mousePosition);
				// Cursor
				if (!ctrl && !shift && !alt) {
					EditorGUIUtility.AddCursorRect(guiRect, MouseCursor.MoveArrow);
				} else if (ctrl) {
					EditorGUIUtility.AddCursorRect(new Rect(0, 0, float.MaxValue, float.MaxValue), MouseCursor.ArrowPlus);
				} else if (shift) {
					EditorGUIUtility.AddCursorRect(new Rect(0, 0, float.MaxValue, float.MaxValue), MouseCursor.ArrowMinus);
				}
			}


			Handles.color = oldC;

		}


		private static void PaintGUI (SceneView scene) {

			if (!EditingMap) { return; }

			var type = Event.current.type;
			bool alt = Event.current.alt;
			bool ctrl = Event.current.control;
			bool shift = Event.current.shift;
			bool picking = PickingTile();
			bool pickingWithReplace = PickingReplaceTileFrom || PickingReplaceTileTo;
			var mouseButton = Event.current.button;
			var mouseGUIPos = Event.current.mousePosition;

			MouseInGUI.a = MouseInGUI.a || new Rect(0, 0, PANEL_WIDTH, scene.position.height).Contains(Event.current.mousePosition);

			float gridSize = EditingMap.GridSize;
			Ray ray = HandleUtility.GUIPointToWorldRay(mouseGUIPos);
			var mouseWorldPos = ray.origin;
			mouseWorldPos.z = EditingMap.transform.position.z;

			// Limit
			if (MouseDownSnapedWorldPos.HasValue && SelectingPaletteItem) {
				mouseWorldPos.x = Mathf.Clamp(mouseWorldPos.x, MouseDownSnapedWorldPos.Value.x - PAINTING_LIMIT * gridSize, MouseDownSnapedWorldPos.Value.x + PAINTING_LIMIT * gridSize);
				mouseWorldPos.y = Mathf.Clamp(mouseWorldPos.y, MouseDownSnapedWorldPos.Value.y - PAINTING_LIMIT * gridSize, MouseDownSnapedWorldPos.Value.y + PAINTING_LIMIT * gridSize);
			}

			var snapedMousePos = EditingMap.GetSnapedCenterPosition(OpeningRoomIndex, mouseWorldPos);

			if (EditingHighlight) {
				EditingHighlight.position = snapedMousePos;
			}

			switch (type) {
				case EventType.MouseMove:
					// Move
					{
						if (DraggingWorldRect.HasValue) {
							DraggingWorldRect = null;
							SetRepaintDirty();
						}
						if (MouseDownSnapedWorldPos.HasValue) {
							MouseDownSnapedWorldPos = null;
						}
						DragInSelection = false;
						HideHighlightTemp = false;
					}
					break;
				case EventType.MouseDown:
					// Down
					{
						DraggingWorldRect = null;
						HideHighlightTemp = false;
						if (!MouseInGUI.a) {
							if (mouseButton == 0) {
								if (picking) {
									// Pick
									if (ColorPickingMode == OliveColorPickingMode.Alt_Key && PickItemIn(snapedMousePos)) {
										Event.current.Use();
									}
								} else {
									// Normal
									MouseDownSnapedWorldPos = snapedMousePos;
									DraggingWorldRect = new Rect(MouseDownSnapedWorldPos.Value - Vector3.one * gridSize * 0.5f, Vector2.one * gridSize);
									DragInSelection = !ctrl && !shift && SelectionWorldRect.HasValue && SelectionWorldRect.Value.Contains(snapedMousePos);
									if (DragInSelection) {
										DraggingWorldRect = SelectionWorldRect;
									}
									SetRepaintDirty();
									Event.current.Use();
								}
							} else if (mouseButton == 1) {
								if (picking && ColorPickingMode == OliveColorPickingMode.Mouse_Right) {
									// Pick
									if (PickItemIn(snapedMousePos)) {
										Event.current.Use();
									}
								}
								MouseDownSnapedWorldPos = null;
							}
						} else {
							MouseDownSnapedWorldPos = null;
						}
					}
					break;
				case EventType.MouseDrag:
					// Drag
					{
						if (!picking && mouseButton == 0 && MouseDownSnapedWorldPos.HasValue) {
							if (DragInSelection) {
								if (SelectionWorldRect.HasValue) {
									DraggingWorldRect = new Rect(
										(Vector3)SelectionWorldRect.Value.position + snapedMousePos - MouseDownSnapedWorldPos.Value,
										SelectionWorldRect.Value.size
									);
								}
							} else {
								var min = Vector3.Min(MouseDownSnapedWorldPos.Value, snapedMousePos) - Vector3.one * gridSize * 0.5f;
								var max = Vector3.Max(MouseDownSnapedWorldPos.Value, snapedMousePos) + Vector3.one * gridSize * 0.5f;
								DraggingWorldRect = new Rect(min, max - min);
							}
							SetRepaintDirty();
						} else if (DraggingWorldRect.HasValue) {
							DraggingWorldRect = null;
							SetRepaintDirty();
						}
					}
					break;
				case EventType.MouseUp:
				case EventType.MouseLeaveWindow:
					// Up / Leave
					{
						HideHighlightTemp = false;
						if (MouseDownSnapedWorldPos.HasValue) {
							if (mouseButton == 0 && !picking) {
								if (gridSize < OliveMap.GRIDSIZE_MIN || OpeningRoomIndex < 0 || OpeningRoomIndex >= EditingMap.MapRoot.childCount) { break; }
								var roomTF = EditingMap.GetRoomTF(OpeningRoomIndex);
								if (!roomTF) { break; }
								var roomPos = roomTF.position;
								if (DragInSelection) {
									// Move Selection
									if (DraggingWorldRect.HasValue && SelectionWorldRect.HasValue) {
										var oldRect = SelectionWorldRect.Value;
										var newRect = DraggingWorldRect.Value;
										if (newRect.position != oldRect.position) {
											var layerTF = EditingMap.GetLayerTF(OpeningRoomIndex, SelectingLayerIndex);
											if (layerTF) {
												Vector3 posOffset = newRect.position - oldRect.position;
												ForSelectingItems((itemTF) => {
													RegistObjUndo(itemTF, "Move Item");
													Vector3 newPos = itemTF.localPosition + posOffset;
													newPos.z = 0;
													itemTF.localPosition = newPos;
												});
											}
											_SelectionWorldRect = null;
											ClearRoomMinMax();
											SetAutoTileDirty(false, SelectionMode.AllLayers);
										}
									}
								} else {
									if (SelectingPaletteItem) {
										// Brush
										switch (ToolType) {
											case OliveToolType.Rect: {
													var halfFix = Vector3.one * gridSize * 0.5f;
													var min = (Vector3.Min(MouseDownSnapedWorldPos.Value, snapedMousePos) - halfFix - roomPos) / gridSize;
													var max = (Vector3.Max(MouseDownSnapedWorldPos.Value, snapedMousePos) + halfFix - roomPos) / gridSize;
													PaintItemForRect(
														Mathf.CeilToInt(min.x), Mathf.CeilToInt(min.y),
														Mathf.FloorToInt(max.x), Mathf.FloorToInt(max.y)
													);
												}
												break;
											case OliveToolType.Line: {
													ForRectInLine(
														DraggingWorldRect.Value,
														MouseDownSnapedWorldPos.Value,
														snapedMousePos, gridSize,
														(rect) => {
															var min = (rect.min - (Vector2)roomPos) / gridSize;
															var max = (rect.max - (Vector2)roomPos) / gridSize;
															PaintItemForRect(
																Mathf.CeilToInt(min.x), Mathf.CeilToInt(min.y),
																Mathf.FloorToInt(max.x), Mathf.FloorToInt(max.y)
															);
														}
													);
												}
												break;
											case OliveToolType.Bucket:
												if (SelectingPaletteItem) {
													PaintOrSelectForBucket(SelectingPaletteItem, snapedMousePos);
												}
												break;
										}
										ClearSelection();
									} else {
										// Create Selection
										SelectionCacheList.Clear();
										switch (ToolType) {
											case OliveToolType.Rect:
												ForSelectItemsIn(
													Vector3.Min(MouseDownSnapedWorldPos.Value, snapedMousePos),
													Vector3.Max(MouseDownSnapedWorldPos.Value, snapedMousePos),
													(itemTF) => {
														SelectionCacheList.Add(itemTF.gameObject);
														itemTF.SetAsLastSibling();
														return false;
													}
												);
												break;
											case OliveToolType.Line:
												ForRectInLine(
													DraggingWorldRect.Value,
													MouseDownSnapedWorldPos.Value,
													snapedMousePos, gridSize,
													(rect) => {
														var halfFix = Vector2.one * gridSize * 0.5f;
														ForSelectItemsIn(
															rect.min + halfFix,
															rect.max - halfFix,
															(itemTF) => {
																SelectionCacheList.Add(itemTF.gameObject);
																itemTF.SetAsLastSibling();
																return false;
															}
														);
													}
												);
												break;
											case OliveToolType.Bucket:
												PaintOrSelectForBucket(null, snapedMousePos);
												break;
										}
										if (ctrl) {
											// Add Select
											SelectionCacheList.AddRange(Selection.objects);
											Selection.objects = SelectionCacheList.ToArray();
										} else if (shift) {
											// Remove Select
											var tempList = new List<Object>(Selection.objects);
											foreach (var obj in SelectionCacheList) {
												tempList.Remove(obj);
											}
											Selection.objects = tempList.ToArray();
										} else if (!alt) {
											// Select
											Selection.objects = SelectionCacheList.ToArray();
										}
										SetOverlapDirty();
									}
								}
								SetRepaintDirty();
							}
							MouseDownSnapedWorldPos = null;
						}
						if (DraggingWorldRect.HasValue) {
							DraggingWorldRect = null;
						}
						DragInSelection = false;
					}
					break;
				case EventType.Repaint:
					// Repaint
					{
						bool usingHighlight = false;
						if (!DragInSelection) {
							usingHighlight = SelectingPaletteItem;
							if (DraggingWorldRect.HasValue) {
								// Dragging Rect
								var colorA = SelectingPaletteItem ? PAINT_COLOR : Color.clear;
								var colorB = SelectingPaletteItem ? Color.clear : Color.white;
								switch (ToolType) {
									case OliveToolType.Rect:
										Handles.DrawSolidRectangleWithOutline(DraggingWorldRect.Value, colorA, colorB);
										break;
									case OliveToolType.Line:
										if (!MouseDownSnapedWorldPos.HasValue) { break; }
										ForRectInLine(
											DraggingWorldRect.Value,
											MouseDownSnapedWorldPos.Value,
											snapedMousePos, gridSize,
											(rect) => {
												Handles.DrawSolidRectangleWithOutline(
													rect,
													colorA, colorB
												);
											}
										);
										break;
								}
								usingHighlight = false;
							} else if (!MouseInSelection && !HideHighlightTemp && !MouseInGUI.a) {
								// Handles Frame Highlight
								var oldC = Handles.color;
								if (SelectingPaletteItem && SelectingPaletteItem.IsEmpty) {
									// Empty Cross
									SetLineRectCache(snapedMousePos, EditingMap.GridSize, false);
									Handles.color = ToolType == OliveToolType.Bucket ? Color.cyan : Color.white;
									Handles.DrawLines(LINE_RECT_CACHE);
									SetLineRectCache(snapedMousePos, EditingMap.GridSize);
									Handles.DrawLines(LINE_RECT_CACHE);
								} else if (ShowHightlightCursor) {
									// Frame
									SetLineRectCache(snapedMousePos, EditingMap.GridSize);
									if (SelectingPaletteItem && SelectingPaletteItem.AssetType == OliveMap.PaletteAssetType.Color) {
										// Color
										int detailIndex = SelectingPaletteItem.IsRandom ? PaletteRandomIndex : 0;
										var color = detailIndex >= 0 && detailIndex < SelectingPaletteItem.ItemCount ? (Color)SelectingPaletteItem.GetItemAt(detailIndex) * PaintingTint : Color.clear;
										color.a *= 0.309f;
										Handles.DrawSolidRectangleWithOutline(new Rect(
											snapedMousePos.x - gridSize * 0.5f,
											snapedMousePos.y - gridSize * 0.5f,
											gridSize, gridSize
										), color, Color.white);
									} else {
										// Other Type or Null
										Handles.color = ToolType == OliveToolType.Bucket ? Color.cyan : Color.white;
										Handles.DrawLines(LINE_RECT_CACHE);
									}
								}
								Handles.color = oldC;
							}
						} else {
							// Drag Move Selection
							if (DraggingWorldRect.HasValue) {
								Handles.DrawSolidRectangleWithOutline(
									DraggingWorldRect.Value,
									PAINT_COLOR_ALT,
									Color.white
								);
							}
							usingHighlight = false;
						}

						// Hightlight Transform
						usingHighlight = usingHighlight && !HideHighlightTemp && !MouseInGUI.a && !pickingWithReplace;
						if (EditingHighlight) {
							if (EditingHighlight.gameObject.activeSelf != usingHighlight) {
								EditingHighlight.gameObject.SetActive(usingHighlight);
							}
						}

						break;
					}
			}
			if (PrevSnapedMouseWorldPos != snapedMousePos) {
				SetRepaintDirty();
				PrevSnapedMouseWorldPos = snapedMousePos;
			}
		}


		#endregion




		#region --- LGC ---


		// GUI
		private static void SetLineRectCache (Vector3 centerWorldPos, float size, bool isRect = true) {
			size *= 0.5f;
			if (isRect) {
				LINE_RECT_CACHE[7] = LINE_RECT_CACHE[0] = centerWorldPos + new Vector3(-size, -size);
				LINE_RECT_CACHE[1] = LINE_RECT_CACHE[2] = centerWorldPos + new Vector3(-size, size);
				LINE_RECT_CACHE[3] = LINE_RECT_CACHE[4] = centerWorldPos + new Vector3(size, size);
				LINE_RECT_CACHE[5] = LINE_RECT_CACHE[6] = centerWorldPos + new Vector3(size, -size);
			} else {
				LINE_RECT_CACHE[0] = LINE_RECT_CACHE[4] = centerWorldPos + new Vector3(-size, -size);
				LINE_RECT_CACHE[1] = LINE_RECT_CACHE[5] = centerWorldPos + new Vector3(size, size);
				LINE_RECT_CACHE[2] = LINE_RECT_CACHE[6] = centerWorldPos + new Vector3(-size, size);
				LINE_RECT_CACHE[3] = LINE_RECT_CACHE[7] = centerWorldPos + new Vector3(size, -size);
			}
		}


		private static void SetLineRectCache (Rect rect, float z = 0f) {
			LINE_RECT_CACHE[7] = LINE_RECT_CACHE[0] = new Vector3(rect.xMin, rect.yMin, z);
			LINE_RECT_CACHE[1] = LINE_RECT_CACHE[2] = new Vector3(rect.xMin, rect.yMax, z);
			LINE_RECT_CACHE[3] = LINE_RECT_CACHE[4] = new Vector3(rect.xMax, rect.yMax, z);
			LINE_RECT_CACHE[5] = LINE_RECT_CACHE[6] = new Vector3(rect.xMax, rect.yMin, z);
		}


		// Misc
		private static void PaintItemForRect (int minX, int minY, int maxX, int maxY) {

			if (!EditingMap || !EditingMap.MapRoot || !SelectingPaletteItem) { return; }

			var roomTF = EditingMap.GetRoomTF(OpeningRoomIndex);
			var layerTF = EditingMap.GetLayerTF(OpeningRoomIndex, SelectingLayerIndex);

			if (!roomTF || !layerTF) { return; }


			switch (SelectingPaletteItem.AssetType) {
				case OliveMap.PaletteAssetType.Sprite:
				case OliveMap.PaletteAssetType.Prefab:
					bool needFixAutoTile = SelectingPaletteItem.Type == OliveMap.PaletteItemTyle.AutoTile || SelectingPaletteItem.IsEmpty;
					// Delete Old
					ForTriggeringLayer(SelectingPaletteItem == EMPTY_PALETTE_ITEM ? LayerSelectionMode : SelectionMode.CurrentLayer, (_layerTF) => {
						bool triggered = false;
						EditingMap.ForItemsIn(_layerTF, minX, minY, maxX, maxY, (item) => {
							if (!needFixAutoTile) {
								var pal = EditingMap.GetPaletteItem(OliveMap.GetItemID(item));
								if (pal && pal.Type == OliveMap.PaletteItemTyle.AutoTile) {
									needFixAutoTile = true;
								}
							}
							DestroyObject(item.gameObject);
							triggered = true;
							return false;
						});
						return triggered;
					});

					// Spawn New
					if (!SelectingPaletteItem.IsEmpty) {
						for (int x = minX; x <= maxX; x++) {
							for (int y = minY; y <= maxY; y++) {
								var itemTF = EditingMap.SpawnItem(SelectingPaletteItem, layerTF, x, y, (int)PaintingRotation, PaintingTint, PaletteRandomIndex, ShowCollider);
								if (itemTF) {
									RegistCreateUndo(itemTF.gameObject, "Spawn Item");
								}
								if (SelectingPaletteItem.IsRandom) {
									ResetSelectingPaletteItemRandomIndex();
								}
							}
						}
					}
					ClearRoomMinMax();
					if (needFixAutoTile) {
						SetAutoTileDirty(false, SelectionMode.AllLayers);
					}
					break;
				case OliveMap.PaletteAssetType.Color:
					if (!SelectingPaletteItem.IsEmpty) {
						Color? color = null;
						EditingMap.ForItemsIn(OpeningRoomIndex, SelectingLayerIndex, minX, minY, maxX, maxY, (itemTF) => {
							if (!color.HasValue) {
								color = PaintingTint;
								var colorObj = SelectingPaletteItem.GetItemAt(PaletteRandomIndex);
								color *= colorObj != null ? (Color)colorObj : Color.white;
							}
							SetItemColor(itemTF, color.Value);
							if (SelectingPaletteItem.IsRandom) {
								ResetSelectingPaletteItemRandomIndex();
								color = null;
							}
							return false;
						});
					}
					break;
			}

			// End
			ClearSelection();
			if (SelectingPaletteItem.IsRandom) {
				SetSelectingPaletteItem(SelectingPaletteIndex, true);
			}
		}


		private static void PaintOrSelectForBucket (OliveMap.PaletteItem pal, Vector2 pos) {
			if (!EditingMap) { return; }
			ResetSelectingPaletteItemRandomIndex();
			int detailIndex = pal && !pal.IsEmpty && pal.IsRandom ? Mathf.Clamp(PaletteRandomIndex, 0, pal.ItemCount - 1) : 0;
			// Do it
			var queue = new Queue<Vector2Transform>();
			var posItemMap = new Dictionary<Int2, Transform>();
			float gridSize = EditingMap.GridSize;
			Color? color = null;
			// Triggering Layer
			ForTriggeringLayer(pal ? SelectionMode.CurrentLayer : LayerSelectionMode == SelectionMode.AllLayers ? SelectionMode.TopLayer : LayerSelectionMode, (layerTF, roomIndex, layerIndex) => {
				// Queue
				queue.Clear();
				posItemMap.Clear();
				var sourceTF = EditingMap.GetItemIn(layerTF, pos);
				long sourceID = OliveMap.GetItemID(sourceTF);
				if (!pal && sourceID < 0) { return false; }
				queue.Enqueue(new Vector2Transform(pos, sourceTF));
				Vector2 basicPos = layerTF.position;
				Vector2 fixedPos = (pos - basicPos) / gridSize;
				posItemMap.Add(new Int2(Mathf.RoundToInt(fixedPos.x), Mathf.RoundToInt(fixedPos.y)), sourceTF);
				var roomRange = new Int4(EditingMap.GetRoomRange(roomIndex));
				int maxCount;
				if (sourceID >= 0) {
					maxCount = layerTF.childCount;
				} else {
					maxCount = ((roomRange.x1 - roomRange.x0 + 1) * (roomRange.y1 - roomRange.y0 + 1)) - layerTF.childCount;
				}
				int progressBarCount = 256;
				float maxQueueCount = 12f;
				float maxProgress = 0f;
				for (; queue.Count > 0 && maxCount >= 0; maxCount--) {
					var posTF = queue.Dequeue();
					if (pal) {
						// Paint
						switch (pal.AssetType) {
							case OliveMap.PaletteAssetType.Sprite:
							case OliveMap.PaletteAssetType.Prefab:
								if (posTF.tf) {
									DestroyObject(posTF.tf.gameObject);
								}
								if (!pal.IsEmpty) {
									var newItemTF = EditingMap.SpawnItem(
										pal, layerTF,
										Mathf.RoundToInt((posTF.v.x - basicPos.x) / gridSize),
										Mathf.RoundToInt((posTF.v.y - basicPos.y) / gridSize),
										(int)PaintingRotation,
										PaintingTint,
										detailIndex,
										ShowCollider
									);
									if (newItemTF) {
										RegistCreateUndo(newItemTF.gameObject, "Spawn Item");
									}
								}
								break;
							case OliveMap.PaletteAssetType.Color:
								if (!color.HasValue) {
									color = PaintingTint;
									if (pal.AssetType == OliveMap.PaletteAssetType.Color && !pal.IsEmpty) {
										var colorObj = pal.GetItemAt(detailIndex);
										color *= colorObj != null ? (Color)colorObj : Color.white;
									}
								}
								SetItemColor(posTF.tf, color.Value);
								break;
						}
						if (!pal.IsEmpty && pal.IsRandom) {
							ResetSelectingPaletteItemRandomIndex();
							detailIndex = PaletteRandomIndex;
							color = null;
						}

					} else {
						// Select
						if (posTF.tf) {
							SelectionCacheList.Add(posTF.tf.gameObject);
						}
					}

					// Do It
					var intP = new Int2(
						Mathf.RoundToInt((posTF.v.x - basicPos.x) / gridSize),
						Mathf.RoundToInt((posTF.v.y - basicPos.y) / gridSize)
					);
					for (int i = 0; i < 4; i++) {
						var _intP = intP;
						int deltaX = i == 2 ? -1 : i == 3 ? 1 : 0;
						int deltaY = i == 0 ? -1 : i == 1 ? 1 : 0;
						_intP.x += deltaX;
						_intP.y += deltaY;
						if (_intP.x < roomRange.x0 || _intP.x > roomRange.x1 || _intP.y < roomRange.y0 || _intP.y > roomRange.y1) { continue; }
						if (!posItemMap.ContainsKey(_intP)) {
							var _p = posTF.v;
							_p.x += gridSize * deltaX;
							_p.y += gridSize * deltaY;
							var _item = EditingMap.GetItemIn(layerTF, _p);
							if ((!_item && sourceID < 0) || OliveMap.GetItemID(_item) == sourceID) {
								queue.Enqueue(new Vector2Transform(_p, _item));
							}
							posItemMap.Add(_intP, _item);
						}
					}
					// Progress Bar
					if (progressBarCount > 0) {
						progressBarCount--;
					} else {
						maxQueueCount = Mathf.Max(maxQueueCount, queue.Count);
						maxProgress = Mathf.Max(maxProgress, 1f - (queue.Count / maxQueueCount));
						EditorUtility.DisplayProgressBar("Bucket", pal ? "Painting..." : "Selecting...", maxProgress);
					}
				}
				return true;
			});
			EditorUtility.ClearProgressBar();
			ClearRoomMinMax();
			SetAutoTileDirty(false, SelectionMode.CurrentLayer);
		}


		private static bool PickItemIn (Vector2 snapedWorldPos) {
			if (!EditingMap || !EditingMap.MapRoot) { return false; }
			int index = -3;
			ForTriggeringLayer(SelectionMode.TopLayer, (layerTF, layerIndex) => {
				EditingMap.ForItemsIn(OpeningRoomIndex, layerIndex, snapedWorldPos, Vector2.one * EditingMap.GridSize * 0.1f, (item) => {
					long id = OliveMap.GetItemID(item);
					var pal = EditingMap.GetPaletteItem(id);
					if (pal) {
						var newIndex = EditingMap.Palette.IndexOf(pal);
						if (newIndex >= 0) {
							index = newIndex;
							return true;
						}
					}
					return false;
				});
				return index != -3;
			});
			if (index != -3) {
				SetSelectingPaletteItem(index);
			} else {
				SetSelectingPaletteItem(!PickingReplaceTileFrom && !PickingReplaceTileTo ? -2 : -1);
			}
			return index != -3;
		}


		private static void ForRectInLine (Rect worldRect, Vector2 startPosition, Vector2 endPosition, float gridSize, System.Action<Rect> action) {
			int stepAxis = worldRect.width < worldRect.height ? 0 : 1;
			int stepCount = Mathf.RoundToInt(worldRect.size[stepAxis] / gridSize);
			float stepAltPos = 0f;
			float stepGrow = gridSize;
			float stepAltGrow = worldRect.size[1 - stepAxis] / stepCount;
			if (endPosition[1 - stepAxis] < startPosition[1 - stepAxis]) {
				stepAltGrow *= -1f;
			}
			if (endPosition[stepAxis] < startPosition[stepAxis]) {
				stepGrow *= -1f;
			}
			Vector2 pos = Vector2.zero;
			Vector2 size = Vector2.zero;
			size[stepAxis] = gridSize;
			size[1 - stepAxis] = gridSize;
			pos[stepAxis] = startPosition[stepAxis];
			pos[1 - stepAxis] = startPosition[1 - stepAxis];
			for (int step = 0; step < stepCount; step++) {
				stepAltPos += stepAltGrow;
				float newPos = Mathf.Round(stepAltPos / gridSize) * gridSize + startPosition[1 - stepAxis];
				size[1 - stepAxis] = Mathf.Abs(pos[1 - stepAxis] - newPos);
				var p = pos;
				p[stepAxis] -= gridSize * 0.5f;
				p[1 - stepAxis] -= stepAltGrow < 0f ? size[1 - stepAxis] - gridSize * 0.5f : gridSize * 0.5f;
				action(new Rect(p, size));
				pos[stepAxis] += stepGrow;
				pos[1 - stepAxis] = newPos;
			}
		}


		private static void ForSelectItemsIn (Vector2 min, Vector2 max, System.Func<Transform, bool> func) {
			ForTriggeringLayer(LayerSelectionMode, (layerTF, i) => {
				bool triggered = false;
				EditingMap.ForItemsIn(
					OpeningRoomIndex, i,
					min, max - min,
					(itemTF) => {
						func.Invoke(itemTF);
						triggered = true;
						return false;
					}
				);
				return triggered;
			});
		}


		private static void SetItemColor (Transform itemTF, Color color) {
			if (!itemTF || itemTF.childCount == 0) { return; }
			var sr = itemTF.GetChild(0).GetComponent<SpriteRenderer>();
			if (sr) {
				RegistObjUndo(sr, "Set Color");
				sr.color = color;
			}
		}


		private static void MoveRoomHandle (Transform roomTF, float handleSize, float gridSize, Vector3 handleOffset) {
			var posFix = Vector3.one * (handleSize * 2f - gridSize) * 0.5f + handleOffset;
			var oldPos = roomTF.position;
			Handles.color = Color.black;
			var newPos = Handles.FreeMoveHandle(
				oldPos + posFix,
				roomTF.rotation,
				handleSize,
				Vector2.zero,
				Handles.DotHandleCap
			) - posFix;
			if (oldPos != newPos) {
				RegistObjUndo(roomTF, "Move Room");
				var snapFix = -EditingMap.transform.position;
				newPos.x = Mathf.Round((newPos.x + snapFix.x) / gridSize) * gridSize - snapFix.x;
				newPos.y = Mathf.Round((newPos.y + snapFix.y) / gridSize) * gridSize - snapFix.y;
				newPos.z = 0f;
				roomTF.position = newPos;
				_SelectionWorldRect = null;
				HideHighlightTemp = true;
			}
		}


		#endregion


	}
}