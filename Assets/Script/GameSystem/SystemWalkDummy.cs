using System.Collections.Generic;
using System;

namespace BB
{
    [AttributeDontInstantiate]
    public class SystemWalkDummy : IGameSystem
    {
        private static readonly SystemWalkDummy system = new SystemWalkDummy();
        private static readonly JobHandle dummy = new JobDummy(system);

        private class JobDummy : JobHandle
        {
            public JobDummy(SystemWalkDummy system) : base(system) { }
            public override void AbandonWork(Work work) { }
            public override void CancelJob() { }
        }

        public IOrdersGiver orders => null;
        public IEnumerable<Work> QueryWork() { yield break; }

        public static Work Create(TaskGoTo task)
            => new Work(dummy, task);
    }
}