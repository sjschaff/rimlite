using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Priority_Queue;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public enum Tool { None, Hammer, Pickaxe, Axe };

    public abstract class Task2
    {
        public enum Status { Continue, Complete, Fail }


        public readonly GameController game;
        public Work work { get; private set; }
        public Minion minion => work.minion;

        public Task2(GameController game) => this.game = game;

        public Status BeginTask(Work work)
        {
            BB.AssertNull(this.work);
            BB.AssertNotNull(work);
            BB.AssertNotNull(work.minion);
            this.work = work;
            return OnBeginTask();
        }

        public void EndTask(bool canceled) => OnEndTask(canceled);


        protected abstract Status OnBeginTask();
        protected abstract void OnEndTask(bool canceled);
        public abstract Status PerformTask(float deltaTime);
    }

    public class TaskLambda : Task2
    {
        private readonly Func<Work, bool> fn;

        public TaskLambda(GameController game, Func<Work, bool> fn)
            : base(game)
        {
            BB.AssertNotNull(fn);
            this.fn = fn;
        }

        protected override Status OnBeginTask()
            => fn(work) ? Status.Complete : Status.Fail;

        public override Status PerformTask(float deltaTime)
            => throw new NotSupportedException();

        protected override void OnEndTask(bool canceled) { }
    }

    public abstract class TaskTimed : Task2
    {
        private float workAmt;
        private readonly Vec2I workTarget;
        private readonly MinionAnim anim;
        private readonly Tool tool;

        protected abstract float WorkSpeed();
        protected abstract void OnWorkUpdated(float workAmt);

        public TaskTimed(GameController game, Vec2I workTarget,
            MinionAnim anim, Tool tool, float workAmt)
            : base(game)
        {
            this.workTarget = workTarget;
            this.anim = anim;
            this.tool = tool;
            this.workAmt = workAmt;
        }

        protected override Status OnBeginTask()
        {
            // TODO: make loading bar
            minion.skin.SetTool(tool);
            minion.skin.SetAnimLoop(anim);
            if (minion.pos != workTarget)
                minion.SetFacing(workTarget - minion.pos);

            return Status.Continue;
        }

        public override Status PerformTask(float deltaTime)
        {
            workAmt = Mathf.Max(workAmt - deltaTime * WorkSpeed(), 0);
            OnWorkUpdated(workAmt);

            if (workAmt <= 0)
                return Status.Complete;
            else
                return Status.Continue;
        }
    }


    // Task GoTo
    // Task claim items
    // Task Pickup Item
    // Task Drop Item
    // Task


    // TODO: handle claims, i.e workbenches, locations, items, etc.
    public class Work
    {
#if DEBUG
        private static int D_nextID = 0;
        public readonly int D_uniqueID;
#endif

        private readonly Queue<Task2> tasks;
        public Minion minion { get; private set; }
        private Task2 activeTask;

        public Work(Queue<Task2> tasks)
        {
#if DEBUG
            D_uniqueID = D_nextID;
            ++D_nextID;
#endif

            BB.AssertNotNull(tasks);
            BB.Assert(tasks.Any());
            this.tasks = tasks;
        }

        public Work(IEnumerable<Task2> tasks)
            : this(new Queue<Task2>(tasks)) { }

        public Work(params Task2[] tasks)
            : this((IEnumerable<Task2>)tasks) { }

        public bool Claim(Minion minion)
        {
            BB.AssertNull(this.minion);
            BB.AssertNull(activeTask);
            this.minion = minion;

            return IterateTasks();
        }

        public void Abandon(Minion minion)
        {
            BB.AssertNotNull(this.minion);
            BB.Assert(this.minion == minion);
            if (activeTask != null)
                activeTask.EndTask(true);
        }

        public void Cancel()
        {
            BB.Assert(this.minion != null);
            minion.AbandonWork();
        }

        private bool IterateTasks()
        {
            while (activeTask == null && tasks.Any())
            {
                activeTask = tasks.Dequeue();

                var status = activeTask.BeginTask(this);
                if (status == Task2.Status.Fail)
                {
                    // TODO: check if we can get an updated task
                    Cancel();
                    return false;
                }
                else if (status == Task2.Status.Continue)
                {
                    return true;
                }

                activeTask.EndTask(false);
                activeTask = null;
            }

            return false;
        }

        public void PerformWork(float deltaTime)
        {
            BB.AssertNotNull(activeTask);

            var status = activeTask.PerformTask(deltaTime);
            if (status == Task2.Status.Continue)
                return;

            minion.skin.SetTool(Tool.None);
            minion.skin.SetAnimLoop(MinionAnim.None);

            if (status == Task2.Status.Complete)
            {
                activeTask.EndTask(false);
                activeTask = null;
                if (IterateTasks())
                    return;
            }

            Cancel();
        }
    }

    public interface IJob
    {
        IEnumerable<Task> AvailableTasks();
        void ClaimTask(Minion minion, Task task);
        void AbandonTask(Minion minion, Task task);

        // Returns a follow up task or null if none
        Task CompleteTask(Minion minion, Task task);
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