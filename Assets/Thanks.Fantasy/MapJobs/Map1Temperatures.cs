using UnityEngine;
using System.Collections;
using System;
using Phevolution;
using System.Collections.Generic;

namespace Thanks.Fantasy
{
    using Grid = _MapJobs.Grid;

    public class Map1Temperatures
    {
        private Grid grid { get; set; }
        private Grid.Cells cells { get; set; }
        private int graphHeight { get; set; }
        private _MapJobs.Coordinates mapCoordinates { get; set; }

        private double temperatureEquatorInput { get; set; }
        private double temperaturePoleInput { get; set; }
        private double heightExponentInput { get; set; }


        public Map1Temperatures(_MapJobs map)
        {
            grid = map.grid;
            cells = grid.cells;

            graphHeight = map.graphHeight;
            mapCoordinates = map.mapCoordinates;

            temperatureEquatorInput = map.temperatureEquatorInput;
            temperaturePoleInput = map.temperaturePoleInput;
            heightExponentInput = map.heightExponentInput;
        }

        public void calculateTemperatures()
        {
            //var msg = new List<string>();
            var temp = new sbyte[cells.i.Length];

            var tEq = +temperatureEquatorInput;
            var tPole = +temperaturePoleInput;
            var tDelta = tEq - tPole;

            //msg.Add($"{tEq} {tPole} {heightExponentInput}");
            var range = D3.range(0, cells.i.Length, (int)grid.cellsX);
            foreach (var r in range)
            {
                var y = grid.points[r][1];
                var lat = Math.Abs(mapCoordinates.latN - y / graphHeight * mapCoordinates.latT);
                var initTemp = tEq - lat / 90 * tDelta;
                for (var i = r; i < r + grid.cellsX; i++)
                {
                    temp[i] = (sbyte)(initTemp - convertToFriendly(cells.h[i]));
                    //msg.Add($"{r} {i} {temp[i]}");
                }
            }
            //DebugHelper.SaveArray("calculateTemperatures.txt", msg);
            cells.temp = temp;
        }

        // temperature decreases by 6.5 degree C per 1km
        private double convertToFriendly(double h)
        {
            if (h < 20) return 0;
            var exponent = +heightExponentInput;
            var height = Math.Pow(h - 18, exponent);
            return Utils.rn(height / 1000 * 6.5);
        }
    }
}