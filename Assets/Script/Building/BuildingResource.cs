using System.Collections.Generic;
using System;

using Vec2I = UnityEngine.Vector2Int;
using UnityEngine;

namespace BB
{
    public class BuildingProtoResource : IBuildingProto
    {
        private readonly BldgMineableDef def;

        public BuildingProtoResource(BldgMineableDef def)
            => this.def = def;

        public IBuilding CreateBuilding() => new BuildingResource(this);

        public bool passable => false;

        public BuildingBounds bounds => BuildingBounds.Unit;

        public RenderFlags renderFlags =>
            def.spriteOver == null ? RenderFlags.None : RenderFlags.Oversized;

        public TileSprite GetSprite(Map map, Vec2I pos, Vec2I subTile)
            => map.game.assets.sprites.Get(def.sprite);

        public TileSprite GetSpriteOver(Map map, Vec2I pos)
            => map.game.assets.sprites.Get(def.spriteOver);

        public class BuildingResource : BuildingBase<BuildingProtoResource>, IMineable
        {
            public float mineAmt { get; set; }
            public float mineTotal { get; }

            public BuildingResource(BuildingProtoResource proto) : base(proto)
                => mineAmt = mineTotal = 2;

            public Tool tool => proto.def.tool;

            public IEnumerable<ItemInfo> GetMinedMaterials()
            {
                foreach (var item in proto.def.resources)
                    yield return item;
            }

        }
    }
}