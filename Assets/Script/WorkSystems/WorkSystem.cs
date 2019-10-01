using System.Collections.Generic;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class WorkHandle
    {
        public readonly IOrdersGiver orders;
        public readonly Vec2I pos;
        public readonly Transform overlay;
        public Job2 activeJob;

        public WorkHandle(IOrdersGiver orders, Vec2I pos)
        {
            BB.Assert(orders != null);
            this.orders = orders;
            this.pos = pos;
            this.overlay = orders.CreateOverlay(pos);
        }

        public void Cancel() => orders.CancelOrder(this);
        public void Destroy() => overlay.Destroy();
    }

    // example: s
    public interface IWorkSystem
    {
        // TODO: some way to track buildings added/removed
        IOrdersGiver orders { get; }

        IEnumerable<Job2> QueryJobs();
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
        void CancelOrder(WorkHandle work);
        Transform CreateOverlay(Vec2I pos);

        OrdersFlags flags { get; }
        bool ApplicableToItem(Item item);
        bool ApplicableToBuilding(IBuilding building);
    }

    public abstract class OrdersBase<TWork> : IOrdersGiver where TWork : WorkHandle
    {
        protected readonly GameController game;
        protected readonly Dictionary<Vec2I, TWork> works
            = new Dictionary<Vec2I, TWork>();

        protected OrdersBase(GameController game) => this.game = game;

        protected abstract SpriteDef sprite { get; }
        protected abstract TWork CreateWork(Vec2I pos);
        public abstract OrdersFlags flags { get; }

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


        public bool HasOrder(Vec2I pos) => works.ContainsKey(pos);

        public void AddOrder(Vec2I pos)
        {
            BB.Assert(!HasOrder(pos));
            var work = CreateWork(pos);
            works.Add(pos, work);
        }

        protected void RemoveOrder(TWork work)
        {
            BB.Assert(work.orders == this);
            BB.Assert(works.TryGetValue(work.pos, out var workContained) && workContained == work);

            works.Remove(work.pos);
            work.Destroy();
        }

        public void CancelOrder(WorkHandle handle)
        {
            TWork work = (TWork)handle;
            BB.Assert(work.orders == this);
            BB.Assert(HasOrder(work.pos));
            BB.Assert(works[work.pos] == work);

            if (work.activeJob != null)
                work.activeJob.Cancel();

            RemoveOrder(work);
        }

        public Transform CreateOverlay(Vec2I pos)
            => game.assets.CreateJobOverlay(game.transform, pos, sprite).transform;
    }

    public abstract class WorkSystemAsOrders<TWork> : OrdersBase<TWork>, IWorkSystem
        where TWork : WorkHandle
    {
        protected abstract Job2 JobForWork(TWork work);

        public WorkSystemAsOrders(GameController game) : base(game) { }

        public IOrdersGiver orders => this;

        public IEnumerable<Job2> QueryJobs()
        {
            foreach (var work in works.Values)
            {
                if (work.activeJob == null)
                    yield return JobForWork(work);
            }
        }
    }
}
