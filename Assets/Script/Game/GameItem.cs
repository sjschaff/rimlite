using System.Collections.Generic;
using System.Linq;

namespace BB
{
    public partial class Game
    {
        private readonly LinkedList<Item> items = new LinkedList<Item>();

        // TODO: get rid of
        public IEnumerable<Item> FindItems(ItemDef def)
        {
            foreach (Item item in items)
                if (item.def == def)
                    yield return item;
        }

        private void DropItem(Tile tile, Item item)
        {
            BB.AssertNotNull(item);
            BB.AssertNull(item.tile);
            BB.Assert(!tile.hasItems);

            item.ReParent(tile);
            item.Configure(Item.Config.Ground);
            items.AddLast(item);
            map.PlaceItem(tile, item);
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
            foreach (ItemInfo info in itemList)
            {
                foreach (var item in itemList)
                {
                    int amt = item.amt;
                    if (itemSet.TryGetValue(info.def, out var prevAmt))
                        amt += prevAmt;

                    itemSet.Add(item.def, amt);
                }
            }

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
                    else if (tile.item.amt < tile.item.def.maxStack &&
                        itemSet.TryGetValue(tile.item.def, out int amtToDrop))
                    {
                        int stackAvailable = tile.item.def.maxStack - tile.item.amt;
                        amtToDrop = DropAmt(tile.item.def, amtToDrop, stackAvailable);
                        tile.item.Add(amtToDrop);
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
            // TODO: interesting things
            return new ItemQuery(new QueryUpdater(this, cfg));
        }

        public void UnregisterItemQuery(QueryUpdater query)
        {
            // TODO: undo interesting things
        }


        // TODO: move item claiming here
        public Work.IClaim ClaimItem(Item item)
        {
            return null;
        }

        // TODO: make this better
        public Item ResolveClaim(Item item, int amt)
        {
            BB.AssertNotNull(item);
            BB.AssertNotNull(item.tile);
            BB.Assert(item.amtAvailable >= amt);
            BB.Assert(items.Contains(item));

            if (amt == item.amt)
            {
                items.Remove(item);
                map.RemoveItem(item);
                return item;
            }
            else
            {
                return item.Split(amt);
            }
        }
    }
}
