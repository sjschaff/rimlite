using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;

namespace BB
{
    public class Line
    {
        private readonly Transform transform;
        private readonly LineRenderer renderer;

        public Line(LineRenderer renderer)
        {
            BB.AssertNotNull(renderer);
            this.transform = renderer.transform;
            this.renderer = renderer;
        }

        public bool enabled { set => renderer.enabled = value; }
        public Vec2 position { set => transform.localPosition = value; }
        public float width { set => renderer.widthMultiplier = value; }

        public void SetPts(Vec2[] pts)
        {
            renderer.positionCount = pts.Length;
            renderer.SetPositions(pts.Select(v => v.Vec3()).ToArray());
        }

        public void SetRect(Vec2 a, Vec2 b)
        {
            SetPts(new Vec2[] {
                new Vec2(a.x, a.y),
                new Vec2(b.x, a.y),
                new Vec2(b.x, b.y),
                new Vec2(a.x, b.y)
            });
        }

        public void SetRect(RectInt rect) => SetRect(rect.min, rect.max);

        public void Destroy()
            => transform.gameObject.Destroy();
    }
}
