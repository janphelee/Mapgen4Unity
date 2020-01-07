
namespace Thanks.Fantasy
{
    using Phevolution;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using static Phevolution.Utils;
    using Grid = _MapJobs.Grid;
    using Random = Phevolution.Random;

    public class Map4Coastline
    {
        private Grid grid { get; set; }
        private Grid pack { get; set; }

        private Grid.Cells cells { get; set; }
        private Grid.Vertices vertices { get; set; }
        private int n { get; set; }

        public Map4Coastline(_MapJobs map)
        {
            grid = map.grid;
            pack = map.pack;
            cells = pack.cells;
            vertices = pack.vertices;
            n = cells.i.Length;
        }

        // Detect and draw the coasline
        public void drawCoastline()
        {
            var msg = new List<string>();

            var features = pack.features;
            var used = new byte[features.Length];
            var data = features.Select(f => (f != null && f.land) ? f.cells : 0).ToArray();
            //DebugHelper.SaveArray("features.cells.txt", data);
            var largestLand = D3.scan(data, (a, b) => b - a);
            //msg.Add($"largestLand {largestLand} {features.Length}");
            foreach (var i in cells.i)
            {
                var startFromEdge = i == 0 && cells.h[i] >= 20;
                if (!startFromEdge && cells.t[i] != -1 && cells.t[i] != 1) continue; // non-edge cell
                var f = cells.f[i];
                if (used[f] != 0) continue; // already connected
                if (features[f].type == "ocean") continue; // ocean cell

                var type = features[f].type == "lake" ? 1 : -1; // type value to search for
                var start = findStart(i, type);
                if (start == -1) continue; // cannot start here

                var vchain = connectVertices(start, type);
                if (features[f].type == "lake") relax(vchain, 1.2);
                used[f] = 1;
                var points = vchain.Select(v => vertices.p[v]).ToArray();
                var area = D3.polygonArea(points); // area with lakes/islands
                //if (area > 0 && features[f].type == "lake")
                if (area < 0)// area < 0 顺时针多边形 area > 0 逆时针多边形
                {
                    points = points.Reverse().ToArray();
                    vchain = vchain.Reverse().ToArray();
                }
                //var pts = new List<string>();
                //foreach (var p in points) pts.Add(DebugHelper.toString(p));
                //msg.Add($"{i} {f} {type} {start} {area}");
                //msg.Add($"{i} {string.Join(",", pts)}");

                features[f].area = Math.Abs(area);
                features[f].vertices = vchain;
                msg.Add(DebugHelper.toString(vchain));
            }
            //DebugHelper.SaveArray("drawCoastline.txt", msg);
            DebugHelper.SaveArray("vchain.txt", msg);
        }


        // find cell vertex to start path detection
        private int findStart(int i, int t)
        {
            if (t == -1 && cells.b[i])
                return Array.Find(cells.v[i], v => Array.Exists(vertices.c[v], c => c >= n)); // map border cell
            var filtered = cells.c[i].Where(c => cells.t[c] == t).ToArray();
            var min = filtered.Length > 0 ? D3.min(filtered) : -1;
            var index = Array.FindIndex(cells.c[i], c => c == min);
            return index == -1 ? index : cells.v[i][index];
        }
        // connect vertices to chain
        private int[] connectVertices(int start, int t)
        {
            var chain = new List<int>(); // vertices chain to form a path
            for (int i = 0, current = start; i == 0 || current != start && i < 50000; ++i)
            {
                var prev = chain.Count > 0 ? chain[chain.Count - 1] : -1; // previous vertex in chain
                                                                          //d3.select("#labels").append("text").attr("x", vertices.p[current][0]).attr("y", vertices.p[current][1]).text(i).attr("font-size", "1px");
                chain.Add(current); // add current vertex to sequence
                var c = vertices.c[current]; // cells adjacent to vertex
                var v = vertices.v[current]; // neighboring vertices
                var c0 = c[0] >= n || cells.t[c[0]] == t;
                var c1 = c[1] >= n || cells.t[c[1]] == t;
                var c2 = c[2] >= n || cells.t[c[2]] == t;
                if (v[0] != prev && c0 != c1) current = v[0];
                else
                if (v[1] != prev && c1 != c2) current = v[1];
                else
                if (v[2] != prev && c0 != c2) current = v[2];
                if (current == chain[chain.Count - 1])
                {
                    UnityEngine.Debug.LogWarning("Next vertex is not found");
                    break;
                }
            }
            //chain.push(chain[0]); // push first vertex as the last one
            return chain.ToArray();
        }
        // move vertices that are too close to already added ones
        private void relax(int[] vchain, double r)
        {
            var p = vertices.p;
            var tree = D3.quadtree();

            for (var i = 0; i < vchain.Length; ++i)
            {
                var v = vchain[i];
                double x = p[v][0], y = p[v][1];
                var outOfRange = i > 0 && i < vchain.Length - 1;
                if (outOfRange && vchain[i + 1] > 0 && tree.find(x, y, r) != null)
                {
                    var v1 = vchain[i - 1];
                    var v2 = vchain[i + 1];
                    double x1 = p[v1][0], y1 = p[v1][1];
                    double x2 = p[v2][0], y2 = p[v2][1];

                    x = (x1 + x2) / 2;
                    y = (y1 + y2) / 2;
                    p[v] = new double[] { x, y };
                }
                tree.add(new D3.Quadtree.Value(x, y, v));
            }
        }


    }
}