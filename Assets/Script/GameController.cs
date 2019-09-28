using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Vec3 = UnityEngine.Vector3;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using System;

public class GameController : MonoBehaviour
{
    // ---- Editor Values ----
    public Transform minionPrefab;
    public Transform itemPrefab;
    public Transform jobOverlayPrefab;
    // -----------------------

    public AssetSrc assets { get; private set; }
    public Registry registry { get; private set; }
    private Map map;

    public Defs defs => registry.defs;


    // TODO: move this stuff to some ui logic somewhere
    private Transform mouseHighlight;
    private LineRenderer dragOutline;

    public Transform CreateJobOverlay(Vec2I pos, Sprite sprite)
    {
        var overlay = jobOverlayPrefab.Instantiate(pos + new Vec2(.5f, .5f), transform);
        overlay.GetComponent<SpriteRenderer>().sprite = sprite;
        return overlay;
    }

    public LineRenderer CreateDragOutline()
        => new GameObject("Drag Outline").AddLineRenderer("Highlight", 0, Color.white, 1, true, true, null);

    public Transform CreateMouseHighlight()
        => new GameObject("Mouse Highlight").AddLineRenderer(
            "Highlight", 0,
            new Color(.2f, .2f, .2f, .5f), 1 / 32f, true, false, new Vec2[] {
                new Vec2(0, 0),
                new Vec2(1, 0),
                new Vec2(1, 1),
                new Vec2(0, 1)
            }).transform;

    private LinkedList<UITool> tools;
    private LinkedListNode<UITool> currentTool;
    private readonly LinkedList<Minion> minions = new LinkedList<Minion>();
    private Minion D_minionNoTask;
    private readonly LinkedList<Item> items = new LinkedList<Item>();
    private readonly LinkedList<Job> currentJobs = new LinkedList<Job>();
    private readonly JobWalkDummy walkDummyJob = new JobWalkDummy();
    private UITool tool => currentTool.Value;

    // TODO: figure out what should be in awake and what should be in start
    private void Awake() {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
    }

    // Start is called before the first frame update
    void Start()
    {
        registry = new Registry();
        assets = new AssetSrc(defs);
        map = new Map(this);

        mouseHighlight = CreateMouseHighlight();
        dragOutline = CreateDragOutline();
        dragOutline.enabled = false;
        dragOutline.sortingOrder = 1;

        for (int i = 0; i < 10; ++i)
        {
            Minion minion = minionPrefab.Instantiate(new Vec2(1 + i, 1), transform).GetComponent<Minion>();
            minion.Init(this);
            minions.AddLast(minion);
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

    public void AddJob(Job job)
    {
        Debug.Log("Added Job: " + job + "(" + job.GetHashCode() + ")");
        currentJobs.AddLast(job);
    }

    public void RemoveJob(Job job)
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

    // Update is called once per frame
    void Update()
    {
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
                //Debug.Log("OnClick: " + time);
                if (map.ValidTile(clickStart.Floor()))
                    tool.OnClick(clickStart.Floor());
            }
            else
            {
                //Debug.Log("OnDragEnd");
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
            //Debug.Log("OnDragStart");
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
