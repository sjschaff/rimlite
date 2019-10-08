using System.Collections.Generic;
using System;

namespace BB
{
    public abstract class JobHandle
    {
        public readonly IGameSystem system;

        public JobHandle(IGameSystem system)
        {
            BB.Assert(system != null);
            this.system = system;
        }

        public abstract void CancelJob();
        public abstract void AbandonWork(Work work);

        // Utility for task generation
        protected TTask Capture<TTask>(TTask task, out TTask outTask)
        {
            outTask = task;
            return task;
        }
    }

    public interface IGameSystem
    {
        /*IGameSystem(Game game);*/
        IEnumerable<IOrdersGiver> GetOrders();
        IEnumerable<ICommandsGiver> GetCommands(); 
        IEnumerable<Work> QueryWork();
        void Update(float dt);
    }

    public interface IToolbarButton
    {
        SpriteDef GuiSprite();
        string GuiText();
    }

    public interface IOrdersGiver : IToolbarButton
    {
        bool HasOrder(Tile tile);
        void AddOrder(Tile tile);
        bool SelectionOnly();
        bool ApplicableToAgent(Agent agent);
        bool ApplicableToItem(TileItem item);
        bool ApplicableToBuilding(IBuilding building);
    }

    public interface ICommandsGiver : IToolbarButton
    {
        bool ApplicableToMinion(Minion agent);
        bool IssueCommand(Agent agent);
    }
}