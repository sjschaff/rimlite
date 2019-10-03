using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public interface IBuildable : IBuildingProto
    {
        IEnumerable<Dir> AllowedOrientations();
        IEnumerable<ItemInfo> GetBuildMaterials();
        IBuilding CreateBuilding(Dir dir);
    }

    public class SystemBuild : GameSystemStandard<SystemBuild, SystemBuild.JobBuild>
    {
        // TODO: kludge
        public static SystemBuild K_instance;

        public SystemBuild(Game game) : base(game)
        {
            BB.AssertNull(K_instance);
            K_instance = this;
        }

        public override IOrdersGiver orders => null;

        public void CreateBuild(IBuildable proto, Tile tile, Dir dir)
            => AddJob(new JobBuild(this, tile, proto, dir));

        public override void WorkAbandoned(JobHandle job, Work work) { }

        protected override IEnumerable<Work> QueryWorkForJob(JobBuild job)
        {
            if (job.building.HasAvailableHauls() ||
                (job.building.HasAllMaterials() && !job.building.hasBuilder))
            yield return new Work(job, job.GetTasks());
        }

        public class JobBuild : JobStandard
        {
            public readonly BuildingConstruction building;
            public readonly RectInt area;

            public JobBuild(SystemBuild build, Tile tile, IBuildable proto, Dir dir)
                : base(build, tile)
            {
                building = new BuildingConstruction(proto, dir);
                area = building.Area(tile);

                building.jobHandles.Add(this);
                game.AddBuilding(tile, building);
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
                                        Vec2.Distance(item.pos, tile.pos) / (float)mat.HaulAmount(item));
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
                            yield return new TaskGoTo(game, PathCfg.Point(itemHaul.pos));
                            yield return new TaskPickupItem(claimItem);
                            yield return new TaskGoTo(game, PathCfg.Area(area));
                            yield return new TaskLambda(game,
                                (work) =>
                                {
                                    if (!work.agent.carryingItem)
                                        return false;

                                    Item item = work.agent.RemoveItem();
                                    int amt = haulAmt;
                                    if (item.amt > haulAmt)
                                    {
                                        item.Remove(haulAmt);
                                        game.DropItem(work.agent.pos, item);
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
                    yield return Capture(new TaskClaim(game,
                        (work) =>
                        {
                            if (building.hasBuilder)
                                return null;

                            building.hasBuilder = true;
                            return new Work.ClaimLambda(() => building.hasBuilder = false);
                        }), out var buildClaim);

                    if (!building.buildProto.passable)
                        yield return new TaskLambda(game,
                            (work) =>
                            {
                                game.VacateTile(tile.pos);
                                if (!game.IsTileOccupied(tile.pos, work.agent))
                                {
                                    building.constructionBegan = true;
                                    game.RerouteMinions(area, true);
                                    return true;
                                }

                                return false;
                            });

                    yield return new TaskGoTo(game, PathCfg.Adjacent(area));
                    yield return new TaskTimedLambda(
                        game, MinionAnim.Slash, Tool.Hammer, 2,
                        TaskTimed.FaceArea(area),
                        _ => 1,
                        // TODO: track work amount on building
                        null, //(work, workAmt) => /**/, 9
                        (work) =>
                        {
                            work.Unclaim(buildClaim);
                            BB.Assert(tile.building == building);
                            building.jobHandles.Remove(this);
                            game.ReplaceBuilding(tile, building.buildProto.CreateBuilding(building.dir));
                            systemTyped.RemoveJob(this);
                        });
                }
            }

            public override void Destroy()
            {
                if (tile.building == building)
                    game.RemoveBuilding(tile);

                base.Destroy();
            }
        }
    }
}