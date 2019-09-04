using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortingLayerHack : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var line = GetComponent<LineRenderer>();
        line.sortingLayerName = "Highlight";
        line.sortingLayerID = SortingLayer.NameToID("Highlight");
        line.sortingOrder = 10000;
        Debug.Log("layer id: " + SortingLayer.NameToID("Default"));
        Debug.Log("layer id: " + SortingLayer.NameToID("Highlight"));
        Debug.Log("layer id: " + SortingLayer.NameToID("poop"));
    }

    // Update is called once per frame
    void Update()
    {
    }
}
