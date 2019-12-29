using Unity.Collections;

#if Use_Double_Float
using Float = System.Double;
#else
using Float = System.Single;
#endif

class Rander
{
    const int DIVISOR = 0x10000000;

    public delegate int RandInt(int N);
    public delegate Float RandFloat();
    public delegate double RandDouble();

    public static RandInt makeRandInt(int seed)
    {
        int i = 0;
        return (N) =>
        {
            i++;
            var x = (uint)HashInt.hashInt(seed + i);
            // if (i < 101) UnityEngine.Debug.Log($"seed:{seed} i:{i} x:{x}");
            return (int)(x % N);
        };
    }

    public static RandFloat makeRandFloat(int seed)
    {
        var rand = makeRandInt(seed);
        return () =>
        {
            return rand(DIVISOR) / (Float)DIVISOR;
        };
    }

    public static RandDouble makeRandDouble(int seed)
    {
        var rand = makeRandInt(seed);
        return () =>
        {
            return rand(DIVISOR) / (double)DIVISOR;
        };
    }


    public static Float randFloat(int seed, int index)
    {
        var x = randInt(DIVISOR, seed, index);
        return (Float)x / DIVISOR;
    }

    public static int randInt(int x, int seed, int index)
    {
        return HashInt.hashInt(seed + index + 1) % x; ;
    }

    public static void randArray(int seed, int index, Float[] buffer)
    {
        for (int i = 0; i < buffer.Length; ++i)
        {
            buffer[i] = randFloat(seed, index + i);
        }
    }

    public static void randArray(int seed, int index, NativeArray<Float> buffer)
    {
        for (int i = 0; i < buffer.Length; ++i)
        {
            buffer[i] = randFloat(seed, index + i);
        }
    }

}