using System.Collections.Generic;
using System;

namespace BB
{
    public class JobTransient : JobHandle
    {
        private static readonly JobTransient job = new JobTransient();
        public override void AbandonWork(Work work) { }
        public override void CancelJob() { }
        public override IEnumerable<WorkDesc> AvailableWorks() { yield break; }
        public override void ReassignWork(WorkDesc desc, Minion minion) =>
            throw new NotSupportedException();
        public static bool AssignWork(Agent agent, string D_workName, Task task)
            => AssignWork(agent, D_workName, task.Enumerate());
        public static bool AssignWork(Agent agent, string D_workName, IEnumerable<Task> tasks)
            => agent.AssignWork(new Work(job, tasks, D_workName));
        public static void AssignIdleWork(Minion minion, string D_workName, IEnumerable<Task> tasks)
            => minion.AssignIdleWork(new Work(job, tasks, D_workName));
    }
}