using System.Collections.Generic;
using System;
using UnityEngine;
using Priority_Queue;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{

    public enum Tool { None, Hammer, Pickaxe, Axe };

    public interface IJob
    {
        IEnumerable<Task> AvailableTasks();
        void ClaimTask(Minion minion, Task task);
        void AbandonTask(Minion minion, Task task);

        // Returns a follow up task or null if none
        Task CompleteTask(Minion minion, Task task);
    }

    public class JobWalkDummy : IJob
    {
        public IEnumerable<Task> AvailableTasks() => throw new NotImplementedException();
        public void ClaimTask(Minion minion, Task task) { }
        public Task CompleteTask(Minion minion, Task task) => null;
        public void AbandonTask(Minion minion, Task task) { }

        public Task CreateWalkTask(Vec2I pos)
            => new Task(this, null, pos, v => v == pos, Tool.None, MinionAnim.None, 0);

        public Task CreateVacateTask(Vec2I pos)
            => new Task(this, null, pos, v => v != pos, Tool.None, MinionAnim.None, 0);
    }

    public abstract class JobStandard : IJob
    {
        protected GameController game { get; private set; }
        protected Vec2I pos { get; private set; }
        protected ITile tile { get; private set; }

        protected JobStandard(GameController game, Vec2I pos)
        {
            this.game = game;
            this.pos = pos;
            tile = game.Tile(pos);
            tile.K_activeJob = this;
        }

        private Task Finish()
        {
            BB.Assert(tile.K_activeJob == this);
            tile.K_activeJob = null;
            game.RemoveJob(this);
            return null;
        }

#if DEBUG
        private bool D_finished = false;
        private Task D_lastTaskOffered = null;
        private readonly Dictionary<Task, Minion> D_outstandingTasks = new Dictionary<Task, Minion>();

        public IEnumerable<Task> AvailableTasks()
        {
            BB.Assert(!D_finished);
            BB.Assert(D_lastTaskOffered == null);

            foreach (Task task in GetAvailableTasks())
            {
                D_lastTaskOffered = task;
                yield return task;
            }

            D_lastTaskOffered = null;
        }

        public void ClaimTask(Minion minion, Task task)
        {
            //Debug.Log("Task claimed job{" + GetHashCode() + "} + task{" + task.GetHashCode() + "} + minion{"+minion.GetHashCode() +"}");
            BB.Assert(!D_finished);
            BB.Assert(task == D_lastTaskOffered);
            BB.Assert(!D_outstandingTasks.ContainsKey(task));
            D_outstandingTasks.Add(task, minion);

            D_lastTaskOffered = null;
            OnClaimTask(minion, task);
        }

        public void AbandonTask(Minion minion, Task task)
        {
            BB.Assert(!D_finished);
            BB.Assert(D_lastTaskOffered == null);
            BB.Assert(D_outstandingTasks.ContainsKey(task));
            BB.Assert(D_outstandingTasks[task] == minion);
            D_outstandingTasks.Remove(task);

            OnAbandonTask(minion, task);
        }

        public Task CompleteTask(Minion minion, Task task)
        {
            //Debug.Log("Task completed job{" + GetHashCode() + "} + task{" + task.GetHashCode() + "} + minion{"+minion.GetHashCode()+"}");
            BB.Assert(!D_finished);
            BB.Assert(D_lastTaskOffered == null);
            BB.Assert(D_outstandingTasks.ContainsKey(task));
            BB.Assert(D_outstandingTasks[task] == minion);
            D_outstandingTasks.Remove(task);

            D_lastTaskOffered = OnCompleteTask(minion, task);
            return D_lastTaskOffered;
        }

        protected Task FinishJob()
        {
            BB.Assert(!D_finished);
            BB.Assert(D_outstandingTasks.Count == 0);

            D_finished = true;
            return Finish();
        }

#else
    public IEnumerable<Task> AvailableTasks() => GetAvailableTasks();
    public void ClaimTask(Minion minion, Task task) => OnClaimTask(minion, task);
    public void AbandonTask(Minion minion, Task task) => OnAbandonTask(minion, task);
    public Task CompleteTask(Minion minion, Task task) => OnCompleteTask(minion, task);
    protected Task FinishJob() => Finish();
#endif

        public abstract IEnumerable<Task> GetAvailableTasks();
        public abstract void OnClaimTask(Minion minion, Task task);
        public abstract void OnAbandonTask(Minion minion, Task task);
        public abstract Task OnCompleteTask(Minion minion, Task task);
    }

    public class JobMine : JobStandard
    {
        private readonly Task task;
        private readonly Transform overlay;
        private bool claimed;

        public static Transform CreateOverlay(GameController game, Vec2I pos)
            => game.CreateJobOverlay(pos, game.assets.sprites.Get(game.defs.Get<SpriteDef>("BB:MineOverlay")));

        public JobMine(GameController game, Vec2I pos) : base(game, pos)
        {
            BB.Assert(tile.hasBuilding);
            BB.Assert(tile.building.K_mineable);

            task = new Task(this, null, pos, v => v.Adjacent(pos), tile.building.miningTool, MinionAnim.Slash, 2);
            claimed = false;

            overlay = CreateOverlay(game, pos);
        }

        public override IEnumerable<Task> GetAvailableTasks()
        {
            if (!claimed)
                yield return task;
        }

        public override void OnClaimTask(Minion minion, Task task)
        {
            BB.Assert(task == this.task);
            BB.Assert(!claimed);

            claimed = true;
        }

        public override void OnAbandonTask(Minion minion, Task task)
        {
            BB.Assert(task == this.task);
            BB.Assert(claimed);

            claimed = false;
        }

        public override Task OnCompleteTask(Minion minion, Task task)
        {
            BB.Assert(task == this.task);
            BB.Assert(claimed);

            IBuilding building = tile.building;
            game.RemoveBuilding(pos);
            foreach (ItemInfo item in building.GetMinedMaterials())
                game.DropItem(pos, item);

            overlay.Destroy();
            return FinishJob();
        }
    }

    // TODO: support hualing to multiple jobs
    public class JobBuild : JobStandard
    {
        private class HaulTaskInfo : ITaskInfo
        {
            public readonly HaulInfo haul;
            public readonly int amt;
            public bool isPickupTask;
            public Item item;

            public HaulTaskInfo(HaulInfo haul, int amt, Item item)
            {
                BB.Assert(amt > 0);
                this.haul = haul;
                this.amt = amt;
                this.isPickupTask = true;
                this.item = item;
            }

            public void Pickup(Item item)
            {
                this.isPickupTask = false;
                this.item = item;
            }

            public override string ToString() => "HaulTask{" + isPickupTask + ", " + amt + "}";
        }

        private class HaulInfo
        {
            public readonly ItemInfo info;
            public int amtStored;
            public int amtClaimed;

            public int amtRemaining => info.amt - amtStored - amtClaimed;

            public HaulInfo(ItemInfo info)
            {
                this.info = info;
                this.amtStored = this.amtClaimed = 0;
            }

            public int HaulAmount(Item item) => Math.Min(amtRemaining, item.amtAvailable);
        }

        private class ItemPriority : FastPriorityQueueNode
        {
            public readonly Item item;
            public ItemPriority(Item item) => this.item = item;
        }

        private enum State { Hauling, BuildUnclaimed, BuildClaimed }

        public readonly IBuildingProto prototype;
        private readonly BuildingProtoConstruction.BuildingConstruction building;
        private bool vacatedTile = false;

        private State state;
        private readonly List<HaulInfo> hauls;

        public JobBuild(GameController game, Vec2I pos, IBuildingProto prototype) : base(game, pos)
        {
            BB.Assert(prototype != null);
            this.prototype = prototype;
            building = BuildingProtoConstruction.K_single.Create(this);
            game.AddBuilding(pos, building);

            state = State.Hauling;
            hauls = new List<HaulInfo>();
            foreach (var item in prototype.GetBuildMaterials())
                hauls.Add(new HaulInfo(item));
        }

        public override IEnumerable<Task> GetAvailableTasks()
        {
            if (state == State.BuildClaimed)
                yield break;

            if (state == State.BuildUnclaimed)
            {
                Task task = TryTaskBuild(null);
                if (task != null)
                    yield return task;

                yield break;
            }

            // TODO: priority should take into account minion locations
            foreach (HaulInfo haul in hauls)
            {
                if (haul.amtRemaining > 0)
                {
                    var items = new List<Item>(game.FindItems(haul.info.def));
                    if (items.Count == 0)
                        continue;

                    var queue = new FastPriorityQueue<ItemPriority>(items.Count);
                    foreach (Item item in items)
                        queue.Enqueue(new ItemPriority(item), Vec2.Distance(item.pos, pos) / (float)haul.HaulAmount(item));

                    foreach (var p in queue)
                    {
                        Item item = p.item;
                        if (item.amtAvailable == 0)
                            continue;

                        int amt = haul.HaulAmount(item);
                        Item item2 = item;
                        var info = new HaulTaskInfo(haul, amt, item);
                        yield return new Task(this, info, item.pos, v => v == item.pos, Tool.None, MinionAnim.Magic, .425f);
                    }
                }
            }
        }

        public override void OnClaimTask(Minion minion, Task task)
        {
            BB.Assert(state != State.BuildClaimed);

            if (state == State.BuildUnclaimed)
                state = State.BuildClaimed;
            else
            {
                HaulTaskInfo info = (HaulTaskInfo)task.info;
                if (info.isPickupTask)
                {
                    info.haul.amtClaimed += info.amt;
                    info.item.Claim(info.amt);
                    BB.Assert(info.haul.amtRemaining >= 0);
                }
            }
        }

        public override void OnAbandonTask(Minion minion, Task task)
        {
            BB.Assert(state != State.BuildUnclaimed);
            if (state == State.BuildClaimed)
            {
                // TODO: track build progress
                // TODO: trigger pathing update when construction starts
                state = State.BuildUnclaimed;
            }
            else
            {
                HaulTaskInfo info = (HaulTaskInfo)task.info;
                if (info.isPickupTask)
                    info.item.Unclaim(info.amt);
                else
                {
                    BB.Assert(minion.carriedItem == info.item);
                    minion.DropItem();
                }

                BB.Assert(info.amt <= info.haul.amtClaimed);
                info.haul.amtClaimed -= info.amt;
            }
        }

        public override Task OnCompleteTask(Minion minion, Task task)
        {
            BB.Assert(state != State.BuildUnclaimed);
            if (state == State.BuildClaimed)
            {
                BB.Assert(tile.building == building);
                game.ReplaceBuilding(pos, prototype.CreateBuilding());

                return FinishJob();
            }
            else
            {
                HaulTaskInfo info = (HaulTaskInfo)task.info;
                if (info.isPickupTask)
                {
                    info.item.Unclaim(info.amt);
                    Item item = game.TakeItem(info.item, info.amt);
                    minion.PickupItem(item);
                    info.Pickup(item);
                    return new Task(this, info, pos, v => v == pos, Tool.None, MinionAnim.None, 0);
                }
                else
                {
                    BB.Assert(minion.carriedItem == info.item);
                    BB.Assert(info.haul.amtClaimed >= info.amt);
                    info.haul.amtClaimed -= info.amt;
                    info.haul.amtStored += info.amt;
                    BB.Assert(info.haul.amtRemaining >= 0);
                    minion.RemoveItem().Destroy();

                    if (info.haul.amtStored == info.haul.info.amt)
                    {
                        BB.Assert(info.haul.amtClaimed == 0);
                        bool doneHauling = true;
                        foreach (var haul in hauls)
                        {
                            if (haul.amtRemaining > 0)
                            {
                                doneHauling = false;
                                break;
                            }
                        }

                        if (doneHauling)
                        {
                            state = State.BuildUnclaimed;
                            return TryTaskBuild(minion);
                        }
                    }

                    return null;
                }
            }
        }

        private Task TryTaskBuild(Minion minion)
        {
            if (prototype.passable)
                vacatedTile = true;

            if (!vacatedTile)
            {
                game.VacateTile(pos);
                if (game.IsTileOccupied(pos, minion))
                    return null;

                building.constructionBegan = true;
                vacatedTile = true;
                game.RerouteMinions(pos, true);
            }

            return new Task(this, null, pos, v => v.Adjacent(pos), Tool.Hammer, MinionAnim.Slash, 2);
        }
    }

    public interface ITaskInfo { }

    public class Task
    {
        private readonly IJob job;
        public readonly ITaskInfo info;
        private readonly Func<Vec2I, bool> workFromFn;

        public readonly Vec2I pos;
        public readonly Tool tool;
        public readonly MinionAnim anim;
        public readonly float workAmt;

        private float workRemaining;

        public Task(IJob job, ITaskInfo info, Vec2I pos, Func<Vec2I, bool> workFromFn, Tool tool, MinionAnim anim, float workAmt)
        {
            BB.Assert(job != null);

            this.job = job;
            this.info = info;
            this.pos = pos;
            this.workFromFn = workFromFn;
            this.tool = tool;
            this.anim = anim;
            this.workAmt = workAmt;

            workRemaining = workAmt;
        }

        public void Claim(Minion minion) => job.ClaimTask(minion, this);

        public bool HasWork() => workRemaining != 0;

        public bool CanWorkFrom(Vec2I tile) => workFromFn(tile);

        public Vec2I[] GetPath(GameController game, Vec2I start) => game.GetPath(start, pos, workFromFn);

        // Returns true if complete
        public void PerformWork(float amt)
        {
            workRemaining = Mathf.Max(0, workRemaining - amt);
        }

        public Task Complete(Minion minion)
        {
            BB.Assert(!HasWork());
            return job.CompleteTask(minion, this);
        }

        public void Abandon(Minion minion)
        {
            job.AbandonTask(minion, this);
        }

        public override string ToString() => "Task{" + job + ", " + info + "}";
    }

}