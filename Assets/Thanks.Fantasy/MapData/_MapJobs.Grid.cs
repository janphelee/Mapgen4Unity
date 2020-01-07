using Phevolution;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Thanks.Fantasy
{
    public partial class _MapJobs
    {
        public class Grid
        {
            public class Feature
            {
                public int i { get; set; }
                public bool land { get; set; }
                public bool border { get; set; }
                public string type { get; set; }
                // pack ====================================
                public int cells { get; set; }
                public int firstCell { get; set; }
                public string group { get; set; }
                // =========================================
                public double area { get; set; }    //drawCoastline
                public int[] vertices { get; set; } //drawCoastline
            }
            public class Cells
            {
                public int[][] v { get; set; }//v = cell vertices, 
                public int[][] c { get; set; }//c = adjacent cells, 
                public bool[] b { get; set; } //b = near-border cell
                public int[] i { get; set; }  //i= indices //TODO public ushort[] i { get; set; }
                public byte[] h { get; set; } //h = HeightmapGenerator

                public ushort[] f { get; set; } //cell feature number
                public sbyte[] t { get; set; }  //cell type: 1 = land coast; -1 = water near coast;
                public sbyte[] temp { get; set; } // Map1Temperatures
                public byte[] prec { get; set; } // Map2Precipitation

                // pack ===========================
                //TODO 跟Vertices.p重叠吗？
                public double[][] p { get; set; }        // reGraph
                public int[] g { get; set; }             // reGraph
                public D3.Quadtree q { get; set; }       // reGraph
                public ushort[] area { get; set; }       // reGraph
                public int[] haven { get; set; } // reMarkFeatures
                public byte[] harbor { get; set; }
            }
            public class Vertices
            {
                public double[][] p { get; set; }//p = vertex coordinates
                public int[][] v { get; set; }   //v = neighboring vertices
                public int[][] c { get; set; }   //c = adjacent cells

            }

            public List<double[]> boundary { get; set; }
            public List<double[]> points { get; set; }
            public double spacing { get; set; }
            public double cellsX { get; set; }
            public double cellsY { get; set; }

            public Cells cells { get; set; }
            public Vertices vertices { get; set; }
            public Feature[] features { get; set; }// MapFeatures

            public int findGridCell(double x, double y)
            {
                var n = Math.Floor(Math.Min(y / spacing, cellsY - 1)) * cellsX + Math.Floor(Math.Min(x / spacing, cellsX - 1));
                return (int)n;
            }

            public double[][] getGridPolygon(int i)
            {
                return cells.v[i].Select(v => vertices.p[v]).ToArray();
            }

            public IList<double[]> getFeaturePoints(int f)
            {
                var ff = features[f];
                if (ff == null) return null;

                var vchain = ff.vertices;
                if (vchain == null) return null;

                return vchain.Select(v => vertices.p[v]).ToList();
            }
        }
    }
}