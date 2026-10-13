using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Oath
{
    /// <summary>Chọn màn: các hòn đảo đã mở khóa, số sao đạt được, mô tả và nút bắt đầu.</summary>
    public class LevelSelectBootstrap : MonoBehaviour
    {
        public LevelConfig[] levels;

        Transform overlay;
        RectTransform detail;
        Text detailTitle, detailBody;
        Button startBtn;
        int selected = -1;
        readonly Image[] cardFrames = new Image[SceneLoader.LevelCount];

        static readonly string[] Thumbs =
        {
            "Tiny Swords/Buildings/Yellow Buildings/House1",
            "Tiny Swords/Buildings/Blue Buildings/Tower",
            "Tiny Swords/Buildings/Yellow Buildings/House3",
            "Tiny Swords/Buildings/Blue Buildings/Castle",
            "Tiny Swords/Buildings/Red Buildings/Castle",
        };

        void Awake()
        {
            Time.timeScale = 1f;
            Application.targetFrameRate = 60;
            GameEvents.ResetAll();
            World.Clear();
            var cam = SceneSetup.EnsureAll();
            MenuBootstrap.BuildBackground(cam, LevelMaps.Menu, new Vector2(18f, 10f), 6.2f, false);
            AudioManager.Warmup();
            BuildUI();
            Select(Mathf.Clamp(SaveManager.UnlockedLevel - 1, 0, SceneLoader.LevelCount - 1));
        }

        void BuildUI()
        {
            var canvas = UIKit.CreateCanvas("LevelSelectUI", 10);
            var root = UIKit.SafeRoot(canvas.transform);
            overlay = UIKit.Rect(canvas.transform, "Overlay");
            UIKit.Stretch((RectTransform)overlay);
            var shade = UIKit.ColorBlock(root, new Color(0.05f, 0.08f, 0.12f, 0.35f), "Shade");
            UIKit.Stretch(shade.rectTransform, -200f);

            var ribbon = UIKit.Image(root, UIKit.Ribbon(1), true, 2f, "Title");
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(900, 150));
            var t = UIKit.Label(ribbon.transform, "CHIẾN DỊCH QUẦN ĐẢO ELARIA", 46, Color.white);
            UIKit.Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(120, 22);
            t.rectTransform.offsetMax = new Vector2(-120, -6);
            t.resizeTextForBestFit = true; t.verticalOverflow = VerticalWrapMode.Truncate; t.resizeTextMaxSize = 46; t.resizeTextMinSize = 24;

            var back = UIKit.Button(root, "◂ Menu", UIKit.BtnRed, () => SceneLoader.Load(SceneLoader.MainMenu), 34);
            UIKit.Place((RectTransform)back.transform, new Vector2(0, 1), new Vector2(20, -30), new Vector2(230, 100));
            var stars = UIKit.Label(root, "★ " + SaveManager.TotalStars + "/" + SceneLoader.LevelCount * 3, 36, UIKit.Gold, TextAnchor.MiddleRight);
            UIKit.Place(stars.rectTransform, new Vector2(1, 1), new Vector2(-30, -50), new Vector2(260, 60));

            int unlocked = SaveManager.UnlockedLevel;
            float cardW = 330, gap = 22;
            float total = SceneLoader.LevelCount * cardW + (SceneLoader.LevelCount - 1) * gap;
            for (int i = 0; i < SceneLoader.LevelCount; i++)
            {
                int level = i + 1;
                bool open = level <= unlocked;
                var cfg = levels != null && i < levels.Length ? levels[i] : null;

                var card = UIKit.Panel(root, open ? UIKit.Paper : UIKit.PaperDark, "Card" + level);
                cardFrames[i] = card;
                UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-total / 2 + cardW / 2 + i * (cardW + gap), 70), new Vector2(cardW, 470));
                card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var btn = card.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                int idx = i;
                btn.onClick.AddListener(() => { AudioManager.Play("select", 0.6f); Select(idx); });
                card.gameObject.AddComponent<PressScale>();

                var num = UIKit.Image(card.transform, UIKit.SmallRibbon(open ? 1 : 5), true, 2f, "Num");
                UIKit.Place(num.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 24), new Vector2(250, 76));
                var nt = UIKit.Label(num.transform, "MÀN " + level, 32, Color.white);
                UIKit.Stretch(nt.rectTransform);
                nt.rectTransform.offsetMin = new Vector2(0, 8);

                var thumb = UIKit.Image(card.transform, Thumbs[i], false, 1, "Thumb");
                thumb.preserveAspect = true;
                UIKit.Place(thumb.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(220, 170));
                if (!open) thumb.color = new Color(0.3f, 0.3f, 0.35f, 0.9f);

                var title = UIKit.Label(card.transform, cfg != null ? cfg.title : "Màn " + level, 30, open ? UIKit.Ink : new Color(0.85f, 0.85f, 0.9f), TextAnchor.UpperCenter, false);
                UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -250), new Vector2(290, 80));
                title.fontStyle = FontStyle.Bold;
                var loc = UIKit.Label(card.transform, cfg != null ? cfg.location : "", 22, open ? new Color(0.4f, 0.3f, 0.18f) : new Color(0.75f, 0.75f, 0.8f), TextAnchor.UpperCenter, false);
                UIKit.Place(loc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -330), new Vector2(290, 36));

                if (open)
                {
                    int s = SaveManager.Stars(level);
                    for (int k = 0; k < 3; k++)
                    {
                        var st = UIKit.Star(card.transform, k < s);
                        UIKit.Place(st.rectTransform, new Vector2(0.5f, 0f), new Vector2((k - 1) * 70, 36), new Vector2(64, 64));
                    }
                }
                else
                {
                    var lk = UIKit.Label(card.transform, "Chưa mở khóa", 26, new Color(1f, 0.75f, 0.6f), TextAnchor.MiddleCenter);
                    UIKit.Place(lk.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 46), new Vector2(290, 50));
                }
            }

            var d = UIKit.Panel(root, UIKit.Paper, "Detail");
            detail = d.rectTransform;
            UIKit.Place(detail, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(1500, 230));
            detailTitle = UIKit.Label(detail, "", 34, UIKit.Ink, TextAnchor.UpperLeft, false);
            UIKit.Place(detailTitle.rectTransform, new Vector2(0, 1), new Vector2(50, -34), new Vector2(1050, 46));
            detailTitle.fontStyle = FontStyle.Bold;
            detailBody = UIKit.Label(detail, "", 26, UIKit.Ink, TextAnchor.UpperLeft, false);
            UIKit.Place(detailBody.rectTransform, new Vector2(0, 1), new Vector2(50, -84), new Vector2(1080, 120));
            startBtn = UIKit.Button(detail, "Bắt đầu ▸", UIKit.BtnBlue, () => { if (selected >= 0) SceneLoader.LoadLevel(selected + 1); }, 40);
            UIKit.Place((RectTransform)startBtn.transform, new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(300, 120));
        }

        void Select(int i)
        {
            selected = i;
            int level = i + 1;
            bool open = level <= SaveManager.UnlockedLevel;
            var cfg = levels != null && i < levels.Length ? levels[i] : null;
            for (int k = 0; k < cardFrames.Length; k++)
                if (cardFrames[k] != null) cardFrames[k].transform.localScale = Vector3.one * (k == i ? 1.06f : 1f);
            detailTitle.text = "Màn " + level + " — " + (cfg != null ? cfg.title : "");
            string body = cfg != null ? cfg.description + "\n<b>Mục tiêu:</b> " + cfg.objective + " qua " + cfg.waves.Count + " đợt tấn công." : "";
            if (open && SaveManager.BestTime(level) > 0) body += "   <b>Kỷ lục:</b> " + Util.Time(SaveManager.BestTime(level));
            if (!open) body = "Hoàn thành màn " + (level - 1) + " để mở khóa hòn đảo này.";
            detailBody.text = body;
            startBtn.interactable = open;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) SceneLoader.Load(SceneLoader.MainMenu);
        }
    }
}
