using System;
using System.Collections.Generic;

namespace Phevolution
{
    public partial class D3
    {
        public class IQuadtree<K, V>
        {
            public class Value
            {
                public K x, y;
                public V v;
                public Value(K x, K y, V v)
                {
                    this.x = x;
                    this.y = y;
                    this.v = v;
                }
            }
            protected class Node
            {
                public Value data { get; set; }
                public Node next { get; set; }
                public Node[] child { get; set; }
            }

            public delegate K KeyOf(Value d);
            public static K defaultX_1(Value d) { return d.x; }
            public static K defaultY_1(Value d) { return d.y; }

            protected KeyOf _x { get; set; }
            protected KeyOf _y { get; set; }
            protected K _x0 { get; set; }
            protected K _y0 { get; set; }
            protected K _x1 { get; set; }
            protected K _y1 { get; set; }
            protected Node _root { get; set; }

            public IQuadtree(KeyOf _x = null, KeyOf _y = null)
            {
                this._x = _x != null ? _x : defaultX_1;
                this._y = _y != null ? _y : defaultY_1;
            }

        }

        public class Quadtree : IQuadtree<double, int>
        {

            public static Quadtree add(Quadtree tree, double x, double y, Value d)
            {
                if (double.IsNaN(x) || double.IsNaN(y)) return tree; // ignore invalid points

                var node = tree._root;
                var leaf = new Node() { data = d };

                double x0 = tree._x0,
                       y0 = tree._y0,
                       x1 = tree._x1,
                       y1 = tree._y1,
                       xm,
                       ym,
                       xp,
                       yp;
                bool right, bottom;

                // If the tree is empty, initialize the root as a leaf.
                if (node == null)
                {
                    tree._root = leaf;
                    return tree;
                }

                Node parent = null;
                int i = -1, j = -1;
                // Find the existing leaf for the new point, or add it.
                while (node.child != null)
                {
                    if (right = x >= (xm = (x0 + x1) / 2)) x0 = xm; else x1 = xm;
                    if (bottom = y >= (ym = (y0 + y1) / 2)) y0 = ym; else y1 = ym;

                    parent = node;
                    i = ((bottom ? 1 : 0) << 1) | (right ? 1 : 0);

                    node = node.child[i];
                    if (node == null) { parent.child[i] = leaf; return tree; }
                }

                // Is the new point is exactly coincident with the existing point?
                xp = tree._x.Invoke(node.data);
                yp = tree._y.Invoke(node.data);
                if (x == xp && y == yp)
                {
                    leaf.next = node;
                    if (parent != null) parent.child[i] = leaf; else tree._root = leaf;
                    return tree;
                }

                // Otherwise, split the leaf node until the old and new point are separated.
                do
                {
                    if (parent != null)
                        parent = parent.child[i] = new Node() { child = new Node[4] };
                    else
                        parent = tree._root = new Node() { child = new Node[4] };
                    if (right = x >= (xm = (x0 + x1) / 2)) x0 = xm; else x1 = xm;
                    if (bottom = y >= (ym = (y0 + y1) / 2)) y0 = ym; else y1 = ym;

                    i = (bottom ? 1 : 0) << 1 | (right ? 1 : 0);
                    j = ((yp >= ym) ? 1 : 0) << 1 | (xp >= xm ? 1 : 0);
                } while (i == j);

                parent.child[j] = node;
                parent.child[i] = leaf;
                return tree;
            }

            public Quadtree(KeyOf x, KeyOf y) : base(x, y)
            {
                _x0 = _y0 = _x1 = _y1 = double.NaN;
            }
            public Quadtree(KeyOf x, KeyOf y, double x0, double y0, double x1, double y1) : base(x, y)
            {
                _x0 = x0; _y0 = y0;
                _x1 = x1; _y1 = y1;
            }

            public Quadtree addAll(Value[] data)
            {
                int n = data.Length;
                var xz = new double[n];
                var yz = new double[n];
                var x0 = double.PositiveInfinity;
                var y0 = double.PositiveInfinity;
                var x1 = double.NegativeInfinity;
                var y1 = double.NegativeInfinity;

                // Compute the points and their extent.
                for (var i = 0; i < n; ++i)
                {
                    var d = data[i];
                    var x = _x.Invoke(d);
                    var y = _y.Invoke(d);
                    if (double.IsNaN(x) || double.IsNaN(y)) continue;

                    xz[i] = x;
                    yz[i] = y;
                    if (x < x0) x0 = x;
                    if (x > x1) x1 = x;
                    if (y < y0) y0 = y;
                    if (y > y1) y1 = y;
                }

                // If there were no (valid) points, abort.
                if (x0 > x1 || y0 > y1) return this;

                // Expand the tree to cover the new points.
                cover(x0, y0).cover(x1, y1);

                // Add the new points.
                for (var i = 0; i < n; ++i)
                {
                    add(this, xz[i], yz[i], data[i]);
                }

                return this;
            }

            public Quadtree add(Value d)
            {
                double
                    x = _x.Invoke(d),
                    y = _y.Invoke(d);
                return add(cover(x, y), x, y, d);
            }

            private Quadtree cover(double x, double y)
            {
                if (double.IsNaN(x) || double.IsNaN(y)) return this;

                double x0 = _x0,
                       y0 = _y0,
                       x1 = _x1,
                       y1 = _y1;


                // If the quadtree has no extent, initialize them.
                // Integer extent are necessary so that if we later double the extent,
                // the existing quadrant boundaries don’t change due to floating point error!
                if (double.IsNaN(x0))
                {
                    x1 = (x0 = Math.Floor(x)) + 1;
                    y1 = (y0 = Math.Floor(y)) + 1;
                }
                // Otherwise, double repeatedly to cover.
                else
                {
                    var z = x1 - x0;
                    var node = _root;

                    while (x0 > x || x >= x1 || y0 > y || y >= y1)
                    {
                        var parent = new Node()
                        {
                            child = new Node[4]
                        };
                        var i = ((y < y0 ? 1 : 0) << 1) | (x < x0 ? 1 : 0);
                        parent.child[i] = node;
                        node = parent;
                        z *= 2;

                        switch (i)
                        {
                            case 0: x1 = x0 + z; y1 = y0 + z; break;
                            case 1: x0 = x1 - z; y1 = y0 + z; break;
                            case 2: x1 = x0 + z; y0 = y1 - z; break;
                            case 3: x0 = x1 - z; y0 = y1 - z; break;
                        }
                    }

                    if (_root != null && _root.child != null) _root = node;
                }

                _x0 = x0;
                _y0 = y0;
                _x1 = x1;
                _y1 = y1;
                return this;
            }

            private class Quad
            {
                public Node node;
                public double x0, y0, x1, y1;
                public Quad(Node node, double x0, double y0, double x1, double y1)
                {
                    this.node = node;
                    this.x0 = x0;
                    this.y0 = y0;
                    this.x1 = x1;
                    this.y1 = y1;
                }
            }
            public Value find(double x, double y, double radius)
            {
                double
                    x0 = this._x0,
                    y0 = this._y0,
                    x1,
                    y1,
                    x2,
                    y2,
                    x3 = this._x1,
                    y3 = this._y1;
                var node = this._root;
                var quads = new List<Quad>();

                if (node != null) quads.Add(new Quad(node, x0, y0, x3, y3));
                if (double.IsNaN(radius)) radius = double.PositiveInfinity;
                else
                {
                    x0 = x - radius; y0 = y - radius;
                    x3 = x + radius; y3 = y + radius;
                    radius *= radius;
                }
                Quad q;
                Value data = null;
                while (quads.Count > 0 && (q = Utils.pop(quads)) != null)
                {
                    node = q.node;
                    // Stop searching if this quadrant can’t contain a closer node.
                    if (node == null
                        || (x1 = q.x0) > x3
                        || (y1 = q.y0) > y3
                        || (x2 = q.x1) < x0
                        || (y2 = q.y1) < y0) continue;

                    // Bisect the current quadrant.
                    if (node.child != null)
                    {
                        var xm = (x1 + x2) / 2;
                        var ym = (y1 + y2) / 2;

                        quads.Add(new Quad(node.child[3], xm, ym, x2, y2));
                        quads.Add(new Quad(node.child[2], x1, ym, xm, y2));
                        quads.Add(new Quad(node.child[1], xm, y1, x2, ym));
                        quads.Add(new Quad(node.child[0], x1, y1, xm, ym));

                        // Visit the closest quadrant first.
                        var i = ((y >= ym) ? 1 : 0) << 1 | ((x >= xm) ? 1 : 0);
                        if (i > 0)
                        {
                            q = quads[quads.Count - 1];
                            quads[quads.Count - 1] = quads[quads.Count - 1 - i];
                            quads[quads.Count - 1 - i] = q;
                        }
                    }

                    // Visit this point. (Visiting coincident points isn’t necessary!)
                    else
                    {
                        double
                            dx = x - _x.Invoke(node.data),
                            dy = y - _y.Invoke(node.data),
                            d2 = dx * dx + dy * dy;
                        if (d2 < radius)
                        {
                            var d = Math.Sqrt(radius = d2);
                            x0 = x - d; y0 = y - d;
                            x3 = x + d; y3 = y + d;
                            data = node.data;
                        }
                    }
                }
                return data;
            }

        }

        public static Quadtree quadtree(
            Quadtree.Value[] data = null,
            Quadtree.KeyOf x = null, Quadtree.KeyOf y = null,
            double x0 = double.NaN, double y0 = double.NaN,
            double x1 = double.NaN, double y1 = double.NaN
            )
        {
            if (x == null) x = Quadtree.defaultX_1;
            if (y == null) y = Quadtree.defaultY_1;
            var tree = new Quadtree(x, y, x0, y0, x1, y1);
            if (data != null) tree.addAll(data);
            return tree;
        }
    }
}