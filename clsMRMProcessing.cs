using System;
using System.Collections.Generic;
using System.IO;
using MASIC.DataOutput;
using MASICPeakFinder;
using PRISM;
using ThermoRawFileReader;

namespace MASIC
{
    public class clsMRMProcessing : clsMasicEventNotifier
    {
        #region "Structures"

        public struct udtSRMListType
        {
            public double ParentIonMZ;
            public double CentralMass;

            public override string ToString()
            {
                return "m/z " + ParentIonMZ.ToString("0.00");
            }
        }

        #endregion

        #region "Classwide variables"
        private readonly clsMASICOptions mOptions;
        private readonly clsDataAggregation mDataAggregation;
        private readonly clsDataOutput mDataOutputHandler;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public clsMRMProcessing(clsMASICOptions masicOptions, clsDataOutput dataOutputHandler)
        {
            mOptions = masicOptions;
            mDataAggregation = new clsDataAggregation();
            RegisterEvents(mDataAggregation);

            mDataOutputHandler = dataOutputHandler;
        }

        private string ConstructSRMMapKey(udtSRMListType udtSRMListEntry)
        {
            return ConstructSRMMapKey(udtSRMListEntry.ParentIonMZ, udtSRMListEntry.CentralMass);
        }

        private string ConstructSRMMapKey(double parentIonMZ, double centralMass)
        {
            return parentIonMZ.ToString("0.000") + "_to_" + centralMass.ToString("0.000");
        }

        private bool DetermineMRMSettings(
            clsScanList scanList,
            out List<clsMRMScanInfo> mrmSettings,
            out List<udtSRMListType> srmList)
        {
            // Returns true if this dataset has MRM data and if it is parsed successfully
            // Returns false if the dataset does not have MRM data, or if an error occurs

            mrmSettings = new List<clsMRMScanInfo>();
            srmList = new List<udtSRMListType>();

            try
            {
                var mrmDataPresent = false;
                UpdateProgress(0, "Determining MRM settings");

                // Initialize the tracking arrays
                var mrmHashToIndexMap = new Dictionary<string, clsMRMScanInfo>();

                // Construct a list of the MRM search values used
                foreach (var fragScan in scanList.FragScans)
                {
                    if (fragScan.MRMScanType == MRMScanTypeConstants.SRM)
                    {
                        mrmDataPresent = true;

                        // See if this MRM spec is already in mrmSettings

                        var mrmInfoHash = GenerateMRMInfoHash(fragScan.MRMScanInfo);

                        if (!mrmHashToIndexMap.TryGetValue(mrmInfoHash, out var mrmInfoForHash))
                        {
                            mrmInfoForHash = DuplicateMRMInfo(fragScan.MRMScanInfo);

                            mrmInfoForHash.ScanCount = 1;
                            mrmInfoForHash.ParentIonInfoIndex = fragScan.FragScanInfo.ParentIonInfoIndex;

                            mrmSettings.Add(mrmInfoForHash);
                            mrmHashToIndexMap.Add(mrmInfoHash, mrmInfoForHash);

                            // Append the new entries to srmList

                            for (var mrmMassIndex = 0; mrmMassIndex <= mrmInfoForHash.MRMMassCount - 1; mrmMassIndex++)
                            {
                                // Add this new transition to srmList() only if not already present
                                var matchFound = false;
                                foreach (var srmItem in srmList)
                                {
                                    if (MRMParentDaughterMatch(srmItem, mrmInfoForHash, mrmMassIndex))
                                    {
                                        matchFound = true;
                                        break;
                                    }
                                }

                                if (!matchFound)
                                {
                                    // Entry is not yet present; add it

                                    var newSRMItem = new udtSRMListType()
                                    {
                                        ParentIonMZ = mrmInfoForHash.ParentIonMZ,
                                        CentralMass = mrmInfoForHash.MRMMassList[mrmMassIndex].CentralMass
                                    };

                                    srmList.Add(newSRMItem);
                                }
                            }
                        }
                        else
                        {
                            mrmInfoForHash.ScanCount += 1;
                        }
                    }
                }

                if (mrmDataPresent)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ReportError("Error determining the MRM settings", ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }
        }

        public static clsMRMScanInfo DuplicateMRMInfo(
            MRMInfo oSource,
            double parentIonMZ)
        {
            var oTarget = new clsMRMScanInfo
            {
                ParentIonMZ = parentIonMZ,
                MRMMassCount = oSource.MRMMassList.Count,
                ScanCount = 0,
                ParentIonInfoIndex = -1,
        };

            if (oSource.MRMMassList == null)
            {
                oTarget.MRMMassList = new List<udtMRMMassRangeType>();
            }
            else
            {
                oTarget.MRMMassList = new List<udtMRMMassRangeType>(oSource.MRMMassList.Count);
                oTarget.MRMMassList.AddRange(oSource.MRMMassList);
            }

            return oTarget;
        }

        private clsMRMScanInfo DuplicateMRMInfo(clsMRMScanInfo oSource)
        {
            var oTarget = new clsMRMScanInfo
            {
                ParentIonMZ = oSource.ParentIonMZ,
                MRMMassCount = oSource.MRMMassCount,
                ScanCount = oSource.ScanCount,
                ParentIonInfoIndex = oSource.ParentIonInfoIndex,
            };

            if (oSource.MRMMassList == null)
            {
                oTarget.MRMMassList = new List<udtMRMMassRangeType>();
            }
            else
            {
                oTarget.MRMMassList = new List<udtMRMMassRangeType>(oSource.MRMMassList.Count);
                oTarget.MRMMassList.AddRange(oSource.MRMMassList);
            }

            return oTarget;
        }

        public bool ExportMRMDataToDisk(
            clsScanList scanList,
            clsSpectraCache spectraCache,
            string inputFileName,
            string outputDirectoryPath)
        {
            if (!DetermineMRMSettings(scanList, out var mrmSettings, out var srmList))
            {
                return false;
            }

            var success = ExportMRMDataToDisk(scanList, spectraCache, mrmSettings, srmList, inputFileName, outputDirectoryPath);

            return success;
        }

        private bool ExportMRMDataToDisk(
            clsScanList scanList,
            clsSpectraCache spectraCache,
            IReadOnlyList<clsMRMScanInfo> mrmSettings,
            IReadOnlyList<udtSRMListType> srmList,
            string inputFileName,
            string outputDirectoryPath)
        {
            // Returns true if the MRM data is successfully written to disk
            // Note that it will also return true if udtMRMSettings() is empty

            const char TAB_DELIMITER = '\t';

            StreamWriter dataWriter = null;
            StreamWriter crosstabWriter = null;

            bool success;

            try
            {
                // Only write this data if 1 or more fragmentation spectra are of type SRM
                if (mrmSettings == null || mrmSettings.Count == 0)
                {
                    return true;
                }

                UpdateProgress(0, "Exporting MRM data");

                // Write out the MRM Settings
                var mrmSettingsFilePath = clsDataOutput.ConstructOutputFilePath(
                    inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.MRMSettingsFile);
                using (var settingsWriter = new StreamWriter(mrmSettingsFilePath))
                {
                    settingsWriter.WriteLine(mDataOutputHandler.GetHeadersForOutputFile(scanList, clsDataOutput.eOutputFileTypeConstants.MRMSettingsFile));

                    var dataColumns = new List<string>();

                    for (var mrmInfoIndex = 0; mrmInfoIndex <= mrmSettings.Count - 1; mrmInfoIndex++)
                    {
                        var mrmSetting = mrmSettings[mrmInfoIndex];
                        for (var mrmMassIndex = 0; mrmMassIndex <= mrmSetting.MRMMassCount - 1; mrmMassIndex++)
                        {
                            dataColumns.Clear();

                            dataColumns.Add(mrmInfoIndex.ToString());
                            dataColumns.Add(mrmSetting.ParentIonMZ.ToString("0.000"));
                            dataColumns.Add(mrmSetting.MRMMassList[mrmMassIndex].CentralMass.ToString("0.000"));
                            dataColumns.Add(mrmSetting.MRMMassList[mrmMassIndex].StartMass.ToString("0.000"));
                            dataColumns.Add(mrmSetting.MRMMassList[mrmMassIndex].EndMass.ToString("0.000"));
                            dataColumns.Add(mrmSetting.ScanCount.ToString());

                            settingsWriter.WriteLine(string.Join(TAB_DELIMITER.ToString(), dataColumns));
                        }
                    }

                    if (mOptions.WriteMRMDataList || mOptions.WriteMRMIntensityCrosstab)
                    {
                        // Populate srmKeyToIndexMap
                        var srmKeyToIndexMap = new Dictionary<string, int>();
                        for (var srmIndex = 0; srmIndex <= srmList.Count - 1; srmIndex++)
                            srmKeyToIndexMap.Add(ConstructSRMMapKey(srmList[srmIndex]), srmIndex);

                        if (mOptions.WriteMRMDataList)
                        {
                            // Write out the raw MRM Data
                            var dataFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.MRMDatafile);
                            dataWriter = new StreamWriter(dataFilePath);

                            // Write the file headers
                            dataWriter.WriteLine(mDataOutputHandler.GetHeadersForOutputFile(scanList, clsDataOutput.eOutputFileTypeConstants.MRMDatafile));
                        }

                        if (mOptions.WriteMRMIntensityCrosstab)
                        {
                            // Write out the raw MRM Data
                            var crosstabFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.MRMCrosstabFile);
                            crosstabWriter = new StreamWriter(crosstabFilePath);

                            // Initialize the crosstab header variable using the data in udtSRMList()

                            var headerNames = new List<string>()
                            {
                                "Scan_First",
                                "ScanTime"
                            };

                            for (var srmIndex = 0; srmIndex <= srmList.Count - 1; srmIndex++)
                                headerNames.Add(ConstructSRMMapKey(srmList[srmIndex]));

                            crosstabWriter.WriteLine(string.Join(TAB_DELIMITER.ToString(), headerNames));
                        }

                        var scanFirst = int.MinValue;
                        float scanTimeFirst = 0;
                        var srmIndexLast = 0;

                        var crosstabColumnValue = new double[srmList.Count];
                        var crosstabColumnFlag = new bool[srmList.Count];

                        // For scanIndex = 0 To scanList.FragScanCount - 1
                        foreach (var fragScan in scanList.FragScans)
                        {
                            if (fragScan.MRMScanType != MRMScanTypeConstants.SRM)
                            {
                                continue;
                            }

                            if (scanFirst == int.MinValue)
                            {
                                scanFirst = fragScan.ScanNumber;
                                scanTimeFirst = fragScan.ScanTime;
                            }

                            // Look for each of the m/z values specified in fragScan.MRMScanInfo.MRMMassList
                            for (var mrmMassIndex = 0; mrmMassIndex <= fragScan.MRMScanInfo.MRMMassCount - 1; mrmMassIndex++)
                            {
                                // Find the maximum value between fragScan.StartMass and fragScan.EndMass
                                // Need to define a tolerance to account for numeric rounding artifacts in the variables

                                var mzStart = fragScan.MRMScanInfo.MRMMassList[mrmMassIndex].StartMass;
                                var mzEnd = fragScan.MRMScanInfo.MRMMassList[mrmMassIndex].EndMass;
                                var mrmToleranceHalfWidth = Math.Round((mzEnd - mzStart) / 2, 6);
                                if (mrmToleranceHalfWidth < 0.001)
                                {
                                    mrmToleranceHalfWidth = 0.001;
                                }

                                var matchFound = mDataAggregation.FindMaxValueInMZRange(
                                    spectraCache, fragScan,
                                    mzStart - mrmToleranceHalfWidth,
                                    mzEnd + mrmToleranceHalfWidth,
                                    out var closestMZ, out var matchIntensity);

                                if (mOptions.WriteMRMDataList)
                                {
                                    dataColumns.Clear();
                                    dataColumns.Add(fragScan.ScanNumber.ToString());
                                    dataColumns.Add(fragScan.MRMScanInfo.ParentIonMZ.ToString("0.000"));

                                    if (matchFound)
                                    {
                                        dataColumns.Add(fragScan.MRMScanInfo.MRMMassList[mrmMassIndex].CentralMass.ToString("0.000"));
                                        dataColumns.Add(matchIntensity.ToString("0.000"));
                                    }
                                    else
                                    {
                                        dataColumns.Add(fragScan.MRMScanInfo.MRMMassList[mrmMassIndex].CentralMass.ToString("0.000"));
                                        dataColumns.Add("0");
                                    }

                                    dataWriter.WriteLine(string.Join(TAB_DELIMITER.ToString(), dataColumns));
                                }

                                if (mOptions.WriteMRMIntensityCrosstab)
                                {
                                    var srmMapKey = ConstructSRMMapKey(fragScan.MRMScanInfo.ParentIonMZ, fragScan.MRMScanInfo.MRMMassList[mrmMassIndex].CentralMass);

                                    // Use srmKeyToIndexMap to determine the appropriate column index for srmMapKey
                                    if (srmKeyToIndexMap.TryGetValue(srmMapKey, out var srmIndex))
                                    {
                                        if (crosstabColumnFlag[srmIndex] ||
                                            srmIndex == 0 && srmIndexLast == srmList.Count - 1)
                                        {
                                            // Either the column is already populated, or the SRMIndex has cycled back to zero
                                            // Write out the current crosstab line and reset the crosstab column arrays
                                            ExportMRMDataWriteLine(crosstabWriter, scanFirst, scanTimeFirst,
                                                                   crosstabColumnValue,
                                                                   crosstabColumnFlag,
                                                                   TAB_DELIMITER, true);

                                            scanFirst = fragScan.ScanNumber;
                                            scanTimeFirst = fragScan.ScanTime;
                                        }

                                        if (matchFound)
                                        {
                                            crosstabColumnValue[srmIndex] = matchIntensity;
                                        }

                                        crosstabColumnFlag[srmIndex] = true;
                                        srmIndexLast = srmIndex;
                                    }
                                    else
                                    {
                                        // Unknown combination of parent ion m/z and daughter m/z; this is unexpected
                                        // We won't write this entry out
                                    }
                                }
                            }

                            UpdateCacheStats(spectraCache);
                            if (mOptions.AbortProcessing)
                            {
                                break;
                            }
                        }

                        if (mOptions.WriteMRMIntensityCrosstab)
                        {
                            // Write out any remaining crosstab values
                            ExportMRMDataWriteLine(crosstabWriter, scanFirst, scanTimeFirst, crosstabColumnValue, crosstabColumnFlag, TAB_DELIMITER, false);
                        }
                    }
                }

                success = true;
            }
            catch (Exception ex)
            {
                ReportError("Error writing the SRM data to disk", ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                success = false;
            }
            finally
            {
                dataWriter?.Close();
                crosstabWriter?.Close();
            }

            return success;
        }

        private void ExportMRMDataWriteLine(
            TextWriter writer,
            int scanFirst,
            float scanTimeFirst,
            IList<double> crosstabColumnValue,
            IList<bool> crosstabColumnFlag,
            char delimiter,
            bool forceWrite)
        {
            // If forceWrite = False, then will only write out the line if 1 or more columns is non-zero

            var nonZeroCount = 0;

            var dataColumns = new List<string>()
            {
                scanFirst.ToString(),
                StringUtilities.DblToString(scanTimeFirst, 5)
            };

            // Construct a tab-delimited list of the values
            // At the same time, clear the arrays
            for (var index = 0; index <= crosstabColumnValue.Count - 1; index++)
            {
                if (crosstabColumnValue[index] > 0)
                {
                    dataColumns.Add(crosstabColumnValue[index].ToString("0.000"));
                    nonZeroCount += 1;
                }
                else
                {
                    dataColumns.Add("0");
                }

                crosstabColumnValue[index] = 0;
                crosstabColumnFlag[index] = false;
            }

            if (nonZeroCount > 0 || forceWrite)
            {
                writer.WriteLine(string.Join(delimiter.ToString(), dataColumns));
            }
        }

        private string GenerateMRMInfoHash(clsMRMScanInfo mrmScanInfo)
        {
            var hashValue = mrmScanInfo.ParentIonMZ + "_" + mrmScanInfo.MRMMassCount;

            for (var index = 0; index <= mrmScanInfo.MRMMassCount - 1; index++)
                hashValue += "_" +
                    mrmScanInfo.MRMMassList[index].CentralMass.ToString("0.000") + "_" +
                    mrmScanInfo.MRMMassList[index].StartMass.ToString("0.000") + "_" +
                    mrmScanInfo.MRMMassList[index].EndMass.ToString("0.000");
            return hashValue;
        }

        private bool MRMParentDaughterMatch(
            udtSRMListType udtSRMListEntry,
            clsMRMScanInfo mrmSettingsEntry,
            int mrmMassIndex)
        {
            return MRMParentDaughterMatch(
                udtSRMListEntry.ParentIonMZ,
                udtSRMListEntry.CentralMass,
                mrmSettingsEntry.ParentIonMZ,
                mrmSettingsEntry.MRMMassList[mrmMassIndex].CentralMass);
        }

        private bool MRMParentDaughterMatch(
            double parentIonMZ1,
            double mrmDaughterMZ1,
            double parentIonMZ2,
            double mrmDaughterMZ2)
        {
            const double COMPARISON_TOLERANCE = 0.01;

            if (Math.Abs(parentIonMZ1 - parentIonMZ2) <= COMPARISON_TOLERANCE &&
                Math.Abs(mrmDaughterMZ1 - mrmDaughterMZ2) <= COMPARISON_TOLERANCE)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ProcessMRMList(
            clsScanList scanList,
            clsSpectraCache spectraCache,
            clsSICProcessing sicProcessor,
            clsXMLResultsWriter xmlResultsWriter,
            clsMASICPeakFinder peakFinder,
            ref int parentIonsProcessed)
        {
            try
            {
                // Initialize sicDetails
                var sicDetails = new clsSICDetails();
                sicDetails.Reset();
                sicDetails.SICScanType = clsScanList.eScanTypeConstants.FragScan;

                var noiseStatsSegments = new List<clsBaselineNoiseStatsSegment>();

                for (var parentIonIndex = 0; parentIonIndex <= scanList.ParentIons.Count - 1; parentIonIndex++)
                {
                    if (scanList.ParentIons[parentIonIndex].MRMDaughterMZ <= 0)
                    {
                        continue;
                    }

                    // Step 1: Create the SIC for this MRM Parent/Daughter pair

                    var parentIonMZ = scanList.ParentIons[parentIonIndex].MZ;
                    var mrmDaughterMZ = scanList.ParentIons[parentIonIndex].MRMDaughterMZ;
                    var searchToleranceHalfWidth = scanList.ParentIons[parentIonIndex].MRMToleranceHalfWidth;

                    // Reset SICData
                    sicDetails.SICData.Clear();

                    // Step through the fragmentation spectra, finding those that have matching parent and daughter ion m/z values
                    for (var scanIndex = 0; scanIndex <= scanList.FragScans.Count - 1; scanIndex++)
                    {
                        if (scanList.FragScans[scanIndex].MRMScanType != MRMScanTypeConstants.SRM)
                        {
                            continue;
                        }

                        var fragScan = scanList.FragScans[scanIndex];

                        var useScan = false;
                        for (var mrmMassIndex = 0; mrmMassIndex <= fragScan.MRMScanInfo.MRMMassCount - 1; mrmMassIndex++)
                        {
                            if (MRMParentDaughterMatch(fragScan.MRMScanInfo.ParentIonMZ,
                                                       fragScan.MRMScanInfo.MRMMassList[mrmMassIndex].CentralMass,
                                                       parentIonMZ, mrmDaughterMZ))
                            {
                                useScan = true;
                                break;
                            }
                        }

                        if (!useScan)
                            continue;

                        // Include this scan in the SIC for this parent ion

                        mDataAggregation.FindMaxValueInMZRange(spectraCache,
                                                               scanList.FragScans[scanIndex],
                                                               mrmDaughterMZ - searchToleranceHalfWidth,
                                                               mrmDaughterMZ + searchToleranceHalfWidth,
                                                               out var closestMZ, out var matchIntensity);

                        sicDetails.AddData(fragScan.ScanNumber, matchIntensity, closestMZ, scanIndex);
                    }

                    // Step 2: Find the largest peak in the SIC

                    // Compute the noise level; the noise level may change with increasing index number if the background is increasing for a given m/z
                    var success = peakFinder.ComputeDualTrimmedNoiseLevelTTest(sicDetails.SICIntensities, 0,
                                                                               sicDetails.SICDataCount - 1,
                                                                               mOptions.SICOptions.SICPeakFinderOptions.
                                                                                   SICBaselineNoiseOptions,
                                                                               out noiseStatsSegments);

                    if (!success)
                    {
                        SetLocalErrorCode(clsMASIC.eMasicErrorCodes.FindSICPeaksError, true);
                        return false;
                    }

                    // Initialize the peak
                    scanList.ParentIons[parentIonIndex].SICStats.Peak = new clsSICStatsPeak();

                    // Find the data point with the maximum intensity
                    double maximumIntensity = 0;
                    scanList.ParentIons[parentIonIndex].SICStats.Peak.IndexObserved = 0;
                    for (var scanIndex = 0; scanIndex <= sicDetails.SICDataCount - 1; scanIndex++)
                    {
                        var intensity = sicDetails.SICIntensities[scanIndex];
                        if (intensity > maximumIntensity)
                        {
                            maximumIntensity = intensity;
                            scanList.ParentIons[parentIonIndex].SICStats.Peak.IndexObserved = scanIndex;
                        }
                    }

                    // Compute the minimum potential peak area in the entire SIC, populating udtSICPotentialAreaStatsInFullSIC
                    peakFinder.FindPotentialPeakArea(sicDetails.SICData,
                                                     out var potentialAreaStatsInFullSIC,
                                                     mOptions.SICOptions.SICPeakFinderOptions);

                    // Update .BaselineNoiseStats in scanList.ParentIons(parentIonIndex).SICStats.Peak
                    scanList.ParentIons[parentIonIndex].SICStats.Peak.BaselineNoiseStats =
                    peakFinder.LookupNoiseStatsUsingSegments(
                        scanList.ParentIons[parentIonIndex].SICStats.Peak.IndexObserved,
                        noiseStatsSegments);

                    var parentIon = scanList.ParentIons[parentIonIndex];

                    // Clear udtSICPotentialAreaStatsForPeak
                    parentIon.SICStats.SICPotentialAreaStatsForPeak = new clsSICPotentialAreaStats();

                    var peakIsValid = peakFinder.FindSICPeakAndArea(sicDetails.SICData,
                                                                    out var potentialAreaStatsForPeakOut,
                                                                    parentIon.SICStats.Peak, out var smoothedYDataSubset,
                                                                    mOptions.SICOptions.SICPeakFinderOptions,
                                                                    potentialAreaStatsInFullSIC, false,
                                                                    scanList.SIMDataPresent, false);
                    parentIon.SICStats.SICPotentialAreaStatsForPeak = potentialAreaStatsForPeakOut;

                    sicProcessor.StorePeakInParentIon(scanList, parentIonIndex, sicDetails,
                                                      parentIon.SICStats.SICPotentialAreaStatsForPeak,
                                                      parentIon.SICStats.Peak, peakIsValid);

                    // Step 3: store the results

                    // Possibly save the stats for this SIC to the SICData file
                    mDataOutputHandler.SaveSICDataToText(mOptions.SICOptions, scanList, parentIonIndex, sicDetails);

                    // Save the stats for this SIC to the XML file
                    xmlResultsWriter.SaveDataToXML(scanList, parentIonIndex, sicDetails, smoothedYDataSubset,
                                                   mDataOutputHandler);

                    parentIonsProcessed += 1;

                    // ---------------------------------------------------------
                    // Update progress
                    // ---------------------------------------------------------
                    try
                    {
                        if (scanList.ParentIons.Count > 1)
                        {
                            UpdateProgress((short)(parentIonsProcessed / (double)(scanList.ParentIons.Count - 1) * 100));
                        }
                        else
                        {
                            UpdateProgress(0);
                        }

                        UpdateCacheStats(spectraCache);
                        if (mOptions.AbortProcessing)
                        {
                            scanList.ProcessingIncomplete = true;
                            break;
                        }

                        if (parentIonsProcessed % 100 == 0)
                        {
                            if (DateTime.UtcNow.Subtract(mOptions.LastParentIonProcessingLogTime).TotalSeconds >= 10 || parentIonsProcessed % 500 == 0)
                            {
                                ReportMessage("Parent Ions Processed: " + parentIonsProcessed.ToString());
                                Console.Write(".");
                                mOptions.LastParentIonProcessingLogTime = DateTime.UtcNow;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ReportError("Error updating progress", ex, clsMASIC.eMasicErrorCodes.CreateSICsError);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error creating SICs for MRM spectra", ex, clsMASIC.eMasicErrorCodes.CreateSICsError);
                return false;
            }
        }
    }
}
