﻿using System;
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
        public static Vec3 Scaled(this Vec3 v, Vec3 mult)
        {
            Vec3 ret = v;
            ret.Scale(mult);
            return ret;
        }
        public static float DistanceSq(this Vec2 v, Vec2 o) => (v - o).sqrMagnitude;
        public static Vec2 Abs(this Vec2 v) => new Vec2(Mathf.Abs(v.x), Mathf.Abs(v.y));
        public static Vec2 Clamp(this Vec2 v, float min, float max) => new Vec2(Mathf.Clamp(v.x, min, max), Mathf.Clamp(v.y, min, max));
        public static Vec2I Floor(this Vec2 v) => new Vec2I(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
        public static Vec2I Ceil(this Vec2 v) => new Vec2I(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y));
        public static bool Adjacent(this Vec2I vA, Vec2I vB)
        {
            return (Math.Abs(vB.y - vA.y) + Math.Abs(vB.x - vA.x)) == 1;
        }

        public static bool IsAdjacent(this RectInt rect, Vec2I pos)
        {
            bool inX = pos.x >= rect.xMin && pos.x < rect.xMax;
            bool inY = pos.y >= rect.yMin && pos.y < rect.yMax;

            if (inX)
                return (pos.y == rect.yMin - 1) || (pos.y == rect.yMax);
            else if (inY)
                return (pos.x == rect.xMin - 1) || (pos.x == rect.xMax);
            else
                return false;
        }

        public static Vec2I ClosestPt(this RectInt rect, Vec2I pt)
        {
            Vec2I minPt = pt;
            float minDist = float.MaxValue;
            foreach (var tile in rect.allPositionsWithin)
            {
                float dist = Vec2I.Distance(pt, tile);
                if (dist < minDist)
                {
                    minDist = dist;
                    minPt = tile;
                }
            }

            return minPt;
        }

        public static bool InGrid(Vec2I gridSize, Vec2I pt) =>
            pt.x >= 0 && pt.y >= 0 && pt.x < gridSize.x && pt.y < gridSize.y;

        public static Rect RectForPts(Vec2 a, Vec2 b)
        {
            Vec2 lower = Vec2.Min(a, b);
            Vec2 upper = Vec2.Max(a, b);
            return new Rect(lower, upper - lower);
        }

        public static RectInt RectInclusive(this Rect rect)
        {
            Vec2I lower = rect.min.Floor();
            Vec2I upper = rect.max.Ceil();

            return new RectInt(lower, upper - lower);
        }

        public static Rect Clamp(this Rect rect, Rect clamp)
        {
            Rect rectClamped = rect;
            if (rect.xMin < clamp.xMin)
                rectClamped.xMin = clamp.xMin;
            if (rect.xMax > clamp.xMax)
                rectClamped.xMax = clamp.xMax;
            if (rect.yMin < clamp.yMin)
                rectClamped.yMin = clamp.yMin;
            if (rect.yMax > clamp.yMax)
                rectClamped.yMax = clamp.yMax;

            return rectClamped;
        }

        public static Rect Expand(this Rect rect, float amt)
        {
            Rect ret = rect;
            ret.xMin -= amt;
            ret.yMin -= amt;
            ret.xMax += amt;
            ret.yMax += amt;
            return ret;
        }

        public static Rect Shift(this Rect rect, Vec2 shift)
        {
            Rect ret = rect;
            ret.min += shift;
            ret.max += shift;
            return ret;
        }

        public static Rect AsRect(this RectInt rect)
            => new Rect(rect.position, rect.size);

        public static float NextBiggest(this float f)
        {
            var bytes = BitConverter.GetBytes(f);
            int bits = BitConverter.ToInt32(bytes, 0);
            if (bits > 0)
                bits += 1;
            else
                bits -= 1;
            bytes = BitConverter.GetBytes(bits);
            return BitConverter.ToSingle(bytes, 0);
        }

        public static Color Scale(this Color c, float f)
            => new Color(c.r * f, c.g * f, c.b * f, c.a);

        public static Color Alpha(this Color c, float a)
            => new Color(c.r, c.g, c.b, a);
    }
}