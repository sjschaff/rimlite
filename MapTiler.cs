using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;


public enum TileType {
    Unique,
    CornerBL, CornerBR, CornerTR, CornerTL,
    ConcaveBL, ConcaveBR, ConcaveTR, ConcaveTL,
    SideL, SideR, SideB, SideT,
    Base, BaseVariant1, BaseVariant2, BaseVariant3,
    WallHackWall, WallHackC, WallHackL, WallHackR,
    WallHackTL, WallHackTR, WallHackBL, WallHackBR, WallHackCL, WallHackCR}

public struct SpriteKey
{
    public Terrain terrain;
    public TileType ttype;
    public int frame;
}

public struct SpriteKey32
{
    public Building building;
    public TileType ttype;
}

public class TileAtlas32 : Atlas<SpriteKey32>
{
    public TileAtlas32(Texture2D atlas) : base(atlas) { }

    public Sprite GetSprite(Building building, TileType ttype)
    {
        SpriteKey32 key;
        key.building = building;
        key.ttype = ttype;
        return GetSprite(key);
    }

    private Vec2I BuildingOrigin(Building building)
    {
        switch (building)
        {
            case Building.WallStoneBrick: return new Vec2I(0, 12);
            case Building.Rock: return new Vec2I(0, 2);
        }

        throw new System.Exception("unknown building: " + building);
    }

    private Vec2I SpriteOffset(TileType ttype)
    {
        switch (ttype)
        {
            case TileType.WallHackWall: return new Vec2I(0, 0);
            case TileType.WallHackL: return new Vec2I(1, 0);
            case TileType.WallHackC: return new Vec2I(2, 0);
            case TileType.WallHackR: return new Vec2I(3, 0);
            case TileType.WallHackTL: return new Vec2I(4, 0);
            case TileType.WallHackTR: return new Vec2I(5, 0);
            case TileType.WallHackBL: return new Vec2I(6, 0);
            case TileType.WallHackBR: return new Vec2I(7, 0);
            case TileType.WallHackCL: return new Vec2I(8, 0);
            case TileType.WallHackCR: return new Vec2I(9, 0);
        }

        return TileAtlas.SpriteOffsetKLUDGE(ttype) + new Vec2I(0, 1);
    }

    protected override Sprite CreateSprite(SpriteKey32 key)
    {
        var buildingStart = BuildingOrigin(key.building);
        var spriteInd = buildingStart;
        int ppu = 32;

        if (key.building == Building.WallStoneBrick || key.building == Building.FloorStoneBrick)
        {
            spriteInd += SpriteOffset(key.ttype);
            ppu = 64;
        }

        float anchor = ppu / 128f;
        return Sprite.Create(
            atlas,
            new Rect(spriteInd * 32, new Vec2(32, 32)),
            new Vec2(anchor, anchor),
            ppu, 0,//4, /// TODO: 0, 1 or 2?
            SpriteMeshType.FullRect,
            new Vector4(0, 0, 0, 0),
            false);
    }
}

public class TileAtlas : Atlas<SpriteKey>
{
    public TileAtlas(Texture2D atlas) : base(atlas) { }

    private Vec2I TerrainOrigin(Terrain terrain, int frame)
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

        throw new System.Exception("unkown ttype: " + terrain);
    }

    public static Vec2I SpriteOffsetKLUDGE(TileType ttype)
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

        throw new System.Exception("unkown ttype: " + ttype);
    }

    private Vec2I SpriteOffset(TileType ttype) => SpriteOffsetKLUDGE(ttype);

    private TileType WaterKLUDGE(Terrain terrain, TileType ttype)
    {
        if (terrain == Terrain.Water)
        {
            switch (ttype)
            {
                case TileType.ConcaveBL: return TileType.ConcaveTR;
                case TileType.ConcaveTR: return TileType.ConcaveBL;
                case TileType.ConcaveBR: return TileType.ConcaveTL;
                case TileType.ConcaveTL: return TileType.ConcaveBR;
            }
        }

        return ttype;
    }

    public Sprite GetSprite(Terrain terrain, TileType ttype, int frame)
    {
        BB.Assert(
            ttype != TileType.WallHackWall && ttype != TileType.WallHackC && ttype != TileType.WallHackL && ttype != TileType.WallHackR
             && ttype != TileType.WallHackTL && ttype != TileType.WallHackTR && ttype != TileType.WallHackBL
             && ttype != TileType.WallHackBR && ttype != TileType.WallHackCL && ttype != TileType.WallHackCR);

        SpriteKey key;
        key.terrain = terrain;
        key.ttype = ttype;
        key.frame = frame;
        return GetSprite(key);
    }

    protected override Sprite CreateSprite(SpriteKey key)
    {
        var terrainStart = TerrainOrigin(key.terrain, key.frame);
        var spriteInd = terrainStart + SpriteOffset(WaterKLUDGE(key.terrain, key.ttype));

        return Sprite.Create(
            atlas,
            new Rect(spriteInd * 16, new Vec2(16, 16)),
            new Vec2(.5f, .5f),
            32, 0,//4, /// TODO: 0, 1 or 2?
            SpriteMeshType.FullRect,
            new Vector4(0, 0, 0, 0),
            false);
    }
}


public class BaseTile : TileBase
{
    private MapTiler tiler;

    public static BaseTile Create(MapTiler tiler)
    {
        BaseTile tile = (BaseTile)CreateInstance(typeof(BaseTile));
        tile.tiler = tiler;
        return tile;
    }

    public override void GetTileData(Vec3I position, ITilemap tilemap, ref TileData tileData)
    {
        tileData = new TileData();
        tileData.sprite = tiler.GetSprite(Terrain.Grass, TileType.Base, 0);
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.gameObject = null;
        tileData.flags = TileFlags.None;
        tileData.colliderType = Tile.ColliderType.None;
    }
}

public class VirtualTile : TileBase
{
    public enum TileTypeA { Corner, Concave, SideA, SideB, Base }

    private MapTiler tiler;

    public static VirtualTile Create(MapTiler tiler)
    {
        VirtualTile tile = (VirtualTile)CreateInstance(typeof(VirtualTile));
        tile.tiler = tiler;
        return tile;
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

    private static TileType GetTileType(bool[,] adj, int rx, int ry)
    {
        if (rx == 0)
        {
            if (ry == 0)
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
            if (ry == 0)
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
        throw new System.Exception("Failed To compute tile type");
    }

    private TileType GetTileType(Vec3I position)
    {
        Vec2I pos = GridToTile(position);
        Vec2I r = GridToSubTile(position);
        var tile = GetTile(pos);

        bool[,] adj = new bool[3, 3];
        for (int ax = 0; ax < 3; ++ax)
            for (int ay = 0; ay < 3; ++ay)
                adj[ax, ay] = IsSame(pos, new Vec2I(pos.x + ax - 1, pos.y + ay - 1));

        TileType ttype = GetTileType(adj, r.x, r.y);
        if (tile.IsWallType())
        {
            if (!adj[1, 0])
            {
                if (r.y == 0)
                    ttype = TileType.WallHackWall;
                else if (ttype == TileType.CornerTL)
                    ttype = TileType.WallHackL;
                else if (ttype == TileType.SideT)
                    ttype = TileType.WallHackC;
                else if (ttype == TileType.CornerTR)
                    ttype = TileType.WallHackR;
                else if (ttype == TileType.SideL)
                    ttype = TileType.CornerBL;
                else if (ttype == TileType.SideR)
                    ttype = TileType.CornerBR;
                else if (ttype == TileType.Base)
                    ttype = TileType.SideB;
                else if (ttype == TileType.ConcaveTR)
                    ttype = TileType.WallHackTR;
                else if (ttype == TileType.ConcaveTL)
                    ttype = TileType.WallHackTL;
            }
            else if (!adj[0, 0] || !adj[2, 0])
            {
                if (ttype == TileType.ConcaveBL)
                    ttype = TileType.SideL;
                else if (ttype == TileType.ConcaveBR)
                    ttype = TileType.SideR;
                else if (ttype == TileType.SideT && !adj[0, 0] && r.x == 0)
                    ttype = TileType.WallHackBL;
                else if (ttype == TileType.SideT && !adj[2, 0] && r.x == 1)
                    ttype = TileType.WallHackBR;
                else if (ttype == TileType.ConcaveTL && !adj[0, 0])
                    ttype = TileType.WallHackCL;
                else if (ttype == TileType.ConcaveTR && !adj[2, 0])
                    ttype = TileType.WallHackCR;
            }
        }

        return ttype;
    }

    private bool IsSame(Vec2I posThis, Vec2I posOther)
    {
        if (!tiler.map.ValidTile(posOther))
            return false;

        var tileThis = tiler.map.Tile(posThis);
        var tileOther = tiler.map.Tile(posOther);
        if (tileThis.IsFullTileBuilding())
            return tileOther.building == tileThis.building;
        else
            return tileOther.terrain == tileThis.terrain;
    }

    public override bool GetTileAnimationData(Vec3I position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        Terrain terrain = GetTile(position).terrain;
        if (terrain == Terrain.Water)
        {
            tileAnimationData.animatedSprites = new Sprite[8];
            for (int i = 0; i < 8; ++i)
                tileAnimationData.animatedSprites[i] = tiler.GetSprite(terrain, GetTileType(position), i);
            tileAnimationData.animationSpeed = 2;
            tileAnimationData.animationStartTime = 0;

            return true;
        }

        return false;
    }

    private BBTile GetTile(Vec2I v) => tiler.map.Tile(v);
    private BBTile GetTile(Vec3I v) => GetTile(GridToTile(v));
    private Vec2I GridToTile(Vec3I v) => new Vec2I(v.x >> 1, v.y >> 1);
    private Vec2I GridToSubTile(Vec3I v) => new Vec2I(v.x % 2, v.y % 2);

    public override void GetTileData(Vec3I position, ITilemap tilemap, ref TileData tileData)
    {
        tileData = new TileData();

        var tile = GetTile(position);
        TileType ttype = GetTileType(position);
        if (tile.IsFullTileBuilding())
        {
            tileData.sprite = tiler.GetSprite32(tile.building, ttype);
        }
        else if (tile.IsBuilding())
        {
            if (GridToSubTile(position) == Vec2I.zero)
            {
                tileData.sprite = tiler.GetSprite32(tile.building, TileType.Unique);
            }
        }
        else
        {
            if (tile.terrain != Terrain.Grass)
                tileData.sprite = tiler.GetSprite(tile.terrain, ttype, 0);
        }
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.gameObject = null;
        tileData.flags = TileFlags.None;
        tileData.colliderType = Tile.ColliderType.None;
    }

    public override void RefreshTile(Vec3I pos, ITilemap tilemap)
    {
        var p = GridToTile(pos);
        var r = GridToSubTile(pos);

        //(0, 0) -> -1, -1
        //(0, 1) -> -1, 0
        //(1, 1) -> 0, 0
        //(1, 0) -> 0, -1

        for (int tx = 0; tx < 2; ++tx)
        {
            for (int ty = 0; ty < 2; ++ty)
            {
                Vec2I t = p + new Vec2I(tx + r.x - 1, ty + r.y - 1);
                if (t != p)
                {
                    t *= 2;
                    for (int i = 0; i < 4; ++i)
                        tilemap.RefreshTile(new Vec3I(t.x + (i >> 1), t.y + (i % 2), 0));
                }
            }
        }

        /*if (r.x == 0)
        {
            if (r.y == 0)
            {
                tilemap.RefreshTile(new Vec3I(pos.x - 1, pos.y, pos.z));
                tilemap.RefreshTile(new Vec3I(pos.x - 1, pos.y - 1, pos.z));
                tilemap.RefreshTile(new Vec3I(pos.x, pos.y - 1, pos.z));
            }
            else
            {
                tilemap.RefreshTile(new Vec3I(pos.x - 1, pos.y, pos.z));
                tilemap.RefreshTile(new Vec3I(pos.x - 1, pos.y + 1, pos.z));
                tilemap.RefreshTile(new Vec3I(pos.x, pos.y + 1, pos.z));
            }
        }
        else
        {
            if (r.y == 0)
            {
                tilemap.RefreshTile(new Vec3I(pos.x + 1, pos.y, pos.z));
                tilemap.RefreshTile(new Vec3I(pos.x + 1, pos.y - 1, pos.z));
                tilemap.RefreshTile(new Vec3I(pos.x, pos.y - 1, pos.z));
            }
            else
            {
                tilemap.RefreshTile(new Vec3I(pos.x + 1, pos.y, pos.z));
                tilemap.RefreshTile(new Vec3I(pos.x + 1, pos.y + 1, pos.z));
                tilemap.RefreshTile(new Vec3I(pos.x, pos.y + 1, pos.z));
            }
        }*/

        tilemap.RefreshTile(pos);
    }
}

public class MapTiler
{
    // TODO: maybe not public
    public Map map;
    private TileAtlas atlas;
    private TileAtlas32 atlas32;

    private Tilemap tilemapBase;
    private Tilemap tilemapOver;

    private BaseTile grassTile;
    private VirtualTile vTileA;
    private VirtualTile vTileB;

    public Sprite GetSprite(Terrain terrain, TileType ttype, int frame) => atlas.GetSprite(terrain, ttype, frame);
    public Sprite GetSprite32(Building building, TileType ttype) => atlas32.GetSprite(building, ttype);

    public MapTiler(Map map, Texture2D atlasTex, Texture2D atlasTex32, Tilemap tilemapBase, Tilemap tilemapOver)
    {
        this.map = map;
        this.tilemapBase = tilemapBase;
        this.tilemapOver = tilemapOver;
        atlas = new TileAtlas(atlasTex);
        atlas32 = new TileAtlas32(atlasTex32);
        grassTile = BaseTile.Create(this);
        vTileA = VirtualTile.Create(this);
        vTileB = VirtualTile.Create(this);

        for (int x = 0; x < map.w * 2; ++x)
        {
            for (int y = 0; y < map.h * 2; ++y)
            {
                tilemapBase.SetTile(new Vec3I(x, y, 0), grassTile);
            }
        }

        for (int x = 0; x < map.w; ++x)
        {
            for (int y = 0; y < map.h; ++y)
            {
                for (int i = 0; i < 4; ++i)
                        tilemapOver.SetTile(new Vec3I(2 * x + (i % 2), 2 * y + (i >> 1), 0), vTileA);
            }
        }
    }

    public void UpdateTile(Vec2I tile)
    {
        var tPrev = (VirtualTile)tilemapOver.GetTile((tile * 2).Vec3());
        var tNew = tPrev == vTileA ? vTileB : vTileA;
        for (int i = 0; i < 4; ++i)
            tilemapOver.SetTile(new Vec3I(tile.x * 2 + (i % 2), tile.y * 2 + (i >> 1), 0), tNew);
    }
}
