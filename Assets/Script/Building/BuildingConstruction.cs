using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    // TODO: do we actually even need this?
    public class BuildingProtoConstruction : IBuildingProto
    {
        public BuildingProtoConstruction() { }

        public BuildingConstruction Create(IBuildable proto, Dir dir)
            => new BuildingConstruction(this, proto, dir);

        public class BuildingConstruction : BuildingBase<BuildingProtoConstruction>
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

            public BuildingConstruction(BuildingProtoConstruction proto,
                IBuildable buildProto, Dir dir)
                : base(proto)
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

            public override TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
                => Virtualize(buildProto.GetSprite(map, dir, pos, subTile));

            public override TileSprite GetSpriteOver(Map map, Vec2I pos)
                => Virtualize(buildProto.GetSpriteOver(map, dir, pos));
        }

        public IBuilding CreateBuilding()
            => throw new NotSupportedException("CreateBuilding called on BuildingProtoConstruction");
        public bool passable
            => throw new NotSupportedException("passable called on BuildingProtoConstruction");
        public BuildingBounds Bounds(Dir dir)
            => throw new NotSupportedException("bounds called on BuildingProtoConstruction");
        public RenderFlags GetFlags(Dir dir)
            => throw new NotSupportedException("renderFlags called on BuildingProtoConstruction");
        public TileSprite GetSprite(Map map, Dir dir, Vec2I pos, Vec2I subTile)
            => throw new NotSupportedException("GetSprite called on BuildingProtoConstruction");
        public TileSprite GetSpriteOver(Map map, Dir dir, Vec2I pos)
            => throw new NotSupportedException("GetSpriteOver called on BuildingProtoConstruction");
    }
}