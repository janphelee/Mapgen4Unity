using UnityEngine;
using System;
using System.Collections.Generic;

namespace Thanks.Fantasy
{
    public class Geometry : IDisposable
    {
        public string name { get; set; }
        public Color color { get; set; }
        public Vector3[] ppp { get; set; }
        public int[] iii { get; set; }

        public void Dispose()
        {
        }
    }
}