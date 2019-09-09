using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

public class BBTile
{
    public Terrain terrain;
    public Building building;
    public Job activeJob;

    public bool hasJob => activeJob != null;
    public bool passable => terrain.passable && (building == null ? true : building.passable);
    public bool hasBuilding => building != null;
    public bool mineable => building == null ? false : building.mineable;

    // Kinda kludgy but fine for now
    public T BuildingAs<T>() where T : class
    {
        if (building != null)
        {
            if (building is T t)
                return t;

            if (building is BuildingVirtual v)
                return v.building as T;
        }

        return null;
    }

    public BBTile(Terrain terrain)
    {
        BB.Assert(terrain != null);
        this.terrain = terrain;
        building = null;
    }
}

public class Map : MonoBehaviour
{
    [HideInInspector]
    public readonly int w = 64;
    [HideInInspector]
    public readonly int h = 64;

    public Texture2D tileset32;
    public Texture2D tileset64;
    public Texture2D sprites32;
    public Texture2D sprites64;

    public Tilemap terrainBase;
    public Tilemap terrainOver;
    public Tilemap buildingBase;
    public Tilemap buildingOver;

    private BBTile[,] tiles;
    public MapTiler tiler; // KLUDGE for sprite access

    public bool ValidTile(Vec2I tile) => tile.x >= 0 && tile.x < w && tile.y >= 0 && tile.y < h;

    public void AssertValidTile(Vec2I tile) => BB.Assert(ValidTile(tile));

    public BBTile Tile(int x, int y) {
        AssertValidTile(new Vec2I(x, y));
        return tiles[x, y];
    }

    public BBTile Tile(Vec2I tile) => Tile(tile.x, tile.y);

    private BBTile[,] GenerateTerrain()
    {
        var grass = new TerrainStandard(TerrainStandard.Terrain.Grass);
        BBTile[,] tiles = new BBTile[w, h];
        for (int x = 0; x < w; ++x)
            for (int y = 0; y < h; ++y)
                tiles[x, y] = new BBTile(grass);

        var water = new TerrainStandard(TerrainStandard.Terrain.Water);
        for (int x = 2; x < 5; ++x)
            for (int y = 2; y < 5; ++y)
                tiles[x, y].terrain = water;

        tiles[5, 5].building = new BuildingResource(BuildingResource.Resource.Rock);
        tiles[6, 5].building = new BuildingResource(BuildingResource.Resource.Tree);

        for (int i = 0; i < 16; ++i)
            tiles[i + 2, 7].building = new BuildingResource(BuildingResource.Resource.Rock);

        return tiles;
    }

    private void Awake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        tiles = GenerateTerrain();
        tiler = new MapTiler(this);
    }

    private void SetBuilding(Vec2I pos, Building building)
    {
        var tile = Tile(pos);
        tile.building = building;
        tiler.UpdateBuilding(pos);
    }

    public void RemoveBuilding(Vec2I pos)
    {
        BB.Assert(Tile(pos).hasBuilding);
        SetBuilding(pos, null);
    }

    public void ReplaceBuilding(Vec2I pos, Building building)
    {
        BB.Assert(Tile(pos).hasBuilding);
        SetBuilding(pos, building);
    }

    public void AddBuilding(Vec2I pos, Building building)
    {
        BB.Assert(!Tile(pos).hasBuilding);
        SetBuilding(pos, building);
    }

    public void ModifyTerrain(Vec2I pos, Terrain terrain)
    {
        var tile = Tile(pos);
        tile.terrain = terrain;
        tiler.UpdateTerrain(pos);
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void GetPath(Vec2I start, Vec2I end)
    {
        AssertValidTile(start);
        AssertValidTile(end);
    }
}
