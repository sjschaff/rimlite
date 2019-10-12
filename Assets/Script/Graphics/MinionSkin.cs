using System.Collections.Generic;
using System;
using UnityEngine;

using Vec3 = UnityEngine.Vector3;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    #region Atlas
    public class MetaAtlas
    {
        private struct AnimKey
        {
            public readonly MinionAnim anim;
            public readonly Dir dir;
            public readonly int frame;

            public AnimKey(MinionAnim anim, Dir dir, int frame)
            {
                this.anim = anim;
                this.dir = dir;
                this.frame = frame;
            }
        }

        private readonly CacheNonNullable<AnimKey, Atlas.Rect> keys;
        private readonly Cache<string, Atlas> atlases;

        public MetaAtlas()
        {
            keys = new CacheNonNullable<AnimKey, Atlas.Rect>(
                animKey =>
                {
                    var origin = new Vec2I(
                        OffsetFrame(animKey.frame),
                        20 - (OriginAnim(animKey.anim) + OffsetDir(animKey.dir)));

                    return new Atlas.Rect(
                        origin * 2,
                        new Vec2I(2, 2),
                        new Vec2I(1, 0));
                });

            atlases = new Cache<string, Atlas>(
                path =>
                {
                    Texture2D tex = Resources.Load<Texture2D>(path);
                    if (tex == null)
                        BB.LogError("No texture: " + path);

                    tex.filterMode = FilterMode.Point;
                    return new Atlas(tex, 32, 64);
                });
        }

        private static int OriginAnim(MinionAnim anim)
        {
            switch (anim)
            {
                case MinionAnim.Magic: return 0;
                case MinionAnim.Thrust: return 4;
                case MinionAnim.Idle:
                case MinionAnim.Walk: return 8;
                case MinionAnim.Slash: return 12;
                case MinionAnim.Reload:
                case MinionAnim.Shoot: return 16;
                case MinionAnim.Hurt: return 20;
            }

            throw new Exception("unknown anim: " + anim);
        }

        private static int OffsetDir(Dir dir)
        {
            switch (dir)
            {
                case Dir.Up: return 0;
                case Dir.Left: return 1;
                case Dir.Down: return 2;
                case Dir.Right: return 3;
            }

            throw new Exception("unknown dir: " + dir);
        }

        private static int OffsetFrame(int frame) => frame;

        public Sprite GetSprite(string type, bool male, string name, MinionAnim anim, Dir dir, int frame)
        {
            if (type == "tabbard")
                type = "torso";
            string path = "character/" + type + "/";
            if (type != "weapon" && type != "arrow")
                path += (male ? "male" : "female") + "/";
            path += name;

            return atlases.Get(path).GetSprite(keys.Get(new AnimKey(anim, dir, frame)));
        }
    }
    #endregion

    public enum MinionAnim
    {
        Idle, Magic, Thrust, Walk, Slash, Shoot, Hurt, Reload
    }

    public static class MinionAnimExt
    {
        public static int NumFrames(this MinionAnim anim)
        {
            switch (anim)
            {
                case MinionAnim.Idle: return 1;
                case MinionAnim.Magic: return 7;
                case MinionAnim.Thrust: return 8;
                case MinionAnim.Walk: return 9;
                case MinionAnim.Slash: return 6;
                case MinionAnim.Reload: return 1;
                case MinionAnim.Shoot: return 12;//that last frame sucks 13;
                case MinionAnim.Hurt: return 6;
                default:
                    throw new ArgumentException($"Invalid Anim {anim}");
            }
        }

        public static float Duration(this MinionAnim anim)
            => anim.NumFrames() / 12f;
    }

    public class MinionSkin : MonoBehaviour
    {
        private static MetaAtlas atlas;
        private Dictionary<string, SpriteRenderer> spriteLayers;

        private Camera cam;
        private Transform spriteContainer;

        private Dictionary<string, string> equipped;
        public Dir dir { get; private set; } = Dir.Down;

        const float frameTime = 1f / 12f;
        private AnimState state;
        private struct AnimState
        {
            public MinionAnim anim;
            public bool loop;
            public int numFrames;
            public int curFrame;
            public float elapsed;

            public bool dirty;

            public void SetAnim(MinionAnim anim, bool loop)
            {
                this.anim = anim;
                this.loop = loop;
                numFrames = anim.NumFrames();
                curFrame = 0;
                elapsed = 0;
                dirty = true;
            }

            public void Update(float dt)
            {
                elapsed += dt;
                if (elapsed > frameTime)
                {
                    dirty = true;
                    elapsed -= frameTime;
                    curFrame += 1;
                    if (curFrame >= numFrames)
                    {
                        if (loop)
                            curFrame %= numFrames;
                        else
                            SetAnim(MinionAnim.Idle, true);
                    }
                }
            }
        }
        public void Init(AssetSrc assets)
        {
            if (atlas == null)
                atlas = new MetaAtlas();

            cam = Camera.main;
            spriteContainer = new GameObject("Sprite Container").transform;
            spriteContainer.SetParent(transform, false);
            spriteContainer.localPosition = new Vec2(.5f, 0);
            spriteContainer.localScale = Vec3.one * 1.75f;

            spriteLayers = new Dictionary<string, SpriteRenderer>();
            int subLayer = 0;
            foreach (string layer in layers)
            {
                var sprite = assets.CreateSpriteObject(
                    spriteContainer, Vec2.zero,
                    layer, null, Color.white, RenderLayer.Minion);
                //sprite
                sprite.transform.localPosition -= new Vec3(0, 0, .001f * subLayer);
                spriteLayers.Add(layer, sprite);
                ++subLayer;
            }

            K_SetOutfit(0);
            state.SetAnim(MinionAnim.Idle, true);
        }

        private string NameForTool(Tool tool)
        {
            switch (tool)
            {
                case Tool.None: return null;
                case Tool.Hammer: return "warhammer";
                case Tool.Pickaxe: return "pickaxe";
                case Tool.Axe: return "axe";
                case Tool.RecurveBow: return "recurvebow";
            }
            throw new Exception("unknown tool: " + tool);
        }

        public void SetTool(Tool tool)
        {
            equipped["weapon"] = NameForTool(tool);
            if (tool == Tool.RecurveBow)
                equipped["arrow"] = "arrow";
            else
                equipped["arrow"] = null;
            state.dirty = true;
        }

        public void SetAnimLoop(MinionAnim anim)
        {
            // TODO: hackz
            state.SetAnim(anim, anim != MinionAnim.Shoot);
        }

        public void PlayAnimOnce(MinionAnim anim)
        {
            state.SetAnim(anim, false);
        }

        public void SetDir(Dir dir)
        {
            this.dir = dir;
            state.dirty = true;
        }

        static readonly string[] layers =
        {
            // "cape back"
            // "quiver"
            "body/base",
            "body/eyes",
            "body/nose",
            "feet",
            "legs",
            "wrist",
            "hands",
            "torso",
            "tabbard",
            "shoulders",
            "belt",
            //"back",
            //"cape neck",
            "face",
            "hair",
            "head",
            "body/ears",
            "weapon",
            "arrow",
        };

        int k_curOutfit = 0;
        private void K_SetOutfit(int i)
        {
            k_curOutfit = i;
            equipped = new Dictionary<string, string>(
                K_outfits[i]);
            state.dirty = true;
        }

        void LateUpdate()
        {
            // Manipulate z coord. to sort sprites tr -> bl
            Rect bounds = cam.WorldRect().Expand(2f); 
            float maxZ = cam.farClipPlane * .95f;
            float horizontalStride = maxZ / bounds.size.y;

            Vec2 position = spriteContainer.position.xy();
            Vec2 posNormalized = (position - bounds.min) / bounds.size;
            posNormalized = posNormalized.Clamp(0, 1);

            float z = posNormalized.y * maxZ + posNormalized.x * horizontalStride;
            spriteContainer.position = new Vec3(position.x, position.y, z);

        }

        public void UpdateAnim(float dt)
        {
            if (Input.GetKeyDown("z"))
                K_SetOutfit((k_curOutfit + 1) % K_outfits.Count);

            state.Update(dt);
            if (state.dirty)
            {
                foreach (var kvp in equipped)
                {
                    if (kvp.Value == null)
                        spriteLayers[kvp.Key].sprite = null;
                    else
                        spriteLayers[kvp.Key].sprite
                            = atlas.GetSprite(kvp.Key, false, kvp.Value, state.anim, dir, state.curFrame);
                }
            }
        }

        private static readonly Dictionary<string, string> K_clothed = new Dictionary<string, string>
        {
            { "body/base", "base" },
            { "body/eyes", "blue" },
            { "body/nose", "buttonnose" },
            { "body/ears", "elvenears" },
            { "hair",  "princess" },
            { "face", null },

            { "feet",  "shoes_brown" },
            { "legs", "pants_white" },
            { "wrist", "bracers_leather" },
            { "hands", null },
            { "torso", "shirt_white" },
            { "tabbard", null },
            { "shoulders", null },
            { "belt", "leather" },
            { "head", "hat_leather" },
            //{ "weapon", }
        };

        private static readonly Dictionary<string, string> K_monk = new Dictionary<string, string>
        {
            { "body/base", "base" },
            { "body/eyes", "blue" },
            { "body/nose", "buttonnose" },
            { "body/ears", "elvenears" },
            { "hair",  null },
            { "face", null },

            { "feet",  "shoes_black" },
            { "legs", "robe" },
            { "wrist", null },
            { "hands", null },
            { "torso", "shirt_white" },
            { "tabbard", null },
            { "shoulders", null },
            { "belt", "cloth_white" },
            { "head", "hood_cloth" },
            //{ "weapon", }
        };

        private static readonly Dictionary<string, string> K_plate = new Dictionary<string, string>
        {
            { "body/base", "base" },
            { "body/eyes", "blue" },
            { "body/nose", "buttonnose" },
            { "body/ears", null },
            { "hair",  null },
            { "face", null },

            { "feet",  "plate_gold" },
            { "legs", "plate_gold" },
            { "wrist", null },
            { "hands", "plate_gold" },
            { "torso", "plate_gold" },
            { "tabbard", null },
            { "shoulders", "plate_gold" },
            { "belt", null },
            { "head", "plate_gold" },
            //{ "weapon", }
        };

        private static readonly List<Dictionary<string, string>> K_outfits =
            new List<Dictionary<string, string>>() { K_clothed, K_monk, K_plate };
    }
}