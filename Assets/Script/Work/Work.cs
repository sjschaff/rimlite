using System.Collections.Generic;
using System;

using Vec2I = UnityEngine.Vector2Int;

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

        public Agent agent { get; private set; }
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
                activeTask.EndTask(true);
            job.AbandonWork(this);
        }

        public void Cancel()
        {
            BB.Assert(this.agent != null);
            ClearClaims();
            agent.AbandonWork();
        }

        public void MakeClaim(IClaim claim) => claims.Add(claim);

        public void Unclaim<T>(TaskClaim<T> task) where T : IClaim
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
        // TODO: move to own file
        public interface IClaim
        {
            void Unclaim();
        }

        // TODO: dont store item publicaly, this will serve as ItemHandle
        public class ItemClaim : IClaim
        {
            private readonly Item item; // TODO: meh

            public Vec2I pos => item.tile.pos;
            public readonly int amt;

            public ItemClaim(Item item, int amt)
            {
                BB.AssertNotNull(item);
                BB.Assert(amt <= item.amtAvailable);

                this.item = item;
                this.amt = amt;
                item.Claim(amt);
            }

            public Item ResolveClaim(Game game, Work work)
            {
                work.Unclaim(this);
                return game.ResolveClaim(item, amt);
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