using System;
using System.Collections.Generic;
using System.Linq;

#if Use_Double_Float
using Float = System.Double;
using Float2 = Unity.Mathematics.double2;
#else
using Float = System.Single;
using Float2 = Unity.Mathematics.float2;
#endif

namespace Assets
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
            public List<Float2> _r_vertex { get; set; }
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
            var _r_vertex = graph._r_vertex;
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
                Float2
                    p0 = _r_vertex[r0],
                    p1 = _r_vertex[r1],
                    p2 = _r_vertex[r2];
                var d0 = new Float[] { p0[0] - p1[0], p0[1] - p1[1] };
                var d2 = new Float[] { p2[0] - p1[0], p2[1] - p1[1] };
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
            var _r_vertex = graph._r_vertex;
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
        public static List<Float[]> addBoundaryPoints(Float spacing, Float size)
        {
            var N = (int)Math.Ceiling(size / spacing);

            var points = new List<Float[]>();
            for (int i = 0; i <= N; i++)
            {
                var t = (i + 0.5f) / (N + 1);
                var w = size * t;
                var offset = (Float)Math.Pow(t - 0.5f, 2f);

                points.Add(new Float[] { offset, w });
                points.Add(new Float[] { size - offset, w });

                points.Add(new Float[] { w, offset });
                points.Add(new Float[] { w, size - offset });
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

            var r_vertex_ghost = new List<Float2>(_r_vertex);
            r_vertex_ghost.Add(new Float2(500, 500));

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

        List<Float[]> points { get; set; }
        int numBoundaryRegions { get; set; }

        /** If boundarySpacing > 0 there will be a boundary added around the 1000x1000 area */
        public MeshBuilder(Float boundarySpacing = 0f)
        {
            var boundaryPoints = boundarySpacing > 0 ? addBoundaryPoints(boundarySpacing, 1000) : new List<Float[]>();
            UnityEngine.Debug.Log($"boundaryPoints:{boundaryPoints.Count} space:{boundarySpacing}");
            this.points = boundaryPoints;
            this.numBoundaryRegions = boundaryPoints.Count;
        }

        /** Points should be [x, y] */
        public MeshBuilder addPoints(List<Float[]> newPoints)
        {
            foreach (var p in newPoints)
            {
                this.points.Add(p);
            }
            return this;
        }

        /** Points will be [x, y] */
        public IEnumerable<Float[]> getNonBoundaryPoints()
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
                generator.addPoint(new Float[] { p[0], p[1] });
            }
            this.points = generator.fill();
            return this;
        }


        public static Delaunator from(List<Float[]> points)
        {
            var n = points.Count;
            var coords = new double[n * 2];

            for (var i = 0; i < n; i++)
            {
                var p = points[i];
                coords[2 * i] = defaultGetX(p);
                coords[2 * i + 1] = defaultGetY(p);
            }
            return new Delaunator(coords);
        }
        private static T defaultGetX<T>(T[] p) { return p[0]; }
        private static T defaultGetY<T>(T[] p) { return p[1]; }

        /** Build and return a TriangleMesh */
        public Graph create(bool runChecks = false)
        {
            // TODO: use Float32Array instead of this, so that we can
            // construct directly from points read in from a file
            var delaunator = from(this.points);

            UnityEngine.Debug.Log($"delaunator Triangles:{delaunator.triangles.Length} Halfedges:{delaunator.halfedges.Length}");
            var graph = new Graph()
            {
                _r_vertex = this.points.Select(t => new Float2(t[0], t[1])).ToList(),
                _triangles = delaunator.triangles,
                _halfedges = delaunator.halfedges
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

            return graph;
        }
    }
}
