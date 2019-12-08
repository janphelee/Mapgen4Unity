using System;
using System.Collections.Generic;

namespace Assets.DualMesh
{
    class MeshData
    {
        public static int s_to_t(int s) { return (s / 3) | 0; }
        public static int s_prev_s(int s) { return (s % 3 == 0) ? s + 2 : s - 1; }
        public static int s_next_s(int s) { return (s % 3 == 2) ? s - 2 : s + 1; }

        public static MeshData fromDelaunator(List<float[]> points, Delaunator delaunator)
        {
            return new MeshData(new MeshBuilder.Graph()
            {
                numBoundaryRegions = 0,
                numSolidSides = delaunator.triangles.Length,
                _r_vertex = points,
                _triangles = delaunator.triangles,
                _halfedges = delaunator.halfedges
            });
        }

        /* Internals */
        public int[] _r_in_s { get; private set; }
        public int[] _halfedges { get; private set; }
        public int[] _triangles { get; private set; }

        public List<float[]> _r_vertex { get; private set; }
        public List<float[]> _t_vertex { get; private set; }

        public float[] s_length { get; set; }

        public int numSides { get; private set; }
        public int numSolidSides { get; private set; }
        public int numRegions { get; private set; }
        public int numSolidRegions { get; private set; }
        public int numTriangles { get; private set; }
        public int numSolidTriangles { get; private set; }
        public int numBoundaryRegions { get; private set; }

        public MeshData(MeshBuilder.Graph graph)
        {
            this.numBoundaryRegions = graph.numBoundaryRegions;
            this.numSolidSides = graph.numSolidSides;
            this._r_vertex = graph._r_vertex;
            this._triangles = graph._triangles;
            this._halfedges = graph._halfedges;

            this._t_vertex = new List<float[]>();
            this._update();
        }

        public void update(MeshBuilder.Graph graph)
        {
            this._r_vertex = graph._r_vertex;
            this._triangles = graph._triangles;
            this._halfedges = _halfedges;

            this._update();
        }

        protected void _update()
        {
            this.numSides = _triangles.Length;
            this.numRegions = _r_vertex.Count;
            this.numSolidRegions = this.numRegions - 1; // TODO: only if there are ghosts
            this.numTriangles = this.numSides / 3;
            this.numSolidTriangles = this.numSolidSides / 3;

            if (_t_vertex.Count < numTriangles)
            {
                // Extend this array to be big enough
                int numOldTriangles = _t_vertex.Count;
                int numNewTriangles = numTriangles - numOldTriangles;
                _t_vertex.AddRange(new float[numNewTriangles][]);
                for (var t = numOldTriangles; t < this.numTriangles; t++)
                {
                    _t_vertex[t] = new float[] { 0, 0 };
                }
            }

            // Construct an index for finding sides connected to a region
            this._r_in_s = new int[numRegions];
            for (var s = 0; s < _triangles.Length; s++)
            {
                var endpoint = _triangles[s_next_s(s)];
                if (this._r_in_s[endpoint] == 0 || _halfedges[s] == -1)
                {
                    this._r_in_s[endpoint] = s;
                }
            }

            // Construct triangle coordinates
            for (var s = 0; s < _triangles.Length; s += 3)
            {
                int t = s / 3;
                float[]
                    a = _r_vertex[_triangles[s]],
                    b = _r_vertex[_triangles[s + 1]],
                    c = _r_vertex[_triangles[s + 2]];
                if (this.s_ghost(s))
                {
                    // ghost triangle center is just outside the unpaired side
                    float dx = b[0] - a[0], dy = b[1] - a[1];
                    var scale = 10 / (float)Math.Sqrt(dx * dx + dy * dy); // go 10units away from side
                    _t_vertex[t][0] = 0.5f * (a[0] + b[0]) + dy * scale;
                    _t_vertex[t][1] = 0.5f * (a[1] + b[1]) - dx * scale;
                }
                else
                {
                    // solid triangle center is at the centroid
                    _t_vertex[t][0] = (a[0] + b[0] + c[0]) / 3;
                    _t_vertex[t][1] = (a[1] + b[1] + c[1]) / 3;
                }
            }
        }

        public float r_x(int r) { return this._r_vertex[r][0]; }
        public float r_y(int r) { return this._r_vertex[r][1]; }
        public float t_x(int t) { return this._t_vertex[t][0]; }
        public float t_y(int t) { return this._t_vertex[t][1]; }
        public float[] r_pos(int r) { var tmp = new float[2]; tmp[0] = this.r_x(r); tmp[1] = this.r_y(r); return tmp; }
        public float[] t_pos(int t) { var tmp = new float[2]; tmp[0] = this.t_x(t); tmp[1] = this.t_y(t); return tmp; }
        public UnityEngine.Vector2 r_v2(int r) { return new UnityEngine.Vector2(r_x(r), r_y(r)); }
        public UnityEngine.Vector2 t_v2(int t) { return new UnityEngine.Vector2(t_x(t), t_y(t)); }

        public int s_begin_r(int s) { return this._triangles[s]; }
        public int s_end_r(int s) { return this._triangles[s_next_s(s)]; }
        public int s_inner_t(int s) { return s_to_t(s); }
        public int s_outer_t(int s) { return s_to_t(this._halfedges[s]); }
        public int s_opposite_s(int s) { return this._halfedges[s]; }

        int[] t_circulate_s(int t) { var out_s = new int[3]; for (var i = 0; i < 3; i++) { out_s[i] = 3 * t + i; } return out_s; }
        int[] t_circulate_r(int t) { var out_r = new int[3]; for (var i = 0; i < 3; i++) { out_r[i] = this._triangles[3 * t + i]; } return out_r; }
        int[] t_circulate_t(int t) { var out_t = new int[3]; for (var i = 0; i < 3; i++) { out_t[i] = this.s_outer_t(3 * t + i); } return out_t; }
        int[] r_circulate_s(int r)
        {
            int s0 = this._r_in_s[r];
            int incoming = s0;
            var out_s = new List<int>();
            do
            {
                out_s.Add(this._halfedges[incoming]);
                var outgoing = s_next_s(incoming);
                incoming = this._halfedges[outgoing];
            } while (incoming != -1 && incoming != s0);
            return out_s.ToArray();
        }

        int[] r_circulate_r(int r)
        {
            int s0 = this._r_in_s[r];
            int incoming = s0;
            var out_r = new List<int>();
            do
            {
                out_r.Add(this.s_begin_r(incoming));
                var outgoing = s_next_s(incoming);
                incoming = this._halfedges[outgoing];
            } while (incoming != -1 && incoming != s0);
            return out_r.ToArray();
        }

        int[] r_circulate_t(int r)
        {
            int s0 = this._r_in_s[r];
            int incoming = s0;
            var out_t = new List<int>();
            do
            {
                out_t.Add(s_to_t(incoming));
                var outgoing = s_next_s(incoming);
                incoming = this._halfedges[outgoing];
            } while (incoming != -1 && incoming != s0);
            return out_t.ToArray();
        }

        public int ghost_r() { return this.numRegions - 1; }
        public bool s_ghost(int s) { return s >= this.numSolidSides; }
        public bool r_ghost(int r) { return r == this.numRegions - 1; }
        public bool t_ghost(int t) { return this.s_ghost(3 * t); }
        public bool s_boundary(int s) { return this.s_ghost(s) && (s % 3 == 0); }
        public bool r_boundary(int r) { return r < this.numBoundaryRegions; }

    }
}
