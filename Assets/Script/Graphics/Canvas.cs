using UnityEngine.UI;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public static class Canvas
    {
        public static Transform CreateCanvas(Transform parent, Vec2I refSize)
        {
            var node = MakeObject(parent, "<canvas>");
            var obj = node.gameObject;

            var canvas = obj.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = refSize;

            return node;
        }

        public static RectTransform MakeObject(string name)
            => (RectTransform)new GameObject(name, typeof(RectTransform)).transform;

        public static RectTransform MakeObject(Transform parent, string name)
        {
            var node = MakeObject(name);
            node.SetParent(parent, false);
            return node;
        }

        public static void SetFill(this RectTransform rect)
            => rect.SetFillWithMargin(0);

        public static void SetFillWithMargin(this RectTransform rect, float margin)
        {
            rect.anchorMin = Vec2.zero;
            rect.anchorMax = new Vec2(1, 1);
            rect.pivot = Vec2.zero;
            rect.offsetMin = new Vec2(margin, margin);
            rect.offsetMax = new Vec2(-margin, -margin);
            rect.ForceUpdateRectTransforms();
        }

        public static void SetSizePivotAnchor(this RectTransform rect, Vec2 size, Vec2 pivot, Vec2 anchor)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
        }
    }
}
