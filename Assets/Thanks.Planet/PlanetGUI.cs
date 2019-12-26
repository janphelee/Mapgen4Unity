using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetGUI : MonoBehaviour
{
    public Transform cameraPivot;
    private Transform came { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        came = cameraPivot.GetChild(0);
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
            rot.x -= dv.y * Time.deltaTime * speedX;//相机上下
            //rot.x = Mathf.Clamp(rot.x, -80f, 80f);
            rot.y += dv.x * Time.deltaTime * speedY;//相机左右
            cameraPivot.localRotation = Quaternion.Euler(rot);
        }

        var p = came.transform.localPosition;
        p.z += Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * scrollSpeed;//相机前后
        p.z = Mathf.Clamp(p.z, -30f, -10.5f);
        came.transform.localPosition = p;
    }
}
