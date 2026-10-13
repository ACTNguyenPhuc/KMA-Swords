using UnityEngine;

namespace Oath
{
    /// <summary>Hiệu ứng hình ảnh dùng bộ Particle FX của Tiny Swords và chữ nổi.</summary>
    public static class FX
    {
        const string PFX = "Tiny Swords/Particle FX/";

        static string Key(string kind, out float fps)
        {
            fps = 14f;
            switch (kind)
            {
                case "dust": return PFX + "Dust_01";
                case "dust2": return PFX + "Dust_02";
                case "explosion": fps = 16f; return PFX + "Explosion_01";
                case "explosion2": fps = 16f; return PFX + "Explosion_02";
                case "fire": fps = 12f; return PFX + "Fire_01";
                case "fire2": fps = 12f; return PFX + "Fire_02";
                case "fire3": fps = 12f; return PFX + "Fire_03";
                case "splash": return PFX + "Water Splash";
                case "heal": fps = 18f; return "Tiny Swords/Units/Extra/Heal Effect/Heal_Effect";
            }
            return PFX + "Dust_01";
        }

        public static void Play(string kind, Vector2 pos, float scale = 1f)
        {
            var frames = GameArt.I.Set(Key(kind, out float fps));
            if (frames.Length == 0) return;
            var go = new GameObject("FX_" + kind);
            if (World.Root != null) go.transform.SetParent(World.Root, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = Util.SortOrder(pos.y - 0.6f);
            var a = go.AddComponent<SpriteAnimator>();
            a.target = sr;
            a.destroyOnComplete = true;
            a.Play(frames, fps, false, true);
        }

        static readonly System.Collections.Generic.Dictionary<string, Sprite[]> impactCache = new System.Collections.Generic.Dictionary<string, Sprite[]>();

        /// <summary>Hiệu ứng va chạm của gói Stone Tower (ảnh rời ghép thành chuỗi).</summary>
        public static void Impact(string fx, Vector2 pos, float scale, float fps = 18f)
        {
            if (!impactCache.TryGetValue(fx, out var frames) || frames == null || frames.Length == 0 || frames[0] == null)
            {
                var ids = TowerCatalog.ImpactFrames(fx);
                frames = new Sprite[ids.Length];
                for (int i = 0; i < ids.Length; i++) frames[i] = GameArt.I.Spr(TowerCatalog.ST + ids[i]);
                impactCache[fx] = frames;
            }
            PlaySprites(frames, pos, scale, fps, Util.SortOrder(pos.y - 0.8f));
        }

        public static void PlaySprites(Sprite[] frames, Vector2 pos, float scale, float fps, int order)
        {
            if (frames == null || frames.Length == 0) return;
            var go = new GameObject("FX");
            if (World.Root != null) go.transform.SetParent(World.Root, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            var a = go.AddComponent<SpriteAnimator>();
            a.target = sr;
            a.destroyOnComplete = true;
            a.Play(frames, fps, false, true);
        }

        public static GameObject Loop(string kind, Transform parent, Vector3 localPos, float scale, int order)
        {
            var frames = GameArt.I.Set(Key(kind, out float fps));
            var go = new GameObject("FX_" + kind);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            var a = go.AddComponent<SpriteAnimator>();
            a.target = sr;
            a.Play(frames, fps, true, true, null, true);
            return go;
        }

        public static void Text(Vector2 pos, string text, Color color, float size = 1f)
        {
            var go = new GameObject("FloatText");
            if (World.Root != null) go.transform.SetParent(World.Root, false);
            go.transform.position = pos;
            go.AddComponent<FloatingText>().Init(text, color, size);
        }
    }

    public class FloatingText : MonoBehaviour
    {
        TextMesh main, shadow;
        float t;
        Color color;

        public void Init(string text, Color c, float size)
        {
            color = c;
            var font = GameArt.I.font;
            shadow = Make("Shadow", text, new Color(0, 0, 0, 0.8f), font, size, new Vector3(0.04f, -0.04f, 0), Util.OrderOverlay - 11);
            main = Make("Text", text, c, font, size, Vector3.zero, Util.OrderOverlay - 10);
        }

        TextMesh Make(string name, string text, Color c, Font font, float size, Vector3 off, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = off;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = font;
            tm.fontSize = 64;
            tm.characterSize = 0.06f * size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = c;
            var mr = go.GetComponent<MeshRenderer>();
            if (font != null) mr.sharedMaterial = font.material;
            mr.sortingOrder = order;
            return tm;
        }

        void Update()
        {
            t += Time.deltaTime;
            transform.position += Vector3.up * Time.deltaTime * 0.8f;
            float a = Mathf.Clamp01(1.6f - t * 1.6f);
            main.color = new Color(color.r, color.g, color.b, a);
            shadow.color = new Color(0, 0, 0, 0.8f * a);
            if (t > 1f) Destroy(gameObject);
        }
    }

    /// <summary>Thanh máu nổi trên đầu đơn vị/công trình (luôn vẽ trên cùng).</summary>
    public class HealthBar : MonoBehaviour
    {
        Health h;
        Transform follow;
        float yOff, width;
        SpriteRenderer bg, fill, back;
        public bool forceShow;
        public bool alwaysShow;
        static Transform container;

        public static HealthBar Create(Health h, Transform follow, float yOffset, float width)
        {
            if (container == null)
            {
                container = new GameObject("HealthBars").transform;
                if (World.Root != null) container.SetParent(World.Root, false);
            }
            var go = new GameObject("HP_" + follow.name);
            go.transform.SetParent(container, false);
            var hb = go.AddComponent<HealthBar>();
            hb.h = h; hb.follow = follow; hb.yOff = yOffset; hb.width = width;
            var px = Util.WhitePixel();
            hb.back = Util.NewSprite("Back", go.transform, px, Vector3.zero, Util.OrderOverlay);
            hb.back.color = new Color(0.1f, 0.06f, 0.05f, 0.85f);
            hb.back.transform.localScale = new Vector3(width + 0.08f, 0.16f, 1);
            hb.bg = Util.NewSprite("Bg", go.transform, px, Vector3.zero, Util.OrderOverlay + 1);
            hb.bg.color = new Color(0.35f, 0.12f, 0.1f, 1f);
            hb.bg.transform.localScale = new Vector3(width, 0.09f, 1);
            hb.fill = Util.NewSprite("Fill", go.transform, px, Vector3.zero, Util.OrderOverlay + 2);
            hb.fill.color = h.team == Team.Player ? new Color(0.45f, 0.9f, 0.3f) : new Color(0.98f, 0.3f, 0.22f);
            hb.Refresh();
            return hb;
        }

        void LateUpdate() { Refresh(); }

        public void SetOffset(float y) { yOff = y; }

        void Refresh()
        {
            if (h == null || follow == null) { Destroy(gameObject); return; }
            bool show = h.Alive && (alwaysShow || forceShow || h.Ratio < 0.999f);
            back.enabled = bg.enabled = fill.enabled = show;
            if (!show) return;
            transform.position = (Vector2)follow.position + Vector2.up * yOff;
            float r = Mathf.Clamp01(h.Ratio);
            fill.transform.localScale = new Vector3(width * r, 0.09f, 1);
            fill.transform.localPosition = new Vector3(-width * (1f - r) * 0.5f, 0, 0);
        }

        public static void ResetContainer() { container = null; }
    }
}
