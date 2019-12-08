using System;
using System.Collections.Generic;

namespace Assets.MapUtil
{
    class PoissonMeta
    {
        /**
         * Get the neighbourhood ordered by distance, including the origin point
         * @param {int} dimensionNumber Number of dimensions
         * @returns {Array} Neighbourhood
         */
        public static List<int[]> getNeighbourhood(int dimensionNumber)
        {
            var neighbourhood = moore(2, dimensionNumber);

            var origin = new List<int>();
            for (int dimension = 0; dimension < dimensionNumber; dimension++)
            {
                origin.Add(0);
            }
            neighbourhood.Add(origin.ToArray());

            // sort by ascending distance to optimize proximity checks
            // see point 5.1 in Parallel Poisson Disk Sampling by Li-Yi Wei, 2008
            // http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.460.3061&rank=1
            neighbourhood.Sort((n1, n2) =>
            {
                var squareDist1 = 0;
                var squareDist2 = 0;

                for (var dimension = 0; dimension < dimensionNumber; dimension++)
                {
                    squareDist1 += (int)Math.Pow(n1[dimension], 2);
                    squareDist2 += (int)Math.Pow(n2[dimension], 2);
                }

                if (squareDist1 < squareDist2)
                {
                    return -1;
                }
                else if (squareDist1 > squareDist2)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            });

            return neighbourhood;
        }

        /**
         * Get the squared euclidean distance from two points of arbitrary, but equal, dimensions
         * @param {Array} point1
         * @param {Array} point2
         * @returns {number} Squared euclidean distance
         */
        public static float squaredEuclideanDistance(float[] point1, float[] point2)
        {
            float result = 0;
            for (int i = 0; i < point1.Length; ++i)
            {
                result += (float)Math.Pow(point1[i] - point2[i], 2);
            }
            return result;
        }

        /**
         * @param {int} d Dimensions
         * @param {Function} rng
         * @returns {Array}
         */
        public static float[] sphereRandom(int d, Rander.RandFloat rng)
        {
            var v = new float[d];
            int d2 = (int)Math.Floor(d / 2.0) << 1, i;

            double r2 = 0.0,
             rr,
             r,
             theta,
             h;

            for (i = 0; i < d2; i += 2)
            {
                rr = -2.0 * Math.Log(rng());
                r = Math.Sqrt(rr);
                theta = 2.0 * Math.PI * rng();

                r2 += rr;
                v[i] = (float)(r * Math.Cos(theta));
                v[i + 1] = (float)(r * Math.Sin(theta));
            }

            if (d % 2 != 0)
            {
                var x = Math.Sqrt(-2.0 * Math.Log(rng())) * Math.Cos(2.0 * Math.PI * rng());
                v[d - 1] = (float)x;
                r2 += Math.Pow(x, 2);
            }

            h = 1.0 / Math.Sqrt(r2);

            for (i = 0; i < d; ++i)
            {
                v[i] *= (float)h;
            }

            return v;
        }

        public static List<int[]> moore(int range, int dimensions)
        {
            //range = range || 1
            //dimensions = dimensions || 2

            var size = range * 2 + 1;
            var length = (int)Math.Pow(size, dimensions) - 1;
            var neighbors = new List<int[]>(length);

            for (var i = 0; i < length; i++)
            {
                var neighbor = neighbors[i] = new int[dimensions];
                var index = i < length / 2 ? i : i + 1;
                for (var dimension = 1; dimension <= dimensions; dimension++)
                {
                    var value = index % (int)Math.Pow(size, dimension);
                    neighbor[dimension - 1] = (int)(value / Math.Pow(size, dimension - 1)) - range;
                    index -= value;
                }
            }
            return neighbors;
        }

        public static void tinyNDArray(List<int> gridShape, Action<int[], int[]> callback)
        {
            var dimensions = gridShape.Count;
            var totalLength = 1;
            var stride = new int[dimensions];

            for (var dimension = dimensions; dimension > 0; dimension--)
            {
                stride[dimension - 1] = totalLength;
                totalLength = totalLength * gridShape[dimension - 1];
            }
            var data = new int[totalLength];
            callback(stride, data);
        }
    }
}
