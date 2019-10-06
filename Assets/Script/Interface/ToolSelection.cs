using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class ToolSelection : UITool
    {
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


        private class Selection
        {
            public readonly ISelectable selectable;
            public Highlight highlight;

            public Selection(ISelectable selectable)
            {
                this.selectable = selectable;
                this.highlight = null;
            }
        }

        private readonly Transform poolRoot;
        private readonly Pool<Highlight> highlights;
        private readonly List<Selection> selections;

        public ToolSelection(GameController ctrl)
            : base(ctrl)
        {
            selections = new List<Selection>();
            poolRoot = new GameObject("Selection Highlights").transform;
            poolRoot.SetParent(ctrl.gui.root, false);

            highlights = new Pool<Highlight>(
                () => new Highlight(ctrl.assets, poolRoot));
        }

        public void SetSelection(ISelectable selectable)
        {
            ClearHighlights();
            selections.Clear();
            selections.Add(new Selection(selectable));
            ctrl.ReplaceTool(this);
        }

        public void SetSelection(IEnumerable<ISelectable> selectables)
        {
            ClearHighlights();
            selections.Clear();
            foreach (var selectable in selectables)
                selections.Add(new Selection(selectable));
            ctrl.ReplaceTool(this);
        }

        private void ClearHighlights()
        {
            BB.LogInfo("clearing highlights");
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

        public override void OnActivate()
        {
            BB.Assert(selections.Count > 0);
            foreach (var selection in selections)
            {
                selection.highlight = highlights.Get();

                RectInt rect;
                if (selection.selectable is IBuilding building) // TODO: jank
                    rect = building.bounds;
                else
                    rect = new RectInt(selection.selectable.pos, Vec2I.one);
                selection.highlight.Enable(rect);
            }

            if (selections.Count == 1)
            {
                ctrl.gui.infoPane.header.text = selections[0].selectable.def.name;
            }
            else
            {
                DefNamed def = selections[0].selectable.def;
                bool allSame = true;
                foreach (Selection selection in selections)
                {
                    if (selection.selectable.def != def)
                    {
                        allSame = false;
                        break;
                    }
                }

                string name = allSame ? def.name : "Various";
                ctrl.gui.infoPane.header.text = $"{name}x{selections.Count}";
            }

            base.OnActivate();
        }

        public override void OnDeactivate()
        {
            ClearHighlights();
            base.OnDeactivate();
        }
    }
}