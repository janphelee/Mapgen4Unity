using Assets.MapJobs;
using System.Collections.Generic;
using UnityEngine;

namespace Assets
{
    public class MapGUI : MonoBehaviour
    {
        private int[] seed = new int[] { 187, 1, 1 << 30 };
        private Dictionary<string, float[]> elevationParams = new Dictionary<string, float[]>() {
            { "island",new float[]{ 0.5f, 0, 1 } },
            { "noisy_coastlines",new float[]{ 0.01f, 0, 0.1f } },
            { "hill_height",new float[]{ 0.02f, 0, 0.1f } },
            { "mountain_jagged",new float[]{ 0, 0, 1 } },
            { "ocean_depth",new float[]{ 1.5f, 1, 3 } },
        };
        private Dictionary<string, float[]> biomesParams = new Dictionary<string, float[]>() {
            { "wind_angle_deg",new float[]{ 0, 0, 360 } },
            { "raininess",new float[]{ 0.9f, 0, 2 } },
            { "rain_shadow",new float[]{ 0.5f, 0.1f, 2 } },
            { "evaporation",new float[]{ 0.5f, 0, 1 } },
        };
        private Dictionary<string, float[]> riversParams = new Dictionary<string, float[]>() {
            { "lg_min_flow",new float[]{ 2.7f, -5, 5 } },
            { "lg_river_width",new float[]{ -2.7f, -5, 5 } },
            { "flow",new float[]{ 0.2f, 0, 1 } },
        };

        private Dictionary<string, float[]> renderParams = new Dictionary<string, float[]>()
        {
            { "zoom",new float[]{500,50,1000} },
            { "x",new float[]{500,0,1000} },
            { "y",new float[]{500,0,1000} },
            { "tilt_deg",new float[]{0,0,90} },
            { "rotate_deg",new float[]{0,0,360} },

            { "light_angle_deg",new float[]{ 80, 0, 360 } },
            { "slope",new float[]{ 2, 0, 5 } },
            { "flat",new float[]{ 2.5f, 0, 5 } },
            { "ambient",new float[]{ 0.25f, 0, 1 } },
            { "overhead",new float[]{ 30, 0, 60 } },
            { "mountain_height",new float[]{ 50, 0, 250 } },

            { "outline_depth",new float[]{1,0,2} },
            { "outline_strength",new float[]{15,0,30} },
            { "outline_threshold",new float[]{0,0,100} },
            { "outline_coast",new float[]{0,0,1} },
            { "outline_water",new float[]{10,0,20} },// things start going wrong when this is high
            { "biome_colors",new float[]{1,0,1} },
        };
        private int selected1 { get; set; }
        private int selected2 { get; set; }

        /// <summary>主窗口尺寸</summary>
        private Rect clientRect { get; set; }
        private Vector2 scrollPosition { get; set; }

        private MapMesh mapMesh { get; set; }
        private bool activeUI { get; set; }
        private Vector3 eulerAngles { get; set; }

        private string[] sizeTxts { get; set; }
        private string[] toolTxts { get; set; }

        // Start is called before the first frame update
        void Awake()
        {
            var d = 10f;
            var w = 200f;
            var h = 800f;
            clientRect = new Rect(Screen.width - w - d, d, w, h);

            scrollPosition = Vector2.zero;

            mapMesh = GetComponent<MapMesh>();
            eulerAngles = transform.parent.localEulerAngles;

            sizeTxts = new string[MapPaint.SIZES.Count];
            MapPaint.SIZES.Keys.CopyTo(sizeTxts, 0);

            toolTxts = new string[MapPaint.TOOLS.Count];
            MapPaint.TOOLS.Keys.CopyTo(toolTxts, 0);

            activeUI = true;
        }

        private bool isHoverRect(Rect rect, Vector3 p)
        {
            float x = p.x, y = Screen.height - p.y;

            if (x < rect.x || x > rect.x + rect.width) return false;
            if (y < rect.y || y > rect.y + rect.height) return false;
            return true;
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H)) activeUI = !activeUI;
            if (!activeUI) return;
            if (isHoverRect(clientRect, Input.mousePosition)) return;

            if (Input.GetMouseButtonDown(0))
            {
                var p = mapMesh.getHitPosition();
                mapMesh.painting.startPen(p, sizeTxts[selected1], toolTxts[selected2]);
            }
            else if (Input.GetMouseButton(0))
            {
                var p = mapMesh.getHitPosition();
                mapMesh.painting.dragPen(p, sizeTxts[selected1], toolTxts[selected2]);
                mapMesh.genereate();
            }
        }
        // Update is called once per frame
        private void OnGUI()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), mapMesh.renderTexture, ScaleMode.StretchToFill, false);

            if (activeUI) clientRect = GUI.Window(10086, clientRect, WindowFunction, "地图参数");

            string txt = $"{Input.mousePosition}\n";
            var tt = mapMesh.elapsedMs.ToArray();
            for (int i = 0; i < tt.Length; ++i)
            {
                txt += "" + tt[i] + "ms\n";
            }
            GUILayout.Label(txt);
        }

        private void WindowFunction(int windowId)
        {
            GUILayout.Label("笔刷大小");
            selected1 = GUILayout.Toolbar(selected1, sizeTxts);
            GUILayout.Label("地形");
            selected2 = GUILayout.Toolbar(selected2, toolTxts);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            renderUU("elevationParams", elevationParams);
            renderUU("biomesParams", biomesParams);
            renderUU("riversParams", riversParams);
            renderUU("renderParams", renderParams);

            GUILayout.EndScrollView();

            if (Input.GetKey(KeyCode.LeftControl)) GUI.DragWindow();
        }

        private void renderUU(string name, Dictionary<string, float[]> dict)
        {
            GUILayout.Label(name, new GUIStyle()
            {
                fontSize = 16,
                normal = new GUIStyleState()
                {
                    textColor = Color.red
                }
            });

            int i = 0;
            foreach (var k in dict)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();

                GUILayout.Label(k.Key);
                float val = GUILayout.HorizontalSlider(k.Value[0], k.Value[1], k.Value[2]);
                if (!k.Value[0].Equals(val))
                {
                    k.Value[0] = val;

                    var cam = mapMesh.mainCamera;
                    switch (i)
                    {
                        case 0:
                            cam.orthographicSize = val;
                            break;
                        case 1:
                        case 2:
                            var pos = cam.transform.localPosition;
                            if (i == 1) pos.x = val; else pos.y = val;
                            cam.transform.localPosition = pos;
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
                    mapMesh.render();
                }
                GUILayout.Label(k.Value[0].ToString());

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                i++;
            }
        }
    }
}