
namespace Thanks.Fantasy
{
    using Phevolution;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using static Phevolution.Utils;
    using Grid = _MapJobs.Grid;
    using Random = Phevolution.Random;

    public class Map3Features
    {
        private Grid grid { get; set; }
        private Grid pack { get; set; }

        private Grid.Cells cells { get; set; }
        private List<Grid.Feature> features { get; set; }

        public Map3Features(_MapJobs map)
        {
            grid = map.grid;
            pack = map.pack;
            cells = pack.cells;
        }

        // Re-mark features (ocean, lakes, islands)
        public void reMarkFeatures()
        {
            //var msg = new List<string>();

            features = new List<Grid.Feature>();
            features.Add(null);

            cells.f = new ushort[cells.i.Length]; // cell feature number
            cells.t = new sbyte[cells.i.Length]; // cell type: 1 = land along coast; -1 = water along coast;
            var haven = new int[cells.i.Length];// cell haven (opposite water cell);
            var harbor = new byte[cells.i.Length]; // cell harbor (number of adjacent water cells);

            var queue = new List<int>(); queue.Add(0);
            for (ushort i = 1; queue[0] != -1; ++i)
            {
                var start = queue[0]; // first cell
                cells.f[start] = i; // assign feature number
                var land = cells.h[start] >= 20;
                var border = false; // true if feature touches map border
                var cellNumber = 1; // to count cells number in a feature

                while (queue.Count > 0)
                {
                    var q = pop(queue);

                    if (cells.b[q]) border = true;
                    foreach (var e in cells.c[q])
                    {
                        var eLand = cells.h[e] >= 20;
                        if (land && !eLand)
                        {
                            cells.t[q] = 1;
                            cells.t[e] = -1;
                            harbor[q]++;
                            if (haven[q] == 0) haven[q] = e;
                        }
                        else if (land && eLand)
                        {
                            if (cells.t[e] == 0 && cells.t[q] == 1) cells.t[e] = 2;
                            else if (cells.t[q] == 0 && cells.t[e] == 1) cells.t[q] = 2;
                        }
                        if (land == eLand && cells.f[e] == 0)
                        {
                            cells.f[e] = i;
                            queue.Add(e);
                            cellNumber++;
                        }
                        //msg.Add($"{i} {start} {q} {e} {eLand} {land} {harbor[q]} {cellNumber}");
                    }
                }

                var type = land ? "island" : border ? "ocean" : "lake";
                string group = null;
                if (type == "lake") group = defineLakeGroup(start, cellNumber);
                else if (type == "ocean") group = "ocean";
                else if (type == "island") group = defineIslandGroup(start, cellNumber);

                features.Add(new Grid.Feature()
                {
                    i = i,
                    land = land,
                    border = border,
                    type = type,
                    cells = cellNumber,
                    firstCell = start,
                    group = group,
                });
                queue.Add(Array.FindIndex(cells.f, f => f == 0));// find unmarked cell
            }
            //DebugHelper.SaveArray("reMarkFeatures.txt", msg);

            cells.haven = haven;
            cells.harbor = harbor;
            pack.features = features.ToArray();
        }

        private string defineLakeGroup(int cell, int number)
        {
            var temp = grid.cells.temp[cells.g[cell]];
            if (temp > 24) return "salt";
            if (temp < -3) return "frozen";

            var height = cells.c[cell].Select(c => cells.h[c]).Max();
            if (height > 69 && number < 3 && cell % 5 == 0) return "sinkhole";
            if (height > 69 && number < 10 && cell % 5 == 0) return "lava";
            return "freshwater";
        }

        private string defineIslandGroup(int cell, int number)
        {
            if (cell > 0)
            {
                var f = cells.f[cell - 1];
                if (f > 0 && f < features.Count && features[f].type == "lake")
                { return "lake_island"; }
            }
            if (number > grid.cells.i.Length / 10) return "continent";
            if (number > grid.cells.i.Length / 1000) return "island";
            return "isle";
        }
    }
}