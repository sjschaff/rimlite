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
        private readonly DeferredSet<IAgentListener> agentListeners
            = new DeferredSet<IAgentListener>();

        public void RegisterAgentListener(IAgentListener listener)
            => agentListeners.Add(listener);
        public void UnregisterAgentListener(IAgentListener listener)
            => agentListeners.Remove(listener);
        private void NotifyAgentAdded(Agent agent)
            => agentListeners.ForEach((l) => l.AgentAdded(agent));
        private void NotifyAgentChanged(Agent agent)
            => agentListeners.ForEach((l) => l.AgentChanged(agent));
        private void NotifyAgentRemoved(Agent agent)
            => agentListeners.ForEach((l) => l.AgentRemoved(agent));


        private readonly DeferredSet<IItemListener> itemListeners
            = new DeferredSet<IItemListener>();
        public void RegisterItemListener(IItemListener listener)
            => itemListeners.Add(listener);
        public void UnregisterItemListener(IItemListener listener)
            => itemListeners.Remove(listener);
        private void NotifyItemAdded(TileItem item)
            => itemListeners.ForEach((l) => l.ItemAdded(item));
        private void NotifyItemChanged(TileItem item)
            => itemListeners.ForEach((l) => l.ItemChanged(item));
        private void NotifyItemRemoved(TileItem item)
            => itemListeners.ForEach((l) => l.ItemRemoved(item));


        private readonly DeferredSet<IBuildingListener> buildingListeners
            = new DeferredSet<IBuildingListener>();
        public void RegisterBuildingListener(IBuildingListener listener)
            => buildingListeners.Add(listener);
        public void UnregisterBuildingListener(IBuildingListener listener)
            => buildingListeners.Remove(listener);
        private void NotifyBuildingAdded(IBuilding building)
            => buildingListeners.ForEach((l) => l.BuildingAdded(building));
        private void NotifyBuildingRemoved(IBuilding building)
            => buildingListeners.ForEach((l) => l.BuildingRemoved(building));
    }
}