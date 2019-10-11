using System;

namespace BB
{
    public abstract class TaskWait : Task
    {
        public TaskWait(Game game, string description)
            : base(game, description) { }

        protected abstract bool DoneWaiting(float dt);

        public sealed override Status PerformTask(float deltaTime)
        {
            if (DoneWaiting(deltaTime))
                return Status.Complete;
            else
                return Status.Continue;
        }

        protected sealed override Status OnBeginTask()
            => Status.Continue;

        protected override void OnEndTask(bool canceled) { }
    }


    public class TaskWaitLambda : TaskWait
    {
        private readonly Func<TaskWaitLambda, float, bool> doneFn;
        private readonly Action<TaskWaitLambda, bool> completeFn;

        public TaskWaitLambda(Game game, string description,
            Func<TaskWaitLambda, float, bool> doneFn,
            Action<TaskWaitLambda, bool> completeFn = null)
            : base(game, description)
        {
            BB.AssertNotNull(doneFn);
            this.doneFn = doneFn;
            this.completeFn = completeFn;
        }

        protected override bool DoneWaiting(float deltaTime)
            => doneFn(this, deltaTime);

        protected override void OnEndTask(bool canceled)
            => completeFn?.Invoke(this, canceled);
    }

    public class TaskWaitDuration : TaskWait
    {
        private readonly float duration;
        private float elapsed;

        public TaskWaitDuration(Game game, string description, float duration)
            : base(game, description)
        {
            this.duration = duration;
            this.elapsed = 0;
        }

        protected override bool DoneWaiting(float dt)
        {
            elapsed += dt;
            return elapsed >= duration;
        }
    }

}
