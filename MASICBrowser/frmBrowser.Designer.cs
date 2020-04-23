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
            components = new Container();
            lblParentIon = new Label();
            txtDataFilePath = new TextBox();
            cmdSelectFile = new Button();
            lblSortOrder = new Label();
            cboSortOrder = new ComboBox();
            chkFixXRange = new CheckBox();
            txtFixXRange = new TextBox();
            lblFixXRange = new Label();
            lblMinimumIntensity = new Label();
            txtMinimumIntensity = new TextBox();
            chkFilterByIntensity = new CheckBox();
            tmrAutoStep = new System.Timers.Timer();
            fraNavigation = new GroupBox();
            chkAutoStepForward = new CheckBox();
            txtAutoStep = new TextBox();
            lblAutoStep = new Label();
            cmdAutoStep = new Button();
            cmdPrevious = new Button();
            cmdNext = new Button();
            cmdJump = new Button();
            txtFilterByMZ = new TextBox();
            lblFilterByMZ = new Label();
            chkFilterByMZ = new CheckBox();
            txtFilterByMZTol = new TextBox();
            lblFilterByMZTolUnits = new Label();
            lblFilterByMZTol = new Label();
            txtFixYRange = new TextBox();
            lblFixYRange = new Label();
            chkFixYRange = new CheckBox();
            lblSICsTypeFilter = new Label();
            cboSICsTypeFilter = new ComboBox();
            txtStats1 = new TextBox();
            MainMenuControl = new MainMenu(components);
            mnuFile = new MenuItem();
            mnuFileSelectMASICInputFile = new MenuItem();
            mnuFileSelectMSMSSearchResultsFile = new MenuItem();
            mnuFileSep1 = new MenuItem();
            mnuFileExit = new MenuItem();
            mnuEdit = new MenuItem();
            mnuEditShowOptimalPeakApexCursor = new MenuItem();
            mnuHelp = new MenuItem();
            mnuHelpAbout = new MenuItem();
            chkSortDescending = new CheckBox();
            lstParentIonData = new ListBox();
            txtMinimumSignalToNoise = new TextBox();
            chkFilterBySignalToNoise = new CheckBox();
            fraResmoothingOptions = new GroupBox();
            chkShowSmoothedData = new CheckBox();
            txtPeakWidthPointsMinimum = new TextBox();
            lblPeakWidthPointsMinimum = new Label();
            optDoNotResmooth = new RadioButton();
            optUseSavitzkyGolaySmooth = new RadioButton();
            txtButterworthSamplingFrequency = new TextBox();
            lblButterworthSamplingFrequency = new Label();
            txtSavitzkyGolayFilterOrder = new TextBox();
            lblSavitzkyGolayFilterOrder = new Label();
            optUseButterworthSmooth = new RadioButton();
            fraPeakFinder = new GroupBox();
            cmdRedoSICPeakFindingAllData = new Button();
            chkUsePeakFinder = new CheckBox();
            chkFindPeaksSubtractBaseline = new CheckBox();
            fraSortOrderAndStats = new GroupBox();
            chkShowBaselineCorrectedStats = new CheckBox();
            txtStats2 = new TextBox();
            txtStats3 = new TextBox();
            TabControl1 = new TabControl();
            tpSICFilters = new TabPage();
            tpMsMsSearchResultsFilters = new TabPage();
            chkSequenceFilterExactMatch = new CheckBox();
            lblSequenceFilter = new Label();
            lblChargeFilter = new Label();
            TextBox2 = new TextBox();
            lblMinimumXCorr = new Label();
            txtSequenceFilter = new TextBox();
            txtMinimumXCorr = new TextBox();
            pnlInputFile = new Panel();
            pnlSICs = new Panel();
            pnlNavigationAndOptions = new Panel();
            pnlBottom = new Panel();
            ((ISupportInitialize)tmrAutoStep).BeginInit();
            fraNavigation.SuspendLayout();
            fraResmoothingOptions.SuspendLayout();
            fraPeakFinder.SuspendLayout();
            fraSortOrderAndStats.SuspendLayout();
            TabControl1.SuspendLayout();
            tpSICFilters.SuspendLayout();
            tpMsMsSearchResultsFilters.SuspendLayout();
            pnlInputFile.SuspendLayout();
            pnlSICs.SuspendLayout();
            pnlNavigationAndOptions.SuspendLayout();
            pnlBottom.SuspendLayout();
            SuspendLayout();
            //
            // lblParentIon
            //
            lblParentIon.Location = new Point(10, 9);
            lblParentIon.Name = "lblParentIon";
            lblParentIon.Size = new Size(182, 19);
            lblParentIon.TabIndex = 2;
            lblParentIon.Text = "Parent Ion SIC to View";
            //
            // txtDataFilePath
            //
            txtDataFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtDataFilePath.Location = new Point(115, 11);
            txtDataFilePath.Name = "txtDataFilePath";
            txtDataFilePath.Size = new Size(587, 22);
            txtDataFilePath.TabIndex = 1;
            txtDataFilePath.Text = @"D:\";
            //
            // cmdSelectFile
            //
            cmdSelectFile.Location = new Point(10, 9);
            cmdSelectFile.Name = "cmdSelectFile";
            cmdSelectFile.Size = new Size(96, 28);
            cmdSelectFile.TabIndex = 0;
            cmdSelectFile.Text = "&Select File";
            cmdSelectFile.Click += new EventHandler(cmdSelectFile_Click);
            //
            // lblSortOrder
            //
            lblSortOrder.Location = new Point(10, 9);
            lblSortOrder.Name = "lblSortOrder";
            lblSortOrder.Size = new Size(105, 19);
            lblSortOrder.TabIndex = 0;
            lblSortOrder.Text = "Sort Order";
            //
            // cboSortOrder
            //
            cboSortOrder.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSortOrder.Location = new Point(10, 32);
            cboSortOrder.Name = "cboSortOrder";
            cboSortOrder.Size = new Size(316, 24);
            cboSortOrder.TabIndex = 1;
            cboSortOrder.SelectedIndexChanged += new EventHandler(cboSortOrder_SelectedIndexChanged);
            //
            // chkFixXRange
            //
            chkFixXRange.Checked = true;
            chkFixXRange.CheckState = CheckState.Checked;
            chkFixXRange.Location = new Point(10, 74);
            chkFixXRange.Name = "chkFixXRange";
            chkFixXRange.Size = new Size(105, 18);
            chkFixXRange.TabIndex = 2;
            chkFixXRange.Text = "Fix X Range";
            chkFixXRange.CheckedChanged += new EventHandler(chkFixXRange_CheckedChanged);
            //
            // txtFixXRange
            //
            txtFixXRange.Location = new Point(154, 74);
            txtFixXRange.Name = "txtFixXRange";
            txtFixXRange.Size = new Size(86, 22);
            txtFixXRange.TabIndex = 3;
            txtFixXRange.Text = "300";
            txtFixXRange.TextChanged += new EventHandler(txtFixXRange_TextChanged);
            txtFixXRange.Validating += new CancelEventHandler(txtFixXRange_Validating);
            //
            // lblFixXRange
            //
            lblFixXRange.Location = new Point(240, 78);
            lblFixXRange.Name = "lblFixXRange";
            lblFixXRange.Size = new Size(55, 19);
            lblFixXRange.TabIndex = 4;
            lblFixXRange.Text = "scans";
            //
            // lblMinimumIntensity
            //
            lblMinimumIntensity.Location = new Point(240, 162);
            lblMinimumIntensity.Name = "lblMinimumIntensity";
            lblMinimumIntensity.Size = new Size(55, 18);
            lblMinimumIntensity.TabIndex = 12;
            lblMinimumIntensity.Text = "counts";
            //
            // txtMinimumIntensity
            //
            txtMinimumIntensity.Location = new Point(173, 157);
            txtMinimumIntensity.Name = "txtMinimumIntensity";
            txtMinimumIntensity.Size = new Size(67, 22);
            txtMinimumIntensity.TabIndex = 11;
            txtMinimumIntensity.Text = "1000000";
            txtMinimumIntensity.Leave += new EventHandler(txtMinimumIntensity_Leave);
            txtMinimumIntensity.Validating += new CancelEventHandler(txtMinimumIntensity_Validating);
            //
            // chkFilterByIntensity
            //
            chkFilterByIntensity.Location = new Point(10, 157);
            chkFilterByIntensity.Name = "chkFilterByIntensity";
            chkFilterByIntensity.Size = new Size(172, 18);
            chkFilterByIntensity.TabIndex = 10;
            chkFilterByIntensity.Text = "Minimum Intensity";
            chkFilterByIntensity.CheckedChanged += new EventHandler(chkFilterByIntensity_CheckedChanged);
            //
            // tmrAutoStep
            //
            tmrAutoStep.Enabled = true;
            tmrAutoStep.Interval = 10.0D;
            tmrAutoStep.SynchronizingObject = this;
            tmrAutoStep.Elapsed += new ElapsedEventHandler(tmrAutoStep_Elapsed);
            //
            // fraNavigation
            //
            fraNavigation.Controls.Add(chkAutoStepForward);
            fraNavigation.Controls.Add(txtAutoStep);
            fraNavigation.Controls.Add(lblAutoStep);
            fraNavigation.Controls.Add(cmdAutoStep);
            fraNavigation.Controls.Add(cmdPrevious);
            fraNavigation.Controls.Add(cmdNext);
            fraNavigation.Controls.Add(cmdJump);
            fraNavigation.Enabled = false;
            fraNavigation.Location = new Point(19, 28);
            fraNavigation.Name = "fraNavigation";
            fraNavigation.Size = new Size(269, 147);
            fraNavigation.TabIndex = 4;
            fraNavigation.TabStop = false;
            fraNavigation.Text = "Navigation";
            //
            // chkAutoStepForward
            //
            chkAutoStepForward.Checked = true;
            chkAutoStepForward.CheckState = CheckState.Checked;
            chkAutoStepForward.Location = new Point(134, 120);
            chkAutoStepForward.Name = "chkAutoStepForward";
            chkAutoStepForward.Size = new Size(125, 18);
            chkAutoStepForward.TabIndex = 6;
            chkAutoStepForward.Text = "Move forward";
            //
            // txtAutoStep
            //
            txtAutoStep.Location = new Point(134, 92);
            txtAutoStep.Name = "txtAutoStep";
            txtAutoStep.Size = new Size(39, 22);
            txtAutoStep.TabIndex = 4;
            txtAutoStep.Text = "150";
            txtAutoStep.TextChanged += new EventHandler(txtAutoStep_TextChanged);
            txtAutoStep.Validating += new CancelEventHandler(txtAutoStep_Validating);
            //
            // lblAutoStep
            //
            lblAutoStep.Location = new Point(182, 92);
            lblAutoStep.Name = "lblAutoStep";
            lblAutoStep.Size = new Size(77, 19);
            lblAutoStep.TabIndex = 5;
            //
            // cmdAutoStep
            //
            cmdAutoStep.Location = new Point(10, 92);
            cmdAutoStep.Name = "cmdAutoStep";
            cmdAutoStep.Size = new Size(105, 28);
            cmdAutoStep.TabIndex = 2;
            cmdAutoStep.Text = "&Auto Step";
            cmdAutoStep.Click += new EventHandler(cmdAutoStep_Click);
            //
            // cmdPrevious
            //
            cmdPrevious.Location = new Point(10, 28);
            cmdPrevious.Name = "cmdPrevious";
            cmdPrevious.Size = new Size(105, 27);
            cmdPrevious.TabIndex = 0;
            cmdPrevious.Text = "&Previous";
            cmdPrevious.Click += new EventHandler(cmdPrevious_Click);
            //
            // cmdNext
            //
            cmdNext.Location = new Point(10, 55);
            cmdNext.Name = "cmdNext";
            cmdNext.Size = new Size(105, 28);
            cmdNext.TabIndex = 1;
            cmdNext.Text = "&Next";
            cmdNext.Click += new EventHandler(cmdNext_Click);
            //
            // cmdJump
            //
            cmdJump.Location = new Point(134, 28);
            cmdJump.Name = "cmdJump";
            cmdJump.Size = new Size(116, 27);
            cmdJump.TabIndex = 3;
            cmdJump.Text = "&Jump to Scan";
            cmdJump.Click += new EventHandler(cmdJump_Click);
            //
            // txtFilterByMZ
            //
            txtFilterByMZ.Location = new Point(173, 185);
            txtFilterByMZ.Name = "txtFilterByMZ";
            txtFilterByMZ.Size = new Size(67, 22);
            txtFilterByMZ.TabIndex = 14;
            txtFilterByMZ.Text = "543";
            txtFilterByMZ.Leave += new EventHandler(txtFilterByMZ_Leave);
            txtFilterByMZ.Validating += new CancelEventHandler(txtFilterByMZ_Validating);
            //
            // lblFilterByMZ
            //
            lblFilterByMZ.Location = new Point(240, 189);
            lblFilterByMZ.Name = "lblFilterByMZ";
            lblFilterByMZ.Size = new Size(29, 19);
            lblFilterByMZ.TabIndex = 15;
            lblFilterByMZ.Text = "m/z";
            //
            // chkFilterByMZ
            //
            chkFilterByMZ.Location = new Point(10, 185);
            chkFilterByMZ.Name = "chkFilterByMZ";
            chkFilterByMZ.Size = new Size(115, 18);
            chkFilterByMZ.TabIndex = 13;
            chkFilterByMZ.Text = "Filter by m/z";
            chkFilterByMZ.CheckedChanged += new EventHandler(chkFilterByMZ_CheckedChanged);
            //
            // txtFilterByMZTol
            //
            txtFilterByMZTol.Location = new Point(192, 212);
            txtFilterByMZTol.Name = "txtFilterByMZTol";
            txtFilterByMZTol.Size = new Size(48, 22);
            txtFilterByMZTol.TabIndex = 17;
            txtFilterByMZTol.Text = "0.2";
            txtFilterByMZTol.Leave += new EventHandler(txtFilterByMZTol_Leave);
            txtFilterByMZTol.Validating += new CancelEventHandler(txtFilterByMZTol_Validating);
            //
            // lblFilterByMZTolUnits
            //
            lblFilterByMZTolUnits.Location = new Point(240, 217);
            lblFilterByMZTolUnits.Name = "lblFilterByMZTolUnits";
            lblFilterByMZTolUnits.Size = new Size(38, 18);
            lblFilterByMZTolUnits.TabIndex = 18;
            lblFilterByMZTolUnits.Text = "m/z";
            //
            // lblFilterByMZTol
            //
            lblFilterByMZTol.Location = new Point(163, 217);
            lblFilterByMZTol.Name = "lblFilterByMZTol";
            lblFilterByMZTol.Size = new Size(19, 18);
            lblFilterByMZTol.TabIndex = 16;
            lblFilterByMZTol.Text = "±";
            lblFilterByMZTol.TextAlign = ContentAlignment.MiddleRight;
            //
            // txtFixYRange
            //
            txtFixYRange.Location = new Point(154, 102);
            txtFixYRange.Name = "txtFixYRange";
            txtFixYRange.Size = new Size(86, 22);
            txtFixYRange.TabIndex = 6;
            txtFixYRange.Text = "1E6";
            txtFixYRange.TextChanged += new EventHandler(txtFixYRange_TextChanged);
            txtFixYRange.Validating += new CancelEventHandler(txtFixYRange_Validating);
            //
            // lblFixYRange
            //
            lblFixYRange.Location = new Point(240, 106);
            lblFixYRange.Name = "lblFixYRange";
            lblFixYRange.Size = new Size(55, 19);
            lblFixYRange.TabIndex = 7;
            lblFixYRange.Text = "counts";
            //
            // chkFixYRange
            //
            chkFixYRange.Checked = true;
            chkFixYRange.CheckState = CheckState.Checked;
            chkFixYRange.Location = new Point(10, 102);
            chkFixYRange.Name = "chkFixYRange";
            chkFixYRange.Size = new Size(105, 18);
            chkFixYRange.TabIndex = 5;
            chkFixYRange.Text = "Fix Y Range";
            chkFixYRange.CheckedChanged += new EventHandler(chkFixYRange_CheckedChanged);
            //
            // lblSICsTypeFilter
            //
            lblSICsTypeFilter.Location = new Point(10, 9);
            lblSICsTypeFilter.Name = "lblSICsTypeFilter";
            lblSICsTypeFilter.Size = new Size(182, 19);
            lblSICsTypeFilter.TabIndex = 0;
            lblSICsTypeFilter.Text = "SIC Type Filter";
            //
            // cboSICsTypeFilter
            //
            cboSICsTypeFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSICsTypeFilter.Location = new Point(10, 28);
            cboSICsTypeFilter.Name = "cboSICsTypeFilter";
            cboSICsTypeFilter.Size = new Size(278, 24);
            cboSICsTypeFilter.TabIndex = 1;
            cboSICsTypeFilter.SelectedIndexChanged += new EventHandler(cboSICsTypeFilter_SelectedIndexChanged);
            //
            // txtStats1
            //
            txtStats1.Location = new Point(10, 92);
            txtStats1.Multiline = true;
            txtStats1.Name = "txtStats1";
            txtStats1.ReadOnly = true;
            txtStats1.Size = new Size(158, 102);
            txtStats1.TabIndex = 4;
            txtStats1.KeyPress += new KeyPressEventHandler(txtStats1_KeyPress);
            //
            // MainMenuControl
            //
            MainMenuControl.MenuItems.AddRange(new MenuItem[] { mnuFile, mnuEdit, mnuHelp });
            //
            // mnuFile
            //
            mnuFile.Index = 0;
            mnuFile.MenuItems.AddRange(new MenuItem[] { mnuFileSelectMASICInputFile, mnuFileSelectMSMSSearchResultsFile, mnuFileSep1, mnuFileExit });
            mnuFile.Text = "&File";
            //
            // mnuFileSelectMASICInputFile
            //
            mnuFileSelectMASICInputFile.Index = 0;
            mnuFileSelectMASICInputFile.Text = "&Select MASIC Input File";
            mnuFileSelectMASICInputFile.Click += new EventHandler(mnuFileSelectMASICInputFile_Click);
            //
            // mnuFileSelectMSMSSearchResultsFile
            //
            mnuFileSelectMSMSSearchResultsFile.Index = 1;
            mnuFileSelectMSMSSearchResultsFile.Text = "Select MS/MS Search &Results File (Syn or FHT)";
            mnuFileSelectMSMSSearchResultsFile.Click += new EventHandler(mnuFileSelectMSMSSearchResultsFile_Click);
            //
            // mnuFileSep1
            //
            mnuFileSep1.Index = 2;
            mnuFileSep1.Text = "-";
            //
            // mnuFileExit
            //
            mnuFileExit.Index = 3;
            mnuFileExit.Text = "E&xit";
            mnuFileExit.Click += new EventHandler(mnuFileExit_Click);
            //
            // mnuEdit
            //
            mnuEdit.Index = 1;
            mnuEdit.MenuItems.AddRange(new MenuItem[] { mnuEditShowOptimalPeakApexCursor });
            mnuEdit.Text = "&Edit";
            //
            // mnuEditShowOptimalPeakApexCursor
            //
            mnuEditShowOptimalPeakApexCursor.Checked = true;
            mnuEditShowOptimalPeakApexCursor.Index = 0;
            mnuEditShowOptimalPeakApexCursor.Text = "&Show optimal peak apex cursor";
            mnuEditShowOptimalPeakApexCursor.Visible = false;
            mnuEditShowOptimalPeakApexCursor.Click += new EventHandler(mnuEditShowOptimalPeakApexCursor_Click);
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
            // chkSortDescending
            //
            chkSortDescending.Location = new Point(192, 9);
            chkSortDescending.Name = "chkSortDescending";
            chkSortDescending.Size = new Size(134, 19);
            chkSortDescending.TabIndex = 2;
            chkSortDescending.Text = "Sort Descending";
            chkSortDescending.CheckedChanged += new EventHandler(chkSortDescending_CheckedChanged);
            //
            // lstParentIonData
            //
            lstParentIonData.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            lstParentIonData.ItemHeight = 16;
            lstParentIonData.Location = new Point(10, 28);
            lstParentIonData.Name = "lstParentIonData";
            lstParentIonData.Size = new Size(326, 100);
            lstParentIonData.TabIndex = 3;
            lstParentIonData.SelectedIndexChanged += new EventHandler(lstParentIonData_SelectedIndexChanged);
            //
            // txtMinimumSignalToNoise
            //
            txtMinimumSignalToNoise.Location = new Point(173, 129);
            txtMinimumSignalToNoise.Name = "txtMinimumSignalToNoise";
            txtMinimumSignalToNoise.Size = new Size(67, 22);
            txtMinimumSignalToNoise.TabIndex = 9;
            txtMinimumSignalToNoise.Text = "3";
            txtMinimumSignalToNoise.Leave += new EventHandler(txtMinimumSignalToNoise_Leave);
            txtMinimumSignalToNoise.Validating += new CancelEventHandler(txtMinimumSignalToNoise_Validating);
            //
            // chkFilterBySignalToNoise
            //
            chkFilterBySignalToNoise.Location = new Point(10, 129);
            chkFilterBySignalToNoise.Name = "chkFilterBySignalToNoise";
            chkFilterBySignalToNoise.Size = new Size(144, 19);
            chkFilterBySignalToNoise.TabIndex = 8;
            chkFilterBySignalToNoise.Text = "Minimum S/N";
            chkFilterBySignalToNoise.CheckedChanged += new EventHandler(chkFilterBySignalToNoise_CheckedChanged);
            //
            // fraResmoothingOptions
            //
            fraResmoothingOptions.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            fraResmoothingOptions.Controls.Add(chkShowSmoothedData);
            fraResmoothingOptions.Controls.Add(txtPeakWidthPointsMinimum);
            fraResmoothingOptions.Controls.Add(lblPeakWidthPointsMinimum);
            fraResmoothingOptions.Controls.Add(optDoNotResmooth);
            fraResmoothingOptions.Controls.Add(optUseSavitzkyGolaySmooth);
            fraResmoothingOptions.Controls.Add(txtButterworthSamplingFrequency);
            fraResmoothingOptions.Controls.Add(lblButterworthSamplingFrequency);
            fraResmoothingOptions.Controls.Add(txtSavitzkyGolayFilterOrder);
            fraResmoothingOptions.Controls.Add(lblSavitzkyGolayFilterOrder);
            fraResmoothingOptions.Controls.Add(optUseButterworthSmooth);
            fraResmoothingOptions.Location = new Point(10, 9);
            fraResmoothingOptions.Name = "fraResmoothingOptions";
            fraResmoothingOptions.Size = new Size(384, 157);
            fraResmoothingOptions.TabIndex = 7;
            fraResmoothingOptions.TabStop = false;
            fraResmoothingOptions.Text = "Smoothing Options";
            //
            // chkShowSmoothedData
            //
            chkShowSmoothedData.Checked = true;
            chkShowSmoothedData.CheckState = CheckState.Checked;
            chkShowSmoothedData.Location = new Point(19, 18);
            chkShowSmoothedData.Name = "chkShowSmoothedData";
            chkShowSmoothedData.Size = new Size(183, 19);
            chkShowSmoothedData.TabIndex = 0;
            chkShowSmoothedData.Text = "Show Smoothed Data";
            //
            // txtPeakWidthPointsMinimum
            //
            txtPeakWidthPointsMinimum.Location = new Point(317, 120);
            txtPeakWidthPointsMinimum.Name = "txtPeakWidthPointsMinimum";
            txtPeakWidthPointsMinimum.Size = new Size(57, 22);
            txtPeakWidthPointsMinimum.TabIndex = 9;
            txtPeakWidthPointsMinimum.Text = "6";
            txtPeakWidthPointsMinimum.TextChanged += new EventHandler(txtPeakWidthPointsMinimum_TextChanged);
            txtPeakWidthPointsMinimum.Validating += new CancelEventHandler(txtPeakWidthPointsMinimum_Validating);
            //
            // lblPeakWidthPointsMinimum
            //
            lblPeakWidthPointsMinimum.Location = new Point(115, 129);
            lblPeakWidthPointsMinimum.Name = "lblPeakWidthPointsMinimum";
            lblPeakWidthPointsMinimum.Size = new Size(192, 19);
            lblPeakWidthPointsMinimum.TabIndex = 8;
            lblPeakWidthPointsMinimum.Text = "Minimum Peak Width (points)";
            //
            // optDoNotResmooth
            //
            optDoNotResmooth.Checked = true;
            optDoNotResmooth.Location = new Point(19, 55);
            optDoNotResmooth.Name = "optDoNotResmooth";
            optDoNotResmooth.Size = new Size(240, 19);
            optDoNotResmooth.TabIndex = 1;
            optDoNotResmooth.TabStop = true;
            optDoNotResmooth.Text = "Do Not Show Smoothed Data";
            optDoNotResmooth.CheckedChanged += new EventHandler(optDoNotResmooth_CheckedChanged);
            //
            // optUseSavitzkyGolaySmooth
            //
            optUseSavitzkyGolaySmooth.Location = new Point(19, 92);
            optUseSavitzkyGolaySmooth.Name = "optUseSavitzkyGolaySmooth";
            optUseSavitzkyGolaySmooth.Size = new Size(202, 19);
            optUseSavitzkyGolaySmooth.TabIndex = 5;
            optUseSavitzkyGolaySmooth.Text = "Use Savitzky Golay Smooth";
            optUseSavitzkyGolaySmooth.CheckedChanged += new EventHandler(optUseSavitzkyGolaySmooth_CheckedChanged);
            //
            // txtButterworthSamplingFrequency
            //
            txtButterworthSamplingFrequency.Location = new Point(317, 65);
            txtButterworthSamplingFrequency.Name = "txtButterworthSamplingFrequency";
            txtButterworthSamplingFrequency.Size = new Size(57, 22);
            txtButterworthSamplingFrequency.TabIndex = 4;
            txtButterworthSamplingFrequency.Text = "0.25";
            txtButterworthSamplingFrequency.TextChanged += new EventHandler(txtButterworthSamplingFrequency_TextChanged);
            txtButterworthSamplingFrequency.Validating += new CancelEventHandler(txtButterworthSamplingFrequency_Validating);
            //
            // lblButterworthSamplingFrequency
            //
            lblButterworthSamplingFrequency.Location = new Point(221, 74);
            lblButterworthSamplingFrequency.Name = "lblButterworthSamplingFrequency";
            lblButterworthSamplingFrequency.Size = new Size(86, 18);
            lblButterworthSamplingFrequency.TabIndex = 3;
            lblButterworthSamplingFrequency.Text = "Filter Order";
            //
            // txtSavitzkyGolayFilterOrder
            //
            txtSavitzkyGolayFilterOrder.Location = new Point(317, 92);
            txtSavitzkyGolayFilterOrder.Name = "txtSavitzkyGolayFilterOrder";
            txtSavitzkyGolayFilterOrder.Size = new Size(57, 22);
            txtSavitzkyGolayFilterOrder.TabIndex = 7;
            txtSavitzkyGolayFilterOrder.Text = "0";
            txtSavitzkyGolayFilterOrder.TextChanged += new EventHandler(txtSavitzkyGolayFilterOrder_TextChanged);
            txtSavitzkyGolayFilterOrder.Validating += new CancelEventHandler(txtSavitzkyGolayFilterOrder_Validating);
            //
            // lblSavitzkyGolayFilterOrder
            //
            lblSavitzkyGolayFilterOrder.Location = new Point(221, 102);
            lblSavitzkyGolayFilterOrder.Name = "lblSavitzkyGolayFilterOrder";
            lblSavitzkyGolayFilterOrder.Size = new Size(86, 18);
            lblSavitzkyGolayFilterOrder.TabIndex = 6;
            lblSavitzkyGolayFilterOrder.Text = "Filter Order";
            //
            // optUseButterworthSmooth
            //
            optUseButterworthSmooth.Location = new Point(19, 74);
            optUseButterworthSmooth.Name = "optUseButterworthSmooth";
            optUseButterworthSmooth.Size = new Size(202, 18);
            optUseButterworthSmooth.TabIndex = 2;
            optUseButterworthSmooth.Text = "Use Butterworth Smooth";
            optUseButterworthSmooth.CheckedChanged += new EventHandler(optUseButterworthSmooth_CheckedChanged);
            //
            // fraPeakFinder
            //
            fraPeakFinder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            fraPeakFinder.Controls.Add(cmdRedoSICPeakFindingAllData);
            fraPeakFinder.Controls.Add(chkUsePeakFinder);
            fraPeakFinder.Controls.Add(chkFindPeaksSubtractBaseline);
            fraPeakFinder.Location = new Point(403, 9);
            fraPeakFinder.Name = "fraPeakFinder";
            fraPeakFinder.Size = new Size(240, 148);
            fraPeakFinder.TabIndex = 8;
            fraPeakFinder.TabStop = false;
            fraPeakFinder.Text = "SIC Peak Finding";
            //
            // cmdRedoSICPeakFindingAllData
            //
            cmdRedoSICPeakFindingAllData.Location = new Point(19, 92);
            cmdRedoSICPeakFindingAllData.Name = "cmdRedoSICPeakFindingAllData";
            cmdRedoSICPeakFindingAllData.Size = new Size(135, 46);
            cmdRedoSICPeakFindingAllData.TabIndex = 20;
            cmdRedoSICPeakFindingAllData.Text = "Redo SIC Peak Finding For All Data";
            cmdRedoSICPeakFindingAllData.Click += new EventHandler(cmdRedoSICPeakFindingAllData_Click);
            //
            // chkUsePeakFinder
            //
            chkUsePeakFinder.Location = new Point(10, 18);
            chkUsePeakFinder.Name = "chkUsePeakFinder";
            chkUsePeakFinder.Size = new Size(220, 19);
            chkUsePeakFinder.TabIndex = 18;
            chkUsePeakFinder.Text = "Recompute SIC Peak Stats";
            chkUsePeakFinder.CheckedChanged += new EventHandler(chkUsePeakFinder_CheckedChanged);
            //
            // chkFindPeaksSubtractBaseline
            //
            chkFindPeaksSubtractBaseline.Checked = true;
            chkFindPeaksSubtractBaseline.CheckState = CheckState.Checked;
            chkFindPeaksSubtractBaseline.Location = new Point(19, 38);
            chkFindPeaksSubtractBaseline.Name = "chkFindPeaksSubtractBaseline";
            chkFindPeaksSubtractBaseline.Size = new Size(211, 37);
            chkFindPeaksSubtractBaseline.TabIndex = 19;
            chkFindPeaksSubtractBaseline.Text = "Subtract baseline when computing Intensity and Area";
            //
            // fraSortOrderAndStats
            //
            fraSortOrderAndStats.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            fraSortOrderAndStats.Controls.Add(chkShowBaselineCorrectedStats);
            fraSortOrderAndStats.Controls.Add(txtStats2);
            fraSortOrderAndStats.Controls.Add(txtStats3);
            fraSortOrderAndStats.Controls.Add(cboSortOrder);
            fraSortOrderAndStats.Controls.Add(lblSortOrder);
            fraSortOrderAndStats.Controls.Add(chkSortDescending);
            fraSortOrderAndStats.Controls.Add(txtStats1);
            fraSortOrderAndStats.Location = new Point(0, 141);
            fraSortOrderAndStats.Name = "fraSortOrderAndStats";
            fraSortOrderAndStats.Size = new Size(338, 259);
            fraSortOrderAndStats.TabIndex = 5;
            fraSortOrderAndStats.TabStop = false;
            //
            // chkShowBaselineCorrectedStats
            //
            chkShowBaselineCorrectedStats.Checked = true;
            chkShowBaselineCorrectedStats.CheckState = CheckState.Checked;
            chkShowBaselineCorrectedStats.Location = new Point(10, 67);
            chkShowBaselineCorrectedStats.Name = "chkShowBaselineCorrectedStats";
            chkShowBaselineCorrectedStats.Size = new Size(259, 18);
            chkShowBaselineCorrectedStats.TabIndex = 14;
            chkShowBaselineCorrectedStats.Text = "Show Baseline Corrected Stats";
            chkShowBaselineCorrectedStats.CheckedChanged += new EventHandler(chkShowBaselineCorrectedStats_CheckedChanged);
            //
            // txtStats2
            //
            txtStats2.Location = new Point(173, 92);
            txtStats2.Multiline = true;
            txtStats2.Name = "txtStats2";
            txtStats2.ReadOnly = true;
            txtStats2.Size = new Size(158, 102);
            txtStats2.TabIndex = 6;
            txtStats2.KeyPress += new KeyPressEventHandler(txtStats2_KeyPress);
            //
            // txtStats3
            //
            txtStats3.Location = new Point(10, 203);
            txtStats3.Multiline = true;
            txtStats3.Name = "txtStats3";
            txtStats3.ReadOnly = true;
            txtStats3.ScrollBars = ScrollBars.Vertical;
            txtStats3.Size = new Size(319, 46);
            txtStats3.TabIndex = 5;
            txtStats3.KeyPress += new KeyPressEventHandler(txStats3_KeyPress);
            //
            // TabControl1
            //
            TabControl1.Controls.Add(tpSICFilters);
            TabControl1.Controls.Add(tpMsMsSearchResultsFilters);
            TabControl1.Location = new Point(19, 185);
            TabControl1.Name = "TabControl1";
            TabControl1.SelectedIndex = 0;
            TabControl1.Size = new Size(307, 277);
            TabControl1.TabIndex = 6;
            //
            // tpSICFilters
            //
            tpSICFilters.Controls.Add(chkFilterByMZ);
            tpSICFilters.Controls.Add(txtFilterByMZTol);
            tpSICFilters.Controls.Add(lblFilterByMZTolUnits);
            tpSICFilters.Controls.Add(lblFilterByMZTol);
            tpSICFilters.Controls.Add(txtFixYRange);
            tpSICFilters.Controls.Add(lblFixYRange);
            tpSICFilters.Controls.Add(chkFixYRange);
            tpSICFilters.Controls.Add(lblSICsTypeFilter);
            tpSICFilters.Controls.Add(cboSICsTypeFilter);
            tpSICFilters.Controls.Add(chkFixXRange);
            tpSICFilters.Controls.Add(txtFixXRange);
            tpSICFilters.Controls.Add(lblFixXRange);
            tpSICFilters.Controls.Add(lblMinimumIntensity);
            tpSICFilters.Controls.Add(txtMinimumIntensity);
            tpSICFilters.Controls.Add(chkFilterByIntensity);
            tpSICFilters.Controls.Add(txtMinimumSignalToNoise);
            tpSICFilters.Controls.Add(chkFilterBySignalToNoise);
            tpSICFilters.Controls.Add(txtFilterByMZ);
            tpSICFilters.Controls.Add(lblFilterByMZ);
            tpSICFilters.Location = new Point(4, 25);
            tpSICFilters.Name = "tpSICFilters";
            tpSICFilters.Size = new Size(299, 248);
            tpSICFilters.TabIndex = 0;
            tpSICFilters.Text = "SIC Filters";
            //
            // tpMsMsSearchResultsFilters
            //
            tpMsMsSearchResultsFilters.Controls.Add(chkSequenceFilterExactMatch);
            tpMsMsSearchResultsFilters.Controls.Add(lblSequenceFilter);
            tpMsMsSearchResultsFilters.Controls.Add(lblChargeFilter);
            tpMsMsSearchResultsFilters.Controls.Add(TextBox2);
            tpMsMsSearchResultsFilters.Controls.Add(lblMinimumXCorr);
            tpMsMsSearchResultsFilters.Controls.Add(txtSequenceFilter);
            tpMsMsSearchResultsFilters.Controls.Add(txtMinimumXCorr);
            tpMsMsSearchResultsFilters.Location = new Point(4, 25);
            tpMsMsSearchResultsFilters.Name = "tpMsMsSearchResultsFilters";
            tpMsMsSearchResultsFilters.Size = new Size(299, 248);
            tpMsMsSearchResultsFilters.TabIndex = 1;
            tpMsMsSearchResultsFilters.Text = "MS/MS Results Filters";
            //
            // chkSequenceFilterExactMatch
            //
            chkSequenceFilterExactMatch.Checked = true;
            chkSequenceFilterExactMatch.CheckState = CheckState.Checked;
            chkSequenceFilterExactMatch.Enabled = false;
            chkSequenceFilterExactMatch.Location = new Point(173, 83);
            chkSequenceFilterExactMatch.Name = "chkSequenceFilterExactMatch";
            chkSequenceFilterExactMatch.Size = new Size(108, 19);
            chkSequenceFilterExactMatch.TabIndex = 5;
            chkSequenceFilterExactMatch.Text = "Exact Match?";
            //
            // lblSequenceFilter
            //
            lblSequenceFilter.Location = new Point(10, 83);
            lblSequenceFilter.Name = "lblSequenceFilter";
            lblSequenceFilter.Size = new Size(115, 19);
            lblSequenceFilter.TabIndex = 4;
            lblSequenceFilter.Text = "Sequence Filter";
            //
            // lblChargeFilter
            //
            lblChargeFilter.Location = new Point(10, 46);
            lblChargeFilter.Name = "lblChargeFilter";
            lblChargeFilter.Size = new Size(115, 19);
            lblChargeFilter.TabIndex = 2;
            lblChargeFilter.Text = "Charge Filter";
            //
            // TextBox2
            //
            TextBox2.Enabled = false;
            TextBox2.Location = new Point(173, 46);
            TextBox2.Name = "TextBox2";
            TextBox2.Size = new Size(67, 22);
            TextBox2.TabIndex = 3;
            TextBox2.Text = "0";
            //
            // lblMinimumXCorr
            //
            lblMinimumXCorr.Location = new Point(10, 18);
            lblMinimumXCorr.Name = "lblMinimumXCorr";
            lblMinimumXCorr.Size = new Size(115, 19);
            lblMinimumXCorr.TabIndex = 0;
            lblMinimumXCorr.Text = "XCorr Minimum";
            //
            // txtSequenceFilter
            //
            txtSequenceFilter.Enabled = false;
            txtSequenceFilter.Location = new Point(10, 102);
            txtSequenceFilter.Name = "txtSequenceFilter";
            txtSequenceFilter.Size = new Size(276, 22);
            txtSequenceFilter.TabIndex = 6;
            //
            // txtMinimumXCorr
            //
            txtMinimumXCorr.Enabled = false;
            txtMinimumXCorr.Location = new Point(173, 18);
            txtMinimumXCorr.Name = "txtMinimumXCorr";
            txtMinimumXCorr.Size = new Size(67, 22);
            txtMinimumXCorr.TabIndex = 1;
            txtMinimumXCorr.Text = "2.2";
            //
            // pnlInputFile
            //
            pnlInputFile.Controls.Add(txtDataFilePath);
            pnlInputFile.Controls.Add(cmdSelectFile);
            pnlInputFile.Dock = DockStyle.Top;
            pnlInputFile.Location = new Point(0, 0);
            pnlInputFile.Name = "pnlInputFile";
            pnlInputFile.Size = new Size(711, 46);
            pnlInputFile.TabIndex = 9;
            //
            // pnlSICs
            //
            pnlSICs.Controls.Add(lblParentIon);
            pnlSICs.Controls.Add(lstParentIonData);
            pnlSICs.Controls.Add(fraSortOrderAndStats);
            pnlSICs.Dock = DockStyle.Left;
            pnlSICs.Location = new Point(0, 46);
            pnlSICs.Name = "pnlSICs";
            pnlSICs.Size = new Size(346, 411);
            pnlSICs.TabIndex = 10;
            //
            // pnlNavigationAndOptions
            //
            pnlNavigationAndOptions.Controls.Add(fraNavigation);
            pnlNavigationAndOptions.Controls.Add(TabControl1);
            pnlNavigationAndOptions.Dock = DockStyle.Left;
            pnlNavigationAndOptions.Location = new Point(346, 46);
            pnlNavigationAndOptions.Name = "pnlNavigationAndOptions";
            pnlNavigationAndOptions.Size = new Size(336, 411);
            pnlNavigationAndOptions.TabIndex = 11;
            //
            // pnlBottom
            //
            pnlBottom.Controls.Add(fraPeakFinder);
            pnlBottom.Controls.Add(fraResmoothingOptions);
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Location = new Point(0, 457);
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Size = new Size(711, 175);
            pnlBottom.TabIndex = 12;
            //
            // frmBrowser
            //
            AutoScaleBaseSize = new Size(6, 15);
            ClientSize = new Size(711, 632);
            Controls.Add(pnlNavigationAndOptions);
            Controls.Add(pnlSICs);
            Controls.Add(pnlInputFile);
            Controls.Add(pnlBottom);
            Menu = MainMenuControl;
            MinimumSize = new Size(614, 0);
            Name = "frmBrowser";
            Text = "MASIC Browser";
            Closing += frmBrowser_Closing;
            Load += frmBrowser_Load;
            Resize += frmBrowser_Resize;
            ((ISupportInitialize)tmrAutoStep).EndInit();
            fraNavigation.ResumeLayout(false);
            fraNavigation.PerformLayout();
            fraResmoothingOptions.ResumeLayout(false);
            fraResmoothingOptions.PerformLayout();
            fraPeakFinder.ResumeLayout(false);
            fraSortOrderAndStats.ResumeLayout(false);
            fraSortOrderAndStats.PerformLayout();
            TabControl1.ResumeLayout(false);
            tpSICFilters.ResumeLayout(false);
            tpSICFilters.PerformLayout();
            tpMsMsSearchResultsFilters.ResumeLayout(false);
            tpMsMsSearchResultsFilters.PerformLayout();
            pnlInputFile.ResumeLayout(false);
            pnlInputFile.PerformLayout();
            pnlSICs.ResumeLayout(false);
            pnlNavigationAndOptions.ResumeLayout(false);
            pnlBottom.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
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
