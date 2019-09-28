using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;
using System;
using System.Collections.Generic;

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
    private Vec2I SpriteOffset(bool[,] adj, Tiling.TileType ttype, Vec2I subTile)
    {
        if (!adj[1, 0])
        {
            if (subTile.y == 0)
                return new Vec2I(0, 0);
            else if (ttype == Tiling.TileType.CornerTL)
                return new Vec2I(1, 0);
            else if (ttype == Tiling.TileType.SideT)
                return new Vec2I(2, 0);
            else if (ttype == Tiling.TileType.CornerTR)
                return new Vec2I(3, 0);
            else if (ttype == Tiling.TileType.SideL)
                ttype = Tiling.TileType.CornerBL;
            else if (ttype == Tiling.TileType.SideR)
                ttype = Tiling.TileType.CornerBR;
            else if (ttype == Tiling.TileType.Base)
                ttype = Tiling.TileType.SideB;
            else if (ttype == Tiling.TileType.ConcaveTR)
                return new Vec2I(5, 0);
            else if (ttype == Tiling.TileType.ConcaveTL)
                return new Vec2I(4, 0);
        }
        else if (!adj[0, 0] || !adj[2, 0])
        {
            if (ttype == Tiling.TileType.ConcaveBL)
                ttype = Tiling.TileType.SideL;
            else if (ttype == Tiling.TileType.ConcaveBR)
                ttype = Tiling.TileType.SideR;
            else if (ttype == Tiling.TileType.SideT && !adj[0, 0] && subTile.x == 0)
                return new Vec2I(6, 0);
            else if (ttype == Tiling.TileType.SideT && !adj[2, 0] && subTile.x == 1)
                return new Vec2I(7, 0);
            else if (ttype == Tiling.TileType.ConcaveTL && !adj[0, 0])
                return new Vec2I(8, 0);
            else if (ttype == Tiling.TileType.ConcaveTR && !adj[2, 0])
                return new Vec2I(9, 0);
        }

        return Tiling.SpriteOffset(ttype) + new Vec2I(0, 1);
    }

    public override TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
    {
        bool[,] adj = Tiling.GenAdjData(pos, p => IsSame(map, p));
        Tiling.TileType ttype = Tiling.GetTileType(adj, subTile);
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