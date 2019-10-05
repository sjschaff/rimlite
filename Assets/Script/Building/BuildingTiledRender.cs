using System;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public abstract class BuildingProtoTiledRender : IBuildingProto
    {
        public readonly Game game;

        protected BuildingProtoTiledRender(Game game)
            => this.game = game;

        public abstract IBuilding CreateBuilding();
        public abstract string name { get; }
        public abstract bool passable { get; }
        public abstract TileSprite GetSprite(Dir dir, Vec2I pos, Vec2I subTile);

        public BuildingBounds Bounds(Dir dir)
        {
            BB.Assert(dir == Dir.Down);
            return BuildingBounds.Unit;
        }

        public RenderFlags GetFlags(Dir dir)
        {
            BB.Assert(dir == Dir.Down);
            return RenderFlags.Tiled;
        }

        public TileSprite GetSpriteOver(Dir dir, Vec2I pos)
            => throw new NotSupportedException("GetSpriteOver called on BuildingProtoTiledRender.");

        // TODO: this is a terrible name
        protected bool GetSame<TThis>(Vec2I pos, out TThis proto) where TThis : BuildingProtoTiledRender
        {
            proto = null;
            if (!game.ValidTile(pos))
                return false;

            var bldgOther = game.Tile(pos).building;
            if (bldgOther is BuildingConstruction bldgConstruction)
                proto = bldgConstruction.buildProto as TThis;
            else
                proto = bldgOther?.prototype as TThis;

            return proto != null;
        }
    }
}