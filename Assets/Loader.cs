using Assets.DualMesh;
using Assets.MapUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets
{
    public class Loader : MonoBehaviour
    {
        private float jitter = 0.5f;
        public float spacing = 5f;
        public int seed = 187;

        [SerializeField]
        private MapMesh mapMesh = null;

        // Start is called before the first frame update
        IEnumerator Start()
        {
            string path = $"file://{Application.streamingAssetsPath}/points-5.data";

            var req = UnityWebRequest.Get(path);
            yield return req.SendWebRequest();

            extractPoints(req.downloadHandler.data, (points, peaks_index) =>
            {
                var points_f = applyJitter(points, spacing * jitter * 0.5f, seed);
                Debug.Log($"dphe points:{points_f.Count} peaks:{peaks_index.Length}");

                var builder = new MeshBuilder(spacing * 1.5f).addPoints(points_f);
                var graph = builder.create();

                if (mapMesh != null) mapMesh.setup(seed, graph, peaks_index, spacing);
            });
        }

        void extractPoints(byte[] bytes, Action<short[], short[]> callback)
        {
            var pointData = new short[bytes.Length / 2];
            Buffer.BlockCopy(bytes, 0, pointData, 0, bytes.Length);

            var numMountainPeaks = pointData[0];

            var peaks_index = new short[numMountainPeaks];
            Array.Copy(pointData, 1, peaks_index, 0, numMountainPeaks);

            var numRegions = (pointData.Length - 1 - numMountainPeaks) / 2;
            List<short> points = new List<short>();
            for (int i = 0; i < numRegions; i++)
            {
                var j = 1 + numMountainPeaks + 2 * i;
                points.Add(pointData[j]);
                points.Add(pointData[j + 1]);
            }
            callback(points.ToArray(), peaks_index);
        }

        List<float[]> applyJitter(short[] points, float dr, int seed)
        {
            var randFloat = Rander.makeRandFloat(seed);

            var tmp = new List<float[]>();
            for (int i = 0; i < points.Length / 2; ++i)
            {
                var r = dr * Math.Sqrt(Math.Abs(randFloat()));
                var a = Math.PI * randFloat();
                var dx = r * Math.Cos(a);
                var dy = r * Math.Sin(a);
                tmp.Add(new float[] { (float)(points[i * 2] + dx), (float)(points[i * 2 + 1] + dy) });
            }
            return tmp;
        }
    }
}