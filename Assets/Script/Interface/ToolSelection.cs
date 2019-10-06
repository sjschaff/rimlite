using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public struct Selectable
    {
        public readonly IBuilding building;
        public readonly Item item;
        // TODO: agents

        private Selectable(IBuilding building, Item item)
        {
            this.building = building;
            this.item = item;
        }

        public Selectable(IBuilding building)
            : this(building, null) { }

        public Selectable(Item item)
            : this(null, item) { }

        public DefNamed def
        {
            get
            {
                if (building != null)
                    return building.def;
                else
                    return item.def;
            }
        }

        public RectInt rect
        {
            get
            {
                if (building != null)
                    return building.bounds;
                else
                    return new RectInt(item.tile.pos, Vec2I.one);
            }
        }

        // TODO: we should just get rid of orders flags
        // and default AppliesToX can return false
        public bool Applicable(IOrdersGiver orders)
        {
            if (building != null)
            {
                if (orders.flags.HasFlag(OrdersFlags.AppliesBuilding))
                    return orders.ApplicableToBuilding(building);
            }
            else
            {
                if (orders.flags.HasFlag(OrdersFlags.AppliesItem))
                    return orders.ApplicableToItem(item);
            }

            return false;
        }
    }

    public class ToolSelection : UITool
    {
        #region Vis
        private class Highlight
        {
            private static readonly Vec2[] ptsBL = new Vec2[]
            {
                new Vec2(0, .25f),
                new Vec2(0, .1f),
                new Vec2(.1f, 0),
                new Vec2(.25f, 0)
            };

            private static readonly Vec2[] ptsBR =
                ptsBL.Select(pt => new Vec2(-pt.x, pt.y)).ToArray();
            private static readonly Vec2[] ptsTR =
                ptsBL.Select(pt => new Vec2(-pt.x, -pt.y)).ToArray();
            private static readonly Vec2[] ptsTL =
                ptsBL.Select(pt => new Vec2(pt.x, -pt.y)).ToArray();

            private readonly Line bl, br, tl, tr;

            private static Line CreateLine(AssetSrc assets, Transform parent, Vec2[] pts)
            {
                var line = assets.CreateLine(
                    parent, "<corner>",
                    RenderLayer.Default.Layer(10000),
                    Color.white, 1 / 32f, false, false);
                line.SetPts(pts);
                return line;
            }

            public Highlight(AssetSrc assets, Transform parent)
            {
                var container = new GameObject("<highlight>").transform;
                container.SetParent(parent, false);

                bl = CreateLine(assets, container, ptsBL);
                br = CreateLine(assets, container, ptsBR);
                tl = CreateLine(assets, container, ptsTL);
                tr = CreateLine(assets, container, ptsTR);
            }

            public void Enable(RectInt rect)
            {
                bl.position = rect.min;
                br.position = new Vec2(rect.xMax, rect.yMin);
                tr.position = rect.max;
                tl.position = new Vec2(rect.xMin, rect.yMax);
                bl.enabled = br.enabled = tr.enabled = tl.enabled = true; 
            }

            public void Disable()
                => bl.enabled = br.enabled = tr.enabled = tl.enabled = false;
        }
        #endregion

        private class Selection
        {
            public readonly Selectable selectable;
            public Highlight highlight;

            public Selection(Selectable selectable)
            {
                this.selectable = selectable;
                this.highlight = null;
            }
        }

        private readonly List<IOrdersGiver> orders;
        private readonly Transform poolRoot;
        private readonly Pool<Highlight> highlights;
        private readonly List<Selection> selections;
        private readonly List<IOrdersGiver> ordersCurrent;

        public ToolSelection(GameController ctrl)
            : base(ctrl)
        {
            selections = new List<Selection>();
            poolRoot = new GameObject("Selection Highlights").transform;
            poolRoot.SetParent(ctrl.gui.root, false);

            highlights = new Pool<Highlight>(
                () => new Highlight(ctrl.assets, poolRoot));

            orders = new List<IOrdersGiver>();
            ordersCurrent = new List<IOrdersGiver>();
            foreach (var system in ctrl.registry.systems)
            {
                if (system.orders != null)
                    orders.Add(system.orders);
            }
        }

        public override void OnButton(int button)
        {
            IOrdersGiver orders = ordersCurrent[button];
            foreach (var selection in selections)
            {
                if (selection.selectable.Applicable(orders))
                    // TODO: MEGA Kludge
                    orders.AddOrder(selection.selectable.building.tile);
            }
        }

        public void SetSelection(Selectable selectable)
        {
            ClearHighlights();
            selections.Clear();
            selections.Add(new Selection(selectable));
            ctrl.ReplaceTool(this);
        }

        public void SetSelection(List<Selectable> selectables)
        {
            if (selectables.Count == 0)
                return;

            ClearHighlights();
            selections.Clear();
            foreach (var selectable in selectables)
                selections.Add(new Selection(selectable));
            ctrl.ReplaceTool(this);
        }

        private void ClearHighlights()
        {
            foreach (var selection in selections)
            {
                if (selection.highlight != null)
                {
                    selection.highlight.Disable();
                    highlights.Return(selection.highlight);
                    selection.highlight = null;
                }
            }
        }

        private void ConfigureInfoPane()
        {
            var first = selections[0].selectable;
            DefNamed def = first.def;
            bool isItem = first.item != null;
            bool allSame = true;
            int itemCount = 0;
            foreach (Selection selection in selections)
            {
                if (selection.selectable.def != def)
                {
                    allSame = false;
                    break;
                }

                if (isItem)
                    itemCount += selection.selectable.item.amt;

            }

            string text;
            if (allSame && isItem)
            {
                text = $"{def.name} x{itemCount}";
            }
            else
            {
                text = allSame ? def.name : "Various";
                if (selections.Count > 1)
                    text += $" x{selections.Count}";
            }
            ctrl.gui.infoPane.header.text = text;
        }

        public override void OnActivate()
        {
            BB.Assert(selections.Count > 0);
            foreach (var selection in selections)
            {
                selection.highlight = highlights.Get();
                selection.highlight.Enable(selection.selectable.rect);
            }

            ConfigureInfoPane();

            foreach (IOrdersGiver orders in orders)
            {
                foreach (var selection in selections)
                {
                    if (selection.selectable.Applicable(orders))
                    {
                        ordersCurrent.Add(orders);
                        break;
                    }
                }
            }

            ctrl.gui.ShowToolbarButtons(ordersCurrent.Count);
            for (int i = 0; i < ordersCurrent.Count; ++i)
                ctrl.gui.buttons[i].Configure(ctrl.assets, ordersCurrent[i]);

            base.OnActivate();
        }

        public override void OnDeactivate()
        {
            ctrl.gui.HideToolbarButtons();
            ordersCurrent.Clear();
            ClearHighlights();
            base.OnDeactivate();
        }
    }
}