using UnityEngine;
using Phevolution;
using Random = Phevolution.Random;
using System.Collections.Generic;
using System.Linq;

namespace Thanks.Fantasy
{
    public class FantasyMesh : MonoBehaviour
    {

        public _MapJobs mapJobs { get; private set; }
        private bool needRender { get; set; }

        private void Awake()
        {
            mapJobs = new _MapJobs();
            mapJobs.processAsync(onCallback);
        }

        // Update is called once per frame
        private void Update()
        {
            if (!needRender) return;
            needRender = !needRender;

            var ff = mapJobs.pack.features;
            for (var i = 0; i < ff.Length; ++i)
            {
                var points = mapJobs.pack.getFeaturePoints(i);
                if (points == null) continue;

                var v3 = new List<Vector3>();
                foreach (var p in points)
                {
                    v3.Add(new Vector3((float)p[0], 0, (float)p[1]));
                }

                var line = new GameObject($"feature_{i}_{v3.Count}");
                line.transform.SetParent(transform);
                var render = line.AddComponent<LineRenderer>();
                render.positionCount = v3.Count;
                render.SetPositions(v3.ToArray());
                render.startWidth = 0.5f;
                render.endWidth = 0.5f;
                render.loop = true;
            }
        }

        public readonly List<int> elapsedMs = new List<int>();
        private void onCallback(long elapsed)
        {
            elapsedMs.Add((int)elapsed);
            if (elapsedMs.Count > 5) elapsedMs.RemoveAt(0);
            needRender = true;
        }
    }
}