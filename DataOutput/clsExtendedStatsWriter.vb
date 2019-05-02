Imports System.Runtime.InteropServices
Imports MASIC.clsMASIC
Imports MASIC.DataOutput.clsDataOutput

Namespace DataOutput
    Public Class clsExtendedStatsWriter
        Inherits clsMasicEventNotifier

#Region "Constants and enums"
        Public Const EXTENDED_STATS_HEADER_COLLISION_MODE As String = "Collision Mode"
        Public Const EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT As String = "Scan Filter Text"
#End Region

#Region "Properties"
        Public ReadOnly Property ExtendedHeaderNameCount As Integer
            Get
                Return mExtendedHeaderNameMap.Count
            End Get
        End Property

#End Region

#Region "Classwide Variables"

        ''' <summary>
        ''' Keys are strings of extended info names
        ''' Values are the assigned ID value for the extended info name
        ''' </summary>
        ''' <remarks>The order of the values defines the appropriate output order for the names</remarks>
        Private ReadOnly mExtendedHeaderNameMap As List(Of KeyValuePair(Of String, Integer))

        Private ReadOnly mOptions As clsMASICOptions
#End Region

        ''' <summary>
        ''' Constructor
        ''' </summary>
        Public Sub New(masicOptions As clsMASICOptions)
            mExtendedHeaderNameMap = New List(Of KeyValuePair(Of String, Integer))
            mOptions = masicOptions
        End Sub

        Private Function ConcatenateExtendedStats(
          nonConstantHeaderIDs As IEnumerable(Of Integer),
          datasetID As Integer,
          scanNumber As Integer,
          extendedHeaderInfo As IReadOnlyDictionary(Of Integer, String)) As IEnumerable(Of String)

            Dim dataValues = New List(Of String) From {
                datasetID.ToString(),
                scanNumber.ToString()
            }

            If Not extendedHeaderInfo Is Nothing AndAlso Not nonConstantHeaderIDs Is Nothing Then

                For Each headerID In (From item In nonConstantHeaderIDs Order By item Select item)

                    Dim value As String = Nothing
                    If extendedHeaderInfo.TryGetValue(headerID, value) Then
                        If clsUtilities.IsNumber(value) Then
                            If Math.Abs(Val(value)) < Single.Epsilon Then value = "0"
                        Else
                            ' ReSharper disable StringLiteralTypo
                            Select Case value
                                Case "ff" : value = "Off"
                                Case "n" : value = "On"
                                Case "eady" : value = "Ready"""
                                Case "cquiring" : value = "Acquiring"
                                Case "oad" : value = "Load"
                            End Select
                            ' ReSharper restore StringLiteralTypo
                        End If
                        dataValues.Add(value)
                    Else
                        dataValues.Add("0")
                    End If
                Next
            End If

            Return dataValues

        End Function

        Public Function ConstructExtendedStatsHeaders() As List(Of String)

            Dim cTrimChars = New Char() {":"c, " "c}

            Dim headerNames = New List(Of String) From {
                "Dataset",
                "ScanNumber"
            }

            ' Populate headerNames

            If mExtendedHeaderNameMap.Count <= 0 Then
                Return headerNames
            End If

            Dim headerNamesByID = New Dictionary(Of Integer, String)

            For Each item In mExtendedHeaderNameMap
                headerNamesByID.Add(item.Value, item.Key)
            Next

            For Each headerItem In (From item In headerNamesByID Order By item.Key Select item.Value)
                headerNames.Add(headerItem.TrimEnd(cTrimChars))
            Next

            Return headerNames

        End Function

        Private Function DeepCopyHeaderInfoDictionary(sourceTable As Dictionary(Of Integer, String)) As Dictionary(Of Integer, String)
            Dim newTable = New Dictionary(Of Integer, String)

            For Each item In sourceTable
                newTable.Add(item.Key, item.Value)
            Next

            Return newTable

        End Function

        ''' <summary>
        ''' Looks through surveyScans and fragScans for ExtendedHeaderInfo values that are constant across all scans
        ''' </summary>
        ''' <param name="nonConstantHeaderIDs">Output: the ID values of the header values that are not constant</param>
        ''' <param name="surveyScans"></param>
        ''' <param name="fragScans"></param>
        ''' <param name="cColDelimiter"></param>
        ''' <returns>
        ''' String that is a newline separated list of header values that are constant, tab delimited, and their constant values, also tab delimited
        ''' Each line is in the form ParameterName_ColumnDelimiter_ParameterValue
        ''' </returns>
        ''' <remarks>mExtendedHeaderNameMap is updated so that constant header values are removed from it</remarks>
        Public Function ExtractConstantExtendedHeaderValues(
          <Out> ByRef nonConstantHeaderIDs As List(Of Integer),
          surveyScans As IList(Of clsScanInfo),
          fragScans As IList(Of clsScanInfo),
          cColDelimiter As Char) As String

            Dim cTrimChars = New Char() {":"c, " "c}

            Dim value As String = String.Empty

            ' Keys are ID values pointing to mExtendedHeaderNameMap (where the name is defined); values are the string or numeric values for the settings
            Dim consolidatedValuesByID As Dictionary(Of Integer, String)

            Dim constantHeaderIDs = New List(Of Integer)

            Dim scanFilterTextHeaderID As Integer

            nonConstantHeaderIDs = New List(Of Integer)

            If mExtendedHeaderNameMap.Count = 0 Then
                Return String.Empty
            End If

            ' Initialize nonConstantHeaderIDs
            For i = 0 To mExtendedHeaderNameMap.Count - 1
                nonConstantHeaderIDs.Add(i)
            Next

            If Not mOptions.ConsolidateConstantExtendedHeaderValues Then
                ' Do not try to consolidate anything
                Return String.Empty
            End If

            If surveyScans.Count > 0 Then
                consolidatedValuesByID = DeepCopyHeaderInfoDictionary(surveyScans(0).ExtendedHeaderInfo)
            ElseIf fragScans.Count > 0 Then
                consolidatedValuesByID = DeepCopyHeaderInfoDictionary(fragScans(0).ExtendedHeaderInfo)
            Else
                Return String.Empty
            End If

            If consolidatedValuesByID Is Nothing Then
                Return String.Empty
            End If

            ' Look for "Scan Filter Text" in mExtendedHeaderNameMap
            If TryGetExtendedHeaderInfoValue(EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, scanFilterTextHeaderID) Then
                ' Match found

                ' Now look for and remove the HeaderID value from consolidatedValuesByID to prevent the scan filter text from being included in the consolidated values file
                If consolidatedValuesByID.ContainsKey(scanFilterTextHeaderID) Then
                    consolidatedValuesByID.Remove(scanFilterTextHeaderID)
                End If
            End If

            ' Examine the values in .ExtendedHeaderInfo() in the survey scans and compare them
            ' to the values in consolidatedValuesByID, looking to see if they match
            For Each surveyScan In surveyScans
                If Not surveyScan.ExtendedHeaderInfo Is Nothing Then
                    For Each dataItem In surveyScan.ExtendedHeaderInfo
                        If consolidatedValuesByID.TryGetValue(dataItem.Key, value) Then
                            If String.Equals(value, dataItem.Value) Then
                                ' Value matches; nothing to do
                            Else
                                ' Value differs; remove the key from consolidatedValuesByID
                                consolidatedValuesByID.Remove(dataItem.Key)
                            End If
                        End If
                    Next
                End If
            Next

            ' Examine the values in .ExtendedHeaderInfo() in the frag scans and compare them
            ' to the values in consolidatedValuesByID, looking to see if they match
            For Each fragScan In fragScans
                If Not fragScan.ExtendedHeaderInfo Is Nothing Then
                    For Each item In fragScan.ExtendedHeaderInfo
                        If consolidatedValuesByID.TryGetValue(item.Key, value) Then
                            If String.Equals(value, item.Value) Then
                                ' Value matches; nothing to do
                            Else
                                ' Value differs; remove key from consolidatedValuesByID
                                consolidatedValuesByID.Remove(item.Key)
                            End If
                        End If
                    Next
                End If
            Next

            If consolidatedValuesByID Is Nothing OrElse consolidatedValuesByID.Count = 0 Then
                Return String.Empty
            End If

            ' Populate consolidatedValueList with the values in consolidatedValuesByID,
            '  separating each header and value with a tab and separating each pair of values with a NewLine character

            ' Need to first populate constantHeaderIDs with the ID values and sort the list so that the values are
            '  stored in consolidatedValueList in the correct order

            Dim consolidatedValueList = New List(Of String) From {
                "Setting" & cColDelimiter & "Value"
            }

            For Each item In consolidatedValuesByID
                constantHeaderIDs.Add(item.Key)
            Next

            Dim keysToRemove = New List(Of String)

            For Each headerId In (From item In constantHeaderIDs Order By item Select item)

                For Each item In mExtendedHeaderNameMap
                    If item.Value = headerId Then
                        consolidatedValueList.Add(item.Key.TrimEnd(cTrimChars) & cColDelimiter & consolidatedValuesByID(headerId))
                        keysToRemove.Add(item.Key)
                        Exit For
                    End If
                Next
            Next

            ' Remove the elements from mExtendedHeaderNameMap that were included in consolidatedValueList;
            '  we couldn't remove these above since that would invalidate the iHeaderEnum enumerator

            For Each keyName In keysToRemove
                For headerIndex = 0 To mExtendedHeaderNameMap.Count - 1
                    If mExtendedHeaderNameMap(headerIndex).Key = keyName Then
                        mExtendedHeaderNameMap.RemoveAt(headerIndex)
                        Exit For
                    End If
                Next
            Next

            nonConstantHeaderIDs.Clear()

            ' Populate nonConstantHeaderIDs with the ID values in mExtendedHeaderNameMap
            For Each item In mExtendedHeaderNameMap
                nonConstantHeaderIDs.Add(item.Value)
            Next

            Return String.Join(ControlChars.NewLine, consolidatedValueList)

        End Function

        Private Function GetScanByMasterScanIndex(scanList As clsScanList, masterScanIndex As Integer) As clsScanInfo
            Dim currentScan = New clsScanInfo()
            If Not scanList.MasterScanOrder Is Nothing Then
                If masterScanIndex < 0 Then
                    masterScanIndex = 0
                ElseIf masterScanIndex >= scanList.MasterScanOrderCount Then
                    masterScanIndex = scanList.MasterScanOrderCount - 1
                End If

                Select Case scanList.MasterScanOrder(masterScanIndex).ScanType
                    Case clsScanList.eScanTypeConstants.SurveyScan
                        ' Survey scan
                        currentScan = scanList.SurveyScans(scanList.MasterScanOrder(masterScanIndex).ScanIndexPointer)
                    Case clsScanList.eScanTypeConstants.FragScan
                        ' Frag Scan
                        currentScan = scanList.FragScans(scanList.MasterScanOrder(masterScanIndex).ScanIndexPointer)
                    Case Else
                        ' Unknown scan type
                End Select
            End If

            Return currentScan

        End Function

        Public Function SaveExtendedScanStatsFiles(
          scanList As clsScanList,
          inputFileName As String,
          outputDirectoryPath As String,
          includeHeaders As Boolean) As Boolean

            ' Writes out a flat file containing the extended scan stats

            Dim extendedConstantHeaderOutputFilePath As String
            Dim extendedNonConstantHeaderOutputFilePath As String = String.Empty

            Const cColDelimiter As Char = ControlChars.Tab

            Dim nonConstantHeaderIDs As List(Of Integer) = Nothing

            Try
                UpdateProgress(0, "Saving extended scan stats to flat file")

                extendedConstantHeaderOutputFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.ScanStatsExtendedConstantFlatFile)
                extendedNonConstantHeaderOutputFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.ScanStatsExtendedFlatFile)

                ReportMessage("Saving extended scan stats flat file to disk: " & Path.GetFileName(extendedNonConstantHeaderOutputFilePath))

                If mExtendedHeaderNameMap.Count = 0 Then
                    ' No extended stats to write; exit the function
                    Exit Try
                End If

                ' Lookup extended stats values that are constants for all scans
                ' The following will also remove the constant header values from mExtendedHeaderNameMap
                Dim constantExtendedHeaderValues = ExtractConstantExtendedHeaderValues(nonConstantHeaderIDs, scanList.SurveyScans, scanList.FragScans, cColDelimiter)
                If constantExtendedHeaderValues Is Nothing Then constantExtendedHeaderValues = String.Empty

                ' Write the constant extended stats values to a text file
                Using writer = New StreamWriter(extendedConstantHeaderOutputFilePath, False)
                    writer.WriteLine(constantExtendedHeaderValues)
                End Using

                ' Now open another output file for the non-constant extended stats
                Using writer = New StreamWriter(extendedNonConstantHeaderOutputFilePath, False)

                    If includeHeaders Then
                        Dim headerNames = ConstructExtendedStatsHeaders()
                        writer.WriteLine(String.Join(cColDelimiter, headerNames))
                    End If

                    For scanIndex = 0 To scanList.MasterScanOrderCount - 1

                        Dim currentScan = GetScanByMasterScanIndex(scanList, scanIndex)

                        Dim dataColumns As IEnumerable(Of String) = ConcatenateExtendedStats(nonConstantHeaderIDs, mOptions.SICOptions.DatasetNumber, currentScan.ScanNumber, currentScan.ExtendedHeaderInfo)
                        writer.WriteLine(String.Join(cColDelimiter, dataColumns))

                        If scanIndex Mod 100 = 0 Then
                            UpdateProgress(CShort(scanIndex / (scanList.MasterScanOrderCount - 1) * 100))
                        End If
                    Next

                End Using

            Catch ex As Exception
                ReportError("Error writing the Extended Scan Stats to: " & extendedNonConstantHeaderOutputFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            UpdateProgress(100)
            Return True

        End Function

        Public Function GetExtendedHeaderInfoIdByName(keyName As String) As Integer

            Dim idValue As Integer

            If TryGetExtendedHeaderInfoValue(keyName, idValue) Then
                ' Match found
            Else
                ' Match not found; add it
                idValue = mExtendedHeaderNameMap.Count
                mExtendedHeaderNameMap.Add(New KeyValuePair(Of String, Integer)(keyName, idValue))
            End If

            Return idValue

        End Function

        Private Function TryGetExtendedHeaderInfoValue(keyName As String, <Out> ByRef headerIndex As Integer) As Boolean

            Dim query = (From item In mExtendedHeaderNameMap Where item.Key = keyName Select item.Value).ToList()
            headerIndex = 0

            If query.Count = 0 Then
                Return False
            End If

            headerIndex = query.First()
            Return True

        End Function

    End Class

End Namespace
