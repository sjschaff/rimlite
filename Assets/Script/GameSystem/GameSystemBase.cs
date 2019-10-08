using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BB
{
    public abstract class JobStandard<TSystem, TThis> : JobHandle
        where TSystem :  GameSystemStandard<TSystem, TThis>
        where TThis : JobStandard<TSystem, TThis>
    {
        public readonly TSystem systemTyped;
        public readonly Tile tile;
        public Game game => systemTyped.game;

        public JobStandard(TSystem system, Tile tile)
            : base(system)
        {
            BB.AssertNotNull(system);
            BB.AssertNotNull(tile);
            this.systemTyped = system;
            this.tile = tile;
        }

        public abstract IEnumerable<Work> QueryWork();

        public override void CancelJob()
            => systemTyped.CancelJob(this);

        public virtual void Destroy() { }
    }

    public abstract class GameSystemStandard<TThis, TJob> : IGameSystem
        where TJob : JobStandard<TThis, TJob>
        where TThis : GameSystemStandard<TThis, TJob>
    {

        public readonly Game game;
        private readonly Dictionary<Tile, TJob> jobs
            = new Dictionary<Tile, TJob>();

        protected GameSystemStandard(Game game) => this.game = game;

        public abstract IOrdersGiver orders { get; }

        public IEnumerable<Work> QueryWork()
        {
            foreach (var job in jobs.Values)
                foreach (var work in job.QueryWork())
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

        public void CancelJob(JobStandard<TThis, TJob> job)
        {
            RemoveJob((TJob)job);
        }
    }

    public abstract class JobBasic<TSystem, TJob> : JobStandard<TSystem, TJob>
        where TSystem : GameSystemStandard<TSystem, TJob>
        where TJob : JobBasic<TSystem, TJob>
    {
        public Work activeWork;

        public JobBasic(TSystem system, Tile tile) : base(system, tile) { }

        public abstract IEnumerable<Task> GetTasks();

        public override void AbandonWork(Work work)
        {
            BB.Assert(activeWork == work);
            activeWork = null;
        }

        public override void Destroy()
        {
            activeWork?.Cancel();
            base.Destroy();
        }

        public override IEnumerable<Work> QueryWork()
        {
            if (activeWork == null)
                yield return new Work(this, GetTasks()
                    .Prepend(new TaskLambda(game, "sys base init",
                        (work) =>
                        {
                            if (activeWork != null)
                                return false;

                            activeWork = work;
                            return true;
                        }))
                    .Append(new TaskLambda(game, "sys base end",
                        (work) =>
                        {
                            activeWork = null;
                            systemTyped.CancelJob(this);
                            return true;
                        })),
                    typeof(TSystem).Name + ":Work");
        }
    }

    public abstract class GameSystemAsOrders<TThis, TJob> 
        : GameSystemStandard<TThis, TJob>, IOrdersGiver
        where TThis : GameSystemAsOrders<TThis, TJob>
        where TJob : JobBasic<TThis, TJob>
    {
        public abstract class JobHandleOrders : JobBasic<TThis, TJob>
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
        public virtual bool ApplicableToItem(TileItem item) => false;
        public bool HasOrder(Tile tile) => HasJob(tile);
        public void AddOrder(Tile tile) => AddJob(CreateJob(tile));
    }
}