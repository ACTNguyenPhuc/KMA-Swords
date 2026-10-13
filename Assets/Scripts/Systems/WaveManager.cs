using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oath
{
    public enum WavePhase { Waiting, Preparing, Spawning, Fighting, Done }

    /// <summary>
    /// Khởi tạo từng đợt địch theo LevelConfig: đếm ngược chuẩn bị, sinh địch tại điểm xuất phát và gán waypoint,
    /// kiểm tra số địch còn sống để kết thúc đợt.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager I { get; private set; }

        LevelConfig cfg;
        public int WaveIndex { get; private set; } = -1;     // đợt hiện tại (0-based), -1 = chưa bắt đầu
        public int TotalWaves => cfg == null ? 0 : (cfg.endless ? WaveIndex + 2 : cfg.waves.Count);
        public bool Endless => cfg != null && cfg.endless;
        public int WavesCleared { get; private set; }
        public WavePhase Phase { get; private set; } = WavePhase.Waiting;
        public float PrepRemaining { get; private set; }
        public int DisplayWave => Mathf.Clamp(WaveIndex + 1, 0, TotalWaves);
        public int EnemiesRemaining => World.CountUnits(Team.Enemy) + pendingSpawns;

        public event Action OnChanged;
        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCleared;

        readonly List<List<Vector2>> routes = new List<List<Vector2>>();
        readonly List<GameObject> previews = new List<GameObject>();
        int pendingSpawns;
        bool everyoneSpawned;

        public IReadOnlyList<List<Vector2>> Routes => routes;

        public void Init(LevelConfig c)
        {
            I = this;
            cfg = c;
            var map = World.Map;
            Vector2 gate = World.Castle != null ? World.Castle.DoorPoint : Vector2.zero;
            foreach (var s in map.spawns)
            {
                Vector2 start = MapData.CellCenter(s);
                var path = World.Nav.FindPath(start, gate, true);
                var route = new List<Vector2> { start };
                if (path != null) route.AddRange(path);
                else Debug.LogWarning("Không tìm được đường từ điểm xuất hiện " + s);
                routes.Add(route);
            }
            BuildPreview();
        }

        void OnDestroy() { if (I == this) I = null; }

        void BuildPreview()
        {
            var parent = new GameObject("EnemyPathPreview").transform;
            if (World.Root != null) parent.SetParent(World.Root, false);
            foreach (var route in routes)
            {
                if (route.Count == 0) continue;
                // Đánh dấu điểm xuất hiện
                var marker = new GameObject("SpawnMarker");
                marker.transform.SetParent(parent, false);
                marker.transform.position = route[0];
                var ring = Util.NewSprite("Ring", marker.transform, Util.Ellipse(true), Vector3.zero, Util.OrderGroundDecal + 3);
                ring.color = new Color(1f, 0.3f, 0.25f, 0.9f);
                ring.transform.localScale = new Vector3(1.6f, 1.6f, 1);
                var icon = Util.NewSprite("Icon", marker.transform, GameArt.I.Spr(GameArt.Icon(5)), new Vector3(0, 0.7f, 0), Util.OrderOverlay - 300);
                icon.color = new Color(1f, 0.55f, 0.5f);
                icon.transform.localScale = Vector3.one * 0.8f;
                marker.AddComponent<Pulser>();
                previews.Add(marker);

                // Chấm đỏ dọc đường đi
                const float Spacing = 0.6f;
                float carry = 0f; // quãng đường kể từ chấm cuối
                for (int i = 1; i < route.Count; i++)
                {
                    Vector2 a = route[i - 1], b = route[i];
                    float len = Vector2.Distance(a, b);
                    float d = Spacing - carry;
                    while (d < len)
                    {
                        Vector2 p = Vector2.Lerp(a, b, d / len);
                        var dot = Util.NewSprite("Dot", parent, Util.Circle(), p, Util.OrderGroundDecal + 2);
                        dot.color = new Color(1f, 0.28f, 0.22f, 0.55f);
                        dot.transform.localScale = Vector3.one * 0.18f;
                        previews.Add(dot.gameObject);
                        d += Spacing;
                    }
                    carry = len - (d - Spacing);
                }
            }
        }

        void SetPreviewVisible(bool on)
        {
            foreach (var p in previews) if (p != null) p.SetActive(on);
        }

        public void BeginFirstPrep()
        {
            if (Phase != WavePhase.Waiting) return;
            Phase = WavePhase.Preparing;
            PrepRemaining = cfg.firstWavePrep;
            SetPreviewVisible(true);
            OnChanged?.Invoke();
        }

        void Update()
        {
            if (GameManager.I == null || GameManager.I.State != GameState.Playing) return;
            switch (Phase)
            {
                case WavePhase.Preparing:
                    PrepRemaining -= Time.deltaTime;
                    if (PrepRemaining <= 0f) StartWave();
                    break;
                case WavePhase.Spawning:
                case WavePhase.Fighting:
                    if (everyoneSpawned && pendingSpawns <= 0)
                    {
                        Phase = WavePhase.Fighting;
                        if (World.CountUnits(Team.Enemy) == 0) WaveCleared();
                    }
                    break;
            }
        }

        public bool CanCallEarly => Phase == WavePhase.Preparing;

        WaveConfig endlessCurrent;
        int endlessIndex = -1;

        WaveConfig CurrentWaveConfig()
        {
            if (!Endless) return cfg.waves[WaveIndex];
            if (endlessIndex != WaveIndex)
            {
                endlessIndex = WaveIndex;
                endlessCurrent = EndlessWaves.Generate(WaveIndex, routes.Count);
            }
            return endlessCurrent;
        }

        /// <summary>Gọi đợt sớm: thưởng vàng theo thời gian còn lại.</summary>
        public void CallEarly()
        {
            if (Phase != WavePhase.Preparing) return;
            int bonus = Mathf.FloorToInt(PrepRemaining * 0.6f);
            if (bonus > 0 && ResourceManager.I != null)
            {
                ResourceManager.I.AddGold(bonus);
                GameEvents.Message("Gọi đợt sớm: +" + bonus + " vàng", MessageKind.Good);
            }
            StartWave();
        }

        void StartWave()
        {
            WaveIndex++;
            Phase = WavePhase.Spawning;
            everyoneSpawned = false;
            SetPreviewVisible(false);
            var wave = CurrentWaveConfig();
            AudioManager.Play("horn", 0.8f);
            string wname = string.IsNullOrEmpty(wave.name) ? "" : " — " + wave.name;
            GameEvents.Message("Đợt " + (WaveIndex + 1) + (Endless ? "" : "/" + TotalWaves) + wname + "!", MessageKind.Danger);
            StartCoroutine(RunWave(wave));
            OnWaveStarted?.Invoke(WaveIndex);
            OnChanged?.Invoke();
        }

        IEnumerator RunWave(WaveConfig wave)
        {
            int running = wave.groups.Count;
            foreach (var g in wave.groups)
            {
                pendingSpawns += g.count;
                StartCoroutine(RunGroup(g, () => running--));
            }
            while (running > 0) yield return null;
            everyoneSpawned = true;
        }

        IEnumerator RunGroup(SpawnGroup g, Action done)
        {
            if (g.delay > 0f) yield return new WaitForSeconds(g.delay);
            for (int i = 0; i < g.count; i++)
            {
                SpawnEnemy(g.enemy, g.spawn);
                pendingSpawns--;
                OnChanged?.Invoke();
                if (i < g.count - 1) yield return new WaitForSeconds(g.interval);
            }
            done();
        }

        public Unit SpawnEnemy(string id, int spawnIdx)
        {
            if (routes.Count == 0) return null;
            var route = routes[Mathf.Abs(spawnIdx) % routes.Count];
            Vector2 pos = route[0] + UnityEngine.Random.insideUnitCircle * 0.3f;
            if (!World.Nav.IsWalkable(pos)) pos = route[0];
            var u = Unit.Spawn(id, pos);
            u.enemyAI.Init(route, UnityEngine.Random.Range(-0.3f, 0.3f));
            if (Endless && WaveIndex > 0)
            {
                // Sinh tồn: kẻ địch bền dần theo số đợt
                float mul = 1f + 0.08f * WaveIndex;
                u.health.max *= mul;
                u.health.SetCurrent(u.health.max);
            }
            FX.Play("dust", pos, 1f);
            if (u.def.boss)
            {
                GameEvents.Message(u.def.name + " đã xuất hiện!", MessageKind.Danger);
                AudioManager.Play("boss", 0.9f);
                if (CameraController.I != null) CameraController.I.Shake(0.4f);
            }
            return u;
        }

        void WaveCleared()
        {
            var wave = CurrentWaveConfig();
            WavesCleared = WaveIndex + 1;
            if (wave.bonusGold > 0 && ResourceManager.I != null) ResourceManager.I.AddGold(wave.bonusGold);
            OnWaveCleared?.Invoke(WaveIndex);
            if (Endless) SaveManager.RecordEndless(WavesCleared);
            if (!Endless && WaveIndex + 1 >= TotalWaves)
            {
                Phase = WavePhase.Done;
                OnChanged?.Invoke();
                if (GameManager.I != null) GameManager.I.Victory();
                return;
            }
            GameEvents.Message("Đã đẩy lùi đợt " + (WaveIndex + 1) + "! +" + wave.bonusGold + " vàng", MessageKind.Good);
            AudioManager.Play("cleared", 0.7f);
            Phase = WavePhase.Preparing;
            PrepRemaining = cfg.betweenWavePrep;
            SetPreviewVisible(true);
            OnChanged?.Invoke();
        }
    }

    /// <summary>Sinh đợt địch cho chế độ Sinh tồn: càng về sau càng đông, càng nhiều loại mạnh; trùm mỗi 5 đợt.</summary>
    public static class EndlessWaves
    {
        static readonly string[][] Tiers =
        {
            new[] { "red_warrior", "goblin", "slime", "duck", "skull" },
            new[] { "red_archer", "goblin_archer", "goblin_warrior", "skull_archer", "mushroom", "eggplant", "wolf" },
            new[] { "red_lancer", "slime_stone", "skull_warrior", "boar", "bomb", "spiderling", "goblin_green_warrior", "red_monk", "fury" },
            new[] { "hellhorse", "centaur", "arachne", "skull_poison_warrior", "duck_elite", "boar_elite", "mushroom_elite", "slime_fire_stone" },
        };
        static readonly string[] Bosses = { "slime_king", "lion", "slime_fire_king", "veyr", "chimera" };
        static readonly string[] Titles = { "Đợt thăm dò", "Bầy quái", "Tấn công dồn dập", "Tinh nhuệ", "Hỗn chiến", "Bão tố" };

        public static WaveConfig Generate(int index, int spawnCount)
        {
            var rnd = new System.Random(7919 + index * 31);
            var w = new WaveConfig { bonusGold = 35 + index * 6 };
            int tierMax = index < 2 ? 0 : (index < 5 ? 1 : (index < 9 ? 2 : 3));
            float budget = 10f + index * 4.5f;
            int kinds = Mathf.Min(2 + index / 3, 5);
            float delay = 0f;
            for (int k = 0; k < kinds && budget > 0f; k++)
            {
                int tier = rnd.Next(Mathf.Max(0, tierMax - 1), tierMax + 1);
                var pool = Tiers[tier];
                string id = pool[rnd.Next(pool.Length)];
                var d = UnitCatalog.Get(id);
                float cost = Mathf.Max(1f, d.hp / 45f);
                int n = Mathf.Clamp(Mathf.RoundToInt(budget / kinds / cost), 1, 14);
                budget -= n * cost;
                w.groups.Add(new SpawnGroup(id, n, rnd.Next(Mathf.Max(1, spawnCount)), delay, Mathf.Clamp(2.4f - index * 0.08f, 0.8f, 2.4f)));
                delay += 3f + rnd.Next(4);
            }
            if ((index + 1) % 5 == 0)
            {
                string boss = Bosses[Mathf.Min(Bosses.Length - 1, (index + 1) / 5 - 1)];
                w.groups.Add(new SpawnGroup(boss, 1 + (index + 1) / 30, rnd.Next(Mathf.Max(1, spawnCount)), delay + 4f, 6f));
                w.name = "Thủ lĩnh: " + UnitCatalog.Get(boss).name;
                w.bonusGold += 60;
            }
            else w.name = Titles[index % Titles.Length];
            return w;
        }
    }

    /// <summary>Phóng to/thu nhỏ nhịp nhàng để thu hút chú ý.</summary>
    public class Pulser : MonoBehaviour
    {
        Vector3 baseScale;
        void Start() { baseScale = transform.localScale; }
        void Update() { transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * 4f) * 0.08f); }
    }
}
