using System.Collections.Generic;
using UnityEngine;

namespace Oath
{
    public enum TowerType { Archer, Catapult, Iron, Fire, Quake }
    public enum TargetMode { First, Strongest, Nearest }

    /// <summary>Thông số một loại tháp qua 3 cấp. Hình ảnh tháp đá lấy từ gói Stone Tower.</summary>
    public class TowerDef
    {
        public TowerType type;
        public string name, shortName, desc;
        public int[] wood = new int[3], gold = new int[3];     // [0] = giá xây, [1],[2] = giá nâng cấp
        public float[] damage = new float[3], range = new float[3], cooldown = new float[3];
        public float[] splash = new float[3];                   // bán kính sát thương lan
        public float[] burnDps = new float[3];                  // thiêu đốt theo giây
        public float[] slow = new float[3];                     // giảm tốc (0..1)
        public float[] stun = new float[3];                     // choáng (giây)
        public bool pierce;                                     // bỏ qua giáp
        public bool hitsAir = true;
        public bool aroundSelf;                                 // nện xung quanh (không bắn đạn)
        public string[] baseSprite = new string[3];
        public string[] topSprite = new string[3];              // thanh ném / búa
        public string[] topFireSprite = new string[3];          // thanh ném khi kéo căng
        public string[] projectile = new string[3];
        public float projectileScale = 0.5f;
        public string impactFx;                                 // "rock" | "rockBig" | "iron" | "fire" | "quake"
        public string icon;

        public int TotalSpent(int level)
        {
            int s = 0;
            for (int i = 0; i < level && i < 3; i++) s += wood[i] + gold[i];
            return s;
        }
    }

    public static class TowerCatalog
    {
        public const string ST = "Stone Tower/PNG/";
        public const float StonePPU = 70f;
        static Dictionary<TowerType, TowerDef> defs;

        public static readonly TowerType[] All = { TowerType.Archer, TowerType.Catapult, TowerType.Iron, TowerType.Fire, TowerType.Quake };

        public static TowerDef Get(TowerType t)
        {
            if (defs == null) Build();
            return defs[t];
        }

        static string S(int n) => ST + n;

        static void Build()
        {
            defs = new Dictionary<TowerType, TowerDef>();

            defs[TowerType.Archer] = new TowerDef
            {
                type = TowerType.Archer, name = "Tháp Cung", shortName = "Cung",
                desc = "Bắn nhanh từng mục tiêu, bắn được quái bay.",
                wood = new[] { 70, 70, 110 }, gold = new[] { 20, 60, 110 },
                damage = new[] { 14f, 21f, 28f }, range = new[] { 5.2f, 5.8f, 6.4f }, cooldown = new[] { 1.05f, 0.85f, 0.7f },
                icon = "Tiny Swords/Buildings/Blue Buildings/Tower",
            };

            defs[TowerType.Catapult] = new TowerDef
            {
                type = TowerType.Catapult, name = "Tháp Bắn Đá", shortName = "Bắn Đá",
                desc = "Ném đá tầm xa, sát thương lan. Không bắn được quái bay.",
                wood = new[] { 90, 90, 140 }, gold = new[] { 40, 80, 130 },
                damage = new[] { 30f, 46f, 66f }, range = new[] { 6.0f, 6.6f, 7.2f }, cooldown = new[] { 2.6f, 2.3f, 2.0f },
                splash = new[] { 1.3f, 1.5f, 1.8f }, hitsAir = false,
                baseSprite = new[] { S(3), S(6), S(7) }, topSprite = new[] { S(1), S(1), S(4) }, topFireSprite = new[] { S(2), S(2), S(5) },
                projectile = new[] { S(29), S(50), S(52) }, projectileScale = 0.55f, impactFx = "rock",
                icon = S(3),
            };

            defs[TowerType.Iron] = new TowerDef
            {
                type = TowerType.Iron, name = "Tháp Bi Sắt Gai", shortName = "Bi Sắt",
                desc = "Bóng sắt gai xuyên giáp và làm chậm kẻ địch.",
                wood = new[] { 70, 80, 120 }, gold = new[] { 70, 90, 140 },
                damage = new[] { 26f, 40f, 58f }, range = new[] { 5.5f, 6.0f, 6.5f }, cooldown = new[] { 1.6f, 1.4f, 1.2f },
                slow = new[] { 0.35f, 0.45f, 0.55f }, pierce = true,
                baseSprite = new[] { S(15), S(16), S(17) }, topSprite = new[] { S(20), S(18), S(22) }, topFireSprite = new[] { S(21), S(19), S(23) },
                projectile = new[] { S(49), S(49), S(49) }, projectileScale = 0.32f, impactFx = "iron",
                icon = S(15),
            };

            defs[TowerType.Fire] = new TowerDef
            {
                type = TowerType.Fire, name = "Tháp Hỏa Ngục", shortName = "Hỏa Ngục",
                desc = "Đá lửa nổ tung, thiêu đốt kẻ địch trong vùng nổ.",
                wood = new[] { 80, 90, 130 }, gold = new[] { 60, 90, 140 },
                damage = new[] { 16f, 24f, 34f }, range = new[] { 5.0f, 5.5f, 6.0f }, cooldown = new[] { 1.8f, 1.6f, 1.4f },
                splash = new[] { 1.0f, 1.2f, 1.45f }, burnDps = new[] { 8f, 12f, 18f },
                baseSprite = new[] { S(12), S(13), S(14) }, topSprite = new[] { S(10), S(10), S(8) }, topFireSprite = new[] { S(11), S(11), S(9) },
                projectile = new[] { S(51), S(51), S(51) }, projectileScale = 0.3f, impactFx = "fire",
                icon = S(12),
            };

            defs[TowerType.Quake] = new TowerDef
            {
                type = TowerType.Quake, name = "Tháp Búa Địa Chấn", shortName = "Địa Chấn",
                desc = "Nện búa xuống đất: sát thương và gây choáng mọi kẻ địch quanh tháp. Không đánh được quái bay.",
                wood = new[] { 100, 100, 150 }, gold = new[] { 50, 90, 140 },
                damage = new[] { 22f, 32f, 46f }, range = new[] { 2.6f, 3.0f, 3.4f }, cooldown = new[] { 2.8f, 2.5f, 2.2f },
                stun = new[] { 0.6f, 0.8f, 1.0f }, hitsAir = false, aroundSelf = true,
                baseSprite = new[] { S(24), S(25), S(26) }, topSprite = new[] { S(28), S(27), S(27) },
                impactFx = "quake",
                icon = S(24),
            };
        }

        public static string TargetModeName(TargetMode m)
        {
            switch (m)
            {
                case TargetMode.Strongest: return "Mạnh nhất";
                case TargetMode.Nearest: return "Gần nhất";
                default: return "Đầu tiên";
            }
        }

        /// <summary>Chuỗi khung hình hiệu ứng va chạm (ảnh rời trong gói Stone Tower).</summary>
        public static int[] ImpactFrames(string fx)
        {
            switch (fx)
            {
                case "rock": return new[] { 30, 31, 32, 33, 34 };
                case "rockBig": return new[] { 45, 46, 47, 48 };
                case "iron": return new[] { 40, 41, 42, 43, 44 };
                case "fire": return new[] { 54, 55, 56, 57, 58, 59, 60, 61 };
                case "sparks": return new[] { 35, 36, 37, 38, 39 };
                case "quake": return new[] { 57, 58, 59, 60, 61 };
                default: return new[] { 30, 31, 32, 33, 34 };
            }
        }
    }
}
