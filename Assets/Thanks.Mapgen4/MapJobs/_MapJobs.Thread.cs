using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Unity.Collections;
using UnityEngine;

#if Use_Double_Float
using Float = System.Double;
#else
using Float = System.Single;
#endif

namespace Assets.MapJobs
{
    using Debug = UnityEngine.Debug;

    partial class _MapJobs
    {
        public static void calculateMountainDistance(int seed, DualMesh mesh, int[] peak_t, Float spacing, Float jaggedness, NativeArray<Float> t_distance)
        {
            int randIndex = 0;

            for (var i = 0; i < t_distance.Length; ++i) t_distance[i] = -1f;

            var queue_t = new List<int>(peak_t);
            for (var i = 0; i < queue_t.Count; i++)
            {
                var current_t = queue_t[i];
                for (var j = 0; j < 3; j++)
                {
                    var s = 3 * current_t + j;
                    var neighbor_t = mesh.s_outer_t(s);
                    if (t_distance[neighbor_t] == -1f)
                    {
                        var rf1 = Rander.randFloat(seed, randIndex++);
                        var rf2 = Rander.randFloat(seed, randIndex++);
                        var increment = spacing * (1 + jaggedness * (rf1 - rf2));
                        t_distance[neighbor_t] = t_distance[current_t] + increment;
                        queue_t.Add(neighbor_t);
                    }
                }
            }
        }

        private int[] createPeaks(short[] peaks_index, int numBoundaryRegions)
        {

            var peaks_t = new List<int>();
            for (var i = 0; i < peaks_index.Length; ++i)
            {
                var index = peaks_index[i];
                var r = index + numBoundaryRegions;
                peaks_t.Add(mesh.s_inner_t(mesh._r_in_s[r]));
            }
            return peaks_t.ToArray();
        }

        public void processAsync(Config other, Action<long> callback)
        {
            new Thread(new ThreadStart(() =>
            {
                process(other, callback);
            })).Start();
        }

        private void process(Config other, Action<long> callback)
        {

            var watcher = new Stopwatch();
            watcher.Start();

            if (config.seed != other.seed || config.island != other.island)
            {
                Job1ElevationGenerate(other.seed, other.island);
                if (debug()) Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms Job1ElevationGenerate seed:{other.seed}");
            }
            if (config.seed != other.seed || config.mountain_jagged != other.mountain_jagged)
            {
                calculateMountainDistance(
                  other.seed, mesh, peaks_t, other.spacing,
                  other.mountain_jagged,
                    t_mountain_distance
                );
                if (debug()) Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms calculateMountainDistance");
            }
            if (config.seed != other.seed)
            {
                JobPrecomputedNoise(other.seed);
                if (debug()) Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms JobPrecomputedNoise");
            }

            assignElevation(other.noisy_coastlines, other.hill_height, other.ocean_depth);
            if (debug()) Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms assignElevation");

            assignRainfall(other.wind_angle_deg, other.raininess, other.rain_shadow, other.evaporation);
            if (debug()) Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms assignRainfall");

            assignRivers(other.flow);
            if (debug()) Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms assignRivers");
            //saveArray("r_rainfall.txt", r_rainfall);
            //saveArray("t_moisture.txt", t_moisture);
            //saveArray("t_downslope_s.txt", t_downslope_s);
            //saveArray("t_flow.txt", t_flow);
            //saveArray("s_flow.txt", s_flow);

            setRiverGeometry(other.lg_min_flow, other.lg_river_width, other.spacing);
            if (debug()) Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms setRiverGeometry riverCount:{riverCount}");
            setMeshGeometry();
            if (debug()) Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms setMeshGeometry");
            setMapGeometry();
            if (debug()) Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms setMapGeometry");
            config = other;

            watcher.Stop();

            if (callback != null) callback.Invoke(watcher.ElapsedMilliseconds);
        }
        private bool debug() { return false; }
    }
}
