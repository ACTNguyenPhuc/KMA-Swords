using UnityEngine;

namespace Oath
{
    /// <summary>
    /// Tháp phòng thủ: quét kẻ địch trong bán kính theo nhịp cố định (không quét mỗi frame), chọn mục tiêu theo
    /// chế độ ưu tiên (Đầu tiên / Mạnh nhất / Gần nhất) rồi tấn công theo loại tháp:
    /// Cung (bắn tên), Bắn Đá (lan), Bi Sắt (xuyên giáp, làm chậm), Hỏa Ngục (thiêu đốt), Búa Địa Chấn (nện quanh tháp, choáng).
    /// </summary>
    public class TowerController : MonoBehaviour
    {
        const string ArcherBase = "Tiny Swords/Units/Blue Units/Archer/Archer_";
        const float ArcherTopY = 2.35f;

        Building b;
        TowerDef def;
        float cd, scanCd;
        Health target;
        readonly SpriteAnimator[] archers = new SpriteAnimator[2];
        SpriteRenderer top;
        Vector3 topBase;
        float topAnim = -1f;

        public TowerDef Def => def;
        int L => Mathf.Clamp(b.level - 1, 0, 2);
        public float Range => def.range[L];
        public float Damage => def.damage[L];
        public float Cooldown => def.cooldown[L];
        Vector2 Center => b.Base + Vector2.up * 0.6f;
        bool IsArcher => def.type == TowerType.Archer;

        public void Init(Building building, TowerType type)
        {
            b = building;
            def = TowerCatalog.Get(type);
            if (IsArcher) archers[0] = MakeArcher(0f);
            else BuildStoneVisual();
            cd = 0.5f;
        }

        // ---------------- Hình ảnh ----------------

        SpriteAnimator MakeArcher(float x)
        {
            var sr = Util.NewSprite("Archer", transform, null, new Vector3(x, ArcherTopY + 0.63f * 0.85f, 0), b.sr.sortingOrder + 1);
            sr.transform.localScale = Vector3.one * 0.85f;
            var a = sr.gameObject.AddComponent<SpriteAnimator>();
            a.target = sr;
            a.Play(GameArt.I.Set(ArcherBase + "Idle"), 9f, true, true, null, true);
            return a;
        }

        /// <summary>Đế + thanh ném (hoặc búa) của tháp đá theo cấp hiện tại.</summary>
        public void BuildStoneVisual()
        {
            var baseSprite = GameArt.I.Spr(def.baseSprite[L]);
            b.SetMainSprite(baseSprite, StoneFeet(baseSprite));
            if (top == null)
            {
                top = Util.NewSprite("Top", transform, null, Vector3.zero, b.sr.sortingOrder + 1);
            }
            top.sprite = GameArt.I.Spr(def.topSprite[L]);
            float baseTop = b.sr.transform.localPosition.y + (baseSprite != null ? baseSprite.bounds.extents.y : 1f);
            if (def.aroundSelf)
                topBase = new Vector3(0f, baseTop + 40f / TowerCatalog.StonePPU - (top.sprite != null ? top.sprite.bounds.extents.y : 0.6f), 0f);
            else
                topBase = new Vector3(0f, baseTop - 8f / TowerCatalog.StonePPU - (top.sprite != null ? top.sprite.bounds.extents.y : 0.2f), 0f);
            top.transform.localPosition = topBase;
        }

        static float StoneFeet(Sprite s) => s != null ? s.bounds.extents.y - 0.12f : 1.1f;

        public void OnUpgrade()
        {
            if (IsArcher)
            {
                if (b.level >= 3 && archers[1] == null)
                {
                    archers[0].transform.localPosition = new Vector3(-0.28f, archers[0].transform.localPosition.y, 0);
                    archers[1] = MakeArcher(0.28f);
                }
            }
            else BuildStoneVisual();
        }

        // ---------------- Vòng lặp ----------------

        void Update()
        {
            if (b == null || !b.health.Alive) return;
            if (top != null) top.color = b.sr.color;
            if (b.constructing) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (IsArcher) foreach (var a in archers) if (a != null) a.target.color = b.sr.color;
            AnimateTop(dt);

            scanCd -= dt;
            if (scanCd <= 0f)
            {
                scanCd = 0.25f;
                if (!Valid(target)) target = FindTarget();
            }
            cd -= dt;
            if (target != null && target.Alive && cd <= 0f) Fire();
        }

        bool Valid(Health h)
        {
            if (h == null || !h.Alive || h.unit == null) return false;
            if (h.unit.def.flying && !def.hitsAir) return false;
            return Vector2.Distance(Center, h.unit.Pos) - h.unit.def.bodyRadius <= Range;
        }

        Health FindTarget()
        {
            Health best = null;
            float bestScore = float.MaxValue;
            foreach (var u in World.Units)
            {
                if (!u.IsAlive || u.def.team != Team.Enemy) continue;
                if (u.def.flying && !def.hitsAir) continue;
                float d = Vector2.Distance(Center, u.Pos) - u.def.bodyRadius;
                if (d > Range) continue;
                float score;
                switch (b.targetMode)
                {
                    case TargetMode.Strongest: score = -u.health.current; break;
                    case TargetMode.Nearest: score = d; break;
                    default: score = World.Castle != null ? World.Castle.DistanceTo(u.Pos) : d; break;
                }
                if (score < bestScore) { bestScore = score; best = u.health; }
            }
            return best;
        }

        void Fire()
        {
            cd = Cooldown;
            switch (def.type)
            {
                case TowerType.Archer: FireArrows(); break;
                case TowerType.Quake: Slam(); break;
                default: Lob(); break;
            }
        }

        // ---------------- Tháp cung ----------------

        void FireArrows()
        {
            int count = b.level >= 3 ? 2 : 1;
            if (count == 2) cd *= 1.6f;
            for (int i = 0; i < count; i++)
            {
                var a = archers[i];
                if (a == null) continue;
                a.target.flipX = target.AimPoint.x < a.transform.position.x;
                a.Play(GameArt.I.Set(ArcherBase + "Shoot"), 18f, false, true,
                    () => a.Play(GameArt.I.Set(ArcherBase + "Idle"), 9f, true, true));
                StartCoroutine(ReleaseArrow(a.transform, i * 0.12f, target));
            }
        }

        System.Collections.IEnumerator ReleaseArrow(Transform from, float extra, Health t)
        {
            yield return new WaitForSeconds(0.28f + extra);
            if (t != null && t.Alive && b != null && b.health.Alive)
            {
                Projectile.Fire((Vector2)from.position + Vector2.up * 0.15f, t, Damage, b.health, "arrow", Color.white);
                AudioManager.Play("arrow", 0.3f);
            }
        }

        // ---------------- Tháp ném (đá, bi sắt, lửa) ----------------

        void Lob()
        {
            if (top != null && def.topFireSprite[L] != null) top.sprite = GameArt.I.Spr(def.topFireSprite[L]);
            topAnim = 0f;
            var t = target;
            Vector2 from = (Vector2)transform.position + (Vector2)topBase + Vector2.up * 0.1f;
            var sprite = GameArt.I.Spr(def.projectile[L]);
            float scale = def.projectileScale * (def.type == TowerType.Catapult && L > 0 ? 0.6f : 1f);
            float arc = def.type == TowerType.Iron ? 1.0f : 1.8f;
            float speed = def.type == TowerType.Iron ? 9f : 7f;
            float spin = def.type == TowerType.Fire ? 0f : 540f * (t.transform.position.x < from.x ? 1f : -1f);
            int lvl = L;
            Projectile.Lob(from, t, sprite, scale, arc, speed, spin, (at, hit) => Impact(at, hit, lvl));
            AudioManager.Play(def.type == TowerType.Fire ? "explode" : "build", 0.25f);
        }

        void Impact(Vector2 at, Health hit, int lvl)
        {
            if (b == null) return;
            float dmg = def.damage[lvl];
            float splash = def.splash[lvl];
            var src = b.health;

            if (splash > 0f)
            {
                for (int i = World.Units.Count - 1; i >= 0; i--)
                {
                    if (i >= World.Units.Count) continue;
                    var u = World.Units[i];
                    if (!u.IsAlive || u.def.team != Team.Enemy) continue;
                    if (u.def.flying && !def.hitsAir) continue;
                    float d = Vector2.Distance(at, u.Pos) - u.def.bodyRadius;
                    if (d > splash) continue;
                    float k = u.health == hit ? 1f : 0.65f;
                    u.health.TakeDamage(dmg * k, src, def.pierce);
                    if (def.burnDps[lvl] > 0f && u.IsAlive) u.ApplyBurn(def.burnDps[lvl], 3f, src);
                    if (def.slow[lvl] > 0f && u.IsAlive) u.ApplySlow(def.slow[lvl], 1.5f);
                }
            }
            else if (hit != null && hit.Alive)
            {
                hit.TakeDamage(dmg, src, def.pierce);
                if (hit.unit != null && hit.unit.IsAlive)
                {
                    if (def.slow[lvl] > 0f) hit.unit.ApplySlow(def.slow[lvl], 1.5f);
                    if (def.burnDps[lvl] > 0f) hit.unit.ApplyBurn(def.burnDps[lvl], 3f, src);
                }
            }

            switch (def.type)
            {
                case TowerType.Catapult:
                    FX.Impact(lvl == 0 ? "rock" : "rockBig", at, lvl == 0 ? 0.9f : 0.75f);
                    FX.Play("dust", at, 0.9f + splash * 0.2f);
                    AudioManager.Play("heavy", 0.45f);
                    break;
                case TowerType.Fire:
                    FX.Impact("fire", at + Vector2.up * 0.3f, 0.45f + splash * 0.15f, 20f);
                    FX.Impact("sparks", at, 0.7f);
                    AudioManager.Play("explode", 0.4f);
                    break;
                case TowerType.Iron:
                    FX.Impact("iron", at + Vector2.up * 0.3f, 0.75f);
                    AudioManager.Play("hit", 0.5f);
                    break;
            }
        }

        // ---------------- Búa địa chấn ----------------

        void Slam()
        {
            topAnim = 0f;
            StartCoroutine(SlamRoutine(L));
        }

        System.Collections.IEnumerator SlamRoutine(int lvl)
        {
            yield return new WaitForSeconds(0.32f);
            if (b == null || !b.health.Alive) yield break;
            Vector2 c = b.Base + Vector2.up * 0.3f;
            float r = def.range[lvl];
            for (int i = World.Units.Count - 1; i >= 0; i--)
            {
                if (i >= World.Units.Count) continue;
                var u = World.Units[i];
                if (!u.IsAlive || u.def.team != Team.Enemy || u.def.flying) continue;
                if (Vector2.Distance(c, u.Pos) - u.def.bodyRadius > r) continue;
                u.health.TakeDamage(def.damage[lvl], b.health);
                if (u.IsAlive) u.ApplyStun(def.stun[lvl]);
            }
            FX.Impact("quake", c, r * 0.55f, 16f);
            FX.Impact("rockBig", c + Vector2.up * 0.2f, 0.9f, 14f);
            FX.Play("dust2", c, 1.4f);
            AudioManager.Play("heavy", 0.8f);
            if (CameraController.I != null) CameraController.I.Shake(0.15f);
        }

        void AnimateTop(float dt)
        {
            if (top == null || topAnim < 0f) return;
            topAnim += dt;
            if (def.aroundSelf)
            {
                // nhấc búa lên rồi nện mạnh xuống
                float y;
                if (topAnim < 0.25f) y = Mathf.SmoothStep(0f, 0.45f, topAnim / 0.25f);
                else if (topAnim < 0.32f) y = Mathf.Lerp(0.45f, -0.35f, (topAnim - 0.25f) / 0.07f);
                else if (topAnim < 0.7f) y = Mathf.Lerp(-0.35f, 0f, (topAnim - 0.32f) / 0.38f);
                else { y = 0f; topAnim = -1f; }
                top.transform.localPosition = topBase + new Vector3(0, y, 0);
            }
            else
            {
                // thanh ném giật lùi rồi trở về
                float k = Mathf.Clamp01(topAnim / 0.35f);
                top.transform.localPosition = topBase + new Vector3(0, -Mathf.Sin(k * Mathf.PI) * 0.12f, 0);
                if (topAnim > 0.35f)
                {
                    top.sprite = GameArt.I.Spr(def.topSprite[L]);
                    top.transform.localPosition = topBase;
                    topAnim = -1f;
                }
            }
        }
    }
}
