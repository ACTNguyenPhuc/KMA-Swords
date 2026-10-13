using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Oath
{
    /// <summary>Các bảng dùng chung giữa menu và màn chơi: hướng dẫn, cài đặt, hộp xác nhận, hội thoại.</summary>
    public static class Panels
    {
        public const string HelpText =
            "<b>MỤC TIÊU</b>\n" +
            "Bảo vệ Lâu đài qua tất cả các đợt tấn công của quân Cờ Đỏ và lũ quái vật. Lâu đài bị phá hủy là thua. " +
            "Một số màn còn yêu cầu bảo vệ nhà dân làng.\n\n" +
            "<b>ĐIỀU KHIỂN CẢM ỨNG</b>\n" +
            "• Chạm một người lính để chọn. Nút <b>Chọn cả đội</b> chọn toàn bộ quân chiến đấu.\n" +
            "• Khi đang chọn quân: chạm lên bản đồ để <b>di chuyển</b>, chạm vào kẻ địch để <b>tấn công</b>.\n" +
            "• Chế độ <b>Tấn công</b> (cạnh phải): quân vừa đi vừa đánh mọi kẻ địch gặp trên đường.\n" +
            "• Kéo một ngón để di chuyển bản đồ, chụm/mở hai ngón để thu nhỏ/phóng to.\n" +
            "• Nút <b>II</b> để tạm dừng, nút <b>x1/x2</b> để tăng tốc độ trận đấu.\n\n" +
            "<b>KINH TẾ</b>\n" +
            "• <b>Gỗ</b>: dùng để xây và sửa công trình. <b>Vàng</b>: dùng để tuyển quân và nâng cấp.\n" +
            "• <b>Thợ</b> tự động chặt cây, đào mỏ vàng và mang về Lâu đài. Chạm Lâu đài để tuyển thêm Thợ.\n" +
            "• Chọn Thợ rồi chạm vào cây/mỏ vàng để chỉ định nơi khai thác.\n" +
            "• Hạ địch nhận vàng thưởng. Gọi đợt sớm được thưởng thêm vàng.\n\n" +
            "<b>CÔNG TRÌNH</b>\n" +
            "• Chạm <b>ô xây dựng</b> (biểu tượng búa) để xây <b>Tháp canh</b> (tự bắn kẻ địch trong tầm) hoặc <b>Nhà ở</b> (+5 quân số).\n" +
            "• Chạm công trình để <b>Sửa chữa</b> (tốn gỗ, chỉ khi không có địch ở gần) hoặc <b>Nâng cấp</b> tháp.\n" +
            "• <b>Doanh trại</b> tuyển Kiếm sĩ, Thương binh, Cung thủ và Tu sĩ.\n\n" +
            "<b>CÁC LOẠI THÁP</b> (mỗi tháp 3 cấp; mở khóa dần qua các màn)\n" +
            "• <b>Tháp Cung</b>: bắn nhanh từng mục tiêu, bắn được quái bay.\n" +
            "• <b>Tháp Bắn Đá</b>: ném đá tầm xa, sát thương lan — mạnh khi địch đi thành đám.\n" +
            "• <b>Tháp Bi Sắt Gai</b>: bóng sắt xuyên giáp và làm chậm — khắc chế quái nhiều giáp.\n" +
            "• <b>Tháp Hỏa Ngục</b>: đá lửa nổ tung, thiêu đốt kẻ địch theo thời gian.\n" +
            "• <b>Tháp Búa Địa Chấn</b>: nện búa gây sát thương và choáng mọi kẻ địch quanh tháp — đặt sát đường đi.\n" +
            "• Tháp Bắn Đá và Búa Địa Chấn <b>không đánh được quái bay</b> (Quỷ Fury).\n" +
            "• Chạm tháp để <b>Nâng cấp</b>, đổi <b>Ưu tiên</b> mục tiêu (Đầu tiên / Mạnh nhất / Gần nhất) hoặc <b>Bán</b> lấy lại 60% chi phí.\n\n" +
            "<b>CHẾ ĐỘ SINH TỒN</b>\n" +
            "Các đợt địch nối tiếp vô tận, ngày càng đông và mạnh, cứ 5 đợt lại có một thủ lĩnh. Hãy trụ được càng lâu càng tốt để lập kỷ lục!\n\n" +
            "<b>CÁC LOẠI QUÂN</b>\n" +
            "• <b>Kiếm sĩ</b>: cận chiến, sát thương tốt.\n" +
            "• <b>Thương binh</b>: rất trâu, đâm làm chậm địch — hãy đặt ở lối hẹp.\n" +
            "• <b>Cung thủ</b>: bắn từ xa, máu thấp — đứng sau tuyến cận chiến.\n" +
            "• <b>Tu sĩ</b>: hồi máu cho đồng minh xung quanh.\n" +
            "• <b>Thợ</b>: khai thác tài nguyên, xây và sửa công trình; tự chạy về Lâu đài khi gặp nguy hiểm.\n\n" +
            "<b>MẸO</b>\n" +
            "Chấm đỏ trên bản đồ cho biết đường tiến quân của địch. Hãy xây tháp dọc đường đi, dùng Thương binh chặn lối hẹp " +
            "và giữ Cung thủ phía sau. Dùng thời gian giữa các đợt để sửa chữa và bổ sung quân.";

        public static GameObject Help(Transform root, Action onClose = null)
        {
            var dim = UIKit.Dim(root, 0.65f);
            var panel = UIKit.Panel(dim.transform, UIKit.Paper);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 900));
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var ribbon = UIKit.Image(panel.transform, UIKit.Ribbon(1), true, 2f, "Ribbon");
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 50), new Vector2(560, 120));
            var title = UIKit.Label(ribbon.transform, "HƯỚNG DẪN", 46, Color.white);
            UIKit.Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(0, 14);

            // Vùng cuộn
            var view = UIKit.Rect(panel.transform, "Viewport");
            view.anchorMin = new Vector2(0, 0); view.anchorMax = new Vector2(1, 1);
            view.offsetMin = new Vector2(70, 140); view.offsetMax = new Vector2(-70, -90);
            var vimg = view.gameObject.AddComponent<Image>();
            vimg.color = new Color(1, 1, 1, 0.003f);
            view.gameObject.AddComponent<RectMask2D>();
            var content = UIKit.Rect(view, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var text = UIKit.Label(content, HelpText, 30, UIKit.Ink, TextAnchor.UpperLeft, false);
            UIKit.Stretch(text.rectTransform);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var close = UIKit.Button(panel.transform, "Đóng", UIKit.BtnRed, () =>
            {
                UnityEngine.Object.Destroy(dim.gameObject);
                onClose?.Invoke();
            }, 36);
            UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(300, 100));
            return dim.gameObject;
        }

        public static GameObject Confirm(Transform root, string message, string yes, Action onYes, string no = "Hủy")
        {
            var dim = UIKit.Dim(root, 0.6f);
            var panel = UIKit.Panel(dim.transform, UIKit.Paper);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 420));
            var t = UIKit.Label(panel.transform, message, 34, UIKit.Ink, TextAnchor.MiddleCenter, false);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(680, 180));
            var by = UIKit.Button(panel.transform, yes, UIKit.BtnRed, () => { UnityEngine.Object.Destroy(dim.gameObject); onYes?.Invoke(); }, 34);
            UIKit.Place((RectTransform)by.transform, new Vector2(0.5f, 0f), new Vector2(-170, 50), new Vector2(300, 100));
            var bn = UIKit.Button(panel.transform, no, UIKit.BtnBlue, () => UnityEngine.Object.Destroy(dim.gameObject), 34);
            UIKit.Place((RectTransform)bn.transform, new Vector2(0.5f, 0f), new Vector2(170, 50), new Vector2(300, 100));
            return dim.gameObject;
        }

        public static void SettingsButtons(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float gap)
        {
            Button sfx = null, vib = null;
            sfx = UIKit.Button(parent, "", UIKit.BtnBlue, () =>
            {
                SaveManager.Sfx = !SaveManager.Sfx;
                UIKit.ButtonLabel(sfx).text = "Âm thanh: " + (SaveManager.Sfx ? "Bật" : "Tắt");
            }, 32);
            UIKit.ButtonLabel(sfx).text = "Âm thanh: " + (SaveManager.Sfx ? "Bật" : "Tắt");
            UIKit.Place((RectTransform)sfx.transform, anchor, pos, size);
            vib = UIKit.Button(parent, "", UIKit.BtnBlue, () =>
            {
                SaveManager.Vibrate = !SaveManager.Vibrate;
                UIKit.ButtonLabel(vib).text = "Rung: " + (SaveManager.Vibrate ? "Bật" : "Tắt");
                if (SaveManager.Vibrate) AudioManager.Vibrate();
            }, 32);
            UIKit.ButtonLabel(vib).text = "Rung: " + (SaveManager.Vibrate ? "Bật" : "Tắt");
            UIKit.Place((RectTransform)vib.transform, anchor, pos + new Vector2(size.x + gap, 0), size);
        }
    }

    /// <summary>Hộp hội thoại cốt truyện: chân dung, tên nhân vật, chữ chạy; chạm để tiếp tục.</summary>
    public class DialogPanel : MonoBehaviour
    {
        List<DialogLine> lines;
        int idx;
        Action done;
        Text nameText, bodyText;
        Image avatar;
        RectTransform avatarFrame;
        float shown;
        string full;
        bool typing;

        public static DialogPanel Show(Transform root, string header, List<DialogLine> lines, Action done)
        {
            var dim = UIKit.Dim(root, 0.45f);
            var d = dim.gameObject.AddComponent<DialogPanel>();
            d.lines = lines;
            d.done = done;
            d.Build(header);
            d.ShowLine(0);
            return d;
        }

        void Build(string header)
        {
            var tapper = gameObject.AddComponent<Button>();
            tapper.transition = Selectable.Transition.None;
            tapper.onClick.AddListener(Next);

            if (!string.IsNullOrEmpty(header))
            {
                var ribbon = UIKit.Image(transform, UIKit.Ribbon(1), true, 2f, "Header");
                UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(1100, 140));
                var ht = UIKit.Label(ribbon.transform, header, 44, Color.white);
                UIKit.Stretch(ht.rectTransform);
                ht.rectTransform.offsetMin = new Vector2(120, 18);
                ht.rectTransform.offsetMax = new Vector2(-120, -6);
                ht.resizeTextForBestFit = true; ht.verticalOverflow = VerticalWrapMode.Truncate; ht.resizeTextMaxSize = 44; ht.resizeTextMinSize = 24;
            }

            var panel = UIKit.Panel(transform, UIKit.Paper, "DialogBox");
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(1560, 330));
            panel.raycastTarget = false;

            avatarFrame = UIKit.Image(panel.transform, UIKit.BannerSlots, true, 2f, "AvatarFrame").rectTransform;
            UIKit.Place(avatarFrame, new Vector2(0f, 0.5f), new Vector2(50, 0), new Vector2(250, 250));
            avatarFrame.pivot = new Vector2(0f, 0.5f);
            avatar = UIKit.Image(avatarFrame, null, false, 1, "Avatar");
            UIKit.Stretch(avatar.rectTransform, 18f);
            avatar.preserveAspect = true;

            var nameRibbon = UIKit.Image(panel.transform, UIKit.SmallRibbon(1), true, 2f, "NameRibbon");
            UIKit.Place(nameRibbon.rectTransform, new Vector2(0f, 1f), new Vector2(320, 34), new Vector2(380, 84));
            nameText = UIKit.Label(nameRibbon.transform, "", 34, Color.white);
            UIKit.Stretch(nameText.rectTransform);
            nameText.rectTransform.offsetMin = new Vector2(40, 8);
            nameText.rectTransform.offsetMax = new Vector2(-40, 0);

            bodyText = UIKit.Label(panel.transform, "", 34, UIKit.Ink, TextAnchor.UpperLeft, false);
            bodyText.rectTransform.anchorMin = new Vector2(0, 0);
            bodyText.rectTransform.anchorMax = new Vector2(1, 1);
            bodyText.rectTransform.offsetMin = new Vector2(330, 70);
            bodyText.rectTransform.offsetMax = new Vector2(-60, -70);

            var hint = UIKit.Label(panel.transform, "Chạm để tiếp tục ▸", 24, new Color(0.45f, 0.32f, 0.2f), TextAnchor.LowerRight, false);
            UIKit.Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(-50, 30), new Vector2(500, 40));

            var skip = UIKit.Button(transform, "Bỏ qua", UIKit.BtnRed, Finish, 30);
            UIKit.Place((RectTransform)skip.transform, new Vector2(1f, 1f), new Vector2(-30, -30), new Vector2(220, 90));
        }

        void ShowLine(int i)
        {
            idx = i;
            if (lines == null || i >= lines.Count) { Finish(); return; }
            var l = lines[i];
            string av = UIKit.AvatarFor(l.speaker);
            avatarFrame.gameObject.SetActive(av != null);
            if (av != null) avatar.sprite = GameArt.I.Spr(av);
            nameText.text = l.speaker;
            nameText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(l.speaker));
            bodyText.rectTransform.offsetMin = new Vector2(av != null ? 330 : 70, 70);
            full = l.text;
            shown = 0f;
            typing = true;
            bodyText.text = "";
            AudioManager.Play("page", 0.5f);
        }

        void Update()
        {
            if (!typing) return;
            shown += Time.unscaledDeltaTime * 55f;
            int n = Mathf.Min(full.Length, Mathf.FloorToInt(shown));
            bodyText.text = full.Substring(0, n);
            if (n >= full.Length) typing = false;
        }

        void Next()
        {
            if (typing) { typing = false; bodyText.text = full; return; }
            ShowLine(idx + 1);
        }

        void Finish()
        {
            var cb = done;
            done = null;
            Destroy(gameObject);
            cb?.Invoke();
        }
    }
}
