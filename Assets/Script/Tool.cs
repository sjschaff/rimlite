using System.Collections.Generic;
using System.Linq;
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

    public abstract class ToolSelector<TSelectable, TManip> : UITool
        where TManip : ToolSelector<TSelectable, TManip>.Manipulator
    {
        public abstract class Manipulator : UITool
        {
            private ToolSelector<TSelectable, TManip> selector;
            protected TSelectable selection;

            protected Manipulator(GameController ctrl) : base(ctrl) { }

            public void Init(ToolSelector<TSelectable, TManip> selector)
                => this.selector = selector;

            public void Configure(TSelectable selection)
                => this.selection = selection;

            public override void OnButton(int button)
                => selector.OnManipButton(button);
        }

        protected readonly List<TSelectable> selectables;
        private readonly TManip manipulator;
        private int selection = -1;

        protected ToolSelector(GameController ctrl, TManip manipulator)
            : base(ctrl)
        {
            this.selectables = new List<TSelectable>();
            this.manipulator = manipulator;
            manipulator.Init(this);
        }

        private void OnManipButton(int button)
        {
            ctrl.gui.buttons[selection].SetSelected(false);
            OnButton(button);
        }

        public override void OnButton(int button)
        {
            if (selection >= 0)
                ctrl.PopTool();

            if (button != selection)
            {
                selection = button;
                ctrl.gui.buttons[selection].SetSelected(true);
                manipulator.Configure(selectables[selection]);
                ctrl.PushTool(manipulator);
            }
            else
                selection = -1;
        }

        public override void OnActivate()
        {
            ctrl.gui.ShowBuildButtons(selectables.Count);
            for (int i = 0; i < selectables.Count; ++i)
                ConfigureButton(ctrl.gui.buttons[i], selectables[i]);
        }

        public override void OnDeactivate()
        {
            selection = -1;
            ctrl.gui.HideBuildButtons();
        }

        public abstract void ConfigureButton(ToolbarButton button, TSelectable selectable);
    }

    public class ToolBuildSelect : ToolSelector<IBuildable, ToolBuildSelect.ToolBuild>
    {
        public ToolBuildSelect(GameController ctrl)
            : base(ctrl, new ToolBuild(ctrl))
        {
            foreach (var proto in ctrl.registry.buildings.Values)
            {
                if (proto is IBuildable buildable)
                    selectables.Add(buildable);
            }
        }

        public override void ConfigureButton(ToolbarButton button, IBuildable buildable)
            => button.Configure(null, buildable.name);

        public override void OnActivate()
        {
            ctrl.gui.buildButton.SetSelected(true);
            base.OnActivate();
        }

        public override void OnDeactivate()
        {
            ctrl.gui.buildButton.SetSelected(false);
            base.OnDeactivate();
        }

        public class ToolBuild : Manipulator
        {
            private static readonly Color colorAllowed = new Color(.2f, .6f, .2f);
            private static readonly Color colorDisallowed = new Color(.6f, .2f, .2f);

            private readonly Line outlineAllow;
            private readonly Line outlineDisallow;

            private Dir curDir;

            public ToolBuild(GameController ctrl) : base(ctrl)
            {
                outlineAllow = ctrl.assets.CreateLine(
                    ctrl.gui.root, "Build Outline",
                    RenderLayer.Highlight,
                    colorAllowed,
                    1 / 32f, true, false);
                outlineAllow.enabled = false;

                outlineDisallow = ctrl.assets.CreateLine(
                    ctrl.gui.root, "Build Outline",
                    RenderLayer.Highlight,
                    colorDisallowed,
                    1 / 32f, true, false);
                outlineDisallow.enabled = false;
            }

            public override void OnUpdate(Vec2I mouse)
            {
                var bounds = selection.Bounds(curDir);
                bool valid =
                    ctrl.game.ValidTile(mouse) && 
                    ctrl.game.CanPlaceBuilding(ctrl.game.Tile(mouse), bounds);
                var area = bounds.AsRect(mouse);

                outlineAllow.SetRect(area);
                outlineAllow.enabled = valid;

                outlineDisallow.SetRect(area);
                outlineDisallow.enabled = !valid;
            }

            public override void OnMouseExit()
            {
                outlineAllow.enabled = false;
                outlineDisallow.enabled = false;
            }

            public override void K_OnTab()
            {
                do {
                    curDir = curDir.NextCW();
                } while (!selection.AllowedOrientations().Contains(curDir));
            }

            public override void OnClick(Vec2I pos)
            {
                var tile = ctrl.game.Tile(pos);
                if (ctrl.game.CanPlaceBuilding(tile, selection.Bounds(curDir)))
                {
                    SystemBuild.K_instance.CreateBuild(selection, tile, curDir);
                    //game.AddBuilding(pos, curProto.CreateBuilding(curDir));
                }
            }

            public override void OnDragEnd(RectInt rect)
            {
                foreach (var v in rect.allPositionsWithin)
                    OnClick(v);
            }

            public override void OnActivate()
            {
                curDir = selection.AllowedOrientations().First();
                BB.LogInfo($"Active Build: {selection.GetType().Name}:{curDir}");
            }

            public override void OnDeactivate()
            {
                outlineAllow.enabled = false;
                outlineDisallow.enabled = false;
            }

            public override bool IsClickable() => true;
            public override bool IsDragable() => true;
        }
    }

    public class ToolOrdersSelect : ToolSelector<IOrdersGiver, ToolOrdersSelect.ToolOrders>
    {
        public ToolOrdersSelect(GameController ctrl)
            : base(ctrl, new ToolOrders(ctrl))
        {
            foreach (var system in ctrl.registry.systems)
            {
                if (system.orders != null)
                    selectables.Add(system.orders);
            }
        }

        public override void ConfigureButton(ToolbarButton button, IOrdersGiver orders)
            => button.Configure(
                ctrl.assets.sprites.Get(orders.GuiSprite()), orders.GuiText());

        public override void OnActivate()
        {
            ctrl.gui.orderButton.SetSelected(true);
            base.OnActivate();
        }

        public override void OnDeactivate()
        {
            ctrl.gui.orderButton.SetSelected(false);
            base.OnDeactivate();
        }

        public class ToolOrders : Manipulator
        {
            private Dictionary<Vec2I, Transform> dragOverlays
                = new Dictionary<Vec2I, Transform>();

            public ToolOrders(GameController ctrl) : base(ctrl) {}

            public override void OnClick(Vec2I pos)
            {
                // TODO: handle items
                var tile = ctrl.game.Tile(pos);
                if (tile.hasBuilding &&
                    selection.ApplicableToBuilding(tile.building) &&
                    !selection.HasOrder(tile))
                    selection.AddOrder(tile);
            }

            public override void OnDragStart(RectInt rect)
                => OnDrag(rect);

            public override void OnDrag(RectInt rect)
            {
                List<Vec2I> toRemove = new List<Vec2I>();
                foreach (var kvp in dragOverlays)
                {
                    if (!rect.Contains(kvp.Key))
                    {
                        kvp.Value.Destroy();
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (var v in toRemove)
                    dragOverlays.Remove(v);

                foreach (var v in rect.allPositionsWithin)
                {
                    if (!dragOverlays.ContainsKey(v))
                    {
                        // TODO: handle items
                        // TODO: overlays will be a bit wierd for large buildings
                        // but it should work out
                        var tile = ctrl.game.Tile(v);
                        if (tile.hasBuilding && selection.ApplicableToBuilding(tile.building) &&
                            !selection.HasOrder(tile))
                        {
                            var overlay = ctrl.assets.CreateJobOverlay(
                                ctrl.game.transform, v, selection.GuiSprite());
                            dragOverlays.Add(v, overlay.transform);
                        }
                    }
                }
            }

            public override void OnDragEnd(RectInt rect)
            {
                foreach (var v in rect.allPositionsWithin)
                    OnClick(v);

                DestroyDragOverlays();
            }

            private void DestroyDragOverlays()
            {
                foreach (var kvp in dragOverlays)
                    kvp.Value.Destroy();

                dragOverlays = new Dictionary<Vec2I, Transform>();
            }

            public override bool IsClickable() => true;
            public override bool IsDragable() => true;
        }
    }

    public abstract class UIToolOLD
    {
        protected readonly Game game;
        protected UIToolOLD(Game game) => this.game = game;
        public virtual void OnClick(Vec2I pos) { }
        protected virtual void OnDragStart(RectInt rect) => OnDragUpdate(rect);
        protected virtual void OnDragUpdate(RectInt rec) { }
        protected virtual void OnDragEnd(RectInt rect) { }
    }

    public class ToolControlMinion : UIToolOLD
    {
        public ToolControlMinion(Game game) : base(game) { }

        public override void OnClick(Vec2I pos)
        {
            if (game.Tile(pos).passable)
                game.K_MoveMinion(pos);
        }
    }

    public class ToolPlace : UIToolOLD
    {
        public ToolPlace(Game game) : base(game) { }

        public override void OnClick(Vec2I pos)
        {
            //game.ModifyTerrain(pos, new Terrain(game, game.defs.Get<TerrainDef>("BB:Path")));
            // game.AddBuilding(pos, game.registry.walls.Get(game.defs.Get<BldgWallDef>("BB:StoneBrick"))
            //     .CreateBuilding());
        }
    }
}