using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace BB
{
    public abstract class GameSystemStandard<TThis, TJob> : IGameSystem
        where TJob : GameSystemStandard<TThis, TJob>.JobStandard
        where TThis : GameSystemStandard<TThis, TJob>
    {
        public abstract class JobStandard : JobHandle
        {
            public readonly TThis systemTyped;
            public readonly Tile tile;
            public Game game => systemTyped.game;

            public JobStandard(TThis system, Tile tile)
                : base(system)
            {
                BB.AssertNotNull(system);
                BB.AssertNotNull(tile);
                this.systemTyped = system;
                this.tile = tile;
            }

            public virtual void Destroy() { }
        }

        public readonly Game game;
        private readonly Dictionary<Tile, TJob> jobs
            = new Dictionary<Tile, TJob>();

        protected GameSystemStandard(Game game) => this.game = game;

        public abstract IOrdersGiver orders { get; }
        public abstract void WorkAbandoned(JobHandle job, Work work);
        protected abstract IEnumerable<Work> QueryWorkForJob(TJob job);

        public IEnumerable<Work> QueryWork()
        {
            foreach (var job in jobs.Values)
                foreach (var work in QueryWorkForJob(job))
                    yield return work;
        }

        protected bool HasJob(Tile tile) => jobs.ContainsKey(tile);

        protected void AddJob(TJob job)
        {
            BB.Assert(!HasJob(job.tile));
            jobs.Add(job.tile, job);
        }

        protected void RemoveJob(TJob job)
        {
            BB.Assert(job.system == this);
            BB.Assert(jobs.TryGetValue(job.tile, out var workContained) && workContained == job);

            job.Destroy();
            jobs.Remove(job.tile);
        }

        public void CancelJob(JobHandle handle)
        {
            TJob job = (TJob)handle;
            BB.Assert(job.system == this);
            RemoveJob(job);
        }
    }

    public abstract class GameSystemBasic<TThis, TJob> : GameSystemStandard<TThis, TJob>
        where TJob : GameSystemBasic<TThis, TJob>.JobBasic
        where TThis : GameSystemAsOrders<TThis, TJob>
    {
        public abstract class JobBasic : JobStandard
        {
            public Work activeWork;

            public JobBasic(TThis system, Tile tile) : base(system, tile) { }

            public abstract IEnumerable<Task> GetTasks();

            public override void Destroy()
            {
                activeWork?.Cancel();
                base.Destroy();
            }
        }

        protected GameSystemBasic(Game game) : base(game) { }

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

    public abstract class GameSystemAsOrders<TThis, TJob> : GameSystemBasic<TThis, TJob>, IOrdersGiver
        where TJob : GameSystemBasic<TThis, TJob>.JobBasic
        where TThis : GameSystemAsOrders<TThis, TJob>
    {
        public abstract class JobHandleOrders : JobBasic
        {
            public readonly Transform overlay;

            public JobHandleOrders(TThis orders, Tile tile)
                : base(orders, tile)
                => overlay = game.assets.CreateJobOverlay(
                    game.workOverlays, tile.pos, orders.guiSprite).transform;

            public override void Destroy()
            {
                overlay.Destroy();
                base.Destroy();
            }
        }

        protected readonly SpriteDef guiSprite;
        protected readonly string guiText;

        protected GameSystemAsOrders(Game game, SpriteDef guiSprite, string guiText)
            : base(game)
        {
            this.guiSprite = guiSprite;
            this.guiText = guiText;
        }

        protected abstract TJob CreateJob(Tile tile);
        public abstract OrdersFlags flags { get; }


        public SpriteDef GuiSprite() => guiSprite;
        public string GuiText() => guiText;
        public override IOrdersGiver orders => this;
        public virtual bool ApplicableToBuilding(IBuilding building) => false;
        public virtual bool ApplicableToItem(Item item) => false;
        public bool HasOrder(Tile tile) => HasJob(tile);
        public void AddOrder(Tile tile) => AddJob(CreateJob(tile));
    }
}