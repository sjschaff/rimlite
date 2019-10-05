using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public abstract class UITool2
    {
        public readonly GameController ctrl;
        protected UITool2(GameController ctrl) => this.ctrl = ctrl;

        public virtual void OnActivate() { }
        public virtual void OnDeactivate() { }
        // TODO: better name
        public virtual void OnSuspend() { }
        public virtual void OnUnsuspend() { }

        public virtual bool IsClickable() => false;
        public virtual bool IsDragable() => false;

        public virtual void OnClick(Vec2I pos) { }
        public virtual void OnDragStart(RectInt rect) { }
        public virtual void OnDrag(RectInt rect) { }
        public virtual void OnDragEnd(RectInt rect) { }

        public virtual void OnButton(int button) { }
        public virtual void K_OnTab() { }
    }

    public class ToolBuildSelect : UITool2
    {
        public readonly List<IBuildable> buildables
            = new List<IBuildable>();
        private readonly ToolBuild builder;
        private int selectedBuild = -1;

        public ToolBuildSelect(GameController ctrl)
            : base(ctrl)
        {
            builder = new ToolBuild(ctrl, this);
            foreach (var proto in ctrl.game.registry.buildings.Values)
            {
                if (proto is IBuildable buildable)
                    buildables.Add(buildable);
            }
        }

        public override void OnButton(int button)
        {
            BB.LogInfo($"build onbutton {button} cur: {selectedBuild}");
            if (selectedBuild >= 0)
                ctrl.PopTool();

            if (button != selectedBuild)
            {
                selectedBuild = button;
                ctrl.PushTool(builder);
            }
            else
                selectedBuild = -1;
            BB.LogInfo($"build endbutton {button} cur: {selectedBuild}");
        }

        public override void OnActivate()
        {
            ctrl.gui.ShowBuildButtons(buildables.Count);
            for (int i = 0; i < buildables.Count; ++i)
                ctrl.gui.buttons[i].SetText(buildables[i].name);
        }

        public override void OnDeactivate()
        {
            BB.LogInfo($"build deactivated");
            selectedBuild = -1;
            ctrl.gui.HideBuildButtons();
        }

        public class ToolBuild : UITool2
        {
            public readonly ToolBuildSelect selector;

            private ToolbarButton button;
            private IBuildable buildable;
            private Dir curDir;

            public ToolBuild(GameController ctrl, ToolBuildSelect selector)
                : base(ctrl) => this.selector = selector;

            public override void OnButton(int button)
                => selector.OnButton(button);

            public override void K_OnTab()
            {
                do
                {
                    curDir = curDir.NextCW();
                } while (!buildable.AllowedOrientations().Contains(curDir));

                BB.LogInfo($"Active Build: {buildable.GetType().Name}:{curDir}");
            }

            public override void OnClick(Vec2I pos)
            {
                var tile = ctrl.game.Tile(pos);
                if (ctrl.game.CanPlaceBuilding(tile, buildable.Bounds(curDir)))
                {
                    SystemBuild.K_instance.CreateBuild(buildable, tile, curDir);
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
                button = ctrl.gui.buttons[selector.selectedBuild];
                buildable = selector.buildables[selector.selectedBuild];
                curDir = buildable.AllowedOrientations().First();

                button.SetSelected(true);
                BB.LogInfo($"Active Build: {buildable.GetType().Name}:{curDir}");
            }

            public override void OnDeactivate()
            {
                button.SetSelected(false);
            }

            public override bool IsClickable() => true;
            public override bool IsDragable() => true;
        }
    }

    public abstract class UITool
    {
        protected readonly Game game;
        protected UITool(Game game) => this.game = game;
        public virtual void OnClick(Vec2I pos) { }
        protected virtual void OnDragStart(RectInt rect) => OnDragUpdate(rect);
        protected virtual void OnDragUpdate(RectInt rec) { }
        protected virtual void OnDragEnd(RectInt rect) { }
    }

    public class ToolControlMinion : UITool
    {
        public ToolControlMinion(Game game) : base(game) { }

        public override void OnClick(Vec2I pos)
        {
            if (game.Tile(pos).passable)
                game.K_MoveMinion(pos);
        }
    }

    public class ToolOrders : UITool
    {
        private IOrdersGiver currentOrders;
        private Dictionary<Vec2I, Transform> activeOverlays
            = new Dictionary<Vec2I, Transform>();

        public ToolOrders(Game game) : base(game) {
            // TODO: janky af
            currentOrders = game.registry.systems[1].orders;
        }

        public override void OnClick(Vec2I pos)
        {
            // TODO: handle items
            var tile = game.Tile(pos);
            if (currentOrders.ApplicableToBuilding(tile.building) && !currentOrders.HasOrder(tile))
                currentOrders.AddOrder(tile);
        }

        protected override void OnDragUpdate(RectInt rect)
        {
            List<Vec2I> toRemove = new List<Vec2I>();
            foreach (var kvp in activeOverlays)
            {
                if (!rect.Contains(kvp.Key))
                {
                    kvp.Value.Destroy();
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var v in toRemove)
                activeOverlays.Remove(v);

            foreach (var v in rect.allPositionsWithin)
            {
                if (!activeOverlays.ContainsKey(v))
                {
                    // TODO: handle items
                    var tile = game.Tile(v);
                    if (currentOrders.ApplicableToBuilding(tile.building) && !currentOrders.HasOrder(tile))
                    {
                        var o = currentOrders.CreateOverlay(tile);
                        activeOverlays.Add(v, o);
                    }
                }
            }
        }

        protected override void OnDragEnd(RectInt rect)
        {
            foreach (var v in rect.allPositionsWithin)
                OnClick(v);

            foreach (var kvp in activeOverlays)
                kvp.Value.Destroy();

            activeOverlays = new Dictionary<Vec2I, Transform>();
        }
    }

    public class ToolPlace : UITool
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