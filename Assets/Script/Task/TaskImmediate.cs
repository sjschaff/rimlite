using System;

namespace BB
{
    public abstract class TaskImmediate : Task2
    {
        public TaskImmediate(GameController game) : base(game) { }
        public override Status PerformTask(float deltaTime)
            => throw new NotSupportedException("Called PerformTask on TaskImmediate");
        protected override void OnEndTask(bool canceled) { }
    }

    public class TaskLambda : TaskImmediate
    {
        private readonly Func<Work, bool> fn;

        public TaskLambda(GameController game, Func<Work, bool> fn)
            : base(game)
        {
            BB.AssertNotNull(fn);
            this.fn = fn;
        }

        protected override Status OnBeginTask()
            => fn(work) ? Status.Complete : Status.Fail;
    }
}