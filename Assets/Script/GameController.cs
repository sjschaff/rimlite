using System.Collections.Generic;
using UnityEngine;

using static UnityEngine.EventSystems.PointerEventData;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    class GameController
    {
        private readonly Camera cam;
        private readonly Game game;
        private readonly Gui gui;

        private readonly LinkedList<UITool> tools;
        private LinkedListNode<UITool> currentTool;
        private UITool tool => currentTool.Value;

        private Vec2 dragStart;

        public GameController(Transform transform)
        {
            cam = Camera.main;
            game = new Game(transform);
            gui = new Gui(this, game.assets, cam);

            tools = UITool.RegisterTools(game);
            currentTool = tools.First;
        }

        public void Update(float dt)
        {
            Vec2 mouse = MousePos();
            gui.mouseHighlight.position = mouse.Floor();

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

        public Vec2 MousePos()
            => cam.ScreenToWorldPoint(Input.mousePosition);

        public void OnClick(Vec2 pos, InputButton button)
        {
            if (game.ValidTile(pos.Floor()))
                tool.OnClick(pos.Floor());
        }

        public void OnDragStart(Vec2 start, Vec2 pos, InputButton button)
        {
            if (button == InputButton.Left)
            {
                dragStart = start;
                tool.OnDragStart(start, pos);
                UpdateDragOutline(pos);
                gui.dragOutline.enabled = true;
            }
        }

        public void OnDrag(Vec2 pos, InputButton button)
        {
            if (button == InputButton.Left)
            {
                tool.OnDragUpdate(dragStart, pos);
                UpdateDragOutline(pos);
            }
        }

        public void OnDragEnd(Vec2 pos, InputButton button)
        {
            tool.OnDragEnd(dragStart, pos);
            gui.dragOutline.enabled = false;
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
