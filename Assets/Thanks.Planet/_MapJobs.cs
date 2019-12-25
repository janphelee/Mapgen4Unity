using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if Use_Double_Float
using Float = System.Double;
using Float2 = Unity.Mathematics.double2;
#else
using Float = System.Single;
using Float2 = Unity.Mathematics.float2;
#endif

namespace Thanks.Planet
{
    public class _MapJobs : IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}