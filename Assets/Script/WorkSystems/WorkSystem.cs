using System.Collections.Generic;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class WorkHandle
    {
        public readonly IWorkSystem system;
        public readonly Vec2I pos;
        public Job2 activeJob;

        public WorkHandle(IWorkSystem system, Vec2I pos)
        {
            BB.Assert(system != null);
            this.system = system;
            this.pos = pos;
        }

        public virtual void Cancel() => system.CancelWork(this);
        public virtual void Destroy() { }
    }

    // example: s
    public interface IWorkSystem
    {
        // TODO: some way to track buildings added/removed
        IOrdersGiver orders { get; }

        IEnumerable<Job2> QueryJobs();

        void CancelWork(WorkHandle work);
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

    public abstract class WorkSystemStandard<TWork> : IWorkSystem
        where TWork : WorkHandle
    {
        public readonly GameController game;
        private readonly Dictionary<Vec2I, TWork> works
            = new Dictionary<Vec2I, TWork>();

        protected WorkSystemStandard(GameController game) => this.game = game;

        public abstract IOrdersGiver orders { get; }
        protected abstract Job2 JobForWork(TWork work);

        public IEnumerable<Job2> QueryJobs()
        {
            foreach (var work in works.Values)
            {
                if (work.activeJob == null)
                    yield return JobForWork(work);
            }
        }

        protected bool HasWork(Vec2I pos) => works.ContainsKey(pos);

        protected void AddWork(TWork work)
        {
            BB.Assert(!HasWork(work.pos));
            works.Add(work.pos, work);
        }

        protected void RemoveWork(TWork work)
        {
            BB.Assert(work.system == this);
            BB.Assert(works.TryGetValue(work.pos, out var workContained) && workContained == work);

            if (work.activeJob != null)
                work.activeJob.Cancel();

            works.Remove(work.pos);
            work.Destroy();
        }

        public void CancelWork(WorkHandle handle)
        {
            TWork work = (TWork)handle;
            BB.Assert(work.system == this);
            RemoveWork(work);
        }
    }

    public abstract class WorkSystemAsOrders<TThis, TWork> : WorkSystemStandard<TWork>, IOrdersGiver
        where TWork : WorkHandle
        where TThis : WorkSystemAsOrders<TThis, TWork>
    {
        public class WorkHandleOrders : WorkHandle
        {
            public readonly TThis orders;
            public readonly Transform overlay;

            public WorkHandleOrders(TThis orders, Vec2I pos)
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
        protected abstract TWork CreateWork(Vec2I pos);
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


        public bool HasOrder(Vec2I pos) => HasWork(pos);
        public void AddOrder(Vec2I pos) => AddWork(CreateWork(pos));

        public Transform CreateOverlay(Vec2I pos)
            => game.assets.CreateJobOverlay(game.transform, pos, sprite).transform;
    }
}
