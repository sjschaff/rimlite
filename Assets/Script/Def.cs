using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public abstract class Def
    {
        public readonly string defType;
        public readonly string defName;

        protected Def(string defType, string defName)
        {
            this.defType = defType;
            this.defName = defName;
        }

        public override string ToString() => $"<{defType}|{defName}>";
    }

    public abstract class DefNamed : Def
    {
        // TODO: localization def (which could contain defs inside, i.e. '<material> Brick Floor'
        public readonly string name;

        protected DefNamed(string defType, string defName, string name)
            : base(defType, defName) => this.name = name;
    }

    public class TypeDef : Def
    {
        public readonly Type type;

        public TypeDef(Type type) : base("BB:Type", type.FullName) => this.type = type;
        public TypeDef(string typeName) : this(Type.GetType(typeName)) { }

        public static TypeDef Create<T>() => new TypeDef(typeof(T));

        public static implicit operator Type(TypeDef t) => t.type;
    }

    public class AtlasDef : Def
    {
        public readonly string file;
        public readonly int pixelsPerTile;
        public readonly int pixelsPerUnit;
        public readonly int tilesPerUnit;

        public AtlasDef(string defName, string file, int pixelsPerTile, int pixelsPerUnit)
            : base("BB:Atlas", defName)
        {
            BB.Assert(pixelsPerUnit > pixelsPerTile);
            BB.Assert((pixelsPerUnit % pixelsPerTile) == 0);
            this.file = file;
            this.pixelsPerTile = pixelsPerTile;
            this.pixelsPerUnit = pixelsPerUnit;
            this.tilesPerUnit = pixelsPerUnit / pixelsPerTile;
        }
    }

    public class SpriteDef : Def
    {
        public readonly AtlasDef atlas;
        public readonly Atlas.Rect rect;

        public Vec2 size => (Vec2)rect.size / (float)atlas.tilesPerUnit;

        public SpriteDef(string defName, AtlasDef atlas, Atlas.Rect rect)
            : base("BB:Sprite", defName)
        {
            this.atlas = atlas;
            this.rect = rect;
        }

        public SpriteDef(string defName, AtlasDef atlas, Vec2I origin, Vec2I size, Vec2I anchor)
            : this(defName, atlas, new Atlas.Rect(origin, size, anchor)) { }
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
        public readonly int maxStack;

        public ItemDef(
            string defName, string name, SpriteDef icon,
            int maxStack)
            : base("BB:Item", defName, name)
        {
            this.sprite = icon;
            this.maxStack = maxStack;
        }
    }

    public class BldgProtoDef : Def
    {
        public readonly TypeDef protoType;
        public readonly TypeDef protoDefType;

        public BldgProtoDef(TypeDef protoType, TypeDef protoDefType)
            : base("BB:Proto", protoType.defName)
        {
            this.protoType = protoType;
            this.protoDefType = protoDefType;
        }

        public static BldgProtoDef Create<TProto, TDef>()
            where TDef : BldgDefG<TDef>
        {
            var def = new BldgProtoDef(TypeDef.Create<TProto>(), TypeDef.Create<TDef>());
            BldgDefG<TDef>.SetProto(def);
            return def;
        }
    }

    public abstract class BldgDef : DefNamed
    {
        public abstract BldgProtoDef proto { get; }

        protected BldgDef(string defType, string defName, string name)
            : base(defType, defName, name) { }
    }

    public class BldgDefG<TBldgDef> : BldgDef
        where TBldgDef : BldgDefG<TBldgDef>
    {
        private static BldgProtoDef protoInternal;

        public static void SetProto(BldgProtoDef def)
        {
            BB.AssertNotNull(def);
            BB.AssertNull(protoInternal,
                $"building prototype def <{def.defName}, {def.protoDefType.defName}> declared twice");
            protoInternal = def;
        }

        public override BldgProtoDef proto => protoInternal;

        public BldgDefG(string defType, string defName, string name)
            : base(defType, defName, name)
        {
            BB.AssertNotNull(proto, $"building def {defType}<{defName}> declared before proto def");
        }
    }

    public class BldgMineableDef : BldgDefG<BldgMineableDef>
    {
        public readonly Tool tool;
        public readonly ItemInfo[] resources;
        public readonly SpriteDef sprite;

        public BldgMineableDef(
            string defName, string name,
            Tool tool, ItemInfo[] resources,
            SpriteDef sprite)
            : base("BB:BldgResource", defName, name)
        {
            this.tool = tool;
            this.resources = resources;
            this.sprite = sprite;
        }

        public BldgMineableDef(
            string defName, string name,
            Tool tool, ItemInfo resource,
            SpriteDef sprite)
            : this(defName, name, tool, new ItemInfo[] { resource }, sprite) { }
    }

    public class BldgFloorDef : BldgDefG<BldgFloorDef>
    {
        public readonly ItemInfo[] materials;
        public readonly AtlasDef atlas;
        public readonly Vec2I spriteOrigin;

        public BldgFloorDef(
            string defName, string name, ItemInfo[] materials,
            AtlasDef atlas, Vec2I spriteOrigin)
            : base("BB:Floor", defName, name)
        {
            this.materials = materials;
            this.atlas = atlas;
            this.spriteOrigin = spriteOrigin;
        }
    }

    public class BldgWallDef : BldgDefG<BldgWallDef>
    {
        public readonly ItemInfo[] materials;
        public readonly AtlasDef atlas;
        public readonly Vec2I spriteOrigin;

        public BldgWallDef(
            string defName, string name, ItemInfo[] materials,
            AtlasDef atlas, Vec2I spriteOrigin)
            : base("BB:Wall", defName, name)
        {
            this.materials = materials;
            this.atlas = atlas;
            this.spriteOrigin = spriteOrigin;
        }
    }

    public class BldgWorkbenchDef : BldgDefG<BldgWorkbenchDef>
    {
        public readonly BuildingBounds bounds;
        public readonly Vec2I workSpot;
        public readonly SpriteDef spriteDown;
        public readonly SpriteDef spriteRight;
        public readonly ItemInfo[] materials;
        // TODO: recipes

        public BldgWorkbenchDef(
            string defName, string name,
            BuildingBounds bounds, Vec2I workSpot,
            SpriteDef spriteDown, SpriteDef spriteRight,
            ItemInfo[] materials)
            : base("BB:Workbench", defName, name)
        {
            BB.Assert(bounds.IsAdjacent(workSpot));
            this.bounds = bounds;
            this.workSpot = workSpot;
            this.spriteDown = spriteDown;
            this.spriteRight = spriteRight;
            this.materials = materials;
        }
    }

    public class BldgConstructionDef : DefNamed
    {
        public readonly IBuildable proto;

        public BldgConstructionDef(IBuildable proto) : base(
            "BB:Construction",
            proto.GetType().Name + "Construct",
            "Construction: " + proto.buildingDef.name)
            => this.proto = proto;
    }

    // TODO: alls these building defs are getting quite unwieldy
    // TODO: maybe some sort of generic building, with modules,
    // i.e minable, has recipes, tiledrender/sprite render,
    // constructable, terrain gen-able, etc.

    // TODO: who knows
    public class AgentDef : DefNamed
    {
        public AgentDef(string defName, string name)
            : base("BB:Agent", defName, name) { }
    }

    public class MinionDef : AgentDef
    {
        public MinionDef(string defName, string name)
            : base(defName, name) { }

    }

    public class Defs
    {
        public Defs()
        {
            // TODO: something neat where these are loaded dynamically
            var tileset32 = Register(new AtlasDef("BB:tileset32", "tileset32", 16, 32));
            var tileset64 = Register(new AtlasDef("BB:tileset64", "tileset64", 32, 64));
            var sprites32 = Register(new AtlasDef("BB:sprites32", "sprites32", 8, 32));
            var sprites64 = Register(new AtlasDef("BB:sprites64", "sprites64", 16, 64));

            Register(new TerrainDef("BB:Grass", "Grass", tileset32, new Vec2I(0, 29)));
            Register(new TerrainDef("BB:Dirt", "Dirt", tileset32, new Vec2I(0, 0)));
            Register(new TerrainDef("BB:Mud", "Mud", tileset32, new Vec2I(0, 26)));
            Register(new TerrainDef("BB:Path", "Path", tileset32, new Vec2I(0, 23)));
            Register(new TerrainDef("BB:Water", "Water", tileset32, new Vec2I[] {
                    new Vec2I(26, 29),
                    new Vec2I(26, 26),
                    new Vec2I(26, 23),
                    new Vec2I(26, 20),
                    new Vec2I(26, 17),
                    new Vec2I(26, 14),
                    new Vec2I(26, 11),
                    new Vec2I(26, 8),
                }, false));

            Register(new SpriteDef("BB:Stone", sprites32, Vec2I.zero, new Vec2I(2, 2), Vec2I.one));
            Register(new SpriteDef("BB:Wood", sprites32, new Vec2I(2, 0), new Vec2I(2, 2), Vec2I.one));
            Register(new SpriteDef("BB:BldgRock", sprites32, new Vec2I(0, 18), new Vec2I(4, 4), Vec2I.zero));
            Register(new SpriteDef("BB:BldgTree", sprites32, new Vec2I(0, 4), new Vec2I(8, 14), new Vec2I(2, 0)));
            Register(new SpriteDef("BB:WoodcuttingTableD", sprites32, new Vec2I(10, 26), new Vec2I(12, 5), new Vec2I(4, 0)));
            Register(new SpriteDef("BB:WoodcuttingTableR", sprites32, new Vec2I(3, 26), new Vec2I(6, 12), new Vec2I(1, 4)));
            Register(new SpriteDef("BB:MineIcon", sprites32, new Vec2I(0, 62), new Vec2I(2, 2), Vec2I.one));
            Register(new SpriteDef("BB:BuildIcon", sprites64, new Vec2I(0, 30), new Vec2I(2, 2), Vec2I.one));
            Register(new SpriteDef("BB:CancelIcon", sprites64, new Vec2I(2, 30), new Vec2I(2, 2), Vec2I.one));
            Register(new SpriteDef("BB:PlayIcon", sprites64, new Vec2I(8, 28), new Vec2I(4, 4), Vec2I.zero));
            Register(new SpriteDef("BB:PauseIcon", sprites64, new Vec2I(12, 28), new Vec2I(4, 4), Vec2I.zero));
            Register(new SpriteDef("BB:PlayFFIcon", sprites64, new Vec2I(16, 28), new Vec2I(6, 4), Vec2I.zero));
            Register(new SpriteDef("BB:PlaySFFIcon", sprites64, new Vec2I(22, 28), new Vec2I(10, 4), Vec2I.zero));

            Register(new ItemDef("BB:Stone", "Stone", Get<SpriteDef>("BB:Stone"), 5));
            Register(new ItemDef("BB:Wood", "Wood", Get<SpriteDef>("BB:Wood"), 5));


            Register(BldgProtoDef.Create<BuildingProtoResource, BldgMineableDef>());
            Register(new BldgMineableDef("BB:Rock", "Rock", Tool.Pickaxe,
                new ItemInfo(Get<ItemDef>("BB:Stone"), 36),
                Get<SpriteDef>("BB:BldgRock")));

            Register(new BldgMineableDef("BB:Tree", "Tree", Tool.Axe,
                new ItemInfo(Get<ItemDef>("BB:Wood"), 25),
                Get<SpriteDef>("BB:BldgTree")));

            Register(BldgProtoDef.Create<BuildingProtoFloor, BldgFloorDef>());
            Register(new BldgFloorDef("BB:StoneBrick", "Stone Brick Floor",
                 new[] { new ItemInfo(Get<ItemDef>("BB:Stone"), 5) },
               tileset64, new Vec2I(0, 9)));

            Register(BldgProtoDef.Create<BuildingProtoWall, BldgWallDef>());
            Register(new BldgWallDef("BB:StoneBrick", "Stone Brick Wall",
                new[] { new ItemInfo(Get<ItemDef>("BB:Stone"), 10) },
                tileset64, new Vec2I(0, 12)));

            Register(BldgProtoDef.Create<BuildingProtoWorkbench, BldgWorkbenchDef>());
            Register(new BldgWorkbenchDef("BB:Woodcutter", "Woodcutting Table",
                new BuildingBounds(new Vec2I(3, 1), new Vec2I(1, 0)), new Vec2I(1, -1),
                Get<SpriteDef>("BB:WoodcuttingTableD"),
                Get<SpriteDef>("BB:WoodcuttingTableR"),
                new[] { new ItemInfo(Get<ItemDef>("BB:Wood"), 10) }));
        }

        private interface IDefList : IEnumerable<Def>
        {
            bool ContainsType(Type type);
        }

        public class DefList<TDef> : IDefList where TDef : Def
        {
            private readonly Dictionary<string, TDef> defs
                = new Dictionary<string, TDef>();

            public bool ContainsType(Type type)
                => type == typeof(TDef) || typeof(TDef).IsSubclassOf(type);

            public void Register(TDef def)
            {
                BB.Assert(!defs.ContainsKey(def.defName));
                defs.Add(def.defName, def);
            }

            // TODO: is this boxing or something? what is this type wizardry?
            IEnumerator<Def> IEnumerable<Def>.GetEnumerator()
                => defs.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => defs.Values.GetEnumerator();

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

        private TDef Register<TDef>(TDef def) where TDef : Def
        {
            DefList<TDef> list;
            if (lists.TryGetValue(typeof(TDef), out var listUntyped))
                list = (DefList<TDef>)listUntyped;
            else
                list = RegisterDefType<TDef>();

            list.Register(def);
            return def;
        }

        public TDef Get<TDef>(string name) where TDef : Def
            => GetList<TDef>()[name];

        public IEnumerable<TDef> GetDefs<TDef>() where TDef : Def
        {
            foreach (var list in lists.Values)
            {
                if (list.ContainsType(typeof(TDef)))
                {
                    foreach (var def in list)
                        yield return (TDef)def;
                }
            }
        }
    }
}