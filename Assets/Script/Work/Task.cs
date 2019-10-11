using UnityEngine;

namespace BB
{
    public abstract class Task
    {
        public enum Status { Continue, Complete, Fail }

        public readonly Game game;
        public Work work { get; private set; }
        public Agent agent => work.agent;
        public readonly string description;
        private bool softCanceled;

        public Task(Game game, string description)
        {
            this.game = game;
            this.description = description;
            this.softCanceled = false;
        }

        public bool SoftCancel() => softCanceled = true;

        public Status BeginTask(Work work)
        {
            BB.AssertNull(this.work);
            BB.AssertNotNull(work);
            BB.AssertNotNull(work.agent);
            this.work = work;
            return OnBeginTask();
        }

        public Status PerformTask(float deltaTime)
        {
            if (softCanceled)
                return Status.Complete;
            else
                return OnPerformTask(deltaTime);
        }

        public void EndTask(bool canceled)
            => OnEndTask(canceled || softCanceled);

        public virtual void Reroute(RectInt rect) { }
        protected abstract Status OnBeginTask();
        protected abstract void OnEndTask(bool canceled);
        public abstract Status OnPerformTask(float deltaTime);
    }
}