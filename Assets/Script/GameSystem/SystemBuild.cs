using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Priority_Queue;

using Vec2 = UnityEngine.Vector2;

namespace BB
{
    public interface IBuildable : IBuildingProto
    {
        IEnumerable<Dir> AllowedOrientations();
        IEnumerable<ItemInfo> GetBuildMaterials();
        IBuilding CreateBuilding(Tile tile, Dir dir);
    }

    public class SystemBuild : GameSystemStandard<SystemBuild, SystemBuild.JobBuild>
    {
        // TODO: kludge
        public static SystemBuild K_instance;

        private readonly Cache<IBuildable, BldgConstructionDef> virtualDefs;

        public SystemBuild(Game game) : base(game)
        {
            BB.AssertNull(K_instance);
            K_instance = this;
            virtualDefs = new Cache<IBuildable, BldgConstructionDef>(
                (buildable) => new BldgConstructionDef(buildable));
        }

        public override IOrdersGiver orders => null;

        public void CreateBuild(IBuildable proto, Tile tile, Dir dir)
            => AddJob(new JobBuild(this, virtualDefs.Get(proto), tile, dir));

        public override void WorkAbandoned(JobHandle handle, Work work)
        {
            var job = (JobBuild)handle;
            job.activeWorks.Remove(work);
        }

        protected override IEnumerable<Work> QueryWorkForJob(JobBuild job)
        {
            if (job.building.HasAvailableHauls() ||
                (job.building.HasAllMaterials() && !job.building.hasBuilder))
            yield return new Work(job, job.GetTasks());
        }

        public class JobBuild : JobStandard
        {
            public readonly HashSet<Work> activeWorks = new HashSet<Work>();
            public readonly BuildingConstruction building;
            public readonly RectInt area;

            public JobBuild(SystemBuild build, BldgConstructionDef def, Tile tile, Dir dir)
                : base(build, tile)
            {
                building = new BuildingConstruction(def, tile, dir);
                area = building.bounds;

                building.jobHandles.Add(this);
                game.AddBuilding(building);
            }

            private class ItemPriority : FastPriorityQueueNode
            {
                public readonly Item item;
                public ItemPriority(Item item) => this.item = item;
            }

            public IEnumerable<Task> GetTasks()
            {
                yield return new TaskLambda(game,
                    (work) =>
                    {
                        activeWorks.Add(work);
                        return true;
                    });

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
                                        Vec2.Distance(item.tile.pos, tile.pos) / mat.HaulAmount(item));
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
                            yield return new TaskGoTo(game, PathCfg.Point(itemHaul.tile.pos));
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
                                        game.K_DropItem(game.Tile(work.agent.pos), item);
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

                    if (!building.conDef.proto.passable)
                        yield return new TaskLambda(game,
                            (work) =>
                            {
                                game.VacateArea(area);
                                if (!game.IsAreaOccupied(area, work.agent))
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
                            game.ReplaceBuilding(
                                building.conDef.proto.CreateBuilding(tile, building.dir));

                            activeWorks.Remove(work);
                            systemTyped.RemoveJob(this);
                        });
                }
                else
                {
                    yield return new TaskLambda(game,
                        (work) =>
                        {
                            activeWorks.Remove(work);
                            return true;
                        });
                }
            }

            public override void Destroy()
            {
                var works = activeWorks.ToList();
                foreach (var work in works)
                    work.Cancel();

                if (tile.building == building)
                {
                    BB.Assert(
                        building.jobHandles.Count == 0 ||
                            (building.jobHandles.Count == 1 &&
                            building.jobHandles.Contains(this)));
                    game.RemoveBuilding(building);
                    game.DropItems(tile, building.materials
                        .Where((mat) => mat.amtStored > 0)
                        .Select((mat) => mat.info.WithAmount(mat.amtStored))
                    );
                }

                base.Destroy();
            }
        }
    }
}