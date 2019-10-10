using System.Collections.Generic;

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
                    game.GoTo(minion, target);
            }
        }
    }
}
