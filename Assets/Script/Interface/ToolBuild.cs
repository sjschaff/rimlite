﻿using System.Linq;
using System;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class ToolBuildSelect : ToolSelector<IBuildable, ToolBuild>
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

        public override void ConfigureButton(
            ToolbarButton button, IBuildable buildable, Action fn)
            => button.Configure(fn, null, buildable.buildingDef.name);

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
    }

    public class ToolBuild : ToolSelectorManipulator<IBuildable, ToolBuild>
    {
        private static readonly Color colorAllowed = new Color(.2f, .6f, .2f);
        private static readonly Color colorDisallowed = new Color(.6f, .2f, .2f);

        private readonly Line outlineAllow;
        private readonly Line outlineDisallow;

        private readonly SystemBuild builds;
        private Dir curDir;

        public ToolBuild(GameController ctrl) : base(ctrl)
        {
            builds = ctrl.registry.GetSystem<SystemBuild>();
            BB.AssertNotNull(builds);

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

        public override void OnUpdate(Vec2 realMouse)
        {
            Vec2I mouse = realMouse.Floor();
            var bounds = selection.Bounds(curDir);
            bool valid =
                ctrl.game.ValidTile(mouse) && 
                ctrl.game.CanPlaceBuilding(bounds.AsRect(mouse));
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

        public override void OnClick(Vec2 realPos)
        {
            Vec2I pos = realPos.Floor();
            if (ctrl.game.CanPlaceBuilding(selection.Bounds(curDir).AsRect(pos)))
            {
                var tile = ctrl.game.Tile(pos);
                builds.CreateBuild(selection, tile, curDir);
                //ctrl.game.AddBuilding(selection.CreateBuilding(tile, curDir));
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