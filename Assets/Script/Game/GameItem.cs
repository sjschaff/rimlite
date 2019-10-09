using System.Collections.Generic;
using System.Linq;

namespace BB
{
    public class TileItem
    {
#if DEBUG
        private static int D_nextID = 0;
        public readonly int D_uniqueID;
#endif

        public readonly Tile tile;
        private Item item;
        private int amtClaimed;

        public ItemInfo info => item.info;
        public ItemDef def => item.def;
        public int amtAvailable => info.amt - amtClaimed;
        public int amt => info.amt;

        public TileItem(Tile tile, Item item)
        {
#if DEBUG
            D_uniqueID = D_nextID;
            ++D_nextID;
#endif
            this.tile = tile;
            this.item = item;
            amtClaimed = 0;

            item.ReParent(tile);
            item.Configure(Item.Config.Ground);
        }

        public bool TryClaim(int amt)
        {
            if (amt > amtAvailable)
                return false;

            amtClaimed += amt;
            return true;
        }

        public void Unclaim(int amt)
        {
            BB.Assert(amtClaimed >= amt);
            amtClaimed -= amt;
        }

        public void Add(int amt)
        {
            BB.Assert(info.amt + amt <= def.maxStack);
            item.ChangeAmt(info.amt + amt);
        }

        public void Remove(int amt)
        {
            BB.Assert(amt < info.amt);
            BB.Assert(amt <= amtAvailable);
            item.ChangeAmt(info.amt - amt);
        }

        public Item RemoveItemAndInvalidate()
        {
            Item ret = item;
            item = null;
            return ret;
        }
    }

    public partial class Game
    {
        private readonly LinkedList<TileItem> items
            = new LinkedList<TileItem>();

        public IEnumerable<TileItem> GUISelectItemsOnTile(Tile tile)
        {
            if (tile.hasItems)
                yield return map.GetItem(tile);
        }

        private void DropItem(Tile tile, Item item)
        {
            BB.AssertNotNull(item);
            BB.Assert(!tile.hasItems);

            var tileItem = new TileItem(tile, item);
            items.AddLast(tileItem);
            map.PlaceItem(tileItem);
            NotifyItemAdded(tileItem);
        }

        // TODO: make Item ItemVis or something, no one should really be
        // holding onto these
        public void K_DropItem(Tile tile, Item item)
        {
            item.Destroy();
            DropItems(tile, item.info.Enumerate());
        }

        public void DropItems(Tile tileDrop, IEnumerable<ItemInfo> itemList)
        {
            Dictionary<ItemDef, int> itemSet = new Dictionary<ItemDef, int>();
            foreach (ItemInfo item in itemList)
            {
                int amt = item.amt;
                if (itemSet.TryGetValue(item.def, out var prevAmt))
                    amt += prevAmt;

                itemSet[item.def] = amt;
            }

            if (itemSet.Count == 0)
                return;

            map.FloodFill(tileDrop, (tile) => tile.passable,
                (tile) =>
                {
                    if (!tile.hasItems)
                    {
                        ItemDef def = itemSet.Keys.First();
                        int amtToDrop = DropAmt(def, itemSet[def], def.maxStack);

                        Item item = new Item(this, new ItemInfo(def, amtToDrop));
                        DropItem(tile, item);
                    }
                    else
                    {
                        TileItem item = map.GetItem(tile);

                        if (item.amt < item.def.maxStack &&
                            itemSet.TryGetValue(item.def, out int amtToDrop))
                        {
                            int stackAvailable = item.def.maxStack - item.amt;
                            amtToDrop = DropAmt(item.def, amtToDrop, stackAvailable);
                            item.Add(amtToDrop);
                            NotifyItemChanged(item);
                        }
                    }

                    return itemSet.Count == 0;
                });

            int DropAmt(ItemDef def, int amtToDrop, int amtAbleToDrop)
            {
                if (amtToDrop <= amtAbleToDrop)
                {
                    itemSet.Remove(def);
                    return amtToDrop;
                }
                else
                {
                    itemSet[def] = amtToDrop - amtAbleToDrop;
                    return amtAbleToDrop;
                }
            }
        }

        public ItemQuery QueryItems(ItemQueryCfg cfg)
        {
            var query = new ItemQuery(this, cfg, items);
            RegisterItemListener(query);
            return query;
        }

        public bool TryClaim(TileItem item, int amt)
        {
            if (item.TryClaim(amt))
            {
                NotifyItemChanged(item);
                return true;
            }

            return false;
        }

        public void Unclaim(TileItem item, int amt)
        {
            item.Unclaim(amt);
            NotifyItemChanged(item);
        }

        public Item ResolveClaim(TileItem item, int amt)
        {
            BB.AssertNotNull(item);
            BB.AssertNotNull(item.tile);
            BB.Assert(item.amtAvailable >= amt);
            BB.Assert(items.Contains(item));

            if (amt == item.amt)
            {
                items.Remove(item);
                map.RemoveItem(item);
                NotifyItemRemoved(item);
                return item.RemoveItemAndInvalidate();
            }
            else
            {
                item.Remove(amt);
                NotifyItemChanged(item);
                return new Item(this, item.info.WithAmount(amt));
            }
        }
    }
}
