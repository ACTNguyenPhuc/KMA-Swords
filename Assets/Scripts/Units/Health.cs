using System;
using UnityEngine;

namespace Oath
{
    /// <summary>
    /// Quản lý máu: nhận sát thương, hồi máu, chết và phát sự kiện cập nhật UI.
    /// Máu mới = Máu hiện tại − (Sát thương − Giáp), tối thiểu 1.
    /// </summary>
    public class Health : MonoBehaviour
    {
        public float max = 100f;
        public float current = 100f;
        public float armor;
        public Team team;
        public Unit unit;          // một trong hai được gán
        public Building building;
        public bool invulnerable;

        public event Action<Health> OnChanged;
        public event Action<Health, Health> OnDamaged;   // (bản thân, nguồn)
        public event Action<Health> OnDied;

        public bool Alive => current > 0f && this != null;
        public float Ratio => max <= 0 ? 0 : current / max;
        public Health LastAttacker { get; private set; }

        public void Init(float maxHp, float armorValue, Team t)
        {
            max = maxHp; current = maxHp; armor = armorValue; team = t;
            OnChanged?.Invoke(this);
        }

        public void SetCurrent(float v)
        {
            current = Mathf.Clamp(v, 0f, max);
            OnChanged?.Invoke(this);
        }

        public void TakeDamage(float damage, Health source, bool pierce = false)
        {
            if (!Alive || invulnerable) return;
            float dmg = Mathf.Max(1f, damage - (pierce ? 0f : armor));
            current -= dmg;
            LastAttacker = source;
            OnDamaged?.Invoke(this, source);
            if (current <= 0f)
            {
                current = 0f;
                OnChanged?.Invoke(this);
                OnDied?.Invoke(this);
            }
            else OnChanged?.Invoke(this);
        }

        public void Heal(float amount)
        {
            if (!Alive) return;
            current = Mathf.Min(max, current + amount);
            OnChanged?.Invoke(this);
        }

        /// <summary>Khoảng cách từ điểm tới rìa đối tượng.</summary>
        public float DistanceTo(Vector2 p)
        {
            if (building != null) return building.DistanceTo(p);
            if (unit != null) return Mathf.Max(0f, Vector2.Distance(p, unit.Pos) - unit.def.bodyRadius);
            return Vector2.Distance(p, transform.position);
        }

        /// <summary>Điểm ngắm cho đạn (giữa thân).</summary>
        public Vector2 AimPoint
        {
            get
            {
                if (building != null) return building.AimPoint;
                if (unit != null) return unit.Pos + Vector2.up * unit.VisualHeight * 0.45f;
                return transform.position;
            }
        }

        /// <summary>Điểm đứng gần nhất để tiếp cận đối tượng từ vị trí p.</summary>
        public Vector2 ApproachPoint(Vector2 p)
        {
            if (building != null) return Util.ClosestPointOnRect(building.WorldRect, p);
            return transform.position;
        }
    }
}
