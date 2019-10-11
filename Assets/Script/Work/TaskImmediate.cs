using System;

namespace BB
{
    public abstract class TaskImmediate : Task
    {
        public TaskImmediate(Game game, string description)
            : base(game, description) { }
        public override Status OnPerformTask(float deltaTime)
            => throw new NotSupportedException("Called PerformTask on TaskImmediate");
        protected override void OnEndTask(bool canceled) { }
    }

    public class TaskLambda : TaskImmediate
    {
        private readonly Func<Work, bool> fn;

        public TaskLambda(Game game, string description, Func<Work, bool> fn)
            : base(game, "TaskLambda:" + description)
        {
            BB.AssertNotNull(fn);
            this.fn = fn;
        }

        protected override Status OnBeginTask()
            => fn(work) ? Status.Complete : Status.Fail;
    }
}