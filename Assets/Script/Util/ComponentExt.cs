using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;

namespace BB
{
    public struct RenderLayer
    {
        public static readonly RenderLayer Default = new RenderLayer("Default", 0);
        public static readonly RenderLayer Minion = new RenderLayer("Minion", 0);
        public static readonly RenderLayer OverMinion = new RenderLayer("Over Minion", 0);
        public static readonly RenderLayer OverMap = new RenderLayer("Over Map", 0);
        public static readonly RenderLayer Highlight = new RenderLayer("Highlight", 0);

        public readonly string layerName;
        public readonly int order;

        private RenderLayer(string layer, int order)
        {
            this.layerName = layer;
            this.order = order;
        }

        public RenderLayer Layer(int order) => new RenderLayer(layerName, order);
    }

    public static class ComponentExt
    {
        public static void Destroy<T>(this T o) where T : Component => UnityEngine.Object.Destroy(o.gameObject);
        public static Transform Instantiate(this Transform prefab, Vec2 pos, Transform parent) =>
            Object.Instantiate(prefab, pos.Vec3(), Quaternion.identity, parent);

        public static void SetLayer(this Renderer renderer, RenderLayer layer)
        {
            renderer.sortingLayerName = layer.layerName;
            renderer.sortingLayerID = SortingLayer.NameToID(layer.layerName);
            renderer.sortingOrder = layer.order;
        }

        public static void SetPts(this LineRenderer line, Vec2[] pts)
        {
            line.positionCount = pts.Length;
            line.SetPositions(pts.Select(v => v.Vec3()).ToArray());
        }
    }
}
