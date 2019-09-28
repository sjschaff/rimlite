using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;
using System;

public struct Terrain
{
    // TODO: Kludge
    public static TerrainDef K_grassDef;
    public static TerrainDef K_waterDef;

    public readonly TerrainDef def;
    public readonly bool passable;
    public readonly bool animated;

    private readonly Atlas atlas;

    public Terrain(GameController game, TerrainDef def)
    {
        if (K_grassDef == null)
        {
            K_grassDef = game.defs.Get<TerrainDef>("BB:Grass");
            K_waterDef = game.defs.Get<TerrainDef>("BB:Water");
        }

        this.def = def;
        passable = def.passable;
        animated = def.spriteFrames.Length > 1;
        atlas = game.assets.atlases.Get(def.atlas);
    }

    private static bool IsSame(Map map, Vec2I pos, TerrainDef def)
        => map.ValidTile(pos) ? (def == map.Tile(pos).terrain.def) : true;

    private Sprite GetSprite(Map map, Vec2I pos, Vec2I subTile, int frame)
    {
        BB.Assert(frame < def.spriteFrames.Length);

        TerrainDef lambdaDef = def; // C# is dumb
        Tiling.TileType ttype = Tiling.GetTileType(pos, subTile, p => IsSame(map, p, lambdaDef));

        // TODO: Kludge for water arrangement in atlas
        if (def == K_waterDef)
        {
            switch (ttype)
            {
                case Tiling.TileType.ConcaveBL: ttype = Tiling.TileType.ConcaveTR; break;
                case Tiling.TileType.ConcaveTR: ttype = Tiling.TileType.ConcaveBL; break;
                case Tiling.TileType.ConcaveBR: ttype = Tiling.TileType.ConcaveTL; break;
                case Tiling.TileType.ConcaveTL: ttype = Tiling.TileType.ConcaveBR; break;
            }
        }

        var spritePos = def.spriteFrames[frame] + Tiling.SpriteOffset(ttype);
        return atlas.GetSprite(spritePos, Vec2I.one);
    }
    public Sprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
        => GetSprite(map, pos, subTile, 0);

    public Sprite[] GetAnimationSprites(Map map, Vec2I pos, Vec2I subTile)
    {
        BB.Assert(animated);

        var sprites = new Sprite[def.spriteFrames.Length];
        for (int i = 0; i < def.spriteFrames.Length; ++i)
            sprites[i] = GetSprite(map, pos, subTile, i);
        return sprites;
    }
};


