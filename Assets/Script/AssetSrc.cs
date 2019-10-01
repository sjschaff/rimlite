using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class AssetSrc
    {
        public readonly Cache<AtlasDef, Atlas> atlases;
        public readonly Cache<SpriteDef, Sprite> sprites;
        public readonly Cache<string, Font> fonts;

        private readonly Shader spriteShader;
        public readonly Material spriteMaterial;

        private readonly Shader lineShader;
        private readonly Cache<Color, Material> lineMaterials;

        public AssetSrc()
        {
            atlases = new Cache<AtlasDef, Atlas>(
                def => new Atlas(Resources.Load<Texture2D>(def.file), def.pixelsPerTile, def.pixelsPerUnit));

            sprites = new Cache<SpriteDef, Sprite>(
                def => atlases.Get(def.atlas).GetSprite(def.key));

            fonts = new Cache<string, Font>(
                font => Resources.GetBuiltinResource<Font>(font));

            spriteShader = Shader.Find("Sprites/Default");
            spriteMaterial = new Material(spriteShader);

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

        public LineRenderer CreateLine(
            Transform parent, Vec2 pos, string name,
            RenderLayer layer, Color color, float width,
            bool loop, bool useWorldspace, Vec2[] pts)
        {

            var line = CreateObjectWithRenderer<LineRenderer>(parent, pos, name, layer);
            line.material = lineMaterials.Get(color);
            line.loop = loop;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = useWorldspace;
            line.widthMultiplier = width;
            if (pts != null)
                line.SetPts(pts);

            return line;
        }
    }
}