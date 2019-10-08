﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class Selection
    {
        public readonly List<Minion> minions
            = new List<Minion>();
        // TODO: other humans
        public readonly List<TileItem> items
            = new List<TileItem>();
        public readonly List<IBuilding> buildings
            = new List<IBuilding>();
        // TODO: animals

        public void FilterType()
        {
            if (minions.Count > 0)
            {
                items.Clear();
                buildings.Clear();
            }
            else if (items.Count > 0)
            {
                buildings.Clear();
            }
        }

        public bool Empty()
        {
            return
                minions.Count == 0 &&
                items.Count == 0 &&
                buildings.Count == 0;
        }

        public void Add(Selection sel)
        {
            minions.AddRange(sel.minions);
            items.AddRange(sel.items);
            buildings.AddRange(sel.buildings);
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

            private readonly Transform parent;
            private readonly Transform container;
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
                this.parent = parent;
                this.container = new GameObject("<highlight>").transform;
                container.SetParent(parent, false);

                bl = CreateLine(assets, container, ptsBL);
                br = CreateLine(assets, container, ptsBR);
                tl = CreateLine(assets, container, ptsTL);
                tr = CreateLine(assets, container, ptsTR);
            }

            public void Enable(Agent agent)
            {
                agent.AttachSelectionHighlight(container);
                SetRect(new Rect(Vec2.zero, Vec2.one));
            }

            public void Enable(Rect rect) => SetRect(rect);

            private void SetRect(Rect rect)
            {
                bl.position = rect.min;
                br.position = new Vec2(rect.xMax, rect.yMin);
                tr.position = rect.max;
                tl.position = new Vec2(rect.xMin, rect.yMax);
                bl.enabled = br.enabled = tr.enabled = tl.enabled = true; 
            }

            public void Disable()
            {
                bl.enabled = br.enabled = tr.enabled = tl.enabled = false;
                container.SetParent(parent, false);
            }
        }
        #endregion

        private class Selectable : IEquatable<Selectable>
        {
            public Highlight highlight;
            public readonly Agent agent;
            public readonly TileItem item;
            public readonly IBuilding building;

            private Selectable(Agent agent, TileItem item, IBuilding building)
            {
                this.agent = agent;
                this.item = item;
                this.building = building;
                this.highlight = null;
            }
            public Selectable(Agent agent)
                : this(agent, null, null) { }

            public Selectable(TileItem item)
                : this(null, item, null) { }

            public Selectable(IBuilding building)
                : this(null, null, building) { }

            public DefNamed def
            {
                get
                {
                    if (agent != null)
                        return agent.def;
                    else if (item != null)
                        return item.def;
                    else
                        return building.def;
                }
            }

            public void AddHighlight(Highlight highlight)
            {
                // TODO: animate
                this.highlight = highlight;

                if (agent != null)
                    highlight.Enable(agent);
                else if (building != null)
                    highlight.Enable(building.bounds.AsRect());
                else
                    highlight.Enable(new Rect(item.tile.pos, Vec2.one));
            }

            public bool Applicable(IOrdersGiver orders)
            {
                if (item != null)
                    return orders.ApplicableToItem(item);
                else if (building != null)
                    return orders.ApplicableToBuilding(building);

                return false;
            }

            #region Equality
            public override bool Equals(object obj)
            {
                return obj is Selectable selectable && Equals(selectable);
            }

            public bool Equals(Selectable other)
            {
                return EqualityComparer<Agent>.Default.Equals(agent, other.agent) &&
                       EqualityComparer<TileItem>.Default.Equals(item, other.item) &&
                       EqualityComparer<IBuilding>.Default.Equals(building, other.building);
            }

            public override int GetHashCode()
            {
                var hashCode = 1264995022;
                hashCode = hashCode * -1521134295 + EqualityComparer<Agent>.Default.GetHashCode(agent);
                hashCode = hashCode * -1521134295 + EqualityComparer<TileItem>.Default.GetHashCode(item);
                hashCode = hashCode * -1521134295 + EqualityComparer<IBuilding>.Default.GetHashCode(building);
                return hashCode;
            }

            public static bool operator ==(Selectable left, Selectable right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Selectable left, Selectable right)
            {
                return !(left == right);
            }
            #endregion
        }

        private readonly List<IOrdersGiver> orders = new List<IOrdersGiver>();
        private readonly Transform poolRoot;
        private readonly Pool<Highlight> highlights;

        private readonly HashSet<Selectable> selectables = new HashSet<Selectable>();
        private readonly List<IOrdersGiver> ordersCurrent = new List<IOrdersGiver>();

        public ToolSelection(GameController ctrl)
            : base(ctrl)
        {
            poolRoot = new GameObject("Selection Highlights").transform;
            poolRoot.SetParent(ctrl.gui.root, false);

            highlights = new Pool<Highlight>(
                () => new Highlight(ctrl.assets, poolRoot));

            foreach (var system in ctrl.registry.systems)
            {
                if (system.orders != null)
                    orders.Add(system.orders);
            }
        }

        public override void OnButton(int button)
        {
            IOrdersGiver orders = ordersCurrent[button];
            foreach (var selectable in selectables)
            {
                if (selectable.item != null)
                {
                    TileItem item = selectable.item;
                    if (orders.ApplicableToItem(item) &&
                        !orders.HasOrder(item.tile))
                        orders.AddOrder(item.tile);
                }
                else if (selectable.building != null)
                {
                    IBuilding building = selectable.building;
                    if (orders.ApplicableToBuilding(building) &&
                        !orders.HasOrder(building.tile))
                        orders.AddOrder(building.tile);
                }

                // TODO: agents
            }
        }

        private static IEnumerable<Selectable> ToSelectables(Selection selection)
        {
            // TODO: other humans, animals
            foreach (var minion in selection.minions)
                yield return new Selectable(minion);
            foreach (var item in selection.items)
                yield return new Selectable(item);
            foreach (var building in selection.buildings)
                yield return new Selectable(building);
        }

        public void SetSelection(Selection selection)
        {
            ClearHighlights();
            selectables.Clear();
            foreach (var sel in ToSelectables(selection))
                selectables.Add(sel);

            ctrl.ReplaceTool(this);
        }

        public void AddSelection(Selection selection)
        {
            foreach (var sel in ToSelectables(selection))
                if (!selectables.Contains(sel))
                    selectables.Add(sel);

            ctrl.ReplaceTool(this);
        }

        private void ClearHighlights()
        {
            foreach (var selectable in selectables)
            {
                if (selectable.highlight != null)
                {
                    selectable.highlight.Disable();
                    highlights.Return(selectable.highlight);
                    selectable.highlight = null;
                }
            }
        }

        private void ConfigureInfoPane()
        {
            var first = selectables.First();
            DefNamed def = first.def;
            bool isItem = first.item != null;
            bool allSame = true;
            int itemCount = 0;
            foreach (var selectable in selectables)
            {
                if (selectable.def != def)
                {
                    allSame = false;
                    break;
                }

                if (isItem)
                    itemCount += selectable.item.amt;
            }

            string text;
            if (allSame && isItem)
            {
                text = $"{def.name} x{itemCount}";
            }
            else
            {
                text = allSame ? def.name : "Various";
                if (selectables.Count > 1)
                    text += $" x{selectables.Count}";
            }
            ctrl.gui.infoPane.header.text = text;
        }

        public override void OnUpdate()
        {
        }

        public override void OnActivate()
        {
            BB.Assert(selectables.Count > 0);
            foreach (var selectable in selectables)
            {
                if (selectable.highlight == null)
                    selectable.AddHighlight(highlights.Get());
            }

            ConfigureInfoPane();

            foreach (IOrdersGiver orders in orders)
            {
                foreach (var selectable in selectables)
                {
                    if (selectable.Applicable(orders))
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