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
    private Job currentJob;
    private LinkedList<Vec2I> path;

    public Vec2 pos
    {
        get => transform.position.xy();
        private set => transform.position = new Vec3(value.x, value.y, 0);
    }

    public bool HasJob() => currentJob != null;

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
                if (pos.Floor() != currentJob.tile)
                    skin.SetDir(currentJob.tile - pos);

                skin.SetSlashing(true);
                skin.SetTool(MinionSkin.Tool.Pickaxe);
            }
        }
        else if (currentJob != null)
        {
            if (currentJob.PerformWork(Time.deltaTime))
            {
                game.CompleteJob(currentJob);
                currentJob = null;
                skin.SetSlashing(false);
                skin.SetTool(MinionSkin.Tool.None);
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

    public bool AssignJob(Job job)
    {
        BB.Assert(job != null);
        if (currentJob != null)
        {
            game.AbandonJob(currentJob);
            currentJob = null;
        }

        if (path == null && job.CanWorkFrom(pos.Floor()))
        {
            currentJob = job;
            return true;
        }
        else
        {
            var pts = PathToJob(job);
            if (pts == null)
            {
                Debug.Log("No Path To Job");
                return false;
            }
            else
            {
                FollowPath(pts);
                currentJob = job;
                return true;
            }
        }
    }

    public void Reroute(Vec2I updatedTile)
    {
        BB.Assert(currentJob != null);

        if (!path.Contains(updatedTile))
            return;

        var pts = PathToJob(currentJob);
        if (pts == null)
        {
            Debug.Log("Abandoning Job: No Path");
            game.AbandonJob(currentJob);
            currentJob = null;

            // TODO: figure out how to handle edge case of currentently in or going into newly solid tile
            path = new LinkedList<Vec2I>();
            path.AddLast(pos.Floor());
        }
        else
        {
            FollowPath(pts);
        }
    }

    private Vec2I[] PathToJob(Job job)
    {
        if (job.WorkAdjacent())
            return AStar.FindPathAdjacent(game.map, pos.Floor(), job.tile);
        else
            return AStar.FindPath(game.map, pos.Floor(), job.tile);
    }

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
