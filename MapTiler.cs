using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;


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
        tileData.sprite = TerrainStandard.GetSprite(tiler, TerrainStandard.Terrain.Grass, TerrainStandard.TileType.Base, 0);
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.gameObject = null;
        tileData.flags = TileFlags.None;
        tileData.colliderType = Tile.ColliderType.None;
    }
}

public abstract class VirtualTileBase : TileBase
{
    protected abstract Sprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile);

    protected MapTiler tiler { get; private set; }

    public static T Create<T>(MapTiler tiler) where T : VirtualTileBase
    {
        T vtile = CreateInstance<T>();
        vtile.tiler = tiler;
        return vtile;
    }

    protected BBTile GetTile(Vec2I v) => tiler.map.Tile(v);
    protected Vec2I GridToTile(Vec3I v) => new Vec2I(v.x >> 1, v.y >> 1);
    protected Vec2I GridToSubTile(Vec3I v) => new Vec2I(v.x % 2, v.y % 2);

    public override void GetTileData(Vec3I gridPos, ITilemap tilemap, ref TileData tileData)
    {
        Vec2I pos = GridToTile(gridPos);
        Vec2I subTile = GridToSubTile(gridPos);
        BBTile tile = GetTile(pos);

        tileData = new TileData();
        tileData.sprite = GetSprite(tile, pos, subTile);
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

        tilemap.RefreshTile(pos);
    }
}

public class VirtualTileTerrainBase : VirtualTileBase
{
    protected override Sprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
        => TerrainStandard.GetSprite(tiler, TerrainStandard.Terrain.Grass, TerrainStandard.TileType.Base, 0);
}

public class VirtualTileTerrainOver : VirtualTileBase
{
    protected override Sprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
        => tile.terrain.GetSprite(tiler, pos, subTile);

    public override bool GetTileAnimationData(Vec3I gridPos, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        Vec2I pos = GridToTile(gridPos);
        Terrain terrain = GetTile(pos).terrain;
        if (!terrain.animated)
            return false;

        tileAnimationData.animatedSprites = terrain.GetAnimationSprites(tiler, pos, GridToSubTile(gridPos));
        tileAnimationData.animationSpeed = 2;
        tileAnimationData.animationStartTime = 0;

        return true;
    }
}

public class VirtualTileBuilding : VirtualTileBase
{
    protected override Sprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
    {
        if (tile.HasBuilding() && (tile.building.tiledRender || subTile == Vec2I.zero))
            return tile.building.GetSprite(tiler, pos, subTile);

        return null;
    }
}

public class VirtualTileBuildingOver : VirtualTileBase
{
    protected override Sprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
    {
        if (tile.HasBuilding() && tile.building.oversized && subTile == Vec2I.zero)
            return tile.building.GetSpriteOver(tiler, pos);

        return null;
    }
}

public class MapTiler
{
    private class TilemapUpdater<T> where T : VirtualTileBase
    {
        private readonly Tilemap tilemap;
        private readonly T vtileA;
        private readonly T vtileB;

        public TilemapUpdater(MapTiler tiler, Tilemap tilemap)
        {
            this.tilemap = tilemap;
            vtileA = VirtualTileBase.Create<T>(tiler);
            vtileB = VirtualTileBase.Create<T>(tiler);
        }

        public void UpdateTile(Vec2I v)
        {
            Vec3I gridPos = (v * 2).Vec3();
            var vtilePrev = tilemap.GetTile(gridPos) as T;
            var vtile = vtilePrev == vtileA ? vtileB : vtileA;
            for (int i = 0; i < 4; ++i)
                tilemap.SetTile(gridPos + new Vec3I(i % 2, i >> 1, 0), vtile);
        }
    }

    public Map map { get; private set; }
    public Atas atlas { get; private set; }
    public Atas atlas32 { get; private set; }

    private TilemapUpdater<VirtualTileTerrainOver> tilemapTerrainOver;
    private TilemapUpdater<VirtualTileBuilding> tilemapBuilding;
    private TilemapUpdater<VirtualTileBuildingOver> tilemapBuildingOver;

    public MapTiler(Map map)
    {
        this.map = map;
        atlas = new Atas(map.atlasTexture, 16);
        atlas32 = new Atas(map.atlas32, 32);

        tilemapTerrainOver = new TilemapUpdater<VirtualTileTerrainOver>(this, map.terrainOver);
        tilemapBuilding = new TilemapUpdater<VirtualTileBuilding>(this, map.buildingBase);
        tilemapBuildingOver = new TilemapUpdater<VirtualTileBuildingOver>(this, map.buildingOver);

        var vtileBase = VirtualTileBase.Create<VirtualTileTerrainBase>(this);
        for (int x = 0; x < map.w * 2; ++x)
            for (int y = 0; y < map.h * 2; ++y)
                map.terrainBase.SetTile(new Vec3I(x, y, 0), vtileBase);

        for (int x = 0; x < map.w; ++x)
        {
            for (int y = 0; y < map.h; ++y)
            {
                var gridPos = new Vec2I(x, y);
                tilemapTerrainOver.UpdateTile(gridPos);
                tilemapBuilding.UpdateTile(gridPos);
                tilemapBuildingOver.UpdateTile(gridPos);
            }
        }
    }

    public void UpdateTerrain(Vec2I tile) => tilemapTerrainOver.UpdateTile(tile);
    public void UpdateBuilding(Vec2I tile)
    {
        tilemapBuilding.UpdateTile(tile);
        tilemapBuildingOver.UpdateTile(tile);
    }
}
