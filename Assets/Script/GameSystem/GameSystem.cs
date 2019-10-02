using System.Collections.Generic;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class JobHandle
    {
        public readonly IWorkSystem system;

        public JobHandle(IWorkSystem system)
        {
            BB.Assert(system != null);
            this.system = system;
        }

        public virtual void CancelJob() => system.CancelJob(this);
        public virtual void AbandonWork(Work work) => system.WorkAbandoned(this, work);

        // Utility for task generation
        protected TTask Capture<TTask>(TTask task, out TTask outTask)
        {
            outTask = task;
            return task;
        }
    }

    public interface IWorkSystem
    {
        // TODO: some way to track buildings added/removed
        IOrdersGiver orders { get; }
        IEnumerable<Work> QueryWork();
        void CancelJob(JobHandle job);
        void WorkAbandoned(JobHandle job, Work work);
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
        Transform CreateOverlay(Vec2I pos);

        OrdersFlags flags { get; }
        bool ApplicableToItem(Item item);
        bool ApplicableToBuilding(IBuilding building);
    }
}