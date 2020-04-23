using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows.Forms;

namespace MASICBrowser
{
    partial class frmBrowser
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
            this.lblParentIon = new System.Windows.Forms.Label();
            this.txtDataFilePath = new System.Windows.Forms.TextBox();
            this.cmdSelectFile = new System.Windows.Forms.Button();
            this.lblSortOrder = new System.Windows.Forms.Label();
            this.cboSortOrder = new System.Windows.Forms.ComboBox();
            this.chkFixXRange = new System.Windows.Forms.CheckBox();
            this.txtFixXRange = new System.Windows.Forms.TextBox();
            this.lblFixXRange = new System.Windows.Forms.Label();
            this.lblMinimumIntensity = new System.Windows.Forms.Label();
            this.txtMinimumIntensity = new System.Windows.Forms.TextBox();
            this.chkFilterByIntensity = new System.Windows.Forms.CheckBox();
            this.tmrAutoStep = new System.Timers.Timer();
            this.fraNavigation = new System.Windows.Forms.GroupBox();
            this.chkAutoStepForward = new System.Windows.Forms.CheckBox();
            this.txtAutoStep = new System.Windows.Forms.TextBox();
            this.lblAutoStep = new System.Windows.Forms.Label();
            this.cmdAutoStep = new System.Windows.Forms.Button();
            this.cmdPrevious = new System.Windows.Forms.Button();
            this.cmdNext = new System.Windows.Forms.Button();
            this.cmdJump = new System.Windows.Forms.Button();
            this.txtFilterByMZ = new System.Windows.Forms.TextBox();
            this.lblFilterByMZ = new System.Windows.Forms.Label();
            this.chkFilterByMZ = new System.Windows.Forms.CheckBox();
            this.txtFilterByMZTol = new System.Windows.Forms.TextBox();
            this.lblFilterByMZTolUnits = new System.Windows.Forms.Label();
            this.lblFilterByMZTol = new System.Windows.Forms.Label();
            this.txtFixYRange = new System.Windows.Forms.TextBox();
            this.lblFixYRange = new System.Windows.Forms.Label();
            this.chkFixYRange = new System.Windows.Forms.CheckBox();
            this.lblSICsTypeFilter = new System.Windows.Forms.Label();
            this.cboSICsTypeFilter = new System.Windows.Forms.ComboBox();
            this.txtStats1 = new System.Windows.Forms.TextBox();
            this.MainMenuControl = new System.Windows.Forms.MainMenu(this.components);
            this.mnuFile = new System.Windows.Forms.MenuItem();
            this.mnuFileSelectMASICInputFile = new System.Windows.Forms.MenuItem();
            this.mnuFileSelectMSMSSearchResultsFile = new System.Windows.Forms.MenuItem();
            this.mnuFileSep1 = new System.Windows.Forms.MenuItem();
            this.mnuFileExit = new System.Windows.Forms.MenuItem();
            this.mnuEdit = new System.Windows.Forms.MenuItem();
            this.mnuEditShowOptimalPeakApexCursor = new System.Windows.Forms.MenuItem();
            this.mnuHelp = new System.Windows.Forms.MenuItem();
            this.mnuHelpAbout = new System.Windows.Forms.MenuItem();
            this.chkSortDescending = new System.Windows.Forms.CheckBox();
            this.lstParentIonData = new System.Windows.Forms.ListBox();
            this.txtMinimumSignalToNoise = new System.Windows.Forms.TextBox();
            this.chkFilterBySignalToNoise = new System.Windows.Forms.CheckBox();
            this.fraResmoothingOptions = new System.Windows.Forms.GroupBox();
            this.chkShowSmoothedData = new System.Windows.Forms.CheckBox();
            this.txtPeakWidthPointsMinimum = new System.Windows.Forms.TextBox();
            this.lblPeakWidthPointsMinimum = new System.Windows.Forms.Label();
            this.optDoNotResmooth = new System.Windows.Forms.RadioButton();
            this.optUseSavitzkyGolaySmooth = new System.Windows.Forms.RadioButton();
            this.txtButterworthSamplingFrequency = new System.Windows.Forms.TextBox();
            this.lblButterworthSamplingFrequency = new System.Windows.Forms.Label();
            this.txtSavitzkyGolayFilterOrder = new System.Windows.Forms.TextBox();
            this.lblSavitzkyGolayFilterOrder = new System.Windows.Forms.Label();
            this.optUseButterworthSmooth = new System.Windows.Forms.RadioButton();
            this.fraPeakFinder = new System.Windows.Forms.GroupBox();
            this.cmdRedoSICPeakFindingAllData = new System.Windows.Forms.Button();
            this.chkUsePeakFinder = new System.Windows.Forms.CheckBox();
            this.chkFindPeaksSubtractBaseline = new System.Windows.Forms.CheckBox();
            this.fraSortOrderAndStats = new System.Windows.Forms.GroupBox();
            this.chkShowBaselineCorrectedStats = new System.Windows.Forms.CheckBox();
            this.txtStats2 = new System.Windows.Forms.TextBox();
            this.txtStats3 = new System.Windows.Forms.TextBox();
            this.TabControl1 = new System.Windows.Forms.TabControl();
            this.tpSICFilters = new System.Windows.Forms.TabPage();
            this.tpMsMsSearchResultsFilters = new System.Windows.Forms.TabPage();
            this.chkSequenceFilterExactMatch = new System.Windows.Forms.CheckBox();
            this.lblSequenceFilter = new System.Windows.Forms.Label();
            this.lblChargeFilter = new System.Windows.Forms.Label();
            this.TextBox2 = new System.Windows.Forms.TextBox();
            this.lblMinimumXCorr = new System.Windows.Forms.Label();
            this.txtSequenceFilter = new System.Windows.Forms.TextBox();
            this.txtMinimumXCorr = new System.Windows.Forms.TextBox();
            this.pnlInputFile = new System.Windows.Forms.Panel();
            this.pnlSICs = new System.Windows.Forms.Panel();
            this.pnlNavigationAndOptions = new System.Windows.Forms.Panel();
            this.pnlBottom = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.tmrAutoStep)).BeginInit();
            this.fraNavigation.SuspendLayout();
            this.fraResmoothingOptions.SuspendLayout();
            this.fraPeakFinder.SuspendLayout();
            this.fraSortOrderAndStats.SuspendLayout();
            this.TabControl1.SuspendLayout();
            this.tpSICFilters.SuspendLayout();
            this.tpMsMsSearchResultsFilters.SuspendLayout();
            this.pnlInputFile.SuspendLayout();
            this.pnlSICs.SuspendLayout();
            this.pnlNavigationAndOptions.SuspendLayout();
            this.pnlBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblParentIon
            // 
            this.lblParentIon.Location = new System.Drawing.Point(8, 8);
            this.lblParentIon.Name = "lblParentIon";
            this.lblParentIon.Size = new System.Drawing.Size(152, 16);
            this.lblParentIon.TabIndex = 2;
            this.lblParentIon.Text = "Parent Ion SIC to View";
            // 
            // txtDataFilePath
            // 
            this.txtDataFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDataFilePath.Location = new System.Drawing.Point(96, 10);
            this.txtDataFilePath.Name = "txtDataFilePath";
            this.txtDataFilePath.Size = new System.Drawing.Size(465, 20);
            this.txtDataFilePath.TabIndex = 1;
            this.txtDataFilePath.Text = "D:\\";
            // 
            // cmdSelectFile
            // 
            this.cmdSelectFile.Location = new System.Drawing.Point(8, 8);
            this.cmdSelectFile.Name = "cmdSelectFile";
            this.cmdSelectFile.Size = new System.Drawing.Size(80, 24);
            this.cmdSelectFile.TabIndex = 0;
            this.cmdSelectFile.Text = "&Select File";
            this.cmdSelectFile.Click += new System.EventHandler(this.cmdSelectFile_Click);
            // 
            // lblSortOrder
            // 
            this.lblSortOrder.Location = new System.Drawing.Point(8, 8);
            this.lblSortOrder.Name = "lblSortOrder";
            this.lblSortOrder.Size = new System.Drawing.Size(88, 16);
            this.lblSortOrder.TabIndex = 0;
            this.lblSortOrder.Text = "Sort Order";
            // 
            // cboSortOrder
            // 
            this.cboSortOrder.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSortOrder.Location = new System.Drawing.Point(8, 28);
            this.cboSortOrder.Name = "cboSortOrder";
            this.cboSortOrder.Size = new System.Drawing.Size(264, 21);
            this.cboSortOrder.TabIndex = 1;
            this.cboSortOrder.SelectedIndexChanged += new System.EventHandler(this.cboSortOrder_SelectedIndexChanged);
            // 
            // chkFixXRange
            // 
            this.chkFixXRange.Checked = true;
            this.chkFixXRange.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFixXRange.Location = new System.Drawing.Point(8, 64);
            this.chkFixXRange.Name = "chkFixXRange";
            this.chkFixXRange.Size = new System.Drawing.Size(88, 16);
            this.chkFixXRange.TabIndex = 2;
            this.chkFixXRange.Text = "Fix X Range";
            this.chkFixXRange.CheckedChanged += new System.EventHandler(this.chkFixXRange_CheckedChanged);
            // 
            // txtFixXRange
            // 
            this.txtFixXRange.Location = new System.Drawing.Point(128, 64);
            this.txtFixXRange.Name = "txtFixXRange";
            this.txtFixXRange.Size = new System.Drawing.Size(72, 20);
            this.txtFixXRange.TabIndex = 3;
            this.txtFixXRange.Text = "300";
            this.txtFixXRange.TextChanged += new System.EventHandler(this.txtFixXRange_TextChanged);
            this.txtFixXRange.Validating += new System.ComponentModel.CancelEventHandler(this.txtFixXRange_Validating);
            // 
            // lblFixXRange
            // 
            this.lblFixXRange.Location = new System.Drawing.Point(200, 68);
            this.lblFixXRange.Name = "lblFixXRange";
            this.lblFixXRange.Size = new System.Drawing.Size(46, 16);
            this.lblFixXRange.TabIndex = 4;
            this.lblFixXRange.Text = "scans";
            // 
            // lblMinimumIntensity
            // 
            this.lblMinimumIntensity.Location = new System.Drawing.Point(200, 140);
            this.lblMinimumIntensity.Name = "lblMinimumIntensity";
            this.lblMinimumIntensity.Size = new System.Drawing.Size(46, 16);
            this.lblMinimumIntensity.TabIndex = 12;
            this.lblMinimumIntensity.Text = "counts";
            // 
            // txtMinimumIntensity
            // 
            this.txtMinimumIntensity.Location = new System.Drawing.Point(144, 136);
            this.txtMinimumIntensity.Name = "txtMinimumIntensity";
            this.txtMinimumIntensity.Size = new System.Drawing.Size(56, 20);
            this.txtMinimumIntensity.TabIndex = 11;
            this.txtMinimumIntensity.Text = "1000000";
            this.txtMinimumIntensity.Leave += new System.EventHandler(this.txtMinimumIntensity_Leave);
            this.txtMinimumIntensity.Validating += new System.ComponentModel.CancelEventHandler(this.txtMinimumIntensity_Validating);
            // 
            // chkFilterByIntensity
            // 
            this.chkFilterByIntensity.Location = new System.Drawing.Point(8, 136);
            this.chkFilterByIntensity.Name = "chkFilterByIntensity";
            this.chkFilterByIntensity.Size = new System.Drawing.Size(144, 16);
            this.chkFilterByIntensity.TabIndex = 10;
            this.chkFilterByIntensity.Text = "Minimum Intensity";
            this.chkFilterByIntensity.CheckedChanged += new System.EventHandler(this.chkFilterByIntensity_CheckedChanged);
            // 
            // tmrAutoStep
            // 
            this.tmrAutoStep.Enabled = true;
            this.tmrAutoStep.Interval = 10D;
            this.tmrAutoStep.SynchronizingObject = this;
            this.tmrAutoStep.Elapsed += new System.Timers.ElapsedEventHandler(this.tmrAutoStep_Elapsed);
            // 
            // fraNavigation
            // 
            this.fraNavigation.Controls.Add(this.chkAutoStepForward);
            this.fraNavigation.Controls.Add(this.txtAutoStep);
            this.fraNavigation.Controls.Add(this.lblAutoStep);
            this.fraNavigation.Controls.Add(this.cmdAutoStep);
            this.fraNavigation.Controls.Add(this.cmdPrevious);
            this.fraNavigation.Controls.Add(this.cmdNext);
            this.fraNavigation.Controls.Add(this.cmdJump);
            this.fraNavigation.Enabled = false;
            this.fraNavigation.Location = new System.Drawing.Point(16, 24);
            this.fraNavigation.Name = "fraNavigation";
            this.fraNavigation.Size = new System.Drawing.Size(224, 128);
            this.fraNavigation.TabIndex = 4;
            this.fraNavigation.TabStop = false;
            this.fraNavigation.Text = "Navigation";
            // 
            // chkAutoStepForward
            // 
            this.chkAutoStepForward.Checked = true;
            this.chkAutoStepForward.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoStepForward.Location = new System.Drawing.Point(112, 104);
            this.chkAutoStepForward.Name = "chkAutoStepForward";
            this.chkAutoStepForward.Size = new System.Drawing.Size(104, 16);
            this.chkAutoStepForward.TabIndex = 6;
            this.chkAutoStepForward.Text = "Move forward";
            // 
            // txtAutoStep
            // 
            this.txtAutoStep.Location = new System.Drawing.Point(112, 80);
            this.txtAutoStep.Name = "txtAutoStep";
            this.txtAutoStep.Size = new System.Drawing.Size(32, 20);
            this.txtAutoStep.TabIndex = 4;
            this.txtAutoStep.Text = "150";
            this.txtAutoStep.TextChanged += new System.EventHandler(this.txtAutoStep_TextChanged);
            this.txtAutoStep.Validating += new System.ComponentModel.CancelEventHandler(this.txtAutoStep_Validating);
            // 
            // lblAutoStep
            // 
            this.lblAutoStep.Location = new System.Drawing.Point(152, 80);
            this.lblAutoStep.Name = "lblAutoStep";
            this.lblAutoStep.Size = new System.Drawing.Size(64, 16);
            this.lblAutoStep.TabIndex = 5;
            // 
            // cmdAutoStep
            // 
            this.cmdAutoStep.Location = new System.Drawing.Point(8, 80);
            this.cmdAutoStep.Name = "cmdAutoStep";
            this.cmdAutoStep.Size = new System.Drawing.Size(88, 24);
            this.cmdAutoStep.TabIndex = 2;
            this.cmdAutoStep.Text = "&Auto Step";
            this.cmdAutoStep.Click += new System.EventHandler(this.cmdAutoStep_Click);
            // 
            // cmdPrevious
            // 
            this.cmdPrevious.Location = new System.Drawing.Point(8, 24);
            this.cmdPrevious.Name = "cmdPrevious";
            this.cmdPrevious.Size = new System.Drawing.Size(88, 24);
            this.cmdPrevious.TabIndex = 0;
            this.cmdPrevious.Text = "&Previous";
            this.cmdPrevious.Click += new System.EventHandler(this.cmdPrevious_Click);
            // 
            // cmdNext
            // 
            this.cmdNext.Location = new System.Drawing.Point(8, 48);
            this.cmdNext.Name = "cmdNext";
            this.cmdNext.Size = new System.Drawing.Size(88, 24);
            this.cmdNext.TabIndex = 1;
            this.cmdNext.Text = "&Next";
            this.cmdNext.Click += new System.EventHandler(this.cmdNext_Click);
            // 
            // cmdJump
            // 
            this.cmdJump.Location = new System.Drawing.Point(112, 24);
            this.cmdJump.Name = "cmdJump";
            this.cmdJump.Size = new System.Drawing.Size(96, 24);
            this.cmdJump.TabIndex = 3;
            this.cmdJump.Text = "&Jump to Scan";
            this.cmdJump.Click += new System.EventHandler(this.cmdJump_Click);
            // 
            // txtFilterByMZ
            // 
            this.txtFilterByMZ.Location = new System.Drawing.Point(144, 160);
            this.txtFilterByMZ.Name = "txtFilterByMZ";
            this.txtFilterByMZ.Size = new System.Drawing.Size(56, 20);
            this.txtFilterByMZ.TabIndex = 14;
            this.txtFilterByMZ.Text = "543";
            this.txtFilterByMZ.Leave += new System.EventHandler(this.txtFilterByMZ_Leave);
            this.txtFilterByMZ.Validating += new System.ComponentModel.CancelEventHandler(this.txtFilterByMZ_Validating);
            // 
            // lblFilterByMZ
            // 
            this.lblFilterByMZ.Location = new System.Drawing.Point(200, 164);
            this.lblFilterByMZ.Name = "lblFilterByMZ";
            this.lblFilterByMZ.Size = new System.Drawing.Size(24, 16);
            this.lblFilterByMZ.TabIndex = 15;
            this.lblFilterByMZ.Text = "m/z";
            // 
            // chkFilterByMZ
            // 
            this.chkFilterByMZ.Location = new System.Drawing.Point(8, 160);
            this.chkFilterByMZ.Name = "chkFilterByMZ";
            this.chkFilterByMZ.Size = new System.Drawing.Size(96, 16);
            this.chkFilterByMZ.TabIndex = 13;
            this.chkFilterByMZ.Text = "Filter by m/z";
            this.chkFilterByMZ.CheckedChanged += new System.EventHandler(this.chkFilterByMZ_CheckedChanged);
            // 
            // txtFilterByMZTol
            // 
            this.txtFilterByMZTol.Location = new System.Drawing.Point(160, 184);
            this.txtFilterByMZTol.Name = "txtFilterByMZTol";
            this.txtFilterByMZTol.Size = new System.Drawing.Size(40, 20);
            this.txtFilterByMZTol.TabIndex = 17;
            this.txtFilterByMZTol.Text = "0.2";
            this.txtFilterByMZTol.Leave += new System.EventHandler(this.txtFilterByMZTol_Leave);
            this.txtFilterByMZTol.Validating += new System.ComponentModel.CancelEventHandler(this.txtFilterByMZTol_Validating);
            // 
            // lblFilterByMZTolUnits
            // 
            this.lblFilterByMZTolUnits.Location = new System.Drawing.Point(200, 188);
            this.lblFilterByMZTolUnits.Name = "lblFilterByMZTolUnits";
            this.lblFilterByMZTolUnits.Size = new System.Drawing.Size(32, 16);
            this.lblFilterByMZTolUnits.TabIndex = 18;
            this.lblFilterByMZTolUnits.Text = "m/z";
            // 
            // lblFilterByMZTol
            // 
            this.lblFilterByMZTol.Location = new System.Drawing.Point(136, 188);
            this.lblFilterByMZTol.Name = "lblFilterByMZTol";
            this.lblFilterByMZTol.Size = new System.Drawing.Size(16, 16);
            this.lblFilterByMZTol.TabIndex = 16;
            this.lblFilterByMZTol.Text = "±";
            this.lblFilterByMZTol.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFixYRange
            // 
            this.txtFixYRange.Location = new System.Drawing.Point(128, 88);
            this.txtFixYRange.Name = "txtFixYRange";
            this.txtFixYRange.Size = new System.Drawing.Size(72, 20);
            this.txtFixYRange.TabIndex = 6;
            this.txtFixYRange.Text = "1E6";
            this.txtFixYRange.TextChanged += new System.EventHandler(this.txtFixYRange_TextChanged);
            this.txtFixYRange.Validating += new System.ComponentModel.CancelEventHandler(this.txtFixYRange_Validating);
            // 
            // lblFixYRange
            // 
            this.lblFixYRange.Location = new System.Drawing.Point(200, 92);
            this.lblFixYRange.Name = "lblFixYRange";
            this.lblFixYRange.Size = new System.Drawing.Size(46, 16);
            this.lblFixYRange.TabIndex = 7;
            this.lblFixYRange.Text = "counts";
            // 
            // chkFixYRange
            // 
            this.chkFixYRange.Checked = true;
            this.chkFixYRange.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFixYRange.Location = new System.Drawing.Point(8, 88);
            this.chkFixYRange.Name = "chkFixYRange";
            this.chkFixYRange.Size = new System.Drawing.Size(88, 16);
            this.chkFixYRange.TabIndex = 5;
            this.chkFixYRange.Text = "Fix Y Range";
            this.chkFixYRange.CheckedChanged += new System.EventHandler(this.chkFixYRange_CheckedChanged);
            // 
            // lblSICsTypeFilter
            // 
            this.lblSICsTypeFilter.Location = new System.Drawing.Point(8, 8);
            this.lblSICsTypeFilter.Name = "lblSICsTypeFilter";
            this.lblSICsTypeFilter.Size = new System.Drawing.Size(152, 16);
            this.lblSICsTypeFilter.TabIndex = 0;
            this.lblSICsTypeFilter.Text = "SIC Type Filter";
            // 
            // cboSICsTypeFilter
            // 
            this.cboSICsTypeFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSICsTypeFilter.Location = new System.Drawing.Point(8, 24);
            this.cboSICsTypeFilter.Name = "cboSICsTypeFilter";
            this.cboSICsTypeFilter.Size = new System.Drawing.Size(232, 21);
            this.cboSICsTypeFilter.TabIndex = 1;
            this.cboSICsTypeFilter.SelectedIndexChanged += new System.EventHandler(this.cboSICsTypeFilter_SelectedIndexChanged);
            // 
            // txtStats1
            // 
            this.txtStats1.Location = new System.Drawing.Point(8, 80);
            this.txtStats1.Multiline = true;
            this.txtStats1.Name = "txtStats1";
            this.txtStats1.ReadOnly = true;
            this.txtStats1.Size = new System.Drawing.Size(132, 88);
            this.txtStats1.TabIndex = 4;
            this.txtStats1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtStats1_KeyPress);
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
            this.mnuFileSelectMASICInputFile,
            this.mnuFileSelectMSMSSearchResultsFile,
            this.mnuFileSep1,
            this.mnuFileExit});
            this.mnuFile.Text = "&File";
            // 
            // mnuFileSelectMASICInputFile
            // 
            this.mnuFileSelectMASICInputFile.Index = 0;
            this.mnuFileSelectMASICInputFile.Text = "&Select MASIC Input File";
            this.mnuFileSelectMASICInputFile.Click += new System.EventHandler(this.mnuFileSelectMASICInputFile_Click);
            // 
            // mnuFileSelectMSMSSearchResultsFile
            // 
            this.mnuFileSelectMSMSSearchResultsFile.Index = 1;
            this.mnuFileSelectMSMSSearchResultsFile.Text = "Select MS/MS Search &Results File (Syn or FHT)";
            this.mnuFileSelectMSMSSearchResultsFile.Click += new System.EventHandler(this.mnuFileSelectMSMSSearchResultsFile_Click);
            // 
            // mnuFileSep1
            // 
            this.mnuFileSep1.Index = 2;
            this.mnuFileSep1.Text = "-";
            // 
            // mnuFileExit
            // 
            this.mnuFileExit.Index = 3;
            this.mnuFileExit.Text = "E&xit";
            this.mnuFileExit.Click += new System.EventHandler(this.mnuFileExit_Click);
            // 
            // mnuEdit
            // 
            this.mnuEdit.Index = 1;
            this.mnuEdit.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuEditShowOptimalPeakApexCursor});
            this.mnuEdit.Text = "&Edit";
            // 
            // mnuEditShowOptimalPeakApexCursor
            // 
            this.mnuEditShowOptimalPeakApexCursor.Checked = true;
            this.mnuEditShowOptimalPeakApexCursor.Index = 0;
            this.mnuEditShowOptimalPeakApexCursor.Text = "&Show optimal peak apex cursor";
            this.mnuEditShowOptimalPeakApexCursor.Visible = false;
            this.mnuEditShowOptimalPeakApexCursor.Click += new System.EventHandler(this.mnuEditShowOptimalPeakApexCursor_Click);
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
            // chkSortDescending
            // 
            this.chkSortDescending.Location = new System.Drawing.Point(160, 8);
            this.chkSortDescending.Name = "chkSortDescending";
            this.chkSortDescending.Size = new System.Drawing.Size(112, 16);
            this.chkSortDescending.TabIndex = 2;
            this.chkSortDescending.Text = "Sort Descending";
            this.chkSortDescending.CheckedChanged += new System.EventHandler(this.chkSortDescending_CheckedChanged);
            // 
            // lstParentIonData
            // 
            this.lstParentIonData.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lstParentIonData.Location = new System.Drawing.Point(8, 24);
            this.lstParentIonData.Name = "lstParentIonData";
            this.lstParentIonData.Size = new System.Drawing.Size(272, 160);
            this.lstParentIonData.TabIndex = 3;
            this.lstParentIonData.SelectedIndexChanged += new System.EventHandler(this.lstParentIonData_SelectedIndexChanged);
            // 
            // txtMinimumSignalToNoise
            // 
            this.txtMinimumSignalToNoise.Location = new System.Drawing.Point(144, 112);
            this.txtMinimumSignalToNoise.Name = "txtMinimumSignalToNoise";
            this.txtMinimumSignalToNoise.Size = new System.Drawing.Size(56, 20);
            this.txtMinimumSignalToNoise.TabIndex = 9;
            this.txtMinimumSignalToNoise.Text = "3";
            this.txtMinimumSignalToNoise.Leave += new System.EventHandler(this.txtMinimumSignalToNoise_Leave);
            this.txtMinimumSignalToNoise.Validating += new System.ComponentModel.CancelEventHandler(this.txtMinimumSignalToNoise_Validating);
            // 
            // chkFilterBySignalToNoise
            // 
            this.chkFilterBySignalToNoise.Location = new System.Drawing.Point(8, 112);
            this.chkFilterBySignalToNoise.Name = "chkFilterBySignalToNoise";
            this.chkFilterBySignalToNoise.Size = new System.Drawing.Size(120, 16);
            this.chkFilterBySignalToNoise.TabIndex = 8;
            this.chkFilterBySignalToNoise.Text = "Minimum S/N";
            this.chkFilterBySignalToNoise.CheckedChanged += new System.EventHandler(this.chkFilterBySignalToNoise_CheckedChanged);
            // 
            // fraResmoothingOptions
            // 
            this.fraResmoothingOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.fraResmoothingOptions.Controls.Add(this.chkShowSmoothedData);
            this.fraResmoothingOptions.Controls.Add(this.txtPeakWidthPointsMinimum);
            this.fraResmoothingOptions.Controls.Add(this.lblPeakWidthPointsMinimum);
            this.fraResmoothingOptions.Controls.Add(this.optDoNotResmooth);
            this.fraResmoothingOptions.Controls.Add(this.optUseSavitzkyGolaySmooth);
            this.fraResmoothingOptions.Controls.Add(this.txtButterworthSamplingFrequency);
            this.fraResmoothingOptions.Controls.Add(this.lblButterworthSamplingFrequency);
            this.fraResmoothingOptions.Controls.Add(this.txtSavitzkyGolayFilterOrder);
            this.fraResmoothingOptions.Controls.Add(this.lblSavitzkyGolayFilterOrder);
            this.fraResmoothingOptions.Controls.Add(this.optUseButterworthSmooth);
            this.fraResmoothingOptions.Location = new System.Drawing.Point(8, 8);
            this.fraResmoothingOptions.Name = "fraResmoothingOptions";
            this.fraResmoothingOptions.Size = new System.Drawing.Size(320, 136);
            this.fraResmoothingOptions.TabIndex = 7;
            this.fraResmoothingOptions.TabStop = false;
            this.fraResmoothingOptions.Text = "Smoothing Options";
            // 
            // chkShowSmoothedData
            // 
            this.chkShowSmoothedData.Checked = true;
            this.chkShowSmoothedData.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowSmoothedData.Location = new System.Drawing.Point(16, 16);
            this.chkShowSmoothedData.Name = "chkShowSmoothedData";
            this.chkShowSmoothedData.Size = new System.Drawing.Size(152, 16);
            this.chkShowSmoothedData.TabIndex = 0;
            this.chkShowSmoothedData.Text = "Show Smoothed Data";
            // 
            // txtPeakWidthPointsMinimum
            // 
            this.txtPeakWidthPointsMinimum.Location = new System.Drawing.Point(264, 104);
            this.txtPeakWidthPointsMinimum.Name = "txtPeakWidthPointsMinimum";
            this.txtPeakWidthPointsMinimum.Size = new System.Drawing.Size(48, 20);
            this.txtPeakWidthPointsMinimum.TabIndex = 9;
            this.txtPeakWidthPointsMinimum.Text = "6";
            this.txtPeakWidthPointsMinimum.TextChanged += new System.EventHandler(this.txtPeakWidthPointsMinimum_TextChanged);
            this.txtPeakWidthPointsMinimum.Validating += new System.ComponentModel.CancelEventHandler(this.txtPeakWidthPointsMinimum_Validating);
            // 
            // lblPeakWidthPointsMinimum
            // 
            this.lblPeakWidthPointsMinimum.Location = new System.Drawing.Point(96, 112);
            this.lblPeakWidthPointsMinimum.Name = "lblPeakWidthPointsMinimum";
            this.lblPeakWidthPointsMinimum.Size = new System.Drawing.Size(160, 16);
            this.lblPeakWidthPointsMinimum.TabIndex = 8;
            this.lblPeakWidthPointsMinimum.Text = "Minimum Peak Width (points)";
            // 
            // optDoNotResmooth
            // 
            this.optDoNotResmooth.Checked = true;
            this.optDoNotResmooth.Location = new System.Drawing.Point(16, 48);
            this.optDoNotResmooth.Name = "optDoNotResmooth";
            this.optDoNotResmooth.Size = new System.Drawing.Size(200, 16);
            this.optDoNotResmooth.TabIndex = 1;
            this.optDoNotResmooth.TabStop = true;
            this.optDoNotResmooth.Text = "Do Not Show Smoothed Data";
            this.optDoNotResmooth.CheckedChanged += new System.EventHandler(this.optDoNotResmooth_CheckedChanged);
            // 
            // optUseSavitzkyGolaySmooth
            // 
            this.optUseSavitzkyGolaySmooth.Location = new System.Drawing.Point(16, 80);
            this.optUseSavitzkyGolaySmooth.Name = "optUseSavitzkyGolaySmooth";
            this.optUseSavitzkyGolaySmooth.Size = new System.Drawing.Size(168, 16);
            this.optUseSavitzkyGolaySmooth.TabIndex = 5;
            this.optUseSavitzkyGolaySmooth.Text = "Use Savitzky Golay Smooth";
            this.optUseSavitzkyGolaySmooth.CheckedChanged += new System.EventHandler(this.optUseSavitzkyGolaySmooth_CheckedChanged);
            // 
            // txtButterworthSamplingFrequency
            // 
            this.txtButterworthSamplingFrequency.Location = new System.Drawing.Point(264, 56);
            this.txtButterworthSamplingFrequency.Name = "txtButterworthSamplingFrequency";
            this.txtButterworthSamplingFrequency.Size = new System.Drawing.Size(48, 20);
            this.txtButterworthSamplingFrequency.TabIndex = 4;
            this.txtButterworthSamplingFrequency.Text = "0.25";
            this.txtButterworthSamplingFrequency.TextChanged += new System.EventHandler(this.txtButterworthSamplingFrequency_TextChanged);
            this.txtButterworthSamplingFrequency.Validating += new System.ComponentModel.CancelEventHandler(this.txtButterworthSamplingFrequency_Validating);
            // 
            // lblButterworthSamplingFrequency
            // 
            this.lblButterworthSamplingFrequency.Location = new System.Drawing.Point(184, 64);
            this.lblButterworthSamplingFrequency.Name = "lblButterworthSamplingFrequency";
            this.lblButterworthSamplingFrequency.Size = new System.Drawing.Size(72, 16);
            this.lblButterworthSamplingFrequency.TabIndex = 3;
            this.lblButterworthSamplingFrequency.Text = "Filter Order";
            // 
            // txtSavitzkyGolayFilterOrder
            // 
            this.txtSavitzkyGolayFilterOrder.Location = new System.Drawing.Point(264, 80);
            this.txtSavitzkyGolayFilterOrder.Name = "txtSavitzkyGolayFilterOrder";
            this.txtSavitzkyGolayFilterOrder.Size = new System.Drawing.Size(48, 20);
            this.txtSavitzkyGolayFilterOrder.TabIndex = 7;
            this.txtSavitzkyGolayFilterOrder.Text = "0";
            this.txtSavitzkyGolayFilterOrder.TextChanged += new System.EventHandler(this.txtSavitzkyGolayFilterOrder_TextChanged);
            this.txtSavitzkyGolayFilterOrder.Validating += new System.ComponentModel.CancelEventHandler(this.txtSavitzkyGolayFilterOrder_Validating);
            // 
            // lblSavitzkyGolayFilterOrder
            // 
            this.lblSavitzkyGolayFilterOrder.Location = new System.Drawing.Point(184, 88);
            this.lblSavitzkyGolayFilterOrder.Name = "lblSavitzkyGolayFilterOrder";
            this.lblSavitzkyGolayFilterOrder.Size = new System.Drawing.Size(72, 16);
            this.lblSavitzkyGolayFilterOrder.TabIndex = 6;
            this.lblSavitzkyGolayFilterOrder.Text = "Filter Order";
            // 
            // optUseButterworthSmooth
            // 
            this.optUseButterworthSmooth.Location = new System.Drawing.Point(16, 64);
            this.optUseButterworthSmooth.Name = "optUseButterworthSmooth";
            this.optUseButterworthSmooth.Size = new System.Drawing.Size(168, 16);
            this.optUseButterworthSmooth.TabIndex = 2;
            this.optUseButterworthSmooth.Text = "Use Butterworth Smooth";
            this.optUseButterworthSmooth.CheckedChanged += new System.EventHandler(this.optUseButterworthSmooth_CheckedChanged);
            // 
            // fraPeakFinder
            // 
            this.fraPeakFinder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.fraPeakFinder.Controls.Add(this.cmdRedoSICPeakFindingAllData);
            this.fraPeakFinder.Controls.Add(this.chkUsePeakFinder);
            this.fraPeakFinder.Controls.Add(this.chkFindPeaksSubtractBaseline);
            this.fraPeakFinder.Location = new System.Drawing.Point(336, 8);
            this.fraPeakFinder.Name = "fraPeakFinder";
            this.fraPeakFinder.Size = new System.Drawing.Size(200, 128);
            this.fraPeakFinder.TabIndex = 8;
            this.fraPeakFinder.TabStop = false;
            this.fraPeakFinder.Text = "SIC Peak Finding";
            // 
            // cmdRedoSICPeakFindingAllData
            // 
            this.cmdRedoSICPeakFindingAllData.Location = new System.Drawing.Point(16, 80);
            this.cmdRedoSICPeakFindingAllData.Name = "cmdRedoSICPeakFindingAllData";
            this.cmdRedoSICPeakFindingAllData.Size = new System.Drawing.Size(112, 40);
            this.cmdRedoSICPeakFindingAllData.TabIndex = 20;
            this.cmdRedoSICPeakFindingAllData.Text = "Redo SIC Peak Finding For All Data";
            this.cmdRedoSICPeakFindingAllData.Click += new System.EventHandler(this.cmdRedoSICPeakFindingAllData_Click);
            // 
            // chkUsePeakFinder
            // 
            this.chkUsePeakFinder.Location = new System.Drawing.Point(8, 16);
            this.chkUsePeakFinder.Name = "chkUsePeakFinder";
            this.chkUsePeakFinder.Size = new System.Drawing.Size(184, 16);
            this.chkUsePeakFinder.TabIndex = 18;
            this.chkUsePeakFinder.Text = "Recompute SIC Peak Stats";
            this.chkUsePeakFinder.CheckedChanged += new System.EventHandler(this.chkUsePeakFinder_CheckedChanged);
            // 
            // chkFindPeaksSubtractBaseline
            // 
            this.chkFindPeaksSubtractBaseline.Checked = true;
            this.chkFindPeaksSubtractBaseline.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFindPeaksSubtractBaseline.Location = new System.Drawing.Point(16, 33);
            this.chkFindPeaksSubtractBaseline.Name = "chkFindPeaksSubtractBaseline";
            this.chkFindPeaksSubtractBaseline.Size = new System.Drawing.Size(176, 32);
            this.chkFindPeaksSubtractBaseline.TabIndex = 19;
            this.chkFindPeaksSubtractBaseline.Text = "Subtract baseline when computing Intensity and Area";
            // 
            // fraSortOrderAndStats
            // 
            this.fraSortOrderAndStats.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.fraSortOrderAndStats.Controls.Add(this.chkShowBaselineCorrectedStats);
            this.fraSortOrderAndStats.Controls.Add(this.txtStats2);
            this.fraSortOrderAndStats.Controls.Add(this.txtStats3);
            this.fraSortOrderAndStats.Controls.Add(this.cboSortOrder);
            this.fraSortOrderAndStats.Controls.Add(this.lblSortOrder);
            this.fraSortOrderAndStats.Controls.Add(this.chkSortDescending);
            this.fraSortOrderAndStats.Controls.Add(this.txtStats1);
            this.fraSortOrderAndStats.Location = new System.Drawing.Point(0, 206);
            this.fraSortOrderAndStats.Name = "fraSortOrderAndStats";
            this.fraSortOrderAndStats.Size = new System.Drawing.Size(282, 225);
            this.fraSortOrderAndStats.TabIndex = 5;
            this.fraSortOrderAndStats.TabStop = false;
            // 
            // chkShowBaselineCorrectedStats
            // 
            this.chkShowBaselineCorrectedStats.Checked = true;
            this.chkShowBaselineCorrectedStats.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowBaselineCorrectedStats.Location = new System.Drawing.Point(8, 58);
            this.chkShowBaselineCorrectedStats.Name = "chkShowBaselineCorrectedStats";
            this.chkShowBaselineCorrectedStats.Size = new System.Drawing.Size(216, 16);
            this.chkShowBaselineCorrectedStats.TabIndex = 14;
            this.chkShowBaselineCorrectedStats.Text = "Show Baseline Corrected Stats";
            this.chkShowBaselineCorrectedStats.CheckedChanged += new System.EventHandler(this.chkShowBaselineCorrectedStats_CheckedChanged);
            // 
            // txtStats2
            // 
            this.txtStats2.Location = new System.Drawing.Point(144, 80);
            this.txtStats2.Multiline = true;
            this.txtStats2.Name = "txtStats2";
            this.txtStats2.ReadOnly = true;
            this.txtStats2.Size = new System.Drawing.Size(132, 88);
            this.txtStats2.TabIndex = 6;
            this.txtStats2.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtStats2_KeyPress);
            // 
            // txtStats3
            // 
            this.txtStats3.Location = new System.Drawing.Point(8, 176);
            this.txtStats3.Multiline = true;
            this.txtStats3.Name = "txtStats3";
            this.txtStats3.ReadOnly = true;
            this.txtStats3.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStats3.Size = new System.Drawing.Size(266, 40);
            this.txtStats3.TabIndex = 5;
            this.txtStats3.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txStats3_KeyPress);
            // 
            // TabControl1
            // 
            this.TabControl1.Controls.Add(this.tpSICFilters);
            this.TabControl1.Controls.Add(this.tpMsMsSearchResultsFilters);
            this.TabControl1.Location = new System.Drawing.Point(16, 160);
            this.TabControl1.Name = "TabControl1";
            this.TabControl1.SelectedIndex = 0;
            this.TabControl1.Size = new System.Drawing.Size(256, 240);
            this.TabControl1.TabIndex = 6;
            // 
            // tpSICFilters
            // 
            this.tpSICFilters.Controls.Add(this.chkFilterByMZ);
            this.tpSICFilters.Controls.Add(this.txtFilterByMZTol);
            this.tpSICFilters.Controls.Add(this.lblFilterByMZTolUnits);
            this.tpSICFilters.Controls.Add(this.lblFilterByMZTol);
            this.tpSICFilters.Controls.Add(this.txtFixYRange);
            this.tpSICFilters.Controls.Add(this.lblFixYRange);
            this.tpSICFilters.Controls.Add(this.chkFixYRange);
            this.tpSICFilters.Controls.Add(this.lblSICsTypeFilter);
            this.tpSICFilters.Controls.Add(this.cboSICsTypeFilter);
            this.tpSICFilters.Controls.Add(this.chkFixXRange);
            this.tpSICFilters.Controls.Add(this.txtFixXRange);
            this.tpSICFilters.Controls.Add(this.lblFixXRange);
            this.tpSICFilters.Controls.Add(this.lblMinimumIntensity);
            this.tpSICFilters.Controls.Add(this.txtMinimumIntensity);
            this.tpSICFilters.Controls.Add(this.chkFilterByIntensity);
            this.tpSICFilters.Controls.Add(this.txtMinimumSignalToNoise);
            this.tpSICFilters.Controls.Add(this.chkFilterBySignalToNoise);
            this.tpSICFilters.Controls.Add(this.txtFilterByMZ);
            this.tpSICFilters.Controls.Add(this.lblFilterByMZ);
            this.tpSICFilters.Location = new System.Drawing.Point(4, 22);
            this.tpSICFilters.Name = "tpSICFilters";
            this.tpSICFilters.Size = new System.Drawing.Size(248, 214);
            this.tpSICFilters.TabIndex = 0;
            this.tpSICFilters.Text = "SIC Filters";
            // 
            // tpMsMsSearchResultsFilters
            // 
            this.tpMsMsSearchResultsFilters.Controls.Add(this.chkSequenceFilterExactMatch);
            this.tpMsMsSearchResultsFilters.Controls.Add(this.lblSequenceFilter);
            this.tpMsMsSearchResultsFilters.Controls.Add(this.lblChargeFilter);
            this.tpMsMsSearchResultsFilters.Controls.Add(this.TextBox2);
            this.tpMsMsSearchResultsFilters.Controls.Add(this.lblMinimumXCorr);
            this.tpMsMsSearchResultsFilters.Controls.Add(this.txtSequenceFilter);
            this.tpMsMsSearchResultsFilters.Controls.Add(this.txtMinimumXCorr);
            this.tpMsMsSearchResultsFilters.Location = new System.Drawing.Point(4, 22);
            this.tpMsMsSearchResultsFilters.Name = "tpMsMsSearchResultsFilters";
            this.tpMsMsSearchResultsFilters.Size = new System.Drawing.Size(248, 214);
            this.tpMsMsSearchResultsFilters.TabIndex = 1;
            this.tpMsMsSearchResultsFilters.Text = "MS/MS Results Filters";
            // 
            // chkSequenceFilterExactMatch
            // 
            this.chkSequenceFilterExactMatch.Checked = true;
            this.chkSequenceFilterExactMatch.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSequenceFilterExactMatch.Enabled = false;
            this.chkSequenceFilterExactMatch.Location = new System.Drawing.Point(144, 72);
            this.chkSequenceFilterExactMatch.Name = "chkSequenceFilterExactMatch";
            this.chkSequenceFilterExactMatch.Size = new System.Drawing.Size(90, 16);
            this.chkSequenceFilterExactMatch.TabIndex = 5;
            this.chkSequenceFilterExactMatch.Text = "Exact Match?";
            // 
            // lblSequenceFilter
            // 
            this.lblSequenceFilter.Location = new System.Drawing.Point(8, 72);
            this.lblSequenceFilter.Name = "lblSequenceFilter";
            this.lblSequenceFilter.Size = new System.Drawing.Size(96, 16);
            this.lblSequenceFilter.TabIndex = 4;
            this.lblSequenceFilter.Text = "Sequence Filter";
            // 
            // lblChargeFilter
            // 
            this.lblChargeFilter.Location = new System.Drawing.Point(8, 40);
            this.lblChargeFilter.Name = "lblChargeFilter";
            this.lblChargeFilter.Size = new System.Drawing.Size(96, 16);
            this.lblChargeFilter.TabIndex = 2;
            this.lblChargeFilter.Text = "Charge Filter";
            // 
            // TextBox2
            // 
            this.TextBox2.Enabled = false;
            this.TextBox2.Location = new System.Drawing.Point(144, 40);
            this.TextBox2.Name = "TextBox2";
            this.TextBox2.Size = new System.Drawing.Size(56, 20);
            this.TextBox2.TabIndex = 3;
            this.TextBox2.Text = "0";
            // 
            // lblMinimumXCorr
            // 
            this.lblMinimumXCorr.Location = new System.Drawing.Point(8, 16);
            this.lblMinimumXCorr.Name = "lblMinimumXCorr";
            this.lblMinimumXCorr.Size = new System.Drawing.Size(96, 16);
            this.lblMinimumXCorr.TabIndex = 0;
            this.lblMinimumXCorr.Text = "XCorr Minimum";
            // 
            // txtSequenceFilter
            // 
            this.txtSequenceFilter.Enabled = false;
            this.txtSequenceFilter.Location = new System.Drawing.Point(8, 88);
            this.txtSequenceFilter.Name = "txtSequenceFilter";
            this.txtSequenceFilter.Size = new System.Drawing.Size(230, 20);
            this.txtSequenceFilter.TabIndex = 6;
            // 
            // txtMinimumXCorr
            // 
            this.txtMinimumXCorr.Enabled = false;
            this.txtMinimumXCorr.Location = new System.Drawing.Point(144, 16);
            this.txtMinimumXCorr.Name = "txtMinimumXCorr";
            this.txtMinimumXCorr.Size = new System.Drawing.Size(56, 20);
            this.txtMinimumXCorr.TabIndex = 1;
            this.txtMinimumXCorr.Text = "2.2";
            // 
            // pnlInputFile
            // 
            this.pnlInputFile.Controls.Add(this.txtDataFilePath);
            this.pnlInputFile.Controls.Add(this.cmdSelectFile);
            this.pnlInputFile.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlInputFile.Location = new System.Drawing.Point(0, 0);
            this.pnlInputFile.Name = "pnlInputFile";
            this.pnlInputFile.Size = new System.Drawing.Size(568, 40);
            this.pnlInputFile.TabIndex = 9;
            // 
            // pnlSICs
            // 
            this.pnlSICs.Controls.Add(this.lblParentIon);
            this.pnlSICs.Controls.Add(this.lstParentIonData);
            this.pnlSICs.Controls.Add(this.fraSortOrderAndStats);
            this.pnlSICs.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSICs.Location = new System.Drawing.Point(0, 40);
            this.pnlSICs.Name = "pnlSICs";
            this.pnlSICs.Size = new System.Drawing.Size(288, 440);
            this.pnlSICs.TabIndex = 10;
            // 
            // pnlNavigationAndOptions
            // 
            this.pnlNavigationAndOptions.Controls.Add(this.fraNavigation);
            this.pnlNavigationAndOptions.Controls.Add(this.TabControl1);
            this.pnlNavigationAndOptions.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlNavigationAndOptions.Location = new System.Drawing.Point(288, 40);
            this.pnlNavigationAndOptions.Name = "pnlNavigationAndOptions";
            this.pnlNavigationAndOptions.Size = new System.Drawing.Size(280, 440);
            this.pnlNavigationAndOptions.TabIndex = 11;
            // 
            // pnlBottom
            // 
            this.pnlBottom.Controls.Add(this.fraPeakFinder);
            this.pnlBottom.Controls.Add(this.fraResmoothingOptions);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 480);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(568, 152);
            this.pnlBottom.TabIndex = 12;
            // 
            // frmBrowser
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(568, 632);
            this.Controls.Add(this.pnlNavigationAndOptions);
            this.Controls.Add(this.pnlSICs);
            this.Controls.Add(this.pnlInputFile);
            this.Controls.Add(this.pnlBottom);
            this.Menu = this.MainMenuControl;
            this.MinimumSize = new System.Drawing.Size(512, 0);
            this.Name = "frmBrowser";
            this.Text = "MASIC Browser";
            ((System.ComponentModel.ISupportInitialize)(this.tmrAutoStep)).EndInit();
            this.fraNavigation.ResumeLayout(false);
            this.fraNavigation.PerformLayout();
            this.fraResmoothingOptions.ResumeLayout(false);
            this.fraResmoothingOptions.PerformLayout();
            this.fraPeakFinder.ResumeLayout(false);
            this.fraSortOrderAndStats.ResumeLayout(false);
            this.fraSortOrderAndStats.PerformLayout();
            this.TabControl1.ResumeLayout(false);
            this.tpSICFilters.ResumeLayout(false);
            this.tpSICFilters.PerformLayout();
            this.tpMsMsSearchResultsFilters.ResumeLayout(false);
            this.tpMsMsSearchResultsFilters.PerformLayout();
            this.pnlInputFile.ResumeLayout(false);
            this.pnlInputFile.PerformLayout();
            this.pnlSICs.ResumeLayout(false);
            this.pnlNavigationAndOptions.ResumeLayout(false);
            this.pnlBottom.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Label lblParentIon;
        private Button cmdSelectFile;
        private Label lblSortOrder;
        private ComboBox cboSortOrder;
        private CheckBox chkFixXRange;
        private TextBox txtFixXRange;
        private Label lblFixXRange;
        private Label lblMinimumIntensity;
        private TextBox txtMinimumIntensity;
        private CheckBox chkFilterByIntensity;
        private System.Timers.Timer tmrAutoStep;
        private GroupBox fraNavigation;
        private CheckBox chkAutoStepForward;
        private TextBox txtAutoStep;
        private Label lblAutoStep;
        private Button cmdAutoStep;
        private Button cmdPrevious;
        private Button cmdNext;
        private Button cmdJump;
        private TextBox txtFilterByMZ;
        private Label lblFilterByMZ;
        private CheckBox chkFilterByMZ;
        private TextBox txtFilterByMZTol;
        private Label lblFilterByMZTolUnits;
        private Label lblFilterByMZTol;
        private TextBox txtFixYRange;
        private Label lblFixYRange;
        private CheckBox chkFixYRange;
        private Label lblSICsTypeFilter;
        private ComboBox cboSICsTypeFilter;
        private MainMenu MainMenuControl;
        private MenuItem mnuFile;
        private MenuItem mnuFileExit;
        private MenuItem mnuFileSelectMASICInputFile;
        private MenuItem mnuFileSep1;
        private MenuItem mnuEdit;
        private MenuItem mnuHelp;
        private MenuItem mnuHelpAbout;
        private MenuItem mnuEditShowOptimalPeakApexCursor;
        private CheckBox chkSortDescending;
        private ListBox lstParentIonData;
        private GroupBox fraResmoothingOptions;
        private RadioButton optUseSavitzkyGolaySmooth;
        private TextBox txtButterworthSamplingFrequency;
        private Label lblButterworthSamplingFrequency;
        private TextBox txtSavitzkyGolayFilterOrder;
        private Label lblSavitzkyGolayFilterOrder;
        private RadioButton optUseButterworthSmooth;
        private RadioButton optDoNotResmooth;
        private TextBox txtPeakWidthPointsMinimum;
        private Label lblPeakWidthPointsMinimum;
        private GroupBox fraPeakFinder;
        private CheckBox chkUsePeakFinder;
        private CheckBox chkFindPeaksSubtractBaseline;
        private TextBox txtMinimumSignalToNoise;
        private CheckBox chkFilterBySignalToNoise;
        private Button cmdRedoSICPeakFindingAllData;
        private GroupBox fraSortOrderAndStats;
        private MenuItem mnuFileSelectMSMSSearchResultsFile;
        private CheckBox chkShowSmoothedData;
        private TabControl TabControl1;
        private TabPage tpSICFilters;
        private TabPage tpMsMsSearchResultsFilters;
        private TextBox txtMinimumXCorr;
        private TextBox txtSequenceFilter;
        private Label lblMinimumXCorr;
        private Label lblChargeFilter;
        private TextBox TextBox2;
        private Label lblSequenceFilter;
        private CheckBox chkSequenceFilterExactMatch;
        private TextBox txtStats1;
        private TextBox txtStats3;
        private TextBox txtStats2;
        private CheckBox chkShowBaselineCorrectedStats;
        private Panel pnlInputFile;
        private Panel pnlSICs;
        private Panel pnlNavigationAndOptions;
        private Panel pnlBottom;
        private TextBox txtDataFilePath;
    }
}
