using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    // Wrapper for aesthetics
    public class TaskGoTo : Agent.InternalTaskGoTo 
    {
        public TaskGoTo(Game game, PathCfg cfg)
            : base(game, cfg) { }
    }

    public partial class Agent
    {
        public abstract class InternalTaskGoTo : Task
        {
            private readonly PathCfg cfg;

            private bool onFallbackPath;
            private LinkedList<Vec2I> path;
            private Line pathVis;

            protected InternalTaskGoTo(Game game, PathCfg cfg)
                : base(game)
            {
                this.cfg = cfg;
                this.onFallbackPath = false;
            }

            private bool GetPath()
            {
                var pts = game.GetPath(agent.realPos.Floor(), cfg);
                if (pts == null)
                    return false;

                path = new LinkedList<Vec2I>(pts);
                var dir = pts[0] - agent.realPos;
                if (pts.Length > 1 && (dir.magnitude < float.Epsilon || Vec2.Dot(dir, pts[1] - pts[0]) < -float.Epsilon))
                    path.RemoveFirst();

                return true;
            }

            private void UpdatePathVis()
            {
                BB.AssertNotNull(pathVis);

                Vec2 ofs = new Vec2(.5f, .5f);
                Vec2[] pts = new Vec2[path.Count + 1];
                pts[0] = agent.realPos + ofs;
                var n = path.First;
                for (int i = 1; i < pts.Length; ++i)
                {
                    pts[i] = n.Value + ofs;
                    n = n.Next;
                }

                pathVis.SetPts(pts);
            }

            protected override Status OnBeginTask()
            {
                if (agent.GridAligned() && cfg.destinationFn(agent.pos))
                    return Status.Complete;

                if (!GetPath())
                    return Status.Fail;

                pathVis = game.assets.CreateLine(
                    game.transform, Vec2.zero, "MinionPath",
                    RenderLayer.Default.Layer(1000),
                    new Color(.2f, .2f, .2f, .5f),
                    1 / 32f, false, true, null);
                UpdatePathVis();

                agent.SetAnim(MinionAnim.Walk);
                return Status.Continue;
            }

            protected override void OnEndTask(bool canceled)
                => pathVis?.Destroy();

            public override void Reroute(RectInt rect)
            {
                if (path == null)
                    return;

                bool intersectsPath = false;
                foreach (var pos in path)
                {
                    if (rect.Contains(pos))
                        intersectsPath = true;
                }

                if (!intersectsPath)
                    return;

                if (!GetPath())
                {
                    path = new LinkedList<Vec2I>();
                    path.AddLast(agent.realPos.Floor());
                    onFallbackPath = true;
                }

                UpdatePathVis();
            }

            public override Status PerformTask(float deltaTime)
            {
                float distance = deltaTime * agent.speed;
                while (true)
                {
                    Vec2 dir = path.First.Value - agent.realPos;
                    if (dir.magnitude > distance)
                    {
                        agent.realPos += dir.normalized * distance;

                        agent.SetFacing(path.First.Value - agent.realPos);
                        UpdatePathVis();
                        return Status.Continue;
                    }
                    else
                    {
                        agent.realPos = path.First.Value;
                        distance -= dir.magnitude;
                        path.RemoveFirst();
                        if (!path.Any())
                        {
                            agent.SetAnim(MinionAnim.None);
                            return onFallbackPath ? Status.Fail : Status.Complete;
                        }
                    }
                }
            }
        }
    }
}