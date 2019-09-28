using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;
using System;
using System.Collections.Generic;

public struct BuildingBounds
{
    public readonly Vec2I size;
    public readonly Vec2I origin;

    public BuildingBounds(Vec2I size, Vec2I origin)
    {
        this.size = size;
        this.origin = origin;
    }

    public static readonly BuildingBounds Unit = new BuildingBounds(Vec2I.one, Vec2I.zero);

    public static bool operator ==(BuildingBounds a, BuildingBounds b)
        => a.size == b.size && a.origin == b.origin;

    public static bool operator !=(BuildingBounds a, BuildingBounds b) => !(a == b);

    public override bool Equals(object obj)
    {
        return obj is BuildingBounds bounds && bounds == this;
    }

    public override int GetHashCode()
    {
        var hashCode = 1845097995;
        hashCode = hashCode * -1521134295 + EqualityComparer<Vec2I>.Default.GetHashCode(size);
        hashCode = hashCode * -1521134295 + EqualityComparer<Vec2I>.Default.GetHashCode(origin);
        return hashCode;
    }
}

public enum RenderFlags
{
    None = 0,
    Tiled = 1,
    Oversized = 2,
}

public interface IBuilding
{
    IBuildingProto prototype { get; }

    bool passable { get; }
    bool K_mineable { get; }
    Tool miningTool { get; }
    IEnumerable<ItemInfo> GetMinedMaterials();

    BuildingBounds bounds { get; }
    RenderFlags renderFlags { get; }
    TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile);
    TileSprite GetSpriteOver(Map map, Vec2I pos);
}

public static class BuildingExt
{
    public static bool TiledRender(this IBuilding bldg)
        => (bldg.renderFlags & RenderFlags.Tiled) != 0;
    public static bool Oversized(this IBuilding bldg)
        => (bldg.renderFlags & RenderFlags.Oversized) != 0;

    public static IEnumerable<Vec2I> AllTiles(this IBuilding bldg, Vec2I pos)
    {
        var bounds = bldg.bounds;
        return new RectInt(pos - bounds.origin, bounds.size).AllTiles();
    }
}

public abstract class BuildingBase<TProto> : IBuilding where TProto : IBuildingProto
{
    protected readonly TProto proto;
    public IBuildingProto prototype => proto;
    protected BuildingBase(TProto proto) => this.proto = proto;

    public virtual bool passable => proto.passable;
    public virtual bool K_mineable => proto.K_mineable;
    public virtual Tool miningTool => proto.miningTool;
    public virtual IEnumerable<ItemInfo> GetMinedMaterials() => proto.GetMinedMaterials();

    public virtual BuildingBounds bounds => proto.bounds;
    public virtual RenderFlags renderFlags => proto.renderFlags;
    public virtual TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
        => proto.GetSprite(map, pos, subTile);
    public virtual TileSprite GetSpriteOver(Map map, Vec2I pos)
        => proto.GetSpriteOver(map, pos);
}

public interface IBuildingProto
{
    IBuilding CreateBuilding();
    IEnumerable<ItemInfo> GetBuildMaterials();

    bool passable { get; }
    bool K_mineable { get; }
    Tool miningTool { get; }
    IEnumerable<ItemInfo> GetMinedMaterials();

    BuildingBounds bounds { get; }
    RenderFlags renderFlags { get; }
    TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile);
    TileSprite GetSpriteOver(Map map, Vec2I pos);
}

public abstract class BuildingProtoTiledRender : IBuildingProto
{
    protected BuildingProtoTiledRender() { }

    public abstract IBuilding CreateBuilding();
    public abstract IEnumerable<ItemInfo> GetBuildMaterials();
    public abstract bool passable { get; }
    public abstract bool K_mineable { get; }
    public abstract Tool miningTool { get; }
    public abstract TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile);

    public BuildingBounds bounds => BuildingBounds.Unit;
    public RenderFlags renderFlags => RenderFlags.Tiled;

    public TileSprite GetSpriteOver(Map map, Vec2I pos)
        => throw new NotSupportedException("GetSpriteOver called on BuildingProtoTiledRender.");

    // TODO: this is a terrible name
    protected bool GetSame<TThis>(Map map, Vec2I pos, out TThis proto) where TThis : BuildingProtoTiledRender
    {
        proto = null;
        if (!map.ValidTile(pos))
            return false;

        var bldgOther = map.Tile(pos).building;
        if (bldgOther is BuildingProtoConstruction.BuildingConstruction bldgConstruction)
            proto = bldgConstruction.job.prototype as TThis;
        else
            proto = bldgOther?.prototype as TThis;

        return proto != null;
    }

    public abstract IEnumerable<ItemInfo> GetMinedMaterials();
}

public class BuildingProtoFloor : BuildingProtoTiledRender
{
    public static BuildingProtoFloor K_Stone = new BuildingProtoFloor(Floor.StoneBrick);

    public enum Floor { StoneBrick } // TODO: get rid of this enum
    public readonly Floor floor;

    public BuildingProtoFloor(Floor floor) => this.floor = floor;

    public override IBuilding CreateBuilding() => new BuildingFloor(this);

    public override bool passable => true;
    public override bool K_mineable => false;
    public override Tool miningTool => throw new NotSupportedException("miningTool called on BuildingProtoFloor");
    public override IEnumerable<ItemInfo> GetMinedMaterials() { yield break; }

    private Vec2I SpriteOrigin()
    {
        switch (floor)
        {
            case Floor.StoneBrick: return new Vec2I(0, 9);
            default:
                throw new NotImplementedException("Unknown Floor: " + floor);
        }
    }

    private bool IsSame(Map map, Vec2I pos)
    {
        if (GetSame<BuildingProtoFloor>(map, pos, out var protoOther))
            return protoOther == this;
        return false;
    }

    public override TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
    {
        var ttype = TerrainStandard.GetTileType(pos, subTile, p => IsSame(map, p));
        Vec2I spritePos = SpriteOrigin() + TerrainStandard.SpriteOffset(ttype);
        return map.game.assets.tileset64.GetSprite(spritePos, Vec2I.one);
    }

    public override IEnumerable<ItemInfo> GetBuildMaterials()
    {
        yield return new ItemInfo(ItemType.Stone, 5);
    }

    private class BuildingFloor : BuildingBase<BuildingProtoFloor>
    {
        public BuildingFloor(BuildingProtoFloor proto) : base(proto) { }
    }
}

public class BuildingProtoWall : BuildingProtoTiledRender
{
    public static BuildingProtoWall K_Stone = new BuildingProtoWall(Wall.StoneBrick);

    // TODO: get rid of this enum
    public enum Wall { StoneBrick }
    private readonly Wall wall;

    public BuildingProtoWall(Wall wall) => this.wall = wall;

    public override IBuilding CreateBuilding() => new BuildingWall(this);

    public override bool passable => false;
    public override bool K_mineable => false;
    public override Tool miningTool => throw new NotSupportedException("miningTool called on BuildingWall");
    public override IEnumerable<ItemInfo> GetMinedMaterials() { yield break; }

    private Vec2I SpriteOrigin()
    {
        switch (wall)
        {
            case Wall.StoneBrick: return new Vec2I(0, 12);
        }

        throw new NotImplementedException("Unknown Building: " + wall);
    }

    private bool IsSame(Map map, Vec2I pos) => GetSame<BuildingProtoWall>(map, pos, out _);

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

    public override TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
    {
        bool[,] adj = TerrainStandard.GenAdjData(pos, p => IsSame(map, p));
        TerrainStandard.TileType ttype = TerrainStandard.GetTileType(adj, subTile);
        Vec2I spritePos = SpriteOrigin() + SpriteOffset(adj, ttype, subTile);
        return map.game.assets.tileset64.GetSprite(spritePos, Vec2I.one);
    }

    public override IEnumerable<ItemInfo> GetBuildMaterials()
    {
        yield return new ItemInfo(ItemType.Stone, 10);
    }

    private class BuildingWall : BuildingBase<BuildingProtoWall>
    {
        public BuildingWall(BuildingProtoWall proto) : base(proto) { }
    }
}

public class BuildingProtoResource : IBuildingProto
{
    public static readonly BuildingProtoResource K_Rock = new BuildingProtoResource(Resource.Rock);
    public static readonly BuildingProtoResource K_Tree = new BuildingProtoResource(Resource.Tree);

    public enum Resource { Rock, Tree }
    private readonly Resource resource;

    public BuildingProtoResource(Resource resource) => this.resource = resource;

    public IBuilding CreateBuilding() => new BuildingResource(this);

    public bool passable => false;
    public bool K_mineable => true;
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

    public BuildingBounds bounds => BuildingBounds.Unit;

    public RenderFlags renderFlags => resource == Resource.Tree ? RenderFlags.Oversized : RenderFlags.None;

    public TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
    {
        BB.Assert(subTile == Vec2I.zero);

        Atlas atlas;
        Vec2I spritePos;
        Vec2I spriteSize;

        switch (resource)
        {
            case Resource.Rock:
                atlas = map.game.assets.sprites32;
                spritePos = new Vec2I(0, 18);
                spriteSize = new Vec2I(4, 4);
                break;
            case Resource.Tree:
                atlas = map.game.assets.sprites32;
                spritePos = new Vec2I(2, 4);
                spriteSize = new Vec2I(4, 4);
                break;
            default:
                throw new NotImplementedException("Unknown Resource: " + resource);
        }

        return atlas.GetSprite(spritePos, spriteSize);
    }

    public TileSprite GetSpriteOver(Map map, Vec2I pos)
    {
        BB.Assert((renderFlags & RenderFlags.Oversized) != 0);

        switch (resource)
        {
            case Resource.Tree:
                return map.game.assets.sprites32.GetSprite(new Vec2I(0, 8), new Vec2I(8, 10), new Vec2I(2, -4));
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

    public class BuildingResource : BuildingBase<BuildingProtoResource>
    {
        float minedAmt; // or some such thing....

        public BuildingResource(BuildingProtoResource proto) : base(proto) => minedAmt = 0;
    }
}

public class BuildingProtoConstruction : IBuildingProto
{
    public static BuildingProtoConstruction K_single = new BuildingProtoConstruction();

    public BuildingProtoConstruction() { }

    public BuildingConstruction Create(JobBuild job)
        => new BuildingConstruction(this, job);

    public class BuildingConstruction : BuildingBase<BuildingProtoConstruction>
    {
        public JobBuild job;
        public bool constructionBegan;
        public float constructionPercent; // or some such thing...

        public BuildingConstruction(BuildingProtoConstruction proto, JobBuild job) : base(proto)
        {
            this.job = job;
            constructionBegan = false;
            constructionPercent = 0;
        }

        public override bool passable => true;
        public override bool K_mineable => false;

        public override BuildingBounds bounds => job.prototype.bounds;
        public override RenderFlags renderFlags => job.prototype.renderFlags;

        private TileSprite Virtualize(TileSprite sprite)
            => new TileSprite(sprite.sprite, sprite.color * new Color(.6f, .6f, 1, .5f));

        public override TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
            => Virtualize(job.prototype.GetSprite(map, pos, subTile));

        public override TileSprite GetSpriteOver(Map map, Vec2I pos)
            => Virtualize(job.prototype.GetSpriteOver(map, pos));
    }

    public IBuilding CreateBuilding()
        => throw new NotSupportedException("CreateBuilding called on BuildingProtoConstruction");
    public IEnumerable<ItemInfo> GetBuildMaterials()
        => throw new NotSupportedException("GetBuildMaterials called on BuildingProtoConstruction");
    public bool passable
        => throw new NotSupportedException("passable called on BuildingProtoConstruction");
    public bool K_mineable
        => throw new NotSupportedException("mineable called on BuildingProtoConstruction");
    public Tool miningTool
        => throw new NotSupportedException("miningTool called on BuildingProtoConstruction");
    public IEnumerable<ItemInfo> GetMinedMaterials()
        => throw new NotSupportedException("GetMinedMaterials called on BuildingProtoConstruction");
    public BuildingBounds bounds
        => throw new NotSupportedException("bounds called on BuildingProtoConstruction");
    public RenderFlags renderFlags
        => throw new NotSupportedException("renderFlags called on BuildingProtoConstruction");
    public TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
        => throw new NotSupportedException("GetSprite called on BuildingProtoConstruction");
    public TileSprite GetSpriteOver(Map map, Vec2I pos)
        => throw new NotSupportedException("GetSpriteOver called on BuildingProtoConstruction");
}