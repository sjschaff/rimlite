using System.Collections.Generic;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public abstract class JobHandle
    {
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
        bool SelectionOnly();
        void AddOrder(Agent agent);
        void AddOrder(TileItem item);
        void AddOrder(IBuilding building);
        bool ApplicableToAgent(Agent agent);
        bool ApplicableToItem(TileItem item);
        bool ApplicableToBuilding(IBuilding building);
    }

    public interface ICommandsGiver : IToolbarButton
    {
        bool ApplicableToMinion(Minion minion);
        void IssueCommand(Minion minion);
    }

    public interface IContextCommand
    {
        void IssueCommand();
        bool Enabled();
        string GuiText();
    }

    public interface IContextMenuProvider
    {
        IEnumerable<IContextCommand> CommandsForTarget(Vec2I pos, Selection sel, List<Minion> minions);
    }
}