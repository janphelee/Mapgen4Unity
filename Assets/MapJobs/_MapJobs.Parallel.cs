using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.MapJobs
{
    using Float = Double;
    using Float2 = double2;

    partial class _MapJobs
    {
        private void Job1ElevationGenerate(int seed, Float island)
        {
            var tmp_buffer_t = new NativeArray<byte>(2048, Allocator.TempJob);
            var simplex = new SimplexNoise(seed, tmp_buffer_t);
            var job = new Job1ElevationGenerate()
            {
                size = CANVAS_SIZE,
                island = island,
                simplex = simplex,
                elevation = elevation
            };
            Parallel.For(0, elevation.Length, i =>
            {
                job.Execute(i);
            });
            tmp_buffer_t.Dispose();
        }

        private void JobPrecomputedNoise(int seed)
        {
            var tmp_buffer_t = new NativeArray<byte>(2048, Allocator.TempJob);
            var simplex = new SimplexNoise(seed, tmp_buffer_t);
            var job = preNoise.create(simplex, mesh._t_vertex);
            Parallel.For(0, mesh.numTriangles, i =>
            {
                job.Execute(i);
            });
            tmp_buffer_t.Dispose();
        }


        public unsafe void assignElevation(Float noisy_coastlines, Float hill_height, Float ocean_depth)
        {
            var job1 = new Job2AssignSolidTriangle()
            {
                elevation = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(elevation),
                paintSize = CANVAS_SIZE,
                noisy_coastlines = noisy_coastlines,
                t_noise4 = preNoise.t_noise4,
                t_noise5 = preNoise.t_noise5,
                t_noise6 = preNoise.t_noise6,
                t_vertex = mesh._t_vertex,
                t_elevation = t_elevation
            };
            int mountain_slope = 20;
            var job2 = new Job2AssignTriangleElevation()
            {
                t_noise0 = preNoise.t_noise0,
                t_noise1 = preNoise.t_noise1,
                t_noise2 = preNoise.t_noise2,
                t_noise4 = preNoise.t_noise4,
                t_mountain_distance = t_mountain_distance,
                hill_height = hill_height,
                ocean_depth = ocean_depth,
                mountain_slope = mountain_slope,
                t_elevation = t_elevation
            };
            var job3 = new Job3AssignRegionElevation()
            {
                _r_in_s = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._r_in_s),
                _halfedges = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._halfedges),
                t_elevation = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(t_elevation),
                r_elevation = r_elevation,
            };
            Parallel.For(0, mesh.numSolidTriangles, i =>
            {
                job1.Execute(i);
            });
            Parallel.For(0, mesh.numTriangles, i =>
            {
                job2.Execute(i);
            });
            Parallel.For(0, mesh.numRegions, i =>
            {
                job3.Execute(i);
            });
        }

        public unsafe void assignRainfall(Float wind_angle_deg, Float raininess, Float rain_shadow, Float evaporation)
        {
            int numRegions = mesh.numRegions;
            var _r_in_s = mesh._r_in_s;
            var _halfedges = mesh._halfedges;

            if (config.wind_angle_deg != wind_angle_deg)
            {
                var rad = Mathf.Deg2Rad * wind_angle_deg;
                var vec = new Float2(Math.Cos(rad), Math.Sin(rad));

                var job1 = new Job4AssignAngleWind()
                {
                    windAngleVec = vec,
                    r_vertex = mesh._r_vertex,
                    r_wind_sort = r_wind_sort,
                };
                Parallel.For(0, numRegions, i =>
                {
                    job1.Execute(i);
                });
                //风向排序========================>>>>>>
                var order_r = new int[numRegions];
                for (int i = 0; i < order_r.Length; ++i) order_r[i] = i;
                Array.Sort(r_wind_sort.ToArray(), order_r);
                wind_order_r.CopyFrom(order_r);
            }

            var job3 = new Job4AssignRainfall()
            {
                raininess = raininess,
                rain_shadow = rain_shadow,
                evaporation = evaporation,

                numBoundaryRegions = mesh.numBoundaryRegions,
                _triangles = mesh._triangles,
                _halfedges = mesh._halfedges,
                _r_in_s = mesh._r_in_s,
                r_elevation = r_elevation,
                r_wind_sort = r_wind_sort,
                wind_order_r = wind_order_r,
                r_rainfall = r_rainfall,
                r_humidity = r_humidity,
            };
            var job4 = new Job4AssignTriangleMoisture()
            {
                _triangles = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._triangles),
                r_rainfall = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(r_rainfall),
                t_moisture = t_moisture
            };
            job3.Execute();
            Parallel.For(0, mesh.numTriangles, i =>
            {
                job4.Execute(i);
            });
        }

        public unsafe void assignRivers(Float flow)
        {
            var numTriangles = mesh.numTriangles;

            var job1 = new Job5AssignDownslope()
            {
                numTriangles = numTriangles,
                _halfedges = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._halfedges),
                t_elevation = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(t_elevation),
                order_t = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(order_t),
                t_downslope_s = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(t_downslope_s),
            };
            var job2 = new Job5AssignFlow()
            {
                flow = flow,
                numTriangles = numTriangles,
                _halfedges = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._halfedges),
                t_elevation = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(t_elevation),
                order_t = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(order_t),
                t_downslope_s = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(t_downslope_s),
                t_moisture = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(t_moisture),
                t_flow = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(t_flow),
                s_flow = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(s_flow),
            };
            job1.Execute();
            job2.Execute();
        }

        public unsafe void setRiverGeometry(Float lg_min_flow, Float lg_river_width, Float spacing)
        {
            var MIN_FLOW = Math.Exp(lg_min_flow);
            var RIVER_WIDTH = Math.Exp(lg_river_width);

            var flow_out_s = new NativeArray<int>(mesh.numSides, Allocator.TempJob);
            var rivers_cnt = new NativeArray<int>(1, Allocator.TempJob);

            var job1 = new Job6SetFlowOutSide()
            {
                MIN_FLOW = MIN_FLOW,
                numSolidTriangles = mesh.numSolidTriangles,
                _halfedges = mesh._halfedges,
                t_downslope_s = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(t_downslope_s),
                s_flow = s_flow,
                flow_out_s = flow_out_s,
                result = rivers_cnt,
            };
            job1.Execute();

            var job2 = new Job6SetRiverGeomerty()
            {
                numRiverSizes = riverTex.NumSizes,
                MIN_FLOW = MIN_FLOW,
                RIVER_WIDTH = RIVER_WIDTH,
                spacing = spacing,
                _halfedges = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._halfedges),
                _triangles = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._triangles),
                _r_vertex = (Float2*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._r_vertex),
                s_flow = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(s_flow),
                s_length = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh.s_length),
                flow_out_s = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(flow_out_s),
                t_downslope_s = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(t_downslope_s),
                river_uv = (Vector2*)NativeArrayUnsafeUtility.GetUnsafePtr(riverTex.uv),
                vertex = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(rivers_v3),
                uv = (Vector2*)NativeArrayUnsafeUtility.GetUnsafePtr(rivers_uv),
            };

            riverCount = rivers_cnt[0];
            //Debug.Log($"rivers_cnt[0]:{rivers_cnt[0]}");

            Parallel.For(0, riverCount, i =>
            {
                job2.Execute(i);
            });
            //for (int i = 0; i < rivers_cnt[0]; ++i) job2.Execute(i);

            flow_out_s.Dispose();
            rivers_cnt.Dispose();
        }

        public unsafe void setMeshGeometry()
        {
            int numRegions = mesh.numRegions, numTriangles = mesh.numTriangles;
            var job = new Job7SetMeshGeomerty()
            {
                numRegions = numRegions,
                _r_vertex = (Float2*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._r_vertex),
                _t_vertex = (Float2*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._t_vertex),
                vertex = land_v3,
            };
            Parallel.For(0, numRegions + numTriangles, i =>
            {
                job.Execute(i);
            });
        }

        public unsafe void setMapGeometry()
        {
            var job1 = new Job8SetMapElevation()
            {
                numRegions = mesh.numRegions,
                _triangles = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._triangles),
                r_elevation = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(r_elevation),
                r_rainfall = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(r_rainfall),
                t_elevation = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(t_elevation),
                uv = land_uv,
            };
            var job2 = new Job8SetMapTriangles()
            {
                numRegions = mesh.numRegions,
                _triangles = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._triangles),
                _halfedges = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(mesh._halfedges),
                r_elevation = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(r_elevation),
                s_flow = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(s_flow),
                I = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(land_i),
            };
            Parallel.For(0, land_uv.Length, i =>
            {
                job1.Execute(i);
            });
            Parallel.For(0, mesh.numSolidSides, i =>
            {
                job2.Execute(i);
            });
        }
    }
}
