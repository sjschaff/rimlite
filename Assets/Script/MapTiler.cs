using System;
using UnityEngine;

using TM = UnityEngine.Tilemaps;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
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

    // TODO: this whole thing is a kludge
    public class VirtualTileTerrainBase : VirtualTileBase
    {
        private static Sprite grassSprite;

        protected override bool HasSprite(BBTile tile, Vec2I pos, Vec2I subTile) => true;

        protected override TileSprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
        {
            if (grassSprite == null)
            {
                var atlas = map.game.assets.atlases.Get(Terrain.K_grassDef.atlas);
                Vec2I spritePos = Terrain.K_grassDef.spriteFrames[0] + Tiling.SpriteOffset(Tiling.TileType.Base);
                grassSprite = atlas.GetSprite(spritePos, Vec2I.one);
            }

            return grassSprite;
        }
    }

    public class VirtualTileTerrainOver : VirtualTileBase
    {
        // TODO:Kludge
        protected override bool HasSprite(BBTile tile, Vec2I pos, Vec2I subTile)
            => tile.terrain.def != Terrain.K_grassDef;

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
            => tile.bldgMain != null && (tile.bldgMain.TiledRender() || subTile == Vec2I.zero);

        protected override TileSprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
            => tile.bldgMain.GetSprite(map, pos, subTile);
    }

    public class VirtualTileBuildingOver : VirtualTileBase
    {
        protected override bool HasSprite(BBTile tile, Vec2I pos, Vec2I subTile)
            => tile.bldgMain != null && tile.bldgMain.Oversized() && subTile == Vec2I.zero;

        protected override TileSprite GetSprite(BBTile tile, Vec2I pos, Vec2I subTile)
            => tile.bldgMain.GetSpriteOver(map, pos);
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
                tilemap.tileAnchor = Vec2.zero;

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

            var material = map.game.assets.spriteMaterial;
            var bounds = new BoundsInt(0, 0, 0, map.size.x * 2, map.size.y * 2, 1);
            var tileBuffer = new TM.TileBase[bounds.size.x * bounds.size.y];

            /*tilemapTerrain =*/new Tilemap<VirtualTileTerrainBase>(map, layout, material, RenderLayer.Default.Layer(0), bounds, tileBuffer);
            tilemapTerrainOver = new Tilemap<VirtualTileTerrainOver>(map, layout, material, RenderLayer.Default.Layer(1), bounds, tileBuffer);
            tilemapBuilding = new Tilemap<VirtualTileBuilding>(map, layout, material, RenderLayer.Default.Layer(2), bounds, tileBuffer);
            tilemapBuildingOver = new Tilemap<VirtualTileBuildingOver>(map, layout, material, RenderLayer.OverMap.Layer(0), bounds, tileBuffer);

            sw.Stop();
            BB.LogInfo("Tilemap generation took " + sw.ElapsedMilliseconds + "ms");
        }

        private Transform CreateGridLayout()
        {
            var node = new GameObject("Tilemap");
            var grid = node.AddComponent<Grid>();
            grid.cellSize = new Vec2(.5f, .5f);
            grid.cellGap = Vec2.zero;
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

    public static class Tiling
    {
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

        // TODO: this is kinda jank
        // shouldnt be new'ing defs, and we'll end up with duplicate sprites
        public static bool SplitSprite(SpriteDef sprite, int height, out SpriteDef lower, out SpriteDef upper)
        {
            var atlas = sprite.atlas;
            var heightTiles = height * atlas.tilesPerUnit;
            var rect = sprite.rect;

            if (sprite.rect.size.y > heightTiles)
            {
                lower = new SpriteDef(null, atlas, new Atlas.Rect(
                    rect.origin,
                    new Vec2I(rect.size.x, heightTiles),
                    rect.anchor));

                var upperOfs = new Vec2I(0, heightTiles);
                upper = new SpriteDef(null, atlas, new Atlas.Rect(
                    rect.origin + upperOfs,
                    new Vec2I(rect.size.x, rect.size.y - heightTiles),
                    rect.anchor - upperOfs));

                return true;
            }

            lower = upper = null;
            return false;
        }
    }
}