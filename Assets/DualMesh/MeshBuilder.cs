﻿using DelaunatorSharp;
using DelaunatorSharp.Interfaces;
using DelaunatorSharp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.MapGen
{
    /**
     * Build a dual mesh from points, with ghost triangles around the exterior.
     *
     * The builder assumes 0 ≤ x < 1000, 0 ≤ y < 1000
     *
     * Options:
     *   - To have equally spaced points added around the 1000x1000 boundary,
     *     pass in boundarySpacing > 0 with the spacing value. If using Poisson
     *     disc points, I recommend 1.5 times the spacing used for Poisson disc.
     *
     * Phases:
     *   - Your own set of points
     *   - Poisson disc points
     *
     * The mesh generator runs some sanity checks but does not correct the
     * generated points.
     *
     * Examples:
     *
     * Build a mesh with poisson disc points and a boundary:
     *
     * new MeshBuilder({boundarySpacing: 150})
     *    .addPoisson(Poisson, 100)
     *    .create()
     */
    class MeshBuilder
    {
        public struct Graph
        {
            public List<float[]> _r_vertex { get; set; }
            public int[] _triangles { get; set; }
            public int[] _halfedges { get; set; }
            public int numBoundaryRegions { get; set; }
            public int numSolidSides { get; set; }
        }

        public static int s_next_s(int s) { return (s % 3 == 2) ? s - 2 : s + 1; }
        public static void checkPointInequality(Graph graph)
        {
            // TODO: check for collinear vertices. Around each red point P if
            // there's a point Q and R both connected to it, and the angle P→Q and
            // the angle P→R are 180° apart, then there's collinearity. This would
            // indicate an issue with point selection.
        }
        public static void checkTriangleInequality(Graph graph)
        {
            List<float[]> _r_vertex = graph._r_vertex;
            int[] _triangles = graph._triangles, _halfedges = graph._halfedges;

            // check for skinny triangles
            const int badAngleLimit = 30;
            var summary = new int[badAngleLimit];
            var count = 0;
            for (var s = 0; s < _triangles.Length; s++)
            {
                int r0 = _triangles[s],
                    r1 = _triangles[s_next_s(s)],
                    r2 = _triangles[s_next_s(s_next_s(s))];
                float[]
                    p0 = _r_vertex[r0],
                    p1 = _r_vertex[r1],
                    p2 = _r_vertex[r2];
                var d0 = new float[] { p0[0] - p1[0], p0[1] - p1[1] };
                var d2 = new float[] { p2[0] - p1[0], p2[1] - p1[1] };
                var dotProduct = d0[0] * d2[0] + d0[1] + d2[1];
                var angleDegrees = 180 / Math.PI * Math.Acos(dotProduct);
                if (angleDegrees < badAngleLimit)
                {
                    summary[(int)angleDegrees | 0]++;
                    count++;
                }
            }
            // NOTE: a much faster test would be the ratio of the inradius to
            // the circumradius, but as I'm generating these offline, I'm not
            // worried about speed right now

            // TODO: consider adding circumcenters of skinny triangles to the point set
            if (count > 0)
            {
                //Console.log('  bad angles:', summary.join(" "));
            }
        }

        public static void checkMeshConnectivity(Graph graph)
        {
            List<float[]> _r_vertex = graph._r_vertex;
            int[] _triangles = graph._triangles, _halfedges = graph._halfedges;

            // 1. make sure each side's opposite is back to itself
            // 2. make sure region-circulating starting from each side works
            var ghost_r = _r_vertex.Count - 1;
            var out_s = new List<int>();
            for (var s0 = 0; s0 < _triangles.Length; s0++)
            {
                if (_halfedges[_halfedges[s0]] != s0)
                {
                    //console.log(`FAIL _halfedges[_halfedges[${ s0}]] !== ${s0}`);
                }
                int s = s0, count = 0;
                out_s.Clear();
                do
                {
                    count++; out_s.Add(s);
                    s = s_next_s(_halfedges[s]);
                    if (count > 100 && _triangles[s0] != ghost_r)
                    {
                        //console.log(`FAIL to circulate around region with start side =${s0} from region ${_triangles[s0]} to ${_triangles[s_next_s(s0)]}, out_s=${out_s}`);
                        break;
                    }
                } while (s != s0);
            }
        }

        /*
         * Add vertices evenly along the boundary of the mesh;
         * use a slight curve so that the Delaunay triangulation
         * doesn't make long thing triangles along the boundary.
         * These points also prevent the Poisson disc generator
         * from making uneven points near the boundary.
         */
        public static List<float[]> addBoundaryPoints(float spacing, float size)
        {
            var N = (int)Math.Ceiling(size / spacing);

            var points = new List<float[]>();
            for (int i = 0; i <= N; i++)
            {
                var t = (i + 0.5f) / (N + 1);
                var w = size * t;
                var offset = (float)Math.Pow(t - 0.5f, 2f);

                //points.Add(new float[] { offset, w });
                //points.Add(new float[] { size - offset, w });

                //points.Add(new float[] { w, offset });
                //points.Add(new float[] { w, size - offset });
            }
            return points;
        }

        public static Graph addGhostStructure(Graph graph)
        {
            var _r_vertex = graph._r_vertex;
            int[] _triangles = graph._triangles, _halfedges = graph._halfedges;

            int numSolidSides = _triangles.Length;
            int ghost_r = _r_vertex.Count;

            int numUnpairedSides = 0, firstUnpairedEdge = -1;
            var r_unpaired_s = new Dictionary<int, int>(); // seed to side
            for (var s = 0; s < numSolidSides; s++)
            {
                if (_halfedges[s] == -1)
                {
                    numUnpairedSides++;
                    r_unpaired_s[_triangles[s]] = s;
                    firstUnpairedEdge = s;
                }
            }

            var r_vertex_ghost = new List<float[]>(_r_vertex);
            r_vertex_ghost.Add(new float[] { 500, 500 });

            // 代表 _r_vertex.index
            var _triangles_r = new int[numSolidSides + 3 * numUnpairedSides];
            Array.Copy(_triangles, _triangles_r, numSolidSides);

            // 代表 _triangles_r.index
            var _halfedges_s = new int[numSolidSides + 3 * numUnpairedSides];
            Array.Copy(_halfedges, _halfedges_s, numSolidSides);

            for (int i = 0, s = firstUnpairedEdge; i < numUnpairedSides; i++)
            {
                var ghost_s = numSolidSides + 3 * i;// 虚边开始的索引

                // Construct the the ghost triangle
                _triangles_r[ghost_s] = _triangles_r[s_next_s(s)];
                _triangles_r[ghost_s + 1] = _triangles_r[s];
                _triangles_r[ghost_s + 2] = ghost_r;

                // Construct a ghost side for s
                _halfedges_s[s] = ghost_s;
                _halfedges_s[ghost_s] = s;

                var k = numSolidSides + (3 * i + 4) % (3 * numUnpairedSides);
                _halfedges_s[ghost_s + 2] = k;
                _halfedges_s[k] = ghost_s + 2;

                s = r_unpaired_s[_triangles_r[s_next_s(s)]];
            }

            return new Graph
            {
                numSolidSides = numSolidSides,
                _r_vertex = r_vertex_ghost,
                _triangles = _triangles_r,
                _halfedges = _halfedges_s
            };
        }

        List<float[]> points { get; set; }
        int numBoundaryRegions { get; set; }

        /** If boundarySpacing > 0 there will be a boundary added around the 1000x1000 area */
        public MeshBuilder(float boundarySpacing = 0f)
        {
            var boundaryPoints = boundarySpacing > 0 ? addBoundaryPoints(boundarySpacing, 1000) : new List<float[]>();
            this.points = boundaryPoints;
            this.numBoundaryRegions = boundaryPoints.Count;
        }

        /** Points should be [x, y] */
        public MeshBuilder addPoints(List<float[]> newPoints)
        {
            foreach (var p in newPoints)
            {
                this.points.Add(p);
            }
            return this;
        }

        /** Points will be [x, y] */
        public IEnumerable<float[]> getNonBoundaryPoints()
        {
            //var seg =new  ArraySegment<float[]>(this.points.ToArray(), 0,0);
            return this.points.Skip(this.numBoundaryRegions);
        }

        /** (used for more advanced mixing of different mesh types) */
        public MeshBuilder clearNonBoundaryPoints()
        {
            this.points.Capacity = this.numBoundaryRegions;
            return this;
        }

        /** Pass in the constructor from the poisson-disk-sampling module */
        public MeshBuilder addPoisson(float spacing, Rander.RandFloat rng)
        {
            var generator = new PoissonDiskSampling(new int[] { 1000, 1000 }, spacing, 0, 0, rng);
            foreach (var p in this.points)
            {
                generator.addPoint(p);
            }
            this.points = generator.fill();
            return this;
        }

        /** Build and return a TriangleMesh */
        public MeshData create(bool runChecks = false)
        {
            // TODO: use Float32Array instead of this, so that we can
            // construct directly from points read in from a file
            var delaunator = new Delaunator(this.points.Select(t => (IPoint)new Point(t[0], t[1])));
            var graph = new Graph()
            {
                _r_vertex = this.points,
                _triangles = delaunator.Triangles,
                _halfedges = delaunator.Halfedges
            };

            if (runChecks)
            {
                checkPointInequality(graph);
                checkTriangleInequality(graph);
            }

            graph = addGhostStructure(graph);
            graph.numBoundaryRegions = this.numBoundaryRegions;
            if (runChecks)
            {
                checkMeshConnectivity(graph);
            }

            return new MeshData(graph);
        }
    }
}