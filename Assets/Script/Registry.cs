using UnityEngine;
using System.Collections;

public class Registry
{
    public readonly Defs defs;

    // TODO: something more general/interesting
    // also not cache since, everything should be registered up front
    public readonly Cache<BldgMineableDef, BuildingProtoResource> resources;
    public readonly Cache<BldgFloorDef, BuildingProtoFloor> floors;
    public readonly Cache<BldgWallDef, BuildingProtoWall> walls;

    public Registry()
    {
        defs = new Defs();

        resources = new Cache<BldgMineableDef, BuildingProtoResource>(
            def => new BuildingProtoResource(def));
        floors = new Cache<BldgFloorDef, BuildingProtoFloor>(
            def => new BuildingProtoFloor(def));
        walls = new Cache<BldgWallDef, BuildingProtoWall>(
            def => new BuildingProtoWall(def));
    }
}
