using Phevolution;
using System;

namespace Thanks.Fantasy
{
    public partial class _MapJobs : JobThread, IDisposable
    {
        public class Coordinates
        {
            public double latT;
            public double latN;
            public double latS;
            public double lonT;
            public double lonW;
            public double lonE;
        }

        public int seed { get; set; }
        public int graphWidth { get; set; }
        public int graphHeight { get; set; }
        public int densityInput { get; set; }
        public string templateInput { get; set; }
        public Grid grid { get; private set; }
        public Grid pack { get; private set; }

        // defineMapSize ==================================
        public double mapSizeInput { get; set; }
        public double latitudeInput { get; set; }
        public double mapSizeOutput { get; set; }
        public double latitudeOutput { get; set; }

        // calculateMapCoordinates 
        public Coordinates mapCoordinates { get; set; }

        // Map1Temperatures
        public double temperatureEquatorInput { get; set; }
        public double temperaturePoleInput { get; set; }
        public double heightExponentInput { get; set; }

        // Map2Precipitation
        public double precInput { get; set; }
        public int[] winds { get; set; }

        public _MapJobs()
        {
            initConfig();
        }

        public void Dispose()
        {
        }
    }
}