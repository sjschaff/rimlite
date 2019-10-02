using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public abstract class TaskTimed : Task
    {
        private float workAmt;
        private readonly Vec2I workTarget;
        private readonly MinionAnim anim;
        private readonly Tool tool;

        protected abstract float WorkSpeed();
        protected virtual void OnWorkUpdated(float workAmt) { }

        public TaskTimed(GameController game, Vec2I workTarget,
            MinionAnim anim, Tool tool, float workAmt)
            : base(game)
        {
            this.workTarget = workTarget;
            this.anim = anim;
            this.tool = tool;
            this.workAmt = workAmt;
        }

        protected override Status OnBeginTask()
        {
            // TODO: make loading bar
            minion.skin.SetTool(tool);
            minion.skin.SetAnimLoop(anim);
            if (minion.pos != workTarget)
                minion.SetFacing(workTarget - minion.pos);

            return Status.Continue;
        }

        public override Status PerformTask(float deltaTime)
        {
            workAmt = Mathf.Max(workAmt - deltaTime * WorkSpeed(), 0);
            OnWorkUpdated(workAmt);

            if (workAmt <= 0)
                return Status.Complete;
            else
                return Status.Continue;
        }
    }

    public class TaskTimedLambda : TaskTimed
    {
        private readonly Func<Work, float> workSpeedFn;
        private readonly Action<Work, float> workFn;
        private readonly Action<Work> completeFn;

        public TaskTimedLambda(
            GameController game, Vec2I workTarget,
            MinionAnim anim, Tool tool, float workAmt,
            Func<Work, float> workSpeedFn,
            Action<Work, float> workFn,
            Action<Work> completeFn)
            : base(game, workTarget, anim, tool, workAmt)
        {
            BB.AssertNotNull(workSpeedFn);
            this.workSpeedFn = workSpeedFn;
            this.workFn = workFn;
            this.completeFn = completeFn;
        }

        protected override float WorkSpeed() => workSpeedFn(work);
        protected override void OnWorkUpdated(float workAmt)
            => workFn?.Invoke(work, workAmt);

        protected override void OnEndTask(bool canceled)
        {
            if (!canceled)
                completeFn?.Invoke(work);
        }
    }
}