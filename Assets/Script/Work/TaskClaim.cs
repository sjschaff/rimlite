using System;

namespace BB
{
    public class TaskClaim<TClaim> : TaskImmediate
        where TClaim : Work.IClaim
    {
        public TClaim claim { get; private set; }
        private readonly Func<Work, TClaim> claimFn;

        public TaskClaim(Game game, Func<Work, TClaim> claimFn)
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
}