Option Strict On

' This program will read the _SICs.xml data file created by MASIC to allow
'   for browsing of the spectra
'
' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started October 17, 2003
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

' E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com
' Website: http://panomics.pnnl.gov/ or http://www.sysbio.org/resources/staff/
' -------------------------------------------------------------------------------
'
' Licensed under the Apache License, Version 2.0; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at
' http://www.apache.org/licenses/LICENSE-2.0
'
' Notice: This computer software was prepared by Battelle Memorial Institute,
' hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the
' Department of Energy (DOE).  All rights in the computer software are reserved
' by DOE on behalf of the United States Government and the Contractor as
' provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY
' WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS
' SOFTWARE.  This notice including this sentence must appear on any copies of
' this computer software.

Imports System.Collections.Generic
Imports System.IO
Imports PNNLOmics.Utilities
Imports System.Windows.Forms
Imports OxyDataPlotter

Public Class frmBrowser
    Inherits Form

    Private Const PROGRAM_DATE As String = "March 29, 2017"

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
    Private components As System.ComponentModel.IContainer

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
        Me.components = New System.ComponentModel.Container()
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
        CType(Me.tmrAutoStep, System.ComponentModel.ISupportInitialize).BeginInit()
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
        Me.txtDataFilePath.Location = New System.Drawing.Point(115, 18)
        Me.txtDataFilePath.Name = "txtDataFilePath"
        Me.txtDataFilePath.Size = New System.Drawing.Size(1217, 22)
        Me.txtDataFilePath.TabIndex = 1
        Me.txtDataFilePath.Text = "D:\"
        '
        'cmdSelectFile
        '
        Me.cmdSelectFile.Location = New System.Drawing.Point(10, 18)
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
        Me.lstParentIonData.Size = New System.Drawing.Size(326, 68)
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
        Me.fraSortOrderAndStats.Location = New System.Drawing.Point(0, 116)
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
        Me.pnlInputFile.Size = New System.Drawing.Size(1341, 65)
        Me.pnlInputFile.TabIndex = 9
        '
        'pnlSICs
        '
        Me.pnlSICs.Controls.Add(Me.lblParentIon)
        Me.pnlSICs.Controls.Add(Me.lstParentIonData)
        Me.pnlSICs.Controls.Add(Me.fraSortOrderAndStats)
        Me.pnlSICs.Dock = System.Windows.Forms.DockStyle.Left
        Me.pnlSICs.Location = New System.Drawing.Point(0, 65)
        Me.pnlSICs.Name = "pnlSICs"
        Me.pnlSICs.Size = New System.Drawing.Size(346, 386)
        Me.pnlSICs.TabIndex = 10
        '
        'pnlNavigationAndOptions
        '
        Me.pnlNavigationAndOptions.Controls.Add(Me.fraNavigation)
        Me.pnlNavigationAndOptions.Controls.Add(Me.TabControl1)
        Me.pnlNavigationAndOptions.Dock = System.Windows.Forms.DockStyle.Left
        Me.pnlNavigationAndOptions.Location = New System.Drawing.Point(346, 65)
        Me.pnlNavigationAndOptions.Name = "pnlNavigationAndOptions"
        Me.pnlNavigationAndOptions.Size = New System.Drawing.Size(336, 386)
        Me.pnlNavigationAndOptions.TabIndex = 11
        '
        'pnlBottom
        '
        Me.pnlBottom.Controls.Add(Me.fraPeakFinder)
        Me.pnlBottom.Controls.Add(Me.fraResmoothingOptions)
        Me.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.pnlBottom.Location = New System.Drawing.Point(0, 451)
        Me.pnlBottom.Name = "pnlBottom"
        Me.pnlBottom.Size = New System.Drawing.Size(1341, 175)
        Me.pnlBottom.TabIndex = 12
        '
        'frmBrowser
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(6, 15)
        Me.ClientSize = New System.Drawing.Size(1341, 626)
        Me.Controls.Add(Me.pnlNavigationAndOptions)
        Me.Controls.Add(Me.pnlSICs)
        Me.Controls.Add(Me.pnlInputFile)
        Me.Controls.Add(Me.pnlBottom)
        Me.Menu = Me.MainMenuControl
        Me.MinimumSize = New System.Drawing.Size(614, 0)
        Me.Name = "frmBrowser"
        Me.Text = "MASIC Browser"
        CType(Me.tmrAutoStep, System.ComponentModel.ISupportInitialize).EndInit()
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

    Private Const TABLE_NAME_MSMSRESULTS As String = "T_MsMsResults"
    Private Const TABLE_NAME_SEQUENCES As String = "T_Sequences"
    Private Const TABLE_NAME_SEQ_TO_PROTEIN_MAP As String = "T_Seq_to_Protein_Map"

    Private Const COL_NAME_SCAN As String = "Scan"
    Private Const COL_NAME_CHARGE As String = "Charge"
    Private Const COL_NAME_MH As String = "MH"
    Private Const COL_NAME_XCORR As String = "XCorr"
    Private Const COL_NAME_DELTACN As String = "DeltaCN"
    Private Const COL_NAME_DELTACN2 As String = "DeltaCn2"
    Private Const COL_NAME_RANKSP As String = "RankSp"
    Private Const COL_NAME_RANKXC As String = "RankXc"
    Private Const COL_NAME_SEQUENCEID As String = "SeqID"
    Private Const COL_NAME_PARENTIONINDEX As String = "ParentIonIndex"

    Private Const COL_NAME_SEQUENCE As String = "Sequence"
    Private Const COL_NAME_PROTEIN As String = "Protein"

    'Private Const COL_NAME_MULTIPROTEINCOUNT As String = "MultiProteinCount"

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

    Protected Enum eScanTypeConstants
        SurveyScan = 0
        FragScan = 1
    End Enum

#End Region

#Region "Structures"
    Public Structure udtSICStatsType
        Public Peak As MASICPeakFinder.clsMASICPeakFinder.udtSICStatsPeakType

        Public SICPeakWidthFullScans As Integer
        Public ScanNumberMaxIntensity As Integer                   ' Scan number of the peak Apex

        Public SICPotentialAreaStatsForPeak As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType

        Public SICSmoothedYDataIndexStart As Integer
        Public SICSmoothedYData() As Single

    End Structure


    Private Structure udtParentIonStatsType
        Public Index As Integer
        Public MZ As Double
        Public SurveyScanNumber As Integer              ' Scan number of the survey scan
        Public FragScanObserved As Integer              ' Scan number of the fragmentation scan
        Public FragScanTime As Single
        Public OptimalPeakApexScanNumber As Integer               ' Optimal peak apex scan number (if parent ion was combined with another parent ion due to similar m/z)
        Public OptimalPeakApexTime As Single
        Public CustomSICPeak As Boolean
        Public CustomSICPeakComment As String

        Public DataCount As Integer
        Public SICScanType As eScanTypeConstants

        Public SICScans() As Integer
        Public SICData() As Single                      ' Intensity data, parallel with SICScans()
        Public SICMassData() As Single                  ' Masses, parallel with SICScans()

        Public SICIntensityMax As Single                ' Maximum intensity in SICData()

        Public SICStats As udtSICStatsType              ' Contains the smoothed SIC data plus the details on the peak identified in the SIC (including its baseline noise stats)

        Public SimilarFragScanCount As Integer
        Public SimilarFragScanList() As Integer         ' List of scan numbers at which this m/z was chosen for fragmentation; the range of scans checked will be from SICScans(0) to SICScans(DataCount)
        Public SimilarFragScanPlottingIntensity() As Single
    End Structure

#End Region

#Region "Properties"
    Public Property FileToAutoLoad As String
#End Region

#Region "Classwide Variables"

    Private mSpectrum As OxyDataPlotter.Spectrum

    Private mParentIonCount As Integer
    Private mParentIonStats() As udtParentIonStatsType          ' 0-based array, ranging from 0 to mParentIonCount-1

    Private mMsMsResults As DataSet

    Private mSICPeakFinderOptions As MASICPeakFinder.clsSICPeakFinderOptions

    Private mParentIonPointerArrayCount As Integer          ' Could be less than mParentIonCount if filtering the data
    Private mParentIonPointerArray() As Integer             ' Pointer array used for de-referencing cboParentIon.SelectedItem to mParentIonStats()

    Private mAutoStepEnabled As Boolean
    Private mAutoStepIntervalMsec As Integer

    Private mLastUpdate As DateTime
    Private mMASICPeakFinder As MASICPeakFinder.clsMASICPeakFinder

    Private mFileLoadTimer As Timer

#End Region

    Private Sub AutoOpenMsMsResults(strMASICFilePath As String, ByRef objProgress As ProgressFormNET.frmProgress)
        ' Look for a corresponding Synopsis or First hits file in the same folder as strMASICFilePath

        Dim ioFolderInfo As New DirectoryInfo(Path.GetDirectoryName(strMASICFilePath))
        Dim strFileNameToFind As String
        Dim strFileNameBase As String

        strFileNameBase = Path.GetFileNameWithoutExtension(strMASICFilePath)
        If strFileNameBase.ToLower.EndsWith("_sics") Then
            strFileNameBase = strFileNameBase.Substring(0, strFileNameBase.Length - 5)
        End If

        strFileNameToFind = strFileNameBase & "_fht.txt"
        strFileNameToFind = Path.Combine(ioFolderInfo.FullName, strFileNameToFind)

        txtStats3.Visible = False
        If File.Exists(strFileNameToFind) Then
            ReadMsMsSearchEngineResults(strFileNameToFind, objProgress)
        Else
            strFileNameToFind = strFileNameBase & "_syn.txt"
            strFileNameToFind = Path.Combine(ioFolderInfo.FullName, strFileNameToFind)

            If File.Exists(strFileNameToFind) Then
                ReadMsMsSearchEngineResults(strFileNameToFind, objProgress)
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
            .Tables(TABLE_NAME_MSMSRESULTS).Clear()
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

    Private Sub DisplaySICStats(intParentIonIndex As Integer, ByRef udtSICStats As udtSICStatsType)
        ' udtSICStats will be populated with either the original SIC stats found by MASIC or with the
        '  updated SIC stats if chkUsePeakFinder is Checked
        ' Also, if re-smooth data is enabled, then the SIC data will be re-smoothed

        Dim eSmoothMode As eSmoothModeConstants
        Dim blnValidPeakFound As Boolean

        Dim sngIntensityToDisplay As Single
        Dim sngAreaToDisplay As Single

        Dim strStats As String

        UpdateSICPeakFinderOptions()

        If intParentIonIndex >= 0 And intParentIonIndex < mParentIonCount Then
            If optUseButterworthSmooth.Checked Then
                eSmoothMode = eSmoothModeConstants.Butterworth
            ElseIf optUseSavitzkyGolaySmooth.Checked Then
                eSmoothMode = eSmoothModeConstants.SavitzkyGolay
            Else
                eSmoothMode = eSmoothModeConstants.DoNotReSmooth
            End If

            blnValidPeakFound = UpdateSICStats(intParentIonIndex, chkUsePeakFinder.Checked, eSmoothMode, udtSICStats)

            ' Display the SIC and SIC Peak stats
            With mParentIonStats(intParentIonIndex)
                strStats = String.Empty
                strStats &= "Index: " & .Index.ToString
                If .OptimalPeakApexScanNumber = .SICStats.ScanNumberMaxIntensity Then
                    strStats &= ControlChars.NewLine & "Scan at apex: " & .SICStats.ScanNumberMaxIntensity
                Else
                    strStats &= ControlChars.NewLine & "Scan at apex: " & .OptimalPeakApexScanNumber & " (Original apex: " & .SICStats.ScanNumberMaxIntensity & ")"
                End If
            End With

            With udtSICStats

                If blnValidPeakFound Then
                    strStats &= ControlChars.NewLine & "Center of mass: " & .Peak.StatisticalMoments.CenterOfMassScan.ToString
                    If chkShowBaselineCorrectedStats.Checked Then
                        sngIntensityToDisplay = MASICPeakFinder.clsMASICPeakFinder.BaselineAdjustIntensity(.Peak, False)
                        sngAreaToDisplay = MASICPeakFinder.clsMASICPeakFinder.BaselineAdjustArea(.Peak, mParentIonStats(intParentIonIndex).SICStats.SICPeakWidthFullScans, False)
                    Else
                        sngIntensityToDisplay = .Peak.MaxIntensityValue
                        sngAreaToDisplay = .Peak.Area
                    End If
                    strStats &= ControlChars.NewLine & "Intensity: " & StringUtilities.ValueToString(sngIntensityToDisplay, 4)
                    strStats &= ControlChars.NewLine & "Area: " & StringUtilities.ValueToString(sngAreaToDisplay, 4)
                    strStats &= ControlChars.NewLine & "FWHM: " & .Peak.FWHMScanWidth.ToString
                Else
                    strStats &= ControlChars.NewLine & "Could not find a valid SIC peak"
                End If

                If mParentIonStats(intParentIonIndex).CustomSICPeak Then
                    strStats &= ControlChars.NewLine & "Custom SIC: " & mParentIonStats(intParentIonIndex).CustomSICPeakComment
                End If

                txtStats1.Text = strStats

                strStats = "m/z: " & mParentIonStats(intParentIonIndex).MZ
                If blnValidPeakFound Then
                    With .Peak.StatisticalMoments
                        strStats &= ControlChars.NewLine & "Peak StDev: " & StringUtilities.ValueToString(.StDev, 3)
                        strStats &= ControlChars.NewLine & "Peak Skew: " & StringUtilities.ValueToString(.Skew, 4)
                        strStats &= ControlChars.NewLine & "Peak KSStat: " & StringUtilities.ValueToString(.KSStat, 4)
                        strStats &= ControlChars.NewLine & "Data Count Used: " & .DataCountUsed
                    End With

                    If .Peak.SignalToNoiseRatio >= 3 Then
                        strStats &= ControlChars.NewLine & "S/N: " & Math.Round(.Peak.SignalToNoiseRatio, 0).ToString
                    Else
                        strStats &= ControlChars.NewLine & "S/N: " & StringUtilities.ValueToString(.Peak.SignalToNoiseRatio, 4)
                    End If

                    With .Peak.BaselineNoiseStats
                        strStats &= ControlChars.NewLine & "Noise level: " & StringUtilities.ValueToString(.NoiseLevel, 4)
                        strStats &= ControlChars.NewLine & "Noise StDev: " & StringUtilities.ValueToString(.NoiseStDev, 3)
                        strStats &= ControlChars.NewLine & "Points used: " & .PointsUsed.ToString
                        strStats &= ControlChars.NewLine & "Noise Mode Used: " & .NoiseThresholdModeUsed.ToString
                    End With

                End If
                txtStats2.Text = strStats

                txtStats3.Text = LookupSequenceForParentIonIndex(intParentIonIndex)
            End With

        Else
            txtStats1.Text = "Invalid parent ion index: " & intParentIonIndex.ToString
            txtStats2.Text = String.Empty
            txtStats3.Text = String.Empty
        End If

    End Sub

    Private Sub DisplaySICStatsForSelectedParentIon()
        Dim udtSICStats As udtSICStatsType = New udtSICStatsType
        If mParentIonPointerArrayCount > 0 Then
            DisplaySICStats(mParentIonPointerArray(lstParentIonData.SelectedIndex), udtSICStats)
        End If
    End Sub
    Private Sub EnableDisableControls()

        Dim blnUseButterworth As Boolean
        Dim blnUseSavitzkyGolay As Boolean

        If optDoNotResmooth.Checked Then
            blnUseButterworth = False
            blnUseSavitzkyGolay = False
        ElseIf optUseButterworthSmooth.Checked Then
            blnUseButterworth = True
            blnUseSavitzkyGolay = False
        ElseIf optUseSavitzkyGolaySmooth.Checked Then
            blnUseButterworth = False
            blnUseSavitzkyGolay = True
        End If

        txtButterworthSamplingFrequency.Enabled = blnUseButterworth
        txtSavitzkyGolayFilterOrder.Enabled = blnUseSavitzkyGolay


    End Sub

    Private Sub FindMinimumPotentialPeakAreaInRegion(intParentIonIndexStart As Integer, intParentIonIndexEnd As Integer, ByRef udtSICPotentialAreaStatsForRegion As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType)
        ' This function finds the minimum potential peak area in the parent ions between
        '  intParentIonIndexStart and intParentIonIndexEnd
        ' However, the summed intensity is not used if the number of points >= .SICNoiseThresholdIntensity is less than Minimum_Peak_Width

        Dim intParentIonIndex As Integer

        With udtSICPotentialAreaStatsForRegion
            .MinimumPotentialPeakArea = Double.MaxValue
            .PeakCountBasisForMinimumPotentialArea = 0
        End With

        For intParentIonIndex = intParentIonIndexStart To intParentIonIndexEnd
            With mParentIonStats(intParentIonIndex)

                If .SICStats.SICPotentialAreaStatsForPeak.MinimumPotentialPeakArea = 0 And
                   .SICStats.SICPotentialAreaStatsForPeak.PeakCountBasisForMinimumPotentialArea = 0 Then
                    ' Need to compute the minimum potential peak area for intParentIonIndex

                    ' Compute the potential peak area for this SIC
                    mMASICPeakFinder.FindPotentialPeakArea(.DataCount, .SICData, .SICStats.SICPotentialAreaStatsForPeak, mSICPeakFinderOptions)
                End If


                With .SICStats.SICPotentialAreaStatsForPeak

                    If .MinimumPotentialPeakArea > 0 And .PeakCountBasisForMinimumPotentialArea >= MASICPeakFinder.clsMASICPeakFinder.MINIMUM_PEAK_WIDTH Then
                        If .PeakCountBasisForMinimumPotentialArea > udtSICPotentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea Then
                            ' The non valid peak count value is larger than the one associated with the current
                            '  minimum potential peak area; update the minimum peak area to dblPotentialPeakArea
                            udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea = .MinimumPotentialPeakArea
                            udtSICPotentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = .PeakCountBasisForMinimumPotentialArea
                        Else
                            If .MinimumPotentialPeakArea < udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea And
                               .PeakCountBasisForMinimumPotentialArea >= udtSICPotentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea Then
                                udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea = .MinimumPotentialPeakArea
                                udtSICPotentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = .PeakCountBasisForMinimumPotentialArea
                            End If
                        End If
                    End If
                End With
            End With
        Next intParentIonIndex

        If udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea = Double.MaxValue Then
            udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea = 1
        End If

    End Sub

    Private Function FindSICPeakAndAreaForParentIon(intParentIonIndex As Integer, ByRef udtSICStats As udtSICStatsType) As Boolean

        Dim intIndex As Integer
        Dim intParentIonIndexStart As Integer

        Dim udtSICPotentialAreaStatsForRegion As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType
        Dim blnReturnClosestPeak As Boolean

        Dim udtSmoothedYDataSubset As MASICPeakFinder.clsMASICPeakFinder.udtSmoothedYDataSubsetType

        ReDim udtSmoothedYDataSubset.Data(100)
        udtSmoothedYDataSubset.DataCount = 0

        Dim blnRecomputeNoiseLevel As Boolean
        Dim blnSuccess As Boolean

        Try
            ' Determine the minimum potential peak area in the last 500 scans
            intParentIonIndexStart = intParentIonIndex - 500
            If intParentIonIndexStart < 0 Then intParentIonIndexStart = 0
            FindMinimumPotentialPeakAreaInRegion(intParentIonIndexStart, intParentIonIndex, udtSICPotentialAreaStatsForRegion)

            With mParentIonStats(intParentIonIndex)
                blnReturnClosestPeak = Not .CustomSICPeak

                ' Look for .SurveyScanNumber in .SICScans in order to populate udtSICStats.Peak.IndexObserved
                If udtSICStats.Peak.IndexObserved = 0 Then
                    For intIndex = 0 To .DataCount - 1
                        If .SICScans(intIndex) = .SurveyScanNumber Then
                            udtSICStats.Peak.IndexObserved = intIndex
                        End If
                    Next intIndex
                End If

                ' Determine the value for .ParentIonIntensity
                blnSuccess = mMASICPeakFinder.ComputeParentIonIntensity(.DataCount, .SICScans, .SICData, udtSICStats.Peak, .FragScanObserved)

                If .SICStats.Peak.BaselineNoiseStats.NoiseThresholdModeUsed = MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.DualTrimmedMeanByAbundance Then
                    blnRecomputeNoiseLevel = False
                Else
                    blnRecomputeNoiseLevel = True
                    ' Note: We cannot use DualTrimmedMeanByAbundance since we don't have access to the full-length SICs
                    If mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.DualTrimmedMeanByAbundance Then
                        mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance
                    End If
                End If
                blnSuccess = mMASICPeakFinder.FindSICPeakAndArea(.DataCount, .SICScans, .SICData, .SICStats.SICPotentialAreaStatsForPeak, udtSICStats.Peak,
                                                                 udtSmoothedYDataSubset, mSICPeakFinderOptions,
                                                                 udtSICPotentialAreaStatsForRegion,
                                                                 blnReturnClosestPeak, False, blnRecomputeNoiseLevel)
            End With

            ' Copy the data out of udtSmoothedYDataSubset and into udtSICStats.SICSmoothedYData
            With udtSmoothedYDataSubset
                If udtSICStats.SICSmoothedYData.Length <> .DataCount Then
                    ReDim udtSICStats.SICSmoothedYData(.DataCount - 1)
                End If
                Array.Copy(.Data, udtSICStats.SICSmoothedYData, .DataCount)

                udtSICStats.SICSmoothedYDataIndexStart = .DataStartIndex
            End With

            Try
                ' Update the two computed values
                With udtSICStats
                    .SICPeakWidthFullScans = mParentIonStats(intParentIonIndex).SICScans(.Peak.IndexBaseRight) - mParentIonStats(intParentIonIndex).SICScans(.Peak.IndexBaseLeft) + 1
                    .ScanNumberMaxIntensity = mParentIonStats(intParentIonIndex).SICScans(.Peak.IndexMax)
                End With
            Catch ex As Exception
                ' Index out of range; ignore the error
            End Try
        Catch ex As Exception
            MessageBox.Show("Error in FindSICPeakAndAreaForParentIon: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Private Sub FindSimilarParentIon()
        Const SIMILAR_MZ_TOLERANCE As Double = 0.2

        FindSimilarParentIon(SIMILAR_MZ_TOLERANCE)
    End Sub

    Private Sub FindSimilarParentIon(dblSimilarMZTolerance As Double)

        Const DEFAULT_INTENSITY As Single = 1

        Dim intParentIonIndex As Integer
        Dim intIndexCompare As Integer

        Dim intFragIndex As Integer

        Dim intScanIndex As Integer
        Dim intMatchIndex As Integer

        Dim dblParentIonMZ As Double
        Dim intScanStart As Integer
        Dim intScanEnd As Integer

        Dim intSimilarFragScanCount As Integer
        Dim intSimilarFragScans() As Integer

        Dim sngInterpolatedYValue As Single

        Dim blnAddScan As Boolean
        Dim blnSuccess As Boolean


        Try
            If mParentIonStats Is Nothing OrElse mParentIonStats.Length = 0 Then
                Exit Sub
            End If

            ' Initially reserve space for 4 similar frag scan entries
            intSimilarFragScanCount = 0
            ReDim intSimilarFragScans(3)

            For intParentIonIndex = 0 To mParentIonCount - 1

                If mParentIonStats(intParentIonIndex).DataCount <= 0 Then
                    mParentIonStats(intParentIonIndex).SimilarFragScanCount = 0
                    ReDim mParentIonStats(intParentIonIndex).SimilarFragScanList(0)
                    ReDim mParentIonStats(intParentIonIndex).SimilarFragScanPlottingIntensity(0)
                Else

                    With mParentIonStats(intParentIonIndex)
                        dblParentIonMZ = .MZ
                        intScanStart = .SICScans(0)
                        intScanEnd = .SICScans(.DataCount - 1)

                        mParentIonStats(intParentIonIndex).SimilarFragScanCount = 0

                        ' Always store this parent ion's fragmentation scan in intSimilarFragScans
                        ' Note that it's possible for .FragScanObserved to be outside the range of intScanStart and intScanEnd
                        ' We'll allow this, but won't be able to properly determine the abundance value to use for .SimilarFragScanPlottingIntensity()
                        intSimilarFragScans(0) = .FragScanObserved
                        intSimilarFragScanCount = 1

                    End With

                    ' Step through the parent ions and look for others with a similar m/z and a fragmentation scan
                    '   between intScanStart and intScanEnd

                    For intIndexCompare = 0 To mParentIonCount - 1
                        With mParentIonStats(intIndexCompare)
                            If intIndexCompare <> intParentIonIndex AndAlso
                               (.FragScanObserved >= intScanStart AndAlso .FragScanObserved <= intScanEnd) Then

                                If Math.Abs(.MZ - dblParentIonMZ) <= dblSimilarMZTolerance Then
                                    ' Similar parent ion m/z found

                                    If intSimilarFragScanCount >= intSimilarFragScans.Length Then
                                        ReDim Preserve intSimilarFragScans(intSimilarFragScans.Length * 2 - 1)
                                    End If

                                    intSimilarFragScans(intSimilarFragScanCount) = .FragScanObserved
                                    intSimilarFragScanCount += 1
                                End If
                            End If
                        End With
                    Next intIndexCompare

                    With mParentIonStats(intParentIonIndex)

                        If intSimilarFragScanCount > 0 Then
                            ' Sort the data in intSimilarFragScans
                            Array.Sort(intSimilarFragScans, 0, intSimilarFragScanCount)

                            ' Copy the sorted data into .SimilarFragScanList
                            ' When copying, make sure we don't have any duplicates
                            ' Also, populate SimilarFragScanPlottingIntensity
                            ReDim .SimilarFragScanList(intSimilarFragScanCount - 1)
                            ReDim .SimilarFragScanPlottingIntensity(intSimilarFragScanCount - 1)

                            intFragIndex = 0
                            Do While intFragIndex < intSimilarFragScanCount
                                blnAddScan = True
                                If .SimilarFragScanCount > 0 Then
                                    If intSimilarFragScans(intFragIndex) = .SimilarFragScanList(.SimilarFragScanCount - 1) Then
                                        blnAddScan = False
                                    End If
                                End If

                                If blnAddScan Then
                                    .SimilarFragScanList(.SimilarFragScanCount) = intSimilarFragScans(intFragIndex)

                                    ' Look for intSimilarFragScans(intFragIndex) in .SICScans() then use the corresponding
                                    '  intensity value in .SICData()

                                    intMatchIndex = clsBinarySearch.BinarySearchFindNearest(.SICScans, intSimilarFragScans(intFragIndex), .DataCount, clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint)

                                    If intMatchIndex < 0 Then
                                        ' Match not found; find the closest match via brute-force searching
                                        intMatchIndex = -1
                                        For intScanIndex = 0 To .DataCount - 2
                                            If .SICScans(intScanIndex) <= intSimilarFragScans(intFragIndex) AndAlso
                                               .SICScans(intScanIndex + 1) >= intSimilarFragScans(intFragIndex) Then
                                                intMatchIndex = intScanIndex
                                                Exit For
                                            End If
                                        Next intScanIndex
                                    End If

                                    If intMatchIndex >= 0 Then

                                        If intMatchIndex < .DataCount - 1 Then
                                            If intSimilarFragScans(intFragIndex) <= .SICScans(intMatchIndex) Then
                                                ' Frag scan is at or before the first scan number in .SICData()
                                                ' Use the intensity at .SICData(0)
                                                sngInterpolatedYValue = .SICData(0)
                                            Else
                                                blnSuccess = InterpolateY(sngInterpolatedYValue, .SICScans(intMatchIndex), .SICScans(intMatchIndex + 1), .SICData(intMatchIndex), .SICData(intMatchIndex + 1), intSimilarFragScans(intFragIndex))
                                            End If
                                        Else
                                            ' Frag scan is at or after the last scan number in .SICData()
                                            ' Use the last data point in .SICData()
                                            sngInterpolatedYValue = .SICData(.DataCount - 1)
                                        End If

                                        If Not blnSuccess Then
                                            sngInterpolatedYValue = DEFAULT_INTENSITY
                                        End If

                                        .SimilarFragScanPlottingIntensity(.SimilarFragScanCount) = sngInterpolatedYValue
                                    Else
                                        .SimilarFragScanPlottingIntensity(.SimilarFragScanCount) = DEFAULT_INTENSITY
                                    End If

                                    .SimilarFragScanCount += 1
                                End If

                                intFragIndex += 1
                            Loop

                        Else
                            .SimilarFragScanCount = 0
                            ReDim .SimilarFragScanList(0)
                            ReDim .SimilarFragScanPlottingIntensity(0)
                        End If
                    End With

                End If
            Next intParentIonIndex

        Catch ex As Exception
            MessageBox.Show("Error in FindSimilarParentIon: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try
    End Sub

    ''' <summary>
    ''' Search dataSeries to find the closest distance between searchValue and a value in dataSeries
    ''' </summary>
    ''' <param name="dataSeries"></param>
    ''' <param name="dataCount"></param>
    ''' <param name="searchValue"></param>
    ''' <returns></returns>
    Private Function GetClosestDistance(dataSeries As IList(Of Double), dataCount As Integer, searchValue As Integer) As Double

        Dim closestDistance = Double.MaxValue

        For intIndex = 0 To dataCount - 1
            Dim distance = Math.Abs(dataSeries(intIndex) - searchValue)
            If distance < closestDistance Then
                closestDistance = distance
            End If
        Next

        Return closestDistance

    End Function

    Private Function GetSettingVal(strAppName As String, strSectionName As String, strKey As String, DefaultValue As Boolean) As Boolean
        Dim strValue As String

        strValue = GetSetting(strAppName, strSectionName, strKey, DefaultValue.ToString)
        Try
            Return CBool(strValue)
        Catch ex As Exception
            Return DefaultValue
        End Try

    End Function

    Private Function GetSettingVal(strAppName As String, strSectionName As String, strKey As String, DefaultValue As Integer) As Integer
        Dim strValue As String

        strValue = GetSetting(strAppName, strSectionName, strKey, DefaultValue.ToString)
        Try
            Return CInt(strValue)
        Catch ex As Exception
            Return DefaultValue
        End Try

    End Function

    Private Function GetSettingVal(strAppName As String, strSectionName As String, strKey As String, DefaultValue As Single) As Single
        Dim strValue As String

        strValue = GetSetting(strAppName, strSectionName, strKey, DefaultValue.ToString)
        Try
            Return CSng(strValue)
        Catch ex As Exception
            Return DefaultValue
        End Try

    End Function

    Private Sub InitializeControls()
        mMASICPeakFinder = New MASICPeakFinder.clsMASICPeakFinder
        mSICPeakFinderOptions = MASICPeakFinder.clsMASICPeakFinder.GetDefaultSICPeakFinderOptions

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

        mFileLoadTimer = New Timer()

        AddHandler mFileLoadTimer.Tick, AddressOf mFileLoadTimer_Tick

        mFileLoadTimer.Interval = 500
        mFileLoadTimer.Start()


    End Sub

    Private Sub InitializeMsMsResultsStorage()
        Dim dtDataTable As DataTable

        '---------------------------------------------------------
        ' Create the MS/MS Search Engine Results DataTable
        '---------------------------------------------------------
        dtDataTable = New DataTable(TABLE_NAME_MSMSRESULTS)

        ' Add the columns to the datatable
        SharedVBNetRoutines.ADONetRoutines.AppendColumnIntegerToTable(dtDataTable, COL_NAME_SCAN)
        SharedVBNetRoutines.ADONetRoutines.AppendColumnIntegerToTable(dtDataTable, COL_NAME_CHARGE)
        SharedVBNetRoutines.ADONetRoutines.AppendColumnSingleToTable(dtDataTable, COL_NAME_MH)
        SharedVBNetRoutines.ADONetRoutines.AppendColumnSingleToTable(dtDataTable, COL_NAME_XCORR)
        SharedVBNetRoutines.ADONetRoutines.AppendColumnSingleToTable(dtDataTable, COL_NAME_DELTACN)
        SharedVBNetRoutines.ADONetRoutines.AppendColumnSingleToTable(dtDataTable, COL_NAME_DELTACN2)
        SharedVBNetRoutines.ADONetRoutines.AppendColumnIntegerToTable(dtDataTable, COL_NAME_RANKSP)
        SharedVBNetRoutines.ADONetRoutines.AppendColumnIntegerToTable(dtDataTable, COL_NAME_RANKXC)
        SharedVBNetRoutines.ADONetRoutines.AppendColumnIntegerToTable(dtDataTable, COL_NAME_SEQUENCEID)
        SharedVBNetRoutines.ADONetRoutines.AppendColumnIntegerToTable(dtDataTable, COL_NAME_PARENTIONINDEX)

        ' Define a primary key
        With dtDataTable
            Dim PrimaryKeyColumn As DataColumn() = New DataColumn() { .Columns(COL_NAME_SCAN), .Columns(COL_NAME_CHARGE), .Columns(COL_NAME_SEQUENCEID)}
            .PrimaryKey = PrimaryKeyColumn
        End With

        ' Instantiate the DataSet
        mMsMsResults = New DataSet("MsMsData")

        ' Add the table to the DataSet
        mMsMsResults.Tables.Add(dtDataTable)

        '---------------------------------------------------------
        ' Create the Sequence to Protein Map DataTable
        '---------------------------------------------------------
        dtDataTable = New DataTable(TABLE_NAME_SEQ_TO_PROTEIN_MAP)

        ' Add the columns to the datatable
        SharedVBNetRoutines.ADONetRoutines.AppendColumnIntegerToTable(dtDataTable, COL_NAME_SEQUENCEID)
        SharedVBNetRoutines.ADONetRoutines.AppendColumnStringToTable(dtDataTable, COL_NAME_PROTEIN)

        ' Define a primary key
        With dtDataTable
            Dim PrimaryKeyColumn As DataColumn() = New DataColumn() { .Columns(COL_NAME_SEQUENCEID), .Columns(COL_NAME_PROTEIN)}
            .PrimaryKey = PrimaryKeyColumn
        End With

        ' Add the table to the DataSet
        mMsMsResults.Tables.Add(dtDataTable)


        '---------------------------------------------------------
        ' Create the Sequences DataTable
        '---------------------------------------------------------
        InitializeSequencesDataTable()

    End Sub

    Private Sub InitializeSequencesDataTable()
        Dim dtDataTable As DataTable

        dtDataTable = New DataTable(TABLE_NAME_SEQUENCES)

        ' Add the columns to the datatable
        SharedVBNetRoutines.ADONetRoutines.AppendColumnIntegerToTable(dtDataTable, COL_NAME_SEQUENCEID, 0, True, True, True)
        SharedVBNetRoutines.ADONetRoutines.AppendColumnStringToTable(dtDataTable, COL_NAME_SEQUENCE)

        ' Define a primary key
        With dtDataTable
            Dim PrimaryKeyColumn As DataColumn() = New DataColumn() { .Columns(COL_NAME_SEQUENCE)}
            .PrimaryKey = PrimaryKeyColumn
        End With

        ' Add the table to the DataSet
        If mMsMsResults.Tables.Contains(TABLE_NAME_SEQUENCES) Then
            mMsMsResults.Tables.Remove(TABLE_NAME_SEQUENCES)
        End If

        mMsMsResults.Tables.Add(dtDataTable)

    End Sub

    Private Function InterpolateY(ByRef sngInterpolatedYValue As Single, X1 As Integer, X2 As Integer, Y1 As Single, Y2 As Single, intTargetX As Integer) As Boolean
        ' Checks if X1 or X2 is less than intTargetX
        ' If it is, then determines the Y value that corresponds to intTargetX by interpolating the line between (X1, Y1) and (X2, Y2)
        '
        ' Returns True if a match is found; otherwise, returns false

        Dim sngDeltaY As Single
        Dim sngFraction As Single
        Dim intDeltaX As Integer
        Dim sngTargetY As Single

        If X1 < intTargetX Or X2 < intTargetX Then
            If X1 < intTargetX And X2 < intTargetX Then
                ' Both of the X values are less than intTargetX
                ' We cannot interpolate
                Debug.Assert(False, "This code should normally not be reached (frmBrowser->InterpolateY)")
            Else
                intDeltaX = X2 - X1
                sngFraction = (intTargetX - X1) / CSng(intDeltaX)
                sngDeltaY = Y2 - Y1

                sngTargetY = sngFraction * sngDeltaY + Y1

                If Math.Abs(sngTargetY - Y1) >= 0 And Math.Abs(sngTargetY - Y2) >= 0 Then
                    sngInterpolatedYValue = sngTargetY
                    Return True
                Else
                    Debug.Assert(False, "TargetY is not between Y1 and Y2; this shouldn't happen (frmBrowser->InterpolateY)")
                    Return False
                End If

            End If
        Else
            Return False
        End If

    End Function

    Private Sub JumpToScan(Optional intScanNumberToFind As Integer = -1)

        Dim strResponse As String
        Dim intIndex As Integer

        Dim intIndexMatch As Integer
        Dim intScanDifference As Integer

        Try
            If mParentIonPointerArrayCount > 0 Then
                If intScanNumberToFind < 0 Then
                    If lstParentIonData.SelectedIndex >= 0 And lstParentIonData.SelectedIndex <= mParentIonPointerArrayCount Then
                        intScanNumberToFind = mParentIonStats(lstParentIonData.SelectedIndex).FragScanObserved
                    Else
                        intScanNumberToFind = mParentIonStats(0).FragScanObserved
                    End If

                    strResponse = InputBox("Enter the scan number to jump to: ", "Jump to Scan", intScanNumberToFind.ToString)
                    If SharedVBNetRoutines.VBNetRoutines.IsNumber(strResponse) Then
                        intScanNumberToFind = CInt(strResponse)
                    End If
                End If

                If intScanNumberToFind >= 0 Then
                    ' Find the best match to intScanNumberToFind
                    ' First search for an exact match
                    intIndexMatch = -1
                    For intIndex = 0 To lstParentIonData.Items.Count - 1
                        If mParentIonStats(mParentIonPointerArray(intIndex)).FragScanObserved = intScanNumberToFind Then
                            intIndexMatch = intIndex
                            Exit For
                        End If
                    Next intIndex

                    If intIndexMatch >= 0 Then
                        lstParentIonData.SelectedIndex = intIndexMatch
                    Else
                        ' Exact match not found; find the closest match to intScanNumberToFind
                        intScanDifference = Integer.MaxValue
                        For intIndex = 0 To lstParentIonData.Items.Count - 1
                            If Math.Abs(mParentIonStats(mParentIonPointerArray(intIndex)).FragScanObserved - intScanNumberToFind) < intScanDifference Then
                                intScanDifference = Math.Abs(mParentIonStats(mParentIonPointerArray(intIndex)).FragScanObserved - intScanNumberToFind)
                                intIndexMatch = intIndex
                            End If
                        Next intIndex

                        If intIndexMatch >= 0 Then
                            lstParentIonData.SelectedIndex = intIndexMatch
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

    Private Function LookupSequenceForParentIonIndex(intParentIonIndex As Integer) As String

        Dim intSequenceID As Integer
        Dim intSequenceCount As Integer

        Dim objRows() As DataRow
        Dim objSeqRows() As DataRow

        Dim objRow As DataRow

        Dim strSequences As String = String.Empty

        With mMsMsResults.Tables(TABLE_NAME_MSMSRESULTS)
            Try
                objRows = .Select(COL_NAME_PARENTIONINDEX & " = " & intParentIonIndex.ToString)
                intSequenceCount = 0
                For Each objRow In objRows
                    intSequenceID = CInt(objRow.Item(COL_NAME_SEQUENCEID))
                    Try
                        objSeqRows = mMsMsResults.Tables(TABLE_NAME_SEQUENCES).Select(COL_NAME_SEQUENCEID & " = " & intSequenceID.ToString)

                        If Not objSeqRows Is Nothing AndAlso objSeqRows.Length > 0 Then
                            If intSequenceCount > 0 Then
                                strSequences &= ControlChars.NewLine
                            End If
                            strSequences &= CStr(objSeqRows(0).Item(COL_NAME_SEQUENCE))
                            intSequenceCount += 1
                        End If
                    Catch ex As Exception
                        ' Ignore errors here
                    End Try
                Next objRow
            Catch ex As Exception
                strSequences = String.Empty
            End Try
        End With

        Return strSequences

    End Function

    Private Function LookupSequenceID(strSequence As String, strProtein As String) As Integer
        ' Looks for strSequence in mMsMsResults.Tables(TABLE_NAME_SEQUENCES)
        ' Returns the SequenceID if found; adds it if not present
        ' Additionally, adds a mapping between strSequence and strProtein in mMsMsResults.Tables(TABLE_NAME_SEQ_TO_PROTEIN_MAP)

        Dim strSequenceNoSuffixes As String
        Dim objNewRow As DataRow
        Dim intSequenceID As Integer

        intSequenceID = -1

        If Not strSequence Is Nothing Then
            If strSequence.Length >= 4 Then
                If strSequence.Substring(1, 1) = "."c And strSequence.Substring(strSequence.Length - 2, 1) = "."c Then
                    strSequenceNoSuffixes = strSequence.Substring(2, strSequence.Length - 4)
                Else
                    strSequenceNoSuffixes = String.Copy(strSequence)
                End If
            Else
                strSequenceNoSuffixes = String.Copy(strSequence)
            End If

            ' Try to add strSequenceNoSuffixes to .Tables(TABLE_NAME_SEQUENCES)
            Try
                With mMsMsResults.Tables(TABLE_NAME_SEQUENCES)
                    objNewRow = .Rows.Find(strSequenceNoSuffixes)
                    If objNewRow Is Nothing Then
                        objNewRow = .NewRow
                        objNewRow.Item(COL_NAME_SEQUENCE) = strSequenceNoSuffixes
                        .Rows.Add(objNewRow)
                    End If
                    intSequenceID = CInt(objNewRow.Item(COL_NAME_SEQUENCEID))
                End With

                If intSequenceID >= 0 Then
                    Try
                        ' Possibly add strSequenceNoSuffixes and strProtein to .Tables(TABLE_NAME_SEQ_TO_PROTEIN_MAP)
                        With mMsMsResults.Tables(TABLE_NAME_SEQ_TO_PROTEIN_MAP)
                            If Not .Rows.Contains(New Object() {intSequenceID, strProtein}) Then
                                objNewRow = .NewRow
                                objNewRow.Item(COL_NAME_SEQUENCEID) = intSequenceID
                                objNewRow.Item(COL_NAME_PROTEIN) = strProtein
                                .Rows.Add(objNewRow)
                            End If
                        End With
                    Catch ex As Exception
                        ' Ignore errors here
                    End Try
                End If
            Catch ex As Exception
                intSequenceID = -1
            End Try
        End If

        Return intSequenceID

    End Function

    Private Sub NavigateScanList(blnMoveForward As Boolean)
        If blnMoveForward Then
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

    Private Sub PlotData(intIndexToPlot As Integer, ByRef udtSICStats As udtSICStatsType)
        ' intIndexToPlot points to an entry in mParentIonStats()

        ' We plot the data as two different series to allow for different coloring
        Dim intDataCountSeries1, intDataCountSeries2, intDataCountSeries3, intDataCountSeries4 As Integer
        Dim dblXDataSeries1(), dblYDataSeries1() As Double          ' Holds the scans and SIC data for data <=0 (data not part of the peak)
        Dim dblXDataSeries2(), dblYDataSeries2() As Double          ' Holds the scans and SIC data for data > 0 (data part of the peak)
        Dim dblXDataSeries3(), dblYDataSeries3() As Double          ' Holds the scan numbers at which the given m/z was chosen for fragmentation
        Dim dblXDataSeries4(), dblYDataSeries4() As Double          ' Holds the smoothed SIC data

        Try
            If intIndexToPlot >= 0 And intIndexToPlot < mParentIonCount Then

                Me.Cursor = Cursors.WaitCursor
                Dim seriesFormatRequired = False

                If mSpectrum Is Nothing Then
                    mSpectrum = New OxyDataPlotter.Spectrum

                    mSpectrum.SetSpectrumFormWindowCaption("Selected Ion Chromatogram")
                    mSpectrum.SetSeriesCount(4)

                    seriesFormatRequired = True
                End If

                mSpectrum.RemoveAllAnnotations()

                If mParentIonStats(intIndexToPlot).DataCount <= 0 Then Return

                intDataCountSeries1 = 0
                intDataCountSeries2 = 0
                intDataCountSeries3 = mParentIonStats(intIndexToPlot).SimilarFragScanCount
                intDataCountSeries4 = 0

                With mParentIonStats(intIndexToPlot)
                    ReDim dblXDataSeries1(.DataCount + 3)   ' Need extra room for potential zero padding
                    ReDim dblYDataSeries1(.DataCount + 3)

                    ReDim dblXDataSeries2(.DataCount + 3)
                    ReDim dblYDataSeries2(.DataCount + 3)

                    ReDim dblXDataSeries3(.SimilarFragScanCount - 1)
                    ReDim dblYDataSeries3(.SimilarFragScanCount - 1)

                    ReDim dblXDataSeries4(.DataCount)
                    ReDim dblYDataSeries4(.DataCount)
                End With

                Dim blnZeroEdgeSeries1 As Boolean

                If udtSICStats.Peak.IndexBaseLeft = 0 Then
                    ' Zero pad Series 1
                    dblXDataSeries1(0) = mParentIonStats(intIndexToPlot).SICScans(0)
                    dblYDataSeries1(0) = 0
                    intDataCountSeries1 += 1

                    ' Zero pad Series 2
                    dblXDataSeries2(0) = mParentIonStats(intIndexToPlot).SICScans(0)
                    dblYDataSeries2(0) = 0
                    intDataCountSeries2 += 1

                    blnZeroEdgeSeries1 = True
                End If

                ' Initialize this to 0, in case .FragScanObserved is out of range
                Dim dblScanObservedIntensity As Double = 0

                ' Initialize this to the maximum intensity, in case .OptimalPeakApexScanNumber is out of range
                Dim dblOptimalPeakApexIntensity As Double = mParentIonStats(intIndexToPlot).SICIntensityMax

                Dim intSmoothedYDataIndexStart As Integer
                Dim dblSmoothedYData() As Double

                If udtSICStats.SICSmoothedYData Is Nothing OrElse udtSICStats.SICSmoothedYData.Length = 0 Then
                    intSmoothedYDataIndexStart = 0
                    ReDim dblSmoothedYData(-1)
                Else
                    intSmoothedYDataIndexStart = udtSICStats.SICSmoothedYDataIndexStart

                    ReDim dblSmoothedYData(udtSICStats.SICSmoothedYData.Length - 1)
                    udtSICStats.SICSmoothedYData.CopyTo(dblSmoothedYData, 0)
                End If

                ' Populate Series 3 with the similar frag scan values
                For intIndex = 0 To mParentIonStats(intIndexToPlot).SimilarFragScanCount - 1
                    dblXDataSeries3(intIndex) = CDbl(mParentIonStats(intIndexToPlot).SimilarFragScanList(intIndex))
                    dblYDataSeries3(intIndex) = CDbl(mParentIonStats(intIndexToPlot).SimilarFragScanPlottingIntensity(intIndex))
                Next intIndex

                For intIndex = 0 To mParentIonStats(intIndexToPlot).DataCount - 1
                    Dim sngInterpolatedYValue As Single

                    If intIndex < mParentIonStats(intIndexToPlot).DataCount - 1 Then
                        With mParentIonStats(intIndexToPlot)
                            If .SICScans(intIndex) <= .FragScanObserved And .SICScans(intIndex + 1) >= .FragScanObserved Then
                                ' Use the survey scan data to calculate the appropriate intensity for the Frag Scan cursor

                                If InterpolateY(sngInterpolatedYValue, .SICScans(intIndex), .SICScans(intIndex + 1), .SICData(intIndex), .SICData(intIndex + 1), .FragScanObserved) Then
                                    dblScanObservedIntensity = sngInterpolatedYValue
                                End If
                            End If

                            If .SICScans(intIndex) <= .OptimalPeakApexScanNumber And .SICScans(intIndex + 1) >= .OptimalPeakApexScanNumber Then
                                ' Use the survey scan data to calculate the appropriate intensity for the Optimal Peak Apex Scan cursor

                                If InterpolateY(sngInterpolatedYValue, .SICScans(intIndex), .SICScans(intIndex + 1), .SICData(intIndex), .SICData(intIndex + 1), .OptimalPeakApexScanNumber) Then
                                    dblOptimalPeakApexIntensity = sngInterpolatedYValue
                                End If
                            End If
                        End With

                    End If

                    If intIndex >= udtSICStats.Peak.IndexBaseLeft And intIndex <= udtSICStats.Peak.IndexBaseRight Then
                        If intIndex > 0 And Not blnZeroEdgeSeries1 Then
                            ' Zero pad Series 1
                            dblXDataSeries1(intDataCountSeries1) = mParentIonStats(intIndexToPlot).SICScans(intIndex)
                            dblYDataSeries1(intDataCountSeries1) = mParentIonStats(intIndexToPlot).SICData(intIndex)
                            intDataCountSeries1 += 1

                            dblXDataSeries1(intDataCountSeries1) = mParentIonStats(intIndexToPlot).SICScans(intIndex)
                            dblYDataSeries1(intDataCountSeries1) = 0
                            intDataCountSeries1 += 1

                            ' Zero pad Series 2
                            dblXDataSeries2(intDataCountSeries2) = mParentIonStats(intIndexToPlot).SICScans(intIndex)
                            dblYDataSeries2(intDataCountSeries2) = 0
                            intDataCountSeries2 += 1

                            blnZeroEdgeSeries1 = True
                        End If

                        dblXDataSeries2(intDataCountSeries2) = mParentIonStats(intIndexToPlot).SICScans(intIndex)
                        dblYDataSeries2(intDataCountSeries2) = mParentIonStats(intIndexToPlot).SICData(intIndex)
                        intDataCountSeries2 += 1
                    Else
                        If intIndex > 0 And blnZeroEdgeSeries1 Then
                            ' Zero pad Series 2
                            dblXDataSeries2(intDataCountSeries2) = mParentIonStats(intIndexToPlot).SICScans(intIndex - 1)
                            dblYDataSeries2(intDataCountSeries2) = 0
                            intDataCountSeries2 += 1

                            ' Zero pad Series 1
                            dblXDataSeries1(intDataCountSeries1) = mParentIonStats(intIndexToPlot).SICScans(intIndex - 1)
                            dblYDataSeries1(intDataCountSeries1) = 0
                            intDataCountSeries1 += 1

                            dblXDataSeries1(intDataCountSeries1) = mParentIonStats(intIndexToPlot).SICScans(intIndex - 1)
                            dblYDataSeries1(intDataCountSeries1) = mParentIonStats(intIndexToPlot).SICData(intIndex - 1)
                            intDataCountSeries1 += 1
                            blnZeroEdgeSeries1 = False
                        End If

                        dblXDataSeries1(intDataCountSeries1) = mParentIonStats(intIndexToPlot).SICScans(intIndex)
                        dblYDataSeries1(intDataCountSeries1) = mParentIonStats(intIndexToPlot).SICData(intIndex)
                        intDataCountSeries1 += 1
                    End If

                    If intIndex >= intSmoothedYDataIndexStart AndAlso Not dblSmoothedYData Is Nothing AndAlso
                       intIndex - intSmoothedYDataIndexStart < dblSmoothedYData.Length Then
                        dblXDataSeries4(intDataCountSeries4) = mParentIonStats(intIndexToPlot).SICScans(intIndex)
                        dblYDataSeries4(intDataCountSeries4) = dblSmoothedYData(intIndex - intSmoothedYDataIndexStart)
                        intDataCountSeries4 += 1
                    End If
                Next intIndex

                ' Shrink the data arrays
                ' SIC Data
                ReDim Preserve dblXDataSeries1(intDataCountSeries1 - 1)
                ReDim Preserve dblYDataSeries1(intDataCountSeries1 - 1)

                ' SIC Peak
                ReDim Preserve dblXDataSeries2(intDataCountSeries2 - 1)
                ReDim Preserve dblYDataSeries2(intDataCountSeries2 - 1)

                ' Smoothed Data
                ReDim Preserve dblXDataSeries4(intDataCountSeries4 - 1)
                ReDim Preserve dblYDataSeries4(intDataCountSeries4 - 1)

                Dim maxIntensity As Double = dblYDataSeries2.Max()


                mSpectrum.ShowSpectrum()

                mSpectrum.SetDataXvsY(1, dblXDataSeries1, dblYDataSeries1, intDataCountSeries1, "SIC Data")

                Dim strCaption As String

                If udtSICStats.Peak.ShoulderCount = 1 Then
                    strCaption = "SIC Data Peak (1 shoulder peak)"
                ElseIf udtSICStats.Peak.ShoulderCount > 1 Then
                    strCaption = "SIC Data Peak (" & udtSICStats.Peak.ShoulderCount & " shoulder peaks)"
                Else
                    strCaption = "SIC Data Peak"
                End If
                mSpectrum.SetDataXvsY(2, dblXDataSeries2, dblYDataSeries2, intDataCountSeries2, strCaption)

                strCaption = "Similar Frag scans"
                mSpectrum.SetDataXvsY(3, dblXDataSeries3, dblYDataSeries3, intDataCountSeries3, strCaption)

                If chkShowSmoothedData.Checked Then
                    If mSpectrum.GetSeriesCount < 4 Then
                        mSpectrum.SetSeriesCount(4)
                        mSpectrum.SetSeriesPlotMode(4, OxyDataPlotter.ctlOxyPlotControl.pmPlotModeConstants.pmLines, True)
                        mSpectrum.SetSeriesPointStyle(4, OxyPlot.MarkerType.None)
                        mSpectrum.SetSeriesColor(4, System.Drawing.Color.Purple)
                        mSpectrum.SetSeriesLineWidth(4, 2)
                    End If
                    mSpectrum.SetDataXvsY(4, dblXDataSeries4, dblYDataSeries4, intDataCountSeries4, "Smoothed data")
                Else
                    If mSpectrum.GetSeriesCount >= 4 Then
                        mSpectrum.SetSeriesCount(3)
                    End If
                End If

                If seriesFormatRequired Then

                    ' SIC Data
                    mSpectrum.SetSeriesPlotMode(1, OxyDataPlotter.ctlOxyPlotControl.pmPlotModeConstants.pmPointsAndLines, True)

                    ' SICData Peak
                    mSpectrum.SetSeriesPlotMode(2, OxyDataPlotter.ctlOxyPlotControl.pmPlotModeConstants.pmPointsAndLines, False)

                    ' Similar Frag Scans
                    mSpectrum.SetSeriesPlotMode(3, OxyDataPlotter.ctlOxyPlotControl.pmPlotModeConstants.pmPoints, False)

                    ' Smoothed Data
                    mSpectrum.SetSeriesPlotMode(4, OxyDataPlotter.ctlOxyPlotControl.pmPlotModeConstants.pmPointsAndLines, False)

                    mSpectrum.SetDisplayPrecisionX(0)
                    mSpectrum.SetDisplayPrecisionY(0)

                End If

                mSpectrum.SetSeriesLineStyle(1, OxyPlot.LineStyle.Automatic)
                mSpectrum.SetSeriesLineStyle(2, OxyPlot.LineStyle.Automatic)
                mSpectrum.SetSeriesLineStyle(3, OxyPlot.LineStyle.None)
                mSpectrum.SetSeriesLineStyle(4, OxyPlot.LineStyle.Automatic)

                mSpectrum.SetSeriesPointStyle(1, OxyPlot.MarkerType.Diamond)
                mSpectrum.SetSeriesPointStyle(2, OxyPlot.MarkerType.Square)
                mSpectrum.SetSeriesPointStyle(3, OxyPlot.MarkerType.Circle)
                mSpectrum.SetSeriesPointStyle(4, OxyPlot.MarkerType.None)

                mSpectrum.SetSeriesColor(1, System.Drawing.Color.Blue)
                mSpectrum.SetSeriesColor(2, System.Drawing.Color.Red)
                mSpectrum.SetSeriesColor(3, System.Drawing.Color.FromArgb(255, 20, 210, 20))
                mSpectrum.SetSeriesColor(4, System.Drawing.Color.Purple)

                mSpectrum.SetSeriesLineWidth(1, 1)
                mSpectrum.SetSeriesLineWidth(2, 2)
                mSpectrum.SetSeriesLineWidth(4, 2)

                mSpectrum.SetSeriesPointSize(3, 7)

                Dim annotationOffsetY = maxIntensity * 0.05
                Dim arrowLengthPixels = 15
                Dim captionOffsetDirection As ctlOxyPlotControl.eCaptionOffsetDirection

                ' Old: mSpectrum.SetCursorPosition(CDbl(.FragScanObserved), dblScanObservedIntensity, 1)
                'mSpectrum.SetAnnotationByXY(mParentIonStats(intIndexToPlot).FragScanObserved, dblScanObservedIntensity + annotationOffsetY, "MS2")

                ' ToDo: Determine whether to associate the annotation with series 1 or series 2

                If mParentIonStats(intIndexToPlot).FragScanObserved <= mParentIonStats(intIndexToPlot).OptimalPeakApexScanNumber Then
                    captionOffsetDirection = ctlOxyPlotControl.eCaptionOffsetDirection.TopLeft
                Else
                    captionOffsetDirection = ctlOxyPlotControl.eCaptionOffsetDirection.TopRight
                End If

                Dim fragScanObserved = mParentIonStats(intIndexToPlot).FragScanObserved
                Dim closestDistance1 As Double = GetClosestDistance(dblXDataSeries1, intDataCountSeries1, fragScanObserved)
                Dim closestDistance2 As Double = GetClosestDistance(dblXDataSeries2, intDataCountSeries2, fragScanObserved)

                Dim seriesToUse As Integer
                If closestDistance2 < closestDistance1 Then
                    seriesToUse = 2
                Else
                    seriesToUse = 1
                End If

                mSpectrum.SetAnnotationForDataPoint(seriesToUse, fragScanObserved, dblScanObservedIntensity,
                                                    "MS2", captionOffsetDirection, arrowLengthPixels)

                If mnuEditShowOptimalPeakApexCursor.Checked Then
                    mSpectrum.SetAnnotationForDataPoint(2, mParentIonStats(intIndexToPlot).OptimalPeakApexScanNumber, dblOptimalPeakApexIntensity,
                                            "Peak", ctlOxyPlotControl.eCaptionOffsetDirection.TopLeft, arrowLengthPixels)
                End If

                Dim intXRangeHalfWidth As Integer
                If SharedVBNetRoutines.VBNetRoutines.IsNumber(txtFixXRange.Text) Then
                    intXRangeHalfWidth = CInt(CInt(txtFixXRange.Text) / 2)
                Else
                    intXRangeHalfWidth = 0
                End If

                Dim sngYRange As Single
                If SharedVBNetRoutines.VBNetRoutines.IsNumber(txtFixYRange.Text) Then
                    sngYRange = CSng(txtFixYRange.Text)
                Else
                    sngYRange = 0
                End If

                Dim blnXFixed As Boolean
                Dim blnYFixed As Boolean

                If chkFixXRange.Checked And intXRangeHalfWidth > 0 Then
                    mSpectrum.SetAutoscaleXAxis(False)
                    mSpectrum.SetRangeX(udtSICStats.ScanNumberMaxIntensity - intXRangeHalfWidth, udtSICStats.ScanNumberMaxIntensity + intXRangeHalfWidth)
                    blnXFixed = True
                End If

                If chkFixYRange.Checked And sngYRange > 0 Then
                    mSpectrum.SetAutoscaleYAxis(False)
                    mSpectrum.SetRangeY(0, CDbl(sngYRange))
                    blnYFixed = True
                End If


                'If blnXFixed And blnYFixed Then
                '    ' Do not autoscale anything
                'ElseIf blnXFixed Then
                '    mSpectrum.SetAutoscaleYAxis(True)
                'ElseIf blnYFixed Then
                '    mSpectrum.SetAutoscaleXAxis(True)
                'End If

            End If

        Catch ex As Exception
            Dim sTrace = PRISM.clsStackTraceFormatter.GetExceptionStackTraceMultiLine(ex)
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
        ' For each row in mMsMsResults.Tables(TABLE_NAME_MSMSRESULTS), find the corresponding row in mParentIonStats

        ' Construct a mapping between .FragScanObserved and Index in mParentIonStats
        ' If multiple parent ions have the same value for .FragScanObserved, then the we will only track the mapping to the first one

        Dim intIndex As Integer

        Dim htFragScanToIndex As New Hashtable
        Dim objRow As DataRow

        For intIndex = 0 To mParentIonCount - 1
            If Not htFragScanToIndex.ContainsKey(mParentIonStats(intIndex).FragScanObserved) Then
                htFragScanToIndex.Add(mParentIonStats(intIndex).FragScanObserved, intIndex)
            End If
        Next intIndex

        For Each objRow In mMsMsResults.Tables(TABLE_NAME_MSMSRESULTS).Rows
            If htFragScanToIndex.Contains(objRow.Item(COL_NAME_SCAN)) Then
                objRow.Item(COL_NAME_PARENTIONINDEX) = htFragScanToIndex(objRow.Item(COL_NAME_SCAN))
            End If
        Next objRow
    End Sub

    Private Sub PopulateSpectrumList(intScanNumberToHighlight As Integer)

        Dim intIndex As Integer

        Dim strParentIonDesc As String

        Try
            lstParentIonData.Items.Clear()

            If mParentIonPointerArrayCount > 0 Then
                For intIndex = 0 To mParentIonPointerArrayCount - 1
                    With mParentIonStats(mParentIonPointerArray(intIndex))
                        strParentIonDesc = "Scan " & .FragScanObserved.ToString & "  (" & Math.Round(.MZ, 4).ToString & " m/z)"
                        lstParentIonData.Items.Add(strParentIonDesc)
                    End With
                Next

                lstParentIonData.SelectedIndex = 0

                JumpToScan(intScanNumberToHighlight)

                fraNavigation.Enabled = True
            Else
                fraNavigation.Enabled = False
            End If

        Catch ex As Exception
            MessageBox.Show("Error in PopulateSpectrumList: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try
    End Sub

    Private Sub PositionControls()
        Dim intDesiredValue As Integer

        'intDesiredValue = Me.Height - lstParentIonData.Top - 425
        'If intDesiredValue < 5 Then
        '    intDesiredValue = 5
        'End If
        'lstParentIonData.Height = intDesiredValue

        'fraSortOrderAndStats.Top = lstParentIonData.Top + lstParentIonData.Height + 1
        If txtStats3.Visible Then
            intDesiredValue = fraSortOrderAndStats.Height - txtStats1.Top - txtStats3.Height - 1 - 5
        Else
            intDesiredValue = fraSortOrderAndStats.Height - txtStats1.Top - 1 - 5
        End If
        If intDesiredValue < 1 Then intDesiredValue = 1
        txtStats1.Height = intDesiredValue
        txtStats2.Height = intDesiredValue

        txtStats3.Top = txtStats1.Top + txtStats1.Height + 1

    End Sub

    Private Sub ReadDataFileXMLTextReader(strFilePath As String)

        Dim srDataFile As StreamReader

        Dim eCurrentXMLDataFileSection As eCurrentXMLDataFileSectionConstants

        Dim strIndexInXMLFile As String = "-1"
        Dim blnValidParentIon As Boolean

        Dim dblPercentComplete As Double
        Dim strErrorLog As String

        Dim sngSICToleranceDa As Single
        Dim sngSimilarIonMZToleranceHalfWidth As Single
        Dim blnFindPeaksOnSmoothedData As Boolean
        Dim blnUseButterworthSmooth, blnUseSavitzkyGolaySmooth As Boolean
        Dim sngButterworthSamplingFrequency As Single
        Dim intSavitzkyGolayFilterOrder As Integer

        Dim intScanStart As Integer
        Dim intPeakScanStart, intPeakScanEnd As Integer
        Dim intIndex As Integer
        Dim intCharIndex As Integer
        Dim intInterval As Integer

        Dim blnSmoothedDataFound As Boolean
        Dim blnBaselineNoiseStatsFound As Boolean

        Dim strValue As String

        Dim strScanIntervals As String = String.Empty
        Dim strIntensityDataList As String = String.Empty
        Dim strMassDataList As String = String.Empty
        Dim strSmoothedYDataList As String = String.Empty

        Dim strDelimiters As String = " ,;" & ControlChars.Tab
        Dim strDelimList As Char() = strDelimiters.ToCharArray()
        Dim strValueList() As String = Nothing

        Dim strSICScanType As String

        If Not File.Exists(strFilePath) Then
            Exit Sub
        End If

        ' Initialize the progress form
        Dim objProgress As ProgressFormNET.frmProgress
        objProgress = New ProgressFormNET.frmProgress

        Try

            ' Initialize the stream reader and the XML Text Reader
            srDataFile = New StreamReader(strFilePath)
            Using objXMLReader As System.Xml.XmlTextReader = New System.Xml.XmlTextReader(srDataFile)

                strErrorLog = String.Empty

                objProgress.InitializeProgressForm("Reading file " & ControlChars.NewLine & SharedVBNetRoutines.VBNetRoutines.CompactPathString(strFilePath, 40), 0, 1, True)
                objProgress.Show()
                Application.DoEvents()

                ' Initialize mParentIonStats, initially reserving space for 5000 items
                mParentIonCount = 0
                ReDim mParentIonStats(4999)

                eCurrentXMLDataFileSection = eCurrentXMLDataFileSectionConstants.UnknownFile
                blnValidParentIon = False

                Do While objXMLReader.Read()

                    XMLTextReaderSkipWhitespace(objXMLReader)
                    If Not objXMLReader.ReadState = Xml.ReadState.Interactive Then Exit Do

                    If objXMLReader.Depth < 2 Then
                        If objXMLReader.NodeType = Xml.XmlNodeType.Element Then
                            Select Case objXMLReader.Name
                                Case "ParentIon"
                                    eCurrentXMLDataFileSection = eCurrentXMLDataFileSectionConstants.ParentIons
                                    blnValidParentIon = False

                                    If objXMLReader.HasAttributes Then
                                        blnBaselineNoiseStatsFound = False
                                        intScanStart = 0
                                        intPeakScanStart = 0
                                        intPeakScanEnd = 0

                                        strScanIntervals = String.Empty
                                        strIntensityDataList = String.Empty
                                        strMassDataList = String.Empty
                                        strSmoothedYDataList = String.Empty

                                        strIndexInXMLFile = objXMLReader.GetAttribute("Index")

                                        If mParentIonStats.Length = mParentIonCount Then
                                            ReDim Preserve mParentIonStats(mParentIonStats.Length * 2 - 1)
                                        End If

                                        With mParentIonStats(mParentIonCount)
                                            .Index = Integer.Parse(strIndexInXMLFile)
                                            .MZ = 0
                                            .SICIntensityMax = 0
                                            With .SICStats
                                                .SICPeakWidthFullScans = 0
                                                With .Peak
                                                    .IndexBaseLeft = -1
                                                    .IndexBaseRight = -1
                                                    .MaxIntensityValue = 0
                                                    .ShoulderCount = 0
                                                End With
                                            End With
                                            .DataCount = 0
                                        End With
                                        mParentIonCount += 1
                                        blnValidParentIon = True

                                        ' Update the progress bar
                                        dblPercentComplete = srDataFile.BaseStream.Position / srDataFile.BaseStream.Length
                                        If dblPercentComplete > 1 Then dblPercentComplete = 1

                                        objProgress.UpdateProgressBar(dblPercentComplete)
                                        Application.DoEvents()
                                        If objProgress.KeyPressAbortProcess Then Exit Do

                                        ' Advance to the next tag
                                        objXMLReader.Read()
                                        XMLTextReaderSkipWhitespace(objXMLReader)
                                        If Not objXMLReader.ReadState = Xml.ReadState.Interactive Then Exit Do

                                    Else
                                        ' Attribute isn't present; skip this parent ion
                                        strIndexInXMLFile = "-1"
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
                        ElseIf objXMLReader.NodeType = Xml.XmlNodeType.EndElement Then
                            If objXMLReader.Name = "ParentIon" Then
                                If blnValidParentIon Then
                                    ' End element found for the current parent ion

                                    With mParentIonStats(mParentIonCount - 1)
                                        ' Split apart the value list variables

                                        .SICScans(0) = intScanStart
                                        ' strScanIntervals contains the intervals from each scan to the next
                                        ' If the interval is <=9, then it is stored as a number
                                        ' For intervals between 10 and 35, uses letters A to Z
                                        ' For intervals between 36 and 61, uses letters A to Z
                                        If Not strScanIntervals Is Nothing Then
                                            For intCharIndex = 1 To strScanIntervals.Length - 1
                                                If Char.IsNumber(strScanIntervals.Chars(intCharIndex)) Then
                                                    intInterval = CInt(strScanIntervals.Substring(intCharIndex, 1))
                                                Else
                                                    If Char.IsUpper(strScanIntervals.Chars(intCharIndex)) Then
                                                        ' Uppercase letter
                                                        intInterval = Asc(strScanIntervals.Chars(intCharIndex)) - 55
                                                    ElseIf Char.IsLower(strScanIntervals.Chars(intCharIndex)) Then
                                                        ' Lowercase letter
                                                        intInterval = Asc(strScanIntervals.Chars(intCharIndex)) - 61
                                                    Else
                                                        ' Not a letter or a number; unknown interval (use 1)
                                                        intInterval = 1
                                                    End If

                                                End If
                                                .SICScans(intCharIndex) = .SICScans(intCharIndex - 1) + intInterval
                                            Next intCharIndex
                                        Else
                                            If strErrorLog.Length < 1024 Then
                                                strErrorLog &= "Missing 'SICScanInterval' node for parent ion '" & strIndexInXMLFile & "'" & ControlChars.NewLine
                                            End If
                                        End If


                                        ' Split apart the Intensity data list using the delimiters in strDelimList
                                        If Not strIntensityDataList Is Nothing Then
                                            strValueList = strIntensityDataList.Trim.Split(strDelimList)
                                            For intIndex = 0 To strValueList.Length - 1
                                                If intIndex >= .DataCount Then
                                                    If strErrorLog.Length < 1024 Then
                                                        strErrorLog &= "Too many intensity data points found in parent ion '" & strIndexInXMLFile & "'" & ControlChars.NewLine
                                                    End If
                                                    Exit For
                                                End If
                                                If SharedVBNetRoutines.VBNetRoutines.IsNumber(strValueList(intIndex)) Then
                                                    .SICData(intIndex) = CSng(strValueList(intIndex))
                                                Else
                                                    .SICData(intIndex) = 0
                                                End If
                                            Next intIndex
                                        Else
                                            If strErrorLog.Length < 1024 Then
                                                strErrorLog &= "Missing 'IntensityDataList' node for parent ion '" & strIndexInXMLFile & "'" & ControlChars.NewLine
                                            End If
                                        End If

                                        ' Split apart the Mass data list using the delimiters in strDelimList
                                        If Not strMassDataList Is Nothing Then
                                            strValueList = strMassDataList.Trim.Split(strDelimList)
                                            For intIndex = 0 To strValueList.Length - 1
                                                If intIndex >= .DataCount Then
                                                    If strErrorLog.Length < 1024 Then
                                                        strErrorLog &= "Too many mass data points found in parent ion '" & strIndexInXMLFile & "'" & ControlChars.NewLine
                                                    End If
                                                    Exit For
                                                End If
                                                If SharedVBNetRoutines.VBNetRoutines.IsNumber(strValueList(intIndex)) Then
                                                    .SICMassData(intIndex) = CSng(strValueList(intIndex))
                                                Else
                                                    .SICMassData(intIndex) = 0
                                                End If
                                            Next intIndex
                                        Else
                                            If strErrorLog.Length < 1024 Then
                                                strErrorLog &= "Missing 'IntensityDataList' node for parent ion '" & strIndexInXMLFile & "'" & ControlChars.NewLine
                                            End If
                                        End If


                                        ' Split apart the smoothed Y data using the delimiters in strDelimList
                                        If Not strSmoothedYDataList Is Nothing Then
                                            strValueList = strSmoothedYDataList.Trim.Split(strDelimList)

                                            For intIndex = 0 To strValueList.Length - 1
                                                If intIndex >= .DataCount Then
                                                    If strErrorLog.Length < 1024 Then
                                                        strErrorLog &= "Too many intensity data points found in parent ion '" & strIndexInXMLFile & "'" & ControlChars.NewLine
                                                    End If
                                                    Exit For
                                                End If
                                                If SharedVBNetRoutines.VBNetRoutines.IsNumber(strValueList(intIndex)) Then
                                                    .SICStats.SICSmoothedYData(intIndex) = CSng(strValueList(intIndex))
                                                    blnSmoothedDataFound = True
                                                Else
                                                    .SICStats.SICSmoothedYData(intIndex) = 0
                                                End If
                                            Next intIndex

                                            ReDim Preserve .SICStats.SICSmoothedYData(Math.Min(strValueList.Length, .DataCount) - 1)
                                        Else
                                            ' No smoothed Y data; that's OK
                                            ReDim .SICStats.SICSmoothedYData(-1)
                                        End If

                                        ' Update the peak stats variables
                                        For intIndex = 0 To .DataCount - 1
                                            If .SICData(intIndex) > .SICIntensityMax Then
                                                .SICIntensityMax = .SICData(intIndex)
                                            End If
                                        Next intIndex

                                        If .SICStats.Peak.IndexBaseLeft < 0 Then
                                            .SICStats.Peak.IndexBaseLeft = 0
                                        Else
                                            If .SICStats.Peak.IndexBaseLeft < .SICScans.Length Then
                                                If intPeakScanStart <> .SICScans(.SICStats.Peak.IndexBaseLeft) Then
                                                    If strErrorLog.Length < 1024 Then
                                                        strErrorLog &= "PeakScanStart does not agree with SICPeakIndexStart for parent ion ' " & strIndexInXMLFile & "'" & ControlChars.NewLine
                                                    End If
                                                End If
                                            Else
                                                If strErrorLog.Length < 1024 Then
                                                    strErrorLog &= ".SICStats.Peak.IndexBaseLeft is larger than .SICScans.Length for parent ion ' " & strIndexInXMLFile & "'" & ControlChars.NewLine
                                                End If
                                            End If
                                        End If

                                        If .SICStats.Peak.IndexBaseRight < 0 Then
                                            .SICStats.Peak.IndexBaseRight = .DataCount - 1
                                        Else
                                            If .SICStats.Peak.IndexBaseRight < .SICScans.Length Then
                                                If intPeakScanEnd <> .SICScans(.SICStats.Peak.IndexBaseRight) Then
                                                    If strErrorLog.Length < 1024 Then
                                                        strErrorLog &= "PeakScanEnd does not agree with SICPeakIndexEnd for parent ion ' " & strIndexInXMLFile & "'" & ControlChars.NewLine
                                                    End If
                                                End If
                                            Else
                                                If strErrorLog.Length < 1024 Then
                                                    strErrorLog &= ".SICStats.Peak.IndexBaseRight is larger than .SICScans.Length for parent ion ' " & strIndexInXMLFile & "'" & ControlChars.NewLine
                                                End If
                                            End If
                                        End If

                                        If .SICStats.Peak.IndexBaseRight < .SICScans.Length And .SICStats.Peak.IndexBaseLeft < .SICScans.Length Then
                                            .SICStats.SICPeakWidthFullScans = .SICScans(.SICStats.Peak.IndexBaseRight) - .SICScans(.SICStats.Peak.IndexBaseLeft) + 1
                                        End If

                                        If Not blnBaselineNoiseStatsFound Then
                                            ' Compute the Noise Threshold for this SIC
                                            ' Note: We cannot use DualTrimmedMeanByAbundance since we don't have access to the full-length SICs
                                            If mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.DualTrimmedMeanByAbundance Then
                                                mSICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance
                                            End If

                                            mMASICPeakFinder.ComputeNoiseLevelInPeakVicinity(.DataCount, .SICScans, .SICData, .SICStats.Peak, mSICPeakFinderOptions.SICBaselineNoiseOptions)
                                        End If

                                    End With

                                End If
                                blnValidParentIon = False
                            End If
                        End If
                    End If

                    If eCurrentXMLDataFileSection <> eCurrentXMLDataFileSectionConstants.UnknownFile AndAlso objXMLReader.NodeType = Xml.XmlNodeType.Element Then
                        Select Case eCurrentXMLDataFileSection
                            Case eCurrentXMLDataFileSectionConstants.Options
                                Try
                                    Select Case objXMLReader.Name
                                        Case "SICToleranceDa"
                                            sngSICToleranceDa = Single.Parse(XMLTextReaderGetInnerText(objXMLReader))
                                        Case "SimilarIonMZToleranceHalfWidth"
                                            sngSimilarIonMZToleranceHalfWidth = Single.Parse(XMLTextReaderGetInnerText(objXMLReader))
                                        Case "FindPeaksOnSmoothedData"
                                            blnFindPeaksOnSmoothedData = Boolean.Parse(XMLTextReaderGetInnerText(objXMLReader))
                                        Case "UseButterworthSmooth"
                                            blnUseButterworthSmooth = Boolean.Parse(XMLTextReaderGetInnerText(objXMLReader))
                                        Case "ButterworthSamplingFrequency"
                                            sngButterworthSamplingFrequency = Single.Parse(XMLTextReaderGetInnerText(objXMLReader))
                                        Case "UseSavitzkyGolaySmooth"
                                            blnUseSavitzkyGolaySmooth = Boolean.Parse(XMLTextReaderGetInnerText(objXMLReader))
                                        Case "SavitzkyGolayFilterOrder"
                                            intSavitzkyGolayFilterOrder = Integer.Parse(XMLTextReaderGetInnerText(objXMLReader))
                                        Case Else
                                            ' Ignore the setting
                                    End Select

                                Catch ex As Exception
                                    ' Ignore any errors looking up smoothing options
                                End Try

                            Case eCurrentXMLDataFileSectionConstants.ParentIons
                                If blnValidParentIon Then
                                    Try
                                        With mParentIonStats(mParentIonCount - 1)
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
                                                    strSICScanType = XMLTextReaderGetInnerText(objXMLReader)
                                                    If strSICScanType.ToLower = "fragscan" Then
                                                        .SICScanType = eScanTypeConstants.FragScan
                                                    Else
                                                        .SICScanType = eScanTypeConstants.SurveyScan
                                                    End If
                                                Case "PeakScanStart"
                                                    intPeakScanStart = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakScanEnd"
                                                    intPeakScanEnd = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakScanMaxIntensity"
                                                    .SICStats.ScanNumberMaxIntensity = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakIntensity"
                                                    .SICStats.Peak.MaxIntensityValue = CSng(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakSignalToNoiseRatio"
                                                    .SICStats.Peak.SignalToNoiseRatio = CSng(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "FWHMInScans"
                                                    .SICStats.Peak.FWHMScanWidth = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakArea"
                                                    .SICStats.Peak.Area = CSng(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "ShoulderCount"
                                                    .SICStats.Peak.ShoulderCount = CInt(XMLTextReaderGetInnerText(objXMLReader))

                                                Case "ParentIonIntensity"
                                                    .SICStats.Peak.ParentIonIntensity = CSng(XMLTextReaderGetInnerText(objXMLReader))

                                                Case "PeakBaselineNoiseLevel"
                                                    .SICStats.Peak.BaselineNoiseStats.NoiseLevel = CSng(XMLTextReaderGetInnerText(objXMLReader))
                                                    blnBaselineNoiseStatsFound = True
                                                Case "PeakBaselineNoiseStDev"
                                                    .SICStats.Peak.BaselineNoiseStats.NoiseStDev = CSng(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakBaselinePointsUsed"
                                                    .SICStats.Peak.BaselineNoiseStats.PointsUsed = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "NoiseThresholdModeUsed"
                                                    .SICStats.Peak.BaselineNoiseStats.NoiseThresholdModeUsed = CType(XMLTextReaderGetInnerText(objXMLReader), MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)

                                                Case "StatMomentsArea"
                                                    .SICStats.Peak.StatisticalMoments.Area = CSng(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "CenterOfMassScan"
                                                    .SICStats.Peak.StatisticalMoments.CenterOfMassScan = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakStDev"
                                                    .SICStats.Peak.StatisticalMoments.StDev = CSng(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakSkew"
                                                    .SICStats.Peak.StatisticalMoments.Skew = CSng(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "PeakKSStat"
                                                    .SICStats.Peak.StatisticalMoments.KSStat = CSng(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "StatMomentsDataCountUsed"
                                                    .SICStats.Peak.StatisticalMoments.DataCountUsed = CInt(XMLTextReaderGetInnerText(objXMLReader))

                                                Case "SICScanStart"
                                                    intScanStart = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "SICScanIntervals"
                                                    strScanIntervals = XMLTextReaderGetInnerText(objXMLReader)
                                                Case "SICPeakIndexStart"
                                                    .SICStats.Peak.IndexBaseLeft = CInt(XMLTextReaderGetInnerText(objXMLReader))
                                                Case "SICPeakIndexEnd"
                                                    .SICStats.Peak.IndexBaseRight = CInt(XMLTextReaderGetInnerText(objXMLReader))

                                                Case "SICDataCount"
                                                    strValue = XMLTextReaderGetInnerText(objXMLReader)
                                                    If SharedVBNetRoutines.VBNetRoutines.IsNumber(strValue) Then
                                                        .DataCount = CInt(strValue)
                                                    Else
                                                        .DataCount = 0
                                                    End If

                                                    If .DataCount > 0 Then
                                                        ' Initialize the .SIC Arrays
                                                        ReDim .SICScans(.DataCount - 1)
                                                        ReDim .SICData(.DataCount - 1)
                                                        ReDim .SICMassData(.DataCount - 1)
                                                        ReDim .SICStats.SICSmoothedYData(.DataCount - 1)
                                                    Else
                                                        ReDim .SICScans(-1)
                                                        ReDim .SICData(-1)
                                                        ReDim .SICMassData(-1)
                                                    End If

                                                Case "SICSmoothedYDataIndexStart"
                                                    strValue = XMLTextReaderGetInnerText(objXMLReader)
                                                    If SharedVBNetRoutines.VBNetRoutines.IsNumber(strValue) Then
                                                        .SICStats.SICSmoothedYDataIndexStart = CInt(strValue)
                                                    Else
                                                        .SICStats.SICSmoothedYDataIndexStart = 0
                                                    End If
                                                Case "IntensityDataList"
                                                    strIntensityDataList = XMLTextReaderGetInnerText(objXMLReader)
                                                Case "MassDataList"
                                                    strMassDataList = XMLTextReaderGetInnerText(objXMLReader)
                                                Case "SmoothedYDataList"
                                                    strSmoothedYDataList = XMLTextReaderGetInnerText(objXMLReader)
                                                Case Else
                                                    ' Unknown child node name; ignore it
                                            End Select
                                        End With
                                    Catch ex As Exception
                                        ' Error parsing value from the ParentIon data
                                        If strErrorLog.Length < 1024 Then
                                            strErrorLog &= "Error parsing value for parent ion '" & strIndexInXMLFile & "'" & ControlChars.NewLine
                                        End If
                                        blnValidParentIon = False
                                    End Try
                                End If
                        End Select
                    End If

                Loop

            End Using

            ' Shrink mParentIonStats
            If mParentIonCount > 0 Then
                ReDim Preserve mParentIonStats(mParentIonCount - 1)
            Else
                ReDim mParentIonStats(-1)
            End If

            ' For each parent ion, find the other nearby parent ions with similar m/z values
            ' Use the tolerance specified by sngSimilarIonMZToleranceHalfWidth, though with a minimum value of 0.1
            If sngSimilarIonMZToleranceHalfWidth < 0.1 Then
                sngSimilarIonMZToleranceHalfWidth = 0.1
            End If
            FindSimilarParentIon(sngSimilarIonMZToleranceHalfWidth * 2)


            If eCurrentXMLDataFileSection = eCurrentXMLDataFileSectionConstants.UnknownFile Then
                MessageBox.Show("Root element 'SICData' not found in the input file: " & ControlChars.NewLine & strFilePath, "Invalid File Format", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Else

                ' Set the smoothing options
                If Not blnFindPeaksOnSmoothedData Then
                    optDoNotResmooth.Checked = True
                Else
                    If blnUseButterworthSmooth OrElse Not blnUseSavitzkyGolaySmooth Then
                        optUseButterworthSmooth.Checked = True
                        txtButterworthSamplingFrequency.Text = sngButterworthSamplingFrequency.ToString
                    Else
                        optUseSavitzkyGolaySmooth.Checked = True
                        txtSavitzkyGolayFilterOrder.Text = intSavitzkyGolayFilterOrder.ToString
                    End If
                End If

                If blnSmoothedDataFound Then
                    optDoNotResmooth.Text = "Do Not Resmooth"
                Else
                    optDoNotResmooth.Text = "Do Not Show Smoothed Data"
                End If

                ' Inform the user if any errors occurred
                If strErrorLog.Length > 0 Then
                    MessageBox.Show(strErrorLog, "Invalid Lines", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
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
                    AutoOpenMsMsResults(Path.GetFullPath(strFilePath), objProgress)
                End If

            End If

        Catch ex As Exception
            MessageBox.Show("Unable to read the input file: " & strFilePath & ControlChars.NewLine & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        Finally
            If Not objProgress Is Nothing Then
                objProgress.HideForm()
                objProgress = Nothing
            End If
        End Try

    End Sub

    Private Sub ReadMsMsSearchEngineResults(strFilePath As String, ByRef objProgress As ProgressFormNET.frmProgress)

        Dim ioStream As FileStream
        Dim chSepChars() As Char = New Char() {ControlChars.Tab}

        Dim strLineIn As String
        Dim strSplitLine() As String

        Dim intSequenceID As Integer
        Dim lngBytesRead As Long
        Dim intLinesRead As Integer

        Dim intScanNumber As Integer, intCharge As Integer

        Dim blnCreatedNewProgressForm As Boolean

        Dim objNewRow As DataRow

        Try
            ioStream = New FileStream(strFilePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)

            Using fsInFile As StreamReader = New StreamReader(ioStream)

                If objProgress Is Nothing Then
                    objProgress = New ProgressFormNET.frmProgress
                    blnCreatedNewProgressForm = True
                End If

                objProgress.InitializeProgressForm("Reading MS/MS Search Engine Results ", 0, ioStream.Length, True)
                objProgress.Show()
                objProgress.BringToFront()
                Application.DoEvents()

                With mMsMsResults
                    If .Tables(TABLE_NAME_MSMSRESULTS).Rows.Count > 0 Or .Tables(TABLE_NAME_SEQ_TO_PROTEIN_MAP).Rows.Count > 0 Or .Tables(TABLE_NAME_SEQUENCES).Rows.Count > 0 Then
                        ClearMsMsResults()
                    End If
                End With

                intLinesRead = 0
                Do While fsInFile.Peek >= 0
                    strLineIn = fsInFile.ReadLine
                    intLinesRead += 1

                    If Not strLineIn Is Nothing Then
                        lngBytesRead += strLineIn.Length + 2

                        If intLinesRead Mod 50 = 0 Then
                            objProgress.UpdateProgressBar(lngBytesRead)
                            Application.DoEvents()
                            If objProgress.KeyPressAbortProcess Then Exit Do
                        End If

                        strSplitLine = strLineIn.Trim.Split(chSepChars)

                        If strSplitLine.Length >= 13 Then
                            intSequenceID = LookupSequenceID(strSplitLine(eMsMsSearchEngineResultColumns.Sequence), strSplitLine(eMsMsSearchEngineResultColumns.Protein))

                            If intSequenceID >= 0 Then
                                Try
                                    intScanNumber = Integer.Parse(strSplitLine(eMsMsSearchEngineResultColumns.Scan))
                                    intCharge = Integer.Parse(strSplitLine(eMsMsSearchEngineResultColumns.Charge))

                                    With mMsMsResults.Tables(TABLE_NAME_MSMSRESULTS)
                                        If Not .Rows.Contains(New Object() {intScanNumber, intCharge, intSequenceID}) Then
                                            objNewRow = mMsMsResults.Tables(TABLE_NAME_MSMSRESULTS).NewRow
                                            objNewRow.Item(COL_NAME_SCAN) = intScanNumber
                                            objNewRow.Item(COL_NAME_CHARGE) = intCharge
                                            objNewRow.Item(COL_NAME_MH) = strSplitLine(eMsMsSearchEngineResultColumns.MH)
                                            objNewRow.Item(COL_NAME_XCORR) = strSplitLine(eMsMsSearchEngineResultColumns.XCorr)
                                            objNewRow.Item(COL_NAME_DELTACN) = strSplitLine(eMsMsSearchEngineResultColumns.DeltaCN)
                                            objNewRow.Item(COL_NAME_DELTACN2) = strSplitLine(eMsMsSearchEngineResultColumns.DeltaCn2)
                                            objNewRow.Item(COL_NAME_RANKSP) = strSplitLine(eMsMsSearchEngineResultColumns.RankSp)
                                            objNewRow.Item(COL_NAME_RANKXC) = strSplitLine(eMsMsSearchEngineResultColumns.RankXc)
                                            objNewRow.Item(COL_NAME_SEQUENCEID) = intSequenceID
                                            objNewRow.Item(COL_NAME_PARENTIONINDEX) = -1

                                            mMsMsResults.Tables(TABLE_NAME_MSMSRESULTS).Rows.Add(objNewRow)
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

            ' Populate column .Item(COL_NAME_PARENTIONINDEX)
            PopulateParentIonIndexColumnInMsMsResultsTable()

            txtStats3.Visible = True
            PositionControls()
        Catch ex As Exception
            MessageBox.Show("Error in ReadMsMsSearchEngineResults: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        Finally

            If blnCreatedNewProgressForm Then
                objProgress.HideForm()
                objProgress = Nothing
            End If

        End Try

    End Sub

    Private Sub RedoSICPeakFindingAllData()

        Const SMOOTH_MODE As eSmoothModeConstants = eSmoothModeConstants.Butterworth

        Dim intParentIonIndex As Integer
        Dim blnValidPeakFound As Boolean

        Dim udtSICStats As udtSICStatsType = New udtSICStatsType

        Dim objProgress As New ProgressFormNET.frmProgress

        Try
            cmdRedoSICPeakFindingAllData.Enabled = False

            objProgress.InitializeProgressForm("Repeating SIC peak finding ", 0, mParentIonCount, True)
            objProgress.Show()
            Application.DoEvents()

            UpdateSICPeakFinderOptions()

            For intParentIonIndex = 0 To mParentIonCount - 1
                blnValidPeakFound = UpdateSICStats(intParentIonIndex, True, SMOOTH_MODE, udtSICStats)

                If blnValidPeakFound Then
                    mParentIonStats(intParentIonIndex).SICStats = udtSICStats
                End If

                objProgress.UpdateProgressBar(intParentIonIndex + 1)
                Application.DoEvents()
                If objProgress.KeyPressAbortProcess Then Exit For

            Next intParentIonIndex

            SortData()

        Catch ex As Exception
            MessageBox.Show("Error in RedoSICPeakFindingAllData: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        Finally
            If Not objProgress Is Nothing Then
                objProgress.HideForm()
                objProgress = Nothing
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
            Me.Height = 648

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
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "WindowSizeWidth", Me.Width.ToString)
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "WindowSizeHeight", Me.Height.ToString)
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "WindowPosTop", Me.Top.ToString)
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "WindowPosLeft", Me.Left.ToString)

                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "SortOrder", cboSortOrder.SelectedIndex.ToString)
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "SortDescending", chkSortDescending.Checked.ToString)

                If SharedVBNetRoutines.VBNetRoutines.IsNumber(txtFixXRange.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FixXRange", CInt(txtFixXRange.Text).ToString)
                End If
                If SharedVBNetRoutines.VBNetRoutines.IsNumber(txtFixYRange.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FixYRange", CInt(txtFixYRange.Text).ToString)
                End If
                If SharedVBNetRoutines.VBNetRoutines.IsNumber(txtMinimumSignalToNoise.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "MinimumSignalToNoise", CSng(txtMinimumSignalToNoise.Text).ToString)
                End If
                If SharedVBNetRoutines.VBNetRoutines.IsNumber(txtMinimumIntensity.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "MinimumIntensity", CInt(txtMinimumIntensity.Text).ToString)
                End If
                If SharedVBNetRoutines.VBNetRoutines.IsNumber(txtFilterByMZ.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FilterByMZ", CSng(txtFilterByMZ.Text).ToString)
                End If
                If SharedVBNetRoutines.VBNetRoutines.IsNumber(txtFilterByMZTol.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FilterByMZTol", CSng(txtFilterByMZTol.Text).ToString)
                End If
                If SharedVBNetRoutines.VBNetRoutines.IsNumber(txtAutoStep.Text) Then
                    SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "AutoStepInterval", CInt(txtAutoStep.Text).ToString)
                End If

                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FixXRangeEnabled", chkFixXRange.Checked.ToString)
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FixYRangeEnabled", chkFixYRange.Checked.ToString)
                SaveSetting(REG_APP_NAME, REG_SECTION_NAME, "FilterBySignalToNoise", chkFilterBySignalToNoise.Checked.ToString)

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

        objToolTipControl = Nothing

    End Sub

    Private Sub ShowAboutBox()
        Dim strMessage As String

        strMessage = String.Empty

        strMessage &= "The MASIC Browser can be used to visualize the SIC's created using MASIC." & ControlChars.NewLine & ControlChars.NewLine

        strMessage &= "Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2003" & ControlChars.NewLine
        strMessage &= "Copyright 2005, Battelle Memorial Institute.  All Rights Reserved." & ControlChars.NewLine & ControlChars.NewLine

        strMessage &= "This is version " & Application.ProductVersion & " (" & PROGRAM_DATE & ")" & ControlChars.NewLine & ControlChars.NewLine

        strMessage &= "E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com" & ControlChars.NewLine
        strMessage &= "Website: http://panomics.pnnl.gov/ or http://www.sysbio.org/resources/staff/" & ControlChars.NewLine & ControlChars.NewLine

        strMessage &= "Licensed under the Apache License, Version 2.0; you may not use this file except in compliance with the License.  "
        strMessage &= "You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0" & ControlChars.NewLine & ControlChars.NewLine

        strMessage &= "Notice: This computer software was prepared by Battelle Memorial Institute, "
        strMessage &= "hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the "
        strMessage &= "Department of Energy (DOE).  All rights in the computer software are reserved "
        strMessage &= "by DOE on behalf of the United States Government and the Contractor as "
        strMessage &= "provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY "
        strMessage &= "WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS "
        strMessage &= "SOFTWARE.  This notice including this sentence must appear on any copies of "
        strMessage &= "this computer software." & ControlChars.NewLine

        MessageBox.Show(strMessage, "About", MessageBoxButtons.OK, MessageBoxIcon.Information)

    End Sub

    Private Sub SortData()

        Dim intIndex As Integer
        Dim dblSortKeys() As Double
        Dim eSortMode As eSortOrderConstants

        Dim intScanNumberSaved As Integer

        Dim sngMinimumIntensity As Single
        Dim sngMZFilter, sngMZFilterTol As Single
        Dim sngMinimumSN As Single

        If mParentIonCount <= 0 Then
            ReDim mParentIonPointerArray(-1)
            ReDim dblSortKeys(-1)

            Exit Sub
        End If

        Try
            If lstParentIonData.SelectedIndex >= 0 Then
                intScanNumberSaved = mParentIonStats(mParentIonPointerArray(lstParentIonData.SelectedIndex)).FragScanObserved
            End If
        Catch ex As Exception
            intScanNumberSaved = 1
        End Try

        mParentIonPointerArrayCount = 0
        ReDim mParentIonPointerArray(mParentIonCount - 1)
        ReDim dblSortKeys(mParentIonCount - 1)

        If cboSortOrder.SelectedIndex >= 0 And cboSortOrder.SelectedIndex < SORT_ORDER_MODE_COUNT Then
            eSortMode = CType(cboSortOrder.SelectedIndex, eSortOrderConstants)
        Else
            eSortMode = eSortOrderConstants.SortByScanPeakCenter
        End If

        If chkFilterByIntensity.Checked AndAlso SharedVBNetRoutines.VBNetRoutines.IsNumber(txtMinimumIntensity.Text) Then
            sngMinimumIntensity = CSng(txtMinimumIntensity.Text)
        Else
            sngMinimumIntensity = Single.MinValue
        End If

        If chkFilterByMZ.Checked AndAlso SharedVBNetRoutines.VBNetRoutines.IsNumber(txtFilterByMZ.Text) AndAlso
           SharedVBNetRoutines.VBNetRoutines.IsNumber(txtFilterByMZTol.Text) Then
            sngMZFilter = Math.Abs(CSng(txtFilterByMZ.Text))
            sngMZFilterTol = Math.Abs(CSng(txtFilterByMZTol.Text))
        Else
            sngMZFilter = -1
            sngMZFilterTol = Single.MaxValue
        End If

        If chkFilterBySignalToNoise.Checked AndAlso SharedVBNetRoutines.VBNetRoutines.IsNumber(txtMinimumSignalToNoise.Text) Then
            sngMinimumSN = CSng(txtMinimumSignalToNoise.Text)
        Else
            sngMinimumSN = Single.MinValue
        End If

        Select Case eSortMode
            Case eSortOrderConstants.SortByPeakIndex
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            dblSortKeys(mParentIonPointerArrayCount) = mParentIonStats(intIndex).Index
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByScanPeakCenter
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            dblSortKeys(mParentIonPointerArrayCount) = mParentIonStats(intIndex).SICStats.ScanNumberMaxIntensity
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByScanOptimalPeakCenter
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            dblSortKeys(mParentIonPointerArrayCount) = CDbl(mParentIonStats(intIndex).OptimalPeakApexScanNumber.ToString & "." & Math.Round(mParentIonStats(intIndex).MZ, 0).ToString("0000") & mParentIonStats(intIndex).Index.ToString("00000"))
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByMz
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            dblSortKeys(mParentIonPointerArrayCount) = CDbl(Math.Round(mParentIonStats(intIndex).MZ, 2).ToString & mParentIonStats(intIndex).SICStats.ScanNumberMaxIntensity.ToString("000000"))
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByPeakSignalToNoise
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            dblSortKeys(mParentIonPointerArrayCount) = mParentIonStats(intIndex).SICStats.Peak.SignalToNoiseRatio
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByBaselineCorrectedPeakIntensity
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            With mParentIonStats(intIndex).SICStats
                                dblSortKeys(mParentIonPointerArrayCount) = MASICPeakFinder.clsMASICPeakFinder.BaselineAdjustIntensity(.Peak, True)
                            End With
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByBaselineCorrectedPeakArea
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            With mParentIonStats(intIndex).SICStats
                                dblSortKeys(mParentIonPointerArrayCount) = MASICPeakFinder.clsMASICPeakFinder.BaselineAdjustArea(.Peak, .SICPeakWidthFullScans, True)
                            End With
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByPeakWidth
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            ' Create a sort key that is based on both PeakFWHMScans and ScanNumberMaxIntensity by separating the two integers with a "."
                            dblSortKeys(mParentIonPointerArrayCount) = CDbl(mParentIonStats(intIndex).SICStats.Peak.FWHMScanWidth.ToString & "." & mParentIonStats(intIndex).SICStats.ScanNumberMaxIntensity.ToString("000000"))
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortBySICIntensityMax
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            dblSortKeys(mParentIonPointerArrayCount) = Math.Round(mParentIonStats(intIndex).SICIntensityMax, 0)
                            ' Append the scan number so that we sort by intensity, then scan
                            If chkSortDescending.Checked Then
                                dblSortKeys(mParentIonPointerArrayCount) += (1 - mParentIonStats(intIndex).FragScanObserved / 1000000.0)
                            Else
                                dblSortKeys(mParentIonPointerArrayCount) += mParentIonStats(intIndex).FragScanObserved / 1000000.0
                            End If
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByPeakIntensity
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            dblSortKeys(mParentIonPointerArrayCount) = mParentIonStats(intIndex).SICStats.Peak.MaxIntensityValue
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByPeakArea
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            dblSortKeys(mParentIonPointerArrayCount) = mParentIonStats(intIndex).SICStats.Peak.Area
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByFragScanToOptimalLocDistance
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            ' Create a sort key that is based on both FragScan-OptimalPeakApexScanNumber and OptimalPeakApexScanNumber by separating the two integers with a "."
                            dblSortKeys(mParentIonPointerArrayCount) = CDbl((mParentIonStats(intIndex).FragScanObserved - mParentIonStats(intIndex).OptimalPeakApexScanNumber).ToString & "." & mParentIonStats(intIndex).OptimalPeakApexScanNumber.ToString("000000"))
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByPeakCenterToOptimalLocDistance
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            ' Create a sort key that is based on both ScanNumberMaxIntensity-OptimalPeakApexScanNumber and OptimalPeakApexScanNumber by separating the two integers with a "."
                            dblSortKeys(mParentIonPointerArrayCount) = CDbl((mParentIonStats(intIndex).SICStats.ScanNumberMaxIntensity - mParentIonStats(intIndex).OptimalPeakApexScanNumber).ToString & "." & mParentIonStats(intIndex).OptimalPeakApexScanNumber.ToString("000000"))
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByShoulderCount
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            ' Create a sort key that is based on both ShoulderCount and OptimalPeakApexScanNumber by separating the two integers with a "."
                            dblSortKeys(mParentIonPointerArrayCount) = CDbl(mParentIonStats(intIndex).SICStats.Peak.ShoulderCount.ToString & "." & mParentIonStats(intIndex).OptimalPeakApexScanNumber.ToString("000000"))
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByParentIonIntensity
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            dblSortKeys(mParentIonPointerArrayCount) = mParentIonStats(intIndex).SICStats.Peak.ParentIonIntensity
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByPeakSkew
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            dblSortKeys(mParentIonPointerArrayCount) = mParentIonStats(intIndex).SICStats.Peak.StatisticalMoments.Skew
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByKSStat
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            dblSortKeys(mParentIonPointerArrayCount) = mParentIonStats(intIndex).SICStats.Peak.StatisticalMoments.KSStat
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex
            Case eSortOrderConstants.SortByBaselineNoiseLevel
                For intIndex = 0 To mParentIonCount - 1
                    With mParentIonStats(intIndex)
                        If SortDataFilterCheck(.SICStats.Peak.MaxIntensityValue, .SICStats.Peak.SignalToNoiseRatio, .MZ, sngMinimumIntensity, sngMinimumSN, sngMZFilter, sngMZFilterTol, .CustomSICPeak) Then
                            mParentIonPointerArray(mParentIonPointerArrayCount) = intIndex
                            dblSortKeys(mParentIonPointerArrayCount) = CDbl(CInt(Math.Round(mParentIonStats(intIndex).SICStats.Peak.BaselineNoiseStats.NoiseLevel, 0)).ToString & "." & mParentIonStats(intIndex).SICStats.Peak.SignalToNoiseRatio.ToString("0000000"))
                            mParentIonPointerArrayCount += 1
                        End If
                    End With
                Next intIndex

        End Select

        If mParentIonPointerArrayCount < mParentIonCount Then
            ReDim Preserve dblSortKeys(mParentIonPointerArrayCount - 1)
            ReDim Preserve mParentIonPointerArray(mParentIonPointerArrayCount - 1)
        End If

        ' Sort mParentIonPointerArray
        Array.Sort(dblSortKeys, mParentIonPointerArray)

        If chkSortDescending.Checked Then
            Array.Reverse(mParentIonPointerArray)
        End If

        PopulateSpectrumList(intScanNumberSaved)

    End Sub

    Private Function SortDataFilterCheck(sngPeakMaxIntensityValue As Single, sngPeakSN As Single, dblPeakMZ As Double, sngMinimumIntensity As Single, sngMinimumSN As Single, sngMZFilter As Single, sngMZFilterTol As Single, blnIsCustomSIC As Boolean) As Boolean

        Dim blnUseData As Boolean

        If cboSICsTypeFilter.SelectedIndex = eSICTypeFilterConstants.CustomSICsOnly Then
            blnUseData = blnIsCustomSIC
        ElseIf cboSICsTypeFilter.SelectedIndex = eSICTypeFilterConstants.NoCustomSICs Then
            blnUseData = Not blnIsCustomSIC
        Else
            blnUseData = True
        End If

        If blnUseData AndAlso sngPeakMaxIntensityValue >= sngMinimumIntensity Then
            If sngPeakSN >= sngMinimumSN Then
                If sngMZFilter >= 0 Then
                    If Math.Abs(dblPeakMZ - sngMZFilter) <= sngMZFilterTol Then
                        blnUseData = True
                    Else
                        blnUseData = False
                    End If
                Else
                    blnUseData = True
                End If
            Else
                blnUseData = False
            End If
        Else
            blnUseData = False
        End If

        Return blnUseData

    End Function

    Private Sub TestValueToString()
        Dim strResults As String = String.Empty
        Dim intDigits As Byte

        For intDigits = 1 To 5
            strResults &= TestValueToStringWork(0.00001234, intDigits) & ControlChars.NewLine
            strResults &= TestValueToStringWork(0.01234, intDigits) & ControlChars.NewLine
            strResults &= TestValueToStringWork(0.1234, intDigits) & ControlChars.NewLine
            strResults &= TestValueToStringWork(0.123, intDigits) & ControlChars.NewLine
            strResults &= TestValueToStringWork(0.12, intDigits) & ControlChars.NewLine
            strResults &= TestValueToStringWork(1.234, intDigits) & ControlChars.NewLine
            strResults &= TestValueToStringWork(12.34, intDigits) & ControlChars.NewLine
            strResults &= TestValueToStringWork(123.4, intDigits) & ControlChars.NewLine
            strResults &= TestValueToStringWork(1234, intDigits) & ControlChars.NewLine
            strResults &= TestValueToStringWork(12340, intDigits) & ControlChars.NewLine
            strResults &= ControlChars.NewLine
        Next intDigits


        MsgBox(strResults)
    End Sub

    Private Function TestValueToStringWork(sngValue As Single, intDigitsOfPrecision As Byte) As String
        Return sngValue.ToString & ": " & StringUtilities.ValueToString(sngValue, intDigitsOfPrecision)
    End Function

    Private Sub ToggleAutoStep(Optional blnForceDisabled As Boolean = False)
        If mAutoStepEnabled Or blnForceDisabled Then
            mAutoStepEnabled = False
            cmdAutoStep.Text = "&Auto Step"
        Else
            If SharedVBNetRoutines.VBNetRoutines.IsNumber(txtAutoStep.Text) Then
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
                .SavitzkyGolayFilterOrder = CShort(SharedVBNetRoutines.VBNetRoutines.ParseTextboxValueInt(txtSavitzkyGolayFilterOrder, "", False, 0))
            Else
                .UseButterworthSmooth = True
                .UseSavitzkyGolaySmooth = False
                .ButterworthSamplingFrequency = SharedVBNetRoutines.VBNetRoutines.ParseTextboxValueSng(txtButterworthSamplingFrequency, "", False, 0.25)
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

    Private Function UpdateSICStats(intParentIonIndex As Integer, blnRepeatPeakFinding As Boolean, eSmoothMode As eSmoothModeConstants, ByRef udtSICStats As udtSICStatsType) As Boolean
        ' Copy the original SIC stats found by MASIC into udtSICStats
        ' This also includes the original smoothed data

        Dim objFilter As DataFilter.clsDataFilter

        Dim intIndex As Integer
        Dim dblSmoothedYData() As Double

        Dim blnValidPeakFound As Boolean

        Dim sngSamplingFrequency As Single

        Dim intSavitzkyGolayFilterOrder As Integer
        Dim intPeakWidthsPointsMinimum As Integer
        Dim intFilterThirdWidth As Integer

        ' Copy the cached SICStats data into udtSICStats
        ' We have to separately copy SICSmoothedYData() otherwise VB.NET keeps
        '  the array linked in both mParentIonStats().SICStats and udtSICStats
        With mParentIonStats(intParentIonIndex)
            udtSICStats = .SICStats
            With .SICStats
                ReDim udtSICStats.SICSmoothedYData(.SICSmoothedYData.Length - 1)
                .SICSmoothedYData.CopyTo(udtSICStats.SICSmoothedYData, 0)
            End With
        End With

        If eSmoothMode <> eSmoothModeConstants.DoNotReSmooth Then
            ' Re-smooth the data
            objFilter = New DataFilter.clsDataFilter

            With mParentIonStats(intParentIonIndex)
                udtSICStats.SICSmoothedYDataIndexStart = 0
                ReDim dblSmoothedYData(.SICData.Length - 1)

                .SICData.CopyTo(dblSmoothedYData, 0)

                If eSmoothMode = eSmoothModeConstants.SavitzkyGolay Then
                    ' Resmooth using a Savitzy Golay filter

                    intSavitzkyGolayFilterOrder = SharedVBNetRoutines.VBNetRoutines.ParseTextboxValueInt(txtSavitzkyGolayFilterOrder, lblSavitzkyGolayFilterOrder.Text & " should be an even number between 0 and 20; assuming 0", False, 0)
                    intPeakWidthsPointsMinimum = SharedVBNetRoutines.VBNetRoutines.ParseTextboxValueInt(txtPeakWidthPointsMinimum, lblPeakWidthPointsMinimum.Text & " should be a positive integer; assuming 6", False, 6)

                    intFilterThirdWidth = CInt(Math.Floor(intPeakWidthsPointsMinimum / 3))
                    If intFilterThirdWidth > 3 Then intFilterThirdWidth = 3

                    ' Make sure intFilterThirdWidth is Odd
                    If intFilterThirdWidth Mod 2 = 0 Then
                        intFilterThirdWidth -= 1
                    End If

                    ' Note that the SavitzkyGolayFilter doesn't work right for PolynomialDegree values greater than 0
                    ' Also note that a PolynomialDegree value of 0 results in the equivalent of a moving average filter
                    objFilter.SavitzkyGolayFilter(dblSmoothedYData, 0, CInt(Math.Min(.SICData.Length, .DataCount)) - 1, intFilterThirdWidth, intFilterThirdWidth, CShort(intSavitzkyGolayFilterOrder), True)

                Else
                    ' Assume eSmoothMode = eSmoothModeConstants.Butterworth
                    sngSamplingFrequency = SharedVBNetRoutines.VBNetRoutines.ParseTextboxValueSng(txtButterworthSamplingFrequency, lblButterworthSamplingFrequency.Text & " should be a number between 0.01 and 0.99; assuming 0.2", False, 0.2)
                    objFilter.ButterworthFilter(dblSmoothedYData, 0, CInt(Math.Min(.SICData.Length, .DataCount)) - 1, sngSamplingFrequency)
                End If

                ' Copy the smoothed data into udtSICStats.SICSmoothedYData
                ReDim udtSICStats.SICSmoothedYData(dblSmoothedYData.Length - 1)
                For intIndex = 0 To dblSmoothedYData.Length - 1
                    udtSICStats.SICSmoothedYData(intIndex) = CSng(dblSmoothedYData(intIndex))
                Next intIndex
            End With
        End If

        If blnRepeatPeakFinding Then
            ' Repeat the finding of the peak in the SIC
            blnValidPeakFound = FindSICPeakAndAreaForParentIon(intParentIonIndex, udtSICStats)
        Else
            blnValidPeakFound = True
        End If

        Return blnValidPeakFound

    End Function

    Private Sub UpdateStatsAndPlot()
        Dim udtSICStats As udtSICStatsType = New udtSICStatsType
        If mParentIonPointerArrayCount > 0 Then
            DisplaySICStats(mParentIonPointerArray(lstParentIonData.SelectedIndex), udtSICStats)
            PlotData(mParentIonPointerArray(lstParentIonData.SelectedIndex), udtSICStats)
        End If
    End Sub

    Private Function XMLTextReaderGetInnerText(ByRef objXMLReader As System.Xml.XmlTextReader) As String
        Dim strValue As String = String.Empty
        Dim blnSuccess As Boolean

        If objXMLReader.NodeType = Xml.XmlNodeType.Element Then
            ' Advance the reader so that we can read the value
            blnSuccess = objXMLReader.Read()
        Else
            blnSuccess = True
        End If

        If blnSuccess AndAlso Not objXMLReader.NodeType = Xml.XmlNodeType.Whitespace And objXMLReader.HasValue Then
            strValue = objXMLReader.Value
        End If

        Return strValue
    End Function

    Private Sub XMLTextReaderSkipWhitespace(ByRef objXMLReader As System.Xml.XmlTextReader)
        If objXMLReader.NodeType = Xml.XmlNodeType.Whitespace Then
            ' Whitspace; read the next node
            objXMLReader.Read()
        End If
    End Sub

#Region "Checkboxes"
    Private Sub chkFilterByIntensity_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkFilterByIntensity.CheckedChanged
        SortData()
    End Sub

    Private Sub chkFilterByMZ_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkFilterByMZ.CheckedChanged
        SortData()
    End Sub

    Private Sub chkFilterBySignalToNoise_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkFilterBySignalToNoise.CheckedChanged
        SortData()
    End Sub

    Private Sub chkFixXRange_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkFixXRange.CheckedChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub chkFixYRange_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkFixYRange.CheckedChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub cmdRedoSICPeakFindingAllData_Click(sender As System.Object, e As System.EventArgs) Handles cmdRedoSICPeakFindingAllData.Click
        RedoSICPeakFindingAllData()
    End Sub

    Private Sub chkShowBaselineCorrectedStats_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkShowBaselineCorrectedStats.CheckedChanged
        DisplaySICStatsForSelectedParentIon()
    End Sub

    Private Sub chkSortDescending_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkSortDescending.CheckedChanged
        SortData()
    End Sub

    Private Sub chkShowSmoothedData_CheckedChanged(sender As System.Object, e As System.EventArgs)
        UpdateStatsAndPlot()
    End Sub

    Private Sub chkUsePeakFinder_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkUsePeakFinder.CheckedChanged
        UpdateStatsAndPlot()
    End Sub
#End Region

#Region "Command Buttons"
    Private Sub cmdAutoStep_Click(sender As System.Object, e As System.EventArgs) Handles cmdAutoStep.Click
        ToggleAutoStep()
    End Sub

    Private Sub cmdNext_Click(sender As System.Object, e As System.EventArgs) Handles cmdNext.Click
        NavigateScanList(True)
    End Sub

    Private Sub cmdPrevious_Click(sender As System.Object, e As System.EventArgs) Handles cmdPrevious.Click
        NavigateScanList(False)
    End Sub

    Private Sub cmdSelectFile_Click(sender As System.Object, e As System.EventArgs) Handles cmdSelectFile.Click
        SelectMASICInputFile()
    End Sub

    Private Sub cmdJump_Click(sender As System.Object, e As System.EventArgs) Handles cmdJump.Click
        JumpToScan()
    End Sub
#End Region

#Region "ListBoxes and Comboboxes"
    Private Sub lstParentIonData_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles lstParentIonData.SelectedIndexChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub cboSICsTypeFilter_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles cboSICsTypeFilter.SelectedIndexChanged
        SortData()
    End Sub

    Private Sub cboSortOrder_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles cboSortOrder.SelectedIndexChanged
        DefineDefaultSortDirection()
        SortData()
        lstParentIonData.Focus()
    End Sub

#End Region

#Region "Option buttons"
    Private Sub optDoNotResmooth_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles optDoNotResmooth.CheckedChanged
        EnableDisableControls()
        UpdateStatsAndPlot()
    End Sub

    Private Sub optUseButterworthSmooth_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles optUseButterworthSmooth.CheckedChanged
        EnableDisableControls()
        UpdateStatsAndPlot()
    End Sub

    Private Sub optUseSavitzkyGolaySmooth_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles optUseSavitzkyGolaySmooth.CheckedChanged
        EnableDisableControls()
        UpdateStatsAndPlot()
    End Sub
#End Region

#Region "Textboxes"

    Private Sub txtAutoStep_TextChanged(sender As System.Object, e As System.EventArgs) Handles txtAutoStep.TextChanged
        Dim intNewInterval As Integer

        If SharedVBNetRoutines.VBNetRoutines.IsNumber(txtAutoStep.Text) Then
            intNewInterval = CInt(txtAutoStep.Text)
            If intNewInterval < 10 Then intNewInterval = 10
            mAutoStepIntervalMsec = intNewInterval
        End If

    End Sub

    Private Sub txtAutoStep_Validating(sender As System.Object, e As System.ComponentModel.CancelEventArgs) Handles txtAutoStep.Validating
        SharedVBNetRoutines.VBNetRoutines.ValidateTextboxInt(txtAutoStep, 10, 9999, 150)
    End Sub

    Private Sub txtButterworthSamplingFrequency_TextChanged(sender As System.Object, e As System.EventArgs) Handles txtButterworthSamplingFrequency.TextChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub txtButterworthSamplingFrequency_Validating(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles txtButterworthSamplingFrequency.Validating
        SharedVBNetRoutines.VBNetRoutines.ValidateTextboxSng(txtButterworthSamplingFrequency, 0.01, 0.99, 0.2)
    End Sub

    Private Sub txtFilterByMZ_Leave(sender As Object, e As System.EventArgs) Handles txtFilterByMZ.Leave
        If chkFilterByMZ.Checked Then SortData()
    End Sub

    Private Sub txtFilterByMZ_Validating(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles txtFilterByMZ.Validating
        SharedVBNetRoutines.VBNetRoutines.ValidateTextboxInt(txtFilterByMZ, 0, 100000, 540)
    End Sub

    Private Sub txtFilterByMZTol_Leave(sender As Object, e As System.EventArgs) Handles txtFilterByMZTol.Leave
        If chkFilterByMZ.Checked Then SortData()
    End Sub

    Private Sub txtFilterByMZTol_Validating(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles txtFilterByMZTol.Validating
        SharedVBNetRoutines.VBNetRoutines.ValidateTextboxSng(txtFilterByMZTol, 0, 100000, 0.2)
    End Sub

    Private Sub txtFixXRange_TextChanged(sender As System.Object, e As System.EventArgs) Handles txtFixXRange.TextChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub txtFixXRange_Validating(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles txtFixXRange.Validating
        SharedVBNetRoutines.VBNetRoutines.ValidateTextboxInt(txtFixXRange, 3, 500000, 100)
    End Sub

    Private Sub txtFixYRange_TextChanged(sender As System.Object, e As System.EventArgs) Handles txtFixYRange.TextChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub txtFixYRange_Validating(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles txtFixYRange.Validating
        SharedVBNetRoutines.VBNetRoutines.ValidateTextboxInt(txtFixYRange, 10, Integer.MaxValue, 5000000)
    End Sub

    Private Sub txtMinimumIntensity_Leave(sender As Object, e As System.EventArgs) Handles txtMinimumIntensity.Leave
        If chkFilterByIntensity.Checked Then SortData()
    End Sub

    Private Sub txtMinimumIntensity_Validating(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles txtMinimumIntensity.Validating
        SharedVBNetRoutines.VBNetRoutines.ValidateTextboxInt(txtMinimumIntensity, 0, 1000000000, 1000000)
    End Sub

    Private Sub txtMinimumSignalToNoise_Leave(sender As Object, e As System.EventArgs) Handles txtMinimumSignalToNoise.Leave
        If chkFilterBySignalToNoise.Checked Then SortData()
    End Sub

    Private Sub txtMinimumSignalToNoise_Validating(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles txtMinimumSignalToNoise.Validating
        SharedVBNetRoutines.VBNetRoutines.ValidateTextboxInt(txtMinimumSignalToNoise, 0, 10000, 2)
    End Sub

    Private Sub txtPeakWidthPointsMinimum_TextChanged(sender As System.Object, e As System.EventArgs) Handles txtPeakWidthPointsMinimum.TextChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub txtPeakWidthPointsMinimum_Validating(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles txtPeakWidthPointsMinimum.Validating
        SharedVBNetRoutines.VBNetRoutines.ValidateTextboxInt(txtPeakWidthPointsMinimum, 2, 100000, 6)
    End Sub

    Private Sub txtSavitzkyGolayFilterOrder_TextChanged(sender As System.Object, e As System.EventArgs) Handles txtSavitzkyGolayFilterOrder.TextChanged
        UpdateStatsAndPlot()
    End Sub

    Private Sub txtSavitzkyGolayFilterOrder_Validating(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles txtSavitzkyGolayFilterOrder.Validating
        SharedVBNetRoutines.VBNetRoutines.ValidateTextboxInt(txtSavitzkyGolayFilterOrder, 0, 20, 0)
    End Sub

    Private Sub txtStats1_KeyPress(sender As System.Object, e As KeyPressEventArgs) Handles txtStats1.KeyPress
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

    Private Sub txtStats2_KeyPress(sender As System.Object, e As KeyPressEventArgs) Handles txtStats2.KeyPress
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

    Private Sub txStats3_KeyPress(sender As System.Object, e As KeyPressEventArgs) Handles txtStats3.KeyPress
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
    Private Sub mnuHelpAbout_Click(sender As System.Object, e As System.EventArgs) Handles mnuHelpAbout.Click
        ShowAboutBox()
    End Sub

    Private Sub mnuEditShowOptimalPeakApexCursor_Click(sender As System.Object, e As System.EventArgs) Handles mnuEditShowOptimalPeakApexCursor.Click
        mnuEditShowOptimalPeakApexCursor.Checked = Not mnuEditShowOptimalPeakApexCursor.Checked
    End Sub

    Private Sub mnuFileSelectMASICInputFile_Click(sender As System.Object, e As System.EventArgs) Handles mnuFileSelectMASICInputFile.Click
        SelectMASICInputFile()
    End Sub

    Private Sub mnuFileSelectMSMSSearchResultsFile_Click(sender As System.Object, e As System.EventArgs) Handles mnuFileSelectMSMSSearchResultsFile.Click
        SelectMsMsSearchResultsInputFile()
    End Sub

    Private Sub mnuFileExit_Click(sender As System.Object, e As System.EventArgs) Handles mnuFileExit.Click
        Me.Close()
    End Sub
#End Region

    Private Sub frmBrowser_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
        RegSaveSettings()
    End Sub

    Private Sub frmBrowser_Load(sender As Object, e As System.EventArgs) Handles MyBase.Load
        ' Note that InitializeControls() is called in Sub New()
    End Sub

    Private Sub tmrAutoStep_Elapsed(sender As Object, e As System.Timers.ElapsedEventArgs) Handles tmrAutoStep.Elapsed
        If mAutoStepEnabled Then CheckAutoStep()
    End Sub

    Private Sub frmBrowser_Resize(sender As Object, e As System.EventArgs) Handles MyBase.Resize
        PositionControls()
    End Sub

    Private Sub mFileLoadTimer_Tick(sender As Object, e As EventArgs)
        If Not String.IsNullOrWhiteSpace(FileToAutoLoad) Then
            mFileLoadTimer.Enabled = False

            txtDataFilePath.Text = FileToAutoLoad
            ReadDataFileXMLTextReader(txtDataFilePath.Text)
        End If
    End Sub

End Class
