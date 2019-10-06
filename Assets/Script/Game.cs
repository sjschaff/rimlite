using System.Collections.Generic;
using System.Linq;
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
    public class Game
    {
        public readonly AssetSrc assets;
        public readonly Registry registry;
        private readonly Map map;

        public Defs defs => registry.defs;
        public Vec2I size => map.size;

        // TODO: be more organized about where we 
        // our game objects
        public Transform transform;

        private readonly LinkedList<Minion> minions = new LinkedList<Minion>();
        private readonly Minion D_minionNoTask;
        private readonly LinkedList<Item> items = new LinkedList<Item>();

        public Game(Registry registry, AssetSrc assets)
        {
            this.registry = registry;
            this.assets = assets;
            // TODO: initialization order is getting wonky
            registry.LoadTypes(this);

            transform = new GameObject("Game").transform;

            map = new Map(this);
            map.InitDebug(new Vec2I(128, 128));

            for (int i = 0; i < 10; ++i)
                minions.AddLast(new Minion(this, new Vec2I(1 + i, 1)));
            D_minionNoTask = minions.First.Value;
        }

        public bool ValidTile(Vec2I pos) => map.ValidTile(pos);
        public Tile Tile(Vec2I pos) => map.GetTile(pos);

        public Vec2I[] GetPath(Vec2I start, PathCfg cfg)
            => map.nav.GetPath(start, cfg);
         
        // required:  true if pos is no longer passable, false if pos is now passable
        public void RerouteMinions(RectInt rect, bool required)
        {
            // TODO: check if minions can reroute more efficiently
            if (!required)
                return;

            foreach (var minion in minions)
            {
                if (minion.hasWork && minion.currentWork.activeTask != null)
                    minion.currentWork.activeTask.Reroute(rect);
            }
        }

        private void RerouteMinions(RectInt rect, bool wasPassable, bool nowPassable)
        {
            if (wasPassable != nowPassable)
                RerouteMinions(rect, wasPassable);
        }

        public IEnumerable<Item> FindItems(ItemDef def)
        {
            foreach (Item item in items)
                if (item.def == def)
                    yield return item;
        }

        public void RemoveBuilding(IBuilding building)
        {
            // TODO: cancel outstanding job handles
            var tile = building.tile;
            BB.Assert(tile.hasBuilding);
            BB.Assert(building.tile.building == building);

            bool passable = tile.passable;
            var bounds = building.bounds;
            map.RemoveBuilding(tile);
            RerouteMinions(bounds, passable, tile.passable);
        }

        public bool CanPlaceBuilding(RectInt area)
        {
            foreach (var pos in area.allPositionsWithin)
                if (!ValidTile(pos))
                    return false;

            return !map.HasBuilding(area);
        }

        public void AddBuilding(IBuilding building)
        {
            BB.Assert(CanPlaceBuilding(building.bounds));

            var tile = building.tile;
            bool passable = tile.passable;
            map.AddBuilding(building);
            RerouteMinions(building.bounds, passable, tile.passable);
        }

        public void ReplaceBuilding(IBuilding building)
        {
            // TODO: cancel outstanding job handles
            var tile = building.tile;
            BB.Assert(tile.hasBuilding);

            bool passable = tile.passable;
            map.ReplaceBuilding(building);
            RerouteMinions(building.bounds, passable, tile.passable);
        }

        public void ModifyTerrain(Tile tile, Terrain terrain)
        {
            bool wasPassable = tile.terrain.passable;
            bool nowPassable = terrain.passable;

            map.ModifyTerrain(tile, terrain);

            if (wasPassable != nowPassable)
                RerouteMinions(new RectInt(tile.pos, Vec2I.one), wasPassable);
        }

        // TODO: make this DropItems so we can do fast flood fill in O(n)
        public void DropItem(Vec2I pos, Item item)
        {
            BB.AssertNotNull(item);
            item.ReParent(transform, pos);
            item.Place(pos);
            item.Configure(Item.Config.Ground);
            items.AddLast(item);
        }

        public void DropItem(Vec2I pos, ItemInfo info) => DropItem(pos, new Item(this, pos, info));

        public Item TakeItem(Item item, int amt)
        {
            BB.AssertNotNull(item);
            BB.Assert(item.amtAvailable >= amt);
            BB.Assert(items.Contains(item));

            if (amt == item.amt)
            {
                items.Remove(item);
                return item;
            }
            else
            {
                return item.Split(amt);
            }
        }

        public void K_MoveMinion(Vec2I pos)
            => D_minionNoTask.AssignWork(SystemWalkDummy.Create(
                new TaskGoTo(this, PathCfg.Point(pos))));

        public bool IsAreaOccupied(RectInt area, Agent agentIgnore)
        {
            foreach (Minion minion in minions)
            {
                if (minion != agentIgnore && minion.InArea(area))
                    return true;
            }

            return false;
        }

        public void VacateArea(RectInt area)
        {
            foreach (Minion minion in minions)
            {
                if (!minion.hasWork && area.Contains(minion.pos))
                    minion.AssignWork(SystemWalkDummy.Create(
                        new TaskGoTo(this, PathCfg.Vacate(area))));
            }
        }

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