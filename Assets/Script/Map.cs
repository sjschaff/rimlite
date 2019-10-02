using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public interface ITile
    {
        bool passable { get; }
        bool hasBuilding { get; }
        IBuilding building { get; }
    }

    public class BBTile : ITile
    {
        public Terrain terrain;
        private IBuilding bldgMain;
        private BBTile bldgAdj;

        public BBTile(Terrain terrain)
        {
            this.terrain = terrain;
            bldgMain = null;
            bldgAdj = null;
        }

        public IBuilding building
        {
            get
            {
                if (bldgAdj != null)
                {
                    BB.AssertNotNull(bldgAdj.bldgMain);
                    return bldgAdj.bldgMain;
                }

                return bldgMain;
            }
        }

        public void K_SetBuilding(IBuilding bldg)
        {
            this.bldgMain = bldg;
        }

        public TBldg GetBuilding<TBldg>() where TBldg : IBuilding
            => (TBldg)building;

        public bool passable => terrain.passable && (hasBuilding ? building.passable : true);
        public bool hasBuilding => bldgMain != null || bldgAdj != null;

    }

    public class Map
    {
        private const int w = 128;
        private const int h = 128;
        public readonly Vec2I size = new Vec2I(w, h);

        public readonly GameController game;
        public readonly Nav nav;

        private readonly MapTiler tiler;
        private readonly BBTile[,] tiles;

        public Map(GameController game)
        {
            this.game = game;
            tiles = GenerateTerrain();
            tiler = new MapTiler(this);
            nav = new Nav(this);
        }

        public bool ValidTile(Vec2I tile) => MathExt.InGrid(size, tile);

        public void AssertValidTile(Vec2I tile) => BB.Assert(ValidTile(tile));

        public BBTile Tile(int x, int y)
        {
            AssertValidTile(new Vec2I(x, y));
            return tiles[x, y];
        }

        public BBTile Tile(Vec2I tile) => Tile(tile.x, tile.y);

        private BBTile[,] GenerateTerrain()
        {
            var grass = new Terrain(game, game.defs.Get<TerrainDef>("BB:Grass"));
            BBTile[,] tiles = new BBTile[w, h];
            for (int x = 0; x < w; ++x)
                for (int y = 0; y < h; ++y)
                    tiles[x, y] = new BBTile(grass);

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

            var proto = game.registry.walls.Get(game.defs.Get<BldgWallDef>("BB:StoneBrick"));
            for (int t = 0; t < perm.Length; ++t)
            {
                for (int b = 0; b < perm.Length; ++b)
                {
                    int xofs = (b* perm.Length + t) * 4 + 12;

                    for (int x = 0; x < 3; ++x)
                    {
                        for (int y = 0; y < 3; ++y)
                        {
                            if (y == 2 && perm[t][x] == 0)
                                continue;
                            if (y == 0 && perm[b][x] == 0)
                                continue;

                            tiles[xofs + x, 2 + y].K_SetBuilding(proto.CreateBuilding());
                        }
                    }
                }
            }

            var rockProto = game.registry.resources.Get(game.defs.Get<BldgMineableDef>("BB:Rock"));
            var treeProto = game.registry.resources.Get(game.defs.Get<BldgMineableDef>("BB:Tree"));
            tiles[5, 5].K_SetBuilding(rockProto.CreateBuilding());
            tiles[6, 5].K_SetBuilding(treeProto.CreateBuilding());

            for (int i = 0; i < 16; ++i)
                tiles[i + 2, 7].K_SetBuilding(rockProto.CreateBuilding());

            return tiles;
        }

        private void SetBuilding(Vec2I pos, IBuilding building)
        {
            var tile = Tile(pos);
            BB.Assert(building == null || building.bounds == BuildingBounds.Unit);
            tile.K_SetBuilding(building);
            tiler.UpdateBuilding(pos);
        }

        public void RemoveBuilding(Vec2I pos)
        {
            BB.Assert(Tile(pos).hasBuilding);
            SetBuilding(pos, null);
        }

        public void ReplaceBuilding(Vec2I pos, IBuilding building)
        {
            BB.Assert(Tile(pos).hasBuilding);
            SetBuilding(pos, building);
        }

        public void AddBuilding(Vec2I pos, IBuilding building)
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
}