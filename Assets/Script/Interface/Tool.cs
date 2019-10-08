using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public abstract class UITool
    {
        public readonly GameController ctrl;
        protected UITool(GameController ctrl) => this.ctrl = ctrl;

        public virtual void OnActivate() { }
        public virtual void OnDeactivate() { }
        public virtual void OnSuspend() { }
        public virtual void OnUnsuspend() { }
        public virtual void OnUpdate() { }

        public virtual bool IsClickable() => false;
        public virtual bool IsDragable() => false;

        public virtual void OnUpdate(Vec2I mouse) { }
        public virtual void OnClick(Vec2I pos) { }
        public virtual void OnDragStart(RectInt rect) { }
        public virtual void OnDrag(RectInt rect) { }
        public virtual void OnDragEnd(RectInt rect) { }
        public virtual void OnMouseEnter() { }
        public virtual void OnMouseExit() { }

        public virtual void OnButton(int button) { }
        public virtual void K_OnTab() { }
    }
}