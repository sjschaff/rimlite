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
            this.size = size;
            this.origin = origin;
        }

        public static readonly BuildingBounds Unit = new BuildingBounds(Vec2I.one, Vec2I.zero);

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

    public interface IBuilding
    {
        IBuildingProto prototype { get; }
        HashSet<JobHandle> jobHandles { get; }

        bool passable { get; }

        // bounds when facing down
        BuildingBounds bounds { get; }
        RenderFlags renderFlags { get; }
        TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile);
        TileSprite GetSpriteOver(Map map, Vec2I pos);
    }

    public static class BuildingExt
    {
        public static bool TiledRender(this IBuilding bldg)
            => bldg.renderFlags.HasFlag(RenderFlags.Tiled);
        public static bool Oversized(this IBuilding bldg)
            => bldg.renderFlags.HasFlag(RenderFlags.Oversized);

        public static IEnumerable<Vec2I> AllTiles(this IBuilding bldg, Vec2I pos)
        {
            var bounds = bldg.bounds;
            return new RectInt(pos - bounds.origin, bounds.size).AllTiles();
        }
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

        public virtual bool passable => proto.passable;
        public virtual BuildingBounds bounds => proto.bounds;
        public virtual RenderFlags renderFlags => proto.renderFlags;
        public virtual TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
            => proto.GetSprite(map, pos, subTile);
        public virtual TileSprite GetSpriteOver(Map map, Vec2I pos)
            => proto.GetSpriteOver(map, pos);
    }
}