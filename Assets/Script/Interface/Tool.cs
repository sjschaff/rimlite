using UnityEngine;
using UnityEngine.EventSystems;

using Vec2 = UnityEngine.Vector2;

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
        public virtual void OnUpdate(float dt) { }

        public virtual bool IsClickable() => false;
        public virtual bool IsDragable() => false;

        public virtual void OnUpdate(Vec2 mouse) { }
        public virtual void OnClick(Vec2 pos) { }
        public virtual void OnRightClick(Vec2 pos, Vec2 scPos) { }
        public virtual void OnDragStart(RectInt rect) { }
        public virtual void OnDrag(RectInt rect) { }
        public virtual void OnDragEnd(RectInt rect) { }
        public virtual void OnMouseEnter() { }
        public virtual void OnMouseExit() { }

        public virtual void K_OnTab() { }

        public virtual void RawMouseDown(PointerEventData.InputButton button, Vec2 pos) {}
        public virtual void RawMouseUp(PointerEventData.InputButton button, Vec2 pos) {}
        public virtual void RawMouseMove(Vec2 pos) {}
    }
}
