using System.Collections.Generic;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

/* Ideas/concepts *
   multi-levels
        reuire walls/supports underneath other walls suppors
   schools of magic -> schools of research
        specialized mages req. to research different trees
   portals to other planes/dimensions possible based on schools of magic
        rare / lategame resources found in planes, unlocks high tier research
        example, capture elementals and study them or something
        maybe early usage allows limited access, i.e planes touch but dont connect
            to allow for heaters/coolers, then later access allows travel between,
            dangerous but allows cooler stuff
   send adventuring parties to explore planes, caves, mines, dungeons, all on 1 map
   lots of concepts from dnd
        stock adventuring kits for sale
        sell magic items, potions etc.
        sell access to teleportaion circle
            private/public circles?
        library access
            buy books from travelers, advance research or maybe unlock special things
   ability to house travelers/adventurers
   random events
        mine infestations
        portal breach
        angry peasants
        attack via tele. circle.
        cave ins
        later game magical beasts attack, eventually dragons
*/

namespace BB
{
    public partial class Game
    {
        public readonly AssetSrc assets;
        public readonly Registry registry;

        public Defs defs => registry.defs;

        private readonly Transform gameContainer;
        public readonly Transform itemContainer;
        public readonly Transform agentContainer;
        public readonly Transform workOverlays;

        private readonly LinkedList<Minion> minions = new LinkedList<Minion>();
        private readonly Minion D_minionNoTask;

        public Game(Registry registry, AssetSrc assets)
        {
            this.registry = registry;
            this.assets = assets;
            // TODO: initialization order is getting wonky
            registry.LoadTypes(this);

            gameContainer = new GameObject("Game").transform;
            itemContainer = CreateContainer("Items");
            agentContainer = CreateContainer("Agents");
            workOverlays = CreateContainer("Work Overlays");

            map = new Map(this);
            map.InitDebug(new Vec2I(128, 128));

            for (int i = 0; i < 10; ++i)
                minions.AddLast(new Minion(this, new Vec2I(1 + i, 1)));
            D_minionNoTask = minions.First.Value;
        }

        private Transform CreateContainer(string name)
        {
            var container = new GameObject(name).transform;
            container.SetParent(gameContainer, false);
            return container;
        }

        public void K_MoveMinion(Vec2I pos)
            => D_minionNoTask.AssignWork(SystemWalkDummy.Create(
                new TaskGoTo(this, "Debug Walking.", PathCfg.Point(pos))));

        public void Update(float dt)
        {
            foreach (Minion minion in minions)
            {
                if (minion == D_minionNoTask)
                    continue;

                if (!minion.hasWork)
                {
                    foreach (var system in registry.systems)
                        foreach (var work in system.QueryWork())
                        {
                            if (minion.AssignWork(work))
                                break;
                        }
                }
            }

            foreach (var minion in minions)
                minion.Update(dt);
        }
    }
}