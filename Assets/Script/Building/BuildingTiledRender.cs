using System;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public abstract class BuildingProtoTiledRender : IBuildingProto
    {
        protected BuildingProtoTiledRender() { }

        public abstract IBuilding CreateBuilding();
        public abstract bool passable { get; }
        public abstract TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile);

        public BuildingBounds bounds => BuildingBounds.Unit;
        public RenderFlags renderFlags => RenderFlags.Tiled;

        public TileSprite GetSpriteOver(Map map, Vec2I pos)
            => throw new NotSupportedException("GetSpriteOver called on BuildingProtoTiledRender.");

        // TODO: this is a terrible name
        protected bool GetSame<TThis>(Map map, Vec2I pos, out TThis proto) where TThis : BuildingProtoTiledRender
        {
            proto = null;
            if (!map.ValidTile(pos))
                return false;

            var bldgOther = map.Tile(pos).building;
            if (bldgOther is BuildingProtoConstruction.BuildingConstruction bldgConstruction)
                proto = bldgConstruction.buildProto as TThis;
            else
                proto = bldgOther?.prototype as TThis;

            return proto != null;
        }
    }
}