using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public interface IBuildingProto
    {
        /*IBuildingProto(Game game, TBldgDef def);*/

        IBuilding CreateBuilding(Tile tile);

        DefNamed buildingDef { get; }
        bool passable { get; }
        BuildingBounds Bounds(Dir dir);
        RenderFlags GetFlags(Dir dir);
        TileSprite GetSprite(Dir dir, Vec2I pos, Vec2I subTile);
        TileSprite GetSpriteOver(Dir dir, Vec2I pos);
    }
}