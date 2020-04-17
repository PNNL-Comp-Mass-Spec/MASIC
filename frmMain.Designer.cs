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
            components = new System.ComponentModel.Container();
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            txtInputFilePath = new TextBox();
            cmdSelectFile = new Button();
            fraInputFilePath = new GroupBox();
            MainMenuControl = new MainMenu(components);
            mnuFile = new MenuItem();
            mnuFileSelectInputFile = new MenuItem();
            mnuFileSelectOutputDirectory = new MenuItem();
            mnuFileSep1 = new MenuItem();
            mnuFileLoadOptions = new MenuItem();
            mnuFileSaveOptions = new MenuItem();
            mnuFileSep2 = new MenuItem();
            mnuFileExit = new MenuItem();
            mnuEdit = new MenuItem();
            mnuEditProcessFile = new MenuItem();
            mnuEditSep1 = new MenuItem();
            mnuEditSaveDefaultOptions = new MenuItem();
            mnuEditResetOptions = new MenuItem();
            mnuHelp = new MenuItem();
            mnuHelpAbout = new MenuItem();
            tbsOptions = new TabControl();
            TabPageMasicExportOptions = new TabPage();
            chkWriteDetailedSICDataFile = new CheckBox();
            chkIncludeScanTimesInSICStatsFile = new CheckBox();
            txtDatasetID = new TextBox();
            lblDatasetID = new Label();
            lblRawDataExportOverview = new Label();
            fraExportAllSpectraDataPoints = new GroupBox();
            txtExportRawDataSignalToNoiseRatioMinimum = new TextBox();
            lblExportRawDataSignalToNoiseRatioMinimum = new Label();
            chkExportRawDataRenumberScans = new CheckBox();
            txtExportRawDataMaxIonCountPerScan = new TextBox();
            lblExportRawDataMaxIonCountPerScan = new Label();
            txtExportRawDataIntensityMinimum = new TextBox();
            lblExportRawDataIntensityMinimum = new Label();
            chkExportRawDataIncludeMSMS = new CheckBox();
            cboExportRawDataFileFormat = new ComboBox();
            lblExportDataPointsFormat = new Label();
            chkExportRawSpectraData = new CheckBox();
            chkIncludeHeaders = new CheckBox();
            TabPageSICOptions = new TabPage();
            fraInputFileRangeFilters = new GroupBox();
            lblTimeEndUnits = new Label();
            lblTimeStartUnits = new Label();
            txtTimeEnd = new TextBox();
            txtTimeStart = new TextBox();
            lblTimeEnd = new Label();
            lblTimeStart = new Label();
            txtScanEnd = new TextBox();
            txtScanStart = new TextBox();
            lblScanEnd = new Label();
            lblScanStart = new Label();
            cmdClearAllRangeFilters = new Button();
            lblSICOptionsOverview = new Label();
            fraSICSearchThresholds = new GroupBox();
            optSICTolerancePPM = new RadioButton();
            optSICToleranceDa = new RadioButton();
            chkRefineReportedParentIonMZ = new CheckBox();
            chkReplaceSICZeroesWithMinimumPositiveValueFromMSData = new CheckBox();
            txtMaxPeakWidthMinutesForward = new TextBox();
            txtMaxPeakWidthMinutesBackward = new TextBox();
            txtIntensityThresholdFractionMax = new TextBox();
            lblIntensityThresholdFractionMax = new Label();
            txtIntensityThresholdAbsoluteMinimum = new TextBox();
            lblIntensityThresholdAbsoluteMinimum = new Label();
            lblMaxPeakWidthMinutesForward = new Label();
            lblMaxPeakWidthMinutesBackward = new Label();
            lblMaxPeakWidthMinutes = new Label();
            txtSICTolerance = new TextBox();
            lblSICToleranceDa = new Label();
            TabPagePeakFindingOptions = new TabPage();
            fraSICNoiseThresholds = new GroupBox();
            cboSICNoiseThresholdMode = new ComboBox();
            lblNoiseThresholdMode = new Label();
            txtSICNoiseFractionLowIntensityDataToAverage = new TextBox();
            lblSICNoiseFractionLowIntensityDataToAverage = new Label();
            txtSICNoiseThresholdIntensity = new TextBox();
            lblSICNoiseThresholdIntensity = new Label();
            fraSmoothingOptions = new GroupBox();
            chkSmoothDataRegardlessOfMinimumPeakWidth = new CheckBox();
            chkFindPeaksOnSmoothedData = new CheckBox();
            optUseSavitzkyGolaySmooth = new RadioButton();
            txtButterworthSamplingFrequency = new TextBox();
            lblButterworthSamplingFrequency = new Label();
            txtSavitzkyGolayFilterOrder = new TextBox();
            lblSavitzkyGolayFilterOrder = new Label();
            optUseButterworthSmooth = new RadioButton();
            fraPeakFindingOptions = new GroupBox();
            txtInitialPeakWidthScansMaximum = new TextBox();
            lblInitialPeakWidthScansMaximum = new Label();
            txtInitialPeakWidthScansScaler = new TextBox();
            lblInitialPeakWidthScansScaler = new Label();
            txtMaxAllowedUpwardSpikeFractionMax = new TextBox();
            lblMaxAllowedUpwardSpikeFractionMax = new Label();
            txtMaxDistanceScansNoOverlap = new TextBox();
            lblMaxDistanceScansNoOverlap = new Label();
            TabPageBinningAndSimilarityOptions = new TabPage();
            fraMassSpectraNoiseThresholds = new GroupBox();
            txtMassSpectraNoiseMinimumSignalToNoiseRatio = new TextBox();
            lblMassSpectraNoiseMinimumSignalToNoiseRatio = new Label();
            txtMassSpectraNoiseThresholdIntensity = new TextBox();
            txtMassSpectraNoiseFractionLowIntensityDataToAverage = new TextBox();
            lblMassSpectraNoiseFractionLowIntensityDataToAverage = new Label();
            cboMassSpectraNoiseThresholdMode = new ComboBox();
            lblMassSpectraNoiseThresholdMode = new Label();
            lblMassSpectraNoiseThresholdIntensity = new Label();
            fraBinningIntensityOptions = new GroupBox();
            lblBinnedDataIntensityPrecisionPctUnits = new Label();
            chkBinnedDataSumAllIntensitiesForBin = new CheckBox();
            chkBinnedDataNormalize = new CheckBox();
            txtBinnedDataIntensityPrecisionPct = new TextBox();
            lblBinnedDataIntensityPrecisionPct = new Label();
            fraSpectrumSimilarityOptions = new GroupBox();
            lblSimilarIonTimeToleranceHalfWidthUnits = new Label();
            txtSpectrumSimilarityMinimum = new TextBox();
            lblSpectrumSimilarityMinimum = new Label();
            txtSimilarIonToleranceHalfWidthMinutes = new TextBox();
            lblSimilarIonTimeToleranceHalfWidth = new Label();
            txtSimilarIonMZToleranceHalfWidth = new TextBox();
            lblSimilarIonMZToleranceHalfWidth = new Label();
            fraBinningMZOptions = new GroupBox();
            txtMaximumBinCount = new TextBox();
            lblMaximumBinCount = new Label();
            txtBinSize = new TextBox();
            lblBinSize = new Label();
            txtBinEndX = new TextBox();
            lblBinEndX = new Label();
            txtBinStartX = new TextBox();
            lblBinStartX = new Label();
            TabPageCustomSICOptions = new TabPage();
            txtCustomSICFileDescription = new TextBox();
            cmdSelectCustomSICFile = new Button();
            txtCustomSICFileName = new TextBox();
            fraCustomSICControls = new GroupBox();
            lblCustomSICToleranceType = new Label();
            optCustomSICScanToleranceAcqTime = new RadioButton();
            optCustomSICScanToleranceRelative = new RadioButton();
            optCustomSICScanToleranceAbsolute = new RadioButton();
            chkLimitSearchToCustomMZs = new CheckBox();
            txtCustomSICScanOrAcqTimeTolerance = new TextBox();
            lblCustomSICScanTolerance = new Label();
            cmdPasteCustomSICList = new Button();
            cmdCustomSICValuesPopulate = new Button();
            cmdClearCustomSICList = new Button();
            dgCustomSICValues = new DataGrid();
            TabPageReporterIons = new TabPage();
            fraDecoyOptions = new GroupBox();
            lblParentIonDecoyMassDaUnits = new Label();
            txtParentIonDecoyMassDa = new TextBox();
            lblParentIonDecoyMassDa = new Label();
            fraMRMOptions = new GroupBox();
            chkMRMWriteIntensityCrosstab = new CheckBox();
            lblMRMInfo = new Label();
            chkMRMWriteDataList = new CheckBox();
            fraReporterIonMassMode = new GroupBox();
            cboReporterIonMassMode = new ComboBox();
            fraReporterIonOptions = new GroupBox();
            chkReporterIonApplyAbundanceCorrection = new CheckBox();
            chkReporterIonSaveUncorrectedIntensities = new CheckBox();
            chkReporterIonSaveObservedMasses = new CheckBox();
            txtReporterIonMZToleranceDa = new TextBox();
            lblReporterIonMZToleranceDa = new Label();
            chkReporterIonStatsEnabled = new CheckBox();
            TabPageAdvancedOptions = new TabPage();
            fraAdditionalInfoFiles = new GroupBox();
            chkConsolidateConstantExtendedHeaderValues = new CheckBox();
            lblStatusLogKeyNameFilterList = new Label();
            txtStatusLogKeyNameFilterList = new TextBox();
            chkSaveExtendedStatsFileIncludeStatusLog = new CheckBox();
            chkSaveExtendedStatsFileIncludeFilterText = new CheckBox();
            chkSaveMSTuneFile = new CheckBox();
            chkSaveMSMethodFile = new CheckBox();
            chkSaveExtendedStatsFile = new CheckBox();
            fraDatasetLookupInfo = new GroupBox();
            cmdSetConnectionStringToPNNLServer = new Button();
            txtDatasetInfoQuerySQL = new TextBox();
            lblDatasetInfoQuerySQL = new Label();
            txtDatabaseConnectionString = new TextBox();
            lblDatabaseConnectionString = new Label();
            lblDatasetLookupFilePath = new Label();
            cmdSelectDatasetLookupFile = new Button();
            txtDatasetLookupFilePath = new TextBox();
            fraMemoryConservationOptions = new GroupBox();
            chkSkipMSMSProcessing = new CheckBox();
            chkSkipSICAndRawDataProcessing = new CheckBox();
            chkExportRawDataOnly = new CheckBox();
            TabPageLog = new TabPage();
            txtLogMessages = new TextBox();
            fraOutputDirectoryPath = new GroupBox();
            cmdStartProcessing = new Button();
            cmdSelectOutputDirectory = new Button();
            txtOutputDirectoryPath = new TextBox();
            fraInputFilePath.SuspendLayout();
            tbsOptions.SuspendLayout();
            TabPageMasicExportOptions.SuspendLayout();
            fraExportAllSpectraDataPoints.SuspendLayout();
            TabPageSICOptions.SuspendLayout();
            fraInputFileRangeFilters.SuspendLayout();
            fraSICSearchThresholds.SuspendLayout();
            TabPagePeakFindingOptions.SuspendLayout();
            fraSICNoiseThresholds.SuspendLayout();
            fraSmoothingOptions.SuspendLayout();
            fraPeakFindingOptions.SuspendLayout();
            TabPageBinningAndSimilarityOptions.SuspendLayout();
            fraMassSpectraNoiseThresholds.SuspendLayout();
            fraBinningIntensityOptions.SuspendLayout();
            fraSpectrumSimilarityOptions.SuspendLayout();
            fraBinningMZOptions.SuspendLayout();
            TabPageCustomSICOptions.SuspendLayout();
            fraCustomSICControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgCustomSICValues).BeginInit();
            TabPageReporterIons.SuspendLayout();
            fraDecoyOptions.SuspendLayout();
            fraMRMOptions.SuspendLayout();
            fraReporterIonMassMode.SuspendLayout();
            fraReporterIonOptions.SuspendLayout();
            TabPageAdvancedOptions.SuspendLayout();
            fraAdditionalInfoFiles.SuspendLayout();
            fraDatasetLookupInfo.SuspendLayout();
            fraMemoryConservationOptions.SuspendLayout();
            TabPageLog.SuspendLayout();
            fraOutputDirectoryPath.SuspendLayout();
            SuspendLayout();
            //
            // txtInputFilePath
            //
            txtInputFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtInputFilePath.Location = new System.Drawing.Point(125, 30);
            txtInputFilePath.Name = "txtInputFilePath";
            txtInputFilePath.Size = new System.Drawing.Size(741, 22);
            txtInputFilePath.TabIndex = 1;
            //
            // cmdSelectFile
            //
            cmdSelectFile.Location = new System.Drawing.Point(10, 28);
            cmdSelectFile.Name = "cmdSelectFile";
            cmdSelectFile.Size = new System.Drawing.Size(96, 27);
            cmdSelectFile.TabIndex = 0;
            cmdSelectFile.Text = "&Select File";
            cmdSelectFile.Click += new EventHandler(cmdSelectFile_Click);
            //
            // fraInputFilePath
            //
            fraInputFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            fraInputFilePath.Controls.Add(cmdSelectFile);
            fraInputFilePath.Controls.Add(txtInputFilePath);
            fraInputFilePath.Location = new System.Drawing.Point(10, 9);
            fraInputFilePath.Name = "fraInputFilePath";
            fraInputFilePath.Size = new System.Drawing.Size(885, 74);
            fraInputFilePath.TabIndex = 0;
            fraInputFilePath.TabStop = false;
            fraInputFilePath.Text = "Input File Path (Thermo .Raw or Agilent .CDF/.MGF combo or mzXML or mz" + "Data)";
            //
            // MainMenuControl
            //
            MainMenuControl.MenuItems.AddRange(new MenuItem[] { mnuFile, mnuEdit, mnuHelp });
            //
            // mnuFile
            //
            mnuFile.Index = 0;
            mnuFile.MenuItems.AddRange(new MenuItem[] { mnuFileSelectInputFile, mnuFileSelectOutputDirectory, mnuFileSep1, mnuFileLoadOptions, mnuFileSaveOptions, mnuFileSep2, mnuFileExit });
            mnuFile.Text = "&File";
            //
            // mnuFileSelectInputFile
            //
            mnuFileSelectInputFile.Index = 0;
            mnuFileSelectInputFile.Text = "&Select Input File ...";
            mnuFileSelectInputFile.Click += new EventHandler(mnuFileSelectInputFile_Click);
            //
            // mnuFileSelectOutputDirectory
            //
            mnuFileSelectOutputDirectory.Index = 1;
            mnuFileSelectOutputDirectory.Text = "Select Output &Directory ...";
            mnuFileSelectOutputDirectory.Click += new EventHandler(mnuFileSelectOutputDirectory_Click);
            //
            // mnuFileSep1
            //
            mnuFileSep1.Index = 2;
            mnuFileSep1.Text = "-";
            //
            // mnuFileLoadOptions
            //
            mnuFileLoadOptions.Index = 3;
            mnuFileLoadOptions.Text = "&Load Options ...";
            mnuFileLoadOptions.Click += new EventHandler(mnuFileLoadOptions_Click);
            //
            // mnuFileSaveOptions
            //
            mnuFileSaveOptions.Index = 4;
            mnuFileSaveOptions.Text = "Sa&ve Options ...";
            mnuFileSaveOptions.Click += new EventHandler(mnuFileSaveOptions_Click);
            //
            // mnuFileSep2
            //
            mnuFileSep2.Index = 5;
            mnuFileSep2.Text = "-";
            //
            // mnuFileExit
            //
            mnuFileExit.Index = 6;
            mnuFileExit.Text = "E&xit";
            mnuFileExit.Click += new EventHandler(mnuFileExit_Click);
            //
            // mnuEdit
            //
            mnuEdit.Index = 1;
            mnuEdit.MenuItems.AddRange(new MenuItem[] { mnuEditProcessFile, mnuEditSep1, mnuEditSaveDefaultOptions, mnuEditResetOptions });
            mnuEdit.Text = "&Edit";
            //
            // mnuEditProcessFile
            //
            mnuEditProcessFile.Index = 0;
            mnuEditProcessFile.Text = "&Process File";
            mnuEditProcessFile.Click += new EventHandler(mnuEditProcessFile_Click);
            //
            // mnuEditSep1
            //
            mnuEditSep1.Index = 1;
            mnuEditSep1.Text = "-";
            //
            // mnuEditSaveDefaultOptions
            //
            mnuEditSaveDefaultOptions.Index = 2;
            mnuEditSaveDefaultOptions.Text = "&Save current options as Default ...";
            mnuEditSaveDefaultOptions.Click += new EventHandler(mnuEditSaveDefaultOptions_Click);
            //
            // mnuEditResetOptions
            //
            mnuEditResetOptions.Index = 3;
            mnuEditResetOptions.Text = "&Reset options to Defaults";
            mnuEditResetOptions.Click += new EventHandler(mnuEditResetOptions_Click);
            //
            // mnuHelp
            //
            mnuHelp.Index = 2;
            mnuHelp.MenuItems.AddRange(new MenuItem[] { mnuHelpAbout });
            mnuHelp.Text = "&Help";
            //
            // mnuHelpAbout
            //
            mnuHelpAbout.Index = 0;
            mnuHelpAbout.Text = "&About";
            mnuHelpAbout.Click += new EventHandler(mnuHelpAbout_Click);
            //
            // tbsOptions
            //
            tbsOptions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbsOptions.Controls.Add(TabPageMasicExportOptions);
            tbsOptions.Controls.Add(TabPageSICOptions);
            tbsOptions.Controls.Add(TabPagePeakFindingOptions);
            tbsOptions.Controls.Add(TabPageBinningAndSimilarityOptions);
            tbsOptions.Controls.Add(TabPageCustomSICOptions);
            tbsOptions.Controls.Add(TabPageReporterIons);
            tbsOptions.Controls.Add(TabPageAdvancedOptions);
            tbsOptions.Controls.Add(TabPageLog);
            tbsOptions.Location = new System.Drawing.Point(10, 203);
            tbsOptions.Name = "tbsOptions";
            tbsOptions.SelectedIndex = 0;
            tbsOptions.Size = new System.Drawing.Size(890, 356);
            tbsOptions.TabIndex = 2;
            //
            // TabPageMasicExportOptions
            //
            TabPageMasicExportOptions.Controls.Add(chkWriteDetailedSICDataFile);
            TabPageMasicExportOptions.Controls.Add(chkIncludeScanTimesInSICStatsFile);
            TabPageMasicExportOptions.Controls.Add(txtDatasetID);
            TabPageMasicExportOptions.Controls.Add(lblDatasetID);
            TabPageMasicExportOptions.Controls.Add(lblRawDataExportOverview);
            TabPageMasicExportOptions.Controls.Add(fraExportAllSpectraDataPoints);
            TabPageMasicExportOptions.Controls.Add(chkIncludeHeaders);
            TabPageMasicExportOptions.Location = new System.Drawing.Point(4, 25);
            TabPageMasicExportOptions.Name = "TabPageMasicExportOptions";
            TabPageMasicExportOptions.Size = new System.Drawing.Size(882, 327);
            TabPageMasicExportOptions.TabIndex = 4;
            TabPageMasicExportOptions.Text = "Export Options";
            TabPageMasicExportOptions.UseVisualStyleBackColor = true;
            //
            // chkWriteDetailedSICDataFile
            //
            chkWriteDetailedSICDataFile.Location = new System.Drawing.Point(19, 62);
            chkWriteDetailedSICDataFile.Name = "chkWriteDetailedSICDataFile";
            chkWriteDetailedSICDataFile.Size = new System.Drawing.Size(250, 19);
            chkWriteDetailedSICDataFile.TabIndex = 7;
            chkWriteDetailedSICDataFile.Text = "Write detailed SIC data file";
            //
            // chkIncludeScanTimesInSICStatsFile
            //
            chkIncludeScanTimesInSICStatsFile.Checked = true;
            chkIncludeScanTimesInSICStatsFile.CheckState = CheckState.Checked;
            chkIncludeScanTimesInSICStatsFile.Location = new System.Drawing.Point(19, 37);
            chkIncludeScanTimesInSICStatsFile.Name = "chkIncludeScanTimesInSICStatsFile";
            chkIncludeScanTimesInSICStatsFile.Size = new System.Drawing.Size(250, 18);
            chkIncludeScanTimesInSICStatsFile.TabIndex = 6;
            chkIncludeScanTimesInSICStatsFile.Text = "Include scan times in SIC stats file";
            //
            // txtDatasetID
            //
            txtDatasetID.Location = new System.Drawing.Point(605, 18);
            txtDatasetID.Name = "txtDatasetID";
            txtDatasetID.Size = new System.Drawing.Size(105, 22);
            txtDatasetID.TabIndex = 4;
            txtDatasetID.Text = "0";
            txtDatasetID.KeyPress += new KeyPressEventHandler(txtDatasetID_KeyPress);
            //
            // lblDatasetID
            //
            lblDatasetID.Location = new System.Drawing.Point(432, 18);
            lblDatasetID.Name = "lblDatasetID";
            lblDatasetID.Size = new System.Drawing.Size(182, 19);
            lblDatasetID.TabIndex = 3;
            lblDatasetID.Text = "Input File Dataset Number";
            //
            // lblRawDataExportOverview
            //
            lblRawDataExportOverview.Location = new System.Drawing.Point(442, 55);
            lblRawDataExportOverview.Name = "lblRawDataExportOverview";
            lblRawDataExportOverview.Size = new System.Drawing.Size(307, 268);
            lblRawDataExportOverview.TabIndex = 5;
            lblRawDataExportOverview.Text = "Raw Data Export Options Overview";
            //
            // fraExportAllSpectraDataPoints
            //
            fraExportAllSpectraDataPoints.Controls.Add(txtExportRawDataSignalToNoiseRatioMinimum);
            fraExportAllSpectraDataPoints.Controls.Add(lblExportRawDataSignalToNoiseRatioMinimum);
            fraExportAllSpectraDataPoints.Controls.Add(chkExportRawDataRenumberScans);
            fraExportAllSpectraDataPoints.Controls.Add(txtExportRawDataMaxIonCountPerScan);
            fraExportAllSpectraDataPoints.Controls.Add(lblExportRawDataMaxIonCountPerScan);
            fraExportAllSpectraDataPoints.Controls.Add(txtExportRawDataIntensityMinimum);
            fraExportAllSpectraDataPoints.Controls.Add(lblExportRawDataIntensityMinimum);
            fraExportAllSpectraDataPoints.Controls.Add(chkExportRawDataIncludeMSMS);
            fraExportAllSpectraDataPoints.Controls.Add(cboExportRawDataFileFormat);
            fraExportAllSpectraDataPoints.Controls.Add(lblExportDataPointsFormat);
            fraExportAllSpectraDataPoints.Controls.Add(chkExportRawSpectraData);
            fraExportAllSpectraDataPoints.Location = new System.Drawing.Point(19, 93);
            fraExportAllSpectraDataPoints.Name = "fraExportAllSpectraDataPoints";
            fraExportAllSpectraDataPoints.Size = new System.Drawing.Size(413, 222);
            fraExportAllSpectraDataPoints.TabIndex = 2;
            fraExportAllSpectraDataPoints.TabStop = false;
            fraExportAllSpectraDataPoints.Text = "Raw Data Point Export Options";
            //
            // txtExportRawDataSignalToNoiseRatioMinimum
            //
            txtExportRawDataSignalToNoiseRatioMinimum.Location = new System.Drawing.Point(240, 129);
            txtExportRawDataSignalToNoiseRatioMinimum.Name = "txtExportRawDataSignalToNoiseRatioMinimum";
            txtExportRawDataSignalToNoiseRatioMinimum.Size = new System.Drawing.Size(48, 22);
            txtExportRawDataSignalToNoiseRatioMinimum.TabIndex = 6;
            txtExportRawDataSignalToNoiseRatioMinimum.Text = "1";
            txtExportRawDataSignalToNoiseRatioMinimum.KeyPress += new KeyPressEventHandler(txtExportRawDataSignalToNoiseRatioMinimum_KeyPress);
            //
            // lblExportRawDataSignalToNoiseRatioMinimum
            //
            lblExportRawDataSignalToNoiseRatioMinimum.Location = new System.Drawing.Point(19, 132);
            lblExportRawDataSignalToNoiseRatioMinimum.Name = "lblExportRawDataSignalToNoiseRatioMinimum";
            lblExportRawDataSignalToNoiseRatioMinimum.Size = new System.Drawing.Size(211, 18);
            lblExportRawDataSignalToNoiseRatioMinimum.TabIndex = 5;
            lblExportRawDataSignalToNoiseRatioMinimum.Text = "Minimum Signal to Noise Ratio";
            //
            // chkExportRawDataRenumberScans
            //
            chkExportRawDataRenumberScans.Location = new System.Drawing.Point(19, 76);
            chkExportRawDataRenumberScans.Name = "chkExportRawDataRenumberScans";
            chkExportRawDataRenumberScans.Size = new System.Drawing.Size(375, 19);
            chkExportRawDataRenumberScans.TabIndex = 3;
            chkExportRawDataRenumberScans.Text = "Renumber survey scan spectra to make sequential";
            //
            // txtExportRawDataMaxIonCountPerScan
            //
            txtExportRawDataMaxIonCountPerScan.Location = new System.Drawing.Point(240, 157);
            txtExportRawDataMaxIonCountPerScan.Name = "txtExportRawDataMaxIonCountPerScan";
            txtExportRawDataMaxIonCountPerScan.Size = new System.Drawing.Size(67, 22);
            txtExportRawDataMaxIonCountPerScan.TabIndex = 8;
            txtExportRawDataMaxIonCountPerScan.Text = "200";
            txtExportRawDataMaxIonCountPerScan.KeyPress += new KeyPressEventHandler(txtExportRawDataMaxIonCountPerScan_KeyPress);
            //
            // lblExportRawDataMaxIonCountPerScan
            //
            lblExportRawDataMaxIonCountPerScan.Location = new System.Drawing.Point(19, 159);
            lblExportRawDataMaxIonCountPerScan.Name = "lblExportRawDataMaxIonCountPerScan";
            lblExportRawDataMaxIonCountPerScan.Size = new System.Drawing.Size(221, 19);
            lblExportRawDataMaxIonCountPerScan.TabIndex = 7;
            lblExportRawDataMaxIonCountPerScan.Text = "Maximum Ion Count per Scan";
            //
            // txtExportRawDataIntensityMinimum
            //
            txtExportRawDataIntensityMinimum.Location = new System.Drawing.Point(240, 185);
            txtExportRawDataIntensityMinimum.Name = "txtExportRawDataIntensityMinimum";
            txtExportRawDataIntensityMinimum.Size = new System.Drawing.Size(106, 22);
            txtExportRawDataIntensityMinimum.TabIndex = 10;
            txtExportRawDataIntensityMinimum.Text = "0";
            txtExportRawDataIntensityMinimum.KeyPress += new KeyPressEventHandler(txtExportRawDataIntensityMinimum_KeyPress);
            //
            // lblExportRawDataIntensityMinimum
            //
            lblExportRawDataIntensityMinimum.Location = new System.Drawing.Point(19, 187);
            lblExportRawDataIntensityMinimum.Name = "lblExportRawDataIntensityMinimum";
            lblExportRawDataIntensityMinimum.Size = new System.Drawing.Size(183, 18);
            lblExportRawDataIntensityMinimum.TabIndex = 9;
            lblExportRawDataIntensityMinimum.Text = "Minimum Intensity (counts)";
            //
            // chkExportRawDataIncludeMSMS
            //
            chkExportRawDataIncludeMSMS.Location = new System.Drawing.Point(19, 99);
            chkExportRawDataIncludeMSMS.Name = "chkExportRawDataIncludeMSMS";
            chkExportRawDataIncludeMSMS.Size = new System.Drawing.Size(384, 19);
            chkExportRawDataIncludeMSMS.TabIndex = 4;
            chkExportRawDataIncludeMSMS.Text = "Export MS/MS Spectra, in addition to survey scan spectra";
            chkExportRawDataIncludeMSMS.CheckedChanged += new EventHandler(chkExportRawDataIncludeMSMS_CheckedChanged);
            //
            // cboExportRawDataFileFormat
            //
            cboExportRawDataFileFormat.DropDownStyle = ComboBoxStyle.DropDownList;
            cboExportRawDataFileFormat.Location = new System.Drawing.Point(106, 46);
            cboExportRawDataFileFormat.Name = "cboExportRawDataFileFormat";
            cboExportRawDataFileFormat.Size = new System.Drawing.Size(172, 24);
            cboExportRawDataFileFormat.TabIndex = 2;
            //
            // lblExportDataPointsFormat
            //
            lblExportDataPointsFormat.Location = new System.Drawing.Point(38, 51);
            lblExportDataPointsFormat.Name = "lblExportDataPointsFormat";
            lblExportDataPointsFormat.Size = new System.Drawing.Size(87, 18);
            lblExportDataPointsFormat.TabIndex = 1;
            lblExportDataPointsFormat.Text = "Format:";
            //
            // chkExportRawSpectraData
            //
            chkExportRawSpectraData.Location = new System.Drawing.Point(19, 28);
            chkExportRawSpectraData.Name = "chkExportRawSpectraData";
            chkExportRawSpectraData.Size = new System.Drawing.Size(288, 18);
            chkExportRawSpectraData.TabIndex = 0;
            chkExportRawSpectraData.Text = "Export All Spectra Data Points";
            chkExportRawSpectraData.CheckedChanged += new EventHandler(chkExportRawSpectraData_CheckedChanged);
            //
            // chkIncludeHeaders
            //
            chkIncludeHeaders.Checked = true;
            chkIncludeHeaders.CheckState = CheckState.Checked;
            chkIncludeHeaders.Location = new System.Drawing.Point(19, 12);
            chkIncludeHeaders.Name = "chkIncludeHeaders";
            chkIncludeHeaders.Size = new System.Drawing.Size(192, 18);
            chkIncludeHeaders.TabIndex = 0;
            chkIncludeHeaders.Text = "Include Column Headers";
            //
            // TabPageSICOptions
            //
            TabPageSICOptions.Controls.Add(fraInputFileRangeFilters);
            TabPageSICOptions.Controls.Add(lblSICOptionsOverview);
            TabPageSICOptions.Controls.Add(fraSICSearchThresholds);
            TabPageSICOptions.Location = new System.Drawing.Point(4, 25);
            TabPageSICOptions.Name = "TabPageSICOptions";
            TabPageSICOptions.Size = new System.Drawing.Size(882, 327);
            TabPageSICOptions.TabIndex = 5;
            TabPageSICOptions.Text = "SIC Options";
            TabPageSICOptions.UseVisualStyleBackColor = true;
            //
            // fraInputFileRangeFilters
            //
            fraInputFileRangeFilters.Controls.Add(lblTimeEndUnits);
            fraInputFileRangeFilters.Controls.Add(lblTimeStartUnits);
            fraInputFileRangeFilters.Controls.Add(txtTimeEnd);
            fraInputFileRangeFilters.Controls.Add(txtTimeStart);
            fraInputFileRangeFilters.Controls.Add(lblTimeEnd);
            fraInputFileRangeFilters.Controls.Add(lblTimeStart);
            fraInputFileRangeFilters.Controls.Add(txtScanEnd);
            fraInputFileRangeFilters.Controls.Add(txtScanStart);
            fraInputFileRangeFilters.Controls.Add(lblScanEnd);
            fraInputFileRangeFilters.Controls.Add(lblScanStart);
            fraInputFileRangeFilters.Controls.Add(cmdClearAllRangeFilters);
            fraInputFileRangeFilters.Location = new System.Drawing.Point(19, 232);
            fraInputFileRangeFilters.Name = "fraInputFileRangeFilters";
            fraInputFileRangeFilters.Size = new System.Drawing.Size(586, 82);
            fraInputFileRangeFilters.TabIndex = 1;
            fraInputFileRangeFilters.TabStop = false;
            fraInputFileRangeFilters.Text = "Input File Range Filters";
            //
            // lblTimeEndUnits
            //
            lblTimeEndUnits.Location = new System.Drawing.Point(499, 52);
            lblTimeEndUnits.Name = "lblTimeEndUnits";
            lblTimeEndUnits.Size = new System.Drawing.Size(77, 18);
            lblTimeEndUnits.TabIndex = 10;
            lblTimeEndUnits.Text = "minutes";
            //
            // lblTimeStartUnits
            //
            lblTimeStartUnits.Location = new System.Drawing.Point(499, 24);
            lblTimeStartUnits.Name = "lblTimeStartUnits";
            lblTimeStartUnits.Size = new System.Drawing.Size(77, 18);
            lblTimeStartUnits.TabIndex = 7;
            lblTimeStartUnits.Text = "minutes";
            //
            // txtTimeEnd
            //
            txtTimeEnd.Location = new System.Drawing.Point(422, 49);
            txtTimeEnd.Name = "txtTimeEnd";
            txtTimeEnd.Size = new System.Drawing.Size(68, 22);
            txtTimeEnd.TabIndex = 9;
            txtTimeEnd.Text = "0";
            txtTimeEnd.KeyPress += new KeyPressEventHandler(txtTimeEnd_KeyPress);
            //
            // txtTimeStart
            //
            txtTimeStart.Location = new System.Drawing.Point(422, 22);
            txtTimeStart.Name = "txtTimeStart";
            txtTimeStart.Size = new System.Drawing.Size(68, 22);
            txtTimeStart.TabIndex = 6;
            txtTimeStart.Text = "0";
            txtTimeStart.KeyPress += new KeyPressEventHandler(txtTimeStart_KeyPress);
            //
            // lblTimeEnd
            //
            lblTimeEnd.Location = new System.Drawing.Point(336, 52);
            lblTimeEnd.Name = "lblTimeEnd";
            lblTimeEnd.Size = new System.Drawing.Size(86, 18);
            lblTimeEnd.TabIndex = 8;
            lblTimeEnd.Text = "End Time";
            //
            // lblTimeStart
            //
            lblTimeStart.Location = new System.Drawing.Point(336, 24);
            lblTimeStart.Name = "lblTimeStart";
            lblTimeStart.Size = new System.Drawing.Size(77, 18);
            lblTimeStart.TabIndex = 5;
            lblTimeStart.Text = "Start Time";
            //
            // txtScanEnd
            //
            txtScanEnd.Location = new System.Drawing.Point(230, 49);
            txtScanEnd.Name = "txtScanEnd";
            txtScanEnd.Size = new System.Drawing.Size(68, 22);
            txtScanEnd.TabIndex = 4;
            txtScanEnd.Text = "0";
            txtScanEnd.KeyPress += new KeyPressEventHandler(txtScanEnd_KeyPress);
            //
            // txtScanStart
            //
            txtScanStart.Location = new System.Drawing.Point(230, 22);
            txtScanStart.Name = "txtScanStart";
            txtScanStart.Size = new System.Drawing.Size(68, 22);
            txtScanStart.TabIndex = 2;
            txtScanStart.Text = "0";
            txtScanStart.KeyPress += new KeyPressEventHandler(txtScanStart_KeyPress);
            //
            // lblScanEnd
            //
            lblScanEnd.Location = new System.Drawing.Point(144, 52);
            lblScanEnd.Name = "lblScanEnd";
            lblScanEnd.Size = new System.Drawing.Size(86, 18);
            lblScanEnd.TabIndex = 3;
            lblScanEnd.Text = "End Scan";
            //
            // lblScanStart
            //
            lblScanStart.Location = new System.Drawing.Point(144, 24);
            lblScanStart.Name = "lblScanStart";
            lblScanStart.Size = new System.Drawing.Size(77, 18);
            lblScanStart.TabIndex = 1;
            lblScanStart.Text = "Start Scan";
            //
            // cmdClearAllRangeFilters
            //
            cmdClearAllRangeFilters.Location = new System.Drawing.Point(19, 31);
            cmdClearAllRangeFilters.Name = "cmdClearAllRangeFilters";
            cmdClearAllRangeFilters.Size = new System.Drawing.Size(106, 28);
            cmdClearAllRangeFilters.TabIndex = 0;
            cmdClearAllRangeFilters.Text = "Clear Filters";
            cmdClearAllRangeFilters.Click += new EventHandler(cmdClearAllRangeFilters_Click);
            //
            // lblSICOptionsOverview
            //
            lblSICOptionsOverview.Location = new System.Drawing.Point(374, 28);
            lblSICOptionsOverview.Name = "lblSICOptionsOverview";
            lblSICOptionsOverview.Size = new System.Drawing.Size(426, 175);
            lblSICOptionsOverview.TabIndex = 2;
            lblSICOptionsOverview.Text = "SIC Options Overview";
            //
            // fraSICSearchThresholds
            //
            fraSICSearchThresholds.Controls.Add(optSICTolerancePPM);
            fraSICSearchThresholds.Controls.Add(optSICToleranceDa);
            fraSICSearchThresholds.Controls.Add(chkRefineReportedParentIonMZ);
            fraSICSearchThresholds.Controls.Add(chkReplaceSICZeroesWithMinimumPositiveValueFromMSData);
            fraSICSearchThresholds.Controls.Add(txtMaxPeakWidthMinutesForward);
            fraSICSearchThresholds.Controls.Add(txtMaxPeakWidthMinutesBackward);
            fraSICSearchThresholds.Controls.Add(txtIntensityThresholdFractionMax);
            fraSICSearchThresholds.Controls.Add(lblIntensityThresholdFractionMax);
            fraSICSearchThresholds.Controls.Add(txtIntensityThresholdAbsoluteMinimum);
            fraSICSearchThresholds.Controls.Add(lblIntensityThresholdAbsoluteMinimum);
            fraSICSearchThresholds.Controls.Add(lblMaxPeakWidthMinutesForward);
            fraSICSearchThresholds.Controls.Add(lblMaxPeakWidthMinutesBackward);
            fraSICSearchThresholds.Controls.Add(lblMaxPeakWidthMinutes);
            fraSICSearchThresholds.Controls.Add(txtSICTolerance);
            fraSICSearchThresholds.Controls.Add(lblSICToleranceDa);
            fraSICSearchThresholds.Location = new System.Drawing.Point(19, 9);
            fraSICSearchThresholds.Name = "fraSICSearchThresholds";
            fraSICSearchThresholds.Size = new System.Drawing.Size(336, 217);
            fraSICSearchThresholds.TabIndex = 0;
            fraSICSearchThresholds.TabStop = false;
            fraSICSearchThresholds.Text = "SIC Search Thresholds";
            //
            // optSICTolerancePPM
            //
            optSICTolerancePPM.Location = new System.Drawing.Point(230, 32);
            optSICTolerancePPM.Name = "optSICTolerancePPM";
            optSICTolerancePPM.Size = new System.Drawing.Size(87, 21);
            optSICTolerancePPM.TabIndex = 14;
            optSICTolerancePPM.Text = "ppm";
            //
            // optSICToleranceDa
            //
            optSICToleranceDa.Checked = true;
            optSICToleranceDa.Location = new System.Drawing.Point(230, 12);
            optSICToleranceDa.Name = "optSICToleranceDa";
            optSICToleranceDa.Size = new System.Drawing.Size(87, 20);
            optSICToleranceDa.TabIndex = 13;
            optSICToleranceDa.TabStop = true;
            optSICToleranceDa.Text = "Da";
            //
            // chkRefineReportedParentIonMZ
            //
            chkRefineReportedParentIonMZ.Location = new System.Drawing.Point(10, 183);
            chkRefineReportedParentIonMZ.Name = "chkRefineReportedParentIonMZ";
            chkRefineReportedParentIonMZ.Size = new System.Drawing.Size(316, 20);
            chkRefineReportedParentIonMZ.TabIndex = 12;
            chkRefineReportedParentIonMZ.Text = "Refine reported parent ion m/z values";
            //
            // chkReplaceSICZeroesWithMinimumPositiveValueFromMSData
            //
            chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Checked = true;
            chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.CheckState = CheckState.Checked;
            chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Location = new System.Drawing.Point(10, 157);
            chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Name = "chkReplaceSICZeroesWithMinimumPositiveValueFromMSData";
            chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Size = new System.Drawing.Size(316, 20);
            chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.TabIndex = 11;
            chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Text = "Replace SIC zeroes with min MS data values";
            //
            // txtMaxPeakWidthMinutesForward
            //
            txtMaxPeakWidthMinutesForward.Location = new System.Drawing.Point(250, 65);
            txtMaxPeakWidthMinutesForward.Name = "txtMaxPeakWidthMinutesForward";
            txtMaxPeakWidthMinutesForward.Size = new System.Drawing.Size(67, 22);
            txtMaxPeakWidthMinutesForward.TabIndex = 6;
            txtMaxPeakWidthMinutesForward.Text = "3";
            txtMaxPeakWidthMinutesForward.KeyPress += new KeyPressEventHandler(txtMaxPeakWidthMinutesForward_KeyPress);
            //
            // txtMaxPeakWidthMinutesBackward
            //
            txtMaxPeakWidthMinutesBackward.Location = new System.Drawing.Point(96, 65);
            txtMaxPeakWidthMinutesBackward.Name = "txtMaxPeakWidthMinutesBackward";
            txtMaxPeakWidthMinutesBackward.Size = new System.Drawing.Size(67, 22);
            txtMaxPeakWidthMinutesBackward.TabIndex = 4;
            txtMaxPeakWidthMinutesBackward.Text = "3";
            txtMaxPeakWidthMinutesBackward.KeyPress += new KeyPressEventHandler(txtMaxPeakWidthMinutesBackward_KeyPress);
            //
            // txtIntensityThresholdFractionMax
            //
            txtIntensityThresholdFractionMax.Location = new System.Drawing.Point(250, 92);
            txtIntensityThresholdFractionMax.Name = "txtIntensityThresholdFractionMax";
            txtIntensityThresholdFractionMax.Size = new System.Drawing.Size(67, 22);
            txtIntensityThresholdFractionMax.TabIndex = 8;
            txtIntensityThresholdFractionMax.Text = "0.01";
            txtIntensityThresholdFractionMax.KeyPress += new KeyPressEventHandler(txtIntensityThresholdFractionMax_KeyPress);
            //
            // lblIntensityThresholdFractionMax
            //
            lblIntensityThresholdFractionMax.Location = new System.Drawing.Point(10, 92);
            lblIntensityThresholdFractionMax.Name = "lblIntensityThresholdFractionMax";
            lblIntensityThresholdFractionMax.Size = new System.Drawing.Size(240, 19);
            lblIntensityThresholdFractionMax.TabIndex = 7;
            lblIntensityThresholdFractionMax.Text = "Intensity Threshold Fraction Max Peak";
            //
            // txtIntensityThresholdAbsoluteMinimum
            //
            txtIntensityThresholdAbsoluteMinimum.Location = new System.Drawing.Point(250, 120);
            txtIntensityThresholdAbsoluteMinimum.Name = "txtIntensityThresholdAbsoluteMinimum";
            txtIntensityThresholdAbsoluteMinimum.Size = new System.Drawing.Size(67, 22);
            txtIntensityThresholdAbsoluteMinimum.TabIndex = 10;
            txtIntensityThresholdAbsoluteMinimum.Text = "0";
            txtIntensityThresholdAbsoluteMinimum.KeyPress += new KeyPressEventHandler(txtIntensityThresholdAbsoluteMinimum_KeyPress);
            //
            // lblIntensityThresholdAbsoluteMinimum
            //
            lblIntensityThresholdAbsoluteMinimum.Location = new System.Drawing.Point(10, 120);
            lblIntensityThresholdAbsoluteMinimum.Name = "lblIntensityThresholdAbsoluteMinimum";
            lblIntensityThresholdAbsoluteMinimum.Size = new System.Drawing.Size(240, 18);
            lblIntensityThresholdAbsoluteMinimum.TabIndex = 9;
            lblIntensityThresholdAbsoluteMinimum.Text = "Intensity Threshold Absolute Minimum";
            //
            // lblMaxPeakWidthMinutesForward
            //
            lblMaxPeakWidthMinutesForward.Location = new System.Drawing.Point(182, 65);
            lblMaxPeakWidthMinutesForward.Name = "lblMaxPeakWidthMinutesForward";
            lblMaxPeakWidthMinutesForward.Size = new System.Drawing.Size(77, 18);
            lblMaxPeakWidthMinutesForward.TabIndex = 5;
            lblMaxPeakWidthMinutesForward.Text = "Forward";
            //
            // lblMaxPeakWidthMinutesBackward
            //
            lblMaxPeakWidthMinutesBackward.Location = new System.Drawing.Point(19, 65);
            lblMaxPeakWidthMinutesBackward.Name = "lblMaxPeakWidthMinutesBackward";
            lblMaxPeakWidthMinutesBackward.Size = new System.Drawing.Size(77, 18);
            lblMaxPeakWidthMinutesBackward.TabIndex = 3;
            lblMaxPeakWidthMinutesBackward.Text = "Backward";
            //
            // lblMaxPeakWidthMinutes
            //
            lblMaxPeakWidthMinutes.Location = new System.Drawing.Point(10, 46);
            lblMaxPeakWidthMinutes.Name = "lblMaxPeakWidthMinutes";
            lblMaxPeakWidthMinutes.Size = new System.Drawing.Size(201, 19);
            lblMaxPeakWidthMinutes.TabIndex = 2;
            lblMaxPeakWidthMinutes.Text = "Maximum Peak Width (minutes)";
            //
            // txtSICTolerance
            //
            txtSICTolerance.Location = new System.Drawing.Point(154, 18);
            txtSICTolerance.Name = "txtSICTolerance";
            txtSICTolerance.Size = new System.Drawing.Size(57, 22);
            txtSICTolerance.TabIndex = 1;
            txtSICTolerance.Text = "0.60";
            txtSICTolerance.KeyPress += new KeyPressEventHandler(txtSICTolerance_KeyPress);
            //
            // lblSICToleranceDa
            //
            lblSICToleranceDa.Location = new System.Drawing.Point(10, 18);
            lblSICToleranceDa.Name = "lblSICToleranceDa";
            lblSICToleranceDa.Size = new System.Drawing.Size(144, 19);
            lblSICToleranceDa.TabIndex = 0;
            lblSICToleranceDa.Text = "SIC Tolerance (Da)";
            //
            // TabPagePeakFindingOptions
            //
            TabPagePeakFindingOptions.Controls.Add(fraSICNoiseThresholds);
            TabPagePeakFindingOptions.Controls.Add(fraSmoothingOptions);
            TabPagePeakFindingOptions.Controls.Add(fraPeakFindingOptions);
            TabPagePeakFindingOptions.Location = new System.Drawing.Point(4, 25);
            TabPagePeakFindingOptions.Name = "TabPagePeakFindingOptions";
            TabPagePeakFindingOptions.Size = new System.Drawing.Size(882, 327);
            TabPagePeakFindingOptions.TabIndex = 7;
            TabPagePeakFindingOptions.Text = "Peak Finding Options";
            TabPagePeakFindingOptions.UseVisualStyleBackColor = true;
            //
            // fraSICNoiseThresholds
            //
            fraSICNoiseThresholds.Controls.Add(cboSICNoiseThresholdMode);
            fraSICNoiseThresholds.Controls.Add(lblNoiseThresholdMode);
            fraSICNoiseThresholds.Controls.Add(txtSICNoiseFractionLowIntensityDataToAverage);
            fraSICNoiseThresholds.Controls.Add(lblSICNoiseFractionLowIntensityDataToAverage);
            fraSICNoiseThresholds.Controls.Add(txtSICNoiseThresholdIntensity);
            fraSICNoiseThresholds.Controls.Add(lblSICNoiseThresholdIntensity);
            fraSICNoiseThresholds.Location = new System.Drawing.Point(19, 9);
            fraSICNoiseThresholds.Name = "fraSICNoiseThresholds";
            fraSICNoiseThresholds.Size = new System.Drawing.Size(384, 148);
            fraSICNoiseThresholds.TabIndex = 0;
            fraSICNoiseThresholds.TabStop = false;
            fraSICNoiseThresholds.Text = "Initial Noise Threshold Determination for SICs";
            //
            // cboSICNoiseThresholdMode
            //
            cboSICNoiseThresholdMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSICNoiseThresholdMode.Location = new System.Drawing.Point(144, 28);
            cboSICNoiseThresholdMode.Name = "cboSICNoiseThresholdMode";
            cboSICNoiseThresholdMode.Size = new System.Drawing.Size(211, 24);
            cboSICNoiseThresholdMode.TabIndex = 1;
            cboSICNoiseThresholdMode.SelectedIndexChanged += new EventHandler(cboSICNoiseThresholdMode_SelectedIndexChanged);
            //
            // lblNoiseThresholdMode
            //
            lblNoiseThresholdMode.Location = new System.Drawing.Point(19, 30);
            lblNoiseThresholdMode.Name = "lblNoiseThresholdMode";
            lblNoiseThresholdMode.Size = new System.Drawing.Size(115, 18);
            lblNoiseThresholdMode.TabIndex = 0;
            lblNoiseThresholdMode.Text = "Threshold Mode:";
            //
            // txtSICNoiseFractionLowIntensityDataToAverage
            //
            txtSICNoiseFractionLowIntensityDataToAverage.Location = new System.Drawing.Point(298, 92);
            txtSICNoiseFractionLowIntensityDataToAverage.Name = "txtSICNoiseFractionLowIntensityDataToAverage";
            txtSICNoiseFractionLowIntensityDataToAverage.Size = new System.Drawing.Size(67, 22);
            txtSICNoiseFractionLowIntensityDataToAverage.TabIndex = 5;
            txtSICNoiseFractionLowIntensityDataToAverage.Text = "0.75";
            txtSICNoiseFractionLowIntensityDataToAverage.KeyPress += new KeyPressEventHandler(txtSICNoiseFractionLowIntensityDataToAverage_KeyPress);
            //
            // lblSICNoiseFractionLowIntensityDataToAverage
            //
            lblSICNoiseFractionLowIntensityDataToAverage.Location = new System.Drawing.Point(19, 95);
            lblSICNoiseFractionLowIntensityDataToAverage.Name = "lblSICNoiseFractionLowIntensityDataToAverage";
            lblSICNoiseFractionLowIntensityDataToAverage.Size = new System.Drawing.Size(231, 16);
            lblSICNoiseFractionLowIntensityDataToAverage.TabIndex = 4;
            lblSICNoiseFractionLowIntensityDataToAverage.Text = "Fraction low intensity data to average";
            //
            // txtSICNoiseThresholdIntensity
            //
            txtSICNoiseThresholdIntensity.Location = new System.Drawing.Point(298, 65);
            txtSICNoiseThresholdIntensity.Name = "txtSICNoiseThresholdIntensity";
            txtSICNoiseThresholdIntensity.Size = new System.Drawing.Size(67, 22);
            txtSICNoiseThresholdIntensity.TabIndex = 3;
            txtSICNoiseThresholdIntensity.Text = "0";
            txtSICNoiseThresholdIntensity.KeyPress += new KeyPressEventHandler(txtSICNoiseThresholdIntensity_KeyPress);
            //
            // lblSICNoiseThresholdIntensity
            //
            lblSICNoiseThresholdIntensity.Location = new System.Drawing.Point(19, 67);
            lblSICNoiseThresholdIntensity.Name = "lblSICNoiseThresholdIntensity";
            lblSICNoiseThresholdIntensity.Size = new System.Drawing.Size(240, 18);
            lblSICNoiseThresholdIntensity.TabIndex = 2;
            lblSICNoiseThresholdIntensity.Text = "Absolute Noise Thereshold Intensity";
            //
            // fraSmoothingOptions
            //
            fraSmoothingOptions.Controls.Add(chkSmoothDataRegardlessOfMinimumPeakWidth);
            fraSmoothingOptions.Controls.Add(chkFindPeaksOnSmoothedData);
            fraSmoothingOptions.Controls.Add(optUseSavitzkyGolaySmooth);
            fraSmoothingOptions.Controls.Add(txtButterworthSamplingFrequency);
            fraSmoothingOptions.Controls.Add(lblButterworthSamplingFrequency);
            fraSmoothingOptions.Controls.Add(txtSavitzkyGolayFilterOrder);
            fraSmoothingOptions.Controls.Add(lblSavitzkyGolayFilterOrder);
            fraSmoothingOptions.Controls.Add(optUseButterworthSmooth);
            fraSmoothingOptions.Location = new System.Drawing.Point(422, 9);
            fraSmoothingOptions.Name = "fraSmoothingOptions";
            fraSmoothingOptions.Size = new System.Drawing.Size(298, 259);
            fraSmoothingOptions.TabIndex = 2;
            fraSmoothingOptions.TabStop = false;
            fraSmoothingOptions.Text = "Smoothing Options";
            //
            // chkSmoothDataRegardlessOfMinimumPeakWidth
            //
            chkSmoothDataRegardlessOfMinimumPeakWidth.Checked = true;
            chkSmoothDataRegardlessOfMinimumPeakWidth.CheckState = CheckState.Checked;
            chkSmoothDataRegardlessOfMinimumPeakWidth.Location = new System.Drawing.Point(29, 203);
            chkSmoothDataRegardlessOfMinimumPeakWidth.Name = "chkSmoothDataRegardlessOfMinimumPeakWidth";
            chkSmoothDataRegardlessOfMinimumPeakWidth.Size = new System.Drawing.Size(192, 46);
            chkSmoothDataRegardlessOfMinimumPeakWidth.TabIndex = 7;
            chkSmoothDataRegardlessOfMinimumPeakWidth.Text = "Smooth Data Regardless Of Minimum Peak Width";
            //
            // chkFindPeaksOnSmoothedData
            //
            chkFindPeaksOnSmoothedData.Checked = true;
            chkFindPeaksOnSmoothedData.CheckState = CheckState.Checked;
            chkFindPeaksOnSmoothedData.Location = new System.Drawing.Point(29, 175);
            chkFindPeaksOnSmoothedData.Name = "chkFindPeaksOnSmoothedData";
            chkFindPeaksOnSmoothedData.Size = new System.Drawing.Size(249, 19);
            chkFindPeaksOnSmoothedData.TabIndex = 6;
            chkFindPeaksOnSmoothedData.Text = "Find Peaks On Smoothed Data";
            //
            // optUseSavitzkyGolaySmooth
            //
            optUseSavitzkyGolaySmooth.Location = new System.Drawing.Point(19, 92);
            optUseSavitzkyGolaySmooth.Name = "optUseSavitzkyGolaySmooth";
            optUseSavitzkyGolaySmooth.Size = new System.Drawing.Size(240, 19);
            optUseSavitzkyGolaySmooth.TabIndex = 3;
            optUseSavitzkyGolaySmooth.Text = "Use Savitzky Golay Smooth";
            optUseSavitzkyGolaySmooth.CheckedChanged += new EventHandler(optUseSavitzkyGolaySmooth_CheckedChanged);
            //
            // txtButterworthSamplingFrequency
            //
            txtButterworthSamplingFrequency.Location = new System.Drawing.Point(134, 55);
            txtButterworthSamplingFrequency.Name = "txtButterworthSamplingFrequency";
            txtButterworthSamplingFrequency.Size = new System.Drawing.Size(58, 22);
            txtButterworthSamplingFrequency.TabIndex = 2;
            txtButterworthSamplingFrequency.Text = "0.25";
            txtButterworthSamplingFrequency.KeyPress += new KeyPressEventHandler(txtButterworthSamplingFrequency_KeyPress);
            txtButterworthSamplingFrequency.Validating += new System.ComponentModel.CancelEventHandler(txtButterworthSamplingFrequency_Validating);
            //
            // lblButterworthSamplingFrequency
            //
            lblButterworthSamplingFrequency.Location = new System.Drawing.Point(38, 55);
            lblButterworthSamplingFrequency.Name = "lblButterworthSamplingFrequency";
            lblButterworthSamplingFrequency.Size = new System.Drawing.Size(87, 19);
            lblButterworthSamplingFrequency.TabIndex = 1;
            lblButterworthSamplingFrequency.Text = "Filter Order";
            //
            // txtSavitzkyGolayFilterOrder
            //
            txtSavitzkyGolayFilterOrder.Location = new System.Drawing.Point(134, 120);
            txtSavitzkyGolayFilterOrder.Name = "txtSavitzkyGolayFilterOrder";
            txtSavitzkyGolayFilterOrder.Size = new System.Drawing.Size(58, 22);
            txtSavitzkyGolayFilterOrder.TabIndex = 5;
            txtSavitzkyGolayFilterOrder.Text = "0";
            txtSavitzkyGolayFilterOrder.KeyPress += new KeyPressEventHandler(txtSavitzkyGolayFilterOrder_KeyPress);
            txtSavitzkyGolayFilterOrder.Validating += new System.ComponentModel.CancelEventHandler(txtSavitzkyGolayFilterOrder_Validating);
            //
            // lblSavitzkyGolayFilterOrder
            //
            lblSavitzkyGolayFilterOrder.Location = new System.Drawing.Point(38, 120);
            lblSavitzkyGolayFilterOrder.Name = "lblSavitzkyGolayFilterOrder";
            lblSavitzkyGolayFilterOrder.Size = new System.Drawing.Size(87, 18);
            lblSavitzkyGolayFilterOrder.TabIndex = 4;
            lblSavitzkyGolayFilterOrder.Text = "Filter Order";
            //
            // optUseButterworthSmooth
            //
            optUseButterworthSmooth.Checked = true;
            optUseButterworthSmooth.Location = new System.Drawing.Point(19, 28);
            optUseButterworthSmooth.Name = "optUseButterworthSmooth";
            optUseButterworthSmooth.Size = new System.Drawing.Size(240, 18);
            optUseButterworthSmooth.TabIndex = 0;
            optUseButterworthSmooth.TabStop = true;
            optUseButterworthSmooth.Text = "Use Butterworth Smooth";
            optUseButterworthSmooth.CheckedChanged += new EventHandler(optUseButterworthSmooth_CheckedChanged);
            //
            // fraPeakFindingOptions
            //
            fraPeakFindingOptions.Controls.Add(txtInitialPeakWidthScansMaximum);
            fraPeakFindingOptions.Controls.Add(lblInitialPeakWidthScansMaximum);
            fraPeakFindingOptions.Controls.Add(txtInitialPeakWidthScansScaler);
            fraPeakFindingOptions.Controls.Add(lblInitialPeakWidthScansScaler);
            fraPeakFindingOptions.Controls.Add(txtMaxAllowedUpwardSpikeFractionMax);
            fraPeakFindingOptions.Controls.Add(lblMaxAllowedUpwardSpikeFractionMax);
            fraPeakFindingOptions.Controls.Add(txtMaxDistanceScansNoOverlap);
            fraPeakFindingOptions.Controls.Add(lblMaxDistanceScansNoOverlap);
            fraPeakFindingOptions.Location = new System.Drawing.Point(19, 166);
            fraPeakFindingOptions.Name = "fraPeakFindingOptions";
            fraPeakFindingOptions.Size = new System.Drawing.Size(384, 148);
            fraPeakFindingOptions.TabIndex = 1;
            fraPeakFindingOptions.TabStop = false;
            fraPeakFindingOptions.Text = "Fine Tuning Peak Finding";
            //
            // txtInitialPeakWidthScansMaximum
            //
            txtInitialPeakWidthScansMaximum.Location = new System.Drawing.Point(288, 111);
            txtInitialPeakWidthScansMaximum.Name = "txtInitialPeakWidthScansMaximum";
            txtInitialPeakWidthScansMaximum.Size = new System.Drawing.Size(67, 22);
            txtInitialPeakWidthScansMaximum.TabIndex = 7;
            txtInitialPeakWidthScansMaximum.Text = "30";
            txtInitialPeakWidthScansMaximum.KeyPress += new KeyPressEventHandler(txtInitialPeakWidthScansMaximum_KeyPress);
            //
            // lblInitialPeakWidthScansMaximum
            //
            lblInitialPeakWidthScansMaximum.Location = new System.Drawing.Point(19, 113);
            lblInitialPeakWidthScansMaximum.Name = "lblInitialPeakWidthScansMaximum";
            lblInitialPeakWidthScansMaximum.Size = new System.Drawing.Size(240, 19);
            lblInitialPeakWidthScansMaximum.TabIndex = 6;
            lblInitialPeakWidthScansMaximum.Text = "Initial Peak Width Maximum (Scans)";
            //
            // txtInitialPeakWidthScansScaler
            //
            txtInitialPeakWidthScansScaler.Location = new System.Drawing.Point(288, 83);
            txtInitialPeakWidthScansScaler.Name = "txtInitialPeakWidthScansScaler";
            txtInitialPeakWidthScansScaler.Size = new System.Drawing.Size(67, 22);
            txtInitialPeakWidthScansScaler.TabIndex = 5;
            txtInitialPeakWidthScansScaler.Text = "1";
            txtInitialPeakWidthScansScaler.KeyPress += new KeyPressEventHandler(txtInitialPeakWidthScansScaler_KeyPress);
            //
            // lblInitialPeakWidthScansScaler
            //
            lblInitialPeakWidthScansScaler.Location = new System.Drawing.Point(19, 85);
            lblInitialPeakWidthScansScaler.Name = "lblInitialPeakWidthScansScaler";
            lblInitialPeakWidthScansScaler.Size = new System.Drawing.Size(240, 19);
            lblInitialPeakWidthScansScaler.TabIndex = 4;
            lblInitialPeakWidthScansScaler.Text = "Initial Peak Width Scaler (Scans)";
            //
            // txtMaxAllowedUpwardSpikeFractionMax
            //
            txtMaxAllowedUpwardSpikeFractionMax.Location = new System.Drawing.Point(288, 55);
            txtMaxAllowedUpwardSpikeFractionMax.Name = "txtMaxAllowedUpwardSpikeFractionMax";
            txtMaxAllowedUpwardSpikeFractionMax.Size = new System.Drawing.Size(67, 22);
            txtMaxAllowedUpwardSpikeFractionMax.TabIndex = 3;
            txtMaxAllowedUpwardSpikeFractionMax.Text = "0.2";
            txtMaxAllowedUpwardSpikeFractionMax.KeyPress += new KeyPressEventHandler(txtMaxAllowedUpwardSpikeFractionMax_KeyPress);
            //
            // lblMaxAllowedUpwardSpikeFractionMax
            //
            lblMaxAllowedUpwardSpikeFractionMax.Location = new System.Drawing.Point(19, 58);
            lblMaxAllowedUpwardSpikeFractionMax.Name = "lblMaxAllowedUpwardSpikeFractionMax";
            lblMaxAllowedUpwardSpikeFractionMax.Size = new System.Drawing.Size(279, 18);
            lblMaxAllowedUpwardSpikeFractionMax.TabIndex = 2;
            lblMaxAllowedUpwardSpikeFractionMax.Text = "Max Allowed Upward Spike (Fraction Max)";
            //
            // txtMaxDistanceScansNoOverlap
            //
            txtMaxDistanceScansNoOverlap.Location = new System.Drawing.Point(288, 28);
            txtMaxDistanceScansNoOverlap.Name = "txtMaxDistanceScansNoOverlap";
            txtMaxDistanceScansNoOverlap.Size = new System.Drawing.Size(67, 22);
            txtMaxDistanceScansNoOverlap.TabIndex = 1;
            txtMaxDistanceScansNoOverlap.Text = "0";
            txtMaxDistanceScansNoOverlap.KeyPress += new KeyPressEventHandler(txtMaxDistanceScansNoOverlap_KeyPress);
            //
            // lblMaxDistanceScansNoOverlap
            //
            lblMaxDistanceScansNoOverlap.Location = new System.Drawing.Point(19, 30);
            lblMaxDistanceScansNoOverlap.Name = "lblMaxDistanceScansNoOverlap";
            lblMaxDistanceScansNoOverlap.Size = new System.Drawing.Size(240, 18);
            lblMaxDistanceScansNoOverlap.TabIndex = 0;
            lblMaxDistanceScansNoOverlap.Text = "Max Distance No Overlap (Scans)";
            //
            // TabPageBinningAndSimilarityOptions
            //
            TabPageBinningAndSimilarityOptions.Controls.Add(fraMassSpectraNoiseThresholds);
            TabPageBinningAndSimilarityOptions.Controls.Add(fraBinningIntensityOptions);
            TabPageBinningAndSimilarityOptions.Controls.Add(fraSpectrumSimilarityOptions);
            TabPageBinningAndSimilarityOptions.Controls.Add(fraBinningMZOptions);
            TabPageBinningAndSimilarityOptions.Location = new System.Drawing.Point(4, 25);
            TabPageBinningAndSimilarityOptions.Name = "TabPageBinningAndSimilarityOptions";
            TabPageBinningAndSimilarityOptions.Size = new System.Drawing.Size(882, 327);
            TabPageBinningAndSimilarityOptions.TabIndex = 6;
            TabPageBinningAndSimilarityOptions.Text = "Binning and Similarity";
            TabPageBinningAndSimilarityOptions.UseVisualStyleBackColor = true;
            //
            // fraMassSpectraNoiseThresholds
            //
            fraMassSpectraNoiseThresholds.Controls.Add(txtMassSpectraNoiseMinimumSignalToNoiseRatio);
            fraMassSpectraNoiseThresholds.Controls.Add(lblMassSpectraNoiseMinimumSignalToNoiseRatio);
            fraMassSpectraNoiseThresholds.Controls.Add(txtMassSpectraNoiseThresholdIntensity);
            fraMassSpectraNoiseThresholds.Controls.Add(txtMassSpectraNoiseFractionLowIntensityDataToAverage);
            fraMassSpectraNoiseThresholds.Controls.Add(lblMassSpectraNoiseFractionLowIntensityDataToAverage);
            fraMassSpectraNoiseThresholds.Controls.Add(cboMassSpectraNoiseThresholdMode);
            fraMassSpectraNoiseThresholds.Controls.Add(lblMassSpectraNoiseThresholdMode);
            fraMassSpectraNoiseThresholds.Controls.Add(lblMassSpectraNoiseThresholdIntensity);
            fraMassSpectraNoiseThresholds.Location = new System.Drawing.Point(10, 18);
            fraMassSpectraNoiseThresholds.Name = "fraMassSpectraNoiseThresholds";
            fraMassSpectraNoiseThresholds.Size = new System.Drawing.Size(412, 148);
            fraMassSpectraNoiseThresholds.TabIndex = 0;
            fraMassSpectraNoiseThresholds.TabStop = false;
            fraMassSpectraNoiseThresholds.Text = "Noise Threshold Determination for Mass Spectra";
            //
            // txtMassSpectraNoiseMinimumSignalToNoiseRatio
            //
            txtMassSpectraNoiseMinimumSignalToNoiseRatio.Location = new System.Drawing.Point(250, 120);
            txtMassSpectraNoiseMinimumSignalToNoiseRatio.Name = "txtMassSpectraNoiseMinimumSignalToNoiseRatio";
            txtMassSpectraNoiseMinimumSignalToNoiseRatio.Size = new System.Drawing.Size(67, 22);
            txtMassSpectraNoiseMinimumSignalToNoiseRatio.TabIndex = 9;
            txtMassSpectraNoiseMinimumSignalToNoiseRatio.Text = "2";
            //
            // lblMassSpectraNoiseMinimumSignalToNoiseRatio
            //
            lblMassSpectraNoiseMinimumSignalToNoiseRatio.Location = new System.Drawing.Point(10, 120);
            lblMassSpectraNoiseMinimumSignalToNoiseRatio.Name = "lblMassSpectraNoiseMinimumSignalToNoiseRatio";
            lblMassSpectraNoiseMinimumSignalToNoiseRatio.Size = new System.Drawing.Size(230, 16);
            lblMassSpectraNoiseMinimumSignalToNoiseRatio.TabIndex = 8;
            lblMassSpectraNoiseMinimumSignalToNoiseRatio.Text = "Minimum Signal to Noise Ratio";
            //
            // txtMassSpectraNoiseThresholdIntensity
            //
            txtMassSpectraNoiseThresholdIntensity.Location = new System.Drawing.Point(250, 65);
            txtMassSpectraNoiseThresholdIntensity.Name = "txtMassSpectraNoiseThresholdIntensity";
            txtMassSpectraNoiseThresholdIntensity.Size = new System.Drawing.Size(67, 22);
            txtMassSpectraNoiseThresholdIntensity.TabIndex = 3;
            txtMassSpectraNoiseThresholdIntensity.Text = "0";
            txtMassSpectraNoiseThresholdIntensity.KeyPress += new KeyPressEventHandler(txtMassSpectraNoiseThresholdIntensity_KeyPress);
            //
            // txtMassSpectraNoiseFractionLowIntensityDataToAverage
            //
            txtMassSpectraNoiseFractionLowIntensityDataToAverage.Location = new System.Drawing.Point(250, 92);
            txtMassSpectraNoiseFractionLowIntensityDataToAverage.Name = "txtMassSpectraNoiseFractionLowIntensityDataToAverage";
            txtMassSpectraNoiseFractionLowIntensityDataToAverage.Size = new System.Drawing.Size(67, 22);
            txtMassSpectraNoiseFractionLowIntensityDataToAverage.TabIndex = 5;
            txtMassSpectraNoiseFractionLowIntensityDataToAverage.Text = "0.5";
            txtMassSpectraNoiseFractionLowIntensityDataToAverage.KeyPress += new KeyPressEventHandler(txtMassSpectraNoiseFractionLowIntensityDataToAverage_KeyPress);
            //
            // lblMassSpectraNoiseFractionLowIntensityDataToAverage
            //
            lblMassSpectraNoiseFractionLowIntensityDataToAverage.Location = new System.Drawing.Point(10, 92);
            lblMassSpectraNoiseFractionLowIntensityDataToAverage.Name = "lblMassSpectraNoiseFractionLowIntensityDataToAverage";
            lblMassSpectraNoiseFractionLowIntensityDataToAverage.Size = new System.Drawing.Size(230, 26);
            lblMassSpectraNoiseFractionLowIntensityDataToAverage.TabIndex = 4;
            lblMassSpectraNoiseFractionLowIntensityDataToAverage.Text = "Fraction low intensity data to average";
            //
            // cboMassSpectraNoiseThresholdMode
            //
            cboMassSpectraNoiseThresholdMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cboMassSpectraNoiseThresholdMode.Location = new System.Drawing.Point(163, 28);
            cboMassSpectraNoiseThresholdMode.Name = "cboMassSpectraNoiseThresholdMode";
            cboMassSpectraNoiseThresholdMode.Size = new System.Drawing.Size(211, 24);
            cboMassSpectraNoiseThresholdMode.TabIndex = 1;
            cboMassSpectraNoiseThresholdMode.SelectedIndexChanged += new EventHandler(cboMassSpectraNoiseThresholdMode_SelectedIndexChanged);
            //
            // lblMassSpectraNoiseThresholdMode
            //
            lblMassSpectraNoiseThresholdMode.Location = new System.Drawing.Point(10, 37);
            lblMassSpectraNoiseThresholdMode.Name = "lblMassSpectraNoiseThresholdMode";
            lblMassSpectraNoiseThresholdMode.Size = new System.Drawing.Size(163, 18);
            lblMassSpectraNoiseThresholdMode.TabIndex = 0;
            lblMassSpectraNoiseThresholdMode.Text = "Noise Threshold Mode:";
            //
            // lblMassSpectraNoiseThresholdIntensity
            //
            lblMassSpectraNoiseThresholdIntensity.Location = new System.Drawing.Point(10, 65);
            lblMassSpectraNoiseThresholdIntensity.Name = "lblMassSpectraNoiseThresholdIntensity";
            lblMassSpectraNoiseThresholdIntensity.Size = new System.Drawing.Size(220, 18);
            lblMassSpectraNoiseThresholdIntensity.TabIndex = 2;
            lblMassSpectraNoiseThresholdIntensity.Text = "Absolute Noise Threshold Intensity";
            //
            // fraBinningIntensityOptions
            //
            fraBinningIntensityOptions.Controls.Add(lblBinnedDataIntensityPrecisionPctUnits);
            fraBinningIntensityOptions.Controls.Add(chkBinnedDataSumAllIntensitiesForBin);
            fraBinningIntensityOptions.Controls.Add(chkBinnedDataNormalize);
            fraBinningIntensityOptions.Controls.Add(txtBinnedDataIntensityPrecisionPct);
            fraBinningIntensityOptions.Controls.Add(lblBinnedDataIntensityPrecisionPct);
            fraBinningIntensityOptions.Location = new System.Drawing.Point(442, 175);
            fraBinningIntensityOptions.Name = "fraBinningIntensityOptions";
            fraBinningIntensityOptions.Size = new System.Drawing.Size(288, 120);
            fraBinningIntensityOptions.TabIndex = 3;
            fraBinningIntensityOptions.TabStop = false;
            fraBinningIntensityOptions.Text = "Binning Intensity Options";
            //
            // lblBinnedDataIntensityPrecisionPctUnits
            //
            lblBinnedDataIntensityPrecisionPctUnits.Location = new System.Drawing.Point(259, 28);
            lblBinnedDataIntensityPrecisionPctUnits.Name = "lblBinnedDataIntensityPrecisionPctUnits";
            lblBinnedDataIntensityPrecisionPctUnits.Size = new System.Drawing.Size(19, 18);
            lblBinnedDataIntensityPrecisionPctUnits.TabIndex = 8;
            lblBinnedDataIntensityPrecisionPctUnits.Text = "%";
            //
            // chkBinnedDataSumAllIntensitiesForBin
            //
            chkBinnedDataSumAllIntensitiesForBin.Location = new System.Drawing.Point(10, 92);
            chkBinnedDataSumAllIntensitiesForBin.Name = "chkBinnedDataSumAllIntensitiesForBin";
            chkBinnedDataSumAllIntensitiesForBin.Size = new System.Drawing.Size(192, 19);
            chkBinnedDataSumAllIntensitiesForBin.TabIndex = 10;
            chkBinnedDataSumAllIntensitiesForBin.Text = "Sum All Intensities For Bin";
            //
            // chkBinnedDataNormalize
            //
            chkBinnedDataNormalize.Location = new System.Drawing.Point(10, 65);
            chkBinnedDataNormalize.Name = "chkBinnedDataNormalize";
            chkBinnedDataNormalize.Size = new System.Drawing.Size(163, 18);
            chkBinnedDataNormalize.TabIndex = 9;
            chkBinnedDataNormalize.Text = "Normalize Intensities";
            //
            // txtBinnedDataIntensityPrecisionPct
            //
            txtBinnedDataIntensityPrecisionPct.Location = new System.Drawing.Point(211, 28);
            txtBinnedDataIntensityPrecisionPct.Name = "txtBinnedDataIntensityPrecisionPct";
            txtBinnedDataIntensityPrecisionPct.Size = new System.Drawing.Size(48, 22);
            txtBinnedDataIntensityPrecisionPct.TabIndex = 7;
            txtBinnedDataIntensityPrecisionPct.Text = "1";
            txtBinnedDataIntensityPrecisionPct.KeyPress += new KeyPressEventHandler(txtBinnedDataIntensityPrecisionPct_KeyPress);
            //
            // lblBinnedDataIntensityPrecisionPct
            //
            lblBinnedDataIntensityPrecisionPct.Location = new System.Drawing.Point(10, 30);
            lblBinnedDataIntensityPrecisionPct.Name = "lblBinnedDataIntensityPrecisionPct";
            lblBinnedDataIntensityPrecisionPct.Size = new System.Drawing.Size(201, 18);
            lblBinnedDataIntensityPrecisionPct.TabIndex = 6;
            lblBinnedDataIntensityPrecisionPct.Text = "Intensity Precision (0.1 to 100)";
            //
            // fraSpectrumSimilarityOptions
            //
            fraSpectrumSimilarityOptions.Controls.Add(lblSimilarIonTimeToleranceHalfWidthUnits);
            fraSpectrumSimilarityOptions.Controls.Add(txtSpectrumSimilarityMinimum);
            fraSpectrumSimilarityOptions.Controls.Add(lblSpectrumSimilarityMinimum);
            fraSpectrumSimilarityOptions.Controls.Add(txtSimilarIonToleranceHalfWidthMinutes);
            fraSpectrumSimilarityOptions.Controls.Add(lblSimilarIonTimeToleranceHalfWidth);
            fraSpectrumSimilarityOptions.Controls.Add(txtSimilarIonMZToleranceHalfWidth);
            fraSpectrumSimilarityOptions.Controls.Add(lblSimilarIonMZToleranceHalfWidth);
            fraSpectrumSimilarityOptions.Location = new System.Drawing.Point(10, 175);
            fraSpectrumSimilarityOptions.Name = "fraSpectrumSimilarityOptions";
            fraSpectrumSimilarityOptions.Size = new System.Drawing.Size(326, 120);
            fraSpectrumSimilarityOptions.TabIndex = 1;
            fraSpectrumSimilarityOptions.TabStop = false;
            fraSpectrumSimilarityOptions.Text = "Spectrum Similarity Options";
            //
            // lblSimilarIonTimeToleranceHalfWidthUnits
            //
            lblSimilarIonTimeToleranceHalfWidthUnits.Location = new System.Drawing.Point(259, 58);
            lblSimilarIonTimeToleranceHalfWidthUnits.Name = "lblSimilarIonTimeToleranceHalfWidthUnits";
            lblSimilarIonTimeToleranceHalfWidthUnits.Size = new System.Drawing.Size(58, 18);
            lblSimilarIonTimeToleranceHalfWidthUnits.TabIndex = 6;
            lblSimilarIonTimeToleranceHalfWidthUnits.Text = "minutes";
            //
            // txtSpectrumSimilarityMinimum
            //
            txtSpectrumSimilarityMinimum.Location = new System.Drawing.Point(202, 83);
            txtSpectrumSimilarityMinimum.Name = "txtSpectrumSimilarityMinimum";
            txtSpectrumSimilarityMinimum.Size = new System.Drawing.Size(48, 22);
            txtSpectrumSimilarityMinimum.TabIndex = 5;
            txtSpectrumSimilarityMinimum.Text = "0.7";
            txtSpectrumSimilarityMinimum.KeyPress += new KeyPressEventHandler(txtSpectrumSimilarityMinimum_KeyPress);
            //
            // lblSpectrumSimilarityMinimum
            //
            lblSpectrumSimilarityMinimum.Location = new System.Drawing.Point(10, 85);
            lblSpectrumSimilarityMinimum.Name = "lblSpectrumSimilarityMinimum";
            lblSpectrumSimilarityMinimum.Size = new System.Drawing.Size(180, 19);
            lblSpectrumSimilarityMinimum.TabIndex = 4;
            lblSpectrumSimilarityMinimum.Text = "Minimum Similarity (0 to 1)";
            //
            // txtSimilarIonToleranceHalfWidthMinutes
            //
            txtSimilarIonToleranceHalfWidthMinutes.Location = new System.Drawing.Point(202, 55);
            txtSimilarIonToleranceHalfWidthMinutes.Name = "txtSimilarIonToleranceHalfWidthMinutes";
            txtSimilarIonToleranceHalfWidthMinutes.Size = new System.Drawing.Size(48, 22);
            txtSimilarIonToleranceHalfWidthMinutes.TabIndex = 3;
            txtSimilarIonToleranceHalfWidthMinutes.Text = "5";
            txtSimilarIonToleranceHalfWidthMinutes.KeyPress += new KeyPressEventHandler(txtSimilarIonToleranceHalfWidthMinutes_KeyPress);
            //
            // lblSimilarIonTimeToleranceHalfWidth
            //
            lblSimilarIonTimeToleranceHalfWidth.Location = new System.Drawing.Point(10, 58);
            lblSimilarIonTimeToleranceHalfWidth.Name = "lblSimilarIonTimeToleranceHalfWidth";
            lblSimilarIonTimeToleranceHalfWidth.Size = new System.Drawing.Size(180, 18);
            lblSimilarIonTimeToleranceHalfWidth.TabIndex = 2;
            lblSimilarIonTimeToleranceHalfWidth.Text = "Time Tolerance Half Width";
            //
            // txtSimilarIonMZToleranceHalfWidth
            //
            txtSimilarIonMZToleranceHalfWidth.Location = new System.Drawing.Point(202, 28);
            txtSimilarIonMZToleranceHalfWidth.Name = "txtSimilarIonMZToleranceHalfWidth";
            txtSimilarIonMZToleranceHalfWidth.Size = new System.Drawing.Size(48, 22);
            txtSimilarIonMZToleranceHalfWidth.TabIndex = 1;
            txtSimilarIonMZToleranceHalfWidth.Text = "0.1";
            txtSimilarIonMZToleranceHalfWidth.KeyPress += new KeyPressEventHandler(txtSimilarIonMZToleranceHalfWidth_KeyPress);
            //
            // lblSimilarIonMZToleranceHalfWidth
            //
            lblSimilarIonMZToleranceHalfWidth.Location = new System.Drawing.Point(10, 30);
            lblSimilarIonMZToleranceHalfWidth.Name = "lblSimilarIonMZToleranceHalfWidth";
            lblSimilarIonMZToleranceHalfWidth.Size = new System.Drawing.Size(180, 18);
            lblSimilarIonMZToleranceHalfWidth.TabIndex = 0;
            lblSimilarIonMZToleranceHalfWidth.Text = "m/z Tolerance Half Width";
            //
            // fraBinningMZOptions
            //
            fraBinningMZOptions.Controls.Add(txtMaximumBinCount);
            fraBinningMZOptions.Controls.Add(lblMaximumBinCount);
            fraBinningMZOptions.Controls.Add(txtBinSize);
            fraBinningMZOptions.Controls.Add(lblBinSize);
            fraBinningMZOptions.Controls.Add(txtBinEndX);
            fraBinningMZOptions.Controls.Add(lblBinEndX);
            fraBinningMZOptions.Controls.Add(txtBinStartX);
            fraBinningMZOptions.Controls.Add(lblBinStartX);
            fraBinningMZOptions.Location = new System.Drawing.Point(442, 18);
            fraBinningMZOptions.Name = "fraBinningMZOptions";
            fraBinningMZOptions.Size = new System.Drawing.Size(288, 148);
            fraBinningMZOptions.TabIndex = 2;
            fraBinningMZOptions.TabStop = false;
            fraBinningMZOptions.Text = "Binning m/z Options";
            //
            // txtMaximumBinCount
            //
            txtMaximumBinCount.Location = new System.Drawing.Point(182, 111);
            txtMaximumBinCount.Name = "txtMaximumBinCount";
            txtMaximumBinCount.Size = new System.Drawing.Size(68, 22);
            txtMaximumBinCount.TabIndex = 7;
            txtMaximumBinCount.Text = "100000";
            txtMaximumBinCount.KeyPress += new KeyPressEventHandler(txtMaximumBinCount_KeyPress);
            //
            // lblMaximumBinCount
            //
            lblMaximumBinCount.Location = new System.Drawing.Point(19, 113);
            lblMaximumBinCount.Name = "lblMaximumBinCount";
            lblMaximumBinCount.Size = new System.Drawing.Size(154, 19);
            lblMaximumBinCount.TabIndex = 6;
            lblMaximumBinCount.Text = "Maximum Bin Count";
            //
            // txtBinSize
            //
            txtBinSize.Location = new System.Drawing.Point(182, 83);
            txtBinSize.Name = "txtBinSize";
            txtBinSize.Size = new System.Drawing.Size(68, 22);
            txtBinSize.TabIndex = 5;
            txtBinSize.Text = "1";
            txtBinSize.KeyPress += new KeyPressEventHandler(txtBinSize_KeyPress);
            //
            // lblBinSize
            //
            lblBinSize.Location = new System.Drawing.Point(19, 85);
            lblBinSize.Name = "lblBinSize";
            lblBinSize.Size = new System.Drawing.Size(154, 19);
            lblBinSize.TabIndex = 4;
            lblBinSize.Text = "Bin Size (m/z units)";
            //
            // txtBinEndX
            //
            txtBinEndX.Location = new System.Drawing.Point(182, 55);
            txtBinEndX.Name = "txtBinEndX";
            txtBinEndX.Size = new System.Drawing.Size(68, 22);
            txtBinEndX.TabIndex = 3;
            txtBinEndX.Text = "2000";
            txtBinEndX.KeyPress += new KeyPressEventHandler(txtBinEndX_KeyPress);
            //
            // lblBinEndX
            //
            lblBinEndX.Location = new System.Drawing.Point(19, 58);
            lblBinEndX.Name = "lblBinEndX";
            lblBinEndX.Size = new System.Drawing.Size(144, 18);
            lblBinEndX.TabIndex = 2;
            lblBinEndX.Text = "Bin End m/z";
            //
            // txtBinStartX
            //
            txtBinStartX.Location = new System.Drawing.Point(182, 28);
            txtBinStartX.Name = "txtBinStartX";
            txtBinStartX.Size = new System.Drawing.Size(68, 22);
            txtBinStartX.TabIndex = 1;
            txtBinStartX.Text = "50";
            txtBinStartX.KeyPress += new KeyPressEventHandler(txtBinStartX_KeyPress);
            //
            // lblBinStartX
            //
            lblBinStartX.Location = new System.Drawing.Point(19, 30);
            lblBinStartX.Name = "lblBinStartX";
            lblBinStartX.Size = new System.Drawing.Size(144, 18);
            lblBinStartX.TabIndex = 0;
            lblBinStartX.Text = "Bin Start m/z";
            //
            // TabPageCustomSICOptions
            //
            TabPageCustomSICOptions.Controls.Add(txtCustomSICFileDescription);
            TabPageCustomSICOptions.Controls.Add(cmdSelectCustomSICFile);
            TabPageCustomSICOptions.Controls.Add(txtCustomSICFileName);
            TabPageCustomSICOptions.Controls.Add(fraCustomSICControls);
            TabPageCustomSICOptions.Controls.Add(dgCustomSICValues);
            TabPageCustomSICOptions.Location = new System.Drawing.Point(4, 25);
            TabPageCustomSICOptions.Name = "TabPageCustomSICOptions";
            TabPageCustomSICOptions.Size = new System.Drawing.Size(882, 327);
            TabPageCustomSICOptions.TabIndex = 3;
            TabPageCustomSICOptions.Text = "Custom SIC Options";
            TabPageCustomSICOptions.UseVisualStyleBackColor = true;
            //
            // txtCustomSICFileDescription
            //
            txtCustomSICFileDescription.Location = new System.Drawing.Point(10, 6);
            txtCustomSICFileDescription.Multiline = true;
            txtCustomSICFileDescription.Name = "txtCustomSICFileDescription";
            txtCustomSICFileDescription.ReadOnly = true;
            txtCustomSICFileDescription.ScrollBars = ScrollBars.Vertical;
            txtCustomSICFileDescription.Size = new System.Drawing.Size(582, 59);
            txtCustomSICFileDescription.TabIndex = 0;
            txtCustomSICFileDescription.Text = "Custom SIC description ... populated via code.";
            txtCustomSICFileDescription.KeyDown += new KeyEventHandler(txtCustomSICFileDescription_KeyDown);
            //
            // cmdSelectCustomSICFile
            //
            cmdSelectCustomSICFile.Location = new System.Drawing.Point(10, 74);
            cmdSelectCustomSICFile.Name = "cmdSelectCustomSICFile";
            cmdSelectCustomSICFile.Size = new System.Drawing.Size(96, 28);
            cmdSelectCustomSICFile.TabIndex = 1;
            cmdSelectCustomSICFile.Text = "&Select File";
            cmdSelectCustomSICFile.Click += new EventHandler(cmdSelectCustomSICFile_Click);
            //
            // txtCustomSICFileName
            //
            txtCustomSICFileName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtCustomSICFileName.Location = new System.Drawing.Point(125, 74);
            txtCustomSICFileName.Name = "txtCustomSICFileName";
            txtCustomSICFileName.Size = new System.Drawing.Size(534, 22);
            txtCustomSICFileName.TabIndex = 2;
            txtCustomSICFileName.TextChanged += new EventHandler(txtCustomSICFileName_TextChanged);
            //
            // fraCustomSICControls
            //
            fraCustomSICControls.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            fraCustomSICControls.Controls.Add(lblCustomSICToleranceType);
            fraCustomSICControls.Controls.Add(optCustomSICScanToleranceAcqTime);
            fraCustomSICControls.Controls.Add(optCustomSICScanToleranceRelative);
            fraCustomSICControls.Controls.Add(optCustomSICScanToleranceAbsolute);
            fraCustomSICControls.Controls.Add(chkLimitSearchToCustomMZs);
            fraCustomSICControls.Controls.Add(txtCustomSICScanOrAcqTimeTolerance);
            fraCustomSICControls.Controls.Add(lblCustomSICScanTolerance);
            fraCustomSICControls.Controls.Add(cmdPasteCustomSICList);
            fraCustomSICControls.Controls.Add(cmdCustomSICValuesPopulate);
            fraCustomSICControls.Controls.Add(cmdClearCustomSICList);
            fraCustomSICControls.Location = new System.Drawing.Point(666, 9);
            fraCustomSICControls.Name = "fraCustomSICControls";
            fraCustomSICControls.Size = new System.Drawing.Size(201, 305);
            fraCustomSICControls.TabIndex = 4;
            fraCustomSICControls.TabStop = false;
            //
            // lblCustomSICToleranceType
            //
            lblCustomSICToleranceType.Location = new System.Drawing.Point(6, 145);
            lblCustomSICToleranceType.Name = "lblCustomSICToleranceType";
            lblCustomSICToleranceType.Size = new System.Drawing.Size(106, 19);
            lblCustomSICToleranceType.TabIndex = 5;
            lblCustomSICToleranceType.Text = "Tolerance Type:";
            //
            // optCustomSICScanToleranceAcqTime
            //
            optCustomSICScanToleranceAcqTime.AutoSize = true;
            optCustomSICScanToleranceAcqTime.Location = new System.Drawing.Point(13, 215);
            optCustomSICScanToleranceAcqTime.Name = "optCustomSICScanToleranceAcqTime";
            optCustomSICScanToleranceAcqTime.Size = new System.Drawing.Size(190, 21);
            optCustomSICScanToleranceAcqTime.TabIndex = 8;
            optCustomSICScanToleranceAcqTime.Text = "Acquisition time (minutes)";
            optCustomSICScanToleranceAcqTime.UseVisualStyleBackColor = true;
            optCustomSICScanToleranceAcqTime.CheckedChanged += new EventHandler(optCustomSICScanToleranceAcqTime_CheckedChanged);
            //
            // optCustomSICScanToleranceRelative
            //
            optCustomSICScanToleranceRelative.AutoSize = true;
            optCustomSICScanToleranceRelative.Location = new System.Drawing.Point(13, 190);
            optCustomSICScanToleranceRelative.Name = "optCustomSICScanToleranceRelative";
            optCustomSICScanToleranceRelative.Size = new System.Drawing.Size(160, 21);
            optCustomSICScanToleranceRelative.TabIndex = 7;
            optCustomSICScanToleranceRelative.Text = "Relative time (0 to 1)";
            optCustomSICScanToleranceRelative.UseVisualStyleBackColor = true;
            optCustomSICScanToleranceRelative.CheckedChanged += new EventHandler(optCustomSICScanToleranceRelative_CheckedChanged);
            //
            // optCustomSICScanToleranceAbsolute
            //
            optCustomSICScanToleranceAbsolute.AutoSize = true;
            optCustomSICScanToleranceAbsolute.Checked = true;
            optCustomSICScanToleranceAbsolute.Location = new System.Drawing.Point(13, 166);
            optCustomSICScanToleranceAbsolute.Name = "optCustomSICScanToleranceAbsolute";
            optCustomSICScanToleranceAbsolute.Size = new System.Drawing.Size(170, 21);
            optCustomSICScanToleranceAbsolute.TabIndex = 6;
            optCustomSICScanToleranceAbsolute.TabStop = true;
            optCustomSICScanToleranceAbsolute.Text = "Absolute scan number";
            optCustomSICScanToleranceAbsolute.UseVisualStyleBackColor = true;
            optCustomSICScanToleranceAbsolute.CheckedChanged += new EventHandler(optCustomSICScanToleranceAbsolute_CheckedChanged);
            //
            // chkLimitSearchToCustomMZs
            //
            chkLimitSearchToCustomMZs.Location = new System.Drawing.Point(10, 249);
            chkLimitSearchToCustomMZs.Name = "chkLimitSearchToCustomMZs";
            chkLimitSearchToCustomMZs.Size = new System.Drawing.Size(182, 51);
            chkLimitSearchToCustomMZs.TabIndex = 9;
            chkLimitSearchToCustomMZs.Text = "Limit search to only use custom m/z values (skip auto-fragmented m/z's)";
            //
            // txtCustomSICScanOrAcqTimeTolerance
            //
            txtCustomSICScanOrAcqTimeTolerance.Location = new System.Drawing.Point(119, 114);
            txtCustomSICScanOrAcqTimeTolerance.Name = "txtCustomSICScanOrAcqTimeTolerance";
            txtCustomSICScanOrAcqTimeTolerance.Size = new System.Drawing.Size(67, 22);
            txtCustomSICScanOrAcqTimeTolerance.TabIndex = 4;
            txtCustomSICScanOrAcqTimeTolerance.Text = "3";
            txtCustomSICScanOrAcqTimeTolerance.KeyPress += new KeyPressEventHandler(txtCustomSICScanOrAcqTimeTolerance_KeyPress);
            //
            // lblCustomSICScanTolerance
            //
            lblCustomSICScanTolerance.Location = new System.Drawing.Point(6, 118);
            lblCustomSICScanTolerance.Name = "lblCustomSICScanTolerance";
            lblCustomSICScanTolerance.Size = new System.Drawing.Size(106, 18);
            lblCustomSICScanTolerance.TabIndex = 3;
            lblCustomSICScanTolerance.Text = "Scan Tolerance";
            //
            // cmdPasteCustomSICList
            //
            cmdPasteCustomSICList.Location = new System.Drawing.Point(10, 18);
            cmdPasteCustomSICList.Name = "cmdPasteCustomSICList";
            cmdPasteCustomSICList.Size = new System.Drawing.Size(80, 47);
            cmdPasteCustomSICList.TabIndex = 0;
            cmdPasteCustomSICList.Text = "Paste Values";
            cmdPasteCustomSICList.Click += new EventHandler(cmdPasteCustomSICList_Click);
            //
            // cmdCustomSICValuesPopulate
            //
            cmdCustomSICValuesPopulate.Location = new System.Drawing.Point(7, 72);
            cmdCustomSICValuesPopulate.Name = "cmdCustomSICValuesPopulate";
            cmdCustomSICValuesPopulate.Size = new System.Drawing.Size(183, 27);
            cmdCustomSICValuesPopulate.TabIndex = 2;
            cmdCustomSICValuesPopulate.Text = "Auto-Populate with Defaults";
            cmdCustomSICValuesPopulate.Click += new EventHandler(cmdCustomSICValuesPopulate_Click);
            //
            // cmdClearCustomSICList
            //
            cmdClearCustomSICList.Location = new System.Drawing.Point(107, 18);
            cmdClearCustomSICList.Name = "cmdClearCustomSICList";
            cmdClearCustomSICList.Size = new System.Drawing.Size(77, 47);
            cmdClearCustomSICList.TabIndex = 1;
            cmdClearCustomSICList.Text = "Clear List";
            cmdClearCustomSICList.Click += new EventHandler(cmdClearCustomSICList_Click);
            //
            // dgCustomSICValues
            //
            dgCustomSICValues.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgCustomSICValues.CaptionText = "Custom SIC Values";
            dgCustomSICValues.DataMember = "";
            dgCustomSICValues.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            dgCustomSICValues.Location = new System.Drawing.Point(10, 120);
            dgCustomSICValues.Name = "dgCustomSICValues";
            dgCustomSICValues.Size = new System.Drawing.Size(649, 194);
            dgCustomSICValues.TabIndex = 3;
            //
            // TabPageReporterIons
            //
            TabPageReporterIons.Controls.Add(fraDecoyOptions);
            TabPageReporterIons.Controls.Add(fraMRMOptions);
            TabPageReporterIons.Controls.Add(fraReporterIonMassMode);
            TabPageReporterIons.Controls.Add(fraReporterIonOptions);
            TabPageReporterIons.Location = new System.Drawing.Point(4, 25);
            TabPageReporterIons.Name = "TabPageReporterIons";
            TabPageReporterIons.Size = new System.Drawing.Size(882, 327);
            TabPageReporterIons.TabIndex = 9;
            TabPageReporterIons.Text = "Reporter Ions / MRM";
            TabPageReporterIons.UseVisualStyleBackColor = true;
            //
            // fraDecoyOptions
            //
            fraDecoyOptions.Controls.Add(lblParentIonDecoyMassDaUnits);
            fraDecoyOptions.Controls.Add(txtParentIonDecoyMassDa);
            fraDecoyOptions.Controls.Add(lblParentIonDecoyMassDa);
            fraDecoyOptions.Location = new System.Drawing.Point(638, 181);
            fraDecoyOptions.Name = "fraDecoyOptions";
            fraDecoyOptions.Size = new System.Drawing.Size(191, 86);
            fraDecoyOptions.TabIndex = 3;
            fraDecoyOptions.TabStop = false;
            fraDecoyOptions.Text = "Decoy Options";
            //
            // lblParentIonDecoyMassDaUnits
            //
            lblParentIonDecoyMassDaUnits.Location = new System.Drawing.Point(86, 52);
            lblParentIonDecoyMassDaUnits.Name = "lblParentIonDecoyMassDaUnits";
            lblParentIonDecoyMassDaUnits.Size = new System.Drawing.Size(41, 20);
            lblParentIonDecoyMassDaUnits.TabIndex = 2;
            lblParentIonDecoyMassDaUnits.Text = "Da";
            //
            // txtParentIonDecoyMassDa
            //
            txtParentIonDecoyMassDa.Location = new System.Drawing.Point(12, 48);
            txtParentIonDecoyMassDa.Name = "txtParentIonDecoyMassDa";
            txtParentIonDecoyMassDa.Size = new System.Drawing.Size(67, 22);
            txtParentIonDecoyMassDa.TabIndex = 1;
            txtParentIonDecoyMassDa.Text = "0";
            //
            // lblParentIonDecoyMassDa
            //
            lblParentIonDecoyMassDa.Location = new System.Drawing.Point(8, 27);
            lblParentIonDecoyMassDa.Name = "lblParentIonDecoyMassDa";
            lblParentIonDecoyMassDa.Size = new System.Drawing.Size(170, 18);
            lblParentIonDecoyMassDa.TabIndex = 0;
            lblParentIonDecoyMassDa.Text = "Parent Ion Decoy Mass";
            //
            // fraMRMOptions
            //
            fraMRMOptions.Controls.Add(chkMRMWriteIntensityCrosstab);
            fraMRMOptions.Controls.Add(lblMRMInfo);
            fraMRMOptions.Controls.Add(chkMRMWriteDataList);
            fraMRMOptions.Location = new System.Drawing.Point(379, 18);
            fraMRMOptions.Name = "fraMRMOptions";
            fraMRMOptions.Size = new System.Drawing.Size(409, 156);
            fraMRMOptions.TabIndex = 2;
            fraMRMOptions.TabStop = false;
            fraMRMOptions.Text = "MRM Options";
            //
            // chkMRMWriteIntensityCrosstab
            //
            chkMRMWriteIntensityCrosstab.Location = new System.Drawing.Point(23, 120);
            chkMRMWriteIntensityCrosstab.Name = "chkMRMWriteIntensityCrosstab";
            chkMRMWriteIntensityCrosstab.Size = new System.Drawing.Size(366, 21);
            chkMRMWriteIntensityCrosstab.TabIndex = 2;
            chkMRMWriteIntensityCrosstab.Text = "Save MRM intensity crosstab (wide, rectangular file)";
            //
            // lblMRMInfo
            //
            lblMRMInfo.Location = new System.Drawing.Point(7, 18);
            lblMRMInfo.Name = "lblMRMInfo";
            lblMRMInfo.Size = new System.Drawing.Size(395, 71);
            lblMRMInfo.TabIndex = 0;
            lblMRMInfo.Text = resources.GetString("lblMRMInfo.Text");
            //
            // chkMRMWriteDataList
            //
            chkMRMWriteDataList.Location = new System.Drawing.Point(23, 92);
            chkMRMWriteDataList.Name = "chkMRMWriteDataList";
            chkMRMWriteDataList.Size = new System.Drawing.Size(366, 21);
            chkMRMWriteDataList.TabIndex = 1;
            chkMRMWriteDataList.Text = "Save MRM data list (long, narrow file)";
            //
            // fraReporterIonMassMode
            //
            fraReporterIonMassMode.Controls.Add(cboReporterIonMassMode);
            fraReporterIonMassMode.Location = new System.Drawing.Point(19, 181);
            fraReporterIonMassMode.Name = "fraReporterIonMassMode";
            fraReporterIonMassMode.Size = new System.Drawing.Size(612, 86);
            fraReporterIonMassMode.TabIndex = 1;
            fraReporterIonMassMode.TabStop = false;
            fraReporterIonMassMode.Text = "Reporter Ion Mass Mode";
            //
            // cboReporterIonMassMode
            //
            cboReporterIonMassMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cboReporterIonMassMode.Location = new System.Drawing.Point(9, 27);
            cboReporterIonMassMode.Name = "cboReporterIonMassMode";
            cboReporterIonMassMode.Size = new System.Drawing.Size(592, 24);
            cboReporterIonMassMode.TabIndex = 13;
            cboReporterIonMassMode.SelectedIndexChanged += new EventHandler(cboReporterIonMassMode_SelectedIndexChanged);
            //
            // fraReporterIonOptions
            //
            fraReporterIonOptions.Controls.Add(chkReporterIonApplyAbundanceCorrection);
            fraReporterIonOptions.Controls.Add(chkReporterIonSaveUncorrectedIntensities);
            fraReporterIonOptions.Controls.Add(chkReporterIonSaveObservedMasses);
            fraReporterIonOptions.Controls.Add(txtReporterIonMZToleranceDa);
            fraReporterIonOptions.Controls.Add(lblReporterIonMZToleranceDa);
            fraReporterIonOptions.Controls.Add(chkReporterIonStatsEnabled);
            fraReporterIonOptions.Location = new System.Drawing.Point(19, 18);
            fraReporterIonOptions.Name = "fraReporterIonOptions";
            fraReporterIonOptions.Size = new System.Drawing.Size(339, 156);
            fraReporterIonOptions.TabIndex = 0;
            fraReporterIonOptions.TabStop = false;
            fraReporterIonOptions.Text = "Reporter Ion Options";
            //
            // chkReporterIonApplyAbundanceCorrection
            //
            chkReporterIonApplyAbundanceCorrection.Location = new System.Drawing.Point(19, 103);
            chkReporterIonApplyAbundanceCorrection.Name = "chkReporterIonApplyAbundanceCorrection";
            chkReporterIonApplyAbundanceCorrection.Size = new System.Drawing.Size(301, 20);
            chkReporterIonApplyAbundanceCorrection.TabIndex = 4;
            chkReporterIonApplyAbundanceCorrection.Text = "Apply iTraq isotopic abundance correction";
            //
            // chkReporterIonSaveUncorrectedIntensities
            //
            chkReporterIonSaveUncorrectedIntensities.Location = new System.Drawing.Point(38, 127);
            chkReporterIonSaveUncorrectedIntensities.Name = "chkReporterIonSaveUncorrectedIntensities";
            chkReporterIonSaveUncorrectedIntensities.Size = new System.Drawing.Size(269, 21);
            chkReporterIonSaveUncorrectedIntensities.TabIndex = 5;
            chkReporterIonSaveUncorrectedIntensities.Text = "Write original uncorrected intensities";
            //
            // chkReporterIonSaveObservedMasses
            //
            chkReporterIonSaveObservedMasses.Location = new System.Drawing.Point(19, 78);
            chkReporterIonSaveObservedMasses.Name = "chkReporterIonSaveObservedMasses";
            chkReporterIonSaveObservedMasses.Size = new System.Drawing.Size(301, 21);
            chkReporterIonSaveObservedMasses.TabIndex = 3;
            chkReporterIonSaveObservedMasses.Text = "Write observed m/z values to Reporter Ions file";
            //
            // txtReporterIonMZToleranceDa
            //
            txtReporterIonMZToleranceDa.Location = new System.Drawing.Point(205, 48);
            txtReporterIonMZToleranceDa.Name = "txtReporterIonMZToleranceDa";
            txtReporterIonMZToleranceDa.Size = new System.Drawing.Size(48, 22);
            txtReporterIonMZToleranceDa.TabIndex = 2;
            txtReporterIonMZToleranceDa.Text = "0.5";
            //
            // lblReporterIonMZToleranceDa
            //
            lblReporterIonMZToleranceDa.Location = new System.Drawing.Point(16, 52);
            lblReporterIonMZToleranceDa.Name = "lblReporterIonMZToleranceDa";
            lblReporterIonMZToleranceDa.Size = new System.Drawing.Size(182, 18);
            lblReporterIonMZToleranceDa.TabIndex = 1;
            lblReporterIonMZToleranceDa.Text = "m/z Tolerance Half Width";
            //
            // chkReporterIonStatsEnabled
            //
            chkReporterIonStatsEnabled.Location = new System.Drawing.Point(19, 28);
            chkReporterIonStatsEnabled.Name = "chkReporterIonStatsEnabled";
            chkReporterIonStatsEnabled.Size = new System.Drawing.Size(240, 20);
            chkReporterIonStatsEnabled.TabIndex = 0;
            chkReporterIonStatsEnabled.Text = "Generate Reporter Ion Stats";
            chkReporterIonStatsEnabled.CheckedChanged += new EventHandler(chkReporterIonStatsEnabled_CheckedChanged);
            //
            // TabPageAdvancedOptions
            //
            TabPageAdvancedOptions.Controls.Add(fraAdditionalInfoFiles);
            TabPageAdvancedOptions.Controls.Add(fraDatasetLookupInfo);
            TabPageAdvancedOptions.Controls.Add(fraMemoryConservationOptions);
            TabPageAdvancedOptions.Location = new System.Drawing.Point(4, 25);
            TabPageAdvancedOptions.Name = "TabPageAdvancedOptions";
            TabPageAdvancedOptions.Size = new System.Drawing.Size(882, 327);
            TabPageAdvancedOptions.TabIndex = 8;
            TabPageAdvancedOptions.Text = "Advanced";
            TabPageAdvancedOptions.UseVisualStyleBackColor = true;
            //
            // fraAdditionalInfoFiles
            //
            fraAdditionalInfoFiles.Controls.Add(chkConsolidateConstantExtendedHeaderValues);
            fraAdditionalInfoFiles.Controls.Add(lblStatusLogKeyNameFilterList);
            fraAdditionalInfoFiles.Controls.Add(txtStatusLogKeyNameFilterList);
            fraAdditionalInfoFiles.Controls.Add(chkSaveExtendedStatsFileIncludeStatusLog);
            fraAdditionalInfoFiles.Controls.Add(chkSaveExtendedStatsFileIncludeFilterText);
            fraAdditionalInfoFiles.Controls.Add(chkSaveMSTuneFile);
            fraAdditionalInfoFiles.Controls.Add(chkSaveMSMethodFile);
            fraAdditionalInfoFiles.Controls.Add(chkSaveExtendedStatsFile);
            fraAdditionalInfoFiles.Location = new System.Drawing.Point(382, 3);
            fraAdditionalInfoFiles.Name = "fraAdditionalInfoFiles";
            fraAdditionalInfoFiles.Size = new System.Drawing.Size(422, 135);
            fraAdditionalInfoFiles.TabIndex = 1;
            fraAdditionalInfoFiles.TabStop = false;
            fraAdditionalInfoFiles.Text = "Thermo Info Files";
            //
            // chkConsolidateConstantExtendedHeaderValues
            //
            chkConsolidateConstantExtendedHeaderValues.Checked = true;
            chkConsolidateConstantExtendedHeaderValues.CheckState = CheckState.Checked;
            chkConsolidateConstantExtendedHeaderValues.Location = new System.Drawing.Point(38, 112);
            chkConsolidateConstantExtendedHeaderValues.Name = "chkConsolidateConstantExtendedHeaderValues";
            chkConsolidateConstantExtendedHeaderValues.Size = new System.Drawing.Size(192, 21);
            chkConsolidateConstantExtendedHeaderValues.TabIndex = 5;
            chkConsolidateConstantExtendedHeaderValues.Text = "Consolidate constant values";
            //
            // lblStatusLogKeyNameFilterList
            //
            lblStatusLogKeyNameFilterList.Location = new System.Drawing.Point(230, 31);
            lblStatusLogKeyNameFilterList.Name = "lblStatusLogKeyNameFilterList";
            lblStatusLogKeyNameFilterList.Size = new System.Drawing.Size(176, 20);
            lblStatusLogKeyNameFilterList.TabIndex = 6;
            lblStatusLogKeyNameFilterList.Text = "Status Log Keys to Include";
            //
            // txtStatusLogKeyNameFilterList
            //
            txtStatusLogKeyNameFilterList.Location = new System.Drawing.Point(234, 54);
            txtStatusLogKeyNameFilterList.Multiline = true;
            txtStatusLogKeyNameFilterList.Name = "txtStatusLogKeyNameFilterList";
            txtStatusLogKeyNameFilterList.ScrollBars = ScrollBars.Vertical;
            txtStatusLogKeyNameFilterList.Size = new System.Drawing.Size(179, 58);
            txtStatusLogKeyNameFilterList.TabIndex = 7;
            //
            // chkSaveExtendedStatsFileIncludeStatusLog
            //
            chkSaveExtendedStatsFileIncludeStatusLog.Location = new System.Drawing.Point(38, 92);
            chkSaveExtendedStatsFileIncludeStatusLog.Name = "chkSaveExtendedStatsFileIncludeStatusLog";
            chkSaveExtendedStatsFileIncludeStatusLog.Size = new System.Drawing.Size(192, 21);
            chkSaveExtendedStatsFileIncludeStatusLog.TabIndex = 4;
            chkSaveExtendedStatsFileIncludeStatusLog.Text = "Include voltage, temp., etc.";
            chkSaveExtendedStatsFileIncludeStatusLog.CheckedChanged += new EventHandler(chkSaveExtendedStatsFileIncludeStatusLog_CheckedChanged);
            //
            // chkSaveExtendedStatsFileIncludeFilterText
            //
            chkSaveExtendedStatsFileIncludeFilterText.Checked = true;
            chkSaveExtendedStatsFileIncludeFilterText.CheckState = CheckState.Checked;
            chkSaveExtendedStatsFileIncludeFilterText.Location = new System.Drawing.Point(38, 74);
            chkSaveExtendedStatsFileIncludeFilterText.Name = "chkSaveExtendedStatsFileIncludeFilterText";
            chkSaveExtendedStatsFileIncludeFilterText.Size = new System.Drawing.Size(192, 18);
            chkSaveExtendedStatsFileIncludeFilterText.TabIndex = 3;
            chkSaveExtendedStatsFileIncludeFilterText.Text = "Include Scan Filter Text";
            //
            // chkSaveMSTuneFile
            //
            chkSaveMSTuneFile.Location = new System.Drawing.Point(19, 37);
            chkSaveMSTuneFile.Name = "chkSaveMSTuneFile";
            chkSaveMSTuneFile.Size = new System.Drawing.Size(211, 18);
            chkSaveMSTuneFile.TabIndex = 1;
            chkSaveMSTuneFile.Text = "Save MS Tune File";
            //
            // chkSaveMSMethodFile
            //
            chkSaveMSMethodFile.Checked = true;
            chkSaveMSMethodFile.CheckState = CheckState.Checked;
            chkSaveMSMethodFile.Location = new System.Drawing.Point(19, 18);
            chkSaveMSMethodFile.Name = "chkSaveMSMethodFile";
            chkSaveMSMethodFile.Size = new System.Drawing.Size(211, 19);
            chkSaveMSMethodFile.TabIndex = 0;
            chkSaveMSMethodFile.Text = "Save MS Method File";
            //
            // chkSaveExtendedStatsFile
            //
            chkSaveExtendedStatsFile.Checked = true;
            chkSaveExtendedStatsFile.CheckState = CheckState.Checked;
            chkSaveExtendedStatsFile.Location = new System.Drawing.Point(19, 55);
            chkSaveExtendedStatsFile.Name = "chkSaveExtendedStatsFile";
            chkSaveExtendedStatsFile.Size = new System.Drawing.Size(211, 19);
            chkSaveExtendedStatsFile.TabIndex = 2;
            chkSaveExtendedStatsFile.Text = "Save Extended Stats File";
            chkSaveExtendedStatsFile.CheckedChanged += new EventHandler(chkSaveExtendedStatsFile_CheckedChanged);
            //
            // fraDatasetLookupInfo
            //
            fraDatasetLookupInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            fraDatasetLookupInfo.Controls.Add(cmdSetConnectionStringToPNNLServer);
            fraDatasetLookupInfo.Controls.Add(txtDatasetInfoQuerySQL);
            fraDatasetLookupInfo.Controls.Add(lblDatasetInfoQuerySQL);
            fraDatasetLookupInfo.Controls.Add(txtDatabaseConnectionString);
            fraDatasetLookupInfo.Controls.Add(lblDatabaseConnectionString);
            fraDatasetLookupInfo.Controls.Add(lblDatasetLookupFilePath);
            fraDatasetLookupInfo.Controls.Add(cmdSelectDatasetLookupFile);
            fraDatasetLookupInfo.Controls.Add(txtDatasetLookupFilePath);
            fraDatasetLookupInfo.Location = new System.Drawing.Point(19, 138);
            fraDatasetLookupInfo.Name = "fraDatasetLookupInfo";
            fraDatasetLookupInfo.Size = new System.Drawing.Size(852, 176);
            fraDatasetLookupInfo.TabIndex = 1;
            fraDatasetLookupInfo.TabStop = false;
            fraDatasetLookupInfo.Text = "Dataset ID lookup information";
            //
            // cmdSetConnectionStringToPNNLServer
            //
            cmdSetConnectionStringToPNNLServer.Location = new System.Drawing.Point(19, 23);
            cmdSetConnectionStringToPNNLServer.Name = "cmdSetConnectionStringToPNNLServer";
            cmdSetConnectionStringToPNNLServer.Size = new System.Drawing.Size(144, 23);
            cmdSetConnectionStringToPNNLServer.TabIndex = 0;
            cmdSetConnectionStringToPNNLServer.Text = "Set to PNNL Server";
            cmdSetConnectionStringToPNNLServer.Click += new EventHandler(cmdSetConnectionStringToPNNLServer_Click);
            //
            // txtDatasetInfoQuerySQL
            //
            txtDatasetInfoQuerySQL.Location = new System.Drawing.Point(202, 74);
            txtDatasetInfoQuerySQL.Name = "txtDatasetInfoQuerySQL";
            txtDatasetInfoQuerySQL.Size = new System.Drawing.Size(499, 22);
            txtDatasetInfoQuerySQL.TabIndex = 4;
            //
            // lblDatasetInfoQuerySQL
            //
            lblDatasetInfoQuerySQL.Location = new System.Drawing.Point(10, 80);
            lblDatasetInfoQuerySQL.Name = "lblDatasetInfoQuerySQL";
            lblDatasetInfoQuerySQL.Size = new System.Drawing.Size(192, 18);
            lblDatasetInfoQuerySQL.TabIndex = 3;
            lblDatasetInfoQuerySQL.Text = "Dataset Info Query SQL:";
            //
            // txtDatabaseConnectionString
            //
            txtDatabaseConnectionString.Location = new System.Drawing.Point(202, 46);
            txtDatabaseConnectionString.Name = "txtDatabaseConnectionString";
            txtDatabaseConnectionString.Size = new System.Drawing.Size(499, 22);
            txtDatabaseConnectionString.TabIndex = 2;
            //
            // lblDatabaseConnectionString
            //
            lblDatabaseConnectionString.Location = new System.Drawing.Point(10, 52);
            lblDatabaseConnectionString.Name = "lblDatabaseConnectionString";
            lblDatabaseConnectionString.Size = new System.Drawing.Size(192, 18);
            lblDatabaseConnectionString.TabIndex = 1;
            lblDatabaseConnectionString.Text = "SQL Server Connection String:";
            //
            // lblDatasetLookupFilePath
            //
            lblDatasetLookupFilePath.Location = new System.Drawing.Point(10, 111);
            lblDatasetLookupFilePath.Name = "lblDatasetLookupFilePath";
            lblDatasetLookupFilePath.Size = new System.Drawing.Size(633, 18);
            lblDatasetLookupFilePath.TabIndex = 5;
            lblDatasetLookupFilePath.Text = "Dataset lookup file (dataset name and dataset ID number, tab-separated); used if " + "DB not available";
            //
            // cmdSelectDatasetLookupFile
            //
            cmdSelectDatasetLookupFile.Location = new System.Drawing.Point(10, 138);
            cmdSelectDatasetLookupFile.Name = "cmdSelectDatasetLookupFile";
            cmdSelectDatasetLookupFile.Size = new System.Drawing.Size(96, 28);
            cmdSelectDatasetLookupFile.TabIndex = 6;
            cmdSelectDatasetLookupFile.Text = "Select File";
            cmdSelectDatasetLookupFile.Click += new EventHandler(cmdSelectDatasetLookupFile_Click);
            //
            // txtDatasetLookupFilePath
            //
            txtDatasetLookupFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtDatasetLookupFilePath.Location = new System.Drawing.Point(125, 138);
            txtDatasetLookupFilePath.Name = "txtDatasetLookupFilePath";
            txtDatasetLookupFilePath.Size = new System.Drawing.Size(708, 22);
            txtDatasetLookupFilePath.TabIndex = 7;
            //
            // fraMemoryConservationOptions
            //
            fraMemoryConservationOptions.Controls.Add(chkSkipMSMSProcessing);
            fraMemoryConservationOptions.Controls.Add(chkSkipSICAndRawDataProcessing);
            fraMemoryConservationOptions.Controls.Add(chkExportRawDataOnly);
            fraMemoryConservationOptions.Location = new System.Drawing.Point(19, 18);
            fraMemoryConservationOptions.Name = "fraMemoryConservationOptions";
            fraMemoryConservationOptions.Size = new System.Drawing.Size(355, 120);
            fraMemoryConservationOptions.TabIndex = 0;
            fraMemoryConservationOptions.TabStop = false;
            fraMemoryConservationOptions.Text = "Memory Usage and Speed Options";
            //
            // chkSkipMSMSProcessing
            //
            chkSkipMSMSProcessing.Location = new System.Drawing.Point(19, 22);
            chkSkipMSMSProcessing.Name = "chkSkipMSMSProcessing";
            chkSkipMSMSProcessing.Size = new System.Drawing.Size(317, 21);
            chkSkipMSMSProcessing.TabIndex = 0;
            chkSkipMSMSProcessing.Text = "Skip MS/MS Processing (no similarity testing)";
            chkSkipMSMSProcessing.CheckedChanged += new EventHandler(chkSkipMSMSProcessing_CheckedChanged);
            //
            // chkSkipSICAndRawDataProcessing
            //
            chkSkipSICAndRawDataProcessing.Location = new System.Drawing.Point(19, 46);
            chkSkipSICAndRawDataProcessing.Name = "chkSkipSICAndRawDataProcessing";
            chkSkipSICAndRawDataProcessing.Size = new System.Drawing.Size(261, 39);
            chkSkipSICAndRawDataProcessing.TabIndex = 1;
            chkSkipSICAndRawDataProcessing.Text = "Only Export Chromatograms and Scan Stats (no SICs or raw data)";
            chkSkipSICAndRawDataProcessing.CheckedChanged += new EventHandler(chkSkipSICAndRawDataProcessing_CheckedChanged);
            //
            // chkExportRawDataOnly
            //
            chkExportRawDataOnly.Location = new System.Drawing.Point(19, 91);
            chkExportRawDataOnly.Name = "chkExportRawDataOnly";
            chkExportRawDataOnly.Size = new System.Drawing.Size(240, 21);
            chkExportRawDataOnly.TabIndex = 2;
            chkExportRawDataOnly.Text = "Export Raw Data Only (No SICs)";
            chkExportRawDataOnly.CheckedChanged += new EventHandler(chkExportRawDataOnly_CheckedChanged);
            //
            // TabPageLog
            //
            TabPageLog.Controls.Add(txtLogMessages);
            TabPageLog.Location = new System.Drawing.Point(4, 25);
            TabPageLog.Name = "TabPageLog";
            TabPageLog.Padding = new Padding(3);
            TabPageLog.Size = new System.Drawing.Size(882, 327);
            TabPageLog.TabIndex = 10;
            TabPageLog.Text = "Log";
            TabPageLog.UseVisualStyleBackColor = true;
            //
            // txtLogMessages
            //
            txtLogMessages.Location = new System.Drawing.Point(6, 6);
            txtLogMessages.Multiline = true;
            txtLogMessages.Name = "txtLogMessages";
            txtLogMessages.ReadOnly = true;
            txtLogMessages.ScrollBars = ScrollBars.Vertical;
            txtLogMessages.Size = new System.Drawing.Size(870, 315);
            txtLogMessages.TabIndex = 1;
            txtLogMessages.Text = "No log messages.";
            //
            // fraOutputDirectoryPath
            //
            fraOutputDirectoryPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            fraOutputDirectoryPath.Controls.Add(cmdStartProcessing);
            fraOutputDirectoryPath.Controls.Add(cmdSelectOutputDirectory);
            fraOutputDirectoryPath.Controls.Add(txtOutputDirectoryPath);
            fraOutputDirectoryPath.Location = new System.Drawing.Point(10, 92);
            fraOutputDirectoryPath.Name = "fraOutputDirectoryPath";
            fraOutputDirectoryPath.Size = new System.Drawing.Size(885, 102);
            fraOutputDirectoryPath.TabIndex = 1;
            fraOutputDirectoryPath.TabStop = false;
            fraOutputDirectoryPath.Text = "Output Folder Path";
            //
            // cmdStartProcessing
            //
            cmdStartProcessing.Location = new System.Drawing.Point(326, 65);
            cmdStartProcessing.Name = "cmdStartProcessing";
            cmdStartProcessing.Size = new System.Drawing.Size(133, 27);
            cmdStartProcessing.TabIndex = 2;
            cmdStartProcessing.Text = "Start &Processing";
            cmdStartProcessing.Click += new EventHandler(cmdStartProcessing_Click);
            //
            // cmdSelectOutputDirectory
            //
            cmdSelectOutputDirectory.Location = new System.Drawing.Point(10, 28);
            cmdSelectOutputDirectory.Name = "cmdSelectOutputDirectory";
            cmdSelectOutputDirectory.Size = new System.Drawing.Size(96, 44);
            cmdSelectOutputDirectory.TabIndex = 0;
            cmdSelectOutputDirectory.Text = "Select &Directory";
            cmdSelectOutputDirectory.Click += new EventHandler(cmdSelectOutputDirectory_Click);
            //
            // txtOutputDirectoryPath
            //
            txtOutputDirectoryPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtOutputDirectoryPath.Location = new System.Drawing.Point(125, 30);
            txtOutputDirectoryPath.Name = "txtOutputDirectoryPath";
            txtOutputDirectoryPath.Size = new System.Drawing.Size(741, 22);
            txtOutputDirectoryPath.TabIndex = 1;
            //
            // frmMain
            //
            AutoScaleBaseSize = new System.Drawing.Size(6, 15);
            ClientSize = new System.Drawing.Size(904, 585);
            Controls.Add(fraOutputDirectoryPath);
            Controls.Add(tbsOptions);
            Controls.Add(fraInputFilePath);
            Menu = MainMenuControl;
            MinimumSize = new System.Drawing.Size(540, 0);
            Name = "frmMain";
            Text = "MASIC";
            fraInputFilePath.ResumeLayout(false);
            fraInputFilePath.PerformLayout();
            tbsOptions.ResumeLayout(false);
            TabPageMasicExportOptions.ResumeLayout(false);
            TabPageMasicExportOptions.PerformLayout();
            fraExportAllSpectraDataPoints.ResumeLayout(false);
            fraExportAllSpectraDataPoints.PerformLayout();
            TabPageSICOptions.ResumeLayout(false);
            fraInputFileRangeFilters.ResumeLayout(false);
            fraInputFileRangeFilters.PerformLayout();
            fraSICSearchThresholds.ResumeLayout(false);
            fraSICSearchThresholds.PerformLayout();
            TabPagePeakFindingOptions.ResumeLayout(false);
            fraSICNoiseThresholds.ResumeLayout(false);
            fraSICNoiseThresholds.PerformLayout();
            fraSmoothingOptions.ResumeLayout(false);
            fraSmoothingOptions.PerformLayout();
            fraPeakFindingOptions.ResumeLayout(false);
            fraPeakFindingOptions.PerformLayout();
            TabPageBinningAndSimilarityOptions.ResumeLayout(false);
            fraMassSpectraNoiseThresholds.ResumeLayout(false);
            fraMassSpectraNoiseThresholds.PerformLayout();
            fraBinningIntensityOptions.ResumeLayout(false);
            fraBinningIntensityOptions.PerformLayout();
            fraSpectrumSimilarityOptions.ResumeLayout(false);
            fraSpectrumSimilarityOptions.PerformLayout();
            fraBinningMZOptions.ResumeLayout(false);
            fraBinningMZOptions.PerformLayout();
            TabPageCustomSICOptions.ResumeLayout(false);
            TabPageCustomSICOptions.PerformLayout();
            fraCustomSICControls.ResumeLayout(false);
            fraCustomSICControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgCustomSICValues).EndInit();
            TabPageReporterIons.ResumeLayout(false);
            fraDecoyOptions.ResumeLayout(false);
            fraDecoyOptions.PerformLayout();
            fraMRMOptions.ResumeLayout(false);
            fraReporterIonMassMode.ResumeLayout(false);
            fraReporterIonOptions.ResumeLayout(false);
            fraReporterIonOptions.PerformLayout();
            TabPageAdvancedOptions.ResumeLayout(false);
            fraAdditionalInfoFiles.ResumeLayout(false);
            fraAdditionalInfoFiles.PerformLayout();
            fraDatasetLookupInfo.ResumeLayout(false);
            fraDatasetLookupInfo.PerformLayout();
            fraMemoryConservationOptions.ResumeLayout(false);
            TabPageLog.ResumeLayout(false);
            TabPageLog.PerformLayout();
            fraOutputDirectoryPath.ResumeLayout(false);
            fraOutputDirectoryPath.PerformLayout();
            Resize += new EventHandler(frmMain_Resize);
            Load += new EventHandler(frmMain_Load);
            Closing += new System.ComponentModel.CancelEventHandler(frmMain_Closing);
            ResumeLayout(false);
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
