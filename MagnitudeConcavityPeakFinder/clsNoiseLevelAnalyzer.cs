using System;
using System.Collections.Generic;

namespace MagnitudeConcavityPeakFinder
{
    public class NoiseLevelAnalyzer
    {
        public enum NoiseThresholdModes
        {
            AbsoluteThreshold = 0,
            TrimmedMeanByAbundance = 1,
            TrimmedMeanByCount = 2,
            TrimmedMedianByAbundance = 3,
            DualTrimmedMeanByAbundance = 4,
            MeanOfDataInPeakVicinity = 5
        }

        public struct BaselineNoiseOptionsType
        {
            /// <summary>
            /// Method to use to determine the baseline noise level
            /// </summary>
            public NoiseThresholdModes BaselineNoiseMode;

            /// <summary>
            /// Explicitly defined noise intensity; only used if .BaselineNoiseMode = NoiseThresholdModes.AbsoluteThreshold; 50000 for SIC, 0 for MS/MS spectra
            /// </summary>
            public float BaselineNoiseLevelAbsolute;

            /// <summary>
            /// Typically 2 or 3 for spectra; 0 for SICs
            /// </summary>
            public float MinimumSignalToNoiseRatio;

            /// <summary>
            /// If the noise threshold computed is less than this value, will use this value to compute S/N; additionally, this is used as the minimum intensity threshold when computing a trimmed noise level
            /// </summary>
            public float MinimumBaselineNoiseLevel;

            /// <summary>
            /// Typically 0.75 for SICs, 0.5 for MS/MS spectra; only used for NoiseThresholdModes.TrimmedMeanByAbundance, .TrimmedMeanByCount, .TrimmedMedianByAbundance
            /// </summary>
            public float TrimmedMeanFractionLowIntensityDataToAverage;

            /// <summary>
            /// Typically 5; distance from the mean in standard deviation units (SquareRoot(Variance)) to discard data for computing the trimmed mean
            /// </summary>
            public short DualTrimmedMeanStdDevLimits;

            /// <summary>
            /// Typically 3; set to 1 to disable segmentation
            /// </summary>
            public short DualTrimmedMeanMaximumSegments;
        }

        public struct BaselineNoiseStatsType
        {
            // Typically the average of the data being sampled to determine the baseline noise estimate
            public double NoiseLevel;
            // Standard Deviation of the data used to compute the baseline estimate
            public double NoiseStDev;
            public int PointsUsed;
            public NoiseThresholdModes NoiseThresholdModeUsed;
        }

        /// <summary>
        /// Computes a trimmed mean or trimmed median using the low intensity data up to baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage
        /// Additionally, computes a full median using all data in intensityData
        /// If ignoreNonPositiveData is True, removes data from intensityData if less than zero and/or less than baselineNoiseOptions.MinimumBaselineNoiseLevel
        /// Returns True if success, False if error (or no data in intensityData)
        /// </summary>
        /// <param name="intensityData"></param>
        /// <param name="indexStart"></param>
        /// <param name="indexEnd"></param>
        /// <param name="baselineNoiseOptions"></param>
        /// <param name="ignoreNonPositiveData"></param>
        /// <param name="baselineNoiseStats"></param>
        /// <returns>True if success, false if an error (or no data in intensityData)</returns>
        /// <remarks>Replaces values of 0 with the minimum positive value in intensityData()</remarks>
        public bool ComputeTrimmedNoiseLevel(
            double[] intensityData,
            int indexStart,
            int indexEnd,
            BaselineNoiseOptionsType baselineNoiseOptions,
            bool ignoreNonPositiveData,
            out BaselineNoiseStatsType baselineNoiseStats)
        {
            double summedIntensity;

            int countSummed;

            // Initialize baselineNoiseStats
            baselineNoiseStats = GetBaselineNoiseStats(baselineNoiseOptions.MinimumBaselineNoiseLevel, baselineNoiseOptions.BaselineNoiseMode);

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
                for (var intIndex = 0; intIndex < dataSortedCount; intIndex++)
                {
                    if (sortedData[intIndex] > 0)
                    {
                        // Copy in place
                        sortedData[validDataCount] = sortedData[intIndex];
                        validDataCount++;
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

            ReplaceSortedDataWithMinimumPositiveValue(dataSortedCount, sortedData);

            switch (baselineNoiseOptions.BaselineNoiseMode)
            {
                case NoiseThresholdModes.TrimmedMeanByAbundance:
                case NoiseThresholdModes.TrimmedMeanByCount:

                    if (baselineNoiseOptions.BaselineNoiseMode == NoiseThresholdModes.TrimmedMeanByAbundance)
                    {
                        // TrimmedMeanByAbundance
                        countSummed = ComputeTrimmedMeanByAbundance(
                            sortedData, dataSortedCount, baselineNoiseOptions,
                            out indexEnd, out summedIntensity);
                    }
                    else
                    {
                        // TrimmedMeanByCount
                        countSummed = ComputeTrimmedMeanByCount(
                            sortedData, dataSortedCount, baselineNoiseOptions,
                            out indexEnd, out summedIntensity);
                    }

                    if (countSummed == 0)
                    {
                        // No data to average; define the noise level to be the minimum intensity

                        baselineNoiseStats.NoiseLevel = sortedData[0];
                        baselineNoiseStats.NoiseStDev = 0;
                        baselineNoiseStats.PointsUsed = 1;
                        break;
                    }

                    // Compute the average
                    // Note that countSummed will be used below in the variance computation

                    baselineNoiseStats.NoiseLevel = summedIntensity / countSummed;
                    baselineNoiseStats.PointsUsed = countSummed;

                    if (countSummed <= 1)
                    {
                        baselineNoiseStats.NoiseStDev = 0;
                        break;
                    }

                    // Compute the variance
                    summedIntensity = 0;
                    for (var intIndex = 0; intIndex <= indexEnd; intIndex++)
                    {
                        summedIntensity += Math.Pow(sortedData[intIndex] - baselineNoiseStats.NoiseLevel, 2);
                    }
                    baselineNoiseStats.NoiseStDev = Math.Sqrt(summedIntensity / (countSummed - 1));

                    break;

                case NoiseThresholdModes.TrimmedMedianByAbundance:
                    if (baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage >= 1)
                    {
                        indexEnd = dataSortedCount - 1;
                    }
                    else
                    {
                        //Find the median of the data that has intensity values less than
                        //  Minimum + baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (Maximum - Minimum)

                        var dblIntensityThreshold =
                            sortedData[0] +
                            baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage *
                            (sortedData[dataSortedCount - 1] -
                            sortedData[0]);

                        // Find the first point with an intensity value <= dblIntensityThreshold
                        indexEnd = dataSortedCount - 1;
                        for (var intIndex = 1; intIndex < dataSortedCount; intIndex++)
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
                        baselineNoiseStats.NoiseLevel = sortedData[indexEnd / 2];
                    }
                    else
                    {
                        // Odd value; average the values on either side of intIndexEnd/2
                        var intIndex = (indexEnd - 1) / 2;
                        if (intIndex < 0)
                            intIndex = 0;
                        summedIntensity = sortedData[intIndex];

                        intIndex++;
                        if (intIndex == dataSortedCount)
                            intIndex = dataSortedCount - 1;
                        summedIntensity += sortedData[intIndex];

                        baselineNoiseStats.NoiseLevel = summedIntensity / 2.0;
                    }

                    // Compute the variance
                    summedIntensity = 0;
                    for (var intIndex = 0; intIndex <= indexEnd; intIndex++)
                    {
                        summedIntensity += Math.Pow(sortedData[intIndex] - baselineNoiseStats.NoiseLevel, 2);
                    }

                    countSummed = indexEnd + 1;
                    if (countSummed > 0)
                    {
                        baselineNoiseStats.NoiseStDev = Math.Sqrt(summedIntensity / (countSummed - 1));
                    }
                    else
                    {
                        baselineNoiseStats.NoiseStDev = 0;
                    }
                    baselineNoiseStats.PointsUsed = countSummed;

                    break;
                default:
                    // Unknown mode
                    throw new Exception("Unknown Noise Threshold Mode encountered: " + baselineNoiseOptions.BaselineNoiseMode);
            }

            // Assure that .NoiseLevel is >= .MinimumBaselineNoiseLevel

            if (baselineNoiseStats.NoiseLevel < baselineNoiseOptions.MinimumBaselineNoiseLevel &&
                baselineNoiseOptions.MinimumBaselineNoiseLevel > 0)
            {
                baselineNoiseStats.NoiseLevel = baselineNoiseOptions.MinimumBaselineNoiseLevel;

                // Set this to 0 since we have overridden .NoiseLevel
                baselineNoiseStats.NoiseStDev = 0;
            }

            return true;
        }

        private static int ComputeTrimmedMeanByCount(
            IList<double> sortedData,
            int dataSortedCount,
            BaselineNoiseOptionsType baselineNoiseOptions,
            out int indexEnd,
            out double summedIntensity)
        {
            // Find the index of the data point at intDataSortedCount * baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage and
            //  average the data from the start to that index
            indexEnd = (int)Math.Round((dataSortedCount - 1) * baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, 0);

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
            BaselineNoiseOptionsType baselineNoiseOptions,
            out int indexEnd,
            out double summedIntensity)
        {
            // Average the data that has intensity values less than
            //  Minimum + baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (Maximum - Minimum)

            var dblIntensityThreshold =
                sortedData[0] +
                baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage *
                (sortedData[dataSortedCount - 1] - sortedData[0]);

            // Initialize countSummed to intDataSortedCount for now, in case all data is within the intensity threshold
            var countSummed = dataSortedCount;

            summedIntensity = 0;
            for (var intIndex = 0; intIndex < dataSortedCount; intIndex++)
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

        public BaselineNoiseStatsType GetBaselineNoiseStats(
            float sngMinimumBaselineNoiseLevel,
            NoiseThresholdModes noiseThresholdMode)
        {
            return new BaselineNoiseStatsType
            {
                NoiseLevel = sngMinimumBaselineNoiseLevel,
                NoiseStDev = 0,
                PointsUsed = 0,
                NoiseThresholdModeUsed = noiseThresholdMode
            };
        }

        /// <summary>
        /// Looks for the minimum positive value in sortedData[] then
        /// replaces all values of 0 in sortedData[] with minimumPositiveValue
        /// </summary>
        /// <param name="dataCount"></param>
        /// <param name="sortedData"></param>
        /// <remarks>Assumes sortedData[] is sorted ascending</remarks>
        /// <returns>The minimum positive value</returns>
        private double ReplaceSortedDataWithMinimumPositiveValue(int dataCount, IList<double> sortedData)
        {
            // Find the minimum positive value in sortedData
            // Since it's sorted, we can stop at the first non-zero value

            var indexFirstPositiveValue = -1;
            double minimumPositiveValue = 0;
            for (var intIndex = 0; intIndex < dataCount; intIndex++)
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

        public static BaselineNoiseOptionsType GetDefaultNoiseThresholdOptions()
        {
            return new BaselineNoiseOptionsType
            {
                BaselineNoiseMode = NoiseThresholdModes.TrimmedMedianByAbundance,
                BaselineNoiseLevelAbsolute = 0,
                MinimumSignalToNoiseRatio = 0,                    // Someday: Figure out how best to use this when > 0; for now, the SICNoiseMinimumSignalToNoiseRatio property ignores any attempts to set this value
                MinimumBaselineNoiseLevel = 1,
                TrimmedMeanFractionLowIntensityDataToAverage = 0.75f,
                DualTrimmedMeanStdDevLimits = 5,
                DualTrimmedMeanMaximumSegments = 3
            };
        }
    }
}
