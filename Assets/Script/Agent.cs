using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public enum Tool { None, Hammer, Pickaxe, Axe };

    public partial class Minion
    {
#if DEBUG
        private static int D_nextID = 0;
        public readonly int D_uniqueID;
#endif

        public readonly GameController game;
        public MinionSkin skin { get; }
        public Work currentWork { get; private set; }

        public Item carriedItem { get; private set; }
        public bool carryingItem => carriedItem != null;
        public float speed => 2;
        public bool hasWork => currentWork != null;

        private Vec2 realPos
        {
            get => skin.transform.position.xy();
            set => skin.transform.position = value;
        }

        public Vec2I pos { get { BB.Assert(GridAligned()); return realPos.Floor(); } }

        public bool GridAligned()
        {
            var p = realPos;
            return (p.x % 1f) < Mathf.Epsilon && (p.y % 1) < Mathf.Epsilon;
        }

        public bool InTile(Vec2I tile) => Vec2.Distance(realPos, tile) < .9f;

        public Minion(GameController game, Vec2 pos)
        {
#if DEBUG
            D_uniqueID = D_nextID;
            ++D_nextID;
#endif

            this.game = game;
            skin = new GameObject("Minion").AddComponent<MinionSkin>();
            skin.transform.SetParent(game.transform, false);
            skin.Init(game.assets);
            realPos = pos;
        }

        public void SetFacing(Vec2 dir)
        {
            skin.SetDir(dir);
            if (carriedItem != null)
                ReconfigureItem();
        }

        private void ReconfigureItem()
        {
            BB.AssertNotNull(carriedItem);
            carriedItem.Configure(
                skin.dir == MinionSkin.Dir.Up ?
                    Item.Config.PlayerBelow :
                    Item.Config.PlayerAbove);
        }

        public void PickupItem(Item item)
        {
            BB.Assert(!carryingItem);
            carriedItem = item;
            carriedItem.ReParent(skin.transform, new Vec2(0, .2f));
            ReconfigureItem();
        }

        public Item RemoveItem()
        {
            BB.AssertNotNull(carriedItem);
            Item ret = carriedItem;
            carriedItem = null;
            return ret;
        }

        public void DropItem() => game.DropItem(pos, RemoveItem());

        public void Update(float deltaTime)
        {
            if (currentWork != null)
                currentWork.PerformWork(deltaTime);
        }

        public bool AssignWork(Work work)
        {
            if (currentWork != null)
                currentWork.Abandon(this);

            currentWork = work;
            return currentWork.ClaimWork(this);
        }

        public void RemoveWork(Work work)
        {
            BB.AssertNotNull(currentWork);
            BB.Assert(work == currentWork);
            currentWork = null;

            // TODO: handle case of not being Grid Aligned
            if (!GridAligned())
            {
                Debug.LogWarning("minion work removed while not grid aligned.");
                realPos = pos;
            }
        }

        public void AbandonWork()
        {
            BB.AssertNotNull(currentWork);
            currentWork.Abandon(this);
            RemoveWork(currentWork);
        }
    }
}