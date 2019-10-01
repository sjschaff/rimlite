using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class JobHandle
    {
        public readonly IWorkSystem system;
        public readonly Vec2I pos;
        public Work activeWork;

        public JobHandle(IWorkSystem system, Vec2I pos)
        {
            BB.Assert(system != null);
            this.system = system;
            this.pos = pos;
        }

        public virtual void CancelJob() => system.CancelJob(this);
        public virtual void AbandonWork() => system.WorkAbandoned(this);

        public virtual void Destroy()
        {
            if (activeWork != null)
                activeWork.Cancel();
        }
    }

    // example: s
    public interface IWorkSystem
    {
        // TODO: some way to track buildings added/removed
        IOrdersGiver orders { get; }

        IEnumerable<Work> QueryWork();

        void CancelJob(JobHandle job);
        void WorkAbandoned(JobHandle job);
    }

    [Flags]
    public enum OrdersFlags
    {
        None = 0,
        AppliesItem = 1,
        AppliesBuilding = 2,
        AppliesGlobally = 4,
    }

    public interface IOrdersGiver
    {
        bool HasOrder(Vec2I pos);
        void AddOrder(Vec2I pos);
        Transform CreateOverlay(Vec2I pos);

        OrdersFlags flags { get; }
        bool ApplicableToItem(Item item);
        bool ApplicableToBuilding(IBuilding building);
    }

    public abstract class WorkSystemStandard<TThis, TJob> : IWorkSystem
        where TJob : WorkSystemStandard<TThis, TJob>.JobStandard
        where TThis : WorkSystemStandard<TThis, TJob>
    {
        public abstract class JobStandard : JobHandle
        {
            public readonly TThis systemTyped;

            public JobStandard(TThis system, Vec2I pos)
                : base(system, pos)
            {
                this.systemTyped = system;
            }

            public abstract IEnumerable<Task2> GetTasks();
        }

        public readonly GameController game;
        private readonly Dictionary<Vec2I, TJob> jobs
            = new Dictionary<Vec2I, TJob>();

        protected WorkSystemStandard(GameController game) => this.game = game;

        public abstract IOrdersGiver orders { get; }

        public IEnumerable<Work> QueryWork()
        {
            foreach (var job in jobs.Values)
            {
                if (job.activeWork == null)
                    yield return new Work(job, job.GetTasks()
                        .Prepend(new TaskLambda(game,
                            (work) =>
                            {
                                if (job.activeWork != null)
                                    return false;

                                job.activeWork = work;
                                return true;
                            }))
                        .Append(new TaskLambda(game,
                            (work) =>
                            {
                                job.activeWork = null;
                                RemoveJob(job);
                                return true;
                            }))
                        );
            }
        }

        protected bool HasJob(Vec2I pos) => jobs.ContainsKey(pos);

        protected void AddJob(TJob job)
        {
            BB.Assert(!HasJob(job.pos));
            jobs.Add(job.pos, job);
        }

        protected void RemoveJob(TJob job)
        {
            BB.Assert(job.system == this);
            BB.Assert(jobs.TryGetValue(job.pos, out var workContained) && workContained == job);

            job.Destroy();
            jobs.Remove(job.pos);
        }

        public void CancelJob(JobHandle handle)
        {
            TJob job = (TJob)handle;
            BB.Assert(job.system == this);
            RemoveJob(job);
        }

        public void WorkAbandoned(JobHandle handle)
        {
            TJob job = (TJob)handle;
            BB.Assert(job.system == this);
            handle.activeWork = null;
        }
    }

    public abstract class WorkSystemAsOrders<TThis, TJob> : WorkSystemStandard<TThis, TJob>, IOrdersGiver
        where TJob : WorkSystemStandard<TThis, TJob>.JobStandard
        where TThis : WorkSystemAsOrders<TThis, TJob>
    {
        public abstract class JobHandleOrders : JobStandard
        {
            public readonly TThis orders;
            public readonly Transform overlay;

            public JobHandleOrders(TThis orders, Vec2I pos)
                : base(orders, pos)
            {
                this.orders = orders;
                this.overlay = orders.CreateOverlay(pos);
            }

            public override void Destroy()
            {
                overlay.Destroy();
                base.Destroy();
            }
        }

        protected WorkSystemAsOrders(GameController game) : base(game) { }

        protected abstract SpriteDef sprite { get; }
        protected abstract TJob CreateJob(Vec2I pos);
        public abstract OrdersFlags flags { get; }


        public override IOrdersGiver orders => this;

        private bool ApplicableErrorCheck(OrdersFlags flag)
        {
            if (flags.HasFlag(flag))
                throw new NotImplementedException();
            else
                throw new NotSupportedException();
        }

        public virtual bool ApplicableToBuilding(IBuilding building)
            => ApplicableErrorCheck(OrdersFlags.AppliesBuilding);

        public virtual bool ApplicableToItem(Item item)
            => ApplicableErrorCheck(OrdersFlags.AppliesItem);


        public bool HasOrder(Vec2I pos) => HasJob(pos);
        public void AddOrder(Vec2I pos) => AddJob(CreateJob(pos));

        public Transform CreateOverlay(Vec2I pos)
            => game.assets.CreateJobOverlay(game.transform, pos, sprite).transform;
    }
}
