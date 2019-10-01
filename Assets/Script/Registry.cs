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

        public readonly List<IWorkSystem> systems = new List<IWorkSystem>();

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
            UnityEngine.Debug.Log("assembly: " + t.Assembly);
            UnityEngine.Debug.Log("name: " + t.Name);
            UnityEngine.Debug.Log("full name: " + t.FullName);
            UnityEngine.Debug.Log("assembly qualified:" + t.AssemblyQualifiedName);
            string typeName = t.AssemblyQualifiedName;
            UnityEngine.Debug.Log("type: " + Type.GetType(typeName));
            var d = defs.Get<BldgMineableDef>("BB:Rock");
            BuildingProtoResource b = (BuildingProtoResource)Activator.CreateInstance(Type.GetType(typeName), d);
            */

            foreach (var workSystem in GetTypesForInterface<IWorkSystem>())
            {
                try
                {
                    if (!workSystem.GetCustomAttributes(typeof(AttributeDontInstantiate), false).Any())
                        systems.Add((IWorkSystem)Activator.CreateInstance(workSystem, (object)game));
                } catch (MissingMethodException)
                {
                    UnityEngine.Debug.Log("Failed to instatiate work system '" + workSystem.FullName +
                        "': missing constructor taking single argument GameController");

                }
            }
            UnityEngine.Debug.Log("found " + systems.Count + " work systems.");
        }

        private IEnumerable<Type> GetTypesForInterface<TInterface>()
            => AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => typeof(TInterface).IsAssignableFrom(x) && !x.IsInterface && ! x.IsAbstract);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AttributeDontInstantiate : Attribute { }
}