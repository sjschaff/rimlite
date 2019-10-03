﻿using System;
using System.Collections.Generic;
using System.Linq;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    class BuildingProtoWorkbench : BuildingProtoSpriteRender, IBuildable
    {
        private readonly BldgWorkbenchDef def;

        public BuildingProtoWorkbench(GameController game, BldgWorkbenchDef def)
            : base(game, def.sprite, def.bounds.size.y)
        {
            this.def = def;
           // BB.Assert(def.size.x >= def.sprite.rect.size.x / (float)def.sprite.atlas.tilesPerUnit);

            // TODO: jank for now
//BB.Assert(def.workSpot.x > 0 && def.workSpot.x < def.size.x && def.workSpot.y == -1);
        }

        public override IBuilding CreateBuilding()
            => throw new NotSupportedException();

        public IBuilding CreateBuilding(MinionSkin.Dir dir)
            => new BuildingWorkbench(this, dir);

        public override bool passable => false;

        public override BuildingBounds Bounds(MinionSkin.Dir dir)
            => def.bounds.RotatedFromDown(dir);

        public IEnumerable<MinionSkin.Dir> AllowedOrientations()
            => BB.Enums<MinionSkin.Dir>();

        public IEnumerable<ItemInfoRO> GetBuildMaterials()
            => def.materials;

        private class BuildingWorkbench : BuildingBase<BuildingProtoWorkbench>
        {
            private readonly MinionSkin.Dir dir;
            public BuildingWorkbench(BuildingProtoWorkbench proto, MinionSkin.Dir dir)
                : base(proto) => this.dir = dir;

            public override BuildingBounds bounds => proto.Bounds(dir);
        }
    }
}
