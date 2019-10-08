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

        public bool SelectionOnly() => false;
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

        public bool ApplicableToAgent(Agent agent)
        {
            // TODO: support agents (i.e cancel hunt)
            return false;
        }

        public bool ApplicableToItem(TileItem item)
        {
            // TODO: support items
            return false;
        }

        public bool ApplicableToBuilding(IBuilding building)
            => building.jobHandles.Count > 0;


        public IEnumerable<IOrdersGiver> GetOrders() { yield return this; }
        public IEnumerable<ICommandsGiver> GetCommands() { yield break; }
        public IEnumerable<Work> QueryWork() { yield break; }
        public void Update(float dt) { }
    }
}
