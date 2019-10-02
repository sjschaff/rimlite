using UnityEngine;

namespace BB
{
    public abstract class BuildingProtoSpriteRender : IBuildingProto
    {
        private readonly Sprite sprite;
        private readonly Sprite spriteOver;

        protected BuildingProtoSpriteRender(GameController game, SpriteDef spriteDef, int height)
        {
            if (Tiling.SplitSprite(spriteDef, height, out var defLower, out var defUpper))
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
        public abstract BuildingBounds bounds { get; }

        public RenderFlags renderFlags
            => spriteOver == null ? RenderFlags.None : RenderFlags.Oversized;

        public TileSprite GetSprite(Map map, Vector2Int pos, Vector2Int subTile)
            => sprite;

        public TileSprite GetSpriteOver(Map map, Vector2Int pos)
            => spriteOver;
    }
}
