using System.Collections.Generic;
using System.Linq;
using System;

namespace BB
{
    class SystemCancel : IGameSystem, IOrdersGiver
    {
        private readonly SpriteDef sprite;

        public SystemCancel(Game game)
            => sprite = game.defs.Get<SpriteDef>("BB:CancelIcon");

        public OrdersFlags flags =>
            OrdersFlags.AppliesItem |
            OrdersFlags.AppliesBuilding |
            OrdersFlags.AppliesGlobally;

        public SpriteDef Sprite() => sprite;

        public bool HasOrder(Tile tile) => false;

        public void AddOrder(Tile tile)
        {
            // TODO: support items
            var jobs = tile.building.jobHandles.ToList();
            foreach (var job in jobs)
                job.CancelJob();
        }

        public bool ApplicableToItem(Item item)
        {
            // TODO: support items
            throw new NotImplementedException();
        }

        public bool ApplicableToBuilding(IBuilding building)
            => building.jobHandles.Count > 0;


        public IOrdersGiver orders => this;
        public IEnumerable<Work> QueryWork() { yield break; }
        public void CancelJob(JobHandle job)
            => throw new NotSupportedException();
        public void WorkAbandoned(JobHandle job, Work work)
            => throw new NotSupportedException();
    }
}
