using UnityEngine;
using System.Collections;
using System;
using Phevolution;
using System.Collections.Generic;

namespace Thanks.Fantasy
{
    using Grid = _MapJobs.Grid;

    public class Map2Precipitation
    {
        private Grid grid { get; set; }
        private Grid.Cells cells { get; set; }
        private _MapJobs.Coordinates mapCoordinates { get; set; }
        private int[] winds { get; set; }

        private double modifier { get; set; }

        public Map2Precipitation(_MapJobs map)
        {
            grid = map.grid;
            cells = grid.cells;
            mapCoordinates = map.mapCoordinates;
            winds = map.winds;

            modifier = map.precInput / 100; // user's input
        }

        // simplest precipitation model
        public void generatePrecipitation()
        {
            cells.prec = new byte[cells.i.Length];// precipitation array

            var cellsX = (int)grid.cellsX;
            var cellsY = (int)grid.cellsY;

            var westerly = new List<double[]>();
            var easterly = new List<double[]>();
            int southerly = 0, northerly = 0;
            {// latitude bands
             // x4 = 0-5 latitude: wet throught the year (rising zone)
             // x2 = 5-20 latitude: wet summer (rising zone), dry winter (sinking zone)
             // x1 = 20-30 latitude: dry all year (sinking zone)
             // x2 = 30-50 latitude: wet winter (rising zone), dry summer (sinking zone)
             // x3 = 50-60 latitude: wet all year (rising zone)
             // x2 = 60-70 latitude: wet summer (rising zone), dry winter (sinking zone)
             // x1 = 70-90 latitude: dry all year (sinking zone)
            }
            var lalitudeModifier = new double[] { 4, 2, 2, 2, 1, 1, 2, 2, 2, 2, 3, 3, 2, 2, 1, 1, 1, 0.5 }; // by 5d step

            // difine wind directions based on cells latitude and prevailing winds there
            var range = D3.range(0, cells.i.Length, cellsX);
            //DebugHelper.SaveArray("range.txt", range);
            for (var i = 0; i < range.Length; ++i)
            {
                var c = range[i];
                var lat = mapCoordinates.latN - mapCoordinates.latT * i / cellsY;
                var band = (int)((Math.Abs(lat) - 1) / 5);
                var latMod = lalitudeModifier[band];
                var tier = (int)(Math.Abs(lat - 89) / 30); // 30d tiers from 0 to 5 from N to S
                if (winds[tier] > 40 && winds[tier] < 140)
                    westerly.Add(new double[] { c, latMod, tier });
                else if (winds[tier] > 220 && winds[tier] < 320)
                    easterly.Add(new double[] { c + cellsX - 1, latMod, tier });
                if (winds[tier] > 100 && winds[tier] < 260) northerly++;
                else if (winds[tier] > 280 || winds[tier] < 80) southerly++;
                //msg.Add($"{ i} { c} { northerly} { southerly} {tier} {winds[tier]}");
            }
            //msg.Add($"---------------------{modifier}");

            // distribute winds by direction
            if (westerly.Count > 0) passWind(westerly, 120 * modifier, 1, cellsX);
            if (easterly.Count > 0) passWind(easterly, 120 * modifier, -1, cellsX);
            double vertT = (southerly + northerly);
            if (northerly > 0)
            {
                var bandN = (int)((Math.Abs(mapCoordinates.latN) - 1) / 5);
                var latModN = mapCoordinates.latT > 60 ? D3.avg(lalitudeModifier) : lalitudeModifier[bandN];
                var maxPrecN = northerly / vertT * 60 * modifier * latModN;
                passWind(D3.range(0, cellsX, 1), maxPrecN, cellsX, cellsY);
            }
            if (southerly > 0)
            {
                var bandS = (int)((Math.Abs(mapCoordinates.latS) - 1) / 5);
                var latModS = mapCoordinates.latT > 60 ? D3.avg(lalitudeModifier) : lalitudeModifier[bandS];
                var maxPrecS = southerly / vertT * 60 * modifier * latModS;
                passWind(D3.range(cells.i.Length - cellsX, cells.i.Length, 1), maxPrecS, -cellsX, cellsY);
            }
            //DebugHelper.SaveArray("generatePrecipitation.txt", msg);
        }
        //private List<string> msg = new List<string>();

        private void passWind(List<double[]> source, double maxPrec, int next, int steps)
        {
            var maxPrecInit = maxPrec;
            for (int i = 0; i < source.Count; ++i)
            {
                var first = source[i];
                maxPrec = Math.Min(maxPrecInit * first[1], 255); ;
                passNext((int)first[0], maxPrec, next, steps);
            }
        }
        private void passWind(int[] source, double maxPrec, int next, int steps)
        {
            for (var i = 0; i < source.Length; ++i)
            {
                passNext(source[i], maxPrec, next, steps);
            }
        }

        private void passNext(int first, double maxPrec, int next, int steps)
        {
            //msg.Add($"passNext {first} {maxPrec} {next} {steps}");
            var prec = cells.prec;
            double humidity = maxPrec - cells.h[first]; // initial water amount
            if (humidity <= 0) return; // if first cell in row is too elevated cosdired wind dry
            for (int s = 0, current = first; s < steps; s++, current += next)
            {
                //msg.Add($"{first} {s} {current} {humidity}");
                // no flux on permafrost
                if (cells.temp[current] < -5) continue;
                // water cell
                if (cells.h[current] < 20)
                {
                    var outOfRange = (current + next) < 0 || (current + next) >= cells.h.Length;
                    if (!outOfRange && (cells.h[current + next] >= 20))
                    {
                        prec[current + next] += (byte)Math.Max(humidity / Utils.rand(10, 20), 1); // coastal precipitation
                    }
                    else
                    {
                        humidity = Math.Min(humidity + 5 * modifier, maxPrec); // wind gets more humidity passing water cell
                        prec[current] += (byte)(5 * modifier); // water cells precipitation (need to correctly pour water through lakes)
                    }
                    continue;
                }

                // land cell
                var precipitation = getPrecipitation(humidity, current, next);
                prec[current] += (byte)precipitation;
                var evaporation = precipitation > 1.5 ? 1 : 0; // some humidity evaporates back to the atmosphere
                humidity = Math.Min(Math.Max(humidity - precipitation + evaporation, 0), maxPrec);
            }
        }

        private double getPrecipitation(double humidity, int i, int n)
        {
            if (cells.h[i + n] > 85) return humidity; // 85 is max passable height
            var normalLoss = Math.Max(humidity / (10 * modifier), 1); // precipitation in normal conditions
            var diff = Math.Max(cells.h[i + n] - cells.h[i], 0); // difference in height
            var mod = Math.Pow(cells.h[i + n] / 70.0, 2); // 50 stands for hills, 70 for mountains
            return Math.Min(Math.Max(normalLoss + diff * mod, 1), humidity);
        }

        //void function drawWindDirection()
        //{
        //    const wind = prec.append("g").attr("id", "wind");

        //    d3.range(0, 6).forEach(function(t) {
        //        if (westerly.length > 1)
        //        {
        //            const west = westerly.filter(w => w[2] === t);
        //            if (west && west.length > 3)
        //            {
        //                const from = west[0][0], to = west[west.length - 1][0];
        //                const y = (grid.points[from][1] + grid.points[to][1]) / 2;
        //                wind.append("text").attr("x", 20).attr("y", y).text("\u21C9");
        //            }
        //        }
        //        if (easterly.length > 1)
        //        {
        //            const east = easterly.filter(w => w[2] === t);
        //            if (east && east.length > 3)
        //            {
        //                const from = east[0][0], to = east[east.length - 1][0];
        //                const y = (grid.points[from][1] + grid.points[to][1]) / 2;
        //                wind.append("text").attr("x", graphWidth - 52).attr("y", y).text("\u21C7");
        //            }
        //        }
        //    });

        //    if (northerly) wind.append("text").attr("x", graphWidth / 2).attr("y", 42).text("\u21CA");
        //    if (southerly) wind.append("text").attr("x", graphWidth / 2).attr("y", graphHeight - 20).text("\u21C8");
        //}
        //();
    }
}