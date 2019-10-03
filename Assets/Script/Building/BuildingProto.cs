using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public interface IBuildingProto
    {
        /*IBuildingProto(GameController game, TBldgDef def);*/

        IBuilding CreateBuilding();

        bool passable { get; }
        BuildingBounds Bounds(Dir dir);
        RenderFlags GetFlags(Dir dir);
        TileSprite GetSprite(Dir dir, Vec2I pos, Vec2I subTile);
        TileSprite GetSpriteOver(Dir dir, Vec2I pos);
    }
}