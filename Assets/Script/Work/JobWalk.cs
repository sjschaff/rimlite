namespace BB
{
    public class JobWalk : JobHandle
    {
        private static readonly JobWalk job = new JobWalk();
        public override void AbandonWork(Work work) { }
        public override void CancelJob() { }
        public static Work Create(TaskGoTo task)
            => new Work(job, task.Enumerate(), "WalkDummy");
    }
}