using System.Collections.Generic;
using UnityEngine;

namespace Oath
{
    /// <summary>Danh sách các đối tượng đang tồn tại trong màn, tránh dùng Find mỗi frame.</summary>
    public static class World
    {
        public static readonly List<Unit> Units = new List<Unit>();
        public static readonly List<Building> Buildings = new List<Building>();
        public static readonly List<ResourceNode> ResourceNodes = new List<ResourceNode>();
        public static readonly List<BuildPlot> Plots = new List<BuildPlot>();
        public static MapData Map;
        public static NavGrid Nav;
        public static Building Castle;
        public static Transform Root;

        public static void Clear()
        {
            Units.Clear(); Buildings.Clear(); ResourceNodes.Clear(); Plots.Clear();
            Map = null; Nav = null; Castle = null; Root = null;
        }

        public static int CountUnits(Team team, bool combatOnly = false)
        {
            int n = 0;
            foreach (var u in Units)
                if (u.IsAlive && u.def.team == team && (!combatOnly || u.def.attack != AttackKind.None)) n++;
            return n;
        }

        /// <summary>Tìm đơn vị gần nhất thuộc đội <paramref name="team"/> trong bán kính.</summary>
        public static Unit NearestUnit(Vector2 pos, Team team, float radius, System.Predicate<Unit> filter = null)
        {
            Unit best = null;
            float bestD = radius;
            foreach (var u in Units)
            {
                if (!u.IsAlive || u.def.team != team) continue;
                if (filter != null && !filter(u)) continue;
                float d = Vector2.Distance(pos, u.Pos) - u.def.bodyRadius;
                if (d < bestD) { bestD = d; best = u; }
            }
            return best;
        }

        public static Building NearestBuilding(Vector2 pos, Team team, float radius, bool includeCastle = true)
        {
            Building best = null;
            float bestD = radius;
            foreach (var b in Buildings)
            {
                if (!b.health.Alive || b.team != team) continue;
                if (!includeCastle && b.type == BuildingType.Castle) continue;
                float d = b.DistanceTo(pos);
                if (d < bestD) { bestD = d; best = b; }
            }
            return best;
        }

        /// <summary>Có kẻ địch của đội <paramref name="team"/> trong bán kính không.</summary>
        public static bool HostileNear(Vector2 pos, Team team, float radius)
        {
            Team other = team == Team.Player ? Team.Enemy : Team.Player;
            return NearestUnit(pos, other, radius) != null;
        }

        public static void DamageArea(Vector2 center, float radius, float damage, Team victimTeam, Health source, Health exclude = null, float buildingMul = 1f)
        {
            // Lặp ngược vì đơn vị có thể bị xoá khỏi danh sách khi chết
            for (int i = Units.Count - 1; i >= 0; i--)
            {
                if (i >= Units.Count) continue;
                var u = Units[i];
                if (!u.IsAlive || u.def.team != victimTeam || u.health == exclude) continue;
                if (Vector2.Distance(center, u.Pos) <= radius + u.def.bodyRadius)
                    u.health.TakeDamage(damage, source);
            }
            for (int i = Buildings.Count - 1; i >= 0; i--)
            {
                if (i >= Buildings.Count) continue;
                var b = Buildings[i];
                if (!b.health.Alive || b.team != victimTeam || b.health == exclude) continue;
                if (b.DistanceTo(center) <= radius)
                    b.health.TakeDamage(damage * buildingMul, source);
            }
        }
    }
}
