using System;

namespace Assets.MapUtil
{
    public class FlatQueue2
    {
        class _Flat
        {
            public int id { get; set; }
            public float value { get; set; }
        }

        private _Flat[] flat { get; set; }
        private int length { get; set; }

        public FlatQueue2()
        {
            flat = new _Flat[51510];
            length = 0;
        }

        public void clear()
        {
            length = 0;
        }

        public int size()
        {
            return length;
        }

        public void push(int id, float value)
        {
            flat[length] = new _Flat() { id = id, value = value };
            int pos = length++;

            while (pos > 0)
            {
                int parent = (pos - 1) >> 1;
                var fdp = flat[parent];
                float parentValue = fdp.value;
                if (value >= parentValue) break;

                var fdc = flat[pos];
                fdc.id = fdp.id;
                fdc.value = fdp.value;
                pos = parent;
            }

            var tmp = flat[pos];
            tmp.id = id;
            tmp.value = value;
        }

        public int pop()
        {
            if (size() == 0) { throw new Exception("length == 0"); };

            int top = flat[0].id;
            length--;

            if (length > 0)
            {
                var fda = flat[0];
                var fdb = flat[length];

                var id = fda.id = fdb.id;
                var value = fda.value = fdb.value;
                var halfLength = length >> 1;

                int pos = 0;

                while (pos < halfLength)
                {
                    int left = (pos << 1) + 1;
                    var right = left + 1;

                    var ldp = flat[left];
                    var rdp = flat[right];
                    var bestIndex = ldp.id;
                    var bestValue = ldp.value;
                    var rightValue = rdp.value;

                    if (right < length && rightValue < bestValue)
                    {
                        left = right;
                        bestIndex = rdp.id;
                        bestValue = rightValue;
                    }
                    if (bestValue >= value) break;

                    var fdc = flat[pos];
                    fdc.id = bestIndex;
                    fdc.value = bestValue;
                    pos = left;
                }

                var tmp = flat[pos];
                tmp.id = id;
                tmp.value = value;
            }

            return top;
        }
    }
}
