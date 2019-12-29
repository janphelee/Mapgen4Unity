using System;

namespace Phevolution
{
    public delegate void OnChange(int i);

    public interface IChanged
    {
        int i { get; set; }
        int t { get; set; }// 0 整型数 1 浮点数 2 字符串 3 布尔类型
        OnChange changed { get; set; }
    }

    public class Change<T> : IChanged where T : IEquatable<T>
    {
        public int i { get; set; }
        public int t { get; set; }
        public OnChange changed { get; set; }
        public T v
        {
            get { return _v; }
            set
            {
                if (!value.Equals(_v))
                {
                    _v = value;
                    changed?.Invoke(i);
                }
            }
        }

        private T _v = default;
    }

    public class ChangeI : Change<int>
    {
        public const int T = 0;
        public ChangeI(int i, int v, OnChange cb)
        {
            this.i = i;
            this.v = v;
            t = T;
            changed = cb;
        }
    }
    public class ChangeF : Change<float>
    {
        public const int T = 1;
        public ChangeF(int i, float v, OnChange cb)
        {
            this.i = i;
            this.v = v;
            t = T;
            changed = cb;
        }
    }
    public class ChangeS : Change<string>
    {
        public const int T = 2;
        public ChangeS(int i, string v, OnChange cb)
        {
            this.i = i;
            this.v = v;
            t = T;
            changed = cb;
        }
    }
    public class ChangeB : Change<bool>
    {
        public const int T = 3;
        public ChangeB(int i, bool v, OnChange cb)
        {
            this.i = i;
            this.v = v;
            t = T;
            changed = cb;
        }
    }
}