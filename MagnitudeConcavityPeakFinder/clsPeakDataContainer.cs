using System;
using System.Collections.Generic;

namespace MagnitudeConcavityPeakFinder
{
    internal class PeakDataContainer
    {
        private int mOriginalPeakLocationIndex;

        public int DataCount { get; private set; }
        public double[] XData { get; private set; }
        public double[] YData { get; private set; }
        public double[] SmoothedYData { get; private set; }

        /// <summary>
        /// Data point index in scanNumbers that should be a part of the peak
        /// </summary>
        public int OriginalPeakLocationIndex
        {
            get => mOriginalPeakLocationIndex;
            set
            {
                if (value < 0)
                    value = 0;

                mOriginalPeakLocationIndex = value;
            }
        }

        public int PeakWidthPointsMinimum { get; set; }

        public List<clsPeakInfo> Peaks { get; set; }

        public clsPeakInfo BestPeak { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public PeakDataContainer()
        {
            DataCount = 0;
            XData = Array.Empty<double>();
            YData = Array.Empty<double>();
            SmoothedYData = Array.Empty<double>();
        }

        public void SetData(List<KeyValuePair<int, double>> xyData)
        {
            DataCount = xyData.Count;

            XData = new double[xyData.Count];
            YData = new double[xyData.Count];

            for (var i = 0; i < xyData.Count; i++)
            {
                XData[i] = xyData[i].Key;
                YData[i] = xyData[i].Value;
            }
        }

        public void SetData(int[] xData, double[] yData, int dataCount)
        {
            dataCount = ValidateDataCount(xData.Length, yData.Length, dataCount);
            DataCount = dataCount;

            XData = new double[dataCount];
            YData = new double[dataCount];

            for (var i = 0; i < dataCount; i++)
            {
                XData[i] = xData[i];
                YData[i] = yData[i];
            }
        }

        public void SetData(double[] xData, double[] yData, int dataCount)
        {
            dataCount = ValidateDataCount(xData.Length, yData.Length, dataCount);
            DataCount = dataCount;

            XData = new double[dataCount];
            YData = new double[dataCount];

            Array.Copy(xData, XData, dataCount);
            Array.Copy(yData, YData, dataCount);
        }

        private int ValidateDataCount(int xDataCount, int yDataCount, int dataCount)
        {
            if (dataCount > xDataCount)
                dataCount = xDataCount;

            if (dataCount > yDataCount)
            {
                throw new Exception(
                    "xData array contains more data points than the yData array; unable to store in PeakDataContainer");
            }

            return dataCount;
        }

        public void SetSmoothedData(double[] newSmoothedData)
        {
            SmoothedYData = newSmoothedData;
        }
    }
}
