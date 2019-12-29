using Phevolution;
using UnityEngine;

namespace Thanks.Planet
{
    public class PlanetGUI : MonoBehaviour
    {
        private PlanetMesh planetMesh { get; set; }

        [SerializeField]
        private Camera cameraMain = null;
        private Transform cameraPivot { get; set; }

        private Rect clientRect { get; set; }
        private void Awake()
        {
            planetMesh = GetComponent<PlanetMesh>();
            cameraPivot = cameraMain.transform.parent;

            var d = 10f;
            var w = 200f;
            var h = 400f;
            clientRect = new Rect(Screen.width - w - d, d, w, h);
        }


        public float speedY = 10;
        public float speedX = 10;
        public float scrollSpeed = 1000;

        private Vector3 mousePosition { get; set; }
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                mousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0))
            {
                var dv = Input.mousePosition - mousePosition;
                mousePosition = Input.mousePosition;

                var rot = cameraPivot.localEulerAngles;
                //rot.x -= dv.y * Time.deltaTime * speedX;//相机上下
                //rot.x = Mathf.Clamp(rot.x, -80f, 80f);
                rot.y += dv.x * Time.deltaTime * speedY;//相机左右
                cameraPivot.localRotation = Quaternion.Euler(rot);
            }

            if (cameraMain.orthographic)
            {
                var size = cameraMain.orthographicSize;
                size -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * scrollSpeed;//相机前后
                size = Mathf.Max(0.1f, size);
                cameraMain.orthographicSize = size;
            }
            else
            {
                var p = cameraMain.transform.localPosition;
                p.z += Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * scrollSpeed;//相机前后
                p.z = Mathf.Min(-10.25f, p.z);
                cameraMain.transform.localPosition = p;
            }
        }

        private void OnGUI()
        {
            clientRect = GUI.Window(10086, clientRect, WindowFunction, "地图参数");
        }

        private int drawMode { get; set; }
        private void WindowFunction(int windowId)
        {
            if (planetMesh.mapJobs == null) return;
            var config = planetMesh.mapJobs.config;

            var mode = GUILayout.Toolbar(drawMode, new string[] { "Flat", "Quad" });
            if (mode != drawMode)
            {
                drawMode = mode;
                var cfg = (ChangeI)config[4];
                cfg.v = drawMode;
            }

            //GUI.DragWindow();
        }
    }
}