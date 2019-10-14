using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BB
{
    struct Color24 : IEquatable<Color24>
    {
        public readonly byte r;
        public readonly byte g;
        public readonly byte b;

        public Color24(byte r, byte g, byte b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        private int Delta(int a, int b)
            => Math.Abs(a - b);

        public int Delta(Color24 o)
        {
            return Delta(r, o.r) + Delta(g, o.g) + Delta(b, o.b);
        }

        public override string ToString()
        {
            return $"({r}, {g}, {b})";
        }

        public override bool Equals(object obj)
        {
            return obj is Color24 color && Equals(color);
        }

        public bool Equals(Color24 other)
        {
            return r == other.r &&
                   g == other.g &&
                   b == other.b;
        }

        public override int GetHashCode()
        {
            var hashCode = -839137856;
            hashCode = hashCode * -1521134295 + r.GetHashCode();
            hashCode = hashCode * -1521134295 + g.GetHashCode();
            hashCode = hashCode * -1521134295 + b.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Color24 left, Color24 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Color24 left, Color24 right)
        {
            return !(left == right);
        }
    }

    static class ColExt
    {
        public static Color24 Col24(this Color32 colr)
            => new Color24(colr.r, colr.g, colr.b);
    }


    class Paletteizer : EditorWindow
    {
        const string path = "Assets/Resources/character/hair";
        const string gender = "male";
        const string _name = "princess";
        static readonly string fileIn = $"{path}/{gender}/purple/{_name}.png";
        static readonly string fileOut = $"{path}/{gender}/{_name}.png";
        static readonly Color24[] skinPalette = new Color24[]
        {
            new Color24(251, 236, 230),
            new Color24(253, 213, 183),
            new Color24(234, 163, 119),
            new Color24(210, 133, 96),
            new Color24(158, 62, 55),
            new Color24(40, 24, 32),
        };

        static readonly Color24[] hairPalette = new Color24[]
        {
            new Color24(255, 255, 255),
            new Color24(250, 248, 249),
            new Color24(204, 160, 197),
            new Color24(156, 89, 145),
            new Color24(106, 40, 94),
            new Color24(48, 7, 39),
        };

        static readonly Color24[] palette = hairPalette;

        [MenuItem("BB/VerifyPaletteM")]
        static void VerifyMany()
        {
            string dir = "Assets/Resources/character/hair/male/purple/";
            string[] files = Directory.GetFiles(dir, "*.png");
            foreach (string f in files)
            {
                Debug.Log($"file: {f}");
                VerifyPalette(f);
            }
        }

        [MenuItem("BB/PaletteizeM")]
        static void PaletteizeMany()
        {
            string dirIn = $"{path}/{gender}/purple/";
            string dirOut = $"{path}/{gender}/";
            string[] files = Directory.GetFiles(dirIn, "*.png");
            foreach (string f in files)
            {
                string file = Path.GetFileName(f);
                string fileIn = dirIn + file;
                string fileOut = dirOut + file;
                Paletteize(fileIn, fileOut, false);
            }
        }

        [MenuItem("BB/VerifyPalette")]
        static void VerifyPalette() => VerifyPalette(fileIn);

        static void VerifyPalette(string fileIn)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(fileIn);
            if (tex == null)
            {
                Debug.Log($"bad path: {fileIn}");
                return;
            }

            Color32[] pixels = tex.GetPixels32();

            Dictionary<Color24, int> colors = new Dictionary<Color24, int>();
            SortedSet<int> deltas = new SortedSet<int>();
            int[] dbg = new int[palette.Length + 1];
            foreach (var p in pixels)
            {
                if (p.a == 0)
                    continue;

                var col = p.Col24();

                int i;
                for (i = 0; i < palette.Length; ++i)
                    if (col == palette[i])
                        break;
                dbg[i]++;
                if (i >= palette.Length)
                {
                    if (!colors.TryGetValue(col, out int prv))
                        prv = 0;
                    colors[col] = prv + 1;

                    int delta = int.MaxValue;
                    for (int t = 0; t < palette.Length; ++t)
                    {
                        int d = col.Delta(palette[t]);
                        if (d < delta)
                            delta = d;
                    }

                    //   BB.LogInfo($"minDelta: {delta}, a={p.a}");
                    deltas.Add(delta);
                }
            }

         //   for (int i = 0; i < palette.Length; ++i)
         //       BB.LogInfo($"pixels[{i}]: {dbg[i]}");
            BB.LogInfo($"    pixels unk: {dbg[palette.Length]}");

        //    foreach (int d in deltas)
        //        BB.LogInfo($"delta: {d}");

            foreach (var kvp in colors)
            {
            //    BB.LogInfo($"{kvp.Key}: {kvp.Value}");
            }
        }

        [MenuItem("BB/Paletteize")]
        static void Paletteize()
            => Paletteize(fileIn, fileOut, false);


        [MenuItem("BB/PaletteizeDebug")]
        static void PaletteizeDebug()
            => Paletteize(fileIn, fileOut, true);

        static void Paletteize(string fileIn, string fileOut, bool debug)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(fileIn);
            if (tex == null)
            {
                Debug.Log($"bad path: {fileIn}");
                return;
            }

            Dictionary<Color24, Color32> indices = new Dictionary<Color24, Color32>();
            HashSet<Color24> colors = new HashSet<Color24>();
            for (int i = 0; i < palette.Length; ++i)
            {
                Color24 col = palette[i];
                colors.Add(col);

                float tx = (i + .5f) / 8f;
                byte b = (byte)(tx * 255);
                indices.Add(col, new Color32(b, b, b, 255));
            }

            Color32[] pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; ++i)
            {
                Color32 p = pixels[i];
                Color24 col = p.Col24();
                if (p.a == 0)
                    pixels[i] = new Color32(0, 0, 0, 0);
                else
                {
                    Color32 colOut;
                    if (debug)
                    {
                        if (colors.Contains(col))
                            colOut = new Color32(col.r, col.g, col.b, 255);
                        else
                            colOut = new Color32(255, 0, 255, 255);
                    }
                    else
                    {
                        if (!colors.Contains(col))
                            col = FindClosest(col, colors);
                        colOut = indices[col];
                    }

                    pixels[i] = colOut;
                }
            }

            var texNew = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            texNew.SetPixels32(pixels);
            var bytes = texNew.EncodeToPNG();
            string filename = fileOut;
            if (debug)
                filename += "-debug.png";
            File.WriteAllBytes(filename, bytes);
        }

        public static Color24 FindClosest(Color24 col, HashSet<Color24> colors)
        {
            int minDelta = int.MaxValue;
            Color24 colMin = new Color24();
            foreach (var colPal in colors)
            {
                int delta = col.Delta(colPal);
                if (delta < minDelta)
                {
                    minDelta = delta;
                    colMin = colPal;
                }
            }

            return colMin;
        }
    }
}
