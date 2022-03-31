namespace OliveMapEditor {
	using System.Collections.Generic;

	// === SUB ===
	public partial class OliveSceneGUI {

		private const int OLIVE_INSPECTOR_COUNT = 3;
		public enum OliveInspectorType {
			Map = 0,
			Detail = 1,
			Setting = 2
		}

		private const int OLIVE_COLOR_PICKING_COUNT = 2;
		private static readonly string[] COLOR_PICKER_LABELS = new string[OLIVE_COLOR_PICKING_COUNT] {
			"Alt Key",
			"Mouse Right",
		};
		public enum OliveColorPickingMode {
			Alt_Key = 0,
			Mouse_Right = 1,
		}

		private const int SELECTION_MODE_COUNT = 3;
		private static readonly string[] SELECTION_MODE_LABELS = new string[SELECTION_MODE_COUNT] {
			"Current Layer",
			"Top Layer",
			"All Layers",
		};
		public enum SelectionMode {
			CurrentLayer = 0,
			TopLayer = 1,
			AllLayers = 2,
		}

		private const int OLIVE_TOOL_COUNT = 3;
		public enum OliveToolType {
			Rect = 0,
			Line = 1,
			Bucket = 2,
		}

		private const int GIZMOS_UI_COUNT = 3;
		private static readonly float[] GIZMOS_UI_WIDTH_MUTI = new float[GIZMOS_UI_COUNT] {
			0f, 0.25f, 0.5f,
		};
		public enum GizmosUIType {
			Normal = 0,
			Bold = 1,
			Extra_Bold = 2,
		}

		public const int OLIVE_ROTATION_COUNT = 4;
		public readonly static string[] OLIVE_ROTATION_LABELS = new string[OLIVE_ROTATION_COUNT * 2] {
			//"↑", "←", "↓", "→",
			//"⇧", "⇦", "⇩", "⇨",
			//"▲", "◀", "▼", "▶",
			"(U)", "(L)", "(D)", "(R)",
			"(U)", "(L)", "(D)", "(R)",
		};

		public class PaletteSorter : IComparer<OliveMap.PaletteItem> {

			public OliveMap.OlivePaletteSortMode Mode = OliveMap.OlivePaletteSortMode.Unsorted;
			public int Compare (OliveMap.PaletteItem x, OliveMap.PaletteItem y) {
				if (x == null || y == null) { 
					return 0; 
				}
				switch (Mode) {
					default:
						return 0;
					case OliveMap.OlivePaletteSortMode.Type:
						int id = ((int)x.Type).CompareTo((int)y.Type);
						if (id == 0) { 
							id = x.AssetType.CompareTo(y.AssetType);
						}
						return id == 0 ? x.ID.CompareTo(y.ID) : id;
					case OliveMap.OlivePaletteSortMode.CreateTime:
						return x.ID.CompareTo(y.ID);
				}
			}
		}

		public class MapSorter : IComparer<OliveMap> {
			public int Compare (OliveMap x, OliveMap y) {
				return x.name.CompareTo(y.name);
			}
		}

		public class ToggleGridUtility {

			private static System.Type m_annotationUtility;
			private static System.Reflection.PropertyInfo m_showGridProperty;

			private static System.Reflection.PropertyInfo ShowGridProperty {
				get {
					try {
						if (m_showGridProperty == null) {
							m_annotationUtility = System.Type.GetType("UnityEditor.AnnotationUtility,UnityEditor.dll");
							if (m_annotationUtility != null) {
								m_showGridProperty = m_annotationUtility.GetProperty("showGrid", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
							}
						}
						return m_showGridProperty;
					} catch { return null; }
				}
			}


			public static bool ShowGrid {
				get {
					try {
						if (ShowGridProperty == null) { return false; }
                        object obj = ShowGridProperty.GetValue(null, null);
						return obj != null && (bool)obj;
					} catch { return false; }
				}
				set {
					try {
						if (ShowGridProperty != null) {
							ShowGridProperty.SetValue(null, value, null);
						}
					} catch { }
				}
			}


		}



	}
}