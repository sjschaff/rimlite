using System.Collections.Generic;
using UnityEngine;

public class AssetSrc
{
    private readonly Defs defs;

    // TODO: remove these
    public readonly Atlas tileset32;
    public readonly Atlas tileset64;
    public readonly Atlas sprites32;
    public readonly Atlas sprites64;

    private readonly Dictionary<string, Atlas> atlases
        = new Dictionary<string, Atlas>();

    public AssetSrc(Defs defs)
    {
        this.defs = defs;

        tileset32 = Atlas("BB:tileset32");
        tileset64 = Atlas("BB:tileset64");
        sprites32 = Atlas("BB:sprites32");
        sprites64 = Atlas("BB:sprites64");
    }

    public Atlas Atlas(string name)
    {
        if (!atlases.TryGetValue(name, out var atlas))
        { 
            AtlasDef def = defs.Atlas(name);
            atlas = new Atlas(Resources.Load<Texture2D>(def.file), def.pixelsPerTile, def.pixelsPerUnit);
            atlases[name] = atlas;
        }

        return atlas;
    }
}
