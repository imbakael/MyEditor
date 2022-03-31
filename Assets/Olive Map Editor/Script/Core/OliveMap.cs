namespace OliveMapEditor {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;


	[ExecuteInEditMode]
	public class OliveMap : MonoBehaviour {

		#region --- SUB ---

		public const int PALETTE_ITEM_TYPE_COUNT = 3;
		public enum PaletteItemTyle {
			Random = 0,
			AutoTile = 1,

		}

		public enum PaletteAssetType {
			Prefab = 0,
			Sprite = 1,
			Color = 2,

		}

		public const int PALETTE_SORT_COUNT = 3;
		public enum OlivePaletteSortMode {
			Unsorted = 0,
			Type = 1,
			CreateTime = 2,

		}

		public const int COLLIDER_TYPE_COUNT = 4;
		public enum OliveColliderType {
			BoxCollider = 0,
			PhysicsShapeCollider = 1,
			NoCollider = 2,

		}


		public enum PaletteItemRotationType {
			AllRotation = 0,
			Horizontal = 1,
			Vertical = 2,
			Up = 3,
			Left = 4,
			Down = 5,
			Right = 6,
		}



		[System.Serializable]
		public class PaletteItem {

			public const float MIN_SCALE = 0.1f;

			[System.Serializable]
			public class ItemData {
				public Sprite Sprite = null;
				public GameObject Prefab = null;
				public Color Color = Color.white;
				public PaletteItemRotationType Rotation = PaletteItemRotationType.AllRotation;
				public short AutoTileData = 0;
				public ItemData () {
					AutoTileData = CreateAutoTile();
				}
				public ItemData (ItemData source) {
					Sprite = source.Sprite;
					Prefab = source.Prefab;
					Color = source.Color;
					AutoTileData = source.AutoTileData;
				}
				public static implicit operator bool (ItemData item) {
					return item != null;
				}
			}


			// API
			public int ItemCount {
				get {
					return m_Items.Count;
				}
			}

			public bool IsEmpty {
				get {
					return ItemCount == 0;
				}
			}

			public bool IsMuti {
				get {
					return ItemCount > 1;
				}
			}

			public bool IsRandom {
				get {
					return IsMuti && Type == PaletteItemTyle.Random;
				}
			}

			public bool UseRotation {
				get {
					return Type != PaletteItemTyle.AutoTile;
				}
			}

			public List<ItemData> Items {
				get {
					return m_Items ?? (m_Items = new List<ItemData>());
				}
			}

			public long ID { get { return m_ID; } }

			public float Scale {
				get {
					return m_Scale;
				}
				set {
					m_Scale = Mathf.Max(value, MIN_SCALE);
				}
			}

			public Color Tint {
				get { return m_Tint; }
				set {
					m_Tint = value;
				}
			}

			public PaletteItemTyle Type {
				get { return m_Type; }
				set {
					m_Type = value;
				}
			}

			public OliveColliderType ColliderType {
				get {
					return m_ColliderType;
				}
				set {
					m_ColliderType = value;
				}
			}

			public PaletteAssetType AssetType {
				get { return m_AssetType; }
				set {
					m_AssetType = value;
				}
			}

			public System.Type AssetSystemType {
				get {
					switch (AssetType) {
						default:
						case PaletteAssetType.Prefab:
							return typeof(GameObject);
						case PaletteAssetType.Sprite:
							return typeof(Sprite);
						case PaletteAssetType.Color:
							return typeof(Color);
					}
				}
			}


			// Ser
			[SerializeField] private long m_ID = 0;
			[SerializeField] private float m_Scale = 1f;
			[SerializeField] private Color m_Tint = Color.white;
			[SerializeField] private PaletteItemTyle m_Type = PaletteItemTyle.Random;
			[SerializeField] private PaletteAssetType m_AssetType = PaletteAssetType.Sprite;
			[SerializeField] private OliveColliderType m_ColliderType = OliveColliderType.BoxCollider;
			[SerializeField] private List<ItemData> m_Items = new List<ItemData>();


			// API
			public PaletteItem (PaletteItemTyle type, PaletteAssetType assetType) {
				Type = type;
				AssetType = assetType;
				m_ID = System.DateTime.Now.ToFileTime();
			}


			public PaletteItem (PaletteItem source) {
				m_ID = System.DateTime.Now.ToFileTime();
				m_Scale = source.m_Scale;
				m_Tint = source.m_Tint;
				m_Type = source.m_Type;
				m_AssetType = source.AssetType;
				m_ColliderType = source.m_ColliderType;
				m_Items = new List<ItemData>();
				foreach (var item in source.m_Items) {
					m_Items.Add(new ItemData(item));
				}
			}


			public object GetItemAt (int index) {
				if (index >= 0 && index < ItemCount) {
					var itemData = m_Items[index];
					if (itemData) {
						switch (AssetType) {
							case PaletteAssetType.Prefab:
								return itemData.Prefab;
							case PaletteAssetType.Sprite:
								return itemData.Sprite;
							case PaletteAssetType.Color:
								return itemData.Color;
						}
					}
				}
				return null;
			}


			public void SetItemAt (object obj, int index) {
				if (index >= 0 && index < ItemCount) {
					var itemData = m_Items[index];
					if (itemData) {
						switch (AssetType) {
							case PaletteAssetType.Prefab:
								if (obj is GameObject) {
									itemData.Prefab = obj as GameObject;
								} else {
									itemData.Prefab = null;
								}
								break;
							case PaletteAssetType.Sprite:
								if (obj is Sprite) {
									itemData.Sprite = obj as Sprite;
								} else {
									itemData.Sprite = null;
								}
								break;
							case PaletteAssetType.Color:
								if (obj is Color) {
									itemData.Color = (Color)obj;
								} else {
									itemData.Color = Color.white;
								}
								break;
						}
					}
				}
			}


			public void RemoveItemAt (int index) {
				m_Items.RemoveAt(index);
			}


			public void MoveItemAt (int index, bool up) {
				int indexAlt = index + (up ? -1 : 1);
				if (index >= 0 && index < m_Items.Count && indexAlt >= 0 && indexAlt < m_Items.Count) {
					var p = m_Items[index];
					m_Items[index] = m_Items[indexAlt];
					m_Items[indexAlt] = p;
				}
			}


			public void InsertNewItemTo (int index) {
				if (index < 0 || index > ItemCount) { return; }
				m_Items.Insert(index, new ItemData());
			}


			public bool? GetAutoTileBitAt (int index, int adIndex) {
				var itemData = m_Items[index];
				if (itemData) {
					short a = itemData.AutoTileData;
					if (GetBit(a, adIndex + 8)) {
						return GetBit(a, adIndex);
					}
				}
				return null;
			}


			public void SetAutoTileBitAt (int index, int adIndex, bool? value) {
				var itemData = m_Items[index];
				if (itemData) {
					short a = m_Items[index].AutoTileData;
					a = SetBitValue(a, adIndex, value ?? true);
					a = SetBitValue(a, adIndex + 8, value.HasValue);
					m_Items[index].AutoTileData = a;
				}
			}


			// AutoTile
			public static bool CheckAutoTile (short AutoTile, bool u, bool d, bool l, bool r, bool ul, bool ur, bool dl, bool dr) {
				return
					(!GetBit(AutoTile, 8) || GetBit(AutoTile, 0) == ul) &&
					(!GetBit(AutoTile, 9) || GetBit(AutoTile, 1) == u) &&
					(!GetBit(AutoTile, 10) || GetBit(AutoTile, 2) == ur) &&
					(!GetBit(AutoTile, 11) || GetBit(AutoTile, 3) == l) &&
					(!GetBit(AutoTile, 12) || GetBit(AutoTile, 4) == r) &&
					(!GetBit(AutoTile, 13) || GetBit(AutoTile, 5) == dl) &&
					(!GetBit(AutoTile, 14) || GetBit(AutoTile, 6) == d) &&
					(!GetBit(AutoTile, 15) || GetBit(AutoTile, 7) == dr);
			}


			public static short CreateAutoTile (bool? u = false, bool? d = false, bool? l = false, bool? r = false, bool? ul = null, bool? ur = null, bool? dl = null, bool? dr = null) {
				short result = 0;
				result = SetBitValue(result, 0, ul ?? true);
				result = SetBitValue(result, 1, u ?? true);
				result = SetBitValue(result, 2, ur ?? true);
				result = SetBitValue(result, 3, l ?? true);
				result = SetBitValue(result, 4, r ?? true);
				result = SetBitValue(result, 5, dl ?? true);
				result = SetBitValue(result, 6, d ?? true);
				result = SetBitValue(result, 7, dr ?? true);
				result = SetBitValue(result, 8, ul.HasValue);
				result = SetBitValue(result, 9, u.HasValue);
				result = SetBitValue(result, 10, ur.HasValue);
				result = SetBitValue(result, 11, l.HasValue);
				result = SetBitValue(result, 12, r.HasValue);
				result = SetBitValue(result, 13, dl.HasValue);
				result = SetBitValue(result, 14, d.HasValue);
				result = SetBitValue(result, 15, dr.HasValue);
				return result;
			}


			public int GetAutoTileFinalIndex (int i) {
				if (Items == null || i < 0 || i >= ItemCount) { return i; }
				int finalIndex = i;
				short autoData = Items[i].AutoTileData;
				for (int j = i + 1; j < ItemCount; j++) {
					var _data = Items[j];
					if (!_data) { continue; }
					if (autoData != _data.AutoTileData) { break; }
					finalIndex = j;
				}
				return finalIndex;
			}


			// Misc
			public static implicit operator bool (PaletteItem pal) { return pal != null; }


			public bool TryFixItemMaxCount () {
				if (ItemCount > 99) {
					Items.RemoveRange(99, ItemCount - 99);
					return true;
				}
				return false;
			}


			// LGC
			private static bool GetBit (short value, int index) {
				var val = 1 << index;
				return (value & val) == val;
			}


			private static short SetBitValue (short value, int index, bool bitValue) {
				var val = (short)(1 << index);
				return (short)(bitValue ? (value | val) : (value & ~val));
			}


		}

		#endregion

		#region --- VAR ---


		// Global
		public const float GRIDSIZE_MIN = 0.001f;
		private readonly static Color COLLIDER_COLOR = new Color(130f / 255f, 200f / 255f, 115f / 255f, 1);


		// Api
		public Transform MapRoot {
			get {
				if (!_MapRoot) {
					_MapRoot = new GameObject("Root").transform;
					_MapRoot.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
					_MapRoot.SetParent(transform);
					_MapRoot.localPosition = Vector3.zero;
					_MapRoot.localRotation = Quaternion.identity;
					_MapRoot.localScale = Vector3.one;
				}
				//_MapRoot.gameObject.hideFlags = HideFlags.None;
				return _MapRoot;
			}
		}

		public List<PaletteItem> Palette {
			get {
				return m_Palette ?? (m_Palette = new List<PaletteItem>());
			}
		}

		public int RoomCount {
			get {
				return MapRoot.childCount;
			}
		}

		public float Thickness {
			get {
				return m_Thickness;
			}
			set {
				m_Thickness = Mathf.Max(value, 0f);
			}
		}

		public float GridSize {
			get {
				return m_GridSize;
			}
			set {
				if (m_GridSize != value) {
					m_GridSize = value;
				}
			}
		}

		public int SelectingRoomIndex {
			get {
				return m_SelectingRoomIndex;
			}

			set {
				m_SelectingRoomIndex = value;
			}
		}

		public int SelectingLayerIndex {
			get {
				if (SelectingRoomIndex >= 0) {
					if (SelectingRoomIndex >= m_SelectingLayerIndexs.Count) {
						int roomCount = RoomCount;
						while (roomCount > m_SelectingLayerIndexs.Count) {
							m_SelectingLayerIndexs.Add(0);
						}
						if (SelectingRoomIndex >= roomCount) {
							return 0;
						}
					}
					return m_SelectingLayerIndexs[SelectingRoomIndex];
				}
				return 0;
			}

			set {
				if (SelectingRoomIndex >= 0) {
					if (SelectingRoomIndex >= m_SelectingLayerIndexs.Count) {
						int roomCount = RoomCount;
						while (roomCount > m_SelectingLayerIndexs.Count) {
							m_SelectingLayerIndexs.Add(0);
						}
						if (SelectingRoomIndex >= roomCount) { return; }
					}
					m_SelectingLayerIndexs[SelectingRoomIndex] = value;
				}
			}
		}

		public OlivePaletteSortMode PaletteSortMode {
			get {
				return m_PaletteSortMode;
			}
			set {
				m_PaletteSortMode = value;
			}
		}

		public bool OnlyShowSelectingRoom {
			get {
				return m_OnlyShowSelectingRoom;
			}
			set {
				m_OnlyShowSelectingRoom = value;
			}
		}


		// Ser
		[HideInInspector, SerializeField] private Transform _MapRoot = null;
		[HideInInspector, SerializeField] private bool m_OnlyShowSelectingRoom = false;
		[HideInInspector, SerializeField] private OlivePaletteSortMode m_PaletteSortMode = OlivePaletteSortMode.CreateTime;
		[HideInInspector, SerializeField] private List<PaletteItem> m_Palette = new List<PaletteItem>();
		[HideInInspector, SerializeField] private float m_Thickness = 0.1f;
		[HideInInspector, SerializeField] private float m_GridSize = 1f;
		[HideInInspector, SerializeField] private int m_SelectingRoomIndex = 0;
		[HideInInspector, SerializeField] private List<int> m_SelectingLayerIndexs = new List<int>();


		// Data
		private readonly static Dictionary<long, PaletteItem> IdPalMap = new Dictionary<long, PaletteItem>();


		// Cache
		private readonly static Vector3[] BOX_COLLIDER_RENDERER_LINES = new Vector3[4] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, };
		private readonly static List<Vector2> PHYSICS_SHAPE_CACHE_2 = new List<Vector2>();
		private readonly static List<Vector3> PHYSICS_SHAPE_CACHE_3 = new List<Vector3>();


		#endregion

		private void Awake () {
			HideRoot();
		}

		#region --- API ---


		public void HideRoot () {
			MapRoot.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
		}


		public void Fix () {
			// Hide
			HideRoot();
			// Misc
			Thickness = Mathf.Max(0, Thickness);
			GridSize = Mathf.Max(GridSize, GRIDSIZE_MIN);
			// Selecting Layer Indexs
			if (m_SelectingLayerIndexs.Count > RoomCount) {
				m_SelectingLayerIndexs.RemoveRange(RoomCount, m_SelectingLayerIndexs.Count - RoomCount);
			}
			// Palette
			foreach (var pal in Palette) {
				pal.TryFixItemMaxCount();
			}
		}


		public void FixAllRoomLayerItem (int openingRoomIndex, System.Action<Transform> callBack, bool fixGridSize = false) {
			var thickness = Thickness;
			// Rooms
			for (int roomIndex = 0; roomIndex < MapRoot.childCount; roomIndex++) {
				var roomTF = MapRoot.GetChild(roomIndex);
				if (callBack != null) {
					callBack(roomTF);
				}
				roomTF.localPosition = Vector3Round(roomTF.localPosition / GridSize) * GridSize;
				roomTF.localRotation = Quaternion.identity;
				roomTF.localScale = Vector3.one;
				roomTF.gameObject.SetActive(!OnlyShowSelectingRoom || openingRoomIndex < 0 || roomIndex == openingRoomIndex);
				// Layers
				for (int layerIndex = 0; layerIndex < roomTF.childCount; layerIndex++) {
					var layerTF = roomTF.GetChild(layerIndex);
                    callBack?.Invoke(layerTF);
                    layerTF.localPosition = new Vector3(0, 0, thickness * layerIndex);
					layerTF.localRotation = Quaternion.identity;
					layerTF.localScale = Vector3.one;
					// Items
					if (fixGridSize) {
						for (int i = 0; i < layerTF.childCount; i++) {
							var itemTF = layerTF.GetChild(i);
                            callBack?.Invoke(itemTF);
                            itemTF.localPosition = itemTF.localPosition * GridSize / itemTF.localScale.x;
							itemTF.localScale = Vector3.one * GridSize;
						}
					}
				}
			}
		}


		public Vector3 GetSnapedCenterPosition (int roomIndex, Vector3 worldPos) {
			var roomTF = GetRoomTF(roomIndex);
			var roomPos = roomTF ? roomTF.position : Vector3.zero;
			return Vector3Round((worldPos - roomPos) / GridSize) * GridSize + roomPos;
		}


		public Vector4 GetMapRange () {
			Vector4? v = null;
			int count = RoomCount;
			for (int i = 0; i < count; i++) {
				var range = GetRoomRange(i);
				var roomTF = GetRoomTF(i);
				if (!roomTF) { continue; }
				var roomPos = roomTF.localPosition / GridSize;
				range.x += roomPos.x;
				range.y += roomPos.y;
				range.z += roomPos.x;
				range.w += roomPos.y;
				if (!v.HasValue) {
					v = range;
				} else {
					v = new Vector4(
						Mathf.Min(v.Value.x, range.x),
						Mathf.Min(v.Value.y, range.y),
						Mathf.Max(v.Value.z, range.z),
						Mathf.Max(v.Value.w, range.w)
					);
				}
			}
			return v ?? Vector4.zero;
		}


		// Palette
		public PaletteItem GetPaletteItem (long id) {
			if (id < 0) { return null; }
			if (IdPalMap.ContainsKey(id)) {
				return IdPalMap[id];
			} else {
				foreach (var pal in Palette) {
					if (!IdPalMap.ContainsKey(pal.ID)) {
						IdPalMap.Add(pal.ID, pal);
						if (pal.ID == id) {
							return pal;
						}
					}
				}
			}
			return null;
		}


		public void ClearIdPalMap () {
			IdPalMap.Clear();
		}


		public void AddPaletteItem (PaletteItem pal) {
			if (pal) {
				Palette.Add(pal);
				if (!IdPalMap.ContainsKey(pal.ID)) {
					IdPalMap.Add(pal.ID, pal);
				}
			}
		}


		public void RemovePaletteItemAt (int index) {
			if (index >= 0 && index < Palette.Count) {
				var pal = Palette[index];
				if (pal && IdPalMap.ContainsKey(pal.ID)) {
					IdPalMap.Remove(pal.ID);
				}
				Palette.RemoveAt(index);
			}
		}


		// Room
		public Transform GetRoomTF (int index) {
			return MapRoot && index >= 0 && index < RoomCount ? MapRoot.GetChild(index) : null;
		}


		public Transform SpawnRoom (string roomName) {
			var roomTF = new GameObject(roomName).transform;
			roomTF.gameObject.hideFlags = HideFlags.NotEditable;
			roomTF.SetParent(MapRoot);
			roomTF.SetAsLastSibling();
			roomTF.localPosition = Vector3.zero;
			roomTF.localRotation = Quaternion.identity;
			roomTF.localScale = Vector3.one;
			return roomTF;
		}


		public Vector4 GetRoomRange (int roomIndex) {
			Vector4? v = null;
			int layerCount = GetLayerCount(roomIndex);
			for (int i = 0; i < layerCount; i++) {
				var range = GetLayerRange(roomIndex, i);
				if (!v.HasValue) {
					v = range;
				} else {
					v = new Vector4(
						Mathf.Min(v.Value.x, range.x),
						Mathf.Min(v.Value.y, range.y),
						Mathf.Max(v.Value.z, range.z),
						Mathf.Max(v.Value.w, range.w)
					);
				}
			}
			return v ?? Vector4.zero;
		}


		public void MoveRoom (int index, bool up) {
			int altIndex = index + (up ? -1 : 1);
			if (altIndex < 0 || altIndex >= RoomCount) { 
				return;
			}
			var roomTF = GetRoomTF(index);
			if (!roomTF) { 
				return;
			}
			roomTF.SetSiblingIndex(altIndex);
			// Selecting Layer Index
			int count = m_SelectingLayerIndexs.Count;
			if (index >= 0 && index < count && altIndex >= 0 && altIndex < count) {
				int temp = m_SelectingLayerIndexs[index];
				m_SelectingLayerIndexs[index] = m_SelectingLayerIndexs[altIndex];
				m_SelectingLayerIndexs[altIndex] = temp;
			}
		}


		// Layer
		public Transform GetLayerTF (int roomIndex, int layerIndex) {
			return GetLayerTF(GetRoomTF(roomIndex), layerIndex);
		}


		public Transform GetLayerTF (Transform roomTF, int layerIndex) {
			return roomTF && layerIndex >= 0 && layerIndex < roomTF.childCount ? roomTF.GetChild(layerIndex) : null;
		}


		public int GetLayerCount (int roomIndex) {
			return roomIndex >= 0 && roomIndex < RoomCount ? MapRoot.GetChild(roomIndex).childCount : 0;
		}


		public Transform SpawnLayer (string layerName, int roomIndex) {
			var roomTF = GetRoomTF(roomIndex);
			if (!roomTF) { return null; }
			var layerTF = new GameObject(layerName).transform;
			layerTF.SetParent(roomTF);
			layerTF.SetAsLastSibling();
			layerTF.gameObject.hideFlags = HideFlags.NotEditable;
			layerTF.localRotation = Quaternion.identity;
			layerTF.localScale = Vector3.one;
			return layerTF;
		}


		public Vector4 GetLayerRange (int roomIndex, int layerIndex) {
			return GetLayerRange(GetLayerTF(roomIndex, layerIndex));
		}


		public Vector4 GetLayerRange (Transform layerTF) {
			if (!layerTF) {
				return Vector4.zero;
			}
			Vector4? v = null;
			int itemCount = layerTF.childCount;
			for (int i = 0; i < itemCount; i++) {
				Vector2 pos = layerTF.GetChild(i).localPosition / GridSize;
				if (!v.HasValue) {
					v = new Vector4(pos.x, pos.y, pos.x, pos.y);
				} else {
					v = new Vector4(
						Mathf.Min(v.Value.x, pos.x),
						Mathf.Min(v.Value.y, pos.y),
						Mathf.Max(v.Value.z, pos.x),
						Mathf.Max(v.Value.w, pos.y)
					);
				}
			}
			return v ?? Vector4.zero;
		}


		// Item
		public static long GetItemID (Transform item) {
			if (!item || item.name.Length < 18) { return -1; }
            var str = item.name.Substring(0, 18);
            if (long.TryParse(str, out long id)) {
				return id;
			}
			return -1;
		}


		public static int GetDetailIndex (Transform item) {
			if (!item || item.name.Length < 20) { return 0; }
			return (item.name[18] - '0') * 10 + (item.name[19] - '0');
		}


		public static int GetItemRotation (Transform item) {
			if (!item || item.name.Length < 21) { return 0; }
			return item.name[20] - '0';
		}


		public static PaletteAssetType GetItemAssetType (Transform item) {
			if (!item || item.name.Length < 22) { return PaletteAssetType.Sprite; }
			return (PaletteAssetType)(item.name[21] - '0');
		}


		public static OliveColliderType GetItemColliderType (Transform item) {
			if (!item || item.name.Length < 23) { return OliveColliderType.NoCollider; }
			return (OliveColliderType)(item.name[22] - '0');
		}


		public Transform GetItemIn (int roomIndex, int layerIndex, int x, int y) {
			var roomTF = GetRoomTF(roomIndex);
			if (!roomTF) { return null; }
			return GetItemIn(
				GetLayerTF(roomIndex, layerIndex),
				new Vector2(x * GridSize, y * GridSize) + (Vector2)roomTF.position
			);
		}


		public Transform GetItemIn (Transform layerTF, Vector2 position) {
			if (!layerTF) { return null; }
			var cols = Physics2D.OverlapBoxAll(position, Vector2.one * GridSize * 0.1f, 0f);
			foreach (var col in cols) {
				if (col.transform.parent == layerTF) {
					return col.transform;
				}
			}
			return null;
		}


		public void ForItemsIn (int roomIndex, int layerIndex, int minX, int minY, int maxX, int maxY, System.Func<Transform, bool> action) {
			var roomTF = GetRoomTF(roomIndex);
			if (!roomTF) { return; }
			ForItemsIn(
				roomIndex, layerIndex,
				new Vector2(minX * GridSize, minY * GridSize) + (Vector2)roomTF.position,
				new Vector2((maxX - minX) * GridSize + GRIDSIZE_MIN, (maxY - minY) * GridSize + GRIDSIZE_MIN),
				action
			);
		}


		public void ForItemsIn (Transform layerTF, int minX, int minY, int maxX, int maxY, System.Func<Transform, bool> action) {
			if (!layerTF || !layerTF.parent) { return; }
			ForItemsIn(
				layerTF,
				new Vector2(minX * GridSize, minY * GridSize) + (Vector2)layerTF.parent.position,
				new Vector2((maxX - minX) * GridSize + GRIDSIZE_MIN, (maxY - minY) * GridSize + GRIDSIZE_MIN),
				action
			);

		}


		public void ForItemsIn (int roomIndex, int layerIndex, Vector2 minPos, Vector2 size, System.Func<Transform, bool> action) {
			ForItemsIn(GetLayerTF(roomIndex, layerIndex), minPos, size, action);
		}


		public void ForItemsIn (Transform layerTF, Vector2 minPos, Vector2 size, System.Func<Transform, bool> action) {
			if (!layerTF) { return; }
			var cols = Physics2D.OverlapBoxAll(minPos + size * 0.5f, size, 0f);
			foreach (var col in cols) {
				if (!col) { continue; }
				if (col.transform.parent == layerTF) {
					bool exit = action(col.transform);
					if (exit) { break; }
				}
			}
		}


		// ※
		public Transform SpawnItem (PaletteItem pal, Transform layerTF, int x, int y, int rotation, Color tint, int detailIndex, bool showCollider) {

			if (pal == null || layerTF == null || pal.IsEmpty || pal.AssetType == PaletteAssetType.Color) { return null; }

			// Auto Tile Random
			if (pal.Type == PaletteItemTyle.AutoTile) {
				int finalIndex = pal.GetAutoTileFinalIndex(detailIndex);
				detailIndex = detailIndex == finalIndex ? detailIndex : (int)Random.Range(detailIndex, finalIndex + 0.999f);
			}

			// Item
			var itemTF = new GameObject(
				GetItemName(pal.ID, detailIndex, rotation, pal.AssetType, pal.ColliderType),
				typeof(BoxCollider2D)
			).transform;
			itemTF.SetParent(layerTF);
			itemTF.SetAsLastSibling();
			itemTF.gameObject.hideFlags = HideFlags.NotEditable | HideFlags.HideInInspector;
			itemTF.localPosition = new Vector3(x * GridSize, y * GridSize, 0);
			itemTF.localScale = Vector3.one * GridSize;
			itemTF.localRotation = pal.UseRotation ? Quaternion.Euler(0, 0, rotation * 90f) : Quaternion.identity;
			// Col
			var col = itemTF.GetComponent<BoxCollider2D>();
			col.size = Vector2.one * 0.618f;
			col.offset = Vector2.zero;
			col.edgeRadius = 0f;
			col.isTrigger = false;
			// Renderer
			Sprite sprite = null;
			Transform prefab = null;
			// Get Asset
			var itemData = pal.Items[Mathf.Clamp(detailIndex, 0, pal.ItemCount - 1)];
			switch (pal.AssetType) {
				case PaletteAssetType.Prefab:
					prefab = itemData != null && itemData.Prefab ? itemData.Prefab.transform : null;
					break;
				case PaletteAssetType.Sprite:
					sprite = itemData != null ? itemData.Sprite : null;
					break;
			}
			// Sprite
			if (sprite) {
				var spriteTF = new GameObject().transform;
				spriteTF.SetParent(itemTF);
				spriteTF.localPosition = Vector3.zero;
				spriteTF.localRotation = Quaternion.identity;
				spriteTF.localScale = GetSpriteScale(sprite);
				var sr = spriteTF.gameObject.AddComponent<SpriteRenderer>();
				sr.sprite = sprite;
				sr.color = pal.Tint * tint;
				SpawnItemEditCollider(itemTF, sprite, pal.ColliderType, showCollider);
				return itemTF;
			}
			// Prefab
			if (prefab) {
				var prefabTF = Instantiate(prefab, itemTF);
				prefabTF.localPosition = Vector3.zero;
				prefabTF.localRotation = Quaternion.identity;
				prefabTF.localScale = Vector3.one * pal.Scale;
				return itemTF;
			}
			// Null
			DestroyImmediate(itemTF.gameObject, false);
			return null;
		}


		public void FixItemsPositionZ (int roomIndex, int layerIndex, float z) {
			var layerTF = GetLayerTF(roomIndex, layerIndex);
			if (layerTF) {
				int count = layerTF.childCount;
				for (int i = 0; i < count; i++) {
					var itemTF = layerTF.GetChild(i);
					var pos = itemTF.localPosition;
					pos.z = z;
					itemTF.localPosition = pos;
				}
			}
		}


		public void RefreshColliderDisplayer (Transform itemTF, OliveColliderType colType, Sprite sp, bool show) {
			if (!itemTF || itemTF.childCount < 2) { return; }
			RefreshColliderDisplayer(itemTF.GetChild(1).GetComponent<LineRenderer>(), colType, sp, show);
		}


		public void ShowAllCollider (bool show) {
			int roomCount = RoomCount;
			for (int _r = 0; _r < RoomCount; _r++) {
				int layerCount = GetLayerCount(_r);
				for (int _l = 0; _l < layerCount; _l++) {
					var layer = GetLayerTF(_r, _l);
					if (!layer) { continue; }
					int itemCount = layer.childCount;
					for (int i = 0; i < itemCount; i++) {
						var item = layer.GetChild(i);
						if (GetItemAssetType(item) != PaletteAssetType.Sprite) { continue; }
						var sp = GetItemSprite(item);
						if (!sp) { continue; }
						var colType = GetItemColliderType(item);
						if (item.childCount < 2) {
							SpawnItemEditCollider(item, sp, colType, show);
						} else {
							RefreshColliderDisplayer(item, colType, sp, show);
						}
					}
				}
			}
		}


		public static Vector3 GetSpriteScale (Sprite sp) {
			return sp ? new Vector3(
				sp.pixelsPerUnit / sp.textureRect.width,
				sp.pixelsPerUnit / sp.textureRect.height,
				1f
			) : Vector3.one;
		}


		public static string GetItemName (long id, int index, int rot, PaletteAssetType assetType, OliveColliderType colType) {
			// paletteID(18) , detailIndex(2) , rotation(1) , asset type(1) , collider(1)
			// 0-17 , 18-19 , 20 , 21 , 22 
			return string.Format("{0}{1}{2}{3}{4}", id.ToString("000000000000000000"), index.ToString("00"), rot.ToString(), ((int)assetType).ToString(), ((int)colType).ToString());
		}


		#endregion


		#region --- LGC ---

		private Vector3 Vector3Round (Vector3 v) {
			v.x = Mathf.Round(v.x);
			v.y = Mathf.Round(v.y);
			v.z = Mathf.Round(v.z);
			return v;
		}


		private void SpawnItemEditCollider (Transform item, Sprite sprite, OliveColliderType colType, bool showCollider) {
			if (item.childCount != 1 || colType == OliveColliderType.NoCollider) { return; }
			var colliderDisplayer = new GameObject("Col", typeof(LineRenderer)).transform;
			colliderDisplayer.SetParent(item);
			colliderDisplayer.SetAsLastSibling();
			colliderDisplayer.localPosition = new Vector3(0, 0, -0.01f);
			colliderDisplayer.localRotation = Quaternion.identity;
			colliderDisplayer.localScale = Vector3.one;
			var line = colliderDisplayer.GetComponent<LineRenderer>();
			line.useWorldSpace = false;
			line.startWidth = line.endWidth = 1f;
			line.widthMultiplier = 0.0618f;
			line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
			line.startColor = line.endColor = COLLIDER_COLOR;
			line.numCapVertices = 1;
			line.loop = true;
			RefreshColliderDisplayer(line, colType, sprite, showCollider);
		}


		private void RefreshColliderDisplayer (LineRenderer line, OliveColliderType colType, Sprite sprite, bool show) {
			if (!line) { return; }
			line.enabled = show;
			line.startColor = line.endColor = COLLIDER_COLOR;
			int shapeCount = sprite ? sprite.GetPhysicsShapeCount() : 0;
			if (colType == OliveColliderType.PhysicsShapeCollider && shapeCount == 0) {
				colType = OliveColliderType.BoxCollider;
			}
			switch (colType) {
				default:
				case OliveColliderType.NoCollider:
					line.positionCount = 0;
					break;
				case OliveColliderType.BoxCollider:
					BOX_COLLIDER_RENDERER_LINES[0] = new Vector2(-0.5f, -0.5f);
					BOX_COLLIDER_RENDERER_LINES[1] = new Vector2(0.5f, -0.5f);
					BOX_COLLIDER_RENDERER_LINES[2] = new Vector2(0.5f, 0.5f);
					BOX_COLLIDER_RENDERER_LINES[3] = new Vector2(-0.5f, 0.5f);
					line.positionCount = 4;
					line.SetPositions(BOX_COLLIDER_RENDERER_LINES);
					break;
				case OliveColliderType.PhysicsShapeCollider:
					int maxCount = 0;
					int maxCountIndex = 0;
					if (shapeCount > 1) {
						for (int i = 0; i < shapeCount; i++) {
							int count = sprite.GetPhysicsShapePointCount(i);
							if (maxCount < count) {
								maxCount = count;
								maxCountIndex = i;
							}
						}
					}
					int len = sprite.GetPhysicsShape(maxCountIndex, PHYSICS_SHAPE_CACHE_2);
					PHYSICS_SHAPE_CACHE_3.Clear();
					for (int i = 0; i < len; i++) {
						PHYSICS_SHAPE_CACHE_3.Add(PHYSICS_SHAPE_CACHE_2[i]);
					}
					line.positionCount = len;
					if (len < PHYSICS_SHAPE_CACHE_3.Count) {
						line.SetPositions(PHYSICS_SHAPE_CACHE_3.GetRange(0, len).ToArray());
					} else {
						line.SetPositions(PHYSICS_SHAPE_CACHE_3.ToArray());
					}
					break;
			}


		}


		private Sprite GetItemSprite (Transform item) {
			if (!item || item.childCount < 1) { return null; }
			var sr = item.GetChild(0).GetComponent<SpriteRenderer>();
			if (!sr) { return null; }
			return sr.sprite;
		}


		#endregion
	}
}