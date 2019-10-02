using UnityEngine;

namespace BB
{
    public abstract class BuildingProtoSpriteRender : IBuildingProto
    {
        private readonly Sprite sprite;
        private readonly Sprite spriteOver;

        protected BuildingProtoSpriteRender(GameController game, SpriteDef spriteDef)
        {
            if (Tiling.SplitSprite(spriteDef, out var defLower, out var defUpper))
            {
                sprite = game.assets.sprites.Get(defLower);
                spriteOver = game.assets.sprites.Get(defUpper);
            }
            else
            {
                sprite = game.assets.sprites.Get(spriteDef);
            }
        }

        public abstract IBuilding CreateBuilding();
        public abstract bool passable { get; }

        // TODO:
        public virtual BuildingBounds bounds
            => throw new System.NotImplementedException();

        public RenderFlags renderFlags
            => spriteOver == null ? RenderFlags.None : RenderFlags.Oversized;

        public TileSprite GetSprite(Map map, Vector2Int pos, Vector2Int subTile)
            => sprite;

        public TileSprite GetSpriteOver(Map map, Vector2Int pos)
            => spriteOver;
    }
}
