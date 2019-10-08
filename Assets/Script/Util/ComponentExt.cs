using UnityEngine;

using Vec2 = UnityEngine.Vector2;

namespace BB
{
    public static class ComponentExt
    {
        public static void Destroy(this GameObject o) => Object.Destroy(o);
        public static void Destroy<T>(this T c) where T : Component => c.gameObject.Destroy();
        public static Transform Instantiate(this Transform prefab, Vec2 pos, Transform parent) =>
            Object.Instantiate(prefab, pos, Quaternion.identity, parent);

        public static Vec2 OrthoSize(this Camera c)
        {
            float h = c.orthographicSize * 2;
            float w = h * c.aspect;
            return new Vec2(w, h);
        }

        public static Rect WorldRect(this Camera c)
        {
            Vec2 size = c.OrthoSize();
            Vec2 pos = c.transform.position.xy();
            return new Rect(pos - size * .5f, size);
        }

        public static void SetLayer(this Renderer renderer, RenderLayer layer)
            => layer.Apply(renderer);

        public static void SetLayer(this Canvas canvas, RenderLayer layer)
            => layer.Apply(canvas);
    }
}