using System;
using System.Collections.Generic;

// -------------------------------------------------------------------------------
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
// Program started October 11, 2003
// Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
// Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
// -------------------------------------------------------------------------------
//
// Licensed under the 2-Clause BSD License; you may not use this file except
// in compliance with the License.  You may obtain a copy of the License at
// https://opensource.org/licenses/BSD-2-Clause

using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MASIC.DataInput;
using PRISM;
using PRISM.FileProcessor;
using PRISMDatabaseUtils;
using PRISMWin;
using ProgressFormNET;
using ShFolderBrowser.FolderBrowser;

namespace MASIC
{
    public partial class frmMain : Form
    {
        public frmMain() : base()
        {
            // This call is required by the Windows Form Designer.
            InitializeComponent();

            // Add any initialization after the InitializeComponent() call

            mCacheOptions = new clsSpectrumCacheOptions();
            mDefaultCustomSICList = new List<udtCustomSICEntryType>();
            mLogMessages = new List<string>();
            mReporterIonIndexToModeMap = new Dictionary<int, clsReporterIons.eReporterIonMassModeConstants>();
            InitializeControls();
            mMasic = new clsMASIC();
            RegisterEvents(mMasic);
        }

        #region "Constants and Enums"

        private const string XML_SETTINGS_FILE_NAME = "MASICParameters.xml";

        private const string CUSTOM_SIC_VALUES_DATA_TABLE = "PeakMatchingThresholds";

        private const string COL_NAME_MZ = "MZ";
        private const string COL_NAME_MZ_TOLERANCE = "MZToleranceDa";
        private const string COL_NAME_SCAN_CENTER = "Scan_Center";
        private const string COL_NAME_SCAN_TOLERANCE = "Scan_Tolerance";
        private const string COL_NAME_SCAN_COMMENT = "Scan_Comment";
        private const string COL_NAME_CUSTOM_SIC_VALUE_ROW_ID = "UniqueRowID";

        #endregion

        #region "Structures"

        private struct udtCustomSICEntryType
        {
            public double MZ;
            public float ScanCenter;
            public string Comment;
        }

        #endregion

        #region "Classwide Variables"

        private DataSet mCustomSICValuesDataset;

        private readonly List<udtCustomSICEntryType> mDefaultCustomSICList;
        private bool mWorking;

        private string mXmlSettingsFilePath;
        private string mPreferredInputFileExtension;

        private readonly clsSpectrumCacheOptions mCacheOptions;

        private bool mSuppressNoParentIonsError;
        private bool mCompressMSSpectraData;
        private bool mCompressMSMSSpectraData;

        private double mCompressToleranceDivisorForDa;
        private double mCompressToleranceDivisorForPPM;

        private int mHeightAdjustForce;
        private DateTime mHeightAdjustTime;

        private clsMASIC mMasic;
        private frmProgress mProgressForm;

        /// <summary>
        /// Log messages, including warnings and errors, with the newest message at the top
        /// </summary>
        private readonly List<string> mLogMessages;

        private readonly Dictionary<int, clsReporterIons.eReporterIonMassModeConstants> mReporterIonIndexToModeMap;

        #endregion

        #region "Properties"

        private clsReporterIons.eReporterIonMassModeConstants SelectedReporterIonMode
        {
            get
            {
                var reporterIonMode = GetSelectedReporterIonMode();
                return reporterIonMode;
            }
            set
            {
                try
                {
                    int targetIndex = GetReporterIonIndexFromMode(value);
                    cboReporterIonMassMode.SelectedIndex = targetIndex;
                }
                catch (Exception ex)
                {
                    // Ignore errors here
                }
            }
        }

        #endregion

        #region "Procedures"

        private void AddCustomSICRow(
            double mz,
            double mzToleranceDa,
            float scanOrAcqTimeCenter,
            float scanOrAcqTimeTolerance,
            string comment,
            out bool existingRowFound)
        {
            existingRowFound = false;
            foreach (DataRow myDataRow in mCustomSICValuesDataset.Tables[CUSTOM_SIC_VALUES_DATA_TABLE].Rows)
            {
                if (Math.Abs(double.Parse(myDataRow[0].ToString()) - mz) < float.Epsilon & Math.Abs(float.Parse(myDataRow[1].ToString()) - scanOrAcqTimeCenter) < float.Epsilon)
                {
                    existingRowFound = true;
                    break;
                }
            }

            if (comment == null)
                comment = string.Empty;

            if (!existingRowFound)
            {
                var newDataRow = mCustomSICValuesDataset.Tables[CUSTOM_SIC_VALUES_DATA_TABLE].NewRow();
                newDataRow[0] = Math.Round(mz, 4);
                newDataRow[1] = Math.Round(mzToleranceDa, 4);
                newDataRow[2] = Math.Round(scanOrAcqTimeCenter, 6);
                newDataRow[3] = Math.Round(scanOrAcqTimeTolerance, 6);
                newDataRow[4] = comment;
                mCustomSICValuesDataset.Tables[CUSTOM_SIC_VALUES_DATA_TABLE].Rows.Add(newDataRow);
            }
        }

        private void AppendCustomSICListItem(double mz, float scanCenter, string comment)
        {
            var customSicEntryItem = new udtCustomSICEntryType()
            {
                MZ = mz,
                ScanCenter = scanCenter,
                Comment = comment
            };
            mDefaultCustomSICList.Add(customSicEntryItem);
        }

        private void AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants reporterIonMassMode, string description)
        {
            cboReporterIonMassMode.Items.Add(description);
            int currentIndex = cboReporterIonMassMode.Items.Count - 1;
            if (mReporterIonIndexToModeMap.ContainsKey(currentIndex))
            {
                mReporterIonIndexToModeMap[currentIndex] = reporterIonMassMode;
            }
            else
            {
                mReporterIonIndexToModeMap.Add(currentIndex, reporterIonMassMode);
            }
        }

        private void AppendToLog(EventLogEntryType messageType, string message)
        {
            if (message.StartsWith("ProcessingStats") || message.StartsWith("Parameter file not specified"))
            {
                return;
            }

            string textToAppend;
            bool doEvents = false;
            switch (messageType)
            {
                case EventLogEntryType.Error:
                    textToAppend = "Error: " + message;
                    tbsOptions.SelectTab(tbsOptions.TabCount - 1);
                    doEvents = true;
                    break;
                case EventLogEntryType.Warning:
                    textToAppend = "Warning: " + message;
                    tbsOptions.SelectTab(tbsOptions.TabCount - 1);
                    doEvents = true;
                    break;
                default:
                    // Includes Case EventLogEntryType.Information
                    textToAppend = message;
                    break;
            }

            mLogMessages.Insert(0, textToAppend);
            txtLogMessages.AppendText(textToAppend + Environment.NewLine);
            txtLogMessages.ScrollToCaret();
            if (doEvents)
            {
                Application.DoEvents();
            }
        }

        private void AutoPopulateCustomSICValues(bool confirmReplaceExistingResults)
        {
            var defaultMZTolerance = default(double);
            var defaultScanOrAcqTimeTolerance = default(float);
            GetCurrentCustomSICTolerances(ref defaultMZTolerance, ref defaultScanOrAcqTimeTolerance);
            if (defaultScanOrAcqTimeTolerance > 1)
            {
                defaultScanOrAcqTimeTolerance = 0.6F;
            }

            if (ClearCustomSICList(confirmReplaceExistingResults))
            {
                // The default values use relative times, so make sure that mode is enabled
                SetCustomSICToleranceType(clsCustomSICList.eCustomSICScanTypeConstants.Relative);
                txtCustomSICScanOrAcqTimeTolerance.Text = defaultScanOrAcqTimeTolerance.ToString();
                foreach (var item in mDefaultCustomSICList)
                {
                    AddCustomSICRow(item.MZ, defaultMZTolerance, item.ScanCenter, defaultScanOrAcqTimeTolerance, item.Comment, out _);
                }
            }
        }

        private bool updating = false;

        private void CatchUnrequestedHeightChange()
        {
            if (!updating)
            {
                if (mHeightAdjustForce != 0 && DateTime.UtcNow.Subtract(mHeightAdjustTime).TotalSeconds <= 5.0)
                {
                    try
                    {
                        updating = true;
                        Height = mHeightAdjustForce;
                        mHeightAdjustForce = 0;
                        mHeightAdjustTime = DateTime.Parse("1900-01-01");
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        updating = false;
                    }
                }
            }
        }

        private void AutoToggleReporterIonStatsEnabled()
        {
            if (SelectedReporterIonMode == clsReporterIons.eReporterIonMassModeConstants.CustomOrNone)
            {
                if (chkReporterIonStatsEnabled.Checked)
                {
                    chkReporterIonStatsEnabled.Checked = false;
                }
            }
            else if (!chkReporterIonStatsEnabled.Checked)
            {
                chkReporterIonStatsEnabled.Checked = true;
            }
        }

        private void AutoToggleReporterIonStatsMode()
        {
            if (chkReporterIonStatsEnabled.Checked)
            {
                if (SelectedReporterIonMode == clsReporterIons.eReporterIonMassModeConstants.CustomOrNone)
                {
                    SelectedReporterIonMode = clsReporterIons.eReporterIonMassModeConstants.ITraqFourMZ;
                }
            }
            else if (SelectedReporterIonMode != clsReporterIons.eReporterIonMassModeConstants.CustomOrNone)
            {
                SelectedReporterIonMode = clsReporterIons.eReporterIonMassModeConstants.CustomOrNone;
            }
        }

        private void ClearAllRangeFilters()
        {
            txtScanStart.Text = "0";
            txtScanEnd.Text = "0";
            txtTimeStart.Text = "0";
            txtTimeEnd.Text = "0";
        }

        private bool ClearCustomSICList(bool confirmReplaceExistingResults)
        {
            // Returns true if the CUSTOM_SIC_VALUES_DATA_TABLE is empty or if it was cleared
            // Returns false if the user is queried about clearing and they do not click Yes

            var eResult = default(DialogResult);

            if (mCustomSICValuesDataset.Tables[CUSTOM_SIC_VALUES_DATA_TABLE].Rows.Count > 0)
            {
                if (confirmReplaceExistingResults)
                {
                    eResult = MessageBox.Show("Are you sure you want to clear the Custom SIC list?", "Clear Custom SICs", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                }

                if (eResult == DialogResult.Yes || !confirmReplaceExistingResults)
                {
                    mCustomSICValuesDataset.Tables[CUSTOM_SIC_VALUES_DATA_TABLE].Rows.Clear();
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        private bool ConfirmPaths()
        {
            if (txtInputFilePath.TextLength == 0)
            {
                MessageBox.Show("Please define an input file path", "Missing Value", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtInputFilePath.Focus();
                return false;
            }
            else if (txtOutputDirectoryPath.TextLength == 0)
            {
                MessageBox.Show("Please define an output directory path", "Missing Value", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtOutputDirectoryPath.Focus();
                return false;
            }
            else
            {
                return true;
            }
        }

        private string CStrSafe(object item)
        {
            try
            {
                if (item == null)
                {
                    return string.Empty;
                }
                else if (Convert.IsDBNull(item))
                {
                    return string.Empty;
                }
                else
                {
                    return Convert.ToString(item);
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        private void DefineDefaultCustomSICList()
        {
            mDefaultCustomSICList.Clear();

            this.AppendCustomSICListItem(824.47422, 0.176F, "Pep-09");
            this.AppendCustomSICListItem(412.74102, 0.176F, "Pep-09");
            this.AppendCustomSICListItem(484.28137, 0.092F, "Pep-11");
            this.AppendCustomSICListItem(459.27687, 0.368F, "Pep-14");
            this.AppendCustomSICListItem(740.01082, 0.574F, "Pep-16");
            this.AppendCustomSICListItem(762.51852, 0.642F, "Pep-26");
            this.AppendCustomSICListItem(657.42992, 0.192F, "Pep-16_Partial");
            this.AppendCustomSICListItem(900.59222, 0.4F, "Pep-26_PartialA");
            this.AppendCustomSICListItem(640.43972, 0.4F, "Pep-26_PartialB");
        }

        private void DefineOverviewText()
        {
            var msg = new StringBuilder();

            msg.Append("When Export All Spectra Data Points is enabled, a separate file is created containing the raw data points (scan number, m/z, and intensity), using the specified file format. ");
            msg.Append("If Export MS/MS Spectra is enabled, then the fragmentation spectra are included, in addition to the survey scan spectra (MS1 scans). ");
            msg.Append("If MS/MS spectra are not included, then one can optionally renumber the survey scan spectra so that they increase in steps of 1, regardless of the number of MS/MS scans between each survey scan. ");
            msg.Append("The Minimum Intensity and Maximum Ion Count options allow you to limit the number of data points exported for each spectrum.");

            lblRawDataExportOverview.Text = msg.ToString();

            msg.Clear();
            msg.Append("These options control how the selected ion chromatogram (SIC) is created for each parent ion mass or custom SIC search mass. ");
            msg.Append("The data in the survey scan spectra (MS1 scans) are searched +/- the SIC Tolerance, looking forward and backward in time until ");
            msg.Append("the intensity of the matching data 1) falls below the Intensity Threshold Fraction Max Peak value, 2) falls below the Intensity ");
            msg.Append("Threshold Absolute Minimum, or 3) spans more than the Maximum Peak Width forward or backward limits defined.");

            lblSICOptionsOverview.Text = msg.ToString();

            msg.Clear();
            msg.Append("When processing Thermo MRM data files, a file named _MRMSettings.txt will be created listing the ");
            msg.Append("parent and daughter m/z values monitored via SRM. ");
            msg.Append("You can optionally export detailed MRM intensity data using these options:");
            lblMRMInfo.Text = msg.ToString();

            msg.Clear();
            msg.Append("Select a comma or tab delimited file to read custom SIC search values from, ");
            msg.Append("or define them in the Custom SIC Values table below.  If using the file, ");
            msg.Append("allowed column names are: " + clsCustomSICListReader.GetCustomMZFileColumnHeaders() + ".  ");
            msg.Append("Note: use " +
                clsCustomSICListReader.CUSTOM_SIC_COLUMN_SCAN_TIME + " and " +
                clsCustomSICListReader.CUSTOM_SIC_COLUMN_TIME_TOLERANCE + " only when specifying ");

            msg.Append("acquisition time-based values.  When doing so, do not include " +
                clsCustomSICListReader.CUSTOM_SIC_COLUMN_SCAN_CENTER + " and " +
                clsCustomSICListReader.CUSTOM_SIC_COLUMN_SCAN_TOLERANCE + ".");

            txtCustomSICFileDescription.Text = msg.ToString();
        }

        private void EnableDisableControls()
        {
            bool rawExportEnabled;

            bool createSICsAndRawData = !chkSkipSICAndRawDataProcessing.Checked;
            bool msmsProcessingEnabled = !chkSkipMSMSProcessing.Checked;
            bool exportRawDataOnly = chkExportRawDataOnly.Checked && chkExportRawSpectraData.Checked;

            chkSkipMSMSProcessing.Enabled = createSICsAndRawData;
            chkExportRawDataOnly.Enabled = createSICsAndRawData && chkExportRawSpectraData.Checked;

            fraExportAllSpectraDataPoints.Enabled = createSICsAndRawData;

            fraSICNoiseThresholds.Enabled = createSICsAndRawData && !exportRawDataOnly;
            fraPeakFindingOptions.Enabled = fraSICNoiseThresholds.Enabled;
            fraSmoothingOptions.Enabled = fraSICNoiseThresholds.Enabled;
            fraSICSearchThresholds.Enabled = fraSICNoiseThresholds.Enabled;

            fraMassSpectraNoiseThresholds.Enabled = createSICsAndRawData;

            fraBinningIntensityOptions.Enabled = createSICsAndRawData && msmsProcessingEnabled && !exportRawDataOnly;
            fraBinningMZOptions.Enabled = fraBinningIntensityOptions.Enabled;
            fraSpectrumSimilarityOptions.Enabled = fraBinningIntensityOptions.Enabled;

            fraCustomSICControls.Enabled = createSICsAndRawData && !exportRawDataOnly;
            dgCustomSICValues.Enabled = createSICsAndRawData && !exportRawDataOnly;

            rawExportEnabled = chkExportRawSpectraData.Checked;

            cboExportRawDataFileFormat.Enabled = rawExportEnabled;
            chkExportRawDataIncludeMSMS.Enabled = rawExportEnabled;

            if (chkExportRawDataIncludeMSMS.Checked)
            {
                chkExportRawDataRenumberScans.Enabled = false;
            }
            else
            {
                chkExportRawDataRenumberScans.Enabled = rawExportEnabled;
            }

            txtExportRawDataSignalToNoiseRatioMinimum.Enabled = rawExportEnabled;
            txtExportRawDataMaxIonCountPerScan.Enabled = rawExportEnabled;
            txtExportRawDataIntensityMinimum.Enabled = rawExportEnabled;

            if (cboSICNoiseThresholdMode.SelectedIndex == (int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold)
            {
                txtSICNoiseThresholdIntensity.Enabled = true;
                txtSICNoiseFractionLowIntensityDataToAverage.Enabled = false;
            }
            else if (cboSICNoiseThresholdMode.SelectedIndex == (int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByAbundance ||
                cboSICNoiseThresholdMode.SelectedIndex == (int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByCount ||
                cboSICNoiseThresholdMode.SelectedIndex == (int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance)
            {
                txtSICNoiseThresholdIntensity.Enabled = false;
                txtSICNoiseFractionLowIntensityDataToAverage.Enabled = true;
            }
            else
            {
                // Unknown mode; disable both
                txtSICNoiseThresholdIntensity.Enabled = false;
                txtSICNoiseFractionLowIntensityDataToAverage.Enabled = false;
            }

            txtButterworthSamplingFrequency.Enabled = optUseButterworthSmooth.Checked;
            txtSavitzkyGolayFilterOrder.Enabled = optUseSavitzkyGolaySmooth.Checked;

            if (cboMassSpectraNoiseThresholdMode.SelectedIndex == (int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold)
            {
                txtMassSpectraNoiseThresholdIntensity.Enabled = true;
                txtMassSpectraNoiseFractionLowIntensityDataToAverage.Enabled = false;
                txtMassSpectraNoiseMinimumSignalToNoiseRatio.Enabled = false;
            }
            else if (cboMassSpectraNoiseThresholdMode.SelectedIndex == (int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByAbundance ||
                cboMassSpectraNoiseThresholdMode.SelectedIndex == (int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByCount ||
                cboMassSpectraNoiseThresholdMode.SelectedIndex == (int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance)
            {
                txtMassSpectraNoiseThresholdIntensity.Enabled = false;
                txtMassSpectraNoiseFractionLowIntensityDataToAverage.Enabled = true;
                txtMassSpectraNoiseMinimumSignalToNoiseRatio.Enabled = true;
            }
            else
            {
                // Unknown mode; disable both
                txtMassSpectraNoiseThresholdIntensity.Enabled = false;
                txtMassSpectraNoiseFractionLowIntensityDataToAverage.Enabled = false;
                txtMassSpectraNoiseMinimumSignalToNoiseRatio.Enabled = false;
            }

            chkSaveExtendedStatsFileIncludeFilterText.Enabled = chkSaveExtendedStatsFile.Checked;
            chkSaveExtendedStatsFileIncludeStatusLog.Enabled = chkSaveExtendedStatsFile.Checked;
            txtStatusLogKeyNameFilterList.Enabled = chkSaveExtendedStatsFile.Checked && chkSaveExtendedStatsFileIncludeStatusLog.Checked;

            chkConsolidateConstantExtendedHeaderValues.Enabled = chkSaveExtendedStatsFile.Checked;

            EnableDisableCustomSICValueGrid();
        }

        private void EnableDisableCustomSICValueGrid()
        {
            bool enableGrid;

            if (txtCustomSICFileName.TextLength > 0)
            {
                enableGrid = false;
                dgCustomSICValues.CaptionText = "Custom SIC Values will be read from the file defined above";
            }
            else
            {
                enableGrid = true;
                dgCustomSICValues.CaptionText = "Custom SIC Values";
            }

            cmdPasteCustomSICList.Enabled = enableGrid;
            cmdCustomSICValuesPopulate.Enabled = enableGrid;
            cmdClearCustomSICList.Enabled = enableGrid;
            dgCustomSICValues.Enabled = enableGrid;
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            CatchUnrequestedHeightChange();
        }

        private void GetCurrentCustomSICTolerances(ref double defaultMZTolerance, ref float defaultScanOrAcqTimeTolerance)
        {
            try
            {
                defaultMZTolerance = double.Parse(txtSICTolerance.Text);

                if (optSICTolerancePPM.Checked)
                {
                    defaultMZTolerance = clsUtilities.PPMToMass(defaultMZTolerance, 1000);
                }
            }
            catch (Exception ex)
            {
                defaultMZTolerance = 0.6;
            }

            try
            {
                defaultScanOrAcqTimeTolerance = float.Parse(txtCustomSICScanOrAcqTimeTolerance.Text);
            }
            catch (Exception ex)
            {
                defaultScanOrAcqTimeTolerance = 0;
            }
        }

        private clsCustomSICList.eCustomSICScanTypeConstants GetCustomSICScanToleranceType()
        {
            if (optCustomSICScanToleranceAbsolute.Checked)
            {
                return clsCustomSICList.eCustomSICScanTypeConstants.Absolute;
            }
            else if (optCustomSICScanToleranceRelative.Checked)
            {
                return clsCustomSICList.eCustomSICScanTypeConstants.Relative;
            }
            else if (optCustomSICScanToleranceAcqTime.Checked)
            {
                return clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime;
            }
            else
            {
                // Assume absolute
                return clsCustomSICList.eCustomSICScanTypeConstants.Absolute;
            }
        }

        private int GetReporterIonIndexFromMode(clsReporterIons.eReporterIonMassModeConstants reporterIonMassMode)
        {
            foreach (var item in mReporterIonIndexToModeMap)
            {
                if (item.Value == reporterIonMassMode)
                {
                    return item.Key;
                }
            }

            throw new InvalidEnumArgumentException("Dictionary mReporterIonIndexToModeMap is missing enum " + reporterIonMassMode);
        }

        private clsReporterIons.eReporterIonMassModeConstants GetReporterIonModeFromIndex(int comboboxIndex)
        {
            clsReporterIons.eReporterIonMassModeConstants reporterIonMassMode;
            if (mReporterIonIndexToModeMap.TryGetValue(comboboxIndex, out reporterIonMassMode))
            {
                return reporterIonMassMode;
            }

            throw new Exception("Dictionary mReporterIonIndexToModeMap is missing index " + comboboxIndex);
        }

        private clsReporterIons.eReporterIonMassModeConstants GetSelectedReporterIonMode()
        {
            return GetReporterIonModeFromIndex(cboReporterIonMassMode.SelectedIndex);
        }

        private string GetSettingsFilePath()
        {
            return ProcessFilesOrDirectoriesBase.GetSettingsFilePathLocal("MASIC", XML_SETTINGS_FILE_NAME);
        }

        private void IniFileLoadOptions(bool updateIOPaths)
        {
            // Prompts the user to select a file to load the options from

            string filePath;

            using (var objOpenFile = new OpenFileDialog()
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".xml",
                DereferenceLinks = true,
                Multiselect = false,
                ValidateNames = true,
                Filter = "Settings files (*.xml)|*.xml|All files (*.*)|*.*",
                FilterIndex = 1
            })
            {
                filePath = mXmlSettingsFilePath;

                if (filePath.Length > 0)
                {
                    try
                    {
                        objOpenFile.InitialDirectory = Directory.GetParent(filePath).ToString();
                    }
                    catch
                    {
                        objOpenFile.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                    }
                }
                else
                {
                    objOpenFile.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                }

                if (File.Exists(filePath))
                {
                    objOpenFile.FileName = Path.GetFileName(filePath);
                }

                objOpenFile.Title = "Specify file to load options from";

                var result = objOpenFile.ShowDialog();
                if (result == DialogResult.Cancel)
                    return;

                if (objOpenFile.FileName.Length > 0)
                {
                    mXmlSettingsFilePath = objOpenFile.FileName;

                    IniFileLoadOptions(mXmlSettingsFilePath, updateIOPaths);
                }
            }
        }

        private void IniFileLoadOptions(string filePath, bool updateIOPaths)
        {
            // Loads options from the given file

            try
            {
                // Utilize MASIC's built-in LoadParameters function, then call ResetToDefaults
                var masicInstance = new clsMASIC();

                bool success = masicInstance.LoadParameterFileSettings(filePath);
                if (!success)
                {
                    MessageBox.Show("LoadParameterFileSettings returned false for: " + Path.GetFileName(filePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }

                ResetToDefaults(false, masicInstance);

                // Sleep for 100 msec, just to be safe
                Thread.Sleep(100);

                // Now load some custom options that aren't loaded by clsMASIC
                var objXmlFile = new XmlSettingsFileAccessor();

                // Pass True to .LoadSettings() to turn off case sensitive matching
                objXmlFile.LoadSettings(filePath, false);

                try
                {
                    txtDatasetLookupFilePath.Text = objXmlFile.GetParam(clsMASICOptions.XML_SECTION_DATABASE_SETTINGS, "DatasetLookupFilePath", txtDatasetLookupFilePath.Text);
                    try
                    {
                        if (!File.Exists(txtDatasetLookupFilePath.Text))
                        {
                            txtDatasetLookupFilePath.Text = string.Empty;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Ignore any errors here
                    }

                    if (updateIOPaths)
                    {
                        txtInputFilePath.Text = objXmlFile.GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "InputFilePath", txtInputFilePath.Text);
                    }

                    Width = objXmlFile.GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowWidth", Width);
                    Height = objXmlFile.GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowHeight", Height);

                    if (updateIOPaths)
                    {
                        txtOutputDirectoryPath.Text = objXmlFile.GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "LastDirectory", txtOutputDirectoryPath.Text);
                    }

                    if (txtOutputDirectoryPath.TextLength == 0)
                    {
                        txtOutputDirectoryPath.Text = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                    }

                    mPreferredInputFileExtension = objXmlFile.GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "PreferredInputFileExtension", mPreferredInputFileExtension);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid parameter in settings file: " + Path.GetFileName(filePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading settings from file: " + filePath + "; " + Environment.NewLine +
                    ex.Message + ";" + Environment.NewLine, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void IniFileSaveDefaultOptions()
        {
            DialogResult eResponse;

            eResponse = MessageBox.Show("Save the current options as defaults?", "Save Defaults", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (eResponse == DialogResult.Yes)
            {
                IniFileSaveOptions(GetSettingsFilePath(), false);
            }
        }

        private void IniFileSaveOptions()
        {
            // Prompts the user to select a file to load the options from

            string filePath;

            using (var objSaveFile = new SaveFileDialog()
            {
                AddExtension = true,
                CheckFileExists = false,
                CheckPathExists = true,
                DefaultExt = ".xml",
                DereferenceLinks = true,
                OverwritePrompt = true,
                ValidateNames = true,
                Filter = "Settings files (*.xml)|*.xml|All files (*.*)|*.*",
                FilterIndex = 1
            })
            {
                filePath = mXmlSettingsFilePath;
                if (filePath.Length > 0)
                {
                    try
                    {
                        objSaveFile.InitialDirectory = Directory.GetParent(filePath).ToString();
                    }
                    catch
                    {
                        objSaveFile.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                    }
                }
                else
                {
                    objSaveFile.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                }

                if (File.Exists(filePath))
                {
                    objSaveFile.FileName = Path.GetFileName(filePath);
                }

                objSaveFile.Title = "Specify file to save options to";

                var result = objSaveFile.ShowDialog();
                if (result == DialogResult.Cancel)
                    return;

                if (objSaveFile.FileName.Length > 0)
                {
                    mXmlSettingsFilePath = objSaveFile.FileName;

                    IniFileSaveOptions(mXmlSettingsFilePath, false);
                }
            }
        }

        private void IniFileSaveOptions(string filePath, bool saveWindowDimensionsOnly = false)
        {
            try
            {
                if (!saveWindowDimensionsOnly)
                {
                    var objMasic = new clsMASIC();

                    UpdateMasicSettings(ref objMasic);

                    objMasic.Options.SaveParameterFileSettings(filePath);

                    // Sleep for 100 msec, just to be safe
                    Thread.Sleep(100);
                }

                // Pass True to .LoadSettings() here so that newly made Xml files will have the correct capitalization
                var objXmlFile = new XmlSettingsFileAccessor();

                objXmlFile.LoadSettings(filePath, true);

                try
                {
                    if (!saveWindowDimensionsOnly)
                    {
                        try
                        {
                            if (File.Exists(txtDatasetLookupFilePath.Text))
                            {
                                objXmlFile.SetParam(clsMASICOptions.XML_SECTION_DATABASE_SETTINGS, "DatasetLookupFilePath", txtDatasetLookupFilePath.Text);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Ignore any errors here
                        }

                        objXmlFile.SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "InputFilePath", txtInputFilePath.Text);
                    }

                    objXmlFile.SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "LastDirectory", txtOutputDirectoryPath.Text);
                    objXmlFile.SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "PreferredInputFileExtension", mPreferredInputFileExtension);

                    objXmlFile.SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowWidth", Width);
                    objXmlFile.SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowHeight", Height);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error storing parameter in settings file: " + Path.GetFileName(filePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }

                objXmlFile.SaveSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving settings to file: " + filePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void InitializeControls()
        {
            DefineDefaultCustomSICList();

            PopulateComboBoxes();

            InitializeCustomSICDataGrid();

            DefineOverviewText();

            mXmlSettingsFilePath = GetSettingsFilePath();
            ProcessFilesOrDirectoriesBase.CreateSettingsFileIfMissing(mXmlSettingsFilePath);

            mPreferredInputFileExtension = ".Raw";

            mHeightAdjustForce = 0;
            mHeightAdjustTime = DateTime.Parse("1900-01-01");

            IniFileLoadOptions(mXmlSettingsFilePath, true);
            SetToolTips();
        }

        private void InitializeCustomSICDataGrid()
        {
            // Make the Peak Matching Thresholds data table
            var customSICValues = new DataTable(CUSTOM_SIC_VALUES_DATA_TABLE);

            // Add the columns to the data table
            DataTableUtils.AppendColumnDoubleToTable(customSICValues, COL_NAME_MZ);
            DataTableUtils.AppendColumnDoubleToTable(customSICValues, COL_NAME_MZ_TOLERANCE);
            DataTableUtils.AppendColumnDoubleToTable(customSICValues, COL_NAME_SCAN_CENTER);
            DataTableUtils.AppendColumnDoubleToTable(customSICValues, COL_NAME_SCAN_TOLERANCE);
            DataTableUtils.AppendColumnStringToTable(customSICValues, COL_NAME_SCAN_COMMENT, string.Empty);
            DataTableUtils.AppendColumnIntegerToTable(customSICValues, COL_NAME_CUSTOM_SIC_VALUE_ROW_ID, 0, true, true);

            var primaryKeyColumn = new DataColumn[] { customSICValues.Columns[COL_NAME_CUSTOM_SIC_VALUE_ROW_ID] };
            customSICValues.PrimaryKey = primaryKeyColumn;

            // Instantiate the dataset
            mCustomSICValuesDataset = new DataSet(CUSTOM_SIC_VALUES_DATA_TABLE);

            // Add the new DataTable to the DataSet.
            mCustomSICValuesDataset.Tables.Add(customSICValues);

            // Bind the DataSet to the DataGrid
            dgCustomSICValues.DataSource = mCustomSICValuesDataset;
            dgCustomSICValues.DataMember = CUSTOM_SIC_VALUES_DATA_TABLE;

            // Update the grid's table style
            UpdateCustomSICDataGridTableStyle();

            // Populate the table
            AutoPopulateCustomSICValues(false);
        }

        private void PasteCustomSICValues(bool clearList)
        {
            var lineDelimiters = new char[] { '\r', '\n' };
            var columnDelimiters = new char[] { '\t', ',' };

            // Examine the clipboard contents
            var objData = Clipboard.GetDataObject();

            if (objData == null)
            {
                return;
            }

            if (!objData.GetDataPresent(DataFormats.StringFormat, true))
            {
                return;
            }

            string data = Convert.ToString(objData.GetData(DataFormats.StringFormat, true));

            // Split data on carriage return or line feed characters
            // Lines that end in CrLf will give two separate lines; one with the text, and one blank; that's OK
            var dataLines = data.Split(lineDelimiters, 50000);

            if (dataLines.Length == 0)
            {
                return;
            }

            var defaultMZTolerance = default(double);
            var defaultScanOrAcqTimeTolerance = default(float);

            GetCurrentCustomSICTolerances(ref defaultMZTolerance, ref defaultScanOrAcqTimeTolerance);

            if (clearList)
            {
                if (!ClearCustomSICList(true))
                    return;
            }

            int rowsAlreadyPresent = 0;
            int rowsSkipped = 0;

            foreach (var dataLine in dataLines)
            {
                if (string.IsNullOrWhiteSpace(dataLine))
                {
                    continue;
                }

                var columns = dataLine.Split(columnDelimiters, 5);
                if (columns.Length < 2)
                {
                    rowsSkipped += 1;
                    continue;
                }

                try
                {
                    double mz = 0;
                    var scanOrAcqTime = default(float);
                    string comment = string.Empty;
                    double mzToleranceDa = defaultMZTolerance;
                    float scanOrAcqTimeTolerance = defaultScanOrAcqTimeTolerance;

                    if (columns.Length == 2)
                    {
                        // Assume pasted data is m/z and scan
                        mz = double.Parse(columns[0]);
                        scanOrAcqTime = float.Parse(columns[1]);
                    }
                    else if (columns.Length >= 3 && columns[2].Length > 0 &&
                        !PRISM.DataUtils.StringToValueUtils.IsNumber(columns[2][0].ToString()))
                    {
                        // Assume pasted data is m/z, scan, and comment
                        mz = double.Parse(columns[0]);
                        scanOrAcqTime = float.Parse(columns[1]);
                        comment = columns[2];
                    }
                    else if (columns.Length > 2)
                    {
                        // Assume pasted data is m/z, m/z tolerance, scan, scan tolerance, and comment
                        mz = double.Parse(columns[0]);
                        mzToleranceDa = double.Parse(columns[1]);
                        if (Math.Abs(mzToleranceDa) < float.Epsilon)
                        {
                            mzToleranceDa = defaultMZTolerance;
                        }

                        scanOrAcqTime = float.Parse(columns[2]);

                        if (columns.Length >= 4)
                        {
                            scanOrAcqTimeTolerance = float.Parse(columns[3]);
                        }
                        else
                        {
                            scanOrAcqTimeTolerance = defaultScanOrAcqTimeTolerance;
                        }

                        if (columns.Length >= 5)
                        {
                            comment = columns[4];
                        }
                        else
                        {
                            comment = string.Empty;
                        }
                    }

                    if (mz > 0)
                    {
                        bool existingRowFound = false;
                        AddCustomSICRow(mz, mzToleranceDa, scanOrAcqTime, scanOrAcqTimeTolerance, comment,
                                        out existingRowFound);

                        if (existingRowFound)
                        {
                            rowsAlreadyPresent += 1;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Skip this row
                    rowsSkipped += 1;
                }
            }

            if (rowsAlreadyPresent > 0)
            {
                string message;
                if (rowsAlreadyPresent == 1)
                {
                    message = "1 row of thresholds was";
                }
                else
                {
                    message = rowsAlreadyPresent.ToString() + " rows of thresholds were";
                }

                MessageBox.Show(message + " already present in the table; duplicate rows are not allowed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            if (rowsSkipped > 0)
            {
                string message;
                if (rowsSkipped == 1)
                {
                    message = "1 row was skipped because it";
                }
                else
                {
                    message = rowsSkipped.ToString() + " rows were skipped because they";
                }

                MessageBox.Show(message + " didn't contain two columns of numeric data.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void PopulateComboBoxes()
        {
            cboExportRawDataFileFormat.Items.Clear();
            cboExportRawDataFileFormat.Items.Insert((int)clsRawDataExportOptions.eExportRawDataFileFormatConstants.PEKFile, "PEK File");
            cboExportRawDataFileFormat.Items.Insert((int)clsRawDataExportOptions.eExportRawDataFileFormatConstants.CSVFile, "DeconTools CSV File");
            cboExportRawDataFileFormat.SelectedIndex = (int)clsRawDataExportOptions.eExportRawDataFileFormatConstants.CSVFile;

            cboSICNoiseThresholdMode.Items.Clear();
            cboSICNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold, "Absolute Threshold");
            cboSICNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByAbundance, "Trimmed Mean By Abundance");
            cboSICNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByCount, "Trimmed Mean By Data Count");
            cboSICNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance, "Trimmed Median By Abundance");
            cboSICNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.DualTrimmedMeanByAbundance, "Dual Trimmed Mean By Abundance");
            cboSICNoiseThresholdMode.SelectedIndex = (int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.DualTrimmedMeanByAbundance;

            cboMassSpectraNoiseThresholdMode.Items.Clear();
            cboMassSpectraNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold, "Absolute Threshold");
            cboMassSpectraNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByAbundance, "Trimmed Mean By Abundance");
            cboMassSpectraNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByCount, "Trimmed Mean By Data Count");
            cboMassSpectraNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance, "Trimmed Median By Abundance");
            cboMassSpectraNoiseThresholdMode.SelectedIndex = (int)MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance;

            cboReporterIonMassMode.Items.Clear();
            mReporterIonIndexToModeMap.Clear();

            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.CustomOrNone, "None");

            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.Acetylation, "Acetylated K");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.FrackingAmine20160217, "Fracking Amine 20160217: 157.089, 170.097, and 234.059");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.FSFACustomCarbonyl, "FSFACustomCarbonyl");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.FSFACustomCarboxylic, "FSFACustomCarboxylic");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.FSFACustomHydroxyl, "FSFACustomHydroxyl");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.HemeCFragment, "Heme C: 616.18 and 617.19");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.ITraqETDThreeMZ, "iTraq ETD: 101, 102, and 104");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.ITraqFourMZ, "iTraq: 114, 115, 116, and 117");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZHighRes, "iTraq 8 for High Res MS/MS: 113, 114, ... 121");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZLowRes, "iTraq 8 for Low Res MS/MS (Considers 120 m/z for immonium loss from phenylalanine)");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.LycAcetFragment, "Lys Acet: 126.091 and 127.095");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.NativeOGlcNAc, "Native OGlcNAc: 126.055, 138.055, 144.065, 168.066, 186.076, 204.087, and 366.14");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.OGlcNAc, "OGlcNAc: 204.087, 300.13, and 503.21");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.PCGalnaz, "PCGalnaz: 300.13 and 503.21");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.TMTTwoMZ, "TMT 2: 126, 127");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.TMTSixMZ, "TMT 6: 126, 127, 128, 129, 130, 131");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.TMTTenMZ, "TMT 10: 126, 127N, 127C, 128N, 128C, 129N, 129C, 130N, 130C, 131");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.TMTElevenMZ, "TMT 11: 126, 127N, 127C, 128N, 128C, 129N, 129C, 130N, 130C, 131N, 131C");
            AppendReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.TMTSixteenMZ, "TMT 16: 126, 127N, 127C, ... 132N, 132C, 133N, 133C, 134N");

            SelectedReporterIonMode = clsReporterIons.eReporterIonMassModeConstants.CustomOrNone;
        }

        private void ProcessFileUsingMASIC()
        {
            string outputDirectoryPath;
            bool success;

            DateTime startTime;

            if (!mWorking && ConfirmPaths())
            {
                try
                {
                    txtLogMessages.ResetText();

                    // Configure settings
                    success = UpdateMasicSettings(ref mMasic);
                    if (!success)
                        return;

                    // Validate settings
                    success = ValidateSettings(ref mMasic);
                    if (!success)
                        return;

                    mProgressForm = new frmProgress();

                    mProgressForm.InitializeProgressForm("Creating SIC's for the parent ions", 0, 100, false, true);
                    mProgressForm.InitializeSubtask("", 0, 100, false);
                    mProgressForm.ResetKeyPressAbortProcess();
                    mProgressForm.Show();
                    Application.DoEvents();

                    Cursor.Current = Cursors.WaitCursor;
                    mWorking = true;
                    cmdStartProcessing.Enabled = false;
                    Application.DoEvents();

                    startTime = DateTime.UtcNow;

                    outputDirectoryPath = txtOutputDirectoryPath.Text;
                    success = mMasic.ProcessFile(txtInputFilePath.Text, outputDirectoryPath);
                    Cursor.Current = Cursors.Default;
                    if (mMasic.Options.AbortProcessing)
                    {
                        MessageBox.Show("Cancelled processing", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    if (success)
                    {
                        // Grab the status message, but insert a carriage return directly after "in folder:"
                        MessageBox.Show(mMasic.StatusMessage.Replace("in folder:", "in folder:" + Environment.NewLine) + Environment.NewLine + "Elapsed time: " + StringUtilities.DblToString(DateTime.UtcNow.Subtract(startTime).TotalSeconds, 2) + " sec", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Error analyzing input file with MASIC: " + Environment.NewLine +
                                        mMasic.GetErrorMessage() + Environment.NewLine +
                                        mMasic.StatusMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error in frmMain->ProcessFileUsingMASIC: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                finally
                {
                    if (mProgressForm != null)
                    {
                        mProgressForm.HideForm();
                        mProgressForm = null;
                    }

                    mWorking = false;
                    cmdStartProcessing.Enabled = true;
                }
            }
        }

        private void RegisterEvents(clsMASIC oClass)
        {
            oClass.StatusEvent += StatusEventHandler;
            oClass.DebugEvent += DebugEventHandler;
            oClass.ErrorEvent += ErrorEventHandler;
            oClass.WarningEvent += WarningEventHandler;

            oClass.ProgressUpdate += MASIC_ProgressUpdate;
            oClass.ProgressResetKeypressAbort += MASIC_ProgressResetKeypressAbort;
            oClass.ProgressSubtaskChanged += MASIC_ProgressSubtaskChanged;
        }

        private void ResetToDefaults(bool confirmReset, clsMASIC masicReferenceClass = null)
        {
            if (confirmReset)
            {
                var response = MessageBox.Show("Are you sure you want to reset all settings to their default values?", "Reset to Defaults", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                if (response != DialogResult.Yes)
                    return;
            }

            clsMASIC masicInstance;
            if (masicReferenceClass == null)
            {
                masicInstance = new clsMASIC();
            }
            else
            {
                masicInstance = masicReferenceClass;
            }

            Width = 710;
            Height = 560;

            // File Paths and Import Options
            try
            {
                if (txtOutputDirectoryPath.TextLength == 0 || !Directory.Exists(txtOutputDirectoryPath.Text))
                {
                    txtOutputDirectoryPath.Text = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                }
            }
            catch (Exception ex)
            {
                if (confirmReset)
                {
                    MessageBox.Show("Exception occurred while validating txtOutputDirectoryPath.Text: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }

            try
            {
                var masicOptions = masicInstance.Options;

                // Import Options
                txtParentIonDecoyMassDa.Text = masicOptions.ParentIonDecoyMassDa.ToString(CultureInfo.InvariantCulture);

                // Masic Export Options
                chkIncludeHeaders.Checked = masicOptions.IncludeHeadersInExportFile;
                chkIncludeScanTimesInSICStatsFile.Checked = masicOptions.IncludeScanTimesInSICStatsFile;

                chkSkipMSMSProcessing.Checked = masicOptions.SkipMSMSProcessing;
                chkSkipSICAndRawDataProcessing.Checked = masicOptions.SkipSICAndRawDataProcessing;
                chkExportRawDataOnly.Checked = masicOptions.ExportRawDataOnly;

                // Raw data export options
                var exportOptions = masicOptions.RawDataExportOptions;
                chkExportRawSpectraData.Checked = exportOptions.ExportEnabled;                 // Create .PEK file, or similar
                cboExportRawDataFileFormat.SelectedIndex = (int)exportOptions.FileFormat;

                chkExportRawDataIncludeMSMS.Checked = exportOptions.IncludeMSMS;
                chkExportRawDataRenumberScans.Checked = exportOptions.RenumberScans;

                txtExportRawDataSignalToNoiseRatioMinimum.Text = exportOptions.MinimumSignalToNoiseRatio.ToString(CultureInfo.InvariantCulture);
                txtExportRawDataMaxIonCountPerScan.Text = exportOptions.MaxIonCountPerScan.ToString(CultureInfo.InvariantCulture);
                txtExportRawDataIntensityMinimum.Text = exportOptions.IntensityMinimum.ToString(CultureInfo.InvariantCulture);

                // Thermo .raw info file options
                chkSaveMSMethodFile.Checked = masicOptions.WriteMSMethodFile;
                chkSaveMSTuneFile.Checked = masicOptions.WriteMSTuneFile;
                chkWriteDetailedSICDataFile.Checked = masicOptions.WriteDetailedSICDataFile;
                chkSaveExtendedStatsFile.Checked = masicOptions.WriteExtendedStats;
                chkSaveExtendedStatsFileIncludeFilterText.Checked = masicOptions.WriteExtendedStatsIncludeScanFilterText;
                chkSaveExtendedStatsFileIncludeStatusLog.Checked = masicOptions.WriteExtendedStatsStatusLog;
                txtStatusLogKeyNameFilterList.Text = masicOptions.GetStatusLogKeyNameFilterListAsText(false);

                chkConsolidateConstantExtendedHeaderValues.Checked = masicOptions.ConsolidateConstantExtendedHeaderValues;

                // Dataset and Database Options
                txtDatasetID.Text = "0";
                txtDatabaseConnectionString.Text = masicOptions.DatabaseConnectionString;
                txtDatasetInfoQuerySQL.Text = masicOptions.DatasetInfoQuerySql;

                try
                {
                    if (File.Exists(masicOptions.DatasetLookupFilePath))
                    {
                        txtDatasetLookupFilePath.Text = masicOptions.DatasetLookupFilePath;
                    }
                    else
                    {
                        txtDatasetLookupFilePath.Text = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    txtDatasetLookupFilePath.Text = string.Empty;
                }

                // SIC Options
                var sicOptions = masicOptions.SICOptions;
                var peakFinderOptions = masicOptions.SICOptions.SICPeakFinderOptions;

                bool sicToleranceIsPPM;
                double sicTolerance = sicOptions.GetSICTolerance(out sicToleranceIsPPM);

                txtSICTolerance.Text = StringUtilities.DblToString(sicTolerance, 6);
                if (sicToleranceIsPPM)
                {
                    optSICTolerancePPM.Checked = true;
                }
                else
                {
                    optSICToleranceDa.Checked = true;
                }

                txtScanStart.Text = sicOptions.ScanRangeStart.ToString();
                txtScanEnd.Text = sicOptions.ScanRangeEnd.ToString();
                txtTimeStart.Text = sicOptions.RTRangeStart.ToString(CultureInfo.InvariantCulture);
                txtTimeEnd.Text = sicOptions.RTRangeEnd.ToString(CultureInfo.InvariantCulture);

                // Note: the following 5 options are not graphically editable
                mSuppressNoParentIonsError = masicOptions.SuppressNoParentIonsError;

                mCompressMSSpectraData = sicOptions.CompressMSSpectraData;
                mCompressMSMSSpectraData = sicOptions.CompressMSMSSpectraData;
                mCompressToleranceDivisorForDa = sicOptions.CompressToleranceDivisorForDa;
                mCompressToleranceDivisorForPPM = sicOptions.CompressToleranceDivisorForPPM;

                txtMaxPeakWidthMinutesBackward.Text = sicOptions.MaxSICPeakWidthMinutesBackward.ToString(CultureInfo.InvariantCulture);
                txtMaxPeakWidthMinutesForward.Text = sicOptions.MaxSICPeakWidthMinutesForward.ToString(CultureInfo.InvariantCulture);

                txtIntensityThresholdFractionMax.Text = peakFinderOptions.IntensityThresholdFractionMax.ToString(CultureInfo.InvariantCulture);
                txtIntensityThresholdAbsoluteMinimum.Text = peakFinderOptions.IntensityThresholdAbsoluteMinimum.ToString(CultureInfo.InvariantCulture);

                chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Checked = sicOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData;
                chkRefineReportedParentIonMZ.Checked = sicOptions.RefineReportedParentIonMZ;
                //chkUseSICStatsFromLargestPeak.checked = sicOptions.UseSICStatsFromLargestPeak;

                // Peak Finding Options
                cboSICNoiseThresholdMode.SelectedIndex = (int)peakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode;
                txtSICNoiseThresholdIntensity.Text = peakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute.ToString(CultureInfo.InvariantCulture);
                txtSICNoiseFractionLowIntensityDataToAverage.Text = peakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage.ToString(CultureInfo.InvariantCulture);

                txtMaxDistanceScansNoOverlap.Text = peakFinderOptions.MaxDistanceScansNoOverlap.ToString();
                txtMaxAllowedUpwardSpikeFractionMax.Text = peakFinderOptions.MaxAllowedUpwardSpikeFractionMax.ToString(CultureInfo.InvariantCulture);
                txtInitialPeakWidthScansScaler.Text = peakFinderOptions.InitialPeakWidthScansScaler.ToString(CultureInfo.InvariantCulture);
                txtInitialPeakWidthScansMaximum.Text = peakFinderOptions.InitialPeakWidthScansMaximum.ToString();

                if (peakFinderOptions.UseButterworthSmooth)
                {
                    optUseButterworthSmooth.Checked = true;
                    optUseSavitzkyGolaySmooth.Checked = false;
                }
                else
                {
                    optUseButterworthSmooth.Checked = false;
                    optUseSavitzkyGolaySmooth.Checked = true;
                }

                txtButterworthSamplingFrequency.Text = peakFinderOptions.ButterworthSamplingFrequency.ToString(CultureInfo.InvariantCulture);
                txtSavitzkyGolayFilterOrder.Text = peakFinderOptions.SavitzkyGolayFilterOrder.ToString();

                chkFindPeaksOnSmoothedData.Checked = peakFinderOptions.FindPeaksOnSmoothedData;
                chkSmoothDataRegardlessOfMinimumPeakWidth.Checked = peakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth;

                // Mass Spectra Noise Threshold Options
                cboMassSpectraNoiseThresholdMode.SelectedIndex = (int)peakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode;
                txtMassSpectraNoiseThresholdIntensity.Text = peakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute.ToString(CultureInfo.InvariantCulture);
                txtMassSpectraNoiseFractionLowIntensityDataToAverage.Text = peakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage.ToString(CultureInfo.InvariantCulture);
                txtMassSpectraNoiseMinimumSignalToNoiseRatio.Text = peakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio.ToString(CultureInfo.InvariantCulture);

                // Similarity Options
                txtSimilarIonMZToleranceHalfWidth.Text = sicOptions.SimilarIonMZToleranceHalfWidth.ToString(CultureInfo.InvariantCulture);
                txtSimilarIonToleranceHalfWidthMinutes.Text = sicOptions.SimilarIonToleranceHalfWidthMinutes.ToString(CultureInfo.InvariantCulture);
                txtSpectrumSimilarityMinimum.Text = sicOptions.SpectrumSimilarityMinimum.ToString(CultureInfo.InvariantCulture);

                var binningOptions = masicOptions.BinningOptions;

                // Binning Options
                txtBinStartX.Text = binningOptions.StartX.ToString(CultureInfo.InvariantCulture);
                txtBinEndX.Text = binningOptions.EndX.ToString(CultureInfo.InvariantCulture);
                txtBinSize.Text = binningOptions.BinSize.ToString(CultureInfo.InvariantCulture);
                txtMaximumBinCount.Text = binningOptions.MaximumBinCount.ToString();

                txtBinnedDataIntensityPrecisionPct.Text = binningOptions.IntensityPrecisionPercent.ToString(CultureInfo.InvariantCulture);

                chkBinnedDataNormalize.Checked = binningOptions.Normalize;
                chkBinnedDataSumAllIntensitiesForBin.Checked = binningOptions.SumAllIntensitiesForBin;

                // Spectrum caching options (not graphically editable)
                mCacheOptions.DiskCachingAlwaysDisabled = masicOptions.CacheOptions.DiskCachingAlwaysDisabled;
                mCacheOptions.DirectoryPath = masicOptions.CacheOptions.DirectoryPath;
                mCacheOptions.SpectraToRetainInMemory = masicOptions.CacheOptions.SpectraToRetainInMemory;

                var reporterIonOptions = masicOptions.ReporterIons;

                // Reporter ion options
                txtReporterIonMZToleranceDa.Text = StringUtilities.DblToString(reporterIonOptions.ReporterIonToleranceDaDefault, 6);

                SelectedReporterIonMode = reporterIonOptions.ReporterIonMassMode;

                chkReporterIonStatsEnabled.Checked = reporterIonOptions.ReporterIonStatsEnabled;
                chkReporterIonApplyAbundanceCorrection.Checked = reporterIonOptions.ReporterIonApplyAbundanceCorrection;

                chkReporterIonSaveObservedMasses.Checked = reporterIonOptions.ReporterIonSaveObservedMasses;
                chkReporterIonSaveUncorrectedIntensities.Checked = reporterIonOptions.ReporterIonSaveUncorrectedIntensities;

                // MRM Options
                chkMRMWriteDataList.Checked = masicOptions.WriteMRMDataList;
                chkMRMWriteIntensityCrosstab.Checked = masicOptions.WriteMRMIntensityCrosstab;

                var customSICOptions = masicOptions.CustomSICList;

                // Custom SIC Options
                txtCustomSICFileName.Text = customSICOptions.CustomSICListFileName;

                chkLimitSearchToCustomMZs.Checked = customSICOptions.LimitSearchToCustomMZList;
                SetCustomSICToleranceType(customSICOptions.ScanToleranceType);

                txtCustomSICScanOrAcqTimeTolerance.Text = customSICOptions.ScanOrAcqTimeTolerance.ToString(CultureInfo.InvariantCulture);

                // Load the Custom m/z values from mCustomSICList
                var customMzList = masicOptions.CustomSICList.CustomMZSearchValues;

                ClearCustomSICList(false);
                foreach (var customMzSpec in customMzList)
                {
                    AddCustomSICRow(customMzSpec.MZ, customMzSpec.MZToleranceDa, customMzSpec.ScanOrAcqTimeCenter, customMzSpec.ScanOrAcqTimeTolerance, customMzSpec.Comment, out _);
                }
            }
            catch (Exception ex)
            {
                if (confirmReset)
                {
                    MessageBox.Show("Error resetting values to defaults: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void SelectDatasetLookupFile()
        {
            using (var objOpenFile = new OpenFileDialog()
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".txt",
                DereferenceLinks = true,
                Multiselect = false,
                ValidateNames = true,
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 1
            })
            {
                if (txtDatasetLookupFilePath.TextLength > 0)
                {
                    try
                    {
                        objOpenFile.InitialDirectory = Directory.GetParent(txtDatasetLookupFilePath.Text).ToString();
                    }
                    catch
                    {
                        objOpenFile.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                    }
                }
                else
                {
                    objOpenFile.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                }

                objOpenFile.Title = "Select dataset lookup file";

                var result = objOpenFile.ShowDialog();
                if (result == DialogResult.Cancel)
                    return;

                if (objOpenFile.FileName.Length > 0)
                {
                    txtDatasetLookupFilePath.Text = objOpenFile.FileName;
                }
            }
        }

        private void SelectCustomSICFile()
        {
            using (var objOpenFile = new OpenFileDialog()
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".txt",
                DereferenceLinks = true,
                Multiselect = false,
                ValidateNames = true,
                Filter = "Text files (*.txt)|*.txt|" +
                         "CSV files (*.csv)|*.csv|" +
                         "All files (*.*)|*.*"
            })
            {
                string fileExtension = ".txt";

                if (txtCustomSICFileName.TextLength > 0)
                {
                    fileExtension = Path.GetExtension(txtCustomSICFileName.Text);
                }

                switch (fileExtension.ToLower())
                {
                    case ".txt":
                        objOpenFile.FilterIndex = 1;
                        break;
                    case "csv":
                        objOpenFile.FilterIndex = 2;
                        break;
                    default:
                        objOpenFile.FilterIndex = 1;
                        break;
                }

                if (txtCustomSICFileName.TextLength > 0)
                {
                    try
                    {
                        objOpenFile.InitialDirectory = Directory.GetParent(txtCustomSICFileName.Text).ToString();
                    }
                    catch
                    {
                        objOpenFile.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                    }
                }
                else
                {
                    objOpenFile.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                }

                objOpenFile.Title = "Select custom SIC values file";

                var result = objOpenFile.ShowDialog();
                if (result == DialogResult.Cancel)
                    return;

                if (objOpenFile.FileName.Length > 0)
                {
                    txtCustomSICFileName.Text = objOpenFile.FileName;
                }
            }
        }

        private void SelectInputFile()
        {
            using (var objOpenFile = new OpenFileDialog()
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".txt",
                DereferenceLinks = true,
                Multiselect = false,
                ValidateNames = true,
                Filter = "Xcalibur Raw files (*.raw)|*.raw|" +
                         "mzXML files (*.mzXML)|*.mzXML|" +
                         "mzML files (*.mzML)|*.mzML|" +
                         "mzData files (*.mzData)|*.mzData|" +
                         "Mascot Generic Format files (*.mgf)|*.mgf|" +
                         "CDF files (*.cdf)|*.cdf|" +
                         "All files (*.*)|*.*"
            })
            {
                string fileExtension = string.Copy(mPreferredInputFileExtension);

                if (txtInputFilePath.TextLength > 0)
                {
                    fileExtension = Path.GetExtension(txtInputFilePath.Text);
                }

                int filterIndex;
                switch (fileExtension.ToLower())
                {
                    case ".mzxml":
                        filterIndex = 2;
                        break;
                    case "mzml":
                        filterIndex = 3;
                        break;
                    case ".mzdata":
                        filterIndex = 4;
                        break;
                    case ".mgf":
                        filterIndex = 5;
                        break;
                    case ".cdf":
                        filterIndex = 6;
                        break;
                    default:
                        filterIndex = 1;
                        break;
                }

                objOpenFile.FilterIndex = filterIndex;

                if (txtInputFilePath.TextLength > 0)
                {
                    try
                    {
                        objOpenFile.InitialDirectory = Directory.GetParent(txtInputFilePath.Text).ToString();
                    }
                    catch
                    {
                        objOpenFile.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                    }
                }
                else
                {
                    objOpenFile.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                }

                objOpenFile.Title = "Select input file";

                var result = objOpenFile.ShowDialog();
                if (result == DialogResult.Cancel)
                    return;

                if (objOpenFile.FileName.Length > 0)
                {
                    txtInputFilePath.Text = objOpenFile.FileName;
                    mPreferredInputFileExtension = Path.GetExtension(objOpenFile.FileName);
                }
            }
        }

        private void SelectOutputDirectory()
        {
            var folderBrowserDialog = new FolderBrowser();

            // No need to set the Browse Flags; default values are already set

            if (txtOutputDirectoryPath.TextLength > 0)
            {
                folderBrowserDialog.FolderPath = txtOutputDirectoryPath.Text;
            }

            if (folderBrowserDialog.BrowseForFolder())
            {
                txtOutputDirectoryPath.Text = folderBrowserDialog.FolderPath;
            }
        }

        private void SetConnectionStringToPNNLServer()
        {
            txtDatabaseConnectionString.Text = clsDatabaseAccess.DATABASE_CONNECTION_STRING_DEFAULT;
            txtDatasetInfoQuerySQL.Text = clsDatabaseAccess.DATABASE_DATASET_INFO_QUERY_DEFAULT;
        }

        private void SetCustomSICToleranceType(clsCustomSICList.eCustomSICScanTypeConstants eCustomSICScanToleranceType)
        {
            switch (eCustomSICScanToleranceType)
            {
                case clsCustomSICList.eCustomSICScanTypeConstants.Absolute:
                    optCustomSICScanToleranceAbsolute.Checked = true;
                    break;
                case clsCustomSICList.eCustomSICScanTypeConstants.Relative:
                    optCustomSICScanToleranceRelative.Checked = true;
                    break;
                case clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime:
                    optCustomSICScanToleranceAcqTime.Checked = true;
                    break;
                default:
                    optCustomSICScanToleranceAbsolute.Checked = true;
                    break;
            }
        }

        public void SetHeightAdjustForce(int heightToForce)
        {
            // This function can be used to prevent the form from resizing itself if the MyBase.Resize event
            // fires within 2 seconds of the current time
            // See CatchUnrequestedHeightChange for more info
            mHeightAdjustForce = heightToForce;
            mHeightAdjustTime = DateTime.UtcNow;
        }

        private void SetToolTips()
        {
            var objToolTipControl = new ToolTip();

            objToolTipControl.SetToolTip(txtDatasetID, "The dataset ID is included as the first column in the output file.");

            objToolTipControl.SetToolTip(txtIntensityThresholdAbsoluteMinimum, "Threshold for extending SIC");
            objToolTipControl.SetToolTip(txtMaxDistanceScansNoOverlap, "Maximum distance that the edge of an identified peak can be away from the scan number that the parent ion was observed in if the identified peak does not contain the parent ion.");
            objToolTipControl.SetToolTip(txtMaxAllowedUpwardSpikeFractionMax, "Maximum fraction of the peak maximum that an upward spike can be to be included in the peak");
            objToolTipControl.SetToolTip(txtInitialPeakWidthScansScaler, "Multiplied by the S/N for the given spectrum to determine the initial minimum peak width (in scans) to try");
            objToolTipControl.SetToolTip(txtInitialPeakWidthScansMaximum, "Maximum initial peak width to allow");

            objToolTipControl.SetToolTip(txtSICTolerance, "Search tolerance for creating SIC; suggest 0.6 Da for ion traps and 20 ppm for TOF, FT or Orbitrap instruments");
            objToolTipControl.SetToolTip(txtButterworthSamplingFrequency, "Value between 0.01 and 0.99; suggested value is 0.25");
            objToolTipControl.SetToolTip(txtSavitzkyGolayFilterOrder, "Even number, 0 or greater; 0 means a moving average filter, 2 means a 2nd order Savitzky Golay filter");

            objToolTipControl.SetToolTip(chkRefineReportedParentIonMZ, "If enabled, then will look through the m/z values in the parent ion spectrum data to find the closest match (within SICTolerance / " + clsSICOptions.DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_DA.ToString() + "); will update the reported m/z value to the one found");

            //objToolTipControl.SetToolTip(chkUseSICStatsFromLargestPeak, "If enabled, SIC stats for similar parent ions will all be based on the largest peak in the selected ion chromatogram");

            objToolTipControl.SetToolTip(txtStatusLogKeyNameFilterList, "Enter a comma and/or NewLine separated list of Status Log Key names to match (will match any part of the key name to the text you enter).  Leave blank to include all Status Log entries.");
        }

        private void ShowAboutBox()
        {
            string message;

            message = string.Empty;

            message += "Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2003" + Environment.NewLine;
            message += "Copyright 2005, Battelle Memorial Institute.  All Rights Reserved." + Environment.NewLine + Environment.NewLine;

            message += "This is version " + Application.ProductVersion + " (" + Program.PROGRAM_DATE + "). ";
            message += "Using MASIC PeakFinder DLL version " + mMasic.MASICPeakFinderDllVersion + Environment.NewLine + Environment.NewLine;

            message += "E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov" + Environment.NewLine;
            message += "Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/" + Environment.NewLine + Environment.NewLine;

            message += "Licensed under the 2-Clause BSD License; you may not use this file except in compliance with the License.  ";
            message += "You may obtain a copy of the License at https://opensource.org/licenses/BSD-2-Clause" + Environment.NewLine + Environment.NewLine;

            MessageBox.Show(message, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateCustomSICDataGridTableStyle()
        {
            DataGridTableStyle tsCustomSICValues;
            bool timeTolerance;

            // Define the PM Thresholds table style
            // Setting the MappingName of the table style to CUSTOM_SIC_VALUES_DATA_TABLE will cause this style to be used with that table
            tsCustomSICValues = new DataGridTableStyle()
            {
                MappingName = CUSTOM_SIC_VALUES_DATA_TABLE,
                AllowSorting = true,
                ColumnHeadersVisible = true,
                RowHeadersVisible = true,
                ReadOnly = false
            };

            DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_MZ, "Custom m/z", 90);
            DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_MZ_TOLERANCE, "m/z tolerance (Da)", 110);

            timeTolerance = false;
            switch (GetCustomSICScanToleranceType())
            {
                case clsCustomSICList.eCustomSICScanTypeConstants.Relative:
                    DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_CENTER, "Relative Scan Number (0 to 1)", 170);
                    DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_TOLERANCE, "Scan Tolerance", 90);
                    break;

                case clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime:
                    DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_CENTER, "Acq time (minutes)", 110);
                    DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_TOLERANCE, "Time Tolerance", 90);
                    timeTolerance = true;
                    break;

                default:
                    // Includes eCustomSICScanTypeConstants.Absolute
                    DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_CENTER, "Scan Number", 90);
                    DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_TOLERANCE, "Scan Tolerance", 90);
                    break;
            }

            DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_COMMENT, "Comment", 90);

            fraCustomSICControls.Left = dgCustomSICValues.Left + dgCustomSICValues.Width + 15;

            dgCustomSICValues.TableStyles.Clear();

            if (!dgCustomSICValues.TableStyles.Contains(tsCustomSICValues))
            {
                dgCustomSICValues.TableStyles.Add(tsCustomSICValues);
            }

            dgCustomSICValues.Refresh();

            if (timeTolerance)
            {
                lblCustomSICScanTolerance.Text = "Time Tolerance";
            }
            else
            {
                lblCustomSICScanTolerance.Text = "Scan Tolerance";
            }
        }

        private bool UpdateMasicSettings(ref clsMASIC objMasic)
        {
            var parseError = default(bool);

            try
            {
                var masicOptions = objMasic.Options;

                // Import options

                masicOptions.ParentIonDecoyMassDa = TextBoxUtils.ParseTextBoxValueDbl(txtParentIonDecoyMassDa, lblParentIonDecoyMassDa.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                // Masic Export Options
                masicOptions.IncludeHeadersInExportFile = chkIncludeHeaders.Checked;
                masicOptions.IncludeScanTimesInSICStatsFile = chkIncludeScanTimesInSICStatsFile.Checked;

                masicOptions.SkipMSMSProcessing = chkSkipMSMSProcessing.Checked;
                masicOptions.SkipSICAndRawDataProcessing = chkSkipSICAndRawDataProcessing.Checked;
                masicOptions.ExportRawDataOnly = chkExportRawDataOnly.Checked;

                // Raw data export options
                var exportOptions = masicOptions.RawDataExportOptions;

                exportOptions.ExportEnabled = chkExportRawSpectraData.Checked;
                exportOptions.FileFormat = (clsRawDataExportOptions.eExportRawDataFileFormatConstants)Convert.ToInt32(cboExportRawDataFileFormat.SelectedIndex);

                exportOptions.IncludeMSMS = chkExportRawDataIncludeMSMS.Checked;
                exportOptions.RenumberScans = chkExportRawDataRenumberScans.Checked;

                exportOptions.MinimumSignalToNoiseRatio = TextBoxUtils.ParseTextBoxValueFloat(txtExportRawDataSignalToNoiseRatioMinimum,
                                                                                              lblExportRawDataSignalToNoiseRatioMinimum.Text + " must be a value", out parseError);
                if (parseError)
                    return false;
                exportOptions.MaxIonCountPerScan = TextBoxUtils.ParseTextBoxValueInt(txtExportRawDataMaxIonCountPerScan,
                                                                                     lblExportRawDataMaxIonCountPerScan.Text + " must be an integer value", out parseError);
                if (parseError)
                    return false;
                exportOptions.IntensityMinimum = TextBoxUtils.ParseTextBoxValueFloat(txtExportRawDataIntensityMinimum,
                                                                                     lblExportRawDataIntensityMinimum.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                // Thermo .raw info file options
                masicOptions.WriteMSMethodFile = chkSaveMSMethodFile.Checked;
                masicOptions.WriteMSTuneFile = chkSaveMSTuneFile.Checked;
                masicOptions.WriteDetailedSICDataFile = chkWriteDetailedSICDataFile.Checked;
                masicOptions.WriteExtendedStats = chkSaveExtendedStatsFile.Checked;
                masicOptions.WriteExtendedStatsIncludeScanFilterText = chkSaveExtendedStatsFileIncludeFilterText.Checked;
                masicOptions.WriteExtendedStatsStatusLog = chkSaveExtendedStatsFileIncludeStatusLog.Checked;
                masicOptions.SetStatusLogKeyNameFilterList(txtStatusLogKeyNameFilterList.Text, ',');

                masicOptions.ConsolidateConstantExtendedHeaderValues = chkConsolidateConstantExtendedHeaderValues.Checked;

                var sicOptions = masicOptions.SICOptions;
                var peakFinderOptions = masicOptions.SICOptions.SICPeakFinderOptions;

                // Dataset and Database options
                sicOptions.DatasetID = TextBoxUtils.ParseTextBoxValueInt(txtDatasetID, lblDatasetID.Text + " must be an integer value", out parseError);
                if (parseError)
                    return false;

                if (txtDatabaseConnectionString.TextLength > 0 & txtDatasetInfoQuerySQL.TextLength > 0)
                {
                    masicOptions.DatabaseConnectionString = txtDatabaseConnectionString.Text;
                    masicOptions.DatasetInfoQuerySql = txtDatasetInfoQuerySQL.Text;
                }
                else
                {
                    masicOptions.DatabaseConnectionString = string.Empty;
                    masicOptions.DatasetInfoQuerySql = string.Empty;
                }

                try
                {
                    if (File.Exists(txtDatasetLookupFilePath.Text))
                    {
                        masicOptions.DatasetLookupFilePath = txtDatasetLookupFilePath.Text;
                    }
                    else
                    {
                        masicOptions.DatasetLookupFilePath = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    masicOptions.DatasetLookupFilePath = string.Empty;
                }

                // SIC Options
                var sicTolerance = TextBoxUtils.ParseTextBoxValueDbl(txtSICTolerance, lblSICToleranceDa.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                sicOptions.SetSICTolerance(sicTolerance, optSICTolerancePPM.Checked);

                sicOptions.ScanRangeStart = TextBoxUtils.ParseTextBoxValueInt(txtScanStart, lblScanStart.Text + " must be a value", out parseError);
                if (parseError)
                    return false;
                sicOptions.ScanRangeEnd = TextBoxUtils.ParseTextBoxValueInt(txtScanEnd, lblScanEnd.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                sicOptions.RTRangeStart = TextBoxUtils.ParseTextBoxValueFloat(txtTimeStart, lblTimeStart.Text + " must be a value", out parseError);
                if (parseError)
                    return false;
                sicOptions.RTRangeEnd = TextBoxUtils.ParseTextBoxValueFloat(txtTimeEnd, lblTimeEnd.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                // Note: the following 5 options are not graphically editable
                masicOptions.SuppressNoParentIonsError = mSuppressNoParentIonsError;

                sicOptions.CompressMSSpectraData = mCompressMSSpectraData;
                sicOptions.CompressMSMSSpectraData = mCompressMSMSSpectraData;
                sicOptions.CompressToleranceDivisorForDa = mCompressToleranceDivisorForDa;
                sicOptions.CompressToleranceDivisorForPPM = mCompressToleranceDivisorForPPM;

                sicOptions.MaxSICPeakWidthMinutesBackward = TextBoxUtils.ParseTextBoxValueFloat(txtMaxPeakWidthMinutesBackward, lblMaxPeakWidthMinutes.Text + " " + lblMaxPeakWidthMinutesBackward.Text + " must be a value", out parseError);
                if (parseError)
                    return false;
                sicOptions.MaxSICPeakWidthMinutesForward = TextBoxUtils.ParseTextBoxValueFloat(txtMaxPeakWidthMinutesForward, lblMaxPeakWidthMinutes.Text + " " + lblMaxPeakWidthMinutesForward.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                peakFinderOptions.IntensityThresholdFractionMax = TextBoxUtils.ParseTextBoxValueFloat(txtIntensityThresholdFractionMax, lblIntensityThresholdFractionMax.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                peakFinderOptions.IntensityThresholdAbsoluteMinimum = TextBoxUtils.ParseTextBoxValueFloat(txtIntensityThresholdAbsoluteMinimum, lblIntensityThresholdAbsoluteMinimum.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                sicOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData = chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Checked;
                sicOptions.RefineReportedParentIonMZ = chkRefineReportedParentIonMZ.Checked;

                // ' sicOptions.UseSICStatsFromLargestPeak = chkUseSICStatsFromLargestPeak.Checked

                // Peak Finding Options
                peakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = (MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)Convert.ToInt32(cboSICNoiseThresholdMode.SelectedIndex);
                peakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute = TextBoxUtils.ParseTextBoxValueFloat(txtSICNoiseThresholdIntensity, lblSICNoiseThresholdIntensity.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                peakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage = TextBoxUtils.ParseTextBoxValueFloat(txtSICNoiseFractionLowIntensityDataToAverage, lblSICNoiseFractionLowIntensityDataToAverage.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                // This value isn't utilized by MASIC for SICs so we'll force it to always be zero
                peakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = 0;

                peakFinderOptions.MaxDistanceScansNoOverlap = TextBoxUtils.ParseTextBoxValueInt(txtMaxDistanceScansNoOverlap, lblMaxDistanceScansNoOverlap.Text + " must be an integer value", out parseError);
                if (parseError)
                    return false;
                peakFinderOptions.MaxAllowedUpwardSpikeFractionMax = TextBoxUtils.ParseTextBoxValueFloat(txtMaxAllowedUpwardSpikeFractionMax, lblMaxAllowedUpwardSpikeFractionMax.Text + " must be a value", out parseError);
                if (parseError)
                    return false;
                peakFinderOptions.InitialPeakWidthScansScaler = TextBoxUtils.ParseTextBoxValueFloat(txtInitialPeakWidthScansScaler, lblInitialPeakWidthScansScaler.Text + " must be a value", out parseError);
                if (parseError)
                    return false;
                peakFinderOptions.InitialPeakWidthScansMaximum = TextBoxUtils.ParseTextBoxValueInt(txtInitialPeakWidthScansMaximum, lblInitialPeakWidthScansMaximum.Text + " must be an integer value", out parseError);
                if (parseError)
                    return false;

                peakFinderOptions.UseButterworthSmooth = optUseButterworthSmooth.Checked;
                peakFinderOptions.ButterworthSamplingFrequency = TextBoxUtils.ParseTextBoxValueFloat(txtButterworthSamplingFrequency, lblButterworthSamplingFrequency.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                peakFinderOptions.UseSavitzkyGolaySmooth = optUseSavitzkyGolaySmooth.Checked;
                peakFinderOptions.SavitzkyGolayFilterOrder = Convert.ToInt16(TextBoxUtils.ParseTextBoxValueInt(txtSavitzkyGolayFilterOrder, lblSavitzkyGolayFilterOrder.Text + " must be an integer value", out parseError));
                if (parseError)
                    return false;

                peakFinderOptions.FindPeaksOnSmoothedData = chkFindPeaksOnSmoothedData.Checked;
                peakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth = chkSmoothDataRegardlessOfMinimumPeakWidth.Checked;

                // Mass Spectra Noise Threshold Options
                peakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = (MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)Convert.ToInt32(cboMassSpectraNoiseThresholdMode.SelectedIndex);
                peakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute = TextBoxUtils.ParseTextBoxValueFloat(txtMassSpectraNoiseThresholdIntensity, lblMassSpectraNoiseThresholdIntensity.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                peakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = TextBoxUtils.ParseTextBoxValueFloat(txtMassSpectraNoiseFractionLowIntensityDataToAverage, lblMassSpectraNoiseFractionLowIntensityDataToAverage.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                peakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = TextBoxUtils.ParseTextBoxValueFloat(txtMassSpectraNoiseMinimumSignalToNoiseRatio, lblMassSpectraNoiseMinimumSignalToNoiseRatio.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                // Similarity Options
                sicOptions.SimilarIonMZToleranceHalfWidth = TextBoxUtils.ParseTextBoxValueFloat(txtSimilarIonMZToleranceHalfWidth, lblSimilarIonMZToleranceHalfWidth.Text + " must be a value", out parseError);
                if (parseError)
                    return false;
                sicOptions.SimilarIonToleranceHalfWidthMinutes = TextBoxUtils.ParseTextBoxValueInt(txtSimilarIonToleranceHalfWidthMinutes, lblSimilarIonTimeToleranceHalfWidth.Text + " must be a value", out parseError);
                if (parseError)
                    return false;
                sicOptions.SpectrumSimilarityMinimum = TextBoxUtils.ParseTextBoxValueFloat(txtSpectrumSimilarityMinimum, lblSpectrumSimilarityMinimum.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                var binningOptions = masicOptions.BinningOptions;

                // Binning Options
                binningOptions.StartX = TextBoxUtils.ParseTextBoxValueFloat(txtBinStartX, lblBinStartX.Text + " must be a value", out parseError);
                if (parseError)
                    return false;
                binningOptions.EndX = TextBoxUtils.ParseTextBoxValueFloat(txtBinEndX, lblBinEndX.Text + " must be a value", out parseError);
                if (parseError)
                    return false;
                binningOptions.BinSize = TextBoxUtils.ParseTextBoxValueFloat(txtBinSize, lblBinSize.Text + " must be a value", out parseError);
                if (parseError)
                    return false;
                binningOptions.MaximumBinCount = TextBoxUtils.ParseTextBoxValueInt(txtMaximumBinCount, lblMaximumBinCount.Text + " must be an integer value", out parseError);
                if (parseError)
                    return false;

                binningOptions.IntensityPrecisionPercent = TextBoxUtils.ParseTextBoxValueFloat(txtBinnedDataIntensityPrecisionPct, lblBinnedDataIntensityPrecisionPct.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                binningOptions.Normalize = chkBinnedDataNormalize.Checked;
                binningOptions.SumAllIntensitiesForBin = chkBinnedDataSumAllIntensitiesForBin.Checked;

                // Spectrum caching options
                masicOptions.CacheOptions.DiskCachingAlwaysDisabled = mCacheOptions.DiskCachingAlwaysDisabled;
                masicOptions.CacheOptions.DirectoryPath = mCacheOptions.DirectoryPath;
                masicOptions.CacheOptions.SpectraToRetainInMemory = mCacheOptions.SpectraToRetainInMemory;

                var reporterIonOptions = masicOptions.ReporterIons;

                // Reporter ion options
                reporterIonOptions.ReporterIonStatsEnabled = chkReporterIonStatsEnabled.Checked;

                // Note that this will set .ReporterIonToleranceDa to 0.5
                reporterIonOptions.ReporterIonMassMode = SelectedReporterIonMode;

                // Update .ReporterIonToleranceDa based on txtReporterIonMZToleranceDa
                reporterIonOptions.ReporterIonToleranceDaDefault = TextBoxUtils.ParseTextBoxValueDbl(txtReporterIonMZToleranceDa, "", out parseError,
                                                                                                     clsReporterIons.REPORTER_ION_TOLERANCE_DA_DEFAULT, false);
                reporterIonOptions.SetReporterIonMassMode(reporterIonOptions.ReporterIonMassMode, reporterIonOptions.ReporterIonToleranceDaDefault);

                reporterIonOptions.ReporterIonApplyAbundanceCorrection = chkReporterIonApplyAbundanceCorrection.Checked;

                reporterIonOptions.ReporterIonSaveObservedMasses = chkReporterIonSaveObservedMasses.Checked;
                reporterIonOptions.ReporterIonSaveUncorrectedIntensities = chkReporterIonSaveUncorrectedIntensities.Checked;

                // MRM Options
                masicOptions.WriteMRMDataList = chkMRMWriteDataList.Checked;
                masicOptions.WriteMRMIntensityCrosstab = chkMRMWriteIntensityCrosstab.Checked;

                // Custom m/z options
                masicOptions.CustomSICList.LimitSearchToCustomMZList = chkLimitSearchToCustomMZs.Checked;

                // Store the custom M/Z values in mCustomSICList

                string customSICFileName = txtCustomSICFileName.Text.Trim();
                masicOptions.CustomSICList.CustomSICListFileName = customSICFileName;

                var mzSearchSpecs = new List<clsCustomMZSearchSpec>();

                // Only use the data in table CUSTOM_SIC_VALUES_DATA_TABLE if the CustomSicFileName is empty
                if (string.IsNullOrWhiteSpace(customSICFileName))
                {
                    foreach (DataRow currentRow in mCustomSICValuesDataset.Tables[CUSTOM_SIC_VALUES_DATA_TABLE].Rows)
                    {
                        if (currentRow[0] != null && double.TryParse(currentRow[0].ToString(), out var col0) &&
                            currentRow[1] != null && double.TryParse(currentRow[1].ToString(), out var col1))
                        {
                            double targetMz = Conversions.ToDouble(currentRow[0]);
                            var mzSearchSpec = new clsCustomMZSearchSpec(targetMz)
                            {
                                MZToleranceDa = col1,
                                ScanOrAcqTimeCenter = Convert.ToSingle(currentRow[2]),
                                ScanOrAcqTimeTolerance = Convert.ToSingle(currentRow[3]),
                                Comment = CStrSafe(currentRow[4])
                            };

                            mzSearchSpecs.Add(mzSearchSpec);
                        }
                    }
                }

                clsCustomSICList.eCustomSICScanTypeConstants scanType;
                if (optCustomSICScanToleranceAbsolute.Checked)
                {
                    scanType = clsCustomSICList.eCustomSICScanTypeConstants.Absolute;
                }
                else if (optCustomSICScanToleranceRelative.Checked)
                {
                    scanType = clsCustomSICList.eCustomSICScanTypeConstants.Relative;
                }
                else if (optCustomSICScanToleranceAcqTime.Checked)
                {
                    scanType = clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime;
                }
                else
                {
                    // Assume absolute
                    scanType = clsCustomSICList.eCustomSICScanTypeConstants.Absolute;
                }

                var scanOrAcqTimeTolerance = TextBoxUtils.ParseTextBoxValueFloat(txtCustomSICScanOrAcqTimeTolerance, lblCustomSICScanTolerance.Text + " must be a value", out parseError);
                if (parseError)
                    return false;

                masicOptions.CustomSICList.SetCustomSICListValues(scanType, scanOrAcqTimeTolerance, mzSearchSpecs);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error applying setting to clsMASIC: " + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            return !parseError;
        }

        private bool ValidateSettings(ref clsMASIC objMasic)
        {
            DialogResult eResponse;

            if (objMasic.Options.ReporterIons.ReporterIonMassMode != clsReporterIons.eReporterIonMassModeConstants.CustomOrNone)
            {
                if (objMasic.Options.ReporterIons.ReporterIonMassMode == clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZHighRes)
                {
                    // Make sure the tolerance is less than 0.03 Da; if not, warn the user
                    if (objMasic.Options.ReporterIons.ReporterIonToleranceDaDefault > 0.03)
                    {
                        eResponse = MessageBox.Show("Warning: the Reporter Ion 'm/z Tolerance Half Width' value should be less than 0.03 m/z when using 'iTraq8 for High Res MS/MS' reporter ions.  It is currently " + objMasic.Options.ReporterIons.ReporterIonToleranceDaDefault.ToString("0.000") + " m/z.  If using a low resolution instrument, you should choose the 'iTraq 8 for Low Res MS/MS' mode.  Continue anyway?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

                        if (eResponse != DialogResult.Yes)
                        {
                            return false;
                        }
                    }
                }
                else if (objMasic.Options.ReporterIons.ReporterIonMassMode == clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZLowRes)
                {
                    // Make sure the tolerance is at least 0.1 Da; if not, warn the user
                    if (objMasic.Options.ReporterIons.ReporterIonToleranceDaDefault < 0.1)
                    {
                        eResponse = MessageBox.Show("Warning: the Reporter Ion 'm/z Tolerance Half Width' value should be at least 0.1 m/z when using 'iTraq8 for Low Res MS/MS' reporter ions.  It is currently " + objMasic.Options.ReporterIons.ReporterIonToleranceDaDefault.ToString("0.000") + " m/z. If using a high resolution instrument, you should choose the 'iTraq 8 for High Res MS/MS' mode.  Continue anyway?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

                        if (eResponse != DialogResult.Yes)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        #endregion

        #region "Combobox Handlers"

        private void cboMassSpectraNoiseThresholdMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        private void cboSICNoiseThresholdMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }
        #endregion

        #region "Button Handlers"
        private void cmdClearAllRangeFilters_Click(object sender, EventArgs e)
        {
            ClearAllRangeFilters();
        }

        private void cmdClearCustomSICList_Click(object sender, EventArgs e)
        {
            ClearCustomSICList(true);
        }

        private void cmdCustomSICValuesPopulate_Click(object sender, EventArgs e)
        {
            AutoPopulateCustomSICValues(true);
        }

        private void cmdPasteCustomSICList_Click(object sender, EventArgs e)
        {
            PasteCustomSICValues(false);
        }

        private void cmdStartProcessing_Click(object sender, EventArgs e)
        {
            ProcessFileUsingMASIC();
        }

        private void cmdSelectDatasetLookupFile_Click(object sender, EventArgs e)
        {
            SelectDatasetLookupFile();
        }

        private void cmdSelectCustomSICFile_Click(object sender, EventArgs e)
        {
            SelectCustomSICFile();
        }

        private void cmdSelectFile_Click(object sender, EventArgs e)
        {
            SelectInputFile();
        }

        private void cmdSelectOutputDirectory_Click(object sender, EventArgs e)
        {
            SelectOutputDirectory();
        }

        private void cmdSetConnectionStringToPNNLServer_Click(object sender, EventArgs e)
        {
            SetConnectionStringToPNNLServer();
        }
        #endregion

        #region "Checkbox Events"
        private void chkExportRawDataOnly_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        private void chkSkipMSMSProcessing_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        private void chkSkipSICAndRawDataProcessing_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        private void chkExportRawSpectraData_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        private void chkExportRawDataIncludeMSMS_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        private void chkSaveExtendedStatsFile_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        private void chkReporterIonStatsEnabled_CheckedChanged(object sender, EventArgs e)
        {
            AutoToggleReporterIonStatsMode();
        }

        private void chkSaveExtendedStatsFileIncludeStatusLog_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        #endregion

        #region "Radio Button Events"
        private void optUseButterworthSmooth_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        private void optUseSavitzkyGolaySmooth_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        private void optCustomSICScanToleranceAbsolute_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCustomSICDataGridTableStyle();
        }

        private void optCustomSICScanToleranceRelative_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCustomSICDataGridTableStyle();
        }

        private void optCustomSICScanToleranceAcqTime_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCustomSICDataGridTableStyle();
        }
        #endregion

        #region "Textbox Events"
        private void txtMassSpectraNoiseThresholdIntensity_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtMassSpectraNoiseThresholdIntensity, e, true, true);
        }

        private void txtMassSpectraNoiseFractionLowIntensityDataToAverage_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtMassSpectraNoiseFractionLowIntensityDataToAverage, e, true, true);
        }

        private void txtBinnedDataIntensityPrecisionPct_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtBinnedDataIntensityPrecisionPct, e, true, true);
        }

        private void txtBinSize_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtBinSize, e, true, true);
        }

        private void txtBinStartX_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtBinStartX, e, true, true);
        }

        private void txtBinEndX_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtBinEndX, e, true, true);
        }

        private void txtButterworthSamplingFrequency_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtButterworthSamplingFrequency, e, true, true);
        }

        private void txtButterworthSamplingFrequency_Validating(object sender, CancelEventArgs e)
        {
            TextBoxUtils.ValidateTextBoxFloat(txtButterworthSamplingFrequency, 0.01f, 0.99f, 0.25f);
        }

        private void txtCustomSICFileDescription_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control == true)
            {
                txtCustomSICFileDescription.SelectAll();
            }
        }

        private void txtCustomSICFileName_TextChanged(object sender, EventArgs e)
        {
            EnableDisableCustomSICValueGrid();
        }

        private void txtCustomSICScanOrAcqTimeTolerance_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtCustomSICScanOrAcqTimeTolerance, e, true, true);
        }

        private void txtDatasetID_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtDatasetID, e, true, false);
        }

        private void txtExportRawDataIntensityMinimum_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtExportRawDataIntensityMinimum, e, true, true);
        }

        private void txtExportRawDataMaxIonCountPerScan_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtExportRawDataMaxIonCountPerScan, e);
        }

        private void txtExportRawDataSignalToNoiseRatioMinimum_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtExportRawDataSignalToNoiseRatioMinimum, e, true, true);
        }

        private void txtInitialPeakWidthScansMaximum_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtInitialPeakWidthScansMaximum, e);
        }

        private void txtInitialPeakWidthScansScaler_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtInitialPeakWidthScansScaler, e);
        }

        private void txtIntensityThresholdAbsoluteMinimum_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtIntensityThresholdAbsoluteMinimum, e, true, true);
        }

        private void txtIntensityThresholdFractionMax_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtIntensityThresholdFractionMax, e, true, true);
        }

        private void txtMaxAllowedUpwardSpikeFractionMax_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtMaxAllowedUpwardSpikeFractionMax, e, true, true);
        }

        private void txtMaxDistanceScansNoOverlap_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtMaxDistanceScansNoOverlap, e);
        }

        private void txtMaximumBinCount_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtMaximumBinCount, e);
        }

        private void txtMaxPeakWidthMinutesBackward_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtMaxPeakWidthMinutesBackward, e, true, true);
        }

        private void txtMaxPeakWidthMinutesForward_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtMaxPeakWidthMinutesForward, e, true, true);
        }

        private void txtSICNoiseThresholdIntensity_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtSICNoiseThresholdIntensity, e, true, true);
        }

        private void txtSICNoiseFractionLowIntensityDataToAverage_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtSICNoiseFractionLowIntensityDataToAverage, e, true, true);
        }

        private void txtSavitzkyGolayFilterOrder_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtSavitzkyGolayFilterOrder, e);
        }

        private void txtSavitzkyGolayFilterOrder_Validating(object sender, CancelEventArgs e)
        {
            TextBoxUtils.ValidateTextBoxInt(txtSavitzkyGolayFilterOrder, 0, 20, 0);
        }

        private void txtSICTolerance_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtSICTolerance, e, true, true);
        }

        private void txtSimilarIonMZToleranceHalfWidth_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtSimilarIonMZToleranceHalfWidth, e, true, true);
        }

        private void txtSimilarIonToleranceHalfWidthMinutes_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtSimilarIonToleranceHalfWidthMinutes, e, true, true);
        }

        private void txtSpectrumSimilarityMinimum_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtSpectrumSimilarityMinimum, e, true, true);
        }

        private void txtScanEnd_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtScanEnd, e, true, false);
        }

        private void txtScanStart_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtScanStart, e, true, false);
        }

        private void txtTimeEnd_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtTimeEnd, e, true, true);
        }

        private void txtTimeStart_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtTimeStart, e, true, true);
        }
        #endregion

        #region "Menu Handlers"
        private void mnuFileSelectInputFile_Click(object sender, EventArgs e)
        {
            SelectInputFile();
        }

        private void mnuFileSelectOutputDirectory_Click(object sender, EventArgs e)
        {
            SelectOutputDirectory();
        }

        private void mnuFileLoadOptions_Click(object sender, EventArgs e)
        {
            IniFileLoadOptions(false);
        }

        private void mnuFileSaveOptions_Click(object sender, EventArgs e)
        {
            IniFileSaveOptions();
        }

        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void mnuEditProcessFile_Click(object sender, EventArgs e)
        {
            ProcessFileUsingMASIC();
        }

        private void mnuEditResetOptions_Click(object sender, EventArgs e)
        {
            ResetToDefaults(true);
        }

        private void mnuEditSaveDefaultOptions_Click(object sender, EventArgs e)
        {
            IniFileSaveDefaultOptions();
        }

        private void mnuHelpAbout_Click(object sender, EventArgs e)
        {
            ShowAboutBox();
        }

        #endregion

        #region "Form and Masic Class Events"
        private void frmMain_Load(object sender, EventArgs e)
        {
            // Note that InitializeControls() is called in Sub New()
        }

        private void frmMain_Closing(object sender, CancelEventArgs e)
        {
            IniFileSaveOptions(GetSettingsFilePath(), true);
        }

        private void MASIC_ProgressUpdate(string taskDescription, float percentComplete)
        {
            if (mProgressForm != null)
            {
                mProgressForm.UpdateCurrentTask(mMasic.ProgressStepDescription);
                mProgressForm.UpdateProgressBar(percentComplete);
                if (mProgressForm.KeyPressAbortProcess)
                {
                    mMasic.AbortProcessingNow();
                }

                Application.DoEvents();
            }
        }

        private void MASIC_ProgressResetKeypressAbort()
        {
            if (mProgressForm != null)
            {
                mProgressForm.ResetKeyPressAbortProcess();
            }
        }

        private void MASIC_ProgressSubtaskChanged()
        {
            if (mProgressForm != null)
            {
                mProgressForm.UpdateCurrentSubTask(mMasic.SubtaskDescription);
                mProgressForm.UpdateSubtaskProgressBar(mMasic.SubtaskProgressPercentComplete);
                if (mProgressForm.KeyPressAbortProcess)
                {
                    mMasic.AbortProcessingNow();
                }

                Application.DoEvents();
            }
        }

        private void StatusEventHandler(string message)
        {
            AppendToLog(EventLogEntryType.Information, message);
            Console.WriteLine(message);
        }

        private void DebugEventHandler(string message)
        {
            ConsoleMsgUtils.ShowDebug(message);
        }

        private void ErrorEventHandler(string message, Exception ex)
        {
            AppendToLog(EventLogEntryType.Error, message);
            ConsoleMsgUtils.ShowError(message, ex);
        }

        private void WarningEventHandler(string message)
        {
            AppendToLog(EventLogEntryType.Warning, message);
            ConsoleMsgUtils.ShowWarning(message);
        }
        #endregion

        private void cboReporterIonMassMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            AutoToggleReporterIonStatsEnabled();
        }
    }
}
