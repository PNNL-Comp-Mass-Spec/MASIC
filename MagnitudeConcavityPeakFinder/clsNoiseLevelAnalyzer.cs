using System;
using System.Collections.Generic;

namespace MagnitudeConcavityPeakFinder
{
    public class NoiseLevelAnalyzer
    {
        #region Structures and Enums

        public enum eNoiseThresholdModes
        {
            AbsoluteThreshold = 0,
            TrimmedMeanByAbundance = 1,
            TrimmedMeanByCount = 2,
            TrimmedMedianByAbundance = 3,
            DualTrimmedMeanByAbundance = 4,
            MeanOfDataInPeakVicinity = 5
        }

        public struct udtBaselineNoiseOptionsType
        {
            // Method to use to determine the baseline noise level
            public eNoiseThresholdModes BaselineNoiseMode;
            // Explicitly defined noise intensity; only used if .BaselineNoiseMode = eNoiseThresholdModes.AbsoluteThreshold; 50000 for SIC, 0 for MS/MS spectra
            public float BaselineNoiseLevelAbsolute;
            // Typically 2 or 3 for spectra; 0 for SICs
            public float MinimumSignalToNoiseRatio;
            // If the noise threshold computed is less than this value, then will use this value to compute S/N; additionally, this is used as the minimum intensity threshold when computing a trimmed noise level
            public float MinimumBaselineNoiseLevel;
            // Typically 0.75 for SICs, 0.5 for MS/MS spectra; only used for eNoiseThresholdModes.TrimmedMeanByAbundance, .TrimmedMeanByCount, .TrimmedMedianByAbundance
            public float TrimmedMeanFractionLowIntensityDataToAverage;
            // Typically 5; distance from the mean in standard deviation units (SqrRt(Variance)) to discard data for computing the trimmed mean
            public short DualTrimmedMeanStdDevLimits;
            // Typically 3; set to 1 to disable segmentation
            public short DualTrimmedMeanMaximumSegments;
        }

        public struct udtBaselineNoiseStatsType
        {
            // Typically the average of the data being sampled to determine the baseline noise estimate
            public double NoiseLevel;
            // Standard Deviation of the data used to compute the baseline estimate
            public double NoiseStDev;
            public int PointsUsed;
            public eNoiseThresholdModes NoiseThresholdModeUsed;
        }

        #endregion

        public bool ComputeTrimmedNoiseLevel(
            double[] intensityData,
            int indexStart,
            int indexEnd,
            udtBaselineNoiseOptionsType udtBaselineNoiseOptions,
            bool ignoreNonPositiveData,
            out udtBaselineNoiseStatsType udtBaselineNoiseStats)
        {
            // Computes a trimmed mean or trimmed median using the low intensity data up to udtBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage
            // Additionally, computes a full median using all data in sngData
            // If blnIgnoreNonPositiveData is True, then removes data from sngData() <= 0 and <= .MinimumBaselineNoiseLevel
            // Returns True if success, False if error (or no data in sngData)

            // Note: Replaces values of 0 with the minimum positive value in sngData()
            // Note: You cannot use sngData.Length to determine the length of the array; use intDataCount


            double summedIntensity;

            int countSummed;

            // Initialize udtBaselineNoiseStats
            udtBaselineNoiseStats = GetBaselineNoiseStats(udtBaselineNoiseOptions.MinimumBaselineNoiseLevel,
                                                          udtBaselineNoiseOptions.BaselineNoiseMode);

            if (intensityData == null || indexEnd - indexStart < 0)
            {
                return false;
            }

            // Copy the data into sortedData
            var dataSortedCount = indexEnd - indexStart + 1;
            var sortedData = new double[dataSortedCount];

            if (indexStart == 0)
            {
                Array.Copy(intensityData, sortedData, dataSortedCount);
            }
            else
            {
                for (var intIndex = indexStart; intIndex <= indexEnd; intIndex++)
                {
                    sortedData[intIndex - indexStart] = intensityData[intIndex];
                }
            }

            // Sort the array
            Array.Sort(sortedData);

            if (ignoreNonPositiveData && sortedData[0] <= 0)
            {
                // Remove data with a value <= 0

                var validDataCount = 0;
                for (var intIndex = 0; intIndex <= dataSortedCount - 1; intIndex++)
                {
                    if (sortedData[intIndex] > 0)
                    {
                        // Copy in place
                        sortedData[validDataCount] = sortedData[intIndex];
                        validDataCount += 1;
                    }
                }

                if (validDataCount < dataSortedCount)
                {
                    dataSortedCount = validDataCount;
                }

                // Check for no data remaining
                if (dataSortedCount <= 0)
                {
                    return false;
                }

            }

            // Look for the minimum positive value and replace all data in sortedData with that value
            var minimumPositiveValue = ReplaceSortedDataWithMinimumPositiveValue(dataSortedCount, sortedData);

            switch (udtBaselineNoiseOptions.BaselineNoiseMode)
            {
                case eNoiseThresholdModes.TrimmedMeanByAbundance:
                case eNoiseThresholdModes.TrimmedMeanByCount:

                    if (udtBaselineNoiseOptions.BaselineNoiseMode == eNoiseThresholdModes.TrimmedMeanByAbundance)
                    {
                        // TrimmedMeanByAbundance
                        countSummed = ComputeTrimmedMeanByAbundance(sortedData, dataSortedCount, udtBaselineNoiseOptions,
                                                                    out indexEnd, out summedIntensity);
                    }
                    else
                    {
                        // TrimmedMeanByCount
                        countSummed = ComputeTrimmedMeanByCount(sortedData, dataSortedCount, udtBaselineNoiseOptions,
                                                                out indexEnd, out summedIntensity);
                    }

                    if (countSummed == 0)
                    {
                        // No data to average; define the noise level to be the minimum intensity

                        udtBaselineNoiseStats.NoiseLevel = sortedData[0];
                        udtBaselineNoiseStats.NoiseStDev = 0;
                        udtBaselineNoiseStats.PointsUsed = 1;
                        break;
                    }


                    // Compute the average
                    // Note that countSummed will be used below in the variance computation

                    udtBaselineNoiseStats.NoiseLevel = summedIntensity / countSummed;
                    udtBaselineNoiseStats.PointsUsed = countSummed;

                    if (countSummed <= 1)
                    {
                        udtBaselineNoiseStats.NoiseStDev = 0;
                        break;
                    }

                    // Compute the variance
                    summedIntensity = 0;
                    for (var intIndex = 0; intIndex <= indexEnd; intIndex++)
                    {
                        summedIntensity += Math.Pow((sortedData[intIndex] - udtBaselineNoiseStats.NoiseLevel), 2);
                    }
                    udtBaselineNoiseStats.NoiseStDev = Math.Sqrt(summedIntensity / (countSummed - 1));

                    break;

                case eNoiseThresholdModes.TrimmedMedianByAbundance:
                    if (udtBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage >= 1)
                    {
                        indexEnd = dataSortedCount - 1;
                    }
                    else
                    {
                        //Find the median of the data that has intensity values less than
                        //  Minimum + udtBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (Maximum - Minimum)

                        var dblIntensityThreshold = sortedData[0] +
                                                       udtBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage *
                                                       (sortedData[dataSortedCount - 1] -
                                                        sortedData[0]);

                        // Find the first point with an intensity value <= dblIntensityThreshold
                        indexEnd = dataSortedCount - 1;
                        for (var intIndex = 1; intIndex <= dataSortedCount - 1; intIndex++)
                        {
                            if (sortedData[intIndex] > dblIntensityThreshold)
                            {
                                indexEnd = intIndex - 1;
                                break;
                            }
                        }
                    }

                    if (indexEnd % 2 == 0)
                    {
                        // Even value
                        udtBaselineNoiseStats.NoiseLevel = sortedData[indexEnd / 2];
                    }
                    else
                    {
                        // Odd value; average the values on either side of intIndexEnd/2
                        var intIndex = (indexEnd - 1) / 2;
                        if (intIndex < 0)
                            intIndex = 0;
                        summedIntensity = sortedData[intIndex];

                        intIndex += 1;
                        if (intIndex == dataSortedCount)
                            intIndex = dataSortedCount - 1;
                        summedIntensity += sortedData[intIndex];

                        udtBaselineNoiseStats.NoiseLevel = summedIntensity / 2.0;
                    }

                    // Compute the variance
                    summedIntensity = 0;
                    for (var intIndex = 0; intIndex <= indexEnd; intIndex++)
                    {
                        summedIntensity += Math.Pow((sortedData[intIndex] - udtBaselineNoiseStats.NoiseLevel), 2);
                    }


                    countSummed = indexEnd + 1;
                    if (countSummed > 0)
                    {
                        udtBaselineNoiseStats.NoiseStDev = Math.Sqrt(summedIntensity / (countSummed - 1));
                    }
                    else
                    {
                        udtBaselineNoiseStats.NoiseStDev = 0;
                    }
                    udtBaselineNoiseStats.PointsUsed = countSummed;

                    break;
                default:
                    // Unknown mode
                    throw new Exception("Unknown Noise Threshold Mode encountered: " +
                                        udtBaselineNoiseOptions.BaselineNoiseMode);
            }

            // Assure that .NoiseLevel is >= .MinimumBaselineNoiseLevel

            if (udtBaselineNoiseStats.NoiseLevel < udtBaselineNoiseOptions.MinimumBaselineNoiseLevel &&
                udtBaselineNoiseOptions.MinimumBaselineNoiseLevel > 0)
            {
                udtBaselineNoiseStats.NoiseLevel = udtBaselineNoiseOptions.MinimumBaselineNoiseLevel;

                // Set this to 0 since we have overridden .NoiseLevel
                udtBaselineNoiseStats.NoiseStDev = 0;
            }

            return true;

        }

        private static int ComputeTrimmedMeanByCount(
            IList<double> sortedData,
            int dataSortedCount,
            udtBaselineNoiseOptionsType udtBaselineNoiseOptions,
            out int indexEnd,
            out double summedIntensity)
        {

            // Find the index of the data point at intDataSortedCount * udtBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage and
            //  average the data from the start to that index
            indexEnd =
                (int)
                    Math.Round(
                        (dataSortedCount - 1) * udtBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, 0);

            var countSummed = indexEnd + 1;

            summedIntensity = 0;
            for (var intIndex = 0; intIndex <= indexEnd; intIndex++)
            {
                summedIntensity += sortedData[intIndex];
            }

            return countSummed;
        }

        private int ComputeTrimmedMeanByAbundance(
            IList<double> sortedData,
            int dataSortedCount,
            udtBaselineNoiseOptionsType udtBaselineNoiseOptions,
            out int indexEnd,
            out double summedIntensity)
        {
            // Average the data that has intensity values less than
            //  Minimum + udtBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (Maximum - Minimum)

            var dblIntensityThreshold = sortedData[0] +
                                           udtBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage *
                                           (sortedData[dataSortedCount - 1] - sortedData[0]);

            // Initialize countSummed to intDataSortedCount for now, in case all data is within the intensity threshold
            var countSummed = dataSortedCount;

            summedIntensity = 0;
            for (var intIndex = 0; intIndex <= dataSortedCount - 1; intIndex++)
            {
                if (sortedData[intIndex] <= dblIntensityThreshold)
                {
                    summedIntensity += sortedData[intIndex];
                }
                else
                {
                    // Update countSummed
                    countSummed = intIndex;
                    break;
                }
            }

            // Update indexEnd
            indexEnd = countSummed - 1;

            return countSummed;
        }

        public udtBaselineNoiseStatsType GetBaselineNoiseStats(
            float sngMinimumBaselineNoiseLevel,
            eNoiseThresholdModes eNoiseThresholdMode)
        {
            var udtBaselineNoiseStats = new udtBaselineNoiseStatsType
            {
                NoiseLevel = sngMinimumBaselineNoiseLevel,
                NoiseStDev = 0,
                PointsUsed = 0,
                NoiseThresholdModeUsed = eNoiseThresholdMode
            };

            return udtBaselineNoiseStats;
        }

        /// <summary>
        /// Looks for the minimum positive value in sortedData[] then
        /// replaces all values of 0 in sortedData[] with minimumPositiveValue
        /// </summary>
        /// <param name="dataCount"></param>
        /// <param name="sortedData"></param>
        /// <returns>The minimum positive value</returns>
        /// <remarks>Asumes sortedData[] is sorted ascending</remarks>
        private double ReplaceSortedDataWithMinimumPositiveValue(int dataCount, IList<double> sortedData)
        {
            // Find the minimum positive value in sortedData
            // Since it's sorted, we can stop at the first non-zero value

            var indexFirstPositiveValue = -1;
            double minimumPositiveValue = 0;
            for (var intIndex = 0; intIndex <= dataCount - 1; intIndex++)
            {
                if (sortedData[intIndex] > 0)
                {
                    indexFirstPositiveValue = intIndex;
                    minimumPositiveValue = sortedData[intIndex];
                    break;
                }
            }

            if (minimumPositiveValue < 1)
                minimumPositiveValue = 1;

            for (var intIndex = indexFirstPositiveValue; intIndex >= 0; intIndex += -1)
            {
                sortedData[intIndex] = minimumPositiveValue;
            }

            return minimumPositiveValue;

        }

        #region Default Options

        public static udtBaselineNoiseOptionsType GetDefaultNoiseThresholdOptions()
        {
            var udtBaselineNoiseOptions = new udtBaselineNoiseOptionsType
            {
                BaselineNoiseMode = eNoiseThresholdModes.TrimmedMedianByAbundance,
                BaselineNoiseLevelAbsolute = 0,
                MinimumSignalToNoiseRatio = 0,                    // Someday: Figure out how best to use this when > 0; for now, the SICNoiseMinimumSignalToNoiseRatio property ignores any attempts to set this value
                MinimumBaselineNoiseLevel = 1,
                TrimmedMeanFractionLowIntensityDataToAverage = 0.75f,
                DualTrimmedMeanStdDevLimits = 5,
                DualTrimmedMeanMaximumSegments = 3
            };

            return udtBaselineNoiseOptions;
        }

        #endregion

    }
}
