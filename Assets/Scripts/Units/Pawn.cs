using UnityEngine;

namespace Oath
{
    /// <summary>
    /// Thợ (Pawn): tự động chặt cây/đào vàng rồi mang về lâu đài, xây và sửa công trình,
    /// tránh giao chiến — chạy về lâu đài khi có địch tới gần.
    /// </summary>
    public class Pawn : MonoBehaviour
    {
        public enum PawnState { Idle, ToResource, Gathering, Returning, Fleeing, ToWork, Working, Manual }

        const string P = "Tiny Swords/Pawn and Resources/Pawn/Blue Pawn/Pawn_";

        public PawnState State { get; private set; }
        Unit u;
        ResourceNode node;
        ResourceType carryType;
        int carry;
        float timer;
        Building work;
        float manualUntil;
        bool countedOnNode;

        Unit U => u != null ? u : (u = GetComponent<Unit>());

        public string StatusText
        {
            get
            {
                switch (State)
                {
                    case PawnState.ToResource: return carryType == ResourceType.Wood ? "Đi chặt gỗ" : "Đi đào vàng";
                    case PawnState.Gathering: return carryType == ResourceType.Wood ? "Đang chặt gỗ" : "Đang đào vàng";
                    case PawnState.Returning: return "Mang " + (carryType == ResourceType.Wood ? "gỗ" : "vàng") + " về lâu đài";
                    case PawnState.Fleeing: return "Đang lánh nạn!";
                    case PawnState.ToWork: return "Tới công trình";
                    case PawnState.Working: return work != null && work.constructing ? "Đang xây dựng" : "Đang sửa chữa";
                    case PawnState.Manual: return "Di chuyển theo lệnh";
                    default: return "Đang nghỉ";
                }
            }
        }

        public bool IsBusyWorking => State == PawnState.ToWork || State == PawnState.Working;
        public Building WorkTarget => work;

        void OnDestroy() { ReleaseNode(); LeaveWork(); }

        public void Think()
        {
            var me = U;
            if (State != PawnState.Fleeing && State != PawnState.Manual && World.HostileNear(me.Pos, Team.Player, 2.6f))
            {
                StartFlee();
                return;
            }

            switch (State)
            {
                case PawnState.Idle:
                    ChooseTask();
                    break;
                case PawnState.Fleeing:
                    if (!me.HasPath && !World.HostileNear(me.Pos, Team.Player, 4.5f)) SetIdle();
                    break;
                case PawnState.ToResource:
                    if (node == null || node.amount <= 0) { ReleaseNode(); SetIdle(); }
                    else if (!me.HasPath)
                    {
                        if (Vector2.Distance(me.Pos, node.Pos) <= 1.55f) StartGather();
                        else GoToNode();
                    }
                    break;
                case PawnState.Gathering:
                    if (node == null) { ReleaseNode(); SetIdle(); }
                    break;
                case PawnState.Returning:
                    if (World.Castle == null || !World.Castle.health.Alive) { SetIdle(); break; }
                    if (!me.HasPath)
                    {
                        if (World.Castle.DistanceTo(me.Pos) <= 1.1f) Deliver();
                        else GoHome();
                    }
                    break;
                case PawnState.ToWork:
                    if (!WorkValid()) { LeaveWork(); SetIdle(); }
                    else if (!me.HasPath)
                    {
                        if (work.DistanceTo(me.Pos) <= 1.2f) StartWork();
                        else GoWork();
                    }
                    break;
                case PawnState.Working:
                    if (!WorkValid()) { LeaveWork(); SetIdle(); }
                    else me.visual.Face(work.Base.x - me.Pos.x);
                    break;
                case PawnState.Manual:
                    if (!me.HasPath && Time.time > manualUntil) SetIdle();
                    break;
            }
        }

        void Update()
        {
            if (State != PawnState.Gathering || !U.IsAlive) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) FinishGather();
        }

        // ---------------- Lệnh từ người chơi ----------------

        public void OnManualMove()
        {
            ReleaseNode();
            LeaveWork();
            U.visual.StopLoop();
            State = PawnState.Manual;
            manualUntil = Time.time + 5f;
            UpdateAnim();
        }

        public void AssignNode(ResourceNode n)
        {
            if (n == null || n.amount <= 0) return;
            LeaveWork();
            ReleaseNode();
            node = n;
            node.gatherers++;
            countedOnNode = true;
            carryType = n.type;
            if (carry > 0 && carryType != n.type) carry = 0;
            State = PawnState.ToResource;
            GoToNode();
        }

        public void AssignWork(Building b)
        {
            if (b == null || !b.health.Alive) return;
            ReleaseNode();
            LeaveWork();
            work = b;
            work.assignedPawns++;
            State = PawnState.ToWork;
            GoWork();
        }

        // ---------------- Nội bộ ----------------

        void ChooseTask()
        {
            // 1) Công trình cần thợ
            Building best = null;
            float bd = float.MaxValue;
            foreach (var b in World.Buildings)
            {
                if (!b.health.Alive || b.team != Team.Player || !b.NeedsWorker || b.assignedPawns > 0) continue;
                if (World.HostileNear(b.Base, Team.Player, 3f)) continue;
                float d = b.DistanceTo(U.Pos);
                if (d < bd) { bd = d; best = b; }
            }
            if (best != null) { AssignWork(best); return; }

            // 2) Đang mang tài nguyên thì về nộp
            if (carry > 0) { State = PawnState.Returning; GoHome(); return; }

            // 3) Chọn loại tài nguyên đang thiếu
            // Chia đều Thợ giữa gỗ và vàng; hoà thì ưu tiên loại đang ít hơn
            int onWood = 0, onGold = 0;
            foreach (var n2 in World.ResourceNodes)
            {
                if (n2 == null) continue;
                if (n2.type == ResourceType.Wood) onWood += n2.gatherers; else onGold += n2.gatherers;
            }
            var rm = ResourceManager.I;
            ResourceType want = onWood < onGold ? ResourceType.Wood : (onGold < onWood ? ResourceType.Gold :
                (rm != null && rm.Gold < rm.Wood ? ResourceType.Gold : ResourceType.Wood));
            var n = FindNode(want) ?? FindNode(want == ResourceType.Wood ? ResourceType.Gold : ResourceType.Wood);
            if (n != null) { AssignNode(n); return; }

            // 4) Không có việc: đứng cạnh lâu đài
            UpdateAnim();
        }

        ResourceNode FindNode(ResourceType type)
        {
            ResourceNode best = null;
            float bd = float.MaxValue;
            foreach (var n in World.ResourceNodes)
            {
                if (n == null || n.type != type || n.amount <= 0) continue;
                if (n.gatherers >= (type == ResourceType.Gold ? 4 : 2)) continue;
                if (World.HostileNear(n.Pos, Team.Player, 3.5f)) continue;
                float d = Vector2.Distance(U.Pos, n.Pos) + (World.Castle != null ? Vector2.Distance(n.Pos, World.Castle.Base) * 0.6f : 0f);
                if (d < bd) { bd = d; best = n; }
            }
            return best;
        }

        void GoToNode()
        {
            if (node == null) return;
            if (!U.MoveTo(node.Pos)) { ReleaseNode(); SetIdle(); return; }
            UpdateAnim();
        }

        void StartGather()
        {
            State = PawnState.Gathering;
            timer = Balance.GatherTime;
            U.StopMoving();
            U.visual.Face(node.Pos.x - U.Pos.x);
            U.visual.PlayLoop(P + (node.type == ResourceType.Wood ? "Interact Axe" : "Interact Pickaxe"), 10f);
            node.Shake();
        }

        void FinishGather()
        {
            U.visual.StopLoop();
            if (node != null)
            {
                carry = node.Take(Balance.GatherAmount);
                carryType = node.type;
                if (node == null || node.amount <= 0) ReleaseNode();
            }
            if (carry > 0) { State = PawnState.Returning; GoHome(); }
            else SetIdle();
        }

        void GoHome()
        {
            if (World.Castle == null) { SetIdle(); return; }
            Vector2 goal = World.Castle.DoorPoint;
            U.MoveTo(goal);
            UpdateAnim();
        }

        void Deliver()
        {
            if (carry > 0 && ResourceManager.I != null)
            {
                ResourceManager.I.Add(carryType, carry);
                FX.Text(U.Pos + Vector2.up * 1.3f, "+" + carry + (carryType == ResourceType.Wood ? " gỗ" : " vàng"),
                    carryType == ResourceType.Wood ? new Color(0.95f, 0.8f, 0.55f) : new Color(1f, 0.9f, 0.3f), 0.8f);
            }
            carry = 0;
            // Quay lại nguồn cũ nếu còn
            if (node != null && node.amount > 0) { State = PawnState.ToResource; GoToNode(); }
            else SetIdle();
        }

        void StartFlee()
        {
            U.visual.StopLoop();
            LeaveWork();
            ReleaseNode();
            State = PawnState.Fleeing;
            if (World.Castle != null)
            {
                var threat = World.NearestUnit(U.Pos, Team.Enemy, 8f);
                Vector2 c = World.Castle.WorldRect.center;
                Vector2 away = threat != null ? (c - threat.Pos).normalized : Vector2.up;
                Vector2 goal = c + away * (World.Castle.cells.width * 0.5f + 2.5f);
                if (World.Nav != null)
                {
                    var cell = World.Nav.NearestWalkable(NavGrid.Cell(goal), 5);
                    goal = MapData.CellCenter(cell);
                }
                U.MoveTo(goal);
            }
            UpdateAnim();
        }

        bool WorkValid() => work != null && work.health.Alive && work.NeedsWorker;

        void GoWork()
        {
            if (work == null) return;
            U.MoveTo(work.ApproachFrom(U.Pos));
            UpdateAnim();
        }

        void StartWork()
        {
            State = PawnState.Working;
            U.StopMoving();
            work.activeWorkers++;
            U.visual.Face(work.Base.x - U.Pos.x);
            U.visual.PlayLoop(P + "Interact Hammer", 8f);
        }

        void LeaveWork()
        {
            if (work == null) return;
            if (State == PawnState.Working) work.activeWorkers = Mathf.Max(0, work.activeWorkers - 1);
            work.assignedPawns = Mathf.Max(0, work.assignedPawns - 1);
            work = null;
            if (u != null && u.visual != null) u.visual.StopLoop();
        }

        void ReleaseNode()
        {
            if (node != null && countedOnNode) node.gatherers = Mathf.Max(0, node.gatherers - 1);
            countedOnNode = false;
            node = null;
        }

        void SetIdle()
        {
            State = PawnState.Idle;
            U.visual.StopLoop();
            UpdateAnim();
        }

        public void OnArrived() { UpdateAnim(); }

        void UpdateAnim()
        {
            string idle = P + "Idle", run = P + "Run";
            if (carry > 0)
            {
                idle = P + (carryType == ResourceType.Wood ? "Idle Wood" : "Idle Gold");
                run = P + (carryType == ResourceType.Wood ? "Run Wood" : "Run Gold");
            }
            else if (State == PawnState.ToResource && node != null)
            {
                idle = P + (node.type == ResourceType.Wood ? "Idle Axe" : "Idle Pickaxe");
                run = P + (node.type == ResourceType.Wood ? "Run Axe" : "Run Pickaxe");
            }
            else if (State == PawnState.ToWork)
            {
                idle = P + "Idle Hammer"; run = P + "Run Hammer";
            }
            U.visual.SetAnimKeys(idle, run);
        }
    }
}
