using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

public enum Terrain { Grass, Mud, Dirt, Path, Water }
public enum Building { None, WallStoneBrick, FloorStoneBrick,/*Stone,*/ Rock, Tree }

// TODO: better name (rename BBTile->VirtualTile)
public struct BBTile
{
    public Terrain terrain;
    public Building building;
    // public Item item;

    public bool passable => Passable(terrain) && Passable(building);

    public bool Passable(Terrain terrain) => terrain != Terrain.Water;
    public bool Passable(Building building) => building == Building.None || building == Building.FloorStoneBrick;

    public bool IsBuilding() => building != Building.None;

    public bool IsFullTileBuilding()
    {
        return building == Building.WallStoneBrick || building == Building.FloorStoneBrick;
    }

    public bool IsWallType()
    {
        return building == Building.WallStoneBrick;
    }

    public bool Mineable() => building == Building.Rock;

    public BBTile(Terrain terrain)
    {
        this.terrain = terrain;
        this.building = Building.None;
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

    private BBTile[,] tiles;
    private MapTiler tiler;

    public bool ValidTile(Vec2I tile) => tile.x >= 0 && tile.x < w && tile.y >= 0 && tile.y < h;

    public void AssertValidTile(Vec2I tile) => BB.Assert(ValidTile(tile));

    public BBTile Tile(int x, int y) {
        AssertValidTile(new Vec2I(x, y));
        return tiles[x, y];
    }

    public BBTile Tile(Vec2I tile) => Tile(tile.x, tile.y);

    private BBTile[,] GenerateTerrain()
    {
        BBTile[,] tiles = new BBTile[w, h];
        for (int x = 0; x < w; ++x)
            for (int y = 0; y < h; ++y)
                tiles[x, y] = new BBTile(Terrain.Grass);

        tiles[2, 2].terrain = tiles[2, 3].terrain = tiles[3, 2].terrain = tiles[3, 3].terrain = Terrain.Water;
        tiles[5, 5].building = Building.Rock;

        return tiles;
    }

    private void Awake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        // TODO: start or awake?
        var tilemapBase = transform.GetChild(0).GetComponent<Tilemap>();
        var tilemapOver = transform.GetChild(1).GetComponent<Tilemap>();

        tiles = GenerateTerrain();
        tiler = new MapTiler(this, atlasTexture, atlas32, tilemapBase, tilemapOver);
    }

    public void RemoveBuilding(Vec2I tile)
    {
        AssertValidTile(tile);

        tiles[tile.x, tile.y].building = Building.None;
        tiler.UpdateTile(tile);
    }

    public void UpdateTile(Vec2I tile, Terrain terrain)
    {
        AssertValidTile(tile);

        tiles[tile.x, tile.y].building = Building.WallStoneBrick;// = new BBTile(terrain);
        tiler.UpdateTile(tile);
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
