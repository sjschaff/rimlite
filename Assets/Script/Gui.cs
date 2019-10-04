using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    class Gui
    {
        GameController ctrl;
        AssetSrc assets;
        Transform canvas;

        public readonly Line mouseHighlight;
        //Transform selectionHighlight;
        public readonly Line dragOutline;

        public Gui(GameController ctrl, AssetSrc assets, Camera cam)
        {
            this.ctrl = ctrl;
            this.assets = assets;

            var root = new GameObject("GUI");
            canvas = CreateCanvas(root.transform, new Vec2I(4096, 2160));

            CreateInputSink(cam);

            mouseHighlight = assets.CreateLine(
                root.transform, Vec2.zero, "Mouse Highlight",
                RenderLayer.Highlight, new Color(.2f, .2f, .2f, .5f),
                1 / 32f, true, false, new Vec2[] {
                    new Vec2(0, 0),
                    new Vec2(1, 0),
                    new Vec2(1, 1),
                    new Vec2(0, 1)
                });

            dragOutline = assets.CreateLine(
                root.transform, Vec2.zero, "DragOutline",
                RenderLayer.Highlight.Layer(1), Color.white,
                1, true, true, null);
            dragOutline.enabled = false;

            var imageTest = Canvas.MakeObject(canvas, "image");
            imageTest.SetSizePivotAnchor(new Vec2(800, 400), Vec2.zero, Vec2.zero);

            var img = imageTest.gameObject.AddComponent<Image>();
            img.color = new Color(.19f, .19f, .19f, 1);

            CreateText(imageTest, null);// Color.red);

            var buttonTest = Canvas.MakeObject(imageTest, "button");
            var buttonImage = buttonTest.gameObject.AddComponent<Image>();
            var button = buttonTest.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => BB.LogInfo("clicked"));
        }

        private void CreateText(Transform parent, Color? backgroundClr)
        {
            var node = Canvas.MakeObject(parent, "<text>");
            node.SetFillWithMargin(60);

            var nodeText = node;
            if (backgroundClr != null)
            {
                var img = node.gameObject.AddComponent<Image>();
                img.color = (Color)backgroundClr;

                nodeText = Canvas.MakeObject(node, "<text>");
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

        }

        private void CreateInputSink(Camera cam)
        {
            var node = Canvas.MakeObject(canvas, "Input Sink");
            node.SetFill();

            var sink = node.gameObject.AddComponent<InputSink>();
            sink.Init(ctrl, cam);
        }

        private Transform CreateCanvas(Transform parent, Vec2I refSize)
        {
            var node = Canvas.CreateCanvas(parent, refSize);
            var obj = node.gameObject;

            obj.AddComponent<GraphicRaycaster>();
            obj.AddComponent<EventSystem>();
            obj.AddComponent<StandaloneInputModule>();

            return node;
        }
    }


    class InputSink : Graphic,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private GameController ctrl;
        private Camera cam;
        private Dictionary<PointerEventData.InputButton, bool> isDragging
            = new Dictionary<PointerEventData.InputButton, bool>();

        public void Init(GameController ctrl, Camera cam)
        {
            this.ctrl = ctrl;
            this.cam = cam;
        }


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
    }
}
