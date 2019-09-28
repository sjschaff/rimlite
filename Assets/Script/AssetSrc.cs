using System.Collections.Generic;
using UnityEngine;

public class AssetSrc
{
    public readonly Cache<AtlasDef, Atlas> atlases;
    public readonly Cache<SpriteDef, Sprite> sprites;

    public AssetSrc(Defs defs)
    {
        atlases = new Cache<AtlasDef, Atlas>(
            def => new Atlas(Resources.Load<Texture2D>(def.file), def.pixelsPerTile, def.pixelsPerUnit));

        sprites = new Cache<SpriteDef, Sprite>(
            def => atlases.Get(def.atlas).GetSprite(def.key));
    }
}
