using System.Collections.Generic;
using UnityEngine;

namespace Oath
{
    /// <summary>
    /// AI quân địch: đi theo waypoint (Spawn → đường → lâu đài), giao chiến khi gặp quân/công trình
    /// trong tầm, bị kéo quá xa thì quay lại tuyến đường.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        Unit u;
        List<Vector2> route;
        int routeIdx;
        bool atEnd;
        Vector2 leavePoint;
        const float Leash = 4.5f;

        Unit U => u != null ? u : (u = GetComponent<Unit>());

        public void Init(List<Vector2> r, float lateral)
        {
            route = new List<Vector2>(r.Count);
            // Lệch nhẹ sang hai bên để quân địch không đi thành một hàng thẳng tắp
            for (int i = 0; i < r.Count; i++)
            {
                Vector2 p = r[i];
                Vector2 dir = i + 1 < r.Count ? r[i + 1] - r[i] : (i > 0 ? r[i] - r[i - 1] : Vector2.right);
                Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
                Vector2 q = p + perp * lateral;
                route.Add(World.Nav != null && World.Nav.IsWalkable(q) ? q : p);
            }
            routeIdx = 0;
            atEnd = false;
            Resume();
        }

        public void InheritRoute(EnemyAI other)
        {
            route = other.route;
            routeIdx = other.routeIdx;
            atEnd = other.atEnd;
            Resume();
        }

        public void Think()
        {
            var me = U;
            AdvanceIndex();

            if (me.def.attack == AttackKind.Heal)
            {
                me.ThinkHealer();
                if (me.target == null && !me.HasPath) Resume();
                return;
            }

            var t = me.target;
            if (t != null)
            {
                if (!t.Alive) me.ClearTarget();
                else if (t.building == null && !atEnd && Vector2.Distance(me.Pos, leavePoint) > Leash)
                {
                    me.ClearTarget();
                    Resume();
                    return;
                }
                else return;
            }

            float aggro = me.def.aggro;
            var pu = World.NearestUnit(me.Pos, Team.Player, aggro);
            if (pu != null) { Engage(pu.health); return; }

            var b = World.NearestBuilding(me.Pos, Team.Player, Mathf.Min(aggro, me.def.IsRanged ? aggro : 2.0f));
            if (b != null) { Engage(b.health); return; }

            if (atEnd || route == null)
            {
                if (World.Castle != null && World.Castle.health.Alive) Engage(World.Castle.health);
                return;
            }
            if (!me.HasPath) Resume();
        }

        void Engage(Health h)
        {
            if (U.target == null) leavePoint = U.Pos;
            U.SetTarget(h, false);
        }

        void AdvanceIndex()
        {
            if (route == null) return;
            Vector2 p = U.Pos;
            while (routeIdx + 1 < route.Count &&
                   Vector2.Distance(p, route[routeIdx + 1]) <= Vector2.Distance(p, route[routeIdx]))
                routeIdx++;
        }

        public void Resume()
        {
            var me = U;
            if (route == null || route.Count == 0) { atEnd = true; return; }
            AdvanceIndex();
            int best = routeIdx;
            float bd = float.MaxValue;
            int end = Mathf.Min(route.Count, routeIdx + 12);
            for (int i = routeIdx; i < end; i++)
            {
                float d = Vector2.Distance(me.Pos, route[i]);
                if (d < bd) { bd = d; best = i; }
            }
            routeIdx = best;
            if (bd < 0.4f && routeIdx + 1 < route.Count) routeIdx++;

            if (World.Nav == null || World.Nav.LineWalkable(me.Pos, route[routeIdx]))
            {
                me.SetPath(route, routeIdx);
                return;
            }
            var prefix = World.Nav.FindPath(me.Pos, route[routeIdx], false);
            var full = new List<Vector2>();
            if (prefix != null) full.AddRange(prefix);
            for (int i = routeIdx + 1; i < route.Count; i++) full.Add(route[i]);
            me.SetPath(full);
        }

        public void OnPathEnd()
        {
            if (route != null && Vector2.Distance(U.Pos, route[route.Count - 1]) < 1.5f) atEnd = true;
            if (atEnd && World.Castle != null && World.Castle.health.Alive && U.target == null) Engage(World.Castle.health);
        }
    }
}
