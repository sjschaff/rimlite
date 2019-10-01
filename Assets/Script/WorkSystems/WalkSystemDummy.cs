using System.Collections.Generic;
using System;

namespace BB
{
    [AttributeDontInstantiate]
    public class WalkSystemDummy : IWorkSystem
    {
        private static readonly WalkSystemDummy system = new WalkSystemDummy();
        private static readonly JobHandle dummy = new JobHandle(system);

        public IOrdersGiver orders => null;
        public void CancelJob(JobHandle job)
            => throw new NotSupportedException();
        public IEnumerable<Work> QueryWork() { yield break; }
        public void WorkAbandoned(JobHandle job, Work work) { }

        public static Work Create(Minion.TaskGoTo task)
            => new Work(dummy, task);
    }
}