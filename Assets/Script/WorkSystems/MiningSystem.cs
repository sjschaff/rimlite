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

    public class MiningSystem : WorkSystemAsOrders<MiningSystem, MiningSystem.JobMine>
    {
        public class JobMine : JobHandleOrders
        {
            public readonly IMineable mineable;

            public JobMine(MiningSystem system, Vec2I pos) : base(system, pos) {
                BB.Assert(system != null);
                var building = system.game.Tile(pos).building;
                BB.Assert(building != null);
                mineable = (IMineable)system.game.Tile(pos).building;
                BB.Assert(mineable != null);

                mineable.jobHandles.Add(this);
            }

            public override void Destroy()
            {
                mineable.jobHandles.Remove(this);
                base.Destroy();
            }
        }

        public MiningSystem(GameController game) : base(game)
        {
            sprite = game.defs.Get<SpriteDef>("BB:MineOverlay");
        }

        protected override JobMine CreateJob(Vec2I pos)
            => new JobMine(this, pos);

        protected override Work WorkForJob(JobMine job)
        {
            return new Work(
                new TaskLambda(game, (work) =>
                {
                    if (job.activeWork != null)
                        return false;
                    job.activeWork = work;
                    return true;
                }),
                Minion.TaskGoTo.Adjacent(game, job.pos),
                new TaskMine(game, job));
        }

        private void MineFinished(JobMine job, bool canceled)
        {
            BB.AssertNotNull(job);
            BB.Assert(job.system == this);

            job.activeWork = null;
            if (canceled)
                return;

            RemoveJob(job);

            game.RemoveBuilding(job.pos);
            foreach (ItemInfo item in job.mineable.GetMinedMaterials())
                game.DropItem(job.pos, item);
        }

        public override OrdersFlags flags => OrdersFlags.AppliesBuilding | OrdersFlags.AppliesGlobally;
        protected override SpriteDef sprite { get; }
        public override bool ApplicableToBuilding(IBuilding building) => building is IMineable;

        private class TaskMine : TaskTimed
        {
            private readonly JobMine job;

            public TaskMine(GameController game, JobMine work)
                : base(game, work.pos, MinionAnim.Slash, work.mineable.tool, work.mineable.mineAmt)
                => this.job = work;

            protected override void OnEndTask(bool canceled)
            {
                BB.Assert(work == job.activeWork);
                BB.Assert(canceled || job.mineable.mineAmt <= 0);
                job.orders.MineFinished(job, canceled);
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