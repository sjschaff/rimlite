using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    // TODO: where should this live
    // TODO: make better
    public class ToolbarButton
    {
        public static Color onColor = new Color(.4f, .4f, .4f);
        public static Color offColor = new Color(.19f, .19f, .19f);

        private readonly Image pane;
        private readonly Button button;
        //private readonly Image img;
        private readonly Text text;

        public ToolbarButton(Image pane, Button button, Text text)
        {
            this.pane = pane;
            this.text = text;
        }

        // TODO: prob have to diable everything
        public bool enabled { set => pane.enabled = value; }

        public void SetSelected(bool selected)
        {
            pane.color = selected ? onColor : offColor;
        }

        public void SetText(string text) => this.text.text = text;
    }

    public partial class Gui
    {
        public readonly GameController ctrl;
        private readonly AssetSrc assets;
        private readonly Transform canvas;

        public Text selectionText;

        public readonly Line mouseHighlight;
        //Transform selectionHighlight;
        public readonly Line dragOutline;

        public readonly List<ToolbarButton> buttons
            = new List<ToolbarButton>();

        public Gui(GameController ctrl, AssetSrc assets)
        {
            this.ctrl = ctrl;
            this.assets = assets;

            var root = new GameObject("GUI");
            canvas = SetupCanvas(root.transform, new Vec2I(4096, 2160));
            CreateInputSink();

            mouseHighlight = assets.CreateLine(
                root.transform, "Mouse Highlight",
                RenderLayer.Highlight,
                new Color(.2f, .2f, .2f, .5f),
                1 / 32f, true, false);
            mouseHighlight.SetSquare(Vec2.zero, Vec2.one);

            dragOutline = assets.CreateLine(
                root.transform, "Drag Outline",
                RenderLayer.Highlight.Layer(1),
                Color.white, 1, true, true);
            dragOutline.enabled = false;

            Color color = new Color(.19f, .19f, .19f);

            var buttonPane1 = CreatePane(canvas, "Build Button", color,
                new Vec2(180, 180), Anchor.BottomLeft, new Vec2(840, 0));
            var spriteBuild = assets.sprites.Get(ctrl.game.defs.Get<SpriteDef>("BB:BuildIcon"));
            CreateButton(buttonPane1.rectTransform, spriteBuild, () => ctrl.OnBuildMenu());

            var buttonPane2 = CreatePane(canvas, "Order Button", color,
                new Vec2(180, 180), Anchor.BottomLeft, new Vec2(840, 220));
            var spriteOrder = assets.sprites.Get(ctrl.game.defs.Get<SpriteDef>("BB:MineOverlay"));
            CreateButton(buttonPane2.rectTransform, spriteOrder, () => ctrl.OnOrdersMenu());

           /* TextCfg cfg = new TextCfg
            {
                font = assets.fonts.Get("Arial.ttf"),
                fontSize = 40,
                style = FontStyle.Normal,
                anchor = TextAnchor.UpperCenter,
                horiWrap = HorizontalWrapMode.Wrap,
                vertWrap = VerticalWrapMode.Truncate
            };

            buildBtns = new Dictionary<IBuildable, Btn>();
            for (int i = 0; i < buildables.Count; ++i)
            {
                int xofs = 840 + 180 + 40;
                xofs += i * (180 + 40);
                IBuildable buildable = buildables[i];

                var buttonPane = CreatePane(canvas, $"Buildable {i}", color,
                    new Vec2(180, 180), Anchor.BottomLeft, new Vec2(xofs, 0));
                cfg.text = buildable.name;
                CreateButton(buttonPane.rectTransform, cfg, () => ctrl.SetBuildable(buildable));
                buildBtns[buildable] = new Btn(buttonPane);
            }*/

            var imageTest = MakeObject(canvas, "image");
            imageTest.SetSizePivotAnchor(new Vec2(800, 400), Vec2.zero, Vec2.zero);

            var img = imageTest.gameObject.AddComponent<Image>();
            img.color = color;

            selectionText = CreateText(imageTest, Color.red);

            var buttonTest = MakeObject(imageTest, "button");
            var buttonImage = buttonTest.gameObject.AddComponent<Image>();
            var button = buttonTest.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => BB.LogInfo("clicked"));

            /*CreatePane(canvas, "bl", Color.blue, new Vec2(200, 200), Anchor.BottomLeft, new Vec2(40, 40));
            CreatePane(canvas, "bc", Color.blue, new Vec2(200, 200), Anchor.Bottom, new Vec2(40, 40));
            CreatePane(canvas, "br", Color.blue, new Vec2(200, 200), Anchor.BottomRight, new Vec2(40, 40));
            CreatePane(canvas, "cl", Color.blue, new Vec2(200, 200), Anchor.Left, new Vec2(40, 40));
            CreatePane(canvas, "cc", Color.blue, new Vec2(200, 200), Anchor.Center, new Vec2(40, 40));
            CreatePane(canvas, "cr", Color.blue, new Vec2(200, 200), Anchor.Right, new Vec2(40, 40));
            CreatePane(canvas, "tl", Color.blue, new Vec2(200, 200), Anchor.TopLeft, new Vec2(40, 40));
            CreatePane(canvas, "tc", Color.blue, new Vec2(200, 200), Anchor.Top, new Vec2(40, 40));
            CreatePane(canvas, "tr", Color.blue, new Vec2(200, 200), Anchor.TopRight, new Vec2(40, 40));*/
        }

        public void HideBuildButtons()
        {
            foreach (var btn in buttons)
                btn.enabled = false;
        }

        public void ShowBuildButtons(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                // TODO: reset button state
                if (i < buttons.Count)
                    buttons[i].enabled = true;
                else
                    buttons.Add(CreateToolbarButton(i));
            }
        }

        private ToolbarButton CreateToolbarButton(int pos)
        {
            TextCfg cfg = new TextCfg
            {
                font = assets.fonts.Get("Arial.ttf"),
                fontSize = 40,
                style = FontStyle.Normal,
                anchor = TextAnchor.UpperCenter,
                horiWrap = HorizontalWrapMode.Wrap,
                vertWrap = VerticalWrapMode.Truncate
            };

            int xofs = 840 + (180 + 40) * (pos + 1);
            var buttonPane = CreatePane(canvas, $"Toolbar {pos}", ToolbarButton.offColor,
                new Vec2(180, 180), Anchor.BottomLeft, new Vec2(xofs, 0));
            var button = CreateButton(buttonPane.rectTransform, cfg, () => ctrl.OnToolbar(pos));
            return new ToolbarButton(buttonPane, button,
                button.gameObject.GetComponentInChildren<Text>());
        }

        private Text CreateText(Transform parent, Color? backgroundClr)
        {
            var node = MakeObject(parent, "<text>");
            node.SetFillWithMargin(40);

            var nodeText = node;
            if (backgroundClr != null)
            {
                var img = node.gameObject.AddComponent<Image>();
                img.color = (Color)backgroundClr;

                nodeText = MakeObject(node, "<text>");
                nodeText.SetFill();
            }

            var text = nodeText.gameObject.AddComponent<Text>();
            text.font = assets.fonts.Get("Arial.ttf");
            text.fontSize = 60;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.UpperCenter;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = "POOP";
            return text;
        }

        private void CreateInputSink()
        {
            var node = MakeObject(canvas, "Input Sink");
            node.SetFill();

            var sink = node.gameObject.AddComponent<InputSink>();
            sink.Init(ctrl);
        }

        private Transform SetupCanvas(Transform parent, Vec2I refSize)
        {
            var node = CreateCanvas(parent, refSize);
            var obj = node.gameObject;

            obj.AddComponent<GraphicRaycaster>();
            var events = obj.AddComponent<EventSystem>();
            events.sendNavigationEvents = false;
            var module = obj.AddComponent<StandaloneInputModule>();
            module.forceModuleActive = true;

            return node;
        }
    }

    class InputSink : Graphic,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IScrollHandler
    {
        private GameController ctrl;
        private Dictionary<PointerEventData.InputButton, bool> isDragging
            = new Dictionary<PointerEventData.InputButton, bool>();

        public void Init(GameController ctrl)
            => this.ctrl = ctrl;

        public void OnPointerEnter(PointerEventData eventData)
            => ctrl.OnMouseEnter();

        public void OnPointerExit(PointerEventData eventData)
            => ctrl.OnMouseExit();

        public void OnPointerClick(PointerEventData evt)
        {
            if (!isDragging.GetOrDefault(evt.button, false))
                ctrl.OnClick(evt.position, evt.button);
        }

        public void OnBeginDrag(PointerEventData evt)
        {
            isDragging[evt.button] = true;
            ctrl.OnDragStart(
                evt.pressPosition,
                evt.position,
                evt.button);
        }

        public void OnDrag(PointerEventData evt)
            => ctrl.OnDrag(evt.position, evt.button);

        public void OnEndDrag(PointerEventData evt)
        {
            isDragging[evt.button] = false;
            ctrl.OnDragEnd(evt.position, evt.button);
        }

        public override bool Raycast(Vec2 sp, Camera eventCamera) => true;
        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();

        public void OnScroll(PointerEventData eventData)
        {
            ctrl.OnScroll(eventData.scrollDelta);
        }
    }
}
