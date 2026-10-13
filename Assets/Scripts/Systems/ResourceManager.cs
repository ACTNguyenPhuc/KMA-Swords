using System;
using UnityEngine;

namespace Oath
{
    /// <summary>Cộng/trừ gỗ, vàng; kiểm tra đủ tài nguyên và giới hạn quân số trước khi xây/tuyển.</summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager I { get; private set; }

        public int Wood { get; private set; }
        public int Gold { get; private set; }
        public int BasePopulation { get; private set; } = 8;
        public event Action OnChanged;

        float passiveTimer;

        public void Init(LevelConfig cfg)
        {
            I = this;
            Wood = cfg.startWood;
            Gold = cfg.startGold;
            BasePopulation = cfg.basePopulation;
            Unit.AnyDied += OnUnitDied;
            Unit.AnySpawned += _ => OnChanged?.Invoke();
            Building.AnyCompleted += _ => OnChanged?.Invoke();
            Building.AnyDestroyed += _ => OnChanged?.Invoke();
            OnChanged?.Invoke();
        }

        void OnDestroy()
        {
            Unit.AnyDied -= OnUnitDied;
            if (I == this) I = null;
        }

        void OnUnitDied(Unit u)
        {
            if (u.def.team == Team.Enemy && u.def.bounty > 0)
            {
                Gold += u.def.bounty;
                FX.Text(u.Pos + Vector2.up * (u.VisualHeight + 0.3f), "+" + u.def.bounty, new Color(1f, 0.88f, 0.3f), 0.7f);
            }
            OnChanged?.Invoke();
        }

        void Update()
        {
            if (GameManager.I == null || GameManager.I.State != GameState.Playing) return;
            passiveTimer += Time.deltaTime;
            if (passiveTimer >= Balance.PassiveGoldInterval)
            {
                passiveTimer = 0f;
                Gold += Balance.PassiveGold;
                OnChanged?.Invoke();
            }
        }

        public int PopulationCap
        {
            get
            {
                int cap = BasePopulation;
                foreach (var b in World.Buildings)
                    if (b.type == BuildingType.House && !b.constructing && b.health.Alive) cap += Balance.HousePopulation;
                return cap;
            }
        }

        public int PopulationUsed
        {
            get
            {
                int n = World.CountUnits(Team.Player);
                foreach (var b in World.Buildings) n += b.queue.Count;
                return n;
            }
        }

        public bool CanAfford(int wood, int gold) => Wood >= wood && Gold >= gold;

        public bool TrySpend(int wood, int gold, bool notify = true)
        {
            if (!CanAfford(wood, gold))
            {
                if (notify)
                {
                    string miss = Wood < wood && Gold < gold ? "gỗ và vàng" : (Wood < wood ? "gỗ" : "vàng");
                    GameEvents.Message("Không đủ " + miss + "!", MessageKind.Warning);
                    AudioManager.Play("error", 0.6f);
                }
                return false;
            }
            Wood -= wood;
            Gold -= gold;
            OnChanged?.Invoke();
            return true;
        }

        public void Add(ResourceType type, int amount)
        {
            if (type == ResourceType.Wood) Wood += amount; else Gold += amount;
            AudioManager.Play("coin", 0.25f);
            OnChanged?.Invoke();
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            OnChanged?.Invoke();
        }

        public void Refund(int wood, int gold)
        {
            Wood += wood; Gold += gold;
            OnChanged?.Invoke();
        }

        public void NotifyChanged() => OnChanged?.Invoke();
    }
}
