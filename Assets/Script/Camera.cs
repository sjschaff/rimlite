using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;

public static class VecExt
{
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

    private const float panSpeed = 2.5f;
    private const float zoomZpeed = .5f;
    private const float minZoom = 2;
    private const float maxZoom = 20;

    Vector3 dragStart;
    Vector3 transStart;

    // Start is called before the first frame update
    public void Start()
    {
        cam = GetComponent<UnityEngine.Camera>();
    }

    // Update is called once per frame
    public void Update()
    {
        float scroll = Input.mouseScrollDelta.y;
        cam.orthographicSize -= zoomZpeed * scroll;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

        Vec2 panDir = new Vec2(0, 0);
        if (Input.GetKey("w")) panDir += new Vec2(0, 1);
        if (Input.GetKey("s")) panDir += new Vec2(0, -1);
        if (Input.GetKey("a")) panDir += new Vec2(-1, 0);
        if (Input.GetKey("d")) panDir += new Vec2(1, 0);

        cam.transform.localPosition += (panDir * panSpeed * cam.orthographicSize * Time.deltaTime).Vec3();

        if (Input.GetMouseButtonDown(1))
        {
            dragStart = Input.mousePosition;
            transStart = transform.localPosition;

        }

        if (Input.GetMouseButton(1))
        {
            Vector3 drag = cam.ScreenToViewportPoint(dragStart - Input.mousePosition);
            drag.Scale(cam.OrthoScale());
            transform.localPosition = transStart + drag;
        }
    }
}
