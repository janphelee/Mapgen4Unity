using Unity.Collections.LowLevel.Unsafe;
using System;
using Unity.Collections;
using System.Threading.Tasks;
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
        private static void CentroidOfTriangle(
            double* r_xyz, int a, int b, int c,
            double* t_xyz, int t
            )
        {
            var ax = r_xyz[a * 3 + 0];
            var ay = r_xyz[a * 3 + 1];
            var az = r_xyz[a * 3 + 2];

            var bx = r_xyz[b * 3 + 0];
            var by = r_xyz[b * 3 + 1];
            var bz = r_xyz[b * 3 + 2];

            var cx = r_xyz[c * 3 + 0];
            var cy = r_xyz[c * 3 + 1];
            var cz = r_xyz[c * 3 + 2];

            // TODO: renormalize to radius 1
            t_xyz[t * 3 + 0] = (ax + bx + cx) / 3;
            t_xyz[t * 3 + 1] = (ay + by + cy) / 3;
            t_xyz[t * 3 + 2] = (az + bz + cz) / 3;
        }

        private static void assignTriangleCenters(DualMesh mesh, NativeArray<double> r_xyz, NativeArray<double> t_xyz)
        {
            var numTriangles = mesh.numTriangles;
            var _r_xyz = (double*)NativeArrayUnsafeUtility.GetUnsafePtr(r_xyz);
            var _t_xyz = (double*)NativeArrayUnsafeUtility.GetUnsafePtr(t_xyz);

            Action<int> action = t =>
            {
                int a = mesh.s_begin_r(3 * t + 0),
                    b = mesh.s_begin_r(3 * t + 1),
                    c = mesh.s_begin_r(3 * t + 2);
                CentroidOfTriangle(_r_xyz, a, b, c, _t_xyz, t);
            };
            //for (var t = 0; t < numTriangles; t++) action(t);
            Parallel.For(0, numTriangles, action);
        }

        void assignMoisture()
        {
            // TODO: assign region moisture in a better way!
            var r_moisture = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(this.r_moisture);
            for (var r = 0; r < mesh.numRegions; r++)
            {
                r_moisture[r] = r_plate[r] % 10 / 10.0f;
            }
        }

        void assignTriangleValues()
        {
            var mesh = this.mesh;
            var t_elevation = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(this.t_elevation);
            var t_moisture = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(this.t_moisture);
            var numTriangles = mesh.numTriangles;

            Action<int> action = t =>
            {
                var s0 = 3 * t;
                int r1 = mesh.s_begin_r(s0),
                    r2 = mesh.s_begin_r(s0 + 1),
                    r3 = mesh.s_begin_r(s0 + 2);
                t_elevation[t] = 1f / 3 * (r_elevation[r1] + r_elevation[r2] + r_elevation[r3]);
                t_moisture[t] = 1f / 3 * (r_moisture[r1] + r_moisture[r2] + r_moisture[r3]);
            };
            //for (var t = 0; t < numTriangles; t++) action(t);
            Parallel.For(0, numTriangles, action);
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


        //===================================================================================
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
                xyz[r] = new Vector3((float)r_xyz[r * 3 + 0], (float)r_xyz[r * 3 + 1], (float)r_xyz[r * 3 + 2]);
                tm[r] = new Vector2(r_elevation[r], r_moisture[r]);
            });
            Parallel.For(0, numTriangles, t =>
            {
                xyz[numRegions + t] = new Vector3((float)t_xyz[t * 3 + 0], (float)t_xyz[t * 3 + 1], (float)t_xyz[t * 3 + 2]);
                tm[numRegions + t] = new Vector2(t_elevation[t], t_moisture[t]);
            });

            //int count_valley = 0, count_ridge = 0;
            Action<int> action = s =>
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
                    I[s * 3 + 0] = r1;
                    I[s * 3 + 1] = numRegions + t2;
                    I[s * 3 + 2] = numRegions + t1;
                    //count_valley++;
                }
                else
                {
                    // It's a ridge
                    I[s * 3 + 0] = r1;
                    I[s * 3 + 1] = r2;
                    I[s * 3 + 2] = numRegions + t1;
                    //count_ridge++;
                }
            };
            //for (var s = 0; s < numSides; s++) action(s);
            Parallel.For(0, numSides, action);
        }

        private void generateVoronoiGeometry()
        {
            var numSides = mesh.numSides;

            var e = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(this.r_elevation);
            var m = (Float*)NativeArrayUnsafeUtility.GetUnsafePtr(this.r_moisture);

            var xyz = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(geometry.flat_xyz);
            var tm = (Vector2*)NativeArrayUnsafeUtility.GetUnsafePtr(geometry.flat_em);

            Action<int> action = s =>
            {
                int inner_t = mesh.s_inner_t(s),
                    outer_t = mesh.s_outer_t(s),
                    begin_r = mesh.s_begin_r(s);
                var rgb = new Vector2(e[begin_r], m[begin_r]);
                xyz[s * 3 + 0] = new Vector3((float)t_xyz[3 * inner_t], (float)t_xyz[3 * inner_t + 1], (float)t_xyz[3 * inner_t + 2]);
                xyz[s * 3 + 1] = new Vector3((float)t_xyz[3 * outer_t], (float)t_xyz[3 * outer_t + 1], (float)t_xyz[3 * outer_t + 2]);
                xyz[s * 3 + 2] = new Vector3((float)r_xyz[3 * begin_r], (float)r_xyz[3 * begin_r + 1], (float)r_xyz[3 * begin_r + 2]);
                tm[s * 3 + 0] = rgb;
                tm[s * 3 + 1] = rgb;
                tm[s * 3 + 2] = rgb;
            };
            //for (var s = 0; s < numSides; s++) action(s);
            Parallel.For(0, numSides, action);
        }

    }
}