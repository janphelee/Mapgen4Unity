using System.Collections.Generic;
using UnityEngine;

namespace Assets.MapUtil
{
    class MeshSplit
    {
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
                if (v3.Count >= short.MaxValue && i % 3 == 2)
                {
                    // 为了保持跟js 绘制同步，调转三角面绘制顺序
                    k1.Reverse();
                    meshs.Add(createMesh(v3.ToArray(), k1.ToArray(), v2.ToArray(), $"{prefix}_{start}-{i}"));

                    start = i + 1;
                    dic.Clear();
                    k1.Clear();
                    v3.Clear();
                    v2.Clear();
                }
                if (i == triangles.Length - 1)
                {
                    k1.Reverse();
                    meshs.Add(createMesh(v3.ToArray(), k1.ToArray(), v2.ToArray(), $"{prefix} {start}-{i}"));
                    break;
                }
            }
            return meshs.ToArray();
        }

        public static Mesh createMesh(Vector3[] vertices, int[] triangles, Vector2[] uv = null, string name = "map_mesh")
        {
            var mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;

            return mesh;
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
    }
}
