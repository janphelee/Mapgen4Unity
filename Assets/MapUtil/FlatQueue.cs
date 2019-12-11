using System;
using System.Collections.Generic;
using System.Collections; // 导入ArrayList的命名空间

namespace Assets.MapUtil
{
    public class FlatQueue<K, V> where K : IComparable where V : IComparable
    {
        List<K> ids { get; set; }
        List<V> values { get; set; }
        int length;
        public FlatQueue()
        {
            this.ids = new List<K>();
            this.values = new List<V>();
            this.length = 0;
        }

        public void clear()
        {
            this.length = 0;
            this.ids.Clear();
            this.values.Clear();
        }

        public int size()
        {
            return this.length;
        }

        public void push(K id, V value)
        {
            this.ids.Add(id);
            this.values.Add(value);

            int pos = this.length++;
            while (pos > 0)
            {
                int parent = (pos - 1) >> 1;
                V parentValue = this.values[parent];
                if (value.CompareTo(parentValue) >= 0) break;
                this.ids[pos] = this.ids[parent];
                this.values[pos] = parentValue;
                pos = parent;
            }

            this.ids[pos] = id;
            this.values[pos] = value;
        }

        public K pop()
        {
            if (this.length == 0) { throw new Exception("length == 0"); };

            var top = this.ids[0];
            this.length--;

            if (this.length > 0)
            {
                var id = this.ids[0] = this.ids[this.length];
                var value = this.values[0] = this.values[this.length];
                var halfLength = this.length >> 1;

                int pos = 0;

                while (pos < halfLength)
                {
                    int left = (pos << 1) + 1;
                    var right = left + 1;
                    var bestIndex = this.ids[left];
                    var bestValue = this.values[left];
                    var rightValue = this.values[right];

                    if (right < this.length && rightValue.CompareTo(bestValue) < 0)
                    {
                        left = right;
                        bestIndex = this.ids[right];
                        bestValue = rightValue;
                    }
                    if (bestValue.CompareTo(value) >= 0) break;

                    this.ids[pos] = bestIndex;
                    this.values[pos] = bestValue;
                    pos = left;
                }

                this.ids[pos] = id;
                this.values[pos] = value;
            }

            this.ids.RemoveAt(this.length);
            this.values.RemoveAt(this.length);

            return top;
        }

        public K peek()
        {
            return this.ids[0];
        }

        public V peekValue()
        {
            return this.values[0];
        }
    }
}
