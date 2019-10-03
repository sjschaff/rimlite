using System.Collections.Generic;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class BuildingProtoFloor : BuildingProtoTiledRender, IBuildable
    {
        public readonly BldgFloorDef def;

        public BuildingProtoFloor(GameController game, BldgFloorDef def)
            : base(game) => this.def = def;

        public override IBuilding CreateBuilding() => new BuildingFloor(this);
        public IBuilding CreateBuilding(Dir dir)
        {
            BB.Assert(dir == Dir.Down);
            return CreateBuilding();
        }

        public override bool passable => true;

        private bool IsSame(Vec2I pos)
        {
            if (GetSame<BuildingProtoFloor>(pos, out var protoOther))
                return protoOther == this;
            return false;
        }

        public override TileSprite GetSprite(Dir dir, Vec2I pos, Vec2I subTile)
        {
            BB.Assert(dir == Dir.Down);
            var ttype = Tiling.GetTileType(pos, subTile, p => IsSame(p));
            Vec2I spritePos = def.spriteOrigin + Tiling.SpriteOffset(ttype);
            return game.assets.atlases.Get(def.atlas).GetSprite(spritePos, Vec2I.one);
        }

        public IEnumerable<Dir> AllowedOrientations()
        {
            yield return Dir.Down;
        }

        public IEnumerable<ItemInfo> GetBuildMaterials()
            => def.materials;

        private class BuildingFloor : BuildingBase<BuildingProtoFloor>
        {
            public BuildingFloor(BuildingProtoFloor proto) : base(proto) { }
            public override Dir dir => Dir.Down;
        }
    }
}