using Phevolution;
using System;
using System.Collections.Generic;

using static Phevolution.Utils;
using static Phevolution.PointsSelection;
using Random = Phevolution.Random;
using System.IO;
using UnityEngine;
using System.Linq;
using System.Diagnostics;

namespace Thanks.Fantasy
{
    public partial class _MapJobs
    {

        private void initConfig()
        {
            seed = 1;
            graphWidth = 1153;
            graphHeight = 717;
            densityInput = 1;
            templateInput = "Archipelago";

            precInput = 127;//[0,500]
            winds = new int[] { 225, 45, 225, 315, 135, 315 }; // default wind directions

            temperatureEquatorInput = 25;
            temperaturePoleInput = -26;//[-30,30]
            heightExponentInput = 1.8;//[1.5,2.1]


            config = new IChanged[] { };
            //config = new IChanged[]
            //    new ChangeI(0,/* seed */ 123, onConfig),
            //    new ChangeI(1,/* N */ 10000, onConfig),
            //    new ChangeI(2,/* P */ 20, onConfig),
            //    new ChangeF(3,/* jitter */ 0.75f, onConfig),
            //    new ChangeI(4,/* drawMode 0 flat/ 1 quad */ 0, onConfig),
            //    new ChangeB(5,/* draw_plateVectors */ false, onConfig),
            //    new ChangeB(6,/* draw_plateBoundaries */ false, onConfig),
        }
        protected override void beforeNextJob()
        {
        }

        protected override void process(Action<long> callback)
        {
            var watcher = new Stopwatch();
            watcher.Start();

            Random.Seed(seed);

            grid = new Grid();
            placePoints();
            //DebugHelper.SaveArray("points.txt", grid.points);
            //DebugHelper.SaveArray("boundary.txt", grid.boundary);
            calculateVoronoi(grid, grid.points, grid.boundary);
            //SaveVoronoi("voronoi.txt", grid);

            new HeightmapGenerator(this).generate();
            //DebugHelper.SaveArray("cells.h.txt", grid.cells.h);

            var feature = new Map1Features(this);
            feature.markFeatures();
            feature.openNearSeaLakes();

            defineMapSize();
            calculateMapCoordinates();
            //Debug.Log(JsonUtility.ToJson(mapCoordinates))
            ;
            new Map1Temperatures(this).calculateTemperatures();
            //DebugHelper.SaveArray("temp.txt", grid.cells.temp);
            new Map2Precipitation(this).generatePrecipitation();
            //DebugHelper.SaveArray("prec.txt", grid.cells.prec);

            pack = new Grid();
            reGraph();
            new Map3Features(this).reMarkFeatures();
            //DebugHelper.SaveArray("cells.f.txt", pack.cells.f);
            //DebugHelper.SaveArray("cells.t.txt", pack.cells.t);
            //DebugHelper.SaveArray("cells.haven.txt", pack.cells.haven);
            //DebugHelper.SaveArray("cells.harbor.txt", pack.cells.harbor);

            new Map4Coastline(this).drawCoastline();


            watcher.Stop();
            callback?.Invoke(watcher.ElapsedMilliseconds);
        }

        private void reGraph()
        {
            var newP = new List<double[]>();
            var newG = new List<int>();
            var newH = new List<byte>();
            {
                var cells = grid.cells;
                var points = grid.points;
                var features = grid.features;
                var spacing2 = grid.spacing * grid.spacing;

                foreach (var i in cells.i)
                {
                    var height = cells.h[i];
                    var type = cells.t[i];
                    if (height < 20 && type != -1 && type != -2) continue; // exclude all deep ocean points
                    if (type == -2 && (i % 4 == 0 || features[cells.f[i]].type == "lake")) continue; // exclude non-coastal lake points
                    double x = points[i][0], y = points[i][1];

                    addNewPoint(i, height, x, y);// add point to array
                                                 // add additional points for cells along coast
                    if (type == 1 || type == -1)
                    {
                        if (cells.b[i]) continue; // not for near-border cells

                        foreach (var e in cells.c[i])
                        {
                            if (i > e) continue;
                            if (cells.t[e] == type)
                            {
                                var dist2 = Math.Pow(y - points[e][1], 2) + Math.Pow(x - points[e][0], 2);
                                if (dist2 < spacing2) continue; // too close to each other
                                var x1 = rn((x + points[e][0]) / 2, 1);
                                var y1 = rn((y + points[e][1]) / 2, 1);
                                addNewPoint(i, height, x1, y1);
                            }
                        }
                    }
                }
                void addNewPoint(int i, byte height, double x, double y)
                {
                    newP.Add(new double[] { x, y });
                    newG.Add(i);
                    newH.Add(height);
                }
            }
            {
                //DebugHelper.SaveArray("addNewPoint.txt", newP);
                calculateVoronoi(pack, newP, grid.boundary);
                var cells = pack.cells;
                cells.p = newP.ToArray();
                cells.g = newG.ToArray();
                cells.q = // points quadtree for fast search
                    D3.quadtree(cells.p.Select((p, d) => new D3.Quadtree.Value(p[0], p[1], d)).ToArray());
                cells.h = newH.ToArray();
                cells.area = // cell area
                    cells.i.Select(i => (ushort)Math.Abs(D3.polygonArea(pack.getGridPolygon(i)))).ToArray();

                //DebugHelper.SaveArray("pack.cells.h.txt", cells.h);
                //DebugHelper.SaveArray("pack.cells.c.txt", cells.c);
            }
        }

        private void placePoints()
        {
            double cellsDesired = 10000 * densityInput; // generate 10k points for each densityInput point
            double spacing = rn(Math.Sqrt(graphWidth * graphHeight / cellsDesired), 2); // spacing between points before jirrering

            grid.boundary = getBoundaryPoints(graphWidth, graphHeight, spacing);
            grid.points = getJitteredGrid(graphWidth, graphHeight, spacing); // jittered square grid
            grid.cellsX = Math.Floor((graphWidth + 0.5 * spacing) / spacing);
            grid.cellsY = Math.Floor((graphHeight + 0.5 * spacing) / spacing);
            grid.spacing = spacing;
        }

        // define map size and position based on template and random factor
        private void defineMapSize()
        {
            var ret = getSizeAndLatitude();
            //if (!locked("mapSize")) mapSizeOutput.value = mapSizeInput.value = size;
            //if (!locked("latitude")) latitudeOutput.value = latitudeInput.value = latitude;
            mapSizeOutput = mapSizeInput = ret[0];
            latitudeOutput = latitudeInput = ret[1];

            double[] getSizeAndLatitude()
            {
                var template = templateInput;// heightmap template
                var part = Array.Exists(grid.features, f => f != null && f.land && f.border);// if land goes over map borders
                var max = part ? 85 : 100;// max size
                var lat = part ? gauss(P(.5) ? 30 : 70, 15, 20, 80) : gauss(50, 20, 15, 85);// latiture shift

                if (!part)
                {
                    var temp = new double[] { 100, 50 };
                    if (template == "Pangea") return temp;
                    if (template == "Shattered" && P(.7)) return temp;
                    if (template == "Continents" && P(.5)) return temp;
                    if (template == "Archipelago" && P(.35)) return temp;
                    if (template == "High Island" && P(.25)) return temp;
                    if (template == "Low Island" && P(.1)) return temp;
                }

                if (template == "Pangea") return new double[] { gauss(75, 20, 30, max), lat };
                if (template == "Volcano") return new double[] { gauss(30, 20, 10, max), lat };
                if (template == "Mediterranean") return new double[] { gauss(30, 30, 15, 80), lat };
                if (template == "Peninsula") return new double[] { gauss(15, 15, 5, 80), lat };
                if (template == "Isthmus") return new double[] { gauss(20, 20, 3, 80), lat };
                if (template == "Atoll") return new double[] { gauss(10, 10, 2, max), lat };

                return new double[] { gauss(40, 20, 15, max), lat }; // Continents, Archipelago, High Island, Low Island
            }
        }

        // calculate map position on globe
        private void calculateMapCoordinates()
        {
            var size = mapSizeOutput;
            var latShift = latitudeOutput;

            var latT = size / 100 * 180;
            var latN = 90 - (180 - latT) * latShift / 100;
            var latS = latN - latT;

            var lon = Math.Min(latT / 2 * graphWidth / graphHeight, 180);

            mapCoordinates = new Coordinates()
            {
                latT = latT,
                latN = latN,
                latS = latS,
                lonT = lon * 2,
                lonW = -lon,
                lonE = lon
            };
        }

        private static void calculateVoronoi(Grid grid, List<double[]> points, List<double[]> boundary)
        {
            var n = points.Count;
            var allPoints = new List<double[]>(points);
            if (boundary != null) allPoints.AddRange(boundary);

            var delauny = fromPoints(allPoints);

            var voronoi = new Voronoi(delauny, allPoints, n);
            grid.cells = voronoi.cells;
            grid.cells.i = D3.range(n);
            grid.vertices = voronoi.vertices;
        }

        public static void SaveVoronoi(string fileName, Grid grid)
        {
            var cells = grid.cells;
            var vertices = grid.vertices;
            var fileInfo = new FileInfo($"{Application.streamingAssetsPath}/{fileName}");
            var streamWriter = fileInfo.CreateText();
            for (var i = 0; i < cells.v.Length; ++i)
            {
                var cv = DebugHelper.toString(cells.v[i]);
                var cc = DebugHelper.toString(cells.c[i]);
                var cb = cells.b[i] ? 1 : 0;
                streamWriter.WriteLine($"cells {i} {cv} {cc} {cb}");
            }
            for (var i = 0; i < vertices.p.Length; ++i)
            {
                var vp = DebugHelper.toString(vertices.p[i]);
                var vv = DebugHelper.toString(vertices.v[i]);
                var vc = DebugHelper.toString(vertices.c[i]);
                streamWriter.WriteLine($"vertices {i} {vp} {vv} {vc}");
            }
            streamWriter.Close();
            streamWriter.Dispose();
        }
    }
}