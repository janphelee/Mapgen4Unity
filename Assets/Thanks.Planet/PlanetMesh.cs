using Assets;
using UnityEngine;

namespace Thanks.Planet
{
    public class PlanetMesh : MonoBehaviour
    {
        public _MapJobs mapJobs { get; private set; }

        private MeshSplit landzs { get; set; }

        public Texture2D u_colormap;

        private bool needUpdate { get; set; }

        void Start()
        {
            mapJobs = new _MapJobs();
            mapJobs.processAsync(t => needUpdate = true);

            landzs = MeshSplit.createMesh(transform, "map mesh");
        }
        private void OnDestroy()
        {
            mapJobs.Dispose();
        }

        private void Update()
        {
            if (!needUpdate) return;
            needUpdate = false;

            var geo = mapJobs.geometry;
            var shader = geo.quad ?
                Shader.Find("Thanks.Planet/PlanetQuadLand") :
                Shader.Find("Thanks.Planet/PlanetLand");
            landzs.setup(
                geo.flat_xyz.ToArray(),
                geo.flat_em.ToArray(),
                geo.flat_i.ToArray(), shader);
            landzs.setShader(shader);
            landzs.setTexture("u_colormap", u_colormap);
        }
    }
}