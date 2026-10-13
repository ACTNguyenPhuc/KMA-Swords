using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oath
{
    public enum UnitState { Idle, Move, Attack, Dead }

    /// <summary>
    /// UnitController: nhận lệnh di chuyển/tấn công, quản lý trạng thái (Idle, Move, Attack, Die),
    /// tự tìm mục tiêu trong tầm, va chạm mềm giữa các đơn vị.
    /// </summary>
    public class Unit : MonoBehaviour
    {
        public static event Action<Unit> AnyDied;
        public static event Action<Unit> AnySpawned;

        public UnitDef def;
        public Health health;
        public UnitVisual visual;
        public EnemyAI enemyAI;
        public Pawn pawn;
        public HealthBar bar;

        public Vector2 Pos => transform.position;
        public bool IsAlive => health != null && health.current > 0f;
        public float VisualHeight => visual != null ? visual.Height : 1f;
        public bool Selected { get; private set; }
        public UnitState State { get; private set; }

        // Lệnh và mục tiêu
        public Health target;
        public bool targetCommanded;
        public bool forcedMove;
        public Vector2 guardPos;

        readonly List<Vector2> path = new List<Vector2>();
        int pathIdx;
        public bool HasPath => pathIdx < path.Count;

        float attackCd, thinkCd, repathCd;
        float pendingHit = -1f;
        Health pendingTarget;
        float slowUntil, slowMul = 1f;
        float burnDps, burnUntil, burnTick;
        Health burnSource;
        float stunUntil;
        GameObject burnFx;
        public bool Stunned => Time.time < stunUntil;
        public bool Burning => Time.time < burnUntil;
        bool movedThisFrame;
        float lastSortY = float.MaxValue;

        public static Unit Spawn(string id, Vector2 pos)
        {
            var def = UnitCatalog.Get(id);
            var go = new GameObject(def.name);
            if (World.Root != null) go.transform.SetParent(World.Root, false);
            go.transform.position = pos;
            var u = go.AddComponent<Unit>();
            u.Setup(def);
            return u;
        }

        void Setup(UnitDef d)
        {
            def = d;
            health = gameObject.AddComponent<Health>();
            health.unit = this;
            health.Init(d.hp, d.armor, d.team);
            health.OnDied += _ => Die();
            health.OnDamaged += (h, src) => { visual.Flash(); OnHurt(src); };

            visual = gameObject.AddComponent<UnitVisual>();
            visual.Init(d);
            bar = HealthBar.Create(health, transform, visual.Height + 0.18f, d.boss ? 1.5f : 0.8f);

            guardPos = Pos;
            if (d.team == Team.Enemy) enemyAI = gameObject.AddComponent<EnemyAI>();
            else if (d.id == "pawn") pawn = gameObject.AddComponent<Pawn>();

            World.Units.Add(this);
            thinkCd = UnityEngine.Random.value * 0.2f;
            attackCd = UnityEngine.Random.value * 0.3f;
            UpdateSorting(true);
            AnySpawned?.Invoke(this);
        }

        void Update()
        {
            if (!IsAlive) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            attackCd -= dt; thinkCd -= dt; repathCd -= dt;
            if (Time.time > slowUntil) slowMul = 1f;

            UpdateStatus(dt);
            if (!IsAlive) return;
            if (Stunned)
            {
                pendingHit = -1f;
                visual.SetMoving(false);
                State = UnitState.Idle;
                return;
            }

            if (pendingHit >= 0f)
            {
                pendingHit -= dt;
                if (pendingHit < 0f) ResolveHit();
            }

            if (thinkCd <= 0f)
            {
                thinkCd = 0.2f + UnityEngine.Random.value * 0.06f;
                Think();
            }

            movedThisFrame = false;
            if (target != null && target.Alive && def.attack != AttackKind.None) Engage(dt);
            else
            {
                if (target != null) ClearTarget();
                FollowPath(dt);
            }

            if (!IsAlive) return;
            Separate(dt);
            visual.SetMoving(movedThisFrame);
            State = movedThisFrame ? UnitState.Move : (target != null ? UnitState.Attack : UnitState.Idle);
        }

        void LateUpdate()
        {
            UpdateSorting(false);
        }

        void UpdateSorting(bool force)
        {
            float y = transform.position.y;
            if (!force && Mathf.Abs(y - lastSortY) < 0.02f) return;
            lastSortY = y;
            if (visual != null && visual.Group != null) visual.Group.sortingOrder = Util.SortOrder(y);
        }

        // ================== Trí tuệ ==================

        void Think()
        {
            if (target != null && !target.Alive) ClearTarget();
            if (enemyAI != null) { enemyAI.Think(); return; }
            if (pawn != null) { pawn.Think(); return; }

            if (def.attack == AttackKind.Heal) { ThinkHealer(); return; }
            if (targetCommanded && target != null) return;

            float leash = def.aggro + 2.5f;
            if (target != null)
            {
                bool tooFar = target.DistanceTo(Pos) > def.aggro + 3f;
                bool strayed = Vector2.Distance(Pos, guardPos) > Mathf.Max(leash, 7.5f) + 3f;
                if (tooFar || strayed) ClearTarget();
                else return;
            }

            if (forcedMove && HasPath) return;
            forcedMove = false;

            Vector2 center = HasPath ? Pos : guardPos;
            var e = World.NearestUnit(Pos, Team.Enemy, def.aggro, x => Vector2.Distance(x.Pos, center) <= leash);
            if (e != null) { SetTarget(e.health, false); return; }

            // Bảo vệ công trình: địch đang đánh công trình trong vùng canh giữ
            if (!HasPath)
            {
                const float DefendRadius = 7.5f;
                var raider = World.NearestUnit(Pos, Team.Enemy, DefendRadius,
                    x => x.target != null && x.target.building != null && Vector2.Distance(x.Pos, guardPos) <= DefendRadius);
                if (raider != null) { SetTarget(raider.health, false); return; }
            }

            if (!HasPath && Vector2.Distance(Pos, guardPos) > 0.7f) MoveTo(guardPos);
        }

        public void ThinkHealer()
        {
            Unit best = null;
            float bestRatio = 0.995f;
            foreach (var u in World.Units)
            {
                if (u == this || !u.IsAlive || u.def.team != def.team || u.def.id == "bomb") continue;
                if (u.health.Ratio >= bestRatio) continue;
                if (Vector2.Distance(Pos, u.Pos) > def.aggro) continue;
                bestRatio = u.health.Ratio;
                best = u;
            }
            if (best != null)
            {
                target = best.health;
                return;
            }
            target = null;
            if (enemyAI == null && !HasPath && Vector2.Distance(Pos, guardPos) > 0.7f) MoveTo(guardPos);
        }

        void OnHurt(Health src)
        {
            if (src == null || src.unit == null || !src.unit.IsAlive) return;
            if (def.attack == AttackKind.None || def.attack == AttackKind.Heal) return;
            if (target != null || targetCommanded || (forcedMove && HasPath)) return;
            if (src.DistanceTo(Pos) <= def.aggro + 3f) SetTarget(src, false);
        }

        // ================== Lệnh ==================

        public void SetSelected(bool on)
        {
            Selected = on;
            visual.SetSelected(on);
            if (bar != null) bar.forceShow = on;
        }

        public void CommandMove(Vector2 dest, bool attackMove)
        {
            ClearTarget();
            forcedMove = !attackMove;
            guardPos = dest;
            MoveTo(dest);
            if (pawn != null) pawn.OnManualMove();
        }

        public void CommandAttack(Health t)
        {
            if (def.attack == AttackKind.None || def.attack == AttackKind.Heal) { CommandMove(t.transform.position, true); return; }
            SetTarget(t, true);
            forcedMove = false;
        }

        public void SetTarget(Health t, bool commanded)
        {
            if (t == null || !t.Alive) return;
            target = t;
            targetCommanded = commanded;
            path.Clear(); pathIdx = 0;
            repathCd = 0f;
        }

        public void ClearTarget()
        {
            if (targetCommanded) guardPos = Pos;
            target = null;
            targetCommanded = false;
        }

        public bool MoveTo(Vector2 dest)
        {
            var p = World.Nav != null ? World.Nav.FindPath(Pos, dest, false) : null;
            if (p == null || p.Count == 0) { path.Clear(); pathIdx = 0; return false; }
            SetPath(p);
            return true;
        }

        public void SetPath(List<Vector2> pts, int start = 0)
        {
            path.Clear();
            for (int i = start; i < pts.Count; i++) path.Add(pts[i]);
            pathIdx = 0;
        }

        public void StopMoving()
        {
            path.Clear(); pathIdx = 0;
        }

        public void ApplyBurn(float dps, float duration, Health source)
        {
            if (!IsAlive || dps <= 0f) return;
            burnDps = Burning ? Mathf.Max(burnDps, dps) : dps;
            burnUntil = Mathf.Max(burnUntil, Time.time + duration);
            burnSource = source;
        }

        public void ApplyStun(float duration)
        {
            if (!IsAlive || duration <= 0f) return;
            if (def.boss) duration *= 0.4f;
            stunUntil = Mathf.Max(stunUntil, Time.time + duration);
        }

        void UpdateStatus(float dt)
        {
            bool burning = Burning;
            if (burning)
            {
                burnTick += dt;
                if (burnTick >= 0.5f)
                {
                    burnTick = 0f;
                    health.TakeDamage(burnDps * 0.5f, burnSource, true);
                    if (!IsAlive) return;
                }
            }
            if (burning != (burnFx != null))
            {
                if (burning) burnFx = FX.Loop("fire", transform, new Vector3(0, VisualHeight * 0.45f, 0), 0.55f, 5);
                else { Destroy(burnFx); burnFx = null; }
            }
            visual.SetStatusTint(Stunned ? new Color(0.6f, 0.65f, 0.85f) : (burning ? new Color(1f, 0.75f, 0.55f) : Color.white));
        }

        public void ApplySlow(float amount, float duration)
        {
            slowMul = Mathf.Min(slowMul, 1f - amount);
            slowUntil = Time.time + duration;
        }

        // ================== Chiến đấu ==================

        void Engage(float dt)
        {
            float d = target.DistanceTo(Pos);
            if (def.attack == AttackKind.Explode && d <= def.range) { Explode(); return; }

            if (d <= def.range)
            {
                StopMoving();
                Vector2 dir = target.AimPoint - (Pos + Vector2.up * VisualHeight * 0.45f);
                visual.Face(dir.x);
                if (attackCd <= 0f && pendingHit < 0f) StartAttack();
            }
            else if (pendingHit < 0f)
            {
                Vector2 goal = target.ApproachPoint(Pos);
                if (repathCd <= 0f || !HasPath)
                {
                    repathCd = 0.5f + UnityEngine.Random.value * 0.2f;
                    PathTowards(goal);
                }
                FollowPath(dt);
            }
        }

        void PathTowards(Vector2 goal)
        {
            if (World.Nav == null) { path.Clear(); path.Add(goal); pathIdx = 0; return; }
            if (World.Nav.LineWalkable(Pos, goal)) { path.Clear(); path.Add(goal); pathIdx = 0; return; }
            var p = World.Nav.FindPath(Pos, goal, false);
            if (p != null && p.Count > 0) SetPath(p);
            else { path.Clear(); path.Add(goal); pathIdx = 0; }
        }

        void StartAttack()
        {
            attackCd = def.cooldown * UnityEngine.Random.Range(0.95f, 1.08f);
            Vector2 dir = target.AimPoint - (Pos + Vector2.up * VisualHeight * 0.45f);
            pendingHit = visual.PlayAttack(dir);
            pendingTarget = target;
        }

        void ResolveHit()
        {
            pendingHit = -1f;
            var t = pendingTarget;
            pendingTarget = null;
            if (t == null || !t.Alive || !IsAlive) return;
            switch (def.attack)
            {
                case AttackKind.Melee:
                    if (t.DistanceTo(Pos) > def.range + 0.7f) return;
                    DealDamage(t, def.damage);
                    AudioManager.Play(def.boss ? "heavy" : "hit", 0.55f);
                    break;
                case AttackKind.Ranged:
                {
                    Vector2 from = Pos + Vector2.up * VisualHeight * 0.55f + Vector2.right * (t.AimPoint.x > Pos.x ? 0.25f : -0.25f);
                    Projectile.Fire(from, t, def.damage, health, def.projectile ?? "arrow", def.projectileColor, def.slow, def.splash, def.buildingMul);
                    AudioManager.Play("arrow", 0.35f);
                    break;
                }
                case AttackKind.Heal:
                    t.Heal(def.damage);
                    FX.Play("heal", t.transform.position + Vector3.up * 0.1f, 0.8f);
                    AudioManager.Play("heal", 0.3f);
                    break;
                case AttackKind.Explode:
                    Explode();
                    break;
            }
        }

        public void DealDamage(Health t, float dmg)
        {
            float m = t.building != null ? def.buildingMul : 1f;
            Vector2 at = t.transform.position;
            Team victims = t.team;
            t.TakeDamage(dmg * m, health);
            if (def.slow > 0f && t.unit != null) t.unit.ApplySlow(def.slow, 2f);
            if (def.splash > 0f) World.DamageArea(at, def.splash, dmg * 0.6f, victims, health, t, def.buildingMul);
        }

        void Explode()
        {
            if (!IsAlive) return;
            Team victims = def.team == Team.Enemy ? Team.Player : Team.Enemy;
            FX.Play("explosion", Pos + Vector2.up * 0.3f, 1.1f);
            AudioManager.Play("explode", 0.8f);
            World.DamageArea(Pos, def.splash, def.damage, victims, health, null, def.buildingMul);
            health.TakeDamage(99999f, null);
        }

        // ================== Di chuyển ==================

        bool StepTowards(Vector2 goal, float dt)
        {
            Vector2 p = Pos;
            Vector2 delta = goal - p;
            float dist = delta.magnitude;
            float step = def.speed * slowMul * dt;
            Vector2 np = dist <= step ? goal : p + delta / Mathf.Max(0.0001f, dist) * step;
            if (World.Nav != null) np = World.Nav.ClampMove(p, np);
            if ((np - p).sqrMagnitude > 1e-8f)
            {
                transform.position = np;
                movedThisFrame = true;
                visual.Face(delta.x);
            }
            return dist <= step + 0.02f;
        }

        void FollowPath(float dt)
        {
            if (!HasPath) return;
            if (StepTowards(path[pathIdx], dt))
            {
                pathIdx++;
                if (!HasPath)
                {
                    forcedMove = false;
                    if (enemyAI != null) enemyAI.OnPathEnd();
                    if (pawn != null) pawn.OnArrived();
                }
            }
        }

        void Separate(float dt)
        {
            Vector2 push = Vector2.zero;
            Vector2 p = Pos;
            float myMass = def.bodyRadius * def.bodyRadius;
            foreach (var o in World.Units)
            {
                if (o == this || !o.IsAlive) continue;
                Vector2 d = p - o.Pos;
                float min = (def.bodyRadius + o.def.bodyRadius) * 0.85f;
                float sq = d.sqrMagnitude;
                if (sq >= min * min) continue;
                float dist = Mathf.Sqrt(sq);
                if (dist < 0.001f) { d = UnityEngine.Random.insideUnitCircle.normalized; dist = 0.001f; }
                else d /= dist;
                float oMass = o.def.bodyRadius * o.def.bodyRadius;
                float w = oMass / (myMass + oMass) * 2f;
                if (o.def.team != def.team) w *= 0.7f;
                push += d * (min - dist) * w;
            }
            if (push.sqrMagnitude < 1e-8f) return;
            Vector2 np = p + push * Mathf.Min(1f, dt * 8f);
            if (World.Nav != null) np = World.Nav.ClampMove(p, np);
            transform.position = np;
        }

        // ================== Chết ==================

        void Die()
        {
            if (State == UnitState.Dead) return;
            State = UnitState.Dead;
            World.Units.Remove(this);
            if (Selected && SelectionManager.I != null) SelectionManager.I.Remove(this);
            if (bar != null) Destroy(bar.gameObject);
            pendingHit = -1f;
            if (burnFx != null) { Destroy(burnFx); burnFx = null; }

            FX.Play("dust", Pos + Vector2.up * 0.2f, def.boss ? 1.6f : 0.9f);
            AudioManager.Play(def.team == Team.Enemy ? "die" : "die_ally", 0.45f);

            if (!string.IsNullOrEmpty(def.deathSpawn))
            {
                for (int i = 0; i < def.deathSpawnCount; i++)
                {
                    Vector2 off = UnityEngine.Random.insideUnitCircle * 0.6f;
                    var child = Spawn(def.deathSpawn, Pos + off);
                    if (enemyAI != null && child.enemyAI != null) child.enemyAI.InheritRoute(enemyAI);
                }
            }

            AnyDied?.Invoke(this);
            visual.Die(() => Destroy(gameObject));
        }

        public static void ResetEvents()
        {
            AnyDied = null;
            AnySpawned = null;
        }
    }
}
