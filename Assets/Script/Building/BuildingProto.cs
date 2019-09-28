using System.Collections.Generic;

using Vec2I = UnityEngine.Vector2Int;

public interface IBuildingProto
{
    IBuilding CreateBuilding();
    IEnumerable<ItemInfo> GetBuildMaterials();

    bool passable { get; }
    bool K_mineable { get; }
    Tool K_miningTool { get; }
    IEnumerable<ItemInfo> K_GetMinedMaterials();

    BuildingBounds bounds { get; }
    RenderFlags renderFlags { get; }
    TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile);
    TileSprite GetSpriteOver(Map map, Vec2I pos);
}