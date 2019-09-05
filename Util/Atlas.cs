using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

public class Atlas
{
    public struct Key
    {
        public readonly Vec2I origin;
        public readonly Vec2I size;
        public readonly Vec2I anchor;
        public readonly float ppu;

        public Key(Vec2I origin, Vec2I size, Vec2I anchor, float ppu)
        {
            this.origin = origin;
            this.size = size;
            this.anchor = anchor;
            this.ppu = ppu;
        }
    }

    private readonly Texture2D atlas;
    private readonly Dictionary<Key, Sprite> spriteCache;
    private readonly int tileSize;

    public Atlas(Texture2D atlas, int tileSize)
    {
        this.atlas = atlas;
        spriteCache = new Dictionary<Key, Sprite>();
        this.tileSize = tileSize;
    }

    public Sprite GetSprite(Vec2I origin, Vec2I size, float ppu)
        => GetSprite(origin, size, Vec2I.zero, ppu);

    public Sprite GetSprite(Vec2I origin, Vec2I size, Vec2I anchor, float ppu)
        => GetSprite(new Key(origin, size, anchor, ppu));

    public Sprite GetSprite(Key key)
    {
        if (spriteCache.TryGetValue(key, out var ret))
            return ret;

        ret = CreateSprite(key);
        spriteCache.Add(key, ret);
        return ret;
    }

    private Sprite CreateSprite(Key key)
    {
        Vec2 anchor = new Vec2(key.anchor.x / (float)key.size.x, key.anchor.y / (float)key.size.y);
        return Sprite.Create(atlas,
            new Rect(key.origin * tileSize, key.size * tileSize),
            anchor,
            key.ppu,
            0, // TODO: 0, 1, 2, 4?
            SpriteMeshType.FullRect,
            Vector4.zero,
            false);
    }
}

