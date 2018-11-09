
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

    ''' <summary>
    ''' Looks through arrayToSearch for itemToSearchFor
    ''' </summary>
    ''' <param name="arrayToSearch"></param>
    ''' <param name="itemToSearchFor"></param>
    ''' <param name="dataCount"></param>
    ''' <param name="eMissingDataMode"></param>
    ''' <returns>The index of the item if found, otherwise, the index of the closest match, based on eMissingDataMode</returns>
    ''' <remarks>Assumes arrayToSearch is already sorted</remarks>
    Public Shared Function BinarySearchFindNearest(
      arrayToSearch As IList(Of Integer), itemToSearchFor As Integer, dataCount As Integer,
      Optional eMissingDataMode As eMissingDataModeConstants = eMissingDataModeConstants.ReturnClosestPoint) As Integer

        Dim indexFirst As Integer, indexLast As Integer
        Dim midIndex As Integer
        Dim currentFirst As Integer, currentLast As Integer
        Dim matchIndex As Integer

        Try
            If arrayToSearch Is Nothing Then Return -1

            indexFirst = 0
            If dataCount > arrayToSearch.Count Then
                dataCount = arrayToSearch.Count
            End If
            indexLast = dataCount - 1

            currentFirst = indexFirst
            currentLast = indexLast

            If currentFirst > currentLast Then
                ' Invalid indices were provided
                matchIndex = -1
            ElseIf currentFirst = currentLast Then
                ' Search space is only one element long; simply return that element's index
                matchIndex = currentFirst
            Else
                midIndex = (currentFirst + currentLast) \ 2            ' Note: Using Integer division
                If midIndex < currentFirst Then midIndex = currentFirst

                Do While currentFirst <= currentLast AndAlso arrayToSearch(midIndex) <> itemToSearchFor
                    If itemToSearchFor < arrayToSearch(midIndex) Then
                        ' Search the lower half
                        currentLast = midIndex - 1
                    ElseIf itemToSearchFor > arrayToSearch(midIndex) Then
                        ' Search the upper half
                        currentFirst = midIndex + 1
                    End If

                    ' Compute the new mid point
                    midIndex = (currentFirst + currentLast) \ 2
                    If midIndex < currentFirst Then
                        midIndex = currentFirst
                        If midIndex > currentLast Then
                            midIndex = currentLast
                        End If
                        Exit Do
                    End If
                Loop

                matchIndex = -1
                ' See if an exact match has been found
                If midIndex >= currentFirst AndAlso midIndex <= currentLast Then
                    If arrayToSearch(midIndex) = itemToSearchFor Then
                        matchIndex = midIndex
                    End If
                End If

                If matchIndex = -1 Then
                    If eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint Then
                        ' No exact match; find the nearest match
                        If arrayToSearch(midIndex) < itemToSearchFor Then
                            If midIndex < indexLast Then
                                If Math.Abs(arrayToSearch(midIndex) - itemToSearchFor) <=
                                   Math.Abs(arrayToSearch(midIndex + 1) - itemToSearchFor) Then
                                    matchIndex = midIndex
                                Else
                                    matchIndex = midIndex + 1
                                End If
                            Else
                                matchIndex = indexLast
                            End If
                        Else
                            ' ArrayToSearch(midIndex) >= ItemToSearchFor
                            If midIndex > indexFirst Then
                                If Math.Abs(arrayToSearch(midIndex - 1) - itemToSearchFor) <=
                                   Math.Abs(arrayToSearch(midIndex) - itemToSearchFor) Then
                                    matchIndex = midIndex - 1
                                Else
                                    matchIndex = midIndex
                                End If
                            Else
                                matchIndex = indexFirst
                            End If
                        End If
                    Else
                        ' No exact match; return the previous point or the next point
                        If arrayToSearch(midIndex) < itemToSearchFor Then
                            If eMissingDataMode = eMissingDataModeConstants.ReturnNextPoint Then
                                matchIndex = midIndex + 1
                                If matchIndex > indexLast Then matchIndex = indexLast
                            Else
                                matchIndex = midIndex
                            End If
                        Else
                            ' ArrayToSearch(midIndex) >= ItemToSearchFor
                            If eMissingDataMode = eMissingDataModeConstants.ReturnNextPoint Then
                                matchIndex = midIndex
                            Else
                                matchIndex = midIndex - 1
                                If matchIndex < indexFirst Then matchIndex = indexFirst
                            End If
                        End If
                    End If
                End If
            End If

        Catch ex As Exception
            matchIndex = -1
        End Try

        Return matchIndex

    End Function

    ''' <summary>
    ''' Looks through arrayToSearch for itemToSearchFor
    ''' </summary>
    ''' <param name="arrayToSearch"></param>
    ''' <param name="itemToSearchFor"></param>
    ''' <param name="dataCount"></param>
    ''' <param name="eMissingDataMode"></param>
    ''' <returns>The index of the item if found, otherwise, the index of the closest match, based on eMissingDataMode</returns>
    ''' <remarks>Assumes arrayToSearch is already sorted</remarks>
    Public Shared Function BinarySearchFindNearest(
      arrayToSearch As IList(Of Single), itemToSearchFor As Single, dataCount As Integer,
      Optional eMissingDataMode As eMissingDataModeConstants = eMissingDataModeConstants.ReturnClosestPoint) As Integer

        Dim indexFirst As Integer, indexLast As Integer
        Dim midIndex As Integer
        Dim currentFirst As Integer, currentLast As Integer
        Dim matchIndex As Integer

        Try
            If arrayToSearch Is Nothing Then Return -1

            indexFirst = 0
            If dataCount > arrayToSearch.Count Then
                dataCount = arrayToSearch.Count
            End If
            indexLast = dataCount - 1

            currentFirst = indexFirst
            currentLast = indexLast

            If currentFirst > currentLast Then
                ' Invalid indices were provided
                matchIndex = -1
            ElseIf currentFirst = currentLast Then
                ' Search space is only one element long; simply return that element's index
                matchIndex = currentFirst
            Else
                midIndex = (currentFirst + currentLast) \ 2            ' Note: Using Integer division
                If midIndex < currentFirst Then midIndex = currentFirst

                Do While currentFirst <= currentLast AndAlso Math.Abs(arrayToSearch(midIndex) - itemToSearchFor) > Single.Epsilon
                    If itemToSearchFor < arrayToSearch(midIndex) Then
                        ' Search the lower half
                        currentLast = midIndex - 1
                    ElseIf itemToSearchFor > arrayToSearch(midIndex) Then
                        ' Search the upper half
                        currentFirst = midIndex + 1
                    End If

                    ' Compute the new mid point
                    midIndex = (currentFirst + currentLast) \ 2
                    If midIndex < currentFirst Then
                        midIndex = currentFirst
                        If midIndex > currentLast Then
                            midIndex = currentLast
                        End If
                        Exit Do
                    End If
                Loop

                matchIndex = -1
                ' See if an exact match has been found
                If midIndex >= currentFirst AndAlso midIndex <= currentLast Then
                    If Math.Abs(arrayToSearch(midIndex) - itemToSearchFor) < Single.Epsilon Then
                        matchIndex = midIndex
                    End If
                End If

                If matchIndex = -1 Then
                    If eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint Then
                        ' No exact match; find the nearest match
                        If arrayToSearch(midIndex) < itemToSearchFor Then
                            If midIndex < indexLast Then
                                If Math.Abs(arrayToSearch(midIndex) - itemToSearchFor) <=
                                   Math.Abs(arrayToSearch(midIndex + 1) - itemToSearchFor) Then
                                    matchIndex = midIndex
                                Else
                                    matchIndex = midIndex + 1
                                End If
                            Else
                                matchIndex = indexLast
                            End If
                        Else
                            ' ArrayToSearch(midIndex) >= ItemToSearchFor
                            If midIndex > indexFirst Then
                                If Math.Abs(arrayToSearch(midIndex - 1) - itemToSearchFor) <=
                                   Math.Abs(arrayToSearch(midIndex) - itemToSearchFor) Then
                                    matchIndex = midIndex - 1
                                Else
                                    matchIndex = midIndex
                                End If
                            Else
                                matchIndex = indexFirst
                            End If
                        End If
                    Else
                        ' No exact match; return the previous point or the next point
                        If arrayToSearch(midIndex) < itemToSearchFor Then
                            If eMissingDataMode = eMissingDataModeConstants.ReturnNextPoint Then
                                matchIndex = midIndex + 1
                                If matchIndex > indexLast Then matchIndex = indexLast
                            Else
                                matchIndex = midIndex
                            End If
                        Else
                            ' ArrayToSearch(midIndex) >= ItemToSearchFor
                            If eMissingDataMode = eMissingDataModeConstants.ReturnNextPoint Then
                                matchIndex = midIndex
                            Else
                                matchIndex = midIndex - 1
                                If matchIndex < indexFirst Then matchIndex = indexFirst
                            End If
                        End If
                    End If
                End If
            End If

        Catch ex As Exception
            matchIndex = -1
        End Try

        Return matchIndex

    End Function

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Looks through arrayToSearch for itemToSearchFor
    ''' </summary>
    ''' <param name="arrayToSearch"></param>
    ''' <param name="itemToSearchFor"></param>
    ''' <param name="dataCount"></param>
    ''' <param name="eMissingDataMode"></param>
    ''' <returns>The index of the item if found, otherwise, the index of the closest match, based on eMissingDataMode</returns>
    ''' <remarks>Assumes arrayToSearch is already sorted</remarks>
    Public Shared Function BinarySearchFindNearest(
      arrayToSearch As IList(Of Double), itemToSearchFor As Double, dataCount As Integer,
      Optional eMissingDataMode As eMissingDataModeConstants = eMissingDataModeConstants.ReturnClosestPoint) As Integer

        Dim indexFirst As Integer, indexLast As Integer
        Dim midIndex As Integer
        Dim currentFirst As Integer, currentLast As Integer
        Dim matchIndex As Integer

        Try
            If arrayToSearch Is Nothing Then Return -1

            indexFirst = 0
            If dataCount > arrayToSearch.Count Then
                dataCount = arrayToSearch.Count
            End If
            indexLast = dataCount - 1

            currentFirst = indexFirst
            currentLast = indexLast

            If currentFirst > currentLast Then
                ' Invalid indices were provided
                matchIndex = -1
            ElseIf currentFirst = currentLast Then
                ' Search space is only one element long; simply return that element's index
                matchIndex = currentFirst
            Else
                midIndex = (currentFirst + currentLast) \ 2            ' Note: Using Integer division
                If midIndex < currentFirst Then midIndex = currentFirst

                Do While currentFirst <= currentLast AndAlso Math.Abs(arrayToSearch(midIndex) - itemToSearchFor) > Single.Epsilon
                    If itemToSearchFor < arrayToSearch(midIndex) Then
                        ' Search the lower half
                        currentLast = midIndex - 1
                    ElseIf itemToSearchFor > arrayToSearch(midIndex) Then
                        ' Search the upper half
                        currentFirst = midIndex + 1
                    End If

                    ' Compute the new mid point
                    midIndex = (currentFirst + currentLast) \ 2
                    If midIndex < currentFirst Then
                        midIndex = currentFirst
                        If midIndex > currentLast Then
                            midIndex = currentLast
                        End If
                        Exit Do
                    End If
                Loop

                matchIndex = -1
                ' See if an exact match has been found
                If midIndex >= currentFirst AndAlso midIndex <= currentLast Then
                    If Math.Abs(arrayToSearch(midIndex) - itemToSearchFor) < Double.Epsilon Then
                        matchIndex = midIndex
                    End If
                End If

                If matchIndex = -1 Then
                    If eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint Then
                        ' No exact match; find the nearest match
                        If arrayToSearch(midIndex) < itemToSearchFor Then
                            If midIndex < indexLast Then
                                If Math.Abs(arrayToSearch(midIndex) - itemToSearchFor) <=
                                   Math.Abs(arrayToSearch(midIndex + 1) - itemToSearchFor) Then
                                    matchIndex = midIndex
                                Else
                                    matchIndex = midIndex + 1
                                End If
                            Else
                                matchIndex = indexLast
                            End If
                        Else
                            ' ArrayToSearch(midIndex) >= ItemToSearchFor
                            If midIndex > indexFirst Then
                                If Math.Abs(arrayToSearch(midIndex - 1) - itemToSearchFor) <=
                                   Math.Abs(arrayToSearch(midIndex) - itemToSearchFor) Then
                                    matchIndex = midIndex - 1
                                Else
                                    matchIndex = midIndex
                                End If
                            Else
                                matchIndex = indexFirst
                            End If
                        End If
                    Else
                        ' No exact match; return the previous point or the next point
                        If arrayToSearch(midIndex) < itemToSearchFor Then
                            If eMissingDataMode = eMissingDataModeConstants.ReturnNextPoint Then
                                matchIndex = midIndex + 1
                                If matchIndex > indexLast Then matchIndex = indexLast
                            Else
                                matchIndex = midIndex
                            End If
                        Else
                            ' ArrayToSearch(midIndex) >= ItemToSearchFor
                            If eMissingDataMode = eMissingDataModeConstants.ReturnNextPoint Then
                                matchIndex = midIndex
                            Else
                                matchIndex = midIndex - 1
                                If matchIndex < indexFirst Then matchIndex = indexFirst
                            End If
                        End If
                    End If
                End If

            End If

        Catch ex As Exception
            matchIndex = -1
        End Try

        Return matchIndex

    End Function

End Class
