using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oath
{
    public enum BuildingType { Castle, Barracks, Tower, House, Village }

    /// <summary>
    /// Công trình: máu, vùng chiếm chỗ trên lưới, xây dựng, sửa chữa, hàng đợi tuyển quân, hiệu ứng cháy khi hư hại.
    /// </summary>
    public class Building : MonoBehaviour
    {
        public static event Action<Building> AnyDestroyed;
        public static event Action<Building> AnyCompleted;

        public BuildingType type;
        public Team team = Team.Player;
        public Health health;
        public RectInt cells;
        public string displayName;
        public SpriteRenderer sr;
        public BuildPlot plot;
        public TowerController tower;
        public HealthBar bar;

        public bool constructing;
        public float buildProgress;
        public bool repairing;
        public int assignedPawns, activeWorkers;
        public int level = 1;
        public TowerType towerType;
        public TargetMode targetMode = TargetMode.First;
        public int totalSpent;      // tổng gỗ + vàng đã chi (để tính tiền bán lại)

        public readonly List<string> queue = new List<string>();
        public float trainTimer;
        public Vector2 rallyPoint;

        float visualHeight = 2f;
        readonly GameObject[] fires = new GameObject[2];
        float dustTimer;
        SpriteRenderer ring;
        bool destroyed;

        public bool NeedsWorker => constructing || repairing;
        public Rect WorldRect => new Rect(cells.x, cells.y, cells.width, cells.height);
        public Vector2 Base => new Vector2(cells.x + cells.width * 0.5f, cells.y);
        public Vector2 AimPoint => Base + Vector2.up * Mathf.Min(visualHeight * 0.4f, 1.6f);
        public Vector2 DoorPoint { get; private set; }
        public bool CanTrain => (type == BuildingType.Barracks || type == BuildingType.Castle) && !constructing;

        public float DistanceTo(Vector2 p)
        {
            return Vector2.Distance(p, Util.ClosestPointOnRect(WorldRect, p));
        }

        /// <summary>Vùng bấm chọn: theo hình ảnh hiển thị thay vì ô chiếm chỗ.</summary>
        public bool HitTest(Vector2 p)
        {
            if (WorldRect.Contains(p)) return true;
            var r = new Rect(cells.x + 0.15f, cells.y, cells.width - 0.3f, visualHeight * 0.85f);
            return r.Contains(p);
        }

        public static string NameOf(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.Castle: return "Lâu đài";
                case BuildingType.Barracks: return "Doanh trại";
                case BuildingType.Tower: return "Tháp canh";
                case BuildingType.House: return "Nhà ở";
                default: return "Nhà dân làng";
            }
        }

        public static Building Create(BuildingType type, RectInt cells, bool constructing, BuildPlot plot = null, float maxHp = 0f,
            TowerType towerType = TowerType.Archer)
        {
            var go = new GameObject(NameOf(type));
            if (World.Root != null) go.transform.SetParent(World.Root, false);
            var b = go.AddComponent<Building>();
            b.type = type;
            b.cells = cells;
            b.plot = plot;
            b.displayName = type == BuildingType.Tower ? TowerCatalog.Get(towerType).name : NameOf(type);
            b.towerType = towerType;
            go.transform.position = b.Base;

            string spriteKey;
            float feet;
            const string BB = "Tiny Swords/Buildings/Blue Buildings/";
            switch (type)
            {
                case BuildingType.Castle: spriteKey = BB + "Castle"; feet = 1.89f; if (maxHp <= 0) maxHp = 1500; break;
                case BuildingType.Barracks: spriteKey = BB + "Barracks"; feet = 1.83f; maxHp = Balance.BarracksHp; break;
                case BuildingType.Tower: spriteKey = BB + "Tower"; feet = 1.59f; maxHp = Balance.TowerHp; break;
                case BuildingType.House:
                {
                    int v = UnityEngine.Random.Range(1, 4);
                    spriteKey = BB + "House" + v; feet = v == 2 ? 1.28f : 1.2f; maxHp = Balance.HouseHp; break;
                }
                default:
                {
                    int v = (cells.x + cells.y) % 3 + 1;
                    spriteKey = "Tiny Swords/Buildings/Yellow Buildings/House" + v; feet = v == 2 ? 1.28f : 1.2f; maxHp = Balance.VillageHp; break;
                }
            }

            b.ring = Util.NewSprite("Ring", go.transform, Util.Ellipse(true), new Vector3(0, 0.3f, 0), Util.SortOrder(cells.y) - 1);
            b.ring.color = new Color(0.45f, 1f, 0.55f, 0.9f);
            b.ring.transform.localScale = new Vector3(cells.width + 0.8f, (cells.width + 0.8f) * 0.9f, 1);
            b.ring.enabled = false;

            b.sr = Util.NewSprite("Sprite", go.transform, GameArt.I.Spr(spriteKey), new Vector3(0, feet, 0), Util.SortOrder(cells.y));
            b.visualHeight = b.sr.sprite != null ? feet + b.sr.sprite.bounds.extents.y - 0.5f : 2f;

            b.health = go.AddComponent<Health>();
            b.health.building = b;
            b.health.Init(maxHp, type == BuildingType.Castle ? 4f : 2f, Team.Player);
            b.health.OnDied += _ => b.OnDestroyed();
            b.health.OnDamaged += (h, src) => b.OnHit();
            b.bar = HealthBar.Create(b.health, go.transform, Mathf.Min(b.visualHeight + 0.2f, 3.6f), Mathf.Max(1.4f, cells.width * 0.6f));
            if (type == BuildingType.Castle) b.bar.alwaysShow = false;

            if (type == BuildingType.Tower)
            {
                b.tower = go.AddComponent<TowerController>();
                b.tower.Init(b, towerType);
            }

            World.Buildings.Add(b);
            if (World.Nav != null) World.Nav.SetBlocked(cells, true);
            b.DoorPoint = b.ComputeDoor();
            b.rallyPoint = b.DoorPoint + Vector2.down * 1.5f;
            if (World.Nav != null && !World.Nav.IsWalkable(b.rallyPoint)) b.rallyPoint = b.DoorPoint;

            if (constructing)
            {
                b.constructing = true;
                b.buildProgress = 0f;
                b.health.SetCurrent(maxHp * 0.1f);
                b.sr.color = new Color(1, 1, 1, 0.55f);
            }
            if (plot != null) plot.building = b;
            return b;
        }

        Vector2 ComputeDoor()
        {
            var nav = World.Nav;
            Vector2Int c = new Vector2Int(cells.x + cells.width / 2, cells.y - 1);
            if (nav == null) return new Vector2(c.x + 0.5f, c.y + 0.5f);
            var w = nav.NearestWalkable(c, 6);
            return new Vector2(w.x + 0.5f, w.y + 0.5f);
        }

        /// <summary>Ô đi được sát công trình, gần điểm p nhất.</summary>
        public Vector2 ApproachFrom(Vector2 p)
        {
            var nav = World.Nav;
            if (nav == null) return DoorPoint;
            Vector2 best = DoorPoint;
            float bd = float.MaxValue;
            for (int x = cells.xMin - 1; x <= cells.xMax; x++)
                for (int y = cells.yMin - 1; y <= cells.yMax; y++)
                {
                    bool edge = x == cells.xMin - 1 || x == cells.xMax || y == cells.yMin - 1 || y == cells.yMax;
                    if (!edge || !nav.Walkable(x, y)) continue;
                    Vector2 c = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Vector2.Distance(c, p);
                    if (d < bd) { bd = d; best = c; }
                }
            return best;
        }

        /// <summary>Đổi hình chính (tháp đá đổi đế theo cấp) và cập nhật chiều cao hiển thị.</summary>
        public void SetMainSprite(Sprite s, float feet)
        {
            if (s == null) return;
            sr.sprite = s;
            sr.transform.localPosition = new Vector3(0, feet, 0);
            visualHeight = feet + s.bounds.extents.y - 0.3f;
            if (bar != null) bar.SetOffset(feet + s.bounds.extents.y + 0.2f);
        }

        /// <summary>Bán công trình: hoàn tiền, giải phóng ô xây dựng (không nổ).</summary>
        public void Dismantle()
        {
            if (destroyed) return;
            destroyed = true;
            World.Buildings.Remove(this);
            if (World.Nav != null) World.Nav.SetBlocked(cells, false);
            if (plot != null) plot.building = null;
            if (SelectionManager.I != null) SelectionManager.I.OnBuildingDestroyed(this);
            FX.Play("dust2", Base + Vector2.up * 0.4f, 1.4f);
            AudioManager.Play("coin", 0.7f);
            if (bar != null) Destroy(bar.gameObject);
            StartCoroutine(FadeOut());
        }

        public void SetSelected(bool on)
        {
            if (ring != null) ring.enabled = on;
            if (bar != null) bar.forceShow = on;
        }

        void OnHit()
        {
            if (type == BuildingType.Castle)
            {
                AudioManager.Vibrate();
            }
        }

        void Update()
        {
            if (destroyed) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (constructing)
            {
                float rate = (activeWorkers > 0 ? 1f : 0.35f) / Balance.BuildTime;
                buildProgress += rate * dt;
                health.SetCurrent(health.max * (0.1f + 0.9f * Mathf.Clamp01(buildProgress)));
                sr.color = new Color(1, 1, 1, 0.5f + 0.3f * buildProgress);
                dustTimer -= dt;
                if (dustTimer <= 0f)
                {
                    dustTimer = 0.6f;
                    FX.Play("dust", Base + new Vector2(UnityEngine.Random.Range(-0.6f, 0.6f), 0.2f), 0.8f);
                }
                if (buildProgress >= 1f) Complete();
                return;
            }

            if (repairing)
            {
                if (!World.HostileNear(Base, Team.Player, 3f))
                {
                    float rate = health.max * (activeWorkers > 0 ? 0.12f : 0.04f);
                    health.Heal(rate * dt);
                }
                if (health.Ratio >= 0.999f) repairing = false;
            }

            if (queue.Count > 0)
            {
                var def = UnitCatalog.Get(queue[0]);
                trainTimer += dt;
                if (trainTimer >= def.trainTime)
                {
                    trainTimer = 0f;
                    queue.RemoveAt(0);
                    SpawnTrained(def);
                }
            }

            UpdateFires();
        }

        void SpawnTrained(UnitDef def)
        {
            var u = Unit.Spawn(def.id, DoorPoint);
            FX.Play("dust", DoorPoint, 0.8f);
            AudioManager.Play("spawn", 0.5f);
            if (def.id != "pawn")
            {
                Vector2 spread = UnityEngine.Random.insideUnitCircle * 0.8f;
                Vector2 dest = rallyPoint + spread;
                if (World.Nav != null && !World.Nav.IsWalkable(dest)) dest = rallyPoint;
                u.CommandMove(dest, true);
            }
            GameEvents.Message(def.name + " đã sẵn sàng!", MessageKind.Info);
        }

        void Complete()
        {
            constructing = false;
            buildProgress = 1f;
            health.SetCurrent(health.max);
            sr.color = Color.white;
            FX.Play("dust2", Base + Vector2.up * 0.3f, 1.4f);
            AudioManager.Play("build", 0.8f);
            GameEvents.Message(displayName + " đã xây xong!", MessageKind.Good);
            AnyCompleted?.Invoke(this);
        }

        void UpdateFires()
        {
            float r = health.Ratio;
            SetFire(0, r < 0.5f, new Vector3(-cells.width * 0.2f, visualHeight * 0.45f, 0));
            SetFire(1, r < 0.25f, new Vector3(cells.width * 0.22f, visualHeight * 0.3f, 0));
        }

        void SetFire(int i, bool on, Vector3 local)
        {
            if (on && fires[i] == null)
                fires[i] = FX.Loop(i == 0 ? "fire" : "fire2", transform, local, 1f, sr.sortingOrder + 2);
            else if (!on && fires[i] != null) { Destroy(fires[i]); fires[i] = null; }
        }

        public void Upgrade()
        {
            level++;
            FX.Play("dust2", Base + Vector2.up * 0.4f, 1.2f);
            AudioManager.Play("build", 0.8f);
            health.max *= 1.25f;
            health.SetCurrent(health.max);
            if (tower != null) tower.OnUpgrade();
        }

        void OnDestroyed()
        {
            if (destroyed) return;
            destroyed = true;
            World.Buildings.Remove(this);
            if (World.Nav != null) World.Nav.SetBlocked(cells, false);
            if (plot != null) plot.building = null;
            if (SelectionManager.I != null) SelectionManager.I.OnBuildingDestroyed(this);
            for (int i = 0; i < 3; i++)
            {
                Vector2 off = new Vector2(UnityEngine.Random.Range(-cells.width * 0.35f, cells.width * 0.35f), UnityEngine.Random.Range(0.2f, visualHeight * 0.6f));
                FX.Play(i == 0 ? "explosion2" : "explosion", Base + off, 1f);
            }
            AudioManager.Play("explode", 1f);
            AnyDestroyed?.Invoke(this);
            if (bar != null) Destroy(bar.gameObject);
            StartCoroutine(FadeOut());
        }

        System.Collections.IEnumerator FadeOut()
        {
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime;
                foreach (var r in GetComponentsInChildren<SpriteRenderer>())
                    if (r != ring) r.color = new Color(0.5f, 0.45f, 0.4f, 1f - t);
                yield return null;
            }
            Destroy(gameObject);
        }

        public static void ResetEvents()
        {
            AnyDestroyed = null;
            AnyCompleted = null;
        }
    }
}
