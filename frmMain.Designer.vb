<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmMain
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmMain))
        Me.txtInputFilePath = New System.Windows.Forms.TextBox()
        Me.cmdSelectFile = New System.Windows.Forms.Button()
        Me.fraInputFilePath = New System.Windows.Forms.GroupBox()
        Me.MainMenuControl = New System.Windows.Forms.MainMenu(Me.components)
        Me.mnuFile = New System.Windows.Forms.MenuItem()
        Me.mnuFileSelectInputFile = New System.Windows.Forms.MenuItem()
        Me.mnuFileSelectOutputFolder = New System.Windows.Forms.MenuItem()
        Me.mnuFileSep1 = New System.Windows.Forms.MenuItem()
        Me.mnuFileLoadOptions = New System.Windows.Forms.MenuItem()
        Me.mnuFileSaveOptions = New System.Windows.Forms.MenuItem()
        Me.mnuFileSep2 = New System.Windows.Forms.MenuItem()
        Me.mnuFileExit = New System.Windows.Forms.MenuItem()
        Me.mnuEdit = New System.Windows.Forms.MenuItem()
        Me.mnuEditProcessFile = New System.Windows.Forms.MenuItem()
        Me.mnuEditSep1 = New System.Windows.Forms.MenuItem()
        Me.mnuEditSaveDefaultOptions = New System.Windows.Forms.MenuItem()
        Me.mnuEditResetOptions = New System.Windows.Forms.MenuItem()
        Me.mnuHelp = New System.Windows.Forms.MenuItem()
        Me.mnuHelpAbout = New System.Windows.Forms.MenuItem()
        Me.tbsOptions = New System.Windows.Forms.TabControl()
        Me.TabPageMasicExportOptions = New System.Windows.Forms.TabPage()
        Me.chkWriteDetailedSICDataFile = New System.Windows.Forms.CheckBox()
        Me.chkIncludeScanTimesInSICStatsFile = New System.Windows.Forms.CheckBox()
        Me.txtDatasetNumber = New System.Windows.Forms.TextBox()
        Me.lblDatasetNumber = New System.Windows.Forms.Label()
        Me.lblRawDataExportOverview = New System.Windows.Forms.Label()
        Me.fraExportAllSpectraDataPoints = New System.Windows.Forms.GroupBox()
        Me.txtExportRawDataSignalToNoiseRatioMinimum = New System.Windows.Forms.TextBox()
        Me.lblExportRawDataSignalToNoiseRatioMinimum = New System.Windows.Forms.Label()
        Me.chkExportRawDataRenumberScans = New System.Windows.Forms.CheckBox()
        Me.txtExportRawDataMaxIonCountPerScan = New System.Windows.Forms.TextBox()
        Me.lblExportRawDataMaxIonCountPerScan = New System.Windows.Forms.Label()
        Me.txtExportRawDataIntensityMinimum = New System.Windows.Forms.TextBox()
        Me.lblExportRawDataIntensityMinimum = New System.Windows.Forms.Label()
        Me.chkExportRawDataIncludeMSMS = New System.Windows.Forms.CheckBox()
        Me.cboExportRawDataFileFormat = New System.Windows.Forms.ComboBox()
        Me.lblExportDataPointsFormat = New System.Windows.Forms.Label()
        Me.chkExportRawSpectraData = New System.Windows.Forms.CheckBox()
        Me.chkIncludeHeaders = New System.Windows.Forms.CheckBox()
        Me.TabPageSICOptions = New System.Windows.Forms.TabPage()
        Me.fraInputFileRangeFilters = New System.Windows.Forms.GroupBox()
        Me.lblTimeEndUnits = New System.Windows.Forms.Label()
        Me.lblTimeStartUnits = New System.Windows.Forms.Label()
        Me.txtTimeEnd = New System.Windows.Forms.TextBox()
        Me.txtTimeStart = New System.Windows.Forms.TextBox()
        Me.lblTimeEnd = New System.Windows.Forms.Label()
        Me.lblTimeStart = New System.Windows.Forms.Label()
        Me.txtScanEnd = New System.Windows.Forms.TextBox()
        Me.txtScanStart = New System.Windows.Forms.TextBox()
        Me.lblScanEnd = New System.Windows.Forms.Label()
        Me.lblScanStart = New System.Windows.Forms.Label()
        Me.cmdClearAllRangeFilters = New System.Windows.Forms.Button()
        Me.lblSICOptionsOverview = New System.Windows.Forms.Label()
        Me.fraSICSearchThresholds = New System.Windows.Forms.GroupBox()
        Me.optSICTolerancePPM = New System.Windows.Forms.RadioButton()
        Me.optSICToleranceDa = New System.Windows.Forms.RadioButton()
        Me.chkRefineReportedParentIonMZ = New System.Windows.Forms.CheckBox()
        Me.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData = New System.Windows.Forms.CheckBox()
        Me.txtMaxPeakWidthMinutesForward = New System.Windows.Forms.TextBox()
        Me.txtMaxPeakWidthMinutesBackward = New System.Windows.Forms.TextBox()
        Me.txtIntensityThresholdFractionMax = New System.Windows.Forms.TextBox()
        Me.lblIntensityThresholdFractionMax = New System.Windows.Forms.Label()
        Me.txtIntensityThresholdAbsoluteMinimum = New System.Windows.Forms.TextBox()
        Me.lblIntensityThresholdAbsoluteMinimum = New System.Windows.Forms.Label()
        Me.lblMaxPeakWidthMinutesForward = New System.Windows.Forms.Label()
        Me.lblMaxPeakWidthMinutesBackward = New System.Windows.Forms.Label()
        Me.lblMaxPeakWidthMinutes = New System.Windows.Forms.Label()
        Me.txtSICTolerance = New System.Windows.Forms.TextBox()
        Me.lblSICToleranceDa = New System.Windows.Forms.Label()
        Me.TabPagePeakFindingOptions = New System.Windows.Forms.TabPage()
        Me.fraSICNoiseThresholds = New System.Windows.Forms.GroupBox()
        Me.cboSICNoiseThresholdMode = New System.Windows.Forms.ComboBox()
        Me.lblNoiseThresholdMode = New System.Windows.Forms.Label()
        Me.txtSICNoiseFractionLowIntensityDataToAverage = New System.Windows.Forms.TextBox()
        Me.lblSICNoiseFractionLowIntensityDataToAverage = New System.Windows.Forms.Label()
        Me.txtSICNoiseThresholdIntensity = New System.Windows.Forms.TextBox()
        Me.lblSICNoiseThresholdIntensity = New System.Windows.Forms.Label()
        Me.fraSmoothingOptions = New System.Windows.Forms.GroupBox()
        Me.chkSmoothDataRegardlessOfMinimumPeakWidth = New System.Windows.Forms.CheckBox()
        Me.chkFindPeaksOnSmoothedData = New System.Windows.Forms.CheckBox()
        Me.optUseSavitzkyGolaySmooth = New System.Windows.Forms.RadioButton()
        Me.txtButterworthSamplingFrequency = New System.Windows.Forms.TextBox()
        Me.lblButterworthSamplingFrequency = New System.Windows.Forms.Label()
        Me.txtSavitzkyGolayFilterOrder = New System.Windows.Forms.TextBox()
        Me.lblSavitzkyGolayFilterOrder = New System.Windows.Forms.Label()
        Me.optUseButterworthSmooth = New System.Windows.Forms.RadioButton()
        Me.fraPeakFindingOptions = New System.Windows.Forms.GroupBox()
        Me.txtInitialPeakWidthScansMaximum = New System.Windows.Forms.TextBox()
        Me.lblInitialPeakWidthScansMaximum = New System.Windows.Forms.Label()
        Me.txtInitialPeakWidthScansScaler = New System.Windows.Forms.TextBox()
        Me.lblInitialPeakWidthScansScaler = New System.Windows.Forms.Label()
        Me.txtMaxAllowedUpwardSpikeFractionMax = New System.Windows.Forms.TextBox()
        Me.lblMaxAllowedUpwardSpikeFractionMax = New System.Windows.Forms.Label()
        Me.txtMaxDistanceScansNoOverlap = New System.Windows.Forms.TextBox()
        Me.lblMaxDistanceScansNoOverlap = New System.Windows.Forms.Label()
        Me.TabPageBinningAndSimilarityOptions = New System.Windows.Forms.TabPage()
        Me.fraMassSpectraNoiseThresholds = New System.Windows.Forms.GroupBox()
        Me.txtMassSpectraNoiseMinimumSignalToNoiseRatio = New System.Windows.Forms.TextBox()
        Me.lblMassSpectraNoiseMinimumSignalToNoiseRatio = New System.Windows.Forms.Label()
        Me.txtMassSpectraNoiseThresholdIntensity = New System.Windows.Forms.TextBox()
        Me.txtMassSpectraNoiseFractionLowIntensityDataToAverage = New System.Windows.Forms.TextBox()
        Me.lblMassSpectraNoiseFractionLowIntensityDataToAverage = New System.Windows.Forms.Label()
        Me.cboMassSpectraNoiseThresholdMode = New System.Windows.Forms.ComboBox()
        Me.lblMassSpectraNoiseThresholdMode = New System.Windows.Forms.Label()
        Me.lblMassSpectraNoiseThresholdIntensity = New System.Windows.Forms.Label()
        Me.fraBinningIntensityOptions = New System.Windows.Forms.GroupBox()
        Me.lblBinnedDataIntensityPrecisionPctUnits = New System.Windows.Forms.Label()
        Me.chkBinnedDataSumAllIntensitiesForBin = New System.Windows.Forms.CheckBox()
        Me.chkBinnedDataNormalize = New System.Windows.Forms.CheckBox()
        Me.txtBinnedDataIntensityPrecisionPct = New System.Windows.Forms.TextBox()
        Me.lblBinnedDataIntensityPrecisionPct = New System.Windows.Forms.Label()
        Me.fraSpectrumSimilarityOptions = New System.Windows.Forms.GroupBox()
        Me.lblSimilarIonTimeToleranceHalfWidthUnits = New System.Windows.Forms.Label()
        Me.txtSpectrumSimilarityMinimum = New System.Windows.Forms.TextBox()
        Me.lblSpectrumSimilarityMinimum = New System.Windows.Forms.Label()
        Me.txtSimilarIonToleranceHalfWidthMinutes = New System.Windows.Forms.TextBox()
        Me.lblSimilarIonTimeToleranceHalfWidth = New System.Windows.Forms.Label()
        Me.txtSimilarIonMZToleranceHalfWidth = New System.Windows.Forms.TextBox()
        Me.lblSimilarIonMZToleranceHalfWidth = New System.Windows.Forms.Label()
        Me.fraBinningMZOptions = New System.Windows.Forms.GroupBox()
        Me.txtMaximumBinCount = New System.Windows.Forms.TextBox()
        Me.lblMaximumBinCount = New System.Windows.Forms.Label()
        Me.txtBinSize = New System.Windows.Forms.TextBox()
        Me.lblBinSize = New System.Windows.Forms.Label()
        Me.txtBinEndX = New System.Windows.Forms.TextBox()
        Me.lblBinEndX = New System.Windows.Forms.Label()
        Me.txtBinStartX = New System.Windows.Forms.TextBox()
        Me.lblBinStartX = New System.Windows.Forms.Label()
        Me.TabPageCustomSICOptions = New System.Windows.Forms.TabPage()
        Me.txtCustomSICFileDescription = New System.Windows.Forms.TextBox()
        Me.cmdSelectCustomSICFile = New System.Windows.Forms.Button()
        Me.txtCustomSICFileName = New System.Windows.Forms.TextBox()
        Me.fraCustomSICControls = New System.Windows.Forms.GroupBox()
        Me.lblCustomSICToleranceType = New System.Windows.Forms.Label()
        Me.optCustomSICScanToleranceAcqTime = New System.Windows.Forms.RadioButton()
        Me.optCustomSICScanToleranceRelative = New System.Windows.Forms.RadioButton()
        Me.optCustomSICScanToleranceAbsolute = New System.Windows.Forms.RadioButton()
        Me.chkLimitSearchToCustomMZs = New System.Windows.Forms.CheckBox()
        Me.txtCustomSICScanOrAcqTimeTolerance = New System.Windows.Forms.TextBox()
        Me.lblCustomSICScanTolerance = New System.Windows.Forms.Label()
        Me.cmdPasteCustomSICList = New System.Windows.Forms.Button()
        Me.cmdCustomSICValuesPopulate = New System.Windows.Forms.Button()
        Me.cmdClearCustomSICList = New System.Windows.Forms.Button()
        Me.dgCustomSICValues = New System.Windows.Forms.DataGrid()
        Me.TabPageReporterIons = New System.Windows.Forms.TabPage()
        Me.fraDecoyOptions = New System.Windows.Forms.GroupBox()
        Me.lblParentIonDecoyMassDaUnits = New System.Windows.Forms.Label()
        Me.txtParentIonDecoyMassDa = New System.Windows.Forms.TextBox()
        Me.lblParentIonDecoyMassDa = New System.Windows.Forms.Label()
        Me.fraMRMOptions = New System.Windows.Forms.GroupBox()
        Me.chkMRMWriteIntensityCrosstab = New System.Windows.Forms.CheckBox()
        Me.lblMRMInfo = New System.Windows.Forms.Label()
        Me.chkMRMWriteDataList = New System.Windows.Forms.CheckBox()
        Me.fraReporterIonMassMode = New System.Windows.Forms.GroupBox()
        Me.cboReporterIonMassMode = New System.Windows.Forms.ComboBox()
        Me.fraReporterIonOptions = New System.Windows.Forms.GroupBox()
        Me.chkReporterIonApplyAbundanceCorrection = New System.Windows.Forms.CheckBox()
        Me.chkReporterIonSaveUncorrectedIntensities = New System.Windows.Forms.CheckBox()
        Me.chkReporterIonSaveObservedMasses = New System.Windows.Forms.CheckBox()
        Me.txtReporterIonMZToleranceDa = New System.Windows.Forms.TextBox()
        Me.lblReporterIonMZToleranceDa = New System.Windows.Forms.Label()
        Me.chkReporterIonStatsEnabled = New System.Windows.Forms.CheckBox()
        Me.TabPageAdvancedOptions = New System.Windows.Forms.TabPage()
        Me.fraAdditionalInfoFiles = New System.Windows.Forms.GroupBox()
        Me.chkConsolidateConstantExtendedHeaderValues = New System.Windows.Forms.CheckBox()
        Me.lblStatusLogKeyNameFilterList = New System.Windows.Forms.Label()
        Me.txtStatusLogKeyNameFilterList = New System.Windows.Forms.TextBox()
        Me.chkSaveExtendedStatsFileIncludeStatusLog = New System.Windows.Forms.CheckBox()
        Me.chkSaveExtendedStatsFileIncludeFilterText = New System.Windows.Forms.CheckBox()
        Me.chkSaveMSTuneFile = New System.Windows.Forms.CheckBox()
        Me.chkSaveMSMethodFile = New System.Windows.Forms.CheckBox()
        Me.chkSaveExtendedStatsFile = New System.Windows.Forms.CheckBox()
        Me.fraDatasetLookupInfo = New System.Windows.Forms.GroupBox()
        Me.cmdSetConnectionStringToPNNLServer = New System.Windows.Forms.Button()
        Me.txtDatasetInfoQuerySQL = New System.Windows.Forms.TextBox()
        Me.lblDatasetInfoQuerySQL = New System.Windows.Forms.Label()
        Me.txtDatabaseConnectionString = New System.Windows.Forms.TextBox()
        Me.lblDatabaseConnectionString = New System.Windows.Forms.Label()
        Me.lblDatasetLookupFilePath = New System.Windows.Forms.Label()
        Me.cmdSelectDatasetLookupFile = New System.Windows.Forms.Button()
        Me.txtDatasetLookupFilePath = New System.Windows.Forms.TextBox()
        Me.fraMemoryConservationOptions = New System.Windows.Forms.GroupBox()
        Me.chkSkipMSMSProcessing = New System.Windows.Forms.CheckBox()
        Me.chkSkipSICAndRawDataProcessing = New System.Windows.Forms.CheckBox()
        Me.chkExportRawDataOnly = New System.Windows.Forms.CheckBox()
        Me.fraOutputFolderPath = New System.Windows.Forms.GroupBox()
        Me.cmdStartProcessing = New System.Windows.Forms.Button()
        Me.cmdSelectOutputFolder = New System.Windows.Forms.Button()
        Me.txtOutputFolderPath = New System.Windows.Forms.TextBox()
        Me.fraInputFilePath.SuspendLayout()
        Me.tbsOptions.SuspendLayout()
        Me.TabPageMasicExportOptions.SuspendLayout()
        Me.fraExportAllSpectraDataPoints.SuspendLayout()
        Me.TabPageSICOptions.SuspendLayout()
        Me.fraInputFileRangeFilters.SuspendLayout()
        Me.fraSICSearchThresholds.SuspendLayout()
        Me.TabPagePeakFindingOptions.SuspendLayout()
        Me.fraSICNoiseThresholds.SuspendLayout()
        Me.fraSmoothingOptions.SuspendLayout()
        Me.fraPeakFindingOptions.SuspendLayout()
        Me.TabPageBinningAndSimilarityOptions.SuspendLayout()
        Me.fraMassSpectraNoiseThresholds.SuspendLayout()
        Me.fraBinningIntensityOptions.SuspendLayout()
        Me.fraSpectrumSimilarityOptions.SuspendLayout()
        Me.fraBinningMZOptions.SuspendLayout()
        Me.TabPageCustomSICOptions.SuspendLayout()
        Me.fraCustomSICControls.SuspendLayout()
        CType(Me.dgCustomSICValues, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TabPageReporterIons.SuspendLayout()
        Me.fraDecoyOptions.SuspendLayout()
        Me.fraMRMOptions.SuspendLayout()
        Me.fraReporterIonMassMode.SuspendLayout()
        Me.fraReporterIonOptions.SuspendLayout()
        Me.TabPageAdvancedOptions.SuspendLayout()
        Me.fraAdditionalInfoFiles.SuspendLayout()
        Me.fraDatasetLookupInfo.SuspendLayout()
        Me.fraMemoryConservationOptions.SuspendLayout()
        Me.fraOutputFolderPath.SuspendLayout()
        Me.SuspendLayout()
        '
        'txtInputFilePath
        '
        Me.txtInputFilePath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtInputFilePath.Location = New System.Drawing.Point(125, 30)
        Me.txtInputFilePath.Name = "txtInputFilePath"
        Me.txtInputFilePath.Size = New System.Drawing.Size(741, 22)
        Me.txtInputFilePath.TabIndex = 1
        '
        'cmdSelectFile
        '
        Me.cmdSelectFile.Location = New System.Drawing.Point(10, 28)
        Me.cmdSelectFile.Name = "cmdSelectFile"
        Me.cmdSelectFile.Size = New System.Drawing.Size(96, 27)
        Me.cmdSelectFile.TabIndex = 0
        Me.cmdSelectFile.Text = "&Select File"
        '
        'fraInputFilePath
        '
        Me.fraInputFilePath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.fraInputFilePath.Controls.Add(Me.cmdSelectFile)
        Me.fraInputFilePath.Controls.Add(Me.txtInputFilePath)
        Me.fraInputFilePath.Location = New System.Drawing.Point(10, 9)
        Me.fraInputFilePath.Name = "fraInputFilePath"
        Me.fraInputFilePath.Size = New System.Drawing.Size(885, 74)
        Me.fraInputFilePath.TabIndex = 0
        Me.fraInputFilePath.TabStop = False
        Me.fraInputFilePath.Text = "Input File Path (Finnigan Ion Trap .Raw or Agilent .CDF/.MGF combo or mzXML or mz" &
    "Data)"
        '
        'MainMenuControl
        '
        Me.MainMenuControl.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.mnuFile, Me.mnuEdit, Me.mnuHelp})
        '
        'mnuFile
        '
        Me.mnuFile.Index = 0
        Me.mnuFile.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.mnuFileSelectInputFile, Me.mnuFileSelectOutputFolder, Me.mnuFileSep1, Me.mnuFileLoadOptions, Me.mnuFileSaveOptions, Me.mnuFileSep2, Me.mnuFileExit})
        Me.mnuFile.Text = "&File"
        '
        'mnuFileSelectInputFile
        '
        Me.mnuFileSelectInputFile.Index = 0
        Me.mnuFileSelectInputFile.Text = "&Select Input File ..."
        '
        'mnuFileSelectOutputFolder
        '
        Me.mnuFileSelectOutputFolder.Index = 1
        Me.mnuFileSelectOutputFolder.Text = "Select Output &Folder ..."
        '
        'mnuFileSep1
        '
        Me.mnuFileSep1.Index = 2
        Me.mnuFileSep1.Text = "-"
        '
        'mnuFileLoadOptions
        '
        Me.mnuFileLoadOptions.Index = 3
        Me.mnuFileLoadOptions.Text = "&Load Options ..."
        '
        'mnuFileSaveOptions
        '
        Me.mnuFileSaveOptions.Index = 4
        Me.mnuFileSaveOptions.Text = "Sa&ve Options ..."
        '
        'mnuFileSep2
        '
        Me.mnuFileSep2.Index = 5
        Me.mnuFileSep2.Text = "-"
        '
        'mnuFileExit
        '
        Me.mnuFileExit.Index = 6
        Me.mnuFileExit.Text = "E&xit"
        '
        'mnuEdit
        '
        Me.mnuEdit.Index = 1
        Me.mnuEdit.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.mnuEditProcessFile, Me.mnuEditSep1, Me.mnuEditSaveDefaultOptions, Me.mnuEditResetOptions})
        Me.mnuEdit.Text = "&Edit"
        '
        'mnuEditProcessFile
        '
        Me.mnuEditProcessFile.Index = 0
        Me.mnuEditProcessFile.Text = "&Process File"
        '
        'mnuEditSep1
        '
        Me.mnuEditSep1.Index = 1
        Me.mnuEditSep1.Text = "-"
        '
        'mnuEditSaveDefaultOptions
        '
        Me.mnuEditSaveDefaultOptions.Index = 2
        Me.mnuEditSaveDefaultOptions.Text = "&Save current options as Default ..."
        '
        'mnuEditResetOptions
        '
        Me.mnuEditResetOptions.Index = 3
        Me.mnuEditResetOptions.Text = "&Reset options to Defaults"
        '
        'mnuHelp
        '
        Me.mnuHelp.Index = 2
        Me.mnuHelp.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.mnuHelpAbout})
        Me.mnuHelp.Text = "&Help"
        '
        'mnuHelpAbout
        '
        Me.mnuHelpAbout.Index = 0
        Me.mnuHelpAbout.Text = "&About"
        '
        'tbsOptions
        '
        Me.tbsOptions.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.tbsOptions.Controls.Add(Me.TabPageMasicExportOptions)
        Me.tbsOptions.Controls.Add(Me.TabPageSICOptions)
        Me.tbsOptions.Controls.Add(Me.TabPagePeakFindingOptions)
        Me.tbsOptions.Controls.Add(Me.TabPageBinningAndSimilarityOptions)
        Me.tbsOptions.Controls.Add(Me.TabPageCustomSICOptions)
        Me.tbsOptions.Controls.Add(Me.TabPageReporterIons)
        Me.tbsOptions.Controls.Add(Me.TabPageAdvancedOptions)
        Me.tbsOptions.Location = New System.Drawing.Point(10, 203)
        Me.tbsOptions.Name = "tbsOptions"
        Me.tbsOptions.SelectedIndex = 0
        Me.tbsOptions.Size = New System.Drawing.Size(890, 356)
        Me.tbsOptions.TabIndex = 2
        '
        'TabPageMasicExportOptions
        '
        Me.TabPageMasicExportOptions.Controls.Add(Me.chkWriteDetailedSICDataFile)
        Me.TabPageMasicExportOptions.Controls.Add(Me.chkIncludeScanTimesInSICStatsFile)
        Me.TabPageMasicExportOptions.Controls.Add(Me.txtDatasetNumber)
        Me.TabPageMasicExportOptions.Controls.Add(Me.lblDatasetNumber)
        Me.TabPageMasicExportOptions.Controls.Add(Me.lblRawDataExportOverview)
        Me.TabPageMasicExportOptions.Controls.Add(Me.fraExportAllSpectraDataPoints)
        Me.TabPageMasicExportOptions.Controls.Add(Me.chkIncludeHeaders)
        Me.TabPageMasicExportOptions.Location = New System.Drawing.Point(4, 25)
        Me.TabPageMasicExportOptions.Name = "TabPageMasicExportOptions"
        Me.TabPageMasicExportOptions.Size = New System.Drawing.Size(882, 327)
        Me.TabPageMasicExportOptions.TabIndex = 4
        Me.TabPageMasicExportOptions.Text = "Export Options"
        Me.TabPageMasicExportOptions.UseVisualStyleBackColor = True
        '
        'chkWriteDetailedSICDataFile
        '
        Me.chkWriteDetailedSICDataFile.Location = New System.Drawing.Point(19, 62)
        Me.chkWriteDetailedSICDataFile.Name = "chkWriteDetailedSICDataFile"
        Me.chkWriteDetailedSICDataFile.Size = New System.Drawing.Size(250, 19)
        Me.chkWriteDetailedSICDataFile.TabIndex = 7
        Me.chkWriteDetailedSICDataFile.Text = "Write detailed SIC data file"
        '
        'chkIncludeScanTimesInSICStatsFile
        '
        Me.chkIncludeScanTimesInSICStatsFile.Checked = True
        Me.chkIncludeScanTimesInSICStatsFile.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkIncludeScanTimesInSICStatsFile.Location = New System.Drawing.Point(19, 37)
        Me.chkIncludeScanTimesInSICStatsFile.Name = "chkIncludeScanTimesInSICStatsFile"
        Me.chkIncludeScanTimesInSICStatsFile.Size = New System.Drawing.Size(250, 18)
        Me.chkIncludeScanTimesInSICStatsFile.TabIndex = 6
        Me.chkIncludeScanTimesInSICStatsFile.Text = "Include scan times in SIC stats file"
        '
        'txtDatasetNumber
        '
        Me.txtDatasetNumber.Location = New System.Drawing.Point(605, 18)
        Me.txtDatasetNumber.Name = "txtDatasetNumber"
        Me.txtDatasetNumber.Size = New System.Drawing.Size(105, 22)
        Me.txtDatasetNumber.TabIndex = 4
        Me.txtDatasetNumber.Text = "0"
        '
        'lblDatasetNumber
        '
        Me.lblDatasetNumber.Location = New System.Drawing.Point(432, 18)
        Me.lblDatasetNumber.Name = "lblDatasetNumber"
        Me.lblDatasetNumber.Size = New System.Drawing.Size(182, 19)
        Me.lblDatasetNumber.TabIndex = 3
        Me.lblDatasetNumber.Text = "Input File Dataset Number"
        '
        'lblRawDataExportOverview
        '
        Me.lblRawDataExportOverview.Location = New System.Drawing.Point(442, 55)
        Me.lblRawDataExportOverview.Name = "lblRawDataExportOverview"
        Me.lblRawDataExportOverview.Size = New System.Drawing.Size(307, 268)
        Me.lblRawDataExportOverview.TabIndex = 5
        Me.lblRawDataExportOverview.Text = "Raw Data Export Options Overview"
        '
        'fraExportAllSpectraDataPoints
        '
        Me.fraExportAllSpectraDataPoints.Controls.Add(Me.txtExportRawDataSignalToNoiseRatioMinimum)
        Me.fraExportAllSpectraDataPoints.Controls.Add(Me.lblExportRawDataSignalToNoiseRatioMinimum)
        Me.fraExportAllSpectraDataPoints.Controls.Add(Me.chkExportRawDataRenumberScans)
        Me.fraExportAllSpectraDataPoints.Controls.Add(Me.txtExportRawDataMaxIonCountPerScan)
        Me.fraExportAllSpectraDataPoints.Controls.Add(Me.lblExportRawDataMaxIonCountPerScan)
        Me.fraExportAllSpectraDataPoints.Controls.Add(Me.txtExportRawDataIntensityMinimum)
        Me.fraExportAllSpectraDataPoints.Controls.Add(Me.lblExportRawDataIntensityMinimum)
        Me.fraExportAllSpectraDataPoints.Controls.Add(Me.chkExportRawDataIncludeMSMS)
        Me.fraExportAllSpectraDataPoints.Controls.Add(Me.cboExportRawDataFileFormat)
        Me.fraExportAllSpectraDataPoints.Controls.Add(Me.lblExportDataPointsFormat)
        Me.fraExportAllSpectraDataPoints.Controls.Add(Me.chkExportRawSpectraData)
        Me.fraExportAllSpectraDataPoints.Location = New System.Drawing.Point(19, 93)
        Me.fraExportAllSpectraDataPoints.Name = "fraExportAllSpectraDataPoints"
        Me.fraExportAllSpectraDataPoints.Size = New System.Drawing.Size(413, 222)
        Me.fraExportAllSpectraDataPoints.TabIndex = 2
        Me.fraExportAllSpectraDataPoints.TabStop = False
        Me.fraExportAllSpectraDataPoints.Text = "Raw Data Point Export Options"
        '
        'txtExportRawDataSignalToNoiseRatioMinimum
        '
        Me.txtExportRawDataSignalToNoiseRatioMinimum.Location = New System.Drawing.Point(240, 129)
        Me.txtExportRawDataSignalToNoiseRatioMinimum.Name = "txtExportRawDataSignalToNoiseRatioMinimum"
        Me.txtExportRawDataSignalToNoiseRatioMinimum.Size = New System.Drawing.Size(48, 22)
        Me.txtExportRawDataSignalToNoiseRatioMinimum.TabIndex = 6
        Me.txtExportRawDataSignalToNoiseRatioMinimum.Text = "1"
        '
        'lblExportRawDataSignalToNoiseRatioMinimum
        '
        Me.lblExportRawDataSignalToNoiseRatioMinimum.Location = New System.Drawing.Point(19, 132)
        Me.lblExportRawDataSignalToNoiseRatioMinimum.Name = "lblExportRawDataSignalToNoiseRatioMinimum"
        Me.lblExportRawDataSignalToNoiseRatioMinimum.Size = New System.Drawing.Size(211, 18)
        Me.lblExportRawDataSignalToNoiseRatioMinimum.TabIndex = 5
        Me.lblExportRawDataSignalToNoiseRatioMinimum.Text = "Minimum Signal to Noise Ratio"
        '
        'chkExportRawDataRenumberScans
        '
        Me.chkExportRawDataRenumberScans.Location = New System.Drawing.Point(19, 76)
        Me.chkExportRawDataRenumberScans.Name = "chkExportRawDataRenumberScans"
        Me.chkExportRawDataRenumberScans.Size = New System.Drawing.Size(375, 19)
        Me.chkExportRawDataRenumberScans.TabIndex = 3
        Me.chkExportRawDataRenumberScans.Text = "Renumber survey scan spectra to make sequential"
        '
        'txtExportRawDataMaxIonCountPerScan
        '
        Me.txtExportRawDataMaxIonCountPerScan.Location = New System.Drawing.Point(240, 157)
        Me.txtExportRawDataMaxIonCountPerScan.Name = "txtExportRawDataMaxIonCountPerScan"
        Me.txtExportRawDataMaxIonCountPerScan.Size = New System.Drawing.Size(67, 22)
        Me.txtExportRawDataMaxIonCountPerScan.TabIndex = 8
        Me.txtExportRawDataMaxIonCountPerScan.Text = "200"
        '
        'lblExportRawDataMaxIonCountPerScan
        '
        Me.lblExportRawDataMaxIonCountPerScan.Location = New System.Drawing.Point(19, 159)
        Me.lblExportRawDataMaxIonCountPerScan.Name = "lblExportRawDataMaxIonCountPerScan"
        Me.lblExportRawDataMaxIonCountPerScan.Size = New System.Drawing.Size(221, 19)
        Me.lblExportRawDataMaxIonCountPerScan.TabIndex = 7
        Me.lblExportRawDataMaxIonCountPerScan.Text = "Maximum Ion Count per Scan"
        '
        'txtExportRawDataIntensityMinimum
        '
        Me.txtExportRawDataIntensityMinimum.Location = New System.Drawing.Point(240, 185)
        Me.txtExportRawDataIntensityMinimum.Name = "txtExportRawDataIntensityMinimum"
        Me.txtExportRawDataIntensityMinimum.Size = New System.Drawing.Size(106, 22)
        Me.txtExportRawDataIntensityMinimum.TabIndex = 10
        Me.txtExportRawDataIntensityMinimum.Text = "0"
        '
        'lblExportRawDataIntensityMinimum
        '
        Me.lblExportRawDataIntensityMinimum.Location = New System.Drawing.Point(19, 187)
        Me.lblExportRawDataIntensityMinimum.Name = "lblExportRawDataIntensityMinimum"
        Me.lblExportRawDataIntensityMinimum.Size = New System.Drawing.Size(183, 18)
        Me.lblExportRawDataIntensityMinimum.TabIndex = 9
        Me.lblExportRawDataIntensityMinimum.Text = "Minimum Intensity (counts)"
        '
        'chkExportRawDataIncludeMSMS
        '
        Me.chkExportRawDataIncludeMSMS.Location = New System.Drawing.Point(19, 99)
        Me.chkExportRawDataIncludeMSMS.Name = "chkExportRawDataIncludeMSMS"
        Me.chkExportRawDataIncludeMSMS.Size = New System.Drawing.Size(384, 19)
        Me.chkExportRawDataIncludeMSMS.TabIndex = 4
        Me.chkExportRawDataIncludeMSMS.Text = "Export MS/MS Spectra, in addition to survey scan spectra"
        '
        'cboExportRawDataFileFormat
        '
        Me.cboExportRawDataFileFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboExportRawDataFileFormat.Location = New System.Drawing.Point(106, 46)
        Me.cboExportRawDataFileFormat.Name = "cboExportRawDataFileFormat"
        Me.cboExportRawDataFileFormat.Size = New System.Drawing.Size(172, 24)
        Me.cboExportRawDataFileFormat.TabIndex = 2
        '
        'lblExportDataPointsFormat
        '
        Me.lblExportDataPointsFormat.Location = New System.Drawing.Point(38, 51)
        Me.lblExportDataPointsFormat.Name = "lblExportDataPointsFormat"
        Me.lblExportDataPointsFormat.Size = New System.Drawing.Size(87, 18)
        Me.lblExportDataPointsFormat.TabIndex = 1
        Me.lblExportDataPointsFormat.Text = "Format:"
        '
        'chkExportRawSpectraData
        '
        Me.chkExportRawSpectraData.Location = New System.Drawing.Point(19, 28)
        Me.chkExportRawSpectraData.Name = "chkExportRawSpectraData"
        Me.chkExportRawSpectraData.Size = New System.Drawing.Size(288, 18)
        Me.chkExportRawSpectraData.TabIndex = 0
        Me.chkExportRawSpectraData.Text = "Export All Spectra Data Points"
        '
        'chkIncludeHeaders
        '
        Me.chkIncludeHeaders.Checked = True
        Me.chkIncludeHeaders.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkIncludeHeaders.Location = New System.Drawing.Point(19, 12)
        Me.chkIncludeHeaders.Name = "chkIncludeHeaders"
        Me.chkIncludeHeaders.Size = New System.Drawing.Size(192, 18)
        Me.chkIncludeHeaders.TabIndex = 0
        Me.chkIncludeHeaders.Text = "Include Column Headers"
        '
        'TabPageSICOptions
        '
        Me.TabPageSICOptions.Controls.Add(Me.fraInputFileRangeFilters)
        Me.TabPageSICOptions.Controls.Add(Me.lblSICOptionsOverview)
        Me.TabPageSICOptions.Controls.Add(Me.fraSICSearchThresholds)
        Me.TabPageSICOptions.Location = New System.Drawing.Point(4, 25)
        Me.TabPageSICOptions.Name = "TabPageSICOptions"
        Me.TabPageSICOptions.Size = New System.Drawing.Size(882, 327)
        Me.TabPageSICOptions.TabIndex = 5
        Me.TabPageSICOptions.Text = "SIC Options"
        Me.TabPageSICOptions.UseVisualStyleBackColor = True
        '
        'fraInputFileRangeFilters
        '
        Me.fraInputFileRangeFilters.Controls.Add(Me.lblTimeEndUnits)
        Me.fraInputFileRangeFilters.Controls.Add(Me.lblTimeStartUnits)
        Me.fraInputFileRangeFilters.Controls.Add(Me.txtTimeEnd)
        Me.fraInputFileRangeFilters.Controls.Add(Me.txtTimeStart)
        Me.fraInputFileRangeFilters.Controls.Add(Me.lblTimeEnd)
        Me.fraInputFileRangeFilters.Controls.Add(Me.lblTimeStart)
        Me.fraInputFileRangeFilters.Controls.Add(Me.txtScanEnd)
        Me.fraInputFileRangeFilters.Controls.Add(Me.txtScanStart)
        Me.fraInputFileRangeFilters.Controls.Add(Me.lblScanEnd)
        Me.fraInputFileRangeFilters.Controls.Add(Me.lblScanStart)
        Me.fraInputFileRangeFilters.Controls.Add(Me.cmdClearAllRangeFilters)
        Me.fraInputFileRangeFilters.Location = New System.Drawing.Point(19, 222)
        Me.fraInputFileRangeFilters.Name = "fraInputFileRangeFilters"
        Me.fraInputFileRangeFilters.Size = New System.Drawing.Size(586, 92)
        Me.fraInputFileRangeFilters.TabIndex = 1
        Me.fraInputFileRangeFilters.TabStop = False
        Me.fraInputFileRangeFilters.Text = "Input File Range Filters"
        '
        'lblTimeEndUnits
        '
        Me.lblTimeEndUnits.Location = New System.Drawing.Point(499, 58)
        Me.lblTimeEndUnits.Name = "lblTimeEndUnits"
        Me.lblTimeEndUnits.Size = New System.Drawing.Size(77, 18)
        Me.lblTimeEndUnits.TabIndex = 10
        Me.lblTimeEndUnits.Text = "minutes"
        '
        'lblTimeStartUnits
        '
        Me.lblTimeStartUnits.Location = New System.Drawing.Point(499, 30)
        Me.lblTimeStartUnits.Name = "lblTimeStartUnits"
        Me.lblTimeStartUnits.Size = New System.Drawing.Size(77, 18)
        Me.lblTimeStartUnits.TabIndex = 7
        Me.lblTimeStartUnits.Text = "minutes"
        '
        'txtTimeEnd
        '
        Me.txtTimeEnd.Location = New System.Drawing.Point(422, 55)
        Me.txtTimeEnd.Name = "txtTimeEnd"
        Me.txtTimeEnd.Size = New System.Drawing.Size(68, 22)
        Me.txtTimeEnd.TabIndex = 9
        Me.txtTimeEnd.Text = "0"
        '
        'txtTimeStart
        '
        Me.txtTimeStart.Location = New System.Drawing.Point(422, 28)
        Me.txtTimeStart.Name = "txtTimeStart"
        Me.txtTimeStart.Size = New System.Drawing.Size(68, 22)
        Me.txtTimeStart.TabIndex = 6
        Me.txtTimeStart.Text = "0"
        '
        'lblTimeEnd
        '
        Me.lblTimeEnd.Location = New System.Drawing.Point(336, 58)
        Me.lblTimeEnd.Name = "lblTimeEnd"
        Me.lblTimeEnd.Size = New System.Drawing.Size(86, 18)
        Me.lblTimeEnd.TabIndex = 8
        Me.lblTimeEnd.Text = "End Time"
        '
        'lblTimeStart
        '
        Me.lblTimeStart.Location = New System.Drawing.Point(336, 30)
        Me.lblTimeStart.Name = "lblTimeStart"
        Me.lblTimeStart.Size = New System.Drawing.Size(77, 18)
        Me.lblTimeStart.TabIndex = 5
        Me.lblTimeStart.Text = "Start Time"
        '
        'txtScanEnd
        '
        Me.txtScanEnd.Location = New System.Drawing.Point(230, 55)
        Me.txtScanEnd.Name = "txtScanEnd"
        Me.txtScanEnd.Size = New System.Drawing.Size(68, 22)
        Me.txtScanEnd.TabIndex = 4
        Me.txtScanEnd.Text = "0"
        '
        'txtScanStart
        '
        Me.txtScanStart.Location = New System.Drawing.Point(230, 28)
        Me.txtScanStart.Name = "txtScanStart"
        Me.txtScanStart.Size = New System.Drawing.Size(68, 22)
        Me.txtScanStart.TabIndex = 2
        Me.txtScanStart.Text = "0"
        '
        'lblScanEnd
        '
        Me.lblScanEnd.Location = New System.Drawing.Point(144, 58)
        Me.lblScanEnd.Name = "lblScanEnd"
        Me.lblScanEnd.Size = New System.Drawing.Size(86, 18)
        Me.lblScanEnd.TabIndex = 3
        Me.lblScanEnd.Text = "End Scan"
        '
        'lblScanStart
        '
        Me.lblScanStart.Location = New System.Drawing.Point(144, 30)
        Me.lblScanStart.Name = "lblScanStart"
        Me.lblScanStart.Size = New System.Drawing.Size(77, 18)
        Me.lblScanStart.TabIndex = 1
        Me.lblScanStart.Text = "Start Scan"
        '
        'cmdClearAllRangeFilters
        '
        Me.cmdClearAllRangeFilters.Location = New System.Drawing.Point(19, 37)
        Me.cmdClearAllRangeFilters.Name = "cmdClearAllRangeFilters"
        Me.cmdClearAllRangeFilters.Size = New System.Drawing.Size(106, 28)
        Me.cmdClearAllRangeFilters.TabIndex = 0
        Me.cmdClearAllRangeFilters.Text = "Clear Filters"
        '
        'lblSICOptionsOverview
        '
        Me.lblSICOptionsOverview.Location = New System.Drawing.Point(374, 28)
        Me.lblSICOptionsOverview.Name = "lblSICOptionsOverview"
        Me.lblSICOptionsOverview.Size = New System.Drawing.Size(426, 175)
        Me.lblSICOptionsOverview.TabIndex = 2
        Me.lblSICOptionsOverview.Text = "SIC Options Overview"
        '
        'fraSICSearchThresholds
        '
        Me.fraSICSearchThresholds.Controls.Add(Me.optSICTolerancePPM)
        Me.fraSICSearchThresholds.Controls.Add(Me.optSICToleranceDa)
        Me.fraSICSearchThresholds.Controls.Add(Me.chkRefineReportedParentIonMZ)
        Me.fraSICSearchThresholds.Controls.Add(Me.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData)
        Me.fraSICSearchThresholds.Controls.Add(Me.txtMaxPeakWidthMinutesForward)
        Me.fraSICSearchThresholds.Controls.Add(Me.txtMaxPeakWidthMinutesBackward)
        Me.fraSICSearchThresholds.Controls.Add(Me.txtIntensityThresholdFractionMax)
        Me.fraSICSearchThresholds.Controls.Add(Me.lblIntensityThresholdFractionMax)
        Me.fraSICSearchThresholds.Controls.Add(Me.txtIntensityThresholdAbsoluteMinimum)
        Me.fraSICSearchThresholds.Controls.Add(Me.lblIntensityThresholdAbsoluteMinimum)
        Me.fraSICSearchThresholds.Controls.Add(Me.lblMaxPeakWidthMinutesForward)
        Me.fraSICSearchThresholds.Controls.Add(Me.lblMaxPeakWidthMinutesBackward)
        Me.fraSICSearchThresholds.Controls.Add(Me.lblMaxPeakWidthMinutes)
        Me.fraSICSearchThresholds.Controls.Add(Me.txtSICTolerance)
        Me.fraSICSearchThresholds.Controls.Add(Me.lblSICToleranceDa)
        Me.fraSICSearchThresholds.Location = New System.Drawing.Point(19, 9)
        Me.fraSICSearchThresholds.Name = "fraSICSearchThresholds"
        Me.fraSICSearchThresholds.Size = New System.Drawing.Size(336, 203)
        Me.fraSICSearchThresholds.TabIndex = 0
        Me.fraSICSearchThresholds.TabStop = False
        Me.fraSICSearchThresholds.Text = "SIC Search Thresholds"
        '
        'optSICTolerancePPM
        '
        Me.optSICTolerancePPM.Location = New System.Drawing.Point(230, 32)
        Me.optSICTolerancePPM.Name = "optSICTolerancePPM"
        Me.optSICTolerancePPM.Size = New System.Drawing.Size(87, 21)
        Me.optSICTolerancePPM.TabIndex = 14
        Me.optSICTolerancePPM.Text = "ppm"
        '
        'optSICToleranceDa
        '
        Me.optSICToleranceDa.Checked = True
        Me.optSICToleranceDa.Location = New System.Drawing.Point(230, 12)
        Me.optSICToleranceDa.Name = "optSICToleranceDa"
        Me.optSICToleranceDa.Size = New System.Drawing.Size(87, 20)
        Me.optSICToleranceDa.TabIndex = 13
        Me.optSICToleranceDa.TabStop = True
        Me.optSICToleranceDa.Text = "Da"
        '
        'chkRefineReportedParentIonMZ
        '
        Me.chkRefineReportedParentIonMZ.Location = New System.Drawing.Point(10, 175)
        Me.chkRefineReportedParentIonMZ.Name = "chkRefineReportedParentIonMZ"
        Me.chkRefineReportedParentIonMZ.Size = New System.Drawing.Size(316, 19)
        Me.chkRefineReportedParentIonMZ.TabIndex = 12
        Me.chkRefineReportedParentIonMZ.Text = "Refine reported parent ion m/z values"
        '
        'chkReplaceSICZeroesWithMinimumPositiveValueFromMSData
        '
        Me.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Checked = True
        Me.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Location = New System.Drawing.Point(10, 157)
        Me.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Name = "chkReplaceSICZeroesWithMinimumPositiveValueFromMSData"
        Me.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Size = New System.Drawing.Size(316, 18)
        Me.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.TabIndex = 11
        Me.chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Text = "Replace SIC zeroes with min MS data values"
        '
        'txtMaxPeakWidthMinutesForward
        '
        Me.txtMaxPeakWidthMinutesForward.Location = New System.Drawing.Point(250, 65)
        Me.txtMaxPeakWidthMinutesForward.Name = "txtMaxPeakWidthMinutesForward"
        Me.txtMaxPeakWidthMinutesForward.Size = New System.Drawing.Size(67, 22)
        Me.txtMaxPeakWidthMinutesForward.TabIndex = 6
        Me.txtMaxPeakWidthMinutesForward.Text = "3"
        '
        'txtMaxPeakWidthMinutesBackward
        '
        Me.txtMaxPeakWidthMinutesBackward.Location = New System.Drawing.Point(96, 65)
        Me.txtMaxPeakWidthMinutesBackward.Name = "txtMaxPeakWidthMinutesBackward"
        Me.txtMaxPeakWidthMinutesBackward.Size = New System.Drawing.Size(67, 22)
        Me.txtMaxPeakWidthMinutesBackward.TabIndex = 4
        Me.txtMaxPeakWidthMinutesBackward.Text = "3"
        '
        'txtIntensityThresholdFractionMax
        '
        Me.txtIntensityThresholdFractionMax.Location = New System.Drawing.Point(250, 92)
        Me.txtIntensityThresholdFractionMax.Name = "txtIntensityThresholdFractionMax"
        Me.txtIntensityThresholdFractionMax.Size = New System.Drawing.Size(67, 22)
        Me.txtIntensityThresholdFractionMax.TabIndex = 8
        Me.txtIntensityThresholdFractionMax.Text = "0.01"
        '
        'lblIntensityThresholdFractionMax
        '
        Me.lblIntensityThresholdFractionMax.Location = New System.Drawing.Point(10, 92)
        Me.lblIntensityThresholdFractionMax.Name = "lblIntensityThresholdFractionMax"
        Me.lblIntensityThresholdFractionMax.Size = New System.Drawing.Size(240, 19)
        Me.lblIntensityThresholdFractionMax.TabIndex = 7
        Me.lblIntensityThresholdFractionMax.Text = "Intensity Threshold Fraction Max Peak"
        '
        'txtIntensityThresholdAbsoluteMinimum
        '
        Me.txtIntensityThresholdAbsoluteMinimum.Location = New System.Drawing.Point(250, 120)
        Me.txtIntensityThresholdAbsoluteMinimum.Name = "txtIntensityThresholdAbsoluteMinimum"
        Me.txtIntensityThresholdAbsoluteMinimum.Size = New System.Drawing.Size(67, 22)
        Me.txtIntensityThresholdAbsoluteMinimum.TabIndex = 10
        Me.txtIntensityThresholdAbsoluteMinimum.Text = "0"
        '
        'lblIntensityThresholdAbsoluteMinimum
        '
        Me.lblIntensityThresholdAbsoluteMinimum.Location = New System.Drawing.Point(10, 120)
        Me.lblIntensityThresholdAbsoluteMinimum.Name = "lblIntensityThresholdAbsoluteMinimum"
        Me.lblIntensityThresholdAbsoluteMinimum.Size = New System.Drawing.Size(240, 18)
        Me.lblIntensityThresholdAbsoluteMinimum.TabIndex = 9
        Me.lblIntensityThresholdAbsoluteMinimum.Text = "Intensity Threshold Absolute Minimum"
        '
        'lblMaxPeakWidthMinutesForward
        '
        Me.lblMaxPeakWidthMinutesForward.Location = New System.Drawing.Point(182, 65)
        Me.lblMaxPeakWidthMinutesForward.Name = "lblMaxPeakWidthMinutesForward"
        Me.lblMaxPeakWidthMinutesForward.Size = New System.Drawing.Size(77, 18)
        Me.lblMaxPeakWidthMinutesForward.TabIndex = 5
        Me.lblMaxPeakWidthMinutesForward.Text = "Forward"
        '
        'lblMaxPeakWidthMinutesBackward
        '
        Me.lblMaxPeakWidthMinutesBackward.Location = New System.Drawing.Point(19, 65)
        Me.lblMaxPeakWidthMinutesBackward.Name = "lblMaxPeakWidthMinutesBackward"
        Me.lblMaxPeakWidthMinutesBackward.Size = New System.Drawing.Size(77, 18)
        Me.lblMaxPeakWidthMinutesBackward.TabIndex = 3
        Me.lblMaxPeakWidthMinutesBackward.Text = "Backward"
        '
        'lblMaxPeakWidthMinutes
        '
        Me.lblMaxPeakWidthMinutes.Location = New System.Drawing.Point(10, 46)
        Me.lblMaxPeakWidthMinutes.Name = "lblMaxPeakWidthMinutes"
        Me.lblMaxPeakWidthMinutes.Size = New System.Drawing.Size(201, 19)
        Me.lblMaxPeakWidthMinutes.TabIndex = 2
        Me.lblMaxPeakWidthMinutes.Text = "Maximum Peak Width (minutes)"
        '
        'txtSICTolerance
        '
        Me.txtSICTolerance.Location = New System.Drawing.Point(154, 18)
        Me.txtSICTolerance.Name = "txtSICTolerance"
        Me.txtSICTolerance.Size = New System.Drawing.Size(57, 22)
        Me.txtSICTolerance.TabIndex = 1
        Me.txtSICTolerance.Text = "0.60"
        '
        'lblSICToleranceDa
        '
        Me.lblSICToleranceDa.Location = New System.Drawing.Point(10, 18)
        Me.lblSICToleranceDa.Name = "lblSICToleranceDa"
        Me.lblSICToleranceDa.Size = New System.Drawing.Size(144, 19)
        Me.lblSICToleranceDa.TabIndex = 0
        Me.lblSICToleranceDa.Text = "SIC Tolerance (Da)"
        '
        'TabPagePeakFindingOptions
        '
        Me.TabPagePeakFindingOptions.Controls.Add(Me.fraSICNoiseThresholds)
        Me.TabPagePeakFindingOptions.Controls.Add(Me.fraSmoothingOptions)
        Me.TabPagePeakFindingOptions.Controls.Add(Me.fraPeakFindingOptions)
        Me.TabPagePeakFindingOptions.Location = New System.Drawing.Point(4, 25)
        Me.TabPagePeakFindingOptions.Name = "TabPagePeakFindingOptions"
        Me.TabPagePeakFindingOptions.Size = New System.Drawing.Size(882, 327)
        Me.TabPagePeakFindingOptions.TabIndex = 7
        Me.TabPagePeakFindingOptions.Text = "Peak Finding Options"
        Me.TabPagePeakFindingOptions.UseVisualStyleBackColor = True
        '
        'fraSICNoiseThresholds
        '
        Me.fraSICNoiseThresholds.Controls.Add(Me.cboSICNoiseThresholdMode)
        Me.fraSICNoiseThresholds.Controls.Add(Me.lblNoiseThresholdMode)
        Me.fraSICNoiseThresholds.Controls.Add(Me.txtSICNoiseFractionLowIntensityDataToAverage)
        Me.fraSICNoiseThresholds.Controls.Add(Me.lblSICNoiseFractionLowIntensityDataToAverage)
        Me.fraSICNoiseThresholds.Controls.Add(Me.txtSICNoiseThresholdIntensity)
        Me.fraSICNoiseThresholds.Controls.Add(Me.lblSICNoiseThresholdIntensity)
        Me.fraSICNoiseThresholds.Location = New System.Drawing.Point(19, 9)
        Me.fraSICNoiseThresholds.Name = "fraSICNoiseThresholds"
        Me.fraSICNoiseThresholds.Size = New System.Drawing.Size(384, 148)
        Me.fraSICNoiseThresholds.TabIndex = 0
        Me.fraSICNoiseThresholds.TabStop = False
        Me.fraSICNoiseThresholds.Text = "Initial Noise Threshold Determination for SICs"
        '
        'cboSICNoiseThresholdMode
        '
        Me.cboSICNoiseThresholdMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboSICNoiseThresholdMode.Location = New System.Drawing.Point(144, 28)
        Me.cboSICNoiseThresholdMode.Name = "cboSICNoiseThresholdMode"
        Me.cboSICNoiseThresholdMode.Size = New System.Drawing.Size(211, 24)
        Me.cboSICNoiseThresholdMode.TabIndex = 1
        '
        'lblNoiseThresholdMode
        '
        Me.lblNoiseThresholdMode.Location = New System.Drawing.Point(19, 30)
        Me.lblNoiseThresholdMode.Name = "lblNoiseThresholdMode"
        Me.lblNoiseThresholdMode.Size = New System.Drawing.Size(115, 18)
        Me.lblNoiseThresholdMode.TabIndex = 0
        Me.lblNoiseThresholdMode.Text = "Threshold Mode:"
        '
        'txtSICNoiseFractionLowIntensityDataToAverage
        '
        Me.txtSICNoiseFractionLowIntensityDataToAverage.Location = New System.Drawing.Point(298, 92)
        Me.txtSICNoiseFractionLowIntensityDataToAverage.Name = "txtSICNoiseFractionLowIntensityDataToAverage"
        Me.txtSICNoiseFractionLowIntensityDataToAverage.Size = New System.Drawing.Size(67, 22)
        Me.txtSICNoiseFractionLowIntensityDataToAverage.TabIndex = 5
        Me.txtSICNoiseFractionLowIntensityDataToAverage.Text = "0.75"
        '
        'lblSICNoiseFractionLowIntensityDataToAverage
        '
        Me.lblSICNoiseFractionLowIntensityDataToAverage.Location = New System.Drawing.Point(19, 95)
        Me.lblSICNoiseFractionLowIntensityDataToAverage.Name = "lblSICNoiseFractionLowIntensityDataToAverage"
        Me.lblSICNoiseFractionLowIntensityDataToAverage.Size = New System.Drawing.Size(231, 16)
        Me.lblSICNoiseFractionLowIntensityDataToAverage.TabIndex = 4
        Me.lblSICNoiseFractionLowIntensityDataToAverage.Text = "Fraction low intensity data to average"
        '
        'txtSICNoiseThresholdIntensity
        '
        Me.txtSICNoiseThresholdIntensity.Location = New System.Drawing.Point(298, 65)
        Me.txtSICNoiseThresholdIntensity.Name = "txtSICNoiseThresholdIntensity"
        Me.txtSICNoiseThresholdIntensity.Size = New System.Drawing.Size(67, 22)
        Me.txtSICNoiseThresholdIntensity.TabIndex = 3
        Me.txtSICNoiseThresholdIntensity.Text = "0"
        '
        'lblSICNoiseThresholdIntensity
        '
        Me.lblSICNoiseThresholdIntensity.Location = New System.Drawing.Point(19, 67)
        Me.lblSICNoiseThresholdIntensity.Name = "lblSICNoiseThresholdIntensity"
        Me.lblSICNoiseThresholdIntensity.Size = New System.Drawing.Size(240, 18)
        Me.lblSICNoiseThresholdIntensity.TabIndex = 2
        Me.lblSICNoiseThresholdIntensity.Text = "Absolute Noise Thereshold Intensity"
        '
        'fraSmoothingOptions
        '
        Me.fraSmoothingOptions.Controls.Add(Me.chkSmoothDataRegardlessOfMinimumPeakWidth)
        Me.fraSmoothingOptions.Controls.Add(Me.chkFindPeaksOnSmoothedData)
        Me.fraSmoothingOptions.Controls.Add(Me.optUseSavitzkyGolaySmooth)
        Me.fraSmoothingOptions.Controls.Add(Me.txtButterworthSamplingFrequency)
        Me.fraSmoothingOptions.Controls.Add(Me.lblButterworthSamplingFrequency)
        Me.fraSmoothingOptions.Controls.Add(Me.txtSavitzkyGolayFilterOrder)
        Me.fraSmoothingOptions.Controls.Add(Me.lblSavitzkyGolayFilterOrder)
        Me.fraSmoothingOptions.Controls.Add(Me.optUseButterworthSmooth)
        Me.fraSmoothingOptions.Location = New System.Drawing.Point(422, 9)
        Me.fraSmoothingOptions.Name = "fraSmoothingOptions"
        Me.fraSmoothingOptions.Size = New System.Drawing.Size(298, 259)
        Me.fraSmoothingOptions.TabIndex = 2
        Me.fraSmoothingOptions.TabStop = False
        Me.fraSmoothingOptions.Text = "Smoothing Options"
        '
        'chkSmoothDataRegardlessOfMinimumPeakWidth
        '
        Me.chkSmoothDataRegardlessOfMinimumPeakWidth.Checked = True
        Me.chkSmoothDataRegardlessOfMinimumPeakWidth.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkSmoothDataRegardlessOfMinimumPeakWidth.Location = New System.Drawing.Point(29, 203)
        Me.chkSmoothDataRegardlessOfMinimumPeakWidth.Name = "chkSmoothDataRegardlessOfMinimumPeakWidth"
        Me.chkSmoothDataRegardlessOfMinimumPeakWidth.Size = New System.Drawing.Size(192, 46)
        Me.chkSmoothDataRegardlessOfMinimumPeakWidth.TabIndex = 7
        Me.chkSmoothDataRegardlessOfMinimumPeakWidth.Text = "Smooth Data Regardless Of Minimum Peak Width"
        '
        'chkFindPeaksOnSmoothedData
        '
        Me.chkFindPeaksOnSmoothedData.Checked = True
        Me.chkFindPeaksOnSmoothedData.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkFindPeaksOnSmoothedData.Location = New System.Drawing.Point(29, 175)
        Me.chkFindPeaksOnSmoothedData.Name = "chkFindPeaksOnSmoothedData"
        Me.chkFindPeaksOnSmoothedData.Size = New System.Drawing.Size(249, 19)
        Me.chkFindPeaksOnSmoothedData.TabIndex = 6
        Me.chkFindPeaksOnSmoothedData.Text = "Find Peaks On Smoothed Data"
        '
        'optUseSavitzkyGolaySmooth
        '
        Me.optUseSavitzkyGolaySmooth.Location = New System.Drawing.Point(19, 92)
        Me.optUseSavitzkyGolaySmooth.Name = "optUseSavitzkyGolaySmooth"
        Me.optUseSavitzkyGolaySmooth.Size = New System.Drawing.Size(240, 19)
        Me.optUseSavitzkyGolaySmooth.TabIndex = 3
        Me.optUseSavitzkyGolaySmooth.Text = "Use Savitzky Golay Smooth"
        '
        'txtButterworthSamplingFrequency
        '
        Me.txtButterworthSamplingFrequency.Location = New System.Drawing.Point(134, 55)
        Me.txtButterworthSamplingFrequency.Name = "txtButterworthSamplingFrequency"
        Me.txtButterworthSamplingFrequency.Size = New System.Drawing.Size(58, 22)
        Me.txtButterworthSamplingFrequency.TabIndex = 2
        Me.txtButterworthSamplingFrequency.Text = "0.25"
        '
        'lblButterworthSamplingFrequency
        '
        Me.lblButterworthSamplingFrequency.Location = New System.Drawing.Point(38, 55)
        Me.lblButterworthSamplingFrequency.Name = "lblButterworthSamplingFrequency"
        Me.lblButterworthSamplingFrequency.Size = New System.Drawing.Size(87, 19)
        Me.lblButterworthSamplingFrequency.TabIndex = 1
        Me.lblButterworthSamplingFrequency.Text = "Filter Order"
        '
        'txtSavitzkyGolayFilterOrder
        '
        Me.txtSavitzkyGolayFilterOrder.Location = New System.Drawing.Point(134, 120)
        Me.txtSavitzkyGolayFilterOrder.Name = "txtSavitzkyGolayFilterOrder"
        Me.txtSavitzkyGolayFilterOrder.Size = New System.Drawing.Size(58, 22)
        Me.txtSavitzkyGolayFilterOrder.TabIndex = 5
        Me.txtSavitzkyGolayFilterOrder.Text = "0"
        '
        'lblSavitzkyGolayFilterOrder
        '
        Me.lblSavitzkyGolayFilterOrder.Location = New System.Drawing.Point(38, 120)
        Me.lblSavitzkyGolayFilterOrder.Name = "lblSavitzkyGolayFilterOrder"
        Me.lblSavitzkyGolayFilterOrder.Size = New System.Drawing.Size(87, 18)
        Me.lblSavitzkyGolayFilterOrder.TabIndex = 4
        Me.lblSavitzkyGolayFilterOrder.Text = "Filter Order"
        '
        'optUseButterworthSmooth
        '
        Me.optUseButterworthSmooth.Checked = True
        Me.optUseButterworthSmooth.Location = New System.Drawing.Point(19, 28)
        Me.optUseButterworthSmooth.Name = "optUseButterworthSmooth"
        Me.optUseButterworthSmooth.Size = New System.Drawing.Size(240, 18)
        Me.optUseButterworthSmooth.TabIndex = 0
        Me.optUseButterworthSmooth.TabStop = True
        Me.optUseButterworthSmooth.Text = "Use Butterworth Smooth"
        '
        'fraPeakFindingOptions
        '
        Me.fraPeakFindingOptions.Controls.Add(Me.txtInitialPeakWidthScansMaximum)
        Me.fraPeakFindingOptions.Controls.Add(Me.lblInitialPeakWidthScansMaximum)
        Me.fraPeakFindingOptions.Controls.Add(Me.txtInitialPeakWidthScansScaler)
        Me.fraPeakFindingOptions.Controls.Add(Me.lblInitialPeakWidthScansScaler)
        Me.fraPeakFindingOptions.Controls.Add(Me.txtMaxAllowedUpwardSpikeFractionMax)
        Me.fraPeakFindingOptions.Controls.Add(Me.lblMaxAllowedUpwardSpikeFractionMax)
        Me.fraPeakFindingOptions.Controls.Add(Me.txtMaxDistanceScansNoOverlap)
        Me.fraPeakFindingOptions.Controls.Add(Me.lblMaxDistanceScansNoOverlap)
        Me.fraPeakFindingOptions.Location = New System.Drawing.Point(19, 166)
        Me.fraPeakFindingOptions.Name = "fraPeakFindingOptions"
        Me.fraPeakFindingOptions.Size = New System.Drawing.Size(384, 148)
        Me.fraPeakFindingOptions.TabIndex = 1
        Me.fraPeakFindingOptions.TabStop = False
        Me.fraPeakFindingOptions.Text = "Fine Tuning Peak Finding"
        '
        'txtInitialPeakWidthScansMaximum
        '
        Me.txtInitialPeakWidthScansMaximum.Location = New System.Drawing.Point(288, 111)
        Me.txtInitialPeakWidthScansMaximum.Name = "txtInitialPeakWidthScansMaximum"
        Me.txtInitialPeakWidthScansMaximum.Size = New System.Drawing.Size(67, 22)
        Me.txtInitialPeakWidthScansMaximum.TabIndex = 7
        Me.txtInitialPeakWidthScansMaximum.Text = "30"
        '
        'lblInitialPeakWidthScansMaximum
        '
        Me.lblInitialPeakWidthScansMaximum.Location = New System.Drawing.Point(19, 113)
        Me.lblInitialPeakWidthScansMaximum.Name = "lblInitialPeakWidthScansMaximum"
        Me.lblInitialPeakWidthScansMaximum.Size = New System.Drawing.Size(240, 19)
        Me.lblInitialPeakWidthScansMaximum.TabIndex = 6
        Me.lblInitialPeakWidthScansMaximum.Text = "Initial Peak Width Maximum (Scans)"
        '
        'txtInitialPeakWidthScansScaler
        '
        Me.txtInitialPeakWidthScansScaler.Location = New System.Drawing.Point(288, 83)
        Me.txtInitialPeakWidthScansScaler.Name = "txtInitialPeakWidthScansScaler"
        Me.txtInitialPeakWidthScansScaler.Size = New System.Drawing.Size(67, 22)
        Me.txtInitialPeakWidthScansScaler.TabIndex = 5
        Me.txtInitialPeakWidthScansScaler.Text = "1"
        '
        'lblInitialPeakWidthScansScaler
        '
        Me.lblInitialPeakWidthScansScaler.Location = New System.Drawing.Point(19, 85)
        Me.lblInitialPeakWidthScansScaler.Name = "lblInitialPeakWidthScansScaler"
        Me.lblInitialPeakWidthScansScaler.Size = New System.Drawing.Size(240, 19)
        Me.lblInitialPeakWidthScansScaler.TabIndex = 4
        Me.lblInitialPeakWidthScansScaler.Text = "Initial Peak Width Scaler (Scans)"
        '
        'txtMaxAllowedUpwardSpikeFractionMax
        '
        Me.txtMaxAllowedUpwardSpikeFractionMax.Location = New System.Drawing.Point(288, 55)
        Me.txtMaxAllowedUpwardSpikeFractionMax.Name = "txtMaxAllowedUpwardSpikeFractionMax"
        Me.txtMaxAllowedUpwardSpikeFractionMax.Size = New System.Drawing.Size(67, 22)
        Me.txtMaxAllowedUpwardSpikeFractionMax.TabIndex = 3
        Me.txtMaxAllowedUpwardSpikeFractionMax.Text = "0.2"
        '
        'lblMaxAllowedUpwardSpikeFractionMax
        '
        Me.lblMaxAllowedUpwardSpikeFractionMax.Location = New System.Drawing.Point(19, 58)
        Me.lblMaxAllowedUpwardSpikeFractionMax.Name = "lblMaxAllowedUpwardSpikeFractionMax"
        Me.lblMaxAllowedUpwardSpikeFractionMax.Size = New System.Drawing.Size(279, 18)
        Me.lblMaxAllowedUpwardSpikeFractionMax.TabIndex = 2
        Me.lblMaxAllowedUpwardSpikeFractionMax.Text = "Max Allowed Upward Spike (Fraction Max)"
        '
        'txtMaxDistanceScansNoOverlap
        '
        Me.txtMaxDistanceScansNoOverlap.Location = New System.Drawing.Point(288, 28)
        Me.txtMaxDistanceScansNoOverlap.Name = "txtMaxDistanceScansNoOverlap"
        Me.txtMaxDistanceScansNoOverlap.Size = New System.Drawing.Size(67, 22)
        Me.txtMaxDistanceScansNoOverlap.TabIndex = 1
        Me.txtMaxDistanceScansNoOverlap.Text = "0"
        '
        'lblMaxDistanceScansNoOverlap
        '
        Me.lblMaxDistanceScansNoOverlap.Location = New System.Drawing.Point(19, 30)
        Me.lblMaxDistanceScansNoOverlap.Name = "lblMaxDistanceScansNoOverlap"
        Me.lblMaxDistanceScansNoOverlap.Size = New System.Drawing.Size(240, 18)
        Me.lblMaxDistanceScansNoOverlap.TabIndex = 0
        Me.lblMaxDistanceScansNoOverlap.Text = "Max Distance No Overlap (Scans)"
        '
        'TabPageBinningAndSimilarityOptions
        '
        Me.TabPageBinningAndSimilarityOptions.Controls.Add(Me.fraMassSpectraNoiseThresholds)
        Me.TabPageBinningAndSimilarityOptions.Controls.Add(Me.fraBinningIntensityOptions)
        Me.TabPageBinningAndSimilarityOptions.Controls.Add(Me.fraSpectrumSimilarityOptions)
        Me.TabPageBinningAndSimilarityOptions.Controls.Add(Me.fraBinningMZOptions)
        Me.TabPageBinningAndSimilarityOptions.Location = New System.Drawing.Point(4, 25)
        Me.TabPageBinningAndSimilarityOptions.Name = "TabPageBinningAndSimilarityOptions"
        Me.TabPageBinningAndSimilarityOptions.Size = New System.Drawing.Size(882, 327)
        Me.TabPageBinningAndSimilarityOptions.TabIndex = 6
        Me.TabPageBinningAndSimilarityOptions.Text = "Binning and Similarity"
        Me.TabPageBinningAndSimilarityOptions.UseVisualStyleBackColor = True
        '
        'fraMassSpectraNoiseThresholds
        '
        Me.fraMassSpectraNoiseThresholds.Controls.Add(Me.txtMassSpectraNoiseMinimumSignalToNoiseRatio)
        Me.fraMassSpectraNoiseThresholds.Controls.Add(Me.lblMassSpectraNoiseMinimumSignalToNoiseRatio)
        Me.fraMassSpectraNoiseThresholds.Controls.Add(Me.txtMassSpectraNoiseThresholdIntensity)
        Me.fraMassSpectraNoiseThresholds.Controls.Add(Me.txtMassSpectraNoiseFractionLowIntensityDataToAverage)
        Me.fraMassSpectraNoiseThresholds.Controls.Add(Me.lblMassSpectraNoiseFractionLowIntensityDataToAverage)
        Me.fraMassSpectraNoiseThresholds.Controls.Add(Me.cboMassSpectraNoiseThresholdMode)
        Me.fraMassSpectraNoiseThresholds.Controls.Add(Me.lblMassSpectraNoiseThresholdMode)
        Me.fraMassSpectraNoiseThresholds.Controls.Add(Me.lblMassSpectraNoiseThresholdIntensity)
        Me.fraMassSpectraNoiseThresholds.Location = New System.Drawing.Point(10, 18)
        Me.fraMassSpectraNoiseThresholds.Name = "fraMassSpectraNoiseThresholds"
        Me.fraMassSpectraNoiseThresholds.Size = New System.Drawing.Size(412, 148)
        Me.fraMassSpectraNoiseThresholds.TabIndex = 0
        Me.fraMassSpectraNoiseThresholds.TabStop = False
        Me.fraMassSpectraNoiseThresholds.Text = "Noise Threshold Determination for Mass Spectra"
        '
        'txtMassSpectraNoiseMinimumSignalToNoiseRatio
        '
        Me.txtMassSpectraNoiseMinimumSignalToNoiseRatio.Location = New System.Drawing.Point(250, 120)
        Me.txtMassSpectraNoiseMinimumSignalToNoiseRatio.Name = "txtMassSpectraNoiseMinimumSignalToNoiseRatio"
        Me.txtMassSpectraNoiseMinimumSignalToNoiseRatio.Size = New System.Drawing.Size(67, 22)
        Me.txtMassSpectraNoiseMinimumSignalToNoiseRatio.TabIndex = 9
        Me.txtMassSpectraNoiseMinimumSignalToNoiseRatio.Text = "2"
        '
        'lblMassSpectraNoiseMinimumSignalToNoiseRatio
        '
        Me.lblMassSpectraNoiseMinimumSignalToNoiseRatio.Location = New System.Drawing.Point(10, 120)
        Me.lblMassSpectraNoiseMinimumSignalToNoiseRatio.Name = "lblMassSpectraNoiseMinimumSignalToNoiseRatio"
        Me.lblMassSpectraNoiseMinimumSignalToNoiseRatio.Size = New System.Drawing.Size(230, 16)
        Me.lblMassSpectraNoiseMinimumSignalToNoiseRatio.TabIndex = 8
        Me.lblMassSpectraNoiseMinimumSignalToNoiseRatio.Text = "Minimum Signal to Noise Ratio"
        '
        'txtMassSpectraNoiseThresholdIntensity
        '
        Me.txtMassSpectraNoiseThresholdIntensity.Location = New System.Drawing.Point(250, 65)
        Me.txtMassSpectraNoiseThresholdIntensity.Name = "txtMassSpectraNoiseThresholdIntensity"
        Me.txtMassSpectraNoiseThresholdIntensity.Size = New System.Drawing.Size(67, 22)
        Me.txtMassSpectraNoiseThresholdIntensity.TabIndex = 3
        Me.txtMassSpectraNoiseThresholdIntensity.Text = "0"
        '
        'txtMassSpectraNoiseFractionLowIntensityDataToAverage
        '
        Me.txtMassSpectraNoiseFractionLowIntensityDataToAverage.Location = New System.Drawing.Point(250, 92)
        Me.txtMassSpectraNoiseFractionLowIntensityDataToAverage.Name = "txtMassSpectraNoiseFractionLowIntensityDataToAverage"
        Me.txtMassSpectraNoiseFractionLowIntensityDataToAverage.Size = New System.Drawing.Size(67, 22)
        Me.txtMassSpectraNoiseFractionLowIntensityDataToAverage.TabIndex = 5
        Me.txtMassSpectraNoiseFractionLowIntensityDataToAverage.Text = "0.5"
        '
        'lblMassSpectraNoiseFractionLowIntensityDataToAverage
        '
        Me.lblMassSpectraNoiseFractionLowIntensityDataToAverage.Location = New System.Drawing.Point(10, 92)
        Me.lblMassSpectraNoiseFractionLowIntensityDataToAverage.Name = "lblMassSpectraNoiseFractionLowIntensityDataToAverage"
        Me.lblMassSpectraNoiseFractionLowIntensityDataToAverage.Size = New System.Drawing.Size(230, 26)
        Me.lblMassSpectraNoiseFractionLowIntensityDataToAverage.TabIndex = 4
        Me.lblMassSpectraNoiseFractionLowIntensityDataToAverage.Text = "Fraction low intensity data to average"
        '
        'cboMassSpectraNoiseThresholdMode
        '
        Me.cboMassSpectraNoiseThresholdMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboMassSpectraNoiseThresholdMode.Location = New System.Drawing.Point(163, 28)
        Me.cboMassSpectraNoiseThresholdMode.Name = "cboMassSpectraNoiseThresholdMode"
        Me.cboMassSpectraNoiseThresholdMode.Size = New System.Drawing.Size(211, 24)
        Me.cboMassSpectraNoiseThresholdMode.TabIndex = 1
        '
        'lblMassSpectraNoiseThresholdMode
        '
        Me.lblMassSpectraNoiseThresholdMode.Location = New System.Drawing.Point(10, 37)
        Me.lblMassSpectraNoiseThresholdMode.Name = "lblMassSpectraNoiseThresholdMode"
        Me.lblMassSpectraNoiseThresholdMode.Size = New System.Drawing.Size(163, 18)
        Me.lblMassSpectraNoiseThresholdMode.TabIndex = 0
        Me.lblMassSpectraNoiseThresholdMode.Text = "Noise Threshold Mode:"
        '
        'lblMassSpectraNoiseThresholdIntensity
        '
        Me.lblMassSpectraNoiseThresholdIntensity.Location = New System.Drawing.Point(10, 65)
        Me.lblMassSpectraNoiseThresholdIntensity.Name = "lblMassSpectraNoiseThresholdIntensity"
        Me.lblMassSpectraNoiseThresholdIntensity.Size = New System.Drawing.Size(220, 18)
        Me.lblMassSpectraNoiseThresholdIntensity.TabIndex = 2
        Me.lblMassSpectraNoiseThresholdIntensity.Text = "Absolute Noise Threshold Intensity"
        '
        'fraBinningIntensityOptions
        '
        Me.fraBinningIntensityOptions.Controls.Add(Me.lblBinnedDataIntensityPrecisionPctUnits)
        Me.fraBinningIntensityOptions.Controls.Add(Me.chkBinnedDataSumAllIntensitiesForBin)
        Me.fraBinningIntensityOptions.Controls.Add(Me.chkBinnedDataNormalize)
        Me.fraBinningIntensityOptions.Controls.Add(Me.txtBinnedDataIntensityPrecisionPct)
        Me.fraBinningIntensityOptions.Controls.Add(Me.lblBinnedDataIntensityPrecisionPct)
        Me.fraBinningIntensityOptions.Location = New System.Drawing.Point(442, 175)
        Me.fraBinningIntensityOptions.Name = "fraBinningIntensityOptions"
        Me.fraBinningIntensityOptions.Size = New System.Drawing.Size(288, 120)
        Me.fraBinningIntensityOptions.TabIndex = 3
        Me.fraBinningIntensityOptions.TabStop = False
        Me.fraBinningIntensityOptions.Text = "Binning Intensity Options"
        '
        'lblBinnedDataIntensityPrecisionPctUnits
        '
        Me.lblBinnedDataIntensityPrecisionPctUnits.Location = New System.Drawing.Point(259, 28)
        Me.lblBinnedDataIntensityPrecisionPctUnits.Name = "lblBinnedDataIntensityPrecisionPctUnits"
        Me.lblBinnedDataIntensityPrecisionPctUnits.Size = New System.Drawing.Size(19, 18)
        Me.lblBinnedDataIntensityPrecisionPctUnits.TabIndex = 8
        Me.lblBinnedDataIntensityPrecisionPctUnits.Text = "%"
        '
        'chkBinnedDataSumAllIntensitiesForBin
        '
        Me.chkBinnedDataSumAllIntensitiesForBin.Location = New System.Drawing.Point(10, 92)
        Me.chkBinnedDataSumAllIntensitiesForBin.Name = "chkBinnedDataSumAllIntensitiesForBin"
        Me.chkBinnedDataSumAllIntensitiesForBin.Size = New System.Drawing.Size(192, 19)
        Me.chkBinnedDataSumAllIntensitiesForBin.TabIndex = 10
        Me.chkBinnedDataSumAllIntensitiesForBin.Text = "Sum All Intensities For Bin"
        '
        'chkBinnedDataNormalize
        '
        Me.chkBinnedDataNormalize.Location = New System.Drawing.Point(10, 65)
        Me.chkBinnedDataNormalize.Name = "chkBinnedDataNormalize"
        Me.chkBinnedDataNormalize.Size = New System.Drawing.Size(163, 18)
        Me.chkBinnedDataNormalize.TabIndex = 9
        Me.chkBinnedDataNormalize.Text = "Normalize Intensities"
        '
        'txtBinnedDataIntensityPrecisionPct
        '
        Me.txtBinnedDataIntensityPrecisionPct.Location = New System.Drawing.Point(211, 28)
        Me.txtBinnedDataIntensityPrecisionPct.Name = "txtBinnedDataIntensityPrecisionPct"
        Me.txtBinnedDataIntensityPrecisionPct.Size = New System.Drawing.Size(48, 22)
        Me.txtBinnedDataIntensityPrecisionPct.TabIndex = 7
        Me.txtBinnedDataIntensityPrecisionPct.Text = "1"
        '
        'lblBinnedDataIntensityPrecisionPct
        '
        Me.lblBinnedDataIntensityPrecisionPct.Location = New System.Drawing.Point(10, 30)
        Me.lblBinnedDataIntensityPrecisionPct.Name = "lblBinnedDataIntensityPrecisionPct"
        Me.lblBinnedDataIntensityPrecisionPct.Size = New System.Drawing.Size(201, 18)
        Me.lblBinnedDataIntensityPrecisionPct.TabIndex = 6
        Me.lblBinnedDataIntensityPrecisionPct.Text = "Intensity Precision (0.1 to 100)"
        '
        'fraSpectrumSimilarityOptions
        '
        Me.fraSpectrumSimilarityOptions.Controls.Add(Me.lblSimilarIonTimeToleranceHalfWidthUnits)
        Me.fraSpectrumSimilarityOptions.Controls.Add(Me.txtSpectrumSimilarityMinimum)
        Me.fraSpectrumSimilarityOptions.Controls.Add(Me.lblSpectrumSimilarityMinimum)
        Me.fraSpectrumSimilarityOptions.Controls.Add(Me.txtSimilarIonToleranceHalfWidthMinutes)
        Me.fraSpectrumSimilarityOptions.Controls.Add(Me.lblSimilarIonTimeToleranceHalfWidth)
        Me.fraSpectrumSimilarityOptions.Controls.Add(Me.txtSimilarIonMZToleranceHalfWidth)
        Me.fraSpectrumSimilarityOptions.Controls.Add(Me.lblSimilarIonMZToleranceHalfWidth)
        Me.fraSpectrumSimilarityOptions.Location = New System.Drawing.Point(10, 175)
        Me.fraSpectrumSimilarityOptions.Name = "fraSpectrumSimilarityOptions"
        Me.fraSpectrumSimilarityOptions.Size = New System.Drawing.Size(326, 120)
        Me.fraSpectrumSimilarityOptions.TabIndex = 1
        Me.fraSpectrumSimilarityOptions.TabStop = False
        Me.fraSpectrumSimilarityOptions.Text = "Spectrum Similarity Options"
        '
        'lblSimilarIonTimeToleranceHalfWidthUnits
        '
        Me.lblSimilarIonTimeToleranceHalfWidthUnits.Location = New System.Drawing.Point(259, 58)
        Me.lblSimilarIonTimeToleranceHalfWidthUnits.Name = "lblSimilarIonTimeToleranceHalfWidthUnits"
        Me.lblSimilarIonTimeToleranceHalfWidthUnits.Size = New System.Drawing.Size(58, 18)
        Me.lblSimilarIonTimeToleranceHalfWidthUnits.TabIndex = 6
        Me.lblSimilarIonTimeToleranceHalfWidthUnits.Text = "minutes"
        '
        'txtSpectrumSimilarityMinimum
        '
        Me.txtSpectrumSimilarityMinimum.Location = New System.Drawing.Point(202, 83)
        Me.txtSpectrumSimilarityMinimum.Name = "txtSpectrumSimilarityMinimum"
        Me.txtSpectrumSimilarityMinimum.Size = New System.Drawing.Size(48, 22)
        Me.txtSpectrumSimilarityMinimum.TabIndex = 5
        Me.txtSpectrumSimilarityMinimum.Text = "0.7"
        '
        'lblSpectrumSimilarityMinimum
        '
        Me.lblSpectrumSimilarityMinimum.Location = New System.Drawing.Point(10, 85)
        Me.lblSpectrumSimilarityMinimum.Name = "lblSpectrumSimilarityMinimum"
        Me.lblSpectrumSimilarityMinimum.Size = New System.Drawing.Size(180, 19)
        Me.lblSpectrumSimilarityMinimum.TabIndex = 4
        Me.lblSpectrumSimilarityMinimum.Text = "Minimum Similarity (0 to 1)"
        '
        'txtSimilarIonToleranceHalfWidthMinutes
        '
        Me.txtSimilarIonToleranceHalfWidthMinutes.Location = New System.Drawing.Point(202, 55)
        Me.txtSimilarIonToleranceHalfWidthMinutes.Name = "txtSimilarIonToleranceHalfWidthMinutes"
        Me.txtSimilarIonToleranceHalfWidthMinutes.Size = New System.Drawing.Size(48, 22)
        Me.txtSimilarIonToleranceHalfWidthMinutes.TabIndex = 3
        Me.txtSimilarIonToleranceHalfWidthMinutes.Text = "5"
        '
        'lblSimilarIonTimeToleranceHalfWidth
        '
        Me.lblSimilarIonTimeToleranceHalfWidth.Location = New System.Drawing.Point(10, 58)
        Me.lblSimilarIonTimeToleranceHalfWidth.Name = "lblSimilarIonTimeToleranceHalfWidth"
        Me.lblSimilarIonTimeToleranceHalfWidth.Size = New System.Drawing.Size(180, 18)
        Me.lblSimilarIonTimeToleranceHalfWidth.TabIndex = 2
        Me.lblSimilarIonTimeToleranceHalfWidth.Text = "Time Tolerance Half Width"
        '
        'txtSimilarIonMZToleranceHalfWidth
        '
        Me.txtSimilarIonMZToleranceHalfWidth.Location = New System.Drawing.Point(202, 28)
        Me.txtSimilarIonMZToleranceHalfWidth.Name = "txtSimilarIonMZToleranceHalfWidth"
        Me.txtSimilarIonMZToleranceHalfWidth.Size = New System.Drawing.Size(48, 22)
        Me.txtSimilarIonMZToleranceHalfWidth.TabIndex = 1
        Me.txtSimilarIonMZToleranceHalfWidth.Text = "0.1"
        '
        'lblSimilarIonMZToleranceHalfWidth
        '
        Me.lblSimilarIonMZToleranceHalfWidth.Location = New System.Drawing.Point(10, 30)
        Me.lblSimilarIonMZToleranceHalfWidth.Name = "lblSimilarIonMZToleranceHalfWidth"
        Me.lblSimilarIonMZToleranceHalfWidth.Size = New System.Drawing.Size(180, 18)
        Me.lblSimilarIonMZToleranceHalfWidth.TabIndex = 0
        Me.lblSimilarIonMZToleranceHalfWidth.Text = "m/z Tolerance Half Width"
        '
        'fraBinningMZOptions
        '
        Me.fraBinningMZOptions.Controls.Add(Me.txtMaximumBinCount)
        Me.fraBinningMZOptions.Controls.Add(Me.lblMaximumBinCount)
        Me.fraBinningMZOptions.Controls.Add(Me.txtBinSize)
        Me.fraBinningMZOptions.Controls.Add(Me.lblBinSize)
        Me.fraBinningMZOptions.Controls.Add(Me.txtBinEndX)
        Me.fraBinningMZOptions.Controls.Add(Me.lblBinEndX)
        Me.fraBinningMZOptions.Controls.Add(Me.txtBinStartX)
        Me.fraBinningMZOptions.Controls.Add(Me.lblBinStartX)
        Me.fraBinningMZOptions.Location = New System.Drawing.Point(442, 18)
        Me.fraBinningMZOptions.Name = "fraBinningMZOptions"
        Me.fraBinningMZOptions.Size = New System.Drawing.Size(288, 148)
        Me.fraBinningMZOptions.TabIndex = 2
        Me.fraBinningMZOptions.TabStop = False
        Me.fraBinningMZOptions.Text = "Binning m/z Options"
        '
        'txtMaximumBinCount
        '
        Me.txtMaximumBinCount.Location = New System.Drawing.Point(182, 111)
        Me.txtMaximumBinCount.Name = "txtMaximumBinCount"
        Me.txtMaximumBinCount.Size = New System.Drawing.Size(68, 22)
        Me.txtMaximumBinCount.TabIndex = 7
        Me.txtMaximumBinCount.Text = "100000"
        '
        'lblMaximumBinCount
        '
        Me.lblMaximumBinCount.Location = New System.Drawing.Point(19, 113)
        Me.lblMaximumBinCount.Name = "lblMaximumBinCount"
        Me.lblMaximumBinCount.Size = New System.Drawing.Size(154, 19)
        Me.lblMaximumBinCount.TabIndex = 6
        Me.lblMaximumBinCount.Text = "Maximum Bin Count"
        '
        'txtBinSize
        '
        Me.txtBinSize.Location = New System.Drawing.Point(182, 83)
        Me.txtBinSize.Name = "txtBinSize"
        Me.txtBinSize.Size = New System.Drawing.Size(68, 22)
        Me.txtBinSize.TabIndex = 5
        Me.txtBinSize.Text = "1"
        '
        'lblBinSize
        '
        Me.lblBinSize.Location = New System.Drawing.Point(19, 85)
        Me.lblBinSize.Name = "lblBinSize"
        Me.lblBinSize.Size = New System.Drawing.Size(154, 19)
        Me.lblBinSize.TabIndex = 4
        Me.lblBinSize.Text = "Bin Size (m/z units)"
        '
        'txtBinEndX
        '
        Me.txtBinEndX.Location = New System.Drawing.Point(182, 55)
        Me.txtBinEndX.Name = "txtBinEndX"
        Me.txtBinEndX.Size = New System.Drawing.Size(68, 22)
        Me.txtBinEndX.TabIndex = 3
        Me.txtBinEndX.Text = "2000"
        '
        'lblBinEndX
        '
        Me.lblBinEndX.Location = New System.Drawing.Point(19, 58)
        Me.lblBinEndX.Name = "lblBinEndX"
        Me.lblBinEndX.Size = New System.Drawing.Size(144, 18)
        Me.lblBinEndX.TabIndex = 2
        Me.lblBinEndX.Text = "Bin End m/z"
        '
        'txtBinStartX
        '
        Me.txtBinStartX.Location = New System.Drawing.Point(182, 28)
        Me.txtBinStartX.Name = "txtBinStartX"
        Me.txtBinStartX.Size = New System.Drawing.Size(68, 22)
        Me.txtBinStartX.TabIndex = 1
        Me.txtBinStartX.Text = "50"
        '
        'lblBinStartX
        '
        Me.lblBinStartX.Location = New System.Drawing.Point(19, 30)
        Me.lblBinStartX.Name = "lblBinStartX"
        Me.lblBinStartX.Size = New System.Drawing.Size(144, 18)
        Me.lblBinStartX.TabIndex = 0
        Me.lblBinStartX.Text = "Bin Start m/z"
        '
        'TabPageCustomSICOptions
        '
        Me.TabPageCustomSICOptions.Controls.Add(Me.txtCustomSICFileDescription)
        Me.TabPageCustomSICOptions.Controls.Add(Me.cmdSelectCustomSICFile)
        Me.TabPageCustomSICOptions.Controls.Add(Me.txtCustomSICFileName)
        Me.TabPageCustomSICOptions.Controls.Add(Me.fraCustomSICControls)
        Me.TabPageCustomSICOptions.Controls.Add(Me.dgCustomSICValues)
        Me.TabPageCustomSICOptions.Location = New System.Drawing.Point(4, 25)
        Me.TabPageCustomSICOptions.Name = "TabPageCustomSICOptions"
        Me.TabPageCustomSICOptions.Size = New System.Drawing.Size(882, 327)
        Me.TabPageCustomSICOptions.TabIndex = 3
        Me.TabPageCustomSICOptions.Text = "Custom SIC Options"
        Me.TabPageCustomSICOptions.UseVisualStyleBackColor = True
        '
        'txtCustomSICFileDescription
        '
        Me.txtCustomSICFileDescription.Location = New System.Drawing.Point(10, 6)
        Me.txtCustomSICFileDescription.Multiline = True
        Me.txtCustomSICFileDescription.Name = "txtCustomSICFileDescription"
        Me.txtCustomSICFileDescription.ReadOnly = True
        Me.txtCustomSICFileDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtCustomSICFileDescription.Size = New System.Drawing.Size(582, 59)
        Me.txtCustomSICFileDescription.TabIndex = 0
        Me.txtCustomSICFileDescription.Text = "Custom SIC description ... populated via code."
        '
        'cmdSelectCustomSICFile
        '
        Me.cmdSelectCustomSICFile.Location = New System.Drawing.Point(10, 74)
        Me.cmdSelectCustomSICFile.Name = "cmdSelectCustomSICFile"
        Me.cmdSelectCustomSICFile.Size = New System.Drawing.Size(96, 28)
        Me.cmdSelectCustomSICFile.TabIndex = 1
        Me.cmdSelectCustomSICFile.Text = "&Select File"
        '
        'txtCustomSICFileName
        '
        Me.txtCustomSICFileName.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtCustomSICFileName.Location = New System.Drawing.Point(125, 74)
        Me.txtCustomSICFileName.Name = "txtCustomSICFileName"
        Me.txtCustomSICFileName.Size = New System.Drawing.Size(534, 22)
        Me.txtCustomSICFileName.TabIndex = 2
        '
        'fraCustomSICControls
        '
        Me.fraCustomSICControls.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.fraCustomSICControls.Controls.Add(Me.lblCustomSICToleranceType)
        Me.fraCustomSICControls.Controls.Add(Me.optCustomSICScanToleranceAcqTime)
        Me.fraCustomSICControls.Controls.Add(Me.optCustomSICScanToleranceRelative)
        Me.fraCustomSICControls.Controls.Add(Me.optCustomSICScanToleranceAbsolute)
        Me.fraCustomSICControls.Controls.Add(Me.chkLimitSearchToCustomMZs)
        Me.fraCustomSICControls.Controls.Add(Me.txtCustomSICScanOrAcqTimeTolerance)
        Me.fraCustomSICControls.Controls.Add(Me.lblCustomSICScanTolerance)
        Me.fraCustomSICControls.Controls.Add(Me.cmdPasteCustomSICList)
        Me.fraCustomSICControls.Controls.Add(Me.cmdCustomSICValuesPopulate)
        Me.fraCustomSICControls.Controls.Add(Me.cmdClearCustomSICList)
        Me.fraCustomSICControls.Location = New System.Drawing.Point(666, 9)
        Me.fraCustomSICControls.Name = "fraCustomSICControls"
        Me.fraCustomSICControls.Size = New System.Drawing.Size(201, 305)
        Me.fraCustomSICControls.TabIndex = 4
        Me.fraCustomSICControls.TabStop = False
        '
        'lblCustomSICToleranceType
        '
        Me.lblCustomSICToleranceType.Location = New System.Drawing.Point(6, 145)
        Me.lblCustomSICToleranceType.Name = "lblCustomSICToleranceType"
        Me.lblCustomSICToleranceType.Size = New System.Drawing.Size(106, 19)
        Me.lblCustomSICToleranceType.TabIndex = 5
        Me.lblCustomSICToleranceType.Text = "Tolerance Type:"
        '
        'optCustomSICScanToleranceAcqTime
        '
        Me.optCustomSICScanToleranceAcqTime.AutoSize = True
        Me.optCustomSICScanToleranceAcqTime.Location = New System.Drawing.Point(13, 215)
        Me.optCustomSICScanToleranceAcqTime.Name = "optCustomSICScanToleranceAcqTime"
        Me.optCustomSICScanToleranceAcqTime.Size = New System.Drawing.Size(190, 21)
        Me.optCustomSICScanToleranceAcqTime.TabIndex = 8
        Me.optCustomSICScanToleranceAcqTime.Text = "Acquisition time (minutes)"
        Me.optCustomSICScanToleranceAcqTime.UseVisualStyleBackColor = True
        '
        'optCustomSICScanToleranceRelative
        '
        Me.optCustomSICScanToleranceRelative.AutoSize = True
        Me.optCustomSICScanToleranceRelative.Location = New System.Drawing.Point(13, 190)
        Me.optCustomSICScanToleranceRelative.Name = "optCustomSICScanToleranceRelative"
        Me.optCustomSICScanToleranceRelative.Size = New System.Drawing.Size(160, 21)
        Me.optCustomSICScanToleranceRelative.TabIndex = 7
        Me.optCustomSICScanToleranceRelative.Text = "Relative time (0 to 1)"
        Me.optCustomSICScanToleranceRelative.UseVisualStyleBackColor = True
        '
        'optCustomSICScanToleranceAbsolute
        '
        Me.optCustomSICScanToleranceAbsolute.AutoSize = True
        Me.optCustomSICScanToleranceAbsolute.Checked = True
        Me.optCustomSICScanToleranceAbsolute.Location = New System.Drawing.Point(13, 166)
        Me.optCustomSICScanToleranceAbsolute.Name = "optCustomSICScanToleranceAbsolute"
        Me.optCustomSICScanToleranceAbsolute.Size = New System.Drawing.Size(170, 21)
        Me.optCustomSICScanToleranceAbsolute.TabIndex = 6
        Me.optCustomSICScanToleranceAbsolute.TabStop = True
        Me.optCustomSICScanToleranceAbsolute.Text = "Absolute scan number"
        Me.optCustomSICScanToleranceAbsolute.UseVisualStyleBackColor = True
        '
        'chkLimitSearchToCustomMZs
        '
        Me.chkLimitSearchToCustomMZs.Location = New System.Drawing.Point(10, 249)
        Me.chkLimitSearchToCustomMZs.Name = "chkLimitSearchToCustomMZs"
        Me.chkLimitSearchToCustomMZs.Size = New System.Drawing.Size(182, 51)
        Me.chkLimitSearchToCustomMZs.TabIndex = 9
        Me.chkLimitSearchToCustomMZs.Text = "Limit search to only use custom m/z values (skip auto-fragmented m/z's)"
        '
        'txtCustomSICScanOrAcqTimeTolerance
        '
        Me.txtCustomSICScanOrAcqTimeTolerance.Location = New System.Drawing.Point(119, 114)
        Me.txtCustomSICScanOrAcqTimeTolerance.Name = "txtCustomSICScanOrAcqTimeTolerance"
        Me.txtCustomSICScanOrAcqTimeTolerance.Size = New System.Drawing.Size(67, 22)
        Me.txtCustomSICScanOrAcqTimeTolerance.TabIndex = 4
        Me.txtCustomSICScanOrAcqTimeTolerance.Text = "3"
        '
        'lblCustomSICScanTolerance
        '
        Me.lblCustomSICScanTolerance.Location = New System.Drawing.Point(6, 118)
        Me.lblCustomSICScanTolerance.Name = "lblCustomSICScanTolerance"
        Me.lblCustomSICScanTolerance.Size = New System.Drawing.Size(106, 18)
        Me.lblCustomSICScanTolerance.TabIndex = 3
        Me.lblCustomSICScanTolerance.Text = "Scan Tolerance"
        '
        'cmdPasteCustomSICList
        '
        Me.cmdPasteCustomSICList.Location = New System.Drawing.Point(10, 18)
        Me.cmdPasteCustomSICList.Name = "cmdPasteCustomSICList"
        Me.cmdPasteCustomSICList.Size = New System.Drawing.Size(80, 47)
        Me.cmdPasteCustomSICList.TabIndex = 0
        Me.cmdPasteCustomSICList.Text = "Paste Values"
        '
        'cmdCustomSICValuesPopulate
        '
        Me.cmdCustomSICValuesPopulate.Location = New System.Drawing.Point(7, 72)
        Me.cmdCustomSICValuesPopulate.Name = "cmdCustomSICValuesPopulate"
        Me.cmdCustomSICValuesPopulate.Size = New System.Drawing.Size(183, 27)
        Me.cmdCustomSICValuesPopulate.TabIndex = 2
        Me.cmdCustomSICValuesPopulate.Text = "Auto-Populate with Defaults"
        '
        'cmdClearCustomSICList
        '
        Me.cmdClearCustomSICList.Location = New System.Drawing.Point(107, 18)
        Me.cmdClearCustomSICList.Name = "cmdClearCustomSICList"
        Me.cmdClearCustomSICList.Size = New System.Drawing.Size(77, 47)
        Me.cmdClearCustomSICList.TabIndex = 1
        Me.cmdClearCustomSICList.Text = "Clear List"
        '
        'dgCustomSICValues
        '
        Me.dgCustomSICValues.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.dgCustomSICValues.CaptionText = "Custom SIC Values"
        Me.dgCustomSICValues.DataMember = ""
        Me.dgCustomSICValues.HeaderForeColor = System.Drawing.SystemColors.ControlText
        Me.dgCustomSICValues.Location = New System.Drawing.Point(10, 120)
        Me.dgCustomSICValues.Name = "dgCustomSICValues"
        Me.dgCustomSICValues.Size = New System.Drawing.Size(649, 194)
        Me.dgCustomSICValues.TabIndex = 3
        '
        'TabPageReporterIons
        '
        Me.TabPageReporterIons.Controls.Add(Me.fraDecoyOptions)
        Me.TabPageReporterIons.Controls.Add(Me.fraMRMOptions)
        Me.TabPageReporterIons.Controls.Add(Me.fraReporterIonMassMode)
        Me.TabPageReporterIons.Controls.Add(Me.fraReporterIonOptions)
        Me.TabPageReporterIons.Location = New System.Drawing.Point(4, 25)
        Me.TabPageReporterIons.Name = "TabPageReporterIons"
        Me.TabPageReporterIons.Size = New System.Drawing.Size(882, 327)
        Me.TabPageReporterIons.TabIndex = 9
        Me.TabPageReporterIons.Text = "Reporter Ions / MRM"
        Me.TabPageReporterIons.UseVisualStyleBackColor = True
        '
        'fraDecoyOptions
        '
        Me.fraDecoyOptions.Controls.Add(Me.lblParentIonDecoyMassDaUnits)
        Me.fraDecoyOptions.Controls.Add(Me.txtParentIonDecoyMassDa)
        Me.fraDecoyOptions.Controls.Add(Me.lblParentIonDecoyMassDa)
        Me.fraDecoyOptions.Location = New System.Drawing.Point(638, 181)
        Me.fraDecoyOptions.Name = "fraDecoyOptions"
        Me.fraDecoyOptions.Size = New System.Drawing.Size(191, 86)
        Me.fraDecoyOptions.TabIndex = 3
        Me.fraDecoyOptions.TabStop = False
        Me.fraDecoyOptions.Text = "Decoy Options"
        '
        'lblParentIonDecoyMassDaUnits
        '
        Me.lblParentIonDecoyMassDaUnits.Location = New System.Drawing.Point(86, 52)
        Me.lblParentIonDecoyMassDaUnits.Name = "lblParentIonDecoyMassDaUnits"
        Me.lblParentIonDecoyMassDaUnits.Size = New System.Drawing.Size(41, 20)
        Me.lblParentIonDecoyMassDaUnits.TabIndex = 2
        Me.lblParentIonDecoyMassDaUnits.Text = "Da"
        '
        'txtParentIonDecoyMassDa
        '
        Me.txtParentIonDecoyMassDa.Location = New System.Drawing.Point(12, 48)
        Me.txtParentIonDecoyMassDa.Name = "txtParentIonDecoyMassDa"
        Me.txtParentIonDecoyMassDa.Size = New System.Drawing.Size(67, 22)
        Me.txtParentIonDecoyMassDa.TabIndex = 1
        Me.txtParentIonDecoyMassDa.Text = "0"
        '
        'lblParentIonDecoyMassDa
        '
        Me.lblParentIonDecoyMassDa.Location = New System.Drawing.Point(8, 27)
        Me.lblParentIonDecoyMassDa.Name = "lblParentIonDecoyMassDa"
        Me.lblParentIonDecoyMassDa.Size = New System.Drawing.Size(170, 18)
        Me.lblParentIonDecoyMassDa.TabIndex = 0
        Me.lblParentIonDecoyMassDa.Text = "Parent Ion Decoy Mass"
        '
        'fraMRMOptions
        '
        Me.fraMRMOptions.Controls.Add(Me.chkMRMWriteIntensityCrosstab)
        Me.fraMRMOptions.Controls.Add(Me.lblMRMInfo)
        Me.fraMRMOptions.Controls.Add(Me.chkMRMWriteDataList)
        Me.fraMRMOptions.Location = New System.Drawing.Point(379, 18)
        Me.fraMRMOptions.Name = "fraMRMOptions"
        Me.fraMRMOptions.Size = New System.Drawing.Size(409, 156)
        Me.fraMRMOptions.TabIndex = 2
        Me.fraMRMOptions.TabStop = False
        Me.fraMRMOptions.Text = "MRM Options"
        '
        'chkMRMWriteIntensityCrosstab
        '
        Me.chkMRMWriteIntensityCrosstab.Location = New System.Drawing.Point(23, 120)
        Me.chkMRMWriteIntensityCrosstab.Name = "chkMRMWriteIntensityCrosstab"
        Me.chkMRMWriteIntensityCrosstab.Size = New System.Drawing.Size(366, 21)
        Me.chkMRMWriteIntensityCrosstab.TabIndex = 2
        Me.chkMRMWriteIntensityCrosstab.Text = "Save MRM intensity crosstab (wide, rectangular file)"
        '
        'lblMRMInfo
        '
        Me.lblMRMInfo.Location = New System.Drawing.Point(7, 18)
        Me.lblMRMInfo.Name = "lblMRMInfo"
        Me.lblMRMInfo.Size = New System.Drawing.Size(395, 71)
        Me.lblMRMInfo.TabIndex = 0
        Me.lblMRMInfo.Text = resources.GetString("lblMRMInfo.Text")
        '
        'chkMRMWriteDataList
        '
        Me.chkMRMWriteDataList.Location = New System.Drawing.Point(23, 92)
        Me.chkMRMWriteDataList.Name = "chkMRMWriteDataList"
        Me.chkMRMWriteDataList.Size = New System.Drawing.Size(366, 21)
        Me.chkMRMWriteDataList.TabIndex = 1
        Me.chkMRMWriteDataList.Text = "Save MRM data list (long, narrow file)"
        '
        'fraReporterIonMassMode
        '
        Me.fraReporterIonMassMode.Controls.Add(Me.cboReporterIonMassMode)
        Me.fraReporterIonMassMode.Location = New System.Drawing.Point(19, 181)
        Me.fraReporterIonMassMode.Name = "fraReporterIonMassMode"
        Me.fraReporterIonMassMode.Size = New System.Drawing.Size(612, 86)
        Me.fraReporterIonMassMode.TabIndex = 1
        Me.fraReporterIonMassMode.TabStop = False
        Me.fraReporterIonMassMode.Text = "Reporter Ion Mass Mode"
        '
        'cboReporterIonMassMode
        '
        Me.cboReporterIonMassMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboReporterIonMassMode.Location = New System.Drawing.Point(9, 27)
        Me.cboReporterIonMassMode.Name = "cboReporterIonMassMode"
        Me.cboReporterIonMassMode.Size = New System.Drawing.Size(592, 24)
        Me.cboReporterIonMassMode.TabIndex = 13
        '
        'fraReporterIonOptions
        '
        Me.fraReporterIonOptions.Controls.Add(Me.chkReporterIonApplyAbundanceCorrection)
        Me.fraReporterIonOptions.Controls.Add(Me.chkReporterIonSaveUncorrectedIntensities)
        Me.fraReporterIonOptions.Controls.Add(Me.chkReporterIonSaveObservedMasses)
        Me.fraReporterIonOptions.Controls.Add(Me.txtReporterIonMZToleranceDa)
        Me.fraReporterIonOptions.Controls.Add(Me.lblReporterIonMZToleranceDa)
        Me.fraReporterIonOptions.Controls.Add(Me.chkReporterIonStatsEnabled)
        Me.fraReporterIonOptions.Location = New System.Drawing.Point(19, 18)
        Me.fraReporterIonOptions.Name = "fraReporterIonOptions"
        Me.fraReporterIonOptions.Size = New System.Drawing.Size(339, 156)
        Me.fraReporterIonOptions.TabIndex = 0
        Me.fraReporterIonOptions.TabStop = False
        Me.fraReporterIonOptions.Text = "Reporter Ion Options"
        '
        'chkReporterIonApplyAbundanceCorrection
        '
        Me.chkReporterIonApplyAbundanceCorrection.Location = New System.Drawing.Point(19, 103)
        Me.chkReporterIonApplyAbundanceCorrection.Name = "chkReporterIonApplyAbundanceCorrection"
        Me.chkReporterIonApplyAbundanceCorrection.Size = New System.Drawing.Size(301, 20)
        Me.chkReporterIonApplyAbundanceCorrection.TabIndex = 4
        Me.chkReporterIonApplyAbundanceCorrection.Text = "Apply iTraq isotopic abundance correction"
        '
        'chkReporterIonSaveUncorrectedIntensities
        '
        Me.chkReporterIonSaveUncorrectedIntensities.Location = New System.Drawing.Point(38, 127)
        Me.chkReporterIonSaveUncorrectedIntensities.Name = "chkReporterIonSaveUncorrectedIntensities"
        Me.chkReporterIonSaveUncorrectedIntensities.Size = New System.Drawing.Size(269, 21)
        Me.chkReporterIonSaveUncorrectedIntensities.TabIndex = 5
        Me.chkReporterIonSaveUncorrectedIntensities.Text = "Write original uncorrected intensities"
        '
        'chkReporterIonSaveObservedMasses
        '
        Me.chkReporterIonSaveObservedMasses.Location = New System.Drawing.Point(19, 78)
        Me.chkReporterIonSaveObservedMasses.Name = "chkReporterIonSaveObservedMasses"
        Me.chkReporterIonSaveObservedMasses.Size = New System.Drawing.Size(301, 21)
        Me.chkReporterIonSaveObservedMasses.TabIndex = 3
        Me.chkReporterIonSaveObservedMasses.Text = "Write observed m/z values to Reporter Ions file"
        '
        'txtReporterIonMZToleranceDa
        '
        Me.txtReporterIonMZToleranceDa.Location = New System.Drawing.Point(205, 48)
        Me.txtReporterIonMZToleranceDa.Name = "txtReporterIonMZToleranceDa"
        Me.txtReporterIonMZToleranceDa.Size = New System.Drawing.Size(48, 22)
        Me.txtReporterIonMZToleranceDa.TabIndex = 2
        Me.txtReporterIonMZToleranceDa.Text = "0.5"
        '
        'lblReporterIonMZToleranceDa
        '
        Me.lblReporterIonMZToleranceDa.Location = New System.Drawing.Point(16, 52)
        Me.lblReporterIonMZToleranceDa.Name = "lblReporterIonMZToleranceDa"
        Me.lblReporterIonMZToleranceDa.Size = New System.Drawing.Size(182, 18)
        Me.lblReporterIonMZToleranceDa.TabIndex = 1
        Me.lblReporterIonMZToleranceDa.Text = "m/z Tolerance Half Width"
        '
        'chkReporterIonStatsEnabled
        '
        Me.chkReporterIonStatsEnabled.Location = New System.Drawing.Point(19, 28)
        Me.chkReporterIonStatsEnabled.Name = "chkReporterIonStatsEnabled"
        Me.chkReporterIonStatsEnabled.Size = New System.Drawing.Size(240, 20)
        Me.chkReporterIonStatsEnabled.TabIndex = 0
        Me.chkReporterIonStatsEnabled.Text = "Generate Reporter Ion Stats"
        '
        'TabPageAdvancedOptions
        '
        Me.TabPageAdvancedOptions.Controls.Add(Me.fraAdditionalInfoFiles)
        Me.TabPageAdvancedOptions.Controls.Add(Me.fraDatasetLookupInfo)
        Me.TabPageAdvancedOptions.Controls.Add(Me.fraMemoryConservationOptions)
        Me.TabPageAdvancedOptions.Location = New System.Drawing.Point(4, 25)
        Me.TabPageAdvancedOptions.Name = "TabPageAdvancedOptions"
        Me.TabPageAdvancedOptions.Size = New System.Drawing.Size(882, 327)
        Me.TabPageAdvancedOptions.TabIndex = 8
        Me.TabPageAdvancedOptions.Text = "Advanced"
        Me.TabPageAdvancedOptions.UseVisualStyleBackColor = True
        '
        'fraAdditionalInfoFiles
        '
        Me.fraAdditionalInfoFiles.Controls.Add(Me.chkConsolidateConstantExtendedHeaderValues)
        Me.fraAdditionalInfoFiles.Controls.Add(Me.lblStatusLogKeyNameFilterList)
        Me.fraAdditionalInfoFiles.Controls.Add(Me.txtStatusLogKeyNameFilterList)
        Me.fraAdditionalInfoFiles.Controls.Add(Me.chkSaveExtendedStatsFileIncludeStatusLog)
        Me.fraAdditionalInfoFiles.Controls.Add(Me.chkSaveExtendedStatsFileIncludeFilterText)
        Me.fraAdditionalInfoFiles.Controls.Add(Me.chkSaveMSTuneFile)
        Me.fraAdditionalInfoFiles.Controls.Add(Me.chkSaveMSMethodFile)
        Me.fraAdditionalInfoFiles.Controls.Add(Me.chkSaveExtendedStatsFile)
        Me.fraAdditionalInfoFiles.Location = New System.Drawing.Point(382, 3)
        Me.fraAdditionalInfoFiles.Name = "fraAdditionalInfoFiles"
        Me.fraAdditionalInfoFiles.Size = New System.Drawing.Size(422, 135)
        Me.fraAdditionalInfoFiles.TabIndex = 1
        Me.fraAdditionalInfoFiles.TabStop = False
        Me.fraAdditionalInfoFiles.Text = "Thermo Finnigan Info Files"
        '
        'chkConsolidateConstantExtendedHeaderValues
        '
        Me.chkConsolidateConstantExtendedHeaderValues.Checked = True
        Me.chkConsolidateConstantExtendedHeaderValues.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkConsolidateConstantExtendedHeaderValues.Location = New System.Drawing.Point(38, 112)
        Me.chkConsolidateConstantExtendedHeaderValues.Name = "chkConsolidateConstantExtendedHeaderValues"
        Me.chkConsolidateConstantExtendedHeaderValues.Size = New System.Drawing.Size(192, 21)
        Me.chkConsolidateConstantExtendedHeaderValues.TabIndex = 5
        Me.chkConsolidateConstantExtendedHeaderValues.Text = "Consolidate constant values"
        '
        'lblStatusLogKeyNameFilterList
        '
        Me.lblStatusLogKeyNameFilterList.Location = New System.Drawing.Point(230, 31)
        Me.lblStatusLogKeyNameFilterList.Name = "lblStatusLogKeyNameFilterList"
        Me.lblStatusLogKeyNameFilterList.Size = New System.Drawing.Size(176, 20)
        Me.lblStatusLogKeyNameFilterList.TabIndex = 6
        Me.lblStatusLogKeyNameFilterList.Text = "Status Log Keys to Include"
        '
        'txtStatusLogKeyNameFilterList
        '
        Me.txtStatusLogKeyNameFilterList.Location = New System.Drawing.Point(234, 54)
        Me.txtStatusLogKeyNameFilterList.Multiline = True
        Me.txtStatusLogKeyNameFilterList.Name = "txtStatusLogKeyNameFilterList"
        Me.txtStatusLogKeyNameFilterList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtStatusLogKeyNameFilterList.Size = New System.Drawing.Size(179, 58)
        Me.txtStatusLogKeyNameFilterList.TabIndex = 7
        '
        'chkSaveExtendedStatsFileIncludeStatusLog
        '
        Me.chkSaveExtendedStatsFileIncludeStatusLog.Location = New System.Drawing.Point(38, 92)
        Me.chkSaveExtendedStatsFileIncludeStatusLog.Name = "chkSaveExtendedStatsFileIncludeStatusLog"
        Me.chkSaveExtendedStatsFileIncludeStatusLog.Size = New System.Drawing.Size(192, 21)
        Me.chkSaveExtendedStatsFileIncludeStatusLog.TabIndex = 4
        Me.chkSaveExtendedStatsFileIncludeStatusLog.Text = "Include voltage, temp., etc."
        '
        'chkSaveExtendedStatsFileIncludeFilterText
        '
        Me.chkSaveExtendedStatsFileIncludeFilterText.Checked = True
        Me.chkSaveExtendedStatsFileIncludeFilterText.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkSaveExtendedStatsFileIncludeFilterText.Location = New System.Drawing.Point(38, 74)
        Me.chkSaveExtendedStatsFileIncludeFilterText.Name = "chkSaveExtendedStatsFileIncludeFilterText"
        Me.chkSaveExtendedStatsFileIncludeFilterText.Size = New System.Drawing.Size(192, 18)
        Me.chkSaveExtendedStatsFileIncludeFilterText.TabIndex = 3
        Me.chkSaveExtendedStatsFileIncludeFilterText.Text = "Include Scan Filter Text"
        '
        'chkSaveMSTuneFile
        '
        Me.chkSaveMSTuneFile.Location = New System.Drawing.Point(19, 37)
        Me.chkSaveMSTuneFile.Name = "chkSaveMSTuneFile"
        Me.chkSaveMSTuneFile.Size = New System.Drawing.Size(211, 18)
        Me.chkSaveMSTuneFile.TabIndex = 1
        Me.chkSaveMSTuneFile.Text = "Save MS Tune File"
        '
        'chkSaveMSMethodFile
        '
        Me.chkSaveMSMethodFile.Checked = True
        Me.chkSaveMSMethodFile.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkSaveMSMethodFile.Location = New System.Drawing.Point(19, 18)
        Me.chkSaveMSMethodFile.Name = "chkSaveMSMethodFile"
        Me.chkSaveMSMethodFile.Size = New System.Drawing.Size(211, 19)
        Me.chkSaveMSMethodFile.TabIndex = 0
        Me.chkSaveMSMethodFile.Text = "Save MS Method File"
        '
        'chkSaveExtendedStatsFile
        '
        Me.chkSaveExtendedStatsFile.Checked = True
        Me.chkSaveExtendedStatsFile.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkSaveExtendedStatsFile.Location = New System.Drawing.Point(19, 55)
        Me.chkSaveExtendedStatsFile.Name = "chkSaveExtendedStatsFile"
        Me.chkSaveExtendedStatsFile.Size = New System.Drawing.Size(211, 19)
        Me.chkSaveExtendedStatsFile.TabIndex = 2
        Me.chkSaveExtendedStatsFile.Text = "Save Extended Stats File"
        '
        'fraDatasetLookupInfo
        '
        Me.fraDatasetLookupInfo.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.fraDatasetLookupInfo.Controls.Add(Me.cmdSetConnectionStringToPNNLServer)
        Me.fraDatasetLookupInfo.Controls.Add(Me.txtDatasetInfoQuerySQL)
        Me.fraDatasetLookupInfo.Controls.Add(Me.lblDatasetInfoQuerySQL)
        Me.fraDatasetLookupInfo.Controls.Add(Me.txtDatabaseConnectionString)
        Me.fraDatasetLookupInfo.Controls.Add(Me.lblDatabaseConnectionString)
        Me.fraDatasetLookupInfo.Controls.Add(Me.lblDatasetLookupFilePath)
        Me.fraDatasetLookupInfo.Controls.Add(Me.cmdSelectDatasetLookupFile)
        Me.fraDatasetLookupInfo.Controls.Add(Me.txtDatasetLookupFilePath)
        Me.fraDatasetLookupInfo.Location = New System.Drawing.Point(19, 138)
        Me.fraDatasetLookupInfo.Name = "fraDatasetLookupInfo"
        Me.fraDatasetLookupInfo.Size = New System.Drawing.Size(852, 176)
        Me.fraDatasetLookupInfo.TabIndex = 1
        Me.fraDatasetLookupInfo.TabStop = False
        Me.fraDatasetLookupInfo.Text = "Dataset ID lookup information"
        '
        'cmdSetConnectionStringToPNNLServer
        '
        Me.cmdSetConnectionStringToPNNLServer.Location = New System.Drawing.Point(19, 23)
        Me.cmdSetConnectionStringToPNNLServer.Name = "cmdSetConnectionStringToPNNLServer"
        Me.cmdSetConnectionStringToPNNLServer.Size = New System.Drawing.Size(144, 23)
        Me.cmdSetConnectionStringToPNNLServer.TabIndex = 0
        Me.cmdSetConnectionStringToPNNLServer.Text = "Set to PNNL Server"
        '
        'txtDatasetInfoQuerySQL
        '
        Me.txtDatasetInfoQuerySQL.Location = New System.Drawing.Point(202, 74)
        Me.txtDatasetInfoQuerySQL.Name = "txtDatasetInfoQuerySQL"
        Me.txtDatasetInfoQuerySQL.Size = New System.Drawing.Size(499, 22)
        Me.txtDatasetInfoQuerySQL.TabIndex = 4
        '
        'lblDatasetInfoQuerySQL
        '
        Me.lblDatasetInfoQuerySQL.Location = New System.Drawing.Point(10, 80)
        Me.lblDatasetInfoQuerySQL.Name = "lblDatasetInfoQuerySQL"
        Me.lblDatasetInfoQuerySQL.Size = New System.Drawing.Size(192, 18)
        Me.lblDatasetInfoQuerySQL.TabIndex = 3
        Me.lblDatasetInfoQuerySQL.Text = "Dataset Info Query SQL:"
        '
        'txtDatabaseConnectionString
        '
        Me.txtDatabaseConnectionString.Location = New System.Drawing.Point(202, 46)
        Me.txtDatabaseConnectionString.Name = "txtDatabaseConnectionString"
        Me.txtDatabaseConnectionString.Size = New System.Drawing.Size(499, 22)
        Me.txtDatabaseConnectionString.TabIndex = 2
        '
        'lblDatabaseConnectionString
        '
        Me.lblDatabaseConnectionString.Location = New System.Drawing.Point(10, 52)
        Me.lblDatabaseConnectionString.Name = "lblDatabaseConnectionString"
        Me.lblDatabaseConnectionString.Size = New System.Drawing.Size(192, 18)
        Me.lblDatabaseConnectionString.TabIndex = 1
        Me.lblDatabaseConnectionString.Text = "SQL Server Connection String:"
        '
        'lblDatasetLookupFilePath
        '
        Me.lblDatasetLookupFilePath.Location = New System.Drawing.Point(10, 111)
        Me.lblDatasetLookupFilePath.Name = "lblDatasetLookupFilePath"
        Me.lblDatasetLookupFilePath.Size = New System.Drawing.Size(633, 18)
        Me.lblDatasetLookupFilePath.TabIndex = 5
        Me.lblDatasetLookupFilePath.Text = "Dataset lookup file (dataset name and dataset ID number, tab-separated); used if " &
    "DB not available"
        '
        'cmdSelectDatasetLookupFile
        '
        Me.cmdSelectDatasetLookupFile.Location = New System.Drawing.Point(10, 138)
        Me.cmdSelectDatasetLookupFile.Name = "cmdSelectDatasetLookupFile"
        Me.cmdSelectDatasetLookupFile.Size = New System.Drawing.Size(96, 28)
        Me.cmdSelectDatasetLookupFile.TabIndex = 6
        Me.cmdSelectDatasetLookupFile.Text = "Select File"
        '
        'txtDatasetLookupFilePath
        '
        Me.txtDatasetLookupFilePath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtDatasetLookupFilePath.Location = New System.Drawing.Point(125, 138)
        Me.txtDatasetLookupFilePath.Name = "txtDatasetLookupFilePath"
        Me.txtDatasetLookupFilePath.Size = New System.Drawing.Size(708, 22)
        Me.txtDatasetLookupFilePath.TabIndex = 7
        '
        'fraMemoryConservationOptions
        '
        Me.fraMemoryConservationOptions.Controls.Add(Me.chkSkipMSMSProcessing)
        Me.fraMemoryConservationOptions.Controls.Add(Me.chkSkipSICAndRawDataProcessing)
        Me.fraMemoryConservationOptions.Controls.Add(Me.chkExportRawDataOnly)
        Me.fraMemoryConservationOptions.Location = New System.Drawing.Point(19, 18)
        Me.fraMemoryConservationOptions.Name = "fraMemoryConservationOptions"
        Me.fraMemoryConservationOptions.Size = New System.Drawing.Size(355, 120)
        Me.fraMemoryConservationOptions.TabIndex = 0
        Me.fraMemoryConservationOptions.TabStop = False
        Me.fraMemoryConservationOptions.Text = "Memory Conservation and Processing Speed Options"
        '
        'chkSkipMSMSProcessing
        '
        Me.chkSkipMSMSProcessing.Location = New System.Drawing.Point(19, 22)
        Me.chkSkipMSMSProcessing.Name = "chkSkipMSMSProcessing"
        Me.chkSkipMSMSProcessing.Size = New System.Drawing.Size(317, 21)
        Me.chkSkipMSMSProcessing.TabIndex = 0
        Me.chkSkipMSMSProcessing.Text = "Skip MS/MS Processing (no similarity testing)"
        '
        'chkSkipSICAndRawDataProcessing
        '
        Me.chkSkipSICAndRawDataProcessing.Location = New System.Drawing.Point(19, 46)
        Me.chkSkipSICAndRawDataProcessing.Name = "chkSkipSICAndRawDataProcessing"
        Me.chkSkipSICAndRawDataProcessing.Size = New System.Drawing.Size(261, 39)
        Me.chkSkipSICAndRawDataProcessing.TabIndex = 1
        Me.chkSkipSICAndRawDataProcessing.Text = "Only Export Chromatograms and Scan Stats (no SICs or raw data)"
        '
        'chkExportRawDataOnly
        '
        Me.chkExportRawDataOnly.Location = New System.Drawing.Point(19, 91)
        Me.chkExportRawDataOnly.Name = "chkExportRawDataOnly"
        Me.chkExportRawDataOnly.Size = New System.Drawing.Size(240, 21)
        Me.chkExportRawDataOnly.TabIndex = 2
        Me.chkExportRawDataOnly.Text = "Export Raw Data Only (No SICs)"
        '
        'fraOutputFolderPath
        '
        Me.fraOutputFolderPath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.fraOutputFolderPath.Controls.Add(Me.cmdStartProcessing)
        Me.fraOutputFolderPath.Controls.Add(Me.cmdSelectOutputFolder)
        Me.fraOutputFolderPath.Controls.Add(Me.txtOutputFolderPath)
        Me.fraOutputFolderPath.Location = New System.Drawing.Point(10, 92)
        Me.fraOutputFolderPath.Name = "fraOutputFolderPath"
        Me.fraOutputFolderPath.Size = New System.Drawing.Size(885, 102)
        Me.fraOutputFolderPath.TabIndex = 1
        Me.fraOutputFolderPath.TabStop = False
        Me.fraOutputFolderPath.Text = "Output Folder Path"
        '
        'cmdStartProcessing
        '
        Me.cmdStartProcessing.Location = New System.Drawing.Point(326, 65)
        Me.cmdStartProcessing.Name = "cmdStartProcessing"
        Me.cmdStartProcessing.Size = New System.Drawing.Size(133, 27)
        Me.cmdStartProcessing.TabIndex = 2
        Me.cmdStartProcessing.Text = "Start &Processing"
        '
        'cmdSelectOutputFolder
        '
        Me.cmdSelectOutputFolder.Location = New System.Drawing.Point(10, 28)
        Me.cmdSelectOutputFolder.Name = "cmdSelectOutputFolder"
        Me.cmdSelectOutputFolder.Size = New System.Drawing.Size(96, 44)
        Me.cmdSelectOutputFolder.TabIndex = 0
        Me.cmdSelectOutputFolder.Text = "Select F&older"
        '
        'txtOutputFolderPath
        '
        Me.txtOutputFolderPath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtOutputFolderPath.Location = New System.Drawing.Point(125, 30)
        Me.txtOutputFolderPath.Name = "txtOutputFolderPath"
        Me.txtOutputFolderPath.Size = New System.Drawing.Size(741, 22)
        Me.txtOutputFolderPath.TabIndex = 1
        '
        'frmMain
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(6, 15)
        Me.ClientSize = New System.Drawing.Size(904, 585)
        Me.Controls.Add(Me.fraOutputFolderPath)
        Me.Controls.Add(Me.tbsOptions)
        Me.Controls.Add(Me.fraInputFilePath)
        Me.Menu = Me.MainMenuControl
        Me.MinimumSize = New System.Drawing.Size(540, 0)
        Me.Name = "frmMain"
        Me.Text = "MASIC"
        Me.fraInputFilePath.ResumeLayout(False)
        Me.fraInputFilePath.PerformLayout()
        Me.tbsOptions.ResumeLayout(False)
        Me.TabPageMasicExportOptions.ResumeLayout(False)
        Me.TabPageMasicExportOptions.PerformLayout()
        Me.fraExportAllSpectraDataPoints.ResumeLayout(False)
        Me.fraExportAllSpectraDataPoints.PerformLayout()
        Me.TabPageSICOptions.ResumeLayout(False)
        Me.fraInputFileRangeFilters.ResumeLayout(False)
        Me.fraInputFileRangeFilters.PerformLayout()
        Me.fraSICSearchThresholds.ResumeLayout(False)
        Me.fraSICSearchThresholds.PerformLayout()
        Me.TabPagePeakFindingOptions.ResumeLayout(False)
        Me.fraSICNoiseThresholds.ResumeLayout(False)
        Me.fraSICNoiseThresholds.PerformLayout()
        Me.fraSmoothingOptions.ResumeLayout(False)
        Me.fraSmoothingOptions.PerformLayout()
        Me.fraPeakFindingOptions.ResumeLayout(False)
        Me.fraPeakFindingOptions.PerformLayout()
        Me.TabPageBinningAndSimilarityOptions.ResumeLayout(False)
        Me.fraMassSpectraNoiseThresholds.ResumeLayout(False)
        Me.fraMassSpectraNoiseThresholds.PerformLayout()
        Me.fraBinningIntensityOptions.ResumeLayout(False)
        Me.fraBinningIntensityOptions.PerformLayout()
        Me.fraSpectrumSimilarityOptions.ResumeLayout(False)
        Me.fraSpectrumSimilarityOptions.PerformLayout()
        Me.fraBinningMZOptions.ResumeLayout(False)
        Me.fraBinningMZOptions.PerformLayout()
        Me.TabPageCustomSICOptions.ResumeLayout(False)
        Me.TabPageCustomSICOptions.PerformLayout()
        Me.fraCustomSICControls.ResumeLayout(False)
        Me.fraCustomSICControls.PerformLayout()
        CType(Me.dgCustomSICValues, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TabPageReporterIons.ResumeLayout(False)
        Me.fraDecoyOptions.ResumeLayout(False)
        Me.fraDecoyOptions.PerformLayout()
        Me.fraMRMOptions.ResumeLayout(False)
        Me.fraReporterIonMassMode.ResumeLayout(False)
        Me.fraReporterIonOptions.ResumeLayout(False)
        Me.fraReporterIonOptions.PerformLayout()
        Me.TabPageAdvancedOptions.ResumeLayout(False)
        Me.fraAdditionalInfoFiles.ResumeLayout(False)
        Me.fraAdditionalInfoFiles.PerformLayout()
        Me.fraDatasetLookupInfo.ResumeLayout(False)
        Me.fraDatasetLookupInfo.PerformLayout()
        Me.fraMemoryConservationOptions.ResumeLayout(False)
        Me.fraOutputFolderPath.ResumeLayout(False)
        Me.fraOutputFolderPath.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents txtInputFilePath As System.Windows.Forms.TextBox
    Friend WithEvents cmdSelectFile As System.Windows.Forms.Button
    Friend WithEvents fraInputFilePath As System.Windows.Forms.GroupBox

    Friend WithEvents mnuFile As System.Windows.Forms.MenuItem
    Friend WithEvents mnuEdit As System.Windows.Forms.MenuItem
    Friend WithEvents mnuEditResetOptions As System.Windows.Forms.MenuItem
    Friend WithEvents mnuHelp As System.Windows.Forms.MenuItem
    Friend WithEvents mnuHelpAbout As System.Windows.Forms.MenuItem
    Friend WithEvents mnuEditSep1 As System.Windows.Forms.MenuItem
    Friend WithEvents MainMenuControl As System.Windows.Forms.MainMenu
    Friend WithEvents tbsOptions As System.Windows.Forms.TabControl
    Friend WithEvents dgCustomSICValues As System.Windows.Forms.DataGrid
    Friend WithEvents cmdClearCustomSICList As System.Windows.Forms.Button
    Friend WithEvents cmdCustomSICValuesPopulate As System.Windows.Forms.Button
    Friend WithEvents cmdPasteCustomSICList As System.Windows.Forms.Button
    Friend WithEvents TabPageCustomSICOptions As System.Windows.Forms.TabPage
    Friend WithEvents fraCustomSICControls As System.Windows.Forms.GroupBox
    Friend WithEvents mnuFileSelectOutputFolder As System.Windows.Forms.MenuItem
    Friend WithEvents mnuFileLoadOptions As System.Windows.Forms.MenuItem
    Friend WithEvents mnuFileSaveOptions As System.Windows.Forms.MenuItem
    Friend WithEvents mnuFileExit As System.Windows.Forms.MenuItem
    Friend WithEvents mnuFileSelectInputFile As System.Windows.Forms.MenuItem
    Friend WithEvents mnuFileSep1 As System.Windows.Forms.MenuItem
    Friend WithEvents mnuFileSep2 As System.Windows.Forms.MenuItem
    Friend WithEvents fraOutputFolderPath As System.Windows.Forms.GroupBox
    Friend WithEvents cmdSelectOutputFolder As System.Windows.Forms.Button
    Friend WithEvents txtOutputFolderPath As System.Windows.Forms.TextBox
    Friend WithEvents mnuEditProcessFile As System.Windows.Forms.MenuItem
    Friend WithEvents TabPageMasicExportOptions As System.Windows.Forms.TabPage
    Friend WithEvents TabPageSICOptions As System.Windows.Forms.TabPage
    Friend WithEvents chkIncludeHeaders As System.Windows.Forms.CheckBox
    Friend WithEvents fraExportAllSpectraDataPoints As System.Windows.Forms.GroupBox
    Friend WithEvents cboExportRawDataFileFormat As System.Windows.Forms.ComboBox
    Friend WithEvents lblExportDataPointsFormat As System.Windows.Forms.Label
    Friend WithEvents chkExportRawDataIncludeMSMS As System.Windows.Forms.CheckBox
    Friend WithEvents txtExportRawDataMaxIonCountPerScan As System.Windows.Forms.TextBox
    Friend WithEvents chkExportRawDataRenumberScans As System.Windows.Forms.CheckBox
    Friend WithEvents txtExportRawDataIntensityMinimum As System.Windows.Forms.TextBox
    Friend WithEvents chkExportRawSpectraData As System.Windows.Forms.CheckBox
    Friend WithEvents fraSICSearchThresholds As System.Windows.Forms.GroupBox
    Friend WithEvents txtSICTolerance As System.Windows.Forms.TextBox
    Friend WithEvents lblSICToleranceDa As System.Windows.Forms.Label
    Friend WithEvents txtMaxPeakWidthMinutesBackward As System.Windows.Forms.TextBox
    Friend WithEvents lblMaxPeakWidthMinutes As System.Windows.Forms.Label
    Friend WithEvents lblMaxPeakWidthMinutesBackward As System.Windows.Forms.Label
    Friend WithEvents lblMaxPeakWidthMinutesForward As System.Windows.Forms.Label
    Friend WithEvents txtMaxPeakWidthMinutesForward As System.Windows.Forms.TextBox
    Friend WithEvents txtIntensityThresholdAbsoluteMinimum As System.Windows.Forms.TextBox
    Friend WithEvents lblIntensityThresholdAbsoluteMinimum As System.Windows.Forms.Label
    Friend WithEvents txtIntensityThresholdFractionMax As System.Windows.Forms.TextBox
    Friend WithEvents lblIntensityThresholdFractionMax As System.Windows.Forms.Label
    Friend WithEvents TabPagePeakFindingOptions As System.Windows.Forms.TabPage
    Friend WithEvents fraPeakFindingOptions As System.Windows.Forms.GroupBox
    Friend WithEvents lblSavitzkyGolayFilterOrder As System.Windows.Forms.Label
    Friend WithEvents txtSavitzkyGolayFilterOrder As System.Windows.Forms.TextBox
    Friend WithEvents txtMaxDistanceScansNoOverlap As System.Windows.Forms.TextBox
    Friend WithEvents txtMaxAllowedUpwardSpikeFractionMax As System.Windows.Forms.TextBox
    Friend WithEvents lblMaxAllowedUpwardSpikeFractionMax As System.Windows.Forms.Label
    Friend WithEvents lblMaxDistanceScansNoOverlap As System.Windows.Forms.Label
    Friend WithEvents txtInitialPeakWidthScansMaximum As System.Windows.Forms.TextBox
    Friend WithEvents lblInitialPeakWidthScansMaximum As System.Windows.Forms.Label
    Friend WithEvents fraSmoothingOptions As System.Windows.Forms.GroupBox
    Friend WithEvents TabPageBinningAndSimilarityOptions As System.Windows.Forms.TabPage
    Friend WithEvents fraBinningMZOptions As System.Windows.Forms.GroupBox
    Friend WithEvents txtBinSize As System.Windows.Forms.TextBox
    Friend WithEvents lblBinSize As System.Windows.Forms.Label
    Friend WithEvents txtBinEndX As System.Windows.Forms.TextBox
    Friend WithEvents lblBinEndX As System.Windows.Forms.Label
    Friend WithEvents txtBinStartX As System.Windows.Forms.TextBox
    Friend WithEvents lblBinStartX As System.Windows.Forms.Label
    Friend WithEvents fraSpectrumSimilarityOptions As System.Windows.Forms.GroupBox
    Friend WithEvents txtSpectrumSimilarityMinimum As System.Windows.Forms.TextBox
    Friend WithEvents lblSpectrumSimilarityMinimum As System.Windows.Forms.Label
    Friend WithEvents txtSimilarIonToleranceHalfWidthMinutes As System.Windows.Forms.TextBox
    Friend WithEvents lblSimilarIonTimeToleranceHalfWidth As System.Windows.Forms.Label
    Friend WithEvents txtSimilarIonMZToleranceHalfWidth As System.Windows.Forms.TextBox
    Friend WithEvents lblSimilarIonMZToleranceHalfWidth As System.Windows.Forms.Label
    Friend WithEvents fraBinningIntensityOptions As System.Windows.Forms.GroupBox
    Friend WithEvents txtBinnedDataIntensityPrecisionPct As System.Windows.Forms.TextBox
    Friend WithEvents lblBinnedDataIntensityPrecisionPct As System.Windows.Forms.Label
    Friend WithEvents txtMaximumBinCount As System.Windows.Forms.TextBox
    Friend WithEvents lblMaximumBinCount As System.Windows.Forms.Label
    Friend WithEvents chkBinnedDataNormalize As System.Windows.Forms.CheckBox
    Friend WithEvents chkBinnedDataSumAllIntensitiesForBin As System.Windows.Forms.CheckBox
    Friend WithEvents cmdStartProcessing As System.Windows.Forms.Button
    Friend WithEvents txtInitialPeakWidthScansScaler As System.Windows.Forms.TextBox
    Friend WithEvents lblInitialPeakWidthScansScaler As System.Windows.Forms.Label
    Friend WithEvents lblBinnedDataIntensityPrecisionPctUnits As System.Windows.Forms.Label
    Friend WithEvents cboSICNoiseThresholdMode As System.Windows.Forms.ComboBox
    Friend WithEvents lblNoiseThresholdMode As System.Windows.Forms.Label
    Friend WithEvents txtSICNoiseFractionLowIntensityDataToAverage As System.Windows.Forms.TextBox
    Friend WithEvents txtSICNoiseThresholdIntensity As System.Windows.Forms.TextBox
    Friend WithEvents lblSICOptionsOverview As System.Windows.Forms.Label
    Friend WithEvents lblRawDataExportOverview As System.Windows.Forms.Label
    Friend WithEvents txtDatasetNumber As System.Windows.Forms.TextBox
    Friend WithEvents lblDatasetNumber As System.Windows.Forms.Label
    Friend WithEvents txtCustomSICScanOrAcqTimeTolerance As System.Windows.Forms.TextBox
    Friend WithEvents lblCustomSICScanTolerance As System.Windows.Forms.Label
    Friend WithEvents lblExportRawDataMaxIonCountPerScan As System.Windows.Forms.Label
    Friend WithEvents lblExportRawDataIntensityMinimum As System.Windows.Forms.Label
    Friend WithEvents lblSICNoiseFractionLowIntensityDataToAverage As System.Windows.Forms.Label
    Friend WithEvents lblSICNoiseThresholdIntensity As System.Windows.Forms.Label
    Friend WithEvents TabPageAdvancedOptions As System.Windows.Forms.TabPage
    Friend WithEvents fraMemoryConservationOptions As System.Windows.Forms.GroupBox
    Friend WithEvents chkSkipMSMSProcessing As System.Windows.Forms.CheckBox
    Friend WithEvents fraDatasetLookupInfo As System.Windows.Forms.GroupBox
    Friend WithEvents cmdSelectDatasetLookupFile As System.Windows.Forms.Button
    Friend WithEvents txtDatasetLookupFilePath As System.Windows.Forms.TextBox
    Friend WithEvents lblDatasetLookupFilePath As System.Windows.Forms.Label
    Friend WithEvents lblDatabaseConnectionString As System.Windows.Forms.Label
    Friend WithEvents txtDatabaseConnectionString As System.Windows.Forms.TextBox
    Friend WithEvents fraSICNoiseThresholds As System.Windows.Forms.GroupBox
    Friend WithEvents fraMassSpectraNoiseThresholds As System.Windows.Forms.GroupBox
    Friend WithEvents txtMassSpectraNoiseFractionLowIntensityDataToAverage As System.Windows.Forms.TextBox
    Friend WithEvents lblMassSpectraNoiseFractionLowIntensityDataToAverage As System.Windows.Forms.Label
    Friend WithEvents cboMassSpectraNoiseThresholdMode As System.Windows.Forms.ComboBox
    Friend WithEvents lblMassSpectraNoiseThresholdMode As System.Windows.Forms.Label
    Friend WithEvents txtMassSpectraNoiseThresholdIntensity As System.Windows.Forms.TextBox
    Friend WithEvents lblMassSpectraNoiseThresholdIntensity As System.Windows.Forms.Label
    Friend WithEvents txtExportRawDataSignalToNoiseRatioMinimum As System.Windows.Forms.TextBox
    Friend WithEvents lblExportRawDataSignalToNoiseRatioMinimum As System.Windows.Forms.Label
    Friend WithEvents txtMassSpectraNoiseMinimumSignalToNoiseRatio As System.Windows.Forms.TextBox
    Friend WithEvents lblMassSpectraNoiseMinimumSignalToNoiseRatio As System.Windows.Forms.Label
    Friend WithEvents fraInputFileRangeFilters As System.Windows.Forms.GroupBox
    Friend WithEvents cmdClearAllRangeFilters As System.Windows.Forms.Button
    Friend WithEvents txtScanEnd As System.Windows.Forms.TextBox
    Friend WithEvents txtScanStart As System.Windows.Forms.TextBox
    Friend WithEvents lblScanEnd As System.Windows.Forms.Label
    Friend WithEvents lblScanStart As System.Windows.Forms.Label
    Friend WithEvents txtTimeEnd As System.Windows.Forms.TextBox
    Friend WithEvents lblTimeEnd As System.Windows.Forms.Label
    Friend WithEvents lblTimeStart As System.Windows.Forms.Label
    Friend WithEvents txtTimeStart As System.Windows.Forms.TextBox
    Friend WithEvents lblTimeEndUnits As System.Windows.Forms.Label
    Friend WithEvents lblTimeStartUnits As System.Windows.Forms.Label
    Friend WithEvents txtButterworthSamplingFrequency As System.Windows.Forms.TextBox
    Friend WithEvents lblButterworthSamplingFrequency As System.Windows.Forms.Label
    Friend WithEvents optUseButterworthSmooth As System.Windows.Forms.RadioButton
    Friend WithEvents optUseSavitzkyGolaySmooth As System.Windows.Forms.RadioButton
    Friend WithEvents chkFindPeaksOnSmoothedData As System.Windows.Forms.CheckBox
    Friend WithEvents chkSmoothDataRegardlessOfMinimumPeakWidth As System.Windows.Forms.CheckBox
    Friend WithEvents lblSimilarIonTimeToleranceHalfWidthUnits As System.Windows.Forms.Label
    Friend WithEvents chkExportRawDataOnly As System.Windows.Forms.CheckBox
    Friend WithEvents chkLimitSearchToCustomMZs As System.Windows.Forms.CheckBox
    Friend WithEvents chkReplaceSICZeroesWithMinimumPositiveValueFromMSData As System.Windows.Forms.CheckBox
    Friend WithEvents cmdSetConnectionStringToPNNLServer As System.Windows.Forms.Button
    Friend WithEvents txtDatasetInfoQuerySQL As System.Windows.Forms.TextBox
    Friend WithEvents lblDatasetInfoQuerySQL As System.Windows.Forms.Label
    Friend WithEvents chkRefineReportedParentIonMZ As System.Windows.Forms.CheckBox
    Friend WithEvents mnuEditSaveDefaultOptions As System.Windows.Forms.MenuItem
    Friend WithEvents chkSkipSICAndRawDataProcessing As System.Windows.Forms.CheckBox
    Friend WithEvents fraAdditionalInfoFiles As System.Windows.Forms.GroupBox
    Friend WithEvents chkSaveExtendedStatsFile As System.Windows.Forms.CheckBox
    Friend WithEvents chkSaveMSMethodFile As System.Windows.Forms.CheckBox
    Friend WithEvents chkSaveMSTuneFile As System.Windows.Forms.CheckBox
    Friend WithEvents chkIncludeScanTimesInSICStatsFile As System.Windows.Forms.CheckBox
    Friend WithEvents cmdSelectCustomSICFile As System.Windows.Forms.Button
    Friend WithEvents txtCustomSICFileName As System.Windows.Forms.TextBox
    Friend WithEvents chkSaveExtendedStatsFileIncludeFilterText As System.Windows.Forms.CheckBox
    Friend WithEvents chkSaveExtendedStatsFileIncludeStatusLog As System.Windows.Forms.CheckBox
    Friend WithEvents TabPageReporterIons As System.Windows.Forms.TabPage
    Friend WithEvents fraReporterIonOptions As System.Windows.Forms.GroupBox
    Friend WithEvents chkReporterIonStatsEnabled As System.Windows.Forms.CheckBox
    Friend WithEvents txtReporterIonMZToleranceDa As System.Windows.Forms.TextBox
    Friend WithEvents lblReporterIonMZToleranceDa As System.Windows.Forms.Label
    Friend WithEvents lblCustomSICToleranceType As System.Windows.Forms.Label
    Friend WithEvents optCustomSICScanToleranceAcqTime As System.Windows.Forms.RadioButton
    Friend WithEvents optCustomSICScanToleranceRelative As System.Windows.Forms.RadioButton
    Friend WithEvents optCustomSICScanToleranceAbsolute As System.Windows.Forms.RadioButton
    Friend WithEvents txtCustomSICFileDescription As System.Windows.Forms.TextBox
    Friend WithEvents fraMRMOptions As System.Windows.Forms.GroupBox
    Friend WithEvents chkMRMWriteIntensityCrosstab As System.Windows.Forms.CheckBox
    Friend WithEvents lblMRMInfo As System.Windows.Forms.Label
    Friend WithEvents chkMRMWriteDataList As System.Windows.Forms.CheckBox
    Friend WithEvents optSICTolerancePPM As System.Windows.Forms.RadioButton
    Friend WithEvents optSICToleranceDa As System.Windows.Forms.RadioButton
    Friend WithEvents lblStatusLogKeyNameFilterList As System.Windows.Forms.Label
    Friend WithEvents txtStatusLogKeyNameFilterList As System.Windows.Forms.TextBox
    Friend WithEvents fraDecoyOptions As System.Windows.Forms.GroupBox
    Friend WithEvents txtParentIonDecoyMassDa As System.Windows.Forms.TextBox
    Friend WithEvents lblParentIonDecoyMassDa As System.Windows.Forms.Label
    Friend WithEvents lblParentIonDecoyMassDaUnits As System.Windows.Forms.Label
    Friend WithEvents chkReporterIonSaveUncorrectedIntensities As System.Windows.Forms.CheckBox
    Friend WithEvents chkReporterIonApplyAbundanceCorrection As System.Windows.Forms.CheckBox
    Friend WithEvents chkConsolidateConstantExtendedHeaderValues As System.Windows.Forms.CheckBox
    Friend WithEvents chkWriteDetailedSICDataFile As System.Windows.Forms.CheckBox
    Friend WithEvents fraReporterIonMassMode As System.Windows.Forms.GroupBox
    Friend WithEvents cboReporterIonMassMode As System.Windows.Forms.ComboBox
    Friend WithEvents chkReporterIonSaveObservedMasses As System.Windows.Forms.CheckBox

End Class
