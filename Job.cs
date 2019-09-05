using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Vec2I = UnityEngine.Vector2Int;

public enum Tool { None, Hammer, Pickaxe, Axe };

public interface Job
{
    IEnumerable<Task> AvailableTasks();
    void ClaimTask(Task task);
    void AbandonTask(Task task);
    void CompleteTask(Task task);
}

public class JobWalkDummy : Job
{
    public IEnumerable<Task> AvailableTasks() => throw new NotImplementedException();
    public void ClaimTask(Task task) => throw new NotImplementedException();
    public void CompleteTask(Task task) { }
    public void AbandonTask(Task task) { }

    public Task CreateWalkTask(Vec2I pos)
        => new Task(this, pos, v => v == pos, Tool.None, 0);
}

public class JobMine : Job
{
    private readonly GameController game;
    private readonly Task task;
    private bool claimed;

    public JobMine(GameController game, Vec2I pos)
    {
        this.game = game;
        var tile = game.map.Tile(pos);
        BB.Assert(tile.HasBuilding());
        BB.Assert(tile.building.mineable);

        task = new Task(this, pos, v => v.Adjacent(pos), tile.building.miningTool, 2);
        claimed = false;
    }

    public IEnumerable<Task> AvailableTasks()
    {
        if (!claimed)
            yield return task;
    }

    public void ClaimTask(Task task)
    {
        BB.Assert(task == this.task);
        BB.Assert(!claimed);

        claimed = true;
    }

    public void AbandonTask(Task task)
    {
        BB.Assert(task == this.task);
        BB.Assert(claimed);

        claimed = false;
    }

    public void CompleteTask(Task task)
    {
        BB.Assert(task == this.task);
        BB.Assert(claimed);
        game.RemoveJob(this);

        game.RemoveBuilding(task.pos);
        game.DropItem(task.pos, new ItemInfo(ItemType.Stone, 37));
    }
}

public class JobBuild
{
   // public 
}


public class Task
{
    private readonly Job job;
    private readonly Func<Vec2I, bool> workFromFn;

    public readonly Vec2I pos;
    public readonly Tool tool;
    // TODO: work anim

    private float workRemaining;

    public Task(Job job, Vec2I pos, Func<Vec2I, bool> workFromFn, Tool tool, float workAmt)
    {
        this.job = job;
        this.pos = pos;
        this.workFromFn = workFromFn;
        this.tool = tool;
        this.workRemaining = workAmt;
    }

    public bool HasWork() => workRemaining != 0;

    public bool CanWorkFrom(Vec2I tile) => workFromFn(tile);

    public Vec2I[] GetPath(Map map, Vec2I start)
        => AStar.FindPath(map, start, pos, workFromFn);

    // Returns true if complete
    public bool PerformWork(float amt)
    {
        workRemaining = Mathf.Max(0, workRemaining - amt);
        bool finished = workRemaining == 0;

        if (finished)
            job.CompleteTask(this);
        return finished;
    }

    public void Abandon()
    {
        job.AbandonTask(this);
    }
}
