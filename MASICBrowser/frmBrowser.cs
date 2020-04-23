// This program will read the _SICs.xml data file created by MASIC to allow
// for browsing of the spectra
//
// -------------------------------------------------------------------------------
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
// Program started October 17, 2003
// Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
// Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
// -------------------------------------------------------------------------------
//
// Licensed under the 2-Clause BSD License; you may Not use this file except
// in compliance with the License.  You may obtain a copy of the License at
// https://opensource.org/licenses/BSD-2-Clause

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
using MASICPeakFinder;
using Microsoft.VisualBasic;
using OxyDataPlotter;
using OxyPlot;
using PRISM;
using PRISMDatabaseUtils;
using ProgressFormNET;

namespace MASICBrowser
{
    public partial class frmBrowser : Form
    {
        private const string PROGRAM_DATE = "February 19, 2020";

        public frmBrowser()
        {
            Application.EnableVisualStyles();
            Application.DoEvents();

            InitializeComponent();

            InitializeControls();
        }

        #region "Constants and Enums"

        private const string REG_APP_NAME = "MASICBrowser";
        private const string REG_SECTION_NAME = "Options";

        private const string TABLE_NAME_MSMS_RESULTS = "T_MsMsResults";
        private const string TABLE_NAME_SEQUENCES = "T_Sequences";
        private const string TABLE_NAME_SEQ_TO_PROTEIN_MAP = "T_Seq_to_Protein_Map";

        private const string COL_NAME_SCAN = "Scan";
        private const string COL_NAME_CHARGE = "Charge";
        private const string COL_NAME_MH = "MH";
        private const string COL_NAME_XCORR = "XCorr";
        private const string COL_NAME_DELTACN = "DeltaCN";
        private const string COL_NAME_DELTACN2 = "DeltaCn2";
        private const string COL_NAME_RANK_SP = "RankSp";
        private const string COL_NAME_RANK_XC = "RankXc";
        private const string COL_NAME_SEQUENCE_ID = "SeqID";
        private const string COL_NAME_PARENT_ION_INDEX = "ParentIonIndex";

        private const string COL_NAME_SEQUENCE = "Sequence";
        private const string COL_NAME_PROTEIN = "Protein";

        // Private Const COL_NAME_MULTI_PROTEIN_COUNT As String = "MultiProteinCount"

        private const int SORT_ORDER_MODE_COUNT = 18;

        private enum eSortOrderConstants
        {
            SortByPeakIndex = 0,
            SortByScanPeakCenter = 1,
            SortByScanOptimalPeakCenter = 2,
            SortByMz = 3,
            SortByPeakSignalToNoise = 4,
            SortByBaselineCorrectedPeakIntensity = 5,
            SortByBaselineCorrectedPeakArea = 6,
            SortByPeakWidth = 7,
            SortBySICIntensityMax = 8,
            SortByPeakIntensity = 9,
            SortByPeakArea = 10,
            SortByFragScanToOptimalLocDistance = 11,
            SortByPeakCenterToOptimalLocDistance = 12,
            SortByShoulderCount = 13,
            SortByParentIonIntensity = 14,
            SortByPeakSkew = 15,
            SortByKSStat = 16,
            SortByBaselineNoiseLevel = 17
        }

        private enum eSICTypeFilterConstants
        {
            AllSICs = 0,
            NoCustomSICs = 1,
            CustomSICsOnly = 2
        }

        private enum eSmoothModeConstants
        {
            DoNotReSmooth = 0,
            Butterworth = 1,
            SavitzkyGolay = 2
        }

        private enum eMsMsSearchEngineResultColumns
        {
            RowIndex = 0,
            Scan,
            NumberOfScans,
            Charge,
            MH,
            XCorr,
            DeltaCN,
            SP,
            Protein,
            MultiProteinCount,
            Sequence,
            DeltaCn2,
            RankSp,
            RankXc,
            DelM,
            XcRatio,
            PassFilt,
            MScore,
            NTT
        }

        private enum eCurrentXMLDataFileSectionConstants : int
        {
            UnknownFile = 0,
            Start = 1,
            Options = 2,
            ParentIons = 3
        }

        #endregion

        #region "Properties"
        public string FileToAutoLoad { get; set; }
        #endregion

        #region "Classwide Variables"

        private Spectrum mSpectrum;

        private List<clsParentIonStats> mParentIonStats;

        private DataSet mMsMsResults;

        private clsSICPeakFinderOptions mSICPeakFinderOptions;

        private int mParentIonPointerArrayCount;          // Could be less than mParentIonStats.Count if filtering the data
        private int[] mParentIonPointerArray = new int[0];  // Pointer array used for de-referencing cboParentIon.SelectedItem to mParentIonStats

        private bool mAutoStepEnabled;
        private int mAutoStepIntervalMsec;

        private DateTime mLastUpdate;
        private clsMASICPeakFinder mMASICPeakFinder;

        private DateTime mLastErrorNotification;

        private System.Windows.Forms.Timer mFileLoadTimer;

        #endregion

        private void AutoOpenMsMsResults(string masicFilePath, frmProgress objProgress)
        {
            // Look for a corresponding Synopsis or First hits file in the same folder as masicFilePath

            var dataDirectoryInfo = new DirectoryInfo(Path.GetDirectoryName(masicFilePath));

            var fileNameBase = Path.GetFileNameWithoutExtension(masicFilePath);
            if (fileNameBase.ToLower().EndsWith("_sics"))
            {
                fileNameBase = fileNameBase.Substring(0, fileNameBase.Length - 5);
            }

            var fileNameToFind = fileNameBase + "_fht.txt";
            fileNameToFind = Path.Combine(dataDirectoryInfo.FullName, fileNameToFind);

            txtStats3.Visible = false;
            if (File.Exists(fileNameToFind))
            {
                ReadMsMsSearchEngineResults(fileNameToFind, objProgress);
            }
            else
            {
                fileNameToFind = fileNameBase + "_syn.txt";
                fileNameToFind = Path.Combine(dataDirectoryInfo.FullName, fileNameToFind);

                if (File.Exists(fileNameToFind))
                {
                    ReadMsMsSearchEngineResults(fileNameToFind, objProgress);
                    txtStats3.Visible = true;
                }
            }

            PositionControls();
        }

        private void CheckAutoStep()
        {
            if (DateTime.Now.Subtract(mLastUpdate).TotalMilliseconds >= mAutoStepIntervalMsec)
            {
                mLastUpdate = mLastUpdate.AddMilliseconds(mAutoStepIntervalMsec);
                NavigateScanList(chkAutoStepForward.Checked);
                Application.DoEvents();
            }
        }

        private void ClearMsMsResults()
        {
            mMsMsResults.Tables[TABLE_NAME_MSMS_RESULTS].Clear();
            mMsMsResults.Tables[TABLE_NAME_SEQ_TO_PROTEIN_MAP].Clear();
            InitializeSequencesDataTable();
        }

        private void DefineDefaultSortDirection()
        {
            try
            {
                var eSortOrder = (eSortOrderConstants)cboSortOrder.SelectedIndex;

                switch (eSortOrder)
                {
                    case eSortOrderConstants.SortByMz:
                    case eSortOrderConstants.SortByPeakIndex:
                    case eSortOrderConstants.SortByScanOptimalPeakCenter:
                    case eSortOrderConstants.SortByScanPeakCenter:
                    case eSortOrderConstants.SortByPeakSkew:
                    case eSortOrderConstants.SortByKSStat:
                        chkSortDescending.Checked = false;
                        break;
                    default:
                        // Sort the others descending by default
                        chkSortDescending.Checked = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                // Ignore any errors
            }
        }

        private void DisplaySICStats(int parentIonIndex, out clsSICStats sicStats)
        {
            // udtSICStats will be populated with either the original SIC stats found by MASIC or with the
            // updated SIC stats if chkUsePeakFinder is Checked
            // Also, if re-smooth data is enabled, then the SIC data will be re-smoothed

            UpdateSICPeakFinderOptions();

            if (parentIonIndex >= 0 && parentIonIndex < mParentIonStats.Count)
            {
                eSmoothModeConstants eSmoothMode;
                if (optUseButterworthSmooth.Checked)
                {
                    eSmoothMode = eSmoothModeConstants.Butterworth;
                }
                else if (optUseSavitzkyGolaySmooth.Checked)
                {
                    eSmoothMode = eSmoothModeConstants.SavitzkyGolay;
                }
                else
                {
                    eSmoothMode = eSmoothModeConstants.DoNotReSmooth;
                }

                var validPeakFound = UpdateSICStats(parentIonIndex, chkUsePeakFinder.Checked, eSmoothMode, out sicStats);

                // Display the SIC and SIC Peak stats
                var ionStats = mParentIonStats[parentIonIndex];
                var statDetails = string.Empty;
                statDetails += "Index: " + ionStats.Index;
                if (ionStats.OptimalPeakApexScanNumber == ionStats.SICStats.ScanNumberMaxIntensity)
                {
                    statDetails += Environment.NewLine + "Scan at apex: " + ionStats.SICStats.ScanNumberMaxIntensity;
                }
                else
                {
                    statDetails += Environment.NewLine + "Scan at apex: " + ionStats.OptimalPeakApexScanNumber + " (Original apex: " + ionStats.SICStats.ScanNumberMaxIntensity + ")";
                }

                if (validPeakFound)
                {
                    statDetails += Environment.NewLine + "Center of mass: " + sicStats.Peak.StatisticalMoments.CenterOfMassScan;
                    double areaToDisplay;
                    double intensityToDisplay;
                    if (chkShowBaselineCorrectedStats.Checked)
                    {
                        intensityToDisplay = clsMASICPeakFinder.BaselineAdjustIntensity(sicStats.Peak, false);
                        areaToDisplay = clsMASICPeakFinder.BaselineAdjustArea(sicStats.Peak, mParentIonStats[parentIonIndex].SICStats.SICPeakWidthFullScans, false);
                    }
                    else
                    {
                        intensityToDisplay = sicStats.Peak.MaxIntensityValue;
                        areaToDisplay = sicStats.Peak.Area;
                    }

                    statDetails += Environment.NewLine + "Intensity: " + StringUtilities.ValueToString(intensityToDisplay, 4);
                    statDetails += Environment.NewLine + "Area: " + StringUtilities.ValueToString(areaToDisplay, 4);
                    statDetails += Environment.NewLine + "FWHM: " + sicStats.Peak.FWHMScanWidth;
                }
                else
                {
                    statDetails += Environment.NewLine + "Could not find a valid SIC peak";
                }

                if (mParentIonStats[parentIonIndex].CustomSICPeak)
                {
                    statDetails += Environment.NewLine + "Custom SIC: " + mParentIonStats[parentIonIndex].CustomSICPeakComment;
                }

                txtStats1.Text = statDetails;

                statDetails = "m/z: " + mParentIonStats[parentIonIndex].MZ;
                if (validPeakFound)
                {
                    var statMoments = sicStats.Peak.StatisticalMoments;
                    statDetails += Environment.NewLine + "Peak StDev: " + StringUtilities.ValueToString(statMoments.StDev, 3);
                    statDetails += Environment.NewLine + "Peak Skew: " + StringUtilities.ValueToString(statMoments.Skew, 4);
                    statDetails += Environment.NewLine + "Peak KSStat: " + StringUtilities.ValueToString(statMoments.KSStat, 4);
                    statDetails += Environment.NewLine + "Data Count Used: " + statMoments.DataCountUsed;

                    if (sicStats.Peak.SignalToNoiseRatio >= 3)
                    {
                        statDetails += Environment.NewLine + "S/N: " + Math.Round(sicStats.Peak.SignalToNoiseRatio, 0);
                    }
                    else
                    {
                        statDetails += Environment.NewLine + "S/N: " + StringUtilities.ValueToString(sicStats.Peak.SignalToNoiseRatio, 4);
                    }

                    var noiseStats = sicStats.Peak.BaselineNoiseStats;
                    statDetails += Environment.NewLine + "Noise level: " + StringUtilities.ValueToString(noiseStats.NoiseLevel, 4);
                    statDetails += Environment.NewLine + "Noise StDev: " + StringUtilities.ValueToString(noiseStats.NoiseStDev, 3);
                    statDetails += Environment.NewLine + "Points used: " + noiseStats.PointsUsed;
                    statDetails += Environment.NewLine + "Noise Mode Used: " + noiseStats.NoiseThresholdModeUsed;
                }

                txtStats2.Text = statDetails;

                txtStats3.Text = LookupSequenceForParentIonIndex(parentIonIndex);
            }
            else
            {
                txtStats1.Text = "Invalid parent ion index: " + parentIonIndex;
                txtStats2.Text = string.Empty;
                txtStats3.Text = string.Empty;
                sicStats = new clsSICStats();
            }
        }

        private void DisplaySICStatsForSelectedParentIon()
        {
            clsSICStats sicStats = null;

            if (mParentIonPointerArrayCount > 0)
            {
                DisplaySICStats(mParentIonPointerArray[lstParentIonData.SelectedIndex], out sicStats);
            }
        }

        private void EnableDisableControls()
        {
            var useButterworth = false;
            var useSavitzkyGolay = false;

            if (optDoNotResmooth.Checked)
            {
                useButterworth = false;
                useSavitzkyGolay = false;
            }
            else if (optUseButterworthSmooth.Checked)
            {
                useButterworth = true;
                useSavitzkyGolay = false;
            }
            else if (optUseSavitzkyGolaySmooth.Checked)
            {
                useButterworth = false;
                useSavitzkyGolay = true;
            }

            txtButterworthSamplingFrequency.Enabled = useButterworth;
            txtSavitzkyGolayFilterOrder.Enabled = useSavitzkyGolay;
        }

        private void FindMinimumPotentialPeakAreaInRegion(int parentIonIndexStart, int parentIonIndexEnd, clsSICPotentialAreaStats potentialAreaStatsForRegion)
        {
            // This function finds the minimum potential peak area in the parent ions between
            // parentIonIndexStart and parentIonIndexEnd
            // However, the summed intensity is not used if the number of points >= .SICNoiseThresholdIntensity is less than Minimum_Peak_Width

            potentialAreaStatsForRegion.MinimumPotentialPeakArea = double.MaxValue;
            potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = 0;
            for (var parentIonIndex = parentIonIndexStart; parentIonIndex <= parentIonIndexEnd; parentIonIndex++)
            {
                var ionStats = mParentIonStats[parentIonIndex];
                if (Math.Abs(ionStats.SICStats.SICPotentialAreaStatsForPeak.MinimumPotentialPeakArea) < float.Epsilon && ionStats.SICStats.SICPotentialAreaStatsForPeak.PeakCountBasisForMinimumPotentialArea == 0)
                {
                    // Need to compute the minimum potential peak area for parentIonIndex

                    // Compute the potential peak area for this SIC
                    mMASICPeakFinder.FindPotentialPeakArea(ionStats.SICData, out var potentialAreaStats, mSICPeakFinderOptions);
                    ionStats.SICStats.SICPotentialAreaStatsForPeak = potentialAreaStats;
                }

                var sicPotentialStats = ionStats.SICStats.SICPotentialAreaStatsForPeak;
                if (sicPotentialStats.MinimumPotentialPeakArea > 0 && sicPotentialStats.PeakCountBasisForMinimumPotentialArea >= clsMASICPeakFinder.MINIMUM_PEAK_WIDTH)
                {
                    if (sicPotentialStats.PeakCountBasisForMinimumPotentialArea > potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea)
                    {
                        // The non valid peak count value is larger than the one associated with the current
                        // minimum potential peak area; update the minimum peak area to potentialPeakArea
                        potentialAreaStatsForRegion.MinimumPotentialPeakArea = sicPotentialStats.MinimumPotentialPeakArea;
                        potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = sicPotentialStats.PeakCountBasisForMinimumPotentialArea;
                    }
                    else if (sicPotentialStats.MinimumPotentialPeakArea < potentialAreaStatsForRegion.MinimumPotentialPeakArea && sicPotentialStats.PeakCountBasisForMinimumPotentialArea >= potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea)
                    {
                        potentialAreaStatsForRegion.MinimumPotentialPeakArea = sicPotentialStats.MinimumPotentialPeakArea;
                        potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = sicPotentialStats.PeakCountBasisForMinimumPotentialArea;
                    }
                }
            }

            if (potentialAreaStatsForRegion.MinimumPotentialPeakArea > double.MaxValue - 1)
            {
                potentialAreaStatsForRegion.MinimumPotentialPeakArea = 1;
            }
        }

        private bool FindSICPeakAndAreaForParentIon(int parentIonIndex, clsSICStats sicStats)
        {
            var sicPotentialAreaStatsForRegion = new clsSICPotentialAreaStats();

            try
            {
                // Determine the minimum potential peak area in the last 500 scans
                var parentIonIndexStart = parentIonIndex - 500;
                if (parentIonIndexStart < 0)
                    parentIonIndexStart = 0;
                FindMinimumPotentialPeakAreaInRegion(parentIonIndexStart, parentIonIndex, sicPotentialAreaStatsForRegion);

                var parentIon = mParentIonStats[parentIonIndex];

                var returnClosestPeak = !parentIon.CustomSICPeak;

                // Look for .SurveyScanNumber in .SICScans in order to populate sicStats.Peak.IndexObserved
                if (sicStats.Peak.IndexObserved == 0)
                {
                    for (var index = 0; index <= parentIon.SICData.Count - 1; index++)
                    {
                        if (parentIon.SICData[index].ScanNumber == parentIon.SurveyScanNumber)
                        {
                            sicStats.Peak.IndexObserved = index;
                        }
                    }
                }

                // Determine the value for .ParentIonIntensity
                mMASICPeakFinder.ComputeParentIonIntensity(parentIon.SICData, sicStats.Peak, parentIon.FragScanObserved);

                bool recomputeNoiseLevel;
                if (parentIon.SICStats.Peak.BaselineNoiseStats.NoiseThresholdModeUsed == clsMASICPeakFinder.eNoiseThresholdModes.DualTrimmedMeanByAbundance)
                {
                    recomputeNoiseLevel = false;
                }
                else
                {
                    recomputeNoiseLevel = true;
                    // Note: We cannot use DualTrimmedMeanByAbundance since we don't have access to the full-length SICs
                    if (mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode == clsMASICPeakFinder.eNoiseThresholdModes.DualTrimmedMeanByAbundance)
                    {
                        mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance;
                    }
                }

                mMASICPeakFinder.FindSICPeakAndArea(parentIon.SICData, out var potentialAreaStatsForPeak, sicStats.Peak,
                                                    out var smoothedYDataSubset, mSICPeakFinderOptions,
                                                    sicPotentialAreaStatsForRegion,
                                                    returnClosestPeak, false, recomputeNoiseLevel);
                sicStats.SICPotentialAreaStatsForPeak = potentialAreaStatsForPeak;

                // Copy the data out of smoothedYDataSubset and into sicStats.SICSmoothedYData
                sicStats.SICSmoothedYData.Clear();

                foreach (var dataPoint in parentIon.SICData)
                    sicStats.SICSmoothedYData.Add(dataPoint.Intensity);

                sicStats.SICSmoothedYDataIndexStart = smoothedYDataSubset.DataStartIndex;

                try
                {
                    // Update the two computed values
                    sicStats.SICPeakWidthFullScans = mParentIonStats[parentIonIndex].SICData[sicStats.Peak.IndexBaseRight].ScanNumber -
                                                     mParentIonStats[parentIonIndex].SICData[sicStats.Peak.IndexBaseLeft].ScanNumber + 1;

                    sicStats.ScanNumberMaxIntensity = mParentIonStats[parentIonIndex].SICData[sicStats.Peak.IndexMax].ScanNumber;
                }
                catch (Exception ex)
                {
                    // Index out of range; ignore the error
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in FindSICPeakAndAreaForParentIon: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
        }

        private void FindSimilarParentIon()
        {
            const double SIMILAR_MZ_TOLERANCE = 0.2;

            FindSimilarParentIon(SIMILAR_MZ_TOLERANCE);
        }

        private void FindSimilarParentIon(double similarMZTolerance)
        {
            const double DEFAULT_INTENSITY = 1;

            try
            {
                if (mParentIonStats == null || mParentIonStats.Count == 0)
                {
                    return;
                }

                var similarFragScans = new List<int>();

                for (var parentIonIndex = 0; parentIonIndex <= mParentIonStats.Count - 1; parentIonIndex++)
                {
                    var currentParentIon = mParentIonStats[parentIonIndex];
                    currentParentIon.SimilarFragScans.Clear();

                    if (currentParentIon.SICData.Count == 0)
                    {
                        continue;
                    }

                    var parentIonMZ = currentParentIon.MZ;
                    var scanStart = currentParentIon.SICData.First().ScanNumber;
                    var scanEnd = currentParentIon.SICData.Last().ScanNumber;

                    currentParentIon.SimilarFragScans.Clear();

                    // Always store this parent ion's fragmentation scan in similarFragScans
                    // Note that it's possible for .FragScanObserved to be outside the range of scanStart and scanEnd
                    // We'll allow this, but won't be able to properly determine the abundance value to use for .SimilarFragScanPlottingIntensity()
                    similarFragScans.Clear();
                    similarFragScans.Add(currentParentIon.FragScanObserved);

                    // Step through the parent ions and look for others with a similar m/z and a fragmentation scan
                    // between scanStart and scanEnd

                    for (var indexCompare = 0; indexCompare <= mParentIonStats.Count - 1; indexCompare++)
                    {
                        var ionStats = mParentIonStats[indexCompare];
                        if (indexCompare != parentIonIndex && ionStats.FragScanObserved >= scanStart && ionStats.FragScanObserved <= scanEnd)
                        {
                            if (Math.Abs(ionStats.MZ - parentIonMZ) <= similarMZTolerance)
                            {
                                // Similar parent ion m/z found

                                similarFragScans.Add(ionStats.FragScanObserved);
                            }
                        }
                    }

                    if (similarFragScans.Count == 0)
                    {
                        continue;
                    }

                    // Sort the data in similarFragScans
                    similarFragScans.Sort();

                    // Copy the sorted data into .SimilarFragScans
                    // When copying, make sure we don't have any duplicates
                    // Also, populate SimilarFragScanPlottingIntensity

                    var scanNumbers = (from item in currentParentIon.SICData select item.ScanNumber).ToList();

                    foreach (var similarFragScan in similarFragScans)
                    {
                        if (currentParentIon.SimilarFragScans.Count > 0)
                        {
                            if (similarFragScan == currentParentIon.SimilarFragScans.Last().ScanNumber)
                            {
                                continue;
                            }
                        }

                        // Look for similarFragScan in .SICScans() then use the corresponding
                        // intensity value in .SICData()

                        var matchIndex = clsBinarySearch.BinarySearchFindNearest(scanNumbers,
                                                                                 similarFragScan,
                                                                                 scanNumbers.Count,
                                                                                 clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint);

                        if (matchIndex < 0)
                        {
                            // Match not found; find the closest match via brute-force searching
                            matchIndex = -1;
                            for (var scanIndex = 0; scanIndex <= scanNumbers.Count - 2; scanIndex++)
                            {
                                if (scanNumbers[scanIndex] <= similarFragScan &&
                                    scanNumbers[scanIndex + 1] >= similarFragScan)
                                {
                                    matchIndex = scanIndex;
                                    break;
                                }
                            }
                        }

                        var interpolatedYValue = DEFAULT_INTENSITY;

                        if (matchIndex >= 0)
                        {
                            if (matchIndex < scanNumbers.Count - 1)
                            {
                                if (similarFragScan <= currentParentIon.SICData[matchIndex].ScanNumber)
                                {
                                    // Frag scan is at or before the first scan number in .SICData()
                                    // Use the intensity at .SICData(0)
                                    interpolatedYValue = currentParentIon.SICData[0].Intensity;
                                }
                                else
                                {
                                    var success = InterpolateY(
                                        out interpolatedYValue,
                                        currentParentIon.SICData[matchIndex].ScanNumber,
                                        currentParentIon.SICData[matchIndex + 1].ScanNumber,
                                        currentParentIon.SICData[matchIndex].Intensity,
                                        currentParentIon.SICData[matchIndex + 1].Intensity,
                                        similarFragScan);

                                    if (!success)
                                    {
                                        interpolatedYValue = DEFAULT_INTENSITY;
                                    }
                                }
                            }
                            else
                            {
                                // Frag scan is at or after the last scan number in .SICData()
                                // Use the last data point in .SICData()
                                interpolatedYValue = currentParentIon.SICData.Last().Intensity;
                            }
                        }

                        var newDataPoint = new clsSICDataPoint(similarFragScan, interpolatedYValue, 0);
                        currentParentIon.SimilarFragScans.Add(newDataPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in FindSimilarParentIon: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private bool GetSettingVal(string appName, string sectionName, string key, bool defaultValue)
        {
            var value = Interaction.GetSetting(appName, sectionName, key, defaultValue.ToString());
            if (bool.TryParse(value, out var parsedValue))
                return parsedValue;

            return defaultValue;
        }

        private int GetSettingVal(string appName, string sectionName, string key, int defaultValue)
        {
            var value = Interaction.GetSetting(appName, sectionName, key, defaultValue.ToString());
            if (int.TryParse(value, out var parsedValue))
                return parsedValue;

            return defaultValue;
        }

        private float GetSettingVal(string appName, string sectionName, string key, float defaultValue)
        {
            var value = Interaction.GetSetting(appName, sectionName, key, defaultValue.ToString(CultureInfo.InvariantCulture));
            if (float.TryParse(value, out var parsedValue))
                return parsedValue;

            return defaultValue;
        }

        private double GetSortKey(int value1, int value2)
        {
            return double.Parse(value1 + "." + value2.ToString("000000"));
        }

        private double GetSortKey(int value1, double value2)
        {
            return double.Parse(value1 + "." + value2.ToString("000000"));
        }

        private void InitializeControls()
        {
            mMASICPeakFinder = new clsMASICPeakFinder();
            mMASICPeakFinder.ErrorEvent += MASICPeakFinderErrorHandler;

            mSICPeakFinderOptions = clsMASICPeakFinder.GetDefaultSICPeakFinderOptions();

            mParentIonStats = new List<clsParentIonStats>();

            InitializeMsMsResultsStorage();
            PopulateComboBoxes();
            RegReadSettings();
            fraNavigation.Enabled = false;

            mAutoStepIntervalMsec = 200;

            tmrAutoStep.Interval = 10;
            tmrAutoStep.Enabled = true;

            optUseButterworthSmooth.Checked = true;

            SetToolTips();

            txtStats3.Visible = false;
            PositionControls();

            mFileLoadTimer = new System.Windows.Forms.Timer();

            mFileLoadTimer.Tick += mFileLoadTimer_Tick;

            mFileLoadTimer.Interval = 500;
            mFileLoadTimer.Start();
        }

        private void InitializeMsMsResultsStorage()
        {
            // ---------------------------------------------------------
            // Create the MS/MS Search Engine Results DataTable
            // ---------------------------------------------------------
            var msmsResultsTable = new DataTable(TABLE_NAME_MSMS_RESULTS);

            // Add the columns to the DataTable
            DataTableUtils.AppendColumnIntegerToTable(msmsResultsTable, COL_NAME_SCAN);
            DataTableUtils.AppendColumnIntegerToTable(msmsResultsTable, COL_NAME_CHARGE);
            DataTableUtils.AppendColumnFloatToTable(msmsResultsTable, COL_NAME_MH);
            DataTableUtils.AppendColumnFloatToTable(msmsResultsTable, COL_NAME_XCORR);
            DataTableUtils.AppendColumnFloatToTable(msmsResultsTable, COL_NAME_DELTACN);
            DataTableUtils.AppendColumnFloatToTable(msmsResultsTable, COL_NAME_DELTACN2);
            DataTableUtils.AppendColumnIntegerToTable(msmsResultsTable, COL_NAME_RANK_SP);
            DataTableUtils.AppendColumnIntegerToTable(msmsResultsTable, COL_NAME_RANK_XC);
            DataTableUtils.AppendColumnIntegerToTable(msmsResultsTable, COL_NAME_SEQUENCE_ID);
            DataTableUtils.AppendColumnIntegerToTable(msmsResultsTable, COL_NAME_PARENT_ION_INDEX);

            // Define a primary key
            msmsResultsTable.PrimaryKey = new DataColumn[] { msmsResultsTable.Columns[COL_NAME_SCAN], msmsResultsTable.Columns[COL_NAME_CHARGE], msmsResultsTable.Columns[COL_NAME_SEQUENCE_ID] };

            // Instantiate the DataSet
            mMsMsResults = new DataSet("MsMsData");

            // Add the table to the DataSet
            mMsMsResults.Tables.Add(msmsResultsTable);

            // ---------------------------------------------------------
            // Create the Sequence to Protein Map DataTable
            // ---------------------------------------------------------
            var seqToProteinMapTable = new DataTable(TABLE_NAME_SEQ_TO_PROTEIN_MAP);

            // Add the columns to the DataTable
            DataTableUtils.AppendColumnIntegerToTable(seqToProteinMapTable, COL_NAME_SEQUENCE_ID);
            DataTableUtils.AppendColumnStringToTable(seqToProteinMapTable, COL_NAME_PROTEIN);

            // Define a primary key
            seqToProteinMapTable.PrimaryKey = new DataColumn[] { seqToProteinMapTable.Columns[COL_NAME_SEQUENCE_ID], seqToProteinMapTable.Columns[COL_NAME_PROTEIN] };

            // Add the table to the DataSet
            mMsMsResults.Tables.Add(seqToProteinMapTable);

            // ---------------------------------------------------------
            // Create the Sequences DataTable
            // ---------------------------------------------------------
            InitializeSequencesDataTable();
        }

        private void InitializeSequencesDataTable()
        {
            var sequenceInfoTable = new DataTable(TABLE_NAME_SEQUENCES);

            // Add the columns to the DataTable
            DataTableUtils.AppendColumnIntegerToTable(sequenceInfoTable, COL_NAME_SEQUENCE_ID, 0, true, true, true);
            DataTableUtils.AppendColumnStringToTable(sequenceInfoTable, COL_NAME_SEQUENCE);

            // Define a primary key
            sequenceInfoTable.PrimaryKey = new DataColumn[] { sequenceInfoTable.Columns[COL_NAME_SEQUENCE] };

            // Add the table to the DataSet
            if (mMsMsResults.Tables.Contains(TABLE_NAME_SEQUENCES))
            {
                mMsMsResults.Tables.Remove(TABLE_NAME_SEQUENCES);
            }

            mMsMsResults.Tables.Add(sequenceInfoTable);
        }

        private bool InterpolateY(out double interpolatedYValue, int X1, int X2, double Y1, double Y2, int targetX)
        {
            // Checks if X1 or X2 is less than targetX
            // If it is, then determines the Y value that corresponds to targetX by interpolating the line between (X1, Y1) and (X2, Y2)
            //
            // Returns True if a match is found; otherwise, returns false

            if (X1 < targetX || X2 < targetX)
            {
                if (X1 < targetX && X2 < targetX)
                {
                    // Both of the X values are less than targetX
                    // We cannot interpolate
                    Debug.Assert(false, "This code should normally not be reached (frmBrowser->InterpolateY)");
                }
                else
                {
                    var deltaX = X2 - X1;
                    var fraction = (targetX - X1) / (double)deltaX;
                    var deltaY = Y2 - Y1;

                    var targetY = fraction * deltaY + Y1;

                    if (Math.Abs(targetY - Y1) >= 0 && Math.Abs(targetY - Y2) >= 0)
                    {
                        interpolatedYValue = targetY;
                        return true;
                    }
                    else
                    {
                        Debug.Assert(false, "TargetY is not between Y1 and Y2; this shouldn't happen (frmBrowser->InterpolateY)");
                        interpolatedYValue = 0;
                        return false;
                    }
                }
            }

            interpolatedYValue = 0;
            return false;
        }

        private void JumpToScan(int scanNumberToFind = -1)
        {
            try
            {
                if (mParentIonPointerArrayCount > 0)
                {
                    if (scanNumberToFind < 0)
                    {
                        if (lstParentIonData.SelectedIndex >= 0 && lstParentIonData.SelectedIndex <= mParentIonPointerArrayCount)
                        {
                            scanNumberToFind = mParentIonStats[lstParentIonData.SelectedIndex].FragScanObserved;
                        }
                        else
                        {
                            scanNumberToFind = mParentIonStats[0].FragScanObserved;
                        }

                        var response = Interaction.InputBox("Enter the scan number to jump to: ", "Jump to Scan", scanNumberToFind.ToString());
                        if (PRISM.DataUtils.StringToValueUtils.IsNumber(response))
                        {
                            scanNumberToFind = int.Parse(response);
                        }
                    }

                    if (scanNumberToFind >= 0)
                    {
                        // Find the best match to scanNumberToFind
                        // First search for an exact match
                        var indexMatch = -1;
                        for (var index = 0; index <= lstParentIonData.Items.Count - 1; index++)
                        {
                            if (mParentIonStats[mParentIonPointerArray[index]].FragScanObserved == scanNumberToFind)
                            {
                                indexMatch = index;
                                break;
                            }
                        }

                        if (indexMatch >= 0)
                        {
                            lstParentIonData.SelectedIndex = indexMatch;
                        }
                        else
                        {
                            // Exact match not found; find the closest match to scanNumberToFind
                            var scanDifference = int.MaxValue;
                            for (var index = 0; index <= lstParentIonData.Items.Count - 1; index++)
                            {
                                if (Math.Abs(mParentIonStats[mParentIonPointerArray[index]].FragScanObserved - scanNumberToFind) < scanDifference)
                                {
                                    scanDifference = Math.Abs(mParentIonStats[mParentIonPointerArray[index]].FragScanObserved - scanNumberToFind);
                                    indexMatch = index;
                                }
                            }

                            if (indexMatch >= 0)
                            {
                                lstParentIonData.SelectedIndex = indexMatch;
                            }
                            else
                            {
                                // Match was not found
                                // Jump to the last entry
                                lstParentIonData.SelectedIndex = lstParentIonData.Items.Count - 1;
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("No data is in memory", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // Ignore any errors in this sub
            }
        }

        private string LookupSequenceForParentIonIndex(int parentIonIndex)
        {
            var sequences = string.Empty;

            var resultTable = mMsMsResults.Tables[TABLE_NAME_MSMS_RESULTS];

            try
            {
                var objRows = resultTable.Select(COL_NAME_PARENT_ION_INDEX + " = " + parentIonIndex);
                var sequenceCount = 0;
                foreach (var objRow in objRows)
                {
                    var sequenceID = (int)objRow[COL_NAME_SEQUENCE_ID];
                    try
                    {
                        var objSeqRows = mMsMsResults.Tables[TABLE_NAME_SEQUENCES].Select(COL_NAME_SEQUENCE_ID + " = " + sequenceID);

                        if (objSeqRows.Length > 0)
                        {
                            if (sequenceCount > 0)
                            {
                                sequences += Environment.NewLine;
                            }

                            sequences += objSeqRows[0][COL_NAME_SEQUENCE].ToString();
                            sequenceCount += 1;
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore errors here
                    }
                }
            }
            catch (Exception)
            {
                sequences = string.Empty;
            }

            return sequences;
        }

        private int LookupSequenceID(string sequence, string protein)
        {
            // Looks for sequence in mMsMsResults.Tables(TABLE_NAME_SEQUENCES)
            // Returns the SequenceID if found; adds it if not present
            // Additionally, adds a mapping between sequence and protein in mMsMsResults.Tables(TABLE_NAME_SEQ_TO_PROTEIN_MAP)

            var sequenceID = -1;
            if (sequence != null)
            {
                var trimmedSequence = sequence.Trim();

                string sequenceNoSuffixes;
                if (trimmedSequence.Length >= 4)
                {
                    if (trimmedSequence.Substring(1, 1).Equals(".") && trimmedSequence.Substring(trimmedSequence.Length - 2, 1).Equals("."))
                    {
                        sequenceNoSuffixes = trimmedSequence.Substring(2, trimmedSequence.Length - 4);
                    }
                    else
                    {
                        sequenceNoSuffixes = string.Copy(trimmedSequence);
                    }
                }
                else
                {
                    sequenceNoSuffixes = string.Copy(trimmedSequence);
                }

                // Try to add sequenceNoSuffixes to .Tables(TABLE_NAME_SEQUENCES)
                try
                {
                    var sequencesTable = mMsMsResults.Tables[TABLE_NAME_SEQUENCES];
                    var objNewRow = sequencesTable.Rows.Find(sequenceNoSuffixes);
                    if (objNewRow == null)
                    {
                        objNewRow = sequencesTable.NewRow();
                        objNewRow[COL_NAME_SEQUENCE] = sequenceNoSuffixes;
                        sequencesTable.Rows.Add(objNewRow);
                    }

                    sequenceID = (int)objNewRow[COL_NAME_SEQUENCE_ID];

                    if (sequenceID >= 0)
                    {
                        try
                        {
                            // Possibly add sequenceNoSuffixes and protein to .Tables(TABLE_NAME_SEQ_TO_PROTEIN_MAP)
                            var seqProtMapTable = mMsMsResults.Tables[TABLE_NAME_SEQ_TO_PROTEIN_MAP];
                            if (!seqProtMapTable.Rows.Contains(new object[] { sequenceID, protein }))
                            {
                                objNewRow = seqProtMapTable.NewRow();
                                objNewRow[COL_NAME_SEQUENCE_ID] = sequenceID;
                                objNewRow[COL_NAME_PROTEIN] = protein;
                                seqProtMapTable.Rows.Add(objNewRow);
                            }
                        }
                        catch (Exception)
                        {
                            // Ignore errors here
                        }
                    }
                }
                catch (Exception)
                {
                    sequenceID = -1;
                }
            }

            return sequenceID;
        }

        private void NavigateScanList(bool moveForward)
        {
            if (moveForward)
            {
                if (lstParentIonData.SelectedIndex < lstParentIonData.Items.Count - 1)
                {
                    lstParentIonData.SelectedIndex += 1;
                }
                else if (mAutoStepEnabled)
                    ToggleAutoStep(true);
            }
            else if (lstParentIonData.SelectedIndex > 0)
            {
                lstParentIonData.SelectedIndex -= 1;
            }
            else if (mAutoStepEnabled)
                ToggleAutoStep(true);
        }

        private void PlotData(int indexToPlot, clsSICStats sicStats)
        {
            // indexToPlot points to an entry in mParentIonStats()

            // We plot the data as two different series to allow for different coloring

            try
            {
                if (indexToPlot < 0 || indexToPlot >= mParentIonStats.Count)
                    return;

                Cursor = Cursors.WaitCursor;

                if (mSpectrum == null)
                {
                    mSpectrum = new Spectrum();
                    mSpectrum.SetSpectrumFormWindowCaption("Selected Ion Chromatogram");
                }

                mSpectrum.RemoveAllAnnotations();

                var currentParentIon = mParentIonStats[indexToPlot];

                if (currentParentIon.SICData.Count == 0)
                    return;

                var dataCountSeries1 = 0;
                var dataCountSeries2 = 0;
                var dataCountSeries3 = currentParentIon.SimilarFragScans.Count;
                var dataCountSeries4 = 0;

                // Length + 1: Need extra room for potential zero padding
                var xDataSeries1 = new double[currentParentIon.SICData.Count + 3 + 1];          // Holds the scans and SIC data for data <=0 (data not part of the peak)
                var yDataSeries1 = new double[currentParentIon.SICData.Count + 3 + 1];          // Holds the scans and SIC data for data <=0 (data not part of the peak)

                var xDataSeries2 = new double[currentParentIon.SICData.Count + 3 + 1];          // Holds the scans and SIC data for data > 0 (data part of the peak)
                var yDataSeries2 = new double[currentParentIon.SICData.Count + 3 + 1];          // Holds the scans and SIC data for data > 0 (data part of the peak)

                var xDataSeries3 = new double[currentParentIon.SimilarFragScans.Count];          // Holds the scan numbers at which the given m/z was chosen for fragmentation
                var yDataSeries3 = new double[currentParentIon.SimilarFragScans.Count];          // Holds the scan numbers at which the given m/z was chosen for fragmentation

                var xDataSeries4 = new double[currentParentIon.SICData.Count + 1];               // Holds the smoothed SIC data
                var yDataSeries4 = new double[currentParentIon.SICData.Count + 1];               // Holds the smoothed SIC data

                var zeroEdgeSeries1 = false;

                if (sicStats.Peak.IndexBaseLeft == 0)
                {
                    // Zero pad Series 1
                    xDataSeries1[0] = currentParentIon.SICData[0].ScanNumber;
                    yDataSeries1[0] = 0;
                    dataCountSeries1 += 1;

                    // Zero pad Series 2
                    xDataSeries2[0] = currentParentIon.SICData[0].ScanNumber;
                    yDataSeries2[0] = 0;
                    dataCountSeries2 += 1;

                    zeroEdgeSeries1 = true;
                }

                // Initialize this to 0, in case .FragScanObserved is out of range
                double scanObservedIntensity = 0;

                // Initialize this to the maximum intensity, in case .OptimalPeakApexScanNumber is out of range
                var optimalPeakApexIntensity = currentParentIon.SICIntensityMax;

                int smoothedYDataIndexStart;
                double[] smoothedYData;

                if (sicStats.SICSmoothedYData == null || sicStats.SICSmoothedYData.Count == 0)
                {
                    smoothedYDataIndexStart = 0;
                    smoothedYData = new double[0];
                }
                else
                {
                    smoothedYDataIndexStart = sicStats.SICSmoothedYDataIndexStart;

                    smoothedYData = new double[sicStats.SICSmoothedYData.Count];
                    for (var index = 0; index <= sicStats.SICSmoothedYData.Count - 1; index++)
                        smoothedYData[index] = sicStats.SICSmoothedYData[index];
                }

                // Populate Series 3 with the similar frag scan values
                for (var index = 0; index <= currentParentIon.SimilarFragScans.Count - 1; index++)
                {
                    xDataSeries3[index] = currentParentIon.SimilarFragScans[index].ScanNumber;
                    yDataSeries3[index] = currentParentIon.SimilarFragScans[index].Intensity;
                }

                for (var index = 0; index <= currentParentIon.SICData.Count - 1; index++)
                {
                    if (index < currentParentIon.SICData.Count - 1)
                    {
                        if (currentParentIon.SICData[index].ScanNumber <= currentParentIon.FragScanObserved && currentParentIon.SICData[index + 1].ScanNumber >= currentParentIon.FragScanObserved)
                        {
                            // Use the survey scan data to calculate the appropriate intensity for the Frag Scan cursor

                            if (InterpolateY(out var interpolatedYValue, currentParentIon.SICData[index].ScanNumber, currentParentIon.SICData[index + 1].ScanNumber, currentParentIon.SICData[index].Intensity, currentParentIon.SICData[index + 1].Intensity, currentParentIon.FragScanObserved))
                            {
                                scanObservedIntensity = interpolatedYValue;
                            }
                        }

                        if (currentParentIon.SICData[index].ScanNumber <= currentParentIon.OptimalPeakApexScanNumber && currentParentIon.SICData[index + 1].ScanNumber >= currentParentIon.OptimalPeakApexScanNumber)
                        {
                            // Use the survey scan data to calculate the appropriate intensity for the Optimal Peak Apex Scan cursor

                            if (InterpolateY(out var interpolatedYValue, currentParentIon.SICData[index].ScanNumber, currentParentIon.SICData[index + 1].ScanNumber, currentParentIon.SICData[index].Intensity, currentParentIon.SICData[index + 1].Intensity, currentParentIon.OptimalPeakApexScanNumber))
                            {
                                optimalPeakApexIntensity = interpolatedYValue;
                            }
                        }
                    }

                    if (index >= sicStats.Peak.IndexBaseLeft && index <= sicStats.Peak.IndexBaseRight)
                    {
                        if (index > 0 && !zeroEdgeSeries1)
                        {
                            // Zero pad Series 1
                            xDataSeries1[dataCountSeries1] = currentParentIon.SICData[index].ScanNumber;
                            yDataSeries1[dataCountSeries1] = currentParentIon.SICData[index].Intensity;
                            dataCountSeries1 += 1;
                            xDataSeries1[dataCountSeries1] = currentParentIon.SICData[index].ScanNumber;
                            yDataSeries1[dataCountSeries1] = 0;
                            dataCountSeries1 += 1;

                            // Zero pad Series 2
                            xDataSeries2[dataCountSeries2] = currentParentIon.SICData[index].ScanNumber;
                            yDataSeries2[dataCountSeries2] = 0;
                            dataCountSeries2 += 1;

                            zeroEdgeSeries1 = true;
                        }

                        xDataSeries2[dataCountSeries2] = currentParentIon.SICData[index].ScanNumber;
                        yDataSeries2[dataCountSeries2] = currentParentIon.SICData[index].Intensity;
                        dataCountSeries2 += 1;
                    }
                    else
                    {
                        if (index > 0 && zeroEdgeSeries1)
                        {
                            // Zero pad Series 2
                            xDataSeries2[dataCountSeries2] = currentParentIon.SICData[index - 1].ScanNumber;
                            yDataSeries2[dataCountSeries2] = 0;
                            dataCountSeries2 += 1;

                            // Zero pad Series 1
                            xDataSeries1[dataCountSeries1] = currentParentIon.SICData[index - 1].ScanNumber;
                            yDataSeries1[dataCountSeries1] = 0;
                            dataCountSeries1 += 1;

                            xDataSeries1[dataCountSeries1] = currentParentIon.SICData[index - 1].ScanNumber;
                            yDataSeries1[dataCountSeries1] = currentParentIon.SICData[index - 1].Intensity;
                            dataCountSeries1 += 1;
                            zeroEdgeSeries1 = false;
                        }

                        xDataSeries1[dataCountSeries1] = currentParentIon.SICData[index].ScanNumber;
                        yDataSeries1[dataCountSeries1] = currentParentIon.SICData[index].Intensity;
                        dataCountSeries1 += 1;
                    }

                    if (index >= smoothedYDataIndexStart &&
                        index - smoothedYDataIndexStart < smoothedYData.Length)
                    {
                        xDataSeries4[dataCountSeries4] = currentParentIon.SICData[index].ScanNumber;
                        yDataSeries4[dataCountSeries4] = smoothedYData[index - smoothedYDataIndexStart];
                        dataCountSeries4 += 1;
                    }
                }

                // Shrink the data arrays
                // SIC Data
                var oldXDataSeries1 = xDataSeries1;
                xDataSeries1 = new double[dataCountSeries1];
                Array.Copy(oldXDataSeries1, xDataSeries1, Math.Min(dataCountSeries1, oldXDataSeries1.Length));
                var oldYDataSeries1 = yDataSeries1;
                yDataSeries1 = new double[dataCountSeries1];
                Array.Copy(oldYDataSeries1, yDataSeries1, Math.Min(dataCountSeries1, oldYDataSeries1.Length));

                // SIC Peak
                var oldXDataSeries2 = xDataSeries2;
                xDataSeries2 = new double[dataCountSeries2];
                Array.Copy(oldXDataSeries2, xDataSeries2, Math.Min(dataCountSeries2, oldXDataSeries2.Length));
                var oldYDataSeries2 = yDataSeries2;
                yDataSeries2 = new double[dataCountSeries2];
                Array.Copy(oldYDataSeries2, yDataSeries2, Math.Min(dataCountSeries2, oldYDataSeries2.Length));

                // Smoothed Data
                var oldXDataSeries4 = xDataSeries4;
                xDataSeries4 = new double[dataCountSeries4];
                Array.Copy(oldXDataSeries4, xDataSeries4, Math.Min(dataCountSeries4, oldXDataSeries4.Length));
                var oldYDataSeries4 = yDataSeries4;
                yDataSeries4 = new double[dataCountSeries4];
                Array.Copy(oldYDataSeries4, yDataSeries4, Math.Min(dataCountSeries4, oldYDataSeries4.Length));

                mSpectrum.ShowSpectrum();

                mSpectrum.SetDataXvsY(1, xDataSeries1, yDataSeries1, dataCountSeries1, ctlOxyPlotControl.SeriesPlotMode.PointsAndLines, "SIC Data");

                string peakCaption;

                if (sicStats.Peak.ShoulderCount == 1)
                {
                    peakCaption = "SIC Data Peak (1 shoulder peak)";
                }
                else if (sicStats.Peak.ShoulderCount > 1)
                {
                    peakCaption = "SIC Data Peak (" + sicStats.Peak.ShoulderCount + " shoulder peaks)";
                }
                else
                {
                    peakCaption = "SIC Data Peak";
                }

                mSpectrum.SetDataXvsY(2, xDataSeries2, yDataSeries2, dataCountSeries2, ctlOxyPlotControl.SeriesPlotMode.PointsAndLines, peakCaption);

                var fragScansCaption = "Similar Frag scans";
                mSpectrum.SetDataXvsY(3, xDataSeries3, yDataSeries3, dataCountSeries3, ctlOxyPlotControl.SeriesPlotMode.Points, fragScansCaption);

                if (chkShowSmoothedData.Checked && xDataSeries4.Length > 0)
                {
                    mSpectrum.SetDataXvsY(4, xDataSeries4, yDataSeries4, dataCountSeries4, ctlOxyPlotControl.SeriesPlotMode.Lines, "Smoothed data");
                }
                else
                {
                    while (mSpectrum.GetSeriesCount() >= 4)
                        mSpectrum.RemoveSeries(4);
                }

                var actualSeriesCount = mSpectrum.GetSeriesCount();

                mSpectrum.SetSeriesLineStyle(1, LineStyle.Automatic);
                mSpectrum.SetSeriesLineStyle(2, LineStyle.Automatic);

                mSpectrum.SetSeriesPointStyle(1, MarkerType.Diamond);
                mSpectrum.SetSeriesPointStyle(2, MarkerType.Square);
                mSpectrum.SetSeriesPointStyle(3, MarkerType.Circle);

                mSpectrum.SetSeriesColor(1, Color.Blue);
                mSpectrum.SetSeriesColor(2, Color.Red);
                mSpectrum.SetSeriesColor(3, Color.FromArgb(255, 20, 210, 20));

                mSpectrum.SetSeriesLineWidth(1, 1);
                mSpectrum.SetSeriesLineWidth(2, 2);

                mSpectrum.SetSeriesPointSize(3, 7);

                if (actualSeriesCount > 3)
                {
                    mSpectrum.SetSeriesLineStyle(4, LineStyle.Automatic);
                    mSpectrum.SetSeriesPointStyle(4, MarkerType.None);
                    mSpectrum.SetSeriesColor(4, Color.Purple);
                    mSpectrum.SetSeriesLineWidth(4, 2);
                }

                var arrowLengthPixels = 15;
                ctlOxyPlotControl.CaptionOffsetDirection captionOffsetDirection;

                if (currentParentIon.FragScanObserved <= currentParentIon.OptimalPeakApexScanNumber)
                {
                    captionOffsetDirection = ctlOxyPlotControl.CaptionOffsetDirection.TopLeft;
                }
                else
                {
                    captionOffsetDirection = ctlOxyPlotControl.CaptionOffsetDirection.TopRight;
                }

                var fragScanObserved = currentParentIon.FragScanObserved;

                const int seriesToUse = 0;
                mSpectrum.SetAnnotationForDataPoint(fragScanObserved, scanObservedIntensity, "MS2",
                                                    seriesToUse, captionOffsetDirection, arrowLengthPixels);

                if (mnuEditShowOptimalPeakApexCursor.Checked)
                {
                    mSpectrum.SetAnnotationForDataPoint(
                        currentParentIon.OptimalPeakApexScanNumber, optimalPeakApexIntensity, "Peak",
                        2, ctlOxyPlotControl.CaptionOffsetDirection.TopLeft, arrowLengthPixels);
                }

                int xRangeHalfWidth;
                if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtFixXRange.Text))
                {
                    xRangeHalfWidth = (int)(int.Parse(txtFixXRange.Text) / 2.0);
                }
                else
                {
                    xRangeHalfWidth = 0;
                }

                double yRange;
                if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtFixYRange.Text))
                {
                    yRange = double.Parse(txtFixYRange.Text);
                }
                else
                {
                    yRange = 0;
                }

                mSpectrum.SetLabelXAxis("Scan number");
                mSpectrum.SetLabelYAxis("Intensity");

                // Update the axis padding
                mSpectrum.XAxisPaddingMinimum = 0.01;
                mSpectrum.XAxisPaddingMaximum = 0.01;

                mSpectrum.YAxisPaddingMinimum = 0.02;
                mSpectrum.YAxisPaddingMaximum = 0.15;

                if (chkFixXRange.Checked && xRangeHalfWidth > 0)
                {
                    mSpectrum.SetAutoscaleXAxis(false);
                    mSpectrum.SetRangeX(sicStats.ScanNumberMaxIntensity - xRangeHalfWidth, sicStats.ScanNumberMaxIntensity + xRangeHalfWidth);
                }
                else
                {
                    mSpectrum.SetAutoscaleXAxis(true);
                }

                if (chkFixYRange.Checked && yRange > 0)
                {
                    mSpectrum.SetAutoscaleYAxis(false);
                    mSpectrum.SetRangeY(0, yRange);
                }
                else
                {
                    mSpectrum.SetAutoscaleYAxis(true);
                }
            }
            catch (Exception ex)
            {
                var sTrace = StackTraceFormatter.GetExceptionStackTraceMultiLine(ex);
                MessageBox.Show("Error in PlotData: " + ex.Message + Environment.NewLine + sTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void PopulateComboBoxes()
        {
            cboSortOrder.Items.Clear();
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByPeakIndex, "Sort by Scan of Peak Index");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByScanPeakCenter, "Sort by Scan of Peak Center");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByScanOptimalPeakCenter, "Sort by Scan of Optimal Peak Apex");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByMz, "Sort by m/z");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByPeakSignalToNoise, "Sort by Peak Signal/Noise");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByBaselineCorrectedPeakIntensity, "Sort by Baseline-corrected Intensity");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByBaselineCorrectedPeakArea, "Sort by Baseline-corrected Area");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByPeakWidth, "Sort by Peak FWHM (Width)");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortBySICIntensityMax, "Sort by SIC Max Intensity");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByPeakIntensity, "Sort by Peak Intensity (uncorrected for noise)");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByPeakArea, "Sort by Peak Area (uncorrected for noise)");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByFragScanToOptimalLocDistance, "Sort by Frag Scan to Optimal Loc Distance");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByPeakCenterToOptimalLocDistance, "Sort by Peak Center to Optimal Loc Distance");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByShoulderCount, "Sort by Shoulder Peak Count");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByParentIonIntensity, "Sort by Parent Ion Intensity");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByPeakSkew, "Sort by Peak Skew");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByKSStat, "Sort by Peak KS Stat");
            cboSortOrder.Items.Insert((int)eSortOrderConstants.SortByBaselineNoiseLevel, "Sort by Baseline Noise level");
            cboSortOrder.SelectedIndex = (int)eSortOrderConstants.SortByPeakSignalToNoise;

            cboSICsTypeFilter.Items.Clear();
            cboSICsTypeFilter.Items.Insert((int)eSICTypeFilterConstants.AllSICs, "All SIC's");
            cboSICsTypeFilter.Items.Insert((int)eSICTypeFilterConstants.NoCustomSICs, "No custom SIC's");
            cboSICsTypeFilter.Items.Insert((int)eSICTypeFilterConstants.CustomSICsOnly, "Custom SIC's only");

            cboSICsTypeFilter.SelectedIndex = (int)eSICTypeFilterConstants.AllSICs;
        }

        private void PopulateParentIonIndexColumnInMsMsResultsTable()
        {
            // For each row in mMsMsResults.Tables(TABLE_NAME_MSMS_RESULTS), find the corresponding row in mParentIonStats

            // Construct a mapping between .FragScanObserved and Index in mParentIonStats
            // If multiple parent ions have the same value for .FragScanObserved, then the we will only track the mapping to the first one

            var htFragScanToIndex = new Hashtable();

            for (var index = 0; index <= mParentIonStats.Count - 1; index++)
            {
                if (!htFragScanToIndex.ContainsKey(mParentIonStats[index].FragScanObserved))
                {
                    htFragScanToIndex.Add(mParentIonStats[index].FragScanObserved, index);
                }
            }

            foreach (DataRow objRow in mMsMsResults.Tables[TABLE_NAME_MSMS_RESULTS].Rows)
            {
                if (htFragScanToIndex.Contains(objRow[COL_NAME_SCAN]))
                {
                    objRow[COL_NAME_PARENT_ION_INDEX] = htFragScanToIndex[objRow[COL_NAME_SCAN]];
                }
            }
        }

        private void PopulateSpectrumList(int scanNumberToHighlight)
        {
            try
            {
                lstParentIonData.Items.Clear();

                if (mParentIonPointerArrayCount > 0)
                {
                    for (var index = 0; index <= mParentIonPointerArrayCount - 1; index++)
                    {
                        var ionStats = mParentIonStats[mParentIonPointerArray[index]];
                        var parentIonDesc = "Scan " + ionStats.FragScanObserved + "  (" + Math.Round(ionStats.MZ, 4) + " m/z)";
                        lstParentIonData.Items.Add(parentIonDesc);
                    }

                    lstParentIonData.SelectedIndex = 0;

                    JumpToScan(scanNumberToHighlight);

                    fraNavigation.Enabled = true;
                }
                else
                {
                    fraNavigation.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in PopulateSpectrumList: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void PositionControls()
        {
            int desiredValue;

            // desiredValue = Me.Height - lstParentIonData.Top - 425
            // If desiredValue < 5 Then
            // desiredValue = 5
            // End If
            // lstParentIonData.Height = desiredValue

            // fraSortOrderAndStats.Top = lstParentIonData.Top + lstParentIonData.Height + 1
            if (txtStats3.Visible)
            {
                desiredValue = fraSortOrderAndStats.Height - txtStats1.Top - txtStats3.Height - 1 - 5;
            }
            else
            {
                desiredValue = fraSortOrderAndStats.Height - txtStats1.Top - 1 - 5;
            }

            if (desiredValue < 1)
                desiredValue = 1;
            txtStats1.Height = desiredValue;
            txtStats2.Height = desiredValue;

            txtStats3.Top = txtStats1.Top + txtStats1.Height + 1;
        }

        private void ReadDataFileXMLTextReader(string filePath)
        {
            var indexInXMLFile = "-1";

            var errorMessages = new List<string>();

            float similarIonMZToleranceHalfWidth = 0;
            var findPeaksOnSmoothedData = false;
            bool useButterworthSmooth = default, useSavitzkyGolaySmooth = default;
            float butterworthSamplingFrequency = 0;
            var savitzkyGolayFilterOrder = 0;
            var scanStart = 0;

            int peakScanStart = default, peakScanEnd = default;

            var smoothedDataFound = false;
            var baselineNoiseStatsFound = false;

            var scanIntervals = string.Empty;
            var intensityDataList = string.Empty;
            var massDataList = string.Empty;
            var smoothedYDataList = string.Empty;
            var delimiters = " ,;" + '\t';

            var delimiterList = delimiters.ToCharArray();

            var expectedSicDataCount = 0;

            if (!File.Exists(filePath))
            {
                return;
            }

            // Initialize the progress form
            var objProgress = new frmProgress();
            try
            {

                // Initialize the stream reader and the XML Text Reader
                eCurrentXMLDataFileSectionConstants eCurrentXMLDataFileSection;
                using (var reader = new StreamReader(filePath))
                using (var objXMLReader = new XmlTextReader(reader))
                {
                    objProgress.InitializeProgressForm("Reading file " + Environment.NewLine + PathUtils.CompactPathString(filePath, 40), 0, 1, true);
                    objProgress.Show();
                    Application.DoEvents();

                    // Initialize mParentIonStats
                    mParentIonStats.Clear();

                    eCurrentXMLDataFileSection = eCurrentXMLDataFileSectionConstants.UnknownFile;
                    var validParentIon = false;

                    while (objXMLReader.Read())
                    {
                        XMLTextReaderSkipWhitespace(objXMLReader);
                        if (objXMLReader.ReadState != ReadState.Interactive)
                            break;
                        if (objXMLReader.Depth < 2)
                        {
                            if (objXMLReader.NodeType == XmlNodeType.Element)
                            {
                                switch (objXMLReader.Name)
                                {
                                    case "ParentIon":
                                        eCurrentXMLDataFileSection = eCurrentXMLDataFileSectionConstants.ParentIons;
                                        validParentIon = false;

                                        if (objXMLReader.HasAttributes)
                                        {
                                            baselineNoiseStatsFound = false;
                                            scanStart = 0;
                                            peakScanStart = 0;
                                            peakScanEnd = 0;

                                            scanIntervals = string.Empty;
                                            intensityDataList = string.Empty;
                                            massDataList = string.Empty;
                                            smoothedYDataList = string.Empty;

                                            indexInXMLFile = objXMLReader.GetAttribute("Index");

                                            var newParentIon = new clsParentIonStats()
                                            {
                                                Index = int.Parse(indexInXMLFile),
                                                MZ = 0.0,
                                                SICIntensityMax = 0.0
                                            };

                                            var sicStats = newParentIon.SICStats;
                                            sicStats.SICPeakWidthFullScans = 0;
                                            var sicStatsPeak = sicStats.Peak;
                                            sicStatsPeak.IndexBaseLeft = -1;
                                            sicStatsPeak.IndexBaseRight = -1;
                                            sicStatsPeak.MaxIntensityValue = 0.0;
                                            sicStatsPeak.ShoulderCount = 0;

                                            mParentIonStats.Add(newParentIon);
                                            validParentIon = true;

                                            // Update the progress bar
                                            var percentComplete = (double)reader.BaseStream.Position / (double)reader.BaseStream.Length;
                                            if (percentComplete > 1.0)
                                                percentComplete = 1.0;

                                            objProgress.UpdateProgressBar(percentComplete);
                                            Application.DoEvents();
                                            if (objProgress.KeyPressAbortProcess)
                                                break;

                                            // Advance to the next tag
                                            objXMLReader.Read();
                                            XMLTextReaderSkipWhitespace(objXMLReader);
                                            if (objXMLReader.ReadState != ReadState.Interactive)
                                                break;
                                        }
                                        else
                                        {
                                            // Attribute isn't present; skip this parent ion
                                            indexInXMLFile = "-1";
                                        }

                                        break;

                                    case "SICData":
                                        eCurrentXMLDataFileSection = eCurrentXMLDataFileSectionConstants.Start;
                                        break;
                                    case "ProcessingSummary":
                                        objXMLReader.Skip();
                                        break;
                                    case "MemoryOptions":
                                        objXMLReader.Skip();
                                        break;
                                    case "SICOptions":
                                        eCurrentXMLDataFileSection = eCurrentXMLDataFileSectionConstants.Options;
                                        break;
                                    case "ProcessingStats":
                                        objXMLReader.Skip();
                                        break;
                                }
                            }
                            else if (objXMLReader.NodeType == XmlNodeType.EndElement)
                            {
                                if ((objXMLReader.Name ?? "") == "ParentIon")
                                {
                                    if (validParentIon)
                                    {
                                        // End element found for the current parent ion

                                        var currentParentIon = mParentIonStats.Last();

                                        // Split apart the value list variables

                                        var sicScans = new List<int>();
                                        var sicIntensities = new List<double>();
                                        var sicMasses = new List<double>();

                                        sicScans.Add(scanStart);

                                        // scanIntervals contains the intervals from each scan to the next
                                        // If the interval is <=9, then it is stored as a number
                                        // For intervals between 10 and 35, uses letters A to Z
                                        // For intervals between 36 and 61, uses letters A to Z
                                        if (scanIntervals != null)
                                        {
                                            for (var charIndex = 1; charIndex <= scanIntervals.Length - 1; charIndex++)
                                            {
                                                int interval;
                                                if (char.IsNumber(scanIntervals[charIndex]))
                                                {
                                                    interval = int.Parse(scanIntervals.Substring(charIndex, 1));
                                                }
                                                else if (char.IsUpper(scanIntervals[charIndex]))
                                                {
                                                    // Uppercase letter
                                                    interval = scanIntervals[charIndex] - 55;
                                                }
                                                else if (char.IsLower(scanIntervals[charIndex]))
                                                {
                                                    // Lowercase letter
                                                    interval = scanIntervals[charIndex] - 61;
                                                }
                                                else
                                                {
                                                    // Not a letter or a number; unknown interval (use 1)
                                                    interval = 1;
                                                }

                                                sicScans.Add(sicScans[charIndex - 1] + interval);
                                            }
                                        }
                                        else
                                        {
                                            errorMessages.Add("Missing 'SICScanInterval' node for parent ion '" + indexInXMLFile + "'");
                                        }

                                        // Split apart the Intensity data list using the delimiters in delimiterList
                                        if (intensityDataList != null)
                                        {
                                            var valueList = intensityDataList.Trim().Split(delimiterList);
                                            for (var index = 0; index <= valueList.Length - 1; index++)
                                            {
                                                if (PRISM.DataUtils.StringToValueUtils.IsNumber(valueList[index]))
                                                {
                                                    sicIntensities.Add(double.Parse(valueList[index]));
                                                }
                                                else
                                                {
                                                    sicIntensities.Add(0);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            errorMessages.Add("Missing 'IntensityDataList' node for parent ion '" + indexInXMLFile + "'");
                                        }

                                        // Split apart the Mass data list using the delimiters in delimiterList
                                        if (massDataList != null)
                                        {
                                            var valueList = massDataList.Trim().Split(delimiterList);
                                            for (var index = 0; index <= valueList.Length - 1; index++)
                                            {
                                                if (PRISM.DataUtils.StringToValueUtils.IsNumber(valueList[index]))
                                                {
                                                    sicMasses.Add(double.Parse(valueList[index]));
                                                }
                                                else
                                                {
                                                    sicMasses.Add(0);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            errorMessages.Add("Missing 'IntensityDataList' node for parent ion '" + indexInXMLFile + "'");
                                        }

                                        for (var index = 0; index <= sicScans.Count - 1; index++)
                                        {
                                            if (index == sicIntensities.Count)
                                            {
                                                break;
                                            }

                                            var massValue = 0.0;
                                            if (index < sicMasses.Count)
                                            {
                                                massValue = sicMasses[index];
                                            }

                                            var newDataPoint = new clsSICDataPoint(sicScans[index], sicIntensities[index], massValue);
                                            currentParentIon.SICData.Add(newDataPoint);
                                        }

                                        if (sicIntensities.Count > sicScans.Count)
                                        {
                                            errorMessages.Add("Too many intensity data points found in parent ion '" + indexInXMLFile + "'");
                                        }

                                        if (sicMasses.Count > sicScans.Count)
                                        {
                                            errorMessages.Add("Too many mass data points found in parent ion '" + indexInXMLFile + "'");
                                        }

                                        var sicData = currentParentIon.SICData;

                                        if (expectedSicDataCount > 0 && expectedSicDataCount != currentParentIon.SICData.Count)
                                        {
                                            errorMessages.Add("Actual SICDataCount (" + currentParentIon.SICData.Count + ") did not match expected count (" + expectedSicDataCount + ")");
                                        }

                                        // Split apart the smoothed Y data using the delimiters in delimiterList
                                        if (smoothedYDataList != null)
                                        {
                                            var valueList = smoothedYDataList.Trim().Split(delimiterList);

                                            for (var index = 0; index <= valueList.Length - 1; index++)
                                            {
                                                if (index >= sicData.Count)
                                                {
                                                    errorMessages.Add("Too many intensity data points found in parent ion '" + indexInXMLFile + "'");
                                                    break;
                                                }

                                                if (PRISM.DataUtils.StringToValueUtils.IsNumber(valueList[index]))
                                                {
                                                    currentParentIon.SICStats.SICSmoothedYData.Add(double.Parse(valueList[index]));
                                                    smoothedDataFound = true;
                                                }
                                                else
                                                {
                                                    currentParentIon.SICStats.SICSmoothedYData.Add(0.0);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // No smoothed Y data; that's OK
                                        }

                                        // Update the peak stats variables
                                        currentParentIon.SICIntensityMax = (from item in sicData select item.Intensity).Max();

                                        if (currentParentIon.SICStats.Peak.IndexBaseLeft < 0)
                                        {
                                            currentParentIon.SICStats.Peak.IndexBaseLeft = 0;
                                        }
                                        else if (currentParentIon.SICStats.Peak.IndexBaseLeft < sicData.Count)
                                        {
                                            if (peakScanStart != sicData[currentParentIon.SICStats.Peak.IndexBaseLeft].ScanNumber)
                                            {
                                                errorMessages.Add("PeakScanStart does not agree with SICPeakIndexStart for parent ion ' " + indexInXMLFile + "'");
                                            }
                                        }
                                        else
                                        {
                                            errorMessages.Add(".SICStats.Peak.IndexBaseLeft is larger than .SICScans.Length for parent ion ' " + indexInXMLFile + "'");
                                        }

                                        if (currentParentIon.SICStats.Peak.IndexBaseRight < 0)
                                        {
                                            currentParentIon.SICStats.Peak.IndexBaseRight = sicData.Count - 1;
                                        }
                                        else if (currentParentIon.SICStats.Peak.IndexBaseRight < sicData.Count)
                                        {
                                            if (peakScanEnd != sicData[currentParentIon.SICStats.Peak.IndexBaseRight].ScanNumber)
                                            {
                                                errorMessages.Add("PeakScanEnd does not agree with SICPeakIndexEnd for parent ion ' " + indexInXMLFile + "'");
                                            }
                                        }
                                        else
                                        {
                                            errorMessages.Add(".SICStats.Peak.IndexBaseRight is larger than .SICScans.Length for parent ion ' " + indexInXMLFile + "'");
                                        }

                                        if (currentParentIon.SICStats.Peak.IndexBaseRight < sicData.Count && currentParentIon.SICStats.Peak.IndexBaseLeft < sicData.Count)
                                        {
                                            currentParentIon.SICStats.SICPeakWidthFullScans = sicData[currentParentIon.SICStats.Peak.IndexBaseRight].ScanNumber - sicData[currentParentIon.SICStats.Peak.IndexBaseLeft].ScanNumber + 1;
                                        }

                                        if (!baselineNoiseStatsFound)
                                        {
                                            // Compute the Noise Threshold for this SIC
                                            // Note: We cannot use DualTrimmedMeanByAbundance since we don't have access to the full-length SICs
                                            if (mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode == clsMASICPeakFinder.eNoiseThresholdModes.DualTrimmedMeanByAbundance)
                                            {
                                                mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance;
                                            }

                                            mMASICPeakFinder.ComputeNoiseLevelInPeakVicinity(sicData, currentParentIon.SICStats.Peak, mSICPeakFinderOptions.SICBaselineNoiseOptions);
                                        }
                                    }

                                    validParentIon = false;
                                }
                            }
                        }

                        if (eCurrentXMLDataFileSection != eCurrentXMLDataFileSectionConstants.UnknownFile && objXMLReader.NodeType == XmlNodeType.Element)
                        {
                            switch (eCurrentXMLDataFileSection)
                            {
                                case eCurrentXMLDataFileSectionConstants.Options:
                                    try
                                    {
                                        switch (objXMLReader.Name)
                                        {
                                            case "SimilarIonMZToleranceHalfWidth":
                                                similarIonMZToleranceHalfWidth = float.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                break;
                                            case "FindPeaksOnSmoothedData":
                                                findPeaksOnSmoothedData = bool.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                break;
                                            case "UseButterworthSmooth":
                                                useButterworthSmooth = bool.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                break;
                                            case "ButterworthSamplingFrequency":
                                                butterworthSamplingFrequency = float.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                break;
                                            case "UseSavitzkyGolaySmooth":
                                                useSavitzkyGolaySmooth = bool.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                break;
                                            case "SavitzkyGolayFilterOrder":
                                                savitzkyGolayFilterOrder = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                break;
                                            default:
                                                // Ignore the setting
                                                break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // Ignore any errors looking up smoothing options
                                    }

                                    break;

                                case eCurrentXMLDataFileSectionConstants.ParentIons:
                                    if (validParentIon)
                                    {
                                        try
                                        {
                                            var ionStats = mParentIonStats[mParentIonStats.Count - 1];
                                            string value;
                                            switch (objXMLReader.Name)
                                            {
                                                case "MZ":
                                                    ionStats.MZ = Math.Round(double.Parse(XMLTextReaderGetInnerText(objXMLReader)), 6);
                                                    break;
                                                case "SurveyScanNumber":
                                                    ionStats.SurveyScanNumber = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "FragScanNumber":
                                                    ionStats.FragScanObserved = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "FragScanTime":
                                                    ionStats.FragScanTime = float.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "OptimalPeakApexScanNumber":
                                                    ionStats.OptimalPeakApexScanNumber = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "OptimalPeakApexScanTime":
                                                    ionStats.OptimalPeakApexTime = float.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "CustomSICPeak":
                                                    ionStats.CustomSICPeak = bool.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "CustomSICPeakComment":
                                                    ionStats.CustomSICPeakComment = XMLTextReaderGetInnerText(objXMLReader);
                                                    break;

                                                case "SICScanType":
                                                    var sicScanType = XMLTextReaderGetInnerText(objXMLReader);
                                                    // ReSharper disable once StringLiteralTypo
                                                    if ((sicScanType.ToLower() ?? "") == "fragscan")
                                                    {
                                                        ionStats.SICScanType = clsParentIonStats.eScanTypeConstants.FragScan;
                                                    }
                                                    else
                                                    {
                                                        ionStats.SICScanType = clsParentIonStats.eScanTypeConstants.SurveyScan;
                                                    }

                                                    break;
                                                case "PeakScanStart":
                                                    peakScanStart = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "PeakScanEnd":
                                                    peakScanEnd = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "PeakScanMaxIntensity":
                                                    ionStats.SICStats.ScanNumberMaxIntensity = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "PeakIntensity":
                                                    ionStats.SICStats.Peak.MaxIntensityValue = double.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "PeakSignalToNoiseRatio":
                                                    ionStats.SICStats.Peak.SignalToNoiseRatio = double.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "FWHMInScans":
                                                    ionStats.SICStats.Peak.FWHMScanWidth = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "PeakArea":
                                                    ionStats.SICStats.Peak.Area = double.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "ShoulderCount":
                                                    ionStats.SICStats.Peak.ShoulderCount = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;

                                                case "ParentIonIntensity":
                                                    ionStats.SICStats.Peak.ParentIonIntensity = double.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;

                                                case "PeakBaselineNoiseLevel":
                                                    ionStats.SICStats.Peak.BaselineNoiseStats.NoiseLevel = double.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    baselineNoiseStatsFound = true;
                                                    break;
                                                case "PeakBaselineNoiseStDev":
                                                    ionStats.SICStats.Peak.BaselineNoiseStats.NoiseStDev = double.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "PeakBaselinePointsUsed":
                                                    ionStats.SICStats.Peak.BaselineNoiseStats.PointsUsed = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "NoiseThresholdModeUsed":
                                                    ionStats.SICStats.Peak.BaselineNoiseStats.NoiseThresholdModeUsed = (clsMASICPeakFinder.eNoiseThresholdModes)int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;

                                                case "StatMomentsArea":
                                                    ionStats.SICStats.Peak.StatisticalMoments.Area = double.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "CenterOfMassScan":
                                                    ionStats.SICStats.Peak.StatisticalMoments.CenterOfMassScan = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "PeakStDev":
                                                    ionStats.SICStats.Peak.StatisticalMoments.StDev = double.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "PeakSkew":
                                                    ionStats.SICStats.Peak.StatisticalMoments.Skew = double.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "PeakKSStat":
                                                    ionStats.SICStats.Peak.StatisticalMoments.KSStat = double.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "StatMomentsDataCountUsed":
                                                    ionStats.SICStats.Peak.StatisticalMoments.DataCountUsed = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;

                                                case "SICScanStart":
                                                    scanStart = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "SICScanIntervals":
                                                    scanIntervals = XMLTextReaderGetInnerText(objXMLReader);
                                                    break;
                                                case "SICPeakIndexStart":
                                                    ionStats.SICStats.Peak.IndexBaseLeft = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;
                                                case "SICPeakIndexEnd":
                                                    ionStats.SICStats.Peak.IndexBaseRight = int.Parse(XMLTextReaderGetInnerText(objXMLReader));
                                                    break;

                                                case "SICDataCount":
                                                    value = XMLTextReaderGetInnerText(objXMLReader);
                                                    if (PRISM.DataUtils.StringToValueUtils.IsNumber(value))
                                                    {
                                                        expectedSicDataCount = int.Parse(value);
                                                    }
                                                    else
                                                    {
                                                        expectedSicDataCount = 0;
                                                    }

                                                    break;

                                                case "SICSmoothedYDataIndexStart":
                                                    value = XMLTextReaderGetInnerText(objXMLReader);
                                                    if (PRISM.DataUtils.StringToValueUtils.IsNumber(value))
                                                    {
                                                        ionStats.SICStats.SICSmoothedYDataIndexStart = int.Parse(value);
                                                    }
                                                    else
                                                    {
                                                        ionStats.SICStats.SICSmoothedYDataIndexStart = 0;
                                                    }

                                                    break;
                                                case "IntensityDataList":
                                                    intensityDataList = XMLTextReaderGetInnerText(objXMLReader);
                                                    break;
                                                case "MassDataList":
                                                    massDataList = XMLTextReaderGetInnerText(objXMLReader);
                                                    break;
                                                case "SmoothedYDataList":
                                                    smoothedYDataList = XMLTextReaderGetInnerText(objXMLReader);
                                                    break;
                                                default:
                                                    // Unknown child node name; ignore it
                                                    break;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            // Error parsing value from the ParentIon data
                                            errorMessages.Add("Error parsing value for parent ion '" + indexInXMLFile + "'");
                                            validParentIon = false;
                                        }
                                    }

                                    break;
                            }
                        }
                    }
                }

                // For each parent ion, find the other nearby parent ions with similar m/z values
                // Use the tolerance specified by similarIonMZToleranceHalfWidth, though with a minimum value of 0.1
                if (similarIonMZToleranceHalfWidth < 0.1)
                {
                    similarIonMZToleranceHalfWidth = 0.1F;
                }

                FindSimilarParentIon(similarIonMZToleranceHalfWidth * 2);

                if (eCurrentXMLDataFileSection == eCurrentXMLDataFileSectionConstants.UnknownFile)
                {
                    MessageBox.Show("Root element 'SICData' not found in the input file: " + Environment.NewLine + filePath, "Invalid File Format", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                else
                {
                    // Set the smoothing options
                    if (!findPeaksOnSmoothedData)
                    {
                        optDoNotResmooth.Checked = true;
                    }
                    else if (useButterworthSmooth || !useSavitzkyGolaySmooth)
                    {
                        optUseButterworthSmooth.Checked = true;
                        txtButterworthSamplingFrequency.Text = butterworthSamplingFrequency.ToString();
                    }
                    else
                    {
                        optUseSavitzkyGolaySmooth.Checked = true;
                        txtSavitzkyGolayFilterOrder.Text = savitzkyGolayFilterOrder.ToString();
                    }

                    if (smoothedDataFound)
                    {
                        optDoNotResmooth.Text = "Do Not Resmooth";
                    }
                    else
                    {
                        optDoNotResmooth.Text = "Do Not Show Smoothed Data";
                    }

                    // Inform the user if any errors occurred
                    if (errorMessages.Count > 0)
                    {
                        MessageBox.Show(string.Join(Environment.NewLine, errorMessages.Take(15)), "Invalid Lines", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }

                    // Sort the data
                    SortData();

                    // Select the first item in lstParentIonData
                    if (lstParentIonData.Items.Count > 0)
                    {
                        lstParentIonData.SelectedIndex = 0;
                    }

                    if (objProgress.KeyPressAbortProcess)
                    {
                        MessageBox.Show("Load cancelled before all of the data was read", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else
                    {
                        AutoOpenMsMsResults(Path.GetFullPath(filePath), objProgress);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to read the input file: " + filePath + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally
            {
                objProgress?.HideForm();
            }
        }

        private void ReadMsMsSearchEngineResults(string filePath, frmProgress objProgress)
        {
            var chSepChars = new char[] { '\t' };

            long bytesRead = 0;

            var createdNewProgressForm = false;

            try
            {
                var dataFileInfo = new FileInfo(filePath);
                var fileSizeBytes = dataFileInfo.Length;

                using (var reader = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    if (objProgress == null)
                    {
                        objProgress = new frmProgress();
                        createdNewProgressForm = true;
                    }

                    objProgress.InitializeProgressForm("Reading MS/MS Search Engine Results ", 0, fileSizeBytes, true);
                    objProgress.Show();
                    objProgress.BringToFront();
                    Application.DoEvents();

                    if (mMsMsResults.Tables[TABLE_NAME_MSMS_RESULTS].Rows.Count > 0 || mMsMsResults.Tables[TABLE_NAME_SEQ_TO_PROTEIN_MAP].Rows.Count > 0 || mMsMsResults.Tables[TABLE_NAME_SEQUENCES].Rows.Count > 0)
                    {
                        ClearMsMsResults();
                    }

                    var linesRead = 0;
                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        linesRead += 1;

                        if (dataLine != null)
                        {
                            bytesRead += dataLine.Length + 2;

                            if (linesRead % 50 == 0)
                            {
                                objProgress.UpdateProgressBar(bytesRead);
                                Application.DoEvents();
                                if (objProgress.KeyPressAbortProcess)
                                    break;
                            }

                            var dataCols = dataLine.Trim().Split(chSepChars);

                            if (dataCols.Length >= 13)
                            {
                                var sequenceID = LookupSequenceID(dataCols[(int)eMsMsSearchEngineResultColumns.Sequence], dataCols[(int)eMsMsSearchEngineResultColumns.Protein]);

                                if (sequenceID >= 0)
                                {
                                    try
                                    {
                                        var scanNumber = int.Parse(dataCols[(int)eMsMsSearchEngineResultColumns.Scan]);
                                        var charge = int.Parse(dataCols[(int)eMsMsSearchEngineResultColumns.Charge]);

                                        var msMsResultsTable = mMsMsResults.Tables[TABLE_NAME_MSMS_RESULTS];
                                        if (!msMsResultsTable.Rows.Contains(new object[] { scanNumber, charge, sequenceID }))
                                        {
                                            var objNewRow = mMsMsResults.Tables[TABLE_NAME_MSMS_RESULTS].NewRow();
                                            objNewRow[COL_NAME_SCAN] = scanNumber;
                                            objNewRow[COL_NAME_CHARGE] = charge;
                                            objNewRow[COL_NAME_MH] = dataCols[(int)eMsMsSearchEngineResultColumns.MH];
                                            objNewRow[COL_NAME_XCORR] = dataCols[(int)eMsMsSearchEngineResultColumns.XCorr];
                                            objNewRow[COL_NAME_DELTACN] = dataCols[(int)eMsMsSearchEngineResultColumns.DeltaCN];
                                            objNewRow[COL_NAME_DELTACN2] = dataCols[(int)eMsMsSearchEngineResultColumns.DeltaCn2];
                                            objNewRow[COL_NAME_RANK_SP] = dataCols[(int)eMsMsSearchEngineResultColumns.RankSp];
                                            objNewRow[COL_NAME_RANK_XC] = dataCols[(int)eMsMsSearchEngineResultColumns.RankXc];
                                            objNewRow[COL_NAME_SEQUENCE_ID] = sequenceID;
                                            objNewRow[COL_NAME_PARENT_ION_INDEX] = -1;

                                            mMsMsResults.Tables[TABLE_NAME_MSMS_RESULTS].Rows.Add(objNewRow);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // Error parsing/adding row
                                        Console.WriteLine("Error reading data from MS/MS Search Results file: " + ex.Message);
                                    }
                                }
                            }
                        }
                    }
                }

                // Populate column .Item(COL_NAME_PARENT_ION_INDEX)
                PopulateParentIonIndexColumnInMsMsResultsTable();

                txtStats3.Visible = true;
                PositionControls();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in ReadMsMsSearchEngineResults: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally
            {
                if (createdNewProgressForm)
                {
                    objProgress.HideForm();
                }
            }
        }

        private void RedoSICPeakFindingAllData()
        {
            const eSmoothModeConstants SMOOTH_MODE = eSmoothModeConstants.Butterworth;

            var objProgress = new frmProgress();

            try
            {
                cmdRedoSICPeakFindingAllData.Enabled = false;

                objProgress.InitializeProgressForm("Repeating SIC peak finding ", 0, mParentIonStats.Count, true);
                objProgress.Show();
                Application.DoEvents();

                UpdateSICPeakFinderOptions();

                for (var parentIonIndex = 0; parentIonIndex <= mParentIonStats.Count - 1; parentIonIndex++)
                {
                    var validPeakFound = UpdateSICStats(parentIonIndex, true, SMOOTH_MODE, out var sicStats);

                    if (validPeakFound)
                    {
                        mParentIonStats[parentIonIndex].SICStats = sicStats;
                    }

                    objProgress.UpdateProgressBar(parentIonIndex + 1);
                    Application.DoEvents();
                    if (objProgress.KeyPressAbortProcess)
                        break;
                }

                SortData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in RedoSICPeakFindingAllData: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally
            {
                objProgress?.HideForm();

                cmdRedoSICPeakFindingAllData.Enabled = true;
            }
        }

        private void RegReadSettings()
        {
            // Load settings from the registry
            try
            {
                txtDataFilePath.Text = Interaction.GetSetting(REG_APP_NAME, REG_SECTION_NAME, "DataFilePath", string.Empty);

                Width = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "WindowSizeWidth", Width);
                // Me.Height = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "WindowSizeHeight", Me.Height)
                Height = 700;

                Top = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "WindowPosTop", Top);
                Left = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "WindowPosLeft", Left);

                cboSortOrder.SelectedIndex = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "SortOrder", cboSortOrder.SelectedIndex);
                chkSortDescending.Checked = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "SortDescending", chkSortDescending.Checked);

                txtFixXRange.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FixXRange", 300).ToString();
                txtFixYRange.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FixYRange", 5000000).ToString();
                txtMinimumSignalToNoise.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "MinimumSignalToNoise", 3).ToString();

                chkFixXRange.Checked = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FixXRangeEnabled", true);
                chkFixYRange.Checked = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FixYRangeEnabled", false);
                chkFilterBySignalToNoise.Checked = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FilterBySignalToNoise", false);

                txtMinimumIntensity.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "MinimumIntensity", 1000000).ToString();
                txtFilterByMZ.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FilterByMZ", (float)550).ToString(CultureInfo.InvariantCulture);
                txtFilterByMZTol.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FilterByMZTol", (float)0.2).ToString(CultureInfo.InvariantCulture);

                txtAutoStep.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "AutoStepInterval", 150).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in RegReadSettings: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void RegSaveSettings()
        {

            // Save settings to the registry
            try
            {
                if (txtDataFilePath.Text.Length > 0)
                {
                    Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "DatafilePath", txtDataFilePath.Text);
                    Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "WindowSizeWidth", Width.ToString());
                    Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "WindowSizeHeight", Height.ToString());
                    Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "WindowPosTop", Top.ToString());
                    Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "WindowPosLeft", Left.ToString());

                    Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "SortOrder", cboSortOrder.SelectedIndex.ToString());
                    Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "SortDescending", chkSortDescending.Checked.ToString());

                    if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtFixXRange.Text))
                    {
                        Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FixXRange", int.Parse(txtFixXRange.Text).ToString());
                    }

                    if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtFixYRange.Text))
                    {
                        Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FixYRange", long.Parse(txtFixYRange.Text).ToString());
                    }

                    if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtMinimumSignalToNoise.Text))
                    {
                        Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "MinimumSignalToNoise", float.Parse(txtMinimumSignalToNoise.Text).ToString(CultureInfo.InvariantCulture));
                    }

                    if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtMinimumIntensity.Text))
                    {
                        Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "MinimumIntensity", int.Parse(txtMinimumIntensity.Text).ToString());
                    }

                    if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtFilterByMZ.Text))
                    {
                        Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FilterByMZ", float.Parse(txtFilterByMZ.Text).ToString(CultureInfo.InvariantCulture));
                    }

                    if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtFilterByMZTol.Text))
                    {
                        Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FilterByMZTol", float.Parse(txtFilterByMZTol.Text).ToString(CultureInfo.InvariantCulture));
                    }

                    if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtAutoStep.Text))
                    {
                        Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "AutoStepInterval", int.Parse(txtAutoStep.Text).ToString());
                    }

                    Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FixXRangeEnabled", chkFixXRange.Checked.ToString());
                    Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FixYRangeEnabled", chkFixYRange.Checked.ToString());
                    Interaction.SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FilterBySignalToNoise", chkFilterBySignalToNoise.Checked.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in RegSaveSettings: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void SelectMASICInputFile()
        {
            var dlgOpenFile = new OpenFileDialog
            {
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                FilterIndex = 1,
                Title = "Select MASIC Results File",
            };

            if (txtDataFilePath.Text.Length > 0)
            {
                try
                {
                    dlgOpenFile.InitialDirectory = Directory.GetParent(txtDataFilePath.Text).ToString();
                }
                catch
                {
                    dlgOpenFile.InitialDirectory = Application.StartupPath;
                }
            }
            else
            {
                dlgOpenFile.InitialDirectory = Application.StartupPath;
            }

            dlgOpenFile.ShowDialog();

            if (dlgOpenFile.FileName.Length > 0)
            {
                txtDataFilePath.Text = dlgOpenFile.FileName;
                ReadDataFileXMLTextReader(dlgOpenFile.FileName);

                //ReadDataFileXML(dlgOpenFile.FileName);
            }

            PositionControls();
        }

        private void SelectMsMsSearchResultsInputFile()
        {
            var dlgOpenFile = new OpenFileDialog
            {
                Filter = "First Hits Files(*_fht.txt)|*_fht.txt|Synopsis Hits Files(*_syn.txt)|*_syn.txt|All files (*.*)|*.*",
                FilterIndex = 1,
                Title = "Select MS/MS Search Engine Results File",
            };

            if (txtDataFilePath.Text.Length > 0)
            {
                try
                {
                    dlgOpenFile.InitialDirectory = Directory.GetParent(txtDataFilePath.Text).ToString();
                }
                catch
                {
                    dlgOpenFile.InitialDirectory = Application.StartupPath;
                }
            }
            else
            {
                dlgOpenFile.InitialDirectory = Application.StartupPath;
            }

            dlgOpenFile.ShowDialog();
            if (dlgOpenFile.FileName.Length > 0)
            {
                ReadMsMsSearchEngineResults(dlgOpenFile.FileName, null);
            }

            PositionControls();
        }

        private void SetToolTips()
        {
            var objToolTipControl = new ToolTip();

            objToolTipControl.SetToolTip(txtButterworthSamplingFrequency, "Value between 0.01 and 0.99; suggested value is 0.20");
            objToolTipControl.SetToolTip(txtSavitzkyGolayFilterOrder, "Even number, 0 or greater; 0 means a moving average filter, 2 means a 2nd order Savitzky Golay filter");
        }

        private void ShowAboutBox()
        {
            var message = new List<string>();

            message.Add("The MASIC Browser can be used to visualize the SIC's created using MASIC.");
            message.Add("");

            message.Add("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2003");
            message.Add("Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.");
            message.Add("");

            message.Add("This is version " + Application.ProductVersion + " (" + PROGRAM_DATE + ")");
            message.Add("");

            message.Add("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
            message.Add("Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/");
            message.Add("");

            message.Add("Licensed under the Apache License, Version 2.0; you may not use this file except in compliance with the License. ");
            message.Add("You may obtain a copy of the License at https://www.apache.org/licenses/LICENSE-2.0");
            message.Add("");

            message.Add("Notice: This computer software was prepared by Battelle Memorial Institute, " +
                        "hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the " +
                        "Department of Energy (DOE).  All rights in the computer software are reserved " +
                        "by DOE on behalf of the United States Government and the Contractor as " +
                        "provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY " +
                        "WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS " +
                        "SOFTWARE.  This notice including this sentence must appear on any copies of " +
                        "this computer software.");

            MessageBox.Show(string.Join(Environment.NewLine, message), "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SortData()
        {
            eSortOrderConstants eSortMode;

            var scanNumberSaved = 0;

            double minimumIntensity;
            double mzFilter, mzFilterTol;
            double minimumSN;

            if (mParentIonStats.Count <= 0)
            {
                mParentIonPointerArray = new int[0];
                return;
            }

            try
            {
                if (lstParentIonData.SelectedIndex >= 0)
                {
                    scanNumberSaved = mParentIonStats[mParentIonPointerArray[lstParentIonData.SelectedIndex]].FragScanObserved;
                }
            }
            catch (Exception ex)
            {
                scanNumberSaved = 1;
            }

            mParentIonPointerArrayCount = 0;
            mParentIonPointerArray = new int[mParentIonStats.Count];
            var sortKeys = new double[mParentIonStats.Count];

            if (cboSortOrder.SelectedIndex >= 0 && cboSortOrder.SelectedIndex < SORT_ORDER_MODE_COUNT)
            {
                eSortMode = (eSortOrderConstants)cboSortOrder.SelectedIndex;
            }
            else
            {
                eSortMode = eSortOrderConstants.SortByScanPeakCenter;
            }

            if (chkFilterByIntensity.Checked && PRISM.DataUtils.StringToValueUtils.IsNumber(txtMinimumIntensity.Text))
            {
                minimumIntensity = double.Parse(txtMinimumIntensity.Text);
            }
            else
            {
                minimumIntensity = double.MinValue;
            }

            if (chkFilterByMZ.Checked && PRISM.DataUtils.StringToValueUtils.IsNumber(txtFilterByMZ.Text) &&
                PRISM.DataUtils.StringToValueUtils.IsNumber(txtFilterByMZTol.Text))
            {
                mzFilter = Math.Abs(double.Parse(txtFilterByMZ.Text));
                mzFilterTol = Math.Abs(double.Parse(txtFilterByMZTol.Text));
            }
            else
            {
                mzFilter = -1;
                mzFilterTol = double.MaxValue;
            }

            if (chkFilterBySignalToNoise.Checked && PRISM.DataUtils.StringToValueUtils.IsNumber(txtMinimumSignalToNoise.Text))
            {
                minimumSN = double.Parse(txtMinimumSignalToNoise.Text);
            }
            else
            {
                minimumSN = double.MinValue;
            }

            switch (eSortMode)
            {
                case eSortOrderConstants.SortByPeakIndex:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].Index;
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByScanPeakCenter:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.ScanNumberMaxIntensity;
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByScanOptimalPeakCenter:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = double.Parse(mParentIonStats[index].OptimalPeakApexScanNumber + "." + Math.Round(mParentIonStats[index].MZ, 0).ToString("0000") + mParentIonStats[index].Index.ToString("00000"));
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByMz:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = double.Parse(Math.Round(mParentIonStats[index].MZ, 2) + mParentIonStats[index].SICStats.ScanNumberMaxIntensity.ToString("000000"));
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByPeakSignalToNoise:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.Peak.SignalToNoiseRatio;
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByBaselineCorrectedPeakIntensity:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = clsMASICPeakFinder.BaselineAdjustIntensity(mParentIonStats[index].SICStats.Peak, true);

                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByBaselineCorrectedPeakArea:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            {
                                var sicStats = mParentIonStats[index].SICStats;
                                sortKeys[mParentIonPointerArrayCount] = clsMASICPeakFinder.BaselineAdjustArea(sicStats.Peak, sicStats.SICPeakWidthFullScans, true);
                            }

                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByPeakWidth:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            // Create a sort key that is based on both PeakFWHMScans and ScanNumberMaxIntensity by separating the two integers with a "."
                            sortKeys[mParentIonPointerArrayCount] = GetSortKey(mParentIonStats[index].SICStats.Peak.FWHMScanWidth,
                                                                               mParentIonStats[index].SICStats.ScanNumberMaxIntensity);
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortBySICIntensityMax:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = Math.Round(mParentIonStats[index].SICIntensityMax, 0);
                            // Append the scan number so that we sort by intensity, then scan
                            if (chkSortDescending.Checked)
                            {
                                sortKeys[mParentIonPointerArrayCount] += 1 - mParentIonStats[index].FragScanObserved / 1000000.0;
                            }
                            else
                            {
                                sortKeys[mParentIonPointerArrayCount] += mParentIonStats[index].FragScanObserved / 1000000.0;
                            }

                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByPeakIntensity:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.Peak.MaxIntensityValue;
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByPeakArea:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.Peak.Area;
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByFragScanToOptimalLocDistance:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            // Create a sort key that is based on both FragScan-OptimalPeakApexScanNumber and OptimalPeakApexScanNumber by separating the two integers with a "."
                            sortKeys[mParentIonPointerArrayCount] = GetSortKey(mParentIonStats[index].FragScanObserved - mParentIonStats[index].OptimalPeakApexScanNumber,
                                                                               mParentIonStats[index].OptimalPeakApexScanNumber);
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByPeakCenterToOptimalLocDistance:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            // Create a sort key that is based on both ScanNumberMaxIntensity-OptimalPeakApexScanNumber and OptimalPeakApexScanNumber by separating the two integers with a "."
                            sortKeys[mParentIonPointerArrayCount] = GetSortKey(mParentIonStats[index].SICStats.ScanNumberMaxIntensity - mParentIonStats[index].OptimalPeakApexScanNumber,
                                                                               mParentIonStats[index].OptimalPeakApexScanNumber);
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByShoulderCount:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            // Create a sort key that is based on both ShoulderCount and OptimalPeakApexScanNumber by separating the two integers with a "."
                            sortKeys[mParentIonPointerArrayCount] = GetSortKey(mParentIonStats[index].SICStats.Peak.ShoulderCount,
                                                                               mParentIonStats[index].OptimalPeakApexScanNumber);
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByParentIonIntensity:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.Peak.ParentIonIntensity;
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByPeakSkew:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.Peak.StatisticalMoments.Skew;
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByKSStat:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.Peak.StatisticalMoments.KSStat;
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
                case eSortOrderConstants.SortByBaselineNoiseLevel:
                    for (var index = 0; index <= mParentIonStats.Count - 1; index++)
                    {
                        var ionStats = mParentIonStats[index];
                        if (SortDataFilterCheck(ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = GetSortKey((int)Math.Round(mParentIonStats[index].SICStats.Peak.BaselineNoiseStats.NoiseLevel, 0),
                                                                               mParentIonStats[index].SICStats.Peak.SignalToNoiseRatio);
                            mParentIonPointerArrayCount += 1;
                        }
                    }

                    break;
            }

            if (mParentIonPointerArrayCount < mParentIonStats.Count)
            {
                var oldSortKeys = sortKeys;
                sortKeys = new double[mParentIonPointerArrayCount];
                Array.Copy(oldSortKeys, sortKeys, Math.Min(mParentIonPointerArrayCount, oldSortKeys.Length));
                var oldMParentIonPointerArray = mParentIonPointerArray;
                mParentIonPointerArray = new int[mParentIonPointerArrayCount];
                Array.Copy(oldMParentIonPointerArray, mParentIonPointerArray, Math.Min(mParentIonPointerArrayCount, oldMParentIonPointerArray.Length));
            }

            // Sort mParentIonPointerArray
            Array.Sort(sortKeys, mParentIonPointerArray);

            if (chkSortDescending.Checked)
            {
                Array.Reverse(mParentIonPointerArray);
            }

            PopulateSpectrumList(scanNumberSaved);
        }

        private bool SortDataFilterCheck(
            double peakMaxIntensityValue,
            double peakSN,
            double peakMZ,
            double minimumIntensity,
            double minimumSN,
            double mzFilter,
            double mzFilterTol,
            bool isCustomSIC)
        {
            bool useData;

            if (cboSICsTypeFilter.SelectedIndex == (int)eSICTypeFilterConstants.CustomSICsOnly)
            {
                useData = isCustomSIC;
            }
            else if (cboSICsTypeFilter.SelectedIndex == (int)eSICTypeFilterConstants.NoCustomSICs)
            {
                useData = !isCustomSIC;
            }
            else
            {
                useData = true;
            }

            if (useData && peakMaxIntensityValue >= minimumIntensity)
            {
                if (peakSN >= minimumSN)
                {
                    if (mzFilter >= 0)
                    {
                        if (Math.Abs(peakMZ - mzFilter) <= mzFilterTol)
                        {
                            useData = true;
                        }
                        else
                        {
                            useData = false;
                        }
                    }
                    else
                    {
                        useData = true;
                    }
                }
                else
                {
                    useData = false;
                }
            }
            else
            {
                useData = false;
            }

            return useData;
        }

        private void TestValueToString()
        {
            var results = string.Empty;
            for (byte digits = 1; digits <= 5; digits++)
            {
                results += this.TestValueToStringWork(0.00001234F, digits) + Environment.NewLine;
                results += this.TestValueToStringWork(0.01234F, digits) + Environment.NewLine;
                results += this.TestValueToStringWork(0.1234F, digits) + Environment.NewLine;
                results += this.TestValueToStringWork(0.123F, digits) + Environment.NewLine;
                results += this.TestValueToStringWork(0.12F, digits) + Environment.NewLine;
                results += this.TestValueToStringWork(1.234F, digits) + Environment.NewLine;
                results += this.TestValueToStringWork(12.34F, digits) + Environment.NewLine;
                results += this.TestValueToStringWork(123.4F, digits) + Environment.NewLine;
                results += TestValueToStringWork(1234, digits) + Environment.NewLine;
                results += TestValueToStringWork(12340, digits) + Environment.NewLine;
                results += Environment.NewLine;
            }

            MessageBox.Show(results);
        }

        private string TestValueToStringWork(float value, byte digitsOfPrecision)
        {
            return value + ": " + StringUtilities.ValueToString(value, digitsOfPrecision);
        }

        private void ToggleAutoStep(bool forceDisabled = false)
        {
            if (mAutoStepEnabled || forceDisabled)
            {
                mAutoStepEnabled = false;
                cmdAutoStep.Text = "&Auto Step";
            }
            else
            {
                if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtAutoStep.Text))
                {
                    mAutoStepIntervalMsec = int.Parse(txtAutoStep.Text);
                }
                else
                {
                    txtAutoStep.Text = "150";
                    mAutoStepIntervalMsec = 150;
                }

                if (lstParentIonData.SelectedIndex == 0 && !chkAutoStepForward.Checked)
                {
                    chkAutoStepForward.Checked = true;
                }
                else if (lstParentIonData.SelectedIndex == mParentIonPointerArrayCount - 1 && chkAutoStepForward.Checked)
                {
                    chkAutoStepForward.Checked = false;
                }

                mLastUpdate = DateTime.Now;
                mAutoStepEnabled = true;
                cmdAutoStep.Text = "Stop &Auto";
            }
        }

        private void UpdateSICPeakFinderOptions()
        {
            //mSICPeakFinderOptions.IntensityThresholdFractionMax=
            //mSICPeakFinderOptions.IntensityThresholdAbsoluteMinimum =

            //var sicNoiseThresholdOpts =  mSICPeakFinderOptions.SICNoiseThresholdOptions;
            //sicNoiseThresholdOpts.NoiseThresholdMode =
            //sicNoiseThresholdOpts.NoiseThresholdIntensity =
            //sicNoiseThresholdOpts.NoiseFractionLowIntensityDataToAverage =
            //sicNoiseThresholdOpts.MinimumSignalToNoiseRatio =
            //sicNoiseThresholdOpts.ExcludePeakDataFromNoiseComputation =
            //sicNoiseThresholdOpts.MinimumNoiseThresholdLevel =

            //mSICPeakFinderOptions.MaxDistanceScansNoOverlap =
            //mSICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax =
            //mSICPeakFinderOptions.InitialPeakWidthScansScaler =
            //mSICPeakFinderOptions.InitialPeakWidthScansMaximum =

            if (optDoNotResmooth.Checked)
            {
                mSICPeakFinderOptions.FindPeaksOnSmoothedData = false;
            }
            else
            {
                mSICPeakFinderOptions.FindPeaksOnSmoothedData = true;
            }

            //mSICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth =
            if (optUseSavitzkyGolaySmooth.Checked)
            {
                mSICPeakFinderOptions.UseButterworthSmooth = false;
                mSICPeakFinderOptions.UseSavitzkyGolaySmooth = true;
                mSICPeakFinderOptions.SavitzkyGolayFilterOrder = (short)PRISMWin.TextBoxUtils.ParseTextBoxValueInt(txtSavitzkyGolayFilterOrder, "", out _);
            }
            else
            {
                mSICPeakFinderOptions.UseButterworthSmooth = true;
                mSICPeakFinderOptions.UseSavitzkyGolaySmooth = false;
                mSICPeakFinderOptions.ButterworthSamplingFrequency = PRISMWin.TextBoxUtils.ParseTextBoxValueFloat(txtButterworthSamplingFrequency, "", out _, 0.25F);
            }
            //mSICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData =

            //var msNoiseThresholdOpts = mSICPeakFinderOptions.MassSpectraNoiseThresholdOptions;
            //msNoiseThresholdOpts.NoiseThresholdMode =
            //msNoiseThresholdOpts.NoiseThresholdIntensity =
            //msNoiseThresholdOpts.NoiseFractionLowIntensityDataToAverage =
            //msNoiseThresholdOpts.MinimumSignalToNoiseRatio =
            //msNoiseThresholdOpts.ExcludePeakDataFromNoiseComputation =
            //msNoiseThresholdOpts.MinimumNoiseThresholdLevel =
        }

        private bool UpdateSICStats(int parentIonIndex, bool repeatPeakFinding, eSmoothModeConstants eSmoothMode, out clsSICStats sicStats)
        {
            // Copy the original SIC stats found by MASIC into udtSICStats
            // This also includes the original smoothed data

            // Copy the cached SICStats data into udtSICStats
            // We have to separately copy SICSmoothedYData() otherwise VB.NET keeps
            // the array linked in both mParentIonStats().SICStats and udtSICStats
            sicStats = mParentIonStats[parentIonIndex].SICStats.Clone();

            if (eSmoothMode != eSmoothModeConstants.DoNotReSmooth)
            {
                // Re-smooth the data
                var objFilter = new DataFilter.DataFilter();

                var currentParentIon = mParentIonStats[parentIonIndex];

                sicStats.SICSmoothedYDataIndexStart = 0;

                var intensities = (from item in currentParentIon.SICData select item.Intensity).ToArray();

                if (eSmoothMode == eSmoothModeConstants.SavitzkyGolay)
                {
                    // Resmooth using a Savitzky Golay filter

                    var savitzkyGolayFilterOrder = PRISMWin.TextBoxUtils.ParseTextBoxValueInt(txtSavitzkyGolayFilterOrder, lblSavitzkyGolayFilterOrder.Text + " should be an even number between 0 and 20; assuming 0", out _, 0);
                    var peakWidthsPointsMinimum = PRISMWin.TextBoxUtils.ParseTextBoxValueInt(txtPeakWidthPointsMinimum, lblPeakWidthPointsMinimum.Text + " should be a positive integer; assuming 6", out _, 6);

                    var filterThirdWidth = (int)Math.Floor(peakWidthsPointsMinimum / 3.0);
                    if (filterThirdWidth > 3)
                        filterThirdWidth = 3;

                    // Make sure filterThirdWidth is Odd
                    if (filterThirdWidth % 2 == 0)
                    {
                        filterThirdWidth -= 1;
                    }

                    var errorMessage = "";

                    // Note that the SavitzkyGolayFilter doesn't work right for PolynomialDegree values greater than 0
                    // Also note that a PolynomialDegree value of 0 results in the equivalent of a moving average filter
                    objFilter.SavitzkyGolayFilter(intensities,
                                                  0, currentParentIon.SICData.Count - 1,
                                                  filterThirdWidth, filterThirdWidth,
                                                  (short)savitzkyGolayFilterOrder, out errorMessage, true);
                }
                else
                {
                    // Assume eSmoothMode = eSmoothModeConstants.Butterworth
                    var samplingFrequency = PRISMWin.TextBoxUtils.ParseTextBoxValueFloat(txtButterworthSamplingFrequency, lblButterworthSamplingFrequency.Text + " should be a number between 0.01 and 0.99; assuming 0.2", out _, 0.2F);
                    objFilter.ButterworthFilter(intensities, 0, currentParentIon.SICData.Count - 1, samplingFrequency);
                }

                // Copy the smoothed data into udtSICStats.SICSmoothedYData
                sicStats.SICSmoothedYData.Clear();

                for (var index = 0; index <= intensities.Length - 1; index++)
                    sicStats.SICSmoothedYData.Add(intensities[index]);
            }

            if (repeatPeakFinding)
            {
                // Repeat the finding of the peak in the SIC
                var validPeakFound = FindSICPeakAndAreaForParentIon(parentIonIndex, sicStats);
                return validPeakFound;
            }
            else
            {
                return true;
            }
        }

        private void UpdateStatsAndPlot()
        {
            if (mParentIonPointerArrayCount > 0)
            {
                DisplaySICStats(mParentIonPointerArray[lstParentIonData.SelectedIndex], out var sicStats);
                PlotData(mParentIonPointerArray[lstParentIonData.SelectedIndex], sicStats);
            }
        }

        private string XMLTextReaderGetInnerText(XmlTextReader objXMLReader)
        {
            var value = string.Empty;
            bool success;

            if (objXMLReader.NodeType == XmlNodeType.Element)
            {
                // Advance the reader so that we can read the value
                success = objXMLReader.Read();
            }
            else
            {
                success = true;
            }

            if ((success && objXMLReader.NodeType != XmlNodeType.Whitespace) && objXMLReader.HasValue)
            {
                value = objXMLReader.Value;
            }

            return value;
        }

        private void XMLTextReaderSkipWhitespace(XmlTextReader objXMLReader)
        {
            if (objXMLReader.NodeType == XmlNodeType.Whitespace)
            {
                // Whitespace; read the next node
                objXMLReader.Read();
            }
        }

        #region "Checkboxes"
        private void chkFilterByIntensity_CheckedChanged(object sender, EventArgs e)
        {
            SortData();
        }

        private void chkFilterByMZ_CheckedChanged(object sender, EventArgs e)
        {
            SortData();
        }

        private void chkFilterBySignalToNoise_CheckedChanged(object sender, EventArgs e)
        {
            SortData();
        }

        private void chkFixXRange_CheckedChanged(object sender, EventArgs e)
        {
            UpdateStatsAndPlot();
        }

        private void chkFixYRange_CheckedChanged(object sender, EventArgs e)
        {
            UpdateStatsAndPlot();
        }

        private void cmdRedoSICPeakFindingAllData_Click(object sender, EventArgs e)
        {
            RedoSICPeakFindingAllData();
        }

        private void chkShowBaselineCorrectedStats_CheckedChanged(object sender, EventArgs e)
        {
            DisplaySICStatsForSelectedParentIon();
        }

        private void chkSortDescending_CheckedChanged(object sender, EventArgs e)
        {
            SortData();
        }

        private void chkShowSmoothedData_CheckedChanged(object sender, EventArgs e)
        {
            UpdateStatsAndPlot();
        }

        private void chkUsePeakFinder_CheckedChanged(object sender, EventArgs e)
        {
            UpdateStatsAndPlot();
        }
        #endregion

        #region "Command Buttons"
        private void cmdAutoStep_Click(object sender, EventArgs e)
        {
            ToggleAutoStep();
        }

        private void cmdNext_Click(object sender, EventArgs e)
        {
            NavigateScanList(true);
        }

        private void cmdPrevious_Click(object sender, EventArgs e)
        {
            NavigateScanList(false);
        }

        private void cmdSelectFile_Click(object sender, EventArgs e)
        {
            SelectMASICInputFile();
        }

        private void cmdJump_Click(object sender, EventArgs e)
        {
            JumpToScan();
        }
        #endregion

        #region "ListBoxes and Comboboxes"
        private void lstParentIonData_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateStatsAndPlot();
        }

        private void cboSICsTypeFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            SortData();
        }

        private void cboSortOrder_SelectedIndexChanged(object sender, EventArgs e)
        {
            DefineDefaultSortDirection();
            SortData();
            lstParentIonData.Focus();
        }

        #endregion

        #region "Option buttons"
        private void optDoNotResmooth_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
            UpdateStatsAndPlot();
        }

        private void optUseButterworthSmooth_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
            UpdateStatsAndPlot();
        }

        private void optUseSavitzkyGolaySmooth_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
            UpdateStatsAndPlot();
        }
        #endregion

        #region "Textboxes"

        private void txtAutoStep_TextChanged(object sender, EventArgs e)
        {
            if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtAutoStep.Text))
            {
                var newInterval = int.Parse(txtAutoStep.Text);
                if (newInterval < 10)
                    newInterval = 10;
                mAutoStepIntervalMsec = newInterval;
            }
        }

        private void txtAutoStep_Validating(object sender, CancelEventArgs e)
        {
            PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtAutoStep, 10, 9999, 150);
        }

        private void txtButterworthSamplingFrequency_TextChanged(object sender, EventArgs e)
        {
            UpdateStatsAndPlot();
        }

        private void txtButterworthSamplingFrequency_Validating(object sender, CancelEventArgs e)
        {
            PRISMWin.TextBoxUtils.ValidateTextBoxFloat(txtButterworthSamplingFrequency, 0.01F, 0.99F, 0.2F);
        }

        private void txtFilterByMZ_Leave(object sender, EventArgs e)
        {
            if (chkFilterByMZ.Checked)
                SortData();
        }

        private void txtFilterByMZ_Validating(object sender, CancelEventArgs e)
        {
            PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtFilterByMZ, 0, 100000, 540);
        }

        private void txtFilterByMZTol_Leave(object sender, EventArgs e)
        {
            if (chkFilterByMZ.Checked)
                SortData();
        }

        private void txtFilterByMZTol_Validating(object sender, CancelEventArgs e)
        {
            PRISMWin.TextBoxUtils.ValidateTextBoxFloat(txtFilterByMZTol, 0, 100000, 0.2F);
        }

        private void txtFixXRange_TextChanged(object sender, EventArgs e)
        {
            UpdateStatsAndPlot();
        }

        private void txtFixXRange_Validating(object sender, CancelEventArgs e)
        {
            PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtFixXRange, 3, 500000, 100);
        }

        private void txtFixYRange_TextChanged(object sender, EventArgs e)
        {
            UpdateStatsAndPlot();
        }

        private void txtFixYRange_Validating(object sender, CancelEventArgs e)
        {
            PRISMWin.TextBoxUtils.ValidateTextBoxFloat(txtFixYRange, 10, long.MaxValue, 5000000);
        }

        private void txtMinimumIntensity_Leave(object sender, EventArgs e)
        {
            if (chkFilterByIntensity.Checked)
                SortData();
        }

        private void txtMinimumIntensity_Validating(object sender, CancelEventArgs e)
        {
            PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtMinimumIntensity, 0, 1000000000, 1000000);
        }

        private void txtMinimumSignalToNoise_Leave(object sender, EventArgs e)
        {
            if (chkFilterBySignalToNoise.Checked)
                SortData();
        }

        private void txtMinimumSignalToNoise_Validating(object sender, CancelEventArgs e)
        {
            PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtMinimumSignalToNoise, 0, 10000, 2);
        }

        private void txtPeakWidthPointsMinimum_TextChanged(object sender, EventArgs e)
        {
            UpdateStatsAndPlot();
        }

        private void txtPeakWidthPointsMinimum_Validating(object sender, CancelEventArgs e)
        {
            PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtPeakWidthPointsMinimum, 2, 100000, 6);
        }

        private void txtSavitzkyGolayFilterOrder_TextChanged(object sender, EventArgs e)
        {
            UpdateStatsAndPlot();
        }

        private void txtSavitzkyGolayFilterOrder_Validating(object sender, CancelEventArgs e)
        {
            PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtSavitzkyGolayFilterOrder, 0, 20, 0);
        }

        private void txtStats1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
            {
                switch (Strings.Asc(e.KeyChar))
                {
                    case 3:
                        // Ctrl+C; allow copy
                        break;

                    case 1:
                        // Ctrl+A; select all
                        txtStats1.SelectAll();
                        break;

                    default:
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        private void txtStats2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
            {
                switch (Strings.Asc(e.KeyChar))
                {
                    case 3:
                        // Ctrl+C; allow copy
                        break;

                    case 1:
                        // Ctrl+A; select all
                        txtStats2.SelectAll();
                        break;

                    default:
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        private void txStats3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
            {
                switch (Strings.Asc(e.KeyChar))
                {
                    case 3:
                        // Ctrl+C; allow copy
                        break;

                    case 1:
                        // Ctrl+A; select all
                        txtStats3.SelectAll();
                        break;

                    default:
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        #endregion

        #region "Menubar"
        private void mnuHelpAbout_Click(object sender, EventArgs e)
        {
            ShowAboutBox();
        }

        private void mnuEditShowOptimalPeakApexCursor_Click(object sender, EventArgs e)
        {
            mnuEditShowOptimalPeakApexCursor.Checked = !mnuEditShowOptimalPeakApexCursor.Checked;
        }

        private void mnuFileSelectMASICInputFile_Click(object sender, EventArgs e)
        {
            SelectMASICInputFile();
        }

        private void mnuFileSelectMSMSSearchResultsFile_Click(object sender, EventArgs e)
        {
            SelectMsMsSearchResultsInputFile();
        }

        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            Close();
        }
        #endregion

        private void frmBrowser_Closing(object sender, CancelEventArgs e)
        {
            RegSaveSettings();
        }

        private void frmBrowser_Load(object sender, EventArgs e)
        {
            // Note that InitializeControls() is called in Sub New()
        }

        private void tmrAutoStep_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (mAutoStepEnabled)
                CheckAutoStep();
        }

        private void frmBrowser_Resize(object sender, EventArgs e)
        {
            PositionControls();
        }

        private void mFileLoadTimer_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(FileToAutoLoad))
            {
                mFileLoadTimer.Enabled = false;

                txtDataFilePath.Text = FileToAutoLoad;
                ReadDataFileXMLTextReader(txtDataFilePath.Text);
            }
        }

        private void MASICPeakFinderErrorHandler(string message, Exception ex)
        {
            if (DateTime.UtcNow.Subtract(mLastErrorNotification).TotalSeconds > 5)
            {
                mLastErrorNotification = DateTime.UtcNow;
                MessageBox.Show("MASICPeakFinder error: " + message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}