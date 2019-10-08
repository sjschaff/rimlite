﻿using System.Collections.Generic;

namespace BB
{
    class SystemDeconstruct
        : GameSystemAsOrders<
            SystemDeconstruct,
            SystemDeconstruct.JobDeconstruct>
    {
        public SystemDeconstruct(Game game)
            : base(game, game.defs.Get<SpriteDef>("BB:BuildIcon"), "Deconstruct") { }

        public override bool SelectionOnly() => false;

        public override bool ApplicableToBuilding(IBuilding building)
            => building.prototype is IBuildable;

        protected override JobDeconstruct CreateJob(Tile tile)
            => new JobDeconstruct(this, tile);

        public class JobDeconstruct : JobHandleOrders
        {
            public readonly IBuilding building;
            public readonly IBuildable buildable;

            public JobDeconstruct(SystemDeconstruct orders, Tile tile)
                : base(orders, tile)
            {
                BB.Assert(tile.hasBuilding);
                building = tile.building;
                buildable = (IBuildable)building.prototype;
                BB.Assert(buildable != null);

                tile.building.jobHandles.Add(this);
            }

            public override void Destroy()
            {
                if (tile.hasBuilding && tile.building == building)
                    building.jobHandles.Remove(this);
                base.Destroy();
            }

            public override IEnumerable<Task> GetTasks()
            {
                string desc = $"Deconstructing {building.def.name}.";
                yield return new TaskGoTo(game, desc, PathCfg.Adjacent(building.bounds));
                yield return new TaskTimedLambda(
                    game, desc, MinionAnim.Slash, Tool.Hammer, 2,
                    TaskTimed.FaceArea(building.bounds),
                    _ => 1,
                    null, // TODO: track deconstruct amt
                    (work) =>
                    {
                        BB.Assert(tile.building == building);
                        building.jobHandles.Remove(this);
                        game.RemoveBuilding(building);
                        game.DropItems(tile, buildable.GetBuildMaterials());
                    });
            }
        }
    }
}
