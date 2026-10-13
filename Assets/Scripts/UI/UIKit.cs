using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Oath
{
    /// <summary>Bộ công cụ dựng giao diện uGUI bằng mã, dùng sprite UI của Tiny Swords và font Inter (hỗ trợ tiếng Việt).</summary>
    public static class UIKit
    {
        public const string BtnBlue = GameArt.UI + "Buttons/BigBlueButton_Regular";
        public const string BtnRed = GameArt.UI + "Buttons/BigRedButton_Regular";
        public const string BtnBlueSq = GameArt.UI + "Buttons/SmallBlueSquareButton_Regular";
        public const string BtnRedSq = GameArt.UI + "Buttons/SmallRedSquareButton_Regular";
        public const string BtnBlueRound = GameArt.UI + "Buttons/SmallBlueRoundButton_Regular";
        public const string BtnRedRound = GameArt.UI + "Buttons/SmallRedRoundButton_Regular";
        public const string Paper = GameArt.UI + "Papers/RegularPaper";
        public const string PaperDark = GameArt.UI + "Papers/SpecialPaper";
        public const string Banner = GameArt.UI + "Banners/Banner";
        public const string BannerSlots = GameArt.UI + "Banners/Banner_Slots";
        public const string Wood = GameArt.UI + "Wood Table/WoodTable";
        public const string BarBase = GameArt.UI + "Bars/BigBar_Base";
        public const string BarFill = GameArt.UI + "Bars/BigBar_Fill";
        public static string Ribbon(int i) => GameArt.UI + "Ribbons/BigRibbons " + i;      // 1 lam, 2 đỏ, 3 vàng, 4 tím, 5 đen
        public static string SmallRibbon(int i) => GameArt.UI + "Ribbons/SmallRibbons " + i;

        public static readonly Color Ink = new Color(0.27f, 0.18f, 0.1f);
        public static readonly Color Cream = new Color(1f, 0.96f, 0.86f);
        public static readonly Color Gold = new Color(1f, 0.85f, 0.3f);

        public static Font Font => GameArt.I.font;

        public static Canvas CreateCanvas(string name, int order)
        {
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.75f;
            s.referencePixelsPerUnit = 100;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static RectTransform SafeRoot(Transform canvas)
        {
            var rt = Rect(canvas, "SafeArea");
            Stretch(rt);
            rt.gameObject.AddComponent<SafeArea>();
            return rt;
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Đặt neo/pivot cùng điểm (ví dụ (0,1) góc trên trái) và vị trí, kích thước.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(Transform parent, string spriteKey, bool sliced = false, float ppuMul = 2f, string name = "Image")
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = spriteKey != null ? GameArt.I.Spr(spriteKey) : null;
            if (sliced) { img.type = UnityEngine.UI.Image.Type.Sliced; img.pixelsPerUnitMultiplier = ppuMul; }
            img.raycastTarget = false;
            return img;
        }

        public static Image ColorBlock(Transform parent, Color c, string name = "Block")
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, bool outline = true, string name = "Text")
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.lineSpacing = 1.05f;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.12f, 0.08f, 0.05f, 0.9f);
                o.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        public static Image Panel(Transform parent, string kind, string name = "Panel")
        {
            float mul = kind == Banner || kind == Wood ? 3f : 2f;
            var img = Image(parent, kind, true, mul, name);
            img.raycastTarget = true; // chặn chạm xuyên xuống bản đồ
            return img;
        }

        public static Button Button(Transform parent, string label, string sprite, Action onClick, int fontSize = 34, string name = "Button")
        {
            var img = Image(parent, sprite, true, sprite == BtnBlue || sprite == BtnRed ? 2.4f : 2f, name);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 1f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.85f);
            cb.fadeDuration = 0.05f;
            b.colors = cb;
            b.onClick.AddListener(() =>
            {
                AudioManager.Play("click", 0.6f);
                onClick?.Invoke();
            });
            img.gameObject.AddComponent<PressScale>();
            if (label != null)
            {
                var t = Label(img.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter, true, "Label");
                Stretch(t.rectTransform, 10f);
                t.rectTransform.offsetMin = new Vector2(14, 14);
                t.rectTransform.offsetMax = new Vector2(-14, -8);
                t.resizeTextForBestFit = true; t.verticalOverflow = VerticalWrapMode.Truncate;
                t.resizeTextMinSize = 16;
                t.resizeTextMaxSize = fontSize;
                t.verticalOverflow = VerticalWrapMode.Truncate;
            }
            return b;
        }

        public static Text ButtonLabel(Button b) => b.transform.Find("Label")?.GetComponent<Text>();

        public static Button IconButton(Transform parent, string icon, string bg, Action onClick, float iconScale = 0.62f)
        {
            var b = Button(parent, null, bg, onClick, 30, "IconButton");
            var ic = Image(b.transform, icon, false, 1, "Icon");
            ic.preserveAspect = true;
            var rt = ic.rectTransform;
            rt.anchorMin = new Vector2(0.5f - iconScale / 2, 0.5f - iconScale / 2 + 0.03f);
            rt.anchorMax = new Vector2(0.5f + iconScale / 2, 0.5f + iconScale / 2 + 0.03f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return b;
        }

        /// <summary>Thanh tiến trình: nền gỗ + phần lấp đầy kiểu Filled.</summary>
        public static Image Bar(Transform parent, Color fill, out RectTransform root)
        {
            root = Rect(parent, "Bar");
            var bg = Image(root, BarBase, true, 2f, "Base");
            Stretch(bg.rectTransform);
            var fillRt = Rect(root, "FillArea");
            fillRt.anchorMin = new Vector2(0, 0); fillRt.anchorMax = new Vector2(1, 1);
            fillRt.offsetMin = new Vector2(16, 12); fillRt.offsetMax = new Vector2(-16, -12);
            var f = fillRt.gameObject.AddComponent<Image>();
            f.sprite = Util.WhitePixel();
            f.type = UnityEngine.UI.Image.Type.Filled;
            f.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            f.fillAmount = 1f;
            f.color = fill;
            f.raycastTarget = false;
            return f;
        }

        public static Image Star(Transform parent, bool filled)
        {
            var rt = Rect(parent, "Star");
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = StarSprite();
            img.color = filled ? Gold : new Color(0.3f, 0.25f, 0.2f, 0.6f);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        static Sprite star;
        public static Sprite StarSprite()
        {
            if (star != null) return star;
            int s = 96;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 5;
                float r = (i % 2 == 0 ? 0.48f : 0.21f) * s;
                pts[i] = new Vector2(s / 2f + Mathf.Cos(a) * r, s / 2f + Mathf.Sin(a) * r - 2);
            }
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    bool inside = InPoly(p, pts);
                    float edge = DistToPoly(p, pts);
                    byte a = 0, v = 255;
                    if (inside) { a = 255; v = edge < 5f ? (byte)90 : (byte)255; }
                    else if (edge < 1.5f) { a = (byte)(255 * (1.5f - edge) / 1.5f); v = 90; }
                    px[y * s + x] = new Color32(v, v, v, a);
                }
            t.SetPixels32(px); t.Apply();
            star = Sprite.Create(t, new UnityEngine.Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100);
            return star;
        }

        static bool InPoly(Vector2 p, Vector2[] poly)
        {
            bool c = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)) c = !c;
            return c;
        }

        static float DistToPoly(Vector2 p, Vector2[] poly)
        {
            float d = float.MaxValue;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                Vector2 a = poly[j], b = poly[i], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                d = Mathf.Min(d, Vector2.Distance(p, a + ab * t));
            }
            return d;
        }

        public static string AvatarFor(string speaker)
        {
            switch (speaker)
            {
                case "Kael": return GameArt.Avatar(1);
                case "Mira": return GameArt.Avatar(5);
                case "Veyr": return GameArt.Avatar(6);
                case "Dân làng": return GameArt.Avatar(14);
                case "Lính gác": return GameArt.Avatar(2);
                default: return null;
            }
        }

        /// <summary>Lớp phủ tối toàn màn hình (chặn chạm phía sau).</summary>
        public static Image Dim(Transform parent, float alpha = 0.6f)
        {
            var img = ColorBlock(parent, new Color(0.05f, 0.04f, 0.08f, alpha), "Dim");
            Stretch(img.rectTransform);
            img.raycastTarget = true;
            return img;
        }
    }

    /// <summary>Nút thu nhỏ khi nhấn để phản hồi cảm giác chạm.</summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Vector3 baseScale = Vector3.one;
        bool down;
        void Awake() { baseScale = transform.localScale; }
        public void OnPointerDown(PointerEventData e) { down = true; transform.localScale = baseScale * 0.93f; }
        public void OnPointerUp(PointerEventData e) { down = false; transform.localScale = baseScale; }
        public void OnPointerExit(PointerEventData e) { if (down) { down = false; transform.localScale = baseScale; } }
        void OnDisable() { transform.localScale = baseScale; }
    }

    /// <summary>Co phần giao diện vào vùng an toàn (tránh tai thỏ/camera đục lỗ).</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect last;
        Vector2Int lastScreen;
        void Update()
        {
            var sa = Screen.safeArea;
            var scr = new Vector2Int(Screen.width, Screen.height);
            if (sa == last && scr == lastScreen) return;
            last = sa; lastScreen = scr;
            var rt = (RectTransform)transform;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
