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

    public class MiningSystem : WorkSystemAsOrders<MiningSystem.MineWork>
    {
        public class MineWork : WorkHandle
        {
            public readonly MiningSystem system;
            public readonly IMineable mineable;

            public MineWork(MiningSystem system, Vec2I pos) : base(system, pos) {
                BB.Assert(system != null);
                this.system = system;
                var building = system.game.Tile(pos).building;
                BB.Assert(building != null);
                mineable = (IMineable)system.game.Tile(pos).building;
                BB.Assert(mineable != null);

                mineable.workHandles.Add(this);
            }
        }

        public MiningSystem(GameController game) : base(game)
        {
            sprite = game.defs.Get<SpriteDef>("BB:MineOverlay");
        }

        protected override Job2 JobForWork(MineWork work)
        {
            return new Job2(
                new TaskLambda(game, (job) =>
                {
                    if (work.activeJob != null)
                        return false;
                    work.activeJob = job;
                    return true;
                }),
                Minion.TaskGoTo.Adjacent(game, work.pos),
                new TaskMine(game, work));
        }

        private void MineFinished(MineWork work, bool canceled)
        {
            BB.AssertNotNull(work);
            BB.Assert(work.system == this);

            if (canceled)
            {
                work.activeJob = null;
                return;
            }

            work.mineable.workHandles.Remove(work);
            RemoveOrder(work);

            game.RemoveBuilding(work.pos);
            foreach (ItemInfo item in work.mineable.GetMinedMaterials())
                game.DropItem(work.pos, item);
        }

        // TODO: maybe we dont need this
        protected override MineWork CreateWork(Vec2I pos)
            => new MineWork(this, pos);

        public override OrdersFlags flags => OrdersFlags.AppliesBuilding | OrdersFlags.AppliesGlobally;
        protected override SpriteDef sprite { get; }
        public override bool ApplicableToBuilding(IBuilding building) => building is IMineable;

        private class TaskMine : TaskTimed
        {
            private readonly MineWork work;

            public TaskMine(GameController game, MineWork work)
                : base(game, work.pos, MinionAnim.Slash, work.mineable.tool, work.mineable.mineAmt)
                => this.work = work;

            protected override void OnEndWork(bool canceled)
            {
                BB.Assert(job == work.activeJob);
                BB.Assert(canceled || work.mineable.mineAmt <= 0);
                work.system.MineFinished(work, canceled);
            }

            protected override void OnWorkUpdated(float workAmt)
            {
                BB.Assert(job == work.activeJob);
                work.mineable.mineAmt -= workAmt;
            }

            protected override float WorkSpeed() => 1;
        }
    }
}