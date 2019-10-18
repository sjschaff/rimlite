using System.Diagnostics;
using UnityEngine;

namespace BB
{
    public partial class Game
    {
        [Conditional("DEBUG")]
        public void D_DebugUpdate(float dt)
        {
            if (Input.GetKeyDown(KeyCode.X)) D_AbandonAllWork();
            if (Input.GetKeyDown(KeyCode.C)) D_DebugDump();
            if (Input.GetKey(KeyCode.V)) D_ChangeSky(dt);
            if (Input.GetKeyDown(KeyCode.Z)) D_ChangeOutfits();
            if (Input.GetKeyDown(KeyCode.B)) D_ChangeSkin();
            if (Input.GetKeyDown(KeyCode.N)) D_ChangeHair();
            if (Input.GetKeyDown(KeyCode.M)) D_ChangeEyes();
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

        [Conditional("DEBUG")]
        public void D_ChangeOutfits()
        {
            foreach (var minion in minions)
                minion.skin.D_NextOutfit();
        }

        [Conditional("DEBUG")]
        public void D_ChangeSkin()
        {
            foreach (var minion in minions)
                minion.skin.D_NextSkin();
        }

        [Conditional("DEBUG")]
        public void D_ChangeHair()
        {
            foreach (var minion in minions)
                minion.skin.D_NextHair();
        }

        [Conditional("DEBUG")]
        public void D_ChangeEyes()
        {
            foreach (var minion in minions)
                minion.skin.D_NextEyes();
        }

        public void D_ChangeSky(float dt)
        {
            tElapsed += dt;
            tElapsed %= tLoop;

            float tNrm = tElapsed / tLoop;
            Color color;
            if (tNrm < .25f || (tNrm > .5f && tNrm < .75f))
            {
                if (tNrm > .5f)
                    tNrm = .25f - (tNrm - .5f);

                tNrm *= 4;
                float fInd = tNrm * skyColors.Length;
                int ind = Mathf.FloorToInt(fInd);
                var colorA = skyColors[ind];
                int indB = ind + 1;
                if (indB >= skyColors.Length)
                    indB = skyColors.Length - 1;
                var colorB = skyColors[indB];
                float interp = fInd - ind;
                color = Color.Lerp(colorA, colorB, interp);
            }
            else if (tNrm <= .5f)
                color = skyColors[skyColors.Length - 1];
            else
                color = skyColors[0];
            color -= Color.white.Alpha(0).Scale(.03f);

            lightGlobal.color = color;
        }
    }
}
