using System.Collections.Generic;

using Vec2I = UnityEngine.Vector2Int;

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
        public SystemMine(Game game) : base(game, game.defs.Get<SpriteDef>("BB:MineOverlay")) { }

        public override OrdersFlags flags => OrdersFlags.AppliesBuilding | OrdersFlags.AppliesGlobally;

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

            public override IEnumerable<Task> GetTasks()
            {
                yield return new TaskGoTo(game, PathCfg.Adjacent(tile.pos));
                yield return new TaskMine(game, this);
            }
        }

        private class TaskMine : TaskTimed
        {
            private readonly JobMine job;

            public TaskMine(Game game, JobMine work)
                : base(game, MinionAnim.Slash, work.mineable.tool, 
                      work.mineable.mineAmt, FacePt(work.tile.pos))
                => this.job = work;

            protected override void OnEndTask(bool canceled)
            {
                BB.Assert(work == job.activeWork);
                if (!canceled)
                {
                    BB.Assert(job.mineable.mineAmt <= 0);

                    game.RemoveBuilding(job.tile);
                    foreach (ItemInfo item in job.mineable.GetMinedMaterials())
                        game.DropItem(job.tile.pos, item);
                }
            }

            protected override void OnWorkUpdated(float workAmt)
            {
                BB.Assert(work == job.activeWork);
                job.mineable.mineAmt -= workAmt;
            }

            protected override float WorkSpeed() => 1;
        }
    }
}