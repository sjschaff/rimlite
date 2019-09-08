﻿using UnityEngine;
using System.Collections.Generic;

using Vec3 = UnityEngine.Vector3;
using Vec2 = UnityEngine.Vector2;
using System.Linq;

public static class LineExt
{
    private static Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    private static Material CreateMaterial(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();

        var material = new Material(Shader.Find("Unlit/Transparent"));
        material.renderQueue = 3000;
        material.mainTexture = texture;

        return material;
    }

    private static Material GetMaterial(Color color)
    {
        if (materials.TryGetValue(color, out var mat))
            return mat;

        mat = CreateMaterial(color);
        materials.Add(color, mat);
        return mat;
    }

    public static LineRenderer AddLineRenderer(this GameObject o,
        string sortingLayer, int sortingOrder, Color color,
        float width, bool loop, bool useWorldspace, Vec2[] pts)
    {
        var line = o.AddComponent<LineRenderer>();
        line.loop = loop;
        line.material = GetMaterial(color);
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
