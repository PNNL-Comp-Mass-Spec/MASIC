using System;
using System.Windows.Forms;

namespace MASIC
{
    public partial class frmMain : Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.txtInputFilePath = new System.Windows.Forms.TextBox();
            this.cmdSelectFile = new System.Windows.Forms.Button();
            this.fraInputFilePath = new System.Windows.Forms.GroupBox();
            this.MainMenuControl = new System.Windows.Forms.MainMenu(this.components);
            this.mnuFile = new System.Windows.Forms.MenuItem();
            this.mnuFileSelectInputFile = new System.Windows.Forms.MenuItem();
            this.mnuFileSelectOutputDirectory = new System.Windows.Forms.MenuItem();
            this.mnuFileSep1 = new System.Windows.Forms.MenuItem();
            this.mnuFileLoadOptions = new System.Windows.Forms.MenuItem();
            this.mnuFileSaveOptions = new System.Windows.Forms.MenuItem();
            this.mnuFileSep2 = new System.Windows.Forms.MenuItem();
            this.mnuFileExit = new System.Windows.Forms.MenuItem();
            this.mnuEdit = new System.Windows.Forms.MenuItem();
            this.mnuEditProcessFile = new System.Windows.Forms.MenuItem();
            this.mnuEditSep1 = new System.Windows.Forms.MenuItem();
            this.mnuEditSaveDefaultOptions = new System.Windows.Forms.MenuItem();
            this.mnuEditResetOptions = new System.Windows.Forms.MenuItem();
            this.mnuHelp = new System.Windows.Forms.MenuItem();
            this.mnuHelpAbout = new System.Windows.Forms.MenuItem();
            this.tbsOptions = new System.Windows.Forms.TabControl();
            this.TabPageMasicExportOptions = new System.Windows.Forms.TabPage();
            this.chkWriteDetailedSICDataFile = new System.Windows.Forms.CheckBox();
            this.chkIncludeScanTimesInSICStatsFile = new System.Windows.Forms.CheckBox();
            this.txtDatasetID = new System.Windows.Forms.TextBox();
            this.lblDatasetID = new System.Windows.Forms.Label();
            this.lblRawDataExportOverview = new System.Windows.Forms.Label();
            this.fraExportAllSpectraDataPoints = new System.Windows.Forms.GroupBox();
            this.txtExportRawDataSignalToNoiseRatioMinimum = new System.Windows.Forms.TextBox();
            this.lblExportRawDataSignalToNoiseRatioMinimum = new System.Windows.Forms.Label();
            this.chkExportRawDataRenumberScans = new System.Windows.Forms.CheckBox();
            this.txtExportRawDataMaxIonCountPerScan = new System.Windows.Forms.TextBox();
            this.lblExportRawDataMaxIonCountPerScan = new System.Windows.Forms.Label();
            this.txtExportRawDataIntensityMinimum = new System.Windows.Forms.TextBox();
            this.lblExportRawDataIntensityMinimum = new System.Windows.Forms.Label();
            this.chkExportRawDataIncludeMSMS = new System.Windows.Forms.CheckBox();
            this.cboExportRawDataFileFormat = new System.Windows.Forms.ComboBox();
            this.lblExportDataPointsFormat = new System.Windows.Forms.Label();
            this.chkExportRawSpectraData = new System.Windows.Forms.CheckBox();
            this.chkIncludeHeaders = new System.Windows.Forms.CheckBox();
            this.TabPageSICOptions = new System.Windows.Forms.TabPage();
            this.fraInputFileRangeFilters = new System.Windows.Forms.GroupBox();
            this.lblTimeEndUnits = new System.Windows.Forms.Label();
            this.lblTimeStartUnits = new System.Windows.Forms.Label();
            this.txtTimeEnd = new System.Windows.Forms.TextBox();
            this.txtTimeStart = new System.Windows.Forms.TextBox();
            this.lblTimeEnd = new System.Windows.Forms.Label();
            this.lblTimeStart = new System.Windows.Forms.Label();
            this.txtScanEnd = new System.Windows.Forms.TextBox();
            this.txtScanStart = new System.Windows.Forms.TextBox();
            this.lblScanEnd = new System.Windows.Forms.Label();
            this.lblScanStart = new System.Windows.Forms.Label();
            this.cmdClearAllRangeFilters = new System.Windows.Forms.Button();
            this.lblSICOptionsOverview = new System.Windows.Forms.Label();
            this.fraSICSearchThresholds = new System.Windows.Forms.GroupBox();
            this.optSICTolerancePPM = new System.Windows.Forms.RadioButton();
            this.optSICToleranceDa = new System.Windows.Forms.RadioButton();
            this.chkRefineReportedParentIonMZ = new System.Windows.Forms.CheckBox();
            this.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData = new System.Windows.Forms.CheckBox();
            this.txtMaxPeakWidthMinutesForward = new System.Windows.Forms.TextBox();
            this.txtMaxPeakWidthMinutesBackward = new System.Windows.Forms.TextBox();
            this.txtIntensityThresholdFractionMax = new System.Windows.Forms.TextBox();
            this.lblIntensityThresholdFractionMax = new System.Windows.Forms.Label();
            this.txtIntensityThresholdAbsoluteMinimum = new System.Windows.Forms.TextBox();
            this.lblIntensityThresholdAbsoluteMinimum = new System.Windows.Forms.Label();
            this.lblMaxPeakWidthMinutesForward = new System.Windows.Forms.Label();
            this.lblMaxPeakWidthMinutesBackward = new System.Windows.Forms.Label();
            this.lblMaxPeakWidthMinutes = new System.Windows.Forms.Label();
            this.txtSICTolerance = new System.Windows.Forms.TextBox();
            this.lblSICToleranceDa = new System.Windows.Forms.Label();
            this.TabPagePeakFindingOptions = new System.Windows.Forms.TabPage();
            this.fraSICNoiseThresholds = new System.Windows.Forms.GroupBox();
            this.cboSICNoiseThresholdMode = new System.Windows.Forms.ComboBox();
            this.lblNoiseThresholdMode = new System.Windows.Forms.Label();
            this.txtSICNoiseFractionLowIntensityDataToAverage = new System.Windows.Forms.TextBox();
            this.lblSICNoiseFractionLowIntensityDataToAverage = new System.Windows.Forms.Label();
            this.txtSICNoiseThresholdIntensity = new System.Windows.Forms.TextBox();
            this.lblSICNoiseThresholdIntensity = new System.Windows.Forms.Label();
            this.fraSmoothingOptions = new System.Windows.Forms.GroupBox();
            this.chkSmoothDataRegardlessOfMinimumPeakWidth = new System.Windows.Forms.CheckBox();
            this.chkFindPeaksOnSmoothedData = new System.Windows.Forms.CheckBox();
            this.optUseSavitzkyGolaySmooth = new System.Windows.Forms.RadioButton();
            this.txtButterworthSamplingFrequency = new System.Windows.Forms.TextBox();
            this.lblButterworthSamplingFrequency = new System.Windows.Forms.Label();
            this.txtSavitzkyGolayFilterOrder = new System.Windows.Forms.TextBox();
            this.lblSavitzkyGolayFilterOrder = new System.Windows.Forms.Label();
            this.optUseButterworthSmooth = new System.Windows.Forms.RadioButton();
            this.fraPeakFindingOptions = new System.Windows.Forms.GroupBox();
            this.txtInitialPeakWidthScansMaximum = new System.Windows.Forms.TextBox();
            this.lblInitialPeakWidthScansMaximum = new System.Windows.Forms.Label();
            this.txtInitialPeakWidthScansScaler = new System.Windows.Forms.TextBox();
            this.lblInitialPeakWidthScansScaler = new System.Windows.Forms.Label();
            this.txtMaxAllowedUpwardSpikeFractionMax = new System.Windows.Forms.TextBox();
            this.lblMaxAllowedUpwardSpikeFractionMax = new System.Windows.Forms.Label();
            this.txtMaxDistanceScansNoOverlap = new System.Windows.Forms.TextBox();
            this.lblMaxDistanceScansNoOverlap = new System.Windows.Forms.Label();
            this.TabPageBinningAndSimilarityOptions = new System.Windows.Forms.TabPage();
            this.fraMassSpectraNoiseThresholds = new System.Windows.Forms.GroupBox();
            this.txtMassSpectraNoiseMinimumSignalToNoiseRatio = new System.Windows.Forms.TextBox();
            this.lblMassSpectraNoiseMinimumSignalToNoiseRatio = new System.Windows.Forms.Label();
            this.txtMassSpectraNoiseThresholdIntensity = new System.Windows.Forms.TextBox();
            this.txtMassSpectraNoiseFractionLowIntensityDataToAverage = new System.Windows.Forms.TextBox();
            this.lblMassSpectraNoiseFractionLowIntensityDataToAverage = new System.Windows.Forms.Label();
            this.cboMassSpectraNoiseThresholdMode = new System.Windows.Forms.ComboBox();
            this.lblMassSpectraNoiseThresholdMode = new System.Windows.Forms.Label();
            this.lblMassSpectraNoiseThresholdIntensity = new System.Windows.Forms.Label();
            this.fraBinningIntensityOptions = new System.Windows.Forms.GroupBox();
            this.lblBinnedDataIntensityPrecisionPctUnits = new System.Windows.Forms.Label();
            this.chkBinnedDataSumAllIntensitiesForBin = new System.Windows.Forms.CheckBox();
            this.chkBinnedDataNormalize = new System.Windows.Forms.CheckBox();
            this.txtBinnedDataIntensityPrecisionPct = new System.Windows.Forms.TextBox();
            this.lblBinnedDataIntensityPrecisionPct = new System.Windows.Forms.Label();
            this.fraSpectrumSimilarityOptions = new System.Windows.Forms.GroupBox();
            this.lblSimilarIonTimeToleranceHalfWidthUnits = new System.Windows.Forms.Label();
            this.txtSpectrumSimilarityMinimum = new System.Windows.Forms.TextBox();
            this.lblSpectrumSimilarityMinimum = new System.Windows.Forms.Label();
            this.txtSimilarIonToleranceHalfWidthMinutes = new System.Windows.Forms.TextBox();
            this.lblSimilarIonTimeToleranceHalfWidth = new System.Windows.Forms.Label();
            this.txtSimilarIonMZToleranceHalfWidth = new System.Windows.Forms.TextBox();
            this.lblSimilarIonMZToleranceHalfWidth = new System.Windows.Forms.Label();
            this.fraBinningMZOptions = new System.Windows.Forms.GroupBox();
            this.txtMaximumBinCount = new System.Windows.Forms.TextBox();
            this.lblMaximumBinCount = new System.Windows.Forms.Label();
            this.txtBinSize = new System.Windows.Forms.TextBox();
            this.lblBinSize = new System.Windows.Forms.Label();
            this.txtBinEndX = new System.Windows.Forms.TextBox();
            this.lblBinEndX = new System.Windows.Forms.Label();
            this.txtBinStartX = new System.Windows.Forms.TextBox();
            this.lblBinStartX = new System.Windows.Forms.Label();
            this.TabPageCustomSICOptions = new System.Windows.Forms.TabPage();
            this.txtCustomSICFileDescription = new System.Windows.Forms.TextBox();
            this.cmdSelectCustomSICFile = new System.Windows.Forms.Button();
            this.txtCustomSICFileName = new System.Windows.Forms.TextBox();
            this.fraCustomSICControls = new System.Windows.Forms.GroupBox();
            this.lblCustomSICToleranceType = new System.Windows.Forms.Label();
            this.optCustomSICScanToleranceAcqTime = new System.Windows.Forms.RadioButton();
            this.optCustomSICScanToleranceRelative = new System.Windows.Forms.RadioButton();
            this.optCustomSICScanToleranceAbsolute = new System.Windows.Forms.RadioButton();
            this.chkLimitSearchToCustomMZs = new System.Windows.Forms.CheckBox();
            this.txtCustomSICScanOrAcqTimeTolerance = new System.Windows.Forms.TextBox();
            this.lblCustomSICScanTolerance = new System.Windows.Forms.Label();
            this.cmdPasteCustomSICList = new System.Windows.Forms.Button();
            this.cmdCustomSICValuesPopulate = new System.Windows.Forms.Button();
            this.cmdClearCustomSICList = new System.Windows.Forms.Button();
            this.dgCustomSICValues = new System.Windows.Forms.DataGrid();
            this.TabPageReporterIons = new System.Windows.Forms.TabPage();
            this.fraDecoyOptions = new System.Windows.Forms.GroupBox();
            this.lblParentIonDecoyMassDaUnits = new System.Windows.Forms.Label();
            this.txtParentIonDecoyMassDa = new System.Windows.Forms.TextBox();
            this.lblParentIonDecoyMassDa = new System.Windows.Forms.Label();
            this.fraMRMOptions = new System.Windows.Forms.GroupBox();
            this.chkMRMWriteIntensityCrosstab = new System.Windows.Forms.CheckBox();
            this.lblMRMInfo = new System.Windows.Forms.Label();
            this.chkMRMWriteDataList = new System.Windows.Forms.CheckBox();
            this.fraReporterIonMassMode = new System.Windows.Forms.GroupBox();
            this.cboReporterIonMassMode = new System.Windows.Forms.ComboBox();
            this.fraReporterIonOptions = new System.Windows.Forms.GroupBox();
            this.chkReporterIonApplyAbundanceCorrection = new System.Windows.Forms.CheckBox();
            this.chkReporterIonSaveUncorrectedIntensities = new System.Windows.Forms.CheckBox();
            this.chkReporterIonSaveObservedMasses = new System.Windows.Forms.CheckBox();
            this.txtReporterIonMZToleranceDa = new System.Windows.Forms.TextBox();
            this.lblReporterIonMZToleranceDa = new System.Windows.Forms.Label();
            this.chkReporterIonStatsEnabled = new System.Windows.Forms.CheckBox();
            this.TabPageAdvancedOptions = new System.Windows.Forms.TabPage();
            this.fraAdditionalInfoFiles = new System.Windows.Forms.GroupBox();
            this.chkConsolidateConstantExtendedHeaderValues = new System.Windows.Forms.CheckBox();
            this.lblStatusLogKeyNameFilterList = new System.Windows.Forms.Label();
            this.txtStatusLogKeyNameFilterList = new System.Windows.Forms.TextBox();
            this.chkSaveExtendedStatsFileIncludeStatusLog = new System.Windows.Forms.CheckBox();
            this.chkSaveExtendedStatsFileIncludeFilterText = new System.Windows.Forms.CheckBox();
            this.chkSaveMSTuneFile = new System.Windows.Forms.CheckBox();
            this.chkSaveMSMethodFile = new System.Windows.Forms.CheckBox();
            this.chkSaveExtendedStatsFile = new System.Windows.Forms.CheckBox();
            this.fraDatasetLookupInfo = new System.Windows.Forms.GroupBox();
            this.cmdSetConnectionStringToPNNLServer = new System.Windows.Forms.Button();
            this.txtDatasetInfoQuerySQL = new System.Windows.Forms.TextBox();
            this.lblDatasetInfoQuerySQL = new System.Windows.Forms.Label();
            this.txtDatabaseConnectionString = new System.Windows.Forms.TextBox();
            this.lblDatabaseConnectionString = new System.Windows.Forms.Label();
            this.lblDatasetLookupFilePath = new System.Windows.Forms.Label();
            this.cmdSelectDatasetLookupFile = new System.Windows.Forms.Button();
            this.txtDatasetLookupFilePath = new System.Windows.Forms.TextBox();
            this.fraMemoryConservationOptions = new System.Windows.Forms.GroupBox();
            this.chkSkipMSMSProcessing = new System.Windows.Forms.CheckBox();
            this.chkSkipSICAndRawDataProcessing = new System.Windows.Forms.CheckBox();
            this.chkExportRawDataOnly = new System.Windows.Forms.CheckBox();
            this.TabPageLog = new System.Windows.Forms.TabPage();
            this.txtLogMessages = new System.Windows.Forms.TextBox();
            this.fraOutputDirectoryPath = new System.Windows.Forms.GroupBox();
            this.cmdStartProcessing = new System.Windows.Forms.Button();
            this.cmdSelectOutputDirectory = new System.Windows.Forms.Button();
            this.txtOutputDirectoryPath = new System.Windows.Forms.TextBox();
            this.fraInputFilePath.SuspendLayout();
            this.tbsOptions.SuspendLayout();
            this.TabPageMasicExportOptions.SuspendLayout();
            this.fraExportAllSpectraDataPoints.SuspendLayout();
            this.TabPageSICOptions.SuspendLayout();
            this.fraInputFileRangeFilters.SuspendLayout();
            this.fraSICSearchThresholds.SuspendLayout();
            this.TabPagePeakFindingOptions.SuspendLayout();
            this.fraSICNoiseThresholds.SuspendLayout();
            this.fraSmoothingOptions.SuspendLayout();
            this.fraPeakFindingOptions.SuspendLayout();
            this.TabPageBinningAndSimilarityOptions.SuspendLayout();
            this.fraMassSpectraNoiseThresholds.SuspendLayout();
            this.fraBinningIntensityOptions.SuspendLayout();
            this.fraSpectrumSimilarityOptions.SuspendLayout();
            this.fraBinningMZOptions.SuspendLayout();
            this.TabPageCustomSICOptions.SuspendLayout();
            this.fraCustomSICControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgCustomSICValues)).BeginInit();
            this.TabPageReporterIons.SuspendLayout();
            this.fraDecoyOptions.SuspendLayout();
            this.fraMRMOptions.SuspendLayout();
            this.fraReporterIonMassMode.SuspendLayout();
            this.fraReporterIonOptions.SuspendLayout();
            this.TabPageAdvancedOptions.SuspendLayout();
            this.fraAdditionalInfoFiles.SuspendLayout();
            this.fraDatasetLookupInfo.SuspendLayout();
            this.fraMemoryConservationOptions.SuspendLayout();
            this.TabPageLog.SuspendLayout();
            this.fraOutputDirectoryPath.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtInputFilePath
            // 
            this.txtInputFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInputFilePath.Location = new System.Drawing.Point(104, 26);
            this.txtInputFilePath.Name = "txtInputFilePath";
            this.txtInputFilePath.Size = new System.Drawing.Size(769, 20);
            this.txtInputFilePath.TabIndex = 1;
            // 
            // cmdSelectFile
            // 
            this.cmdSelectFile.Location = new System.Drawing.Point(8, 24);
            this.cmdSelectFile.Name = "cmdSelectFile";
            this.cmdSelectFile.Size = new System.Drawing.Size(80, 24);
            this.cmdSelectFile.TabIndex = 0;
            this.cmdSelectFile.Text = "&Select File";
            this.cmdSelectFile.Click += new System.EventHandler(this.cmdSelectFile_Click);
            // 
            // fraInputFilePath
            // 
            this.fraInputFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fraInputFilePath.Controls.Add(this.cmdSelectFile);
            this.fraInputFilePath.Controls.Add(this.txtInputFilePath);
            this.fraInputFilePath.Location = new System.Drawing.Point(8, 8);
            this.fraInputFilePath.Name = "fraInputFilePath";
            this.fraInputFilePath.Size = new System.Drawing.Size(889, 64);
            this.fraInputFilePath.TabIndex = 0;
            this.fraInputFilePath.TabStop = false;
            this.fraInputFilePath.Text = "Input File Path (Thermo .Raw or Agilent .CDF/.MGF combo or mzXML or mzData)";
            // 
            // MainMenuControl
            // 
            this.MainMenuControl.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuFile,
            this.mnuEdit,
            this.mnuHelp});
            // 
            // mnuFile
            // 
            this.mnuFile.Index = 0;
            this.mnuFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuFileSelectInputFile,
            this.mnuFileSelectOutputDirectory,
            this.mnuFileSep1,
            this.mnuFileLoadOptions,
            this.mnuFileSaveOptions,
            this.mnuFileSep2,
            this.mnuFileExit});
            this.mnuFile.Text = "&File";
            // 
            // mnuFileSelectInputFile
            // 
            this.mnuFileSelectInputFile.Index = 0;
            this.mnuFileSelectInputFile.Text = "&Select Input File ...";
            this.mnuFileSelectInputFile.Click += new System.EventHandler(this.mnuFileSelectInputFile_Click);
            // 
            // mnuFileSelectOutputDirectory
            // 
            this.mnuFileSelectOutputDirectory.Index = 1;
            this.mnuFileSelectOutputDirectory.Text = "Select Output &Directory ...";
            this.mnuFileSelectOutputDirectory.Click += new System.EventHandler(this.mnuFileSelectOutputDirectory_Click);
            // 
            // mnuFileSep1
            // 
            this.mnuFileSep1.Index = 2;
            this.mnuFileSep1.Text = "-";
            // 
            // mnuFileLoadOptions
            // 
            this.mnuFileLoadOptions.Index = 3;
            this.mnuFileLoadOptions.Text = "&Load Options ...";
            this.mnuFileLoadOptions.Click += new System.EventHandler(this.mnuFileLoadOptions_Click);
            // 
            // mnuFileSaveOptions
            // 
            this.mnuFileSaveOptions.Index = 4;
            this.mnuFileSaveOptions.Text = "Sa&ve Options ...";
            this.mnuFileSaveOptions.Click += new System.EventHandler(this.mnuFileSaveOptions_Click);
            // 
            // mnuFileSep2
            // 
            this.mnuFileSep2.Index = 5;
            this.mnuFileSep2.Text = "-";
            // 
            // mnuFileExit
            // 
            this.mnuFileExit.Index = 6;
            this.mnuFileExit.Text = "E&xit";
            this.mnuFileExit.Click += new System.EventHandler(this.mnuFileExit_Click);
            // 
            // mnuEdit
            // 
            this.mnuEdit.Index = 1;
            this.mnuEdit.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuEditProcessFile,
            this.mnuEditSep1,
            this.mnuEditSaveDefaultOptions,
            this.mnuEditResetOptions});
            this.mnuEdit.Text = "&Edit";
            // 
            // mnuEditProcessFile
            // 
            this.mnuEditProcessFile.Index = 0;
            this.mnuEditProcessFile.Text = "&Process File";
            this.mnuEditProcessFile.Click += new System.EventHandler(this.mnuEditProcessFile_Click);
            // 
            // mnuEditSep1
            // 
            this.mnuEditSep1.Index = 1;
            this.mnuEditSep1.Text = "-";
            // 
            // mnuEditSaveDefaultOptions
            // 
            this.mnuEditSaveDefaultOptions.Index = 2;
            this.mnuEditSaveDefaultOptions.Text = "&Save current options as Default ...";
            this.mnuEditSaveDefaultOptions.Click += new System.EventHandler(this.mnuEditSaveDefaultOptions_Click);
            // 
            // mnuEditResetOptions
            // 
            this.mnuEditResetOptions.Index = 3;
            this.mnuEditResetOptions.Text = "&Reset options to Defaults";
            this.mnuEditResetOptions.Click += new System.EventHandler(this.mnuEditResetOptions_Click);
            // 
            // mnuHelp
            // 
            this.mnuHelp.Index = 2;
            this.mnuHelp.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuHelpAbout});
            this.mnuHelp.Text = "&Help";
            // 
            // mnuHelpAbout
            // 
            this.mnuHelpAbout.Index = 0;
            this.mnuHelpAbout.Text = "&About";
            this.mnuHelpAbout.Click += new System.EventHandler(this.mnuHelpAbout_Click);
            // 
            // tbsOptions
            // 
            this.tbsOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbsOptions.Controls.Add(this.TabPageMasicExportOptions);
            this.tbsOptions.Controls.Add(this.TabPageSICOptions);
            this.tbsOptions.Controls.Add(this.TabPagePeakFindingOptions);
            this.tbsOptions.Controls.Add(this.TabPageBinningAndSimilarityOptions);
            this.tbsOptions.Controls.Add(this.TabPageCustomSICOptions);
            this.tbsOptions.Controls.Add(this.TabPageReporterIons);
            this.tbsOptions.Controls.Add(this.TabPageAdvancedOptions);
            this.tbsOptions.Controls.Add(this.TabPageLog);
            this.tbsOptions.Location = new System.Drawing.Point(8, 176);
            this.tbsOptions.Name = "tbsOptions";
            this.tbsOptions.SelectedIndex = 0;
            this.tbsOptions.Size = new System.Drawing.Size(893, 308);
            this.tbsOptions.TabIndex = 2;
            // 
            // TabPageMasicExportOptions
            // 
            this.TabPageMasicExportOptions.Controls.Add(this.chkWriteDetailedSICDataFile);
            this.TabPageMasicExportOptions.Controls.Add(this.chkIncludeScanTimesInSICStatsFile);
            this.TabPageMasicExportOptions.Controls.Add(this.txtDatasetID);
            this.TabPageMasicExportOptions.Controls.Add(this.lblDatasetID);
            this.TabPageMasicExportOptions.Controls.Add(this.lblRawDataExportOverview);
            this.TabPageMasicExportOptions.Controls.Add(this.fraExportAllSpectraDataPoints);
            this.TabPageMasicExportOptions.Controls.Add(this.chkIncludeHeaders);
            this.TabPageMasicExportOptions.Location = new System.Drawing.Point(4, 22);
            this.TabPageMasicExportOptions.Name = "TabPageMasicExportOptions";
            this.TabPageMasicExportOptions.Size = new System.Drawing.Size(885, 282);
            this.TabPageMasicExportOptions.TabIndex = 4;
            this.TabPageMasicExportOptions.Text = "Export Options";
            this.TabPageMasicExportOptions.UseVisualStyleBackColor = true;
            // 
            // chkWriteDetailedSICDataFile
            // 
            this.chkWriteDetailedSICDataFile.Location = new System.Drawing.Point(16, 54);
            this.chkWriteDetailedSICDataFile.Name = "chkWriteDetailedSICDataFile";
            this.chkWriteDetailedSICDataFile.Size = new System.Drawing.Size(208, 16);
            this.chkWriteDetailedSICDataFile.TabIndex = 7;
            this.chkWriteDetailedSICDataFile.Text = "Write detailed SIC data file";
            // 
            // chkIncludeScanTimesInSICStatsFile
            // 
            this.chkIncludeScanTimesInSICStatsFile.Checked = true;
            this.chkIncludeScanTimesInSICStatsFile.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIncludeScanTimesInSICStatsFile.Location = new System.Drawing.Point(16, 32);
            this.chkIncludeScanTimesInSICStatsFile.Name = "chkIncludeScanTimesInSICStatsFile";
            this.chkIncludeScanTimesInSICStatsFile.Size = new System.Drawing.Size(208, 16);
            this.chkIncludeScanTimesInSICStatsFile.TabIndex = 6;
            this.chkIncludeScanTimesInSICStatsFile.Text = "Include scan times in SIC stats file";
            // 
            // txtDatasetID
            // 
            this.txtDatasetID.Location = new System.Drawing.Point(504, 16);
            this.txtDatasetID.Name = "txtDatasetID";
            this.txtDatasetID.Size = new System.Drawing.Size(88, 20);
            this.txtDatasetID.TabIndex = 4;
            this.txtDatasetID.Text = "0";
            this.txtDatasetID.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtDatasetID_KeyPress);
            // 
            // lblDatasetID
            // 
            this.lblDatasetID.Location = new System.Drawing.Point(360, 16);
            this.lblDatasetID.Name = "lblDatasetID";
            this.lblDatasetID.Size = new System.Drawing.Size(152, 16);
            this.lblDatasetID.TabIndex = 3;
            this.lblDatasetID.Text = "Input File Dataset Number";
            // 
            // lblRawDataExportOverview
            // 
            this.lblRawDataExportOverview.Location = new System.Drawing.Point(368, 48);
            this.lblRawDataExportOverview.Name = "lblRawDataExportOverview";
            this.lblRawDataExportOverview.Size = new System.Drawing.Size(256, 232);
            this.lblRawDataExportOverview.TabIndex = 5;
            this.lblRawDataExportOverview.Text = "Raw Data Export Options Overview";
            // 
            // fraExportAllSpectraDataPoints
            // 
            this.fraExportAllSpectraDataPoints.Controls.Add(this.txtExportRawDataSignalToNoiseRatioMinimum);
            this.fraExportAllSpectraDataPoints.Controls.Add(this.lblExportRawDataSignalToNoiseRatioMinimum);
            this.fraExportAllSpectraDataPoints.Controls.Add(this.chkExportRawDataRenumberScans);
            this.fraExportAllSpectraDataPoints.Controls.Add(this.txtExportRawDataMaxIonCountPerScan);
            this.fraExportAllSpectraDataPoints.Controls.Add(this.lblExportRawDataMaxIonCountPerScan);
            this.fraExportAllSpectraDataPoints.Controls.Add(this.txtExportRawDataIntensityMinimum);
            this.fraExportAllSpectraDataPoints.Controls.Add(this.lblExportRawDataIntensityMinimum);
            this.fraExportAllSpectraDataPoints.Controls.Add(this.chkExportRawDataIncludeMSMS);
            this.fraExportAllSpectraDataPoints.Controls.Add(this.cboExportRawDataFileFormat);
            this.fraExportAllSpectraDataPoints.Controls.Add(this.lblExportDataPointsFormat);
            this.fraExportAllSpectraDataPoints.Controls.Add(this.chkExportRawSpectraData);
            this.fraExportAllSpectraDataPoints.Location = new System.Drawing.Point(16, 81);
            this.fraExportAllSpectraDataPoints.Name = "fraExportAllSpectraDataPoints";
            this.fraExportAllSpectraDataPoints.Size = new System.Drawing.Size(344, 192);
            this.fraExportAllSpectraDataPoints.TabIndex = 2;
            this.fraExportAllSpectraDataPoints.TabStop = false;
            this.fraExportAllSpectraDataPoints.Text = "Raw Data Point Export Options";
            // 
            // txtExportRawDataSignalToNoiseRatioMinimum
            // 
            this.txtExportRawDataSignalToNoiseRatioMinimum.Location = new System.Drawing.Point(200, 112);
            this.txtExportRawDataSignalToNoiseRatioMinimum.Name = "txtExportRawDataSignalToNoiseRatioMinimum";
            this.txtExportRawDataSignalToNoiseRatioMinimum.Size = new System.Drawing.Size(40, 20);
            this.txtExportRawDataSignalToNoiseRatioMinimum.TabIndex = 6;
            this.txtExportRawDataSignalToNoiseRatioMinimum.Text = "1";
            this.txtExportRawDataSignalToNoiseRatioMinimum.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtExportRawDataSignalToNoiseRatioMinimum_KeyPress);
            // 
            // lblExportRawDataSignalToNoiseRatioMinimum
            // 
            this.lblExportRawDataSignalToNoiseRatioMinimum.Location = new System.Drawing.Point(16, 114);
            this.lblExportRawDataSignalToNoiseRatioMinimum.Name = "lblExportRawDataSignalToNoiseRatioMinimum";
            this.lblExportRawDataSignalToNoiseRatioMinimum.Size = new System.Drawing.Size(176, 16);
            this.lblExportRawDataSignalToNoiseRatioMinimum.TabIndex = 5;
            this.lblExportRawDataSignalToNoiseRatioMinimum.Text = "Minimum Signal to Noise Ratio";
            // 
            // chkExportRawDataRenumberScans
            // 
            this.chkExportRawDataRenumberScans.Location = new System.Drawing.Point(16, 66);
            this.chkExportRawDataRenumberScans.Name = "chkExportRawDataRenumberScans";
            this.chkExportRawDataRenumberScans.Size = new System.Drawing.Size(312, 16);
            this.chkExportRawDataRenumberScans.TabIndex = 3;
            this.chkExportRawDataRenumberScans.Text = "Renumber survey scan spectra to make sequential";
            // 
            // txtExportRawDataMaxIonCountPerScan
            // 
            this.txtExportRawDataMaxIonCountPerScan.Location = new System.Drawing.Point(200, 136);
            this.txtExportRawDataMaxIonCountPerScan.Name = "txtExportRawDataMaxIonCountPerScan";
            this.txtExportRawDataMaxIonCountPerScan.Size = new System.Drawing.Size(56, 20);
            this.txtExportRawDataMaxIonCountPerScan.TabIndex = 8;
            this.txtExportRawDataMaxIonCountPerScan.Text = "200";
            this.txtExportRawDataMaxIonCountPerScan.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtExportRawDataMaxIonCountPerScan_KeyPress);
            // 
            // lblExportRawDataMaxIonCountPerScan
            // 
            this.lblExportRawDataMaxIonCountPerScan.Location = new System.Drawing.Point(16, 138);
            this.lblExportRawDataMaxIonCountPerScan.Name = "lblExportRawDataMaxIonCountPerScan";
            this.lblExportRawDataMaxIonCountPerScan.Size = new System.Drawing.Size(184, 16);
            this.lblExportRawDataMaxIonCountPerScan.TabIndex = 7;
            this.lblExportRawDataMaxIonCountPerScan.Text = "Maximum Ion Count per Scan";
            // 
            // txtExportRawDataIntensityMinimum
            // 
            this.txtExportRawDataIntensityMinimum.Location = new System.Drawing.Point(200, 160);
            this.txtExportRawDataIntensityMinimum.Name = "txtExportRawDataIntensityMinimum";
            this.txtExportRawDataIntensityMinimum.Size = new System.Drawing.Size(88, 20);
            this.txtExportRawDataIntensityMinimum.TabIndex = 10;
            this.txtExportRawDataIntensityMinimum.Text = "0";
            this.txtExportRawDataIntensityMinimum.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtExportRawDataIntensityMinimum_KeyPress);
            // 
            // lblExportRawDataIntensityMinimum
            // 
            this.lblExportRawDataIntensityMinimum.Location = new System.Drawing.Point(16, 162);
            this.lblExportRawDataIntensityMinimum.Name = "lblExportRawDataIntensityMinimum";
            this.lblExportRawDataIntensityMinimum.Size = new System.Drawing.Size(152, 16);
            this.lblExportRawDataIntensityMinimum.TabIndex = 9;
            this.lblExportRawDataIntensityMinimum.Text = "Minimum Intensity (counts)";
            // 
            // chkExportRawDataIncludeMSMS
            // 
            this.chkExportRawDataIncludeMSMS.Location = new System.Drawing.Point(16, 86);
            this.chkExportRawDataIncludeMSMS.Name = "chkExportRawDataIncludeMSMS";
            this.chkExportRawDataIncludeMSMS.Size = new System.Drawing.Size(320, 16);
            this.chkExportRawDataIncludeMSMS.TabIndex = 4;
            this.chkExportRawDataIncludeMSMS.Text = "Export MS/MS Spectra, in addition to survey scan spectra";
            this.chkExportRawDataIncludeMSMS.CheckedChanged += new System.EventHandler(this.chkExportRawDataIncludeMSMS_CheckedChanged);
            // 
            // cboExportRawDataFileFormat
            // 
            this.cboExportRawDataFileFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboExportRawDataFileFormat.Location = new System.Drawing.Point(88, 40);
            this.cboExportRawDataFileFormat.Name = "cboExportRawDataFileFormat";
            this.cboExportRawDataFileFormat.Size = new System.Drawing.Size(144, 21);
            this.cboExportRawDataFileFormat.TabIndex = 2;
            // 
            // lblExportDataPointsFormat
            // 
            this.lblExportDataPointsFormat.Location = new System.Drawing.Point(32, 44);
            this.lblExportDataPointsFormat.Name = "lblExportDataPointsFormat";
            this.lblExportDataPointsFormat.Size = new System.Drawing.Size(72, 16);
            this.lblExportDataPointsFormat.TabIndex = 1;
            this.lblExportDataPointsFormat.Text = "Format:";
            // 
            // chkExportRawSpectraData
            // 
            this.chkExportRawSpectraData.Location = new System.Drawing.Point(16, 24);
            this.chkExportRawSpectraData.Name = "chkExportRawSpectraData";
            this.chkExportRawSpectraData.Size = new System.Drawing.Size(240, 16);
            this.chkExportRawSpectraData.TabIndex = 0;
            this.chkExportRawSpectraData.Text = "Export All Spectra Data Points";
            this.chkExportRawSpectraData.CheckedChanged += new System.EventHandler(this.chkExportRawSpectraData_CheckedChanged);
            // 
            // chkIncludeHeaders
            // 
            this.chkIncludeHeaders.Checked = true;
            this.chkIncludeHeaders.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIncludeHeaders.Location = new System.Drawing.Point(16, 10);
            this.chkIncludeHeaders.Name = "chkIncludeHeaders";
            this.chkIncludeHeaders.Size = new System.Drawing.Size(160, 16);
            this.chkIncludeHeaders.TabIndex = 0;
            this.chkIncludeHeaders.Text = "Include Column Headers";
            // 
            // TabPageSICOptions
            // 
            this.TabPageSICOptions.Controls.Add(this.fraInputFileRangeFilters);
            this.TabPageSICOptions.Controls.Add(this.lblSICOptionsOverview);
            this.TabPageSICOptions.Controls.Add(this.fraSICSearchThresholds);
            this.TabPageSICOptions.Location = new System.Drawing.Point(4, 22);
            this.TabPageSICOptions.Name = "TabPageSICOptions";
            this.TabPageSICOptions.Size = new System.Drawing.Size(885, 282);
            this.TabPageSICOptions.TabIndex = 5;
            this.TabPageSICOptions.Text = "SIC Options";
            this.TabPageSICOptions.UseVisualStyleBackColor = true;
            // 
            // fraInputFileRangeFilters
            // 
            this.fraInputFileRangeFilters.Controls.Add(this.lblTimeEndUnits);
            this.fraInputFileRangeFilters.Controls.Add(this.lblTimeStartUnits);
            this.fraInputFileRangeFilters.Controls.Add(this.txtTimeEnd);
            this.fraInputFileRangeFilters.Controls.Add(this.txtTimeStart);
            this.fraInputFileRangeFilters.Controls.Add(this.lblTimeEnd);
            this.fraInputFileRangeFilters.Controls.Add(this.lblTimeStart);
            this.fraInputFileRangeFilters.Controls.Add(this.txtScanEnd);
            this.fraInputFileRangeFilters.Controls.Add(this.txtScanStart);
            this.fraInputFileRangeFilters.Controls.Add(this.lblScanEnd);
            this.fraInputFileRangeFilters.Controls.Add(this.lblScanStart);
            this.fraInputFileRangeFilters.Controls.Add(this.cmdClearAllRangeFilters);
            this.fraInputFileRangeFilters.Location = new System.Drawing.Point(16, 201);
            this.fraInputFileRangeFilters.Name = "fraInputFileRangeFilters";
            this.fraInputFileRangeFilters.Size = new System.Drawing.Size(488, 71);
            this.fraInputFileRangeFilters.TabIndex = 1;
            this.fraInputFileRangeFilters.TabStop = false;
            this.fraInputFileRangeFilters.Text = "Input File Range Filters";
            // 
            // lblTimeEndUnits
            // 
            this.lblTimeEndUnits.Location = new System.Drawing.Point(416, 45);
            this.lblTimeEndUnits.Name = "lblTimeEndUnits";
            this.lblTimeEndUnits.Size = new System.Drawing.Size(64, 16);
            this.lblTimeEndUnits.TabIndex = 10;
            this.lblTimeEndUnits.Text = "minutes";
            // 
            // lblTimeStartUnits
            // 
            this.lblTimeStartUnits.Location = new System.Drawing.Point(416, 21);
            this.lblTimeStartUnits.Name = "lblTimeStartUnits";
            this.lblTimeStartUnits.Size = new System.Drawing.Size(64, 15);
            this.lblTimeStartUnits.TabIndex = 7;
            this.lblTimeStartUnits.Text = "minutes";
            // 
            // txtTimeEnd
            // 
            this.txtTimeEnd.Location = new System.Drawing.Point(352, 42);
            this.txtTimeEnd.Name = "txtTimeEnd";
            this.txtTimeEnd.Size = new System.Drawing.Size(56, 20);
            this.txtTimeEnd.TabIndex = 9;
            this.txtTimeEnd.Text = "0";
            this.txtTimeEnd.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtTimeEnd_KeyPress);
            // 
            // txtTimeStart
            // 
            this.txtTimeStart.Location = new System.Drawing.Point(352, 19);
            this.txtTimeStart.Name = "txtTimeStart";
            this.txtTimeStart.Size = new System.Drawing.Size(56, 20);
            this.txtTimeStart.TabIndex = 6;
            this.txtTimeStart.Text = "0";
            this.txtTimeStart.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtTimeStart_KeyPress);
            // 
            // lblTimeEnd
            // 
            this.lblTimeEnd.Location = new System.Drawing.Point(280, 45);
            this.lblTimeEnd.Name = "lblTimeEnd";
            this.lblTimeEnd.Size = new System.Drawing.Size(72, 16);
            this.lblTimeEnd.TabIndex = 8;
            this.lblTimeEnd.Text = "End Time";
            // 
            // lblTimeStart
            // 
            this.lblTimeStart.Location = new System.Drawing.Point(280, 21);
            this.lblTimeStart.Name = "lblTimeStart";
            this.lblTimeStart.Size = new System.Drawing.Size(64, 15);
            this.lblTimeStart.TabIndex = 5;
            this.lblTimeStart.Text = "Start Time";
            // 
            // txtScanEnd
            // 
            this.txtScanEnd.Location = new System.Drawing.Point(192, 42);
            this.txtScanEnd.Name = "txtScanEnd";
            this.txtScanEnd.Size = new System.Drawing.Size(56, 20);
            this.txtScanEnd.TabIndex = 4;
            this.txtScanEnd.Text = "0";
            this.txtScanEnd.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtScanEnd_KeyPress);
            // 
            // txtScanStart
            // 
            this.txtScanStart.Location = new System.Drawing.Point(192, 19);
            this.txtScanStart.Name = "txtScanStart";
            this.txtScanStart.Size = new System.Drawing.Size(56, 20);
            this.txtScanStart.TabIndex = 2;
            this.txtScanStart.Text = "0";
            this.txtScanStart.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtScanStart_KeyPress);
            // 
            // lblScanEnd
            // 
            this.lblScanEnd.Location = new System.Drawing.Point(120, 45);
            this.lblScanEnd.Name = "lblScanEnd";
            this.lblScanEnd.Size = new System.Drawing.Size(72, 16);
            this.lblScanEnd.TabIndex = 3;
            this.lblScanEnd.Text = "End Scan";
            // 
            // lblScanStart
            // 
            this.lblScanStart.Location = new System.Drawing.Point(120, 21);
            this.lblScanStart.Name = "lblScanStart";
            this.lblScanStart.Size = new System.Drawing.Size(64, 15);
            this.lblScanStart.TabIndex = 1;
            this.lblScanStart.Text = "Start Scan";
            // 
            // cmdClearAllRangeFilters
            // 
            this.cmdClearAllRangeFilters.Location = new System.Drawing.Point(16, 27);
            this.cmdClearAllRangeFilters.Name = "cmdClearAllRangeFilters";
            this.cmdClearAllRangeFilters.Size = new System.Drawing.Size(88, 24);
            this.cmdClearAllRangeFilters.TabIndex = 0;
            this.cmdClearAllRangeFilters.Text = "Clear Filters";
            this.cmdClearAllRangeFilters.Click += new System.EventHandler(this.cmdClearAllRangeFilters_Click);
            // 
            // lblSICOptionsOverview
            // 
            this.lblSICOptionsOverview.Location = new System.Drawing.Point(312, 24);
            this.lblSICOptionsOverview.Name = "lblSICOptionsOverview";
            this.lblSICOptionsOverview.Size = new System.Drawing.Size(355, 152);
            this.lblSICOptionsOverview.TabIndex = 2;
            this.lblSICOptionsOverview.Text = "SIC Options Overview";
            // 
            // fraSICSearchThresholds
            // 
            this.fraSICSearchThresholds.Controls.Add(this.optSICTolerancePPM);
            this.fraSICSearchThresholds.Controls.Add(this.optSICToleranceDa);
            this.fraSICSearchThresholds.Controls.Add(this.chkRefineReportedParentIonMZ);
            this.fraSICSearchThresholds.Controls.Add(this.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData);
            this.fraSICSearchThresholds.Controls.Add(this.txtMaxPeakWidthMinutesForward);
            this.fraSICSearchThresholds.Controls.Add(this.txtMaxPeakWidthMinutesBackward);
            this.fraSICSearchThresholds.Controls.Add(this.txtIntensityThresholdFractionMax);
            this.fraSICSearchThresholds.Controls.Add(this.lblIntensityThresholdFractionMax);
            this.fraSICSearchThresholds.Controls.Add(this.txtIntensityThresholdAbsoluteMinimum);
            this.fraSICSearchThresholds.Controls.Add(this.lblIntensityThresholdAbsoluteMinimum);
            this.fraSICSearchThresholds.Controls.Add(this.lblMaxPeakWidthMinutesForward);
            this.fraSICSearchThresholds.Controls.Add(this.lblMaxPeakWidthMinutesBackward);
            this.fraSICSearchThresholds.Controls.Add(this.lblMaxPeakWidthMinutes);
            this.fraSICSearchThresholds.Controls.Add(this.txtSICTolerance);
            this.fraSICSearchThresholds.Controls.Add(this.lblSICToleranceDa);
            this.fraSICSearchThresholds.Location = new System.Drawing.Point(16, 8);
            this.fraSICSearchThresholds.Name = "fraSICSearchThresholds";
            this.fraSICSearchThresholds.Size = new System.Drawing.Size(280, 188);
            this.fraSICSearchThresholds.TabIndex = 0;
            this.fraSICSearchThresholds.TabStop = false;
            this.fraSICSearchThresholds.Text = "SIC Search Thresholds";
            // 
            // optSICTolerancePPM
            // 
            this.optSICTolerancePPM.Location = new System.Drawing.Point(192, 28);
            this.optSICTolerancePPM.Name = "optSICTolerancePPM";
            this.optSICTolerancePPM.Size = new System.Drawing.Size(72, 18);
            this.optSICTolerancePPM.TabIndex = 14;
            this.optSICTolerancePPM.Text = "ppm";
            // 
            // optSICToleranceDa
            // 
            this.optSICToleranceDa.Checked = true;
            this.optSICToleranceDa.Location = new System.Drawing.Point(192, 10);
            this.optSICToleranceDa.Name = "optSICToleranceDa";
            this.optSICToleranceDa.Size = new System.Drawing.Size(72, 18);
            this.optSICToleranceDa.TabIndex = 13;
            this.optSICToleranceDa.TabStop = true;
            this.optSICToleranceDa.Text = "Da";
            // 
            // chkRefineReportedParentIonMZ
            // 
            this.chkRefineReportedParentIonMZ.Location = new System.Drawing.Point(8, 159);
            this.chkRefineReportedParentIonMZ.Name = "chkRefineReportedParentIonMZ";
            this.chkRefineReportedParentIonMZ.Size = new System.Drawing.Size(264, 17);
            this.chkRefineReportedParentIonMZ.TabIndex = 12;
            this.chkRefineReportedParentIonMZ.Text = "Refine reported parent ion m/z values";
            // 
            // chkReplaceSICZeroesWithMinimumPositiveValueFromMSData
            // 
            this.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Checked = true;
            this.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Location = new System.Drawing.Point(8, 136);
            this.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Name = "chkReplaceSICZeroesWithMinimumPositiveValueFromMSData";
            this.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Size = new System.Drawing.Size(264, 17);
            this.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.TabIndex = 11;
            this.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Text = "Replace SIC zeroes with min MS data values";
            // 
            // txtMaxPeakWidthMinutesForward
            // 
            this.txtMaxPeakWidthMinutesForward.Location = new System.Drawing.Point(208, 56);
            this.txtMaxPeakWidthMinutesForward.Name = "txtMaxPeakWidthMinutesForward";
            this.txtMaxPeakWidthMinutesForward.Size = new System.Drawing.Size(56, 20);
            this.txtMaxPeakWidthMinutesForward.TabIndex = 6;
            this.txtMaxPeakWidthMinutesForward.Text = "3";
            this.txtMaxPeakWidthMinutesForward.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMaxPeakWidthMinutesForward_KeyPress);
            // 
            // txtMaxPeakWidthMinutesBackward
            // 
            this.txtMaxPeakWidthMinutesBackward.Location = new System.Drawing.Point(80, 56);
            this.txtMaxPeakWidthMinutesBackward.Name = "txtMaxPeakWidthMinutesBackward";
            this.txtMaxPeakWidthMinutesBackward.Size = new System.Drawing.Size(56, 20);
            this.txtMaxPeakWidthMinutesBackward.TabIndex = 4;
            this.txtMaxPeakWidthMinutesBackward.Text = "3";
            this.txtMaxPeakWidthMinutesBackward.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMaxPeakWidthMinutesBackward_KeyPress);
            // 
            // txtIntensityThresholdFractionMax
            // 
            this.txtIntensityThresholdFractionMax.Location = new System.Drawing.Point(208, 80);
            this.txtIntensityThresholdFractionMax.Name = "txtIntensityThresholdFractionMax";
            this.txtIntensityThresholdFractionMax.Size = new System.Drawing.Size(56, 20);
            this.txtIntensityThresholdFractionMax.TabIndex = 8;
            this.txtIntensityThresholdFractionMax.Text = "0.01";
            this.txtIntensityThresholdFractionMax.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtIntensityThresholdFractionMax_KeyPress);
            // 
            // lblIntensityThresholdFractionMax
            // 
            this.lblIntensityThresholdFractionMax.Location = new System.Drawing.Point(8, 80);
            this.lblIntensityThresholdFractionMax.Name = "lblIntensityThresholdFractionMax";
            this.lblIntensityThresholdFractionMax.Size = new System.Drawing.Size(200, 16);
            this.lblIntensityThresholdFractionMax.TabIndex = 7;
            this.lblIntensityThresholdFractionMax.Text = "Intensity Threshold Fraction Max Peak";
            // 
            // txtIntensityThresholdAbsoluteMinimum
            // 
            this.txtIntensityThresholdAbsoluteMinimum.Location = new System.Drawing.Point(208, 104);
            this.txtIntensityThresholdAbsoluteMinimum.Name = "txtIntensityThresholdAbsoluteMinimum";
            this.txtIntensityThresholdAbsoluteMinimum.Size = new System.Drawing.Size(56, 20);
            this.txtIntensityThresholdAbsoluteMinimum.TabIndex = 10;
            this.txtIntensityThresholdAbsoluteMinimum.Text = "0";
            this.txtIntensityThresholdAbsoluteMinimum.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtIntensityThresholdAbsoluteMinimum_KeyPress);
            // 
            // lblIntensityThresholdAbsoluteMinimum
            // 
            this.lblIntensityThresholdAbsoluteMinimum.Location = new System.Drawing.Point(8, 104);
            this.lblIntensityThresholdAbsoluteMinimum.Name = "lblIntensityThresholdAbsoluteMinimum";
            this.lblIntensityThresholdAbsoluteMinimum.Size = new System.Drawing.Size(200, 16);
            this.lblIntensityThresholdAbsoluteMinimum.TabIndex = 9;
            this.lblIntensityThresholdAbsoluteMinimum.Text = "Intensity Threshold Absolute Minimum";
            // 
            // lblMaxPeakWidthMinutesForward
            // 
            this.lblMaxPeakWidthMinutesForward.Location = new System.Drawing.Point(152, 56);
            this.lblMaxPeakWidthMinutesForward.Name = "lblMaxPeakWidthMinutesForward";
            this.lblMaxPeakWidthMinutesForward.Size = new System.Drawing.Size(64, 16);
            this.lblMaxPeakWidthMinutesForward.TabIndex = 5;
            this.lblMaxPeakWidthMinutesForward.Text = "Forward";
            // 
            // lblMaxPeakWidthMinutesBackward
            // 
            this.lblMaxPeakWidthMinutesBackward.Location = new System.Drawing.Point(16, 56);
            this.lblMaxPeakWidthMinutesBackward.Name = "lblMaxPeakWidthMinutesBackward";
            this.lblMaxPeakWidthMinutesBackward.Size = new System.Drawing.Size(64, 16);
            this.lblMaxPeakWidthMinutesBackward.TabIndex = 3;
            this.lblMaxPeakWidthMinutesBackward.Text = "Backward";
            // 
            // lblMaxPeakWidthMinutes
            // 
            this.lblMaxPeakWidthMinutes.Location = new System.Drawing.Point(8, 40);
            this.lblMaxPeakWidthMinutes.Name = "lblMaxPeakWidthMinutes";
            this.lblMaxPeakWidthMinutes.Size = new System.Drawing.Size(168, 16);
            this.lblMaxPeakWidthMinutes.TabIndex = 2;
            this.lblMaxPeakWidthMinutes.Text = "Maximum Peak Width (minutes)";
            // 
            // txtSICTolerance
            // 
            this.txtSICTolerance.Location = new System.Drawing.Point(128, 16);
            this.txtSICTolerance.Name = "txtSICTolerance";
            this.txtSICTolerance.Size = new System.Drawing.Size(48, 20);
            this.txtSICTolerance.TabIndex = 1;
            this.txtSICTolerance.Text = "0.60";
            this.txtSICTolerance.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSICTolerance_KeyPress);
            // 
            // lblSICToleranceDa
            // 
            this.lblSICToleranceDa.Location = new System.Drawing.Point(8, 16);
            this.lblSICToleranceDa.Name = "lblSICToleranceDa";
            this.lblSICToleranceDa.Size = new System.Drawing.Size(120, 16);
            this.lblSICToleranceDa.TabIndex = 0;
            this.lblSICToleranceDa.Text = "SIC Tolerance (Da)";
            // 
            // TabPagePeakFindingOptions
            // 
            this.TabPagePeakFindingOptions.Controls.Add(this.fraSICNoiseThresholds);
            this.TabPagePeakFindingOptions.Controls.Add(this.fraSmoothingOptions);
            this.TabPagePeakFindingOptions.Controls.Add(this.fraPeakFindingOptions);
            this.TabPagePeakFindingOptions.Location = new System.Drawing.Point(4, 22);
            this.TabPagePeakFindingOptions.Name = "TabPagePeakFindingOptions";
            this.TabPagePeakFindingOptions.Size = new System.Drawing.Size(885, 282);
            this.TabPagePeakFindingOptions.TabIndex = 7;
            this.TabPagePeakFindingOptions.Text = "Peak Finding Options";
            this.TabPagePeakFindingOptions.UseVisualStyleBackColor = true;
            // 
            // fraSICNoiseThresholds
            // 
            this.fraSICNoiseThresholds.Controls.Add(this.cboSICNoiseThresholdMode);
            this.fraSICNoiseThresholds.Controls.Add(this.lblNoiseThresholdMode);
            this.fraSICNoiseThresholds.Controls.Add(this.txtSICNoiseFractionLowIntensityDataToAverage);
            this.fraSICNoiseThresholds.Controls.Add(this.lblSICNoiseFractionLowIntensityDataToAverage);
            this.fraSICNoiseThresholds.Controls.Add(this.txtSICNoiseThresholdIntensity);
            this.fraSICNoiseThresholds.Controls.Add(this.lblSICNoiseThresholdIntensity);
            this.fraSICNoiseThresholds.Location = new System.Drawing.Point(16, 8);
            this.fraSICNoiseThresholds.Name = "fraSICNoiseThresholds";
            this.fraSICNoiseThresholds.Size = new System.Drawing.Size(320, 128);
            this.fraSICNoiseThresholds.TabIndex = 0;
            this.fraSICNoiseThresholds.TabStop = false;
            this.fraSICNoiseThresholds.Text = "Initial Noise Threshold Determination for SICs";
            // 
            // cboSICNoiseThresholdMode
            // 
            this.cboSICNoiseThresholdMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSICNoiseThresholdMode.Location = new System.Drawing.Point(120, 24);
            this.cboSICNoiseThresholdMode.Name = "cboSICNoiseThresholdMode";
            this.cboSICNoiseThresholdMode.Size = new System.Drawing.Size(176, 21);
            this.cboSICNoiseThresholdMode.TabIndex = 1;
            this.cboSICNoiseThresholdMode.SelectedIndexChanged += new System.EventHandler(this.cboSICNoiseThresholdMode_SelectedIndexChanged);
            // 
            // lblNoiseThresholdMode
            // 
            this.lblNoiseThresholdMode.Location = new System.Drawing.Point(16, 26);
            this.lblNoiseThresholdMode.Name = "lblNoiseThresholdMode";
            this.lblNoiseThresholdMode.Size = new System.Drawing.Size(96, 16);
            this.lblNoiseThresholdMode.TabIndex = 0;
            this.lblNoiseThresholdMode.Text = "Threshold Mode:";
            // 
            // txtSICNoiseFractionLowIntensityDataToAverage
            // 
            this.txtSICNoiseFractionLowIntensityDataToAverage.Location = new System.Drawing.Point(248, 80);
            this.txtSICNoiseFractionLowIntensityDataToAverage.Name = "txtSICNoiseFractionLowIntensityDataToAverage";
            this.txtSICNoiseFractionLowIntensityDataToAverage.Size = new System.Drawing.Size(56, 20);
            this.txtSICNoiseFractionLowIntensityDataToAverage.TabIndex = 5;
            this.txtSICNoiseFractionLowIntensityDataToAverage.Text = "0.75";
            this.txtSICNoiseFractionLowIntensityDataToAverage.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSICNoiseFractionLowIntensityDataToAverage_KeyPress);
            // 
            // lblSICNoiseFractionLowIntensityDataToAverage
            // 
            this.lblSICNoiseFractionLowIntensityDataToAverage.Location = new System.Drawing.Point(16, 82);
            this.lblSICNoiseFractionLowIntensityDataToAverage.Name = "lblSICNoiseFractionLowIntensityDataToAverage";
            this.lblSICNoiseFractionLowIntensityDataToAverage.Size = new System.Drawing.Size(192, 14);
            this.lblSICNoiseFractionLowIntensityDataToAverage.TabIndex = 4;
            this.lblSICNoiseFractionLowIntensityDataToAverage.Text = "Fraction low intensity data to average";
            // 
            // txtSICNoiseThresholdIntensity
            // 
            this.txtSICNoiseThresholdIntensity.Location = new System.Drawing.Point(248, 56);
            this.txtSICNoiseThresholdIntensity.Name = "txtSICNoiseThresholdIntensity";
            this.txtSICNoiseThresholdIntensity.Size = new System.Drawing.Size(56, 20);
            this.txtSICNoiseThresholdIntensity.TabIndex = 3;
            this.txtSICNoiseThresholdIntensity.Text = "0";
            this.txtSICNoiseThresholdIntensity.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSICNoiseThresholdIntensity_KeyPress);
            // 
            // lblSICNoiseThresholdIntensity
            // 
            this.lblSICNoiseThresholdIntensity.Location = new System.Drawing.Point(16, 58);
            this.lblSICNoiseThresholdIntensity.Name = "lblSICNoiseThresholdIntensity";
            this.lblSICNoiseThresholdIntensity.Size = new System.Drawing.Size(200, 16);
            this.lblSICNoiseThresholdIntensity.TabIndex = 2;
            this.lblSICNoiseThresholdIntensity.Text = "Absolute Noise Thereshold Intensity";
            // 
            // fraSmoothingOptions
            // 
            this.fraSmoothingOptions.Controls.Add(this.chkSmoothDataRegardlessOfMinimumPeakWidth);
            this.fraSmoothingOptions.Controls.Add(this.chkFindPeaksOnSmoothedData);
            this.fraSmoothingOptions.Controls.Add(this.optUseSavitzkyGolaySmooth);
            this.fraSmoothingOptions.Controls.Add(this.txtButterworthSamplingFrequency);
            this.fraSmoothingOptions.Controls.Add(this.lblButterworthSamplingFrequency);
            this.fraSmoothingOptions.Controls.Add(this.txtSavitzkyGolayFilterOrder);
            this.fraSmoothingOptions.Controls.Add(this.lblSavitzkyGolayFilterOrder);
            this.fraSmoothingOptions.Controls.Add(this.optUseButterworthSmooth);
            this.fraSmoothingOptions.Location = new System.Drawing.Point(352, 8);
            this.fraSmoothingOptions.Name = "fraSmoothingOptions";
            this.fraSmoothingOptions.Size = new System.Drawing.Size(248, 224);
            this.fraSmoothingOptions.TabIndex = 2;
            this.fraSmoothingOptions.TabStop = false;
            this.fraSmoothingOptions.Text = "Smoothing Options";
            // 
            // chkSmoothDataRegardlessOfMinimumPeakWidth
            // 
            this.chkSmoothDataRegardlessOfMinimumPeakWidth.Checked = true;
            this.chkSmoothDataRegardlessOfMinimumPeakWidth.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSmoothDataRegardlessOfMinimumPeakWidth.Location = new System.Drawing.Point(24, 176);
            this.chkSmoothDataRegardlessOfMinimumPeakWidth.Name = "chkSmoothDataRegardlessOfMinimumPeakWidth";
            this.chkSmoothDataRegardlessOfMinimumPeakWidth.Size = new System.Drawing.Size(160, 40);
            this.chkSmoothDataRegardlessOfMinimumPeakWidth.TabIndex = 7;
            this.chkSmoothDataRegardlessOfMinimumPeakWidth.Text = "Smooth Data Regardless Of Minimum Peak Width";
            // 
            // chkFindPeaksOnSmoothedData
            // 
            this.chkFindPeaksOnSmoothedData.Checked = true;
            this.chkFindPeaksOnSmoothedData.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFindPeaksOnSmoothedData.Location = new System.Drawing.Point(24, 152);
            this.chkFindPeaksOnSmoothedData.Name = "chkFindPeaksOnSmoothedData";
            this.chkFindPeaksOnSmoothedData.Size = new System.Drawing.Size(208, 16);
            this.chkFindPeaksOnSmoothedData.TabIndex = 6;
            this.chkFindPeaksOnSmoothedData.Text = "Find Peaks On Smoothed Data";
            // 
            // optUseSavitzkyGolaySmooth
            // 
            this.optUseSavitzkyGolaySmooth.Location = new System.Drawing.Point(16, 80);
            this.optUseSavitzkyGolaySmooth.Name = "optUseSavitzkyGolaySmooth";
            this.optUseSavitzkyGolaySmooth.Size = new System.Drawing.Size(200, 16);
            this.optUseSavitzkyGolaySmooth.TabIndex = 3;
            this.optUseSavitzkyGolaySmooth.Text = "Use Savitzky Golay Smooth";
            this.optUseSavitzkyGolaySmooth.CheckedChanged += new System.EventHandler(this.optUseSavitzkyGolaySmooth_CheckedChanged);
            // 
            // txtButterworthSamplingFrequency
            // 
            this.txtButterworthSamplingFrequency.Location = new System.Drawing.Point(112, 48);
            this.txtButterworthSamplingFrequency.Name = "txtButterworthSamplingFrequency";
            this.txtButterworthSamplingFrequency.Size = new System.Drawing.Size(48, 20);
            this.txtButterworthSamplingFrequency.TabIndex = 2;
            this.txtButterworthSamplingFrequency.Text = "0.25";
            this.txtButterworthSamplingFrequency.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtButterworthSamplingFrequency_KeyPress);
            this.txtButterworthSamplingFrequency.Validating += new System.ComponentModel.CancelEventHandler(this.txtButterworthSamplingFrequency_Validating);
            // 
            // lblButterworthSamplingFrequency
            // 
            this.lblButterworthSamplingFrequency.Location = new System.Drawing.Point(32, 48);
            this.lblButterworthSamplingFrequency.Name = "lblButterworthSamplingFrequency";
            this.lblButterworthSamplingFrequency.Size = new System.Drawing.Size(72, 16);
            this.lblButterworthSamplingFrequency.TabIndex = 1;
            this.lblButterworthSamplingFrequency.Text = "Filter Order";
            // 
            // txtSavitzkyGolayFilterOrder
            // 
            this.txtSavitzkyGolayFilterOrder.Location = new System.Drawing.Point(112, 104);
            this.txtSavitzkyGolayFilterOrder.Name = "txtSavitzkyGolayFilterOrder";
            this.txtSavitzkyGolayFilterOrder.Size = new System.Drawing.Size(48, 20);
            this.txtSavitzkyGolayFilterOrder.TabIndex = 5;
            this.txtSavitzkyGolayFilterOrder.Text = "0";
            this.txtSavitzkyGolayFilterOrder.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSavitzkyGolayFilterOrder_KeyPress);
            this.txtSavitzkyGolayFilterOrder.Validating += new System.ComponentModel.CancelEventHandler(this.txtSavitzkyGolayFilterOrder_Validating);
            // 
            // lblSavitzkyGolayFilterOrder
            // 
            this.lblSavitzkyGolayFilterOrder.Location = new System.Drawing.Point(32, 104);
            this.lblSavitzkyGolayFilterOrder.Name = "lblSavitzkyGolayFilterOrder";
            this.lblSavitzkyGolayFilterOrder.Size = new System.Drawing.Size(72, 16);
            this.lblSavitzkyGolayFilterOrder.TabIndex = 4;
            this.lblSavitzkyGolayFilterOrder.Text = "Filter Order";
            // 
            // optUseButterworthSmooth
            // 
            this.optUseButterworthSmooth.Checked = true;
            this.optUseButterworthSmooth.Location = new System.Drawing.Point(16, 24);
            this.optUseButterworthSmooth.Name = "optUseButterworthSmooth";
            this.optUseButterworthSmooth.Size = new System.Drawing.Size(200, 16);
            this.optUseButterworthSmooth.TabIndex = 0;
            this.optUseButterworthSmooth.TabStop = true;
            this.optUseButterworthSmooth.Text = "Use Butterworth Smooth";
            this.optUseButterworthSmooth.CheckedChanged += new System.EventHandler(this.optUseButterworthSmooth_CheckedChanged);
            // 
            // fraPeakFindingOptions
            // 
            this.fraPeakFindingOptions.Controls.Add(this.txtInitialPeakWidthScansMaximum);
            this.fraPeakFindingOptions.Controls.Add(this.lblInitialPeakWidthScansMaximum);
            this.fraPeakFindingOptions.Controls.Add(this.txtInitialPeakWidthScansScaler);
            this.fraPeakFindingOptions.Controls.Add(this.lblInitialPeakWidthScansScaler);
            this.fraPeakFindingOptions.Controls.Add(this.txtMaxAllowedUpwardSpikeFractionMax);
            this.fraPeakFindingOptions.Controls.Add(this.lblMaxAllowedUpwardSpikeFractionMax);
            this.fraPeakFindingOptions.Controls.Add(this.txtMaxDistanceScansNoOverlap);
            this.fraPeakFindingOptions.Controls.Add(this.lblMaxDistanceScansNoOverlap);
            this.fraPeakFindingOptions.Location = new System.Drawing.Point(16, 144);
            this.fraPeakFindingOptions.Name = "fraPeakFindingOptions";
            this.fraPeakFindingOptions.Size = new System.Drawing.Size(320, 128);
            this.fraPeakFindingOptions.TabIndex = 1;
            this.fraPeakFindingOptions.TabStop = false;
            this.fraPeakFindingOptions.Text = "Fine Tuning Peak Finding";
            // 
            // txtInitialPeakWidthScansMaximum
            // 
            this.txtInitialPeakWidthScansMaximum.Location = new System.Drawing.Point(240, 96);
            this.txtInitialPeakWidthScansMaximum.Name = "txtInitialPeakWidthScansMaximum";
            this.txtInitialPeakWidthScansMaximum.Size = new System.Drawing.Size(56, 20);
            this.txtInitialPeakWidthScansMaximum.TabIndex = 7;
            this.txtInitialPeakWidthScansMaximum.Text = "30";
            this.txtInitialPeakWidthScansMaximum.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtInitialPeakWidthScansMaximum_KeyPress);
            // 
            // lblInitialPeakWidthScansMaximum
            // 
            this.lblInitialPeakWidthScansMaximum.Location = new System.Drawing.Point(16, 98);
            this.lblInitialPeakWidthScansMaximum.Name = "lblInitialPeakWidthScansMaximum";
            this.lblInitialPeakWidthScansMaximum.Size = new System.Drawing.Size(200, 16);
            this.lblInitialPeakWidthScansMaximum.TabIndex = 6;
            this.lblInitialPeakWidthScansMaximum.Text = "Initial Peak Width Maximum (Scans)";
            // 
            // txtInitialPeakWidthScansScaler
            // 
            this.txtInitialPeakWidthScansScaler.Location = new System.Drawing.Point(240, 72);
            this.txtInitialPeakWidthScansScaler.Name = "txtInitialPeakWidthScansScaler";
            this.txtInitialPeakWidthScansScaler.Size = new System.Drawing.Size(56, 20);
            this.txtInitialPeakWidthScansScaler.TabIndex = 5;
            this.txtInitialPeakWidthScansScaler.Text = "1";
            this.txtInitialPeakWidthScansScaler.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtInitialPeakWidthScansScaler_KeyPress);
            // 
            // lblInitialPeakWidthScansScaler
            // 
            this.lblInitialPeakWidthScansScaler.Location = new System.Drawing.Point(16, 74);
            this.lblInitialPeakWidthScansScaler.Name = "lblInitialPeakWidthScansScaler";
            this.lblInitialPeakWidthScansScaler.Size = new System.Drawing.Size(200, 16);
            this.lblInitialPeakWidthScansScaler.TabIndex = 4;
            this.lblInitialPeakWidthScansScaler.Text = "Initial Peak Width Scaler (Scans)";
            // 
            // txtMaxAllowedUpwardSpikeFractionMax
            // 
            this.txtMaxAllowedUpwardSpikeFractionMax.Location = new System.Drawing.Point(240, 48);
            this.txtMaxAllowedUpwardSpikeFractionMax.Name = "txtMaxAllowedUpwardSpikeFractionMax";
            this.txtMaxAllowedUpwardSpikeFractionMax.Size = new System.Drawing.Size(56, 20);
            this.txtMaxAllowedUpwardSpikeFractionMax.TabIndex = 3;
            this.txtMaxAllowedUpwardSpikeFractionMax.Text = "0.2";
            this.txtMaxAllowedUpwardSpikeFractionMax.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMaxAllowedUpwardSpikeFractionMax_KeyPress);
            // 
            // lblMaxAllowedUpwardSpikeFractionMax
            // 
            this.lblMaxAllowedUpwardSpikeFractionMax.Location = new System.Drawing.Point(16, 50);
            this.lblMaxAllowedUpwardSpikeFractionMax.Name = "lblMaxAllowedUpwardSpikeFractionMax";
            this.lblMaxAllowedUpwardSpikeFractionMax.Size = new System.Drawing.Size(232, 16);
            this.lblMaxAllowedUpwardSpikeFractionMax.TabIndex = 2;
            this.lblMaxAllowedUpwardSpikeFractionMax.Text = "Max Allowed Upward Spike (Fraction Max)";
            // 
            // txtMaxDistanceScansNoOverlap
            // 
            this.txtMaxDistanceScansNoOverlap.Location = new System.Drawing.Point(240, 24);
            this.txtMaxDistanceScansNoOverlap.Name = "txtMaxDistanceScansNoOverlap";
            this.txtMaxDistanceScansNoOverlap.Size = new System.Drawing.Size(56, 20);
            this.txtMaxDistanceScansNoOverlap.TabIndex = 1;
            this.txtMaxDistanceScansNoOverlap.Text = "0";
            this.txtMaxDistanceScansNoOverlap.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMaxDistanceScansNoOverlap_KeyPress);
            // 
            // lblMaxDistanceScansNoOverlap
            // 
            this.lblMaxDistanceScansNoOverlap.Location = new System.Drawing.Point(16, 26);
            this.lblMaxDistanceScansNoOverlap.Name = "lblMaxDistanceScansNoOverlap";
            this.lblMaxDistanceScansNoOverlap.Size = new System.Drawing.Size(200, 16);
            this.lblMaxDistanceScansNoOverlap.TabIndex = 0;
            this.lblMaxDistanceScansNoOverlap.Text = "Max Distance No Overlap (Scans)";
            // 
            // TabPageBinningAndSimilarityOptions
            // 
            this.TabPageBinningAndSimilarityOptions.Controls.Add(this.fraMassSpectraNoiseThresholds);
            this.TabPageBinningAndSimilarityOptions.Controls.Add(this.fraBinningIntensityOptions);
            this.TabPageBinningAndSimilarityOptions.Controls.Add(this.fraSpectrumSimilarityOptions);
            this.TabPageBinningAndSimilarityOptions.Controls.Add(this.fraBinningMZOptions);
            this.TabPageBinningAndSimilarityOptions.Location = new System.Drawing.Point(4, 22);
            this.TabPageBinningAndSimilarityOptions.Name = "TabPageBinningAndSimilarityOptions";
            this.TabPageBinningAndSimilarityOptions.Size = new System.Drawing.Size(885, 282);
            this.TabPageBinningAndSimilarityOptions.TabIndex = 6;
            this.TabPageBinningAndSimilarityOptions.Text = "Binning and Similarity";
            this.TabPageBinningAndSimilarityOptions.UseVisualStyleBackColor = true;
            // 
            // fraMassSpectraNoiseThresholds
            // 
            this.fraMassSpectraNoiseThresholds.Controls.Add(this.txtMassSpectraNoiseMinimumSignalToNoiseRatio);
            this.fraMassSpectraNoiseThresholds.Controls.Add(this.lblMassSpectraNoiseMinimumSignalToNoiseRatio);
            this.fraMassSpectraNoiseThresholds.Controls.Add(this.txtMassSpectraNoiseThresholdIntensity);
            this.fraMassSpectraNoiseThresholds.Controls.Add(this.txtMassSpectraNoiseFractionLowIntensityDataToAverage);
            this.fraMassSpectraNoiseThresholds.Controls.Add(this.lblMassSpectraNoiseFractionLowIntensityDataToAverage);
            this.fraMassSpectraNoiseThresholds.Controls.Add(this.cboMassSpectraNoiseThresholdMode);
            this.fraMassSpectraNoiseThresholds.Controls.Add(this.lblMassSpectraNoiseThresholdMode);
            this.fraMassSpectraNoiseThresholds.Controls.Add(this.lblMassSpectraNoiseThresholdIntensity);
            this.fraMassSpectraNoiseThresholds.Location = new System.Drawing.Point(8, 16);
            this.fraMassSpectraNoiseThresholds.Name = "fraMassSpectraNoiseThresholds";
            this.fraMassSpectraNoiseThresholds.Size = new System.Drawing.Size(344, 128);
            this.fraMassSpectraNoiseThresholds.TabIndex = 0;
            this.fraMassSpectraNoiseThresholds.TabStop = false;
            this.fraMassSpectraNoiseThresholds.Text = "Noise Threshold Determination for Mass Spectra";
            // 
            // txtMassSpectraNoiseMinimumSignalToNoiseRatio
            // 
            this.txtMassSpectraNoiseMinimumSignalToNoiseRatio.Location = new System.Drawing.Point(208, 104);
            this.txtMassSpectraNoiseMinimumSignalToNoiseRatio.Name = "txtMassSpectraNoiseMinimumSignalToNoiseRatio";
            this.txtMassSpectraNoiseMinimumSignalToNoiseRatio.Size = new System.Drawing.Size(56, 20);
            this.txtMassSpectraNoiseMinimumSignalToNoiseRatio.TabIndex = 9;
            this.txtMassSpectraNoiseMinimumSignalToNoiseRatio.Text = "2";
            // 
            // lblMassSpectraNoiseMinimumSignalToNoiseRatio
            // 
            this.lblMassSpectraNoiseMinimumSignalToNoiseRatio.Location = new System.Drawing.Point(8, 104);
            this.lblMassSpectraNoiseMinimumSignalToNoiseRatio.Name = "lblMassSpectraNoiseMinimumSignalToNoiseRatio";
            this.lblMassSpectraNoiseMinimumSignalToNoiseRatio.Size = new System.Drawing.Size(192, 14);
            this.lblMassSpectraNoiseMinimumSignalToNoiseRatio.TabIndex = 8;
            this.lblMassSpectraNoiseMinimumSignalToNoiseRatio.Text = "Minimum Signal to Noise Ratio";
            // 
            // txtMassSpectraNoiseThresholdIntensity
            // 
            this.txtMassSpectraNoiseThresholdIntensity.Location = new System.Drawing.Point(208, 56);
            this.txtMassSpectraNoiseThresholdIntensity.Name = "txtMassSpectraNoiseThresholdIntensity";
            this.txtMassSpectraNoiseThresholdIntensity.Size = new System.Drawing.Size(56, 20);
            this.txtMassSpectraNoiseThresholdIntensity.TabIndex = 3;
            this.txtMassSpectraNoiseThresholdIntensity.Text = "0";
            this.txtMassSpectraNoiseThresholdIntensity.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMassSpectraNoiseThresholdIntensity_KeyPress);
            // 
            // txtMassSpectraNoiseFractionLowIntensityDataToAverage
            // 
            this.txtMassSpectraNoiseFractionLowIntensityDataToAverage.Location = new System.Drawing.Point(208, 80);
            this.txtMassSpectraNoiseFractionLowIntensityDataToAverage.Name = "txtMassSpectraNoiseFractionLowIntensityDataToAverage";
            this.txtMassSpectraNoiseFractionLowIntensityDataToAverage.Size = new System.Drawing.Size(56, 20);
            this.txtMassSpectraNoiseFractionLowIntensityDataToAverage.TabIndex = 5;
            this.txtMassSpectraNoiseFractionLowIntensityDataToAverage.Text = "0.5";
            this.txtMassSpectraNoiseFractionLowIntensityDataToAverage.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMassSpectraNoiseFractionLowIntensityDataToAverage_KeyPress);
            // 
            // lblMassSpectraNoiseFractionLowIntensityDataToAverage
            // 
            this.lblMassSpectraNoiseFractionLowIntensityDataToAverage.Location = new System.Drawing.Point(8, 80);
            this.lblMassSpectraNoiseFractionLowIntensityDataToAverage.Name = "lblMassSpectraNoiseFractionLowIntensityDataToAverage";
            this.lblMassSpectraNoiseFractionLowIntensityDataToAverage.Size = new System.Drawing.Size(192, 22);
            this.lblMassSpectraNoiseFractionLowIntensityDataToAverage.TabIndex = 4;
            this.lblMassSpectraNoiseFractionLowIntensityDataToAverage.Text = "Fraction low intensity data to average";
            // 
            // cboMassSpectraNoiseThresholdMode
            // 
            this.cboMassSpectraNoiseThresholdMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMassSpectraNoiseThresholdMode.Location = new System.Drawing.Point(136, 24);
            this.cboMassSpectraNoiseThresholdMode.Name = "cboMassSpectraNoiseThresholdMode";
            this.cboMassSpectraNoiseThresholdMode.Size = new System.Drawing.Size(176, 21);
            this.cboMassSpectraNoiseThresholdMode.TabIndex = 1;
            this.cboMassSpectraNoiseThresholdMode.SelectedIndexChanged += new System.EventHandler(this.cboMassSpectraNoiseThresholdMode_SelectedIndexChanged);
            // 
            // lblMassSpectraNoiseThresholdMode
            // 
            this.lblMassSpectraNoiseThresholdMode.Location = new System.Drawing.Point(8, 32);
            this.lblMassSpectraNoiseThresholdMode.Name = "lblMassSpectraNoiseThresholdMode";
            this.lblMassSpectraNoiseThresholdMode.Size = new System.Drawing.Size(136, 16);
            this.lblMassSpectraNoiseThresholdMode.TabIndex = 0;
            this.lblMassSpectraNoiseThresholdMode.Text = "Noise Threshold Mode:";
            // 
            // lblMassSpectraNoiseThresholdIntensity
            // 
            this.lblMassSpectraNoiseThresholdIntensity.Location = new System.Drawing.Point(8, 56);
            this.lblMassSpectraNoiseThresholdIntensity.Name = "lblMassSpectraNoiseThresholdIntensity";
            this.lblMassSpectraNoiseThresholdIntensity.Size = new System.Drawing.Size(184, 16);
            this.lblMassSpectraNoiseThresholdIntensity.TabIndex = 2;
            this.lblMassSpectraNoiseThresholdIntensity.Text = "Absolute Noise Threshold Intensity";
            // 
            // fraBinningIntensityOptions
            // 
            this.fraBinningIntensityOptions.Controls.Add(this.lblBinnedDataIntensityPrecisionPctUnits);
            this.fraBinningIntensityOptions.Controls.Add(this.chkBinnedDataSumAllIntensitiesForBin);
            this.fraBinningIntensityOptions.Controls.Add(this.chkBinnedDataNormalize);
            this.fraBinningIntensityOptions.Controls.Add(this.txtBinnedDataIntensityPrecisionPct);
            this.fraBinningIntensityOptions.Controls.Add(this.lblBinnedDataIntensityPrecisionPct);
            this.fraBinningIntensityOptions.Location = new System.Drawing.Point(368, 152);
            this.fraBinningIntensityOptions.Name = "fraBinningIntensityOptions";
            this.fraBinningIntensityOptions.Size = new System.Drawing.Size(240, 104);
            this.fraBinningIntensityOptions.TabIndex = 3;
            this.fraBinningIntensityOptions.TabStop = false;
            this.fraBinningIntensityOptions.Text = "Binning Intensity Options";
            // 
            // lblBinnedDataIntensityPrecisionPctUnits
            // 
            this.lblBinnedDataIntensityPrecisionPctUnits.Location = new System.Drawing.Point(216, 24);
            this.lblBinnedDataIntensityPrecisionPctUnits.Name = "lblBinnedDataIntensityPrecisionPctUnits";
            this.lblBinnedDataIntensityPrecisionPctUnits.Size = new System.Drawing.Size(16, 16);
            this.lblBinnedDataIntensityPrecisionPctUnits.TabIndex = 8;
            this.lblBinnedDataIntensityPrecisionPctUnits.Text = "%";
            // 
            // chkBinnedDataSumAllIntensitiesForBin
            // 
            this.chkBinnedDataSumAllIntensitiesForBin.Location = new System.Drawing.Point(8, 80);
            this.chkBinnedDataSumAllIntensitiesForBin.Name = "chkBinnedDataSumAllIntensitiesForBin";
            this.chkBinnedDataSumAllIntensitiesForBin.Size = new System.Drawing.Size(160, 16);
            this.chkBinnedDataSumAllIntensitiesForBin.TabIndex = 10;
            this.chkBinnedDataSumAllIntensitiesForBin.Text = "Sum All Intensities For Bin";
            // 
            // chkBinnedDataNormalize
            // 
            this.chkBinnedDataNormalize.Location = new System.Drawing.Point(8, 56);
            this.chkBinnedDataNormalize.Name = "chkBinnedDataNormalize";
            this.chkBinnedDataNormalize.Size = new System.Drawing.Size(136, 16);
            this.chkBinnedDataNormalize.TabIndex = 9;
            this.chkBinnedDataNormalize.Text = "Normalize Intensities";
            // 
            // txtBinnedDataIntensityPrecisionPct
            // 
            this.txtBinnedDataIntensityPrecisionPct.Location = new System.Drawing.Point(176, 24);
            this.txtBinnedDataIntensityPrecisionPct.Name = "txtBinnedDataIntensityPrecisionPct";
            this.txtBinnedDataIntensityPrecisionPct.Size = new System.Drawing.Size(40, 20);
            this.txtBinnedDataIntensityPrecisionPct.TabIndex = 7;
            this.txtBinnedDataIntensityPrecisionPct.Text = "1";
            this.txtBinnedDataIntensityPrecisionPct.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtBinnedDataIntensityPrecisionPct_KeyPress);
            // 
            // lblBinnedDataIntensityPrecisionPct
            // 
            this.lblBinnedDataIntensityPrecisionPct.Location = new System.Drawing.Point(8, 26);
            this.lblBinnedDataIntensityPrecisionPct.Name = "lblBinnedDataIntensityPrecisionPct";
            this.lblBinnedDataIntensityPrecisionPct.Size = new System.Drawing.Size(168, 16);
            this.lblBinnedDataIntensityPrecisionPct.TabIndex = 6;
            this.lblBinnedDataIntensityPrecisionPct.Text = "Intensity Precision (0.1 to 100)";
            // 
            // fraSpectrumSimilarityOptions
            // 
            this.fraSpectrumSimilarityOptions.Controls.Add(this.lblSimilarIonTimeToleranceHalfWidthUnits);
            this.fraSpectrumSimilarityOptions.Controls.Add(this.txtSpectrumSimilarityMinimum);
            this.fraSpectrumSimilarityOptions.Controls.Add(this.lblSpectrumSimilarityMinimum);
            this.fraSpectrumSimilarityOptions.Controls.Add(this.txtSimilarIonToleranceHalfWidthMinutes);
            this.fraSpectrumSimilarityOptions.Controls.Add(this.lblSimilarIonTimeToleranceHalfWidth);
            this.fraSpectrumSimilarityOptions.Controls.Add(this.txtSimilarIonMZToleranceHalfWidth);
            this.fraSpectrumSimilarityOptions.Controls.Add(this.lblSimilarIonMZToleranceHalfWidth);
            this.fraSpectrumSimilarityOptions.Location = new System.Drawing.Point(8, 152);
            this.fraSpectrumSimilarityOptions.Name = "fraSpectrumSimilarityOptions";
            this.fraSpectrumSimilarityOptions.Size = new System.Drawing.Size(272, 104);
            this.fraSpectrumSimilarityOptions.TabIndex = 1;
            this.fraSpectrumSimilarityOptions.TabStop = false;
            this.fraSpectrumSimilarityOptions.Text = "Spectrum Similarity Options";
            // 
            // lblSimilarIonTimeToleranceHalfWidthUnits
            // 
            this.lblSimilarIonTimeToleranceHalfWidthUnits.Location = new System.Drawing.Point(216, 50);
            this.lblSimilarIonTimeToleranceHalfWidthUnits.Name = "lblSimilarIonTimeToleranceHalfWidthUnits";
            this.lblSimilarIonTimeToleranceHalfWidthUnits.Size = new System.Drawing.Size(48, 16);
            this.lblSimilarIonTimeToleranceHalfWidthUnits.TabIndex = 6;
            this.lblSimilarIonTimeToleranceHalfWidthUnits.Text = "minutes";
            // 
            // txtSpectrumSimilarityMinimum
            // 
            this.txtSpectrumSimilarityMinimum.Location = new System.Drawing.Point(168, 72);
            this.txtSpectrumSimilarityMinimum.Name = "txtSpectrumSimilarityMinimum";
            this.txtSpectrumSimilarityMinimum.Size = new System.Drawing.Size(40, 20);
            this.txtSpectrumSimilarityMinimum.TabIndex = 5;
            this.txtSpectrumSimilarityMinimum.Text = "0.7";
            this.txtSpectrumSimilarityMinimum.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSpectrumSimilarityMinimum_KeyPress);
            // 
            // lblSpectrumSimilarityMinimum
            // 
            this.lblSpectrumSimilarityMinimum.Location = new System.Drawing.Point(8, 74);
            this.lblSpectrumSimilarityMinimum.Name = "lblSpectrumSimilarityMinimum";
            this.lblSpectrumSimilarityMinimum.Size = new System.Drawing.Size(150, 16);
            this.lblSpectrumSimilarityMinimum.TabIndex = 4;
            this.lblSpectrumSimilarityMinimum.Text = "Minimum Similarity (0 to 1)";
            // 
            // txtSimilarIonToleranceHalfWidthMinutes
            // 
            this.txtSimilarIonToleranceHalfWidthMinutes.Location = new System.Drawing.Point(168, 48);
            this.txtSimilarIonToleranceHalfWidthMinutes.Name = "txtSimilarIonToleranceHalfWidthMinutes";
            this.txtSimilarIonToleranceHalfWidthMinutes.Size = new System.Drawing.Size(40, 20);
            this.txtSimilarIonToleranceHalfWidthMinutes.TabIndex = 3;
            this.txtSimilarIonToleranceHalfWidthMinutes.Text = "5";
            this.txtSimilarIonToleranceHalfWidthMinutes.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSimilarIonToleranceHalfWidthMinutes_KeyPress);
            // 
            // lblSimilarIonTimeToleranceHalfWidth
            // 
            this.lblSimilarIonTimeToleranceHalfWidth.Location = new System.Drawing.Point(8, 50);
            this.lblSimilarIonTimeToleranceHalfWidth.Name = "lblSimilarIonTimeToleranceHalfWidth";
            this.lblSimilarIonTimeToleranceHalfWidth.Size = new System.Drawing.Size(150, 16);
            this.lblSimilarIonTimeToleranceHalfWidth.TabIndex = 2;
            this.lblSimilarIonTimeToleranceHalfWidth.Text = "Time Tolerance Half Width";
            // 
            // txtSimilarIonMZToleranceHalfWidth
            // 
            this.txtSimilarIonMZToleranceHalfWidth.Location = new System.Drawing.Point(168, 24);
            this.txtSimilarIonMZToleranceHalfWidth.Name = "txtSimilarIonMZToleranceHalfWidth";
            this.txtSimilarIonMZToleranceHalfWidth.Size = new System.Drawing.Size(40, 20);
            this.txtSimilarIonMZToleranceHalfWidth.TabIndex = 1;
            this.txtSimilarIonMZToleranceHalfWidth.Text = "0.1";
            this.txtSimilarIonMZToleranceHalfWidth.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSimilarIonMZToleranceHalfWidth_KeyPress);
            // 
            // lblSimilarIonMZToleranceHalfWidth
            // 
            this.lblSimilarIonMZToleranceHalfWidth.Location = new System.Drawing.Point(8, 26);
            this.lblSimilarIonMZToleranceHalfWidth.Name = "lblSimilarIonMZToleranceHalfWidth";
            this.lblSimilarIonMZToleranceHalfWidth.Size = new System.Drawing.Size(150, 16);
            this.lblSimilarIonMZToleranceHalfWidth.TabIndex = 0;
            this.lblSimilarIonMZToleranceHalfWidth.Text = "m/z Tolerance Half Width";
            // 
            // fraBinningMZOptions
            // 
            this.fraBinningMZOptions.Controls.Add(this.txtMaximumBinCount);
            this.fraBinningMZOptions.Controls.Add(this.lblMaximumBinCount);
            this.fraBinningMZOptions.Controls.Add(this.txtBinSize);
            this.fraBinningMZOptions.Controls.Add(this.lblBinSize);
            this.fraBinningMZOptions.Controls.Add(this.txtBinEndX);
            this.fraBinningMZOptions.Controls.Add(this.lblBinEndX);
            this.fraBinningMZOptions.Controls.Add(this.txtBinStartX);
            this.fraBinningMZOptions.Controls.Add(this.lblBinStartX);
            this.fraBinningMZOptions.Location = new System.Drawing.Point(368, 16);
            this.fraBinningMZOptions.Name = "fraBinningMZOptions";
            this.fraBinningMZOptions.Size = new System.Drawing.Size(240, 128);
            this.fraBinningMZOptions.TabIndex = 2;
            this.fraBinningMZOptions.TabStop = false;
            this.fraBinningMZOptions.Text = "Binning m/z Options";
            // 
            // txtMaximumBinCount
            // 
            this.txtMaximumBinCount.Location = new System.Drawing.Point(152, 96);
            this.txtMaximumBinCount.Name = "txtMaximumBinCount";
            this.txtMaximumBinCount.Size = new System.Drawing.Size(56, 20);
            this.txtMaximumBinCount.TabIndex = 7;
            this.txtMaximumBinCount.Text = "100000";
            this.txtMaximumBinCount.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMaximumBinCount_KeyPress);
            // 
            // lblMaximumBinCount
            // 
            this.lblMaximumBinCount.Location = new System.Drawing.Point(16, 98);
            this.lblMaximumBinCount.Name = "lblMaximumBinCount";
            this.lblMaximumBinCount.Size = new System.Drawing.Size(128, 16);
            this.lblMaximumBinCount.TabIndex = 6;
            this.lblMaximumBinCount.Text = "Maximum Bin Count";
            // 
            // txtBinSize
            // 
            this.txtBinSize.Location = new System.Drawing.Point(152, 72);
            this.txtBinSize.Name = "txtBinSize";
            this.txtBinSize.Size = new System.Drawing.Size(56, 20);
            this.txtBinSize.TabIndex = 5;
            this.txtBinSize.Text = "1";
            this.txtBinSize.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtBinSize_KeyPress);
            // 
            // lblBinSize
            // 
            this.lblBinSize.Location = new System.Drawing.Point(16, 74);
            this.lblBinSize.Name = "lblBinSize";
            this.lblBinSize.Size = new System.Drawing.Size(128, 16);
            this.lblBinSize.TabIndex = 4;
            this.lblBinSize.Text = "Bin Size (m/z units)";
            // 
            // txtBinEndX
            // 
            this.txtBinEndX.Location = new System.Drawing.Point(152, 48);
            this.txtBinEndX.Name = "txtBinEndX";
            this.txtBinEndX.Size = new System.Drawing.Size(56, 20);
            this.txtBinEndX.TabIndex = 3;
            this.txtBinEndX.Text = "2000";
            this.txtBinEndX.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtBinEndX_KeyPress);
            // 
            // lblBinEndX
            // 
            this.lblBinEndX.Location = new System.Drawing.Point(16, 50);
            this.lblBinEndX.Name = "lblBinEndX";
            this.lblBinEndX.Size = new System.Drawing.Size(120, 16);
            this.lblBinEndX.TabIndex = 2;
            this.lblBinEndX.Text = "Bin End m/z";
            // 
            // txtBinStartX
            // 
            this.txtBinStartX.Location = new System.Drawing.Point(152, 24);
            this.txtBinStartX.Name = "txtBinStartX";
            this.txtBinStartX.Size = new System.Drawing.Size(56, 20);
            this.txtBinStartX.TabIndex = 1;
            this.txtBinStartX.Text = "50";
            this.txtBinStartX.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtBinStartX_KeyPress);
            // 
            // lblBinStartX
            // 
            this.lblBinStartX.Location = new System.Drawing.Point(16, 26);
            this.lblBinStartX.Name = "lblBinStartX";
            this.lblBinStartX.Size = new System.Drawing.Size(120, 16);
            this.lblBinStartX.TabIndex = 0;
            this.lblBinStartX.Text = "Bin Start m/z";
            // 
            // TabPageCustomSICOptions
            // 
            this.TabPageCustomSICOptions.Controls.Add(this.txtCustomSICFileDescription);
            this.TabPageCustomSICOptions.Controls.Add(this.cmdSelectCustomSICFile);
            this.TabPageCustomSICOptions.Controls.Add(this.txtCustomSICFileName);
            this.TabPageCustomSICOptions.Controls.Add(this.fraCustomSICControls);
            this.TabPageCustomSICOptions.Controls.Add(this.dgCustomSICValues);
            this.TabPageCustomSICOptions.Location = new System.Drawing.Point(4, 22);
            this.TabPageCustomSICOptions.Name = "TabPageCustomSICOptions";
            this.TabPageCustomSICOptions.Size = new System.Drawing.Size(885, 282);
            this.TabPageCustomSICOptions.TabIndex = 3;
            this.TabPageCustomSICOptions.Text = "Custom SIC Options";
            this.TabPageCustomSICOptions.UseVisualStyleBackColor = true;
            // 
            // txtCustomSICFileDescription
            // 
            this.txtCustomSICFileDescription.Location = new System.Drawing.Point(8, 5);
            this.txtCustomSICFileDescription.Multiline = true;
            this.txtCustomSICFileDescription.Name = "txtCustomSICFileDescription";
            this.txtCustomSICFileDescription.ReadOnly = true;
            this.txtCustomSICFileDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtCustomSICFileDescription.Size = new System.Drawing.Size(485, 51);
            this.txtCustomSICFileDescription.TabIndex = 0;
            this.txtCustomSICFileDescription.Text = "Custom SIC description ... populated via code.";
            this.txtCustomSICFileDescription.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtCustomSICFileDescription_KeyDown);
            // 
            // cmdSelectCustomSICFile
            // 
            this.cmdSelectCustomSICFile.Location = new System.Drawing.Point(8, 64);
            this.cmdSelectCustomSICFile.Name = "cmdSelectCustomSICFile";
            this.cmdSelectCustomSICFile.Size = new System.Drawing.Size(80, 24);
            this.cmdSelectCustomSICFile.TabIndex = 1;
            this.cmdSelectCustomSICFile.Text = "&Select File";
            this.cmdSelectCustomSICFile.Click += new System.EventHandler(this.cmdSelectCustomSICFile_Click);
            // 
            // txtCustomSICFileName
            // 
            this.txtCustomSICFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCustomSICFileName.Location = new System.Drawing.Point(104, 64);
            this.txtCustomSICFileName.Name = "txtCustomSICFileName";
            this.txtCustomSICFileName.Size = new System.Drawing.Size(596, 20);
            this.txtCustomSICFileName.TabIndex = 2;
            this.txtCustomSICFileName.TextChanged += new System.EventHandler(this.txtCustomSICFileName_TextChanged);
            // 
            // fraCustomSICControls
            // 
            this.fraCustomSICControls.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.fraCustomSICControls.Controls.Add(this.lblCustomSICToleranceType);
            this.fraCustomSICControls.Controls.Add(this.optCustomSICScanToleranceAcqTime);
            this.fraCustomSICControls.Controls.Add(this.optCustomSICScanToleranceRelative);
            this.fraCustomSICControls.Controls.Add(this.optCustomSICScanToleranceAbsolute);
            this.fraCustomSICControls.Controls.Add(this.chkLimitSearchToCustomMZs);
            this.fraCustomSICControls.Controls.Add(this.txtCustomSICScanOrAcqTimeTolerance);
            this.fraCustomSICControls.Controls.Add(this.lblCustomSICScanTolerance);
            this.fraCustomSICControls.Controls.Add(this.cmdPasteCustomSICList);
            this.fraCustomSICControls.Controls.Add(this.cmdCustomSICValuesPopulate);
            this.fraCustomSICControls.Controls.Add(this.cmdClearCustomSICList);
            this.fraCustomSICControls.Location = new System.Drawing.Point(706, 8);
            this.fraCustomSICControls.Name = "fraCustomSICControls";
            this.fraCustomSICControls.Size = new System.Drawing.Size(167, 264);
            this.fraCustomSICControls.TabIndex = 4;
            this.fraCustomSICControls.TabStop = false;
            // 
            // lblCustomSICToleranceType
            // 
            this.lblCustomSICToleranceType.Location = new System.Drawing.Point(5, 126);
            this.lblCustomSICToleranceType.Name = "lblCustomSICToleranceType";
            this.lblCustomSICToleranceType.Size = new System.Drawing.Size(88, 16);
            this.lblCustomSICToleranceType.TabIndex = 5;
            this.lblCustomSICToleranceType.Text = "Tolerance Type:";
            // 
            // optCustomSICScanToleranceAcqTime
            // 
            this.optCustomSICScanToleranceAcqTime.AutoSize = true;
            this.optCustomSICScanToleranceAcqTime.Location = new System.Drawing.Point(11, 186);
            this.optCustomSICScanToleranceAcqTime.Name = "optCustomSICScanToleranceAcqTime";
            this.optCustomSICScanToleranceAcqTime.Size = new System.Drawing.Size(143, 17);
            this.optCustomSICScanToleranceAcqTime.TabIndex = 8;
            this.optCustomSICScanToleranceAcqTime.Text = "Acquisition time (minutes)";
            this.optCustomSICScanToleranceAcqTime.UseVisualStyleBackColor = true;
            this.optCustomSICScanToleranceAcqTime.CheckedChanged += new System.EventHandler(this.optCustomSICScanToleranceAcqTime_CheckedChanged);
            // 
            // optCustomSICScanToleranceRelative
            // 
            this.optCustomSICScanToleranceRelative.AutoSize = true;
            this.optCustomSICScanToleranceRelative.Location = new System.Drawing.Point(11, 165);
            this.optCustomSICScanToleranceRelative.Name = "optCustomSICScanToleranceRelative";
            this.optCustomSICScanToleranceRelative.Size = new System.Drawing.Size(122, 17);
            this.optCustomSICScanToleranceRelative.TabIndex = 7;
            this.optCustomSICScanToleranceRelative.Text = "Relative time (0 to 1)";
            this.optCustomSICScanToleranceRelative.UseVisualStyleBackColor = true;
            this.optCustomSICScanToleranceRelative.CheckedChanged += new System.EventHandler(this.optCustomSICScanToleranceRelative_CheckedChanged);
            // 
            // optCustomSICScanToleranceAbsolute
            // 
            this.optCustomSICScanToleranceAbsolute.AutoSize = true;
            this.optCustomSICScanToleranceAbsolute.Checked = true;
            this.optCustomSICScanToleranceAbsolute.Location = new System.Drawing.Point(11, 144);
            this.optCustomSICScanToleranceAbsolute.Name = "optCustomSICScanToleranceAbsolute";
            this.optCustomSICScanToleranceAbsolute.Size = new System.Drawing.Size(130, 17);
            this.optCustomSICScanToleranceAbsolute.TabIndex = 6;
            this.optCustomSICScanToleranceAbsolute.TabStop = true;
            this.optCustomSICScanToleranceAbsolute.Text = "Absolute scan number";
            this.optCustomSICScanToleranceAbsolute.UseVisualStyleBackColor = true;
            this.optCustomSICScanToleranceAbsolute.CheckedChanged += new System.EventHandler(this.optCustomSICScanToleranceAbsolute_CheckedChanged);
            // 
            // chkLimitSearchToCustomMZs
            // 
            this.chkLimitSearchToCustomMZs.Location = new System.Drawing.Point(8, 216);
            this.chkLimitSearchToCustomMZs.Name = "chkLimitSearchToCustomMZs";
            this.chkLimitSearchToCustomMZs.Size = new System.Drawing.Size(152, 44);
            this.chkLimitSearchToCustomMZs.TabIndex = 9;
            this.chkLimitSearchToCustomMZs.Text = "Limit search to only use custom m/z values (skip auto-fragmented m/z\'s)";
            // 
            // txtCustomSICScanOrAcqTimeTolerance
            // 
            this.txtCustomSICScanOrAcqTimeTolerance.Location = new System.Drawing.Point(99, 99);
            this.txtCustomSICScanOrAcqTimeTolerance.Name = "txtCustomSICScanOrAcqTimeTolerance";
            this.txtCustomSICScanOrAcqTimeTolerance.Size = new System.Drawing.Size(56, 20);
            this.txtCustomSICScanOrAcqTimeTolerance.TabIndex = 4;
            this.txtCustomSICScanOrAcqTimeTolerance.Text = "3";
            this.txtCustomSICScanOrAcqTimeTolerance.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtCustomSICScanOrAcqTimeTolerance_KeyPress);
            // 
            // lblCustomSICScanTolerance
            // 
            this.lblCustomSICScanTolerance.Location = new System.Drawing.Point(5, 102);
            this.lblCustomSICScanTolerance.Name = "lblCustomSICScanTolerance";
            this.lblCustomSICScanTolerance.Size = new System.Drawing.Size(88, 16);
            this.lblCustomSICScanTolerance.TabIndex = 3;
            this.lblCustomSICScanTolerance.Text = "Scan Tolerance";
            // 
            // cmdPasteCustomSICList
            // 
            this.cmdPasteCustomSICList.Location = new System.Drawing.Point(8, 16);
            this.cmdPasteCustomSICList.Name = "cmdPasteCustomSICList";
            this.cmdPasteCustomSICList.Size = new System.Drawing.Size(67, 40);
            this.cmdPasteCustomSICList.TabIndex = 0;
            this.cmdPasteCustomSICList.Text = "Paste Values";
            this.cmdPasteCustomSICList.Click += new System.EventHandler(this.cmdPasteCustomSICList_Click);
            // 
            // cmdCustomSICValuesPopulate
            // 
            this.cmdCustomSICValuesPopulate.Location = new System.Drawing.Point(6, 62);
            this.cmdCustomSICValuesPopulate.Name = "cmdCustomSICValuesPopulate";
            this.cmdCustomSICValuesPopulate.Size = new System.Drawing.Size(152, 24);
            this.cmdCustomSICValuesPopulate.TabIndex = 2;
            this.cmdCustomSICValuesPopulate.Text = "Auto-Populate with Defaults";
            this.cmdCustomSICValuesPopulate.Click += new System.EventHandler(this.cmdCustomSICValuesPopulate_Click);
            // 
            // cmdClearCustomSICList
            // 
            this.cmdClearCustomSICList.Location = new System.Drawing.Point(89, 16);
            this.cmdClearCustomSICList.Name = "cmdClearCustomSICList";
            this.cmdClearCustomSICList.Size = new System.Drawing.Size(64, 40);
            this.cmdClearCustomSICList.TabIndex = 1;
            this.cmdClearCustomSICList.Text = "Clear List";
            this.cmdClearCustomSICList.Click += new System.EventHandler(this.cmdClearCustomSICList_Click);
            // 
            // dgCustomSICValues
            // 
            this.dgCustomSICValues.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgCustomSICValues.CaptionText = "Custom SIC Values";
            this.dgCustomSICValues.DataMember = "";
            this.dgCustomSICValues.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dgCustomSICValues.Location = new System.Drawing.Point(8, 104);
            this.dgCustomSICValues.Name = "dgCustomSICValues";
            this.dgCustomSICValues.Size = new System.Drawing.Size(692, 168);
            this.dgCustomSICValues.TabIndex = 3;
            // 
            // TabPageReporterIons
            // 
            this.TabPageReporterIons.Controls.Add(this.fraDecoyOptions);
            this.TabPageReporterIons.Controls.Add(this.fraMRMOptions);
            this.TabPageReporterIons.Controls.Add(this.fraReporterIonMassMode);
            this.TabPageReporterIons.Controls.Add(this.fraReporterIonOptions);
            this.TabPageReporterIons.Location = new System.Drawing.Point(4, 22);
            this.TabPageReporterIons.Name = "TabPageReporterIons";
            this.TabPageReporterIons.Size = new System.Drawing.Size(885, 282);
            this.TabPageReporterIons.TabIndex = 9;
            this.TabPageReporterIons.Text = "Reporter Ions / MRM";
            this.TabPageReporterIons.UseVisualStyleBackColor = true;
            // 
            // fraDecoyOptions
            // 
            this.fraDecoyOptions.Controls.Add(this.lblParentIonDecoyMassDaUnits);
            this.fraDecoyOptions.Controls.Add(this.txtParentIonDecoyMassDa);
            this.fraDecoyOptions.Controls.Add(this.lblParentIonDecoyMassDa);
            this.fraDecoyOptions.Location = new System.Drawing.Point(532, 157);
            this.fraDecoyOptions.Name = "fraDecoyOptions";
            this.fraDecoyOptions.Size = new System.Drawing.Size(159, 74);
            this.fraDecoyOptions.TabIndex = 3;
            this.fraDecoyOptions.TabStop = false;
            this.fraDecoyOptions.Text = "Decoy Options";
            // 
            // lblParentIonDecoyMassDaUnits
            // 
            this.lblParentIonDecoyMassDaUnits.Location = new System.Drawing.Point(72, 45);
            this.lblParentIonDecoyMassDaUnits.Name = "lblParentIonDecoyMassDaUnits";
            this.lblParentIonDecoyMassDaUnits.Size = new System.Drawing.Size(34, 17);
            this.lblParentIonDecoyMassDaUnits.TabIndex = 2;
            this.lblParentIonDecoyMassDaUnits.Text = "Da";
            // 
            // txtParentIonDecoyMassDa
            // 
            this.txtParentIonDecoyMassDa.Location = new System.Drawing.Point(10, 42);
            this.txtParentIonDecoyMassDa.Name = "txtParentIonDecoyMassDa";
            this.txtParentIonDecoyMassDa.Size = new System.Drawing.Size(56, 20);
            this.txtParentIonDecoyMassDa.TabIndex = 1;
            this.txtParentIonDecoyMassDa.Text = "0";
            // 
            // lblParentIonDecoyMassDa
            // 
            this.lblParentIonDecoyMassDa.Location = new System.Drawing.Point(7, 23);
            this.lblParentIonDecoyMassDa.Name = "lblParentIonDecoyMassDa";
            this.lblParentIonDecoyMassDa.Size = new System.Drawing.Size(141, 16);
            this.lblParentIonDecoyMassDa.TabIndex = 0;
            this.lblParentIonDecoyMassDa.Text = "Parent Ion Decoy Mass";
            // 
            // fraMRMOptions
            // 
            this.fraMRMOptions.Controls.Add(this.chkMRMWriteIntensityCrosstab);
            this.fraMRMOptions.Controls.Add(this.lblMRMInfo);
            this.fraMRMOptions.Controls.Add(this.chkMRMWriteDataList);
            this.fraMRMOptions.Location = new System.Drawing.Point(316, 16);
            this.fraMRMOptions.Name = "fraMRMOptions";
            this.fraMRMOptions.Size = new System.Drawing.Size(341, 135);
            this.fraMRMOptions.TabIndex = 2;
            this.fraMRMOptions.TabStop = false;
            this.fraMRMOptions.Text = "MRM Options";
            // 
            // chkMRMWriteIntensityCrosstab
            // 
            this.chkMRMWriteIntensityCrosstab.Location = new System.Drawing.Point(19, 104);
            this.chkMRMWriteIntensityCrosstab.Name = "chkMRMWriteIntensityCrosstab";
            this.chkMRMWriteIntensityCrosstab.Size = new System.Drawing.Size(305, 18);
            this.chkMRMWriteIntensityCrosstab.TabIndex = 2;
            this.chkMRMWriteIntensityCrosstab.Text = "Save MRM intensity crosstab (wide, rectangular file)";
            // 
            // lblMRMInfo
            // 
            this.lblMRMInfo.Location = new System.Drawing.Point(6, 16);
            this.lblMRMInfo.Name = "lblMRMInfo";
            this.lblMRMInfo.Size = new System.Drawing.Size(329, 61);
            this.lblMRMInfo.TabIndex = 0;
            this.lblMRMInfo.Text = resources.GetString("lblMRMInfo.Text");
            // 
            // chkMRMWriteDataList
            // 
            this.chkMRMWriteDataList.Location = new System.Drawing.Point(19, 80);
            this.chkMRMWriteDataList.Name = "chkMRMWriteDataList";
            this.chkMRMWriteDataList.Size = new System.Drawing.Size(305, 18);
            this.chkMRMWriteDataList.TabIndex = 1;
            this.chkMRMWriteDataList.Text = "Save MRM data list (long, narrow file)";
            // 
            // fraReporterIonMassMode
            // 
            this.fraReporterIonMassMode.Controls.Add(this.cboReporterIonMassMode);
            this.fraReporterIonMassMode.Location = new System.Drawing.Point(16, 157);
            this.fraReporterIonMassMode.Name = "fraReporterIonMassMode";
            this.fraReporterIonMassMode.Size = new System.Drawing.Size(510, 74);
            this.fraReporterIonMassMode.TabIndex = 1;
            this.fraReporterIonMassMode.TabStop = false;
            this.fraReporterIonMassMode.Text = "Reporter Ion Mass Mode";
            // 
            // cboReporterIonMassMode
            // 
            this.cboReporterIonMassMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboReporterIonMassMode.Location = new System.Drawing.Point(7, 23);
            this.cboReporterIonMassMode.Name = "cboReporterIonMassMode";
            this.cboReporterIonMassMode.Size = new System.Drawing.Size(494, 21);
            this.cboReporterIonMassMode.TabIndex = 13;
            this.cboReporterIonMassMode.SelectedIndexChanged += new System.EventHandler(this.cboReporterIonMassMode_SelectedIndexChanged);
            // 
            // fraReporterIonOptions
            // 
            this.fraReporterIonOptions.Controls.Add(this.chkReporterIonApplyAbundanceCorrection);
            this.fraReporterIonOptions.Controls.Add(this.chkReporterIonSaveUncorrectedIntensities);
            this.fraReporterIonOptions.Controls.Add(this.chkReporterIonSaveObservedMasses);
            this.fraReporterIonOptions.Controls.Add(this.txtReporterIonMZToleranceDa);
            this.fraReporterIonOptions.Controls.Add(this.lblReporterIonMZToleranceDa);
            this.fraReporterIonOptions.Controls.Add(this.chkReporterIonStatsEnabled);
            this.fraReporterIonOptions.Location = new System.Drawing.Point(16, 16);
            this.fraReporterIonOptions.Name = "fraReporterIonOptions";
            this.fraReporterIonOptions.Size = new System.Drawing.Size(282, 135);
            this.fraReporterIonOptions.TabIndex = 0;
            this.fraReporterIonOptions.TabStop = false;
            this.fraReporterIonOptions.Text = "Reporter Ion Options";
            // 
            // chkReporterIonApplyAbundanceCorrection
            // 
            this.chkReporterIonApplyAbundanceCorrection.Location = new System.Drawing.Point(16, 89);
            this.chkReporterIonApplyAbundanceCorrection.Name = "chkReporterIonApplyAbundanceCorrection";
            this.chkReporterIonApplyAbundanceCorrection.Size = new System.Drawing.Size(251, 18);
            this.chkReporterIonApplyAbundanceCorrection.TabIndex = 4;
            this.chkReporterIonApplyAbundanceCorrection.Text = "Apply iTraq isotopic abundance correction";
            // 
            // chkReporterIonSaveUncorrectedIntensities
            // 
            this.chkReporterIonSaveUncorrectedIntensities.Location = new System.Drawing.Point(32, 110);
            this.chkReporterIonSaveUncorrectedIntensities.Name = "chkReporterIonSaveUncorrectedIntensities";
            this.chkReporterIonSaveUncorrectedIntensities.Size = new System.Drawing.Size(224, 18);
            this.chkReporterIonSaveUncorrectedIntensities.TabIndex = 5;
            this.chkReporterIonSaveUncorrectedIntensities.Text = "Write original uncorrected intensities";
            // 
            // chkReporterIonSaveObservedMasses
            // 
            this.chkReporterIonSaveObservedMasses.Location = new System.Drawing.Point(16, 68);
            this.chkReporterIonSaveObservedMasses.Name = "chkReporterIonSaveObservedMasses";
            this.chkReporterIonSaveObservedMasses.Size = new System.Drawing.Size(251, 18);
            this.chkReporterIonSaveObservedMasses.TabIndex = 3;
            this.chkReporterIonSaveObservedMasses.Text = "Write observed m/z values to Reporter Ions file";
            // 
            // txtReporterIonMZToleranceDa
            // 
            this.txtReporterIonMZToleranceDa.Location = new System.Drawing.Point(171, 42);
            this.txtReporterIonMZToleranceDa.Name = "txtReporterIonMZToleranceDa";
            this.txtReporterIonMZToleranceDa.Size = new System.Drawing.Size(40, 20);
            this.txtReporterIonMZToleranceDa.TabIndex = 2;
            this.txtReporterIonMZToleranceDa.Text = "0.5";
            // 
            // lblReporterIonMZToleranceDa
            // 
            this.lblReporterIonMZToleranceDa.Location = new System.Drawing.Point(13, 45);
            this.lblReporterIonMZToleranceDa.Name = "lblReporterIonMZToleranceDa";
            this.lblReporterIonMZToleranceDa.Size = new System.Drawing.Size(152, 16);
            this.lblReporterIonMZToleranceDa.TabIndex = 1;
            this.lblReporterIonMZToleranceDa.Text = "m/z Tolerance Half Width";
            // 
            // chkReporterIonStatsEnabled
            // 
            this.chkReporterIonStatsEnabled.Location = new System.Drawing.Point(16, 24);
            this.chkReporterIonStatsEnabled.Name = "chkReporterIonStatsEnabled";
            this.chkReporterIonStatsEnabled.Size = new System.Drawing.Size(200, 18);
            this.chkReporterIonStatsEnabled.TabIndex = 0;
            this.chkReporterIonStatsEnabled.Text = "Generate Reporter Ion Stats";
            this.chkReporterIonStatsEnabled.CheckedChanged += new System.EventHandler(this.chkReporterIonStatsEnabled_CheckedChanged);
            // 
            // TabPageAdvancedOptions
            // 
            this.TabPageAdvancedOptions.Controls.Add(this.fraAdditionalInfoFiles);
            this.TabPageAdvancedOptions.Controls.Add(this.fraDatasetLookupInfo);
            this.TabPageAdvancedOptions.Controls.Add(this.fraMemoryConservationOptions);
            this.TabPageAdvancedOptions.Location = new System.Drawing.Point(4, 22);
            this.TabPageAdvancedOptions.Name = "TabPageAdvancedOptions";
            this.TabPageAdvancedOptions.Size = new System.Drawing.Size(885, 282);
            this.TabPageAdvancedOptions.TabIndex = 8;
            this.TabPageAdvancedOptions.Text = "Advanced";
            this.TabPageAdvancedOptions.UseVisualStyleBackColor = true;
            // 
            // fraAdditionalInfoFiles
            // 
            this.fraAdditionalInfoFiles.Controls.Add(this.chkConsolidateConstantExtendedHeaderValues);
            this.fraAdditionalInfoFiles.Controls.Add(this.lblStatusLogKeyNameFilterList);
            this.fraAdditionalInfoFiles.Controls.Add(this.txtStatusLogKeyNameFilterList);
            this.fraAdditionalInfoFiles.Controls.Add(this.chkSaveExtendedStatsFileIncludeStatusLog);
            this.fraAdditionalInfoFiles.Controls.Add(this.chkSaveExtendedStatsFileIncludeFilterText);
            this.fraAdditionalInfoFiles.Controls.Add(this.chkSaveMSTuneFile);
            this.fraAdditionalInfoFiles.Controls.Add(this.chkSaveMSMethodFile);
            this.fraAdditionalInfoFiles.Controls.Add(this.chkSaveExtendedStatsFile);
            this.fraAdditionalInfoFiles.Location = new System.Drawing.Point(318, 3);
            this.fraAdditionalInfoFiles.Name = "fraAdditionalInfoFiles";
            this.fraAdditionalInfoFiles.Size = new System.Drawing.Size(352, 117);
            this.fraAdditionalInfoFiles.TabIndex = 1;
            this.fraAdditionalInfoFiles.TabStop = false;
            this.fraAdditionalInfoFiles.Text = "Thermo Info Files";
            // 
            // chkConsolidateConstantExtendedHeaderValues
            // 
            this.chkConsolidateConstantExtendedHeaderValues.Checked = true;
            this.chkConsolidateConstantExtendedHeaderValues.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkConsolidateConstantExtendedHeaderValues.Location = new System.Drawing.Point(32, 97);
            this.chkConsolidateConstantExtendedHeaderValues.Name = "chkConsolidateConstantExtendedHeaderValues";
            this.chkConsolidateConstantExtendedHeaderValues.Size = new System.Drawing.Size(160, 18);
            this.chkConsolidateConstantExtendedHeaderValues.TabIndex = 5;
            this.chkConsolidateConstantExtendedHeaderValues.Text = "Consolidate constant values";
            // 
            // lblStatusLogKeyNameFilterList
            // 
            this.lblStatusLogKeyNameFilterList.Location = new System.Drawing.Point(192, 27);
            this.lblStatusLogKeyNameFilterList.Name = "lblStatusLogKeyNameFilterList";
            this.lblStatusLogKeyNameFilterList.Size = new System.Drawing.Size(146, 17);
            this.lblStatusLogKeyNameFilterList.TabIndex = 6;
            this.lblStatusLogKeyNameFilterList.Text = "Status Log Keys to Include";
            // 
            // txtStatusLogKeyNameFilterList
            // 
            this.txtStatusLogKeyNameFilterList.Location = new System.Drawing.Point(195, 47);
            this.txtStatusLogKeyNameFilterList.Multiline = true;
            this.txtStatusLogKeyNameFilterList.Name = "txtStatusLogKeyNameFilterList";
            this.txtStatusLogKeyNameFilterList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatusLogKeyNameFilterList.Size = new System.Drawing.Size(149, 50);
            this.txtStatusLogKeyNameFilterList.TabIndex = 7;
            // 
            // chkSaveExtendedStatsFileIncludeStatusLog
            // 
            this.chkSaveExtendedStatsFileIncludeStatusLog.Location = new System.Drawing.Point(32, 80);
            this.chkSaveExtendedStatsFileIncludeStatusLog.Name = "chkSaveExtendedStatsFileIncludeStatusLog";
            this.chkSaveExtendedStatsFileIncludeStatusLog.Size = new System.Drawing.Size(160, 18);
            this.chkSaveExtendedStatsFileIncludeStatusLog.TabIndex = 4;
            this.chkSaveExtendedStatsFileIncludeStatusLog.Text = "Include voltage, temp., etc.";
            this.chkSaveExtendedStatsFileIncludeStatusLog.CheckedChanged += new System.EventHandler(this.chkSaveExtendedStatsFileIncludeStatusLog_CheckedChanged);
            // 
            // chkSaveExtendedStatsFileIncludeFilterText
            // 
            this.chkSaveExtendedStatsFileIncludeFilterText.Checked = true;
            this.chkSaveExtendedStatsFileIncludeFilterText.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSaveExtendedStatsFileIncludeFilterText.Location = new System.Drawing.Point(32, 64);
            this.chkSaveExtendedStatsFileIncludeFilterText.Name = "chkSaveExtendedStatsFileIncludeFilterText";
            this.chkSaveExtendedStatsFileIncludeFilterText.Size = new System.Drawing.Size(160, 16);
            this.chkSaveExtendedStatsFileIncludeFilterText.TabIndex = 3;
            this.chkSaveExtendedStatsFileIncludeFilterText.Text = "Include Scan Filter Text";
            // 
            // chkSaveMSTuneFile
            // 
            this.chkSaveMSTuneFile.Location = new System.Drawing.Point(16, 32);
            this.chkSaveMSTuneFile.Name = "chkSaveMSTuneFile";
            this.chkSaveMSTuneFile.Size = new System.Drawing.Size(176, 16);
            this.chkSaveMSTuneFile.TabIndex = 1;
            this.chkSaveMSTuneFile.Text = "Save MS Tune File";
            // 
            // chkSaveMSMethodFile
            // 
            this.chkSaveMSMethodFile.Checked = true;
            this.chkSaveMSMethodFile.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSaveMSMethodFile.Location = new System.Drawing.Point(16, 16);
            this.chkSaveMSMethodFile.Name = "chkSaveMSMethodFile";
            this.chkSaveMSMethodFile.Size = new System.Drawing.Size(176, 16);
            this.chkSaveMSMethodFile.TabIndex = 0;
            this.chkSaveMSMethodFile.Text = "Save MS Method File";
            // 
            // chkSaveExtendedStatsFile
            // 
            this.chkSaveExtendedStatsFile.Checked = true;
            this.chkSaveExtendedStatsFile.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSaveExtendedStatsFile.Location = new System.Drawing.Point(16, 48);
            this.chkSaveExtendedStatsFile.Name = "chkSaveExtendedStatsFile";
            this.chkSaveExtendedStatsFile.Size = new System.Drawing.Size(176, 16);
            this.chkSaveExtendedStatsFile.TabIndex = 2;
            this.chkSaveExtendedStatsFile.Text = "Save Extended Stats File";
            this.chkSaveExtendedStatsFile.CheckedChanged += new System.EventHandler(this.chkSaveExtendedStatsFile_CheckedChanged);
            // 
            // fraDatasetLookupInfo
            // 
            this.fraDatasetLookupInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fraDatasetLookupInfo.Controls.Add(this.cmdSetConnectionStringToPNNLServer);
            this.fraDatasetLookupInfo.Controls.Add(this.txtDatasetInfoQuerySQL);
            this.fraDatasetLookupInfo.Controls.Add(this.lblDatasetInfoQuerySQL);
            this.fraDatasetLookupInfo.Controls.Add(this.txtDatabaseConnectionString);
            this.fraDatasetLookupInfo.Controls.Add(this.lblDatabaseConnectionString);
            this.fraDatasetLookupInfo.Controls.Add(this.lblDatasetLookupFilePath);
            this.fraDatasetLookupInfo.Controls.Add(this.cmdSelectDatasetLookupFile);
            this.fraDatasetLookupInfo.Controls.Add(this.txtDatasetLookupFilePath);
            this.fraDatasetLookupInfo.Location = new System.Drawing.Point(16, 120);
            this.fraDatasetLookupInfo.Name = "fraDatasetLookupInfo";
            this.fraDatasetLookupInfo.Size = new System.Drawing.Size(861, 152);
            this.fraDatasetLookupInfo.TabIndex = 1;
            this.fraDatasetLookupInfo.TabStop = false;
            this.fraDatasetLookupInfo.Text = "Dataset ID lookup information";
            // 
            // cmdSetConnectionStringToPNNLServer
            // 
            this.cmdSetConnectionStringToPNNLServer.Location = new System.Drawing.Point(16, 20);
            this.cmdSetConnectionStringToPNNLServer.Name = "cmdSetConnectionStringToPNNLServer";
            this.cmdSetConnectionStringToPNNLServer.Size = new System.Drawing.Size(120, 20);
            this.cmdSetConnectionStringToPNNLServer.TabIndex = 0;
            this.cmdSetConnectionStringToPNNLServer.Text = "Set to PNNL Server";
            this.cmdSetConnectionStringToPNNLServer.Click += new System.EventHandler(this.cmdSetConnectionStringToPNNLServer_Click);
            // 
            // txtDatasetInfoQuerySQL
            // 
            this.txtDatasetInfoQuerySQL.Location = new System.Drawing.Point(168, 64);
            this.txtDatasetInfoQuerySQL.Name = "txtDatasetInfoQuerySQL";
            this.txtDatasetInfoQuerySQL.Size = new System.Drawing.Size(416, 20);
            this.txtDatasetInfoQuerySQL.TabIndex = 4;
            // 
            // lblDatasetInfoQuerySQL
            // 
            this.lblDatasetInfoQuerySQL.Location = new System.Drawing.Point(8, 69);
            this.lblDatasetInfoQuerySQL.Name = "lblDatasetInfoQuerySQL";
            this.lblDatasetInfoQuerySQL.Size = new System.Drawing.Size(160, 16);
            this.lblDatasetInfoQuerySQL.TabIndex = 3;
            this.lblDatasetInfoQuerySQL.Text = "Dataset Info Query SQL:";
            // 
            // txtDatabaseConnectionString
            // 
            this.txtDatabaseConnectionString.Location = new System.Drawing.Point(168, 40);
            this.txtDatabaseConnectionString.Name = "txtDatabaseConnectionString";
            this.txtDatabaseConnectionString.Size = new System.Drawing.Size(416, 20);
            this.txtDatabaseConnectionString.TabIndex = 2;
            // 
            // lblDatabaseConnectionString
            // 
            this.lblDatabaseConnectionString.Location = new System.Drawing.Point(8, 45);
            this.lblDatabaseConnectionString.Name = "lblDatabaseConnectionString";
            this.lblDatabaseConnectionString.Size = new System.Drawing.Size(160, 16);
            this.lblDatabaseConnectionString.TabIndex = 1;
            this.lblDatabaseConnectionString.Text = "SQL Server Connection String:";
            // 
            // lblDatasetLookupFilePath
            // 
            this.lblDatasetLookupFilePath.Location = new System.Drawing.Point(8, 96);
            this.lblDatasetLookupFilePath.Name = "lblDatasetLookupFilePath";
            this.lblDatasetLookupFilePath.Size = new System.Drawing.Size(528, 16);
            this.lblDatasetLookupFilePath.TabIndex = 5;
            this.lblDatasetLookupFilePath.Text = "Dataset lookup file (dataset name and dataset ID number, tab-separated); used if " +
    "DB not available";
            // 
            // cmdSelectDatasetLookupFile
            // 
            this.cmdSelectDatasetLookupFile.Location = new System.Drawing.Point(8, 120);
            this.cmdSelectDatasetLookupFile.Name = "cmdSelectDatasetLookupFile";
            this.cmdSelectDatasetLookupFile.Size = new System.Drawing.Size(80, 24);
            this.cmdSelectDatasetLookupFile.TabIndex = 6;
            this.cmdSelectDatasetLookupFile.Text = "Select File";
            this.cmdSelectDatasetLookupFile.Click += new System.EventHandler(this.cmdSelectDatasetLookupFile_Click);
            // 
            // txtDatasetLookupFilePath
            // 
            this.txtDatasetLookupFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDatasetLookupFilePath.Location = new System.Drawing.Point(104, 120);
            this.txtDatasetLookupFilePath.Name = "txtDatasetLookupFilePath";
            this.txtDatasetLookupFilePath.Size = new System.Drawing.Size(741, 20);
            this.txtDatasetLookupFilePath.TabIndex = 7;
            // 
            // fraMemoryConservationOptions
            // 
            this.fraMemoryConservationOptions.Controls.Add(this.chkSkipMSMSProcessing);
            this.fraMemoryConservationOptions.Controls.Add(this.chkSkipSICAndRawDataProcessing);
            this.fraMemoryConservationOptions.Controls.Add(this.chkExportRawDataOnly);
            this.fraMemoryConservationOptions.Location = new System.Drawing.Point(16, 16);
            this.fraMemoryConservationOptions.Name = "fraMemoryConservationOptions";
            this.fraMemoryConservationOptions.Size = new System.Drawing.Size(296, 104);
            this.fraMemoryConservationOptions.TabIndex = 0;
            this.fraMemoryConservationOptions.TabStop = false;
            this.fraMemoryConservationOptions.Text = "Memory Usage and Speed Options";
            // 
            // chkSkipMSMSProcessing
            // 
            this.chkSkipMSMSProcessing.Location = new System.Drawing.Point(16, 19);
            this.chkSkipMSMSProcessing.Name = "chkSkipMSMSProcessing";
            this.chkSkipMSMSProcessing.Size = new System.Drawing.Size(264, 18);
            this.chkSkipMSMSProcessing.TabIndex = 0;
            this.chkSkipMSMSProcessing.Text = "Skip MS/MS Processing (no similarity testing)";
            this.chkSkipMSMSProcessing.CheckedChanged += new System.EventHandler(this.chkSkipMSMSProcessing_CheckedChanged);
            // 
            // chkSkipSICAndRawDataProcessing
            // 
            this.chkSkipSICAndRawDataProcessing.Location = new System.Drawing.Point(16, 40);
            this.chkSkipSICAndRawDataProcessing.Name = "chkSkipSICAndRawDataProcessing";
            this.chkSkipSICAndRawDataProcessing.Size = new System.Drawing.Size(217, 34);
            this.chkSkipSICAndRawDataProcessing.TabIndex = 1;
            this.chkSkipSICAndRawDataProcessing.Text = "Only Export Chromatograms and Scan Stats (no SICs or raw data)";
            this.chkSkipSICAndRawDataProcessing.CheckedChanged += new System.EventHandler(this.chkSkipSICAndRawDataProcessing_CheckedChanged);
            // 
            // chkExportRawDataOnly
            // 
            this.chkExportRawDataOnly.Location = new System.Drawing.Point(16, 79);
            this.chkExportRawDataOnly.Name = "chkExportRawDataOnly";
            this.chkExportRawDataOnly.Size = new System.Drawing.Size(200, 18);
            this.chkExportRawDataOnly.TabIndex = 2;
            this.chkExportRawDataOnly.Text = "Export Raw Data Only (No SICs)";
            this.chkExportRawDataOnly.CheckedChanged += new System.EventHandler(this.chkExportRawDataOnly_CheckedChanged);
            // 
            // TabPageLog
            // 
            this.TabPageLog.Controls.Add(this.txtLogMessages);
            this.TabPageLog.Location = new System.Drawing.Point(4, 22);
            this.TabPageLog.Name = "TabPageLog";
            this.TabPageLog.Padding = new System.Windows.Forms.Padding(3);
            this.TabPageLog.Size = new System.Drawing.Size(885, 282);
            this.TabPageLog.TabIndex = 10;
            this.TabPageLog.Text = "Log";
            this.TabPageLog.UseVisualStyleBackColor = true;
            // 
            // txtLogMessages
            // 
            this.txtLogMessages.Location = new System.Drawing.Point(5, 5);
            this.txtLogMessages.Multiline = true;
            this.txtLogMessages.Name = "txtLogMessages";
            this.txtLogMessages.ReadOnly = true;
            this.txtLogMessages.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLogMessages.Size = new System.Drawing.Size(725, 273);
            this.txtLogMessages.TabIndex = 1;
            this.txtLogMessages.Text = "No log messages.";
            // 
            // fraOutputDirectoryPath
            // 
            this.fraOutputDirectoryPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fraOutputDirectoryPath.Controls.Add(this.cmdStartProcessing);
            this.fraOutputDirectoryPath.Controls.Add(this.cmdSelectOutputDirectory);
            this.fraOutputDirectoryPath.Controls.Add(this.txtOutputDirectoryPath);
            this.fraOutputDirectoryPath.Location = new System.Drawing.Point(8, 80);
            this.fraOutputDirectoryPath.Name = "fraOutputDirectoryPath";
            this.fraOutputDirectoryPath.Size = new System.Drawing.Size(889, 88);
            this.fraOutputDirectoryPath.TabIndex = 1;
            this.fraOutputDirectoryPath.TabStop = false;
            this.fraOutputDirectoryPath.Text = "Output Folder Path";
            // 
            // cmdStartProcessing
            // 
            this.cmdStartProcessing.Location = new System.Drawing.Point(272, 56);
            this.cmdStartProcessing.Name = "cmdStartProcessing";
            this.cmdStartProcessing.Size = new System.Drawing.Size(110, 24);
            this.cmdStartProcessing.TabIndex = 2;
            this.cmdStartProcessing.Text = "Start &Processing";
            this.cmdStartProcessing.Click += new System.EventHandler(this.cmdStartProcessing_Click);
            // 
            // cmdSelectOutputDirectory
            // 
            this.cmdSelectOutputDirectory.Location = new System.Drawing.Point(8, 24);
            this.cmdSelectOutputDirectory.Name = "cmdSelectOutputDirectory";
            this.cmdSelectOutputDirectory.Size = new System.Drawing.Size(80, 38);
            this.cmdSelectOutputDirectory.TabIndex = 0;
            this.cmdSelectOutputDirectory.Text = "Select &Directory";
            this.cmdSelectOutputDirectory.Click += new System.EventHandler(this.cmdSelectOutputDirectory_Click);
            // 
            // txtOutputDirectoryPath
            // 
            this.txtOutputDirectoryPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutputDirectoryPath.Location = new System.Drawing.Point(104, 26);
            this.txtOutputDirectoryPath.Name = "txtOutputDirectoryPath";
            this.txtOutputDirectoryPath.Size = new System.Drawing.Size(769, 20);
            this.txtOutputDirectoryPath.TabIndex = 1;
            // 
            // frmMain
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(904, 625);
            this.Controls.Add(this.fraOutputDirectoryPath);
            this.Controls.Add(this.tbsOptions);
            this.Controls.Add(this.fraInputFilePath);
            this.Menu = this.MainMenuControl;
            this.MinimumSize = new System.Drawing.Size(450, 0);
            this.Name = "frmMain";
            this.Text = "MASIC";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.frmMain_Closing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.Resize += new System.EventHandler(this.frmMain_Resize);
            this.fraInputFilePath.ResumeLayout(false);
            this.fraInputFilePath.PerformLayout();
            this.tbsOptions.ResumeLayout(false);
            this.TabPageMasicExportOptions.ResumeLayout(false);
            this.TabPageMasicExportOptions.PerformLayout();
            this.fraExportAllSpectraDataPoints.ResumeLayout(false);
            this.fraExportAllSpectraDataPoints.PerformLayout();
            this.TabPageSICOptions.ResumeLayout(false);
            this.fraInputFileRangeFilters.ResumeLayout(false);
            this.fraInputFileRangeFilters.PerformLayout();
            this.fraSICSearchThresholds.ResumeLayout(false);
            this.fraSICSearchThresholds.PerformLayout();
            this.TabPagePeakFindingOptions.ResumeLayout(false);
            this.fraSICNoiseThresholds.ResumeLayout(false);
            this.fraSICNoiseThresholds.PerformLayout();
            this.fraSmoothingOptions.ResumeLayout(false);
            this.fraSmoothingOptions.PerformLayout();
            this.fraPeakFindingOptions.ResumeLayout(false);
            this.fraPeakFindingOptions.PerformLayout();
            this.TabPageBinningAndSimilarityOptions.ResumeLayout(false);
            this.fraMassSpectraNoiseThresholds.ResumeLayout(false);
            this.fraMassSpectraNoiseThresholds.PerformLayout();
            this.fraBinningIntensityOptions.ResumeLayout(false);
            this.fraBinningIntensityOptions.PerformLayout();
            this.fraSpectrumSimilarityOptions.ResumeLayout(false);
            this.fraSpectrumSimilarityOptions.PerformLayout();
            this.fraBinningMZOptions.ResumeLayout(false);
            this.fraBinningMZOptions.PerformLayout();
            this.TabPageCustomSICOptions.ResumeLayout(false);
            this.TabPageCustomSICOptions.PerformLayout();
            this.fraCustomSICControls.ResumeLayout(false);
            this.fraCustomSICControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgCustomSICValues)).EndInit();
            this.TabPageReporterIons.ResumeLayout(false);
            this.fraDecoyOptions.ResumeLayout(false);
            this.fraDecoyOptions.PerformLayout();
            this.fraMRMOptions.ResumeLayout(false);
            this.fraReporterIonMassMode.ResumeLayout(false);
            this.fraReporterIonOptions.ResumeLayout(false);
            this.fraReporterIonOptions.PerformLayout();
            this.TabPageAdvancedOptions.ResumeLayout(false);
            this.fraAdditionalInfoFiles.ResumeLayout(false);
            this.fraAdditionalInfoFiles.PerformLayout();
            this.fraDatasetLookupInfo.ResumeLayout(false);
            this.fraDatasetLookupInfo.PerformLayout();
            this.fraMemoryConservationOptions.ResumeLayout(false);
            this.TabPageLog.ResumeLayout(false);
            this.TabPageLog.PerformLayout();
            this.fraOutputDirectoryPath.ResumeLayout(false);
            this.fraOutputDirectoryPath.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private TextBox txtInputFilePath;
        private Button cmdSelectFile;
        private GroupBox fraInputFilePath;
        private MenuItem mnuFile;
        private MenuItem mnuEdit;
        private MenuItem mnuEditResetOptions;
        private MenuItem mnuHelp;
        private MenuItem mnuHelpAbout;
        private MenuItem mnuEditSep1;
        private MainMenu MainMenuControl;
        private TabControl tbsOptions;
        private DataGrid dgCustomSICValues;
        private Button cmdClearCustomSICList;
        private Button cmdCustomSICValuesPopulate;
        private Button cmdPasteCustomSICList;
        private TabPage TabPageCustomSICOptions;
        private GroupBox fraCustomSICControls;
        private MenuItem mnuFileSelectOutputDirectory;
        private MenuItem mnuFileLoadOptions;
        private MenuItem mnuFileSaveOptions;
        private MenuItem mnuFileExit;
        private MenuItem mnuFileSelectInputFile;
        private MenuItem mnuFileSep1;
        private MenuItem mnuFileSep2;
        private GroupBox fraOutputDirectoryPath;
        private Button cmdSelectOutputDirectory;
        private TextBox txtOutputDirectoryPath;
        private MenuItem mnuEditProcessFile;
        private TabPage TabPageMasicExportOptions;
        private TabPage TabPageSICOptions;
        private CheckBox chkIncludeHeaders;
        private GroupBox fraExportAllSpectraDataPoints;
        private ComboBox cboExportRawDataFileFormat;
        private Label lblExportDataPointsFormat;
        private CheckBox chkExportRawDataIncludeMSMS;
        private TextBox txtExportRawDataMaxIonCountPerScan;
        private CheckBox chkExportRawDataRenumberScans;
        private TextBox txtExportRawDataIntensityMinimum;
        private CheckBox chkExportRawSpectraData;
        private GroupBox fraSICSearchThresholds;
        private TextBox txtSICTolerance;
        private Label lblSICToleranceDa;
        private TextBox txtMaxPeakWidthMinutesBackward;
        private Label lblMaxPeakWidthMinutes;
        private Label lblMaxPeakWidthMinutesBackward;
        private Label lblMaxPeakWidthMinutesForward;
        private TextBox txtMaxPeakWidthMinutesForward;
        private TextBox txtIntensityThresholdAbsoluteMinimum;
        private Label lblIntensityThresholdAbsoluteMinimum;
        private TextBox txtIntensityThresholdFractionMax;
        private Label lblIntensityThresholdFractionMax;
        private TabPage TabPagePeakFindingOptions;
        private GroupBox fraPeakFindingOptions;
        private Label lblSavitzkyGolayFilterOrder;
        private TextBox txtSavitzkyGolayFilterOrder;
        private TextBox txtMaxDistanceScansNoOverlap;
        private TextBox txtMaxAllowedUpwardSpikeFractionMax;
        private Label lblMaxAllowedUpwardSpikeFractionMax;
        private Label lblMaxDistanceScansNoOverlap;
        private TextBox txtInitialPeakWidthScansMaximum;
        private Label lblInitialPeakWidthScansMaximum;
        private GroupBox fraSmoothingOptions;
        private TabPage TabPageBinningAndSimilarityOptions;
        private GroupBox fraBinningMZOptions;
        private TextBox txtBinSize;
        private Label lblBinSize;
        private TextBox txtBinEndX;
        private Label lblBinEndX;
        private TextBox txtBinStartX;
        private Label lblBinStartX;
        private GroupBox fraSpectrumSimilarityOptions;
        private TextBox txtSpectrumSimilarityMinimum;
        private Label lblSpectrumSimilarityMinimum;
        private TextBox txtSimilarIonToleranceHalfWidthMinutes;
        private Label lblSimilarIonTimeToleranceHalfWidth;
        private TextBox txtSimilarIonMZToleranceHalfWidth;
        private Label lblSimilarIonMZToleranceHalfWidth;
        private GroupBox fraBinningIntensityOptions;
        private TextBox txtBinnedDataIntensityPrecisionPct;
        private Label lblBinnedDataIntensityPrecisionPct;
        private TextBox txtMaximumBinCount;
        private Label lblMaximumBinCount;
        private CheckBox chkBinnedDataNormalize;
        private CheckBox chkBinnedDataSumAllIntensitiesForBin;
        private Button cmdStartProcessing;
        private TextBox txtInitialPeakWidthScansScaler;
        private Label lblInitialPeakWidthScansScaler;
        private Label lblBinnedDataIntensityPrecisionPctUnits;
        private ComboBox cboSICNoiseThresholdMode;
        private Label lblNoiseThresholdMode;
        private TextBox txtSICNoiseFractionLowIntensityDataToAverage;
        private TextBox txtSICNoiseThresholdIntensity;
        private Label lblSICOptionsOverview;
        private Label lblRawDataExportOverview;
        private TextBox txtDatasetID;
        private Label lblDatasetID;
        private TextBox txtCustomSICScanOrAcqTimeTolerance;
        private Label lblCustomSICScanTolerance;
        private Label lblExportRawDataMaxIonCountPerScan;
        private Label lblExportRawDataIntensityMinimum;
        private Label lblSICNoiseFractionLowIntensityDataToAverage;
        private Label lblSICNoiseThresholdIntensity;
        private TabPage TabPageAdvancedOptions;
        private GroupBox fraMemoryConservationOptions;
        private CheckBox chkSkipMSMSProcessing;
        private GroupBox fraDatasetLookupInfo;
        private Button cmdSelectDatasetLookupFile;
        private TextBox txtDatasetLookupFilePath;
        private Label lblDatasetLookupFilePath;
        private Label lblDatabaseConnectionString;
        private TextBox txtDatabaseConnectionString;
        private GroupBox fraSICNoiseThresholds;
        private GroupBox fraMassSpectraNoiseThresholds;
        private TextBox txtMassSpectraNoiseFractionLowIntensityDataToAverage;
        private Label lblMassSpectraNoiseFractionLowIntensityDataToAverage;
        private ComboBox cboMassSpectraNoiseThresholdMode;
        private Label lblMassSpectraNoiseThresholdMode;
        private TextBox txtMassSpectraNoiseThresholdIntensity;
        private Label lblMassSpectraNoiseThresholdIntensity;
        private TextBox txtExportRawDataSignalToNoiseRatioMinimum;
        private Label lblExportRawDataSignalToNoiseRatioMinimum;
        private TextBox txtMassSpectraNoiseMinimumSignalToNoiseRatio;
        private Label lblMassSpectraNoiseMinimumSignalToNoiseRatio;
        private GroupBox fraInputFileRangeFilters;
        private Button cmdClearAllRangeFilters;
        private TextBox txtScanEnd;
        private TextBox txtScanStart;
        private Label lblScanEnd;
        private Label lblScanStart;
        private TextBox txtTimeEnd;
        private Label lblTimeEnd;
        private Label lblTimeStart;
        private TextBox txtTimeStart;
        private Label lblTimeEndUnits;
        private Label lblTimeStartUnits;
        private TextBox txtButterworthSamplingFrequency;
        private Label lblButterworthSamplingFrequency;
        private RadioButton optUseButterworthSmooth;
        private RadioButton optUseSavitzkyGolaySmooth;
        private CheckBox chkFindPeaksOnSmoothedData;
        private CheckBox chkSmoothDataRegardlessOfMinimumPeakWidth;
        private Label lblSimilarIonTimeToleranceHalfWidthUnits;
        private CheckBox chkExportRawDataOnly;
        private CheckBox chkLimitSearchToCustomMZs;
        private CheckBox chkReplaceSICZeroesWithMinimumPositiveValueFromMSData;
        private Button cmdSetConnectionStringToPNNLServer;
        private TextBox txtDatasetInfoQuerySQL;
        private Label lblDatasetInfoQuerySQL;
        private CheckBox chkRefineReportedParentIonMZ;
        private MenuItem mnuEditSaveDefaultOptions;
        private CheckBox chkSkipSICAndRawDataProcessing;
        private GroupBox fraAdditionalInfoFiles;
        private CheckBox chkSaveExtendedStatsFile;
        private CheckBox chkSaveMSMethodFile;
        private CheckBox chkSaveMSTuneFile;
        private CheckBox chkIncludeScanTimesInSICStatsFile;
        private Button cmdSelectCustomSICFile;
        private TextBox txtCustomSICFileName;
        private CheckBox chkSaveExtendedStatsFileIncludeFilterText;
        private CheckBox chkSaveExtendedStatsFileIncludeStatusLog;
        private TabPage TabPageReporterIons;
        private GroupBox fraReporterIonOptions;
        private CheckBox chkReporterIonStatsEnabled;
        private TextBox txtReporterIonMZToleranceDa;
        private Label lblReporterIonMZToleranceDa;
        private Label lblCustomSICToleranceType;
        private RadioButton optCustomSICScanToleranceAcqTime;
        private RadioButton optCustomSICScanToleranceRelative;
        private RadioButton optCustomSICScanToleranceAbsolute;
        private TextBox txtCustomSICFileDescription;
        private GroupBox fraMRMOptions;
        private CheckBox chkMRMWriteIntensityCrosstab;
        private Label lblMRMInfo;
        private CheckBox chkMRMWriteDataList;
        private RadioButton optSICTolerancePPM;
        private RadioButton optSICToleranceDa;
        private Label lblStatusLogKeyNameFilterList;
        private TextBox txtStatusLogKeyNameFilterList;
        private GroupBox fraDecoyOptions;
        private TextBox txtParentIonDecoyMassDa;
        private Label lblParentIonDecoyMassDa;
        private Label lblParentIonDecoyMassDaUnits;
        private CheckBox chkReporterIonSaveUncorrectedIntensities;
        private CheckBox chkReporterIonApplyAbundanceCorrection;
        private CheckBox chkConsolidateConstantExtendedHeaderValues;
        private CheckBox chkWriteDetailedSICDataFile;
        private GroupBox fraReporterIonMassMode;
        private ComboBox cboReporterIonMassMode;
        private CheckBox chkReporterIonSaveObservedMasses;
        private TabPage TabPageLog;
        private TextBox txtLogMessages;
    }
}
