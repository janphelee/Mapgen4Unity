using Assets.MapUtil;
using System.IO;
using UnityEngine;

namespace Assets.MapGen
{
    class MapMesh : MonoBehaviour
    {
        private Shader[] shaders { get; set; }
        private Texture texture { get; set; }

        [SerializeField]
        private Camera rtCamera = null;

        [SerializeField] private RenderTexture rt_WaterColor = null;
        [SerializeField] private RenderTexture rt_LandColor = null;

        [SerializeField]
        private Texture2D riverBitmap = null;

        private MeshRenderer[] waters { get; set; }
        private MeshRenderer[] lands { get; set; }

        private void Awake()
        {
            shaders = new Shader[]
            {
                Shader.Find("Custom/VertexColorsOnly"),
                Shader.Find("Custom/VertexLandOnly"),
                Shader.Find("Custom/VertexWaterOnly"),
            };
        }

        private void LateUpdate()
        {
            var rt = rtCamera.targetTexture;

            foreach (var r in waters) r.gameObject.SetActive(true);
            foreach (var r in lands) r.gameObject.SetActive(false);
            rtCamera.targetTexture = rt_WaterColor;
            rtCamera.RenderWithShader(shaders[2], string.Empty);

            foreach (var r in waters) r.gameObject.SetActive(false);
            foreach (var r in lands) r.gameObject.SetActive(true);
            rtCamera.targetTexture = rt_LandColor;
            rtCamera.RenderWithShader(shaders[1], string.Empty);

            rtCamera.targetTexture = rt;
        }

        public void setTexture(string name, Texture value)
        {
            if (lands == null) return;
            foreach (var r in lands) r.material.SetTexture(name, value);
        }
        public void SetFloat(string name, float value)
        {
            if (lands == null) return;
            foreach (var r in lands) r.material.SetFloat(name, value);
        }

        public void setup(MeshData mesh, int[] peaks_t, float spacing, int mountain_height = 50)
        {
            var painting = new MapPainting();
            painting.setElevationParam(/*int seed = 187, float island = 0.5f*/);

            var mapData = new MapData(mesh, peaks_t, spacing);
            mapData.assignElevation(painting);//海拔地势
            mapData.assignRainfall();//风带植被
            mapData.assignRivers();//河流

            {
                int[] triangles;
                Vector3[] vertices;
                Vector2[] uvs;
                mapData.setRiverTextures(out vertices, out uvs, out triangles);
                Debug.Log($"setRiverTextures triangles:{vertices.Length / 3}");

                var meshs = MeshSplit.splitMesh(vertices, triangles, uvs, "river mesh");
                waters = MeshSplit.createMeshRender(meshs, this.transform, shaders[2], "river");
                // riverBitmap要开启mipmaps,且FilterMode.Trilinear
                foreach (var r in waters) r.material.SetTexture("_rivertexturemap", riverBitmap);
            }

            {
                var triangles = new int[3 * mesh.numSolidSides];
                var vertices = new Vector3[mesh.numRegions + mesh.numTriangles];
                var uvs = new Vector2[mesh.numRegions + mesh.numTriangles];
                mapData.setGeometry(vertices, uvs, triangles);

                var meshs = MeshSplit.splitMesh(vertices, triangles, uvs);
                lands = MeshSplit.createMeshRender(meshs, this.transform, shaders[0], "map");
                setTexture("_vertex_water", rt_WaterColor);
            }

            texture = ColorMap.texture();

            setTexture("_ColorMap", texture);
            setTexture("_vertex_land", rt_LandColor);
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
