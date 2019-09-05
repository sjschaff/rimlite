using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;
using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

public class GameController : MonoBehaviour
{
    // TODO: probably use polymorphic class in array w/ Click(tile) etc. rather than enum
    private enum Tool
    {
        CommandMove,
        Mine,
        Place,
        Build,
    }

    public Map map; // TODO: this does not need to be a gameobject
    public Minion _minion;
    public Transform mouseHighlight;
    public Transform itemPrefab;

    private Tool tool;
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
        tool = Tool.CommandMove;
        walkDummyJob = new JobWalkDummy();
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

    private void ModifyTerrain(Vec2I pos, Terrain terrain)
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

    // Update is called once per frame
    void Update()
    {
        var tile = map.MouseToTile();
        mouseHighlight.localPosition = (Vec2)tile;

        if (Input.GetKeyDown("l"))
        {
            tool = tool.Next();
            Debug.Log("Current Tool: " + tool);
        }

        if (Input.GetMouseButtonDown(0) && map.ValidTile(tile))
        {
            if (tool == Tool.CommandMove)
            {
                _minion.AssignTask(walkDummyJob.CreateWalkTask(tile));
            }
            else if (tool == Tool.Mine)
            {
                if (map.Tile(tile).Mineable())
                {
                    Debug.Log("added job");
                    currentJobs.AddLast(new JobMine(this, tile));
                }
            }
            else if (tool == Tool.Place)
            {
                ModifyTerrain(tile, new TerrainStandard(TerrainStandard.Terrain.Path));
            }
            else if (tool == Tool.Build)
            {
                if (!map.Tile(tile).HasBuilding())
                    AddBuilding(tile, new BuildingWall(BuildingWall.Wall.StoneBrick));
            }
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

    public void RemoveJob(Job job)
    {
        currentJobs.Remove(job);
    }
}
