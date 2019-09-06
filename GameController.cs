using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

public class GameController : MonoBehaviour
{
    public Map map; // TODO: this does not need to be a gameobject
    public Minion _minion;
    public Transform mouseHighlight;
    public Transform itemPrefab;

    private LinkedList<UITool> tools;
    private LinkedListNode<UITool> currentTool;
    private UITool tool => currentTool.Value;
    private LinkedList<Minion> minions = new LinkedList<Minion>();
    private LinkedList<Item> items = new LinkedList<Item>();
    private LinkedList<Job> currentJobs = new LinkedList<Job>();
    private JobWalkDummy walkDummyJob = new JobWalkDummy();

    private void Awake() {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
    }

    // Start is called before the first frame update
    void Start()
    {
        minions.AddLast(_minion);
        _minion.Init(this);

        tools = UITool.RegisterTools(this);
        currentTool = tools.First;
    }

    // required:  true if pos is no longer passable, false if pos is now passable
    private void RerouteMinions(Vec2I pos, bool required)
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

    public IEnumerable<Item> FindItems(ItemType type)
    {
        foreach (Item item in items)
            if (item.type == type)
                yield return item;
    }

    public void RemoveBuilding(Vec2I pos)
    {
        var tile = map.Tile(pos);
        BB.Assert(tile.HasBuilding());

        bool passable = tile.Passable();
        map.RemoveBuilding(pos);
        RerouteMinions(pos, passable, tile.Passable());
    }

    public void AddBuilding(Vec2I pos, Building building)
    {
        var tile = map.Tile(pos);
        BB.Assert(!tile.HasBuilding());

        bool passable = tile.Passable();
        map.AddBuilding(pos, building);
        RerouteMinions(pos, passable, tile.Passable());
    }

    public void ReplaceBuilding(Vec2I pos, Building building)
    {
        var tile = map.Tile(pos);
        BB.Assert(tile.HasBuilding());

        bool passable = tile.Passable();
        map.ReplaceBuilding(pos, building);
        RerouteMinions(pos, passable, tile.Passable());
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
        item.ShowText(true);
        items.AddLast(item);
    }

    public void DropItem(Vec2I pos, ItemInfo info) => DropItem(pos, CreateItem(pos, info));

    private Item CreateItem(Vec2I pos, ItemInfo info)
    {
        var item = Instantiate(itemPrefab, pos.Vec3(), Quaternion.identity).GetComponent<Item>();
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
            return CreateItem(item.pos, new ItemInfo(item.type, amt));
        }
    }

    public void K_MoveMinion(Vec2I pos) => _minion.AssignTask(walkDummyJob.CreateWalkTask(pos));

    public void AddJob(Job job)
    {
        Debug.Log("Added Job: " + job);
        currentJobs.AddLast(job);
    }

    public void RemoveJob(Job job)
    {
        currentJobs.Remove(job);
    }

    // Update is called once per frame
    void Update()
    {
        var tile = map.MouseToTile();
        mouseHighlight.localPosition = (Vec2)tile;

        if (Input.GetKeyDown("l"))
        {
            currentTool = currentTool.Next;
            if (currentTool == null)
                currentTool = tools.First;

            Debug.Log("Current Tool: " + tool);
        }

        if (Input.GetMouseButtonDown(0) && map.ValidTile(tile))
        {
            tool.OnClick(tile);
        }

        foreach (Minion minion in minions)
        {
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
