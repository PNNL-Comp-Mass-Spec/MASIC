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

    Private Function ConstructSRMMapKey(parentIonMZ As Double, centralMass As Double) As String
        Dim mapKey As String

        mapKey = parentIonMZ.ToString("0.000") & "_to_" & centralMass.ToString("0.000")

        Return mapKey
    End Function

    Private Function DetermineMRMSettings(
      scanList As clsScanList,
      <Out> ByRef mrmSettings As List(Of clsMRMScanInfo),
      <Out> ByRef srmList As List(Of udtSRMListType)) As Boolean

        ' Returns true if this dataset has MRM data and if it is parsed successfully
        ' Returns false if the dataset does not have MRM data, or if an error occurs

        Dim mrmInfoHash As String

        Dim mrmMassIndex As Integer

        Dim mrmDataPresent As Boolean
        Dim matchFound As Boolean

        mrmSettings = New List(Of clsMRMScanInfo)
        srmList = New List(Of udtSRMListType)

        Try
            mrmDataPresent = False

            UpdateProgress(0, "Determining MRM settings")

            ' Initialize the tracking arrays
            Dim mrmHashToIndexMap = New Dictionary(Of String, clsMRMScanInfo)

            ' Construct a list of the MRM search values used
            For Each fragScan In scanList.FragScans
                If fragScan.MRMScanType = MRMScanTypeConstants.SRM Then
                    mrmDataPresent = True

                    ' See if this MRM spec is already in mrmSettings

                    mrmInfoHash = GenerateMRMInfoHash(fragScan.MRMScanInfo)

                    Dim mrmInfoForHash As clsMRMScanInfo = Nothing
                    If Not mrmHashToIndexMap.TryGetValue(mrmInfoHash, mrmInfoForHash) Then

                        mrmInfoForHash = DuplicateMRMInfo(fragScan.MRMScanInfo)

                        mrmInfoForHash.ScanCount = 1
                        mrmInfoForHash.ParentIonInfoIndex = fragScan.FragScanInfo.ParentIonInfoIndex

                        mrmSettings.Add(mrmInfoForHash)
                        mrmHashToIndexMap.Add(mrmInfoHash, mrmInfoForHash)


                        ' Append the new entries to srmList

                        For mrmMassIndex = 0 To mrmInfoForHash.MRMMassCount - 1

                            ' Add this new transition to srmList() only if not already present
                            matchFound = False
                            For Each srmItem In srmList
                                If MRMParentDaughterMatch(srmItem, mrmInfoForHash, mrmMassIndex) Then
                                    matchFound = True
                                    Exit For
                                End If
                            Next

                            If Not matchFound Then
                                ' Entry is not yet present; add it

                                Dim newSRMItem = New udtSRMListType() With {
                                    .ParentIonMZ = mrmInfoForHash.ParentIonMZ,
                                    .CentralMass = mrmInfoForHash.MRMMassList(mrmMassIndex).CentralMass
                                }

                                srmList.Add(newSRMItem)
                            End If
                        Next
                    Else
                        mrmInfoForHash.ScanCount += 1
                    End If

                End If
            Next

            If mrmDataPresent Then
                Return True
            Else
                Return False
            End If

        Catch ex As Exception
            ReportError("Error determining the MRM settings", ex, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

    End Function

    Public Shared Function DuplicateMRMInfo(
      oSource As MRMInfo,
      parentIonMZ As Double) As clsMRMScanInfo

        Dim oTarget = New clsMRMScanInfo()

        oTarget.ParentIonMZ = parentIonMZ
        oTarget.MRMMassCount = oSource.MRMMassList.Count

        If oSource.MRMMassList Is Nothing Then
            oTarget.MRMMassList = New List(Of udtMRMMassRangeType)
        Else
            oTarget.MRMMassList = New List(Of udtMRMMassRangeType)(oSource.MRMMassList.Count)
            oTarget.MRMMassList.AddRange(oSource.MRMMassList)
        End If

        oTarget.ScanCount = 0
        oTarget.ParentIonInfoIndex = -1

        Return oTarget
    End Function

    Private Function DuplicateMRMInfo(oSource As clsMRMScanInfo) As clsMRMScanInfo
        Dim oTarget = New clsMRMScanInfo()

        oTarget.ParentIonMZ = oSource.ParentIonMZ
        oTarget.MRMMassCount = oSource.MRMMassCount

        If oSource.MRMMassList Is Nothing Then
            oTarget.MRMMassList = New List(Of udtMRMMassRangeType)
        Else
            oTarget.MRMMassList = New List(Of udtMRMMassRangeType)(oSource.MRMMassList.Count)
            oTarget.MRMMassList.AddRange(oSource.MRMMassList)
        End If

        oTarget.ScanCount = oSource.ScanCount
        oTarget.ParentIonInfoIndex = oSource.ParentIonInfoIndex

        Return oTarget

    End Function

    Public Function ExportMRMDataToDisk(
      scanList As clsScanList,
      spectraCache As clsSpectraCache,
      inputFileName As String,
      outputDirectoryPath As String) As Boolean

        Dim mrmSettings As List(Of clsMRMScanInfo) = Nothing
        Dim srmList As List(Of udtSRMListType) = Nothing

        If Not DetermineMRMSettings(scanList, mrmSettings, srmList) Then
            Return False
        End If

        Dim success = ExportMRMDataToDisk(scanList, spectraCache, mrmSettings, srmList, inputFileName, outputDirectoryPath)

        Return success

    End Function

    Private Function ExportMRMDataToDisk(
      scanList As clsScanList,
      spectraCache As clsSpectraCache,
      mrmSettings As IReadOnlyList(Of clsMRMScanInfo),
      srmList As IReadOnlyList(Of udtSRMListType),
      inputFileName As String,
      outputDirectoryPath As String) As Boolean

        ' Returns true if the MRM data is successfully written to disk
        ' Note that it will also return true if udtMRMSettings() is empty

        Const cColDelimiter As Char = ControlChars.Tab

        Dim dataWriter As StreamWriter = Nothing
        Dim crosstabWriter As StreamWriter = Nothing

        Dim success As Boolean

        Try
            ' Only write this data if 1 or more fragmentation spectra are of type SRM
            If mrmSettings Is Nothing OrElse mrmSettings.Count = 0 Then
                success = True
                Exit Try
            End If

            UpdateProgress(0, "Exporting MRM data")

            ' Write out the MRM Settings
            Dim mrmSettingsFilePath = clsDataOutput.ConstructOutputFilePath(
                inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.MRMSettingsFile)

            Using settingsWriter = New StreamWriter(mrmSettingsFilePath)

                settingsWriter.WriteLine(mDataOutputHandler.GetHeadersForOutputFile(scanList, clsDataOutput.eOutputFileTypeConstants.MRMSettingsFile))

                Dim dataColumns = New List(Of String)

                For mrmInfoIndex = 0 To mrmSettings.Count - 1
                    With mrmSettings(mrmInfoIndex)
                        For mrmMassIndex = 0 To .MRMMassCount - 1
                            dataColumns.Clear()

                            dataColumns.Add(mrmInfoIndex.ToString())
                            dataColumns.Add(.ParentIonMZ.ToString("0.000"))
                            dataColumns.Add(.MRMMassList(mrmMassIndex).CentralMass.ToString("0.000"))
                            dataColumns.Add(.MRMMassList(mrmMassIndex).StartMass.ToString("0.000"))
                            dataColumns.Add(.MRMMassList(mrmMassIndex).EndMass.ToString("0.000"))
                            dataColumns.Add(.ScanCount.ToString())

                            settingsWriter.WriteLine(String.Join(cColDelimiter, dataColumns))
                        Next

                    End With
                Next

                If mOptions.WriteMRMDataList Or mOptions.WriteMRMIntensityCrosstab Then

                    ' Populate srmKeyToIndexMap
                    Dim srmKeyToIndexMap = New Dictionary(Of String, Integer)
                    For srmIndex = 0 To srmList.Count - 1
                        srmKeyToIndexMap.Add(ConstructSRMMapKey(srmList(srmIndex)), srmIndex)
                    Next

                    If mOptions.WriteMRMDataList Then
                        ' Write out the raw MRM Data
                        Dim dataFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.MRMDatafile)
                        dataWriter = New StreamWriter(dataFilePath)

                        ' Write the file headers
                        dataWriter.WriteLine(mDataOutputHandler.GetHeadersForOutputFile(scanList, clsDataOutput.eOutputFileTypeConstants.MRMDatafile))
                    End If

                    If mOptions.WriteMRMIntensityCrosstab Then
                        ' Write out the raw MRM Data
                        Dim crosstabFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.MRMCrosstabFile)
                        crosstabWriter = New StreamWriter(crosstabFilePath)

                        ' Initialize the crosstab header variable using the data in udtSRMList()

                        Dim headerNames = New List(Of String) From {
                            "Scan_First",
                            "ScanTime"
                        }

                        For srmIndex = 0 To srmList.Count - 1
                            headerNames.Add(ConstructSRMMapKey(srmList(srmIndex)))
                        Next

                        crosstabWriter.WriteLine(String.Join(cColDelimiter, headerNames))
                    End If

                    Dim scanFirst = Integer.MinValue
                    Dim scanTimeFirst As Single
                    Dim srmIndexLast = 0

                    Dim crosstabColumnValue() As Double
                    Dim crosstabColumnFlag() As Boolean

                    ReDim crosstabColumnValue(srmList.Count - 1)
                    ReDim crosstabColumnFlag(srmList.Count - 1)

                    'For scanIndex = 0 To scanList.FragScanCount - 1
                    For Each fragScan In scanList.FragScans
                        If fragScan.MRMScanType <> MRMScanTypeConstants.SRM Then
                            Continue For
                        End If

                        If scanFirst = Integer.MinValue Then
                            scanFirst = fragScan.ScanNumber
                            scanTimeFirst = fragScan.ScanTime
                        End If

                        ' Look for each of the m/z values specified in fragScan.MRMScanInfo.MRMMassList
                        For mrmMassIndex = 0 To fragScan.MRMScanInfo.MRMMassCount - 1
                            ' Find the maximum value between fragScan.StartMass and fragScan.EndMass
                            ' Need to define a tolerance to account for numeric rounding artifacts in the variables

                            Dim mzStart = fragScan.MRMScanInfo.MRMMassList(mrmMassIndex).StartMass
                            Dim mzEnd = fragScan.MRMScanInfo.MRMMassList(mrmMassIndex).EndMass
                            Dim mrmToleranceHalfWidth = Math.Round((mzEnd - mzStart) / 2, 6)
                            If mrmToleranceHalfWidth < 0.001 Then
                                mrmToleranceHalfWidth = 0.001
                            End If

                            Dim closestMZ As Double
                            Dim matchIntensity As Double

                            Dim matchFound = mDataAggregation.FindMaxValueInMZRange(
                                spectraCache, fragScan,
                                mzStart - mrmToleranceHalfWidth,
                                mzEnd + mrmToleranceHalfWidth,
                                closestMZ, matchIntensity)

                            If mOptions.WriteMRMDataList Then
                                dataColumns.Clear()
                                dataColumns.Add(fragScan.ScanNumber.ToString())
                                dataColumns.Add(fragScan.MRMScanInfo.ParentIonMZ.ToString("0.000"))

                                If matchFound Then
                                    dataColumns.Add(fragScan.MRMScanInfo.MRMMassList(mrmMassIndex).CentralMass.ToString("0.000"))
                                    dataColumns.Add(matchIntensity.ToString("0.000"))

                                Else
                                    dataColumns.Add(fragScan.MRMScanInfo.MRMMassList(mrmMassIndex).CentralMass.ToString("0.000"))
                                    dataColumns.Add("0")
                                End If

                                dataWriter.WriteLine(String.Join(cColDelimiter, dataColumns))
                            End If


                            If mOptions.WriteMRMIntensityCrosstab Then
                                Dim srmMapKey = ConstructSRMMapKey(fragScan.MRMScanInfo.ParentIonMZ, fragScan.MRMScanInfo.MRMMassList(mrmMassIndex).CentralMass)
                                Dim srmIndex As Integer

                                ' Use srmKeyToIndexMap to determine the appropriate column index for srmMapKey
                                If srmKeyToIndexMap.TryGetValue(srmMapKey, srmIndex) Then

                                    If crosstabColumnFlag(srmIndex) OrElse
                                       (srmIndex = 0 And srmIndexLast = srmList.Count - 1) Then
                                        ' Either the column is already populated, or the SRMIndex has cycled back to zero
                                        ' Write out the current crosstab line and reset the crosstab column arrays
                                        ExportMRMDataWriteLine(crosstabWriter, scanFirst, scanTimeFirst,
                                                               crosstabColumnValue,
                                                               crosstabColumnFlag,
                                                               cColDelimiter, True)

                                        scanFirst = fragScan.ScanNumber
                                        scanTimeFirst = fragScan.ScanTime
                                    End If

                                    If matchFound Then
                                        crosstabColumnValue(srmIndex) = matchIntensity
                                    End If
                                    crosstabColumnFlag(srmIndex) = True
                                    srmIndexLast = srmIndex
                                Else
                                    ' Unknown combination of parent ion m/z and daughter m/z; this is unexpected
                                    ' We won't write this entry out
                                    srmIndexLast = srmIndexLast
                                End If
                            End If

                        Next

                        UpdateCacheStats(spectraCache)
                        If mOptions.AbortProcessing Then
                            Exit For
                        End If
                    Next

                    If mOptions.WriteMRMIntensityCrosstab Then
                        ' Write out any remaining crosstab values
                        ExportMRMDataWriteLine(crosstabWriter, scanFirst, scanTimeFirst, crosstabColumnValue, crosstabColumnFlag, cColDelimiter, False)
                    End If

                End If

            End Using

            success = True

        Catch ex As Exception
            ReportError("Error writing the SRM data to disk", ex, eMasicErrorCodes.OutputFileWriteError)
            success = False
        Finally
            If Not dataWriter Is Nothing Then
                dataWriter.Close()
            End If

            If Not crosstabWriter Is Nothing Then
                crosstabWriter.Close()
            End If
        End Try

        Return success

    End Function

    Private Sub ExportMRMDataWriteLine(
      writer As TextWriter,
      scanFirst As Integer,
      scanTimeFirst As Single,
      crosstabColumnValue As IList(Of Double),
      crosstabColumnFlag As IList(Of Boolean),
      cColDelimiter As Char,
      forceWrite As Boolean)

        ' If forceWrite = False, then will only write out the line if 1 or more columns is non-zero

        Dim index As Integer
        Dim nonZeroCount = 0

        Dim dataColumns = New List(Of String) From {
            scanFirst.ToString(),
            StringUtilities.DblToString(scanTimeFirst, 5)
        }

        ' Construct a tab-delimited list of the values
        ' At the same time, clear the arrays
        For index = 0 To crosstabColumnValue.Count - 1
            If crosstabColumnValue(index) > 0 Then
                dataColumns.Add(crosstabColumnValue(index).ToString("0.000"))
                nonZeroCount += 1
            Else
                dataColumns.Add("0")
            End If

            crosstabColumnValue(index) = 0
            crosstabColumnFlag(index) = False
        Next

        If nonZeroCount > 0 OrElse forceWrite Then
            writer.WriteLine(String.Join(cColDelimiter, dataColumns))
        End If

    End Sub

    Private Function GenerateMRMInfoHash(mrmScanInfo As clsMRMScanInfo) As String
        Dim hashValue As String
        Dim index As Integer

        hashValue = mrmScanInfo.ParentIonMZ & "_" & mrmScanInfo.MRMMassCount

        For index = 0 To mrmScanInfo.MRMMassCount - 1
            hashValue &= "_" &
              mrmScanInfo.MRMMassList(index).CentralMass.ToString("0.000") & "_" &
              mrmScanInfo.MRMMassList(index).StartMass.ToString("0.000") & "_" &
              mrmScanInfo.MRMMassList(index).EndMass.ToString("0.000")

        Next

        Return hashValue

    End Function

    Private Function MRMParentDaughterMatch(
      ByRef udtSRMListEntry As udtSRMListType,
      mrmSettingsEntry As clsMRMScanInfo,
      mrmMassIndex As Integer) As Boolean

        Return MRMParentDaughterMatch(
          udtSRMListEntry.ParentIonMZ,
          udtSRMListEntry.CentralMass,
          mrmSettingsEntry.ParentIonMZ,
          mrmSettingsEntry.MRMMassList(mrmMassIndex).CentralMass)
    End Function

    Private Function MRMParentDaughterMatch(
      parentIonMZ1 As Double,
      mrmDaughterMZ1 As Double,
      parentIonMZ2 As Double,
      mrmDaughterMZ2 As Double) As Boolean

        Const COMPARISON_TOLERANCE = 0.01

        If Math.Abs(parentIonMZ1 - parentIonMZ2) <= COMPARISON_TOLERANCE AndAlso
           Math.Abs(mrmDaughterMZ1 - mrmDaughterMZ2) <= COMPARISON_TOLERANCE Then
            Return True
        Else
            Return False
        End If

    End Function

    Public Function ProcessMRMList(
      scanList As clsScanList,
      spectraCache As clsSpectraCache,
      sicProcessor As clsSICProcessing,
      xmlResultsWriter As clsXMLResultsWriter,
      peakFinder As clsMASICPeakFinder,
      ByRef parentIonsProcessed As Integer) As Boolean

        Try

            ' Initialize sicDetails
            Dim sicDetails = New clsSICDetails()
            sicDetails.Reset()
            sicDetails.SICScanType = clsScanList.eScanTypeConstants.FragScan

            Dim noiseStatsSegments = New List(Of clsBaselineNoiseStatsSegment)

            For parentIonIndex = 0 To scanList.ParentIons.Count - 1

                If scanList.ParentIons(parentIonIndex).MRMDaughterMZ <= 0 Then
                    Continue For
                End If

                ' Step 1: Create the SIC for this MRM Parent/Daughter pair

                Dim parentIonMZ = scanList.ParentIons(parentIonIndex).MZ
                Dim mrmDaughterMZ = scanList.ParentIons(parentIonIndex).MRMDaughterMZ
                Dim searchToleranceHalfWidth = scanList.ParentIons(parentIonIndex).MRMToleranceHalfWidth

                ' Reset SICData
                sicDetails.SICData.Clear()

                ' Step through the fragmentation spectra, finding those that have matching parent and daughter ion m/z values
                For scanIndex = 0 To scanList.FragScans.Count - 1
                    If scanList.FragScans(scanIndex).MRMScanType <> MRMScanTypeConstants.SRM Then
                        Continue For
                    End If

                    With scanList.FragScans(scanIndex)

                        Dim useScan = False
                        For mrmMassIndex = 0 To .MRMScanInfo.MRMMassCount - 1
                            If _
                                MRMParentDaughterMatch(.MRMScanInfo.ParentIonMZ,
                                                       .MRMScanInfo.MRMMassList(mrmMassIndex).CentralMass,
                                                       parentIonMZ, mrmDaughterMZ) Then
                                useScan = True
                                Exit For
                            End If
                        Next

                        If Not useScan Then Continue For

                        ' Include this scan in the SIC for this parent ion

                        Dim matchIntensity As Double
                        Dim closestMZ As Double

                        mDataAggregation.FindMaxValueInMZRange(spectraCache,
                                                              scanList.FragScans(scanIndex),
                                                              mrmDaughterMZ - searchToleranceHalfWidth,
                                                              mrmDaughterMZ + searchToleranceHalfWidth,
                                                              closestMZ, matchIntensity)

                        sicDetails.AddData(.ScanNumber, matchIntensity, closestMZ, scanIndex)

                    End With

                Next


                ' Step 2: Find the largest peak in the SIC

                ' Compute the noise level; the noise level may change with increasing index number if the background is increasing for a given m/z
                Dim success = peakFinder.ComputeDualTrimmedNoiseLevelTTest(sicDetails.SICIntensities, 0,
                                                                          sicDetails.SICDataCount - 1,
                                                                          mOptions.SICOptions.SICPeakFinderOptions.
                                                                             SICBaselineNoiseOptions,
                                                                          noiseStatsSegments)

                If Not success Then
                    SetLocalErrorCode(eMasicErrorCodes.FindSICPeaksError, True)
                    Return False
                End If

                ' Initialize the peak
                scanList.ParentIons(parentIonIndex).SICStats.Peak = New clsSICStatsPeak()

                ' Find the data point with the maximum intensity
                Dim maximumIntensity As Double = 0
                scanList.ParentIons(parentIonIndex).SICStats.Peak.IndexObserved = 0
                For scanIndex = 0 To sicDetails.SICDataCount - 1
                    Dim intensity = sicDetails.SICIntensities(scanIndex)
                    If intensity > maximumIntensity Then
                        maximumIntensity = intensity
                        scanList.ParentIons(parentIonIndex).SICStats.Peak.IndexObserved = scanIndex
                    End If
                Next

                Dim potentialAreaStatsInFullSIC As clsSICPotentialAreaStats = Nothing

                ' Compute the minimum potential peak area in the entire SIC, populating udtSICPotentialAreaStatsInFullSIC
                peakFinder.FindPotentialPeakArea(sicDetails.SICData,
                                                 potentialAreaStatsInFullSIC,
                                                 mOptions.SICOptions.SICPeakFinderOptions)

                ' Update .BaselineNoiseStats in scanList.ParentIons(parentIonIndex).SICStats.Peak
                scanList.ParentIons(parentIonIndex).SICStats.Peak.BaselineNoiseStats =
                    peakFinder.LookupNoiseStatsUsingSegments(
                        scanList.ParentIons(parentIonIndex).SICStats.Peak.IndexObserved,
                        noiseStatsSegments)

                Dim smoothedYDataSubset As clsSmoothedYDataSubset = Nothing

                With scanList.ParentIons(parentIonIndex)

                    ' Clear udtSICPotentialAreaStatsForPeak
                    .SICStats.SICPotentialAreaStatsForPeak = New clsSICPotentialAreaStats()

                    Dim peakIsValid = peakFinder.FindSICPeakAndArea(sicDetails.SICData,
                                                                    .SICStats.SICPotentialAreaStatsForPeak,
                                                                    .SICStats.Peak, smoothedYDataSubset,
                                                                    mOptions.SICOptions.SICPeakFinderOptions,
                                                                    potentialAreaStatsInFullSIC, False,
                                                                    scanList.SIMDataPresent, False)


                    sicProcessor.StorePeakInParentIon(scanList, parentIonIndex, sicDetails,
                                                      .SICStats.SICPotentialAreaStatsForPeak,
                                                      .SICStats.Peak, peakIsValid)
                End With


                ' Step 3: store the results

                ' Possibly save the stats for this SIC to the SICData file
                mDataOutputHandler.SaveSICDataToText(mOptions.SICOptions, scanList, parentIonIndex, sicDetails)

                ' Save the stats for this SIC to the XML file
                xmlResultsWriter.SaveDataToXML(scanList, parentIonIndex, sicDetails, smoothedYDataSubset,
                                               mDataOutputHandler)

                parentIonsProcessed += 1

                '---------------------------------------------------------
                ' Update progress
                '---------------------------------------------------------
                Try

                    If scanList.ParentIons.Count > 1 Then
                        UpdateProgress(CShort(parentIonsProcessed / (scanList.ParentIons.Count - 1) * 100))
                    Else
                        UpdateProgress(0)
                    End If

                    UpdateCacheStats(spectraCache)
                    If mOptions.AbortProcessing Then
                        scanList.ProcessingIncomplete = True
                        Exit For
                    End If

                    If parentIonsProcessed Mod 100 = 0 Then
                        If DateTime.UtcNow.Subtract(mOptions.LastParentIonProcessingLogTime).TotalSeconds >= 10 OrElse parentIonsProcessed Mod 500 = 0 Then
                            ReportMessage("Parent Ions Processed: " & parentIonsProcessed.ToString())
                            Console.Write(".")
                            mOptions.LastParentIonProcessingLogTime = DateTime.UtcNow
                        End If
                    End If

                Catch ex As Exception
                    ReportError("Error updating progress", ex, eMasicErrorCodes.CreateSICsError)
                End Try

            Next

            Return True

        Catch ex As Exception
            ReportError("Error creating SICs for MRM spectra", ex, eMasicErrorCodes.CreateSICsError)
            Return False
        End Try

    End Function

End Class
