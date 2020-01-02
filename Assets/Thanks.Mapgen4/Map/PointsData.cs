using System;
using System.IO;

namespace Assets
{
    using UInt16 = UInt16;

    public class PointsData
    {
        public static void loadFromFile(string path)
        {
            FileInfo file = new FileInfo(path);
            var stream = file.Create();
            var length = stream.Length;
            var bytes = new byte[length];
            int ret = stream.Read(bytes, 0, (int)length);
            if (ret != length) { }

            var pointData = new PointsData();

            var data = new short[ret / 2];
            Buffer.BlockCopy(bytes, 0, data, 0, ret);

            var numPeaks = data[0];
            var peaks = new UInt16[numPeaks];
            Array.Copy(data, 1, peaks, 0, numPeaks);

            var numPoints = (data.Length - 1 - numPeaks) / 2;
            var points = new UInt16[numPoints][];
            for (int i = 0; i < numPoints; i++)
            {
                var j = 1 + numPeaks + i * 2;
                points[i] = new UInt16[] { (UInt16)data[j], (UInt16)data[j + 1] };
            }

            var pd = new PointsData()
            {
                peaks = peaks,
                points = points,
            };
        }

        public UInt16 numMountainPeaks { get { return (UInt16)peaks.Length; } }
        public UInt16[] peaks { get; private set; }

        public UInt16 numRegionPoints { get { return (UInt16)points.Length; } }
        public UInt16[][] points { get; private set; }

        private PointsData()
        {

        }

        public PointsData(int seed, int width, int height, float mountainSpacing, float spacing)
        {
            var shape = new int[] { width, height };
            var poissonPeaks = new PoissonDiskSampling(shape, mountainSpacing, 0, 0, Rander.makeRandFloat(seed)).fill();

            var generator = new PoissonDiskSampling(shape, spacing, 0, 0, Rander.makeRandFloat(seed));
            foreach (var p in poissonPeaks)
            {
                generator.addPoint(p);
            }
            var mesh = generator.fill();
            var tmpPoints = new int[mesh.Count];
            var indexs = new int[mesh.Count];
            for (int i = 0; i < indexs.Length; ++i)
            {
                var p = mesh[i];
                tmpPoints[i] = ((int)p[0]) << 16 | (int)p[1];
                indexs[i] = i;
            }
            Array.Sort(tmpPoints, indexs);

            points = new UInt16[tmpPoints.Length][];
            peaks = new UInt16[poissonPeaks.Count];

            int count = 0;
            for (int i = 0; i < indexs.Length; ++i)
            {
                var p = tmpPoints[i];
                points[i] = new UInt16[] { (UInt16)((p >> 16) & 0xffff), (UInt16)(p & 0xffff) };
                if (indexs[i] < peaks.Length) peaks[count++] = (UInt16)i;
            }
        }
    }
}