using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Rendering;
using Phevolution;
using System.Collections.Generic;
using System;
using Random = Phevolution.Random;

namespace Thanks.Fantasy
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PolygonRimMesh : MonoBehaviour
    {
        private MeshFilter mf { get; set; }
        private MeshRenderer mr { get; set; }
        private Mesh mesh { get; set; }

        private void Awake()
        {
            mf = GetComponent<MeshFilter>();
            mr = GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Thanks.Fantasy/VertexRimColor"));
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        public void SetColor(Color color)
        {
            mr.material.SetColor("u_color", color);
        }

        private Vector3[] pp { get; set; }
        private Vector3[] pd { get; set; }
        private void OnDrawGizmosSelected()
        {
            if (pp != null)
            {

                for (var i = 0; i < pp.Length; ++i)
                {
                    Gizmos.DrawLine(pp[i], pd[i]);
                }
            }
        }

        public void SetPositions(Vector3[] points)
        {
            //舍弃距离太近的点
            points = BezierCurve.duplicate(points);

            var rd = Rander.makeRandFloat(Random.Next());
            var coords = new double[points.Length * 2];
            //var colors = new Color[points.Length];
            for (var i = 0; i < points.Length; ++i)
            {
                coords[i * 2 + 0] = points[i].x;
                coords[i * 2 + 1] = points[i].y;
                //colors[i] = new Color(rd(), rd(), rd(), 0.382f);
            }
            var delauny = new Delaunator(coords);

            var triangle = new List<int>();
            var n = delauny.triangles.Length / 3;
            for (var i = 0; i < n; ++i)
            {
                var tt = new List<int>();
                tt.Add(delauny.triangles[i * 3 + 0]);
                tt.Add(delauny.triangles[i * 3 + 1]);
                tt.Add(delauny.triangles[i * 3 + 2]);

                var st = tt.ToArray();
                Array.Sort(st);

                var a = points[st[0]];
                var b = points[st[1]];
                var c = points[st[2]];

                var cross = Vector3.Cross(b - a, c - a);
                if (cross.z > 0) triangle.AddRange(tt);
            }

            var length = points.Length;
            var pppp = new Vector3[length * 2];
            Array.Copy(points, 0, pppp, 0, length);
            Array.Copy(points, 0, pppp, length, length);

            var nnnn = new Vector3[length * 2];
            var tttt = new List<int>();

            pp = points;
            pd = new Vector3[pp.Length];

            for (var idx0 = 0; idx0 < length; ++idx0)
            {
                var idx1 = (idx0 + 1) % length;
                var idx2 = (idx0 - 1 + length) % length;
                var p0 = points[idx0];
                var p1 = points[idx1] - p0;
                var p2 = points[idx2] - p0;

                var aa = Utils.angle(p1.x, p1.y, p2.x, p2.y);
                var normal = Quaternion.AngleAxis(aa / 2, Vector3.forward) * p1;
                pd[idx0] = pp[idx0] + normal;

                //pppp[idx0] += normal * 2f;
                //pppp[idx0 + length] -= normal * 2f;

                nnnn[idx0] = new Vector3(normal.x, normal.y, 0.682f);
                nnnn[idx0 + length] = new Vector3(-normal.x, -normal.y, 0);

                //跟我想象的逆时针顺序不一致,需要顺时针方向
                //顶点数据是逆时针方向的，三角面顺序要顺时针
                var tt = new int[] {
                    idx0, idx1, idx0 + length,
                    idx0 + length, idx1, idx1 + length
                };
                tttt.AddRange(tt);
            }
            triangle.AddRange(tttt);
            tttt = triangle;

            //DebugHelper.SaveArray($"{gameObject.name}.txt", delauny.triangles);
            mesh = new Mesh();
            mesh.vertices = pppp;
            mesh.normals = nnnn;
            mesh.triangles = tttt.ToArray();

            mf.mesh = mesh;

            SetColor(new Color(rd(), rd(), rd(), rd()));

            //var render = gameObject.AddComponent<LineRenderer>();
            //render.positionCount = points.Length;
            //render.SetPositions(points);
            //render.startWidth = 0.5f;
            //render.endWidth = 0.5f;
            //render.loop = true;
        }
    }
}
