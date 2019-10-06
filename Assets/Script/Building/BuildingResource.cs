using System.Collections.Generic;

namespace BB
{
    public class BuildingProtoResource : BuildingProtoSpriteRender
    {
        private readonly BldgMineableDef def;

        public BuildingProtoResource(Game game, BldgMineableDef def)
            : base(game, BuildingBounds.Unit, def.sprite, null)
            => this.def = def;

        public override IBuilding CreateBuilding(Tile tile)
            => new BuildingResource(this, tile);

        public override DefNamed buildingDef => def;

        public override bool passable => false;

        public class BuildingResource : BuildingBase<BuildingProtoResource>, IMineable
        {
            public float mineAmt { get; set; }
            public float mineTotal { get; }

            public BuildingResource(BuildingProtoResource proto, Tile tile)
                : base(proto, tile)
                => mineAmt = mineTotal = 2;

            public override Dir dir => Dir.Down;

            public Tool tool => proto.def.tool;

            public IEnumerable<ItemInfo> GetMinedMaterials()
                => proto.def.resources;
        }
    }
}