using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oath
{
    [Serializable]
    public class DialogLine
    {
        public string speaker;   // "Kael", "Mira", "Veyr", "Kể chuyện"
        [TextArea(2, 5)] public string text;

        public DialogLine() { }
        public DialogLine(string speaker, string text) { this.speaker = speaker; this.text = text; }
    }

    [Serializable]
    public class SpawnGroup
    {
        public string enemy;          // id trong UnitCatalog
        public int count = 3;
        public int spawn;             // chỉ số điểm xuất hiện (thứ tự ký tự 'S' trên bản đồ, đọc trái→phải, trên→dưới)
        public float delay;           // giây tính từ lúc đợt bắt đầu
        public float interval = 1.2f; // giây giữa hai con

        public SpawnGroup() { }
        public SpawnGroup(string enemy, int count, int spawn = 0, float delay = 0f, float interval = 1.2f)
        {
            this.enemy = enemy; this.count = count; this.spawn = spawn; this.delay = delay; this.interval = interval;
        }
    }

    [Serializable]
    public class WaveConfig
    {
        public string name;
        public int bonusGold = 40;
        public List<SpawnGroup> groups = new List<SpawnGroup>();
    }

    /// <summary>
    /// Cấu hình một màn chơi: bản đồ, tài nguyên ban đầu, các đợt tấn công, cốt truyện.
    /// Bản đồ là lưới ký tự (xem MapLegend trong MapData.cs).
    /// </summary>
    [CreateAssetMenu(menuName = "Oath/Level Config", fileName = "LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Thông tin")]
        public int levelIndex = 1;
        public string title = "Ngọn Lửa Đầu Tiên";
        public string location = "Làng Greenhaven";
        [TextArea(2, 4)] public string description;
        [TextArea(1, 3)] public string objective = "Bảo vệ lâu đài";
        public string sceneName;

        [Header("Cốt truyện")]
        public List<DialogLine> intro = new List<DialogLine>();
        public List<DialogLine> outro = new List<DialogLine>();
        public List<string> tutorialHints = new List<string>();

        [Header("Bản đồ")]
        [TextArea(10, 40)] public string map;
        public int groundColor = 1;      // Tilemap_color1..5
        public int roadColor = 4;

        [Header("Kinh tế")]
        public int startWood = 150;
        public int startGold = 150;
        public int startPawns = 2;
        public int basePopulation = 10;
        public int castleHp = 1500;
        public bool allowTowerUpgrade = true;
        public List<TowerType> towers = new List<TowerType>();   // tháp được phép xây (trống = tất cả)
        public int maxTowerLevel = 3;

        [Header("Chế độ sinh tồn")]
        public bool endless;

        [Header("Nhiệm vụ phụ")]
        public bool protectVillage;
        public int villageMinAlive = 1;

        [Header("Đợt tấn công")]
        public float firstWavePrep = 40f;
        public float betweenWavePrep = 22f;
        public List<WaveConfig> waves = new List<WaveConfig>();

        public string[] MapRows()
        {
            return map.Replace("\r", "").Trim('\n').Split('\n');
        }
    }
}
