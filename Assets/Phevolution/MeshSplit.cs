using System.Collections.Generic;
using UnityEngine;

namespace Assets
{
    class MeshSplit : MonoBehaviour
    {
        private static Mesh createMesh(Vector3[] vertices, int[] triangles, Vector2[] uv = null, string name = "map_mesh")
        {
            var mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;

            return mesh;
        }
        public static Mesh[] splitMesh(Vector3[] vertices, int[] triangles, Vector2[] uvs, string prefix = "map_mesh")
        {
            var meshs = new List<Mesh>();
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
                    v2.Add(uvs[v]);
                }
                else
                {
                    k1.Add(dic[v]);
                }

                // 检查超过顶点数量
                if (v3.Count >= ushort.MaxValue && i % 3 == 2)
                {
                    // 为了保持跟js 绘制同步，调转三角面绘制顺序
                    meshs.Add(createMesh(v3.ToArray(), k1.ToArray(), v2.ToArray(), $"{prefix}_{start}-{i}"));

                    start = i + 1;
                    dic.Clear();
                    k1.Clear();
                    v3.Clear();
                    v2.Clear();
                }
                if (i == triangles.Length - 1)
                {
                    meshs.Add(createMesh(v3.ToArray(), k1.ToArray(), v2.ToArray(), $"{prefix} {start}-{i}"));
                    break;
                }
            }
            return meshs.ToArray();
        }

        public static MeshRenderer[] createMeshRender(Mesh[] meshs, Transform parent, Shader shader, string tag = "")
        {
            var renderList = new List<MeshRenderer>();
            foreach (var mesh in meshs)
            {
                var obj = new GameObject(mesh.name);
                obj.transform.SetParent(parent, true);
                //if (!string.IsNullOrEmpty(tag)) obj.tag = tag;

                var filter = obj.AddComponent<MeshFilter>();
                filter.mesh = mesh;

                var render = obj.AddComponent<MeshRenderer>();
                // render.material = new Material(Shader.Find("Custom/VertexColors"));
                render.material = new Material(shader);
                render.receiveShadows = false;
                render.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

                renderList.Add(render);
            }
            return renderList.ToArray();
        }
        public static MeshSplit createMesh(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var s = obj.AddComponent<MeshSplit>();
            return s;
        }

        private Mesh[] meshes { get; set; }
        private MeshRenderer[] renderers { get; set; }

        private void Awake()
        {
            meshes = new Mesh[0];
            renderers = new MeshRenderer[0];
        }

        public void setup(Vector3[] vertices, Vector2[] uv, int[] triangles, Shader shader)
        {
            var vk1 = new List<int[]>();
            var vv3 = new List<Vector3[]>();
            var vv2 = new List<Vector2[]>();

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
                    //if (v >= vertices.Length) Debug.Log("v >= vertices.Length");
                    dic[v] = v3.Count;
                    v3.Add(vertices[v]);
                    v2.Add(uv[v]);
                }
                k1.Add(dic[v]);

                // 检查超过顶点数量
                if (v3.Count >= ushort.MaxValue && i % 3 == 2)
                {
                    vk1.Add(k1.ToArray());
                    vv3.Add(v3.ToArray());
                    vv2.Add(v2.ToArray());

                    start = i + 1;
                    dic.Clear();
                    k1.Clear();
                    v3.Clear();
                    v2.Clear();
                }
                if (i == triangles.Length - 1)
                {
                    vk1.Add(k1.ToArray());
                    vv3.Add(v3.ToArray());
                    vv2.Add(v2.ToArray());
                    break;
                }
            }

            if (meshes.Length != vk1.Count)
            {
                var vm = new List<Mesh>(meshes);
                var vr = new List<MeshRenderer>(renderers);
                while (vr.Count > vk1.Count)
                {
                    var idx = vr.Count - 1;
                    var r = vr[idx];
                    vr.RemoveAt(idx);
                    vm.RemoveAt(idx);
                    r.gameObject.SetActive(false);
                    Destroy(r.gameObject);
                }
                while (vr.Count < vk1.Count)
                {
                    var mesh = new Mesh();
                    mesh.name = string.Format("{0}-{1}", gameObject.name, vr.Count);
                    vm.Add(mesh);

                    var obj = new GameObject(mesh.name);
                    obj.transform.SetParent(transform, false);
                    //if (!string.IsNullOrEmpty(tag)) obj.tag = tag;

                    var filter = obj.AddComponent<MeshFilter>();
                    filter.mesh = mesh;

                    var render = obj.AddComponent<MeshRenderer>();
                    render.material = new Material(shader);
                    render.receiveShadows = false;
                    render.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

                    vr.Add(render);
                }

                this.meshes = vm.ToArray();
                this.renderers = vr.ToArray();
            }

            for (int i = 0; i < meshes.Length; ++i)
            {
                //! Mesh.vertices is too small https://blog.csdn.net/luckydogyxx/article/details/83302823
                meshes[i].Clear();
                meshes[i].vertices = vv3[i];
                meshes[i].triangles = vk1[i];
                meshes[i].uv = vv2[i];
            }
        }
        public void setShader(Shader value)
        {
            if (renderers == null) return;
            foreach (var r in renderers) r.material.shader = value;
        }
        public void setTexture(string name, Texture value)
        {
            if (renderers == null) return;
            foreach (var r in renderers) r.material.SetTexture(name, value);
        }
        public void setFloat(string name, float value)
        {
            if (renderers == null) return;
            foreach (var r in renderers) r.material.SetFloat(name, value);
        }
    }
}
