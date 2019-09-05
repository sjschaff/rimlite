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

    private LinkedList<Minion> minions;

    private void Awake()
    {
        minions = new LinkedList<Minion>();
    }

    // Start is called before the first frame update
    void Start()
    {
        minions.AddLast(_minion);
        _minion.Init(this);
    }

    // required:  true if tile is no longer passable, false if tile is now passable
    private void RerouteMinions(Vec2I tile, bool required)
    {
        // TODO: check if minions can reroute more efficiently
        if (!required)
            return;

        foreach (var minion in minions)
        {
            if (minion.HasJob())
                minion.Reroute(tile);
        }
    }

    private void RemoveBuilding(Vec2I pos)
    {
        var tile = map.Tile(pos);
        BB.Assert(tile.HasBuilding());

        bool reroute = !tile.building.passable;
        map.RemoveBuilding(pos);

        if (reroute)
            RerouteMinions(pos, false);
    }

    private void AddBuilding(Vec2I pos, Building building)
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

    // TODO: probably use polymorphic class in array w/ Click(tile) etc. rather than enum
    private enum Tool
    {
        CommandMove,
        Mine,
        Place,
        Build,
    }

    private Tool tool = Tool.CommandMove;

    private LinkedList<Job> currentJobs = new LinkedList<Job>();

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
                Job job = new Job(JobType.Move, tile);
                _minion.AssignJob(job);
            }
            else if (tool == Tool.Mine)
            {
                if (map.Tile(tile).Mineable())
                {
                    Debug.Log("added job");
                    currentJobs.AddLast(new Job(JobType.Mine, tile));
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
            if (!minion.HasJob())
            {
                foreach (var job in currentJobs)
                {
                    if (minion.AssignJob(job))
                    {
                        currentJobs.Remove(job);
                        break;
                    }
                    else
                        Debug.Log("did not assign job");
                }
            }
        }
    }

    public void CompleteJob(Job job)
    {
        Debug.Log("Job Finished.");
        if (job.type == JobType.Mine)
        {
            RemoveBuilding(job.tile);
            var item = Instantiate(itemPrefab, job.tile.Vec3(), Quaternion.identity).GetComponent<ItemVis>();
            item.Init(item.spriteRenderer.sprite, "Poooo");
        }
    }

    public void AbandonJob(Job job)
    {
        Debug.Log("job abandoned: " + job.type);
        if (!job.IsPersonal())
            currentJobs.AddLast(job);
    }
}
