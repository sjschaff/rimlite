using UnityEngine;
using System.Collections;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using System.Collections.Generic;
using System.Linq;

public class Minion : MonoBehaviour
{
    const float speed = 2;

    private LineRenderer line;
    public MinionSkin skin;

    private GameController game;
    private Task currentTask;
    public Item carriedItem { get; private set; }
    public bool carryingItem => carriedItem != null;
    private LinkedList<Vec2I> path;

    public bool idle => currentTask == null; // TODO: make sure path is always null if currentTask is null

    public Vec2 pos
    {
        get => transform.position.xy();
        private set => transform.position = new Vec3(value.x, value.y, 0);
    }

    public bool HasTask() => currentTask != null;

    public void Init(GameController game)
    {
        this.game = game;
    }

    void Awake()
    {
        line = gameObject.AddLineRenderer(new Color(.2f, .2f, .2f, .5f), 1/32f, false, true, null);
    }

    // Use this for initialization
    void Start()
    {
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
        carriedItem.transform.parent = transform;
        carriedItem.transform.localPosition = new Vec3(0, .2f, 0);
        ReconfigureItem();
    }

    public Item RemoveItem()
    {
        BB.Assert(carriedItem);
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

    // Update is called once per frame
    void Update()
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

    private Vec2I[] PathToTask(Task task) => task.GetPath(game.map, pos.Floor());

    private void FollowPath(Vec2I[] pts)
    {
        path = new LinkedList<Vec2I>(pts);
        var dir = pts[0] - pos;
        if (dir.magnitude < float.Epsilon || Vec2.Dot(dir, pts[1] - pts[0]) < -float.Epsilon)
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
