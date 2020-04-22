using System;
using System.Collections.Generic;

namespace MASICPeakFinder
{
    public class clsSmoothedYDataSubset
    {
        public int DataCount { get; }
        public double[] Data { get; }
        public int DataStartIndex { get; }

        public clsSmoothedYDataSubset()
        {
            DataCount = 0;
            DataStartIndex = 0;
            Data = new double[1];
        }

        public clsSmoothedYDataSubset(IList<double> yData, int startIndex, int endIndex)
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
                Data[intIndex - startIndex] = Math.Min(yData[intIndex], double.MaxValue);
        }
    }
}