using UnityEngine.UI;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using UnityEngine.Events;

namespace BB
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
        public int fontSizeMin;
        public int fontSizeMax;
        public bool autoResize;
        public FontStyle style;
        public TextAnchor anchor;
        public HorizontalWrapMode horiWrap;
        public VerticalWrapMode vertWrap;

        public static TextCfg Default(Font font)
        {
            return new TextCfg()
            {
                font = font,
                autoResize = false,
                anchor = TextAnchor.UpperLeft,
                horiWrap = HorizontalWrapMode.Overflow,
                vertWrap = VerticalWrapMode.Truncate,
            };
        }

        public void Apply(Text text)
        {
            text.font = font;
            text.fontSize = fontSize;
            if (autoResize)
            {
                text.resizeTextMinSize = fontSizeMin;
                text.resizeTextMaxSize = fontSizeMax;
            }
            text.resizeTextForBestFit = autoResize;
            text.alignByGeometry = autoResize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.horizontalOverflow = horiWrap;
            text.verticalOverflow = vertWrap;
            text.text = this.text;
            text.raycastTarget = false;
            text.supportRichText = false;
        }
    }

    public static class Gui
    {
        public static Button AddButton(GameObject obj, UnityAction fn = null)
        {
            var button = obj.AddComponent<Button>();
            if (fn != null)
                button.onClick.AddListener(fn);
            var colors = button.colors;
            colors.pressedColor = new Color(.5f, .5f, .5f, 1);
            colors.highlightedColor = new Color(.78f, .78f, .78f, 1);
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
            var node = CreateObject(parent, name);
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
            var node = CreateObject(parent, name);
            var img = node.gameObject.AddComponent<Image>();
            return img;
        }

        public static RectTransform CreateCanvas(Transform parent, Vec2I refSize)
        {
            var node = CreateObject(parent, "<canvas>");
            var obj = node.gameObject;

            var canvas = obj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = refSize;
            scaler.matchWidthOrHeight = .5f;

            return node;
        }

        public static RectTransform CreateObject(string name)
            => (RectTransform)new GameObject(name, typeof(RectTransform)).transform;

        public static RectTransform CreateObject(Transform parent, string name)
        {
            var node = CreateObject(name);
            node.SetParent(parent, false);
            return node;
        }
    }

    public static class GuiExt
    {
        private static bool IsOneOf(this Anchor t,
            Anchor a, Anchor b, Anchor c)
            => t.Equals(a) || t.Equals(b) || t.Equals(c);

        public static bool IsTop(this Anchor anchor)
            => anchor.IsOneOf(Anchor.TopLeft, Anchor.Top, Anchor.TopRight);
        public static bool IsBottom(this Anchor anchor)
            => anchor.IsOneOf(Anchor.BottomLeft, Anchor.Bottom, Anchor.BottomRight);
        public static bool IsLeft(this Anchor anchor)
            => anchor.IsOneOf(Anchor.TopLeft, Anchor.Left, Anchor.BottomLeft);
        public static bool IsRight(this Anchor anchor)
            => anchor.IsOneOf(Anchor.TopRight, Anchor.Right, Anchor.BottomRight);

        public static Vec2 Pivot(this Anchor anchor)
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

        public static void SetFixed(this RectTransform rect, Anchor anchor, Vec2 ofs, Vec2 size)
        {
            Vec2 pivot = anchor.Pivot();
            rect.anchorMin = rect.anchorMax = rect.pivot = pivot;
            rect.offsetMin = new Vec2((1 - pivot.x) * ofs.x, (1 - pivot.y) * ofs.y);
            rect.offsetMax = new Vec2(pivot.x * -ofs.x, pivot.y * -ofs.y);
            rect.sizeDelta = size;
        }

        public static void SetFill(this RectTransform rect)
            => rect.SetFillWithMargin(0);

        public static void SetFillWithMargin(this RectTransform rect, float margin)
        {
            rect.SetFillPartial(Vec2.zero, Vec2.one);
            rect.offsetMin = new Vec2(margin, margin);
            rect.offsetMax = new Vec2(-margin, -margin);
        }

        public static void SetFillPartial(this RectTransform rect, Vec2 anchorMin, Vec2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = Vec2.zero;
            rect.offsetMin = rect.offsetMax = Vec2.zero;
        }

        public static void SetSizePivotAnchor(this RectTransform rect, Vec2 size, Vec2 pivot, Vec2 anchor)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
        }
    }
}
