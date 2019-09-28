using System;
using System.Collections.Generic;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

public abstract class Def
{
    public readonly string defType;
    public readonly string defName;

    protected Def(string defType, string defName)
    {
        this.defType = defType;
        this.defName = defName;
    }
}

public abstract class DefNamed : Def
{
    // TODO: localization def (which could contain defs inside, i.e. '<material> Brick Floor'
    public readonly string name;

    protected DefNamed(string defType, string defName, string name)
        : base(defType, defName) => this.name = name;
}

public class AtlasDef : Def
{
    public readonly string file;
    public readonly int pixelsPerTile;
    public readonly int pixelsPerUnit;

    public AtlasDef(string defName, string file, int pixelsPerTile, int pixelsPerUnit)
        : base("BB:Atlas", defName)
    {
        this.file = file;
        this.pixelsPerTile = pixelsPerTile;
        this.pixelsPerUnit = pixelsPerUnit;
    }
}

public class SpriteDef : Def
{
    public readonly AtlasDef atlas;
    public readonly Atlas.Key key;

    public SpriteDef(string defName, AtlasDef atlas, Atlas.Key key)
        : base("BB:Sprite", defName)
    {
        this.atlas = atlas;
        this.key = key;
    }

    public SpriteDef(string defName, AtlasDef atlas, Vec2I origin, Vec2I size, Vec2I anchor)
        : this(defName, atlas, new Atlas.Key(origin, size, anchor)) { }
}

public class TerrainDef : DefNamed
{
    public readonly AtlasDef atlas;
    public readonly Vec2I[] spriteFrames;
    public readonly bool passable;

    public TerrainDef(string defName, string name,
        AtlasDef atlas, Vec2I[] spriteFrames, bool passable = true)
        : base("BB:Terrain", defName, name)
    {
        BB.Assert(spriteFrames != null);
        BB.Assert(spriteFrames.Length > 0);

        this.atlas = atlas;
        this.spriteFrames = spriteFrames;
        this.passable = passable;
    }

    public TerrainDef(string defName, string name,
        AtlasDef atlas, Vec2I spriteOrigin)
        : this(defName, name, atlas, new Vec2I[] { spriteOrigin }) { }
}

public class ItemDef : DefNamed
{
    public readonly SpriteDef sprite;

    public ItemDef(string defName, string name, SpriteDef icon)
        : base("BB:Item", defName, name) => this.sprite = icon;

}

public class BldgMineableDef : DefNamed
{
    public readonly Tool tool;
    public readonly ItemInfoRO[] resources;
    public readonly SpriteDef sprite;
    public readonly SpriteDef spriteOver;

    public BldgMineableDef(
        string defName, string name,
        Tool tool, ItemInfoRO[] resources,
        SpriteDef sprite, SpriteDef spriteOver = null)
        : base("BB:BldgResource", defName, name)
    {
        this.tool = tool;
        this.resources = resources;
        this.sprite = sprite;
        this.spriteOver = spriteOver;
    }

    public BldgMineableDef(
        string defName, string name,
        Tool tool, ItemInfoRO resource,
        SpriteDef sprite, SpriteDef spriteOver = null)
        : this(defName, name, tool, new ItemInfoRO[] { resource }, sprite, spriteOver) { }
}

public class BldgFloorDef : DefNamed
{
    public readonly ItemInfoRO[] materials;
    public readonly AtlasDef atlas;
    public readonly Vec2I spriteOrigin;

    public BldgFloorDef(string defName, string name, ItemInfoRO[] materials, AtlasDef atlas, Vec2I spriteOrigin)
        : base("BB:Floor", defName, name)
    {
        this.materials = materials;
        this.atlas = atlas;
        this.spriteOrigin = spriteOrigin;
    }

    public BldgFloorDef(string defName, string name, ItemInfoRO material, AtlasDef atlas, Vec2I spriteOrigin)
        : this(defName, name, new ItemInfoRO[] { material }, atlas, spriteOrigin) { }
}

public class BldgWallDef : DefNamed
{
    public readonly ItemInfoRO[] materials;
    public readonly AtlasDef atlas;
    public readonly Vec2I spriteOrigin;

    public BldgWallDef(string defName, string name, ItemInfoRO[] materials, AtlasDef atlas, Vec2I spriteOrigin)
        : base("BB:Wall", defName, name)
    {
        this.materials = materials;
        this.atlas = atlas;
        this.spriteOrigin = spriteOrigin;
    }

    public BldgWallDef(string defName, string name, ItemInfoRO material, AtlasDef atlas, Vec2I spriteOrigin)
        : this(defName, name, new ItemInfoRO[] { material }, atlas, spriteOrigin) { }
}

// TODO: alls these building defs are getting quite unwieldy
// TODO: maybe some sort of generic building, with modules,
// i.e minable, has recipes, tiledrender/sprite render,
// constructable, terrain gen-able, etc.



public class Defs
{
    public Defs()
    {
        // TODO: something neat where these are loaded dynamically
        Register(new AtlasDef("BB:tileset32", "tileset32", 16, 32));
        Register(new AtlasDef("BB:tileset64", "tileset64", 32, 64));
        Register(new AtlasDef("BB:sprites32", "sprites32", 8, 32));
        Register(new AtlasDef("BB:sprites64", "sprites64", 16, 64));

        Register(new TerrainDef("BB:Grass", "Grass", Get<AtlasDef>("BB:tileset32"), new Vec2I(0, 29)));
        Register(new TerrainDef("BB:Dirt", "Dirt", Get<AtlasDef>("BB:tileset32"), new Vec2I(0, 0)));
        Register(new TerrainDef("BB:Mud", "Mud", Get<AtlasDef>("BB:tileset32"), new Vec2I(0, 26)));
        Register(new TerrainDef("BB:Path", "Path", Get<AtlasDef>("BB:tileset32"), new Vec2I(0, 23)));
        Register(new TerrainDef("BB:Water", "Water", Get<AtlasDef>("BB:tileset32"), new Vec2I[]
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

        Register(new SpriteDef("BB:Stone", Get<AtlasDef>("BB:sprites32"), Vec2I.zero, new Vec2I(2, 2), Vec2I.one));
        Register(new SpriteDef("BB:Wood", Get<AtlasDef>("BB:sprites32"), new Vec2I(2, 0), new Vec2I(2, 2), Vec2I.one));
        Register(new SpriteDef("BB:BldgRock", Get<AtlasDef>("BB:sprites32"), new Vec2I(0, 18), new Vec2I(4, 4), Vec2I.zero));
        Register(new SpriteDef("BB:BldgTree", Get<AtlasDef>("BB:sprites32"), new Vec2I(2, 4), new Vec2I(4, 4), Vec2I.zero));
        Register(new SpriteDef("BB:BldgTreeOver", Get<AtlasDef>("BB:sprites32"), new Vec2I(0, 8), new Vec2I(8, 10), new Vec2I(2, -4)));
        Register(new SpriteDef("BB:MineOverlay", Get<AtlasDef>("BB:sprites32"), new Vec2I(0, 62), new Vec2I(2, 2), Vec2I.one));

        Register(new ItemDef("BB:Stone", "Stone", Get<SpriteDef>("BB:Stone")));
        Register(new ItemDef("BB:Wood", "Wood", Get<SpriteDef>("BB:Wood")));


        Register(new BldgMineableDef("BB:Rock", "Rock", Tool.Pickaxe,
            new ItemInfoRO(Get<ItemDef>("BB:Stone"), 36),
            Get<SpriteDef>("BB:BldgRock")));

        Register(new BldgMineableDef("BB:Tree", "Tree", Tool.Axe,
            new ItemInfoRO(Get<ItemDef>("BB:Wood"), 25),
            Get<SpriteDef>("BB:BldgTree"),
            Get<SpriteDef>("BB:BldgTreeOver")));

        Register(new BldgFloorDef("BB:StoneBrick", "Stone Brick Floor",
            new ItemInfoRO(Get<ItemDef>("BB:Stone"), 5),
            Get<AtlasDef>("BB:tileset64"), new Vec2I(0, 9)));

        Register(new BldgWallDef("BB:StoneBrick", "Stone Brick Wall",
            new ItemInfoRO(Get<ItemDef>("BB:Stone"), 10),
            Get<AtlasDef>("BB:tileset64"), new Vec2I(0, 12)));
    }

   // private Dictionary<Type, >

    private interface IDefList { }
    public class DefList<TDef> : IDefList where TDef : Def
    {
        private readonly Dictionary<string, TDef> defs
            = new Dictionary<string, TDef>();

        public void Register(TDef def)
        {
            BB.Assert(!defs.ContainsKey(def.defName));
            defs.Add(def.defName, def);
        }

        public TDef this[string defName]
        {
            get
            {
                if (defs.TryGetValue(defName, out TDef def))
                    return def;

                Debug.LogError("Missing " + typeof(TDef).FullName + " for '" + defName + "'");
                return null;
            }
        }
    }

    private readonly Dictionary<Type, IDefList> lists = new Dictionary<Type, IDefList>();

    private DefList<TDef> GetList<TDef>() where TDef : Def
        => (DefList<TDef>)lists[typeof(TDef)];

    private DefList<TDef> RegisterDefType<TDef>() where TDef : Def
    {
        var list = new DefList<TDef>();
        lists.Add(typeof(TDef), list);
        return list;
    }

    private void Register<TDef>(TDef def) where TDef : Def
    {
        DefList<TDef> list;
        if (lists.TryGetValue(typeof(TDef), out var listUntyped))
            list = (DefList<TDef>)listUntyped;
        else
            list = RegisterDefType<TDef>();

        list.Register(def);
    }

    public TDef Get<TDef>(string name) where TDef : Def
        => GetList<TDef>()[name];
}
