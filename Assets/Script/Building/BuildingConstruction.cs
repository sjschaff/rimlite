using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class BuildingConstruction : BuildingBase<IBuildingProto>
    {
        public readonly IBuildable buildProto;
        public override Dir dir { get; }

        // All stuff used by JobBuild
        // TODO: this whole file should prob be moved to BuildSystem
        public readonly List<MaterialInfo> materials;
        public bool constructionBegan;
        public float constructionPercent; // or some such thing...
        public bool hasBuilder;

        public class MaterialInfo
        {
            public readonly ItemInfo info;
            public int amtStored;
            public int amtClaimed;

            public int amtRemaining => info.amt - amtStored;
            public int haulRemaining => amtRemaining - amtClaimed;

            public MaterialInfo(ItemInfo info)
            {
                this.info = info;
                this.amtStored = this.amtClaimed = 0;
            }

            public int HaulAmount(Item item) => Math.Min(haulRemaining, item.amtAvailable);
        }


        public bool HasAvailableHauls()
        {
            foreach (var info in materials)
                if (info.haulRemaining > 0)
                    return true;

            return false;
        }

        public bool HasAllMaterials()
        {
            foreach (var info in materials)
                if (info.amtRemaining != 0)
                    return false;

            return true;
        }

        public BuildingConstruction(IBuildable buildProto, Dir dir)
            : base(null)
        {
            this.buildProto = buildProto;
            this.dir = dir;
            constructionBegan = false;
            constructionPercent = 0;
            materials = new List<MaterialInfo>(
                buildProto.GetBuildMaterials()
                .Select(i => new MaterialInfo(i)));
        }

        public override bool passable => constructionBegan ? buildProto.passable : true;
        public override BuildingBounds bounds => buildProto.Bounds(dir);
        public override RenderFlags renderFlags => buildProto.GetFlags(dir);

        private TileSprite Virtualize(TileSprite sprite)
            => new TileSprite(sprite.sprite, sprite.color * new Color(.6f, .6f, 1, .5f));

        public override TileSprite GetSprite(Vec2I pos, Vec2I subTile)
            => Virtualize(buildProto.GetSprite(dir, pos, subTile));

        public override TileSprite GetSpriteOver(Vec2I pos)
            => Virtualize(buildProto.GetSpriteOver(dir, pos));
    }
}