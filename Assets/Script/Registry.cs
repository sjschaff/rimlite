using System.Collections.Generic;
using System.Linq;
using System;

namespace BB
{
    public class Registry
    {
        public readonly GameController game;
        public readonly Defs defs;

        // TODO: something more general/interesting
        // also not cache since, everything should be registered up front
        public readonly Cache<BldgMineableDef, BuildingProtoResource> resources;
        public readonly Cache<BldgFloorDef, BuildingProtoFloor> floors;
        public readonly Cache<BldgWallDef, BuildingProtoWall> walls;

        public readonly List<IGameSystem> systems = new List<IGameSystem>();

        public Registry(GameController game)
        {
            this.game = game;
            defs = new Defs();

            resources = new Cache<BldgMineableDef, BuildingProtoResource>(
                def => new BuildingProtoResource(def));
            floors = new Cache<BldgFloorDef, BuildingProtoFloor>(
                def => new BuildingProtoFloor(def));
            walls = new Cache<BldgWallDef, BuildingProtoWall>(
                def => new BuildingProtoWall(def));
        }

        public void LoadTypes()
        {
            // test
            /*var t = typeof(BuildingProtoResource);
            BB.Log("assembly: " + t.Assembly);
            BB.Log("name: " + t.Name);
            BB.Log("full name: " + t.FullName);
            BB.Log("assembly qualified:" + t.AssemblyQualifiedName);
            string typeName = t.AssemblyQualifiedName;
            BB.Log("type: " + Type.GetType(typeName));
            var d = defs.Get<BldgMineableDef>("BB:Rock");
            BuildingProtoResource b = (BuildingProtoResource)Activator.CreateInstance(Type.GetType(typeName), d);
            */

            foreach (var workSystem in GetTypesForInterface<IGameSystem>())
            {
                try
                {
                    if (!workSystem.GetCustomAttributes(typeof(AttributeDontInstantiate), false).Any())
                        systems.Add((IGameSystem)Activator.CreateInstance(workSystem, (object)game));
                } catch (MissingMethodException)
                {
                    BB.LogWarning("Failed to instatiate work system '" + workSystem.FullName +
                        "': missing constructor taking single argument GameController");

                }
            }
            BB.LogInfo("Found " + systems.Count + " work systems:");
            foreach (var system in systems)
                BB.LogInfo("    " + system.GetType().FullName);
        }

        private IEnumerable<Type> GetTypesForInterface<TInterface>()
            => AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => typeof(TInterface).IsAssignableFrom(x) && !x.IsInterface && ! x.IsAbstract);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AttributeDontInstantiate : Attribute { }
}