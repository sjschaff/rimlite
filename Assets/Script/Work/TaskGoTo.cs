using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public partial class Minion
    {
        public class TaskGoTo : Task
        {
            private readonly Func<Vec2I, bool> dstFn;
            private readonly Vec2I endHint;
            // private readonly Func<Vec2I, float> hueFn;

            private bool onFallbackPath;
            private LinkedList<Vec2I> path;
            private LineRenderer pathVis;

            private TaskGoTo(GameController game, Func<Vec2I, bool> dstFn, Vec2I endHint)
                : base(game)
            {
                this.dstFn = dstFn;
                this.endHint = endHint;
                this.onFallbackPath = false;
            }

            public static TaskGoTo Point(GameController game, Vec2I end)
                => new TaskGoTo(game, pt => pt == end, end);

            // TODO: this is gonna get interesting with claims, well have to find a path first
            // then claim the endpoint, pathfinding will also need to not allow claimed tiles
            // also the hueristic for this is totalled f'ed right now
            public static TaskGoTo Vacate(GameController game, Vec2I pos)
                => new TaskGoTo(game, v => v != pos, pos);

            // TODO: see Vacate, also take building size or something instead
            public static TaskGoTo Adjacent(GameController game, Vec2I pos)
                => new TaskGoTo(game, v => v.Adjacent(pos), pos);

            private bool GetPath()
            {
                var pts = game.GetPath(minion.realPos.Floor(), endHint, dstFn);
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
                if (minion.GridAligned() && dstFn(minion.pos))
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