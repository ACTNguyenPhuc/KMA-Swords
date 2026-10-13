using System;
using UnityEngine;

namespace Oath
{
    /// <summary>Mũi tên / quả cầu phép / đá ném bay theo đường cong tới mục tiêu.</summary>
    public class Projectile : MonoBehaviour
    {
        Health target, source;
        Vector2 start, lastTarget;
        float damage, slow, splash, buildingMul, t, duration, arc;
        bool rotate;
        float spin;
        Team victimTeam;
        SpriteRenderer sr, shadow;
        Action<Vector2, Health> onImpact;

        public static void Fire(Vector2 from, Health target, float damage, Health source, string kind, Color color,
            float slow = 0f, float splash = 0f, float buildingMul = 1f)
        {
            if (target == null) return;
            var p = Create(from, target, source);
            if (kind == "orb")
            {
                p.sr.sprite = Util.Circle();
                p.sr.color = color;
                p.transform.localScale = Vector3.one * 0.45f;
                p.arc = 0.25f;
                var glow = Util.NewSprite("Glow", p.transform, Util.Circle(), Vector3.zero, Util.OrderOverlay - 201);
                glow.color = new Color(color.r, color.g, color.b, 0.35f);
                glow.transform.localScale = Vector3.one * 2f;
            }
            else
            {
                p.sr.sprite = GameArt.I.Spr("Tiny Swords/Units/Extra/Arrow/Arrow");
                p.sr.color = source != null && source.team == Team.Enemy ? new Color(1f, 0.8f, 0.75f) : Color.white;
                p.transform.localScale = Vector3.one * 0.8f;
                p.rotate = true;
                p.arc = 0.6f;
            }
            p.damage = damage;
            p.slow = slow;
            p.splash = splash;
            p.buildingMul = buildingMul;
            p.Finish(11f);
        }

        /// <summary>Bắn một vật thể tuỳ ý (đá, bóng sắt...) theo đường cong; khi chạm đất gọi onImpact(điểm rơi, mục tiêu).</summary>
        public static void Lob(Vector2 from, Health target, Sprite sprite, float scale, float arcHeight, float speed, float spinDeg,
            Action<Vector2, Health> onImpact)
        {
            if (target == null) return;
            var p = Create(from, target, null);
            p.sr.sprite = sprite;
            p.transform.localScale = Vector3.one * scale;
            p.arc = arcHeight;
            p.spin = spinDeg;
            p.onImpact = onImpact;
            p.shadow = Util.NewSprite("Shadow", World.Root, Util.Ellipse(false), from, Util.OrderGroundDecal + 20);
            p.shadow.color = new Color(0, 0, 0, 0.25f);
            p.shadow.transform.localScale = Vector3.one * 0.5f;
            p.Finish(speed);
        }

        static Projectile Create(Vector2 from, Health target, Health source)
        {
            var go = new GameObject("Projectile");
            if (World.Root != null) go.transform.SetParent(World.Root, false);
            go.transform.position = from;
            var p = go.AddComponent<Projectile>();
            p.sr = go.AddComponent<SpriteRenderer>();
            p.sr.sortingOrder = Util.OrderOverlay - 200;
            p.target = target;
            p.source = source;
            p.start = from;
            p.lastTarget = target.AimPoint;
            p.victimTeam = target.team;
            return p;
        }

        void Finish(float speed)
        {
            float dist = Vector2.Distance(start, lastTarget);
            duration = Mathf.Clamp(dist / speed, 0.12f, 1.2f);
            if (onImpact == null) arc *= Mathf.Clamp01(dist / 4f);
        }

        void Update()
        {
            if (target != null && target.Alive) lastTarget = onImpact != null ? (Vector2)target.transform.position : target.AimPoint;
            t += Time.deltaTime / duration;
            float k = Mathf.Min(1f, t);
            Vector2 ground = Vector2.Lerp(start, lastTarget, k);
            Vector2 pos = ground + Vector2.up * arc * 4f * k * (1f - k);
            if (rotate)
            {
                Vector2 vel = (lastTarget - start) + Vector2.up * arc * 4f * (1f - 2f * k);
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg);
            }
            else if (spin != 0f) transform.Rotate(0, 0, spin * Time.deltaTime);
            transform.position = pos;
            if (shadow != null) shadow.transform.position = ground;
            if (t >= 1f) Hit();
        }

        void Hit()
        {
            if (shadow != null) Destroy(shadow.gameObject);
            if (onImpact != null)
            {
                onImpact(lastTarget, target != null && target.Alive ? target : null);
                Destroy(gameObject);
                return;
            }
            if (target != null && target.Alive)
            {
                float m = target.building != null ? buildingMul : 1f;
                Vector2 at = target.transform.position;
                target.TakeDamage(damage * m, source);
                if (slow > 0f && target != null && target.unit != null) target.unit.ApplySlow(slow, 2f);
                if (splash > 0f) World.DamageArea(at, splash, damage * 0.6f, victimTeam, source, target, buildingMul);
                if (!rotate) FX.Play("dust", lastTarget, 0.5f);
            }
            Destroy(gameObject);
        }
    }
}
