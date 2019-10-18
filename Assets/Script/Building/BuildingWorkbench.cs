using System.Collections.Generic;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class BuildingProtoWorkbench : BuildingProtoSpriteRender, IBuildable
    {
        public readonly BldgWorkbenchDef def;

        public BuildingProtoWorkbench(Game game, BldgWorkbenchDef def)
            : base(game, def.bounds, def.spriteDown, def.spriteRight)
            => this.def = def;

        public override IBuilding CreateBuilding(Tile tile)
            => throw new NotSupportedException();

        public IBuilding CreateBuilding(Tile tile, Dir dir)
            => new BuildingWorkbench(this, tile, dir);

        public override DefNamed buildingDef => def;

        public override bool passable => false;

        public IEnumerable<Dir> AllowedOrientations()
            => BB.Enums<Dir>();

        public IEnumerable<ItemInfo> GetBuildMaterials()
            => def.materials;
    }

    public class BuildingWorkbench : BuildingBase<BuildingProtoWorkbench>
    {
        public BuildingWorkbench(BuildingProtoWorkbench proto, Tile tile, Dir dir)
            : base(proto, tile) => this.dir = dir;

        public override Dir dir { get; }
        public override RectInt bounds
            => proto.Bounds(dir).AsRect(tile);

        public Vec2I workSpot => proto.def.workSpot + tile.pos;
    }
}
