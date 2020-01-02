using Phevolution;
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
    public partial class _MapJobs : JobThread, IDisposable
    {
        public DualMesh mesh { get; private set; }

        private NativeArray<double> r_xyz { get; set; }
        private NativeArray<double> t_xyz { get; set; }

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


        private SimplexNoise simplex { get; set; }

        public Geometry geometry { get; private set; }


        public _MapJobs()
        {
            config = new IChanged[] {
                new ChangeI(0,/* seed */ 123, onConfig),
                new ChangeI(1,/* N */ 10000, onConfig),
                new ChangeI(2,/* P */ 20, onConfig),
                new ChangeF(3,/* jitter */ 0.75f, onConfig),
                new ChangeI(4,/* drawMode 0 flat/ 1 quad */ 0, onConfig),
                new ChangeB(5,/* draw_plateVectors */ false, onConfig),
                new ChangeB(6,/* draw_plateBoundaries */ false, onConfig),
            };
        }

        private void disposeMesh()
        {
            if (mesh != null)
            {
                mesh.Dispose();
                mesh = null;
            }
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

            r_plate.Dispose();
            plate_vec.Dispose();

            geometry.Dispose();
        }

        public void Dispose()
        {
            plate_r.Clear();
            plate_is_ocean.Clear();

            disposeMesh();

            if (simplex != null)
            {
                simplex.Dispose();
                simplex = null;
            }
        }
    }
}