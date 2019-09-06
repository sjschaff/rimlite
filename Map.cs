using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;


// TODO: better name (rename BBTile->VirtualTile)


public class BBTile
{
    public Terrain terrain;
    public Building building;
    // public Item item;

    public bool Passable() => terrain.passable && (building == null ? true : building.passable);
    public bool HasBuilding() => building != null;
    public bool Mineable() => building == null ? false : building.mineable;

    // Kinda kludgy but fine for now
    public T BuildingAs<T>() where T : class
    {
        if (building != null)
        {
            T t = building as T;
            if (t != null)
                return t;

            var v = building as BuildingVirtual;
            if (v != null)
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
    public readonly int w = 32;
    [HideInInspector]
    public readonly int h = 32;

    public Texture2D atlasTexture;
    public Texture2D atlas32;
    public Tilemap terrainBase;
    public Tilemap terrainOver;
    public Tilemap buildingBase;
    public Tilemap buildingOver;

    private BBTile[,] tiles;
    private MapTiler tiler;
    public Atlas itemAtlas;// KLUUUUUDGE

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

        tiles[2, 2].terrain = tiles[2, 3].terrain = tiles[3, 2].terrain = tiles[3, 3].terrain = new TerrainStandard(TerrainStandard.Terrain.Water);
        tiles[5, 5].building = new BuildingResource(BuildingResource.Resource.Rock);
        tiles[6, 5].building = new BuildingResource(BuildingResource.Resource.Tree);

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

        itemAtlas = new Atlas(atlasTexture, 8);
    }

    private void SetBuilding(Vec2I pos, Building building)
    {
        var tile = Tile(pos);
        tile.building = building;
        tiler.UpdateBuilding(pos);
    }

    public void RemoveBuilding(Vec2I pos)
    {
        BB.Assert(Tile(pos).HasBuilding());
        SetBuilding(pos, null);
    }

    public void ReplaceBuilding(Vec2I pos, Building building)
    {
        BB.Assert(Tile(pos).HasBuilding());
        SetBuilding(pos, building);
    }

    public void AddBuilding(Vec2I pos, Building building)
    {
        BB.Assert(!Tile(pos).HasBuilding());
        SetBuilding(pos, building);
    }

    public void ModifyTerrain(Vec2I pos, Terrain terrain)
    {
        var tile = Tile(pos);
        tile.terrain = terrain;
        tiler.UpdateTerrain(pos);
    }

    public Vec2I MouseToTile() => UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition).xy().Floor();

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
