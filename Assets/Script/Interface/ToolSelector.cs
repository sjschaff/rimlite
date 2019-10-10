using System.Collections.Generic;
using System;

namespace BB
{
    public abstract class ToolSelectorManipulator<TSelectable, TManipulator> : UITool
        where TManipulator : ToolSelectorManipulator<TSelectable, TManipulator>
    {
        protected TSelectable selection;

        protected ToolSelectorManipulator(GameController ctrl)
            : base(ctrl) { }

        public void Configure(TSelectable selection)
            => this.selection = selection;
    }

    public abstract class ToolSelector<TSelectable, TManip> : UITool
        where TManip : ToolSelectorManipulator<TSelectable, TManip>
    {
        protected readonly List<TSelectable> selectables;
        private readonly TManip manipulator;
        private int selection = -1;

        protected ToolSelector(GameController ctrl, TManip manipulator)
            : base(ctrl)
        {
            this.selectables = new List<TSelectable>();
            this.manipulator = manipulator;
        }

        private void OnButton(int button)
        {
            bool wasSelected = button == selection;
            if (selection >= 0)
                ctrl.PopTool();

            if (!wasSelected)
            {
                selection = button;
                ctrl.gui.buttons[selection].SetSelected(true);
                manipulator.Configure(selectables[selection]);
                ctrl.PushTool(manipulator);
            }
        }

        public override void OnUnsuspend()
        {
            if (selection != -1)
            {
                ctrl.gui.buttons[selection].SetSelected(false);
                selection = -1;
            }
        }

        public override void OnActivate()
        {
            ctrl.gui.ShowToolbarButtons(selectables.Count);
            for (int i = 0; i < selectables.Count; ++i)
                ConfigureButton(ctrl.gui.buttons[i], selectables[i],
                    ToolbarAction(i));
        }

        private Action ToolbarAction(int i) => () => OnButton(i);

        public override void OnDeactivate()
        {
            selection = -1;
            ctrl.gui.HideToolbarButtons();
        }

        public abstract void ConfigureButton(
            ToolbarButton button, TSelectable selectable, Action fn);
    }
}