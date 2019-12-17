using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Assets.MapJobs
{
    unsafe class RiverTexture : IDisposable
    {
        public const int Types = 2;
        public const int Sides = 3;

        const int numRiverSizes = 24; // NOTE: too high and rivers are low quality; too low and there's not enough variation
        const int riverTextureSize = 4096;
        const int riverTextureSpacing = 40; // TODO: should depend on river size

        private int spacing, numSizes, textureSize;

        public NativeArray<Vector2> vertex { get; private set; }
        public NativeArray<Vector2> uv { get; private set; }

        public int NumSizes { get { return numSizes; } }

        public static RiverTexture createDefault()
        {
            return new RiverTexture(numRiverSizes, riverTextureSize, riverTextureSpacing);
        }

        private RiverTexture(int numSizes, int textureSize, int spacing)
        {
            this.spacing = spacing;
            this.numSizes = numSizes;
            this.textureSize = textureSize;

            var size = (numSizes + 1) * (numSizes + 1) * Types * Sides;

            vertex = new NativeArray<Vector2>(size, Allocator.Persistent);
            uv = new NativeArray<Vector2>(size, Allocator.Persistent);

            assignTextureCoordinates();
        }

        public void Dispose()
        {
            vertex.Dispose();
            uv.Dispose();
        }

        private void assignTextureCoordinates()
        {
            var width = Mathf.Floor((textureSize - 2 * spacing) / (2 * numSizes + 3)) - spacing;
            var height = Mathf.Floor((textureSize - 2 * spacing) / (numSizes + 1)) - spacing;

            var pV = (Vector2*)NativeArrayUnsafeUtility.GetUnsafePtr(vertex);
            var pU = (Vector2*)NativeArrayUnsafeUtility.GetUnsafePtr(uv);

            var SizeMax = numSizes + 1;
            for (var row = 0; row < SizeMax; ++row)
            {
                for (var col = 0; col < SizeMax; ++col)
                {
                    var baseX = spacing + (2 * spacing + 2 * width) * col;
                    var baseY = spacing + (spacing + height) * row;

                    var index = (row * SizeMax + col) * 6;
                    setXY(baseX, baseY, width, height, &pV[index]);
                    setUV(&pV[index], &pU[index]);
                }
            }
        }

        private void setXY(float baseX, float baseY, float width, float height, Vector2* xy)
        {
            xy[0] = new Vector2(baseX + width,     /**/baseY);
            xy[1] = new Vector2(baseX,             /**/baseY + height);
            xy[2] = new Vector2(baseX + 2 * width, /**/baseY + height);

            xy[3] = new Vector2(baseX + 2 * width + spacing, baseY + height);
            xy[4] = new Vector2(baseX + 3 * width + spacing, baseY);
            xy[5] = new Vector2(baseX + width + spacing, /**/baseY);
        }

        private void setUV(Vector2* xy, Vector2* uv)
        {
            for (int i = 0; i < 6; ++i)
                uv[i] = (xy[i] + Vector2.one * 0.5f) / textureSize;
        }
    }
}
