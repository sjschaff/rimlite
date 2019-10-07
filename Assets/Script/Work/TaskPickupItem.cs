namespace BB
{
    public class TaskPickupItem : TaskTimed
    {
        private readonly TaskClaimItem claim;

        public TaskPickupItem(TaskClaimItem claim)
            : base(claim.game, "Picking up item.", MinionAnim.Magic, Tool.None, .425f, FaceSame())
        {
            BB.AssertNotNull(claim);
            this.claim = claim;
        }

        protected override Status OnBeginTask()
        {
            if (claim.claim == null)
                return Status.Fail;

            return base.OnBeginTask();
        }

        protected override void OnEndTask(bool canceled)
        {
            if (!canceled)
                agent.PickupItem(claim.claim.ResolveClaim(work));

            base.OnEndTask(canceled);
        }

        protected override float WorkSpeed() => 1;
    }
}