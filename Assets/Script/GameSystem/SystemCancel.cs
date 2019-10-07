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
            OrdersFlags.AppliesGlobally;

        public SpriteDef GuiSprite() => sprite;
        public string GuiText() => "Cancel";

        public bool HasOrder(Tile tile) => false;

        public void AddOrder(Tile tile)
        {
            // TODO: support items
            var jobs = tile.building.jobHandles.ToList();
            foreach (var job in jobs)
                job.CancelJob();
        }

        public bool ApplicableToItem(TileItem item)
        {
            // TODO: support items
            return false;
        }

        public bool ApplicableToBuilding(IBuilding building)
            => building.jobHandles.Count > 0;


        public IOrdersGiver orders => this;
        public IEnumerable<Work> QueryWork() { yield break; }
    }
}
