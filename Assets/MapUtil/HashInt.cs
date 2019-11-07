namespace Assets.MapGen
{
    class HashInt
    {
        static int[] A = new int[1];

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
            A[0] = x | 0;
            A[0] -= (A[0] << 6);
            A[0] ^= moveRight(A[0], 17);
            A[0] -= (A[0] << 9);
            A[0] ^= (A[0] << 4);
            A[0] -= (A[0] << 3);
            A[0] ^= (A[0] << 10);
            A[0] ^= moveRight(A[0], 15);
            return A[0] & int.MaxValue;
        }
    }
}
