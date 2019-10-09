using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec3 = UnityEngine.Vector3;

namespace BB
{
    public class SelectionHighlight
    {
        private static readonly Vec2[] ptsBL = new Vec2[]
        {
            new Vec2(0, .25f),
            new Vec2(0, .1f),
            new Vec2(.1f, 0),
            new Vec2(.25f, 0)
        };

        private static readonly Vec2[] ptsBR =
            ptsBL.Select(pt => new Vec2(-pt.x, pt.y)).ToArray();
        private static readonly Vec2[] ptsTR =
            ptsBL.Select(pt => new Vec2(-pt.x, -pt.y)).ToArray();
        private static readonly Vec2[] ptsTL =
            ptsBL.Select(pt => new Vec2(pt.x, -pt.y)).ToArray();


        private readonly Transform parent;
        private readonly Transform container;
        private readonly Transform scaler;
        private readonly Line bl, br, tl, tr;
        private float animTime;

        private static readonly Vec3 scaleStart = Vec3.one * 1.65f;
        private const float animDur = .225f;

        private static Line CreateLine(AssetSrc assets, Transform parent, Vec2[] pts)
        {
            var line = assets.CreateLine(
                parent, "<corner>",
                RenderLayer.Highlight.Layer(10000),
                Color.white, 1 / 32f, false, false);
            line.SetPts(pts);
            return line;
        }

        public SelectionHighlight(AssetSrc assets, Transform parent)
        {
            this.parent = parent;
            this.container = new GameObject("<highlight>").transform;
            container.SetParent(parent, false);

            this.scaler = new GameObject("<scaler>").transform;
            scaler.SetParent(container, false);

            bl = CreateLine(assets, scaler, ptsBL);
            br = CreateLine(assets, scaler, ptsBR);
            tl = CreateLine(assets, scaler, ptsTL);
            tr = CreateLine(assets, scaler, ptsTR);
        }

        public void Enable(Agent agent)
        {
            container.SetParent(agent.transform, false);
            SetRect(new Rect(Vec2.zero, Vec2.one));
        }

        public void Enable(Rect rect) => SetRect(rect);

        private void SetRect(Rect rect)
        {
            Vec2 center = rect.center;
            scaler.localPosition = center;
            rect = rect.Shift(-center);
            bl.position = rect.min;
            br.position = new Vec2(rect.xMax, rect.yMin);
            tr.position = rect.max;
            tl.position = new Vec2(rect.xMin, rect.yMax);
            bl.enabled = br.enabled = tr.enabled = tl.enabled = true;

            animTime = 0;
            scaler.localScale = scaleStart;
        }

        public void Update(float dt)
        {
            if (animTime >= 1)
                return;

            animTime = Mathf.Min(animTime + (dt/animDur), 1);
            scaler.localScale = Vec3.Lerp(
                scaleStart, Vec3.one, AnimUtil.EaseOut(animTime));
        }

        public void Disable()
        {
            bl.enabled = br.enabled = tr.enabled = tl.enabled = false;
            container.SetParent(parent, false);
        }
    }
}