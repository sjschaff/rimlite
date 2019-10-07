using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        public class JobBuild : JobStandard<SystemBuild, JobBuild>
        {
            private readonly HashSet<Work> activeWorks = new HashSet<Work>();
            private readonly BuildingConstruction building;
            private readonly RectInt area;

            // Build State
            private readonly List<HaulProvider> hauls;
            private bool hasBuilder;

            public JobBuild(SystemBuild build, BldgConstructionDef def, Tile tile, Dir dir)
                : base(build, tile)
            {
                building = new BuildingConstruction(def, tile, dir);
                area = building.bounds;

                building.jobHandles.Add(this);
                game.AddBuilding(building);

                hauls = new List<HaulProvider>();
                PathCfg dst = PathCfg.Area(area);
                hauls = def.proto.GetBuildMaterials().Select(
                    item => new HaulProvider(build.game, dst, item)).ToList();
            }

            private IClaim ClaimBuild()
            {
                if (hasBuilder)
                    return null;

                hasBuilder = true;
                return new ClaimLambda(() => hasBuilder = false);
            }

            public override IEnumerable<Work> QueryWork()
            {
                // Already building
                if (hasBuilder)
                    yield break;

                // Ready to build
                if (HasAllMaterials())
                    yield return new Work(this, GetBuildWork());

                // Ready to haul and build
                if (AllMaterialsAvailable())
                    yield return new Work(this, GetHaulAndBuildWork());

                // Hauls only
                foreach (var haul in hauls)
                    if (haul.HasAvailableHauls())
                        yield return new Work(this, GetHaulWork(haul));
            }

            private bool HasAvailableHauls(out HaulProvider haulAvailable)
            {
                haulAvailable = null;
                foreach (var haul in hauls)
                    if (haul.HasAvailableHauls())
                    {
                        haulAvailable = haul;
                        return true;
                    }

                return false;
            }

            private bool AllMaterialsAvailable()
            {
                foreach (var haul in hauls)
                    if (!haul.HasAllMaterials() && !haul.HasAvailableHauls())
                        return false;

                return true;
            }

            private bool HasAllMaterials()
            {
                foreach (var haul in hauls)
                    if (!haul.HasAllMaterials())
                        return false;

                return true;
            }

            private Task TaskBegin()
                => new TaskLambda(game, (work) => activeWorks.Add(work));
            private Task TaskEnd()
                => new TaskLambda(game, (work) => activeWorks.Remove(work));

            private IEnumerable<Task> GetHaulWork(HaulProvider haul)
                => haul.GetHaulTasks().Append(TaskBegin()).Prepend(TaskEnd());

            private IEnumerable<Task> GetBuildWork()
                => GetBuildTasks().Prepend(TaskBegin());

            public IEnumerable<Task> GetHaulAndBuildWork()
            {
                yield return TaskBegin();

                while (HasAvailableHauls(out var haul))
                {
                    foreach (var task in haul.GetHaulTasks())
                        yield return task;
                }

                if (HasAllMaterials())
                {
                    foreach (var task in GetBuildTasks())
                        yield return task;
                }
                else
                {
                    yield return TaskEnd();
                }
            }

            private IEnumerable<Task> GetBuildTasks()
            {
                // TODO: move items out of build area
                yield return Capture(new TaskClaim(game,
                    (work) => ClaimBuild()), out var buildClaim);

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
                        BB.Assert(tile.building == building);

                        work.Unclaim(buildClaim);
                        foreach (var haul in hauls)
                            haul.RemoveStored();

                        building.jobHandles.Remove(this);
                        game.ReplaceBuilding(
                            building.conDef.proto.CreateBuilding(tile, building.dir));

                        activeWorks.Remove(work);
                        systemTyped.RemoveJob(this);
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
                    game.RemoveBuilding(building);

                    game.DropItems(tile, hauls
                        .Where((haul) => haul.HasSomeMaterials())
                        .Select((haul) => haul.RemoveStored())
                    );
                }

                foreach (var haul in hauls)
                    haul.Destroy();

                base.Destroy();
            }
        }
    }
}