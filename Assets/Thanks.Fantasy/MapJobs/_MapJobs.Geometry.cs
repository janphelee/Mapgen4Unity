using Phevolution;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LibTessDotNet;

namespace Thanks.Fantasy
{

    public partial class _MapJobs
    {
        public Geometry[] landmass { get; set; }
        public Geometry[] heightmap { get; set; }

        private LibTessDotNet.Tess tess { get; set; }

        private void generate()
        {
            tess = new LibTessDotNet.Tess();
            createLandmass();
            createHeightmap();

        }

        private Func<float, Color> getColorScheme()
        {
            var s = D3.Spectral;
            return t => s(t);
        }
        private Color getColor(int value, Func<float, Color> scheme)
        {
            var c = scheme(1 - (value < 20 ? value - 5 : value) / 100f);
            //Debug.Log($"{value} {c}");
            return c;
        }

        private void createHeightmap()
        {
            //var msg = new List<string>();

            var cells = pack.cells;
            var vertices = pack.vertices;
            var n = cells.i.Length;
            var used = new byte[cells.i.Length];

            //const terracing = terrs.attr("terracing") / 10; // add additional shifted darker layer for pseudo-3d effect
            //const skip = +terrs.attr("skip") + 1;
            var terracing = 0f / 10;
            var skip = 5 + 1;
            var simplification = 0;

            var currentLayer = 20;
            var heights = new List<int>(cells.i);
            Utils.InsertionSort(heights, (a, b) => cells.h[a] - cells.h[b]);
            cells.i = heights.ToArray();
            //DebugHelper.SaveArray("pack.cells.i.txt", heights);

            var paths = new Dictionary<int, List<Geometry>>();
            foreach (var i in heights)
            {
                var h = cells.h[i];
                if (h > currentLayer) currentLayer += skip;
                if (currentLayer > 100) break; // no layers possible with height > 100
                if (h < currentLayer) continue;
                if (used[i] != 0) continue;

                var onborder = Array.Exists(cells.c[i], c => cells.h[c] < h);
                if (!onborder) continue;

                var vertex = Array.Find(cells.v[i], v => Array.Exists(vertices.c[v], index => cells.h[index] < h));
                var chain = connectVertices(vertex, h);
                //msg.Add($"{i} {h} {vertex} {DebugHelper.toString(chain)}");
                if (chain.Length < 3) continue;

                IList<double[]> points = simplifyLine(chain).Select(v => vertices.p[v]).ToList();
                points = duplicate(points);

                tess.AddContour(
                    points.Select(v => new ContourVertex(new Vec3((float)v[0], -(float)v[1], 0))).ToArray(),
                    ContourOrientation.CounterClockwise
                    );
                tess.Tessellate();

                var geo = new Geometry()
                {
                    name = $"heightmap_{h}_{i}_{points.Count}|{tess.VertexCount}_{tess.ElementCount}",
                    ppp = tess.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, 0)).ToArray(),
                    iii = tess.Elements,
                };

                if (!paths.ContainsKey(h)) paths[h] = new List<Geometry>();
                paths[h].Add(geo);
            }
            //DebugHelper.SaveArray("createHeightmap.txt", msg);

            var scheme = getColorScheme();
            var geoms = new List<Geometry>();

            landmass[0].color = scheme(0.8f);
            geoms.Add(landmass[0]);

            foreach (var kv in paths)
            {
                var ppp = new List<Vector3>();
                var iii = new List<int>();
                foreach (var v in kv.Value)
                {
                    iii.AddRange(v.iii.Select(i => i + ppp.Count));
                    ppp.AddRange(v.ppp);
                }
                geoms.Add(new Geometry()
                {
                    ppp = ppp.ToArray(),
                    iii = iii.ToArray(),
                    name = kv.Value[0].name,
                    color = getColor(kv.Key, scheme),
                });
            }

            heightmap = geoms.ToArray();

            // connect vertices to chain
            int[] connectVertices(int start, int h)
            {
                var chain = new List<int>(); // vertices chain to form a path
                for (int i = 0, current = start; i == 0 || current != start && i < 20000; i++)
                {
                    var prev = i == 0 ? -1 : chain[chain.Count - 1]; // previous vertex in chain
                    chain.Add(current); // add current vertex to sequence
                    var c = vertices.c[current]; // cells adjacent to vertex
                    var filter = c.Where(index => cells.h[index] == h);
                    foreach (var index in filter) used[index] = 1;
                    var c0 = c[0] >= n || cells.h[c[0]] < h;
                    var c1 = c[1] >= n || cells.h[c[1]] < h;
                    var c2 = c[2] >= n || cells.h[c[2]] < h;
                    var v = vertices.v[current]; // neighboring vertices
                    if (v[0] != prev && c0 != c1) current = v[0];
                    else if (v[1] != prev && c1 != c2) current = v[1];
                    else if (v[2] != prev && c0 != c2) current = v[2];
                    if (current == chain[chain.Count - 1])
                    {
                        //UnityEngine.Debug.LogError("Next vertex is not found");
                        break;
                    }
                }
                return chain.ToArray();
            }

            int[] simplifyLine(int[] chain)
            {
                if (simplification == 0) return chain;
                var nth = simplification + 1; // filter each nth element
                return chain.Where((d, i) => i % nth == 0).ToArray();
            }
        }

        private void createLandmass()
        {
            var geos = new List<Geometry>();

            var ff = pack.features;
            for (var i = 0; i < ff.Length; ++i)
            {
                var points = pack.getFeaturePoints(i);
                if (points == null) continue;

                points = duplicate(points);

                tess.AddContour(
                    points.Select(v => new ContourVertex(new Vec3((float)v[0], -(float)v[1], 0))).ToArray(),
                    ContourOrientation.CounterClockwise
                    );
                tess.Tessellate();
                var geo = new Geometry()
                {
                    name = $"landmass_{geos.Count}_{i}_{points.Count}_{tess.ElementCount}",
                    color = Color.white,
                    ppp = tess.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, 0)).ToArray(),
                    iii = tess.Elements,
                };
                //Debug.Log(geo.name);
                geos.Add(geo);
            }

            var ppp = new List<Vector3>();
            var iii = new List<int>();
            foreach (var v in geos)
            {
                iii.AddRange(v.iii.Select(i => i + ppp.Count));
                ppp.AddRange(v.ppp);
            }
            landmass = new Geometry[] { new Geometry() {
                name =  $"landmass_{geos.Count}_{ppp.Count}_{iii.Count}",
                color = geos[0].color,
                iii = iii.ToArray(),
                ppp = ppp.ToArray(),
            } };
        }


        IList<double[]> duplicate(IList<double[]> points)
        {
            float S(double[] p0, double[] p1)
            {
                var x = p0[0] - p1[0];
                var y = p0[1] - p1[1];
                return (float)(x * x + y * y);
            }
            var valid = new List<double[]>();
            for (var i = 0; i < points.Count; ++i)
            {
                var idx1 = (i + 1) % points.Count;
                if (S(points[i], points[idx1]) > 0.001f)
                    valid.Add(points[i]);
            }
            return valid;
        }


    }
}