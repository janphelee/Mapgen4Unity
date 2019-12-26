using Assets;
using UnityEngine;

namespace Thanks.Planet
{
    public class PlanetMesh : MonoBehaviour
    {
        private _MapJobs mapJobs { get; set; }

        private MeshSplit landzs { get; set; }

        public int seed = 123;
        public int N = 10000;
        public int P = 20;
        public float jitter = 0.75f;

        public Texture2D u_colormap;

        void Start()
        {
            mapJobs = new _MapJobs();
            mapJobs.generateMesh(N, P, jitter, seed);

            var geo = mapJobs.geometry;
            landzs = MeshSplit.createMesh(transform, "map mesh");
            landzs.setup(
                geo.flat_xyz.ToArray(),
                geo.flat_i.ToArray(),
                geo.flat_em.ToArray(), Shader.Find("Thanks.Planet/PlanetLand"));
            landzs.setTexture("u_colormap", u_colormap);
        }
        private void OnDestroy()
        {
            mapJobs.Dispose();
        }
    }
}