namespace OliveMapEditor {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;
	using Saving;


	// === Main ===
	public partial class OliveSceneGUI {

		#region --- VAR ---

		// Const
		private static readonly string[] PALETTE_SORT_MODES = new string[OliveMap.PALETTE_SORT_COUNT] { "Unsorted", "by Type", "by Time" };
		private static readonly Color BG_COLOR = EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f, 1f) : new Color(0.8f, 0.8f, 0.8f, 1f);
		private static readonly Color BG_COLOR_ALT = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.82f, 0.82f, 0.82f, 1f);
		private static readonly Color HIGHT_LIGHT_COLOR = new Color(0.1f, 0.5f, 0.2f, 0.5f);
		private static readonly Color HIGHT_LIGHT_COLOR_ALT = EditorGUIUtility.isProSkin ? new Color(0.1f, 0.5f, 0.2f, 0.2f) : new Color(0.1f, 0.5f, 0.2f, 0.4f);
		private static readonly Color PAINT_COLOR = new Color(0.32f, 0.68f, 0.37f, 1f);
		private static readonly Color PAINT_COLOR_ALT = new Color(0.32f, 0.68f, 0.37f, 0.2f);
		private static readonly Color HIGHT_LIGHT_COLOR_BLUE = EditorGUIUtility.isProSkin ? new Color(0.1f, 0.2f, 0.5f, 0.4f) : new Color(0.1f, 0.2f, 0.5f, 0.4f);
		private static readonly Color PAINTING_ITEM_NUMBER_BG_COLOR = EditorGUIUtility.isProSkin ? Color.black : new Color(0.9f, 0.9f, 0.9f, 1f);
		private static readonly OliveMap.PaletteItem EMPTY_PALETTE_ITEM = new OliveMap.PaletteItem(OliveMap.PaletteItemTyle.Random, OliveMap.PaletteAssetType.Sprite);
		private const float PANEL_WIDTH = 200;
		private const float SCROLL_WIDTH = 16;
		private const float BAR_HEIGHT = 18;
		private const float ITEM_HEIGHT_LARGE = 36;
		private const float ITEM_HEIGHT = 18;
		private const float ITEM_HEIGHT_SMALL = 16;
		private const float FIELD_GAP_Y = 2;
		private const float SELECTING_ITEM_POS_Z = -0.05f;


		// Short
		public static OliveMap EditingMap {
			get;
			private set;
		}

		private static OliveMap.PaletteItem SelectingPaletteItem {
			get {
				return
					!EditingMap ? null :
					(SelectingPaletteIndex >= 0 && SelectingPaletteIndex < EditingMap.Palette.Count) ? EditingMap.Palette[SelectingPaletteIndex] :
					SelectingPaletteIndex == -2 ? EMPTY_PALETTE_ITEM :
					null;
			}
		}

		private static Vector4 RoomMinMax {
			get {
				if (!_RoomMinMax.HasValue) {
					var minMax = Vector4.zero;
					Transform roomTF = EditingMap ? EditingMap.GetRoomTF(OpeningRoomIndex) : null;
					if (roomTF) {
						int layerCount = EditingMap.GetLayerCount(OpeningRoomIndex);
						for (int layerIndex = 0; layerIndex < layerCount; layerIndex++) {
							Transform layerTF = EditingMap.GetLayerTF(OpeningRoomIndex, layerIndex);
							if (!layerTF) { 
								continue; 
							}
							int itemCount = layerTF.childCount;
							for (int i = 0; i < itemCount; i++) {
								var itemPos = layerTF.GetChild(i).localPosition;
								minMax.x = Mathf.Min(minMax.x, itemPos.x);
								minMax.y = Mathf.Min(minMax.y, itemPos.y);
								minMax.z = Mathf.Max(minMax.z, itemPos.x);
								minMax.w = Mathf.Max(minMax.w, itemPos.y);
							}
						}
					}
					_RoomMinMax = minMax;
				}
				return _RoomMinMax.Value;
			}
		}

		private static Rect? SelectionWorldRect {
			get {
				if (!_SelectionWorldRect.HasValue) {
					var min = new Vector2(float.MaxValue, float.MaxValue);
					var max = new Vector2(float.MinValue, float.MinValue);
					bool hasValue = ForSelectingItems((tf) => {
						Vector2 pos = tf.position;
						min = Vector2.Min(min, pos);
						max = Vector2.Max(max, pos);
					});
					if (hasValue) {
						float halfGridSize = EditingMap.GridSize * 0.5f;
						_SelectionWorldRect = Rect.MinMaxRect(
							min.x - halfGridSize, min.y - halfGridSize,
							max.x + halfGridSize, max.y + halfGridSize
						);
					}
				}
				return _SelectionWorldRect;
			}
		}

		private static OliveColorPickingMode ColorPickingMode {
			get {
				return (OliveColorPickingMode)ColorPickingModeIndex.Value;
			}
		}

		private static SelectionMode LayerSelectionMode {
			get {
				return (SelectionMode)SelectionModeIndex.Value;
			}
		}

		private static OliveToolType ToolType {
			get {
				return (OliveToolType)ToolIndex.Value;
			}
			set {
				ToolIndex.Value = (int)value;
				ToolIndex.TrySave();
			}
		}

		private static GizmosUIType GizmosUI {
			get {
				return (GizmosUIType)GizmosUIIndex.Value;
			}
		}

		private static int SelectingLayerIndex {
			get {
				return EditingMap ? EditingMap.SelectingLayerIndex : 0;
			}
			set {
				if (!EditingMap) { return; }
				EditingMap.SelectingLayerIndex = value;
			}
		}

		private static int OpeningRoomIndex {
			get {
				return EditingMap ? EditingMap.SelectingRoomIndex : 0;
			}
			set {
				if (!EditingMap) { return; }
				EditingMap.SelectingRoomIndex = value;
			}
		}


		// GUI Style
		private static GUIStyle ClearBoxStyle {
			get {
				return _ClearBoxStyle ??= new GUIStyle(GUI.skin.box) {
					margin = new RectOffset(),
				};
			}
		}

		private static GUIStyle BoxStyle {
			get {
				if (_BoxStyle == null) {
					_BoxStyle = new GUIStyle() {
						normal = new GUIStyleState() {
							textColor = new Color32(128, 128, 128, 255),
						},
						border = new RectOffset(2, 2, 2, 2),
						alignment = TextAnchor.LowerCenter,
						fontSize = 36,
					};
				}
				return _BoxStyle;
			}
		}

		private static GUIStyle CenteredPopupStyle {
			get {
				if (_CenteredPopupStyle == null) {
					_CenteredPopupStyle = new GUIStyle(EditorStyles.popup) {
						alignment = TextAnchor.MiddleCenter
					};
				}
				return _CenteredPopupStyle;
			}
		}

		private static GUIStyle BlackBGLabel {
			get {
				if (_BlackBGLabel == null) {
					var c = new Color32(0, 0, 0, 128);
					var texture = new Texture2D(1, 1) {
						filterMode = FilterMode.Point
					};
					texture.SetPixels32(new Color32[1] { c });
					texture.Apply();
					_BlackBGLabel = new GUIStyle(GUI.skin.label) {
						normal = new GUIStyleState() {
							background = texture,
							textColor = Color.white,
						},
					};
				}
				return _BlackBGLabel;
			}
		}


		// Data
		private readonly static Dictionary<IndexedPaletteItem, Texture2D> PalettePreviewMap = new Dictionary<IndexedPaletteItem, Texture2D>();
		private readonly static List<OliveMap> AllMapsInScene = new List<OliveMap>();
		private static GUIStyle _ClearBoxStyle = null;
		private static GUIStyle _BoxStyle = null;
		private static GUIStyle _CenteredPopupStyle = null;
		private static GUIStyle _BlackBGLabel = null;
		private static Texture2D _OliveIconTexture = null;
		private static GameObject PrevLinkedPrefab = null;
		private static Transform EditingRoot = null;
		private static Transform EditingHighlight = null;
		private static string[] SameParentMapNames = new string[0];
		private static int EditingIndexInParentMaps = -1;
		private static int SelectingPaletteIndex = -1; // -1 Nothing | -2 Erase | 0+ Index
		private static int PaletteRandomIndex = 0;
		private static float PaletteScrollValue = 0f;
		private static float DetailScrollValue = 0f;
		private static float PaletteHeight = 300;
		private static float InspectorPanelHeight = 200;
		private static bool ShowEditButtons = false;
		private static Bool2 MouseInGUI = new Bool2(false, false);
		private static Vector4? _RoomMinMax = null;
		private static Rect? _SelectionWorldRect = null;
		private static OliveInspectorType OliveInspector = OliveInspectorType.Map;
		private static OliveRotationType PaintingRotation = OliveRotationType.Up;
		private static bool ShowReplaceTileGUI = false;
		private static OliveMap.PaletteItem ReplaceTileFrom = null;
		private static OliveMap.PaletteItem ReplaceTileTo = null;
		private static bool PickingReplaceTileFrom = false;
		private static bool PickingReplaceTileTo = false;

		// Saving
		private readonly static EditorSavingInt PaletteSizeIndex = new EditorSavingInt("OliveSceneGUI.PaletteSizeIndex", 1);
		private readonly static EditorSavingBool ShowHierarchyIcon = new EditorSavingBool("OliveSceneGUI.ShowHierarchyIcon", true);
		private readonly static EditorSavingBool ShowHightlightCursor = new EditorSavingBool("OliveSceneGUI.ShowHightlightCursor", true);
		private readonly static EditorSavingBool FocusOnOpeningRoom = new EditorSavingBool("OliveSceneGUI.FocusOnOpeningRoom", true);
		private readonly static EditorSavingInt ColorPickingModeIndex = new EditorSavingInt("OliveSceneGUI.ColorPickingModeIndex", 0);
		private readonly static EditorSavingBool OliveUndoable = new EditorSavingBool("OliveSceneGUI.OliveUndoable", true);
		private readonly static EditorSavingInt SelectionModeIndex = new EditorSavingInt("OliveSceneGUI.SelectionModeIndex", 0);
		private readonly static EditorSavingInt ToolIndex = new EditorSavingInt("OliveSceneGUI.ToolIndex", 0);
		private readonly static EditorSavingInt GizmosUIIndex = new EditorSavingInt("OliveSceneGUI.GizmosUIIndex", 1);
		private readonly static EditorSavingBool ShowGizmosLabel = new EditorSavingBool("OliveSceneGUI.ShowGizmosLabel", true);
		private readonly static EditorSavingBool ShowPaintingItem = new EditorSavingBool("OliveSceneGUI.ShowPaintingItem", true);
		private readonly static EditorSavingColor PaintingTint = new EditorSavingColor("OliveSceneGUI.PaintingTint", Color.white);
		private readonly static EditorSavingBool AllLayerForReplaceTile = new EditorSavingBool("OliveSceneGUI.AllLayerForReplaceTile", true);
		private readonly static EditorSavingBool AllRoomForReplaceTile = new EditorSavingBool("OliveSceneGUI.AllRoomForReplaceTile", false);
		private readonly static EditorSavingBool ShowCollider = new EditorSavingBool("OliveSceneGUI.ShowCollider", false);
		private readonly static EditorSavingBool ShowExTools = new EditorSavingBool("OliveSceneGUI.ShowExTools", false);

		// Cache
		private readonly static Dictionary<Int4, TransformLongInt> PosItemMapForAutoTile = new Dictionary<Int4, TransformLongInt>();
		private readonly static Dictionary<Int2, Transform> OverlapCacheMap = new Dictionary<Int2, Transform>();
		private readonly static List<Object> SelectionCacheList = new List<Object>();
		private readonly static List<LongInt4> CopyCacheList = new List<LongInt4>(); // id detailIndex x y rotation
		private static long NeedFixAutoTileID = -1;
		private static int NeedFixAutoTile = -1; // -1 No Need | -2 Need | -3 Force Fix | 0+ Fix Index
		private static SelectionMode FixAutoTileSelectionMode = SelectionMode.CurrentLayer;
		private static bool FixAutoTileForAllRooms = false;
		private static bool NeedFixAllRoomLayerItem = false;
		private static bool NeedDeleteOverlap = false;
		private static bool MouseInGUIRepaint = false;
		private static bool NeedRepaintSceneView = false;


		#endregion

		#region --- MSG ---

		[InitializeOnLoadMethod]
		private static void Init () {

			SceneView.duringSceneGui -= DuringSceneGUI;
			SceneView.duringSceneGui += DuringSceneGUI;

			EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
			EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;

			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
			Undo.undoRedoPerformed += OnUndoRedoPerformed;

			PrefabUtility.prefabInstanceUpdated -= HideMapRoot;
			PrefabUtility.prefabInstanceUpdated += HideMapRoot;

			Selection.selectionChanged -= SelectionChanged;
			Selection.selectionChanged += SelectionChanged;

			EditorApplication.update -= EditorUpdate;
			EditorApplication.update += EditorUpdate;

			LoadSettings();
		}


		private static void DuringSceneGUI (SceneView scene) {

			if (!EditingMap) { return; }

			// Editing Prefab 
			var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null) {
				EditMap(null);
				return;
			}

			AddDefaultRoomLayerIfEmpty();

			// Tool
			if (Tools.current != Tool.None) {
				Tools.current = Tool.None;
			}

			// 2D Mode
			if (!SceneView.currentDrawingSceneView.in2DMode) {
				SceneView.currentDrawingSceneView.in2DMode = true;
				Debug.Log("[Olive] Scene view must set to 2D mode when editing a map.");
			}


			// Default Control
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

			// Global Vars
			float viewHeight = scene.position.height - 16f;
			PaletteHeight = viewHeight * 400f / 834f;
			InspectorPanelHeight = viewHeight - PaletteHeight - 2f * BAR_HEIGHT;

			// Cancel Control
			Event e = Event.current;
			if (e.type == EventType.KeyDown && !e.control && !e.shift && !e.alt) {
				if (e.keyCode == KeyCode.Return) {
					if (EditingMap) {
						GUI.FocusControl("");
						SetRepaintDirty();
						e.Use();
					}
				}
			}

			// Sub GUI
			PreKeyGUI();
			GizmosGUI(scene);
			PaintGUI(scene);
			ViewGUI(scene);
			PaletteGUI();
			InspectorBarGUI();
			KeyGUI();
			CleanGUI();
		}


		private static void EditorUpdate () {
			// Repaint
			if (NeedRepaintSceneView) {
				SceneView.RepaintAll();
				NeedRepaintSceneView = false;
			}
		}


		private static void OnHierarchyGUI (int instanceID, Rect selectionRect) {
			var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
			if (!obj) { return; }
			if (EditingMap) {
				if (obj == EditingMap.gameObject) {
					var oldC = GUI.color;
					GUI.color = new Color(0.1f, 1, 0.2f, 0.618f);
					var rect = selectionRect;
					rect.width = EditorGUIUtility.currentViewWidth - 6f;
					rect.x = 1f;
					GUI.Box(rect, GUIContent.none);
					rect.x = rect.width - 46;
					rect.width = 42;
					GUI.Label(rect, "Editing");
					GUI.color = oldC;
					return;
				}
			}
			if (ShowHierarchyIcon) {
				var r = obj.GetComponent<OliveMap>();
				if (r) {
					var rect = selectionRect;
					rect.x = EditorGUIUtility.currentViewWidth - rect.height - 12;
					rect.y += 2;
					rect.height *= 0.9f;
					rect.width = rect.height;
					var oldC = GUI.color;
					var c = EditorGUIUtility.isProSkin ? Color.white : Color.black;
					GUI.color = c;
					GUI.DrawTexture(rect, GetOliveIconTexture(r));
					GUI.color = oldC;
				}
			}
		}


		private static void OnUndoRedoPerformed () {
			if (!EditingMap || !OliveUndoable) { return; }
			// Fix All Stuff
			EditingMap.FixAllRoomLayerItem(OpeningRoomIndex, null, true);
			SetSelectingPaletteItem(SelectingPaletteIndex, true);
			NeedFixAllRoomLayerItem = false;
			// Auto Tile
			SetAutoTileDirty(false, SelectionMode.AllLayers);
			// Clear
			PalettePreviewMap.Clear();
			ClearRoomMinMax();
			EditingMap.ClearIdPalMap();
		}


		private static void SelectionChanged () {
			if (!EditingMap) { return; }
			_SelectionWorldRect = null;
			// Selection Z
			EditingMap.FixItemsPositionZ(OpeningRoomIndex, SelectingLayerIndex, 0f);
			bool hasItem = false;
			ForSelectingItems((itemTF) => {
				itemTF.SetAsLastSibling();
				var pos = itemTF.localPosition;
				pos.z = SELECTING_ITEM_POS_Z;
				itemTF.localPosition = pos;
				hasItem = true;
			});
			if (!hasItem) {
				SetAutoTileDirty();
			}
		}


		// Menu Item
		[MenuItem("GameObject/Olive Map/New Map", false, 0)]
		[MenuItem("Tools/Olive Map Editor/Create New Map")]
		public static void CreateNewMap () {
			Selection.activeTransform = new GameObject("New Map", typeof(OliveMap)).transform;
		}


		[MenuItem("Tools/Olive Map Editor/Edit Selecting Map")]
		public static void EditSelectMap () {
			GameObject g = Selection.activeObject as GameObject;
			if (!g) { return; }
			var map = g.GetComponent<OliveMap>();
			if (!map) { return; }
			EditMap(map);
		}


		[MenuItem("Tools/Olive Map Editor/Edit Selecting Map", validate = true)]
		public static bool EditSelectMap_Validate () {
			var g = Selection.activeObject as GameObject;
			if (!g) { return false; }
			return g.GetComponent<OliveMap>();
		}


		// Sub GUI
		private static void InspectorBarGUI () {

			if (!EditingMap) { return; }

			Handles.BeginGUI();
			GUILayout.BeginArea(new Rect(0, PaletteHeight + BAR_HEIGHT, PANEL_WIDTH, InspectorPanelHeight + BAR_HEIGHT));

			// Bar
			var viewRect = new Rect(0, 0, PANEL_WIDTH, BAR_HEIGHT);
			GUI.Box(viewRect, GUIContent.none, EditorStyles.toolbar);

			// Button
			const float BUTTON_WIDTH = 52;
			for (int i = 0; i < OLIVE_INSPECTOR_COUNT; i++) {
				var r = new Rect(i * BUTTON_WIDTH, viewRect.y, BUTTON_WIDTH, viewRect.height);
				var style = EditorStyles.toolbarButton;
				if ((int)OliveInspector == i) {
					style = new GUIStyle(style) {
						normal = style.active,
					};
				}
				var ins = (OliveInspectorType)i;
				if (GUI.Button(r, ins.ToString(), style)) {
					OliveInspector = ins;
					DetailScrollValue = 0f;
				}
			}

			// Show Edit Buttons Button
			if (OliveInspector == OliveInspectorType.Map || OliveInspector == OliveInspectorType.Detail) {
				var r = new Rect(viewRect.xMax - 23, viewRect.y, 22, viewRect.height);
				var style = EditorStyles.toolbarButton;
				if (ShowEditButtons) {
					style = new GUIStyle(EditorStyles.toolbarButton) {
						normal = EditorStyles.toolbarButton.active,
					};
				}
				if (GUI.Button(r, "✏", style)) {
					ShowEditButtons = !ShowEditButtons;
				}
			}


			GUILayout.EndArea();
			Handles.EndGUI();

			switch (OliveInspector) {
				default:
				case OliveInspectorType.Map:
					HierarchyGUI();
					break;
				case OliveInspectorType.Detail:
					DetailGUI();
					break;
				case OliveInspectorType.Setting:
					SettingGUI();
					break;
			}

		}


		private static void PaletteGUI () {

			if (!EditingMap) { return; }

			var data = EditingMap;

			bool mouseDownInPalette = false;
			bool mouseDownInPaletteItem = false;
			int row = PaletteSizeIndex == 0 ? 9 : PaletteSizeIndex == 1 ? 5 : 3;
			float itemSize = (PANEL_WIDTH - SCROLL_WIDTH) / row;
			float PAINTING_HEIGHT = ShowPaintingItem ? 122 : 18;
			const float THUMBNAIL_SIZE = 86;

			Handles.BeginGUI();
			GUILayout.BeginArea(new Rect(0, 0, PANEL_WIDTH, PaletteHeight + BAR_HEIGHT));


			// Content
			{
				var paletteRect = new Rect(0, BAR_HEIGHT, PANEL_WIDTH - SCROLL_WIDTH, PaletteHeight - PAINTING_HEIGHT);
				float height = Mathf.Ceil((data.Palette.Count + 1) / (float)row + 1f) * itemSize;

				// Logic
				switch (Event.current.type) {
					// Mouse
					case EventType.ScrollWheel:
						if (paletteRect.Contains(Event.current.mousePosition)) {
							PaletteScrollValue += Event.current.delta.y * (paletteRect.height / height) * 0.1f;
							Event.current.Use();
						}
						break;
					case EventType.MouseDown:
						if (paletteRect.Contains(Event.current.mousePosition)) {
							mouseDownInPalette = true;
						}
						break;
				}
				PaletteDragAndDropGUI(paletteRect);

				// Background
				ColorBlock(paletteRect, BG_COLOR);

				// Scroll GUI
				PaletteScrollValue = GUI.VerticalScrollbar(
					new Rect(PANEL_WIDTH - SCROLL_WIDTH, paletteRect.y, SCROLL_WIDTH, paletteRect.height),
					PaletteScrollValue, Mathf.Clamp01(paletteRect.height / height), 0, 1
				);

				// Content
				float scrollFix = Mathf.Min(-height * PaletteScrollValue, 0);
				int palButtonGap = PaletteSizeIndex == 0 ? 1 : 3;
				int palCount = data.Palette.Count;

				for (int i = 0; i < palCount; i++) {

					var item = data.Palette[i];
					if (!item) { continue; }

					var r = new Rect(
						i % row * itemSize,
						i / row * itemSize + paletteRect.y + scrollFix,
						itemSize,
						itemSize
					);

					// Mouse Down Logic
					if (mouseDownInPalette && r.Contains(Event.current.mousePosition)) {
						switch (Event.current.button) {
							case 0: // Left
								SetSelectingPaletteItem(i);
								break;
							case 1: // Right
								ShowPaletteItemMenu(i);
								break;
						}
						mouseDownInPaletteItem = true;
					}

					// BG
					var _r = r;
					_r.x += palButtonGap;
					_r.y += palButtonGap;
					_r.width -= 2 * palButtonGap;
					_r.height -= 2 * palButtonGap;
					var preview = GetPalettePreview(item);
					switch (item.AssetType) {
						case OliveMap.PaletteAssetType.Sprite:
						case OliveMap.PaletteAssetType.Prefab:
							if (preview) {
								GUI.DrawTexture(_r, preview, ScaleMode.ScaleToFit);
							} else {
								GUI.Box(_r, GUIContent.none, ClearBoxStyle);
							}
							break;
						case OliveMap.PaletteAssetType.Color:
							if (item.ItemCount > 0) {
								ColorPaletteItemThumbnailGUI(_r, item, 6);
							} else {
								GUI.Box(_r, GUIContent.none, ClearBoxStyle);
							}
							break;
					}

					// Highlight
					if (item == SelectingPaletteItem) {
						ColorBlock(r, HIGHT_LIGHT_COLOR);
					}

					// Random Number
					PaletteRandomNumberGUI(_r, item, PaletteSizeIndex == 0 ? 5 : PaletteSizeIndex == 1 ? 9 : 13);

				}

				// Alt Button
				{
					var oldE = GUI.enabled;
					int altButtonCount = palCount;
					palButtonGap = PaletteSizeIndex == 0 ? 1 : PaletteSizeIndex == 1 ? 8 : 16;

					// Erase Button
					var eButtonRect = new Rect(
						altButtonCount % row * itemSize + palButtonGap,
						altButtonCount / row * itemSize + paletteRect.y + scrollFix + palButtonGap,
						itemSize - palButtonGap * 2f,
						itemSize - palButtonGap * 2f
					);
					var oldC = GUI.color;

					if (SelectingPaletteItem == EMPTY_PALETTE_ITEM) {
						GUI.color = new Color(0.5f, 0.9f, 0.6f, 1);
					}
					if (GUI.Button(eButtonRect, "R")) {
						SetSelectingPaletteItem(-2);
					}
					GUI.color = oldC;

					if (ShowEditButtons) {

						// Add Button
						altButtonCount++;
						eButtonRect.position = new Vector2(
							altButtonCount % row * itemSize + palButtonGap,
							altButtonCount / row * itemSize + paletteRect.y + scrollFix + palButtonGap
						);
						if (mouseDownInPalette && eButtonRect.Contains(Event.current.mousePosition)) {
							mouseDownInPaletteItem = true;
						}
						if (GUI.Button(eButtonRect, "+")) {
							if (SelectingPaletteItem) {
								TryDuplicatePaletteItem(SelectingPaletteItem);
							} else {
								TryAddPaletteItem();
							}
						}

						// Remove Button
						altButtonCount++;
						eButtonRect.position = new Vector2(
							altButtonCount % row * itemSize + palButtonGap,
							altButtonCount / row * itemSize + paletteRect.y + scrollFix + palButtonGap
						);
						if (mouseDownInPalette && eButtonRect.Contains(Event.current.mousePosition)) {
							mouseDownInPaletteItem = true;
						}
						GUI.enabled = SelectingPaletteItem && SelectingPaletteItem != EMPTY_PALETTE_ITEM;
						if (GUI.Button(eButtonRect, "×")) {
							TryDeletePaletteItem(SelectingPaletteIndex);
						}

					}

					GUI.enabled = oldE;
				}

				// Hint
				if (palCount == 0) {
					var r = new Rect(paletteRect);
					r.x += 6;
					r.y += itemSize + 6;
					r.width -= 12;
					r.height = 36;
					EditorGUI.HelpBox(r, "Drag prefab/sprite here to create new item.", MessageType.Info);
				}

			}


			// Bar
			{
				// Bar
				var barRect = new Rect(0, 0, PANEL_WIDTH, BAR_HEIGHT);
				GUI.Box(barRect, GUIContent.none, EditorStyles.toolbar);

				// Map Name
				var popRect = new Rect(barRect) { x = 2, width = 114 };
				if (Event.current.type == EventType.MouseDown && popRect.Contains(Event.current.mousePosition)) {
					RefreshAllSceneMaps(EditingMap);
				}
				int newMapIndex = EditorGUI.Popup(
					popRect,
					EditingIndexInParentMaps,
					SameParentMapNames,
					EditorStyles.toolbarPopup
				);
				if (newMapIndex != EditingIndexInParentMaps) {
					EditMap(AllMapsInScene[newMapIndex]);
				}

			}


			// Painting Item
			var paintingItemRect = new Rect(0, PaletteHeight - PAINTING_HEIGHT + BAR_HEIGHT, PANEL_WIDTH, PAINTING_HEIGHT);

			// Painting BG
			ColorBlock(new Rect(paintingItemRect), BG_COLOR_ALT);

			if (GUI.Button(new Rect(paintingItemRect) { height = 18, }, ShowPaintingItem ? "▼" : "▲", EditorStyles.toolbarButton)) {
				ShowPaintingItem.Value = !ShowPaintingItem;
				ShowPaintingItem.TrySave();
			}
			if (ShowPaintingItem) {

				var r = new Rect(paintingItemRect) {
					y = paintingItemRect.y + 26,
					x = 4
				};

				var oldE = GUI.enabled;
				GUI.enabled = SelectingPaletteItem != EMPTY_PALETTE_ITEM;

				// Thumbnail
				r.height = r.width = THUMBNAIL_SIZE;
				if (SelectingPaletteItem) {
					switch (SelectingPaletteItem.AssetType) {
						case OliveMap.PaletteAssetType.Prefab:
						case OliveMap.PaletteAssetType.Sprite:
							var preview = GetPalettePreview(SelectingPaletteItem);
							if (preview) {
								var oldC = GUI.color;
								GUI.color = SelectingPaletteItem.Tint;
								GUI.DrawTexture(r, preview, ScaleMode.ScaleToFit);
								GUI.color = oldC;
							} else {
								GUI.Box(r, GUIContent.none);
							}
							break;
						case OliveMap.PaletteAssetType.Color:
							if (!SelectingPaletteItem.IsEmpty) {
								ColorPaletteItemThumbnailGUI(r, SelectingPaletteItem);
							}
							break;
					}
				} else {
					GUI.Box(r, GUIContent.none);
				}
				PaletteRandomNumberGUI(r, SelectingPaletteItem, 16, 0.3f, true);
				if (SelectingPaletteItem && SelectingPaletteItem.IsEmpty) {
					if (SelectingPaletteItem != EMPTY_PALETTE_ITEM) {
						EditorGUI.HelpBox(r, "Drag " + SelectingPaletteItem.AssetType.ToString() + " here to add them in.\n\nUse \"Detail\" panel below to add new item in.", MessageType.None);
					} else {
						EditorGUI.HelpBox(r, "(Erase)", MessageType.None);
					}
				}

				if (SelectingPaletteItem && SelectingPaletteItem != EMPTY_PALETTE_ITEM) {

					// Type
					r.x += r.width + 4;
					float basicX = r.x;
					r.y = paintingItemRect.y + 26;
					r.width = (paintingItemRect.width - r.x - 8) * 0.5f;
					r.height = ITEM_HEIGHT;
					GUI.enabled = SelectingPaletteItem.AssetType != OliveMap.PaletteAssetType.Color;
					var newType = (OliveMap.PaletteItemTyle)EditorGUI.EnumPopup(
						r, SelectingPaletteItem.Type, EditorStyles.miniButtonLeft
					);
					if (SelectingPaletteItem.AssetType == OliveMap.PaletteAssetType.Color &&
						SelectingPaletteItem.Type == OliveMap.PaletteItemTyle.AutoTile
					) {
						// Fix Type For Color
						newType = OliveMap.PaletteItemTyle.Random;
					}
					if (newType != SelectingPaletteItem.Type) {
						RegistUndo("Palette Item Type Changed");
						SelectingPaletteItem.Type = newType;
						if (data.PaletteSortMode == OliveMap.OlivePaletteSortMode.Type) {
							SortPaletteItems();
						}
						RemovePreviewFor(SelectingPaletteItem);
						SetAllRoomLayerItemDirty();
					}

					// Asset Type
					r.x += r.width;
					GUI.enabled = oldE;
					var newAssetType = (OliveMap.PaletteAssetType)EditorGUI.EnumPopup(
						r, SelectingPaletteItem.AssetType, EditorStyles.miniButtonRight
					);
					if (newAssetType != SelectingPaletteItem.AssetType) {
						RegistUndo("Palette Item Asset Type Changed");
						SelectingPaletteItem.AssetType = newAssetType;
						if (data.PaletteSortMode == OliveMap.OlivePaletteSortMode.Type) {
							SortPaletteItems();
						}
						RemovePreviewFor(SelectingPaletteItem);
						SetAllRoomLayerItemDirty();
					}


					// Collider Type
					r.x = basicX;
					r.y += r.height + 4;
					r.width *= 2f;
					GUI.enabled = SelectingPaletteItem.AssetType == OliveMap.PaletteAssetType.Sprite;
					var newColliderType = (OliveMap.OliveColliderType)EditorGUI.EnumPopup(
						r, SelectingPaletteItem.ColliderType, EditorStyles.miniButton
					);
					if (newColliderType != SelectingPaletteItem.ColliderType) {
						SelectingPaletteItem.ColliderType = newColliderType;
					}


					// Move Left
					r.y += r.height + 4;
					r.width *= 0.25f;
					var mode = data.PaletteSortMode;
					GUI.enabled = mode == OliveMap.OlivePaletteSortMode.Unsorted && data.Palette.Count > 0 && SelectingPaletteItem != data.Palette[0];
					if (GUI.Button(r, "◀", EditorStyles.miniButtonLeft) && mode == OliveMap.OlivePaletteSortMode.Unsorted) {
						TryMovePaletteItemAt(data.Palette.IndexOf(SelectingPaletteItem), true);
						SetRepaintDirty();
					}

					// Clear / Delete 
					r.x += r.width;
					r.width *= 2f;
					GUI.enabled = SelectingPaletteItem && SelectingPaletteItem != EMPTY_PALETTE_ITEM;
					bool isEmpty = SelectingPaletteItem.IsEmpty;
					if (GUI.Button(r, isEmpty ? "Delete" : "Clear", EditorStyles.miniButtonMid)) {
						if (isEmpty) {
							TryDeletePaletteItem(SelectingPaletteIndex);
						} else if (Dialog("Confirm", "Clear this palette item?", "Clear", "Cancel")) {
							RegistUndo("Clear Palette Item");
							SelectingPaletteItem.Items.Clear();
							RemovePreviewFor(SelectingPaletteItem);
							SetAllRoomLayerItemDirty();

						}
					}

					// Move Right
					r.x += r.width;
					r.width *= 0.5f;
					GUI.enabled = mode == OliveMap.OlivePaletteSortMode.Unsorted && data.Palette.Count > 0 && SelectingPaletteItem != data.Palette[data.Palette.Count - 1];
					if (GUI.Button(r, "▶", EditorStyles.miniButtonRight) && mode == OliveMap.OlivePaletteSortMode.Unsorted) {
						TryMovePaletteItemAt(data.Palette.IndexOf(SelectingPaletteItem), false);
						SetRepaintDirty();
					}
					GUI.enabled = oldE;

					switch (SelectingPaletteItem.AssetType) {
						case OliveMap.PaletteAssetType.Sprite:
							// Tint
							r.x = basicX;
							r.y += r.height + 6;
							r.width = paintingItemRect.width - r.x - 8;
							var tint = ColorField(r, SelectingPaletteItem.Tint, "Tint");
							if (tint != SelectingPaletteItem.Tint) {
								SelectingPaletteItem.Tint = tint;
								SetSelectingPaletteItem(SelectingPaletteIndex, true);
							}
							break;
						case OliveMap.PaletteAssetType.Prefab:
							// Scale
							r.x = basicX;
							r.y += r.height + 6;
							r.width = paintingItemRect.width - r.x - 8;
							var newScale = SelectingPaletteItem.Scale;
							FloatFieldWithButtons(r, ref newScale, "Scale", 32);
							if (newScale != SelectingPaletteItem.Scale) {
								SelectingPaletteItem.Scale = newScale;
								SetSelectingPaletteItem(SelectingPaletteIndex, true);
							}
							break;
						default:
						case OliveMap.PaletteAssetType.Color:
							break;
					}

					// Drag
					PaletteDragAndDropGUI(paintingItemRect, SelectingPaletteItem, DragAndDropVisualMode.Link);

					// Cancel Selection
					if (Event.current.type == EventType.MouseDown && paintingItemRect.Contains(Event.current.mousePosition)) {
						GUI.FocusControl("");
						SetRepaintDirty();
					}
				}

				GUI.enabled = oldE;
			}


			// End
			if (mouseDownInPalette) {
				Event.current.Use();
				if (!mouseDownInPaletteItem) {
					SetSelectingPaletteItem(-1);
					if (Event.current.button == 1) {
						ShowPaletteMenu();
					}
				}
			}

			GUILayout.EndArea();
			Handles.EndGUI();
		}


		private static void HierarchyGUI () {

			if (!EditingMap) { return; }

			Handles.BeginGUI();
			GUILayout.BeginArea(new Rect(0, BAR_HEIGHT + PaletteHeight + BAR_HEIGHT, PANEL_WIDTH, InspectorPanelHeight));

			var viewRect = new Rect(0, 0, PANEL_WIDTH, InspectorPanelHeight);

			int roomCount = EditingMap.RoomCount;
			int layerCount = EditingMap.GetLayerCount(OpeningRoomIndex);
			float contentHeight = (roomCount + layerCount + 1) * (ITEM_HEIGHT_SMALL + FIELD_GAP_Y) + 56;
			int needRemove = -1;

			// BG
			ColorBlock(viewRect, BG_COLOR);

			// Scroll Bar
			DetailScrollValue = ScrollGUI(DetailScrollValue, viewRect, contentHeight, InspectorPanelHeight);
			float scrollFix = Mathf.Min(-contentHeight * DetailScrollValue, 0);

			// Content
			{
				var oldE = GUI.enabled;
				float ALT_BUTTON_WIDTH = ShowEditButtons ? 62 : 4;

				var rect = new Rect(
					0, scrollFix + 2 + viewRect.y,
					PANEL_WIDTH - ALT_BUTTON_WIDTH - SCROLL_WIDTH - 16,
					ITEM_HEIGHT_SMALL
				);

				for (int i = 0; i < roomCount; i++) {

					var r = rect;
					rect.y += ITEM_HEIGHT_SMALL + FIELD_GAP_Y;

					var roomTF = EditingMap.GetRoomTF(i);
					if (!roomTF) { continue; }

					// Click
					var _r = r;
					_r.width += 24;
					if (Event.current.type == EventType.MouseDown && _r.Contains(Event.current.mousePosition)) {
						if (OpeningRoomIndex != i) {
							Event.current.Use();
						}
						SetOpeningRoomIndex(i);
					}

					// Name
					r.x = 2;
					var roomName = roomTF.name;
					GUI.enabled = i == OpeningRoomIndex;
					StringField(r, ref roomName);
					if (roomName != roomTF.name) {
						roomTF.name = roomName;
					}

					// Buttons
					if (ShowEditButtons) {
						r.x += r.width + 6;
						r.width = ALT_BUTTON_WIDTH / 3f;
						GUI.enabled = i > 0;
						if (GUI.Button(r, "▲", EditorStyles.miniButtonLeft) && i > 0) {
							RegistUndo("Move Room");
							MoveRoom(EditingMap, i, true);
							if (OpeningRoomIndex == i) {
								SetOpeningRoomIndex(OpeningRoomIndex - 1);
							} else if (OpeningRoomIndex == i - 1) {
								SetOpeningRoomIndex(OpeningRoomIndex + 1);
							}
						}
						r.x += r.width;
						GUI.enabled = i < roomCount - 1;
						if (GUI.Button(r, "▼", EditorStyles.miniButtonMid) && i < roomCount - 1) {
							RegistUndo("Move Room");
							MoveRoom(EditingMap, i, false);
							if (OpeningRoomIndex == i) {
								SetOpeningRoomIndex(OpeningRoomIndex + 1);
							} else if (OpeningRoomIndex == i + 1) {
								SetOpeningRoomIndex(OpeningRoomIndex - 1);
							}
						}
						GUI.enabled = oldE;
						r.x += r.width;
						if (GUI.Button(r, "×", EditorStyles.miniButtonRight)) {
							needRemove = i;
						}
					}
					GUI.enabled = oldE;

					// Highlight
					if (OpeningRoomIndex == i) {
						r.x = 0f;
						r.width = PANEL_WIDTH - SCROLL_WIDTH;
						ColorBlock(r, HIGHT_LIGHT_COLOR_BLUE);
					}

					// Layer
					if (i == OpeningRoomIndex) {
						rect.y += LayerGUI(rect);
					}

				}

				// Add Button
				rect.y += 4;
				if (GUI.Button(new Rect(0, rect.y, 62, ITEM_HEIGHT), "+ Room ", EditorStyles.miniButtonRight)) {
					TryAddRoom();
				}

				GUI.enabled = oldE;

			}

			// Cancel
			if (Event.current.type == EventType.MouseDown) {
				GUI.FocusControl("");
				SetRepaintDirty();
			}

			// Remove
			if (needRemove >= 0) {
				TryDeleteRoomAt(needRemove);
			}

			GUILayout.EndArea();
			Handles.EndGUI();
		}


		private static float LayerGUI (Rect baseRect) {

			var oldE = GUI.enabled;
			const float ALT_BUTTON_WIDTH = 82;
			int layerCount = EditingMap.GetLayerCount(OpeningRoomIndex);
			int needRemove = -1;
			var rect = baseRect;
			const float BASE_X = 16;
			rect.x += BASE_X;
			rect.y += 6;
			rect.width -= rect.x + 12;

			// BG
			var _bgLineRect = new Rect(
				BASE_X * 0.5f,
				baseRect.y - 2,
				PANEL_WIDTH - BASE_X * 0.5f - SCROLL_WIDTH,
				layerCount * (rect.height + FIELD_GAP_Y) + 34
			);
			ColorBlock(_bgLineRect, EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.05f) : new Color(0, 0, 0, 0.1f));

			// Line
			_bgLineRect.width = 1f;
			ColorBlock(_bgLineRect, EditorGUIUtility.isProSkin ? Color.grey : Color.black);

			// Content
			for (int i = 0; i < layerCount; i++) {

				var r = rect;
				rect.y += rect.height + FIELD_GAP_Y;
				var layerTF = EditingMap.GetLayerTF(OpeningRoomIndex, i);

				// Click
				var _r = r;
				if (Event.current.type == EventType.MouseDown && _r.Contains(Event.current.mousePosition)) {
					if (SelectingLayerIndex != i) {
						Event.current.Use();
					}
					SetSelectingLayerIndex(i);
				}

				// Name
				r.x = BASE_X;
				var layerName = layerTF.name;
				GUI.enabled = i == SelectingLayerIndex;
				StringField(r, ref layerName);
				if (layerName != layerTF.name) {
					layerTF.name = layerName;
				}

				// Buttons
				r.x += r.width + 6;
				r.width = ALT_BUTTON_WIDTH / 4f;
				GUI.enabled = oldE;
				if (GUI.Button(r, layerTF.gameObject.activeSelf ? "●" : "", ShowEditButtons ? EditorStyles.miniButtonLeft : EditorStyles.miniButton)) {
					layerTF.gameObject.SetActive(!layerTF.gameObject.activeSelf);
				}

				if (ShowEditButtons) {
					r.x += r.width;
					GUI.enabled = i > 0;
					if (GUI.Button(r, "▲", EditorStyles.miniButtonMid) && i > 0) {
						RegistUndo("Move Layer");
						MoveLayer(EditingMap, i, true);
						if (SelectingLayerIndex == i) {
							SelectingLayerIndex--;
						} else if (SelectingLayerIndex == i - 1) {
							SelectingLayerIndex++;
						}
					}

					r.x += r.width;
					GUI.enabled = i < layerCount - 1;
					if (GUI.Button(r, "▼", EditorStyles.miniButtonMid) && i < layerCount - 1) {
						RegistUndo("Move Layer");
						MoveLayer(EditingMap, i, false);
						if (SelectingLayerIndex == i) {
							SelectingLayerIndex++;
						} else if (SelectingLayerIndex == i + 1) {
							SelectingLayerIndex--;
						}
					}
					GUI.enabled = oldE;

					r.x += r.width;
					if (GUI.Button(r, "×", EditorStyles.miniButtonRight)) {
						needRemove = i;
					}
				}
				GUI.enabled = oldE;

				// Highlight
				if (SelectingLayerIndex == i) {
					r.x = BASE_X;
					r.width = PANEL_WIDTH - SCROLL_WIDTH - BASE_X;
					ColorBlock(r, HIGHT_LIGHT_COLOR_ALT);
				}

			}


			// Add Button
			if (GUI.Button(new Rect(
				BASE_X + 4, rect.y + 4,
				62, ITEM_HEIGHT
			), "+ Layer", EditorStyles.miniButton)) {
				TryAddLayer();
			}

			// Remove
			if (needRemove >= 0) {
				TryDeleteLayerAt(needRemove);
			}

			return rect.y - baseRect.y + 24 + ITEM_HEIGHT;
		}


		private static void DetailGUI () {

			if (!EditingMap) { return; }

			const float GAP_Y = 6;

			Handles.BeginGUI();
			GUILayout.BeginArea(new Rect(0, BAR_HEIGHT + PaletteHeight + BAR_HEIGHT, PANEL_WIDTH, InspectorPanelHeight));

			var viewRect = new Rect(0, 0, PANEL_WIDTH, InspectorPanelHeight);

			// BG
			ColorBlock(viewRect, BG_COLOR);

			if (!SelectingPaletteItem || SelectingPaletteItem == EMPTY_PALETTE_ITEM) {
				viewRect.x += 12;
				viewRect.y += 12;
				viewRect.width -= 24;
				viewRect.height = 42;
				EditorGUI.HelpBox(viewRect, SelectingPaletteItem ? "(E for Erase)" : "Nothing selected in the palette above.", MessageType.Info);
				GUILayout.EndArea();
				Handles.EndGUI();
				return;
			}

			int itemCount = SelectingPaletteItem.ItemCount;
			bool isAutoTile = SelectingPaletteItem.Type == OliveMap.PaletteItemTyle.AutoTile;
			float contentHeight = (itemCount + 1) * (
				isAutoTile ? ITEM_HEIGHT_LARGE * 3f + GAP_Y : ITEM_HEIGHT_SMALL + FIELD_GAP_Y
			) + 36;

			DetailScrollValue = ScrollGUI(DetailScrollValue, viewRect, contentHeight, InspectorPanelHeight);
			float scrollFix = Mathf.Min(-contentHeight * DetailScrollValue, 0);

			// Content
			int needRemove = -1;
			int needInsert = -1;

			// AutoTile
			if (isAutoTile) {

				var lineBGColor = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.015f) : new Color(0, 0, 0, 0.05f);
				var mouseGuiPos = Event.current.mousePosition;

				for (int i = 0; i < SelectingPaletteItem.ItemCount; i++) {

					// Line BG
					if (i % 2 == 1) {
						ColorBlock(new Rect(
							0,
							GAP_Y + i * (3 * ITEM_HEIGHT_LARGE + GAP_Y) + scrollFix + 2 + viewRect.y,
							viewRect.width - SCROLL_WIDTH,
							3 * ITEM_HEIGHT_LARGE
						), lineBGColor);
					}

					// Range Check
					float minY = GAP_Y + i * (3 * ITEM_HEIGHT_LARGE + GAP_Y) + scrollFix + 2 + viewRect.y;
					float maxY = minY + ITEM_HEIGHT_LARGE * 3;
					if (minY > viewRect.yMax || maxY < viewRect.yMin) { continue; }

					// Content
					var itemData = SelectingPaletteItem.Items[i];
					if (!itemData) { continue; }

					var itemObject = SelectingPaletteItem.GetItemAt(i);

					int index = 0;
					for (int y = 0; y < 3; y++) {
						for (int x = 0; x < 3; x++) {

							var r = new Rect(
								12 + x * ITEM_HEIGHT_LARGE,
								GAP_Y + i * (3 * ITEM_HEIGHT_LARGE + GAP_Y) + y * ITEM_HEIGHT_LARGE + scrollFix + 2 + viewRect.y,
								ITEM_HEIGHT_LARGE,
								ITEM_HEIGHT_LARGE
							);

							// Block 9x9
							if (x == 1 && y == 1) {

								// Index Label
								GUI.Label(new Rect(r) { x = 0, width = 12 }, i.ToString(), EditorStyles.centeredGreyMiniLabel);

								// Sprite Object
								switch (SelectingPaletteItem.AssetType) {
									case OliveMap.PaletteAssetType.Sprite:
									case OliveMap.PaletteAssetType.Prefab:
										var newItem = EditorGUI.ObjectField(
											r, itemObject as Object, SelectingPaletteItem.AssetSystemType, false
										);
										if ((itemObject as Object) != newItem) {
											SelectingPaletteItem.SetItemAt(newItem, i);
											RemovePreviewFor(SelectingPaletteItem, i);
											SetAllRoomLayerItemDirty();
											SetAutoTileDirty(SelectingPaletteItem.ID, i, SelectionMode.AllLayers, true);
										}
										break;
									case OliveMap.PaletteAssetType.Color:
										var newColor = ColorField(r, (Color)itemObject, "");
										if (newColor != (Color)itemObject) {
											SelectingPaletteItem.SetItemAt(newColor, i);
											RemovePreviewFor(SelectingPaletteItem, i);
											SetAllRoomLayerItemDirty();
											SetAutoTileDirty(SelectingPaletteItem.ID, i, SelectionMode.AllLayers, true);
										}
										break;
								}

								if (!r.Contains(mouseGuiPos)) {
									var thumbnail = GetPalettePreview(SelectingPaletteItem, i);
									if (thumbnail) {
										ColorBlock(r, BG_COLOR);
										GUI.DrawTexture(
											new Rect(
												r.x + 6, r.y + 6,
												r.width - 12, r.height - 12
											),
											thumbnail,
											ScaleMode.ScaleAndCrop
										);
									}
									if (MouseInGUIRepaint) {
										MouseInGUIRepaint = false;
										SetRepaintDirty();
									}
								} else {
									if (!MouseInGUIRepaint) {
										MouseInGUIRepaint = true;
										SetRepaintDirty();
									}
								}
							} else {
								// AutoTile
								var useAj = SelectingPaletteItem.GetAutoTileBitAt(i, index);
								if (GUI.Button(r, "", GUI.skin.box)) {
									if (useAj.HasValue) {
										if (useAj.Value) {
											useAj = null;
										} else {
											useAj = true;
										}
									} else {
										useAj = false;
									}
									SelectingPaletteItem.SetAutoTileBitAt(i, index, useAj);
									SetAllRoomLayerItemDirty();
									SetAutoTileDirty(SelectingPaletteItem.ID, i, SelectionMode.AllLayers, true);
								}
								// Block
								if (useAj.HasValue) {
									GUI.Box(
										new Rect(r.x + 5, r.y + 5, r.width - 10, r.height - 10),
										useAj.Value ? "■" : "□",
										BoxStyle
									);
								}
								index++;
							}

						}
					}


					// Buttons
					var oldE = GUI.enabled;

					if (ShowEditButtons) {

						var rect = new Rect(
							33 + 3 * ITEM_HEIGHT_LARGE,
							GAP_Y + i * (3 * ITEM_HEIGHT_LARGE + GAP_Y) + scrollFix + 2 + viewRect.y + 18,
							22, ITEM_HEIGHT_LARGE - 12
						);

						GUI.enabled = i > 0;
						if (GUI.Button(rect, "▲")) {
							RegistUndo("Move Palette Detail Item");
							SelectingPaletteItem.MoveItemAt(i, true);
							SetAutoTileDirty(true, SelectionMode.AllLayers, true);
							RemovePreviewFor(SelectingPaletteItem, i);
							RemovePreviewFor(SelectingPaletteItem, i - 1);
						}

						rect.x -= rect.width * 0.5f;
						rect.y += rect.height + 2;
						rect.height -= 4;
						GUI.enabled = true;
						if (GUI.Button(rect, "×", EditorStyles.miniButtonLeft)) {
							needRemove = i;
						}
						rect.x += rect.width;
						if (GUI.Button(rect, "+", EditorStyles.miniButtonRight)) {
							needInsert = i;
						}

						rect.x -= rect.width * 0.5f;
						rect.height += 4;

						rect.y += rect.height;
						GUI.enabled = i < SelectingPaletteItem.ItemCount - 1;
						if (GUI.Button(rect, "▼")) {
							RegistUndo("Move Palette Item");
							SelectingPaletteItem.MoveItemAt(i, false);
							SetAutoTileDirty(true, SelectionMode.AllLayers, true);
							RemovePreviewFor(SelectingPaletteItem, i);
							RemovePreviewFor(SelectingPaletteItem, i + 1);
						}
					} else {
						// Rotation Button
						var rot = (OliveMap.PaletteItemRotationType)EditorGUI.EnumPopup(new Rect(
							16 + 3 * ITEM_HEIGHT_LARGE,
							GAP_Y + i * (3 * ITEM_HEIGHT_LARGE + GAP_Y) + scrollFix + 2 + viewRect.y + 48,
							56, 18
						), itemData.Rotation);
						if (rot != itemData.Rotation) {
							itemData.Rotation = rot;
						}
					}
					GUI.enabled = oldE;

				}

				// Add Button
				if (GUI.Button(new Rect(
					0, GAP_Y + itemCount * (ITEM_HEIGHT_LARGE * 3 + GAP_Y) + scrollFix + 2 + viewRect.y + 4,
					62, ITEM_HEIGHT
				), "+", EditorStyles.miniButtonRight)) {
					TryAddPaletteItem(null, true, SelectingPaletteItem);
					RemovePreviewFor(SelectingPaletteItem);
				}

			} else {

				// Random List Content
				float ALT_BUTTON_WIDTH = ShowEditButtons ? 62 : 4;
				for (int i = 0; i < itemCount; i++) {

					var r = new Rect(
						24,
						i * (ITEM_HEIGHT_SMALL + FIELD_GAP_Y) + scrollFix + 2 + viewRect.y,
						PANEL_WIDTH - ALT_BUTTON_WIDTH - SCROLL_WIDTH - 30,
						ITEM_HEIGHT_SMALL
					);

					// Range Check
					if (r.y > viewRect.yMax || r.y + ITEM_HEIGHT_SMALL < viewRect.yMin) {
						continue;
					}

					// Index
					GUI.Label(new Rect(r) { width = 22, x = 2 }, i.ToString("00"));

					// Item
					var itemObject = SelectingPaletteItem.GetItemAt(i);
					if (itemObject == null || itemObject is Object) {
						Object item = itemObject as Object;
						var newItem = EditorGUI.ObjectField(r, item, SelectingPaletteItem.AssetSystemType, false);
						if (item != newItem) {
							SelectingPaletteItem.SetItemAt(newItem, i);
							RemovePreviewFor(SelectingPaletteItem, i);
							SetAllRoomLayerItemDirty();
						}
					} else if (itemObject is Color color) {
						var newColor = ColorField(new Rect(r.x + 6, r.y, r.height, r.height), color, "");
						if (newColor != color) {
							SelectingPaletteItem.SetItemAt(newColor, i);
							RemovePreviewFor(SelectingPaletteItem, i);
							SetAllRoomLayerItemDirty();
						}
					}

					// Alt Buttons
					var oldE = GUI.enabled;
					if (ShowEditButtons) {
						r.x += r.width;
						r.width = ALT_BUTTON_WIDTH / 3f;
						GUI.enabled = i > 0;
						if (GUI.Button(r, "▲", EditorStyles.miniButtonLeft) && i > 0) {
							RegistUndo("Move Palette Item");
							SelectingPaletteItem.MoveItemAt(i, true);
							RemovePreviewFor(SelectingPaletteItem, i);
							RemovePreviewFor(SelectingPaletteItem, i - 1);
						}
						r.x += r.width;
						GUI.enabled = i < itemCount - 1;
						if (GUI.Button(r, "▼", EditorStyles.miniButtonMid) && i < itemCount - 1) {
							RegistUndo("Move Palette Item");
							SelectingPaletteItem.MoveItemAt(i, false);
							if (i == 0) {
								RemovePreviewFor(SelectingPaletteItem, i);
								RemovePreviewFor(SelectingPaletteItem, i + 1);
							}
						}
						GUI.enabled = oldE;
						r.x += r.width;
						if (GUI.Button(r, "×", EditorStyles.miniButtonRight)) {
							needRemove = i;
						}
					}
					GUI.enabled = oldE;

				}


				// Add Button
				if (GUI.Button(new Rect(
					0, itemCount * (ITEM_HEIGHT_SMALL + FIELD_GAP_Y) + scrollFix + 2 + viewRect.y + 4,
					62, ITEM_HEIGHT
				), "+", EditorStyles.miniButtonRight)) {
					TryAddPaletteItem(null, true, SelectingPaletteItem);
				}


			}

			// Remove
			if (needRemove >= 0) {
				RegistUndo("Remove Palette Detail Item");
				SelectingPaletteItem.RemoveItemAt(needRemove);
				SetAutoTileDirty(true, SelectionMode.AllLayers, true);
				RemovePreviewFor(SelectingPaletteItem);
				SetSelectingPaletteItem(SelectingPaletteIndex, true);
			}

			// Add
			if (needInsert >= 0) {
				RegistUndo("Insert Palette Detail Item");
				RemovePreviewFor(SelectingPaletteItem);
				SelectingPaletteItem.InsertNewItemTo(needInsert);
				SetAutoTileDirty(true, SelectionMode.AllLayers, true);
			}

			GUILayout.EndArea();
			Handles.EndGUI();
		}


		private static void SettingGUI () {

			if (!EditingMap) { return; }

			Handles.BeginGUI();
			GUILayout.BeginArea(new Rect(0, BAR_HEIGHT + PaletteHeight + BAR_HEIGHT, PANEL_WIDTH, InspectorPanelHeight));

			var viewRect = new Rect(0, 0, PANEL_WIDTH, InspectorPanelHeight);

			int itemCount = EditingMap.RoomCount;
			const float CONTENT_HEIGHT = 386;

			// BG
			ColorBlock(viewRect, BG_COLOR);

			// Scroll Bar
			DetailScrollValue = ScrollGUI(DetailScrollValue, viewRect, CONTENT_HEIGHT, InspectorPanelHeight);
			float scrollFix = Mathf.Min(-CONTENT_HEIGHT * DetailScrollValue, 0);
			const float LABEL_WIDTH = 92;
			const float LEFT = 12;
			float buttonWidth = viewRect.width - LEFT - 4 - LABEL_WIDTH - SCROLL_WIDTH;
			var r = new Rect(LEFT, LEFT + scrollFix + viewRect.y, LABEL_WIDTH, ITEM_HEIGHT);

			// Map
			{
				// Room Spawn
				r.x = LEFT - 8;
				GUI.Label(r, "Map");

				r.x = LEFT;
				r.y += r.height + FIELD_GAP_Y;
				GUI.Label(r, "Show Room");
				r.x += r.width;
				r.width = buttonWidth;
				if (GUI.Button(r, EditingMap.OnlyShowSelectingRoom ? "Selecting Only" : "Show All", EditorStyles.miniButton)) {
					EditingMap.OnlyShowSelectingRoom = !EditingMap.OnlyShowSelectingRoom;
					SetAllRoomLayerItemDirty();
				}

				// Thickness
				r.x = LEFT;
				r.y += r.height + FIELD_GAP_Y;
				r.width = LABEL_WIDTH + buttonWidth;
				float thickness = EditingMap.Thickness;
				if (FloatFieldWithButtons(r, ref thickness, "Layer Thickness", LABEL_WIDTH)) {
					RegistUndo("Layer Thickness");
					EditingMap.Thickness = thickness;
					SetAllRoomLayerItemDirty();
				}

				// Grid Size
				r.x = LEFT;
				r.y += r.height + FIELD_GAP_Y;
				r.width = LABEL_WIDTH + buttonWidth;
				float gridSize = EditingMap.GridSize;
				if (FloatFieldWithButtons(r, ref gridSize, "Grid Size", LABEL_WIDTH)) {
					RegistUndo("Grid Size");
					EditingMap.GridSize = Mathf.Max(gridSize, OliveMap.GRIDSIZE_MIN);
					EditingMap.FixAllRoomLayerItem(OpeningRoomIndex, (tf) => {
						RegistObjUndo(tf, "Fix Stuff");
					}, true);
					SetSelectingPaletteItem(SelectingPaletteIndex, true);
					ClearRoomMinMax();
				}

			}

			// Palette
			{
				// Title
				r.x = LEFT - 8;
				r.y += r.height + FIELD_GAP_Y + 2;
				r.width = LABEL_WIDTH;
				GUI.Label(r, "Palette");

				// Palette Size
				r.x = LEFT;
				r.y += r.height + FIELD_GAP_Y;
				GUI.Label(r, "Palette Size");

				r.x += r.width;
				r.width = buttonWidth;
				if (GUI.Button(r, PaletteSizeIndex == 0 ? "Small" : PaletteSizeIndex == 1 ? "Medium" : "Large", EditorStyles.miniButton)) {
					PaletteSizeIndex.Value = (PaletteSizeIndex + 1) % 3;
					PaletteSizeIndex.TrySave();
				}

				// Sort
				r.x = LEFT;
				r.y += r.height + FIELD_GAP_Y;
				r.width = LABEL_WIDTH;
				GUI.Label(r, "Palette Sort");

				r.x += r.width;
				r.width = buttonWidth;
				int newSortIndex = EditorGUI.Popup(r, (int)EditingMap.PaletteSortMode, PALETTE_SORT_MODES, CenteredPopupStyle);
				if (newSortIndex != (int)EditingMap.PaletteSortMode) {
					RegistUndo("Sort Palette Items");
					EditingMap.PaletteSortMode = (OliveMap.OlivePaletteSortMode)newSortIndex;
					SortPaletteItems();
				}
			}


			// System
			{
				// Title
				r.x = LEFT - 8;
				r.y += r.height + FIELD_GAP_Y + 2;
				r.width = LABEL_WIDTH;
				GUI.Label(r, "System");

				// Picking
				r.x = LEFT;
				r.y += r.height + FIELD_GAP_Y;
				r.width = LABEL_WIDTH;
				GUI.Label(r, "Picker Key");

				r.x += r.width;
				r.width = buttonWidth;
				if (GUI.Button(r, COLOR_PICKER_LABELS[ColorPickingModeIndex], EditorStyles.miniButton)) {
					ColorPickingModeIndex.Value = (ColorPickingModeIndex + 1) % OLIVE_COLOR_PICKING_COUNT;
					ColorPickingModeIndex.TrySave();
				}

				// Hierarchy Icon
				r.x = LEFT;
				r.y += r.height + FIELD_GAP_Y;
				r.width = LABEL_WIDTH;
				GUI.Label(r, "Hierarchy Icon");

				r.x += r.width;
				r.width = buttonWidth;
				if (GUI.Button(r, ShowHierarchyIcon ? "Show" : "Hide", EditorStyles.miniButton)) {
					ShowHierarchyIcon.Value = !ShowHierarchyIcon;
					ShowHierarchyIcon.TrySave();
					EditorApplication.RepaintHierarchyWindow();
				}

				// Show Hightlight Cursor
				r.x = LEFT;
				r.y += r.height + FIELD_GAP_Y;
				r.width = LABEL_WIDTH;
				GUI.Label(r, "Cursor Frame");

				r.x += r.width;
				r.width = buttonWidth;
				if (GUI.Button(r, ShowHightlightCursor.Value ? "Show" : "Hide", EditorStyles.miniButton)) {
					ShowHightlightCursor.Value = !ShowHightlightCursor;
					ShowHightlightCursor.TrySave();
				}

				// Focus On Opening Room
				r.x = LEFT;
				r.y += r.height + FIELD_GAP_Y;
				r.width = LABEL_WIDTH;
				GUI.Label(r, "Switch Room");

				r.x += r.width;
				r.width = buttonWidth;
				if (GUI.Button(r, FocusOnOpeningRoom.Value ? "Focus Room" : "(Do Nothing)", EditorStyles.miniButton)) {
					FocusOnOpeningRoom.Value = !FocusOnOpeningRoom;
					FocusOnOpeningRoom.TrySave();
				}

				// Undoable
				r.x = LEFT;
				r.y += r.height + FIELD_GAP_Y;
				r.width = LABEL_WIDTH;
				GUI.Label(r, "Undoable");

				r.x += r.width;
				r.width = buttonWidth;
				if (GUI.Button(r, OliveUndoable.Value ? "Undoable" : "Not Undoable", EditorStyles.miniButton)) {
					if (OliveUndoable || Dialog("", "It may be slow when painting too many items when undo turned on.", "OK", "Cancel")) {
						OliveUndoable.Value = !OliveUndoable;
						OliveUndoable.TrySave();
						Undo.ClearAll();
					}
				}

				// Gizmos UI
				r.x = LEFT;
				r.y += r.height + FIELD_GAP_Y;
				r.width = LABEL_WIDTH;
				GUI.Label(r, "Gizmos UI");

				r.x += r.width;
				r.width = buttonWidth;
				if (GUI.Button(r, GizmosUI.ToString(), EditorStyles.miniButton)) {
					GizmosUIIndex.Value = (GizmosUIIndex + 1) % GIZMOS_UI_COUNT;
					GizmosUIIndex.TrySave();
				}

				// Show Gizmos Label
				r.x = LEFT;
				r.y += r.height + FIELD_GAP_Y;
				r.width = LABEL_WIDTH;
				GUI.Label(r, "Label");

				r.x += r.width;
				r.width = buttonWidth;
				if (GUI.Button(r, ShowGizmosLabel.Value ? "Show" : "Hide", EditorStyles.miniButton)) {
					ShowGizmosLabel.Value = !ShowGizmosLabel;
					ShowGizmosLabel.TrySave();
				}

				// Show Collider
				r.x = LEFT;
				r.y += r.height + FIELD_GAP_Y;
				r.width = LABEL_WIDTH;
				GUI.Label(r, "Collider");

				r.x += r.width;
				r.width = buttonWidth;
				if (GUI.Button(r, ShowCollider.Value ? "Show" : "Hide", EditorStyles.miniButton)) {
					ShowCollider.Value = !ShowCollider;
					ShowCollider.TrySave();
					EditingMap.ShowAllCollider(ShowCollider);
				}



				// Reset
				r.x = LEFT;
				r.y += r.height + 40f;
				r.width = LABEL_WIDTH + buttonWidth;
				if (GUI.Button(r, "Reset Setting", EditorStyles.miniButton) && Dialog("Warning", "Reset all settings for Olive Map Editor?", "Reset", "Cancel")) {
					DeleteSettings();
					LoadSettings();
				}

			}

			// Cancel
			if (Event.current.type == EventType.MouseDown) {
				GUI.FocusControl("");
				SetRepaintDirty();
			}

			GUILayout.EndArea();
			Handles.EndGUI();
		}


		private static void ViewGUI (SceneView scene) {

			var viewRect = new Rect(PANEL_WIDTH + 6, 0, scene.position.width - PANEL_WIDTH - 6, 64);

			Handles.BeginGUI();
			GUILayout.BeginArea(viewRect);

			var oldE = GUI.enabled;
			var oldC = GUI.color;

			// Tools
			if (!ShowReplaceTileGUI) {
				const float BUTTON_WIDTH = 86;
				var rect = new Rect(36, 24, BUTTON_WIDTH * OLIVE_TOOL_COUNT, 18);
				MouseInGUI.a = rect.Contains(Event.current.mousePosition);
				rect.width = BUTTON_WIDTH;
				for (int i = 0; i < OLIVE_TOOL_COUNT; i++) {
					var tool = (OliveToolType)i;
					var style = i == 0 ? EditorStyles.miniButtonLeft : i == OLIVE_TOOL_COUNT - 1 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;
					if (tool == ToolType) {
						style = new GUIStyle(style) { normal = style.active, };
					}
					if (GUI.Button(rect, !SelectingPaletteItem && tool == OliveToolType.Bucket ? "Magic Wand" : tool.ToString(), style)) {
						if (tool != ToolType) {
							ToolType = tool;
						}
					}
					rect.x += rect.width;
				}

				rect.x += 18;
				Rect migRect = rect;
				if (ShowExTools) {

					// Alt Tool
					rect.width = 96;

					// Layer Selecting Mode
					bool picking = PickingTile();
					bool forceCurrentLayer = SelectingPaletteItem && SelectingPaletteItem != EMPTY_PALETTE_ITEM && !picking;
					var realLayerMode = picking ? SelectionMode.TopLayer : forceCurrentLayer ? SelectionMode.CurrentLayer :
						(PickingTile() || ToolType == OliveToolType.Bucket) && LayerSelectionMode == SelectionMode.AllLayers ? SelectionMode.TopLayer :
						LayerSelectionMode;
					bool layerChanged = realLayerMode != LayerSelectionMode;
					GUI.enabled = !forceCurrentLayer && !picking;
					migRect = rect;
					if (GUI.Button(rect, SELECTION_MODE_LABELS[(int)realLayerMode] + (layerChanged ? "*" : ""), EditorStyles.miniButtonLeft)) {
						SelectionModeIndex.Value = (SelectionModeIndex + 1) % SELECTION_MODE_COUNT;
						SelectionModeIndex.TrySave();
					}

					// Tint
					GUI.enabled = oldE;
					rect.x += rect.width;
					rect.width = rect.height;
					migRect.width += rect.width;
					var tint = ColorField(rect, PaintingTint, "");
					GUI.color = PaintingTint;
					GUI.Box(rect, "▓▓", EditorStyles.miniButtonMid);
					if (tint != PaintingTint) {
						PaintingTint.Value = tint;
						PaintingTint.TrySave();
						SetSelectingPaletteItem(SelectingPaletteIndex, true);
					}

					// Rotation
					GUI.color = oldC;
					GUI.enabled = SelectingPaletteItem != EMPTY_PALETTE_ITEM || !SelectingPaletteItem;
					rect.x += rect.width;
					rect.width = 36;
					migRect.width += rect.width;
					if (GUI.Button(
						rect,
						OLIVE_ROTATION_LABELS[(int)PaintingRotation + (SelectingPaletteItem && SelectingPaletteItem.Type == OliveMap.PaletteItemTyle.AutoTile ? OLIVE_ROTATION_COUNT : 0)],
						EditorStyles.miniButtonMid
					)) {
						SetPaintingRotation((OliveRotationType)((((int)PaintingRotation) + 1) % OLIVE_ROTATION_COUNT));
						SetSelectingPaletteItem(SelectingPaletteIndex, true);
					}

					// Replace Tile
					GUI.color = oldC;
					GUI.enabled = oldE;
					rect.x += rect.width;
					rect.width = 96;
					migRect.width += rect.width;
					if (GUI.Button(rect, "Replace Tile", EditorStyles.miniButtonMid)) {
						ShowReplaceTileGUI = !ShowReplaceTileGUI;
						PickingReplaceTileFrom = false;
						PickingReplaceTileTo = false;
					}

					// Hide Button
					rect.x += rect.width;
					rect.width = 24;
					migRect.width += rect.width;
					if (GUI.Button(rect, "◁", EditorStyles.miniButtonRight)) {
						ShowExTools.Value = false;
						ShowExTools.TrySave();
					}

				} else {
					// Show Button
					rect.width = 24;
					migRect.width += rect.width;
					if (GUI.Button(rect, "▷", EditorStyles.miniButton)) {
						ShowExTools.Value = true;
						ShowExTools.TrySave();
					}
				}

				// Done
				GUI.color = oldC;
				GUI.enabled = oldE;
				rect.x += rect.width + 18;
				rect.width = 62;
				migRect.width += rect.width + 18;
				if (GUI.Button(rect, "Done", EditorStyles.miniButton)) {
					EditMap(null);
				}

				// Mouse In GUI
				MouseInGUI.a = MouseInGUI.a || migRect.Contains(Event.current.mousePosition);
			} else {
				MouseInGUI.a = false;
			}

			GUILayout.EndArea();
			Handles.EndGUI();

			// Replace Tile GUI
			if (ShowReplaceTileGUI) {
				GUI.enabled = true;
				GUI.color = Color.white;
				var viewR = scene.position;
				viewR.x = 0;
				viewR.y = 0;
				Handles.BeginGUI();
				GUILayout.BeginArea(viewR);
				float basicX = viewR.width - 180 - 24;
				var r = new Rect(basicX, 24, 180, 120);

				// Blocker BG
				ColorBlock(viewR, new Color(0, 0, 0, 0.2f));

				// Content
				{
					// BG
					ColorBlock(r, BG_COLOR);

					// Title
					r.height = 18;
					GUI.Label(r, " Replace Tile", new GUIStyle(EditorStyles.toolbar) { alignment = TextAnchor.MiddleLeft });

					// Close Button
					r.x = r.xMax - 18;
					r.width = 18;
					if (GUI.Button(r, "×", EditorStyles.toolbarButton)) {
						ShowReplaceTileGUI = false;
					}

					// From Label
					r.x = basicX + 8;
					r.y += r.height + 6;
					r.width = 62;
					r.height = 18;
					GUI.Label(r, "From:");

					// From Button
					r.x += r.width;
					r.width = 48;
					var guiStyle = EditorStyles.miniButton;
					if (PickingReplaceTileFrom) {
						guiStyle = new GUIStyle(EditorStyles.miniButton) {
							normal = EditorStyles.miniButton.active,
						};
					}
					if (GUI.Button(r, PickingReplaceTileFrom ? "Picking" : "Pick", guiStyle)) {
						PickingReplaceTileFrom = !PickingReplaceTileFrom;
						if (PickingReplaceTileFrom) {
							PickingReplaceTileTo = false;
						}
					}

					// From Thumbnail
					r.x += r.width + 2;
					r.width = r.height;
					if (ReplaceTileFrom) {
						var thumbnail = GetPalettePreview(ReplaceTileFrom, 0);
						if (thumbnail) {
							GUI.DrawTexture(r, thumbnail);
						}
					} else {
						GUI.Box(r, GUIContent.none);
					}


					// To Label
					r.x = basicX + 8;
					r.y += r.height + 6;
					r.width = 62;
					r.height = 18;
					GUI.Label(r, "To:");

					// To Button
					r.x += r.width;
					r.width = 48;
					guiStyle = EditorStyles.miniButton;
					if (PickingReplaceTileTo) {
						guiStyle = new GUIStyle(EditorStyles.miniButton) {
							normal = EditorStyles.miniButton.active,
						};
					}
					if (GUI.Button(r, PickingReplaceTileTo ? "Picking" : "Pick", guiStyle)) {
						PickingReplaceTileTo = !PickingReplaceTileTo;
						if (PickingReplaceTileTo) {
							PickingReplaceTileFrom = false;
						}
					}

					// To Thumbnail
					r.x += r.width + 2;
					r.width = r.height;
					if (ReplaceTileTo) {
						var thumbnail = GetPalettePreview(ReplaceTileTo, 0);
						if (thumbnail) {
							GUI.DrawTexture(r, thumbnail);
						}
					} else {
						GUI.Box(r, GUIContent.none);
					}

					// All Rooms
					r.y += r.height + 6;
					r.x = basicX + 6;
					r.width = 90;
					GUI.enabled = oldE;
					bool newAllRooms = EditorGUI.ToggleLeft(r, " All Rooms", AllRoomForReplaceTile);
					if (newAllRooms != AllRoomForReplaceTile) {
						AllRoomForReplaceTile.Value = newAllRooms;
						AllRoomForReplaceTile.TrySave();
					}

					// All Layers
					r.x += r.width;
					GUI.enabled = !AllRoomForReplaceTile;
					bool newAllLayers = EditorGUI.ToggleLeft(r, " All Layers", AllRoomForReplaceTile || AllLayerForReplaceTile);
					if (!AllRoomForReplaceTile && newAllLayers != AllLayerForReplaceTile) {
						AllLayerForReplaceTile.Value = newAllLayers;
						AllLayerForReplaceTile.TrySave();
					}

					// Replace Button
					r.y += r.height + 6;
					r.x = basicX + 24;
					r.width = 120;
					GUI.enabled = ReplaceTileFrom && !ReplaceTileFrom.IsEmpty && ReplaceTileFrom.AssetType != OliveMap.PaletteAssetType.Color;
					if (GUI.Button(r, "Replace", EditorStyles.miniButton)) {
						TryReplaceTile();
					}

				}

				// Mouse In GUI
				if (!PickingReplaceTileFrom && !PickingReplaceTileTo) {
					MouseInGUI.a = true;
				}

				GUILayout.EndArea();
				Handles.EndGUI();
			}

			GUI.enabled = oldE;
			GUI.color = oldC;
		}


		private static void PreKeyGUI () {
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space) {
				// Space
				if (SelectingPaletteItem) {
					SetSelectingPaletteItem(-1);
				} else {
					PaintingTint.Value = Color.white;
					PaintingTint.TrySave();
				}
				ShowReplaceTileGUI = false;
				PickingReplaceTileFrom = false;
				PickingReplaceTileTo = false;
				ClearSelection();
				Event.current.Use();
				SetRepaintDirty();
			}
		}


		private static void KeyGUI () {

			if (!EditingMap) { return; }
			var e = Event.current;

			// Shift
			if (e.type == EventType.KeyDown && e.shift) {
				switch (e.keyCode) {
					case KeyCode.W:
						SetSelectingLayerIndex(Mathf.Clamp(SelectingLayerIndex - 1, 0, EditingMap.GetLayerCount(OpeningRoomIndex) - 1));
						e.Use();
						SetRepaintDirty();
						break;
					case KeyCode.S:
						SetSelectingLayerIndex(Mathf.Clamp(SelectingLayerIndex + 1, 0, EditingMap.GetLayerCount(OpeningRoomIndex) - 1));
						e.Use();
						SetRepaintDirty();
						break;
				}
			}

			// Ctrl
			if (e.type == EventType.KeyDown && e.control) {
				switch (e.keyCode) {
					case KeyCode.A: {
						SelectAllLogic();
						e.Use();
						SetRepaintDirty();
					}
					break;
					case KeyCode.X:
					case KeyCode.C:
						bool hasItem = CopySelectingItems(e.keyCode == KeyCode.X);
						if (hasItem) {
							e.Use();
							SetRepaintDirty();
						}
						break;
					case KeyCode.V:
						bool pasted = PasteFromCopyedData();
						if (pasted) {
							ClearRoomMinMax();
							SetAutoTileDirty(false, SelectionMode.CurrentLayer);
							Selection.objects = SelectionCacheList.ToArray();
							e.Use();
							SetRepaintDirty();
						}
						break;
				}
			}

			// None
			if (e.type == EventType.KeyDown && !e.control && !e.shift && !e.alt) {

				switch (e.keyCode) {
					// WASD
					case KeyCode.Minus:
					case KeyCode.A: {
						int count = EditingMap.Palette.Count;
						int index = SelectingPaletteIndex;
						index = index == -2 ? count - 1 :
							index == -1 ? -2 :
							index > 0 ? index - 1 : -2;
						SetSelectingPaletteItem(index);
						e.Use();
						SetRepaintDirty();
						break;
					}
					case KeyCode.Equals:
					case KeyCode.D: {
						int count = EditingMap.Palette.Count;
						int index = SelectingPaletteIndex;
						index = index == -2 ? 0 :
							index == -1 ? 0 :
							index < count - 1 ? index + 1 : -2;
						SetSelectingPaletteItem(index);

						e.Use();
						SetRepaintDirty();
						break;
					}
					case KeyCode.W: {
						int count = EditingMap.Palette.Count;
						int index = SelectingPaletteIndex;
						int row = PaletteSizeIndex == 0 ? 9 : PaletteSizeIndex == 1 ? 5 : 3;
						index = index == -1 ? -2 :
							index == -2 ? count - row :
							index >= row ? index - row : index;
						SetSelectingPaletteItem(index);
						e.Use();
						SetRepaintDirty();
						break;
					}
					case KeyCode.S: {
						int count = EditingMap.Palette.Count;
						int index = SelectingPaletteIndex;
						int row = PaletteSizeIndex == 0 ? 9 : PaletteSizeIndex == 1 ? 5 : 3;
						index = index == -1 ? 0 :
							index == -2 ? index :
							index == count - row ? -2 :
							index < count - row ? index + row : index;
						SetSelectingPaletteItem(index);
						e.Use();
						SetRepaintDirty();
						break;
					}
					// ZXEQ
					case KeyCode.Z:
						OliveInspector = (OliveInspectorType)Mathf.Clamp(
							(int)OliveInspector - 1, 0, OLIVE_INSPECTOR_COUNT
						);
						DetailScrollValue = 0f;
						e.Use();
						SetRepaintDirty();
						break;
					case KeyCode.X:
						OliveInspector = (OliveInspectorType)Mathf.Clamp(
							(int)OliveInspector + 1, 0, OLIVE_INSPECTOR_COUNT - 1
						);
						DetailScrollValue = 0f;
						e.Use();
						SetRepaintDirty();
						break;
					case KeyCode.R:
						SetSelectingPaletteItem(-2);
						e.Use();
						SetRepaintDirty();
						break;

					// Tool
					case KeyCode.U:
					case KeyCode.Alpha1:
						ToolType = OliveToolType.Rect;
						e.Use();
						SetRepaintDirty();
						break;
					case KeyCode.L:
					case KeyCode.Alpha2:
						ToolType = OliveToolType.Line;
						e.Use();
						SetRepaintDirty();
						break;
					case KeyCode.G:
					case KeyCode.Alpha3:
						ToolType = OliveToolType.Bucket;
						e.Use();
						SetRepaintDirty();
						break;

					// Rotation
					case KeyCode.Q:
						SetPaintingRotation((OliveRotationType)((((int)PaintingRotation) + 1) % OLIVE_ROTATION_COUNT));
						e.Use();
						SetRepaintDirty();
						break;
					case KeyCode.E:
						SetPaintingRotation((OliveRotationType)((int)PaintingRotation == 0 ? OLIVE_ROTATION_COUNT - 1 : (int)PaintingRotation - 1));
						e.Use();
						SetRepaintDirty();
						break;
					// Misc
					case KeyCode.Escape:
						if (ShowReplaceTileGUI) {
							ShowReplaceTileGUI = false;
						} else {
							EditMap(null);
						}
						e.Use();
						SetRepaintDirty();
						break;
					case KeyCode.Backspace:
					case KeyCode.Delete:
						bool hasItem = ForSelectingItems((itemTF) => {
							DestroyObject(itemTF.gameObject);
						});
						if (hasItem) {
							SetAutoTileDirty(false, LayerSelectionMode);
							ClearRoomMinMax();
							e.Use();
							SetRepaintDirty();
						}
						break;
				}
			}

		}


		private static void CleanGUI () {
			if (!EditingMap || Event.current.type != EventType.Repaint) { return; }
			// Room Layer Position
			if (NeedFixAllRoomLayerItem) {
				EditingMap.FixAllRoomLayerItem(OpeningRoomIndex, (tf) => {
					RegistObjUndo(tf, "Fix Stuff");
				});
				SetSelectingPaletteItem(SelectingPaletteIndex, true);
				NeedFixAllRoomLayerItem = false;
				SetRepaintDirty();
			}
			// Overlap
			if (NeedDeleteOverlap) {
				DeleteAllOverlap(OpeningRoomIndex, SelectingLayerIndex);
				NeedDeleteOverlap = false;
				SetRepaintDirty();
			}
			// AutoTile
			if (NeedFixAutoTile != -1) {
				FixAllAutoTileItems();
				NeedFixAutoTile = -1;
				SetRepaintDirty();
			}
			// Mouse In GUI
			if (MouseInGUI.a != MouseInGUI.b) {
				MouseInGUI.b = MouseInGUI.a;
				SetRepaintDirty();
			}
		}


		#endregion

		#region --- API ---


		public static void EditMap (OliveMap map) {

			// Stop
			if (EditingMap) {
				EditingMap.Fix();
				NeedFixAutoTile = -3;
				FixAllAutoTileItems();
				LinkPrefab(EditingMap, PrevLinkedPrefab);
			}
			PrevLinkedPrefab = null;

			// Open New
			PrevLinkedPrefab = UnpackMap(map);
			EditingMap = map;

			// Start
			if (EditingMap) {
				bool isPrefab = !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(EditingMap.gameObject));
				if (!isPrefab) {
					RespawnEditingStuff();
					if (SceneView.lastActiveSceneView) {
						SceneView.lastActiveSceneView.Focus();
					}
					Selection.activeTransform = EditingMap.transform;
				} else {
					Debug.Log("[Olive] The prefab is not able to be unpacked.");
				}
				EditingMap.ShowAllCollider(ShowCollider);
				NeedFixAutoTile = -3;
				FixAllAutoTileItems();
				AddDefaultRoomLayerIfEmpty();
			}

			// Hide All Roots
			RefreshAllSceneMaps(map);
			foreach (var m in AllMapsInScene) {
				if (!m) { continue; }
				m.HideRoot();
			}

			// Done
			ShowReplaceTileGUI = false;
			ReplaceTileFrom = null;
			ReplaceTileTo = null;
			ClearSelection();
			SortPaletteItems();
			ClearRoomMinMax();
			PalettePreviewMap.Clear();
			SetSelectingPaletteItem(-1);
			SetAutoTileDirty(false, SelectionMode.AllLayers, true);
			SetInspectorExpandedForBoxCollider2D(!EditingMap);
			FocusCurrentRoom();
			UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
		}


		public static Texture2D GetPalettePreview (OliveMap.PaletteItem palItem, int detailIndex = 0) {
			if (!palItem || palItem.IsEmpty) {
				return null;
			}
			var item = new IndexedPaletteItem(palItem, detailIndex);
			if (PalettePreviewMap.ContainsKey(item)) {
				return PalettePreviewMap[item];
			} else {
				OliveMap.PaletteItem.ItemData itemData = null;
				if (item.detailIndex < palItem.ItemCount) {
					itemData = palItem.Items[item.detailIndex];
				}
				switch (palItem.AssetType) {
					case OliveMap.PaletteAssetType.Prefab: {
						var prefab = itemData?.Prefab;
						if (!prefab) { break; }
						var preview = AssetPreview.GetAssetPreview(prefab);
						if (preview) {
							PalettePreviewMap.Add(item, preview);
						}
						return preview;
					}
					case OliveMap.PaletteAssetType.Sprite: {
						var sp = itemData?.Sprite;
						if (sp && sp.texture) {
							try {
								int spWidth = (int)sp.rect.width;
								int spHeight = (int)sp.rect.height;
								int spX = (int)sp.rect.x;
								int spY = (int)sp.rect.y;
								var preview = new Texture2D(spWidth, spHeight) {
									filterMode = FilterMode.Point
								};
								TryMakeTextureReadable(sp.texture);
								preview.SetPixels(sp.texture.GetPixels(spX, spY, spWidth, spHeight));
								preview.Apply();
								PalettePreviewMap.Add(item, preview);
								return preview;
							} catch { }
						}
						break;
					}
					case OliveMap.PaletteAssetType.Color:
						break;
				}
			}
			return null;
		}


		public static GameObject UnpackMap (OliveMap map) {
			if (!map) { return null; }
			bool missingPrefab = PrefabUtility.GetPrefabAssetType(map.gameObject) == PrefabAssetType.MissingAsset;
			var prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(map.gameObject);
			if (prefab) {
				PrefabUtility.UnpackPrefabInstance(map.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
			}
			if (missingPrefab) {
				PrefabUtility.UnpackPrefabInstance(map.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
			}
			return prefab;
		}


		public static void LinkPrefab (OliveMap map, GameObject linkedPrefab) {
			if (linkedPrefab && map) {
				try {
					PrefabUtility.SaveAsPrefabAssetAndConnect(
					   map.gameObject,
					   AssetDatabase.GetAssetPath(linkedPrefab),
					   InteractionMode.AutomatedAction
					);
				} catch { }
			}
		}


		#endregion

	}
}