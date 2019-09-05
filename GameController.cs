using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
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
    private LinkedList<Minion> minions;
    private LinkedList<Job> currentJobs;
    private JobWalkDummy walkDummyJob;

    private void Awake()
    {
        minions = new LinkedList<Minion>();
    }

    // Start is called before the first frame update
    void Start()
    {
        minions.AddLast(_minion);
        _minion.Init(this);
        currentJobs = new LinkedList<Job>();
        walkDummyJob = new JobWalkDummy();

        tools = UITool.RegisterTools(this);
        currentTool = tools.First;
    }

    // required:  true if tile is no longer passable, false if tile is now passable
    private void RerouteMinions(Vec2I tile, bool required)
    {
        // TODO: check if minions can reroute more efficiently
        if (!required)
            return;

        foreach (var minion in minions)
        {
            if (minion.HasTask())
                minion.Reroute(tile);
        }
    }

    public void RemoveBuilding(Vec2I pos)
    {
        var tile = map.Tile(pos);
        BB.Assert(tile.HasBuilding());

        bool reroute = !tile.building.passable;
        map.RemoveBuilding(pos);

        if (reroute)
            RerouteMinions(pos, false);
    }

    public void AddBuilding(Vec2I pos, Building building)
    {
        var tile = map.Tile(pos);
        BB.Assert(!tile.HasBuilding());

        map.AddBuilding(pos, building);
        RerouteMinions(pos, true);
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

    public void DropItem(Vec2I pos/*, item info*/)
    {
        // TODO: make this real
        var item = Instantiate(itemPrefab, pos.Vec3(), Quaternion.identity).GetComponent<ItemVis>();
        item.Init(item.spriteRenderer.sprite, "25");
    }

    public void K_MoveMinion(Vec2I pos) => _minion.AssignTask(walkDummyJob.CreateWalkTask(pos));

    public void AddJob(Job job)
    {
        Debug.Log("Added Job.");
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
                    foreach (var task in job.AvailableTasks())
                    {
                        if (minion.AssignTask(task))
                        {
                            job.ClaimTask(task);
                            break;
                        }
                    }
                }
            }
        }
    }
}
