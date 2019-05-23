' This program will read the _SICs.xml data file created by MASIC to allow
'   for browsing of the spectra
'
' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started October 17, 2003
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

' E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
' Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
' -------------------------------------------------------------------------------
'
' Licensed under the 2-Clause BSD License; you may Not use this file except
' in compliance with the License.  You may obtain a copy of the License at
' https://opensource.org/licenses/BSD-2-Clause

Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Timers
Imports System.Xml
Imports DataFilter
Imports MASICPeakFinder
Imports OxyDataPlotter
Imports OxyPlot
Imports PRISM
Imports ProgressFormNET

Public Class frmBrowser
    Inherits Form

    Private Const PROGRAM_DATE As String = "May 23, 2019"

#Region "Windows Form Designer generated code"

    Public Sub New()
        MyBase.New()

        Application.EnableVisualStyles()
        Application.DoEvents()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call
        InitializeControls()

    End Sub

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

#Region "Constants and Enums"

    Private Const REG_APP_NAME As String = "MASICBrowser"
    Private Const REG_SECTION_NAME As String = "Options"

    Private Const TABLE_NAME_MSMS_RESULTS As String = "T_MsMsResults"
    Private Const TABLE_NAME_SEQUENCES As String = "T_Sequences"
    Private Const TABLE_NAME_SEQ_TO_PROTEIN_MAP As String = "T_Seq_to_Protein_Map"

    Private Const COL_NAME_SCAN As String = "Scan"
    Private Const COL_NAME_CHARGE As String = "Charge"
    Private Const COL_NAME_MH As String = "MH"
    Private Const COL_NAME_XCORR As String = "XCorr"
    Private Const COL_NAME_DELTACN As String = "DeltaCN"
    Private Const COL_NAME_DELTACN2 As String = "DeltaCn2"
    Private Const COL_NAME_RANK_SP As String = "RankSp"
    Private Const COL_NAME_RANK_XC As String = "RankXc"
    Private Const COL_NAME_SEQUENCE_ID As String = "SeqID"
    Private Const COL_NAME_PARENT_ION_INDEX As String = "ParentIonIndex"

    Private Const COL_NAME_SEQUENCE As String = "Sequence"
    Private Const COL_NAME_PROTEIN As String = "Protein"

    'Private Const COL_NAME_MULTI_PROTEIN_COUNT As String = "MultiProteinCount"

    Private Const SORT_ORDER_MODE_COUNT As Integer = 18
    Private Enum eSortOrderConstants
        SortByPeakIndex = 0
        SortByScanPeakCenter = 1
        SortByScanOptimalPeakCenter = 2
        SortByMz = 3
        SortByPeakSignalToNoise = 4
        SortByBaselineCorrectedPeakIntensity = 5
        SortByBaselineCorrectedPeakArea = 6
        SortByPeakWidth = 7
        SortBySICIntensityMax = 8
        SortByPeakIntensity = 9
        SortByPeakArea = 10
        SortByFragScanToOptimalLocDistance = 11
        SortByPeakCenterToOptimalLocDistance = 12
        SortByShoulderCount = 13
        SortByParentIonIntensity = 14
        SortByPeakSkew = 15
        SortByKSStat = 16
        SortByBaselineNoiseLevel = 17
    End Enum

    Private Enum eSICTypeFilterConstants
        AllSICs = 0
        NoCustomSICs = 1
        CustomSICsOnly = 2
    End Enum

    Private Enum eSmoothModeConstants
        DoNotReSmooth = 0
        Butterworth = 1
        SavitzkyGolay = 2
    End Enum

    Private Enum eMsMsSearchEngineResultColumns
        RowIndex = 0
        Scan
        NumberOfScans
        Charge
        MH
        XCorr
        DeltaCN
        SP
        Protein
        MultiProteinCount
        Sequence
        DeltaCn2
        RankSp
        RankXc
        DelM
        XcRatio
        PassFilt
        MScore
        NTT
    End Enum

    Private Enum eCurrentXMLDataFileSectionConstants As Integer
        UnknownFile = 0
        Start = 1
        Options = 2
        ParentIons = 3
    End Enum

#End Region

#Region "Properties"
    Public Property FileToAutoLoad As String
#End Region

#Region "Classwide Variables"

    Private mSpectrum As Spectrum

    Private mParentIonStats As List(Of clsParentIonStats)

    Private mMsMsResults As DataSet

    Private mSICPeakFinderOptions As clsSICPeakFinderOptions

    Private mParentIonPointerArrayCount As Integer          ' Could be less than mParentIonStats.Count if filtering the data
    Private mParentIonPointerArray() As Integer             ' Pointer array used for de-referencing cboParentIon.SelectedItem to mParentIonStats

    Private mAutoStepEnabled As Boolean
    Private mAutoStepIntervalMsec As Integer

    Private mLastUpdate As DateTime
    Private mMASICPeakFinder As clsMASICPeakFinder

    Private mLastErrorNotification As DateTime

    Private mFileLoadTimer As Windows.Forms.Timer

#End Region

    Private Sub AutoOpenMsMsResults(masicFilePath As String, ByRef objProgress As frmProgress)
        ' Look for a corresponding Synopsis or First hits file in the same folder as masicFilePath

        Dim dataDirectoryInfo As New DirectoryInfo(Path.GetDirectoryName(masicFilePath))
        Dim fileNameToFind As String
        Dim fileNameBase As String

        fileNameBase = Path.GetFileNameWithoutExtension(masicFilePath)
        If fileNameBase.ToLower.EndsWith("_sics") Then
            fileNameBase = fileNameBase.Substring(0, fileNameBase.Length - 5)
        End If

        fileNameToFind = fileNameBase & "_fht.txt"
        fileNameToFind = Path.Combine(dataDirectoryInfo.FullName, fileNameToFind)

        txtStats3.Visible = False
        If File.Exists(fileNameToFind) Then
            ReadMsMsSearchEngineResults(fileNameToFind, objProgress)
        Else
            fileNameToFind = fileNameBase & "_syn.txt"
            fileNameToFind = Path.Combine(dataDirectoryInfo.FullName, fileNameToFind)

            If File.Exists(fileNameToFind) Then
                ReadMsMsSearchEngineResults(fileNameToFind, objProgress)
                txtStats3.Visible = True
            End If
        End If

        PositionControls()
    End Sub

    Private Sub CheckAutoStep()
        If DateTime.Now().Subtract(mLastUpdate).TotalMilliseconds >= mAutoStepIntervalMsec Then
            mLastUpdate = mLastUpdate.AddMilliseconds(mAutoStepIntervalMsec)
            NavigateScanList(chkAutoStepForward.Checked)
            Application.DoEvents()
        End If
    End Sub

    Private Sub ClearMsMsResults()
        With mMsMsResults
            .Tables(TABLE_NAME_MSMS_RESULTS).Clear()
            .Tables(TABLE_NAME_SEQ_TO_PROTEIN_MAP).Clear()
        End With
        InitializeSequencesDataTable()
    End Sub

    Private Sub DefineDefaultSortDirection()

        Dim eSortOrder As eSortOrderConstants

        Try
            eSortOrder = CType(cboSortOrder.SelectedIndex, eSortOrderConstants)

            Select Case eSortOrder
                Case eSortOrderConstants.SortByMz, eSortOrderConstants.SortByPeakIndex, eSortOrderConstants.SortByScanOptimalPeakCenter, eSortOrderConstants.SortByScanPeakCenter, eSortOrderConstants.SortByPeakSkew, eSortOrderConstants.SortByKSStat
                    chkSortDescending.Checked = False
                Case Else
                    ' Sort the others descending by default
                    chkSortDescending.Checked = True
            End Select
        Catch ex As Exception
            ' Ignore any errors
        End Try

    End Sub

    Private Sub DisplaySICStats(parentIonIndex As Integer, <Out()> ByRef sicStats As clsSICStats)
        ' udtSICStats will be populated with either the original SIC stats found by MASIC or with the
        '  updated SIC stats if chkUsePeakFinder is Checked
        ' Also, if re-smooth data is enabled, then the SIC data will be re-smoothed

        Dim eSmoothMode As eSmoothModeConstants
        Dim validPeakFound As Boolean

        Dim intensityToDisplay As Double
        Dim areaToDisplay As Double

        Dim statDetails As String

        UpdateSICPeakFinderOptions()

        If parentIonIndex >= 0 And parentIonIndex < mParentIonStats.Count Then
            If optUseButterworthSmooth.Checked Then
                eSmoothMode = eSmoothModeConstants.Butterworth
            ElseIf optUseSavitzkyGolaySmooth.Checked Then
                eSmoothMode = eSmoothModeConstants.SavitzkyGolay
            Else
                eSmoothMode = eSmoothModeConstants.DoNotReSmooth
            End If

            validPeakFound = UpdateSICStats(parentIonIndex, chkUsePeakFinder.Checked, eSmoothMode, sicStats)

            ' Display the SIC and SIC Peak stats
            With mParentIonStats(parentIonIndex)
                statDetails = String.Empty
                statDetails &= "Index: " & .Index.ToString
                If .OptimalPeakApexScanNumber = .SICStats.ScanNumberMaxIntensity Then
                    statDetails &= ControlChars.NewLine & "Scan at apex: " & .SICStats.ScanNumberMaxIntensity
                Else
                    statDetails &= ControlChars.NewLine & "Scan at apex: " & .OptimalPeakApexScanNumber & " (Original apex: " & .SICStats.ScanNumberMaxIntensity & ")"
                End If
            End With

            If validPeakFound Then
                statDetails &= ControlChars.NewLine & "Center of mass: " & sicStats.Peak.StatisticalMoments.CenterOfMassScan.ToString
                If chkShowBaselineCorrectedStats.Checked Then
                    intensityToDisplay = clsMASICPeakFinder.BaselineAdjustIntensity(sicStats.Peak, False)
                    areaToDisplay = clsMASICPeakFinder.BaselineAdjustArea(sicStats.Peak, mParentIonStats(parentIonIndex).SICStats.SICPeakWidthFullScans, False)
                Else
                    intensityToDisplay = sicStats.Peak.MaxIntensityValue
                    areaToDisplay = sicStats.Peak.Area
                End If
                statDetails &= ControlChars.NewLine & "Intensity: " & StringUtilities.ValueToString(intensityToDisplay, 4)
                statDetails &= ControlChars.NewLine & "Area: " & StringUtilities.ValueToString(areaToDisplay, 4)
                statDetails &= ControlChars.NewLine & "FWHM: " & sicStats.Peak.FWHMScanWidth.ToString
            Else
                statDetails &= ControlChars.NewLine & "Could not find a valid SIC peak"
            End If

            If mParentIonStats(parentIonIndex).CustomSICPeak Then
                statDetails &= ControlChars.NewLine & "Custom SIC: " & mParentIonStats(parentIonIndex).CustomSICPeakComment
            End If

            txtStats1.Text = statDetails

            statDetails = "m/z: " & mParentIonStats(parentIonIndex).MZ
            If validPeakFound Then
                With sicStats.Peak.StatisticalMoments
                    statDetails &= ControlChars.NewLine & "Peak StDev: " & StringUtilities.ValueToString(.StDev, 3)
                    statDetails &= ControlChars.NewLine & "Peak Skew: " & StringUtilities.ValueToString(.Skew, 4)
                    statDetails &= ControlChars.NewLine & "Peak KSStat: " & StringUtilities.ValueToString(.KSStat, 4)
                    statDetails &= ControlChars.NewLine & "Data Count Used: " & .DataCountUsed
                End With

                If sicStats.Peak.SignalToNoiseRatio >= 3 Then
                    statDetails &= ControlChars.NewLine & "S/N: " & Math.Round(sicStats.Peak.SignalToNoiseRatio, 0).ToString
                Else
                    statDetails &= ControlChars.NewLine & "S/N: " & StringUtilities.ValueToString(sicStats.Peak.SignalToNoiseRatio, 4)
                End If

                With sicStats.Peak.BaselineNoiseStats
                    statDetails &= ControlChars.NewLine & "Noise level: " & StringUtilities.ValueToString(.NoiseLevel, 4)
                    statDetails &= ControlChars.NewLine & "Noise StDev: " & StringUtilities.ValueToString(.NoiseStDev, 3)
                    statDetails &= ControlChars.NewLine & "Points used: " & .PointsUsed.ToString
                    statDetails &= ControlChars.NewLine & "Noise Mode Used: " & .NoiseThresholdModeUsed.ToString
                End With

            End If
            txtStats2.Text = statDetails

            txtStats3.Text = LookupSequenceForParentIonIndex(parentIonIndex)
        Else
            txtStats1.Text = "Invalid parent ion index: " & parentIonIndex.ToString
            txtStats2.Text = String.Empty
            txtStats3.Text = String.Empty
            sicStats = New clsSICStats()
        End If

    End Sub

    Private Sub DisplaySICStatsForSelectedParentIon()
        Dim sicStats As clsSICStats = Nothing

        If mParentIonPointerArrayCount > 0 Then
            DisplaySICStats(mParentIonPointerArray(lstParentIonData.SelectedIndex), sicStats)
        End If
    End Sub
    Private Sub EnableDisableControls()

        Dim useButterworth As Boolean
        Dim useSavitzkyGolay As Boolean

        If optDoNotResmooth.Checked Then
            useButterworth = False
            useSavitzkyGolay = False
        ElseIf optUseButterworthSmooth.Checked Then
            useButterworth = True
            useSavitzkyGolay = False
        ElseIf optUseSavitzkyGolaySmooth.Checked Then
            useButterworth = False
            useSavitzkyGolay = True
        End If

        txtButterworthSamplingFrequency.Enabled = useButterworth
        txtSavitzkyGolayFilterOrder.Enabled = useSavitzkyGolay


    End Sub

    Private Sub FindMinimumPotentialPeakAreaInRegion(
      parentIonIndexStart As Integer,
      parentIonIndexEnd As Integer,
      potentialAreaStatsForRegion As clsSICPotentialAreaStats)

        ' This function finds the minimum potential peak area in the parent ions between
        '  parentIonIndexStart and parentIonIndexEnd
        ' However, the summed intensity is not used if the number of points >= .SICNoiseThresholdIntensity is less than Minimum_Peak_Width

        Dim parentIonIndex As Integer

        With potentialAreaStatsForRegion
            .MinimumPotentialPeakArea = Double.MaxValue
            .PeakCountBasisForMinimumPotentialArea = 0
        End With

        For parentIonIndex = parentIonIndexStart To parentIonIndexEnd
            With mParentIonStats(parentIonIndex)

                If Math.Abs(.SICStats.SICPotentialAreaStatsForPeak.MinimumPotentialPeakArea) < Single.Epsilon And
                   .SICStats.SICPotentialAreaStatsForPeak.PeakCountBasisForMinimumPotentialArea = 0 Then
                    ' Need to compute the minimum potential peak area for parentIonIndex

                    ' Compute the potential peak area for this SIC
                    mMASICPeakFinder.FindPotentialPeakArea(.SICData, .SICStats.SICPotentialAreaStatsForPeak, mSICPeakFinderOptions)
                End If


                With .SICStats.SICPotentialAreaStatsForPeak

                    If .MinimumPotentialPeakArea > 0 And .PeakCountBasisForMinimumPotentialArea >= clsMASICPeakFinder.MINIMUM_PEAK_WIDTH Then
                        If .PeakCountBasisForMinimumPotentialArea > potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea Then
                            ' The non valid peak count value is larger than the one associated with the current
                            '  minimum potential peak area; update the minimum peak area to potentialPeakArea
                            potentialAreaStatsForRegion.MinimumPotentialPeakArea = .MinimumPotentialPeakArea
                            potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = .PeakCountBasisForMinimumPotentialArea
                        Else
                            If .MinimumPotentialPeakArea < potentialAreaStatsForRegion.MinimumPotentialPeakArea And
                               .PeakCountBasisForMinimumPotentialArea >= potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea Then
                                potentialAreaStatsForRegion.MinimumPotentialPeakArea = .MinimumPotentialPeakArea
                                potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = .PeakCountBasisForMinimumPotentialArea
                            End If
                        End If
                    End If
                End With
            End With
        Next

        If potentialAreaStatsForRegion.MinimumPotentialPeakArea > Double.MaxValue - 1 Then
            potentialAreaStatsForRegion.MinimumPotentialPeakArea = 1
        End If

    End Sub

    Private Function FindSICPeakAndAreaForParentIon(parentIonIndex As Integer, sicStats As clsSICStats) As Boolean

        Dim index As Integer
        Dim parentIonIndexStart As Integer

        Dim sicPotentialAreaStatsForRegion = New clsSICPotentialAreaStats()
        Dim returnClosestPeak As Boolean

        Dim recomputeNoiseLevel As Boolean

        Try
            ' Determine the minimum potential peak area in the last 500 scans
            parentIonIndexStart = parentIonIndex - 500
            If parentIonIndexStart < 0 Then parentIonIndexStart = 0
            FindMinimumPotentialPeakAreaInRegion(parentIonIndexStart, parentIonIndex, sicPotentialAreaStatsForRegion)

            Dim parentIon = mParentIonStats(parentIonIndex)

            returnClosestPeak = Not parentIon.CustomSICPeak

            ' Look for .SurveyScanNumber in .SICScans in order to populate sicStats.Peak.IndexObserved
            If sicStats.Peak.IndexObserved = 0 Then
                For index = 0 To parentIon.SICData.Count - 1
                    If parentIon.SICData(index).ScanNumber = parentIon.SurveyScanNumber Then
                        sicStats.Peak.IndexObserved = index
                    End If
                Next
            End If

            ' Determine the value for .ParentIonIntensity
            mMASICPeakFinder.ComputeParentIonIntensity(parentIon.SICData, sicStats.Peak, parentIon.FragScanObserved)

            If parentIon.SICStats.Peak.BaselineNoiseStats.NoiseThresholdModeUsed = clsMASICPeakFinder.eNoiseThresholdModes.DualTrimmedMeanByAbundance Then
                recomputeNoiseLevel = False
            Else
                recomputeNoiseLevel = True
                ' Note: We cannot use DualTrimmedMeanByAbundance since we don't have access to the full-length SICs
                If mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = clsMASICPeakFinder.eNoiseThresholdModes.DualTrimmedMeanByAbundance Then
                    mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance
                End If
            End If

            Dim smoothedYDataSubset As clsSmoothedYDataSubset = Nothing

            mMASICPeakFinder.FindSICPeakAndArea(parentIon.SICData, sicStats.SICPotentialAreaStatsForPeak, sicStats.Peak,
                                                smoothedYDataSubset, mSICPeakFinderOptions,
                                                sicPotentialAreaStatsForRegion,
                                                returnClosestPeak, False, recomputeNoiseLevel)


            ' Copy the data out of smoothedYDataSubset and into sicStats.SICSmoothedYData
            With smoothedYDataSubset
                sicStats.SICSmoothedYData.Clear()

                For Each dataPoint In parentIon.SICData
                    sicStats.SICSmoothedYData.Add(dataPoint.Intensity)
                Next

                sicStats.SICSmoothedYDataIndexStart = .DataStartIndex
            End With

            Try
                ' Update the two computed values
                With sicStats
                    .SICPeakWidthFullScans = mParentIonStats(parentIonIndex).SICData(.Peak.IndexBaseRight).ScanNumber -
                                             mParentIonStats(parentIonIndex).SICData(.Peak.IndexBaseLeft).ScanNumber + 1

                    .ScanNumberMaxIntensity = mParentIonStats(parentIonIndex).SICData(.Peak.IndexMax).ScanNumber
                End With
            Catch ex As Exception
                ' Index out of range; ignore the error
            End Try

            Return True

        Catch ex As Exception
            MessageBox.Show("Error in FindSICPeakAndAreaForParentIon: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return False
        End Try

    End Function

    Private Sub FindSimilarParentIon()
        Const SIMILAR_MZ_TOLERANCE = 0.2

        FindSimilarParentIon(SIMILAR_MZ_TOLERANCE)
    End Sub

    Private Sub FindSimilarParentIon(similarMZTolerance As Double)

        Const DEFAULT_INTENSITY As Double = 1

        Try
            If mParentIonStats Is Nothing OrElse mParentIonStats.Count = 0 Then
                Exit Sub
            End If

            Dim similarFragScans = New List(Of Integer)

            For parentIonIndex = 0 To mParentIonStats.Count - 1

                Dim currentParentIon = mParentIonStats(parentIonIndex)
                currentParentIon.SimilarFragScans.Clear()

                If currentParentIon.SICData.Count = 0 Then
                    Continue For
                End If

                Dim parentIonMZ = currentParentIon.MZ
                Dim scanStart = currentParentIon.SICData.First().ScanNumber
                Dim scanEnd = currentParentIon.SICData.Last().ScanNumber

                currentParentIon.SimilarFragScans.Clear()

                ' Always store this parent ion's fragmentation scan in similarFragScans
                ' Note that it's possible for .FragScanObserved to be outside the range of scanStart and scanEnd
                ' We'll allow this, but won't be able to properly determine the abundance value to use for .SimilarFragScanPlottingIntensity()
                similarFragScans.Clear()
                similarFragScans.Add(currentParentIon.FragScanObserved)

                ' Step through the parent ions and look for others with a similar m/z and a fragmentation scan
                '   between scanStart and scanEnd

                For indexCompare = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(indexCompare)
                        If indexCompare <> parentIonIndex AndAlso
                           (.FragScanObserved >= scanStart AndAlso .FragScanObserved <= scanEnd) Then

                            If Math.Abs(.MZ - parentIonMZ) <= similarMZTolerance Then
                                ' Similar parent ion m/z found

                                similarFragScans.Add(.FragScanObserved)
                            End If
                        End If
                    End With
                Next

                If similarFragScans.Count = 0 Then
                    Continue For
                End If

                ' Sort the data in similarFragScans
                similarFragScans.Sort()

                ' Copy the sorted data into .SimilarFragScans
                ' When copying, make sure we don't have any duplicates
                ' Also, populate SimilarFragScanPlottingIntensity

                Dim scanNumbers = (From item In currentParentIon.SICData Select item.ScanNumber).ToList()

                For Each similarFragScan In similarFragScans

                    If currentParentIon.SimilarFragScans.Count > 0 Then
                        If similarFragScan = currentParentIon.SimilarFragScans.Last().ScanNumber Then
                            Continue For
                        End If
                    End If

                    ' Look for similarFragScan in .SICScans() then use the corresponding
                    '  intensity value in .SICData()

                    Dim matchIndex = clsBinarySearch.BinarySearchFindNearest(scanNumbers,
                                                                                similarFragScan,
                                                                                scanNumbers.Count,
                                                                                clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint)

                    If matchIndex < 0 Then
                        ' Match not found; find the closest match via brute-force searching
                        matchIndex = -1
                        For scanIndex = 0 To scanNumbers.Count - 2
                            If scanNumbers(scanIndex) <= similarFragScan AndAlso
                               scanNumbers(scanIndex + 1) >= similarFragScan Then
                                matchIndex = scanIndex
                                Exit For
                            End If
                        Next
                    End If

                    Dim interpolatedYValue = DEFAULT_INTENSITY

                    If matchIndex >= 0 Then

                        If matchIndex < scanNumbers.Count - 1 Then
                            If similarFragScan <= currentParentIon.SICData(matchIndex).ScanNumber Then
                                ' Frag scan is at or before the first scan number in .SICData()
                                ' Use the intensity at .SICData(0)
                                interpolatedYValue = currentParentIon.SICData(0).Intensity
                            Else
                                Dim success = InterpolateY(
                                    interpolatedYValue,
                                    currentParentIon.SICData(matchIndex).ScanNumber,
                                    currentParentIon.SICData(matchIndex + 1).ScanNumber,
                                    currentParentIon.SICData(matchIndex).Intensity,
                                    currentParentIon.SICData(matchIndex + 1).Intensity,
                                    similarFragScan)

                                If Not success Then
                                    interpolatedYValue = DEFAULT_INTENSITY
                                End If
                            End If
                        Else
                            ' Frag scan is at or after the last scan number in .SICData()
                            ' Use the last data point in .SICData()
                            interpolatedYValue = currentParentIon.SICData.Last().Intensity
                        End If
                    End If

                    Dim newDataPoint = New clsSICDataPoint(similarFragScan, interpolatedYValue, 0)
                    currentParentIon.SimilarFragScans.Add(newDataPoint)

                Next

            Next

        Catch ex As Exception
            MessageBox.Show("Error in FindSimilarParentIon: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try
    End Sub

    Private Function GetSettingVal(appName As String, sectionName As String, key As String, DefaultValue As Boolean) As Boolean
        Dim value As String

        value = GetSetting(appName, sectionName, key, DefaultValue.ToString)
        Try
            Return CBool(value)
        Catch ex As Exception
            Return DefaultValue
        End Try

    End Function

    Private Function GetSettingVal(appName As String, sectionName As String, key As String, DefaultValue As Integer) As Integer
        Dim value As String

        value = GetSetting(appName, sectionName, key, DefaultValue.ToString)
        Try
            Return CInt(value)
        Catch ex As Exception
            Return DefaultValue
        End Try

    End Function

    Private Function GetSettingVal(appName As String, sectionName As String, key As String, DefaultValue As Single) As Single
        Dim value As String

        value = GetSetting(appName, sectionName, key, DefaultValue.ToString)
        Try
            Return CSng(value)
        Catch ex As Exception
            Return DefaultValue
        End Try

    End Function

    Private Function GetSortKey(value1 As Integer, value2 As Integer) As Double
        Return CDbl(value1.ToString() & "." & value2.ToString("000000"))
    End Function

    Private Function GetSortKey(value1 As Integer, value2 As Double) As Double
        Return CDbl(value1.ToString() & "." & value2.ToString("000000"))
    End Function

    Private Sub InitializeControls()
        mMASICPeakFinder = New clsMASICPeakFinder()
        AddHandler mMASICPeakFinder.ErrorEvent, AddressOf MASICPeakFinderErrorHandler

        mSICPeakFinderOptions = clsMASICPeakFinder.GetDefaultSICPeakFinderOptions

        mParentIonStats = New List(Of clsParentIonStats)

        InitializeMsMsResultsStorage()
        PopulateComboBoxes()
        RegReadSettings()
        fraNavigation.Enabled = False

        mAutoStepIntervalMsec = 200

        tmrAutoStep.Interval = 10
        tmrAutoStep.Enabled = True

        optUseButterworthSmooth.Checked = True

        SetToolTips()

        txtStats3.Visible = False
        PositionControls()

        mFileLoadTimer = New Windows.Forms.Timer()

        AddHandler mFileLoadTimer.Tick, AddressOf mFileLoadTimer_Tick

        mFileLoadTimer.Interval = 500
        mFileLoadTimer.Start()


    End Sub

    Private Sub InitializeMsMsResultsStorage()

        '---------------------------------------------------------
        ' Create the MS/MS Search Engine Results DataTable
        '---------------------------------------------------------
        Dim msmsResultsTable = New DataTable(TABLE_NAME_MSMS_RESULTS)

        ' Add the columns to the DataTable
        DatabaseUtils.DataTableUtils.AppendColumnIntegerToTable(msmsResultsTable, COL_NAME_SCAN)
        DatabaseUtils.DataTableUtils.AppendColumnIntegerToTable(msmsResultsTable, COL_NAME_CHARGE)
        DatabaseUtils.DataTableUtils.AppendColumnFloatToTable(msmsResultsTable, COL_NAME_MH)
        DatabaseUtils.DataTableUtils.AppendColumnFloatToTable(msmsResultsTable, COL_NAME_XCORR)
        DatabaseUtils.DataTableUtils.AppendColumnFloatToTable(msmsResultsTable, COL_NAME_DELTACN)
        DatabaseUtils.DataTableUtils.AppendColumnFloatToTable(msmsResultsTable, COL_NAME_DELTACN2)
        DatabaseUtils.DataTableUtils.AppendColumnIntegerToTable(msmsResultsTable, COL_NAME_RANK_SP)
        DatabaseUtils.DataTableUtils.AppendColumnIntegerToTable(msmsResultsTable, COL_NAME_RANK_XC)
        DatabaseUtils.DataTableUtils.AppendColumnIntegerToTable(msmsResultsTable, COL_NAME_SEQUENCE_ID)
        DatabaseUtils.DataTableUtils.AppendColumnIntegerToTable(msmsResultsTable, COL_NAME_PARENT_ION_INDEX)

        ' Define a primary key
        With msmsResultsTable
            Dim PrimaryKeyColumn = New DataColumn() { .Columns(COL_NAME_SCAN), .Columns(COL_NAME_CHARGE), .Columns(COL_NAME_SEQUENCE_ID)}
            .PrimaryKey = PrimaryKeyColumn
        End With

        ' Instantiate the DataSet
        mMsMsResults = New DataSet("MsMsData")

        ' Add the table to the DataSet
        mMsMsResults.Tables.Add(msmsResultsTable)

        '---------------------------------------------------------
        ' Create the Sequence to Protein Map DataTable
        '---------------------------------------------------------
        Dim seqToProteinMapTable = New DataTable(TABLE_NAME_SEQ_TO_PROTEIN_MAP)

        ' Add the columns to the DataTable
        DatabaseUtils.DataTableUtils.AppendColumnIntegerToTable(seqToProteinMapTable, COL_NAME_SEQUENCE_ID)
        DatabaseUtils.DataTableUtils.AppendColumnStringToTable(seqToProteinMapTable, COL_NAME_PROTEIN)

        ' Define a primary key
        With seqToProteinMapTable
            Dim PrimaryKeyColumn = New DataColumn() { .Columns(COL_NAME_SEQUENCE_ID), .Columns(COL_NAME_PROTEIN)}
            .PrimaryKey = PrimaryKeyColumn
        End With

        ' Add the table to the DataSet
        mMsMsResults.Tables.Add(seqToProteinMapTable)


        '---------------------------------------------------------
        ' Create the Sequences DataTable
        '---------------------------------------------------------
        InitializeSequencesDataTable()

    End Sub

    Private Sub InitializeSequencesDataTable()
        Dim sequenceInfoTable = New DataTable(TABLE_NAME_SEQUENCES)

        ' Add the columns to the DataTable
        DatabaseUtils.DataTableUtils.AppendColumnIntegerToTable(sequenceInfoTable, COL_NAME_SEQUENCE_ID, 0, True, True, True)
        DatabaseUtils.DataTableUtils.AppendColumnStringToTable(sequenceInfoTable, COL_NAME_SEQUENCE)

        ' Define a primary key
        With sequenceInfoTable
            Dim PrimaryKeyColumn = New DataColumn() { .Columns(COL_NAME_SEQUENCE)}
            .PrimaryKey = PrimaryKeyColumn
        End With

        ' Add the table to the DataSet
        If mMsMsResults.Tables.Contains(TABLE_NAME_SEQUENCES) Then
            mMsMsResults.Tables.Remove(TABLE_NAME_SEQUENCES)
        End If

        mMsMsResults.Tables.Add(sequenceInfoTable)

    End Sub

    Private Function InterpolateY(<Out()> ByRef interpolatedYValue As Double, X1 As Integer, X2 As Integer, Y1 As Double, Y2 As Double, targetX As Integer) As Boolean
        ' Checks if X1 or X2 is less than targetX
        ' If it is, then determines the Y value that corresponds to targetX by interpolating the line between (X1, Y1) and (X2, Y2)
        '
        ' Returns True if a match is found; otherwise, returns false

        Dim deltaY As Double
        Dim fraction As Double
        Dim deltaX As Integer
        Dim targetY As Double

        If X1 < targetX Or X2 < targetX Then
            If X1 < targetX And X2 < targetX Then
                ' Both of the X values are less than targetX
                ' We cannot interpolate
                Debug.Assert(False, "This code should normally not be reached (frmBrowser->InterpolateY)")
            Else
                deltaX = X2 - X1
                fraction = (targetX - X1) / CDbl(deltaX)
                deltaY = Y2 - Y1

                targetY = fraction * deltaY + Y1

                If Math.Abs(targetY - Y1) >= 0 And Math.Abs(targetY - Y2) >= 0 Then
                    interpolatedYValue = targetY
                    Return True
                Else
                    Debug.Assert(False, "TargetY is not between Y1 and Y2; this shouldn't happen (frmBrowser->InterpolateY)")
                    interpolatedYValue = 0
                    Return False
                End If

            End If
        End If

        interpolatedYValue = 0
        Return False

    End Function

    Private Sub JumpToScan(Optional scanNumberToFind As Integer = -1)

        Try
            If mParentIonPointerArrayCount > 0 Then
                If scanNumberToFind < 0 Then
                    If lstParentIonData.SelectedIndex >= 0 And lstParentIonData.SelectedIndex <= mParentIonPointerArrayCount Then
                        scanNumberToFind = mParentIonStats(lstParentIonData.SelectedIndex).FragScanObserved
                    Else
                        scanNumberToFind = mParentIonStats(0).FragScanObserved
                    End If

                    Dim eResponse = InputBox("Enter the scan number to jump to: ", "Jump to Scan", scanNumberToFind.ToString)
                    If DataUtils.StringToValueUtils.IsNumber(eResponse) Then
                        scanNumberToFind = CInt(eResponse)
                    End If
                End If

                If scanNumberToFind >= 0 Then
                    ' Find the best match to scanNumberToFind
                    ' First search for an exact match
                    Dim indexMatch = -1
                    For index = 0 To lstParentIonData.Items.Count - 1
                        If mParentIonStats(mParentIonPointerArray(index)).FragScanObserved = scanNumberToFind Then
                            indexMatch = index
                            Exit For
                        End If
                    Next

                    If indexMatch >= 0 Then
                        lstParentIonData.SelectedIndex = indexMatch
                    Else
                        ' Exact match not found; find the closest match to scanNumberToFind
                        Dim scanDifference = Integer.MaxValue
                        For index = 0 To lstParentIonData.Items.Count - 1
                            If Math.Abs(mParentIonStats(mParentIonPointerArray(index)).FragScanObserved - scanNumberToFind) < scanDifference Then
                                scanDifference = Math.Abs(mParentIonStats(mParentIonPointerArray(index)).FragScanObserved - scanNumberToFind)
                                indexMatch = index
                            End If
                        Next

                        If indexMatch >= 0 Then
                            lstParentIonData.SelectedIndex = indexMatch
                        Else
                            ' Match was not found
                            ' Jump to the last entry
                            lstParentIonData.SelectedIndex = lstParentIonData.Items.Count - 1
                        End If
                    End If
                End If
            Else
                MessageBox.Show("No data is in memory", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As Exception
            ' Ignore any errors in this sub
        End Try

    End Sub

    Private Function LookupSequenceForParentIonIndex(parentIonIndex As Integer) As String

        Dim sequenceID As Integer
        Dim sequenceCount As Integer

        Dim objRows() As DataRow
        Dim objSeqRows() As DataRow

        Dim objRow As DataRow

        Dim sequences As String = String.Empty

        With mMsMsResults.Tables(TABLE_NAME_MSMS_RESULTS)
            Try
                objRows = .Select(COL_NAME_PARENT_ION_INDEX & " = " & parentIonIndex.ToString)
                sequenceCount = 0
                For Each objRow In objRows
                    sequenceID = CInt(objRow.Item(COL_NAME_SEQUENCE_ID))
                    Try
                        objSeqRows = mMsMsResults.Tables(TABLE_NAME_SEQUENCES).Select(COL_NAME_SEQUENCE_ID & " = " & sequenceID.ToString)

                        If Not objSeqRows Is Nothing AndAlso objSeqRows.Length > 0 Then
                            If sequenceCount > 0 Then
                                sequences &= ControlChars.NewLine
                            End If
                            sequences &= CStr(objSeqRows(0).Item(COL_NAME_SEQUENCE))
                            sequenceCount += 1
                        End If
                    Catch ex As Exception
                        ' Ignore errors here
                    End Try
                Next objRow
            Catch ex As Exception
                sequences = String.Empty
            End Try
        End With

        Return sequences

    End Function

    Private Function LookupSequenceID(sequence As String, protein As String) As Integer
        ' Looks for sequence in mMsMsResults.Tables(TABLE_NAME_SEQUENCES)
        ' Returns the SequenceID if found; adds it if not present
        ' Additionally, adds a mapping between sequence and protein in mMsMsResults.Tables(TABLE_NAME_SEQ_TO_PROTEIN_MAP)

        Dim sequenceNoSuffixes As String
        Dim objNewRow As DataRow
        Dim sequenceID As Integer

        sequenceID = -1

        If Not sequence Is Nothing Then
            If sequence.Length >= 4 Then
                If sequence.Substring(1, 1) = "."c And sequence.Substring(sequence.Length - 2, 1) = "."c Then
                    sequenceNoSuffixes = sequence.Substring(2, sequence.Length - 4)
                Else
                    sequenceNoSuffixes = String.Copy(sequence)
                End If
            Else
                sequenceNoSuffixes = String.Copy(sequence)
            End If

            ' Try to add sequenceNoSuffixes to .Tables(TABLE_NAME_SEQUENCES)
            Try
                With mMsMsResults.Tables(TABLE_NAME_SEQUENCES)
                    objNewRow = .Rows.Find(sequenceNoSuffixes)
                    If objNewRow Is Nothing Then
                        objNewRow = .NewRow
                        objNewRow.Item(COL_NAME_SEQUENCE) = sequenceNoSuffixes
                        .Rows.Add(objNewRow)
                    End If
                    sequenceID = CInt(objNewRow.Item(COL_NAME_SEQUENCE_ID))
                End With

                If sequenceID >= 0 Then
                    Try
                        ' Possibly add sequenceNoSuffixes and protein to .Tables(TABLE_NAME_SEQ_TO_PROTEIN_MAP)
                        With mMsMsResults.Tables(TABLE_NAME_SEQ_TO_PROTEIN_MAP)
                            If Not .Rows.Contains(New Object() {sequenceID, protein}) Then
                                objNewRow = .NewRow
                                objNewRow.Item(COL_NAME_SEQUENCE_ID) = sequenceID
                                objNewRow.Item(COL_NAME_PROTEIN) = protein
                                .Rows.Add(objNewRow)
                            End If
                        End With
                    Catch ex As Exception
                        ' Ignore errors here
                    End Try
                End If
            Catch ex As Exception
                sequenceID = -1
            End Try
        End If

        Return sequenceID

    End Function

    Private Sub NavigateScanList(moveForward As Boolean)
        If moveForward Then
            If lstParentIonData.SelectedIndex < lstParentIonData.Items.Count - 1 Then
                lstParentIonData.SelectedIndex += 1
            Else
                If mAutoStepEnabled Then ToggleAutoStep(True)
            End If
        Else
            If lstParentIonData.SelectedIndex > 0 Then
                lstParentIonData.SelectedIndex -= 1
            Else
                If mAutoStepEnabled Then ToggleAutoStep(True)
            End If
        End If
    End Sub

    Private Sub PlotData(indexToPlot As Integer, sicStats As clsSICStats)
        ' indexToPlot points to an entry in mParentIonStats()

        ' We plot the data as two different series to allow for different coloring
        Dim dataCountSeries1, dataCountSeries2, dataCountSeries3, dataCountSeries4 As Integer
        Dim xDataSeries1(), yDataSeries1() As Double          ' Holds the scans and SIC data for data <=0 (data not part of the peak)
        Dim xDataSeries2(), yDataSeries2() As Double          ' Holds the scans and SIC data for data > 0 (data part of the peak)
        Dim xDataSeries3(), yDataSeries3() As Double          ' Holds the scan numbers at which the given m/z was chosen for fragmentation
        Dim xDataSeries4(), yDataSeries4() As Double          ' Holds the smoothed SIC data

        Try
            If indexToPlot < 0 Or indexToPlot >= mParentIonStats.Count Then Return

            Me.Cursor = Cursors.WaitCursor

            If mSpectrum Is Nothing Then
                mSpectrum = New Spectrum()
                mSpectrum.SetSpectrumFormWindowCaption("Selected Ion Chromatogram")
            End If

            mSpectrum.RemoveAllAnnotations()

            Dim currentParentIon = mParentIonStats(indexToPlot)

            If currentParentIon.SICData.Count = 0 Then Return

            dataCountSeries1 = 0
            dataCountSeries2 = 0
            dataCountSeries3 = currentParentIon.SimilarFragScans.Count
            dataCountSeries4 = 0

            With currentParentIon
                ReDim xDataSeries1(.SICData.Count + 3)   ' Need extra room for potential zero padding
                ReDim yDataSeries1(.SICData.Count + 3)

                ReDim xDataSeries2(.SICData.Count + 3)
                ReDim yDataSeries2(.SICData.Count + 3)

                ReDim xDataSeries3(.SimilarFragScans.Count - 1)
                ReDim yDataSeries3(.SimilarFragScans.Count - 1)

                ReDim xDataSeries4(.SICData.Count)
                ReDim yDataSeries4(.SICData.Count)
            End With

            Dim zeroEdgeSeries1 As Boolean

            If sicStats.Peak.IndexBaseLeft = 0 Then
                ' Zero pad Series 1
                xDataSeries1(0) = currentParentIon.SICData(0).ScanNumber
                yDataSeries1(0) = 0
                dataCountSeries1 += 1

                ' Zero pad Series 2
                xDataSeries2(0) = currentParentIon.SICData(0).ScanNumber
                yDataSeries2(0) = 0
                dataCountSeries2 += 1

                zeroEdgeSeries1 = True
            End If

            ' Initialize this to 0, in case .FragScanObserved is out of range
            Dim scanObservedIntensity As Double = 0

            ' Initialize this to the maximum intensity, in case .OptimalPeakApexScanNumber is out of range
            Dim optimalPeakApexIntensity As Double = currentParentIon.SICIntensityMax

            Dim smoothedYDataIndexStart As Integer
            Dim smoothedYData() As Double

            If sicStats.SICSmoothedYData Is Nothing OrElse sicStats.SICSmoothedYData.Count = 0 Then
                smoothedYDataIndexStart = 0
                ReDim smoothedYData(-1)
            Else
                smoothedYDataIndexStart = sicStats.SICSmoothedYDataIndexStart

                ReDim smoothedYData(sicStats.SICSmoothedYData.Count - 1)
                For index = 0 To sicStats.SICSmoothedYData.Count - 1
                    smoothedYData(index) = sicStats.SICSmoothedYData(index)
                Next
            End If

            ' Populate Series 3 with the similar frag scan values
            For index = 0 To currentParentIon.SimilarFragScans.Count - 1
                xDataSeries3(index) = currentParentIon.SimilarFragScans(index).ScanNumber
                yDataSeries3(index) = currentParentIon.SimilarFragScans(index).Intensity
            Next

            For index = 0 To currentParentIon.SICData.Count - 1
                Dim interpolatedYValue As Double

                If index < currentParentIon.SICData.Count - 1 Then
                    With currentParentIon
                        If .SICData(index).ScanNumber <= .FragScanObserved And .SICData(index + 1).ScanNumber >= .FragScanObserved Then
                            ' Use the survey scan data to calculate the appropriate intensity for the Frag Scan cursor

                            If InterpolateY(interpolatedYValue, .SICData(index).ScanNumber, .SICData(index + 1).ScanNumber, .SICData(index).Intensity, .SICData(index + 1).Intensity, .FragScanObserved) Then
                                scanObservedIntensity = interpolatedYValue
                            End If
                        End If

                        If .SICData(index).ScanNumber <= .OptimalPeakApexScanNumber And .SICData(index + 1).ScanNumber >= .OptimalPeakApexScanNumber Then
                            ' Use the survey scan data to calculate the appropriate intensity for the Optimal Peak Apex Scan cursor

                            If InterpolateY(interpolatedYValue, .SICData(index).ScanNumber, .SICData(index + 1).ScanNumber, .SICData(index).Intensity, .SICData(index + 1).Intensity, .OptimalPeakApexScanNumber) Then
                                optimalPeakApexIntensity = interpolatedYValue
                            End If
                        End If
                    End With

                End If

                If index >= sicStats.Peak.IndexBaseLeft And index <= sicStats.Peak.IndexBaseRight Then
                    If index > 0 And Not zeroEdgeSeries1 Then
                        ' Zero pad Series 1
                        xDataSeries1(dataCountSeries1) = currentParentIon.SICData(index).ScanNumber
                        yDataSeries1(dataCountSeries1) = currentParentIon.SICData(index).Intensity
                        dataCountSeries1 += 1

                        xDataSeries1(dataCountSeries1) = currentParentIon.SICData(index).ScanNumber
                        yDataSeries1(dataCountSeries1) = 0
                        dataCountSeries1 += 1

                        ' Zero pad Series 2
                        xDataSeries2(dataCountSeries2) = currentParentIon.SICData(index).ScanNumber
                        yDataSeries2(dataCountSeries2) = 0
                        dataCountSeries2 += 1

                        zeroEdgeSeries1 = True
                    End If

                    xDataSeries2(dataCountSeries2) = currentParentIon.SICData(index).ScanNumber
                    yDataSeries2(dataCountSeries2) = currentParentIon.SICData(index).Intensity
                    dataCountSeries2 += 1
                Else
                    If index > 0 And zeroEdgeSeries1 Then
                        ' Zero pad Series 2
                        xDataSeries2(dataCountSeries2) = currentParentIon.SICData(index - 1).ScanNumber
                        yDataSeries2(dataCountSeries2) = 0
                        dataCountSeries2 += 1

                        ' Zero pad Series 1
                        xDataSeries1(dataCountSeries1) = currentParentIon.SICData(index - 1).ScanNumber
                        yDataSeries1(dataCountSeries1) = 0
                        dataCountSeries1 += 1

                        xDataSeries1(dataCountSeries1) = currentParentIon.SICData(index - 1).ScanNumber
                        yDataSeries1(dataCountSeries1) = currentParentIon.SICData(index - 1).Intensity
                        dataCountSeries1 += 1
                        zeroEdgeSeries1 = False
                    End If

                    xDataSeries1(dataCountSeries1) = currentParentIon.SICData(index).ScanNumber
                    yDataSeries1(dataCountSeries1) = currentParentIon.SICData(index).Intensity
                    dataCountSeries1 += 1
                End If

                If index >= smoothedYDataIndexStart AndAlso Not smoothedYData Is Nothing AndAlso
                   index - smoothedYDataIndexStart < smoothedYData.Length Then
                    xDataSeries4(dataCountSeries4) = currentParentIon.SICData(index).ScanNumber
                    yDataSeries4(dataCountSeries4) = smoothedYData(index - smoothedYDataIndexStart)
                    dataCountSeries4 += 1
                End If
            Next

            ' Shrink the data arrays
            ' SIC Data
            ReDim Preserve xDataSeries1(dataCountSeries1 - 1)
            ReDim Preserve yDataSeries1(dataCountSeries1 - 1)

            ' SIC Peak
            ReDim Preserve xDataSeries2(dataCountSeries2 - 1)
            ReDim Preserve yDataSeries2(dataCountSeries2 - 1)

            ' Smoothed Data
            ReDim Preserve xDataSeries4(dataCountSeries4 - 1)
            ReDim Preserve yDataSeries4(dataCountSeries4 - 1)

            mSpectrum.ShowSpectrum()

            mSpectrum.SetDataXvsY(1, xDataSeries1, yDataSeries1, dataCountSeries1, ctlOxyPlotControl.SeriesPlotMode.PointsAndLines, "SIC Data")

            Dim peakCaption As String

            If sicStats.Peak.ShoulderCount = 1 Then
                peakCaption = "SIC Data Peak (1 shoulder peak)"
            ElseIf sicStats.Peak.ShoulderCount > 1 Then
                peakCaption = "SIC Data Peak (" & sicStats.Peak.ShoulderCount & " shoulder peaks)"
            Else
                peakCaption = "SIC Data Peak"
            End If
            mSpectrum.SetDataXvsY(2, xDataSeries2, yDataSeries2, dataCountSeries2, ctlOxyPlotControl.SeriesPlotMode.PointsAndLines, peakCaption)

            Dim fragScansCaption = "Similar Frag scans"
            mSpectrum.SetDataXvsY(3, xDataSeries3, yDataSeries3, dataCountSeries3, ctlOxyPlotControl.SeriesPlotMode.Points, fragScansCaption)

            If chkShowSmoothedData.Checked AndAlso xDataSeries4.Length > 0 Then
                mSpectrum.SetDataXvsY(4, xDataSeries4, yDataSeries4, dataCountSeries4, ctlOxyPlotControl.SeriesPlotMode.Lines, "Smoothed data")
            Else
                Do While mSpectrum.GetSeriesCount >= 4
                    mSpectrum.RemoveSeries(4)
                Loop
            End If

            Dim actualSeriesCount = mSpectrum.GetSeriesCount()

            mSpectrum.SetSeriesLineStyle(1, LineStyle.Automatic)
            mSpectrum.SetSeriesLineStyle(2, LineStyle.Automatic)

            mSpectrum.SetSeriesPointStyle(1, MarkerType.Diamond)
            mSpectrum.SetSeriesPointStyle(2, MarkerType.Square)
            mSpectrum.SetSeriesPointStyle(3, MarkerType.Circle)

            mSpectrum.SetSeriesColor(1, Color.Blue)
            mSpectrum.SetSeriesColor(2, Color.Red)
            mSpectrum.SetSeriesColor(3, Color.FromArgb(255, 20, 210, 20))

            mSpectrum.SetSeriesLineWidth(1, 1)
            mSpectrum.SetSeriesLineWidth(2, 2)

            mSpectrum.SetSeriesPointSize(3, 7)

            If actualSeriesCount > 3 Then
                mSpectrum.SetSeriesLineStyle(4, LineStyle.Automatic)
                mSpectrum.SetSeriesPointStyle(4, MarkerType.None)
                mSpectrum.SetSeriesColor(4, Color.Purple)
                mSpectrum.SetSeriesLineWidth(4, 2)
            End If

            Dim arrowLengthPixels = 15
            Dim captionOffsetDirection As ctlOxyPlotControl.CaptionOffsetDirection

            If currentParentIon.FragScanObserved <= currentParentIon.OptimalPeakApexScanNumber Then
                captionOffsetDirection = ctlOxyPlotControl.CaptionOffsetDirection.TopLeft
            Else
                captionOffsetDirection = ctlOxyPlotControl.CaptionOffsetDirection.TopRight
            End If

            Dim fragScanObserved = currentParentIon.FragScanObserved

            Const seriesToUse = 0
            mSpectrum.SetAnnotationForDataPoint(fragScanObserved, scanObservedIntensity, "MS2",
                                                seriesToUse, captionOffsetDirection, arrowLengthPixels, )

            If mnuEditShowOptimalPeakApexCursor.Checked Then
                mSpectrum.SetAnnotationForDataPoint(
                    currentParentIon.OptimalPeakApexScanNumber, optimalPeakApexIntensity, "Peak",
                    2, ctlOxyPlotControl.CaptionOffsetDirection.TopLeft, arrowLengthPixels)
            End If

            Dim xRangeHalfWidth As Integer
            If DataUtils.StringToValueUtils.IsNumber(txtFixXRange.Text) Then
                xRangeHalfWidth = CInt(CInt(txtFixXRange.Text) / 2)
            Else
                xRangeHalfWidth = 0
            End If

            Dim yRange As Double
            If DataUtils.StringToValueUtils.IsNumber(txtFixYRange.Text) Then
                yRange = CDbl(txtFixYRange.Text)
            Else
                yRange = 0
            End If

            mSpectrum.SetLabelXAxis("Scan number")
            mSpectrum.SetLabelYAxis("Intensity")

            ' Update the axis padding
            mSpectrum.XAxisPaddingMinimum = 0.01
            mSpectrum.XAxisPaddingMaximum = 0.01

            mSpectrum.YAxisPaddingMinimum = 0.02
            mSpectrum.YAxisPaddingMaximum = 0.15

            If chkFixXRange.Checked And xRangeHalfWidth > 0 Then
                mSpectrum.SetAutoscaleXAxis(False)
                mSpectrum.SetRangeX(sicStats.ScanNumberMaxIntensity - xRangeHalfWidth, sicStats.ScanNumberMaxIntensity + xRangeHalfWidth)
            Else
                mSpectrum.SetAutoscaleXAxis(True)
            End If

            If chkFixYRange.Checked And yRange > 0 Then
                mSpectrum.SetAutoscaleYAxis(False)
                mSpectrum.SetRangeY(0, yRange)
            Else
                mSpectrum.SetAutoscaleYAxis(True)
            End If

        Catch ex As Exception
            Dim sTrace = StackTraceFormatter.GetExceptionStackTraceMultiLine(ex)
            MessageBox.Show("Error in PlotData: " & ex.Message & ControlChars.CrLf & sTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        Finally
            Me.Cursor = Cursors.Default
        End Try

    End Sub

    Private Sub PopulateComboBoxes()

        With cboSortOrder
            With .Items
                .Clear()
                .Insert(eSortOrderConstants.SortByPeakIndex, "Sort by Scan of Peak Index")
                .Insert(eSortOrderConstants.SortByScanPeakCenter, "Sort by Scan of Peak Center")
                .Insert(eSortOrderConstants.SortByScanOptimalPeakCenter, "Sort by Scan of Optimal Peak Apex")
                .Insert(eSortOrderConstants.SortByMz, "Sort by m/z")
                .Insert(eSortOrderConstants.SortByPeakSignalToNoise, "Sort by Peak Signal/Noise")
                .Insert(eSortOrderConstants.SortByBaselineCorrectedPeakIntensity, "Sort by Baseline-corrected Intensity")
                .Insert(eSortOrderConstants.SortByBaselineCorrectedPeakArea, "Sort by Baseline-corrected Area")
                .Insert(eSortOrderConstants.SortByPeakWidth, "Sort by Peak FWHM (Width)")
                .Insert(eSortOrderConstants.SortBySICIntensityMax, "Sort by SIC Max Intensity")
                .Insert(eSortOrderConstants.SortByPeakIntensity, "Sort by Peak Intensity (uncorrected for noise)")
                .Insert(eSortOrderConstants.SortByPeakArea, "Sort by Peak Area (uncorrected for noise)")
                .Insert(eSortOrderConstants.SortByFragScanToOptimalLocDistance, "Sort by Frag Scan to Optimal Loc Distance")
                .Insert(eSortOrderConstants.SortByPeakCenterToOptimalLocDistance, "Sort by Peak Center to Optimal Loc Distance")
                .Insert(eSortOrderConstants.SortByShoulderCount, "Sort by Shoulder Peak Count")
                .Insert(eSortOrderConstants.SortByParentIonIntensity, "Sort by Parent Ion Intensity")
                .Insert(eSortOrderConstants.SortByPeakSkew, "Sort by Peak Skew")
                .Insert(eSortOrderConstants.SortByKSStat, "Sort by Peak KS Stat")
                .Insert(eSortOrderConstants.SortByBaselineNoiseLevel, "Sort by Baseline Noise level")
            End With
            .SelectedIndex = eSortOrderConstants.SortByPeakSignalToNoise
        End With

        With cboSICsTypeFilter
            With .Items
                .Clear()
                .Insert(eSICTypeFilterConstants.AllSICs, "All SIC's")
                .Insert(eSICTypeFilterConstants.NoCustomSICs, "No custom SIC's")
                .Insert(eSICTypeFilterConstants.CustomSICsOnly, "Custom SIC's only")

            End With
            .SelectedIndex = eSICTypeFilterConstants.AllSICs
        End With

    End Sub

    Private Sub PopulateParentIonIndexColumnInMsMsResultsTable()
        ' For each row in mMsMsResults.Tables(TABLE_NAME_MSMS_RESULTS), find the corresponding row in mParentIonStats

        ' Construct a mapping between .FragScanObserved and Index in mParentIonStats
        ' If multiple parent ions have the same value for .FragScanObserved, then the we will only track the mapping to the first one

        Dim index As Integer

        Dim htFragScanToIndex As New Hashtable
        Dim objRow As DataRow

        For index = 0 To mParentIonStats.Count - 1
            If Not htFragScanToIndex.ContainsKey(mParentIonStats(index).FragScanObserved) Then
                htFragScanToIndex.Add(mParentIonStats(index).FragScanObserved, index)
            End If
        Next

        For Each objRow In mMsMsResults.Tables(TABLE_NAME_MSMS_RESULTS).Rows
            If htFragScanToIndex.Contains(objRow.Item(COL_NAME_SCAN)) Then
                objRow.Item(COL_NAME_PARENT_ION_INDEX) = htFragScanToIndex(objRow.Item(COL_NAME_SCAN))
            End If
        Next objRow
    End Sub

    Private Sub PopulateSpectrumList(scanNumberToHighlight As Integer)

        Dim index As Integer

        Dim parentIonDesc As String

        Try
            lstParentIonData.Items.Clear()

            If mParentIonPointerArrayCount > 0 Then
                For index = 0 To mParentIonPointerArrayCount - 1
                    With mParentIonStats(mParentIonPointerArray(index))
                        parentIonDesc = "Scan " & .FragScanObserved.ToString & "  (" & Math.Round(.MZ, 4).ToString & " m/z)"
                        lstParentIonData.Items.Add(parentIonDesc)
                    End With
                Next

                lstParentIonData.SelectedIndex = 0

                JumpToScan(scanNumberToHighlight)

                fraNavigation.Enabled = True
            Else
                fraNavigation.Enabled = False
            End If

        Catch ex As Exception
            MessageBox.Show("Error in PopulateSpectrumList: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try
    End Sub

    Private Sub PositionControls()
        Dim desiredValue As Integer

        'desiredValue = Me.Height - lstParentIonData.Top - 425
        'If desiredValue < 5 Then
        '    desiredValue = 5
        'End If
        'lstParentIonData.Height = desiredValue

        'fraSortOrderAndStats.Top = lstParentIonData.Top + lstParentIonData.Height + 1
        If txtStats3.Visible Then
            desiredValue = fraSortOrderAndStats.Height - txtStats1.Top - txtStats3.Height - 1 - 5
        Else
            desiredValue = fraSortOrderAndStats.Height - txtStats1.Top - 1 - 5
        End If
        If desiredValue < 1 Then desiredValue = 1
        txtStats1.Height = desiredValue
        txtStats2.Height = desiredValue

        txtStats3.Top = txtStats1.Top + txtStats1.Height + 1

    End Sub

    Private Sub ReadDataFileXMLTextReader(filePath As String)

        Dim eCurrentXMLDataFileSection As eCurrentXMLDataFileSectionConstants

        Dim indexInXMLFile = "-1"
        Dim validParentIon As Boolean

        Dim percentComplete As Double
        Dim errorMessages = New List(Of String)

        Dim similarIonMZToleranceHalfWidth As Single
        Dim findPeaksOnSmoothedData As Boolean
        Dim useButterworthSmooth, useSavitzkyGolaySmooth As Boolean
        Dim butterworthSamplingFrequency As Single
        Dim savitzkyGolayFilterOrder As Integer

        Dim scanStart As Integer
        Dim peakScanStart, peakScanEnd As Integer
        Dim index As Integer
        Dim charIndex As Integer
        Dim interval As Integer

        Dim smoothedDataFound As Boolean
        Dim baselineNoiseStatsFound As Boolean

        Dim value As String

        Dim scanIntervals As String = String.Empty
        Dim intensityDataList As String = String.Empty
        Dim massDataList As String = String.Empty
        Dim smoothedYDataList As String = String.Empty

        Dim delimiters As String = " ,;" & ControlChars.Tab
        Dim delimiterList As Char() = delimiters.ToCharArray()
        Dim valueList() As String

        Dim expectedSicDataCount = 0

        If Not File.Exists(filePath) Then
            Exit Sub
        End If

        ' Initialize the progress form
        Dim objProgress As frmProgress
        objProgress = New frmProgress

        Try

            ' Initialize the stream reader and the XML Text Reader
            Using reader = New StreamReader(filePath),
              objXMLReader = New XmlTextReader(reader)


                objProgress.InitializeProgressForm("Reading file " & ControlChars.NewLine & PRISM.PathUtils.CompactPathString(filePath, 40), 0, 1, True)
                objProgress.Show()
                Application.DoEvents()

                ' Initialize mParentIonStats
                mParentIonStats.Clear()

                eCurrentXMLDataFileSection = eCurrentXMLDataFileSectionConstants.UnknownFile
                validParentIon = False

                Do While objXMLReader.Read()

                    XMLTextReaderSkipWhitespace(objXMLReader)
                    If Not objXMLReader.ReadState = ReadState.Interactive Then Exit Do

                    If objXMLReader.Depth < 2 Then
                        If objXMLReader.NodeType = XmlNodeType.Element Then
                            Select Case objXMLReader.Name
                                Case "ParentIon"
                                    eCurrentXMLDataFileSection = eCurrentXMLDataFileSectionConstants.ParentIons
                                    validParentIon = False

                                    If objXMLReader.HasAttributes Then
                                        baselineNoiseStatsFound = False
                                        scanStart = 0
                                        peakScanStart = 0
                                        peakScanEnd = 0

                                        scanIntervals = String.Empty
                                        intensityDataList = String.Empty
                                        massDataList = String.Empty
                                        smoothedYDataList = String.Empty

                                        indexInXMLFile = objXMLReader.GetAttribute("Index")

                                        Dim newParentIon = New clsParentIonStats With {
                                            .Index = Integer.Parse(indexInXMLFile),
                                            .MZ = 0,
                                            .SICIntensityMax = 0
                                        }

                                        With newParentIon.SICStats
                                            .SICPeakWidthFullScans = 0
                                            With .Peak
                                                .IndexBaseLeft = -1
                                                .IndexBaseRight = -1
                                                .MaxIntensityValue = 0
                                                .ShoulderCount = 0
                                            End With
                                        End With

                                        mParentIonStats.Add(newParentIon)
                                        validParentIon = True

                                        ' Update the progress bar
                                        percentComplete = reader.BaseStream.Position / reader.BaseStream.Length
                                        If percentComplete > 1 Then percentComplete = 1

                                        objProgress.UpdateProgressBar(percentComplete)
                                        Application.DoEvents()
                                        If objProgress.KeyPressAbortProcess Then Exit Do

                                        ' Advance to the next tag
                                        objXMLReader.Read()
                                        XMLTextReaderSkipWhitespace(objXMLReader)
                                        If Not objXMLReader.ReadState = ReadState.Interactive Then Exit Do

                                    Else
                                        ' Attribute isn't present; skip this parent ion
                                        indexInXMLFile = "-1"
                                    End If


                                Case "SICData"
                                    eCurrentXMLDataFileSection = eCurrentXMLDataFileSectionConstants.Start
                                Case "ProcessingSummary"
                                    objXMLReader.Skip()
                                Case "MemoryOptions"
                                    objXMLReader.Skip()
                                Case "SICOptions"
                                    eCurrentXMLDataFileSection = eCurrentXMLDataFileSectionConstants.Options
                                Case "ProcessingStats"
                                    objXMLReader.Skip()
                            End Select
                        ElseIf objXMLReader.NodeType = XmlNodeType.EndElement Then
                            If objXMLReader.Name = "ParentIon" Then
                                If validParentIon Then
                                    ' End element found for the current parent ion

                                    Dim currentParentIon = mParentIonStats.Last()

                                    ' Split apart the value list variables

                                    Dim sicScans = New List(Of Integer)
                                    Dim sicIntensities = New List(Of Double)
                                    Dim sicMasses = New List(Of Double)

                                    sicScans.Add(scanStart)

                                    ' scanIntervals contains the intervals from each scan to the next
                                    ' If the interval is <=9, then it is stored as a number
                                    ' For intervals between 10 and 35, uses letters A to Z
                                    ' For intervals between 36 and 61, uses letters A to Z
                                    If Not scanIntervals Is Nothing Then
                                        For charIndex = 1 To scanIntervals.Length - 1
                                            If Char.IsNumber(scanIntervals.Chars(charIndex)) Then
                                                interval = CInt(scanIntervals.Substring(charIndex, 1))
                                            Else
                                                If Char.IsUpper(scanIntervals.Chars(charIndex)) Then
                                                    ' Uppercase letter
                                                    interval = Asc(scanIntervals.Chars(charIndex)) - 55
                                                ElseIf Char.IsLower(scanIntervals.Chars(charIndex)) Then
                                                    ' Lowercase letter
                                                    interval = Asc(scanIntervals.Chars(charIndex)) - 61
                                                Else
                                                    ' Not a letter or a number; unknown interval (use 1)
                                                    interval = 1
                                                End If

                                            End If

                                            sicScans.Add(sicScans(charIndex - 1) + interval)
                                        Next
                                    Else
                                        errorMessages.Add("Missing 'SICScanInterval' node for parent ion '" & indexInXMLFile & "'")
                                    End If


                                    ' Split apart the Intensity data list using the delimiters in delimiterList
                                    If Not intensityDataList Is Nothing Then
                                        valueList = intensityDataList.Trim.Split(delimiterList)
                                        For index = 0 To valueList.Length - 1
                                            If DataUtils.StringToValueUtils.IsNumber(valueList(index)) Then
                                                sicIntensities.Add(CDbl(valueList(index)))
                                            Else
                                                sicIntensities.Add(0)
                                            End If
                                        Next
                                    Else
                                        errorMessages.Add("Missing 'IntensityDataList' node for parent ion '" & indexInXMLFile & "'")
                                    End If

                                    ' Split apart the Mass data list using the delimiters in delimiterList
                                    If Not massDataList Is Nothing Then
                                        valueList = massDataList.Trim.Split(delimiterList)
                                        For index = 0 To valueList.Length - 1
                                            If DataUtils.StringToValueUtils.IsNumber(valueList(index)) Then
                                                sicMasses.Add(CDbl(valueList(index)))
                                            Else
                                                sicMasses.Add(0)
                                            End If
                                        Next
                                    Else
                                        errorMessages.Add("Missing 'IntensityDataList' node for parent ion '" & indexInXMLFile & "'")
                                    End If

                                    For index = 0 To sicScans.Count - 1

                                        If index = sicIntensities.Count Then
                                            Exit For
                                        End If

                                        Dim massValue As Double = 0
                                        If index < sicMasses.Count Then
                                            massValue = sicMasses(index)
                                        End If

                                        Dim newDataPoint = New clsSICDataPoint(sicScans(index), sicIntensities(index), massValue)
                                        currentParentIon.SICData.Add(newDataPoint)
                                    Next

                                    If sicIntensities.Count > sicScans.Count Then
                                        errorMessages.Add("Too many intensity data points found in parent ion '" & indexInXMLFile & "'")
                                    End If

                                    If sicMasses.Count > sicScans.Count Then
                                        errorMessages.Add("Too many mass data points found in parent ion '" & indexInXMLFile & "'")
                                    End If

                                    Dim sicData = currentParentIon.SICData

                                    If expectedSicDataCount > 0 AndAlso expectedSicDataCount <> currentParentIon.SICData.Count Then
                                        errorMessages.Add("Actual SICDataCount (" & currentParentIon.SICData.Count & ") did not match expected count (" & expectedSicDataCount & ")")
                                    End If

                                    ' Split apart the smoothed Y data using the delimiters in delimiterList
                                    If Not smoothedYDataList Is Nothing Then
                                        valueList = smoothedYDataList.Trim.Split(delimiterList)

                                        For index = 0 To valueList.Length - 1
                                            If index >= sicData.Count Then
                                                errorMessages.Add("Too many intensity data points found in parent ion '" & indexInXMLFile & "'")
                                                Exit For
                                            End If
                                            If DataUtils.StringToValueUtils.IsNumber(valueList(index)) Then
                                                currentParentIon.SICStats.SICSmoothedYData.Add(CDbl(valueList(index)))
                                                smoothedDataFound = True
                                            Else
                                                currentParentIon.SICStats.SICSmoothedYData.Add(0)
                                            End If
                                        Next

                                    Else
                                        ' No smoothed Y data; that's OK
                                    End If

                                    ' Update the peak stats variables
                                    currentParentIon.SICIntensityMax = (From item In sicData Select item.Intensity).Max()

                                    If currentParentIon.SICStats.Peak.IndexBaseLeft < 0 Then
                                        currentParentIon.SICStats.Peak.IndexBaseLeft = 0
                                    Else
                                        If currentParentIon.SICStats.Peak.IndexBaseLeft < sicData.Count Then
                                            If peakScanStart <> sicData(currentParentIon.SICStats.Peak.IndexBaseLeft).ScanNumber Then
                                                errorMessages.Add("PeakScanStart does not agree with SICPeakIndexStart for parent ion ' " & indexInXMLFile & "'")
                                            End If
                                        Else
                                            errorMessages.Add(".SICStats.Peak.IndexBaseLeft is larger than .SICScans.Length for parent ion ' " & indexInXMLFile & "'")
                                        End If
                                    End If

                                    If currentParentIon.SICStats.Peak.IndexBaseRight < 0 Then
                                        currentParentIon.SICStats.Peak.IndexBaseRight = sicData.Count - 1
                                    Else
                                        If currentParentIon.SICStats.Peak.IndexBaseRight < sicData.Count Then
                                            If peakScanEnd <> sicData(currentParentIon.SICStats.Peak.IndexBaseRight).ScanNumber Then
                                                errorMessages.Add("PeakScanEnd does not agree with SICPeakIndexEnd for parent ion ' " & indexInXMLFile & "'")
                                            End If
                                        Else
                                            errorMessages.Add(".SICStats.Peak.IndexBaseRight is larger than .SICScans.Length for parent ion ' " & indexInXMLFile & "'")
                                        End If
                                    End If

                                    If currentParentIon.SICStats.Peak.IndexBaseRight < sicData.Count AndAlso
                                       currentParentIon.SICStats.Peak.IndexBaseLeft < sicData.Count Then
                                        currentParentIon.SICStats.SICPeakWidthFullScans = sicData(currentParentIon.SICStats.Peak.IndexBaseRight).ScanNumber - sicData(currentParentIon.SICStats.Peak.IndexBaseLeft).ScanNumber + 1
                                    End If

                                    If Not baselineNoiseStatsFound Then
                                        ' Compute the Noise Threshold for this SIC
                                        ' Note: We cannot use DualTrimmedMeanByAbundance since we don't have access to the full-length SICs
                                        If mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = clsMASICPeakFinder.eNoiseThresholdModes.DualTrimmedMeanByAbundance Then
                                            mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance
                                        End If

                                        mMASICPeakFinder.ComputeNoiseLevelInPeakVicinity(sicData, currentParentIon.SICStats.Peak, mSICPeakFinderOptions.SICBaselineNoiseOptions)
                                    End If


                                End If
                                validParentIon = False
                            End If
                        End If
                    End If

                    If eCurrentXMLDataFileSection <> eCurrentXMLDataFileSectionConstants.UnknownFile AndAlso objXMLReader.NodeType = XmlNodeType.Element Then
                        Select Case eCurrentXMLDataFileSection
                            Case eCurrentXMLDataFileSectionConstants.Options
                                Try
                                    Select Case objXMLReader.Name
                                        Case "SimilarIonMZToleranceHalfWidth"
                                            similarIonMZToleranceHalfWidth = Single.Parse(XMLTextReaderGetInnerText(objXMLReader))
                                        Case "FindPeaksOnSmoothedData"
                                            findPeaksOnSmoothedData = Boolean.Parse(XMLTextReaderGetInnerText(objXMLReader))
                                        Case "UseButterworthSmooth"
                                            useButterworthSmooth = Boolean.Parse(XMLTextReaderGetInnerText(objXMLReader))
                                        Case "ButterworthSamplingFrequency"
                                            butterworthSamplingFrequency = Single.Parse(XMLTextReaderGetInnerText(objXMLReader))
                                        Case "UseSavitzkyGolaySmooth"
                                            useSavitzkyGolaySmooth = Boolean.Parse(XMLTextReaderGetInnerText(objXMLReader))
                                        Case "SavitzkyGolayFilterOrder"
                                            savitzkyGolayFilterOrder = Integer.Parse(XMLTextReaderGetInnerText(objXMLReader))
                                        Case Else
                                            ' Ignore the setting
                                    End Select

                                Catch ex As Exception
                                    ' Ignore any errors looking up smoothing options
                                End Try

                            Case eCurrentXMLDataFileSectionConstants.ParentIons
                                If validParentIon Then
                                    Try
                                        With mParentIonStats(mParentIonStats.Count - 1)
                                            Select Case objXMLReader.Name
                                                Case "MZ"
                                                    .MZ = Math.Round(CDbl(XMLTextReaderGetInnerText(objXMLReader)), 6)
                                                Case "SurveyScanNumber"
                                                    .SurveyScanNumber = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "FragScanNumber"
                                                    .FragScanObserved = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "FragScanTime"
                                                    .FragScanTime = CSng(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "OptimalPeakApexScanNumber"
                                                    .OptimalPeakApexScanNumber = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "OptimalPeakApexScanTime"
                                                    .OptimalPeakApexTime = CSng(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "CustomSICPeak"
                                                    .CustomSICPeak = CBool(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "CustomSICPeakComment"
                                                    .CustomSICPeakComment = XMLTextReaderGetInnerText(objXMLReader)

                                                Case "SICScanType"
                                                    Dim sicScanType = XMLTextReaderGetInnerText(objXMLReader)
                                                    ' ReSharper disable once StringLiteralTypo
                                                    If sicScanType.ToLower() = "fragscan" Then
                                                        .SICScanType = clsParentIonStats.eScanTypeConstants.FragScan
                                                    Else
                                                        .SICScanType = clsParentIonStats.eScanTypeConstants.SurveyScan
                                                    End If
                                                Case "PeakScanStart"
                                                    peakScanStart = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakScanEnd"
                                                    peakScanEnd = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakScanMaxIntensity"
                                                    .SICStats.ScanNumberMaxIntensity = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakIntensity"
                                                    .SICStats.Peak.MaxIntensityValue = CDbl(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakSignalToNoiseRatio"
                                                    .SICStats.Peak.SignalToNoiseRatio = CDbl(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "FWHMInScans"
                                                    .SICStats.Peak.FWHMScanWidth = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakArea"
                                                    .SICStats.Peak.Area = CDbl(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "ShoulderCount"
                                                    .SICStats.Peak.ShoulderCount = CInt(XMLTextReaderGetInnerText(objXMLReader))

                                                Case "ParentIonIntensity"
                                                    .SICStats.Peak.ParentIonIntensity = CDbl(XMLTextReaderGetInnerText(objXMLReader))

                                                Case "PeakBaselineNoiseLevel"
                                                    .SICStats.Peak.BaselineNoiseStats.NoiseLevel = CDbl(XMLTextReaderGetInnerText(objXMLReader))
                                                    baselineNoiseStatsFound = True
                                                Case "PeakBaselineNoiseStDev"
                                                    .SICStats.Peak.BaselineNoiseStats.NoiseStDev = CDbl(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakBaselinePointsUsed"
                                                    .SICStats.Peak.BaselineNoiseStats.PointsUsed = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "NoiseThresholdModeUsed"
                                                    .SICStats.Peak.BaselineNoiseStats.NoiseThresholdModeUsed = CType(XMLTextReaderGetInnerText(objXMLReader), clsMASICPeakFinder.eNoiseThresholdModes)

                                                Case "StatMomentsArea"
                                                    .SICStats.Peak.StatisticalMoments.Area = CDbl(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "CenterOfMassScan"
                                                    .SICStats.Peak.StatisticalMoments.CenterOfMassScan = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakStDev"
                                                    .SICStats.Peak.StatisticalMoments.StDev = CDbl(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakSkew"
                                                    .SICStats.Peak.StatisticalMoments.Skew = CDbl(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakKSStat"
                                                    .SICStats.Peak.StatisticalMoments.KSStat = CDbl(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "StatMomentsDataCountUsed"
                                                    .SICStats.Peak.StatisticalMoments.DataCountUsed = CInt(XMLTextReaderGetInnerText(objXMLReader))

                                                Case "SICScanStart"
                                                    scanStart = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "SICScanIntervals"
                                                    scanIntervals = XMLTextReaderGetInnerText(objXMLReader)
                                                Case "SICPeakIndexStart"
                                                    .SICStats.Peak.IndexBaseLeft = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "SICPeakIndexEnd"
                                                    .SICStats.Peak.IndexBaseRight = CInt(XMLTextReaderGetInnerText(objXMLReader))

                                                Case "SICDataCount"
                                                    value = XMLTextReaderGetInnerText(objXMLReader)
                                                    If DataUtils.StringToValueUtils.IsNumber(value) Then
                                                        expectedSicDataCount = CInt(value)
                                                    Else
                                                        expectedSicDataCount = 0
                                                    End If

                                                Case "SICSmoothedYDataIndexStart"
                                                    value = XMLTextReaderGetInnerText(objXMLReader)
                                                    If DataUtils.StringToValueUtils.IsNumber(value) Then
                                                        .SICStats.SICSmoothedYDataIndexStart = CInt(value)
                                                    Else
                                                        .SICStats.SICSmoothedYDataIndexStart = 0
                                                    End If
                                                Case "IntensityDataList"
                                                    intensityDataList = XMLTextReaderGetInnerText(objXMLReader)
                                                Case "MassDataList"
                                                    massDataList = XMLTextReaderGetInnerText(objXMLReader)
                                                Case "SmoothedYDataList"
                                                    smoothedYDataList = XMLTextReaderGetInnerText(objXMLReader)
                                                Case Else
                                                    ' Unknown child node name; ignore it
                                            End Select
                                        End With
                                    Catch ex As Exception
                                        ' Error parsing value from the ParentIon data
                                        errorMessages.Add("Error parsing value for parent ion '" & indexInXMLFile & "'")
                                        validParentIon = False
                                    End Try
                                End If
                        End Select
                    End If

                Loop

            End Using

            ' For each parent ion, find the other nearby parent ions with similar m/z values
            ' Use the tolerance specified by similarIonMZToleranceHalfWidth, though with a minimum value of 0.1
            If similarIonMZToleranceHalfWidth < 0.1 Then
                similarIonMZToleranceHalfWidth = 0.1
            End If
            FindSimilarParentIon(similarIonMZToleranceHalfWidth * 2)


            If eCurrentXMLDataFileSection = eCurrentXMLDataFileSectionConstants.UnknownFile Then
                MessageBox.Show("Root element 'SICData' not found in the input file: " & ControlChars.NewLine & filePath, "Invalid File Format", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Else

                ' Set the smoothing options
                If Not findPeaksOnSmoothedData Then
                    optDoNotResmooth.Checked = True
                Else
                    If useButterworthSmooth OrElse Not useSavitzkyGolaySmooth Then
                        optUseButterworthSmooth.Checked = True
                        txtButterworthSamplingFrequency.Text = butterworthSamplingFrequency.ToString
                    Else
                        optUseSavitzkyGolaySmooth.Checked = True
                        txtSavitzkyGolayFilterOrder.Text = savitzkyGolayFilterOrder.ToString
                    End If
                End If

                If smoothedDataFound Then
                    optDoNotResmooth.Text = "Do Not Resmooth"
                Else
                    optDoNotResmooth.Text = "Do Not Show Smoothed Data"
                End If

                ' Inform the user if any errors occurred
                If errorMessages.Count > 0 Then
                    MessageBox.Show(String.Join(ControlChars.NewLine, errorMessages.Take(15)), "Invalid Lines", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                End If

                ' Sort the data
                SortData()

                ' Select the first item in lstParentIonData
                If lstParentIonData.Items.Count > 0 Then
                    lstParentIonData.SelectedIndex = 0
                End If

                If objProgress.KeyPressAbortProcess Then
                    MessageBox.Show("Load cancelled before all of the data was read", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Else
                    AutoOpenMsMsResults(Path.GetFullPath(filePath), objProgress)
                End If

            End If

        Catch ex As Exception
            MessageBox.Show("Unable to read the input file: " & filePath & ControlChars.NewLine & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        Finally
            If Not objProgress Is Nothing Then
                objProgress.HideForm()
            End If
        End Try

    End Sub

    Private Sub ReadMsMsSearchEngineResults(filePath As String, ByRef objProgress As frmProgress)

        Dim chSepChars = New Char() {ControlChars.Tab}

        Dim sequenceID As Integer
        Dim bytesRead As Long
        Dim linesRead As Integer

        Dim scanNumber As Integer, charge As Integer

        Dim createdNewProgressForm As Boolean

        Dim objNewRow As DataRow

        Try
            Dim dataFileInfo = New FileInfo(filePath)
            Dim fileSizeBytes = dataFileInfo.Length

            Using reader = New StreamReader(New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))

                If objProgress Is Nothing Then
                    objProgress = New frmProgress
                    createdNewProgressForm = True
                End If

                objProgress.InitializeProgressForm("Reading MS/MS Search Engine Results ", 0, fileSizeBytes, True)
                objProgress.Show()
                objProgress.BringToFront()
                Application.DoEvents()

                With mMsMsResults
                    If .Tables(TABLE_NAME_MSMS_RESULTS).Rows.Count > 0 Or .Tables(TABLE_NAME_SEQ_TO_PROTEIN_MAP).Rows.Count > 0 Or .Tables(TABLE_NAME_SEQUENCES).Rows.Count > 0 Then
                        ClearMsMsResults()
                    End If
                End With

                linesRead = 0
                Do While Not reader.EndOfStream
                    Dim dataLine = reader.ReadLine
                    linesRead += 1

                    If Not dataLine Is Nothing Then
                        bytesRead += dataLine.Length + 2

                        If linesRead Mod 50 = 0 Then
                            objProgress.UpdateProgressBar(bytesRead)
                            Application.DoEvents()
                            If objProgress.KeyPressAbortProcess Then Exit Do
                        End If

                        Dim dataCols = dataLine.Trim().Split(chSepChars)

                        If dataCols.Length >= 13 Then
                            sequenceID = LookupSequenceID(dataCols(eMsMsSearchEngineResultColumns.Sequence), dataCols(eMsMsSearchEngineResultColumns.Protein))

                            If sequenceID >= 0 Then
                                Try
                                    scanNumber = Integer.Parse(dataCols(eMsMsSearchEngineResultColumns.Scan))
                                    charge = Integer.Parse(dataCols(eMsMsSearchEngineResultColumns.Charge))

                                    With mMsMsResults.Tables(TABLE_NAME_MSMS_RESULTS)
                                        If Not .Rows.Contains(New Object() {scanNumber, charge, sequenceID}) Then
                                            objNewRow = mMsMsResults.Tables(TABLE_NAME_MSMS_RESULTS).NewRow
                                            objNewRow.Item(COL_NAME_SCAN) = scanNumber
                                            objNewRow.Item(COL_NAME_CHARGE) = charge
                                            objNewRow.Item(COL_NAME_MH) = dataCols(eMsMsSearchEngineResultColumns.MH)
                                            objNewRow.Item(COL_NAME_XCORR) = dataCols(eMsMsSearchEngineResultColumns.XCorr)
                                            objNewRow.Item(COL_NAME_DELTACN) = dataCols(eMsMsSearchEngineResultColumns.DeltaCN)
                                            objNewRow.Item(COL_NAME_DELTACN2) = dataCols(eMsMsSearchEngineResultColumns.DeltaCn2)
                                            objNewRow.Item(COL_NAME_RANK_SP) = dataCols(eMsMsSearchEngineResultColumns.RankSp)
                                            objNewRow.Item(COL_NAME_RANK_XC) = dataCols(eMsMsSearchEngineResultColumns.RankXc)
                                            objNewRow.Item(COL_NAME_SEQUENCE_ID) = sequenceID
                                            objNewRow.Item(COL_NAME_PARENT_ION_INDEX) = -1

                                            mMsMsResults.Tables(TABLE_NAME_MSMS_RESULTS).Rows.Add(objNewRow)
                                        End If
                                    End With
                                Catch ex As Exception
                                    ' Error parsing/adding row
                                    Console.WriteLine("Error reading data from MS/MS Search Results file: " & ex.Message)
                                End Try

                            End If
                        End If
                    End If
                Loop

            End Using

            ' Populate column .Item(COL_NAME_PARENT_ION_INDEX)
            PopulateParentIonIndexColumnInMsMsResultsTable()

            txtStats3.Visible = True
            PositionControls()
        Catch ex As Exception
            MessageBox.Show("Error in ReadMsMsSearchEngineResults: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        Finally

            If createdNewProgressForm Then
                objProgress.HideForm()
                objProgress = Nothing
            End If

        End Try

    End Sub

    Private Sub RedoSICPeakFindingAllData()

        Const SMOOTH_MODE = eSmoothModeConstants.Butterworth

        Dim parentIonIndex As Integer
        Dim validPeakFound As Boolean

        Dim sicStats As clsSICStats = Nothing

        Dim objProgress As New frmProgress

        Try
            cmdRedoSICPeakFindingAllData.Enabled = False

            objProgress.InitializeProgressForm("Repeating SIC peak finding ", 0, mParentIonStats.Count, True)
            objProgress.Show()
            Application.DoEvents()

            UpdateSICPeakFinderOptions()

            For parentIonIndex = 0 To mParentIonStats.Count - 1
                validPeakFound = UpdateSICStats(parentIonIndex, True, SMOOTH_MODE, sicStats)

                If validPeakFound Then
                    mParentIonStats(parentIonIndex).SICStats = sicStats
                End If

                objProgress.UpdateProgressBar(parentIonIndex + 1)
                Application.DoEvents()
                If objProgress.KeyPressAbortProcess Then Exit For

            Next

            SortData()

        Catch ex As Exception
            MessageBox.Show("Error in RedoSICPeakFindingAllData: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        Finally
            If Not objProgress Is Nothing Then
                objProgress.HideForm()
            End If
            cmdRedoSICPeakFindingAllData.Enabled = True
        End Try

    End Sub

    Private Sub RegReadSettings()
        ' Load settings from the registry
        Try
            txtDataFilePath.Text = GetSetting(REG_APP_NAME, REG_SECTION_NAME, "DataFilePath", String.Empty)

            Me.Width = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "WindowSizeWidth", Me.Width)
            'Me.Height = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "WindowSizeHeight", Me.Height)
            Me.Height = 700

            Me.Top = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "WindowPosTop", Me.Top)
            Me.Left = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "WindowPosLeft", Me.Left)

            cboSortOrder.SelectedIndex = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "SortOrder", cboSortOrder.SelectedIndex)
            chkSortDescending.Checked = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "SortDescending", chkSortDescending.Checked)

            txtFixXRange.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FixXRange", 300).ToString
            txtFixYRange.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FixYRange", 5000000).ToString
            txtMinimumSignalToNoise.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "MinimumSignalToNoise", 3).ToString

            chkFixXRange.Checked = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FixXRangeEnabled", True)
            chkFixYRange.Checked = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FixYRangeEnabled", False)
            chkFilterBySignalToNoise.Checked = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FilterBySignalToNoise", False)

            txtMinimumIntensity.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "MinimumIntensity", 1000000).ToString
            txtFilterByMZ.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FilterByMZ", CSng(550)).ToString
            txtFilterByMZTol.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "FilterByMZTol", CSng(0.2)).ToString

            txtAutoStep.Text = GetSettingVal(REG_APP_NAME, REG_SECTION_NAME, "AutoStepInterval", 150).ToString

        Catch ex As Exception
            MessageBox.Show("Error in RegReadSettings: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try

    End Sub

    Private Sub RegSaveSettings()

        ' Save settings to the registry
        Try
            If txtDataFilePath.Text.Length > 0 Then
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "DatafilePath", txtDataFilePath.Text)
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "WindowSizeWidth", Me.Width.ToString())
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "WindowSizeHeight", Me.Height.ToString())
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "WindowPosTop", Me.Top.ToString())
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "WindowPosLeft", Me.Left.ToString())

                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "SortOrder", cboSortOrder.SelectedIndex.ToString())
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "SortDescending", chkSortDescending.Checked.ToString())

                If DataUtils.StringToValueUtils.IsNumber(txtFixXRange.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FixXRange", CInt(txtFixXRange.Text).ToString())
                End If
                If DataUtils.StringToValueUtils.IsNumber(txtFixYRange.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FixYRange", CLng(txtFixYRange.Text).ToString())
                End If
                If DataUtils.StringToValueUtils.IsNumber(txtMinimumSignalToNoise.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "MinimumSignalToNoise", CSng(txtMinimumSignalToNoise.Text).ToString())
                End If
                If DataUtils.StringToValueUtils.IsNumber(txtMinimumIntensity.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "MinimumIntensity", CInt(txtMinimumIntensity.Text).ToString())
                End If
                If DataUtils.StringToValueUtils.IsNumber(txtFilterByMZ.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FilterByMZ", CSng(txtFilterByMZ.Text).ToString())
                End If
                If DataUtils.StringToValueUtils.IsNumber(txtFilterByMZTol.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FilterByMZTol", CSng(txtFilterByMZTol.Text).ToString())
                End If
                If DataUtils.StringToValueUtils.IsNumber(txtAutoStep.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "AutoStepInterval", CInt(txtAutoStep.Text).ToString())
                End If

                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FixXRangeEnabled", chkFixXRange.Checked.ToString())
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FixYRangeEnabled", chkFixYRange.Checked.ToString())
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FilterBySignalToNoise", chkFilterBySignalToNoise.Checked.ToString())

            End If
        Catch ex As Exception
            MessageBox.Show("Error in RegSaveSettings: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try
    End Sub

    Private Sub SelectMASICInputFile()

        Dim dlgOpenFile As New OpenFileDialog

        With dlgOpenFile
            .Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*"
            .FilterIndex = 1
            If Len(txtDataFilePath.Text.Length) > 0 Then
                Try
                    .InitialDirectory = Directory.GetParent(txtDataFilePath.Text).ToString
                Catch
                    .InitialDirectory = Application.StartupPath
                End Try
            Else
                .InitialDirectory = Application.StartupPath
            End If

            .Title = "Select MASIC Results File"

            .ShowDialog()

            If .FileName.Length > 0 Then
                txtDataFilePath.Text = .FileName
                ReadDataFileXMLTextReader(.FileName)

                'ReadDataFileXML(.FileName)
            End If
        End With

        PositionControls()
    End Sub

    Private Sub SelectMsMsSearchResultsInputFile()
        Dim dlgOpenFile As New OpenFileDialog

        With dlgOpenFile
            .Filter = "First Hits Files(*_fht.txt)|*_fht.txt|Synopsis Hits Files(*_syn.txt)|*_syn.txt|All files (*.*)|*.*"
            .FilterIndex = 1
            If Len(txtDataFilePath.Text.Length) > 0 Then
                Try
                    .InitialDirectory = Directory.GetParent(txtDataFilePath.Text).ToString
                Catch
                    .InitialDirectory = Application.StartupPath
                End Try
            Else
                .InitialDirectory = Application.StartupPath
            End If

            .Title = "Select MS/MS Search Engine Results File"

            .ShowDialog()
            If .FileName.Length > 0 Then
                ReadMsMsSearchEngineResults(.FileName, Nothing)
            End If
        End With

        PositionControls()
    End Sub

    Private Sub SetToolTips()
        Dim objToolTipControl As New ToolTip

        With objToolTipControl

            .SetToolTip(txtButterworthSamplingFrequency, "Value between 0.01 and 0.99; suggested value is 0.20")
            .SetToolTip(txtSavitzkyGolayFilterOrder, "Even number, 0 or greater; 0 means a moving average filter, 2 means a 2nd order Savitzky Golay filter")

        End With

    End Sub

    Private Sub ShowAboutBox()
        Dim message = New List(Of String)

        message.Add("The MASIC Browser can be used to visualize the SIC's created using MASIC.")
        message.Add("")

        message.Add("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2003")
        message.Add("Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.")
        message.Add("")

        message.Add("This is version " & Application.ProductVersion & " (" & PROGRAM_DATE & ")")
        message.Add("")

        message.Add("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov")
        message.Add("Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/")
        message.Add("")

        message.Add("Licensed under the Apache License, Version 2.0; you may not use this file except in compliance with the License. ")
        message.Add("You may obtain a copy of the License at https://www.apache.org/licenses/LICENSE-2.0")
        message.Add("")

        message.Add("Notice: This computer software was prepared by Battelle Memorial Institute, " &
                    "hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the " &
                    "Department of Energy (DOE).  All rights in the computer software are reserved " &
                    "by DOE on behalf of the United States Government and the Contractor as " &
                    "provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY " &
                    "WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS " &
                    "SOFTWARE.  This notice including this sentence must appear on any copies of " &
                    "this computer software.")

        MessageBox.Show(String.Join(ControlChars.NewLine, message), "About", MessageBoxButtons.OK, MessageBoxIcon.Information)

    End Sub

    Private Sub SortData()

        Dim sortKeys() As Double
        Dim eSortMode As eSortOrderConstants

        Dim scanNumberSaved As Integer

        Dim minimumIntensity As Double
        Dim mzFilter, mzFilterTol As Double
        Dim minimumSN As Double

        If mParentIonStats.Count <= 0 Then
            ReDim mParentIonPointerArray(-1)
            Exit Sub
        End If

        Try
            If lstParentIonData.SelectedIndex >= 0 Then
                scanNumberSaved = mParentIonStats(mParentIonPointerArray(lstParentIonData.SelectedIndex)).FragScanObserved
            End If
        Catch ex As Exception
            scanNumberSaved = 1
        End Try

        mParentIonPointerArrayCount = 0
        ReDim mParentIonPointerArray(mParentIonStats.Count - 1)
        ReDim sortKeys(mParentIonStats.Count - 1)

        If cboSortOrder.SelectedIndex >= 0 And cboSortOrder.SelectedIndex < SORT_ORDER_MODE_COUNT Then
            eSortMode = CType(cboSortOrder.SelectedIndex, eSortOrderConstants)
        Else
            eSortMode = eSortOrderConstants.SortByScanPeakCenter
        End If

        If chkFilterByIntensity.Checked AndAlso DataUtils.StringToValueUtils.IsNumber(txtMinimumIntensity.Text) Then
            minimumIntensity = CDbl(txtMinimumIntensity.Text)
        Else
            minimumIntensity = Double.MinValue
        End If

        If chkFilterByMZ.Checked AndAlso DataUtils.StringToValueUtils.IsNumber(txtFilterByMZ.Text) AndAlso
           DataUtils.StringToValueUtils.IsNumber(txtFilterByMZTol.Text) Then
            mzFilter = Math.Abs(CDbl(txtFilterByMZ.Text))
            mzFilterTol = Math.Abs(CDbl(txtFilterByMZTol.Text))
        Else
            mzFilter = -1
            mzFilterTol = Double.MaxValue
        End If

        If chkFilterBySignalToNoise.Checked AndAlso DataUtils.StringToValueUtils.IsNumber(txtMinimumSignalToNoise.Text) Then
            minimumSN = CDbl(txtMinimumSignalToNoise.Text)
        Else
            minimumSN = Double.MinValue
        End If

        Select Case eSortMode
            Case eSortOrderConstants.SortByPeakIndex
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            sortKeys(mParentIonPointerArrayCount) = mParentIonStats(index).Index
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByScanPeakCenter
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            sortKeys(mParentIonPointerArrayCount) = mParentIonStats(index).SICStats.ScanNumberMaxIntensity
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByScanOptimalPeakCenter
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            sortKeys(mParentIonPointerArrayCount) = CDbl(mParentIonStats(index).OptimalPeakApexScanNumber.ToString() & "." & Math.Round(mParentIonStats(index).MZ, 0).ToString("0000") & mParentIonStats(index).Index.ToString("00000"))
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByMz
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            sortKeys(mParentIonPointerArrayCount) = CDbl(Math.Round(mParentIonStats(index).MZ, 2).ToString() & mParentIonStats(index).SICStats.ScanNumberMaxIntensity.ToString("000000"))
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByPeakSignalToNoise
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            sortKeys(mParentIonPointerArrayCount) = mParentIonStats(index).SICStats.Peak.SignalToNoiseRatio
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByBaselineCorrectedPeakIntensity
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            With mParentIonStats(index).SICStats
                                sortKeys(mParentIonPointerArrayCount) = clsMASICPeakFinder.BaselineAdjustIntensity(.Peak, True)
                            End With
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByBaselineCorrectedPeakArea
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            With mParentIonStats(index).SICStats
                                sortKeys(mParentIonPointerArrayCount) = clsMASICPeakFinder.BaselineAdjustArea(.Peak, .SICPeakWidthFullScans, True)
                            End With
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByPeakWidth
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            ' Create a sort key that is based on both PeakFWHMScans and ScanNumberMaxIntensity by separating the two integers with a "."
                            sortKeys(mParentIonPointerArrayCount) = GetSortKey(mParentIonStats(index).SICStats.Peak.FWHMScanWidth,
                                                                               mParentIonStats(index).SICStats.ScanNumberMaxIntensity)
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortBySICIntensityMax
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            sortKeys(mParentIonPointerArrayCount) = Math.Round(mParentIonStats(index).SICIntensityMax, 0)
                            ' Append the scan number so that we sort by intensity, then scan
                            If chkSortDescending.Checked Then
                                sortKeys(mParentIonPointerArrayCount) += (1 - mParentIonStats(index).FragScanObserved / 1000000.0)
                            Else
                                sortKeys(mParentIonPointerArrayCount) += mParentIonStats(index).FragScanObserved / 1000000.0
                            End If
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByPeakIntensity
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            sortKeys(mParentIonPointerArrayCount) = mParentIonStats(index).SICStats.Peak.MaxIntensityValue
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByPeakArea
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            sortKeys(mParentIonPointerArrayCount) = mParentIonStats(index).SICStats.Peak.Area
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByFragScanToOptimalLocDistance
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            ' Create a sort key that is based on both FragScan-OptimalPeakApexScanNumber and OptimalPeakApexScanNumber by separating the two integers with a "."
                            sortKeys(mParentIonPointerArrayCount) = GetSortKey(mParentIonStats(index).FragScanObserved - mParentIonStats(index).OptimalPeakApexScanNumber,
                                                                               mParentIonStats(index).OptimalPeakApexScanNumber)
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByPeakCenterToOptimalLocDistance
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            ' Create a sort key that is based on both ScanNumberMaxIntensity-OptimalPeakApexScanNumber and OptimalPeakApexScanNumber by separating the two integers with a "."
                            sortKeys(mParentIonPointerArrayCount) = GetSortKey(mParentIonStats(index).SICStats.ScanNumberMaxIntensity - mParentIonStats(index).OptimalPeakApexScanNumber,
                                                                               mParentIonStats(index).OptimalPeakApexScanNumber)
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByShoulderCount
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            ' Create a sort key that is based on both ShoulderCount and OptimalPeakApexScanNumber by separating the two integers with a "."
                            sortKeys(mParentIonPointerArrayCount) = GetSortKey(mParentIonStats(index).SICStats.Peak.ShoulderCount,
                                                                               mParentIonStats(index).OptimalPeakApexScanNumber)
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByParentIonIntensity
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            sortKeys(mParentIonPointerArrayCount) = mParentIonStats(index).SICStats.Peak.ParentIonIntensity
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByPeakSkew
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            sortKeys(mParentIonPointerArrayCount) = mParentIonStats(index).SICStats.Peak.StatisticalMoments.Skew
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByKSStat
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            sortKeys(mParentIonPointerArrayCount) = mParentIonStats(index).SICStats.Peak.StatisticalMoments.KSStat
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next
            Case eSortOrderConstants.SortByBaselineNoiseLevel
                For index = 0 To mParentIonStats.Count - 1
                    With mParentIonStats(index)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ,
                                               minimumIntensity, minimumSN, mzFilter, mzFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = index
                            sortKeys(mParentIonPointerArrayCount) = GetSortKey(CInt(Math.Round(mParentIonStats(index).SICStats.Peak.BaselineNoiseStats.NoiseLevel, 0)),
                                                                               mParentIonStats(index).SICStats.Peak.SignalToNoiseRatio)
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next

        End Select

        If mParentIonPointerArrayCount < mParentIonStats.Count Then
            ReDim Preserve sortKeys(mParentIonPointerArrayCount - 1)
            ReDim Preserve mParentIonPointerArray(mParentIonPointerArrayCount - 1)
        End If

        ' Sort mParentIonPointerArray
        Array.Sort(sortKeys, mParentIonPointerArray)

        If chkSortDescending.Checked Then
            Array.Reverse(mParentIonPointerArray)
        End If

        PopulateSpectrumList(scanNumberSaved)

    End Sub

    Private Function SortDataFilterCheck(
      peakMaxIntensityValue As Double,
      peakSN As Double,
      peakMZ As Double,
      minimumIntensity As Double,
      minimumSN As Double,
      mzFilter As Double,
      mzFilterTol As Double,
      isCustomSIC As Boolean) As Boolean

        Dim useData As Boolean

        If cboSICsTypeFilter.SelectedIndex = eSICTypeFilterConstants.CustomSICsOnly Then
            useData = isCustomSIC
        ElseIf cboSICsTypeFilter.SelectedIndex = eSICTypeFilterConstants.NoCustomSICs Then
            useData = Not isCustomSIC
        Else
            useData = True
        End If

        If useData AndAlso peakMaxIntensityValue >= minimumIntensity Then
            If peakSN >= minimumSN Then
                If mzFilter >= 0 Then
                    If Math.Abs(peakMZ - mzFilter) <= mzFilterTol Then
                        useData = True
                    Else
                        useData = False
                    End If
                Else
                    useData = True
                End If
            Else
                useData = False
            End If
        Else
            useData = False
        End If

        Return useData

    End Function

    Private Sub TestValueToString()
        Dim results As String = String.Empty

        For digits As Byte = 1 To 5
            results &= TestValueToStringWork(0.00001234, digits) & ControlChars.NewLine
            results &= TestValueToStringWork(0.01234, digits) & ControlChars.NewLine
            results &= TestValueToStringWork(0.1234, digits) & ControlChars.NewLine
            results &= TestValueToStringWork(0.123, digits) & ControlChars.NewLine
            results &= TestValueToStringWork(0.12, digits) & ControlChars.NewLine
            results &= TestValueToStringWork(1.234, digits) & ControlChars.NewLine
            results &= TestValueToStringWork(12.34, digits) & ControlChars.NewLine
            results &= TestValueToStringWork(123.4, digits) & ControlChars.NewLine
            results &= TestValueToStringWork(1234, digits) & ControlChars.NewLine
            results &= TestValueToStringWork(12340, digits) & ControlChars.NewLine
            results &= ControlChars.NewLine
        Next

        MsgBox(results)
    End Sub

    Private Function TestValueToStringWork(value As Single, digitsOfPrecision As Byte) As String
        Return value.ToString & ": " & StringUtilities.ValueToString(value, digitsOfPrecision)
    End Function

    Private Sub ToggleAutoStep(Optional forceDisabled As Boolean = False)
        If mAutoStepEnabled Or forceDisabled Then
            mAutoStepEnabled = False
            cmdAutoStep.Text = "&Auto Step"
        Else
            If DataUtils.StringToValueUtils.IsNumber(txtAutoStep.Text) Then
                mAutoStepIntervalMsec = CInt(txtAutoStep.Text)
            Else
                txtAutoStep.Text = "150"
                mAutoStepIntervalMsec = 150
            End If

            If lstParentIonData.SelectedIndex = 0 AndAlso Not chkAutoStepForward.Checked Then
                chkAutoStepForward.Checked = True
            ElseIf lstParentIonData.SelectedIndex = mParentIonPointerArrayCount - 1 AndAlso chkAutoStepForward.Checked Then
                chkAutoStepForward.Checked = False
            End If
            mLastUpdate = DateTime.Now()
            mAutoStepEnabled = True
            cmdAutoStep.Text = "Stop &Auto"
        End If
    End Sub

    Private Sub UpdateSICPeakFinderOptions()
        With mSICPeakFinderOptions
            ''.IntensityThresholdFractionMax=
            ''.IntensityThresholdAbsoluteMinimum =

            ''With .SICNoiseThresholdOptions
            ''    .NoiseThresholdMode =
            ''    .NoiseThresholdIntensity =
            ''    .NoiseFractionLowIntensityDataToAverage =
            ''    .MinimumSignalToNoiseRatio =
            ''    .ExcludePeakDataFromNoiseComputation =
            ''    .MinimumNoiseThresholdLevel =
            ''End With

            ''.MaxDistanceScansNoOverlap =
            ''.MaxAllowedUpwardSpikeFractionMax =
            ''.InitialPeakWidthScansScaler =
            ''.InitialPeakWidthScansMaximum =

            If optDoNotResmooth.Checked Then
                .FindPeaksOnSmoothedData = False
            Else
                .FindPeaksOnSmoothedData = True
            End If

            ''.SmoothDataRegardlessOfMinimumPeakWidth =
            If optUseSavitzkyGolaySmooth.Checked Then
                .UseButterworthSmooth = False
                .UseSavitzkyGolaySmooth = True
                .SavitzkyGolayFilterOrder = CShort(PRISMWin.TextBoxUtils.ParseTextBoxValueInt(txtSavitzkyGolayFilterOrder, "", False, 0))
            Else
                .UseButterworthSmooth = True
                .UseSavitzkyGolaySmooth = False
                .ButterworthSamplingFrequency = PRISMWin.TextBoxUtils.ParseTextBoxValueFloat(txtButterworthSamplingFrequency, "", False, 0.25)
            End If
            ''.ButterworthSamplingFrequencyDoubledForSIMData =

            ''With .MassSpectraNoiseThresholdOptions
            ''    .NoiseThresholdMode =
            ''    .NoiseThresholdIntensity =
            ''    .NoiseFractionLowIntensityDataToAverage =
            ''    .MinimumSignalToNoiseRatio =
            ''    .ExcludePeakDataFromNoiseComputation =
            ''    .MinimumNoiseThresholdLevel =
            ''End With
        End With
    End Sub

    Private Function UpdateSICStats(
      parentIonIndex As Integer,
      repeatPeakFinding As Boolean,
      eSmoothMode As eSmoothModeConstants,
      <Out()> ByRef sicStats As clsSICStats) As Boolean

        ' Copy the original SIC stats found by MASIC into udtSICStats
        ' This also includes the original smoothed data

        ' Copy the cached SICStats data into udtSICStats
        ' We have to separately copy SICSmoothedYData() otherwise VB.NET keeps
        '  the array linked in both mParentIonStats().SICStats and udtSICStats
        With mParentIonStats(parentIonIndex)
            sicStats = .SICStats
            With .SICStats
                sicStats.SICSmoothedYData.Clear()
                For Each dataPoint In .SICSmoothedYData
                    sicStats.SICSmoothedYData.Add(dataPoint)
                Next
            End With
        End With

        If eSmoothMode <> eSmoothModeConstants.DoNotReSmooth Then
            ' Re-smooth the data
            Dim objFilter = New DataFilter.DataFilter()

            Dim currentParentIon = mParentIonStats(parentIonIndex)

            sicStats.SICSmoothedYDataIndexStart = 0

            Dim intensities = (From item In currentParentIon.SICData Select CDbl(item.Intensity)).ToArray()

            If eSmoothMode = eSmoothModeConstants.SavitzkyGolay Then
                ' Resmooth using a Savitzy Golay filter

                Dim savitzkyGolayFilterOrder = PRISMWin.TextBoxUtils.ParseTextBoxValueInt(txtSavitzkyGolayFilterOrder, lblSavitzkyGolayFilterOrder.Text & " should be an even number between 0 and 20; assuming 0", False, 0)
                Dim peakWidthsPointsMinimum = PRISMWin.TextBoxUtils.ParseTextBoxValueInt(txtPeakWidthPointsMinimum, lblPeakWidthPointsMinimum.Text & " should be a positive integer; assuming 6", False, 6)

                Dim filterThirdWidth = CInt(Math.Floor(peakWidthsPointsMinimum / 3))
                If filterThirdWidth > 3 Then filterThirdWidth = 3

                ' Make sure filterThirdWidth is Odd
                If filterThirdWidth Mod 2 = 0 Then
                    filterThirdWidth -= 1
                End If

                Dim errorMessage = ""

                ' Note that the SavitzkyGolayFilter doesn't work right for PolynomialDegree values greater than 0
                ' Also note that a PolynomialDegree value of 0 results in the equivalent of a moving average filter
                objFilter.SavitzkyGolayFilter(intensities,
                                              0, currentParentIon.SICData.Count - 1,
                                              filterThirdWidth, filterThirdWidth,
                                              CShort(savitzkyGolayFilterOrder), errorMessage, True)

            Else
                ' Assume eSmoothMode = eSmoothModeConstants.Butterworth
                Dim samplingFrequency = PRISMWin.TextBoxUtils.ParseTextBoxValueFloat(txtButterworthSamplingFrequency, lblButterworthSamplingFrequency.Text & " should be a number between 0.01 and 0.99; assuming 0.2", False, 0.2)
                objFilter.ButterworthFilter(intensities, 0, currentParentIon.SICData.Count - 1, samplingFrequency)
            End If

            ' Copy the smoothed data into udtSICStats.SICSmoothedYData
            sicStats.SICSmoothedYData.Clear()

            For index = 0 To intensities.Length - 1
                sicStats.SICSmoothedYData.Add(CDbl(intensities(index)))
            Next

        End If

        If repeatPeakFinding Then
            ' Repeat the finding of the peak in the SIC
            Dim validPeakFound = FindSICPeakAndAreaForParentIon(parentIonIndex, sicStats)
            Return validPeakFound
        Else
            Return True
        End If

    End Function

    Private Sub UpdateStatsAndPlot()
        Dim sicStats As clsSICStats = Nothing
        If mParentIonPointerArrayCount > 0 Then
            DisplaySICStats(mParentIonPointerArray(lstParentIonData.SelectedIndex), sicStats)
            PlotData(mParentIonPointerArray(lstParentIonData.SelectedIndex), sicStats)
        End If
    End Sub

    Private Function XMLTextReaderGetInnerText(ByRef objXMLReader As XmlTextReader) As String
        Dim value As String = String.Empty
        Dim success As Boolean

        If objXMLReader.NodeType = XmlNodeType.Element Then
            ' Advance the reader so that we can read the value
            success = objXMLReader.Read()
        Else
            success = True
        End If

        If success AndAlso Not objXMLReader.NodeType = XmlNodeType.Whitespace And objXMLReader.HasValue Then
            value = objXMLReader.Value
        End If

        Return value
    End Function

    Private Sub XMLTextReaderSkipWhitespace(ByRef objXMLReader As XmlTextReader)
        If objXMLReader.NodeType = XmlNodeType.Whitespace Then
            ' Whitespace; read the next node
            objXMLReader.Read()
        End If
    End Sub

#Region "Checkboxes"
    Private Sub chkFilterByIntensity_CheckedChanged(sender As Object, e As EventArgs) Handles chkFilterByIntensity.CheckedChanged
        SortData()
    End Sub

    Private Sub chkFilterByMZ_CheckedChanged(sender As Object, e As EventArgs) Handles chkFilterByMZ.CheckedChanged
        SortData()
    End Sub

    Private Sub chkFilterBySignalToNoise_CheckedChanged(sender As Object, e As EventArgs) Handles chkFilterBySignalToNoise.CheckedChanged
        SortData()
    End Sub

    Private Sub chkFixXRange_CheckedChanged(sender As Object, e As EventArgs) Handles chkFixXRange.CheckedChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub chkFixYRange_CheckedChanged(sender As Object, e As EventArgs) Handles chkFixYRange.CheckedChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub cmdRedoSICPeakFindingAllData_Click(sender As Object, e As EventArgs) Handles cmdRedoSICPeakFindingAllData.Click
        RedoSICPeakFindingAllData()
    End Sub

    Private Sub chkShowBaselineCorrectedStats_CheckedChanged(sender As Object, e As EventArgs) Handles chkShowBaselineCorrectedStats.CheckedChanged
        DisplaySICStatsForSelectedParentIon()
    End Sub

    Private Sub chkSortDescending_CheckedChanged(sender As Object, e As EventArgs) Handles chkSortDescending.CheckedChanged
        SortData()
    End Sub

    Private Sub chkShowSmoothedData_CheckedChanged(sender As Object, e As EventArgs)
        UpdateStatsAndPlot()
    End Sub

    Private Sub chkUsePeakFinder_CheckedChanged(sender As Object, e As EventArgs) Handles chkUsePeakFinder.CheckedChanged
        UpdateStatsAndPlot()
    End Sub
#End Region

#Region "Command Buttons"
    Private Sub cmdAutoStep_Click(sender As Object, e As EventArgs) Handles cmdAutoStep.Click
        ToggleAutoStep()
    End Sub

    Private Sub cmdNext_Click(sender As Object, e As EventArgs) Handles cmdNext.Click
        NavigateScanList(True)
    End Sub

    Private Sub cmdPrevious_Click(sender As Object, e As EventArgs) Handles cmdPrevious.Click
        NavigateScanList(False)
    End Sub

    Private Sub cmdSelectFile_Click(sender As Object, e As EventArgs) Handles cmdSelectFile.Click
        SelectMASICInputFile()
    End Sub

    Private Sub cmdJump_Click(sender As Object, e As EventArgs) Handles cmdJump.Click
        JumpToScan()
    End Sub
#End Region

#Region "ListBoxes and Comboboxes"
    Private Sub lstParentIonData_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstParentIonData.SelectedIndexChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub cboSICsTypeFilter_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboSICsTypeFilter.SelectedIndexChanged
        SortData()
    End Sub

    Private Sub cboSortOrder_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboSortOrder.SelectedIndexChanged
        DefineDefaultSortDirection()
        SortData()
        lstParentIonData.Focus()
    End Sub

#End Region

#Region "Option buttons"
    Private Sub optDoNotResmooth_CheckedChanged(sender As Object, e As EventArgs) Handles optDoNotResmooth.CheckedChanged
        EnableDisableControls()
        UpdateStatsAndPlot()
    End Sub

    Private Sub optUseButterworthSmooth_CheckedChanged(sender As Object, e As EventArgs) Handles optUseButterworthSmooth.CheckedChanged
        EnableDisableControls()
        UpdateStatsAndPlot()
    End Sub

    Private Sub optUseSavitzkyGolaySmooth_CheckedChanged(sender As Object, e As EventArgs) Handles optUseSavitzkyGolaySmooth.CheckedChanged
        EnableDisableControls()
        UpdateStatsAndPlot()
    End Sub
#End Region

#Region "Textboxes"

    Private Sub txtAutoStep_TextChanged(sender As Object, e As EventArgs) Handles txtAutoStep.TextChanged
        Dim newInterval As Integer

        If DataUtils.StringToValueUtils.IsNumber(txtAutoStep.Text) Then
            newInterval = CInt(txtAutoStep.Text)
            If newInterval < 10 Then newInterval = 10
            mAutoStepIntervalMsec = newInterval
        End If

    End Sub

    Private Sub txtAutoStep_Validating(sender As Object, e As CancelEventArgs) Handles txtAutoStep.Validating
        PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtAutoStep, 10, 9999, 150)
    End Sub

    Private Sub txtButterworthSamplingFrequency_TextChanged(sender As Object, e As EventArgs) Handles txtButterworthSamplingFrequency.TextChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub txtButterworthSamplingFrequency_Validating(sender As Object, e As CancelEventArgs) Handles txtButterworthSamplingFrequency.Validating
        PRISMWin.TextBoxUtils.ValidateTextBoxFloat(txtButterworthSamplingFrequency, 0.01, 0.99, 0.2)
    End Sub

    Private Sub txtFilterByMZ_Leave(sender As Object, e As EventArgs) Handles txtFilterByMZ.Leave
        If chkFilterByMZ.Checked Then SortData()
    End Sub

    Private Sub txtFilterByMZ_Validating(sender As Object, e As CancelEventArgs) Handles txtFilterByMZ.Validating
        PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtFilterByMZ, 0, 100000, 540)
    End Sub

    Private Sub txtFilterByMZTol_Leave(sender As Object, e As EventArgs) Handles txtFilterByMZTol.Leave
        If chkFilterByMZ.Checked Then SortData()
    End Sub

    Private Sub txtFilterByMZTol_Validating(sender As Object, e As CancelEventArgs) Handles txtFilterByMZTol.Validating
        PRISMWin.TextBoxUtils.ValidateTextBoxFloat(txtFilterByMZTol, 0, 100000, 0.2)
    End Sub

    Private Sub txtFixXRange_TextChanged(sender As Object, e As EventArgs) Handles txtFixXRange.TextChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub txtFixXRange_Validating(sender As Object, e As CancelEventArgs) Handles txtFixXRange.Validating
        PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtFixXRange, 3, 500000, 100)
    End Sub

    Private Sub txtFixYRange_TextChanged(sender As Object, e As EventArgs) Handles txtFixYRange.TextChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub txtFixYRange_Validating(sender As Object, e As CancelEventArgs) Handles txtFixYRange.Validating
        PRISMWin.TextBoxUtils.ValidateTextBoxFloat(txtFixYRange, 10, Long.MaxValue, 5000000)
    End Sub

    Private Sub txtMinimumIntensity_Leave(sender As Object, e As EventArgs) Handles txtMinimumIntensity.Leave
        If chkFilterByIntensity.Checked Then SortData()
    End Sub

    Private Sub txtMinimumIntensity_Validating(sender As Object, e As CancelEventArgs) Handles txtMinimumIntensity.Validating
        PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtMinimumIntensity, 0, 1000000000, 1000000)
    End Sub

    Private Sub txtMinimumSignalToNoise_Leave(sender As Object, e As EventArgs) Handles txtMinimumSignalToNoise.Leave
        If chkFilterBySignalToNoise.Checked Then SortData()
    End Sub

    Private Sub txtMinimumSignalToNoise_Validating(sender As Object, e As CancelEventArgs) Handles txtMinimumSignalToNoise.Validating
        PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtMinimumSignalToNoise, 0, 10000, 2)
    End Sub

    Private Sub txtPeakWidthPointsMinimum_TextChanged(sender As Object, e As EventArgs) Handles txtPeakWidthPointsMinimum.TextChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub txtPeakWidthPointsMinimum_Validating(sender As Object, e As CancelEventArgs) Handles txtPeakWidthPointsMinimum.Validating
        PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtPeakWidthPointsMinimum, 2, 100000, 6)
    End Sub

    Private Sub txtSavitzkyGolayFilterOrder_TextChanged(sender As Object, e As EventArgs) Handles txtSavitzkyGolayFilterOrder.TextChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub txtSavitzkyGolayFilterOrder_Validating(sender As Object, e As CancelEventArgs) Handles txtSavitzkyGolayFilterOrder.Validating
        PRISMWin.TextBoxUtils.ValidateTextBoxInt(txtSavitzkyGolayFilterOrder, 0, 20, 0)
    End Sub

    Private Sub txtStats1_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtStats1.KeyPress
        If Char.IsControl(e.KeyChar) Then
            Select Case Asc(e.KeyChar)
                Case 3
                    ' Ctrl+C; allow copy
                Case 1
                    ' Ctrl+A; select all
                    txtStats1.SelectAll()
                Case Else
                    e.Handled = True
            End Select
        Else
            e.Handled = True
        End If
    End Sub

    Private Sub txtStats2_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtStats2.KeyPress
        If Char.IsControl(e.KeyChar) Then
            Select Case Asc(e.KeyChar)
                Case 3
                    ' Ctrl+C; allow copy
                Case 1
                    ' Ctrl+A; select all
                    txtStats2.SelectAll()
                Case Else
                    e.Handled = True
            End Select
        Else
            e.Handled = True
        End If
    End Sub

    Private Sub txStats3_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtStats3.KeyPress
        If Char.IsControl(e.KeyChar) Then
            Select Case Asc(e.KeyChar)
                Case 3
                    ' Ctrl+C; allow copy
                Case 1
                    ' Ctrl+A; select all
                    txtStats3.SelectAll()
                Case Else
                    e.Handled = True
            End Select
        Else
            e.Handled = True
        End If
    End Sub

#End Region

#Region "Menubar"
    Private Sub mnuHelpAbout_Click(sender As Object, e As EventArgs) Handles mnuHelpAbout.Click
        ShowAboutBox()
    End Sub

    Private Sub mnuEditShowOptimalPeakApexCursor_Click(sender As Object, e As EventArgs) Handles mnuEditShowOptimalPeakApexCursor.Click
        mnuEditShowOptimalPeakApexCursor.Checked = Not mnuEditShowOptimalPeakApexCursor.Checked
    End Sub

    Private Sub mnuFileSelectMASICInputFile_Click(sender As Object, e As EventArgs) Handles mnuFileSelectMASICInputFile.Click
        SelectMASICInputFile()
    End Sub

    Private Sub mnuFileSelectMSMSSearchResultsFile_Click(sender As Object, e As EventArgs) Handles mnuFileSelectMSMSSearchResultsFile.Click
        SelectMsMsSearchResultsInputFile()
    End Sub

    Private Sub mnuFileExit_Click(sender As Object, e As EventArgs) Handles mnuFileExit.Click
        Me.Close()
    End Sub
#End Region

    Private Sub frmBrowser_Closing(sender As Object, e As CancelEventArgs) Handles MyBase.Closing
        RegSaveSettings()
    End Sub

    Private Sub frmBrowser_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Note that InitializeControls() is called in Sub New()
    End Sub

    Private Sub tmrAutoStep_Elapsed(sender As Object, e As ElapsedEventArgs) Handles tmrAutoStep.Elapsed
        If mAutoStepEnabled Then CheckAutoStep()
    End Sub

    Private Sub frmBrowser_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        PositionControls()
    End Sub

    Private Sub mFileLoadTimer_Tick(sender As Object, e As EventArgs)
        If Not String.IsNullOrWhiteSpace(FileToAutoLoad) Then
            mFileLoadTimer.Enabled = False

            txtDataFilePath.Text = FileToAutoLoad
            ReadDataFileXMLTextReader(txtDataFilePath.Text)
        End If
    End Sub

    Private Sub MASICPeakFinderErrorHandler(message As String, ex As Exception)
        If DateTime.UtcNow.Subtract(mLastErrorNotification).TotalSeconds > 5 Then
            mLastErrorNotification = DateTime.UtcNow
            MessageBox.Show("MASICPeakFinder error: " & message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End If
    End Sub

End Class
