using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;
using System;

public interface Building
{
    Sprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile);
    Sprite GetSpriteOver(MapTiler tiler, Vec2I pos);

    bool passable { get; }
    bool tiledRender { get; }
    bool mineable { get; }
    bool oversized { get; }
}

public abstract class BuildingSmp : Building
{
    public Sprite GetSpriteOver(MapTiler tiler, Vec2I pos)
        => throw new Exception("Get Sprite Over Called on BuildingSmp");
    public bool oversized => false;
    public bool mineable => false;

    public abstract Sprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile);
    public abstract bool passable { get; }
    public abstract bool tiledRender { get; }

}


public class BuildingFloor : BuildingSmp
{
    public enum Floor { StoneBrick } // TODO: get rid of this enum
    private readonly Floor floor;

    public override bool passable => true;
    public override bool tiledRender => true;

    private static Vec2I FloorOrigin(Floor floor)
    {
        throw new NotImplementedException("Unknown Floor: " + floor);
    }

    public BuildingFloor(Floor floor) => this.floor = floor;

    private bool IsSame(Map map, Vec2I pos)
    {
        if (!map.ValidTile(pos))
            return false;

        var other = map.Tile(pos).building as BuildingFloor;
        return other == null ? false : other.floor == floor;
    }

    public override Sprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile)
    {
        TerrainStandard.TileType ttype = TerrainStandard.GetTileType(pos, subTile, p => IsSame(tiler.map, p));
        Vec2I spritePos = FloorOrigin(floor) + TerrainStandard.SpriteOffset(ttype);
        return tiler.atlas32.GetSprite(spritePos, Vec2I.one, 64);
    }
}

public class BuildingWall : BuildingSmp
{
    // TODO: get rid of this enum
    public enum Wall { StoneBrick }
    private readonly Wall wall;

    public override bool passable => false;
    public override bool tiledRender => true;

    private static Vec2I WallOrigin(Wall wall)
    {
        switch (wall)
        {
            case Wall.StoneBrick: return new Vec2I(0, 12);
        }

        throw new NotImplementedException("Unknown Building: " + wall);
    }

    public BuildingWall(Wall wall) => this.wall = wall;

    private bool IsSame(Map map, Vec2I pos)
    {
        if (!map.ValidTile(pos))
            return false;

        var other = map.Tile(pos).building as BuildingWall;
        return other == null ? false : other.wall == wall;
    }

    // TODO: so ghetto
    private Vec2I SpriteOffset(bool[,] adj, TerrainStandard.TileType ttype, Vec2I subTile)
    {
        if (!adj[1, 0])
        {
            if (subTile.y == 0)
                return new Vec2I(0, 0);
            else if (ttype == TerrainStandard.TileType.CornerTL)
                return new Vec2I(1, 0);
            else if (ttype == TerrainStandard.TileType.SideT)
                return new Vec2I(2, 0);
            else if (ttype == TerrainStandard.TileType.CornerTR)
                return new Vec2I(3, 0);
            else if (ttype == TerrainStandard.TileType.SideL)
                ttype = TerrainStandard.TileType.CornerBL;
            else if (ttype == TerrainStandard.TileType.SideR)
                ttype = TerrainStandard.TileType.CornerBR;
            else if (ttype == TerrainStandard.TileType.Base)
                ttype = TerrainStandard.TileType.SideB;
            else if (ttype == TerrainStandard.TileType.ConcaveTR)
                return new Vec2I(5, 0);
            else if (ttype == TerrainStandard.TileType.ConcaveTL)
                return new Vec2I(4, 0);
        }
        else if (!adj[0, 0] || !adj[2, 0])
        {
            if (ttype == TerrainStandard.TileType.ConcaveBL)
                ttype = TerrainStandard.TileType.SideL;
            else if (ttype == TerrainStandard.TileType.ConcaveBR)
                ttype = TerrainStandard.TileType.SideR;
            else if (ttype == TerrainStandard.TileType.SideT && !adj[0, 0] && subTile.x == 0)
                return new Vec2I(6, 0);
            else if (ttype == TerrainStandard.TileType.SideT && !adj[2, 0] && subTile.x == 1)
                return new Vec2I(7, 0);
            else if (ttype == TerrainStandard.TileType.ConcaveTL && !adj[0, 0])
                return new Vec2I(8, 0);
            else if (ttype == TerrainStandard.TileType.ConcaveTR && !adj[2, 0])
                return new Vec2I(9, 0);
        }

        return TerrainStandard.SpriteOffset(ttype) + new Vec2I(0, 1);
    }

    public override Sprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile)
    {
        bool[,] adj = TerrainStandard.GenAdjData(pos, p => IsSame(tiler.map, p));
        TerrainStandard.TileType ttype = TerrainStandard.GetTileType(adj, subTile);
        Vec2I spritePos = WallOrigin(wall) + SpriteOffset(adj, ttype, subTile);
        return tiler.atlas32.GetSprite(spritePos, Vec2I.one, 64);
    }
}

public class BuildingResource : Building
{
    public enum Resource { Rock, Tree }
    private readonly Resource resource;

    public bool passable => false;
    public bool tiledRender => false;
    public bool mineable => true;
    public bool oversized => resource == Resource.Tree;

    public BuildingResource(Resource resource) => this.resource = resource;

    public Sprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile)
    {
        BB.Assert(subTile == Vec2I.zero);

        _Atlas atlas;
        Vec2I spritePos;
        Vec2I spriteSize;
        float ppu = 32;

        switch (resource)
        {
            case Resource.Rock:
                atlas = tiler.atlas32;
                spritePos = new Vec2I(0, 2);
                spriteSize = Vec2I.one;
                break;
            case Resource.Tree:
                atlas = tiler.atlas;
                spritePos = new Vec2I(1, 2);
                spriteSize = new Vec2I(2, 2);
                break;
            default:
                throw new NotImplementedException("Unknown Resource: " + resource);
        }

        return atlas.GetSprite(spritePos, spriteSize, ppu);
    }

    public Sprite GetSpriteOver(MapTiler tiler, Vec2I pos)
    {
        BB.Assert(oversized);

        switch (resource)
        {
            case Resource.Tree:
                return tiler.atlas.GetSprite(new Vec2I(0, 4), new Vec2I(4, 5), new Vec2I(1, -2), 32);
            default:
                throw new NotImplementedException("Unhandled Resource: " + resource);
        }
    }
}