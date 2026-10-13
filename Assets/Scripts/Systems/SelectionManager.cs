using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oath
{
    public enum CommandMode { Move, Attack }

    /// <summary>Chọn đơn vị/công trình/ô xây dựng và chuyển chạm trên bản đồ thành lệnh.</summary>
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager I { get; private set; }

        public readonly List<Unit> Selected = new List<Unit>();
        public Building SelectedBuilding { get; private set; }
        public BuildPlot SelectedPlot { get; private set; }
        public Unit InspectedEnemy { get; private set; }
        public CommandMode Mode { get; private set; } = CommandMode.Move;

        public event Action OnSelectionChanged;
        public event Action<Vector2> OnGroundCommand;

        public void Init() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        public bool HasUnits => Selected.Count > 0;

        public void SetMode(CommandMode m)
        {
            Mode = m;
            OnSelectionChanged?.Invoke();
        }

        // ---------------- Chạm trên bản đồ ----------------

        public void OnTap(Vector2 w)
        {
            if (GameManager.I == null || GameManager.I.State != GameState.Playing) return;

            var friendly = HitUnit(w, Team.Player);
            if (friendly != null)
            {
                SelectUnit(friendly);
                AudioManager.Play("select", 0.5f);
                return;
            }

            var enemy = HitUnit(w, Team.Enemy);
            if (enemy != null)
            {
                if (HasCombatUnits())
                {
                    foreach (var u in Selected) u.CommandAttack(enemy.health);
                    TapMarker.Spawn(enemy.Pos, new Color(1f, 0.3f, 0.25f));
                    AudioManager.Play("command", 0.5f);
                }
                else
                {
                    ClearAll(false);
                    InspectedEnemy = enemy;
                    enemy.SetSelected(true);
                    Changed();
                }
                return;
            }

            foreach (var b in World.Buildings)
            {
                if (!b.health.Alive || !b.HitTest(w)) continue;
                SelectBuilding(b);
                AudioManager.Play("select", 0.5f);
                return;
            }

            foreach (var p in World.Plots)
            {
                if (!p.HitTest(w)) continue;
                ClearAll(false);
                SelectedPlot = p;
                p.SetSelected(true);
                AudioManager.Play("select", 0.5f);
                Changed();
                return;
            }

            if (HasUnits)
            {
                // Thợ được chọn + chạm vào nguồn tài nguyên → khai thác
                var node = HitNode(w);
                if (node != null)
                {
                    bool any = false;
                    foreach (var u in Selected) if (u.pawn != null) { u.pawn.AssignNode(node); any = true; }
                    if (any) { TapMarker.Spawn(node.Pos, new Color(1f, 0.85f, 0.3f)); AudioManager.Play("command", 0.5f); return; }
                }

                if (World.Nav == null || !World.Nav.IsWalkable(w))
                {
                    var c = World.Nav != null ? World.Nav.NearestWalkable(NavGrid.Cell(w), 2) : NavGrid.Cell(w);
                    if (World.Nav == null || !World.Nav.Walkable(c.x, c.y))
                    {
                        GameEvents.Message("Không thể di chuyển tới đó.", MessageKind.Warning);
                        AudioManager.Play("error", 0.5f);
                        return;
                    }
                    w = MapData.CellCenter(c);
                }
                IssueMove(w, Mode == CommandMode.Attack);
                return;
            }

            ClearAll(true);
        }

        void IssueMove(Vector2 w, bool attackMove)
        {
            var points = FormationPoints(w, Selected.Count);
            // Gán điểm gần nhất cho từng đơn vị
            var used = new bool[points.Count];
            var order = new List<Unit>(Selected);
            order.Sort((a, b) => Vector2.Distance(a.Pos, w).CompareTo(Vector2.Distance(b.Pos, w)));
            foreach (var u in order)
            {
                int best = -1;
                float bd = float.MaxValue;
                for (int i = 0; i < points.Count; i++)
                {
                    if (used[i]) continue;
                    float d = Vector2.Distance(u.Pos, points[i]);
                    if (d < bd) { bd = d; best = i; }
                }
                Vector2 dest = best >= 0 ? points[best] : w;
                if (best >= 0) used[best] = true;
                u.CommandMove(dest, attackMove);
            }
            TapMarker.Spawn(w, attackMove ? new Color(1f, 0.45f, 0.3f) : new Color(0.5f, 1f, 0.55f));
            AudioManager.Play("command", 0.5f);
            OnGroundCommand?.Invoke(w);
        }

        static List<Vector2> FormationPoints(Vector2 c, int n)
        {
            var pts = new List<Vector2>();
            if (World.Nav == null || World.Nav.IsWalkable(c)) pts.Add(c);
            float r = 0.75f;
            int ring = 1;
            while (pts.Count < n && ring < 6)
            {
                int count = ring * 6;
                for (int i = 0; i < count && pts.Count < n * 2; i++)
                {
                    float a = (i / (float)count) * Mathf.PI * 2f + ring * 0.4f;
                    Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f) * r;
                    if (World.Nav == null || World.Nav.IsWalkable(p)) pts.Add(p);
                }
                r += 0.7f;
                ring++;
            }
            if (pts.Count == 0) pts.Add(c);
            return pts;
        }

        bool HasCombatUnits()
        {
            foreach (var u in Selected)
                if (u.def.attack == AttackKind.Melee || u.def.attack == AttackKind.Ranged) return true;
            return false;
        }

        public static Unit HitUnit(Vector2 w, Team team)
        {
            Unit best = null;
            float bd = float.MaxValue;
            foreach (var u in World.Units)
            {
                if (!u.IsAlive || u.def.team != team) continue;
                Vector2 c = u.Pos + Vector2.up * u.VisualHeight * 0.45f;
                float rx = Mathf.Max(0.45f, u.def.bodyRadius * 1.4f);
                float ry = Mathf.Max(0.6f, u.VisualHeight * 0.6f);
                float dx = (w.x - c.x) / rx, dy = (w.y - c.y) / ry;
                float d = dx * dx + dy * dy;
                if (d <= 1f && d < bd) { bd = d; best = u; }
            }
            return best;
        }

        static ResourceNode HitNode(Vector2 w)
        {
            foreach (var n in World.ResourceNodes)
                if (n != null && n.amount > 0 && Vector2.Distance(w, n.Pos + Vector2.up * 0.5f) < 0.9f) return n;
            return null;
        }

        // ---------------- Thao tác chọn ----------------

        public void SelectUnit(Unit u)
        {
            ClearAll(false);
            Selected.Add(u);
            u.SetSelected(true);
            Changed();
        }

        public void SelectAllArmy()
        {
            ClearAll(false);
            foreach (var u in World.Units)
            {
                if (!u.IsAlive || u.def.team != Team.Player || u.pawn != null) continue;
                Selected.Add(u);
                u.SetSelected(true);
            }
            if (Selected.Count == 0) GameEvents.Message("Chưa có quân nào — hãy tuyển ở Doanh trại.", MessageKind.Info);
            else AudioManager.Play("select", 0.5f);
            Changed();
        }

        public void SelectAllOfType(string id)
        {
            ClearAll(false);
            foreach (var u in World.Units)
            {
                if (!u.IsAlive || u.def.team != Team.Player || u.def.id != id) continue;
                Selected.Add(u);
                u.SetSelected(true);
            }
            Changed();
        }

        public void SelectBuilding(Building b)
        {
            ClearAll(false);
            SelectedBuilding = b;
            b.SetSelected(true);
            Changed();
        }

        public void ClearAll(bool notify = true)
        {
            foreach (var u in Selected) if (u != null) u.SetSelected(false);
            Selected.Clear();
            if (SelectedBuilding != null) SelectedBuilding.SetSelected(false);
            SelectedBuilding = null;
            if (SelectedPlot != null) SelectedPlot.SetSelected(false);
            SelectedPlot = null;
            if (InspectedEnemy != null) InspectedEnemy.SetSelected(false);
            InspectedEnemy = null;
            if (notify) Changed();
        }

        public void Remove(Unit u)
        {
            if (Selected.Remove(u)) Changed();
            if (InspectedEnemy == u) { InspectedEnemy = null; Changed(); }
        }

        public void OnBuildingDestroyed(Building b)
        {
            if (SelectedBuilding == b) { SelectedBuilding = null; Changed(); }
        }

        public void RefreshPlot()
        {
            if (SelectedPlot != null && SelectedPlot.Occupied)
            {
                var b = SelectedPlot.building;
                SelectedPlot.SetSelected(false);
                SelectedPlot = null;
                SelectBuilding(b);
            }
        }

        void Changed() => OnSelectionChanged?.Invoke();
    }

    /// <summary>Vòng tròn thu nhỏ đánh dấu điểm ra lệnh.</summary>
    public class TapMarker : MonoBehaviour
    {
        SpriteRenderer sr;
        float t;
        Color c;

        public static void Spawn(Vector2 p, Color color)
        {
            var go = new GameObject("TapMarker");
            if (World.Root != null) go.transform.SetParent(World.Root, false);
            go.transform.position = p;
            var m = go.AddComponent<TapMarker>();
            m.c = color;
            m.sr = go.AddComponent<SpriteRenderer>();
            m.sr.sprite = Util.Ellipse(true);
            m.sr.sortingOrder = Util.OrderGroundDecal + 10;
            m.Update();
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.5f);
            transform.localScale = Vector3.one * Mathf.Lerp(1.6f, 0.6f, k);
            sr.color = new Color(c.r, c.g, c.b, 1f - k);
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
