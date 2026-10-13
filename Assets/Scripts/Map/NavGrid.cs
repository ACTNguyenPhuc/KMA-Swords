using System.Collections.Generic;
using UnityEngine;

namespace Oath
{
    /// <summary>Lưới ô đi được và tìm đường A* 8 hướng (không cắt góc vật cản).</summary>
    public class NavGrid
    {
        public readonly int W, H;
        readonly bool[] walk;
        readonly bool[] road;
        readonly int[] blockCount;

        // Bộ nhớ dùng lại cho A*
        readonly float[] g;
        readonly int[] parent;
        readonly int[] stamp;
        readonly bool[] closed;
        int curStamp;
        readonly List<int> heap = new List<int>();
        readonly float[] f;

        public NavGrid(MapData m)
        {
            W = m.W; H = m.H;
            int n = W * H;
            walk = new bool[n]; road = new bool[n]; blockCount = new int[n];
            g = new float[n]; f = new float[n]; parent = new int[n]; stamp = new int[n]; closed = new bool[n];
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                {
                    int i = y * W + x;
                    walk[i] = m.land[x, y] && !m.plateau[x, y] && !m.blockedStatic[x, y];
                    road[i] = m.road[x, y];
                }
        }

        public bool Walkable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return false;
            int i = y * W + x;
            return walk[i] && blockCount[i] == 0;
        }

        public bool IsRoad(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && road[y * W + x];

        public bool IsWalkable(Vector2 p) => Walkable(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));

        public static Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));

        public void SetBlocked(RectInt r, bool blocked)
        {
            foreach (var c in r.allPositionsWithin)
            {
                if (c.x < 0 || c.y < 0 || c.x >= W || c.y >= H) continue;
                int i = c.y * W + c.x;
                blockCount[i] = Mathf.Max(0, blockCount[i] + (blocked ? 1 : -1));
            }
        }

        /// <summary>Trượt dọc vật cản thay vì xuyên qua.</summary>
        public Vector2 ClampMove(Vector2 from, Vector2 to)
        {
            if (IsWalkable(to)) return to;
            if (!IsWalkable(from)) return to; // đang kẹt trong vật cản: cho thoát ra
            var a = new Vector2(to.x, from.y);
            if (IsWalkable(a)) return a;
            var b = new Vector2(from.x, to.y);
            if (IsWalkable(b)) return b;
            return from;
        }

        public bool LineWalkable(Vector2 a, Vector2 b)
        {
            float d = Vector2.Distance(a, b);
            int steps = Mathf.CeilToInt(d / 0.2f);
            for (int i = 1; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
                if (!IsWalkable(p)) return false;
            }
            return true;
        }

        bool LineOnRoad(Vector2 a, Vector2 b)
        {
            float d = Vector2.Distance(a, b);
            int steps = Mathf.CeilToInt(d / 0.2f);
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
                var c = Cell(p);
                if (!Walkable(c.x, c.y) || !IsRoad(c.x, c.y)) return false;
            }
            return true;
        }

        public Vector2Int NearestWalkable(Vector2Int c, int maxR)
        {
            if (Walkable(c.x, c.y)) return c;
            for (int r = 1; r <= maxR; r++)
            {
                Vector2Int best = c;
                float bd = float.MaxValue;
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                        int x = c.x + dx, y = c.y + dy;
                        if (!Walkable(x, y)) continue;
                        float d = dx * dx + dy * dy;
                        if (d < bd) { bd = d; best = new Vector2Int(x, y); }
                    }
                if (bd < float.MaxValue) return best;
            }
            return c;
        }

        /// <summary>
        /// Tìm đường từ <paramref name="from"/> tới <paramref name="to"/>.
        /// enemyRoute: ưu tiên ô đường (chi phí cỏ cao) và chỉ làm mượt trên đường.
        /// </summary>
        public List<Vector2> FindPath(Vector2 from, Vector2 to, bool enemyRoute)
        {
            var s = NearestWalkable(Cell(from), 4);
            var tCell = Cell(to);
            bool exactGoal = Walkable(tCell.x, tCell.y);
            var t = NearestWalkable(tCell, 10);
            if (!Walkable(s.x, s.y) || !Walkable(t.x, t.y)) return null;

            var cells = AStar(s, t, enemyRoute);
            if (cells == null) return null;

            var pts = new List<Vector2>(cells.Count);
            foreach (var c in cells) pts.Add(new Vector2(c.x + 0.5f, c.y + 0.5f));
            if (exactGoal && pts.Count > 0) pts[pts.Count - 1] = to;
            if (pts.Count == 0) pts.Add(exactGoal ? to : new Vector2(t.x + 0.5f, t.y + 0.5f));

            // Làm mượt (kéo dây)
            var outPts = new List<Vector2>();
            Vector2 cur = from;
            int k = 0;
            while (k < pts.Count)
            {
                int far = k;
                for (int j = pts.Count - 1; j > k; j--)
                {
                    bool ok = enemyRoute ? (LineOnRoad(cur, pts[j]) && j - k < 4) : LineWalkable(cur, pts[j]);
                    if (ok) { far = j; break; }
                }
                outPts.Add(pts[far]);
                cur = pts[far];
                k = far + 1;
            }
            return outPts;
        }

        List<Vector2Int> AStar(Vector2Int s, Vector2Int t, bool enemyRoute)
        {
            curStamp++;
            heap.Clear();
            int si = s.y * W + s.x, ti = t.y * W + t.x;
            Touch(si);
            g[si] = 0; f[si] = H8(s, t); parent[si] = -1;
            Push(si);
            int iterations = 0;
            while (heap.Count > 0 && iterations++ < 20000)
            {
                int cur = Pop();
                if (cur == ti) break;
                if (closed[cur]) continue;
                closed[cur] = true;
                int cx = cur % W, cy = cur / W;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = cx + dx, ny = cy + dy;
                        if (!Walkable(nx, ny)) continue;
                        if (dx != 0 && dy != 0 && (!Walkable(cx + dx, cy) || !Walkable(cx, cy + dy))) continue;
                        int ni = ny * W + nx;
                        Touch(ni);
                        if (closed[ni]) continue;
                        float step = (dx != 0 && dy != 0) ? 1.4142f : 1f;
                        if (enemyRoute && !road[ni]) step *= 8f;
                        float ng = g[cur] + step;
                        if (ng < g[ni])
                        {
                            g[ni] = ng;
                            f[ni] = ng + H8(new Vector2Int(nx, ny), t);
                            parent[ni] = cur;
                            Push(ni);
                        }
                    }
            }
            if (stamp[ti] != curStamp || (parent[ti] == -1 && ti != si)) return null;

            var result = new List<Vector2Int>();
            int c = ti;
            int guard = 0;
            while (c != -1 && guard++ < W * H)
            {
                result.Add(new Vector2Int(c % W, c / W));
                c = parent[c];
            }
            result.Reverse();
            if (result.Count > 1) result.RemoveAt(0); // bỏ ô xuất phát
            return result;
        }

        void Touch(int i)
        {
            if (stamp[i] == curStamp) return;
            stamp[i] = curStamp;
            g[i] = float.MaxValue;
            f[i] = float.MaxValue;
            parent[i] = -1;
            closed[i] = false;
        }

        static float H8(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
            return (dx + dy) + (1.4142f - 2f) * Mathf.Min(dx, dy);
        }

        // Binary heap theo f
        void Push(int i)
        {
            heap.Add(i);
            int k = heap.Count - 1;
            while (k > 0)
            {
                int p = (k - 1) >> 1;
                if (f[heap[p]] <= f[heap[k]]) break;
                (heap[p], heap[k]) = (heap[k], heap[p]);
                k = p;
            }
        }

        int Pop()
        {
            int top = heap[0];
            int last = heap.Count - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);
            int k = 0;
            while (true)
            {
                int l = k * 2 + 1, r = l + 1, m = k;
                if (l < heap.Count && f[heap[l]] < f[heap[m]]) m = l;
                if (r < heap.Count && f[heap[r]] < f[heap[m]]) m = r;
                if (m == k) break;
                (heap[m], heap[k]) = (heap[k], heap[m]);
                k = m;
            }
            return top;
        }
    }
}
