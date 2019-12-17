using Unity.Collections;

namespace Assets.MapJobs
{
    struct Rander
    {
        const int DIVISOR = 0x10000000;

        public static int moveRight(int x, int n)
        {
            int mask = 0x7fffffff; //Integer.MAX_VALUE
            for (int i = 0; i < n; i++)
            {
                x >>= 1;
                x &= mask;
            }
            return x;
        }

        public static int hashInt(int x)
        {
            var AA = x | 0;
            AA -= (AA << 6);
            AA ^= moveRight(AA, 17);
            AA -= (AA << 9);
            AA ^= (AA << 4);
            AA -= (AA << 3);
            AA ^= (AA << 10);
            AA ^= moveRight(AA, 15);
            return AA & int.MaxValue;
        }

        public static float randFloat(int seed, int index)
        {
            var x = randInt(DIVISOR, seed, index);
            return (float)x / DIVISOR;
        }

        public static int randInt(int x, int seed, int index)
        {
            return hashInt(seed + index + 1) % x; ;
        }

        public static void randArray(int seed, int index, float[] buffer)
        {
            for (int i = 0; i < buffer.Length; ++i)
            {
                buffer[i] = randFloat(seed, index + i);
            }
        }
        public static void randArray(int seed, int index, NativeArray<float> buffer)
        {
            for (int i = 0; i < buffer.Length; ++i)
            {
                buffer[i] = randFloat(seed, index + i);
            }
        }
    }
}
