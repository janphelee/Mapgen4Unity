using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

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

            var w = new Stopwatch();
            w.Start();

            var local = Vector3.zero;
            foreach (var geom in mapJobs.heightmap)
            {
                var obj = new GameObject(geom.name);
                obj.transform.SetParent(transform);
                obj.AddComponent<PolygonMesh>().SetPositions(geom.ppp, geom.iii, geom.color);

                obj.transform.localPosition = local;
                local.z -= 0.1f;
            }

            Debug.Log($"fillmesh {w.ElapsedMilliseconds}ms");

            w.Stop();
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