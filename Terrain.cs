using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;
using System;


public interface Terrain
{
    bool animated { get; }
    Sprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile);
    Sprite[] GetAnimationSprites(MapTiler tiler, Vec2I pos, Vec2I subTile);
    bool passable { get; }
};

public class TerrainStandard : Terrain
{
    // TODO: do away with this enum
    public enum Terrain { Grass, Mud, Dirt, Path, Water }
    public readonly Terrain terrain;

    public TerrainStandard(Terrain terrain) => this.terrain = terrain;

    // BEGIN SHARED ----------------------------------------------------------------------
    public enum TileType
    {
        Unique,
        CornerBL, CornerBR, CornerTR, CornerTL,
        ConcaveBL, ConcaveBR, ConcaveTR, ConcaveTL,
        SideL, SideR, SideB, SideT,
        Base, BaseVariant1, BaseVariant2, BaseVariant3,
    }

    public enum TileTypeA { Corner, Concave, SideA, SideB, Base }

    public static bool[,] GenAdjData(Vec2I pos, Func<Vec2I, bool> IsSame)
    {
        bool[,] adj = new bool[3, 3];
        for (int ax = 0; ax < 3; ++ax)
            for (int ay = 0; ay < 3; ++ay)
                adj[ax, ay] = IsSame(new Vec2I(pos.x + ax - 1, pos.y + ay - 1));

        return adj;
    }

    // Calculates abstract tile type where sideB is 90deg CW from side A
    private static TileTypeA GetTileTypeA(bool sideA, bool corner, bool sideB)
    {
        if (sideA && sideB && corner)
            return TileTypeA.Base;
        else if (sideA && sideB)
            return TileTypeA.Concave;
        else if (sideA)
            return TileTypeA.SideB;
        else if (sideB)
            return TileTypeA.SideA;
        else
            return TileTypeA.Corner;
    }

    public static TileType GetTileType(bool[,] adj, Vec2I subTile)
    {
        if (subTile.x == 0)
        {
            if (subTile.y == 0)
            {
                var ttypeA = GetTileTypeA(adj[1, 0], adj[0, 0], adj[0, 1]);
                switch (ttypeA)
                {
                    case TileTypeA.Base: return TileType.Base;
                    case TileTypeA.Corner: return TileType.CornerBL;
                    case TileTypeA.Concave: return TileType.ConcaveBL;
                    case TileTypeA.SideA: return TileType.SideB;
                    case TileTypeA.SideB: return TileType.SideL;
                }
            }
            else
            {
                var ttypeA = GetTileTypeA(adj[0, 1], adj[0, 2], adj[1, 2]);
                switch (ttypeA)
                {
                    case TileTypeA.Base: return TileType.Base;
                    case TileTypeA.Corner: return TileType.CornerTL;
                    case TileTypeA.Concave: return TileType.ConcaveTL;
                    case TileTypeA.SideA: return TileType.SideL;
                    case TileTypeA.SideB: return TileType.SideT;
                }
            }
        }
        else
        {
            if (subTile.y == 0)
            {
                var ttypeA = GetTileTypeA(adj[2, 1], adj[2, 0], adj[1, 0]);
                switch (ttypeA)
                {
                    case TileTypeA.Base: return TileType.Base;
                    case TileTypeA.Corner: return TileType.CornerBR;
                    case TileTypeA.Concave: return TileType.ConcaveBR;
                    case TileTypeA.SideA: return TileType.SideR;
                    case TileTypeA.SideB: return TileType.SideB;
                }
            }
            else
            {
                var ttypeA = GetTileTypeA(adj[1, 2], adj[2, 2], adj[2, 1]);
                switch (ttypeA)
                {
                    case TileTypeA.Base: return TileType.Base;
                    case TileTypeA.Corner: return TileType.CornerTR;
                    case TileTypeA.Concave: return TileType.ConcaveTR;
                    case TileTypeA.SideA: return TileType.SideT;
                    case TileTypeA.SideB: return TileType.SideR;
                }
            }
        }
        throw new Exception("Failed To compute tile type");
    }

    public static TileType GetTileType(Vec2I pos, Vec2I subTile, Func<Vec2I, bool> IsSame)
        => GetTileType(GenAdjData(pos, IsSame), subTile);

    public static Vec2I SpriteOffset(TileType ttype)
    {
        switch (ttype)
        {
            case TileType.CornerBL: return new Vec2I(0, 0);
            case TileType.CornerBR: return new Vec2I(2, 0);
            case TileType.CornerTR: return new Vec2I(2, 2);
            case TileType.CornerTL: return new Vec2I(0, 2);
            case TileType.ConcaveBL: return new Vec2I(3, 0);
            case TileType.ConcaveBR: return new Vec2I(5, 0);
            case TileType.ConcaveTR: return new Vec2I(5, 2);
            case TileType.ConcaveTL: return new Vec2I(3, 2);
            case TileType.SideL: return new Vec2I(0, 1);
            case TileType.SideR: return new Vec2I(2, 1);
            case TileType.SideB: return new Vec2I(1, 0);
            case TileType.SideT: return new Vec2I(1, 2);
            case TileType.Base: return new Vec2I(1, 1);
            case TileType.BaseVariant1: return new Vec2I(6, 2);
            case TileType.BaseVariant2: return new Vec2I(6, 1);
            case TileType.BaseVariant3: return new Vec2I(6, 0);
        }

        throw new Exception("unkown ttype: " + ttype);
    }
    // END SHARED -------------------------------------------------------------------------

    private static Vec2I TerrainOrigin(Terrain terrain, int frame)
    {
        if (terrain == Terrain.Water)
        {
            BB.Assert(frame >= 0 && frame < 8);
            return new Vec2I(26, 29) - new Vec2I(0, 3 * frame);
        }

        BB.Assert(frame == 0);
        switch (terrain)
        {
            case Terrain.Dirt: return new Vec2I(0, 0);
            case Terrain.Grass: return new Vec2I(0, 29);
            case Terrain.Mud: return new Vec2I(0, 26);
            case Terrain.Path: return new Vec2I(0, 23);
        }

        throw new Exception("Unknown Terrain: " + terrain);
    }

    private bool IsSame(Map map, Vec2I pos)
    {
        if (!map.ValidTile(pos))
            return false;

        var other = map.Tile(pos).terrain as TerrainStandard;
        return other == null ? false : other.terrain == terrain;
    }

    public static Sprite GetSprite(MapTiler tiler, Terrain terrain, TileType ttype, int frame)
    {
        var spritePos = TerrainOrigin(terrain, frame) + SpriteOffset(ttype);
        return tiler.atlas.GetSprite(spritePos, Vec2I.one, 32);
    }

    public Sprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile)
        => GetSprite(tiler, pos, subTile, 0);

    private Sprite GetSprite(MapTiler tiler, Vec2I pos, Vec2I subTile, int frame)
    {
        BB.Assert(frame == 0 || animated);
        if (terrain == Terrain.Grass)
            return null;

        TileType ttype = GetTileType(pos, subTile, p => IsSame(tiler.map, p));

        // KLUDGE for water arrangement in atlas
        if (terrain == Terrain.Water)
        {
            switch (ttype)
            {
                case TileType.ConcaveBL: ttype = TileType.ConcaveTR; break;
                case TileType.ConcaveTR: ttype = TileType.ConcaveBL; break;
                case TileType.ConcaveBR: ttype = TileType.ConcaveTL; break;
                case TileType.ConcaveTL: ttype = TileType.ConcaveBR; break;
            }
        }

        return GetSprite(tiler, terrain, ttype, 0);
    }

    public Sprite[] GetAnimationSprites(MapTiler tiler, Vec2I pos, Vec2I subTile)
    {
        BB.Assert(terrain == Terrain.Water);

        var sprites = new Sprite[8];
        for (int i = 0; i < 8; ++i)
            sprites[i] = GetSprite(tiler, pos, subTile, i);
        return sprites;
    }

    public bool animated => terrain == Terrain.Water;

    public bool passable => terrain != Terrain.Water;
};


