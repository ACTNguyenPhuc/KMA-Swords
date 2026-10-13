using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Oath
{
    /// <summary>
    /// Hiển thị đơn vị: sprite động Tiny Swords, hoặc sprite tĩnh/prefab quái vật kèm hoạt ảnh thủ tục
    /// (nhún khi đi, lao tới khi đánh, nháy đỏ khi trúng đòn, mờ dần khi chết).
    /// </summary>
    public class UnitVisual : MonoBehaviour
    {
        UnitDef def;
        Transform body, art;
        SpriteRenderer mainSr;
        SpriteAnimator anim;
        SpriteRenderer[] renderers = new SpriteRenderer[0];
        Color[] baseColors = new Color[0];
        SpriteRenderer shadow, ring;
        public SortingGroup Group { get; private set; }

        float baseScale = 1f;
        Vector2 artOffset;
        int facing = 1;
        bool moving;
        float moveT, idleT;
        float attackT = -1f, attackDur = 0.35f;
        Vector2 attackDir;
        float flashT;
        float alpha = 1f;
        Color statusTint = Color.white;
        bool dying;
        string idleKey, runKey, loopKey;
        float loopFps;

        public float Height { get; private set; } = 1.2f;
        bool IsTS => def.visual == VisualKind.TinySwords;

        public void Init(UnitDef d)
        {
            def = d;
            Group = gameObject.AddComponent<SortingGroup>();

            shadow = Util.NewSprite("Shadow", transform, Util.Ellipse(false), Vector3.zero, -3);
            shadow.color = new Color(0f, 0f, 0f, 0.28f);
            float sw = d.bodyRadius * 3.2f;
            shadow.transform.localScale = new Vector3(sw, sw, 1f);

            ring = Util.NewSprite("Ring", transform, Util.Ellipse(true), Vector3.zero, -2);
            ring.color = d.team == Team.Player ? new Color(0.45f, 1f, 0.55f, 0.95f) : new Color(1f, 0.35f, 0.3f, 0.95f);
            ring.transform.localScale = new Vector3(sw * 1.05f, sw * 1.05f, 1f);
            ring.enabled = false;

            body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            art = new GameObject("Art").transform;
            art.SetParent(body, false);

            switch (d.visual)
            {
                case VisualKind.TinySwords:
                {
                    mainSr = art.gameObject.AddComponent<SpriteRenderer>();
                    anim = art.gameObject.AddComponent<SpriteAnimator>();
                    anim.target = mainSr;
                    baseScale = d.scale;
                    bool lancer = d.idle != null && d.idle.Contains("Lancer");
                    artOffset = new Vector2(0f, (lancer ? 0.6f : 0.63f) * d.scale);
                    Height = 1.25f * d.scale;
                    idleKey = d.idle; runKey = d.run;
                    anim.Play(GameArt.I.Set(idleKey), 10f, true, true, null, true);
                    break;
                }
                case VisualKind.Sprite:
                {
                    mainSr = art.gameObject.AddComponent<SpriteRenderer>();
                    mainSr.sprite = GameArt.I.Spr(d.sprite);
                    if (mainSr.sprite != null)
                    {
                        var b = mainSr.sprite.bounds;
                        baseScale = d.height / Mathf.Max(0.01f, b.size.y);
                        artOffset = new Vector2(-b.center.x * baseScale, -b.min.y * baseScale);
                    }
                    Height = d.height;
                    break;
                }
                case VisualKind.Prefab:
                {
                    var prefab = GameArt.I.Prefab(d.sprite);
                    if (prefab != null)
                    {
                        var inst = Instantiate(prefab, art);
                        inst.transform.localPosition = Vector3.zero;
                        // Đo kích thước khi chưa co giãn
                        var srs = inst.GetComponentsInChildren<SpriteRenderer>(true);
                        Bounds bb = new Bounds();
                        bool first = true;
                        foreach (var sr in srs)
                        {
                            if (sr.sprite == null || !sr.enabled || !sr.gameObject.activeInHierarchy) continue;
                            if (first) { bb = sr.bounds; first = false; } else bb.Encapsulate(sr.bounds);
                        }
                        Vector3 local = bb.center - art.position;
                        baseScale = d.height / Mathf.Max(0.01f, bb.size.y);
                        artOffset = new Vector2(-local.x * baseScale, -(local.y - bb.extents.y) * baseScale);
                    }
                    Height = d.height;
                    break;
                }
            }

            if (d.flying)
            {
                artOffset.y += 0.7f;
                Height += 0.7f;
                shadow.transform.localScale *= 0.75f;
            }
            renderers = art.GetComponentsInChildren<SpriteRenderer>(true);
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (d.visual != VisualKind.Prefab) renderers[i].color = d.tint;
                baseColors[i] = renderers[i].color;
            }
            ApplyTransform(Vector2.zero, Vector2.one, 0f);
        }

        public void SetSelected(bool on) { if (ring != null) ring.enabled = on; }

        public void SetMoving(bool m)
        {
            moving = m;
            if (IsTS && attackT < 0f && loopKey == null)
                PlayBase();
        }

        void PlayBase()
        {
            if (moving) anim.Play(GameArt.I.Set(runKey), 12f, true);
            else anim.Play(GameArt.I.Set(idleKey), 10f, true);
        }

        /// <summary>Đổi bộ hoạt ảnh nghỉ/chạy (dùng cho Thợ khi cầm công cụ hay mang tài nguyên).</summary>
        public void SetAnimKeys(string idle, string run)
        {
            if (!IsTS) return;
            if (idle == idleKey && run == runKey) return;
            idleKey = idle; runKey = run;
            if (attackT < 0f && loopKey == null) PlayBase();
        }

        /// <summary>Phát lặp một hoạt ảnh đặc biệt (chặt cây, đào vàng, gõ búa).</summary>
        public void PlayLoop(string key, float fps)
        {
            if (!IsTS) return;
            loopKey = key; loopFps = fps;
            anim.Play(GameArt.I.Set(key), fps, true);
        }

        public void StopLoop()
        {
            if (loopKey == null) return;
            loopKey = null;
            if (IsTS) PlayBase();
        }

        public void Face(float dx)
        {
            if (Mathf.Abs(dx) < 0.01f) return;
            facing = dx > 0 ? 1 : -1;
        }

        /// <summary>Bắt đầu đòn đánh. Trả về thời gian tới lúc gây sát thương.</summary>
        public float PlayAttack(Vector2 dir)
        {
            attackDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right * facing;
            Face(attackDir.x);
            loopKey = null;
            if (IsTS && def.attacks != null && def.attacks.Length > 0)
            {
                string key = def.attacks[0];
                if (def.attacks.Length == 2) key = def.attacks[UnityEngine.Random.Range(0, 2)];
                else if (def.attacks.Length == 5)
                {
                    // Thương binh: chọn hướng đâm theo góc
                    float ang = Mathf.Atan2(attackDir.y, Mathf.Abs(attackDir.x)) * Mathf.Rad2Deg;
                    if (ang > 67f) key = def.attacks[3];
                    else if (ang > 22f) key = def.attacks[1];
                    else if (ang < -67f) key = def.attacks[4];
                    else if (ang < -22f) key = def.attacks[2];
                    else key = def.attacks[0];
                }
                var frames = GameArt.I.Set(key);
                float dur = Mathf.Clamp(frames.Length * 0.075f, 0.25f, Mathf.Min(0.85f, def.cooldown * 0.9f));
                attackDur = dur;
                attackT = 0f;
                anim.Play(frames, frames.Length / dur, false, true, () => { attackT = -1f; PlayBase(); });
                return dur * 0.55f;
            }
            attackDur = 0.36f;
            attackT = 0f;
            return 0.16f;
        }

        public void Flash() { flashT = 0.14f; }

        public void Die(Action done)
        {
            if (dying) return;
            dying = true;
            if (ring != null) ring.enabled = false;
            if (anim != null) anim.enabled = false;
            foreach (var an in GetComponentsInChildren<Animator>()) an.speed = 0f;
            StartCoroutine(DieRoutine(done));
        }

        System.Collections.IEnumerator DieRoutine(Action done)
        {
            float t = 0f;
            while (t < 0.7f)
            {
                t += Time.deltaTime;
                alpha = 1f - t / 0.7f;
                body.localPosition = new Vector3(0f, -t * 0.2f, 0f);
                body.localRotation = Quaternion.Euler(0, 0, t * 60f * facing);
                ApplyColors();
                if (shadow != null) shadow.color = new Color(0, 0, 0, 0.28f * alpha);
                yield return null;
            }
            done?.Invoke();
        }

        void Update()
        {
            if (dying) return;
            float dt = Time.deltaTime;
            idleT += dt;
            if (moving) moveT += dt;

            Vector2 offset = Vector2.zero;
            Vector2 squash = Vector2.one;
            float rot = 0f;

            if (attackT >= 0f)
            {
                attackT += dt;
                float p = Mathf.Clamp01(attackT / attackDur);
                if (!IsTS)
                {
                    float k = Mathf.Sin(p * Mathf.PI);
                    offset = attackDir * k * 0.28f;
                    squash = new Vector2(1f + 0.12f * k, 1f - 0.1f * k);
                    rot = -attackDir.y * 8f * k * facing;
                }
                if (p >= 1f && !IsTS) attackT = -1f;
            }
            else if (!IsTS)
            {
                if (moving)
                {
                    float s = Mathf.Sin(moveT * 10f);
                    offset.y = Mathf.Abs(s) * 0.08f * Mathf.Max(1f, Height * 0.6f);
                    rot = s * 5f;
                }
                else
                {
                    float s = Mathf.Sin(idleT * 3.2f + transform.position.x);
                    squash = new Vector2(1f - 0.02f * s, 1f + 0.035f * s);
                }
            }

            if (def.flying) offset.y += Mathf.Sin(idleT * 3f + transform.position.x) * 0.12f;
            ApplyTransform(offset, squash, rot);

            if (flashT > 0f)
            {
                flashT -= dt;
                ApplyColors();
            }
            else if (flashT > -1f)
            {
                flashT = -2f;
                ApplyColors();
            }
        }

        void ApplyTransform(Vector2 offset, Vector2 squash, float rot)
        {
            int sign = facing * (def.facesRight ? 1 : -1);
            body.localPosition = new Vector3(offset.x, offset.y, 0f);
            body.localRotation = Quaternion.Euler(0f, 0f, rot);
            art.localPosition = new Vector3(artOffset.x * sign, artOffset.y * squash.y, 0f);
            art.localScale = new Vector3(baseScale * sign * squash.x, baseScale * squash.y, 1f);
        }

        void ApplyColors()
        {
            bool flash = flashT > 0f;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = baseColors[i];
                c = new Color(c.r * statusTint.r, c.g * statusTint.g, c.b * statusTint.b, c.a);
                if (flash) c = new Color(1f, c.g * 0.45f, c.b * 0.45f, c.a);
                c.a *= alpha;
                renderers[i].color = c;
            }
        }

        public void SetStatusTint(Color c)
        {
            if (c == statusTint) return;
            statusTint = c;
            ApplyColors();
        }

        public void SetAlpha(float a)
        {
            alpha = a;
            ApplyColors();
        }
    }
}
