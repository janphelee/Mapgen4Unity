using System;
using System.Threading;
using UnityEngine;

namespace Assets.Map
{
    class MapWorker
    {
        public class _Mesh
        {
            public int[] triangles;
            public Vector3[] vertices;
            public Vector2[] uv;
        }

        public delegate void Callback(int i);
        public delegate void CallbackBuffer(int i, _Mesh[] buffers);

        private MapPainting painting { get; set; }
        private MapData mapData { get; set; }
        private Callback callback { get; set; }

        private Thread thread { get; set; }
        private bool requestWork { get; set; }

        public _Mesh landsBuffer = new _Mesh();
        public _Mesh waterBuffer = new _Mesh();

        public MapWorker(MapPainting painting, MapData mapData, Callback callback)
        {
            this.painting = painting;
            this.mapData = mapData;
            this.callback = callback;
        }

        public void start()
        {
            if (thread != null)
            {
                requestWork = true;
                return;
            }

            requestWork = false;
            thread = new Thread(new ThreadStart(process));
            thread.Start();
        }
        public void getBuffer(CallbackBuffer callback)
        {
            lock (this)
            {
                callback?.Invoke(0, new _Mesh[] { waterBuffer, landsBuffer });
            }
        }
        private void process()
        {
            var t1 = DateTime.Now.Ticks;
            painting.setElevationParam(/*int seed = 187, float island = 0.5f*/);

            mapData.assignElevation(painting);//海拔地势
            mapData.assignRainfall();//风带植被
            mapData.assignRivers();//河流

            lock (this)
            {
                mapData.setRiverGeometry(out waterBuffer.vertices, out waterBuffer.uv, out waterBuffer.triangles);
                mapData.setMeshGeometry(out landsBuffer.vertices);
                mapData.setMapGeometry(out landsBuffer.uv, out landsBuffer.triangles);
            }
            var t2 = DateTime.Now.Ticks;
            var elasped = t2 - t1;
            callback?.Invoke((int)elasped);

            if (requestWork)
            {
                requestWork = false;
                thread = new Thread(new ThreadStart(process));
                thread.Start();
            }
            else
            {
                thread = null;
            }
        }
    }
}
