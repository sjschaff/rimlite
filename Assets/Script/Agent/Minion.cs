﻿using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class Minion : Agent
    {
        // TODO: something more robust
        private static int nextRenderLayer = 0;

        public MinionSkin skin { get; }

        public Minion(Game game, Vec2I pos)
            : base(game, pos, "Minion")
        {
            skin = transform.gameObject.AddComponent<MinionSkin>();
            skin.Init(game.assets, nextRenderLayer);
            ++nextRenderLayer;
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
                skin.dir == Dir.Up ?
                    Item.Config.PlayerBelow :
                    Item.Config.PlayerAbove);
        }
    }
}