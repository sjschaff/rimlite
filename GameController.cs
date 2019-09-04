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

    void RemoveBuilding(Vec2I tile)
    {
        map.RemoveBuilding(tile);

        // TODO: check if minions can reroute more efficiently
    }

    void UpdateTile(Vec2I tile, Terrain terrain)
    {
        map.UpdateTile(tile, terrain);

        foreach (var minion in minions)
        {
            if (minion.HasJob())
                minion.Reroute(tile);
        }
    }

    // TODO: probably use polymorphic class in array w/ Click(tile) etc. rather than enum
    private enum Tool
    {
        CommandMove,
        Mine,
        Place,
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
                UpdateTile(tile, Terrain.Path);
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
