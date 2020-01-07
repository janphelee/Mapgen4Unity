using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Phevolution
{
    public class BezierCurve
    {
        // A quadratic Beziér curve.
        // B(t) = (1 - t)2 P0 + 2 (1 - t) t P1 + t2 P2.
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * p0 +
                2f * oneMinusT * t * p1 +
                t * t * p2;
        }
        // B'(t) = 2 (1 - t) (P1 - P0) + 2 t (P2 - P1).
        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return
                2f * (1f - t) * (p1 - p0) +
                2f * t * (p2 - p1);
        }
        // A cubic Beziér curves
        // B(t) = (1 - t)3 P0 + 3 (1 - t)2 t P1 + 3 (1 - t) t2 P2 + t3 P3.
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * oneMinusT * p0 +
                3f * oneMinusT * oneMinusT * t * p1 +
                3f * oneMinusT * t * t * p2 +
                t * t * t * p3;
        }
        // B'(t) = 3 (1 - t)2 (P1 - P0) + 6 (1 - t) t (P2 - P1) + 3 t2 (P3 - P2)
        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                3f * oneMinusT * oneMinusT * (p1 - p0) +
                6f * oneMinusT * t * (p2 - p1) +
                3f * t * t * (p3 - p2);
        }

        //舍弃距离太近的点
        public static Vector3[] duplicate(Vector3[] points)
        {
            var valid = new List<Vector3>();
            for (var i = 0; i < points.Length; ++i)
            {
                var idx1 = (i + 1) % points.Length;
                if (Vector3.Distance(points[i], points[idx1]) > 0.01f)
                    valid.Add(points[i]);
            }
            return valid.ToArray();
        }

        public static Vector3[] bezier(Vector3[] points, float stepDeg = 5f)
        {
            var pp = new List<Vector3>();

            points = duplicate(points);

            var length = points.Length;

            float outMod = 0;
            Vector3[] outPoints;
            for (var idx1 = 0; idx1 < length; ++idx1)
            {
                var idx0 = (idx1 - 1 + length) % length;
                var idx2 = (idx1 + 1) % length;
                var p0 = points[idx0];
                var p1 = points[idx1];
                var p2 = points[idx2];
                p0 = (p0 + p1) * 0.5f;
                p2 = (p2 + p1) * 0.5f;

                var ret = bezier(p0, p1, p2, stepDeg, outMod, out outPoints, out outMod);
                if (ret > 0) pp.AddRange(outPoints);
            }
            return pp.ToArray();
        }

        public static int bezier(Vector3 p0, Vector3 p1, Vector3 p2, float stepDeg, float modDeg, out Vector3[] outPoints, out float outMod)
        {
            var v1 = p0 - p1; v1.z = 0; v1.Normalize();
            var v2 = p2 - p1; v2.z = 0; v2.Normalize();
            var aa = Utils.angle(new Vector2(v1.x, v1.y), new Vector2(v2.x, v2.y));
            if (aa > 180 || aa < 0) Debug.LogError($"bezier aa={aa}");

            if (aa == 180)
            {
                outMod = modDeg;
                outPoints = null;
                return 0;
            }

            var ab = Mathf.Abs(180 - aa);

            var pp = new List<Vector3>();

            var step = stepDeg / ab;
            var current = -modDeg / ab;

            while (current + step < 1f)
            {
                current += step;
                pp.Add(GetPoint(p0, p1, p2, current));
            }
            outPoints = pp.ToArray();
            outMod = ab * (1 - current);

            return pp.Count;
        }

        public static void iiii(Vector3[] points, int start, List<int> triangles)
        {
            var coords = new double[points.Length * 2];
            //var colors = new Color[points.Length];
            for (var i = 0; i < points.Length; ++i)
            {
                coords[i * 2 + 0] = points[i].x;
                coords[i * 2 + 1] = points[i].y;
                //colors[i] = new Color(rd(), rd(), rd(), 0.382f);
            }
            var delauny = new Delaunator(coords);
            var dts = delauny.triangles;
            var n = dts.Length / 3;
            for (var i = 0; i < n; ++i)
            {
                var tt = new List<int>();
                tt.Add(dts[i * 3 + 0]);
                tt.Add(dts[i * 3 + 1]);
                tt.Add(dts[i * 3 + 2]);

                var st = tt.ToArray();
                Array.Sort(st);

                var a = points[st[0]];
                var b = points[st[1]];
                var c = points[st[2]];

                var cross = Vector3.Cross(b - a, c - a);
                if (cross.z > 0) triangles.AddRange(tt.Select(t => t + start));
            }
        }


    }
}