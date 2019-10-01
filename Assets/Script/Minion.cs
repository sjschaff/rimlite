using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class Minion
    {
#if DEBUG
        private static int D_nextID = 0;
        public readonly int D_uniqueID;
#endif

        public readonly GameController game;
        public MinionSkin skin { get; }

        private Job2 currentJob;

        public Item carriedItem { get; private set; }
        public bool carryingItem => carriedItem != null;
        public float speed => 2;
        public bool hasJob => currentJob != null;

        private Vec2 realPos
        {
            get => skin.transform.position.xy();
            set => skin.transform.position = value;
        }

        public Vec2I pos { get { BB.Assert(GridAligned()); return realPos.Floor(); } }

        public bool GridAligned()
        {
            var p = realPos;
            return (p.x % 1f) < Mathf.Epsilon && (p.y % 1) < Mathf.Epsilon;
        }

        public bool InTile(Vec2I tile) => Vec2.Distance(realPos, tile) < .9f;

        public Minion(GameController game, Vec2 pos)
        {
#if DEBUG
            D_uniqueID = D_nextID;
            ++D_nextID;
#endif

            this.game = game;
            skin = new GameObject("Minion").AddComponent<MinionSkin>();
            skin.transform.SetParent(game.transform, false);
            skin.Init(game.assets);
            realPos = pos;
        }

        public void SetFacing(Vec2 dir)
        {
            skin.SetDir(dir);
            if (carriedItem != null)
                ReconfigureItem();
        }

        private void ReconfigureItem()
        {
            BB.AssertNotNull(carriedItem);
            carriedItem.Configure(
                skin.dir == MinionSkin.Dir.Up ?
                    Item.Config.PlayerBelow :
                    Item.Config.PlayerAbove);
        }

        public void PickupItem(Item item)
        {
            BB.Assert(!carryingItem);
            carriedItem = item;
            carriedItem.ReParent(skin.transform, new Vec2(0, .2f));
            ReconfigureItem();
        }

        public Item RemoveItem()
        {
            BB.AssertNotNull(carriedItem);
            Item ret = carriedItem;
            carriedItem = null;
            return ret;
        }

        public void DropItem() => game.DropItem(pos, RemoveItem());

        public void Update(float deltaTime)
        {
            if (currentJob != null)
                currentJob.PerformWork(deltaTime);
        }

        public bool AssignJob(Job2 job)
        {
            if (currentJob != null)
                currentJob.Abandon(this);

            currentJob = job;
            return currentJob.Claim(this);
        }

        public void AbandonJob()
        {
            BB.AssertNotNull(currentJob);
            currentJob.Abandon(this);
            currentJob = null;

            // TODO: handle case of not being Grid Aligned
            if (!GridAligned())
                realPos = pos;
        }

        public class TaskGoTo : Task2
        {
            private readonly Func<Vec2I, bool> dstFn;
            private readonly Vec2I endHint;
            // private readonly Func<Vec2I, float> hueFn;

            private LinkedList<Vec2I> path;
            private LineRenderer pathVis;

            private TaskGoTo(GameController game, Func<Vec2I, bool> dstFn, Vec2I endHint)
                : base(game)
            {
                this.dstFn = dstFn;
                this.endHint = endHint;
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

            protected override WorkStatus OnBeginWork()
            {
                if (minion.GridAligned() && dstFn(minion.pos))
                    return WorkStatus.Complete;

                if (!GetPath())
                    return WorkStatus.Fail;

                pathVis = game.assets.CreateLine(
                    game.transform, Vec2.zero, "MinionPath",
                    RenderLayer.Default.Layer(1000),
                    new Color(.2f, .2f, .2f, .5f),
                    1 / 32f, false, true, null);
                UpdatePathVis();

                minion.skin.SetAnimLoop(MinionAnim.Walk);
                return WorkStatus.Continue;
            }

            protected override void OnEndWork(bool canceled)
            {
                if (pathVis != null)
                    pathVis.transform.gameObject.Destroy();
            }

            // TODO: call this from somewhere useful
            public void Reroute(Vec2I updatedTile)
            {
                BB.Assert(path != null);

                if (!path.Contains(updatedTile))
                    return;

                if (!GetPath())
                {
                    job.Cancel();
                     // actually this could track its failure state and
                     // finish moving then return Fail
                     // TODO: assign new job to minion to walk to nearest tile

                    //// TODO: figure out how to handle edge case of currentently in or going into newly solid tile
                    //path = new LinkedList<Vec2I>();
                    //path.AddLast(pos.Floor());


                    throw new NotImplementedException();
                }

                UpdatePathVis();
            }

            public override WorkStatus PerformWork(float deltaTime)
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
                        return WorkStatus.Continue;
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
                            return WorkStatus.Complete;
                        }
                    }
                }
            }
        }
    }
}