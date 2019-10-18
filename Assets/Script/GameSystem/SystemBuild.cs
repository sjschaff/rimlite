using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;

namespace BB
{
    public interface IBuildable : IBuildingProto
    {
        IEnumerable<Dir> AllowedOrientations();
        IEnumerable<ItemInfo> GetBuildMaterials();
        IBuilding CreateBuilding(Tile tile, Dir dir);
    }

    public class SystemBuild : GameSystemStandard<SystemBuild, Tile, SystemBuild.JobBuild>
    {
        private readonly Cache<IBuildable, BldgConstructionDef> virtualDefs;

        public SystemBuild(Game game) : base(game)
        {
            virtualDefs = new Cache<IBuildable, BldgConstructionDef>(
                (buildable) => new BldgConstructionDef(buildable));
        }

        public void CreateBuild(IBuildable proto, Tile tile, Dir dir)
            => AddJob(new JobBuild(this, virtualDefs.Get(proto), tile, dir));

        public class JobBuild : JobStandard<SystemBuild, Tile, JobBuild>
        {
            private readonly HashSet<Work> activeWorks = new HashSet<Work>();
            private readonly BuildingConstruction building;
            private readonly RectInt area;
            private readonly string name;
            private Tile tile => key;

            // Build State
            private readonly HaulProviders hauls;
            private bool hasBuilder;

            public JobBuild(SystemBuild build, BldgConstructionDef def, Tile tile, Dir dir)
                : base(build, tile)
            {
                building = new BuildingConstruction(def, tile, dir);
                area = building.bounds;
                name = building.conDef.proto.buildingDef.name;

                building.jobHandles.Add(this);
                game.AddBuilding(building);

                PathCfg dst = PathCfg.Area(area);
                hauls = new HaulProviders(
                    build.game, name, dst, def.proto.GetBuildMaterials());
            }

            private IClaim ClaimBuild()
            {
                if (hasBuilder)
                    return null;

                hasBuilder = true;
                return new ClaimLambda(() => hasBuilder = false);
            }

            // TODO:
            public override IEnumerable<WorkDesc> AvailableWorks()
                { yield break; }
            public override void ReassignWork(WorkDesc desc, Minion minion)
                => throw new System.NotSupportedException();

            public override IEnumerable<Work> QueryWork()
            {
                // Already building
                if (hasBuilder)
                    yield break;

                // Ready to build
                if (CanBuild(null)) // TODO: would be real nice if we could have a minion here
                    yield return new Work(this, GetBuildWork(), "Build_Build");

                // Ready to haul and build
                if (hauls.HasAvailableHauls(out _) && hauls.AllMaterialsAvailable())
                    yield return new Work(this, GetHaulAndBuildWork(), "Build_HaulAndBuild");

                // Hauls only
                foreach (var haul in hauls.hauls)
                    if (haul.HasAvailableHauls())
                        yield return new Work(this, GetHaulWork(haul), "Build_Haul");
            }

            private bool Passable()
                => building.conDef.proto.passable;

            private bool HasDebris()
            {
                if (Passable())
                    return false;

                foreach (var tile in building.bounds.allPositionsWithin)
                    if (game.Tile(tile).hasItems)
                        return true;

                return false;
            }

            private bool IsBlocked(Minion minionIgnore)
            {
                return !Passable() && game.IsAreaOccupied(area, minionIgnore);
            }

            private bool CanBuild(Minion minionIgnore)
            {
                return hauls.HasAllMaterials() && !IsBlocked(minionIgnore) && !HasDebris();
            }

            private Task TaskBegin()
                => new TaskLambda(game, "add handle", (work) => activeWorks.Add(work));
            private Task TaskEnd()
                => new TaskLambda(game, "rem handle", (work) => activeWorks.Remove(work));

            private IEnumerable<Task> GetHaulWork(HaulProvider haul)
                => haul.GetHaulTasks()
                    .Prepend(TaskBegin())
                    .Append(TaskVacate())
                    .Append(TaskEnd());

            private IEnumerable<Task> GetBuildWork()
                => GetBuildTasks().Prepend(TaskBegin());

            private IEnumerable<Task> GetHaulAndBuildWork()
            {
                yield return TaskBegin();

                while (hauls.HasAvailableHauls(out var haul))
                {
                    foreach (var task in haul.GetHaulTasks())
                        yield return task;
                }

                if (hauls.HasAllMaterials())
                {
                    yield return TaskVacate();
                    foreach (var task in GetBuildTasks())
                        yield return task;
                }
                else
                {
                    yield return TaskEnd();
                }
            }

            private IEnumerable<Task> GetClearDebrisTasks()
            {
                const string desc = "Hauling debris from build site.";
                while (HasDebris())
                {
                    foreach (var pos in building.bounds.allPositionsWithin)
                    {
                        var tile = game.Tile(pos);
                        if (tile.hasItems)
                        {
                            yield return Capture(game.ClaimItem(tile), out var claim);
                            yield return new TaskGoTo(game, desc, PathCfg.Point(pos));
                            yield return new TaskPickupItem(claim);
                            yield return new TaskGoTo(game, desc,
                                new PathCfg(
                                    pt => !area.Contains(pt) && !game.Tile(pt).hasItems,
                                    pt => Vec2.Distance(pt, area.center)));
                            yield return new TaskLambda(game, "drop item",
                                (work) =>
                                {
                                    BB.Assert(work.agent.carryingItem);
                                    Item item = work.agent.RemoveItem();
                                    game.DropItems(
                                        game.Tile(work.agent.pos),
                                        item.info.Enumerate());
                                    item.Destroy();
                                    return true;
                                });
                        }

                    }
                }
            }

            private Task TaskVacate()
            {
                return new TaskLambda(
                    game, "vacate build",
                    (work) =>
                    {
                        if (!Passable() && hauls.HasAllMaterials())
                            game.VacateArea(area, "build site");

                        return true;
                    });
            }

            private IEnumerable<Task> GetBuildTasks()
            {
                yield return Capture(new TaskClaim(game,
                    (work) => ClaimBuild()), out var buildClaim);

                // TODO: only the builder can clear debris for
                // now to prevent jobs failing immediately after
                // all the debris is claimed
                foreach (var task in GetClearDebrisTasks())
                    yield return task;

                if (!building.conDef.proto.passable)
                    yield return new TaskLambda(
                        game, "init. build",
                        (work) =>
                        {
                            if (!IsBlocked(work.minion))
                            {
                                building.constructionBegan = true;
                                game.RerouteMinions(area, true);
                                return true;
                            }

                            return false;
                        });
                yield return new TaskGoTo(game, $"Building {name}.", PathCfg.Adjacent(area));
                yield return new TaskTimedLambda(
                    game, $"Building {name}.",
                    MinionAnim.Slash, Tool.Hammer, 2,
                    TaskTimed.FaceArea(area),
                    _ => 1, // TODO: workspeed
                    // TODO: track work amount on building
                    null, //(work, workAmt) => /**/, 9
                    (task) =>
                    {
                        BB.Assert(tile.building == building);

                        task.work.Unclaim(buildClaim);
                        hauls.RemoveStored();

                        building.jobHandles.Remove(this);
                        game.ReplaceBuilding(
                            building.conDef.proto.CreateBuilding(tile, building.dir));

                        activeWorks.Remove(task.work);
                        system.RemoveJob(this);
                    });
            }

            public override void AbandonWork(Work work)
            {
                BB.Assert(activeWorks.Contains(work));
                activeWorks.Remove(work);
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
                    building.jobHandles.Remove(this);
                    game.RemoveBuilding(building);

                    game.DropItems(tile, hauls.RemoveStored());
                }

                base.Destroy();
            }
        }
    }
}