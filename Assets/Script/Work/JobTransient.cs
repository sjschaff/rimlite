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
        public static bool AssignWork(Minion minion, string D_workName, Task task)
            => AssignWork(minion, D_workName, task.Enumerate());
        public static bool AssignWork(Minion minion, string D_workName, IEnumerable<Task> tasks)
            => minion.AssignWork(new Work(job, tasks, D_workName));
    }
}