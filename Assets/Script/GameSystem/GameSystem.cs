﻿using System.Collections.Generic;
using System;

namespace BB
{
    public class JobHandle
    {
        public readonly IGameSystem system;

        public JobHandle(IGameSystem system)
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

    public interface IGameSystem
    {
        /*IGameSystem(Game game);*/
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
        bool HasOrder(Tile tile);
        void AddOrder(Tile tile);
        SpriteDef Sprite();
        OrdersFlags flags { get; }
        bool ApplicableToItem(Item item);
        bool ApplicableToBuilding(IBuilding building);
    }
}