Option Strict On

' This class can be used to search a list of values for the value closest to the search value
' If an exact match is found, then the index of that result is returned
' If an exact match is not found, then the MissingDataMode defines which value will be returned (closest, always previous, or always next)
'
' Note: The search functions assume the input data is already sorted
'
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Copyright 2008, Battelle Memorial Institute.  All Rights Reserved.
'
' Last modified April 17, 2008

Public Class clsBinarySearch

    Public Enum eMissingDataModeConstants
        ReturnClosestPoint = 0
        ReturnPreviousPoint = 1
        ReturnNextPoint = 2
    End Enum

    Public Shared Function BinarySearchFindNearest(ByRef intArrayToSearch() As Integer, ByVal intItemToSearchFor As Integer, ByVal intDataCount As Integer, Optional ByVal eMissingDataMode As eMissingDataModeConstants = eMissingDataModeConstants.ReturnClosestPoint) As Integer
        ' Looks through intArrayToSearch() for intItemToSearchFor, returning
        '  the index of the item if found
		' If not found, returns the index of the closest match, based on eMissingDataMode
        ' Assumes intArrayToSearch() is already sorted

        Dim intIndexFirst As Integer, intIndexLast As Integer
        Dim intMidIndex As Integer
        Dim intCurrentFirst As Integer, intCurrentLast As Integer
        Dim intMatchIndex As Integer

        Try
            If intArrayToSearch Is Nothing Then Return -1

            intIndexFirst = 0
            If intDataCount > intArrayToSearch.Length Then
                intDataCount = intArrayToSearch.Length
            End If
            intIndexLast = intDataCount - 1

            intCurrentFirst = intIndexFirst
            intCurrentLast = intIndexLast

            If intCurrentFirst > intCurrentLast Then
                ' Invalid indices were provided
                intMatchIndex = -1
            ElseIf intCurrentFirst = intCurrentLast Then
                ' Search space is only one element long; simply return that element's index
                intMatchIndex = intCurrentFirst
            Else
                intMidIndex = (intCurrentFirst + intCurrentLast) \ 2            ' Note: Using Integer division
                If intMidIndex < intCurrentFirst Then intMidIndex = intCurrentFirst

                Do While intCurrentFirst <= intCurrentLast AndAlso intArrayToSearch(intMidIndex) <> intItemToSearchFor
                    If intItemToSearchFor < intArrayToSearch(intMidIndex) Then
                        ' Search the lower half
                        intCurrentLast = intMidIndex - 1
                    ElseIf intItemToSearchFor > intArrayToSearch(intMidIndex) Then
                        ' Search the upper half
                        intCurrentFirst = intMidIndex + 1
                    End If

                    ' Compute the new mid point
                    intMidIndex = (intCurrentFirst + intCurrentLast) \ 2
                    If intMidIndex < intCurrentFirst Then
                        intMidIndex = intCurrentFirst
                        If intMidIndex > intCurrentLast Then
                            intMidIndex = intCurrentLast
                        End If
                        Exit Do
                    End If
                Loop

                intMatchIndex = -1
                ' See if an exact match has been found
                If intMidIndex >= intCurrentFirst AndAlso intMidIndex <= intCurrentLast Then
                    If intArrayToSearch(intMidIndex) = intItemToSearchFor Then
                        intMatchIndex = intMidIndex
                    End If
                End If

                If intMatchIndex = -1 Then
                    If eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint Then
                        ' No exact match; find the nearest match
                        If intArrayToSearch(intMidIndex) < intItemToSearchFor Then
                            If intMidIndex < intIndexLast Then
                                If Math.Abs(intArrayToSearch(intMidIndex) - intItemToSearchFor) <= _
                                   Math.Abs(intArrayToSearch(intMidIndex + 1) - intItemToSearchFor) Then
                                    intMatchIndex = intMidIndex
                                Else
                                    intMatchIndex = intMidIndex + 1
                                End If
                            Else
                                intMatchIndex = intIndexLast
                            End If
                        Else
                            ' ArrayToSearch(intMidIndex) >= ItemToSearchFor
                            If intMidIndex > intIndexFirst Then
                                If Math.Abs(intArrayToSearch(intMidIndex - 1) - intItemToSearchFor) <= _
                                   Math.Abs(intArrayToSearch(intMidIndex) - intItemToSearchFor) Then
                                    intMatchIndex = intMidIndex - 1
                                Else
                                    intMatchIndex = intMidIndex
                                End If
                            Else
                                intMatchIndex = intIndexFirst
                            End If
                        End If
                    Else
                        ' No exact match; return the previous point or the next point
                        If intArrayToSearch(intMidIndex) < intItemToSearchFor Then
                            If eMissingDataMode = eMissingDataModeConstants.ReturnNextPoint Then
                                intMatchIndex = intMidIndex + 1
                                If intMatchIndex > intIndexLast Then intMatchIndex = intIndexLast
                            Else
                                intMatchIndex = intMidIndex
                            End If
                        Else
                            ' ArrayToSearch(intMidIndex) >= ItemToSearchFor
                            If eMissingDataMode = eMissingDataModeConstants.ReturnNextPoint Then
                                intMatchIndex = intMidIndex
                            Else
                                intMatchIndex = intMidIndex - 1
                                If intMatchIndex < intIndexFirst Then intMatchIndex = intIndexFirst
                            End If
                        End If
                    End If
                End If
            End If

        Catch ex As Exception
            intMatchIndex = -1
        End Try

        Return intMatchIndex

    End Function

    Public Shared Function BinarySearchFindNearest(ByRef sngArrayToSearch() As Single, ByVal sngItemToSearchFor As Single, ByVal intDataCount As Integer, Optional ByVal eMissingDataMode As eMissingDataModeConstants = eMissingDataModeConstants.ReturnClosestPoint) As Integer
        ' Looks through sngArrayToSearch() for sngItemToSearchFor, returning
        '  the index of the item if found
        ' If not found, returns the index of the closest match, returning the next highest if blnReturnNextHighestIfMissing = True, or the next lowest if blnReturnNextHighestIfMissing = false
        ' Assumes sngArrayToSearch() is already sorted

        Dim intIndexFirst As Integer, intIndexLast As Integer
        Dim intMidIndex As Integer
        Dim intCurrentFirst As Integer, intCurrentLast As Integer
        Dim intMatchIndex As Integer

        Try
            If sngArrayToSearch Is Nothing Then Return -1

            intIndexFirst = 0
            If intDataCount > sngArrayToSearch.Length Then
                intDataCount = sngArrayToSearch.Length
            End If
            intIndexLast = intDataCount - 1

            intCurrentFirst = intIndexFirst
            intCurrentLast = intIndexLast

            If intCurrentFirst > intCurrentLast Then
                ' Invalid indices were provided
                intMatchIndex = -1
            ElseIf intCurrentFirst = intCurrentLast Then
                ' Search space is only one element long; simply return that element's index
                intMatchIndex = intCurrentFirst
            Else
                intMidIndex = (intCurrentFirst + intCurrentLast) \ 2            ' Note: Using Integer division
                If intMidIndex < intCurrentFirst Then intMidIndex = intCurrentFirst

                Do While intCurrentFirst <= intCurrentLast AndAlso sngArrayToSearch(intMidIndex) <> sngItemToSearchFor
                    If sngItemToSearchFor < sngArrayToSearch(intMidIndex) Then
                        ' Search the lower half
                        intCurrentLast = intMidIndex - 1
                    ElseIf sngItemToSearchFor > sngArrayToSearch(intMidIndex) Then
                        ' Search the upper half
                        intCurrentFirst = intMidIndex + 1
                    End If

                    ' Compute the new mid point
                    intMidIndex = (intCurrentFirst + intCurrentLast) \ 2
                    If intMidIndex < intCurrentFirst Then
                        intMidIndex = intCurrentFirst
                        If intMidIndex > intCurrentLast Then
                            intMidIndex = intCurrentLast
                        End If
                        Exit Do
                    End If
                Loop

                intMatchIndex = -1
                ' See if an exact match has been found
                If intMidIndex >= intCurrentFirst AndAlso intMidIndex <= intCurrentLast Then
                    If sngArrayToSearch(intMidIndex) = sngItemToSearchFor Then
                        intMatchIndex = intMidIndex
                    End If
                End If

                If intMatchIndex = -1 Then
                    If eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint Then
                        ' No exact match; find the nearest match
                        If sngArrayToSearch(intMidIndex) < sngItemToSearchFor Then
                            If intMidIndex < intIndexLast Then
                                If Math.Abs(sngArrayToSearch(intMidIndex) - sngItemToSearchFor) <= _
                                   Math.Abs(sngArrayToSearch(intMidIndex + 1) - sngItemToSearchFor) Then
                                    intMatchIndex = intMidIndex
                                Else
                                    intMatchIndex = intMidIndex + 1
                                End If
                            Else
                                intMatchIndex = intIndexLast
                            End If
                        Else
                            ' ArrayToSearch(intMidIndex) >= ItemToSearchFor
                            If intMidIndex > intIndexFirst Then
                                If Math.Abs(sngArrayToSearch(intMidIndex - 1) - sngItemToSearchFor) <= _
                                   Math.Abs(sngArrayToSearch(intMidIndex) - sngItemToSearchFor) Then
                                    intMatchIndex = intMidIndex - 1
                                Else
                                    intMatchIndex = intMidIndex
                                End If
                            Else
                                intMatchIndex = intIndexFirst
                            End If
                        End If
                    Else
                        ' No exact match; return the previous point or the next point
                        If sngArrayToSearch(intMidIndex) < sngItemToSearchFor Then
                            If eMissingDataMode = eMissingDataModeConstants.ReturnNextPoint Then
                                intMatchIndex = intMidIndex + 1
                                If intMatchIndex > intIndexLast Then intMatchIndex = intIndexLast
                            Else
                                intMatchIndex = intMidIndex
                            End If
                        Else
                            ' ArrayToSearch(intMidIndex) >= ItemToSearchFor
                            If eMissingDataMode = eMissingDataModeConstants.ReturnNextPoint Then
                                intMatchIndex = intMidIndex
                            Else
                                intMatchIndex = intMidIndex - 1
                                If intMatchIndex < intIndexFirst Then intMatchIndex = intIndexFirst
                            End If
                        End If
                    End If
                End If
            End If

        Catch ex As Exception
            intMatchIndex = -1
        End Try

        Return intMatchIndex

    End Function

    Public Shared Function BinarySearchFindNearest(ByRef dblArrayToSearch() As Double, ByVal dblItemToSearchFor As Double, ByVal intDataCount As Integer, Optional ByVal eMissingDataMode As eMissingDataModeConstants = eMissingDataModeConstants.ReturnClosestPoint) As Integer
        ' Looks through dblArrayToSearch() for dblItemToSearchFor, returning
        '  the index of the item if found
        ' If not found, returns the index of the closest match, returning the next highest if blnReturnNextHighestIfMissing = True, or the next lowest if blnReturnNextHighestIfMissing = false
        ' Assumes dblArrayToSearch() is already sorted

        Dim intIndexFirst As Integer, intIndexLast As Integer
        Dim intMidIndex As Integer
        Dim intCurrentFirst As Integer, intCurrentLast As Integer
        Dim intMatchIndex As Integer

        Try
            If dblArrayToSearch Is Nothing Then Return -1

            intIndexFirst = 0
            If intDataCount > dblArrayToSearch.Length Then
                intDataCount = dblArrayToSearch.Length
            End If
            intIndexLast = intDataCount - 1

            intCurrentFirst = intIndexFirst
            intCurrentLast = intIndexLast

            If intCurrentFirst > intCurrentLast Then
                ' Invalid indices were provided
                intMatchIndex = -1
            ElseIf intCurrentFirst = intCurrentLast Then
                ' Search space is only one element long; simply return that element's index
                intMatchIndex = intCurrentFirst
            Else
                intMidIndex = (intCurrentFirst + intCurrentLast) \ 2            ' Note: Using Integer division
                If intMidIndex < intCurrentFirst Then intMidIndex = intCurrentFirst

                Do While intCurrentFirst <= intCurrentLast AndAlso dblArrayToSearch(intMidIndex) <> dblItemToSearchFor
                    If dblItemToSearchFor < dblArrayToSearch(intMidIndex) Then
                        ' Search the lower half
                        intCurrentLast = intMidIndex - 1
                    ElseIf dblItemToSearchFor > dblArrayToSearch(intMidIndex) Then
                        ' Search the upper half
                        intCurrentFirst = intMidIndex + 1
                    End If

                    ' Compute the new mid point
                    intMidIndex = (intCurrentFirst + intCurrentLast) \ 2
                    If intMidIndex < intCurrentFirst Then
                        intMidIndex = intCurrentFirst
                        If intMidIndex > intCurrentLast Then
                            intMidIndex = intCurrentLast
                        End If
                        Exit Do
                    End If
                Loop

                intMatchIndex = -1
                ' See if an exact match has been found
                If intMidIndex >= intCurrentFirst AndAlso intMidIndex <= intCurrentLast Then
                    If dblArrayToSearch(intMidIndex) = dblItemToSearchFor Then
                        intMatchIndex = intMidIndex
                    End If
                End If

                If intMatchIndex = -1 Then
                    If eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint Then
                        ' No exact match; find the nearest match
                        If dblArrayToSearch(intMidIndex) < dblItemToSearchFor Then
                            If intMidIndex < intIndexLast Then
                                If Math.Abs(dblArrayToSearch(intMidIndex) - dblItemToSearchFor) <= _
                                   Math.Abs(dblArrayToSearch(intMidIndex + 1) - dblItemToSearchFor) Then
                                    intMatchIndex = intMidIndex
                                Else
                                    intMatchIndex = intMidIndex + 1
                                End If
                            Else
                                intMatchIndex = intIndexLast
                            End If
                        Else
                            ' ArrayToSearch(intMidIndex) >= ItemToSearchFor
                            If intMidIndex > intIndexFirst Then
                                If Math.Abs(dblArrayToSearch(intMidIndex - 1) - dblItemToSearchFor) <= _
                                   Math.Abs(dblArrayToSearch(intMidIndex) - dblItemToSearchFor) Then
                                    intMatchIndex = intMidIndex - 1
                                Else
                                    intMatchIndex = intMidIndex
                                End If
                            Else
                                intMatchIndex = intIndexFirst
                            End If
                        End If
                    Else
                        ' No exact match; return the previous point or the next point
                        If dblArrayToSearch(intMidIndex) < dblItemToSearchFor Then
                            If eMissingDataMode = eMissingDataModeConstants.ReturnNextPoint Then
                                intMatchIndex = intMidIndex + 1
                                If intMatchIndex > intIndexLast Then intMatchIndex = intIndexLast
                            Else
                                intMatchIndex = intMidIndex
                            End If
                        Else
                            ' ArrayToSearch(intMidIndex) >= ItemToSearchFor
                            If eMissingDataMode = eMissingDataModeConstants.ReturnNextPoint Then
                                intMatchIndex = intMidIndex
                            Else
                                intMatchIndex = intMidIndex - 1
                                If intMatchIndex < intIndexFirst Then intMatchIndex = intIndexFirst
                            End If
                        End If
                    End If
                End If

            End If

        Catch ex As Exception
            intMatchIndex = -1
        End Try

        Return intMatchIndex

    End Function

    Public Sub TestSearchFunctionsInt()
        Const DATA_MODE_COUNT As Integer = 3

        Dim intDataList() As Integer
        Dim intSearchResults(,) As Integer
        Dim intMaxDataValue As Integer

        Dim intIndex As Integer
        Dim intDataMode As Integer

        Dim intSearchValueStart As Integer
        Dim intSearchValueEnd As Integer
        Dim intSearchResultIndex As Integer
        Dim intIndexMatch As Integer

        Dim eMissingDataMode As eMissingDataModeConstants

        Dim srOutFile As StreamWriter
        Dim strLineOut As String

        Try
            ' Initialize a data list with 10 items
            ReDim intDataList(19)

            For intIndex = 0 To intDataList.Length - 1
                intMaxDataValue = intIndex + CInt(Math.Pow(intIndex, 1.5))
                intDataList(intIndex) = intMaxDataValue
            Next intIndex

            Array.Sort(intDataList)

            ' Write the data to disk
            srOutFile = New StreamWriter("BinarySearch_Test_Int.txt", False)

            srOutFile.WriteLine("Data_Index" & ControlChars.Tab & "Data_Value")
            For intIndex = 0 To intDataList.Length - 1
                srOutFile.WriteLine(intIndex.ToString & ControlChars.Tab & intDataList(intIndex).ToString)
            Next intIndex

            srOutFile.WriteLine()

            ' Initialize intSearchResults
            ' Note that intSearchResults(0,x) will contain the search values
            '  while intSearchResults(1,x) and up will contain the search reuslts

            intSearchValueStart = -10
            intSearchValueEnd = intMaxDataValue + 10

            ReDim intSearchResults(DATA_MODE_COUNT, intSearchValueEnd - intSearchValueStart)

            intSearchResultIndex = 0
            For intIndex = intSearchValueStart To intSearchValueEnd
                intSearchResults(0, intSearchResultIndex) = intIndex
                intSearchResultIndex += 1
            Next intIndex

            ' Search intDataList for each number between intSearchValueStart and intSearchValueEnd
            For intDataMode = 0 To DATA_MODE_COUNT - 1
                eMissingDataMode = CType(intDataMode, eMissingDataModeConstants)

                For intIndex = 0 To intSearchResults.GetUpperBound(1)
                    intIndexMatch = BinarySearchFindNearest(intDataList, intSearchResults(0, intIndex), intDataList.Length, eMissingDataMode)
                    intSearchResults(intDataMode + 1, intIndex) = intIndexMatch
                Next intIndex

            Next intDataMode

            ' Write the results to disk
            strLineOut = "Search Value"
            For intDataMode = 0 To DATA_MODE_COUNT - 1
                strLineOut &= ControlChars.Tab & GetMissingDataModeName(CType(intDataMode, eMissingDataModeConstants)) & " Value"
            Next

            For intDataMode = 0 To DATA_MODE_COUNT - 1
                strLineOut &= ControlChars.Tab & GetMissingDataModeName(CType(intDataMode, eMissingDataModeConstants))
            Next

            srOutFile.WriteLine(strLineOut)

            intSearchResultIndex = 0
            For intIndex = 0 To intSearchResults.GetUpperBound(1)
                strLineOut = intSearchResults(0, intIndex).ToString

                For intDataMode = 1 To DATA_MODE_COUNT
                    strLineOut &= ControlChars.Tab & intDataList(intSearchResults(intDataMode, intIndex)).ToString
                Next intDataMode

                For intDataMode = 1 To DATA_MODE_COUNT
                    strLineOut &= ControlChars.Tab & intSearchResults(intDataMode, intIndex).ToString
                Next intDataMode

                srOutFile.WriteLine(strLineOut)

            Next intIndex

            srOutFile.Close()

        Catch ex As Exception
            Console.WriteLine("Error in clsBinarySearch->TestSearchFunctions: " & ex.Message)
        End Try

    End Sub

    Public Sub TestSearchFunctionsDbl()
        Const DATA_MODE_COUNT As Integer = 3

        Dim dblDataList() As Double
        Dim dblSearchResults(,) As Double
        Dim dblMaxDataValue As Double

        Dim intIndex As Integer
        Dim intDataMode As Integer

        Dim intSearchValueStart As Integer
        Dim intSearchValueEnd As Integer
        Dim intSearchResultIndex As Integer
        Dim intIndexMatch As Integer

        Dim eMissingDataMode As eMissingDataModeConstants

        Dim srOutFile As StreamWriter
        Dim strLineOut As String

        Try
            ' Initialize a data list with 10 items
            ReDim dblDataList(19)

            For intIndex = 0 To dblDataList.Length - 1
                dblMaxDataValue = intIndex + CDbl(Math.Pow(intIndex, 1.5))
                dblDataList(intIndex) = dblMaxDataValue
            Next intIndex

            Array.Sort(dblDataList)

            ' Write the data to disk
            srOutFile = New StreamWriter("BinarySearch_Test_Double.txt", False)

            srOutFile.WriteLine("Data_Index" & ControlChars.Tab & "Data_Value")
            For intIndex = 0 To dblDataList.Length - 1
                srOutFile.WriteLine(intIndex.ToString & ControlChars.Tab & dblDataList(intIndex).ToString)
            Next intIndex

            srOutFile.WriteLine()

            ' Initialize dblSearchResults
            ' Note that dblSearchResults(0,x) will contain the search values
            '  while dblSearchResults(1,x) and up will contain the search reuslts

            intSearchValueStart = -10
            intSearchValueEnd = CInt(dblMaxDataValue + 10)

            ReDim dblSearchResults(DATA_MODE_COUNT, intSearchValueEnd - intSearchValueStart)

            intSearchResultIndex = 0
            For intIndex = intSearchValueStart To intSearchValueEnd
                dblSearchResults(0, intSearchResultIndex) = CDbl(intIndex) + intIndex / 10.0
                intSearchResultIndex += 1
            Next intIndex

            ' Search dblDataList for each number between intSearchValueStart and intSearchValueEnd
            For intDataMode = 0 To DATA_MODE_COUNT - 1
                eMissingDataMode = CType(intDataMode, eMissingDataModeConstants)

                For intIndex = 0 To dblSearchResults.GetUpperBound(1)
                    intIndexMatch = BinarySearchFindNearest(dblDataList, dblSearchResults(0, intIndex), dblDataList.Length, eMissingDataMode)
                    dblSearchResults(intDataMode + 1, intIndex) = intIndexMatch
                Next intIndex

            Next intDataMode

            ' Write the results to disk
            strLineOut = "Search Value"
            For intDataMode = 0 To DATA_MODE_COUNT - 1
                strLineOut &= ControlChars.Tab & GetMissingDataModeName(CType(intDataMode, eMissingDataModeConstants)) & " Value"
            Next

            For intDataMode = 0 To DATA_MODE_COUNT - 1
                strLineOut &= ControlChars.Tab & GetMissingDataModeName(CType(intDataMode, eMissingDataModeConstants))
            Next

            srOutFile.WriteLine(strLineOut)

            intSearchResultIndex = 0
            For intIndex = 0 To dblSearchResults.GetUpperBound(1)
                strLineOut = dblSearchResults(0, intIndex).ToString

                For intDataMode = 1 To DATA_MODE_COUNT
                    strLineOut &= ControlChars.Tab & dblDataList(CInt(dblSearchResults(intDataMode, intIndex))).ToString
                Next intDataMode

                For intDataMode = 1 To DATA_MODE_COUNT
                    strLineOut &= ControlChars.Tab & dblSearchResults(intDataMode, intIndex).ToString
                Next intDataMode

                srOutFile.WriteLine(strLineOut)

            Next intIndex

            srOutFile.Close()

        Catch ex As Exception
            Console.WriteLine("Error in clsBinarySearch->TestSearchFunctions: " & ex.Message)
        End Try

    End Sub

    Private Function GetMissingDataModeName(ByVal eMissingDataMode As eMissingDataModeConstants) As String
        Select Case eMissingDataMode
            Case eMissingDataModeConstants.ReturnClosestPoint
                Return "Return Closest Point"
            Case eMissingDataModeConstants.ReturnPreviousPoint
                Return "Return Previous Point"
            Case eMissingDataModeConstants.ReturnNextPoint
                Return "Return Next Point"
            Case Else
                Return "Unknown mode"
        End Select
    End Function
End Class
