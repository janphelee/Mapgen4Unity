using Assets.DualMesh;
using Assets.Map;
using Assets.MapUtil;
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

        private MapPainting painting = new MapPainting();
        private MapData mapData { get; set; }
        private bool needRender { get; set; }


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
        }
        private void Update()
        {
        }
        private void LateUpdate()
        {
            if (!needRender) return;
            needRender = !needRender;

            painting.setElevationParam(/*int seed = 187, float island = 0.5f*/);
            mapData.assignElevation(painting);//海拔地势
            mapData.assignRainfall();//风带植被
            mapData.assignRivers();//河流

            var mesh = mapData.mesh;
            {
                int[] triangles;
                Vector3[] vertices;
                Vector2[] uvs;
                mapData.setRiverGeometry(out vertices, out uvs, out triangles);
                //Debug.Log($"setRiverTextures triangles:{vertices.Length / 3}");

                waters.setup(vertices, triangles, uvs, shaders[2]);
                // riverBitmap要开启mipmaps,且FilterMode.Trilinear
                waters.setTexture("_rivertexturemap", riverBitmap);
            }

            {
                var vertices = new Vector3[mesh.numRegions + mesh.numTriangles];
                var uvs = new Vector2[mesh.numRegions + mesh.numTriangles];
                var triangles = new int[3 * mesh.numSolidSides];
                mapData.setMeshGeometry(vertices);
                mapData.setMapGeometry(uvs, triangles);

                landzs.setup(vertices, triangles, uvs, shaders[0]);
                landzs.setTexture("_vertex_water", rt_WaterColor);
                //texture = ColorMap.texture();
                //saveToPng(texture as Texture2D, Application.streamingAssetsPath + "/colormap.png");
                landzs.setTexture("_ColorMap", colorBitmap);
                landzs.setTexture("_vertex_land", rt_LandColor);
            }

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


        public void setup(MeshData mesh, int[] peaks_t, float spacing, int mountain_height = 50)
        {
            mapData = new MapData(mesh, peaks_t, spacing);
            needRender = true;
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
