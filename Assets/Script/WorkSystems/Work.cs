using System.Collections.Generic;
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

    public class Work
    {
#if DEBUG
        private static int D_nextID = 0;
        public readonly int D_uniqueID;
#endif

        public interface IClaim
        {
            void Unclaim();
        }

        // TODO: make more general
        public class ItemClaim : IClaim
        {
            private readonly Item item;
            private readonly int amt;

            public ItemClaim(Item item, int amt)
            {
                BB.AssertNotNull(item);
                BB.Assert(amt <= item.amtAvailable);

                this.item = item;
                this.amt = amt;
                item.Claim(amt);
            }

            public void Unclaim()
            {
                item.Unclaim(amt);
            }
        }

        public class ClaimLambda : IClaim
        {
            private readonly Action unclaimFn;
            public ClaimLambda(Action unclaimFn) => this.unclaimFn = unclaimFn;
            public void Unclaim() => unclaimFn();
        }

        public void MakeClaim(IClaim claim) => claims.Add(claim);

        public void Unclaim(TaskClaim task) => Unclaim(task.claim);

        public void Unclaim(IClaim claim)
        {
            BB.AssertNotNull(claim);
            BB.Assert(claims.Contains(claim));
            claim.Unclaim();
            claims.Remove(claim);
        }

        private readonly JobHandle job;
        private readonly HashSet<IClaim> claims;
        private readonly IEnumerator<Task2> taskIt;

        public Minion minion { get; private set; }
        private Task2 activeTask;

        public Work(JobHandle job, IEnumerable<Task2> tasks)
        {
#if DEBUG
            D_uniqueID = D_nextID;
            ++D_nextID;
#endif
            BB.AssertNotNull(job);
            BB.AssertNotNull(tasks);

            this.job = job;
            this.claims = new HashSet<IClaim>();
            taskIt = tasks.GetEnumerator();
        }

        public Work(JobHandle job, params Task2[] tasks)
            : this(job, (IEnumerable<Task2>)tasks) { }

        public bool ClaimWork(Minion minion)
        {
            BB.AssertNull(this.minion);
            BB.AssertNull(activeTask);
            this.minion = minion;

            return MoveToNextTask();
        }

        public void Abandon(Minion minion)
        {
            BB.AssertNotNull(this.minion);
            BB.Assert(this.minion == minion);
            if (activeTask != null)
                activeTask.EndTask(true);
            job.AbandonWork(this);
        }

        private void ClearClaims()
        {
            foreach (IClaim claim in claims)
                claim.Unclaim();
        }

        public void Cancel()
        {
            BB.Assert(this.minion != null);
            ClearClaims();
            minion.AbandonWork();
        }

        private void Complete()
        {
            BB.Assert(claims.Count == 0);
            if (claims.Count != 0)
                BB.Log("Task completed with claims left over, this is a bug");
            ClearClaims();
            minion.RemoveWork(this);
        }

        private bool MoveToNextTask()
        {
            var status = IterateTasks();

            if (status == Task2.Status.Continue)
                return true;

            if (status == Task2.Status.Fail)
                Cancel();
            else if (status == Task2.Status.Complete)
                Complete();

            return false;
        }

        private Task2.Status IterateTasks()
        {
            while (taskIt.MoveNext())
            {
                activeTask = taskIt.Current;
                var status = activeTask.BeginTask(this);

                if (status == Task2.Status.Complete)
                    activeTask.EndTask(false);
                else
                    return status;
            }

            activeTask = null; // just in case
            return Task2.Status.Complete;
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
                MoveToNextTask();
            }
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

    public abstract class JobStandardOLD : IJob
    {
        protected GameController game { get; private set; }
        protected Vec2I pos { get; private set; }
        protected ITile tile { get; private set; }

        protected JobStandardOLD(GameController game, Vec2I pos)
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
    public class JobBuild : JobStandardOLD
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
            // building = BuildingProtoConstruction.K_single.Create(this);
            building = null;
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