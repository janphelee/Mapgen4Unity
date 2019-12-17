using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Unity.Collections;
using UnityEngine;

namespace Assets.MapJobs
{
    using Debug = UnityEngine.Debug;
    partial class _MapJobs
    {
        public static void calculateMountainDistance(int seed, MeshData mesh, int[] peak_t, float spacing, float jaggedness, NativeArray<float> t_distance)
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
                config.island = other.island;
                Job1ElevationGenerate(other.seed);
                //Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms Job1ElevationGenerate");
            }

            if (config.seed != other.seed || config.mountainJaggedness != other.mountainJaggedness)
            {
                config.mountainJaggedness = other.mountainJaggedness;

                calculateMountainDistance(
                  other.seed, mesh, peaks_t, config.spacing,
                  config.mountainJaggedness,
                    t_mountain_distance
                );
                //Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms calculateMountainDistance");
            }

            if (config.seed != other.seed)
            {
                config.seed = other.seed;
                JobPrecomputedNoise(other.seed);
                //Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms JobPrecomputedNoise");
            }

            assignElevation();
            //Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms assignElevation");

            assignRainfall();
            //Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms assignRainfall");

            assignRivers(config.flow);
            //Debug.Log($"process Elapsed:{watcher.ElapsedMilliseconds}ms assignRivers");
            //saveArray("r_rainfall.txt", r_rainfall);
            //saveArray("t_moisture.txt", t_moisture);
            //saveArray("t_downslope_s.txt", t_downslope_s);
            //saveArray("t_flow.txt", t_flow);
            //saveArray("s_flow.txt", s_flow);

            setRiverGeometry();
            //debugLog($"process Elapsed:{watcher.ElapsedMilliseconds}ms setRiverGeometry riverCount:{riverCount}");
            setMeshGeometry();
            //debugLog($"process Elapsed:{watcher.ElapsedMilliseconds}ms setMeshGeometry");
            setMapGeometry();
            //debugLog($"process Elapsed:{watcher.ElapsedMilliseconds}ms setMapGeometry");

            watcher.Stop();

            if (callback != null) callback.Invoke(watcher.ElapsedMilliseconds);
        }

        private void debugLog(string msg)
        {
            Debug.Log(msg);
        }

        private void saveArray<T>(string fileName, NativeArray<T> d, int limit = int.MaxValue) where T : struct
        {
            var fileInfo = new FileInfo($"{Application.streamingAssetsPath}/{fileName}");
            var streamWriter = fileInfo.CreateText();
            for (var i = 0; i < d.Length && i < limit; ++i)
                streamWriter.WriteLine($"{i} {d[i]}");
            streamWriter.Close();
            streamWriter.Dispose();
        }
    }
}
