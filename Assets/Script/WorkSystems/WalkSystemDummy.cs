using System.Collections.Generic;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    [AttributeDontInstantiate]
    public class WalkSystemDummy : IWorkSystem
    {
        private static readonly WalkSystemDummy system = new WalkSystemDummy();
        private static readonly JobHandle dummy = new JobHandle(system, new Vec2I(-1, -1));

        public IOrdersGiver orders => null;
        public void CancelJob(JobHandle job) { }
        public IEnumerable<Work> QueryWork() { yield break; }
        public void WorkAbandoned(JobHandle job) { }

        public static Work Create(Minion.TaskGoTo task)
            => new Work(dummy, task);
    }
}