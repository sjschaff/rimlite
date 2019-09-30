using System.Collections.Generic;
using System;
using UnityEngine;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public static class MathExt
    {

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

        public static bool InGrid(Vec2I gridSize, Vec2I pt) =>
            pt.x >= 0 && pt.y >= 0 && pt.x < gridSize.x && pt.y < gridSize.y;
    }
}