using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Oath
{
    /// <summary>
    /// Ô gợi ý của Mira. Ở màn 1, mỗi gợi ý chỉ biến mất khi người chơi làm đúng thao tác
    /// (xây tháp, tuyển quân, ra lệnh di chuyển...). Các màn sau gợi ý tự chuyển theo thời gian.
    /// </summary>
    public class TutorialHints : MonoBehaviour
    {
        List<string> hints;
        bool eventDriven;
        int idx = -1;
        float shownFor, pollTimer;
        Text text;
        int initialTowers;
        bool commanded;

        public static TutorialHints Create(Transform root, List<string> hints, bool eventDriven)
        {
            var panel = UIKit.Panel(root, UIKit.Paper, "TutorialHint");
            UIKit.Place(panel.rectTransform, new Vector2(0, 1), new Vector2(16, -128), new Vector2(600, 190));
            var t = panel.gameObject.AddComponent<TutorialHints>();
            t.hints = hints;
            t.eventDriven = eventDriven;

            var av = UIKit.Image(panel.transform, GameArt.Avatar(5), false, 1, "Mira");
            av.preserveAspect = true;
            UIKit.Place(av.rectTransform, new Vector2(0, 0.5f), new Vector2(22, 4), new Vector2(110, 110));
            av.rectTransform.pivot = new Vector2(0, 0.5f);
            var name = UIKit.Label(panel.transform, "Mira", 22, new Color(0.25f, 0.4f, 0.6f), TextAnchor.UpperCenter, false);
            UIKit.Place(name.rectTransform, new Vector2(0, 0), new Vector2(22, 12), new Vector2(110, 30));
            name.rectTransform.pivot = new Vector2(0, 0);

            t.text = UIKit.Label(panel.transform, "", 24, UIKit.Ink, TextAnchor.MiddleLeft, false);
            t.text.rectTransform.anchorMin = Vector2.zero; t.text.rectTransform.anchorMax = Vector2.one;
            t.text.rectTransform.offsetMin = new Vector2(146, 22); t.text.rectTransform.offsetMax = new Vector2(-50, -22);

            var close = UIKit.Button(panel.transform, "×", UIKit.BtnRedRound, () => Destroy(panel.gameObject), 30);
            UIKit.Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(14, 14), new Vector2(64, 64));

            foreach (var b in World.Buildings) if (b.type == BuildingType.Tower) t.initialTowers++;
            if (SelectionManager.I != null) SelectionManager.I.OnGroundCommand += _ => t.commanded = true;
            panel.gameObject.SetActive(true);
            t.Next();
            return t;
        }

        bool Done(int i)
        {
            if (!eventDriven) return shownFor > 9f;
            switch (i)
            {
                case 0:
                {
                    int towers = 0;
                    foreach (var b in World.Buildings) if (b.type == BuildingType.Tower) towers++;
                    return towers > initialTowers;
                }
                case 1: return World.CountUnits(Team.Player, true) > 0;
                case 2: return commanded;
                case 3: return shownFor > 9f;
                default: return WaveManager.I != null && WaveManager.I.WaveIndex >= 0 || shownFor > 14f;
            }
        }

        void Next()
        {
            do { idx++; } while (eventDriven && idx < hints.Count && idx < 3 && Done(idx));
            if (idx >= hints.Count) { Destroy(gameObject); return; }
            shownFor = 0f;
            text.text = hints[idx];
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            if (GameManager.I == null || GameManager.I.State != GameState.Playing) return;
            shownFor += Time.unscaledDeltaTime;
            pollTimer -= Time.unscaledDeltaTime;
            if (pollTimer > 0f) return;
            pollTimer = 0.3f;
            if (Done(idx)) { AudioManager.Play("select", 0.4f); Next(); }
        }
    }
}
