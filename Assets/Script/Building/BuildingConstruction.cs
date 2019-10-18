using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class BuildingConstruction : BuildingBase<IBuildingProto>
    {
        public readonly BldgConstructionDef conDef;
        public override Dir dir { get; }

        public bool constructionBegan;
        public float constructionPercent; // or some such thing...

        public BuildingConstruction(BldgConstructionDef def, Tile tile, Dir dir)
            : base(null, tile)
        {
            this.conDef = def;
            this.dir = dir;
            constructionBegan = false;
            constructionPercent = 0;
        }

        public override DefNamed def => conDef;

        public override bool passable => constructionBegan ? conDef.proto.passable : true;
        public override RectInt bounds => conDef.proto.Bounds(dir).AsRect(tile);
        public override RenderFlags renderFlags => conDef.proto.GetFlags(dir);

        private TileSprite Virtualize(TileSprite sprite)
            => new TileSprite(sprite.sprite, sprite.color * new Color(.6f, .6f, 1, .65f));

        public override TileSprite GetSprite(Vec2I pos, Vec2I subTile)
            => Virtualize(conDef.proto.GetSprite(dir, pos, subTile));

        public override TileSprite GetSpriteOver(Vec2I pos)
            => Virtualize(conDef.proto.GetSpriteOver(dir, pos));
    }
}