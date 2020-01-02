namespace Phevolution
{
    public class Random
    {
        private static Rander random { get; set; }

        public static void Seed(int seed)
        {
            random = new Rander(seed);
        }
        public static int Next()
        {
            return random.Next();
        }
        public static int Next(int maxValue)
        {
            return random.Next(maxValue);
        }
        public static int Next(int minValue, int maxValue)
        {
            return random.Next(minValue, maxValue);
        }
        public static double NextDouble()
        {
            return random.NextDouble();
        }
    }
}