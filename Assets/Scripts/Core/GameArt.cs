using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Oath
{
    [Serializable]
    public class SpriteSet
    {
        public string key;
        public Sprite[] frames;
    }

    [Serializable]
    public class NamedPrefab
    {
        public string key;
        public GameObject prefab;
    }

    [Serializable]
    public class NamedTile
    {
        public string key;
        public TileBase tile;
    }

    /// <summary>
    /// Kho tham chiếu tới toàn bộ hình ảnh dùng trong game (sinh tự động bởi Editor: Oath/Thiết lập toàn bộ).
    /// Khoá là đường dẫn asset bỏ "Assets/" và phần mở rộng, ví dụ "Tiny Swords/Units/Blue Units/Warrior/Warrior_Idle".
    /// </summary>
    public class GameArt : ScriptableObject
    {
        public Font font;
        public List<SpriteSet> sets = new List<SpriteSet>();
        public List<NamedPrefab> prefabs = new List<NamedPrefab>();
        public List<NamedTile> tiles = new List<NamedTile>();

        static GameArt instance;
        Dictionary<string, Sprite[]> setMap;
        Dictionary<string, GameObject> prefabMap;
        Dictionary<string, TileBase> tileMap;

        public static GameArt I
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameArt>("GameArt");
                    if (instance == null)
                        Debug.LogError("Không tìm thấy Resources/GameArt. Hãy chạy menu Oath > Thiết lập toàn bộ.");
                }
                return instance;
            }
        }

        void BuildMaps()
        {
            setMap = new Dictionary<string, Sprite[]>();
            foreach (var s in sets) setMap[s.key] = s.frames;
            prefabMap = new Dictionary<string, GameObject>();
            foreach (var p in prefabs) prefabMap[p.key] = p.prefab;
            tileMap = new Dictionary<string, TileBase>();
            foreach (var t in tiles) tileMap[t.key] = t.tile;
        }

        public Sprite[] Set(string key)
        {
            if (setMap == null) BuildMaps();
            if (setMap.TryGetValue(key, out var f)) return f;
            Debug.LogWarning("Thiếu sprite set: " + key);
            return Array.Empty<Sprite>();
        }

        public bool Has(string key)
        {
            if (setMap == null) BuildMaps();
            return setMap.ContainsKey(key);
        }

        public Sprite Spr(string key, int index = 0)
        {
            var f = Set(key);
            if (f.Length == 0) return null;
            return f[Mathf.Clamp(index, 0, f.Length - 1)];
        }

        public GameObject Prefab(string key)
        {
            if (prefabMap == null) BuildMaps();
            prefabMap.TryGetValue(key, out var p);
            if (p == null) Debug.LogWarning("Thiếu prefab: " + key);
            return p;
        }

        public TileBase Tile(string key)
        {
            if (tileMap == null) BuildMaps();
            tileMap.TryGetValue(key, out var t);
            return t;
        }

        // Đường dẫn hay dùng
        public const string TS = "Tiny Swords/";
        public const string UI = "Tiny Swords/UI Elements/";
        public static string Icon(int i) => UI + "Icons/Icon_" + i.ToString("00");
        public static string Avatar(int i) => UI + "Human Avatars/Avatars_" + i.ToString("00");
    }
}
