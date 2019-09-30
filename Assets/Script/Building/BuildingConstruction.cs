using System.Collections.Generic;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{

    public class BuildingProtoConstruction : IBuildingProto
    {
        public static BuildingProtoConstruction K_single = new BuildingProtoConstruction();

        public BuildingProtoConstruction() { }

        public BuildingConstruction Create(JobBuild job)
            => new BuildingConstruction(this, job);

        public class BuildingConstruction : BuildingBase<BuildingProtoConstruction>
        {
            public JobBuild job;
            public bool constructionBegan;
            public float constructionPercent; // or some such thing...

            public BuildingConstruction(BuildingProtoConstruction proto, JobBuild job) : base(proto)
            {
                this.job = job;
                constructionBegan = false;
                constructionPercent = 0;
            }

            public override bool passable => true;
            public override BuildingBounds bounds => job.prototype.bounds;
            public override RenderFlags renderFlags => job.prototype.renderFlags;

            private TileSprite Virtualize(TileSprite sprite)
                => new TileSprite(sprite.sprite, sprite.color * new Color(.6f, .6f, 1, .5f));

            public override TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
                => Virtualize(job.prototype.GetSprite(map, pos, subTile));

            public override TileSprite GetSpriteOver(Map map, Vec2I pos)
                => Virtualize(job.prototype.GetSpriteOver(map, pos));
        }

        public IBuilding CreateBuilding()
            => throw new NotSupportedException("CreateBuilding called on BuildingProtoConstruction");
        public IEnumerable<ItemInfo> GetBuildMaterials()
            => throw new NotSupportedException("GetBuildMaterials called on BuildingProtoConstruction");
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