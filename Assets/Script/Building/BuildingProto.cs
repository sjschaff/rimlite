﻿using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public interface IBuildingProto
    {
        IBuilding CreateBuilding();

        bool passable { get; }
        BuildingBounds bounds { get; }
        RenderFlags renderFlags { get; }
        TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile);
        TileSprite GetSpriteOver(Map map, Vec2I pos);
    }
}