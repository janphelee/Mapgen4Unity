namespace Assets.MapGen
{
    class Rander
    {
        public delegate int RandInt(int N);
        public delegate float RandFloat();

        public static RandInt makeRandInt(int seed)
        {
            int i = 0;
            return (N) =>
            {
                i++;
                var x = HashInt.hashInt(seed + i) % N;
                // if (i < 101) UnityEngine.Debug.Log($"seed:{seed} i:{i} x:{x}");
                return x;
            };
        }

        public static RandFloat makeRandFloat(int seed)
        {
            var rand = makeRandInt(seed);
            var divisor = 0x10000000;
            return () =>
            {
                return rand(divisor) / (float)divisor;
            };
        }

    }
}
