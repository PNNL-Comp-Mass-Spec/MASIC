using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable UnusedMember.Global

namespace MASICPeakFinder
{
    // -------------------------------------------------------------------------------
    // Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
    // Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

    // E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
    // Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://www.pnnl.gov/integrative-omics
    // -------------------------------------------------------------------------------
    //
    // Licensed under the 2-Clause BSD License; you may not use this file except
    // in compliance with the License.  You may obtain a copy of the License at
    // https://opensource.org/licenses/BSD-2-Clause

    /// <summary>
    /// MASIC peak finder
    /// </summary>
    public class clsMASICPeakFinder : PRISM.EventNotifier
    {
        // Ignore Spelling: Butterworth, cls, frag, fwhm, Golay, pdf, prepend, Savitzky, unsmoothed

        /// <summary>
        /// Program date
        /// </summary>
        public string PROGRAM_DATE = "August 18, 2024";

        /// <summary>
        /// Minimum peak width, in points
        /// </summary>
        public const int MINIMUM_PEAK_WIDTH = 3;

        /// <summary>
        /// Noise threshold modes
        /// </summary>
        public enum NoiseThresholdModes
        {
            /// <summary>
            /// Absolute intensity
            /// </summary>
            AbsoluteThreshold = 0,

            /// <summary>
            /// Trimmed mean by abundance
            /// </summary>
            TrimmedMeanByAbundance = 1,

            /// <summary>
            /// Trimmed mean by count
            /// </summary>
            TrimmedMeanByCount = 2,

            /// <summary>
            /// Trimmed median by abundance
            /// </summary>
            TrimmedMedianByAbundance = 3,

            /// <summary>
            /// Dual trimmed mean by abundance
            /// </summary>
            DualTrimmedMeanByAbundance = 4,

            /// <summary>
            /// Mean of data near the peak
            /// </summary>
            MeanOfDataInPeakVicinity = 5
        }

        /// <summary>
        /// T-test confidence levels
        /// </summary>
        public enum TTestConfidenceLevelConstants
        {
            /// <summary>
            /// 80%
            /// </summary>
            Conf80Pct = 0,

            /// <summary>
            /// 90%
            /// </summary>
            Conf90Pct = 1,

            /// <summary>
            /// 95%
            /// </summary>
            Conf95Pct = 2,

            /// <summary>
            /// 98%
            /// </summary>
            Conf98Pct = 3,

            /// <summary>
            /// 99%
            /// </summary>
            Conf99Pct = 4,

            /// <summary>
            /// 99.5%
            /// </summary>
            Conf99_5Pct = 5,

            /// <summary>
            /// 99.8%
            /// </summary>
            Conf99_8Pct = 6,

            /// <summary>
            /// 99.9%
            /// </summary>
            Conf99_9Pct = 7
        }

        private string mStatusMessage;

        /// <summary>
        /// TTest Significance Table.
        /// Confidence Levels and critical values:
        /// 80%, 90%, 95%, 98%, 99%, 99.5%, 99.8%, 99.9%
        /// 1.886, 2.920, 4.303, 6.965, 9.925, 14.089, 22.327, 31.598
        /// </summary>
        private readonly double[] TTestConfidenceLevels = { 1.886, 2.92, 4.303, 6.965, 9.925, 14.089, 22.327, 31.598 };

        /// <summary>
        /// Program date
        /// </summary>
        public string ProgramDate => PROGRAM_DATE;

        /// <summary>
        /// Program version
        /// </summary>
        public string ProgramVersion => GetVersionForExecutingAssembly();

        /// <summary>
        /// Status message
        /// </summary>
        public string StatusMessage => mStatusMessage;

        /// <summary>
        /// Constructor
        /// </summary>
        public clsMASICPeakFinder()
        {
            mStatusMessage = string.Empty;
        }

        /// <summary>
        /// Compute an updated peak area by adjusting for the baseline
        /// </summary>
        /// <remarks>This method is used by MASIC Browser</remarks>
        /// <param name="sicPeak"></param>
        /// <param name="sicPeakWidthFullScans"></param>
        /// <param name="allowNegativeValues"></param>
        /// <returns>Adjusted peak area</returns>
        public static double BaselineAdjustArea(
            SICStatsPeak sicPeak, int sicPeakWidthFullScans, bool allowNegativeValues)
        {
            // Note, compute sicPeakWidthFullScans using:
            // Width = sicScanNumbers(.Peak.IndexBaseRight) - sicScanNumbers(.Peak.IndexBaseLeft) + 1

            return BaselineAdjustArea(sicPeak.Area, sicPeak.BaselineNoiseStats.NoiseLevel, sicPeak.FWHMScanWidth, sicPeakWidthFullScans, allowNegativeValues);
        }

        /// <summary>
        /// Compute an updated peak area by adjusting for the baseline
        /// </summary>
        /// <param name="peakArea"></param>
        /// <param name="baselineNoiseLevel"></param>
        /// <param name="sicPeakFWHMScans"></param>
        /// <param name="sicPeakWidthFullScans"></param>
        /// <param name="allowNegativeValues"></param>
        public static double BaselineAdjustArea(
            double peakArea,
            double baselineNoiseLevel,
            int sicPeakFWHMScans,
            int sicPeakWidthFullScans,
            bool allowNegativeValues)
        {
            var widthToSubtract = ComputeWidthAtBaseUsingFWHM(sicPeakFWHMScans, sicPeakWidthFullScans, 4);

            var correctedArea = peakArea - baselineNoiseLevel * widthToSubtract;

            if (allowNegativeValues || correctedArea > 0)
            {
                return correctedArea;
            }

            return 0;
        }

        /// <summary>
        /// Adjust the intensity of a SIC peak based on baseline noise stats
        /// </summary>
        /// <param name="sicPeak"></param>
        /// <param name="allowNegativeValues"></param>
        public static double BaselineAdjustIntensity(SICStatsPeak sicPeak, bool allowNegativeValues)
        {
            return BaselineAdjustIntensity(sicPeak.MaxIntensityValue, sicPeak.BaselineNoiseStats.NoiseLevel, allowNegativeValues);
        }

        /// <summary>
        /// Adjust the intensity of a data point based on baseline noise stats
        /// </summary>
        /// <param name="rawIntensity"></param>
        /// <param name="baselineNoiseLevel"></param>
        /// <param name="allowNegativeValues"></param>
        public static double BaselineAdjustIntensity(
            double rawIntensity,
            double baselineNoiseLevel,
            bool allowNegativeValues)
        {
            if (allowNegativeValues || rawIntensity > baselineNoiseLevel)
            {
                return rawIntensity - baselineNoiseLevel;
            }

            return 0;
        }

        private bool ComputeAverageNoiseLevelCheckCounts(
            int validDataCountA, int validDataCountB,
            double sumA, double sumB,
            int minimumCount,
            BaselineNoiseStats baselineNoiseStats)
        {
            var useLeftData = false;

            if (minimumCount < 1)
                minimumCount = 1;
            var useBothSides = false;

            if (validDataCountA < minimumCount && validDataCountB < minimumCount)
                return false;

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
            // else
            // {
            //     // Will use the data to the right of the peak apex
            // }

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
            else
            {
                // Use right data only
                baselineNoiseStats.NoiseLevel = sumB / validDataCountB;
                baselineNoiseStats.NoiseStDev = 0;
                baselineNoiseStats.PointsUsed = validDataCountB;
            }

            return true;
        }

        private bool ComputeAverageNoiseLevelExcludingRegion(
            IList<SICDataPoint> sicData,
            int indexStart, int indexEnd,
            int exclusionIndexStart, int exclusionIndexEnd,
            BaselineNoiseOptions baselineNoiseOptions,
            BaselineNoiseStats baselineNoiseStats)
        {
            // Compute the average intensity level between indexStart and exclusionIndexStart
            // Also compute the average between exclusionIndexEnd and indexEnd
            // Use ComputeAverageNoiseLevelCheckCounts to determine whether both averages are used to determine
            // the baseline noise level or whether just one of the averages is used

            var success = false;

            // Examine the exclusion range.  If the exclusion range excludes all
            // data to the left or right of the peak, use a few data points anyway, even if this does include some of the peak
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
                var minimumPositiveValue = FindMinimumPositiveValue(sicData, 1);

                var validDataCountA = 0;
                double sumA = 0;

                for (var i = indexStart; i <= exclusionIndexStart; i++)
                {
                    sumA += Math.Max(minimumPositiveValue, sicData[i].Intensity);
                    validDataCountA++;
                }

                var validDataCountB = 0;
                double sumB = 0;

                for (var i = exclusionIndexEnd; i <= indexEnd; i++)
                {
                    sumB += Math.Max(minimumPositiveValue, sicData[i].Intensity);
                    validDataCountB++;
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
                    for (var i = indexStart; i <= exclusionIndexStart; i++)
                    {
                        sumA += Math.Pow(Math.Max(minimumPositiveValue, sicData[i].Intensity) - baselineNoiseStats.NoiseLevel, 2);
                        validDataCountA++;
                    }

                    for (var i = exclusionIndexEnd; i <= indexEnd; i++)
                    {
                        sumB += Math.Pow(Math.Max(minimumPositiveValue, sicData[i].Intensity) - baselineNoiseStats.NoiseLevel, 2);
                        validDataCountB++;
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

                baselineNoiseOptionsOverride.BaselineNoiseMode = NoiseThresholdModes.TrimmedMedianByAbundance;
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
        BaselineNoiseOptions baselineNoiseOptions,
        out List<BaselineNoiseStatsSegment> noiseStatsSegments)
        {
            noiseStatsSegments = new List<BaselineNoiseStatsSegment>(Math.Max(3, (int)baselineNoiseOptions.DualTrimmedMeanMaximumSegments));

            try
            {
                int segmentCountLocal = baselineNoiseOptions.DualTrimmedMeanMaximumSegments;

                if (segmentCountLocal == 0)
                    segmentCountLocal = 3;

                if (segmentCountLocal < 1)
                    segmentCountLocal = 1;

                // Initialize BaselineNoiseStats for each segment now, in case an error occurs
                for (var i = 0; i < segmentCountLocal; i++)
                {
                    var baselineNoiseStats = InitializeBaselineNoiseStats(
                        baselineNoiseOptions.MinimumBaselineNoiseLevel,
                        NoiseThresholdModes.DualTrimmedMeanByAbundance);
                    noiseStatsSegments.Add(new BaselineNoiseStatsSegment(baselineNoiseStats));
                }

                // Determine the segment length
                var segmentLength = (int)Math.Round((indexEnd - indexStart) / (double)segmentCountLocal, 0);

                // Initialize the first segment
                var firstSegment = noiseStatsSegments.First();
                firstSegment.SegmentIndexStart = indexStart;

                if (segmentCountLocal == 1)
                {
                    firstSegment.SegmentIndexEnd = indexEnd;
                }
                else
                {
                    firstSegment.SegmentIndexEnd = segmentLength + firstSegment.SegmentIndexStart - 1;
                }

                // Initialize the remaining segments
                for (var i = 1; i < segmentCountLocal; i++)
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
                for (var i = 0; i < segmentCountLocal; i++)
                {
                    var current = noiseStatsSegments[i];

                    ComputeDualTrimmedNoiseLevel(dataList, current.SegmentIndexStart, current.SegmentIndexEnd, baselineNoiseOptions, out var baselineNoiseStats);
                    current.BaselineNoiseStats = baselineNoiseStats;
                }

                // Compare adjacent segments using a T-Test, starting with the final segment and working backward
                const TTestConfidenceLevelConstants confidenceLevel = TTestConfidenceLevelConstants.Conf90Pct;
                var segmentIndex = segmentCountLocal - 1;

                while (segmentIndex > 0)
                {
                    var previous = noiseStatsSegments[segmentIndex - 1];
                    var current = noiseStatsSegments[segmentIndex];

                    var significantDifference = TestSignificanceUsingTTest(
                        current.BaselineNoiseStats.NoiseLevel,
                        previous.BaselineNoiseStats.NoiseLevel,
                        current.BaselineNoiseStats.NoiseStDev,
                        previous.BaselineNoiseStats.NoiseStDev,
                        current.BaselineNoiseStats.PointsUsed,
                        previous.BaselineNoiseStats.PointsUsed,
                        confidenceLevel,
                        out _);

                    if (significantDifference)
                    {
                        // Significant difference; leave the 2 segments intact
                    }
                    else
                    {
                        // Not a significant difference; recompute the Baseline Noise stats using the two segments combined
                        previous.SegmentIndexEnd = current.SegmentIndexEnd;

                        ComputeDualTrimmedNoiseLevel(
                            dataList,
                            previous.SegmentIndexStart,
                            previous.SegmentIndexEnd,
                            baselineNoiseOptions,
                            out var baselineNoiseStats);

                        previous.BaselineNoiseStats = baselineNoiseStats;

                        for (var segmentIndexCopy = segmentIndex; segmentIndexCopy < segmentCountLocal - 1; segmentIndexCopy++)
                        {
                            noiseStatsSegments[segmentIndexCopy] = noiseStatsSegments[segmentIndexCopy + 1];
                        }

                        segmentCountLocal--;
                    }

                    segmentIndex--;
                }

                while (noiseStatsSegments.Count > segmentCountLocal)
                {
                    noiseStatsSegments.RemoveAt(noiseStatsSegments.Count - 1);
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Computes the average of all the data in dataList()
        /// Next, discards the data above and below baselineNoiseOptions.DualTrimmedMeanStdDevLimits of the mean
        /// Finally, recomputes the average using the data that remains
        /// </summary>
        /// <remarks>
        /// Replaces values of 0 with the minimum positive value in dataList()
        /// You cannot use dataList.Length to determine the length of the array; use indexStart and indexEnd to find the limits
        /// </remarks>
        /// <param name="dataList"></param>
        /// <param name="indexStart"></param>
        /// <param name="indexEnd"></param>
        /// <param name="baselineNoiseOptions"></param>
        /// <param name="baselineNoiseStats"></param>
        /// <returns>True if success, False if error (or no data in dataList)</returns>
        public bool ComputeDualTrimmedNoiseLevel(
            IReadOnlyList<double> dataList, int indexStart, int indexEnd,
            BaselineNoiseOptions baselineNoiseOptions,
            out BaselineNoiseStats baselineNoiseStats)
        {
            // Initialize baselineNoiseStats
            baselineNoiseStats = InitializeBaselineNoiseStats(
                baselineNoiseOptions.MinimumBaselineNoiseLevel,
                NoiseThresholdModes.DualTrimmedMeanByAbundance);

            if (dataList == null || indexEnd < indexStart)
            {
                return false;
            }

            // Copy the data into dataListSorted
            var dataSortedCount = indexEnd - indexStart + 1;
            var dataListSorted = new double[dataSortedCount];

            for (var i = indexStart; i <= indexEnd; i++)
            {
                dataListSorted[i - indexStart] = dataList[i];
            }

            // Sort the array
            Array.Sort(dataListSorted);

            // Look for the minimum positive value and replace all data in dataListSorted with that value
            var minimumPositiveValue = ReplaceSortedDataWithMinimumPositiveValue(dataSortedCount, dataListSorted);

            // Initialize the indices to use in dataListSorted()
            var dataSortedIndexStart = 0;
            var dataSortedIndexEnd = dataSortedCount - 1;

            // Compute the average using the data in dataListSorted between dataSortedIndexStart and dataSortedIndexEnd (i.e. all the data)
            double sum = 0;

            for (var i = dataSortedIndexStart; i <= dataSortedIndexEnd; i++)
            {
                sum += dataListSorted[i];
            }

            var dataUsedCount = dataSortedIndexEnd + 1;
            var average = sum / dataUsedCount;
            double variance;

            if (dataUsedCount > 1)
            {
                // Compute the variance (this is a sample variance, not a population variance)
                sum = 0;

                for (var i = dataSortedIndexStart; i <= dataSortedIndexEnd; i++)
                {
                    sum += Math.Pow(dataListSorted[i] - average, 2);
                }

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
            var intensityThresholdMin = average - Math.Sqrt(variance) * baselineNoiseOptions.DualTrimmedMeanStdDevLimits;
            var intensityThresholdMax = average + Math.Sqrt(variance) * baselineNoiseOptions.DualTrimmedMeanStdDevLimits;

            // Recompute the average using only the data between intensityThresholdMin and intensityThresholdMax in dataListSorted
            sum = 0;

            for (var sortedIndex = dataSortedIndexStart; sortedIndex <= dataSortedIndexEnd; sortedIndex++)
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

                        sortedIndex++;
                    }
                }
            }

            dataUsedCount = dataSortedIndexEnd - dataSortedIndexStart + 1;

            if (dataUsedCount > 0)
            {
                baselineNoiseStats.NoiseLevel = sum / dataUsedCount;

                // Compute the variance (this is a sample variance, not a population variance)
                sum = 0;

                for (var i = dataSortedIndexStart; i <= dataSortedIndexEnd; i++)
                {
                    sum += Math.Pow(dataListSorted[i] - baselineNoiseStats.NoiseLevel, 2);
                }

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
            IList<SICDataPoint> sicData,
            SICStatsPeak sicPeak,
            bool subtractBaselineNoise)
        {
            // Note: The calling function should have already populated sicPeak.MaxIntensityValue, plus .IndexMax, .IndexBaseLeft, and .IndexBaseRight
            // If subtractBaselineNoise is True, this function also uses sicPeak.BaselineNoiseStats....
            // Note: This function returns the FWHM value in units of scan number; it does not update the value stored in sicPeak
            // This function does, however, update sicPeak.IndexMax if it is not between sicPeak.IndexBaseLeft and sicPeak.IndexBaseRight

            const bool ALLOW_NEGATIVE_VALUES = false;
            int fwhmScans;

            // Determine the full width at half max (fwhm), in units of absolute scan number
            try
            {
                if (sicPeak.IndexMax <= sicPeak.IndexBaseLeft || sicPeak.IndexMax >= sicPeak.IndexBaseRight)
                {
                    // Find the index of the maximum (between .IndexBaseLeft and .IndexBaseRight)
                    double maximumIntensity = 0;

                    if (sicPeak.IndexMax < sicPeak.IndexBaseLeft || sicPeak.IndexMax > sicPeak.IndexBaseRight)
                    {
                        sicPeak.IndexMax = sicPeak.IndexBaseLeft;
                    }

                    for (var dataIndex = sicPeak.IndexBaseLeft; dataIndex <= sicPeak.IndexBaseRight; dataIndex++)
                    {
                        if (sicData[dataIndex].Intensity > maximumIntensity)
                        {
                            sicPeak.IndexMax = dataIndex;
                            maximumIntensity = sicData[dataIndex].Intensity;
                        }
                    }
                }

                // Look for the intensity halfway down the peak (correcting for baseline noise level if subtractBaselineNoise = True)
                double targetIntensity;

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
                    double fwhmScanStart = -1;
                    double y2;
                    double y1;

                    for (var dataIndex = sicPeak.IndexBaseLeft; dataIndex < sicPeak.IndexMax; dataIndex++)
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
                                var indexOffset = (int)Math.Round((sicPeak.IndexMax - sicPeak.IndexBaseLeft) / 2.0, 0);

                                fwhmScanStart = sicData[dataIndex + indexOffset].ScanNumber;
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

                    double fwhmScanEnd = -1;

                    for (var dataIndex = sicPeak.IndexBaseRight - 1; dataIndex >= sicPeak.IndexMax; dataIndex--)
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
                                var indexOffset = (int)Math.Round((sicPeak.IndexBaseRight - sicPeak.IndexMax) / 2.0, 0);

                                fwhmScanEnd = sicData[dataIndex + 1 - indexOffset].ScanNumber;
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

                    fwhmScans = (int)Math.Round(fwhmScanEnd - fwhmScanStart, 0);

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

        /// <summary>
        /// Test ComputeKSStatistic
        /// </summary>
        public void TestComputeKSStat()
        {
            var scanNumbers = new[] { 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40 };
            var intensities = new double[] { 2, 5, 7, 10, 11, 18, 19, 15, 8, 4, 1 };

            const int scanAtApex = 20;
            const double fwhm = 25;

            // Sigma = fwhm / 2.35482 = fwhm / (2 * Sqrt(2 * NaturalLog(2)))
            const double peakMean = scanAtApex;

            // peakStDev = 28.8312
            const double peakStDev = fwhm / 2.35482;

            ComputeKSStatistic(scanNumbers.Length, scanNumbers, intensities, peakMean, peakStDev);

            // ToDo: Update program to call ComputeKSStatistic

            // ToDo: Update Statistical Moments computation to:
            // a) Create baseline adjusted intensity values
            // b) Remove the contiguous data from either end that is <= 0
            // c) Step through the remaining data and interpolate across gaps with intensities of 0 (linear interpolation)
            // d) Use this final data to compute the statistical moments and KS Statistic

            // If less than 3 points remain with the above procedure, use the 5 points centered around the peak maximum, non-baseline corrected data
        }

        private double ComputeKSStatistic(
            int dataCount,
            IList<int> xDataIn,
            IList<double> yDataIn,
            double peakMean,
            double peakStDev)
        {
            // Copy data from xDataIn() to xData, subtracting the value in xDataIn(0) from each scan
            var xData = new int[dataCount];
            var yData = new double[dataCount];

            var scanOffset = xDataIn[0];

            for (var i = 0; i < dataCount; i++)
            {
                xData[i] = xDataIn[i] - scanOffset;
                yData[i] = yDataIn[i];
            }

            var yDataSum = yData.Sum();

            if (Math.Abs(yDataSum) < double.Epsilon)
                yDataSum = 1;

            // Compute the Vector of normalized intensities = observed pdf
            var yDataNormalized = new double[yData.Length];

            for (var i = 0; i < yData.Length; i++)
            {
                yDataNormalized[i] = yData[i] / yDataSum;
            }

            // Estimate the empirical distribution function (EDF) using an accumulating sum
            yDataSum = 0;
            var yDataEDF = new double[yDataNormalized.Length];

            for (var i = 0; i < yDataNormalized.Length; i++)
            {
                yDataSum += yDataNormalized[i];
                yDataEDF[i] = yDataSum;
            }

            // Compute the Vector of Normal PDF values evaluated at the X values in the peak window
            var xDataPDF = new double[xData.Length];

            for (var i = 0; i < xData.Length; i++)
            {
                xDataPDF[i] = 1 / (Math.Sqrt(2 * Math.PI) * peakStDev) *
                    Math.Exp(-1 / 2.0 * Math.Pow((xData[i] - (peakMean - scanOffset)) / peakStDev, 2));
            }
            var xDataPDFSum = xDataPDF.Sum();

            // Estimate the theoretical CDF using an accumulating sum
            var xDataCDF = new double[xDataPDF.Length];
            yDataSum = 0;

            for (var i = 0; i < xDataPDF.Length; i++)
            {
                yDataSum += xDataPDF[i];
                xDataCDF[i] = yDataSum / ((1 + 1 / (double)xData.Length) * xDataPDFSum);
            }

            // Compute the maximum of the absolute differences between the YData EDF and XData CDF
            double KS_gof = 0;

            for (var i = 0; i < xDataCDF.Length; i++)
            {
                var compareVal = Math.Abs(yDataEDF[i] - xDataCDF[i]);

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
        /// <remarks>Updates baselineNoiseStats with the baseline noise level</remarks>
        /// <param name="dataCount"></param>
        /// <param name="dataList"></param>
        /// <param name="baselineNoiseOptions"></param>
        /// <param name="baselineNoiseStats"></param>
        /// <returns>Returns True if success, false in an error</returns>
        public bool ComputeNoiseLevelForSICData(
            int dataCount, IReadOnlyList<double> dataList,
            BaselineNoiseOptions baselineNoiseOptions,
            out BaselineNoiseStats baselineNoiseStats)
        {
            const bool IGNORE_NON_POSITIVE_DATA = false;

            if (baselineNoiseOptions.BaselineNoiseMode == NoiseThresholdModes.AbsoluteThreshold)
            {
                baselineNoiseStats = InitializeBaselineNoiseStats(
                    baselineNoiseOptions.BaselineNoiseLevelAbsolute,
                    baselineNoiseOptions.BaselineNoiseMode);

                return true;
            }

            if (baselineNoiseOptions.BaselineNoiseMode == NoiseThresholdModes.DualTrimmedMeanByAbundance)
            {
                return ComputeDualTrimmedNoiseLevel(dataList, 0, dataCount - 1, baselineNoiseOptions, out baselineNoiseStats);
            }

            return ComputeTrimmedNoiseLevel(dataList, 0, dataCount - 1, baselineNoiseOptions, IGNORE_NON_POSITIVE_DATA, out baselineNoiseStats);
        }

        /// <summary>
        /// Compute the noise level based on the data near the peak
        /// </summary>
        /// <param name="dataCount"></param>
        /// <param name="sicScanNumbers"></param>
        /// <param name="sicIntensities"></param>
        /// <param name="sicPeak"></param>
        /// <param name="baselineNoiseOptions"></param>
        [Obsolete("Use the version that takes a List(Of SICDataPoint")]
        public bool ComputeNoiseLevelInPeakVicinity(
            int dataCount, int[] sicScanNumbers, double[] sicIntensities,
            SICStatsPeak sicPeak,
            BaselineNoiseOptions baselineNoiseOptions)
        {
            var sicData = new List<SICDataPoint>(dataCount);

            for (var index = 0; index < dataCount; index++)
            {
                sicData.Add(new SICDataPoint(sicScanNumbers[index], sicIntensities[index], 0));
            }

            return ComputeNoiseLevelInPeakVicinity(sicData, sicPeak, baselineNoiseOptions);
        }

        /// <summary>
        /// Compute the noise level based on the data near the peak
        /// </summary>
        /// <param name="sicData"></param>
        /// <param name="sicPeak"></param>
        /// <param name="baselineNoiseOptions"></param>
        public bool ComputeNoiseLevelInPeakVicinity(
            List<SICDataPoint> sicData,
            SICStatsPeak sicPeak,
            BaselineNoiseOptions baselineNoiseOptions)
        {
            const int NOISE_ESTIMATE_DATA_COUNT_MINIMUM = 5;
            const int NOISE_ESTIMATE_DATA_COUNT_MAXIMUM = 100;

            // Initialize baselineNoiseStats
            sicPeak.BaselineNoiseStats = InitializeBaselineNoiseStats(
                baselineNoiseOptions.MinimumBaselineNoiseLevel,
                NoiseThresholdModes.MeanOfDataInPeakVicinity);

            // Only use a portion of the data to compute the noise level
            // The number of points to extend from the left and right is based on the width at 4 sigma; useful for tailing peaks
            // Also, determine the peak start using the smaller of the width at 4 sigma vs. the observed peak width

            // Estimate fwhm since it is sometimes not yet known when this function is called
            // The reason it's not yet know is that the final fwhm value is computed using baseline corrected intensity data, but
            // the whole purpose of this function is to compute the baseline level
            sicPeak.FWHMScanWidth = ComputeFWHM(sicData, sicPeak, false);

            // Minimum of peak width at 4 sigma vs. peakWidthFullScans
            var peakWidthBaseScans = ComputeWidthAtBaseUsingFWHM(sicPeak, sicData, 4);
            var peakWidthPoints = ConvertScanWidthToPoints(peakWidthBaseScans, sicPeak, sicData);

            var peakHalfWidthPoints = (int)Math.Round(peakWidthPoints / 1.5, 0);

            // Make sure that peakHalfWidthPoints is at least NOISE_ESTIMATE_DATA_COUNT_MINIMUM

            if (peakHalfWidthPoints < NOISE_ESTIMATE_DATA_COUNT_MINIMUM)
            {
                peakHalfWidthPoints = NOISE_ESTIMATE_DATA_COUNT_MINIMUM;
            }

            // Copy the peak base indices
            var indexBaseLeft = sicPeak.IndexBaseLeft;
            var indexBaseRight = sicPeak.IndexBaseRight;

            // Define IndexStart and IndexEnd, making sure that peakHalfWidthPoints is no larger than NOISE_ESTIMATE_DATA_COUNT_MAXIMUM
            var indexStart = indexBaseLeft - Math.Min(peakHalfWidthPoints, NOISE_ESTIMATE_DATA_COUNT_MAXIMUM);
            var indexEnd = sicPeak.IndexBaseRight + Math.Min(peakHalfWidthPoints, NOISE_ESTIMATE_DATA_COUNT_MAXIMUM);

            if (indexStart < 0)
                indexStart = 0;

            if (indexEnd >= sicData.Count)
                indexEnd = sicData.Count - 1;

            // Compare indexStart to sicPeak.PreviousPeakFWHMPointRight
            // If it is less than .PreviousPeakFWHMPointRight, update accordingly
            if (indexStart < sicPeak.PreviousPeakFWHMPointRight &&
                sicPeak.PreviousPeakFWHMPointRight < sicPeak.IndexMax)
            {
                // Update indexStart to be at PreviousPeakFWHMPointRight
                indexStart = sicPeak.PreviousPeakFWHMPointRight;

                if (indexStart < 0)
                    indexStart = 0;

                // If not enough points, alternately shift indexStart to the left 1 point and
                // indexBaseLeft to the right one point until we do have enough points
                var shiftLeft = true;

                while (indexBaseLeft - indexStart + 1 < NOISE_ESTIMATE_DATA_COUNT_MINIMUM)
                {
                    if (shiftLeft)
                    {
                        if (indexStart > 0)
                            indexStart--;
                    }
                    else if (indexBaseLeft < sicPeak.IndexMax)
                    {
                        indexBaseLeft++;
                    }

                    if (indexStart <= 0 && indexBaseLeft >= sicPeak.IndexMax)
                    {
                        break;
                    }

                    shiftLeft = !shiftLeft;
                }
            }

            // Compare indexEnd to sicPeak.NextPeakFWHMPointLeft
            // If it is greater than .NextPeakFWHMPointLeft, update accordingly
            if (indexEnd >= sicPeak.NextPeakFWHMPointLeft &&
                sicPeak.NextPeakFWHMPointLeft > sicPeak.IndexMax)
            {
                indexEnd = sicPeak.NextPeakFWHMPointLeft;

                if (indexEnd >= sicData.Count)
                    indexEnd = sicData.Count - 1;

                // If not enough points, alternately shift indexEnd to the right 1 point and
                // indexBaseRight to the left one point until we do have enough points
                var shiftLeft = false;

                while (indexEnd - indexBaseRight + 1 < NOISE_ESTIMATE_DATA_COUNT_MINIMUM)
                {
                    if (shiftLeft)
                    {
                        if (indexBaseRight > sicPeak.IndexMax)
                            indexBaseRight--;
                    }
                    else if (indexEnd < sicData.Count - 1)
                    {
                        indexEnd++;
                    }

                    if (indexBaseRight <= sicPeak.IndexMax && indexEnd >= sicData.Count - 1)
                    {
                        break;
                    }

                    shiftLeft = !shiftLeft;
                }
            }

            var success = ComputeAverageNoiseLevelExcludingRegion(
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
        [Obsolete("Use the version that takes a List(Of SICDataPoint")]
        public bool ComputeParentIonIntensity(
            int dataCount,
            int[] sicScanNumbers,
            double[] sicIntensities,
            SICStatsPeak sicPeak,
            int fragScanNumber)
        {
            var sicData = new List<SICDataPoint>(dataCount);

            for (var index = 0; index < dataCount; index++)
            {
                sicData.Add(new SICDataPoint(sicScanNumbers[index], sicIntensities[index], 0));
            }

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
        public bool ComputeParentIonIntensity(
            IList<SICDataPoint> sicData,
            SICStatsPeak sicPeak,
            int fragScanNumber)
        {
            bool success;

            try
            {
                // Lookup the scan number and intensity of the SIC scan at sicPeak.IndexObserved
                var x1 = sicData[sicPeak.IndexObserved].ScanNumber;
                var y1 = sicData[sicPeak.IndexObserved].Intensity;

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
                    // If the data file only has MS spectra and no MS/MS spectra, and if the parent ion is a custom M/Z value, this code will be reached
                    sicPeak.ParentIonIntensity = y1;
                }
                // We need to perform some interpolation to determine .ParentIonIntensity
                // Lookup the scan number and intensity of the next SIC scan
                else if (sicPeak.IndexObserved < sicData.Count - 1)
                {
                    var x2 = sicData[sicPeak.IndexObserved + 1].ScanNumber;
                    var y2 = sicData[sicPeak.IndexObserved + 1].Intensity;

                    success = InterpolateY(out var interpolatedIntensity, x1, x2, y1, y2, fragScanNumber - 1);

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
            catch (Exception)
            {
                // Ignore errors here
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Compute the peak area
        /// </summary>
        /// <remarks>The calling function must populate sicPeak.IndexMax, sicPeak.IndexBaseLeft, and sicPeak.IndexBaseRight</remarks>
        /// <param name="sicData"></param>
        /// <param name="sicPeak"></param>
        private bool ComputeSICPeakArea(IList<SICDataPoint> sicData, SICStatsPeak sicPeak)
        {
            try
            {
                // Compute the peak area

                // Copy the matching data from the source arrays to scanNumbers() and intensities
                // When copying, assure that the first and last points have an intensity of 0

                // We're reserving extra space in case we need to prepend or append a minimum value
                var scanNumbers = new int[sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 2 + 1];
                var intensities = new double[sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 2 + 1];

                // Define an intensity threshold of 5% of MaximumIntensity
                // If the peak data is not flanked by points <= intensityThreshold, we'll add them
                var intensityThreshold = sicData[sicPeak.IndexMax].Intensity * 0.05;

                // Estimate the average scan interval between each data point
                var avgScanInterval = (int)Math.Round(ComputeAvgScanInterval(sicData, sicPeak.IndexBaseLeft, sicPeak.IndexBaseRight), 0);

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
                for (var dataIndex = sicPeak.IndexBaseLeft; dataIndex <= sicPeak.IndexBaseRight; dataIndex++)
                {
                    var indexPointer = dataIndex - sicPeak.IndexBaseLeft + areaDataBaseIndex;
                    scanNumbers[indexPointer] = sicData[dataIndex].ScanNumber;
                    intensities[indexPointer] = sicData[dataIndex].Intensity;

                    // intensitiesSmoothed[indexPointer] = smoothedYDataSubset.Data[dataIndex - smoothedYDataSubset.DataStartIndex];
                    // if (intensitiesSmoothed[indexPointer] < 0)
                    //     intensitiesSmoothed[indexPointer] = 0;
                }

                var areaDataCount = sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 1 + areaDataBaseIndex;

                if (sicData[sicPeak.IndexBaseRight].Intensity > intensityThreshold)
                {
                    // Append an intensity data point of intensityThreshold, with a scan number avgScanInterval more than the last scan number for the actual peak data
                    var dataIndex = sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + areaDataBaseIndex + 1;

                    scanNumbers[dataIndex] = sicData[sicPeak.IndexBaseRight].ScanNumber + avgScanInterval;

                    intensities[dataIndex] = intensityThreshold;
                    areaDataCount++;
                    // intensitiesSmoothed(dataIndex) = intensityThreshold
                }

                // Compute the area
                // Note that we're using real data for this and not smoothed data
                // Also note that we're using raw data for the peak area (not baseline corrected values)
                double peakArea = 0;

                for (var dataIndex = 0; dataIndex < areaDataCount - 1; dataIndex++)
                {
                    // Use the Trapezoid area formula to compute the area slice to add to sicPeak.Area
                    // Peak Area = 0.5 * DeltaX * (Y1 + Y2)
                    var scanDelta = scanNumbers[dataIndex + 1] - scanNumbers[dataIndex];
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

        private double ComputeAvgScanInterval(IList<SICDataPoint> sicData, int dataIndexStart, int dataIndexEnd)
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
            catch (Exception)
            {
                scansPerPoint = 1;
            }

            return scansPerPoint;
        }

        private bool ComputeStatisticalMomentsStats(
            IList<SICDataPoint> sicData,
            SmoothedYDataSubset smoothedYDataSubset,
            SICStatsPeak sicPeak)
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

                var statMomentsData = new StatisticalMoments
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
                catch (Exception)
                {
                    // Ignore errors here
                }

                sicPeak.StatisticalMoments = statMomentsData;

                var dataCount = sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 1;

                if (dataCount < 1)
                {
                    // Do not continue if less than one point across the peak
                    return false;
                }

                // When reserving memory for these arrays, include room to add a minimum value at the beginning and end of the data, if needed
                // Also, reserve space for a minimum of 5 elements
                var minimumDataCount = DEFAULT_MINIMUM_DATA_COUNT;

                if (minimumDataCount > dataCount)
                {
                    minimumDataCount = 3;
                }

                // Contains values from sicData[x].ScanNumber
                var scanNumbers = new int[Math.Max(dataCount, minimumDataCount) + 1 + 1];
                // Contains values from sicData[x].Intensity subtracted by the baseline noise level; if the result is less than 0, will contain 0
                var intensities = new double[scanNumbers.Length];
                var useRawDataAroundMaximum = false;

                // Populate scanNumbers() and intensities()
                // Simultaneously, determine the maximum intensity
                double maximumBaselineAdjustedIntensity = 0;
                var indexMaximumIntensity = 0;

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (USE_SMOOTHED_DATA)
                {
                    dataCount = 0;

                    for (var dataIndex = sicPeak.IndexBaseLeft; dataIndex <= sicPeak.IndexBaseRight; dataIndex++)
                    {
                        var smoothedDataPointer = dataIndex - smoothedYDataSubset.DataStartIndex;

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

                            dataCount++;
                        }
                    }
                }
                else
                // ReSharper disable HeuristicUnreachableCode
#pragma warning disable 162
                {
                    dataCount = 0;

                    for (var dataIndex = sicPeak.IndexBaseLeft; dataIndex <= sicPeak.IndexBaseRight; dataIndex++)
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

                        dataCount++;
                    }
                }
#pragma warning restore 162
                // ReSharper restore HeuristicUnreachableCode

                // Define an intensity threshold of 10% of MaximumBaselineAdjustedIntensity
                var intensityThreshold = maximumBaselineAdjustedIntensity * 0.1;

                if (intensityThreshold < 1)
                    intensityThreshold = 1;

                // Step left from indexMaximumIntensity to find the first data point < intensityThreshold
                // Note that the final data will include one data point less than intensityThreshold at the beginning and end of the data
                var validDataIndexLeft = indexMaximumIntensity;

                while (validDataIndexLeft > 0 && intensities[validDataIndexLeft] >= intensityThreshold)
                {
                    validDataIndexLeft--;
                }

                // Step right from indexMaximumIntensity to find the first data point < intensityThreshold
                var validDataIndexRight = indexMaximumIntensity;

                while (validDataIndexRight < dataCount - 1 && intensities[validDataIndexRight] >= intensityThreshold)
                {
                    validDataIndexRight++;
                }

                if (validDataIndexLeft > 0 || validDataIndexRight < dataCount - 1)
                {
                    // Shrink the arrays to only retain the data centered around indexMaximumIntensity and
                    // having and intensity >= intensityThreshold, though one additional data point is retained at the beginning and end of the data
                    for (var dataIndex = validDataIndexLeft; dataIndex <= validDataIndexRight; dataIndex++)
                    {
                        var indexPointer = dataIndex - validDataIndexLeft;
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
                    {
                        validDataIndexLeft++;
                    }

                    if (validDataIndexLeft >= dataCount - 1)
                    {
                        // All the data is <= intensityThreshold
                        useRawDataAroundMaximum = true;
                    }
                    else
                    {
                        if (validDataIndexLeft > 0)
                        {
                            // Shrink the array to remove the values at the beginning that are < intensityThreshold, retaining one point < intensityThreshold
                            // Due to the algorithm used to find the contiguous data centered around the peak maximum, this code will typically never be reached
                            for (var dataIndex = validDataIndexLeft; dataIndex < dataCount; dataIndex++)
                            {
                                var indexPointer = dataIndex - validDataIndexLeft;
                                scanNumbers[indexPointer] = scanNumbers[dataIndex];
                                intensities[indexPointer] = intensities[dataIndex];
                            }

                            dataCount -= validDataIndexLeft;
                        }

                        // Remove the contiguous data from the right that is < intensityThreshold, retaining one point < intensityThreshold
                        // Due to the algorithm used to find the contiguous data centered around the peak maximum, this will typically have no effect
                        validDataIndexRight = dataCount - 1;

                        while (validDataIndexRight > 0 && intensities[validDataIndexRight - 1] < intensityThreshold)
                        {
                            validDataIndexRight--;
                        }

                        if (validDataIndexRight < dataCount - 1)
                        {
                            // Shrink the array to remove the values at the end that are < intensityThreshold, retaining one point < intensityThreshold
                            // Due to the algorithm used to find the contiguous data centered around the peak maximum, this code will typically never be reached
                            dataCount = validDataIndexRight + 1;
                        }

                        // Estimate the average scan interval between the data points in scanNumbers
                        var avgScanInterval = (int)Math.Round(ComputeAvgScanInterval(sicData, 0, dataCount - 1), 0);

                        // Make sure that intensities(0) is <= intensityThreshold
                        if (intensities[0] > intensityThreshold)
                        {
                            // Prepend a data point with intensity intensityThreshold and with a scan number 1 less than the first scan number in the valid data
                            for (var dataIndex = dataCount; dataIndex >= 1; dataIndex--)
                            {
                                scanNumbers[dataIndex] = scanNumbers[dataIndex - 1];
                                intensities[dataIndex] = intensities[dataIndex - 1];
                            }

                            scanNumbers[0] = scanNumbers[1] - avgScanInterval;
                            intensities[0] = intensityThreshold;
                            dataCount++;
                        }

                        // Make sure that intensities(dataCount-1) is <= intensityThreshold
                        if (intensities[dataCount - 1] > intensityThreshold)
                        {
                            // Append a data point with intensity intensityThreshold and with a scan number 1 more than the last scan number in the valid data

                            scanNumbers[dataCount] = scanNumbers[dataCount - 1] + avgScanInterval;
                            intensities[dataCount] = intensityThreshold;
                            dataCount++;
                        }
                    }
                }

                if (useRawDataAroundMaximum || dataCount < minimumDataCount)
                {
                    // Populate scanNumbers() and intensities() with the five data points centered around sicPeak.IndexMax
                    if (USE_SMOOTHED_DATA)
                    {
                        validDataIndexLeft = sicPeak.IndexMax - (int)Math.Floor(minimumDataCount / 2.0);

                        if (validDataIndexLeft < 0)
                            validDataIndexLeft = 0;
                        dataCount = 0;

                        for (var dataIndex = validDataIndexLeft; dataIndex < Math.Min(validDataIndexLeft + minimumDataCount, sicData.Count); dataIndex++)
                        {
                            var smoothedDataPointer = dataIndex - smoothedYDataSubset.DataStartIndex;

                            if (smoothedDataPointer >= 0 && smoothedDataPointer < smoothedYDataSubset.DataCount)
                            {
                                if (smoothedYDataSubset.Data[smoothedDataPointer] > 0)
                                {
                                    scanNumbers[dataCount] = sicData[dataIndex].ScanNumber;
                                    intensities[dataCount] = smoothedYDataSubset.Data[smoothedDataPointer];
                                    dataCount++;
                                }
                            }
                        }
                    }
                    else
                    // ReSharper disable HeuristicUnreachableCode
#pragma warning disable 162
                    {
                        validDataIndexLeft = sicPeak.IndexMax - (int)Math.Floor(minimumDataCount / 2.0);

                        if (validDataIndexLeft < 0)
                            validDataIndexLeft = 0;
                        dataCount = 0;

                        for (var dataIndex = validDataIndexLeft; dataIndex < Math.Min(validDataIndexLeft + minimumDataCount, sicData.Count); dataIndex++)
                        {
                            if (sicData[dataIndex].Intensity > 0)
                            {
                                scanNumbers[dataCount] = sicData[dataIndex].ScanNumber;
                                intensities[dataCount] = sicData[dataIndex].Intensity;
                                dataCount++;
                            }
                        }
                    }
#pragma warning restore 162
                    // ReSharper restore HeuristicUnreachableCode

                    if (dataCount < 3)
                    {
                        // We don't even have 3 positive values in the raw data; do not continue
                        return false;
                    }
                }

                // Step through intensities and interpolate across gaps with intensities of 0
                // Due to the algorithm used to find the contiguous data centered around the peak maximum, this will typically have no effect
                var pointIndex = 1;

                while (pointIndex < dataCount - 1)
                {
                    if (intensities[pointIndex] <= 0)
                    {
                        // Current point has an intensity of 0
                        // Find the next positive point
                        validDataIndexLeft = pointIndex + 1;

                        while (validDataIndexLeft < dataCount && intensities[validDataIndexLeft] <= 0)
                        {
                            validDataIndexLeft++;
                        }

                        // Interpolate between pointIndex-1 and validDataIndexLeft
                        for (var indexPointer = pointIndex; indexPointer < validDataIndexLeft; indexPointer++)
                        {
                            if (InterpolateY(
                                out var interpolatedIntensity,
                                scanNumbers[pointIndex - 1], scanNumbers[validDataIndexLeft],
                                intensities[pointIndex - 1], intensities[validDataIndexLeft],
                                scanNumbers[indexPointer]))
                            {
                                intensities[indexPointer] = interpolatedIntensity;
                            }
                        }

                        pointIndex = validDataIndexLeft + 1;
                    }
                    else
                    {
                        pointIndex++;
                    }
                }

                // Compute the zeroth moment (m0)
                double peakArea = 0;

                for (var dataIndex = 0; dataIndex < dataCount - 1; dataIndex++)
                {
                    // Use the Trapezoid area formula to compute the area slice to add to peakArea
                    // Area = 0.5 * DeltaX * (Y1 + Y2)
                    var scanDelta = scanNumbers[dataIndex + 1] - scanNumbers[dataIndex];
                    peakArea += 0.5 * scanDelta * (intensities[dataIndex] + intensities[dataIndex + 1]);
                }

                // For the first moment (m1), need to sum: intensity times scan number.
                // For each of the moments, need to subtract scanNumbers(0) from the scan numbers since
                // statistical moments calculations are skewed if the first X value is not zero.
                // When ScanDelta is > 1, we need to interpolate.

                var moment1Sum = (scanNumbers[0] - scanNumbers[0]) * intensities[0];

                for (var dataIndex = 1; dataIndex < dataCount; dataIndex++)
                {
                    moment1Sum += (scanNumbers[dataIndex] - scanNumbers[0]) * intensities[dataIndex];

                    var scanDelta = scanNumbers[dataIndex] - scanNumbers[dataIndex - 1];

                    if (scanDelta > 1)
                    {
                        // Data points are more than 1 scan apart; need to interpolate values
                        // However, no need to interpolate if both intensity values are 0
                        if (intensities[dataIndex - 1] > 0 || intensities[dataIndex] > 0)
                        {
                            for (var scanNumberInterpolate = scanNumbers[dataIndex - 1] + 1; scanNumberInterpolate < scanNumbers[dataIndex]; scanNumberInterpolate++)
                            {
                                // Use InterpolateY() to fill in the scans between dataIndex-1 and dataIndex
                                if (InterpolateY(
                                    out var interpolatedIntensity,
                                    scanNumbers[dataIndex - 1], scanNumbers[dataIndex],
                                    intensities[dataIndex - 1], intensities[dataIndex],
                                    scanNumberInterpolate))
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
                    var centerOfMassDecimal = 0.0;

                    var indexPointer = sicPeak.IndexMax - sicPeak.IndexBaseLeft;

                    if (indexPointer >= 0 && indexPointer < scanNumbers.Length)
                    {
                        centerOfMassDecimal = scanNumbers[indexPointer];
                    }

                    statMomentsData.CenterOfMassScan = (int)Math.Round(centerOfMassDecimal, 0);
                    statMomentsData.DataCountUsed = 1;
                }
                else
                {
                    // Area is positive; compute the center of mass

                    var centerOfMassDecimal = moment1Sum / peakArea + scanNumbers[0];

                    statMomentsData.Area = Math.Min(double.MaxValue, peakArea);
                    statMomentsData.CenterOfMassScan = (int)Math.Round(centerOfMassDecimal, 0);
                    statMomentsData.DataCountUsed = dataCount;

                    // For the second moment (m2), need to sum: (ScanNumber - m1)^2 * Intensity
                    // For the third moment (m3), need to sum: (ScanNumber - m1)^3 * Intensity
                    // When ScanDelta is > 1, we need to interpolate
                    var moment2Sum = Math.Pow(scanNumbers[0] - centerOfMassDecimal, 2) * intensities[0];
                    var moment3Sum = Math.Pow(scanNumbers[0] - centerOfMassDecimal, 3) * intensities[0];

                    for (var dataIndex = 1; dataIndex < dataCount; dataIndex++)
                    {
                        moment2Sum += Math.Pow(scanNumbers[dataIndex] - centerOfMassDecimal, 2) * intensities[dataIndex];
                        moment3Sum += Math.Pow(scanNumbers[dataIndex] - centerOfMassDecimal, 3) * intensities[dataIndex];

                        var scanDelta = scanNumbers[dataIndex] - scanNumbers[dataIndex - 1];

                        if (scanDelta > 1)
                        {
                            // Data points are more than 1 scan apart; need to interpolate values
                            // However, no need to interpolate if both intensity values are 0
                            if (intensities[dataIndex - 1] > 0 || intensities[dataIndex] > 0)
                            {
                                for (var scanNumberInterpolate = scanNumbers[dataIndex - 1] + 1; scanNumberInterpolate < scanNumbers[dataIndex]; scanNumberInterpolate++)
                                {
                                    // Use InterpolateY() to fill in the scans between dataIndex-1 and dataIndex
                                    if (InterpolateY(
                                        out var interpolatedIntensity,
                                        scanNumbers[dataIndex - 1], scanNumbers[dataIndex],
                                        intensities[dataIndex - 1], intensities[dataIndex],
                                        scanNumberInterpolate))
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
                // ReSharper disable HeuristicUnreachableCode
#pragma warning disable 162
                {
                    peakMean = sicData[sicPeak.IndexMax].ScanNumber;
                    // Sigma = fwhm / 2.35482 = fwhm / (2 * Sqrt(2 * NaturalLog(2)))
                    peakStDev = sicPeak.FWHMScanWidth / 2.35482;
                }
#pragma warning restore 162
                // ReSharper restore HeuristicUnreachableCode

                statMomentsData.KSStat = ComputeKSStatistic(dataCount, scanNumbers, intensities, peakMean, peakStDev);
            }
            catch (Exception ex)
            {
                LogErrors("clsMASICPeakFinder->ComputeStatisticalMomentsStats", "Error computing statistical moments", ex, false);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Compute the signal-to-noise ratio
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="noiseThresholdIntensity"></param>
        public static double ComputeSignalToNoise(double signal, double noiseThresholdIntensity)
        {
            if (noiseThresholdIntensity > 0)
            {
                return signal / noiseThresholdIntensity;
            }

            return 0;
        }

        /// <summary>
        /// Computes a trimmed mean or trimmed median using the low intensity data up to baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage
        /// Additionally, computes a full median using all data in dataList
        /// If ignoreNonPositiveData is True, removes data from dataList() less than zero 0 and less than .MinimumBaselineNoiseLevel
        /// </summary>
        /// <remarks>
        /// Replaces values of 0 with the minimum positive value in dataList()
        /// You cannot use dataList.Length to determine the length of the array; use dataCount
        /// </remarks>
        /// <param name="dataList"></param>
        /// <param name="indexStart"></param>
        /// <param name="indexEnd"></param>
        /// <param name="baselineNoiseOptions"></param>
        /// <param name="ignoreNonPositiveData"></param>
        /// <param name="baselineNoiseStats"></param>
        /// <returns>Returns True if success, False if error (or no data in dataList)</returns>
        public bool ComputeTrimmedNoiseLevel(
            IReadOnlyList<double> dataList, int indexStart, int indexEnd,
            BaselineNoiseOptions baselineNoiseOptions,
            bool ignoreNonPositiveData,
            out BaselineNoiseStats baselineNoiseStats)
        {
            // Initialize baselineNoiseStats
            baselineNoiseStats = InitializeBaselineNoiseStats(baselineNoiseOptions.MinimumBaselineNoiseLevel, baselineNoiseOptions.BaselineNoiseMode);

            if (dataList == null || indexEnd < indexStart)
            {
                return false;
            }

            // Copy the data into dataListSorted
            var dataSortedCount = indexEnd - indexStart + 1;
            // Note: You cannot use dataListSorted.Length to determine the length of the array; use indexStart and indexEnd to find the limits
            var dataListSorted = new double[dataSortedCount];

            for (var i = indexStart; i <= indexEnd; i++)
            {
                dataListSorted[i - indexStart] = dataList[i];
            }

            // Sort the array
            Array.Sort(dataListSorted);

            if (ignoreNonPositiveData)
            {
                // Remove data with a value <= 0

                if (dataListSorted[0] <= 0)
                {
                    var validDataCount = 0;

                    for (var i = 0; i < dataSortedCount; i++)
                    {
                        if (dataListSorted[i] > 0)
                        {
                            dataListSorted[validDataCount] = dataListSorted[i];
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
            }

            // Look for the minimum positive value and replace all data in dataListSorted with that value

            ReplaceSortedDataWithMinimumPositiveValue(dataSortedCount, dataListSorted);

            switch (baselineNoiseOptions.BaselineNoiseMode)
            {
                case NoiseThresholdModes.TrimmedMeanByAbundance:
                case NoiseThresholdModes.TrimmedMeanByCount:
                    int countSummed;
                    double sum;

                    if (baselineNoiseOptions.BaselineNoiseMode == NoiseThresholdModes.TrimmedMeanByAbundance)
                    {
                        // Average the data that has intensity values less than
                        // Minimum + baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (Maximum - Minimum)

                        var intensityThreshold = dataListSorted[0] + baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (dataListSorted[dataSortedCount - 1] - dataListSorted[0]);

                        // Initialize countSummed to dataSortedCount for now, in case all data is within the intensity threshold
                        countSummed = dataSortedCount;
                        sum = 0;

                        for (var i = 0; i < dataSortedCount; i++)
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
                        // NoiseThresholdModes.TrimmedMeanByCount
                        // Find the index of the data point at dataSortedCount * baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage and
                        // average the data from the start to that index
                        indexEnd = (int)Math.Round((dataSortedCount - 1) * baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, 0);

                        countSummed = indexEnd + 1;

                        sum = 0;

                        for (var i = 0; i <= indexEnd; i++)
                        {
                            sum += dataListSorted[i];
                        }
                    }

                    if (countSummed > 0)
                    {
                        // Compute the average
                        // Note that countSummed will be used below in the variance computation
                        baselineNoiseStats.NoiseLevel = sum / countSummed;
                        baselineNoiseStats.PointsUsed = countSummed;

                        if (countSummed > 1)
                        {
                            // Compute the variance
                            sum = 0;

                            for (var i = 0; i <= indexEnd; i++)
                            {
                                sum += Math.Pow(dataListSorted[i] - baselineNoiseStats.NoiseLevel, 2);
                            }

                            baselineNoiseStats.NoiseStDev = Math.Sqrt(sum / (countSummed - 1));
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

                case NoiseThresholdModes.TrimmedMedianByAbundance:
                    if (baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage >= 1)
                    {
                        indexEnd = dataSortedCount - 1;
                    }
                    else
                    {
                        // Find the median of the data that has intensity values less than
                        // Minimum + baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (Maximum - Minimum)
                        var intensityThreshold =
                            dataListSorted[0] +
                            baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage *
                            (dataListSorted[dataSortedCount - 1] - dataListSorted[0]);

                        // Find the first point with an intensity value <= intensityThreshold
                        indexEnd = dataSortedCount - 1;

                        for (var i = 1; i < dataSortedCount; i++)
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
                        baselineNoiseStats.NoiseLevel = dataListSorted[(int)Math.Round(indexEnd / 2.0)];
                    }
                    else
                    {
                        // Odd value; average the values on either side of indexEnd/2
                        var i = (int)Math.Round((indexEnd - 1) / 2.0);

                        if (i < 0)
                            i = 0;
                        var sum2 = dataListSorted[i];

                        i++;

                        if (i == dataSortedCount)
                            i = dataSortedCount - 1;
                        sum2 += dataListSorted[i];
                        baselineNoiseStats.NoiseLevel = sum2 / 2.0;
                    }

                    // Compute the variance
                    double varianceSum = 0;

                    for (var i = 0; i <= indexEnd; i++)
                    {
                        varianceSum += Math.Pow(dataListSorted[i] - baselineNoiseStats.NoiseLevel, 2);
                    }

                    var countSummed2 = indexEnd + 1;

                    if (countSummed2 > 0)
                    {
                        baselineNoiseStats.NoiseStDev = Math.Sqrt(varianceSum / (countSummed2 - 1));
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
                              "Unknown Noise Threshold Mode encountered: " + baselineNoiseOptions.BaselineNoiseMode,
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
        private static int ComputeWidthAtBaseUsingFWHM(
            SICStatsPeak sicPeak,
            IList<SICDataPoint> sicData,
            short sigmaValueForBase)
        {
            try
            {
                var peakWidthFullScans = sicData[sicPeak.IndexBaseRight].ScanNumber - sicData[sicPeak.IndexBaseLeft].ScanNumber + 1;
                return ComputeWidthAtBaseUsingFWHM(sicPeak.FWHMScanWidth, peakWidthFullScans, sigmaValueForBase);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Computes the width of the peak (in scans) using the fwhm value
        /// </summary>
        /// <remarks>Does not allow the width determined to be larger than sicPeakWidthFullScans</remarks>
        /// <param name="sicPeakFWHMScans"></param>
        /// <param name="sicPeakWidthFullScans"></param>
        /// <param name="sigmaValueForBase"></param>
        private static int ComputeWidthAtBaseUsingFWHM(
            int sicPeakFWHMScans,
            int sicPeakWidthFullScans,
            short sigmaValueForBase = 4)
        {
            int widthAtBase;

            if (sigmaValueForBase < 4)
                sigmaValueForBase = 4;

            if (sicPeakFWHMScans == 0)
            {
                widthAtBase = sicPeakWidthFullScans;
            }
            else
            {
                // Compute the peak width
                // Note: Sigma = fwhm / 2.35482 = fwhm / (2 * Sqrt(2 * NaturalLog(2)))
                var sigmaBasedWidth = (int)Math.Round(sigmaValueForBase * sicPeakFWHMScans / 2.35482);

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
        private int ConvertScanWidthToPoints(
            int peakWidthBaseScans,
            SICStatsPeak sicPeak,
            IList<SICDataPoint> sicData)
        {
            var scansPerPoint = ComputeAvgScanInterval(sicData, sicPeak.IndexBaseLeft, sicPeak.IndexBaseRight);
            return (int)(Math.Round(peakWidthBaseScans / scansPerPoint, 0));
        }

        /// <summary>
        /// Determine the minimum positive value in the list, or absoluteMinimumValue if the list is empty
        /// </summary>
        /// <param name="sicData"></param>
        /// <param name="absoluteMinimumValue"></param>
        public double FindMinimumPositiveValue(IList<SICDataPoint> sicData, double absoluteMinimumValue)
        {
            var minimumPositiveValue = (from item in sicData where item.Intensity > 0 select item.Intensity).DefaultIfEmpty(absoluteMinimumValue).Min();

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
        public double FindMinimumPositiveValue(IList<double> dataList, double absoluteMinimumValue)
        {
            var minimumPositiveValue = (from item in dataList where item > 0 select item).DefaultIfEmpty(absoluteMinimumValue).Min();

            if (minimumPositiveValue < absoluteMinimumValue)
            {
                return absoluteMinimumValue;
            }

            return minimumPositiveValue;
        }

        /// <summary>
        /// Determine the minimum positive value in the list, examining the first dataCount items
        /// </summary>
        /// <remarks>
        /// Does not use dataList.Length to determine the length of the list; uses dataCount
        /// However, if dataCount is > dataList.Length, dataList.Length-1 will be used for the maximum index to examine
        /// </remarks>
        /// <param name="dataCount"></param>
        /// <param name="dataList"></param>
        /// <param name="absoluteMinimumValue"></param>
        public double FindMinimumPositiveValue(int dataCount, IReadOnlyList<double> dataList, double absoluteMinimumValue)
        {
            if (dataCount > dataList.Count)
            {
                dataCount = dataList.Count;
            }

            var minimumPositiveValue = (from item in dataList.Take(dataCount) where item > 0 select item).DefaultIfEmpty(absoluteMinimumValue).Min();

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
            IList<SICDataPoint> sicData,
            ref int peakIndexStart,
            ref int peakIndexEnd,
            ref int peakLocationIndex,
            ref int previousPeakFWHMPointRight,
            ref int nextPeakFWHMPointLeft,
            ref int shoulderCount,
            out SmoothedYDataSubset smoothedYDataSubset,
            bool simDataPresent,
            SICPeakFinderOptions sicPeakFinderOptions,
            double sicNoiseThresholdIntensity,
            double minimumPotentialPeakArea,
            bool returnClosestPeak)
        {
            const int SMOOTHED_DATA_PADDING_COUNT = 2;

            bool validPeakFound;

            smoothedYDataSubset = new SmoothedYDataSubset();

            try
            {
                var peakDetector = new PeakDetection();

                var peakData = new PeaksContainer { SourceDataCount = sicData.Count };

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

                var maximumIntensity = sicData[0].Intensity;
                double maximumPotentialPeakArea = 0;
                var indexMaxIntensity = 0;

                // Initialize the intensity queue
                // The queue is used to keep track of the most recent intensity values
                var intensityQueue = new Queue<double>();

                double potentialPeakArea = 0;
                var dataPointCountAboveThreshold = 0;

                for (var i = 0; i < peakData.SourceDataCount; i++)
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
                            potentialPeakArea -= intensityQueue.Dequeue();
                        }
                        // Add this intensity to the queue
                        intensityQueue.Enqueue(sicData[i].Intensity);

                        if (potentialPeakArea > maximumPotentialPeakArea)
                        {
                            maximumPotentialPeakArea = potentialPeakArea;
                        }

                        dataPointCountAboveThreshold++;
                    }
                }

                // Determine the initial value for .PeakWidthPointsMinimum
                // We will use maximumIntensity and minimumPeakIntensity to compute a signal-to-noise value to help pick .PeakWidthPointsMinimum

                // Old: if (sicPeakFinderOptions.SICNoiseThresholdIntensity < 1)
                //          sicPeakFinderOptions.SICNoiseThresholdIntensity = 1;
                // Old: peakAreaSignalToNoise = maximumIntensity / sicPeakFinderOptions.SICNoiseThresholdIntensity;

                if (minimumPotentialPeakArea < 1)
                    minimumPotentialPeakArea = 1;
                var peakAreaSignalToNoise = maximumPotentialPeakArea / minimumPotentialPeakArea;

                if (peakAreaSignalToNoise < 1)
                    peakAreaSignalToNoise = 1;

                if (Math.Abs(sicPeakFinderOptions.ButterworthSamplingFrequency) < double.Epsilon)
                {
                    sicPeakFinderOptions.ButterworthSamplingFrequency = 0.25;
                }

                peakData.PeakWidthPointsMinimum =
                    (int)Math.Round(sicPeakFinderOptions.InitialPeakWidthScansScaler *
                    Math.Log10(Math.Floor(peakAreaSignalToNoise)) * 10);

                // Assure that .InitialPeakWidthScansMaximum is no greater than .InitialPeakWidthScansMaximum
                // and no greater than dataPointCountAboveThreshold/2 (rounded up)
                peakData.PeakWidthPointsMinimum = Math.Min(peakData.PeakWidthPointsMinimum, sicPeakFinderOptions.InitialPeakWidthScansMaximum);
                peakData.PeakWidthPointsMinimum = Math.Min(peakData.PeakWidthPointsMinimum, (int)Math.Ceiling(dataPointCountAboveThreshold / 2.0));

                if (peakData.PeakWidthPointsMinimum > peakData.SourceDataCount * 0.8)
                {
                    peakData.PeakWidthPointsMinimum = (int)Math.Floor(peakData.SourceDataCount * 0.8);
                }

                if (peakData.PeakWidthPointsMinimum < MINIMUM_PEAK_WIDTH)
                    peakData.PeakWidthPointsMinimum = MINIMUM_PEAK_WIDTH;

                // Save the original value for peakLocationIndex
                peakData.OriginalPeakLocationIndex = peakLocationIndex;
                peakData.MaxAllowedUpwardSpikeFractionMax = sicPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax;

                do
                {
                    var testingMinimumPeakWidth = peakData.PeakWidthPointsMinimum == MINIMUM_PEAK_WIDTH;

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
                        // If found, narrow the peak to leave just one zero intensity value
                        foreach (var currentPeak in peakData.Peaks)
                        {
                            while (currentPeak.LeftEdge < sicData.Count - 1 &&
                                   currentPeak.LeftEdge < currentPeak.RightEdge)
                            {
                                if (Math.Abs(sicData[currentPeak.LeftEdge].Intensity) < double.Epsilon &&
                                    Math.Abs(sicData[currentPeak.LeftEdge + 1].Intensity) < double.Epsilon)
                                {
                                    currentPeak.LeftEdge++;
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
                                    currentPeak.RightEdge--;
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
                        var smoothedYDataStartIndex = peakIndexStart - SMOOTHED_DATA_PADDING_COUNT;
                        var smoothedYDataEndIndex = peakIndexEnd + SMOOTHED_DATA_PADDING_COUNT;

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
                        smoothedYDataSubset = new SmoothedYDataSubset(peakData.SmoothedYData, smoothedYDataStartIndex, smoothedYDataEndIndex);

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
                                        // The current peak has a peak center between the "official" peak's boundaries
                                        // Make sure it's not the same peak as the "official" peak
                                        if (peakItem.PeakLocation != peakLocationIndex)
                                        {
                                            // Now see if the comparison peak's intensity is at least .IntensityThresholdFractionMax of the intensity of the "official" peak
                                            if (sicData[peakItem.PeakLocation].Intensity >= sicPeakFinderOptions.IntensityThresholdFractionMax * sicData[peakLocationIndex].Intensity)
                                            {
                                                // Yes, this is a shoulder peak
                                                shoulderCount++;
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

                        for (var i = peakIndexStart; i <= peakIndexEnd; i++)
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

                        // Populate previousPeakFWHMPointRight and nextPeakFWHMPointLeft
                        var adjacentPeakIntensityThreshold = sicData[peakLocationIndex].Intensity / 3;

                        // Search through peakDataSaved to find the closest peak (with a significant intensity) to the left of this peak
                        // Note that the peaks in peakDataSaved are not necessarily ordered by increasing index,
                        // thus the need for an exhaustive search

                        var smallestIndexDifference = sicData.Count + 1;

                        for (var peakIndexCompare = 0; peakIndexCompare < peakDataSaved.Peaks.Count; peakIndexCompare++)
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

                                    for (var dataIndex = comparisonPeakEdgeIndex; dataIndex >= comparisonPeak.PeakLocation; dataIndex--)
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

                        for (var peakIndexCompare = peakDataSaved.Peaks.Count - 1; peakIndexCompare >= 0; peakIndexCompare--)
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

                                    for (var dataIndex = comparisonPeakEdgeIndex; dataIndex <= comparisonPeak.PeakLocation; dataIndex++)
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
                    // If peakData.PeakWidthPointsMinimum is greater than 3 and testingMinimumPeakWidth = False, decrement it by 50%
                    else if (peakData.PeakWidthPointsMinimum > MINIMUM_PEAK_WIDTH && !testingMinimumPeakWidth)
                    {
                        peakData.PeakWidthPointsMinimum = (int)Math.Floor(peakData.PeakWidthPointsMinimum / 2.0);

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
        /// <remarks>All the identified peaks are returned in peaksContainer.Peaks(), regardless of whether they are valid or not</remarks>
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
        private bool FindPeaksWork(
            PeakDetection peakDetector,
            IList<int> scanNumbers,
            PeaksContainer peaksContainer,
            bool simDataPresent,
            SICPeakFinderOptions sicPeakFinderOptions,
            bool testingMinimumPeakWidth,
            bool returnClosestPeak)
        {
            var errorMessage = string.Empty;

            // Smooth the Y data, and store in peaksContainer.SmoothedYData
            // Note that if using a Butterworth filter, we increase peaksContainer.PeakWidthPointsMinimum if too small, compared to 1/SamplingFrequency
            var peakWidthPointsMinimum = peaksContainer.PeakWidthPointsMinimum;
            var dataIsSmoothed = FindPeaksWorkSmoothData(
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
                    (int)Math.Round(sicPeakFinderOptions.IntensityThresholdFractionMax * 100), 2, true, true);
            }
            else
            {
                // Look for the peaks, using peaksContainer.PeakWidthPointsMinimum as the minimum peak width
                peaksContainer.Peaks = peakDetector.DetectPeaks(
                    peaksContainer.XData,
                    peaksContainer.YData,
                    sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum,
                    peaksContainer.PeakWidthPointsMinimum,
                    (int)Math.Round(sicPeakFinderOptions.IntensityThresholdFractionMax * 100), 2, true, true);
            }

            if (peaksContainer.Peaks.Count == -1)
            {
                // Fatal error occurred while finding peaks
                return false;
            }

            var peakMaximum = peaksContainer.YData.Max();

            if (testingMinimumPeakWidth)
            {
                if (peaksContainer.Peaks.Count == 0)
                {
                    // No peaks were found; create a new peak list using the original peak location index as the peak center
                    var newPeak = new PeakInfo(peaksContainer.OriginalPeakLocationIndex)
                    {
                        LeftEdge = peaksContainer.OriginalPeakLocationIndex,
                        RightEdge = peaksContainer.OriginalPeakLocationIndex
                    };

                    peaksContainer.Peaks.Add(newPeak);
                }
                else if (returnClosestPeak)
                {
                    // Make sure one of the peaks is within 1 of the original peak location
                    var success = peaksContainer.Peaks.Any(t => Math.Abs(t.PeakLocation - peaksContainer.OriginalPeakLocationIndex) <= 1);

                    if (!success)
                    {
                        // No match was found; add a new peak at peaksContainer.OriginalPeakLocationIndex

                        var newPeak = new PeakInfo(peaksContainer.OriginalPeakLocationIndex)
                        {
                            LeftEdge = peaksContainer.OriginalPeakLocationIndex,
                            RightEdge = peaksContainer.OriginalPeakLocationIndex,
                            PeakArea = peaksContainer.YData[peaksContainer.OriginalPeakLocationIndex]
                        };

                        peaksContainer.Peaks.Add(newPeak);
                    }
                }
            }

            if (peaksContainer.Peaks.Count == 0)
            {
                // No peaks were found
                return false;
            }

            foreach (var peakItem in peaksContainer.Peaks)
            {
                peakItem.PeakIsValid = false;

                // Find the center and boundaries of this peak

                // Copy from the PeakEdges arrays to the working variables
                var peakLocationIndex = peakItem.PeakLocation;
                var peakIndexStart = peakItem.LeftEdge;
                var peakIndexEnd = peakItem.RightEdge;

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
                // next point after the increasing point is less than the current point, possibly keep stepping; the
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
                        peakIndexStart++;
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
                        peakIndexEnd--;
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
                var stepOverIncreaseCount = 0;

                while (peakIndexStart > 0)
                {
                    // currentSlope = peakDetector.ComputeSlope(peaksContainer.XData, peaksContainer.SmoothedYData, peakIndexStart, peakLocationIndex);

                    //if (currentSlope > 0 &&
                    //    peakLocationIndex - peakIndexStart > 3 &&
                    //    peaksContainer.SmoothedYData[peakIndexStart - 1] < Math.Max(sicPeakFinderOptions.IntensityThresholdFractionMax * peakMaximum, sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum))
                    //{
                    //    // We reached a low intensity data point and we're going downhill (i.e. the slope from this point to peakLocationIndex is positive)
                    //    // Step once more and stop
                    //    peakIndexStart--;
                    //    break;
                    //}

                    if (peaksContainer.SmoothedYData[peakIndexStart - 1] < peaksContainer.SmoothedYData[peakIndexStart])
                    {
                        // The adjacent point is lower than the current point
                        peakIndexStart--;
                    }
                    else if (Math.Abs(peaksContainer.SmoothedYData[peakIndexStart - 1] -
                                      peaksContainer.SmoothedYData[peakIndexStart]) < double.Epsilon)
                    {
                        // The adjacent point is equal to the current point
                        peakIndexStart--;
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

                            peakIndexStart--;

                            stepOverIncreaseCount++;
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
                    //    peakIndexEnd++;
                    //    break;
                    //}

                    if (peaksContainer.SmoothedYData[peakIndexEnd + 1] < peaksContainer.SmoothedYData[peakIndexEnd])
                    {
                        // The adjacent point is lower than the current point
                        peakIndexEnd++;
                    }
                    else if (Math.Abs(peaksContainer.SmoothedYData[peakIndexEnd + 1] -
                        peaksContainer.SmoothedYData[peakIndexEnd]) < double.Epsilon)
                    {
                        // The adjacent point is equal to the current point
                        peakIndexEnd++;
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

                            peakIndexEnd++;

                            stepOverIncreaseCount++;
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
                    // If peaksContainer.OriginalPeakLocationIndex is not between peakIndexStart and peakIndexEnd, check
                    // if the scan number for peaksContainer.OriginalPeakLocationIndex is within .MaxDistanceScansNoOverlap scans of
                    // either of the peak edges; if not, mark the peak as invalid since it does not contain the
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

            for (var foundPeakIndex = 0; foundPeakIndex < peaksContainer.Peaks.Count; foundPeakIndex++)
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

            return peaksContainer.BestPeakIndex >= 0;
        }

        private bool FindPeaksWorkSmoothData(
            PeaksContainer peaksContainer,
            bool simDataPresent,
            SICPeakFinderOptions sicPeakFinderOptions,
            ref int peakWidthPointsMinimum,
            ref string errorMessage)
        {
            // Returns True if the data was smoothed; false if not or an error
            // The smoothed data is returned in peakData.SmoothedYData

            var filter = new DataFilter.DataFilter();

            peaksContainer.SmoothedYData = new double[peaksContainer.SourceDataCount];

            if (peakWidthPointsMinimum > 4 && (sicPeakFinderOptions.UseSavitzkyGolaySmooth || sicPeakFinderOptions.UseButterworthSmooth) ||
                sicPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth)
            {
                peaksContainer.YData.CopyTo(peaksContainer.SmoothedYData, 0);
                bool success;

                if (sicPeakFinderOptions.UseButterworthSmooth)
                {
                    // Filter the data with a Butterworth filter (.UseButterworthSmooth takes precedence over .UseSavitzkyGolaySmooth)
                    double butterWorthFrequency;

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

                    // Data was smoothed
                    // Validate that peakWidthPointsMinimum is large enough
                    if (butterWorthFrequency > 0)
                    {
                        var peakWidthPointsCompare = (int)Math.Round(1 / butterWorthFrequency, 0);

                        if (peakWidthPointsMinimum < peakWidthPointsCompare)
                        {
                            peakWidthPointsMinimum = peakWidthPointsCompare;
                        }
                    }

                    return true;
                }

                // Filter the data with a Savitzky Golay filter
                var filterThirdWidth = (int)Math.Floor(peaksContainer.PeakWidthPointsMinimum / 3.0);

                if (filterThirdWidth > 3)
                    filterThirdWidth = 3;

                // Make sure filterThirdWidth is Odd
                if (filterThirdWidth % 2 == 0)
                {
                    filterThirdWidth--;
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

                // Data was smoothed
                return true;
            }

            // Do not filter
            peaksContainer.YData.CopyTo(peaksContainer.SmoothedYData, 0);
            return false;
        }

        /// <summary>
        /// Compute the potential peak area for a given list of intensity values
        /// </summary>
        /// <param name="dataCount"></param>
        /// <param name="sicIntensities"></param>
        /// <param name="potentialAreaStats"></param>
        /// <param name="sicPeakFinderOptions"></param>
        public void FindPotentialPeakArea(
            int dataCount,
            IReadOnlyList<double> sicIntensities,
            out SICPotentialAreaStats potentialAreaStats,
            SICPeakFinderOptions sicPeakFinderOptions)
        {
            var sicData = new List<SICDataPoint>(dataCount);

            for (var index = 0; index < dataCount; index++)
            {
                sicData.Add(new SICDataPoint(0, sicIntensities[index], 0));
            }

            FindPotentialPeakArea(sicData, out potentialAreaStats, sicPeakFinderOptions);
        }

        /// <summary>
        /// <para>
        /// Compute the potential peak area for a given SIC
        /// Stores the value in potentialAreaStats.MinimumPotentialPeakArea
        /// </para>
        /// <para>
        /// However, the summed intensity is not stored if the number of points
        /// greater than or equal to .SICBaselineNoiseOptions.MinimumBaselineNoiseLevel is less than Minimum_Peak_Width
        /// </para>
        /// </summary>
        /// <param name="sicData"></param>
        /// <param name="potentialAreaStats"></param>
        /// <param name="sicPeakFinderOptions"></param>
        public void FindPotentialPeakArea(
            IList<SICDataPoint> sicData,
            out SICPotentialAreaStats potentialAreaStats,
            SICPeakFinderOptions sicPeakFinderOptions)
        {
            // This queue is used to keep track of the most recent intensity values
            var intensityQueue = new Queue<double>();

            var minimumPotentialPeakArea = double.MaxValue;

            var peakCountBasisForMinimumPotentialArea = 0;

            if (sicData.Count > 0)
            {
                intensityQueue.Clear();
                double potentialPeakArea = 0;
                var validPeakCount = 0;

                // Find the minimum intensity in SICData()
                var minimumPositiveValue = FindMinimumPositiveValue(sicData, 1);

                foreach (var dataPoint in sicData)
                {
                    // If this data point is > .MinimumBaselineNoiseLevel, add this intensity to potentialPeakArea
                    // and increment validPeakCount
                    var intensityToUse = Math.Max(minimumPositiveValue, dataPoint.Intensity);

                    if (intensityToUse >= sicPeakFinderOptions.SICBaselineNoiseOptions.MinimumBaselineNoiseLevel)
                    {
                        potentialPeakArea += intensityToUse;
                        validPeakCount++;
                    }

                    if (intensityQueue.Count >= sicPeakFinderOptions.InitialPeakWidthScansMaximum)
                    {
                        // Decrement potentialPeakArea by the oldest item in the queue
                        // If that item is >= .MinimumBaselineNoiseLevel, decrement validPeakCount too
                        var oldestIntensity = intensityQueue.Dequeue();

                        if (oldestIntensity >= sicPeakFinderOptions.SICBaselineNoiseOptions.MinimumBaselineNoiseLevel && oldestIntensity > 0)
                        {
                            potentialPeakArea -= oldestIntensity;
                            validPeakCount--;
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

            potentialAreaStats = new SICPotentialAreaStats
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
        [Obsolete("Use the version that takes List<SICDataPoint>")]
        public bool FindSICPeakAndArea(
            int dataCount,
            int[] sicScanNumbers,
            double[] sicIntensities,
            out SICPotentialAreaStats potentialAreaStatsForPeak,
            SICStatsPeak sicPeak,
            out SmoothedYDataSubset smoothedYDataSubset,
            SICPeakFinderOptions sicPeakFinderOptions,
            SICPotentialAreaStats potentialAreaStatsForRegion,
            bool returnClosestPeak,
            bool simDataPresent,
            bool recomputeNoiseLevel)
        {
            var sicData = new List<SICDataPoint>(dataCount);

            for (var index = 0; index < dataCount; index++)
            {
                sicData.Add(new SICDataPoint(sicScanNumbers[index], sicIntensities[index], 0));
            }

            return FindSICPeakAndArea(sicData,
                                      out potentialAreaStatsForPeak, sicPeak,
                                      out smoothedYDataSubset, sicPeakFinderOptions,
                                      potentialAreaStatsForRegion,
                                      returnClosestPeak, simDataPresent, recomputeNoiseLevel);
        }

        /// <summary>
        /// Find SIC Peak and Area
        /// </summary>
        /// <remarks>
        /// The calling function should populate sicPeak.IndexObserved with the index in SICData() where the
        /// parent ion m/z was actually observed; this will be used as the default peak location if a peak cannot be found
        /// </remarks>
        /// <param name="sicData">Selected Ion Chromatogram data (scan, intensity, mass)</param>
        /// <param name="potentialAreaStatsForPeak">Output: potential area stats for the identified peak</param>
        /// <param name="sicPeak">Output: identified Peak</param>
        /// <param name="smoothedYDataSubset"></param>
        /// <param name="sicPeakFinderOptions"></param>
        /// <param name="potentialAreaStatsForRegion"></param>
        /// <param name="returnClosestPeak"></param>
        /// <param name="simDataPresent">Set to true if Select Ion Monitoring data is present and there are thus large gaps in the survey scan numbers</param>
        /// <param name="recomputeNoiseLevel"></param>
        public bool FindSICPeakAndArea(
            List<SICDataPoint> sicData,
            out SICPotentialAreaStats potentialAreaStatsForPeak,
            SICStatsPeak sicPeak,
            out SmoothedYDataSubset smoothedYDataSubset,
            SICPeakFinderOptions sicPeakFinderOptions,
            SICPotentialAreaStats potentialAreaStatsForRegion,
            bool returnClosestPeak,
            bool simDataPresent,
            bool recomputeNoiseLevel)
        {
            potentialAreaStatsForPeak = new SICPotentialAreaStats();
            smoothedYDataSubset = new SmoothedYDataSubset();

            try
            {
                // Compute the potential peak area for this SIC
                FindPotentialPeakArea(sicData, out potentialAreaStatsForPeak, sicPeakFinderOptions);

                // See if the potential peak area for this SIC is lower than the values for the Region
                // If so, update the region values with this peak's values
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

                    smoothedYDataSubset = new SmoothedYDataSubset();
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
                        ComputeNoiseLevelForSICData(
                        sicData.Count, intensities,
                        sicPeakFinderOptions.SICBaselineNoiseOptions,
                        out var baselineNoiseStats);
                        sicPeak.BaselineNoiseStats = baselineNoiseStats;
                    }

                    // Use a peak-finder algorithm to find the peak closest to .Peak.IndexMax
                    // Note that .Peak.IndexBaseLeft, .Peak.IndexBaseRight, and .Peak.IndexMax are passed ByRef and get updated by FindPeaks
                    var peakIndexStart = sicPeak.IndexBaseLeft;
                    var peakIndexEnd = sicPeak.IndexBaseRight;
                    var peakLocationIndex = sicPeak.IndexMax;
                    var previousPeakFWHMPointRight = sicPeak.PreviousPeakFWHMPointRight;
                    var nextPeakFWHMPointLeft = sicPeak.NextPeakFWHMPointLeft;
                    var shoulderCount = sicPeak.ShoulderCount;

                    var success = FindPeaks(
                        sicData, ref peakIndexStart, ref peakIndexEnd, ref peakLocationIndex,
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
                            ComputeNoiseLevelInPeakVicinity(
                                sicData, sicPeak,
                                sicPeakFinderOptions.SICBaselineNoiseOptions);
                        }

                        // // Compute the trimmed median of the data in SICData (replacing non-positive values with the minimum)
                        // // If the median is less than sicPeak.BaselineNoiseStats.NoiseLevel, update sicPeak.BaselineNoiseStats.NoiseLevel
                        // noiseOptionsOverride.BaselineNoiseMode = NoiseThresholdModes.TrimmedMedianByAbundance;
                        // noiseOptionsOverride.TrimmedMeanFractionLowIntensityDataToAverage = 0.75;
                        //
                        // success = ComputeNoiseLevelForSICData[sicData, noiseOptionsOverride, noiseStatsCompare];
                        // if (noiseStatsCompare.PointsUsed >= MINIMUM_NOISE_SCANS_REQUIRED)
                        // {
                        //    // Check whether the comparison noise level is less than the existing noise level times 0.75
                        //    if (noiseStatsCompare.NoiseLevel < sicPeak.BaselineNoiseStats.NoiseLevel * 0.75)
                        //    {
                        //        // Yes, the comparison noise level is lower
                        //        // Use a T-Test to see if the comparison noise level is significantly different from the primary noise level
                        //        if (TestSignificanceUsingTTest[noiseStatsCompare.NoiseLevel, sicPeak.BaselineNoiseStats.NoiseLevel, noiseStatsCompare.NoiseStDev, sicPeak.BaselineNoiseStats.NoiseStDev, noiseStatsCompare.PointsUsed, sicPeak.BaselineNoiseStats.PointsUsed, TTestConfidenceLevelConstants.Conf95Pct, tCalculated])
                        //        {
                        //            sicPeak.BaselineNoiseStats = noiseStatsCompare;
                        //        }
                        //    }
                        // }

                        // If smoothing was enabled, see if the smoothed value is larger than sicPeak.MaxIntensityValue
                        // If it is, use the smoothed value for sicPeak.MaxIntensityValue
                        if (sicPeakFinderOptions.UseSavitzkyGolaySmooth || sicPeakFinderOptions.UseButterworthSmooth)
                        {
                            var dataIndex = sicPeak.IndexMax - smoothedYDataSubset.DataStartIndex;

                            if (dataIndex >= 0 && smoothedYDataSubset.Data != null &&
                                dataIndex < smoothedYDataSubset.DataCount)
                            {
                                // Possibly use the intensity of the smoothed data as the peak intensity
                                var intensityCompare = smoothedYDataSubset.Data[dataIndex];

                                if (intensityCompare > sicPeak.MaxIntensityValue)
                                {
                                    sicPeak.MaxIntensityValue = intensityCompare;
                                }
                            }
                        }

                        // Compute the signal-to-noise ratio for the peak
                        sicPeak.SignalToNoiseRatio = ComputeSignalToNoise(sicPeak.MaxIntensityValue, sicPeak.BaselineNoiseStats.NoiseLevel);

                        // Compute the Full Width at Half Max (fwhm) value, this time subtracting the noise level from the baseline
                        sicPeak.FWHMScanWidth = ComputeFWHM(sicData, sicPeak, true);

                        // Compute the Area (this function uses .FWHMScanWidth and therefore needs to be called after ComputeFWHM)
                        ComputeSICPeakArea(sicData, sicPeak);

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

        /// <summary>
        /// Get the default noise threshold options
        /// </summary>
        public static BaselineNoiseOptions GetDefaultNoiseThresholdOptions()
        {
            return new BaselineNoiseOptions
            {
                BaselineNoiseMode = NoiseThresholdModes.TrimmedMedianByAbundance,
                BaselineNoiseLevelAbsolute = 0,
                MinimumSignalToNoiseRatio = 0,                      // ToDo: Figure out how best to use this when > 0; for now, the SICNoiseMinimumSignalToNoiseRatio property ignores any attempts to set this value
                MinimumBaselineNoiseLevel = 1,
                TrimmedMeanFractionLowIntensityDataToAverage = 0.75,
                DualTrimmedMeanStdDevLimits = 5,
                DualTrimmedMeanMaximumSegments = 3
            };
        }

        /// <summary>
        /// Get the default peak finder options
        /// </summary>
        public static SICPeakFinderOptions GetDefaultSICPeakFinderOptions()
        {
            var sicPeakFinderOptions = new SICPeakFinderOptions
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

                UseSavitzkyGolaySmooth = false,
                SavitzkyGolayFilterOrder = 0,                               // Moving average filter if 0, Savitzky Golay filter if 2, 4, 6, etc.

                // Set the default Mass Spectra noise threshold options
                MassSpectraNoiseThresholdOptions = GetDefaultNoiseThresholdOptions()
            };

            // Customize a few values
            sicPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = NoiseThresholdModes.TrimmedMedianByAbundance;

            sicPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = NoiseThresholdModes.TrimmedMedianByAbundance;
            sicPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = 0.5;
            sicPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = 2;

            return sicPeakFinderOptions;
        }

        private string GetVersionForExecutingAssembly()
        {
            try
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            catch (Exception)
            {
                return "??.??.??.??";
            }
        }

        /// <summary>
        /// Initialize the baseline noise stats
        /// </summary>
        /// <param name="minimumBaselineNoiseLevel"></param>
        /// <param name="noiseThresholdMode"></param>
        public static BaselineNoiseStats InitializeBaselineNoiseStats(
            double minimumBaselineNoiseLevel,
            NoiseThresholdModes noiseThresholdMode)
        {
            return new BaselineNoiseStats
            {
                NoiseLevel = minimumBaselineNoiseLevel,
                NoiseStDev = 0,
                PointsUsed = 0,
                NoiseThresholdModeUsed = noiseThresholdMode
            };
        }

        /// <summary>
        /// Initialize the baseline noise stats
        /// </summary>
        /// <param name="baselineNoiseStats"></param>
        /// <param name="minimumBaselineNoiseLevel"></param>
        /// <param name="noiseThresholdMode"></param>
        [Obsolete("Use the version that returns baselineNoiseStatsType")]
        public static void InitializeBaselineNoiseStats(
            out BaselineNoiseStats baselineNoiseStats,
            double minimumBaselineNoiseLevel,
            NoiseThresholdModes noiseThresholdMode)
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
            var deltaY = y2 - y1;                                 // This is y-two minus y-one
            var ratio = (targetY - y1) / deltaY;
            var deltaX = x2 - x1;                                 // This is x-two minus x-one

            var targetX = ratio * deltaX + x1;

            if (Math.Abs(targetX - x1) >= 0 && Math.Abs(targetX - x2) >= 0)
            {
                interpolatedXValue = targetX;
                return true;
            }

            LogErrors("clsMasicPeakFinder->InterpolateX", "TargetX is not between X1 and X2; this shouldn't happen", null, false);
            interpolatedXValue = 0;
            return false;
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
        private bool InterpolateY(
            out double interpolatedIntensity,
            int X1, int X2,
            double Y1, double Y2,
            double xValToInterpolate)
        {
            var scanDifference = X2 - X1;

            if (scanDifference != 0)
            {
                interpolatedIntensity = Y1 + (Y2 - Y1) * ((xValToInterpolate - X1) / scanDifference);
                return true;
            }

            // xValToInterpolate is not between X1 and X2; cannot interpolate
            interpolatedIntensity = 0;
            return false;
        }

        private void LogErrors(
            string source,
            string message,
            Exception ex,
            bool allowThrowingException = true)
        {
            mStatusMessage = message;

            var messageWithoutCRLF = mStatusMessage.Replace(Environment.NewLine, "; ");

            OnErrorEvent(source + ": " + messageWithoutCRLF, ex);

            if (allowThrowingException)
            {
                throw new Exception(mStatusMessage, ex);
            }
        }

        /// <summary>
        /// Lookup noise stats for a given data point, using segment-based noise stats
        /// </summary>
        /// <param name="scanIndexObserved"></param>
        /// <param name="noiseStatsSegments"></param>
        public BaselineNoiseStats LookupNoiseStatsUsingSegments(
            int scanIndexObserved,
            List<BaselineNoiseStatsSegment> noiseStatsSegments)
        {
            BaselineNoiseStats baselineNoiseStats = null;
            var segmentMidPointA = 0;
            var segmentMidPointB = 0;

            try
            {
                if (noiseStatsSegments == null || noiseStatsSegments.Count < 1)
                {
                    return InitializeBaselineNoiseStats(
                        GetDefaultNoiseThresholdOptions().MinimumBaselineNoiseLevel,
                        NoiseThresholdModes.DualTrimmedMeanByAbundance);
                }

                if (noiseStatsSegments.Count <= 1)
                {
                    return noiseStatsSegments.First().BaselineNoiseStats;
                }

                // First, initialize to the first segment
                baselineNoiseStats = noiseStatsSegments.First().BaselineNoiseStats.Clone();

                // Initialize indexSegmentA and indexSegmentB to 0, indicating no extrapolation needed
                var indexSegmentA = 0;
                var indexSegmentB = 0;
                var matchFound = false;                // Next, see if scanIndexObserved matches any of the segments (provided more than one segment exists)
                for (var noiseSegmentIndex = 0; noiseSegmentIndex < noiseStatsSegments.Count; noiseSegmentIndex++)
                {
                    var current = noiseStatsSegments[noiseSegmentIndex];

                    if (scanIndexObserved >= current.SegmentIndexStart && scanIndexObserved <= current.SegmentIndexEnd)
                    {
                        segmentMidPointA = current.SegmentIndexStart + (int)Math.Round((current.SegmentIndexEnd - current.SegmentIndexStart) / 2.0);
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
                                segmentMidPointA = previous.SegmentIndexStart + (int)Math.Round((previous.SegmentIndexEnd - previous.SegmentIndexStart) / 2.0);
                            }
                            // else
                            // {
                            //    // scanIndexObserved occurs before the midpoint, but we're in the first segment; no need to Interpolate
                            // }
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

                                segmentMidPointB = nextSegment.SegmentIndexStart + (int)Math.Round((nextSegment.SegmentIndexEnd - nextSegment.SegmentIndexStart) / 2.0);
                            }
                            // else
                            // {
                            //     // scanIndexObserved occurs after the midpoint, but we're in the last segment; no need to Interpolate
                            // }
                        }
                        // else
                        // {
                        //     // scanIndexObserved occurs at the midpoint; no need to Interpolate
                        // }

                        if (indexSegmentA != indexSegmentB)
                        {
                            // Interpolate between the two segments
                            var fractionFromSegmentB = (scanIndexObserved - segmentMidPointA) / (double)(segmentMidPointB - segmentMidPointA);

                            if (fractionFromSegmentB < 0)
                            {
                                fractionFromSegmentB = 0;
                            }
                            else if (fractionFromSegmentB > 1)
                            {
                                fractionFromSegmentB = 1;
                            }

                            var fractionFromSegmentA = 1 - fractionFromSegmentB;

                            // Compute the weighted average values
                            var segmentA = noiseStatsSegments[indexSegmentA].BaselineNoiseStats;
                            var segmentB = noiseStatsSegments[indexSegmentB].BaselineNoiseStats;

                            baselineNoiseStats.NoiseLevel = segmentA.NoiseLevel * fractionFromSegmentA + segmentB.NoiseLevel * fractionFromSegmentB;
                            baselineNoiseStats.NoiseStDev = segmentA.NoiseStDev * fractionFromSegmentA + segmentB.NoiseStDev * fractionFromSegmentB;
                            baselineNoiseStats.PointsUsed = (int)Math.Round(segmentA.PointsUsed * fractionFromSegmentA + segmentB.PointsUsed * fractionFromSegmentB);
                        }

                        break;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore Errors
            }

            return baselineNoiseStats;
        }

        /// <summary>
        /// Looks for the minimum positive value in dataListSorted and replaces all values of 0 in dataListSorted with the minimumPositiveValue
        /// </summary>
        /// <remarks>Assumes data in dataListSorted() is sorted ascending</remarks>
        /// <param name="dataCount"></param>
        /// <param name="dataListSorted"></param>
        /// <returns>Minimum positive value</returns>
        private double ReplaceSortedDataWithMinimumPositiveValue(int dataCount, IList<double> dataListSorted)
        {
            // Find the minimum positive value in dataListSorted
            // Since it's sorted, we can stop at the first non-zero value

            var indexFirstPositiveValue = -1;
            double minimumPositiveValue = 0;

            for (var i = 0; i < dataCount; i++)
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

            for (var i = indexFirstPositiveValue; i >= 0; i--)
            {
                dataListSorted[i] = minimumPositiveValue;
            }

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
            TTestConfidenceLevelConstants confidenceLevel,
            out double tCalculated)
        {
            // To use the t-test you must use sample variance values, not population variance values
            // Note: Variance_Sample = Sum((x-mean)^2) / (count-1)
            // Note: Sigma = SquareRoot(Variance_Sample)

            if (dataCount1 + dataCount2 <= 2)
            {
                // Cannot compute the T-Test
                tCalculated = 0;
                return false;
            }

            var sPooled = Math.Sqrt((Math.Pow(stDev1, 2) * (dataCount1 - 1) + Math.Pow(stDev2, 2) * (dataCount2 - 1)) / (dataCount1 + dataCount2 - 2));
            tCalculated = (mean1 - mean2) / sPooled * Math.Sqrt(dataCount1 * dataCount2 / (double)(dataCount1 + dataCount2));

            var confidenceLevelIndex = (int)confidenceLevel;

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

            // Differences are not significant
            return false;
        }
    }
}
