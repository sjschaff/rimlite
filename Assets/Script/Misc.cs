// A dumping ground for things I don't yet know where to put
using System;

using Vec2 = UnityEngine.Vector2;

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
    }

}
