using System.Collections.Generic;

namespace BB
{
    public interface IMineable : IBuilding
    {
        Tool tool { get; }
        IEnumerable<ItemInfo> GetMinedMaterials();

        // TODO: better names
        float mineAmt { get; set; }
        float mineTotal { get; }
    }

    public class SystemMine : GameSystemAsOrders<SystemMine, SystemMine.JobMine>
    {
        public SystemMine(Game game)
            : base(game, game.defs.Get<SpriteDef>("BB:MineIcon"), "Mine") { }

        public override bool SelectionOnly() => false;

        public override bool AppliesToBuilding(IBuilding building) => building is IMineable;

        protected override JobMine CreateJob(IBuilding building)
            => new JobMine(this, building);

        public class JobMine : JobHandleOrders
        {
            private readonly IMineable mineable;
            private Tile tile => mineable.tile;

            public JobMine(SystemMine system, IBuilding building)
                : base(system, building, $"Mine {building.def.name}.") // TODO: tool type
            {
                BB.AssertNotNull(building);
                mineable = (IMineable)building;
                BB.AssertNotNull(mineable);
            }

            private static string DescForTool(Tool tool)
            {
                switch (tool)
                {
                    case Tool.Pickaxe: return "Mining";
                    case Tool.Axe: return "Chopping";
                    case Tool.Hammer: return "Breaking";
                    case Tool.None:
                    default:
                        return "Intimidating";
                }
            }

            protected override IEnumerable<Task> GetTasks()
            {
                string desc = $"{DescForTool(mineable.tool)} {mineable.def.name}.";
                yield return new TaskGoTo(game, desc, PathCfg.Adjacent(tile.pos));
                yield return new TaskTimedLambda(
                    game, desc, MinionAnim.Slash,
                    mineable.tool, mineable.mineAmt,
                    TaskTimed.FacePt(tile.pos),
                    _ => 1,
                    (task, workAmt) =>
                    {
                        BB.Assert(task.work == activeWork);
                        mineable.mineAmt -= workAmt;
                        return true;
                    },
                    (task) =>
                    {
                        BB.Assert(task.work == activeWork);
                        BB.Assert(mineable.mineAmt <= 0);

                        mineable.jobHandles.Remove(this);
                        game.RemoveBuilding(mineable);
                        game.DropItems(tile, mineable.GetMinedMaterials());
                    }
                );
            }
        }
    }
}