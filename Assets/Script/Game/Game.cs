using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering.Universal;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

/* Ideas/concepts *
    [2019 (lol)]
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
    golem workshop
        parts made separately then combined then enchanted

    [2023 edition]
    robust magic system
        key mechanic: stability (eg thaumcraft)
        "aether" magic simulation similar to a gas sim
            different metrics
                temp
                pressure
                chaos
                ??
            different elements interact differently
                create instability or stability
                physically repel/attract
                react to create/destroy
            objects in world interact in unique ways
            highly quantized for nicer ability to control
        pipe/pipelike object



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
        public readonly Transform aetherContainer;

        private readonly Light2D lightGlobal;

        private readonly LinkedList<Minion> minions = new LinkedList<Minion>();
        private readonly List<Agent> agents = new List<Agent>();
        private readonly DeferredSet<Effect> effects =
            new DeferredSet<Effect>(e => e.Destroy());

        private readonly MinionIdle idleJobs;
        private readonly AetherSim aether;

        public Game(Registry registry, AssetSrc assets)
        {
            this.registry = registry;
            this.assets = assets;
            // TODO: initialization order is getting wonky
            registry.LoadTypes(this);

            idleJobs = new MinionIdle(this);

            gameContainer = new GameObject("Game").transform;
            itemContainer = CreateContainer("Items");
            agentContainer = CreateContainer("Agents");
            workOverlays = CreateContainer("Work Overlays");
            aetherContainer = CreateContainer("Aether Container");

            aether = new AetherSim(this, 32);
            map = new Map(this);
            map.InitDebug(new Vec2I(32, 32));

            for (int i = 0; i < 10; ++i)
                minions.AddLast(new Minion(this, new Vec2I(1 + i, 1)));
            agents.AddRange(minions);

            lightGlobal = Camera.main.gameObject.GetComponent<Light2D>();

            var benchProto = (IBuildable)registry.D_GetProto<BldgWorkbenchDef>("BB:Woodcutter");
            AddBuilding(benchProto.CreateBuilding(Tile(new Vec2I(9, 3)), Dir.Down));
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

        const float minionSelectThreshold = .4f;
        public IEnumerable<Minion> GUISelectMinions(Vec2 pos)
        {
            foreach (var minion in minions)
                if (minion.bounds.Contains(pos))
                    yield return minion;
        }

        public IEnumerable<Minion> GUISelectMinions(Rect area)
        {
            foreach (var minion in minions)
                if (minion.bounds.Intersects(area))
                    yield return minion;
        }

        public bool HasLineOfSight(Vec2 pos, Vec2 target)
            => GetFirstRaycastTarget(Ray.FromPts(pos, target), false) == null;

        public RaycastTarget GetFirstRaycastTarget(Ray ray, bool allowInternal)
            => Raycast(ray, allowInternal).FirstOrDefault();

        private IEnumerable<RaycastTarget> Raycast(Ray ray, bool allowInternal)
        {
            SortedList<float, RaycastTarget> hits = new SortedList<float, RaycastTarget>();

            foreach (var agent in agents)
                if (ray.IntersectsCircle(agent.bounds, allowInternal, out float fr))
                    hits.Add(fr, new RaycastTarget(fr, agent));

            Vec2I start = ray.start.Floor();
            BB.Assert(ValidTile(start));

            float t = 0;
            int stepX = ray.dir.x < 0 ? -1 : 1; // 0 shouldnt matter
            int stepY = ray.dir.y < 0 ? -1 : 1; // 0 shouldnt matter

            float dt_dx = stepX * 1 / (ray.dir.x * ray.mag); // may be inf.
            float dt_dy = stepY * 1 / (ray.dir.y * ray.mag); // may be inf.

            float dxFirst = ray.start.x - start.x;
            float dyFirst = ray.start.y - start.y;
            if (stepX > 0) dxFirst = 1 - dxFirst;
            if (stepY > 0) dyFirst = 1 - dyFirst;

            float txNext = (ray.dir.x == 0) ? float.PositiveInfinity : dxFirst * dt_dx;
            float tyNext = (ray.dir.y == 0) ? float.PositiveInfinity : dyFirst * dt_dy;

            Vec2I pos = start;
            while (t < 1 && ValidTile(pos))
            {
                if (allowInternal || pos != start)
                {
                    var tile = Tile(pos);
                    if (tile.hasBuilding && !tile.building.passable)
                    {
                        if (hits.ContainsKey(t)) // Fucking hell
                            t = t.NextBiggest();
                        hits.Add(t, new RaycastTarget(t, tile.building));
                    }
                }

                // Note: this will bias vertically when going exactly through corners
                if (txNext < tyNext)
                {
                    pos.x += stepX;
                    t = txNext;
                    txNext += dt_dx;
                }
                else
                {
                    pos.y += stepY;
                    t = tyNext;
                    tyNext += dt_dy;
                }
            }

            foreach (var hit in hits)
                yield return hit.Value;
        }

        private static Color ColorInt(int r, int g, int b)
            => new Color(r / 255f, g / 255f, b / 255f);

        private static readonly Color[] skyColors = new Color[]
        {
            ColorInt(31, 28, 55),
            ColorInt(43, 40, 67),
            ColorInt(57, 38, 83),
            ColorInt(78, 37, 107),
            ColorInt(99, 37, 127),
            ColorInt(121, 41, 133),
            ColorInt(147, 50, 123),
            ColorInt(172, 61, 103),
            ColorInt(183, 78, 85),
            ColorInt(190, 99, 94),
            ColorInt(198, 116, 93),
            ColorInt(202, 138, 92),
            ColorInt(201, 158, 104),
            ColorInt(219, 189, 134),
            ColorInt(240, 224, 187),
            ColorInt(255, 255, 255),
        };

        float tElapsed = 3;
        const float tLoop = 8;

        public void Update(float dt)
        {
            aether.Update(dt);
            effects.ForEach(e => e.Update(dt));

            foreach (Minion minion in minions)
            {
                if (!minion.isDrafted)
                {
                    if (!minion.hasWork || minion.isIdle)
                    {
                        foreach (var system in registry.systems)
                        {
                            foreach (var work in system.QueryWork())
                            {
                                if (minion.AssignWork(work))
                                    break;
                            }

                            if (minion.hasWork && !minion.isIdle)
                                break;
                        }
                    }

                    if (!minion.hasWork)
                        idleJobs.AssignIdleTask(minion);
                }
            }

            foreach (var agent in agents)
                agent.Update(dt);
        }
    }
}
