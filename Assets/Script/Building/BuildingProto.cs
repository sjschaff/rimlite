using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public interface IBuildingProto
    {
        /*IBuildingProto(GameController game, TBldgDef def);*/

        IBuilding CreateBuilding();

        bool passable { get; }
        BuildingBounds Bounds(MinionSkin.Dir dir);
        RenderFlags renderFlags { get; }
        TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile);
        TileSprite GetSpriteOver(Map map, Vec2I pos);
    }
}