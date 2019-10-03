using System.Collections.Generic;

namespace BB
{
    public class BuildingProtoResource : BuildingProtoSpriteRender
    {
        private readonly BldgMineableDef def;

        public BuildingProtoResource(Game game, BldgMineableDef def)
            : base(game, BuildingBounds.Unit, def.sprite, null)
            => this.def = def;

        public override IBuilding CreateBuilding() => new BuildingResource(this);

        public override bool passable => false;

        public class BuildingResource : BuildingBase<BuildingProtoResource>, IMineable
        {
            public float mineAmt { get; set; }
            public float mineTotal { get; }

            public BuildingResource(BuildingProtoResource proto) : base(proto)
                => mineAmt = mineTotal = 2;

            public override Dir dir => Dir.Down;

            public Tool tool => proto.def.tool;

            public IEnumerable<ItemInfo> GetMinedMaterials()
                => proto.def.resources;
        }
    }
}