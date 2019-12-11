using System.Collections.Generic;
using UnityEngine;

namespace Assets
{
    public class MapGUI : MonoBehaviour
    {
        //    const initialParams = {
        //  elevation: [
        //    ['seed', 187, 1, 1 << 30],
        //    ['island', 0.5, 0, 1],
        //    ['noisy_coastlines', 0.01, 0, 0.1],
        //    ['hill_height', 0.02, 0, 0.1],
        //    ['mountain_jagged', 0, 0, 1],
        //    ['ocean_depth', 1.5, 1, 3]
        //  ],
        //  biomes: [
        //    ['wind_angle_deg', 0, 0, 360],
        //    ['raininess', 0.9, 0, 2],
        //    ['rain_shadow', 0.5, 0.1, 2],
        //    ['evaporation', 0.5, 0, 1]
        //  ],
        //  rivers: [
        //    ['lg_min_flow', 2.7, -5, 5],
        //    ['lg_river_width', -2.7, -5, 5],
        //    ['flow', 0.2, 0, 1]
        //  ],
        //  render: [
        //    ['light_angle_deg', 80, 0, 360],
        //    ['slope', 2, 0, 5],
        //    ['flat', 2.5, 0, 5],
        //    ['ambient', 0.25, 0, 1],
        //    ['overhead', 30, 0, 60],
        //    ['mountain_height', 50, 0, 250],
        //    ['outline_depth', 1, 0, 2],
        //    ['outline_strength', 15, 0, 30],
        //    ['outline_threshold', 0, 0, 100],
        //    ['outline_coast', 0, 0, 1],
        //    ['outline_water', 10.0, 0, 20], // things start going wrong when this is high
        //    ['biome_colors', 1, 0, 1]
        //  ]
        //};
        private Dictionary<string, float[]> mapParams = new Dictionary<string, float[]>()
    {
        { "zoom",new float[]{500,50,1000} },
        { "x",new float[]{500,0,1000} },
        { "y",new float[]{500,0,1000} },
        { "tilt_deg",new float[]{0,0,90} },
        { "rotate_deg",new float[]{0,0,360} },

        { "outline_depth",new float[]{1,0,2} },
        { "outline_strength",new float[]{15,0,30} },
        { "outline_threshold",new float[]{0,0,100} },
        { "outline_coast",new float[]{0,0,1} },
        { "outline_water",new float[]{10,0,20} },// things start going wrong when this is high
        { "biome_colors",new float[]{1,0,1} },
    };

        /// <summary>主窗口尺寸</summary>
        private Rect clientRect { get; set; }
        private Vector2 scrollPosition { get; set; }

        private MapMesh mapMesh { get; set; }
        private bool activeUI { get; set; }
        private Camera mainCamera { get; set; }
        private Vector3 eulerAngles { get; set; }

        // Start is called before the first frame update
        void Start()
        {
            var d = 10f;
            var w = 200f;
            var h = 800f;
            clientRect = new Rect(Screen.width - w - d, d, w, h);

            scrollPosition = Vector2.zero;

            mapMesh = GetComponent<MapMesh>();
            mainCamera = Camera.main;
            eulerAngles = transform.parent.localEulerAngles;

            activeUI = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H)) activeUI = !activeUI;
            if (!activeUI) return;

            if (Input.GetMouseButtonDown(0))
            {
                var p = mapMesh.getHitPosition();
                mapMesh.painting.startPen(p);
            }
            else if (Input.GetMouseButton(0))
            {
                var p = mapMesh.getHitPosition();
                mapMesh.painting.dragPen(p);
                mapMesh.redraw();
            }
        }
        // Update is called once per frame
        private void OnGUI()
        {
            if (activeUI) clientRect = GUI.Window(10086, clientRect, WindowFunction, "地图参数");
        }

        private void WindowFunction(int windowId)
        {
            int i = 0;

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var k in mapParams)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();

                GUILayout.Label(k.Key);
                float val = GUILayout.HorizontalSlider(k.Value[0], k.Value[1], k.Value[2]);
                if (!k.Value[0].Equals(val))
                {
                    k.Value[0] = val;
                    switch (i)
                    {
                        case 0:
                            mainCamera.orthographicSize = val;
                            break;
                        case 1:
                        case 2:
                            var pos = mainCamera.transform.localPosition;
                            if (i == 1) pos.x = val; else pos.y = val;
                            mainCamera.transform.localPosition = pos;
                            break;
                        case 3:
                        case 4:
                            var rot = eulerAngles;
                            if (i == 3) rot.x = val; else rot.z = val;
                            transform.parent.localEulerAngles = eulerAngles = rot;
                            break;
                        default:
                            mapMesh.landzs.setFloat($"_{k.Key}", val);
                            break;
                    }
                }
                GUILayout.Label(k.Value[0].ToString());

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                i++;
            }
            GUILayout.EndScrollView();

            if (Input.GetKey(KeyCode.LeftControl)) GUI.DragWindow();
        }
    }
}