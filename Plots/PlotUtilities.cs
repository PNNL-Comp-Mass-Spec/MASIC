using System;
using System.Collections.Generic;
using System.Linq;

namespace MASIC.Plots
{
    internal static class PlotUtilities
    {
        public static void GetAxisFormatInfo(
            IList<double> dataPoints,
            bool integerData,
            AxisInfo axisInfo)
        {
            var absoluteValueMin = dataPoints.Count == 0 ? 0 : Math.Abs(dataPoints[0]);
            var absoluteValueMax = absoluteValueMin;

            foreach (var currentValAbs in from value in dataPoints select Math.Abs(value))
            {
                absoluteValueMin = Math.Min(absoluteValueMin, currentValAbs);
                absoluteValueMax = Math.Max(absoluteValueMax, currentValAbs);
            }

            GetAxisFormatInfo(absoluteValueMin, absoluteValueMax, integerData, axisInfo);
        }

        public static void GetAxisFormatInfo(
            double absoluteValueMin,
            double absoluteValueMax,
            bool integerData,
            AxisInfo axisInfo)
        {
            if (Math.Abs(absoluteValueMin) < float.Epsilon && Math.Abs(absoluteValueMax) < float.Epsilon)
            {
                axisInfo.StringFormat = "0";
                axisInfo.MinorGridLineThickness = 0;
                axisInfo.MajorStep = 1;
                return;
            }

            if (integerData)
            {
                if (absoluteValueMax >= 1000000)
                {
                    axisInfo.StringFormat = AxisInfo.EXPONENTIAL_FORMAT;
                }
                return;
            }

            var minDigitsPrecision = 0;

            if (absoluteValueMax < 0.02)
            {
                axisInfo.StringFormat = AxisInfo.EXPONENTIAL_FORMAT;
            }
            else if (absoluteValueMax < 0.2)
            {
                minDigitsPrecision = 2;
                axisInfo.StringFormat = "0.00";
            }
            else if (absoluteValueMax < 2)
            {
                minDigitsPrecision = 1;
                axisInfo.StringFormat = "0.0";
            }
            else if (absoluteValueMax >= 1000000)
            {
                axisInfo.StringFormat = AxisInfo.EXPONENTIAL_FORMAT;
            }

            if (absoluteValueMax - absoluteValueMin < 1E-05)
            {
                if (!axisInfo.StringFormat.Contains("."))
                {
                    axisInfo.StringFormat = "0.00";
                }
            }
            else
            {
                // Examine the range of values between the minimum and the maximum
                // If the range is small, e.g. between 3.95 and 3.98, then we need to guarantee that we have at least 2 digits of precision
                // The following combination of Log10 and ceiling determines the minimum needed
                var minDigitsRangeBased = (int)Math.Ceiling(-Math.Log10(absoluteValueMax - absoluteValueMin));

                if (minDigitsRangeBased > minDigitsPrecision)
                {
                    minDigitsPrecision = minDigitsRangeBased;
                }

                if (minDigitsPrecision > 0)
                {
                    axisInfo.StringFormat = "0." + new string('0', minDigitsPrecision);
                }
            }
        }

        public static double GetNumberOrDefault(double value, double defaultValue)
        {
            if (double.IsNaN(value))
                return defaultValue;

            return value;
        }
    }
}
