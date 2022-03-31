namespace OliveMapEditor {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;
	using Saving;



	[CustomEditor(typeof(OliveMap))]
	[DisallowMultipleComponent]
	public class OliveMapInspector : Editor {

		// Const
		private const string DEFAULT_SPRITE_SHADER = "Sprites/Default";

		// Short
		private static Shader SpriteShader {
			get {
				if (_SpriteShader == null) {
					_SpriteShader = Shader.Find(SpriteShaderPath);
                }
				return _SpriteShader;
			}

			set {
				if (_SpriteShader != value) {
					_SpriteShader = value;
					SpriteShaderPath.Value = value ? value.name : "";
					SpriteShaderPath.TrySave();
				}
			}
		}

		// Data
		private bool NotPrefab = false;
		private static Shader _SpriteShader = null;
		private static Texture2D OliveIcon = null;

		// Saving
		private readonly static EditorSavingString SpriteShaderPath = new EditorSavingString("OliveMapInspector.SpriteShader", DEFAULT_SPRITE_SHADER);

		#region --- MSG ---

		private void OnEnable () {
			var map = target as OliveMap;
			NotPrefab = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(map.gameObject));
			for (int i = 0; i < map.Palette.Count; i++) {
				OliveSceneGUI.GetPalettePreview(map.Palette[i], 0);
			}
			SpriteShaderPath.Load();
			// Icon
			if (OliveIcon == null) {
				try {
					OliveIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(CombinePaths(GetOliveRootPath(map), "Image", "Olive Icon.psd"));
				} catch { }
			}
		}


		public override void OnInspectorGUI () {
			if (NotPrefab) {
				Space(4);
				var targetMap = target as OliveMap;
				bool editingThisMap = OliveSceneGUI.EditingMap && OliveSceneGUI.EditingMap == targetMap;

				// Buttons
				Space(6);
				LayoutH(() => {
					GUIRect(0, 22);
					var buttonRect = GUIRect(82, 20);
					// Edit
					buttonRect.x = EditorGUIUtility.currentViewWidth - buttonRect.width;
					bool newEditing = GUI.Toggle(buttonRect, editingThisMap, "Edit      ", EditorStyles.miniButtonMid);
					if (newEditing != editingThisMap) {
						OliveSceneGUI.EditMap(newEditing ? targetMap : null);
					}
					// Icon
					if (OliveIcon) {
						GUI.DrawTexture(new Rect(buttonRect.xMax - 18 - 6, buttonRect.y + 2, 18, 18), OliveIcon);
					}
					// Export
					var oldE = GUI.enabled;
					GUI.enabled = !editingThisMap;
					buttonRect.width = 82;
					buttonRect.x -= buttonRect.width;
					if (GUI.Button(buttonRect, "Export", EditorStyles.miniButtonLeft)) {
						var path = EditorUtility.SaveFilePanelInProject("Export Map", targetMap.name, "prefab", "Export map to a prefab.");
						if (!string.IsNullOrEmpty(path)) {
							// Get Old Prefab
							Vector3 oldPos = Vector3.zero;
							GameObject newMapPrefab = null;
							try {
								newMapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
								if (newMapPrefab) {
									// Delete Objs in Old Prefab
									var objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
									foreach (var obj in objs) {
										DestroyImmediate(obj, true);
									}
								}
							} catch { }
							// Create Map
							List<Object> resources;
							var newMap = OliveOptimizer.CreateOptimizedMap(targetMap, SpriteShader ?? Shader.Find(DEFAULT_SPRITE_SHADER), out resources);
							// Prefab
							if (newMap) {
								var tempGameObject = new GameObject("[Olive] Temp");
								try {
									bool success = true;
									if (!newMapPrefab) {
										newMapPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(tempGameObject, path, InteractionMode.AutomatedAction, out success);
									}
									if (success && newMapPrefab) {
										// Resources
										if (resources != null) {
											foreach (var obj in resources) {
												AssetDatabase.AddObjectToAsset(obj, newMapPrefab);
											}
										}
										// Link
										newMapPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(newMap.gameObject, path, InteractionMode.AutomatedAction, out success);
										if (!success) {
											Debug.LogWarning("[Olive] Fail to create map prefab.");
										}
										// End
										AssetDatabase.SaveAssets();
										AssetDatabase.Refresh();
										Selection.activeObject = newMapPrefab;
										EditorGUIUtility.PingObject(newMapPrefab);
									}
								} catch { Debug.LogWarning("[Olive] Fail to create prefab for map."); }
								DestroyImmediate(tempGameObject, false);
								DestroyImmediate(newMap.gameObject, false);
							} else {
								Debug.LogWarning("[Olive] Fail to create optimized map.");
							}
						}
					}
					GUI.enabled = oldE;
				});
				Space(6);

				if (!editingThisMap) {

					// Palette
					{
						const float PALETTE_SIZE = 18;
						var palRect = GUIRect(0, PALETTE_SIZE);
						int maxCount = (int)((palRect.width - PALETTE_SIZE) / PALETTE_SIZE);
						for (int i = 0; i < maxCount; i++) {
							var rect = new Rect(
								 palRect.x + i * PALETTE_SIZE + 1,
								 palRect.y + 1,
								 PALETTE_SIZE - 2,
								 PALETTE_SIZE - 2
							);
							var pal = i < targetMap.Palette.Count ? targetMap.Palette[i] : null;
							var texture = OliveSceneGUI.GetPalettePreview(pal, 0);
							GUI.Box(rect, GUIContent.none);
							if (texture) {
								GUI.DrawTexture(rect, texture);
							}
						}
						if (targetMap.Palette.Count > maxCount) {
							GUI.Label(
								new Rect(palRect) {
									x = palRect.xMax - PALETTE_SIZE - 4,
									width = PALETTE_SIZE,
								},
								"+" + (targetMap.Palette.Count - maxCount),
								EditorStyles.centeredGreyMiniLabel
							);
						}
						Space(4);
					}

					// Info
					{
						LayoutH(() => {
							var range = targetMap.GetMapRange();
							GUI.Label(GUIRect(0, 18), string.Format(
								"Size: {0}×{1}",
								Mathf.RoundToInt(range.z - range.x),
								Mathf.RoundToInt(range.w - range.y)
							));
							GUI.Label(GUIRect(0, 18), string.Format(
								"Grid Size: {0}",
								targetMap.GridSize.ToString("0.0")
							));
						});
						Space(4);

					}

					// Setting
					{
						SpriteShader = (Shader)EditorGUI.ObjectField(GUIRect(0, 16), new GUIContent("Sprite Shader"), SpriteShader, typeof(Shader), false);


					}

				}
			}
		}


		#endregion

		#region --- UTL ---


		public Rect GUIRect (float width, float height) {
			return GUILayoutUtility.GetRect(
				width, height,
				GUILayout.ExpandWidth(width == 0), GUILayout.ExpandHeight(height == 0)
			);
		}


		public void Space (float space = 4f) {
			GUILayout.Space(space);
		}


		public void LayoutH (System.Action action, bool box = false, GUIStyle style = null) {
			if (box) {
				style = new GUIStyle(GUI.skin.box) {
					padding = new RectOffset(6, 6, 2, 2)
				};
			}
			if (style != null) {
				GUILayout.BeginHorizontal(style);
			} else {
				GUILayout.BeginHorizontal();
			}
			action();
			GUILayout.EndHorizontal();
		}


		public void LayoutV (System.Action action, bool box = false, GUIStyle style = null) {
			if (box) {
				style = new GUIStyle(GUI.skin.box) {
					padding = new RectOffset(6, 6, 2, 2)
				};
			}
			if (style != null) {
				GUILayout.BeginVertical(style);
			} else {
				GUILayout.BeginVertical();
			}
			action();
			GUILayout.EndVertical();
		}

		public string GetDisplayString (string str, int maxLength) {
			return str.Length > maxLength ? str.Substring(0, maxLength - 3) + "..." : str;
		}

		public static string GetOliveRootPath (MonoBehaviour mono) {
			string rootPath = "";
			MonoScript script = MonoScript.FromMonoBehaviour(mono);
			if (script) {
				var path = AssetDatabase.GetAssetPath(script);
				if (!string.IsNullOrEmpty(path)) {
					int index = path.LastIndexOf(OliveSceneGUI.ROOT_NAME);
					if (index >= 0) {
						rootPath = path.Substring(0, index + OliveSceneGUI.ROOT_NAME.Length);
					}
				}
			}
			return rootPath;
		}


		public static string CombinePaths (params string[] paths) {
			string path = "";
			for (int i = 0; i < paths.Length; i++) {
				path = System.IO.Path.Combine(path, paths[i]);
			}
			return path;
		}


		#endregion

	}
}