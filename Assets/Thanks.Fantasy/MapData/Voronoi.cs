using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Thanks.Fantasy
{
    using Cells = _MapJobs.Grid.Cells;
    using Vertices = _MapJobs.Grid.Vertices;

    public class Voronoi
    {
        public Cells cells { get; private set; }
        public Vertices vertices { get; private set; }

        public Voronoi(Delaunator delaunay, List<double[]> points, int pointsN)
        {
            var triangles = delaunay.triangles;
            var numSides = triangles.Length;
            var numTriangles = numSides / 3;

            cells = new Cells()//数组大小为区域数
            {
                v = new int[pointsN][],
                c = new int[pointsN][],
                b = new bool[pointsN],
                //i = new int[numTriangles][],
            };
            vertices = new Vertices()//数组大小为三角数
            {
                p = new double[numTriangles][],
                v = new int[numTriangles][],
                c = new int[numTriangles][],
            };

            List<int> edgesAroundPoint(int start)
            {
                var result = new List<int>();
                var incoming = start;
                do
                {
                    result.Add(incoming);
                    var outgoing = nextHalfedge(incoming);
                    incoming = delaunay.halfedges[outgoing];
                } while (incoming != -1 && incoming != start && result.Count < 20);
                return result;
            }

            int[] pointsOfTriangle(int t)
            {
                return edgesOfTriangle(t).Select(s => triangles[s]).ToArray();
            }

            double[] triangleCenter(int t)
            {
                var vertices = pointsOfTriangle(t).Select(p => points[p]).ToArray();
                return circumcenter(vertices[0], vertices[1], vertices[2]);
            }

            int[] trianglesAdjacentToTriangle(int t)
            {
                var edges = edgesOfTriangle(t);
                return edges.Select((edge, index) =>
                {
                    var opposite = delaunay.halfedges[edge];
                    return triangleOfEdge(opposite);
                }).ToArray();
            }


            for (var e = 0; e < numSides; ++e)
            {
                var p = triangles[nextHalfedge(e)];
                //if (p >= pointsN) Debug.Log($"e:{e} p:{p} >= pointsN:{pointsN}");
                //if (cells.c[p] != null) Debug.Log($"e:{e} cells.c[p] != null");
                if (p < pointsN && cells.c[p] == null)
                {
                    var edges = edgesAroundPoint(e);
                    // cell: adjacent vertex
                    cells.v[p] = edges.Select(s => triangleOfEdge(s)).ToArray();
                    // cell: adjacent valid cells
                    cells.c[p] = edges.Select(s => triangles[s]).Where(c => c < pointsN).ToArray();
                    // cell: is border
                    cells.b[p] = edges.Count > cells.c[p].Length ? true : false;
                }

                var t = triangleOfEdge(e);// numSides/3
                //if (vertices.p[t] != null) Debug.Log($"e:{e} t:{t} vertices.p[t] != null");
                if (vertices.p[t] == null)
                {
                    // vertex: coordinates
                    vertices.p[t] = triangleCenter(t);
                    // vertex: adjacent vertices
                    vertices.v[t] = trianglesAdjacentToTriangle(t);
                    // vertex: adjacent cells
                    vertices.c[t] = pointsOfTriangle(t);
                }
            }
        }
        private int[] edgesOfTriangle(int t)
        {
            return new int[] { 3 * t + 0, 3 * t + 1, 3 * t + 2 };
        }
        private int triangleOfEdge(int e) { return (int)Math.Floor(e / 3.0); }
        private int nextHalfedge(int e) { return ((e % 3) == 2) ? e - 2 : e + 1; }

        private double[] circumcenter(double[] a, double[] b, double[] c)
        {
            double ad = a[0] * a[0] + a[1] * a[1],
                   bd = b[0] * b[0] + b[1] * b[1],
                   cd = c[0] * c[0] + c[1] * c[1];
            double D = 2 * (a[0] * (b[1] - c[1]) + b[0] * (c[1] - a[1]) + c[0] * (a[1] - b[1]));
            return new double[]{
              Math.Floor(1 / D * (ad * (b[1] - c[1]) + bd * (c[1] - a[1]) + cd * (a[1] - b[1]))),
              Math.Floor(1 / D * (ad * (c[0] - b[0]) + bd * (a[0] - c[0]) + cd * (b[0] - a[0])))
            };
        }
    }
}