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
        BB.Assert(amt > 0);
        this.type = type;
        this.amt = amt;
    }

    public ItemInfo WithNewAmount(int amt) => new ItemInfo(type, amt);
}

public class Item : MonoBehaviour
{
    private GameController game;
    public SpriteRenderer spriteRenderer;
    public Text text;

    public Vec2I pos { get; private set; }
    private ItemInfo info { get;  set; } // TODO: make this normal? figure out publicicty
    private int amtClaimed;
    public int amtAvailable => info.amt - amtClaimed;
    public int amt => info.amt;
    public ItemType type => info.type;

    private Sprite GetSprite()
    {
        switch (info.type)
        {
            case ItemType.Stone:
                return game.map.tiler.sprites32.GetSprite(Vec2I.zero, new Vec2I(2, 2), Vec2I.one);
            case ItemType.Wood:
                return game.map.tiler.sprites32.GetSprite(new Vec2I(2, 0), new Vec2I(2, 2), Vec2I.one);
            default:
                throw new NotImplementedException("Unkown Item Type: " + info.type);
        }
    }

    public void Init(GameController game, Vec2I pos, ItemInfo info)
    {
        BB.Assert(info.amt > 0);
        this.game = game;
        this.pos = pos;
        this.info = info;
        this.amtClaimed = 0;

        spriteRenderer.sprite = GetSprite();
        UpdateText();
    }

    public void Destroy() => Destroy(gameObject);

    public void Claim(int amt)
    {
        BB.Assert(amtClaimed + amt <= info.amt);
        amtClaimed += amt;
    }

    public void Unclaim(int amt)
    {
        BB.Assert(amtClaimed >= amt);
        amtClaimed -= amt;
    }

    // TODO: Add
    public void Remove(int amt)
    {
        BB.Assert(amt < info.amt);
        BB.Assert(amt <= amtAvailable);
        info = info.WithNewAmount(info.amt - amt);
        UpdateText();
    }

    public enum Config { Ground, PlayerAbove, PlayerBelow }

    public void Configure(Config config)
    {
        if (config == Config.Ground)
        {
            text.enabled = true;
            spriteRenderer.sortingLayerName = "Default";
        }
        else
        {
            text.enabled = false;
            if (config == Config.PlayerAbove)
                spriteRenderer.sortingLayerName = "Over Player";
            else if (config == Config.PlayerBelow)
                spriteRenderer.sortingLayerName = "Under Player";
            else
                throw new NotSupportedException("Unknown Item Config: " + config);
        }
    }

    private void UpdateText() => text.text = info.amt.ToString();

    public void Place(Vec2I pos) => this.pos = pos;
}
