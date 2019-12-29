using Phevolution;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Debug = UnityEngine.Debug;

#if Use_Double_Float
using Float = System.Double;
#else
using Float = System.Single;
#endif

namespace Thanks.Planet
{
    unsafe partial class _MapJobs
    {
        private Action<long> lastCallback { get; set; }
        private void onConfig(int i)
        {
            int b = 1 << i;
            if (working) nextFlag |= b;
            else needFlag |= b;

            processAsync(lastCallback);
        }

        private int nextFlag = 0;
        private int needFlag = 0xffff;

        protected override void beforeNextJob()
        {
            needFlag = nextFlag;
            nextFlag = 0;
        }

        protected override void process(Action<long> callback)
        {
            lastCallback = callback;

            var watcher = new Stopwatch();
            watcher.Start();

            if ((needFlag & 0b1111) != 0)
            {
                var N = vInt(1);
                var P = vInt(2);
                var seed = vInt(0);
                var jitter = vFloat(3);

                generateMesh(N, P, jitter, seed);
                //DebugHelper.SaveArray("r_xyz.txt", r_xyz);
                //DebugHelper.SaveArray("_triangles.txt", mesh._triangles);
                //DebugHelper.SaveArray("_halfedges.txt", mesh._halfedges);

                plate_r = generatePlates(mesh, r_xyz, P, N, seed, r_plate, plate_vec);
                //DebugHelper.SaveArray("r_plate.txt", r_plate);
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
                assignMoisture();
                assignTriangleValues();
                //assignDownflow();
                //assignFlow();
            }

            if (geometry != null) geometry.Dispose();
            // 更改渲染网格，只需要最后一步
            if (vInt(4) == 0)
            {
                geometry = new Geometry(mesh.numSides);
                generateVoronoiGeometry();
            }
            else
            {
                geometry = new Geometry(mesh.numSides, mesh.numRegions, mesh.numTriangles);
                generateMeshGeometry();
            }

            needFlag = 0;

            watcher.Stop();

            callback?.Invoke(watcher.ElapsedMilliseconds);
        }

        void generateMesh(int N, int P, Float jitter, int seed)
        {
            if (mesh != null) disposeMesh();

            // 噪音只初始化一次就好
            if (simplex == null) simplex = new SimplexNoise(seed);

            DualMesh m;
            List<double> d;
            DualMesh.makeSphere(N, jitter, seed, out m, out d);

            mesh = m;
            r_xyz = new NativeArray<double>(d.ToArray(), Allocator.Persistent);
            t_xyz = new NativeArray<double>(mesh.numSides, Allocator.Persistent);
            assignTriangleCenters(mesh, r_xyz, t_xyz);

            r_elevation = new NativeArray<Float>(mesh.numRegions, Allocator.Persistent);
            r_moisture = new NativeArray<Float>(mesh.numRegions, Allocator.Persistent);
            t_elevation = new NativeArray<Float>(mesh.numTriangles, Allocator.Persistent);
            t_moisture = new NativeArray<Float>(mesh.numTriangles, Allocator.Persistent);

            t_downflow_s = new NativeArray<int>(mesh.numTriangles, Allocator.Persistent);
            // order_t[0] 保存个数
            order_t = new NativeArray<int>(mesh.numTriangles + 1, Allocator.Persistent);
            t_flow = new NativeArray<Float>(mesh.numTriangles, Allocator.Persistent);
            // numSides = numTriangles * 3
            s_flow = new NativeArray<Float>(mesh.numSides, Allocator.Persistent);

            r_plate = new NativeArray<int>(mesh.numRegions, Allocator.Persistent);
            plate_vec = new NativeArray<Vector3>(mesh.numRegions, Allocator.Persistent);

            Debug.Log($"generateMesh N:{N} P:{P} jitter:{jitter} seed:{seed} region:{mesh.numRegions} tri:{mesh.numTriangles}");
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

        static HashSet<int> generatePlates(
            DualMesh mesh,
            NativeArray<double> r_xyz,
            int P, int N, int seed,
            /** output */
            NativeArray<int> r_plate,
            NativeArray<Vector3> plate_vec
            )
        {
            //r_plate.fill(-1);
            for (var i = 0; i < r_plate.Length; ++i) r_plate[i] = -1;

            var plate_r = pickRandomRegions(mesh, Math.Min(P, N), seed);
            var queue = new List<int>(plate_r);
            foreach (var r in queue) { r_plate[r] = r; }

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
            //DebugHelper.SaveArray("plate_r.txt", plate_r);
            //DebugHelper.SaveArray("queue.txt", queue);

            Vector3 xyz(int i) { return new Vector3((float)r_xyz[i + 0], (float)r_xyz[i + 1], (float)r_xyz[i + 2]); }
            // Assign a random movement vector for each plate
            foreach (var center_r in plate_r)
            {
                var neighbor_r = mesh.r_circulate_r(center_r)[0];
                Vector3 p0 = xyz(3 * center_r),
                        p1 = xyz(3 * neighbor_r);
                plate_vec[center_r] = (p1 - p0).normalized;
                //Debug.Log($"{center_r}, {neighbor_r} {plate_vec[center_r].x},{plate_vec[center_r].y},{plate_vec[center_r].z}");
            }

            return plate_r;
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

            Vector3 xyz(int r) { return new Vector3((float)r_xyz[r + 0], (float)r_xyz[r + 1], (float)r_xyz[r + 2]); }
            /* For each region, I want to know how much it's being compressed
               into an adjacent region. The "compression" is the change in
               distance as the two regions move. I'm looking for the adjacent
               region from a different plate that pushes most into this one*/
            for (var current_r = 0; current_r < numRegions; current_r++)
            {
                //if (r_plate[current_r] < 0)
                //{
                //    Debug.Log($"r_plate[current_r] == -1 {current_r}");
                //    continue;
                //}
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

            Debug.Log($"'seeds mountain/coastline/ocean:'{mountain_r.Count}, {coastline_r.Count}, {ocean_r.Count} plate_is_ocean:{plate_is_ocean.Count}");
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
                r_elevation[r] += 0.1f * fbm_noise(simplex, (float)r_xyz[3 * r], (float)r_xyz[3 * r + 1], (float)r_xyz[3 * r + 2]);
            }
        }
    }
}