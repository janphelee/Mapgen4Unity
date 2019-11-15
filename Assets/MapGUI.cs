using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGUI : MonoBehaviour
{
    /// <summary>主窗口尺寸</summary>
    private Rect mWindowRect = new Rect(0f, 0f, 100f, 400f);

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    private void OnGUI()
    {
        mWindowRect = GUI.Window(10086, mWindowRect, WindowFunction, $"测试一下窗口 {mWindowRect.x},{mWindowRect.y}");
    }

    private void WindowFunction(int windowId)
    {
        GUI.DragWindow();
    }
}
