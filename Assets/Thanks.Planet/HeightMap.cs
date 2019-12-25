using Assets;
using UnityEngine;

public class HeightMap : MonoBehaviour
{
    private MeshSplit landzs { get; set; }
    // Start is called before the first frame update
    void Start()
    {
        landzs = MeshSplit.createMesh(transform, "map mesh");

        var data = new PointsData(234, 1000, 1000, 45, 6);
        var tmp = new short[data.points.Length * 2];
        for (int i = 0; i < data.points.Length; ++i)
        {
            var p = data.points[i];
            tmp[i * 2] = (short)p.x;
            tmp[i * 2 + 1] = (short)p.y;
        }
        var points_f = Loader.applyJitter(tmp, 5 * 5 * 0.5f, 234);

        //var delaunator = Delaunator.from(points_f);

    }

    // Update is called once per frame
    void Update()
    {

    }
}
