namespace BB
{
    public abstract class Task
    {
        public enum Status { Continue, Complete, Fail }


        public readonly GameController game;
        public Work work { get; private set; }
        public Minion minion => work.minion;

        public Task(GameController game) => this.game = game;

        public Status BeginTask(Work work)
        {
            BB.AssertNull(this.work);
            BB.AssertNotNull(work);
            BB.AssertNotNull(work.minion);
            this.work = work;
            return OnBeginTask();
        }

        public void EndTask(bool canceled) => OnEndTask(canceled);


        protected abstract Status OnBeginTask();
        protected abstract void OnEndTask(bool canceled);
        public abstract Status PerformTask(float deltaTime);
    }
}