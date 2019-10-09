using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public abstract partial class Agent
    {
#if DEBUG
        private static int D_nextID = 0;
        public readonly int D_uniqueID;
#endif

        public readonly Game game;
        public readonly AgentDef def;
        public Transform transform { get; private set; }

        public Work currentWork { get; private set; }
        public bool hasWork => currentWork != null;

        public Item carriedItem { get; private set; }
        public bool carryingItem => carriedItem != null;

        // TODO: make an api that doesn't suck
        public abstract void SetTool(Tool tool);
        public abstract void SetAnim(MinionAnim anim);
        public abstract void SetFacing(Vec2 dir);
        protected abstract void ReconfigureItem(); // Jank AF

        public Agent(Game game, AgentDef def, Vec2I pos, string nodeName)
        {
#if DEBUG
            D_uniqueID = D_nextID;
            ++D_nextID;
#endif
            this.game = game;
            this.def = def;
            transform = new GameObject(nodeName).transform;
            transform.SetParent(game.agentContainer, false);
            realPos = pos;
        }

        public float speed => 2;

        public Vec2 realPos
        {
            get => transform.position.xy();
            private set => transform.position = value;
        }

        public Vec2I pos { get { BB.Assert(GridAligned()); return realPos.Floor(); } }

        public bool GridAligned()
        {
            var p = realPos;
            return (p.x % 1f) < Mathf.Epsilon && (p.y % 1) < Mathf.Epsilon;
        }

        const float containmentThresh = .9f;
        public bool InTile(Vec2I tile) =>
            Vec2.Distance(realPos, tile) < containmentThresh;

        public bool InArea(RectInt area)
        {
            Vec2 dist = (realPos - area.center).Abs();
            Vec2 halfSize = (Vec2)area.size * .5f;
            float invThresh = 1 - containmentThresh;
            return
                dist.x < (halfSize.x - invThresh) &&
                dist.y < (halfSize.y - invThresh);
        }

        public void PickupItem(Item item)
        {
            BB.Assert(!carryingItem);
            carriedItem = item;
            carriedItem.ReParent(transform, new Vec2(0, .2f));
            ReconfigureItem();
        }

        public Item RemoveItem()
        {
            BB.AssertNotNull(carriedItem);
            Item ret = carriedItem;
            carriedItem = null;
            return ret;
        }

        public void DropItem() => game.K_DropItem(game.Tile(pos), RemoveItem());

        public void Update(float deltaTime)
        {
            if (currentWork != null)
                currentWork.PerformWork(deltaTime);
        }

        public bool AssignWork(Work work)
        {
            if (currentWork != null)
                currentWork.Abandon(this);

            // TODO: if can do work:
            {
                currentWork = work;
                currentWork.ClaimWork(this);
                return true;
            }
            // else return false
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
                realPos = realPos.Floor();
            }

            if (carryingItem)
                DropItem();
        }

        public void AbandonWork()
        {
            BB.AssertNotNull(currentWork);
            currentWork.Abandon(this);
            RemoveWork(currentWork);
        }
    }
}