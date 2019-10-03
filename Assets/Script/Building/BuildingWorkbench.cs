using System;
using System.Collections.Generic;

namespace BB
{
    class BuildingProtoWorkbench : BuildingProtoSpriteRender, IBuildable
    {
        private readonly BldgWorkbenchDef def;

        public BuildingProtoWorkbench(GameController game, BldgWorkbenchDef def)
            : base(game, def.bounds, def.spriteDown, def.spriteRight)
            => this.def = def;

        public override IBuilding CreateBuilding()
            => throw new NotSupportedException();

        public IBuilding CreateBuilding(Dir dir)
            => new BuildingWorkbench(this, dir);

        public override bool passable => false;

        public IEnumerable<Dir> AllowedOrientations()
            => BB.Enums<Dir>();

        public IEnumerable<ItemInfo> GetBuildMaterials()
            => def.materials;

        private class BuildingWorkbench : BuildingBase<BuildingProtoWorkbench>
        {
            public BuildingWorkbench(BuildingProtoWorkbench proto, Dir dir)
                : base(proto) => this.dir = dir;

            public override Dir dir { get; }
            public override BuildingBounds bounds => proto.Bounds(dir);
        }
    }
}
