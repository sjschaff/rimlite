using System.Collections.Generic;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
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
    managers for assiging work
        maybe they speed up underlings
        maybe as a req. for directly prioritizing tasks
        or certain recipe types i.e. make until x
    apprenticeship needed to learn a new skill (or smart+books? workaround with magic perhaps)
        slows down mentor, consumes materials without producing anything
    workspeed logarithmic with skill level
    architecture drawings as an alternative style of research, needed to make new buildings/benches
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
        public readonly Transform effectsContainer;

        private readonly LinkedList<Minion> minions = new LinkedList<Minion>();
        private readonly DeferredSet<Effect> effects =
            new DeferredSet<Effect>(e => e.Destroy());

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

            assets.CreateSpriteObject(gameContainer, Vec2.zero, "ARROW", defs.Get<SpriteDef>("BB:ProjArrow"), Color.white, RenderLayer.Highlight);
        }

        private Transform CreateContainer(string name)
        {
            var container = new GameObject(name).transform;
            container.SetParent(gameContainer, false);
            return container;
        }

        public void AddEffect(Effect effect)
            => effects.Add(effect);

        public void RemoveEffect(Effect effect)
            => effects.Remove(effect);

        // TODO: use agent bounds instead
        const float minionSelectThreshold = .4f;
        public IEnumerable<Minion> GUISelectMinions(Vec2 pos)
        {
            float threshSq = minionSelectThreshold * minionSelectThreshold;
            pos += new Vec2(-.5f, -.5f);
            foreach (var minion in minions)
                if (pos.DistanceSq(minion.realPos) <= threshSq)
                    yield return minion;
        }

        // TODO: use agent bounds instead
        public IEnumerable<Minion> GUISelectMinions(Rect area)
        {
            // TODO: make distance to rect method, can be used for pathfinding too
            Rect rectThresh =
                area.Expand(minionSelectThreshold)
                    .Shift(new Vec2(-.5f, -.5f));
            foreach (var minion in minions)
                if (rectThresh.Contains(minion.realPos))
                    yield return minion;
        }

        public bool HasLineOfSight(Vec2 pos, Vec2 target)
            => GetFirstRaycastTarget(pos, target) == null;

        public RaycastTarget GetFirstRaycastTarget(Vec2 pos, Vec2 ray)
            => Raycast(pos, ray).FirstOrDefault();

        private IEnumerable<RaycastTarget> Raycast(Vec2 pos, Vec2 ray)
        {
            // TODO:
            yield break;
        }

        public void GoTo(Minion minion, Vec2I pos)
            => minion.AssignWork(JobWalk.Create(
                new TaskGoTo(this, "Walking.", PathCfg.Point(pos))));

        public void Update(float dt)
        {
            D_DebugUpdate();

            effects.ForEach(e => e.Update(dt));

            foreach (Minion minion in minions)
            {
                if (!minion.hasWork && !minion.isDrafted)
                {
                    foreach (var system in registry.systems)
                    {
                        foreach (var work in system.QueryWork())
                        {
                            if (minion.AssignWork(work))
                                break;
                        }

                        if (minion.hasWork)
                            break;
                    }
                }
            }

            foreach (var minion in minions)
                minion.Update(dt);
        }
    }
}