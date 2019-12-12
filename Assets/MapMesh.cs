using Assets.DualMesh;
using Assets.Map;
using Assets.MapUtil;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Assets
{
    class MapMesh : MonoBehaviour
    {
        [SerializeField] private Camera rtCamera = null;
        [SerializeField] private RenderTexture rt_WaterColor = null;
        [SerializeField] private RenderTexture rt_LandColor = null;
        [SerializeField] private Texture2D riverBitmap = null;
        [SerializeField] private Texture2D colorBitmap = null;

        private Shader[] shaders { get; set; }
        public MeshSplit waters { get; private set; }
        public MeshSplit landzs { get; private set; }

        public MapPainting painting { get; private set; }
        private MapData mapData { get; set; }
        private MapWorker worker { get; set; }
        private bool needRender { get; set; }
        private GameObject tmpObj { get; set; }
        private Camera mainCamera { get; set; }


        private void Awake()
        {
            shaders = new Shader[]
            {
                Shader.Find("Custom/VertexColorsOnly"),
                Shader.Find("Custom/VertexLandOnly"),
                Shader.Find("Custom/VertexWaterOnly"),
            };
            waters = MeshSplit.createMesh(transform, "river mesh");
            landzs = MeshSplit.createMesh(transform, "map mesh");

            tmpObj = new GameObject("__worldToLandPosition");
            mainCamera = Camera.main;
            painting = new MapPainting();
        }

        private void LateUpdate()
        {
            if (!needRender) return;
            needRender = !needRender;

            worker.getBuffer((c, bz) =>
            {
                var v31 = bz[0].vertices;
                var v21 = bz[0].uv;
                var t31 = bz[0].triangles;
                waters.setup(v31, t31, v21, shaders[2]);
                // riverBitmap要开启mipmaps,且FilterMode.Trilinear
                waters.setTexture("_rivertexturemap", riverBitmap);

                var v32 = bz[1].vertices;
                var v22 = bz[1].uv;
                var t32 = bz[1].triangles;
                landzs.setup(v32, t32, v22, shaders[0]);
                landzs.setTexture("_vertex_water", rt_WaterColor);
                //texture = ColorMap.texture();
                //saveToPng(texture as Texture2D, Application.streamingAssetsPath + "/colormap.png");
                landzs.setTexture("_ColorMap", colorBitmap);
                landzs.setTexture("_vertex_land", rt_LandColor);
            });

            var rt = rtCamera.targetTexture;

            waters.gameObject.SetActive(true);
            landzs.gameObject.SetActive(false);

            rtCamera.targetTexture = rt_WaterColor;
            rtCamera.RenderWithShader(shaders[2], string.Empty);

            waters.gameObject.SetActive(false);
            landzs.gameObject.SetActive(true);

            rtCamera.targetTexture = rt_LandColor;
            rtCamera.RenderWithShader(shaders[1], string.Empty);

            rtCamera.targetTexture = rt;
        }

        public Vector3 getHitPosition()
        {
            if (!mainCamera)
            {
                Debug.Log($"getHitPosition zero 111");
                return Vector3.zero;
            }
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, mainCamera.farClipPlane))
            {
                return worldToLandPosition(hit.point);
            }
            Debug.Log($"getHitPosition zero 222");
            return Vector3.zero;
        }
        public Vector3 worldToLandPosition(Vector3 p)
        {
            tmpObj.transform.SetParent(null);
            tmpObj.transform.position = p;
            tmpObj.transform.SetParent(transform, true);

            var sp = tmpObj.transform.localPosition / 1000f;
            //Debug.Log($"worldToLandPosition {tmpObj.transform.localPosition} {sp.x},{sp.y}");
            return sp;
        }

        public readonly List<int> editTicks = new List<int>();
        public void setup(MeshData mesh, int[] peaks_t, float spacing, int mountain_height = 50)
        {
            mapData = new MapData(mesh, peaks_t, spacing);
            worker = new MapWorker(painting, mapData, i =>
            {
                editTicks.Add(i);
                if (editTicks.Count > 5) editTicks.RemoveAt(0);
                needRender = true;
            });
            redraw();
        }

        public void redraw()
        {
            if (worker != null) worker.start();
        }

        public void readTargetTexture(Camera camera, Texture2D output)
        {
            var currentRT = RenderTexture.active;
            RenderTexture.active = camera.targetTexture;
            output.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            output.Apply();

            RenderTexture.active = currentRT;
        }

        // Take a "screenshot" of a camera's Render Texture.
        public Texture2D renderTargetImage(Camera camera, Shader shader, string tag = "", FilterMode filterMode = FilterMode.Point, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            // The Render Texture in RenderTexture.active is the one
            // that will be read by ReadPixels.
            var currentRT = RenderTexture.active;
            RenderTexture.active = camera.targetTexture;

            // Render the camera's view.
            camera.RenderWithShader(shader, tag);

            // Make a new texture and read the active Render Texture into it.
            Texture2D image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGBA32, false, false);
            image.filterMode = filterMode;
            image.wrapMode = wrapMode;
            image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            image.Apply();

            // Replace the original active Render Texture.
            RenderTexture.active = currentRT;
            return image;
        }

        public void saveToPng(Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            FileStream file = File.Open(path, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(file);
            writer.Write(bytes);
            file.Close();

            Debug.Log($"saveTo path:{path}");
        }
    }
}
