using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

public abstract class Atlas<Key>
{
    protected Texture2D atlas;
    private Dictionary<Key, Sprite> spriteCache;

    protected Atlas(Texture2D atlas)
    {
        this.atlas = atlas;
        spriteCache = new Dictionary<Key, Sprite>();
    }

    public Sprite GetSprite(Key key)
    {
        if (spriteCache.TryGetValue(key, out var ret))
            return ret;

        ret = CreateSprite(key);
        spriteCache.Add(key, ret);
        return ret;
    }

    protected abstract Sprite CreateSprite(Key key);
}

public class _Atlas
{
    protected struct Key
    {
        public readonly Vec2I origin;
        public readonly Vec2I size;
        public readonly float ppu;

        public Key(Vec2I origin, Vec2I size, float ppu)
        {
            this.origin = origin;
            this.size = size;
            this.ppu = ppu;
        }
    }

    private readonly Texture2D atlas;
    private readonly Dictionary<Key, Sprite> spriteCache;
    private readonly int tileSize;

    public _Atlas(Texture2D atlas, int tileSize)
    {
        this.atlas = atlas;
        spriteCache = new Dictionary<Key, Sprite>();
        this.tileSize = tileSize;
    }

    public Sprite GetSprite(Vec2I origin, Vec2I size, float ppu)
        => GetSprite(new Key(origin, size, ppu));

    private Sprite GetSprite(Key key)
    {
        if (spriteCache.TryGetValue(key, out var ret))
            return ret;

        ret = CreateSprite(key);
        spriteCache.Add(key, ret);
        return ret;
    }

    private Sprite CreateSprite(Key key)
    {
        return Sprite.Create(atlas,
            new Rect(key.origin * tileSize, key.size * tileSize),
            Vec2.zero,
            key.ppu,
            0, // TODO: 0, 1, 2, 4?
            SpriteMeshType.FullRect,
            Vector4.zero,
            false);
    }
}

