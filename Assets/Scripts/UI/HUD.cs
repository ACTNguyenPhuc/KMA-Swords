using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Oath
{
    /// <summary>
    /// Giao diện trong màn chơi: thanh tài nguyên, mục tiêu/đợt, máu lâu đài, nút Pause/tốc độ,
    /// bảng thông tin–lệnh của đối tượng đang chọn, thông báo, hướng dẫn, tạm dừng và kết quả.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        LevelConfig cfg;
        Canvas canvas;
        RectTransform root, overlay;

        Text woodText, goldText, popText, objectiveText, waveText, castleText;
        Image castleFill;
        Button callEarlyBtn, speedBtn, selectAllBtn, deselectBtn, moveBtn, attackBtn;
        RectTransform waveRow;

        RectTransform context;
        Image portrait;
        Text ctxTitle, ctxLine1, ctxLine2;
        RectTransform actionRow;
        readonly List<ActionButton> actions = new List<ActionButton>();
        Image progressFill;
        RectTransform progressRoot;

        RectTransform messageRoot;
        readonly List<GameObject> messages = new List<GameObject>();

        TutorialHints tutorial;
        GameObject pauseMenu, resultPanel;
        float refreshTimer, castleWarnTimer;

        class ActionButton
        {
            public Button button;
            public Text label;
            public Func<bool> enabled;
            public Func<string> text;
        }

        public void Init(LevelConfig c)
        {
            cfg = c;
            canvas = UIKit.CreateCanvas("HUD", 10);
            canvas.transform.SetParent(transform, false);
            root = UIKit.SafeRoot(canvas.transform);
            overlay = UIKit.Rect(canvas.transform, "Overlay");
            UIKit.Stretch(overlay);

            BuildTopBar();
            BuildSides();
            BuildContext();
            messageRoot = UIKit.Rect(root, "Messages");
            UIKit.Place(messageRoot, new Vector2(0.5f, 1f), new Vector2(0, -250), new Vector2(900, 300));

            ResourceManager.I.OnChanged += RefreshResources;
            WaveManager.I.OnChanged += RefreshWave;
            GameManager.I.OnStateChanged += OnState;
            SelectionManager.I.OnSelectionChanged += RebuildContext;
            GameEvents.OnMessage += ShowMessage;
            if (World.Castle != null) World.Castle.health.OnDamaged += OnCastleHit;

            if (cfg.tutorialHints != null && cfg.tutorialHints.Count > 0)
                tutorial = TutorialHints.Create(root, cfg.tutorialHints, cfg.levelIndex == 1);

            RefreshResources();
            RefreshWave();
            RebuildContext();
            OnState(GameManager.I.State);
        }

        void OnDestroy()
        {
            if (ResourceManager.I != null) ResourceManager.I.OnChanged -= RefreshResources;
            if (WaveManager.I != null) WaveManager.I.OnChanged -= RefreshWave;
            if (GameManager.I != null) GameManager.I.OnStateChanged -= OnState;
            if (SelectionManager.I != null) SelectionManager.I.OnSelectionChanged -= RebuildContext;
            GameEvents.OnMessage -= ShowMessage;
        }

        // ======================= Thanh trên =======================

        void BuildTopBar()
        {
            // Tài nguyên
            var res = UIKit.Panel(root, UIKit.PaperDark, "Resources");
            UIKit.Place(res.rectTransform, new Vector2(0, 1), new Vector2(16, -16), new Vector2(560, 104));
            woodText = ResourceItem(res.transform, GameArt.Icon(2), 24);
            goldText = ResourceItem(res.transform, GameArt.Icon(3), 200);
            popText = ResourceItem(res.transform, GameArt.Avatar(2), 376);

            // Mục tiêu + máu lâu đài
            var obj = UIKit.Panel(root, UIKit.PaperDark, "Objective");
            UIKit.Place(obj.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(660, 140));
            objectiveText = UIKit.Label(obj.transform, "", 28, UIKit.Cream);
            objectiveText.resizeTextForBestFit = true; objectiveText.resizeTextMinSize = 16; objectiveText.resizeTextMaxSize = 28;
            objectiveText.verticalOverflow = VerticalWrapMode.Truncate; objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIKit.Place(objectiveText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(600, 44));
            castleFill = UIKit.Bar(obj.transform, new Color(0.45f, 0.85f, 0.35f), out var barRoot);
            UIKit.Place(barRoot, new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(560, 60));
            castleText = UIKit.Label(barRoot, "", 24, Color.white);
            UIKit.Stretch(castleText.rectTransform);

            // Hàng đếm ngược đợt
            var row = UIKit.Panel(root, UIKit.PaperDark, "WaveRow");
            waveRow = row.rectTransform;
            UIKit.Place(waveRow, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(660, 86));
            waveText = UIKit.Label(row.transform, "", 28, UIKit.Cream, TextAnchor.MiddleLeft);
            UIKit.Place(waveText.rectTransform, new Vector2(0, 0.5f), new Vector2(34, 2), new Vector2(380, 60));
            waveText.rectTransform.pivot = new Vector2(0, 0.5f);
            callEarlyBtn = UIKit.Button(row.transform, "Gọi ngay ▸", UIKit.BtnRed, () => WaveManager.I.CallEarly(), 28);
            UIKit.Place((RectTransform)callEarlyBtn.transform, new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(230, 78));

            // Tạm dừng & tốc độ
            var pause = UIKit.Button(root, "II", UIKit.BtnBlueSq, () => GameManager.I.Pause(), 44);
            UIKit.Place((RectTransform)pause.transform, new Vector2(1, 1), new Vector2(-16, -16), new Vector2(108, 108));
            speedBtn = UIKit.Button(root, "x1", UIKit.BtnBlueSq, () => GameManager.I.ToggleSpeed(), 36);
            UIKit.Place((RectTransform)speedBtn.transform, new Vector2(1, 1), new Vector2(-134, -16), new Vector2(108, 108));
            var help = UIKit.Button(root, "?", UIKit.BtnBlueSq, () =>
            {
                GameManager.I.Pause();
            }, 44);
            UIKit.Place((RectTransform)help.transform, new Vector2(1, 1), new Vector2(-252, -16), new Vector2(108, 108));
            help.onClick.AddListener(() => Panels.Help(overlay));
        }

        Text ResourceItem(Transform parent, string icon, float x)
        {
            var ic = UIKit.Image(parent, icon, false, 1, "Icon");
            ic.preserveAspect = true;
            UIKit.Place(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(x, 4), new Vector2(64, 64));
            ic.rectTransform.pivot = new Vector2(0, 0.5f);
            var t = UIKit.Label(parent, "0", 34, Color.white, TextAnchor.MiddleLeft);
            UIKit.Place(t.rectTransform, new Vector2(0, 0.5f), new Vector2(x + 70, 4), new Vector2(110, 60));
            t.rectTransform.pivot = new Vector2(0, 0.5f);
            return t;
        }

        // ======================= Hai cạnh =======================

        void BuildSides()
        {
            selectAllBtn = UIKit.Button(root, "Chọn cả đội", UIKit.BtnBlue, () => SelectionManager.I.SelectAllArmy(), 32);
            UIKit.Place((RectTransform)selectAllBtn.transform, new Vector2(0, 0.5f), new Vector2(16, 70), new Vector2(250, 108));
            deselectBtn = UIKit.Button(root, "Bỏ chọn", UIKit.BtnRed, () => SelectionManager.I.ClearAll(), 32);
            UIKit.Place((RectTransform)deselectBtn.transform, new Vector2(0, 0.5f), new Vector2(16, -50), new Vector2(250, 100));

            moveBtn = UIKit.Button(root, "Di chuyển", UIKit.BtnBlue, () => SelectionManager.I.SetMode(CommandMode.Move), 32);
            UIKit.Place((RectTransform)moveBtn.transform, new Vector2(1, 0.5f), new Vector2(-16, 70), new Vector2(250, 108));
            attackBtn = UIKit.Button(root, "Tấn công", UIKit.BtnRed, () => SelectionManager.I.SetMode(CommandMode.Attack), 32);
            UIKit.Place((RectTransform)attackBtn.transform, new Vector2(1, 0.5f), new Vector2(-16, -50), new Vector2(250, 108));
        }

        // ======================= Bảng ngữ cảnh =======================

        void BuildContext()
        {
            var p = UIKit.Panel(root, UIKit.Paper, "Context");
            context = p.rectTransform;
            UIKit.Place(context, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(1440, 230));

            var frame = UIKit.Image(context, UIKit.BannerSlots, true, 2f, "PortraitFrame");
            UIKit.Place(frame.rectTransform, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(170, 170));
            frame.rectTransform.pivot = new Vector2(0, 0.5f);
            portrait = UIKit.Image(frame.transform, null, false, 1, "Portrait");
            UIKit.Stretch(portrait.rectTransform, 14f);
            portrait.preserveAspect = true;

            ctxTitle = UIKit.Label(context, "", 34, UIKit.Ink, TextAnchor.UpperLeft, false);
            UIKit.Place(ctxTitle.rectTransform, new Vector2(0, 1), new Vector2(230, -38), new Vector2(380, 46));
            ctxTitle.fontStyle = FontStyle.Bold;
            ctxLine1 = UIKit.Label(context, "", 25, UIKit.Ink, TextAnchor.UpperLeft, false);
            UIKit.Place(ctxLine1.rectTransform, new Vector2(0, 1), new Vector2(230, -86), new Vector2(380, 36));
            ctxLine2 = UIKit.Label(context, "", 23, new Color(0.38f, 0.27f, 0.16f), TextAnchor.UpperLeft, false);
            UIKit.Place(ctxLine2.rectTransform, new Vector2(0, 1), new Vector2(230, -124), new Vector2(380, 70));

            progressFill = UIKit.Bar(context, new Color(0.35f, 0.65f, 1f), out progressRoot);
            UIKit.Place(progressRoot, new Vector2(0, 0), new Vector2(225, 26), new Vector2(380, 44));

            actionRow = UIKit.Rect(context, "Actions");
            actionRow.anchorMin = new Vector2(0, 0); actionRow.anchorMax = new Vector2(1, 1);
            actionRow.offsetMin = new Vector2(600, 28); actionRow.offsetMax = new Vector2(-34, -30);
            var hl = actionRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10; hl.childAlignment = TextAnchor.MiddleRight;
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = false; hl.childForceExpandHeight = true;
        }

        void ClearActions()
        {
            foreach (var a in actions) if (a.button != null) Destroy(a.button.gameObject);
            actions.Clear();
        }

        void AddAction(Func<string> text, Func<bool> enabled, Action onClick, bool red = false, string icon = null)
        {
            var b = UIKit.Button(actionRow, "", red ? UIKit.BtnRed : UIKit.BtnBlue, onClick, 26);
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 158; le.minWidth = 112; le.flexibleWidth = 0;
            var lbl = UIKit.ButtonLabel(b);
            lbl.resizeTextMaxSize = 26; lbl.resizeTextMinSize = 14;
            lbl.rectTransform.offsetMin = new Vector2(8, 14); lbl.rectTransform.offsetMax = new Vector2(-8, -8);
            if (icon != null)
            {
                var ic = UIKit.Image(b.transform, icon, false, 1, "Icon");
                ic.preserveAspect = true;
                UIKit.Place(ic.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(80, 72));
                ic.rectTransform.pivot = new Vector2(0.5f, 1f);
                lbl.rectTransform.offsetMax = new Vector2(-6, -80);
            }
            var ab = new ActionButton { button = b, label = lbl, enabled = enabled, text = text };
            actions.Add(ab);
            lbl.text = text();
            b.interactable = enabled == null || enabled();
        }

        void RebuildContext()
        {
            ClearActions();
            var sel = SelectionManager.I;
            bool units = sel.Selected.Count > 0;
            deselectBtn.gameObject.SetActive(units || sel.SelectedBuilding != null || sel.SelectedPlot != null || sel.InspectedEnemy != null);
            bool combat = false;
            foreach (var u in sel.Selected) if (u.pawn == null) combat = true;
            moveBtn.gameObject.SetActive(combat);
            attackBtn.gameObject.SetActive(combat);
            SetModeVisual();

            context.gameObject.SetActive(true);
            progressRoot.gameObject.SetActive(false);
            if (units) BuildUnitContext(sel.Selected);
            else if (sel.SelectedBuilding != null) BuildBuildingContext(sel.SelectedBuilding);
            else if (sel.SelectedPlot != null) BuildPlotContext(sel.SelectedPlot);
            else if (sel.InspectedEnemy != null) BuildEnemyContext(sel.InspectedEnemy);
            else context.gameObject.SetActive(false);
            RefreshContextDynamic();
        }

        void SetModeVisual()
        {
            if (SelectionManager.I == null) return;
            bool atk = SelectionManager.I.Mode == CommandMode.Attack;
            moveBtn.image.color = atk ? new Color(1, 1, 1, 0.55f) : Color.white;
            attackBtn.image.color = atk ? Color.white : new Color(1, 1, 1, 0.55f);
            UIKit.ButtonLabel(moveBtn).text = atk ? "Di chuyển" : "▸ Di chuyển";
            UIKit.ButtonLabel(attackBtn).text = atk ? "▸ Tấn công" : "Tấn công";
        }

        static string PortraitFor(UnitDef d)
        {
            if (d.visual == VisualKind.Sprite) return d.sprite;
            if (d.visual == VisualKind.TinySwords) return GameArt.Avatar(d.avatar);
            return null;
        }

        void SetPortrait(string key, Sprite fallback = null)
        {
            portrait.sprite = key != null ? GameArt.I.Spr(key) : fallback;
            portrait.enabled = portrait.sprite != null;
        }

        void BuildUnitContext(List<Unit> list)
        {
            if (list.Count == 1)
            {
                var u = list[0];
                SetPortrait(PortraitFor(u.def), u.def.visual == VisualKind.Prefab ? GameArt.I.Spr(GameArt.Icon(5)) : null);
                ctxTitle.text = u.def.name;
                if (u.pawn != null)
                {
                    AddAction(() => "Về lâu đài", null, () => { if (World.Castle != null) u.CommandMove(World.Castle.DoorPoint, false); });
                }
                else
                {
                    AddAction(() => "Chọn cùng loại", null, () => SelectionManager.I.SelectAllOfType(u.def.id));
                    AddAction(() => "Giữ tại chỗ", null, () => { u.ClearTarget(); u.StopMoving(); u.guardPos = u.Pos; });
                }
            }
            else
            {
                SetPortrait(GameArt.Avatar(1));
                ctxTitle.text = list.Count + " đơn vị";
                AddAction(() => "Giữ tại chỗ", null, () =>
                {
                    foreach (var u in SelectionManager.I.Selected) { u.ClearTarget(); u.StopMoving(); u.guardPos = u.Pos; }
                });
            }
        }

        void BuildBuildingContext(Building b)
        {
            SetPortrait(b.type == BuildingType.Tower && b.towerType != TowerType.Archer ? TowerCatalog.Get(b.towerType).baseSprite[Mathf.Clamp(b.level - 1, 0, 2)] : null, b.sr.sprite);
            ctxTitle.text = b.displayName + (b.type == BuildingType.Tower ? " (cấp " + b.level + ")" : "");
            var bm = BuildingManager.I;

            switch (b.type)
            {
                case BuildingType.Castle:
                    TrainAction(b, "pawn");
                    break;
                case BuildingType.Barracks:
                    TrainAction(b, "warrior");
                    TrainAction(b, "lancer");
                    TrainAction(b, "archer");
                    TrainAction(b, "monk");
                    break;
                case BuildingType.Tower:
                    AddAction(() =>
                        {
                            if (!bm.CanUpgrade(b, out int w, out int g)) return b.level >= 3 || !cfg.allowTowerUpgrade ? "Cấp tối đa" : "Nâng cấp";
                            return "Nâng cấp\n" + CostLines(w, g);
                        },
                        () => bm.CanUpgrade(b, out int w, out int g) && ResourceManager.I.CanAfford(w, g),
                        () => { bm.TryUpgrade(b); RebuildContext(); });
                    AddAction(() => "Ưu tiên\n<size=20>" + TowerCatalog.TargetModeName(b.targetMode) + "</size>", null,
                        () => bm.CycleTargetMode(b));
                    break;
            }
            if (b.plot != null && (b.type == BuildingType.Tower || b.type == BuildingType.House))
                AddAction(() => "Bán\n<size=20>+" + bm.SellValue(b) + "</size>", () => !b.constructing,
                    () => Panels.Confirm(overlay, "Bán " + b.displayName + "?\nNhận lại " + bm.SellValue(b) + " (gỗ + vàng).", "Bán", () => bm.TrySell(b)), true);
            AddAction(() => b.repairing ? "Đang sửa…" : (b.health.Ratio >= 0.999f ? "Sửa chữa\n<size=18>(máu đầy)</size>" : "Sửa chữa\n" + CostLines(bm.RepairCost(b), 0)),
                () => !b.constructing && !b.repairing && b.health.Ratio < 0.999f && ResourceManager.I.CanAfford(bm.RepairCost(b), 0),
                () => bm.TryRepair(b), true);
        }

        static string CostInline(int wood, int gold)
        {
            var parts = new List<string>();
            if (wood > 0) parts.Add("<color=#f3c79a>" + wood + "g</color>");
            if (gold > 0) parts.Add("<color=#ffe08a>" + gold + "v</color>");
            return "<size=18>" + string.Join(" ", parts) + "</size>";
        }

        static string CostLines(int wood, int gold)
        {
            var parts = new List<string>();
            if (gold > 0) parts.Add("<color=#ffe08a>" + gold + " vàng</color>");
            if (wood > 0) parts.Add("<color=#f3c79a>" + wood + " gỗ</color>");
            return "<size=20>" + (parts.Count == 0 ? "Miễn phí" : string.Join("\n", parts)) + "</size>";
        }

        void TrainAction(Building b, string id)
        {
            var d = UnitCatalog.Get(id);
            AddAction(() => d.name + "\n" + CostLines(d.costWood, d.costGold),
                () => !b.constructing && ResourceManager.I.CanAfford(d.costWood, d.costGold) && b.queue.Count < 5 &&
                      ResourceManager.I.PopulationUsed + d.pop <= ResourceManager.I.PopulationCap,
                () => BuildingManager.I.TryTrain(b, id));
        }

        void BuildPlotContext(BuildPlot p)
        {
            SetPortrait(GameArt.Icon(1));
            ctxTitle.text = "Ô xây dựng";
            foreach (var t in TowerCatalog.All)
            {
                if (!BuildingManager.I.IsTowerAllowed(t)) continue;
                var d = TowerCatalog.Get(t);
                AddAction(() => "<size=22>" + d.shortName + "</size>\n" + CostInline(d.wood[0], d.gold[0]),
                    () => ResourceManager.I.CanAfford(d.wood[0], d.gold[0]),
                    () => { if (BuildingManager.I.TryBuildTower(p, t)) SelectionManager.I.RefreshPlot(); }, false, d.icon);
            }
            AddAction(() => "<size=22>Nhà ở</size>\n" + CostInline(Balance.HouseWood, Balance.HouseGold),
                () => ResourceManager.I.CanAfford(Balance.HouseWood, Balance.HouseGold),
                () => { if (BuildingManager.I.TryBuild(p, BuildingType.House)) SelectionManager.I.RefreshPlot(); }, false,
                "Tiny Swords/Buildings/Blue Buildings/House1");
        }

        void BuildEnemyContext(Unit e)
        {
            SetPortrait(PortraitFor(e.def), GameArt.I.Spr(GameArt.Icon(5)));
            ctxTitle.text = e.def.name;
        }

        void RefreshContextDynamic()
        {
            var sel = SelectionManager.I;
            foreach (var a in actions)
            {
                if (a.button == null) continue;
                a.label.text = a.text();
                a.button.interactable = a.enabled == null || a.enabled();
            }
            if (!context.gameObject.activeSelf) return;
            progressRoot.gameObject.SetActive(false);

            if (sel.Selected.Count == 1)
            {
                var u = sel.Selected[0];
                ctxLine1.text = "Máu: " + Mathf.CeilToInt(u.health.current) + "/" + Mathf.CeilToInt(u.health.max) + (u.def.armor > 0 ? "   Giáp: " + u.def.armor : "");
                ctxLine2.text = u.pawn != null ? u.pawn.StatusText : u.def.role;
            }
            else if (sel.Selected.Count > 1)
            {
                var counts = new Dictionary<string, int>();
                foreach (var u in sel.Selected) { counts.TryGetValue(u.def.name, out int n); counts[u.def.name] = n + 1; }
                var sb = new StringBuilder();
                foreach (var kv in counts) { if (sb.Length > 0) sb.Append(", "); sb.Append(kv.Value).Append(' ').Append(kv.Key); }
                ctxLine1.text = sb.ToString();
                ctxLine2.text = SelectionManager.I.Mode == CommandMode.Attack ? "Chạm bản đồ: vừa đi vừa đánh" : "Chạm bản đồ để di chuyển, chạm địch để tấn công";
            }
            else if (sel.SelectedBuilding != null)
            {
                var b = sel.SelectedBuilding;
                ctxLine1.text = "Máu: " + Mathf.CeilToInt(b.health.current) + "/" + Mathf.CeilToInt(b.health.max);
                if (b.constructing)
                {
                    ctxLine2.text = "Đang xây dựng…";
                    progressRoot.gameObject.SetActive(true);
                    progressFill.fillAmount = b.buildProgress;
                }
                else if (b.queue.Count > 0)
                {
                    var d = UnitCatalog.Get(b.queue[0]);
                    ctxLine2.text = "Đang huấn luyện: " + d.name + (b.queue.Count > 1 ? "  (+" + (b.queue.Count - 1) + " chờ)" : "");
                    progressRoot.gameObject.SetActive(true);
                    progressFill.fillAmount = Mathf.Clamp01(b.trainTimer / d.trainTime);
                }
                else
                {
                    switch (b.type)
                    {
                        case BuildingType.Castle: ctxLine2.text = "Công trình mục tiêu — bị phá hủy là thua.\nTuyển Thợ để khai thác tài nguyên."; break;
                        case BuildingType.Barracks: ctxLine2.text = "Tuyển quân. Quân số: " + ResourceManager.I.PopulationUsed + "/" + ResourceManager.I.PopulationCap; break;
                        case BuildingType.Tower:
                            ctxLine2.text = TowerStats(b); break;
                        case BuildingType.House: ctxLine2.text = "+" + Balance.HousePopulation + " giới hạn quân số"; break;
                        default: ctxLine2.text = "Nhà của dân làng — hãy bảo vệ!"; break;
                    }
                    if (b.repairing) ctxLine2.text = "Đang sửa chữa…";
                }
            }
            else if (sel.SelectedPlot != null)
            {
                ctxLine1.text = "Chọn công trình để xây";
                ctxLine2.text = "g = gỗ, v = vàng. Mỗi tháp có 3 cấp.\nChi tiết các tháp: xem Hướng dẫn (?).";
            }
            else if (sel.InspectedEnemy != null)
            {
                var e = sel.InspectedEnemy;
                ctxLine1.text = "Máu: " + Mathf.CeilToInt(e.health.current) + "/" + Mathf.CeilToInt(e.health.max) + (e.def.armor > 0 ? "   Giáp: " + e.def.armor : "");
                ctxLine2.text = "Quân địch • Sát thương " + e.def.damage + (e.def.IsRanged ? " • Đánh xa" : "") + (e.def.flying ? " • Bay" : "") + (e.def.boss ? " • THỦ LĨNH" : "");
            }
        }

        static string TowerStats(Building b)
        {
            var d = b.tower.Def;
            int l = Mathf.Clamp(b.level - 1, 0, 2);
            var s = "Sát thương " + d.damage[l] + " • Tầm " + d.range[l].ToString("0.0");
            if (d.splash[l] > 0) s += " • Lan " + d.splash[l].ToString("0.0");
            if (d.burnDps[l] > 0) s += " • Thiêu " + d.burnDps[l] + "/s";
            if (d.slow[l] > 0) s += " • Chậm " + Mathf.RoundToInt(d.slow[l] * 100) + "%";
            if (d.stun[l] > 0) s += " • Choáng " + d.stun[l].ToString("0.0") + "s";
            if (d.pierce) s += " • Xuyên giáp";
            if (!d.hitsAir) s += "\nKhông đánh được quái bay";
            else if (d.type == TowerType.Archer && b.level >= 3) s += " • 2 cung thủ";
            return s;
        }

        // ======================= Cập nhật =======================

        void RefreshResources()
        {
            var rm = ResourceManager.I;
            if (rm == null) return;
            woodText.text = rm.Wood.ToString();
            goldText.text = rm.Gold.ToString();
            int used = rm.PopulationUsed, cap = rm.PopulationCap;
            popText.text = used + "/" + cap;
            popText.color = used >= cap ? new Color(1f, 0.55f, 0.45f) : Color.white;
            RefreshContextDynamic();
        }

        void RefreshWave()
        {
            var wm = WaveManager.I;
            if (wm == null) return;
            string obj = cfg.objective;
            if (wm.Endless) obj += " • Đợt " + Mathf.Max(1, wm.DisplayWave) + " • Kỷ lục " + SaveManager.BestEndless;
            else obj += " • Đợt " + Mathf.Max(1, wm.DisplayWave) + "/" + wm.TotalWaves;
            if (cfg.protectVillage) obj += " • Nhà dân " + GameManager.I.VillagesAlive + "/" + GameManager.I.VillagesTotal;
            objectiveText.text = obj;
            callEarlyBtn.gameObject.SetActive(wm.CanCallEarly);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (gm.State == GameState.Playing) gm.Pause();
                else if (gm.State == GameState.Paused) gm.Resume();
            }

            if (World.Castle != null)
            {
                var h = World.Castle.health;
                castleFill.fillAmount = h.Ratio;
                castleFill.color = Color.Lerp(new Color(0.95f, 0.3f, 0.2f), new Color(0.45f, 0.85f, 0.35f), h.Ratio);
                castleText.text = "Lâu đài  " + Mathf.CeilToInt(h.current) + "/" + Mathf.CeilToInt(h.max);
            }

            var wm = WaveManager.I;
            if (wm != null)
            {
                switch (wm.Phase)
                {
                    case WavePhase.Waiting: waveText.text = "Chuẩn bị phòng thủ…"; break;
                    case WavePhase.Preparing: waveText.text = "Đợt " + (wm.WaveIndex + 2) + " sau " + Util.Time(wm.PrepRemaining); break;
                    case WavePhase.Spawning:
                    case WavePhase.Fighting: waveText.text = "Còn " + wm.EnemiesRemaining + " kẻ địch"; break;
                    default: waveText.text = "Đã đẩy lùi mọi đợt!"; break;
                }
                callEarlyBtn.gameObject.SetActive(wm.CanCallEarly);
            }
            UIKit.ButtonLabel(speedBtn).text = gm.Speed > 1.5f ? "x2" : "x1";

            castleWarnTimer -= Time.unscaledDeltaTime;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.25f;
                RefreshResources();
            }
        }

        void OnCastleHit(Health h, Health src)
        {
            if (castleWarnTimer > 0f) return;
            castleWarnTimer = 10f;
            GameEvents.Message("Lâu đài đang bị tấn công!", MessageKind.Danger);
        }

        // ======================= Thông báo =======================

        void ShowMessage(string text, MessageKind kind)
        {
            if (messageRoot == null) return;
            messages.RemoveAll(m => m == null);
            foreach (var old in messages)
            {
                var ot = old.GetComponentInChildren<Text>();
                if (ot != null && ot.text == text) { old.GetComponent<FadeAway>().Restart(); return; }
            }
            while (messages.Count >= 3)
            {
                if (messages[0] != null) Destroy(messages[0]);
                messages.RemoveAt(0);
            }
            var pill = UIKit.Image(messageRoot, UIKit.PaperDark, true, 2f, "Message");
            pill.color = new Color(1, 1, 1, 0.92f);
            var t = UIKit.Label(pill.transform, text, 28, KindColor(kind));
            UIKit.Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(30, 10);
            t.rectTransform.offsetMax = new Vector2(-30, -6);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            float w = Mathf.Clamp(t.preferredWidth + 80f, 300f, 900f);
            pill.rectTransform.sizeDelta = new Vector2(w, 70);
            pill.gameObject.AddComponent<FadeAway>().life = kind == MessageKind.Danger ? 3.2f : 2.4f;
            messages.Add(pill.gameObject);
            LayoutMessages();
        }

        void LayoutMessages()
        {
            messages.RemoveAll(m => m == null);
            for (int i = 0; i < messages.Count; i++)
            {
                var rt = (RectTransform)messages[messages.Count - 1 - i].transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0, -i * 76);
            }
        }

        static Color KindColor(MessageKind k)
        {
            switch (k)
            {
                case MessageKind.Good: return new Color(0.6f, 1f, 0.55f);
                case MessageKind.Warning: return new Color(1f, 0.85f, 0.4f);
                case MessageKind.Danger: return new Color(1f, 0.5f, 0.42f);
                default: return UIKit.Cream;
            }
        }

        // ======================= Trạng thái game =======================

        void OnState(GameState s)
        {
            switch (s)
            {
                case GameState.Intro:
                    root.gameObject.SetActive(true);
                    string header = cfg.endless ? "Chế độ Sinh tồn — " + cfg.title : "Màn " + cfg.levelIndex + " — " + cfg.title;
                    if (cfg.intro != null && cfg.intro.Count > 0)
                        DialogPanel.Show(overlay, header, cfg.intro, () => GameManager.I.StartPlaying());
                    else GameManager.I.StartPlaying();
                    break;
                case GameState.Playing:
                    if (pauseMenu != null) Destroy(pauseMenu);
                    break;
                case GameState.Paused:
                    if (pauseMenu == null) pauseMenu = BuildPause();
                    break;
                case GameState.Victory:
                    if (pauseMenu != null) Destroy(pauseMenu);
                    if (cfg.outro != null && cfg.outro.Count > 0)
                        DialogPanel.Show(overlay, "Chiến thắng!", cfg.outro, () => resultPanel = BuildResult(true));
                    else resultPanel = BuildResult(true);
                    break;
                case GameState.Defeat:
                    if (pauseMenu != null) Destroy(pauseMenu);
                    resultPanel = BuildResult(false);
                    break;
            }
        }

        GameObject BuildPause()
        {
            var dim = UIKit.Dim(overlay, 0.6f);
            var panel = UIKit.Panel(dim.transform, UIKit.Banner);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(760, 780));
            var ribbon = UIKit.Image(panel.transform, UIKit.Ribbon(1), true, 2f, "Ribbon");
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 60), new Vector2(620, 140));
            var title = UIKit.Label(ribbon.transform, "TẠM DỪNG", 52, Color.white);
            UIKit.Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(0, 16);

            string[] labels = { "Tiếp tục", "Chơi lại", "Hướng dẫn", "Về menu" };
            Action[] acts =
            {
                () => GameManager.I.Resume(),
                () => Panels.Confirm(overlay, "Chơi lại màn này từ đầu?", "Chơi lại", () => GameManager.I.Restart()),
                () => Panels.Help(overlay),
                () => Panels.Confirm(overlay, "Thoát về menu chính?\nTiến độ màn này sẽ không được lưu.", "Về menu", () => GameManager.I.ToMenu()),
            };
            for (int i = 0; i < labels.Length; i++)
            {
                var b = UIKit.Button(panel.transform, labels[i], i == 0 ? UIKit.BtnBlue : (i == 3 ? UIKit.BtnRed : UIKit.BtnBlue), acts[i], 38);
                UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0, -130 - i * 118), new Vector2(460, 108));
            }
            Panels.SettingsButtons(panel.transform, new Vector2(0.5f, 0f), new Vector2(-120, 60), new Vector2(230, 96), 10);
            return dim.gameObject;
        }

        GameObject BuildResult(bool win)
        {
            var gm = GameManager.I;
            var dim = UIKit.Dim(overlay, 0.6f);
            var panel = UIKit.Panel(dim.transform, UIKit.Banner);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(900, 760));
            var ribbon = UIKit.Image(panel.transform, UIKit.Ribbon(win ? 1 : 2), true, 2f, "Ribbon");
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 66), new Vector2(720, 150));
            bool endless = WaveManager.I != null && WaveManager.I.Endless;
            var title = UIKit.Label(ribbon.transform, win ? "CHIẾN THẮNG!" : (endless ? "KẾT THÚC" : "THẤT BẠI"), 58, Color.white);
            UIKit.Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(0, 18);

            float y = -120;
            if (win)
            {
                for (int i = 0; i < 3; i++)
                {
                    var st = UIKit.Star(panel.transform, i < gm.StarsEarned);
                    float size = i == 1 ? 130 : 105;
                    UIKit.Place(st.rectTransform, new Vector2(0.5f, 1f), new Vector2((i - 1) * 150, y - (i == 1 ? 0 : 18)), new Vector2(size, size));
                    st.rectTransform.pivot = new Vector2(0.5f, 1f);
                    st.gameObject.AddComponent<PopIn>().delay = 0.3f + i * 0.25f;
                }
                y -= 160;
            }
            else
            {
                string reasonText = endless
                    ? "Bạn đã trụ vững <b>" + WaveManager.I.WavesCleared + "</b> đợt!\nKỷ lục: " + SaveManager.BestEndless + " đợt"
                    : gm.DefeatReason;
                var reason = UIKit.Label(panel.transform, reasonText, 34, new Color(0.55f, 0.15f, 0.1f), TextAnchor.MiddleCenter, false);
                UIKit.Place(reason.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, y - 10), new Vector2(720, 110));
                y -= 140;
            }

            float castle = World.Castle != null ? World.Castle.health.Ratio : 0f;
            string stats =
                "Thời gian:  <b>" + Util.Time(gm.Elapsed) + "</b>\n" +
                "Địch bị hạ:  <b>" + gm.Kills + "</b>\n" +
                "Quân hy sinh:  <b>" + gm.UnitsLost + "</b>\n" +
                "Máu lâu đài:  <b>" + Mathf.RoundToInt(castle * 100) + "%</b>" +
                (endless ? "\nĐợt đạt tới:  <b>" + Mathf.Max(1, WaveManager.I.DisplayWave) + "</b>"
                    : (win ? "\nĐã đẩy lùi:  <b>" + WaveManager.I.TotalWaves + " đợt</b>" : "\nĐợt:  <b>" + Mathf.Max(1, WaveManager.I.DisplayWave) + "/" + WaveManager.I.TotalWaves + "</b>"));
            var st2 = UIKit.Label(panel.transform, stats, 32, UIKit.Ink, TextAnchor.UpperCenter, false);
            UIKit.Place(st2.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(700, 230));

            if (win)
            {
                bool last = cfg.levelIndex >= SceneLoader.LevelCount;
                var next = UIKit.Button(panel.transform, last ? "Đoạn kết ▸" : "Màn tiếp ▸", UIKit.BtnBlue, () => gm.NextLevel(), 36);
                UIKit.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(250, 56), new Vector2(270, 108));
                var again = UIKit.Button(panel.transform, "Chơi lại", UIKit.BtnBlue, () => gm.Restart(), 36);
                UIKit.Place((RectTransform)again.transform, new Vector2(0.5f, 0f), new Vector2(-10, 56), new Vector2(230, 108));
                var menu = UIKit.Button(panel.transform, "Về menu", UIKit.BtnRed, () => gm.ToMenu(), 36);
                UIKit.Place((RectTransform)menu.transform, new Vector2(0.5f, 0f), new Vector2(-260, 56), new Vector2(230, 108));
            }
            else
            {
                var again = UIKit.Button(panel.transform, "Thử lại", UIKit.BtnBlue, () => gm.Restart(), 38);
                UIKit.Place((RectTransform)again.transform, new Vector2(0.5f, 0f), new Vector2(150, 56), new Vector2(270, 108));
                var menu = UIKit.Button(panel.transform, "Về menu", UIKit.BtnRed, () => gm.ToMenu(), 38);
                UIKit.Place((RectTransform)menu.transform, new Vector2(0.5f, 0f), new Vector2(-150, 56), new Vector2(270, 108));
            }
            return dim.gameObject;
        }
    }

    /// <summary>Mờ dần rồi tự huỷ (thông báo).</summary>
    public class FadeAway : MonoBehaviour
    {
        public float life = 2.4f;
        float t;
        CanvasGroup g;
        void Start() { g = gameObject.AddComponent<CanvasGroup>(); g.blocksRaycasts = false; g.interactable = false; }
        public void Restart() { t = Mathf.Min(t, 0.15f); }
        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (g != null) g.alpha = t < 0.15f ? t / 0.15f : Mathf.Clamp01((life - t) / 0.4f);
            if (t >= life) Destroy(gameObject);
        }
    }

    /// <summary>Hiệu ứng bật lên (ngôi sao kết quả).</summary>
    public class PopIn : MonoBehaviour
    {
        public float delay;
        float t;
        void Start() { transform.localScale = Vector3.zero; }
        void Update()
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01((t - delay) / 0.3f);
            float s = k < 1f ? Mathf.Sin(k * Mathf.PI * 0.5f) * 1.15f : 1f;
            transform.localScale = Vector3.one * s;
            if (k >= 1f) enabled = false;
        }
    }
}
