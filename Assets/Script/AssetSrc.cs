using Unity.Collections;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class AssetSrc
    {
        public readonly Cache<string, Texture2D> textures;
        public readonly Cache<AtlasDef, Atlas> atlases;
        public readonly Cache<SpriteDef, Sprite> sprites;
        public readonly Cache<string, Font> fonts;

        public readonly Shader spriteShader;
        public readonly Material spriteMaterial;

        private readonly Shader lineShader;
        private readonly Cache<Color, Material> lineMaterials;

        public AssetSrc()
        {
            textures = new Cache<string, Texture2D>(
                file => LoadTex(file));

            atlases = new Cache<AtlasDef, Atlas>(
                def => new Atlas(textures.Get(def.file), def.pixelsPerTile, def.pixelsPerUnit));

            sprites = new Cache<SpriteDef, Sprite>(
                def => atlases.Get(def.atlas).GetSprite(def.rect));

            fonts = new Cache<string, Font>(
                font => Resources.GetBuiltinResource<Font>(font));

            spriteShader = Resources.Load<Shader>("BBLitDefault");
            spriteMaterial = new Material(spriteShader);

            Texture2D tex = new Texture2D(128, 128, TextureFormat.R8, false, true)
            {
                filterMode = FilterMode.Point
            };
            NativeArray<byte> data = tex.GetRawTextureData<byte>();

            bool[,] roofs = new bool[128, 128];

            for (int x = 8; x < 12; ++x)
                for (int y = 10; y < 14; ++y)
                    roofs[x, y] = true;

            int index = 0;
            for (int y = 0; y < 128; ++y)
            {
                for (int x = 0; x < 128; ++x)
                {
                    bool roof = roofs[x, y];
                    data[index++] = (byte)(roof ? 64 : 255);
                }
            }
            tex.Apply();
            spriteMaterial.SetTexture("_MaskTex", tex);
            spriteMaterial.SetVector("_MapSize", new Vector4(128, 128));

            Shader.SetGlobalColor("_globalLight", Color.white * .25f);

            lineShader = Shader.Find("Unlit/Transparent");
            lineMaterials = new Cache<Color, Material>(
                color =>
                {
                    var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    texture.SetPixel(0, 0, color);
                    texture.Apply();

                    var material = new Material(lineShader)
                    {
                        renderQueue = 3000,
                        mainTexture = texture
                    };

                    return material;
                });
        }

        private Texture2D LoadTex(string path)
        {
            var tex = Resources.Load<Texture2D>(path);
            tex.filterMode = FilterMode.Point;
            return tex;
        }

        public T CreateObjectWithRenderer<T>(Transform parent, Vec2 pos, string name, RenderLayer layer)
            where T : Renderer
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent, false);
            node.transform.localPosition = pos;

            var renderer = node.AddComponent<T>();
            renderer.SetLayer(layer);
            return renderer;
        }

        public SpriteRenderer CreateSpriteObject(Transform parent, Vec2 pos, string name, SpriteDef sprite, Color color, RenderLayer layer)
        {
            var renderer = CreateObjectWithRenderer<SpriteRenderer>(parent, pos, name, layer);
            renderer.material = spriteMaterial;
            renderer.sprite = sprites.Get(sprite);
            renderer.color = color;
            return renderer;
        }

        // TODO: maybe this goes somewhere else
        public SpriteRenderer CreateJobOverlay(Transform parent, Vec2I pos, SpriteDef sprite)
            => CreateSpriteObject(
                parent,
                pos + new Vec2(.5f, .5f),
                "JobOverlay",
                sprite,
                new Color(.6f, .6f, 1, .5f),
                RenderLayer.Highlight);

        public Line CreateLine(
            Transform parent, string name,
            RenderLayer layer, Color color, float width,
            bool loop, bool useWorldspace)
        {
            var line = CreateObjectWithRenderer<LineRenderer>(parent, Vec2.zero, name, layer);
            line.material = lineMaterials.Get(color);
            line.loop = loop;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = useWorldspace;
            line.widthMultiplier = width;
            var ret = new Line(line);

            return ret;
        }
    }
}