using System.Collections.Generic;
using System;
using Phevolution;

#if Use_Double_Float
using Float = System.Double;
using Float2 = Unity.Mathematics.double2;
#else
using Float = System.Single;
using Float2 = Unity.Mathematics.float2;
#endif

partial class DualMesh
{
    public static void makeSphere(int N, double jitter, int seed, out DualMesh mesh, out List<double> d)
    {
        var latlong = generateFibonacciSphere(N, jitter, seed);
        //DebugHelper.SaveArray("generateFibonacciSphere.txt", latlong);
        var r_xyz = new List<double>();
        for (var r = 0; r < latlong.Count / 2; r++)
        {
            pushCartesianFromSpherical(r_xyz, latlong[2 * r], latlong[2 * r + 1]);
        }

        var r_xy = stereographicProjection(r_xyz);
        //DebugHelper.SaveArray("stereographicProjection.txt", r_xy);
        var delaunay = new Delaunator(r_xy.ToArray());

        /* TODO: rotate an existing point into this spot instead of creating one */
        r_xyz.AddRange(new double[] { 0, 0, 1 });
        addSouthPoleToMesh(r_xyz.Count / 3 - 1, delaunay);

        var dummy_r_vertex = new Float2[N + 1];
        dummy_r_vertex[0] = new Float2(0, 0);
        for (var i = 1; i < N + 1; i++)
        {
            dummy_r_vertex[i] = dummy_r_vertex[0];
        }

        mesh = new DualMesh(new Graph()
        {
            numBoundaryRegions = 0,
            numSolidSides = delaunay.triangles.Length,
            _r_vertex = dummy_r_vertex,
            _triangles = delaunay.triangles,
            _halfedges = delaunay.halfedges,
        });
        d = r_xyz;
    }
    /* calculate x,y,z from spherical coordinates lat,lon and then push
     * them onto out array; for one-offs pass [] as the first argument */
    private static void pushCartesianFromSpherical(List<double> r_xyz, double latDeg, double lonDeg)
    {
        var latRad = latDeg / 180.0 * Math.PI;
        var lonRad = lonDeg / 180.0 * Math.PI;
        r_xyz.Add(Math.Cos(latRad) * Math.Cos(lonRad));
        r_xyz.Add(Math.Cos(latRad) * Math.Sin(lonRad));
        r_xyz.Add(Math.Sin(latRad));
    }



    /** Add south pole back into the mesh.
     *
     * We run the Delaunay Triangulation on all points *except* the south
     * pole, which gets mapped to infinity with the stereographic
     * projection. This function adds the south pole into the
     * triangulation. The Delaunator guide explains how the halfedges have
     * to be connected to make the mesh work.
     * <https://mapbox.github.io/delaunator/>
     *
     * Returns the new {triangles, halfedges} for the triangulation with
     * one additional point added around the convex hull.
     */
    private static void addSouthPoleToMesh(int southPoleId, Delaunator delaunator)
    {
        // This logic is from <https://github.com/redblobgames/dual-mesh>,
        // where I use it to insert a "ghost" region on the "back" side of
        // the planar map. The same logic works here. In that code I use
        // "s" for edges ("sides"), "r" for regions ("points"), t for triangles
        var triangles = delaunator.triangles;
        var halfedges = delaunator.halfedges;

        var numSides = triangles.Length;
        //int s_next_s(int s) { return (s % 3 == 2) ? s - 2 : s + 1; }

        int numUnpairedSides = 0, firstUnpairedSide = -1;
        var pointIdToSideId = new Dictionary<int, int>(); // seed to side
        for (var s = 0; s < numSides; s++)
        {
            if (halfedges[s] == -1)
            {
                numUnpairedSides++;
                pointIdToSideId[triangles[s]] = s;
                firstUnpairedSide = s;
            }
        }

        var newTriangles = new int[numSides + 3 * numUnpairedSides];
        var newHalfedges = new int[numSides + 3 * numUnpairedSides];
        Array.Copy(triangles, newTriangles, triangles.Length);
        Array.Copy(halfedges, newHalfedges, halfedges.Length);

        for (int i = 0, s = firstUnpairedSide;
             i < numUnpairedSides;
             i++, s = pointIdToSideId[newTriangles[s_next_s(s)]])
        {

            // Construct a pair for the unpaired side s
            var newSide = numSides + 3 * i;
            newHalfedges[s] = newSide;
            newHalfedges[newSide] = s;
            newTriangles[newSide] = newTriangles[s_next_s(s)];

            // Construct a triangle connecting the new side to the south pole
            newTriangles[newSide + 1] = newTriangles[s];
            newTriangles[newSide + 2] = southPoleId;
            var k = numSides + (3 * i + 4) % (3 * numUnpairedSides);
            newHalfedges[newSide + 2] = k;
            newHalfedges[k] = newSide + 2;
        }

        delaunator.triangles = newTriangles;
        delaunator.halfedges = newHalfedges;
    }

    private static List<double> stereographicProjection(List<double> r_xyz)
    {
        // See <https://en.wikipedia.org/wiki/Stereographic_projection>
        //var degToRad = Math.PI / 180;
        var numPoints = r_xyz.Count / 3;
        var r_XY = new List<double>();
        for (var r = 0; r < numPoints; r++)
        {
            double x = r_xyz[3 * r],
                   y = r_xyz[3 * r + 1],
                   z = r_xyz[3 * r + 2];
            double X = x / (1 - z),
                   Y = y / (1 - z);

            r_XY.Add(X);
            r_XY.Add(Y);
        }
        return r_XY;
    }

    private static List<double> generateFibonacciSphere(int N = 10000, double jitter = 0.75f, int seed = 123)
    {
        var randFloat = Rander.makeRandDouble(seed);

        var a_latlong = new List<double>();

        // Second algorithm from http://web.archive.org/web/20120421191837/http://www.cgafaq.info/wiki/Evenly_distributed_points_on_sphere
        var s = 3.6 / Math.Sqrt(N);
        double dlong = Math.PI * (3 - Math.Sqrt(5)),  /* ~2.39996323 */
              _long = 0,
              dz = 2.0 / N,
              _z = 1 - dz / 2;
        for (int k = 0; k < N; k++)
        {
            var r = Math.Sqrt(1 - _z * _z);
            var latDeg = Math.Asin(_z) * 180 / Math.PI;
            var lonDeg = _long * 180 / Math.PI;
            //if (_randomLat[k] === undefined) _randomLat[k] = randFloat() - randFloat();
            //if (_randomLon[k] === undefined) _randomLon[k] = randFloat() - randFloat();
            var _randomLat = randFloat() - randFloat();
            var _randomLon = randFloat() - randFloat();
            latDeg += jitter * _randomLat * (latDeg - Math.Asin(Math.Max(-1, _z - dz * 2 * Math.PI * r / s)) * 180 / Math.PI);
            lonDeg += jitter * _randomLon * (s / r * 180 / Math.PI);

            a_latlong.Add(latDeg);
            a_latlong.Add(lonDeg % 360.0);

            _long += dlong;
            _z -= dz;
        }
        return a_latlong;
    }
}
