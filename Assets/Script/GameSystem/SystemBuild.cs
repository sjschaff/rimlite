using System.Collections.Generic;
using Priority_Queue;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class SystemBuild : GameSystemStandard<SystemBuild, SystemBuild.JobBuild>
    {
        public static SystemBuild K_instance;

        private readonly BuildingProtoConstruction proto = new BuildingProtoConstruction();

        public SystemBuild(GameController game) : base(game)
        {
            BB.AssertNull(K_instance);
            K_instance = this;
        }

        public override IOrdersGiver orders => null;

        public void CreateBuild(IBuildingProto proto, Vec2I pos)
            => AddJob(new JobBuild(this, pos, proto));

        public override void WorkAbandoned(JobHandle job, Work work) { }

        protected override IEnumerable<Work> QueryWorkForJob(JobBuild job)
        {
            if (job.building.HasAvailableHauls() ||
                (job.building.HasAllMaterials() && !job.building.hasBuilder))
            yield return new Work(job, job.GetTasks());
        }

        public class JobBuild : JobStandard
        {
            public readonly BuildingProtoConstruction.BuildingConstruction building;

            public JobBuild(SystemBuild build, Vec2I pos, IBuildingProto proto)
                : base(build, pos)
            {
                building = build.proto.Create(proto);
                building.jobHandles.Add(this);
                game.AddBuilding(pos, building);
            }

            private class ItemPriority : FastPriorityQueueNode
            {
                public readonly Item item;
                public ItemPriority(Item item) => this.item = item;
            }


            public IEnumerable<Task> GetTasks()
            {
                while (building.HasAvailableHauls())
                {
                    bool foundNothing = true;
                    foreach (var mat in building.materials)
                    {
                        if (mat.haulRemaining > 0)
                        {
                            var items = new List<Item>(game.FindItems(mat.info.def));
                            if (items.Count == 0)
                                continue;

                            // TODO: make this more general, move somewhere useful
                            var queue = new FastPriorityQueue<ItemPriority>(items.Count);
                            foreach (Item item in items)
                            {
                                if (item.amtAvailable > 0)
                                    queue.Enqueue(
                                        new ItemPriority(item),
                                        Vec2.Distance(item.pos, pos) / (float)mat.HaulAmount(item));
                            }

                            if (queue.Count == 0)
                                continue;

                            foundNothing = false;
                            Item itemHaul = queue.Dequeue().item;
                            int haulAmt = mat.HaulAmount(itemHaul);
                            yield return Capture(new TaskClaim(game,
                                (work) =>
                                {
                                    // This really should never happen
                                    if (mat.haulRemaining < haulAmt)
                                        return null;

                                    mat.amtClaimed += haulAmt;
                                    return new Work.ClaimLambda(() => mat.amtClaimed -= haulAmt);
                                }), out var claimHaul);
                            yield return Capture(new TaskClaimItem(game, itemHaul, haulAmt), out var claimItem);
                            yield return Minion.TaskGoTo.Point(game, itemHaul.pos);
                            yield return new TaskPickupItem(claimItem);
                            yield return Minion.TaskGoTo.Point(game, pos); // TODO: this could be anywhere in the building
                            yield return new TaskLambda(game,
                                (work) =>
                                {
                                    if (!work.minion.carryingItem)
                                        return false;

                                    Item item = work.minion.RemoveItem();
                                    int amt = haulAmt;
                                    if (item.amt > haulAmt)
                                    {
                                        item.Remove(haulAmt);
                                        game.DropItem(work.minion.pos, item);
                                    }
                                    else
                                    {
                                        if (item.amt < haulAmt)
                                            amt = item.amt;
                                        item.Destroy();
                                    }

                                    work.Unclaim(claimHaul);
                                    mat.amtStored += item.amt;
                                    return true;
                                });
                        }
                    }

                    if (foundNothing)
                        break;
                }

                // TODO: move items out of build area

                if (building.HasAllMaterials())
                {
                    if (!building.buildProto.passable)
                        yield return new TaskLambda(game,
                            (work) =>
                            {
                                game.VacateTile(pos);
                                if (!game.IsTileOccupied(pos, work.minion))
                                {
                                    game.RerouteMinions(pos, true);
                                    return true;
                                }

                                return false;
                            });
                    // TODO: should this go before or after the vacate task?
                    yield return Capture(new TaskClaim(game,
                        (work) =>
                        {
                            if (building.hasBuilder)
                                return null;

                            building.hasBuilder = true;
                            return new Work.ClaimLambda(() => building.hasBuilder = false);
                        }), out var buildClaim);
                    yield return Minion.TaskGoTo.Adjacent(game, pos);
                    yield return new TaskTimedLambda(
                        game, pos, MinionAnim.Slash, Tool.Hammer, 2, _ => 1,
                        // TODO: track work amount on building
                        null, //(work, workAmt) => /**/, 9
                        (work) =>
                        {
                            work.Unclaim(buildClaim);
                            BB.Assert(game.Tile(pos).building == building);
                            building.jobHandles.Remove(this);
                            game.ReplaceBuilding(pos, building.buildProto.CreateBuilding());
                            systemTyped.RemoveJob(this);
                        });
                }
            }

            public override void Destroy()
            {
                if (game.Tile(pos).building == building)
                    game.RemoveBuilding(pos);

                base.Destroy();
            }
        }
    }
}