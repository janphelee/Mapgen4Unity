using UnityEngine;
using System;
using Unity.Collections;
using System.Threading.Tasks;

namespace Thanks.Planet
{
    public class Geometry : IDisposable
    {
        public bool quad { get; set; }

        public NativeArray<Vector3> flat_xyz { get; private set; }
        public NativeArray<Vector2> flat_em { get; private set; }
        public NativeArray<int> flat_i { get; private set; }

        public Geometry(int numSides)
        {
            flat_xyz = new NativeArray<Vector3>(numSides * 3, Allocator.Persistent);
            flat_em = new NativeArray<Vector2>(numSides * 3, Allocator.Persistent);
            flat_i = new NativeArray<int>(numSides * 3, Allocator.Persistent);

            var I = flat_i;
            Parallel.For(0, I.Length, i => I[i] = I.Length - 1 - i);
            quad = false;
        }

        public Geometry(int numSides, int numRegions, int numTriangles)
        {
            flat_xyz = new NativeArray<Vector3>(numRegions + numTriangles, Allocator.Persistent);
            flat_em = new NativeArray<Vector2>(numRegions + numTriangles, Allocator.Persistent);
            flat_i = new NativeArray<int>(numSides * 3, Allocator.Persistent);
            quad = true;
        }

        public virtual void Dispose()
        {
            flat_xyz.Dispose();
            flat_em.Dispose();
            flat_i.Dispose();
        }
    }

}