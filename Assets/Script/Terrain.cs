using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;
using System;

public struct Terrain
{
    // TODO: do away with this enum
    public enum TerrainType { Grass, Mud, Dirt, Path, Water }
    public readonly TerrainType type;

    public Terrain(TerrainType type) => this.type = type;

    public bool passable => type != TerrainType.Water;
    public bool animated => type == TerrainType.Water;

    private static Vec2I TerrainOrigin(TerrainType type, int frame)
    {
        if (type == TerrainType.Water)
        {
            BB.Assert(frame >= 0 && frame < 8);
            return new Vec2I(26, 29) - new Vec2I(0, 3 * frame);
        }

        BB.Assert(frame == 0);
        switch (type)
        {
            case TerrainType.Dirt: return new Vec2I(0, 0);
            case TerrainType.Grass: return new Vec2I(0, 29);
            case TerrainType.Mud: return new Vec2I(0, 26);
            case TerrainType.Path: return new Vec2I(0, 23);
        }

        throw new Exception("Unknown Terrain: " + type);
    }

    private static bool IsSame(Map map, Vec2I pos, TerrainType type)
        => map.ValidTile(pos) ? (type == map.Tile(pos).terrain.type) : true;

    // Kludge for terrain base which we should get rid of
    public static Sprite GetSprite(AssetSrc assets, TerrainType type, Tiling.TileType ttype, int frame)
    {
        var spritePos = TerrainOrigin(type, frame) + Tiling.SpriteOffset(ttype);
        return assets.tileset32.GetSprite(spritePos, Vec2I.one);
    }

    public Sprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
        => GetSprite(map, pos, subTile, 0);

    private Sprite GetSprite(Map map, Vec2I pos, Vec2I subTile, int frame)
    {
        BB.Assert(frame == 0 || animated);
        if (type == TerrainType.Grass)
            return null;

        TerrainType lambdaType = type; // C# is dumb
        Tiling.TileType ttype = Tiling.GetTileType(pos, subTile, p => IsSame(map, p, lambdaType));

        // KLUDGE for water arrangement in atlas
        if (type == TerrainType.Water)
        {
            switch (ttype)
            {
                case Tiling.TileType.ConcaveBL: ttype = Tiling.TileType.ConcaveTR; break;
                case Tiling.TileType.ConcaveTR: ttype = Tiling.TileType.ConcaveBL; break;
                case Tiling.TileType.ConcaveBR: ttype = Tiling.TileType.ConcaveTL; break;
                case Tiling.TileType.ConcaveTL: ttype = Tiling.TileType.ConcaveBR; break;
            }
        }

        return GetSprite(map.game.assets, type, ttype, frame);
    }

    public Sprite[] GetAnimationSprites(Map map, Vec2I pos, Vec2I subTile)
    {
        BB.Assert(type == TerrainType.Water);

        var sprites = new Sprite[8];
        for (int i = 0; i < 8; ++i)
            sprites[i] = GetSprite(map, pos, subTile, i);
        return sprites;
    }
};


