using System.Collections.Generic;

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

        public void AddOrder(Agent agent)
        {
            // TODO: support agents
        }

        public void AddOrder(TileItem item)
        {
            // TODO: support items
        }

        public void AddOrder(IBuilding building)
            => building.CancelAllJobs();

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
