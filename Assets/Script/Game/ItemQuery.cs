using System;
using System.Collections.Generic;
using Priority_Queue;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    // TODO: someday this might be more interesting
    public struct ItemFilter
    {
        private readonly ItemDef def;

        public ItemFilter(ItemDef def)
        {
            this.def = def;
        }

        public bool Applies(ItemDef def)
        {
            return this.def == def;
        }
    }


    public struct ItemQueryCfg
    {
        public readonly ItemFilter filter;
        public readonly PathCfg dst;
        public readonly int amt;

        public ItemQueryCfg(ItemFilter filter, int amt, PathCfg dst)
        {
            this.filter = filter;
            this.amt = amt;
            this.dst = dst;
        }

        public ItemQueryCfg(ItemDef def, int amt, PathCfg dst)
            : this(new ItemFilter(def), amt, dst) { }
    }

    public class ItemQuery : IItemListener
    {
        public readonly Game game;
        private readonly ItemQueryCfg cfg;
        private readonly HashSet<TileItem> unavailable;
        private readonly SimplePriorityQueue<TileItem> queue;

        public ItemQuery(Game game, ItemQueryCfg cfg, IEnumerable<TileItem> items)
        {
            this.game = game;
            this.cfg = cfg;
            this.unavailable = new HashSet<TileItem>();
            this.queue = new SimplePriorityQueue<TileItem>();
            foreach (var item in items)
                ItemAdded(item);
        }

        // TODO: deal with items having avail == 0

        private int HaulAmt(TileItem item, int amt)
            => Math.Min(amt, item.amtAvailable);

        private float Priority(TileItem item)
            => cfg.dst.hueristicFn(item.tile.pos) / HaulAmt(item, cfg.amt);

        public void ItemAdded(TileItem item)
        {
            if (cfg.filter.Applies(item.def))
            {
                if (item.amtAvailable > 0)
                    queue.Enqueue(item, Priority(item));
                else
                    unavailable.Add(item);
            }
        }

        public void ItemRemoved(TileItem item)
        {
            if (item.amtAvailable > 0)
                queue.Remove(item);
            else
                unavailable.Remove(item);
        }

        public void ItemChanged(TileItem item)
        {
            if (unavailable.Remove(item))
            {
                if (item.amtAvailable > 0)
                    queue.Enqueue(item, Priority(item));
                else
                    unavailable.Add(item);
            }
            else
            {
                if (item.amtAvailable > 0)
                    queue.UpdatePriority(item, Priority(item));
                else
                {
                    queue.Remove(item);
                    unavailable.Add(item);
                }
            }
        }

        public bool HasAvailable() => queue.Count > 0;

        // TODO: this is a bit awkward since it might prioritize bigger
        // than necessary stacks farther away when amt < cfg.amt
        public TaskClaimItem TaskClaim(int amt)
            => new TaskClaimItem(game,
                (work) =>
                {
                    if (queue.Count == 0)
                        return null;

                    TileItem item = queue.First;
                    if (item.amtAvailable > 0)
                        return ItemClaim.MakeClaim(game, item, HaulAmt(item, amt));

                    return null;
                });

        public void Close() => game.UnregisterItemListener(this);
    }

    public class ItemClaim : IClaim
    {
        public readonly Game game;
        private readonly TileItem item;
        public readonly int amt;
        public Vec2I pos => item.tile.pos;

        private ItemClaim(Game game, TileItem item, int amt)
        {
            this.game = game;
            this.item = item;
            this.amt = amt;
        }

        public static ItemClaim MakeClaim(Game game, TileItem item, int amt)
        {
            BB.AssertNotNull(item);
            if (!game.TryClaim(item, amt))
                return null;

            return new ItemClaim(game, item, amt);
        }

        public Item ResolveClaim(Work work)
        {
            work.Unclaim(this);
            return game.ResolveClaim(item, amt);
        }

        public void Unclaim()
            => game.Unclaim(item, amt);
    }

}
