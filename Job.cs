using UnityEngine;
using System.Collections;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

public enum JobType { Move, Mine };

public class Job
{
    public readonly GameController game;
    public readonly JobType type;
    public readonly Vec2I pos;

    private float workRemaining;
    public Job(GameController game, JobType type, Vec2I pos)
    {
        this.game = game;
        this.type = type;
        this.pos = pos;

        workRemaining = 2;
        if (type == JobType.Move)
            workRemaining = 0;
    }

    public bool WorkAdjacent()
    {
        switch (type)
        {
            case JobType.Move: return false;
            case JobType.Mine: return true;
        }

        throw new System.Exception("Unknown job: " + type);
    }

    public bool CanWorkFrom(Vec2I tile) => WorkAdjacent() ? tile.Adjacent(this.pos) : tile == this.pos;
        

    public bool IsPersonal()
    {
        return type == JobType.Move;
    }

    public MinionSkin.Tool Tool()
    {
        switch (type)
        {
            case JobType.Mine:
            {
                var tile = game.map.Tile(pos);
                BB.Assert(tile.HasBuilding());
                BB.Assert(tile.building.mineable);
                return tile.building.miningTool;
            }
            case JobType.Move:
            default: return MinionSkin.Tool.None;
        }
    }

    public bool PerformWork(float amt)
    {
        workRemaining = Mathf.Max(0, workRemaining - amt);
        return workRemaining == 0;
    }
}
