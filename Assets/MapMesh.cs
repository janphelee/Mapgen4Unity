using Assets.MapUtil;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.MapGen
{
    class MapMesh : MonoBehaviour
    {
        private Shader[] shaders { get; set; }
        private Texture texture { get; set; }

        private List<MeshRenderer> renderers = new List<MeshRenderer>();
        [SerializeField]
        private Camera rtCamera = null;
        [SerializeField]
        private Texture2D landTexture;
        [SerializeField]
        private Texture2D waterTexture;
        [SerializeField]
        private Texture2D riverBitmap = null;


        private void Awake()
        {
            shaders = new Shader[]
            {
                Shader.Find("Custom/VertexColorsOnly"),
                Shader.Find("Custom/VertexLandOnly"),
                Shader.Find("Custom/VertexWaterOnly"),
            };
        }

        public int camW;
        public int camH;
        public float aspect;

        private void LateUpdate()
        {
            if (Camera.current)
            {
                if (camW != Camera.current.pixelWidth || camH != Camera.current.pixelHeight)
                {
                    camW = Camera.current.pixelWidth;
                    camH = Camera.current.pixelHeight;
                    aspect = Camera.current.aspect;
                    //depthTexture = new RenderTexture(2048, 2048 * camH / camW, 16);
                    ////depthTexture.filterMode = FilterMode.Point;
                    //depthTexture.wrapMode = TextureWrapMode.Clamp;

                    //setTexture("_vertex_depth", depthTexture);
                }
                //var targetTexture = rtCamera.targetTexture;
                rtCamera.CopyFrom(Camera.current);
                //rtCamera.targetTexture = targetTexture;
                //rtCamera.RenderWithShader(shaders[2], "");

                ////readTargetTexture(rtCamera, depthTexture);
            }

        }

        public void setTexture(string name, Texture value)
        {
            foreach (var r in renderers) r.material.SetTexture(name, value);
        }
        public void SetFloat(string name, float value)
        {
            foreach (var r in renderers) r.material.SetFloat(name, value);
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

                var meshs = MeshSplit.splitMesh(vertices, triangles, uvs, "river mesh");
                var ret = MeshSplit.createMeshRender(meshs, this.transform, shaders[2], "river");

                foreach (var r in ret) r.material.SetTexture("_rivertexturemap", riverBitmap);
                waterTexture = renderTargetImage(rtCamera, shaders[2], string.Empty, FilterMode.Bilinear, TextureWrapMode.Clamp);
                foreach (var r in ret) r.gameObject.SetActive(false);
            }

            {
                var triangles = new int[3 * mesh.numSolidSides];
                var vertices = new Vector3[mesh.numRegions + mesh.numTriangles];
                var uvs = new Vector2[mesh.numRegions + mesh.numTriangles];
                mapData.setGeometry(vertices, uvs, triangles);

                var meshs = MeshSplit.splitMesh(vertices, triangles, uvs);
                var ret = MeshSplit.createMeshRender(meshs, this.transform, shaders[0], "map");

                renderers.AddRange(ret);
                setTexture("_vertex_water", waterTexture);

                landTexture = renderTargetImage(rtCamera, shaders[1], string.Empty);
            }

            texture = ColorMap.texture();

            setTexture("_ColorMap", texture);
            setTexture("_vertex_land", landTexture);
            //setTexture("_vertex_water", waterTexture);
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
