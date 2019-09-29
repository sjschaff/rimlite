using System;
using UnityEngine;
using UnityEngine.UI;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{

    // TODO: can we merge these? (i.e. does ItemInfo need to be mutable?)
    public struct ItemInfoRO
    {
        public readonly ItemDef def;
        public readonly int amt;

        public ItemInfoRO(ItemDef def, int amt)
        {
            BB.Assert(amt > 0);
            this.def = def;
            this.amt = amt;
        }

        public static implicit operator ItemInfo(ItemInfoRO item)
            => new ItemInfo(item.def, item.amt);
    }

    public struct ItemInfo
    {
        public readonly ItemDef def;
        public int amt;

        public ItemInfo(ItemDef def, int amt)
        {
            BB.Assert(amt > 0);
            this.def = def;
            this.amt = amt;
        }

        public ItemInfo WithNewAmount(int amt) => new ItemInfo(def, amt);
    }

    public class Item : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;
        public Text text;

        public Vec2I pos { get; private set; }
        private ItemInfo info { get; set; } // TODO: make this normal? figure out publicicty
        private int amtClaimed;
        public int amtAvailable => info.amt - amtClaimed;
        public int amt => info.amt;
        public ItemDef def => info.def;

        public void Init(GameController game, Vec2I pos, ItemInfo info)
        {
            BB.Assert(info.amt > 0);
            this.pos = pos;
            this.info = info;
            this.amtClaimed = 0;

            spriteRenderer.sprite = game.assets.sprites.Get(info.def.sprite);
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
                spriteRenderer.SetLayer("Default", 1000);
            }
            else
            {
                text.enabled = false;
                if (config == Config.PlayerAbove)
                    spriteRenderer.SetLayer("Over Player");
                else if (config == Config.PlayerBelow)
                    spriteRenderer.SetLayer("Default", 1000);
                else
                    throw new NotSupportedException("Unknown Item Config: " + config);
            }
        }

        private void UpdateText() => text.text = info.amt.ToString();

        public void Place(Vec2I pos) => this.pos = pos;
    }

}