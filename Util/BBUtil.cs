using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using System;

public static class BB
{
    public static void Assert(bool a, string msg = null)
    {
        // TODO: make debug only
        if (!a)
        {
            throw new System.Exception("Assert failed - " + msg);
        }
    }

    public static T Next<T>(this T src) where T : struct
    {
        if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

        T[] Arr = (T[])Enum.GetValues(src.GetType());
        int j = Array.IndexOf<T>(Arr, src) + 1;
        return (Arr.Length == j) ? Arr[0] : Arr[j];
    }

    public static Vec2 xy(this Vec3 v) => new Vec2(v.x, v.y);
    public static Vec2I xy(this Vec3I v) => new Vec2I(v.x, v.y);
    public static Vec3I Vec3(this Vec2I v) => new Vec3I(v.x, v.y, 0);
    public static Vec3 Vec3(this Vec2 v) => new Vec3(v.x, v.y, 0);

    public static Vec2I Floor(this Vec2 v) => new Vec2I(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
    public static bool Adjacent(this Vec2I vA, Vec2I vB)
    {
        return (Math.Abs(vB.y - vA.y) + Math.Abs(vB.x - vA.x)) == 1;
    }
}
