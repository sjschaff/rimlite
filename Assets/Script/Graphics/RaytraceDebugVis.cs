using UnityEngine;

using Vec2 = UnityEngine.Vector2;

namespace BB
{
    public class RaycastDebugVis
    {
        private readonly Game game;
        private readonly Line clear;
        private readonly Line hit;

        public RaycastDebugVis(Game game)
        {
            this.game = game;
            clear = game.assets.CreateLine(
                game.agentContainer, "dbg",
                RenderLayer.Highlight, Color.green,
                1 / 32f, false, true);

            hit = game.assets.CreateLine(
                game.agentContainer, "dbg",
                RenderLayer.Highlight.Layer(1), Color.red,
                1 / 32f, false, true);
        }

        public void Update(Vec2 a, Vec2 b, bool allowInternal)
        {
            clear.SetPts(new Vec2[] { a, b });
            var hit = game.GetFirstRaycastTarget(Ray.FromPts(a, b), allowInternal);
            if (hit != null)
            {
                this.hit.enabled = true;
                this.hit.SetPts(new Vec2[] { hit.frDist * (b - a) + a, b });
            }
            else
                this.hit.enabled = false;
        }

        public void Destroy()
        {
            clear.Destroy();
            hit.Destroy();
        }
    }
}
