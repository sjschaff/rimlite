using System;

namespace BB
{
    public class TaskClaimT<TClaim> : TaskImmediate
        where TClaim : IClaim
    {
        public TClaim claim { get; private set; }
        private readonly Func<Work, TClaim> claimFn;

        public TaskClaimT(Game game, Func<Work, TClaim> claimFn)
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

    public class TaskClaim : TaskClaimT<IClaim>
    {
        public TaskClaim(Game game, Func<Work, IClaim> claimFn)
            : base(game, claimFn) { }
    }

    public class TaskClaimItem : TaskClaimT<ItemClaim>
    {
        public TaskClaimItem(Game game, Func<Work, ItemClaim> claimFn)
            : base(game, claimFn) { }
    }
}