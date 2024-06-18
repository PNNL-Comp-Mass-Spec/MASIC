// This program will read the _SICs.xml data file created by MASIC to allow
// for browsing of the spectra
//
// -------------------------------------------------------------------------------
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
// Program started October 17, 2003
// Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
// Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics
// -------------------------------------------------------------------------------
//
// Licensed under the 2-Clause BSD License; you may Not use this file except
// in compliance with the License.  You may obtain a copy of the License at
// https://opensource.org/licenses/BSD-2-Clause

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
using C5;                      // TreeDictionary
using MASICPeakFinder;
using Microsoft.Win32;
using OxyDataPlotter;
using OxyPlot;
using PRISM;
using PRISMDatabaseUtils;
using ProgressFormNET;

namespace MASICBrowser
{
    /// <summary>
    /// MASIC Browser
    /// </summary>
    public partial class frmBrowser : Form
    {
        // ReSharper disable once CommentTypo
        // Ignore Spelling: cancelled, const, Ctrl, frag, fragscan, frm, Golay, Loc, resmooth, Savitzky, similarFragScan

        private const string PROGRAM_DATE = "June 18, 2024";

        /// <summary>
        /// Constructor
        /// </summary>
        public frmBrowser()
        {
            Application.EnableVisualStyles();
            Application.DoEvents();

            InitializeComponent();

            InitializeControls();
        }

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

        private enum SortOrderConstants
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

        private enum SICTypeFilterConstants
        {
            AllSICs = 0,
            NoCustomSICs = 1,
            CustomSICsOnly = 2
        }

        private enum SmoothModeConstants
        {
            DoNotReSmooth = 0,
            Butterworth = 1,
            SavitzkyGolay = 2
        }

        private enum MsMsSearchEngineResultColumns
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

        private enum CurrentXMLDataFileSectionConstants
        {
            UnknownFile = 0,
            Start = 1,
            Options = 2,
            ParentIons = 3
        }

        /// <summary>
        /// Path to the data file to load
        /// </summary>
        public string FileToAutoLoad { get; set; }

        private Spectrum mSpectrum;

        private List<clsParentIonStats> mParentIonStats;

        private DataSet mMsMsResults;

        private SICPeakFinderOptions mSICPeakFinderOptions;

        /// <summary>
        /// Number of data point in mParentIonPointerArray
        /// </summary>
        /// <remarks>Could be less than mParentIonStats.Count if filtering the data</remarks>
        private int mParentIonPointerArrayCount;

        /// <summary>
        /// Pointer array used for dereferencing cboParentIon.SelectedItem to mParentIonStats
        /// </summary>
        private int[] mParentIonPointerArray = Array.Empty<int>();

        private bool mAutoStepEnabled;
        private int mAutoStepIntervalMsec;

        private DateTime mLastUpdate;
        private clsMASICPeakFinder mMASICPeakFinder;

        private DateTime mLastErrorNotification;

        private System.Windows.Forms.Timer mFileLoadTimer;

        /// <summary>
        /// Look for a corresponding Synopsis or First hits file in the same folder as masicFilePath
        /// </summary>
        /// <param name="masicFilePath"></param>
        /// <param name="progressForm"></param>
        private void AutoOpenMsMsResults(string masicFilePath, frmProgress progressForm)
        {
            var dataDirectoryInfo = new DirectoryInfo(Path.GetDirectoryName(masicFilePath) ?? ".");

            var fileNameBase = Path.GetFileNameWithoutExtension(masicFilePath);

            if (fileNameBase.EndsWith("_sics", StringComparison.OrdinalIgnoreCase))
            {
                fileNameBase = fileNameBase.Substring(0, fileNameBase.Length - 5);
            }

            var fileNameToFind = fileNameBase + "_fht.txt";
            fileNameToFind = Path.Combine(dataDirectoryInfo.FullName, fileNameToFind);

            txtStats3.Visible = false;

            if (File.Exists(fileNameToFind))
            {
                ReadMsMsSearchEngineResults(fileNameToFind, progressForm);
            }
            else
            {
                fileNameToFind = fileNameBase + "_syn.txt";
                fileNameToFind = Path.Combine(dataDirectoryInfo.FullName, fileNameToFind);

                if (File.Exists(fileNameToFind))
                {
                    ReadMsMsSearchEngineResults(fileNameToFind, progressForm);
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
                var sortOrder = (SortOrderConstants)cboSortOrder.SelectedIndex;

                chkSortDescending.Checked = sortOrder switch
                {
                    // Set SortDescending to false to sort these ascending
                    SortOrderConstants.SortByMz => false,
                    SortOrderConstants.SortByPeakIndex => false,
                    SortOrderConstants.SortByScanOptimalPeakCenter => false,
                    SortOrderConstants.SortByScanPeakCenter => false,
                    SortOrderConstants.SortByPeakSkew => false,
                    SortOrderConstants.SortByKSStat => false,
                    // Sort the others descending by default
                    _ => true
                };
            }
            catch (Exception)
            {
                // Ignore any errors
            }
        }

        /// <summary>
        /// Display SIC stats
        /// </summary>
        /// <remarks>If re-smooth data is enabled, the SIC data returned in sicStats will be re-smoothed</remarks>
        /// <param name="parentIonIndex"></param>
        /// <param name="sicStats">
        /// Output: populated with either the original SIC stats found by MASIC
        /// or with the updated SIC stats if chkUsePeakFinder is Checked
        /// </param>
        private void DisplaySICStats(int parentIonIndex, out clsSICStats sicStats)
        {
            UpdateSICPeakFinderOptions();

            if (parentIonIndex >= 0 && parentIonIndex < mParentIonStats.Count)
            {
                SmoothModeConstants smoothMode;

                if (optUseButterworthSmooth.Checked)
                {
                    smoothMode = SmoothModeConstants.Butterworth;
                }
                else if (optUseSavitzkyGolaySmooth.Checked)
                {
                    smoothMode = SmoothModeConstants.SavitzkyGolay;
                }
                else
                {
                    smoothMode = SmoothModeConstants.DoNotReSmooth;
                }

                var validPeakFound = UpdateSICStats(parentIonIndex, chkUsePeakFinder.Checked, smoothMode, out sicStats);

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
            if (mParentIonPointerArrayCount > 0)
            {
                DisplaySICStats(mParentIonPointerArray[lstParentIonData.SelectedIndex], out _);
            }
        }

        private void EnableDisableControls()
        {
            bool useButterworth;
            bool useSavitzkyGolay;

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
            else
            {
                useButterworth = false;
                useSavitzkyGolay = false;
            }

            txtButterworthSamplingFrequency.Enabled = useButterworth;
            txtSavitzkyGolayFilterOrder.Enabled = useSavitzkyGolay;
        }

        /// <summary>
        /// Find the minimum potential peak area in the parent ions between
        /// parentIonIndexStart and parentIonIndexEnd, storing in ionStats.SICStats.SICPotentialAreaStatsForPeak
        /// </summary>
        /// <remarks>
        /// The summed intensity is not used if the number of points greater than or equal to
        /// .SICNoiseThresholdIntensity is less than Minimum_Peak_Width
        /// </remarks>
        /// <param name="parentIonIndexStart"></param>
        /// <param name="parentIonIndexEnd"></param>
        /// <param name="potentialAreaStatsForRegion"></param>
        private void FindMinimumPotentialPeakAreaInRegion(int parentIonIndexStart, int parentIonIndexEnd, SICPotentialAreaStats potentialAreaStatsForRegion)
        {
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
        }

        private bool FindSICPeakAndAreaForParentIon(int parentIonIndex, clsSICStats sicStats)
        {
            var sicPotentialAreaStatsForRegion = new SICPotentialAreaStats();

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
                    for (var index = 0; index < parentIon.SICData.Count; index++)
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

                if (parentIon.SICStats.Peak.BaselineNoiseStats.NoiseThresholdModeUsed == clsMASICPeakFinder.NoiseThresholdModes.DualTrimmedMeanByAbundance)
                {
                    recomputeNoiseLevel = false;
                }
                else
                {
                    recomputeNoiseLevel = true;
                    // Note: We cannot use DualTrimmedMeanByAbundance since we don't have access to the full-length SICs
                    if (mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode == clsMASICPeakFinder.NoiseThresholdModes.DualTrimmedMeanByAbundance)
                    {
                        mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = clsMASICPeakFinder.NoiseThresholdModes.TrimmedMedianByAbundance;
                    }
                }

                mMASICPeakFinder.FindSICPeakAndArea(
                    parentIon.SICData, out var potentialAreaStatsForPeak, sicStats.Peak,
                    out var smoothedYDataSubset, mSICPeakFinderOptions,
                    sicPotentialAreaStatsForRegion,
                    returnClosestPeak, false, recomputeNoiseLevel);

                sicStats.SICPotentialAreaStatsForPeak = potentialAreaStatsForPeak;

                // Copy the data out of smoothedYDataSubset and into sicStats.SICSmoothedYData
                sicStats.SICSmoothedYData.Clear();

                foreach (var dataPoint in parentIon.SICData)
                {
                    sicStats.SICSmoothedYData.Add(dataPoint.Intensity);
                }

                sicStats.SICSmoothedYDataIndexStart = smoothedYDataSubset.DataStartIndex;

                try
                {
                    // Update the two computed values
                    sicStats.SICPeakWidthFullScans =
                        mParentIonStats[parentIonIndex].SICData[sicStats.Peak.IndexBaseRight].ScanNumber -
                        mParentIonStats[parentIonIndex].SICData[sicStats.Peak.IndexBaseLeft].ScanNumber + 1;

                    sicStats.ScanNumberMaxIntensity = mParentIonStats[parentIonIndex].SICData[sicStats.Peak.IndexMax].ScanNumber;
                }
                catch (Exception)
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

        private void FindSimilarParentIon(frmProgress progressForm)
        {
            const double SIMILAR_MZ_TOLERANCE = 0.2;

            FindSimilarParentIon(SIMILAR_MZ_TOLERANCE, progressForm);
        }

        private void FindSimilarParentIon(double similarMZTolerance, frmProgress progressForm)
        {
            const double DEFAULT_INTENSITY = 1;

            try
            {
                if (mParentIonStats == null || mParentIonStats.Count == 0)
                {
                    return;
                }

                progressForm.UpdateCurrentTask("Finding similar parent ions");
                Application.DoEvents();

                var similarFragScans = new SortedSet<int>();
                var parentIonCount = mParentIonStats.Count;

                // Populate a dictionary mapping fragmentation scan to parent ion stats
                // Using a TreeDictionary since it supports RangeFromTo
                var fragScanObservedToParentIonMap = new TreeDictionary<int, List<clsParentIonStats>>();

                foreach (var ionStats in mParentIonStats)
                {
                    if (fragScanObservedToParentIonMap.Contains(ionStats.FragScanObserved))
                    {
                        fragScanObservedToParentIonMap[ionStats.FragScanObserved].Add(ionStats);
                    }
                    else
                    {
                        fragScanObservedToParentIonMap.Add(ionStats.FragScanObserved, new List<clsParentIonStats> { ionStats });
                    }
                }

                for (var parentIonIndex = 0; parentIonIndex < parentIonCount; parentIonIndex++)
                {
                    // Percent complete will be between 0.5 and 1.0
                    var percentComplete = 0.5 + parentIonIndex / (double)parentIonCount / 2;

                    if (percentComplete > 1.0)
                        percentComplete = 1.0;

                    progressForm.UpdateProgressBar(percentComplete);
                    Application.DoEvents();

                    if (progressForm.KeyPressAbortProcess)
                        break;

                    var currentParentIon = mParentIonStats[parentIonIndex];

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

                    // Find parent ions with a fragmentation scan between scanStart and scanEnd
                    // and a similar m/z

                    foreach (var item in fragScanObservedToParentIonMap.RangeFromTo(scanStart, scanEnd))
                    {
                        foreach (var parentIonToCompare in item.Value)
                        {
                            if (currentParentIon.Index == parentIonToCompare.Index)
                                continue;

                            var comparisonFragScanObserved = parentIonToCompare.FragScanObserved;

                            if (comparisonFragScanObserved < scanStart || comparisonFragScanObserved > scanEnd)
                                continue;

                            if (Math.Abs(parentIonToCompare.MZ - parentIonMZ) > similarMZTolerance)
                                continue;

                            // Similar parent ion m/z found
                            if (!similarFragScans.Contains(comparisonFragScanObserved))
                            {
                                similarFragScans.Add(comparisonFragScanObserved);
                            }
                        }
                    }

                    //
                    // Old, inefficient brute force:
                    //

                    /*
                    for (var indexCompare = 0; indexCompare < mParentIonStats.Count; indexCompare++)
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
                    */

                    if (similarFragScans.Count == 0)
                    {
                        continue;
                    }

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

                        var matchIndex = BinarySearch.BinarySearchFindNearest(
                            scanNumbers,
                            similarFragScan,
                            BinarySearch.MissingDataModeConstants.ReturnPreviousPoint);

                        if (matchIndex < 0)
                        {
                            // Match not found; find the closest match via brute-force searching
                            matchIndex = -1;

                            for (var scanIndex = 0; scanIndex < scanNumbers.Count - 1; scanIndex++)
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
                                    // Fragmentation scan is at or before the first scan number in .SICData()
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
                                // Fragmentation scan is at or after the last scan number in .SICData()
                                // Use the last data point in .SICData()
                                interpolatedYValue = currentParentIon.SICData.Last().Intensity;
                            }
                        }

                        var newDataPoint = new SICDataPoint(similarFragScan, interpolatedYValue, 0);
                        currentParentIon.SimilarFragScans.Add(newDataPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in FindSimilarParentIon: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private bool GetSettingValue(RegistryKey regKey, string name, bool defaultValue)
        {
            var value = regKey?.GetValue(name, defaultValue.ToString())?.ToString() ?? string.Empty;

            if (bool.TryParse(value, out var parsedValue))
                return parsedValue;

            return defaultValue;
        }

        private int GetSettingValue(RegistryKey regKey, string name, int defaultValue)
        {
            var value = regKey?.GetValue(name, defaultValue.ToString())?.ToString() ?? string.Empty;

            if (int.TryParse(value, out var parsedValue))
                return parsedValue;

            return defaultValue;
        }

        private float GetSettingValue(RegistryKey regKey, string name, float defaultValue)
        {
            var value = regKey?.GetValue(name, defaultValue.ToString(CultureInfo.InvariantCulture))?.ToString() ?? string.Empty;

            if (float.TryParse(value, out var parsedValue))
                return parsedValue;

            return defaultValue;
        }

        private string GetSettingValue(RegistryKey regKey, string name, string defaultValue)
        {
            var value = regKey?.GetValue(name, defaultValue)?.ToString() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(value))
                return value;

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
            msmsResultsTable.PrimaryKey = new[] { msmsResultsTable.Columns[COL_NAME_SCAN], msmsResultsTable.Columns[COL_NAME_CHARGE], msmsResultsTable.Columns[COL_NAME_SEQUENCE_ID] };

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
            seqToProteinMapTable.PrimaryKey = new[] { seqToProteinMapTable.Columns[COL_NAME_SEQUENCE_ID], seqToProteinMapTable.Columns[COL_NAME_PROTEIN] };

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
            sequenceInfoTable.PrimaryKey = new[] { sequenceInfoTable.Columns[COL_NAME_SEQUENCE] };

            // Add the table to the DataSet
            if (mMsMsResults.Tables.Contains(TABLE_NAME_SEQUENCES))
            {
                mMsMsResults.Tables.Remove(TABLE_NAME_SEQUENCES);
            }

            mMsMsResults.Tables.Add(sequenceInfoTable);
        }

        /// <summary>
        /// Checks if X1 or X2 is less than targetX
        /// If it is, determines the Y value that corresponds to targetX by interpolating the line between (X1, Y1) and (X2, Y2)
        /// </summary>
        /// <param name="interpolatedYValue"></param>
        /// <param name="X1"></param>
        /// <param name="X2"></param>
        /// <param name="Y1"></param>
        /// <param name="Y2"></param>
        /// <param name="targetX"></param>
        /// <returns>True if the value could be interpolated; otherwise, false</returns>
        private bool InterpolateY(out double interpolatedYValue, int X1, int X2, double Y1, double Y2, int targetX)
        {
            if (X1 < targetX || X2 < targetX)
            {
                if (X1 < targetX && X2 < targetX)
                {
                    // Both of the X values are less than targetX
                    // We cannot interpolate
                    ConsoleMsgUtils.ShowWarning("This code should normally not be reached (frmBrowser->InterpolateY)");
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

                    ConsoleMsgUtils.ShowWarning("TargetY is not between Y1 and Y2; this shouldn't happen (frmBrowser->InterpolateY)");
                    interpolatedYValue = 0;
                    return false;
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

                        var response = InputBox.Show("Enter the scan number to jump to: ", "Jump to Scan",
                            scanNumberToFind.ToString(),
                            (sender, args) =>
                            {
                                if (!int.TryParse(args.Text, out _))
                                {
                                    args.Cancel = true;
                                    args.Message = "Input must be a whole number";
                                }
                            });

                        if (response.OK && int.TryParse(response.Text, out var numberToFind))
                        {
                            scanNumberToFind = numberToFind;
                        }
                    }

                    if (scanNumberToFind >= 0)
                    {
                        // Find the best match to scanNumberToFind
                        // First search for an exact match
                        var indexMatch = -1;

                        for (var index = 0; index < lstParentIonData.Items.Count; index++)
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

                            for (var index = 0; index < lstParentIonData.Items.Count; index++)
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
            catch (Exception)
            {
                // Ignore any errors in this method
            }
        }

        private string LookupSequenceForParentIonIndex(int parentIonIndex)
        {
            var sequences = string.Empty;

            var resultTable = mMsMsResults.Tables[TABLE_NAME_MSMS_RESULTS];

            try
            {
                var matchingRows = resultTable.Select(COL_NAME_PARENT_ION_INDEX + " = " + parentIonIndex);
                var sequenceCount = 0;

                foreach (var currentRow in matchingRows)
                {
                    var sequenceID = (int)currentRow[COL_NAME_SEQUENCE_ID];
                    try
                    {
                        var sequenceRows = mMsMsResults.Tables[TABLE_NAME_SEQUENCES].Select(COL_NAME_SEQUENCE_ID + " = " + sequenceID);

                        if (sequenceRows.Length > 0)
                        {
                            if (sequenceCount > 0)
                            {
                                sequences += Environment.NewLine;
                            }

                            sequences += sequenceRows[0][COL_NAME_SEQUENCE].ToString();
                            sequenceCount++;
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

        /// <summary>
        /// Looks for sequence in mMsMsResults.Tables(TABLE_NAME_SEQUENCES)
        /// Returns the SequenceID if found; adds it if not present
        /// Additionally, adds a mapping between sequence and protein in mMsMsResults.Tables(TABLE_NAME_SEQ_TO_PROTEIN_MAP)
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="protein"></param>
        /// <returns>Sequence ID, or -1 if an error</returns>
        private int LookupSequenceID(string sequence, string protein)
        {
            if (sequence == null)
                return -1;

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
                var newSequenceRow = sequencesTable.Rows.Find(sequenceNoSuffixes);

                if (newSequenceRow == null)
                {
                    newSequenceRow = sequencesTable.NewRow();
                    newSequenceRow[COL_NAME_SEQUENCE] = sequenceNoSuffixes;
                    sequencesTable.Rows.Add(newSequenceRow);
                }

                var sequenceID = (int)newSequenceRow[COL_NAME_SEQUENCE_ID];

                if (sequenceID >= 0)
                {
                    try
                    {
                        // Possibly add sequenceNoSuffixes and protein to .Tables(TABLE_NAME_SEQ_TO_PROTEIN_MAP)
                        var seqToProteinMapTable = mMsMsResults.Tables[TABLE_NAME_SEQ_TO_PROTEIN_MAP];

                        if (!seqToProteinMapTable.Rows.Contains(new object[] { sequenceID, protein }))
                        {
                            var newMapRow = seqToProteinMapTable.NewRow();
                            newMapRow[COL_NAME_SEQUENCE_ID] = sequenceID;
                            newMapRow[COL_NAME_PROTEIN] = protein;
                            seqToProteinMapTable.Rows.Add(newMapRow);
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore errors here
                    }
                }

                return sequenceID;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private void NavigateScanList(bool moveForward)
        {
            if (moveForward)
            {
                if (lstParentIonData.SelectedIndex < lstParentIonData.Items.Count - 1)
                {
                    lstParentIonData.SelectedIndex++;
                }
                else if (mAutoStepEnabled)
                {
                    ToggleAutoStep(true);
                }
            }
            else if (lstParentIonData.SelectedIndex > 0)
            {
                lstParentIonData.SelectedIndex--;
            }
            else if (mAutoStepEnabled)
            {
                ToggleAutoStep(true);
            }
        }

        /// <summary>
        /// Plot the selected ion chromatogram
        /// </summary>
        /// <param name="indexToPlot">Index in mParentIonStats()</param>
        /// <param name="sicStats"></param>
        private void PlotData(int indexToPlot, clsSICStats sicStats)
        {
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
                    dataCountSeries1++;

                    // Zero pad Series 2
                    xDataSeries2[0] = currentParentIon.SICData[0].ScanNumber;
                    yDataSeries2[0] = 0;
                    dataCountSeries2++;

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
                    smoothedYData = Array.Empty<double>();
                }
                else
                {
                    smoothedYDataIndexStart = sicStats.SICSmoothedYDataIndexStart;

                    smoothedYData = new double[sicStats.SICSmoothedYData.Count];

                    for (var index = 0; index < sicStats.SICSmoothedYData.Count; index++)
                    {
                        smoothedYData[index] = sicStats.SICSmoothedYData[index];
                    }
                }

                // Populate Series 3 with the similar fragmentation scan values
                for (var index = 0; index < currentParentIon.SimilarFragScans.Count; index++)
                {
                    xDataSeries3[index] = currentParentIon.SimilarFragScans[index].ScanNumber;
                    yDataSeries3[index] = currentParentIon.SimilarFragScans[index].Intensity;
                }

                for (var index = 0; index < currentParentIon.SICData.Count; index++)
                {
                    if (index < currentParentIon.SICData.Count - 1)
                    {
                        if (currentParentIon.SICData[index].ScanNumber <= currentParentIon.FragScanObserved && currentParentIon.SICData[index + 1].ScanNumber >= currentParentIon.FragScanObserved)
                        {
                            // Use the survey scan data to calculate the appropriate intensity for the Fragmentation Scan cursor

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
                            dataCountSeries1++;
                            xDataSeries1[dataCountSeries1] = currentParentIon.SICData[index].ScanNumber;
                            yDataSeries1[dataCountSeries1] = 0;
                            dataCountSeries1++;

                            // Zero pad Series 2
                            xDataSeries2[dataCountSeries2] = currentParentIon.SICData[index].ScanNumber;
                            yDataSeries2[dataCountSeries2] = 0;
                            dataCountSeries2++;

                            zeroEdgeSeries1 = true;
                        }

                        xDataSeries2[dataCountSeries2] = currentParentIon.SICData[index].ScanNumber;
                        yDataSeries2[dataCountSeries2] = currentParentIon.SICData[index].Intensity;
                        dataCountSeries2++;
                    }
                    else
                    {
                        if (index > 0 && zeroEdgeSeries1)
                        {
                            // Zero pad Series 2
                            xDataSeries2[dataCountSeries2] = currentParentIon.SICData[index - 1].ScanNumber;
                            yDataSeries2[dataCountSeries2] = 0;
                            dataCountSeries2++;

                            // Zero pad Series 1
                            xDataSeries1[dataCountSeries1] = currentParentIon.SICData[index - 1].ScanNumber;
                            yDataSeries1[dataCountSeries1] = 0;
                            dataCountSeries1++;

                            xDataSeries1[dataCountSeries1] = currentParentIon.SICData[index - 1].ScanNumber;
                            yDataSeries1[dataCountSeries1] = currentParentIon.SICData[index - 1].Intensity;
                            dataCountSeries1++;
                            zeroEdgeSeries1 = false;
                        }

                        xDataSeries1[dataCountSeries1] = currentParentIon.SICData[index].ScanNumber;
                        yDataSeries1[dataCountSeries1] = currentParentIon.SICData[index].Intensity;
                        dataCountSeries1++;
                    }

                    if (index >= smoothedYDataIndexStart &&
                        index - smoothedYDataIndexStart < smoothedYData.Length)
                    {
                        xDataSeries4[dataCountSeries4] = currentParentIon.SICData[index].ScanNumber;
                        yDataSeries4[dataCountSeries4] = smoothedYData[index - smoothedYDataIndexStart];
                        dataCountSeries4++;
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

                const string fragScansCaption = "Similar Frag scans";
                mSpectrum.SetDataXvsY(3, xDataSeries3, yDataSeries3, dataCountSeries3, ctlOxyPlotControl.SeriesPlotMode.Points, fragScansCaption);

                if (chkShowSmoothedData.Checked && xDataSeries4.Length > 0)
                {
                    mSpectrum.SetDataXvsY(4, xDataSeries4, yDataSeries4, dataCountSeries4, ctlOxyPlotControl.SeriesPlotMode.Lines, "Smoothed data");
                }
                else
                {
                    while (mSpectrum.GetSeriesCount() >= 4)
                    {
                        mSpectrum.RemoveSeries(4);
                    }
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

                const int arrowLengthPixels = 15;
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
                mSpectrum.SetAnnotationForDataPoint(
                    fragScanObserved, scanObservedIntensity, "MS2",
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
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByPeakIndex, "Sort by Scan of Peak Index");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByScanPeakCenter, "Sort by Scan of Peak Center");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByScanOptimalPeakCenter, "Sort by Scan of Optimal Peak Apex");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByMz, "Sort by m/z");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByPeakSignalToNoise, "Sort by Peak Signal/Noise");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByBaselineCorrectedPeakIntensity, "Sort by Baseline-corrected Intensity");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByBaselineCorrectedPeakArea, "Sort by Baseline-corrected Area");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByPeakWidth, "Sort by Peak FWHM (Width)");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortBySICIntensityMax, "Sort by SIC Max Intensity");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByPeakIntensity, "Sort by Peak Intensity (uncorrected for noise)");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByPeakArea, "Sort by Peak Area (uncorrected for noise)");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByFragScanToOptimalLocDistance, "Sort by Frag Scan to Optimal Loc Distance");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByPeakCenterToOptimalLocDistance, "Sort by Peak Center to Optimal Loc Distance");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByShoulderCount, "Sort by Shoulder Peak Count");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByParentIonIntensity, "Sort by Parent Ion Intensity");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByPeakSkew, "Sort by Peak Skew");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByKSStat, "Sort by Peak KS Stat");
            cboSortOrder.Items.Insert((int)SortOrderConstants.SortByBaselineNoiseLevel, "Sort by Baseline Noise level");
            cboSortOrder.SelectedIndex = (int)SortOrderConstants.SortByPeakSignalToNoise;

            cboSICsTypeFilter.Items.Clear();
            cboSICsTypeFilter.Items.Insert((int)SICTypeFilterConstants.AllSICs, "All SIC's");
            cboSICsTypeFilter.Items.Insert((int)SICTypeFilterConstants.NoCustomSICs, "No custom SIC's");
            cboSICsTypeFilter.Items.Insert((int)SICTypeFilterConstants.CustomSICsOnly, "Custom SIC's only");

            cboSICsTypeFilter.SelectedIndex = (int)SICTypeFilterConstants.AllSICs;
        }

        /// <summary>
        /// <para>
        /// For each row in mMsMsResults.Tables(TABLE_NAME_MSMS_RESULTS), find the corresponding row in mParentIonStats
        /// </para>
        /// <para>
        /// Construct a mapping between .FragScanObserved and Index in mParentIonStats
        /// If multiple parent ions have the same value for .FragScanObserved, we will only track the mapping to the first one
        /// </para>
        /// </summary>
        private void PopulateParentIonIndexColumnInMsMsResultsTable()
        {
            var fragScanToIndexMap = new Dictionary<int, int>();

            for (var index = 0; index < mParentIonStats.Count; index++)
            {
                if (!fragScanToIndexMap.ContainsKey(mParentIonStats[index].FragScanObserved))
                {
                    fragScanToIndexMap.Add(mParentIonStats[index].FragScanObserved, index);
                }
            }

            foreach (DataRow currentRow in mMsMsResults.Tables[TABLE_NAME_MSMS_RESULTS].Rows)
            {
                if (fragScanToIndexMap.TryGetValue((int)currentRow[COL_NAME_SCAN], out var value))
                {
                    currentRow[COL_NAME_PARENT_ION_INDEX] = value;
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
                    for (var index = 0; index < mParentIonPointerArrayCount; index++)
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
            var useButterworthSmooth = false;
            var useSavitzkyGolaySmooth = false;
            float butterworthSamplingFrequency = 0;
            var savitzkyGolayFilterOrder = 0;
            var scanStart = 0;

            var peakScanStart = 0;
            var peakScanEnd = 0;

            var smoothedDataFound = false;
            var baselineNoiseStatsFound = false;

            var scanIntervals = string.Empty;
            var intensityDataList = string.Empty;
            var massDataList = string.Empty;
            var smoothedYDataList = string.Empty;
            const string delimiters = " ,;\t";

            var delimiterList = delimiters.ToCharArray();

            var expectedSicDataCount = 0;

            if (!File.Exists(filePath))
            {
                return;
            }

            // Initialize the progress form
            var progressForm = new frmProgress();
            try
            {
                // Initialize the stream reader and the XML Text Reader
                CurrentXMLDataFileSectionConstants currentXMLDataFileSection;

                using (var reader = new StreamReader(filePath))
                using (var xmlReader = new XmlTextReader(reader))
                {
                    progressForm.InitializeProgressForm("Reading file " + Environment.NewLine + PathUtils.CompactPathString(filePath), 0, 1, true);
                    progressForm.Show();
                    Application.DoEvents();

                    // Initialize mParentIonStats
                    mParentIonStats.Clear();

                    currentXMLDataFileSection = CurrentXMLDataFileSectionConstants.UnknownFile;
                    var validParentIon = false;

                    while (xmlReader.Read())
                    {
                        XMLTextReaderSkipWhitespace(xmlReader);

                        if (xmlReader.ReadState != ReadState.Interactive)
                            break;

                        if (xmlReader.Depth < 2)
                        {
                            if (xmlReader.NodeType == XmlNodeType.Element)
                            {
                                switch (xmlReader.Name)
                                {
                                    case "ParentIon":
                                        currentXMLDataFileSection = CurrentXMLDataFileSectionConstants.ParentIons;
                                        validParentIon = false;

                                        if (xmlReader.HasAttributes)
                                        {
                                            baselineNoiseStatsFound = false;
                                            scanStart = 0;
                                            peakScanStart = 0;
                                            peakScanEnd = 0;

                                            scanIntervals = string.Empty;
                                            intensityDataList = string.Empty;
                                            massDataList = string.Empty;
                                            smoothedYDataList = string.Empty;

                                            indexInXMLFile = xmlReader.GetAttribute("Index");

                                            var newParentIon = new clsParentIonStats
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
                                            // Dividing by two since FindSimilarParentIons can take time
                                            var percentComplete = reader.BaseStream.Position / (double)reader.BaseStream.Length / 2;

                                            if (percentComplete > 1.0)
                                                percentComplete = 1.0;

                                            progressForm.UpdateProgressBar(percentComplete);
                                            Application.DoEvents();

                                            if (progressForm.KeyPressAbortProcess)
                                                break;

                                            // Advance to the next tag
                                            xmlReader.Read();
                                            XMLTextReaderSkipWhitespace(xmlReader);

                                            if (xmlReader.ReadState != ReadState.Interactive)
                                                break;
                                        }
                                        else
                                        {
                                            // Attribute isn't present; skip this parent ion
                                            indexInXMLFile = "-1";
                                        }

                                        break;

                                    case "SICData":
                                        currentXMLDataFileSection = CurrentXMLDataFileSectionConstants.Start;
                                        break;

                                    case "ProcessingSummary":
                                    case "MemoryOptions":
                                        xmlReader.Skip();
                                        break;

                                    case "SICOptions":
                                        currentXMLDataFileSection = CurrentXMLDataFileSectionConstants.Options;
                                        break;

                                    case "ProcessingStats":
                                        xmlReader.Skip();
                                        break;
                                }
                            }
                            else if (xmlReader.NodeType == XmlNodeType.EndElement)
                            {
                                if ((xmlReader.Name ?? string.Empty) == "ParentIon")
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
                                        // If the interval is <= 9, it is stored as a number
                                        // For intervals between 10 and 35, uses letters A to Z
                                        // For intervals between 36 and 61, uses letters A to Z
                                        if (scanIntervals != null)
                                        {
                                            for (var charIndex = 1; charIndex < scanIntervals.Length; charIndex++)
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
                                            foreach (var intensity in intensityDataList.Trim().Split(delimiterList))
                                            {
                                                if (PRISM.DataUtils.StringToValueUtils.IsNumber(intensity))
                                                {
                                                    sicIntensities.Add(double.Parse(intensity));
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
                                            foreach (var mz in massDataList.Trim().Split(delimiterList))
                                            {
                                                if (PRISM.DataUtils.StringToValueUtils.IsNumber(mz))
                                                {
                                                    sicMasses.Add(double.Parse(mz));
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

                                        for (var index = 0; index < sicScans.Count; index++)
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

                                            var newDataPoint = new SICDataPoint(sicScans[index], sicIntensities[index], massValue);
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

                                            for (var index = 0; index < valueList.Length; index++)
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
                                            if (mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode == clsMASICPeakFinder.NoiseThresholdModes.DualTrimmedMeanByAbundance)
                                            {
                                                mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = clsMASICPeakFinder.NoiseThresholdModes.TrimmedMedianByAbundance;
                                            }

                                            mMASICPeakFinder.ComputeNoiseLevelInPeakVicinity(sicData, currentParentIon.SICStats.Peak, mSICPeakFinderOptions.SICBaselineNoiseOptions);
                                        }
                                    }

                                    validParentIon = false;
                                }
                            }
                        }

                        if (currentXMLDataFileSection != CurrentXMLDataFileSectionConstants.UnknownFile && xmlReader.NodeType == XmlNodeType.Element)
                        {
                            switch (currentXMLDataFileSection)
                            {
                                case CurrentXMLDataFileSectionConstants.Options:
                                    try
                                    {
                                        switch (xmlReader.Name)
                                        {
                                            case "SimilarIonMZToleranceHalfWidth":
                                                similarIonMZToleranceHalfWidth = float.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                break;

                                            case "FindPeaksOnSmoothedData":
                                                findPeaksOnSmoothedData = bool.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                break;

                                            case "UseButterworthSmooth":
                                                useButterworthSmooth = bool.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                break;

                                            case "ButterworthSamplingFrequency":
                                                butterworthSamplingFrequency = float.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                break;

                                            case "UseSavitzkyGolaySmooth":
                                                useSavitzkyGolaySmooth = bool.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                break;

                                            case "SavitzkyGolayFilterOrder":
                                                savitzkyGolayFilterOrder = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                break;

                                            default:
                                                // Ignore the setting
                                                break;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        // Ignore any errors looking up smoothing options
                                    }

                                    break;

                                case CurrentXMLDataFileSectionConstants.ParentIons:
                                    if (validParentIon)
                                    {
                                        try
                                        {
                                            var ionStats = mParentIonStats[mParentIonStats.Count - 1];
                                            string value;
                                            switch (xmlReader.Name)
                                            {
                                                case "MZ":
                                                    ionStats.MZ = Math.Round(double.Parse(XMLTextReaderGetInnerText(xmlReader)), 6);
                                                    break;

                                                case "SurveyScanNumber":
                                                    ionStats.SurveyScanNumber = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "FragScanNumber":
                                                    ionStats.FragScanObserved = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "FragScanTime":
                                                    ionStats.FragScanTime = float.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "OptimalPeakApexScanNumber":
                                                    ionStats.OptimalPeakApexScanNumber = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "OptimalPeakApexScanTime":
                                                    ionStats.OptimalPeakApexTime = float.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "CustomSICPeak":
                                                    ionStats.CustomSICPeak = bool.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "CustomSICPeakComment":
                                                    ionStats.CustomSICPeakComment = XMLTextReaderGetInnerText(xmlReader);
                                                    break;

                                                case "SICScanType":
                                                    var sicScanType = XMLTextReaderGetInnerText(xmlReader);

                                                    // ReSharper disable once StringLiteralTypo
                                                    if ((sicScanType.ToLower() ?? string.Empty) == "fragscan")
                                                    {
                                                        ionStats.SICScanType = clsParentIonStats.eScanTypeConstants.FragScan;
                                                    }
                                                    else
                                                    {
                                                        ionStats.SICScanType = clsParentIonStats.eScanTypeConstants.SurveyScan;
                                                    }

                                                    break;

                                                case "PeakScanStart":
                                                    peakScanStart = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "PeakScanEnd":
                                                    peakScanEnd = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "PeakScanMaxIntensity":
                                                    ionStats.SICStats.ScanNumberMaxIntensity = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "PeakIntensity":
                                                    ionStats.SICStats.Peak.MaxIntensityValue = double.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "PeakSignalToNoiseRatio":
                                                    ionStats.SICStats.Peak.SignalToNoiseRatio = double.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "FWHMInScans":
                                                    ionStats.SICStats.Peak.FWHMScanWidth = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "PeakArea":
                                                    ionStats.SICStats.Peak.Area = double.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "ShoulderCount":
                                                    ionStats.SICStats.Peak.ShoulderCount = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "ParentIonIntensity":
                                                    ionStats.SICStats.Peak.ParentIonIntensity = double.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "PeakBaselineNoiseLevel":
                                                    ionStats.SICStats.Peak.BaselineNoiseStats.NoiseLevel = double.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    baselineNoiseStatsFound = true;
                                                    break;

                                                case "PeakBaselineNoiseStDev":
                                                    ionStats.SICStats.Peak.BaselineNoiseStats.NoiseStDev = double.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "PeakBaselinePointsUsed":
                                                    ionStats.SICStats.Peak.BaselineNoiseStats.PointsUsed = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "NoiseThresholdModeUsed":
                                                    ionStats.SICStats.Peak.BaselineNoiseStats.NoiseThresholdModeUsed = (clsMASICPeakFinder.NoiseThresholdModes)int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "StatMomentsArea":
                                                    ionStats.SICStats.Peak.StatisticalMoments.Area = double.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "CenterOfMassScan":
                                                    ionStats.SICStats.Peak.StatisticalMoments.CenterOfMassScan = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "PeakStDev":
                                                    ionStats.SICStats.Peak.StatisticalMoments.StDev = double.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "PeakSkew":
                                                    ionStats.SICStats.Peak.StatisticalMoments.Skew = double.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "PeakKSStat":
                                                    ionStats.SICStats.Peak.StatisticalMoments.KSStat = double.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "StatMomentsDataCountUsed":
                                                    ionStats.SICStats.Peak.StatisticalMoments.DataCountUsed = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "SICScanStart":
                                                    scanStart = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "SICScanIntervals":
                                                    scanIntervals = XMLTextReaderGetInnerText(xmlReader);
                                                    break;

                                                case "SICPeakIndexStart":
                                                    ionStats.SICStats.Peak.IndexBaseLeft = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "SICPeakIndexEnd":
                                                    ionStats.SICStats.Peak.IndexBaseRight = int.Parse(XMLTextReaderGetInnerText(xmlReader));
                                                    break;

                                                case "SICDataCount":
                                                    value = XMLTextReaderGetInnerText(xmlReader);

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
                                                    value = XMLTextReaderGetInnerText(xmlReader);

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
                                                    intensityDataList = XMLTextReaderGetInnerText(xmlReader);
                                                    break;

                                                case "MassDataList":
                                                    massDataList = XMLTextReaderGetInnerText(xmlReader);
                                                    break;

                                                case "SmoothedYDataList":
                                                    smoothedYDataList = XMLTextReaderGetInnerText(xmlReader);
                                                    break;

                                                default:
                                                    // Unknown child node name; ignore it
                                                    break;
                                            }
                                        }
                                        catch (Exception)
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

                FindSimilarParentIon(similarIonMZToleranceHalfWidth * 2, progressForm);

                if (currentXMLDataFileSection == CurrentXMLDataFileSectionConstants.UnknownFile)
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
                        txtButterworthSamplingFrequency.Text = butterworthSamplingFrequency.ToString(CultureInfo.InvariantCulture);
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

                    progressForm.UpdateCurrentTask("Populating the parent ion list");
                    Application.DoEvents();

                    // Sort the data and populate lstParentIonData
                    SortData();

                    // Select the first item in lstParentIonData
                    if (lstParentIonData.Items.Count > 0)
                    {
                        lstParentIonData.SelectedIndex = 0;
                    }

                    if (progressForm.KeyPressAbortProcess)
                    {
                        MessageBox.Show("Load cancelled before all of the data was read", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else
                    {
                        AutoOpenMsMsResults(Path.GetFullPath(filePath), progressForm);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to read the input file: " + filePath + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally
            {
                progressForm?.HideForm();
            }
        }

        private void ReadMsMsSearchEngineResults(string filePath, frmProgress progressForm)
        {
            var chSepChars = new[] { '\t' };

            long bytesRead = 0;

            var createdNewProgressForm = false;

            try
            {
                var dataFileInfo = new FileInfo(filePath);
                var fileSizeBytes = dataFileInfo.Length;

                using (var reader = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    if (progressForm == null)
                    {
                        progressForm = new frmProgress();
                        createdNewProgressForm = true;
                    }

                    progressForm.InitializeProgressForm("Reading MS/MS Search Engine Results ", 0, fileSizeBytes, true);
                    progressForm.Show();
                    progressForm.BringToFront();
                    Application.DoEvents();

                    if (mMsMsResults.Tables[TABLE_NAME_MSMS_RESULTS].Rows.Count > 0 || mMsMsResults.Tables[TABLE_NAME_SEQ_TO_PROTEIN_MAP].Rows.Count > 0 || mMsMsResults.Tables[TABLE_NAME_SEQUENCES].Rows.Count > 0)
                    {
                        ClearMsMsResults();
                    }

                    var linesRead = 0;

                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        linesRead++;

                        if (dataLine != null)
                        {
                            bytesRead += dataLine.Length + 2;

                            if (linesRead % 50 == 0)
                            {
                                progressForm.UpdateProgressBar(bytesRead);
                                Application.DoEvents();

                                if (progressForm.KeyPressAbortProcess)
                                    break;
                            }

                            var dataCols = dataLine.Trim().Split(chSepChars);

                            if (dataCols.Length >= 13)
                            {
                                var sequenceID = LookupSequenceID(dataCols[(int)MsMsSearchEngineResultColumns.Sequence], dataCols[(int)MsMsSearchEngineResultColumns.Protein]);

                                if (sequenceID >= 0)
                                {
                                    try
                                    {
                                        var scanNumber = int.Parse(dataCols[(int)MsMsSearchEngineResultColumns.Scan]);
                                        var charge = int.Parse(dataCols[(int)MsMsSearchEngineResultColumns.Charge]);

                                        var msMsResultsTable = mMsMsResults.Tables[TABLE_NAME_MSMS_RESULTS];

                                        if (!msMsResultsTable.Rows.Contains(new object[] { scanNumber, charge, sequenceID }))
                                        {
                                            var newRow = mMsMsResults.Tables[TABLE_NAME_MSMS_RESULTS].NewRow();
                                            newRow[COL_NAME_SCAN] = scanNumber;
                                            newRow[COL_NAME_CHARGE] = charge;
                                            newRow[COL_NAME_MH] = dataCols[(int)MsMsSearchEngineResultColumns.MH];
                                            newRow[COL_NAME_XCORR] = dataCols[(int)MsMsSearchEngineResultColumns.XCorr];
                                            newRow[COL_NAME_DELTACN] = dataCols[(int)MsMsSearchEngineResultColumns.DeltaCN];
                                            newRow[COL_NAME_DELTACN2] = dataCols[(int)MsMsSearchEngineResultColumns.DeltaCn2];
                                            newRow[COL_NAME_RANK_SP] = dataCols[(int)MsMsSearchEngineResultColumns.RankSp];
                                            newRow[COL_NAME_RANK_XC] = dataCols[(int)MsMsSearchEngineResultColumns.RankXc];
                                            newRow[COL_NAME_SEQUENCE_ID] = sequenceID;
                                            newRow[COL_NAME_PARENT_ION_INDEX] = -1;

                                            mMsMsResults.Tables[TABLE_NAME_MSMS_RESULTS].Rows.Add(newRow);
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
                    progressForm.HideForm();
                }
            }
        }

        private void RedoSICPeakFindingAllData()
        {
            const SmoothModeConstants SMOOTH_MODE = SmoothModeConstants.Butterworth;

            var progressForm = new frmProgress();

            try
            {
                cmdRedoSICPeakFindingAllData.Enabled = false;

                progressForm.InitializeProgressForm("Repeating SIC peak finding ", 0, mParentIonStats.Count, true);
                progressForm.Show();
                Application.DoEvents();

                UpdateSICPeakFinderOptions();

                for (var parentIonIndex = 0; parentIonIndex < mParentIonStats.Count; parentIonIndex++)
                {
                    var validPeakFound = UpdateSICStats(parentIonIndex, true, SMOOTH_MODE, out var sicStats);

                    if (validPeakFound)
                    {
                        mParentIonStats[parentIonIndex].SICStats = sicStats;
                    }

                    progressForm.UpdateProgressBar(parentIonIndex + 1);
                    Application.DoEvents();

                    if (progressForm.KeyPressAbortProcess)
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
                progressForm.HideForm();

                cmdRedoSICPeakFindingAllData.Enabled = true;
            }
        }

        /// <summary>
        /// Load settings from the registry
        /// </summary>
        private void RegReadSettings()
        {
            try
            {
                var regKey = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\PNNL PAST Toolkit\\{REG_APP_NAME}\\{REG_SECTION_NAME}") ??
                             Registry.CurrentUser.OpenSubKey($"SOFTWARE\\VB and VBA Program Settings\\{REG_APP_NAME}\\{REG_SECTION_NAME}");

                txtDataFilePath.Text = GetSettingValue(regKey, "DataFilePath", string.Empty);

                Width = GetSettingValue(regKey, "WindowSizeWidth", Width);

                // Height = GetSettingValue(regKey, "WindowSizeHeight", Height)
                Height = 700;

                Top = GetSettingValue(regKey, "WindowPosTop", Top);
                Left = GetSettingValue(regKey, "WindowPosLeft", Left);

                cboSortOrder.SelectedIndex = GetSettingValue(regKey, "SortOrder", cboSortOrder.SelectedIndex);
                chkSortDescending.Checked = GetSettingValue(regKey, "SortDescending", chkSortDescending.Checked);

                txtFixXRange.Text = GetSettingValue(regKey, "FixXRange", 300).ToString();
                txtFixYRange.Text = GetSettingValue(regKey, "FixYRange", 5000000).ToString();
                txtMinimumSignalToNoise.Text = GetSettingValue(regKey, "MinimumSignalToNoise", 3).ToString();

                chkFixXRange.Checked = GetSettingValue(regKey, "FixXRangeEnabled", true);
                chkFixYRange.Checked = GetSettingValue(regKey, "FixYRangeEnabled", false);
                chkFilterBySignalToNoise.Checked = GetSettingValue(regKey, "FilterBySignalToNoise", false);

                txtMinimumIntensity.Text = GetSettingValue(regKey, "MinimumIntensity", 1000000).ToString();
                txtFilterByMZ.Text = GetSettingValue(regKey, "FilterByMZ", (float)550).ToString(CultureInfo.InvariantCulture);
                txtFilterByMZTol.Text = GetSettingValue(regKey, "FilterByMZTol", (float)0.2).ToString(CultureInfo.InvariantCulture);

                txtAutoStep.Text = GetSettingValue(regKey, "AutoStepInterval", 150).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in RegReadSettings: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        /// Save settings to the registry
        /// </summary>
        private void RegSaveSettings()
        {
            try
            {
                if (txtDataFilePath.Text.Length == 0)
                    return;

                var regKey = Registry.CurrentUser.CreateSubKey($"SOFTWARE\\PNNL PAST Toolkit\\{REG_APP_NAME}\\{REG_SECTION_NAME}");

                if (regKey == null)
                    return;

                regKey.SetValue("DatafilePath", txtDataFilePath.Text, RegistryValueKind.String);
                regKey.SetValue("DatafilePath", txtDataFilePath.Text, RegistryValueKind.String);
                regKey.SetValue("WindowSizeWidth", Width.ToString(), RegistryValueKind.String);
                regKey.SetValue("WindowSizeHeight", Height.ToString(), RegistryValueKind.String);
                regKey.SetValue("WindowPosTop", Top.ToString(), RegistryValueKind.String);
                regKey.SetValue("WindowPosLeft", Left.ToString(), RegistryValueKind.String);

                regKey.SetValue("SortOrder", cboSortOrder.SelectedIndex.ToString(), RegistryValueKind.String);
                regKey.SetValue("SortDescending", chkSortDescending.Checked.ToString(), RegistryValueKind.String);

                if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtFixXRange.Text))
                {
                    regKey.SetValue("FixXRange", int.Parse(txtFixXRange.Text).ToString(), RegistryValueKind.String);
                }

                if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtFixYRange.Text))
                {
                    regKey.SetValue("FixYRange", long.Parse(txtFixYRange.Text).ToString(), RegistryValueKind.String);
                }

                if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtMinimumSignalToNoise.Text))
                {
                    regKey.SetValue("MinimumSignalToNoise", float.Parse(txtMinimumSignalToNoise.Text).ToString(CultureInfo.InvariantCulture),
                        RegistryValueKind.String);
                }

                if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtMinimumIntensity.Text))
                {
                    regKey.SetValue("MinimumIntensity", int.Parse(txtMinimumIntensity.Text).ToString(), RegistryValueKind.String);
                }

                if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtFilterByMZ.Text))
                {
                    regKey.SetValue("FilterByMZ", float.Parse(txtFilterByMZ.Text).ToString(CultureInfo.InvariantCulture),
                        RegistryValueKind.String);
                }

                if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtFilterByMZTol.Text))
                {
                    regKey.SetValue("FilterByMZTol", float.Parse(txtFilterByMZTol.Text).ToString(CultureInfo.InvariantCulture),
                        RegistryValueKind.String);
                }

                if (PRISM.DataUtils.StringToValueUtils.IsNumber(txtAutoStep.Text))
                {
                    regKey.SetValue("AutoStepInterval", int.Parse(txtAutoStep.Text).ToString(), RegistryValueKind.String);
                }

                regKey.SetValue("FixXRangeEnabled", chkFixXRange.Checked.ToString(), RegistryValueKind.String);
                regKey.SetValue("FixYRangeEnabled", chkFixYRange.Checked.ToString(), RegistryValueKind.String);
                regKey.SetValue("FilterBySignalToNoise", chkFilterBySignalToNoise.Checked.ToString(), RegistryValueKind.String);
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
                    dlgOpenFile.InitialDirectory = Directory.GetParent(txtDataFilePath.Text)?.ToString();
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
                    dlgOpenFile.InitialDirectory = Directory.GetParent(txtDataFilePath.Text)?.ToString();
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
            var toolTipControl = new ToolTip();

            toolTipControl.SetToolTip(txtButterworthSamplingFrequency, "Value between 0.01 and 0.99; suggested value is 0.20");
            toolTipControl.SetToolTip(txtSavitzkyGolayFilterOrder, "Even number, 0 or greater; 0 means a moving average filter, 2 means a 2nd order Savitzky Golay filter");
        }

        private void ShowAboutBox()
        {
            var message = new List<string>
            {
                "The MASIC Browser can be used to visualize the SIC's created using MASIC.",
                string.Empty,
                "Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)",
                "Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.",
                string.Empty,
                "This is version " + Application.ProductVersion + " (" + PROGRAM_DATE + ")",
                string.Empty,
                "E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov",
                "Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics",
                string.Empty,
                "Licensed under the Apache License, Version 2.0; you may not use this file except in compliance with the License. ",
                "You may obtain a copy of the License at https://www.apache.org/licenses/LICENSE-2.0",
                string.Empty,
                "Notice: This computer software was prepared by Battelle Memorial Institute, " +
                "hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the " +
                "Department of Energy (DOE).  All rights in the computer software are reserved " +
                "by DOE on behalf of the United States Government and the Contractor as " +
                "provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY " +
                "WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS " +
                "SOFTWARE.  This notice including this sentence must appear on any copies of " +
                "this computer software."
            };

            MessageBox.Show(string.Join(Environment.NewLine, message), "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Sort the data shown in lstParentIonData
        /// </summary>
        private void SortData()
        {
            SortOrderConstants sortMode;

            var scanNumberSaved = 0;

            double minimumIntensity;
            double mzFilter, mzFilterTol;
            double minimumSN;

            if (mParentIonStats.Count == 0)
            {
                mParentIonPointerArray = Array.Empty<int>();
                return;
            }

            try
            {
                if (lstParentIonData.SelectedIndex >= 0)
                {
                    scanNumberSaved = mParentIonStats[mParentIonPointerArray[lstParentIonData.SelectedIndex]].FragScanObserved;
                }
            }
            catch (Exception)
            {
                scanNumberSaved = 1;
            }

            mParentIonPointerArrayCount = 0;
            mParentIonPointerArray = new int[mParentIonStats.Count];
            var sortKeys = new double[mParentIonStats.Count];

            if (cboSortOrder.SelectedIndex is >= 0 and < SORT_ORDER_MODE_COUNT)
            {
                sortMode = (SortOrderConstants)cboSortOrder.SelectedIndex;
            }
            else
            {
                sortMode = SortOrderConstants.SortByScanPeakCenter;
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

            switch (sortMode)
            {
                case SortOrderConstants.SortByPeakIndex:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].Index;
                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByScanPeakCenter:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.ScanNumberMaxIntensity;
                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByScanOptimalPeakCenter:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = double.Parse(mParentIonStats[index].OptimalPeakApexScanNumber + "." + Math.Round(mParentIonStats[index].MZ, 0).ToString("0000") + mParentIonStats[index].Index.ToString("00000"));
                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByMz:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = double.Parse(Math.Round(mParentIonStats[index].MZ, 2) + mParentIonStats[index].SICStats.ScanNumberMaxIntensity.ToString("000000"));
                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByPeakSignalToNoise:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.Peak.SignalToNoiseRatio;
                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByBaselineCorrectedPeakIntensity:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = clsMASICPeakFinder.BaselineAdjustIntensity(mParentIonStats[index].SICStats.Peak, true);

                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByBaselineCorrectedPeakArea:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            {
                                var sicStats = mParentIonStats[index].SICStats;
                                sortKeys[mParentIonPointerArrayCount] = clsMASICPeakFinder.BaselineAdjustArea(sicStats.Peak, sicStats.SICPeakWidthFullScans, true);
                            }

                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByPeakWidth:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            // Create a sort key that is based on both PeakFWHMScans and ScanNumberMaxIntensity by separating the two integers with a "."
                            sortKeys[mParentIonPointerArrayCount] = GetSortKey(
                                mParentIonStats[index].SICStats.Peak.FWHMScanWidth,
                                mParentIonStats[index].SICStats.ScanNumberMaxIntensity);

                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortBySICIntensityMax:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
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

                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByPeakIntensity:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.Peak.MaxIntensityValue;
                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByPeakArea:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.Peak.Area;
                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByFragScanToOptimalLocDistance:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            // Create a sort key that is based on both FragScan-OptimalPeakApexScanNumber and OptimalPeakApexScanNumber by separating the two integers with a "."
                            sortKeys[mParentIonPointerArrayCount] = GetSortKey(
                                mParentIonStats[index].FragScanObserved - mParentIonStats[index].OptimalPeakApexScanNumber,
                                mParentIonStats[index].OptimalPeakApexScanNumber);

                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByPeakCenterToOptimalLocDistance:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            // Create a sort key that is based on both ScanNumberMaxIntensity-OptimalPeakApexScanNumber and OptimalPeakApexScanNumber by separating the two integers with a "."
                            sortKeys[mParentIonPointerArrayCount] = GetSortKey(
                                mParentIonStats[index].SICStats.ScanNumberMaxIntensity - mParentIonStats[index].OptimalPeakApexScanNumber,
                                mParentIonStats[index].OptimalPeakApexScanNumber);

                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByShoulderCount:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            // Create a sort key that is based on both ShoulderCount and OptimalPeakApexScanNumber by separating the two integers with a "."
                            sortKeys[mParentIonPointerArrayCount] = GetSortKey(
                                mParentIonStats[index].SICStats.Peak.ShoulderCount,
                                mParentIonStats[index].OptimalPeakApexScanNumber);

                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByParentIonIntensity:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.Peak.ParentIonIntensity;
                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByPeakSkew:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.Peak.StatisticalMoments.Skew;
                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByKSStat:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = mParentIonStats[index].SICStats.Peak.StatisticalMoments.KSStat;
                            mParentIonPointerArrayCount++;
                        }
                    }

                    break;
                case SortOrderConstants.SortByBaselineNoiseLevel:
                    for (var index = 0; index < mParentIonStats.Count; index++)
                    {
                        var ionStats = mParentIonStats[index];

                        if (SortDataFilterCheck(
                                ionStats.SICStats.Peak.MaxIntensityValue, ionStats.SICStats.Peak.SignalToNoiseRatio, ionStats.MZ,
                                minimumIntensity, minimumSN, mzFilter, mzFilterTol, ionStats.CustomSICPeak))
                        {
                            mParentIonPointerArray[mParentIonPointerArrayCount] = index;
                            sortKeys[mParentIonPointerArrayCount] = GetSortKey(
                                (int)Math.Round(mParentIonStats[index].SICStats.Peak.BaselineNoiseStats.NoiseLevel, 0),
                                mParentIonStats[index].SICStats.Peak.SignalToNoiseRatio);

                            mParentIonPointerArrayCount++;
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

            if (cboSICsTypeFilter.SelectedIndex == (int)SICTypeFilterConstants.CustomSICsOnly)
            {
                useData = isCustomSIC;
            }
            else if (cboSICsTypeFilter.SelectedIndex == (int)SICTypeFilterConstants.NoCustomSICs)
            {
                useData = !isCustomSIC;
            }
            else
            {
                useData = true;
            }

            if (!useData || peakMaxIntensityValue < minimumIntensity)
            {
                return false;
            }

            if (peakSN < minimumSN)
            {
                return false;
            }

            if (mzFilter <= 0)
            {
                return true;
            }

            var mzInRange = Math.Abs(peakMZ - mzFilter) <= mzFilterTol;
            return mzInRange;
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

            mSICPeakFinderOptions.FindPeaksOnSmoothedData = !optDoNotResmooth.Checked;

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

        private bool UpdateSICStats(int parentIonIndex, bool repeatPeakFinding, SmoothModeConstants smoothMode, out clsSICStats sicStats)
        {
            // Copy the original SIC stats found by MASIC into udtSICStats
            // This also includes the original smoothed data

            // Copy the cached SICStats data into udtSICStats
            // We have to separately copy SICSmoothedYData() otherwise VB.NET keeps
            // the array linked in both mParentIonStats().SICStats and udtSICStats
            sicStats = mParentIonStats[parentIonIndex].SICStats.Clone();

            if (smoothMode != SmoothModeConstants.DoNotReSmooth)
            {
                // Re-smooth the data
                var dataFilter = new DataFilter.DataFilter();

                var currentParentIon = mParentIonStats[parentIonIndex];

                sicStats.SICSmoothedYDataIndexStart = 0;

                var intensities = (from item in currentParentIon.SICData select item.Intensity).ToArray();

                if (smoothMode == SmoothModeConstants.SavitzkyGolay)
                {
                    // Resmooth using a Savitzky Golay filter

                    var savitzkyGolayFilterOrder = PRISMWin.TextBoxUtils.ParseTextBoxValueInt(txtSavitzkyGolayFilterOrder, lblSavitzkyGolayFilterOrder.Text + " should be an even number between 0 and 20; assuming 0", out _);
                    var peakWidthsPointsMinimum = PRISMWin.TextBoxUtils.ParseTextBoxValueInt(txtPeakWidthPointsMinimum, lblPeakWidthPointsMinimum.Text + " should be a positive integer; assuming 6", out _, 6);

                    var filterThirdWidth = (int)Math.Floor(peakWidthsPointsMinimum / 3.0);

                    if (filterThirdWidth > 3)
                        filterThirdWidth = 3;

                    // Make sure filterThirdWidth is Odd
                    if (filterThirdWidth % 2 == 0)
                    {
                        filterThirdWidth--;
                    }

                    // Note that the SavitzkyGolayFilter doesn't work right for PolynomialDegree values greater than 0
                    // Also note that a PolynomialDegree value of 0 results in the equivalent of a moving average filter
                    dataFilter.SavitzkyGolayFilter(
                        intensities,
                        0, currentParentIon.SICData.Count - 1,
                        filterThirdWidth, filterThirdWidth,
                        (short)savitzkyGolayFilterOrder, out _, true);
                }
                else
                {
                    // Assume smoothMode = SmoothModeConstants.Butterworth
                    var samplingFrequency = PRISMWin.TextBoxUtils.ParseTextBoxValueFloat(txtButterworthSamplingFrequency, lblButterworthSamplingFrequency.Text + " should be a number between 0.01 and 0.99; assuming 0.2", out _, 0.2F);
                    dataFilter.ButterworthFilter(intensities, 0, currentParentIon.SICData.Count - 1, samplingFrequency);
                }

                // Copy the smoothed data into udtSICStats.SICSmoothedYData
                sicStats.SICSmoothedYData.Clear();

                sicStats.SICSmoothedYData.AddRange(intensities);
            }

            if (repeatPeakFinding)
            {
                // Repeat the finding of the peak in the SIC
                var validPeakFound = FindSICPeakAndAreaForParentIon(parentIonIndex, sicStats);
                return validPeakFound;
            }

            return true;
        }

        private void UpdateStatsAndPlot()
        {
            if (mParentIonPointerArrayCount > 0)
            {
                DisplaySICStats(mParentIonPointerArray[lstParentIonData.SelectedIndex], out var sicStats);
                PlotData(mParentIonPointerArray[lstParentIonData.SelectedIndex], sicStats);
            }
        }

        private string XMLTextReaderGetInnerText(XmlReader xmlReader)
        {
            var value = string.Empty;
            bool success;

            if (xmlReader.NodeType == XmlNodeType.Element)
            {
                // Advance the reader so that we can read the value
                success = xmlReader.Read();
            }
            else
            {
                success = true;
            }

            if (success && xmlReader.NodeType != XmlNodeType.Whitespace && xmlReader.HasValue)
            {
                value = xmlReader.Value;
            }

            return value;
        }

        private void XMLTextReaderSkipWhitespace(XmlReader xmlReader)
        {
            if (xmlReader.NodeType == XmlNodeType.Whitespace)
            {
                // Whitespace; read the next node
                xmlReader.Read();
            }
        }

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
                // See http://www.physics.udel.edu/~watson/scen103/ascii.html for reference
                switch ((int)e.KeyChar)
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
                // See http://www.physics.udel.edu/~watson/scen103/ascii.html for reference
                switch ((int)e.KeyChar)
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
                // See http://www.physics.udel.edu/~watson/scen103/ascii.html for reference
                switch ((int)e.KeyChar)
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

        private void frmBrowser_Closing(object sender, FormClosingEventArgs e)
        {
            RegSaveSettings();
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
