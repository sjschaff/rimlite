using System;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;

// A dumping ground for things I don't yet know where to put
namespace BB
{
    public enum Tool { None, Hammer, Pickaxe, Axe, RecurveBow };

    public enum Dir { Down, Left, Up, Right }

    public static class DirExt
    {
        public static Dir NextCW(this Dir dir)
        {
            switch (dir)
            {
                case Dir.Down: return Dir.Left;
                case Dir.Left: return Dir.Up;
                case Dir.Up: return Dir.Right;
                case Dir.Right: return Dir.Down;
                default:
                    throw new ArgumentException();
            }
        }

        public static Dir NextCCW(this Dir dir)
        {
            switch (dir)
            {
                case Dir.Down: return Dir.Right;
                case Dir.Right: return Dir.Up;
                case Dir.Up: return Dir.Left;
                case Dir.Left: return Dir.Down;
                default:
                    throw new ArgumentException();
            }
        }
    }

    public class RaycastTarget
    {
        public readonly float frDist;
        public readonly Agent agent;
        public readonly IBuilding building;

        private RaycastTarget(float frDist, Agent agent, IBuilding building)
        {
            this.frDist = frDist;
            this.agent = agent;
            this.building = building;
        }

        public RaycastTarget(float frDist, Agent agent)
            : this(frDist, agent, null) { }
        public RaycastTarget(float frDist, IBuilding building)
            : this(frDist, null, building) { }
    }

    public struct Circle
    {
        public readonly Vec2 center;
        public readonly float radius;

        public Circle(Vec2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        public static Circle operator +(Circle circle, Vec2 offset)
            => new Circle(circle.center + offset, circle.radius);

        public bool Contains(Vec2 pt)
            => (pt - center).sqrMagnitude <= radius * radius;

        public bool Intersects(Rect rect)
        {
            if (rect.Contains(center))
                return true;

            Vec2 bl = rect.min;
            Vec2 br = new Vec2(rect.xMax, rect.yMin);
            Vec2 tr = rect.max;
            Vec2 tl = new Vec2(rect.xMin, rect.yMax);
            return
                Ray.FromPts(bl, br).IntersectsCircle(this, true, out _) ||
                Ray.FromPts(br, tr).IntersectsCircle(this, true, out _) ||
                Ray.FromPts(tr, tl).IntersectsCircle(this, true, out _) ||
                Ray.FromPts(tl, bl).IntersectsCircle(this, true, out _);
        }
    }

    // Really a line segment parameterized like a ray,
    // but ray is less verbose and who needs actual
    // rays anyways
    public struct Ray
    {
        public Vec2 start;
        public Vec2 dir;
        public float mag;

        private Ray(Vec2 start, Vec2 ray)
        {
            BB.Assert(ray.sqrMagnitude > 0);
            this.start = start;
            this.mag = ray.magnitude;
            this.dir = ray / mag;
        }

        public static Ray FromDir(Vec2 start, Vec2 dir)
            => new Ray(start, dir);
        public static Ray FromPts(Vec2 start, Vec2 end)
            => new Ray(start, end - start);

        public bool IntersectsCircle(
            Circle c, bool allowIntersectFromWithin, out float frIntersection)
        {
            frIntersection = -1;

            // Get c in ray space
            c += -start;

            float dot = Vec2.Dot(dir, c.center);
            if (dot > mag + c.radius || dot < -c.radius)
                return false;

            Vec2 ptNearest = dir * dot;
            float distSq = (c.center - ptNearest).sqrMagnitude;
            float radSq = c.radius * c.radius;
            if (distSq > radSq)
                return false;

            float deltaIntersect = Mathf.Sqrt(radSq - distSq);
            float dotIntersect = dot - deltaIntersect;
            // not this does not check for circles the ray starts in
            // to do so we would have

            if (dotIntersect >= 0 && dotIntersect <= mag)
            {
                frIntersection = dotIntersect / mag;
                return true;
            }

            if (allowIntersectFromWithin)
            {
                dotIntersect = dot + deltaIntersect;
                if (dotIntersect >= 0 && dotIntersect <= mag)
                {
                    frIntersection = dotIntersect / mag;
                    return true;
                }
            }

            return false;
        }
    }
}
