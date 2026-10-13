using UnityEngine;

namespace Oath
{
    /// <summary>Ô xây dựng hợp lệ: người chơi chạm để xây Tháp canh hoặc Nhà ở.</summary>
    public class BuildPlot : MonoBehaviour
    {
        public RectInt cells;
        public Building building;
        SpriteRenderer square, icon, ring;
        float t;

        public Vector2 Center => new Vector2(cells.x + cells.width * 0.5f, cells.y + 0.5f);
        public bool Occupied => building != null && building.health != null && building.health.Alive;

        public static BuildPlot Create(RectInt cells)
        {
            var go = new GameObject("BuildPlot");
            if (World.Root != null) go.transform.SetParent(World.Root, false);
            var p = go.AddComponent<BuildPlot>();
            p.cells = cells;
            go.transform.position = p.Center;
            p.square = Util.NewSprite("Square", go.transform, Util.PlotSquare(), Vector3.zero, Util.OrderGroundDecal);
            p.square.transform.localScale = new Vector3(cells.width - 0.1f, 0.9f, 1f);
            p.square.color = new Color(1f, 1f, 1f, 0.85f);
            p.icon = Util.NewSprite("Icon", go.transform, GameArt.I.Spr(GameArt.Icon(1)), new Vector3(0, 0.35f, 0), Util.SortOrder(cells.y));
            p.icon.transform.localScale = Vector3.one * 0.6f;
            p.ring = Util.NewSprite("Ring", go.transform, Util.Ellipse(true), Vector3.zero, Util.OrderGroundDecal + 1);
            p.ring.color = new Color(1f, 0.95f, 0.5f, 1f);
            p.ring.transform.localScale = new Vector3(cells.width + 0.6f, cells.width + 0.6f, 1f);
            p.ring.enabled = false;
            World.Plots.Add(p);
            return p;
        }

        public void SetSelected(bool on) { ring.enabled = on && !Occupied; }

        public bool HitTest(Vector2 p)
        {
            return !Occupied && new Rect(cells.x - 0.1f, cells.y - 0.1f, cells.width + 0.2f, 1.4f).Contains(p);
        }

        void Update()
        {
            bool show = !Occupied;
            if (square.enabled != show) { square.enabled = show; icon.enabled = show; if (!show) ring.enabled = false; }
            if (!show) return;
            t += Time.deltaTime;
            icon.transform.localPosition = new Vector3(0, 0.35f + Mathf.Sin(t * 3f) * 0.08f, 0);
        }
    }
}
