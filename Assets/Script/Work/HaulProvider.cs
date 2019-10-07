using System.Collections.Generic;

namespace BB
{
    class HaulProvider
    {
        private readonly Game game;
        private readonly PathCfg dst;
        private readonly ItemInfo info;
        private readonly ItemQuery query;

        private int amtStored;
        private int amtClaimed;
        private int amtRemaining => info.amt - amtStored;
        private int haulRemaining => amtRemaining - amtClaimed;

        public HaulProvider(Game game, PathCfg dst, ItemInfo info)
        {
            this.game = game;
            this.dst = dst;
            this.info = info;
            query = game.QueryItems(new ItemQueryCfg(info.def, dst));
            amtStored = amtClaimed = 0;
        }

        public bool HasSomeMaterials() => amtStored > 0;
        public bool HasAllMaterials() => amtStored == info.amt;
        public bool HasAvailableHauls()
            => haulRemaining > 0 && query.HasAvailable(haulRemaining);

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
            var haulClaim = new TaskClaim<Work.IClaim>(game,
                (work) => {
                    BB.AssertNotNull(itemClaim.claim);
                    Work.ItemClaim item = itemClaim.claim;
                    if (item.amt > haulRemaining)
                        return null;

                    amtClaimed += item.amt;
                    return new Work.ClaimLambda(() => amtClaimed -= item.amt);
                });
            yield return haulClaim;
            yield return new TaskGoTo(game, PathCfg.Point(itemClaim.claim.pos));
            yield return new TaskPickupItem(itemClaim);
            yield return new TaskGoTo(game, dst);

            yield return new TaskLambda(game,
                (work) =>
                {
                    if (!work.agent.carryingItem)
                        return false;

                    work.Unclaim(haulClaim);

                    Item item = work.agent.RemoveItem();

                    // Should never happen
                    int amt = itemClaim.claim.amt;
                    if (amt > haulRemaining)
                        amt = haulRemaining;

                    if (item.amt > amt)
                    {
                        item.Remove(amt);
                        game.K_DropItem(game.Tile(work.agent.pos), item);
                    }
                    else
                    {
                        // Also should never happen
                        if (item.amt < amt)
                            amt = item.amt;

                        item.Destroy();
                    }

                    amtStored += amt;
                    return true;
                });
        }
    }
}
