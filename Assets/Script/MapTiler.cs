using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

public struct TileSprite
{
    public readonly Sprite sprite;
    public readonly Color color;

    public TileSprite(Sprite sprite, Color color)
    {
        this.sprite = sprite;
        this.color = color;
    }

    public TileSprite(Sprite sprite) : this(sprite, Color.white) { }

    public static implicit operator TileSprite(Sprite sprite) => new TileSprite(sprite);
}

// Could probably just use stock Tile for this instead
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
        tileData = new TileData
        {
            sprite = TerrainStandard.GetSprite(tiler, TerrainStandard.Terrain.Grass, TerrainStandard.TileType.Base, 0),
            color = Color.white,
            transform = Matrix4x4.identity,
            gameObject = null,
            flags = TileFlags.None,
            colliderType = Tile.ColliderType.None
        };
    }
}

public abstract class VirtualTileBase : Tile
{
    protected abstract bool HasSprite(BBTile tile, Vec2I pos, Vec2I subTile);
    protected abstract TileSprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile);

    public static bool disableRefresh = false;

    protected MapTiler tiler { get; private set; }
    private Tilemap tilemap;
    public static T Create<T>(Tilemap tilemap, MapTiler tiler) where T : VirtualTileBase
    {
        T vtile = CreateInstance<T>();
        vtile.tiler = tiler;
        vtile.tilemap = tilemap;
        return vtile;
    }

    protected BBTile GetTile(Vec2I v) => tiler.map.Tile(v);
    protected Vec2I GridToTile(Vec3I v) => new Vec2I(v.x >> 1, v.y >> 1);
    protected Vec2I GridToSubTile(Vec3I v) => new Vec2I(v.x % 2, v.y % 2);

    public override void GetTileData(Vec3I gridPos, ITilemap itilemap, ref TileData tileData)
    {
        Vec2I pos = GridToTile(gridPos);
        Vec2I subTile = GridToSubTile(gridPos);
        BBTile tile = GetTile(pos);

        TileSprite sprite = HasSprite(tile, pos, subTile) ? GetSprite(tile, pos, subTile) : null;

        tileData = new TileData
        {
            sprite = sprite.sprite,
            color = sprite.color,
            transform = Matrix4x4.identity,
            gameObject = null,
            flags = TileFlags.None,
            colliderType = Tile.ColliderType.None
        };

        // Unity so broken (requires tileData.color to be set also *shrug*)
        tilemap.SetColor(gridPos, sprite.color);
    }


    public override void RefreshTile(Vec3I gridPos, ITilemap tilemap)
    {
        Vec2I pos = GridToTile(gridPos);
        Vec2I subTile = GridToSubTile(gridPos);
        BBTile tile = GetTile(pos);

        if (!disableRefresh || HasSprite(tile, pos, subTile))
            tilemap.RefreshTile(gridPos);

        if (disableRefresh)
            return;

        for (int tx = 0; tx < 2; ++tx)
        {
            for (int ty = 0; ty < 2; ++ty)
            {
                Vec2I t = pos + new Vec2I(tx + subTile.x - 1, ty + subTile.y - 1);
                if (t != pos)
                {
                    t *= 2;
                    for (int i = 0; i < 4; ++i)
                        tilemap.RefreshTile(new Vec3I(t.x + (i >> 1), t.y + (i % 2), 0));
                }
            }
        }
    }
}

public class VirtualTileTerrainBase : VirtualTileBase
{
    protected override bool HasSprite(BBTile tile, Vec2I pos, Vec2I subTile) => true;

    protected override TileSprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
        => TerrainStandard.GetSprite(tiler, TerrainStandard.Terrain.Grass, TerrainStandard.TileType.Base, 0);
}

public class VirtualTileTerrainOver : VirtualTileBase
{
    protected override bool HasSprite(BBTile tile, Vec2I pos, Vec2I subTile)
    {
        var terrainStandard = tile.terrain as TerrainStandard;
        return terrainStandard == null || terrainStandard.terrain != TerrainStandard.Terrain.Grass;
    }

    protected override TileSprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
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
    protected override bool HasSprite(BBTile tile, Vec2I pos, Vec2I subTile)
        => tile.hasBuilding && (tile.building.tiledRender || subTile == Vec2I.zero);

    protected override TileSprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
        => tile.building.GetSprite(tiler, pos, subTile);
}

public class VirtualTileBuildingOver : VirtualTileBase
{
    protected override bool HasSprite(BBTile tile, Vec2I pos, Vec2I subTile)
        => tile.hasBuilding && tile.building.oversized && subTile == Vec2I.zero;

    protected override TileSprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
        => tile.building.GetSpriteOver(tiler, pos);
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
            vtileA = VirtualTileBase.Create<T>(tilemap, tiler);
            vtileB = VirtualTileBase.Create<T>(tilemap, tiler);
        }

        public void InitTilemap(BoundsInt bounds, TileBase[] buffer)
        {
            for (int i = 0; i < buffer.Length; ++i)
                buffer[i] = vtileA;
            tilemap.SetTilesBlock(bounds, buffer);
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
    public Atlas tileset32 { get; private set; }
    public Atlas tileset64 { get; private set; }
    public Atlas sprites32 { get; private set; }
    public Atlas sprites64 { get; private set; }

    private readonly TilemapUpdater<VirtualTileTerrainBase> tilemapTerrain;
    private readonly TilemapUpdater<VirtualTileTerrainOver> tilemapTerrainOver;
    private readonly TilemapUpdater<VirtualTileBuilding> tilemapBuilding;
    private readonly TilemapUpdater<VirtualTileBuildingOver> tilemapBuildingOver;

    public MapTiler(Map map)
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        this.map = map;
        tileset32 = new Atlas(map.tileset32, 16, 32);
        tileset64 = new Atlas(map.tileset64, 32, 64);
        sprites32 = new Atlas(map.sprites32, 8, 32);
        sprites64 = new Atlas(map.sprites64, 16, 64);

        tilemapTerrain = new TilemapUpdater<VirtualTileTerrainBase>(this, map.terrainBase);
        tilemapTerrainOver = new TilemapUpdater<VirtualTileTerrainOver>(this, map.terrainOver);
        tilemapBuilding = new TilemapUpdater<VirtualTileBuilding>(this, map.buildingBase);
        tilemapBuildingOver = new TilemapUpdater<VirtualTileBuildingOver>(this, map.buildingOver);

        sw.Start();
        var bounds = new BoundsInt(0, 0, 0, map.size.x * 2, map.size.y * 2, 1);
        var tileBuffer = new TileBase[bounds.size.x * bounds.size.y];

        VirtualTileBase.disableRefresh = true;
        tilemapTerrain.InitTilemap(bounds, tileBuffer);
        tilemapTerrainOver.InitTilemap(bounds, tileBuffer);
        tilemapBuilding.InitTilemap(bounds, tileBuffer);
        tilemapBuildingOver.InitTilemap(bounds, tileBuffer);
        VirtualTileBase.disableRefresh = false;

        sw.Stop();
        Debug.Log("tiles took " + sw.ElapsedMilliseconds + "ms");
        sw.Reset();
    }

    public void UpdateTerrain(Vec2I tile) => tilemapTerrainOver.UpdateTile(tile);
    public void UpdateBuilding(Vec2I tile)
    {
        tilemapBuilding.UpdateTile(tile);
        tilemapBuildingOver.UpdateTile(tile);
    }
}
