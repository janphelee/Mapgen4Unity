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
        private float spacing = 5f;
        private int seed = 187;

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
                var mesh = builder.create();
                Debug.Log($"triangles = {mesh.numTriangles} regions = {mesh.numRegions}");

                mesh.s_length = new float[mesh.numSides];
                for (var s = 0; s < mesh.numSides; s++)
                {
                    int r1 = mesh.s_begin_r(s),
                        r2 = mesh.s_end_r(s);
                    float dx = mesh.r_x(r1) - mesh.r_x(r2),
                        dy = mesh.r_y(r1) - mesh.r_y(r2);
                    mesh.s_length[s] = (float)Math.Sqrt(dx * dx + dy * dy);
                }

                /* The input points get assigned to different positions in the
                 * output mesh. The peaks_index has indices into the original
                 * array. This test makes sure that the logic for mapping input
                 * indices to output indices hasn't changed. */
                short p1 = points[210 * 2], p2 = points[210 * 2 + 1];
                short
                p3 = (short)Math.Round(mesh.r_x(210 + mesh.numBoundaryRegions)),
                p4 = (short)Math.Round(mesh.r_y(210 + mesh.numBoundaryRegions));
                if (p1 != p3 || p2 != p4)
                {
                    Debug.Log($"Mapping from input points to output points has changed p:{p1},{p2} r:{p3},{p4}");
                }

                var peaks_t = new List<int>();
                foreach (var i in peaks_index)
                {
                    var r = i + mesh.numBoundaryRegions;
                    peaks_t.Add(mesh.s_inner_t(mesh._r_in_s[r]));
                }

                setup(mesh, peaks_t.ToArray());
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

        void setup(MeshData mesh, int[] peaks_t)
        {
            if (mapMesh != null)
                mapMesh.setup(mesh, peaks_t, spacing);
        }
    }
}