using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
