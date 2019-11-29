using Assets.MapGen;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    //    ['zoom', 100 / 480, 100 / 1000, 100 / 50],
    //    ['x', 500, 0, 1000],
    //    ['y', 500, 0, 1000],
    //    ['light_angle_deg', 80, 0, 360],
    //    ['slope', 2, 0, 5],
    //    ['flat', 2.5, 0, 5],
    //    ['ambient', 0.25, 0, 1],
    //    ['overhead', 30, 0, 60],
    //    ['tilt_deg', 0, 0, 90],
    //    ['rotate_deg', 0, -180, 180],
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
        { "outline_depth",new float[]{1,0,2} },
        { "outline_strength",new float[]{15,0,30} },
        { "outline_threshold",new float[]{0,0,100} },
    };

    /// <summary>主窗口尺寸</summary>
    private Rect mWindowRect = new Rect(0f, 0f, 200f, 400f);

    private MapMesh mapMesh { get; set; }
    private bool activeUI { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        mapMesh = GetComponent<MapMesh>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H)) activeUI = !activeUI;
    }
    // Update is called once per frame
    private void OnGUI()
    {
        if (activeUI) mWindowRect = GUI.Window(10086, mWindowRect, WindowFunction, "地图参数");
    }

    private void WindowFunction(int windowId)
    {
        foreach (var k in mapParams)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            GUILayout.Label(k.Key);
            float old = k.Value[0];
            k.Value[0] = GUILayout.HorizontalSlider(k.Value[0], k.Value[1], k.Value[2]);
            if (!old.Equals(k.Value[0]))
            {
                mapMesh.SetFloat($"_{k.Key}", k.Value[0]);
            }
            GUILayout.Label(k.Value[0].ToString());

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        //GUI.DragWindow();
    }
}
