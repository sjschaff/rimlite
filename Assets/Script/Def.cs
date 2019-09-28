using System.Collections.Generic;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;
public class Def
{
    public readonly string type;
    public readonly string name;

    protected Def(string type, string name)
    {
        this.type = type;
        this.name = name;
    }
}

public class AtlasDef : Def
{
    public readonly string file;
    public readonly int pixelsPerTile;
    public readonly int pixelsPerUnit;

    public AtlasDef(string name, string file, int pixelsPerTile, int pixelsPerUnit)
        : base("BB:Atlas", name)
    {
        this.file = file;
        this.pixelsPerTile = pixelsPerTile;
        this.pixelsPerUnit = pixelsPerUnit;
    }
}

public class TerrainDef : Def
{
    public readonly string atlas;
    public readonly Vec2I[] spriteFrames;
    public readonly bool passable;

    public TerrainDef(string name, string atlas, Vec2I[] spriteFrames, bool passable = true)
        : base("BB:Terrain", name)
    {
        BB.Assert(spriteFrames != null);
        BB.Assert(spriteFrames.Length > 0);

        this.atlas = atlas;
        this.spriteFrames = spriteFrames;
        this.passable = passable;
    }

    public TerrainDef(string name, string atlas, Vec2I spriteOrigin)
        : this(name, atlas, new Vec2I[] { spriteOrigin }) { }
}

public class Defs
{
    private readonly Dictionary<string, AtlasDef> atlasDefs
        = new Dictionary<string, AtlasDef>();

    private readonly Dictionary<string, TerrainDef> terrainDefs
        = new Dictionary<string, TerrainDef>();

    public Defs()
    {
        // TODO: something neat where these are loaded dynamically
        RegisterDef(new AtlasDef("BB:tileset32", "tileset32", 16, 32));
        RegisterDef(new AtlasDef("BB:tileset64", "tileset64", 32, 64));
        RegisterDef(new AtlasDef("BB:sprites32", "sprites32", 8, 32));
        RegisterDef(new AtlasDef("BB:sprites64", "sprites64", 16, 64));

        RegisterDef(new TerrainDef("BB:Grass", "BB:tileset32", new Vec2I(0, 29)));
        RegisterDef(new TerrainDef("BB:Dirt", "BB:tileset32", new Vec2I(0, 0)));
        RegisterDef(new TerrainDef("BB:Mud", "BB:tileset32", new Vec2I(0, 26)));
        RegisterDef(new TerrainDef("BB:Path", "BB:tileset32", new Vec2I(0, 23)));
        RegisterDef(new TerrainDef("BB:Water", "BB:tileset32", new Vec2I[]
        {
            new Vec2I(26, 29),
            new Vec2I(26, 26),
            new Vec2I(26, 23),
            new Vec2I(26, 20),
            new Vec2I(26, 17),
            new Vec2I(26, 14),
            new Vec2I(26, 11),
            new Vec2I(26, 8),
        }, false));
    }

    // TODO: error checking here for missing def
    public TerrainDef Terrain(string name) => terrainDefs[name];
    public AtlasDef Atlas(string name) => atlasDefs[name];

    private static void RegisterDef<TDef>(TDef def, Dictionary<string, TDef> defs)
        where TDef : Def
    {
        BB.Assert(!defs.ContainsKey(def.name));
        defs.Add(def.name, def);
    }

    private void RegisterDef(AtlasDef def) => RegisterDef(def, atlasDefs);
    private void RegisterDef(TerrainDef def) => RegisterDef(def, terrainDefs);
}
