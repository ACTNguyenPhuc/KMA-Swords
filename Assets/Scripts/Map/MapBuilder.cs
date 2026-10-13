using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Oath
{
    /// <summary>
    /// Dựng bản đồ từ MapData bằng Tilemap Tiny Swords: nước, bọt sóng, mặt cỏ (tự nối viền 16 trường hợp),
    /// đường đi, cao nguyên + vách đá, trang trí, tài nguyên và công trình.
    /// </summary>
    public static class MapBuilder
    {
        // Bảng chọn ô theo mặt nạ hàng xóm (bit: Bắc=1, Nam=2, Đông=4, Tây=8)
        static readonly int[] FlatTable = { 27, 19, 3, 11, 24, 16, 0, 8, 26, 18, 2, 10, 25, 17, 1, 9 };
        static readonly int[] HighTable = { 31, 23, 7, 15, 28, 20, 4, 12, 30, 22, 6, 14, 29, 21, 5, 13 };

        static readonly Dictionary<Sprite, Tile> tileCache = new Dictionary<Sprite, Tile>();

        static readonly float[] BushFeet = { 0.23f, 0.19f, 0.31f, 0.23f };
        static readonly float[] RockFeet = { 0.30f, 0.33f, 0.31f, 0.38f };
        static readonly float[] WaterRockFeet = { 0.25f, 0.28f, 0.25f, 0.31f };

        public static Tile TileFor(Sprite s)
        {
            if (s == null) return null;
            if (tileCache.TryGetValue(s, out var t) && t != null) return t;
            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = s;
            t.colliderType = Tile.ColliderType.None;
            tileCache[s] = t;
            return t;
        }

        static Tilemap NewLayer(Transform grid, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid, false);
            var tm = go.AddComponent<Tilemap>();
            var r = go.AddComponent<TilemapRenderer>();
            r.sortingOrder = order;
            r.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Manual;
            r.chunkCullingBounds = new Vector3(2f, 2f, 0f);
            return tm;
        }

        /// <summary>Dựng phần hình ảnh địa hình + trang trí. Trả về gốc chứa bản đồ.</summary>
        public static Transform BuildTerrain(MapData m, int groundColor, int roadColor, Transform parent, int waterPad = 16)
        {
            var root = new GameObject("Map").transform;
            root.SetParent(parent, false);
            var grid = root.gameObject.AddComponent<Grid>();
            grid.cellSize = new Vector3(1, 1, 0);

            var art = GameArt.I;
            var water = NewLayer(root, "Water", Util.OrderWater);
            var foam = NewLayer(root, "Foam", Util.OrderFoam);
            var ground = NewLayer(root, "Ground", Util.OrderGround);
            var roadTm = NewLayer(root, "Road", Util.OrderRoad);
            var high = NewLayer(root, "Plateau", Util.OrderPlateau);

            // Nước
            var waterTile = art.Tile("water");
            var bounds = new BoundsInt(-waterPad, -waterPad, 0, m.W + waterPad * 2, m.H + waterPad * 2, 1);
            var wt = new TileBase[bounds.size.x * bounds.size.y];
            for (int i = 0; i < wt.Length; i++) wt[i] = waterTile;
            water.SetTilesBlock(bounds, wt);

            var groundSprites = art.Set("Tiny Swords/Terrain/Tileset/Tilemap_color" + Mathf.Clamp(groundColor, 1, 5));
            var roadSprites = art.Set("Tiny Swords/Terrain/Tileset/Tilemap_color" + Mathf.Clamp(roadColor, 1, 5));
            var foamTile = art.Tile("foam");

            for (int x = 0; x < m.W; x++)
                for (int y = 0; y < m.H; y++)
                {
                    if (!m.land[x, y]) continue;
                    var pos = new Vector3Int(x, y, 0);
                    int mask = (m.Land(x, y + 1) ? 1 : 0) | (m.Land(x, y - 1) ? 2 : 0) | (m.Land(x + 1, y) ? 4 : 0) | (m.Land(x - 1, y) ? 8 : 0);
                    if (groundSprites.Length > 40) ground.SetTile(pos, TileFor(groundSprites[FlatTable[mask]]));

                    bool shore = false;
                    for (int dx = -1; dx <= 1 && !shore; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            if (!m.Land(x + dx, y + dy)) { shore = true; break; }
                    if (shore && foamTile != null) foam.SetTile(pos, foamTile);

                    if (m.road[x, y] && roadSprites.Length > 40)
                    {
                        int rm = (m.Road(x, y + 1) ? 1 : 0) | (m.Road(x, y - 1) ? 2 : 0) | (m.Road(x + 1, y) ? 4 : 0) | (m.Road(x - 1, y) ? 8 : 0);
                        roadTm.SetTile(pos, TileFor(roadSprites[FlatTable[rm]]));
                    }

                    if (m.plateau[x, y] && groundSprites.Length > 40)
                    {
                        int pm = (m.Plateau(x, y + 1) ? 1 : 0) | (m.Plateau(x, y - 1) ? 2 : 0) | (m.Plateau(x + 1, y) ? 4 : 0) | (m.Plateau(x - 1, y) ? 8 : 0);
                        high.SetTile(pos, TileFor(groundSprites[HighTable[pm]]));
                    }
                    if (m.cliff[x, y] && groundSprites.Length > 40)
                    {
                        bool l = m.In(x - 1, y) && m.cliff[x - 1, y];
                        bool r = m.In(x + 1, y) && m.cliff[x + 1, y];
                        int idx = l && r ? 35 : (l ? 36 : (r ? 34 : 37));
                        high.SetTile(pos, TileFor(groundSprites[idx]));
                    }
                }
            roadTm.color = new Color(1f, 0.97f, 0.88f, 1f);

            BuildDecorations(m, root);
            return root;
        }

        static void BuildDecorations(MapData m, Transform root)
        {
            var deco = new GameObject("Decorations").transform;
            deco.SetParent(root, false);
            var rnd = new System.Random(m.W * 31 + m.H);

            foreach (var c in m.bushes)
            {
                int v = rnd.Next(1, 5);
                var p = new Vector2(c.x + 0.5f + (float)(rnd.NextDouble() - 0.5) * 0.3f, c.y + 0.35f);
                var sr = Util.NewSprite("Bush", deco, null, p + new Vector2(0, BushFeet[v - 1]), Util.SortOrder(p.y));
                var a = sr.gameObject.AddComponent<SpriteAnimator>();
                a.target = sr;
                a.Play(GameArt.I.Set("Tiny Swords/Terrain/Decorations/Bushes/Bush " + v), 8f, true, true, null, true);
            }
            foreach (var c in m.rocks)
            {
                int v = rnd.Next(1, 5);
                var p = new Vector2(c.x + 0.5f, c.y + 0.4f);
                Util.NewSprite("Rock", deco, GameArt.I.Spr("Tiny Swords/Terrain/Decorations/Rocks/Rock" + v), p + new Vector2(0, RockFeet[v - 1]), Util.SortOrder(p.y));
            }
            foreach (var c in m.waterRocks)
            {
                int v = rnd.Next(1, 5);
                var p = new Vector2(c.x + 0.5f, c.y + 0.4f);
                var sr = Util.NewSprite("WaterRock", deco, null, p + new Vector2(0, WaterRockFeet[v - 1]), Util.OrderFoam + 5);
                var a = sr.gameObject.AddComponent<SpriteAnimator>();
                a.target = sr;
                a.Play(GameArt.I.Set("Tiny Swords/Terrain/Decorations/Rocks in the Water/Water Rocks_0" + v), 8f, true, true, null, true);
            }
            foreach (var c in m.ducks)
            {
                var sr = Util.NewSprite("RubberDuck", deco, null, new Vector3(c.x + 0.5f, c.y + 0.5f, 0), Util.OrderFoam + 6);
                var a = sr.gameObject.AddComponent<SpriteAnimator>();
                a.target = sr;
                a.Play(GameArt.I.Set("Tiny Swords/Terrain/Decorations/Rubber Duck/Rubber duck"), 4f, true, true, null, true);
                sr.gameObject.AddComponent<Bobber>();
            }
            foreach (var c in m.sheep)
            {
                var p = new Vector2(c.x + 0.5f, c.y + 0.4f);
                var go = new GameObject("Sheep");
                go.transform.SetParent(deco, false);
                go.transform.position = p;
                go.AddComponent<Sheep>();
            }
        }

        /// <summary>Tạo tài nguyên, công trình, ô xây dựng và lưới dẫn đường cho màn chơi.</summary>
        public static void BuildGameplay(MapData m, LevelConfig cfg)
        {
            World.Map = m;
            World.Nav = new NavGrid(m);
            var rnd = new System.Random(cfg != null ? cfg.levelIndex * 97 : 7);

            foreach (var c in m.trees) ResourceNode.CreateTree(c, rnd.Next(1, 5));
            foreach (var c in m.gold) ResourceNode.CreateGold(c, rnd.Next(3, 7));

            foreach (var r in m.plots) BuildPlot.Create(r);
            foreach (var r in m.prebuiltTowers)
            {
                var plot = BuildPlot.Create(r);
                var t = Building.Create(BuildingType.Tower, r, false, plot);
                t.totalSpent = TowerCatalog.Get(TowerType.Archer).wood[0] + TowerCatalog.Get(TowerType.Archer).gold[0];
            }
            foreach (var r in m.villages) Building.Create(BuildingType.Village, r, false);

            if (m.barracks.width > 0) Building.Create(BuildingType.Barracks, m.barracks, false);
            if (m.castle.width > 0)
            {
                World.Castle = Building.Create(BuildingType.Castle, m.castle, false, null, cfg != null ? cfg.castleHp : 1500);
                if (m.rally.x >= 0)
                {
                    Vector2 rally = MapData.CellCenter(m.rally);
                    World.Castle.rallyPoint = rally;
                    foreach (var b in World.Buildings) if (b.type == BuildingType.Barracks) b.rallyPoint = rally;
                }
            }
        }
    }

    /// <summary>Nhấp nhô nhẹ (vịt cao su trên mặt nước).</summary>
    public class Bobber : MonoBehaviour
    {
        Vector3 basePos;
        float phase;
        void Start() { basePos = transform.localPosition; phase = Random.value * 6f; }
        void Update() { transform.localPosition = basePos + new Vector3(0, Mathf.Sin(Time.time * 2f + phase) * 0.05f, 0); }
    }

    /// <summary>Cừu trang trí: lúc đứng, lúc gặm cỏ, thỉnh thoảng đi loanh quanh.</summary>
    public class Sheep : MonoBehaviour
    {
        const string S = "Tiny Swords/Pawn and Resources/Meat/Sheep/Sheep_";
        SpriteRenderer sr;
        SpriteAnimator anim;
        Vector2 home, target;
        float timer;
        bool walking;

        void Start()
        {
            home = transform.position;
            target = home;
            var shadow = Util.NewSprite("Shadow", transform, Util.Ellipse(false), Vector3.zero, Util.SortOrder(home.y) - 1);
            shadow.color = new Color(0, 0, 0, 0.22f);
            shadow.transform.localScale = new Vector3(0.7f, 0.7f, 1);
            sr = Util.NewSprite("Sprite", transform, null, new Vector3(0, 0.31f, 0), Util.SortOrder(home.y));
            anim = sr.gameObject.AddComponent<SpriteAnimator>();
            anim.target = sr;
            anim.Play(GameArt.I.Set(S + "Idle"), 8f, true, true, null, true);
            sr.flipX = Random.value < 0.5f;
            timer = Random.Range(1f, 4f);
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (walking)
            {
                Vector2 p = transform.position;
                Vector2 np = Vector2.MoveTowards(p, target, Time.deltaTime * 0.6f);
                if (World.Nav != null && !World.Nav.IsWalkable(np)) { walking = false; timer = 1f; }
                else transform.position = np;
                sr.sortingOrder = Util.SortOrder(transform.position.y);
                if (Vector2.Distance(np, target) < 0.02f || timer <= 0f)
                {
                    walking = false;
                    timer = Random.Range(2f, 5f);
                    anim.Play(GameArt.I.Set(S + (Random.value < 0.6f ? "Grass" : "Idle")), 8f, true);
                }
                return;
            }
            if (timer > 0f) return;
            target = home + Random.insideUnitCircle * 1.2f;
            sr.flipX = target.x < transform.position.x;
            walking = true;
            timer = 3f;
            anim.Play(GameArt.I.Set(S + "Move"), 8f, true);
        }
    }
}
