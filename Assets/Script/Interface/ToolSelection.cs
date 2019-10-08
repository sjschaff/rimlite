using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;

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

    public class ToolSelection : UITool,
        IAgentListener,
        IItemListener,
        IBuildingListener
    {
        #region Vis

        #endregion

        private class Selectable : IEquatable<Selectable>
        {
            public SelectionHighlight highlight;
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

            public void AddHighlight(SelectionHighlight highlight)
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
        private readonly Pool<SelectionHighlight> highlights;

        private readonly HashSet<Selectable> selectables = new HashSet<Selectable>();
        private readonly List<IOrdersGiver> ordersCurrent = new List<IOrdersGiver>();

        public ToolSelection(GameController ctrl)
            : base(ctrl)
        {
            poolRoot = new GameObject("Selection Highlights").transform;
            poolRoot.SetParent(ctrl.gui.root, false);

            highlights = new Pool<SelectionHighlight>(
                () => new SelectionHighlight(ctrl.assets, poolRoot));

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

        private void DeactiveHighlight(Selectable selectable)
        {
            if (selectable.highlight != null)
            {
                selectable.highlight.Disable();
                highlights.Return(selectable.highlight);
                selectable.highlight = null;
            }
        }

        private void ClearHighlights()
        {
            foreach (var selectable in selectables)
                DeactiveHighlight(selectable);
        }

        public override void OnUpdate(float dt)
        {
            foreach (var selectable in selectables)
                selectable.highlight.Update(dt);
        }

        private void InvalidateUI()
        {
            if (selectables.Count == 0)
            {
                ctrl.PopAll();
                return;
            }

            ClearUI();
            InitUI();
        }

        private void ClearUI()
        {
            ctrl.gui.HideToolbarButtons();
            ordersCurrent.Clear();
        }

        private void InitUI()
        {
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
        }

        public override void OnActivate()
        {
            BB.Assert(selectables.Count > 0);
            foreach (var selectable in selectables)
            {
                if (selectable.highlight == null)
                    selectable.AddHighlight(highlights.Get());
            }

            InitUI();
            ctrl.game.RegisterAgentListener(this);
            ctrl.game.RegisterItemListener(this);
            ctrl.game.RegisterBuildingListener(this);
            base.OnActivate();
        }

        public override void OnDeactivate()
        {
            ClearUI();
            ClearHighlights();
            ctrl.game.UnregisterAgentListener(this);
            ctrl.game.UnregisterItemListener(this);
            ctrl.game.UnregisterBuildingListener(this);
            base.OnDeactivate();
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

        #region Invalidation
        private void SelectableRemoved(Selectable selectable)
        {
            // TODO: handle this tool not being on top of the stack
            if (selectables.TryGetValue(selectable, out var selActual))
            {
                DeactiveHighlight(selActual);
                selectables.Remove(selectable);
                InvalidateUI();
            }
        }

        private void SelectableChanged(Selectable selectable)
        {
            if (selectables.Contains(selectable))
                InvalidateUI();
        }

        public void AgentRemoved(Agent agent)
            => SelectableRemoved(new Selectable(agent));
        public void ItemRemoved(TileItem item)
            => SelectableRemoved(new Selectable(item));
        public void BuildingRemoved(IBuilding building)
            => SelectableRemoved(new Selectable(building));

        public void AgentChanged(Agent agent)
            => SelectableChanged(new Selectable(agent));
        public void ItemChanged(TileItem item)
            => SelectableChanged(new Selectable(item));

        public void AgentAdded(Agent agent) { }
        public void ItemAdded(TileItem item) { }
        public void BuildingAdded(IBuilding building) { }
        #endregion
    }
}