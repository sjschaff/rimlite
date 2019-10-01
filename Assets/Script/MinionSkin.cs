using System.Collections.Generic;
using System;
using UnityEngine;

using Vec3 = UnityEngine.Vector3;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class MetaAtlas
    {
        private struct AnimKey
        {
            public readonly string anim;
            public readonly string dir;
            public readonly string frame;

            public AnimKey(string anim, string dir, string frame)
            {
                this.anim = anim;
                this.dir = dir;
                this.frame = frame;
            }
        }

        private readonly CacheNonNullable<AnimKey, Atlas.Key> keys;
        private readonly Cache<string, Atlas> atlases;

        public MetaAtlas()
        {
            keys = new CacheNonNullable<AnimKey, Atlas.Key>(
                animKey =>
                {
                    var origin = new Vec2I(
                        OffsetFrame(animKey.frame),
                        20 - (OriginAnim(animKey.anim) + OffsetDir(animKey.dir)));

                    return new Atlas.Key(
                        origin * 2,
                        new Vec2I(2, 2),
                        new Vec2I(1, 0));
                });

            atlases = new Cache<string, Atlas>(
                path =>
                {
                    Texture2D tex = Resources.Load<Texture2D>(path);
                    if (tex == null)
                        Debug.LogError("No texture: " + path);

                    tex.filterMode = FilterMode.Point;
                    return new Atlas(tex, 32, 64);
                });
        }

        private static int OriginAnim(string anim)
        {
            switch (anim)
            {
                case "magic": return 0;
                case "thrust": return 4;
                case "walk": return 8;
                case "slash": return 12;
                case "shoot": return 16;
                case "hurt": return 20;
            }

            throw new Exception("unknown anim: " + anim);
        }

        private static int OffsetDir(string dir)
        {
            switch (dir)
            {
                case "up": return 0;
                case "left": return 1;
                case "down": return 2;
                case "right": return 3;
            }

            throw new Exception("unknown dir: " + dir);
        }

        private static int OffsetFrame(string frame) => Int32.Parse(frame);

        public Sprite GetSprite(string type, bool male, string name, string anim, string dir, string frame)
        {
            if (type == "tabbard")
                type = "torso";
            string path = "character/" + type + "/";
            if (type != "weapon")
                path += (male ? "male" : "female") + "/";
            path += name;

            return atlases.Get(path).GetSprite(keys.Get(new AnimKey(anim, dir, frame)));
        }
    }

    public enum MinionAnim
    {
        None, Magic, Walk, Slash, Thrust, Hurt
    }

    public class MinionSkin : MonoBehaviour
    {
        public enum Dir { Up, Left, Down, Right }

        private static MetaAtlas atlas;
        private Dictionary<string, SpriteRenderer> spriteLayers;

        private SpriteRenderer animDummy;
        private Animator animator;

        private Dictionary<string, string> equipped;
        public Dir dir { get; private set; } = Dir.Down;

        private string lastSprite = null;

        private void DirtySprite() => lastSprite = null;

        public void Init(AssetSrc assets)
        {
            if (atlas == null)
                atlas = new MetaAtlas();

            animDummy = assets.CreateSpriteObject(
                transform, new Vec2(.5f, 0), "AnimDummy", null, Color.white, RenderLayer.Highlight);
            animDummy.transform.localScale = Vec3.one * 1.75f;
            animDummy.enabled = false;
            animator = animDummy.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("anim/MinionAnimController");


            spriteLayers = new Dictionary<string, SpriteRenderer>();
            int renderLayer = 0;
            foreach (string layer in layers)
            {
                spriteLayers.Add(layer, assets.CreateSpriteObject(
                    animDummy.transform, Vec2.zero,
                    name, null, Color.white,
                    RenderLayer.Minion.Layer(renderLayer)));
                ++renderLayer;
            }

            K_SetOutfit(0);
        }

        void Awake() { }
        void Start() { }

        private string NameForTool(Tool tool)
        {
            switch (tool)
            {
                case Tool.None: return null;
                case Tool.Hammer: return "warhammer";
                case Tool.Pickaxe: return "pickaxe";
                case Tool.Axe: return "axe";
            }
            throw new Exception("unknown tool: " + tool);
        }

        public void SetTool(Tool tool)
        {
            equipped["weapon"] = NameForTool(tool);
            DirtySprite();
        }

        private void SetAnimLoop(MinionAnim anim, bool f)
        {
            switch (anim)
            {
                case MinionAnim.Walk:
                    animator.SetBool("walking", f);
                    break;
                case MinionAnim.Slash:
                    animator.SetBool("slash_loop", f);
                    break;
                case MinionAnim.Magic:
                    animator.SetBool("magic_loop", f);
                    break;
                default:
                    throw new NotImplementedException("Anim loop not implemented for: " + anim);
            }
        }

        public void SetAnimLoop(MinionAnim anim)
        {
            if (anim == MinionAnim.None)
            {
                SetAnimLoop(MinionAnim.Walk, false);
                SetAnimLoop(MinionAnim.Slash, false);
                SetAnimLoop(MinionAnim.Magic, false);
            }
            else
                SetAnimLoop(anim, true);
        }

        public bool PlayAnimOnce(MinionAnim anim)
        {
            throw new NotImplementedException("anim once not implemented for " + anim);
            /*
                animator.SetTrigger("slash");
                animator.SetTrigger("thrust");
                animator.SetTrigger("magic");
                animator.SetTrigger("shoot");
             */
        }

        public void SetDir(Dir dir)
        {
            this.dir = dir;
            DirtySprite();
        }

        public void SetDir(Vec2 dir)
        {
            if (Mathf.Abs(dir.x) > float.Epsilon)
            {
                if (dir.x < 0)
                    SetDir(MinionSkin.Dir.Left);
                else
                    SetDir(MinionSkin.Dir.Right);
            }
            else
            {
                if (dir.y < 0)
                    SetDir(MinionSkin.Dir.Down);
                else
                    SetDir(MinionSkin.Dir.Up);
            }
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
        };

        int k_curOutfit = 0;
        private void K_SetOutfit(int i)
        {
            k_curOutfit = i;
            equipped = new Dictionary<string, string>(
                K_outfits[i]);
            DirtySprite();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown("z"))
                K_SetOutfit((k_curOutfit + 1) % K_outfits.Count);
        }

        private void LateUpdate()
        {
            if (animDummy.sprite != null)
            {
                string spriteName = animDummy.sprite.name;
                if (lastSprite != spriteName)
                {
                    lastSprite = animDummy.sprite.name;
                    var vals = spriteName.Split('_');
                    BB.Assert(vals.Length == 2);
                    string anim = vals[0];
                    string frame = vals[1];

                    foreach (var kvp in equipped)
                    {
                        if (kvp.Value == null)
                            spriteLayers[kvp.Key].sprite = null;
                        else
                            spriteLayers[kvp.Key].sprite
                                = atlas.GetSprite(kvp.Key, false, kvp.Value, anim, dir.ToString().ToLower(), frame);
                    }
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