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
        protected virtual bool OnWorkUpdated(float workAmt) => true;

        public static Func<Vec2I, Vec2I> FaceSame() => (pt => pt);
        public static Func<Vec2I, Vec2I> FacePt(Vec2I ptTarget) => (pt => ptTarget);
        public static Func<Vec2I, Vec2I> FaceArea(RectInt rect) => (pt => rect.ClosestPt(pt));

        public TaskTimed(
            Game game, string description,
            MinionAnim anim, Tool tool,
            float workAmt, Func<Vec2I, Vec2I> faceFn)
            : base(game, description)
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

        public override Status OnPerformTask(float deltaTime)
        {
            workAmt = Mathf.Max(workAmt - deltaTime * WorkSpeed(), 0);
            if (!OnWorkUpdated(workAmt))
                return Status.Fail;

            if (workAmt <= 0)
                return Status.Complete;
            else
                return Status.Continue;
        }

        protected override void OnEndTask(bool canceled)
        {
            agent.SetTool(Tool.None);
            agent.SetAnim(MinionAnim.Idle);
        }
    }

    public class TaskTimedLambda : TaskTimed
    {
        private readonly Func<TaskTimedLambda, float> workSpeedFn;
        private readonly Func<TaskTimedLambda, float, bool> workFn;
        private readonly Action<TaskTimedLambda> completeFn;

        public TaskTimedLambda(
            Game game, string description,
            MinionAnim anim,
            Tool tool, float workAmt,
            Func<Vec2I, Vec2I> faceFn,
            Func<TaskTimedLambda, float> workSpeedFn,
            Func<TaskTimedLambda, float, bool> workFn,
            Action<TaskTimedLambda> completeFn)
            : base(game, description, anim, tool, workAmt, faceFn)
        {
            BB.AssertNotNull(workSpeedFn);
            this.workSpeedFn = workSpeedFn;
            this.workFn = workFn;
            this.completeFn = completeFn;
        }

        protected override float WorkSpeed() => workSpeedFn(this);
        protected override bool OnWorkUpdated(float workAmt)
            => workFn == null ? true : workFn(this, workAmt);

        protected override void OnEndTask(bool canceled)
        {
            base.OnEndTask(canceled);

            if (!canceled)
                completeFn?.Invoke(this);
        }
    }
}