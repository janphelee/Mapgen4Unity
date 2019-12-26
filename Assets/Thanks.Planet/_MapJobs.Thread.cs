using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

#if Use_Double_Float
using Float = System.Double;
#else
using Float = System.Single;
#endif

namespace Thanks.Planet
{
    unsafe partial class _MapJobs
    {
        /* Calculate the centroid and push it onto an array */
        static void pushCentroidOfTriangle(
            Float* outt, int t,
            Float ax, Float ay, Float az,
            Float bx, Float by, Float bz,
            Float cx, Float cy, Float cz
            )
        {
            // TODO: renormalize to radius 1
            outt[t * 3 + 0] = (ax + bx + cx) / 3;
            outt[t * 3 + 1] = (ay + by + cy) / 3;
            outt[t * 3 + 2] = (az + bz + cz) / 3;
        }

        static NativeArray<Float> generateTriangleCenters(DualMesh mesh, NativeArray<Float> r_xyz)
        {
            var numTriangles = mesh.numTriangles;
            var t_xyz = new NativeArray<Float>(numTriangles * 3, Allocator.Persistent);
            var outt = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(t_xyz);

            for (var t = 0; t < numTriangles; t++)
            {
                int a = mesh.s_begin_r(3 * t),
                    b = mesh.s_begin_r(3 * t + 1),
                    c = mesh.s_begin_r(3 * t + 2);
                pushCentroidOfTriangle(outt, t,
                         r_xyz[3 * a], r_xyz[3 * a + 1], r_xyz[3 * a + 2],
                         r_xyz[3 * b], r_xyz[3 * b + 1], r_xyz[3 * b + 2],
                         r_xyz[3 * c], r_xyz[3 * c + 1], r_xyz[3 * c + 2]);
            }
            return t_xyz;
        }

        public void generateMesh(int N, int P, Float jitter, int seed)
        {
            if (mesh != null) Dispose();

            DualMesh.makeSphere(N, jitter, seed, (m, d) =>
            {
                mesh = m;
                r_xyz = new NativeArray<Float>(d.ToArray(), Allocator.Persistent);
                t_xyz = generateTriangleCenters(mesh, r_xyz);
            });

            r_elevation = new NativeArray<Float>(mesh.numRegions, Allocator.Persistent);
            t_elevation = new NativeArray<Float>(mesh.numTriangles, Allocator.Persistent);
            r_moisture = new NativeArray<Float>(mesh.numRegions, Allocator.Persistent);
            t_moisture = new NativeArray<Float>(mesh.numTriangles, Allocator.Persistent);

            t_downflow_s = new NativeArray<int>(mesh.numTriangles, Allocator.Persistent);
            // order_t[0] 保存个数
            order_t = new NativeArray<int>(mesh.numTriangles + 1, Allocator.Persistent);
            t_flow = new NativeArray<Float>(mesh.numTriangles, Allocator.Persistent);
            s_flow = new NativeArray<Float>(mesh.numSides, Allocator.Persistent);

            if (_randomNoise == null)
            {
                tempBuffer = new NativeArray<byte>(2048, Allocator.Persistent);
                _randomNoise = new SimplexNoise(seed, tempBuffer);
            }
            geometry = new Geometry(mesh.numSides);
            //geometry = new Geometry(mesh.numSides, mesh.numRegions, mesh.numTriangles);

            generateMap(N, P, seed);
        }

        void generateMap(int N, int P, int seed)
        {
            generatePlates(mesh, r_xyz, P, N, seed, (a, b, c) =>
            {
                plate_r = a;
                r_plate = b;
                plate_vec = c;
            });

            plate_is_ocean = new HashSet<int>();
            foreach (var r in plate_r)
            {
                if (Rander.makeRandInt(r)(10) < 5)
                {
                    plate_is_ocean.Add(r);
                    // TODO: either make tiny plates non-ocean, or make sure tiny plates don't create seeds for rivers
                }
            }

            assignRegionElevation(seed);

            // TODO: assign region moisture in a better way!
            var r_moisture = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(this.r_moisture);
            for (var r = 0; r < mesh.numRegions; r++)
            {
                r_moisture[r] = r_plate[r] % 10 / 10.0f;
            }
            assignTriangleValues();
            assignDownflow();
            assignFlow();

            if (geometry.quad)
            {
                generateMeshGeometry();
            }
            else
            {
                generateVoronoiGeometry();
            }
            //draw();
        }

        /**********************************************************************
         * Plates
         */
        static HashSet<int> pickRandomRegions(DualMesh mesh, int N, int seed)
        {
            var randInt = Rander.makeRandInt(seed);
            var numRegions = mesh.numRegions;
            var chosen_r = new HashSet<int>();
            while (chosen_r.Count < N && chosen_r.Count < numRegions)
            {
                chosen_r.Add(randInt(numRegions));
            }
            return chosen_r;
        }

        static void generatePlates(
            DualMesh mesh, NativeArray<Float> r_xyz, int P, int N, int seed,
            Action<HashSet<int>, NativeArray<int>, NativeArray<Vector3>> callback
            )
        {
            var r_plate = new NativeArray<int>(mesh.numRegions, Allocator.Persistent);
            //r_plate.fill(-1);
            for (var i = 0; i < r_plate.Length; ++i) r_plate[i] = -1;

            var plate_r = pickRandomRegions(mesh, Math.Min(P, N), seed);
            var queue = new List<int>();
            foreach (var r in plate_r)
            {
                queue.Add(r);
                r_plate[r] = r;
            }

            var randInt = Rander.makeRandInt(seed);

            /* In Breadth First Search (BFS) the queue will be all elements in
               queue[queue_out ... queue.length-1]. Pushing onto the queue
               adds an element to the end, increasing queue.length. Popping
               from the queue removes an element from the beginning by
               increasing queue_out.

               To add variety, use a random search instead of a breadth first
               search. The frontier of elements to be expanded is still
               queue[queue_out ... queue.length-1], but pick a random element
               to pop instead of the earliest one. Do this by swapping
               queue[pos] and queue[queue_out].
            */
            for (var queue_out = 0; queue_out < queue.Count; queue_out++)
            {
                var pos = queue_out + randInt(queue.Count - queue_out);
                var current_r = queue[pos];
                queue[pos] = queue[queue_out];
                var out_r = mesh.r_circulate_r(current_r);
                foreach (var neighbor_r in out_r)
                {
                    if (r_plate[neighbor_r] == -1)
                    {
                        r_plate[neighbor_r] = r_plate[current_r];
                        queue.Add(neighbor_r);
                    }
                }
            }
            Vector3 xyz(int i) { return new Vector3(r_xyz[i + 0], r_xyz[i + 1], r_xyz[i + 2]); }
            // Assign a random movement vector for each plate
            var plate_vec = new NativeArray<Vector3>(mesh.numRegions, Allocator.Persistent);
            foreach (var center_r in plate_r)
            {
                var neighbor_r = mesh.r_circulate_r(center_r)[0];
                Vector3 p0 = xyz(3 * center_r),
                        p1 = xyz(3 * neighbor_r);
                plate_vec[center_r] = (p1 - p0).normalized;
            }

            callback(plate_r, r_plate, plate_vec);
        }


        /* Calculate the collision measure, which is the amount
         * that any neighbor's plate vector is pushing against 
         * the current plate vector. */
        const Float COLLISION_THRESHOLD = (Float)0.75;
        void findCollisions(Action<HashSet<int>, HashSet<int>, HashSet<int>> callback)
        {
            const Float deltaTime = (Float)1e-2; // simulate movement
            var numRegions = mesh.numRegions;
            var mountain_r = new HashSet<int>();
            var coastline_r = new HashSet<int>();
            var ocean_r = new HashSet<int>();

            Vector3 xyz(int r) { return new Vector3(r_xyz[r + 0], r_xyz[r + 1], r_xyz[r + 2]); }
            /* For each region, I want to know how much it's being compressed
               into an adjacent region. The "compression" is the change in
               distance as the two regions move. I'm looking for the adjacent
               region from a different plate that pushes most into this one*/
            for (var current_r = 0; current_r < numRegions; current_r++)
            {
                var bestCompression = Float.PositiveInfinity;
                var best_r = -1;
                var r_out = mesh.r_circulate_r(current_r);
                foreach (var neighbor_r in r_out)
                {
                    if (r_plate[current_r] != r_plate[neighbor_r])
                    {
                        /* sometimes I regret storing xyz in a compact array... */
                        var current_pos = xyz(3 * current_r);
                        var neighbor_pos = xyz(3 * neighbor_r);
                        /* simulate movement for deltaTime seconds */
                        Float distanceBefore = Vector3.Distance(current_pos, neighbor_pos);
                        current_pos += plate_vec[r_plate[current_r]] * deltaTime;
                        neighbor_pos += plate_vec[r_plate[neighbor_r]] * deltaTime;
                        Float distanceAfter = Vector3.Distance(current_pos, neighbor_pos);

                        /* how much closer did these regions get to each other? */
                        var compression = distanceBefore - distanceAfter;
                        /* keep track of the adjacent region that gets closest */
                        if (compression < bestCompression)
                        {
                            best_r = neighbor_r;
                            bestCompression = compression;
                        }
                    }
                }
                if (best_r != -1)
                {
                    /* at this point, bestCompression tells us how much closer
                       we are getting to the region that's pushing into us the most */
                    var collided = bestCompression > COLLISION_THRESHOLD * deltaTime;

                    var curr = plate_is_ocean.Contains(current_r);
                    var best = plate_is_ocean.Contains(best_r);

                    if (curr && best)
                    {
                        (collided ? coastline_r : ocean_r).Add(current_r);
                    }
                    else if (!curr && !best)
                    {
                        if (collided) mountain_r.Add(current_r);
                    }
                    else
                    {
                        (collided ? mountain_r : coastline_r).Add(current_r);
                    }
                }
            }
            callback(mountain_r, coastline_r, ocean_r);
        }


        /* Distance from any point in seeds_r to all other points, but 
         * don't go past any point in stop_r */
        static Float[] assignDistanceField(DualMesh mesh, HashSet<int> seeds_r, HashSet<int> stop_r, int seed = 123)
        {
            var randInt = Rander.makeRandInt(seed);
            var numRegions = mesh.numRegions;
            var r_distance = new Float[numRegions];
            for (int i = 0; i < numRegions; ++i) r_distance[i] = Float.PositiveInfinity;

            var queue = new List<int>();
            foreach (var r in seeds_r)
            {
                queue.Add(r);
                r_distance[r] = 0;
            }

            /* Random search adapted from breadth first search */
            for (var queue_out = 0; queue_out < queue.Count; queue_out++)
            {
                var pos = queue_out + randInt(queue.Count - queue_out);
                var current_r = queue[pos];
                queue[pos] = queue[queue_out];
                var out_r = mesh.r_circulate_r(current_r);
                foreach (var neighbor_r in out_r)
                {
                    if (r_distance[neighbor_r] == Float.PositiveInfinity && !stop_r.Contains(neighbor_r))
                    {
                        r_distance[neighbor_r] = r_distance[current_r] + 1;
                        queue.Add(neighbor_r);
                    }
                }
            }
            return r_distance;
            // TODO: possible enhancement: keep track of which seed is closest
            // to this point, so that we can assign variable mountain/ocean
            // elevation to each seed instead of them always being +1/-1
        }

        static Float fbm_noise(SimplexNoise simplex, Float nx, Float ny, Float nz)
        {
            var amplitudes = new Float[5];
            for (int i = 0; i < amplitudes.Length; ++i) amplitudes[i] = (Float)Math.Pow(2.0 / 3, i);

            Float sum = 0, sumOfAmplitudes = 0;
            for (var octave = 0; octave < amplitudes.Length; octave++)
            {
                var frequency = 1 << octave;
                sum += amplitudes[octave] * (Float)simplex.noise(nx * frequency, ny * frequency, nz * frequency);
                sumOfAmplitudes += amplitudes[octave];
            }
            return sum / sumOfAmplitudes;
        }

        void assignRegionElevation(int seed)
        {
            const Float epsilon = (Float)1e-3;
            var numRegions = mesh.numRegions;

            HashSet<int> mountain_r = null,
                         coastline_r = null,
                         ocean_r = null;

            findCollisions((a, b, c) =>
            {
                mountain_r = a;
                coastline_r = b;
                ocean_r = c;
            });

            for (var r = 0; r < numRegions; r++)
            {
                if (r_plate[r] == r)
                {
                    (plate_is_ocean.Contains(r) ? ocean_r : coastline_r).Add(r);
                }
            }

            var stop_r = new HashSet<int>();
            foreach (var r in mountain_r) { stop_r.Add(r); }
            foreach (var r in coastline_r) { stop_r.Add(r); }
            foreach (var r in ocean_r) { stop_r.Add(r); }

            //console.log('seeds mountain/coastline/ocean:', mountain_r.size, coastline_r.size, ocean_r.size, 'plate_is_ocean', plate_is_ocean.size, '/', P);
            var r_distance_a = assignDistanceField(mesh, mountain_r, ocean_r, seed);
            var r_distance_b = assignDistanceField(mesh, ocean_r, coastline_r, seed);
            var r_distance_c = assignDistanceField(mesh, coastline_r, stop_r, seed);

            var r_elevation = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(this.r_elevation);
            for (var r = 0; r < numRegions; r++)
            {
                Float a = r_distance_a[r] + epsilon,
                      b = r_distance_b[r] + epsilon,
                      c = r_distance_c[r] + epsilon;
                if (a == Float.PositiveInfinity && b == Float.PositiveInfinity)
                {
                    r_elevation[r] = (Float)0.1;
                }
                else
                {
                    r_elevation[r] = (1 / a - 1 / b) / (1 / a + 1 / b + 1 / c);
                }
                r_elevation[r] += 0.1f * fbm_noise(_randomNoise, r_xyz[3 * r], r_xyz[3 * r + 1], r_xyz[3 * r + 2]);
            }
        }

        void assignTriangleValues()
        {
            var t_elevation = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(this.t_elevation);
            var t_moisture = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(this.t_moisture);
            var numTriangles = mesh.numTriangles;
            for (var t = 0; t < numTriangles; t++)
            {
                var s0 = 3 * t;
                int r1 = mesh.s_begin_r(s0),
                    r2 = mesh.s_begin_r(s0 + 1),
                    r3 = mesh.s_begin_r(s0 + 2);
                t_elevation[t] = 1 / 3 * (r_elevation[r1] + r_elevation[r2] + r_elevation[r3]);
                t_moisture[t] = 1 / 3 * (r_moisture[r1] + r_moisture[r2] + r_moisture[r3]);
            }
        }

        private void assignDownflow()
        {
            var job1 = new Assets.MapJobs.Job5AssignDownslope()
            {
                numTriangles = mesh.numTriangles,
                _halfedges = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._halfedges),
                t_elevation = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(t_elevation),
                order_t = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(order_t),
                t_downslope_s = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(t_downflow_s),
            };
            job1.Execute();
        }

        private void assignFlow(Float flow = 0.5f)
        {
            var job2 = new Assets.MapJobs.Job5AssignFlow()
            {
                flow = flow,
                numTriangles = mesh.numTriangles,
                _halfedges = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._halfedges),
                t_elevation = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(t_elevation),
                order_t = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(order_t),
                t_downslope_s = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(t_downflow_s),
                t_moisture = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(t_moisture),
                t_flow = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(t_flow),
                s_flow = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(s_flow),
            };
            job2.Execute();
        }

        private void generateMeshGeometry()
        {
            //const Float V = 0.95f;
            var numSides = mesh.numSides;
            var numRegions = mesh.numRegions;
            var numTriangles = mesh.numTriangles;


            var xyz = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(geometry.flat_xyz);
            var tm = (Vector2*)NativeArrayUnsafeUtility.GetUnsafePtr(geometry.flat_em);
            var I = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(geometry.flat_i);

            // TODO: multiply all the r, t points by the elevation, taking V into account
            Parallel.For(0, numRegions, r =>
            {
                xyz[r] = new Vector3(r_xyz[r * 3 + 0], r_xyz[r * 3 + 1], r_xyz[r * 3 + 2]);
                tm[r] = new Vector2(r_elevation[r], r_moisture[r]);
            });
            Parallel.For(0, numTriangles, t =>
            {
                xyz[numRegions + t] = new Vector3(t_xyz[t * 3 + 0], t_xyz[t * 3 + 1], t_xyz[t * 3 + 2]);
                tm[numRegions + t] = new Vector2(t_elevation[t], t_moisture[t]);
            });

            int i = 0, count_valley = 0, count_ridge = 0;
            var _halfedges = mesh._halfedges;
            var _triangles = mesh._triangles;
            for (var s = 0; s < numSides; s++)
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
                var coast = r_elevation[r1] < 0.0 || r_elevation[r2] < 0.0;
                if (coast || s_flow[s] > 0 || s_flow[opposite_s] > 0)
                {
                    // It's a coastal or river edge, forming a valley
                    I[i++] = r1; I[i++] = numRegions + t2; I[i++] = numRegions + t1;
                    //I[i++] = numRegions + t1; I[i++] = numRegions + t2; I[i++] = r1;//逆序
                    count_valley++;
                }
                else
                {
                    // It's a ridge
                    I[i++] = r1; I[i++] = r2; I[i++] = numRegions + t1;
                    //I[i++] = numRegions + t1; I[i++] = r2; I[i++] = r1;//逆序
                    count_ridge++;
                }
            }
        }

        private void generateVoronoiGeometry()
        {
            var numSides = mesh.numSides;

            var e = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(this.r_elevation);
            var m = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(this.r_moisture);

            var xyz = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(geometry.flat_xyz);
            var tm = (Vector2*)NativeArrayUnsafeUtility.GetUnsafePtr(geometry.flat_em);

            for (var s = 0; s < numSides; s++)
            {
                int inner_t = mesh.s_inner_t(s),
                    outer_t = mesh.s_outer_t(s),
                    begin_r = mesh.s_begin_r(s);
                var rgb = new Vector2(e[begin_r], m[begin_r]);
                xyz[s * 3 + 0] = new Vector3(t_xyz[3 * inner_t], t_xyz[3 * inner_t + 1], t_xyz[3 * inner_t + 2]);
                xyz[s * 3 + 1] = new Vector3(t_xyz[3 * outer_t], t_xyz[3 * outer_t + 1], t_xyz[3 * outer_t + 2]);
                xyz[s * 3 + 2] = new Vector3(r_xyz[3 * begin_r], r_xyz[3 * begin_r + 1], r_xyz[3 * begin_r + 2]);
                tm[s * 3 + 0] = rgb;
                tm[s * 3 + 1] = rgb;
                tm[s * 3 + 2] = rgb;
            }
        }
    }
}