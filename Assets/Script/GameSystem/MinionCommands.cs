using System.Collections.Generic;
using System.Linq;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class MinionCommands : IGameSystem
    {
        public readonly Game game;
        private readonly List<ICommandsGiver> commands;

        public MinionCommands(Game game)
        {
            this.game = game;
            commands = new List<ICommandsGiver>
            {
                new CommandStop(),
                new CommandDraft(),
                new CommandUndraft()
            };
        }

        public IEnumerable<ICommandsGiver> GetCommands() => commands;
        public IEnumerable<IOrdersGiver> GetOrders() { yield break; }
        public IEnumerable<Work> QueryWork() { yield break; }
        public void Update(float dt) { }
    }

    public abstract class CommandBase : ICommandsGiver
    {
        private readonly SpriteDef guiSprite;
        private readonly string guiText;

        protected CommandBase(SpriteDef sprite, string text)
        {
            this.guiSprite = sprite;
            this.guiText = text;
        }

        public abstract bool ApplicableToMinion(Minion minion);
        public abstract void IssueCommand(Minion minion);

        // For now
        public SpriteDef GuiSprite() => guiSprite;
        public string GuiText() => guiText;
    }

    public class CommandStop : CommandBase
    {
        public CommandStop() : base(null, "Stop") { }

        public override bool ApplicableToMinion(Minion minion) => minion.hasWork;
        public override void IssueCommand(Minion minion)
        {
            if (minion.hasWork)
                minion.AbandonWork();
        }
    }

    public class CommandDraft : CommandBase
    {
        public CommandDraft() : base(null, "Draft") { }
        public override bool ApplicableToMinion(Minion minion) => !minion.isDrafted;
        public override void IssueCommand(Minion minion) => minion.Draft();
    }

    public class CommandUndraft : CommandBase
    {
        public CommandUndraft() : base(null, "Undraft") { }
        public override bool ApplicableToMinion(Minion minion) => minion.isDrafted;
        public override void IssueCommand(Minion minion) => minion.Undraft();
    }

    // TODO: allow for other targets
    public static class CombatProvider
    {
        public static void AssignFireAtRepeating(Game game, Minion minion, Vec2I target)
            => JobTransient.AssignWork(minion, "FireAtRepeat", GetTasks(game, minion, target));
        public static IEnumerable<Task> GetTasks(Game game, Minion minion, Vec2I target)
        {
            while (true)
            {
               /* if (!minion.HasLineOfSight(target))
                    yield return new TaskWaitLambda(
                        game, "Waiting for line of sight.",
                        (task, dt) => task.work.minion.HasLineOfSight(target));*/

                bool canceled = false;
                bool fired = false;

                yield return new TaskTimedLambda(
                    game, "Firing at ground.", MinionAnim.Shoot,
                    Tool.RecurveBow, MinionAnim.Shoot.Duration(), (p) => target,
                    (task) => 1,
                    (task, time) =>
                    {
                        if (!task.work.minion.HasLineOfSight(target))
                        {
                       //     canceled = true;
                       //     task.SoftCancel();
                        }

                        if (!fired && time <= (4f/12f))
                        {
                            fired = true;
                            Vec2 ofs = Vec2.zero;
                            switch (minion.dir)
                            {
                                case Dir.Right: ofs = new Vec2(.3f, .35f); break;
                                case Dir.Left: ofs = new Vec2(-.3f, .35f); break;
                                case Dir.Up: ofs = new Vec2(0, 1.25f); break;
                                case Dir.Down: ofs = new Vec2(0, -.2f); break;
                            }

                            game.AddEffect(new Projectile(
                                game, minion.bounds.center + ofs,
                                target + Vec2.one * .5f, 24f,
                                game.defs.Get<SpriteDef>("BB:ProjArrow"),
                                new Vec2(-1, 0)));
                        }
                    },
                    (task) => { });

                if (!canceled)
                    yield return new TaskTimedLambda(
                        game, "Reloading", MinionAnim.Reload,
                        Tool.RecurveBow, .5f, pt => pt,
                        _ => 1, (_1,_2) => { }, _ => { });
            }
        }
    }

    public class CombatContextProvider : IContextMenuProvider, IContextCommand
    {
        public readonly Game game;

        private Vec2I target;
        private bool enabled;
        private List<Minion> minions;

        public CombatContextProvider(Game game) => this.game = game;

        public IEnumerable<IContextCommand> CommandsForTarget(
            Vec2I pos, Selection sel, List<Minion> minions)
        {
            this.target = pos;
            this.minions = minions;

            bool available = false;
            enabled = false;
            foreach (var minion in minions)
            {
                if (minion.isDrafted)
                {
                    available = true;
                    if (minion.HasLineOfSight(target))
                        enabled = true;
                }
            }

            if (available)
                yield return this;
        }

        public void IssueCommand()
        {
            foreach (var minion in minions)
                if (minion.isDrafted)
                    CombatProvider.AssignFireAtRepeating(game, minion, target);
        }

        public bool Enabled() => enabled;
        // TODO: name target if not ground, actually fire at target if not ground
        public string GuiText() => enabled ? $"Fire at target." : "Cannot fire at target (No line of sight)";
    }

    public class PrioritizeWorkContextProvider : IContextMenuProvider
    {
        public readonly Game game;
        public PrioritizeWorkContextProvider(Game game) => this.game = game;

        public IEnumerable<IContextCommand> CommandsForTarget(
            Vec2I pos, Selection sel, List<Minion> minions)
        {
            if (minions.Count > 1)
                yield break;

            // TODO: agents, items
            if (sel.buildings.Count > 0)
            {
                IBuilding building = sel.buildings[0];
                foreach (var desc in building.jobHandles.SelectMany(j => j.AvailableWorks()))
                    yield return new WorkPriorty(minions[0], desc);
            }
        }

        private class WorkPriorty : IContextCommand
        {
            private readonly Minion minion;
            private readonly WorkDesc work;
            private readonly bool canDoWork;
            public WorkPriorty(Minion minion, WorkDesc work)
            {
                this.minion = minion;
                this.work = work;
                this.canDoWork = minion.CanDoWork(work);
            }

            public void IssueCommand()
            {
                BB.Assert(Enabled());
                work.job.ReassignWork(work, minion);
            }

            public bool Enabled() => !work.disabled && canDoWork;

            public string GuiText()
            {
                if (!Enabled())
                {
                    string desc = $"Cannot '{work.description}' ";
                    if (!canDoWork)
                        desc += $"(<unknown>)";
                    else
                        desc += $"({work.disabledReason})";
                    return desc;
                }
                else
                {
                    string desc = $"Prioritize '{work.description}'";
                    if (work.currentAssignee != null)
                        desc += $" (Assigned to {work.currentAssignee.def.name})";
                    return desc;
                }
            }
        }
    }

    [AttributeDontInstantiate]
    public class GoToContextProvider : IContextMenuProvider, IContextCommand
    {
        public readonly Game game;

        private Vec2I target;
        private bool enabled;
        private List<Minion> minions;
        public GoToContextProvider(Game game) => this.game = game;

        public bool Enabled() => enabled;
        public string GuiText() => "Go Here";

        private bool HasPath(Minion minion) => true; // TODO: use islands or something

        public IEnumerable<IContextCommand> CommandsForTarget(
            Vec2I pos, Selection sel, List<Minion> minions)
        {
            this.target = pos;
            this.minions = minions;
            this.enabled = true;

            bool applicable = false;

            foreach (var minion in minions)
            {
                if (minion.isDrafted)
                {
                    applicable = true;
                    if (!HasPath(minion))
                        enabled = false;
                }
            }

            if (applicable)
                yield return this;
        }

        public void IssueCommand()
        {
            foreach (var minion in minions)
            {
                // TODO: make them all go to different spots
                if (minion.isDrafted)
                    JobTransient.AssignWork(minion, "WalkCmd", 
                    new TaskGoTo(game, "Walking.", PathCfg.Point(target)));
            }
        }
    }
}
