Imports System.Runtime.InteropServices
Imports MASIC.clsMASIC
Imports MASIC.DataOutput
Imports MASICPeakFinder
Imports PRISM
Imports ThermoRawFileReader

Public Class clsMRMProcessing
    Inherits clsMasicEventNotifier

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
      <Out> ByRef mrmSettings As List(Of clsMRMScanInfo),
      <Out> ByRef srmList As List(Of udtSRMListType)) As Boolean

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

                                    Dim newSRMItem = New udtSRMListType() With {
                                        .ParentIonMZ = mrmInfoForHash.ParentIonMZ,
                                        .CentralMass = mrmInfoForHash.MRMMassList(intMRMMassIndex).CentralMass
                                    }

                                    srmList.Add(newSRMItem)
                                End If
                            Next
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
            ReportError("Error determining the MRM settings", ex, eMasicErrorCodes.OutputFileWriteError)
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
      outputDirectoryPath As String) As Boolean

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
                Next

                If mOptions.WriteMRMDataList Or mOptions.WriteMRMIntensityCrosstab Then

                    ' Populate srmKeyToIndexMap
                    Dim srmKeyToIndexMap = New Dictionary(Of String, Integer)
                    For intSRMIndex = 0 To srmList.Count - 1
                        srmKeyToIndexMap.Add(ConstructSRMMapKey(srmList(intSRMIndex)), intSRMIndex)
                    Next

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
                        Next

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

                            Next

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
            ReportError("Error writing the SRM data to disk", ex, eMasicErrorCodes.OutputFileWriteError)
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
                     StringUtilities.DblToString(sngScanTimeFirst, 5)

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
        Next

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

            Next
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
      peakFinder As clsMASICPeakFinder,
      ByRef intParentIonsProcessed As Integer) As Boolean

        Dim blnSuccess As Boolean

        Try
            blnSuccess = True

            ' Initialize sicDetails
            Dim sicDetails = New clsSICDetails()
            sicDetails.Reset()
            sicDetails.SICScanType = clsScanList.eScanTypeConstants.FragScan

            Dim noiseStatsSegments = New List(Of clsBaselineNoiseStatsSegment)

            For intParentIonIndex = 0 To scanList.ParentIonInfoCount - 1

                If scanList.ParentIons(intParentIonIndex).MRMDaughterMZ <= 0 Then
                    Continue For
                End If

                ' Step 1: Create the SIC for this MRM Parent/Daughter pair

                Dim dblParentIonMZ = scanList.ParentIons(intParentIonIndex).MZ
                Dim dblMRMDaughterMZ = scanList.ParentIons(intParentIonIndex).MRMDaughterMZ
                Dim dblSearchToleranceHalfWidth = scanList.ParentIons(intParentIonIndex).MRMToleranceHalfWidth

                ' Reset SICData
                sicDetails.SICData.Clear()

                ' Step through the fragmentation spectra, finding those that have matching parent and daughter ion m/z values
                For intScanIndex = 0 To scanList.FragScans.Count - 1
                    If scanList.FragScans(intScanIndex).MRMScanType <> MRMScanTypeConstants.SRM Then
                        Continue For
                    End If

                    With scanList.FragScans(intScanIndex)

                        Dim blnUseScan = False
                        For intMRMMassIndex = 0 To .MRMScanInfo.MRMMassCount - 1
                            If _
                                MRMParentDaughterMatch(.MRMScanInfo.ParentIonMZ,
                                                       .MRMScanInfo.MRMMassList(intMRMMassIndex).CentralMass,
                                                       dblParentIonMZ, dblMRMDaughterMZ) Then
                                blnUseScan = True
                                Exit For
                            End If
                        Next

                        If Not blnUseScan Then Continue For

                        ' Include this scan in the SIC for this parent ion

                        Dim sngMatchIntensity As Single
                        Dim dblClosestMZ As Double

                        mDataAggregation.FindMaxValueInMZRange(objSpectraCache,
                                                              scanList.FragScans(intScanIndex),
                                                              dblMRMDaughterMZ - dblSearchToleranceHalfWidth,
                                                              dblMRMDaughterMZ + dblSearchToleranceHalfWidth,
                                                              dblClosestMZ, sngMatchIntensity)

                        sicDetails.AddData(.ScanNumber, sngMatchIntensity, dblClosestMZ, intScanIndex)

                    End With

                Next


                ' Step 2: Find the largest peak in the SIC

                ' Compute the noise level; the noise level may change with increasing index number if the background is increasing for a given m/z
                blnSuccess = peakFinder.ComputeDualTrimmedNoiseLevelTTest(sicDetails.SICIntensities, 0,
                                                                          sicDetails.SICDataCount - 1,
                                                                          mOptions.SICOptions.SICPeakFinderOptions.
                                                                             SICBaselineNoiseOptions,
                                                                          noiseStatsSegments)

                If Not blnSuccess Then
                    SetLocalErrorCode(eMasicErrorCodes.FindSICPeaksError, True)
                    Exit Try
                End If

                ' Initialize the peak
                scanList.ParentIons(intParentIonIndex).SICStats.Peak = New clsSICStatsPeak()

                ' Find the data point with the maximum intensity
                Dim sngMaximumIntensity As Single = 0
                scanList.ParentIons(intParentIonIndex).SICStats.Peak.IndexObserved = 0
                For intScanIndex = 0 To sicDetails.SICDataCount - 1
                    Dim intensity = sicDetails.SICIntensities(intScanIndex)
                    If intensity > sngMaximumIntensity Then
                        sngMaximumIntensity = intensity
                        scanList.ParentIons(intParentIonIndex).SICStats.Peak.IndexObserved = intScanIndex
                    End If
                Next

                Dim potentialAreaStatsInFullSIC As clsSICPotentialAreaStats = Nothing

                ' Compute the minimum potential peak area in the entire SIC, populating udtSICPotentialAreaStatsInFullSIC
                peakFinder.FindPotentialPeakArea(sicDetails.SICData,
                                                 potentialAreaStatsInFullSIC,
                                                 mOptions.SICOptions.SICPeakFinderOptions)

                ' Update .BaselineNoiseStats in scanList.ParentIons(intParentIonIndex).SICStats.Peak
                scanList.ParentIons(intParentIonIndex).SICStats.Peak.BaselineNoiseStats =
                    peakFinder.LookupNoiseStatsUsingSegments(
                        scanList.ParentIons(intParentIonIndex).SICStats.Peak.IndexObserved,
                        noiseStatsSegments)

                Dim smoothedYDataSubset As clsSmoothedYDataSubset = Nothing

                With scanList.ParentIons(intParentIonIndex)

                    ' Clear udtSICPotentialAreaStatsForPeak
                    .SICStats.SICPotentialAreaStatsForPeak = New clsSICPotentialAreaStats()

                    blnSuccess = peakFinder.FindSICPeakAndArea(sicDetails.SICData,
                                                               .SICStats.SICPotentialAreaStatsForPeak,
                                                               .SICStats.Peak, smoothedYDataSubset,
                                                               mOptions.SICOptions.SICPeakFinderOptions,
                                                               potentialAreaStatsInFullSIC, False,
                                                               scanList.SIMDataPresent, False)


                    blnSuccess = sicProcessor.StorePeakInParentIon(scanList, intParentIonIndex, sicDetails,
                                                                   .SICStats.SICPotentialAreaStatsForPeak,
                                                                   .SICStats.Peak, blnSuccess)
                End With


                ' Step 3: store the results

                ' Possibly save the stats for this SIC to the SICData file
                mDataOutputHandler.SaveSICDataToText(mOptions.SICOptions, scanList, intParentIonIndex, sicDetails)

                ' Save the stats for this SIC to the XML file
                xmlResultsWriter.SaveDataToXML(scanList, intParentIonIndex, sicDetails, smoothedYDataSubset,
                                               mDataOutputHandler)

                intParentIonsProcessed += 1

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
                            ReportMessage("Parent Ions Processed: " & intParentIonsProcessed.ToString())
                            Console.Write(".")
                            mOptions.LastParentIonProcessingLogTime = DateTime.UtcNow
                        End If
                    End If

                Catch ex As Exception
                    ReportError("Error updating progress", ex, eMasicErrorCodes.CreateSICsError)
                End Try

            Next

        Catch ex As Exception
            ReportError("Error creating SICs for MRM spectra", ex, eMasicErrorCodes.CreateSICsError)
            blnSuccess = False
        End Try

        Return blnSuccess
    End Function

End Class
