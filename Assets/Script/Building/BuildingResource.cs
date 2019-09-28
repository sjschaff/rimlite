using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;
using System;
using System.Collections.Generic;

public class BuildingProtoResource : IBuildingProto
{
    private readonly BldgMineableDef def;

    public BuildingProtoResource(BldgMineableDef def) => this.def = def;

    public IBuilding CreateBuilding() => new BuildingResource(this);

    public bool passable => false;
    public bool K_mineable => true;
    public Tool K_miningTool => def.tool;

    public BuildingBounds bounds => BuildingBounds.Unit;

    public RenderFlags renderFlags =>
        def.spriteOver == null ? RenderFlags.None : RenderFlags.Oversized;

    public TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
        => map.game.assets.sprites.Get(def.sprite);

    public TileSprite GetSpriteOver(Map map, Vec2I pos)
        => map.game.assets.sprites.Get(def.spriteOver);

    public IEnumerable<ItemInfo> GetBuildMaterials()
        => throw new NotSupportedException("GetBuildMaterials called on BuildingResource");

    public IEnumerable<ItemInfo> K_GetMinedMaterials()
    {
        foreach (var item in def.resources)
            yield return item;
    }

    public class BuildingResource : BuildingBase<BuildingProtoResource>
    {
        float minedAmt; // or some such thing....

        public BuildingResource(BuildingProtoResource proto) : base(proto) => minedAmt = 0;
    }
}