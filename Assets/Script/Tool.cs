using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

public abstract class UITool
{
    public static LinkedList<UITool> RegisterTools(GameController game)
    {
        LinkedList<UITool> tools = new LinkedList<UITool>();

        tools.AddLast(new ToolControlMinion(game));
        tools.AddLast(new ToolMine(game));
        tools.AddLast(new ToolPlace(game));
        tools.AddLast(new ToolBuild(game));

        return tools;
    }

    protected readonly GameController game;

    protected UITool(GameController game) => this.game = game;

    public static RectInt RectInclusive(Vec2 start, Vec2 end)
    {
        Vec2 lower = Vec2.Min(start, end);
        Vec2 upper = Vec2.Max(start, end);

        Vec2I tileStart = lower.Floor();
        Vec2I tileEnd = upper.Ceil();

        return new RectInt(tileStart, tileEnd - tileStart);
    }

    // TODO: make this more general
    public virtual void OnTab() { }

    public virtual void OnClick(Vec2I pos) { }
    public virtual void OnDragStart(Vec2 start, Vec2 end)
        => OnDragStart(RectInclusive(start, end));

    public virtual void OnDragUpdate(Vec2 start, Vec2 end)
        => OnDragUpdate(RectInclusive(start, end));

    public virtual void OnDragEnd(Vec2 start, Vec2 end)
        => OnDragEnd(RectInclusive(start, end));

    protected virtual void OnDragStart(RectInt rect) => OnDragUpdate(rect);
    protected virtual void OnDragUpdate(RectInt rec) { }
    protected virtual void OnDragEnd(RectInt rect) { }
}

public class ToolControlMinion : UITool
{
    public ToolControlMinion(GameController game) : base(game) { }

    public override void OnClick(Vec2I pos)
    {
        game.K_MoveMinion(pos);
    }
}

public class ToolMine : UITool
{
    private Dictionary<Vec2I, Transform> activeOverlays = new Dictionary<Vec2I, Transform>();

    public ToolMine(GameController game) : base(game) { }

    public override void OnClick(Vec2I pos)
    {
        var tile = game.map.Tile(pos);
        if (tile.mineable && !tile.hasJob)
            game.AddJob(new JobMine(game, pos));
    }

    protected override void OnDragUpdate(RectInt rect)
    {
        List<Vec2I> toRemove = new List<Vec2I>();
        foreach (var kvp in activeOverlays)
        {
            if (!rect.Contains(kvp.Key))
            {
                kvp.Value.Destroy();
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var v in toRemove)
            activeOverlays.Remove(v);

        foreach (var v in rect.AllTiles())
        {
            if (!activeOverlays.ContainsKey(v))
            {
                if (game.map.Tile(v).mineable)
                {
                    var o = JobMine.CreateOverlay(game, v);
                    activeOverlays.Add(v, o);
                }
            }
        }
    }

    protected override void OnDragEnd(RectInt rect)
    {
        foreach (var v in rect.AllTiles())
            OnClick(v);

        foreach (var kvp in activeOverlays)
            kvp.Value.Destroy();

        activeOverlays = new Dictionary<Vec2I, Transform>();
    }
}

public class ToolBuild : UITool
{
    private int currentBuild = 0;

    public ToolBuild(GameController game) : base(game) { }

    public override void OnTab()
    {
        currentBuild = (currentBuild + 1) % 2;
        Debug.Log("Build " + currentBuild);
    }

    public override void OnClick(Vec2I pos)
    {
        if (!game.map.Tile(pos).hasBuilding)
        {
            Building building = null;
            switch (currentBuild)
            {
                case 0: building = new BuildingWall(BuildingWall.Wall.StoneBrick); break;
                case 1: building = new BuildingFloor(BuildingFloor.Floor.StoneBrick); break;
            }
            game.AddJob(new JobBuild(game, pos, building));
        }
    }

    protected override void OnDragEnd(RectInt rect)
    {
        foreach (var v in rect.AllTiles())
            OnClick(v);
    }
}

public class ToolPlace : UITool
{
    public ToolPlace(GameController game) : base(game) { }

    public override void OnClick(Vec2I pos)
    {
        game.ModifyTerrain(pos, new TerrainStandard(TerrainStandard.Terrain.Path));
    }
}
