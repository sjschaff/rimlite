using System.Collections.Generic;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public struct WorkDesc
    {
        // TODO: work type info for checking if minions are capable
        // also target pos hint to prevent assigning unreachable jobs
        // TODO: share logic for description of things like
        // cant do (not assigned to <X> taks)
        // (reserved by <x>)
        public readonly JobHandle job;
        public readonly string description;
        public readonly string disabledReason;
        public readonly Minion currentAssignee;
        public readonly object workData;
        public bool disabled => disabledReason != null;

        public WorkDesc(
            JobHandle job,
            string description,
            string disabledReason,
            Minion currentAssignee,
            object data)
        {
            this.job = job;
            this.description = description;
            this.disabledReason = disabledReason;
            this.currentAssignee = currentAssignee;
            this.workData = data;
        }
    }
    public abstract class JobHandle
    {
        public abstract void CancelJob();
        public abstract void AbandonWork(Work work);
        public abstract IEnumerable<WorkDesc> AvailableWorks();
        public abstract void ReassignWork(WorkDesc desc, Minion minion);

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