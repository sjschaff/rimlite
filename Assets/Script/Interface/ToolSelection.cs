﻿using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;

namespace BB
{
    public class ToolSelection : UITool,
        IAgentListener,
        IItemListener,
        IBuildingListener
    {
        #region Selectable
        private abstract class Selectable : IEquatable<Selectable>
        {
            public SelectionHighlight highlight;
            protected Selectable() { }
            public abstract DefNamed def { get; }
            public virtual int count => 1;

            public void SetHighlight(SelectionHighlight highlight)
            {
                this.highlight = highlight;
                ConfigureHighlight();
            }

            protected abstract void ConfigureHighlight();
            public abstract bool Applicable(IOrdersGiver orders);
            public abstract void Issue(IOrdersGiver orders);
            public abstract bool Equals(Selectable other);
            public override bool Equals(object obj)
                => obj is Selectable selectable && Equals(selectable);
            public override abstract int GetHashCode();
        }

        private class SelAgent : Selectable
        {
            public readonly Agent agent;
            public SelAgent(Agent agent) => this.agent = agent;
            public override DefNamed def => agent.def;
            protected override void ConfigureHighlight()
                => highlight.Enable(agent);
            public override bool Applicable(IOrdersGiver orders)
                => orders.ApplicableToAgent(agent);
            public override void Issue(IOrdersGiver orders)
                => orders.AddOrder(agent);
            public override bool Equals(Selectable other)
                => other is SelAgent oa && oa.agent == agent;
            public override int GetHashCode()
                => 159371670 + EqualityComparer<Agent>.Default.GetHashCode(agent);
        }

        private class SelMinion : SelAgent
        {
            public readonly Minion minion;
            public SelMinion(Minion minion) : base(minion)
                => this.minion = minion;
        }

        private class SelItem : Selectable
        {
            public readonly TileItem item;
            public SelItem(TileItem item) => this.item = item;
            public override DefNamed def => item.def;
            public override int count => item.amt;
            protected override void ConfigureHighlight()
                => highlight.Enable(new Rect(item.tile.pos, Vec2.one));
            public override bool Applicable(IOrdersGiver orders)
                => orders.ApplicableToItem(item);
            public override void Issue(IOrdersGiver orders)
                => orders.AddOrder(item);
            public override bool Equals(Selectable other)
                => other is SelItem oi && oi.item == item;
            public override int GetHashCode()
                => -1566986794 + EqualityComparer<TileItem>.Default.GetHashCode(item);
        }

        private class SelBuilding : Selectable
        {
            public readonly IBuilding building;
            public SelBuilding(IBuilding building) => this.building = building;
            public override DefNamed def => building.def;
            protected override void ConfigureHighlight()
                => highlight.Enable(building.bounds.AsRect());
            public override bool Applicable(IOrdersGiver orders)
                => orders.ApplicableToBuilding(building);
            public override void Issue(IOrdersGiver orders)
                => orders.AddOrder(building);
            public override bool Equals(Selectable other)
                => other is SelBuilding ob && ob.building == building;
            public override int GetHashCode()
                => -1229560131 + EqualityComparer<IBuilding>.Default.GetHashCode(building);
        }
        #endregion

        private readonly List<IOrdersGiver> orders = new List<IOrdersGiver>();
        private readonly List<ICommandsGiver> commands = new List<ICommandsGiver>();
        private readonly Transform poolRoot;
        private readonly Pool<SelectionHighlight> highlights;

        private readonly HashSet<Selectable> selectables = new HashSet<Selectable>();
        private readonly HashSet<SelMinion> minions = new HashSet<SelMinion>();

        private readonly List<Selectable> selectablesRemoved = new List<Selectable>();
        private bool isIssuing;

        public ToolSelection(GameController ctrl)
            : base(ctrl)
        {
            poolRoot = new GameObject("Selection Highlights").transform;
            poolRoot.SetParent(ctrl.gui.root, false);

            highlights = new Pool<SelectionHighlight>(
                () => new SelectionHighlight(ctrl.assets, poolRoot));

            foreach (var system in ctrl.registry.systems)
                foreach (var c in system.GetCommands())
                    commands.Add(c);

            foreach (var system in ctrl.registry.systems)
                foreach (var o in system.GetOrders())
                    orders.Add(o);

            isIssuing = false;
        }

        private void OnButton(IOrdersGiver order)
        {
            isIssuing = true;
            foreach (var selectable in selectables)
                if (selectable.Applicable(order))
                    selectable.Issue(order);
            isIssuing = false;
            OnFinishIssuing();
        }

        private void OnButton(ICommandsGiver command)
        {
            isIssuing = true;
            foreach (var minion in minions)
                if (command.ApplicableToMinion(minion.minion))
                    command.IssueCommand(minion.minion);
            isIssuing = false;
            OnFinishIssuing();
        }

        private void OnFinishIssuing()
        {
            foreach (var selectable in selectablesRemoved)
                RemoveSelectable(selectable);
            selectablesRemoved.Clear();
            InvalidateUI();
        }

        private static IEnumerable<Selectable> ToSelectables(Selection selection)
        {
            // TODO: other humans, animals
            foreach (var minion in selection.minions)
                yield return new SelMinion(minion);
            foreach (var item in selection.items)
                yield return new SelItem(item);
            foreach (var building in selection.buildings)
                yield return new SelBuilding(building);
        }

        public void SetSelection(Selection selection)
        {
            ClearHighlights();
            selectables.Clear();
            minions.Clear();
            AddSelection(selection);
        }

        public void AddSelection(Selection selection)
        {
            foreach (var sel in ToSelectables(selection))
                if (!selectables.Contains(sel))
                {
                    selectables.Add(sel);
                    if (sel is SelMinion minion)
                        minions.Add(minion);
                }

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
        }

        private void InitUI()
        {
            ConfigureInfoPane();

            List<KeyValuePair<IToolbarButton, Action>> buttons
                = new List<KeyValuePair<IToolbarButton, Action>>();

            foreach (var command in commands)
            {
                foreach (var minion in minions)
                {
                    if (command.ApplicableToMinion(minion.minion))
                    {
                        buttons.Add(new KeyValuePair<IToolbarButton, Action>(
                            command, () => OnButton(command)));
                        break;
                    }
                }
            }

            foreach (var order in orders)
            {
                foreach (var selectable in selectables)
                {
                    if (selectable.Applicable(order))
                    {
                        buttons.Add(new KeyValuePair<IToolbarButton, Action>(
                            order, () => OnButton(order)));
                        break;
                    }
                }
            }

            ctrl.gui.ShowToolbarButtons(buttons.Count);
            for (int i = 0; i < buttons.Count; ++i)
                ctrl.gui.buttons[i].Configure(buttons[i].Value, ctrl.assets, buttons[i].Key);
        }

        public override void OnActivate()
        {
            BB.Assert(selectables.Count > 0);
            foreach (var selectable in selectables)
            {
                if (selectable.highlight == null)
                    selectable.SetHighlight(highlights.Get());
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
            bool allSame = true;
            int count = 0;
            foreach (var selectable in selectables)
            {
                if (allSame && selectable.def != def)
                    allSame = false;

                count += selectable.count;
            }

            string text = allSame ? def.name : "Various";
            if (count > 1)
                text += $" x{count}";
            ctrl.gui.infoPane.header.text = text;

            if (selectables.Count == 1 && first is SelBuilding b)
            {
                IBuilding building = b.building;
                ctrl.gui.infoPane.header.text =
                    $"{building.jobHandles.Count} handles.";
            }
            if (selectables.Count == 1 && first is SelMinion m)
            {
                Minion minion = m.minion;
                if (minion.hasWork)
                    ctrl.gui.infoPane.header.text =
                    $"Active Work: {minion.currentWork.D_workName}, task: {minion.currentWork.activeTask.description}";
                else
                    ctrl.gui.infoPane.header.text = "No work.";
            }
        }

        #region Invalidation
        private bool RemoveSelectable(Selectable selectable)
        {
            if (selectables.TryGetValue(selectable, out var selActual))
            {
                DeactiveHighlight(selActual);
                selectables.Remove(selectable);
                if (selectable is SelMinion minion)
                    minions.Remove(minion);

                return true;
            }

            return false;
        }

        private void SelectableRemoved(Selectable selectable)
        {
            if (isIssuing)
            {
                if (selectables.Contains(selectable))
                    selectablesRemoved.Add(selectable);
            }
            else
            {
                // TODO: handle this tool not being on top of the stack
                if (RemoveSelectable(selectable))
                    InvalidateUI();
            }
        }

        private void SelectableChanged(Selectable selectable)
        {
            if (!isIssuing && selectables.Contains(selectable))
                InvalidateUI();
        }

        public void AgentRemoved(Agent agent)
            => SelectableRemoved(new SelAgent(agent));
        public void ItemRemoved(TileItem item)
            => SelectableRemoved(new SelItem(item));
        public void BuildingRemoved(IBuilding building)
            => SelectableRemoved(new SelBuilding(building));

        public void AgentChanged(Agent agent)
            => SelectableChanged(new SelAgent(agent));
        public void ItemChanged(TileItem item)
            => SelectableChanged(new SelItem(item));

        public void AgentAdded(Agent agent) { }
        public void ItemAdded(TileItem item) { }
        public void BuildingAdded(IBuilding building) { }
        #endregion
    }
}