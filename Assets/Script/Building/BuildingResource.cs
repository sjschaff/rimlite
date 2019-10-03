using System.Collections.Generic;

namespace BB
{
    public class BuildingProtoResource : BuildingProtoSpriteRender
    {
        private readonly BldgMineableDef def;

        public BuildingProtoResource(GameController game, BldgMineableDef def)
            : base(game, def.sprite, 1)
            => this.def = def;

        public override IBuilding CreateBuilding() => new BuildingResource(this);

        public override bool passable => false;

        public override BuildingBounds Bounds(MinionSkin.Dir dir)
        {
            BB.Assert(dir == MinionSkin.Dir.Down);
            return BuildingBounds.Unit;
        }

        public class BuildingResource : BuildingBase<BuildingProtoResource>, IMineable
        {
            public float mineAmt { get; set; }
            public float mineTotal { get; }

            public BuildingResource(BuildingProtoResource proto) : base(proto)
                => mineAmt = mineTotal = 2;

            public Tool tool => proto.def.tool;

            public IEnumerable<ItemInfo> GetMinedMaterials()
            {
                foreach (var item in proto.def.resources)
                    yield return item;
            }

        }
    }
}