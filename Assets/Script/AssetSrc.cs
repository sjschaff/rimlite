using UnityEngine;

namespace BB {

public class AssetSrc
{
    public readonly Cache<AtlasDef, Atlas> atlases;
    public readonly Cache<SpriteDef, Sprite> sprites;

    public AssetSrc()
    {
        atlases = new Cache<AtlasDef, Atlas>(
            def => new Atlas(Resources.Load<Texture2D>(def.file), def.pixelsPerTile, def.pixelsPerUnit));

        sprites = new Cache<SpriteDef, Sprite>(
            def => atlases.Get(def.atlas).GetSprite(def.key));
    }
}

}
