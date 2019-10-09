using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public partial class Game
    {
        private readonly Map map;

        public Vec2I size => map.size;
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

        public void RemoveBuilding(IBuilding building)
        {
            var tile = building.tile;
            BB.Assert(tile.hasBuilding);
            BB.Assert(building.tile.building == building);

            bool passable = tile.passable;
            var bounds = building.bounds;
            building.CancelAllJobs();
            map.RemoveBuilding(tile);
            NotifyBuildingRemoved(building);
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
            NotifyBuildingAdded(building);
            RerouteMinions(building.bounds, passable, tile.passable);
        }

        public void ReplaceBuilding(IBuilding building)
        {
            var tile = building.tile;
            BB.Assert(tile.hasBuilding);
            var buildingPrev = tile.building;

            bool passable = tile.passable;
            tile.building.CancelAllJobs();
            map.ReplaceBuilding(building);
            NotifyBuildingRemoved(buildingPrev);
            NotifyBuildingAdded(building);
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
                        new TaskGoTo(this, "Vacating the area.", PathCfg.Vacate(area))));
            }
        }
    }
}