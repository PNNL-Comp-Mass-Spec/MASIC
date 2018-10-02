Option Strict On

' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started October 11, 2003
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

' E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
' Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
' -------------------------------------------------------------------------------
'
' Licensed under the 2-Clause BSD License; you may Not use this file except
' in compliance with the License.  You may obtain a copy of the License at
' https://opensource.org/licenses/BSD-2-Clause


Imports System.ComponentModel
Imports System.Threading
Imports MASIC.DataInput
Imports MASICPeakFinder.clsMASICPeakFinder
Imports PRISM
Imports PRISM.FileProcessor
Imports ProgressFormNET
Imports SharedVBNetRoutines
Imports SharedVBNetRoutines.VBNetRoutines
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

    Private Const CUSTOM_SIC_VALUES_DATATABLE As String = "PeakMatchingThresholds"

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
      dblMZ As Double,
      dblMZToleranceDa As Double,
      sngScanOrAcqTimeCenter As Single,
      sngScanOrAcqTimeTolerance As Single,
      strComment As String,
      Optional ByRef blnExistingRowFound As Boolean = False)

        Dim myDataRow As DataRow

        With mCustomSICValuesDataset.Tables(CUSTOM_SIC_VALUES_DATATABLE)

            For Each myDataRow In .Rows
                With myDataRow
                    If Math.Abs(CDbl(.Item(0)) - dblMZ) < Single.Epsilon And Math.Abs(CSng(.Item(1)) - sngScanOrAcqTimeCenter) < Single.Epsilon Then
                        blnExistingRowFound = True
                        Exit For
                    End If
                End With
            Next myDataRow

            If strComment Is Nothing Then strComment = String.Empty

            If Not blnExistingRowFound Then
                myDataRow = .NewRow
                myDataRow(0) = Math.Round(dblMZ, 4)
                myDataRow(1) = Math.Round(dblMZToleranceDa, 4)
                myDataRow(2) = Math.Round(sngScanOrAcqTimeCenter, 6)
                myDataRow(3) = Math.Round(sngScanOrAcqTimeTolerance, 6)
                myDataRow(4) = strComment
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

    Private Sub AutoPopulateCustomSICValues(blnConfirmReplaceExistingResults As Boolean)

        Dim intIndex As Integer

        Dim dblDefaultMZTolerance As Double
        Dim sngDefaultScanOrAcqTimeTolerance As Single

        GetCurrentCustomSICTolerances(dblDefaultMZTolerance, sngDefaultScanOrAcqTimeTolerance)
        If sngDefaultScanOrAcqTimeTolerance > 1 Then
            sngDefaultScanOrAcqTimeTolerance = 0.6
        End If

        If ClearCustomSICList(blnConfirmReplaceExistingResults) Then
            ' The default values use relative times, so make sure that mode is enabled
            SetCustomSICToleranceType(clsCustomSICList.eCustomSICScanTypeConstants.Relative)

            txtCustomSICScanOrAcqTimeTolerance.Text = sngDefaultScanOrAcqTimeTolerance.ToString

            For intIndex = 0 To mDefaultCustomSICList.Length - 1
                With mDefaultCustomSICList(intIndex)
                    AddCustomSICRow(.MZ, dblDefaultMZTolerance, .ScanCenter, sngDefaultScanOrAcqTimeTolerance, .Comment)
                End With
            Next

        End If

    End Sub

    Private Sub CatchUnrequestedHeightChange()
        Static blnUpdating As Boolean

        If Not blnUpdating Then
            If mHeightAdjustForce <> 0 AndAlso DateTime.UtcNow.Subtract(mHeightAdjustTime).TotalSeconds <= 5 Then
                Try
                    blnUpdating = True
                    Me.Height = mHeightAdjustForce
                    mHeightAdjustForce = 0
                    mHeightAdjustTime = #1/1/1900#
                Catch ex As Exception
                Finally
                    blnUpdating = False
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

    Private Function ClearCustomSICList(blnConfirmReplaceExistingResults As Boolean) As Boolean
        ' Returns true if the CUSTOM_SIC_VALUES_DATATABLE is empty or if it was cleared
        ' Returns false if the user is queried about clearing and they do not click Yes

        Dim eResult As DialogResult
        Dim blnSuccess As Boolean

        blnSuccess = False
        With mCustomSICValuesDataset.Tables(CUSTOM_SIC_VALUES_DATATABLE)
            If .Rows.Count > 0 Then
                If blnConfirmReplaceExistingResults Then
                    eResult = MessageBox.Show("Are you sure you want to clear the Custom SIC list?", "Clear Custom SICs", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
                End If

                If eResult = DialogResult.Yes OrElse Not blnConfirmReplaceExistingResults Then
                    .Rows.Clear()
                    blnSuccess = True
                End If
            Else
                blnSuccess = True
            End If
        End With

        Return blnSuccess
    End Function

    Private Function ConfirmPaths() As Boolean
        If txtInputFilePath.TextLength = 0 Then
            MessageBox.Show("Please define an input file path", "Missing Value", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            txtInputFilePath.Focus()
            Return False
        ElseIf txtOutputFolderPath.TextLength = 0 Then
            MessageBox.Show("Please define an output folder path", "Missing Value", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            txtOutputFolderPath.Focus()
            Return False
        Else
            Return True
        End If
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
        Dim Msg As String

        Msg = String.Empty
        Msg &= "When Export All Spectra Data Points is enabled, a separate file is created containing the raw data points (scan number, m/z, and intensity), using the specified file format. "
        Msg &= "If Export MS/MS Spectra is enabled, then the fragmentation spectra are included, in addition to the survey scan spectra (MS1 scans). "
        Msg &= "If MS/MS spectra are not included, then one can optionally renumber the survey scan spectra so that they increase in steps of 1, regardless of the number of MS/MS scans between each survey scan. "
        Msg &= "The Minimum Intensity and Maximum Ion Count options allow you to limit the number of data points exported for each spectrum."

        lblRawDataExportOverview.Text = Msg

        Msg = String.Empty
        Msg &= "These options control how the selected ion chromatogram (SIC) is created for each parent ion mass or custom SIC search mass. "
        Msg &= "The data in the survey scan spectra (MS1 scans) are searched +/- the SIC Tolerance, looking forward and backward in time until "
        Msg &= "the intensity of the matching data 1) falls below the Intensity Threshold Fraction Max Peak value, 2) falls below the Intensity "
        Msg &= "Threshold Absolute Minimum, or 3) spans more than the Maximum Peak Width forward or backward limits defined."

        lblSICOptionsOverview.Text = Msg


        Msg = String.Empty
        Msg &= "When processing Thermo-Finnigan MRM data files, a file named _MRMSettings.txt will be created listing the "
        Msg &= "parent and daughter m/z values monitored via SRM. "
        Msg &= "You can optionally export detailed MRM intensity data using these options:"
        lblMRMInfo.Text = Msg


        Msg = String.Empty
        Msg &= "Select a comma or tab delimited file to read custom SIC search values from, "
        Msg &= "or define them in the Custom SIC Values table below.  If using the file, "
        Msg &= "allowed column names are: " & clsCustomSICListReader.GetCustomMZFileColumnHeaders() & ".  "
        Msg &= "Note: use " &
          clsCustomSICListReader.CUSTOM_SIC_COLUMN_SCAN_TIME & " and " &
          clsCustomSICListReader.CUSTOM_SIC_COLUMN_TIME_TOLERANCE & " only when specifying "

        Msg &= "acquisition time-based values.  When doing so, do not include " &
          clsCustomSICListReader.CUSTOM_SIC_COLUMN_SCAN_CENTER & " and " &
          clsCustomSICListReader.CUSTOM_SIC_COLUMN_SCAN_TOLERNACE & "."

        txtCustomSICFileDescription.Text = Msg

    End Sub

    Private Sub EnableDisableControls()

        Dim blnRawExportEnabled As Boolean

        Dim blnCreateSICsAndRawData As Boolean = Not chkSkipSICAndRawDataProcessing.Checked
        Dim blnMSMSProcessingEnabled As Boolean = Not chkSkipMSMSProcessing.Checked
        Dim blnExportRawDataOnly As Boolean = chkExportRawDataOnly.Checked And chkExportRawSpectraData.Checked

        chkSkipMSMSProcessing.Enabled = blnCreateSICsAndRawData
        chkExportRawDataOnly.Enabled = blnCreateSICsAndRawData And chkExportRawSpectraData.Checked

        fraExportAllSpectraDataPoints.Enabled = blnCreateSICsAndRawData

        fraSICNoiseThresholds.Enabled = blnCreateSICsAndRawData And Not blnExportRawDataOnly
        fraPeakFindingOptions.Enabled = fraSICNoiseThresholds.Enabled
        fraSmoothingOptions.Enabled = fraSICNoiseThresholds.Enabled
        fraSICSearchThresholds.Enabled = fraSICNoiseThresholds.Enabled

        fraMassSpectraNoiseThresholds.Enabled = blnCreateSICsAndRawData

        fraBinningIntensityOptions.Enabled = blnCreateSICsAndRawData And blnMSMSProcessingEnabled And Not blnExportRawDataOnly
        fraBinningMZOptions.Enabled = fraBinningIntensityOptions.Enabled
        fraSpectrumSimilarityOptions.Enabled = fraBinningIntensityOptions.Enabled

        fraCustomSICControls.Enabled = blnCreateSICsAndRawData And Not blnExportRawDataOnly
        dgCustomSICValues.Enabled = blnCreateSICsAndRawData And Not blnExportRawDataOnly

        blnRawExportEnabled = chkExportRawSpectraData.Checked

        cboExportRawDataFileFormat.Enabled = blnRawExportEnabled
        chkExportRawDataIncludeMSMS.Enabled = blnRawExportEnabled

        If chkExportRawDataIncludeMSMS.Checked Then
            chkExportRawDataRenumberScans.Enabled = False
        Else
            chkExportRawDataRenumberScans.Enabled = blnRawExportEnabled
        End If

        txtExportRawDataSignalToNoiseRatioMinimum.Enabled = blnRawExportEnabled
        txtExportRawDataMaxIonCountPerScan.Enabled = blnRawExportEnabled
        txtExportRawDataIntensityMinimum.Enabled = blnRawExportEnabled


        If cboSICNoiseThresholdMode.SelectedIndex = eNoiseThresholdModes.AbsoluteThreshold Then
            txtSICNoiseThresholdIntensity.Enabled = True
            txtSICNoiseFractionLowIntensityDataToAverage.Enabled = False
        ElseIf cboSICNoiseThresholdMode.SelectedIndex = eNoiseThresholdModes.TrimmedMeanByAbundance Or
         cboSICNoiseThresholdMode.SelectedIndex = eNoiseThresholdModes.TrimmedMeanByCount Or
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
        ElseIf cboMassSpectraNoiseThresholdMode.SelectedIndex = eNoiseThresholdModes.TrimmedMeanByAbundance Or
          cboMassSpectraNoiseThresholdMode.SelectedIndex = eNoiseThresholdModes.TrimmedMeanByCount Or
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
        txtStatusLogKeyNameFilterList.Enabled = chkSaveExtendedStatsFile.Checked And chkSaveExtendedStatsFileIncludeStatusLog.Checked

        chkConsolidateConstantExtendedHeaderValues.Enabled = chkSaveExtendedStatsFile.Checked

        EnableDisableCustomSICValueGrid()

    End Sub

    Private Sub EnableDisableCustomSICValueGrid()
        Dim blnEnableGrid As Boolean

        If txtCustomSICFileName.TextLength > 0 Then
            blnEnableGrid = False
            dgCustomSICValues.CaptionText = "Custom SIC Values will be read from the file defined above"
        Else
            blnEnableGrid = True
            dgCustomSICValues.CaptionText = "Custom SIC Values"
        End If

        cmdPasteCustomSICList.Enabled = blnEnableGrid
        cmdCustomSICValuesPopulate.Enabled = blnEnableGrid
        cmdClearCustomSICList.Enabled = blnEnableGrid
        dgCustomSICValues.Enabled = blnEnableGrid

    End Sub

    Private Sub frmMain_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        CatchUnrequestedHeightChange()
    End Sub

    Private Sub GetCurrentCustomSICTolerances(ByRef dblDefaultMZTolerance As Double, ByRef sngDefaultScanOrAcqTimeTolerance As Single)

        Try
            dblDefaultMZTolerance = Double.Parse(txtSICTolerance.Text)

            If optSICTolerancePPM.Checked Then
                dblDefaultMZTolerance = clsUtilities.PPMToMass(dblDefaultMZTolerance, 1000)
            End If

        Catch ex As Exception
            dblDefaultMZTolerance = 0.6
        End Try

        Try
            sngDefaultScanOrAcqTimeTolerance = Single.Parse(txtCustomSICScanOrAcqTimeTolerance.Text)
        Catch ex As Exception
            sngDefaultScanOrAcqTimeTolerance = 0
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

    Private Sub IniFileLoadOptions(blnUpdateIOPaths As Boolean)
        ' Prompts the user to select a file to load the options from

        Dim strFilePath As String

        Dim objOpenFile As New OpenFileDialog

        strFilePath = mXmlSettingsFilePath

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

            If strFilePath.Length > 0 Then
                Try
                    .InitialDirectory = Directory.GetParent(strFilePath).ToString
                Catch
                    .InitialDirectory = ProcessFilesBase.GetAppFolderPath()
                End Try
            Else
                .InitialDirectory = ProcessFilesBase.GetAppFolderPath()
            End If

            If File.Exists(strFilePath) Then
                .FileName = Path.GetFileName(strFilePath)
            End If

            .Title = "Specify file to load options from"

            .ShowDialog()
            If .FileName.Length > 0 Then
                mXmlSettingsFilePath = .FileName

                IniFileLoadOptions(mXmlSettingsFilePath, blnUpdateIOPaths)
            End If
        End With

    End Sub

    Private Sub IniFileLoadOptions(strFilePath As String, blnUpdateIOPaths As Boolean)
        ' Loads options from the given file

        Try

            ' Utilize clsMASIC's built-in LoadParameters function, then call ResetToDefaults
            Dim objMasic = New clsMASIC()

            Dim success = objMasic.LoadParameterFileSettings(strFilePath)
            If Not success Then
                MessageBox.Show("LoadParameterFileSettings returned false for: " & Path.GetFileName(strFilePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If

            ResetToDefaults(False, objMasic)

            ' Sleep for 100 msec, just to be safe
            Thread.Sleep(100)

            ' Now load some custom options that aren't loaded by clsMASIC
            Dim objXmlFile = New XmlSettingsFileAccessor()

            With objXmlFile
                ' Pass True to .LoadSettings() to turn off case sensitive matching
                .LoadSettings(strFilePath, False)

                Try
                    txtDatasetLookupFilePath.Text = .GetParam(clsMASICOptions.XML_SECTION_DATABASE_SETTINGS, "DatasetLookupFilePath", txtDatasetLookupFilePath.Text)
                    Try
                        If Not File.Exists(txtDatasetLookupFilePath.Text) Then
                            txtDatasetLookupFilePath.Text = String.Empty
                        End If
                    Catch ex As Exception
                        ' Ignore any errors here
                    End Try

                    If blnUpdateIOPaths Then
                        txtInputFilePath.Text = .GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "InputFilePath", txtInputFilePath.Text)
                    End If

                    Me.Width = .GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowWidth", Me.Width)
                    Me.Height = .GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowHeight", Me.Height)

                    If blnUpdateIOPaths Then
                        txtOutputFolderPath.Text = .GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "LastDirectory", txtOutputFolderPath.Text)
                    End If

                    If txtOutputFolderPath.TextLength = 0 Then
                        txtOutputFolderPath.Text = ProcessFilesBase.GetAppFolderPath()
                    End If

                    mPreferredInputFileExtension = .GetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "PreferredInputFileExtension", mPreferredInputFileExtension)

                Catch ex As Exception
                    MessageBox.Show("Invalid parameter in settings file: " & Path.GetFileName(strFilePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                End Try
            End With

        Catch ex As Exception
            MessageBox.Show("Error loading settings from file: " & strFilePath & "; " & ControlChars.NewLine &
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

        Dim strFilePath As String

        Dim objSaveFile As New SaveFileDialog

        strFilePath = mXmlSettingsFilePath

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

            If strFilePath.Length > 0 Then
                Try
                    .InitialDirectory = Directory.GetParent(strFilePath).ToString
                Catch
                    .InitialDirectory = ProcessFilesBase.GetAppFolderPath()
                End Try
            Else
                .InitialDirectory = ProcessFilesBase.GetAppFolderPath()
            End If

            If File.Exists(strFilePath) Then
                .FileName = Path.GetFileName(strFilePath)
            End If

            .Title = "Specify file to save options to"

            .ShowDialog()
            If .FileName.Length > 0 Then
                mXmlSettingsFilePath = .FileName

                IniFileSaveOptions(mXmlSettingsFilePath, False)
            End If
        End With

    End Sub

    Private Sub IniFileSaveOptions(strFilePath As String, Optional blnSaveWindowDimensionsOnly As Boolean = False)
        Dim objXmlFile As New XmlSettingsFileAccessor

        Dim objMasic As clsMASIC

        Try
            If Not blnSaveWindowDimensionsOnly Then
                objMasic = New clsMASIC()

                UpdateMasicSettings(objMasic)

                objMasic.Options.SaveParameterFileSettings(strFilePath)

                ' Sleep for 100 msec, just to be safe
                Thread.Sleep(100)
            End If

            With objXmlFile
                ' Pass True to .LoadSettings() here so that newly made Xml files will have the correct capitalization
                .LoadSettings(strFilePath, True)

                Try
                    If Not blnSaveWindowDimensionsOnly Then
                        Try
                            If File.Exists(txtDatasetLookupFilePath.Text) Then
                                .SetParam(clsMASICOptions.XML_SECTION_DATABASE_SETTINGS, "DatasetLookupFilePath", txtDatasetLookupFilePath.Text)
                            End If
                        Catch ex As Exception
                            ' Ignore any errors here
                        End Try

                        .SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "InputFilePath", txtInputFilePath.Text)
                    End If

                    .SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "LastDirectory", txtOutputFolderPath.Text)
                    .SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "PreferredInputFileExtension", mPreferredInputFileExtension)

                    .SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowWidth", Me.Width)
                    .SetParam(clsMASICOptions.XML_SECTION_IMPORT_OPTIONS, "WindowHeight", Me.Height)

                Catch ex As Exception
                    MessageBox.Show("Error storing parameter in settings file: " & Path.GetFileName(strFilePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                End Try

                .SaveSettings()
            End With
        Catch ex As Exception
            MessageBox.Show("Error saving settings to file: " & strFilePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
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

        ' Make the Peak Matching Thresholds datatable
        Dim dtCustomSICValues = New DataTable(CUSTOM_SIC_VALUES_DATATABLE)

        ' Add the columns to the datatable
        ADONetRoutines.AppendColumnDoubleToTable(dtCustomSICValues, COL_NAME_MZ)
        ADONetRoutines.AppendColumnDoubleToTable(dtCustomSICValues, COL_NAME_MZ_TOLERANCE)
        ADONetRoutines.AppendColumnDoubleToTable(dtCustomSICValues, COL_NAME_SCAN_CENTER)
        ADONetRoutines.AppendColumnDoubleToTable(dtCustomSICValues, COL_NAME_SCAN_TOLERANCE)
        ADONetRoutines.AppendColumnStringToTable(dtCustomSICValues, COL_NAME_SCAN_COMMENT, String.Empty)
        ADONetRoutines.AppendColumnIntegerToTable(dtCustomSICValues, COL_NAME_CUSTOM_SIC_VALUE_ROW_ID, 0, True, True)

        With dtCustomSICValues
            Dim PrimaryKeyColumn = New DataColumn() { .Columns(COL_NAME_CUSTOM_SIC_VALUE_ROW_ID)}
            .PrimaryKey = PrimaryKeyColumn
        End With

        ' Instantiate the dataset
        mCustomSICValuesDataset = New DataSet(CUSTOM_SIC_VALUES_DATATABLE)

        ' Add the new DataTable to the DataSet.
        mCustomSICValuesDataset.Tables.Add(dtCustomSICValues)

        ' Bind the DataSet to the DataGrid
        With dgCustomSICValues
            .DataSource = mCustomSICValuesDataset
            .DataMember = CUSTOM_SIC_VALUES_DATATABLE
        End With

        ' Update the grid's table style
        UpdateCustomSICDataGridTableStyle()

        ' Populate the table
        AutoPopulateCustomSICValues(False)

    End Sub

    Private Sub PasteCustomSICValues(blnClearList As Boolean)

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

        Dim strData = CType(objData.GetData(DataFormats.StringFormat, True), String)

        ' Split strData on carriage return or line feed characters
        ' Lines that end in CrLf will give two separate lines; one with with the text, and one blank; that's OK
        Dim strLines = strData.Split(LineDelimiters, 50000)

        If strLines.Length = 0 Then
            Return
        End If

        Dim dblDefaultMZTolerance As Double
        Dim sngDefaultScanOrAcqTimeTolerance As Single

        GetCurrentCustomSICTolerances(dblDefaultMZTolerance, sngDefaultScanOrAcqTimeTolerance)

        If blnClearList Then
            If Not ClearCustomSICList(True) Then Return
        End If

        Dim intRowsAlreadyPresent = 0
        Dim intRowsSkipped = 0

        For Each dataLine In strLines
            If String.IsNullOrWhiteSpace(dataLine) Then
                Continue For
            End If

            Dim strColumns = dataLine.Split(ColumnDelimiters, 5)
            If strColumns.Length < 2 Then
                intRowsSkipped += 1
                Continue For
            End If

            Try
                Dim dblMZ As Double = 0
                Dim sngScanOrAcqTime As Single
                Dim strComment As String = String.Empty
                Dim dblMZToleranceDa = dblDefaultMZTolerance
                Dim sngScanOrAcqTimeTolerance = sngDefaultScanOrAcqTimeTolerance

                If strColumns.Length = 2 Then
                    ' Assume pasted data is m/z and scan
                    dblMZ = Double.Parse(strColumns(0))
                    sngScanOrAcqTime = Single.Parse(strColumns(1))

                ElseIf strColumns.Length >= 3 AndAlso strColumns(2).Length > 0 AndAlso
                    Not IsNumber(strColumns(2).Chars(0)) Then
                    ' Assume pasted data is m/z, scan, and comment
                    dblMZ = Double.Parse(strColumns(0))
                    sngScanOrAcqTime = Single.Parse(strColumns(1))
                    strComment = strColumns(2)

                ElseIf strColumns.Length > 2 Then
                    ' Assume pasted data is m/z, m/z tolerance, scan, scan tolerance, and comment
                    dblMZ = Double.Parse(strColumns(0))
                    dblMZToleranceDa = Double.Parse(strColumns(1))
                    If Math.Abs(dblMZToleranceDa) < Single.Epsilon Then
                        dblMZToleranceDa = dblDefaultMZTolerance
                    End If

                    sngScanOrAcqTime = Single.Parse(strColumns(2))

                    If strColumns.Length >= 4 Then
                        sngScanOrAcqTimeTolerance = Single.Parse(strColumns(3))
                    Else
                        sngScanOrAcqTimeTolerance = sngDefaultScanOrAcqTimeTolerance
                    End If

                    If strColumns.Length >= 5 Then
                        strComment = strColumns(4)
                    Else
                        strComment = String.Empty
                    End If

                End If

                If dblMZ > 0 Then
                    Dim blnExistingRowFound = False
                    AddCustomSICRow(dblMZ, dblMZToleranceDa, sngScanOrAcqTime, sngScanOrAcqTimeTolerance, strComment,
                                    blnExistingRowFound)

                    If blnExistingRowFound Then
                        intRowsAlreadyPresent += 1
                    End If
                End If

            Catch ex As Exception
                ' Skip this row
                intRowsSkipped += 1
            End Try

        Next

        If intRowsAlreadyPresent > 0 Then
            Dim strMessage As String
            If intRowsAlreadyPresent = 1 Then
                strMessage = "1 row of thresholds was"
            Else
                strMessage = intRowsAlreadyPresent.ToString() & " rows of thresholds were"
            End If

            MessageBox.Show(strMessage & " already present in the table; duplicate rows are not allowed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End If

        If intRowsSkipped > 0 Then
            Dim strMessage As String
            If intRowsSkipped = 1 Then
                strMessage = "1 row was skipped because it"
            Else
                strMessage = intRowsSkipped.ToString() & " rows were skipped because they"
            End If

            MessageBox.Show(strMessage & " didn't contain two columns of numeric data.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
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


            End With
            .SelectedIndex = clsReporterIons.eReporterIonMassModeConstants.CustomOrNone
        End With

    End Sub

    Private Sub ProcessFileUsingMASIC()

        Dim strOutputFolderPath As String
        Dim blnSuccess As Boolean

        Dim dtStartTime As DateTime

        If Not mWorking AndAlso ConfirmPaths() Then
            Try

                txtLogMessages.ResetText()

                ' Configure settings
                blnSuccess = UpdateMasicSettings(mMasic)
                If Not blnSuccess Then Exit Try

                ' Validate settings
                blnSuccess = ValidateSettings(mMasic)
                If Not blnSuccess Then Exit Try

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

                    dtStartTime = DateTime.UtcNow

                    strOutputFolderPath = txtOutputFolderPath.Text
                    blnSuccess = .ProcessFile(txtInputFilePath.Text, strOutputFolderPath)

                    Cursor.Current = Cursors.Default

                    If .Options.AbortProcessing Then
                        MessageBox.Show("Cancelled processing", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End If

                    If blnSuccess Then
                        ' Grab the status message, but insert a carriage return directly after "in folder:"
                        MessageBox.Show(.StatusMessage.Replace("in folder:", "in folder:" & ControlChars.NewLine) & ControlChars.NewLine & "Elapsed time: " & StringUtilities.DblToString(DateTime.UtcNow.Subtract(dtStartTime).TotalSeconds, 2) & " sec", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information)
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

    Private Sub ResetToDefaults(blnConfirm As Boolean, Optional ByRef objMasic As clsMASIC = Nothing)

        Dim eResponse As DialogResult
        Dim blnExistingMasicObjectUsed As Boolean

        If blnConfirm Then
            eResponse = MessageBox.Show("Are you sure you want to reset all settings to their default values?", "Reset to Defaults", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1)
            If eResponse <> DialogResult.Yes Then Exit Sub
        End If

        If objMasic Is Nothing Then
            objMasic = New clsMASIC()
            blnExistingMasicObjectUsed = False
        Else
            blnExistingMasicObjectUsed = True
        End If

        Me.Width = 710
        Me.Height = 560

        ' File Paths and Import Options
        Try
            If txtOutputFolderPath.TextLength = 0 OrElse Not Directory.Exists(txtOutputFolderPath.Text) Then
                txtOutputFolderPath.Text = ProcessFilesBase.GetAppFolderPath()
            End If
        Catch ex As Exception
            If blnConfirm Then
                MessageBox.Show("Exception occurred while validating txtOutputFolderPath.Text: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
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
                txtDatasetNumber.Text = "0"
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
                Dim blnSICToleranceIsPPM As Boolean
                Dim dblSICTolerance = .SICOptions.GetSICTolerance(blnSICToleranceIsPPM)

                txtSICTolerance.Text = StringUtilities.DblToString(dblSICTolerance, 6)
                If blnSICToleranceIsPPM Then
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
                mCacheOptions.FolderPath = .FolderPath
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
            If blnConfirm Then
                MessageBox.Show("Error resetting values to defaults: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If
        End Try

        If Not blnExistingMasicObjectUsed Then
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
                    .InitialDirectory = ProcessFilesBase.GetAppFolderPath()
                End Try
            Else
                .InitialDirectory = ProcessFilesBase.GetAppFolderPath()
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
        Dim strExtension As String

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

            strExtension = ".txt"

            If txtCustomSICFileName.TextLength > 0 Then
                strExtension = Path.GetExtension(txtCustomSICFileName.Text)
            End If

            Select Case strExtension.ToLower
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
                    .InitialDirectory = ProcessFilesBase.GetAppFolderPath()
                End Try
            Else
                .InitialDirectory = ProcessFilesBase.GetAppFolderPath()
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
        Dim strExtension As String

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

            strExtension = String.Copy(mPreferredInputFileExtension)

            If txtInputFilePath.TextLength > 0 Then
                strExtension = Path.GetExtension(txtInputFilePath.Text)
            End If

            Select Case strExtension.ToLower
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
                    .InitialDirectory = ProcessFilesBase.GetAppFolderPath()
                End Try
            Else
                .InitialDirectory = ProcessFilesBase.GetAppFolderPath()
            End If

            .Title = "Select input file"

            .ShowDialog()
            If .FileName.Length > 0 Then
                txtInputFilePath.Text = .FileName
                mPreferredInputFileExtension = Path.GetExtension(.FileName)
            End If
        End With

    End Sub

    Private Sub SelectOutputFolder()

        Dim objFolderBrowserDialog As New FolderBrowser()

        With objFolderBrowserDialog
            ' No need to set the Browse Flags; default values are already set

            If txtOutputFolderPath.TextLength > 0 Then
                .FolderPath = txtOutputFolderPath.Text
            End If

            If .BrowseForFolder() Then
                txtOutputFolderPath.Text = .FolderPath
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

    Public Sub SetHeightAdjustForce(intHeightToForce As Integer)
        ' This function can be used to prevent the form from resizing itself if the MyBase.Resize event
        '  fires within 2 seconds of the current time
        ' See CatchUnrequestedHeightChange for more info
        mHeightAdjustForce = intHeightToForce
        mHeightAdjustTime = DateTime.UtcNow
    End Sub

    Private Sub SetToolTips()
        Dim objToolTipControl As New ToolTip

        With objToolTipControl
            .SetToolTip(txtDatasetNumber, "The dataset number is included as the first column in the output file.")

            .SetToolTip(txtIntensityThresholdAbsoluteMinimum, "Threshold for extending SIC")
            .SetToolTip(txtMaxDistanceScansNoOverlap, "Maximum distance that the edge of an identified peak can be away from the scan number that the parent ion was observed in if the identified peak does not contain the parent ion.")
            .SetToolTip(txtMaxAllowedUpwardSpikeFractionMax, "Maximum fraction of the peak maximum that an upward spike can be to be included in the peak")
            .SetToolTip(txtInitialPeakWidthScansScaler, "Multiplied by the S/N for the given spectrum to determine the initial minimum peak width (in scans) to try")
            .SetToolTip(txtInitialPeakWidthScansMaximum, "Maximum initial peak width to allow")

            .SetToolTip(txtSICTolerance, "Search tolerance for creating SIC; suggest 0.6 Da for ion traps and 20 ppm for TOF, FT or Orbitrap instruments")
            .SetToolTip(txtButterworthSamplingFrequency, "Value between 0.01 and 0.99; suggested value is 0.25")
            .SetToolTip(txtSavitzkyGolayFilterOrder, "Even number, 0 or greater; 0 means a moving average filter, 2 means a 2nd order Savitzky Golay filter")

            .SetToolTip(chkRefineReportedParentIonMZ, "If enabled, then will look through the m/z values in the parent ion spectrum data to find the closest match (within SICTolerance / " & clsSICOptions.DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_DA.ToString() & "); will update the reported m/z value to the one found")

            .SetToolTip(txtStatusLogKeyNameFilterList, "Enter a comma and/or NewLine separated list of Status Log Key names to match (will match any part of the key name to the text you enter).  Leave blank to include all Status Log entries.")
        End With

    End Sub

    Private Sub ShowAboutBox()
        Dim strMessage As String

        strMessage = String.Empty

        strMessage &= "Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2003" & ControlChars.NewLine
        strMessage &= "Copyright 2005, Battelle Memorial Institute.  All Rights Reserved." & ControlChars.NewLine & ControlChars.NewLine

        strMessage &= "This is version " & Application.ProductVersion & " (" & PROGRAM_DATE & "). "
        strMessage &= "Using MASIC PeakFinder DLL version " & mMasic.MASICPeakFinderDllVersion & ControlChars.NewLine & ControlChars.NewLine

        strMessage &= "E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov" & ControlChars.NewLine
        strMessage &= "Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/" & ControlChars.NewLine & ControlChars.NewLine

        strMessage &= "Licensed under the 2-Clause BSD License; you may not use this file except in compliance with the License.  "
        strMessage &= "You may obtain a copy of the License at https://opensource.org/licenses/BSD-2-Clause" & ControlChars.NewLine & ControlChars.NewLine

        MessageBox.Show(strMessage, "About", MessageBoxButtons.OK, MessageBoxIcon.Information)

    End Sub

    Private Sub UpdateCustomSICDataGridTableStyle()
        Dim tsCustomSICValues As DataGridTableStyle
        Dim blnTimeTolerance As Boolean

        ' Define the PM Thresholds table style
        tsCustomSICValues = New DataGridTableStyle

        ' Setting the MappingName of the table style to CUSTOM_SIC_VALUES_DATATABLE will cause this style to be used with that table
        With tsCustomSICValues
            .MappingName = CUSTOM_SIC_VALUES_DATATABLE
            .AllowSorting = True
            .ColumnHeadersVisible = True
            .RowHeadersVisible = True
            .ReadOnly = False
        End With

        ADONetRoutines.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_MZ, "Custom m/z", 90)
        ADONetRoutines.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_MZ_TOLERANCE, "m/z tolerance (Da)", 110)

        blnTimeTolerance = False
        Select Case GetCustomSICScanToleranceType()
            Case clsCustomSICList.eCustomSICScanTypeConstants.Relative
                ADONetRoutines.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_CENTER, "Relative Scan Number (0 to 1)", 170)
                ADONetRoutines.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_TOLERANCE, "Scan Tolerance", 90)

            Case clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime
                ADONetRoutines.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_CENTER, "Acq time (minutes)", 110)
                ADONetRoutines.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_TOLERANCE, "Time Tolerance", 90)
                blnTimeTolerance = True

            Case Else
                ' Includes eCustomSICScanTypeConstants.Absolute
                ADONetRoutines.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_CENTER, "Scan Number", 90)
                ADONetRoutines.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_TOLERANCE, "Scan Tolerance", 90)
        End Select

        ADONetRoutines.AppendColumnToTableStyle(tsCustomSICValues, COL_NAME_SCAN_COMMENT, "Comment", 90)

        fraCustomSICControls.Left = dgCustomSICValues.Left + dgCustomSICValues.Width + 15

        With dgCustomSICValues
            .TableStyles.Clear()

            If Not .TableStyles.Contains(tsCustomSICValues) Then
                .TableStyles.Add(tsCustomSICValues)
            End If

            .Refresh()
        End With

        If blnTimeTolerance Then
            lblCustomSICScanTolerance.Text = "Time Tolerance"
        Else
            lblCustomSICScanTolerance.Text = "Scan Tolerance"
        End If

    End Sub

    Private Function UpdateMasicSettings(ByRef objMasic As clsMASIC) As Boolean

        Dim blnError As Boolean

        Try
            Dim masicOptions = objMasic.Options

            With masicOptions
                ' Import options

                .ParentIonDecoyMassDa = ParseTextboxValueDbl(txtParentIonDecoyMassDa, lblParentIonDecoyMassDa.Text & " must be a value", blnError)
                If blnError Then Exit Try

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

                .MinimumSignalToNoiseRatio = ParseTextboxValueSng(txtExportRawDataSignalToNoiseRatioMinimum,
                                                                  lblExportRawDataSignalToNoiseRatioMinimum.Text & " must be a value", blnError)
                If blnError Then Exit Try
                .MaxIonCountPerScan = ParseTextboxValueInt(txtExportRawDataMaxIonCountPerScan,
                                                           lblExportRawDataMaxIonCountPerScan.Text & " must be an integer value", blnError)
                If blnError Then Exit Try
                .IntensityMinimum = ParseTextboxValueSng(txtExportRawDataIntensityMinimum,
                                                         lblExportRawDataIntensityMinimum.Text & " must be a value", blnError)
                If blnError Then Exit Try
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
                .SICOptions.DatasetNumber = ParseTextboxValueInt(txtDatasetNumber, lblDatasetNumber.Text & " must be an integer value", blnError)
                If blnError Then Exit Try

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
            Dim dblSICTolerance = ParseTextboxValueDbl(txtSICTolerance, lblSICToleranceDa.Text & " must be a value", blnError)
            If blnError Then Exit Try

            With masicOptions.SICOptions
                .SetSICTolerance(dblSICTolerance, optSICTolerancePPM.Checked)

                .ScanRangeStart = ParseTextboxValueInt(txtScanStart, lblScanStart.Text & " must be a value", blnError)
                If blnError Then Exit Try
                .ScanRangeEnd = ParseTextboxValueInt(txtScanEnd, lblScanEnd.Text & " must be a value", blnError)
                If blnError Then Exit Try

                .RTRangeStart = ParseTextboxValueSng(txtTimeStart, lblTimeStart.Text & " must be a value", blnError)
                If blnError Then Exit Try
                .RTRangeEnd = ParseTextboxValueSng(txtTimeEnd, lblTimeEnd.Text & " must be a value", blnError)
                If blnError Then Exit Try
            End With

            ' Note: the following 5 options are not graphically editable
            masicOptions.SuppressNoParentIonsError = mSuppressNoParentIonsError

            With masicOptions.SICOptions
                .CompressMSSpectraData = mCompressMSSpectraData
                .CompressMSMSSpectraData = mCompressMSMSSpectraData
                .CompressToleranceDivisorForDa = mCompressToleranceDivisorForDa
                .CompressToleranceDivisorForPPM = mCompressToleranceDivisorForPPM

                .MaxSICPeakWidthMinutesBackward = ParseTextboxValueSng(txtMaxPeakWidthMinutesBackward, lblMaxPeakWidthMinutes.Text & " " & lblMaxPeakWidthMinutesBackward.Text & " must be a value", blnError)
                If blnError Then Exit Try
                .MaxSICPeakWidthMinutesForward = ParseTextboxValueSng(txtMaxPeakWidthMinutesForward, lblMaxPeakWidthMinutes.Text & " " & lblMaxPeakWidthMinutesForward.Text & " must be a value", blnError)
                If blnError Then Exit Try
            End With

            With masicOptions.SICOptions
                .SICPeakFinderOptions.IntensityThresholdFractionMax = ParseTextboxValueSng(txtIntensityThresholdFractionMax, lblIntensityThresholdFractionMax.Text & " must be a value", blnError)
                If blnError Then Exit Try

                .SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum = ParseTextboxValueSng(txtIntensityThresholdAbsoluteMinimum, lblIntensityThresholdAbsoluteMinimum.Text & " must be a value", blnError)
                If blnError Then Exit Try

                .ReplaceSICZeroesWithMinimumPositiveValueFromMSData = chkReplaceSICZeroesWithMinimumPositiveValueFromMSData.Checked
                .RefineReportedParentIonMZ = chkRefineReportedParentIonMZ.Checked
            End With

            With masicOptions.SICOptions.SICPeakFinderOptions
                ' Peak Finding Options
                .SICBaselineNoiseOptions.BaselineNoiseMode = CType(cboSICNoiseThresholdMode.SelectedIndex, eNoiseThresholdModes)
                .SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute = ParseTextboxValueSng(txtSICNoiseThresholdIntensity, lblSICNoiseThresholdIntensity.Text & " must be a value", blnError)
                If blnError Then Exit Try

                .SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage = ParseTextboxValueSng(txtSICNoiseFractionLowIntensityDataToAverage, lblSICNoiseFractionLowIntensityDataToAverage.Text & " must be a value", blnError)
                If blnError Then Exit Try

                ' This value isn't utilized by MASIC for SICs so we'll force it to always be zero
                .SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = 0

                .MaxDistanceScansNoOverlap = ParseTextboxValueInt(txtMaxDistanceScansNoOverlap, lblMaxDistanceScansNoOverlap.Text & " must be an integer value", blnError)
                If blnError Then Exit Try
                .MaxAllowedUpwardSpikeFractionMax = ParseTextboxValueSng(txtMaxAllowedUpwardSpikeFractionMax, lblMaxAllowedUpwardSpikeFractionMax.Text & " must be a value", blnError)
                If blnError Then Exit Try
                .InitialPeakWidthScansScaler = ParseTextboxValueSng(txtInitialPeakWidthScansScaler, lblInitialPeakWidthScansScaler.Text & " must be a value", blnError)
                If blnError Then Exit Try
                .InitialPeakWidthScansMaximum = ParseTextboxValueInt(txtInitialPeakWidthScansMaximum, lblInitialPeakWidthScansMaximum.Text & " must be an integer value", blnError)
                If blnError Then Exit Try

                .UseButterworthSmooth = optUseButterworthSmooth.Checked
                .ButterworthSamplingFrequency = ParseTextboxValueSng(txtButterworthSamplingFrequency, lblButterworthSamplingFrequency.Text & " must be a value", blnError)
                If blnError Then Exit Try

                .UseSavitzkyGolaySmooth = optUseSavitzkyGolaySmooth.Checked
                .SavitzkyGolayFilterOrder = CShort(ParseTextboxValueInt(txtSavitzkyGolayFilterOrder, lblSavitzkyGolayFilterOrder.Text & " must be an integer value", blnError))
                If blnError Then Exit Try

                .FindPeaksOnSmoothedData = chkFindPeaksOnSmoothedData.Checked
                .SmoothDataRegardlessOfMinimumPeakWidth = chkSmoothDataRegardlessOfMinimumPeakWidth.Checked

                ' Mass Spectra Noise Threshold Options
                .MassSpectraNoiseThresholdOptions.BaselineNoiseMode = CType(cboMassSpectraNoiseThresholdMode.SelectedIndex, eNoiseThresholdModes)
                .MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute = ParseTextboxValueSng(txtMassSpectraNoiseThresholdIntensity, lblMassSpectraNoiseThresholdIntensity.Text & " must be a value", blnError)
                If blnError Then Exit Try

                .MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = ParseTextboxValueSng(txtMassSpectraNoiseFractionLowIntensityDataToAverage, lblMassSpectraNoiseFractionLowIntensityDataToAverage.Text & " must be a value", blnError)
                If blnError Then Exit Try

                .MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = ParseTextboxValueSng(txtMassSpectraNoiseMinimumSignalToNoiseRatio, lblMassSpectraNoiseMinimumSignalToNoiseRatio.Text & " must be a value", blnError)
                If blnError Then Exit Try

            End With

            With masicOptions.SICOptions
                ' Similarity Options
                .SimilarIonMZToleranceHalfWidth = ParseTextboxValueSng(txtSimilarIonMZToleranceHalfWidth, lblSimilarIonMZToleranceHalfWidth.Text & " must be a value", blnError)
                If blnError Then Exit Try
                .SimilarIonToleranceHalfWidthMinutes = ParseTextboxValueInt(txtSimilarIonToleranceHalfWidthMinutes, lblSimilarIonTimeToleranceHalfWidth.Text & " must be a value", blnError)
                If blnError Then Exit Try
                .SpectrumSimilarityMinimum = ParseTextboxValueSng(txtSpectrumSimilarityMinimum, lblSpectrumSimilarityMinimum.Text & " must be a value", blnError)
                If blnError Then Exit Try
            End With

            With masicOptions.BinningOptions

                ' Binning Options
                .StartX = ParseTextboxValueSng(txtBinStartX, lblBinStartX.Text & " must be a value", blnError)
                If blnError Then Exit Try
                .EndX = ParseTextboxValueSng(txtBinEndX, lblBinEndX.Text & " must be a value", blnError)
                If blnError Then Exit Try
                .BinSize = ParseTextboxValueSng(txtBinSize, lblBinSize.Text & " must be a value", blnError)
                If blnError Then Exit Try
                .MaximumBinCount = ParseTextboxValueInt(txtMaximumBinCount, lblMaximumBinCount.Text & " must be an integer value", blnError)
                If blnError Then Exit Try

                .IntensityPrecisionPercent = ParseTextboxValueSng(txtBinnedDataIntensityPrecisionPct, lblBinnedDataIntensityPrecisionPct.Text & " must be a value", blnError)
                If blnError Then Exit Try

                .Normalize = chkBinnedDataNormalize.Checked
                .SumAllIntensitiesForBin = chkBinnedDataSumAllIntensitiesForBin.Checked
            End With

            With masicOptions.CacheOptions
                ' Spectrum caching options
                .DiskCachingAlwaysDisabled = mCacheOptions.DiskCachingAlwaysDisabled
                .FolderPath = mCacheOptions.FolderPath
                .SpectraToRetainInMemory = mCacheOptions.SpectraToRetainInMemory
            End With

            With masicOptions.ReporterIons
                ' Reporter ion options
                .ReporterIonStatsEnabled = chkReporterIonStatsEnabled.Checked

                ' Note that this will set .ReporterIonToleranceDa to 0.5
                .ReporterIonMassMode = CType(cboReporterIonMassMode.SelectedIndex, clsReporterIons.eReporterIonMassModeConstants)

                ' Update .ReporterIonToleranceDa based on txtReporterIonMZToleranceDa
                .ReporterIonToleranceDaDefault = ParseTextboxValueDbl(txtReporterIonMZToleranceDa, "", blnError,
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

            Dim strCustomSICFileName = txtCustomSICFileName.Text.Trim()
            masicOptions.CustomSICList.CustomSICListFileName = strCustomSICFileName

            Dim mzSearchSpecs = New List(Of clsCustomMZSearchSpec)

            ' Only use the data in table CUSTOM_SIC_VALUES_DATATABLE if the CustomSicFileName is empty
            If String.IsNullOrWhiteSpace(strCustomSICFileName) Then

                For Each myDataRow As DataRow In mCustomSICValuesDataset.Tables(CUSTOM_SIC_VALUES_DATATABLE).Rows
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

            Dim sngScanOrAcqTimeTolerance = ParseTextboxValueSng(txtCustomSICScanOrAcqTimeTolerance, lblCustomSICScanTolerance.Text & " must be a value", blnError)
            If blnError Then Exit Try

            masicOptions.CustomSICList.SetCustomSICListValues(eScanType, sngScanOrAcqTimeTolerance, mzSearchSpecs)

        Catch ex As Exception
            MessageBox.Show("Error applying setting to clsMASIC: " & ControlChars.NewLine & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try

        Return Not blnError

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

    Private Sub cmdSelectOutputFolder_Click(sender As Object, e As EventArgs) Handles cmdSelectOutputFolder.Click
        SelectOutputFolder()
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
        ValidateTextboxSng(txtButterworthSamplingFrequency, 0.01, 0.99, 0.25)
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

    Private Sub txtDatasetNumber_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtDatasetNumber.KeyPress
        TextBoxKeyPressHandler(txtDatasetNumber, e, True, False)
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

    Private Sub mnuFileSelectOutputFolder_Click(sender As Object, e As EventArgs) Handles mnuFileSelectOutputFolder.Click
        SelectOutputFolder()
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
