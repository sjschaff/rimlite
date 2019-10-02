using System.Collections.Generic;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class BuildingProtoFloor : BuildingProtoTiledRender, IBuildable
    {
        public readonly BldgFloorDef def;

        public BuildingProtoFloor(GameController game, BldgFloorDef def) => this.def = def;

        public override IBuilding CreateBuilding() => new BuildingFloor(this);

        public override bool passable => true;

        private bool IsSame(Map map, Vec2I pos)
        {
            if (GetSame<BuildingProtoFloor>(map, pos, out var protoOther))
                return protoOther == this;
            return false;
        }

        public override TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
        {
            var ttype = Tiling.GetTileType(pos, subTile, p => IsSame(map, p));
            Vec2I spritePos = def.spriteOrigin + Tiling.SpriteOffset(ttype);
            return map.game.assets.atlases.Get(def.atlas).GetSprite(spritePos, Vec2I.one);
        }

        public IEnumerable<MinionSkin.Dir> AllowedOrientations()
        {
            yield return MinionSkin.Dir.Down;
        }

        public IEnumerable<ItemInfoRO> GetBuildMaterials()
            => def.materials;

        private class BuildingFloor : BuildingBase<BuildingProtoFloor>
        {
            public BuildingFloor(BuildingProtoFloor proto) : base(proto) { }
        }
    }
}