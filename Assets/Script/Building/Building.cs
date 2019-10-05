using System.Collections.Generic;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public struct BuildingBounds
    {
        public readonly Vec2I size;
        public readonly Vec2I origin;

        public BuildingBounds(Vec2I size, Vec2I origin)
        {
            BB.Assert(origin.x >= 0 && origin.y >= 0 && origin.x <= size.x && origin.y <= size.y);
            this.size = size;
            this.origin = origin;
        }

        public static readonly BuildingBounds Unit = new BuildingBounds(Vec2I.one, Vec2I.zero);

        public RectInt AsRect(Tile tile) => AsRect(tile.pos);
        public RectInt AsRect(Vec2I pos) => new RectInt(pos - origin, size);

        public bool IsAdjacent(Vec2I pos) => AsRect(Vec2I.zero).IsAdjacent(pos);

        public BuildingBounds RotatedFromDown(Dir dir)
        {
            if (dir == Dir.Down)
                return this;

            switch (dir)
            {
                case Dir.Down: return this;

                case Dir.Up:
                    return new BuildingBounds(
                        size,
                        new Vec2I(size.x - 1 - origin.x, size.y - 1 - origin.y));

                case Dir.Left:
                    return new BuildingBounds(
                        new Vec2I(size.y, size.x),
                        new Vec2I(origin.y, size.x - 1 - origin.x));

                case Dir.Right:
                    return new BuildingBounds(
                        new Vec2I(size.y, size.x),
                        new Vec2I(size.y - 1 - origin.y, origin.x));

                default:
                    throw new ArgumentException();
            }
        }

        public static bool operator ==(BuildingBounds a, BuildingBounds b)
            => a.size == b.size && a.origin == b.origin;

        public static bool operator !=(BuildingBounds a, BuildingBounds b) => !(a == b);

        public override bool Equals(object obj)
        {
            return obj is BuildingBounds bounds && bounds == this;
        }

        public override int GetHashCode()
        {
            var hashCode = 1845097995;
            hashCode = hashCode * -1521134295 + EqualityComparer<Vec2I>.Default.GetHashCode(size);
            hashCode = hashCode * -1521134295 + EqualityComparer<Vec2I>.Default.GetHashCode(origin);
            return hashCode;
        }
    }

    [Flags]
    public enum RenderFlags
    {
        None = 0,
        Tiled = 1,
        Oversized = 2,
    }

    public interface IBuilding : ISelectable
    {
        IBuildingProto prototype { get; }
        HashSet<JobHandle> jobHandles { get; }

        bool passable { get; }

        // bounds when facing down
        BuildingBounds bounds { get; }
        RenderFlags renderFlags { get; }
        TileSprite GetSprite(Vec2I pos, Vec2I subTile);
        TileSprite GetSpriteOver(Vec2I pos);
    }

    public static class BuildingExt
    {
        public static bool TiledRender(this IBuilding bldg)
            => bldg.renderFlags.HasFlag(RenderFlags.Tiled);
        public static bool Oversized(this IBuilding bldg)
            => bldg.renderFlags.HasFlag(RenderFlags.Oversized);

        public static RectInt Area(this IBuilding bldg, Vec2I pos) => bldg.bounds.AsRect(pos);
        public static RectInt Area(this IBuilding bldg, Tile tile) => bldg.Area(tile.pos);
    }

    public abstract class BuildingBase<TProto> : IBuilding where TProto : IBuildingProto
    {
        protected readonly TProto proto;
        public IBuildingProto prototype => proto;
        public HashSet<JobHandle> jobHandles { get; }
        protected BuildingBase(TProto proto)
        {
            this.proto = proto;
            this.jobHandles = new HashSet<JobHandle>();
        }

        public abstract Dir dir { get; }

        public virtual string name => proto.name;
        public virtual bool passable => proto.passable;
        public virtual BuildingBounds bounds => proto.Bounds(Dir.Down);
        public virtual RenderFlags renderFlags => proto.GetFlags(Dir.Down);
        public virtual TileSprite GetSprite(Vec2I pos, Vec2I subTile)
            => proto.GetSprite(dir, pos, subTile);
        public virtual TileSprite GetSpriteOver(Vec2I pos)
            => proto.GetSpriteOver(dir, pos);
    }
}