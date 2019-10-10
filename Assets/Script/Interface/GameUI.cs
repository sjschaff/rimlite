using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class ToolbarButton
    {
        private static readonly Color onColor = new Color(.4f, .4f, .4f);
        private static readonly Color offColor = new Color(.19f, .19f, .19f);

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

    public class GameUI
    {
        public readonly GameController ctrl;

        public readonly Transform root;
        public
#warning public
            readonly RectTransform canvas;

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

        public readonly Image contextMenu;

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

            Color color = new Color(.19f, .19f, .19f);

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

            contextMenu = Gui.CreateColor(canvas, "Context Menu", color);
            var r = contextMenu.rectTransform;

            r.anchorMin = r.anchorMax = new Vec2(.5f, .5f);
            r.pivot = new Vec2(0, 1);
            r.offsetMin = Vec2.zero;
            r.offsetMax = Vec2.zero;
            r.sizeDelta = new Vec2(150, 300);
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
