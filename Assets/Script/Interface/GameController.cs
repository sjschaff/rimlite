using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.PointerEventData;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using Vec3 = UnityEngine.Vector3;

namespace BB
{
    public enum PlaySpeed
    {
        Paused, Normal, Fast, SuperFast
    }

    public class GameController
    {
        public bool lockToMap = false;
        private const float panSpeed = 2.5f;
        private const float zoomZpeed = .5f;
        private const float minZoom = 1;//2;
        private const float maxZoom = 20;

        private static float Speed(PlaySpeed speed)
        {
            switch (speed)
            {
                case PlaySpeed.SuperFast: return 3;
                case PlaySpeed.Fast: return 1.5f;
                default: return 1;
            }
        }

        private static PlaySpeed NextPlaySpeed(PlaySpeed speed)
        {
            switch (speed)
            {
                case PlaySpeed.Paused: return PlaySpeed.Normal;
                case PlaySpeed.Normal: return PlaySpeed.Fast;
                case PlaySpeed.Fast: return PlaySpeed.SuperFast;
                case PlaySpeed.SuperFast:
                default:
                    return PlaySpeed.Paused;
            }
        }

        // Game State
        public readonly Registry registry;
        public readonly AssetSrc assets;

        private readonly Camera cam;
        public readonly Game game;
        public readonly GameUI gui;

        private PlaySpeed speed;

        // Tool State
        private readonly ToolBuildSelect builds;
        private readonly ToolOrdersSelect orders;
        private readonly ToolSelection selection;
        private readonly Stack<UITool> activeTools = new Stack<UITool>();
        private UITool baseTool;
        private UITool activeTool => activeTools.Count > 0 ? activeTools.Peek() : null;

        // Interaction State
        private bool mouseOver;
        private Vec2 dragStart;
        private Vec2 dragStartCam;
        private Vec3 camPosInitial;
        private Selection lastSingleClickSelection;

        public static GameController ctrl; // TODO: less shitty

        public GameController()
        {
            ctrl = this;
            registry = new Registry();
            assets = AssetSrc.singleton;
            cam = Camera.main;
            game = new Game(registry, assets);
            gui = new GameUI(this);

            builds = new ToolBuildSelect(this);
            orders = new ToolOrdersSelect(this);
            selection = new ToolSelection(this);

            speed = PlaySpeed.Normal;
            OnSpeedChange(speed);
        }

        public void ReplaceTool(UITool tool)
        {
            PopAll();
            PushTool(tool);
        }

        public void PushTool(UITool tool)
        {
            // TODO: handle case where we are currently dragging
            if (activeTools.Count == 0)
                baseTool = tool;
            else
                activeTool.OnSuspend();

            activeTools.Push(tool);
            tool.OnActivate();
        }

        public void PopTool()
        {
            // TODO: handle case where we are currently dragging
            BB.Assert(activeTools.Count > 0);
            activeTool.OnDeactivate();
            activeTools.Pop();

            if (activeTools.Count == 0)
                baseTool = null;
            else
                activeTool.OnUnsuspend();
        }

        public void PopAll()
        {
            while (activeTools.Count > 0)
                PopTool();
        }

        public void Update(float dt)
        {
            Vec2 mouse = MousePos();

            // Speed
            if (Input.GetKeyDown(KeyCode.Tab))
                OnSpeedChange(NextPlaySpeed(speed));
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (speed == PlaySpeed.Paused)
                    OnSpeedChange(PlaySpeed.Normal);
                else
                    OnSpeedChange(PlaySpeed.Paused);
            }

            // Panning
            Vec2 panDir = new Vec2(0, 0);
            if (Input.GetKey("w")) panDir += new Vec2(0, 1);
            if (Input.GetKey("s")) panDir += new Vec2(0, -1);
            if (Input.GetKey("a")) panDir += new Vec2(-1, 0);
            if (Input.GetKey("d")) panDir += new Vec2(1, 0);
            if (panDir != Vec2.zero)
                cam.transform.localPosition += (panDir * panSpeed * cam.orthographicSize * Time.deltaTime).Vec3();

            // Tool
            if (mouseOver)
                activeTool?.OnUpdate(mouse);

            if (Input.GetKeyDown(KeyCode.E))
                activeTool?.K_OnTab();
            if (Input.GetKeyDown(KeyCode.Escape))
                if (activeTools.Count > 0)
                    PopTool();

            activeTool?.OnUpdate(dt);

            // Game
            game.D_DebugUpdate(dt);

            if (speed != PlaySpeed.Paused)
                game.Update(dt * Speed(speed));
        }

        public void OnBuildMenu() => ReplaceTool(builds);
        public void OnOrdersMenu() => ReplaceTool(orders);

        public void OnSpeedChange(PlaySpeed speed)
        {
            gui.ButtonForSpeed(this.speed).SetSelected(false);
            gui.ButtonForSpeed(speed).SetSelected(true);
            this.speed = speed;
            // TODO: change animation speed
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

        private bool IsShift()
            => Input.GetKey(KeyCode.LeftShift) ||
               Input.GetKey(KeyCode.RightShift);

        public void OnClick(Vec2 scPos, InputButton button, int clickCount)
        {
            Vec2 realPos = ScreenToWorld(scPos);
            Vec2I pos = realPos.Floor();
            if (button == InputButton.Left)
            {
                if (game.ValidTile(pos))
                {
                    if (activeTool != null && activeTool.IsClickable())
                        activeTool.OnClick(realPos);
                    else
                        SelectClick(realPos, clickCount % 2);
                }
            }
            else if (button == InputButton.Right)
            {
                activeTool?.OnRightClick(realPos, scPos);
            }
        }

        private void SetSelection(Selection sel)
        {
            sel.FilterType();
            if (sel.Empty())
                PopAll();
            else
            {
                if (!IsShift() || baseTool != selection)
                    selection.SetSelection(sel);
                else
                    selection.AddSelection(sel);
            }
        }

        private void SelectClick(Vec2 pos, int clickCount)
        {
            if (clickCount == 1 || lastSingleClickSelection == null)
                SelectClick(pos);
            else
            {
                Rect area = ClampToMap(cam.WorldRect());

                Selection selection = new Selection();
                if (lastSingleClickSelection.minions.Count > 0)
                    selection.minions.AddRange(game.GUISelectMinions(area));
                else if (lastSingleClickSelection.items.Count > 0)
                {
                    var item = lastSingleClickSelection.items[0];
                    selection.items.AddRange(GetItems(area).Where(i => i.def == item.def));
                }
                else if (lastSingleClickSelection.buildings.Count > 0)
                {
                    var building = lastSingleClickSelection.buildings[0];
                    foreach (var t in area.RectInclusive().allPositionsWithin)
                    {
                        var tile = game.Tile(t);
                        if (tile.hasBuilding && tile.building.def == building.def)
                            selection.buildings.Add(tile.building);
                    }
                }

                SetSelection(selection);
            }
        }

        public Selection SelectAll(Vec2 pos)
        {
            Selection selection = new Selection();
            foreach (var minion in game.GUISelectMinions(pos))
                selection.minions.Add(minion);

            // TODO:agents

            var tile = game.Tile(pos.Floor());
            if (tile.hasItems)
                selection.items.AddRange(game.GUISelectItemsOnTile(tile));

            if (tile.hasBuilding)
                selection.buildings.Add(tile.building);

            return selection;
        }

        private void SelectClick(Vec2 pos)
        {
            var selection = SelectAll(pos);
            selection.SelectSingle();
            if (selection.Empty())
                lastSingleClickSelection = null;
            else
                lastSingleClickSelection = selection;
            SetSelection(selection);
        }

        private void SelectDrag(Vec2 dragEnd)
        {
            var area = DragRect(dragEnd);
            Selection selection = new Selection();
            foreach (var minion in game.GUISelectMinions(area))
                selection.minions.Add(minion);

            if (selection.minions.Count == 0)
                selection.items.AddRange(GetItems(area));

            SetSelection(selection);
        }

        private IEnumerable<TileItem> GetItems(Rect area)
        {
            RectInt areaInc = area.RectInclusive();
            foreach (var pos in areaInc.allPositionsWithin)
            {
                var tile = game.Tile(pos);
                if (tile.hasItems)
                    foreach (var item in game.GUISelectItemsOnTile(tile))
                        yield return item;
            }
        }

        private Rect ClampToMap(Rect rect)
            => rect.Clamp(new Rect(Vec2.zero, game.size));

        private Rect DragRect(Vec2 pos)
            => ClampToMap(MathExt.RectForPts(dragStart, pos));

        private RectInt DragRectInt(Vec2 pos)
            => DragRect(pos).RectInclusive();

        public void OnDragStart(Vec2 scStart, Vec2 scPos, InputButton button)
        {
            Vec2 start = ScreenToWorld(scStart);
            Vec2 pos = ScreenToWorld(scPos);
            if (button == InputButton.Left)
            {
                dragStart = start;
                if (activeTool != null && activeTool.IsDragable())
                {
                    activeTool.OnDragStart(DragRectInt(pos));
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
            activeTool?.RawMouseMove(pos);

            if (button == InputButton.Left)
            {
                if (activeTool != null && activeTool.IsDragable())
                {
                    activeTool.OnDrag(DragRectInt(pos));
                }
                else
                {
                    // TODO: selection preview?
                }

                UpdateDragOutline(pos);
            }
            else if (button == InputButton.Right && !Input.GetKey(KeyCode.LeftControl))
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
                    activeTool.OnDragEnd(DragRectInt(pos));
                else
                    SelectDrag(pos);

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


        public void OnMouseDown(Vec2 scPos, InputButton button) {
          activeTool?.RawMouseDown(button, ScreenToWorld(scPos));
        }

        public void OnMouseUp(Vec2 scPos, InputButton button) {
          activeTool?.RawMouseUp(button, ScreenToWorld(scPos));
        }
    }
}
