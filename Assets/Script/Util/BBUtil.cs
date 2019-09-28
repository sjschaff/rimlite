using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using System;

public struct RenderLayer
{
    public readonly string layer;
    public readonly int order;

    public RenderLayer(string layer, int order)
    {
        this.layer = layer;
        this.order = order;
    }
}

public static class BB
{
    public static void Assert(bool a, string msg = null)
    {
        // TODO: make debug only
        if (!a)
        {
            throw new Exception("Assert failed - " + msg);
        }
    }

    public static void AssertNotNull<T>(T t, string msg = null) where T : class
        => Assert(t != null, msg);

    public static T Next<T>(this T src) where T : struct
    {
        if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

        T[] Arr = (T[])Enum.GetValues(src.GetType());
        int j = Array.IndexOf<T>(Arr, src) + 1;
        return (Arr.Length == j) ? Arr[0] : Arr[j];
    }

    public static void Destroy<T>(this T o) where T : Component => UnityEngine.Object.Destroy(o.gameObject);
    public static Transform Instantiate(this Transform prefab, Vec2 pos, Transform parent) =>
        UnityEngine.Object.Instantiate(prefab, pos.Vec3(), Quaternion.identity, parent);

    public static void SetLayer(this Renderer renderer, RenderLayer layer)
    {
        renderer.sortingLayerName = layer.layer;
        renderer.sortingLayerID = SortingLayer.NameToID(layer.layer);
        renderer.sortingOrder = layer.order;
    }

    public static void SetLayer(this Renderer renderer, string name, int layer = 0)
        => renderer.SetLayer(new RenderLayer(name, layer));

    public static bool InGrid(Vec2I gridSize, Vec2I pt) =>
        pt.x >= 0 && pt.y >= 0 && pt.x < gridSize.x && pt.y < gridSize.y;

}

public static class MathExt {

#pragma warning disable IDE1006 // Naming Styles
    public static Vec2 xy(this Vec3 v) => new Vec2(v.x, v.y);
    public static Vec2I xy(this Vec3I v) => new Vec2I(v.x, v.y);
#pragma warning restore IDE1006 // Naming Styles
    public static Vec3I Vec3(this Vec2I v) => new Vec3I(v.x, v.y, 0);
    public static Vec3 Vec3(this Vec2 v) => new Vec3(v.x, v.y, 0);

    public static Vec2I Floor(this Vec2 v) => new Vec2I(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
    public static Vec2I Ceil(this Vec2 v) => new Vec2I(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y));
    public static bool Adjacent(this Vec2I vA, Vec2I vB)
    {
        return (Math.Abs(vB.y - vA.y) + Math.Abs(vB.x - vA.x)) == 1;
    }

    public static IEnumerable<Vec2I> AllTiles(this RectInt rect)
    {
        for (int x = 0; x < rect.width; ++x)
            for (int y = 0; y < rect.height; ++y)
                yield return new Vec2I(rect.x + x, rect.y + y);
    }
}
