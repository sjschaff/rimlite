using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class Minion : Agent
    {
        public MinionSkin skin { get; }
        public bool isDrafted { get; private set; }
        public bool isIdle { get; private set; }

        private static AgentDef CreateMinionDef(Game game)
            => new AgentDef("BB:Minion", "Minion",
                game.defs.agentMinion,
                new Circle(Vec2.one * .5f, .4f));

        public Minion(Game game, Vec2I pos)
            : base(game, CreateMinionDef(game), pos, "Minion")
        {
            skin = transform.gameObject.AddComponent<MinionSkin>();
            skin.Init(game.assets);
        }

        public void Draft()
        {
            BB.Assert(!isDrafted);
            isDrafted = true;
            if (hasWork)
                AbandonWork();
        }

        public void Undraft()
        {
            BB.Assert(isDrafted);
            isDrafted = false;
            if (hasWork)
                AbandonWork();
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            skin.UpdateAnim(dt);
        }

        public override bool AssignWork(Work work)
        {
            isIdle = false;
            return base.AssignWork(work);
        }

        public void AssignIdleWork(Work work)
        {
            if (base.AssignWork(work))
                isIdle = true;
        }

        // TODO:
        public bool CanDoWork(WorkDesc desc) => true;

        public override void SetTool(Tool tool)
            => skin.SetTool(tool);
        public override void SetAnim(MinionAnim anim)
            => skin.SetAnimLoop(anim);
        public override void UpdateSkinDir()
            => skin.SetDir(dir);
    }
}
