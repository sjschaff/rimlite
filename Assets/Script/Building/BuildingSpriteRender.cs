using UnityEngine;

namespace BB
{
    public abstract class BuildingProtoSpriteRender : IBuildingProto
    {
        protected readonly BuildingBounds bounds;
        private readonly Sprite spriteDown;
        private readonly Sprite spriteDownOver;
        private readonly Sprite spriteRight;
        private readonly Sprite spriteRightOver;

        protected BuildingProtoSpriteRender(
            Game game,
            BuildingBounds rect,
            SpriteDef spriteDefDown,
            SpriteDef spriteDefRight)
        {
            this.bounds = rect;
            SplitSprite(game.assets, spriteDefDown, rect.size.y, out spriteDown, out spriteDownOver);
            if (spriteDefRight != null)
                SplitSprite(game.assets, spriteDefRight, rect.size.x, out spriteRight, out spriteRightOver);
        }

        private void SplitSprite(AssetSrc assets, SpriteDef def,
            int size, out Sprite sprite, out Sprite spriteOver)
        {
            if (Tiling.SplitSprite(def, size, out var defLower, out var defUpper))
            {
                sprite = assets.sprites.Get(defLower);
                spriteOver = assets.sprites.Get(defUpper);
            }
            else
            {
                sprite = assets.sprites.Get(def);
                spriteOver = null;
            }
        }

        public abstract IBuilding CreateBuilding();
        public abstract string name { get; }
        public abstract bool passable { get; }

        public BuildingBounds Bounds(Dir dir) => bounds.RotatedFromDown(dir);

        private RenderFlags GetFlags(Sprite spriteOver)
            => spriteOver == null ? RenderFlags.None : RenderFlags.Oversized;

        private T Switch<T>(Dir dir, T down, T up, T right, T left)
        {
            if (dir == Dir.Down)
                return down;
            else if (dir == Dir.Up)
                return up;
            else
            {
                BB.AssertNotNull(spriteRight, "Rotated building did not provide side sprite.");
                return dir == Dir.Right ? right : left;
            }
        }

        private T Switch<T>(Dir dir, T down, T right)
            => Switch(dir, down, down, right, right);

        public RenderFlags GetFlags(Dir dir)
            => Switch(dir, GetFlags(spriteDownOver), GetFlags(spriteRightOver));

        private TileSprite Flip(Sprite sprite)
            => sprite == null ? sprite : TileSprite.FlippedX(sprite, Color.white);

        public TileSprite GetSprite(Dir dir, Vector2Int pos, Vector2Int subTile)
            => Switch(dir,
                spriteDown,
                Flip(spriteDown),
                spriteRight,
                Flip(spriteRight));

        public TileSprite GetSpriteOver(Dir dir, Vector2Int pos)
            => Switch(dir,
                spriteDownOver,
                Flip(spriteDownOver),
                spriteRightOver,
                Flip(spriteRightOver));
    }
}
