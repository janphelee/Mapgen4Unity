using System;
using Unity.Collections;
using UnityEngine;

using Float = System.Single;

namespace Assets.MapJobs
{
    partial class _MapJobs : IDisposable
    {
        const int CANVAS_SIZE = 128;

        public struct Config
        {
            public int seed;
            public Float spacing;

            public Float island;
            public Float mountain_jagged;
            public Float noisy_coastlines;
            public Float hill_height;
            public Float ocean_depth;

            public Float wind_angle_deg;
            public Float raininess;
            public Float rain_shadow;
            public Float evaporation;

            public Float lg_min_flow;
            public Float lg_river_width;
            public Float flow;
        }

        public Config config { get; set; }

        private DualMesh mesh { get; set; }
        private RiverTexture riverTex { get; set; }
        private int[] peaks_t { get; set; }


        // Job1ElevationGenerate =====================================
        public NativeArray<Float> elevation { get; private set; }// double
        private PreNoise preNoise { get; set; }

        // Job2AssignElevation ========================================
        private NativeArray<Float> t_elevation { get; set; }//海拔三角形
        private NativeArray<Float> t_mountain_distance { get; set; }
        private NativeArray<Float> r_elevation { get; set; }//海拔区域

        // Job4AssignRainfall =========================================
        private NativeArray<Float> r_wind_sort { get; set; }
        private NativeArray<int> wind_order_r { get; set; }
        private NativeArray<Float> r_rainfall { get; set; }//降雨量区域
        private NativeArray<Float> r_humidity { get; set; }//湿度
        private NativeArray<Float> t_moisture { get; set; }//降雨量三角形

        // Job5AssignDownslope ========================================
        private NativeArray<int> order_t { get; set; }//三角形海拔排序
        private NativeArray<int> t_downslope_s { get; set; }//三角形流出的边
        private NativeArray<Float> t_flow { get; set; }//水流量
        private NativeArray<Float> s_flow { get; set; }//水流量

        public NativeArray<Vector3> rivers_v3 { get; set; }
        public NativeArray<Vector2> rivers_uv { get; set; }
        public int riverCount { get; private set; }

        public NativeArray<Vector3> land_v3 { get; set; }
        public NativeArray<Vector2> land_uv { get; set; }
        public NativeArray<int> land_i { get; set; }


        public _MapJobs(DualMesh.Graph graph, short[] peaks_index, Float spacing)
        {
            mesh = new DualMesh(graph);
            riverTex = RiverTexture.createDefault();

            peaks_t = createPeaks(peaks_index, mesh.numBoundaryRegions);

            config = new Config()
            {
                seed = -1,
                island = -1,
                mountain_jagged = -1,
                wind_angle_deg = -1,
            };

            elevation = new NativeArray<Float>(CANVAS_SIZE * CANVAS_SIZE, Allocator.Persistent);
            preNoise = new PreNoise();

            t_elevation = new NativeArray<Float>(mesh.numTriangles, Allocator.Persistent);
            t_mountain_distance = new NativeArray<Float>(mesh.numTriangles, Allocator.Persistent);

            r_elevation = new NativeArray<Float>(mesh.numRegions, Allocator.Persistent);

            r_wind_sort = new NativeArray<Float>(mesh.numRegions, Allocator.Persistent);
            wind_order_r = new NativeArray<int>(mesh.numRegions, Allocator.Persistent);
            r_rainfall = new NativeArray<Float>(mesh.numRegions, Allocator.Persistent);
            r_humidity = new NativeArray<Float>(mesh.numRegions, Allocator.Persistent);
            t_moisture = new NativeArray<Float>(mesh.numTriangles, Allocator.Persistent);

            order_t = new NativeArray<int>(mesh.numTriangles + 1, Allocator.Persistent);
            t_downslope_s = new NativeArray<int>(mesh.numTriangles, Allocator.Persistent);
            t_flow = new NativeArray<Float>(mesh.numTriangles, Allocator.Persistent);
            s_flow = new NativeArray<Float>(mesh.numSides, Allocator.Persistent);

            rivers_v3 = new NativeArray<Vector3>(mesh.numSides, Allocator.Persistent);
            rivers_uv = new NativeArray<Vector2>(mesh.numSides, Allocator.Persistent);
            land_v3 = new NativeArray<Vector3>(mesh.numRegions + mesh.numTriangles, Allocator.Persistent);
            land_uv = new NativeArray<Vector2>(mesh.numRegions + mesh.numTriangles, Allocator.Persistent);
            land_i = new NativeArray<int>(mesh.numSolidSides * 3, Allocator.Persistent);

        }

        public void Dispose()
        {
            mesh.Dispose();
            riverTex.Dispose();

            elevation.Dispose();
            preNoise.Dispose();

            t_elevation.Dispose();
            t_mountain_distance.Dispose();

            r_elevation.Dispose();

            r_wind_sort.Dispose();
            wind_order_r.Dispose();
            r_rainfall.Dispose();
            r_humidity.Dispose();
            t_moisture.Dispose();

            order_t.Dispose();
            t_downslope_s.Dispose();
            t_flow.Dispose();
            s_flow.Dispose();

            rivers_v3.Dispose();
            rivers_uv.Dispose();
            land_v3.Dispose();
            land_uv.Dispose();
            land_i.Dispose();
        }
    }
}
