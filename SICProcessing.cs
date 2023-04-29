using System;
using System.Collections.Generic;
using System.Linq;
using MASIC.Data;
using MASIC.DataOutput;
using MASIC.Options;
using MASICPeakFinder;

namespace MASIC
{
    /// <summary>
    /// This class creates selected ion chromatograms for parent ions
    /// </summary>
    public class SICProcessing : MasicEventNotifier
    {
        private const string CREATING_SICS = "Creating SIC's for parent ions";

        private readonly clsMASICPeakFinder mMASICPeakFinder;
        private readonly MRMProcessing mMRMProcessor;

        /// <summary>
        /// Constructor
        /// </summary>
        public SICProcessing(clsMASICPeakFinder peakFinder, MRMProcessing mrmProcessor)
        {
            mMASICPeakFinder = peakFinder;
            mMRMProcessor = mrmProcessor;
        }

        private static short ComputeMzSearchChunkProgress(
            int parentIonsProcessed, int parentIonsCount,
            int surveyScanIndex, int surveyScansCount,
            double mzSearchChunkProgressFraction)
        {
            var progressAddon = surveyScanIndex / (double)surveyScansCount * mzSearchChunkProgressFraction * 100;
            if (parentIonsCount < 1)
            {
                return (short)Math.Round(progressAddon, 0);
            }

            var percentComplete = parentIonsProcessed / (double)(parentIonsCount - 1) * 100 + progressAddon;
            return (short)Math.Round(percentComplete, 0);
        }

        private static List<MzBinInfo> CreateMZLookupList(
            MASICOptions masicOptions,
            ScanList scanList,
            bool processSIMScans,
            int simIndex)
        {
            var mzBinList = new List<MzBinInfo>(scanList.ParentIons.Count);

            var sicOptions = masicOptions.SICOptions;

            for (var parentIonIndex = 0; parentIonIndex < scanList.ParentIons.Count; parentIonIndex++)
            {
                bool includeParentIon;

                if (scanList.ParentIons[parentIonIndex].IsDIA)
                {
                    includeParentIon = false;
                }
                else if (scanList.ParentIons[parentIonIndex].MRMDaughterMZ > 0)
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
                            includeParentIon = surveyScan.SIMIndex == simIndex;
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

                if (!includeParentIon)
                {
                    continue;
                }

                var newMzBin = new MzBinInfo
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

            // Sort mzBinList by m/z
            return mzBinList.OrderBy(item => item.MZ).ToList();
        }

        /// <summary>
        /// Create selected ion chromatograms for the parent ions
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="spectraCache"></param>
        /// <param name="masicOptions"></param>
        /// <param name="dataOutputHandler"></param>
        /// <param name="sicProcessor"></param>
        /// <param name="xmlResultsWriter"></param>
        public bool CreateParentIonSICs(
            ScanList scanList,
            SpectraCache spectraCache,
            MASICOptions masicOptions,
            DataOutput.DataOutput dataOutputHandler,
            SICProcessing sicProcessor,
            XMLResultsWriter xmlResultsWriter)
        {
            var success = false;

            if (scanList.ParentIons.Count == 0)
            {
                // No parent ions
                if (masicOptions.SuppressNoParentIonsError)
                {
                    return true;
                }

                SetLocalErrorCode(clsMASIC.MasicErrorCodes.NoParentIonsFoundInInputFile);
                return false;
            }

            if (scanList.SurveyScans.Count == 0)
            {
                // No survey scans
                if (masicOptions.SuppressNoParentIonsError)
                {
                    return true;
                }

                SetLocalErrorCode(clsMASIC.MasicErrorCodes.NoSurveyScansFoundInInputFile);
                return false;
            }

            try
            {
                var parentIonsProcessed = 0;
                masicOptions.LastParentIonProcessingLogTime = DateTime.UtcNow;

                UpdateProgress(0, CREATING_SICS);
                Console.Write(CREATING_SICS);
                ReportMessage(CREATING_SICS);

                // Create an array of m/z values in scanList.ParentIons, sort by m/z
                // Next, step through the data in order of m/z, creating SICs for each grouping of m/z's within half of the SIC tolerance

                // First process the non SIM, non MRM scans
                // If this file only has MRM scans, CreateMZLookupList will return False
                var mzBinList = CreateMZLookupList(masicOptions, scanList, false, 0);
                if (mzBinList.Count > 0)
                {
                    success = ProcessMZList(
                        scanList, spectraCache, masicOptions,
                        dataOutputHandler, xmlResultsWriter,
                        mzBinList, false, 0, ref parentIonsProcessed);
                }

                if (success && !masicOptions.CustomSICList.LimitSearchToCustomMZList)
                {
                    // Now process the SIM scans (if any)
                    // First, see if any SIMScans are present and determine the maximum SIM Index
                    var simIndexMax = -1;
                    foreach (var parentIon in scanList.ParentIons)
                    {
                        var surveyScan = scanList.SurveyScans[parentIon.SurveyScanIndex];
                        if (surveyScan.SIMScan && surveyScan.SIMIndex > simIndexMax)
                        {
                            simIndexMax = surveyScan.SIMIndex;
                        }
                    }

                    // Now process each SIM Scan type
                    for (var simIndex = 0; simIndex <= simIndexMax; simIndex++)
                    {
                        var mzBinListSIM = CreateMZLookupList(masicOptions, scanList, true, simIndex);
                        if (mzBinListSIM.Count > 0)
                        {
                            ProcessMZList(
                                scanList, spectraCache, masicOptions,
                                dataOutputHandler, xmlResultsWriter,
                                mzBinListSIM, true, simIndex, ref parentIonsProcessed);
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
                ReportError("Error creating Parent Ion SICs", ex, clsMASIC.MasicErrorCodes.CreateSICsError);
                return false;
            }
        }

        private SICStatsPeak ExtractSICDetailsFromFullSIC(
            int mzIndexWork,
            List<BaselineNoiseStatsSegment> baselineNoiseStatSegments,
            int fullSICDataCount,
            int[,] fullSICScanIndices,
            double[,] fullSICIntensities,
            double[,] fullSICMasses,
            ScanList scanList,
            int scanIndexObservedInFullSIC,
            SICDetails sicDetails,
            MASICOptions masicOptions,
            ScanNumScanTimeConversion scanNumScanConverter,
            bool customSICPeak,
            float customSICPeakScanOrAcqTimeTolerance)
        {
            // Minimum number of scans to extend left or right of the scan that meets the minimum intensity threshold requirement
            const int MINIMUM_NOISE_SCANS_TO_INCLUDE = 10;

            float customSICScanToleranceMinutesHalfWidth = 0;

            // Pointers to entries in fullSICScanIndices() and fullSICIntensities()
            int scanIndexStart = 0, scanIndexEnd = 0;

            var maximumIntensity = 0.0;

            var sicOptions = masicOptions.SICOptions;

            var baselineNoiseStats = mMASICPeakFinder.LookupNoiseStatsUsingSegments(scanIndexObservedInFullSIC, baselineNoiseStatSegments);

            // Initialize the peak
            var sicPeak = new SICStatsPeak
            {
                BaselineNoiseStats = baselineNoiseStats
            };

            // Initialize the values for the maximum width of the SIC peak; these might get altered for custom SIC values
            var maxSICPeakWidthMinutesBackward = sicOptions.MaxSICPeakWidthMinutesBackward;
            var maxSICPeakWidthMinutesForward = sicOptions.MaxSICPeakWidthMinutesForward;

            // Limit the data examined to a portion of fullSICScanIndices() and fullSICIntensities, populating sicDetails
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
                    if (masicOptions.CustomSICList.ScanToleranceType == CustomSICList.CustomSICScanTypeConstants.Relative && customSICPeakScanOrAcqTimeTolerance > 10)
                    {
                        // Relative scan time should only range from 0 to 1; we'll allow values up to 10
                        customSICPeakScanOrAcqTimeTolerance = 10;
                    }

                    customSICScanToleranceMinutesHalfWidth = scanNumScanConverter.ScanOrAcqTimeToScanTime(
                        scanList, customSICPeakScanOrAcqTimeTolerance / 2, masicOptions.CustomSICList.ScanToleranceType, true);
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
                ReportError("Error initializing SIC start/end indices", ex, clsMASIC.MasicErrorCodes.CreateSICsError);
            }

            if (scanIndexEnd >= scanIndexStart)
            {
                var scanIndexMax = 0;

                try
                {
                    // Start by using the 3 survey scans centered around scanIndexObservedInFullSIC
                    maximumIntensity = -1;
                    scanIndexMax = -1;
                    for (var scanIndex = scanIndexStart; scanIndex <= scanIndexEnd; scanIndex++)
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
                    ReportError("Error while creating initial SIC", ex, clsMASIC.MasicErrorCodes.CreateSICsError);
                }

                // Now extend the SIC, stepping left and right until a threshold is reached
                var leftDone = false;
                var rightDone = false;

                // The index of the first scan found to be below threshold (on the left)
                var scanIndexBelowThresholdLeft = -1;

                // The index of the first scan found to be below threshold (on the right)
                var scanIndexBelowThresholdRight = -1;

                while (scanIndexStart > 0 && !leftDone || scanIndexEnd < fullSICDataCount - 1 && !rightDone)
                {
                    try
                    {
                        // Extend the SIC to the left until the threshold is reached
                        if (scanIndexStart > 0 && !leftDone)
                        {
                            if (fullSICIntensities[mzIndexWork, scanIndexStart] < sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum ||
                                fullSICIntensities[mzIndexWork, scanIndexStart] < sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax * maximumIntensity ||
                                fullSICIntensities[mzIndexWork, scanIndexStart] < sicPeak.BaselineNoiseStats.NoiseLevel)
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

                            var peakWidthMinutesBackward =
                                scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexObservedInFullSIC]].ScanTime -
                                scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexStart]].ScanTime;

                            if (leftDone)
                            {
                                // Require a minimum distance of InitialPeakWidthScansMaximum data points to the left of scanIndexObservedInFullSIC and to the left of scanIndexMax
                                if (scanIndexObservedInFullSIC - scanIndexStart < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum)
                                    leftDone = false;

                                if (scanIndexMax - scanIndexStart < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum)
                                    leftDone = false;

                                // For custom SIC values, make sure the scan range has been satisfied
                                if (leftDone && customSICPeak && peakWidthMinutesBackward < customSICScanToleranceMinutesHalfWidth)
                                {
                                    leftDone = false;
                                }
                            }

                            if (!leftDone)
                            {
                                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                                if (scanIndexStart == 0)
                                {
                                    leftDone = true;
                                }
                                else
                                {
                                    scanIndexStart--;
                                    if (fullSICIntensities[mzIndexWork, scanIndexStart] > maximumIntensity)
                                    {
                                        maximumIntensity = fullSICIntensities[mzIndexWork, scanIndexStart];
                                        scanIndexMax = scanIndexStart;
                                    }
                                }
                            }

                            peakWidthMinutesBackward = scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexObservedInFullSIC]].ScanTime -
                                scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexStart]].ScanTime;

                            if (peakWidthMinutesBackward >= maxSICPeakWidthMinutesBackward)
                            {
                                leftDone = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ReportError("Error extending SIC to the left", ex, clsMASIC.MasicErrorCodes.CreateSICsError);
                    }

                    try
                    {
                        // Extend the SIC to the right until the threshold is reached
                        if (scanIndexEnd < fullSICDataCount - 1 && !rightDone)
                        {
                            if (fullSICIntensities[mzIndexWork, scanIndexEnd] < sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum ||
                                fullSICIntensities[mzIndexWork, scanIndexEnd] < sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax * maximumIntensity ||
                                fullSICIntensities[mzIndexWork, scanIndexEnd] < sicPeak.BaselineNoiseStats.NoiseLevel)
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

                            var peakWidthMinutesForward =
                                scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexEnd]].ScanTime -
                                scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexObservedInFullSIC]].ScanTime;

                            if (rightDone)
                            {
                                // Require a minimum distance of InitialPeakWidthScansMaximum data points to the right of scanIndexObservedInFullSIC and to the right of scanIndexMax
                                if (scanIndexEnd - scanIndexObservedInFullSIC < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum)
                                    rightDone = false;
                                if (scanIndexEnd - scanIndexMax < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum)
                                    rightDone = false;

                                // For custom SIC values, make sure the scan range has been satisfied
                                if (rightDone && customSICPeak && peakWidthMinutesForward < customSICScanToleranceMinutesHalfWidth)
                                {
                                    rightDone = false;
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
                                    scanIndexEnd++;
                                    if (fullSICIntensities[mzIndexWork, scanIndexEnd] > maximumIntensity)
                                    {
                                        maximumIntensity = fullSICIntensities[mzIndexWork, scanIndexEnd];
                                        scanIndexMax = scanIndexEnd;
                                    }
                                }
                            }

                            peakWidthMinutesForward = scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexEnd]].ScanTime -
                                scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndexObservedInFullSIC]].ScanTime;

                            if (peakWidthMinutesForward >= maxSICPeakWidthMinutesForward)
                            {
                                rightDone = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ReportError("Error extending SIC to the right", ex, clsMASIC.MasicErrorCodes.CreateSICsError);
                    }
                }    // While Not LeftDone and Not RightDone
            }

            // Populate sicDetails with the data between scanIndexStart and scanIndexEnd
            if (scanIndexStart < 0)
                scanIndexStart = 0;
            if (scanIndexEnd >= fullSICDataCount)
                scanIndexEnd = fullSICDataCount - 1;

            if (scanIndexEnd < scanIndexStart)
            {
                ReportError("Programming error: scanIndexEnd < scanIndexStart", clsMASIC.MasicErrorCodes.FindSICPeaksError);
                scanIndexEnd = scanIndexStart;
            }

            try
            {
                // Copy the scan index values from fullSICScanIndices to .SICScanIndices()
                // Copy the intensity values from fullSICIntensities() to .SICData()
                // Copy the mz values from fullSICMasses() to .SICMasses()

                sicDetails.SICScanType = ScanList.ScanTypeConstants.SurveyScan;
                sicDetails.SICData.Clear();

                sicPeak.IndexObserved = 0;
                for (var scanIndex = scanIndexStart; scanIndex <= scanIndexEnd; scanIndex++)
                {
                    if (fullSICScanIndices[mzIndexWork, scanIndex] >= 0)
                    {
                        sicDetails.AddData(scanList.SurveyScans[fullSICScanIndices[mzIndexWork, scanIndex]].ScanNumber,
                            fullSICIntensities[mzIndexWork, scanIndex],
                            fullSICMasses[mzIndexWork, scanIndex],
                            fullSICScanIndices[mzIndexWork, scanIndex]);

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
                ReportError("Error populating .SICScanIndices, .SICData, and .SICMasses", ex, clsMASIC.MasicErrorCodes.CreateSICsError);
            }

            return sicPeak;
        }

        private bool ProcessMZList(
            ScanList scanList,
            SpectraCache spectraCache,
            MASICOptions masicOptions,
            DataOutput.DataOutput dataOutputHandler,
            XMLResultsWriter xmlResultsWriter,
            IReadOnlyList<MzBinInfo> mzBinList,
            bool processSIMScans,
            int simIndex,
            ref int parentIonsProcessed)
        {
            // Step through the data in order of m/z, creating SICs for each grouping of m/z's within half of the SIC tolerance
            // Note that mzBinList and parentIonIndices() are parallel arrays, with mzBinList() sorted on ascending m/z
            const int MAX_RAW_DATA_MEMORY_USAGE_MB = 50;

            int maxMZCountInChunk;

            var mzSearchChunks = new List<MzSearchInfo>(mzBinList.Count);

            bool[] parentIonUpdated;

            try
            {
                // Determine the maximum number of m/z values to process simultaneously
                // Limit the total memory usage to ~50 MB
                // Each m/z value will require 12 bytes per scan

                if (scanList.SurveyScans.Count > 0)
                {
                    maxMZCountInChunk = (int)(MAX_RAW_DATA_MEMORY_USAGE_MB * 1024 * 1024 / (double)(scanList.SurveyScans.Count * 12));
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
                ReportError("Error reserving memory for the m/z chunks", ex, clsMASIC.MasicErrorCodes.CreateSICsError);
                return false;
            }

            try
            {
                var dataAggregation = new DataAggregation();
                RegisterEvents(dataAggregation);

                var scanNumScanConverter = new ScanNumScanTimeConversion();
                RegisterEvents(scanNumScanConverter);

                var parentIonIndices = (from item in mzBinList select item.ParentIonIndex).ToList();

                for (var mzIndex = 0; mzIndex < mzBinList.Count; mzIndex++)
                {
                    // ---------------------------------------------------------
                    // Find the next group of m/z values to use, starting with mzIndex
                    // ---------------------------------------------------------

                    // Initially set the MZIndexStart to mzIndex

                    // Look for adjacent m/z values within udtMZBinList(.MZIndexStart).MZToleranceDa / 2
                    // of the m/z value that starts this group
                    // Only group m/z values with the same udtMZBinList().MZTolerance and udtMZBinList().MZToleranceIsPPM values

                    var mzSearchChunk = new MzSearchInfo
                    {
                        MZIndexStart = mzIndex,
                        MZTolerance = mzBinList[mzIndex].MZTolerance,
                        MZToleranceIsPPM = mzBinList[mzIndex].MZToleranceIsPPM
                    };

                    double mzToleranceDa;
                    if (mzSearchChunk.MZToleranceIsPPM)
                    {
                        mzToleranceDa = Utilities.PPMToMass(mzSearchChunk.MZTolerance, mzBinList[mzSearchChunk.MZIndexStart].MZ);
                    }
                    else
                    {
                        mzToleranceDa = mzSearchChunk.MZTolerance;
                    }

                    while (mzIndex < mzBinList.Count - 2 &&
                           Math.Abs(mzBinList[mzIndex + 1].MZTolerance - mzSearchChunk.MZTolerance) < double.Epsilon &&
                           mzBinList[mzIndex + 1].MZToleranceIsPPM == mzSearchChunk.MZToleranceIsPPM &&
                           mzBinList[mzIndex + 1].MZ - mzBinList[mzSearchChunk.MZIndexStart].MZ <= mzToleranceDa / 2)
                    {
                        mzIndex++;
                    }

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
                        mzSearchChunk.MZIndexMidpoint = mzSearchChunk.MZIndexStart + (int)Math.Round((mzSearchChunk.MZIndexEnd - mzSearchChunk.MZIndexStart) / (double)2);
                        mzSearchChunk.SearchMZ = mzBinList[mzSearchChunk.MZIndexMidpoint].MZ;
                    }
                    else
                    {
                        // Even number of points; average the values on either side of (.mzIndexEnd - .mzIndexStart / 2)
                        mzSearchChunk.MZIndexMidpoint = mzSearchChunk.MZIndexStart + (int)Math.Floor((mzSearchChunk.MZIndexEnd - mzSearchChunk.MZIndexStart) / 2.0);
                        mzSearchChunk.SearchMZ = (mzBinList[mzSearchChunk.MZIndexMidpoint].MZ + mzBinList[mzSearchChunk.MZIndexMidpoint + 1].MZ) / 2;
                    }

                    mzSearchChunks.Add(mzSearchChunk);

                    if (mzSearchChunks.Count >= maxMZCountInChunk || mzIndex == mzBinList.Count - 1)
                    {
                        // ---------------------------------------------------------
                        // Reached maxMZCountInChunk m/z value
                        // Process all of the m/z values in udtMZSearchChunk
                        // ---------------------------------------------------------

                        var mzSearchChunkProgressFraction = mzSearchChunks.Count / (double)mzBinList.Count;

                        var success = ProcessMzSearchChunk(
                            masicOptions,
                            scanList,
                            dataAggregation, dataOutputHandler, xmlResultsWriter,
                            spectraCache, scanNumScanConverter,
                            mzSearchChunks,
                            mzSearchChunkProgressFraction,
                            parentIonIndices,
                            processSIMScans,
                            simIndex,
                            parentIonUpdated,
                            ref parentIonsProcessed);

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
                }

                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error processing the m/z chunks to create the SIC data", ex, clsMASIC.MasicErrorCodes.CreateSICsError);
                return false;
            }
        }

        private bool ProcessMzSearchChunk(
            MASICOptions masicOptions,
            ScanList scanList,
            DataAggregation dataAggregation,
            DataOutput.DataOutput dataOutputHandler,
            XMLResultsWriter xmlResultsWriter,
            SpectraCache spectraCache,
            ScanNumScanTimeConversion scanNumScanConverter,
            IReadOnlyList<MzSearchInfo> mzSearchChunk,
            double mzSearchChunkProgressFraction,
            IList<int> parentIonIndices,
            bool processSIMScans,
            int simIndex,
            IList<bool> parentIonUpdated,
            ref int parentIonsProcessed)
        {
            // The following are 2D arrays, ranging from 0 to mzSearchChunkCount-1 in the first dimension and 0 to .SurveyScans.Count - 1 in the second dimension
            // We could have included these in udtMZSearchChunk but memory management is more efficient if I use 2D arrays for this data

            // Reserve room in fullSICScanIndices for at most maxMZCountInChunk values and .SurveyScans.Count scans
            // Count of the number of valid entries in the second dimension of the above 3 arrays
            var fullSICDataCount = new int[mzSearchChunk.Count];

            // Pointer into .SurveyScans
            var fullSICScanIndices = new int[mzSearchChunk.Count, scanList.SurveyScans.Count];
            var fullSICIntensities = new double[mzSearchChunk.Count, scanList.SurveyScans.Count];
            var fullSICMasses = new double[mzSearchChunk.Count, scanList.SurveyScans.Count];

            // The following is a 1D array, containing the SIC intensities for a single m/z group
            var fullSICIntensities1D = new double[scanList.SurveyScans.Count];

            // Initialize .MaximumIntensity and .ScanIndexMax
            // Additionally, reset fullSICDataCount() and, for safety, set fullSICScanIndices() to -1
            for (var mzIndexWork = 0; mzIndexWork < mzSearchChunk.Count; mzIndexWork++)
            {
                mzSearchChunk[mzIndexWork].ResetMaxIntensity();

                fullSICDataCount[mzIndexWork] = 0;
                for (var surveyScanIndex = 0; surveyScanIndex < scanList.SurveyScans.Count; surveyScanIndex++)
                {
                    fullSICScanIndices[mzIndexWork, surveyScanIndex] = -1;
                }
            }

            // ---------------------------------------------------------
            // Step through scanList to obtain the scan numbers and intensity data for each .SearchMZ in udtMZSearchChunk
            // We're stepping scan by scan since the process of loading a scan from disk is slower than the process of searching for each m/z in the scan
            // ---------------------------------------------------------
            for (var surveyScanIndex = 0; surveyScanIndex < scanList.SurveyScans.Count; surveyScanIndex++)
            {
                bool useScan;

                if (processSIMScans)
                {
                    useScan = scanList.SurveyScans[surveyScanIndex].SIMScan &&
                              scanList.SurveyScans[surveyScanIndex].SIMIndex == simIndex;
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

                if (!spectraCache.GetSpectrum(scanList.SurveyScans[surveyScanIndex].ScanNumber, out var spectrum, true))
                {
                    SetLocalErrorCode(clsMASIC.MasicErrorCodes.ErrorUncachingSpectrum);
                    return false;
                }

                for (var mzIndexWork = 0; mzIndexWork < mzSearchChunk.Count; mzIndexWork++)
                {
                    var current = mzSearchChunk[mzIndexWork];

                    double mzToleranceDa;

                    if (current.MZToleranceIsPPM)
                    {
                        mzToleranceDa = Utilities.PPMToMass(current.MZTolerance, current.SearchMZ);
                    }
                    else
                    {
                        mzToleranceDa = current.MZTolerance;
                    }

                    var ionSum = dataAggregation.AggregateIonsInRange(
                        spectrum,
                        current.SearchMZ, mzToleranceDa,
                        out _, out var closestMZ, false);

                    var dataIndex = fullSICDataCount[mzIndexWork];
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

                    fullSICDataCount[mzIndexWork]++;
                }

                if (surveyScanIndex % 100 == 0)
                {
                    var subtaskPercentComplete = ComputeMzSearchChunkProgress(
                        parentIonsProcessed, scanList.ParentIons.Count,
                        surveyScanIndex, scanList.SurveyScans.Count, mzSearchChunkProgressFraction);

                    var progressMessage = "Loading raw SIC data: " + surveyScanIndex + " / " + scanList.SurveyScans.Count;

                    UpdateProgress(subtaskPercentComplete, progressMessage);
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
            for (var mzIndexWork = 0; mzIndexWork < mzSearchChunk.Count; mzIndexWork++)
            {
                // Use this for debugging
                if (Math.Abs(mzSearchChunk[mzIndexWork].SearchMZ - DebugMZToFind) < 0.1)
                {
                    // ReSharper disable once UnusedVariable
                    var parentIonIndexPointer = mzSearchChunk[mzIndexWork].MZIndexStart;
                }

                // Copy the data for this m/z into fullSICIntensities1D()
                for (var dataIndex = 0; dataIndex < fullSICDataCount[mzIndexWork]; dataIndex++)
                {
                    fullSICIntensities1D[dataIndex] = fullSICIntensities[mzIndexWork, dataIndex];
                }

                // Compute the noise level; the noise level may change with increasing index number if the background is increasing for a given m/z

                var success = mMASICPeakFinder.ComputeDualTrimmedNoiseLevelTTest(
                    fullSICIntensities1D, 0, fullSICDataCount[mzIndexWork] - 1,
                    masicOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions,
                    out var noiseStatSegments);

                if (!success)
                {
                    SetLocalErrorCode(clsMASIC.MasicErrorCodes.FindSICPeaksError, true);
                    return false;
                }

                mzSearchChunk[mzIndexWork].BaselineNoiseStatSegments = noiseStatSegments;

                // Compute the minimum potential peak area in the entire SIC, populating udtSICPotentialAreaStatsInFullSIC
                mMASICPeakFinder.FindPotentialPeakArea(
                    fullSICDataCount[mzIndexWork],
                    fullSICIntensities1D,
                    out var potentialAreaStatsInFullSIC,
                    masicOptions.SICOptions.SICPeakFinderOptions);

                var scanIndexObservedInFullSIC = mzSearchChunk[mzIndexWork].ScanIndexMax;

                // Initialize sicDetails
                var sicDetails = new SICDetails
                {
                    SICScanType = ScanList.ScanTypeConstants.SurveyScan
                };

                // Populate sicDetails using the data centered around the highest intensity in fullSICIntensities
                // Note that this function will update sicPeak.IndexObserved
                var sicPeak = ExtractSICDetailsFromFullSIC(
                    mzIndexWork, mzSearchChunk[mzIndexWork].BaselineNoiseStatSegments,
                    fullSICDataCount[mzIndexWork], fullSICScanIndices, fullSICIntensities, fullSICMasses,
                    scanList, scanIndexObservedInFullSIC,
                    sicDetails,
                    masicOptions, scanNumScanConverter, false, 0);

                UpdateSICStatsUsingLargestPeak(
                    sicDetails,
                    sicPeak,
                    masicOptions,
                    potentialAreaStatsInFullSIC,
                    scanList,
                    mzSearchChunk,
                    mzIndexWork,
                    parentIonIndices,
                    debugParentIonIndexToFind,
                    dataOutputHandler,
                    xmlResultsWriter,
                    parentIonUpdated,
                    ref parentIonsProcessed);

                // --------------------------------------------------------
                // Now step through the parent ions and process those that were not updated using sicPeak
                // For each, search for the closest peak in SICIntensity
                // --------------------------------------------------------
                for (var parentIonIndexPointer = mzSearchChunk[mzIndexWork].MZIndexStart; parentIonIndexPointer <= mzSearchChunk[mzIndexWork].MZIndexEnd; parentIonIndexPointer++)
                {
                    if (parentIonUpdated[parentIonIndexPointer])
                        continue;

                    if (parentIonIndices[parentIonIndexPointer] == debugParentIonIndexToFind)
                    {
                        // ReSharper disable once RedundantAssignment
                        scanIndexObservedInFullSIC = -1;
                    }

                    var currentParentIon = scanList.ParentIons[parentIonIndices[parentIonIndexPointer]];

                    // Clear udtSICPotentialAreaStatsForPeak
                    currentParentIon.SICStats.SICPotentialAreaStatsForPeak = new SICPotentialAreaStats();

                    // Record the index in the Full SIC that the parent ion mass was first observed
                    // Search for .SurveyScanIndex in fullSICScanIndices
                    scanIndexObservedInFullSIC = -1;
                    for (var dataIndex = 0; dataIndex < fullSICDataCount[mzIndexWork]; dataIndex++)
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
                        ReportError("Programming error: survey scan index not found in fullSICScanIndices()", clsMASIC.MasicErrorCodes.FindSICPeaksError);
                        scanIndexObservedInFullSIC = 0;
                    }

                    // Populate sicDetails using the data centered around scanIndexObservedInFullSIC
                    // Note that this function will update sicStatsPeak.IndexObserved
                    var sicStatsPeak = ExtractSICDetailsFromFullSIC(
                        mzIndexWork, mzSearchChunk[mzIndexWork].BaselineNoiseStatSegments,
                        fullSICDataCount[mzIndexWork], fullSICScanIndices, fullSICIntensities, fullSICMasses,
                        scanList, scanIndexObservedInFullSIC,
                        sicDetails,
                        masicOptions, scanNumScanConverter,
                        currentParentIon.CustomSICPeak, currentParentIon.CustomSICPeakScanOrAcqTimeTolerance);

                    currentParentIon.SICStats.Peak = sicStatsPeak;

                    var returnClosestPeak = !currentParentIon.CustomSICPeak;

                    var peakIsValid = mMASICPeakFinder.FindSICPeakAndArea(
                        sicDetails.SICData,
                        out var potentialAreaStatsForPeakOut,
                        sicStatsPeak,
                        out var smoothedYDataSubsetInSearchChunk,
                        masicOptions.SICOptions.SICPeakFinderOptions,
                        potentialAreaStatsInFullSIC,
                        returnClosestPeak,
                        scanList.SIMDataPresent,
                        false);

                    currentParentIon.SICStats.SICPotentialAreaStatsForPeak = potentialAreaStatsForPeakOut;

                    StorePeakInParentIon(
                        scanList,
                        parentIonIndices[parentIonIndexPointer],
                        sicDetails,
                        currentParentIon.SICStats.SICPotentialAreaStatsForPeak,
                        sicStatsPeak,
                        peakIsValid);

                    // Possibly save the stats for this SIC to the SICData file
                    dataOutputHandler.SaveSICDataToText(masicOptions.SICOptions, scanList,
                        parentIonIndices[parentIonIndexPointer], sicDetails);

                    // Save the stats for this SIC to the XML file
                    xmlResultsWriter.SaveDataToXML(
                        scanList,
                        parentIonIndices[parentIonIndexPointer], sicDetails,
                        smoothedYDataSubsetInSearchChunk, dataOutputHandler);

                    parentIonUpdated[parentIonIndexPointer] = true;
                    parentIonsProcessed++;
                }

                // ---------------------------------------------------------
                // Update progress
                // ---------------------------------------------------------
                try
                {
                    var subtaskPercentComplete = ComputeMzSearchChunkProgress(
                        parentIonsProcessed, scanList.ParentIons.Count,
                        scanList.SurveyScans.Count, scanList.SurveyScans.Count, mzSearchChunkProgressFraction);

                    UpdateProgress(subtaskPercentComplete);

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
                            ReportMessage("Parent Ions Processed: " + parentIonsProcessed);
                            Console.Write(".");
                            masicOptions.LastParentIonProcessingLogTime = DateTime.UtcNow;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ReportError("Error updating progress", ex, clsMASIC.MasicErrorCodes.CreateSICsError);
                }
            }

            return true;
        }

        private void UpdateSICStatsUsingLargestPeak(
            SICDetails sicDetails,
            SICStatsPeak sicPeak,
            MASICOptions masicOptions,
            SICPotentialAreaStats potentialAreaStatsInFullSIC,
            ScanList scanList,
            IReadOnlyList<MzSearchInfo> mzSearchChunk,
            int mzIndexWork,
            IList<int> parentIonIndices,
            int debugParentIonIndexToFind,
            DataOutput.DataOutput dataOutputHandler,
            XMLResultsWriter xmlResultsWriter,
            IList<bool> parentIonUpdated,
            ref int parentIonsProcessed)
        {
            // Find the largest peak in the SIC for this m/z
            var largestPeakFound = mMASICPeakFinder.FindSICPeakAndArea(
                sicDetails.SICData,
                out var potentialAreaStatsForPeak, sicPeak,
                out var smoothedYDataSubset, masicOptions.SICOptions.SICPeakFinderOptions,
                potentialAreaStatsInFullSIC,
                true, scanList.SIMDataPresent, false);

            if (!largestPeakFound)
            {
                return;
            }

            // --------------------------------------------------------
            // Step through the parent ions and see if .SurveyScanIndex is contained in sicPeak
            // If it is, assign the stats of the largest peak to the given parent ion
            // --------------------------------------------------------

            var mzIndexSICIndices = sicDetails.SICScanIndices;

            for (var parentIonIndexPointer = mzSearchChunk[mzIndexWork].MZIndexStart; parentIonIndexPointer <= mzSearchChunk[mzIndexWork].MZIndexEnd; parentIonIndexPointer++)
            {
                var storePeak = false;

                // Use this for debugging
                if (parentIonIndices[parentIonIndexPointer] == debugParentIonIndexToFind)
                {
                    storePeak = false;
                }

                if (scanList.ParentIons[parentIonIndices[parentIonIndexPointer]].CustomSICPeak)
                    continue;

                // Assign the stats of the largest peak to each parent ion with .SurveyScanIndex contained in the peak
                var currentParentIon = scanList.ParentIons[parentIonIndices[parentIonIndexPointer]];
                if (currentParentIon.SurveyScanIndex >= mzIndexSICIndices[sicPeak.IndexBaseLeft] &&
                    currentParentIon.SurveyScanIndex <= mzIndexSICIndices[sicPeak.IndexBaseRight])
                {
                    storePeak = true;
                }

                if (!storePeak)
                    continue;

                StorePeakInParentIon(
                    scanList, parentIonIndices[parentIonIndexPointer],
                    sicDetails, potentialAreaStatsForPeak, sicPeak, true);

                // Possibly save the stats for this SIC to the SICData file
                dataOutputHandler.SaveSICDataToText(
                    masicOptions.SICOptions, scanList,
                    parentIonIndices[parentIonIndexPointer], sicDetails);

                // Save the stats for this SIC to the XML file
                xmlResultsWriter.SaveDataToXML(
                    scanList,
                    parentIonIndices[parentIonIndexPointer], sicDetails,
                    smoothedYDataSubset, dataOutputHandler);

                parentIonUpdated[parentIonIndexPointer] = true;
                parentIonsProcessed++;
            }
        }

        /// <summary>
        /// Store a selected ion chromatogram peak in a parent ion
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="parentIonIndex"></param>
        /// <param name="sicDetails"></param>
        /// <param name="potentialAreaStatsForPeak"></param>
        /// <param name="sicPeak"></param>
        /// <param name="peakIsValid"></param>
        public bool StorePeakInParentIon(
            ScanList scanList,
            int parentIonIndex,
            SICDetails sicDetails,
            SICPotentialAreaStats potentialAreaStatsForPeak,
            SICStatsPeak sicPeak,
            bool peakIsValid)
        {
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

                var scanIndexObserved = currentParentIon.SurveyScanIndex;
                if (scanIndexObserved < 0)
                    scanIndexObserved = 0;

                var processingMRMPeak = currentParentIon.MRMDaughterMZ > 0;

                var sicStats = currentParentIon.SICStats;

                sicStats.SICPotentialAreaStatsForPeak = potentialAreaStatsForPeak;

                // Clone sicPeak since it will be updated to include the ParentIonIntensity for this parent ion's fragmentation scan
                sicStats.Peak = sicPeak.Clone();

                sicStats.ScanTypeForPeakIndices = sicDetails.SICScanType;
                if (processingMRMPeak)
                {
                    if (sicStats.ScanTypeForPeakIndices != ScanList.ScanTypeConstants.FragScan)
                    {
                        // ScanType is not FragScan; this is unexpected
                        ReportError("Programming error: sicStats.SICScanType is not FragScan even though we're processing an MRM peak", clsMASIC.MasicErrorCodes.FindSICPeaksError);
                        sicStats.ScanTypeForPeakIndices = ScanList.ScanTypeConstants.FragScan;
                    }
                }

                if (processingMRMPeak)
                {
                    sicStats.Peak.IndexObserved = 0;
                }
                else
                {
                    // Record the index (of data in .SICData) that the parent ion mass was first observed
                    // This is not necessarily the same as sicPeak.IndexObserved, so we need to search for it here

                    // Search for scanIndexObserved in sicScanIndices()
                    sicStats.Peak.IndexObserved = -1;
                    for (var dataIndex = 0; dataIndex < sicDetails.SICDataCount; dataIndex++)
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
                        ReportError("Programming error: survey scan index not found in sicScanIndices", clsMASIC.MasicErrorCodes.FindSICPeaksError);
                        sicStats.Peak.IndexObserved = 0;
                    }
                }

                int fragScanNumber;
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
                    mMASICPeakFinder.ComputeParentIonIntensity(
                        sicData,
                        sicStats.Peak,
                        fragScanNumber);
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

                    sicStatsPeak.SignalToNoiseRatio = clsMASICPeakFinder.ComputeSignalToNoise(
                        sicStatsPeak.MaxIntensityValue,
                        sicStatsPeak.BaselineNoiseStats.NoiseLevel);
                }

                // Update .OptimalPeakApexScanNumber
                // Note that a valid peak will typically have .IndexBaseLeft or .IndexBaseRight different from .IndexMax
                var scanIndexPointer = sicData[currentParentIon.SICStats.Peak.IndexMax].ScanIndex;
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
                ReportError("Error finding SIC peaks and their areas", ex, clsMASIC.MasicErrorCodes.FindSICPeaksError);
                return false;
            }
        }
    }
}
