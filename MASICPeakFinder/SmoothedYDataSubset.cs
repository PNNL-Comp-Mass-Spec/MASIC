using System;
using System.Collections.Generic;

namespace MASICPeakFinder
{
    /// <summary>
    /// Container for tracking a subset of smoothed intensity data
    /// </summary>
    public class SmoothedYDataSubset
    {
        /// <summary>
        /// Data count
        /// </summary>
        public int DataCount { get; }

        /// <summary>
        /// Intensity values
        /// </summary>
        public double[] Data { get; }

        /// <summary>
        /// Index in the original data array where this subset of intensity data starts
        /// </summary>
        public int DataStartIndex { get; }

        /// <summary>
        /// Parameterless constructor
        /// </summary>
        public SmoothedYDataSubset()
        {
            DataCount = 0;
            DataStartIndex = 0;
            Data = new double[1];
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="yData"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        public SmoothedYDataSubset(IList<double> yData, int startIndex, int endIndex)
        {
            if (yData == null || endIndex < startIndex || startIndex < 0)
            {
                DataCount = 0;
                DataStartIndex = 0;
                Data = new double[1];
                return;
            }

            DataStartIndex = startIndex;

            DataCount = endIndex - startIndex + 1;
            Data = new double[DataCount + 1];

            for (var intIndex = startIndex; intIndex <= endIndex; intIndex++)
            {
                Data[intIndex - startIndex] = Math.Min(yData[intIndex], double.MaxValue);
            }
        }
    }
}