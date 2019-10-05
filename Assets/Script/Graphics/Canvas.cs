using UnityEngine.UI;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using UnityEngine.Events;

namespace BB
{
    public partial class Gui
    {
        public enum Anchor
        {
            TopLeft, Top, TopRight,
            Left, Center, Right,
            BottomLeft, Bottom, BottomRight
        }

        public struct TextCfg
        {
            public string text;
            public Font font;
            public int fontSize;
            public FontStyle style;
            public TextAnchor anchor;
            public HorizontalWrapMode horiWrap;
            public VerticalWrapMode vertWrap;

            public void Apply(Text text)
            {
                text.font = font;
                text.fontSize = fontSize;
                text.fontStyle = style;
                text.alignment = anchor;
                text.horizontalOverflow = horiWrap;
                text.verticalOverflow = vertWrap;
                text.text = this.text;
                text.raycastTarget = false;
                text.supportRichText = false;
            }
        }

        public static Button CreateButton(
            Transform parent, TextCfg cfg, UnityAction fn)
        {
            var text = CreateText(parent, "<button>", cfg);
            text.rectTransform.SetFill();
            return SetupButton(parent.gameObject, fn);
        }

        public static Button CreateButton(
            Transform parent, Color color, UnityAction fn)
        {
            var image = CreateColor(parent, "<button>", color);
            image.rectTransform.SetFill();
            return SetupButton(image.gameObject, fn);
        }

        public static Button CreateButton(
            Transform parent, Sprite sprite, UnityAction fn)
        {
            var image = CreateImage(parent, "<button>", sprite);
            image.rectTransform.SetFill();
            return SetupButton(image.gameObject, fn);
        }

        private static Button SetupButton(GameObject obj, UnityAction fn)
        {
            var button = obj.AddComponent<Button>();
            button.onClick.AddListener(fn);
            var colors = button.colors;
            colors.pressedColor = new Color(.5f, .5f, .5f, 1);
            colors.highlightedColor = new Color(.78f, .78f, .78f, 1);
            //colors.selectedColor = Color.green;// new Color(.6f, .6f, .6f, 1);
            colors.fadeDuration = .06f;
            button.colors = colors;
            return button;
        }

        public static Image CreatePane(
            Transform parent, string name, Color color, 
            Vec2 size, Anchor anchor, Vec2 offset)
        {
            var image = CreateColor(parent, name, color);
            var node = image.rectTransform;

            Vec2 min = Vec2.zero;
            Vec2 max = Vec2.zero;
            if (anchor.IsLeft()) min.x = offset.x;
            else if (anchor.IsRight()) max.x = -offset.x;
            if (anchor.IsBottom()) min.y = offset.y;
            else if (anchor.IsTop()) max.y = -offset.y;

            Vec2 pivot = anchor.Pivot();

            node.anchorMin = node.anchorMax = pivot;
            node.pivot = pivot;
            node.offsetMin = min;
            node.offsetMax = max;
            node.sizeDelta = size;

            return image;
        }

        public static Text CreateText(
            Transform parent, string name, TextCfg cfg)
        {
            var node = MakeObject(parent, name);
            var text = node.gameObject.AddComponent<Text>();
            cfg.Apply(text);
            return text;
        }

        public static Image CreateImage(
            Transform parent, string name, Sprite sprite)
        {
            var img = CreateImage(parent, name);
            img.sprite = sprite;
            return img;
        }

        public static Image CreateColor(
            Transform parent, string name, Color color)
        {
            var img = CreateImage(parent, name);
            img.color = color;
            return img;
        }

        public static Image CreateImage(
            Transform parent, string name)
        {
            var node = MakeObject(parent, name);
            var img = node.gameObject.AddComponent<Image>();
            return img;
        }

        public static Transform CreateCanvas(Transform parent, Vec2I refSize)
        {
            var node = MakeObject(parent, "<canvas>");
            var obj = node.gameObject;

            var canvas = obj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = refSize;

            return node;
        }

        public static RectTransform MakeObject(string name)
            => (RectTransform)new GameObject(name, typeof(RectTransform)).transform;

        public static RectTransform MakeObject(Transform parent, string name)
        {
            var node = MakeObject(name);
            node.SetParent(parent, false);
            return node;
        }
    }

    public static class GuiExt
    {
        private static bool IsOneOf(this Gui.Anchor t,
            Gui.Anchor a, Gui.Anchor b, Gui.Anchor c)
            => t.Equals(a) || t.Equals(b) || t.Equals(c);

        public static bool IsTop(this Gui.Anchor anchor)
            => anchor.IsOneOf(Gui.Anchor.TopLeft, Gui.Anchor.Top, Gui.Anchor.TopRight);
        public static bool IsBottom(this Gui.Anchor anchor)
            => anchor.IsOneOf(Gui.Anchor.BottomLeft, Gui.Anchor.Bottom, Gui.Anchor.BottomRight);
        public static bool IsLeft(this Gui.Anchor anchor)
            => anchor.IsOneOf(Gui.Anchor.TopLeft, Gui.Anchor.Left, Gui.Anchor.BottomLeft);
        public static bool IsRight(this Gui.Anchor anchor)
            => anchor.IsOneOf(Gui.Anchor.TopRight, Gui.Anchor.Right, Gui.Anchor.BottomRight);

        public static Vec2 Pivot(this Gui.Anchor anchor)
            =>  new Vec2(
                    anchor.IsLeft() ? 0 : (anchor.IsRight() ? 1 : .5f),
                    anchor.IsBottom() ? 0 : (anchor.IsTop() ? 1 : .5f));

        // Note: Setting in this order seems to work
        // anchors
        // pivot
        // offset
        // size

        // Known not working:
        // anchors -> pivot -> size -> offset

        public static void SetFill(this RectTransform rect)
            => rect.SetFillWithMargin(0);

        public static void SetFillWithMargin(this RectTransform rect, float margin)
        {
            rect.anchorMin = Vec2.zero;
            rect.anchorMax = new Vec2(1, 1);
            rect.pivot = Vec2.zero;
            rect.offsetMin = new Vec2(margin, margin);
            rect.offsetMax = new Vec2(-margin, -margin);
        }

        public static void SetSizePivotAnchor(this RectTransform rect, Vec2 size, Vec2 pivot, Vec2 anchor)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
        }
    }
}
