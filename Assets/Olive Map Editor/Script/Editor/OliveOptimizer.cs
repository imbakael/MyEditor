namespace OliveMapEditor {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	public static class OliveOptimizer {

		private class MeshStreamData {
			public class SubMeshData {
				public List<int> MeshTriangles;
				public Material Mat;
			}
			public List<Color> MeshColors;
			public List<Vector3> MeshVertices;
			public List<Vector2> MeshUVs;
			public Dictionary<Texture2D, SubMeshData> SubMeshMap;
			public MeshRenderer Renderer;
		}


		private struct SpriteTextureData {
			public Transform LayerTF;
			public Sprite Sprite;
			public Vector3 LocalPosition;
			public int LocalRotation;
			public Color Tint;
			public Mesh Mesh;
		}

		private const float COLLIDER_SIZE = 1.0001f;

		public static Transform CreateOptimizedMap (OliveMap map, Shader spriteShader, out List<Object> resources) {
			// Check
			resources = new List<Object>();
			if (!map || !map.MapRoot) { return null; }
			// Map
			var textureMatMap = new Dictionary<Texture2D, Material>();
			var root = new GameObject(map.name).transform;
			root.position = Vector3.zero;
			root.rotation = Quaternion.identity;
			root.localScale = Vector3.one;
			var itemDataList = new List<SpriteTextureData>();
			var meshStreamMap = new Dictionary<Mesh, MeshStreamData>();
			float gridSize = map.GridSize;
			// Collider
			var colliderRoot = new GameObject("Temp Collider Root").transform;
			colliderRoot.SetParent(root);
			colliderRoot.localPosition = Vector3.zero;
			colliderRoot.localRotation = Quaternion.identity;
			colliderRoot.localScale = Vector3.one;
			var pShapeListCache = new List<Vector2>();
			// Rooms
			int roomCount = map.RoomCount;
			for (int roomIndex = 0; roomIndex < roomCount; roomIndex++) {
				// <Room Loop>
				var sourceRoomTF = map.GetRoomTF(roomIndex);
				if (!sourceRoomTF) { continue; }
				var roomTF = new GameObject(sourceRoomTF.name).transform;
				roomTF.SetParent(root);
				roomTF.localPosition = sourceRoomTF.localPosition;
				roomTF.localRotation = sourceRoomTF.localRotation;
				roomTF.localScale = sourceRoomTF.localScale;
				roomTF.gameObject.SetActive(sourceRoomTF.gameObject.activeSelf);
				// Collider
				var roomColliderHolder = new GameObject().transform;
				roomColliderHolder.SetParent(colliderRoot);
				roomColliderHolder.localPosition = sourceRoomTF.localPosition;
				roomColliderHolder.localRotation = sourceRoomTF.localRotation;
				roomColliderHolder.localScale = sourceRoomTF.localScale;
				// Layers
				int layerCount = map.GetLayerCount(roomIndex);
				for (int layerIndex = 0; layerIndex < layerCount; layerIndex++) {
					// <Layer Loop>
					var sourceLayerTF = map.GetLayerTF(sourceRoomTF, layerIndex);
					if (!sourceLayerTF) { continue; }
					var layerTF = new GameObject(sourceLayerTF.name, typeof(MeshFilter), typeof(MeshRenderer)).transform;
					layerTF.SetParent(roomTF);
					layerTF.localPosition = sourceLayerTF.localPosition;
					layerTF.localRotation = sourceLayerTF.localRotation;
					layerTF.localScale = sourceLayerTF.localScale;
					layerTF.gameObject.SetActive(sourceLayerTF.gameObject.activeSelf);
					// Collider
					var layerColliderHolder = new GameObject().transform;
					layerColliderHolder.SetParent(roomColliderHolder);
					layerColliderHolder.localPosition = sourceLayerTF.localPosition;
					layerColliderHolder.localRotation = sourceLayerTF.localRotation;
					layerColliderHolder.localScale = sourceLayerTF.localScale;
					// Mesh Renderer
					var mr = layerTF.GetComponent<MeshRenderer>();
					mr.receiveShadows = false;
					mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
					mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
					// Mesh Filter
					var mf = layerTF.GetComponent<MeshFilter>();
					mf.sharedMesh = new Mesh() { name = roomTF.name + "-" + layerTF.name, };
					if (!meshStreamMap.ContainsKey(mf.sharedMesh)) {
						meshStreamMap.Add(mf.sharedMesh, new MeshStreamData() {
							Renderer = mr,
							SubMeshMap = new Dictionary<Texture2D, MeshStreamData.SubMeshData>(),
							MeshColors = new List<Color>(),
							MeshVertices = new List<Vector3>(),
							MeshUVs = new List<Vector2>(),
						});
					}
					resources.Add(mf.sharedMesh);
					// Items
					int itemCount = sourceLayerTF.childCount;
					int prefabCount = 0;
					for (int i = 0; i < itemCount; i++) {
						// <Item Loop>
						var sourceItemTF = sourceLayerTF.GetChild(i);
						var assetType = OliveMap.GetItemAssetType(sourceItemTF);
						switch (assetType) {
							case OliveMap.PaletteAssetType.Prefab:
								// Prefab
								var sourcePrefab = sourceItemTF.childCount > 0 ? sourceItemTF.GetChild(0).gameObject : null;
								if (!sourcePrefab) { break; }
								var itemTF = Object.Instantiate(sourcePrefab, layerTF).transform;
								itemTF.name = "Prefab_" + prefabCount;
								itemTF.localPosition = sourceItemTF.localPosition;
								itemTF.localRotation = sourceItemTF.localRotation;
								var scl = sourceItemTF.localScale;
								scl.Scale(sourcePrefab.transform.localScale);
								itemTF.localScale = scl;
								prefabCount++;
								break;
							case OliveMap.PaletteAssetType.Sprite:
								// Sprite
								// Render
								if (sourceItemTF.childCount == 0) { break; }
								var sr = sourceItemTF.GetChild(0).GetComponent<SpriteRenderer>();
								if (!sr) { break; }
								var sp = sr.sprite;
								if (!sp || !sp.texture) { break; }
								itemDataList.Add(new SpriteTextureData() {
									Sprite = sp,
									LayerTF = layerTF,
									LocalPosition = sourceItemTF.localPosition,
									LocalRotation = Mathf.RoundToInt(Mathf.Repeat(sourceItemTF.localRotation.eulerAngles.z, 360f) / 90f),
									Mesh = mf.sharedMesh,
									Tint = sr.color,
								});
								// Mesh Stream Map Texture
								if (meshStreamMap.ContainsKey(mf.sharedMesh)) {
									var steam = meshStreamMap[mf.sharedMesh];
									if (steam != null) {
										if (!steam.SubMeshMap.ContainsKey(sp.texture)) {
											Material mat;
											if (!textureMatMap.ContainsKey(sp.texture)) {
												mat = new Material(spriteShader) {
													name = sp.texture.name,
													mainTexture = sp.texture,
												};
												resources.Add(mat);
												textureMatMap.Add(sp.texture, mat);
											} else {
												mat = textureMatMap[sp.texture];
											}
											steam.SubMeshMap.Add(sp.texture, new MeshStreamData.SubMeshData() {
												MeshTriangles = new List<int>(),
												Mat = mat,
											});
										}
									}
								}
								// Collider
								var colliderType = OliveMap.GetItemColliderType(sourceItemTF);
								if (colliderType != OliveMap.OliveColliderType.NoCollider) {
									var spriteCollider = new GameObject().transform;
									spriteCollider.SetParent(layerColliderHolder);
									spriteCollider.localPosition = sourceItemTF.localPosition;
									spriteCollider.localRotation = sourceItemTF.localRotation;
									spriteCollider.localScale = Vector3.one * gridSize * COLLIDER_SIZE;
									switch (colliderType) {
										case OliveMap.OliveColliderType.BoxCollider:
											var bCol = spriteCollider.gameObject.AddComponent<BoxCollider2D>();
											bCol.size = Vector2.one;
											bCol.offset = Vector2.zero;
											bCol.usedByComposite = true;
											break;
										case OliveMap.OliveColliderType.PhysicsShapeCollider:
											int pCount = sp.GetPhysicsShapeCount();
											if (pCount == 0) {
												var col = spriteCollider.gameObject.AddComponent<BoxCollider2D>();
												col.size = Vector2.one;
												col.offset = Vector2.zero;
												col.usedByComposite = true;
											} else {
												var pCol = spriteCollider.gameObject.AddComponent<PolygonCollider2D>();
												pCol.offset = Vector2.zero;
												pCol.usedByComposite = true;
												pCol.pathCount = pCount;
												for (int pIndex = 0; pIndex < pCount; pIndex++) {
													sp.GetPhysicsShape(pIndex, pShapeListCache);
													int len = sp.GetPhysicsShapePointCount(pIndex);
													if (len > 0) {
														if (len < pShapeListCache.Count) {
															pCol.SetPath(pIndex, pShapeListCache.GetRange(0, len).ToArray());
														} else {
															pCol.SetPath(pIndex, pShapeListCache.ToArray());
														}
													}
												}
											}
											break;
										default:
											break;
									}
								}
								break;
						}
						// </Item Loop>
					}
					// Layer Collider
					var rig = layerColliderHolder.gameObject.AddComponent<Rigidbody2D>();
					rig.bodyType = RigidbodyType2D.Kinematic;
					var comCol = layerColliderHolder.gameObject.AddComponent<CompositeCollider2D>();
					comCol.generationType = CompositeCollider2D.GenerationType.Manual;
					comCol.geometryType = CompositeCollider2D.GeometryType.Polygons;
					comCol.GenerateGeometry();
					if (comCol.pointCount > 0) {
						var layerCol = layerTF.gameObject.AddComponent<PolygonCollider2D>();
						int pathCount = comCol.pathCount;
						layerCol.pathCount = pathCount;
						for (int pIndex = 0; pIndex < pathCount; pIndex++) {
							comCol.GetPath(pIndex, pShapeListCache);
							int len = comCol.GetPathPointCount(pIndex);
							if (len > 0) {
								if (len < pShapeListCache.Count) {
									layerCol.SetPath(pIndex, pShapeListCache.GetRange(0, len).ToArray());
								} else {
									layerCol.SetPath(pIndex, pShapeListCache.ToArray());
								}
							}
						}
					}
					// </Layer Loop>
				}
				// </Room Loop>
			}

			// --- Add Data Into Sub Mesh Data
			float halfGridSize = map.GridSize * 0.5f;
			Vector2[] uvCaches = new Vector2[4];
			foreach (var data in itemDataList) {
				if (!data.Mesh || !data.Sprite || !data.Sprite.texture || !meshStreamMap.ContainsKey(data.Mesh) || meshStreamMap[data.Mesh] == null) { continue; }
				var texture = data.Sprite.texture;
				var stream = meshStreamMap[data.Mesh];
				if (!stream.SubMeshMap.ContainsKey(texture)) { continue; }
				var subMeshData = stream.SubMeshMap[texture];
				// Vertex
				int vertIndex = stream.MeshVertices.Count;
				stream.MeshVertices.Add(new Vector2(data.LocalPosition.x - halfGridSize, data.LocalPosition.y - halfGridSize));
				stream.MeshVertices.Add(new Vector2(data.LocalPosition.x - halfGridSize, data.LocalPosition.y + halfGridSize));
				stream.MeshVertices.Add(new Vector2(data.LocalPosition.x + halfGridSize, data.LocalPosition.y + halfGridSize));
				stream.MeshVertices.Add(new Vector2(data.LocalPosition.x + halfGridSize, data.LocalPosition.y - halfGridSize));
				// UV
				var rect = data.Sprite.rect;
				float width = texture.width;
				float height = texture.height;
				uvCaches[0] = new Vector2(rect.xMin / width, rect.yMin / height);
				uvCaches[1] = new Vector2(rect.xMin / width, rect.yMax / height);
				uvCaches[2] = new Vector2(rect.xMax / width, rect.yMax / height);
				uvCaches[3] = new Vector2(rect.xMax / width, rect.yMin / height);
				int indexOffset = data.LocalRotation;
				stream.MeshUVs.Add(uvCaches[(0 + indexOffset) % 4]);
				stream.MeshUVs.Add(uvCaches[(1 + indexOffset) % 4]);
				stream.MeshUVs.Add(uvCaches[(2 + indexOffset) % 4]);
				stream.MeshUVs.Add(uvCaches[(3 + indexOffset) % 4]);
				// Color
				stream.MeshColors.Add(data.Tint);
				stream.MeshColors.Add(data.Tint);
				stream.MeshColors.Add(data.Tint);
				stream.MeshColors.Add(data.Tint);
				// Tri
				subMeshData.MeshTriangles.Add(vertIndex + 0);
				subMeshData.MeshTriangles.Add(vertIndex + 1);
				subMeshData.MeshTriangles.Add(vertIndex + 2);
				subMeshData.MeshTriangles.Add(vertIndex + 0);
				subMeshData.MeshTriangles.Add(vertIndex + 2);
				subMeshData.MeshTriangles.Add(vertIndex + 3);
			}

			// --- Create Meshs From Sub Mesh Data
			foreach (var pair in meshStreamMap) {
				var mesh = pair.Key;
				var stream = pair.Value;
				// Delete Empty Mesh
				if (stream.MeshVertices.Count == 0) {
					var mf = stream.Renderer.GetComponent<MeshFilter>();
					if (mf) { Object.DestroyImmediate(mf, false); }
					Object.DestroyImmediate(stream.Renderer, false);
					if (resources.Contains(mesh)) {
						resources.Remove(mesh);
					}
					continue;
				}
				// Mesh
				mesh.subMeshCount = stream.SubMeshMap.Count;
				mesh.SetVertices(stream.MeshVertices);
				mesh.SetUVs(0, stream.MeshUVs);
				mesh.SetColors(stream.MeshColors);
				// Sub Mesh
				var mats = new List<Material>();
				foreach (var subPair in stream.SubMeshMap) {
					mesh.SetTriangles(subPair.Value.MeshTriangles, mats.Count);
					mats.Add(subPair.Value.Mat);
				}
				stream.Renderer.sharedMaterials = mats.ToArray();
				// End
				mesh.RecalculateNormals();
				mesh.RecalculateTangents();
				mesh.RecalculateBounds();
				mesh.UploadMeshData(true);
			}

			// End
			Object.DestroyImmediate(colliderRoot.gameObject, false);
			return root;
		}


	}
}