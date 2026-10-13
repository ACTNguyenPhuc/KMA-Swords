using System.Collections.Generic;
using UnityEngine;

namespace Oath
{
    public enum Team { Player, Enemy }
    public enum AttackKind { Melee, Ranged, Heal, Explode, None }
    public enum VisualKind { TinySwords, Sprite, Prefab }

    /// <summary>Thông số thiết kế của một loại đơn vị (không phải thông số gốc của asset).</summary>
    public class UnitDef
    {
        public string id, name, role;
        public Team team;
        public float hp = 100, damage = 10, range = 0.9f, cooldown = 1f, speed = 1.5f, armor, aggro = 3.2f;
        public AttackKind attack = AttackKind.Melee;
        public string projectile;               // "arrow" | "orb" | null
        public Color projectileColor = Color.white;
        public float splash;                    // bán kính sát thương lan
        public float slow;                      // hệ số giảm tốc gây ra cho mục tiêu (0..1)
        public float buildingMul = 1f;          // hệ số sát thương lên công trình
        public int costGold, costWood, bounty, pop = 1;
        public float trainTime = 4f;
        public bool boss;
        public bool flying;                     // quái bay: tháp đá/búa không đánh được
        public string deathSpawn; public int deathSpawnCount;
        public int avatar = 1;

        // Hình ảnh
        public VisualKind visual = VisualKind.TinySwords;
        public string idle, run, heal;
        public string[] attacks;
        public string sprite;                  // khoá sprite tĩnh hoặc prefab
        public bool facesRight = true;
        public float height = 1.1f;            // chiều cao hiển thị (đơn vị thế giới) cho Sprite/Prefab
        public float scale = 1f;               // tỉ lệ cho TinySwords
        public Color tint = Color.white;
        public float bodyRadius = 0.32f;

        public bool IsRanged => attack == AttackKind.Ranged || attack == AttackKind.Heal;
    }

    public static class UnitCatalog
    {
        static Dictionary<string, UnitDef> defs;

        public static UnitDef Get(string id)
        {
            if (defs == null) Build();
            if (defs.TryGetValue(id, out var d)) return d;
            Debug.LogError("Không có đơn vị: " + id);
            return defs["red_warrior"];
        }

        public static IEnumerable<UnitDef> All
        {
            get { if (defs == null) Build(); return defs.Values; }
        }

        static void Add(UnitDef d) { defs[d.id] = d; }

        const string U = "Tiny Swords/Units/";
        const string LL = "Layer Lab/2D Characters-CasualMonsters/PNG/All/";
        const string GR = "PotionJunkies/7Enemies/Texture/";
        const string MOB = "OldStyleCartoonMobsPack1/Prefabs/";

        static UnitDef TS(string id, string name, Team team, string color, string kind)
        {
            var d = new UnitDef { id = id, name = name, team = team, visual = VisualKind.TinySwords };
            string b = U + color + " Units/" + kind + "/";
            switch (kind)
            {
                case "Warrior":
                    d.idle = b + "Warrior_Idle"; d.run = b + "Warrior_Run";
                    d.attacks = new[] { b + "Warrior_Attack1", b + "Warrior_Attack2" };
                    break;
                case "Archer":
                    d.idle = b + "Archer_Idle"; d.run = b + "Archer_Run";
                    d.attacks = new[] { b + "Archer_Shoot" };
                    break;
                case "Lancer":
                    d.idle = b + "Lancer_Idle"; d.run = b + "Lancer_Run";
                    // thứ tự: phải, lên-phải, xuống-phải, lên, xuống
                    d.attacks = new[] { b + "Lancer_Right_Attack", b + "Lancer_UpRight_Attack", b + "Lancer_DownRight_Attack", b + "Lancer_Up_Attack", b + "Lancer_Down_Attack" };
                    break;
                case "Monk":
                    d.idle = b + "Idle"; d.run = b + "Run"; d.heal = b + "Heal";
                    d.attacks = new[] { b + "Heal" };
                    break;
            }
            return d;
        }

        static UnitDef Spr(string id, string name, string sprite, float height, bool facesRight = true)
        {
            return new UnitDef { id = id, name = name, team = Team.Enemy, visual = VisualKind.Sprite, sprite = sprite, height = height, facesRight = facesRight };
        }

        static UnitDef Pre(string id, string name, string prefab, float height, bool facesRight = true)
        {
            return new UnitDef { id = id, name = name, team = Team.Enemy, visual = VisualKind.Prefab, sprite = prefab, height = height, facesRight = facesRight };
        }

        static void Build()
        {
            defs = new Dictionary<string, UnitDef>();

            // ===================== QUÂN LAM (người chơi) =====================
            var pawn = new UnitDef
            {
                id = "pawn", name = "Thợ", role = "Thu thập gỗ, vàng; xây và sửa công trình", team = Team.Player,
                visual = VisualKind.TinySwords, hp = 55, speed = 2.4f, attack = AttackKind.None, costGold = 30, trainTime = 3f, avatar = 4,
                idle = "Tiny Swords/Pawn and Resources/Pawn/Blue Pawn/Pawn_Idle",
                run = "Tiny Swords/Pawn and Resources/Pawn/Blue Pawn/Pawn_Run",
            };
            Add(pawn);

            var w = TS("warrior", "Kiếm sĩ", Team.Player, "Blue", "Warrior");
            w.role = "Cận chiến, sát thương tốt"; w.hp = 150; w.damage = 16; w.range = 0.95f; w.cooldown = 0.9f; w.speed = 2.2f; w.armor = 1;
            w.costGold = 50; w.trainTime = 4f; w.aggro = 3.6f; w.avatar = 2; Add(w);

            var l = TS("lancer", "Thương binh", Team.Player, "Blue", "Lancer");
            l.role = "Giữ phòng tuyến, làm chậm địch"; l.hp = 240; l.damage = 11; l.range = 1.3f; l.cooldown = 1.1f; l.speed = 1.6f; l.armor = 3;
            l.slow = 0.5f; l.costGold = 65; l.costWood = 10; l.trainTime = 5f; l.aggro = 3.0f; l.avatar = 3; Add(l);

            var a = TS("archer", "Cung thủ", Team.Player, "Blue", "Archer");
            a.role = "Tấn công từ xa, cần đứng sau tuyến cận chiến"; a.hp = 75; a.damage = 12; a.range = 4.8f; a.cooldown = 1.15f; a.speed = 2.0f;
            a.attack = AttackKind.Ranged; a.projectile = "arrow"; a.costGold = 55; a.costWood = 15; a.trainTime = 4.5f; a.aggro = 5.2f; a.avatar = 5; Add(a);

            var m = TS("monk", "Tu sĩ", Team.Player, "Blue", "Monk");
            m.role = "Hồi máu cho đồng minh trong bán kính"; m.hp = 85; m.damage = 16; m.range = 3.6f; m.cooldown = 1.6f; m.speed = 2.0f;
            m.attack = AttackKind.Heal; m.costGold = 80; m.trainTime = 6f; m.aggro = 4.5f; m.avatar = 4; Add(m);

            // ===================== QUÂN CỜ ĐỎ =====================
            var rw = TS("red_warrior", "Kiếm sĩ Cờ Đỏ", Team.Enemy, "Red", "Warrior");
            rw.hp = 85; rw.damage = 9; rw.cooldown = 1f; rw.speed = 1.45f; rw.bounty = 7; rw.avatar = 7; Add(rw);

            var rl = TS("red_lancer", "Thương binh Cờ Đỏ", Team.Enemy, "Red", "Lancer");
            rl.hp = 160; rl.damage = 9; rl.range = 1.3f; rl.cooldown = 1.2f; rl.speed = 1.15f; rl.armor = 2; rl.bounty = 10; rl.avatar = 8; Add(rl);

            var ra = TS("red_archer", "Cung thủ Cờ Đỏ", Team.Enemy, "Red", "Archer");
            ra.hp = 55; ra.damage = 8; ra.range = 4.2f; ra.cooldown = 1.4f; ra.speed = 1.4f; ra.attack = AttackKind.Ranged; ra.projectile = "arrow";
            ra.aggro = 4.4f; ra.bounty = 8; ra.avatar = 10; Add(ra);

            var rm = TS("red_monk", "Tu sĩ Cờ Đỏ", Team.Enemy, "Red", "Monk");
            rm.hp = 75; rm.damage = 10; rm.range = 3.4f; rm.cooldown = 1.8f; rm.speed = 1.35f; rm.attack = AttackKind.Heal; rm.bounty = 12; rm.avatar = 9; Add(rm);

            var veyr = TS("veyr", "Tướng Veyr", Team.Enemy, "Red", "Warrior");
            veyr.hp = 1700; veyr.damage = 30; veyr.range = 1.25f; veyr.cooldown = 1.1f; veyr.speed = 1.3f; veyr.armor = 5; veyr.splash = 1.3f;
            veyr.scale = 1.6f; veyr.tint = new Color(1f, 0.82f, 0.62f); veyr.boss = true; veyr.bounty = 200; veyr.aggro = 3.5f; veyr.avatar = 6;
            veyr.bodyRadius = 0.5f; Add(veyr);

            // ===================== QUÁI VẬT: 2D Casual Monsters (Layer Lab) =====================
            var g = Spr("goblin", "Yêu tinh", LL + "goblin/goblin", 0.85f);
            g.hp = 45; g.damage = 6; g.speed = 2.1f; g.cooldown = 0.8f; g.bounty = 4; Add(g);
            var ga = Spr("goblin_archer", "Yêu tinh cung thủ", LL + "goblin/goblin_archer", 0.95f);
            ga.hp = 42; ga.damage = 7; ga.range = 3.8f; ga.cooldown = 1.3f; ga.speed = 1.8f; ga.attack = AttackKind.Ranged; ga.projectile = "arrow"; ga.aggro = 4f; ga.bounty = 6; Add(ga);
            var gw = Spr("goblin_warrior", "Yêu tinh chiến binh", LL + "goblin/goblin_warrior", 1.05f);
            gw.hp = 100; gw.damage = 11; gw.armor = 1; gw.speed = 1.6f; gw.bounty = 9; Add(gw);
            var gg = Spr("goblin_green", "Yêu tinh rừng", LL + "goblin_green/goblin", 0.85f);
            gg.hp = 65; gg.damage = 8; gg.speed = 2.2f; gg.cooldown = 0.75f; gg.bounty = 5; Add(gg);
            var gga = Spr("goblin_green_archer", "Yêu tinh rừng cung thủ", LL + "goblin_green/goblin_archer", 0.95f);
            gga.hp = 55; gga.damage = 9; gga.range = 4f; gga.cooldown = 1.25f; gga.speed = 1.8f; gga.attack = AttackKind.Ranged; gga.projectile = "arrow"; gga.aggro = 4.2f; gga.bounty = 7; Add(gga);
            var ggw = Spr("goblin_green_warrior", "Yêu tinh rừng chiến binh", LL + "goblin_green/goblin_warrior", 1.05f);
            ggw.hp = 140; ggw.damage = 13; ggw.armor = 2; ggw.speed = 1.6f; ggw.bounty = 11; Add(ggw);

            var s = Spr("slime", "Chất nhờn", LL + "green_slime/slime", 0.7f);
            s.hp = 55; s.damage = 6; s.speed = 1.1f; s.bounty = 4; Add(s);
            var ss = Spr("slime_stone", "Chất nhờn đá", LL + "green_slime/stone_slime", 0.8f);
            ss.hp = 190; ss.damage = 9; ss.armor = 3; ss.speed = 0.9f; ss.bounty = 10; Add(ss);
            var sk = Spr("slime_king", "Vua Chất Nhờn", LL + "green_slime/slime_king", 1.6f);
            sk.hp = 650; sk.damage = 18; sk.armor = 2; sk.speed = 0.85f; sk.splash = 1f; sk.bounty = 60; sk.boss = true;
            sk.deathSpawn = "slime"; sk.deathSpawnCount = 3; sk.bodyRadius = 0.55f; Add(sk);
            var so = Spr("slime_fire", "Chất nhờn lửa", LL + "orange_slime/slime", 0.75f);
            so.hp = 90; so.damage = 10; so.speed = 1.25f; so.bounty = 6; Add(so);
            var sos = Spr("slime_fire_stone", "Chất nhờn đá lửa", LL + "orange_slime/stone_slime", 0.85f);
            sos.hp = 260; sos.damage = 12; sos.armor = 4; sos.speed = 0.95f; sos.bounty = 13; Add(sos);
            var sok = Spr("slime_fire_king", "Vua Chất Nhờn Lửa", LL + "orange_slime/slime_king", 1.7f);
            sok.hp = 900; sok.damage = 22; sok.armor = 3; sok.speed = 0.85f; sok.splash = 1.1f; sok.bounty = 80; sok.boss = true;
            sok.deathSpawn = "slime_fire"; sok.deathSpawnCount = 3; sok.bodyRadius = 0.55f; Add(sok);

            var sku = Spr("skull", "Bộ xương", LL + "skull/skull", 1.0f);
            sku.hp = 70; sku.damage = 9; sku.speed = 1.5f; sku.bounty = 6; Add(sku);
            var ska = Spr("skull_archer", "Xương cung thủ", LL + "skull/skull_archer", 1.05f);
            ska.hp = 55; ska.damage = 9; ska.range = 4.2f; ska.cooldown = 1.35f; ska.speed = 1.4f; ska.attack = AttackKind.Ranged; ska.projectile = "arrow"; ska.aggro = 4.4f; ska.bounty = 8; Add(ska);
            var skw = Spr("skull_warrior", "Xương khiên", LL + "skull/skull_warrior", 1.1f);
            skw.hp = 160; skw.damage = 11; skw.armor = 4; skw.speed = 1.2f; skw.bounty = 12; Add(skw);
            var skp = Spr("skull_poison", "Xương độc", LL + "skull_posion/skull", 1.0f);
            skp.hp = 100; skp.damage = 12; skp.speed = 1.6f; skp.bounty = 8; Add(skp);
            var skpa = Spr("skull_poison_archer", "Xương độc cung thủ", LL + "skull_posion/skull_archer", 1.05f);
            skpa.hp = 80; skpa.damage = 12; skpa.range = 4.4f; skpa.cooldown = 1.3f; skpa.speed = 1.4f; skpa.attack = AttackKind.Ranged; skpa.projectile = "orb";
            skpa.projectileColor = new Color(0.55f, 1f, 0.4f); skpa.aggro = 4.6f; skpa.bounty = 10; Add(skpa);
            var skpw = Spr("skull_poison_warrior", "Xương độc khiên", LL + "skull_posion/skull_warrior", 1.1f);
            skpw.hp = 230; skpw.damage = 14; skpw.armor = 5; skpw.speed = 1.2f; skpw.bounty = 15; Add(skpw);

            // ===================== QUÁI VẬT: Old Style Cartoon Mobs =====================
            var m1 = Pre("duck", "Vịt Chiến", MOB + "Pre-Mobs-1/Mob-1-1", 1.0f);
            m1.hp = 70; m1.damage = 8; m1.speed = 1.8f; m1.bounty = 6; Add(m1);
            var m1b = Pre("duck_elite", "Vịt Chiến Tinh Nhuệ", MOB + "Pre-Mobs-1/Mob-1-4", 1.05f);
            m1b.hp = 130; m1b.damage = 12; m1b.armor = 2; m1b.speed = 1.8f; m1b.bounty = 10; Add(m1b);
            var m2 = Pre("mushroom", "Nấm Độc", MOB + "Pre-Mobs-2/Mob-2-1", 1.15f);
            m2.hp = 200; m2.damage = 10; m2.armor = 2; m2.speed = 0.95f; m2.bounty = 11; Add(m2);
            var m2b = Pre("mushroom_elite", "Nấm Độc Tím", MOB + "Pre-Mobs-2/Mob-2-4", 1.2f);
            m2b.hp = 300; m2b.damage = 14; m2b.armor = 3; m2b.speed = 0.95f; m2b.bounty = 15; Add(m2b);
            var m3 = Pre("eggplant", "Phù Thủy Cà Tím", MOB + "Pre-Mobs-3/Mob-3-1", 1.1f);
            m3.hp = 90; m3.damage = 12; m3.range = 4f; m3.cooldown = 1.5f; m3.speed = 1.3f; m3.attack = AttackKind.Ranged; m3.projectile = "orb";
            m3.projectileColor = new Color(0.8f, 0.5f, 1f); m3.aggro = 4.3f; m3.bounty = 10; Add(m3);
            var m4 = Pre("bomb", "Bom Biết Đi", MOB + "Pre-Mobs-4/Mob-4-1", 0.95f);
            m4.hp = 60; m4.damage = 70; m4.range = 0.8f; m4.speed = 1.9f; m4.attack = AttackKind.Explode; m4.splash = 1.6f; m4.buildingMul = 1.6f; m4.bounty = 9; Add(m4);
            var m5 = Pre("boar", "Lợn Rừng Cầm Chùy", MOB + "Pre-Mobs-5/Mob-5-1", 1.25f);
            m5.hp = 210; m5.damage = 16; m5.armor = 2; m5.speed = 1.35f; m5.bounty = 14; Add(m5);
            var m5b = Pre("boar_elite", "Lợn Rừng Đầu Đàn", MOB + "Pre-Mobs-5/Mob-5-3", 1.35f);
            m5b.hp = 320; m5b.damage = 20; m5b.armor = 3; m5b.speed = 1.35f; m5b.splash = 0.8f; m5b.bounty = 18; Add(m5b);
            var m6 = Pre("spiderling", "Bọ Gai", MOB + "Pre-Mobs-6/Mob-6-1", 1.0f);
            m6.hp = 110; m6.damage = 10; m6.speed = 1.7f; m6.bounty = 8; Add(m6);

            // ===================== QUÁI VẬT: Greek/Fantasy Enemies =====================
            var wolf = Spr("wolf", "Sói Xám", GR + "07_Wolf", 1.15f);
            wolf.hp = 80; wolf.damage = 9; wolf.speed = 2.7f; wolf.cooldown = 0.8f; wolf.bounty = 7; Add(wolf);
            var horse = Spr("hellhorse", "Ngựa Địa Ngục", GR + "05_Hellhourse", 1.5f);
            horse.hp = 260; horse.damage = 15; horse.armor = 2; horse.speed = 2.2f; horse.bounty = 18; horse.bodyRadius = 0.45f; Add(horse);
            var arachne = Spr("arachne", "Nữ Nhện Arachne", GR + "01_Arachne", 1.6f);
            arachne.hp = 340; arachne.damage = 13; arachne.range = 3.8f; arachne.cooldown = 1.4f; arachne.speed = 1.2f; arachne.attack = AttackKind.Ranged;
            arachne.projectile = "orb"; arachne.projectileColor = new Color(0.9f, 0.9f, 1f); arachne.slow = 0.55f; arachne.aggro = 4.2f; arachne.bounty = 25; arachne.bodyRadius = 0.5f; Add(arachne);
            var fury = Spr("fury", "Quỷ Fury", GR + "04_FuryDemon", 1.6f);
            fury.hp = 210; fury.damage = 17; fury.speed = 2.0f; fury.armor = 1; fury.bounty = 20; fury.flying = true; Add(fury);
            var centaur = Spr("centaur", "Nhân Mã Chiron", GR + "03_ChironCentaur", 1.7f);
            centaur.hp = 280; centaur.damage = 16; centaur.range = 4.6f; centaur.cooldown = 1.4f; centaur.speed = 1.5f; centaur.attack = AttackKind.Ranged;
            centaur.projectile = "orb"; centaur.projectileColor = new Color(0.5f, 1f, 0.5f); centaur.aggro = 4.8f; centaur.bounty = 25; centaur.bodyRadius = 0.45f; Add(centaur);
            var lion = Spr("lion", "Sư Tử Nemea", GR + "06_NemianLion", 1.9f);
            lion.hp = 1400; lion.damage = 28; lion.armor = 7; lion.speed = 1.3f; lion.splash = 1.1f; lion.boss = true; lion.bounty = 120; lion.bodyRadius = 0.6f; Add(lion);
            var chimera = Spr("chimera", "Chimera", GR + "02_Chimera", 2.3f);
            chimera.hp = 2500; chimera.damage = 34; chimera.armor = 5; chimera.speed = 1.15f; chimera.splash = 1.5f; chimera.range = 1.3f; chimera.boss = true;
            chimera.bounty = 250; chimera.bodyRadius = 0.75f; Add(chimera);
        }
    }

    /// <summary>Thông số công trình và kinh tế.</summary>
    public static class Balance
    {
        public const int TowerWood = 70, TowerGold = 20;
        public const int HouseWood = 60, HouseGold = 0;
        public const int HousePopulation = 5;
        public static readonly int[] TowerUpgradeWood = { 0, 70, 110 };
        public static readonly int[] TowerUpgradeGold = { 0, 60, 110 };
        public static readonly float[] TowerDamage = { 14f, 21f, 28f };
        public static readonly float[] TowerRange = { 5.2f, 5.8f, 6.4f };
        public static readonly float[] TowerCooldown = { 1.05f, 0.85f, 0.7f };
        public const float TowerHp = 420f, HouseHp = 260f, BarracksHp = 700f, VillageHp = 300f;
        public const float BuildTime = 3f;
        public const int GatherAmount = 10;
        public const float GatherTime = 1.8f;
        public const float PassiveGoldInterval = 2f;
        public const int PassiveGold = 1;
    }
}
