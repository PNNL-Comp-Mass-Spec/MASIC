using System;
using System.Collections.Generic;

namespace MagnitudeConcavityPeakFinder
{
    class PeakDataContainer
    {

        private int mDataCount;
        private double[] mXData;
        private double[] mYData;
        private double[] mSmoothedYData;

        private int mOriginalPeakLocationIndex;

        public int DataCount { get { return mDataCount; } }
        public double[] XData { get { return mXData; } }
        public double[] YData { get { return mYData; } }
        public double[] SmoothedYData { get { return mSmoothedYData; } }

        /// <summary>
        /// Data point index in scanNumbers that should be a part of the peak
        /// </summary>
        public int OriginalPeakLocationIndex 
        {
            get 
            { 
                return mOriginalPeakLocationIndex; 
            }
            set 
            { 
                if (value < 0) 
                    value = 0;

                mOriginalPeakLocationIndex = value;
            } 
        }


        public int PeakWidthPointsMinimum { get; set; }

        public List<clsPeak> Peaks { get; set; }

        public clsPeak BestPeak { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public PeakDataContainer()
        {
            mDataCount = 0;
            mXData = new double[0];
            mYData = new double[0];
            mSmoothedYData = new double[0];

        }

        public void SetData(List<KeyValuePair<int, double>> xyData)
        {
            mDataCount = xyData.Count;

            mXData = new double[xyData.Count];
            mYData = new double[xyData.Count];

            for (int i = 0; i < xyData.Count; i++)
            {
                mXData[i] = xyData[i].Key;
                mYData[i] = xyData[i].Value;
            }
            
        }

        public void SetData(int[] xData, double[] yData, int dataCount)
        {
            dataCount = ValidateDataCount(xData.Length, yData.Length, dataCount);
            mDataCount = dataCount;

            mXData = new double[dataCount];
            mYData = new double[dataCount];

            for (int i = 0; i < dataCount; i++)
            {
                mXData[i] = xData[i];
                mYData[i] = yData[i];
            }
        }

        public void SetData(double[] xData, double[] yData, int dataCount)
        {
            dataCount = ValidateDataCount(xData.Length, yData.Length, dataCount);
            mDataCount = dataCount;

            mXData = new double[dataCount];
            mYData = new double[dataCount];

            Array.Copy(xData, mXData, dataCount);
            Array.Copy(yData, mYData, dataCount);
          
        }

        private int ValidateDataCount(int xDataCount, int yDataCount, int dataCount)
        {
            if (dataCount > xDataCount)
                dataCount = xDataCount;

            if (dataCount > yDataCount)
                throw new Exception(
                    "xData array contains more data points than the yData array; unable to store in PeakDataContainer");

            return dataCount;
        }

        public void SetSmoothedData(double[] newSmoothdData)
        {
            mSmoothedYData = newSmoothdData;
        }
    }
}
