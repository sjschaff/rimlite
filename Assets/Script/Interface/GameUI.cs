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
    public interface IActivatable
    {
        void SetActive(bool active);
    }

    public class UIPool<T>
        where T : IActivatable
    {
        private readonly List<T> ts = new List<T>();
        private readonly Func<int, T> createFn;

        public UIPool(Func<int, T> createFn)
            => this.createFn = createFn;

        public T Get(int i) => ts[i];

        public void Hide()
        {
            foreach (var t in ts)
                t.SetActive(false);
        }

        public void Show(int c)
        {
            for (int i = 0; i < c; ++i)
            {
                if (i < ts.Count)
                    ts[i].SetActive(true);
                else
                    ts.Add(createFn(i));
            }
        }
    }


    // TODO: this whole thing is a nightmare
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

    public class InfoPane
    {
        public readonly Image pane;
        public readonly Text header;
        public readonly Text info;

        public InfoPane(Transform parent, Font font)
        {
            Vec2 size = new Vec2(400, 200);
            pane = Gui.CreatePane(
                parent, "Info Panel", ToolbarButton.offColor,
                size, Anchor.BottomLeft, Vec2.zero);

            TextCfg cfg = new TextCfg()
            {
                font = font,
                fontSize = 24,
                autoResize = false,
                style = FontStyle.Bold,
                anchor = TextAnchor.UpperLeft,
                horiWrap = HorizontalWrapMode.Overflow,
                vertWrap = VerticalWrapMode.Truncate
            };

            header = Gui.CreateText(pane.rectTransform, "<header>", cfg);
            header.rectTransform.SetSizePivotAnchor(
                new Vec2(360, 27), new Vec2(0, 1), new Vec2(0, 1));
            header.rectTransform.offsetMin = new Vec2(20, 0);
            header.rectTransform.offsetMax = new Vec2(0, -14);
            header.rectTransform.sizeDelta = new Vec2(360, 27);

            cfg.fontSize = 18;
            cfg.style = FontStyle.Normal;
            cfg.horiWrap = HorizontalWrapMode.Wrap;
            info = Gui.CreateText(pane.rectTransform, "<info>", cfg);
            info.rectTransform.SetSizePivotAnchor(
                new Vec2(360, 144), new Vec2(0, 1), new Vec2(0, 1));
            info.rectTransform.offsetMin = new Vec2(20, 0);
            info.rectTransform.offsetMax = new Vec2(0, -46);
            info.rectTransform.sizeDelta = new Vec2(360, 144);
        }
    }

    public class ContextMenu
    {
        private readonly RectTransform canvas;
        private readonly RectTransform pane;
        private readonly VerticalLayoutGroup vertLayout;
        private readonly Font font;
        private readonly UIPool<Btn> buttons;
        private int numShown;
        private float longestText = 0;
        const float maxWidth = 300;

        private class Btn : IActivatable
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

            public void SetActive(bool a) => obj.SetActive(a);
        }

        public ContextMenu(RectTransform canvas, Font font)
        {
            this.canvas = canvas;
            this.font = font;

            buttons = new UIPool<Btn>(i => CreateButton());

            pane = Gui.CreateColor(canvas, "Context Menu", ToolbarButton.onColor).rectTransform;
            pane.anchorMin = pane.anchorMax = new Vec2(.5f, .5f);
            pane.pivot = new Vec2(0, 1);
            pane.offsetMin = Vec2.zero;
            pane.offsetMax = Vec2.zero;
            pane.sizeDelta = new Vec2(300, 0);

            var sizeFitter = pane.gameObject.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            vertLayout = pane.gameObject.AddComponent<VerticalLayoutGroup>();
            vertLayout.childAlignment = TextAnchor.UpperLeft;
            vertLayout.childControlWidth = true;
            vertLayout.childControlHeight = true;
            vertLayout.childForceExpandWidth = true;
            vertLayout.childForceExpandHeight = true;
            vertLayout.childScaleHeight = true;
            vertLayout.childScaleWidth = true;
            vertLayout.spacing = 2;
            vertLayout.padding = new RectOffset(2, 2, 2, 2);

            Hide();
        }

        private Btn CreateButton()
        {
            var img = Gui.CreateColor(pane, "<button>", ToolbarButton.offColor);
            img.rectTransform.pivot = new Vec2(0, 1);
            var btn = Gui.AddButton(img.gameObject);
            var layout = img.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childScaleHeight = true;
            layout.childScaleWidth = true;
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
            longestText = 0;
            pane.sizeDelta = new Vec2(maxWidth, 0);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas, scPos, null, out var guiPos);

            Vec2 guiNorm = new Vec2(.5f, .5f) + guiPos / canvas.sizeDelta;
            pane.anchorMin = pane.anchorMax = guiNorm;
            Vec2 pivot;
            pivot.x = guiNorm.x < .5f ? 0 : 1;
            pivot.y = guiNorm.y < .5f ? 0 : 1;
            pane.pivot = pivot;

            if (pivot.x == 0)
                vertLayout.childAlignment = TextAnchor.UpperLeft;
            else
                vertLayout.childAlignment = TextAnchor.UpperRight;
            if (pivot.y == 0)
                numShown = numButtons;
            else
                numShown = -numButtons;

            pane.gameObject.SetActive(true);
            buttons.Show(numButtons);
        }

        public void Hide()
        {
            pane.gameObject.SetActive(false);
            buttons.Hide();
        }

        public void ConfigureButton(int i, string text, UnityAction fn, bool enabled)
        {
            bool lastButton = i == Math.Abs(numShown) - 1;
            if (numShown > 0)
                i = (numShown - 1) - i;
            var btn = buttons.Get(i);
            btn.text.text = text;
            if (btn.text.preferredWidth > longestText)
                longestText = btn.text.preferredWidth;

            btn.button.onClick.RemoveAllListeners();
            btn.button.onClick.AddListener(fn);
            btn.button.interactable = enabled;

            if (lastButton)
                pane.sizeDelta = new Vec2(Math.Min(maxWidth - 20, longestText) + 20, 0);
        }
    }

    public class WorkbenchPane
    {
        private readonly Font font;
        private readonly Image pane;
        private readonly UIPool<Btn> btnsA;
        private readonly UIPool<Btn> btnsB;

        class Btn : IActivatable
        {
            public Button button;
            public Text textMain;
            public Text textSub;

            public void SetActive(bool a) => button.gameObject.SetActive(a);
        }

        public WorkbenchPane(RectTransform canvas, Font font)
        {
            this.font = font;

            Vec2 size = new Vec2(510, 600);
            pane = Gui.CreatePane(
                canvas, "Workbench Panel", ToolbarButton.offColor,
                size, Anchor.BottomLeft, new Vec2(0, 202));

            float colWidth = (size.x - 60) / 2;
            float xColB = colWidth + 40;

            TextCfg cfg = new TextCfg()
            {
                font = font,
                fontSize = 24,
                autoResize = false,
                style = FontStyle.Bold,
                anchor = TextAnchor.UpperLeft,
                horiWrap = HorizontalWrapMode.Overflow,
                vertWrap = VerticalWrapMode.Truncate
            };

            var headerA = Gui.CreateText(pane.rectTransform, "<header>", cfg);
            headerA.text = "Recipes:";
            headerA.rectTransform.anchorMin = headerA.rectTransform.anchorMax = new Vec2(0, 1);
            headerA.rectTransform.pivot = new Vec2(0, 1);
            headerA.rectTransform.offsetMin = new Vec2(20, 0);
            headerA.rectTransform.offsetMax = new Vec2(0, -14);
            headerA.rectTransform.sizeDelta = new Vec2(colWidth, 27);

            var headerB = Gui.CreateText(pane.rectTransform, "<header>", cfg);
            headerB.text = "Orders:";
            headerB.rectTransform.anchorMin = headerB.rectTransform.anchorMax = new Vec2(0, 1);
            headerB.rectTransform.pivot = new Vec2(0, 1);
            headerB.rectTransform.offsetMin = new Vec2(xColB, 0);
            headerB.rectTransform.offsetMax = new Vec2(0, -14);
            headerB.rectTransform.sizeDelta = new Vec2(colWidth, 27);

            Color colColor = ToolbarButton.offColor.Scale(.8f);
            float colOfs = 29 + headerA.rectTransform.sizeDelta.y;
            float colHeight = size.y - (20 + colOfs);
            var colA = Gui.CreateColor(pane.rectTransform, "<col>", colColor);
            colA.rectTransform.SetFixed(Anchor.TopLeft, new Vec2(20, colOfs), new Vec2(colWidth, colHeight));

            var colB = Gui.CreateColor(pane.rectTransform, "<col>", colColor);
            colB.rectTransform.SetFixed(Anchor.TopLeft, new Vec2(xColB, colOfs), new Vec2(colWidth, colHeight));

            btnsA = new UIPool<Btn>(i => MakeButton(colA.rectTransform, i));
            btnsB = new UIPool<Btn>(i => MakeButton(colB.rectTransform, i));

            SetActive(false);
        }

        private Btn MakeButton(RectTransform parent, int order)
        {
            Image pane = Gui.CreateColor(parent, "<button>", ToolbarButton.offColor);
            var xfPane = pane.rectTransform;
            xfPane.SetFixed(Anchor.TopLeft, new Vec2(8, 8 + 58*order), new Vec2(parent.sizeDelta.x - 16, 50));
            var btn = Gui.AddButton(pane.gameObject);

            TextCfg cfg = TextCfg.Default(font);
            cfg.fontSize = 20;
            cfg.style = FontStyle.Bold;
            cfg.anchor = TextAnchor.UpperCenter;

            Text topText = Gui.CreateText(pane.rectTransform, "<name>", cfg);
            topText.rectTransform.SetFixed(Anchor.TopLeft, new Vec2(6, 4), new Vec2(xfPane.sizeDelta.x, 30));

            cfg.fontSize = 16;
            cfg.style = FontStyle.Normal;
            Text botText = Gui.CreateText(pane.rectTransform, "<materials>", cfg);
            botText.rectTransform.SetFixed(Anchor.TopLeft, new Vec2(6, 26), new Vec2(xfPane.sizeDelta.x, 30));

            return new Btn()
            {
                button = btn,
                textMain = topText,
                textSub = botText
            };
        }

        public void SetActive(bool a)
            => pane.gameObject.SetActive(a);

        public void SetRecipes(RecipeDef[] recipes, Action<RecipeDef> cbFn)
        {
            btnsA.Hide();
            btnsA.Show(recipes.Length);
            for (int i = 0; i < recipes.Length; ++i)
            {
                var btn = btnsA.Get(i);
                var rec = recipes[i];
                btn.textMain.text = rec.description;
                var mat = rec.materials[0]; // TODO: multi
                var prod = rec.product[0];
                btn.textSub.text =
                    $"{mat.def.name} x{mat.amt} -> {prod.def.name} x{prod.amt}";
                btn.button.onClick.RemoveAllListeners();
                btn.button.onClick.AddListener(() => cbFn(rec));
            }
        }

        public void ShowOrders(int num)
        {
            btnsB.Hide();
            btnsB.Show(num);
        }

        public void ConfigureButton(int i, string main, string sub, UnityAction cbFn)
        {
            var btn = btnsB.Get(i);
            btn.textMain.text = main;
            btn.textSub.text = sub;
            btn.button.onClick.RemoveAllListeners();
            btn.button.onClick.AddListener(cbFn);
        }
    }

    public class GameUI
    {
        public readonly GameController ctrl;

        public readonly Transform root;
        private readonly RectTransform canvas;

        public readonly Line dragOutline;
        public readonly ContextMenu ctxtMenu;
        public readonly InfoPane infoPane;
        public readonly WorkbenchPane workbench;

        public readonly RectTransform toolbarContainer;
        public readonly ToolbarButton buildButton;
        public readonly ToolbarButton orderButton;
        public readonly List<ToolbarButton> buttons
            = new List<ToolbarButton>();

        public readonly RectTransform playButtonContainer;
        private readonly ToolbarButton pauseButton;
        private readonly ToolbarButton playButton;
        private readonly ToolbarButton playFFButton;
        private readonly ToolbarButton playSFFButton;


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

            toolbarContainer = Gui.CreateObject(canvas, "Toolbar");
            toolbarContainer.SetFill();

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
            infoPane = new InfoPane(canvas, font);

            playButtonContainer = Gui.CreateObject(canvas, "Speed Controls");
            playButtonContainer.SetFill();

            pauseButton = new ToolbarButton(
                playButtonContainer, "Pause", Anchor.TopLeft,
                new Vec2(10, 10), new Vec2(38, 38), 4, font);
            pauseButton.Configure(
                () => ctrl.OnSpeedChange(PlaySpeed.Paused),
                GetSprite("BB:PauseIcon"));

            playButton = new ToolbarButton(
                playButtonContainer, "Play", Anchor.TopLeft,
                new Vec2(52, 10), new Vec2(38, 38), 6, font);
            playButton.Configure(
                () => ctrl.OnSpeedChange(PlaySpeed.Normal),
                GetSprite("BB:PlayIcon"));

            playFFButton = new ToolbarButton(
                playButtonContainer, "Fast", Anchor.TopLeft,
                new Vec2(94, 10), new Vec2(51, 38), 6, font);
            playFFButton.Configure(
                () => ctrl.OnSpeedChange(PlaySpeed.Fast),
                GetSprite("BB:PlayFFIcon"));

            playSFFButton = new ToolbarButton(
                playButtonContainer, "Super Fast", Anchor.TopLeft,
                new Vec2(149, 10), new Vec2(77, 38), 6, font);
            playSFFButton.Configure(
                () => ctrl.OnSpeedChange(PlaySpeed.SuperFast),
                GetSprite("BB:PlaySFFIcon"));

            ctxtMenu = new ContextMenu(canvas, font);
            workbench = new WorkbenchPane(canvas, font);
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
                toolbarContainer, name, Anchor.BottomLeft,
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
