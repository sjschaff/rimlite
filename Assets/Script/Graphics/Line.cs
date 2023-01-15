using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;

namespace BB
{
    public class Line
    {
        public readonly Transform transform;
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

        public void SetCircle(Circle circle, int subdivisions)
        {
            Vec2[] pts = new Vec2[subdivisions];
            float drad = 2 * Mathf.PI / subdivisions;
            for (int i = 0; i < subdivisions; ++i)
            {
                float rad = drad * i;
                float x = Mathf.Cos(rad);
                float y = Mathf.Sin(rad);
                pts[i] = new Vec2(x, y) * circle.radius + circle.center;
            }

            SetPts(pts);
        }

        public void Destroy()
            => transform.gameObject.Destroy();
    }
}
