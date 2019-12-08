using System;
using System.Collections.Generic;

namespace Assets.MapUtil
{
    using static PoissonMeta;

    class PoissonDiskSampling
    {
        int[] shape { get; set; }
        int dimension { get; set; }
        float minDistance { get; set; }
        float squaredMinDistance { get; set; }
        float deltaDistance { get; set; }
        float cellSize { get; set; }
        int maxTries { get; set; }
        Rander.RandFloat rng { get; set; }
        List<int[]> neighbourhood { get; set; }

        float[] currentPoint { get; set; }
        List<float[]> processList { get; set; }
        List<float[]> samplePoints { get; set; }
        List<int> gridShape { get; set; }
        public int[] gridStride { get; set; }
        public int[] gridData { get; set; }

        /**
         * PoissonDiskSampling constructor
         * @param {Array} shape Shape of the space
         * @param {float} minDistance Minimum distance between each points
         * @param {float} [maxDistance] Maximum distance between each points, defaults to minDistance * 2
         * @param {int} [maxTries] Number of times the algorithm has to try to place a point in the neighbourhood of another points, defaults to 30
         * @param {function|null} [rng] RNG function, defaults to Math.random
         * @constructor
         */
        public PoissonDiskSampling(int[] shape, float minDistance, float maxDistance, int maxTries, Rander.RandFloat rng)
        {
            maxDistance = maxDistance > minDistance ? maxDistance : minDistance * 2;

            this.shape = shape;
            this.dimension = shape.Length;
            this.minDistance = minDistance;
            this.squaredMinDistance = minDistance * minDistance;
            this.deltaDistance = maxDistance - minDistance;
            this.cellSize = minDistance / (float)Math.Sqrt(this.dimension);
            this.maxTries = maxTries > 0 ? maxTries : 30;
            this.rng = rng;// || Math.random;

            this.neighbourhood = getNeighbourhood(this.dimension);

            this.currentPoint = null;
            this.processList = new List<float[]>();
            this.samplePoints = new List<float[]>();

            // cache grid
            this.gridShape = new List<int>();

            for (var i = 0; i < this.dimension; i++)
            {
                this.gridShape.Add((int)Math.Ceiling(shape[i] / this.cellSize));
            }

            tinyNDArray(this.gridShape, (stride, data) =>
            {
                this.gridStride = stride;
                this.gridData = data;
            }); //will store references to samplePoints
        }

        /**
         * Add a totally random point in the grid
         * @returns {Array} The point added to the grid
         */
        public float[] addRandomPoint()
        {
            var point = new float[this.dimension];
            for (int dimension = 0; dimension < this.dimension; ++dimension)
            {
                point[dimension] = rng() * shape[dimension];
            }
            return directAddPoint(point);
        }

        /**
         * Add a given point to the grid
         * @param {Array} point Point
         * @returns {Array|null} The point added to the grid, null if the point is out of the bound or not of the correct dimension
         */
        public float[] addPoint(float[] point)
        {
            var valid = true;
            if (point.Length == this.dimension)
            {
                for (int dimension = 0; dimension < this.dimension && valid; ++dimension)
                {
                    valid = (point[dimension] >= 0 && point[dimension] <= this.shape[dimension]);
                }
            }
            else
            {
                valid = false;
            }
            return valid ? directAddPoint(point) : null;
        }

        /**
         * Add a given point to the grid, without any check
         * @param {Array} point Point
         * @returns {Array} The point added to the grid
         * @protected
         */
        protected float[] directAddPoint(float[] point)
        {
            var internalArrayIndex = 0;
            var stride = this.gridStride;

            this.processList.Add(point);
            this.samplePoints.Add(point);

            for (int dimension = 0; dimension < this.dimension; dimension++)
            {
                internalArrayIndex += ((int)(point[dimension] / this.cellSize) | 0) * stride[dimension];
            }

            this.gridData[internalArrayIndex] = this.samplePoints.Count; // store the point reference

            return point;
        }

        /**
         * Check whether a given point is in the neighbourhood of existing points
         * @param {Array} point Point
         * @returns {boolean} Whether the point is in the neighbourhood of another point
         * @protected
         */
        protected bool inNeighbourhood(float[] point)
        {
            int dimensionNumber = this.dimension;

            float[] existingPoint;
            var stride = this.gridStride;

            for (int neighbourIndex = 0; neighbourIndex < this.neighbourhood.Count; neighbourIndex++)
            {
                int internalArrayIndex = 0;

                for (int dimension = 0; dimension < dimensionNumber; dimension++)
                {
                    int currentDimensionValue = ((int)(point[dimension] / this.cellSize) | 0) + this.neighbourhood[neighbourIndex][dimension];

                    if (currentDimensionValue >= 0 && currentDimensionValue < this.gridShape[dimension])
                    {
                        internalArrayIndex += currentDimensionValue * stride[dimension];
                    }
                }

                if (this.gridData[internalArrayIndex] != 0)
                {
                    existingPoint = this.samplePoints[this.gridData[internalArrayIndex] - 1];

                    if (squaredEuclideanDistance(point, existingPoint) < this.squaredMinDistance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /**
         * Try to generate a new point in the grid, returns null if it wasn't possible
         * @returns {Array|null} The added point or null
         */
        public float[] next()
        {
            float[] newPoint;
            float[] currentPoint;

            while (this.processList.Count > 0)
            {
                if (this.currentPoint == null)
                {
                    this.currentPoint = processList[0];
                    processList.RemoveAt(0);
                }

                currentPoint = this.currentPoint;

                int tries;
                for (tries = 0; tries < this.maxTries; tries++)
                {
                    var inShape = true;
                    var distance = this.minDistance + this.deltaDistance * this.rng();

                    if (this.dimension == 2)
                    {
                        var angle = this.rng() * Math.PI * 2;
                        newPoint = new float[] {
                            (float)Math.Cos(angle),
                            (float)Math.Sin(angle)
                        };
                    }
                    else
                    {
                        newPoint = sphereRandom(this.dimension, this.rng);
                    }

                    for (int i = 0; inShape && i < this.dimension; i++)
                    {
                        newPoint[i] = currentPoint[i] + newPoint[i] * distance;
                        inShape = (newPoint[i] >= 0 && newPoint[i] <= this.shape[i] - 1);
                    }

                    if (inShape && !this.inNeighbourhood(newPoint))
                    {
                        return this.directAddPoint(newPoint);
                    }
                }

                if (tries == this.maxTries)
                {
                    this.currentPoint = null;
                }
            }

            return null;
        }

        /**
         * Automatically fill the grid, adding a random point to start the process if needed.
         * Will block the thread, probably best to use it in a web worker or child process.
         * @returns {Array[]} Sample points
         */
        public List<float[]> fill()
        {
            if (this.samplePoints.Count == 0)
            {
                this.addRandomPoint();
            }
            while (this.next() != null) { }
            return this.samplePoints;
        }

        /**
         * Get all the points in the grid.
         * @returns {Array[]} Sample points
         */
        public List<float[]> getAllPoints()
        {
            return this.samplePoints;
        }

        /**
         * Reinitialize the grid as well as the internal state
         */
        public void reset()
        {
            var gridData = this.gridData;
            int i = 0;

            // reset the cache grid
            for (i = 0; i < gridData.Length; i++)
            {
                gridData[i] = 0;
            }

            // new array for the samplePoints as it is passed by reference to the outside
            this.samplePoints = new List<float[]>();

            // reset the internal state
            this.currentPoint = null;
            this.processList.Clear();
        }
    }
}
