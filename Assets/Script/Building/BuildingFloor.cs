using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;
using System;
using System.Collections.Generic;

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
        var ttype = Tiling.GetTileType(pos, subTile, p => IsSame(map, p));
        Vec2I spritePos = SpriteOrigin() + Tiling.SpriteOffset(ttype);
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