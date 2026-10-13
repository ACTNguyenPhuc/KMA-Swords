using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Oath
{
    /// <summary>Màn hình chính: logo, Chơi, Chọn màn, Hướng dẫn, Cài đặt, Thoát — nền là hòn đảo sống động.</summary>
    public class MenuBootstrap : MonoBehaviour
    {
        public static bool ShowEndingOnLoad;

        Transform overlay;
        GameObject popup;

        void Awake()
        {
            Time.timeScale = 1f;
            Application.targetFrameRate = 60;
            GameEvents.ResetAll();
            World.Clear();
            var cam = SceneSetup.EnsureAll();
            BuildBackground(cam, LevelMaps.Menu, new Vector2(14.5f, 9.5f), 5.6f, true);
            AudioManager.Warmup();
            BuildUI();
            if (ShowEndingOnLoad)
            {
                ShowEndingOnLoad = false;
                ShowEnding();
            }
        }

        /// <summary>Dựng nền bản đồ trang trí (dùng chung với màn chọn màn).</summary>
        public static void BuildBackground(Camera cam, string mapText, Vector2 focus, float size, bool soldiers)
        {
            var root = new GameObject("World").transform;
            World.Root = root;
            var map = MapData.Parse(mapText.Replace("\r", "").Trim('\n').Split('\n'));
            MapBuilder.BuildTerrain(map, 1, 4, root);
            MapBuilder.BuildGameplay(map, null);
            foreach (var b in World.Buildings) if (b.bar != null) b.bar.gameObject.SetActive(false);

            if (soldiers && World.Castle != null)
            {
                Vector2 door = World.Castle.DoorPoint;
                string[] ids = { "warrior", "archer", "lancer", "warrior", "monk", "archer" };
                for (int i = 0; i < ids.Length; i++)
                {
                    Vector2 p = door + new Vector2(-2.5f + i * 1.1f, -1.2f - (i % 2) * 0.6f);
                    if (World.Nav.IsWalkable(p)) Unit.Spawn(ids[i], p).visual.Face(i % 2 == 0 ? 1 : -1);
                }
                for (int i = 0; i < 3; i++) Unit.Spawn("pawn", door + new Vector2(1 + i, -0.4f));
            }

            cam.orthographicSize = size;
            cam.transform.position = new Vector3(focus.x, focus.y, -10f);
            var drift = cam.gameObject.AddComponent<MenuCameraDrift>();
            drift.center = focus;
            CloudLayer.Create(root, new Rect(focus.x - 20, focus.y - 8, 40, 16));
        }

        void BuildUI()
        {
            var canvas = UIKit.CreateCanvas("MenuUI", 10);
            var root = UIKit.SafeRoot(canvas.transform);
            overlay = UIKit.Rect(canvas.transform, "Overlay");
            UIKit.Stretch((RectTransform)overlay);

            var ribbon = UIKit.Image(root, UIKit.Ribbon(1), true, 1.6f, "TitleRibbon");
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(1160, 200));
            var title = UIKit.Label(ribbon.transform, "LỜI THỀ QUẦN ĐẢO", 84, Color.white);
            UIKit.Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(150, 30);
            title.rectTransform.offsetMax = new Vector2(-150, -10);
            title.resizeTextForBestFit = true; title.verticalOverflow = VerticalWrapMode.Truncate; title.resizeTextMaxSize = 84; title.resizeTextMinSize = 40;
            var sub = UIKit.Label(root, "OATH OF THE ISLES", 38, UIKit.Cream);
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -238), new Vector2(800, 56));

            var panel = UIKit.Panel(root, UIKit.Banner, "MenuPanel");
            UIKit.Place(panel.rectTransform, new Vector2(0f, 0.5f), new Vector2(60, -110), new Vector2(560, 700));

            int unlocked = SaveManager.UnlockedLevel;
            bool anyProgress = SaveManager.TotalStars > 0;
            var entries = new List<(string, string, System.Action)>
            {
                (anyProgress ? "Chơi tiếp — Màn " + unlocked : "Bắt đầu chiến dịch", UIKit.BtnBlue, () => SceneLoader.LoadLevel(unlocked)),
                ("Chọn màn", UIKit.BtnBlue, () => SceneLoader.Load(SceneLoader.LevelSelect)),
                ("Sinh tồn" + (SaveManager.BestEndless > 0 ? " (kỷ lục " + SaveManager.BestEndless + ")" : ""), UIKit.BtnRed, () => SceneLoader.Load(SceneLoader.Endless)),
                ("Hướng dẫn", UIKit.BtnBlue, () => Panels.Help(overlay)),
                ("Cài đặt", UIKit.BtnBlue, ShowSettings),
                ("Thoát", UIKit.BtnRed, ConfirmQuit),
            };
            for (int i = 0; i < entries.Count; i++)
            {
                var (label, sprite, act) = entries[i];
                var b = UIKit.Button(panel.transform, label, sprite, act, 38);
                UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0, -56 - i * 100), new Vector2(450, 94));
            }

            var foot = UIKit.Label(root, "Đồ án học phần Phát triển game trên Android  •  Đồ họa: Tiny Swords (Pixel Frog) & các gói quái vật",
                22, new Color(1, 1, 1, 0.85f));
            UIKit.Place(foot.rectTransform, new Vector2(1f, 0f), new Vector2(-30, 14), new Vector2(1100, 40));
            foot.alignment = TextAnchor.LowerRight;
            var stars = UIKit.Label(root, "★ " + SaveManager.TotalStars + "/" + (SceneLoader.LevelCount * 3), 34, UIKit.Gold, TextAnchor.UpperRight);
            UIKit.Place(stars.rectTransform, new Vector2(1f, 1f), new Vector2(-30, -26), new Vector2(240, 50));
        }

        void ShowSettings()
        {
            if (popup != null) Destroy(popup);
            var dim = UIKit.Dim(overlay, 0.6f);
            popup = dim.gameObject;
            var panel = UIKit.Panel(dim.transform, UIKit.Paper);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 560));
            var ribbon = UIKit.Image(panel.transform, UIKit.Ribbon(1), true, 2f, "Ribbon");
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 50), new Vector2(520, 120));
            var t = UIKit.Label(ribbon.transform, "CÀI ĐẶT", 46, Color.white);
            UIKit.Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(0, 14);
            Panels.SettingsButtons(panel.transform, new Vector2(0.5f, 1f), new Vector2(-155, -130), new Vector2(300, 100), 10);
            var reset = UIKit.Button(panel.transform, "Xóa tiến trình", UIKit.BtnRed, () =>
                Panels.Confirm(overlay, "Xóa toàn bộ tiến trình chiến dịch?\nThao tác này không thể hoàn tác.", "Xóa", () =>
                {
                    SaveManager.ResetProgress();
                    SceneLoader.Load(SceneLoader.MainMenu);
                }), 34);
            UIKit.Place((RectTransform)reset.transform, new Vector2(0.5f, 1f), new Vector2(0, -250), new Vector2(400, 100));
            var close = UIKit.Button(panel.transform, "Đóng", UIKit.BtnBlue, () => Destroy(dim.gameObject), 34);
            UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(300, 100));
        }

        void ConfirmQuit()
        {
            Panels.Confirm(overlay, "Bạn muốn thoát game?", "Thoát", () =>
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            });
        }

        void ShowEnding()
        {
            DialogPanel.Show(overlay, "Đoạn kết — Ánh Sáng Thuộc Về Tất Cả", EndingLines, () =>
            {
                var dim = UIKit.Dim(overlay, 0.75f);
                var panel = UIKit.Panel(dim.transform, UIKit.Banner);
                UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 700));
                var ribbon = UIKit.Image(panel.transform, UIKit.Ribbon(3), true, 2f, "Ribbon");
                UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 60), new Vector2(760, 140));
                var t = UIKit.Label(ribbon.transform, "HẾT CHIẾN DỊCH", 52, Color.white);
                UIKit.Stretch(t.rectTransform);
                t.rectTransform.offsetMin = new Vector2(0, 16);
                var body = UIKit.Label(panel.transform,
                    "Cảm ơn bạn đã bảo vệ quần đảo Elaria!\n\n" +
                    "Tổng số sao: <b>" + SaveManager.TotalStars + "/" + SceneLoader.LevelCount * 3 + "</b>\n\n" +
                    "Kịch bản & lập trình: nhóm đồ án\nĐồ họa: Tiny Swords — Pixel Frog\nQuái vật: Layer Lab, Devils Pig, Potion Junkies\nFont chữ: Inter (SIL Open Font License)",
                    32, UIKit.Ink, TextAnchor.MiddleCenter, false);
                UIKit.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(900, 440));
                var close = UIKit.Button(panel.transform, "Về menu", UIKit.BtnBlue, () => Destroy(dim.gameObject), 36);
                UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(320, 104));
            });
        }

        public static readonly List<DialogLine> EndingLines = new List<DialogLine>
        {
            new DialogLine("Kể chuyện", "Đội quân tinh nhuệ cuối cùng của Cờ Đỏ tan rã. Giữa quảng trường kinh đô, Trái Tim Bình Minh rung lên dữ dội, những tia sáng chập chờn như sắp vụt tắt."),
            new DialogLine("Veyr", "Ngươi thắng rồi, Kael. Nhưng nếu tinh thể sụp đổ, chẳng còn hòn đảo nào để mà bảo vệ nữa."),
            new DialogLine("Kael", "Vậy thì hãy cùng nhau cứu nó. Không phải cho triều đình, cũng không phải cho Cờ Đỏ — mà cho tất cả mọi người."),
            new DialogLine("Mira", "Các điểm dẫn truyền trên từng hòn đảo vẫn còn. Nếu kích hoạt lại cùng lúc, năng lượng sẽ chảy về khắp Elaria chứ không chỉ riêng kinh đô."),
            new DialogLine("Kể chuyện", "Quân Lam và Cờ Đỏ cùng đứng canh các điểm dẫn truyền. Ánh sáng lan khắp quần đảo — tới cả những làng chài xa xôi từng bị lãng quên."),
            new DialogLine("Veyr", "Hòa ước được ký. Từ nay nguồn sáng sẽ được chia đều. Lời thề của ta… cuối cùng cũng được thực hiện."),
            new DialogLine("Kael", "Lời thề quần đảo: không hòn đảo nào bị bỏ lại phía sau."),
        };

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) ConfirmQuit();
        }
    }

    /// <summary>Camera trôi chậm qua lại trên nền menu.</summary>
    public class MenuCameraDrift : MonoBehaviour
    {
        public Vector2 center;
        void Update()
        {
            float t = Time.unscaledTime;
            transform.position = new Vector3(center.x + Mathf.Sin(t * 0.08f) * 2.2f, center.y + Mathf.Sin(t * 0.05f) * 0.6f, -10f);
        }
    }

    /// <summary>Mây trôi ngang bầu trời (chỉ dùng ở menu).</summary>
    public class CloudLayer : MonoBehaviour
    {
        readonly List<Transform> clouds = new List<Transform>();
        Rect area;

        public static void Create(Transform parent, Rect area)
        {
            var go = new GameObject("Clouds");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<CloudLayer>();
            c.area = area;
            for (int i = 0; i < 5; i++)
            {
                var sr = Util.NewSprite("Cloud", go.transform, GameArt.I.Spr("Tiny Swords/Terrain/Decorations/Clouds/Clouds_0" + (i % 8 + 1)),
                    new Vector3(area.xMin + Random.value * area.width, area.yMin + Random.value * area.height, 0), Util.OrderOverlay - 1000);
                sr.color = new Color(1, 1, 1, 0.75f);
                c.clouds.Add(sr.transform);
            }
        }

        void Update()
        {
            for (int i = 0; i < clouds.Count; i++)
            {
                var t = clouds[i];
                t.position += Vector3.right * Time.unscaledDeltaTime * (0.25f + i * 0.06f);
                if (t.position.x > area.xMax + 5f) t.position = new Vector3(area.xMin - 5f, area.yMin + Random.value * area.height, 0);
            }
        }
    }
}
