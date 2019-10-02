using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class Minion : Agent
    {
        public MinionSkin skin { get; }

        public Minion(GameController game, Vec2I pos)
            : base(game, pos, "Minion")
        {
            skin = transform.gameObject.AddComponent<MinionSkin>();
            skin.Init(game.assets);
        }

        public override void SetTool(Tool tool)
            => skin.SetTool(tool);

        public override void SetAnim(MinionAnim anim)
            => skin.SetAnimLoop(anim);

        public override void SetFacing(Vec2 dir)
        {
            skin.SetDir(dir);
            if (carriedItem != null)
                ReconfigureItem();
        }

        protected override void ReconfigureItem()
        {
            BB.AssertNotNull(carriedItem);
            carriedItem.Configure(
                skin.dir == MinionSkin.Dir.Up ?
                    Item.Config.PlayerBelow :
                    Item.Config.PlayerAbove);
        }
    }
}
