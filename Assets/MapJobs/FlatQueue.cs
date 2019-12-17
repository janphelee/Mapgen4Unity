using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Assets.MapJobs
{
    unsafe struct FlatQueue : IDisposable
    {
        private NativeArray<int> __k;
        private NativeArray<double> __v;

        private int* pk;
        private double* pv;
        private int length;

        public FlatQueue(int size)
        {
            __k = new NativeArray<int>(size, Allocator.TempJob);
            __v = new NativeArray<double>(size, Allocator.TempJob);

            pk = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(__k);
            pv = (double*)NativeArrayUnsafeUtility.GetUnsafePtr(__v);
            length = 0;
        }

        public void Dispose()
        {
            __k.Dispose();
            __v.Dispose();
        }

        public void push(int key, double value)
        {
            pk[length] = key;
            pv[length] = value;
            var pos = length++;

            while (pos > 0)
            {
                int parent = (pos - 1) >> 1;
                var parentValue = pv[parent];
                if (value >= parentValue) break;

                pk[pos] = pk[parent];
                pv[pos] = pv[parent];
                pos = parent;
            }

            pk[pos] = key;
            pv[pos] = value;
        }

        public int pop()
        {
            var top = pk[0];
            length--;

            if (length > 0)
            {
                var key = pk[0] = pk[length];
                var value = pv[0] = pv[length];
                var halfLength = length >> 1;

                var pos = 0;

                while (pos < halfLength)
                {
                    var left = (pos << 1) + 1;
                    var right = left + 1;

                    var bestIndex = pk[left];
                    var bestValue = pv[left];
                    var rightValue = pv[right];

                    if (right < length && rightValue < bestValue)
                    {
                        left = right;
                        bestIndex = pk[right];
                        bestValue = rightValue;
                    }
                    if (bestValue >= value) break;

                    pk[pos] = bestIndex;
                    pv[pos] = bestValue;
                    pos = left;
                }

                pk[pos] = key;
                pv[pos] = value;
            }
            return top;
        }

    }
}
