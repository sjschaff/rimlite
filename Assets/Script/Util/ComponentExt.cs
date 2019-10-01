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
        public readonly int layerID;
        public readonly int order;

        private RenderLayer(string layer, int layerID, int order)
        {
            this.layerName = layer;
            this.layerID = layerID;
            this.order = order;
        }

        private RenderLayer(string layer, int order)
            : this(layer, SortingLayer.NameToID(layer), order) { }


        public RenderLayer Layer(int order) => new RenderLayer(layerName, layerID, order);
    }

    public static class ComponentExt
    {
        public static void Destroy(this GameObject o) => Object.Destroy(o);
        public static void Destroy<T>(this T c) where T : Component => c.gameObject.Destroy();
        public static Transform Instantiate(this Transform prefab, Vec2 pos, Transform parent) =>
            Object.Instantiate(prefab, pos, Quaternion.identity, parent);

        public static void SetLayer(this Renderer renderer, RenderLayer layer)
        {
            renderer.sortingLayerName = layer.layerName;
            renderer.sortingLayerID = layer.layerID;
            renderer.sortingOrder = layer.order;
        }

        public static void SetLayer(this Canvas canvas, RenderLayer layer)
        {
            canvas.sortingLayerName = layer.layerName;
            canvas.sortingLayerID = layer.layerID;
            canvas.sortingOrder = layer.order;
        }

        public static void SetPts(this LineRenderer line, Vec2[] pts)
        {
            line.positionCount = pts.Length;
            line.SetPositions(pts.Select(v => v.Vec3()).ToArray());
        }
    }
}