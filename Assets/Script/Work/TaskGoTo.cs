using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    // Wrapper for aesthetics
    public class TaskGoTo : Minion.InternalTaskGoTo
    {
        public TaskGoTo(GameController game, PathCfg cfg)
            : base(game, cfg) { }
    }

    public partial class Minion
    {
        public abstract class InternalTaskGoTo : Task
        {
            private readonly PathCfg cfg;

            private bool onFallbackPath;
            private LinkedList<Vec2I> path;
            private LineRenderer pathVis;

            protected InternalTaskGoTo(GameController game, PathCfg cfg)
                : base(game)
            {
                this.cfg = cfg;
                this.onFallbackPath = false;
            }

            private bool GetPath()
            {
                var pts = game.GetPath(minion.realPos.Floor(), cfg);
                if (pts == null)
                    return false;

                path = new LinkedList<Vec2I>(pts);
                var dir = pts[0] - minion.realPos;
                if (pts.Length > 1 && (dir.magnitude < float.Epsilon || Vec2.Dot(dir, pts[1] - pts[0]) < -float.Epsilon))
                    path.RemoveFirst();

                return true;
            }

            private void UpdatePathVis()
            {
                BB.AssertNotNull(pathVis);

                Vec2 ofs = new Vec2(.5f, .5f);
                pathVis.positionCount = path.Count + 1;
                pathVis.SetPosition(0, minion.realPos + ofs);
                var n = path.First;
                for (int i = 1; i < pathVis.positionCount; ++i)
                {
                    pathVis.SetPosition(i, n.Value + ofs);
                    n = n.Next;
                }
            }

            protected override Status OnBeginTask()
            {
                if (minion.GridAligned() && cfg.destinationFn(minion.pos))
                    return Status.Complete;

                if (!GetPath())
                    return Status.Fail;

                pathVis = game.assets.CreateLine(
                    game.transform, Vec2.zero, "MinionPath",
                    RenderLayer.Default.Layer(1000),
                    new Color(.2f, .2f, .2f, .5f),
                    1 / 32f, false, true, null);
                UpdatePathVis();

                minion.skin.SetAnimLoop(MinionAnim.Walk);
                return Status.Continue;
            }

            protected override void OnEndTask(bool canceled)
            {
                if (pathVis != null)
                    pathVis.transform.gameObject.Destroy();
            }

            // TODO: call this from somewhere useful
            public override void Reroute(Vec2I updatedTile)
            {
                if (path == null || !path.Contains(updatedTile))
                    return;

                if (!GetPath())
                {
                    path = new LinkedList<Vec2I>();
                    path.AddLast(minion.realPos.Floor());
                    onFallbackPath = true;
                }

                UpdatePathVis();
            }

            public override Status PerformTask(float deltaTime)
            {
                float distance = deltaTime * minion.speed;
                while (true)
                {
                    Vec2 dir = path.First.Value - minion.realPos;
                    if (dir.magnitude > distance)
                    {
                        minion.realPos += dir.normalized * distance;

                        minion.SetFacing(path.First.Value - minion.realPos);
                        UpdatePathVis();
                        return Status.Continue;
                    }
                    else
                    {
                        minion.realPos = path.First.Value;
                        distance -= dir.magnitude;
                        path.RemoveFirst();
                        if (!path.Any())
                        {
                            // TODO: delete path vis
                            minion.skin.SetAnimLoop(MinionAnim.None);
                            return onFallbackPath ? Status.Fail : Status.Complete;
                        }
                    }
                }
            }
        }
    }
}