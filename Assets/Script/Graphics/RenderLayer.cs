using UnityEngine;

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

        public void Apply(Renderer renderer)
        {
            renderer.sortingLayerName = layerName;
            renderer.sortingLayerID = layerID;
            renderer.sortingOrder = order;
        }

        public void Apply(Canvas canvas)
        {
            canvas.sortingLayerName = layerName;
            canvas.sortingLayerID = layerID;
            canvas.sortingOrder = order;
        }
    }
}
