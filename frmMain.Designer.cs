using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace MASIC
{
    [DesignerGenerated()]
    public partial class frmMain : Form
    {
        // Form overrides dispose to clean up the component list.
        [DebuggerNonUserCode()]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && components is object)
                {
                    components.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // Required by the Windows Form Designer
        private System.ComponentModel.IContainer components;

        // NOTE: The following procedure is required by the Windows Form Designer
        // It can be modified using the Windows Form Designer.
        // Do not modify it using the code editor.
        [DebuggerStepThrough()]
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            _txtInputFilePath = new TextBox();
            _cmdSelectFile = new Button();
            _cmdSelectFile.Click += new EventHandler(cmdSelectFile_Click);
            _fraInputFilePath = new GroupBox();
            _MainMenuControl = new MainMenu(components);
            _mnuFile = new MenuItem();
            _mnuFileSelectInputFile = new MenuItem();
            _mnuFileSelectInputFile.Click += new EventHandler(mnuFileSelectInputFile_Click);
            _mnuFileSelectOutputDirectory = new MenuItem();
            _mnuFileSelectOutputDirectory.Click += new EventHandler(mnuFileSelectOutputDirectory_Click);
            _mnuFileSep1 = new MenuItem();
            _mnuFileLoadOptions = new MenuItem();
            _mnuFileLoadOptions.Click += new EventHandler(mnuFileLoadOptions_Click);
            _mnuFileSaveOptions = new MenuItem();
            _mnuFileSaveOptions.Click += new EventHandler(mnuFileSaveOptions_Click);
            _mnuFileSep2 = new MenuItem();
            _mnuFileExit = new MenuItem();
            _mnuFileExit.Click += new EventHandler(mnuFileExit_Click);
            _mnuEdit = new MenuItem();
            _mnuEditProcessFile = new MenuItem();
            _mnuEditProcessFile.Click += new EventHandler(mnuEditProcessFile_Click);
            _mnuEditSep1 = new MenuItem();
            _mnuEditSaveDefaultOptions = new MenuItem();
            _mnuEditSaveDefaultOptions.Click += new EventHandler(mnuEditSaveDefaultOptions_Click);
            _mnuEditResetOptions = new MenuItem();
            _mnuEditResetOptions.Click += new EventHandler(mnuEditResetOptions_Click);
            _mnuHelp = new MenuItem();
            _mnuHelpAbout = new MenuItem();
            _mnuHelpAbout.Click += new EventHandler(mnuHelpAbout_Click);
            _tbsOptions = new TabControl();
            _TabPageMasicExportOptions = new TabPage();
            _chkWriteDetailedSICDataFile = new CheckBox();
            _chkIncludeScanTimesInSICStatsFile = new CheckBox();
            _txtDatasetID = new TextBox();
            _txtDatasetID.KeyPress += new KeyPressEventHandler(txtDatasetID_KeyPress);
            _lblDatasetID = new Label();
            _lblRawDataExportOverview = new Label();
            _fraExportAllSpectraDataPoints = new GroupBox();
            _txtExportRawDataSignalToNoiseRatioMinimum = new TextBox();
            _txtExportRawDataSignalToNoiseRatioMinimum.KeyPress += new KeyPressEventHandler(txtExportRawDataSignalToNoiseRatioMinimum_KeyPress);
            _lblExportRawDataSignalToNoiseRatioMinimum = new Label();
            _chkExportRawDataRenumberScans = new CheckBox();
            _txtExportRawDataMaxIonCountPerScan = new TextBox();
            _txtExportRawDataMaxIonCountPerScan.KeyPress += new KeyPressEventHandler(txtExportRawDataMaxIonCountPerScan_KeyPress);
            _lblExportRawDataMaxIonCountPerScan = new Label();
            _txtExportRawDataIntensityMinimum = new TextBox();
            _txtExportRawDataIntensityMinimum.KeyPress += new KeyPressEventHandler(txtExportRawDataIntensityMinimum_KeyPress);
            _lblExportRawDataIntensityMinimum = new Label();
            _chkExportRawDataIncludeMSMS = new CheckBox();
            _chkExportRawDataIncludeMSMS.CheckedChanged += new EventHandler(chkExportRawDataIncludeMSMS_CheckedChanged);
            _cboExportRawDataFileFormat = new ComboBox();
            _lblExportDataPointsFormat = new Label();
            _chkExportRawSpectraData = new CheckBox();
            _chkExportRawSpectraData.CheckedChanged += new EventHandler(chkExportRawSpectraData_CheckedChanged);
            _chkIncludeHeaders = new CheckBox();
            _TabPageSICOptions = new TabPage();
            _fraInputFileRangeFilters = new GroupBox();
            _lblTimeEndUnits = new Label();
            _lblTimeStartUnits = new Label();
            _txtTimeEnd = new TextBox();
            _txtTimeEnd.KeyPress += new KeyPressEventHandler(txtTimeEnd_KeyPress);
            _txtTimeStart = new TextBox();
            _txtTimeStart.KeyPress += new KeyPressEventHandler(txtTimeStart_KeyPress);
            _lblTimeEnd = new Label();
            _lblTimeStart = new Label();
            _txtScanEnd = new TextBox();
            _txtScanEnd.KeyPress += new KeyPressEventHandler(txtScanEnd_KeyPress);
            _txtScanStart = new TextBox();
            _txtScanStart.KeyPress += new KeyPressEventHandler(txtScanStart_KeyPress);
            _lblScanEnd = new Label();
            _lblScanStart = new Label();
            _cmdClearAllRangeFilters = new Button();
            _cmdClearAllRangeFilters.Click += new EventHandler(cmdClearAllRangeFilters_Click);
            _lblSICOptionsOverview = new Label();
            _fraSICSearchThresholds = new GroupBox();
            _optSICTolerancePPM = new RadioButton();
            _optSICToleranceDa = new RadioButton();
            _chkRefineReportedParentIonMZ = new CheckBox();
            _chkReplaceSICZeroesWithMinimumPositiveValueFromMSData = new CheckBox();
            _txtMaxPeakWidthMinutesForward = new TextBox();
            _txtMaxPeakWidthMinutesForward.KeyPress += new KeyPressEventHandler(txtMaxPeakWidthMinutesForward_KeyPress);
            _txtMaxPeakWidthMinutesBackward = new TextBox();
            _txtMaxPeakWidthMinutesBackward.KeyPress += new KeyPressEventHandler(txtMaxPeakWidthMinutesBackward_KeyPress);
            _txtIntensityThresholdFractionMax = new TextBox();
            _txtIntensityThresholdFractionMax.KeyPress += new KeyPressEventHandler(txtIntensityThresholdFractionMax_KeyPress);
            _lblIntensityThresholdFractionMax = new Label();
            _txtIntensityThresholdAbsoluteMinimum = new TextBox();
            _txtIntensityThresholdAbsoluteMinimum.KeyPress += new KeyPressEventHandler(txtIntensityThresholdAbsoluteMinimum_KeyPress);
            _lblIntensityThresholdAbsoluteMinimum = new Label();
            _lblMaxPeakWidthMinutesForward = new Label();
            _lblMaxPeakWidthMinutesBackward = new Label();
            _lblMaxPeakWidthMinutes = new Label();
            _txtSICTolerance = new TextBox();
            _txtSICTolerance.KeyPress += new KeyPressEventHandler(txtSICTolerance_KeyPress);
            _lblSICToleranceDa = new Label();
            _TabPagePeakFindingOptions = new TabPage();
            _fraSICNoiseThresholds = new GroupBox();
            _cboSICNoiseThresholdMode = new ComboBox();
            _cboSICNoiseThresholdMode.SelectedIndexChanged += new EventHandler(cboSICNoiseThresholdMode_SelectedIndexChanged);
            _lblNoiseThresholdMode = new Label();
            _txtSICNoiseFractionLowIntensityDataToAverage = new TextBox();
            _txtSICNoiseFractionLowIntensityDataToAverage.KeyPress += new KeyPressEventHandler(txtSICNoiseFractionLowIntensityDataToAverage_KeyPress);
            _lblSICNoiseFractionLowIntensityDataToAverage = new Label();
            _txtSICNoiseThresholdIntensity = new TextBox();
            _txtSICNoiseThresholdIntensity.KeyPress += new KeyPressEventHandler(txtSICNoiseThresholdIntensity_KeyPress);
            _lblSICNoiseThresholdIntensity = new Label();
            _fraSmoothingOptions = new GroupBox();
            _chkSmoothDataRegardlessOfMinimumPeakWidth = new CheckBox();
            _chkFindPeaksOnSmoothedData = new CheckBox();
            _optUseSavitzkyGolaySmooth = new RadioButton();
            _optUseSavitzkyGolaySmooth.CheckedChanged += new EventHandler(optUseSavitzkyGolaySmooth_CheckedChanged);
            _txtButterworthSamplingFrequency = new TextBox();
            _txtButterworthSamplingFrequency.KeyPress += new KeyPressEventHandler(txtButterworthSamplingFrequency_KeyPress);
            _txtButterworthSamplingFrequency.Validating += new System.ComponentModel.CancelEventHandler(txtButterworthSamplingFrequency_Validating);
            _lblButterworthSamplingFrequency = new Label();
            _txtSavitzkyGolayFilterOrder = new TextBox();
            _txtSavitzkyGolayFilterOrder.KeyPress += new KeyPressEventHandler(txtSavitzkyGolayFilterOrder_KeyPress);
            _txtSavitzkyGolayFilterOrder.Validating += new System.ComponentModel.CancelEventHandler(txtSavitzkyGolayFilterOrder_Validating);
            _lblSavitzkyGolayFilterOrder = new Label();
            _optUseButterworthSmooth = new RadioButton();
            _optUseButterworthSmooth.CheckedChanged += new EventHandler(optUseButterworthSmooth_CheckedChanged);
            _fraPeakFindingOptions = new GroupBox();
            _txtInitialPeakWidthScansMaximum = new TextBox();
            _txtInitialPeakWidthScansMaximum.KeyPress += new KeyPressEventHandler(txtInitialPeakWidthScansMaximum_KeyPress);
            _lblInitialPeakWidthScansMaximum = new Label();
            _txtInitialPeakWidthScansScaler = new TextBox();
            _txtInitialPeakWidthScansScaler.KeyPress += new KeyPressEventHandler(txtInitialPeakWidthScansScaler_KeyPress);
            _lblInitialPeakWidthScansScaler = new Label();
            _txtMaxAllowedUpwardSpikeFractionMax = new TextBox();
            _txtMaxAllowedUpwardSpikeFractionMax.KeyPress += new KeyPressEventHandler(txtMaxAllowedUpwardSpikeFractionMax_KeyPress);
            _lblMaxAllowedUpwardSpikeFractionMax = new Label();
            _txtMaxDistanceScansNoOverlap = new TextBox();
            _txtMaxDistanceScansNoOverlap.KeyPress += new KeyPressEventHandler(txtMaxDistanceScansNoOverlap_KeyPress);
            _lblMaxDistanceScansNoOverlap = new Label();
            _TabPageBinningAndSimilarityOptions = new TabPage();
            _fraMassSpectraNoiseThresholds = new GroupBox();
            _txtMassSpectraNoiseMinimumSignalToNoiseRatio = new TextBox();
            _lblMassSpectraNoiseMinimumSignalToNoiseRatio = new Label();
            _txtMassSpectraNoiseThresholdIntensity = new TextBox();
            _txtMassSpectraNoiseThresholdIntensity.KeyPress += new KeyPressEventHandler(txtMassSpectraNoiseThresholdIntensity_KeyPress);
            _txtMassSpectraNoiseFractionLowIntensityDataToAverage = new TextBox();
            _txtMassSpectraNoiseFractionLowIntensityDataToAverage.KeyPress += new KeyPressEventHandler(txtMassSpectraNoiseFractionLowIntensityDataToAverage_KeyPress);
            _lblMassSpectraNoiseFractionLowIntensityDataToAverage = new Label();
            _cboMassSpectraNoiseThresholdMode = new ComboBox();
            _cboMassSpectraNoiseThresholdMode.SelectedIndexChanged += new EventHandler(cboMassSpectraNoiseThresholdMode_SelectedIndexChanged);
            _lblMassSpectraNoiseThresholdMode = new Label();
            _lblMassSpectraNoiseThresholdIntensity = new Label();
            _fraBinningIntensityOptions = new GroupBox();
            _lblBinnedDataIntensityPrecisionPctUnits = new Label();
            _chkBinnedDataSumAllIntensitiesForBin = new CheckBox();
            _chkBinnedDataNormalize = new CheckBox();
            _txtBinnedDataIntensityPrecisionPct = new TextBox();
            _txtBinnedDataIntensityPrecisionPct.KeyPress += new KeyPressEventHandler(txtBinnedDataIntensityPrecisionPct_KeyPress);
            _lblBinnedDataIntensityPrecisionPct = new Label();
            _fraSpectrumSimilarityOptions = new GroupBox();
            _lblSimilarIonTimeToleranceHalfWidthUnits = new Label();
            _txtSpectrumSimilarityMinimum = new TextBox();
            _txtSpectrumSimilarityMinimum.KeyPress += new KeyPressEventHandler(txtSpectrumSimilarityMinimum_KeyPress);
            _lblSpectrumSimilarityMinimum = new Label();
            _txtSimilarIonToleranceHalfWidthMinutes = new TextBox();
            _txtSimilarIonToleranceHalfWidthMinutes.KeyPress += new KeyPressEventHandler(txtSimilarIonToleranceHalfWidthMinutes_KeyPress);
            _lblSimilarIonTimeToleranceHalfWidth = new Label();
            _txtSimilarIonMZToleranceHalfWidth = new TextBox();
            _txtSimilarIonMZToleranceHalfWidth.KeyPress += new KeyPressEventHandler(txtSimilarIonMZToleranceHalfWidth_KeyPress);
            _lblSimilarIonMZToleranceHalfWidth = new Label();
            _fraBinningMZOptions = new GroupBox();
            _txtMaximumBinCount = new TextBox();
            _txtMaximumBinCount.KeyPress += new KeyPressEventHandler(txtMaximumBinCount_KeyPress);
            _lblMaximumBinCount = new Label();
            _txtBinSize = new TextBox();
            _txtBinSize.KeyPress += new KeyPressEventHandler(txtBinSize_KeyPress);
            _lblBinSize = new Label();
            _txtBinEndX = new TextBox();
            _txtBinEndX.KeyPress += new KeyPressEventHandler(txtBinEndX_KeyPress);
            _lblBinEndX = new Label();
            _txtBinStartX = new TextBox();
            _txtBinStartX.KeyPress += new KeyPressEventHandler(txtBinStartX_KeyPress);
            _lblBinStartX = new Label();
            _TabPageCustomSICOptions = new TabPage();
            _txtCustomSICFileDescription = new TextBox();
            _txtCustomSICFileDescription.KeyDown += new KeyEventHandler(txtCustomSICFileDescription_KeyDown);
            _cmdSelectCustomSICFile = new Button();
            _cmdSelectCustomSICFile.Click += new EventHandler(cmdSelectCustomSICFile_Click);
            _txtCustomSICFileName = new TextBox();
            _txtCustomSICFileName.TextChanged += new EventHandler(txtCustomSICFileName_TextChanged);
            _fraCustomSICControls = new GroupBox();
            _lblCustomSICToleranceType = new Label();
            _optCustomSICScanToleranceAcqTime = new RadioButton();
            _optCustomSICScanToleranceAcqTime.CheckedChanged += new EventHandler(optCustomSICScanToleranceAcqTime_CheckedChanged);
            _optCustomSICScanToleranceRelative = new RadioButton();
            _optCustomSICScanToleranceRelative.CheckedChanged += new EventHandler(optCustomSICScanToleranceRelative_CheckedChanged);
            _optCustomSICScanToleranceAbsolute = new RadioButton();
            _optCustomSICScanToleranceAbsolute.CheckedChanged += new EventHandler(optCustomSICScanToleranceAbsolute_CheckedChanged);
            _chkLimitSearchToCustomMZs = new CheckBox();
            _txtCustomSICScanOrAcqTimeTolerance = new TextBox();
            _txtCustomSICScanOrAcqTimeTolerance.KeyPress += new KeyPressEventHandler(txtCustomSICScanOrAcqTimeTolerance_KeyPress);
            _lblCustomSICScanTolerance = new Label();
            _cmdPasteCustomSICList = new Button();
            _cmdPasteCustomSICList.Click += new EventHandler(cmdPasteCustomSICList_Click);
            _cmdCustomSICValuesPopulate = new Button();
            _cmdCustomSICValuesPopulate.Click += new EventHandler(cmdCustomSICValuesPopulate_Click);
            _cmdClearCustomSICList = new Button();
            _cmdClearCustomSICList.Click += new EventHandler(cmdClearCustomSICList_Click);
            _dgCustomSICValues = new DataGrid();
            _TabPageReporterIons = new TabPage();
            _fraDecoyOptions = new GroupBox();
            _lblParentIonDecoyMassDaUnits = new Label();
            _txtParentIonDecoyMassDa = new TextBox();
            _lblParentIonDecoyMassDa = new Label();
            _fraMRMOptions = new GroupBox();
            _chkMRMWriteIntensityCrosstab = new CheckBox();
            _lblMRMInfo = new Label();
            _chkMRMWriteDataList = new CheckBox();
            _fraReporterIonMassMode = new GroupBox();
            _cboReporterIonMassMode = new ComboBox();
            _cboReporterIonMassMode.SelectedIndexChanged += new EventHandler(cboReporterIonMassMode_SelectedIndexChanged);
            _fraReporterIonOptions = new GroupBox();
            _chkReporterIonApplyAbundanceCorrection = new CheckBox();
            _chkReporterIonSaveUncorrectedIntensities = new CheckBox();
            _chkReporterIonSaveObservedMasses = new CheckBox();
            _txtReporterIonMZToleranceDa = new TextBox();
            _lblReporterIonMZToleranceDa = new Label();
            _chkReporterIonStatsEnabled = new CheckBox();
            _chkReporterIonStatsEnabled.CheckedChanged += new EventHandler(chkReporterIonStatsEnabled_CheckedChanged);
            _TabPageAdvancedOptions = new TabPage();
            _fraAdditionalInfoFiles = new GroupBox();
            _chkConsolidateConstantExtendedHeaderValues = new CheckBox();
            _lblStatusLogKeyNameFilterList = new Label();
            _txtStatusLogKeyNameFilterList = new TextBox();
            _chkSaveExtendedStatsFileIncludeStatusLog = new CheckBox();
            _chkSaveExtendedStatsFileIncludeStatusLog.CheckedChanged += new EventHandler(chkSaveExtendedStatsFileIncludeStatusLog_CheckedChanged);
            _chkSaveExtendedStatsFileIncludeFilterText = new CheckBox();
            _chkSaveMSTuneFile = new CheckBox();
            _chkSaveMSMethodFile = new CheckBox();
            _chkSaveExtendedStatsFile = new CheckBox();
            _chkSaveExtendedStatsFile.CheckedChanged += new EventHandler(chkSaveExtendedStatsFile_CheckedChanged);
            _fraDatasetLookupInfo = new GroupBox();
            _cmdSetConnectionStringToPNNLServer = new Button();
            _cmdSetConnectionStringToPNNLServer.Click += new EventHandler(cmdSetConnectionStringToPNNLServer_Click);
            _txtDatasetInfoQuerySQL = new TextBox();
            _lblDatasetInfoQuerySQL = new Label();
            _txtDatabaseConnectionString = new TextBox();
            _lblDatabaseConnectionString = new Label();
            _lblDatasetLookupFilePath = new Label();
            _cmdSelectDatasetLookupFile = new Button();
            _cmdSelectDatasetLookupFile.Click += new EventHandler(cmdSelectDatasetLookupFile_Click);
            _txtDatasetLookupFilePath = new TextBox();
            _fraMemoryConservationOptions = new GroupBox();
            _chkSkipMSMSProcessing = new CheckBox();
            _chkSkipMSMSProcessing.CheckedChanged += new EventHandler(chkSkipMSMSProcessing_CheckedChanged);
            _chkSkipSICAndRawDataProcessing = new CheckBox();
            _chkSkipSICAndRawDataProcessing.CheckedChanged += new EventHandler(chkSkipSICAndRawDataProcessing_CheckedChanged);
            _chkExportRawDataOnly = new CheckBox();
            _chkExportRawDataOnly.CheckedChanged += new EventHandler(chkExportRawDataOnly_CheckedChanged);
            _TabPageLog = new TabPage();
            _txtLogMessages = new TextBox();
            _fraOutputDirectoryPath = new GroupBox();
            _cmdStartProcessing = new Button();
            _cmdStartProcessing.Click += new EventHandler(cmdStartProcessing_Click);
            _cmdSelectOutputDirectory = new Button();
            _cmdSelectOutputDirectory.Click += new EventHandler(cmdSelectOutputDirectory_Click);
            _txtOutputDirectoryPath = new TextBox();
            _fraInputFilePath.SuspendLayout();
            _tbsOptions.SuspendLayout();
            _TabPageMasicExportOptions.SuspendLayout();
            _fraExportAllSpectraDataPoints.SuspendLayout();
            _TabPageSICOptions.SuspendLayout();
            _fraInputFileRangeFilters.SuspendLayout();
            _fraSICSearchThresholds.SuspendLayout();
            _TabPagePeakFindingOptions.SuspendLayout();
            _fraSICNoiseThresholds.SuspendLayout();
            _fraSmoothingOptions.SuspendLayout();
            _fraPeakFindingOptions.SuspendLayout();
            _TabPageBinningAndSimilarityOptions.SuspendLayout();
            _fraMassSpectraNoiseThresholds.SuspendLayout();
            _fraBinningIntensityOptions.SuspendLayout();
            _fraSpectrumSimilarityOptions.SuspendLayout();
            _fraBinningMZOptions.SuspendLayout();
            _TabPageCustomSICOptions.SuspendLayout();
            _fraCustomSICControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_dgCustomSICValues).BeginInit();
            _TabPageReporterIons.SuspendLayout();
            _fraDecoyOptions.SuspendLayout();
            _fraMRMOptions.SuspendLayout();
            _fraReporterIonMassMode.SuspendLayout();
            _fraReporterIonOptions.SuspendLayout();
            _TabPageAdvancedOptions.SuspendLayout();
            _fraAdditionalInfoFiles.SuspendLayout();
            _fraDatasetLookupInfo.SuspendLayout();
            _fraMemoryConservationOptions.SuspendLayout();
            _TabPageLog.SuspendLayout();
            _fraOutputDirectoryPath.SuspendLayout();
            SuspendLayout();
            //
            // txtInputFilePath
            //
            _txtInputFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _txtInputFilePath.Location = new System.Drawing.Point(125, 30);
            _txtInputFilePath.Name = "txtInputFilePath";
            _txtInputFilePath.Size = new System.Drawing.Size(741, 22);
            _txtInputFilePath.TabIndex = 1;
            //
            // cmdSelectFile
            //
            _cmdSelectFile.Location = new System.Drawing.Point(10, 28);
            _cmdSelectFile.Name = "cmdSelectFile";
            _cmdSelectFile.Size = new System.Drawing.Size(96, 27);
            _cmdSelectFile.TabIndex = 0;
            _cmdSelectFile.Text = "&Select File";
            //
            // fraInputFilePath
            //
            _fraInputFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _fraInputFilePath.Controls.Add(_cmdSelectFile);
            _fraInputFilePath.Controls.Add(_txtInputFilePath);
            _fraInputFilePath.Location = new System.Drawing.Point(10, 9);
            _fraInputFilePath.Name = "fraInputFilePath";
            _fraInputFilePath.Size = new System.Drawing.Size(885, 74);
            _fraInputFilePath.TabIndex = 0;
            _fraInputFilePath.TabStop = false;
            _fraInputFilePath.Text = "Input File Path (Thermo .Raw or Agilent .CDF/.MGF combo or mzXML or mz" + "Data)";
            //
            // MainMenuControl
            //
            _MainMenuControl.MenuItems.AddRange(new MenuItem[] { _mnuFile, _mnuEdit, _mnuHelp });
            //
            // mnuFile
            //
            _mnuFile.Index = 0;
            _mnuFile.MenuItems.AddRange(new MenuItem[] { _mnuFileSelectInputFile, _mnuFileSelectOutputDirectory, _mnuFileSep1, _mnuFileLoadOptions, _mnuFileSaveOptions, _mnuFileSep2, _mnuFileExit });
            _mnuFile.Text = "&File";
            //
            // mnuFileSelectInputFile
            //
            _mnuFileSelectInputFile.Index = 0;
            _mnuFileSelectInputFile.Text = "&Select Input File ...";
            //
            // mnuFileSelectOutputDirectory
            //
            _mnuFileSelectOutputDirectory.Index = 1;
            _mnuFileSelectOutputDirectory.Text = "Select Output &Directory ...";
            //
            // mnuFileSep1
            //
            _mnuFileSep1.Index = 2;
            _mnuFileSep1.Text = "-";
            //
            // mnuFileLoadOptions
            //
            _mnuFileLoadOptions.Index = 3;
            _mnuFileLoadOptions.Text = "&Load Options ...";
            //
            // mnuFileSaveOptions
            //
            _mnuFileSaveOptions.Index = 4;
            _mnuFileSaveOptions.Text = "Sa&ve Options ...";
            //
            // mnuFileSep2
            //
            _mnuFileSep2.Index = 5;
            _mnuFileSep2.Text = "-";
            //
            // mnuFileExit
            //
            _mnuFileExit.Index = 6;
            _mnuFileExit.Text = "E&xit";
            //
            // mnuEdit
            //
            _mnuEdit.Index = 1;
            _mnuEdit.MenuItems.AddRange(new MenuItem[] { _mnuEditProcessFile, _mnuEditSep1, _mnuEditSaveDefaultOptions, _mnuEditResetOptions });
            _mnuEdit.Text = "&Edit";
            //
            // mnuEditProcessFile
            //
            _mnuEditProcessFile.Index = 0;
            _mnuEditProcessFile.Text = "&Process File";
            //
            // mnuEditSep1
            //
            _mnuEditSep1.Index = 1;
            _mnuEditSep1.Text = "-";
            //
            // mnuEditSaveDefaultOptions
            //
            _mnuEditSaveDefaultOptions.Index = 2;
            _mnuEditSaveDefaultOptions.Text = "&Save current options as Default ...";
            //
            // mnuEditResetOptions
            //
            _mnuEditResetOptions.Index = 3;
            _mnuEditResetOptions.Text = "&Reset options to Defaults";
            //
            // mnuHelp
            //
            _mnuHelp.Index = 2;
            _mnuHelp.MenuItems.AddRange(new MenuItem[] { _mnuHelpAbout });
            _mnuHelp.Text = "&Help";
            //
            // mnuHelpAbout
            //
            _mnuHelpAbout.Index = 0;
            _mnuHelpAbout.Text = "&About";
            //
            // tbsOptions
            //
            _tbsOptions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _tbsOptions.Controls.Add(_TabPageMasicExportOptions);
            _tbsOptions.Controls.Add(_TabPageSICOptions);
            _tbsOptions.Controls.Add(_TabPagePeakFindingOptions);
            _tbsOptions.Controls.Add(_TabPageBinningAndSimilarityOptions);
            _tbsOptions.Controls.Add(_TabPageCustomSICOptions);
            _tbsOptions.Controls.Add(_TabPageReporterIons);
            _tbsOptions.Controls.Add(_TabPageAdvancedOptions);
            _tbsOptions.Controls.Add(_TabPageLog);
            _tbsOptions.Location = new System.Drawing.Point(10, 203);
            _tbsOptions.Name = "tbsOptions";
            _tbsOptions.SelectedIndex = 0;
            _tbsOptions.Size = new System.Drawing.Size(890, 356);
            _tbsOptions.TabIndex = 2;
            //
            // TabPageMasicExportOptions
            //
            _TabPageMasicExportOptions.Controls.Add(_chkWriteDetailedSICDataFile);
            _TabPageMasicExportOptions.Controls.Add(_chkIncludeScanTimesInSICStatsFile);
            _TabPageMasicExportOptions.Controls.Add(_txtDatasetID);
            _TabPageMasicExportOptions.Controls.Add(_lblDatasetID);
            _TabPageMasicExportOptions.Controls.Add(_lblRawDataExportOverview);
            _TabPageMasicExportOptions.Controls.Add(_fraExportAllSpectraDataPoints);
            _TabPageMasicExportOptions.Controls.Add(_chkIncludeHeaders);
            _TabPageMasicExportOptions.Location = new System.Drawing.Point(4, 25);
            _TabPageMasicExportOptions.Name = "TabPageMasicExportOptions";
            _TabPageMasicExportOptions.Size = new System.Drawing.Size(882, 327);
            _TabPageMasicExportOptions.TabIndex = 4;
            _TabPageMasicExportOptions.Text = "Export Options";
            _TabPageMasicExportOptions.UseVisualStyleBackColor = true;
            //
            // chkWriteDetailedSICDataFile
            //
            _chkWriteDetailedSICDataFile.Location = new System.Drawing.Point(19, 62);
            _chkWriteDetailedSICDataFile.Name = "chkWriteDetailedSICDataFile";
            _chkWriteDetailedSICDataFile.Size = new System.Drawing.Size(250, 19);
            _chkWriteDetailedSICDataFile.TabIndex = 7;
            _chkWriteDetailedSICDataFile.Text = "Write detailed SIC data file";
            //
            // chkIncludeScanTimesInSICStatsFile
            //
            _chkIncludeScanTimesInSICStatsFile.Checked = true;
            _chkIncludeScanTimesInSICStatsFile.CheckState = CheckState.Checked;
            _chkIncludeScanTimesInSICStatsFile.Location = new System.Drawing.Point(19, 37);
            _chkIncludeScanTimesInSICStatsFile.Name = "chkIncludeScanTimesInSICStatsFile";
            _chkIncludeScanTimesInSICStatsFile.Size = new System.Drawing.Size(250, 18);
            _chkIncludeScanTimesInSICStatsFile.TabIndex = 6;
            _chkIncludeScanTimesInSICStatsFile.Text = "Include scan times in SIC stats file";
            //
            // txtDatasetID
            //
            _txtDatasetID.Location = new System.Drawing.Point(605, 18);
            _txtDatasetID.Name = "txtDatasetID";
            _txtDatasetID.Size = new System.Drawing.Size(105, 22);
            _txtDatasetID.TabIndex = 4;
            _txtDatasetID.Text = "0";
            //
            // lblDatasetID
            //
            _lblDatasetID.Location = new System.Drawing.Point(432, 18);
            _lblDatasetID.Name = "lblDatasetID";
            _lblDatasetID.Size = new System.Drawing.Size(182, 19);
            _lblDatasetID.TabIndex = 3;
            _lblDatasetID.Text = "Input File Dataset Number";
            //
            // lblRawDataExportOverview
            //
            _lblRawDataExportOverview.Location = new System.Drawing.Point(442, 55);
            _lblRawDataExportOverview.Name = "lblRawDataExportOverview";
            _lblRawDataExportOverview.Size = new System.Drawing.Size(307, 268);
            _lblRawDataExportOverview.TabIndex = 5;
            _lblRawDataExportOverview.Text = "Raw Data Export Options Overview";
            //
            // fraExportAllSpectraDataPoints
            //
            _fraExportAllSpectraDataPoints.Controls.Add(_txtExportRawDataSignalToNoiseRatioMinimum);
            _fraExportAllSpectraDataPoints.Controls.Add(_lblExportRawDataSignalToNoiseRatioMinimum);
            _fraExportAllSpectraDataPoints.Controls.Add(_chkExportRawDataRenumberScans);
            _fraExportAllSpectraDataPoints.Controls.Add(_txtExportRawDataMaxIonCountPerScan);
            _fraExportAllSpectraDataPoints.Controls.Add(_lblExportRawDataMaxIonCountPerScan);
            _fraExportAllSpectraDataPoints.Controls.Add(_txtExportRawDataIntensityMinimum);
            _fraExportAllSpectraDataPoints.Controls.Add(_lblExportRawDataIntensityMinimum);
            _fraExportAllSpectraDataPoints.Controls.Add(_chkExportRawDataIncludeMSMS);
            _fraExportAllSpectraDataPoints.Controls.Add(_cboExportRawDataFileFormat);
            _fraExportAllSpectraDataPoints.Controls.Add(_lblExportDataPointsFormat);
            _fraExportAllSpectraDataPoints.Controls.Add(_chkExportRawSpectraData);
            _fraExportAllSpectraDataPoints.Location = new System.Drawing.Point(19, 93);
            _fraExportAllSpectraDataPoints.Name = "fraExportAllSpectraDataPoints";
            _fraExportAllSpectraDataPoints.Size = new System.Drawing.Size(413, 222);
            _fraExportAllSpectraDataPoints.TabIndex = 2;
            _fraExportAllSpectraDataPoints.TabStop = false;
            _fraExportAllSpectraDataPoints.Text = "Raw Data Point Export Options";
            //
            // txtExportRawDataSignalToNoiseRatioMinimum
            //
            _txtExportRawDataSignalToNoiseRatioMinimum.Location = new System.Drawing.Point(240, 129);
            _txtExportRawDataSignalToNoiseRatioMinimum.Name = "txtExportRawDataSignalToNoiseRatioMinimum";
            _txtExportRawDataSignalToNoiseRatioMinimum.Size = new System.Drawing.Size(48, 22);
            _txtExportRawDataSignalToNoiseRatioMinimum.TabIndex = 6;
            _txtExportRawDataSignalToNoiseRatioMinimum.Text = "1";
            //
            // lblExportRawDataSignalToNoiseRatioMinimum
            //
            _lblExportRawDataSignalToNoiseRatioMinimum.Location = new System.Drawing.Point(19, 132);
            _lblExportRawDataSignalToNoiseRatioMinimum.Name = "lblExportRawDataSignalToNoiseRatioMinimum";
            _lblExportRawDataSignalToNoiseRatioMinimum.Size = new System.Drawing.Size(211, 18);
            _lblExportRawDataSignalToNoiseRatioMinimum.TabIndex = 5;
            _lblExportRawDataSignalToNoiseRatioMinimum.Text = "Minimum Signal to Noise Ratio";
            //
            // chkExportRawDataRenumberScans
            //
            _chkExportRawDataRenumberScans.Location = new System.Drawing.Point(19, 76);
            _chkExportRawDataRenumberScans.Name = "chkExportRawDataRenumberScans";
            _chkExportRawDataRenumberScans.Size = new System.Drawing.Size(375, 19);
            _chkExportRawDataRenumberScans.TabIndex = 3;
            _chkExportRawDataRenumberScans.Text = "Renumber survey scan spectra to make sequential";
            //
            // txtExportRawDataMaxIonCountPerScan
            //
            _txtExportRawDataMaxIonCountPerScan.Location = new System.Drawing.Point(240, 157);
            _txtExportRawDataMaxIonCountPerScan.Name = "txtExportRawDataMaxIonCountPerScan";
            _txtExportRawDataMaxIonCountPerScan.Size = new System.Drawing.Size(67, 22);
            _txtExportRawDataMaxIonCountPerScan.TabIndex = 8;
            _txtExportRawDataMaxIonCountPerScan.Text = "200";
            //
            // lblExportRawDataMaxIonCountPerScan
            //
            _lblExportRawDataMaxIonCountPerScan.Location = new System.Drawing.Point(19, 159);
            _lblExportRawDataMaxIonCountPerScan.Name = "lblExportRawDataMaxIonCountPerScan";
            _lblExportRawDataMaxIonCountPerScan.Size = new System.Drawing.Size(221, 19);
            _lblExportRawDataMaxIonCountPerScan.TabIndex = 7;
            _lblExportRawDataMaxIonCountPerScan.Text = "Maximum Ion Count per Scan";
            //
            // txtExportRawDataIntensityMinimum
            //
            _txtExportRawDataIntensityMinimum.Location = new System.Drawing.Point(240, 185);
            _txtExportRawDataIntensityMinimum.Name = "txtExportRawDataIntensityMinimum";
            _txtExportRawDataIntensityMinimum.Size = new System.Drawing.Size(106, 22);
            _txtExportRawDataIntensityMinimum.TabIndex = 10;
            _txtExportRawDataIntensityMinimum.Text = "0";
            //
            // lblExportRawDataIntensityMinimum
            //
            _lblExportRawDataIntensityMinimum.Location = new System.Drawing.Point(19, 187);
            _lblExportRawDataIntensityMinimum.Name = "lblExportRawDataIntensityMinimum";
            _lblExportRawDataIntensityMinimum.Size = new System.Drawing.Size(183, 18);
            _lblExportRawDataIntensityMinimum.TabIndex = 9;
            _lblExportRawDataIntensityMinimum.Text = "Minimum Intensity (counts)";
            //
            // chkExportRawDataIncludeMSMS
            //
            _chkExportRawDataIncludeMSMS.Location = new System.Drawing.Point(19, 99);
            _chkExportRawDataIncludeMSMS.Name = "chkExportRawDataIncludeMSMS";
            _chkExportRawDataIncludeMSMS.Size = new System.Drawing.Size(384, 19);
            _chkExportRawDataIncludeMSMS.TabIndex = 4;
            _chkExportRawDataIncludeMSMS.Text = "Export MS/MS Spectra, in addition to survey scan spectra";
            //
            // cboExportRawDataFileFormat
            //
            _cboExportRawDataFileFormat.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboExportRawDataFileFormat.Location = new System.Drawing.Point(106, 46);
            _cboExportRawDataFileFormat.Name = "cboExportRawDataFileFormat";
            _cboExportRawDataFileFormat.Size = new System.Drawing.Size(172, 24);
            _cboExportRawDataFileFormat.TabIndex = 2;
            //
            // lblExportDataPointsFormat
            //
            _lblExportDataPointsFormat.Location = new System.Drawing.Point(38, 51);
            _lblExportDataPointsFormat.Name = "lblExportDataPointsFormat";
            _lblExportDataPointsFormat.Size = new System.Drawing.Size(87, 18);
            _lblExportDataPointsFormat.TabIndex = 1;
            _lblExportDataPointsFormat.Text = "Format:";
            //
            // chkExportRawSpectraData
            //
            _chkExportRawSpectraData.Location = new System.Drawing.Point(19, 28);
            _chkExportRawSpectraData.Name = "chkExportRawSpectraData";
            _chkExportRawSpectraData.Size = new System.Drawing.Size(288, 18);
            _chkExportRawSpectraData.TabIndex = 0;
            _chkExportRawSpectraData.Text = "Export All Spectra Data Points";
            //
            // chkIncludeHeaders
            //
            _chkIncludeHeaders.Checked = true;
            _chkIncludeHeaders.CheckState = CheckState.Checked;
            _chkIncludeHeaders.Location = new System.Drawing.Point(19, 12);
            _chkIncludeHeaders.Name = "chkIncludeHeaders";
            _chkIncludeHeaders.Size = new System.Drawing.Size(192, 18);
            _chkIncludeHeaders.TabIndex = 0;
            _chkIncludeHeaders.Text = "Include Column Headers";
            //
            // TabPageSICOptions
            //
            _TabPageSICOptions.Controls.Add(_fraInputFileRangeFilters);
            _TabPageSICOptions.Controls.Add(_lblSICOptionsOverview);
            _TabPageSICOptions.Controls.Add(_fraSICSearchThresholds);
            _TabPageSICOptions.Location = new System.Drawing.Point(4, 25);
            _TabPageSICOptions.Name = "TabPageSICOptions";
            _TabPageSICOptions.Size = new System.Drawing.Size(882, 327);
            _TabPageSICOptions.TabIndex = 5;
            _TabPageSICOptions.Text = "SIC Options";
            _TabPageSICOptions.UseVisualStyleBackColor = true;
            //
            // fraInputFileRangeFilters
            //
            _fraInputFileRangeFilters.Controls.Add(_lblTimeEndUnits);
            _fraInputFileRangeFilters.Controls.Add(_lblTimeStartUnits);
            _fraInputFileRangeFilters.Controls.Add(_txtTimeEnd);
            _fraInputFileRangeFilters.Controls.Add(_txtTimeStart);
            _fraInputFileRangeFilters.Controls.Add(_lblTimeEnd);
            _fraInputFileRangeFilters.Controls.Add(_lblTimeStart);
            _fraInputFileRangeFilters.Controls.Add(_txtScanEnd);
            _fraInputFileRangeFilters.Controls.Add(_txtScanStart);
            _fraInputFileRangeFilters.Controls.Add(_lblScanEnd);
            _fraInputFileRangeFilters.Controls.Add(_lblScanStart);
            _fraInputFileRangeFilters.Controls.Add(_cmdClearAllRangeFilters);
            _fraInputFileRangeFilters.Location = new System.Drawing.Point(19, 232);
            _fraInputFileRangeFilters.Name = "fraInputFileRangeFilters";
            _fraInputFileRangeFilters.Size = new System.Drawing.Size(586, 82);
            _fraInputFileRangeFilters.TabIndex = 1;
            _fraInputFileRangeFilters.TabStop = false;
            _fraInputFileRangeFilters.Text = "Input File Range Filters";
            //
            // lblTimeEndUnits
            //
            _lblTimeEndUnits.Location = new System.Drawing.Point(499, 52);
            _lblTimeEndUnits.Name = "lblTimeEndUnits";
            _lblTimeEndUnits.Size = new System.Drawing.Size(77, 18);
            _lblTimeEndUnits.TabIndex = 10;
            _lblTimeEndUnits.Text = "minutes";
            //
            // lblTimeStartUnits
            //
            _lblTimeStartUnits.Location = new System.Drawing.Point(499, 24);
            _lblTimeStartUnits.Name = "lblTimeStartUnits";
            _lblTimeStartUnits.Size = new System.Drawing.Size(77, 18);
            _lblTimeStartUnits.TabIndex = 7;
            _lblTimeStartUnits.Text = "minutes";
            //
            // txtTimeEnd
            //
            _txtTimeEnd.Location = new System.Drawing.Point(422, 49);
            _txtTimeEnd.Name = "txtTimeEnd";
            _txtTimeEnd.Size = new System.Drawing.Size(68, 22);
            _txtTimeEnd.TabIndex = 9;
            _txtTimeEnd.Text = "0";
            //
            // txtTimeStart
            //
            _txtTimeStart.Location = new System.Drawing.Point(422, 22);
            _txtTimeStart.Name = "txtTimeStart";
            _txtTimeStart.Size = new System.Drawing.Size(68, 22);
            _txtTimeStart.TabIndex = 6;
            _txtTimeStart.Text = "0";
            //
            // lblTimeEnd
            //
            _lblTimeEnd.Location = new System.Drawing.Point(336, 52);
            _lblTimeEnd.Name = "lblTimeEnd";
            _lblTimeEnd.Size = new System.Drawing.Size(86, 18);
            _lblTimeEnd.TabIndex = 8;
            _lblTimeEnd.Text = "End Time";
            //
            // lblTimeStart
            //
            _lblTimeStart.Location = new System.Drawing.Point(336, 24);
            _lblTimeStart.Name = "lblTimeStart";
            _lblTimeStart.Size = new System.Drawing.Size(77, 18);
            _lblTimeStart.TabIndex = 5;
            _lblTimeStart.Text = "Start Time";
            //
            // txtScanEnd
            //
            _txtScanEnd.Location = new System.Drawing.Point(230, 49);
            _txtScanEnd.Name = "txtScanEnd";
            _txtScanEnd.Size = new System.Drawing.Size(68, 22);
            _txtScanEnd.TabIndex = 4;
            _txtScanEnd.Text = "0";
            //
            // txtScanStart
            //
            _txtScanStart.Location = new System.Drawing.Point(230, 22);
            _txtScanStart.Name = "txtScanStart";
            _txtScanStart.Size = new System.Drawing.Size(68, 22);
            _txtScanStart.TabIndex = 2;
            _txtScanStart.Text = "0";
            //
            // lblScanEnd
            //
            _lblScanEnd.Location = new System.Drawing.Point(144, 52);
            _lblScanEnd.Name = "lblScanEnd";
            _lblScanEnd.Size = new System.Drawing.Size(86, 18);
            _lblScanEnd.TabIndex = 3;
            _lblScanEnd.Text = "End Scan";
            //
            // lblScanStart
            //
            _lblScanStart.Location = new System.Drawing.Point(144, 24);
            _lblScanStart.Name = "lblScanStart";
            _lblScanStart.Size = new System.Drawing.Size(77, 18);
            _lblScanStart.TabIndex = 1;
            _lblScanStart.Text = "Start Scan";
            //
            // cmdClearAllRangeFilters
            //
            _cmdClearAllRangeFilters.Location = new System.Drawing.Point(19, 31);
            _cmdClearAllRangeFilters.Name = "cmdClearAllRangeFilters";
            _cmdClearAllRangeFilters.Size = new System.Drawing.Size(106, 28);
            _cmdClearAllRangeFilters.TabIndex = 0;
            _cmdClearAllRangeFilters.Text = "Clear Filters";
            //
            // lblSICOptionsOverview
            //
            _lblSICOptionsOverview.Location = new System.Drawing.Point(374, 28);
            _lblSICOptionsOverview.Name = "lblSICOptionsOverview";
            _lblSICOptionsOverview.Size = new System.Drawing.Size(426, 175);
            _lblSICOptionsOverview.TabIndex = 2;
            _lblSICOptionsOverview.Text = "SIC Options Overview";
            //
            // fraSICSearchThresholds
            //
            _fraSICSearchThresholds.Controls.Add(_optSICTolerancePPM);
            _fraSICSearchThresholds.Controls.Add(_optSICToleranceDa);
            _fraSICSearchThresholds.Controls.Add(_chkRefineReportedParentIonMZ);
            _fraSICSearchThresholds.Controls.Add(_chkReplaceSICZeroesWithMinimumPositiveValueFromMSData);
            _fraSICSearchThresholds.Controls.Add(_txtMaxPeakWidthMinutesForward);
            _fraSICSearchThresholds.Controls.Add(_txtMaxPeakWidthMinutesBackward);
            _fraSICSearchThresholds.Controls.Add(_txtIntensityThresholdFractionMax);
            _fraSICSearchThresholds.Controls.Add(_lblIntensityThresholdFractionMax);
            _fraSICSearchThresholds.Controls.Add(_txtIntensityThresholdAbsoluteMinimum);
            _fraSICSearchThresholds.Controls.Add(_lblIntensityThresholdAbsoluteMinimum);
            _fraSICSearchThresholds.Controls.Add(_lblMaxPeakWidthMinutesForward);
            _fraSICSearchThresholds.Controls.Add(_lblMaxPeakWidthMinutesBackward);
            _fraSICSearchThresholds.Controls.Add(_lblMaxPeakWidthMinutes);
            _fraSICSearchThresholds.Controls.Add(_txtSICTolerance);
            _fraSICSearchThresholds.Controls.Add(_lblSICToleranceDa);
            _fraSICSearchThresholds.Location = new System.Drawing.Point(19, 9);
            _fraSICSearchThresholds.Name = "fraSICSearchThresholds";
            _fraSICSearchThresholds.Size = new System.Drawing.Size(336, 217);
            _fraSICSearchThresholds.TabIndex = 0;
            _fraSICSearchThresholds.TabStop = false;
            _fraSICSearchThresholds.Text = "SIC Search Thresholds";
            //
            // optSICTolerancePPM
            //
            _optSICTolerancePPM.Location = new System.Drawing.Point(230, 32);
            _optSICTolerancePPM.Name = "optSICTolerancePPM";
            _optSICTolerancePPM.Size = new System.Drawing.Size(87, 21);
            _optSICTolerancePPM.TabIndex = 14;
            _optSICTolerancePPM.Text = "ppm";
            //
            // optSICToleranceDa
            //
            _optSICToleranceDa.Checked = true;
            _optSICToleranceDa.Location = new System.Drawing.Point(230, 12);
            _optSICToleranceDa.Name = "optSICToleranceDa";
            _optSICToleranceDa.Size = new System.Drawing.Size(87, 20);
            _optSICToleranceDa.TabIndex = 13;
            _optSICToleranceDa.TabStop = true;
            _optSICToleranceDa.Text = "Da";
            //
            // chkRefineReportedParentIonMZ
            //
            _chkRefineReportedParentIonMZ.Location = new System.Drawing.Point(10, 183);
            _chkRefineReportedParentIonMZ.Name = "chkRefineReportedParentIonMZ";
            _chkRefineReportedParentIonMZ.Size = new System.Drawing.Size(316, 20);
            _chkRefineReportedParentIonMZ.TabIndex = 12;
            _chkRefineReportedParentIonMZ.Text = "Refine reported parent ion m/z values";
            //
            // chkReplaceSICZeroesWithMinimumPositiveValueFromMSData
            //
            _chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Checked = true;
            _chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.CheckState = CheckState.Checked;
            _chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Location = new System.Drawing.Point(10, 157);
            _chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Name = "chkReplaceSICZeroesWithMinimumPositiveValueFromMSData";
            _chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Size = new System.Drawing.Size(316, 20);
            _chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.TabIndex = 11;
            _chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Text = "Replace SIC zeroes with min MS data values";
            //
            // txtMaxPeakWidthMinutesForward
            //
            _txtMaxPeakWidthMinutesForward.Location = new System.Drawing.Point(250, 65);
            _txtMaxPeakWidthMinutesForward.Name = "txtMaxPeakWidthMinutesForward";
            _txtMaxPeakWidthMinutesForward.Size = new System.Drawing.Size(67, 22);
            _txtMaxPeakWidthMinutesForward.TabIndex = 6;
            _txtMaxPeakWidthMinutesForward.Text = "3";
            //
            // txtMaxPeakWidthMinutesBackward
            //
            _txtMaxPeakWidthMinutesBackward.Location = new System.Drawing.Point(96, 65);
            _txtMaxPeakWidthMinutesBackward.Name = "txtMaxPeakWidthMinutesBackward";
            _txtMaxPeakWidthMinutesBackward.Size = new System.Drawing.Size(67, 22);
            _txtMaxPeakWidthMinutesBackward.TabIndex = 4;
            _txtMaxPeakWidthMinutesBackward.Text = "3";
            //
            // txtIntensityThresholdFractionMax
            //
            _txtIntensityThresholdFractionMax.Location = new System.Drawing.Point(250, 92);
            _txtIntensityThresholdFractionMax.Name = "txtIntensityThresholdFractionMax";
            _txtIntensityThresholdFractionMax.Size = new System.Drawing.Size(67, 22);
            _txtIntensityThresholdFractionMax.TabIndex = 8;
            _txtIntensityThresholdFractionMax.Text = "0.01";
            //
            // lblIntensityThresholdFractionMax
            //
            _lblIntensityThresholdFractionMax.Location = new System.Drawing.Point(10, 92);
            _lblIntensityThresholdFractionMax.Name = "lblIntensityThresholdFractionMax";
            _lblIntensityThresholdFractionMax.Size = new System.Drawing.Size(240, 19);
            _lblIntensityThresholdFractionMax.TabIndex = 7;
            _lblIntensityThresholdFractionMax.Text = "Intensity Threshold Fraction Max Peak";
            //
            // txtIntensityThresholdAbsoluteMinimum
            //
            _txtIntensityThresholdAbsoluteMinimum.Location = new System.Drawing.Point(250, 120);
            _txtIntensityThresholdAbsoluteMinimum.Name = "txtIntensityThresholdAbsoluteMinimum";
            _txtIntensityThresholdAbsoluteMinimum.Size = new System.Drawing.Size(67, 22);
            _txtIntensityThresholdAbsoluteMinimum.TabIndex = 10;
            _txtIntensityThresholdAbsoluteMinimum.Text = "0";
            //
            // lblIntensityThresholdAbsoluteMinimum
            //
            _lblIntensityThresholdAbsoluteMinimum.Location = new System.Drawing.Point(10, 120);
            _lblIntensityThresholdAbsoluteMinimum.Name = "lblIntensityThresholdAbsoluteMinimum";
            _lblIntensityThresholdAbsoluteMinimum.Size = new System.Drawing.Size(240, 18);
            _lblIntensityThresholdAbsoluteMinimum.TabIndex = 9;
            _lblIntensityThresholdAbsoluteMinimum.Text = "Intensity Threshold Absolute Minimum";
            //
            // lblMaxPeakWidthMinutesForward
            //
            _lblMaxPeakWidthMinutesForward.Location = new System.Drawing.Point(182, 65);
            _lblMaxPeakWidthMinutesForward.Name = "lblMaxPeakWidthMinutesForward";
            _lblMaxPeakWidthMinutesForward.Size = new System.Drawing.Size(77, 18);
            _lblMaxPeakWidthMinutesForward.TabIndex = 5;
            _lblMaxPeakWidthMinutesForward.Text = "Forward";
            //
            // lblMaxPeakWidthMinutesBackward
            //
            _lblMaxPeakWidthMinutesBackward.Location = new System.Drawing.Point(19, 65);
            _lblMaxPeakWidthMinutesBackward.Name = "lblMaxPeakWidthMinutesBackward";
            _lblMaxPeakWidthMinutesBackward.Size = new System.Drawing.Size(77, 18);
            _lblMaxPeakWidthMinutesBackward.TabIndex = 3;
            _lblMaxPeakWidthMinutesBackward.Text = "Backward";
            //
            // lblMaxPeakWidthMinutes
            //
            _lblMaxPeakWidthMinutes.Location = new System.Drawing.Point(10, 46);
            _lblMaxPeakWidthMinutes.Name = "lblMaxPeakWidthMinutes";
            _lblMaxPeakWidthMinutes.Size = new System.Drawing.Size(201, 19);
            _lblMaxPeakWidthMinutes.TabIndex = 2;
            _lblMaxPeakWidthMinutes.Text = "Maximum Peak Width (minutes)";
            //
            // txtSICTolerance
            //
            _txtSICTolerance.Location = new System.Drawing.Point(154, 18);
            _txtSICTolerance.Name = "txtSICTolerance";
            _txtSICTolerance.Size = new System.Drawing.Size(57, 22);
            _txtSICTolerance.TabIndex = 1;
            _txtSICTolerance.Text = "0.60";
            //
            // lblSICToleranceDa
            //
            _lblSICToleranceDa.Location = new System.Drawing.Point(10, 18);
            _lblSICToleranceDa.Name = "lblSICToleranceDa";
            _lblSICToleranceDa.Size = new System.Drawing.Size(144, 19);
            _lblSICToleranceDa.TabIndex = 0;
            _lblSICToleranceDa.Text = "SIC Tolerance (Da)";
            //
            // TabPagePeakFindingOptions
            //
            _TabPagePeakFindingOptions.Controls.Add(_fraSICNoiseThresholds);
            _TabPagePeakFindingOptions.Controls.Add(_fraSmoothingOptions);
            _TabPagePeakFindingOptions.Controls.Add(_fraPeakFindingOptions);
            _TabPagePeakFindingOptions.Location = new System.Drawing.Point(4, 25);
            _TabPagePeakFindingOptions.Name = "TabPagePeakFindingOptions";
            _TabPagePeakFindingOptions.Size = new System.Drawing.Size(882, 327);
            _TabPagePeakFindingOptions.TabIndex = 7;
            _TabPagePeakFindingOptions.Text = "Peak Finding Options";
            _TabPagePeakFindingOptions.UseVisualStyleBackColor = true;
            //
            // fraSICNoiseThresholds
            //
            _fraSICNoiseThresholds.Controls.Add(_cboSICNoiseThresholdMode);
            _fraSICNoiseThresholds.Controls.Add(_lblNoiseThresholdMode);
            _fraSICNoiseThresholds.Controls.Add(_txtSICNoiseFractionLowIntensityDataToAverage);
            _fraSICNoiseThresholds.Controls.Add(_lblSICNoiseFractionLowIntensityDataToAverage);
            _fraSICNoiseThresholds.Controls.Add(_txtSICNoiseThresholdIntensity);
            _fraSICNoiseThresholds.Controls.Add(_lblSICNoiseThresholdIntensity);
            _fraSICNoiseThresholds.Location = new System.Drawing.Point(19, 9);
            _fraSICNoiseThresholds.Name = "fraSICNoiseThresholds";
            _fraSICNoiseThresholds.Size = new System.Drawing.Size(384, 148);
            _fraSICNoiseThresholds.TabIndex = 0;
            _fraSICNoiseThresholds.TabStop = false;
            _fraSICNoiseThresholds.Text = "Initial Noise Threshold Determination for SICs";
            //
            // cboSICNoiseThresholdMode
            //
            _cboSICNoiseThresholdMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboSICNoiseThresholdMode.Location = new System.Drawing.Point(144, 28);
            _cboSICNoiseThresholdMode.Name = "cboSICNoiseThresholdMode";
            _cboSICNoiseThresholdMode.Size = new System.Drawing.Size(211, 24);
            _cboSICNoiseThresholdMode.TabIndex = 1;
            //
            // lblNoiseThresholdMode
            //
            _lblNoiseThresholdMode.Location = new System.Drawing.Point(19, 30);
            _lblNoiseThresholdMode.Name = "lblNoiseThresholdMode";
            _lblNoiseThresholdMode.Size = new System.Drawing.Size(115, 18);
            _lblNoiseThresholdMode.TabIndex = 0;
            _lblNoiseThresholdMode.Text = "Threshold Mode:";
            //
            // txtSICNoiseFractionLowIntensityDataToAverage
            //
            _txtSICNoiseFractionLowIntensityDataToAverage.Location = new System.Drawing.Point(298, 92);
            _txtSICNoiseFractionLowIntensityDataToAverage.Name = "txtSICNoiseFractionLowIntensityDataToAverage";
            _txtSICNoiseFractionLowIntensityDataToAverage.Size = new System.Drawing.Size(67, 22);
            _txtSICNoiseFractionLowIntensityDataToAverage.TabIndex = 5;
            _txtSICNoiseFractionLowIntensityDataToAverage.Text = "0.75";
            //
            // lblSICNoiseFractionLowIntensityDataToAverage
            //
            _lblSICNoiseFractionLowIntensityDataToAverage.Location = new System.Drawing.Point(19, 95);
            _lblSICNoiseFractionLowIntensityDataToAverage.Name = "lblSICNoiseFractionLowIntensityDataToAverage";
            _lblSICNoiseFractionLowIntensityDataToAverage.Size = new System.Drawing.Size(231, 16);
            _lblSICNoiseFractionLowIntensityDataToAverage.TabIndex = 4;
            _lblSICNoiseFractionLowIntensityDataToAverage.Text = "Fraction low intensity data to average";
            //
            // txtSICNoiseThresholdIntensity
            //
            _txtSICNoiseThresholdIntensity.Location = new System.Drawing.Point(298, 65);
            _txtSICNoiseThresholdIntensity.Name = "txtSICNoiseThresholdIntensity";
            _txtSICNoiseThresholdIntensity.Size = new System.Drawing.Size(67, 22);
            _txtSICNoiseThresholdIntensity.TabIndex = 3;
            _txtSICNoiseThresholdIntensity.Text = "0";
            //
            // lblSICNoiseThresholdIntensity
            //
            _lblSICNoiseThresholdIntensity.Location = new System.Drawing.Point(19, 67);
            _lblSICNoiseThresholdIntensity.Name = "lblSICNoiseThresholdIntensity";
            _lblSICNoiseThresholdIntensity.Size = new System.Drawing.Size(240, 18);
            _lblSICNoiseThresholdIntensity.TabIndex = 2;
            _lblSICNoiseThresholdIntensity.Text = "Absolute Noise Thereshold Intensity";
            //
            // fraSmoothingOptions
            //
            _fraSmoothingOptions.Controls.Add(_chkSmoothDataRegardlessOfMinimumPeakWidth);
            _fraSmoothingOptions.Controls.Add(_chkFindPeaksOnSmoothedData);
            _fraSmoothingOptions.Controls.Add(_optUseSavitzkyGolaySmooth);
            _fraSmoothingOptions.Controls.Add(_txtButterworthSamplingFrequency);
            _fraSmoothingOptions.Controls.Add(_lblButterworthSamplingFrequency);
            _fraSmoothingOptions.Controls.Add(_txtSavitzkyGolayFilterOrder);
            _fraSmoothingOptions.Controls.Add(_lblSavitzkyGolayFilterOrder);
            _fraSmoothingOptions.Controls.Add(_optUseButterworthSmooth);
            _fraSmoothingOptions.Location = new System.Drawing.Point(422, 9);
            _fraSmoothingOptions.Name = "fraSmoothingOptions";
            _fraSmoothingOptions.Size = new System.Drawing.Size(298, 259);
            _fraSmoothingOptions.TabIndex = 2;
            _fraSmoothingOptions.TabStop = false;
            _fraSmoothingOptions.Text = "Smoothing Options";
            //
            // chkSmoothDataRegardlessOfMinimumPeakWidth
            //
            _chkSmoothDataRegardlessOfMinimumPeakWidth.Checked = true;
            _chkSmoothDataRegardlessOfMinimumPeakWidth.CheckState = CheckState.Checked;
            _chkSmoothDataRegardlessOfMinimumPeakWidth.Location = new System.Drawing.Point(29, 203);
            _chkSmoothDataRegardlessOfMinimumPeakWidth.Name = "chkSmoothDataRegardlessOfMinimumPeakWidth";
            _chkSmoothDataRegardlessOfMinimumPeakWidth.Size = new System.Drawing.Size(192, 46);
            _chkSmoothDataRegardlessOfMinimumPeakWidth.TabIndex = 7;
            _chkSmoothDataRegardlessOfMinimumPeakWidth.Text = "Smooth Data Regardless Of Minimum Peak Width";
            //
            // chkFindPeaksOnSmoothedData
            //
            _chkFindPeaksOnSmoothedData.Checked = true;
            _chkFindPeaksOnSmoothedData.CheckState = CheckState.Checked;
            _chkFindPeaksOnSmoothedData.Location = new System.Drawing.Point(29, 175);
            _chkFindPeaksOnSmoothedData.Name = "chkFindPeaksOnSmoothedData";
            _chkFindPeaksOnSmoothedData.Size = new System.Drawing.Size(249, 19);
            _chkFindPeaksOnSmoothedData.TabIndex = 6;
            _chkFindPeaksOnSmoothedData.Text = "Find Peaks On Smoothed Data";
            //
            // optUseSavitzkyGolaySmooth
            //
            _optUseSavitzkyGolaySmooth.Location = new System.Drawing.Point(19, 92);
            _optUseSavitzkyGolaySmooth.Name = "optUseSavitzkyGolaySmooth";
            _optUseSavitzkyGolaySmooth.Size = new System.Drawing.Size(240, 19);
            _optUseSavitzkyGolaySmooth.TabIndex = 3;
            _optUseSavitzkyGolaySmooth.Text = "Use Savitzky Golay Smooth";
            //
            // txtButterworthSamplingFrequency
            //
            _txtButterworthSamplingFrequency.Location = new System.Drawing.Point(134, 55);
            _txtButterworthSamplingFrequency.Name = "txtButterworthSamplingFrequency";
            _txtButterworthSamplingFrequency.Size = new System.Drawing.Size(58, 22);
            _txtButterworthSamplingFrequency.TabIndex = 2;
            _txtButterworthSamplingFrequency.Text = "0.25";
            //
            // lblButterworthSamplingFrequency
            //
            _lblButterworthSamplingFrequency.Location = new System.Drawing.Point(38, 55);
            _lblButterworthSamplingFrequency.Name = "lblButterworthSamplingFrequency";
            _lblButterworthSamplingFrequency.Size = new System.Drawing.Size(87, 19);
            _lblButterworthSamplingFrequency.TabIndex = 1;
            _lblButterworthSamplingFrequency.Text = "Filter Order";
            //
            // txtSavitzkyGolayFilterOrder
            //
            _txtSavitzkyGolayFilterOrder.Location = new System.Drawing.Point(134, 120);
            _txtSavitzkyGolayFilterOrder.Name = "txtSavitzkyGolayFilterOrder";
            _txtSavitzkyGolayFilterOrder.Size = new System.Drawing.Size(58, 22);
            _txtSavitzkyGolayFilterOrder.TabIndex = 5;
            _txtSavitzkyGolayFilterOrder.Text = "0";
            //
            // lblSavitzkyGolayFilterOrder
            //
            _lblSavitzkyGolayFilterOrder.Location = new System.Drawing.Point(38, 120);
            _lblSavitzkyGolayFilterOrder.Name = "lblSavitzkyGolayFilterOrder";
            _lblSavitzkyGolayFilterOrder.Size = new System.Drawing.Size(87, 18);
            _lblSavitzkyGolayFilterOrder.TabIndex = 4;
            _lblSavitzkyGolayFilterOrder.Text = "Filter Order";
            //
            // optUseButterworthSmooth
            //
            _optUseButterworthSmooth.Checked = true;
            _optUseButterworthSmooth.Location = new System.Drawing.Point(19, 28);
            _optUseButterworthSmooth.Name = "optUseButterworthSmooth";
            _optUseButterworthSmooth.Size = new System.Drawing.Size(240, 18);
            _optUseButterworthSmooth.TabIndex = 0;
            _optUseButterworthSmooth.TabStop = true;
            _optUseButterworthSmooth.Text = "Use Butterworth Smooth";
            //
            // fraPeakFindingOptions
            //
            _fraPeakFindingOptions.Controls.Add(_txtInitialPeakWidthScansMaximum);
            _fraPeakFindingOptions.Controls.Add(_lblInitialPeakWidthScansMaximum);
            _fraPeakFindingOptions.Controls.Add(_txtInitialPeakWidthScansScaler);
            _fraPeakFindingOptions.Controls.Add(_lblInitialPeakWidthScansScaler);
            _fraPeakFindingOptions.Controls.Add(_txtMaxAllowedUpwardSpikeFractionMax);
            _fraPeakFindingOptions.Controls.Add(_lblMaxAllowedUpwardSpikeFractionMax);
            _fraPeakFindingOptions.Controls.Add(_txtMaxDistanceScansNoOverlap);
            _fraPeakFindingOptions.Controls.Add(_lblMaxDistanceScansNoOverlap);
            _fraPeakFindingOptions.Location = new System.Drawing.Point(19, 166);
            _fraPeakFindingOptions.Name = "fraPeakFindingOptions";
            _fraPeakFindingOptions.Size = new System.Drawing.Size(384, 148);
            _fraPeakFindingOptions.TabIndex = 1;
            _fraPeakFindingOptions.TabStop = false;
            _fraPeakFindingOptions.Text = "Fine Tuning Peak Finding";
            //
            // txtInitialPeakWidthScansMaximum
            //
            _txtInitialPeakWidthScansMaximum.Location = new System.Drawing.Point(288, 111);
            _txtInitialPeakWidthScansMaximum.Name = "txtInitialPeakWidthScansMaximum";
            _txtInitialPeakWidthScansMaximum.Size = new System.Drawing.Size(67, 22);
            _txtInitialPeakWidthScansMaximum.TabIndex = 7;
            _txtInitialPeakWidthScansMaximum.Text = "30";
            //
            // lblInitialPeakWidthScansMaximum
            //
            _lblInitialPeakWidthScansMaximum.Location = new System.Drawing.Point(19, 113);
            _lblInitialPeakWidthScansMaximum.Name = "lblInitialPeakWidthScansMaximum";
            _lblInitialPeakWidthScansMaximum.Size = new System.Drawing.Size(240, 19);
            _lblInitialPeakWidthScansMaximum.TabIndex = 6;
            _lblInitialPeakWidthScansMaximum.Text = "Initial Peak Width Maximum (Scans)";
            //
            // txtInitialPeakWidthScansScaler
            //
            _txtInitialPeakWidthScansScaler.Location = new System.Drawing.Point(288, 83);
            _txtInitialPeakWidthScansScaler.Name = "txtInitialPeakWidthScansScaler";
            _txtInitialPeakWidthScansScaler.Size = new System.Drawing.Size(67, 22);
            _txtInitialPeakWidthScansScaler.TabIndex = 5;
            _txtInitialPeakWidthScansScaler.Text = "1";
            //
            // lblInitialPeakWidthScansScaler
            //
            _lblInitialPeakWidthScansScaler.Location = new System.Drawing.Point(19, 85);
            _lblInitialPeakWidthScansScaler.Name = "lblInitialPeakWidthScansScaler";
            _lblInitialPeakWidthScansScaler.Size = new System.Drawing.Size(240, 19);
            _lblInitialPeakWidthScansScaler.TabIndex = 4;
            _lblInitialPeakWidthScansScaler.Text = "Initial Peak Width Scaler (Scans)";
            //
            // txtMaxAllowedUpwardSpikeFractionMax
            //
            _txtMaxAllowedUpwardSpikeFractionMax.Location = new System.Drawing.Point(288, 55);
            _txtMaxAllowedUpwardSpikeFractionMax.Name = "txtMaxAllowedUpwardSpikeFractionMax";
            _txtMaxAllowedUpwardSpikeFractionMax.Size = new System.Drawing.Size(67, 22);
            _txtMaxAllowedUpwardSpikeFractionMax.TabIndex = 3;
            _txtMaxAllowedUpwardSpikeFractionMax.Text = "0.2";
            //
            // lblMaxAllowedUpwardSpikeFractionMax
            //
            _lblMaxAllowedUpwardSpikeFractionMax.Location = new System.Drawing.Point(19, 58);
            _lblMaxAllowedUpwardSpikeFractionMax.Name = "lblMaxAllowedUpwardSpikeFractionMax";
            _lblMaxAllowedUpwardSpikeFractionMax.Size = new System.Drawing.Size(279, 18);
            _lblMaxAllowedUpwardSpikeFractionMax.TabIndex = 2;
            _lblMaxAllowedUpwardSpikeFractionMax.Text = "Max Allowed Upward Spike (Fraction Max)";
            //
            // txtMaxDistanceScansNoOverlap
            //
            _txtMaxDistanceScansNoOverlap.Location = new System.Drawing.Point(288, 28);
            _txtMaxDistanceScansNoOverlap.Name = "txtMaxDistanceScansNoOverlap";
            _txtMaxDistanceScansNoOverlap.Size = new System.Drawing.Size(67, 22);
            _txtMaxDistanceScansNoOverlap.TabIndex = 1;
            _txtMaxDistanceScansNoOverlap.Text = "0";
            //
            // lblMaxDistanceScansNoOverlap
            //
            _lblMaxDistanceScansNoOverlap.Location = new System.Drawing.Point(19, 30);
            _lblMaxDistanceScansNoOverlap.Name = "lblMaxDistanceScansNoOverlap";
            _lblMaxDistanceScansNoOverlap.Size = new System.Drawing.Size(240, 18);
            _lblMaxDistanceScansNoOverlap.TabIndex = 0;
            _lblMaxDistanceScansNoOverlap.Text = "Max Distance No Overlap (Scans)";
            //
            // TabPageBinningAndSimilarityOptions
            //
            _TabPageBinningAndSimilarityOptions.Controls.Add(_fraMassSpectraNoiseThresholds);
            _TabPageBinningAndSimilarityOptions.Controls.Add(_fraBinningIntensityOptions);
            _TabPageBinningAndSimilarityOptions.Controls.Add(_fraSpectrumSimilarityOptions);
            _TabPageBinningAndSimilarityOptions.Controls.Add(_fraBinningMZOptions);
            _TabPageBinningAndSimilarityOptions.Location = new System.Drawing.Point(4, 25);
            _TabPageBinningAndSimilarityOptions.Name = "TabPageBinningAndSimilarityOptions";
            _TabPageBinningAndSimilarityOptions.Size = new System.Drawing.Size(882, 327);
            _TabPageBinningAndSimilarityOptions.TabIndex = 6;
            _TabPageBinningAndSimilarityOptions.Text = "Binning and Similarity";
            _TabPageBinningAndSimilarityOptions.UseVisualStyleBackColor = true;
            //
            // fraMassSpectraNoiseThresholds
            //
            _fraMassSpectraNoiseThresholds.Controls.Add(_txtMassSpectraNoiseMinimumSignalToNoiseRatio);
            _fraMassSpectraNoiseThresholds.Controls.Add(_lblMassSpectraNoiseMinimumSignalToNoiseRatio);
            _fraMassSpectraNoiseThresholds.Controls.Add(_txtMassSpectraNoiseThresholdIntensity);
            _fraMassSpectraNoiseThresholds.Controls.Add(_txtMassSpectraNoiseFractionLowIntensityDataToAverage);
            _fraMassSpectraNoiseThresholds.Controls.Add(_lblMassSpectraNoiseFractionLowIntensityDataToAverage);
            _fraMassSpectraNoiseThresholds.Controls.Add(_cboMassSpectraNoiseThresholdMode);
            _fraMassSpectraNoiseThresholds.Controls.Add(_lblMassSpectraNoiseThresholdMode);
            _fraMassSpectraNoiseThresholds.Controls.Add(_lblMassSpectraNoiseThresholdIntensity);
            _fraMassSpectraNoiseThresholds.Location = new System.Drawing.Point(10, 18);
            _fraMassSpectraNoiseThresholds.Name = "fraMassSpectraNoiseThresholds";
            _fraMassSpectraNoiseThresholds.Size = new System.Drawing.Size(412, 148);
            _fraMassSpectraNoiseThresholds.TabIndex = 0;
            _fraMassSpectraNoiseThresholds.TabStop = false;
            _fraMassSpectraNoiseThresholds.Text = "Noise Threshold Determination for Mass Spectra";
            //
            // txtMassSpectraNoiseMinimumSignalToNoiseRatio
            //
            _txtMassSpectraNoiseMinimumSignalToNoiseRatio.Location = new System.Drawing.Point(250, 120);
            _txtMassSpectraNoiseMinimumSignalToNoiseRatio.Name = "txtMassSpectraNoiseMinimumSignalToNoiseRatio";
            _txtMassSpectraNoiseMinimumSignalToNoiseRatio.Size = new System.Drawing.Size(67, 22);
            _txtMassSpectraNoiseMinimumSignalToNoiseRatio.TabIndex = 9;
            _txtMassSpectraNoiseMinimumSignalToNoiseRatio.Text = "2";
            //
            // lblMassSpectraNoiseMinimumSignalToNoiseRatio
            //
            _lblMassSpectraNoiseMinimumSignalToNoiseRatio.Location = new System.Drawing.Point(10, 120);
            _lblMassSpectraNoiseMinimumSignalToNoiseRatio.Name = "lblMassSpectraNoiseMinimumSignalToNoiseRatio";
            _lblMassSpectraNoiseMinimumSignalToNoiseRatio.Size = new System.Drawing.Size(230, 16);
            _lblMassSpectraNoiseMinimumSignalToNoiseRatio.TabIndex = 8;
            _lblMassSpectraNoiseMinimumSignalToNoiseRatio.Text = "Minimum Signal to Noise Ratio";
            //
            // txtMassSpectraNoiseThresholdIntensity
            //
            _txtMassSpectraNoiseThresholdIntensity.Location = new System.Drawing.Point(250, 65);
            _txtMassSpectraNoiseThresholdIntensity.Name = "txtMassSpectraNoiseThresholdIntensity";
            _txtMassSpectraNoiseThresholdIntensity.Size = new System.Drawing.Size(67, 22);
            _txtMassSpectraNoiseThresholdIntensity.TabIndex = 3;
            _txtMassSpectraNoiseThresholdIntensity.Text = "0";
            //
            // txtMassSpectraNoiseFractionLowIntensityDataToAverage
            //
            _txtMassSpectraNoiseFractionLowIntensityDataToAverage.Location = new System.Drawing.Point(250, 92);
            _txtMassSpectraNoiseFractionLowIntensityDataToAverage.Name = "txtMassSpectraNoiseFractionLowIntensityDataToAverage";
            _txtMassSpectraNoiseFractionLowIntensityDataToAverage.Size = new System.Drawing.Size(67, 22);
            _txtMassSpectraNoiseFractionLowIntensityDataToAverage.TabIndex = 5;
            _txtMassSpectraNoiseFractionLowIntensityDataToAverage.Text = "0.5";
            //
            // lblMassSpectraNoiseFractionLowIntensityDataToAverage
            //
            _lblMassSpectraNoiseFractionLowIntensityDataToAverage.Location = new System.Drawing.Point(10, 92);
            _lblMassSpectraNoiseFractionLowIntensityDataToAverage.Name = "lblMassSpectraNoiseFractionLowIntensityDataToAverage";
            _lblMassSpectraNoiseFractionLowIntensityDataToAverage.Size = new System.Drawing.Size(230, 26);
            _lblMassSpectraNoiseFractionLowIntensityDataToAverage.TabIndex = 4;
            _lblMassSpectraNoiseFractionLowIntensityDataToAverage.Text = "Fraction low intensity data to average";
            //
            // cboMassSpectraNoiseThresholdMode
            //
            _cboMassSpectraNoiseThresholdMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboMassSpectraNoiseThresholdMode.Location = new System.Drawing.Point(163, 28);
            _cboMassSpectraNoiseThresholdMode.Name = "cboMassSpectraNoiseThresholdMode";
            _cboMassSpectraNoiseThresholdMode.Size = new System.Drawing.Size(211, 24);
            _cboMassSpectraNoiseThresholdMode.TabIndex = 1;
            //
            // lblMassSpectraNoiseThresholdMode
            //
            _lblMassSpectraNoiseThresholdMode.Location = new System.Drawing.Point(10, 37);
            _lblMassSpectraNoiseThresholdMode.Name = "lblMassSpectraNoiseThresholdMode";
            _lblMassSpectraNoiseThresholdMode.Size = new System.Drawing.Size(163, 18);
            _lblMassSpectraNoiseThresholdMode.TabIndex = 0;
            _lblMassSpectraNoiseThresholdMode.Text = "Noise Threshold Mode:";
            //
            // lblMassSpectraNoiseThresholdIntensity
            //
            _lblMassSpectraNoiseThresholdIntensity.Location = new System.Drawing.Point(10, 65);
            _lblMassSpectraNoiseThresholdIntensity.Name = "lblMassSpectraNoiseThresholdIntensity";
            _lblMassSpectraNoiseThresholdIntensity.Size = new System.Drawing.Size(220, 18);
            _lblMassSpectraNoiseThresholdIntensity.TabIndex = 2;
            _lblMassSpectraNoiseThresholdIntensity.Text = "Absolute Noise Threshold Intensity";
            //
            // fraBinningIntensityOptions
            //
            _fraBinningIntensityOptions.Controls.Add(_lblBinnedDataIntensityPrecisionPctUnits);
            _fraBinningIntensityOptions.Controls.Add(_chkBinnedDataSumAllIntensitiesForBin);
            _fraBinningIntensityOptions.Controls.Add(_chkBinnedDataNormalize);
            _fraBinningIntensityOptions.Controls.Add(_txtBinnedDataIntensityPrecisionPct);
            _fraBinningIntensityOptions.Controls.Add(_lblBinnedDataIntensityPrecisionPct);
            _fraBinningIntensityOptions.Location = new System.Drawing.Point(442, 175);
            _fraBinningIntensityOptions.Name = "fraBinningIntensityOptions";
            _fraBinningIntensityOptions.Size = new System.Drawing.Size(288, 120);
            _fraBinningIntensityOptions.TabIndex = 3;
            _fraBinningIntensityOptions.TabStop = false;
            _fraBinningIntensityOptions.Text = "Binning Intensity Options";
            //
            // lblBinnedDataIntensityPrecisionPctUnits
            //
            _lblBinnedDataIntensityPrecisionPctUnits.Location = new System.Drawing.Point(259, 28);
            _lblBinnedDataIntensityPrecisionPctUnits.Name = "lblBinnedDataIntensityPrecisionPctUnits";
            _lblBinnedDataIntensityPrecisionPctUnits.Size = new System.Drawing.Size(19, 18);
            _lblBinnedDataIntensityPrecisionPctUnits.TabIndex = 8;
            _lblBinnedDataIntensityPrecisionPctUnits.Text = "%";
            //
            // chkBinnedDataSumAllIntensitiesForBin
            //
            _chkBinnedDataSumAllIntensitiesForBin.Location = new System.Drawing.Point(10, 92);
            _chkBinnedDataSumAllIntensitiesForBin.Name = "chkBinnedDataSumAllIntensitiesForBin";
            _chkBinnedDataSumAllIntensitiesForBin.Size = new System.Drawing.Size(192, 19);
            _chkBinnedDataSumAllIntensitiesForBin.TabIndex = 10;
            _chkBinnedDataSumAllIntensitiesForBin.Text = "Sum All Intensities For Bin";
            //
            // chkBinnedDataNormalize
            //
            _chkBinnedDataNormalize.Location = new System.Drawing.Point(10, 65);
            _chkBinnedDataNormalize.Name = "chkBinnedDataNormalize";
            _chkBinnedDataNormalize.Size = new System.Drawing.Size(163, 18);
            _chkBinnedDataNormalize.TabIndex = 9;
            _chkBinnedDataNormalize.Text = "Normalize Intensities";
            //
            // txtBinnedDataIntensityPrecisionPct
            //
            _txtBinnedDataIntensityPrecisionPct.Location = new System.Drawing.Point(211, 28);
            _txtBinnedDataIntensityPrecisionPct.Name = "txtBinnedDataIntensityPrecisionPct";
            _txtBinnedDataIntensityPrecisionPct.Size = new System.Drawing.Size(48, 22);
            _txtBinnedDataIntensityPrecisionPct.TabIndex = 7;
            _txtBinnedDataIntensityPrecisionPct.Text = "1";
            //
            // lblBinnedDataIntensityPrecisionPct
            //
            _lblBinnedDataIntensityPrecisionPct.Location = new System.Drawing.Point(10, 30);
            _lblBinnedDataIntensityPrecisionPct.Name = "lblBinnedDataIntensityPrecisionPct";
            _lblBinnedDataIntensityPrecisionPct.Size = new System.Drawing.Size(201, 18);
            _lblBinnedDataIntensityPrecisionPct.TabIndex = 6;
            _lblBinnedDataIntensityPrecisionPct.Text = "Intensity Precision (0.1 to 100)";
            //
            // fraSpectrumSimilarityOptions
            //
            _fraSpectrumSimilarityOptions.Controls.Add(_lblSimilarIonTimeToleranceHalfWidthUnits);
            _fraSpectrumSimilarityOptions.Controls.Add(_txtSpectrumSimilarityMinimum);
            _fraSpectrumSimilarityOptions.Controls.Add(_lblSpectrumSimilarityMinimum);
            _fraSpectrumSimilarityOptions.Controls.Add(_txtSimilarIonToleranceHalfWidthMinutes);
            _fraSpectrumSimilarityOptions.Controls.Add(_lblSimilarIonTimeToleranceHalfWidth);
            _fraSpectrumSimilarityOptions.Controls.Add(_txtSimilarIonMZToleranceHalfWidth);
            _fraSpectrumSimilarityOptions.Controls.Add(_lblSimilarIonMZToleranceHalfWidth);
            _fraSpectrumSimilarityOptions.Location = new System.Drawing.Point(10, 175);
            _fraSpectrumSimilarityOptions.Name = "fraSpectrumSimilarityOptions";
            _fraSpectrumSimilarityOptions.Size = new System.Drawing.Size(326, 120);
            _fraSpectrumSimilarityOptions.TabIndex = 1;
            _fraSpectrumSimilarityOptions.TabStop = false;
            _fraSpectrumSimilarityOptions.Text = "Spectrum Similarity Options";
            //
            // lblSimilarIonTimeToleranceHalfWidthUnits
            //
            _lblSimilarIonTimeToleranceHalfWidthUnits.Location = new System.Drawing.Point(259, 58);
            _lblSimilarIonTimeToleranceHalfWidthUnits.Name = "lblSimilarIonTimeToleranceHalfWidthUnits";
            _lblSimilarIonTimeToleranceHalfWidthUnits.Size = new System.Drawing.Size(58, 18);
            _lblSimilarIonTimeToleranceHalfWidthUnits.TabIndex = 6;
            _lblSimilarIonTimeToleranceHalfWidthUnits.Text = "minutes";
            //
            // txtSpectrumSimilarityMinimum
            //
            _txtSpectrumSimilarityMinimum.Location = new System.Drawing.Point(202, 83);
            _txtSpectrumSimilarityMinimum.Name = "txtSpectrumSimilarityMinimum";
            _txtSpectrumSimilarityMinimum.Size = new System.Drawing.Size(48, 22);
            _txtSpectrumSimilarityMinimum.TabIndex = 5;
            _txtSpectrumSimilarityMinimum.Text = "0.7";
            //
            // lblSpectrumSimilarityMinimum
            //
            _lblSpectrumSimilarityMinimum.Location = new System.Drawing.Point(10, 85);
            _lblSpectrumSimilarityMinimum.Name = "lblSpectrumSimilarityMinimum";
            _lblSpectrumSimilarityMinimum.Size = new System.Drawing.Size(180, 19);
            _lblSpectrumSimilarityMinimum.TabIndex = 4;
            _lblSpectrumSimilarityMinimum.Text = "Minimum Similarity (0 to 1)";
            //
            // txtSimilarIonToleranceHalfWidthMinutes
            //
            _txtSimilarIonToleranceHalfWidthMinutes.Location = new System.Drawing.Point(202, 55);
            _txtSimilarIonToleranceHalfWidthMinutes.Name = "txtSimilarIonToleranceHalfWidthMinutes";
            _txtSimilarIonToleranceHalfWidthMinutes.Size = new System.Drawing.Size(48, 22);
            _txtSimilarIonToleranceHalfWidthMinutes.TabIndex = 3;
            _txtSimilarIonToleranceHalfWidthMinutes.Text = "5";
            //
            // lblSimilarIonTimeToleranceHalfWidth
            //
            _lblSimilarIonTimeToleranceHalfWidth.Location = new System.Drawing.Point(10, 58);
            _lblSimilarIonTimeToleranceHalfWidth.Name = "lblSimilarIonTimeToleranceHalfWidth";
            _lblSimilarIonTimeToleranceHalfWidth.Size = new System.Drawing.Size(180, 18);
            _lblSimilarIonTimeToleranceHalfWidth.TabIndex = 2;
            _lblSimilarIonTimeToleranceHalfWidth.Text = "Time Tolerance Half Width";
            //
            // txtSimilarIonMZToleranceHalfWidth
            //
            _txtSimilarIonMZToleranceHalfWidth.Location = new System.Drawing.Point(202, 28);
            _txtSimilarIonMZToleranceHalfWidth.Name = "txtSimilarIonMZToleranceHalfWidth";
            _txtSimilarIonMZToleranceHalfWidth.Size = new System.Drawing.Size(48, 22);
            _txtSimilarIonMZToleranceHalfWidth.TabIndex = 1;
            _txtSimilarIonMZToleranceHalfWidth.Text = "0.1";
            //
            // lblSimilarIonMZToleranceHalfWidth
            //
            _lblSimilarIonMZToleranceHalfWidth.Location = new System.Drawing.Point(10, 30);
            _lblSimilarIonMZToleranceHalfWidth.Name = "lblSimilarIonMZToleranceHalfWidth";
            _lblSimilarIonMZToleranceHalfWidth.Size = new System.Drawing.Size(180, 18);
            _lblSimilarIonMZToleranceHalfWidth.TabIndex = 0;
            _lblSimilarIonMZToleranceHalfWidth.Text = "m/z Tolerance Half Width";
            //
            // fraBinningMZOptions
            //
            _fraBinningMZOptions.Controls.Add(_txtMaximumBinCount);
            _fraBinningMZOptions.Controls.Add(_lblMaximumBinCount);
            _fraBinningMZOptions.Controls.Add(_txtBinSize);
            _fraBinningMZOptions.Controls.Add(_lblBinSize);
            _fraBinningMZOptions.Controls.Add(_txtBinEndX);
            _fraBinningMZOptions.Controls.Add(_lblBinEndX);
            _fraBinningMZOptions.Controls.Add(_txtBinStartX);
            _fraBinningMZOptions.Controls.Add(_lblBinStartX);
            _fraBinningMZOptions.Location = new System.Drawing.Point(442, 18);
            _fraBinningMZOptions.Name = "fraBinningMZOptions";
            _fraBinningMZOptions.Size = new System.Drawing.Size(288, 148);
            _fraBinningMZOptions.TabIndex = 2;
            _fraBinningMZOptions.TabStop = false;
            _fraBinningMZOptions.Text = "Binning m/z Options";
            //
            // txtMaximumBinCount
            //
            _txtMaximumBinCount.Location = new System.Drawing.Point(182, 111);
            _txtMaximumBinCount.Name = "txtMaximumBinCount";
            _txtMaximumBinCount.Size = new System.Drawing.Size(68, 22);
            _txtMaximumBinCount.TabIndex = 7;
            _txtMaximumBinCount.Text = "100000";
            //
            // lblMaximumBinCount
            //
            _lblMaximumBinCount.Location = new System.Drawing.Point(19, 113);
            _lblMaximumBinCount.Name = "lblMaximumBinCount";
            _lblMaximumBinCount.Size = new System.Drawing.Size(154, 19);
            _lblMaximumBinCount.TabIndex = 6;
            _lblMaximumBinCount.Text = "Maximum Bin Count";
            //
            // txtBinSize
            //
            _txtBinSize.Location = new System.Drawing.Point(182, 83);
            _txtBinSize.Name = "txtBinSize";
            _txtBinSize.Size = new System.Drawing.Size(68, 22);
            _txtBinSize.TabIndex = 5;
            _txtBinSize.Text = "1";
            //
            // lblBinSize
            //
            _lblBinSize.Location = new System.Drawing.Point(19, 85);
            _lblBinSize.Name = "lblBinSize";
            _lblBinSize.Size = new System.Drawing.Size(154, 19);
            _lblBinSize.TabIndex = 4;
            _lblBinSize.Text = "Bin Size (m/z units)";
            //
            // txtBinEndX
            //
            _txtBinEndX.Location = new System.Drawing.Point(182, 55);
            _txtBinEndX.Name = "txtBinEndX";
            _txtBinEndX.Size = new System.Drawing.Size(68, 22);
            _txtBinEndX.TabIndex = 3;
            _txtBinEndX.Text = "2000";
            //
            // lblBinEndX
            //
            _lblBinEndX.Location = new System.Drawing.Point(19, 58);
            _lblBinEndX.Name = "lblBinEndX";
            _lblBinEndX.Size = new System.Drawing.Size(144, 18);
            _lblBinEndX.TabIndex = 2;
            _lblBinEndX.Text = "Bin End m/z";
            //
            // txtBinStartX
            //
            _txtBinStartX.Location = new System.Drawing.Point(182, 28);
            _txtBinStartX.Name = "txtBinStartX";
            _txtBinStartX.Size = new System.Drawing.Size(68, 22);
            _txtBinStartX.TabIndex = 1;
            _txtBinStartX.Text = "50";
            //
            // lblBinStartX
            //
            _lblBinStartX.Location = new System.Drawing.Point(19, 30);
            _lblBinStartX.Name = "lblBinStartX";
            _lblBinStartX.Size = new System.Drawing.Size(144, 18);
            _lblBinStartX.TabIndex = 0;
            _lblBinStartX.Text = "Bin Start m/z";
            //
            // TabPageCustomSICOptions
            //
            _TabPageCustomSICOptions.Controls.Add(_txtCustomSICFileDescription);
            _TabPageCustomSICOptions.Controls.Add(_cmdSelectCustomSICFile);
            _TabPageCustomSICOptions.Controls.Add(_txtCustomSICFileName);
            _TabPageCustomSICOptions.Controls.Add(_fraCustomSICControls);
            _TabPageCustomSICOptions.Controls.Add(_dgCustomSICValues);
            _TabPageCustomSICOptions.Location = new System.Drawing.Point(4, 25);
            _TabPageCustomSICOptions.Name = "TabPageCustomSICOptions";
            _TabPageCustomSICOptions.Size = new System.Drawing.Size(882, 327);
            _TabPageCustomSICOptions.TabIndex = 3;
            _TabPageCustomSICOptions.Text = "Custom SIC Options";
            _TabPageCustomSICOptions.UseVisualStyleBackColor = true;
            //
            // txtCustomSICFileDescription
            //
            _txtCustomSICFileDescription.Location = new System.Drawing.Point(10, 6);
            _txtCustomSICFileDescription.Multiline = true;
            _txtCustomSICFileDescription.Name = "txtCustomSICFileDescription";
            _txtCustomSICFileDescription.ReadOnly = true;
            _txtCustomSICFileDescription.ScrollBars = ScrollBars.Vertical;
            _txtCustomSICFileDescription.Size = new System.Drawing.Size(582, 59);
            _txtCustomSICFileDescription.TabIndex = 0;
            _txtCustomSICFileDescription.Text = "Custom SIC description ... populated via code.";
            //
            // cmdSelectCustomSICFile
            //
            _cmdSelectCustomSICFile.Location = new System.Drawing.Point(10, 74);
            _cmdSelectCustomSICFile.Name = "cmdSelectCustomSICFile";
            _cmdSelectCustomSICFile.Size = new System.Drawing.Size(96, 28);
            _cmdSelectCustomSICFile.TabIndex = 1;
            _cmdSelectCustomSICFile.Text = "&Select File";
            //
            // txtCustomSICFileName
            //
            _txtCustomSICFileName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _txtCustomSICFileName.Location = new System.Drawing.Point(125, 74);
            _txtCustomSICFileName.Name = "txtCustomSICFileName";
            _txtCustomSICFileName.Size = new System.Drawing.Size(534, 22);
            _txtCustomSICFileName.TabIndex = 2;
            //
            // fraCustomSICControls
            //
            _fraCustomSICControls.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _fraCustomSICControls.Controls.Add(_lblCustomSICToleranceType);
            _fraCustomSICControls.Controls.Add(_optCustomSICScanToleranceAcqTime);
            _fraCustomSICControls.Controls.Add(_optCustomSICScanToleranceRelative);
            _fraCustomSICControls.Controls.Add(_optCustomSICScanToleranceAbsolute);
            _fraCustomSICControls.Controls.Add(_chkLimitSearchToCustomMZs);
            _fraCustomSICControls.Controls.Add(_txtCustomSICScanOrAcqTimeTolerance);
            _fraCustomSICControls.Controls.Add(_lblCustomSICScanTolerance);
            _fraCustomSICControls.Controls.Add(_cmdPasteCustomSICList);
            _fraCustomSICControls.Controls.Add(_cmdCustomSICValuesPopulate);
            _fraCustomSICControls.Controls.Add(_cmdClearCustomSICList);
            _fraCustomSICControls.Location = new System.Drawing.Point(666, 9);
            _fraCustomSICControls.Name = "fraCustomSICControls";
            _fraCustomSICControls.Size = new System.Drawing.Size(201, 305);
            _fraCustomSICControls.TabIndex = 4;
            _fraCustomSICControls.TabStop = false;
            //
            // lblCustomSICToleranceType
            //
            _lblCustomSICToleranceType.Location = new System.Drawing.Point(6, 145);
            _lblCustomSICToleranceType.Name = "lblCustomSICToleranceType";
            _lblCustomSICToleranceType.Size = new System.Drawing.Size(106, 19);
            _lblCustomSICToleranceType.TabIndex = 5;
            _lblCustomSICToleranceType.Text = "Tolerance Type:";
            //
            // optCustomSICScanToleranceAcqTime
            //
            _optCustomSICScanToleranceAcqTime.AutoSize = true;
            _optCustomSICScanToleranceAcqTime.Location = new System.Drawing.Point(13, 215);
            _optCustomSICScanToleranceAcqTime.Name = "optCustomSICScanToleranceAcqTime";
            _optCustomSICScanToleranceAcqTime.Size = new System.Drawing.Size(190, 21);
            _optCustomSICScanToleranceAcqTime.TabIndex = 8;
            _optCustomSICScanToleranceAcqTime.Text = "Acquisition time (minutes)";
            _optCustomSICScanToleranceAcqTime.UseVisualStyleBackColor = true;
            //
            // optCustomSICScanToleranceRelative
            //
            _optCustomSICScanToleranceRelative.AutoSize = true;
            _optCustomSICScanToleranceRelative.Location = new System.Drawing.Point(13, 190);
            _optCustomSICScanToleranceRelative.Name = "optCustomSICScanToleranceRelative";
            _optCustomSICScanToleranceRelative.Size = new System.Drawing.Size(160, 21);
            _optCustomSICScanToleranceRelative.TabIndex = 7;
            _optCustomSICScanToleranceRelative.Text = "Relative time (0 to 1)";
            _optCustomSICScanToleranceRelative.UseVisualStyleBackColor = true;
            //
            // optCustomSICScanToleranceAbsolute
            //
            _optCustomSICScanToleranceAbsolute.AutoSize = true;
            _optCustomSICScanToleranceAbsolute.Checked = true;
            _optCustomSICScanToleranceAbsolute.Location = new System.Drawing.Point(13, 166);
            _optCustomSICScanToleranceAbsolute.Name = "optCustomSICScanToleranceAbsolute";
            _optCustomSICScanToleranceAbsolute.Size = new System.Drawing.Size(170, 21);
            _optCustomSICScanToleranceAbsolute.TabIndex = 6;
            _optCustomSICScanToleranceAbsolute.TabStop = true;
            _optCustomSICScanToleranceAbsolute.Text = "Absolute scan number";
            _optCustomSICScanToleranceAbsolute.UseVisualStyleBackColor = true;
            //
            // chkLimitSearchToCustomMZs
            //
            _chkLimitSearchToCustomMZs.Location = new System.Drawing.Point(10, 249);
            _chkLimitSearchToCustomMZs.Name = "chkLimitSearchToCustomMZs";
            _chkLimitSearchToCustomMZs.Size = new System.Drawing.Size(182, 51);
            _chkLimitSearchToCustomMZs.TabIndex = 9;
            _chkLimitSearchToCustomMZs.Text = "Limit search to only use custom m/z values (skip auto-fragmented m/z's)";
            //
            // txtCustomSICScanOrAcqTimeTolerance
            //
            _txtCustomSICScanOrAcqTimeTolerance.Location = new System.Drawing.Point(119, 114);
            _txtCustomSICScanOrAcqTimeTolerance.Name = "txtCustomSICScanOrAcqTimeTolerance";
            _txtCustomSICScanOrAcqTimeTolerance.Size = new System.Drawing.Size(67, 22);
            _txtCustomSICScanOrAcqTimeTolerance.TabIndex = 4;
            _txtCustomSICScanOrAcqTimeTolerance.Text = "3";
            //
            // lblCustomSICScanTolerance
            //
            _lblCustomSICScanTolerance.Location = new System.Drawing.Point(6, 118);
            _lblCustomSICScanTolerance.Name = "lblCustomSICScanTolerance";
            _lblCustomSICScanTolerance.Size = new System.Drawing.Size(106, 18);
            _lblCustomSICScanTolerance.TabIndex = 3;
            _lblCustomSICScanTolerance.Text = "Scan Tolerance";
            //
            // cmdPasteCustomSICList
            //
            _cmdPasteCustomSICList.Location = new System.Drawing.Point(10, 18);
            _cmdPasteCustomSICList.Name = "cmdPasteCustomSICList";
            _cmdPasteCustomSICList.Size = new System.Drawing.Size(80, 47);
            _cmdPasteCustomSICList.TabIndex = 0;
            _cmdPasteCustomSICList.Text = "Paste Values";
            //
            // cmdCustomSICValuesPopulate
            //
            _cmdCustomSICValuesPopulate.Location = new System.Drawing.Point(7, 72);
            _cmdCustomSICValuesPopulate.Name = "cmdCustomSICValuesPopulate";
            _cmdCustomSICValuesPopulate.Size = new System.Drawing.Size(183, 27);
            _cmdCustomSICValuesPopulate.TabIndex = 2;
            _cmdCustomSICValuesPopulate.Text = "Auto-Populate with Defaults";
            //
            // cmdClearCustomSICList
            //
            _cmdClearCustomSICList.Location = new System.Drawing.Point(107, 18);
            _cmdClearCustomSICList.Name = "cmdClearCustomSICList";
            _cmdClearCustomSICList.Size = new System.Drawing.Size(77, 47);
            _cmdClearCustomSICList.TabIndex = 1;
            _cmdClearCustomSICList.Text = "Clear List";
            //
            // dgCustomSICValues
            //
            _dgCustomSICValues.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _dgCustomSICValues.CaptionText = "Custom SIC Values";
            _dgCustomSICValues.DataMember = "";
            _dgCustomSICValues.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            _dgCustomSICValues.Location = new System.Drawing.Point(10, 120);
            _dgCustomSICValues.Name = "dgCustomSICValues";
            _dgCustomSICValues.Size = new System.Drawing.Size(649, 194);
            _dgCustomSICValues.TabIndex = 3;
            //
            // TabPageReporterIons
            //
            _TabPageReporterIons.Controls.Add(_fraDecoyOptions);
            _TabPageReporterIons.Controls.Add(_fraMRMOptions);
            _TabPageReporterIons.Controls.Add(_fraReporterIonMassMode);
            _TabPageReporterIons.Controls.Add(_fraReporterIonOptions);
            _TabPageReporterIons.Location = new System.Drawing.Point(4, 25);
            _TabPageReporterIons.Name = "TabPageReporterIons";
            _TabPageReporterIons.Size = new System.Drawing.Size(882, 327);
            _TabPageReporterIons.TabIndex = 9;
            _TabPageReporterIons.Text = "Reporter Ions / MRM";
            _TabPageReporterIons.UseVisualStyleBackColor = true;
            //
            // fraDecoyOptions
            //
            _fraDecoyOptions.Controls.Add(_lblParentIonDecoyMassDaUnits);
            _fraDecoyOptions.Controls.Add(_txtParentIonDecoyMassDa);
            _fraDecoyOptions.Controls.Add(_lblParentIonDecoyMassDa);
            _fraDecoyOptions.Location = new System.Drawing.Point(638, 181);
            _fraDecoyOptions.Name = "fraDecoyOptions";
            _fraDecoyOptions.Size = new System.Drawing.Size(191, 86);
            _fraDecoyOptions.TabIndex = 3;
            _fraDecoyOptions.TabStop = false;
            _fraDecoyOptions.Text = "Decoy Options";
            //
            // lblParentIonDecoyMassDaUnits
            //
            _lblParentIonDecoyMassDaUnits.Location = new System.Drawing.Point(86, 52);
            _lblParentIonDecoyMassDaUnits.Name = "lblParentIonDecoyMassDaUnits";
            _lblParentIonDecoyMassDaUnits.Size = new System.Drawing.Size(41, 20);
            _lblParentIonDecoyMassDaUnits.TabIndex = 2;
            _lblParentIonDecoyMassDaUnits.Text = "Da";
            //
            // txtParentIonDecoyMassDa
            //
            _txtParentIonDecoyMassDa.Location = new System.Drawing.Point(12, 48);
            _txtParentIonDecoyMassDa.Name = "txtParentIonDecoyMassDa";
            _txtParentIonDecoyMassDa.Size = new System.Drawing.Size(67, 22);
            _txtParentIonDecoyMassDa.TabIndex = 1;
            _txtParentIonDecoyMassDa.Text = "0";
            //
            // lblParentIonDecoyMassDa
            //
            _lblParentIonDecoyMassDa.Location = new System.Drawing.Point(8, 27);
            _lblParentIonDecoyMassDa.Name = "lblParentIonDecoyMassDa";
            _lblParentIonDecoyMassDa.Size = new System.Drawing.Size(170, 18);
            _lblParentIonDecoyMassDa.TabIndex = 0;
            _lblParentIonDecoyMassDa.Text = "Parent Ion Decoy Mass";
            //
            // fraMRMOptions
            //
            _fraMRMOptions.Controls.Add(_chkMRMWriteIntensityCrosstab);
            _fraMRMOptions.Controls.Add(_lblMRMInfo);
            _fraMRMOptions.Controls.Add(_chkMRMWriteDataList);
            _fraMRMOptions.Location = new System.Drawing.Point(379, 18);
            _fraMRMOptions.Name = "fraMRMOptions";
            _fraMRMOptions.Size = new System.Drawing.Size(409, 156);
            _fraMRMOptions.TabIndex = 2;
            _fraMRMOptions.TabStop = false;
            _fraMRMOptions.Text = "MRM Options";
            //
            // chkMRMWriteIntensityCrosstab
            //
            _chkMRMWriteIntensityCrosstab.Location = new System.Drawing.Point(23, 120);
            _chkMRMWriteIntensityCrosstab.Name = "chkMRMWriteIntensityCrosstab";
            _chkMRMWriteIntensityCrosstab.Size = new System.Drawing.Size(366, 21);
            _chkMRMWriteIntensityCrosstab.TabIndex = 2;
            _chkMRMWriteIntensityCrosstab.Text = "Save MRM intensity crosstab (wide, rectangular file)";
            //
            // lblMRMInfo
            //
            _lblMRMInfo.Location = new System.Drawing.Point(7, 18);
            _lblMRMInfo.Name = "lblMRMInfo";
            _lblMRMInfo.Size = new System.Drawing.Size(395, 71);
            _lblMRMInfo.TabIndex = 0;
            _lblMRMInfo.Text = resources.GetString("lblMRMInfo.Text");
            //
            // chkMRMWriteDataList
            //
            _chkMRMWriteDataList.Location = new System.Drawing.Point(23, 92);
            _chkMRMWriteDataList.Name = "chkMRMWriteDataList";
            _chkMRMWriteDataList.Size = new System.Drawing.Size(366, 21);
            _chkMRMWriteDataList.TabIndex = 1;
            _chkMRMWriteDataList.Text = "Save MRM data list (long, narrow file)";
            //
            // fraReporterIonMassMode
            //
            _fraReporterIonMassMode.Controls.Add(_cboReporterIonMassMode);
            _fraReporterIonMassMode.Location = new System.Drawing.Point(19, 181);
            _fraReporterIonMassMode.Name = "fraReporterIonMassMode";
            _fraReporterIonMassMode.Size = new System.Drawing.Size(612, 86);
            _fraReporterIonMassMode.TabIndex = 1;
            _fraReporterIonMassMode.TabStop = false;
            _fraReporterIonMassMode.Text = "Reporter Ion Mass Mode";
            //
            // cboReporterIonMassMode
            //
            _cboReporterIonMassMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboReporterIonMassMode.Location = new System.Drawing.Point(9, 27);
            _cboReporterIonMassMode.Name = "cboReporterIonMassMode";
            _cboReporterIonMassMode.Size = new System.Drawing.Size(592, 24);
            _cboReporterIonMassMode.TabIndex = 13;
            //
            // fraReporterIonOptions
            //
            _fraReporterIonOptions.Controls.Add(_chkReporterIonApplyAbundanceCorrection);
            _fraReporterIonOptions.Controls.Add(_chkReporterIonSaveUncorrectedIntensities);
            _fraReporterIonOptions.Controls.Add(_chkReporterIonSaveObservedMasses);
            _fraReporterIonOptions.Controls.Add(_txtReporterIonMZToleranceDa);
            _fraReporterIonOptions.Controls.Add(_lblReporterIonMZToleranceDa);
            _fraReporterIonOptions.Controls.Add(_chkReporterIonStatsEnabled);
            _fraReporterIonOptions.Location = new System.Drawing.Point(19, 18);
            _fraReporterIonOptions.Name = "fraReporterIonOptions";
            _fraReporterIonOptions.Size = new System.Drawing.Size(339, 156);
            _fraReporterIonOptions.TabIndex = 0;
            _fraReporterIonOptions.TabStop = false;
            _fraReporterIonOptions.Text = "Reporter Ion Options";
            //
            // chkReporterIonApplyAbundanceCorrection
            //
            _chkReporterIonApplyAbundanceCorrection.Location = new System.Drawing.Point(19, 103);
            _chkReporterIonApplyAbundanceCorrection.Name = "chkReporterIonApplyAbundanceCorrection";
            _chkReporterIonApplyAbundanceCorrection.Size = new System.Drawing.Size(301, 20);
            _chkReporterIonApplyAbundanceCorrection.TabIndex = 4;
            _chkReporterIonApplyAbundanceCorrection.Text = "Apply iTraq isotopic abundance correction";
            //
            // chkReporterIonSaveUncorrectedIntensities
            //
            _chkReporterIonSaveUncorrectedIntensities.Location = new System.Drawing.Point(38, 127);
            _chkReporterIonSaveUncorrectedIntensities.Name = "chkReporterIonSaveUncorrectedIntensities";
            _chkReporterIonSaveUncorrectedIntensities.Size = new System.Drawing.Size(269, 21);
            _chkReporterIonSaveUncorrectedIntensities.TabIndex = 5;
            _chkReporterIonSaveUncorrectedIntensities.Text = "Write original uncorrected intensities";
            //
            // chkReporterIonSaveObservedMasses
            //
            _chkReporterIonSaveObservedMasses.Location = new System.Drawing.Point(19, 78);
            _chkReporterIonSaveObservedMasses.Name = "chkReporterIonSaveObservedMasses";
            _chkReporterIonSaveObservedMasses.Size = new System.Drawing.Size(301, 21);
            _chkReporterIonSaveObservedMasses.TabIndex = 3;
            _chkReporterIonSaveObservedMasses.Text = "Write observed m/z values to Reporter Ions file";
            //
            // txtReporterIonMZToleranceDa
            //
            _txtReporterIonMZToleranceDa.Location = new System.Drawing.Point(205, 48);
            _txtReporterIonMZToleranceDa.Name = "txtReporterIonMZToleranceDa";
            _txtReporterIonMZToleranceDa.Size = new System.Drawing.Size(48, 22);
            _txtReporterIonMZToleranceDa.TabIndex = 2;
            _txtReporterIonMZToleranceDa.Text = "0.5";
            //
            // lblReporterIonMZToleranceDa
            //
            _lblReporterIonMZToleranceDa.Location = new System.Drawing.Point(16, 52);
            _lblReporterIonMZToleranceDa.Name = "lblReporterIonMZToleranceDa";
            _lblReporterIonMZToleranceDa.Size = new System.Drawing.Size(182, 18);
            _lblReporterIonMZToleranceDa.TabIndex = 1;
            _lblReporterIonMZToleranceDa.Text = "m/z Tolerance Half Width";
            //
            // chkReporterIonStatsEnabled
            //
            _chkReporterIonStatsEnabled.Location = new System.Drawing.Point(19, 28);
            _chkReporterIonStatsEnabled.Name = "chkReporterIonStatsEnabled";
            _chkReporterIonStatsEnabled.Size = new System.Drawing.Size(240, 20);
            _chkReporterIonStatsEnabled.TabIndex = 0;
            _chkReporterIonStatsEnabled.Text = "Generate Reporter Ion Stats";
            //
            // TabPageAdvancedOptions
            //
            _TabPageAdvancedOptions.Controls.Add(_fraAdditionalInfoFiles);
            _TabPageAdvancedOptions.Controls.Add(_fraDatasetLookupInfo);
            _TabPageAdvancedOptions.Controls.Add(_fraMemoryConservationOptions);
            _TabPageAdvancedOptions.Location = new System.Drawing.Point(4, 25);
            _TabPageAdvancedOptions.Name = "TabPageAdvancedOptions";
            _TabPageAdvancedOptions.Size = new System.Drawing.Size(882, 327);
            _TabPageAdvancedOptions.TabIndex = 8;
            _TabPageAdvancedOptions.Text = "Advanced";
            _TabPageAdvancedOptions.UseVisualStyleBackColor = true;
            //
            // fraAdditionalInfoFiles
            //
            _fraAdditionalInfoFiles.Controls.Add(_chkConsolidateConstantExtendedHeaderValues);
            _fraAdditionalInfoFiles.Controls.Add(_lblStatusLogKeyNameFilterList);
            _fraAdditionalInfoFiles.Controls.Add(_txtStatusLogKeyNameFilterList);
            _fraAdditionalInfoFiles.Controls.Add(_chkSaveExtendedStatsFileIncludeStatusLog);
            _fraAdditionalInfoFiles.Controls.Add(_chkSaveExtendedStatsFileIncludeFilterText);
            _fraAdditionalInfoFiles.Controls.Add(_chkSaveMSTuneFile);
            _fraAdditionalInfoFiles.Controls.Add(_chkSaveMSMethodFile);
            _fraAdditionalInfoFiles.Controls.Add(_chkSaveExtendedStatsFile);
            _fraAdditionalInfoFiles.Location = new System.Drawing.Point(382, 3);
            _fraAdditionalInfoFiles.Name = "fraAdditionalInfoFiles";
            _fraAdditionalInfoFiles.Size = new System.Drawing.Size(422, 135);
            _fraAdditionalInfoFiles.TabIndex = 1;
            _fraAdditionalInfoFiles.TabStop = false;
            _fraAdditionalInfoFiles.Text = "Thermo Info Files";
            //
            // chkConsolidateConstantExtendedHeaderValues
            //
            _chkConsolidateConstantExtendedHeaderValues.Checked = true;
            _chkConsolidateConstantExtendedHeaderValues.CheckState = CheckState.Checked;
            _chkConsolidateConstantExtendedHeaderValues.Location = new System.Drawing.Point(38, 112);
            _chkConsolidateConstantExtendedHeaderValues.Name = "chkConsolidateConstantExtendedHeaderValues";
            _chkConsolidateConstantExtendedHeaderValues.Size = new System.Drawing.Size(192, 21);
            _chkConsolidateConstantExtendedHeaderValues.TabIndex = 5;
            _chkConsolidateConstantExtendedHeaderValues.Text = "Consolidate constant values";
            //
            // lblStatusLogKeyNameFilterList
            //
            _lblStatusLogKeyNameFilterList.Location = new System.Drawing.Point(230, 31);
            _lblStatusLogKeyNameFilterList.Name = "lblStatusLogKeyNameFilterList";
            _lblStatusLogKeyNameFilterList.Size = new System.Drawing.Size(176, 20);
            _lblStatusLogKeyNameFilterList.TabIndex = 6;
            _lblStatusLogKeyNameFilterList.Text = "Status Log Keys to Include";
            //
            // txtStatusLogKeyNameFilterList
            //
            _txtStatusLogKeyNameFilterList.Location = new System.Drawing.Point(234, 54);
            _txtStatusLogKeyNameFilterList.Multiline = true;
            _txtStatusLogKeyNameFilterList.Name = "txtStatusLogKeyNameFilterList";
            _txtStatusLogKeyNameFilterList.ScrollBars = ScrollBars.Vertical;
            _txtStatusLogKeyNameFilterList.Size = new System.Drawing.Size(179, 58);
            _txtStatusLogKeyNameFilterList.TabIndex = 7;
            //
            // chkSaveExtendedStatsFileIncludeStatusLog
            //
            _chkSaveExtendedStatsFileIncludeStatusLog.Location = new System.Drawing.Point(38, 92);
            _chkSaveExtendedStatsFileIncludeStatusLog.Name = "chkSaveExtendedStatsFileIncludeStatusLog";
            _chkSaveExtendedStatsFileIncludeStatusLog.Size = new System.Drawing.Size(192, 21);
            _chkSaveExtendedStatsFileIncludeStatusLog.TabIndex = 4;
            _chkSaveExtendedStatsFileIncludeStatusLog.Text = "Include voltage, temp., etc.";
            //
            // chkSaveExtendedStatsFileIncludeFilterText
            //
            _chkSaveExtendedStatsFileIncludeFilterText.Checked = true;
            _chkSaveExtendedStatsFileIncludeFilterText.CheckState = CheckState.Checked;
            _chkSaveExtendedStatsFileIncludeFilterText.Location = new System.Drawing.Point(38, 74);
            _chkSaveExtendedStatsFileIncludeFilterText.Name = "chkSaveExtendedStatsFileIncludeFilterText";
            _chkSaveExtendedStatsFileIncludeFilterText.Size = new System.Drawing.Size(192, 18);
            _chkSaveExtendedStatsFileIncludeFilterText.TabIndex = 3;
            _chkSaveExtendedStatsFileIncludeFilterText.Text = "Include Scan Filter Text";
            //
            // chkSaveMSTuneFile
            //
            _chkSaveMSTuneFile.Location = new System.Drawing.Point(19, 37);
            _chkSaveMSTuneFile.Name = "chkSaveMSTuneFile";
            _chkSaveMSTuneFile.Size = new System.Drawing.Size(211, 18);
            _chkSaveMSTuneFile.TabIndex = 1;
            _chkSaveMSTuneFile.Text = "Save MS Tune File";
            //
            // chkSaveMSMethodFile
            //
            _chkSaveMSMethodFile.Checked = true;
            _chkSaveMSMethodFile.CheckState = CheckState.Checked;
            _chkSaveMSMethodFile.Location = new System.Drawing.Point(19, 18);
            _chkSaveMSMethodFile.Name = "chkSaveMSMethodFile";
            _chkSaveMSMethodFile.Size = new System.Drawing.Size(211, 19);
            _chkSaveMSMethodFile.TabIndex = 0;
            _chkSaveMSMethodFile.Text = "Save MS Method File";
            //
            // chkSaveExtendedStatsFile
            //
            _chkSaveExtendedStatsFile.Checked = true;
            _chkSaveExtendedStatsFile.CheckState = CheckState.Checked;
            _chkSaveExtendedStatsFile.Location = new System.Drawing.Point(19, 55);
            _chkSaveExtendedStatsFile.Name = "chkSaveExtendedStatsFile";
            _chkSaveExtendedStatsFile.Size = new System.Drawing.Size(211, 19);
            _chkSaveExtendedStatsFile.TabIndex = 2;
            _chkSaveExtendedStatsFile.Text = "Save Extended Stats File";
            //
            // fraDatasetLookupInfo
            //
            _fraDatasetLookupInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _fraDatasetLookupInfo.Controls.Add(_cmdSetConnectionStringToPNNLServer);
            _fraDatasetLookupInfo.Controls.Add(_txtDatasetInfoQuerySQL);
            _fraDatasetLookupInfo.Controls.Add(_lblDatasetInfoQuerySQL);
            _fraDatasetLookupInfo.Controls.Add(_txtDatabaseConnectionString);
            _fraDatasetLookupInfo.Controls.Add(_lblDatabaseConnectionString);
            _fraDatasetLookupInfo.Controls.Add(_lblDatasetLookupFilePath);
            _fraDatasetLookupInfo.Controls.Add(_cmdSelectDatasetLookupFile);
            _fraDatasetLookupInfo.Controls.Add(_txtDatasetLookupFilePath);
            _fraDatasetLookupInfo.Location = new System.Drawing.Point(19, 138);
            _fraDatasetLookupInfo.Name = "fraDatasetLookupInfo";
            _fraDatasetLookupInfo.Size = new System.Drawing.Size(852, 176);
            _fraDatasetLookupInfo.TabIndex = 1;
            _fraDatasetLookupInfo.TabStop = false;
            _fraDatasetLookupInfo.Text = "Dataset ID lookup information";
            //
            // cmdSetConnectionStringToPNNLServer
            //
            _cmdSetConnectionStringToPNNLServer.Location = new System.Drawing.Point(19, 23);
            _cmdSetConnectionStringToPNNLServer.Name = "cmdSetConnectionStringToPNNLServer";
            _cmdSetConnectionStringToPNNLServer.Size = new System.Drawing.Size(144, 23);
            _cmdSetConnectionStringToPNNLServer.TabIndex = 0;
            _cmdSetConnectionStringToPNNLServer.Text = "Set to PNNL Server";
            //
            // txtDatasetInfoQuerySQL
            //
            _txtDatasetInfoQuerySQL.Location = new System.Drawing.Point(202, 74);
            _txtDatasetInfoQuerySQL.Name = "txtDatasetInfoQuerySQL";
            _txtDatasetInfoQuerySQL.Size = new System.Drawing.Size(499, 22);
            _txtDatasetInfoQuerySQL.TabIndex = 4;
            //
            // lblDatasetInfoQuerySQL
            //
            _lblDatasetInfoQuerySQL.Location = new System.Drawing.Point(10, 80);
            _lblDatasetInfoQuerySQL.Name = "lblDatasetInfoQuerySQL";
            _lblDatasetInfoQuerySQL.Size = new System.Drawing.Size(192, 18);
            _lblDatasetInfoQuerySQL.TabIndex = 3;
            _lblDatasetInfoQuerySQL.Text = "Dataset Info Query SQL:";
            //
            // txtDatabaseConnectionString
            //
            _txtDatabaseConnectionString.Location = new System.Drawing.Point(202, 46);
            _txtDatabaseConnectionString.Name = "txtDatabaseConnectionString";
            _txtDatabaseConnectionString.Size = new System.Drawing.Size(499, 22);
            _txtDatabaseConnectionString.TabIndex = 2;
            //
            // lblDatabaseConnectionString
            //
            _lblDatabaseConnectionString.Location = new System.Drawing.Point(10, 52);
            _lblDatabaseConnectionString.Name = "lblDatabaseConnectionString";
            _lblDatabaseConnectionString.Size = new System.Drawing.Size(192, 18);
            _lblDatabaseConnectionString.TabIndex = 1;
            _lblDatabaseConnectionString.Text = "SQL Server Connection String:";
            //
            // lblDatasetLookupFilePath
            //
            _lblDatasetLookupFilePath.Location = new System.Drawing.Point(10, 111);
            _lblDatasetLookupFilePath.Name = "lblDatasetLookupFilePath";
            _lblDatasetLookupFilePath.Size = new System.Drawing.Size(633, 18);
            _lblDatasetLookupFilePath.TabIndex = 5;
            _lblDatasetLookupFilePath.Text = "Dataset lookup file (dataset name and dataset ID number, tab-separated); used if " + "DB not available";
            //
            // cmdSelectDatasetLookupFile
            //
            _cmdSelectDatasetLookupFile.Location = new System.Drawing.Point(10, 138);
            _cmdSelectDatasetLookupFile.Name = "cmdSelectDatasetLookupFile";
            _cmdSelectDatasetLookupFile.Size = new System.Drawing.Size(96, 28);
            _cmdSelectDatasetLookupFile.TabIndex = 6;
            _cmdSelectDatasetLookupFile.Text = "Select File";
            //
            // txtDatasetLookupFilePath
            //
            _txtDatasetLookupFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _txtDatasetLookupFilePath.Location = new System.Drawing.Point(125, 138);
            _txtDatasetLookupFilePath.Name = "txtDatasetLookupFilePath";
            _txtDatasetLookupFilePath.Size = new System.Drawing.Size(708, 22);
            _txtDatasetLookupFilePath.TabIndex = 7;
            //
            // fraMemoryConservationOptions
            //
            _fraMemoryConservationOptions.Controls.Add(_chkSkipMSMSProcessing);
            _fraMemoryConservationOptions.Controls.Add(_chkSkipSICAndRawDataProcessing);
            _fraMemoryConservationOptions.Controls.Add(_chkExportRawDataOnly);
            _fraMemoryConservationOptions.Location = new System.Drawing.Point(19, 18);
            _fraMemoryConservationOptions.Name = "fraMemoryConservationOptions";
            _fraMemoryConservationOptions.Size = new System.Drawing.Size(355, 120);
            _fraMemoryConservationOptions.TabIndex = 0;
            _fraMemoryConservationOptions.TabStop = false;
            _fraMemoryConservationOptions.Text = "Memory Usage and Speed Options";
            //
            // chkSkipMSMSProcessing
            //
            _chkSkipMSMSProcessing.Location = new System.Drawing.Point(19, 22);
            _chkSkipMSMSProcessing.Name = "chkSkipMSMSProcessing";
            _chkSkipMSMSProcessing.Size = new System.Drawing.Size(317, 21);
            _chkSkipMSMSProcessing.TabIndex = 0;
            _chkSkipMSMSProcessing.Text = "Skip MS/MS Processing (no similarity testing)";
            //
            // chkSkipSICAndRawDataProcessing
            //
            _chkSkipSICAndRawDataProcessing.Location = new System.Drawing.Point(19, 46);
            _chkSkipSICAndRawDataProcessing.Name = "chkSkipSICAndRawDataProcessing";
            _chkSkipSICAndRawDataProcessing.Size = new System.Drawing.Size(261, 39);
            _chkSkipSICAndRawDataProcessing.TabIndex = 1;
            _chkSkipSICAndRawDataProcessing.Text = "Only Export Chromatograms and Scan Stats (no SICs or raw data)";
            //
            // chkExportRawDataOnly
            //
            _chkExportRawDataOnly.Location = new System.Drawing.Point(19, 91);
            _chkExportRawDataOnly.Name = "chkExportRawDataOnly";
            _chkExportRawDataOnly.Size = new System.Drawing.Size(240, 21);
            _chkExportRawDataOnly.TabIndex = 2;
            _chkExportRawDataOnly.Text = "Export Raw Data Only (No SICs)";
            //
            // TabPageLog
            //
            _TabPageLog.Controls.Add(_txtLogMessages);
            _TabPageLog.Location = new System.Drawing.Point(4, 25);
            _TabPageLog.Name = "TabPageLog";
            _TabPageLog.Padding = new Padding(3);
            _TabPageLog.Size = new System.Drawing.Size(882, 327);
            _TabPageLog.TabIndex = 10;
            _TabPageLog.Text = "Log";
            _TabPageLog.UseVisualStyleBackColor = true;
            //
            // txtLogMessages
            //
            _txtLogMessages.Location = new System.Drawing.Point(6, 6);
            _txtLogMessages.Multiline = true;
            _txtLogMessages.Name = "txtLogMessages";
            _txtLogMessages.ReadOnly = true;
            _txtLogMessages.ScrollBars = ScrollBars.Vertical;
            _txtLogMessages.Size = new System.Drawing.Size(870, 315);
            _txtLogMessages.TabIndex = 1;
            _txtLogMessages.Text = "No log messages.";
            //
            // fraOutputDirectoryPath
            //
            _fraOutputDirectoryPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _fraOutputDirectoryPath.Controls.Add(_cmdStartProcessing);
            _fraOutputDirectoryPath.Controls.Add(_cmdSelectOutputDirectory);
            _fraOutputDirectoryPath.Controls.Add(_txtOutputDirectoryPath);
            _fraOutputDirectoryPath.Location = new System.Drawing.Point(10, 92);
            _fraOutputDirectoryPath.Name = "fraOutputDirectoryPath";
            _fraOutputDirectoryPath.Size = new System.Drawing.Size(885, 102);
            _fraOutputDirectoryPath.TabIndex = 1;
            _fraOutputDirectoryPath.TabStop = false;
            _fraOutputDirectoryPath.Text = "Output Folder Path";
            //
            // cmdStartProcessing
            //
            _cmdStartProcessing.Location = new System.Drawing.Point(326, 65);
            _cmdStartProcessing.Name = "cmdStartProcessing";
            _cmdStartProcessing.Size = new System.Drawing.Size(133, 27);
            _cmdStartProcessing.TabIndex = 2;
            _cmdStartProcessing.Text = "Start &Processing";
            //
            // cmdSelectOutputDirectory
            //
            _cmdSelectOutputDirectory.Location = new System.Drawing.Point(10, 28);
            _cmdSelectOutputDirectory.Name = "cmdSelectOutputDirectory";
            _cmdSelectOutputDirectory.Size = new System.Drawing.Size(96, 44);
            _cmdSelectOutputDirectory.TabIndex = 0;
            _cmdSelectOutputDirectory.Text = "Select &Directory";
            //
            // txtOutputDirectoryPath
            //
            _txtOutputDirectoryPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _txtOutputDirectoryPath.Location = new System.Drawing.Point(125, 30);
            _txtOutputDirectoryPath.Name = "txtOutputDirectoryPath";
            _txtOutputDirectoryPath.Size = new System.Drawing.Size(741, 22);
            _txtOutputDirectoryPath.TabIndex = 1;
            //
            // frmMain
            //
            AutoScaleBaseSize = new System.Drawing.Size(6, 15);
            ClientSize = new System.Drawing.Size(904, 585);
            Controls.Add(_fraOutputDirectoryPath);
            Controls.Add(_tbsOptions);
            Controls.Add(_fraInputFilePath);
            Menu = _MainMenuControl;
            MinimumSize = new System.Drawing.Size(540, 0);
            Name = "frmMain";
            Text = "MASIC";
            _fraInputFilePath.ResumeLayout(false);
            _fraInputFilePath.PerformLayout();
            _tbsOptions.ResumeLayout(false);
            _TabPageMasicExportOptions.ResumeLayout(false);
            _TabPageMasicExportOptions.PerformLayout();
            _fraExportAllSpectraDataPoints.ResumeLayout(false);
            _fraExportAllSpectraDataPoints.PerformLayout();
            _TabPageSICOptions.ResumeLayout(false);
            _fraInputFileRangeFilters.ResumeLayout(false);
            _fraInputFileRangeFilters.PerformLayout();
            _fraSICSearchThresholds.ResumeLayout(false);
            _fraSICSearchThresholds.PerformLayout();
            _TabPagePeakFindingOptions.ResumeLayout(false);
            _fraSICNoiseThresholds.ResumeLayout(false);
            _fraSICNoiseThresholds.PerformLayout();
            _fraSmoothingOptions.ResumeLayout(false);
            _fraSmoothingOptions.PerformLayout();
            _fraPeakFindingOptions.ResumeLayout(false);
            _fraPeakFindingOptions.PerformLayout();
            _TabPageBinningAndSimilarityOptions.ResumeLayout(false);
            _fraMassSpectraNoiseThresholds.ResumeLayout(false);
            _fraMassSpectraNoiseThresholds.PerformLayout();
            _fraBinningIntensityOptions.ResumeLayout(false);
            _fraBinningIntensityOptions.PerformLayout();
            _fraSpectrumSimilarityOptions.ResumeLayout(false);
            _fraSpectrumSimilarityOptions.PerformLayout();
            _fraBinningMZOptions.ResumeLayout(false);
            _fraBinningMZOptions.PerformLayout();
            _TabPageCustomSICOptions.ResumeLayout(false);
            _TabPageCustomSICOptions.PerformLayout();
            _fraCustomSICControls.ResumeLayout(false);
            _fraCustomSICControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_dgCustomSICValues).EndInit();
            _TabPageReporterIons.ResumeLayout(false);
            _fraDecoyOptions.ResumeLayout(false);
            _fraDecoyOptions.PerformLayout();
            _fraMRMOptions.ResumeLayout(false);
            _fraReporterIonMassMode.ResumeLayout(false);
            _fraReporterIonOptions.ResumeLayout(false);
            _fraReporterIonOptions.PerformLayout();
            _TabPageAdvancedOptions.ResumeLayout(false);
            _fraAdditionalInfoFiles.ResumeLayout(false);
            _fraAdditionalInfoFiles.PerformLayout();
            _fraDatasetLookupInfo.ResumeLayout(false);
            _fraDatasetLookupInfo.PerformLayout();
            _fraMemoryConservationOptions.ResumeLayout(false);
            _TabPageLog.ResumeLayout(false);
            _TabPageLog.PerformLayout();
            _fraOutputDirectoryPath.ResumeLayout(false);
            _fraOutputDirectoryPath.PerformLayout();
            Resize += new EventHandler(frmMain_Resize);
            Load += new EventHandler(frmMain_Load);
            Closing += new System.ComponentModel.CancelEventHandler(frmMain_Closing);
            ResumeLayout(false);
        }

        private TextBox _txtInputFilePath;

        internal TextBox txtInputFilePath
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtInputFilePath;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtInputFilePath != null)
                {
                }

                _txtInputFilePath = value;
                if (_txtInputFilePath != null)
                {
                }
            }
        }

        private Button _cmdSelectFile;

        internal Button cmdSelectFile
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cmdSelectFile;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cmdSelectFile != null)
                {
                    _cmdSelectFile.Click -= cmdSelectFile_Click;
                }

                _cmdSelectFile = value;
                if (_cmdSelectFile != null)
                {
                    _cmdSelectFile.Click += cmdSelectFile_Click;
                }
            }
        }

        private GroupBox _fraInputFilePath;

        internal GroupBox fraInputFilePath
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraInputFilePath;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraInputFilePath != null)
                {
                }

                _fraInputFilePath = value;
                if (_fraInputFilePath != null)
                {
                }
            }
        }

        private MenuItem _mnuFile;

        internal MenuItem mnuFile
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuFile;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuFile != null)
                {
                }

                _mnuFile = value;
                if (_mnuFile != null)
                {
                }
            }
        }

        private MenuItem _mnuEdit;

        internal MenuItem mnuEdit
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuEdit;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuEdit != null)
                {
                }

                _mnuEdit = value;
                if (_mnuEdit != null)
                {
                }
            }
        }

        private MenuItem _mnuEditResetOptions;

        internal MenuItem mnuEditResetOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuEditResetOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuEditResetOptions != null)
                {
                    _mnuEditResetOptions.Click -= mnuEditResetOptions_Click;
                }

                _mnuEditResetOptions = value;
                if (_mnuEditResetOptions != null)
                {
                    _mnuEditResetOptions.Click += mnuEditResetOptions_Click;
                }
            }
        }

        private MenuItem _mnuHelp;

        internal MenuItem mnuHelp
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuHelp;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuHelp != null)
                {
                }

                _mnuHelp = value;
                if (_mnuHelp != null)
                {
                }
            }
        }

        private MenuItem _mnuHelpAbout;

        internal MenuItem mnuHelpAbout
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuHelpAbout;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuHelpAbout != null)
                {
                    _mnuHelpAbout.Click -= mnuHelpAbout_Click;
                }

                _mnuHelpAbout = value;
                if (_mnuHelpAbout != null)
                {
                    _mnuHelpAbout.Click += mnuHelpAbout_Click;
                }
            }
        }

        private MenuItem _mnuEditSep1;

        internal MenuItem mnuEditSep1
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuEditSep1;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuEditSep1 != null)
                {
                }

                _mnuEditSep1 = value;
                if (_mnuEditSep1 != null)
                {
                }
            }
        }

        private MainMenu _MainMenuControl;

        internal MainMenu MainMenuControl
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _MainMenuControl;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_MainMenuControl != null)
                {
                }

                _MainMenuControl = value;
                if (_MainMenuControl != null)
                {
                }
            }
        }

        private TabControl _tbsOptions;

        internal TabControl tbsOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _tbsOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_tbsOptions != null)
                {
                }

                _tbsOptions = value;
                if (_tbsOptions != null)
                {
                }
            }
        }

        private DataGrid _dgCustomSICValues;

        internal DataGrid dgCustomSICValues
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _dgCustomSICValues;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_dgCustomSICValues != null)
                {
                }

                _dgCustomSICValues = value;
                if (_dgCustomSICValues != null)
                {
                }
            }
        }

        private Button _cmdClearCustomSICList;

        internal Button cmdClearCustomSICList
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cmdClearCustomSICList;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cmdClearCustomSICList != null)
                {
                    _cmdClearCustomSICList.Click -= cmdClearCustomSICList_Click;
                }

                _cmdClearCustomSICList = value;
                if (_cmdClearCustomSICList != null)
                {
                    _cmdClearCustomSICList.Click += cmdClearCustomSICList_Click;
                }
            }
        }

        private Button _cmdCustomSICValuesPopulate;

        internal Button cmdCustomSICValuesPopulate
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cmdCustomSICValuesPopulate;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cmdCustomSICValuesPopulate != null)
                {
                    _cmdCustomSICValuesPopulate.Click -= cmdCustomSICValuesPopulate_Click;
                }

                _cmdCustomSICValuesPopulate = value;
                if (_cmdCustomSICValuesPopulate != null)
                {
                    _cmdCustomSICValuesPopulate.Click += cmdCustomSICValuesPopulate_Click;
                }
            }
        }

        private Button _cmdPasteCustomSICList;

        internal Button cmdPasteCustomSICList
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cmdPasteCustomSICList;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cmdPasteCustomSICList != null)
                {
                    _cmdPasteCustomSICList.Click -= cmdPasteCustomSICList_Click;
                }

                _cmdPasteCustomSICList = value;
                if (_cmdPasteCustomSICList != null)
                {
                    _cmdPasteCustomSICList.Click += cmdPasteCustomSICList_Click;
                }
            }
        }

        private TabPage _TabPageCustomSICOptions;

        internal TabPage TabPageCustomSICOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _TabPageCustomSICOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_TabPageCustomSICOptions != null)
                {
                }

                _TabPageCustomSICOptions = value;
                if (_TabPageCustomSICOptions != null)
                {
                }
            }
        }

        private GroupBox _fraCustomSICControls;

        internal GroupBox fraCustomSICControls
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraCustomSICControls;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraCustomSICControls != null)
                {
                }

                _fraCustomSICControls = value;
                if (_fraCustomSICControls != null)
                {
                }
            }
        }

        private MenuItem _mnuFileSelectOutputDirectory;

        internal MenuItem mnuFileSelectOutputDirectory
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuFileSelectOutputDirectory;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuFileSelectOutputDirectory != null)
                {
                    _mnuFileSelectOutputDirectory.Click -= mnuFileSelectOutputDirectory_Click;
                }

                _mnuFileSelectOutputDirectory = value;
                if (_mnuFileSelectOutputDirectory != null)
                {
                    _mnuFileSelectOutputDirectory.Click += mnuFileSelectOutputDirectory_Click;
                }
            }
        }

        private MenuItem _mnuFileLoadOptions;

        internal MenuItem mnuFileLoadOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuFileLoadOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuFileLoadOptions != null)
                {
                    _mnuFileLoadOptions.Click -= mnuFileLoadOptions_Click;
                }

                _mnuFileLoadOptions = value;
                if (_mnuFileLoadOptions != null)
                {
                    _mnuFileLoadOptions.Click += mnuFileLoadOptions_Click;
                }
            }
        }

        private MenuItem _mnuFileSaveOptions;

        internal MenuItem mnuFileSaveOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuFileSaveOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuFileSaveOptions != null)
                {
                    _mnuFileSaveOptions.Click -= mnuFileSaveOptions_Click;
                }

                _mnuFileSaveOptions = value;
                if (_mnuFileSaveOptions != null)
                {
                    _mnuFileSaveOptions.Click += mnuFileSaveOptions_Click;
                }
            }
        }

        private MenuItem _mnuFileExit;

        internal MenuItem mnuFileExit
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuFileExit;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuFileExit != null)
                {
                    _mnuFileExit.Click -= mnuFileExit_Click;
                }

                _mnuFileExit = value;
                if (_mnuFileExit != null)
                {
                    _mnuFileExit.Click += mnuFileExit_Click;
                }
            }
        }

        private MenuItem _mnuFileSelectInputFile;

        internal MenuItem mnuFileSelectInputFile
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuFileSelectInputFile;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuFileSelectInputFile != null)
                {
                    _mnuFileSelectInputFile.Click -= mnuFileSelectInputFile_Click;
                }

                _mnuFileSelectInputFile = value;
                if (_mnuFileSelectInputFile != null)
                {
                    _mnuFileSelectInputFile.Click += mnuFileSelectInputFile_Click;
                }
            }
        }

        private MenuItem _mnuFileSep1;

        internal MenuItem mnuFileSep1
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuFileSep1;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuFileSep1 != null)
                {
                }

                _mnuFileSep1 = value;
                if (_mnuFileSep1 != null)
                {
                }
            }
        }

        private MenuItem _mnuFileSep2;

        internal MenuItem mnuFileSep2
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuFileSep2;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuFileSep2 != null)
                {
                }

                _mnuFileSep2 = value;
                if (_mnuFileSep2 != null)
                {
                }
            }
        }

        private GroupBox _fraOutputDirectoryPath;

        internal GroupBox fraOutputDirectoryPath
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraOutputDirectoryPath;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraOutputDirectoryPath != null)
                {
                }

                _fraOutputDirectoryPath = value;
                if (_fraOutputDirectoryPath != null)
                {
                }
            }
        }

        private Button _cmdSelectOutputDirectory;

        internal Button cmdSelectOutputDirectory
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cmdSelectOutputDirectory;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cmdSelectOutputDirectory != null)
                {
                    _cmdSelectOutputDirectory.Click -= cmdSelectOutputDirectory_Click;
                }

                _cmdSelectOutputDirectory = value;
                if (_cmdSelectOutputDirectory != null)
                {
                    _cmdSelectOutputDirectory.Click += cmdSelectOutputDirectory_Click;
                }
            }
        }

        private TextBox _txtOutputDirectoryPath;

        internal TextBox txtOutputDirectoryPath
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtOutputDirectoryPath;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtOutputDirectoryPath != null)
                {
                }

                _txtOutputDirectoryPath = value;
                if (_txtOutputDirectoryPath != null)
                {
                }
            }
        }

        private MenuItem _mnuEditProcessFile;

        internal MenuItem mnuEditProcessFile
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuEditProcessFile;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuEditProcessFile != null)
                {
                    _mnuEditProcessFile.Click -= mnuEditProcessFile_Click;
                }

                _mnuEditProcessFile = value;
                if (_mnuEditProcessFile != null)
                {
                    _mnuEditProcessFile.Click += mnuEditProcessFile_Click;
                }
            }
        }

        private TabPage _TabPageMasicExportOptions;

        internal TabPage TabPageMasicExportOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _TabPageMasicExportOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_TabPageMasicExportOptions != null)
                {
                }

                _TabPageMasicExportOptions = value;
                if (_TabPageMasicExportOptions != null)
                {
                }
            }
        }

        private TabPage _TabPageSICOptions;

        internal TabPage TabPageSICOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _TabPageSICOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_TabPageSICOptions != null)
                {
                }

                _TabPageSICOptions = value;
                if (_TabPageSICOptions != null)
                {
                }
            }
        }

        private CheckBox _chkIncludeHeaders;

        internal CheckBox chkIncludeHeaders
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkIncludeHeaders;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkIncludeHeaders != null)
                {
                }

                _chkIncludeHeaders = value;
                if (_chkIncludeHeaders != null)
                {
                }
            }
        }

        private GroupBox _fraExportAllSpectraDataPoints;

        internal GroupBox fraExportAllSpectraDataPoints
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraExportAllSpectraDataPoints;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraExportAllSpectraDataPoints != null)
                {
                }

                _fraExportAllSpectraDataPoints = value;
                if (_fraExportAllSpectraDataPoints != null)
                {
                }
            }
        }

        private ComboBox _cboExportRawDataFileFormat;

        internal ComboBox cboExportRawDataFileFormat
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cboExportRawDataFileFormat;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cboExportRawDataFileFormat != null)
                {
                }

                _cboExportRawDataFileFormat = value;
                if (_cboExportRawDataFileFormat != null)
                {
                }
            }
        }

        private Label _lblExportDataPointsFormat;

        internal Label lblExportDataPointsFormat
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblExportDataPointsFormat;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblExportDataPointsFormat != null)
                {
                }

                _lblExportDataPointsFormat = value;
                if (_lblExportDataPointsFormat != null)
                {
                }
            }
        }

        private CheckBox _chkExportRawDataIncludeMSMS;

        internal CheckBox chkExportRawDataIncludeMSMS
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkExportRawDataIncludeMSMS;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkExportRawDataIncludeMSMS != null)
                {
                    _chkExportRawDataIncludeMSMS.CheckedChanged -= chkExportRawDataIncludeMSMS_CheckedChanged;
                }

                _chkExportRawDataIncludeMSMS = value;
                if (_chkExportRawDataIncludeMSMS != null)
                {
                    _chkExportRawDataIncludeMSMS.CheckedChanged += chkExportRawDataIncludeMSMS_CheckedChanged;
                }
            }
        }

        private TextBox _txtExportRawDataMaxIonCountPerScan;

        internal TextBox txtExportRawDataMaxIonCountPerScan
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtExportRawDataMaxIonCountPerScan;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtExportRawDataMaxIonCountPerScan != null)
                {
                    _txtExportRawDataMaxIonCountPerScan.KeyPress -= txtExportRawDataMaxIonCountPerScan_KeyPress;
                }

                _txtExportRawDataMaxIonCountPerScan = value;
                if (_txtExportRawDataMaxIonCountPerScan != null)
                {
                    _txtExportRawDataMaxIonCountPerScan.KeyPress += txtExportRawDataMaxIonCountPerScan_KeyPress;
                }
            }
        }

        private CheckBox _chkExportRawDataRenumberScans;

        internal CheckBox chkExportRawDataRenumberScans
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkExportRawDataRenumberScans;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkExportRawDataRenumberScans != null)
                {
                }

                _chkExportRawDataRenumberScans = value;
                if (_chkExportRawDataRenumberScans != null)
                {
                }
            }
        }

        private TextBox _txtExportRawDataIntensityMinimum;

        internal TextBox txtExportRawDataIntensityMinimum
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtExportRawDataIntensityMinimum;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtExportRawDataIntensityMinimum != null)
                {
                    _txtExportRawDataIntensityMinimum.KeyPress -= txtExportRawDataIntensityMinimum_KeyPress;
                }

                _txtExportRawDataIntensityMinimum = value;
                if (_txtExportRawDataIntensityMinimum != null)
                {
                    _txtExportRawDataIntensityMinimum.KeyPress += txtExportRawDataIntensityMinimum_KeyPress;
                }
            }
        }

        private CheckBox _chkExportRawSpectraData;

        internal CheckBox chkExportRawSpectraData
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkExportRawSpectraData;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkExportRawSpectraData != null)
                {
                    _chkExportRawSpectraData.CheckedChanged -= chkExportRawSpectraData_CheckedChanged;
                }

                _chkExportRawSpectraData = value;
                if (_chkExportRawSpectraData != null)
                {
                    _chkExportRawSpectraData.CheckedChanged += chkExportRawSpectraData_CheckedChanged;
                }
            }
        }

        private GroupBox _fraSICSearchThresholds;

        internal GroupBox fraSICSearchThresholds
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraSICSearchThresholds;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraSICSearchThresholds != null)
                {
                }

                _fraSICSearchThresholds = value;
                if (_fraSICSearchThresholds != null)
                {
                }
            }
        }

        private TextBox _txtSICTolerance;

        internal TextBox txtSICTolerance
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtSICTolerance;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtSICTolerance != null)
                {
                    _txtSICTolerance.KeyPress -= txtSICTolerance_KeyPress;
                }

                _txtSICTolerance = value;
                if (_txtSICTolerance != null)
                {
                    _txtSICTolerance.KeyPress += txtSICTolerance_KeyPress;
                }
            }
        }

        private Label _lblSICToleranceDa;

        internal Label lblSICToleranceDa
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblSICToleranceDa;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblSICToleranceDa != null)
                {
                }

                _lblSICToleranceDa = value;
                if (_lblSICToleranceDa != null)
                {
                }
            }
        }

        private TextBox _txtMaxPeakWidthMinutesBackward;

        internal TextBox txtMaxPeakWidthMinutesBackward
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtMaxPeakWidthMinutesBackward;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtMaxPeakWidthMinutesBackward != null)
                {
                    _txtMaxPeakWidthMinutesBackward.KeyPress -= txtMaxPeakWidthMinutesBackward_KeyPress;
                }

                _txtMaxPeakWidthMinutesBackward = value;
                if (_txtMaxPeakWidthMinutesBackward != null)
                {
                    _txtMaxPeakWidthMinutesBackward.KeyPress += txtMaxPeakWidthMinutesBackward_KeyPress;
                }
            }
        }

        private Label _lblMaxPeakWidthMinutes;

        internal Label lblMaxPeakWidthMinutes
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblMaxPeakWidthMinutes;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblMaxPeakWidthMinutes != null)
                {
                }

                _lblMaxPeakWidthMinutes = value;
                if (_lblMaxPeakWidthMinutes != null)
                {
                }
            }
        }

        private Label _lblMaxPeakWidthMinutesBackward;

        internal Label lblMaxPeakWidthMinutesBackward
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblMaxPeakWidthMinutesBackward;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblMaxPeakWidthMinutesBackward != null)
                {
                }

                _lblMaxPeakWidthMinutesBackward = value;
                if (_lblMaxPeakWidthMinutesBackward != null)
                {
                }
            }
        }

        private Label _lblMaxPeakWidthMinutesForward;

        internal Label lblMaxPeakWidthMinutesForward
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblMaxPeakWidthMinutesForward;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblMaxPeakWidthMinutesForward != null)
                {
                }

                _lblMaxPeakWidthMinutesForward = value;
                if (_lblMaxPeakWidthMinutesForward != null)
                {
                }
            }
        }

        private TextBox _txtMaxPeakWidthMinutesForward;

        internal TextBox txtMaxPeakWidthMinutesForward
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtMaxPeakWidthMinutesForward;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtMaxPeakWidthMinutesForward != null)
                {
                    _txtMaxPeakWidthMinutesForward.KeyPress -= txtMaxPeakWidthMinutesForward_KeyPress;
                }

                _txtMaxPeakWidthMinutesForward = value;
                if (_txtMaxPeakWidthMinutesForward != null)
                {
                    _txtMaxPeakWidthMinutesForward.KeyPress += txtMaxPeakWidthMinutesForward_KeyPress;
                }
            }
        }

        private TextBox _txtIntensityThresholdAbsoluteMinimum;

        internal TextBox txtIntensityThresholdAbsoluteMinimum
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtIntensityThresholdAbsoluteMinimum;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtIntensityThresholdAbsoluteMinimum != null)
                {
                    _txtIntensityThresholdAbsoluteMinimum.KeyPress -= txtIntensityThresholdAbsoluteMinimum_KeyPress;
                }

                _txtIntensityThresholdAbsoluteMinimum = value;
                if (_txtIntensityThresholdAbsoluteMinimum != null)
                {
                    _txtIntensityThresholdAbsoluteMinimum.KeyPress += txtIntensityThresholdAbsoluteMinimum_KeyPress;
                }
            }
        }

        private Label _lblIntensityThresholdAbsoluteMinimum;

        internal Label lblIntensityThresholdAbsoluteMinimum
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblIntensityThresholdAbsoluteMinimum;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblIntensityThresholdAbsoluteMinimum != null)
                {
                }

                _lblIntensityThresholdAbsoluteMinimum = value;
                if (_lblIntensityThresholdAbsoluteMinimum != null)
                {
                }
            }
        }

        private TextBox _txtIntensityThresholdFractionMax;

        internal TextBox txtIntensityThresholdFractionMax
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtIntensityThresholdFractionMax;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtIntensityThresholdFractionMax != null)
                {
                    _txtIntensityThresholdFractionMax.KeyPress -= txtIntensityThresholdFractionMax_KeyPress;
                }

                _txtIntensityThresholdFractionMax = value;
                if (_txtIntensityThresholdFractionMax != null)
                {
                    _txtIntensityThresholdFractionMax.KeyPress += txtIntensityThresholdFractionMax_KeyPress;
                }
            }
        }

        private Label _lblIntensityThresholdFractionMax;

        internal Label lblIntensityThresholdFractionMax
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblIntensityThresholdFractionMax;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblIntensityThresholdFractionMax != null)
                {
                }

                _lblIntensityThresholdFractionMax = value;
                if (_lblIntensityThresholdFractionMax != null)
                {
                }
            }
        }

        private TabPage _TabPagePeakFindingOptions;

        internal TabPage TabPagePeakFindingOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _TabPagePeakFindingOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_TabPagePeakFindingOptions != null)
                {
                }

                _TabPagePeakFindingOptions = value;
                if (_TabPagePeakFindingOptions != null)
                {
                }
            }
        }

        private GroupBox _fraPeakFindingOptions;

        internal GroupBox fraPeakFindingOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraPeakFindingOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraPeakFindingOptions != null)
                {
                }

                _fraPeakFindingOptions = value;
                if (_fraPeakFindingOptions != null)
                {
                }
            }
        }

        private Label _lblSavitzkyGolayFilterOrder;

        internal Label lblSavitzkyGolayFilterOrder
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblSavitzkyGolayFilterOrder;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblSavitzkyGolayFilterOrder != null)
                {
                }

                _lblSavitzkyGolayFilterOrder = value;
                if (_lblSavitzkyGolayFilterOrder != null)
                {
                }
            }
        }

        private TextBox _txtSavitzkyGolayFilterOrder;

        internal TextBox txtSavitzkyGolayFilterOrder
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtSavitzkyGolayFilterOrder;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtSavitzkyGolayFilterOrder != null)
                {
                    _txtSavitzkyGolayFilterOrder.KeyPress -= txtSavitzkyGolayFilterOrder_KeyPress;
                    _txtSavitzkyGolayFilterOrder.Validating -= txtSavitzkyGolayFilterOrder_Validating;
                }

                _txtSavitzkyGolayFilterOrder = value;
                if (_txtSavitzkyGolayFilterOrder != null)
                {
                    _txtSavitzkyGolayFilterOrder.KeyPress += txtSavitzkyGolayFilterOrder_KeyPress;
                    _txtSavitzkyGolayFilterOrder.Validating += txtSavitzkyGolayFilterOrder_Validating;
                }
            }
        }

        private TextBox _txtMaxDistanceScansNoOverlap;

        internal TextBox txtMaxDistanceScansNoOverlap
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtMaxDistanceScansNoOverlap;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtMaxDistanceScansNoOverlap != null)
                {
                    _txtMaxDistanceScansNoOverlap.KeyPress -= txtMaxDistanceScansNoOverlap_KeyPress;
                }

                _txtMaxDistanceScansNoOverlap = value;
                if (_txtMaxDistanceScansNoOverlap != null)
                {
                    _txtMaxDistanceScansNoOverlap.KeyPress += txtMaxDistanceScansNoOverlap_KeyPress;
                }
            }
        }

        private TextBox _txtMaxAllowedUpwardSpikeFractionMax;

        internal TextBox txtMaxAllowedUpwardSpikeFractionMax
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtMaxAllowedUpwardSpikeFractionMax;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtMaxAllowedUpwardSpikeFractionMax != null)
                {
                    _txtMaxAllowedUpwardSpikeFractionMax.KeyPress -= txtMaxAllowedUpwardSpikeFractionMax_KeyPress;
                }

                _txtMaxAllowedUpwardSpikeFractionMax = value;
                if (_txtMaxAllowedUpwardSpikeFractionMax != null)
                {
                    _txtMaxAllowedUpwardSpikeFractionMax.KeyPress += txtMaxAllowedUpwardSpikeFractionMax_KeyPress;
                }
            }
        }

        private Label _lblMaxAllowedUpwardSpikeFractionMax;

        internal Label lblMaxAllowedUpwardSpikeFractionMax
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblMaxAllowedUpwardSpikeFractionMax;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblMaxAllowedUpwardSpikeFractionMax != null)
                {
                }

                _lblMaxAllowedUpwardSpikeFractionMax = value;
                if (_lblMaxAllowedUpwardSpikeFractionMax != null)
                {
                }
            }
        }

        private Label _lblMaxDistanceScansNoOverlap;

        internal Label lblMaxDistanceScansNoOverlap
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblMaxDistanceScansNoOverlap;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblMaxDistanceScansNoOverlap != null)
                {
                }

                _lblMaxDistanceScansNoOverlap = value;
                if (_lblMaxDistanceScansNoOverlap != null)
                {
                }
            }
        }

        private TextBox _txtInitialPeakWidthScansMaximum;

        internal TextBox txtInitialPeakWidthScansMaximum
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtInitialPeakWidthScansMaximum;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtInitialPeakWidthScansMaximum != null)
                {
                    _txtInitialPeakWidthScansMaximum.KeyPress -= txtInitialPeakWidthScansMaximum_KeyPress;
                }

                _txtInitialPeakWidthScansMaximum = value;
                if (_txtInitialPeakWidthScansMaximum != null)
                {
                    _txtInitialPeakWidthScansMaximum.KeyPress += txtInitialPeakWidthScansMaximum_KeyPress;
                }
            }
        }

        private Label _lblInitialPeakWidthScansMaximum;

        internal Label lblInitialPeakWidthScansMaximum
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblInitialPeakWidthScansMaximum;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblInitialPeakWidthScansMaximum != null)
                {
                }

                _lblInitialPeakWidthScansMaximum = value;
                if (_lblInitialPeakWidthScansMaximum != null)
                {
                }
            }
        }

        private GroupBox _fraSmoothingOptions;

        internal GroupBox fraSmoothingOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraSmoothingOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraSmoothingOptions != null)
                {
                }

                _fraSmoothingOptions = value;
                if (_fraSmoothingOptions != null)
                {
                }
            }
        }

        private TabPage _TabPageBinningAndSimilarityOptions;

        internal TabPage TabPageBinningAndSimilarityOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _TabPageBinningAndSimilarityOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_TabPageBinningAndSimilarityOptions != null)
                {
                }

                _TabPageBinningAndSimilarityOptions = value;
                if (_TabPageBinningAndSimilarityOptions != null)
                {
                }
            }
        }

        private GroupBox _fraBinningMZOptions;

        internal GroupBox fraBinningMZOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraBinningMZOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraBinningMZOptions != null)
                {
                }

                _fraBinningMZOptions = value;
                if (_fraBinningMZOptions != null)
                {
                }
            }
        }

        private TextBox _txtBinSize;

        internal TextBox txtBinSize
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtBinSize;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtBinSize != null)
                {
                    _txtBinSize.KeyPress -= txtBinSize_KeyPress;
                }

                _txtBinSize = value;
                if (_txtBinSize != null)
                {
                    _txtBinSize.KeyPress += txtBinSize_KeyPress;
                }
            }
        }

        private Label _lblBinSize;

        internal Label lblBinSize
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblBinSize;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblBinSize != null)
                {
                }

                _lblBinSize = value;
                if (_lblBinSize != null)
                {
                }
            }
        }

        private TextBox _txtBinEndX;

        internal TextBox txtBinEndX
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtBinEndX;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtBinEndX != null)
                {
                    _txtBinEndX.KeyPress -= txtBinEndX_KeyPress;
                }

                _txtBinEndX = value;
                if (_txtBinEndX != null)
                {
                    _txtBinEndX.KeyPress += txtBinEndX_KeyPress;
                }
            }
        }

        private Label _lblBinEndX;

        internal Label lblBinEndX
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblBinEndX;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblBinEndX != null)
                {
                }

                _lblBinEndX = value;
                if (_lblBinEndX != null)
                {
                }
            }
        }

        private TextBox _txtBinStartX;

        internal TextBox txtBinStartX
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtBinStartX;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtBinStartX != null)
                {
                    _txtBinStartX.KeyPress -= txtBinStartX_KeyPress;
                }

                _txtBinStartX = value;
                if (_txtBinStartX != null)
                {
                    _txtBinStartX.KeyPress += txtBinStartX_KeyPress;
                }
            }
        }

        private Label _lblBinStartX;

        internal Label lblBinStartX
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblBinStartX;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblBinStartX != null)
                {
                }

                _lblBinStartX = value;
                if (_lblBinStartX != null)
                {
                }
            }
        }

        private GroupBox _fraSpectrumSimilarityOptions;

        internal GroupBox fraSpectrumSimilarityOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraSpectrumSimilarityOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraSpectrumSimilarityOptions != null)
                {
                }

                _fraSpectrumSimilarityOptions = value;
                if (_fraSpectrumSimilarityOptions != null)
                {
                }
            }
        }

        private TextBox _txtSpectrumSimilarityMinimum;

        internal TextBox txtSpectrumSimilarityMinimum
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtSpectrumSimilarityMinimum;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtSpectrumSimilarityMinimum != null)
                {
                    _txtSpectrumSimilarityMinimum.KeyPress -= txtSpectrumSimilarityMinimum_KeyPress;
                }

                _txtSpectrumSimilarityMinimum = value;
                if (_txtSpectrumSimilarityMinimum != null)
                {
                    _txtSpectrumSimilarityMinimum.KeyPress += txtSpectrumSimilarityMinimum_KeyPress;
                }
            }
        }

        private Label _lblSpectrumSimilarityMinimum;

        internal Label lblSpectrumSimilarityMinimum
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblSpectrumSimilarityMinimum;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblSpectrumSimilarityMinimum != null)
                {
                }

                _lblSpectrumSimilarityMinimum = value;
                if (_lblSpectrumSimilarityMinimum != null)
                {
                }
            }
        }

        private TextBox _txtSimilarIonToleranceHalfWidthMinutes;

        internal TextBox txtSimilarIonToleranceHalfWidthMinutes
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtSimilarIonToleranceHalfWidthMinutes;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtSimilarIonToleranceHalfWidthMinutes != null)
                {
                    _txtSimilarIonToleranceHalfWidthMinutes.KeyPress -= txtSimilarIonToleranceHalfWidthMinutes_KeyPress;
                }

                _txtSimilarIonToleranceHalfWidthMinutes = value;
                if (_txtSimilarIonToleranceHalfWidthMinutes != null)
                {
                    _txtSimilarIonToleranceHalfWidthMinutes.KeyPress += txtSimilarIonToleranceHalfWidthMinutes_KeyPress;
                }
            }
        }

        private Label _lblSimilarIonTimeToleranceHalfWidth;

        internal Label lblSimilarIonTimeToleranceHalfWidth
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblSimilarIonTimeToleranceHalfWidth;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblSimilarIonTimeToleranceHalfWidth != null)
                {
                }

                _lblSimilarIonTimeToleranceHalfWidth = value;
                if (_lblSimilarIonTimeToleranceHalfWidth != null)
                {
                }
            }
        }

        private TextBox _txtSimilarIonMZToleranceHalfWidth;

        internal TextBox txtSimilarIonMZToleranceHalfWidth
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtSimilarIonMZToleranceHalfWidth;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtSimilarIonMZToleranceHalfWidth != null)
                {
                    _txtSimilarIonMZToleranceHalfWidth.KeyPress -= txtSimilarIonMZToleranceHalfWidth_KeyPress;
                }

                _txtSimilarIonMZToleranceHalfWidth = value;
                if (_txtSimilarIonMZToleranceHalfWidth != null)
                {
                    _txtSimilarIonMZToleranceHalfWidth.KeyPress += txtSimilarIonMZToleranceHalfWidth_KeyPress;
                }
            }
        }

        private Label _lblSimilarIonMZToleranceHalfWidth;

        internal Label lblSimilarIonMZToleranceHalfWidth
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblSimilarIonMZToleranceHalfWidth;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblSimilarIonMZToleranceHalfWidth != null)
                {
                }

                _lblSimilarIonMZToleranceHalfWidth = value;
                if (_lblSimilarIonMZToleranceHalfWidth != null)
                {
                }
            }
        }

        private GroupBox _fraBinningIntensityOptions;

        internal GroupBox fraBinningIntensityOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraBinningIntensityOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraBinningIntensityOptions != null)
                {
                }

                _fraBinningIntensityOptions = value;
                if (_fraBinningIntensityOptions != null)
                {
                }
            }
        }

        private TextBox _txtBinnedDataIntensityPrecisionPct;

        internal TextBox txtBinnedDataIntensityPrecisionPct
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtBinnedDataIntensityPrecisionPct;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtBinnedDataIntensityPrecisionPct != null)
                {
                    _txtBinnedDataIntensityPrecisionPct.KeyPress -= txtBinnedDataIntensityPrecisionPct_KeyPress;
                }

                _txtBinnedDataIntensityPrecisionPct = value;
                if (_txtBinnedDataIntensityPrecisionPct != null)
                {
                    _txtBinnedDataIntensityPrecisionPct.KeyPress += txtBinnedDataIntensityPrecisionPct_KeyPress;
                }
            }
        }

        private Label _lblBinnedDataIntensityPrecisionPct;

        internal Label lblBinnedDataIntensityPrecisionPct
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblBinnedDataIntensityPrecisionPct;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblBinnedDataIntensityPrecisionPct != null)
                {
                }

                _lblBinnedDataIntensityPrecisionPct = value;
                if (_lblBinnedDataIntensityPrecisionPct != null)
                {
                }
            }
        }

        private TextBox _txtMaximumBinCount;

        internal TextBox txtMaximumBinCount
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtMaximumBinCount;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtMaximumBinCount != null)
                {
                    _txtMaximumBinCount.KeyPress -= txtMaximumBinCount_KeyPress;
                }

                _txtMaximumBinCount = value;
                if (_txtMaximumBinCount != null)
                {
                    _txtMaximumBinCount.KeyPress += txtMaximumBinCount_KeyPress;
                }
            }
        }

        private Label _lblMaximumBinCount;

        internal Label lblMaximumBinCount
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblMaximumBinCount;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblMaximumBinCount != null)
                {
                }

                _lblMaximumBinCount = value;
                if (_lblMaximumBinCount != null)
                {
                }
            }
        }

        private CheckBox _chkBinnedDataNormalize;

        internal CheckBox chkBinnedDataNormalize
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkBinnedDataNormalize;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkBinnedDataNormalize != null)
                {
                }

                _chkBinnedDataNormalize = value;
                if (_chkBinnedDataNormalize != null)
                {
                }
            }
        }

        private CheckBox _chkBinnedDataSumAllIntensitiesForBin;

        internal CheckBox chkBinnedDataSumAllIntensitiesForBin
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkBinnedDataSumAllIntensitiesForBin;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkBinnedDataSumAllIntensitiesForBin != null)
                {
                }

                _chkBinnedDataSumAllIntensitiesForBin = value;
                if (_chkBinnedDataSumAllIntensitiesForBin != null)
                {
                }
            }
        }

        private Button _cmdStartProcessing;

        internal Button cmdStartProcessing
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cmdStartProcessing;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cmdStartProcessing != null)
                {
                    _cmdStartProcessing.Click -= cmdStartProcessing_Click;
                }

                _cmdStartProcessing = value;
                if (_cmdStartProcessing != null)
                {
                    _cmdStartProcessing.Click += cmdStartProcessing_Click;
                }
            }
        }

        private TextBox _txtInitialPeakWidthScansScaler;

        internal TextBox txtInitialPeakWidthScansScaler
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtInitialPeakWidthScansScaler;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtInitialPeakWidthScansScaler != null)
                {
                    _txtInitialPeakWidthScansScaler.KeyPress -= txtInitialPeakWidthScansScaler_KeyPress;
                }

                _txtInitialPeakWidthScansScaler = value;
                if (_txtInitialPeakWidthScansScaler != null)
                {
                    _txtInitialPeakWidthScansScaler.KeyPress += txtInitialPeakWidthScansScaler_KeyPress;
                }
            }
        }

        private Label _lblInitialPeakWidthScansScaler;

        internal Label lblInitialPeakWidthScansScaler
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblInitialPeakWidthScansScaler;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblInitialPeakWidthScansScaler != null)
                {
                }

                _lblInitialPeakWidthScansScaler = value;
                if (_lblInitialPeakWidthScansScaler != null)
                {
                }
            }
        }

        private Label _lblBinnedDataIntensityPrecisionPctUnits;

        internal Label lblBinnedDataIntensityPrecisionPctUnits
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblBinnedDataIntensityPrecisionPctUnits;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblBinnedDataIntensityPrecisionPctUnits != null)
                {
                }

                _lblBinnedDataIntensityPrecisionPctUnits = value;
                if (_lblBinnedDataIntensityPrecisionPctUnits != null)
                {
                }
            }
        }

        private ComboBox _cboSICNoiseThresholdMode;

        internal ComboBox cboSICNoiseThresholdMode
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cboSICNoiseThresholdMode;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cboSICNoiseThresholdMode != null)
                {
                    _cboSICNoiseThresholdMode.SelectedIndexChanged -= cboSICNoiseThresholdMode_SelectedIndexChanged;
                }

                _cboSICNoiseThresholdMode = value;
                if (_cboSICNoiseThresholdMode != null)
                {
                    _cboSICNoiseThresholdMode.SelectedIndexChanged += cboSICNoiseThresholdMode_SelectedIndexChanged;
                }
            }
        }

        private Label _lblNoiseThresholdMode;

        internal Label lblNoiseThresholdMode
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblNoiseThresholdMode;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblNoiseThresholdMode != null)
                {
                }

                _lblNoiseThresholdMode = value;
                if (_lblNoiseThresholdMode != null)
                {
                }
            }
        }

        private TextBox _txtSICNoiseFractionLowIntensityDataToAverage;

        internal TextBox txtSICNoiseFractionLowIntensityDataToAverage
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtSICNoiseFractionLowIntensityDataToAverage;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtSICNoiseFractionLowIntensityDataToAverage != null)
                {
                    _txtSICNoiseFractionLowIntensityDataToAverage.KeyPress -= txtSICNoiseFractionLowIntensityDataToAverage_KeyPress;
                }

                _txtSICNoiseFractionLowIntensityDataToAverage = value;
                if (_txtSICNoiseFractionLowIntensityDataToAverage != null)
                {
                    _txtSICNoiseFractionLowIntensityDataToAverage.KeyPress += txtSICNoiseFractionLowIntensityDataToAverage_KeyPress;
                }
            }
        }

        private TextBox _txtSICNoiseThresholdIntensity;

        internal TextBox txtSICNoiseThresholdIntensity
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtSICNoiseThresholdIntensity;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtSICNoiseThresholdIntensity != null)
                {
                    _txtSICNoiseThresholdIntensity.KeyPress -= txtSICNoiseThresholdIntensity_KeyPress;
                }

                _txtSICNoiseThresholdIntensity = value;
                if (_txtSICNoiseThresholdIntensity != null)
                {
                    _txtSICNoiseThresholdIntensity.KeyPress += txtSICNoiseThresholdIntensity_KeyPress;
                }
            }
        }

        private Label _lblSICOptionsOverview;

        internal Label lblSICOptionsOverview
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblSICOptionsOverview;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblSICOptionsOverview != null)
                {
                }

                _lblSICOptionsOverview = value;
                if (_lblSICOptionsOverview != null)
                {
                }
            }
        }

        private Label _lblRawDataExportOverview;

        internal Label lblRawDataExportOverview
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblRawDataExportOverview;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblRawDataExportOverview != null)
                {
                }

                _lblRawDataExportOverview = value;
                if (_lblRawDataExportOverview != null)
                {
                }
            }
        }

        private TextBox _txtDatasetID;

        internal TextBox txtDatasetID
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtDatasetID;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtDatasetID != null)
                {
                    _txtDatasetID.KeyPress -= txtDatasetID_KeyPress;
                }

                _txtDatasetID = value;
                if (_txtDatasetID != null)
                {
                    _txtDatasetID.KeyPress += txtDatasetID_KeyPress;
                }
            }
        }

        private Label _lblDatasetID;

        internal Label lblDatasetID
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblDatasetID;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblDatasetID != null)
                {
                }

                _lblDatasetID = value;
                if (_lblDatasetID != null)
                {
                }
            }
        }

        private TextBox _txtCustomSICScanOrAcqTimeTolerance;

        internal TextBox txtCustomSICScanOrAcqTimeTolerance
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtCustomSICScanOrAcqTimeTolerance;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtCustomSICScanOrAcqTimeTolerance != null)
                {
                    _txtCustomSICScanOrAcqTimeTolerance.KeyPress -= txtCustomSICScanOrAcqTimeTolerance_KeyPress;
                }

                _txtCustomSICScanOrAcqTimeTolerance = value;
                if (_txtCustomSICScanOrAcqTimeTolerance != null)
                {
                    _txtCustomSICScanOrAcqTimeTolerance.KeyPress += txtCustomSICScanOrAcqTimeTolerance_KeyPress;
                }
            }
        }

        private Label _lblCustomSICScanTolerance;

        internal Label lblCustomSICScanTolerance
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblCustomSICScanTolerance;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblCustomSICScanTolerance != null)
                {
                }

                _lblCustomSICScanTolerance = value;
                if (_lblCustomSICScanTolerance != null)
                {
                }
            }
        }

        private Label _lblExportRawDataMaxIonCountPerScan;

        internal Label lblExportRawDataMaxIonCountPerScan
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblExportRawDataMaxIonCountPerScan;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblExportRawDataMaxIonCountPerScan != null)
                {
                }

                _lblExportRawDataMaxIonCountPerScan = value;
                if (_lblExportRawDataMaxIonCountPerScan != null)
                {
                }
            }
        }

        private Label _lblExportRawDataIntensityMinimum;

        internal Label lblExportRawDataIntensityMinimum
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblExportRawDataIntensityMinimum;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblExportRawDataIntensityMinimum != null)
                {
                }

                _lblExportRawDataIntensityMinimum = value;
                if (_lblExportRawDataIntensityMinimum != null)
                {
                }
            }
        }

        private Label _lblSICNoiseFractionLowIntensityDataToAverage;

        internal Label lblSICNoiseFractionLowIntensityDataToAverage
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblSICNoiseFractionLowIntensityDataToAverage;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblSICNoiseFractionLowIntensityDataToAverage != null)
                {
                }

                _lblSICNoiseFractionLowIntensityDataToAverage = value;
                if (_lblSICNoiseFractionLowIntensityDataToAverage != null)
                {
                }
            }
        }

        private Label _lblSICNoiseThresholdIntensity;

        internal Label lblSICNoiseThresholdIntensity
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblSICNoiseThresholdIntensity;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblSICNoiseThresholdIntensity != null)
                {
                }

                _lblSICNoiseThresholdIntensity = value;
                if (_lblSICNoiseThresholdIntensity != null)
                {
                }
            }
        }

        private TabPage _TabPageAdvancedOptions;

        internal TabPage TabPageAdvancedOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _TabPageAdvancedOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_TabPageAdvancedOptions != null)
                {
                }

                _TabPageAdvancedOptions = value;
                if (_TabPageAdvancedOptions != null)
                {
                }
            }
        }

        private GroupBox _fraMemoryConservationOptions;

        internal GroupBox fraMemoryConservationOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraMemoryConservationOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraMemoryConservationOptions != null)
                {
                }

                _fraMemoryConservationOptions = value;
                if (_fraMemoryConservationOptions != null)
                {
                }
            }
        }

        private CheckBox _chkSkipMSMSProcessing;

        internal CheckBox chkSkipMSMSProcessing
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkSkipMSMSProcessing;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkSkipMSMSProcessing != null)
                {
                    _chkSkipMSMSProcessing.CheckedChanged -= chkSkipMSMSProcessing_CheckedChanged;
                }

                _chkSkipMSMSProcessing = value;
                if (_chkSkipMSMSProcessing != null)
                {
                    _chkSkipMSMSProcessing.CheckedChanged += chkSkipMSMSProcessing_CheckedChanged;
                }
            }
        }

        private GroupBox _fraDatasetLookupInfo;

        internal GroupBox fraDatasetLookupInfo
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraDatasetLookupInfo;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraDatasetLookupInfo != null)
                {
                }

                _fraDatasetLookupInfo = value;
                if (_fraDatasetLookupInfo != null)
                {
                }
            }
        }

        private Button _cmdSelectDatasetLookupFile;

        internal Button cmdSelectDatasetLookupFile
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cmdSelectDatasetLookupFile;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cmdSelectDatasetLookupFile != null)
                {
                    _cmdSelectDatasetLookupFile.Click -= cmdSelectDatasetLookupFile_Click;
                }

                _cmdSelectDatasetLookupFile = value;
                if (_cmdSelectDatasetLookupFile != null)
                {
                    _cmdSelectDatasetLookupFile.Click += cmdSelectDatasetLookupFile_Click;
                }
            }
        }

        private TextBox _txtDatasetLookupFilePath;

        internal TextBox txtDatasetLookupFilePath
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtDatasetLookupFilePath;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtDatasetLookupFilePath != null)
                {
                }

                _txtDatasetLookupFilePath = value;
                if (_txtDatasetLookupFilePath != null)
                {
                }
            }
        }

        private Label _lblDatasetLookupFilePath;

        internal Label lblDatasetLookupFilePath
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblDatasetLookupFilePath;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblDatasetLookupFilePath != null)
                {
                }

                _lblDatasetLookupFilePath = value;
                if (_lblDatasetLookupFilePath != null)
                {
                }
            }
        }

        private Label _lblDatabaseConnectionString;

        internal Label lblDatabaseConnectionString
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblDatabaseConnectionString;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblDatabaseConnectionString != null)
                {
                }

                _lblDatabaseConnectionString = value;
                if (_lblDatabaseConnectionString != null)
                {
                }
            }
        }

        private TextBox _txtDatabaseConnectionString;

        internal TextBox txtDatabaseConnectionString
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtDatabaseConnectionString;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtDatabaseConnectionString != null)
                {
                }

                _txtDatabaseConnectionString = value;
                if (_txtDatabaseConnectionString != null)
                {
                }
            }
        }

        private GroupBox _fraSICNoiseThresholds;

        internal GroupBox fraSICNoiseThresholds
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraSICNoiseThresholds;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraSICNoiseThresholds != null)
                {
                }

                _fraSICNoiseThresholds = value;
                if (_fraSICNoiseThresholds != null)
                {
                }
            }
        }

        private GroupBox _fraMassSpectraNoiseThresholds;

        internal GroupBox fraMassSpectraNoiseThresholds
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraMassSpectraNoiseThresholds;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraMassSpectraNoiseThresholds != null)
                {
                }

                _fraMassSpectraNoiseThresholds = value;
                if (_fraMassSpectraNoiseThresholds != null)
                {
                }
            }
        }

        private TextBox _txtMassSpectraNoiseFractionLowIntensityDataToAverage;

        internal TextBox txtMassSpectraNoiseFractionLowIntensityDataToAverage
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtMassSpectraNoiseFractionLowIntensityDataToAverage;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtMassSpectraNoiseFractionLowIntensityDataToAverage != null)
                {
                    _txtMassSpectraNoiseFractionLowIntensityDataToAverage.KeyPress -= txtMassSpectraNoiseFractionLowIntensityDataToAverage_KeyPress;
                }

                _txtMassSpectraNoiseFractionLowIntensityDataToAverage = value;
                if (_txtMassSpectraNoiseFractionLowIntensityDataToAverage != null)
                {
                    _txtMassSpectraNoiseFractionLowIntensityDataToAverage.KeyPress += txtMassSpectraNoiseFractionLowIntensityDataToAverage_KeyPress;
                }
            }
        }

        private Label _lblMassSpectraNoiseFractionLowIntensityDataToAverage;

        internal Label lblMassSpectraNoiseFractionLowIntensityDataToAverage
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblMassSpectraNoiseFractionLowIntensityDataToAverage;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblMassSpectraNoiseFractionLowIntensityDataToAverage != null)
                {
                }

                _lblMassSpectraNoiseFractionLowIntensityDataToAverage = value;
                if (_lblMassSpectraNoiseFractionLowIntensityDataToAverage != null)
                {
                }
            }
        }

        private ComboBox _cboMassSpectraNoiseThresholdMode;

        internal ComboBox cboMassSpectraNoiseThresholdMode
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cboMassSpectraNoiseThresholdMode;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cboMassSpectraNoiseThresholdMode != null)
                {
                    _cboMassSpectraNoiseThresholdMode.SelectedIndexChanged -= cboMassSpectraNoiseThresholdMode_SelectedIndexChanged;
                }

                _cboMassSpectraNoiseThresholdMode = value;
                if (_cboMassSpectraNoiseThresholdMode != null)
                {
                    _cboMassSpectraNoiseThresholdMode.SelectedIndexChanged += cboMassSpectraNoiseThresholdMode_SelectedIndexChanged;
                }
            }
        }

        private Label _lblMassSpectraNoiseThresholdMode;

        internal Label lblMassSpectraNoiseThresholdMode
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblMassSpectraNoiseThresholdMode;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblMassSpectraNoiseThresholdMode != null)
                {
                }

                _lblMassSpectraNoiseThresholdMode = value;
                if (_lblMassSpectraNoiseThresholdMode != null)
                {
                }
            }
        }

        private TextBox _txtMassSpectraNoiseThresholdIntensity;

        internal TextBox txtMassSpectraNoiseThresholdIntensity
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtMassSpectraNoiseThresholdIntensity;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtMassSpectraNoiseThresholdIntensity != null)
                {
                    _txtMassSpectraNoiseThresholdIntensity.KeyPress -= txtMassSpectraNoiseThresholdIntensity_KeyPress;
                }

                _txtMassSpectraNoiseThresholdIntensity = value;
                if (_txtMassSpectraNoiseThresholdIntensity != null)
                {
                    _txtMassSpectraNoiseThresholdIntensity.KeyPress += txtMassSpectraNoiseThresholdIntensity_KeyPress;
                }
            }
        }

        private Label _lblMassSpectraNoiseThresholdIntensity;

        internal Label lblMassSpectraNoiseThresholdIntensity
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblMassSpectraNoiseThresholdIntensity;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblMassSpectraNoiseThresholdIntensity != null)
                {
                }

                _lblMassSpectraNoiseThresholdIntensity = value;
                if (_lblMassSpectraNoiseThresholdIntensity != null)
                {
                }
            }
        }

        private TextBox _txtExportRawDataSignalToNoiseRatioMinimum;

        internal TextBox txtExportRawDataSignalToNoiseRatioMinimum
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtExportRawDataSignalToNoiseRatioMinimum;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtExportRawDataSignalToNoiseRatioMinimum != null)
                {
                    _txtExportRawDataSignalToNoiseRatioMinimum.KeyPress -= txtExportRawDataSignalToNoiseRatioMinimum_KeyPress;
                }

                _txtExportRawDataSignalToNoiseRatioMinimum = value;
                if (_txtExportRawDataSignalToNoiseRatioMinimum != null)
                {
                    _txtExportRawDataSignalToNoiseRatioMinimum.KeyPress += txtExportRawDataSignalToNoiseRatioMinimum_KeyPress;
                }
            }
        }

        private Label _lblExportRawDataSignalToNoiseRatioMinimum;

        internal Label lblExportRawDataSignalToNoiseRatioMinimum
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblExportRawDataSignalToNoiseRatioMinimum;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblExportRawDataSignalToNoiseRatioMinimum != null)
                {
                }

                _lblExportRawDataSignalToNoiseRatioMinimum = value;
                if (_lblExportRawDataSignalToNoiseRatioMinimum != null)
                {
                }
            }
        }

        private TextBox _txtMassSpectraNoiseMinimumSignalToNoiseRatio;

        internal TextBox txtMassSpectraNoiseMinimumSignalToNoiseRatio
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtMassSpectraNoiseMinimumSignalToNoiseRatio;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtMassSpectraNoiseMinimumSignalToNoiseRatio != null)
                {
                }

                _txtMassSpectraNoiseMinimumSignalToNoiseRatio = value;
                if (_txtMassSpectraNoiseMinimumSignalToNoiseRatio != null)
                {
                }
            }
        }

        private Label _lblMassSpectraNoiseMinimumSignalToNoiseRatio;

        internal Label lblMassSpectraNoiseMinimumSignalToNoiseRatio
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblMassSpectraNoiseMinimumSignalToNoiseRatio;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblMassSpectraNoiseMinimumSignalToNoiseRatio != null)
                {
                }

                _lblMassSpectraNoiseMinimumSignalToNoiseRatio = value;
                if (_lblMassSpectraNoiseMinimumSignalToNoiseRatio != null)
                {
                }
            }
        }

        private GroupBox _fraInputFileRangeFilters;

        internal GroupBox fraInputFileRangeFilters
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraInputFileRangeFilters;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraInputFileRangeFilters != null)
                {
                }

                _fraInputFileRangeFilters = value;
                if (_fraInputFileRangeFilters != null)
                {
                }
            }
        }

        private Button _cmdClearAllRangeFilters;

        internal Button cmdClearAllRangeFilters
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cmdClearAllRangeFilters;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cmdClearAllRangeFilters != null)
                {
                    _cmdClearAllRangeFilters.Click -= cmdClearAllRangeFilters_Click;
                }

                _cmdClearAllRangeFilters = value;
                if (_cmdClearAllRangeFilters != null)
                {
                    _cmdClearAllRangeFilters.Click += cmdClearAllRangeFilters_Click;
                }
            }
        }

        private TextBox _txtScanEnd;

        internal TextBox txtScanEnd
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtScanEnd;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtScanEnd != null)
                {
                    _txtScanEnd.KeyPress -= txtScanEnd_KeyPress;
                }

                _txtScanEnd = value;
                if (_txtScanEnd != null)
                {
                    _txtScanEnd.KeyPress += txtScanEnd_KeyPress;
                }
            }
        }

        private TextBox _txtScanStart;

        internal TextBox txtScanStart
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtScanStart;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtScanStart != null)
                {
                    _txtScanStart.KeyPress -= txtScanStart_KeyPress;
                }

                _txtScanStart = value;
                if (_txtScanStart != null)
                {
                    _txtScanStart.KeyPress += txtScanStart_KeyPress;
                }
            }
        }

        private Label _lblScanEnd;

        internal Label lblScanEnd
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblScanEnd;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblScanEnd != null)
                {
                }

                _lblScanEnd = value;
                if (_lblScanEnd != null)
                {
                }
            }
        }

        private Label _lblScanStart;

        internal Label lblScanStart
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblScanStart;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblScanStart != null)
                {
                }

                _lblScanStart = value;
                if (_lblScanStart != null)
                {
                }
            }
        }

        private TextBox _txtTimeEnd;

        internal TextBox txtTimeEnd
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtTimeEnd;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtTimeEnd != null)
                {
                    _txtTimeEnd.KeyPress -= txtTimeEnd_KeyPress;
                }

                _txtTimeEnd = value;
                if (_txtTimeEnd != null)
                {
                    _txtTimeEnd.KeyPress += txtTimeEnd_KeyPress;
                }
            }
        }

        private Label _lblTimeEnd;

        internal Label lblTimeEnd
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblTimeEnd;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblTimeEnd != null)
                {
                }

                _lblTimeEnd = value;
                if (_lblTimeEnd != null)
                {
                }
            }
        }

        private Label _lblTimeStart;

        internal Label lblTimeStart
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblTimeStart;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblTimeStart != null)
                {
                }

                _lblTimeStart = value;
                if (_lblTimeStart != null)
                {
                }
            }
        }

        private TextBox _txtTimeStart;

        internal TextBox txtTimeStart
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtTimeStart;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtTimeStart != null)
                {
                    _txtTimeStart.KeyPress -= txtTimeStart_KeyPress;
                }

                _txtTimeStart = value;
                if (_txtTimeStart != null)
                {
                    _txtTimeStart.KeyPress += txtTimeStart_KeyPress;
                }
            }
        }

        private Label _lblTimeEndUnits;

        internal Label lblTimeEndUnits
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblTimeEndUnits;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblTimeEndUnits != null)
                {
                }

                _lblTimeEndUnits = value;
                if (_lblTimeEndUnits != null)
                {
                }
            }
        }

        private Label _lblTimeStartUnits;

        internal Label lblTimeStartUnits
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblTimeStartUnits;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblTimeStartUnits != null)
                {
                }

                _lblTimeStartUnits = value;
                if (_lblTimeStartUnits != null)
                {
                }
            }
        }

        private TextBox _txtButterworthSamplingFrequency;

        internal TextBox txtButterworthSamplingFrequency
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtButterworthSamplingFrequency;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtButterworthSamplingFrequency != null)
                {
                    _txtButterworthSamplingFrequency.KeyPress -= txtButterworthSamplingFrequency_KeyPress;
                    _txtButterworthSamplingFrequency.Validating -= txtButterworthSamplingFrequency_Validating;
                }

                _txtButterworthSamplingFrequency = value;
                if (_txtButterworthSamplingFrequency != null)
                {
                    _txtButterworthSamplingFrequency.KeyPress += txtButterworthSamplingFrequency_KeyPress;
                    _txtButterworthSamplingFrequency.Validating += txtButterworthSamplingFrequency_Validating;
                }
            }
        }

        private Label _lblButterworthSamplingFrequency;

        internal Label lblButterworthSamplingFrequency
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblButterworthSamplingFrequency;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblButterworthSamplingFrequency != null)
                {
                }

                _lblButterworthSamplingFrequency = value;
                if (_lblButterworthSamplingFrequency != null)
                {
                }
            }
        }

        private RadioButton _optUseButterworthSmooth;

        internal RadioButton optUseButterworthSmooth
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _optUseButterworthSmooth;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_optUseButterworthSmooth != null)
                {
                    _optUseButterworthSmooth.CheckedChanged -= optUseButterworthSmooth_CheckedChanged;
                }

                _optUseButterworthSmooth = value;
                if (_optUseButterworthSmooth != null)
                {
                    _optUseButterworthSmooth.CheckedChanged += optUseButterworthSmooth_CheckedChanged;
                }
            }
        }

        private RadioButton _optUseSavitzkyGolaySmooth;

        internal RadioButton optUseSavitzkyGolaySmooth
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _optUseSavitzkyGolaySmooth;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_optUseSavitzkyGolaySmooth != null)
                {
                    _optUseSavitzkyGolaySmooth.CheckedChanged -= optUseSavitzkyGolaySmooth_CheckedChanged;
                }

                _optUseSavitzkyGolaySmooth = value;
                if (_optUseSavitzkyGolaySmooth != null)
                {
                    _optUseSavitzkyGolaySmooth.CheckedChanged += optUseSavitzkyGolaySmooth_CheckedChanged;
                }
            }
        }

        private CheckBox _chkFindPeaksOnSmoothedData;

        internal CheckBox chkFindPeaksOnSmoothedData
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkFindPeaksOnSmoothedData;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkFindPeaksOnSmoothedData != null)
                {
                }

                _chkFindPeaksOnSmoothedData = value;
                if (_chkFindPeaksOnSmoothedData != null)
                {
                }
            }
        }

        private CheckBox _chkSmoothDataRegardlessOfMinimumPeakWidth;

        internal CheckBox chkSmoothDataRegardlessOfMinimumPeakWidth
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkSmoothDataRegardlessOfMinimumPeakWidth;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkSmoothDataRegardlessOfMinimumPeakWidth != null)
                {
                }

                _chkSmoothDataRegardlessOfMinimumPeakWidth = value;
                if (_chkSmoothDataRegardlessOfMinimumPeakWidth != null)
                {
                }
            }
        }

        private Label _lblSimilarIonTimeToleranceHalfWidthUnits;

        internal Label lblSimilarIonTimeToleranceHalfWidthUnits
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblSimilarIonTimeToleranceHalfWidthUnits;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblSimilarIonTimeToleranceHalfWidthUnits != null)
                {
                }

                _lblSimilarIonTimeToleranceHalfWidthUnits = value;
                if (_lblSimilarIonTimeToleranceHalfWidthUnits != null)
                {
                }
            }
        }

        private CheckBox _chkExportRawDataOnly;

        internal CheckBox chkExportRawDataOnly
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkExportRawDataOnly;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkExportRawDataOnly != null)
                {
                    _chkExportRawDataOnly.CheckedChanged -= chkExportRawDataOnly_CheckedChanged;
                }

                _chkExportRawDataOnly = value;
                if (_chkExportRawDataOnly != null)
                {
                    _chkExportRawDataOnly.CheckedChanged += chkExportRawDataOnly_CheckedChanged;
                }
            }
        }

        private CheckBox _chkLimitSearchToCustomMZs;

        internal CheckBox chkLimitSearchToCustomMZs
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkLimitSearchToCustomMZs;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkLimitSearchToCustomMZs != null)
                {
                }

                _chkLimitSearchToCustomMZs = value;
                if (_chkLimitSearchToCustomMZs != null)
                {
                }
            }
        }

        private CheckBox _chkReplaceSICZeroesWithMinimumPositiveValueFromMSData;

        internal CheckBox chkReplaceSICZeroesWithMinimumPositiveValueFromMSData
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkReplaceSICZeroesWithMinimumPositiveValueFromMSData;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkReplaceSICZeroesWithMinimumPositiveValueFromMSData != null)
                {
                }

                _chkReplaceSICZeroesWithMinimumPositiveValueFromMSData = value;
                if (_chkReplaceSICZeroesWithMinimumPositiveValueFromMSData != null)
                {
                }
            }
        }

        private Button _cmdSetConnectionStringToPNNLServer;

        internal Button cmdSetConnectionStringToPNNLServer
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cmdSetConnectionStringToPNNLServer;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cmdSetConnectionStringToPNNLServer != null)
                {
                    _cmdSetConnectionStringToPNNLServer.Click -= cmdSetConnectionStringToPNNLServer_Click;
                }

                _cmdSetConnectionStringToPNNLServer = value;
                if (_cmdSetConnectionStringToPNNLServer != null)
                {
                    _cmdSetConnectionStringToPNNLServer.Click += cmdSetConnectionStringToPNNLServer_Click;
                }
            }
        }

        private TextBox _txtDatasetInfoQuerySQL;

        internal TextBox txtDatasetInfoQuerySQL
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtDatasetInfoQuerySQL;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtDatasetInfoQuerySQL != null)
                {
                }

                _txtDatasetInfoQuerySQL = value;
                if (_txtDatasetInfoQuerySQL != null)
                {
                }
            }
        }

        private Label _lblDatasetInfoQuerySQL;

        internal Label lblDatasetInfoQuerySQL
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblDatasetInfoQuerySQL;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblDatasetInfoQuerySQL != null)
                {
                }

                _lblDatasetInfoQuerySQL = value;
                if (_lblDatasetInfoQuerySQL != null)
                {
                }
            }
        }

        private CheckBox _chkRefineReportedParentIonMZ;

        internal CheckBox chkRefineReportedParentIonMZ
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkRefineReportedParentIonMZ;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkRefineReportedParentIonMZ != null)
                {
                }

                _chkRefineReportedParentIonMZ = value;
                if (_chkRefineReportedParentIonMZ != null)
                {
                }
            }
        }

        private MenuItem _mnuEditSaveDefaultOptions;

        internal MenuItem mnuEditSaveDefaultOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _mnuEditSaveDefaultOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_mnuEditSaveDefaultOptions != null)
                {
                    _mnuEditSaveDefaultOptions.Click -= mnuEditSaveDefaultOptions_Click;
                }

                _mnuEditSaveDefaultOptions = value;
                if (_mnuEditSaveDefaultOptions != null)
                {
                    _mnuEditSaveDefaultOptions.Click += mnuEditSaveDefaultOptions_Click;
                }
            }
        }

        private CheckBox _chkSkipSICAndRawDataProcessing;

        internal CheckBox chkSkipSICAndRawDataProcessing
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkSkipSICAndRawDataProcessing;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkSkipSICAndRawDataProcessing != null)
                {
                    _chkSkipSICAndRawDataProcessing.CheckedChanged -= chkSkipSICAndRawDataProcessing_CheckedChanged;
                }

                _chkSkipSICAndRawDataProcessing = value;
                if (_chkSkipSICAndRawDataProcessing != null)
                {
                    _chkSkipSICAndRawDataProcessing.CheckedChanged += chkSkipSICAndRawDataProcessing_CheckedChanged;
                }
            }
        }

        private GroupBox _fraAdditionalInfoFiles;

        internal GroupBox fraAdditionalInfoFiles
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraAdditionalInfoFiles;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraAdditionalInfoFiles != null)
                {
                }

                _fraAdditionalInfoFiles = value;
                if (_fraAdditionalInfoFiles != null)
                {
                }
            }
        }

        private CheckBox _chkSaveExtendedStatsFile;

        internal CheckBox chkSaveExtendedStatsFile
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkSaveExtendedStatsFile;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkSaveExtendedStatsFile != null)
                {
                    _chkSaveExtendedStatsFile.CheckedChanged -= chkSaveExtendedStatsFile_CheckedChanged;
                }

                _chkSaveExtendedStatsFile = value;
                if (_chkSaveExtendedStatsFile != null)
                {
                    _chkSaveExtendedStatsFile.CheckedChanged += chkSaveExtendedStatsFile_CheckedChanged;
                }
            }
        }

        private CheckBox _chkSaveMSMethodFile;

        internal CheckBox chkSaveMSMethodFile
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkSaveMSMethodFile;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkSaveMSMethodFile != null)
                {
                }

                _chkSaveMSMethodFile = value;
                if (_chkSaveMSMethodFile != null)
                {
                }
            }
        }

        private CheckBox _chkSaveMSTuneFile;

        internal CheckBox chkSaveMSTuneFile
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkSaveMSTuneFile;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkSaveMSTuneFile != null)
                {
                }

                _chkSaveMSTuneFile = value;
                if (_chkSaveMSTuneFile != null)
                {
                }
            }
        }

        private CheckBox _chkIncludeScanTimesInSICStatsFile;

        internal CheckBox chkIncludeScanTimesInSICStatsFile
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkIncludeScanTimesInSICStatsFile;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkIncludeScanTimesInSICStatsFile != null)
                {
                }

                _chkIncludeScanTimesInSICStatsFile = value;
                if (_chkIncludeScanTimesInSICStatsFile != null)
                {
                }
            }
        }

        private Button _cmdSelectCustomSICFile;

        internal Button cmdSelectCustomSICFile
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cmdSelectCustomSICFile;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cmdSelectCustomSICFile != null)
                {
                    _cmdSelectCustomSICFile.Click -= cmdSelectCustomSICFile_Click;
                }

                _cmdSelectCustomSICFile = value;
                if (_cmdSelectCustomSICFile != null)
                {
                    _cmdSelectCustomSICFile.Click += cmdSelectCustomSICFile_Click;
                }
            }
        }

        private TextBox _txtCustomSICFileName;

        internal TextBox txtCustomSICFileName
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtCustomSICFileName;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtCustomSICFileName != null)
                {
                    _txtCustomSICFileName.TextChanged -= txtCustomSICFileName_TextChanged;
                }

                _txtCustomSICFileName = value;
                if (_txtCustomSICFileName != null)
                {
                    _txtCustomSICFileName.TextChanged += txtCustomSICFileName_TextChanged;
                }
            }
        }

        private CheckBox _chkSaveExtendedStatsFileIncludeFilterText;

        internal CheckBox chkSaveExtendedStatsFileIncludeFilterText
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkSaveExtendedStatsFileIncludeFilterText;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkSaveExtendedStatsFileIncludeFilterText != null)
                {
                }

                _chkSaveExtendedStatsFileIncludeFilterText = value;
                if (_chkSaveExtendedStatsFileIncludeFilterText != null)
                {
                }
            }
        }

        private CheckBox _chkSaveExtendedStatsFileIncludeStatusLog;

        internal CheckBox chkSaveExtendedStatsFileIncludeStatusLog
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkSaveExtendedStatsFileIncludeStatusLog;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkSaveExtendedStatsFileIncludeStatusLog != null)
                {
                    _chkSaveExtendedStatsFileIncludeStatusLog.CheckedChanged -= chkSaveExtendedStatsFileIncludeStatusLog_CheckedChanged;
                }

                _chkSaveExtendedStatsFileIncludeStatusLog = value;
                if (_chkSaveExtendedStatsFileIncludeStatusLog != null)
                {
                    _chkSaveExtendedStatsFileIncludeStatusLog.CheckedChanged += chkSaveExtendedStatsFileIncludeStatusLog_CheckedChanged;
                }
            }
        }

        private TabPage _TabPageReporterIons;

        internal TabPage TabPageReporterIons
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _TabPageReporterIons;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_TabPageReporterIons != null)
                {
                }

                _TabPageReporterIons = value;
                if (_TabPageReporterIons != null)
                {
                }
            }
        }

        private GroupBox _fraReporterIonOptions;

        internal GroupBox fraReporterIonOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraReporterIonOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraReporterIonOptions != null)
                {
                }

                _fraReporterIonOptions = value;
                if (_fraReporterIonOptions != null)
                {
                }
            }
        }

        private CheckBox _chkReporterIonStatsEnabled;

        internal CheckBox chkReporterIonStatsEnabled
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkReporterIonStatsEnabled;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkReporterIonStatsEnabled != null)
                {
                    _chkReporterIonStatsEnabled.CheckedChanged -= chkReporterIonStatsEnabled_CheckedChanged;
                }

                _chkReporterIonStatsEnabled = value;
                if (_chkReporterIonStatsEnabled != null)
                {
                    _chkReporterIonStatsEnabled.CheckedChanged += chkReporterIonStatsEnabled_CheckedChanged;
                }
            }
        }

        private TextBox _txtReporterIonMZToleranceDa;

        internal TextBox txtReporterIonMZToleranceDa
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtReporterIonMZToleranceDa;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtReporterIonMZToleranceDa != null)
                {
                }

                _txtReporterIonMZToleranceDa = value;
                if (_txtReporterIonMZToleranceDa != null)
                {
                }
            }
        }

        private Label _lblReporterIonMZToleranceDa;

        internal Label lblReporterIonMZToleranceDa
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblReporterIonMZToleranceDa;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblReporterIonMZToleranceDa != null)
                {
                }

                _lblReporterIonMZToleranceDa = value;
                if (_lblReporterIonMZToleranceDa != null)
                {
                }
            }
        }

        private Label _lblCustomSICToleranceType;

        internal Label lblCustomSICToleranceType
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblCustomSICToleranceType;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblCustomSICToleranceType != null)
                {
                }

                _lblCustomSICToleranceType = value;
                if (_lblCustomSICToleranceType != null)
                {
                }
            }
        }

        private RadioButton _optCustomSICScanToleranceAcqTime;

        internal RadioButton optCustomSICScanToleranceAcqTime
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _optCustomSICScanToleranceAcqTime;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_optCustomSICScanToleranceAcqTime != null)
                {
                    _optCustomSICScanToleranceAcqTime.CheckedChanged -= optCustomSICScanToleranceAcqTime_CheckedChanged;
                }

                _optCustomSICScanToleranceAcqTime = value;
                if (_optCustomSICScanToleranceAcqTime != null)
                {
                    _optCustomSICScanToleranceAcqTime.CheckedChanged += optCustomSICScanToleranceAcqTime_CheckedChanged;
                }
            }
        }

        private RadioButton _optCustomSICScanToleranceRelative;

        internal RadioButton optCustomSICScanToleranceRelative
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _optCustomSICScanToleranceRelative;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_optCustomSICScanToleranceRelative != null)
                {
                    _optCustomSICScanToleranceRelative.CheckedChanged -= optCustomSICScanToleranceRelative_CheckedChanged;
                }

                _optCustomSICScanToleranceRelative = value;
                if (_optCustomSICScanToleranceRelative != null)
                {
                    _optCustomSICScanToleranceRelative.CheckedChanged += optCustomSICScanToleranceRelative_CheckedChanged;
                }
            }
        }

        private RadioButton _optCustomSICScanToleranceAbsolute;

        internal RadioButton optCustomSICScanToleranceAbsolute
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _optCustomSICScanToleranceAbsolute;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_optCustomSICScanToleranceAbsolute != null)
                {
                    _optCustomSICScanToleranceAbsolute.CheckedChanged -= optCustomSICScanToleranceAbsolute_CheckedChanged;
                }

                _optCustomSICScanToleranceAbsolute = value;
                if (_optCustomSICScanToleranceAbsolute != null)
                {
                    _optCustomSICScanToleranceAbsolute.CheckedChanged += optCustomSICScanToleranceAbsolute_CheckedChanged;
                }
            }
        }

        private TextBox _txtCustomSICFileDescription;

        internal TextBox txtCustomSICFileDescription
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtCustomSICFileDescription;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtCustomSICFileDescription != null)
                {
                    _txtCustomSICFileDescription.KeyDown -= txtCustomSICFileDescription_KeyDown;
                }

                _txtCustomSICFileDescription = value;
                if (_txtCustomSICFileDescription != null)
                {
                    _txtCustomSICFileDescription.KeyDown += txtCustomSICFileDescription_KeyDown;
                }
            }
        }

        private GroupBox _fraMRMOptions;

        internal GroupBox fraMRMOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraMRMOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraMRMOptions != null)
                {
                }

                _fraMRMOptions = value;
                if (_fraMRMOptions != null)
                {
                }
            }
        }

        private CheckBox _chkMRMWriteIntensityCrosstab;

        internal CheckBox chkMRMWriteIntensityCrosstab
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkMRMWriteIntensityCrosstab;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkMRMWriteIntensityCrosstab != null)
                {
                }

                _chkMRMWriteIntensityCrosstab = value;
                if (_chkMRMWriteIntensityCrosstab != null)
                {
                }
            }
        }

        private Label _lblMRMInfo;

        internal Label lblMRMInfo
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblMRMInfo;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblMRMInfo != null)
                {
                }

                _lblMRMInfo = value;
                if (_lblMRMInfo != null)
                {
                }
            }
        }

        private CheckBox _chkMRMWriteDataList;

        internal CheckBox chkMRMWriteDataList
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkMRMWriteDataList;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkMRMWriteDataList != null)
                {
                }

                _chkMRMWriteDataList = value;
                if (_chkMRMWriteDataList != null)
                {
                }
            }
        }

        private RadioButton _optSICTolerancePPM;

        internal RadioButton optSICTolerancePPM
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _optSICTolerancePPM;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_optSICTolerancePPM != null)
                {
                }

                _optSICTolerancePPM = value;
                if (_optSICTolerancePPM != null)
                {
                }
            }
        }

        private RadioButton _optSICToleranceDa;

        internal RadioButton optSICToleranceDa
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _optSICToleranceDa;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_optSICToleranceDa != null)
                {
                }

                _optSICToleranceDa = value;
                if (_optSICToleranceDa != null)
                {
                }
            }
        }

        private Label _lblStatusLogKeyNameFilterList;

        internal Label lblStatusLogKeyNameFilterList
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblStatusLogKeyNameFilterList;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblStatusLogKeyNameFilterList != null)
                {
                }

                _lblStatusLogKeyNameFilterList = value;
                if (_lblStatusLogKeyNameFilterList != null)
                {
                }
            }
        }

        private TextBox _txtStatusLogKeyNameFilterList;

        internal TextBox txtStatusLogKeyNameFilterList
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtStatusLogKeyNameFilterList;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtStatusLogKeyNameFilterList != null)
                {
                }

                _txtStatusLogKeyNameFilterList = value;
                if (_txtStatusLogKeyNameFilterList != null)
                {
                }
            }
        }

        private GroupBox _fraDecoyOptions;

        internal GroupBox fraDecoyOptions
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraDecoyOptions;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraDecoyOptions != null)
                {
                }

                _fraDecoyOptions = value;
                if (_fraDecoyOptions != null)
                {
                }
            }
        }

        private TextBox _txtParentIonDecoyMassDa;

        internal TextBox txtParentIonDecoyMassDa
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtParentIonDecoyMassDa;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtParentIonDecoyMassDa != null)
                {
                }

                _txtParentIonDecoyMassDa = value;
                if (_txtParentIonDecoyMassDa != null)
                {
                }
            }
        }

        private Label _lblParentIonDecoyMassDa;

        internal Label lblParentIonDecoyMassDa
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblParentIonDecoyMassDa;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblParentIonDecoyMassDa != null)
                {
                }

                _lblParentIonDecoyMassDa = value;
                if (_lblParentIonDecoyMassDa != null)
                {
                }
            }
        }

        private Label _lblParentIonDecoyMassDaUnits;

        internal Label lblParentIonDecoyMassDaUnits
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblParentIonDecoyMassDaUnits;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblParentIonDecoyMassDaUnits != null)
                {
                }

                _lblParentIonDecoyMassDaUnits = value;
                if (_lblParentIonDecoyMassDaUnits != null)
                {
                }
            }
        }

        private CheckBox _chkReporterIonSaveUncorrectedIntensities;

        internal CheckBox chkReporterIonSaveUncorrectedIntensities
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkReporterIonSaveUncorrectedIntensities;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkReporterIonSaveUncorrectedIntensities != null)
                {
                }

                _chkReporterIonSaveUncorrectedIntensities = value;
                if (_chkReporterIonSaveUncorrectedIntensities != null)
                {
                }
            }
        }

        private CheckBox _chkReporterIonApplyAbundanceCorrection;

        internal CheckBox chkReporterIonApplyAbundanceCorrection
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkReporterIonApplyAbundanceCorrection;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkReporterIonApplyAbundanceCorrection != null)
                {
                }

                _chkReporterIonApplyAbundanceCorrection = value;
                if (_chkReporterIonApplyAbundanceCorrection != null)
                {
                }
            }
        }

        private CheckBox _chkConsolidateConstantExtendedHeaderValues;

        internal CheckBox chkConsolidateConstantExtendedHeaderValues
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkConsolidateConstantExtendedHeaderValues;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkConsolidateConstantExtendedHeaderValues != null)
                {
                }

                _chkConsolidateConstantExtendedHeaderValues = value;
                if (_chkConsolidateConstantExtendedHeaderValues != null)
                {
                }
            }
        }

        private CheckBox _chkWriteDetailedSICDataFile;

        internal CheckBox chkWriteDetailedSICDataFile
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkWriteDetailedSICDataFile;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkWriteDetailedSICDataFile != null)
                {
                }

                _chkWriteDetailedSICDataFile = value;
                if (_chkWriteDetailedSICDataFile != null)
                {
                }
            }
        }

        private GroupBox _fraReporterIonMassMode;

        internal GroupBox fraReporterIonMassMode
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _fraReporterIonMassMode;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_fraReporterIonMassMode != null)
                {
                }

                _fraReporterIonMassMode = value;
                if (_fraReporterIonMassMode != null)
                {
                }
            }
        }

        private ComboBox _cboReporterIonMassMode;

        internal ComboBox cboReporterIonMassMode
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cboReporterIonMassMode;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cboReporterIonMassMode != null)
                {
                    _cboReporterIonMassMode.SelectedIndexChanged -= cboReporterIonMassMode_SelectedIndexChanged;
                }

                _cboReporterIonMassMode = value;
                if (_cboReporterIonMassMode != null)
                {
                    _cboReporterIonMassMode.SelectedIndexChanged += cboReporterIonMassMode_SelectedIndexChanged;
                }
            }
        }

        private CheckBox _chkReporterIonSaveObservedMasses;

        internal CheckBox chkReporterIonSaveObservedMasses
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _chkReporterIonSaveObservedMasses;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_chkReporterIonSaveObservedMasses != null)
                {
                }

                _chkReporterIonSaveObservedMasses = value;
                if (_chkReporterIonSaveObservedMasses != null)
                {
                }
            }
        }

        private TabPage _TabPageLog;

        internal TabPage TabPageLog
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _TabPageLog;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_TabPageLog != null)
                {
                }

                _TabPageLog = value;
                if (_TabPageLog != null)
                {
                }
            }
        }

        private TextBox _txtLogMessages;

        internal TextBox txtLogMessages
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _txtLogMessages;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_txtLogMessages != null)
                {
                }

                _txtLogMessages = value;
                if (_txtLogMessages != null)
                {
                }
            }
        }
    }
}