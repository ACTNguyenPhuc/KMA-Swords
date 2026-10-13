using System.Collections.Generic;
using UnityEngine;

namespace Oath
{
    /// <summary>
    /// Bản đồ dạng lưới ký tự. Chú giải:
    ///  '~' hoặc ' ' nước      '.' cỏ            '=' đường (địch đi)     '^' cao nguyên (không đi được)
    ///  'T' cây (gỗ)           '$' mỏ vàng       'b' bụi cây             'o' đá nhỏ          's' cừu
    ///  'w' đá dưới nước        'd' vịt cao su    'C' lâu đài (ô giữa hàng dưới, khối 5x2)
    ///  'K' doanh trại (ô giữa hàng dưới, khối 3x2)   '#' ô xây dựng (2 ô ngang)   't' tháp có sẵn (2 ô)
    ///  'H' nhà dân làng (2 ô)  'S' điểm xuất hiện địch (trên đường)   'R' điểm tập kết quân
    /// </summary>
    public class MapData
    {
        public int W, H;
        public char[,] c;
        public bool[,] land, road, plateau, cliff, blockedStatic;

        public readonly List<Vector2Int> spawns = new List<Vector2Int>();
        public RectInt castle = new RectInt(-1, -1, 0, 0);
        public RectInt barracks = new RectInt(-1, -1, 0, 0);
        public Vector2Int rally = new Vector2Int(-1, -1);
        public readonly List<RectInt> plots = new List<RectInt>();
        public readonly List<RectInt> prebuiltTowers = new List<RectInt>();
        public readonly List<RectInt> villages = new List<RectInt>();
        public readonly List<Vector2Int> trees = new List<Vector2Int>();
        public readonly List<Vector2Int> gold = new List<Vector2Int>();
        public readonly List<Vector2Int> bushes = new List<Vector2Int>();
        public readonly List<Vector2Int> rocks = new List<Vector2Int>();
        public readonly List<Vector2Int> waterRocks = new List<Vector2Int>();
        public readonly List<Vector2Int> sheep = new List<Vector2Int>();
        public readonly List<Vector2Int> ducks = new List<Vector2Int>();

        public bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
        public bool Land(int x, int y) => In(x, y) && land[x, y];
        public bool Road(int x, int y) => In(x, y) && road[x, y];
        public bool Plateau(int x, int y) => In(x, y) && plateau[x, y];

        public RectInt Bounds
        {
            get
            {
                int minX = W, minY = H, maxX = 0, maxY = 0;
                for (int x = 0; x < W; x++)
                    for (int y = 0; y < H; y++)
                        if (land[x, y]) { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }
                if (minX > maxX) return new RectInt(0, 0, W, H);
                return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            }
        }

        static bool IsWaterChar(char ch) => ch == '~' || ch == ' ' || ch == 'w' || ch == 'd';

        public static MapData Parse(string[] rows)
        {
            var m = new MapData();
            m.H = rows.Length;
            m.W = 0;
            foreach (var r in rows) m.W = Mathf.Max(m.W, r.TrimEnd().Length);
            m.c = new char[m.W, m.H];
            m.land = new bool[m.W, m.H];
            m.road = new bool[m.W, m.H];
            m.plateau = new bool[m.W, m.H];
            m.cliff = new bool[m.W, m.H];
            m.blockedStatic = new bool[m.W, m.H];

            for (int row = 0; row < m.H; row++)
            {
                int y = m.H - 1 - row;
                string line = rows[row];
                for (int x = 0; x < m.W; x++)
                {
                    char ch = x < line.Length ? line[x] : '~';
                    m.c[x, y] = ch;
                    m.land[x, y] = !IsWaterChar(ch);
                }
            }

            // Duyệt theo thứ tự đọc (trên xuống, trái sang) để đánh số điểm xuất hiện
            var consumed = new bool[m.W, m.H];
            for (int row = 0; row < m.H; row++)
            {
                int y = m.H - 1 - row;
                for (int x = 0; x < m.W; x++)
                {
                    if (consumed[x, y]) continue;
                    char ch = m.c[x, y];
                    var p = new Vector2Int(x, y);
                    switch (ch)
                    {
                        case '=': m.road[x, y] = true; break;
                        case 'S': m.road[x, y] = true; m.spawns.Add(p); break;
                        case '^': m.plateau[x, y] = true; break;
                        case 'T': m.trees.Add(p); m.blockedStatic[x, y] = true; break;
                        case '$': m.gold.Add(p); m.blockedStatic[x, y] = true; break;
                        case 'b': m.bushes.Add(p); break;
                        case 'o': m.rocks.Add(p); break;
                        case 's': m.sheep.Add(p); break;
                        case 'w': m.waterRocks.Add(p); break;
                        case 'd': m.ducks.Add(p); break;
                        case 'R': m.rally = p; break;
                        case 'C': m.castle = new RectInt(x - 2, y, 5, 2); break;
                        case 'K': m.barracks = new RectInt(x - 1, y, 3, 2); break;
                        case '#':
                        case 't':
                        case 'H':
                        {
                            var r = new RectInt(x, y, 2, 1);
                            if (m.In(x + 1, y)) consumed[x + 1, y] = true;
                            if (ch == '#') m.plots.Add(r);
                            else if (ch == 't') m.prebuiltTowers.Add(r);
                            else m.villages.Add(r);
                            break;
                        }
                    }
                }
            }

            // Ô dưới chân công trình là đất liền
            foreach (var r in new[] { m.castle, m.barracks })
                if (r.width > 0)
                    foreach (var cell in r.allPositionsWithin)
                        if (m.In(cell.x, cell.y)) m.land[cell.x, cell.y] = true;

            // Vách đá: ô ngay dưới mép dưới của cao nguyên
            for (int x = 0; x < m.W; x++)
                for (int y = 1; y < m.H; y++)
                    if (m.plateau[x, y] && !m.plateau[x, y - 1] && m.land[x, y - 1])
                    {
                        m.cliff[x, y - 1] = true;
                        m.blockedStatic[x, y - 1] = true;
                    }
            return m;
        }

        public static Vector2 CellCenter(Vector2Int c) => new Vector2(c.x + 0.5f, c.y + 0.5f);
    }
}
