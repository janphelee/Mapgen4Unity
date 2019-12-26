using Assets.MapJobs;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

#if Use_Double_Float
using Float = System.Double;
#else
using Float = System.Single;
#endif

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

        private _MapJobs mapJobs { get; set; }
        public MapPaint painting { get; private set; }

        private bool needRender { get; set; }
        private GameObject tmpObj { get; set; }
        public Camera mainCamera { get; private set; }

        public RenderTexture renderTexture { get; private set; }

        public _MapJobs.Config config { get; set; }


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

            renderTexture = new RenderTexture(Screen.width, Screen.height, 16);//要开启depth，不然会穿透
            renderTexture.filterMode = FilterMode.Point;
            //renderTexture.format = RenderTextureFormat.ARGB32;

            mainCamera = Camera.main;
            mainCamera.targetTexture = renderTexture;
            mainCamera.gameObject.SetActive(false);

        }

        private void OnDestroy()
        {
            mapJobs.Dispose();
        }

        private void LateUpdate()
        {
            if (!needRender) return;
            needRender = !needRender;

            {
                var count = mapJobs.riverCount;
                var v31 = new Vector3[count * 3];
                System.Array.Copy(mapJobs.rivers_v3.ToArray(), v31, v31.Length);
                var v21 = new Vector2[count * 3];
                System.Array.Copy(mapJobs.rivers_uv.ToArray(), v21, v21.Length);
                var t31 = new int[count * 3];
                //索引逆序
                for (int i = 0; i < t31.Length; ++i) t31[i] = t31.Length - 1 - i;
                waters.setup(v31, t31, v21, shaders[2]);
                // riverBitmap要开启mipmaps,且FilterMode.Trilinear
                waters.setTexture("_rivertexturemap", riverBitmap);

                var v32 = mapJobs.land_v3.ToArray();
                var v22 = mapJobs.land_uv.ToArray();
                var t32 = mapJobs.land_i.ToArray();
                landzs.setup(v32, t32, v22, shaders[0]);
                //texture = ColorMap.texture();
                //saveToPng(texture as Texture2D, Application.streamingAssetsPath + "/colormap.png");
                landzs.setTexture("_ColorMap", colorBitmap);
                landzs.setTexture("_vertex_land", rt_LandColor);
                landzs.setTexture("_vertex_water", rt_WaterColor);
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

            renderColor();

        }

        public void renderLand()
        {
            var rt = rtCamera.targetTexture;

            rtCamera.targetTexture = rt_LandColor;
            rtCamera.RenderWithShader(shaders[1], string.Empty);

            rtCamera.targetTexture = rt;
        }

        public void renderColor()
        {
            mainCamera.Render();
        }

        public Vector3 getHitPosition()
        {
            if (!mainCamera)
            {
                return Vector3.zero;
            }
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, mainCamera.farClipPlane))
            {
                return worldToLandPosition(hit.point);
            }
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

        public void setup(int seed, MeshBuilder.Graph graph, short[] peaks_index, Float spacing)
        {
            var g2 = new DualMesh.Graph()
            {
                numBoundaryRegions = graph.numBoundaryRegions,
                numSolidSides = graph.numSolidSides,
                _halfedges = graph._halfedges,
                _r_vertex = graph._r_vertex.ToArray(),
                _triangles = graph._triangles,
            };
            mapJobs = new _MapJobs(g2, peaks_index, spacing);
            painting = new MapPaint(mapJobs.elevation);

            var cfg = mapJobs.config;
            cfg.seed = seed;
            cfg.spacing = spacing;
            cfg.island = 0.5f;
            cfg.mountain_jagged = 0;
            cfg.noisy_coastlines = 0.01f;
            cfg.hill_height = 0.02f;
            cfg.ocean_depth = 1.5f;
            cfg.wind_angle_deg = 0;
            cfg.raininess = 0.9f;
            cfg.rain_shadow = 0.5f;
            cfg.evaporation = 0.5f;
            cfg.lg_min_flow = 2.7f;
            cfg.lg_river_width = -2.7f;
            cfg.flow = 0.2f;

            setConfig(cfg);
        }

        public readonly List<int> elapsedMs = new List<int>();
        private void onCallback(long elapsed)
        {
            elapsedMs.Add((int)elapsed);
            if (elapsedMs.Count > 5) elapsedMs.RemoveAt(0);
            needRender = true;
        }

        private bool working = false;
        private bool requestJob = false;
        public void genereate()
        {
            if (working)
            {
                //Debug.Log("busying, requestJob = true");
                requestJob = true;
                return;
            }
            requestJob = false;
            working = true;
            mapJobs.processAsync(config, t =>
            {
                onCallback(t);
                working = false;
                if (requestJob) genereate();
            });
        }

        public void setConfig(_MapJobs.Config cfg)
        {
            config = cfg;
            genereate();
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
