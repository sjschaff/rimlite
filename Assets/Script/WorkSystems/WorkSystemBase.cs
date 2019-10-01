using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public abstract class WorkSystemStandard<TThis, TJob> : IWorkSystem
        where TJob : WorkSystemStandard<TThis, TJob>.JobStandard
        where TThis : WorkSystemStandard<TThis, TJob>
    {
        public abstract class JobStandard : JobHandle
        {
            public readonly TThis systemTyped;
            public readonly Vec2I pos;
            public GameController game => systemTyped.game;

            public JobStandard(TThis system, Vec2I pos)
                : base(system)
            {
                this.pos = pos;
                this.systemTyped = system;
            }

            public virtual void Destroy() { }
        }

        public readonly GameController game;
        private readonly Dictionary<Vec2I, TJob> jobs
            = new Dictionary<Vec2I, TJob>();

        protected WorkSystemStandard(GameController game) => this.game = game;

        public abstract IOrdersGiver orders { get; }
        public abstract void WorkAbandoned(JobHandle job, Work work);
        protected abstract IEnumerable<Work> QueryWorkForJob(TJob job);

        public IEnumerable<Work> QueryWork()
            => jobs.Values.SelectMany(job => QueryWorkForJob(job));

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
    }

    public abstract class WorkSystemBasic<TThis, TJob> : WorkSystemStandard<TThis, TJob>
        where TJob : WorkSystemBasic<TThis, TJob>.JobBasic
        where TThis : WorkSystemAsOrders<TThis, TJob>
    {
        public abstract class JobBasic : JobStandard
        {
            public Work activeWork;

            public JobBasic(TThis system, Vec2I pos) : base(system, pos) { }

            public abstract IEnumerable<Task2> GetTasks();

            public override void Destroy()
            {
                if (activeWork != null)
                    activeWork.Cancel();

                base.Destroy();
            }
        }

        protected WorkSystemBasic(GameController game) : base(game) { }

        protected override IEnumerable<Work> QueryWorkForJob(TJob job)
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

        public override void WorkAbandoned(JobHandle handle, Work work)
        {
            TJob job = (TJob)handle;
            BB.Assert(job.system == this);
            BB.Assert(job.activeWork == work);
            job.activeWork = null;
        }
    }

    public abstract class WorkSystemAsOrders<TThis, TJob> : WorkSystemBasic<TThis, TJob>, IOrdersGiver
        where TJob : WorkSystemBasic<TThis, TJob>.JobBasic
        where TThis : WorkSystemAsOrders<TThis, TJob>
    {
        public abstract class JobHandleOrders : JobBasic
        {
            public readonly Transform overlay;

            public JobHandleOrders(TThis orders, Vec2I pos)
                : base(orders, pos) => overlay = orders.CreateOverlay(pos);

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
