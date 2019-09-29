using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;

namespace BB
{

    public static class LineExt
    {
        private static readonly Cache<Color, Material> materials
            = new Cache<Color, Material>(color => CreateMaterial(color));

        private static Material CreateMaterial(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();

            var material = new Material(Shader.Find("Unlit/Transparent"))
            {
                renderQueue = 3000,
                mainTexture = texture
            };

            return material;
        }

        public static LineRenderer AddLineRenderer(this GameObject o,
            string sortingLayer, int sortingOrder, Color color,
            float width, bool loop, bool useWorldspace, Vec2[] pts)
        {
            var line = o.AddComponent<LineRenderer>();
            line.loop = loop;
            line.material = materials.Get(color);
            line.SetLayer(sortingLayer, sortingOrder);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = useWorldspace;
            line.widthMultiplier = width;
            if (pts != null)
                line.SetPts(pts);

            return line;
        }

        public static void SetPts(this LineRenderer line, Vec2[] pts)
        {
            line.positionCount = pts.Length;
            line.SetPositions(pts.Select(v => v.Vec3()).ToArray());
        }
    }

}