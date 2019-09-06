using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VecExt
{

   // public static Vector2 poop(this Vector3 v) { return new Vector2(v.x, v.y); }
   public static Vector3 OrthoScale(this UnityEngine.Camera c)
    {
        float h = c.orthographicSize * 2;
        float w = h * c.aspect;
        return new Vector3(w, h, 0);
    }
}

public class Camera : MonoBehaviour
{
    UnityEngine.Camera cam;

    private const float scrollSpeed = .5f;
    private const float minZoom = 1;
    private const float maxZoom = 10;

    Vector3 dragStart;
    Vector3 transStart;
    Transform t;

    // Start is called before the first frame update
    public void Start()
    {
        cam = GetComponent<UnityEngine.Camera>();
    }

    // Update is called once per frame
    public void Update()
    {
        float scroll = Input.mouseScrollDelta.y;
        cam.orthographicSize -= scrollSpeed * scroll;
        //if (scroll != 0)
        //    Debug.Log("size: " + cam.orthographicSize);
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

        if (Input.GetMouseButtonDown(1))
        {
            dragStart = Input.mousePosition;
            transStart = transform.localPosition;
            //Debug.Log("start:" + transStart);
            //Debug.Log("mouse: " + cam.ScreenToViewportPoint(Input.mousePosition));

        }
        if (Input.GetMouseButton(1))
        {
            Vector3 drag = cam.ScreenToViewportPoint(dragStart - Input.mousePosition);
            drag.Scale(cam.OrthoScale());
            transform.localPosition = transStart + drag;
        }

        if (Input.GetMouseButtonDown(0))
        {
            //Debug.Log("mouse: " + cam.ScreenToWorldPoint(Input.mousePosition));
        }
    }
}
