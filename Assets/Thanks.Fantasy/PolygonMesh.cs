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
    public class PolygonMesh : MonoBehaviour
    {
        private MeshFilter mf { get; set; }
        private MeshRenderer mr { get; set; }
        private Mesh mesh { get; set; }

        private void Awake()
        {
            mf = GetComponent<MeshFilter>();
            mr = GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Thanks.Fantasy/VertexColor"));
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        public void SetColor(Color color)
        {
            mr.material.SetColor("u_color", color);
        }

        public void SetPositions(Vector2[] ppp, ushort[] iii, Color color, bool flipYAxis = false, float z = 0)
        {
            mesh = new Mesh();
            mesh.vertices = ppp.Select(p => new Vector3(p.x, flipYAxis ? -p.y : p.y, -z)).ToArray();
            mesh.triangles = iii.Select(i => (int)i).ToArray();
            mf.mesh = mesh;

            SetColor(color);
        }

        public void SetPositions(Vector3[] ppp, int[] iii, Color color)
        {
            mesh = new Mesh();
            mesh.vertices = ppp;
            mesh.triangles = iii;
            mf.mesh = mesh;

            SetColor(color);
        }

        public void SetPositions(Vector3[] points)
        {
            var ii = new List<int>();
            BezierCurve.iiii(points, 0, ii);

            var rd = Rander.makeRandFloat(Random.Next());
            SetPositions(points, ii.ToArray(), new Color(rd(), rd(), rd(), rd()));

            //var render = gameObject.AddComponent<LineRenderer>();
            //render.positionCount = points.Length;
            //render.SetPositions(points);
            //render.startWidth = 0.5f;
            //render.endWidth = 0.5f;
            //render.loop = true;
        }


        public void SetPositions(List<Vector3[]> points, Color color)
        {
            var ii = new List<int>();
            var pp = new List<Vector3>();
            for (var i = 0; i < points.Count; ++i)
            {
                BezierCurve.iiii(points[i], pp.Count, ii);
                pp.AddRange(points[i]);
            }

            SetPositions(pp.ToArray(), ii.ToArray(), color);
        }


    }
}
