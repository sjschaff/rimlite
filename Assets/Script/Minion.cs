using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{

    public class Minion
    {
        const float speed = 2;

        private readonly GameController game;
        private readonly MinionSkin skin;
        private readonly LineRenderer line;

        private Task currentTask;
        public Item carriedItem { get; private set; }
        public bool carryingItem => carriedItem != null;
        private LinkedList<Vec2I> path;

        public bool idle => currentTask == null; // TODO: make sure path is always null if currentTask is null

        public Vec2 pos
        {
            get => skin.transform.position.xy();
            private set => skin.transform.position = value;
        }

        public bool HasTask() => currentTask != null;

        public Minion(GameController game, Vec2 pos)
        {
            this.game = game;
            skin = new GameObject("skin").AddComponent<MinionSkin>();
            skin.transform.SetParent(game.transform, false);
            skin.Init(game.assets);
            this.pos = pos;

            line = game.assets.CreateLine(
                skin.transform, Vec2.zero, "MinionPath",
                RenderLayer.Default.Layer(1000),
                new Color(.2f, .2f, .2f, .5f),
                1 / 32f, false, true, null);
        }

        private void SetDir(Vec2 dir)
        {
            skin.SetDir(dir);
            if (carriedItem != null)
                ReconfigureItem();
        }

        public void ReconfigureItem()
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

        public void DropItem() => game.DropItem(pos.Floor(), RemoveItem());

        private void Move()
        {
            float distance = Time.deltaTime * speed;
            while (distance > float.Epsilon)
            {
                Vec2 dir = path.First.Value - pos;
                if (dir.magnitude > distance)
                {
                    pos += dir.normalized * distance;
                    distance = 0;
                }
                else
                {
                    pos = path.First.Value;
                    distance -= dir.magnitude;
                    path.RemoveFirst();
                    if (!path.Any())
                    {
                        PathFinished();
                        distance = 0;
                    }
                }
            }

            if (path != null)
            {
                SetDir(path.First.Value - pos);
                UpdatePathVis();
            }
        }

        private void OnBeginWork()
        {
            BB.Assert(path == null);
            BB.Assert(currentTask != null);

            if (pos.Floor() != currentTask.pos)
                SetDir(currentTask.pos - pos);

            skin.SetTool(currentTask.tool);
            skin.SetAnimLoop(currentTask.anim);
        }

        private void OnEndWork()
        {
            BB.Assert(currentTask == null);

            skin.SetTool(Tool.None);
            skin.SetAnimLoop(MinionAnim.None);
        }

        public void Update()
        {
            if (path != null)
            {
                Move();

                if (path == null)
                    OnBeginWork();
            }
            else if (currentTask != null)
            {
                currentTask.PerformWork(Time.deltaTime);
                if (!currentTask.HasWork())
                {
                    //Debug.Log("Completed Task: " + currentTask);
                    Task taskNext = currentTask.Complete(this);
                    currentTask = null;
                    OnEndWork();

                    if (taskNext != null)
                    {
                        //Debug.Log("Assigning followup task.");
                        AssignTask(taskNext);
                    }
                }
            }
        }

        private void UpdatePathVis()
        {
            Vec2 ofs = new Vec2(.5f, .5f);
            line.positionCount = path.Count + 1;
            line.SetPosition(0, pos + ofs);
            var n = path.First;
            for (int i = 1; i < line.positionCount; ++i)
            {
                line.SetPosition(i, n.Value + ofs);
                n = n.Next;
            }
        }

        private void AssignTask(Task task, Vec2I[] pts)
        {
            if (currentTask != null)
            {
                //Debug.Log("Abandoning current task: " + currentTask);
                currentTask.Abandon(this);
            }

            currentTask = task;
            currentTask.Claim(this);

            if (pts == null)
            {
                BB.Assert(task.CanWorkFrom(pos.Floor()));
                OnBeginWork();
            }
            else
            {
                FollowPath(pts);
            }
        }

        public bool AssignTask(Task task)
        {
            BB.Assert(task != null);

            if (path == null && task.CanWorkFrom(pos.Floor()))
            {
                //Debug.Log("Accepting task (adj): " + task);
                AssignTask(task, null);
                return true;
            }
            else
            {
                var pts = PathToTask(task);
                if (pts != null)
                {
                    //Debug.Log("Accepting task (path): " + task);
                    AssignTask(task, pts);
                    return true;
                }
                else
                {
                    Debug.Log("Rejecting task (no path): " + task);
                    return false;
                }
            }
        }

        public void Reroute(Vec2I updatedTile)
        {
            BB.Assert(currentTask != null);

            if (path == null || !path.Contains(updatedTile))
                return;

            var pts = PathToTask(currentTask);
            if (pts == null)
            {
                Debug.Log("Abandoning Task: No Path");
                currentTask.Abandon(this);
                currentTask = null;

                // TODO: figure out how to handle edge case of currentently in or going into newly solid tile
                path = new LinkedList<Vec2I>();
                path.AddLast(pos.Floor());
            }
            else
            {
                FollowPath(pts);
            }
        }

        private Vec2I[] PathToTask(Task task) => task.GetPath(game, pos.Floor());

        private void FollowPath(Vec2I[] pts)
        {
            path = new LinkedList<Vec2I>(pts);
            var dir = pts[0] - pos;
            if (pts.Length > 1 && (dir.magnitude < float.Epsilon || Vec2.Dot(dir, pts[1] - pts[0]) < -float.Epsilon))
                path.RemoveFirst();

            UpdatePathVis();
            line.enabled = true;
            skin.SetAnimLoop(MinionAnim.Walk);
        }

        private void PathFinished()
        {
            path = null;
            line.enabled = false;
            skin.SetAnimLoop(MinionAnim.None);
        }
    }

}