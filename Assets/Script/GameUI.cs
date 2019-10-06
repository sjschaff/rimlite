using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Events;
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
        private static readonly Color onColor = new Color(.4f, .4f, .4f);
        private static readonly Color offColor = new Color(.19f, .19f, .19f);

        private readonly Image pane;
        private readonly Image img;
        private readonly Text text;

        public ToolbarButton(
            Transform parent, string name,
            Vec2 pos, Vec2 size, Font font,
            UnityAction fn)
        {
            pane = Gui.CreatePane(
                parent, name, offColor,
                size, Anchor.BottomLeft, pos);

            img = Gui.CreateImage(pane.rectTransform, "<image>", null);
            img.rectTransform.SetFill();
            Gui.AddButton(img.gameObject, fn);

            TextCfg cfg = new TextCfg
            {
                font = font,
                fontSizeMin = 10,
                fontSizeMax = 27,
                autoResize = true,
                style = FontStyle.Bold,
                anchor = TextAnchor.LowerCenter,
                horiWrap = HorizontalWrapMode.Wrap,
                vertWrap = VerticalWrapMode.Truncate
            };

            text = Gui.CreateText(img.transform, "<text>", cfg);
            text.rectTransform.SetFillPartial(
                new Vec2(0, .08f), new Vec2(1, .48f));
        }

        public void SetActive(bool active)
            => pane.gameObject.SetActive(active);

        public void SetSelected(bool selected)
        {
            pane.color = selected ? onColor : offColor;
        }

        public void Configure(Sprite sprite, string text = "", bool bigText = false)
        {
            this.text.text = text;
            img.color = sprite == null ? offColor : Color.white;
            img.sprite = sprite;
            if (bigText)
                this.text.resizeTextMaxSize = 40;
            else
                this.text.resizeTextMaxSize = 27;
        }

        public void Reset() => SetSelected(false);
    }

    public class InfoPane
    {
        public readonly Text header;

        public InfoPane(Text header) => this.header = header;
    }

    public class GameUI
    {
        public readonly GameController ctrl;

        public readonly Transform root;
        private readonly Transform canvas;

        public readonly Line dragOutline;

        public InfoPane infoPane;
        public readonly ToolbarButton buildButton;
        public readonly ToolbarButton orderButton;
        public readonly List<ToolbarButton> buttons
            = new List<ToolbarButton>();

        public GameUI(GameController ctrl)
        {
            this.ctrl = ctrl;

            root = new GameObject("GUI").transform;
            canvas = CreateCanvas(root.transform, new Vec2I(4096, 2160));
            CreateInputSink();

            dragOutline = ctrl.assets.CreateLine(
                root.transform, "Drag Outline",
                RenderLayer.Highlight.Layer(1),
                Color.white, 1, true, true);
            dragOutline.enabled = false;

            Color color = new Color(.19f, .19f, .19f);

            Sprite spriteBuild = ctrl.assets.sprites.Get(
                    ctrl.registry.defs.Get<SpriteDef>("BB:BuildIcon"));
            buildButton = CreateToolbarButton(
                "Build Button", new Vec2(840, 0),
                () => ctrl.OnBuildMenu());
            buildButton.Configure(spriteBuild, "Build", true);

            Sprite spriteOrders = ctrl.assets.sprites.Get(
                    ctrl.registry.defs.Get<SpriteDef>("BB:MineIcon"));
            orderButton = CreateToolbarButton(
                "Orders Button", new Vec2(840, 220),
                () => ctrl.OnOrdersMenu());
            orderButton.Configure(spriteOrders, "Orders", true);


            var imageTest = Gui.CreateObject(canvas, "image");
            imageTest.SetSizePivotAnchor(new Vec2(800, 400), Vec2.zero, Vec2.zero);

            var img = imageTest.gameObject.AddComponent<Image>();
            img.color = color;

            var selectionText = CreateTextTest(imageTest, Color.red);
            infoPane = new InfoPane(selectionText);

            var buttonTest = Gui.CreateObject(imageTest, "button");
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
                btn.SetActive(false);
        }

        public void ShowBuildButtons(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                // TODO: reset button state
                if (i < buttons.Count)
                {
                    ToolbarButton button = buttons[i];
                    button.SetActive(true);
                    button.Reset();
                }
                else
                    buttons.Add(CreateToolbarButton(i));
            }
        }

        private ToolbarButton CreateToolbarButton(int pos)
            => CreateToolbarButton(
                $"Toolbar {pos}",
                new Vec2(840 + (180 + 40) * (pos + 1), 0),
                () => ctrl.OnToolbar(pos));

        private ToolbarButton CreateToolbarButton(string name, Vec2 pos, UnityAction fn)
        {
            return new ToolbarButton(
                canvas, name, pos, new Vec2(180, 180),
                ctrl.assets.fonts.Get("Arial.ttf"), fn);
        }

        private Text CreateTextTest(Transform parent, Color? backgroundClr)
        {
            var node = Gui.CreateObject(parent, "<text>");
            node.SetFillWithMargin(40);

            var nodeText = node;
            if (backgroundClr != null)
            {
                var img = node.gameObject.AddComponent<Image>();
                img.color = (Color)backgroundClr;

                nodeText = Gui.CreateObject(node, "<text>");
                nodeText.SetFill();
            }

            var text = nodeText.gameObject.AddComponent<Text>();
            text.font = ctrl.assets.fonts.Get("Arial.ttf");
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
            var node = Gui.CreateObject(canvas, "Input Sink");
            node.SetFill();

            var sink = node.gameObject.AddComponent<InputSink>();
            sink.Init(ctrl);
        }

        private Transform CreateCanvas(Transform parent, Vec2I refSize)
        {
            var node = Gui.CreateCanvas(parent, refSize);
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
        private readonly Dictionary<PointerEventData.InputButton, bool> isDragging
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
