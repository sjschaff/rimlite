using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public abstract class TaskTimed : Task
    {
        private float workAmt;
        private readonly Func<Vec2I, Vec2I> faceFn;
        private readonly MinionAnim anim;
        private readonly Tool tool;

        protected abstract float WorkSpeed();
        protected virtual void OnWorkUpdated(float workAmt) { }

        public static Func<Vec2I, Vec2I> FaceSame() => (pt => pt);
        public static Func<Vec2I, Vec2I> FacePt(Vec2I ptTarget) => (pt => ptTarget);
        public static Func<Vec2I, Vec2I> FaceArea(RectInt rect) => (pt => rect.ClosestPt(pt));

        public TaskTimed(Game game,
            MinionAnim anim, Tool tool,
            float workAmt, Func<Vec2I, Vec2I> faceFn)
            : base(game)
        {
            this.faceFn = faceFn;
            this.anim = anim;
            this.tool = tool;
            this.workAmt = workAmt;
        }

        protected override Status OnBeginTask()
        {
            // TODO: make loading bar
            agent.SetTool(tool);
            agent.SetAnim(anim);
            Vec2I workTarget = faceFn(agent.pos);
            if (agent.pos != workTarget)
                agent.SetFacing(workTarget - agent.pos);

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
            Game game, MinionAnim anim,
            Tool tool, float workAmt,
            Func<Vec2I, Vec2I> faceFn,
            Func<Work, float> workSpeedFn,
            Action<Work, float> workFn,
            Action<Work> completeFn)
            : base(game, anim, tool, workAmt, faceFn)
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