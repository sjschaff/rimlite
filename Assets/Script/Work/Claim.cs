using System;

namespace BB
{
    public interface IClaim
    {
        void Unclaim();
    }

    public class ClaimLambda : IClaim
    {
        private readonly Action unclaimFn;
        public ClaimLambda(Action unclaimFn) => this.unclaimFn = unclaimFn;
        public void Unclaim() => unclaimFn();
    }
}
