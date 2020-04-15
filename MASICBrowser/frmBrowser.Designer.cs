Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Timers
Imports System.Xml
Imports MASICPeakFinder
Imports OxyDataPlotter
Imports OxyPlot
Imports PRISM
Imports PRISMDatabaseUtils
Imports ProgressFormNET

Partial Class frmBrowser

#Region "Windows Form Designer generated code"


    'Form overrides dispose to clean up the component list.
    Protected Overloads Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            If Not (components Is Nothing) Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Me.components = New ComponentModel.Container()
        Me.lblParentIon = New System.Windows.Forms.Label()
        Me.txtDataFilePath = New System.Windows.Forms.TextBox()
        Me.cmdSelectFile = New System.Windows.Forms.Button()
        Me.lblSortOrder = New System.Windows.Forms.Label()
        Me.cboSortOrder = New System.Windows.Forms.ComboBox()
        Me.chkFixXRange = New System.Windows.Forms.CheckBox()
        Me.txtFixXRange = New System.Windows.Forms.TextBox()
        Me.lblFixXRange = New System.Windows.Forms.Label()
        Me.lblMinimumIntensity = New System.Windows.Forms.Label()
        Me.txtMinimumIntensity = New System.Windows.Forms.TextBox()
        Me.chkFilterByIntensity = New System.Windows.Forms.CheckBox()
        Me.tmrAutoStep = New System.Timers.Timer()
        Me.fraNavigation = New System.Windows.Forms.GroupBox()
        Me.chkAutoStepForward = New System.Windows.Forms.CheckBox()
        Me.txtAutoStep = New System.Windows.Forms.TextBox()
        Me.lblAutoStep = New System.Windows.Forms.Label()
        Me.cmdAutoStep = New System.Windows.Forms.Button()
        Me.cmdPrevious = New System.Windows.Forms.Button()
        Me.cmdNext = New System.Windows.Forms.Button()
        Me.cmdJump = New System.Windows.Forms.Button()
        Me.txtFilterByMZ = New System.Windows.Forms.TextBox()
        Me.lblFilterByMZ = New System.Windows.Forms.Label()
        Me.chkFilterByMZ = New System.Windows.Forms.CheckBox()
        Me.txtFilterByMZTol = New System.Windows.Forms.TextBox()
        Me.lblFilterByMZTolUnits = New System.Windows.Forms.Label()
        Me.lblFilterByMZTol = New System.Windows.Forms.Label()
        Me.txtFixYRange = New System.Windows.Forms.TextBox()
        Me.lblFixYRange = New System.Windows.Forms.Label()
        Me.chkFixYRange = New System.Windows.Forms.CheckBox()
        Me.lblSICsTypeFilter = New System.Windows.Forms.Label()
        Me.cboSICsTypeFilter = New System.Windows.Forms.ComboBox()
        Me.txtStats1 = New System.Windows.Forms.TextBox()
        Me.MainMenuControl = New System.Windows.Forms.MainMenu(Me.components)
        Me.mnuFile = New System.Windows.Forms.MenuItem()
        Me.mnuFileSelectMASICInputFile = New System.Windows.Forms.MenuItem()
        Me.mnuFileSelectMSMSSearchResultsFile = New System.Windows.Forms.MenuItem()
        Me.mnuFileSep1 = New System.Windows.Forms.MenuItem()
        Me.mnuFileExit = New System.Windows.Forms.MenuItem()
        Me.mnuEdit = New System.Windows.Forms.MenuItem()
        Me.mnuEditShowOptimalPeakApexCursor = New System.Windows.Forms.MenuItem()
        Me.mnuHelp = New System.Windows.Forms.MenuItem()
        Me.mnuHelpAbout = New System.Windows.Forms.MenuItem()
        Me.chkSortDescending = New System.Windows.Forms.CheckBox()
        Me.lstParentIonData = New System.Windows.Forms.ListBox()
        Me.txtMinimumSignalToNoise = New System.Windows.Forms.TextBox()
        Me.chkFilterBySignalToNoise = New System.Windows.Forms.CheckBox()
        Me.fraResmoothingOptions = New System.Windows.Forms.GroupBox()
        Me.chkShowSmoothedData = New System.Windows.Forms.CheckBox()
        Me.txtPeakWidthPointsMinimum = New System.Windows.Forms.TextBox()
        Me.lblPeakWidthPointsMinimum = New System.Windows.Forms.Label()
        Me.optDoNotResmooth = New System.Windows.Forms.RadioButton()
        Me.optUseSavitzkyGolaySmooth = New System.Windows.Forms.RadioButton()
        Me.txtButterworthSamplingFrequency = New System.Windows.Forms.TextBox()
        Me.lblButterworthSamplingFrequency = New System.Windows.Forms.Label()
        Me.txtSavitzkyGolayFilterOrder = New System.Windows.Forms.TextBox()
        Me.lblSavitzkyGolayFilterOrder = New System.Windows.Forms.Label()
        Me.optUseButterworthSmooth = New System.Windows.Forms.RadioButton()
        Me.fraPeakFinder = New System.Windows.Forms.GroupBox()
        Me.cmdRedoSICPeakFindingAllData = New System.Windows.Forms.Button()
        Me.chkUsePeakFinder = New System.Windows.Forms.CheckBox()
        Me.chkFindPeaksSubtractBaseline = New System.Windows.Forms.CheckBox()
        Me.fraSortOrderAndStats = New System.Windows.Forms.GroupBox()
        Me.chkShowBaselineCorrectedStats = New System.Windows.Forms.CheckBox()
        Me.txtStats2 = New System.Windows.Forms.TextBox()
        Me.txtStats3 = New System.Windows.Forms.TextBox()
        Me.TabControl1 = New System.Windows.Forms.TabControl()
        Me.tpSICFilters = New System.Windows.Forms.TabPage()
        Me.tpMsMsSearchResultsFilters = New System.Windows.Forms.TabPage()
        Me.chkSequenceFilterExactMatch = New System.Windows.Forms.CheckBox()
        Me.lblSequenceFilter = New System.Windows.Forms.Label()
        Me.lblChargeFilter = New System.Windows.Forms.Label()
        Me.TextBox2 = New System.Windows.Forms.TextBox()
        Me.lblMinimumXCorr = New System.Windows.Forms.Label()
        Me.txtSequenceFilter = New System.Windows.Forms.TextBox()
        Me.txtMinimumXCorr = New System.Windows.Forms.TextBox()
        Me.pnlInputFile = New System.Windows.Forms.Panel()
        Me.pnlSICs = New System.Windows.Forms.Panel()
        Me.pnlNavigationAndOptions = New System.Windows.Forms.Panel()
        Me.pnlBottom = New System.Windows.Forms.Panel()
        CType(Me.tmrAutoStep, ComponentModel.ISupportInitialize).BeginInit()
        Me.fraNavigation.SuspendLayout()
        Me.fraResmoothingOptions.SuspendLayout()
        Me.fraPeakFinder.SuspendLayout()
        Me.fraSortOrderAndStats.SuspendLayout()
        Me.TabControl1.SuspendLayout()
        Me.tpSICFilters.SuspendLayout()
        Me.tpMsMsSearchResultsFilters.SuspendLayout()
        Me.pnlInputFile.SuspendLayout()
        Me.pnlSICs.SuspendLayout()
        Me.pnlNavigationAndOptions.SuspendLayout()
        Me.pnlBottom.SuspendLayout()
        Me.SuspendLayout()
        '
        'lblParentIon
        '
        Me.lblParentIon.Location = New System.Drawing.Point(10, 9)
        Me.lblParentIon.Name = "lblParentIon"
        Me.lblParentIon.Size = New System.Drawing.Size(182, 19)
        Me.lblParentIon.TabIndex = 2
        Me.lblParentIon.Text = "Parent Ion SIC to View"
        '
        'txtDataFilePath
        '
        Me.txtDataFilePath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtDataFilePath.Location = New System.Drawing.Point(115, 11)
        Me.txtDataFilePath.Name = "txtDataFilePath"
        Me.txtDataFilePath.Size = New System.Drawing.Size(587, 22)
        Me.txtDataFilePath.TabIndex = 1
        Me.txtDataFilePath.Text = "D:\"
        '
        'cmdSelectFile
        '
        Me.cmdSelectFile.Location = New System.Drawing.Point(10, 9)
        Me.cmdSelectFile.Name = "cmdSelectFile"
        Me.cmdSelectFile.Size = New System.Drawing.Size(96, 28)
        Me.cmdSelectFile.TabIndex = 0
        Me.cmdSelectFile.Text = "&Select File"
        '
        'lblSortOrder
        '
        Me.lblSortOrder.Location = New System.Drawing.Point(10, 9)
        Me.lblSortOrder.Name = "lblSortOrder"
        Me.lblSortOrder.Size = New System.Drawing.Size(105, 19)
        Me.lblSortOrder.TabIndex = 0
        Me.lblSortOrder.Text = "Sort Order"
        '
        'cboSortOrder
        '
        Me.cboSortOrder.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboSortOrder.Location = New System.Drawing.Point(10, 32)
        Me.cboSortOrder.Name = "cboSortOrder"
        Me.cboSortOrder.Size = New System.Drawing.Size(316, 24)
        Me.cboSortOrder.TabIndex = 1
        '
        'chkFixXRange
        '
        Me.chkFixXRange.Checked = True
        Me.chkFixXRange.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkFixXRange.Location = New System.Drawing.Point(10, 74)
        Me.chkFixXRange.Name = "chkFixXRange"
        Me.chkFixXRange.Size = New System.Drawing.Size(105, 18)
        Me.chkFixXRange.TabIndex = 2
        Me.chkFixXRange.Text = "Fix X Range"
        '
        'txtFixXRange
        '
        Me.txtFixXRange.Location = New System.Drawing.Point(154, 74)
        Me.txtFixXRange.Name = "txtFixXRange"
        Me.txtFixXRange.Size = New System.Drawing.Size(86, 22)
        Me.txtFixXRange.TabIndex = 3
        Me.txtFixXRange.Text = "300"
        '
        'lblFixXRange
        '
        Me.lblFixXRange.Location = New System.Drawing.Point(240, 78)
        Me.lblFixXRange.Name = "lblFixXRange"
        Me.lblFixXRange.Size = New System.Drawing.Size(55, 19)
        Me.lblFixXRange.TabIndex = 4
        Me.lblFixXRange.Text = "scans"
        '
        'lblMinimumIntensity
        '
        Me.lblMinimumIntensity.Location = New System.Drawing.Point(240, 162)
        Me.lblMinimumIntensity.Name = "lblMinimumIntensity"
        Me.lblMinimumIntensity.Size = New System.Drawing.Size(55, 18)
        Me.lblMinimumIntensity.TabIndex = 12
        Me.lblMinimumIntensity.Text = "counts"
        '
        'txtMinimumIntensity
        '
        Me.txtMinimumIntensity.Location = New System.Drawing.Point(173, 157)
        Me.txtMinimumIntensity.Name = "txtMinimumIntensity"
        Me.txtMinimumIntensity.Size = New System.Drawing.Size(67, 22)
        Me.txtMinimumIntensity.TabIndex = 11
        Me.txtMinimumIntensity.Text = "1000000"
        '
        'chkFilterByIntensity
        '
        Me.chkFilterByIntensity.Location = New System.Drawing.Point(10, 157)
        Me.chkFilterByIntensity.Name = "chkFilterByIntensity"
        Me.chkFilterByIntensity.Size = New System.Drawing.Size(172, 18)
        Me.chkFilterByIntensity.TabIndex = 10
        Me.chkFilterByIntensity.Text = "Minimum Intensity"
        '
        'tmrAutoStep
        '
        Me.tmrAutoStep.Enabled = True
        Me.tmrAutoStep.Interval = 10.0R
        Me.tmrAutoStep.SynchronizingObject = Me
        '
        'fraNavigation
        '
        Me.fraNavigation.Controls.Add(Me.chkAutoStepForward)
        Me.fraNavigation.Controls.Add(Me.txtAutoStep)
        Me.fraNavigation.Controls.Add(Me.lblAutoStep)
        Me.fraNavigation.Controls.Add(Me.cmdAutoStep)
        Me.fraNavigation.Controls.Add(Me.cmdPrevious)
        Me.fraNavigation.Controls.Add(Me.cmdNext)
        Me.fraNavigation.Controls.Add(Me.cmdJump)
        Me.fraNavigation.Enabled = False
        Me.fraNavigation.Location = New System.Drawing.Point(19, 28)
        Me.fraNavigation.Name = "fraNavigation"
        Me.fraNavigation.Size = New System.Drawing.Size(269, 147)
        Me.fraNavigation.TabIndex = 4
        Me.fraNavigation.TabStop = False
        Me.fraNavigation.Text = "Navigation"
        '
        'chkAutoStepForward
        '
        Me.chkAutoStepForward.Checked = True
        Me.chkAutoStepForward.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkAutoStepForward.Location = New System.Drawing.Point(134, 120)
        Me.chkAutoStepForward.Name = "chkAutoStepForward"
        Me.chkAutoStepForward.Size = New System.Drawing.Size(125, 18)
        Me.chkAutoStepForward.TabIndex = 6
        Me.chkAutoStepForward.Text = "Move forward"
        '
        'txtAutoStep
        '
        Me.txtAutoStep.Location = New System.Drawing.Point(134, 92)
        Me.txtAutoStep.Name = "txtAutoStep"
        Me.txtAutoStep.Size = New System.Drawing.Size(39, 22)
        Me.txtAutoStep.TabIndex = 4
        Me.txtAutoStep.Text = "150"
        '
        'lblAutoStep
        '
        Me.lblAutoStep.Location = New System.Drawing.Point(182, 92)
        Me.lblAutoStep.Name = "lblAutoStep"
        Me.lblAutoStep.Size = New System.Drawing.Size(77, 19)
        Me.lblAutoStep.TabIndex = 5
        '
        'cmdAutoStep
        '
        Me.cmdAutoStep.Location = New System.Drawing.Point(10, 92)
        Me.cmdAutoStep.Name = "cmdAutoStep"
        Me.cmdAutoStep.Size = New System.Drawing.Size(105, 28)
        Me.cmdAutoStep.TabIndex = 2
        Me.cmdAutoStep.Text = "&Auto Step"
        '
        'cmdPrevious
        '
        Me.cmdPrevious.Location = New System.Drawing.Point(10, 28)
        Me.cmdPrevious.Name = "cmdPrevious"
        Me.cmdPrevious.Size = New System.Drawing.Size(105, 27)
        Me.cmdPrevious.TabIndex = 0
        Me.cmdPrevious.Text = "&Previous"
        '
        'cmdNext
        '
        Me.cmdNext.Location = New System.Drawing.Point(10, 55)
        Me.cmdNext.Name = "cmdNext"
        Me.cmdNext.Size = New System.Drawing.Size(105, 28)
        Me.cmdNext.TabIndex = 1
        Me.cmdNext.Text = "&Next"
        '
        'cmdJump
        '
        Me.cmdJump.Location = New System.Drawing.Point(134, 28)
        Me.cmdJump.Name = "cmdJump"
        Me.cmdJump.Size = New System.Drawing.Size(116, 27)
        Me.cmdJump.TabIndex = 3
        Me.cmdJump.Text = "&Jump to Scan"
        '
        'txtFilterByMZ
        '
        Me.txtFilterByMZ.Location = New System.Drawing.Point(173, 185)
        Me.txtFilterByMZ.Name = "txtFilterByMZ"
        Me.txtFilterByMZ.Size = New System.Drawing.Size(67, 22)
        Me.txtFilterByMZ.TabIndex = 14
        Me.txtFilterByMZ.Text = "543"
        '
        'lblFilterByMZ
        '
        Me.lblFilterByMZ.Location = New System.Drawing.Point(240, 189)
        Me.lblFilterByMZ.Name = "lblFilterByMZ"
        Me.lblFilterByMZ.Size = New System.Drawing.Size(29, 19)
        Me.lblFilterByMZ.TabIndex = 15
        Me.lblFilterByMZ.Text = "m/z"
        '
        'chkFilterByMZ
        '
        Me.chkFilterByMZ.Location = New System.Drawing.Point(10, 185)
        Me.chkFilterByMZ.Name = "chkFilterByMZ"
        Me.chkFilterByMZ.Size = New System.Drawing.Size(115, 18)
        Me.chkFilterByMZ.TabIndex = 13
        Me.chkFilterByMZ.Text = "Filter by m/z"
        '
        'txtFilterByMZTol
        '
        Me.txtFilterByMZTol.Location = New System.Drawing.Point(192, 212)
        Me.txtFilterByMZTol.Name = "txtFilterByMZTol"
        Me.txtFilterByMZTol.Size = New System.Drawing.Size(48, 22)
        Me.txtFilterByMZTol.TabIndex = 17
        Me.txtFilterByMZTol.Text = "0.2"
        '
        'lblFilterByMZTolUnits
        '
        Me.lblFilterByMZTolUnits.Location = New System.Drawing.Point(240, 217)
        Me.lblFilterByMZTolUnits.Name = "lblFilterByMZTolUnits"
        Me.lblFilterByMZTolUnits.Size = New System.Drawing.Size(38, 18)
        Me.lblFilterByMZTolUnits.TabIndex = 18
        Me.lblFilterByMZTolUnits.Text = "m/z"
        '
        'lblFilterByMZTol
        '
        Me.lblFilterByMZTol.Location = New System.Drawing.Point(163, 217)
        Me.lblFilterByMZTol.Name = "lblFilterByMZTol"
        Me.lblFilterByMZTol.Size = New System.Drawing.Size(19, 18)
        Me.lblFilterByMZTol.TabIndex = 16
        Me.lblFilterByMZTol.Text = "±"
        Me.lblFilterByMZTol.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'txtFixYRange
        '
        Me.txtFixYRange.Location = New System.Drawing.Point(154, 102)
        Me.txtFixYRange.Name = "txtFixYRange"
        Me.txtFixYRange.Size = New System.Drawing.Size(86, 22)
        Me.txtFixYRange.TabIndex = 6
        Me.txtFixYRange.Text = "1E6"
        '
        'lblFixYRange
        '
        Me.lblFixYRange.Location = New System.Drawing.Point(240, 106)
        Me.lblFixYRange.Name = "lblFixYRange"
        Me.lblFixYRange.Size = New System.Drawing.Size(55, 19)
        Me.lblFixYRange.TabIndex = 7
        Me.lblFixYRange.Text = "counts"
        '
        'chkFixYRange
        '
        Me.chkFixYRange.Checked = True
        Me.chkFixYRange.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkFixYRange.Location = New System.Drawing.Point(10, 102)
        Me.chkFixYRange.Name = "chkFixYRange"
        Me.chkFixYRange.Size = New System.Drawing.Size(105, 18)
        Me.chkFixYRange.TabIndex = 5
        Me.chkFixYRange.Text = "Fix Y Range"
        '
        'lblSICsTypeFilter
        '
        Me.lblSICsTypeFilter.Location = New System.Drawing.Point(10, 9)
        Me.lblSICsTypeFilter.Name = "lblSICsTypeFilter"
        Me.lblSICsTypeFilter.Size = New System.Drawing.Size(182, 19)
        Me.lblSICsTypeFilter.TabIndex = 0
        Me.lblSICsTypeFilter.Text = "SIC Type Filter"
        '
        'cboSICsTypeFilter
        '
        Me.cboSICsTypeFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboSICsTypeFilter.Location = New System.Drawing.Point(10, 28)
        Me.cboSICsTypeFilter.Name = "cboSICsTypeFilter"
        Me.cboSICsTypeFilter.Size = New System.Drawing.Size(278, 24)
        Me.cboSICsTypeFilter.TabIndex = 1
        '
        'txtStats1
        '
        Me.txtStats1.Location = New System.Drawing.Point(10, 92)
        Me.txtStats1.Multiline = True
        Me.txtStats1.Name = "txtStats1"
        Me.txtStats1.ReadOnly = True
        Me.txtStats1.Size = New System.Drawing.Size(158, 102)
        Me.txtStats1.TabIndex = 4
        '
        'MainMenuControl
        '
        Me.MainMenuControl.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.mnuFile, Me.mnuEdit, Me.mnuHelp})
        '
        'mnuFile
        '
        Me.mnuFile.Index = 0
        Me.mnuFile.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.mnuFileSelectMASICInputFile, Me.mnuFileSelectMSMSSearchResultsFile, Me.mnuFileSep1, Me.mnuFileExit})
        Me.mnuFile.Text = "&File"
        '
        'mnuFileSelectMASICInputFile
        '
        Me.mnuFileSelectMASICInputFile.Index = 0
        Me.mnuFileSelectMASICInputFile.Text = "&Select MASIC Input File"
        '
        'mnuFileSelectMSMSSearchResultsFile
        '
        Me.mnuFileSelectMSMSSearchResultsFile.Index = 1
        Me.mnuFileSelectMSMSSearchResultsFile.Text = "Select MS/MS Search &Results File (Syn or FHT)"
        '
        'mnuFileSep1
        '
        Me.mnuFileSep1.Index = 2
        Me.mnuFileSep1.Text = "-"
        '
        'mnuFileExit
        '
        Me.mnuFileExit.Index = 3
        Me.mnuFileExit.Text = "E&xit"
        '
        'mnuEdit
        '
        Me.mnuEdit.Index = 1
        Me.mnuEdit.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.mnuEditShowOptimalPeakApexCursor})
        Me.mnuEdit.Text = "&Edit"
        '
        'mnuEditShowOptimalPeakApexCursor
        '
        Me.mnuEditShowOptimalPeakApexCursor.Checked = True
        Me.mnuEditShowOptimalPeakApexCursor.Index = 0
        Me.mnuEditShowOptimalPeakApexCursor.Text = "&Show optimal peak apex cursor"
        Me.mnuEditShowOptimalPeakApexCursor.Visible = False
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
        'chkSortDescending
        '
        Me.chkSortDescending.Location = New System.Drawing.Point(192, 9)
        Me.chkSortDescending.Name = "chkSortDescending"
        Me.chkSortDescending.Size = New System.Drawing.Size(134, 19)
        Me.chkSortDescending.TabIndex = 2
        Me.chkSortDescending.Text = "Sort Descending"
        '
        'lstParentIonData
        '
        Me.lstParentIonData.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lstParentIonData.ItemHeight = 16
        Me.lstParentIonData.Location = New System.Drawing.Point(10, 28)
        Me.lstParentIonData.Name = "lstParentIonData"
        Me.lstParentIonData.Size = New System.Drawing.Size(326, 100)
        Me.lstParentIonData.TabIndex = 3
        '
        'txtMinimumSignalToNoise
        '
        Me.txtMinimumSignalToNoise.Location = New System.Drawing.Point(173, 129)
        Me.txtMinimumSignalToNoise.Name = "txtMinimumSignalToNoise"
        Me.txtMinimumSignalToNoise.Size = New System.Drawing.Size(67, 22)
        Me.txtMinimumSignalToNoise.TabIndex = 9
        Me.txtMinimumSignalToNoise.Text = "3"
        '
        'chkFilterBySignalToNoise
        '
        Me.chkFilterBySignalToNoise.Location = New System.Drawing.Point(10, 129)
        Me.chkFilterBySignalToNoise.Name = "chkFilterBySignalToNoise"
        Me.chkFilterBySignalToNoise.Size = New System.Drawing.Size(144, 19)
        Me.chkFilterBySignalToNoise.TabIndex = 8
        Me.chkFilterBySignalToNoise.Text = "Minimum S/N"
        '
        'fraResmoothingOptions
        '
        Me.fraResmoothingOptions.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.fraResmoothingOptions.Controls.Add(Me.chkShowSmoothedData)
        Me.fraResmoothingOptions.Controls.Add(Me.txtPeakWidthPointsMinimum)
        Me.fraResmoothingOptions.Controls.Add(Me.lblPeakWidthPointsMinimum)
        Me.fraResmoothingOptions.Controls.Add(Me.optDoNotResmooth)
        Me.fraResmoothingOptions.Controls.Add(Me.optUseSavitzkyGolaySmooth)
        Me.fraResmoothingOptions.Controls.Add(Me.txtButterworthSamplingFrequency)
        Me.fraResmoothingOptions.Controls.Add(Me.lblButterworthSamplingFrequency)
        Me.fraResmoothingOptions.Controls.Add(Me.txtSavitzkyGolayFilterOrder)
        Me.fraResmoothingOptions.Controls.Add(Me.lblSavitzkyGolayFilterOrder)
        Me.fraResmoothingOptions.Controls.Add(Me.optUseButterworthSmooth)
        Me.fraResmoothingOptions.Location = New System.Drawing.Point(10, 9)
        Me.fraResmoothingOptions.Name = "fraResmoothingOptions"
        Me.fraResmoothingOptions.Size = New System.Drawing.Size(384, 157)
        Me.fraResmoothingOptions.TabIndex = 7
        Me.fraResmoothingOptions.TabStop = False
        Me.fraResmoothingOptions.Text = "Smoothing Options"
        '
        'chkShowSmoothedData
        '
        Me.chkShowSmoothedData.Checked = True
        Me.chkShowSmoothedData.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkShowSmoothedData.Location = New System.Drawing.Point(19, 18)
        Me.chkShowSmoothedData.Name = "chkShowSmoothedData"
        Me.chkShowSmoothedData.Size = New System.Drawing.Size(183, 19)
        Me.chkShowSmoothedData.TabIndex = 0
        Me.chkShowSmoothedData.Text = "Show Smoothed Data"
        '
        'txtPeakWidthPointsMinimum
        '
        Me.txtPeakWidthPointsMinimum.Location = New System.Drawing.Point(317, 120)
        Me.txtPeakWidthPointsMinimum.Name = "txtPeakWidthPointsMinimum"
        Me.txtPeakWidthPointsMinimum.Size = New System.Drawing.Size(57, 22)
        Me.txtPeakWidthPointsMinimum.TabIndex = 9
        Me.txtPeakWidthPointsMinimum.Text = "6"
        '
        'lblPeakWidthPointsMinimum
        '
        Me.lblPeakWidthPointsMinimum.Location = New System.Drawing.Point(115, 129)
        Me.lblPeakWidthPointsMinimum.Name = "lblPeakWidthPointsMinimum"
        Me.lblPeakWidthPointsMinimum.Size = New System.Drawing.Size(192, 19)
        Me.lblPeakWidthPointsMinimum.TabIndex = 8
        Me.lblPeakWidthPointsMinimum.Text = "Minimum Peak Width (points)"
        '
        'optDoNotResmooth
        '
        Me.optDoNotResmooth.Checked = True
        Me.optDoNotResmooth.Location = New System.Drawing.Point(19, 55)
        Me.optDoNotResmooth.Name = "optDoNotResmooth"
        Me.optDoNotResmooth.Size = New System.Drawing.Size(240, 19)
        Me.optDoNotResmooth.TabIndex = 1
        Me.optDoNotResmooth.TabStop = True
        Me.optDoNotResmooth.Text = "Do Not Show Smoothed Data"
        '
        'optUseSavitzkyGolaySmooth
        '
        Me.optUseSavitzkyGolaySmooth.Location = New System.Drawing.Point(19, 92)
        Me.optUseSavitzkyGolaySmooth.Name = "optUseSavitzkyGolaySmooth"
        Me.optUseSavitzkyGolaySmooth.Size = New System.Drawing.Size(202, 19)
        Me.optUseSavitzkyGolaySmooth.TabIndex = 5
        Me.optUseSavitzkyGolaySmooth.Text = "Use Savitzky Golay Smooth"
        '
        'txtButterworthSamplingFrequency
        '
        Me.txtButterworthSamplingFrequency.Location = New System.Drawing.Point(317, 65)
        Me.txtButterworthSamplingFrequency.Name = "txtButterworthSamplingFrequency"
        Me.txtButterworthSamplingFrequency.Size = New System.Drawing.Size(57, 22)
        Me.txtButterworthSamplingFrequency.TabIndex = 4
        Me.txtButterworthSamplingFrequency.Text = "0.25"
        '
        'lblButterworthSamplingFrequency
        '
        Me.lblButterworthSamplingFrequency.Location = New System.Drawing.Point(221, 74)
        Me.lblButterworthSamplingFrequency.Name = "lblButterworthSamplingFrequency"
        Me.lblButterworthSamplingFrequency.Size = New System.Drawing.Size(86, 18)
        Me.lblButterworthSamplingFrequency.TabIndex = 3
        Me.lblButterworthSamplingFrequency.Text = "Filter Order"
        '
        'txtSavitzkyGolayFilterOrder
        '
        Me.txtSavitzkyGolayFilterOrder.Location = New System.Drawing.Point(317, 92)
        Me.txtSavitzkyGolayFilterOrder.Name = "txtSavitzkyGolayFilterOrder"
        Me.txtSavitzkyGolayFilterOrder.Size = New System.Drawing.Size(57, 22)
        Me.txtSavitzkyGolayFilterOrder.TabIndex = 7
        Me.txtSavitzkyGolayFilterOrder.Text = "0"
        '
        'lblSavitzkyGolayFilterOrder
        '
        Me.lblSavitzkyGolayFilterOrder.Location = New System.Drawing.Point(221, 102)
        Me.lblSavitzkyGolayFilterOrder.Name = "lblSavitzkyGolayFilterOrder"
        Me.lblSavitzkyGolayFilterOrder.Size = New System.Drawing.Size(86, 18)
        Me.lblSavitzkyGolayFilterOrder.TabIndex = 6
        Me.lblSavitzkyGolayFilterOrder.Text = "Filter Order"
        '
        'optUseButterworthSmooth
        '
        Me.optUseButterworthSmooth.Location = New System.Drawing.Point(19, 74)
        Me.optUseButterworthSmooth.Name = "optUseButterworthSmooth"
        Me.optUseButterworthSmooth.Size = New System.Drawing.Size(202, 18)
        Me.optUseButterworthSmooth.TabIndex = 2
        Me.optUseButterworthSmooth.Text = "Use Butterworth Smooth"
        '
        'fraPeakFinder
        '
        Me.fraPeakFinder.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.fraPeakFinder.Controls.Add(Me.cmdRedoSICPeakFindingAllData)
        Me.fraPeakFinder.Controls.Add(Me.chkUsePeakFinder)
        Me.fraPeakFinder.Controls.Add(Me.chkFindPeaksSubtractBaseline)
        Me.fraPeakFinder.Location = New System.Drawing.Point(403, 9)
        Me.fraPeakFinder.Name = "fraPeakFinder"
        Me.fraPeakFinder.Size = New System.Drawing.Size(240, 148)
        Me.fraPeakFinder.TabIndex = 8
        Me.fraPeakFinder.TabStop = False
        Me.fraPeakFinder.Text = "SIC Peak Finding"
        '
        'cmdRedoSICPeakFindingAllData
        '
        Me.cmdRedoSICPeakFindingAllData.Location = New System.Drawing.Point(19, 92)
        Me.cmdRedoSICPeakFindingAllData.Name = "cmdRedoSICPeakFindingAllData"
        Me.cmdRedoSICPeakFindingAllData.Size = New System.Drawing.Size(135, 46)
        Me.cmdRedoSICPeakFindingAllData.TabIndex = 20
        Me.cmdRedoSICPeakFindingAllData.Text = "Redo SIC Peak Finding For All Data"
        '
        'chkUsePeakFinder
        '
        Me.chkUsePeakFinder.Location = New System.Drawing.Point(10, 18)
        Me.chkUsePeakFinder.Name = "chkUsePeakFinder"
        Me.chkUsePeakFinder.Size = New System.Drawing.Size(220, 19)
        Me.chkUsePeakFinder.TabIndex = 18
        Me.chkUsePeakFinder.Text = "Recompute SIC Peak Stats"
        '
        'chkFindPeaksSubtractBaseline
        '
        Me.chkFindPeaksSubtractBaseline.Checked = True
        Me.chkFindPeaksSubtractBaseline.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkFindPeaksSubtractBaseline.Location = New System.Drawing.Point(19, 38)
        Me.chkFindPeaksSubtractBaseline.Name = "chkFindPeaksSubtractBaseline"
        Me.chkFindPeaksSubtractBaseline.Size = New System.Drawing.Size(211, 37)
        Me.chkFindPeaksSubtractBaseline.TabIndex = 19
        Me.chkFindPeaksSubtractBaseline.Text = "Subtract baseline when computing Intensity and Area"
        '
        'fraSortOrderAndStats
        '
        Me.fraSortOrderAndStats.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.fraSortOrderAndStats.Controls.Add(Me.chkShowBaselineCorrectedStats)
        Me.fraSortOrderAndStats.Controls.Add(Me.txtStats2)
        Me.fraSortOrderAndStats.Controls.Add(Me.txtStats3)
        Me.fraSortOrderAndStats.Controls.Add(Me.cboSortOrder)
        Me.fraSortOrderAndStats.Controls.Add(Me.lblSortOrder)
        Me.fraSortOrderAndStats.Controls.Add(Me.chkSortDescending)
        Me.fraSortOrderAndStats.Controls.Add(Me.txtStats1)
        Me.fraSortOrderAndStats.Location = New System.Drawing.Point(0, 141)
        Me.fraSortOrderAndStats.Name = "fraSortOrderAndStats"
        Me.fraSortOrderAndStats.Size = New System.Drawing.Size(338, 259)
        Me.fraSortOrderAndStats.TabIndex = 5
        Me.fraSortOrderAndStats.TabStop = False
        '
        'chkShowBaselineCorrectedStats
        '
        Me.chkShowBaselineCorrectedStats.Checked = True
        Me.chkShowBaselineCorrectedStats.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkShowBaselineCorrectedStats.Location = New System.Drawing.Point(10, 67)
        Me.chkShowBaselineCorrectedStats.Name = "chkShowBaselineCorrectedStats"
        Me.chkShowBaselineCorrectedStats.Size = New System.Drawing.Size(259, 18)
        Me.chkShowBaselineCorrectedStats.TabIndex = 14
        Me.chkShowBaselineCorrectedStats.Text = "Show Baseline Corrected Stats"
        '
        'txtStats2
        '
        Me.txtStats2.Location = New System.Drawing.Point(173, 92)
        Me.txtStats2.Multiline = True
        Me.txtStats2.Name = "txtStats2"
        Me.txtStats2.ReadOnly = True
        Me.txtStats2.Size = New System.Drawing.Size(158, 102)
        Me.txtStats2.TabIndex = 6
        '
        'txtStats3
        '
        Me.txtStats3.Location = New System.Drawing.Point(10, 203)
        Me.txtStats3.Multiline = True
        Me.txtStats3.Name = "txtStats3"
        Me.txtStats3.ReadOnly = True
        Me.txtStats3.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtStats3.Size = New System.Drawing.Size(319, 46)
        Me.txtStats3.TabIndex = 5
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.tpSICFilters)
        Me.TabControl1.Controls.Add(Me.tpMsMsSearchResultsFilters)
        Me.TabControl1.Location = New System.Drawing.Point(19, 185)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(307, 277)
        Me.TabControl1.TabIndex = 6
        '
        'tpSICFilters
        '
        Me.tpSICFilters.Controls.Add(Me.chkFilterByMZ)
        Me.tpSICFilters.Controls.Add(Me.txtFilterByMZTol)
        Me.tpSICFilters.Controls.Add(Me.lblFilterByMZTolUnits)
        Me.tpSICFilters.Controls.Add(Me.lblFilterByMZTol)
        Me.tpSICFilters.Controls.Add(Me.txtFixYRange)
        Me.tpSICFilters.Controls.Add(Me.lblFixYRange)
        Me.tpSICFilters.Controls.Add(Me.chkFixYRange)
        Me.tpSICFilters.Controls.Add(Me.lblSICsTypeFilter)
        Me.tpSICFilters.Controls.Add(Me.cboSICsTypeFilter)
        Me.tpSICFilters.Controls.Add(Me.chkFixXRange)
        Me.tpSICFilters.Controls.Add(Me.txtFixXRange)
        Me.tpSICFilters.Controls.Add(Me.lblFixXRange)
        Me.tpSICFilters.Controls.Add(Me.lblMinimumIntensity)
        Me.tpSICFilters.Controls.Add(Me.txtMinimumIntensity)
        Me.tpSICFilters.Controls.Add(Me.chkFilterByIntensity)
        Me.tpSICFilters.Controls.Add(Me.txtMinimumSignalToNoise)
        Me.tpSICFilters.Controls.Add(Me.chkFilterBySignalToNoise)
        Me.tpSICFilters.Controls.Add(Me.txtFilterByMZ)
        Me.tpSICFilters.Controls.Add(Me.lblFilterByMZ)
        Me.tpSICFilters.Location = New System.Drawing.Point(4, 25)
        Me.tpSICFilters.Name = "tpSICFilters"
        Me.tpSICFilters.Size = New System.Drawing.Size(299, 248)
        Me.tpSICFilters.TabIndex = 0
        Me.tpSICFilters.Text = "SIC Filters"
        '
        'tpMsMsSearchResultsFilters
        '
        Me.tpMsMsSearchResultsFilters.Controls.Add(Me.chkSequenceFilterExactMatch)
        Me.tpMsMsSearchResultsFilters.Controls.Add(Me.lblSequenceFilter)
        Me.tpMsMsSearchResultsFilters.Controls.Add(Me.lblChargeFilter)
        Me.tpMsMsSearchResultsFilters.Controls.Add(Me.TextBox2)
        Me.tpMsMsSearchResultsFilters.Controls.Add(Me.lblMinimumXCorr)
        Me.tpMsMsSearchResultsFilters.Controls.Add(Me.txtSequenceFilter)
        Me.tpMsMsSearchResultsFilters.Controls.Add(Me.txtMinimumXCorr)
        Me.tpMsMsSearchResultsFilters.Location = New System.Drawing.Point(4, 25)
        Me.tpMsMsSearchResultsFilters.Name = "tpMsMsSearchResultsFilters"
        Me.tpMsMsSearchResultsFilters.Size = New System.Drawing.Size(299, 248)
        Me.tpMsMsSearchResultsFilters.TabIndex = 1
        Me.tpMsMsSearchResultsFilters.Text = "MS/MS Results Filters"
        '
        'chkSequenceFilterExactMatch
        '
        Me.chkSequenceFilterExactMatch.Checked = True
        Me.chkSequenceFilterExactMatch.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkSequenceFilterExactMatch.Enabled = False
        Me.chkSequenceFilterExactMatch.Location = New System.Drawing.Point(173, 83)
        Me.chkSequenceFilterExactMatch.Name = "chkSequenceFilterExactMatch"
        Me.chkSequenceFilterExactMatch.Size = New System.Drawing.Size(108, 19)
        Me.chkSequenceFilterExactMatch.TabIndex = 5
        Me.chkSequenceFilterExactMatch.Text = "Exact Match?"
        '
        'lblSequenceFilter
        '
        Me.lblSequenceFilter.Location = New System.Drawing.Point(10, 83)
        Me.lblSequenceFilter.Name = "lblSequenceFilter"
        Me.lblSequenceFilter.Size = New System.Drawing.Size(115, 19)
        Me.lblSequenceFilter.TabIndex = 4
        Me.lblSequenceFilter.Text = "Sequence Filter"
        '
        'lblChargeFilter
        '
        Me.lblChargeFilter.Location = New System.Drawing.Point(10, 46)
        Me.lblChargeFilter.Name = "lblChargeFilter"
        Me.lblChargeFilter.Size = New System.Drawing.Size(115, 19)
        Me.lblChargeFilter.TabIndex = 2
        Me.lblChargeFilter.Text = "Charge Filter"
        '
        'TextBox2
        '
        Me.TextBox2.Enabled = False
        Me.TextBox2.Location = New System.Drawing.Point(173, 46)
        Me.TextBox2.Name = "TextBox2"
        Me.TextBox2.Size = New System.Drawing.Size(67, 22)
        Me.TextBox2.TabIndex = 3
        Me.TextBox2.Text = "0"
        '
        'lblMinimumXCorr
        '
        Me.lblMinimumXCorr.Location = New System.Drawing.Point(10, 18)
        Me.lblMinimumXCorr.Name = "lblMinimumXCorr"
        Me.lblMinimumXCorr.Size = New System.Drawing.Size(115, 19)
        Me.lblMinimumXCorr.TabIndex = 0
        Me.lblMinimumXCorr.Text = "XCorr Minimum"
        '
        'txtSequenceFilter
        '
        Me.txtSequenceFilter.Enabled = False
        Me.txtSequenceFilter.Location = New System.Drawing.Point(10, 102)
        Me.txtSequenceFilter.Name = "txtSequenceFilter"
        Me.txtSequenceFilter.Size = New System.Drawing.Size(276, 22)
        Me.txtSequenceFilter.TabIndex = 6
        '
        'txtMinimumXCorr
        '
        Me.txtMinimumXCorr.Enabled = False
        Me.txtMinimumXCorr.Location = New System.Drawing.Point(173, 18)
        Me.txtMinimumXCorr.Name = "txtMinimumXCorr"
        Me.txtMinimumXCorr.Size = New System.Drawing.Size(67, 22)
        Me.txtMinimumXCorr.TabIndex = 1
        Me.txtMinimumXCorr.Text = "2.2"
        '
        'pnlInputFile
        '
        Me.pnlInputFile.Controls.Add(Me.txtDataFilePath)
        Me.pnlInputFile.Controls.Add(Me.cmdSelectFile)
        Me.pnlInputFile.Dock = System.Windows.Forms.DockStyle.Top
        Me.pnlInputFile.Location = New System.Drawing.Point(0, 0)
        Me.pnlInputFile.Name = "pnlInputFile"
        Me.pnlInputFile.Size = New System.Drawing.Size(711, 46)
        Me.pnlInputFile.TabIndex = 9
        '
        'pnlSICs
        '
        Me.pnlSICs.Controls.Add(Me.lblParentIon)
        Me.pnlSICs.Controls.Add(Me.lstParentIonData)
        Me.pnlSICs.Controls.Add(Me.fraSortOrderAndStats)
        Me.pnlSICs.Dock = System.Windows.Forms.DockStyle.Left
        Me.pnlSICs.Location = New System.Drawing.Point(0, 46)
        Me.pnlSICs.Name = "pnlSICs"
        Me.pnlSICs.Size = New System.Drawing.Size(346, 411)
        Me.pnlSICs.TabIndex = 10
        '
        'pnlNavigationAndOptions
        '
        Me.pnlNavigationAndOptions.Controls.Add(Me.fraNavigation)
        Me.pnlNavigationAndOptions.Controls.Add(Me.TabControl1)
        Me.pnlNavigationAndOptions.Dock = System.Windows.Forms.DockStyle.Left
        Me.pnlNavigationAndOptions.Location = New System.Drawing.Point(346, 46)
        Me.pnlNavigationAndOptions.Name = "pnlNavigationAndOptions"
        Me.pnlNavigationAndOptions.Size = New System.Drawing.Size(336, 411)
        Me.pnlNavigationAndOptions.TabIndex = 11
        '
        'pnlBottom
        '
        Me.pnlBottom.Controls.Add(Me.fraPeakFinder)
        Me.pnlBottom.Controls.Add(Me.fraResmoothingOptions)
        Me.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.pnlBottom.Location = New System.Drawing.Point(0, 457)
        Me.pnlBottom.Name = "pnlBottom"
        Me.pnlBottom.Size = New System.Drawing.Size(711, 175)
        Me.pnlBottom.TabIndex = 12
        '
        'frmBrowser
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(6, 15)
        Me.ClientSize = New System.Drawing.Size(711, 632)
        Me.Controls.Add(Me.pnlNavigationAndOptions)
        Me.Controls.Add(Me.pnlSICs)
        Me.Controls.Add(Me.pnlInputFile)
        Me.Controls.Add(Me.pnlBottom)
        Me.Menu = Me.MainMenuControl
        Me.MinimumSize = New System.Drawing.Size(614, 0)
        Me.Name = "frmBrowser"
        Me.Text = "MASIC Browser"
        CType(Me.tmrAutoStep, ComponentModel.ISupportInitialize).EndInit()
        Me.fraNavigation.ResumeLayout(False)
        Me.fraNavigation.PerformLayout()
        Me.fraResmoothingOptions.ResumeLayout(False)
        Me.fraResmoothingOptions.PerformLayout()
        Me.fraPeakFinder.ResumeLayout(False)
        Me.fraSortOrderAndStats.ResumeLayout(False)
        Me.fraSortOrderAndStats.PerformLayout()
        Me.TabControl1.ResumeLayout(False)
        Me.tpSICFilters.ResumeLayout(False)
        Me.tpSICFilters.PerformLayout()
        Me.tpMsMsSearchResultsFilters.ResumeLayout(False)
        Me.tpMsMsSearchResultsFilters.PerformLayout()
        Me.pnlInputFile.ResumeLayout(False)
        Me.pnlInputFile.PerformLayout()
        Me.pnlSICs.ResumeLayout(False)
        Me.pnlNavigationAndOptions.ResumeLayout(False)
        Me.pnlBottom.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

#End Region

    Friend WithEvents lblParentIon As Label
    Friend WithEvents cmdSelectFile As Button
    Friend WithEvents lblSortOrder As Label
    Friend WithEvents cboSortOrder As ComboBox
    Friend WithEvents chkFixXRange As CheckBox
    Friend WithEvents txtFixXRange As TextBox
    Friend WithEvents lblFixXRange As Label
    Friend WithEvents lblMinimumIntensity As Label
    Friend WithEvents txtMinimumIntensity As TextBox
    Friend WithEvents chkFilterByIntensity As CheckBox
    Friend WithEvents tmrAutoStep As System.Timers.Timer
    Friend WithEvents fraNavigation As GroupBox
    Friend WithEvents chkAutoStepForward As CheckBox
    Friend WithEvents txtAutoStep As TextBox
    Friend WithEvents lblAutoStep As Label
    Friend WithEvents cmdAutoStep As Button
    Friend WithEvents cmdPrevious As Button
    Friend WithEvents cmdNext As Button
    Friend WithEvents cmdJump As Button
    Friend WithEvents txtFilterByMZ As TextBox
    Friend WithEvents lblFilterByMZ As Label
    Friend WithEvents chkFilterByMZ As CheckBox
    Friend WithEvents txtFilterByMZTol As TextBox
    Friend WithEvents lblFilterByMZTolUnits As Label
    Friend WithEvents lblFilterByMZTol As Label
    Friend WithEvents txtFixYRange As TextBox
    Friend WithEvents lblFixYRange As Label
    Friend WithEvents chkFixYRange As CheckBox
    Friend WithEvents lblSICsTypeFilter As Label
    Friend WithEvents cboSICsTypeFilter As ComboBox
    Friend WithEvents MainMenuControl As MainMenu
    Friend WithEvents mnuFile As MenuItem
    Friend WithEvents mnuFileExit As MenuItem
    Friend WithEvents mnuFileSelectMASICInputFile As MenuItem
    Friend WithEvents mnuFileSep1 As MenuItem
    Friend WithEvents mnuEdit As MenuItem
    Friend WithEvents mnuHelp As MenuItem
    Friend WithEvents mnuHelpAbout As MenuItem
    Friend WithEvents mnuEditShowOptimalPeakApexCursor As MenuItem
    Friend WithEvents chkSortDescending As CheckBox
    Friend WithEvents lstParentIonData As ListBox
    Friend WithEvents fraResmoothingOptions As GroupBox
    Friend WithEvents optUseSavitzkyGolaySmooth As RadioButton
    Friend WithEvents txtButterworthSamplingFrequency As TextBox
    Friend WithEvents lblButterworthSamplingFrequency As Label
    Friend WithEvents txtSavitzkyGolayFilterOrder As TextBox
    Friend WithEvents lblSavitzkyGolayFilterOrder As Label
    Friend WithEvents optUseButterworthSmooth As RadioButton
    Friend WithEvents optDoNotResmooth As RadioButton
    Friend WithEvents txtPeakWidthPointsMinimum As TextBox
    Friend WithEvents lblPeakWidthPointsMinimum As Label
    Friend WithEvents fraPeakFinder As GroupBox
    Friend WithEvents chkUsePeakFinder As CheckBox
    Friend WithEvents chkFindPeaksSubtractBaseline As CheckBox
    Friend WithEvents txtMinimumSignalToNoise As TextBox
    Friend WithEvents chkFilterBySignalToNoise As CheckBox
    Friend WithEvents cmdRedoSICPeakFindingAllData As Button
    Friend WithEvents fraSortOrderAndStats As GroupBox
    Friend WithEvents mnuFileSelectMSMSSearchResultsFile As MenuItem
    Friend WithEvents chkShowSmoothedData As CheckBox
    Friend WithEvents TabControl1 As TabControl
    Friend WithEvents tpSICFilters As TabPage
    Friend WithEvents tpMsMsSearchResultsFilters As TabPage
    Friend WithEvents txtMinimumXCorr As TextBox
    Friend WithEvents txtSequenceFilter As TextBox
    Friend WithEvents lblMinimumXCorr As Label
    Friend WithEvents lblChargeFilter As Label
    Friend WithEvents TextBox2 As TextBox
    Friend WithEvents lblSequenceFilter As Label
    Friend WithEvents chkSequenceFilterExactMatch As CheckBox
    Friend WithEvents txtStats1 As TextBox
    Friend WithEvents txtStats3 As TextBox
    Friend WithEvents txtStats2 As TextBox
    Friend WithEvents chkShowBaselineCorrectedStats As CheckBox
    Friend WithEvents pnlInputFile As Panel
    Friend WithEvents pnlSICs As Panel
    Friend WithEvents pnlNavigationAndOptions As Panel
    Friend WithEvents pnlBottom As Panel
    Friend WithEvents txtDataFilePath As TextBox

End Class
