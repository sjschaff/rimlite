using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    // TODO: rename BBTile -> MapTile (or maybe inner class),
    // rename ITile-> Tile, make class, store pos, can be used as
    // a handle for everyone
    public class Tile
    {
        private readonly Map.BBTile tile;
        public readonly Vec2I pos;

        public Tile(Map.BBTile tile, Vec2I pos)
        {
            BB.AssertNotNull(tile);
            this.tile = tile;
            this.pos = pos;
        }

        public bool passable => terrain.passable && (hasBuilding ? building.passable : true);
        public bool hasBuilding => tile.hasBuilding;
        public Terrain terrain => tile.terrain;

        public IBuilding building
        {
            get
            {
                if (tile.bldgAdj != null)
                {
                    BB.AssertNotNull(tile.bldgAdj.bldgMain);
                    return tile.bldgAdj.bldgMain;
                }

                return tile.bldgMain;
            }
        }
    }

    public class Map
    {
        public class BBTile
        {
            public readonly Tile handle;

            public Terrain terrain;
            public IBuilding bldgMain;
            public BBTile bldgAdj;

            public BBTile(Vec2I pos) => handle = new Tile(this, pos);

            public bool hasBuilding => bldgMain != null || bldgAdj != null;
        }


        public readonly Game game;
        public Vec2I size { get; private set; }
        public Nav nav { get; private set; }
        private MapTiler tiler;
        private BBTile[,] tiles;

        public Map(Game game)
        {
            this.game = game;
            this.size = size;
        }

        private void InitTiles(Vec2I size)
        {
            BB.AssertNull(tiles);

            this.size = size;
            tiles = new BBTile[size.x, size.y];
            for (int x = 0; x < size.x; ++x)
                for (int y = 0; y < size.y; ++y)
                    tiles[x, y] = new BBTile(new Vec2I(x, y));
        }

        private void Init()
        {
            BB.AssertNotNull(tiles);
            BB.AssertNull(tiler);
            BB.AssertNull(nav);

            tiler = new MapTiler(this);
            nav = new Nav(this);
        }

        public bool ValidTile(Vec2I tile) => MathExt.InGrid(size, tile);

        public void AssertValidTile(Vec2I tile) => BB.Assert(ValidTile(tile));

        public BBTile Tile(Vec2I pos)
        {
            AssertValidTile(pos);
            return tiles[pos.x, pos.y];
        }

        public Tile GetTile(Vec2I pos) => Tile(pos).handle;

        public bool HasBuilding(RectInt area)
        {
            foreach (var t in area.allPositionsWithin)
            {
                if (Tile(t).hasBuilding)
                    return true;
            }

            return false;
        }

        private void SetBuilding(Vec2I pos, IBuilding building)
        {
            var tileMain = Tile(pos);
            if (building == null)
            {
                building = tileMain.bldgMain;
                tileMain = null;
            }

            foreach (var t in building.Area(pos).allPositionsWithin)
            {
                var tile = Tile(t);
                tile.bldgMain = null;
                tile.bldgAdj = tileMain;
            }

            if (tileMain != null)
            {
                tileMain.bldgMain = building;
                tileMain.bldgAdj = null;
            }

            tiler.UpdateBuilding(pos);
        }

        public void RemoveBuilding(Tile tile)
        {
            BB.Assert(tile.hasBuilding);
            SetBuilding(tile.pos, null);
        }

        public void ReplaceBuilding(Tile tile, IBuilding building)
        {
            BB.Assert(tile.hasBuilding);
            BB.Assert(tile.building.bounds == building.bounds);
            SetBuilding(tile.pos, building);
        }

        public void AddBuilding(Tile tile, IBuilding building)
        {
            BB.Assert(!HasBuilding(building.Area(tile)));
            SetBuilding(tile.pos, building);
        }

        public void ModifyTerrain(Tile tile, Terrain terrain)
        {
            Tile(tile.pos).terrain = terrain;
            tiler.UpdateTerrain(tile.pos);
        }

        private void GetPath(Vec2I start, Vec2I end)
        {
            AssertValidTile(start);
            AssertValidTile(end);
        }

        // TODO:
        /*public void Init(MapData or File or something)
        {
            InitTiles();
            // load data
            Init();
        }*/

        public void InitDebug(Vec2I size)
        {
            InitTiles(size);

            var grass = new Terrain(game, game.defs.Get<TerrainDef>("BB:Grass"));
            for (int x = 0; x < size.x; ++x)
                for (int y = 0; y < size.y; ++y)
                    tiles[x, y].terrain = grass;

            var water = new Terrain(game, game.defs.Get<TerrainDef>("BB:Water"));
            for (int x = 2; x < 5; ++x)
                for (int y = 2; y < 5; ++y)
                    tiles[x, y].terrain = water;

            int[][] perm =
            {
                new int[] { 0, 1, 0 },
                new int[] { 1, 1, 0 },
                new int[] { 0, 1, 1 },
                new int[] { 1, 1, 1 }
            };

            var wallProto = game.registry.D_GetProto<BldgWallDef>("BB:StoneBrick");
            var rockProto = game.registry.D_GetProto<BldgMineableDef>("BB:Rock");
            var treeProto = game.registry.D_GetProto<BldgMineableDef>("BB:Tree");

            for (int t = 0; t < perm.Length; ++t)
            {
                for (int b = 0; b < perm.Length; ++b)
                {
                    int xofs = (b * perm.Length + t) * 4 + 12;

                    for (int x = 0; x < 3; ++x)
                    {
                        for (int y = 0; y < 3; ++y)
                        {
                            if (y == 2 && perm[t][x] == 0)
                                continue;
                            if (y == 0 && perm[b][x] == 0)
                                continue;

                            tiles[xofs + x, 2 + y].bldgMain = wallProto.CreateBuilding();
                        }
                    }
                }
            }

            tiles[5, 5].bldgMain = rockProto.CreateBuilding();
            tiles[6, 5].bldgMain = treeProto.CreateBuilding();

            for (int i = 0; i < 16; ++i)
                tiles[i + 2, 7].bldgMain = rockProto.CreateBuilding();

            Init();
        }
    }
}