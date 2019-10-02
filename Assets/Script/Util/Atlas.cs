using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class Atlas
    {
        public struct Rect
        {
            public readonly Vec2I origin;
            public readonly Vec2I size;
            public readonly Vec2I anchor;

            public Rect(Vec2I origin, Vec2I size, Vec2I anchor)
            {
                this.origin = origin;
                this.size = size;
                this.anchor = anchor;
            }
        }

        private readonly Texture2D atlas;
        private readonly Cache<Rect, Sprite> spriteCache;
        private readonly int tileSize;
        private readonly int ppu;

        public Atlas(Texture2D atlas, int tileSize, int ppu)
        {
            this.atlas = atlas;
            spriteCache = new Cache<Rect, Sprite>(rect => CreateSprite(rect));
            this.tileSize = tileSize;
            this.ppu = ppu;
        }

        public Sprite GetSprite(Vec2I origin, Vec2I size)
            => GetSprite(origin, size, Vec2I.zero);

        public Sprite GetSprite(Vec2I origin, Vec2I size, Vec2I anchor)
            => GetSprite(new Rect(origin, size, anchor));

        public Sprite GetSprite(Rect rect) => spriteCache.Get(rect);

        private Sprite CreateSprite(Rect rect)
        {
            Vec2 anchor = new Vec2(rect.anchor.x / (float)rect.size.x, rect.anchor.y / (float)rect.size.y);
            return Sprite.Create(atlas,
                new UnityEngine.Rect(rect.origin * tileSize, rect.size * tileSize),
                anchor,
                ppu,
                0, // TODO: 0, 1, 2, 4?
                SpriteMeshType.FullRect,
                Vector4.zero,
                false);
        }
    }
}