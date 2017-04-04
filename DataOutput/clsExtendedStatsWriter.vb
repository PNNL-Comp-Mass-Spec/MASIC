Imports System.Runtime.InteropServices
Imports MASIC.clsMASIC
Imports MASIC.DataOutput.clsDataOutput

Namespace DataOutput
    Public Class clsExtendedStatsWriter
        Inherits clsEventNotifier

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
          ByRef intNonConstantHeaderIDs() As Integer,
          intDatasetID As Integer,
          intScanNumber As Integer,
          ExtendedHeaderInfo As IReadOnlyDictionary(Of Integer, String),
          cColDelimiter As Char) As String

            Dim strOutLine = intDatasetID.ToString() & cColDelimiter & intScanNumber.ToString() & cColDelimiter

            If Not ExtendedHeaderInfo Is Nothing AndAlso Not intNonConstantHeaderIDs Is Nothing Then
                For intIndex = 0 To intNonConstantHeaderIDs.Length - 1

                    Dim strValue As String = Nothing
                    If ExtendedHeaderInfo.TryGetValue(intNonConstantHeaderIDs(intIndex), strValue) Then
                        If clsUtilities.IsNumber(strValue) Then
                            If Math.Abs(Val(strValue)) < Single.Epsilon Then strValue = "0"
                        Else
                            Select Case strValue
                                Case "ff" : strValue = "Off"
                                Case "n" : strValue = "On"
                                Case "eady" : strValue = "Ready"""
                                Case "cquiring" : strValue = "Acquiring"
                                Case "oad" : strValue = "Load"
                            End Select
                        End If
                        strOutLine &= strValue & cColDelimiter
                    Else
                        strOutLine &= "0" & cColDelimiter
                    End If
                Next

                ' remove the trailing delimiter
                If strOutLine.Length > 0 Then
                    strOutLine = strOutLine.TrimEnd(cColDelimiter)
                End If

            End If

            Return strOutLine

        End Function

        Public Function ConstructExtendedStatsHeaders(cColDelimiter As Char) As String

            Dim cTrimChars = New Char() {":"c, " "c}
            Dim strHeaderNames() As String

            Dim intIndex As Integer
            Dim strHeaders As String

            Dim intHeaderIDs() As Integer
            Dim intHeaderCount As Integer

            strHeaders = "Dataset" & cColDelimiter & "ScanNumber" & cColDelimiter

            ' Populate strHeaders

            If mExtendedHeaderNameMap.Count > 0 Then
                ReDim strHeaderNames(mExtendedHeaderNameMap.Count - 1)
                ReDim intHeaderIDs(mExtendedHeaderNameMap.Count - 1)

                intHeaderCount = 0
                For Each item In mExtendedHeaderNameMap
                    strHeaderNames(intHeaderCount) = item.Key
                    intHeaderIDs(intHeaderCount) = item.Value
                    intHeaderCount += 1
                Next

                Array.Sort(intHeaderIDs, strHeaderNames)

                For intIndex = 0 To intHeaderCount - 1
                    strHeaders &= strHeaderNames(intIndex).TrimEnd(cTrimChars) & cColDelimiter
                Next

                ' remove the trailing delimiter
                If strHeaders.Length > 0 Then
                    strHeaders = strHeaders.TrimEnd(cColDelimiter)
                End If
            End If

            Return strHeaders

        End Function

        Private Function DeepCopyHeaderInfoDictionary(sourceTable As Dictionary(Of Integer, String)) As Dictionary(Of Integer, String)
            Dim newTable = New Dictionary(Of Integer, String)

            For Each item In sourceTable
                newTable.Add(item.Key, item.Value)
            Next

            Return newTable

        End Function

        Public Function ExtractConstantExtendedHeaderValues(
          ByRef intNonConstantHeaderIDs() As Integer,
          surveyScans As IList(Of clsScanInfo),
          fragScans As IList(Of clsScanInfo),
          cColDelimiter As Char) As String

            ' Looks through surveyScans and fragScans for ExtendedHeaderInfo values that are constant across all scans
            ' Returns a string containing the header values that are constant, tab delimited, and their constant values, also tab delimited
            ' intNonConstantHeaderIDs() returns the ID values of the header values that are not constant
            ' mExtendedHeaderNameMap is updated so that constant header values are removed from it

            Dim cTrimChars = New Char() {":"c, " "c}

            Dim intNonConstantCount As Integer

            Dim strValue As String = String.Empty

            Dim htConsolidatedValues As Dictionary(Of Integer, String)

            Dim intConstantHeaderIDs() As Integer

            Dim intScanFilterTextHeaderID As Integer

            If mExtendedHeaderNameMap.Count = 0 Then
                Return String.Empty
            End If

            ' Initialize this for now; it will get re-dimmed below if any constant values are found
            ReDim intNonConstantHeaderIDs(mExtendedHeaderNameMap.Count - 1)
            For intIndex = 0 To mExtendedHeaderNameMap.Count - 1
                intNonConstantHeaderIDs(intIndex) = intIndex
            Next

            If Not mOptions.ConsolidateConstantExtendedHeaderValues Then
                ' Do not try to consolidate anything
                Return String.Empty
            End If

            If surveyScans.Count > 0 Then
                htConsolidatedValues = DeepCopyHeaderInfoDictionary(surveyScans(0).ExtendedHeaderInfo)
            ElseIf fragScans.Count > 0 Then
                htConsolidatedValues = DeepCopyHeaderInfoDictionary(fragScans(0).ExtendedHeaderInfo)
            Else
                Return String.Empty
            End If

            If htConsolidatedValues Is Nothing Then
                Return String.Empty
            End If

            ' Look for "Scan Filter Text" in mExtendedHeaderNameMap
            If TryGetExtendedHeaderInfoValue(EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, intScanFilterTextHeaderID) Then
                ' Match found

                ' Now look for and remove the HeaderID value from htConsolidatedValues to prevent the scan filter text from being included in the consolidated values file
                If htConsolidatedValues.ContainsKey(intScanFilterTextHeaderID) Then
                    htConsolidatedValues.Remove(intScanFilterTextHeaderID)
                End If
            End If

            ' Examine the values in .ExtendedHeaderInfo() in the survey scans and compare them
            ' to the values in htConsolidatedValues, looking to see if they match
            For Each surveyScan In surveyScans
                If Not surveyScan.ExtendedHeaderInfo Is Nothing Then
                    For Each dataItem In surveyScan.ExtendedHeaderInfo
                        If htConsolidatedValues.TryGetValue(dataItem.Key, strValue) Then
                            If String.Equals(strValue, dataItem.Value) Then
                                ' Value matches; nothing to do
                            Else
                                ' Value differs; remove key from htConsolidatedValues
                                htConsolidatedValues.Remove(dataItem.Key)
                            End If
                        End If
                    Next
                End If
            Next

            ' Examine the values in .ExtendedHeaderInfo() in the frag scans and compare them
            ' to the values in htConsolidatedValues, looking to see if they match
            For Each fragScan In fragScans
                If Not fragScan.ExtendedHeaderInfo Is Nothing Then
                    For Each item In fragScan.ExtendedHeaderInfo
                        If htConsolidatedValues.TryGetValue(item.Key, strValue) Then
                            If String.Equals(strValue, item.Value) Then
                                ' Value matches; nothing to do
                            Else
                                ' Value differs; remove key from htConsolidatedValues
                                htConsolidatedValues.Remove(item.Key)
                            End If
                        End If
                    Next
                End If
            Next

            If htConsolidatedValues Is Nothing OrElse htConsolidatedValues.Count = 0 Then
                Return String.Empty
            End If

            ' Populate strConsolidatedValues with the values in htConsolidatedValues, 
            '  separating each header and value with a tab and separating each pair of values with a NewLine character
            ' Need to first populate intConstantHeaderIDs with the ID values and sort the list so that the values are
            '  stored in strConsolidatedValueList in the correct order

            Dim strConsolidatedValueList = "Setting" & cColDelimiter & "Value" & ControlChars.NewLine

            ReDim intConstantHeaderIDs(htConsolidatedValues.Count - 1)

            Dim targetIndex = 0
            For Each item In htConsolidatedValues
                intConstantHeaderIDs(targetIndex) = item.Key
                targetIndex += 1
            Next

            Array.Sort(intConstantHeaderIDs)

            Dim htKeysToRemove = New List(Of String)

            For headerIndex = 0 To intConstantHeaderIDs.Length - 1

                For Each item In mExtendedHeaderNameMap
                    If item.Value = intConstantHeaderIDs(headerIndex) Then
                        strConsolidatedValueList &= item.Key.TrimEnd(cTrimChars) & cColDelimiter
                        strConsolidatedValueList &= htConsolidatedValues(intConstantHeaderIDs(headerIndex)) & ControlChars.NewLine
                        htKeysToRemove.Add(item.Key)
                        Exit For
                    End If
                Next
            Next

            ' Remove the elements from mExtendedHeaderNameMap that were included in strConsolidatedValueList;
            '  we couldn't remove these above since that would invalidate the iHeaderEnum enumerator

            For Each keyName In htKeysToRemove
                For headerIndex = 0 To mExtendedHeaderNameMap.Count - 1
                    If mExtendedHeaderNameMap(headerIndex).Key = keyName Then
                        mExtendedHeaderNameMap.RemoveAt(headerIndex)
                        Exit For
                    End If
                Next
            Next

            ReDim intNonConstantHeaderIDs(mExtendedHeaderNameMap.Count - 1)
            intNonConstantCount = 0

            ' Populate intNonConstantHeaderIDs with the ID values in mExtendedHeaderNameMap
            For Each item In mExtendedHeaderNameMap
                intNonConstantHeaderIDs(intNonConstantCount) = item.Value
                intNonConstantCount += 1
            Next

            Array.Sort(intNonConstantHeaderIDs)

            Return strConsolidatedValueList

        End Function

        Private Function GetScanByMasterScanIndex(scanList As clsScanList, intMasterScanIndex As Integer) As clsScanInfo
            Dim currentScan = New clsScanInfo()
            If Not scanList.MasterScanOrder Is Nothing Then
                If intMasterScanIndex < 0 Then
                    intMasterScanIndex = 0
                ElseIf intMasterScanIndex >= scanList.MasterScanOrderCount Then
                    intMasterScanIndex = scanList.MasterScanOrderCount - 1
                End If

                Select Case scanList.MasterScanOrder(intMasterScanIndex).ScanType
                    Case clsScanList.eScanTypeConstants.SurveyScan
                        ' Survey scan
                        currentScan = scanList.SurveyScans(scanList.MasterScanOrder(intMasterScanIndex).ScanIndexPointer)
                    Case clsScanList.eScanTypeConstants.FragScan
                        ' Frag Scan
                        currentScan = scanList.FragScans(scanList.MasterScanOrder(intMasterScanIndex).ScanIndexPointer)
                    Case Else
                        ' Unkown scan type
                End Select
            End If

            Return currentScan

        End Function

        Public Function SaveExtendedScanStatsFiles(
          scanList As clsScanList,
          strInputFileName As String,
          strOutputFolderPath As String,
          blnIncludeHeaders As Boolean) As Boolean

            ' Writes out a flat file containing the extended scan stats

            Dim strExtendedConstantHeaderOutputFilePath As String
            Dim strExtendedNonConstantHeaderOutputFilePath As String = String.Empty

            Const cColDelimiter As Char = ControlChars.Tab

            Dim intNonConstantHeaderIDs() As Integer = Nothing

            Try
                UpdateProgress(0, "Saving extended scan stats to flat file")

                strExtendedConstantHeaderOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.ScanStatsExtendedConstantFlatFile)
                strExtendedNonConstantHeaderOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.ScanStatsExtendedFlatFile)

                ReportMessage("Saving extended scan stats flat file to disk: " & Path.GetFileName(strExtendedNonConstantHeaderOutputFilePath))

                If mExtendedHeaderNameMap.Count = 0 Then
                    ' No extended stats to write; exit the function
                    Exit Try
                End If

                ' Lookup extended stats values that are constants for all scans
                ' The following will also remove the constant header values from mExtendedHeaderNameMap
                Dim strConstantExtendedHeaderValues = ExtractConstantExtendedHeaderValues(intNonConstantHeaderIDs, scanList.SurveyScans, scanList.FragScans, cColDelimiter)
                If strConstantExtendedHeaderValues Is Nothing Then strConstantExtendedHeaderValues = String.Empty

                ' Write the constant extended stats values to a text file
                Using srOutFile = New StreamWriter(strExtendedConstantHeaderOutputFilePath, False)
                    srOutFile.WriteLine(strConstantExtendedHeaderValues)
                End Using

                ' Now open another output file for the non-constant extended stats
                Using srOutFile = New StreamWriter(strExtendedNonConstantHeaderOutputFilePath, False)

                    If blnIncludeHeaders Then
                        Dim strOutLine = ConstructExtendedStatsHeaders(cColDelimiter)
                        srOutFile.WriteLine(strOutLine)
                    End If

                    For intScanIndex = 0 To scanList.MasterScanOrderCount - 1

                        Dim currentScan = GetScanByMasterScanIndex(scanList, intScanIndex)

                        Dim strOutLine = ConcatenateExtendedStats(intNonConstantHeaderIDs, mOptions.SICOptions.DatasetNumber, currentScan.ScanNumber, currentScan.ExtendedHeaderInfo, cColDelimiter)
                        srOutFile.WriteLine(strOutLine)

                        If intScanIndex Mod 100 = 0 Then
                            UpdateProgress(CShort(intScanIndex / (scanList.MasterScanOrderCount - 1) * 100))
                        End If
                    Next

                End Using

            Catch ex As Exception
                ReportError("SaveExtendedScanStatsFiles", "Error writing the Extended Scan Stats to: " & strExtendedNonConstantHeaderOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            UpdateProgress(100)
            Return True

        End Function

        Public Function GetExtendedHeaderInfoIdByName(keyName As String) As Integer

            Dim intIDValue As Integer

            If TryGetExtendedHeaderInfoValue(keyName, intIDValue) Then
                ' Match found
            Else
                ' Match not found; add it
                intIDValue = mExtendedHeaderNameMap.Count
                mExtendedHeaderNameMap.Add(New KeyValuePair(Of String, Integer)(keyName, intIDValue))
            End If

            Return intIDValue

        End Function

        Private Function TryGetExtendedHeaderInfoValue(keyName As String, <Out()> ByRef headerIndex As Integer) As Boolean

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
