namespace OliveMapEditor {
	using UnityEngine;

	public struct Int2 {
		public int x;
		public int y;
		public Int2 (int _x, int _y) {
			x = _x;
			y = _y;
		}
	}

	public struct Int4 {

		public int x0;
		public int y0;
		public int x1;
		public int y1;
		public Int4 (int _x0, int _y0, int _x1, int _y1) {
			x0 = _x0;
			y0 = _y0;
			x1 = _x1;
			y1 = _y1;
		}
		public Int4 (Vector4 v) {
			x0 = Mathf.RoundToInt(v.x);
			y0 = Mathf.RoundToInt(v.y);
			x1 = Mathf.RoundToInt(v.z);
			y1 = Mathf.RoundToInt(v.w);
		}
	}

	public struct Bool2 {
		public bool a;
		public bool b;
		public Bool2 (bool _a, bool _b) {
			a = _a;
			b = _b;
		}
	}

	public struct IndexedPaletteItem {
		public OliveMap.PaletteItem item;
		public int detailIndex;
		public IndexedPaletteItem (OliveMap.PaletteItem _item, int _detailIndex) {
			item = _item;
			if (_item == null || _item.IsEmpty) {
				_detailIndex = 0;
			}
			detailIndex = Mathf.Clamp(_detailIndex, 0, _item.ItemCount - 1);
		}
	}

	public struct TransformLongInt {
		public Transform transform;
		public long a;
		public int b;
		public TransformLongInt (Transform t, long _a, int _b) {
			transform = t;
			a = _a;
			b = _b;
		}
	}

	public struct LongInt4 {
		public long a;
		public int index;
		public int x;
		public int y;
		public int z;
		public LongInt4 (long _a, int _index, int _x, int _y, int _z) {
			a = _a;
			index = _index;
			x = _x;
			y = _y;
			z = _z;
		}
	}

	public struct Vector2Transform {
		public Vector2 v;
		public Transform tf;
		public Vector2Transform (Vector2 _v, Transform _tf) {
			v = _v;
			tf = _tf;
		}
	}

	public enum OliveRotationType {
		Up = 0,
		Left = 1,
		Down = 2,
		Right = 3,

	}


}