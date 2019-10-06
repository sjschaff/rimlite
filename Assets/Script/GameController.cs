using System.Collections.Generic;
using UnityEngine;

using static UnityEngine.EventSystems.PointerEventData;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using Vec3 = UnityEngine.Vector3;

namespace BB
{
    public class GameController
    {
        public bool lockToMap = false;
        private const float panSpeed = 2.5f;
        private const float zoomZpeed = .5f;
        private const float minZoom = 2;
        private const float maxZoom = 20;

        public readonly Registry registry;
        public readonly AssetSrc assets;

        private readonly Camera cam;
        public readonly Game game;
        public readonly GameUI gui;

        private readonly ToolBuildSelect builds;
        private readonly ToolOrdersSelect orders;
        private readonly ToolSelection selection;
        private readonly Stack<UITool> activeTools = new Stack<UITool>();
        private UITool activeTool => activeTools.Count > 0 ? activeTools.Peek() : null;

        private bool mouseOver;
        private Vec2 dragStart;
        private Vec2 dragStartCam;
        private Vec3 camPosInitial;

        public GameController()
        {
            registry = new Registry();
            assets = new AssetSrc();
            cam = Camera.main;
            game = new Game(registry, assets);
            gui = new GameUI(this);

            builds = new ToolBuildSelect(this);
            orders = new ToolOrdersSelect(this);
            selection = new ToolSelection(this);
        }

        public void PushTool(UITool tool)
        {
            // TODO: handle case where we are currently dragging
            activeTool?.OnSuspend();
            activeTools.Push(tool);
            tool.OnActivate();
        }

        public void ReplaceTool(UITool tool)
        {
            PopAll();
            PushTool(tool);
        }

        public void PopTool()
        {
            // TODO: handle case where we are currently dragging
            BB.Assert(activeTools.Count > 0);
            activeTool.OnDeactivate();
            activeTools.Pop();
            activeTool?.OnUnsuspend();
        }

        public void PopAll()
        {
            while (activeTools.Count > 0)
                PopTool();
        }

        public void Update(float dt)
        {
            Vec2 mouse = MousePos();

            // Tool
            if (mouseOver)
                activeTool?.OnUpdate(mouse.Floor());

            if (Input.GetKeyDown(KeyCode.Tab))
                activeTool?.K_OnTab();
            if (Input.GetKeyDown(KeyCode.Escape))
                if (activeTools.Count > 0)
                    PopTool();

            // Panning
            Vec2 panDir = new Vec2(0, 0);
            if (Input.GetKey("w")) panDir += new Vec2(0, 1);
            if (Input.GetKey("s")) panDir += new Vec2(0, -1);
            if (Input.GetKey("a")) panDir += new Vec2(-1, 0);
            if (Input.GetKey("d")) panDir += new Vec2(1, 0);
            if (panDir != Vec2.zero)
                cam.transform.localPosition += (panDir * panSpeed * cam.orthographicSize * Time.deltaTime).Vec3();

            // Game
            game.Update(dt);
        }

        public void OnBuildMenu() => ReplaceTool(builds);
        public void OnOrdersMenu() => ReplaceTool(orders);

        public void OnToolbar(int button)
        {
            activeTool?.OnButton(button);
        }

        private void UpdateDragOutline(Vec2 end)
        {
            float units = cam.orthographicSize * 2;
            float pixels = Screen.height;

            float unitsPerPixel = units / pixels;

            gui.dragOutline.SetRect(dragStart, end);
            gui.dragOutline.width = 2 * unitsPerPixel;
        }

        private Vec2 ScreenToWorld(Vec2 pt)
            => cam.ScreenToWorldPoint(pt).xy();

        public Vec2 MousePos()
            => ScreenToWorld(Input.mousePosition);

        public void OnClick(Vec2 scPos, InputButton button)
        {
            Vec2I pos = ScreenToWorld(scPos).Floor();
            if (button == InputButton.Left)
            {
                if (game.ValidTile(pos))
                {
                    if (activeTool != null && activeTool.IsClickable())
                    {
                        activeTool.OnClick(pos);
                    }
                    else
                    {
                        // TODO: items/minions
                        var tile = game.Tile(pos);
                        if (tile.hasBuilding)
                        {
                            selection.SetSelection(tile.building);
                        }
                    }
                }
            }
            else if (button == InputButton.Right)
            {
                // TODO: context menu?
            }
        }

        private IEnumerable<ISelectable> SelectDrag(Vec2 dragEnd)
        {
            var area = DragRect(dragEnd);
            foreach (var pos in area.allPositionsWithin)
            {
                var tile = game.Tile(pos);
                if (tile.hasBuilding)
                    yield return tile.building;
            }
        }

        private RectInt DragRect(Vec2 pos)
        {
            RectInt rect = MathExt.RectInclusive(dragStart, pos);
            return rect.Clamp(new RectInt(Vec2I.zero, game.size));
        }

        public void OnDragStart(Vec2 scStart, Vec2 scPos, InputButton button)
        {
            Vec2 start = ScreenToWorld(scStart);
            Vec2 pos = ScreenToWorld(scPos);
            if (button == InputButton.Left)
            {
                dragStart = start;
                if (activeTool != null && activeTool.IsDragable())
                {
                    activeTool.OnDragStart(DragRect(pos));
                }
                else
                {
                    // TODO: selection preview?
                }

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
                if (activeTool != null && activeTool.IsDragable())
                {
                    activeTool.OnDrag(DragRect(pos));
                }
                else
                {
                    // TODO: selection preview?
                }

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
                if (activeTool != null && activeTool.IsDragable())
                    activeTool.OnDragEnd(DragRect(pos));
                else
                    selection.SetSelection(SelectDrag(pos));

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
            mouseOver = true;
            activeTool?.OnMouseEnter();
        }

        public void OnMouseExit()
        {
            mouseOver = false;
            activeTool?.OnMouseExit();
        }
    }
}
