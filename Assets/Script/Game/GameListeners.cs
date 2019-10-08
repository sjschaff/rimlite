namespace BB
{
    public interface IAgentListener
    {
        void AgentAdded(Agent agent);
        void AgentChanged(Agent agent);
        void AgentRemoved(Agent agent);
    }

    public interface IItemListener
    {
        void ItemAdded(TileItem item);
        void ItemChanged(TileItem item);
        void ItemRemoved(TileItem item);
    }

    public interface IBuildingListener
    {
        void BuildingAdded(IBuilding building);
        void BuildingRemoved(IBuilding building);
    }

    public partial class Game
    {
        private readonly ListenerRegistry<IAgentListener> agentListeners
            = new ListenerRegistry<IAgentListener>();

        public void RegisterAgentListener(IAgentListener listener)
            => agentListeners.Register(listener);
        public void UnregisterAgentListener(IAgentListener listener)
            => agentListeners.Unregister(listener);
        private void NotifyAgentAdded(Agent agent)
            => agentListeners.MessageAll((l) => l.AgentAdded(agent));
        private void NotifyAgentChanged(Agent agent)
            => agentListeners.MessageAll((l) => l.AgentChanged(agent));
        private void NotifyAgentRemoved(Agent agent)
            => agentListeners.MessageAll((l) => l.AgentRemoved(agent));


        private readonly ListenerRegistry<IItemListener> itemListeners
            = new ListenerRegistry<IItemListener>();
        public void RegisterItemListener(IItemListener listener)
            => itemListeners.Register(listener);
        public void UnregisterItemListener(IItemListener listener)
            => itemListeners.Unregister(listener);
        private void NotifyItemAdded(TileItem item)
            => itemListeners.MessageAll((l) => l.ItemAdded(item));
        private void NotifyItemChanged(TileItem item)
            => itemListeners.MessageAll((l) => l.ItemChanged(item));
        private void NotifyItemRemoved(TileItem item)
            => itemListeners.MessageAll((l) => l.ItemRemoved(item));


        private readonly ListenerRegistry<IBuildingListener> buildingListeners
            = new ListenerRegistry<IBuildingListener>();
        public void RegisterBuildingListener(IBuildingListener listener)
            => buildingListeners.Register(listener);
        public void UnregisterBuildingListener(IBuildingListener listener)
            => buildingListeners.Unregister(listener);
        private void NotifyBuildingAdded(IBuilding building)
            => buildingListeners.MessageAll((l) => l.BuildingAdded(building));
        private void NotifyBuildingRemoved(IBuilding building)
            => buildingListeners.MessageAll((l) => l.BuildingRemoved(building));
    }
}