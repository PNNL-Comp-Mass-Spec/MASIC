using System;
using System.Collections.Generic;
using MASIC.DatasetStats;
using MASICPeakFinder;

namespace MASIC
{
    public class clsScanTracking : clsMasicEventNotifier
    {
        #region // TODO
        // Absolute maximum number of ions that will be tracked for a mass spectrum
        private const int MAX_ALLOWABLE_ION_COUNT = 50000;

        #endregion
        #region // TODO
        public List<ScanStatsEntry> ScanStats { get; private set; }
        #endregion
        #region // TODO
        private readonly clsReporterIons mReporterIons;
        private readonly clsMASICPeakFinder mPeakFinder;
        private int mSpectraFoundExceedingMaxIonCount = 0;
        private int mMaxIonCountReported = 0;

        #endregion
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reporterIons"></param>
        /// <param name="peakFinder"></param>
        public clsScanTracking(clsReporterIons reporterIons, clsMASICPeakFinder peakFinder)
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
        public bool CheckScanInRange(int scanNumber, clsSICOptions sicOptions)
        {
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
        public bool CheckScanInRange(int scanNumber, double elutionTime, clsSICOptions sicOptions)
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

        private void CompressSpectraData(clsMSSpectrum msSpectrum, double msDataResolution, double mzIgnoreRangeStart, double mzIgnoreRangeEnd)
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
            int targetIndex = 0;
            int index = 0;
            while (index < msSpectrum.IonCount)
            {
                if (msSpectrum.IonsIntensity[index] < float.Epsilon)
                {
                    int countCombined = 0;
                    for (int comparisonIndex = index + 1, loopTo = msSpectrum.IonCount - 1; comparisonIndex <= loopTo; comparisonIndex++)
                    {
                        if (msSpectrum.IonsIntensity[comparisonIndex] < float.Epsilon)
                        {
                            countCombined += 1;
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
                        targetIndex += 1;
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
                // After that, targetIndex will always be less than index and we will thus always need to copy data
                else if (targetIndex != index)
                {
                    msSpectrum.IonsMZ[targetIndex] = msSpectrum.IonsMZ[index];
                    msSpectrum.IonsIntensity[targetIndex] = msSpectrum.IonsIntensity[index];
                }

                index += 1;
                targetIndex += 1;
            }

            // Update .IonCount with the new data count
            msSpectrum.ShrinkArrays(targetIndex);

            // Step through the data, consolidating data within msDataResolution
            // Note that we're copying in place rather than making a new, duplicate array
            // If the m/z value is between mzIgnoreRangeStart and mzIgnoreRangeEnd, then we will not compress the data

            targetIndex = 0;
            index = 0;
            while (index < msSpectrum.IonCount)
            {
                int countCombined = 0;
                double bestMz = msSpectrum.IonsMZ[index];

                // Only combine data if the first data point has a positive intensity value
                if (msSpectrum.IonsIntensity[index] > 0)
                {
                    bool pointInIgnoreRange = clsUtilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ[index], mzIgnoreRangeStart, mzIgnoreRangeEnd);
                    if (!pointInIgnoreRange)
                    {
                        for (int comparisonIndex = index + 1, loopTo1 = msSpectrum.IonCount - 1; comparisonIndex <= loopTo1; comparisonIndex++)
                        {
                            if (clsUtilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ[comparisonIndex], mzIgnoreRangeStart, mzIgnoreRangeEnd))
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

                                countCombined += 1;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                // Note: targetIndex will be the same as index until the first time that data is combined (countCombined > 0)
                // After that, targetIndex will always be less than index and we will thus always need to copy data
                if (targetIndex != index || countCombined > 0)
                {
                    msSpectrum.IonsMZ[targetIndex] = bestMz;
                    msSpectrum.IonsIntensity[targetIndex] = msSpectrum.IonsIntensity[index];
                    index += countCombined;
                }

                index += 1;
                targetIndex += 1;
            }

            // Update .IonCount with the new data count
            msSpectrum.ShrinkArrays(targetIndex);
        }

        private void ComputeNoiseLevelForMassSpectrum(clsScanInfo scanInfo, clsMSSpectrum msSpectrum, clsBaselineNoiseOptions noiseThresholdOptions)
        {
            const bool IGNORE_NON_POSITIVE_DATA = true;
            scanInfo.BaselineNoiseStats = clsMASICPeakFinder.InitializeBaselineNoiseStats(0, noiseThresholdOptions.BaselineNoiseMode);
            if (noiseThresholdOptions.BaselineNoiseMode == clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold)
            {
                scanInfo.BaselineNoiseStats.NoiseLevel = noiseThresholdOptions.BaselineNoiseLevelAbsolute;
                scanInfo.BaselineNoiseStats.PointsUsed = 1;
            }
            else if (msSpectrum.IonCount > 0)
            {
                clsBaselineNoiseStats newBaselineNoiseStats = null;
                mPeakFinder.ComputeTrimmedNoiseLevel(msSpectrum.IonsIntensity, 0, msSpectrum.IonCount - 1, noiseThresholdOptions, IGNORE_NON_POSITIVE_DATA, out newBaselineNoiseStats);
                scanInfo.BaselineNoiseStats = newBaselineNoiseStats;
            }
        }

        public bool ProcessAndStoreSpectrum(clsScanInfo scanInfo, DataInput.clsDataImport dataImportUtilities, clsSpectraCache spectraCache, clsMSSpectrum msSpectrum, clsBaselineNoiseOptions noiseThresholdOptions, bool discardLowIntensityData, bool compressData, double msDataResolution, bool keepRawSpectrum)
        {
            string lastKnownLocation = "Start";
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
                    // If we are searching for Reporter ions, then it is important to not discard any of the ions in the region of the reporter ion m/z values
                    lastKnownLocation = "Call DiscardDataBelowNoiseThreshold";
                    dataImportUtilities.DiscardDataBelowNoiseThreshold(msSpectrum, scanInfo.BaselineNoiseStats.NoiseLevel, mReporterIons.MZIntensityFilterIgnoreRangeStart, mReporterIons.MZIntensityFilterIgnoreRangeEnd, noiseThresholdOptions);
                    scanInfo.IonCount = msSpectrum.IonCount;
                }

                if (compressData)
                {
                    lastKnownLocation = "Call CompressSpectraData";
                    // Again, if we are searching for Reporter ions, then it is important to not discard any of the ions in the region of the reporter ion m/z values
                    CompressSpectraData(msSpectrum, msDataResolution, mReporterIons.MZIntensityFilterIgnoreRangeStart, mReporterIons.MZIntensityFilterIgnoreRangeEnd);
                }

                if (msSpectrum.IonCount > MAX_ALLOWABLE_ION_COUNT)
                {
                    // Do not keep more than 50,000 ions
                    lastKnownLocation = "Call DiscardDataToLimitIonCount";
                    mSpectraFoundExceedingMaxIonCount += 1;

                    // Display a message at the console the first 10 times we encounter spectra with over MAX_ALLOWABLE_ION_COUNT ions
                    // In addition, display a new message every time a new max value is encountered
                    if (mSpectraFoundExceedingMaxIonCount <= 10 || msSpectrum.IonCount > mMaxIonCountReported)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Note: Scan " + scanInfo.ScanNumber + " has " + msSpectrum.IonCount + " ions; " + "will only retain " + MAX_ALLOWABLE_ION_COUNT + " (trimmed " + mSpectraFoundExceedingMaxIonCount.ToString() + " spectra)");
                        mMaxIonCountReported = msSpectrum.IonCount;
                    }

                    dataImportUtilities.DiscardDataToLimitIonCount(msSpectrum, mReporterIons.MZIntensityFilterIgnoreRangeStart, mReporterIons.MZIntensityFilterIgnoreRangeEnd, MAX_ALLOWABLE_ION_COUNT);
                    scanInfo.IonCount = msSpectrum.IonCount;
                }

                lastKnownLocation = "Call AddSpectrumToPool";
                bool success = spectraCache.AddSpectrumToPool(msSpectrum, scanInfo.ScanNumber);
                return success;
            }
            catch (Exception ex)
            {
                ReportError("Error in ProcessAndStoreSpectrum (LastKnownLocation: " + lastKnownLocation + ")", ex, clsMASIC.eMasicErrorCodes.InputFileDataReadError);
                return false;
            }
        }
    }
}