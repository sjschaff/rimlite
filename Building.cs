using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;
using System;
using System.Collections.Generic;

public interface Building
{
    TileSprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile);
    TileSprite GetSpriteOver(MapTiler tiler, Vec2I pos);

    bool passable { get; }
    bool tiledRender { get; }
    bool oversized { get; }
    bool mineable { get; }
    Tool miningTool { get; }

    IEnumerable<ItemInfo> GetBuildMaterials();
    IEnumerable<ItemInfo> GetMinedMaterials();
}

public abstract class BuildingSmp : Building
{
    public TileSprite GetSpriteOver(MapTiler tiler, Vec2I pos)
        => throw new Exception("GetSpriteOver called on BuildingSmp");
    public bool oversized => false;
    public bool mineable => false;
    public Tool miningTool =>
        throw new NotSupportedException("miningTool called on BuildingSmp");
    public IEnumerable<ItemInfo> GetMinedMaterials() =>
        throw new NotSupportedException("GetMinedMaterials called on BuildingSmp");

    public abstract TileSprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile);
    public abstract bool passable { get; }
    public abstract bool tiledRender { get; }
    public abstract IEnumerable<ItemInfo> GetBuildMaterials();
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

        var other = map.Tile(pos).BuildingAs<BuildingFloor>();
        return other == null ? false : other.floor == floor;
    }

    public override TileSprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile)
    {
        TerrainStandard.TileType ttype = TerrainStandard.GetTileType(pos, subTile, p => IsSame(tiler.map, p));
        Vec2I spritePos = FloorOrigin(floor) + TerrainStandard.SpriteOffset(ttype);
        return tiler.tileset64.GetSprite(spritePos, Vec2I.one);
    }

    public override IEnumerable<ItemInfo> GetBuildMaterials()
    {
        yield return new ItemInfo(ItemType.Stone, 5);
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

        var other = map.Tile(pos).BuildingAs<BuildingWall>();
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

    public override TileSprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile)
    {
        bool[,] adj = TerrainStandard.GenAdjData(pos, p => IsSame(tiler.map, p));
        TerrainStandard.TileType ttype = TerrainStandard.GetTileType(adj, subTile);
        Vec2I spritePos = WallOrigin(wall) + SpriteOffset(adj, ttype, subTile);
        return tiler.tileset64.GetSprite(spritePos, Vec2I.one);
    }

    public override IEnumerable<ItemInfo> GetBuildMaterials()
    {
        yield return new ItemInfo(ItemType.Stone, 10);
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

    public Tool miningTool
    {
        get
        {
            switch (resource)
            {
                case Resource.Rock: return Tool.Pickaxe;
                case Resource.Tree: return Tool.Axe;
                default: throw new NotImplementedException("Unkown resource: " + resource);
            }
        }
    }

    public BuildingResource(Resource resource) => this.resource = resource;

    public TileSprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile)
    {
        BB.Assert(subTile == Vec2I.zero);

        Atlas atlas;
        Vec2I spritePos;
        Vec2I spriteSize;

        switch (resource)
        {
            case Resource.Rock:
                atlas = tiler.sprites32;
                spritePos = new Vec2I(0, 18);
                spriteSize = new Vec2I(4, 4);
                break;
            case Resource.Tree:
                atlas = tiler.sprites32;
                spritePos = new Vec2I(2, 4);
                spriteSize = new Vec2I(4, 4);
                break;
            default:
                throw new NotImplementedException("Unknown Resource: " + resource);
        }

        return atlas.GetSprite(spritePos, spriteSize);
    }

    public TileSprite GetSpriteOver(MapTiler tiler, Vec2I pos)
    {
        BB.Assert(oversized);

        switch (resource)
        {
            case Resource.Tree:
                return tiler.sprites32.GetSprite(new Vec2I(0, 8), new Vec2I(8, 10), new Vec2I(2, -4));
            default:
                throw new NotImplementedException("Unhandled Resource: " + resource);
        }
    }

    public IEnumerable<ItemInfo> GetBuildMaterials()
        => throw new NotSupportedException("GetBuildMaterials called on BuildingResource");

    public IEnumerable<ItemInfo> GetMinedMaterials()
    {
        switch (resource)
        {
            case Resource.Rock: yield return new ItemInfo(ItemType.Stone, 36); break;
            case Resource.Tree: yield return new ItemInfo(ItemType.Wood, 25); break;
            default: throw new NotImplementedException("Unknown resource: " + resource);
        }
    }
}

public class BuildingVirtual : Building
{
    private readonly JobBuild job;
    public Building building { get; private set; }
    public float constructionPercent;
    public bool beganConstruction => constructionPercent > 0;
    //private items?

    public BuildingVirtual(JobBuild job, Building building)
    {
        this.job = job;
        this.building = building;
        this.constructionPercent = 0;
    }

    public bool mineable => false;
    public bool tiledRender => building.tiledRender;
    public bool oversized => building.oversized;
    public bool passable => beganConstruction ? building.passable : true;
    public Tool miningTool => throw new NotImplementedException("Mining tool requested for virtual building");

    private TileSprite Virtualize(TileSprite sprite)
        => new TileSprite(sprite.sprite, sprite.color* new Color(.6f, .6f, 1, .5f));

    public TileSprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile)
        => Virtualize(building.GetSprite(tiler, pos, subTile));

    public TileSprite GetSpriteOver(MapTiler tiler, Vec2I pos)
        => Virtualize(building.GetSpriteOver(tiler, pos));

    public IEnumerable<ItemInfo> GetBuildMaterials()
        => throw new NotSupportedException("GetBuildMaterials called on BuildingVirtual");
    public IEnumerable<ItemInfo> GetMinedMaterials()
        => throw new NotSupportedException("GetMinedMaterials called on BuildingVirtual");
}