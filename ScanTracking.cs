using System;
using System.Collections.Generic;
using MASIC.Data;
using MASIC.DatasetStats;
using MASIC.Options;
using MASICPeakFinder;

namespace MASIC
{
    /// <summary>
    /// Class for tracking scan metadata
    /// </summary>
    public class ScanTracking : MasicEventNotifier
    {
        // Ignore Spelling: MASIC

        /// <summary>
        /// Absolute maximum number of ions that will be tracked for a mass spectrum
        /// </summary>
        private const int MAX_ALLOWABLE_ION_COUNT = 50000;

        /// <summary>
        /// Scan Stats list
        /// </summary>
        public List<ScanStatsEntry> ScanStats { get; }

        private readonly ReporterIons mReporterIons;

        private readonly clsMASICPeakFinder mPeakFinder;

        private int mSpectraFoundExceedingMaxIonCount;

        private int mMaxIonCountReported;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reporterIons"></param>
        /// <param name="peakFinder"></param>
        public ScanTracking(ReporterIons reporterIons, clsMASICPeakFinder peakFinder)
        {
            mReporterIons = reporterIons;
            mPeakFinder = peakFinder;

            ScanStats = new List<ScanStatsEntry>();
        }

        /// <summary>
        /// Check whether the scan number is within the range specified by sicOptions
        /// </summary>
        /// <param name="scanNumber"></param>
        /// <param name="sicOptions"></param>
        /// <returns>True if filtering is disabled, or if scanNumber is within the limits</returns>
        public bool CheckScanInRange(
            int scanNumber,
            SICOptions sicOptions)
        {
            // ReSharper disable once InvertIf
            if (sicOptions.ScanRangeStart >= 0 && sicOptions.ScanRangeEnd > sicOptions.ScanRangeStart)
            {
                if (scanNumber < sicOptions.ScanRangeStart || scanNumber > sicOptions.ScanRangeEnd)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check whether the scan number and elution time are within the ranges specified by sicOptions
        /// </summary>
        /// <param name="scanNumber"></param>
        /// <param name="elutionTime"></param>
        /// <param name="sicOptions"></param>
        /// <returns>True if filtering is disabled, or if scanNumber and elutionTime are within the limits</returns>
        public bool CheckScanInRange(
            int scanNumber,
            double elutionTime,
            SICOptions sicOptions)
        {
            if (!CheckScanInRange(scanNumber, sicOptions))
            {
                return false;
            }

            if (sicOptions.RTRangeStart >= 0 && sicOptions.RTRangeEnd > sicOptions.RTRangeStart)
            {
                if (elutionTime < sicOptions.RTRangeStart || elutionTime > sicOptions.RTRangeEnd)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Resizes the lists to a set capacity (unless there are existing contents larger than <paramref name="scanCount"/>)
        /// </summary>
        /// <param name="scanCount"></param>
        public void ReserveListCapacity(int scanCount)
        {
            ScanStats.Capacity = Math.Max(ScanStats.Count, scanCount);
        }

        /// <summary>
        /// Reduces memory usage by resizing the list to the contents
        /// </summary>
        public void SetListCapacityToCount()
        {
            ScanStats.Capacity = ScanStats.Count;
        }

        private void CompressSpectraData(
            MSSpectrum msSpectrum,
            double msDataResolution,
            double mzIgnoreRangeStart,
            double mzIgnoreRangeEnd)
        {
            // First, look for blocks of data points that consecutively have an intensity value of 0
            // For each block of data found, reduce the data to only retain the first data point and last data point in the block
            //
            // Next, look for data points in msSpectrum that are within msDataResolution units of one another (m/z units)
            // If found, combine into just one data point, keeping the largest intensity and the m/z value corresponding to the largest intensity

            if (msSpectrum.IonCount <= 1)
            {
                return;
            }

            // Look for blocks of data points that all have an intensity value of 0
            var targetIndex = 0;
            var index = 0;

            while (index < msSpectrum.IonCount)
            {
                if (msSpectrum.IonsIntensity[index] < float.Epsilon)
                {
                    var countCombined = 0;

                    for (var comparisonIndex = index + 1; comparisonIndex < msSpectrum.IonCount; comparisonIndex++)
                    {
                        if (msSpectrum.IonsIntensity[comparisonIndex] < float.Epsilon)
                        {
                            countCombined++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (countCombined > 1)
                    {
                        // Only keep the first and last data point in the block

                        msSpectrum.IonsMZ[targetIndex] = msSpectrum.IonsMZ[index];
                        msSpectrum.IonsIntensity[targetIndex] = msSpectrum.IonsIntensity[index];

                        targetIndex++;
                        msSpectrum.IonsMZ[targetIndex] = msSpectrum.IonsMZ[index + countCombined];
                        msSpectrum.IonsIntensity[targetIndex] = msSpectrum.IonsIntensity[index + countCombined];

                        index += countCombined;
                    }
                    // Keep this data point since a single zero
                    else if (targetIndex != index)
                    {
                        msSpectrum.IonsMZ[targetIndex] = msSpectrum.IonsMZ[index];
                        msSpectrum.IonsIntensity[targetIndex] = msSpectrum.IonsIntensity[index];
                    }
                }

                // Note: targetIndex will be the same as index until the first time that data is combined (countCombined > 0)
                // After that, targetIndex will always be less than index, and we will thus always need to copy data
                else if (targetIndex != index)
                {
                    msSpectrum.IonsMZ[targetIndex] = msSpectrum.IonsMZ[index];
                    msSpectrum.IonsIntensity[targetIndex] = msSpectrum.IonsIntensity[index];
                }

                index++;
                targetIndex++;
            }

            // Update .IonCount with the new data count
            msSpectrum.ShrinkArrays(targetIndex);

            // Step through the data, consolidating data within msDataResolution
            // Note that we're copying in place rather than making a new, duplicate array
            // If the m/z value is between mzIgnoreRangeStart and mzIgnoreRangeEnd, we will not compress the data

            targetIndex = 0;
            index = 0;

            while (index < msSpectrum.IonCount)
            {
                var countCombined = 0;
                var bestMz = msSpectrum.IonsMZ[index];

                // Only combine data if the first data point has a positive intensity value
                if (msSpectrum.IonsIntensity[index] > 0)
                {
                    var pointInIgnoreRange = Utilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ[index], mzIgnoreRangeStart, mzIgnoreRangeEnd);

                    if (!pointInIgnoreRange)
                    {
                        for (var comparisonIndex = index + 1; comparisonIndex < msSpectrum.IonCount; comparisonIndex++)
                        {
                            if (Utilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ[comparisonIndex], mzIgnoreRangeStart, mzIgnoreRangeEnd))
                            {
                                // Reached the ignore range; do not allow to be combined with the current data point
                                break;
                            }

                            if (msSpectrum.IonsMZ[comparisonIndex] - msSpectrum.IonsMZ[index] < msDataResolution)
                            {
                                if (msSpectrum.IonsIntensity[comparisonIndex] > msSpectrum.IonsIntensity[index])
                                {
                                    msSpectrum.IonsIntensity[index] = msSpectrum.IonsIntensity[comparisonIndex];
                                    bestMz = msSpectrum.IonsMZ[comparisonIndex];
                                }

                                countCombined++;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                // Note: targetIndex will be the same as index until the first time that data is combined (countCombined > 0)
                // After that, targetIndex will always be less than index, and we will thus always need to copy data
                if (targetIndex != index || countCombined > 0)
                {
                    msSpectrum.IonsMZ[targetIndex] = bestMz;
                    msSpectrum.IonsIntensity[targetIndex] = msSpectrum.IonsIntensity[index];

                    index += countCombined;
                }

                index++;
                targetIndex++;
            }

            // Update .IonCount with the new data count
            msSpectrum.ShrinkArrays(targetIndex);
        }

        private void ComputeNoiseLevelForMassSpectrum(
            ScanInfo scanInfo,
            MSSpectrum msSpectrum,
            BaselineNoiseOptions noiseThresholdOptions)
        {
            const bool IGNORE_NON_POSITIVE_DATA = true;

            scanInfo.BaselineNoiseStats = clsMASICPeakFinder.InitializeBaselineNoiseStats(0, noiseThresholdOptions.BaselineNoiseMode);

            if (noiseThresholdOptions.BaselineNoiseMode == clsMASICPeakFinder.NoiseThresholdModes.AbsoluteThreshold)
            {
                scanInfo.BaselineNoiseStats.NoiseLevel = noiseThresholdOptions.BaselineNoiseLevelAbsolute;
                scanInfo.BaselineNoiseStats.PointsUsed = 1;
            }
            else if (msSpectrum.IonCount > 0)
            {
                mPeakFinder.ComputeTrimmedNoiseLevel(
                    msSpectrum.IonsIntensity, 0, msSpectrum.IonCount - 1,
                    noiseThresholdOptions, IGNORE_NON_POSITIVE_DATA,
                    out var newBaselineNoiseStats);

                scanInfo.BaselineNoiseStats = newBaselineNoiseStats;
            }
        }

        /// <summary>
        /// Process and store a spectrum
        /// </summary>
        /// <param name="scanInfo"></param>
        /// <param name="dataImportUtilities"></param>
        /// <param name="spectraCache"></param>
        /// <param name="msSpectrum"></param>
        /// <param name="noiseThresholdOptions"></param>
        /// <param name="discardLowIntensityData"></param>
        /// <param name="compressData"></param>
        /// <param name="msDataResolution"></param>
        /// <param name="keepRawSpectrum"></param>
        /// <returns>True if success, false if an error</returns>
        public bool ProcessAndStoreSpectrum(
            ScanInfo scanInfo,
            DataInput.DataImport dataImportUtilities,
            SpectraCache spectraCache,
            MSSpectrum msSpectrum,
            BaselineNoiseOptions noiseThresholdOptions,
            bool discardLowIntensityData,
            bool compressData,
            double msDataResolution,
            bool keepRawSpectrum)
        {
            var lastKnownLocation = "Start";

            try
            {
                // Determine the noise threshold intensity for this spectrum
                // Stored in scanInfo.BaselineNoiseStats
                lastKnownLocation = "Call ComputeNoiseLevelForMassSpectrum";
                ComputeNoiseLevelForMassSpectrum(scanInfo, msSpectrum, noiseThresholdOptions);

                if (!keepRawSpectrum)
                {
                    return true;
                }

                // Discard low intensity data, but not for MRM scans
                if (discardLowIntensityData && scanInfo.MRMScanType == ThermoRawFileReader.MRMScanTypeConstants.NotMRM)
                {
                    // Discard data below the noise level or below the minimum S/N level
                    // If we are searching for Reporter ions, it is important to not discard any of the ions in the region of the reporter ion m/z values
                    lastKnownLocation = "Call DiscardDataBelowNoiseThreshold";
                    dataImportUtilities.DiscardDataBelowNoiseThreshold(
                        msSpectrum,
                        scanInfo.BaselineNoiseStats.NoiseLevel,
                        mReporterIons.MZIntensityFilterIgnoreRangeStart,
                        mReporterIons.MZIntensityFilterIgnoreRangeEnd,
                        noiseThresholdOptions);

                    scanInfo.IonCount = msSpectrum.IonCount;
                }

                if (compressData)
                {
                    lastKnownLocation = "Call CompressSpectraData";
                    // Again, if we are searching for Reporter ions, it is important to not discard any of the ions in the region of the reporter ion m/z values
                    CompressSpectraData(msSpectrum, msDataResolution,
                                        mReporterIons.MZIntensityFilterIgnoreRangeStart,
                                        mReporterIons.MZIntensityFilterIgnoreRangeEnd);
                }

                if (msSpectrum.IonCount > MAX_ALLOWABLE_ION_COUNT)
                {
                    // Do not keep more than 50,000 ions
                    lastKnownLocation = "Call DiscardDataToLimitIonCount";
                    mSpectraFoundExceedingMaxIonCount++;

                    // Display a message at the console the first 10 times we encounter spectra with over MAX_ALLOWABLE_ION_COUNT ions
                    // In addition, display a new message every time a new max value is encountered
                    if (mSpectraFoundExceedingMaxIonCount <= 10 || msSpectrum.IonCount > mMaxIonCountReported)
                    {
                        Console.WriteLine();
                        Console.WriteLine(
                            "Note: Scan " + scanInfo.ScanNumber + " has " + msSpectrum.IonCount + " ions; " +
                            "will only retain " + MAX_ALLOWABLE_ION_COUNT + " (trimmed " +
                            mSpectraFoundExceedingMaxIonCount + " spectra)");

                        mMaxIonCountReported = msSpectrum.IonCount;
                    }

                    dataImportUtilities.DiscardDataToLimitIonCount(
                        msSpectrum,
                        mReporterIons.MZIntensityFilterIgnoreRangeStart,
                        mReporterIons.MZIntensityFilterIgnoreRangeEnd,
                        MAX_ALLOWABLE_ION_COUNT);

                    scanInfo.IonCount = msSpectrum.IonCount;
                }

                lastKnownLocation = "Call AddSpectrumToPool";
                return spectraCache.AddSpectrumToPool(msSpectrum, scanInfo.ScanNumber);
            }
            catch (Exception ex)
            {
                ReportError("Error in ProcessAndStoreSpectrum (LastKnownLocation: " + lastKnownLocation + ")", ex, clsMASIC.MasicErrorCodes.InputFileDataReadError);
                return false;
            }
        }
    }
}
