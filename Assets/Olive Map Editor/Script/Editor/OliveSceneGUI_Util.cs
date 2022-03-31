namespace OliveMapEditor {
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using UnityEditor;


	// === Util ===
	public partial class OliveSceneGUI {


		public const string ROOT_NAME = "Olive Map Editor";


		public static string GetOliveRootPath (MonoBehaviour mono) {
			var rootPath = "";
			var script = MonoScript.FromMonoBehaviour(mono);
			if (script) {
				var path = AssetDatabase.GetAssetPath(script);
				if (!string.IsNullOrEmpty(path)) {
					int index = path.LastIndexOf(ROOT_NAME);
					if (index >= 0) {
						rootPath = path.Substring(0, index + ROOT_NAME.Length);
					}
				}
			}
			return rootPath;
		}


		public static string CombinePaths (params string[] paths) {
			string path = "";
			for (int i = 0; i < paths.Length; i++) {
				path = Path.Combine(path, paths[i]);
			}
			return path;
		}


		public static bool Dialog (string title, string msg, string ok, string cancel = "") {
			bool sure;
			if (string.IsNullOrEmpty(cancel)) {
				sure = EditorUtility.DisplayDialog(title, msg, ok);
			} else {
				sure = EditorUtility.DisplayDialog(title, msg, ok, cancel);
			}
			var view = SceneView.currentDrawingSceneView;
			if (view) {
				view.Focus();
			}
			return sure;
		}


		// GUI
		private static string GetDisplayString (string str, int maxLength) {
			return str.Length > maxLength ? str.Substring(0, maxLength - 3) + "..." : str;
		}


		private static bool ColorfulButton (Rect rect, string label, Color color, GUIStyle style = null) {
			Color oldColor = GUI.color;
			GUI.color = color;
			bool pressed = style == null ? GUI.Button(rect, label) : GUI.Button(rect, label, style);
			GUI.color = oldColor;
			return pressed;
		}


		private static void ColorBlock (Rect rect) {
			ColorBlock(rect, new Color(1, 1, 1, 0.1f));
		}


		private static void ColorBlock (Rect rect, Color color) {
			var oldC = GUI.color;
			GUI.color = color;
			GUI.DrawTexture(rect, Texture2D.whiteTexture);
			GUI.color = oldC;
		}


		private static bool StringField (Rect rect, ref string valueStr) {
			bool changed = false;
			var newValue = GUI.TextField(rect, valueStr);
			if (newValue != valueStr) {
				valueStr = newValue;
				changed = true;
			}
			return changed;
		}


		private static bool FloatField (Rect rect, ref float value) {
			bool changed = false;
			var newValue = EditorGUI.DelayedFloatField(rect, value);
			if (value != newValue) {
				value = newValue;
				changed = true;
			}
			return changed;
		}


		private static bool FloatFieldWithButtons (Rect r, ref float value, string label, float labelWidth, float step = 0.1f) {

			bool changed = false;
			float width = r.width;
			const float BUTTON_WIDTH = 20;
			r.width = labelWidth;
			GUI.Label(r, label);

			r.x += r.width;
			r.width = width - labelWidth - BUTTON_WIDTH * 2;
			if (FloatField(r, ref value)) {
				changed = true;
			}

			r.x += r.width;
			r.width = BUTTON_WIDTH;
			r.height -= 1;
			r.y += 1;
			if (GUI.Button(r, "▼", EditorStyles.miniButtonMid)) {
				value = Mathf.Round((value - step) * 10f) / 10f;
				changed = true;
			}
			r.x += BUTTON_WIDTH;
			if (GUI.Button(r, "▲", EditorStyles.miniButtonRight)) {
				value = Mathf.Round((value + step) * 10f) / 10f;
				changed = true;
			}
			r.y -= 1;
			r.height += 1;

			return changed;
		}


		private static Color ColorField (Rect r, Color value, string label) {
			r.width -= r.height;
			GUI.Label(r, label);
			r.x = r.xMax;
			r.width = r.height;
#if UNITY_2017 || UNITY_5 || UNITY_4
			return EditorGUI.ColorField(r, GUIContent.none, value, false, true, false, null);
#else
			return EditorGUI.ColorField(r, GUIContent.none, value, false, true, false);
#endif
		}


		private static void SetInspectorExpandedForBoxCollider2D (bool expend) {
			var g = new GameObject("", typeof(BoxCollider2D)) {
				hideFlags = HideFlags.HideAndDontSave
			};
			try {
				UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(g.GetComponent<BoxCollider2D>(), expend);
			} catch { }
			Object.DestroyImmediate(g);
		}


		private static void FocusCurrentRoom () {
			EditorApplication.delayCall += () => {
				if (!EditingMap) { return; }
				var roomTF = EditingMap.GetRoomTF(OpeningRoomIndex);
				if (!roomTF) { return; }
				// Focus
				var oldFlag = roomTF.gameObject.hideFlags;
				roomTF.gameObject.hideFlags = HideFlags.None;
				var oldSelect = Selection.activeObject;
				Selection.activeObject = roomTF.gameObject;
				if (SceneView.lastActiveSceneView) {
					SceneView.lastActiveSceneView.FrameSelected(false);
				}
				roomTF.gameObject.hideFlags = oldFlag;
				Selection.activeObject = oldSelect;
			};
		}


		private static void TryMakeTextureReadable (Texture2D tex) {
			TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
			if (ti && !ti.isReadable) {
				ti.isReadable = true;
				AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(tex));
			}
		}


		private static bool PickingTile () {
			if (PickingReplaceTileFrom || PickingReplaceTileTo) {
				return true;
			}
			switch (ColorPickingMode) {
				case OliveColorPickingMode.Alt_Key:
					return Event.current.alt;
				case OliveColorPickingMode.Mouse_Right:
					return Event.current.button == 1;
			}
			return false;
		}


		private static void ColorPaletteItemThumbnailGUI (Rect r, OliveMap.PaletteItem item, int maxColor = 24) {
			if (!item) { return; }
			r.width /= Mathf.Clamp(item.ItemCount, 1f, maxColor);
			for (int i = 0; i < item.ItemCount && i < maxColor; i++) {
				var colorObj = item.GetItemAt(i);
				if (colorObj != null) {
					ColorBlock(r, (Color)colorObj);
					r.x += r.width;
				}
			}
		}


	}
}