using System;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public interface IClaim
    {
        void Unclaim();
    }

    // TODO: dont store item publicaly, this will serve as ItemHandle
    public class ItemClaim : IClaim
    {
        private readonly Item item; // TODO: meh

        public Vec2I pos => item.tile.pos;
        public readonly int amt;

        public ItemClaim(Item item, int amt)
        {
            BB.AssertNotNull(item);
            BB.Assert(amt <= item.amtAvailable);

            this.item = item;
            this.amt = amt;
            item.Claim(amt);
        }

        public Item ResolveClaim(Game game, Work work)
        {
            work.Unclaim(this);
            return game.ResolveClaim(item, amt);
        }

        public void Unclaim()
        {
            item.Unclaim(amt);
        }
    }

    public class ClaimLambda : IClaim
    {
        private readonly Action unclaimFn;
        public ClaimLambda(Action unclaimFn) => this.unclaimFn = unclaimFn;
        public void Unclaim() => unclaimFn();
    }
}
