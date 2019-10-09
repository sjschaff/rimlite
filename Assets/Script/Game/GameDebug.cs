using System.Diagnostics;
using UnityEngine;

namespace BB
{
    public partial class Game
    {
        [Conditional("DEBUG")]
        public void D_DebugUpdate()
        {
            if (Input.GetKey(KeyCode.X)) D_AbandonAllWork();
            if (Input.GetKey(KeyCode.C)) D_DebugDump();
        }

        [Conditional("DEBUG")]
        public void D_AbandonAllWork()
        {
            foreach (var minion in minions)
                if (minion.hasWork)
                    minion.AbandonWork();
        }

        [Conditional("DEBUG")]
        public void D_DebugDump()
        {
            //SystemBuild.K_instance.DebugDump();
        }
    }
}
