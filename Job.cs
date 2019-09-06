using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Vec2I = UnityEngine.Vector2Int;

public enum Tool { None, Hammer, Pickaxe, Axe };

public interface Job
{
    IEnumerable<Task> AvailableTasks();
    void ClaimTask(Minion minion, Task task);
    void AbandonTask(Minion minion, Task task);

    // Returns a follow up task or null if none
    Task CompleteTask(Minion minion, Task task);
}

public class JobWalkDummy : Job
{
    public IEnumerable<Task> AvailableTasks() => throw new NotImplementedException();
    public void ClaimTask(Minion minion, Task task) { }
    public Task CompleteTask(Minion minion, Task task) => null;
    public void AbandonTask(Minion minion, Task task) { }

    public Task CreateWalkTask(Vec2I pos)
        => new Task(this, null, pos, v => v == pos, Tool.None, MinionAnim.None, 0);
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

        task = new Task(this, null, pos, v => v.Adjacent(pos), tile.building.miningTool, MinionAnim.Slash, 2);
        claimed = false;
    }

    public IEnumerable<Task> AvailableTasks()
    {
        if (!claimed)
            yield return task;
    }

    public void ClaimTask(Minion minion, Task task)
    {
        BB.Assert(task == this.task);
        BB.Assert(!claimed);

        claimed = true;
    }

    public void AbandonTask(Minion minion, Task task)
    {
        BB.Assert(task == this.task);
        BB.Assert(claimed);

        claimed = false;
    }

    public Task CompleteTask(Minion minion, Task task)
    {
        BB.Assert(task == this.task);
        BB.Assert(claimed);

        Building building = game.map.Tile(task.pos).building;
        game.RemoveBuilding(task.pos);
        foreach (ItemInfo item in building.GetMinedMaterials())
            game.DropItem(task.pos, item);

        game.RemoveJob(this);
        return null;
    }
}

// TODO: support hualing to multiple jobs
public class JobBuild : Job
{
    private class HaulTaskInfo : TaskInfo
    {
        public readonly HaulInfo haul;
        public readonly int amt;
        public bool isPickupTask;
        public Item item;

        public HaulTaskInfo(HaulInfo haul, int amt, Item item)
        {
            BB.Assert(amt > 0);
            this.haul = haul;
            this.amt = amt;
            this.isPickupTask = true;
            this.item = item;
        }

        public void Pickup(Item item)
        {
            this.isPickupTask = false;
            this.item = item;
        }

        public override string ToString() => "HaulTask{" + isPickupTask + ", " + amt + "}";
    }

    private class HaulInfo
    {
        public readonly ItemInfo info;
        public int amtStored;
        public int amtClaimed;

        public int amtRemaining => info.amt - amtStored - amtClaimed;

        public HaulInfo(ItemInfo info)
        {
            this.info = info;
            this.amtStored = this.amtClaimed = 0;
        }
    }

    private enum State { Hauling, BuildUnclaimed, BuildClaimed }

    private readonly GameController game;
    private readonly Vec2I pos;
    private readonly BuildingVirtual virtualBuilding;

    private State state;
    private readonly List<HaulInfo> hauls;

    public JobBuild(GameController game, Vec2I pos, Building building)
    {
        this.game = game;
        this.pos = pos;
        virtualBuilding = new BuildingVirtual(this, building);
        game.AddBuilding(pos, virtualBuilding);

        state = State.Hauling;
        hauls = new List<HaulInfo>();
        foreach (var item in building.GetBuildMaterials())
            hauls.Add(new HaulInfo(item));
    }

    public IEnumerable<Task> AvailableTasks()
    {
        if (state == State.BuildClaimed)
            yield break;

        if (state == State.BuildUnclaimed)
        {
            yield return TaskBuild();
            yield break;
        }

        // TODO: something far more interesting whereby nearer items and larger stack sizes are prioritized
        foreach (HaulInfo haul in hauls)
        {
            if (haul.amtRemaining > 0)
            {
                foreach (Item item in game.FindItems(haul.info.type))
                {
                    if (item.amtAvailable == 0)
                        continue;

                    int amt = Math.Min(haul.amtRemaining, item.amtAvailable);
                    Item item2 = item;
                    var info = new HaulTaskInfo(haul, amt, item);
                    yield return new Task(this, info, item.pos, v => v == item.pos, Tool.None, MinionAnim.Magic, .425f);
                }
            }
        }
    }

    public void ClaimTask(Minion minion, Task task)
    {
        BB.Assert(state != State.BuildClaimed);

        if (state == State.BuildUnclaimed)
            state = State.BuildClaimed;
        else
        {
            HaulTaskInfo info = (HaulTaskInfo)task.info;
            if (info.isPickupTask)
            {
                info.haul.amtClaimed += info.amt;
                info.item.Claim(info.amt);
                BB.Assert(info.haul.amtRemaining >= 0);
            }
        }
    }

    public void AbandonTask(Minion minion, Task task)
    {
        BB.Assert(state != State.BuildUnclaimed);
        if (state == State.BuildClaimed)
        {
            // TODO: track build progress
            // TODO: trigger pathing update when construction starts
            state = State.BuildUnclaimed;
        }
        else
        {
            HaulTaskInfo info = (HaulTaskInfo)task.info;
            if (info.isPickupTask)
                info.item.Unclaim(info.amt);
            else
            {
                BB.Assert(minion.carriedItem == info.item);
                minion.DropItem();
            }

            BB.Assert(info.amt <= info.haul.amtClaimed);
            info.haul.amtClaimed -= info.amt;
        }
    }

    public Task CompleteTask(Minion minion, Task task)
    {
        BB.Assert(state != State.BuildUnclaimed);
        if (state == State.BuildClaimed)
        {
            BB.Assert(game.map.Tile(pos).building == virtualBuilding);
            Debug.Log("build complete.");
            game.ReplaceBuilding(pos, virtualBuilding.building);
            game.RemoveJob(this);
            return null;
        }
        else
        {
            HaulTaskInfo info = (HaulTaskInfo)task.info;
            if (info.isPickupTask)
            {
                info.item.Unclaim(info.amt);
                Item item = game.TakeItem(info.item, info.amt);
                minion.PickupItem(item);
                info.Pickup(item);
                return new Task(this, info, pos, v => v == pos, Tool.None, MinionAnim.None, 0);
            }
            else
            {
                BB.Assert(minion.carriedItem == info.item);
                BB.Assert(info.haul.amtClaimed >= info.amt);
                info.haul.amtClaimed -= info.amt;
                info.haul.amtStored += info.amt;
                BB.Assert(info.haul.amtRemaining >= 0);
                minion.RemoveItem().Destroy();

                if (info.haul.amtRemaining == 0)
                {
                    bool doneHauling = true;
                    foreach (var haul in hauls)
                    {
                        if (haul.amtRemaining > 0)
                        {
                            doneHauling = false;
                            break;
                        }
                    }

                    if (doneHauling)
                    {
                        state = State.BuildUnclaimed;
                        return TaskBuild();
                    }
                }

                return null;
            }
        }
    }

    private Task TaskBuild() => new Task(this, null, pos, v => v.Adjacent(pos), Tool.Hammer, MinionAnim.Slash, 2);
}

public interface TaskInfo {}

public class Task
{
    private readonly Job job;
    public readonly TaskInfo info;
    private readonly Func<Vec2I, bool> workFromFn;

    public readonly Vec2I pos;
    public readonly Tool tool;
    public readonly MinionAnim anim;
    public readonly float workAmt;

    private float workRemaining;

    public Task(Job job, TaskInfo info, Vec2I pos, Func<Vec2I, bool> workFromFn, Tool tool, MinionAnim anim, float workAmt)
    {
        BB.Assert(job != null);

        this.job = job;
        this.info = info;
        this.pos = pos;
        this.workFromFn = workFromFn;
        this.tool = tool;
        this.anim = anim;
        this.workAmt = workAmt;

        workRemaining = workAmt;
    }

    public void Claim(Minion minion) => job.ClaimTask(minion, this);

    public bool HasWork() => workRemaining != 0;

    public bool CanWorkFrom(Vec2I tile) => workFromFn(tile);

    public Vec2I[] GetPath(Map map, Vec2I start)
        => AStar.FindPath(map, start, pos, workFromFn);

    // Returns true if complete
    public void PerformWork(float amt)
    {
        workRemaining = Mathf.Max(0, workRemaining - amt);
    }

    public Task Complete(Minion minion)
    {
        BB.Assert(!HasWork());
        return job.CompleteTask(minion, this);
    }

    public void Abandon(Minion minion)
    {
        job.AbandonTask(minion, this);
    }

    public override string ToString() => "Task{" + job + ", " + info + "}";
}
