using System.Collections.Generic;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    class BuildingProtoWorkbench : BuildingProtoSpriteRender, IBuildable
    {
        private readonly BldgWorkbenchDef def;

        public BuildingProtoWorkbench(GameController game, BldgWorkbenchDef def)
            : base(game, def.sprite, def.size.y)
        {
            this.def = def;
            BB.Assert(def.size.x >= def.sprite.rect.size.x / (float)def.sprite.atlas.tilesPerUnit);

            // TODO: jank for now
            BB.Assert(def.workSpot.x > 0 && def.workSpot.x < def.size.x && def.workSpot.y == -1);
        }

        public override IBuilding CreateBuilding()
            => new BuildingWorkbench(this);

        public override bool passable => true;

        // TODO: allow for orientations
        public override BuildingBounds bounds
            // TODO: make this more functional
            => new BuildingBounds(def.size, def.workSpot + new Vec2I(0, 1));

        public IEnumerable<MinionSkin.Dir> AllowedOrientations()
        {
            yield return MinionSkin.Dir.Down;
        }

        public IEnumerable<ItemInfoRO> GetBuildMaterials()
            => def.materials;

        private class BuildingWorkbench : BuildingBase<BuildingProtoWorkbench>
        {
            public BuildingWorkbench(BuildingProtoWorkbench proto)
                : base(proto) { }
        }
    }
}
