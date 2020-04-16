using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MASIC.DataOutput;
using MASICPeakFinder;

namespace MASIC
{
    public class clsSICProcessing : clsMasicEventNotifier
    {
        private const string CREATING_SICS = "Creating SIC's for parent ions";
        private readonly clsMASICPeakFinder mMASICPeakFinder;
        private readonly clsMRMProcessing mMRMProcessor;

        /// <summary>
        /// Constructor
        /// </summary>
        public clsSICProcessing(clsMASICPeakFinder peakFinder, clsMRMProcessing mrmProcessor)
        {
            mMASICPeakFinder = peakFinder;
            mMRMProcessor = mrmProcessor;
        }

        private List<clsMzBinInfo> CreateMZLookupList(clsMASICOptions masicOptions, clsScanList scanList, bool processSIMScans, int simIndex)
        {
            int parentIonIndex;
            bool includeParentIon;
            var mzBinList = new List<clsMzBinInfo>(scanList.ParentIons.Count - 1);
            var sicOptions = masicOptions.SICOptions;
            for (parentIonIndex = 0; parentIonIndex <= scanList.ParentIons.Count - 1; parentIonIndex++)
            {
                if (scanList.ParentIons[parentIonIndex].MRMDaughterMZ > 0)
                {
                    includeParentIon = false;
                }
                else if (masicOptions.CustomSICList.LimitSearchToCustomMZList)
                {
                    // Always include CustomSICPeak entries
                    includeParentIon = scanList.ParentIons[parentIonIndex].CustomSICPeak;
                }
                else
                {
                    // Use processingSIMScans and .SIMScan to decide whether or not to include the entry
                    var surveyScan = scanList.SurveyScans[scanList.ParentIons[parentIonIndex].SurveyScanIndex];
                    if (processSIMScans)
                    {
                        if (surveyScan.SIMScan)
                        {
                            if (surveyScan.SIMIndex == simIndex)
                            {
                                includeParentIon = true;
                            }
                            else
                            {
                                includeParentIon = false;
                            }
                        }
                        else
                        {
                            includeParentIon = false;
                        }
                    }
                    else
                    {
                        includeParentIon = !surveyScan.SIMScan;
                    }
                }

                if (includeParentIon)
                {
                    var newMzBin = new clsMzBinInfo()
                    {
                        MZ = scanList.ParentIons[parentIonIndex].MZ,
                        ParentIonIndex = parentIonIndex
                    };
                    if (scanList.ParentIons[parentIonIndex].CustomSICPeak)
                    {
                        newMzBin.MZTolerance = scanList.ParentIons[parentIonIndex].CustomSICPeakMZToleranceDa;
                        newMzBin.MZToleranceIsPPM = false;
                    }
                    else
                    {
                        newMzBin.MZTolerance = sicOptions.SICTolerance;
                        newMzBin.MZToleranceIsPPM = sicOptions.SICToleranceIsPPM;
                    }

                    mzBinList.Add(newMzBin);
                }
            }

            // Sort mzBinList by m/z
            var sortedMzBins = (from item in mzBinList
                                select item).ToList();
            return sortedMzBins;
        }

        public bool CreateParentIonSICs(clsScanList scanList, clsSpectraCache spectraCache, clsMASICOptions masicOptions, clsDataOutput dataOutputHandler, clsSICProcessing sicProcessor, clsXMLResultsWriter xmlResultsWriter)
        {
            var success = default(bool);
            int parentIonIndex;
            int parentIonsProcessed;
            if (scanList.ParentIons.Count <= 0)
            {
                // No parent ions
                if (masicOptions.SuppressNoParentIonsError)
                {
                    return true;
                }
                else
                {
                    SetLocalErrorCode(clsMASIC.eMasicErrorCodes.NoParentIonsFoundInInputFile);
                    return false;
                }
            }
            else if (scanList.SurveyScans.Count <= 0)
            {
                // No survey scans
                if (masicOptions.SuppressNoParentIonsError)
                {
                    return true;
                }
                else
                {
                    SetLocalErrorCode(clsMASIC.eMasicErrorCodes.NoSurveyScansFoundInInputFile);
                    return false;
                }
            }

            try
            {
                parentIonsProcessed = 0;
                masicOptions.LastParentIonProcessingLogTime = DateTime.UtcNow;
                UpdateProgress(0, CREATING_SICS);
                Console.Write(CREATING_SICS);
                ReportMessage(CREATING_SICS);

                // Create an array of m/z values in scanList.ParentIons, then sort by m/z
                // Next, step through the data in order of m/z, creating SICs for each grouping of m/z's within half of the SIC tolerance

                int simIndex;
                int simIndexMax;

                // First process the non SIM, non MRM scans
                // If this file only has MRM scans, then CreateMZLookupList will return False
                var mzBinList = CreateMZLookupList(masicOptions, scanList, false, 0);
                if (mzBinList.Count > 0)
                {
                    success = ProcessMZList(scanList, spectraCache, masicOptions, dataOutputHandler, xmlResultsWriter, mzBinList, false, 0, ref parentIonsProcessed);
                }

                if (success && !masicOptions.CustomSICList.LimitSearchToCustomMZList)
                {
                    // Now process the SIM scans (if any)
                    // First, see if any SIMScans are present and determine the maximum SIM Index
                    simIndexMax = -1;
                    for (parentIonIndex = 0; parentIonIndex <= scanList.ParentIons.Count - 1; parentIonIndex++)
                    {
                        var surveyScan = scanList.SurveyScans[scanList.ParentIons[parentIonIndex].SurveyScanIndex];
                        if (surveyScan.SIMScan)
                        {
                            if (surveyScan.SIMIndex > simIndexMax)
                            {
                                simIndexMax = surveyScan.SIMIndex;
                            }
                        }
                    }

                    // Now process each SIM Scan type
                    for (simIndex = 0; simIndex <= simIndexMax; simIndex++)
                    {
                        var mzBinListSIM = CreateMZLookupList(masicOptions, scanList, true, simIndex);
                        if (mzBinListSIM.Count > 0)
                        {
                            ProcessMZList(scanList, spectraCache, masicOptions, dataOutputHandler, xmlResultsWriter, mzBinListSIM, true, simIndex, ref parentIonsProcessed);
                        }
                    }
                }

                // Lastly, process the MRM scans (if any)
                if (scanList.MRMDataPresent)
                {
                    mMRMProcessor.ProcessMRMList(scanList, spectraCache, sicProcessor, xmlResultsWriter, mMASICPeakFinder, ref parentIonsProcessed);
                }

                Console.WriteLine();
                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error creating Parent Ion SICs", ex, clsMASIC.eMasicErrorCodes.CreateSICsError);
                return false;
            }
        }

        private clsSICStatsPeak ExtractSICDetailsFromFullSIC(int mzIndexWork, List<clsBaselineNoiseStatsSegment> baselineNoiseStatSegments, int fullSICDataCount, int[,] fullSICScanIndices, double[,] fullSICIntensities, double[,] fullSICMasses, clsScanList scanList, int scanIndexObservedInFullSIC, clsSICDetails sicDetails, clsMASICOptions masicOptions, clsScanNumScanTimeConversion scanNumScanConverter, bool customSICPeak, float customSICPeakScanOrAcqTimeTolerance)
        {
            // Minimum number of scans to extend left or right of the scan that meets the minimum intensity threshold requirement
            const int MINIMUM_NOISE_SCANS_TO_INCLUDE = 10;
            var customSICScanToleranceMinutesHalfWidth = default(float);

            // Pointers to entries in fullSICScanIndices() and fullSICIntensities()
            var scanIndexStart = default(int);
            var scanIndexEnd = default(int);
            var maximumIntensity = default(double);
            var sicOptions = masicOptions.SICOptions;
            var baselineNoiseStats = mMASICPeakFinder.LookupNoiseStatsUsingSegments(scanIndexObservedInFullSIC, baselineNoiseStatSegments);

            // Initialize the peak
            var sicPeak = new clsSICStatsPeak() { BaselineNoiseStats = baselineNoiseStats };

            // Initialize the values for the maximum width of the SIC peak; these might get altered for custom SIC values
            float maxSICPeakWidthMinutesBackward = sicOptions.MaxSICPeakWidthMinutesBackward;
            float maxSICPeakWidthMinutesForward = sicOptions.MaxSICPeakWidthMinutesForward;

            // Limit the data examined to a portion of fullSICScanIndices() and fullSICIntensities, populating udtSICDetails
            try
            {
                // Initialize customSICScanToleranceHalfWidth
                if (customSICPeakScanOrAcqTimeTolerance < float.Epsilon)
                {
                    customSICPeakScanOrAcqTimeTolerance = masicOptions.CustomSICList.ScanOrAcqTimeTolerance;
                }

                if (customSICPeakScanOrAcqTimeTolerance < float.Epsilon)
                {
                    // Use the entire SIC
                    // Specify this by setting customSICScanToleranceMinutesHalfWidth to the maximum scan time in .MasterScanTimeList()
                    if (scanList.MasterScanOrderCount > 0)
                    {
                        customSICScanToleranceMinutesHalfWidth = scanList.MasterScanTimeList[scanList.MasterScanOrderCount - 1];
                    }
                    else
                    {
                        customSICScanToleranceMinutesHalfWidth = float.MaxValue;
                    }
                }
                else
                {
                    if (masicOptions.CustomSICList.ScanToleranceType == clsCustomSICList.eCustomSICScanTypeConstants.Relative && customSICPeakScanOrAcqTimeTolerance > 10)
                    {
                        // Relative scan time should only range from 0 to 1; we'll allow values up to 10
                        customSICPeakScanOrAcqTimeTolerance = 10;
                    }

                    customSICScanToleranceMinutesHalfWidth = scanNumScanConverter.ScanOrAcqTimeToScanTime(scanList, customSICPeakScanOrAcqTimeTolerance / 2, masicOptions.CustomSICList.ScanToleranceType, true);
                }

                if (customSICPeak)
                {
                    if (maxSICPeakWidthMinutesBackward < customSICScanToleranceMinutesHalfWidth)
                    {
                        maxSICPeakWidthMinutesBackward = customSICScanToleranceMinutesHalfWidth;
                    }

                    if (maxSICPeakWidthMinutesForward < customSICScanToleranceMinutesHalfWidth)
                    {
                        maxSICPeakWidthMinutesForward = customSICScanToleranceMinutesHalfWidth;
                    }
                }

                // Initially use just 3 survey scans, centered around scanIndexObservedInFullSIC
                if (scanIndexObservedInFullSIC > 0)
                {
                    scanIndexStart = scanIndexObservedInFullSIC - 1;
                    scanIndexEnd = scanIndexObservedInFullSIC + 1;
                }
                else
                {
                    scanIndexStart = 0;
                    scanIndexEnd = 1;
                    scanIndexObservedInFullSIC = 0;
                }

                if (scanIndexEnd >= fullSICDataCount)
                    scanIndexEnd = fullSICDataCount - 1;
            }
            catch (Exception ex)
            {
                ReportError("Error initializing SIC start/end indices", ex, clsMASIC.eMasicErrorCodes.CreateSICsError);
            }

            if (scanIndexEnd >= scanIndexStart)
            {
                var scanIndexMax = default(int);
                try
                {
                    // Start by using the 3 survey scans centered around scanIndexObservedInFullSIC
                    maximumIntensity = -1;
                    scanIndexMax = -1;
                    for (int scanIndex = scanIndexStart; scanIndex <= scanIndexEnd; scanIndex++)
                    {
                        if (fullSICIntensities[mzIndexWork, scanIndex] > maximumIntensity)
                        {
                            maximumIntensity = fullSICIntensities[mzIndexWork, scanIndex];
                            scanIndexMax = scanIndex;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ReportError("Error while creating initial SIC", ex, clsMASIC.eMasicErrorCodes.CreateSICsError);
                }

                // Now extend the SIC, stepping left and right until a threshold is reached
                bool leftDone = false;
                bool rightDone = false;

                // The index of the first scan found to be below threshold (on the left)
                int scanIndexBelowThresholdLeft = -1;

                // The index of the first scan found to be below threshold (on the right)
                int scanIndexBelowThresholdRight = -1;
                while (scanIndexStart > 0 && !leftDone || scanIndexEnd < fullSICDataCount - 1 && !rightDone)
                {
                    try
                    {
                        // Extend the SIC to the left until the threshold is reached
                        if (scanIndexStart > 0 && !leftDone)
                        {
                            if (fullSICIntensities[mzIndexWork, scanIndexStart] < sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum || fullSICIntensities[mzIndexWork, scanIndexStart] < sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax * maximumIntensity || fullSICIntensities[mzIndexWork, scanIndexStart] < sicPeak.BaselineNoiseStats.NoiseLevel)
                            {
                                if (scanIndexBelowThresholdLeft < 0)
                                {
                                    scanIndexBelowThresholdLeft = scanIndexStart;
                                }
                                else if (scanIndexStart <= scanIndexBelowThresholdLeft - MINIMUM_NOISE_SCANS_TO_INCLUDE)
                                {
                                    // We have now processed MINIMUM_NOISE_SCANS_TO_INCLUDE+1 scans that are below the thresholds
                                    // Stop creating the SIC to the left
                                    leftDone = true;
                                }
                            }
                            else
                            {
                                scanIndexBelowThresholdLeft = -1;
                            }

                            float peakWidthMinutesBackward = scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexObservedInFullSIC]].ScanTime - scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexStart]].ScanTime;
                            if (leftDone)
                            {
                                // Require a minimum distance of InitialPeakWidthScansMaximum data points to the left of scanIndexObservedInFullSIC and to the left of scanIndexMax
                                if (scanIndexObservedInFullSIC - scanIndexStart < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum)
                                    leftDone = false;
                                if (scanIndexMax - scanIndexStart < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum)
                                    leftDone = false;

                                // For custom SIC values, make sure the scan range has been satisfied
                                if (leftDone && customSICPeak)
                                {
                                    if (peakWidthMinutesBackward < customSICScanToleranceMinutesHalfWidth)
                                    {
                                        leftDone = false;
                                    }
                                }
                            }

                            if (!leftDone)
                            {
                                if (scanIndexStart == 0)
                                {
                                    leftDone = true;
                                }
                                else
                                {
                                    scanIndexStart -= 1;
                                    if (fullSICIntensities[mzIndexWork, scanIndexStart] > maximumIntensity)
                                    {
                                        maximumIntensity = fullSICIntensities[mzIndexWork, scanIndexStart];
                                        scanIndexMax = scanIndexStart;
                                    }
                                }
                            }

                            peakWidthMinutesBackward = scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexObservedInFullSIC]].ScanTime - scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexStart]].ScanTime;
                            if (peakWidthMinutesBackward >= maxSICPeakWidthMinutesBackward)
                            {
                                leftDone = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ReportError("Error extending SIC to the left", ex, clsMASIC.eMasicErrorCodes.CreateSICsError);
                    }

                    try
                    {
                        // Extend the SIC to the right until the threshold is reached
                        if (scanIndexEnd < fullSICDataCount - 1 && !rightDone)
                        {
                            if (fullSICIntensities[mzIndexWork, scanIndexEnd] < sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum || fullSICIntensities[mzIndexWork, scanIndexEnd] < sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax * maximumIntensity || fullSICIntensities[mzIndexWork, scanIndexEnd] < sicPeak.BaselineNoiseStats.NoiseLevel)
                            {
                                if (scanIndexBelowThresholdRight < 0)
                                {
                                    scanIndexBelowThresholdRight = scanIndexEnd;
                                }
                                else if (scanIndexEnd >= scanIndexBelowThresholdRight + MINIMUM_NOISE_SCANS_TO_INCLUDE)
                                {
                                    // We have now processed MINIMUM_NOISE_SCANS_TO_INCLUDE+1 scans that are below the thresholds
                                    // Stop creating the SIC to the right
                                    rightDone = true;
                                }
                            }
                            else
                            {
                                scanIndexBelowThresholdRight = -1;
                            }

                            float peakWidthMinutesForward = scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexEnd]].ScanTime - scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexObservedInFullSIC]].ScanTime;
                            if (rightDone)
                            {
                                // Require a minimum distance of InitialPeakWidthScansMaximum data points to the right of scanIndexObservedInFullSIC and to the right of scanIndexMax
                                if (scanIndexEnd - scanIndexObservedInFullSIC < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum)
                                    rightDone = false;
                                if (scanIndexEnd - scanIndexMax < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum)
                                    rightDone = false;

                                // For custom SIC values, make sure the scan range has been satisfied
                                if (rightDone && customSICPeak)
                                {
                                    if (peakWidthMinutesForward < customSICScanToleranceMinutesHalfWidth)
                                    {
                                        rightDone = false;
                                    }
                                }
                            }

                            if (!rightDone)
                            {
                                if (scanIndexEnd == fullSICDataCount - 1)
                                {
                                    rightDone = true;
                                }
                                else
                                {
                                    scanIndexEnd += 1;
                                    if (fullSICIntensities[mzIndexWork, scanIndexEnd] > maximumIntensity)
                                    {
                                        maximumIntensity = fullSICIntensities[mzIndexWork, scanIndexEnd];
                                        scanIndexMax = scanIndexEnd;
                                    }
                                }
                            }

                            peakWidthMinutesForward = scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexEnd]].ScanTime - scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexObservedInFullSIC]].ScanTime;
                            if (peakWidthMinutesForward >= maxSICPeakWidthMinutesForward)
                            {
                                rightDone = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ReportError("Error extending SIC to the right", ex, clsMASIC.eMasicErrorCodes.CreateSICsError);
                    }
                }    // While Not LeftDone and Not RightDone
            }

            // Populate udtSICDetails with the data between scanIndexStart and scanIndexEnd
            if (scanIndexStart < 0)
                scanIndexStart = 0;
            if (scanIndexEnd >= fullSICDataCount)
                scanIndexEnd = fullSICDataCount - 1;
            if (scanIndexEnd < scanIndexStart)
            {
                ReportError("Programming error: scanIndexEnd < scanIndexStart", clsMASIC.eMasicErrorCodes.FindSICPeaksError);
                scanIndexEnd = scanIndexStart;
            }

            try
            {
                // Copy the scan index values from fullSICScanIndices to .SICScanIndices()
                // Copy the intensity values from fullSICIntensities() to .SICData()
                // Copy the mz values from fullSICMasses() to .SICMasses()

                sicDetails.SICScanType = clsScanList.eScanTypeConstants.SurveyScan;
                sicDetails.SICData.Clear();
                sicPeak.IndexObserved = 0;
                for (int scanIndex = scanIndexStart; scanIndex <= scanIndexEnd; scanIndex++)
                {
                    if (fullSICScanIndices[mzIndexWork, scanIndex] >= 0)
                    {
                        sicDetails.AddData(scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndex]].ScanNumber, fullSICIntensities[mzIndexWork, scanIndex], fullSICMasses[mzIndexWork, scanIndex], fullSICScanIndices[mzIndexWork, scanIndex]);
                        if (scanIndex == scanIndexObservedInFullSIC)
                        {
                            sicPeak.IndexObserved = sicDetails.SICDataCount - 1;
                        }
                    }
                    else
                    {
                        // This shouldn't happen
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError("Error populating .SICScanIndices, .SICData, and .SICMasses", ex, clsMASIC.eMasicErrorCodes.CreateSICsError);
            }

            return sicPeak;
        }

        private bool ProcessMZList(clsScanList scanList, clsSpectraCache spectraCache, clsMASICOptions masicOptions, clsDataOutput dataOutputHandler, clsXMLResultsWriter xmlResultsWriter, IReadOnlyList<clsMzBinInfo> mzBinList, bool processSIMScans, int simIndex, ref int parentIonsProcessed)
        {
            // Step through the data in order of m/z, creating SICs for each grouping of m/z's within half of the SIC tolerance
            // Note that mzBinList and parentIonIndices() are parallel arrays, with mzBinList() sorted on ascending m/z
            const int MAX_RAW_DATA_MEMORY_USAGE_MB = 50;
            int maxMZCountInChunk;
            var mzSearchChunks = new List<clsMzSearchInfo>();
            bool[] parentIonUpdated;
            try
            {
                // Determine the maximum number of m/z values to process simultaneously
                // Limit the total memory usage to ~50 MB
                // Each m/z value will require 12 bytes per scan

                if (scanList.SurveyScans.Count > 0)
                {
                    maxMZCountInChunk = Convert.ToInt32(MAX_RAW_DATA_MEMORY_USAGE_MB * 1024 * 1024 / (double)(scanList.SurveyScans.Count * 12));
                }
                else
                {
                    maxMZCountInChunk = 1;
                }

                if (maxMZCountInChunk > mzBinList.Count)
                {
                    maxMZCountInChunk = mzBinList.Count;
                }

                if (maxMZCountInChunk < 1)
                    maxMZCountInChunk = 1;

                // Reserve room in parentIonUpdated
                parentIonUpdated = new bool[mzBinList.Count];
            }
            catch (Exception ex)
            {
                ReportError("Error reserving memory for the m/z chunks", ex, clsMASIC.eMasicErrorCodes.CreateSICsError);
                return false;
            }

            try
            {
                var dataAggregation = new clsDataAggregation();
                RegisterEvents(dataAggregation);
                var scanNumScanConverter = new clsScanNumScanTimeConversion();
                RegisterEvents(scanNumScanConverter);
                var parentIonIndices = (from item in mzBinList
                                        select item.ParentIonIndex).ToList();
                int mzIndex = 0;
                while (mzIndex < mzBinList.Count)
                {
                    // ---------------------------------------------------------
                    // Find the next group of m/z values to use, starting with mzIndex
                    // ---------------------------------------------------------

                    // Initially set the MZIndexStart to mzIndex

                    // Look for adjacent m/z values within udtMZBinList(.MZIndexStart).MZToleranceDa / 2
                    // of the m/z value that starts this group
                    // Only group m/z values with the same udtMZBinList().MZTolerance and udtMZBinList().MZToleranceIsPPM values

                    var mzSearchChunk = new clsMzSearchInfo()
                    {
                        MZIndexStart = mzIndex,
                        MZTolerance = mzBinList[mzIndex].MZTolerance,
                        MZToleranceIsPPM = mzBinList[mzIndex].MZToleranceIsPPM
                    };
                    double mzToleranceDa;
                    if (mzSearchChunk.MZToleranceIsPPM)
                    {
                        mzToleranceDa = clsUtilities.PPMToMass(mzSearchChunk.MZTolerance, mzBinList[mzSearchChunk.MZIndexStart].MZ);
                    }
                    else
                    {
                        mzToleranceDa = mzSearchChunk.MZTolerance;
                    }

                    while (mzIndex < mzBinList.Count - 2 && Math.Abs(mzBinList[mzIndex + 1].MZTolerance - mzSearchChunk.MZTolerance) < double.Epsilon && mzBinList[mzIndex + 1].MZToleranceIsPPM == mzSearchChunk.MZToleranceIsPPM && mzBinList[mzIndex + 1].MZ - mzBinList[mzSearchChunk.MZIndexStart].MZ <= mzToleranceDa / 2)
                        mzIndex += 1;
                    mzSearchChunk.MZIndexEnd = mzIndex;
                    if (mzSearchChunk.MZIndexEnd == mzSearchChunk.MZIndexStart)
                    {
                        mzSearchChunk.MZIndexMidpoint = mzSearchChunk.MZIndexEnd;
                        mzSearchChunk.SearchMZ = mzBinList[mzSearchChunk.MZIndexStart].MZ;
                    }
                    // Determine the median m/z of the members in the m/z group
                    else if ((mzSearchChunk.MZIndexEnd - mzSearchChunk.MZIndexStart) % 2 == 0)
                    {
                        // Odd number of points; use the m/z value of the midpoint
                        mzSearchChunk.MZIndexMidpoint = mzSearchChunk.MZIndexStart + Convert.ToInt32((mzSearchChunk.MZIndexEnd - mzSearchChunk.MZIndexStart) / (double)2);
                        mzSearchChunk.SearchMZ = mzBinList[mzSearchChunk.MZIndexMidpoint].MZ;
                    }
                    else
                    {
                        // Even number of points; average the values on either side of (.mzIndexEnd - .mzIndexStart / 2)
                        mzSearchChunk.MZIndexMidpoint = mzSearchChunk.MZIndexStart + Convert.ToInt32(Math.Floor((mzSearchChunk.MZIndexEnd - mzSearchChunk.MZIndexStart) / (double)2));
                        mzSearchChunk.SearchMZ = (mzBinList[mzSearchChunk.MZIndexMidpoint].MZ + mzBinList[mzSearchChunk.MZIndexMidpoint + 1].MZ) / 2;
                    }

                    mzSearchChunks.Add(mzSearchChunk);
                    if (mzSearchChunks.Count >= maxMZCountInChunk || mzIndex == mzBinList.Count - 1)
                    {
                        // ---------------------------------------------------------
                        // Reached maxMZCountInChunk m/z value
                        // Process all of the m/z values in udtMZSearchChunk
                        // ---------------------------------------------------------

                        bool success = ProcessMzSearchChunk(masicOptions, scanList, dataAggregation, dataOutputHandler, xmlResultsWriter, spectraCache, scanNumScanConverter, mzSearchChunks, parentIonIndices, processSIMScans, simIndex, parentIonUpdated, ref parentIonsProcessed);
                        if (!success)
                        {
                            return false;
                        }

                        // Clear mzSearchChunks
                        mzSearchChunks.Clear();
                    }

                    if (masicOptions.AbortProcessing)
                    {
                        scanList.ProcessingIncomplete = true;
                        break;
                    }

                    mzIndex += 1;
                }

                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error processing the m/z chunks to create the SIC data", ex, clsMASIC.eMasicErrorCodes.CreateSICsError);
                return false;
            }
        }

        private bool ProcessMzSearchChunk(clsMASICOptions masicOptions, clsScanList scanList, clsDataAggregation dataAggregation, clsDataOutput dataOutputHandler, clsXMLResultsWriter xmlResultsWriter, clsSpectraCache spectraCache, clsScanNumScanTimeConversion scanNumScanConverter, IReadOnlyList<clsMzSearchInfo> mzSearchChunk, IList<int> parentIonIndices, bool processSIMScans, int simIndex, IList<bool> parentIonUpdated, ref int parentIonsProcessed)
        {
            // The following are 2D arrays, ranging from 0 to mzSearchChunkCount-1 in the first dimension and 0 to .SurveyScans.Count - 1 in the second dimension
            // We could have included these in udtMZSearchChunk but memory management is more efficient if I use 2D arrays for this data
            int[,] fullSICScanIndices;     // Pointer into .SurveyScans
            double[,] fullSICIntensities;
            double[,] fullSICMasses;
            int[] fullSICDataCount;        // Count of the number of valid entries in the second dimension of the above 3 arrays

            // The following is a 1D array, containing the SIC intensities for a single m/z group
            double[] fullSICIntensities1D;

            // Reserve room in fullSICScanIndices for at most maxMZCountInChunk values and .SurveyScans.Count scans
            fullSICDataCount = new int[mzSearchChunk.Count];
            fullSICScanIndices = new int[mzSearchChunk.Count, scanList.SurveyScans.Count];
            fullSICIntensities = new double[mzSearchChunk.Count, scanList.SurveyScans.Count];
            fullSICMasses = new double[mzSearchChunk.Count, scanList.SurveyScans.Count];
            fullSICIntensities1D = new double[scanList.SurveyScans.Count];

            // Initialize .MaximumIntensity and .ScanIndexMax
            // Additionally, reset fullSICDataCount() and, for safety, set fullSICScanIndices() to -1
            for (int mzIndexWork = 0; mzIndexWork <= mzSearchChunk.Count - 1; mzIndexWork++)
            {
                mzSearchChunk[mzIndexWork].ResetMaxIntensity();
                fullSICDataCount[mzIndexWork] = 0;
                for (int surveyScanIndex = 0; surveyScanIndex <= scanList.SurveyScans.Count - 1; surveyScanIndex++)
                    fullSICScanIndices[mzIndexWork, surveyScanIndex] = -1;
            }

            // ---------------------------------------------------------
            // Step through scanList to obtain the scan numbers and intensity data for each .SearchMZ in udtMZSearchChunk
            // We're stepping scan by scan since the process of loading a scan from disk is slower than the process of searching for each m/z in the scan
            // ---------------------------------------------------------
            for (int surveyScanIndex = 0; surveyScanIndex <= scanList.SurveyScans.Count - 1; surveyScanIndex++)
            {
                bool useScan;
                if (processSIMScans)
                {
                    if (scanList.SurveyScans[surveyScanIndex].SIMScan && scanList.SurveyScans[surveyScanIndex].SIMIndex == simIndex)
                    {
                        useScan = true;
                    }
                    else
                    {
                        useScan = false;
                    }
                }
                else
                {
                    useScan = !scanList.SurveyScans[surveyScanIndex].SIMScan;
                    if (scanList.SurveyScans[surveyScanIndex].ZoomScan)
                    {
                        useScan = false;
                    }
                }

                if (!useScan)
                {
                    continue;
                }

                int poolIndex;
                if (!spectraCache.ValidateSpectrumInPool(scanList.SurveyScans[surveyScanIndex].ScanNumber, out poolIndex))
                {
                    SetLocalErrorCode(clsMASIC.eMasicErrorCodes.ErrorUncachingSpectrum);
                    return false;
                }

                for (int mzIndexWork = 0; mzIndexWork <= mzSearchChunk.Count - 1; mzIndexWork++)
                {
                    var current = mzSearchChunk[mzIndexWork];
                    double mzToleranceDa;
                    if (current.MZToleranceIsPPM)
                    {
                        mzToleranceDa = clsUtilities.PPMToMass(current.MZTolerance, current.SearchMZ);
                    }
                    else
                    {
                        mzToleranceDa = current.MZTolerance;
                    }

                    int ionMatchCount;
                    double closestMZ;
                    double ionSum = dataAggregation.AggregateIonsInRange(spectraCache.SpectraPool[poolIndex], current.SearchMZ, mzToleranceDa, out ionMatchCount, out closestMZ, false);
                    int dataIndex = fullSICDataCount[mzIndexWork];
                    fullSICScanIndices[mzIndexWork, dataIndex] = surveyScanIndex;
                    fullSICIntensities[mzIndexWork, dataIndex] = ionSum;
                    if (ionSum < float.Epsilon && masicOptions.SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData)
                    {
                        fullSICIntensities[mzIndexWork, dataIndex] = scanList.SurveyScans[surveyScanIndex].MinimumPositiveIntensity;
                    }

                    fullSICMasses[mzIndexWork, dataIndex] = closestMZ;
                    if (ionSum > current.MaximumIntensity)
                    {
                        current.MaximumIntensity = ionSum;
                        current.ScanIndexMax = dataIndex;
                    }

                    fullSICDataCount[mzIndexWork] += 1;
                }

                if (surveyScanIndex % 100 == 0)
                {
                    UpdateProgress("Loading raw SIC data: " + surveyScanIndex + " / " + scanList.SurveyScans.Count);
                    if (masicOptions.AbortProcessing)
                    {
                        scanList.ProcessingIncomplete = true;
                        return false;
                    }
                }
            }

            UpdateProgress(CREATING_SICS);
            if (masicOptions.AbortProcessing)
            {
                scanList.ProcessingIncomplete = true;
                return false;
            }

            const int debugParentIonIndexToFind = 3139;
            const float DebugMZToFind = 488.47F;

            // ---------------------------------------------------------
            // Compute the noise level in fullSICIntensities() for each m/z in udtMZSearchChunk
            // Also, find the peaks for each m/z in udtMZSearchChunk and retain the largest peak found
            // ---------------------------------------------------------
            for (int mzIndexWork = 0; mzIndexWork <= mzSearchChunk.Count - 1; mzIndexWork++)
            {
                // Use this for debugging
                if (Math.Abs(mzSearchChunk[mzIndexWork].SearchMZ - DebugMZToFind) < 0.1)
                {
                    // ReSharper disable once UnusedVariable
                    int parentIonIndexPointer = mzSearchChunk[mzIndexWork].MZIndexStart;
                }

                // Copy the data for this m/z into fullSICIntensities1D()
                for (int dataIndex = 0; dataIndex <= fullSICDataCount[mzIndexWork] - 1; dataIndex++)
                    fullSICIntensities1D[dataIndex] = fullSICIntensities[mzIndexWork, dataIndex];

                // Compute the noise level; the noise level may change with increasing index number if the background is increasing for a given m/z
                List<clsBaselineNoiseStatsSegment> noiseStatSegments = null;
                bool success = mMASICPeakFinder.ComputeDualTrimmedNoiseLevelTTest(fullSICIntensities1D, 0, fullSICDataCount[mzIndexWork] - 1, masicOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions, out noiseStatSegments);
                if (!success)
                {
                    SetLocalErrorCode(clsMASIC.eMasicErrorCodes.FindSICPeaksError, true);
                    return false;
                }

                mzSearchChunk[mzIndexWork].BaselineNoiseStatSegments = noiseStatSegments;
                clsSICPotentialAreaStats potentialAreaStatsInFullSIC = null;

                // Compute the minimum potential peak area in the entire SIC, populating udtSICPotentialAreaStatsInFullSIC
                mMASICPeakFinder.FindPotentialPeakArea(fullSICDataCount[mzIndexWork], fullSICIntensities1D, out potentialAreaStatsInFullSIC, masicOptions.SICOptions.SICPeakFinderOptions);
                clsSICPotentialAreaStats potentialAreaStatsForPeak = null;
                int scanIndexObservedInFullSIC = mzSearchChunk[mzIndexWork].ScanIndexMax;

                // Initialize sicDetails
                var sicDetails = new clsSICDetails() { SICScanType = clsScanList.eScanTypeConstants.SurveyScan };

                // Populate sicDetails using the data centered around the highest intensity in fullSICIntensities
                // Note that this function will update sicPeak.IndexObserved
                var sicPeak = ExtractSICDetailsFromFullSIC(mzIndexWork, mzSearchChunk[mzIndexWork].BaselineNoiseStatSegments, fullSICDataCount[mzIndexWork], fullSICScanIndices, fullSICIntensities, fullSICMasses, scanList, scanIndexObservedInFullSIC, sicDetails, masicOptions, scanNumScanConverter, false, 0);
                UpdateSICStatsUsingLargestPeak(sicDetails, potentialAreaStatsForPeak, sicPeak, masicOptions, potentialAreaStatsInFullSIC, scanList, mzSearchChunk, mzIndexWork, parentIonIndices, debugParentIonIndexToFind, dataOutputHandler, xmlResultsWriter, parentIonUpdated, ref parentIonsProcessed);

                // --------------------------------------------------------
                // Now step through the parent ions and process those that were not updated using sicPeak
                // For each, search for the closest peak in SICIntensity
                // --------------------------------------------------------
                for (int parentIonIndexPointer = mzSearchChunk[mzIndexWork].MZIndexStart; parentIonIndexPointer <= mzSearchChunk[mzIndexWork].MZIndexEnd; parentIonIndexPointer++)
                {
                    if (parentIonUpdated[parentIonIndexPointer])
                        continue;
                    if (parentIonIndices[parentIonIndexPointer] == debugParentIonIndexToFind)
                    {
                        // ReSharper disable once RedundantAssignment
                        scanIndexObservedInFullSIC = -1;
                    }

                    clsSmoothedYDataSubset smoothedYDataSubsetInSearchChunk = null;
                    var currentParentIon = scanList.ParentIons[parentIonIndices[parentIonIndexPointer]];

                    // Clear udtSICPotentialAreaStatsForPeak
                    currentParentIon.SICStats.SICPotentialAreaStatsForPeak = new clsSICPotentialAreaStats();

                    // Record the index in the Full SIC that the parent ion mass was first observed
                    // Search for .SurveyScanIndex in fullSICScanIndices
                    scanIndexObservedInFullSIC = -1;
                    for (int dataIndex = 0; dataIndex <= fullSICDataCount[mzIndexWork] - 1; dataIndex++)
                    {
                        if (fullSICScanIndices[mzIndexWork, dataIndex] >= currentParentIon.SurveyScanIndex)
                        {
                            scanIndexObservedInFullSIC = dataIndex;
                            break;
                        }
                    }

                    if (scanIndexObservedInFullSIC == -1)
                    {
                        // Match wasn't found; this is unexpected
                        ReportError("Programming error: survey scan index not found in fullSICScanIndices()", clsMASIC.eMasicErrorCodes.FindSICPeaksError);
                        scanIndexObservedInFullSIC = 0;
                    }

                    // Populate udtSICDetails using the data centered around scanIndexObservedInFullSIC
                    // Note that this function will update sicStatsPeak.IndexObserved
                    var sicStatsPeak = ExtractSICDetailsFromFullSIC(mzIndexWork, mzSearchChunk[mzIndexWork].BaselineNoiseStatSegments, fullSICDataCount[mzIndexWork], fullSICScanIndices, fullSICIntensities, fullSICMasses, scanList, scanIndexObservedInFullSIC, sicDetails, masicOptions, scanNumScanConverter, currentParentIon.CustomSICPeak, currentParentIon.CustomSICPeakScanOrAcqTimeTolerance);
                    currentParentIon.SICStats.Peak = sicStatsPeak;
                    bool returnClosestPeak = !currentParentIon.CustomSICPeak;
                    bool peakIsValid = mMASICPeakFinder.FindSICPeakAndArea(sicDetails.SICData, out var potentialAreaStatsForPeakOut, sicStatsPeak, out smoothedYDataSubsetInSearchChunk, masicOptions.SICOptions.SICPeakFinderOptions, potentialAreaStatsInFullSIC, returnClosestPeak, scanList.SIMDataPresent, false);
                    currentParentIon.SICStats.SICPotentialAreaStatsForPeak = potentialAreaStatsForPeakOut;
                    StorePeakInParentIon(scanList, parentIonIndices[parentIonIndexPointer], sicDetails, currentParentIon.SICStats.SICPotentialAreaStatsForPeak, sicStatsPeak, peakIsValid);

                    // Possibly save the stats for this SIC to the SICData file
                    dataOutputHandler.SaveSICDataToText(masicOptions.SICOptions, scanList, parentIonIndices[parentIonIndexPointer], sicDetails);

                    // Save the stats for this SIC to the XML file
                    xmlResultsWriter.SaveDataToXML(scanList, parentIonIndices[parentIonIndexPointer], sicDetails, smoothedYDataSubsetInSearchChunk, dataOutputHandler);
                    parentIonUpdated[parentIonIndexPointer] = true;
                    parentIonsProcessed += 1;
                }

                // ---------------------------------------------------------
                // Update progress
                // ---------------------------------------------------------
                try
                {
                    if (scanList.ParentIons.Count > 1)
                    {
                        UpdateProgress(Convert.ToInt16(parentIonsProcessed / (double)(scanList.ParentIons.Count - 1) * 100));
                    }
                    else
                    {
                        UpdateProgress(0);
                    }

                    UpdateCacheStats(spectraCache);
                    if (masicOptions.AbortProcessing)
                    {
                        scanList.ProcessingIncomplete = true;
                        break;
                    }

                    if (parentIonsProcessed % 100 == 0)
                    {
                        if (DateTime.UtcNow.Subtract(masicOptions.LastParentIonProcessingLogTime).TotalSeconds >= 10 || parentIonsProcessed % 500 == 0)
                        {
                            ReportMessage("Parent Ions Processed: " + parentIonsProcessed.ToString());
                            Console.Write(".");
                            masicOptions.LastParentIonProcessingLogTime = DateTime.UtcNow;
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

        private void UpdateSICStatsUsingLargestPeak(clsSICDetails sicDetails, clsSICPotentialAreaStats potentialAreaStatsForPeak, clsSICStatsPeak sicPeak, clsMASICOptions masicOptions, clsSICPotentialAreaStats potentialAreaStatsInFullSIC, clsScanList scanList, IReadOnlyList<clsMzSearchInfo> mzSearchChunk, int mzIndexWork, IList<int> parentIonIndices, int debugParentIonIndexToFind, clsDataOutput dataOutputHandler, clsXMLResultsWriter xmlResultsWriter, IList<bool> parentIonUpdated, ref int parentIonsProcessed)
        {
            clsSmoothedYDataSubset smoothedYDataSubset = null;

            // Find the largest peak in the SIC for this m/z
            bool largestPeakFound = mMASICPeakFinder.FindSICPeakAndArea(sicDetails.SICData, out potentialAreaStatsForPeak, sicPeak, out smoothedYDataSubset, masicOptions.SICOptions.SICPeakFinderOptions, potentialAreaStatsInFullSIC, true, scanList.SIMDataPresent, false);
            if (!largestPeakFound)
            {
                return;
            }

            // --------------------------------------------------------
            // Step through the parent ions and see if .SurveyScanIndex is contained in udtSICPeak
            // If it is, assign the stats of the largest peak to the given parent ion
            // --------------------------------------------------------

            var mzIndexSICIndices = sicDetails.SICScanIndices;
            for (int parentIonIndexPointer = mzSearchChunk[mzIndexWork].MZIndexStart; parentIonIndexPointer <= mzSearchChunk[mzIndexWork].MZIndexEnd; parentIonIndexPointer++)
            {
                bool storePeak = false;

                // Use this for debugging
                if (parentIonIndices[parentIonIndexPointer] == debugParentIonIndexToFind)
                {
                    storePeak = false;
                }

                if (scanList.ParentIons[parentIonIndices[parentIonIndexPointer]].CustomSICPeak)
                    continue;

                // Assign the stats of the largest peak to each parent ion with .SurveyScanIndex contained in the peak
                var currentParentIon = scanList.ParentIons[parentIonIndices[parentIonIndexPointer]];
                if (currentParentIon.SurveyScanIndex >= mzIndexSICIndices[sicPeak.IndexBaseLeft] && currentParentIon.SurveyScanIndex <= mzIndexSICIndices[sicPeak.IndexBaseRight])
                {
                    storePeak = true;
                }

                if (!storePeak)
                    continue;
                StorePeakInParentIon(scanList, parentIonIndices[parentIonIndexPointer], sicDetails, potentialAreaStatsForPeak, sicPeak, true);

                // Possibly save the stats for this SIC to the SICData file
                dataOutputHandler.SaveSICDataToText(masicOptions.SICOptions, scanList, parentIonIndices[parentIonIndexPointer], sicDetails);

                // Save the stats for this SIC to the XML file
                xmlResultsWriter.SaveDataToXML(scanList, parentIonIndices[parentIonIndexPointer], sicDetails, smoothedYDataSubset, dataOutputHandler);
                parentIonUpdated[parentIonIndexPointer] = true;
                parentIonsProcessed += 1;
            }
        }

        public bool StorePeakInParentIon(clsScanList scanList, int parentIonIndex, clsSICDetails sicDetails, clsSICPotentialAreaStats potentialAreaStatsForPeak, clsSICStatsPeak sicPeak, bool peakIsValid)
        {
            int dataIndex;
            int scanIndexObserved;
            int fragScanNumber;
            bool processingMRMPeak;
            try
            {
                if (sicDetails.SICDataCount == 0)
                {
                    // Either .SICData is nothing or no SIC data exists
                    // Cannot find peaks for this parent ion
                    var sicStatsPeak = scanList.ParentIons[parentIonIndex].SICStats.Peak;
                    sicStatsPeak.IndexObserved = 0;
                    sicStatsPeak.IndexBaseLeft = 0;
                    sicStatsPeak.IndexBaseRight = 0;
                    sicStatsPeak.IndexMax = 0;
                    return true;
                }

                var sicData = sicDetails.SICData;
                var currentParentIon = scanList.ParentIons[parentIonIndex];
                scanIndexObserved = currentParentIon.SurveyScanIndex;
                if (scanIndexObserved < 0)
                    scanIndexObserved = 0;
                if (currentParentIon.MRMDaughterMZ > 0)
                {
                    processingMRMPeak = true;
                }
                else
                {
                    processingMRMPeak = false;
                }

                var sicStats = currentParentIon.SICStats;
                sicStats.SICPotentialAreaStatsForPeak = potentialAreaStatsForPeak;

                // Clone sicPeak since it will be updated to include the ParentIonIntensity for this parent ion's fragmentation scan
                sicStats.Peak = sicPeak.Clone();
                sicStats.ScanTypeForPeakIndices = sicDetails.SICScanType;
                if (processingMRMPeak)
                {
                    if (sicStats.ScanTypeForPeakIndices != clsScanList.eScanTypeConstants.FragScan)
                    {
                        // ScanType is not FragScan; this is unexpected
                        ReportError("Programming error: udtSICDetails.SICScanType is not FragScan even though we're processing an MRM peak", clsMASIC.eMasicErrorCodes.FindSICPeaksError);
                        sicStats.ScanTypeForPeakIndices = clsScanList.eScanTypeConstants.FragScan;
                    }
                }

                if (processingMRMPeak)
                {
                    sicStats.Peak.IndexObserved = 0;
                }
                else
                {
                    // Record the index (of data in .SICData) that the parent ion mass was first observed
                    // This is not necessarily the same as udtSICPeak.IndexObserved, so we need to search for it here

                    // Search for scanIndexObserved in sicScanIndices()
                    sicStats.Peak.IndexObserved = -1;
                    for (dataIndex = 0; dataIndex <= sicDetails.SICDataCount - 1; dataIndex++)
                    {
                        if (sicData[dataIndex].ScanIndex == scanIndexObserved)
                        {
                            sicStats.Peak.IndexObserved = dataIndex;
                            break;
                        }
                    }

                    if (sicStats.Peak.IndexObserved == -1)
                    {
                        // Match wasn't found; this is unexpected
                        ReportError("Programming error: survey scan index not found in sicScanIndices", clsMASIC.eMasicErrorCodes.FindSICPeaksError);
                        sicStats.Peak.IndexObserved = 0;
                    }
                }

                if (scanList.FragScans.Count > 0 && currentParentIon.FragScanIndices[0] < scanList.FragScans.Count)
                {
                    // Record the fragmentation scan number
                    fragScanNumber = scanList.FragScans[currentParentIon.FragScanIndices[0]].ScanNumber;
                }
                else
                {
                    // Use the parent scan number as the fragmentation scan number
                    // This is OK based on how mMASICPeakFinder.ComputeParentIonIntensity() uses fragScanNumber
                    fragScanNumber = scanList.SurveyScans[currentParentIon.SurveyScanIndex].ScanNumber;
                }

                if (processingMRMPeak)
                {
                    sicStats.Peak.ParentIonIntensity = 0;
                }
                else
                {
                    // Determine the value for .ParentIonIntensity
                    mMASICPeakFinder.ComputeParentIonIntensity(sicData, sicStats.Peak, fragScanNumber);
                }

                if (peakIsValid)
                {
                    // Record the survey scan indices of the peak max, start, and end
                    // Note that .ScanTypeForPeakIndices was set earlier in this function
                    sicStats.PeakScanIndexMax = sicData[sicStats.Peak.IndexMax].ScanIndex;
                    sicStats.PeakScanIndexStart = sicData[sicStats.Peak.IndexBaseLeft].ScanIndex;
                    sicStats.PeakScanIndexEnd = sicData[sicStats.Peak.IndexBaseRight].ScanIndex;
                }
                else
                {
                    // No peak found
                    sicStats.PeakScanIndexMax = sicData[sicStats.Peak.IndexMax].ScanIndex;
                    sicStats.PeakScanIndexStart = sicStats.PeakScanIndexMax;
                    sicStats.PeakScanIndexEnd = sicStats.PeakScanIndexMax;
                    var sicStatsPeak = sicStats.Peak;
                    sicStatsPeak.MaxIntensityValue = sicData[sicStatsPeak.IndexMax].Intensity;
                    sicStatsPeak.IndexBaseLeft = sicStatsPeak.IndexMax;
                    sicStatsPeak.IndexBaseRight = sicStatsPeak.IndexMax;
                    sicStatsPeak.FWHMScanWidth = 1;

                    // Assign the intensity of the peak at the observed maximum to the area
                    sicStatsPeak.Area = sicStatsPeak.MaxIntensityValue;
                    sicStatsPeak.SignalToNoiseRatio = clsMASICPeakFinder.ComputeSignalToNoise(sicStatsPeak.MaxIntensityValue, sicStatsPeak.BaselineNoiseStats.NoiseLevel);
                }

                // Update .OptimalPeakApexScanNumber
                // Note that a valid peak will typically have .IndexBaseLeft or .IndexBaseRight different from .IndexMax
                int scanIndexPointer = sicData[currentParentIon.SICStats.Peak.IndexMax].ScanIndex;
                if (processingMRMPeak)
                {
                    currentParentIon.OptimalPeakApexScanNumber = scanList.FragScans[scanIndexPointer].ScanNumber;
                }
                else
                {
                    currentParentIon.OptimalPeakApexScanNumber = scanList.SurveyScans[scanIndexPointer].ScanNumber;
                }

                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error finding SIC peaks and their areas", ex, clsMASIC.eMasicErrorCodes.FindSICPeaksError);
                return false;
            }
        }
    }
}
