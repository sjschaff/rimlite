using System;

namespace BB
{
    public class TaskClaim : TaskImmediate
    {
        public Work.IClaim claim { get; private set; }
        private readonly Func<Work, Work.IClaim> claimFn;

        public TaskClaim(GameController game, Func<Work, Work.IClaim> claimFn)
            : base(game) => this.claimFn = claimFn;

        protected override Status OnBeginTask()
        {
            claim = claimFn(work);
            if (claim != null)
            {
                work.MakeClaim(claim);
                return Status.Complete;
            }

            return Status.Fail;
        }
    }

    public class TaskClaimItem : TaskClaim
    {
        public readonly Item item;
        public readonly int amt;

        public TaskClaimItem(GameController game, Item item, int amt)
            : base(game, (work) =>
            {
                if (item.amtAvailable < amt)
                    return null;

                return new Work.ItemClaim(item, amt);
            })
        {
            this.item = item;
            this.amt = amt;
        }
    }
}