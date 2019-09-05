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
    private LinkedList<Vec2I> path;

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
        line = GetComponent<LineRenderer>();
    }

    // Use this for initialization
    void Start()
    {
    }

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
            skin.SetDir(path.First.Value - pos);
            UpdateLine();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (path != null)
        {
            Move();

            if (path == null)
            {
                if (pos.Floor() != currentTask.pos)
                    skin.SetDir(currentTask.pos - pos);

                if (currentTask.HasWork())
                {
                    skin.SetSlashing(true);
                    skin.SetTool(currentTask.tool);
                }
            }
        }
        else if (currentTask != null)
        {
            if (currentTask.PerformWork(Time.deltaTime))
            {
                currentTask = null;
                skin.SetSlashing(false);
                skin.SetTool(Tool.None);
            }
        }
    }

    private void UpdateLine()
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

    public bool AssignTask(Task task)
    {
        BB.Assert(task != null);
        if (currentTask != null)
        {
            currentTask.Abandon();
            currentTask = null;
        }

        if (path == null && task.CanWorkFrom(pos.Floor()))
        {
            currentTask = task;
            return true;
        }
        else
        {
            var pts = PathToTask(task);
            if (pts == null)
            {
                Debug.Log("No Path To Task");
                return false;
            }
            else
            {
                FollowPath(pts);
                currentTask = task;
                return true;
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
            currentTask.Abandon();
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

        UpdateLine();
        line.enabled = true;
        skin.SetWalking(true);
    }

    private void PathFinished()
    {
        path = null;
        line.enabled = false;
        skin.SetWalking(false);
    }
}
