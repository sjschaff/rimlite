using UnityEngine;

namespace BB
{
    public abstract class Task
    {
        public enum Status { Continue, Complete, Fail }


        public readonly GameController game;
        public Work work { get; private set; }
        public Agent agent => work.agent;

        public Task(GameController game) => this.game = game;

        public Status BeginTask(Work work)
        {
            BB.AssertNull(this.work);
            BB.AssertNotNull(work);
            BB.AssertNotNull(work.agent);
            this.work = work;
            return OnBeginTask();
        }

        public void EndTask(bool canceled) => OnEndTask(canceled);
        public virtual void Reroute(RectInt rect) { }
        protected abstract Status OnBeginTask();
        protected abstract void OnEndTask(bool canceled);
        public abstract Status PerformTask(float deltaTime);
    }
}