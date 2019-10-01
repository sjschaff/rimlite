using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec3 = UnityEngine.Vector3;

namespace BB
{
    public static class VecExt
    {
        public static Vec3 OrthoScale(this UnityEngine.Camera c)
        {
            float h = c.orthographicSize * 2;
            float w = h * c.aspect;
            return new Vec3(w, h, 0);
        }
    }

    public class Camera : MonoBehaviour
    {
        UnityEngine.Camera cam;

        private const float panSpeed = 2.5f;
        private const float zoomZpeed = .5f;
        private const float minZoom = 2;
        private const float maxZoom = 20;
        public bool lockToMap = false;

        Vec3 dragStart;
        Vec3 transStart;

        public void Start()
        {
            cam = GetComponent<UnityEngine.Camera>();
        }

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
                Vec3 drag = cam.ScreenToViewportPoint(dragStart - Input.mousePosition);
                drag.Scale(cam.OrthoScale());
                transform.localPosition = transStart + drag;
            }

            if (lockToMap)
            {
                Vec2 halfSize = new Vec2(cam.orthographicSize * cam.aspect, cam.orthographicSize);

                Vec2 pos = cam.transform.localPosition.xy();
                pos = Vec2.Max(pos, halfSize);
                pos = Vec2.Min(pos, new Vec2(64, 64) - halfSize); // TODO: use real map size
                cam.transform.localPosition = new Vec3(pos.x, pos.y, -11);
            }
        }
    }
}