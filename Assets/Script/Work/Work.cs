using System.Collections.Generic;

namespace BB
{
    public class Work
    {
#if DEBUG
        private static int D_nextID = 0;
        public readonly int D_uniqueID;
        public readonly string D_workName;
        public readonly List<string> D_tasksCompleted
            = new List<string>();
#endif
        [System.Diagnostics.Conditional("DEBUG")]
        private void D_TrackTaskCompleted(Task task)
            => D_tasksCompleted.Add(task.description);

        private readonly JobHandle job;
        private readonly HashSet<IClaim> claims;
        private readonly IEnumerator<Task> tasks;
        public Task activeTask { get; private set; }

        public Agent agent { get; private set; }
        public Minion minion { get; private set; }

        #region Public API
        public Work(JobHandle job, IEnumerable<Task> tasks, string D_workName)
        {
#if DEBUG
            D_uniqueID = D_nextID;
            ++D_nextID;
            this.D_workName = D_workName;
#endif
            BB.AssertNotNull(job);
            BB.AssertNotNull(tasks);

            this.job = job;
            this.claims = new HashSet<IClaim>();
            this.tasks = tasks.GetEnumerator();
        }

        public void ClaimWork(Agent agent)
        {
            BB.AssertNull(this.agent);
            BB.AssertNull(activeTask);
            this.agent = agent;
            if (agent is Minion minion)
                this.minion = minion;

            BB.Assert(MoveToNextTask(), "Work task failed immediately");
        }

        public void Abandon(Agent agent)
        {
            BB.AssertNotNull(this.agent);
            BB.Assert(this.agent == agent);
            if (activeTask != null)
                EndActiveTask(true);
            job.AbandonWork(this);
            ClearClaims();
        }

        public void Cancel()
        {
            BB.Assert(this.agent != null);
            agent.AbandonWork();
        }

        private void EndActiveTask(bool canceled)
        {
            D_TrackTaskCompleted(activeTask);
            activeTask.EndTask(canceled);
        }

        public void MakeClaim(IClaim claim) => claims.Add(claim);

        public void Unclaim<T>(TaskClaimT<T> task) where T : IClaim
            => Unclaim(task.claim);

        public void Unclaim(IClaim claim)
        {
            BB.AssertNotNull(claim);
            BB.Assert(claims.Contains(claim));
            claim.Unclaim();
            claims.Remove(claim);
        }

        #endregion

        #region Implementation
        private void ClearClaims()
        {
            foreach (IClaim claim in claims)
                claim.Unclaim();
        }

        private void Complete()
        {
            BB.Assert(claims.Count == 0);
            if (claims.Count != 0)
                BB.LogError("Task completed with claims left over, this is a bug");
            ClearClaims();
            agent.RemoveWork(this);
        }

        private bool MoveToNextTask()
        {
            var status = IterateTasks();

            if (status == Task.Status.Continue)
                return true;

            if (status == Task.Status.Fail)
                Cancel();
            else if (status == Task.Status.Complete)
                Complete();

            return false;
        }

        private Task.Status IterateTasks()
        {
            while (tasks.MoveNext())
            {
                activeTask = tasks.Current;
                var status = activeTask.BeginTask(this);

                if (status == Task.Status.Complete)
                    EndActiveTask(false);
                else
                    return status;
            }

            activeTask = null; // just in case
            return Task.Status.Complete;
        }

        public void PerformWork(float deltaTime)
        {
            BB.AssertNotNull(activeTask);

            var status = activeTask.PerformTask(deltaTime);
            if (status == Task.Status.Continue)
                return;

            if (status == Task.Status.Complete)
            {
                EndActiveTask(false);
                MoveToNextTask();
            }
            else
                Cancel();
        }

        #endregion
    }
}