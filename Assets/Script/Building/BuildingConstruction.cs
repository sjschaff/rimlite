using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class BuildingProtoConstruction : IBuildingProto
    {
        public BuildingProtoConstruction() { }

        public BuildingConstruction Create(IBuildable proto)
            => new BuildingConstruction(this, proto);

        public class BuildingConstruction : BuildingBase<BuildingProtoConstruction>
        {
            public readonly IBuildable buildProto;

            public class MaterialInfo
            {
                public readonly ItemInfoRO info;
                public int amtStored;
                public int amtClaimed;

                public int amtRemaining => info.amt - amtStored;
                public int haulRemaining => amtRemaining - amtClaimed;

                public MaterialInfo(ItemInfoRO info)
                {
                    this.info = info;
                    this.amtStored = this.amtClaimed = 0;
                }

               public int HaulAmount(Item item) => Math.Min(haulRemaining, item.amtAvailable);
            }

            // All stuff used by JobBuild
            // TODO: this whole file should prob be moved to BuildSystem
            public readonly List<MaterialInfo> materials;
            public bool constructionBegan;
            public float constructionPercent; // or some such thing...
            public bool hasBuilder;

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

            public BuildingConstruction(BuildingProtoConstruction proto, IBuildable buildProto)
                : base(proto)
            {
                this.buildProto = buildProto;
                constructionBegan = false;
                constructionPercent = 0;
                materials = new List<MaterialInfo>(
                    buildProto.GetBuildMaterials()
                    .Select(i => new MaterialInfo(i)));
            }

            public override bool passable => constructionBegan ? buildProto.passable : true;
            public override BuildingBounds bounds => buildProto.bounds;
            public override RenderFlags renderFlags => buildProto.renderFlags;

            private TileSprite Virtualize(TileSprite sprite)
                => new TileSprite(sprite.sprite, sprite.color * new Color(.6f, .6f, 1, .5f));

            public override TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
                => Virtualize(buildProto.GetSprite(map, pos, subTile));

            public override TileSprite GetSpriteOver(Map map, Vec2I pos)
                => Virtualize(buildProto.GetSpriteOver(map, pos));
        }

        public IBuilding CreateBuilding()
            => throw new NotSupportedException("CreateBuilding called on BuildingProtoConstruction");
        public bool passable
            => throw new NotSupportedException("passable called on BuildingProtoConstruction");
        public BuildingBounds bounds
            => throw new NotSupportedException("bounds called on BuildingProtoConstruction");
        public RenderFlags renderFlags
            => throw new NotSupportedException("renderFlags called on BuildingProtoConstruction");
        public TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
            => throw new NotSupportedException("GetSprite called on BuildingProtoConstruction");
        public TileSprite GetSpriteOver(Map map, Vec2I pos)
            => throw new NotSupportedException("GetSpriteOver called on BuildingProtoConstruction");
    }
}