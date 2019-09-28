using UnityEngine;
using TM = UnityEngine.Tilemaps;
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

public abstract class VirtualTileBase : TM.Tile
{
    protected abstract bool HasSprite(BBTile tile, Vec2I pos, Vec2I subTile);
    protected abstract TileSprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile);

    public static bool disableRefresh = false;

    protected Map map { get; private set; }
    private TM.Tilemap tilemap;
    public static T Create<T>(Map map, TM.Tilemap tilemap) where T : VirtualTileBase
    {
        T vtile = CreateInstance<T>();
        vtile.map = map;
        vtile.tilemap = tilemap;
        return vtile;
    }

    protected BBTile GetTile(Vec2I v) => map.Tile(v);
    protected Vec2I GridToTile(Vec3I v) => new Vec2I(v.x >> 1, v.y >> 1);
    protected Vec2I GridToSubTile(Vec3I v) => new Vec2I(v.x % 2, v.y % 2);

    public override void GetTileData(Vec3I gridPos, TM.ITilemap itilemap, ref TM.TileData tileData)
    {
        Vec2I pos = GridToTile(gridPos);
        Vec2I subTile = GridToSubTile(gridPos);
        BBTile tile = GetTile(pos);

        TileSprite sprite = HasSprite(tile, pos, subTile) ? GetSprite(tile, pos, subTile) : null;

        tileData = new TM.TileData
        {
            sprite = sprite.sprite,
            color = sprite.color,
            transform = Matrix4x4.identity,
            gameObject = null,
            flags = TM.TileFlags.None,
            colliderType = TM.Tile.ColliderType.None
        };

        // Unity so broken (requires tileData.color to be set also *shrug*)
        tilemap.SetColor(gridPos, sprite.color);
    }


    public override void RefreshTile(Vec3I gridPos, TM.ITilemap tilemap)
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
        => TerrainStandard.GetSprite(map.game.assets, TerrainStandard.Terrain.Grass, TerrainStandard.TileType.Base, 0);
}

public class VirtualTileTerrainOver : VirtualTileBase
{
    protected override bool HasSprite(BBTile tile, Vec2I pos, Vec2I subTile)
    {
        if (tile.terrain is TerrainStandard terrain)
            return terrain.terrain != TerrainStandard.Terrain.Grass;
        return true;
    }

    protected override TileSprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
        => tile.terrain.GetSprite(map, pos, subTile);

    public override bool GetTileAnimationData(Vec3I gridPos, TM.ITilemap tilemap, ref TM.TileAnimationData tileAnimationData)
    {
        Vec2I pos = GridToTile(gridPos);
        Terrain terrain = GetTile(pos).terrain;
        if (!terrain.animated)
            return false;

        tileAnimationData.animatedSprites = terrain.GetAnimationSprites(map, pos, GridToSubTile(gridPos));
        tileAnimationData.animationSpeed = 2;
        tileAnimationData.animationStartTime = 0;

        return true;
    }
}

public class VirtualTileBuilding : VirtualTileBase
{
    protected override bool HasSprite(BBTile tile, Vec2I pos, Vec2I subTile)
        => tile.hasBuilding && (tile.building.TiledRender() || subTile == Vec2I.zero);

    protected override TileSprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
        => tile.building.GetSprite(map, pos, subTile);
}

public class VirtualTileBuildingOver : VirtualTileBase
{
    protected override bool HasSprite(BBTile tile, Vec2I pos, Vec2I subTile)
        => tile.hasBuilding && tile.building.Oversized() && subTile == Vec2I.zero;

    protected override TileSprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
        => tile.building.GetSpriteOver(map, pos);
}

public class MapTiler
{
    private class Tilemap<T> where T : VirtualTileBase
    {
        private readonly TM.Tilemap tilemap;
        private readonly T vtileA;
        private readonly T vtileB;

        public Tilemap(Map map, Transform layout, Material mat, RenderLayer layer, BoundsInt bounds, TM.TileBase[] buffer)
        {
            var node = new GameObject();
            node.transform.SetParent(layout, false);
            tilemap = node.AddComponent<TM.Tilemap>();
            tilemap.origin = Vec3I.zero;
            tilemap.size = new Vec3I(bounds.size.x, bounds.size.y, bounds.size.z);
            tilemap.tileAnchor = Vec3.zero;

            var render = node.AddComponent<TM.TilemapRenderer>();
            render.sortOrder = TM.TilemapRenderer.SortOrder.TopRight;
            render.mode = TM.TilemapRenderer.Mode.Chunk;
            render.detectChunkCullingBounds = TM.TilemapRenderer.DetectChunkCullingBounds.Auto;
            render.maskInteraction = SpriteMaskInteraction.None;
            render.material = mat;
            render.SetLayer(layer);

            vtileA = VirtualTileBase.Create<T>(map, tilemap);
            vtileB = VirtualTileBase.Create<T>(map, tilemap);

            InitTilemap(bounds, buffer);
        }

        private void InitTilemap(BoundsInt bounds, TM.TileBase[] buffer)
        {
            VirtualTileBase.disableRefresh = true;
            for (int i = 0; i < buffer.Length; ++i)
                buffer[i] = vtileA;
            tilemap.SetTilesBlock(bounds, buffer);
            VirtualTileBase.disableRefresh = false;
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
    private readonly Transform layout;
    private readonly Material material;

    //private readonly Tilemap<VirtualTileTerrainBase> tilemapTerrain;
    private readonly Tilemap<VirtualTileTerrainOver> tilemapTerrainOver;
    private readonly Tilemap<VirtualTileBuilding> tilemapBuilding;
    private readonly Tilemap<VirtualTileBuildingOver> tilemapBuildingOver;

    public MapTiler(Map map)
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        this.map = map;

        sw.Start();
        this.layout = CreateGridLayout();
        this.material = new Material(Shader.Find("Sprites/Default"));

        var bounds = new BoundsInt(0, 0, 0, map.size.x * 2, map.size.y * 2, 1);
        var tileBuffer = new TM.TileBase[bounds.size.x * bounds.size.y];

        /*tilemapTerrain =*/    new Tilemap<VirtualTileTerrainBase> (map, layout, material, new RenderLayer("Default", 0),  bounds, tileBuffer);
        tilemapTerrainOver =    new Tilemap<VirtualTileTerrainOver> (map, layout, material, new RenderLayer("Default", 1),  bounds, tileBuffer);
        tilemapBuilding =       new Tilemap<VirtualTileBuilding>    (map, layout, material, new RenderLayer("Default", 2),  bounds, tileBuffer);
        tilemapBuildingOver =   new Tilemap<VirtualTileBuildingOver>(map, layout, material, new RenderLayer("Over Map", 0), bounds, tileBuffer);

        sw.Stop();
        Debug.Log("tiles took " + sw.ElapsedMilliseconds + "ms");
    }

    private Transform CreateGridLayout()
    {
        var node = new GameObject("Tilemap");
        var grid = node.AddComponent<Grid>();
        grid.cellSize = new Vec3(.5f, .5f, 0);
        grid.cellGap = Vec3.zero;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
        return node.transform;
    }

    public void UpdateTerrain(Vec2I tile) => tilemapTerrainOver.UpdateTile(tile);
    public void UpdateBuilding(Vec2I tile)
    {
        tilemapBuilding.UpdateTile(tile);
        tilemapBuildingOver.UpdateTile(tile);
    }
}
