using UnityEngine;
using UnityEngine.UI;
using System;

using Vec2I = UnityEngine.Vector2Int;


// TODO something more extensible than enum
public enum ItemType { Stone, Wood }

public struct ItemInfo
{
    public readonly ItemType type;
    public int amt;

    public ItemInfo(ItemType type, int amt)
    {
        this.type = type;
        this.amt = amt;
    }
}

public class Item : MonoBehaviour
{
    private GameController game;
    public SpriteRenderer spriteRenderer;
    public Text text;

    private Vec2I pos;
    private ItemInfo info;

    private Sprite GetSprite()
    {
        switch (info.type)
        {
            case ItemType.Stone:
                return game.map.itemAtlas.GetSprite(Vec2I.zero, new Vec2I(2, 2), new Vec2I(1, 1), 32);
            case ItemType.Wood:
            default:
                throw new NotImplementedException("Unkown Item Type: " + info.type);
        }
    }

    public void Init(GameController game, Vec2I pos, ItemInfo info)
    {
        this.game = game;
        this.pos = pos;
        this.info = info;

        spriteRenderer.sprite = GetSprite();
        this.text.text = info.amt.ToString();
    }
}
