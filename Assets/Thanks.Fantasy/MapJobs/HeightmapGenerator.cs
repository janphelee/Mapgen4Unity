using System;
using System.Collections.Generic;

namespace Thanks.Fantasy
{
    using Phevolution;
    using System.Linq;
    using static Phevolution.Utils;
    using Grid = _MapJobs.Grid;
    using Random = Phevolution.Random;

    public partial class HeightmapGenerator
    {
        private int graphWidth { get; set; }
        private int graphHeight { get; set; }
        private int densityInput { get; set; }
        private string templateInput { get; set; }

        private Grid grid { get; set; }
        private Grid.Cells cells { get; set; }
        private List<double[]> p { get; set; }

        public HeightmapGenerator(_MapJobs map)
        {
            graphWidth = map.graphWidth;
            graphHeight = map.graphHeight;
            densityInput = map.densityInput;
            templateInput = map.templateInput;

            grid = map.grid;
        }

        public void generate()
        {
            p = grid.points;
            cells = grid.cells;
            cells.h = new byte[p.Count];

            switch (templateInput)
            {
                case "Volcano": templateVolcano(); break;
                case "High Island": templateHighIsland(); break;
                case "Low Island": templateLowIsland(); break;
                case "Continents": templateContinents(); break;
                case "Archipelago": templateArchipelago(); break;
                case "Atoll": templateAtoll(); break;
                case "Mediterranean": templateMediterranean(); break;
                case "Peninsula": templatePeninsula(); break;
                case "Pangea": templatePangea(); break;
                case "Isthmus": templateIsthmus(); break;
                case "Shattered": templateShattered(); break;
            }
        }

        private void addStep(string a1, double a2, string a3 = "")
        {
            addStep(a1, a2.ToString(), a3, null, null);
        }
        private void addStep(string a1, string a2, string a3)
        {
            addStep(a1, a2, a3, null, null);
        }
        private void addStep(string a1, string a2, string a3, string a4, string a5)
        {
            if (a1 == "Hill") addHill(a2, a3, a4, a5);                 //已验证
            else if (a1 == "Pit") addPit(a2, a3, a4, a5);
            else if (a1 == "Range") addRange(a2, a3, a4, a5);          //已验证
            else if (a1 == "Trough") addTrough(a2, a3, a4, a5);        //已验证
            else if (a1 == "Strait") addStrait(a2, a3);                //已验证
            else if (a1 == "Add") modify(a3, double.Parse(a2), 1);     //已验证
            else if (a1 == "Multiply") modify(a3, 0, double.Parse(a2));
            else if (a1 == "Smooth") smooth(double.Parse(a2));         //已验证
        }

        private double getBlobPower()
        {
            switch (densityInput)
            {
                case 1: return .98;
                case 2: return .985;
                case 3: return .987;
                case 4: return .9892;
                case 5: return .9911;
                case 6: return .9921;
                case 7: return .9934;
                case 8: return .9942;
                case 9: return .9946;
                case 10: return .995;
            }
            return 0;
        }

        private double getLinePower()
        {
            switch (densityInput)
            {
                case 1: return .81;
                case 2: return .82;
                case 3: return .83;
                case 4: return .84;
                case 5: return .855;
                case 6: return .87;
                case 7: return .885;
                case 8: return .91;
                case 9: return .92;
                case 10: return .93;
            }
            return 0;
        }

        private void addHill(string count_s, string height, string rangeX, string rangeY)
        {
            //var msg = new List<string>();
            //msg.Add(Random.NextDouble().ToString());
            var count = getNumberInRange(count_s);
            var power = getBlobPower();
            while (count > 0) { addOneHill(); count--; }
            //DebugHelper.SaveArray("addHill.txt", msg);

            void addOneHill()
            {
                var change = new byte[cells.h.Length];
                int limit = 0, start;
                double h = lim(getNumberInRange(height));

                do
                {
                    var x = getPointInRange(rangeX, graphWidth);
                    var y = getPointInRange(rangeY, graphHeight);
                    start = grid.findGridCell(x, y);
                    limit++;
                    //msg.Add($"{rangeX} {rangeY} limit:{limit} x:{x} y:{y} {start}");
                } while (cells.h[start] + h > 90 && limit < 50);

                change[start] = (byte)h;

                var queue = new List<int>();
                queue.Add(start);

                while (queue.Count > 0)
                {
                    var q = queue[0];
                    queue.RemoveAt(0);

                    foreach (var c in cells.c[q])
                    {
                        if (change[c] > 0) continue;
                        var n = Math.Pow(change[q], power) * (Random.NextDouble() * .2 + .9);
                        change[c] = (byte)n;
                        if (change[c] > 1) queue.Add(c);
                        //msg.Add($"{queue.Count} {c} {change[q]} {n}");
                    }
                }

                cells.h = cells.h.Select((o, i) => lim(o + change[i])).ToArray();
            }
        }

        private void addPit(string count_s, string height, string rangeX, string rangeY)
        {
            var count = getNumberInRange(count_s);
            while (count > 0) { addOnePit(); count--; }

            void addOnePit()
            {
                var used = new byte[cells.h.Length];
                int limit = 0, start;
                double h = lim(getNumberInRange(height));

                do
                {
                    var x = getPointInRange(rangeX, graphWidth);
                    var y = getPointInRange(rangeY, graphHeight);
                    start = grid.findGridCell(x, y);
                    limit++;
                } while (cells.h[start] < 20 && limit < 50);


                var queue = new List<int>();
                queue.Add(start);
                while (queue.Count > 0)
                {
                    var q = queue[0];
                    queue.RemoveAt(0);

                    h = Math.Pow(h, getBlobPower()) * (Random.NextDouble() * .2 + .9);
                    if (h < 1) return;

                    var old = cells.h;
                    foreach (var c in cells.c[q])
                    {
                        if (used[c] != 0) return;
                        var n = old[c] - h * (Random.NextDouble() * .2 + .9);
                        old[c] = lim(n);
                        used[c] = 1;
                        queue.Add(c);
                    }
                    cells.h = old;
                }
            }
        }

        private void addRange(string count_s, string height, string rangeX, string rangeY)
        {
            //var msg = new List<string>();
            var count = getNumberInRange(count_s);
            var power = getLinePower();
            while (count > 0) { addOneRange(); count--; }

            //DebugHelper.SaveArray($"range_{count}.txt", msg);

            void addOneRange()
            {
                var old = cells.h;
                var used = new byte[cells.h.Length];
                double h = lim(getNumberInRange(height));

                // find start and end points
                var startX = getPointInRange(rangeX, graphWidth);
                var startY = getPointInRange(rangeY, graphHeight);

                double dist = 0, limit = 0, endX, endY;
                do
                {
                    endX = Random.NextDouble() * graphWidth * .8 + graphWidth * .1;
                    endY = Random.NextDouble() * graphHeight * .7 + graphHeight * .15;
                    dist = Math.Abs(endY - startY) + Math.Abs(endX - startX);
                    limit++;
                } while ((dist < graphWidth / 8.0 || dist > graphWidth / 3.0) && limit < 50);

                var range = getRange(grid.findGridCell(startX, startY), grid.findGridCell(endX, endY));

                // get main ridge
                int[] getRange(int cur, int end)
                {
                    var ret = new List<int>();
                    ret.Add(cur);
                    used[cur] = 1;

                    while (cur != end)
                    {
                        var min = double.PositiveInfinity;
                        foreach (var e in cells.c[cur])
                        {
                            //msg.Add(used[e].ToString());
                            if (used[e] == 1) continue;
                            var diff = Math.Pow(p[end][0] - p[e][0], 2) + Math.Pow(p[end][1] - p[e][1], 2);
                            //msg.Add($"cur:{cur} e:{e} {diff} {min}");
                            if (Random.NextDouble() > .85) diff = diff / 2;
                            if (diff < min) { min = diff; cur = e; }
                        }
                        if (min == double.PositiveInfinity) return ret.ToArray();
                        ret.Add(cur);
                        used[cur] = 1;
                    }

                    return ret.ToArray();
                }

                // add height to ridge and cells around
                var queue = new List<int>(range);
                var index = 0;
                while (queue.Count > 0)
                {
                    var frontier = queue.ToArray();
                    queue.Clear(); index++;
                    foreach (var i in frontier)
                    {
                        var n = old[i] + h * (Random.NextDouble() * .3 + .85);
                        old[i] = lim(n);
                        //msg.Add($"{index} {i} {h} {n} {old[i]}");
                    }
                    h = Math.Pow(h, power) - 1;
                    if (h < 2) break;

                    foreach (var f in frontier)
                    {
                        foreach (var i in cells.c[f])
                        {
                            if (used[i] != 1) { queue.Add(i); used[i] = 1; }
                        }
                    }
                }

                // generate prominences
                for (int d = 0; d < range.Length; ++d)
                {
                    var cur = range[d];
                    if (d % 6 != 0) continue;
                    for (int i = 0; i < index; ++i)
                    {
                        var idx = D3.scan(cells.c[cur], (a, b) => old[a] - old[b]);
                        var min = cells.c[cur][idx];// downhill cell
                        // debug.append("circle").attr("cx", p[min][0]).attr("cy", p[min][1]).attr("r", 1);
                        old[min] = (byte)((old[cur] * 2 + old[min]) / 3);
                        cur = min;
                    }
                }
                cells.h = old;
            }
        }

        private void addTrough(string count_s, string height, string rangeX, string rangeY)
        {
            //var msg = new List<string>();
            var count = getNumberInRange(count_s);
            var power = getLinePower();
            while (count > 0) { addOneTrough(); count--; }
            //DebugHelper.SaveArray("addTrough.txt", msg);

            void addOneTrough()
            {
                var old = cells.h;
                var used = new byte[cells.h.Length];
                double h = lim(getNumberInRange(height));

                // find start and end points
                double limit = 0, startX, startY, dist = 0, endX, endY;
                int start;
                do
                {
                    startX = getPointInRange(rangeX, graphWidth);
                    startY = getPointInRange(rangeY, graphHeight);
                    start = grid.findGridCell(startX, startY);
                    limit++;
                    //msg.Add($"{rangeX} {rangeY} limit:{limit} x:{startX} y:{startY} {start} --- start");
                } while (old[start] < 20 && limit < 50);


                limit = 0;
                do
                {
                    endX = Random.NextDouble() * graphWidth * .8 + graphWidth * .1;
                    endY = Random.NextDouble() * graphHeight * .7 + graphHeight * .15;
                    dist = Math.Abs(endY - startY) + Math.Abs(endX - startX);
                    limit++;
                    //msg.Add($"{rangeX} {rangeY} limit:{limit} x:{endX} y:{endY} {dist} --- dist");
                } while ((dist < graphWidth / 8.0 || dist > graphWidth / 2.0) && limit < 50);


                var range = getRange(start, grid.findGridCell(endX, endY));

                // get main ridge
                int[] getRange(int cur, int end)
                {
                    var ret = new List<int>();
                    ret.Add(cur);
                    used[cur] = 1;

                    while (cur != end)
                    {
                        var min = double.PositiveInfinity;
                        foreach (var e in cells.c[cur])
                        {
                            if (used[e] == 1) continue;
                            var diff = Math.Pow(p[end][0] - p[e][0], 2) + Math.Pow(p[end][1] - p[e][1], 2);
                            if (Random.NextDouble() > .8) diff = diff / 2;
                            if (diff < min) { min = diff; cur = e; }
                        }
                        if (min == double.PositiveInfinity) return ret.ToArray();

                        ret.Add(cur);
                        used[cur] = 1;
                    }

                    return ret.ToArray();
                }

                // add height to ridge and cells around
                var queue = new List<int>(range);
                var index = 0;
                while (queue.Count > 0)
                {
                    var frontier = queue.ToArray();
                    queue.Clear(); index++;
                    foreach (var i in frontier)
                    {
                        var n = old[i] - h * (Random.NextDouble() * .3 + .85);
                        //msg.Add($"{index} {i} {old[i]} {h} {n} {Random.NextDouble()}");
                        old[i] = lim(n);
                    }
                    h = Math.Pow(h, power) - 1;
                    if (h < 2) break;

                    foreach (var f in frontier)
                    {
                        foreach (var i in cells.c[f])
                        {
                            if (used[i] == 0) { queue.Add(i); used[i] = 1; }
                        }
                    }
                }

                // generate prominences
                for (int d = 0; d < range.Length; ++d)
                {
                    var cur = range[d];
                    if (d % 6 != 0) continue;
                    for (int i = 0; i < index; ++i)
                    {
                        var idx = D3.scan(cells.c[cur], (a, b) => old[a] - old[b]);
                        var min = cells.c[cur][idx];// downhill cell
                        //msg.Add($"{d} {i} {idx} {cur} {min}");
                        // debug.append("circle").attr("cx", p[min][0]).attr("cy", p[min][1]).attr("r", 1);
                        old[min] = (byte)((old[cur] * 2 + old[min]) / 3);
                        cur = min;
                    }
                }
                cells.h = old;
            }
        }

        private void addStrait(string width_s, string direction = "vertical")
        {
            var width = Math.Min(getNumberInRange(width_s), grid.cellsX / 3);
            if (width < 1 && P(width)) return;

            var old = cells.h;
            var used = new byte[cells.h.Length];
            var vert = direction == "vertical";
            double startX = vert ? Math.Floor(Random.NextDouble() * graphWidth * .4 + graphWidth * .3) : 5;
            double startY = vert ? 5 : Math.Floor(Random.NextDouble() * graphHeight * .4 + graphHeight * .3);
            double endX = vert ? Math.Floor((graphWidth - startX) - (graphWidth * .1) + (Random.NextDouble() * graphWidth * .2)) : graphWidth - 5;
            double endY = vert ? graphHeight - 5 : Math.Floor((graphHeight - startY) - (graphHeight * .1) + (Random.NextDouble() * graphHeight * .2));


            int[] getRange(int cur, int end)
            {
                var ret = new List<int>();

                while (cur != end)
                {
                    var min = double.PositiveInfinity;
                    foreach (var e in cells.c[cur])
                    {
                        var diff = Math.Pow(p[end][0] - p[e][0], 2) + Math.Pow(p[end][1] - p[e][1], 2);
                        if (Random.NextDouble() > 0.8) diff = diff / 2;
                        if (diff < min) { min = diff; cur = e; }
                    }
                    ret.Add(cur);
                }

                return ret.ToArray();
            }

            var start = grid.findGridCell(startX, startY);
            var range = getRange(start, grid.findGridCell(endX, endY));
            var query = new List<int>();

            var step = .1 / width;

            while (width > 0)
            {
                var exp = .9 - step * width;
                foreach (var r in range)
                {
                    foreach (var e in cells.c[r])
                    {
                        if (used[e] == 1) continue;
                        used[e] = 1;
                        query.Add(e);
                        old[e] = (byte)Math.Pow(old[e], exp);
                        if (old[e] > 100) old[e] = 5;
                    }
                }
                range = query.ToArray();

                width--;
            }
            cells.h = old;
        }

        private void modify(string range, double add, double mult, double power = 0)
        {
            var strs = range.Split('-');
            var min = range == "land" ? 20 : range == "all" ? 0 : int.Parse(strs[0]);
            var max = range == "land" || range == "all" ? 100 : int.Parse(strs[1]);

            grid.cells.h = grid.cells.h.Select(h => h >= min && h <= max ? mod(h) : h).ToArray();

            byte mod(double v)
            {
                if (add != 0) v = min == 20 ? Math.Max(v + add, 20) : v + add;
                if (mult != 1) v = min == 20 ? (v - 20) * mult + 20 : v * mult;
                if (power != 0) v = min == 20 ? Math.Pow(v - 20, power) + 20 : Math.Pow(v, power);
                return lim(v);
            }
        }

        private void smooth(double fr = 2, double add = 0)
        {
            cells.h = cells.h.Select((h, i) =>
            {
                var a = new List<byte>();
                a.Add(h);
                foreach (var c in cells.c[i])
                {
                    a.Add(cells.h[c]);
                }
                return lim((h * (fr - 1) + D3.avg(a.ToArray()) + add) / fr);
            }).ToArray();
        }

        private double getPointInRange(string range, int length)
        {
            var strs = range.Split('-');
            var min = double.Parse(strs[0]) / 100;
            var max = double.Parse(strs[1]) / 100;
            return rand(min * length, max * length);
        }

    }
}