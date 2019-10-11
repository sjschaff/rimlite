using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class ToolbarButton
    {
        public static readonly Color onColor = new Color(.4f, .4f, .4f);
        public static readonly Color offColor = new Color(.19f, .19f, .19f);

        private readonly Image pane;
        private readonly Image img;
        private readonly Text text;

        private Action fn;

        public ToolbarButton(
            Transform parent, string name,
            Anchor anchor, Vec2 pos, Vec2 size,
            float margin, Font font)
        {
            pane = Gui.CreatePane(
                parent, name, offColor,
                size, anchor, pos);

            img = Gui.CreateImage(pane.rectTransform, "<image>", null);
            img.rectTransform.SetFillWithMargin(margin);
            Gui.AddButton(img.gameObject, () => OnClick());

            TextCfg cfg = new TextCfg
            {
                font = font,
                fontSizeMin = 5,
                fontSizeMax = 14,
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

        private void OnClick() => fn?.Invoke();

        public void SetActive(bool active)
            => pane.gameObject.SetActive(active);

        public void SetSelected(bool selected)
        {
            pane.color = selected ? onColor : offColor;
            if (img.sprite == null)
                img.color = pane.color;
        }

        public void Configure(Action fn, Sprite sprite, string text = "", bool bigText = false)
        {
            this.text.text = text;
            img.color = sprite == null ? offColor : Color.white;
            img.sprite = sprite;
            if (bigText)
                this.text.resizeTextMaxSize = 20;
            else
                this.text.resizeTextMaxSize = 14;
            this.fn = fn;
        }

        public void Configure(Action fn, AssetSrc assets, IToolbarButton btn)
            => Configure(fn, assets.sprites.Get(btn.GuiSprite()), btn.GuiText());

        public void Reset() => SetSelected(false);
    }

    // TODO: make better
    public class InfoPane
    {
        public readonly Image pane;
        public readonly Text header;

        public InfoPane(Transform parent, Vec2 size, Font font)
        {
            pane = Gui.CreatePane(
                parent, "Info Panel", new Color(.19f, .19f, .19f),
                size, Anchor.BottomLeft, Vec2.zero);

            TextCfg cfg = new TextCfg()
            {
                font = font,
                fontSize = 30,
                autoResize = false,
                style = FontStyle.Bold,
                anchor = TextAnchor.UpperCenter,
                horiWrap = HorizontalWrapMode.Wrap,
                vertWrap = VerticalWrapMode.Truncate
            };

            header = Gui.CreateText(pane.rectTransform, "<header>", cfg);
            header.rectTransform.SetFillWithMargin(40);
        }
    }

    public class ContextMenu
    {
        private readonly RectTransform canvas;
        private readonly RectTransform pane;
        private readonly RectTransform container;
        private readonly HorizontalLayoutGroup horiLayout;
        private readonly Font font;
        private readonly List<Btn> buttons =
            new List<Btn>();
        private int numShown;

        private struct Btn
        {
            public readonly GameObject obj;
            public readonly Button button;
            public readonly Text text;
            public Btn(GameObject obj, Button button, Text text)
            {
                this.obj = obj;
                this.button = button;
                this.text = text;
            }
        }

        public ContextMenu(RectTransform canvas, Font font)
        {
            this.canvas = canvas;
            this.font = font;

            pane = Gui.CreateObject(canvas, "Context Menu");
            pane.anchorMin = pane.anchorMax = new Vec2(.5f, .5f);
            pane.pivot = new Vec2(0, 1);
            pane.offsetMin = Vec2.zero;
            pane.offsetMax = Vec2.zero;
            pane.sizeDelta = new Vec2(300, 0);

            var sizeFitter = pane.gameObject.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            horiLayout = pane.gameObject.AddComponent<HorizontalLayoutGroup>();
            horiLayout.childAlignment = TextAnchor.UpperLeft;
            horiLayout.childControlWidth = true;
            horiLayout.childControlHeight = true;
            horiLayout.childForceExpandWidth = false;
            horiLayout.childForceExpandHeight = false;

            var bg = Gui.CreateColor(pane, "<background>", ToolbarButton.onColor);
            container = bg.rectTransform;
            container.pivot = new Vec2(0, 1);
            bg.gameObject.AddComponent<LayoutElement>();
            var vertLayout = bg.gameObject.AddComponent<VerticalLayoutGroup>();
            vertLayout.childAlignment = TextAnchor.UpperLeft;
            vertLayout.childControlWidth = true;
            vertLayout.childControlHeight = true;
            vertLayout.childForceExpandWidth = false;
            vertLayout.childForceExpandHeight = false;
            vertLayout.spacing = 2;
            vertLayout.padding = new RectOffset(2, 2, 2, 2);

            Hide();
        }

        private Btn CreateButton()
        {
            var img = Gui.CreateColor(container, "<button>", ToolbarButton.offColor);
            img.rectTransform.pivot = new Vec2(0, 1);
            var btn = Gui.AddButton(img.gameObject, () => { });
            var layout = img.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(8, 8, 8, 8);

            TextCfg cfg = new TextCfg
            {
                font = font,
                fontSize = 20,
                autoResize = false,
                style = FontStyle.Normal,// Bold,
                anchor = TextAnchor.UpperLeft,
                horiWrap = HorizontalWrapMode.Wrap,
                vertWrap = VerticalWrapMode.Overflow
            };

            var text = Gui.CreateText(img.transform, "<text>", cfg);
            text.rectTransform.pivot = new Vec2(0, 1);
            text.text = "Foobar manchu monskder sdf";

            return new Btn(img.gameObject, btn, text);
        }

        public void Show(Vec2 scPos, int numButtons)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas, scPos, null, out var guiPos);

            Vec2 guiNorm = new Vec2(.5f, .5f) + guiPos / canvas.sizeDelta;
            pane.anchorMin = pane.anchorMax = guiNorm;
            Vec2 pivot;
            pivot.x = guiNorm.x < .5f ? 0 : 1;
            pivot.y = guiNorm.y < .5f ? 0 : 1;
            pane.pivot = pivot;

            if (pivot.x == 0)
                horiLayout.childAlignment = TextAnchor.UpperLeft;
            else
                horiLayout.childAlignment = TextAnchor.UpperRight;
            if (pivot.y == 0)
                numShown = numButtons;
            else
                numShown = -1;

            pane.gameObject.SetActive(true);

            for (int i = 0; i < numButtons; ++i)
            {
                if (i < buttons.Count)
                    buttons[i].obj.SetActive(true);
                else
                    buttons.Add(CreateButton());
            }
        }

        public void Hide()
        {
            pane.gameObject.SetActive(false);
            foreach (var button in buttons)
                button.obj.SetActive(false);
        }

        public void ConfigureButton(int i, string text, UnityAction fn, bool enabled)
        {
            // TODO: enabling
            if (numShown > 0)
                i = (numShown - 1) - i;
            var btn = buttons[i];
            btn.text.text = text;
            btn.button.onClick.RemoveAllListeners();
            btn.button.onClick.AddListener(fn);
        }
    }

    public class GameUI
    {
        public readonly GameController ctrl;

        public readonly Transform root;
        private readonly RectTransform canvas;

        public readonly Line dragOutline;

        public InfoPane infoPane;
        public readonly ToolbarButton buildButton;
        public readonly ToolbarButton orderButton;
        public readonly List<ToolbarButton> buttons
            = new List<ToolbarButton>();

        private readonly ToolbarButton pauseButton;
        private readonly ToolbarButton playButton;
        private readonly ToolbarButton playFFButton;
        private readonly ToolbarButton playSFFButton;

        public readonly ContextMenu ctxtMenu;

        public GameUI(GameController ctrl)
        {
            this.ctrl = ctrl;

            root = new GameObject("GUI").transform;
            canvas = CreateCanvas(root.transform, new Vec2I(1920, 1080));
            CreateInputSink();

            dragOutline = ctrl.assets.CreateLine(
                root.transform, "Drag Outline",
                RenderLayer.Highlight.Layer(1),
                Color.white, 1, true, true);
            dragOutline.enabled = false;

            Color color = ToolbarButton.offColor;

            buildButton = CreateToolbarButton(
                "Build Button", new Vec2(420, 0));
            buildButton.Configure(
                () => ctrl.OnBuildMenu(),
                GetSprite("BB:BuildIcon"),
                "Build", true);

            orderButton = CreateToolbarButton(
                "Orders Button", new Vec2(420, 110));
            orderButton.Configure(
                () => ctrl.OnOrdersMenu(),
                GetSprite("BB:MineIcon"),
                "Orders", true);

            Font font = ctrl.assets.fonts.Get("Arial.ttf");
            infoPane = new InfoPane(canvas, new Vec2(400, 200), font);

            pauseButton = new ToolbarButton(
                canvas, "Pause", Anchor.TopLeft,
                new Vec2(10, 10), new Vec2(38, 38), 4, font);
            pauseButton.Configure(
                () => ctrl.OnSpeedChange(PlaySpeed.Paused),
                GetSprite("BB:PauseIcon"));

            playButton = new ToolbarButton(
                canvas, "Play", Anchor.TopLeft,
                new Vec2(52, 10), new Vec2(38, 38), 6, font);
            playButton.Configure(
                () => ctrl.OnSpeedChange(PlaySpeed.Normal),
                GetSprite("BB:PlayIcon"));

            playFFButton = new ToolbarButton(
                canvas, "Fast", Anchor.TopLeft,
                new Vec2(94, 10), new Vec2(51, 38), 6, font);
            playFFButton.Configure(
                () => ctrl.OnSpeedChange(PlaySpeed.Fast),
                GetSprite("BB:PlayFFIcon"));

            playSFFButton = new ToolbarButton(
                canvas, "Super Fast", Anchor.TopLeft,
                new Vec2(149, 10), new Vec2(77, 38), 6, font);
            playSFFButton.Configure(
                () => ctrl.OnSpeedChange(PlaySpeed.SuperFast),
                GetSprite("BB:PlaySFFIcon"));

            ctxtMenu = new ContextMenu(canvas, font);
            /*
            CreatePane(canvas, "bl", Color.blue, new Vec2(100, 100), Anchor.BottomLeft, new Vec2(20, 20));
            CreatePane(canvas, "bc", Color.blue, new Vec2(100, 100), Anchor.Bottom, new Vec2(20, 20));
            CreatePane(canvas, "br", Color.blue, new Vec2(100, 100), Anchor.BottomRight, new Vec2(20, 20));
            CreatePane(canvas, "cl", Color.blue, new Vec2(100, 100), Anchor.Left, new Vec2(20, 20));
            CreatePane(canvas, "cc", Color.blue, new Vec2(100, 100), Anchor.Center, new Vec2(20, 20));
            CreatePane(canvas, "cr", Color.blue, new Vec2(100, 100), Anchor.Right, new Vec2(20, 20));
            CreatePane(canvas, "tl", Color.blue, new Vec2(100, 100), Anchor.TopLeft, new Vec2(20, 20));
            CreatePane(canvas, "tc", Color.blue, new Vec2(100, 100), Anchor.Top, new Vec2(20, 20));
            CreatePane(canvas, "tr", Color.blue, new Vec2(100, 100), Anchor.TopRight, new Vec2(20, 20));*/
        }

        public void HideToolbarButtons()
        {
            foreach (var btn in buttons)
                btn.SetActive(false);
        }

        public void ShowToolbarButtons(int count)
        {
            for (int i = 0; i < count; ++i)
            {
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
                new Vec2(420 + (90 + 20) * (pos + 1), 0));

        private ToolbarButton CreateToolbarButton(string name, Vec2 pos)
        {
            return new ToolbarButton(
                canvas, name, Anchor.BottomLeft,
                pos, new Vec2(90, 90), 0,
                ctrl.assets.fonts.Get("Arial.ttf"));
        }

        public ToolbarButton ButtonForSpeed(PlaySpeed speed)
        {
            switch (speed)
            {
                case PlaySpeed.Paused: return pauseButton;
                case PlaySpeed.Normal: return playButton;
                case PlaySpeed.Fast: return playFFButton;
                case PlaySpeed.SuperFast: return playSFFButton;
                default: return null;
            }
        }

        private Sprite GetSprite(string name)
            => ctrl.assets.sprites.Get(ctrl.registry.defs.Get<SpriteDef>(name));

        private void CreateInputSink()
        {
            var node = Gui.CreateObject(canvas, "Input Sink");
            node.SetFill();

            var sink = node.gameObject.AddComponent<InputSink>();
            sink.Init(ctrl);
        }

        private RectTransform CreateCanvas(Transform parent, Vec2I refSize)
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
                ctrl.OnClick(evt.position, evt.button, evt.clickCount);
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
