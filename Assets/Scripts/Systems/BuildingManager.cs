using UnityEngine;

namespace Oath
{
    /// <summary>Đặt công trình tại ô hợp lệ, áp giá, nâng cấp, sửa chữa và tuyển quân.</summary>
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager I { get; private set; }
        LevelConfig cfg;

        public void Init(LevelConfig c)
        {
            I = this;
            cfg = c;
        }

        void OnDestroy() { if (I == this) I = null; }

        public static int CostWood(BuildingType t) => t == BuildingType.Tower ? TowerCatalog.Get(TowerType.Archer).wood[0] : Balance.HouseWood;
        public static int CostGold(BuildingType t) => t == BuildingType.Tower ? TowerCatalog.Get(TowerType.Archer).gold[0] : Balance.HouseGold;

        public const float SellRefund = 0.6f;

        public bool IsTowerAllowed(TowerType t)
        {
            return cfg == null || cfg.towers == null || cfg.towers.Count == 0 || cfg.towers.Contains(t);
        }

        public bool TryBuildTower(BuildPlot plot, TowerType t)
        {
            if (plot == null || plot.Occupied || !IsTowerAllowed(t)) return false;
            var d = TowerCatalog.Get(t);
            if (World.HostileNear(plot.Center, Team.Player, 2.5f))
            {
                GameEvents.Message("Không thể xây khi có địch ở gần!", MessageKind.Warning);
                AudioManager.Play("error", 0.6f);
                return false;
            }
            if (!ResourceManager.I.TrySpend(d.wood[0], d.gold[0])) return false;
            var b = Building.Create(BuildingType.Tower, plot.cells, true, plot, 0f, t);
            b.totalSpent = d.wood[0] + d.gold[0];
            AssignNearestPawn(b);
            AudioManager.Play("build", 0.6f);
            GameEvents.Message("Bắt đầu xây " + d.name + "…", MessageKind.Info);
            return true;
        }

        public int SellValue(Building b) => b == null ? 0 : Mathf.RoundToInt(b.totalSpent * SellRefund);

        /// <summary>Bán công trình trên ô xây dựng, hoàn lại 60% tổng chi phí (chia đều gỗ/vàng).</summary>
        public bool TrySell(Building b)
        {
            if (b == null || b.plot == null || (b.type != BuildingType.Tower && b.type != BuildingType.House)) return false;
            int v = SellValue(b);
            int wood = v / 2, gold = v - v / 2;
            ResourceManager.I.Refund(wood, gold);
            FX.Text(b.Base + Vector2.up * 2f, "+" + wood + " gỗ  +" + gold + " vàng", UIKit.Gold, 0.8f);
            GameEvents.Message("Đã bán " + b.displayName + ".", MessageKind.Info);
            b.Dismantle();
            return true;
        }

        public void CycleTargetMode(Building b)
        {
            if (b == null || b.tower == null) return;
            b.targetMode = (TargetMode)(((int)b.targetMode + 1) % 3);
            GameEvents.Message("Ưu tiên mục tiêu: " + TowerCatalog.TargetModeName(b.targetMode), MessageKind.Info);
        }

        public bool TryBuild(BuildPlot plot, BuildingType type)
        {
            if (type == BuildingType.Tower) return TryBuildTower(plot, TowerType.Archer);
            if (plot == null || plot.Occupied) return false;
            if (World.HostileNear(plot.Center, Team.Player, 2.5f))
            {
                GameEvents.Message("Không thể xây khi có địch ở gần!", MessageKind.Warning);
                AudioManager.Play("error", 0.6f);
                return false;
            }
            if (!ResourceManager.I.TrySpend(CostWood(type), CostGold(type))) return false;
            var b = Building.Create(type, plot.cells, true, plot);
            b.totalSpent = CostWood(type) + CostGold(type);
            AssignNearestPawn(b);
            AudioManager.Play("build", 0.6f);
            GameEvents.Message("Bắt đầu xây " + b.displayName.ToLower() + "…", MessageKind.Info);
            return true;
        }

        public bool CanUpgrade(Building b, out int wood, out int gold)
        {
            wood = gold = 0;
            if (b == null || b.type != BuildingType.Tower || b.constructing) return false;
            if (!cfg.allowTowerUpgrade || b.level >= Mathf.Min(3, cfg.maxTowerLevel)) return false;
            var d = TowerCatalog.Get(b.towerType);
            wood = d.wood[b.level];
            gold = d.gold[b.level];
            return true;
        }

        public bool TryUpgrade(Building b)
        {
            if (!CanUpgrade(b, out int w, out int g)) return false;
            if (!ResourceManager.I.TrySpend(w, g)) return false;
            b.totalSpent += w + g;
            b.Upgrade();
            GameEvents.Message(b.displayName + " đã lên cấp " + b.level + "!", MessageKind.Good);
            return true;
        }

        public int RepairCost(Building b)
        {
            if (b == null) return 0;
            float missing = 1f - b.health.Ratio;
            int baseCost = b.type == BuildingType.Castle ? 120 : 50;
            return Mathf.CeilToInt(missing * baseCost);
        }

        public bool TryRepair(Building b)
        {
            if (b == null || b.constructing || b.repairing || b.health.Ratio >= 0.999f) return false;
            if (World.HostileNear(b.Base, Team.Player, 3f))
            {
                GameEvents.Message("Không thể sửa khi có địch ở gần!", MessageKind.Warning);
                AudioManager.Play("error", 0.6f);
                return false;
            }
            int cost = RepairCost(b);
            if (!ResourceManager.I.TrySpend(cost, 0)) return false;
            b.repairing = true;
            if (!AssignNearestPawn(b))
                GameEvents.Message("Không có Thợ rảnh — sửa chữa sẽ chậm hơn.", MessageKind.Info);
            else GameEvents.Message("Thợ đang tới sửa " + b.displayName.ToLower() + ".", MessageKind.Info);
            return true;
        }

        bool AssignNearestPawn(Building b)
        {
            Pawn best = null;
            float bd = float.MaxValue;
            foreach (var u in World.Units)
            {
                if (!u.IsAlive || u.pawn == null || u.pawn.IsBusyWorking) continue;
                if (u.pawn.State == Pawn.PawnState.Fleeing) continue;
                float d = b.DistanceTo(u.Pos);
                if (d < bd) { bd = d; best = u.pawn; }
            }
            if (best == null) return false;
            best.AssignWork(b);
            return true;
        }

        public bool TryTrain(Building b, string unitId)
        {
            if (b == null || !b.CanTrain) return false;
            var def = UnitCatalog.Get(unitId);
            var rm = ResourceManager.I;
            if (b.queue.Count >= 5)
            {
                GameEvents.Message("Hàng chờ đã đầy!", MessageKind.Warning);
                AudioManager.Play("error", 0.6f);
                return false;
            }
            if (rm.PopulationUsed + def.pop > rm.PopulationCap)
            {
                GameEvents.Message("Đã đạt giới hạn quân số — hãy xây thêm Nhà ở!", MessageKind.Warning);
                AudioManager.Play("error", 0.6f);
                return false;
            }
            if (!rm.TrySpend(def.costWood, def.costGold)) return false;
            b.queue.Add(unitId);
            rm.NotifyChanged();
            AudioManager.Play("click", 0.5f);
            return true;
        }

        public void CancelLast(Building b)
        {
            if (b == null || b.queue.Count == 0) return;
            var def = UnitCatalog.Get(b.queue[b.queue.Count - 1]);
            b.queue.RemoveAt(b.queue.Count - 1);
            if (b.queue.Count == 0) b.trainTimer = 0f;
            ResourceManager.I.Refund(def.costWood, def.costGold);
        }
    }
}
