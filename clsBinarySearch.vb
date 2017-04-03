Option Strict On

''' <summary>
''' This class can be used to search a list of values for the value closest to the search value
''' If an exact match is found, then the index of that result is returned
''' If an exact match is not found, then the MissingDataMode defines which value will be returned (closest, always previous, or always next)
''' </summary>
''' <remarks>The search functions assume the input data is already sorted</remarks>
Public Class clsBinarySearch

    Public Enum eMissingDataModeConstants
        ReturnClosestPoint = 0
        ReturnPreviousPoint = 1
        ReturnNextPoint = 2
    End Enum

    Public Shared Function BinarySearchFindNearest(
      intArrayToSearch() As Integer, intItemToSearchFor As Integer, intDataCount As Integer,
      Optional eMissingDataMode As eMissingDataModeConstants = eMissingDataModeConstants.ReturnClosestPoint) As Integer
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
                                If Math.Abs(intArrayToSearch(intMidIndex) - intItemToSearchFor) <=
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
                                If Math.Abs(intArrayToSearch(intMidIndex - 1) - intItemToSearchFor) <=
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

    Public Shared Function BinarySearchFindNearest(
      sngArrayToSearch() As Single, sngItemToSearchFor As Single, intDataCount As Integer,
      Optional eMissingDataMode As eMissingDataModeConstants = eMissingDataModeConstants.ReturnClosestPoint) As Integer
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

                Do While intCurrentFirst <= intCurrentLast AndAlso Math.Abs(sngArrayToSearch(intMidIndex) - sngItemToSearchFor) > Single.Epsilon
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
                    If Math.Abs(sngArrayToSearch(intMidIndex) - sngItemToSearchFor) < Single.Epsilon Then
                        intMatchIndex = intMidIndex
                    End If
                End If

                If intMatchIndex = -1 Then
                    If eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint Then
                        ' No exact match; find the nearest match
                        If sngArrayToSearch(intMidIndex) < sngItemToSearchFor Then
                            If intMidIndex < intIndexLast Then
                                If Math.Abs(sngArrayToSearch(intMidIndex) - sngItemToSearchFor) <=
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
                                If Math.Abs(sngArrayToSearch(intMidIndex - 1) - sngItemToSearchFor) <=
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

    Public Shared Function BinarySearchFindNearest(
      dblArrayToSearch() As Double, dblItemToSearchFor As Double, intDataCount As Integer,
      Optional eMissingDataMode As eMissingDataModeConstants = eMissingDataModeConstants.ReturnClosestPoint) As Integer
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

                Do While intCurrentFirst <= intCurrentLast AndAlso Math.Abs(dblArrayToSearch(intMidIndex) - dblItemToSearchFor) > Single.Epsilon
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
                    If Math.Abs(dblArrayToSearch(intMidIndex) - dblItemToSearchFor) < Double.Epsilon Then
                        intMatchIndex = intMidIndex
                    End If
                End If

                If intMatchIndex = -1 Then
                    If eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint Then
                        ' No exact match; find the nearest match
                        If dblArrayToSearch(intMidIndex) < dblItemToSearchFor Then
                            If intMidIndex < intIndexLast Then
                                If Math.Abs(dblArrayToSearch(intMidIndex) - dblItemToSearchFor) <=
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
                                If Math.Abs(dblArrayToSearch(intMidIndex - 1) - dblItemToSearchFor) <=
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

End Class
