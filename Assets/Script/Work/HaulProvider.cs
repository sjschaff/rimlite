﻿using System.Collections.Generic;

namespace BB
{
    class HaulProvider
    {
        private readonly Game game;
        private readonly PathCfg dst;
        private readonly ItemInfo info;
        private readonly ItemQuery query;
        private readonly string taskDesc;

        private int amtStored;
        private int amtClaimed;
        private int amtRemaining => info.amt - amtStored;
        private int haulRemaining => amtRemaining - amtClaimed;

        public HaulProvider(Game game, PathCfg dst, ItemInfo info, string dstDesc)
        {
            this.game = game;
            this.dst = dst;
            this.info = info;
            this.taskDesc = $"Hauling {info.def.name} to {dstDesc}.";
            query = game.QueryItems(new ItemQueryCfg(info.def, info.amt, dst));
            amtStored = amtClaimed = 0;
        }

        public bool HasSomeMaterials() => amtStored > 0;
        public bool HasAllMaterials() => amtStored == info.amt;
        public bool HasAvailableHauls()
            => haulRemaining > 0 && query.HasAvailable();

        public ItemInfo RemoveStored()
        {
            ItemInfo ret = info.WithAmount(amtStored);
            amtStored = 0;
            return ret;
        }

        public void Destroy()
        {
            BB.Assert(amtStored == 0, "HaulProvider destroyed without removing items");
            query.Close();
        }

        public IEnumerable<Task> GetHaulTasks()
        {
            var itemClaim = query.TaskClaim(haulRemaining);
            yield return itemClaim;
            var haulClaim = new TaskClaim(game,
                (work) => {
                    BB.AssertNotNull(itemClaim.claim);
                    ItemClaim item = itemClaim.claim;
                    if (item.amt > haulRemaining)
                        return null;

                    amtClaimed += item.amt;
                    return new ClaimLambda(() => amtClaimed -= item.amt);
                });
            yield return haulClaim;
            yield return new TaskGoTo(game, taskDesc, PathCfg.Point(itemClaim.claim.pos));
            yield return new TaskPickupItem(itemClaim);
            yield return new TaskGoTo(game, taskDesc, dst);
            yield return new TaskLambda(game, "dropoff item",
                (work) =>
                {
                    if (!work.agent.carryingItem)
                        return false;

                    work.Unclaim(haulClaim);

                    Item item = work.agent.RemoveItem();

                    // Should never happen
                    int haulAmt = itemClaim.claim.amt;
                    if (haulAmt > haulRemaining)
                        haulAmt = haulRemaining;

                    if (item.info.amt > haulAmt)
                    {
                        game.DropItems(
                            game.Tile(work.agent.pos),
                            item.info.WithAmount(item.info.amt - haulAmt).Enumerate());
                    }
                    // Also should never happen
                    else if (item.info.amt < haulAmt)
                    {
                        haulAmt = item.info.amt;
                    }

                    item.Destroy();

                    amtStored += haulAmt;
                    return true;
                });
        }
    }
}
