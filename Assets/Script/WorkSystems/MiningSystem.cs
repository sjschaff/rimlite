using System.Collections.Generic;
using System;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public interface IMineable : IBuilding
    {
        Tool tool { get; }
        IEnumerable<ItemInfo> GetMinedMaterials();
    }

    public class MiningSystem : OrdersBase<MiningSystem.MineWork>, IWorkSystem
    {
        public class MineWork : WorkHandle
        {
            public readonly IMineable building;
            // job

            public MineWork(MiningSystem system, Vec2I pos) : base(system, pos) { }
        }

        public MiningSystem(GameController game) : base(game)
        {
            sprite = game.defs.Get<SpriteDef>("BB:MineOverlay");
        }

        // TODO: maybe we dont need this
        protected override MineWork CreateWork(Vec2I pos)
        {
            // TODO: make this more correct
            game.AddJob(new JobMine(game, pos));
            return new MineWork(this, pos);
        } 

        public IOrdersGiver orders => this;
        public override OrdersFlags flags => OrdersFlags.AppliesBuilding | OrdersFlags.AppliesGlobally;
        protected override SpriteDef sprite { get; }
        public override bool ApplicableToBuilding(IBuilding building) => building is IMineable;
        public override bool ApplicableToItem(Item item) => throw new NotSupportedException();
    }
}