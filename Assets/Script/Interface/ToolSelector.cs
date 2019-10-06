using System.Collections.Generic;

namespace BB
{
    public abstract class ToolSelectorManipulator<TSelectable, TManipulator> : UITool
        where TManipulator : ToolSelectorManipulator<TSelectable, TManipulator>
    {
        private ToolSelector<TSelectable, TManipulator> selector;
        protected TSelectable selection;

        protected ToolSelectorManipulator(GameController ctrl)
            : base(ctrl) { }

        public void Init(ToolSelector<TSelectable, TManipulator> selector)
            => this.selector = selector;

        public void Configure(TSelectable selection)
            => this.selection = selection;

        public override void OnButton(int button)
            => selector.OnManipulatorButton(button);
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
            manipulator.Init(this);
        }

        public void OnManipulatorButton(int button)
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
}