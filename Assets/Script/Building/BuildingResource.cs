using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;
using System;
using System.Collections.Generic;

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