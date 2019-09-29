﻿using System.Collections.Generic;
using System;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{

    public class BuildingProtoWall : BuildingProtoTiledRender
    {
        public readonly BldgWallDef def;

        public BuildingProtoWall(BldgWallDef def) => this.def = def;

        public override IBuilding CreateBuilding() => new BuildingWall(this);

        public override bool passable => false;
        public override bool K_mineable => false;
        public override Tool K_miningTool => throw new NotSupportedException("miningTool called on BuildingWall");
        public override IEnumerable<ItemInfo> K_GetMinedMaterials() { yield break; }

        private bool IsSame(Map map, Vec2I pos) => GetSame<BuildingProtoWall>(map, pos, out _);

        // TODO: so ghetto
        private Vec2I SpriteOffset(bool[,] adj, Tiling.TileType ttype, Vec2I subTile)
        {
            // TODO: broken in case: (if a and/or b are set
            //      a x b
            //      x x x
            //      - x -

            if (!adj[1, 0])
            {
                if (subTile.y == 0)
                    return new Vec2I(0, 0);
                else if (ttype == Tiling.TileType.CornerTL)
                    return new Vec2I(1, 0);
                else if (ttype == Tiling.TileType.SideT)
                    return new Vec2I(2, 0);
                else if (ttype == Tiling.TileType.CornerTR)
                    return new Vec2I(3, 0);
                else if (ttype == Tiling.TileType.SideL)
                    ttype = Tiling.TileType.CornerBL;
                else if (ttype == Tiling.TileType.SideR)
                    ttype = Tiling.TileType.CornerBR;
                else if (ttype == Tiling.TileType.Base)
                    ttype = Tiling.TileType.SideB;
                else if (ttype == Tiling.TileType.ConcaveTR)
                    return new Vec2I(5, 0);
                else if (ttype == Tiling.TileType.ConcaveTL)
                    return new Vec2I(4, 0);
            }
            else if (!adj[0, 0] || !adj[2, 0])
            {
                if (ttype == Tiling.TileType.ConcaveBL)
                    ttype = Tiling.TileType.SideL;
                else if (ttype == Tiling.TileType.ConcaveBR)
                    ttype = Tiling.TileType.SideR;
                else if (ttype == Tiling.TileType.SideT && !adj[0, 0] && subTile.x == 0)
                    return new Vec2I(6, 0);
                else if (ttype == Tiling.TileType.SideT && !adj[2, 0] && subTile.x == 1)
                    return new Vec2I(7, 0);
                else if (ttype == Tiling.TileType.ConcaveTL && !adj[0, 0])
                    return new Vec2I(8, 0);
                else if (ttype == Tiling.TileType.ConcaveTR && !adj[2, 0])
                    return new Vec2I(9, 0);
            }

            return Tiling.SpriteOffset(ttype) + new Vec2I(0, 1);
        }

        public override TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
        {
            bool[,] adj = Tiling.GenAdjData(pos, p => IsSame(map, p));
            Tiling.TileType ttype = Tiling.GetTileType(adj, subTile);
            Vec2I spritePos = def.spriteOrigin + SpriteOffset(adj, ttype, subTile);
            return map.game.assets.atlases.Get(def.atlas).GetSprite(spritePos, Vec2I.one);
        }

        public override IEnumerable<ItemInfo> GetBuildMaterials()
        {
            foreach (var item in def.materials)
                yield return item;
        }

        private class BuildingWall : BuildingBase<BuildingProtoWall>
        {
            public BuildingWall(BuildingProtoWall proto) : base(proto) { }
        }
    }

}