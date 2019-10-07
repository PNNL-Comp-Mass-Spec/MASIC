Option Strict On

' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started October 11, 2003
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

' E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
' Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
' -------------------------------------------------------------------------------
'
' Licensed under the 2-Clause BSD License; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at
' https://opensource.org/licenses/BSD-2-Clause


Imports System.ComponentModel
Imports System.Text
Imports System.Threading
Imports MASIC.DataInput
Imports MASICPeakFinder.clsMASICPeakFinder
Imports PRISM
Imports PRISM.FileProcessor
Imports PRISMWin
Imports PRISMWin.TextBoxUtils
Imports ProgressFormNET
Imports ShFolderBrowser.FolderBrowser

Public Class frmMain

    Public Sub New()
        MyBase.New()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call
        InitializeControls()

        mCacheOptions = New clsSpectrumCacheOptions()
        mLogMessages = New List(Of String)

        mMasic = New clsMASIC()
        RegisterEvents(mMasic)

    End Sub

#Region "Constants and Enums"

    Private Const XML_SETTINGS_FILE_NAME As String = "MASICParameters.xml"

    Private Const CUSTOM_SIC_VALUES_DATA_TABLE As String = "PeakMatchingThresholds"

    Private Const COL_NAME_MZ As String = "MZ"
    Private Const COL_NAME_MZ_TOLERANCE As String = "MZToleranceDa"
    Private Const COL_NAME_SCAN_CENTER As String = "Scan_Center"
    Private Const COL_NAME_SCAN_TOLERANCE As String = "Scan_Tolerance"
    Private Const COL_NAME_SCAN_COMMENT As String = "Scan_Comment"
    Private Const COL_NAME_CUSTOM_SIC_VALUE_ROW_ID As String = "UniqueRowID"

#End Region

#Region "Structures"

    Private Structure udtCustomSICEntryType
        Public MZ As Double
        Public ScanCenter As Single
        Public Comment As String
    End Structure

#End Region

#Region "Classwide Variables"

    Private mCustomSICValuesDataset As DataSet

    Private mDefaultCustomSICList() As udtCustomSICEntryType
    Private mWorking As Boolean

    Private mXmlSettingsFilePath As String
    Private mPreferredInputFileExtension As String

    Private ReadOnly mCacheOptions As clsSpectrumCacheOptions

    Private mSuppressNoParentIonsError As Boolean
    Private mCompressMSSpectraData As Boolean
    Private mCompressMSMSSpectraData As Boolean

    Private mCompressToleranceDivisorForDa As Double
    Private mCompressToleranceDivisorForPPM As Double

    Private mHeightAdjustForce As Integer
    Private mHeightAdjustTime As DateTime

    Private mMasic As clsMASIC
    Private mProgressForm As frmProgress

    ''' <summary>
    ''' Log messages, including warnings and errors, with the newest message at the top
    ''' </summary>
    Private ReadOnly mLogMessages As List(Of String)

#End Region

#Region "Procedures"

    Private Sub AddCustomSICRow(
      mz As Double,
      mzToleranceDa As Double,
      scanOrAcqTimeCenter As Single,
      scanOrAcqTimeTolerance As Single,
      comment As String,
      Optional ByRef existingRowFound As Boolean = False)

        Dim myDataRow As DataRow

        With mCustomSICValuesDataset.Tables(CUSTOM_SIC_VALUES_DATA_TABLE)

            For Each myDataRow In .Rows
                With myDataRow
                    If Math.Abs(CDbl(.Item(0)) - mz) < Single.Epsilon And Math.Abs(CSng(.Item(1)) - scanOrAcqTimeCenter) < Single.Epsilon Then
                        existingRowFound = True
                        Exit For
                    End If
                End With
            Next myDataRow

            If comment Is Nothing Then comment = String.Empty

            If Not existingRowFound Then
                myDataRow = .NewRow
                myDataRow(0) = Math.Round(mz, 4)
                myDataRow(1) = Math.Round(mzToleranceDa, 4)
                myDataRow(2) = Math.Round(scanOrAcqTimeCenter, 6)
                myDataRow(3) = Math.Round(scanOrAcqTimeTolerance, 6)
                myDataRow(4) = comment
                .Rows.Add(myDataRow)
            End If
        End With

    End Sub

    Private Sub AppendToLog(messageType As EventLogEntryType, message As String)

        If message.StartsWith("ProcessingStats") OrElse message.StartsWith("Parameter file not specified") Then
            Return
        End If

        Dim textToAppend As String
        Dim doEvents = False

        Select Case messageType
            Case EventLogEntryType.Error
                textToAppend = "Error: " & message
                tbsOptions.SelectTab(tbsOptions.TabCount - 1)
                doEvents = True
            Case EventLogEntryType.Warning
                textToAppend = "Warning: " & message
                tbsOptions.SelectTab(tbsOptions.TabCount - 1)
                doEvents = True
            Case Else
                ' Includes Case EventLogEntryType.Information
                textToAppend = message
        End Select

        mLogMessages.Insert(0, textToAppend)

        txtLogMessages.AppendText(textToAppend & ControlChars.NewLine)
        txtLogMessages.ScrollToCaret()

        If doEvents Then
            Application.DoEvents()
        End If

    End Sub

    Private Sub AutoPopulateCustomSICValues(confirmReplaceExistingResults As Boolean)

        Dim index As Integer

        Dim defaultMZTolerance As Double
        Dim defaultScanOrAcqTimeTolerance As Single

        GetCurrentCustomSICTolerances(defaultMZTolerance, defaultScanOrAcqTimeTolerance)
        If defaultScanOrAcqTimeTolerance > 1 Then
            defaultScanOrAcqTimeTolerance = 0.6
        End If

        If ClearCustomSICList(confirmReplaceExistingResults) Then
            ' The default values use relative times, so make sure that mode is enabled
            SetCustomSICToleranceType(clsCustomSICList.eCustomSICScanTypeConstants.Relative)

            txtCustomSICScanOrAcqTimeTolerance.Text = defaultScanOrAcqTimeTolerance.ToString

            For index = 0 To mDefaultCustomSICList.Length - 1
                With mDefaultCustomSICList(index)
                    AddCustomSICRow(.MZ, defaultMZTolerance, .ScanCenter, defaultScanOrAcqTimeTolerance, .Comment)
                End With
            Next

        End If

    End Sub

    Private Sub CatchUnrequestedHeightChange()
        Static updating As Boolean

        If Not updating Then
            If mHeightAdjustForce <> 0 AndAlso DateTime.UtcNow.Subtract(mHeightAdjustTime).TotalSeconds <= 5 Then
                Try
                    updating = True
                    Me.Height = mHeightAdjustForce
                    mHeightAdjustForce = 0
                    mHeightAdjustTime = #1/1/1900#
                Catch ex As Exception
                Finally
                    updating = False
                End Try
            End If
        End If
    End Sub

    Private Sub AutoToggleReporterIonStatsEnabled()
        If cboReporterIonMassMode.SelectedIndex = clsReporterIons.eReporterIonMassModeConstants.CustomOrNone Then
            If chkReporterIonStatsEnabled.Checked Then
                chkReporterIonStatsEnabled.Checked = False
            End If
        Else
            If Not chkReporterIonStatsEnabled.Checked Then
                chkReporterIonStatsEnabled.Checked = True
            End If
        End If
    End Sub

    Private Sub AutoToggleReporterIonStatsMode()
        If chkReporterIonStatsEnabled.Checked Then
            If cboReporterIonMassMode.SelectedIndex = clsReporterIons.eReporterIonMassModeConstants.CustomOrNone Then
                cboReporterIonMassMode.SelectedIndex = clsReporterIons.eReporterIonMassModeConstants.ITraqFourMZ
            End If
        Else
            If cboReporterIonMassMode.SelectedIndex <> clsReporterIons.eReporterIonMassModeConstants.CustomOrNone Then
                cboReporterIonMassMode.SelectedIndex = clsReporterIons.eReporterIonMassModeConstants.CustomOrNone
            End If
        End If
    End Sub

    Private Sub ClearAllRangeFilters()
        txtScanStart.Text = "0"
        txtScanEnd.Text = "0"
        txtTimeStart.Text = "0"
        txtTimeEnd.Text = "0"
    End Sub

    Private Function ClearCustomSICList(confirmReplaceExistingResults As Boolean) As Boolean
        ' Returns true if the CUSTOM_SIC_VALUES_DATA_TABLE is empty or if it was cleared
        ' Returns false if the user is queried about clearing and they do not click Yes

        Dim eResult As DialogResult

        If mCustomSICValuesDataset.Tables(CUSTOM_SIC_VALUES_DATA_TABLE).Rows.Count > 0 Then
            If confirmReplaceExistingResults Then
                eResult = MessageBox.Show("Are you sure you want to clear the Custom SIC list?", "Clear Custom SICs", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
            End If

            If eResult = DialogResult.Yes OrElse Not confirmReplaceExistingResults Then
                mCustomSICValuesDataset.Tables(CUSTOM_SIC_VALUES_DATA_TABLE).Rows.Clear()
                Return True
            End If
        Else
            Return True
        End If

        Return False
    End Function

    Private Function ConfirmPaths() As Boolean
        If txtInputFilePath.TextLength = 0 Then
            MessageBox.Show("Please define an input file path", "Missing Value", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            txtInputFilePath.Focus()
            Return False
        ElseIf txtOutputDirectoryPath.TextLength = 0 Then
            MessageBox.Show("Please define an output directory path", "Missing Value", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            txtOutputDirectoryPath.Focus()
            Return False
        Else
            Return True
        End If
    End Function

    Private Function CStrSafe(ByRef item As Object) As String
        Try
            If item Is Nothing Then
                Return String.Empty
            ElseIf Convert.IsDBNull(item) Then
                Return String.Empty
            Else
                Return CStr(item)
            End If
        Catch ex As Exception
            Return String.Empty
        End Try
    End Function

    Private Sub DefineDefaultCustomSICList()

        ReDim mDefaultCustomSICList(8)

        With mDefaultCustomSICList(0)
            .MZ = 824.47422
            .ScanCenter = 0.176
            .Comment = "Pep-09"
        End With

        With mDefaultCustomSICList(1)
            .MZ = 412.74102
            .ScanCenter = 0.176
            .Comment = "Pep-09"
        End With

        With mDefaultCustomSICList(2)
            .MZ = 484.28137
            .ScanCenter = 0.092
            .Comment = "Pep-11"
        End With

        With mDefaultCustomSICList(3)
            .MZ = 459.27687
            .ScanCenter = 0.368
            .Comment = "Pep-14"
        End With

        With mDefaultCustomSICList(4)
            .MZ = 740.01082
            .ScanCenter = 0.574
            .Comment = "Pep-16"
        End With

        With mDefaultCustomSICList(5)
            .MZ = 762.51852
            .ScanCenter = 0.642
            .Comment = "Pep-26"
        End With

        With mDefaultCustomSICList(6)
            .MZ = 657.42992
            .ScanCenter = 0.192
            .Comment = "Pep-16_Partial"
        End With

        With mDefaultCustomSICList(7)
            .MZ = 900.59222
            .ScanCenter = 0.4
            .Comment = "Pep-26_PartialA"
        End With

        With mDefaultCustomSICList(8)
            .MZ = 640.43972
            .ScanCenter = 0.4
            .Comment = "Pep-26_PartialB"
        End With

    End Sub

    Private Sub DefineOverviewText()
        Dim msg = New StringBuilder()

        msg.Append("When Export All Spectra Data Points is enabled, a separate file is created containing the raw data points (scan number, m/z, and intensity), using the specified file format. ")
        msg.Append("If Export MS/MS Spectra is enabled, then the fragmentation spectra are included, in addition to the survey scan spectra (MS1 scans). ")
        msg.Append("If MS/MS spectra are not included, then one can optionally renumber the survey scan spectra so that they increase in steps of 1, regardless of the number of MS/MS scans between each survey scan. ")
        msg.Append("The Minimum Intensity and Maximum Ion Count options allow you to limit the number of data points exported for each spectrum.")

        lblRawDataExportOverview.Text = msg.ToString()

        msg.Clear()
        msg.Append("These options control how the selected ion chromatogram (SIC) is created for each parent ion mass or custom SIC search mass. ")
        msg.Append("The data in the survey scan spectra (MS1 scans) are searched +/- the SIC Tolerance, looking forward and backward in time until ")
        msg.Append("the intensity of the matching data 1) falls below the Intensity Threshold Fraction Max Peak value, 2) falls below the Intensity ")
        msg.Append("Threshold Absolute Minimum, or 3) spans more than the Maximum Peak Width forward or backward limits defined.")

        lblSICOptionsOverview.Text = msg.ToString()


        msg.Clear()
        msg.Append("When processing Thermo-Finnigan MRM data files, a file named _MRMSettings.txt will be created listing the ")
        msg.Append("parent and daughter m/z values monitored via SRM. ")
        msg.Append("You can optionally export detailed MRM intensity data using these options:")
        lblMRMInfo.Text = msg.ToString()


        msg.Clear()
        msg.Append("Select a comma or tab delimited file to read custom SIC search values from, ")
        msg.Append("or define them in the Custom SIC Values table below.  If using the file, ")
        msg.Append("allowed column names are: " & clsCustomSICListReader.GetCustomMZFileColumnHeaders() & ".  ")
        msg.Append("Note: use " &
          clsCustomSICListReader.CUSTOM_SIC_COLUMN_SCAN_TIME & " and " &
          clsCustomSICListReader.CUSTOM_SIC_COLUMN_TIME_TOLERANCE & " only when specifying ")

        msg.Append("acquisition time-based values.  When doing so, do not include " &
          clsCustomSICListReader.CUSTOM_SIC_COLUMN_SCAN_CENTER & " and " &
          clsCustomSICListReader.CUSTOM_SIC_COLUMN_SCAN_TOLERANCE & ".")

        txtCustomSICFileDescription.Text = msg.ToString()

    End Sub

    Private Sub EnableDisableControls()

        Dim rawExportEnabled As Boolean

        Dim createSICsAndRawData As Boolean = Not chkSkipSICAndRawDataProcessing.Checked
        Dim msmsProcessingEnabled As Boolean = Not chkSkipMSMSProcessing.Checked
        Dim exportRawDataOnly As Boolean = chkExportRawDataOnly.Checked AndAlso chkExportRawSpectraData.Checked

        chkSkipMSMSProcessing.Enabled = createSICsAndRawData
        chkExportRawDataOnly.Enabled = createSICsAndRawData AndAlso chkExportRawSpectraData.Checked

        fraExportAllSpectraDataPoints.Enabled = createSICsAndRawData

        fraSICNoiseThresholds.Enabled = createSICsAndRawData AndAlso Not exportRawDataOnly
        fraPeakFindingOptions.Enabled = fraSICNoiseThresholds.Enabled
        fraSmoothingOptions.Enabled = fraSICNoiseThresholds.Enabled
        fraSICSearchThresholds.Enabled = fraSICNoiseThresholds.Enabled

        fraMassSpectraNoiseThresholds.Enabled = createSICsAndRawData

        fraBinningIntensityOptions.Enabled = createSICsAndRawData AndAlso msmsProcessingEnabled AndAlso Not exportRawDataOnly
        fraBinningMZOptions.Enabled = fraBinningIntensityOptions.Enabled
        fraSpectrumSimilarityOptions.Enabled = fraBinningIntensityOptions.Enabled

        fraCustomSICControls.Enabled = createSICsAndRawData AndAlso Not exportRawDataOnly
        dgCustomSICValues.Enabled = createSICsAndRawData AndAlso Not exportRawDataOnly

        rawExportEnabled = chkExportRawSpectraData.Checked

        cboExportRawDataFileFormat.Enabled = rawExportEnabled
        chkExportRawDataIncludeMSMS.Enabled = rawExportEnabled

        If chkExportRawDataIncludeMSMS.Checked Then
            chkExportRawDataRenumberScans.Enabled = False
        Else
            chkExportRawDataRenumberScans.Enabled = rawExportEnabled
        End If

        txtExportRawDataSignalToNoiseRatioMinimum.Enabled = rawExportEnabled
        txtExportRawDataMaxIonCountPerScan.Enabled = rawExportEnabled
        txtExportRawDataIntensityMinimum.Enabled = rawExportEnabled


        If cboSICNoiseThresholdMode.SelectedIndex = eNoiseThresholdModes.AbsoluteThreshold Then
            txtSICNoiseThresholdIntensity.Enabled = True
            txtSICNoiseFractionLowIntensityDataToAverage.Enabled = False
        ElseIf cboSICNoiseThresholdMode.SelectedIndex = eNoiseThresholdModes.TrimmedMeanByAbundance OrElse
         cboSICNoiseThresholdMode.SelectedIndex = eNoiseThresholdModes.TrimmedMeanByCount OrElse
         cboSICNoiseThresholdMode.SelectedIndex = eNoiseThresholdModes.TrimmedMedianByAbundance Then
            txtSICNoiseThresholdIntensity.Enabled = False
            txtSICNoiseFractionLowIntensityDataToAverage.Enabled = True
        Else
            ' Unknown mode; disable both
            txtSICNoiseThresholdIntensity.Enabled = False
            txtSICNoiseFractionLowIntensityDataToAverage.Enabled = False
        End If

        txtButterworthSamplingFrequency.Enabled = optUseButterworthSmooth.Checked
        txtSavitzkyGolayFilterOrder.Enabled = optUseSavitzkyGolaySmooth.Checked

        If cboMassSpectraNoiseThresholdMode.SelectedIndex = eNoiseThresholdModes.AbsoluteThreshold Then
            txtMassSpectraNoiseThresholdIntensity.Enabled = True
            txtMassSpectraNoiseFractionLowIntensityDataToAverage.Enabled = False
            txtMassSpectraNoiseMinimumSignalToNoiseRatio.Enabled = False
        ElseIf cboMassSpectraNoiseThresholdMode.SelectedIndex = eNoiseThresholdModes.TrimmedMeanByAbundance OrElse
          cboMassSpectraNoiseThresholdMode.SelectedIndex = eNoiseThresholdModes.TrimmedMeanByCount OrElse
          cboMassSpectraNoiseThresholdMode.SelectedIndex = eNoiseThresholdModes.TrimmedMedianByAbundance Then
            txtMassSpectraNoiseThresholdIntensity.Enabled = False
            txtMassSpectraNoiseFractionLowIntensityDataToAverage.Enabled = True
            txtMassSpectraNoiseMinimumSignalToNoiseRatio.Enabled = True
        Else
            ' Unknown mode; disable both
            txtMassSpectraNoiseThresholdIntensity.Enabled = False
            txtMassSpectraNoiseFractionLowIntensityDataToAverage.Enabled = False
            txtMassSpectraNoiseMinimumSignalToNoiseRatio.Enabled = False
        End If

        chkSaveExtendedStatsFileIncludeFilterText.Enabled = chkSaveExtendedStatsFile.Checked
        chkSaveExtendedStatsFileIncludeStatusLog.Enabled = chkSaveExtendedStatsFile.Checked
        txtStatusLogKeyNameFilterList.Enabled = chkSaveExtendedStatsFile.Checked AndAlso chkSaveExtendedStatsFileIncludeStatusLog.Checked

        chkConsolidateConstantExtendedHeaderValues.Enabled = chkSaveExtendedStatsFile.Checked

        EnableDisableCustomSICValueGrid()

    End Sub

    Private Sub EnableDisableCustomSICValueGrid()
        Dim enableGrid As Boolean

        If txtCustomSICFileName.TextLength > 0 Then
            enableGrid = False
            dgCustomSICValues.CaptionText = "Custom SIC Values will be read from the file defined above"
        Else
            enableGrid = True
            dgCustomSICValues.CaptionText = "Custom SIC Values"
        End If

        cmdPasteCustomSICList.Enabled = enableGrid
        cmdCustomSICValuesPopulate.Enabled = enableGrid
        cmdClearCustomSICList.Enabled = enableGrid
        dgCustomSICValues.Enabled = enableGrid

    End Sub

    Private Sub frmMain_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        CatchUnrequestedHeightChange()
    End Sub

    Private Sub GetCurrentCustomSICTolerances(ByRef defaultMZTolerance As Double, ByRef defaultScanOrAcqTimeTolerance As Single)

        Try
            defaultMZTolerance = Double.Parse(txtSICTolerance.Text)

            If optSICTolerancePPM.Checked Then
                defaultMZTolerance = clsUtilities.PPMToMass(defaultMZTolerance, 1000)
            End If

        Catch ex As Exception
            defaultMZTolerance = 0.6
        End Try

        Try
            defaultScanOrAcqTimeTolerance = Single.Parse(txtCustomSICScanOrAcqTimeTolerance.Text)
        Catch ex As Exception
            defaultScanOrAcqTimeTolerance = 0
        End Try
    End Sub

    Private Function GetCustomSICScanToleranceType() As clsCustomSICList.eCustomSICScanTypeConstants

        If optCustomSICScanToleranceAbsolute.Checked Then
            Return clsCustomSICList.eCustomSICScanTypeConstants.Absolute

        ElseIf optCustomSICScanToleranceRelative.Checked Then
            Return clsCustomSICList.eCustomSICScanTypeConstants.Relative

        ElseIf optCustomSICScanToleranceAcqTime.Checked Then
            Return clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime

        Else
            ' Assume absolute
            Return clsCustomSICList.eCustomSICScanTypeConstants.Absolute
        End If

    End Function

    Private Function GetSettingsFilePath() As String
        Return ProcessFilesBase.GetSettingsFilePathLocal("MASIC", XML_SETTINGS_FILE_NAME)
    End Function

    Private Sub IniFileLoadOptions(updateIOPaths As Boolean)
        ' Prompts the user to select a file to load the options from

        Dim filePath As String

        Dim objOpenFile As New OpenFileDialog

        filePath = mXmlSettingsFilePath

        With objOpenFile
            .AddExtension = True
            .CheckFileExists = True
            .CheckPathExists = True
            .DefaultExt = ".xml"
            .DereferenceLinks = True
            .Multiselect = False
            .ValidateNames = True

            .Filter = "Settings files (*.xml)|*.xml|All files (*.*)|*.*"

            .FilterIndex = 1

            If filePath.Length > 0 Then
                Try
                    .InitialDirectory = Directory.GetParent(filePath).ToString
                Catch
                    .InitialDirectory = ProcessFilesBase.GetAppDirectoryPath()
                End Try
            Else
                .InitialDirectory = ProcessFilesBase.GetAppDirectoryPath()
            End If

            If File.Exists(filePath) Then
                .FileName = Path.GetFileName(filePath)
            End If

            .Title = "Specify file to load options from"

            .ShowDialog()
            If .FileName.Length > 0 Then
                mXmlSettingsFilePath = .FileName

                IniFileLoadOptions(mXmlSettingsFilePath, updateIOPaths)
            End If
        End With

    End Sub

    Private Sub IniFileLoadOptions(filePath As String, updateIOPaths As Boolean)
        ' Loads options from the given file

        Try

            ' Utilize MASIC's built-in LoadParameters function, then call ResetToDefaults
            Dim objMasic = New clsMASIC()

            Dim success = objMasic.LoadParameterFileSettings(filePath)
            If Not success Then
                MessageBox.Show("LoadParameterFileSettings returned false for: " & Path.GetFileName(filePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If

            ResetToDefaults(False, objMasic)

            ' Sleep for 100 msec, just to be safe
            Thread.Sleep(100)

            ' Now load some custom options that aren't loaded by clsMASIC
            Dim objXmlFile = New XmlSettingsFileAccessor()

            With objXmlFile
                ' Pass True to .LoadSettings() to turn off case sensitive matching
                .LoadSettings(filePath, False)

                Try
                    txtDatasetLookupFilePath.Text = .GetParam(clsMASICOptions.XML_SECTION_DATABASE_SETTINGS, "DatasetLookupFilePath", txtDatasetLookupFilePath.Text)
                    Try
                        If Not File.Exists(txtDatasetLookupFilePath.Text) Then
                            txtDatasetLookupFilePath.Text = String.Empty
                        End If
                    Catch ex As Exception
                        ' Ignore any errors here
                    End Try

                    If updateIOPaths Then
                        txtInputFilePath.Text = .GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "InputFilePath", txtInputFilePath.Text)
                    End If

                    Me.Width = .GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowWidth", Me.Width)
                    Me.Height = .GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowHeight", Me.Height)

                    If updateIOPaths Then
                        txtOutputDirectoryPath.Text = .GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "LastDirectory", txtOutputDirectoryPath.Text)
                    End If

                    If txtOutputDirectoryPath.TextLength = 0 Then
                        txtOutputDirectoryPath.Text = ProcessFilesBase.GetAppDirectoryPath()
                    End If

                    mPreferredInputFileExtension = .GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "PreferredInputFileExtension", mPreferredInputFileExtension)

                Catch ex As Exception
                    MessageBox.Show("Invalid parameter in settings file: " & Path.GetFileName(filePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                End Try
            End With

        Catch ex As Exception
            MessageBox.Show("Error loading settings from file: " & filePath & "; " & ControlChars.NewLine &
             ex.Message & ";" & ControlChars.NewLine, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try

    End Sub

    Private Sub IniFileSaveDefaultOptions()
        Dim eResponse As DialogResult

        eResponse = MessageBox.Show("Save the current options as defaults?", "Save Defaults", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1)
        If eResponse = DialogResult.Yes Then
            IniFileSaveOptions(GetSettingsFilePath(), False)
        End If
    End Sub

    Private Sub IniFileSaveOptions()
        ' Prompts the user to select a file to load the options from

        Dim filePath As String

        Dim objSaveFile As New SaveFileDialog

        filePath = mXmlSettingsFilePath

        With objSaveFile
            .AddExtension = True
            .CheckFileExists = False
            .CheckPathExists = True
            .DefaultExt = ".xml"
            .DereferenceLinks = True
            .OverwritePrompt = True
            .ValidateNames = True

            .Filter = "Settings files (*.xml)|*.xml|All files (*.*)|*.*"

            .FilterIndex = 1

            If filePath.Length > 0 Then
                Try
                    .InitialDirectory = Directory.GetParent(filePath).ToString
                Catch
                    .InitialDirectory = ProcessFilesBase.GetAppDirectoryPath()
                End Try
            Else
                .InitialDirectory = ProcessFilesBase.GetAppDirectoryPath()
            End If

            If File.Exists(filePath) Then
                .FileName = Path.GetFileName(filePath)
            End If

            .Title = "Specify file to save options to"

            .ShowDialog()
            If .FileName.Length > 0 Then
                mXmlSettingsFilePath = .FileName

                IniFileSaveOptions(mXmlSettingsFilePath, False)
            End If
        End With

    End Sub

    Private Sub IniFileSaveOptions(filePath As String, Optional saveWindowDimensionsOnly As Boolean = False)
        Dim objXmlFile As New XmlSettingsFileAccessor

        Dim objMasic As clsMASIC

        Try
            If Not saveWindowDimensionsOnly Then
                objMasic = New clsMASIC()

                UpdateMasicSettings(objMasic)

                objMasic.Options.SaveParameterFileSettings(filePath)

                ' Sleep for 100 msec, just to be safe
                Thread.Sleep(100)
            End If

            With objXmlFile
                ' Pass True to .LoadSettings() here so that newly made Xml files will have the correct capitalization
                .LoadSettings(filePath, True)

                Try
                    If Not saveWindowDimensionsOnly Then
                        Try
                            If File.Exists(txtDatasetLookupFilePath.Text) Then
                                .SetParam(clsMASICOptions.XML_SECTION_DATABASE_SETTINGS, "DatasetLookupFilePath", txtDatasetLookupFilePath.Text)
                            End If
                        Catch ex As Exception
                            ' Ignore any errors here
                        End Try

                        .SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "InputFilePath", txtInputFilePath.Text)
                    End If

                    .SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "LastDirectory", txtOutputDirectoryPath.Text)
                    .SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "PreferredInputFileExtension", mPreferredInputFileExtension)

                    .SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowWidth", Me.Width)
                    .SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowHeight", Me.Height)

                Catch ex As Exception
                    MessageBox.Show("Error storing parameter in settings file: " & Path.GetFileName(filePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                End Try

                .SaveSettings()
            End With
        Catch ex As Exception
            MessageBox.Show("Error saving settings to file: " & filePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try


    End Sub

    Private Sub InitializeControls()

        DefineDefaultCustomSICList()

        PopulateComboBoxes()

        InitializeCustomSICDataGrid()

        DefineOverviewText()

        mXmlSettingsFilePath = GetSettingsFilePath()
        ProcessFilesBase.CreateSettingsFileIfMissing(mXmlSettingsFilePath)

        mPreferredInputFileExtension = ".Raw"

        mHeightAdjustForce = 0
        mHeightAdjustTime = #1/1/1900#

        IniFileLoadOptions(mXmlSettingsFilePath, True)
        SetToolTips()

    End Sub

    Private Sub InitializeCustomSICDataGrid()

        ' Make the Peak Matching Thresholds data table
        Dim customSICValues = New DataTable(CUSTOM_SIC_VALUES_DATA_TABLE)

        ' Add the columns to the data table
        DatabaseUtils.DataTableUtils.AppendColumnDoubleToTable(customSICValues, COL_NAME_MZ)
        DatabaseUtils.DataTableUtils.AppendColumnDoubleToTable(customSICValues, COL_NAME_MZ_TOLERANCE)
        DatabaseUtils.DataTableUtils.AppendColumnDoubleToTable(customSICValues, COL_NAME_SCAN_CENTER)
        DatabaseUtils.DataTableUtils.AppendColumnDoubleToTable(customSICValues, COL_NAME_SCAN_TOLERANCE)
        DatabaseUtils.DataTableUtils.AppendColumnStringToTable(customSICValues, COL_NAME_SCAN_COMMENT, String.Empty)
        DatabaseUtils.DataTableUtils.AppendColumnIntegerToTable(customSICValues, COL_NAME_CUSTOM_SIC_VALUE_ROW_ID, 0, True, True)

        With customSICValues
            Dim PrimaryKeyColumn = New DataColumn() { .Columns(COL_NAME_CUSTOM_SIC_VALUE_ROW_ID)}
            .PrimaryKey = PrimaryKeyColumn
        End With

        ' Instantiate the dataset
        mCustomSICValuesDataset = New DataSet(CUSTOM_SIC_VALUES_DATA_TABLE)

        ' Add the new DataTable to the DataSet.
        mCustomSICValuesDataset.Tables.Add(customSICValues)

        ' Bind the DataSet to the DataGrid
        With dgCustomSICValues
            .DataSource = mCustomSICValuesDataset
            .DataMember = CUSTOM_SIC_VALUES_DATA_TABLE
        End With

        ' Update the grid's table style
        UpdateCustomSICDataGridTableStyle()

        ' Populate the table
        AutoPopulateCustomSICValues(False)

    End Sub

    Private Sub PasteCustomSICValues(clearList As Boolean)

        Dim LineDelimiters = New Char() {ControlChars.Cr, ControlChars.Lf}
        Dim ColumnDelimiters = New Char() {ControlChars.Tab, ","c}

        ' Examine the clipboard contents
        Dim objData = Clipboard.GetDataObject()

        If objData Is Nothing Then
            Return
        End If

        If Not objData.GetDataPresent(DataFormats.StringFormat, True) Then
            Return
        End If

        Dim data = CType(objData.GetData(DataFormats.StringFormat, True), String)

        ' Split data on carriage return or line feed characters
        ' Lines that end in CrLf will give two separate lines; one with the text, and one blank; that's OK
        Dim dataLines = data.Split(LineDelimiters, 50000)

        If dataLines.Length = 0 Then
            Return
        End If

        Dim defaultMZTolerance As Double
        Dim defaultScanOrAcqTimeTolerance As Single

        GetCurrentCustomSICTolerances(defaultMZTolerance, defaultScanOrAcqTimeTolerance)

        If clearList Then
            If Not ClearCustomSICList(True) Then Return
        End If

        Dim rowsAlreadyPresent = 0
        Dim rowsSkipped = 0

        For Each dataLine In dataLines
            If String.IsNullOrWhiteSpace(dataLine) Then
                Continue For
            End If

            Dim columns = dataLine.Split(ColumnDelimiters, 5)
            If columns.Length < 2 Then
                rowsSkipped += 1
                Continue For
            End If

            Try
                Dim mz As Double = 0
                Dim scanOrAcqTime As Single
                Dim comment As String = String.Empty
                Dim mzToleranceDa = defaultMZTolerance
                Dim scanOrAcqTimeTolerance = defaultScanOrAcqTimeTolerance

                If columns.Length = 2 Then
                    ' Assume pasted data is m/z and scan
                    mz = Double.Parse(columns(0))
                    scanOrAcqTime = Single.Parse(columns(1))

                ElseIf columns.Length >= 3 AndAlso columns(2).Length > 0 AndAlso
                    Not DataUtils.StringToValueUtils.IsNumber(columns(2).Chars(0)) Then
                    ' Assume pasted data is m/z, scan, and comment
                    mz = Double.Parse(columns(0))
                    scanOrAcqTime = Single.Parse(columns(1))
                    comment = columns(2)

                ElseIf columns.Length > 2 Then
                    ' Assume pasted data is m/z, m/z tolerance, scan, scan tolerance, and comment
                    mz = Double.Parse(columns(0))
                    mzToleranceDa = Double.Parse(columns(1))
                    If Math.Abs(mzToleranceDa) < Single.Epsilon Then
                        mzToleranceDa = defaultMZTolerance
                    End If

                    scanOrAcqTime = Single.Parse(columns(2))

                    If columns.Length >= 4 Then
                        scanOrAcqTimeTolerance = Single.Parse(columns(3))
                    Else
                        scanOrAcqTimeTolerance = defaultScanOrAcqTimeTolerance
                    End If

                    If columns.Length >= 5 Then
                        comment = columns(4)
                    Else
                        comment = String.Empty
                    End If

                End If

                If mz > 0 Then
                    Dim existingRowFound = False
                    AddCustomSICRow(mz, mzToleranceDa, scanOrAcqTime, scanOrAcqTimeTolerance, comment,
                                    existingRowFound)

                    If existingRowFound Then
                        rowsAlreadyPresent += 1
                    End If
                End If

            Catch ex As Exception
                ' Skip this row
                rowsSkipped += 1
            End Try

        Next

        If rowsAlreadyPresent > 0 Then
            Dim message As String
            If rowsAlreadyPresent = 1 Then
                message = "1 row of thresholds was"
            Else
                message = rowsAlreadyPresent.ToString() & " rows of thresholds were"
            End If

            MessageBox.Show(message & " already present in the table; duplicate rows are not allowed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End If

        If rowsSkipped > 0 Then
            Dim message As String
            If rowsSkipped = 1 Then
                message = "1 row was skipped because it"
            Else
                message = rowsSkipped.ToString() & " rows were skipped because they"
            End If

            MessageBox.Show(message & " didn't contain two columns of numeric data.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End If

    End Sub

    Private Sub PopulateComboBoxes()

        With cboExportRawDataFileFormat
            With .Items
                .Clear()
                .Insert(clsRawDataExportOptions.eExportRawDataFileFormatConstants.PEKFile, "PEK File")
                .Insert(clsRawDataExportOptions.eExportRawDataFileFormatConstants.CSVFile, "DeconTools CSV File")
            End With
            .SelectedIndex = clsRawDataExportOptions.eExportRawDataFileFormatConstants.CSVFile
        End With

        With cboSICNoiseThresholdMode
            With .Items
                .Clear()
                .Insert(eNoiseThresholdModes.AbsoluteThreshold, "Absolute Threshold")
                .Insert(eNoiseThresholdModes.TrimmedMeanByAbundance, "Trimmed Mean By Abundance")
                .Insert(eNoiseThresholdModes.TrimmedMeanByCount, "Trimmed Mean By Data Count")
                .Insert(eNoiseThresholdModes.TrimmedMedianByAbundance, "Trimmed Median By Abundance")
                .Insert(eNoiseThresholdModes.DualTrimmedMeanByAbundance, "Dual Trimmed Mean By Abundance")
            End With
            .SelectedIndex = eNoiseThresholdModes.DualTrimmedMeanByAbundance
        End With

        With cboMassSpectraNoiseThresholdMode
            With .Items
                .Clear()
                .Insert(eNoiseThresholdModes.AbsoluteThreshold, "Absolute Threshold")
                .Insert(eNoiseThresholdModes.TrimmedMeanByAbundance, "Trimmed Mean By Abundance")
                .Insert(eNoiseThresholdModes.TrimmedMeanByCount, "Trimmed Mean By Data Count")
                .Insert(eNoiseThresholdModes.TrimmedMedianByAbundance, "Trimmed Median By Abundance")
            End With
            .SelectedIndex = eNoiseThresholdModes.TrimmedMedianByAbundance
        End With

        With cboReporterIonMassMode
            With .Items
                .Clear()
                .Insert(clsReporterIons.eReporterIonMassModeConstants.CustomOrNone, "None")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.ITraqFourMZ, "iTraq: 114, 115, 116, and 117")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.ITraqETDThreeMZ, "iTraq ETD: 101, 102, and 104")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.TMTTwoMZ, "TMT 2: 126, 127")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.TMTSixMZ, "TMT 6: 126, 127, 128, 129, 130, 131")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZHighRes, "iTraq 8 for High Res MS/MS: 113, 114, ... 121")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZLowRes, "iTraq 8 for Low Res MS/MS (Considers 120 m/z for immonium loss from phenylalanine)")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.PCGalnaz, "PCGalnaz: 300.13 and 503.21")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.HemeCFragment, "Heme C: 616.18 and 617.19")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.LycAcetFragment, "Lys Acet: 126.091 and 127.095")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.TMTTenMZ, "TMT 10: 126, 127N, 127C, 128N, 128C, 129N, 129C, 130N, 130C, 131")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.OGlcNAc, "OGlcNAc: 204.087, 300.13, and 503.21")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.FrackingAmine20160217, "Fracking Amine 20160217: 157.089, 170.097, and 234.059")

                .Insert(clsReporterIons.eReporterIonMassModeConstants.FSFACustomCarbonyl, "FSFACustomCarbonyl")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.FSFACustomCarboxylic, "FSFACustomCarboxylic")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.FSFACustomHydroxyl, "FSFACustomHydroxyl")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.TMTElevenMZ, "TMT 11: 126, 127N, 127C, 128N, 128C, 129N, 129C, 130N, 130C, 131N, 131C")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.Acetylation, "Acetylated K")
                .Insert(clsReporterIons.eReporterIonMassModeConstants.TMTSixteenMZ, "TMT 16: 126, 127N, 127C, ... 132N, 132C, 133N, 133C, 134N")

            End With
            .SelectedIndex = clsReporterIons.eReporterIonMassModeConstants.CustomOrNone
        End With

    End Sub

    Private Sub ProcessFileUsingMASIC()

        Dim outputDirectoryPath As String
        Dim success As Boolean

        Dim startTime As DateTime

        If Not mWorking AndAlso ConfirmPaths() Then
            Try

                txtLogMessages.ResetText()

                ' Configure settings
                success = UpdateMasicSettings(mMasic)
                If Not success Then Exit Try

                ' Validate settings
                success = ValidateSettings(mMasic)
                If Not success Then Exit Try

                mProgressForm = New frmProgress()

                mProgressForm.InitializeProgressForm("Creating SIC's for the parent ions", 0, 100, False, True)
                mProgressForm.InitializeSubtask("", 0, 100, False)
                mProgressForm.ResetKeyPressAbortProcess()
                mProgressForm.Show()
                Application.DoEvents()

                With mMasic
                    Cursor.Current = Cursors.WaitCursor
                    mWorking = True
                    cmdStartProcessing.Enabled = False
                    Application.DoEvents()

                    startTime = DateTime.UtcNow

                    outputDirectoryPath = txtOutputDirectoryPath.Text
                    success = .ProcessFile(txtInputFilePath.Text, outputDirectoryPath)

                    Cursor.Current = Cursors.Default

                    If .Options.AbortProcessing Then
                        MessageBox.Show("Cancelled processing", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End If

                    If success Then
                        ' Grab the status message, but insert a carriage return directly after "in folder:"
                        MessageBox.Show(.StatusMessage.Replace("in folder:", "in folder:" & ControlChars.NewLine) & ControlChars.NewLine & "Elapsed time: " & StringUtilities.DblToString(DateTime.UtcNow.Subtract(startTime).TotalSeconds, 2) & " sec", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Else
                        MessageBox.Show("Error analyzing input file with MASIC: " & ControlChars.NewLine & .GetErrorMessage() & ControlChars.NewLine & .StatusMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    End If
                End With

            Catch ex As Exception
                MessageBox.Show("Error in frmMain->ProcessFileUsingMASIC: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Finally
                If Not mProgressForm Is Nothing Then
                    mProgressForm.HideForm()
                    mProgressForm = Nothing
                End If

                mWorking = False
                cmdStartProcessing.Enabled = True
            End Try
        End If
    End Sub

    Private Sub RegisterEvents(oClass As clsMASIC)
        AddHandler oClass.StatusEvent, AddressOf StatusEventHandler
        AddHandler oClass.DebugEvent, AddressOf DebugEventHandler
        AddHandler oClass.ErrorEvent, AddressOf ErrorEventHandler
        AddHandler oClass.WarningEvent, AddressOf WarningEventHandler

        AddHandler oClass.ProgressUpdate, AddressOf MASIC_ProgressUpdate
        AddHandler oClass.ProgressResetKeypressAbort, AddressOf MASIC_ProgressResetKeypressAbort
        AddHandler oClass.ProgressSubtaskChanged, AddressOf MASIC_ProgressSubtaskChanged
    End Sub

    Private Sub ResetToDefaults(confirmReset As Boolean, Optional ByRef objMasic As clsMASIC = Nothing)

        Dim eResponse As DialogResult
        Dim existingMasicObjectUsed As Boolean

        If confirmReset Then
            eResponse = MessageBox.Show("Are you sure you want to reset all settings to their default values?", "Reset to Defaults", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1)
            If eResponse <> DialogResult.Yes Then Exit Sub
        End If

        If objMasic Is Nothing Then
            objMasic = New clsMASIC()
            existingMasicObjectUsed = False
        Else
            existingMasicObjectUsed = True
        End If

        Me.Width = 710
        Me.Height = 560

        ' File Paths and Import Options
        Try
            If txtOutputDirectoryPath.TextLength = 0 OrElse Not Directory.Exists(txtOutputDirectoryPath.Text) Then
                txtOutputDirectoryPath.Text = ProcessFilesBase.GetAppDirectoryPath()
            End If
        Catch ex As Exception
            If confirmReset Then
                MessageBox.Show("Exception occurred while validating txtOutputDirectoryPath.Text: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If
        End Try

        Try
            Dim masicOptions = objMasic.Options

            With masicOptions
                ' Import Options
                txtParentIonDecoyMassDa.Text = .ParentIonDecoyMassDa.ToString()

                ' Masic Export Options
                chkIncludeHeaders.Checked = .IncludeHeadersInExportFile
                chkIncludeScanTimesInSICStatsFile.Checked = .IncludeScanTimesInSICStatsFile

                chkSkipMSMSProcessing.Checked = .SkipMSMSProcessing
                chkSkipSICAndRawDataProcessing.Checked = .SkipSICAndRawDataProcessing
                chkExportRawDataOnly.Checked = .ExportRawDataOnly
            End With

            With masicOptions.RawDataExportOptions
                ' Raw data export options
                chkExportRawSpectraData.Checked = .ExportEnabled                 ' Create .PEK file, or similar
                cboExportRawDataFileFormat.SelectedIndex = .FileFormat

                chkExportRawDataIncludeMSMS.Checked = .IncludeMSMS
                chkExportRawDataRenumberScans.Checked = .RenumberScans

                txtExportRawDataSignalToNoiseRatioMinimum.Text = .MinimumSignalToNoiseRatio.ToString()
                txtExportRawDataMaxIonCountPerScan.Text = .MaxIonCountPerScan.ToString()
                txtExportRawDataIntensityMinimum.Text = .IntensityMinimum.ToString()
            End With

            With masicOptions
                ' Finnigan Info File options
                chkSaveMSMethodFile.Checked = .WriteMSMethodFile
                chkSaveMSTuneFile.Checked = .WriteMSTuneFile
                chkWriteDetailedSICDataFile.Checked = .WriteDetailedSICDataFile
                chkSaveExtendedStatsFile.Checked = .WriteExtendedStats
                chkSaveExtendedStatsFileIncludeFilterText.Checked = .WriteExtendedStatsIncludeScanFilterText
                chkSaveExtendedStatsFileIncludeStatusLog.Checked = .WriteExtendedStatsStatusLog
                txtStatusLogKeyNameFilterList.Text = .GetStatusLogKeyNameFilterListAsText(False)

                chkConsolidateConstantExtendedHeaderValues.Checked = .ConsolidateConstantExtendedHeaderValues

                ' Dataset and Database Options
                txtDatasetID.Text = "0"
                txtDatabaseConnectionString.Text = .DatabaseConnectionString
                txtDatasetInfoQuerySQL.Text = .DatasetInfoQuerySql

                Try
                    If File.Exists(.DatasetLookupFilePath) Then
                        txtDatasetLookupFilePath.Text = .DatasetLookupFilePath
                    Else
                        txtDatasetLookupFilePath.Text = String.Empty
                    End If
                Catch ex As Exception
                    txtDatasetLookupFilePath.Text = String.Empty
                End Try

                ' SIC Options
                Dim sicToleranceIsPPM As Boolean
                Dim sicTolerance = .SICOptions.GetSICTolerance(sicToleranceIsPPM)

                txtSICTolerance.Text = StringUtilities.DblToString(sicTolerance, 6)
                If sicToleranceIsPPM Then
                    optSICTolerancePPM.Checked = True
                Else
                    optSICToleranceDa.Checked = True
                End If
            End With

            With masicOptions.SICOptions
                txtScanStart.Text = .ScanRangeStart.ToString()
                txtScanEnd.Text = .ScanRangeEnd.ToString()
                txtTimeStart.Text = .RTRangeStart.ToString()
                txtTimeEnd.Text = .RTRangeEnd.ToString()
            End With

            With masicOptions
                ' Note: the following 5 options are not graphically editable
                mSuppressNoParentIonsError = .SuppressNoParentIonsError

            End With

            With masicOptions.SICOptions
                mCompressMSSpectraData = .CompressMSSpectraData
                mCompressMSMSSpectraData = .CompressMSMSSpectraData
                mCompressToleranceDivisorForDa = .CompressToleranceDivisorForDa
                mCompressToleranceDivisorForPPM = .CompressToleranceDivisorForPPM

                txtMaxPeakWidthMinutesBackward.Text = .MaxSICPeakWidthMinutesBackward.ToString()
                txtMaxPeakWidthMinutesForward.Text = .MaxSICPeakWidthMinutesForward.ToString()
            End With

            With masicOptions.SICOptions.SICPeakFinderOptions
                txtIntensityThresholdFractionMax.Text = .IntensityThresholdFractionMax.ToString()
                txtIntensityThresholdAbsoluteMinimum.Text = .IntensityThresholdAbsoluteMinimum.ToString()
            End With

            With masicOptions.SICOptions
                chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Checked = .ReplaceSICZeroesWithMinimumPositiveValueFromMSData
                chkRefineReportedParentIonMZ.Checked = .RefineReportedParentIonMZ
                '' chkUseSICStatsFromLargestPeak.checked = .UseSICStatsFromLargestPeak
            End With

            With masicOptions.SICOptions.SICPeakFinderOptions
                ' Peak Finding Options
                cboSICNoiseThresholdMode.SelectedIndex = .SICBaselineNoiseOptions.BaselineNoiseMode
                txtSICNoiseThresholdIntensity.Text = .SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute.ToString()
                txtSICNoiseFractionLowIntensityDataToAverage.Text = .SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage.ToString()

                txtMaxDistanceScansNoOverlap.Text = .MaxDistanceScansNoOverlap.ToString()
                txtMaxAllowedUpwardSpikeFractionMax.Text = .MaxAllowedUpwardSpikeFractionMax.ToString()
                txtInitialPeakWidthScansScaler.Text = .InitialPeakWidthScansScaler.ToString()
                txtInitialPeakWidthScansMaximum.Text = .InitialPeakWidthScansMaximum.ToString()

                If .UseButterworthSmooth Then
                    optUseButterworthSmooth.Checked = True
                    optUseSavitzkyGolaySmooth.Checked = False
                Else
                    optUseButterworthSmooth.Checked = False
                    optUseSavitzkyGolaySmooth.Checked = True
                End If

                txtButterworthSamplingFrequency.Text = .ButterworthSamplingFrequency.ToString()
                txtSavitzkyGolayFilterOrder.Text = .SavitzkyGolayFilterOrder.ToString()

                chkFindPeaksOnSmoothedData.Checked = .FindPeaksOnSmoothedData
                chkSmoothDataRegardlessOfMinimumPeakWidth.Checked = .SmoothDataRegardlessOfMinimumPeakWidth

                ' Mass Spectra Noise Threshold Options
                cboMassSpectraNoiseThresholdMode.SelectedIndex = .MassSpectraNoiseThresholdOptions.BaselineNoiseMode
                txtMassSpectraNoiseThresholdIntensity.Text = .MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute.ToString()
                txtMassSpectraNoiseFractionLowIntensityDataToAverage.Text = .MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage.ToString()
                txtMassSpectraNoiseMinimumSignalToNoiseRatio.Text = .MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio.ToString()

            End With

            With masicOptions.SICOptions
                ' Similarity Options
                txtSimilarIonMZToleranceHalfWidth.Text = .SimilarIonMZToleranceHalfWidth.ToString()
                txtSimilarIonToleranceHalfWidthMinutes.Text = .SimilarIonToleranceHalfWidthMinutes.ToString()
                txtSpectrumSimilarityMinimum.Text = .SpectrumSimilarityMinimum.ToString()
            End With

            With masicOptions.BinningOptions
                ' Binning Options
                txtBinStartX.Text = .StartX.ToString()
                txtBinEndX.Text = .EndX.ToString()
                txtBinSize.Text = .BinSize.ToString()
                txtMaximumBinCount.Text = .MaximumBinCount.ToString()

                txtBinnedDataIntensityPrecisionPct.Text = .IntensityPrecisionPercent.ToString()

                chkBinnedDataNormalize.Checked = .Normalize
                chkBinnedDataSumAllIntensitiesForBin.Checked = .SumAllIntensitiesForBin
            End With

            With masicOptions.CacheOptions
                ' Spectrum caching options (not graphically editable)
                mCacheOptions.DiskCachingAlwaysDisabled = .DiskCachingAlwaysDisabled
                mCacheOptions.DirectoryPath = .DirectoryPath
                mCacheOptions.SpectraToRetainInMemory = .SpectraToRetainInMemory

            End With

            With masicOptions.ReporterIons
                ' Reporter ion options
                txtReporterIonMZToleranceDa.Text = StringUtilities.DblToString(.ReporterIonToleranceDaDefault, 6)

                cboReporterIonMassMode.SelectedIndex = .ReporterIonMassMode

                chkReporterIonStatsEnabled.Checked = .ReporterIonStatsEnabled
                chkReporterIonApplyAbundanceCorrection.Checked = .ReporterIonApplyAbundanceCorrection

                chkReporterIonSaveObservedMasses.Checked = .ReporterIonSaveObservedMasses
                chkReporterIonSaveUncorrectedIntensities.Checked = .ReporterIonSaveUncorrectedIntensities
            End With

            With masicOptions
                ' MRM Options
                chkMRMWriteDataList.Checked = .WriteMRMDataList
                chkMRMWriteIntensityCrosstab.Checked = .WriteMRMIntensityCrosstab
            End With

            With masicOptions.CustomSICList
                ' Custom SIC Options
                txtCustomSICFileName.Text = .CustomSICListFileName

                chkLimitSearchToCustomMZs.Checked = .LimitSearchToCustomMZList
                SetCustomSICToleranceType(.ScanToleranceType)

                txtCustomSICScanOrAcqTimeTolerance.Text = .ScanOrAcqTimeTolerance.ToString()

            End With

            ' Load the Custom m/z values from mCustomSICList
            Dim customMzList = masicOptions.CustomSICList.CustomMZSearchValues()

            ClearCustomSICList(False)
            For Each customMzSpec In customMzList
                With customMzSpec
                    AddCustomSICRow(.MZ, .MZToleranceDa, .ScanOrAcqTimeCenter, .ScanOrAcqTimeTolerance, .Comment)
                End With
            Next

        Catch ex As Exception
            If confirmReset Then
                MessageBox.Show("Error resetting values to defaults: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If
        End Try

        If Not existingMasicObjectUsed Then
            objMasic = Nothing
        End If

    End Sub

    Private Sub SelectDatasetLookupFile()
        Dim objOpenFile As New OpenFileDialog

        With objOpenFile
            .AddExtension = True
            .CheckFileExists = True
            .CheckPathExists = True
            .DefaultExt = ".txt"
            .DereferenceLinks = True
            .Multiselect = False
            .ValidateNames = True

            .Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            .FilterIndex = 1

            If txtDatasetLookupFilePath.TextLength > 0 Then
                Try
                    .InitialDirectory = Directory.GetParent(txtDatasetLookupFilePath.Text).ToString
                Catch
                    .InitialDirectory = ProcessFilesBase.GetAppDirectoryPath()
                End Try
            Else
                .InitialDirectory = ProcessFilesBase.GetAppDirectoryPath()
            End If

            .Title = "Select dataset lookup file"

            .ShowDialog()
            If .FileName.Length > 0 Then
                txtDatasetLookupFilePath.Text = .FileName
            End If
        End With

    End Sub

    Private Sub SelectCustomSICFile()

        Dim objOpenFile As New OpenFileDialog
        Dim fileExtension As String

        With objOpenFile
            .AddExtension = True
            .CheckFileExists = True
            .CheckPathExists = True
            .DefaultExt = ".txt"
            .DereferenceLinks = True
            .Multiselect = False
            .ValidateNames = True

            .Filter = "Text files (*.txt)|*.txt|" &
             "CSV files (*.csv)|*.csv|" &
             "All files (*.*)|*.*"

            fileExtension = ".txt"

            If txtCustomSICFileName.TextLength > 0 Then
                fileExtension = Path.GetExtension(txtCustomSICFileName.Text)
            End If

            Select Case fileExtension.ToLower()
                Case ".txt"
                    .FilterIndex = 1
                Case "csv"
                    .FilterIndex = 2
                Case Else
                    .FilterIndex = 1
            End Select

            If txtCustomSICFileName.TextLength > 0 Then
                Try
                    .InitialDirectory = Directory.GetParent(txtCustomSICFileName.Text).ToString
                Catch
                    .InitialDirectory = ProcessFilesBase.GetAppDirectoryPath()
                End Try
            Else
                .InitialDirectory = ProcessFilesBase.GetAppDirectoryPath()
            End If

            .Title = "Select custom SIC values file"

            .ShowDialog()
            If .FileName.Length > 0 Then
                txtCustomSICFileName.Text = .FileName
            End If
        End With

    End Sub

    Private Sub SelectInputFile()

        Dim objOpenFile As New OpenFileDialog
        Dim fileExtension As String

        With objOpenFile
            .AddExtension = True
            .CheckFileExists = True
            .CheckPathExists = True
            .DefaultExt = ".txt"
            .DereferenceLinks = True
            .Multiselect = False
            .ValidateNames = True

            .Filter = "Xcalibur Raw files (*.raw)|*.raw|" &
             "mzXML files (*.mzXML)|*.mzXML|" &
             "mzXML files (*mzXML.xml)|*mzXML.xml|" &
             "mzData files (*.mzData)|*.mzData|" &
             "mzData files (*mzData.xml)|*mzData.xml|" &
             "Mascot Generic Format files (*.mgf)|*.mgf|" &
             "CDF files (*.cdf)|*.cdf|" &
             "All files (*.*)|*.*"

            fileExtension = String.Copy(mPreferredInputFileExtension)

            If txtInputFilePath.TextLength > 0 Then
                fileExtension = Path.GetExtension(txtInputFilePath.Text)
            End If

            Select Case fileExtension.ToLower()
                Case ".mzxml"
                    .FilterIndex = 2
                Case "mzxml.xml"
                    .FilterIndex = 3
                Case ".mzdata"
                    .FilterIndex = 4
                Case "mzdata.xml"
                    .FilterIndex = 5
                Case ".mgf"
                    .FilterIndex = 6
                Case ".cdf"
                    .FilterIndex = 7
                Case Else
                    .FilterIndex = 1
            End Select

            If txtInputFilePath.TextLength > 0 Then
                Try
                    .InitialDirectory = Directory.GetParent(txtInputFilePath.Text).ToString
                Catch
                    .InitialDirectory = ProcessFilesBase.GetAppDirectoryPath()
                End Try
            Else
                .InitialDirectory = ProcessFilesBase.GetAppDirectoryPath()
            End If

            .Title = "Select input file"

            .ShowDialog()
            If .FileName.Length > 0 Then
                txtInputFilePath.Text = .FileName
                mPreferredInputFileExtension = Path.GetExtension(.FileName)
            End If
        End With

    End Sub

    Private Sub SelectOutputDirectory()

        Dim objFolderBrowserDialog As New FolderBrowser()

        With objFolderBrowserDialog
            ' No need to set the Browse Flags; default values are already set

            If txtOutputDirectoryPath.TextLength > 0 Then
                .FolderPath = txtOutputDirectoryPath.Text
            End If

            If .BrowseForFolder() Then
                txtOutputDirectoryPath.Text = .FolderPath
            End If
        End With
    End Sub

    Private Sub SetConnectionStringToPNNLServer()
        txtDatabaseConnectionString.Text = clsDatabaseAccess.DATABASE_CONNECTION_STRING_DEFAULT
        txtDatasetInfoQuerySQL.Text = clsDatabaseAccess.DATABASE_DATASET_INFO_QUERY_DEFAULT
    End Sub

    Private Sub SetCustomSICToleranceType(eCustomSICScanToleranceType As clsCustomSICList.eCustomSICScanTypeConstants)
        Select Case eCustomSICScanToleranceType
            Case clsCustomSICList.eCustomSICScanTypeConstants.Absolute
                optCustomSICScanToleranceAbsolute.Checked = True

            Case clsCustomSICList.eCustomSICScanTypeConstants.Relative
                optCustomSICScanToleranceRelative.Checked = True

            Case clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime
                optCustomSICScanToleranceAcqTime.Checked = True

            Case Else
                optCustomSICScanToleranceAbsolute.Checked = True
        End Select
    End Sub

    Public Sub SetHeightAdjustForce(heightToForce As Integer)
        ' This function can be used to prevent the form from resizing itself if the MyBase.Resize event
        '  fires within 2 seconds of the current time
        ' See CatchUnrequestedHeightChange for more info
        mHeightAdjustForce = heightToForce
        mHeightAdjustTime = DateTime.UtcNow
    End Sub

    Private Sub SetToolTips()
        Dim objToolTipControl As New ToolTip

        With objToolTipControl
            .SetToolTip(txtDatasetID, "The dataset ID is included as the first column in the output file.")

            .SetToolTip(txtIntensityThresholdAbsoluteMinimum, "Threshold for extending SIC")
            .SetToolTip(txtMaxDistanceScansNoOverlap, "Maximum distance that the edge of an identified peak can be away from the scan number that the parent ion was observed in if the identified peak does not contain the parent ion.")
            .SetToolTip(txtMaxAllowedUpwardSpikeFractionMax, "Maximum fraction of the peak maximum that an upward spike can be to be included in the peak")
            .SetToolTip(txtInitialPeakWidthScansScaler, "Multiplied by the S/N for the given spectrum to determine the initial minimum peak width (in scans) to try")
            .SetToolTip(txtInitialPeakWidthScansMaximum, "Maximum initial peak width to allow")

            .SetToolTip(txtSICTolerance, "Search tolerance for creating SIC; suggest 0.6 Da for ion traps and 20 ppm for TOF, FT or Orbitrap instruments")
            .SetToolTip(txtButterworthSamplingFrequency, "Value between 0.01 and 0.99; suggested value is 0.25")
            .SetToolTip(txtSavitzkyGolayFilterOrder, "Even number, 0 or greater; 0 means a moving average filter, 2 means a 2nd order Savitzky Golay filter")

            .SetToolTip(chkRefineReportedParentIonMZ, "If enabled, then will look through the m/z values in the parent ion spectrum data to find the closest match (within SICTolerance / " & clsSICOptions.DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_DA.ToString() & "); will update the reported m/z value to the one found")

            '' .SetToolTip(chkUseSICStatsFromLargestPeak, "If enabled, SIC stats for similar parent ions will all be based on the largest peak in the selected ion chromatogram")

            .SetToolTip(txtStatusLogKeyNameFilterList, "Enter a comma and/or NewLine separated list of Status Log Key names to match (will match any part of the key name to the text you enter).  Leave blank to include all Status Log entries.")
        End With

    End Sub

    Private Sub ShowAboutBox()
        Dim message As String

        message = String.Empty

        message &= "Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2003" & ControlChars.NewLine
        message &= "Copyright 2005, Battelle Memorial Institute.  All Rights Reserved." & ControlChars.NewLine & ControlChars.NewLine

        message &= "This is version " & Application.ProductVersion & " (" & PROGRAM_DATE & "). "
        message &= "Using MASIC PeakFinder DLL version " & mMasic.MASICPeakFinderDllVersion & ControlChars.NewLine & ControlChars.NewLine

        message &= "E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov" & ControlChars.NewLine
        message &= "Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/" & ControlChars.NewLine & ControlChars.NewLine

        message &= "Licensed under the 2-Clause BSD License; you may not use this file except in compliance with the License.  "
        message &= "You may obtain a copy of the License at https://opensource.org/licenses/BSD-2-Clause" & ControlChars.NewLine & ControlChars.NewLine

        MessageBox.Show(message, "About", MessageBoxButtons.OK, MessageBoxIcon.Information)

    End Sub

    Private Sub UpdateCustomSICDataGridTableStyle()
        Dim tsCustomSICValues As DataGridTableStyle
        Dim timeTolerance As Boolean

        ' Define the PM Thresholds table style
        tsCustomSICValues = New DataGridTableStyle

        ' Setting the MappingName of the table style to CUSTOM_SIC_VALUES_DATA_TABLE will cause this style to be used with that table
        With tsCustomSICValues
            .MappingName = CUSTOM_SIC_VALUES_DATA_TABLE
            .AllowSorting = True
            .ColumnHeadersVisible = True
            .RowHeadersVisible = True
            .ReadOnly = False
        End With

        DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_MZ, "Custom m/z", 90)
        DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_MZ_TOLERANCE, "m/z tolerance (Da)", 110)

        timeTolerance = False
        Select Case GetCustomSICScanToleranceType()
            Case clsCustomSICList.eCustomSICScanTypeConstants.Relative
                DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_CENTER, "Relative Scan Number (0 to 1)", 170)
                DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_TOLERANCE, "Scan Tolerance", 90)

            Case clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime
                DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_CENTER, "Acq time (minutes)", 110)
                DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_TOLERANCE, "Time Tolerance", 90)
                timeTolerance = True

            Case Else
                ' Includes eCustomSICScanTypeConstants.Absolute
                DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_CENTER, "Scan Number", 90)
                DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_TOLERANCE, "Scan Tolerance", 90)
        End Select

        DataGridUtils.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_COMMENT, "Comment", 90)

        fraCustomSICControls.Left = dgCustomSICValues.Left + dgCustomSICValues.Width + 15

        With dgCustomSICValues
            .TableStyles.Clear()

            If Not .TableStyles.Contains(tsCustomSICValues) Then
                .TableStyles.Add(tsCustomSICValues)
            End If

            .Refresh()
        End With

        If timeTolerance Then
            lblCustomSICScanTolerance.Text = "Time Tolerance"
        Else
            lblCustomSICScanTolerance.Text = "Scan Tolerance"
        End If

    End Sub

    Private Function UpdateMasicSettings(ByRef objMasic As clsMASIC) As Boolean

        Dim parseError As Boolean

        Try
            Dim masicOptions = objMasic.Options

            With masicOptions
                ' Import options

                .ParentIonDecoyMassDa = ParseTextboxValueDbl(txtParentIonDecoyMassDa, lblParentIonDecoyMassDa.Text & " must be a value", parseError)
                If parseError Then Exit Try

                ' Masic Export Options
                .IncludeHeadersInExportFile = chkIncludeHeaders.Checked
                .IncludeScanTimesInSICStatsFile = chkIncludeScanTimesInSICStatsFile.Checked

                .SkipMSMSProcessing = chkSkipMSMSProcessing.Checked
                .SkipSICAndRawDataProcessing = chkSkipSICAndRawDataProcessing.Checked
                .ExportRawDataOnly = chkExportRawDataOnly.Checked
            End With

            With masicOptions.RawDataExportOptions
                ' Raw data export options
                .ExportEnabled = chkExportRawSpectraData.Checked
                .FileFormat = CType(cboExportRawDataFileFormat.SelectedIndex, clsRawDataExportOptions.eExportRawDataFileFormatConstants)

                .IncludeMSMS = chkExportRawDataIncludeMSMS.Checked
                .RenumberScans = chkExportRawDataRenumberScans.Checked

                .MinimumSignalToNoiseRatio = ParseTextBoxValueFloat(txtExportRawDataSignalToNoiseRatioMinimum,
                                                                    lblExportRawDataSignalToNoiseRatioMinimum.Text & " must be a value", parseError)
                If parseError Then Exit Try
                .MaxIonCountPerScan = ParseTextBoxValueInt(txtExportRawDataMaxIonCountPerScan,
                                                           lblExportRawDataMaxIonCountPerScan.Text & " must be an integer value", parseError)
                If parseError Then Exit Try
                .IntensityMinimum = ParseTextBoxValueFloat(txtExportRawDataIntensityMinimum,
                                                           lblExportRawDataIntensityMinimum.Text & " must be a value", parseError)
                If parseError Then Exit Try
            End With

            With masicOptions

                ' Finnigan Info File options
                .WriteMSMethodFile = chkSaveMSMethodFile.Checked
                .WriteMSTuneFile = chkSaveMSTuneFile.Checked
                .WriteDetailedSICDataFile = chkWriteDetailedSICDataFile.Checked
                .WriteExtendedStats = chkSaveExtendedStatsFile.Checked
                .WriteExtendedStatsIncludeScanFilterText = chkSaveExtendedStatsFileIncludeFilterText.Checked
                .WriteExtendedStatsStatusLog = chkSaveExtendedStatsFileIncludeStatusLog.Checked
                .SetStatusLogKeyNameFilterList(txtStatusLogKeyNameFilterList.Text, ","c)

                .ConsolidateConstantExtendedHeaderValues = chkConsolidateConstantExtendedHeaderValues.Checked

                ' Dataset and Database options
                .SICOptions.DatasetID = ParseTextBoxValueInt(txtDatasetID, lblDatasetID.Text & " must be an integer value", parseError)
                If parseError Then Exit Try

                If txtDatabaseConnectionString.TextLength > 0 And txtDatasetInfoQuerySQL.TextLength > 0 Then
                    .DatabaseConnectionString = txtDatabaseConnectionString.Text
                    .DatasetInfoQuerySql = txtDatasetInfoQuerySQL.Text
                Else
                    .DatabaseConnectionString = String.Empty
                    .DatasetInfoQuerySql = String.Empty
                End If

                Try
                    If File.Exists(txtDatasetLookupFilePath.Text) Then
                        .DatasetLookupFilePath = txtDatasetLookupFilePath.Text
                    Else
                        .DatasetLookupFilePath = String.Empty
                    End If
                Catch ex As Exception
                    .DatasetLookupFilePath = String.Empty
                End Try

            End With

            ' SIC Options
            Dim sicTolerance = ParseTextBoxValueDbl(txtSICTolerance, lblSICToleranceDa.Text & " must be a value", parseError)
            If parseError Then Exit Try

            With masicOptions.SICOptions
                .SetSICTolerance(sicTolerance, optSICTolerancePPM.Checked)

                .ScanRangeStart = ParseTextBoxValueInt(txtScanStart, lblScanStart.Text & " must be a value", parseError)
                If parseError Then Exit Try
                .ScanRangeEnd = ParseTextBoxValueInt(txtScanEnd, lblScanEnd.Text & " must be a value", parseError)
                If parseError Then Exit Try

                .RTRangeStart = ParseTextBoxValueFloat(txtTimeStart, lblTimeStart.Text & " must be a value", parseError)
                If parseError Then Exit Try
                .RTRangeEnd = ParseTextBoxValueFloat(txtTimeEnd, lblTimeEnd.Text & " must be a value", parseError)
                If parseError Then Exit Try
            End With

            ' Note: the following 5 options are not graphically editable
            masicOptions.SuppressNoParentIonsError = mSuppressNoParentIonsError

            With masicOptions.SICOptions
                .CompressMSSpectraData = mCompressMSSpectraData
                .CompressMSMSSpectraData = mCompressMSMSSpectraData
                .CompressToleranceDivisorForDa = mCompressToleranceDivisorForDa
                .CompressToleranceDivisorForPPM = mCompressToleranceDivisorForPPM

                .MaxSICPeakWidthMinutesBackward = ParseTextBoxValueFloat(txtMaxPeakWidthMinutesBackward, lblMaxPeakWidthMinutes.Text & " " & lblMaxPeakWidthMinutesBackward.Text & " must be a value", parseError)
                If parseError Then Exit Try
                .MaxSICPeakWidthMinutesForward = ParseTextBoxValueFloat(txtMaxPeakWidthMinutesForward, lblMaxPeakWidthMinutes.Text & " " & lblMaxPeakWidthMinutesForward.Text & " must be a value", parseError)
                If parseError Then Exit Try
            End With

            With masicOptions.SICOptions
                .SICPeakFinderOptions.IntensityThresholdFractionMax = ParseTextBoxValueFloat(txtIntensityThresholdFractionMax, lblIntensityThresholdFractionMax.Text & " must be a value", parseError)
                If parseError Then Exit Try

                .SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum = ParseTextBoxValueFloat(txtIntensityThresholdAbsoluteMinimum, lblIntensityThresholdAbsoluteMinimum.Text & " must be a value", parseError)
                If parseError Then Exit Try

                .ReplaceSICZeroesWithMinimumPositiveValueFromMSData = chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Checked
                .RefineReportedParentIonMZ = chkRefineReportedParentIonMZ.Checked

                '' .UseSICStatsFromLargestPeak = chkUseSICStatsFromLargestPeak.Checked
            End With

            With masicOptions.SICOptions.SICPeakFinderOptions
                ' Peak Finding Options
                .SICBaselineNoiseOptions.BaselineNoiseMode = CType(cboSICNoiseThresholdMode.SelectedIndex, eNoiseThresholdModes)
                .SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute = ParseTextBoxValueFloat(txtSICNoiseThresholdIntensity, lblSICNoiseThresholdIntensity.Text & " must be a value", parseError)
                If parseError Then Exit Try

                .SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage = ParseTextBoxValueFloat(txtSICNoiseFractionLowIntensityDataToAverage, lblSICNoiseFractionLowIntensityDataToAverage.Text & " must be a value", parseError)
                If parseError Then Exit Try

                ' This value isn't utilized by MASIC for SICs so we'll force it to always be zero
                .SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = 0

                .MaxDistanceScansNoOverlap = ParseTextBoxValueInt(txtMaxDistanceScansNoOverlap, lblMaxDistanceScansNoOverlap.Text & " must be an integer value", parseError)
                If parseError Then Exit Try
                .MaxAllowedUpwardSpikeFractionMax = ParseTextBoxValueFloat(txtMaxAllowedUpwardSpikeFractionMax, lblMaxAllowedUpwardSpikeFractionMax.Text & " must be a value", parseError)
                If parseError Then Exit Try
                .InitialPeakWidthScansScaler = ParseTextBoxValueFloat(txtInitialPeakWidthScansScaler, lblInitialPeakWidthScansScaler.Text & " must be a value", parseError)
                If parseError Then Exit Try
                .InitialPeakWidthScansMaximum = ParseTextBoxValueInt(txtInitialPeakWidthScansMaximum, lblInitialPeakWidthScansMaximum.Text & " must be an integer value", parseError)
                If parseError Then Exit Try

                .UseButterworthSmooth = optUseButterworthSmooth.Checked
                .ButterworthSamplingFrequency = ParseTextBoxValueFloat(txtButterworthSamplingFrequency, lblButterworthSamplingFrequency.Text & " must be a value", parseError)
                If parseError Then Exit Try

                .UseSavitzkyGolaySmooth = optUseSavitzkyGolaySmooth.Checked
                .SavitzkyGolayFilterOrder = CShort(ParseTextBoxValueInt(txtSavitzkyGolayFilterOrder, lblSavitzkyGolayFilterOrder.Text & " must be an integer value", parseError))
                If parseError Then Exit Try

                .FindPeaksOnSmoothedData = chkFindPeaksOnSmoothedData.Checked
                .SmoothDataRegardlessOfMinimumPeakWidth = chkSmoothDataRegardlessOfMinimumPeakWidth.Checked

                ' Mass Spectra Noise Threshold Options
                .MassSpectraNoiseThresholdOptions.BaselineNoiseMode = CType(cboMassSpectraNoiseThresholdMode.SelectedIndex, eNoiseThresholdModes)
                .MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute = ParseTextBoxValueFloat(txtMassSpectraNoiseThresholdIntensity, lblMassSpectraNoiseThresholdIntensity.Text & " must be a value", parseError)
                If parseError Then Exit Try

                .MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = ParseTextBoxValueFloat(txtMassSpectraNoiseFractionLowIntensityDataToAverage, lblMassSpectraNoiseFractionLowIntensityDataToAverage.Text & " must be a value", parseError)
                If parseError Then Exit Try

                .MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = ParseTextBoxValueFloat(txtMassSpectraNoiseMinimumSignalToNoiseRatio, lblMassSpectraNoiseMinimumSignalToNoiseRatio.Text & " must be a value", parseError)
                If parseError Then Exit Try

            End With

            With masicOptions.SICOptions
                ' Similarity Options
                .SimilarIonMZToleranceHalfWidth = ParseTextBoxValueFloat(txtSimilarIonMZToleranceHalfWidth, lblSimilarIonMZToleranceHalfWidth.Text & " must be a value", parseError)
                If parseError Then Exit Try
                .SimilarIonToleranceHalfWidthMinutes = ParseTextBoxValueInt(txtSimilarIonToleranceHalfWidthMinutes, lblSimilarIonTimeToleranceHalfWidth.Text & " must be a value", parseError)
                If parseError Then Exit Try
                .SpectrumSimilarityMinimum = ParseTextBoxValueFloat(txtSpectrumSimilarityMinimum, lblSpectrumSimilarityMinimum.Text & " must be a value", parseError)
                If parseError Then Exit Try
            End With

            With masicOptions.BinningOptions

                ' Binning Options
                .StartX = ParseTextBoxValueFloat(txtBinStartX, lblBinStartX.Text & " must be a value", parseError)
                If parseError Then Exit Try
                .EndX = ParseTextBoxValueFloat(txtBinEndX, lblBinEndX.Text & " must be a value", parseError)
                If parseError Then Exit Try
                .BinSize = ParseTextBoxValueFloat(txtBinSize, lblBinSize.Text & " must be a value", parseError)
                If parseError Then Exit Try
                .MaximumBinCount = ParseTextBoxValueInt(txtMaximumBinCount, lblMaximumBinCount.Text & " must be an integer value", parseError)
                If parseError Then Exit Try

                .IntensityPrecisionPercent = ParseTextBoxValueFloat(txtBinnedDataIntensityPrecisionPct, lblBinnedDataIntensityPrecisionPct.Text & " must be a value", parseError)
                If parseError Then Exit Try

                .Normalize = chkBinnedDataNormalize.Checked
                .SumAllIntensitiesForBin = chkBinnedDataSumAllIntensitiesForBin.Checked
            End With

            With masicOptions.CacheOptions
                ' Spectrum caching options
                .DiskCachingAlwaysDisabled = mCacheOptions.DiskCachingAlwaysDisabled
                .DirectoryPath = mCacheOptions.DirectoryPath
                .SpectraToRetainInMemory = mCacheOptions.SpectraToRetainInMemory
            End With

            With masicOptions.ReporterIons
                ' Reporter ion options
                .ReporterIonStatsEnabled = chkReporterIonStatsEnabled.Checked

                ' Note that this will set .ReporterIonToleranceDa to 0.5
                .ReporterIonMassMode = CType(cboReporterIonMassMode.SelectedIndex, clsReporterIons.eReporterIonMassModeConstants)

                ' Update .ReporterIonToleranceDa based on txtReporterIonMZToleranceDa
                .ReporterIonToleranceDaDefault = ParseTextBoxValueDbl(txtReporterIonMZToleranceDa, "", parseError,
                                                                      clsReporterIons.REPORTER_ION_TOLERANCE_DA_DEFAULT, False)
                .SetReporterIonMassMode(.ReporterIonMassMode, .ReporterIonToleranceDaDefault)

                .ReporterIonApplyAbundanceCorrection = chkReporterIonApplyAbundanceCorrection.Checked

                .ReporterIonSaveObservedMasses = chkReporterIonSaveObservedMasses.Checked
                .ReporterIonSaveUncorrectedIntensities = chkReporterIonSaveUncorrectedIntensities.Checked
            End With

            With masicOptions
                ' MRM Options
                .WriteMRMDataList = chkMRMWriteDataList.Checked
                .WriteMRMIntensityCrosstab = chkMRMWriteIntensityCrosstab.Checked

                ' Custom m/z options
                .CustomSICList.LimitSearchToCustomMZList = chkLimitSearchToCustomMZs.Checked
            End With

            ' Store the custom M/Z values in mCustomSICList

            Dim customSICFileName = txtCustomSICFileName.Text.Trim()
            masicOptions.CustomSICList.CustomSICListFileName = customSICFileName

            Dim mzSearchSpecs = New List(Of clsCustomMZSearchSpec)

            ' Only use the data in table CUSTOM_SIC_VALUES_DATA_TABLE if the CustomSicFileName is empty
            If String.IsNullOrWhiteSpace(customSICFileName) Then

                For Each myDataRow As DataRow In mCustomSICValuesDataset.Tables(CUSTOM_SIC_VALUES_DATA_TABLE).Rows
                    With myDataRow
                        If IsNumeric(.Item(0)) And IsNumeric(.Item(1)) Then

                            Dim targetMz = CDbl(.Item(0))
                            Dim mzSearchSpec = New clsCustomMZSearchSpec(targetMz)

                            mzSearchSpec.MZToleranceDa = CDbl(.Item(1))

                            mzSearchSpec.ScanOrAcqTimeCenter = CSng(.Item(2))
                            mzSearchSpec.ScanOrAcqTimeTolerance = CSng(.Item(3))
                            mzSearchSpec.Comment = CStrSafe(.Item(4))

                            mzSearchSpecs.Add(mzSearchSpec)
                        End If
                    End With
                Next myDataRow

            End If

            Dim eScanType As clsCustomSICList.eCustomSICScanTypeConstants
            If optCustomSICScanToleranceAbsolute.Checked Then
                eScanType = clsCustomSICList.eCustomSICScanTypeConstants.Absolute
            ElseIf optCustomSICScanToleranceRelative.Checked Then
                eScanType = clsCustomSICList.eCustomSICScanTypeConstants.Relative
            ElseIf optCustomSICScanToleranceAcqTime.Checked Then
                eScanType = clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime
            Else
                ' Assume absolute
                eScanType = clsCustomSICList.eCustomSICScanTypeConstants.Absolute
            End If

            Dim scanOrAcqTimeTolerance = ParseTextBoxValueFloat(txtCustomSICScanOrAcqTimeTolerance, lblCustomSICScanTolerance.Text & " must be a value", parseError)
            If parseError Then Exit Try

            masicOptions.CustomSICList.SetCustomSICListValues(eScanType, scanOrAcqTimeTolerance, mzSearchSpecs)

        Catch ex As Exception
            MessageBox.Show("Error applying setting to clsMASIC: " & ControlChars.NewLine & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try

        Return Not parseError

    End Function

    Private Function ValidateSettings(ByRef objMasic As clsMASIC) As Boolean

        Dim eResponse As DialogResult

        If objMasic.Options.ReporterIons.ReporterIonMassMode <> clsReporterIons.eReporterIonMassModeConstants.CustomOrNone Then
            If objMasic.Options.ReporterIons.ReporterIonMassMode = clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZHighRes Then
                ' Make sure the tolerance is less than 0.03 Da; if not, warn the user
                If objMasic.Options.ReporterIons.ReporterIonToleranceDaDefault > 0.03 Then
                    eResponse = MessageBox.Show("Warning: the Reporter Ion 'm/z Tolerance Half Width' value should be less than 0.03 m/z when using 'iTraq8 for High Res MS/MS' reporter ions.  It is currently " & objMasic.Options.ReporterIons.ReporterIonToleranceDaDefault.ToString("0.000") & " m/z.  If using a low resolution instrument, you should choose the 'iTraq 8 for Low Res MS/MS' mode.  Continue anyway?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)

                    If eResponse <> DialogResult.Yes Then
                        Return False
                    End If
                End If

            ElseIf objMasic.Options.ReporterIons.ReporterIonMassMode = clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZLowRes Then
                ' Make sure the tolerance is at least 0.1 Da; if not, warn the user
                If objMasic.Options.ReporterIons.ReporterIonToleranceDaDefault < 0.1 Then
                    eResponse = MessageBox.Show("Warning: the Reporter Ion 'm/z Tolerance Half Width' value should be at least 0.1 m/z when using 'iTraq8 for Low Res MS/MS' reporter ions.  It is currently " & objMasic.Options.ReporterIons.ReporterIonToleranceDaDefault.ToString("0.000") & " m/z. If using a high resolution instrument, you should choose the 'iTraq 8 for High Res MS/MS' mode.  Continue anyway?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)

                    If eResponse <> DialogResult.Yes Then
                        Return False
                    End If
                End If

            End If
        End If

        Return True
    End Function

#End Region

#Region "Combobox Handlers"

    Private Sub cboMassSpectraNoiseThresholdMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboMassSpectraNoiseThresholdMode.SelectedIndexChanged
        EnableDisableControls()
    End Sub

    Private Sub cboSICNoiseThresholdMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboSICNoiseThresholdMode.SelectedIndexChanged
        EnableDisableControls()
    End Sub
#End Region

#Region "Button Handlers"
    Private Sub cmdClearAllRangeFilters_Click(sender As Object, e As EventArgs) Handles cmdClearAllRangeFilters.Click
        ClearAllRangeFilters()
    End Sub

    Private Sub cmdClearCustomSICList_Click(sender As Object, e As EventArgs) Handles cmdClearCustomSICList.Click
        ClearCustomSICList(True)
    End Sub

    Private Sub cmdCustomSICValuesPopulate_Click(sender As Object, e As EventArgs) Handles cmdCustomSICValuesPopulate.Click
        AutoPopulateCustomSICValues(True)
    End Sub

    Private Sub cmdPasteCustomSICList_Click(sender As Object, e As EventArgs) Handles cmdPasteCustomSICList.Click
        PasteCustomSICValues(False)
    End Sub

    Private Sub cmdStartProcessing_Click(sender As Object, e As EventArgs) Handles cmdStartProcessing.Click
        ProcessFileUsingMASIC()
    End Sub

    Private Sub cmdSelectDatasetLookupFile_Click(sender As Object, e As EventArgs) Handles cmdSelectDatasetLookupFile.Click
        SelectDatasetLookupFile()
    End Sub

    Private Sub cmdSelectCustomSICFile_Click(sender As Object, e As EventArgs) Handles cmdSelectCustomSICFile.Click
        SelectCustomSICFile()
    End Sub

    Private Sub cmdSelectFile_Click(sender As Object, e As EventArgs) Handles cmdSelectFile.Click
        SelectInputFile()
    End Sub

    Private Sub cmdSelectOutputDirectory_Click(sender As Object, e As EventArgs) Handles cmdSelectOutputDirectory.Click
        SelectOutputDirectory()
    End Sub

    Private Sub cmdSetConnectionStringToPNNLServer_Click(sender As Object, e As EventArgs) Handles cmdSetConnectionStringToPNNLServer.Click
        SetConnectionStringToPNNLServer()
    End Sub
#End Region

#Region "Checkbox Events"
    Private Sub chkExportRawDataOnly_CheckedChanged(sender As Object, e As EventArgs) Handles chkExportRawDataOnly.CheckedChanged
        EnableDisableControls()
    End Sub

    Private Sub chkSkipMSMSProcessing_CheckedChanged(sender As Object, e As EventArgs) Handles chkSkipMSMSProcessing.CheckedChanged
        EnableDisableControls()
    End Sub

    Private Sub chkSkipSICAndRawDataProcessing_CheckedChanged(sender As Object, e As EventArgs) Handles chkSkipSICAndRawDataProcessing.CheckedChanged
        EnableDisableControls()
    End Sub

    Private Sub chkExportRawSpectraData_CheckedChanged(sender As Object, e As EventArgs) Handles chkExportRawSpectraData.CheckedChanged
        EnableDisableControls()
    End Sub

    Private Sub chkExportRawDataIncludeMSMS_CheckedChanged(sender As Object, e As EventArgs) Handles chkExportRawDataIncludeMSMS.CheckedChanged
        EnableDisableControls()
    End Sub

    Private Sub chkSaveExtendedStatsFile_CheckedChanged(sender As Object, e As EventArgs) Handles chkSaveExtendedStatsFile.CheckedChanged
        EnableDisableControls()
    End Sub

    Private Sub chkReporterIonStatsEnabled_CheckedChanged(sender As Object, e As EventArgs) Handles chkReporterIonStatsEnabled.CheckedChanged
        AutoToggleReporterIonStatsMode()
    End Sub

    Private Sub chkSaveExtendedStatsFileIncludeStatusLog_CheckedChanged(sender As Object, e As EventArgs) Handles chkSaveExtendedStatsFileIncludeStatusLog.CheckedChanged
        EnableDisableControls()
    End Sub

#End Region

#Region "Radio Button Events"
    Private Sub optUseButterworthSmooth_CheckedChanged(sender As Object, e As EventArgs) Handles optUseButterworthSmooth.CheckedChanged
        EnableDisableControls()
    End Sub

    Private Sub optUseSavitzkyGolaySmooth_CheckedChanged(sender As Object, e As EventArgs) Handles optUseSavitzkyGolaySmooth.CheckedChanged
        EnableDisableControls()
    End Sub

    Private Sub optCustomSICScanToleranceAbsolute_CheckedChanged(sender As Object, e As EventArgs) Handles optCustomSICScanToleranceAbsolute.CheckedChanged
        UpdateCustomSICDataGridTableStyle()
    End Sub

    Private Sub optCustomSICScanToleranceRelative_CheckedChanged(sender As Object, e As EventArgs) Handles optCustomSICScanToleranceRelative.CheckedChanged
        UpdateCustomSICDataGridTableStyle()
    End Sub

    Private Sub optCustomSICScanToleranceAcqTime_CheckedChanged(sender As Object, e As EventArgs) Handles optCustomSICScanToleranceAcqTime.CheckedChanged
        UpdateCustomSICDataGridTableStyle()
    End Sub
#End Region

#Region "Textbox Events"
    Private Sub txtMassSpectraNoiseThresholdIntensity_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtMassSpectraNoiseThresholdIntensity.KeyPress
        TextBoxKeyPressHandler(txtMassSpectraNoiseThresholdIntensity, e, True, True)
    End Sub

    Private Sub txtMassSpectraNoiseFractionLowIntensityDataToAverage_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtMassSpectraNoiseFractionLowIntensityDataToAverage.KeyPress
        TextBoxKeyPressHandler(txtMassSpectraNoiseFractionLowIntensityDataToAverage, e, True, True)
    End Sub

    Private Sub txtBinnedDataIntensityPrecisionPct_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtBinnedDataIntensityPrecisionPct.KeyPress
        TextBoxKeyPressHandler(txtBinnedDataIntensityPrecisionPct, e, True, True)
    End Sub

    Private Sub txtBinSize_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtBinSize.KeyPress
        TextBoxKeyPressHandler(txtBinSize, e, True, True)
    End Sub

    Private Sub txtBinStartX_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtBinStartX.KeyPress
        TextBoxKeyPressHandler(txtBinStartX, e, True, True)
    End Sub

    Private Sub txtBinEndX_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtBinEndX.KeyPress
        TextBoxKeyPressHandler(txtBinEndX, e, True, True)
    End Sub

    Private Sub txtButterworthSamplingFrequency_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtButterworthSamplingFrequency.KeyPress
        TextBoxKeyPressHandler(txtButterworthSamplingFrequency, e, True, True)
    End Sub

    Private Sub txtButterworthSamplingFrequency_Validating(sender As Object, e As CancelEventArgs) Handles txtButterworthSamplingFrequency.Validating
        ValidateTextBoxFloat(txtButterworthSamplingFrequency, 0.01, 0.99, 0.25)
    End Sub

    Private Sub txtCustomSICFileDescription_KeyDown(sender As Object, e As KeyEventArgs) Handles txtCustomSICFileDescription.KeyDown
        If e.KeyCode = Keys.A AndAlso e.Control = True Then
            txtCustomSICFileDescription.SelectAll()
        End If
    End Sub

    Private Sub txtCustomSICFileName_TextChanged(sender As Object, e As EventArgs) Handles txtCustomSICFileName.TextChanged
        EnableDisableCustomSICValueGrid()
    End Sub

    Private Sub txtCustomSICScanOrAcqTimeTolerance_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtCustomSICScanOrAcqTimeTolerance.KeyPress
        TextBoxKeyPressHandler(txtCustomSICScanOrAcqTimeTolerance, e, True, True)
    End Sub

    Private Sub txtDatasetID_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtDatasetID.KeyPress
        TextBoxKeyPressHandler(txtDatasetID, e, True, False)
    End Sub

    Private Sub txtExportRawDataIntensityMinimum_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtExportRawDataIntensityMinimum.KeyPress
        TextBoxKeyPressHandler(txtExportRawDataIntensityMinimum, e, True, True)
    End Sub

    Private Sub txtExportRawDataMaxIonCountPerScan_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtExportRawDataMaxIonCountPerScan.KeyPress
        TextBoxKeyPressHandler(txtExportRawDataMaxIonCountPerScan, e)
    End Sub

    Private Sub txtExportRawDataSignalToNoiseRatioMinimum_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtExportRawDataSignalToNoiseRatioMinimum.KeyPress
        TextBoxKeyPressHandler(txtExportRawDataSignalToNoiseRatioMinimum, e, True, True)
    End Sub

    Private Sub txtInitialPeakWidthScansMaximum_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtInitialPeakWidthScansMaximum.KeyPress
        TextBoxKeyPressHandler(txtInitialPeakWidthScansMaximum, e)
    End Sub

    Private Sub txtInitialPeakWidthScansScaler_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtInitialPeakWidthScansScaler.KeyPress
        TextBoxKeyPressHandler(txtInitialPeakWidthScansScaler, e)
    End Sub

    Private Sub txtIntensityThresholdAbsoluteMinimum_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtIntensityThresholdAbsoluteMinimum.KeyPress
        TextBoxKeyPressHandler(txtIntensityThresholdAbsoluteMinimum, e, True, True)
    End Sub

    Private Sub txtIntensityThresholdFractionMax_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtIntensityThresholdFractionMax.KeyPress
        TextBoxKeyPressHandler(txtIntensityThresholdFractionMax, e, True, True)
    End Sub

    Private Sub txtMaxAllowedUpwardSpikeFractionMax_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtMaxAllowedUpwardSpikeFractionMax.KeyPress
        TextBoxKeyPressHandler(txtMaxAllowedUpwardSpikeFractionMax, e, True, True)
    End Sub

    Private Sub txtMaxDistanceScansNoOverlap_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtMaxDistanceScansNoOverlap.KeyPress
        TextBoxKeyPressHandler(txtMaxDistanceScansNoOverlap, e)
    End Sub

    Private Sub txtMaximumBinCount_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtMaximumBinCount.KeyPress
        TextBoxKeyPressHandler(txtMaximumBinCount, e)
    End Sub

    Private Sub txtMaxPeakWidthMinutesBackward_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtMaxPeakWidthMinutesBackward.KeyPress
        TextBoxKeyPressHandler(txtMaxPeakWidthMinutesBackward, e, True, True)
    End Sub

    Private Sub txtMaxPeakWidthMinutesForward_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtMaxPeakWidthMinutesForward.KeyPress
        TextBoxKeyPressHandler(txtMaxPeakWidthMinutesForward, e, True, True)
    End Sub

    Private Sub txtSICNoiseThresholdIntensity_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtSICNoiseThresholdIntensity.KeyPress
        TextBoxKeyPressHandler(txtSICNoiseThresholdIntensity, e, True, True)
    End Sub

    Private Sub txtSICNoiseFractionLowIntensityDataToAverage_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtSICNoiseFractionLowIntensityDataToAverage.KeyPress
        TextBoxKeyPressHandler(txtSICNoiseFractionLowIntensityDataToAverage, e, True, True)
    End Sub

    Private Sub txtSavitzkyGolayFilterOrder_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtSavitzkyGolayFilterOrder.KeyPress
        TextBoxKeyPressHandler(txtSavitzkyGolayFilterOrder, e)
    End Sub

    Private Sub txtSavitzkyGolayFilterOrder_Validating(sender As Object, e As CancelEventArgs) Handles txtSavitzkyGolayFilterOrder.Validating
        ValidateTextboxInt(txtSavitzkyGolayFilterOrder, 0, 20, 0)
    End Sub

    Private Sub txtSICTolerance_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtSICTolerance.KeyPress
        TextBoxKeyPressHandler(txtSICTolerance, e, True, True)
    End Sub

    Private Sub txtSimilarIonMZToleranceHalfWidth_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtSimilarIonMZToleranceHalfWidth.KeyPress
        TextBoxKeyPressHandler(txtSimilarIonMZToleranceHalfWidth, e, True, True)
    End Sub

    Private Sub txtSimilarIonToleranceHalfWidthMinutes_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtSimilarIonToleranceHalfWidthMinutes.KeyPress
        TextBoxKeyPressHandler(txtSimilarIonToleranceHalfWidthMinutes, e, True, True)
    End Sub

    Private Sub txtSpectrumSimilarityMinimum_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtSpectrumSimilarityMinimum.KeyPress
        TextBoxKeyPressHandler(txtSpectrumSimilarityMinimum, e, True, True)
    End Sub

    Private Sub txtScanEnd_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtScanEnd.KeyPress
        TextBoxKeyPressHandler(txtScanEnd, e, True, False)
    End Sub

    Private Sub txtScanStart_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtScanStart.KeyPress
        TextBoxKeyPressHandler(txtScanStart, e, True, False)
    End Sub

    Private Sub txtTimeEnd_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtTimeEnd.KeyPress
        TextBoxKeyPressHandler(txtTimeEnd, e, True, True)
    End Sub

    Private Sub txtTimeStart_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtTimeStart.KeyPress
        TextBoxKeyPressHandler(txtTimeStart, e, True, True)
    End Sub
#End Region

#Region "Menu Handlers"
    Private Sub mnuFileSelectInputFile_Click(sender As Object, e As EventArgs) Handles mnuFileSelectInputFile.Click
        SelectInputFile()
    End Sub

    Private Sub mnuFileSelectOutputDirectory_Click(sender As Object, e As EventArgs) Handles mnuFileSelectOutputDirectory.Click
        SelectOutputDirectory()
    End Sub

    Private Sub mnuFileLoadOptions_Click(sender As Object, e As EventArgs) Handles mnuFileLoadOptions.Click
        IniFileLoadOptions(False)
    End Sub

    Private Sub mnuFileSaveOptions_Click(sender As Object, e As EventArgs) Handles mnuFileSaveOptions.Click
        IniFileSaveOptions()
    End Sub

    Private Sub mnuFileExit_Click(sender As Object, e As EventArgs) Handles mnuFileExit.Click
        Me.Close()
    End Sub

    Private Sub mnuEditProcessFile_Click(sender As Object, e As EventArgs) Handles mnuEditProcessFile.Click
        ProcessFileUsingMASIC()
    End Sub

    Private Sub mnuEditResetOptions_Click(sender As Object, e As EventArgs) Handles mnuEditResetOptions.Click
        ResetToDefaults(True)
    End Sub

    Private Sub mnuEditSaveDefaultOptions_Click(sender As Object, e As EventArgs) Handles mnuEditSaveDefaultOptions.Click
        IniFileSaveDefaultOptions()
    End Sub

    Private Sub mnuHelpAbout_Click(sender As Object, e As EventArgs) Handles mnuHelpAbout.Click
        ShowAboutBox()
    End Sub

#End Region

#Region "Form and Masic Class Events"
    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Note that InitializeControls() is called in Sub New()
    End Sub

    Private Sub frmMain_Closing(sender As Object, e As CancelEventArgs) Handles MyBase.Closing
        IniFileSaveOptions(GetSettingsFilePath(), True)
    End Sub

    Private Sub MASIC_ProgressUpdate(taskDescription As String, percentComplete As Single)
        If Not mProgressForm Is Nothing Then
            mProgressForm.UpdateCurrentTask(mMasic.ProgressStepDescription)
            mProgressForm.UpdateProgressBar(percentComplete)
            If mProgressForm.KeyPressAbortProcess Then
                mMasic.AbortProcessingNow()
            End If
            Application.DoEvents()
        End If
    End Sub

    Private Sub MASIC_ProgressResetKeypressAbort()
        If Not mProgressForm Is Nothing Then
            mProgressForm.ResetKeyPressAbortProcess()
        End If
    End Sub

    Private Sub MASIC_ProgressSubtaskChanged()
        If Not mProgressForm Is Nothing Then
            mProgressForm.UpdateCurrentSubTask(mMasic.SubtaskDescription)
            mProgressForm.UpdateSubtaskProgressBar(mMasic.SubtaskProgressPercentComplete)
            If mProgressForm.KeyPressAbortProcess Then
                mMasic.AbortProcessingNow()
            End If
            Application.DoEvents()
        End If
    End Sub

    Private Sub StatusEventHandler(message As String)
        AppendToLog(EventLogEntryType.Information, message)
        Console.WriteLine(message)
    End Sub

    Private Sub DebugEventHandler(message As String)
        ConsoleMsgUtils.ShowDebug(message)
    End Sub

    Private Sub ErrorEventHandler(message As String, ex As Exception)
        AppendToLog(EventLogEntryType.Error, message)
        ConsoleMsgUtils.ShowError(message, ex)
    End Sub

    Private Sub WarningEventHandler(message As String)
        AppendToLog(EventLogEntryType.Warning, message)
        ConsoleMsgUtils.ShowWarning(message)
    End Sub
#End Region

    Private Sub cboReporterIonMassMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboReporterIonMassMode.SelectedIndexChanged
        AutoToggleReporterIonStatsEnabled()
    End Sub
End Class
