using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public abstract class JobStandard<TSystem, TKey, TThis> : JobHandle
        where TSystem :  GameSystemStandard<TSystem, TKey, TThis>
        where TThis : JobStandard<TSystem, TKey, TThis>
    {
        public readonly TSystem systemTyped;
        public readonly TKey key;
        public Game game => systemTyped.game;

        public JobStandard(TSystem system, TKey key) : base(system)
        {
            BB.AssertNotNull(system);
            this.systemTyped = system;
            this.key = key;
        }

        public abstract IEnumerable<Work> QueryWork();

        public override void CancelJob()
            => systemTyped.CancelJob((TThis)this);

        public virtual void Destroy() { }
    }

    public abstract class GameSystemStandard<TThis, TKey, TJob> : IGameSystem
        where TJob : JobStandard<TThis, TKey, TJob>
        where TThis : GameSystemStandard<TThis, TKey, TJob>
    {
        public readonly Game game;
        private readonly Dictionary<TKey, TJob> jobs
            = new Dictionary<TKey, TJob>();

        protected GameSystemStandard(Game game) => this.game = game;

        public virtual IEnumerable<IOrdersGiver> GetOrders() { yield break; }
        public virtual IEnumerable<ICommandsGiver> GetCommands() { yield break; }
        public virtual void Update(float dt) { }

        public IEnumerable<Work> QueryWork()
        {
            foreach (var job in jobs.Values)
                foreach (var work in job.QueryWork())
                    yield return work;
        }

        protected bool HasJob(TKey key) => jobs.ContainsKey(key);

        protected void AddJob(TJob job)
        {
            BB.Assert(!HasJob(job.key));
            jobs.Add(job.key, job);
        }

        protected void RemoveJob(TJob job)
        {
            BB.Assert(job.system == this);
            BB.Assert(jobs.TryGetValue(job.key, out var workContained) && workContained == job);

            job.Destroy();
            jobs.Remove(job.key);
        }

        public void CancelJob(TJob job) => RemoveJob(job);
    }

    #region JobBasicKey
    public class JobBasicKey : IEquatable<JobBasicKey>
    {
        public readonly Agent agent;
        public readonly TileItem item;
        public readonly IBuilding building;
        private JobBasicKey(Agent agent, TileItem item, IBuilding building)
        {
            this.building = building;
            this.item = item;
            this.agent = agent;
        }
        public JobBasicKey(Agent agent)
            : this(agent, null, null) { }
        public JobBasicKey(TileItem item)
            : this(null, item, null) { }
        public JobBasicKey(IBuilding building)
            : this(null, null, building) { }
        public static implicit operator JobBasicKey(Agent agent) => new JobBasicKey(agent);
        public static implicit operator JobBasicKey(TileItem item) => new JobBasicKey(item);
        public override bool Equals(object obj) => Equals(obj as JobBasicKey);
        public bool Equals(JobBasicKey other)
        {
            return other != null &&
                   building == other.building &&
                   item == other.item &&
                   agent == other.agent;
        }
        public override int GetHashCode()
        {
            var hashCode = -1282026303;
            hashCode = hashCode * -1521134295 + EqualityComparer<IBuilding>.Default.GetHashCode(building);
            hashCode = hashCode * -1521134295 + EqualityComparer<TileItem>.Default.GetHashCode(item);
            hashCode = hashCode * -1521134295 + EqualityComparer<Agent>.Default.GetHashCode(agent);
            return hashCode;
        }
    }
    #endregion

    public abstract class JobBasic<TSystem, TJob> : JobStandard<TSystem, JobBasicKey, TJob>
        where TSystem : GameSystemStandard<TSystem, JobBasicKey, TJob>
        where TJob : JobBasic<TSystem, TJob>
    {
        public Work activeWork;

        public JobBasic(TSystem system, JobBasicKey key) : base(system, key) { }
        public JobBasic(TSystem system, IBuilding building)
            : base(system, new JobBasicKey(building)) { }

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
                            systemTyped.CancelJob((TJob)this);
                            return true;
                        })),
                    typeof(TSystem).Name + ":Work");
        }
    }

    public abstract class GameSystemAsOrders<TThis, TJob> 
        : GameSystemStandard<TThis, JobBasicKey, TJob>, IOrdersGiver
        where TThis : GameSystemAsOrders<TThis, TJob>
        where TJob : JobBasic<TThis, TJob>
    {
        public abstract class JobHandleOrders : JobBasic<TThis, TJob>
        {
            public readonly Transform overlay;

            public JobHandleOrders(TThis orders, JobBasicKey key)
                : base(orders, key)
            {
                Transform parent = game.workOverlays;
                Vec2I pos;
                if (key.agent != null)
                {
                    parent = key.agent.transform;
                    pos = Vec2I.zero;
                }
                else if (key.item != null)
                    pos = key.item.tile.pos;
                else
                    pos = key.building.tile.pos;
                overlay = game.assets.CreateJobOverlay(parent, pos, orders.guiSprite).transform;
            }

            public JobHandleOrders(TThis orders, IBuilding building)
                : this(orders, new JobBasicKey(building)) { }

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

        public abstract bool SelectionOnly();
        public SpriteDef GuiSprite() => guiSprite;
        public string GuiText() => guiText;
        public override IEnumerable<IOrdersGiver> GetOrders() { yield return this; }
        public override IEnumerable<ICommandsGiver> GetCommands() { yield break; }

        public virtual bool AppliesToAgent(Agent agent) => false;
        public virtual bool AppliesToItem(TileItem item) => false;
        public virtual bool AppliesToBuilding(IBuilding building) => false;
        public bool ApplicableToAgent(Agent agent) =>
            !HasJob(agent) && AppliesToAgent(agent);
        public bool ApplicableToItem(TileItem item) =>
            !HasJob(item) && AppliesToItem(item);
        public bool ApplicableToBuilding(IBuilding building) =>
            !HasJob(new JobBasicKey(building)) && AppliesToBuilding(building);
        protected virtual TJob CreateJob(Agent agent)
            => throw new NotImplementedException();
        protected virtual TJob CreateJob(TileItem item)
            => throw new NotImplementedException();
        protected virtual TJob CreateJob(IBuilding building)
            => throw new NotImplementedException();
        public void AddOrder(Agent agent) => AddJob(CreateJob(agent));
        public void AddOrder(TileItem item) => AddJob(CreateJob(item));
        public void AddOrder(IBuilding building) => AddJob(CreateJob(building));
        public override void Update(float dt) { }
    }
}