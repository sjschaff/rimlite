using System.Collections.Generic;
using UnityEngine;

using static UnityEngine.EventSystems.PointerEventData;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using Vec3 = UnityEngine.Vector3;

namespace BB
{
    class GameController
    {
        public bool lockToMap = false;
        private const float panSpeed = 2.5f;
        private const float zoomZpeed = .5f;
        private const float minZoom = 2;
        private const float maxZoom = 20;

        private readonly Camera cam;
        private readonly Game game;
        private readonly Gui gui;

        private readonly LinkedList<UITool> tools;
        private LinkedListNode<UITool> currentTool;
        private UITool tool => currentTool.Value;

        private Vec2 dragStart;
        private Vec2 dragStartCam;
        private Vec3 camPosInitial;

        public GameController()
        {
            cam = Camera.main;
            game = new Game();
            gui = new Gui(this, game.assets, cam);

            tools = UITool.RegisterTools(game);
            currentTool = tools.First;
        }

        public void Update(float dt)
        {
            Vec2 mouse = MousePos();
            gui.mouseHighlight.position = mouse.Floor();

            // Panning
            Vec2 panDir = new Vec2(0, 0);
            if (Input.GetKey("w")) panDir += new Vec2(0, 1);
            if (Input.GetKey("s")) panDir += new Vec2(0, -1);
            if (Input.GetKey("a")) panDir += new Vec2(-1, 0);
            if (Input.GetKey("d")) panDir += new Vec2(1, 0);
            if (panDir != Vec2.zero)
                cam.transform.localPosition += (panDir * panSpeed * cam.orthographicSize * Time.deltaTime).Vec3();

            // Tools
            if (Input.GetKeyDown("tab"))
                tool.OnTab();
            if (Input.GetKeyDown("p"))
                tool.OnKeyP();

            if (Input.GetKeyDown("l"))
            {
                currentTool = currentTool.Next;
                if (currentTool == null)
                    currentTool = tools.First;

                BB.LogInfo("Current Tool: " + tool);
            }

            // Game
            game.Update(dt);
        }

        private void UpdateDragOutline(Vec2 end)
        {
            float units = cam.orthographicSize * 2;
            float pixels = Screen.height;

            float unitsPerPixel = units / pixels;

            gui.dragOutline.SetSquare(dragStart, end);
            gui.dragOutline.width = 2 * unitsPerPixel;
        }

        private Vec2 ScreenToWorld(Vec2 pt)
            => cam.ScreenToWorldPoint(pt).xy();

        public Vec2 MousePos()
            => ScreenToWorld(Input.mousePosition);

        public void OnClick(Vec2 scPos, InputButton button)
        {
            Vec2 pos = ScreenToWorld(scPos);
            if (game.ValidTile(pos.Floor()))
                tool.OnClick(pos.Floor());
        }

        public void OnDragStart(Vec2 scStart, Vec2 scPos, InputButton button)
        {
            Vec2 start = ScreenToWorld(scStart);
            Vec2 pos = ScreenToWorld(scPos);
            if (button == InputButton.Left)
            {
                dragStart = start;
                tool.OnDragStart(start, pos);
                UpdateDragOutline(pos);
                gui.dragOutline.enabled = true;
            }
            else if (button == InputButton.Right)
            {
                dragStartCam = scStart;
                camPosInitial = cam.transform.localPosition;
            }
        }

        public void OnDrag(Vec2 scPos, InputButton button)
        {
            Vec2 pos = ScreenToWorld(scPos);
            if (button == InputButton.Left)
            {
                tool.OnDragUpdate(dragStart, pos);
                UpdateDragOutline(pos);
            }
            else if (button == InputButton.Right)
            {
                Vec2 scDrag = dragStartCam - scPos;
                Vec3 drag = cam.ScreenToViewportPoint(scDrag).Scaled(cam.OrthoSize());
                cam.transform.localPosition = camPosInitial + drag;
            }
        }

        public void OnDragEnd(Vec2 scPos, InputButton button)
        {
            Vec2 pos = ScreenToWorld(scPos);
            if (button == InputButton.Left)
            {
                tool.OnDragEnd(dragStart, pos);
                gui.dragOutline.enabled = false;
            }
        }

        public void OnScroll(Vec2 delta)
        {
            float scroll = delta.y;
            cam.orthographicSize -= zoomZpeed * scroll;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

            if (lockToMap)
            {
                Vec2 halfSize = new Vec2(cam.orthographicSize * cam.aspect, cam.orthographicSize);

                Vec2 pos = cam.transform.localPosition.xy();
                pos = Vec2.Max(pos, halfSize);
                pos = Vec2.Min(pos, game.size - halfSize);
                cam.transform.localPosition = new Vec3(pos.x, pos.y, -11);
            }
        }

        public void OnMouseEnter()
        {
            gui.mouseHighlight.enabled = true;
        }

        public void OnMouseExit()
        {
            gui.mouseHighlight.enabled = false;
        }
    }
}
