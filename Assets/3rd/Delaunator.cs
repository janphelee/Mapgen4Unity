using System;
using System.Linq;

/** - [Guide to data structures](https://mapbox.github.io/delaunator/) */
class Delaunator
{
    private double EPSILON = Math.Pow(2, -52);
    private int[] EDGE_STACK = new int[512];

    private double[] coords;
    private int[] _triangles;
    private int[] _halfedges;

    private int _hashSize;
    private int[] _hullHash;
    private int[] _hullPrev;
    private int[] _hullNext;
    private int[] _hullTri;

    private int[] _ids;
    private double[] _dists;
    private int _hullStart;
    private int trianglesLen;

    private double _cx { get; set; }
    private double _cy { get; set; }

    public int[] hull { get; set; }
    public int[] triangles { get; set; }
    public int[] halfedges { get; set; }

    public Delaunator(double[] coords)
    {
        var n = coords.Length >> 1;
        this.coords = coords;

        // arrays that will store the triangulation graph
        var maxTriangles = Math.Max(2 * n - 5, 0);
        this._triangles = new int[maxTriangles * 3];
        this._halfedges = new int[maxTriangles * 3];

        // temporary arrays for tracking the edges of the advancing convex hull
        this._hashSize = (int)Math.Ceiling(Math.Sqrt(n));
        this._hullPrev = new int[n]; // edge to prev edge
        this._hullNext = new int[n]; // edge to next edge
        this._hullTri = new int[n]; // edge to adjacent triangle
        this._hullHash = new int[this._hashSize]; // angular edge hash
        for (var i = 0; i < _hullHash.Length; ++i) _hullHash[i] = -1;

        // temporary arrays for sorting points
        this._ids = new int[n];
        this._dists = new double[n];

        this.update();
    }

    private void update()
    {
        var coords = this.coords;
        var hullPrev = this._hullPrev;
        var hullNext = this._hullNext;
        var hullTri = this._hullTri;
        var hullHash = this._hullHash;
        var n = coords.Length >> 1;

        // populate an array of point indices; calculate input data bbox
        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;

        for (var i = 0; i < n; i++)
        {
            var x = coords[2 * i];
            var y = coords[2 * i + 1];
            if (x < minX) { minX = x; }
            if (y < minY) { minY = y; }
            if (x > maxX) { maxX = x; }
            if (y > maxY) { maxY = y; }
            this._ids[i] = i;
        }
        var cx = (minX + maxX) / 2;
        var cy = (minY + maxY) / 2;
        //UnityEngine.Debug.Log($"n:{n} min:{minX},{minY} max;{maxX},{maxY} cen:{cx},{cy}");

        var minDist = double.PositiveInfinity;
        int i0 = 0, i1 = 0, i2 = 0;

        // pick a seed point close to the center
        for (var i = 0; i < n; i++)
        {
            var d = dist(cx, cy, coords[2 * i], coords[2 * i + 1]);
            if (d < minDist)
            {
                i0 = i;
                minDist = d;
            }
        }
        var i0x = coords[2 * i0];
        var i0y = coords[2 * i0 + 1];

        minDist = double.PositiveInfinity;

        // find the point closest to the seed
        for (var i = 0; i < n; i++)
        {
            if (i == i0) { continue; }
            var d = dist(i0x, i0y, coords[2 * i], coords[2 * i + 1]);
            if (d < minDist && d > 0)
            {
                i1 = i;
                minDist = d;
            }
        }
        var i1x = coords[2 * i1];
        var i1y = coords[2 * i1 + 1];

        var minRadius = double.PositiveInfinity;

        // find the third point which forms the smallest circumcircle with the first two
        for (var i = 0; i < n; i++)
        {
            if (i == i0 || i == i1) { continue; }
            var r = circumradius(i0x, i0y, i1x, i1y, coords[2 * i], coords[2 * i + 1]);
            if (r < minRadius)
            {
                i2 = i;
                minRadius = r;
            }
        }
        var i2x = coords[2 * i2];
        var i2y = coords[2 * i2 + 1];

        if (minRadius == double.PositiveInfinity)
        {
            // order collinear points by dx (or dy if all x are identical)
            // and return the list as a hull
            for (var i = 0; i < n; i++)
            {
                var dx = coords[2 * i] - coords[0];
                var dy = coords[2 * i + 1] - coords[1];
                this._dists[i] = dx > 0 ? dx : dy;
            }
            quicksort(this._ids, this._dists, 0, n - 1);

            var hull = new int[n];
            var j = 0;
            double d0 = double.NegativeInfinity;
            for (var i = 0; i < n; i++)
            {
                var id = this._ids[i];
                if (this._dists[id] > d0)
                {
                    hull[j++] = id;
                    d0 = this._dists[id];
                }
            }
            this.hull = hull.Take(j).ToArray();
            this.triangles = new int[0];
            this.halfedges = new int[0];
            return;
        }

        // swap the order of the seed points for counter-clockwise orientation
        if (orient(i0x, i0y, i1x, i1y, i2x, i2y))
        {
            var i = i1;
            var x = i1x;
            var y = i1y;
            i1 = i2;
            i1x = i2x;
            i1y = i2y;
            i2 = i;
            i2x = x;
            i2y = y;
        }

        var center = circumcenter(i0x, i0y, i1x, i1y, i2x, i2y);
        this._cx = center[0];
        this._cy = center[1];

        for (var i = 0; i < n; i++)
        {
            this._dists[i] = dist(coords[2 * i], coords[2 * i + 1], center[0], center[1]);
        }

        // sort the points by distance from the seed triangle circumcenter
        quicksort(this._ids, this._dists, 0, n - 1);

        // set up the seed triangle as the starting hull
        this._hullStart = i0;
        var hullSize = 3;

        hullNext[i0] = hullPrev[i2] = i1;
        hullNext[i1] = hullPrev[i0] = i2;
        hullNext[i2] = hullPrev[i1] = i0;

        hullTri[i0] = 0;
        hullTri[i1] = 1;
        hullTri[i2] = 2;

        for (var i = 0; i < hullHash.Length; ++i) hullHash[i] = -1;
        hullHash[this._hashKey(i0x, i0y)] = i0;
        hullHash[this._hashKey(i1x, i1y)] = i1;
        hullHash[this._hashKey(i2x, i2y)] = i2;

        this.trianglesLen = 0;
        this._addTriangle(i0, i1, i2, -1, -1, -1);

        double xp = 0;
        double yp = 0;
        for (var k = 0; k < this._ids.Length; k++)
        {
            var i_8 = this._ids[k];
            var x_2 = coords[2 * i_8];
            var y_2 = coords[2 * i_8 + 1];

            // skip near-duplicate points
            if (k > 0 && Math.Abs(x_2 - xp) <= EPSILON && Math.Abs(y_2 - yp) <= EPSILON) { continue; }
            xp = x_2;
            yp = y_2;

            // skip seed triangle points
            if (i_8 == i0 || i_8 == i1 || i_8 == i2) { continue; }

            // find a visible edge on the convex hull using edge hash
            int start = 0;
            var key = this._hashKey(x_2, y_2);
            for (var j_1 = 0; j_1 < this._hashSize; j_1++)
            {
                start = hullHash[(key + j_1) % this._hashSize];
                if (start != -1 && start != hullNext[start]) { break; }
            }

            start = hullPrev[start];
            var e = start;
            var q = hullNext[e];

            while (!orient(x_2, y_2, coords[2 * e], coords[2 * e + 1], coords[2 * q], coords[2 * q + 1]))
            {
                e = q;
                if (e == start)
                {
                    e = -1;// a near-duplicate point
                    break;
                }
                q = hullNext[e];
            }
            if (e == -1) { continue; } // likely a near-duplicate point; skip it

            // add the first triangle from the point
            var t = this._addTriangle(e, i_8, hullNext[e], -1, -1, hullTri[e]);

            // recursively flip triangles from the point until they satisfy the Delaunay condition
            hullTri[i_8] = this._legalize(t + 2);
            hullTri[e] = t; // keep track of boundary triangles on the hull
            hullSize++;

            // walk forward through the hull, adding more triangles and flipping recursively
            var next = hullNext[e];
            q = hullNext[next];

            while (orient(x_2, y_2, coords[2 * next], coords[2 * next + 1], coords[2 * q], coords[2 * q + 1]))
            {
                t = this._addTriangle(next, i_8, q, hullTri[i_8], -1, hullTri[next]);
                hullTri[i_8] = this._legalize(t + 2);
                hullNext[next] = next; // mark as removed
                hullSize--;
                next = q;

                q = hullNext[next];
            }

            // walk backward from the other side, adding more triangles and flipping
            if (e == start)
            {
                q = hullPrev[e];

                while (orient(x_2, y_2, coords[2 * q], coords[2 * q + 1], coords[2 * e], coords[2 * e + 1]))
                {
                    t = this._addTriangle(q, i_8, e, -1, hullTri[e], hullTri[q]);
                    this._legalize(t + 2);
                    hullTri[q] = t;
                    hullNext[e] = e; // mark as removed
                    hullSize--;
                    e = q;

                    q = hullPrev[e];
                }
            }

            // update the hull indices
            this._hullStart = hullPrev[i_8] = e;
            hullNext[e] = hullPrev[next] = i_8;
            hullNext[i_8] = next;

            // save the two new edges in the hash table
            hullHash[this._hashKey(x_2, y_2)] = i_8;
            hullHash[this._hashKey(coords[2 * e], coords[2 * e + 1])] = e;
        }

        this.hull = new int[hullSize];
        for (int i = 0, s = this._hullStart; i < hullSize; i++)
        {
            this.hull[i] = s;
            s = hullNext[s];
        }

        // trim typed triangle mesh arrays
        this.triangles = this._triangles.Take(trianglesLen).ToArray();
        this.halfedges = this._halfedges.Take(trianglesLen).ToArray();
    }

    private int _hashKey(double x, double y)
    {
        return (int)Math.Floor(pseudoAngle(x - this._cx, y - this._cy) * this._hashSize) % this._hashSize;
    }

    private int _legalize(int a)
    {
        var triangles = this._triangles;
        var halfedges = this._halfedges;
        var coords = this.coords;

        var i = 0;
        var ar = 0;

        // recursion eliminated with a fixed-size stack
        while (true)
        {
            var b = halfedges[a];

            /* if the pair of triangles doesn't satisfy the Delaunay condition
             * (p1 is inside the circumcircle of [p0, pl, pr]), flip them,
             * then do the same check/flip recursively for the new pair of triangles
             *
             *       pl                pl
             *      /||\              /  \
             *   al/ || \bl        al/\a
             *    /  ||  \          /  \
             *   /  a||b  \flip/___ar___\
             * p0\   ||   /p1   =>   p0\---bl---/p1
             *    \  ||  /          \  /
             *   ar\ || /br         b\/br
             *      \||/              \  /
             *       pr                pr
             */
            var a0 = a - a % 3;
            ar = a0 + (a + 2) % 3;

            if (b == -1)
            { // convex hull edge
                if (i == 0) { break; }
                a = EDGE_STACK[--i];
                continue;
            }

            var b0 = b - b % 3;
            var al = a0 + (a + 1) % 3;
            var bl = b0 + (b + 2) % 3;

            var p0 = triangles[ar];
            var pr = triangles[a];
            var pl = triangles[al];
            var p1 = triangles[bl];

            var illegal = inCircle(
                coords[2 * p0], coords[2 * p0 + 1],
                coords[2 * pr], coords[2 * pr + 1],
                coords[2 * pl], coords[2 * pl + 1],
                coords[2 * p1], coords[2 * p1 + 1]);

            if (illegal)
            {
                triangles[a] = p1;
                triangles[b] = p0;

                var hbl = halfedges[bl];

                // edge swapped on the other side of the hull (rare); fix the halfedge reference
                if (hbl == -1)
                {
                    var e = this._hullStart;
                    do
                    {
                        if (this._hullTri[e] == bl)
                        {
                            this._hullTri[e] = a;
                            break;
                        }
                        e = this._hullPrev[e];
                    } while (e != this._hullStart);
                }
                this._link(a, hbl);
                this._link(b, halfedges[ar]);
                this._link(ar, bl);

                var br = b0 + (b + 1) % 3;

                // don't worry about hitting the cap: it can only happen on extremely degenerate input
                if (i < EDGE_STACK.Length)
                {
                    EDGE_STACK[i++] = br;
                }
            }
            else
            {
                if (i == 0) { break; }
                a = EDGE_STACK[--i];
            }
        }

        return ar;
    }

    private void _link(int a, int b)
    {
        this._halfedges[a] = b;
        if (b != -1) { this._halfedges[b] = a; }
    }

    private int _addTriangle(int i0, int i1, int i2, int a, int b, int c)
    {
        var t = this.trianglesLen;

        this._triangles[t] = i0;
        this._triangles[t + 1] = i1;
        this._triangles[t + 2] = i2;

        this._link(t, a);
        this._link(t + 1, b);
        this._link(t + 2, c);

        this.trianglesLen += 3;

        return t;
    }

    // monotonically increases with real angle, but doesn't need expensive trigonometry
    private static double pseudoAngle(double dx, double dy)
    {
        var p = dx / (Math.Abs(dx) + Math.Abs(dy));
        return (dy > 0 ? 3 - p : 1 + p) / 4; // [0..1]
    }

    private static double dist(double ax, double ay, double bx, double by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return dx * dx + dy * dy;
    }

    // return 2d orientation sign if we're confident in it through J. Shewchuk's error bound check
    private static double orientIfSure(double px, double py, double rx, double ry, double qx, double qy)
    {
        var l = (ry - py) * (qx - px);
        var r = (rx - px) * (qy - py);
        return (Math.Abs(l - r) >= 3.3306690738754716e-16 * Math.Abs(l + r)) ? l - r : 0;
    }

    private static bool orient(double rx, double ry, double qx, double qy, double px, double py)
    {
        var sign = orientIfSure(px, py, rx, ry, qx, qy);
        if (sign == 0)
        {
            sign = orientIfSure(rx, ry, qx, qy, px, py);
            if (sign == 0)
            {
                sign = orientIfSure(qx, qy, px, py, rx, ry);
            }
        }
        return sign < 0;
    }

    private static bool inCircle(double ax, double ay, double bx, double by, double cx, double cy, double px, double py)
    {
        var dx = ax - px;
        var dy = ay - py;
        var ex = bx - px;
        var ey = by - py;
        var fx = cx - px;
        var fy = cy - py;

        var ap = dx * dx + dy * dy;
        var bp = ex * ex + ey * ey;
        var cp = fx * fx + fy * fy;

        return dx * (ey * cp - bp * fy) -
               dy * (ex * cp - bp * fx) +
               ap * (ex * fy - ey * fx) < 0;
    }

    private static double circumradius(double ax, double ay, double bx, double by, double cx, double cy)
    {
        var dx = bx - ax;
        var dy = by - ay;
        var ex = cx - ax;
        var ey = cy - ay;

        var bl = dx * dx + dy * dy;
        var cl = ex * ex + ey * ey;
        var d = 0.5 / (dx * ey - dy * ex);

        var x = (ey * bl - dy * cl) * d;
        var y = (dx * cl - ex * bl) * d;

        return x * x + y * y;
    }

    private static double[] circumcenter(double ax, double ay, double bx, double by, double cx, double cy)
    {
        var dx = bx - ax;
        var dy = by - ay;
        var ex = cx - ax;
        var ey = cy - ay;

        var bl = dx * dx + dy * dy;
        var cl = ex * ex + ey * ey;
        var d = 0.5 / (dx * ey - dy * ex);

        var x = ax + (ey * bl - dy * cl) * d;
        var y = ay + (dx * cl - ex * bl) * d;

        return new double[] { x, y };
    }

    private static void quicksort(int[] ids, double[] dists, int left, int right)
    {
        if (right - left <= 20)
        {
            for (var i = left + 1; i <= right; i++)
            {
                var temp = ids[i];
                var tempDist = dists[temp];
                var j = i - 1;
                while (j >= left && dists[ids[j]] > tempDist) { ids[j + 1] = ids[j--]; }
                ids[j + 1] = temp;
            }
        }
        else
        {
            var median = (left + right) >> 1;
            var i = left + 1;
            var j = right;
            swap(ids, median, i);
            if (dists[ids[left]] > dists[ids[right]]) { swap(ids, left, right); }
            if (dists[ids[i]] > dists[ids[right]]) { swap(ids, i, right); }
            if (dists[ids[left]] > dists[ids[i]]) { swap(ids, left, i); }

            var temp_1 = ids[i];
            var tempDist_1 = dists[temp_1];
            while (true)
            {
                do { i++; } while (dists[ids[i]] < tempDist_1);
                do { j--; } while (dists[ids[j]] > tempDist_1);
                if (j < i) { break; }
                swap(ids, i, j);
            }
            ids[left + 1] = ids[j];
            ids[j] = temp_1;

            if (right - i + 1 >= j - left)
            {
                quicksort(ids, dists, i, right);
                quicksort(ids, dists, left, j - 1);
            }
            else
            {
                quicksort(ids, dists, left, j - 1);
                quicksort(ids, dists, i, right);
            }
        }
    }

    private static void swap<T>(T[] arr, int i, int j)
    {
        var tmp = arr[i];
        arr[i] = arr[j];
        arr[j] = tmp;
    }
}
