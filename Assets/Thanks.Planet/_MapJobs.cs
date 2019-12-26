using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

#if Use_Double_Float
using Float = System.Double;
#else
using Float = System.Single;
#endif

namespace Thanks.Planet
{
    partial class _MapJobs : IDisposable
    {
        public DualMesh mesh { get; set; }

        private NativeArray<Float> r_xyz { get; set; }
        private NativeArray<Float> t_xyz { get; set; }

        private NativeArray<Float> r_elevation { get; set; }
        private NativeArray<Float> t_elevation { get; set; }
        private NativeArray<Float> r_moisture { get; set; }
        private NativeArray<Float> t_moisture { get; set; }

        private NativeArray<int> t_downflow_s { get; set; }
        private NativeArray<int> order_t { get; set; }
        private NativeArray<Float> t_flow { get; set; }
        private NativeArray<Float> s_flow { get; set; }


        private HashSet<int> plate_r { get; set; }

        private NativeArray<int> r_plate { get; set; }

        private NativeArray<Vector3> plate_vec { get; set; }

        private HashSet<int> plate_is_ocean { get; set; }


        private SimplexNoise _randomNoise { get; set; }
        private NativeArray<byte> tempBuffer { get; set; }

        public Geometry geometry { get; private set; }

        public void Dispose()
        {
            mesh.Dispose();
            r_xyz.Dispose();
            t_xyz.Dispose();
            r_elevation.Dispose();
            t_elevation.Dispose();
            r_moisture.Dispose();
            t_moisture.Dispose();
            t_downflow_s.Dispose();
            order_t.Dispose();
            t_flow.Dispose();
            s_flow.Dispose();

            plate_r.Clear();
            r_plate.Dispose();
            plate_vec.Dispose();
            plate_is_ocean.Clear();

            tempBuffer.Dispose();
            _randomNoise = null;

            geometry.Dispose();
        }
    }
}