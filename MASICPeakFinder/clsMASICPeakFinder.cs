﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MASICPeakFinder
{
    // -------------------------------------------------------------------------------
    // Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
    // Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

    // E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
    // Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
    // -------------------------------------------------------------------------------
    //
    // Licensed under the 2-Clause BSD License; you may not use this file except
    // in compliance with the License.  You may obtain a copy of the License at
    // https://opensource.org/licenses/BSD-2-Clause

    public class clsMASICPeakFinder : PRISM.EventNotifier
    {
        #region "Constants and Enums"
        public string PROGRAM_DATE = "May 23, 2019";

        public const int MINIMUM_PEAK_WIDTH = 3;                         // Width in points

        public enum eNoiseThresholdModes
        {
            AbsoluteThreshold = 0,
            TrimmedMeanByAbundance = 1,
            TrimmedMeanByCount = 2,
            TrimmedMedianByAbundance = 3,
            DualTrimmedMeanByAbundance = 4,
            MeanOfDataInPeakVicinity = 5
        }

        public enum eTTestConfidenceLevelConstants
        {
            // ReSharper disable UnusedMember.Global
            Conf80Pct = 0,
            Conf90Pct = 1,
            Conf95Pct = 2,
            Conf98Pct = 3,
            Conf99Pct = 4,
            Conf99_5Pct = 5,
            Conf99_8Pct = 6,
            Conf99_9Pct = 7
            // ReSharper restore UnusedMember.Global
        }
        #endregion

        #region "Classwide Variables"
        private string mStatusMessage;

        /// <summary>
        /// TTest Significance Table.
        /// Confidence Levels and critical values:
        /// 80%, 90%, 95%, 98%, 99%, 99.5%, 99.8%, 99.9%
        /// 1.886, 2.920, 4.303, 6.965, 9.925, 14.089, 22.327, 31.598
        /// </summary>
        private readonly double[] TTestConfidenceLevels = new double[] { 1.886, 2.92, 4.303, 6.965, 9.925, 14.089, 22.327, 31.598 };

        #endregion

        #region "Properties"
        // ReSharper disable once UnusedMember.Global
        public string ProgramDate => PROGRAM_DATE;

        public string ProgramVersion => GetVersionForExecutingAssembly();

        // ReSharper disable once UnusedMember.Global
        public string StatusMessage => mStatusMessage;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public clsMASICPeakFinder()
        {
            mStatusMessage = string.Empty;
        }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// Compute an updated peak area by adjusting for the baseline
        /// </summary>
        /// <param name="sicPeak"></param>
        /// <param name="sicPeakWidthFullScans"></param>
        /// <param name="allowNegativeValues"></param>
        /// <returns>Adjusted peak area</returns>
        /// <remarks>This method is used by MASIC Browser</remarks>
        public static double BaselineAdjustArea(
            clsSICStatsPeak sicPeak, int sicPeakWidthFullScans, bool allowNegativeValues)
        {
            // Note, compute sicPeakWidthFullScans using:
            // Width = sicScanNumbers(.Peak.IndexBaseRight) - sicScanNumbers(.Peak.IndexBaseLeft) + 1

            return BaselineAdjustArea(sicPeak.Area, sicPeak.BaselineNoiseStats.NoiseLevel, sicPeak.FWHMScanWidth, sicPeakWidthFullScans, allowNegativeValues);
        }

        public static double BaselineAdjustArea(
            double peakArea,
            double baselineNoiseLevel,
            int sicPeakFWHMScans,
            int sicPeakWidthFullScans,
            bool allowNegativeValues)
        {
            int widthToSubtract = ComputeWidthAtBaseUsingFWHM(sicPeakFWHMScans, sicPeakWidthFullScans, 4);

            double correctedArea = peakArea - baselineNoiseLevel * widthToSubtract;
            if (allowNegativeValues || correctedArea > 0)
            {
                return correctedArea;
            }
            else
            {
                return 0;
            }
        }

        // ReSharper disable once UnusedMember.Global
        public static double BaselineAdjustIntensity(clsSICStatsPeak sicPeak, bool allowNegativeValues)
        {
            return BaselineAdjustIntensity(sicPeak.MaxIntensityValue, sicPeak.BaselineNoiseStats.NoiseLevel, allowNegativeValues);
        }

        public static double BaselineAdjustIntensity(
            double rawIntensity,
            double baselineNoiseLevel,
            bool allowNegativeValues)
        {
            if (allowNegativeValues || rawIntensity > baselineNoiseLevel)
            {
                return rawIntensity - baselineNoiseLevel;
            }
            else
            {
                return 0;
            }
        }

        private bool ComputeAverageNoiseLevelCheckCounts(
            int validDataCountA, int validDataCountB,
            double sumA, double sumB,
            int minimumCount,
            clsBaselineNoiseStats baselineNoiseStats)
        {
            var useLeftData = default(bool);
            var useRightData = default(bool);
            if (minimumCount < 1)
                minimumCount = 1;
            bool useBothSides = false;

            if (validDataCountA >= minimumCount || validDataCountB >= minimumCount)
            {
                if (validDataCountA >= minimumCount && validDataCountB >= minimumCount)
                {
                    // Both meet the minimum count criterion
                    // Return an overall average
                    useBothSides = true;
                }
                else if (validDataCountA >= minimumCount)
                {
                    useLeftData = true;
                }
                else
                {
                    useRightData = true;
                }

                if (useBothSides)
                {
                    baselineNoiseStats.NoiseLevel = (sumA + sumB) / (validDataCountA + validDataCountB);
                    baselineNoiseStats.NoiseStDev = 0;      // We'll compute noise StDev outside this function
                    baselineNoiseStats.PointsUsed = validDataCountA + validDataCountB;
                }
                else if (useLeftData)
                {
                    // Use left data only
                    baselineNoiseStats.NoiseLevel = sumA / validDataCountA;
                    baselineNoiseStats.NoiseStDev = 0;
                    baselineNoiseStats.PointsUsed = validDataCountA;
                }
                else if (useRightData)
                {
                    // Use right data only
                    baselineNoiseStats.NoiseLevel = sumB / validDataCountB;
                    baselineNoiseStats.NoiseStDev = 0;
                    baselineNoiseStats.PointsUsed = validDataCountB;
                }
                else
                {
                    throw new Exception("Logic error; This code should not be reached");
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ComputeAverageNoiseLevelExcludingRegion(
            IList<clsSICDataPoint> sicData,
            int indexStart, int indexEnd,
            int exclusionIndexStart, int exclusionIndexEnd,
            clsBaselineNoiseOptions baselineNoiseOptions,
            clsBaselineNoiseStats baselineNoiseStats)
        {
            // Compute the average intensity level between indexStart and exclusionIndexStart
            // Also compute the average between exclusionIndexEnd and indexEnd
            // Use ComputeAverageNoiseLevelCheckCounts to determine whether both averages are used to determine
            // the baseline noise level or whether just one of the averages is used

            bool success = false;

            // Examine the exclusion range.  If the exclusion range excludes all
            // data to the left or right of the peak, then use a few data points anyway, even if this does include some of the peak
            if (exclusionIndexStart < indexStart + MINIMUM_PEAK_WIDTH)
            {
                exclusionIndexStart = indexStart + MINIMUM_PEAK_WIDTH;
                if (exclusionIndexStart >= indexEnd)
                {
                    exclusionIndexStart = indexEnd - 1;
                }
            }

            if (exclusionIndexEnd > indexEnd - MINIMUM_PEAK_WIDTH)
            {
                exclusionIndexEnd = indexEnd - MINIMUM_PEAK_WIDTH;
                if (exclusionIndexEnd < 0)
                {
                    exclusionIndexEnd = 0;
                }
            }

            if (exclusionIndexStart >= indexStart && exclusionIndexStart <= indexEnd &&
                exclusionIndexEnd >= exclusionIndexStart && exclusionIndexEnd <= indexEnd)
            {
                double minimumPositiveValue = FindMinimumPositiveValue(sicData, 1);

                int validDataCountA = 0;
                double sumA = 0;
                for (int i = indexStart; i <= exclusionIndexStart; i++)
                {
                    sumA += Math.Max(minimumPositiveValue, sicData[i].Intensity);
                    validDataCountA += 1;
                }

                int validDataCountB = 0;
                double sumB = 0;
                for (int i = exclusionIndexEnd; i <= indexEnd; i++)
                {
                    sumB += Math.Max(minimumPositiveValue, sicData[i].Intensity);
                    validDataCountB += 1;
                }

                success = ComputeAverageNoiseLevelCheckCounts(
                    validDataCountA, validDataCountB,
                    sumA, sumB,
                    MINIMUM_PEAK_WIDTH, baselineNoiseStats);

                // Assure that .NoiseLevel is at least as large as minimumPositiveValue
                if (baselineNoiseStats.NoiseLevel < minimumPositiveValue)
                {
                    baselineNoiseStats.NoiseLevel = minimumPositiveValue;
                }

                // Populate .NoiseStDev
                validDataCountA = 0;
                validDataCountB = 0;
                sumA = 0;
                sumB = 0;
                if (baselineNoiseStats.PointsUsed > 0)
                {
                    for (int i = indexStart; i <= exclusionIndexStart; i++)
                    {
                        sumA += Math.Pow(Math.Max(minimumPositiveValue, sicData[i].Intensity) - baselineNoiseStats.NoiseLevel, 2);
                        validDataCountA += 1;
                    }

                    for (int i = exclusionIndexEnd; i <= indexEnd; i++)
                    {
                        sumB += Math.Pow(Math.Max(minimumPositiveValue, sicData[i].Intensity) - baselineNoiseStats.NoiseLevel, 2);
                        validDataCountB += 1;
                    }
                }

                if (validDataCountA + validDataCountB > 0)
                {
                    baselineNoiseStats.NoiseStDev = Math.Sqrt((sumA + sumB) / (validDataCountA + validDataCountB));
                }
                else
                {
                    baselineNoiseStats.NoiseStDev = 0;
                }
            }

            if (!success)
            {
                var baselineNoiseOptionsOverride = baselineNoiseOptions.Clone();

                baselineNoiseOptionsOverride.BaselineNoiseMode = eNoiseThresholdModes.TrimmedMedianByAbundance;
                baselineNoiseOptionsOverride.TrimmedMeanFractionLowIntensityDataToAverage = 0.33;

                var intensities = (from item in sicData select item.Intensity).ToArray();

                success = ComputeTrimmedNoiseLevel(intensities, indexStart, indexEnd, baselineNoiseOptionsOverride, false, out baselineNoiseStats);
            }

            return success;
        }

        /// <summary>
        /// Divide the data into the number of segments given by baselineNoiseOptions.DualTrimmedMeanMaximumSegments  (use 3 by default)
        /// Call ComputeDualTrimmedNoiseLevel for each segment
        /// Use a TTest to determine whether we need to define a custom noise threshold for each segment
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="indexStart"></param>
        /// <param name="indexEnd"></param>
        /// <param name="baselineNoiseOptions"></param>
        /// <param name="noiseStatsSegments"></param>
        /// <returns>True if success, False if error</returns>
        public bool ComputeDualTrimmedNoiseLevelTTest(
        IReadOnlyList<double> dataList, int indexStart, int indexEnd,
        clsBaselineNoiseOptions baselineNoiseOptions,
        out List<clsBaselineNoiseStatsSegment> noiseStatsSegments)
        {
            noiseStatsSegments = new List<clsBaselineNoiseStatsSegment>();

            try
            {
                int segmentCountLocal = Convert.ToInt32(baselineNoiseOptions.DualTrimmedMeanMaximumSegments);
                if (segmentCountLocal == 0)
                    segmentCountLocal = 3;
                if (segmentCountLocal < 1)
                    segmentCountLocal = 1;

                // Initialize BaselineNoiseStats for each segment now, in case an error occurs
                for (int i = 0; i < segmentCountLocal; i++)
                {
                    var baselineNoiseStats = InitializeBaselineNoiseStats(
                        baselineNoiseOptions.MinimumBaselineNoiseLevel,
                        eNoiseThresholdModes.DualTrimmedMeanByAbundance);
                    noiseStatsSegments.Add(new clsBaselineNoiseStatsSegment(baselineNoiseStats));
                }

                // Determine the segment length
                int segmentLength = Convert.ToInt32(Math.Round((indexEnd - indexStart) / (double)segmentCountLocal, 0));

                // Initialize the first segment
                var firstSegment = noiseStatsSegments.First();
                firstSegment.SegmentIndexStart = indexStart;
                if (segmentCountLocal == 1)
                {
                    firstSegment.SegmentIndexEnd = indexEnd;
                }
                else
                {
                    firstSegment.SegmentIndexEnd = firstSegment.SegmentIndexStart + segmentLength - 1;
                }

                // Initialize the remaining segments
                for (int i = 1; i < segmentCountLocal; i++)
                {
                    noiseStatsSegments[i].SegmentIndexStart = noiseStatsSegments[i - 1].SegmentIndexEnd + 1;
                    if (i == segmentCountLocal - 1)
                    {
                        noiseStatsSegments[i].SegmentIndexEnd = indexEnd;
                    }
                    else
                    {
                        noiseStatsSegments[i].SegmentIndexEnd = noiseStatsSegments[i].SegmentIndexStart + segmentLength - 1;
                    }
                }

                // Call ComputeDualTrimmedNoiseLevel for each segment
                for (int i = 0; i < segmentCountLocal; i++)
                {
                    var current = noiseStatsSegments[i];

                    ComputeDualTrimmedNoiseLevel(dataList, current.SegmentIndexStart, current.SegmentIndexEnd, baselineNoiseOptions, out var baselineNoiseStats);
                    current.BaselineNoiseStats = baselineNoiseStats;
                }

                // Compare adjacent segments using a T-Test, starting with the final segment and working backward
                var confidenceLevel = eTTestConfidenceLevelConstants.Conf90Pct;
                int segmentIndex = segmentCountLocal - 1;

                while (segmentIndex > 0)
                {
                    var previous = noiseStatsSegments[segmentIndex - 1];
                    var current = noiseStatsSegments[segmentIndex];

                    bool significantDifference;
                    double tCalculated;
                    significantDifference = TestSignificanceUsingTTest(
                        current.BaselineNoiseStats.NoiseLevel,
                        previous.BaselineNoiseStats.NoiseLevel,
                        current.BaselineNoiseStats.NoiseStDev,
                        previous.BaselineNoiseStats.NoiseStDev,
                        current.BaselineNoiseStats.PointsUsed,
                        previous.BaselineNoiseStats.PointsUsed,
                        confidenceLevel,
                        out tCalculated);

                    if (significantDifference)
                    {
                        // Significant difference; leave the 2 segments intact
                    }
                    else
                    {
                        // Not a significant difference; recompute the Baseline Noise stats using the two segments combined
                        previous.SegmentIndexEnd = current.SegmentIndexEnd;
                        ComputeDualTrimmedNoiseLevel(dataList,
                                                          previous.SegmentIndexStart,
                                                          previous.SegmentIndexEnd,
                                                          baselineNoiseOptions,
                                                          out var baselineNoiseStats);
                        previous.BaselineNoiseStats = baselineNoiseStats;

                        for (int segmentIndexCopy = segmentIndex; segmentIndexCopy <= segmentCountLocal - 2; segmentIndexCopy++)
                            noiseStatsSegments[segmentIndexCopy] = noiseStatsSegments[segmentIndexCopy + 1];
                        segmentCountLocal -= 1;
                    }

                    segmentIndex -= 1;
                }

                while (noiseStatsSegments.Count > segmentCountLocal)
                    noiseStatsSegments.RemoveAt(noiseStatsSegments.Count - 1);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Computes the average of all of the data in dataList()
        /// Next, discards the data above and below baselineNoiseOptions.DualTrimmedMeanStdDevLimits of the mean
        /// Finally, recomputes the average using the data that remains
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="indexStart"></param>
        /// <param name="indexEnd"></param>
        /// <param name="baselineNoiseOptions"></param>
        /// <param name="baselineNoiseStats"></param>
        /// <returns>True if success, False if error (or no data in dataList)</returns>
        /// <remarks>
        /// Replaces values of 0 with the minimum positive value in dataList()
        /// You cannot use dataList.Length to determine the length of the array; use indexStart and indexEnd to find the limits
        /// </remarks>
        public bool ComputeDualTrimmedNoiseLevel(IReadOnlyList<double> dataList, int indexStart, int indexEnd,
                                                 clsBaselineNoiseOptions baselineNoiseOptions,
                                                 out clsBaselineNoiseStats baselineNoiseStats)
        {
            // Initialize baselineNoiseStats
            baselineNoiseStats = InitializeBaselineNoiseStats(
                baselineNoiseOptions.MinimumBaselineNoiseLevel,
                eNoiseThresholdModes.DualTrimmedMeanByAbundance);

            if (dataList == null || indexEnd - indexStart < 0)
            {
                return false;
            }

            // Copy the data into dataListSorted
            int dataSortedCount = indexEnd - indexStart + 1;
            double[] dataListSorted;
            dataListSorted = new double[dataSortedCount];

            for (int i = indexStart; i <= indexEnd; i++)
                dataListSorted[i - indexStart] = dataList[i];

            // Sort the array
            Array.Sort(dataListSorted);

            // Look for the minimum positive value and replace all data in dataListSorted with that value
            double minimumPositiveValue = ReplaceSortedDataWithMinimumPositiveValue(dataSortedCount, dataListSorted);

            // Initialize the indices to use in dataListSorted()
            int dataSortedIndexStart = 0;
            int dataSortedIndexEnd = dataSortedCount - 1;

            // Compute the average using the data in dataListSorted between dataSortedIndexStart and dataSortedIndexEnd (i.e. all the data)
            double sum = 0;
            for (int i = dataSortedIndexStart; i <= dataSortedIndexEnd; i++)
                sum += dataListSorted[i];

            int dataUsedCount = dataSortedIndexEnd - dataSortedIndexStart + 1;
            double average = sum / dataUsedCount;
            double variance;

            if (dataUsedCount > 1)
            {
                // Compute the variance (this is a sample variance, not a population variance)
                sum = 0;
                for (int i = dataSortedIndexStart; i <= dataSortedIndexEnd; i++)
                    sum += Math.Pow(dataListSorted[i] - average, 2);
                variance = sum / (dataUsedCount - 1);
            }
            else
            {
                variance = 0;
            }

            if (baselineNoiseOptions.DualTrimmedMeanStdDevLimits < 1)
            {
                baselineNoiseOptions.DualTrimmedMeanStdDevLimits = 1;
            }

            // Note: Standard Deviation = sigma = SquareRoot(Variance)
            double intensityThresholdMin = average - Math.Sqrt(variance) * baselineNoiseOptions.DualTrimmedMeanStdDevLimits;
            double intensityThresholdMax = average + Math.Sqrt(variance) * baselineNoiseOptions.DualTrimmedMeanStdDevLimits;

            // Recompute the average using only the data between intensityThresholdMin and intensityThresholdMax in dataListSorted
            sum = 0;
            int sortedIndex = dataSortedIndexStart;
            while (sortedIndex <= dataSortedIndexEnd)
            {
                if (dataListSorted[sortedIndex] >= intensityThresholdMin)
                {
                    dataSortedIndexStart = sortedIndex;
                    while (sortedIndex <= dataSortedIndexEnd)
                    {
                        if (dataListSorted[sortedIndex] <= intensityThresholdMax)
                        {
                            sum += dataListSorted[sortedIndex];
                        }
                        else
                        {
                            dataSortedIndexEnd = sortedIndex - 1;
                            break;
                        }

                        sortedIndex += 1;
                    }
                }

                sortedIndex += 1;
            }

            dataUsedCount = dataSortedIndexEnd - dataSortedIndexStart + 1;

            if (dataUsedCount > 0)
            {
                baselineNoiseStats.NoiseLevel = sum / dataUsedCount;

                // Compute the variance (this is a sample variance, not a population variance)
                sum = 0;
                for (int i = dataSortedIndexStart; i <= dataSortedIndexEnd; i++)
                    sum += Math.Pow(dataListSorted[i] - baselineNoiseStats.NoiseLevel, 2);

                if (dataUsedCount > 1)
                {
                    baselineNoiseStats.NoiseStDev = Math.Sqrt(sum / (dataUsedCount - 1));
                }
                else
                {
                    baselineNoiseStats.NoiseStDev = 0;
                }

                baselineNoiseStats.PointsUsed = dataUsedCount;
            }
            else
            {
                baselineNoiseStats.NoiseLevel = Math.Max(minimumPositiveValue, baselineNoiseOptions.MinimumBaselineNoiseLevel);
                baselineNoiseStats.NoiseStDev = 0;
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

        private int ComputeFWHM(
            IList<clsSICDataPoint> sicData,
            clsSICStatsPeak sicPeak,
            bool subtractBaselineNoise)
        {
            // Note: The calling function should have already populated sicPeak.MaxIntensityValue, plus .IndexMax, .IndexBaseLeft, and .IndexBaseRight
            // If subtractBaselineNoise is True, then this function also uses sicPeak.BaselineNoiseStats....
            // Note: This function returns the FWHM value in units of scan number; it does not update the value stored in sicPeak
            // This function does, however, update sicPeak.IndexMax if it is not between sicPeak.IndexBaseLeft and sicPeak.IndexBaseRight

            const bool ALLOW_NEGATIVE_VALUES = false;
            double fwhmScanStart, fwhmScanEnd;
            int fwhmScans;
            double targetIntensity;
            double maximumIntensity;
            double y1, y2;

            // Determine the full width at half max (fwhm), in units of absolute scan number
            try
            {
                if (sicPeak.IndexMax <= sicPeak.IndexBaseLeft || sicPeak.IndexMax >= sicPeak.IndexBaseRight)
                {
                    // Find the index of the maximum (between .IndexBaseLeft and .IndexBaseRight)
                    maximumIntensity = 0;
                    if (sicPeak.IndexMax < sicPeak.IndexBaseLeft || sicPeak.IndexMax > sicPeak.IndexBaseRight)
                    {
                        sicPeak.IndexMax = sicPeak.IndexBaseLeft;
                    }

                    for (int dataIndex = sicPeak.IndexBaseLeft; dataIndex <= sicPeak.IndexBaseRight; dataIndex++)
                    {
                        if (sicData[dataIndex].Intensity > maximumIntensity)
                        {
                            sicPeak.IndexMax = dataIndex;
                            maximumIntensity = sicData[dataIndex].Intensity;
                        }
                    }
                }

                // Look for the intensity halfway down the peak (correcting for baseline noise level if subtractBaselineNoise = True)
                if (subtractBaselineNoise)
                {
                    targetIntensity = BaselineAdjustIntensity(sicPeak.MaxIntensityValue, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES) / 2;
                    if (targetIntensity <= 0)
                    {
                        // The maximum intensity of the peak is below the baseline; do not correct for baseline noise level
                        targetIntensity = sicPeak.MaxIntensityValue / 2;
                        subtractBaselineNoise = false;
                    }
                }
                else
                {
                    targetIntensity = sicPeak.MaxIntensityValue / 2;
                }

                if (targetIntensity > 0)
                {

                    // Start the search at each peak edge to thus determine the largest fwhm value
                    fwhmScanStart = -1;
                    for (int dataIndex = sicPeak.IndexBaseLeft; dataIndex <= sicPeak.IndexMax - 1; dataIndex++)
                    {
                        if (subtractBaselineNoise)
                        {
                            y1 = BaselineAdjustIntensity(sicData[dataIndex].Intensity, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES);
                            y2 = BaselineAdjustIntensity(sicData[dataIndex + 1].Intensity, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES);
                        }
                        else
                        {
                            y1 = sicData[dataIndex].Intensity;
                            y2 = sicData[dataIndex + 1].Intensity;
                        }

                        if (y1 > targetIntensity || y2 > targetIntensity)
                        {
                            if (y1 <= targetIntensity && y2 >= targetIntensity)
                            {
                                InterpolateX(out fwhmScanStart, sicData[dataIndex].ScanNumber, sicData[dataIndex + 1].ScanNumber, y1, y2, targetIntensity);
                            }
                            // targetIntensity is not between y1 and y2; simply use dataIndex
                            else if (dataIndex == sicPeak.IndexBaseLeft)
                            {
                                // At the start of the peak; use the scan number halfway between .IndexBaseLeft and .IndexMax
                                fwhmScanStart = sicData[dataIndex + Convert.ToInt32(Math.Round((sicPeak.IndexMax - sicPeak.IndexBaseLeft) / (double)2, 0))].ScanNumber;
                            }
                            else
                            {
                                // This code will probably never be reached
                                fwhmScanStart = sicData[dataIndex].ScanNumber;
                            }

                            break;
                        }
                    }

                    if (fwhmScanStart < 0)
                    {
                        if (sicPeak.IndexMax > sicPeak.IndexBaseLeft)
                        {
                            fwhmScanStart = sicData[sicPeak.IndexMax - 1].ScanNumber;
                        }
                        else
                        {
                            fwhmScanStart = sicData[sicPeak.IndexBaseLeft].ScanNumber;
                        }
                    }

                    fwhmScanEnd = -1;
                    for (int dataIndex = sicPeak.IndexBaseRight - 1; dataIndex >= sicPeak.IndexMax; dataIndex--)
                    {
                        if (subtractBaselineNoise)
                        {
                            y1 = BaselineAdjustIntensity(sicData[dataIndex].Intensity, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES);
                            y2 = BaselineAdjustIntensity(sicData[dataIndex + 1].Intensity, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES);
                        }
                        else
                        {
                            y1 = sicData[dataIndex].Intensity;
                            y2 = sicData[dataIndex + 1].Intensity;
                        }

                        if (y1 > targetIntensity || y2 > targetIntensity)
                        {
                            if (y1 >= targetIntensity && y2 <= targetIntensity)
                            {
                                InterpolateX(
                                    out fwhmScanEnd,
                                    sicData[dataIndex].ScanNumber, sicData[dataIndex + 1].ScanNumber,
                                    y1, y2, targetIntensity);
                            }
                            // targetIntensity is not between y1 and y2; simply use dataIndex+1
                            else if (dataIndex == sicPeak.IndexBaseRight - 1)
                            {
                                // At the end of the peak; use the scan number halfway between .IndexBaseRight and .IndexMax
                                fwhmScanEnd = sicData[dataIndex + 1 - Convert.ToInt32(Math.Round((sicPeak.IndexBaseRight - sicPeak.IndexMax) / (double)2, 0))].ScanNumber;
                            }
                            else
                            {
                                // This code will probably never be reached
                                fwhmScanEnd = sicData[dataIndex + 1].ScanNumber;
                            }

                            break;
                        }
                    }

                    if (fwhmScanEnd < 0)
                    {
                        if (sicPeak.IndexMax < sicPeak.IndexBaseRight)
                        {
                            fwhmScanEnd = sicData[sicPeak.IndexMax + 1].ScanNumber;
                        }
                        else
                        {
                            fwhmScanEnd = sicData[sicPeak.IndexBaseRight].ScanNumber;
                        }
                    }

                    fwhmScans = Convert.ToInt32(Math.Round(fwhmScanEnd - fwhmScanStart, 0));
                    if (fwhmScans <= 0)
                        fwhmScans = 0;
                }
                else
                {
                    // Maximum intensity value is <= 0
                    // Set fwhm to 1
                    fwhmScans = 1;
                }
            }
            catch (Exception ex)
            {
                LogErrors("clsMASICPeakFinder->ComputeFWHM", "Error finding fwhm", ex, false);
                fwhmScans = 0;
            }

            return fwhmScans;
        }

        // ReSharper disable once UnusedMember.Global
        public void TestComputeKSStat()
        {
            var scanNumbers = new int[] { 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40 };
            var intensities = new double[] { 2, 5, 7, 10, 11, 18, 19, 15, 8, 4, 1 };

            int scanAtApex = 20;
            double fwhm = 25;

            double peakMean = scanAtApex;
            // fwhm / 2.35482 = fwhm / (2 * Sqrt(2 * Ln(2)))

            double peakStDev = fwhm / 2.35482;
            // peakStDev = 28.8312

            ComputeKSStatistic(scanNumbers.Length, scanNumbers, intensities, peakMean, peakStDev);

            // ToDo: Update program to call ComputeKSStatistic

            // ToDo: Update Statistical Moments computation to:
            // a) Create baseline adjusted intensity values
            // b) Remove the contiguous data from either end that is <= 0
            // c) Step through the remaining data and interpolate across gaps with intensities of 0 (linear interpolation)
            // d) Use this final data to compute the statistical moments and KS Statistic

            // If less than 3 points remain with the above procedure, then use the 5 points centered around the peak maximum, non-baseline corrected data
        }

        private double ComputeKSStatistic(
            int dataCount,
            IList<int> xDataIn,
            IList<double> yDataIn,
            double peakMean,
            double peakStDev)
        {
            int[] xData;
            double[] yData;

            double[] yDataNormalized;
            double[] xDataPDF;
            double[] yDataEDF;
            double[] xDataCDF;

            double yDataSum;

            // Copy data from xDataIn() to xData, subtracting the value in xDataIn(0) from each scan
            xData = new int[dataCount];
            yData = new double[dataCount];

            int scanOffset = xDataIn[0];
            for (int i = 0; i <= dataCount - 1; i++)
            {
                xData[i] = xDataIn[i] - scanOffset;
                yData[i] = yDataIn[i];
            }

            yDataSum = 0;
            for (int i = 0; i <= yData.Length - 1; i++)
                yDataSum += yData[i];
            if (Math.Abs(yDataSum) < double.Epsilon)
                yDataSum = 1;

            // Compute the Vector of normalized intensities = observed pdf
            yDataNormalized = new double[yData.Length];
            for (int i = 0; i <= yData.Length - 1; i++)
                yDataNormalized[i] = yData[i] / yDataSum;

            // Estimate the empirical distribution function (EDF) using an accumulating sum
            yDataSum = 0;
            yDataEDF = new double[yDataNormalized.Length];
            for (int i = 0; i <= yDataNormalized.Length - 1; i++)
            {
                yDataSum += yDataNormalized[i];
                yDataEDF[i] = yDataSum;
            }

            // Compute the Vector of Normal PDF values evaluated at the X values in the peak window
            xDataPDF = new double[xData.Length];
            for (int i = 0; i <= xData.Length - 1; i++)
                xDataPDF[i] = 1 / (Math.Sqrt(2 * Math.PI) * peakStDev) * Math.Exp(-1 / (double)2 *
                                      Math.Pow((xData[i] - (peakMean - scanOffset)) / peakStDev, 2));

            double xDataPDFSum = 0;
            for (int i = 0; i <= xDataPDF.Length - 1; i++)
                xDataPDFSum += xDataPDF[i];

            // Estimate the theoretical CDF using an accumulating sum
            xDataCDF = new double[xDataPDF.Length];
            yDataSum = 0;
            for (int i = 0; i <= xDataPDF.Length - 1; i++)
            {
                yDataSum += xDataPDF[i];
                xDataCDF[i] = yDataSum / ((1 + 1 / (double)xData.Length) * xDataPDFSum);
            }

            // Compute the maximum of the absolute differences between the YData EDF and XData CDF
            double KS_gof = 0;
            for (int i = 0; i <= xDataCDF.Length - 1; i++)
            {
                double compareVal = Math.Abs(yDataEDF[i] - xDataCDF[i]);
                if (compareVal > KS_gof)
                {
                    KS_gof = compareVal;
                }
            }

            return Math.Sqrt(xData.Length) * KS_gof;   // return modified KS statistic
        }

        /// <summary>
        /// Compute the noise level
        /// </summary>
        /// <param name="dataCount"></param>
        /// <param name="dataList"></param>
        /// <param name="baselineNoiseOptions"></param>
        /// <param name="baselineNoiseStats"></param>
        /// <returns>Returns True if success, false in an error</returns>
        /// <remarks>Updates baselineNoiseStats with the baseline noise level</remarks>
        public bool ComputeNoiseLevelForSICData(int dataCount, IReadOnlyList<double> dataList,
                                                clsBaselineNoiseOptions baselineNoiseOptions,
                                                out clsBaselineNoiseStats baselineNoiseStats)
        {
            const bool IGNORE_NON_POSITIVE_DATA = false;

            if (baselineNoiseOptions.BaselineNoiseMode == eNoiseThresholdModes.AbsoluteThreshold)
            {
                baselineNoiseStats = InitializeBaselineNoiseStats(
                    baselineNoiseOptions.BaselineNoiseLevelAbsolute,
                    baselineNoiseOptions.BaselineNoiseMode);

                return true;
            }

            if (baselineNoiseOptions.BaselineNoiseMode == eNoiseThresholdModes.DualTrimmedMeanByAbundance)
            {
                return ComputeDualTrimmedNoiseLevel(dataList, 0, dataCount - 1, baselineNoiseOptions, out baselineNoiseStats);
            }
            else
            {
                return ComputeTrimmedNoiseLevel(dataList, 0, dataCount - 1, baselineNoiseOptions, IGNORE_NON_POSITIVE_DATA, out baselineNoiseStats);
            }
        }

        [Obsolete("Use the version that takes a List(Of clsSICDataPoint")]
        public bool ComputeNoiseLevelInPeakVicinity(
            int dataCount, int[] sicScanNumbers, double[] sicIntensities,
            clsSICStatsPeak sicPeak,
            clsBaselineNoiseOptions baselineNoiseOptions)
        {
            var sicData = new List<clsSICDataPoint>();

            for (int index = 0; index <= dataCount - 1; index++)
                sicData.Add(new clsSICDataPoint(sicScanNumbers[index], sicIntensities[index], 0));

            return ComputeNoiseLevelInPeakVicinity(sicData, sicPeak, baselineNoiseOptions);
        }

        public bool ComputeNoiseLevelInPeakVicinity(
            List<clsSICDataPoint> sicData,
            clsSICStatsPeak sicPeak,
            clsBaselineNoiseOptions baselineNoiseOptions)
        {
            const int NOISE_ESTIMATE_DATA_COUNT_MINIMUM = 5;
            const int NOISE_ESTIMATE_DATA_COUNT_MAXIMUM = 100;

            bool success;

            // Initialize baselineNoiseStats
            sicPeak.BaselineNoiseStats = InitializeBaselineNoiseStats(
                baselineNoiseOptions.MinimumBaselineNoiseLevel,
                eNoiseThresholdModes.MeanOfDataInPeakVicinity);

            // Only use a portion of the data to compute the noise level
            // The number of points to extend from the left and right is based on the width at 4 sigma; useful for tailing peaks
            // Also, determine the peak start using the smaller of the width at 4 sigma vs. the observed peak width

            // Estimate fwhm since it is sometimes not yet known when this function is called
            // The reason it's not yet know is that the final fwhm value is computed using baseline corrected intensity data, but
            // the whole purpose of this function is to compute the baseline level
            sicPeak.FWHMScanWidth = ComputeFWHM(sicData, sicPeak, false);

            // Minimum of peak width at 4 sigma vs. peakWidthFullScans
            int peakWidthBaseScans = ComputeWidthAtBaseUsingFWHM(sicPeak, sicData, 4);
            int peakWidthPoints = ConvertScanWidthToPoints(peakWidthBaseScans, sicPeak, sicData);

            int peakHalfWidthPoints = Convert.ToInt32(Math.Round(peakWidthPoints / 1.5, 0));

            // Make sure that peakHalfWidthPoints is at least NOISE_ESTIMATE_DATA_COUNT_MINIMUM
            if (peakHalfWidthPoints < NOISE_ESTIMATE_DATA_COUNT_MINIMUM)
            {
                peakHalfWidthPoints = NOISE_ESTIMATE_DATA_COUNT_MINIMUM;
            }

            // Copy the peak base indices
            int indexBaseLeft = sicPeak.IndexBaseLeft;
            int indexBaseRight = sicPeak.IndexBaseRight;

            // Define IndexStart and IndexEnd, making sure that peakHalfWidthPoints is no larger than NOISE_ESTIMATE_DATA_COUNT_MAXIMUM
            int indexStart = indexBaseLeft - Math.Min(peakHalfWidthPoints, NOISE_ESTIMATE_DATA_COUNT_MAXIMUM);
            int indexEnd = sicPeak.IndexBaseRight + Math.Min(peakHalfWidthPoints, NOISE_ESTIMATE_DATA_COUNT_MAXIMUM);

            if (indexStart < 0)
                indexStart = 0;
            if (indexEnd >= sicData.Count)
                indexEnd = sicData.Count - 1;

            // Compare indexStart to sicPeak.PreviousPeakFWHMPointRight
            // If it is less than .PreviousPeakFWHMPointRight, then update accordingly
            if (indexStart < sicPeak.PreviousPeakFWHMPointRight &&
                sicPeak.PreviousPeakFWHMPointRight < sicPeak.IndexMax)
            {
                // Update indexStart to be at PreviousPeakFWHMPointRight
                indexStart = sicPeak.PreviousPeakFWHMPointRight;
                if (indexStart < 0)
                    indexStart = 0;

                // If not enough points, then alternately shift indexStart to the left 1 point and
                // indexBaseLeft to the right one point until we do have enough points
                bool shiftLeft = true;
                while (indexBaseLeft - indexStart + 1 < NOISE_ESTIMATE_DATA_COUNT_MINIMUM)
                {
                    if (shiftLeft)
                    {
                        if (indexStart > 0)
                            indexStart -= 1;
                    }
                    else if (indexBaseLeft < sicPeak.IndexMax)
                        indexBaseLeft += 1;
                    if (indexStart <= 0 && indexBaseLeft >= sicPeak.IndexMax)
                    {
                        break;
                    }
                    else
                    {
                        shiftLeft = !shiftLeft;
                    }
                }
            }

            // Compare indexEnd to sicPeak.NextPeakFWHMPointLeft
            // If it is greater than .NextPeakFWHMPointLeft, then update accordingly
            if (indexEnd >= sicPeak.NextPeakFWHMPointLeft &&
                sicPeak.NextPeakFWHMPointLeft > sicPeak.IndexMax)
            {
                indexEnd = sicPeak.NextPeakFWHMPointLeft;

                if (indexEnd >= sicData.Count)
                    indexEnd = sicData.Count - 1;

                // If not enough points, then alternately shift indexEnd to the right 1 point and
                // indexBaseRight to the left one point until we do have enough points
                bool shiftLeft = false;
                while (indexEnd - indexBaseRight + 1 < NOISE_ESTIMATE_DATA_COUNT_MINIMUM)
                {
                    if (shiftLeft)
                    {
                        if (indexBaseRight > sicPeak.IndexMax)
                            indexBaseRight -= 1;
                    }
                    else if (indexEnd < sicData.Count - 1)
                        indexEnd += 1;
                    if (indexBaseRight <= sicPeak.IndexMax && indexEnd >= sicData.Count - 1)
                    {
                        break;
                    }
                    else
                    {
                        shiftLeft = !shiftLeft;
                    }
                }
            }

            success = ComputeAverageNoiseLevelExcludingRegion(
                sicData,
                indexStart, indexEnd,
                indexBaseLeft, indexBaseRight,
                baselineNoiseOptions, sicPeak.BaselineNoiseStats);

            // Assure that .NoiseLevel is >= .MinimumBaselineNoiseLevel
            if (sicPeak.BaselineNoiseStats.NoiseLevel < Math.Max(1, baselineNoiseOptions.MinimumBaselineNoiseLevel))
            {
                var noiseStats = sicPeak.BaselineNoiseStats;

                noiseStats.NoiseLevel = Math.Max(1, baselineNoiseOptions.MinimumBaselineNoiseLevel);

                // Set this to 0 since we have overridden .NoiseLevel
                noiseStats.NoiseStDev = 0;

                sicPeak.BaselineNoiseStats = noiseStats;
            }

            return success;
        }

        /// <summary>
        /// Determine the value for sicPeak.ParentIonIntensity
        /// The goal is to determine the intensity that the SIC data has in one scan prior to sicPeak.IndexObserved
        /// This intensity value may be an interpolated value between two observed SIC values
        /// </summary>
        /// <param name="dataCount"></param>
        /// <param name="sicScanNumbers">List of scan numbers</param>
        /// <param name="sicIntensities">List of intensities</param>
        /// <param name="sicPeak"></param>
        /// <param name="fragScanNumber"></param>
        /// <returns></returns>
        [Obsolete("Use the version that takes a List(Of clsSICDataPoint")]
        public bool ComputeParentIonIntensity(
            int dataCount,
            int[] sicScanNumbers,
            double[] sicIntensities,
            clsSICStatsPeak sicPeak,
            int fragScanNumber)
        {
            var sicData = new List<clsSICDataPoint>();

            for (int index = 0; index <= dataCount - 1; index++)
                sicData.Add(new clsSICDataPoint(sicScanNumbers[index], sicIntensities[index], 0));

            return ComputeParentIonIntensity(sicData, sicPeak, fragScanNumber);
        }

        /// <summary>
        /// Determine the value for sicPeak.ParentIonIntensity
        /// The goal is to determine the intensity that the SIC data has in one scan prior to sicPeak.IndexObserved
        /// This intensity value may be an interpolated value between two observed SIC values
        /// </summary>
        /// <param name="sicData"></param>
        /// <param name="sicPeak"></param>
        /// <param name="fragScanNumber"></param>
        /// <returns></returns>
        public bool ComputeParentIonIntensity(
            IList<clsSICDataPoint> sicData,
            clsSICStatsPeak sicPeak,
            int fragScanNumber)
        {
            bool success;

            try
            {
                // Lookup the scan number and intensity of the SIC scan at sicPeak.IndexObserved
                int x1 = sicData[sicPeak.IndexObserved].ScanNumber;
                double y1 = sicData[sicPeak.IndexObserved].Intensity;

                if (x1 == fragScanNumber - 1)
                {
                    // The fragmentation scan was the next scan after the SIC scan the data was observed in
                    // We can use y1 for .ParentIonIntensity
                    sicPeak.ParentIonIntensity = y1;
                }
                else if (x1 >= fragScanNumber)
                {
                    // The fragmentation scan has the same scan number as the SIC scan just before it, or the SIC scan is greater than the fragmentation scan
                    // This shouldn't normally happen, but we'll account for the possibility anyway
                    // If the data file only has MS spectra and no MS/MS spectra, and if the parent ion is a custom M/Z value, then this code will be reached
                    sicPeak.ParentIonIntensity = y1;
                }
                // We need to perform some interpolation to determine .ParentIonIntensity
                // Lookup the scan number and intensity of the next SIC scan
                else if (sicPeak.IndexObserved < sicData.Count - 1)
                {
                    int x2 = sicData[sicPeak.IndexObserved + 1].ScanNumber;
                    double y2 = sicData[sicPeak.IndexObserved + 1].Intensity;
                    double interpolatedIntensity;

                    success = InterpolateY(out interpolatedIntensity, x1, x2, y1, y2, (double)(fragScanNumber - 1));

                    if (success)
                    {
                        sicPeak.ParentIonIntensity = interpolatedIntensity;
                    }
                    else
                    {
                        // Interpolation failed; use y1
                        sicPeak.ParentIonIntensity = y1;
                    }
                }
                else
                {
                    // Cannot interpolate; we'll have to use y1 as .ParentIonIntensity
                    sicPeak.ParentIonIntensity = y1;
                }

                success = true;
            }
            catch (Exception ex)
            {
                // Ignore errors here
                success = false;
            }

            return success;
        }

        private bool ComputeSICPeakArea(IList<clsSICDataPoint> sicData, clsSICStatsPeak sicPeak)
        {
            // The calling function must populate sicPeak.IndexMax, sicPeak.IndexBaseLeft, and sicPeak.IndexBaseRight

            int[] scanNumbers;
            double[] intensities;
            try
            {

                // Compute the peak area

                // Copy the matching data from the source arrays to scanNumbers() and intensities
                // When copying, assure that the first and last points have an intensity of 0

                // We're reserving extra space in case we need to prepend or append a minimum value
                scanNumbers = new int[sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 2 + 1];
                intensities = new double[sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 2 + 1];

                // Define an intensity threshold of 5% of MaximumIntensity
                // If the peak data is not flanked by points <= intensityThreshold, then we'll add them
                double intensityThreshold = sicData[sicPeak.IndexMax].Intensity * 0.05;

                // Estimate the average scan interval between each data point
                int avgScanInterval = Convert.ToInt32(Math.Round(ComputeAvgScanInterval(sicData, sicPeak.IndexBaseLeft, sicPeak.IndexBaseRight), 0));

                int areaDataBaseIndex;
                if (sicData[sicPeak.IndexBaseLeft].Intensity > intensityThreshold)
                {
                    // Prepend an intensity data point of intensityThreshold, with a scan number avgScanInterval less than the first scan number for the actual peak data
                    scanNumbers[0] = sicData[sicPeak.IndexBaseLeft].ScanNumber - avgScanInterval;
                    intensities[0] = intensityThreshold;
                    // intensitiesSmoothed(0) = intensityThreshold
                    areaDataBaseIndex = 1;
                }
                else
                {
                    areaDataBaseIndex = 0;
                }

                // Populate scanNumbers() and intensities()
                for (int dataIndex = sicPeak.IndexBaseLeft; dataIndex <= sicPeak.IndexBaseRight; dataIndex++)
                {
                    int indexPointer = dataIndex - sicPeak.IndexBaseLeft + areaDataBaseIndex;
                    scanNumbers[indexPointer] = sicData[dataIndex].ScanNumber;
                    intensities[indexPointer] = sicData[dataIndex].Intensity;
                    // intensitiesSmoothed(indexPointer) = smoothedYDataSubset.Data(dataIndex - smoothedYDataSubset.DataStartIndex)
                    // If intensitiesSmoothed(indexPointer) < 0 Then intensitiesSmoothed(indexPointer) = 0
                }

                int areaDataCount = sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 1 + areaDataBaseIndex;

                if (sicData[sicPeak.IndexBaseRight].Intensity > intensityThreshold)
                {
                    // Append an intensity data point of intensityThreshold, with a scan number avgScanInterval more than the last scan number for the actual peak data
                    int dataIndex = sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + areaDataBaseIndex + 1;
                    scanNumbers[dataIndex] = sicData[sicPeak.IndexBaseRight].ScanNumber + avgScanInterval;
                    intensities[dataIndex] = intensityThreshold;
                    areaDataCount += 1;
                    // intensitiesSmoothed(dataIndex) = intensityThreshold
                }

                // Compute the area
                // Note that we're using real data for this and not smoothed data
                // Also note that we're using raw data for the peak area (not baseline corrected values)
                double peakArea = 0;
                for (int dataIndex = 0; dataIndex <= areaDataCount - 2; dataIndex++)
                {
                    // Use the Trapezoid area formula to compute the area slice to add to sicPeak.Area
                    // Area = 0.5 * DeltaX * (Y1 + Y2)
                    int scanDelta = scanNumbers[dataIndex + 1] - scanNumbers[dataIndex];
                    peakArea += 0.5 * scanDelta * (intensities[dataIndex] + intensities[dataIndex + 1]);
                }

                if (peakArea < 0)
                {
                    sicPeak.Area = 0;
                }
                else
                {
                    sicPeak.Area = peakArea;
                }
            }
            catch (Exception ex)
            {
                LogErrors("clsMASICPeakFinder->ComputeSICPeakArea", "Error computing area", ex, false);
                return false;
            }

            return true;
        }

        private double ComputeAvgScanInterval(IList<clsSICDataPoint> sicData, int dataIndexStart, int dataIndexEnd)
        {
            double scansPerPoint;
            try
            {
                // Estimate the average scan interval between each data point
                if (dataIndexEnd >= dataIndexStart)
                {
                    scansPerPoint = (sicData[dataIndexEnd].ScanNumber - sicData[dataIndexStart].ScanNumber) / (double)(dataIndexEnd - dataIndexStart + 1);
                    if (scansPerPoint < 1)
                        scansPerPoint = 1;
                }
                else
                {
                    scansPerPoint = 1;
                }
            }
            catch (Exception ex)
            {
                scansPerPoint = 1;
            }

            return scansPerPoint;
        }

        private bool ComputeStatisticalMomentsStats(
            IList<clsSICDataPoint> sicData,
            clsSmoothedYDataSubset smoothedYDataSubset,
            clsSICStatsPeak sicPeak)
        {
            // The calling function must populate sicPeak.IndexMax, sicPeak.IndexBaseLeft, and sicPeak.IndexBaseRight
            // Returns True if success; false if an error or less than 3 usable data points

            const bool ALLOW_NEGATIVE_VALUES = false;
            const bool USE_SMOOTHED_DATA = true;
            const int DEFAULT_MINIMUM_DATA_COUNT = 5;

            // Note that we're using baseline corrected intensity values for the statistical moments
            // However, it is important that we use continuous, positive data for computing statistical moments

            try
            {
                // Initialize to default values

                var statMomentsData = new clsStatisticalMoments()
                {
                    Area = 0,
                    StDev = 0,
                    Skew = 0,
                    KSStat = 0,
                    DataCountUsed = 0
                };

                try
                {
                    if (sicPeak.IndexMax >= 0 && sicPeak.IndexMax < sicData.Count)
                    {
                        statMomentsData.CenterOfMassScan = sicData[sicPeak.IndexMax].ScanNumber;
                    }
                }
                catch (Exception ex)
                {
                    // Ignore errors here
                }

                sicPeak.StatisticalMoments = statMomentsData;

                int dataCount = sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 1;
                if (dataCount < 1)
                {
                    // Do not continue if less than one point across the peak
                    return false;
                }

                // When reserving memory for these arrays, include room to add a minimum value at the beginning and end of the data, if needed
                // Also, reserve space for a minimum of 5 elements
                int minimumDataCount = DEFAULT_MINIMUM_DATA_COUNT;
                if (minimumDataCount > dataCount)
                {
                    minimumDataCount = 3;
                }

                int[] scanNumbers;             // Contains values from sicData[x].ScanNumber
                double[] intensities;          // Contains values from sicData[x].Intensity subtracted by the baseline noise level; if the result is less than 0, then will contain 0

                scanNumbers = new int[Math.Max(dataCount, minimumDataCount) + 1 + 1];
                intensities = new double[scanNumbers.Length];
                bool useRawDataAroundMaximum = false;

                // Populate scanNumbers() and intensities()
                // Simultaneously, determine the maximum intensity
                double maximumBaselineAdjustedIntensity = 0;
                int indexMaximumIntensity = 0;

                if (USE_SMOOTHED_DATA)
                {
                    dataCount = 0;
                    for (int dataIndex = sicPeak.IndexBaseLeft; dataIndex <= sicPeak.IndexBaseRight; dataIndex++)
                    {
                        int smoothedDataPointer = dataIndex - smoothedYDataSubset.DataStartIndex;
                        if (smoothedDataPointer >= 0 && smoothedDataPointer < smoothedYDataSubset.DataCount)
                        {
                            scanNumbers[dataCount] = sicData[dataIndex].ScanNumber;
                            intensities[dataCount] = BaselineAdjustIntensity(
                                smoothedYDataSubset.Data[smoothedDataPointer],
                                sicPeak.BaselineNoiseStats.NoiseLevel,
                                ALLOW_NEGATIVE_VALUES);

                            if (intensities[dataCount] > maximumBaselineAdjustedIntensity)
                            {
                                maximumBaselineAdjustedIntensity = intensities[dataCount];
                                indexMaximumIntensity = dataCount;
                            }

                            dataCount += 1;
                        }
                    }
                }
                else
                {
                    dataCount = 0;
                    for (int dataIndex = sicPeak.IndexBaseLeft; dataIndex <= sicPeak.IndexBaseRight; dataIndex++)
                    {
                        scanNumbers[dataCount] = sicData[dataIndex].ScanNumber;
                        intensities[dataCount] = BaselineAdjustIntensity(
                            sicData[dataIndex].Intensity,
                            sicPeak.BaselineNoiseStats.NoiseLevel,
                            ALLOW_NEGATIVE_VALUES);

                        if (intensities[dataCount] > maximumBaselineAdjustedIntensity)
                        {
                            maximumBaselineAdjustedIntensity = intensities[dataCount];
                            indexMaximumIntensity = dataCount;
                        }

                        dataCount += 1;
                    }
                }

                // Define an intensity threshold of 10% of MaximumBaselineAdjustedIntensity
                double intensityThreshold = maximumBaselineAdjustedIntensity * 0.1;
                if (intensityThreshold < 1)
                    intensityThreshold = 1;

                // Step left from indexMaximumIntensity to find the first data point < intensityThreshold
                // Note that the final data will include one data point less than intensityThreshold at the beginning and end of the data
                int validDataIndexLeft = indexMaximumIntensity;
                while (validDataIndexLeft > 0 && intensities[validDataIndexLeft] >= intensityThreshold)
                    validDataIndexLeft -= 1;

                // Step right from indexMaximumIntensity to find the first data point < intensityThreshold
                int validDataIndexRight = indexMaximumIntensity;
                while (validDataIndexRight < dataCount - 1 && intensities[validDataIndexRight] >= intensityThreshold)
                    validDataIndexRight += 1;

                if (validDataIndexLeft > 0 || validDataIndexRight < dataCount - 1)
                {
                    // Shrink the arrays to only retain the data centered around indexMaximumIntensity and
                    // having and intensity >= intensityThreshold, though one additional data point is retained at the beginning and end of the data
                    for (int dataIndex = validDataIndexLeft; dataIndex <= validDataIndexRight; dataIndex++)
                    {
                        int indexPointer = dataIndex - validDataIndexLeft;
                        scanNumbers[indexPointer] = scanNumbers[dataIndex];
                        intensities[indexPointer] = intensities[dataIndex];
                    }

                    dataCount = validDataIndexRight - validDataIndexLeft + 1;
                }

                if (dataCount < minimumDataCount)
                {
                    useRawDataAroundMaximum = true;
                }
                else
                {
                    // Remove the contiguous data from the left that is < intensityThreshold, retaining one point < intensityThreshold
                    // Due to the algorithm used to find the contiguous data centered around the peak maximum, this will typically have no effect
                    validDataIndexLeft = 0;
                    while (validDataIndexLeft < dataCount - 1 && intensities[validDataIndexLeft + 1] < intensityThreshold)
                        validDataIndexLeft += 1;

                    if (validDataIndexLeft >= dataCount - 1)
                    {
                        // All of the data is <= intensityThreshold
                        useRawDataAroundMaximum = true;
                    }
                    else
                    {
                        if (validDataIndexLeft > 0)
                        {
                            // Shrink the array to remove the values at the beginning that are < intensityThreshold, retaining one point < intensityThreshold
                            // Due to the algorithm used to find the contiguous data centered around the peak maximum, this code will typically never be reached
                            for (int dataIndex = validDataIndexLeft; dataIndex <= dataCount - 1; dataIndex++)
                            {
                                int indexPointer = dataIndex - validDataIndexLeft;
                                scanNumbers[indexPointer] = scanNumbers[dataIndex];
                                intensities[indexPointer] = intensities[dataIndex];
                            }

                            dataCount -= validDataIndexLeft;
                        }

                        // Remove the contiguous data from the right that is < intensityThreshold, retaining one point < intensityThreshold
                        // Due to the algorithm used to find the contiguous data centered around the peak maximum, this will typically have no effect
                        validDataIndexRight = dataCount - 1;
                        while (validDataIndexRight > 0 && intensities[validDataIndexRight - 1] < intensityThreshold)
                            validDataIndexRight -= 1;

                        if (validDataIndexRight < dataCount - 1)
                        {
                            // Shrink the array to remove the values at the end that are < intensityThreshold, retaining one point < intensityThreshold
                            // Due to the algorithm used to find the contiguous data centered around the peak maximum, this code will typically never be reached
                            dataCount = validDataIndexRight + 1;
                        }

                        // Estimate the average scan interval between the data points in scanNumbers
                        int avgScanInterval = Convert.ToInt32(Math.Round(ComputeAvgScanInterval(sicData, 0, dataCount - 1), 0));

                        // Make sure that intensities(0) is <= intensityThreshold
                        if (intensities[0] > intensityThreshold)
                        {
                            // Prepend a data point with intensity intensityThreshold and with a scan number 1 less than the first scan number in the valid data
                            for (int dataIndex = dataCount; dataIndex >= 1; dataIndex--)
                            {
                                scanNumbers[dataIndex] = scanNumbers[dataIndex - 1];
                                intensities[dataIndex] = intensities[dataIndex - 1];
                            }

                            scanNumbers[0] = scanNumbers[1] - avgScanInterval;
                            intensities[0] = intensityThreshold;
                            dataCount += 1;
                        }

                        // Make sure that intensities(dataCount-1) is <= intensityThreshold
                        if (intensities[dataCount - 1] > intensityThreshold)
                        {
                            // Append a data point with intensity intensityThreshold and with a scan number 1 more than the last scan number in the valid data
                            scanNumbers[dataCount] = scanNumbers[dataCount - 1] + avgScanInterval;
                            intensities[dataCount] = intensityThreshold;
                            dataCount += 1;
                        }
                    }
                }

                if (useRawDataAroundMaximum || dataCount < minimumDataCount)
                {
                    // Populate scanNumbers() and intensities() with the five data points centered around sicPeak.IndexMax
                    if (USE_SMOOTHED_DATA)
                    {
                        validDataIndexLeft = sicPeak.IndexMax - Convert.ToInt32(Math.Floor(minimumDataCount / (double)2));
                        if (validDataIndexLeft < 0)
                            validDataIndexLeft = 0;
                        dataCount = 0;
                        for (int dataIndex = validDataIndexLeft; dataIndex <= Math.Min(validDataIndexLeft + minimumDataCount - 1, sicData.Count - 1); dataIndex++)
                        {
                            int smoothedDataPointer = dataIndex - smoothedYDataSubset.DataStartIndex;
                            if (smoothedDataPointer >= 0 && smoothedDataPointer < smoothedYDataSubset.DataCount)
                            {
                                if (smoothedYDataSubset.Data[smoothedDataPointer] > 0)
                                {
                                    scanNumbers[dataCount] = sicData[dataIndex].ScanNumber;
                                    intensities[dataCount] = smoothedYDataSubset.Data[smoothedDataPointer];
                                    dataCount += 1;
                                }
                            }
                        }
                    }
                    else
                    {
                        validDataIndexLeft = sicPeak.IndexMax - Convert.ToInt32(Math.Floor(minimumDataCount / (double)2));
                        if (validDataIndexLeft < 0)
                            validDataIndexLeft = 0;
                        dataCount = 0;
                        for (int dataIndex = validDataIndexLeft; dataIndex <= Math.Min(validDataIndexLeft + minimumDataCount - 1, sicData.Count - 1); dataIndex++)
                        {
                            if (sicData[dataIndex].Intensity > 0)
                            {
                                scanNumbers[dataCount] = sicData[dataIndex].ScanNumber;
                                intensities[dataCount] = sicData[dataIndex].Intensity;
                                dataCount += 1;
                            }
                        }
                    }

                    if (dataCount < 3)
                    {
                        // We don't even have 3 positive values in the raw data; do not continue
                        return false;
                    }
                }

                // Step through intensities and interpolate across gaps with intensities of 0
                // Due to the algorithm used to find the contiguous data centered around the peak maximum, this will typically have no effect
                int pointIndex = 1;
                while (pointIndex < dataCount - 1)
                {
                    if (intensities[pointIndex] <= 0)
                    {
                        // Current point has an intensity of 0
                        // Find the next positive point
                        validDataIndexLeft = pointIndex + 1;
                        while (validDataIndexLeft < dataCount && intensities[validDataIndexLeft] <= 0)
                            validDataIndexLeft += 1;

                        // Interpolate between pointIndex-1 and validDataIndexLeft
                        for (int indexPointer = pointIndex; indexPointer <= validDataIndexLeft - 1; indexPointer++)
                        {
                            double interpolatedIntensity;

                            if (InterpolateY(
                                out interpolatedIntensity,
                                scanNumbers[pointIndex - 1], scanNumbers[validDataIndexLeft],
                                intensities[pointIndex - 1], intensities[validDataIndexLeft],
                                (double)scanNumbers[indexPointer]))
                            {
                                intensities[indexPointer] = interpolatedIntensity;
                            }
                        }

                        pointIndex = validDataIndexLeft + 1;
                    }
                    else
                    {
                        pointIndex += 1;
                    }
                }

                // Compute the zeroth moment (m0)
                double peakArea = 0;
                for (int dataIndex = 0; dataIndex <= dataCount - 2; dataIndex++)
                {
                    // Use the Trapezoid area formula to compute the area slice to add to peakArea
                    // Area = 0.5 * DeltaX * (Y1 + Y2)
                    int scanDelta = scanNumbers[dataIndex + 1] - scanNumbers[dataIndex];
                    peakArea += 0.5 * scanDelta * (intensities[dataIndex] + intensities[dataIndex + 1]);
                }

                // For the first moment (m1), need to sum: intensity times scan number.
                // For each of the moments, need to subtract scanNumbers(0) from the scan numbers since
                // statistical moments calculations are skewed if the first X value is not zero.
                // When ScanDelta is > 1, then need to interpolate.

                double moment1Sum = (scanNumbers[0] - scanNumbers[0]) * intensities[0];
                for (int dataIndex = 1; dataIndex <= dataCount - 1; dataIndex++)
                {
                    moment1Sum += (scanNumbers[dataIndex] - scanNumbers[0]) * intensities[dataIndex];

                    int scanDelta = scanNumbers[dataIndex] - scanNumbers[dataIndex - 1];
                    if (scanDelta > 1)
                    {
                        // Data points are more than 1 scan apart; need to interpolate values
                        // However, no need to interpolate if both intensity values are 0
                        if (intensities[dataIndex - 1] > 0 || intensities[dataIndex] > 0)
                        {
                            for (int scanNumberInterpolate = scanNumbers[dataIndex - 1] + 1; scanNumberInterpolate <= scanNumbers[dataIndex] - 1; scanNumberInterpolate++)
                            {
                                // Use InterpolateY() to fill in the scans between dataIndex-1 and dataIndex
                                double interpolatedIntensity;
                                if (InterpolateY(
                                    out interpolatedIntensity,
                                    scanNumbers[dataIndex - 1], scanNumbers[dataIndex],
                                    intensities[dataIndex - 1], intensities[dataIndex],
                                    (double)scanNumberInterpolate))
                                {
                                    moment1Sum += (scanNumberInterpolate - scanNumbers[0]) * interpolatedIntensity;
                                }
                            }
                        }
                    }
                }

                if (peakArea <= 0)
                {
                    // Cannot compute the center of mass; use the scan at .IndexMax instead
                    var centerOfMassDecimal = default(double);

                    int indexPointer = sicPeak.IndexMax - sicPeak.IndexBaseLeft;
                    if (indexPointer >= 0 && indexPointer < scanNumbers.Length)
                    {
                        centerOfMassDecimal = scanNumbers[indexPointer];
                    }

                    statMomentsData.CenterOfMassScan = Convert.ToInt32(Math.Round(centerOfMassDecimal, 0));
                    statMomentsData.DataCountUsed = 1;
                }
                else
                {
                    // Area is positive; compute the center of mass

                    double centerOfMassDecimal = moment1Sum / peakArea + scanNumbers[0];

                    statMomentsData.Area = Math.Min(double.MaxValue, peakArea);
                    statMomentsData.CenterOfMassScan = Convert.ToInt32(Math.Round(centerOfMassDecimal, 0));
                    statMomentsData.DataCountUsed = dataCount;

                    // For the second moment (m2), need to sum: (ScanNumber - m1)^2 * Intensity
                    // For the third moment (m3), need to sum: (ScanNumber - m1)^3 * Intensity
                    // When ScanDelta is > 1, then need to interpolate
                    double moment2Sum = Math.Pow(scanNumbers[0] - centerOfMassDecimal, 2) * intensities[0];
                    double moment3Sum = Math.Pow(scanNumbers[0] - centerOfMassDecimal, 3) * intensities[0];
                    for (int dataIndex = 1; dataIndex <= dataCount - 1; dataIndex++)
                    {
                        moment2Sum += Math.Pow(scanNumbers[dataIndex] - centerOfMassDecimal, 2) * intensities[dataIndex];
                        moment3Sum += Math.Pow(scanNumbers[dataIndex] - centerOfMassDecimal, 3) * intensities[dataIndex];

                        int scanDelta = scanNumbers[dataIndex] - scanNumbers[dataIndex - 1];
                        if (scanDelta > 1)
                        {
                            // Data points are more than 1 scan apart; need to interpolate values
                            // However, no need to interpolate if both intensity values are 0
                            if (intensities[dataIndex - 1] > 0 || intensities[dataIndex] > 0)
                            {
                                for (int scanNumberInterpolate = scanNumbers[dataIndex - 1] + 1; scanNumberInterpolate <= scanNumbers[dataIndex] - 1; scanNumberInterpolate++)
                                {
                                    // Use InterpolateY() to fill in the scans between dataIndex-1 and dataIndex
                                    double interpolatedIntensity;
                                    if (InterpolateY(
                                        out interpolatedIntensity,
                                        scanNumbers[dataIndex - 1], scanNumbers[dataIndex],
                                        intensities[dataIndex - 1], intensities[dataIndex],
                                        (double)scanNumberInterpolate))
                                    {
                                        moment2Sum += Math.Pow(scanNumberInterpolate - centerOfMassDecimal, 2) * interpolatedIntensity;
                                        moment3Sum += Math.Pow(scanNumberInterpolate - centerOfMassDecimal, 3) * interpolatedIntensity;
                                    }
                                }
                            }
                        }
                    }

                    statMomentsData.StDev = Math.Sqrt(moment2Sum / peakArea);

                    // thirdMoment = moment3Sum / peakArea
                    // skew = thirdMoment / sigma^3
                    // skew = (moment3Sum / peakArea) / sigma^3
                    if (statMomentsData.StDev > 0)
                    {
                        statMomentsData.Skew = moment3Sum / peakArea / Math.Pow(statMomentsData.StDev, 3);
                        if (Math.Abs(statMomentsData.Skew) < 0.0001)
                        {
                            statMomentsData.Skew = 0;
                        }
                    }
                    else
                    {
                        statMomentsData.Skew = 0;
                    }
                }

                const bool useStatMomentsStats = true;
                double peakMean;
                double peakStDev;

                if (useStatMomentsStats)
                {
                    peakMean = statMomentsData.CenterOfMassScan;
                    peakStDev = statMomentsData.StDev;
                }
                else
                {
                    peakMean = sicData[sicPeak.IndexMax].ScanNumber;
                    // fwhm / 2.35482 = fwhm / (2 * Sqrt(2 * Ln(2)))
                    peakStDev = sicPeak.FWHMScanWidth / 2.35482;
                }

                statMomentsData.KSStat = ComputeKSStatistic(dataCount, scanNumbers, intensities, peakMean, peakStDev);
            }

            catch (Exception ex)
            {
                LogErrors("clsMASICPeakFinder->ComputeStatisticalMomentsStats", "Error computing statistical moments", ex, false);
                return false;
            }

            return true;
        }

        public static double ComputeSignalToNoise(double signal, double noiseThresholdIntensity)
        {
            if (noiseThresholdIntensity > 0)
            {
                return signal / noiseThresholdIntensity;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Computes a trimmed mean or trimmed median using the low intensity data up to baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage
        /// Additionally, computes a full median using all data in dataList
        /// If ignoreNonPositiveData is True, then removes data from dataList() less than zero 0 and less than .MinimumBaselineNoiseLevel
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="indexStart"></param>
        /// <param name="indexEnd"></param>
        /// <param name="baselineNoiseOptions"></param>
        /// <param name="ignoreNonPositiveData"></param>
        /// <param name="baselineNoiseStats"></param>
        /// <returns>Returns True if success, False if error (or no data in dataList)</returns>
        /// <remarks>
        /// Replaces values of 0 with the minimum positive value in dataList()
        /// You cannot use dataList.Length to determine the length of the array; use dataCount
        /// </remarks>
        public bool ComputeTrimmedNoiseLevel(
            IReadOnlyList<double> dataList, int indexStart, int indexEnd,
            clsBaselineNoiseOptions baselineNoiseOptions,
            bool ignoreNonPositiveData,
            out clsBaselineNoiseStats baselineNoiseStats)
        {
            double[] dataListSorted;           // Note: You cannot use dataListSorted.Length to determine the length of the array; use indexStart and indexEnd to find the limits

            // Initialize baselineNoiseStats
            baselineNoiseStats = InitializeBaselineNoiseStats(baselineNoiseOptions.MinimumBaselineNoiseLevel, baselineNoiseOptions.BaselineNoiseMode);

            if (dataList == null || indexEnd - indexStart < 0)
            {
                return false;
            }

            // Copy the data into dataListSorted
            int dataSortedCount = indexEnd - indexStart + 1;
            dataListSorted = new double[dataSortedCount];

            for (int i = indexStart; i <= indexEnd; i++)
                dataListSorted[i - indexStart] = dataList[i];

            // Sort the array
            Array.Sort(dataListSorted);

            if (ignoreNonPositiveData)
            {
                // Remove data with a value <= 0

                if (dataListSorted[0] <= 0)
                {
                    int validDataCount = 0;
                    for (int i = 0; i <= dataSortedCount - 1; i++)
                    {
                        if (dataListSorted[i] > 0)
                        {
                            dataListSorted[validDataCount] = dataListSorted[i];
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
            }

            // Look for the minimum positive value and replace all data in dataListSorted with that value
            double minimumPositiveValue = ReplaceSortedDataWithMinimumPositiveValue(dataSortedCount, dataListSorted);

            switch (baselineNoiseOptions.BaselineNoiseMode)
            {
                case eNoiseThresholdModes.TrimmedMeanByAbundance:
                case eNoiseThresholdModes.TrimmedMeanByCount:
                    int countSummed;
                    double sum;

                    if (baselineNoiseOptions.BaselineNoiseMode == eNoiseThresholdModes.TrimmedMeanByAbundance)
                    {
                        // Average the data that has intensity values less than
                        // Minimum + baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (Maximum - Minimum)

                        double intensityThreshold = dataListSorted[0] + baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (dataListSorted[dataSortedCount - 1] - dataListSorted[0]);

                        // Initialize countSummed to dataSortedCount for now, in case all data is within the intensity threshold
                        countSummed = dataSortedCount;
                        sum = 0;
                        for (int i = 0; i <= dataSortedCount - 1; i++)
                        {
                            if (dataListSorted[i] <= intensityThreshold)
                            {
                                sum += dataListSorted[i];
                            }
                            else
                            {
                                // Update countSummed
                                countSummed = i;
                                break;
                            }
                        }

                        indexEnd = countSummed - 1;
                    }
                    else
                    {
                        // eNoiseThresholdModes.TrimmedMeanByCount
                        // Find the index of the data point at dataSortedCount * baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage and
                        // average the data from the start to that index
                        indexEnd = Convert.ToInt32(Math.Round((dataSortedCount - 1) * baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, 0));

                        countSummed = indexEnd + 1;
                        sum = 0;
                        for (int i = 0; i <= indexEnd; i++)
                            sum += dataListSorted[i];
                    }

                    if (countSummed > 0)
                    {
                        // Compute the average
                        // Note that countSummed will be used below in the variance computation
                        baselineNoiseStats.NoiseLevel = sum / Convert.ToDouble(countSummed);
                        baselineNoiseStats.PointsUsed = countSummed;

                        if (countSummed > 1)
                        {
                            // Compute the variance
                            sum = 0;
                            for (int i = 0; i <= indexEnd; i++)
                                sum += Math.Pow(dataListSorted[i] - baselineNoiseStats.NoiseLevel, 2);
                            baselineNoiseStats.NoiseStDev = Math.Sqrt(sum / Convert.ToDouble(countSummed - 1));
                        }
                        else
                        {
                            baselineNoiseStats.NoiseStDev = 0;
                        }
                    }
                    else
                    {
                        // No data to average; define the noise level to be the minimum intensity
                        baselineNoiseStats.NoiseLevel = dataListSorted[0];
                        baselineNoiseStats.NoiseStDev = 0;
                        baselineNoiseStats.PointsUsed = 1;
                    }

                    break;

                case eNoiseThresholdModes.TrimmedMedianByAbundance:
                    if (baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage >= 1)
                    {
                        indexEnd = dataSortedCount - 1;
                    }
                    else
                    {
                        // Find the median of the data that has intensity values less than
                        // Minimum + baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (Maximum - Minimum)
                        double intensityThreshold = dataListSorted[0] +
                                                    baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage *
                                                    (dataListSorted[dataSortedCount - 1] - dataListSorted[0]);

                        // Find the first point with an intensity value <= intensityThreshold
                        indexEnd = dataSortedCount - 1;
                        for (int i = 1; i <= dataSortedCount - 1; i++)
                        {
                            if (dataListSorted[i] > intensityThreshold)
                            {
                                indexEnd = i - 1;
                                break;
                            }
                        }
                    }

                    if (indexEnd % 2 == 0)
                    {
                        // Even value
                        baselineNoiseStats.NoiseLevel = dataListSorted[Convert.ToInt32(indexEnd / (double)2)];
                    }
                    else
                    {
                        // Odd value; average the values on either side of indexEnd/2
                        int i = Convert.ToInt32((indexEnd - 1) / (double)2);
                        if (i < 0)
                            i = 0;
                        double sum2 = dataListSorted[i];

                        i += 1;
                        if (i == dataSortedCount)
                            i = dataSortedCount - 1;
                        sum2 += dataListSorted[i];
                        baselineNoiseStats.NoiseLevel = sum2 / 2.0;
                    }

                    // Compute the variance
                    double varianceSum = 0;
                    for (int i = 0; i <= indexEnd; i++)
                        varianceSum += Math.Pow(dataListSorted[i] - baselineNoiseStats.NoiseLevel, 2);

                    int countSummed2 = indexEnd + 1;
                    if (countSummed2 > 0)
                    {
                        baselineNoiseStats.NoiseStDev = Math.Sqrt(varianceSum / Convert.ToDouble(countSummed2 - 1));
                    }
                    else
                    {
                        baselineNoiseStats.NoiseStDev = 0;
                    }

                    baselineNoiseStats.PointsUsed = countSummed2;
                    break;

                default:
                    // Unknown mode
                    LogErrors("clsMASICPeakFinder->ComputeTrimmedNoiseLevel",
                              "Unknown Noise Threshold Mode encountered: " + baselineNoiseOptions.BaselineNoiseMode.ToString(),
                              null, false);
                    return false;
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

        /// <summary>
        /// Computes the width of the peak (in scans) using the fwhm value in sicPeak
        /// </summary>
        /// <param name="sicPeak"></param>
        /// <param name="sicData"></param>
        /// <param name="sigmaValueForBase"></param>
        /// <returns></returns>
        private static int ComputeWidthAtBaseUsingFWHM(
            clsSICStatsPeak sicPeak,
            IList<clsSICDataPoint> sicData,
            short sigmaValueForBase)
        {
            int peakWidthFullScans;
            try
            {
                peakWidthFullScans = sicData[sicPeak.IndexBaseRight].ScanNumber - sicData[sicPeak.IndexBaseLeft].ScanNumber + 1;
                return ComputeWidthAtBaseUsingFWHM(sicPeak.FWHMScanWidth, peakWidthFullScans, sigmaValueForBase);
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        /// <summary>
        /// Computes the width of the peak (in scans) using the fwhm value
        /// </summary>
        /// <param name="sicPeakFWHMScans"></param>
        /// <param name="sicPeakWidthFullScans"></param>
        /// <param name="sigmaValueForBase"></param>
        /// <returns></returns>
        /// <remarks>Does not allow the width determined to be larger than sicPeakWidthFullScans</remarks>
        private static int ComputeWidthAtBaseUsingFWHM(
            int sicPeakFWHMScans,
            int sicPeakWidthFullScans,
            short sigmaValueForBase = 4)
        {
            int widthAtBase;
            int sigmaBasedWidth;

            if (sigmaValueForBase < 4)
                sigmaValueForBase = 4;

            if (sicPeakFWHMScans == 0)
            {
                widthAtBase = sicPeakWidthFullScans;
            }
            else
            {
                // Compute the peak width
                // Note: Sigma = fwhm / 2.35482 = fwhm / (2 * Sqrt(2 * Ln(2)))
                sigmaBasedWidth = Convert.ToInt32(sigmaValueForBase * sicPeakFWHMScans / 2.35482);

                if (sigmaBasedWidth <= 0)
                {
                    widthAtBase = sicPeakWidthFullScans;
                }
                else if (sicPeakWidthFullScans == 0)
                {
                    widthAtBase = sigmaBasedWidth;
                }
                else
                {
                    // Compare the sigma-based peak width to sicPeakWidthFullScans
                    // Assign the smaller of the two values to widthAtBase
                    widthAtBase = Math.Min(sigmaBasedWidth, sicPeakWidthFullScans);
                }
            }

            return widthAtBase;
        }

        /// <summary>
        /// Convert from peakWidthFullScans to points; estimate number of scans per point to get this
        /// </summary>
        /// <param name="peakWidthBaseScans"></param>
        /// <param name="sicPeak"></param>
        /// <param name="sicData"></param>
        /// <returns></returns>
        private int ConvertScanWidthToPoints(
            int peakWidthBaseScans,
            clsSICStatsPeak sicPeak,
            IList<clsSICDataPoint> sicData)
        {
            double scansPerPoint = ComputeAvgScanInterval(sicData, sicPeak.IndexBaseLeft, sicPeak.IndexBaseRight);
            return Convert.ToInt32(Math.Round(peakWidthBaseScans / scansPerPoint, 0));
        }

        /// <summary>
        /// Determine the minimum positive value in the list, or absoluteMinimumValue if the list is empty
        /// </summary>
        /// <param name="sicData"></param>
        /// <param name="absoluteMinimumValue"></param>
        /// <returns></returns>
        public double FindMinimumPositiveValue(IList<clsSICDataPoint> sicData, double absoluteMinimumValue)
        {
            double minimumPositiveValue = (from item in sicData where item.Intensity > 0 select item.Intensity).DefaultIfEmpty(absoluteMinimumValue).Min();
            if (minimumPositiveValue < absoluteMinimumValue)
            {
                return absoluteMinimumValue;
            }

            return minimumPositiveValue;
        }

        /// <summary>
        /// Determine the minimum positive value in the list, or absoluteMinimumValue if the list is empty
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="absoluteMinimumValue"></param>
        /// <returns></returns>
        public double FindMinimumPositiveValue(IList<double> dataList, double absoluteMinimumValue)
        {
            double minimumPositiveValue = (from item in dataList where item > 0 select item).DefaultIfEmpty(absoluteMinimumValue).Min();
            if (minimumPositiveValue < absoluteMinimumValue)
            {
                return absoluteMinimumValue;
            }

            return minimumPositiveValue;
        }

        /// <summary>
        /// Determine the minimum positive value in the list, examining the first dataCount items
        /// </summary>
        /// <param name="dataCount"></param>
        /// <param name="dataList"></param>
        /// <param name="absoluteMinimumValue"></param>
        /// <returns></returns>
        /// <remarks>
        /// Does not use dataList.Length to determine the length of the list; uses dataCount
        /// However, if dataCount is > dataList.Length, dataList.Length-1 will be used for the maximum index to examine
        /// </remarks>
        public double FindMinimumPositiveValue(int dataCount, IReadOnlyList<double> dataList, double absoluteMinimumValue)
        {
            if (dataCount > dataList.Count)
            {
                dataCount = dataList.Count;
            }

            double minimumPositiveValue = (from item in dataList.Take(dataCount) where item > 0 select item).DefaultIfEmpty(absoluteMinimumValue).Min();
            if (minimumPositiveValue < absoluteMinimumValue)
            {
                return absoluteMinimumValue;
            }

            return minimumPositiveValue;
        }

        /// <summary>
        /// Find peaks in the scan/intensity data tracked by sicData
        /// </summary>
        /// <param name="sicData"></param>
        /// <param name="peakIndexStart">Output</param>
        /// <param name="peakIndexEnd">Output</param>
        /// <param name="peakLocationIndex">Output</param>
        /// <param name="previousPeakFWHMPointRight">Output</param>
        /// <param name="nextPeakFWHMPointLeft">Output</param>
        /// <param name="shoulderCount">Output</param>
        /// <param name="smoothedYDataSubset">Output</param>
        /// <param name="simDataPresent"></param>
        /// <param name="sicPeakFinderOptions"></param>
        /// <param name="sicNoiseThresholdIntensity"></param>
        /// <param name="minimumPotentialPeakArea"></param>
        /// <param name="returnClosestPeak">
        /// When true, peakLocationIndex should be populated with the "best guess" location of the peak in the scanNumbers() and intensityData() arrays
        /// The peak closest to peakLocationIndex will be the chosen peak, even if it is not the most intense peak found
        /// </param>
        /// <returns>Returns True if a valid peak is found in intensityData(), otherwise false</returns>
        private bool FindPeaks(
            IList<clsSICDataPoint> sicData,
            ref int peakIndexStart,
            ref int peakIndexEnd,
            ref int peakLocationIndex,
            ref int previousPeakFWHMPointRight,
            ref int nextPeakFWHMPointLeft,
            ref int shoulderCount,
            out clsSmoothedYDataSubset smoothedYDataSubset,
            bool simDataPresent,
            clsSICPeakFinderOptions sicPeakFinderOptions,
            double sicNoiseThresholdIntensity,
            double minimumPotentialPeakArea,
            bool returnClosestPeak)
        {
            const int SMOOTHED_DATA_PADDING_COUNT = 2;

            bool validPeakFound;

            smoothedYDataSubset = new clsSmoothedYDataSubset();

            try
            {
                var peakDetector = new clsPeakDetection();

                var peakData = new clsPeaksContainer() { SourceDataCount = sicData.Count };

                if (peakData.SourceDataCount <= 1)
                {
                    // Only 1 or fewer points in intensityData()
                    // No point in looking for a "peak"
                    peakIndexStart = 0;
                    peakIndexEnd = 0;
                    peakLocationIndex = 0;

                    return false;
                }

                // Try to find the peak using the Peak Detector class
                // First need to populate .XData() and copy from intensityData() to .YData()
                // At the same time, find maximumIntensity and maximumPotentialPeakArea

                // The peak finder class requires Arrays of type Double
                // Copy the data from the source arrays into peakData.XData() and peakData.YData()
                peakData.XData = new double[peakData.SourceDataCount];
                peakData.YData = new double[peakData.SourceDataCount];

                var scanNumbers = (from item in sicData select item.ScanNumber).ToArray();

                double maximumIntensity = sicData[0].Intensity;
                double maximumPotentialPeakArea = 0;
                int indexMaxIntensity = 0;

                // Initialize the intensity queue
                // The queue is used to keep track of the most recent intensity values
                var intensityQueue = new Queue();

                double potentialPeakArea = 0;
                int dataPointCountAboveThreshold = 0;

                for (int i = 0; i <= peakData.SourceDataCount - 1; i++)
                {
                    peakData.XData[i] = sicData[i].ScanNumber;
                    peakData.YData[i] = sicData[i].Intensity;
                    if (peakData.YData[i] > maximumIntensity)
                    {
                        maximumIntensity = peakData.YData[i];
                        indexMaxIntensity = i;
                    }

                    if (sicData[i].Intensity >= sicNoiseThresholdIntensity)
                    {
                        // Add this intensity to potentialPeakArea
                        potentialPeakArea += sicData[i].Intensity;
                        if (intensityQueue.Count >= sicPeakFinderOptions.InitialPeakWidthScansMaximum)
                        {
                            // Decrement potentialPeakArea by the oldest item in the queue
                            potentialPeakArea -= Convert.ToDouble(intensityQueue.Dequeue());
                        }
                        // Add this intensity to the queue
                        intensityQueue.Enqueue(sicData[i].Intensity);

                        if (potentialPeakArea > maximumPotentialPeakArea)
                        {
                            maximumPotentialPeakArea = potentialPeakArea;
                        }

                        dataPointCountAboveThreshold += 1;
                    }
                }

                // Determine the initial value for .PeakWidthPointsMinimum
                // We will use maximumIntensity and minimumPeakIntensity to compute a S/N value to help pick .PeakWidthPointsMinimum

                // Old: If sicPeakFinderOptions.SICNoiseThresholdIntensity < 1 Then sicPeakFinderOptions.SICNoiseThresholdIntensity = 1
                // Old: peakAreaSignalToNoise = maximumIntensity / sicPeakFinderOptions.SICNoiseThresholdIntensity

                if (minimumPotentialPeakArea < 1)
                    minimumPotentialPeakArea = 1;
                double peakAreaSignalToNoise = maximumPotentialPeakArea / minimumPotentialPeakArea;
                if (peakAreaSignalToNoise < 1)
                    peakAreaSignalToNoise = 1;

                if (Math.Abs(sicPeakFinderOptions.ButterworthSamplingFrequency) < double.Epsilon)
                {
                    sicPeakFinderOptions.ButterworthSamplingFrequency = 0.25;
                }

                peakData.PeakWidthPointsMinimum = Convert.ToInt32(sicPeakFinderOptions.InitialPeakWidthScansScaler *
                                                                  Math.Log10(Math.Floor(peakAreaSignalToNoise)) * 10);

                // Assure that .InitialPeakWidthScansMaximum is no greater than .InitialPeakWidthScansMaximum
                // and no greater than dataPointCountAboveThreshold/2 (rounded up)
                peakData.PeakWidthPointsMinimum = Math.Min(peakData.PeakWidthPointsMinimum, sicPeakFinderOptions.InitialPeakWidthScansMaximum);
                peakData.PeakWidthPointsMinimum = Math.Min(peakData.PeakWidthPointsMinimum, Convert.ToInt32(Math.Ceiling(dataPointCountAboveThreshold / (double)2)));

                if (peakData.PeakWidthPointsMinimum > peakData.SourceDataCount * 0.8)
                {
                    peakData.PeakWidthPointsMinimum = Convert.ToInt32(Math.Floor(peakData.SourceDataCount * 0.8));
                }

                if (peakData.PeakWidthPointsMinimum < MINIMUM_PEAK_WIDTH)
                    peakData.PeakWidthPointsMinimum = MINIMUM_PEAK_WIDTH;

                // Save the original value for peakLocationIndex
                peakData.OriginalPeakLocationIndex = peakLocationIndex;
                peakData.MaxAllowedUpwardSpikeFractionMax = sicPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax;

                do
                {
                    bool testingMinimumPeakWidth;

                    if (peakData.PeakWidthPointsMinimum == MINIMUM_PEAK_WIDTH)
                    {
                        testingMinimumPeakWidth = true;
                    }
                    else
                    {
                        testingMinimumPeakWidth = false;
                    }

                    try
                    {
                        validPeakFound = FindPeaksWork(
                            peakDetector, scanNumbers, peakData,
                            simDataPresent, sicPeakFinderOptions,
                            testingMinimumPeakWidth, returnClosestPeak);
                    }
                    catch (Exception ex)
                    {
                        LogErrors("clsMASICPeakFinder->FindPeaks", "Error calling FindPeaksWork", ex, true);
                        validPeakFound = false;
                        break;
                    }

                    if (validPeakFound)
                    {

                        // For each peak, see if several zero intensity values are in a row in the raw data
                        // If found, then narrow the peak to leave just one zero intensity value
                        for (int peakIndexCompare = 0; peakIndexCompare <= peakData.Peaks.Count - 1; peakIndexCompare++)
                        {
                            var currentPeak = peakData.Peaks[peakIndexCompare];

                            while (currentPeak.LeftEdge < sicData.Count - 1 &&
                            currentPeak.LeftEdge < currentPeak.RightEdge)
                            {
                                if (Math.Abs(sicData[currentPeak.LeftEdge].Intensity) < double.Epsilon &&
                                    Math.Abs(sicData[currentPeak.LeftEdge + 1].Intensity) < double.Epsilon)
                                {
                                    currentPeak.LeftEdge += 1;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            while (currentPeak.RightEdge > 0 &&
                                currentPeak.RightEdge > currentPeak.LeftEdge)
                            {
                                if (Math.Abs(sicData[currentPeak.RightEdge].Intensity) < double.Epsilon &&
                                    Math.Abs(sicData[currentPeak.RightEdge - 1].Intensity) < double.Epsilon)
                                {
                                    currentPeak.RightEdge -= 1;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        // Update the stats for the "official" peak
                        var bestPeak = peakData.Peaks[peakData.BestPeakIndex];

                        peakLocationIndex = bestPeak.PeakLocation;
                        peakIndexStart = bestPeak.LeftEdge;
                        peakIndexEnd = bestPeak.RightEdge;

                        // Copy the smoothed Y data for the peak into smoothedYDataSubset.Data()
                        // Include some data to the left and right of the peak start and end
                        // Additionally, be sure the smoothed data includes the data around the most intense data point in intensityData
                        int smoothedYDataStartIndex = peakIndexStart - SMOOTHED_DATA_PADDING_COUNT;
                        int smoothedYDataEndIndex = peakIndexEnd + SMOOTHED_DATA_PADDING_COUNT;

                        // Make sure the maximum intensity point is included (with padding on either side)
                        if (indexMaxIntensity - SMOOTHED_DATA_PADDING_COUNT < smoothedYDataStartIndex)
                        {
                            smoothedYDataStartIndex = indexMaxIntensity - SMOOTHED_DATA_PADDING_COUNT;
                        }

                        if (indexMaxIntensity + SMOOTHED_DATA_PADDING_COUNT > smoothedYDataEndIndex)
                        {
                            smoothedYDataEndIndex = indexMaxIntensity + SMOOTHED_DATA_PADDING_COUNT;
                        }

                        // Make sure the indices aren't out of range
                        if (smoothedYDataStartIndex < 0)
                        {
                            smoothedYDataStartIndex = 0;
                        }

                        if (smoothedYDataEndIndex >= sicData.Count)
                        {
                            smoothedYDataEndIndex = sicData.Count - 1;
                        }

                        // Copy the smoothed data into smoothedYDataSubset
                        smoothedYDataSubset = new clsSmoothedYDataSubset(peakData.SmoothedYData, smoothedYDataStartIndex, smoothedYDataEndIndex);

                        // Copy the peak location info into peakDataSaved since we're going to call FindPeaksWork again and the data will get overwritten
                        var peakDataSaved = peakData.Clone(true);

                        if (peakData.PeakWidthPointsMinimum != MINIMUM_PEAK_WIDTH)
                        {
                            // Determine the number of shoulder peaks for this peak
                            // Use a minimum peak width of MINIMUM_PEAK_WIDTH and use a Max Allow Upward Spike Fraction of just 0.05 (= 5%)
                            peakData.PeakWidthPointsMinimum = MINIMUM_PEAK_WIDTH;
                            if (peakData.MaxAllowedUpwardSpikeFractionMax > 0.05)
                            {
                                peakData.MaxAllowedUpwardSpikeFractionMax = 0.05;
                            }

                            validPeakFound = FindPeaksWork(
                                peakDetector, scanNumbers, peakData,
                                simDataPresent, sicPeakFinderOptions,
                                true, returnClosestPeak);

                            if (validPeakFound)
                            {
                                shoulderCount = 0;

                                foreach (var peakItem in peakData.Peaks)
                                {
                                    if (peakItem.PeakLocation >= peakIndexStart && peakItem.PeakLocation <= peakIndexEnd)
                                    {
                                        // The peak at i has a peak center between the "official" peak's boundaries
                                        // Make sure it's not the same peak as the "official" peak
                                        if (peakItem.PeakLocation != peakLocationIndex)
                                        {
                                            // Now see if the comparison peak's intensity is at least .IntensityThresholdFractionMax of the intensity of the "official" peak
                                            if (sicData[peakItem.PeakLocation].Intensity >= sicPeakFinderOptions.IntensityThresholdFractionMax * sicData[peakLocationIndex].Intensity)
                                            {
                                                // Yes, this is a shoulder peak
                                                shoulderCount += 1;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            shoulderCount = 0;
                        }

                        // Make sure peakLocationIndex really is the point with the highest intensity (in the smoothed data)
                        maximumIntensity = peakData.SmoothedYData[peakLocationIndex];
                        for (int i = peakIndexStart; i <= peakIndexEnd; i++)
                        {
                            if (peakData.SmoothedYData[i] > maximumIntensity)
                            {
                                // A more intense data point was found; update peakLocationIndex
                                maximumIntensity = peakData.SmoothedYData[i];
                                peakLocationIndex = i;
                            }
                        }

                        int comparisonPeakEdgeIndex;
                        double targetIntensity;
                        int dataIndex;

                        // Populate previousPeakFWHMPointRight and nextPeakFWHMPointLeft
                        double adjacentPeakIntensityThreshold = sicData[peakLocationIndex].Intensity / 3;

                        // Search through peakDataSaved to find the closest peak (with a significant intensity) to the left of this peak
                        // Note that the peaks in peakDataSaved are not necessarily ordered by increasing index,
                        // thus the need for an exhaustive search

                        int smallestIndexDifference = sicData.Count + 1;
                        for (int peakIndexCompare = 0; peakIndexCompare <= peakDataSaved.Peaks.Count - 1; peakIndexCompare++)
                        {
                            var comparisonPeak = peakDataSaved.Peaks[peakIndexCompare];

                            if (peakIndexCompare != peakDataSaved.BestPeakIndex &&
                                comparisonPeak.PeakLocation <= peakIndexStart)
                            {
                                // The peak is before peakIndexStart; is its intensity large enough?
                                if (sicData[comparisonPeak.PeakLocation].Intensity >= adjacentPeakIntensityThreshold)
                                {
                                    // Yes, the intensity is large enough

                                    // Initialize comparisonPeakEdgeIndex to the right edge of the adjacent peak
                                    comparisonPeakEdgeIndex = comparisonPeak.RightEdge;

                                    // Find the first point in the adjacent peak that is at least 50% of the maximum in the adjacent peak
                                    // Store that point in comparisonPeakEdgeIndex
                                    targetIntensity = sicData[comparisonPeak.PeakLocation].Intensity / 2;
                                    for (dataIndex = comparisonPeakEdgeIndex; dataIndex >= comparisonPeak.PeakLocation; dataIndex--)
                                    {
                                        if (sicData[dataIndex].Intensity >= targetIntensity)
                                        {
                                            comparisonPeakEdgeIndex = dataIndex;
                                            break;
                                        }
                                    }

                                    // Assure that comparisonPeakEdgeIndex is less than peakIndexStart
                                    if (comparisonPeakEdgeIndex >= peakIndexStart)
                                    {
                                        comparisonPeakEdgeIndex = peakIndexStart - 1;
                                        if (comparisonPeakEdgeIndex < 0)
                                            comparisonPeakEdgeIndex = 0;
                                    }

                                    // Possibly update previousPeakFWHMPointRight
                                    if (peakIndexStart - comparisonPeakEdgeIndex <= smallestIndexDifference)
                                    {
                                        previousPeakFWHMPointRight = comparisonPeakEdgeIndex;
                                        smallestIndexDifference = peakIndexStart - comparisonPeakEdgeIndex;
                                    }
                                }
                            }
                        }

                        // Search through peakDataSaved to find the closest peak to the right of this peak
                        smallestIndexDifference = sicData.Count + 1;
                        for (int peakIndexCompare = peakDataSaved.Peaks.Count - 1; peakIndexCompare >= 0; peakIndexCompare--)
                        {
                            var comparisonPeak = peakDataSaved.Peaks[peakIndexCompare];
                            if (peakIndexCompare != peakDataSaved.BestPeakIndex && comparisonPeak.PeakLocation >= peakIndexEnd)
                            {

                                // The peak is after peakIndexEnd; is its intensity large enough?
                                if (sicData[comparisonPeak.PeakLocation].Intensity >= adjacentPeakIntensityThreshold)
                                {
                                    // Yes, the intensity is large enough

                                    // Initialize comparisonPeakEdgeIndex to the left edge of the adjacent peak
                                    comparisonPeakEdgeIndex = comparisonPeak.LeftEdge;

                                    // Find the first point in the adjacent peak that is at least 50% of the maximum in the adjacent peak
                                    // Store that point in comparisonPeakEdgeIndex
                                    targetIntensity = sicData[comparisonPeak.PeakLocation].Intensity / 2;
                                    for (dataIndex = comparisonPeakEdgeIndex; dataIndex <= comparisonPeak.PeakLocation; dataIndex++)
                                    {
                                        if (sicData[dataIndex].Intensity >= targetIntensity)
                                        {
                                            comparisonPeakEdgeIndex = dataIndex;
                                            break;
                                        }
                                    }

                                    // Assure that comparisonPeakEdgeIndex is greater than peakIndexEnd
                                    if (peakIndexEnd >= comparisonPeakEdgeIndex)
                                    {
                                        comparisonPeakEdgeIndex = peakIndexEnd + 1;
                                        if (comparisonPeakEdgeIndex >= sicData.Count)
                                            comparisonPeakEdgeIndex = sicData.Count - 1;
                                    }

                                    // Possibly update nextPeakFWHMPointLeft
                                    if (comparisonPeakEdgeIndex - peakIndexEnd <= smallestIndexDifference)
                                    {
                                        nextPeakFWHMPointLeft = comparisonPeakEdgeIndex;
                                        smallestIndexDifference = comparisonPeakEdgeIndex - peakIndexEnd;
                                    }
                                }
                            }
                        }
                    }
                    // No peaks or no peaks containing .OriginalPeakLocationIndex
                    // If peakData.PeakWidthPointsMinimum is greater than 3 and testingMinimumPeakWidth = False, then decrement it by 50%
                    else if (peakData.PeakWidthPointsMinimum > MINIMUM_PEAK_WIDTH && !testingMinimumPeakWidth)
                    {
                        peakData.PeakWidthPointsMinimum = Convert.ToInt32(Math.Floor(peakData.PeakWidthPointsMinimum / (double)2));
                        if (peakData.PeakWidthPointsMinimum < MINIMUM_PEAK_WIDTH)
                            peakData.PeakWidthPointsMinimum = MINIMUM_PEAK_WIDTH;
                    }
                    else
                    {
                        peakLocationIndex = peakData.OriginalPeakLocationIndex;
                        peakIndexStart = peakData.OriginalPeakLocationIndex;
                        peakIndexEnd = peakData.OriginalPeakLocationIndex;
                        previousPeakFWHMPointRight = peakIndexStart;
                        nextPeakFWHMPointLeft = peakIndexEnd;
                        validPeakFound = true;
                    }
                }
                while (!validPeakFound);
            }
            catch (Exception ex)
            {
                LogErrors("clsMASICPeakFinder->FindPeaks", "Error in FindPeaks", ex, false);
                validPeakFound = false;
            }

            return validPeakFound;
        }

        /// <summary>
        /// Find peaks
        /// </summary>
        /// <param name="peakDetector">peak detector object</param>
        /// <param name="scanNumbers">Scan numbers of the data tracked by peaksContainer</param>
        /// <param name="peaksContainer">Container object with XData, YData, SmoothedData, found Peaks, and various tracking properties</param>
        /// <param name="simDataPresent">
        /// Set to true if processing selected ion monitoring data (or if there are huge gaps in the scan numbers).
        /// When true, and if sicPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData is true, uses a larger Butterworth filter sampling frequency
        /// </param>
        /// <param name="sicPeakFinderOptions">Peak finder options</param>
        /// <param name="testingMinimumPeakWidth">When true, assure that at least one peak is returned</param>
        /// <param name="returnClosestPeak">
        /// When true, a valid peak is one that contains peaksContainer.OriginalPeakLocationIndex
        /// When false, stores the index of the most intense peak in peaksContainer.BestPeakIndex
        /// </param>
        /// <returns>True if a valid peak is found; otherwise, returns false</returns>
        /// <remarks>All of the identified peaks are returned in peaksContainer.Peaks(), regardless of whether they are valid or not</remarks>
        private bool FindPeaksWork(
            clsPeakDetection peakDetector,
            IList<int> scanNumbers,
            clsPeaksContainer peaksContainer,
            bool simDataPresent,
            clsSICPeakFinderOptions sicPeakFinderOptions,
            bool testingMinimumPeakWidth,
            bool returnClosestPeak)
        {
            string errorMessage = string.Empty;

            // Smooth the Y data, and store in peaksContainer.SmoothedYData
            // Note that if using a Butterworth filter, then we increase peaksContainer.PeakWidthPointsMinimum if too small, compared to 1/SamplingFrequency
            int peakWidthPointsMinimum = peaksContainer.PeakWidthPointsMinimum;
            bool dataIsSmoothed = FindPeaksWorkSmoothData(
                peaksContainer, simDataPresent,
                sicPeakFinderOptions, ref peakWidthPointsMinimum,
                ref errorMessage);
            peaksContainer.PeakWidthPointsMinimum = peakWidthPointsMinimum;

            if (sicPeakFinderOptions.FindPeaksOnSmoothedData && dataIsSmoothed)
            {
                peaksContainer.Peaks = peakDetector.DetectPeaks(
                    peaksContainer.XData,
                    peaksContainer.SmoothedYData,
                    sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum,
                    peaksContainer.PeakWidthPointsMinimum,
                    Convert.ToInt32(sicPeakFinderOptions.IntensityThresholdFractionMax * 100), 2, true, true);
            }

            // usedSmoothedDataForPeakDetection = True
            else
            {
                // Look for the peaks, using peaksContainer.PeakWidthPointsMinimum as the minimum peak width
                peaksContainer.Peaks = peakDetector.DetectPeaks(
                    peaksContainer.XData,
                    peaksContainer.YData,
                    sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum,
                    peaksContainer.PeakWidthPointsMinimum,
                    Convert.ToInt32(sicPeakFinderOptions.IntensityThresholdFractionMax * 100), 2, true, true);
                // usedSmoothedDataForPeakDetection = False
            }

            if (peaksContainer.Peaks.Count == -1)
            {
                // Fatal error occurred while finding peaks
                return false;
            }

            double peakMaximum = peaksContainer.YData.Max();

            if (testingMinimumPeakWidth)
            {
                if (peaksContainer.Peaks.Count <= 0)
                {
                    // No peaks were found; create a new peak list using the original peak location index as the peak center
                    var newPeak = new clsPeakInfo(peaksContainer.OriginalPeakLocationIndex)
                    {
                        LeftEdge = peaksContainer.OriginalPeakLocationIndex,
                        RightEdge = peaksContainer.OriginalPeakLocationIndex
                    };

                    peaksContainer.Peaks.Add(newPeak);
                }
                else if (returnClosestPeak)
                {
                    // Make sure one of the peaks is within 1 of the original peak location
                    bool success = false;
                    for (int foundPeakIndex = 0; foundPeakIndex <= peaksContainer.Peaks.Count - 1; foundPeakIndex++)
                    {
                        if (Math.Abs(peaksContainer.Peaks[foundPeakIndex].PeakLocation - peaksContainer.OriginalPeakLocationIndex) <= 1)
                        {
                            success = true;
                            break;
                        }
                    }

                    if (!success)
                    {
                        // No match was found; add a new peak at peaksContainer.OriginalPeakLocationIndex

                        var newPeak = new clsPeakInfo(peaksContainer.OriginalPeakLocationIndex)
                        {
                            LeftEdge = peaksContainer.OriginalPeakLocationIndex,
                            RightEdge = peaksContainer.OriginalPeakLocationIndex,
                            PeakArea = peaksContainer.YData[peaksContainer.OriginalPeakLocationIndex]
                        };

                        peaksContainer.Peaks.Add(newPeak);
                    }
                }
            }

            if (peaksContainer.Peaks.Count <= 0)
            {
                // No peaks were found
                return false;
            }

            foreach (var peakItem in peaksContainer.Peaks)
            {
                peakItem.PeakIsValid = false;

                // Find the center and boundaries of this peak

                // Copy from the PeakEdges arrays to the working variables
                int peakLocationIndex = peakItem.PeakLocation;
                int peakIndexStart = peakItem.LeftEdge;
                int peakIndexEnd = peakItem.RightEdge;

                // Make sure peakLocationIndex is between peakIndexStart and peakIndexEnd
                if (peakIndexStart > peakLocationIndex)
                {
                    LogErrors("clsMasicPeakFinder->FindPeaksWork",
                              "peakIndexStart is > peakLocationIndex; this is probably a programming error", null, false);
                    peakIndexStart = peakLocationIndex;
                }

                if (peakIndexEnd < peakLocationIndex)
                {
                    LogErrors("clsMasicPeakFinder->FindPeaksWork",
                              "peakIndexEnd is < peakLocationIndex; this is probably a programming error", null, false);
                    peakIndexEnd = peakLocationIndex;
                }

                // See if the peak boundaries (left and right edges) need to be narrowed or expanded
                // Do this by stepping left or right while the intensity is decreasing.  If an increase is found, but the
                // next point after the increasing point is less than the current point, then possibly keep stepping; the
                // test for whether to keep stepping is that the next point away from the increasing point must be less
                // than the current point.  If this is the case, replace the increasing point with the average of the
                // current point and the point two points away
                //
                // Use smoothed data for this step
                // Determine the smoothing window based on peaksContainer.PeakWidthPointsMinimum
                // If peaksContainer.PeakWidthPointsMinimum <= 4 then do not filter

                if (!dataIsSmoothed)
                {
                    // Need to smooth the data now
                    peakWidthPointsMinimum = peaksContainer.PeakWidthPointsMinimum;
                    dataIsSmoothed = FindPeaksWorkSmoothData(
                        peaksContainer, simDataPresent,
                        sicPeakFinderOptions, ref peakWidthPointsMinimum, ref errorMessage);
                    peaksContainer.PeakWidthPointsMinimum = peakWidthPointsMinimum;
                }

                // First see if we need to narrow the peak by looking for decreasing intensities moving toward the peak center
                // We'll use the unsmoothed data for this
                while (peakIndexStart < peakLocationIndex - 1)
                {
                    if (peaksContainer.YData[peakIndexStart] > peaksContainer.YData[peakIndexStart + 1])
                        // || (usedSmoothedDataForPeakDetection && peaksContainer.SmoothedYData[peakIndexStart] < 0))
                    {
                        peakIndexStart += 1;
                    }
                    else
                    {
                        break;
                    }
                }

                while (peakIndexEnd > peakLocationIndex + 1)
                {
                    if (peaksContainer.YData[peakIndexEnd - 1] < peaksContainer.YData[peakIndexEnd])
                        // || (usedSmoothedDataForPeakDetection && peaksContainer.SmoothedYData[peakIndexEnd] < 0))
                    {
                        peakIndexEnd -= 1;
                    }
                    else
                    {
                        break;
                    }
                }

                // Now see if we need to expand the peak by looking for decreasing intensities moving away from the peak center,
                // but allowing for small increases
                // We'll use the smoothed data for this; if we encounter negative values in the smoothed data, we'll keep going until we reach the low point since huge peaks can cause some odd behavior with the Butterworth filter
                // Keep track of the number of times we step over an increased value
                int stepOverIncreaseCount = 0;
                while (peakIndexStart > 0)
                {
                    // currentSlope = peakDetector.ComputeSlope(peaksContainer.XData, peaksContainer.SmoothedYData, peakIndexStart, peakLocationIndex);

                    //if (currentSlope > 0 &&
                    //    peakLocationIndex - peakIndexStart > 3 &&
                    //    peaksContainer.SmoothedYData[peakIndexStart - 1] < Math.Max(sicPeakFinderOptions.IntensityThresholdFractionMax * peakMaximum, sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum))
                    //{
                    //    // We reached a low intensity data point and we're going downhill (i.e. the slope from this point to peakLocationIndex is positive)
                    //    // Step once more and stop
                    //    peakIndexStart -= 1;
                    //    break;
                    //}

                    if (peaksContainer.SmoothedYData[peakIndexStart - 1] < peaksContainer.SmoothedYData[peakIndexStart])
                    {
                        // The adjacent point is lower than the current point
                        peakIndexStart -= 1;
                    }
                    else if (Math.Abs(peaksContainer.SmoothedYData[peakIndexStart - 1] -
                                      peaksContainer.SmoothedYData[peakIndexStart]) < double.Epsilon)
                    {
                        // The adjacent point is equal to the current point
                        peakIndexStart -= 1;
                    }
                    // The next point to the left is not lower; what about the point after it?
                    else if (peakIndexStart > 1)
                    {
                        if (peaksContainer.SmoothedYData[peakIndexStart - 2] <= peaksContainer.SmoothedYData[peakIndexStart])
                        {
                            // Only allow ignoring an upward spike if the delta from this point to the next is <= .MaxAllowedUpwardSpikeFractionMax of peakMaximum
                            if (peaksContainer.SmoothedYData[peakIndexStart - 1] - peaksContainer.SmoothedYData[peakIndexStart] >
                                peaksContainer.MaxAllowedUpwardSpikeFractionMax * peakMaximum)
                            {
                                break;
                            }

                            if (dataIsSmoothed)
                            {
                                // Only ignore an upward spike twice if the data is smoothed
                                if (stepOverIncreaseCount >= 2)
                                    break;
                            }

                            peakIndexStart -= 1;

                            stepOverIncreaseCount += 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                stepOverIncreaseCount = 0;
                while (peakIndexEnd < peaksContainer.SourceDataCount - 1)
                {
                    // currentSlope = peakDetector.ComputeSlope(peaksContainer.XData, peaksContainer.SmoothedYData, peakLocationIndex, peakIndexEnd);

                    //if (currentSlope < 0 &&
                    //    peakIndexEnd - peakLocationIndex > 3 &&
                    //    peaksContainer.SmoothedYData(peakIndexEnd + 1) < Math.Max(sicPeakFinderOptions.IntensityThresholdFractionMax * peakMaximum, sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum))
                    //{
                    //    // We reached a low intensity data point and we're going downhill (i.e. the slope from peakLocationIndex to this point is negative)
                    //    peakIndexEnd += 1;
                    //    break;
                    //}

                    if (peaksContainer.SmoothedYData[peakIndexEnd + 1] < peaksContainer.SmoothedYData[peakIndexEnd])
                    {
                        // The adjacent point is lower than the current point
                        peakIndexEnd += 1;
                    }
                    else if (Math.Abs(peaksContainer.SmoothedYData[peakIndexEnd + 1] -
                        peaksContainer.SmoothedYData[peakIndexEnd]) < double.Epsilon)
                    {
                        // The adjacent point is equal to the current point
                        peakIndexEnd += 1;
                    }
                    // The next point to the right is not lower; what about the point after it?
                    else if (peakIndexEnd < peaksContainer.SourceDataCount - 2)
                    {
                        if (peaksContainer.SmoothedYData[peakIndexEnd + 2] <= peaksContainer.SmoothedYData[peakIndexEnd])
                        {
                            // Only allow ignoring an upward spike if the delta from this point to the next is <= .MaxAllowedUpwardSpikeFractionMax of peakMaximum
                            if (peaksContainer.SmoothedYData[peakIndexEnd + 1] - peaksContainer.SmoothedYData[peakIndexEnd] >
                                peaksContainer.MaxAllowedUpwardSpikeFractionMax * peakMaximum)
                            {
                                break;
                            }

                            if (dataIsSmoothed)
                            {
                                // Only ignore an upward spike twice if the data is smoothed
                                if (stepOverIncreaseCount >= 2)
                                    break;
                            }

                            peakIndexEnd += 1;

                            stepOverIncreaseCount += 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                peakItem.PeakIsValid = true;
                if (returnClosestPeak)
                {
                    // If peaksContainer.OriginalPeakLocationIndex is not between peakIndexStart and peakIndexEnd, then check
                    // if the scan number for peaksContainer.OriginalPeakLocationIndex is within .MaxDistanceScansNoOverlap scans of
                    // either of the peak edges; if not, then mark the peak as invalid since it does not contain the
                    // scan for the parent ion
                    if (peaksContainer.OriginalPeakLocationIndex < peakIndexStart)
                    {
                        if (Math.Abs(scanNumbers[peaksContainer.OriginalPeakLocationIndex] -
                                     scanNumbers[peakIndexStart]) > sicPeakFinderOptions.MaxDistanceScansNoOverlap)
                        {
                            peakItem.PeakIsValid = false;
                        }
                    }
                    else if (peaksContainer.OriginalPeakLocationIndex > peakIndexEnd)
                    {
                        if (Math.Abs(scanNumbers[peaksContainer.OriginalPeakLocationIndex] -
                                     scanNumbers[peakIndexEnd]) > sicPeakFinderOptions.MaxDistanceScansNoOverlap)
                        {
                            peakItem.PeakIsValid = false;
                        }
                    }
                }

                // Copy back from the working variables to the PeakEdges arrays
                peakItem.PeakLocation = peakLocationIndex;
                peakItem.LeftEdge = peakIndexStart;
                peakItem.RightEdge = peakIndexEnd;
            }

            // Find the peak with the largest area that has peaksContainer.PeakIsValid = True
            peaksContainer.BestPeakIndex = -1;
            peaksContainer.BestPeakArea = double.MinValue;
            for (int foundPeakIndex = 0; foundPeakIndex <= peaksContainer.Peaks.Count - 1; foundPeakIndex++)
            {
                var currentPeak = peaksContainer.Peaks[foundPeakIndex];

                if (currentPeak.PeakIsValid)
                {
                    if (currentPeak.PeakArea > peaksContainer.BestPeakArea)
                    {
                        peaksContainer.BestPeakIndex = foundPeakIndex;
                        peaksContainer.BestPeakArea = Math.Min(currentPeak.PeakArea, double.MaxValue);
                    }
                }
            }

            if (peaksContainer.BestPeakIndex >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool FindPeaksWorkSmoothData(
            clsPeaksContainer peaksContainer,
            bool simDataPresent,
            clsSICPeakFinderOptions sicPeakFinderOptions,
            ref int peakWidthPointsMinimum,
            ref string errorMessage)
        {
            // Returns True if the data was smoothed; false if not or an error
            // The smoothed data is returned in peakData.SmoothedYData

            int filterThirdWidth;
            bool success;

            double butterWorthFrequency;

            int peakWidthPointsCompare;

            var filter = new DataFilter.DataFilter();

            peaksContainer.SmoothedYData = new double[peaksContainer.SourceDataCount];

            if (peakWidthPointsMinimum > 4 && (sicPeakFinderOptions.UseSavitzkyGolaySmooth || sicPeakFinderOptions.UseButterworthSmooth) ||
                sicPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth)
            {
                peaksContainer.YData.CopyTo(peaksContainer.SmoothedYData, 0);
                if (sicPeakFinderOptions.UseButterworthSmooth)
                {
                    // Filter the data with a Butterworth filter (.UseButterworthSmooth takes precedence over .UseSavitzkyGolaySmooth)
                    if (simDataPresent && sicPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData)
                    {
                        butterWorthFrequency = sicPeakFinderOptions.ButterworthSamplingFrequency * 2;
                    }
                    else
                    {
                        butterWorthFrequency = sicPeakFinderOptions.ButterworthSamplingFrequency;
                    }

                    success = filter.ButterworthFilter(
                        peaksContainer.SmoothedYData, 0,
                        peaksContainer.SourceDataCount - 1, butterWorthFrequency);
                    if (!success)
                    {
                        LogErrors("clsMasicPeakFinder->FindPeaksWorkSmoothData",
                                  "Error with the Butterworth filter" + errorMessage, null, false);
                        return false;
                    }
                    else
                    {
                        // Data was smoothed
                        // Validate that peakWidthPointsMinimum is large enough
                        if (butterWorthFrequency > 0)
                        {
                            peakWidthPointsCompare = Convert.ToInt32(Math.Round(1 / butterWorthFrequency, 0));
                            if (peakWidthPointsMinimum < peakWidthPointsCompare)
                            {
                                peakWidthPointsMinimum = peakWidthPointsCompare;
                            }
                        }

                        return true;
                    }
                }
                else
                {
                    // Filter the data with a Savitzky Golay filter
                    filterThirdWidth = Convert.ToInt32(Math.Floor(peaksContainer.PeakWidthPointsMinimum / (double)3));
                    if (filterThirdWidth > 3)
                        filterThirdWidth = 3;

                    // Make sure filterThirdWidth is Odd
                    if (filterThirdWidth % 2 == 0)
                    {
                        filterThirdWidth -= 1;
                    }

                    // Note that the SavitzkyGolayFilter doesn't work right for PolynomialDegree values greater than 0
                    // Also note that a PolynomialDegree value of 0 results in the equivalent of a moving average filter
                    success = filter.SavitzkyGolayFilter(
                        peaksContainer.SmoothedYData, 0,
                        peaksContainer.SmoothedYData.Length - 1,
                        filterThirdWidth, filterThirdWidth,
                        sicPeakFinderOptions.SavitzkyGolayFilterOrder, out errorMessage, true);

                    if (!success)
                    {
                        LogErrors("clsMasicPeakFinder->FindPeaksWorkSmoothData",
                                  "Error with the Savitzky-Golay filter: " + errorMessage, null, false);
                        return false;
                    }
                    else
                    {
                        // Data was smoothed
                        return true;
                    }
                }
            }
            else
            {
                // Do not filter
                peaksContainer.YData.CopyTo(peaksContainer.SmoothedYData, 0);
                return false;
            }
        }

        public void FindPotentialPeakArea(
            int dataCount,
            IReadOnlyList<double> sicIntensities,
            out clsSICPotentialAreaStats potentialAreaStats,
            clsSICPeakFinderOptions sicPeakFinderOptions)
        {
            var sicData = new List<clsSICDataPoint>();

            for (int index = 0; index <= dataCount - 1; index++)
                sicData.Add(new clsSICDataPoint(0, sicIntensities[index], 0));

            FindPotentialPeakArea((IList<clsSICDataPoint>)sicData, out potentialAreaStats, sicPeakFinderOptions);
        }

        public void FindPotentialPeakArea(
            IList<clsSICDataPoint> sicData,
            out clsSICPotentialAreaStats potentialAreaStats,
            clsSICPeakFinderOptions sicPeakFinderOptions)
        {
            // This function computes the potential peak area for a given SIC
            // and stores in potentialAreaStats.MinimumPotentialPeakArea
            // However, the summed intensity is not used if the number of points >= .SICBaselineNoiseOptions.MinimumBaselineNoiseLevel is less than Minimum_Peak_Width

            // Note: You cannot use SICData.Length to determine the length of the array; use dataCount

            double minimumPositiveValue;
            double intensityToUse;

            double oldestIntensity;
            double potentialPeakArea, minimumPotentialPeakArea;
            int peakCountBasisForMinimumPotentialArea;
            int validPeakCount;

            // The queue is used to keep track of the most recent intensity values
            var intensityQueue = new Queue();
            minimumPotentialPeakArea = double.MaxValue;
            peakCountBasisForMinimumPotentialArea = 0;
            if (sicData.Count > 0)
            {
                intensityQueue.Clear();
                potentialPeakArea = 0;
                validPeakCount = 0;

                // Find the minimum intensity in SICData()
                minimumPositiveValue = FindMinimumPositiveValue(sicData, 1);

                for (int i = 0; i <= sicData.Count - 1; i++)
                {

                    // If this data point is > .MinimumBaselineNoiseLevel, then add this intensity to potentialPeakArea
                    // and increment validPeakCount
                    intensityToUse = Math.Max(minimumPositiveValue, sicData[i].Intensity);
                    if (intensityToUse >= sicPeakFinderOptions.SICBaselineNoiseOptions.MinimumBaselineNoiseLevel)
                    {
                        potentialPeakArea += intensityToUse;
                        validPeakCount += 1;
                    }

                    if (intensityQueue.Count >= sicPeakFinderOptions.InitialPeakWidthScansMaximum)
                    {
                        // Decrement potentialPeakArea by the oldest item in the queue
                        // If that item is >= .MinimumBaselineNoiseLevel, then decrement validPeakCount too
                        oldestIntensity = Convert.ToDouble(intensityQueue.Dequeue());

                        if (oldestIntensity >= sicPeakFinderOptions.SICBaselineNoiseOptions.MinimumBaselineNoiseLevel && oldestIntensity > 0)
                        {
                            potentialPeakArea -= oldestIntensity;
                            validPeakCount -= 1;
                        }
                    }
                    // Add this intensity to the queue
                    intensityQueue.Enqueue(intensityToUse);

                    if (potentialPeakArea > 0 && validPeakCount >= MINIMUM_PEAK_WIDTH)
                    {
                        if (validPeakCount > peakCountBasisForMinimumPotentialArea)
                        {
                            // The non valid peak count value is larger than the one associated with the current
                            // minimum potential peak area; update the minimum peak area to potentialPeakArea
                            minimumPotentialPeakArea = potentialPeakArea;
                            peakCountBasisForMinimumPotentialArea = validPeakCount;
                        }
                        else if (potentialPeakArea < minimumPotentialPeakArea &&
                            validPeakCount == peakCountBasisForMinimumPotentialArea)
                        {
                            minimumPotentialPeakArea = potentialPeakArea;
                        }
                    }
                }
            }

            if (minimumPotentialPeakArea >= double.MaxValue)
            {
                minimumPotentialPeakArea = 1;
            }

            potentialAreaStats = new clsSICPotentialAreaStats()
            {
                MinimumPotentialPeakArea = minimumPotentialPeakArea,
                PeakCountBasisForMinimumPotentialArea = peakCountBasisForMinimumPotentialArea
            };
        }

        /// <summary>
        /// Find SIC Peak and Area
        /// </summary>
        /// <param name="dataCount"></param>
        /// <param name="sicScanNumbers"></param>
        /// <param name="sicIntensities"></param>
        /// <param name="potentialAreaStatsForPeak"></param>
        /// <param name="sicPeak"></param>
        /// <param name="smoothedYDataSubset"></param>
        /// <param name="sicPeakFinderOptions"></param>
        /// <param name="potentialAreaStatsForRegion"></param>
        /// <param name="returnClosestPeak"></param>
        /// <param name="simDataPresent">True if Select Ion Monitoring data is present</param>
        /// <param name="recomputeNoiseLevel"></param>
        [Obsolete("Use the version that takes a List(Of clsSICDataPoint")]
        public bool FindSICPeakAndArea(
            int dataCount,
            int[] sicScanNumbers,
            double[] sicIntensities,
            out clsSICPotentialAreaStats potentialAreaStatsForPeak,
            clsSICStatsPeak sicPeak,
            out clsSmoothedYDataSubset smoothedYDataSubset,
            clsSICPeakFinderOptions sicPeakFinderOptions,
            clsSICPotentialAreaStats potentialAreaStatsForRegion,
            bool returnClosestPeak,
            bool simDataPresent,
            bool recomputeNoiseLevel)
        {
            var sicData = new List<clsSICDataPoint>();

            for (int index = 0; index <= dataCount - 1; index++)
                sicData.Add(new clsSICDataPoint(sicScanNumbers[index], sicIntensities[index], 0));

            return FindSICPeakAndArea(sicData,
                                      out potentialAreaStatsForPeak, sicPeak,
                                      out smoothedYDataSubset, sicPeakFinderOptions,
                                      potentialAreaStatsForRegion,
                                      returnClosestPeak, simDataPresent, recomputeNoiseLevel);
        }

        /// <summary>
        /// Find SIC Peak and Area
        /// </summary>
        /// <param name="sicData">Selected Ion Chromatogram data (scan, intensity, mass)</param>
        /// <param name="potentialAreaStatsForPeak">Output: potential area stats for the identified peak</param>
        /// <param name="sicPeak">Output: identified Peak</param>
        /// <param name="smoothedYDataSubset"></param>
        /// <param name="sicPeakFinderOptions"></param>
        /// <param name="potentialAreaStatsForRegion"></param>
        /// <param name="returnClosestPeak"></param>
        /// <param name="simDataPresent">True if Select Ion Monitoring data is present</param>
        /// <param name="recomputeNoiseLevel"></param>
        /// <returns></returns>
        public bool FindSICPeakAndArea(
            List<clsSICDataPoint> sicData,
            out clsSICPotentialAreaStats potentialAreaStatsForPeak,
            clsSICStatsPeak sicPeak,
            out clsSmoothedYDataSubset smoothedYDataSubset,
            clsSICPeakFinderOptions sicPeakFinderOptions,
            clsSICPotentialAreaStats potentialAreaStatsForRegion,
            bool returnClosestPeak,
            bool simDataPresent,
            bool recomputeNoiseLevel)
        {
            // Note: The calling function should populate sicPeak.IndexObserved with the index in SICData() that the
            // parent ion m/z was actually observed; this will be used as the default peak location if a peak cannot be found

            // Set simDataPresent to True when there are large gaps in the survey scan numbers

            int dataIndex;
            double intensityCompare;

            bool success;

            potentialAreaStatsForPeak = new clsSICPotentialAreaStats();
            smoothedYDataSubset = new clsSmoothedYDataSubset();

            try
            {
                // Compute the potential peak area for this SIC
                FindPotentialPeakArea((IList<clsSICDataPoint>)sicData, out potentialAreaStatsForPeak, sicPeakFinderOptions);

                // See if the potential peak area for this SIC is lower than the values for the Region
                // If so, then update the region values with this peak's values
                if (potentialAreaStatsForPeak.MinimumPotentialPeakArea > 1 && potentialAreaStatsForPeak.PeakCountBasisForMinimumPotentialArea >= MINIMUM_PEAK_WIDTH)
                {
                    if (potentialAreaStatsForPeak.PeakCountBasisForMinimumPotentialArea > potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea)
                    {
                        potentialAreaStatsForRegion.MinimumPotentialPeakArea = potentialAreaStatsForPeak.MinimumPotentialPeakArea;
                        potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = potentialAreaStatsForPeak.PeakCountBasisForMinimumPotentialArea;
                    }
                    else if (potentialAreaStatsForPeak.MinimumPotentialPeakArea < potentialAreaStatsForRegion.MinimumPotentialPeakArea &&
                             potentialAreaStatsForPeak.PeakCountBasisForMinimumPotentialArea >= potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea)
                    {
                        potentialAreaStatsForRegion.MinimumPotentialPeakArea = potentialAreaStatsForPeak.MinimumPotentialPeakArea;
                        potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = potentialAreaStatsForPeak.PeakCountBasisForMinimumPotentialArea;
                    }
                }

                if (sicData == null || sicData.Count == 0)
                {
                    // Either .SICData is nothing or no SIC data exists
                    // Cannot find peaks for this parent ion

                    sicPeak.IndexObserved = 0;
                    sicPeak.IndexBaseLeft = sicPeak.IndexObserved;
                    sicPeak.IndexBaseRight = sicPeak.IndexObserved;
                    sicPeak.IndexMax = sicPeak.IndexObserved;
                    sicPeak.ParentIonIntensity = 0;
                    sicPeak.PreviousPeakFWHMPointRight = 0;
                    sicPeak.NextPeakFWHMPointLeft = 0;

                    smoothedYDataSubset = new clsSmoothedYDataSubset();
                }
                else
                {
                    // Initialize the following to the entire range of the SICData
                    sicPeak.IndexBaseLeft = 0;
                    sicPeak.IndexBaseRight = sicData.Count - 1;

                    // Initialize .IndexMax to .IndexObserved (which should have been defined by the calling function)
                    sicPeak.IndexMax = sicPeak.IndexObserved;
                    if (sicPeak.IndexMax < 0 || sicPeak.IndexMax >= sicData.Count)
                    {
                        LogErrors("clsMasicPeakFinder->FindSICPeakAndArea", "Unexpected .IndexMax value", null, false);
                        sicPeak.IndexMax = 0;
                    }

                    sicPeak.PreviousPeakFWHMPointRight = sicPeak.IndexBaseLeft;
                    sicPeak.NextPeakFWHMPointLeft = sicPeak.IndexBaseRight;

                    if (recomputeNoiseLevel)
                    {
                        var intensities = (from item in sicData select item.Intensity).ToArray();

                        // Compute the Noise Threshold for this SIC
                        // This value is first computed using all data in the SIC; it is later updated
                        // to be the minimum value of the average of the data to the immediate left and
                        // immediate right of the peak identified in the SIC
                        success = ComputeNoiseLevelForSICData(
                        sicData.Count, intensities,
                        sicPeakFinderOptions.SICBaselineNoiseOptions,
                        out var baselineNoiseStats);
                        sicPeak.BaselineNoiseStats = baselineNoiseStats;
                    }

                    // Use a peak-finder algorithm to find the peak closest to .Peak.IndexMax
                    // Note that .Peak.IndexBaseLeft, .Peak.IndexBaseRight, and .Peak.IndexMax are passed ByRef and get updated by FindPeaks
                    int peakIndexStart = sicPeak.IndexBaseLeft;
                    int peakIndexEnd = sicPeak.IndexBaseRight;
                    int peakLocationIndex = sicPeak.IndexMax;
                    int previousPeakFWHMPointRight = sicPeak.PreviousPeakFWHMPointRight;
                    int nextPeakFWHMPointLeft = sicPeak.NextPeakFWHMPointLeft;
                    int shoulderCount = sicPeak.ShoulderCount;
                    success = FindPeaks(sicData, ref peakIndexStart, ref peakIndexEnd, ref peakLocationIndex,
                        ref previousPeakFWHMPointRight, ref nextPeakFWHMPointLeft, ref shoulderCount,
                        out smoothedYDataSubset, simDataPresent, sicPeakFinderOptions,
                        sicPeak.BaselineNoiseStats.NoiseLevel,
                        potentialAreaStatsForRegion.MinimumPotentialPeakArea,
                        returnClosestPeak);
                    sicPeak.IndexBaseLeft = peakIndexStart;
                    sicPeak.IndexBaseRight = peakIndexEnd;
                    sicPeak.IndexMax = peakLocationIndex;
                    sicPeak.PreviousPeakFWHMPointRight = previousPeakFWHMPointRight;
                    sicPeak.NextPeakFWHMPointLeft = nextPeakFWHMPointLeft;
                    sicPeak.ShoulderCount = shoulderCount;

                    if (success)
                    {
                        // Update the maximum peak intensity (required prior to call to ComputeNoiseLevelInPeakVicinity and call to ComputeSICPeakArea)
                        sicPeak.MaxIntensityValue = sicData[sicPeak.IndexMax].Intensity;

                        if (recomputeNoiseLevel)
                        {
                            // Update the value for potentialAreaStatsForPeak.SICNoiseThresholdIntensity based on the data around the peak
                            success = ComputeNoiseLevelInPeakVicinity(
                                sicData, sicPeak,
                                sicPeakFinderOptions.SICBaselineNoiseOptions);
                        }

                        //// Compute the trimmed median of the data in SICData (replacing non positive values with the minimum)
                        //// If the median is less than sicPeak.BaselineNoiseStats.NoiseLevel then update sicPeak.BaselineNoiseStats.NoiseLevel
                        //noiseOptionsOverride.BaselineNoiseMode = eNoiseThresholdModes.TrimmedMedianByAbundance;
                        //noiseOptionsOverride.TrimmedMeanFractionLowIntensityDataToAverage = 0.75;
                        //
                        //success = ComputeNoiseLevelForSICData[sicData, noiseOptionsOverride, noiseStatsCompare];
                        //if (noiseStatsCompare.PointsUsed >= MINIMUM_NOISE_SCANS_REQUIRED)
                        //{
                        //    // Check whether the comparison noise level is less than the existing noise level times 0.75
                        //    if (noiseStatsCompare.NoiseLevel < sicPeak.BaselineNoiseStats.NoiseLevel * 0.75)
                        //    {
                        //        // Yes, the comparison noise level is lower
                        //        // Use a T-Test to see if the comparison noise level is significantly different than the primary noise level
                        //        if (TestSignificanceUsingTTest[noiseStatsCompare.NoiseLevel, sicPeak.BaselineNoiseStats.NoiseLevel, noiseStatsCompare.NoiseStDev, sicPeak.BaselineNoiseStats.NoiseStDev, noiseStatsCompare.PointsUsed, sicPeak.BaselineNoiseStats.PointsUsed, eTTestConfidenceLevelConstants.Conf95Pct, tCalculated])
                        //        {
                        //            sicPeak.BaselineNoiseStats = noiseStatsCompare;
                        //        }
                        //    }
                        //}

                        // If smoothing was enabled, then see if the smoothed value is larger than sicPeak.MaxIntensityValue
                        // If it is, then use the smoothed value for sicPeak.MaxIntensityValue
                        if (sicPeakFinderOptions.UseSavitzkyGolaySmooth || sicPeakFinderOptions.UseButterworthSmooth)
                        {
                            dataIndex = sicPeak.IndexMax - smoothedYDataSubset.DataStartIndex;
                            if (dataIndex >= 0 && smoothedYDataSubset.Data != null &&
                                dataIndex < smoothedYDataSubset.DataCount)
                            {
                                // Possibly use the intensity of the smoothed data as the peak intensity
                                intensityCompare = smoothedYDataSubset.Data[dataIndex];
                                if (intensityCompare > sicPeak.MaxIntensityValue)
                                {
                                    sicPeak.MaxIntensityValue = intensityCompare;
                                }
                            }
                        }

                        // Compute the signal to noise ratio for the peak
                        sicPeak.SignalToNoiseRatio = ComputeSignalToNoise(sicPeak.MaxIntensityValue, sicPeak.BaselineNoiseStats.NoiseLevel);

                        // Compute the Full Width at Half Max (fwhm) value, this time subtracting the noise level from the baseline
                        sicPeak.FWHMScanWidth = ComputeFWHM(sicData, sicPeak, true);

                        // Compute the Area (this function uses .FWHMScanWidth and therefore needs to be called after ComputeFWHM)
                        success = ComputeSICPeakArea(sicData, sicPeak);

                        // Compute the Statistical Moments values
                        ComputeStatisticalMomentsStats(sicData, smoothedYDataSubset, sicPeak);
                    }
                    else
                    {
                        // No peak found

                        sicPeak.MaxIntensityValue = sicData[sicPeak.IndexMax].Intensity;
                        sicPeak.IndexBaseLeft = sicPeak.IndexMax;
                        sicPeak.IndexBaseRight = sicPeak.IndexMax;
                        sicPeak.FWHMScanWidth = 1;

                        // Assign the intensity of the peak at the observed maximum to the area
                        sicPeak.Area = sicPeak.MaxIntensityValue;

                        sicPeak.SignalToNoiseRatio = ComputeSignalToNoise(sicPeak.MaxIntensityValue, sicPeak.BaselineNoiseStats.NoiseLevel);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LogErrors("clsMASICPeakFinder->FindSICPeakAndArea", "Error finding SIC peaks and their areas", ex, false);
                return false;
            }
        }

        public static clsBaselineNoiseOptions GetDefaultNoiseThresholdOptions()
        {
            var baselineNoiseOptions = new clsBaselineNoiseOptions
            {
                BaselineNoiseMode = eNoiseThresholdModes.TrimmedMedianByAbundance,
                BaselineNoiseLevelAbsolute = 0,
                MinimumSignalToNoiseRatio = 0,                      // ToDo: Figure out how best to use this when > 0; for now, the SICNoiseMinimumSignalToNoiseRatio property ignores any attempts to set this value
                MinimumBaselineNoiseLevel = 1,
                TrimmedMeanFractionLowIntensityDataToAverage = 0.75,
                DualTrimmedMeanStdDevLimits = 5,
                DualTrimmedMeanMaximumSegments = 3
            };

            return baselineNoiseOptions;
        }

        public static clsSICPeakFinderOptions GetDefaultSICPeakFinderOptions()
        {
            var sicPeakFinderOptions = new clsSICPeakFinderOptions
            {
                IntensityThresholdFractionMax = 0.01,           // 1% of the peak maximum
                IntensityThresholdAbsoluteMinimum = 0,

                // Set the default SIC Baseline noise threshold options
                SICBaselineNoiseOptions = GetDefaultNoiseThresholdOptions(),

                // Customize a few values
                MaxDistanceScansNoOverlap = 0,
                MaxAllowedUpwardSpikeFractionMax = 0.2,         // 20%
                InitialPeakWidthScansScaler = 1,
                InitialPeakWidthScansMaximum = 30,

                FindPeaksOnSmoothedData = true,
                SmoothDataRegardlessOfMinimumPeakWidth = true,
                UseButterworthSmooth = true,                                // If this is true, will ignore UseSavitzkyGolaySmooth
                ButterworthSamplingFrequency = 0.25,
                ButterworthSamplingFrequencyDoubledForSIMData = true,

                UseSavitzkyGolaySmooth = true,
                SavitzkyGolayFilterOrder = 0,                               // Moving average filter if 0, Savitzky Golay filter if 2, 4, 6, etc.

                // Set the default Mass Spectra noise threshold options
                MassSpectraNoiseThresholdOptions = GetDefaultNoiseThresholdOptions()
            };

            // Customize a few values
            sicPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = eNoiseThresholdModes.TrimmedMedianByAbundance;

            sicPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = eNoiseThresholdModes.TrimmedMedianByAbundance;
            sicPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = 0.5;
            sicPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = 2;

            return sicPeakFinderOptions;
        }

        private string GetVersionForExecutingAssembly()
        {
            try
            {
                string versionInfo = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                return versionInfo;
            }
            catch (Exception ex)
            {
                return "??.??.??.??";
            }
        }

        public static clsBaselineNoiseStats InitializeBaselineNoiseStats(
            double minimumBaselineNoiseLevel,
            eNoiseThresholdModes noiseThresholdMode)
        {
            var baselineNoiseStats = new clsBaselineNoiseStats()
            {
                NoiseLevel = minimumBaselineNoiseLevel,
                NoiseStDev = 0,
                PointsUsed = 0,
                NoiseThresholdModeUsed = noiseThresholdMode
            };

            return baselineNoiseStats;
        }

        [Obsolete("Use the version that returns baselineNoiseStatsType")]
        public static void InitializeBaselineNoiseStats(
            ref clsBaselineNoiseStats baselineNoiseStats,
            double minimumBaselineNoiseLevel,
            eNoiseThresholdModes noiseThresholdMode)
        {
            baselineNoiseStats = InitializeBaselineNoiseStats(minimumBaselineNoiseLevel, noiseThresholdMode);
        }

        /// <summary>
        /// Determines the X value that corresponds to targetY by interpolating the line between (X1, Y1) and (X2, Y2)
        /// </summary>
        /// <param name="interpolatedXValue"></param>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <param name="targetY"></param>
        /// <returns>Returns True on success, false on error</returns>
        private bool InterpolateX(
            out double interpolatedXValue,
            int x1, int x2,
            double y1, double y2,
            double targetY)
        {
            double deltaY = y2 - y1;                                 // This is y-two minus y-one
            double ratio = (targetY - y1) / deltaY;
            int deltaX = x2 - x1;                                 // This is x-two minus x-one

            double targetX = ratio * deltaX + x1;

            if (Math.Abs(targetX - x1) >= 0 && Math.Abs(targetX - x2) >= 0)
            {
                interpolatedXValue = targetX;
                return true;
            }
            else
            {
                LogErrors("clsMasicPeakFinder->InterpolateX", "TargetX is not between X1 and X2; this shouldn't happen", null, false);
                interpolatedXValue = 0;
                return false;
            }
        }

        /// <summary>
        /// Determines the Y value that corresponds to xValToInterpolate by interpolating the line between (X1, Y1) and (X2, Y2)
        /// </summary>
        /// <param name="interpolatedIntensity"></param>
        /// <param name="X1"></param>
        /// <param name="X2"></param>
        /// <param name="Y1"></param>
        /// <param name="Y2"></param>
        /// <param name="xValToInterpolate"></param>
        /// <returns></returns>
        private bool InterpolateY(
        out double interpolatedIntensity,
        int X1, int X2,
        double Y1, double Y2,
        double xValToInterpolate)
        {
            int scanDifference = X2 - X1;
            if (scanDifference != 0)
            {
                interpolatedIntensity = Y1 + (Y2 - Y1) * ((xValToInterpolate - X1) / scanDifference);
                return true;
            }
            else
            {
                // xValToInterpolate is not between X1 and X2; cannot interpolate
                interpolatedIntensity = 0;
                return false;
            }
        }

        private void LogErrors(
            string source,
            string message,
            Exception ex,
            bool allowThrowingException = true)
        {
            string messageWithoutCRLF;

            mStatusMessage = string.Copy(message);

            messageWithoutCRLF = mStatusMessage.Replace(Environment.NewLine, "; ");

            OnErrorEvent(source + ": " + messageWithoutCRLF, ex);

            if (allowThrowingException)
            {
                throw new Exception(mStatusMessage, ex);
            }
        }

        public clsBaselineNoiseStats LookupNoiseStatsUsingSegments(
            int scanIndexObserved,
            List<clsBaselineNoiseStatsSegment> noiseStatsSegments)
        {
            int noiseSegmentIndex;
            int indexSegmentA;
            int indexSegmentB;

            clsBaselineNoiseStats baselineNoiseStats = null;
            var segmentMidPointA = default(int);
            var segmentMidPointB = default(int);
            bool matchFound;

            double fractionFromSegmentB;
            double fractionFromSegmentA;

            try
            {
                if (noiseStatsSegments == null || noiseStatsSegments.Count < 1)
                {
                    baselineNoiseStats = InitializeBaselineNoiseStats(
                        GetDefaultNoiseThresholdOptions().MinimumBaselineNoiseLevel,
                        eNoiseThresholdModes.DualTrimmedMeanByAbundance);

                    return baselineNoiseStats;
                }

                if (noiseStatsSegments.Count <= 1)
                {
                    return noiseStatsSegments.First().BaselineNoiseStats;
                }

                // First, initialize to the first segment
                baselineNoiseStats = noiseStatsSegments.First().BaselineNoiseStats.Clone();

                // Initialize indexSegmentA and indexSegmentB to 0, indicating no extrapolation needed
                indexSegmentA = 0;
                indexSegmentB = 0;
                matchFound = false;                // Next, see if scanIndexObserved matches any of the segments (provided more than one segment exists)
                for (noiseSegmentIndex = 0; noiseSegmentIndex < noiseStatsSegments.Count; noiseSegmentIndex++)
                {
                    var current = noiseStatsSegments[noiseSegmentIndex];

                    if (scanIndexObserved >= current.SegmentIndexStart && scanIndexObserved <= current.SegmentIndexEnd)
                    {
                        segmentMidPointA = current.SegmentIndexStart + Convert.ToInt32((current.SegmentIndexEnd - current.SegmentIndexStart) / (double)2);
                        matchFound = true;
                    }

                    if (matchFound)
                    {
                        baselineNoiseStats = current.BaselineNoiseStats.Clone();

                        if (scanIndexObserved < segmentMidPointA)
                        {
                            if (noiseSegmentIndex > 0)
                            {
                                // Need to Interpolate using this segment and the next one
                                indexSegmentA = noiseSegmentIndex - 1;
                                indexSegmentB = noiseSegmentIndex;

                                // Copy segmentMidPointA to segmentMidPointB since the current segment is actually segment B
                                // Define segmentMidPointA
                                segmentMidPointB = segmentMidPointA;
                                var previous = noiseStatsSegments[noiseSegmentIndex - 1];
                                segmentMidPointA = previous.SegmentIndexStart + Convert.ToInt32((previous.SegmentIndexEnd - previous.SegmentIndexStart) / (double)2);
                            }
                            else
                            {
                                // scanIndexObserved occurs before the midpoint, but we're in the first segment; no need to Interpolate
                            }
                        }
                        else if (scanIndexObserved > segmentMidPointA)
                        {
                            if (noiseSegmentIndex < noiseStatsSegments.Count - 1)
                            {
                                // Need to Interpolate using this segment and the one before it
                                indexSegmentA = noiseSegmentIndex;
                                indexSegmentB = noiseSegmentIndex + 1;

                                // Note: segmentMidPointA is already defined since the current segment is segment A
                                // Define segmentMidPointB
                                var nextSegment = noiseStatsSegments[noiseSegmentIndex + 1];

                                segmentMidPointB = nextSegment.SegmentIndexStart + Convert.ToInt32((nextSegment.SegmentIndexEnd - nextSegment.SegmentIndexStart) / (double)2);
                            }
                            else
                            {
                                // scanIndexObserved occurs after the midpoint, but we're in the last segment; no need to Interpolate
                            }
                        }
                        else
                        {
                            // scanIndexObserved occurs at the midpoint; no need to Interpolate
                        }

                        if (indexSegmentA != indexSegmentB)
                        {
                            // Interpolate between the two segments
                            fractionFromSegmentB = Convert.ToDouble(scanIndexObserved - segmentMidPointA) /
                                                                    Convert.ToDouble(segmentMidPointB - segmentMidPointA);
                            if (fractionFromSegmentB < 0)
                            {
                                fractionFromSegmentB = 0;
                            }
                            else if (fractionFromSegmentB > 1)
                            {
                                fractionFromSegmentB = 1;
                            }

                            fractionFromSegmentA = 1 - fractionFromSegmentB;

                            // Compute the weighted average values
                            var segmentA = noiseStatsSegments[indexSegmentA].BaselineNoiseStats;
                            var segmentB = noiseStatsSegments[indexSegmentB].BaselineNoiseStats;

                            baselineNoiseStats.NoiseLevel = segmentA.NoiseLevel * fractionFromSegmentA + segmentB.NoiseLevel * fractionFromSegmentB;
                            baselineNoiseStats.NoiseStDev = segmentA.NoiseStDev * fractionFromSegmentA + segmentB.NoiseStDev * fractionFromSegmentB;
                            baselineNoiseStats.PointsUsed = Convert.ToInt32(segmentA.PointsUsed * fractionFromSegmentA + segmentB.PointsUsed * fractionFromSegmentB);
                        }

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignore Errors
            }

            return baselineNoiseStats;
        }

        /// <summary>
        /// Looks for the minimum positive value in dataListSorted() and replaces all values of 0 in dataListSorted() with minimumPositiveValue
        /// </summary>
        /// <param name="dataCount"></param>
        /// <param name="dataListSorted"></param>
        /// <returns>Minimum positive value</returns>
        /// <remarks>Assumes data in dataListSorted() is sorted ascending</remarks>
        private double ReplaceSortedDataWithMinimumPositiveValue(int dataCount, IList<double> dataListSorted)
        {
            double minimumPositiveValue;
            int indexFirstPositiveValue;

            // Find the minimum positive value in dataListSorted
            // Since it's sorted, we can stop at the first non-zero value

            indexFirstPositiveValue = -1;
            minimumPositiveValue = 0;
            for (int i = 0; i < dataCount; i++)
            {
                if (dataListSorted[i] > 0)
                {
                    indexFirstPositiveValue = i;
                    minimumPositiveValue = dataListSorted[i];
                    break;
                }
            }

            if (minimumPositiveValue < 1)
                minimumPositiveValue = 1;
            for (int i = indexFirstPositiveValue; i >= 0; i--)
                dataListSorted[i] = minimumPositiveValue;

            return minimumPositiveValue;
        }

        /// <summary>
        /// Uses the means and sigma values to compute the t-test value between the two populations to determine if they are statistically different
        /// </summary>
        /// <param name="mean1"></param>
        /// <param name="mean2"></param>
        /// <param name="stDev1"></param>
        /// <param name="stDev2"></param>
        /// <param name="dataCount1"></param>
        /// <param name="dataCount2"></param>
        /// <param name="confidenceLevel"></param>
        /// <param name="tCalculated"></param>
        /// <returns>True if the two populations are statistically different, based on the given significance threshold</returns>
        private bool TestSignificanceUsingTTest(
            double mean1, double mean2,
            double stDev1, double stDev2,
            int dataCount1, int dataCount2,
            eTTestConfidenceLevelConstants confidenceLevel,
            out double tCalculated)
        {
            // To use the t-test you must use sample variance values, not population variance values
            // Note: Variance_Sample = Sum((x-mean)^2) / (count-1)
            // Note: Sigma = SquareRoot(Variance_Sample)

            double sPooled;
            int confidenceLevelIndex;

            if (dataCount1 + dataCount2 <= 2)
            {
                // Cannot compute the T-Test
                tCalculated = 0;
                return false;
            }
            else
            {
                sPooled = Math.Sqrt((Math.Pow(stDev1, 2) * (dataCount1 - 1) + Math.Pow(stDev2, 2) * (dataCount2 - 1)) / (dataCount1 + dataCount2 - 2));
                tCalculated = (mean1 - mean2) / sPooled * Math.Sqrt(dataCount1 * dataCount2 / (double)(dataCount1 + dataCount2));

                confidenceLevelIndex = (int)confidenceLevel;
                if (confidenceLevelIndex < 0)
                {
                    confidenceLevelIndex = 0;
                }
                else if (confidenceLevelIndex >= TTestConfidenceLevels.Length)
                {
                    confidenceLevelIndex = TTestConfidenceLevels.Length - 1;
                }

                if (tCalculated >= TTestConfidenceLevels[confidenceLevelIndex])
                {
                    // Differences are significant
                    return true;
                }
                else
                {
                    // Differences are not significant
                    return false;
                }
            }
        }
    }
}