using System;
using System.Collections.Generic;

namespace Phevolution
{
    public class PointsSelection
    {

        // add boundary points to pseudo-clip voronoi cells
        public static List<double[]> getBoundaryPoints(int width, int height, double spacing)
        {
            var offset = Utils.rn(-1 * spacing);
            var bSpacing = spacing * 2;
            var w = width - offset * 2;
            var h = height - offset * 2;
            var numberX = Math.Ceiling(w / bSpacing) - 1;
            var numberY = Math.Ceiling(h / bSpacing) - 1;
            var points = new List<double[]>();
            for (var i = 0.5; i < numberX; i++)
            {
                var x = Math.Ceiling(w * i / numberX + offset);
                points.Add(new double[] { x, offset });
                points.Add(new double[] { x, h + offset });
            }
            for (var i = 0.5; i < numberY; i++)
            {
                var y = Math.Ceiling(h * i / numberY + offset);
                points.Add(new double[] { offset, y });
                points.Add(new double[] { w + offset, y });
            }
            return points;
        }

        public static List<double[]> getJitteredGrid(int width, int height, double spacing)
        {
            var radius = spacing / 2; // square radius
            var jittering = radius * .9; // max deviation
            double jitter() => Random.NextDouble() * 2 * jittering - jittering;

            var points = new List<double[]>();
            for (var y = radius; y < height; y += spacing)
            {
                for (var x = radius; x < width; x += spacing)
                {
                    var xj = Math.Min(Utils.rn(x + jitter(), 2), width);
                    var yj = Math.Min(Utils.rn(y + jitter(), 2), height);
                    points.Add(new double[] { xj, yj });
                }
            }
            return points;
        }

        public static Delaunator fromPoints(List<double[]> points)
        {
            var coords = new double[2 * points.Count];
            int i = 0;
            for (int p = 0; p < points.Count; ++p)
            {
                coords[i++] = points[p][0];
                coords[i++] = points[p][1];
            }
            var delauny = new Delaunator(coords);
            return delauny;
        }
    }
}