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

    public abstract void OnClick(Vec2I pos);
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
    public ToolMine(GameController game) : base(game) { }

    public override void OnClick(Vec2I pos)
    {
        var tile = game.map.Tile(pos);
        if (tile.mineable && !tile.hasJob)
            game.AddJob(new JobMine(game, pos));
    }
}

public class ToolBuild : UITool
{
    public ToolBuild(GameController game) : base(game) { }

    public override void OnClick(Vec2I pos)
    {
        if (!game.map.Tile(pos).hasBuilding)
            game.AddJob(new JobBuild(game, pos, new BuildingWall(BuildingWall.Wall.StoneBrick)));
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
