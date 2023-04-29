using System;
using System.Collections.Generic;

// -------------------------------------------------------------------------------
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
// Program started October 11, 2003
// Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
// Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MASIC.Data;
using MASIC.DataInput;
using MASIC.Options;
using PRISM;
using PRISM.FileProcessor;
using PRISMDatabaseUtils;
using PRISMWin;
using ProgressFormNET;
using ShFolderBrowser.FolderBrowser;

namespace MASIC
{
    /// <summary>
    /// Main GUI window
    /// </summary>
    public partial class frmMain : Form
    {
        // ReSharper disable CommentTypo

        // Ignore Spelling: Acet, Acq, Acetylated, amine, Checkbox, Combobox, CrLf, csv, Da, dpi, fracking frmMain
        // Ignore Spelling: Golay, immonium, iTraq, Lys, mzml, Orbitrap Savitzky, Textbox

        // ReSharper restore CommentTypo

        /// <summary>
        /// Constructor
        /// </summary>
        public frmMain()
        {
            // This call is required by the Windows Form Designer.
            InitializeComponent();

            // Add any initialization after the InitializeComponent() call

            mCacheOptions = new SpectrumCacheOptions();
            mDefaultCustomSICList = new List<CustomSICEntryType>();
            mLogMessages = new List<string>();
            mReporterIonIndexToModeMap = new Dictionary<int, ReporterIons.ReporterIonMassModeConstants>();
            InitializeControls();

            mMasic = new clsMASIC();
            RegisterEvents(mMasic);
        }

        private const string XML_SETTINGS_FILE_NAME = "MASICParameters.xml";

        private const string CUSTOM_SIC_VALUES_DATA_TABLE = "PeakMatchingThresholds";

        private const string COL_NAME_MZ = "MZ";
        private const string COL_NAME_MZ_TOLERANCE = "MZToleranceDa";
        private const string COL_NAME_SCAN_CENTER = "Scan_Center";
        private const string COL_NAME_SCAN_TOLERANCE = "Scan_Tolerance";
        private const string COL_NAME_SCAN_COMMENT = "Scan_Comment";
        private const string COL_NAME_CUSTOM_SIC_VALUE_ROW_ID = "UniqueRowID";

        private struct CustomSICEntryType
        {
            public double MZ;
            public float ScanCenter;
            public string Comment;
        }

        private DataSet mCustomSICValuesDataset;

        private readonly List<CustomSICEntryType> mDefaultCustomSICList;
        private bool mWorking;

        private string mXmlSettingsFilePath;
        private string mPreferredInputFileExtension;

        private readonly SpectrumCacheOptions mCacheOptions;

        private bool mSuppressNoParentIonsError;
        private bool mCompressMSSpectraData;
        private bool mCompressMSMSSpectraData;

        private double mCompressToleranceDivisorForDa;
        private double mCompressToleranceDivisorForPPM;

        private int mHeightAdjustForce;
        private DateTime mHeightAdjustTime;

        /// <summary>
        /// Default instance of MASIC
        /// </summary>
        private readonly clsMASIC mMasic;

        private frmProgress mProgressForm;

        /// <summary>
        /// Log messages, including warnings and errors, with the newest message at the top
        /// </summary>
        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<string> mLogMessages;

        private readonly Dictionary<int, ReporterIons.ReporterIonMassModeConstants> mReporterIonIndexToModeMap;

        private ReporterIons.ReporterIonMassModeConstants SelectedReporterIonMode
        {
            get => GetSelectedReporterIonMode();
            set
            {
                try
                {
                    cboReporterIonMassMode.SelectedIndex = GetReporterIonIndexFromMode(value);
                }
                catch (Exception)
                {
                    // Ignore errors here
                }
            }
        }

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
                if (Math.Abs(double.Parse(myDataRow[0].ToString()) - mz) < float.Epsilon &&
                    Math.Abs(float.Parse(myDataRow[1].ToString()) - scanOrAcqTimeCenter) < float.Epsilon)
                {
                    existingRowFound = true;
                    break;
                }
            }

            comment ??= string.Empty;

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
            var customSicEntryItem = new CustomSICEntryType
            {
                MZ = mz,
                ScanCenter = scanCenter,
                Comment = comment
            };

            mDefaultCustomSICList.Add(customSicEntryItem);
        }

        private void AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants reporterIonMassMode, string description)
        {
            cboReporterIonMassMode.Items.Add(description);
            var currentIndex = cboReporterIonMassMode.Items.Count - 1;

            // Add or update the value for key currentIndex
            mReporterIonIndexToModeMap[currentIndex] = reporterIonMassMode;
        }

        private void AppendToLog(EventLogEntryType messageType, string message)
        {
            if (message.StartsWith("ProcessingStats") || message.StartsWith("Parameter file not specified"))
            {
                return;
            }

            string textToAppend;
            var doEvents = false;
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
            GetCurrentCustomSICTolerances(out var defaultMZTolerance, out var defaultScanOrAcqTimeTolerance);

            if (defaultScanOrAcqTimeTolerance > 1)
            {
                defaultScanOrAcqTimeTolerance = 0.6F;
            }

            if (ClearCustomSICList(confirmReplaceExistingResults))
            {
                // The default values use relative times, so make sure that mode is enabled
                SetCustomSICToleranceType(CustomSICList.CustomSICScanTypeConstants.Relative);
                txtCustomSICScanOrAcqTimeTolerance.Text = defaultScanOrAcqTimeTolerance.ToString(CultureInfo.InvariantCulture);

                foreach (var item in mDefaultCustomSICList)
                {
                    AddCustomSICRow(item.MZ, defaultMZTolerance, item.ScanCenter, defaultScanOrAcqTimeTolerance, item.Comment, out _);
                }
            }
        }

        private bool mUpdating;

        private void CatchUnrequestedHeightChange()
        {
            if (mUpdating)
                return;

            if (mHeightAdjustForce == 0 || DateTime.UtcNow.Subtract(mHeightAdjustTime).TotalSeconds > 5.0)
                return;

            try
            {
                mUpdating = true;
                Height = mHeightAdjustForce;
                mHeightAdjustForce = 0;
                mHeightAdjustTime = DateTime.Parse("1900-01-01");
            }
            catch (Exception)
            {
                // Ignore errors here
            }
            finally
            {
                mUpdating = false;
            }
        }

        private void AutoToggleReporterIonStatsEnabled()
        {
            if (SelectedReporterIonMode == ReporterIons.ReporterIonMassModeConstants.CustomOrNone)
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
                if (SelectedReporterIonMode == ReporterIons.ReporterIonMassModeConstants.CustomOrNone)
                {
                    SelectedReporterIonMode = ReporterIons.ReporterIonMassModeConstants.ITraqFourMZ;
                }
            }
            else if (SelectedReporterIonMode != ReporterIons.ReporterIonMassModeConstants.CustomOrNone)
            {
                SelectedReporterIonMode = ReporterIons.ReporterIonMassModeConstants.CustomOrNone;
            }
        }

        private void ClearAllRangeFilters()
        {
            txtScanStart.Text = "0";
            txtScanEnd.Text = "0";
            txtTimeStart.Text = "0";
            txtTimeEnd.Text = "0";
        }

        /// <summary>
        /// Clear the custom SIC list
        /// </summary>
        /// <param name="confirmReplaceExistingResults"></param>
        /// <returns>
        /// True if the CUSTOM_SIC_VALUES_DATA_TABLE is empty or if it was cleared
        /// False if the user is queried about clearing and they do not click Yes
        /// </returns>
        private bool ClearCustomSICList(bool confirmReplaceExistingResults)
        {
            if (mCustomSICValuesDataset.Tables[CUSTOM_SIC_VALUES_DATA_TABLE].Rows.Count == 0)
                return true;

            DialogResult result;

            if (confirmReplaceExistingResults)
            {
                result = MessageBox.Show("Are you sure you want to clear the Custom SIC list?", "Clear Custom SICs", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            }
            else
            {
                result = DialogResult.Yes;
            }

            if (result == DialogResult.Yes)
            {
                mCustomSICValuesDataset.Tables[CUSTOM_SIC_VALUES_DATA_TABLE].Rows.Clear();
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

            if (txtOutputDirectoryPath.TextLength == 0)
            {
                MessageBox.Show("Please define an output directory path", "Missing Value", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtOutputDirectoryPath.Focus();
                return false;
            }

            return true;
        }

        private string CStrSafe(object item)
        {
            try
            {
                if (item == null)
                {
                    return string.Empty;
                }

                if (Convert.IsDBNull(item))
                {
                    return string.Empty;
                }

                return Convert.ToString(item);
            }
            catch (Exception)
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
            msg.Append("If Export MS/MS Spectra is enabled, the fragmentation spectra are included, in addition to the survey scan spectra (MS1 scans). ");
            msg.Append("If MS/MS spectra are not included, one can optionally renumber the survey scan spectra so that they increase in steps of 1, regardless of the number of MS/MS scans between each survey scan. ");
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
            msg.Append("allowed column names are: ").Append(CustomSICListReader.GetCustomMZFileColumnHeaders()).Append(".  ");
            msg.Append("Note: use " +
                CustomSICListReader.CUSTOM_SIC_COLUMN_SCAN_TIME + " and " +
                CustomSICListReader.CUSTOM_SIC_COLUMN_TIME_TOLERANCE + " only when specifying ");

            msg.Append("acquisition time-based values.  When doing so, do not include " +
                CustomSICListReader.CUSTOM_SIC_COLUMN_SCAN_CENTER + " and " +
                CustomSICListReader.CUSTOM_SIC_COLUMN_SCAN_TOLERANCE + ".");

            txtCustomSICFileDescription.Text = msg.ToString();
        }

        private void EnableDisableControls()
        {
            var createSICsAndRawData = !chkSkipSICAndRawDataProcessing.Checked;
            var msmsProcessingEnabled = !chkSkipMSMSProcessing.Checked;
            var exportRawDataOnly = chkExportRawDataOnly.Checked && chkExportRawSpectraData.Checked;

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

            var rawExportEnabled = chkExportRawSpectraData.Checked;

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

            if (cboSICNoiseThresholdMode.SelectedIndex == (int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.AbsoluteThreshold)
            {
                txtSICNoiseThresholdIntensity.Enabled = true;
                txtSICNoiseFractionLowIntensityDataToAverage.Enabled = false;
            }
            else if (cboSICNoiseThresholdMode.SelectedIndex is
                (int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMeanByAbundance or
                (int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMeanByCount or
                (int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMedianByAbundance)
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

            if (cboMassSpectraNoiseThresholdMode.SelectedIndex == (int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.AbsoluteThreshold)
            {
                txtMassSpectraNoiseThresholdIntensity.Enabled = true;
                txtMassSpectraNoiseFractionLowIntensityDataToAverage.Enabled = false;
                txtMassSpectraNoiseMinimumSignalToNoiseRatio.Enabled = false;
            }
            else if (cboMassSpectraNoiseThresholdMode.SelectedIndex is
                (int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMeanByAbundance or
                (int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMeanByCount or
                (int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMedianByAbundance)
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

        private void GetCurrentCustomSICTolerances(out double defaultMZTolerance, out float defaultScanOrAcqTimeTolerance)
        {
            try
            {
                defaultMZTolerance = double.Parse(txtSICTolerance.Text);

                if (optSICTolerancePPM.Checked)
                {
                    defaultMZTolerance = Utilities.PPMToMass(defaultMZTolerance, 1000);
                }
            }
            catch (Exception)
            {
                defaultMZTolerance = 0.6;
            }

            try
            {
                defaultScanOrAcqTimeTolerance = float.Parse(txtCustomSICScanOrAcqTimeTolerance.Text);
            }
            catch (Exception)
            {
                defaultScanOrAcqTimeTolerance = 0;
            }
        }

        private CustomSICList.CustomSICScanTypeConstants GetCustomSICScanToleranceType()
        {
            if (optCustomSICScanToleranceAbsolute.Checked)
            {
                return CustomSICList.CustomSICScanTypeConstants.Absolute;
            }

            if (optCustomSICScanToleranceRelative.Checked)
            {
                return CustomSICList.CustomSICScanTypeConstants.Relative;
            }

            if (optCustomSICScanToleranceAcqTime.Checked)
            {
                return CustomSICList.CustomSICScanTypeConstants.AcquisitionTime;
            }

            // Assume absolute
            return CustomSICList.CustomSICScanTypeConstants.Absolute;
        }

        private int GetReporterIonIndexFromMode(ReporterIons.ReporterIonMassModeConstants reporterIonMassMode)
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

        private ReporterIons.ReporterIonMassModeConstants GetReporterIonModeFromIndex(int comboboxIndex)
        {
            if (mReporterIonIndexToModeMap.TryGetValue(comboboxIndex, out var reporterIonMassMode))
            {
                return reporterIonMassMode;
            }

            throw new Exception("Dictionary mReporterIonIndexToModeMap is missing index " + comboboxIndex);
        }

        private ReporterIons.ReporterIonMassModeConstants GetSelectedReporterIonMode()
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

            using var fileSelector = new OpenFileDialog
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
            };

            var filePath = mXmlSettingsFilePath;

            if (filePath.Length > 0)
            {
                try
                {
                    fileSelector.InitialDirectory = Directory.GetParent(filePath)?.ToString();
                }
                catch
                {
                    fileSelector.InitialDirectory = AppUtils.GetAppDirectoryPath();
                }
            }
            else
            {
                fileSelector.InitialDirectory = AppUtils.GetAppDirectoryPath();
            }

            if (File.Exists(filePath))
            {
                fileSelector.FileName = Path.GetFileName(filePath);
            }

            fileSelector.Title = "Specify file to load options from";

            var result = fileSelector.ShowDialog();

            if (result == DialogResult.Cancel)
                return;

            if (fileSelector.FileName.Length > 0)
            {
                mXmlSettingsFilePath = fileSelector.FileName;

                IniFileLoadOptions(mXmlSettingsFilePath, updateIOPaths);
            }
        }

        private void IniFileLoadOptions(string filePath, bool updateIOPaths)
        {
            // Loads options from the given file

            try
            {
                // Utilize the built-in LoadParameterFileSettings function, then call ResetToDefaults

                var masicInstance = mMasic ?? new clsMASIC();

                var success = masicInstance.LoadParameterFileSettings(filePath);

                if (!success)
                {
                    MessageBox.Show("LoadParameterFileSettings returned false for: " + Path.GetFileName(filePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }

                ResetToDefaults(false);

                // Sleep for 100 msec, just to be safe
                Thread.Sleep(100);

                // Now load some custom options that aren't loaded by clsMASIC
                var xmlFileReader = new XmlSettingsFileAccessor();

                // Pass True to .LoadSettings() to turn off case sensitive matching
                xmlFileReader.LoadSettings(filePath, false);

                try
                {
                    txtDatasetLookupFilePath.Text = xmlFileReader.GetParam(MASICOptions.XML_SECTION_DATABASE_SETTINGS, "DatasetLookupFilePath", txtDatasetLookupFilePath.Text);
                    try
                    {
                        if (!File.Exists(txtDatasetLookupFilePath.Text))
                        {
                            txtDatasetLookupFilePath.Text = string.Empty;
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore any errors here
                    }

                    if (updateIOPaths)
                    {
                        txtInputFilePath.Text = xmlFileReader.GetParam(MASICOptions.XML_SECTION_IMPORT_OPTIONS, "InputFilePath", txtInputFilePath.Text);
                    }

                    Width = xmlFileReader.GetParam(MASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowWidth", Width);
                    Height = xmlFileReader.GetParam(MASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowHeight", Height);

                    // Uncomment to test DPI scaling
                    //var graphics = this.CreateGraphics();
                    //var dpiX = graphics.DpiX;
                    //var dpiY = graphics.DpiY;

                    //var savedWidth = xmlFileReader.GetParam(MASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowWidth", 0);
                    //var savedHeight = xmlFileReader.GetParam(MASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowHeight", 0);
                    //if (savedWidth > 0)
                    //    Width = (int)Math.Floor(savedWidth * 96 / dpiX);

                    //if (savedHeight > 0)
                    //    Height = (int)Math.Floor(savedHeight * 96 / dpiY);

                    if (updateIOPaths)
                    {
                        txtOutputDirectoryPath.Text = xmlFileReader.GetParam(MASICOptions.XML_SECTION_IMPORT_OPTIONS, "LastDirectory", txtOutputDirectoryPath.Text);
                    }

                    if (txtOutputDirectoryPath.TextLength == 0)
                    {
                        txtOutputDirectoryPath.Text = AppUtils.GetAppDirectoryPath();
                    }

                    mPreferredInputFileExtension = xmlFileReader.GetParam(MASICOptions.XML_SECTION_IMPORT_OPTIONS, "PreferredInputFileExtension", mPreferredInputFileExtension);
                }
                catch (Exception)
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
            var response = MessageBox.Show("Save the current options as defaults?", "Save Defaults", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

            if (response == DialogResult.Yes)
            {
                IniFileSaveOptions(GetSettingsFilePath(), false);
            }
        }

        private void IniFileSaveOptions()
        {
            // Prompts the user to select a file to load the options from

            using var fileSelector = new SaveFileDialog
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
            };

            var filePath = mXmlSettingsFilePath;

            if (filePath.Length > 0)
            {
                try
                {
                    fileSelector.InitialDirectory = Directory.GetParent(filePath)?.ToString();
                }
                catch
                {
                    fileSelector.InitialDirectory = AppUtils.GetAppDirectoryPath();
                }
            }
            else
            {
                fileSelector.InitialDirectory = AppUtils.GetAppDirectoryPath();
            }

            if (File.Exists(filePath))
            {
                fileSelector.FileName = Path.GetFileName(filePath);
            }

            fileSelector.Title = "Specify file to save options to";

            var result = fileSelector.ShowDialog();

            if (result == DialogResult.Cancel)
                return;

            if (fileSelector.FileName.Length > 0)
            {
                mXmlSettingsFilePath = fileSelector.FileName;

                IniFileSaveOptions(mXmlSettingsFilePath, false);
            }
        }

        private void IniFileSaveOptions(string filePath, bool saveWindowDimensionsOnly = false)
        {
            try
            {
                if (!saveWindowDimensionsOnly)
                {
                    UpdateMasicSettings(mMasic);

                    mMasic.Options.SaveParameterFileSettings(filePath);

                    // Sleep for 100 msec, just to be safe
                    Thread.Sleep(100);
                }

                // Pass True to .LoadSettings() here so that newly made Xml files will have the correct capitalization
                var xmlFileReader = new XmlSettingsFileAccessor();

                xmlFileReader.LoadSettings(filePath, true);

                try
                {
                    if (!saveWindowDimensionsOnly)
                    {
                        try
                        {
                            if (File.Exists(txtDatasetLookupFilePath.Text))
                            {
                                xmlFileReader.SetParam(MASICOptions.XML_SECTION_DATABASE_SETTINGS, "DatasetLookupFilePath", txtDatasetLookupFilePath.Text);
                            }
                        }
                        catch (Exception)
                        {
                            // Ignore any errors here
                        }

                        xmlFileReader.SetParam(MASICOptions.XML_SECTION_IMPORT_OPTIONS, "InputFilePath", txtInputFilePath.Text);
                    }

                    xmlFileReader.SetParam(MASICOptions.XML_SECTION_IMPORT_OPTIONS, "LastDirectory", txtOutputDirectoryPath.Text);
                    xmlFileReader.SetParam(MASICOptions.XML_SECTION_IMPORT_OPTIONS, "PreferredInputFileExtension", mPreferredInputFileExtension);

                    xmlFileReader.SetParam(MASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowWidth", Width);
                    xmlFileReader.SetParam(MASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowHeight", Height);
                }
                catch (Exception)
                {
                    MessageBox.Show("Error storing parameter in settings file: " + Path.GetFileName(filePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }

                xmlFileReader.SaveSettings();
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowWarning("Error saving settings to file: " + filePath);
                ConsoleMsgUtils.ShowWarning(ex.Message);

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

            var primaryKeyColumn = new[] { customSICValues.Columns[COL_NAME_CUSTOM_SIC_VALUE_ROW_ID] };
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
            var lineDelimiters = new[] { '\r', '\n' };

            // Examine the clipboard contents
            var clipboardData = Clipboard.GetDataObject();

            if (clipboardData == null)
            {
                return;
            }

            if (!clipboardData.GetDataPresent(DataFormats.StringFormat, true))
            {
                return;
            }

            var data = Convert.ToString(clipboardData.GetData(DataFormats.StringFormat, true));

            // Split data on carriage return or line feed characters
            // Lines that end in CrLf will give two separate lines; one with the text, and one blank; that's OK
            var dataLines = data.Split(lineDelimiters, 50000);

            if (dataLines.Length == 0)
            {
                return;
            }

            GetCurrentCustomSICTolerances(out var defaultMZTolerance, out var defaultScanOrAcqTimeTolerance);

            if (clearList && !ClearCustomSICList(true))
            {
                return;
            }

            var rowsAlreadyPresent = 0;
            var rowsSkipped = 0;

            var tabDelimiter = new[] { '\t' };
            var commaDelimiter = new[] { ',' };

            // Auto-determine the column delimiter by counting the number of lines that are tab-delimited and counting the number that are comma delimited

            // Keys in these dictionaries are the number of columns in a line, values are the number of lines with the given column count
            var tabStats = new Dictionary<int, int>();
            var commaStats = new Dictionary<int, int>();

            foreach (var dataLine in dataLines)
            {
                var tabSeparatedColumns = dataLine.Split(tabDelimiter, 5);
                var commaSeparatedColumns = dataLine.Split(commaDelimiter, 5);

                if (tabSeparatedColumns.Length > 1)
                {
                    if (tabStats.TryGetValue(tabSeparatedColumns.Length, out var columnCount))
                        tabStats[tabSeparatedColumns.Length] = columnCount + 1;
                    else
                        tabStats.Add(tabSeparatedColumns.Length, 1);
                }

                // ReSharper disable once InvertIf
                if (commaSeparatedColumns.Length > 1)
                {
                    if (commaStats.TryGetValue(commaSeparatedColumns.Length, out var columnCount))
                        commaStats[commaSeparatedColumns.Length] = columnCount + 1;
                    else
                        commaStats.Add(commaSeparatedColumns.Length, 1);
                }
            }

            // Find the largest value in the two dictionaries
            var maxTabCount = tabStats.Values.Max();
            var maxCommaCount = commaStats.Values.Max();

            var columnDelimiterList = new List<char>();

            if (maxTabCount > maxCommaCount)
            {
                columnDelimiterList.Add('\t');
            }
            else if (maxCommaCount > maxTabCount)
            {
                columnDelimiterList.Add(',');
            }
            else
            {
                if (maxCommaCount > 0)
                    columnDelimiterList.Add(',');

                if (maxTabCount > 0 || columnDelimiterList.Count == 0)
                    columnDelimiterList.Add('\t');
            }

            var columnDelimiters = columnDelimiterList.ToArray();
            var rowNumber = 0;

            foreach (var dataLine in dataLines)
            {
                if (string.IsNullOrWhiteSpace(dataLine))
                {
                    continue;
                }

                rowNumber++;

                var columns = dataLine.Split(columnDelimiters, 5);

                if (columns.Length < 2)
                {
                    rowsSkipped++;
                    continue;
                }

                if (rowNumber == 1)
                {
                    var numericColumns = columns.Count(column => double.TryParse(column, out _));

                    if (numericColumns == 0)
                    {
                        // This is a header row; skip it (and do not increment rowsSkipped)
                        continue;
                    }
                }

                try
                {
                    double mz = 0;
                    float scanOrAcqTime = 0;
                    var comment = string.Empty;
                    var mzToleranceDa = defaultMZTolerance;
                    var scanOrAcqTimeTolerance = defaultScanOrAcqTimeTolerance;

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
                        // Assume pasted data is either
                        // m/z, m/z tolerance, scanOrTime, scanOrTime tolerance, and comment
                        //   or
                        // m/z, m/z tolerance, scanOrTime, and comment

                        mz = double.Parse(columns[0]);
                        mzToleranceDa = double.Parse(columns[1]);

                        if (Math.Abs(mzToleranceDa) < float.Epsilon)
                        {
                            mzToleranceDa = defaultMZTolerance;
                        }

                        scanOrAcqTime = float.Parse(columns[2]);

                        switch (columns.Length)
                        {
                            case >= 4 when float.TryParse(columns[3], out var timeTolerance):
                                scanOrAcqTimeTolerance = timeTolerance;
                                break;

                            case 4:
                                comment = columns[3];
                                break;

                            case >= 5:
                                comment = columns[4];
                                break;
                        }
                    }

                    if (mz > 0)
                    {
                        AddCustomSICRow(
                            mz, mzToleranceDa,
                            scanOrAcqTime, scanOrAcqTimeTolerance,
                            comment,
                            out var existingRowFound);

                        if (existingRowFound)
                        {
                            rowsAlreadyPresent++;
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip this row
                    rowsSkipped++;
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
                    message = rowsAlreadyPresent + " rows of thresholds were";
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
                    message = rowsSkipped + " rows were skipped because they";
                }

                MessageBox.Show(message + " didn't contain two columns of numeric data.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void PopulateComboBoxes()
        {
            cboExportRawDataFileFormat.Items.Clear();
            cboExportRawDataFileFormat.Items.Insert((int)RawDataExportOptions.ExportRawDataFileFormatConstants.PEKFile, "PEK File");
            cboExportRawDataFileFormat.Items.Insert((int)RawDataExportOptions.ExportRawDataFileFormatConstants.CSVFile, "DeconTools CSV File");
            cboExportRawDataFileFormat.SelectedIndex = (int)RawDataExportOptions.ExportRawDataFileFormatConstants.CSVFile;

            cboSICNoiseThresholdMode.Items.Clear();
            cboSICNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.AbsoluteThreshold, "Absolute Threshold");
            cboSICNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMeanByAbundance, "Trimmed Mean By Abundance");
            cboSICNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMeanByCount, "Trimmed Mean By Data Count");
            cboSICNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMedianByAbundance, "Trimmed Median By Abundance");
            cboSICNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.DualTrimmedMeanByAbundance, "Dual Trimmed Mean By Abundance");
            cboSICNoiseThresholdMode.SelectedIndex = (int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.DualTrimmedMeanByAbundance;

            cboMassSpectraNoiseThresholdMode.Items.Clear();
            cboMassSpectraNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.AbsoluteThreshold, "Absolute Threshold");
            cboMassSpectraNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMeanByAbundance, "Trimmed Mean By Abundance");
            cboMassSpectraNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMeanByCount, "Trimmed Mean By Data Count");
            cboMassSpectraNoiseThresholdMode.Items.Insert((int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMedianByAbundance, "Trimmed Median By Abundance");
            cboMassSpectraNoiseThresholdMode.SelectedIndex = (int)MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMedianByAbundance;

            cboReporterIonMassMode.Items.Clear();
            mReporterIonIndexToModeMap.Clear();

            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.CustomOrNone, "None");

            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.Acetylation, "Acetylated K");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.FrackingAmine20160217, "Fracking Amine 20160217: 157.089, 170.097, and 234.059");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.FSFACustomCarbonyl, "FSFACustomCarbonyl");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.FSFACustomCarboxylic, "FSFACustomCarboxylic");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.FSFACustomHydroxyl, "FSFACustomHydroxyl");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.HemeCFragment, "Heme C: 616.18 and 617.19");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.ITraqETDThreeMZ, "iTraq ETD: 101, 102, and 104");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.ITraqFourMZ, "iTraq: 114, 115, 116, and 117");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.ITraqEightMZHighRes, "iTraq 8 for High Res MS/MS: 113, 114, ... 121");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.ITraqEightMZLowRes, "iTraq 8 for Low Res MS/MS (Considers 120 m/z for immonium loss from phenylalanine)");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.LycAcetFragment, "Lys Acet: 126.091 and 127.095");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.NativeOGlcNAc, "Native OGlcNAc: 126.055, 138.055, 144.065, 168.066, 186.076, 204.087, and 366.14");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.OGlcNAc, "OGlcNAc: 204.087, 300.13, and 503.21");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.PCGalnaz, "PCGalnaz: 300.13 and 503.21");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.TMTTwoMZ, "TMT 2: 126, 127");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.TMTSixMZ, "TMT 6: 126, 127, 128, 129, 130, 131");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.TMTTenMZ, "TMT 10: 126, 127N, 127C, ... 130C, 131");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.TMTElevenMZ, "TMT 11: 126, 127N, 127C, ... 130C, 131N, 131C");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.TMTSixteenMZ, "TMT 16: 126, 127N, 127C, ... 133N, 133C, 134N");
            AppendReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.TMTEighteenMZ, "TMT 18: 126, 127N, 127C, ... 133N, 133C, 134N, 134C, 135N");

            SelectedReporterIonMode = ReporterIons.ReporterIonMassModeConstants.CustomOrNone;
        }

        private void ProcessFileUsingMASIC()
        {
            if (mWorking || !ConfirmPaths())
            {
                return;
            }

            try
            {
                txtLogMessages.ResetText();
                mMasic.Options.AbortProcessing = false;

                // Configure settings
                var success = UpdateMasicSettings(mMasic);

                if (!success)
                    return;

                // Validate settings
                success = ValidateSettings(mMasic);

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

                var startTime = DateTime.UtcNow;

                var outputDirectoryPath = txtOutputDirectoryPath.Text;
                success = mMasic.ProcessFile(txtInputFilePath.Text, outputDirectoryPath);
                Cursor.Current = Cursors.Default;

                if (mMasic.Options.AbortProcessing)
                {
                    MessageBox.Show("Canceled processing", "Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void RegisterEvents(clsMASIC masicInstance)
        {
            masicInstance.StatusEvent += StatusEventHandler;
            masicInstance.DebugEvent += DebugEventHandler;
            masicInstance.ErrorEvent += ErrorEventHandler;
            masicInstance.WarningEvent += WarningEventHandler;

            masicInstance.ProgressUpdate += MASIC_ProgressUpdate;
            masicInstance.ProgressResetKeypressAbort += MASIC_ProgressResetKeypressAbort;
            masicInstance.ProgressSubtaskChanged += MASIC_ProgressSubtaskChanged;
        }

        private void ResetToDefaults(bool confirmReset)
        {
            if (confirmReset)
            {
                var response = MessageBox.Show("Are you sure you want to reset all settings to their default values?", "Reset to Defaults", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                if (response != DialogResult.Yes)
                    return;
            }

            var masicInstance = mMasic ?? new clsMASIC();

            Width = 710;
            Height = 560;

            // File Paths and Import Options
            try
            {
                if (txtOutputDirectoryPath.TextLength == 0 || !Directory.Exists(txtOutputDirectoryPath.Text))
                {
                    txtOutputDirectoryPath.Text = AppUtils.GetAppDirectoryPath();
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

                // MASIC Export Options
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
                catch (Exception)
                {
                    txtDatasetLookupFilePath.Text = string.Empty;
                }

                // SIC Options
                var sicOptions = masicOptions.SICOptions;
                var peakFinderOptions = masicOptions.SICOptions.SICPeakFinderOptions;

                var sicTolerance = sicOptions.GetSICTolerance(out var sicToleranceIsPPM);

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
            using var fileSelector = new OpenFileDialog
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
            };

            if (txtDatasetLookupFilePath.TextLength > 0)
            {
                try
                {
                    fileSelector.InitialDirectory = Directory.GetParent(txtDatasetLookupFilePath.Text)?.ToString();
                }
                catch
                {
                    fileSelector.InitialDirectory = AppUtils.GetAppDirectoryPath();
                }
            }
            else
            {
                fileSelector.InitialDirectory = AppUtils.GetAppDirectoryPath();
            }

            fileSelector.Title = "Select dataset lookup file";

            var result = fileSelector.ShowDialog();

            if (result == DialogResult.Cancel)
                return;

            if (fileSelector.FileName.Length > 0)
            {
                txtDatasetLookupFilePath.Text = fileSelector.FileName;
            }
        }

        private void SelectCustomSICFile()
        {
            using var fileSelector = new OpenFileDialog
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
            };

            var fileExtension = ".txt";

            if (txtCustomSICFileName.TextLength > 0)
            {
                fileExtension = Path.GetExtension(txtCustomSICFileName.Text);
            }

            fileSelector.FilterIndex = fileExtension.ToLower() switch
            {
                ".txt" => 1,
                "csv" => 2,
                _ => 1
            };

            if (txtCustomSICFileName.TextLength > 0)
            {
                try
                {
                    fileSelector.InitialDirectory = Directory.GetParent(txtCustomSICFileName.Text ?? ".")?.ToString();
                }
                catch
                {
                    fileSelector.InitialDirectory = AppUtils.GetAppDirectoryPath();
                }
            }
            else
            {
                fileSelector.InitialDirectory = AppUtils.GetAppDirectoryPath();
            }

            fileSelector.Title = "Select custom SIC values file";

            var result = fileSelector.ShowDialog();

            if (result == DialogResult.Cancel)
                return;

            if (fileSelector.FileName.Length > 0)
            {
                txtCustomSICFileName.Text = fileSelector.FileName;
            }
        }

        private void SelectInputFile()
        {
            using var fileSelector = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".txt",
                DereferenceLinks = true,
                Multiselect = false,
                ValidateNames = true,
                Filter = "Thermo Raw files (*.raw)|*.raw|" +
                         "mzXML files (*.mzXML)|*.mzXML|" +
                         "mzML files (*.mzML)|*.mzML|" +
                         "mzData files (*.mzData)|*.mzData|" +
                         "Mascot Generic Format files (*.mgf)|*.mgf|" +
                         "CDF files (*.cdf)|*.cdf|" +
                         "All files (*.*)|*.*"
            };

            var fileExtension = mPreferredInputFileExtension;

            if (txtInputFilePath.TextLength > 0)
            {
                fileExtension = Path.GetExtension(txtInputFilePath.Text);
            }

            // ReSharper disable StringLiteralTypo
            fileSelector.FilterIndex = fileExtension.ToLower() switch
            {
                ".mzxml" => 2,
                "mzml" => 3,
                ".mzdata" => 4,
                ".mgf" => 5,
                ".cdf" => 6,
                _ => 1
            };
            // ReSharper restore StringLiteralTypo

            if (txtInputFilePath.TextLength > 0)
            {
                try
                {
                    fileSelector.InitialDirectory = Directory.GetParent(txtInputFilePath.Text ?? ".")?.ToString();
                }
                catch
                {
                    fileSelector.InitialDirectory = AppUtils.GetAppDirectoryPath();
                }
            }
            else
            {
                fileSelector.InitialDirectory = AppUtils.GetAppDirectoryPath();
            }

            fileSelector.Title = "Select input file";

            var result = fileSelector.ShowDialog();

            if (result == DialogResult.Cancel)
                return;

            if (fileSelector.FileName.Length > 0)
            {
                txtInputFilePath.Text = fileSelector.FileName;
                mPreferredInputFileExtension = Path.GetExtension(fileSelector.FileName);
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
            txtDatabaseConnectionString.Text = DatabaseAccess.DATABASE_CONNECTION_STRING_DEFAULT;
            txtDatasetInfoQuerySQL.Text = DatabaseAccess.DATABASE_DATASET_INFO_QUERY_DEFAULT;
        }

        private void SetCustomSICToleranceType(CustomSICList.CustomSICScanTypeConstants customSICScanToleranceType)
        {
            switch (customSICScanToleranceType)
            {
                case CustomSICList.CustomSICScanTypeConstants.Absolute:
                    optCustomSICScanToleranceAbsolute.Checked = true;
                    break;
                case CustomSICList.CustomSICScanTypeConstants.Relative:
                    optCustomSICScanToleranceRelative.Checked = true;
                    break;
                case CustomSICList.CustomSICScanTypeConstants.AcquisitionTime:
                    optCustomSICScanToleranceAcqTime.Checked = true;
                    break;
                default:
                    optCustomSICScanToleranceAbsolute.Checked = true;
                    break;
            }
        }

        /// <summary>
        /// This function can be used to prevent the form from resizing itself
        /// if the MyBase.Resize event fires within 2 seconds of the current time
        /// </summary>
        /// <remarks>See CatchUnrequestedHeightChange for more info</remarks>
        /// <param name="heightToForce"></param>
        public void SetHeightAdjustForce(int heightToForce)
        {
            mHeightAdjustForce = heightToForce;
            mHeightAdjustTime = DateTime.UtcNow;
        }

        private void SetToolTips()
        {
            var toolTipControl = new ToolTip();

            toolTipControl.SetToolTip(txtDatasetID, "The dataset ID is included as the first column in the output file.");

            toolTipControl.SetToolTip(txtIntensityThresholdAbsoluteMinimum, "Threshold for extending SIC");
            toolTipControl.SetToolTip(txtMaxDistanceScansNoOverlap, "Maximum distance that the edge of an identified peak can be away from the scan number that the parent ion was observed in if the identified peak does not contain the parent ion.");
            toolTipControl.SetToolTip(txtMaxAllowedUpwardSpikeFractionMax, "Maximum fraction of the peak maximum that an upward spike can be to be included in the peak");
            toolTipControl.SetToolTip(txtInitialPeakWidthScansScaler, "Multiplied by the S/N for the given spectrum to determine the initial minimum peak width (in scans) to try");
            toolTipControl.SetToolTip(txtInitialPeakWidthScansMaximum, "Maximum initial peak width to allow");

            toolTipControl.SetToolTip(txtSICTolerance, "Search tolerance for creating SIC; suggest 0.6 Da for ion traps and 20 ppm for TOF, FT or Orbitrap instruments");
            toolTipControl.SetToolTip(txtButterworthSamplingFrequency, "Value between 0.01 and 0.99; suggested value is 0.25");
            toolTipControl.SetToolTip(txtSavitzkyGolayFilterOrder, "Even number, 0 or greater; 0 means a moving average filter, 2 means a 2nd order Savitzky Golay filter");

            toolTipControl.SetToolTip(chkRefineReportedParentIonMZ, string.Format(
                "If enabled, will look through the m/z values in the parent ion spectrum data to find the closest match (within SICTolerance / {0:F0}); " +
                "will update the reported m/z value to the one found",
                SICOptions.DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_DA));

            //toolTipControl.SetToolTip(chkUseSICStatsFromLargestPeak, "If enabled, SIC stats for similar parent ions will all be based on the largest peak in the selected ion chromatogram");

            toolTipControl.SetToolTip(txtStatusLogKeyNameFilterList, "Enter a comma and/or NewLine separated list of Status Log Key names to match (will match any part of the key name to the text you enter).  Leave blank to include all Status Log entries.");
        }

        private void ShowAboutBox()
        {
            var message = string.Empty;

            message += "Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)" + Environment.NewLine;
            message += "Copyright 2021, Battelle Memorial Institute.  All Rights Reserved." + Environment.NewLine + Environment.NewLine;

            message += "This is version " + Application.ProductVersion + " (" + Program.PROGRAM_DATE + "). ";
            message += "Using MASIC PeakFinder DLL version " + mMasic.MASICPeakFinderDllVersion + Environment.NewLine + Environment.NewLine;

            message += "E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov" + Environment.NewLine;
            message += "Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics" + Environment.NewLine + Environment.NewLine;

            message += "Licensed under the 2-Clause BSD License; you may not use this file except in compliance with the License.  ";
            message += "You may obtain a copy of the License at https://opensource.org/licenses/BSD-2-Clause" + Environment.NewLine + Environment.NewLine;

            MessageBox.Show(message, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateCustomSICDataGridTableStyle()
        {
            // Define the PM Thresholds table style
            // Setting the MappingName of the table style to CUSTOM_SIC_VALUES_DATA_TABLE will cause this style to be used with that table
            var tsCustomSICValues = new DataGridTableStyle
            {
                MappingName = CUSTOM_SIC_VALUES_DATA_TABLE,
                AllowSorting = true,
                ColumnHeadersVisible = true,
                RowHeadersVisible = true,
                ReadOnly = false
            };

            DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_MZ, "Custom m/z", 90);
            DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_MZ_TOLERANCE, "m/z tolerance (Da)", 110);

            var timeTolerance = false;
            switch (GetCustomSICScanToleranceType())
            {
                case CustomSICList.CustomSICScanTypeConstants.Relative:
                    DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_CENTER, "Relative Scan Number (0 to 1)", 170);
                    DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_TOLERANCE, "Scan Tolerance", 90);
                    break;

                case CustomSICList.CustomSICScanTypeConstants.AcquisitionTime:
                    DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_CENTER, "Acq time (minutes)", 110);
                    DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_TOLERANCE, "Time Tolerance", 90);
                    timeTolerance = true;
                    break;

                default:
                    // Includes CustomSICScanTypeConstants.Absolute
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

        private bool UpdateMasicSettings(clsMASIC masicInstance)
        {
            var parseError = false;

            try
            {
                var masicOptions = masicInstance.Options;

                // Import options

                masicOptions.ParentIonDecoyMassDa = TextBoxUtils.ParseTextBoxValueDbl(txtParentIonDecoyMassDa, lblParentIonDecoyMassDa.Text + " must be a value", out parseError);

                if (parseError)
                    return false;

                // MASIC Export Options
                masicOptions.IncludeHeadersInExportFile = chkIncludeHeaders.Checked;
                masicOptions.IncludeScanTimesInSICStatsFile = chkIncludeScanTimesInSICStatsFile.Checked;

                masicOptions.SkipMSMSProcessing = chkSkipMSMSProcessing.Checked;
                masicOptions.SkipSICAndRawDataProcessing = chkSkipSICAndRawDataProcessing.Checked;
                masicOptions.ExportRawDataOnly = chkExportRawDataOnly.Checked;

                // Raw data export options
                var exportOptions = masicOptions.RawDataExportOptions;

                exportOptions.ExportEnabled = chkExportRawSpectraData.Checked;
                exportOptions.FileFormat = (RawDataExportOptions.ExportRawDataFileFormatConstants)cboExportRawDataFileFormat.SelectedIndex;

                exportOptions.IncludeMSMS = chkExportRawDataIncludeMSMS.Checked;
                exportOptions.RenumberScans = chkExportRawDataRenumberScans.Checked;

                exportOptions.MinimumSignalToNoiseRatio = TextBoxUtils.ParseTextBoxValueFloat(
                    txtExportRawDataSignalToNoiseRatioMinimum,
                    lblExportRawDataSignalToNoiseRatioMinimum.Text + " must be a value", out parseError);

                if (parseError)
                    return false;

                exportOptions.MaxIonCountPerScan = TextBoxUtils.ParseTextBoxValueInt(
                    txtExportRawDataMaxIonCountPerScan,
                    lblExportRawDataMaxIonCountPerScan.Text + " must be an integer value", out parseError);

                if (parseError)
                    return false;

                exportOptions.IntensityMinimum = TextBoxUtils.ParseTextBoxValueFloat(
                    txtExportRawDataIntensityMinimum,
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

                if (txtDatabaseConnectionString.TextLength > 0 && txtDatasetInfoQuerySQL.TextLength > 0)
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
                catch (Exception)
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
                peakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = (MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes)cboSICNoiseThresholdMode.SelectedIndex;
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
                peakFinderOptions.SavitzkyGolayFilterOrder = (short)(TextBoxUtils.ParseTextBoxValueInt(txtSavitzkyGolayFilterOrder, lblSavitzkyGolayFilterOrder.Text + " must be an integer value", out parseError));

                if (parseError)
                    return false;

                peakFinderOptions.FindPeaksOnSmoothedData = chkFindPeaksOnSmoothedData.Checked;
                peakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth = chkSmoothDataRegardlessOfMinimumPeakWidth.Checked;

                // Mass Spectra Noise Threshold Options
                peakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = (MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes)cboMassSpectraNoiseThresholdMode.SelectedIndex;
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
                reporterIonOptions.ReporterIonToleranceDaDefault = TextBoxUtils.ParseTextBoxValueDbl(
                    txtReporterIonMZToleranceDa, "", out parseError,
                    ReporterIons.REPORTER_ION_TOLERANCE_DA_DEFAULT, false);

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

                var customSICFileName = txtCustomSICFileName.Text.Trim();
                masicOptions.CustomSICList.CustomSICListFileName = customSICFileName;

                var mzSearchSpecs = new List<CustomMZSearchSpec>();

                // Only use the data in table CUSTOM_SIC_VALUES_DATA_TABLE if the CustomSicFileName is empty
                if (string.IsNullOrWhiteSpace(customSICFileName))
                {
                    foreach (DataRow currentRow in mCustomSICValuesDataset.Tables[CUSTOM_SIC_VALUES_DATA_TABLE].Rows)
                    {
                        if (currentRow[0] != null && double.TryParse(currentRow[0].ToString(), out var col0) &&
                            currentRow[1] != null && double.TryParse(currentRow[1].ToString(), out var col1))
                        {
                            var targetMz = col0;
                            var mzSearchSpec = new CustomMZSearchSpec(targetMz)
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

                CustomSICList.CustomSICScanTypeConstants scanType;

                if (optCustomSICScanToleranceAbsolute.Checked)
                {
                    scanType = CustomSICList.CustomSICScanTypeConstants.Absolute;
                }
                else if (optCustomSICScanToleranceRelative.Checked)
                {
                    scanType = CustomSICList.CustomSICScanTypeConstants.Relative;
                }
                else if (optCustomSICScanToleranceAcqTime.Checked)
                {
                    scanType = CustomSICList.CustomSICScanTypeConstants.AcquisitionTime;
                }
                else
                {
                    // Assume absolute
                    scanType = CustomSICList.CustomSICScanTypeConstants.Absolute;
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

        private bool ValidateSettings(clsMASIC masicInstance)
        {
            if (masicInstance.Options.ReporterIons.ReporterIonMassMode == ReporterIons.ReporterIonMassModeConstants.CustomOrNone)
                return true;

            if (masicInstance.Options.ReporterIons.ReporterIonMassMode == ReporterIons.ReporterIonMassModeConstants.ITraqEightMZHighRes)
            {
                // Make sure the tolerance is less than 0.03 Da; if not, warn the user
                if (masicInstance.Options.ReporterIons.ReporterIonToleranceDaDefault > 0.03)
                {
                    var warningMessage = string.Format(
                        "Warning: the Reporter Ion 'm/z Tolerance Half Width' value should be less than 0.03 m/z " +
                        "when using 'iTraq8 for High Res MS/MS' reporter ions. It is currently {0:F3} m/z. " +
                        "If using a low resolution instrument, you should choose the 'iTraq 8 for Low Res MS/MS' mode. " +
                        "Continue anyway?",
                        masicInstance.Options.ReporterIons.ReporterIonToleranceDaDefault);

                    var response = MessageBox.Show(warningMessage, "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

                    if (response != DialogResult.Yes)
                    {
                        return false;
                    }
                }
            }
            else if (masicInstance.Options.ReporterIons.ReporterIonMassMode == ReporterIons.ReporterIonMassModeConstants.ITraqEightMZLowRes)
            {
                // Make sure the tolerance is at least 0.1 Da; if not, warn the user
                if (masicInstance.Options.ReporterIons.ReporterIonToleranceDaDefault < 0.1)
                {
                    var warningMessage = string.Format(
                        "Warning: the Reporter Ion 'm/z Tolerance Half Width' value should be at least 0.1 m/z " +
                        "when using 'iTraq8 for Low Res MS/MS' reporter ions. It is currently {0:F3} m/z. " +
                        "If using a high resolution instrument, you should choose the 'iTraq 8 for High Res MS/MS' mode. " +
                        "Continue anyway?",
                        masicInstance.Options.ReporterIons.ReporterIonToleranceDaDefault);

                    var response = MessageBox.Show(warningMessage, "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

                    if (response != DialogResult.Yes)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void cboMassSpectraNoiseThresholdMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        private void cboReporterIonMassMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            AutoToggleReporterIonStatsEnabled();
        }

        private void cboSICNoiseThresholdMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

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
            if (e.KeyCode == Keys.A && e.Control)
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
            TextBoxUtils.TextBoxKeyPressHandler(txtInitialPeakWidthScansScaler, e, true, true);
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
            mProgressForm?.ResetKeyPressAbortProcess();
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
    }
}
