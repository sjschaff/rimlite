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

        public override bool ApplicableToBuilding(IBuilding building) => building is IMineable;

        protected override JobMine CreateJob(Tile tile)
            => new JobMine(this, tile);

        public class JobMine : JobHandleOrders
        {
            public readonly IMineable mineable;

            public JobMine(SystemMine system, Tile tile) : base(system, tile)
            {
                BB.Assert(tile.hasBuilding);
                mineable = (IMineable)tile.building;
                BB.Assert(mineable != null);

                // TODO: move this more general?
                // would probably require some kind of HasJobHandles
                // style interface
                mineable.jobHandles.Add(this);
            }

            public override void Destroy()
            {
                mineable.jobHandles.Remove(this);
                base.Destroy();
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

            public override IEnumerable<Task> GetTasks()
            {
                string desc = $"{DescForTool(mineable.tool)} {mineable.def.name}.";
                yield return new TaskGoTo(game, desc, PathCfg.Adjacent(tile.pos));
                yield return new TaskTimedLambda(
                    game, desc, MinionAnim.Slash,
                    mineable.tool, mineable.mineAmt,
                    TaskTimed.FacePt(tile.pos),
                    _ => 1,
                    (work, workAmt) =>
                    {
                        BB.Assert(work == activeWork);
                        mineable.mineAmt -= workAmt;
                    },
                    (work) =>
                    {
                        BB.Assert(work == activeWork);
                        BB.Assert(mineable.mineAmt <= 0);

                        game.RemoveBuilding(mineable);
                        game.DropItems(tile, mineable.GetMinedMaterials());
                    }
                );
            }
        }
    }
}