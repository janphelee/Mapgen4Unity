using Assets.MapGen.MapUtil;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.MapGen
{
    class MapData
    {
        /**
         * Mountains are peaks surrounded by steep dropoffs. In the point
         * selection process (mesh.js) we pick the mountain peak locations.
         * Here we calculate a distance field from peaks to all other points.
         *
         * We'll use breadth first search for this because it's simple and
         * fast. Dijkstra's Algorithm would produce a more accurate distance
         * field, but we only need an approximation. For increased
         * interestingness, we add some randomness to the distance field.
         *
         * @param {Mesh} mesh
         * @param {number[]} seeds_t - a list of triangles with mountain peaks
         * @param {number} spacing - the global param.spacing value
         * @param {number} jaggedness - how much randomness to mix into the distances
         * @param {function(): number} randFloat - random number generator
         * @param {Float32Array} t_distance - the distance field indexed by t, OUTPUT
         */
        public static void calculateMountainDistance(MeshData mesh, int[] seeds_t, float spacing, float jaggedness, Rander.RandFloat randFloat, float[] t_distance)
        {
            var s_length = mesh.s_length;
            // 要赋值给t_distance的，不能重新生成数组
            for (var i = 0; i < t_distance.Length; ++i) t_distance[i] = -1;

            var queue_t = new List<int>(seeds_t);
            for (var i = 0; i < queue_t.Count; i++)
            {
                var current_t = queue_t[i];
                for (var j = 0; j < 3; j++)
                {
                    var s = 3 * current_t + j;
                    var neighbor_t = mesh.s_outer_t(s);
                    if (t_distance[neighbor_t] == -1f)
                    {
                        var increment = spacing * (1 + jaggedness * (randFloat() - randFloat()));
                        t_distance[neighbor_t] = t_distance[current_t] + increment;
                        queue_t.Add(neighbor_t);
                    }
                }
            }
        }

        struct PreNoise
        {
            public float[] t_noise0 { get; set; }
            public float[] t_noise1 { get; set; }
            public float[] t_noise2 { get; set; }
            public float[] t_noise3 { get; set; }
            public float[] t_noise4 { get; set; }
            public float[] t_noise5 { get; set; }
            public float[] t_noise6 { get; set; }
        }
        private static PreNoise precalculateNoise(Rander.RandFloat randFloat, MeshData mesh)
        {
            var noise = new SimplexNoise(randFloat);
            var numTriangles = mesh.numTriangles;
            float[]
                t_noise0 = new float[numTriangles],
                t_noise1 = new float[numTriangles],
                t_noise2 = new float[numTriangles],
                t_noise3 = new float[numTriangles],
                t_noise4 = new float[numTriangles],
                t_noise5 = new float[numTriangles],
                t_noise6 = new float[numTriangles];
            for (var t = 0; t < numTriangles; t++)
            {
                float
                    nx = (mesh.t_x(t) - 500) / 500,
                    ny = (mesh.t_y(t) - 500) / 500;
                t_noise0[t] = (float)noise.noise(nx, ny);
                t_noise1[t] = (float)noise.noise(2 * nx + 5, 2 * ny + 5);
                t_noise2[t] = (float)noise.noise(4 * nx + 7, 4 * ny + 7);
                t_noise3[t] = (float)noise.noise(8 * nx + 9, 8 * ny + 9);
                t_noise4[t] = (float)noise.noise(16 * nx + 15, 16 * ny + 15);
                t_noise5[t] = (float)noise.noise(32 * nx + 31, 32 * ny + 31);
                t_noise6[t] = (float)noise.noise(64 * nx + 67, 64 * ny + 67);
            }
            return new PreNoise()
            {
                t_noise0 = t_noise0,
                t_noise1 = t_noise1,
                t_noise2 = t_noise2,
                t_noise3 = t_noise3,
                t_noise4 = t_noise4,
                t_noise5 = t_noise5,
                t_noise6 = t_noise6
            };
        }

        public MeshData mesh { get; private set; }
        int[] peaks_t { get; set; }
        int seed { get; set; }
        float spacing { get; set; }
        float mountainJaggedness { get; set; }
        float windAngleDeg { get; set; }

        float[] t_elevation { get; set; }
        float[] r_elevation { get; set; }
        float[] r_humidity { get; set; }
        float[] t_moisture { get; set; }
        float[] r_rainfall { get; set; }
        int[] t_downslope_s { get; set; }
        int[] order_t { get; set; }
        float[] t_flow { get; set; }
        float[] s_flow { get; set; }
        int[] wind_order_r { get; set; }
        float[] r_wind_sort { get; set; }
        float[] t_mountain_distance { get; set; }

        PreNoise precomputed { get; set; }

        public MapData(MeshData mesh, int[] peaks_t, float spacing)
        {
            this.mesh = mesh;
            this.peaks_t = peaks_t;
            this.seed = -1;
            this.spacing = spacing;
            this.mountainJaggedness = float.NegativeInfinity;
            this.windAngleDeg = float.PositiveInfinity;
            this.t_elevation = new float[mesh.numTriangles];
            this.r_elevation = new float[mesh.numRegions];
            this.r_humidity = new float[mesh.numRegions];
            this.t_moisture = new float[mesh.numTriangles];
            this.r_rainfall = new float[mesh.numRegions];
            this.t_downslope_s = new int[mesh.numTriangles];
            this.order_t = new int[mesh.numTriangles];
            this.t_flow = new float[mesh.numTriangles];
            this.s_flow = new float[mesh.numSides];
            this.wind_order_r = new int[mesh.numRegions];
            this.r_wind_sort = new float[mesh.numRegions];
            this.t_mountain_distance = new float[mesh.numTriangles];
        }

        public void assignElevation(MapPainting painting, int seed = 187, float mountain_jagged = 0/*[0,1]*/)
        {
            if (this.seed != seed || this.mountainJaggedness != mountain_jagged)
            {
                this.mountainJaggedness = mountain_jagged;
                calculateMountainDistance(
                    this.mesh, this.peaks_t, this.spacing,
                    this.mountainJaggedness, Rander.makeRandFloat(seed),
                    this.t_mountain_distance
                );
            }

            if (this.seed != seed)
            {
                // TODO: function should reuse existing arrays
                this.seed = seed;
                this.precomputed = precalculateNoise(Rander.makeRandFloat(seed), this.mesh);
            }

            this.assignTriangleElevation(painting);
            this.assignRegionElevation();
        }

        private delegate double ConstraintAt(float x, float y);
        private void assignTriangleElevation(MapPainting painting, float noisy_coastlines = 0.01f, float hill_height = 0.02f, float ocean_depth = 1.5f)
        {

            int numTriangles = mesh.numTriangles,
                numSolidTriangles = mesh.numSolidTriangles;
            float[]
                t_noise0 = precomputed.t_noise0, t_noise1 = precomputed.t_noise1,
                t_noise2 = precomputed.t_noise2, t_noise4 = precomputed.t_noise4,
                t_noise5 = precomputed.t_noise5, t_noise6 = precomputed.t_noise6;

            ConstraintAt constraintAt = (x, y) =>
             {
                 // https://en.wikipedia.org/wiki/Bilinear_interpolation
                 var C = painting.elevation;
                 var size = MapPainting.CANVAS_SIZE;
                 x *= size; y *= size;

                 int xInt = (int)Math.Floor(x), yInt = (int)Math.Floor(y);
                 float xFrac = x - xInt, yFrac = y - yInt;

                 if (0 <= xInt && xInt + 1 < size && 0 <= yInt && yInt + 1 < size)
                 {
                     int p = size * yInt + xInt;
                     double
                         e00 = C[p],
                         e01 = C[p + 1],
                         e10 = C[p + size],
                         e11 = C[p + size + 1];
                     return ((e00 * (1 - xFrac) + e01 * xFrac) * (1 - yFrac)
                             + (e10 * (1 - xFrac) + e11 * xFrac) * yFrac);
                 }
                 else
                 {
                     return -1.0;
                 }
             };

            for (var t = 0; t < numSolidTriangles; t++)
            {
                var e = constraintAt(mesh.t_x(t) / 1000, mesh.t_y(t) / 1000);
                // TODO: e*e*e*e seems too steep for this, as I want this
                // to apply mostly at the original coastlines and not
                // elsewhere
                t_elevation[t] = (float)(e + noisy_coastlines * (1 - e * e * e * e) * (t_noise4[t] + t_noise5[t] / 2 + t_noise6[t] / 4));
            }

            // For land triangles, mix hill and mountain terrain together
            int mountain_slope = 20;
            for (var t = 0; t < numTriangles; t++)
            {
                double e = t_elevation[t];
                if (e > 0.0)
                {
                    /* Mix two sources of elevation:
                     *
                     * 1. eh: Hills are formed using simplex noise. These
                     *    are very low amplitude, and the main purpose is
                     *    to make the rivers meander. The amplitude
                     *    doesn't make much difference in the river
                     *    meandering. These hills shouldn't be
                     *    particularly visible so I've kept the amplitude
                     *    low.
                     *
                     * 2. em: Mountains are formed using something similar to
                     *    worley noise. These form distinct peaks, with
                     *    varying distance between them.
                     */
                    // TODO: precompute eh, em per triangle
                    var noisiness = 1.0 - 0.5 * (1 + t_noise0[t]);
                    var eh = (1 + noisiness * t_noise4[t] + (1 - noisiness) * t_noise2[t]) * hill_height;
                    if (eh < 0.01) { eh = 0.01; }
                    var em = 1 - mountain_slope / 1000.0 * t_mountain_distance[t];
                    if (em < 0.01) { em = 0.01; }
                    var weight = e * e;
                    e = (1 - weight) * eh + weight * em;
                }
                else
                {
                    /* Add noise to make it more interesting. */
                    e *= ocean_depth + t_noise1[t];
                }
                if (e < -1.0) { e = -1.0; }
                if (e > +1.0) { e = +1.0; }
                t_elevation[t] = (float)e;
            }
        }

        private void assignRegionElevation()
        {
            int numRegions = mesh.numRegions;
            int[] _r_in_s = mesh._r_in_s, _halfedges = mesh._halfedges;

            for (var r = 0; r < numRegions; r++)
            {
                var count = 0;
                float e = 0;
                bool water = false;
                var s0 = _r_in_s[r];
                var incoming = s0;
                do
                {
                    var t = (incoming / 3) | 0;
                    e += t_elevation[t];
                    water = water || t_elevation[t] < 0;
                    var outgoing = MeshData.s_next_s(incoming);
                    incoming = _halfedges[outgoing];
                    count++;
                } while (incoming != s0);
                e /= count;
                if (water && e > 0) { e = -0.001f; }
                r_elevation[r] = e;
            }
        }

        public void assignRainfall(float wind_angle_deg = 0/*[0,360]*/, float raininess = 0.9f/*[0,2]*/, float evaporation = 0.5f/*[0,1]*/, float rain_shadow = 0.5f/*[0.1,2]*/)
        {
            int numRegions = mesh.numRegions;
            int[] _r_in_s = mesh._r_in_s, _halfedges = mesh._halfedges;

            if (wind_angle_deg != this.windAngleDeg)
            {
                this.windAngleDeg = wind_angle_deg;
                var windAngleRad = Mathf.PI / 180 * this.windAngleDeg;
                var windAngleVec = new float[] { Mathf.Cos(windAngleRad), Mathf.Sin(windAngleRad) };
                for (var r = 0; r < numRegions; r++)
                {
                    wind_order_r[r] = r;
                    r_wind_sort[r] = mesh.r_x(r) * windAngleVec[0] + mesh.r_y(r) * windAngleVec[1];
                }
                Array.Sort(wind_order_r, (int r1, int r2) => r_wind_sort[r1].CompareTo(r_wind_sort[r2]));
            }

            foreach (var r in wind_order_r)
            {
                int count = 0;
                var sum = 0.0;
                int s0 = _r_in_s[r], incoming = s0;
                do
                {
                    var neighbor_r = mesh.s_begin_r(incoming);
                    if (r_wind_sort[neighbor_r] < r_wind_sort[r])
                    {
                        count++;
                        sum += r_humidity[neighbor_r];
                    }
                    var outgoing = MeshData.s_next_s(incoming);
                    incoming = _halfedges[outgoing];
                } while (incoming != s0);

                double humidity = 0.0, rainfall = 0.0;
                if (count > 0)
                {
                    humidity = sum / count;
                    rainfall += raininess * humidity;
                }
                if (mesh.r_boundary(r))
                {
                    humidity = 1.0;
                }
                if (r_elevation[r] < 0.0)
                {
                    var evaporate = evaporation * -r_elevation[r];
                    humidity += evaporate;
                }
                if (humidity > 1.0 - r_elevation[r])
                {
                    var orographicRainfall = rain_shadow * (humidity - (1.0 - r_elevation[r]));
                    rainfall += raininess * orographicRainfall;
                    humidity -= orographicRainfall;
                }
                r_rainfall[r] = (float)rainfall;
                r_humidity[r] = (float)humidity;
            }
        }

        public void assignRivers()
        {
            assignDownslope();
            assignMoisture();
            assignFlow();
        }
        /**
         * Use prioritized graph exploration to assign river flow direction
         *
         * @param {Mesh} mesh
         * @param {Float32Array} t_elevation - elevation per triangle
         * @param {Int32Array} t_downslope_s - OUT parameter - the side each triangle flows out of
         * @param {Int32Array} order_t - OUT parameter - pre-order in which the graph was traversed,
         *   so roots of the tree always get visited before leaves; use reverse to visit leaves before roots
         */
        private void assignDownslope()
        {
            /* Use a priority queue, starting with the ocean triangles and
             * moving upwards using elevation as the priority, to visit all
             * the land triangles */
            var mesh = this.mesh;
            int numTriangles = mesh.numTriangles;
            int queue_in = 0;

            // t_downslope_s.fill(-999);
            for (int i = 0; i < t_downslope_s.Length; ++i) t_downslope_s[i] = -999;

            var queue = new FlatQueue<int, float>();

            /* Part 1: non-shallow ocean triangles get downslope assigned to the lowest neighbor */
            for (var t = 0; t < numTriangles; t++)
            {
                if (t_elevation[t] < -0.1)
                {
                    int best_s = -1;
                    var best_e = t_elevation[t];
                    for (var j = 0; j < 3; j++)
                    {
                        int s = 3 * t + j;
                        var e = t_elevation[mesh.s_outer_t(s)];
                        if (e < best_e)
                        {
                            best_e = e;
                            best_s = s;
                        }
                    }
                    order_t[queue_in++] = t;
                    t_downslope_s[t] = best_s;
                    queue.push(t, t_elevation[t]);
                }
            }
            /* Part 2: land triangles get visited in elevation priority */
            for (var queue_out = 0; queue_out < numTriangles; queue_out++)
            {
                int current_t = queue.pop();
                for (var j = 0; j < 3; j++)
                {
                    var s = 3 * current_t + j;
                    var neighbor_t = mesh.s_outer_t(s); // uphill from current_t
                    if (t_downslope_s[neighbor_t] == -999)
                    {
                        t_downslope_s[neighbor_t] = mesh.s_opposite_s(s);
                        order_t[queue_in++] = neighbor_t;
                        queue.push(neighbor_t, t_elevation[neighbor_t]);
                    }
                }
            }
        }
        /**
         * @param {Mesh} mesh
         * @param {Float32Array} r_rainfall - per region
         * @param {Float32Array} t_moisture - OUT parameter - per triangle
         */
        private void assignMoisture()
        {
            var mesh = this.mesh;
            int numTriangles = mesh.numTriangles;
            for (var t = 0; t < numTriangles; t++)
            {
                var moisture = 0.0f;
                for (var i = 0; i < 3; i++)
                {
                    int s = 3 * t + i,
                        r = mesh.s_begin_r(s);
                    moisture += r_rainfall[r] / 3;
                }
                t_moisture[t] = moisture;
            }
        }
        /**
         * @param {Int32Array} order_t
         * @param {any} riversParam
         * @param {Float32Array} t_elevation
         * @param {Float32Array} t_moisture
         * @param {Int32Array} t_downslope_s
         * @param {Float32Array} t_flow
         */
        private void assignFlow(float flow = 0.2f)
        {
            var mesh = this.mesh;
            int numTriangles = mesh.numTriangles;
            int[] _halfedges = mesh._halfedges;

            for (var t = 0; t < numTriangles; t++)
            {
                if (t_elevation[t] >= 0)
                {
                    t_flow[t] = flow * t_moisture[t] * t_moisture[t];
                }
                else
                {
                    t_flow[t] = 0;
                }
            }
            for (var i = order_t.Length - 1; i >= 0; i--)
            {
                var tributary_t = order_t[i];
                var flow_s = t_downslope_s[tributary_t];
                var trunk_t = flow_s < 0 ? 0 : (_halfedges[flow_s] / 3);
                if (flow_s >= 0)// 可能有未重新赋值的-999
                {
                    t_flow[trunk_t] += t_flow[tributary_t];
                    s_flow[flow_s] += t_flow[tributary_t]; // TODO: s_flow[t_downslope_s[t]] === t_flow[t]; redundant?
                    if (t_elevation[trunk_t] > t_elevation[tributary_t] && t_elevation[tributary_t] >= 0)
                    {
                        t_elevation[trunk_t] = t_elevation[tributary_t];
                    }
                }
            }
        }

        class _XyUv
        {
            public float[] xy { get; set; }
            public Vector2 uv { get; set; }
        }
        /**
         * Create a bitmap that will be used for texture mapping
         *   BEND textures will be ordered: {blank side, input side, output side}
         *   FORK textures will be ordered: {passive input side, active input side, output side}
         *
         * Cols will be the input flow rate
         * Rows will be the output flow rate
         */
        private _XyUv[][][][] assignTextureCoordinates(float spacing, int numSizes, int textureSize)
        {
            /* create (numSizes+1)^2 size combinations, each with two triangles */
            _XyUv UV(float x, float y)
            {
                return new _XyUv()
                {
                    xy = new float[] { x, y },
                    uv = new Vector2((x + 0.5f) / textureSize, (y + 0.5f) / textureSize)
                };
            }

            var triangles = new _XyUv[numSizes + 1][][][];
            float width = Mathf.Floor((textureSize - 2 * spacing) / (2 * numSizes + 3)) - spacing,
                  height = Mathf.Floor((textureSize - 2 * spacing) / (numSizes + 1)) - spacing;
            for (var row = 0; row <= numSizes; row++)
            {
                triangles[row] = new _XyUv[numSizes + 1][][];
                for (var col = 0; col <= numSizes; col++)
                {
                    float baseX = spacing + (2 * spacing + 2 * width) * col,
                          baseY = spacing + (spacing + height) * row;
                    var t1 = new _XyUv[] { UV(baseX + width, baseY), UV(baseX, baseY + height), UV(baseX + 2 * width, baseY + height) };
                    var t2 = new _XyUv[] { UV(baseX + 2 * width + spacing, baseY + height), UV(baseX + 3 * width + spacing, baseY), UV(baseX + width + spacing, baseY) };
                    triangles[row][col] = new _XyUv[][] { t1, t2 };
                }
            }
            return triangles;
        }

        /**
         * 单个网格顶点数量不能超过 UInt16 MaxValue = 65535
         */
        public void setGeometry(Vector3[] P, Vector2[] E, int[] I)
        {
            setVertices(P);
            setElevation(E);
            setTriangles(I);
        }

        private void setVertices(Vector3[] P)
        {
            var mesh = this.mesh;
            int numRegions = mesh.numRegions, numTriangles = mesh.numTriangles;
            if (P.Length != (numRegions + numTriangles)) { throw new Exception("wrong size"); }

            var p = 0;
            for (var r = 0; r < numRegions; r++)
            {
                P[p++] = new Vector3(mesh.r_x(r), mesh.r_y(r));
            }
            for (var t = 0; t < numTriangles; t++)
            {
                P[p++] = new Vector3(mesh.t_x(t), mesh.t_y(t));
            }
        }

        private void setElevation(Vector2[] E)
        {
            // TODO: V should probably depend on the slope, or elevation, or maybe it should be 0.95 in mountainous areas and 0.99 elsewhere
            const float V = 0.95f; // reduce elevation in valleys
            var mesh = this.mesh;
            var map = this;
            float[]
                r_elevation = map.r_elevation,
                t_elevation = map.t_elevation,
                r_rainfall = map.r_rainfall,
                s_flow = map.s_flow;
            int numSolidSides = mesh.numSolidSides, numRegions = mesh.numRegions, numTriangles = mesh.numTriangles;
            if (E.Length != (numRegions + numTriangles)) { throw new Exception("wrong size"); }

            var p = 0;
            for (var r = 0; r < numRegions; r++)
            {
                E[p++] = new Vector2(r_elevation[r], r_rainfall[r]);
                //E[p++] = new Vector2(r_elevation[r], 0);
            }
            for (var t = 0; t < numTriangles; t++)
            {
                var x = V * t_elevation[t];
                var s0 = 3 * t;
                int r1 = mesh.s_begin_r(s0),
                    r2 = mesh.s_begin_r(s0 + 1),
                    r3 = mesh.s_begin_r(s0 + 2);
                var y = 1f / 3 * (r_rainfall[r1] + r_rainfall[r2] + r_rainfall[r3]);
                E[p++] = new Vector2(x, y);
                //E[p++] = new Vector2(0, 0);
            }

            if (E.Length != p) { throw new Exception("wrong size"); }
        }

        private void setTriangles(int[] I)
        {
            var mesh = this.mesh;
            int numSolidSides = mesh.numSolidSides, numRegions = mesh.numRegions;

            if (I.Length != 3 * numSolidSides) { throw new Exception("wrong size"); }

            // TODO: split this into its own function; it can be updated separately, and maybe not as often
            var i = 0;
            //let { _halfedges, _triangles} = mesh;
            for (var s = 0; s < numSolidSides; s++)
            {
                int opposite_s = mesh.s_opposite_s(s),
                   r1 = mesh.s_begin_r(s),
                   r2 = mesh.s_begin_r(opposite_s),
                   t1 = mesh.s_inner_t(s),
                   t2 = mesh.s_inner_t(opposite_s);

                // Each quadrilateral is turned into two triangles, so each
                // half-edge gets turned into one. There are two ways to fold
                // a quadrilateral. This is usually a nuisance but in this
                // case it's a feature. See the explanation here
                // https://www.redblobgames.com/x/1725-procedural-elevation/#rendering
                var coast = r_elevation[r1] < 0 || r_elevation[r2] < 0;
                if (coast || s_flow[s] > 0 || s_flow[opposite_s] > 0)
                {
                    // It's a coastal or river edge, forming a valley
                    I[i++] = r1; I[i++] = numRegions + t2; I[i++] = numRegions + t1;
                }
                else
                {
                    // It's a ridge
                    I[i++] = r1; I[i++] = r2; I[i++] = numRegions + t1;
                }
            }

            if (I.Length != i) { throw new Exception("wrong size"); }
        }


        const int riverTextureSpacing = 40;
        const int numRiverSizes = 24;
        const int riverTextureSize = 4096;
        const float riverMaximumFractionOfWidth = 0.5f;
        private _XyUv[][][][] riverTexturePositions { get { return assignTextureCoordinates(riverTextureSpacing, numRiverSizes, riverTextureSize); } }

        public void setRiverTextures(out Vector3[] vertices, out Vector2[] uvs, out int[] triangles)
        {
            const float lg_min_flow = 2.7f;
            const float lg_river_width = -2.7f;
            float MIN_FLOW = Mathf.Exp(lg_min_flow);
            float RIVER_WIDTH = Mathf.Exp(lg_river_width);

            var numSolidTriangles = mesh.numSolidTriangles;
            var s_length = mesh.s_length;
            var riverTexturePositions = this.riverTexturePositions;
            var spacing = this.spacing;

            int riverSize(int s, float flow)
            {
                // TODO: performance: build a table of flow to width
                if (s < 0) { return 1; }
                var width = Mathf.Sqrt(flow - MIN_FLOW) * spacing * RIVER_WIDTH;
                var size = Mathf.Ceil(width * numRiverSizes / s_length[s]);
                return Mathf.Clamp((int)size, 1, numRiverSizes);
            }

            var P = new List<Vector3>();
            var E = new List<Vector2>();
            var I = new List<int>();
            int p = 0;
            for (var t = 0; t < numSolidTriangles; t++)
            {
                var out_s = t_downslope_s[t];
                if (out_s < 0) continue;

                var out_flow = s_flow[out_s];
                if (out_flow < MIN_FLOW) continue;

                int r1 = mesh.s_begin_r(3 * t),
                    r2 = mesh.s_begin_r(3 * t + 1),
                    r3 = mesh.s_begin_r(3 * t + 2);
                int in1_s = MeshData.s_next_s(out_s);
                int in2_s = MeshData.s_next_s(in1_s);
                var in1_flow = s_flow[mesh.s_opposite_s(in1_s)];
                var in2_flow = s_flow[mesh.s_opposite_s(in2_s)];
                var textureRow = riverSize(out_s, out_flow);

                void add(int r, int c, int i, int j, int k)
                {
                    var T = riverTexturePositions[r][c][0];
                    /**
                       P[p    ] = mesh.r_x(r1);
                       P[p + 1] = mesh.r_y(r1);2  3
                       P[p + 4] = mesh.r_x(r2);
                       P[p + 5] = mesh.r_y(r2);6  7
                       P[p + 8] = mesh.r_x(r3);
                       P[p + 9] = mesh.r_y(r3);10 11
                       P[p + 4*(out_s - 3*t) + 2] = T[i].uv[0];
                       P[p + 4*(out_s - 3*t) + 3] = T[i].uv[1];
                       P[p + 4*(in1_s - 3*t) + 2] = T[j].uv[0];
                       P[p + 4*(in1_s - 3*t) + 3] = T[j].uv[1];
                       P[p + 4*(in2_s - 3*t) + 2] = T[k].uv[0];
                       P[p + 4*(in2_s - 3*t) + 3] = T[k].uv[1];
                     */
                    I.Add(p);
                    I.Add(p + 1);
                    I.Add(p + 2);

                    P.Add(new Vector3(mesh.r_x(r1), mesh.r_y(r1)));
                    P.Add(new Vector3(mesh.r_x(r2), mesh.r_y(r2)));
                    P.Add(new Vector3(mesh.r_x(r3), mesh.r_y(r3)));

                    var te = new Vector2[3];
                    te[(out_s - 3 * t)] = T[i].uv;
                    te[(in1_s - 3 * t)] = T[j].uv;
                    te[(in2_s - 3 * t)] = T[k].uv;
                    E.Add(te[0]);
                    E.Add(te[1]);
                    E.Add(te[2]);

                    p += 3;
                }

                if (in1_flow >= MIN_FLOW)
                {
                    add(textureRow, riverSize(in1_s, in1_flow), 0, 2, 1);
                }
                if (in2_flow >= MIN_FLOW)
                {
                    add(textureRow, riverSize(in2_s, in2_flow), 2, 1, 0);
                }
            }
            triangles = I.ToArray();
            vertices = P.ToArray();
            uvs = E.ToArray();
        }

    }
}
