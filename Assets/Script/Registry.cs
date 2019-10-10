using System.Collections.Generic;
using System.Linq;
using System;

namespace BB
{
    public class Registry
    {
        public readonly Defs defs;

        // TODO: make these readonly, or at the very list private
        public readonly Dictionary<BldgDef, IBuildingProto> buildings
             = new Dictionary<BldgDef, IBuildingProto>();
        public readonly List<IGameSystem> systems
            = new List<IGameSystem>();
        public readonly List<IContextMenuProvider> contextProviders
            = new List<IContextMenuProvider>();

        public IBuildingProto D_GetProto<TDef>(string name) where TDef : BldgDef
            => buildings[defs.Get<TDef>(name)];

        public Registry() => defs = new Defs();

        public void LoadTypes(Game game)
        {
            InstantiateTypes(game, systems, "game system");
            InstantiateTypes(game, contextProviders, "context provider");

            // TODO: more evidence that these suck
            foreach (var def in defs.GetDefs<BldgDef>())
            {
                BldgProtoDef protoDef = def.proto;
                Type typeProto = protoDef.protoType;
                Type typeDef = protoDef.protoDefType;
                if (typeDef != def.GetType())
                {
                    BB.Assert(false, $"{def.GetType().Name}{def} registered with mismatched prototype (expected a {typeDef.Name}).");
                    continue;
                }

                var proto = TryInstantiate<IBuildingProto>("prototype", typeProto, game, def);
                if (proto != null)
                    buildings.Add(def, proto);
            }

            BB.LogInfo($"Found {buildings.Count} buildings:");
            foreach (var def in buildings.Keys)
                BB.LogInfo($"    {def}");
        }

        private static void InstantiateTypes<T>(Game game, List<T> list, string debugName)
            where T : class
        {
            foreach (var t in GetTypesForInterface<T>())
            {
                if (!t.GetCustomAttributes(typeof(AttributeDontInstantiate), false).Any())
                {
                    var instance = TryInstantiate<T>(debugName, t, game);
                    if (instance != null)
                        list.Add(instance);
                }
            }

            BB.LogInfo($"Found {list.Count} {debugName}s:");
            foreach (var t in list)
                BB.LogInfo($"    {t.GetType().FullName}");
        }

        private static T TryInstantiate<T>(string failName, Type type, params object[] args)
            where T : class
        {
            // TODO: use reflection to check first so we dont have to deal with exceptions
            try
            {
                return (T)Activator.CreateInstance(type, args);
            } catch (MissingMethodException)
            {
                string ctor = $"{type.Name}(";
                if (args.Length > 0)
                    ctor += args[0].GetType().Name;
                for (int i = 1; i < args.Length; ++i)
                    ctor += ", " + args[i].GetType().Name;
                ctor += ")";
                BB.LogWarning($"Failed to instatiate {failName} '{type.FullName}': " +
                   $"missing constructor {ctor}");
            }

            return null;
        }

        private static IEnumerable<Type> GetTypesForInterface<TInterface>()
            => AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => typeof(TInterface).IsAssignableFrom(x) && !x.IsInterface && ! x.IsAbstract);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AttributeDontInstantiate : Attribute { }
}