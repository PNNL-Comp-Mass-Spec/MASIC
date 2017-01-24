Imports System.Runtime.InteropServices
Imports MASIC.clsMASIC
Imports MASIC.DataOutput
Imports ThermoRawFileReader

Public Class clsMRMProcessing
    Inherits clsEventNotifier

#Region "Structures"

    Public Structure udtSRMListType
        Public ParentIonMZ As Double
        Public CentralMass As Double

        Public Overrides Function ToString() As String
            Return "m/z " & ParentIonMZ.ToString("0.00")
        End Function
    End Structure

#End Region

#Region "Classwide variables"
    Private ReadOnly mOptions As clsMASICOptions
    Private ReadOnly mDataAggregation As clsDataAggregation
    Private ReadOnly mDataOutputHandler As clsDataOutput
#End Region

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New(masicOptions As clsMASICOptions, dataOutputHandler As clsDataOutput)
        mOptions = masicOptions
        mDataAggregation = New clsDataAggregation()
        RegisterEvents(mDataAggregation)

        mDataOutputHandler = dataOutputHandler
    End Sub

    Private Function ConstructSRMMapKey(udtSRMListEntry As udtSRMListType) As String
        Return ConstructSRMMapKey(udtSRMListEntry.ParentIonMZ, udtSRMListEntry.CentralMass)
    End Function

    Private Function ConstructSRMMapKey(dblParentIonMZ As Double, dblCentralMass As Double) As String
        Dim strMapKey As String

        strMapKey = dblParentIonMZ.ToString("0.000") & "_to_" & dblCentralMass.ToString("0.000")

        Return strMapKey
    End Function

    Private Function DetermineMRMSettings(
      scanList As clsScanList,
      <Out()> ByRef mrmSettings As List(Of clsMRMScanInfo),
      <Out()> ByRef srmList As List(Of udtSRMListType)) As Boolean

        ' Returns true if this dataset has MRM data and if it is parsed successfully
        ' Returns false if the dataset does not have MRM data, or if an error occurs

        Dim strMRMInfoHash As String

        Dim intMRMMassIndex As Integer

        Dim blnMRMDataPresent As Boolean
        Dim blnMatchFound As Boolean
        Dim blnSuccess As Boolean

        mrmSettings = New List(Of clsMRMScanInfo)
        srmList = New List(Of udtSRMListType)

        Try
            blnMRMDataPresent = False

            UpdateProgress(0, "Determining MRM settings")

            ' Initialize the tracking arrays
            Dim mrmHashToIndexMap = New Dictionary(Of String, clsMRMScanInfo)

            ' Construct a list of the MRM search values used
            For Each fragScan In scanList.FragScans
                If fragScan.MRMScanType = MRMScanTypeConstants.SRM Then
                    blnMRMDataPresent = True

                    With fragScan
                        ' See if this MRM spec is already in mrmSettings

                        strMRMInfoHash = GenerateMRMInfoHash(.MRMScanInfo)

                        Dim mrmInfoForHash As clsMRMScanInfo = Nothing
                        If Not mrmHashToIndexMap.TryGetValue(strMRMInfoHash, mrmInfoForHash) Then

                            mrmInfoForHash = DuplicateMRMInfo(.MRMScanInfo)

                            mrmInfoForHash.ScanCount = 1
                            mrmInfoForHash.ParentIonInfoIndex = .FragScanInfo.ParentIonInfoIndex

                            mrmSettings.Add(mrmInfoForHash)
                            mrmHashToIndexMap.Add(strMRMInfoHash, mrmInfoForHash)


                            ' Append the new entries to srmList

                            For intMRMMassIndex = 0 To mrmInfoForHash.MRMMassCount - 1

                                ' Add this new transition to srmList() only if not already present
                                blnMatchFound = False
                                For Each srmItem In srmList
                                    If MRMParentDaughterMatch(srmItem, mrmInfoForHash, intMRMMassIndex) Then
                                        blnMatchFound = True
                                        Exit For
                                    End If
                                Next

                                If Not blnMatchFound Then
                                    ' Entry is not yet present; add it

                                    Dim newSRMItem = New udtSRMListType
                                    newSRMItem.ParentIonMZ = mrmInfoForHash.ParentIonMZ
                                    newSRMItem.CentralMass = mrmInfoForHash.MRMMassList(intMRMMassIndex).CentralMass

                                    srmList.Add(newSRMItem)
                                End If
                            Next intMRMMassIndex
                        Else
                            mrmInfoForHash.ScanCount += 1
                        End If

                    End With
                End If
            Next

            If blnMRMDataPresent Then
                blnSuccess = True
            Else
                blnSuccess = False
            End If

        Catch ex As Exception
            ReportError("DetermineMRMSettings", "Error determining the MRM settings", ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Public Shared Function DuplicateMRMInfo(
      oSource As MRMInfo,
      dblParentIonMZ As Double) As clsMRMScanInfo

        Dim oTarget = New clsMRMScanInfo()

        With oSource
            oTarget.ParentIonMZ = dblParentIonMZ
            oTarget.MRMMassCount = .MRMMassList.Count

            If .MRMMassList Is Nothing Then
                oTarget.MRMMassList = New List(Of udtMRMMassRangeType)
            Else
                oTarget.MRMMassList = New List(Of udtMRMMassRangeType)(.MRMMassList.Count)
                oTarget.MRMMassList.AddRange(.MRMMassList)
            End If

            oTarget.ScanCount = 0
            oTarget.ParentIonInfoIndex = -1
        End With

        Return oTarget
    End Function

    Private Function DuplicateMRMInfo(oSource As clsMRMScanInfo) As clsMRMScanInfo
        Dim oTarget = New clsMRMScanInfo()

        With oSource
            oTarget.ParentIonMZ = .ParentIonMZ
            oTarget.MRMMassCount = .MRMMassCount

            If .MRMMassList Is Nothing Then
                oTarget.MRMMassList = New List(Of udtMRMMassRangeType)
            Else
                oTarget.MRMMassList = New List(Of udtMRMMassRangeType)(.MRMMassList.Count)
                oTarget.MRMMassList.AddRange(.MRMMassList)
            End If
        End With

        oTarget.ScanCount = oSource.ScanCount
        oTarget.ParentIonInfoIndex = oSource.ParentIonInfoIndex

        Return oTarget

    End Function

    Public Function ExportMRMDataToDisk(
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      strInputFileName As String,
      strOutputFolderPath As String) As Boolean

        Dim mrmSettings As List(Of clsMRMScanInfo) = Nothing
        Dim srmList As List(Of udtSRMListType) = Nothing

        Dim blnSuccess = DetermineMRMSettings(scanList, mrmSettings, srmList)

        If blnSuccess Then
            blnSuccess = ExportMRMDataToDisk(scanList, objSpectraCache, mrmSettings, srmList, strInputFileName, strOutputFolderPath)
        End If

        Return blnSuccess

    End Function

    Private Function ExportMRMDataToDisk(
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      mrmSettings As List(Of clsMRMScanInfo),
      srmList As List(Of udtSRMListType),
      strInputFileName As String,
      strOutputFolderPath As String) As Boolean

        ' Returns true if the MRM data is successfully written to disk
        ' Note that it will also return true if udtMRMSettings() is empty

        Const cColDelimiter As Char = ControlChars.Tab

        Dim srDataOutfile As StreamWriter = Nothing
        Dim srCrosstabOutfile As StreamWriter = Nothing

        Dim blnSuccess As Boolean

        Try
            ' Only write this data if 1 or more fragmentation spectra are of type SRM
            If mrmSettings Is Nothing OrElse mrmSettings.Count = 0 Then
                blnSuccess = True
                Exit Try
            End If

            UpdateProgress(0, "Exporting MRM data")

            ' Write out the MRM Settings
            Dim strMRMSettingsFilePath = clsDataOutput.ConstructOutputFilePath(
                strInputFileName, strOutputFolderPath, clsDataOutput.eOutputFileTypeConstants.MRMSettingsFile)

            Using srSettingsOutFile = New StreamWriter(strMRMSettingsFilePath)

                srSettingsOutFile.WriteLine(mDataOutputHandler.GetHeadersForOutputFile(scanList, clsDataOutput.eOutputFileTypeConstants.MRMSettingsFile))


                For intMRMInfoIndex = 0 To mrmSettings.Count - 1
                    With mrmSettings(intMRMInfoIndex)
                        For intMRMMassIndex = 0 To .MRMMassCount - 1
                            Dim strOutLine = intMRMInfoIndex & cColDelimiter &
                             .ParentIonMZ.ToString("0.000") & cColDelimiter &
                             .MRMMassList(intMRMMassIndex).CentralMass & cColDelimiter &
                             .MRMMassList(intMRMMassIndex).StartMass & cColDelimiter &
                             .MRMMassList(intMRMMassIndex).EndMass & cColDelimiter &
                             .ScanCount.ToString

                            srSettingsOutFile.WriteLine(strOutLine)
                        Next

                    End With
                Next intMRMInfoIndex

                If mOptions.WriteMRMDataList Or mOptions.WriteMRMIntensityCrosstab Then

                    ' Populate srmKeyToIndexMap
                    Dim srmKeyToIndexMap = New Dictionary(Of String, Integer)
                    For intSRMIndex = 0 To srmList.Count - 1
                        srmKeyToIndexMap.Add(ConstructSRMMapKey(srmList(intSRMIndex)), intSRMIndex)
                    Next intSRMIndex

                    If mOptions.WriteMRMDataList Then
                        ' Write out the raw MRM Data
                        Dim strDataFilePath = clsDataOutput.ConstructOutputFilePath(strInputFileName, strOutputFolderPath, clsDataOutput.eOutputFileTypeConstants.MRMDatafile)
                        srDataOutfile = New StreamWriter(strDataFilePath)

                        ' Write the file headers
                        srDataOutfile.WriteLine(mDataOutputHandler.GetHeadersForOutputFile(scanList, clsDataOutput.eOutputFileTypeConstants.MRMDatafile))
                    End If


                    If mOptions.WriteMRMIntensityCrosstab Then
                        ' Write out the raw MRM Data
                        Dim strCrosstabFilePath = clsDataOutput.ConstructOutputFilePath(strInputFileName, strOutputFolderPath, clsDataOutput.eOutputFileTypeConstants.MRMCrosstabFile)
                        srCrosstabOutfile = New StreamWriter(strCrosstabFilePath)

                        ' Initialize the crosstab header variable using the data in udtSRMList()
                        Dim strCrosstabHeaders = "Scan_First" & cColDelimiter & "ScanTime"

                        For intSRMIndex = 0 To srmList.Count - 1
                            strCrosstabHeaders &= cColDelimiter & ConstructSRMMapKey(srmList(intSRMIndex))
                        Next intSRMIndex

                        srCrosstabOutfile.WriteLine(strCrosstabHeaders)
                    End If

                    Dim intScanFirst = Integer.MinValue
                    Dim sngScanTimeFirst As Single
                    Dim intSRMIndexLast = 0

                    Dim sngCrosstabColumnValue() As Single
                    Dim blnCrosstabColumnFlag() As Boolean

                    ReDim sngCrosstabColumnValue(srmList.Count - 1)
                    ReDim blnCrosstabColumnFlag(srmList.Count - 1)

                    'For intScanIndex = 0 To scanList.FragScanCount - 1
                    For Each fragScan In scanList.FragScans
                        If fragScan.MRMScanType <> MRMScanTypeConstants.SRM Then
                            Continue For
                        End If

                        With fragScan

                            If intScanFirst = Integer.MinValue Then
                                intScanFirst = .ScanNumber
                                sngScanTimeFirst = .ScanTime
                            End If

                            Dim strLineStart = .ScanNumber & cColDelimiter &
                                               .MRMScanInfo.ParentIonMZ.ToString("0.000") & cColDelimiter

                            ' Look for each of the m/z values specified in .MRMScanInfo.MRMMassList
                            For intMRMMassIndex = 0 To .MRMScanInfo.MRMMassCount - 1
                                ' Find the maximum value between .StartMass and .EndMass
                                ' Need to define a tolerance to account for numeric rounding artifacts in the variables

                                Dim dblMZStart = .MRMScanInfo.MRMMassList(intMRMMassIndex).StartMass
                                Dim dblMZEnd = .MRMScanInfo.MRMMassList(intMRMMassIndex).EndMass
                                Dim dblMRMToleranceHalfWidth = Math.Round((dblMZEnd - dblMZStart) / 2, 6)
                                If dblMRMToleranceHalfWidth < 0.001 Then
                                    dblMRMToleranceHalfWidth = 0.001
                                End If

                                Dim dblClosestMZ As Double
                                Dim sngMatchIntensity As Single

                                Dim blnMatchFound = mDataAggregation.FindMaxValueInMZRange(
                                    objSpectraCache, fragScan,
                                    dblMZStart - dblMRMToleranceHalfWidth,
                                    dblMZEnd + dblMRMToleranceHalfWidth,
                                    dblClosestMZ, sngMatchIntensity)

                                If mOptions.WriteMRMDataList Then
                                    Dim strOutLine As String
                                    If blnMatchFound Then
                                        strOutLine = strLineStart & .MRMScanInfo.MRMMassList(intMRMMassIndex).CentralMass.ToString("0.000") &
                                         cColDelimiter & sngMatchIntensity.ToString("0.000")

                                    Else
                                        strOutLine = strLineStart & .MRMScanInfo.MRMMassList(intMRMMassIndex).CentralMass.ToString("0.000") &
                                         cColDelimiter & "0"
                                    End If

                                    srDataOutfile.WriteLine(strOutLine)
                                End If


                                If mOptions.WriteMRMIntensityCrosstab Then
                                    Dim strSRMMapKey = ConstructSRMMapKey(.MRMScanInfo.ParentIonMZ, .MRMScanInfo.MRMMassList(intMRMMassIndex).CentralMass)
                                    Dim intSRMIndex As Integer

                                    ' Use srmKeyToIndexMap to determine the appropriate column index for strSRMMapKey
                                    If srmKeyToIndexMap.TryGetValue(strSRMMapKey, intSRMIndex) Then

                                        If blnCrosstabColumnFlag(intSRMIndex) OrElse
                                           (intSRMIndex = 0 And intSRMIndexLast = srmList.Count - 1) Then
                                            ' Either the column is already populated, or the SRMIndex has cycled back to zero; write out the current crosstab line and reset the crosstab column arrays
                                            ExportMRMDataWriteLine(srCrosstabOutfile, intScanFirst, sngScanTimeFirst, sngCrosstabColumnValue, blnCrosstabColumnFlag, cColDelimiter, True)

                                            intScanFirst = .ScanNumber
                                            sngScanTimeFirst = .ScanTime
                                        End If

                                        If blnMatchFound Then
                                            sngCrosstabColumnValue(intSRMIndex) = sngMatchIntensity
                                        End If
                                        blnCrosstabColumnFlag(intSRMIndex) = True
                                        intSRMIndexLast = intSRMIndex
                                    Else
                                        ' Unknown combination of parent ion m/z and daughter m/z; this is unexpected
                                        ' We won't write this entry out
                                        intSRMIndexLast = intSRMIndexLast
                                    End If
                                End If

                            Next intMRMMassIndex

                        End With

                        UpdateCacheStats(objSpectraCache)
                        If mOptions.AbortProcessing Then
                            Exit For
                        End If
                    Next

                    If mOptions.WriteMRMIntensityCrosstab Then
                        ' Write out any remaining crosstab values
                        ExportMRMDataWriteLine(srCrosstabOutfile, intScanFirst, sngScanTimeFirst, sngCrosstabColumnValue, blnCrosstabColumnFlag, cColDelimiter, False)
                    End If

                End If

            End Using

            blnSuccess = True

        Catch ex As Exception
            ReportError("ExportMRMDataToDisk", "Error writing the SRM data to disk", ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            blnSuccess = False
        Finally
            If Not srDataOutfile Is Nothing Then
                srDataOutfile.Close()
            End If

            If Not srCrosstabOutfile Is Nothing Then
                srCrosstabOutfile.Close()
            End If
        End Try

        Return blnSuccess

    End Function

    Private Sub ExportMRMDataWriteLine(
      srCrosstabOutfile As StreamWriter,
      intScanFirst As Integer,
      sngScanTimeFirst As Single,
      sngCrosstabColumnValue() As Single,
      blnCrosstabColumnFlag() As Boolean,
      cColDelimiter As Char,
      blnForceWrite As Boolean)

        ' If blnForceWrite = False, then will only write out the line if 1 or more columns is non-zero

        Dim intIndex As Integer
        Dim intNonZeroCount As Integer

        Dim strOutLine As String

        intNonZeroCount = 0
        strOutLine = intScanFirst.ToString() & cColDelimiter &
         Math.Round(sngScanTimeFirst, 5).ToString

        ' Construct a tab-delimited list of the values
        ' At the same time, clear the arrays
        For intIndex = 0 To sngCrosstabColumnValue.Length - 1
            If sngCrosstabColumnValue(intIndex) > 0 Then
                strOutLine &= cColDelimiter & sngCrosstabColumnValue(intIndex).ToString("0.000")
                intNonZeroCount += 1
            Else
                strOutLine &= cColDelimiter & "0"
            End If

            sngCrosstabColumnValue(intIndex) = 0
            blnCrosstabColumnFlag(intIndex) = False
        Next intIndex

        If intNonZeroCount > 0 OrElse blnForceWrite Then
            srCrosstabOutfile.WriteLine(strOutLine)
        End If

    End Sub

    Private Function GenerateMRMInfoHash(mrmScanInfo As clsMRMScanInfo) As String
        Dim strHash As String
        Dim intIndex As Integer

        With mrmScanInfo
            strHash = .ParentIonMZ & "_" & .MRMMassCount

            For intIndex = 0 To .MRMMassCount - 1
                strHash &= "_" &
                  .MRMMassList(intIndex).CentralMass.ToString("0.000") & "_" &
                  .MRMMassList(intIndex).StartMass.ToString("0.000") & "_" &
                  .MRMMassList(intIndex).EndMass.ToString("0.000")

            Next intIndex
        End With

        Return strHash

    End Function

    Private Function MRMParentDaughterMatch(
      ByRef udtSRMListEntry As udtSRMListType,
      mrmSettingsEntry As clsMRMScanInfo,
      intMRMMassIndex As Integer) As Boolean

        Return MRMParentDaughterMatch(
          udtSRMListEntry.ParentIonMZ,
          udtSRMListEntry.CentralMass,
          mrmSettingsEntry.ParentIonMZ,
          mrmSettingsEntry.MRMMassList(intMRMMassIndex).CentralMass)
    End Function

    Private Function MRMParentDaughterMatch(
      dblParentIonMZ1 As Double,
      dblMRMDaughterMZ1 As Double,
      dblParentIonMZ2 As Double,
      dblMRMDaughterMZ2 As Double) As Boolean

        Const COMPARISON_TOLERANCE = 0.01

        If Math.Abs(dblParentIonMZ1 - dblParentIonMZ2) <= COMPARISON_TOLERANCE AndAlso
           Math.Abs(dblMRMDaughterMZ1 - dblMRMDaughterMZ2) <= COMPARISON_TOLERANCE Then
            Return True
        Else
            Return False
        End If

    End Function

    Public Function ProcessMRMList(
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      sicProcessor As clsSICProcessing,
      xmlResultsWriter As clsXMLResultsWriter,
      peakFinder As MASICPeakFinder.clsMASICPeakFinder,
      ByRef intParentIonsProcessed As Integer) As Boolean


        Dim intParentIonIndex As Integer

        Dim intScanIndex As Integer
        Dim intMRMMassIndex As Integer


        Dim dblParentIonMZ As Double
        Dim dblMRMDaughterMZ As Double

        Dim dblSearchToleranceHalfWidth As Double
        Dim dblClosestMZ As Double

        Dim sngMatchIntensity As Single

        Dim sngMaximumIntensity As Single

        Dim udtSICDetails As clsDataObjects.udtSICStatsDetailsType

        Dim udtSICPotentialAreaStatsInFullSIC As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType

        Dim udtSmoothedYData As MASICPeakFinder.clsMASICPeakFinder.udtSmoothedYDataSubsetType
        Dim udtSmoothedYDataSubset As MASICPeakFinder.clsMASICPeakFinder.udtSmoothedYDataSubsetType

        Dim udtBaselineNoiseStatSegments() As MASICPeakFinder.clsMASICPeakFinder.udtBaselineNoiseStatSegmentsType

        Dim blnUseScan As Boolean
        ' ReSharper disable once NotAccessedVariable
        Dim blnMatchFound As Boolean
        Dim blnSuccess As Boolean

        Try
            blnSuccess = True

            ' Initialize udtSICDetails
            With udtSICDetails
                .SICDataCount = 0
                .SICScanType = clsScanList.eScanTypeConstants.FragScan

                ReDim .SICScanIndices(scanList.FragScans.Count)
                ReDim .SICScanNumbers(scanList.FragScans.Count)
                ReDim .SICData(scanList.FragScans.Count)
                ReDim .SICMasses(scanList.FragScans.Count)
            End With

            ' Reserve room in udtSmoothedYData and udtSmoothedYDataSubset
            With udtSmoothedYData
                .DataCount = 0
                ReDim .Data(scanList.FragScans.Count)
            End With

            With udtSmoothedYDataSubset
                .DataCount = 0
                ReDim .Data(scanList.FragScans.Count)
            End With

            ReDim udtBaselineNoiseStatSegments(0)

            For intParentIonIndex = 0 To scanList.ParentIonInfoCount - 1

                If scanList.ParentIons(intParentIonIndex).MRMDaughterMZ > 0 Then
                    ' Step 1: Create the SIC for this MRM Parent/Daughter pair

                    dblParentIonMZ = scanList.ParentIons(intParentIonIndex).MZ
                    dblMRMDaughterMZ = scanList.ParentIons(intParentIonIndex).MRMDaughterMZ
                    dblSearchToleranceHalfWidth = scanList.ParentIons(intParentIonIndex).MRMToleranceHalfWidth

                    ' Reset udtSICDetails 
                    udtSICDetails.SICDataCount = 0

                    ' Step through the fragmentation spectra, finding those that have matching parent and daughter ion m/z values
                    For intScanIndex = 0 To scanList.FragScans.Count - 1
                        If scanList.FragScans(intScanIndex).MRMScanType <> MRMScanTypeConstants.SRM Then
                            Continue For
                        End If

                        With scanList.FragScans(intScanIndex)

                            blnUseScan = False
                            For intMRMMassIndex = 0 To .MRMScanInfo.MRMMassCount - 1
                                If MRMParentDaughterMatch(
                                    .MRMScanInfo.ParentIonMZ,
                                    .MRMScanInfo.MRMMassList(intMRMMassIndex).CentralMass,
                                    dblParentIonMZ, dblMRMDaughterMZ) Then
                                    blnUseScan = True
                                    Exit For
                                End If
                            Next

                            If Not blnUseScan Then Continue For

                            ' Include this scan in the SIC for this parent ion

                            blnMatchFound = mDataAggregation.FindMaxValueInMZRange(objSpectraCache, scanList.FragScans(intScanIndex), dblMRMDaughterMZ - dblSearchToleranceHalfWidth, dblMRMDaughterMZ + dblSearchToleranceHalfWidth, dblClosestMZ, sngMatchIntensity)

                            If udtSICDetails.SICDataCount >= udtSICDetails.SICData.Length Then
                                ReDim Preserve udtSICDetails.SICScanIndices(udtSICDetails.SICScanIndices.Length * 2 - 1)
                                ReDim Preserve udtSICDetails.SICScanNumbers(udtSICDetails.SICScanIndices.Length - 1)
                                ReDim Preserve udtSICDetails.SICData(udtSICDetails.SICScanIndices.Length - 1)
                                ReDim Preserve udtSICDetails.SICMasses(udtSICDetails.SICScanIndices.Length - 1)
                            End If

                            udtSICDetails.SICScanIndices(udtSICDetails.SICDataCount) = intScanIndex
                            udtSICDetails.SICScanNumbers(udtSICDetails.SICDataCount) = .ScanNumber
                            udtSICDetails.SICData(udtSICDetails.SICDataCount) = sngMatchIntensity
                            udtSICDetails.SICMasses(udtSICDetails.SICDataCount) = dblClosestMZ

                            udtSICDetails.SICDataCount += 1


                        End With

                    Next intScanIndex


                    ' Step 2: Find the largest peak in the SIC

                    ' Compute the noise level; the noise level may change with increasing index number if the background is increasing for a given m/z
                    blnSuccess = peakFinder.ComputeDualTrimmedNoiseLevelTTest(
                        udtSICDetails.SICData, 0, udtSICDetails.SICDataCount - 1,
                        mOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions,
                        udtBaselineNoiseStatSegments)

                    If Not blnSuccess Then
                        SetLocalErrorCode(eMasicErrorCodes.FindSICPeaksError, True)
                        Exit Try
                    End If

                    ' Initialize the peak
                    scanList.ParentIons(intParentIonIndex).SICStats.Peak = New MASICPeakFinder.clsMASICPeakFinder.udtSICStatsPeakType

                    ' Find the data point with the maximum intensity
                    sngMaximumIntensity = 0
                    scanList.ParentIons(intParentIonIndex).SICStats.Peak.IndexObserved = 0
                    For intScanIndex = 0 To udtSICDetails.SICDataCount - 1
                        If udtSICDetails.SICData(intScanIndex) > sngMaximumIntensity Then
                            sngMaximumIntensity = udtSICDetails.SICData(intScanIndex)
                            scanList.ParentIons(intParentIonIndex).SICStats.Peak.IndexObserved = intScanIndex
                        End If
                    Next intScanIndex


                    ' Compute the minimum potential peak area in the entire SIC, populating udtSICPotentialAreaStatsInFullSIC
                    peakFinder.FindPotentialPeakArea(udtSICDetails.SICDataCount, udtSICDetails.SICData,
                                                     udtSICPotentialAreaStatsInFullSIC, mOptions.SICOptions.SICPeakFinderOptions)

                    ' Update .BaselineNoiseStats in scanList.ParentIons(intParentIonIndex).SICStats.Peak
                    scanList.ParentIons(intParentIonIndex).SICStats.Peak.BaselineNoiseStats = peakFinder.LookupNoiseStatsUsingSegments(
                        scanList.ParentIons(intParentIonIndex).SICStats.Peak.IndexObserved, udtBaselineNoiseStatSegments)

                    With scanList.ParentIons(intParentIonIndex)

                        ' Clear udtSICPotentialAreaStatsForPeak
                        .SICStats.SICPotentialAreaStatsForPeak = New MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType

                        blnSuccess = peakFinder.FindSICPeakAndArea(
                         udtSICDetails.SICDataCount, udtSICDetails.SICScanNumbers, udtSICDetails.SICData,
                         .SICStats.SICPotentialAreaStatsForPeak, .SICStats.Peak,
                         udtSmoothedYDataSubset, mOptions.SICOptions.SICPeakFinderOptions,
                         udtSICPotentialAreaStatsInFullSIC,
                         False, scanList.SIMDataPresent, False)


                        blnSuccess = sicProcessor.StorePeakInParentIon(scanList, intParentIonIndex,
                                                                       udtSICDetails,
                                                                       .SICStats.SICPotentialAreaStatsForPeak,
                                                                       .SICStats.Peak,
                                                                       blnSuccess)
                    End With


                    ' Step 3: store the results

                    ' Possibly save the stats for this SIC to the SICData file
                    mDataOutputHandler.SaveSICDataToText(mOptions.SICOptions, scanList, intParentIonIndex, udtSICDetails)

                    ' Save the stats for this SIC to the XML file
                    xmlResultsWriter.SaveDataToXML(scanList, intParentIonIndex, udtSICDetails, udtSmoothedYDataSubset, mDataOutputHandler)

                    intParentIonsProcessed += 1

                End If


                '---------------------------------------------------------
                ' Update progress
                '---------------------------------------------------------
                Try

                    If scanList.ParentIonInfoCount > 1 Then
                        UpdateProgress(CShort(intParentIonsProcessed / (scanList.ParentIonInfoCount - 1) * 100))
                    Else
                        UpdateProgress(0)
                    End If

                    UpdateCacheStats(objSpectraCache)
                    If mOptions.AbortProcessing Then
                        scanList.ProcessingIncomplete = True
                        Exit For
                    End If

                    If intParentIonsProcessed Mod 100 = 0 Then
                        If DateTime.UtcNow.Subtract(mOptions.LastParentIonProcessingLogTime).TotalSeconds >= 10 OrElse intParentIonsProcessed Mod 500 = 0 Then
                            ReportMessage("Parent Ions Processed: " & intParentIonsProcessed.ToString)
                            Console.Write(".")
                            mOptions.LastParentIonProcessingLogTime = DateTime.UtcNow
                        End If
                    End If

                Catch ex As Exception
                    ReportError("ProcessMRMList", "Error updating progress", ex, True, True, eMasicErrorCodes.CreateSICsError)
                End Try

            Next intParentIonIndex

        Catch ex As Exception
            ReportError("ProcessMRMList", "Error creating SICs for MRM spectra", ex, True, True, eMasicErrorCodes.CreateSICsError)
            blnSuccess = False
        End Try

        Return blnSuccess
    End Function

End Class
