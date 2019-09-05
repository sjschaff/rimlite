using System;
using System.Collections.Generic;
using UnityEngine;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

public class MetaAtlas
{
    private struct AnimKey
    {
        public string anim;
        public string dir;
        public string frame;
    }

    private Dictionary<AnimKey, Atas.Key> keys;
    private Dictionary<string, Atas> atlases;

    public MetaAtlas()
    {
        keys = new Dictionary<AnimKey, Atas.Key>();
        atlases = new Dictionary<string, Atas>();
    }

    private int OriginAnim(string anim)
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

    private int OffsetDir(string dir)
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

    private int OffsetFrame(string frame) => Int32.Parse(frame);

    private Atas.Key GetKey(string anim, string dir, string frame)
    {
        AnimKey animKey;
        animKey.anim = anim;
        animKey.dir = dir;
        animKey.frame = frame;
        if (keys.TryGetValue(animKey, out var key))
            return key;

        var origin = new Vec2I(OffsetFrame(frame), 20 - (OriginAnim(anim) + OffsetDir(dir)));

        key = new Atas.Key(
            origin * 2,
            new Vec2I(2, 2),
            new Vec2I(1, 0),
            64);

        keys.Add(animKey, key);
        return key;
    }

    public Sprite GetSprite(string type, bool male, string name, string anim, string dir, string frame)
    {
        if (type == "tabbard")
            type = "torso";
        string path = "character/" + type + "/";
        if (type != "weapon")
            path += (male ? "male" : "female") + "/";
        path += name;

        if (!atlases.TryGetValue(path, out var atlas))
        {
            Texture2D tex = Resources.Load<Texture2D>(path);
            BB.Assert(tex != null, "no texture: " + path);
            tex.filterMode = FilterMode.Point;
            atlas = new Atas(tex, 32);
            atlases.Add(path, atlas);
        }

        return atlas.GetSprite(GetKey(anim, dir, frame));
    }
}

public class MinionSkin : MonoBehaviour
{
    static readonly string[] dirs = { "up", "left", "down", "right" };
    public enum Dir { Up = 0, Left = 1, Down = 2, Right = 3 }

    public enum Tool { None, Hammer, Pickaxe, Axe };


    private static MetaAtlas atlas = null;
    private Dictionary<string, SpriteRenderer> spriteLayers;

    private SpriteRenderer animDummy;
    private Animator animator;

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

    public void SetTool(Tool tool) => equipped["weapon"] = NameForTool(tool);

    public void SetWalking(bool w) => animator.SetBool("walking", w);

    public void SetSlashing(bool w) => animator.SetBool("slash_loop", w);

    public void SetDir(Dir dir) => curDir = (int)dir;

    public void SetDir(Vec2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
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


    string k_curSprite;
    int curDir = 0;

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

    private static Dictionary<string, string> clothed = new Dictionary<string, string>
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

    private static Dictionary<string, string> monk = new Dictionary<string, string>
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

    private static Dictionary<string, string> plate = new Dictionary<string, string>
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

    private Dictionary<string, string> equipped = clothed;

    private SpriteRenderer CreateSpriteLayer(string name, int renderLayer)
    {
        var layer = new GameObject(name);
        layer.transform.parent = transform;
        layer.transform.localPosition = Vec3.zero;

        var sprite = layer.AddComponent<SpriteRenderer>();
        sprite.sortingLayerName = "Player";
        sprite.sortingLayerID = SortingLayer.NameToID("Player");
        sprite.sortingOrder = renderLayer;

        return sprite;
    }

    void Awake()
    {
        if (atlas == null)
            atlas = new MetaAtlas();

        animDummy = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    // Start is called before the first frame update
    void Start()
    {
        spriteLayers = new Dictionary<string, SpriteRenderer>();

        int renderLayer = 0;
        foreach (string layer in layers)
        {
            spriteLayers.Add(layer, CreateSpriteLayer(layer, renderLayer));
            ++renderLayer;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("w"))
            animator.SetTrigger("slash");
        if (Input.GetKeyDown("e"))
            animator.SetTrigger("thrust");
        if (Input.GetKeyDown("r"))
            animator.SetTrigger("magic");
        if (Input.GetKeyDown("t"))
            animator.SetTrigger("shoot");

        if (Input.GetKeyDown("a"))
        {
            curDir = (curDir + 1) % dirs.Length;
        }
        if (Input.GetKeyDown("z"))
        {
            if (equipped == clothed)
                equipped = monk;
            else if (equipped == monk)
                equipped = plate;
            else
                equipped = clothed;
        }
    }

    private void LateUpdate()
    {
       // if (spriteRenderer.sprite.name != k_curSprite)
        if (animDummy.sprite != null)
        {
            k_curSprite = animDummy.sprite.name;
            var vals = k_curSprite.Split('_');
            BB.Assert(vals.Length == 3);
            string anim = vals[0];
            string dir = vals[1];
            string frame = vals[2];
          //  Debug.Log("[" + anim + ", " + dir + ", " + frame + "]");

            foreach (var kvp in equipped)
            {
                if (kvp.Value == null)
                    spriteLayers[kvp.Key].sprite = null;
                else
                    spriteLayers[kvp.Key].sprite = atlas.GetSprite(kvp.Key, false, kvp.Value, anim, dirs[curDir], frame);
            }

            //spriteRenderer.sprite = atlas.GetSprite(anim, dirs[curDir], frame);
           // weaponRenderer.sprite = weaponAtlas.GetSprite(anim, dirs[curDir], frame);
        }
    }
}
