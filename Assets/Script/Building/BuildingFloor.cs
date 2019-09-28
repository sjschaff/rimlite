﻿using System.Collections.Generic;
using System;

using Vec2I = UnityEngine.Vector2Int;

public class BuildingProtoFloor : BuildingProtoTiledRender
{
    public readonly BldgFloorDef def;

    public BuildingProtoFloor(BldgFloorDef def) => this.def = def;

    public override IBuilding CreateBuilding() => new BuildingFloor(this);

    public override bool passable => true;
    public override bool K_mineable => false;
    public override Tool K_miningTool => throw new NotSupportedException("miningTool called on BuildingProtoFloor");
    public override IEnumerable<ItemInfo> K_GetMinedMaterials() { yield break; }

    private bool IsSame(Map map, Vec2I pos)
    {
        if (GetSame<BuildingProtoFloor>(map, pos, out var protoOther))
            return protoOther == this;
        return false;
    }

    public override TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
    {
        var ttype = Tiling.GetTileType(pos, subTile, p => IsSame(map, p));
        Vec2I spritePos = def.spriteOrigin + Tiling.SpriteOffset(ttype);
        return map.game.assets.atlases.Get(def.atlas).GetSprite(spritePos, Vec2I.one);
    }

    public override IEnumerable<ItemInfo> GetBuildMaterials()
    {
        foreach (var item in def.materials)
            yield return item;
    }

    private class BuildingFloor : BuildingBase<BuildingProtoFloor>
    {
        public BuildingFloor(BuildingProtoFloor proto) : base(proto) { }
    }
}