using System.Collections.Generic;
using System;
using UnityEngine;

using Vec3 = UnityEngine.Vector3;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

/* Ideas/concepts *
   multi-levels
        reuire walls/supports underneath other walls suppors
   schools of magic -> schools of research
        specialized mages req. to research different trees
   portals to other planes/dimensions possible based on schools of magic
        rare / lategame resources found in planes, unlocks high tier research
        example, capture elementals and study them or something
        maybe early usage allows limited access, i.e planes touch but dont connect
            to allow for heaters/coolers, then later access allows travel between,
            dangerous but allows cooler stuff
   send adventuring parties to explore planes, caves, mines, dungeons, all on 1 map
   lots of concepts from dnd
        stock adventuring kits for sale
        sell magic items, potions etc.
        sell access to teleportaion circle
            private/public circles?
        library access
            buy books from travelers, advance research or maybe unlock special things
   ability to house travelers/adventurers
   random events
        mine infestations
        portal breach
        angry peasants
        attack via tele. circle.
        cave ins
        later game magical beasts attack, eventually dragons
*/

namespace BB
{
    public class GameController : MonoBehaviour
    {
        // ---- Editor Values ----
        public Transform itemPrefab;
        // -----------------------

        public AssetSrc assets { get; private set; }
        public Registry registry { get; private set; }
        private Map map;

        public Defs defs => registry.defs;


        // TODO: move this stuff to some ui logic somewhere
        private Transform mouseHighlight;
        private LineRenderer dragOutline;

        private LinkedList<UITool> tools;
        private LinkedListNode<UITool> currentTool;
        private readonly LinkedList<Minion> minions = new LinkedList<Minion>();
        private Minion D_minionNoTask;
        private readonly LinkedList<Item> items = new LinkedList<Item>();
        private readonly LinkedList<IJob> currentJobs = new LinkedList<IJob>();
        private readonly JobWalkDummy walkDummyJob = new JobWalkDummy();
        private UITool tool => currentTool.Value;

        // TODO: figure out what should be in awake and what should be in start
        private void Awake()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        }

        // Start is called before the first frame update
        void Start()
        {
            registry = new Registry();
            assets = new AssetSrc();
            map = new Map(this);

            mouseHighlight = assets.CreateLine(
                transform, Vec2.zero, "Mouse Highlight",
                RenderLayer.Highlight, new Color(.2f, .2f, .2f, .5f),
                1 / 32f, true, false, new Vec2[] {
                    new Vec2(0, 0),
                    new Vec2(1, 0),
                    new Vec2(1, 1),
                    new Vec2(0, 1)
                }).transform;

            dragOutline = assets.CreateLine(
                transform, Vec2.zero, "DragOutline",
                RenderLayer.Highlight.Layer(1), Color.white,
                1, true, true, null);
            dragOutline.enabled = false;

            for (int i = 0; i < 10; ++i)
            {
                minions.AddLast(new Minion(this, new Vec2(1 + i, 1)));
            }
            D_minionNoTask = minions.First.Value;

            tools = UITool.RegisterTools(this);
            currentTool = tools.First;
        }

        public Vec2I[] GetPath(Vec2I start, Vec2I endHint, Func<Vec2I, bool> dstFn)
            => map.nav.GetPath(start, endHint, dstFn);

        public ITile Tile(Vec2I pos) => map.Tile(pos);

        // required:  true if pos is no longer passable, false if pos is now passable
        public void RerouteMinions(Vec2I pos, bool required)
        {
            // TODO: check if minions can reroute more efficiently
            if (!required)
                return;

            foreach (var minion in minions)
            {
                if (minion.HasTask())
                    minion.Reroute(pos);
            }
        }

        private void RerouteMinions(Vec2I pos, bool wasPassable, bool nowPassable)
        {
            if (wasPassable != nowPassable)
                RerouteMinions(pos, wasPassable);
        }

        public IEnumerable<Item> FindItems(ItemDef def)
        {
            foreach (Item item in items)
                if (item.def == def)
                    yield return item;
        }

        public void RemoveBuilding(Vec2I pos)
        {
            var tile = map.Tile(pos);
            BB.Assert(tile.hasBuilding);

            bool passable = tile.passable;
            map.RemoveBuilding(pos);
            RerouteMinions(pos, passable, tile.passable);
        }

        public void AddBuilding(Vec2I pos, IBuilding building)
        {
            var tile = map.Tile(pos);
            BB.Assert(!tile.hasBuilding);

            bool passable = tile.passable;
            map.AddBuilding(pos, building);
            RerouteMinions(pos, passable, tile.passable);
        }

        public void ReplaceBuilding(Vec2I pos, IBuilding building)
        {
            var tile = map.Tile(pos);
            BB.Assert(tile.hasBuilding);

            bool passable = tile.passable;
            map.ReplaceBuilding(pos, building);
            RerouteMinions(pos, passable, tile.passable);
        }

        public void ModifyTerrain(Vec2I pos, Terrain terrain)
        {
            var tile = map.Tile(pos);
            bool wasPassable = tile.terrain.passable;
            bool nowPassable = terrain.passable;

            map.ModifyTerrain(pos, terrain);

            if (wasPassable != nowPassable)
                RerouteMinions(pos, wasPassable);
        }

        public void DropItem(Vec2I pos, Item item)
        {
            BB.AssertNotNull(item);
            item.transform.parent = transform;
            item.transform.localPosition = pos.Vec3();
            item.Place(pos);
            item.Configure(Item.Config.Ground);
            items.AddLast(item);
        }

        public void DropItem(Vec2I pos, ItemInfo info) => DropItem(pos, CreateItem(pos, info));

        private Item CreateItem(Vec2I pos, ItemInfo info)
        {
            var item = itemPrefab.Instantiate(pos, transform).GetComponent<Item>();
            item.Init(this, pos, info);
            return item;
        }

        public Item TakeItem(Item item, int amt)
        {
            BB.AssertNotNull(item);
            BB.Assert(item.amtAvailable >= amt);
            BB.Assert(items.Contains(item));

            if (amt == item.amt)
            {
                items.Remove(item);
                return item;
            }
            else
            {
                item.Remove(amt);
                return CreateItem(item.pos, new ItemInfo(item.def, amt));
            }
        }

        public void K_MoveMinion(Vec2I pos) => D_minionNoTask.AssignTask(walkDummyJob.CreateWalkTask(pos));

        public void AddJob(IJob job)
        {
            Debug.Log("Added Job: " + job + "(" + job.GetHashCode() + ")");
            currentJobs.AddLast(job);
        }

        public void RemoveJob(IJob job)
        {
            currentJobs.Remove(job);
        }

        public bool IsTileOccupied(Vec2I pos, Minion minionIgnore)
        {
            foreach (Minion minion in minions)
            {
                if (minion != minionIgnore && Vec2.Distance(minion.pos, pos) < .9f)
                    return true;
            }

            return false;
        }

        public void VacateTile(Vec2I pos)
        {
            foreach (Minion minion in minions)
            {
                if (minion.idle && minion.pos.Floor() == pos)
                {
                    if (!minion.AssignTask(walkDummyJob.CreateVacateTask(pos)))
                        Debug.Log("cannon vacate: no path");
                }
            }
        }

        // TODO: move to ui code somewhere
        float time = 0;
        bool isDraging = false;
        Vec2 clickStart;

        private const float dragTime = .16f;

        private void UpdateDragOutline(Vec2 end)
        {
            float units = UnityEngine.Camera.main.orthographicSize * 2;
            float pixels = Screen.height;

            float unitsPerPixel = units / pixels;
            dragOutline.widthMultiplier = 2 * unitsPerPixel;

            var a = clickStart;
            var b = end;
            dragOutline.positionCount = 4;
            dragOutline.SetPositions(new Vec3[] {
                new Vec3(a.x, a.y),
                new Vec3(b.x, a.y),
                new Vec3(b.x, b.y),
                new Vec3(a.x, b.y)
            });
        }

        void Update()
        {
            foreach (var minion in minions)
                minion.Update();

            if (Input.GetKeyDown("tab"))
                tool.OnTab();

            var mousePos = UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition).xy();
            mouseHighlight.localPosition = (Vec2)mousePos.Floor();
            UpdateDragOutline(mousePos);

            if (Input.GetKeyDown("l"))
            {
                currentTool = currentTool.Next;
                if (currentTool == null)
                    currentTool = tools.First;

                Debug.Log("Current Tool: " + tool);
            }

            if (Input.GetMouseButtonDown(0))
            {
                time = 0;
                clickStart = mousePos;
            }
            else if (Input.GetMouseButton(0))
                time += Time.deltaTime;

            if (Input.GetMouseButtonUp(0))
            {
                if (!isDraging)
                {
                    if (map.ValidTile(clickStart.Floor()))
                        tool.OnClick(clickStart.Floor());
                }
                else
                {
                    tool.OnDragEnd(clickStart, mousePos);
                    dragOutline.enabled = false;
                }

                time = 0;
                isDraging = false;
            }

            if (isDraging)
                tool.OnDragUpdate(clickStart, mousePos);

            if (time > dragTime && !isDraging)
            {
                isDraging = true;
                tool.OnDragStart(clickStart, mousePos);
                dragOutline.enabled = true;
            }

            foreach (Minion minion in minions)
            {
                if (minion == D_minionNoTask)
                    continue;

                if (!minion.HasTask())
                {
                    foreach (var job in currentJobs)
                    {
                        bool gotTask = false;
                        foreach (var task in job.AvailableTasks())
                        {
                            if (minion.AssignTask(task))
                            {
                                gotTask = true;
                                break;
                            }
                        }

                        if (gotTask)
                            break;
                    }
                }
            }
        }
    }

}