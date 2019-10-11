using System.Collections.Generic;

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

        public override bool AppliesToBuilding(IBuilding building)
            => building.prototype is IBuildable;

        protected override JobDeconstruct CreateJob(IBuilding building)
            => new JobDeconstruct(this, building);

        public class JobDeconstruct : JobHandleOrders
        {
            private IBuilding building => key.building;
            public readonly IBuildable buildable;

            public JobDeconstruct(SystemDeconstruct orders, IBuilding building)
                : base(orders, building, $"Deconstruct {building.def.name}.")
            {
                BB.AssertNotNull(building);
                buildable = (IBuildable)building.prototype;
                BB.Assert(buildable != null);
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
                    (task) =>
                    {
                        BB.Assert(building.tile.building == building);
                        building.jobHandles.Remove(this);
                        game.RemoveBuilding(building);
                        game.DropItems(building.tile, buildable.GetBuildMaterials());
                    });
            }
        }
    }
}
