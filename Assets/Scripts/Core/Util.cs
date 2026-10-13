using System.Collections.Generic;
using UnityEngine;

namespace Oath
{
    /// <summary>Hằng số thứ tự hiển thị và tiện ích sinh sprite đơn giản.</summary>
    public static class Util
    {
        public const int OrderWater = -3000, OrderFoam = -2990, OrderGround = -2980, OrderRoad = -2970,
            OrderPlateauShadow = -2965, OrderPlateau = -2960, OrderGroundDecal = -2900, OrderOverlay = 20000;

        /// <summary>Đối tượng càng thấp (y nhỏ) càng vẽ đè lên trên.</summary>
        public static int SortOrder(float y) => Mathf.RoundToInt(-y * 20f);

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite WhitePixel()
        {
            return Cached("px", () =>
            {
                var t = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var c = new Color32[16];
                for (int i = 0; i < 16; i++) c[i] = new Color32(255, 255, 255, 255);
                t.SetPixels32(c); t.Apply();
                return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            });
        }

        /// <summary>Hình elip mềm (bóng đổ) hoặc vòng elip (chọn đơn vị).</summary>
        public static Sprite Ellipse(bool ring)
        {
            return Cached(ring ? "ring" : "ellipse", () =>
            {
                int w = 128, h = 64;
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = (x + 0.5f - w / 2f) / (w / 2f), dy = (y + 0.5f - h / 2f) / (h / 2f);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a;
                        if (ring) a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.13f);
                        else a = Mathf.Clamp01((1f - d) * 2.2f);
                        px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                t.SetPixels32(px); t.Apply();
                return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 128);
            });
        }

        public static Sprite Circle()
        {
            return Cached("circle", () =>
            {
                int s = 64;
                var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float dx = (x + 0.5f - s / 2f) / (s / 2f), dy = (y + 0.5f - s / 2f) / (s / 2f);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01((1f - d) * 3f);
                        float core = Mathf.Clamp01((0.55f - d) * 4f);
                        byte v = (byte)(200 + 55 * core);
                        px[y * s + x] = new Color32(v, v, v, (byte)(a * 255));
                    }
                t.SetPixels32(px); t.Apply();
                return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 64);
            });
        }

        /// <summary>Ô xây dựng: hình vuông bo góc viền nét đứt.</summary>
        public static Sprite PlotSquare()
        {
            return Cached("plot", () =>
            {
                int s = 64;
                var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        int ex = Mathf.Min(x, s - 1 - x), ey = Mathf.Min(y, s - 1 - y);
                        int e = Mathf.Min(ex, ey);
                        bool corner = ex < 6 && ey < 6 && (6 - ex) * (6 - ex) + (6 - ey) * (6 - ey) > 36;
                        byte a = 0;
                        if (!corner)
                        {
                            if (e < 4) a = (byte)((((x + y) / 6) % 2 == 0) ? 230 : 90);
                            else a = 55;
                        }
                        px[y * s + x] = new Color32(255, 250, 220, a);
                    }
                t.SetPixels32(px); t.Apply();
                return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 64);
            });
        }

        static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = make();
            cache[key] = s;
            return s;
        }

        public static SpriteRenderer NewSprite(string name, Transform parent, Sprite sprite, Vector3 localPos, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        public static Vector2 ClosestPointOnRect(Rect r, Vector2 p)
        {
            return new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax));
        }

        public static string Money(int wood, int gold)
        {
            var parts = new List<string>();
            if (wood > 0) parts.Add(wood + " gỗ");
            if (gold > 0) parts.Add(gold + " vàng");
            return parts.Count == 0 ? "Miễn phí" : string.Join(", ", parts);
        }

        public static string Time(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }
    }
}
