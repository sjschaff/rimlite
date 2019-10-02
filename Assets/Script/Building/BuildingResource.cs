using System.Collections.Generic;

using Vec2I = UnityEngine.Vector2Int;
using UnityEngine;

namespace BB
{
    public class BuildingProtoResource : BuildingProtoSpriteRender
    {
        private readonly BldgMineableDef def;

        public BuildingProtoResource(GameController game, BldgMineableDef def)
            : base(game, def.sprite)
            => this.def = def;

        public override IBuilding CreateBuilding() => new BuildingResource(this);

        public override bool passable => false;

        public override BuildingBounds bounds => BuildingBounds.Unit;

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