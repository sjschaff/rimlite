using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

public class Atlas
{
    public struct Key
    {
        public readonly Vec2I origin;
        public readonly Vec2I size;
        public readonly Vec2I anchor;

        public Key(Vec2I origin, Vec2I size, Vec2I anchor)
        {
            this.origin = origin;
            this.size = size;
            this.anchor = anchor;
        }
    }

    private readonly Texture2D atlas;
    private readonly Cache<Key, Sprite> spriteCache;
    private readonly int tileSize;
    private readonly int ppu;

    public Atlas(Texture2D atlas, int tileSize, int ppu)
    {
        this.atlas = atlas;
        spriteCache = new Cache<Key, Sprite>(key => CreateSprite(key));
        this.tileSize = tileSize;
        this.ppu = ppu;
    }

    public Sprite GetSprite(Vec2I origin, Vec2I size)
        => GetSprite(origin, size, Vec2I.zero);

    public Sprite GetSprite(Vec2I origin, Vec2I size, Vec2I anchor)
        => GetSprite(new Key(origin, size, anchor));

    public Sprite GetSprite(Key key) => spriteCache.Get(key);

    private Sprite CreateSprite(Key key)
    {
        Vec2 anchor = new Vec2(key.anchor.x / (float)key.size.x, key.anchor.y / (float)key.size.y);
        return Sprite.Create(atlas,
            new Rect(key.origin * tileSize, key.size * tileSize),
            anchor,
            ppu,
            0, // TODO: 0, 1, 2, 4?
            SpriteMeshType.FullRect,
            Vector4.zero,
            false);
    }
}

