using System.Collections.Generic;
using System;

namespace BB
{
    public class Work
    {
#if DEBUG
        private static int D_nextID = 0;
        public readonly int D_uniqueID;
#endif

        private readonly JobHandle job;
        private readonly HashSet<IClaim> claims;
        private readonly IEnumerator<Task> tasks;
        public Task activeTask { get; private set; }

        public Minion minion { get; private set; }

        #region Public API
        public Work(JobHandle job, IEnumerable<Task> tasks)
        {
#if DEBUG
            D_uniqueID = D_nextID;
            ++D_nextID;
#endif
            BB.AssertNotNull(job);
            BB.AssertNotNull(tasks);

            this.job = job;
            this.claims = new HashSet<IClaim>();
            this.tasks = tasks.GetEnumerator();
        }

        public Work(JobHandle job, params Task[] tasks)
            : this(job, (IEnumerable<Task>)tasks) { }

        public bool ClaimWork(Minion minion)
        {
            BB.AssertNull(this.minion);
            BB.AssertNull(activeTask);
            this.minion = minion;

            return MoveToNextTask();
        }

        public void Abandon(Minion minion)
        {
            BB.AssertNotNull(this.minion);
            BB.Assert(this.minion == minion);
            if (activeTask != null)
                activeTask.EndTask(true);
            job.AbandonWork(this);
        }

        public void Cancel()
        {
            BB.Assert(this.minion != null);
            ClearClaims();
            minion.AbandonWork();
        }
        public void MakeClaim(IClaim claim) => claims.Add(claim);

        public void Unclaim(TaskClaim task) => Unclaim(task.claim);

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
            minion.RemoveWork(this);
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
                    activeTask.EndTask(false);
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

            minion.skin.SetTool(Tool.None);
            minion.skin.SetAnimLoop(MinionAnim.None);

            if (status == Task.Status.Complete)
            {
                activeTask.EndTask(false);
                MoveToNextTask();
            }
            else
                Cancel();
        }

        #endregion

        #region Claims
        public interface IClaim
        {
            void Unclaim();
        }

        public class ItemClaim : IClaim
        {
            private readonly Item item;
            private readonly int amt;

            public ItemClaim(Item item, int amt)
            {
                BB.AssertNotNull(item);
                BB.Assert(amt <= item.amtAvailable);

                this.item = item;
                this.amt = amt;
                item.Claim(amt);
            }

            public void Unclaim()
            {
                item.Unclaim(amt);
            }
        }

        public class ClaimLambda : IClaim
        {
            private readonly Action unclaimFn;
            public ClaimLambda(Action unclaimFn) => this.unclaimFn = unclaimFn;
            public void Unclaim() => unclaimFn();
        }

        #endregion
    }
}