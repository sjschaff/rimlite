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
        public abstract void UpdateSkinDir();

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

            /*    var line = game.assets.CreateLine(
                    transform, "DgbBounds", RenderLayer.Highlight,
                    Color.blue, 1 / 32f, true, false);
                line.SetCircle(def.bounds, 32);*/
        }

        // TODO: this is turning into a shit show

        public float speed => 2;

        public Circle bounds => def.bounds + realPos;

        public Dir dir { get; private set; } = Dir.Down;

        public void SetFacing(Vec2 dir)
        {
            float dot = Vec2.Dot(dir.normalized, Vec2.right);
            if (dot < .65f && dot > -.65f)
            {
                if (dir.y > 0)
                    this.dir = Dir.Up;
                else
                    this.dir = Dir.Down;
            }
            else
            {
                if (dir.x > 0)
                    this.dir = Dir.Right;
                else
                    this.dir = Dir.Left;
            }

            UpdateSkinDir();
            if (carriedItem != null)
                ReconfigureItem();
        }

        private void ReconfigureItem()
        {
            BB.AssertNotNull(carriedItem);
            carriedItem.Configure(
                dir == Dir.Up ?
                    Item.Config.PlayerBelow :
                    Item.Config.PlayerAbove);
        }

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

        public bool InTile(Vec2I tile)
            => bounds.Intersects(new Rect(tile, Vec2.one));

        public bool InArea(RectInt area)
            => bounds.Intersects(new Rect(area.min, area.size));

        public void PickupItem(Item item)
        {
            BB.Assert(!carryingItem);
            carriedItem = item;
            carriedItem.ReParent(transform);
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

        public bool HasLineOfSight(Vec2I target) =>
            game.HasLineOfSight(bounds.center, target + new Vec2(.5f, .5f));

        public virtual void Update(float deltaTime)
        {
            if (currentWork != null)
                currentWork.PerformWork(deltaTime);
        }

        public virtual bool AssignWork(Work work)
        {
            if (currentWork != null)
                RemoveWork(true, true);

            // TODO: if can do work:
            {
                currentWork = work;
                currentWork.ClaimWork(this);
                return true;
            }
            // else return false
        }

        private void RemoveWork(bool abandon, bool hasNewWork)
        {
            BB.AssertNotNull(currentWork);
            if (abandon)
                currentWork.Abandon(this);
            currentWork = null;

            if (!hasNewWork && !GridAligned())
            {
                Debug.LogWarning("minion work removed while not grid aligned.");
                realPos = realPos.Floor();
            }

            if (carryingItem)
                DropItem();
        }

        public void WorkCompleted(Work work)
        {
            BB.AssertNotNull(currentWork);
            BB.Assert(work == currentWork);
            RemoveWork(false, false);
        }

        public void AbandonWork()
        {
            BB.AssertNotNull(currentWork);
            if (!GridAligned())
                JobTransient.AssignWork(this, "WalkGridAlign",
                    new TaskGoTo(game, "Wandering.", PathCfg.Anywhere(realPos)).Enumerate());
            else
                RemoveWork(true, false);
        }
    }
}