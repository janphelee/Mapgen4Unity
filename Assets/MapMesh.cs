using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.MapGen
{
    class MapMesh : MonoBehaviour
    {
        private Shader[] shaders { get; set; }
        private Texture texture { get; set; }

        private List<MeshRenderer> renderers = new List<MeshRenderer>();
        [SerializeField]
        private Camera rtCamera;
        [SerializeField]
        private Texture2D landTexture;

        [SerializeField]
        private RenderTexture depthTexture;

        private void Awake()
        {
            shaders = new Shader[]
            {
                Shader.Find("Custom/VertexColorsOnly"),
                Shader.Find("Custom/VertexLandOnly"),
                Shader.Find("Custom/VertexDepthOnly"),
            };
        }

        private int camW { get; set; }
        private int camH { get; set; }

        private void LateUpdate()
        {
            if (Camera.current)
            {
                if (camW != Camera.current.pixelWidth || camH != Camera.current.pixelHeight)
                {
                    camW = Camera.current.pixelWidth;
                    camH = Camera.current.pixelHeight;
                    depthTexture = new RenderTexture(2048, 2048 * camH / camW, 16);
                    //depthTexture.filterMode = FilterMode.Point;
                    depthTexture.wrapMode = TextureWrapMode.Clamp;

                    setTexture("_vertex_depth", depthTexture);
                }
                //var targetTexture = rtCamera.targetTexture;
                rtCamera.CopyFrom(Camera.current);
                rtCamera.targetTexture = depthTexture;
                rtCamera.RenderWithShader(shaders[2], "");

                //readTargetTexture(rtCamera, depthTexture);
            }

        }

        private void splitMesh(Vector3[] vertices, int[] triangles, Vector2[] uv)
        {
            int start = 0;

            var dic = new Dictionary<int, int>();
            var k1 = new List<int>();
            var v3 = new List<Vector3>();
            var v2 = new List<Vector2>();
            for (int i = 0; i < triangles.Length; ++i)
            {
                var v = triangles[i];
                if (!dic.ContainsKey(v))
                {
                    dic[v] = v3.Count;
                    k1.Add(v3.Count);
                    v3.Add(vertices[v]);
                    v2.Add(uv[v]);
                }
                else
                {
                    k1.Add(dic[v]);
                }

                // 检查超过顶点数量
                if (v3.Count >= short.MaxValue && i % 3 == 2)
                {
                    // 为了保持跟js 绘制同步，调转三角面绘制顺序
                    k1.Reverse();
                    createMesh(v3.ToArray(), k1.ToArray(), v2.ToArray(), $"map mesh {start}-{i}");

                    start = i + 1;
                    dic.Clear();
                    k1.Clear();
                    v3.Clear();
                    v2.Clear();
                }
                if (i == triangles.Length - 1)
                {
                    k1.Reverse();
                    createMesh(v3.ToArray(), k1.ToArray(), v2.ToArray(), $"map mesh {start}-{i}");
                    break;
                }
            }
        }

        private void createMesh(Vector3[] vertices, int[] triangles, Vector2[] uv = null, string name = "map mesh")
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(this.transform, true);

            var filter = obj.AddComponent<MeshFilter>();
            var mesh = filter.mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;

            var render = obj.AddComponent<MeshRenderer>();
            // render.material = new Material(Shader.Find("Custom/VertexColors"));
            render.material = new Material(Shader.Find("Custom/VertexColorsOnly"));
            render.receiveShadows = false;
            render.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            renderers.Add(render);
        }

        private void setTexture(string name, Texture value)
        {
            foreach (var r in renderers) r.material.SetTexture(name, value);
        }

        public void setup(MeshData mesh, int[] peaks_t, float spacing, int mountain_height = 50)
        {
            var painting = new MapPainting();
            painting.setElevationParam(/*int seed = 187, float island = 0.5f*/);

            var mapData = new MapData(mesh, peaks_t, spacing);
            mapData.assignElevation(painting);//海拔地势
            mapData.assignRainfall();//风带植被
            mapData.assignRivers();//河流

            var vertices = new Vector3[mesh.numRegions + mesh.numTriangles];
            var triangles = new int[3 * mesh.numSolidSides];
            var emUV = new Vector2[mesh.numRegions + mesh.numTriangles];
            mapData.setGeometry(vertices, emUV, triangles);


            splitMesh(vertices, triangles, emUV);

            texture = ColorMap.texture();
            landTexture = renderTargetImage(rtCamera, shaders[1], string.Empty);

            setTexture("_ColorMap", texture);
            setTexture("_vertex_land", landTexture);
        }

        public void setMountainHeight(float mountain_height)
        {
            foreach (var render in renderers)
            {
                render.material.SetFloat("_MountainHeight", mountain_height);
            }
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
            Texture2D image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
            image.filterMode = filterMode;
            image.wrapMode = wrapMode;
            image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            image.Apply();

            // Replace the original active Render Texture.
            RenderTexture.active = currentRT;
            return image;
        }
    }
}
