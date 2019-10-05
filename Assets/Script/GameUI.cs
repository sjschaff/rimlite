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
        private readonly Image textBg;
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

            textBg = Gui.CreateColor(pane.rectTransform, "<text>", offColor);
            textBg.rectTransform.SetFill();
            Gui.AddButton(textBg.gameObject, fn);

            TextCfg cfg = new TextCfg
            {
                font = font,
                fontSize = 40,
                style = FontStyle.Normal,
                anchor = TextAnchor.UpperCenter,
                horiWrap = HorizontalWrapMode.Wrap,
                vertWrap = VerticalWrapMode.Truncate
            };
            text = Gui.CreateText(textBg.transform, "<text>", cfg);
            text.rectTransform.SetFill();
        }

        public void SetActive(bool active)
            => pane.gameObject.SetActive(active);

        public void SetSelected(bool selected)
        {
            pane.color = selected ? onColor : offColor;
            textBg.color = pane.color;
        }

        public void Configure(string text)
        {
            this.text.text = text;
            textBg.gameObject.SetActive(true);
            img.gameObject.SetActive(false);
        }

        public void Configure(Sprite sprite)
        {
            this.img.sprite = sprite;
            img.gameObject.SetActive(true);
            textBg.gameObject.SetActive(false);
        }

        public void Reset() => SetSelected(false);
    }

    public class GameUI
    {
        public readonly GameController ctrl;

        public readonly Transform root;
        private readonly Transform canvas;

        public Text selectionText;

        //Transform selectionHighlight;
        public readonly Line dragOutline;

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

            buildButton = CreateToolbarButton(
                "Build Button", new Vec2(840, 0),
                () => ctrl.OnBuildMenu());
            buildButton.Configure(
                ctrl.assets.sprites.Get(
                    ctrl.registry.defs.Get<SpriteDef>("BB:BuildIcon")));

            orderButton = CreateToolbarButton(
                "Order Button", new Vec2(840, 220),
                () => ctrl.OnOrdersMenu());
            orderButton.Configure(
                ctrl.assets.sprites.Get(
                    ctrl.registry.defs.Get<SpriteDef>("BB:MineIcon")));


            var imageTest = Gui.CreateObject(canvas, "image");
            imageTest.SetSizePivotAnchor(new Vec2(800, 400), Vec2.zero, Vec2.zero);

            var img = imageTest.gameObject.AddComponent<Image>();
            img.color = color;

            selectionText = CreateTextTest(imageTest, Color.red);

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
