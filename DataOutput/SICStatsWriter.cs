﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MASIC.Data;
using MASIC.Options;
using MASICPeakFinder;
using PRISM;

namespace MASIC.DataOutput
{
    /// <summary>
    /// SIC Stats Writer
    /// </summary>
    public class SICStatsWriter : MasicEventNotifier
    {
        // Ignore Spelling: Frag, MASIC

        /// <summary>
        /// SIC stats file path
        /// </summary>
        public string SICStatsFilePath { get; private set; } = string.Empty;

        private ParentIonInfo GetFakeParentIonForFragScan(ScanList scanList, int fragScanIndex)
        {
            var currentFragScan = scanList.FragScans[fragScanIndex];

            var newParentIon = new ParentIonInfo(currentFragScan.BasePeakIonMZ)
            {
                SurveyScanIndex = 0
            };

            // Find the previous MS1 scan that occurs before the frag scan
            var surveyScanNumberAbsolute = currentFragScan.ScanNumber - 1;

            newParentIon.FragScanIndices.Add(fragScanIndex);

            if (scanList.MasterScanOrderCount > 0)
            {
                var surveyScanIndexMatch = BinarySearch.BinarySearchFindNearest(
                    scanList.MasterScanNumList,
                    surveyScanNumberAbsolute,
                    BinarySearch.MissingDataModeConstants.ReturnClosestPoint);

                while (surveyScanIndexMatch >= 0 && scanList.MasterScanOrder[surveyScanIndexMatch].ScanType == ScanList.ScanTypeConstants.FragScan)
                {
                    surveyScanIndexMatch--;
                }

                if (surveyScanIndexMatch < 0)
                {
                    // Did not find the previous survey scan; find the next survey scan
                    surveyScanIndexMatch++;

                    while (surveyScanIndexMatch < scanList.MasterScanOrderCount && scanList.MasterScanOrder[surveyScanIndexMatch].ScanType == ScanList.ScanTypeConstants.FragScan)
                    {
                        surveyScanIndexMatch++;
                    }

                    if (surveyScanIndexMatch >= scanList.MasterScanOrderCount)
                    {
                        surveyScanIndexMatch = 0;
                    }
                }

                newParentIon.SurveyScanIndex = scanList.MasterScanOrder[surveyScanIndexMatch].ScanIndexPointer;
            }

            if (newParentIon.SurveyScanIndex < scanList.SurveyScans.Count)
            {
                newParentIon.OptimalPeakApexScanNumber = scanList.SurveyScans[newParentIon.SurveyScanIndex].ScanNumber;
            }
            else
            {
                newParentIon.OptimalPeakApexScanNumber = surveyScanNumberAbsolute;
            }

            newParentIon.PeakApexOverrideParentIonIndex = -1;

            newParentIon.SICStats.ScanTypeForPeakIndices = ScanList.ScanTypeConstants.FragScan;
            newParentIon.SICStats.PeakScanIndexStart = fragScanIndex;
            newParentIon.SICStats.PeakScanIndexEnd = fragScanIndex;
            newParentIon.SICStats.PeakScanIndexMax = fragScanIndex;

            var peak = newParentIon.SICStats.Peak;
            peak.MaxIntensityValue = currentFragScan.BasePeakIonIntensity;
            peak.SignalToNoiseRatio = 1;
            peak.FWHMScanWidth = 1;
            peak.Area = currentFragScan.BasePeakIonIntensity;
            peak.ParentIonIntensity = currentFragScan.BasePeakIonIntensity;

            return newParentIon;
        }

        // ReSharper disable once UnusedMember.Local

        [Obsolete("Unused")]
        private void PopulateScanListPointerArray(
            IList<ScanInfo> surveyScans,
            int surveyScanCount,
            out int[] scanListArray)
        {
            if (surveyScanCount > 0)
            {
                scanListArray = new int[surveyScanCount];

                for (var index = 0; index < surveyScanCount; index++)
                {
                    scanListArray[index] = surveyScans[index].ScanNumber;
                }
            }
            else
            {
                scanListArray = new int[1];
            }
        }

        /// <summary>
        /// Writes out a flat file containing identified peaks and statistics
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="inputFileName"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <param name="masicOptions"></param>
        /// <param name="dataOutputHandler"></param>
        public bool SaveSICStatsFlatFile(
            ScanList scanList,
            string inputFileName,
            string outputDirectoryPath,
            MASICOptions masicOptions,
            DataOutput dataOutputHandler)
        {
            SICStatsFilePath = string.Empty;

            const char TAB_DELIMITER = '\t';

            try
            {
                UpdateProgress(0, "Saving SIC data to flat file");

                SICStatsFilePath = DataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, DataOutput.OutputFileTypeConstants.SICStatsFlatFile);

                if (string.IsNullOrWhiteSpace(SICStatsFilePath))
                {
                    throw new Exception("ConstructOutputFilePath returned an empty file path for the SIC stats flat file");
                }

                ReportMessage("Saving SIC flat file to disk: " + Path.GetFileName(SICStatsFilePath));

                using var writer = new StreamWriter(SICStatsFilePath, false);

                // Write the SIC stats to the output file
                // The file is tab delimited

                if (masicOptions.IncludeHeadersInExportFile)
                {
                    writer.WriteLine(dataOutputHandler.GetHeadersForOutputFile(scanList, DataOutput.OutputFileTypeConstants.SICStatsFlatFile, TAB_DELIMITER));
                }

                if (scanList.SurveyScans.Count == 0 && scanList.ParentIons.Count == 0)
                {
                    // Write out fake values to the _SICStats.txt file so that downstream software can still access some of the information
                    for (var fragScanIndex = 0; fragScanIndex < scanList.FragScans.Count; fragScanIndex++)
                    {
                        var fakeParentIon = GetFakeParentIonForFragScan(scanList, fragScanIndex);
                        const int parentIonIndex = 0;

                        const int surveyScanNumber = 0;
                        const float surveyScanTime = 0;

                        WriteSICStatsFlatFileEntry(writer, TAB_DELIMITER, masicOptions.SICOptions, scanList,
                            fakeParentIon, parentIonIndex, surveyScanNumber, surveyScanTime,
                            0, masicOptions);
                    }
                }
                else
                {
                    for (var parentIonIndex = 0; parentIonIndex < scanList.ParentIons.Count; parentIonIndex++)
                    {
                        bool includeParentIon;

                        if (masicOptions.CustomSICList.LimitSearchToCustomMZList)
                        {
                            includeParentIon = scanList.ParentIons[parentIonIndex].CustomSICPeak;
                        }
                        else
                        {
                            includeParentIon = true;
                        }

                        if (includeParentIon)
                        {
                            for (var fragScanIndex = 0; fragScanIndex < scanList.ParentIons[parentIonIndex].FragScanIndices.Count; fragScanIndex++)
                            {
                                var parentIon = scanList.ParentIons[parentIonIndex];
                                int surveyScanNumber;
                                float surveyScanTime;

                                if (parentIon.SurveyScanIndex >= 0 && parentIon.SurveyScanIndex < scanList.SurveyScans.Count)
                                {
                                    surveyScanNumber = scanList.SurveyScans[parentIon.SurveyScanIndex].ScanNumber;
                                    surveyScanTime = scanList.SurveyScans[parentIon.SurveyScanIndex].ScanTime;
                                }
                                else
                                {
                                    surveyScanNumber = -1;
                                    surveyScanTime = 0;
                                }

                                WriteSICStatsFlatFileEntry(writer, TAB_DELIMITER, masicOptions.SICOptions, scanList,
                                    parentIon, parentIonIndex, surveyScanNumber, surveyScanTime,
                                    fragScanIndex, masicOptions);
                            }
                        }

                        if (scanList.ParentIons.Count > 1)
                        {
                            if (parentIonIndex % 100 == 0)
                            {
                                UpdateProgress((short)(parentIonIndex / (double)(scanList.ParentIons.Count - 1) * 100));
                            }
                        }
                        else
                        {
                            UpdateProgress(1);
                        }

                        if (masicOptions.AbortProcessing)
                        {
                            scanList.ProcessingIncomplete = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                ReportError("Error writing the Peak Stats to: " + SICStatsFilePath, ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }

        private float ScanNumberToScanTime(
            ScanList scanList,
            int scanNumber)
        {
            var surveyScanMatches = (from item in scanList.SurveyScans where item.ScanNumber == scanNumber select item).ToList();

            if (surveyScanMatches.Count > 0)
            {
                return surveyScanMatches.First().ScanTime;
            }

            var fragScanMatches = (from item in scanList.FragScans where item.ScanNumber == scanNumber select item).ToList();

            if (fragScanMatches.Count > 0)
            {
                return fragScanMatches.First().ScanTime;
            }

            return 0;
        }

        private void WriteSICStatsFlatFileEntry(
            TextWriter sicStatsWriter,
            char delimiter,
            SICOptions sicOptions,
            ScanList scanList,
            ParentIonInfo parentIon,
            int parentIonIndex,
            int surveyScanNumber,
            float surveyScanTime,
            int fragScanIndex,
            MASICOptions masicOptions)
        {
            var dataValues = new List<string>(40);

            float fragScanTime = 0;
            float optimalPeakApexScanTime = 0;

            dataValues.Add(sicOptions.DatasetID.ToString());                 // Dataset ID
            dataValues.Add(parentIonIndex.ToString());                       // Parent Ion Index

            dataValues.Add(StringUtilities.DblToString(parentIon.MZ, 4));    // MZ

            dataValues.Add(surveyScanNumber.ToString());                     // Survey scan number

            double interferenceScore;

            if (fragScanIndex < scanList.FragScans.Count)
            {
                var fragScanNumber = scanList.FragScans[parentIon.FragScanIndices[fragScanIndex]].ScanNumber;
                dataValues.Add(fragScanNumber.ToString());  // Fragmentation scan number
                interferenceScore = scanList.FragScans[parentIon.FragScanIndices[fragScanIndex]].FragScanInfo.InterferenceScore;
            }
            else
            {
                dataValues.Add("0");    // Fragmentation scan does not exist
                interferenceScore = 0;
            }

            dataValues.Add(parentIon.OptimalPeakApexScanNumber.ToString());                // Optimal peak apex scan number

            if (masicOptions.IncludeScanTimesInSICStatsFile)
            {
                if (fragScanIndex < scanList.FragScans.Count)
                {
                    fragScanTime = scanList.FragScans[parentIon.FragScanIndices[fragScanIndex]].ScanTime;
                }
                else
                {
                    fragScanTime = 0;               // Fragmentation scan does not exist
                }

                optimalPeakApexScanTime = ScanNumberToScanTime(scanList, parentIon.OptimalPeakApexScanNumber);
            }

            dataValues.Add(parentIon.PeakApexOverrideParentIonIndex.ToString());           // Parent Ion Index that supplied the optimal peak apex scan number
            if (parentIon.CustomSICPeak)
            {
                dataValues.Add("1");   // Custom SIC peak, record 1
            }
            else
            {
                dataValues.Add("0");   // Not a Custom SIC peak, record 0
            }

            var currentSIC = parentIon.SICStats;

            if (currentSIC.ScanTypeForPeakIndices == ScanList.ScanTypeConstants.FragScan)
            {
                dataValues.Add(scanList.FragScans[currentSIC.PeakScanIndexStart].ScanNumber.ToString());    // Peak Scan Start
                dataValues.Add(scanList.FragScans[currentSIC.PeakScanIndexEnd].ScanNumber.ToString());      // Peak Scan End
                dataValues.Add(scanList.FragScans[currentSIC.PeakScanIndexMax].ScanNumber.ToString());      // Peak Scan Max Intensity
            }
            else
            {
                dataValues.Add(scanList.SurveyScans[currentSIC.PeakScanIndexStart].ScanNumber.ToString());  // Peak Scan Start
                dataValues.Add(scanList.SurveyScans[currentSIC.PeakScanIndexEnd].ScanNumber.ToString());    // Peak Scan End
                dataValues.Add(scanList.SurveyScans[currentSIC.PeakScanIndexMax].ScanNumber.ToString());    // Peak Scan Max Intensity
            }

            var currentPeak = currentSIC.Peak;
            dataValues.Add(StringUtilities.ValueToString(currentPeak.MaxIntensityValue, 5));          // Peak Intensity
            dataValues.Add(StringUtilities.ValueToString(currentPeak.SignalToNoiseRatio, 4));         // Peak signal-to-noise ratio
            dataValues.Add(currentPeak.FWHMScanWidth.ToString());                                                    // Full width at half max (in scans)
            dataValues.Add(StringUtilities.ValueToString(currentPeak.Area, 5));                       // Peak area

            dataValues.Add(StringUtilities.ValueToString(currentPeak.ParentIonIntensity, 5));         // Intensity of the parent ion (just before the fragmentation scan)
            dataValues.Add(StringUtilities.ValueToString(currentPeak.BaselineNoiseStats.NoiseLevel, 5));
            dataValues.Add(StringUtilities.ValueToString(currentPeak.BaselineNoiseStats.NoiseStDev, 3));
            dataValues.Add(currentPeak.BaselineNoiseStats.PointsUsed.ToString());

            var statMoments = currentPeak.StatisticalMoments;

            dataValues.Add(StringUtilities.ValueToString(statMoments.Area, 5));
            dataValues.Add(statMoments.CenterOfMassScan.ToString());
            dataValues.Add(StringUtilities.ValueToString(statMoments.StDev, 3));
            dataValues.Add(StringUtilities.ValueToString(statMoments.Skew, 4));
            dataValues.Add(StringUtilities.ValueToString(statMoments.KSStat, 4));
            dataValues.Add(statMoments.DataCountUsed.ToString());

            dataValues.Add(StringUtilities.ValueToString(interferenceScore, 4));     // Interference Score

            if (masicOptions.IncludeScanTimesInSICStatsFile)
            {
                dataValues.Add(StringUtilities.DblToString(surveyScanTime, 5));            // SurveyScanTime
                dataValues.Add(StringUtilities.DblToString(fragScanTime, 5));              // FragScanTime
                dataValues.Add(StringUtilities.DblToString(optimalPeakApexScanTime, 5));   // OptimalPeakApexScanTime
            }

            if (masicOptions.IncludeCustomSICCommentsInSICStatsFile)
            {
                dataValues.Add(parentIon.CustomSICPeakComment);
            }

            sicStatsWriter.WriteLine(string.Join(delimiter.ToString(), dataValues));
        }
    }
}
