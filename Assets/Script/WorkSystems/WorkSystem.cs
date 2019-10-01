using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class JobHandle
    {
        public readonly IWorkSystem system;
        public readonly Vec2I pos;
        public Work activeWork;

        public JobHandle(IWorkSystem system, Vec2I pos)
        {
            BB.Assert(system != null);
            this.system = system;
            this.pos = pos;
        }

        public virtual void CancelJob() => system.CancelJob(this);
        public virtual void AbandonWork() => system.WorkAbandoned(this);

        public virtual void Destroy()
        {
            if (activeWork != null)
                activeWork.Cancel();
        }
    }

    // example: s
    public interface IWorkSystem
    {
        // TODO: some way to track buildings added/removed
        IOrdersGiver orders { get; }

        IEnumerable<Work> QueryWork();

        void CancelJob(JobHandle job);
        void WorkAbandoned(JobHandle job);
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
