using System.Collections.Generic;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class ToolOrdersSelect : ToolSelector<IOrdersGiver, ToolOrders>
    {
        public ToolOrdersSelect(GameController ctrl)
            : base(ctrl, new ToolOrders(ctrl))
        {
            foreach (var system in ctrl.registry.systems)
            {
                foreach (var orders in system.GetOrders())
                    if (!orders.SelectionOnly())
                        selectables.Add(orders);
            }
        }

        public override void ConfigureButton(ToolbarButton button, IOrdersGiver orders)
            => button.Configure(ctrl.assets, orders);

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
    }

    public class ToolOrders : ToolSelectorManipulator<IOrdersGiver, ToolOrders>
    {
        private Dictionary<Vec2I, Transform> dragOverlays
            = new Dictionary<Vec2I, Transform>();

        public ToolOrders(GameController ctrl) : base(ctrl) {}

        public override void OnClick(Vec2 realPos)
        {
            var pos = realPos.Floor();
            var tile = ctrl.game.Tile(pos);
            if (tile.hasBuilding && selection.ApplicableToBuilding(tile.building))
                selection.AddOrder(tile.building);

            if (tile.hasItems)
            {
                foreach (var item in ctrl.game.GUISelectItemsOnTile(tile))
                {
                    if (selection.ApplicableToItem(item))
                        selection.AddOrder(item);
                }
            }

            var agent = ctrl.game.GUISelectMinion(pos);
            if (agent != null && selection.ApplicableToAgent(agent))
                selection.AddOrder(agent);
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

            // TODO: handle agents
            foreach (var v in rect.allPositionsWithin)
            {
                if (!dragOverlays.ContainsKey(v))
                {
                    // TODO: handle items
                    // TODO: overlays will be a bit wierd for large buildings
                    // but it should work out
                    var tile = ctrl.game.Tile(v);
                    if (tile.hasBuilding && selection.ApplicableToBuilding(tile.building))
                    {
                        var overlay = ctrl.assets.CreateJobOverlay(
                            ctrl.game.workOverlays, v, selection.GuiSprite());
                        dragOverlays.Add(v, overlay.transform);
                    }

                    if (tile.hasItems)
                    {
                        foreach (var item in ctrl.game.GUISelectItemsOnTile(tile))
                        {
                            if (selection.ApplicableToItem(item))
                            {
                                var overlay = ctrl.assets.CreateJobOverlay(
                                    ctrl.game.workOverlays, v, selection.GuiSprite());
                                dragOverlays.Add(v, overlay.transform);
                                break;
                            }
                        }
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