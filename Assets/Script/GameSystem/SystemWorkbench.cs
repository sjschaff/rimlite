using System;
using System.Collections.Generic;
using System.Linq;

namespace BB
{
    public class SystemWorkbench :
        GameSystemStandard<SystemWorkbench, BuildingWorkbench, SystemWorkbench.JobBench>
    {
        public SystemWorkbench(Game game) : base(game) { }

        public class JobBench : JobStandard<SystemWorkbench, BuildingWorkbench, JobBench>
        {
            private Work activeWork;
            private readonly List<Order> orders
                = new List<Order>();

            private class Order
            {
                public readonly RecipeDef recipe;
                public HaulProviders hauls;
                public int amtOrdered;
                public float progress;

                public Order(RecipeDef recipe)
                {
                    this.recipe = recipe;
                    this.amtOrdered = 1;
                    this.progress = recipe.workAmt;
                }
            }

            private BuildingWorkbench bench => key;

            public JobBench(SystemWorkbench system, BuildingWorkbench key)
                : base(system, key)
                => key.jobHandles.Add(this);

            public override void Destroy()
            {
                if (activeWork != null)
                    activeWork.Cancel();

                game.DropItems(bench.tile, orders.SelectMany(o => o.hauls.RemoveStored()));
            }

            public override IEnumerable<Work> QueryWork()
            {
                if (activeWork == null)
                {
                    foreach (var order in orders)
                    {
                        if (order.amtOrdered > 0)
                            if (order.hauls.HasAllMaterials() || order.hauls.AllMaterialsAvailable())
                                yield return new Work(this, GetTasks(order), "WorkingBench");
                    }
                }
            }

            public override IEnumerable<WorkDesc> AvailableWorks()
            {
                throw new NotImplementedException();
            }

            public override void ReassignWork(WorkDesc desc, Minion minion)
            {
                throw new NotSupportedException();
                //    if (activeWork != null)
                //        activeWork.minion.AbandonWork();

                //     if (system.HasJob(key))
                //         minion.AssignWork(GetWork());
            }

            private IEnumerable<Task> GetTasks(Order order)
            {
                yield return new TaskLambda(game, "add handle",
                    (work) =>
                    {
                        if (activeWork != null)
                            return false;

                        activeWork = work;
                        return true;
                    });

                while (order.hauls.HasAvailableHauls(out var haul))
                    foreach (var task in haul.GetHaulTasks())
                        yield return task;

                if (order.hauls.HasAllMaterials())
                {
                    // TODO: clear bench of debris
                    yield return new TaskGoTo(game, $"Walking to {bench.def.name}.",
                        PathCfg.Point(bench.workSpot));
                    yield return new TaskTimedLambda(
                        game, order.recipe.description, MinionAnim.Idle,
                        Tool.None, order.progress, TaskTimed.FaceArea(bench.bounds),
                        _ => 1, // TODO: workspeed
                        (task, amt) =>
                        {
                            if (order.amtOrdered <= 0)
                                return false;

                            order.progress = amt;
                            return true;
                        },
                        (task) =>
                        {
                            BB.Assert(order.amtOrdered > 0);
                            order.amtOrdered -= 1;
                            order.progress = order.recipe.workAmt;
                            order.hauls.RemoveStored();
                            game.DropItems(bench.tile, order.recipe.product);
                        });
                }

                yield return new TaskLambda(game, "rem handle",
                    (work) =>
                    {
                        BB.Assert(activeWork == work);
                        activeWork = null;
                        return true;
                    });
            }

            public override void AbandonWork(Work work)
            {
                BB.Assert(activeWork == work);
                activeWork = null;
            }

            private void AddOrder(RecipeDef recipe)
            {
                BB.Assert(bench.proto.def.recipes.Contains(recipe));
                var order = new Order(recipe);
                orders.Add(order);

                var path = PathCfg.Adjacent(bench.bounds);
                order.hauls = new HaulProviders(game, bench.def.name, path,
                    recipe.materials);
            }

            public void ConfigureGUI(WorkbenchPane pane)
            {
                pane.SetRecipes(bench.proto.def.recipes,
                    recipe => {
                        AddOrder(recipe);
                        ConfigureGUI(pane);
                    });

                pane.ShowOrders(orders.Count);
                for (int i = 0; i < orders.Count; ++i)
                {
                    Order order = orders[i];
                    pane.ConfigureButton(i,
                        order.recipe.description,
                        $"x{order.amtOrdered}",
                        () =>
                        {
                            order.amtOrdered++;
                            ConfigureGUI(pane);
                        });
                }
            }
        }

        public void ConfigureGUI(WorkbenchPane pane, BuildingWorkbench bench)
        {
            JobBench job;
            if (HasJob(bench))
            {
                job = GetJob(bench);
                BB.Assert(bench.jobHandles.Contains(job));
            }
            else
            {
                job = new JobBench(this, bench);
                AddJob(job);
            }

            job.ConfigureGUI(pane);
        }
    }
}
