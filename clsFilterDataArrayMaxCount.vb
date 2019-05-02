Option Strict On

Public Class clsFilterDataArrayMaxCount

    ' This class can be used to select the top N data points in a list, sorting descending
    ' It does not require a full sort of the data, which allows for faster filtering of the data
    '
    ' To use, first call AddDataPoint() for each source data point, specifying the value to sort on and a data point index
    ' When done, call FilterData()
    '  This routine will determine which data points to retain
    '  For the remaining points, their data values will be changed to mSkipDataPointFlag (defaults to -1)

    Private Const INITIAL_MEMORY_RESERVE As Integer = 50000

    Private Const DEFAULT_SKIP_DATA_POINT_FLAG As Single = -1

    ' 4 steps in Sub FilterDataByMaxDataCountToLoad
    Private Const SUBTASK_STEP_COUNT As Integer = 4


    Private mDataCount As Integer
    Private mDataValues() As Single
    Private mDataIndices() As Integer

    Private mMaximumDataCountToKeep As Integer

    Private mSkipDataPointFlag As Single

    Private mTotalIntensityPercentageFilterEnabled As Boolean
    Private mTotalIntensityPercentageFilter As Single

    Private mProgress As Single     ' Value between 0 and 100

    Public Event ProgressChanged(progressVal As Single)

#Region "Properties"
    Public Property MaximumDataCountToLoad As Integer
        Get
            Return mMaximumDataCountToKeep
        End Get
        Set
            mMaximumDataCountToKeep = Value
        End Set
    End Property

    Public ReadOnly Property Progress As Single
        Get
            Return mProgress
        End Get
    End Property

    Public Property SkipDataPointFlag As Single
        Get
            Return mSkipDataPointFlag
        End Get
        Set
            mSkipDataPointFlag = Value
        End Set
    End Property

    Public Property TotalIntensityPercentageFilterEnabled As Boolean
        Get
            Return mTotalIntensityPercentageFilterEnabled
        End Get
        Set
            mTotalIntensityPercentageFilterEnabled = Value
        End Set
    End Property

    Public Property TotalIntensityPercentageFilter As Single
        Get
            Return mTotalIntensityPercentageFilter
        End Get
        Set
            mTotalIntensityPercentageFilter = Value
        End Set
    End Property
#End Region

    Public Sub New()
        Me.New(INITIAL_MEMORY_RESERVE)
    End Sub

    Public Sub New(InitialCapacity As Integer)
        mSkipDataPointFlag = DEFAULT_SKIP_DATA_POINT_FLAG
        Me.Clear(InitialCapacity)
    End Sub

    Public Sub AddDataPoint(abundance As Single, dataPointIndex As Integer)

        If mDataCount >= mDataValues.Length Then
            ReDim Preserve mDataValues(CInt(Math.Floor(mDataValues.Length * 1.5)) - 1)
            ReDim Preserve mDataIndices(mDataValues.Length - 1)
        End If

        mDataValues(mDataCount) = abundance
        mDataIndices(mDataCount) = dataPointIndex

        mDataCount += 1
    End Sub

    Public Sub Clear(InitialCapacity As Integer)
        mMaximumDataCountToKeep = 400000

        mTotalIntensityPercentageFilterEnabled = False
        mTotalIntensityPercentageFilter = 90

        If InitialCapacity < 4 Then
            InitialCapacity = 4
        End If

        mDataCount = 0
        ReDim mDataValues(InitialCapacity - 1)
        ReDim mDataIndices(InitialCapacity - 1)
    End Sub

    Public Function GetAbundanceByIndex(dataPointIndex As Integer) As Single
        If dataPointIndex >= 0 And dataPointIndex < mDataCount Then
            Return mDataValues(dataPointIndex)
        Else
            ' Invalid data point index value
            Return -1
        End If
    End Function

    Public Sub FilterData()

        If mDataCount <= 0 Then
            ' Nothing to do
        Else
            '' Shrink the arrays to mDataCount
            If mDataCount < mDataValues.Length Then
                ReDim Preserve mDataValues(mDataCount - 1)
                ReDim Preserve mDataIndices(mDataCount - 1)
            End If

            FilterDataByMaxDataCountToKeep()

        End If

    End Sub

    Private Sub FilterDataByMaxDataCountToKeep()

        Const HISTOGRAM_BIN_COUNT = 5000

        Try

            Dim binToSortAbundances() As Single
            Dim binToSortDataIndices() As Integer

            ReDim binToSortAbundances(9)
            ReDim binToSortDataIndices(9)

            UpdateProgress(0)

            Dim useFullDataSort = False
            If mDataCount = 0 Then
                ' No data loaded
                UpdateProgress(4 / SUBTASK_STEP_COUNT * 100.0#)
                Exit Sub
            ElseIf mDataCount <= mMaximumDataCountToKeep Then
                ' Loaded less than mMaximumDataCountToKeep data points
                ' Nothing to filter
                UpdateProgress(4 / SUBTASK_STEP_COUNT * 100.0#)
                Exit Sub
            End If

            ' In order to speed up the sorting, we're first going to make a histogram
            '  (aka frequency distribution) of the abundances in mDataValues

            ' First, determine the maximum abundance value in mDataValues
            Dim maxAbundance = Single.MinValue
            For index = 0 To mDataCount - 1
                If mDataValues(index) > maxAbundance Then
                    maxAbundance = mDataValues(index)
                End If
            Next

            ' Round maxAbundance up to the next highest integer
            maxAbundance = CLng(Math.Ceiling(maxAbundance))

            ' Now determine the histogram bin size
            Dim binSize = maxAbundance / HISTOGRAM_BIN_COUNT
            If binSize < 1 Then binSize = 1

            ' Initialize histogramData
            Dim binCount = CInt(maxAbundance / binSize) + 1

            Dim histogramBinCounts() As Integer
            Dim histogramBinStartIntensity() As Double

            ReDim histogramBinCounts(binCount - 1)
            ReDim histogramBinStartIntensity(binCount - 1)

            For index = 0 To binCount - 1
                histogramBinStartIntensity(index) = index * binSize
            Next

            ' Parse mDataValues to populate histogramBinCounts
            For index = 0 To mDataCount - 1

                Dim targetBin As Integer
                If mDataValues(index) <= 0 Then
                    targetBin = 0
                Else
                    targetBin = CInt(Math.Floor(mDataValues(index) / binSize))
                End If

                If targetBin < binCount - 1 Then
                    If mDataValues(index) >= histogramBinStartIntensity(targetBin + 1) Then
                        targetBin += 1
                    End If
                End If

                histogramBinCounts(targetBin) += 1

                If mDataValues(index) < histogramBinStartIntensity(targetBin) Then
                    If mDataValues(index) < histogramBinStartIntensity(targetBin) - binSize / 1000 Then
                        ' This is unexpected
                        mDataValues(index) = mDataValues(index)
                    End If
                End If

                If index Mod 10000 = 0 Then
                    UpdateProgress(CSng((0 + (index + 1) / CDbl(mDataCount)) / SUBTASK_STEP_COUNT * 100.0#))
                End If
            Next

            ' Now examine the frequencies in histogramBinCounts() to determine the minimum value to consider when sorting
            Dim pointTotal = 0
            Dim binToSort = -1
            For index = binCount - 1 To 0 Step -1
                pointTotal = pointTotal + histogramBinCounts(index)
                If pointTotal >= mMaximumDataCountToKeep Then
                    binToSort = index
                    Exit For
                End If
            Next

            UpdateProgress(1 / SUBTASK_STEP_COUNT * 100.0#)

            If binToSort >= 0 Then
                ' Find the data with intensity >= histogramBinStartIntensity(binToSort)
                ' We actually only need to sort the data in bin binToSort

                Dim binToSortAbundanceMinimum As Double = histogramBinStartIntensity(binToSort)
                Dim binToSortAbundanceMaximum As Double = maxAbundance + 1

                If binToSort < binCount - 1 Then
                    binToSortAbundanceMaximum = histogramBinStartIntensity(binToSort + 1)
                End If

                If Math.Abs(binToSortAbundanceMaximum - binToSortAbundanceMinimum) < Single.Epsilon Then
                    ' Is this code ever reached?
                    ' If yes, then the code below won't populate binToSortAbundances() and binToSortDataIndices() with any data
                    useFullDataSort = True
                End If

                Dim binToSortDataCount = 0

                If Not useFullDataSort Then
                    If histogramBinCounts(binToSort) > 0 Then
                        ReDim binToSortAbundances(histogramBinCounts(binToSort) - 1)
                        ReDim binToSortDataIndices(histogramBinCounts(binToSort) - 1)
                    Else
                        ' Is this code ever reached?
                        useFullDataSort = True
                    End If
                End If

                If Not useFullDataSort Then
                    Dim dataCountImplicitlyIncluded = 0
                    For index = 0 To mDataCount - 1
                        If mDataValues(index) < binToSortAbundanceMinimum Then
                            ' Skip this data point when re-reading the input data file
                            mDataValues(index) = mSkipDataPointFlag
                        ElseIf mDataValues(index) < binToSortAbundanceMaximum Then
                            ' Value is in the bin to sort; add to the BinToSort arrays

                            If binToSortDataCount >= binToSortAbundances.Length Then
                                ' Need to reserve more space (this is unexpected)
                                ReDim Preserve binToSortAbundances(binToSortAbundances.Length * 2 - 1)
                                ReDim Preserve binToSortDataIndices(binToSortAbundances.Length - 1)
                            End If

                            binToSortAbundances(binToSortDataCount) = mDataValues(index)
                            binToSortDataIndices(binToSortDataCount) = mDataIndices(index)
                            binToSortDataCount += 1
                        Else
                            dataCountImplicitlyIncluded = dataCountImplicitlyIncluded + 1
                        End If

                        If index Mod 10000 = 0 Then
                            UpdateProgress(CSng((1 + (index + 1) / CDbl(mDataCount)) / SUBTASK_STEP_COUNT * 100.0#))
                        End If
                    Next

                    If binToSortDataCount > 0 Then
                        If binToSortDataCount < binToSortAbundances.Length Then
                            ReDim Preserve binToSortAbundances(binToSortDataCount - 1)
                            ReDim Preserve binToSortDataIndices(binToSortDataCount - 1)
                        End If
                    Else
                        ' This code shouldn't be reached
                    End If

                    If mMaximumDataCountToKeep - dataCountImplicitlyIncluded - binToSortDataCount = 0 Then
                        ' No need to sort and examine the data for BinToSort since we'll ultimately include all of it
                    Else
                        SortAndMarkPointsToSkip(binToSortAbundances, binToSortDataIndices, binToSortDataCount, mMaximumDataCountToKeep - dataCountImplicitlyIncluded, SUBTASK_STEP_COUNT)
                    End If

                    ' Synchronize the data in binToSortAbundances and binToSortDataIndices with mDataValues and mDataValues
                    ' mDataValues and mDataIndices have not been sorted and therefore mDataIndices should currently be sorted ascending on "valid data point index"
                    ' binToSortDataIndices should also currently be sorted ascending on "valid data point index" so the following Do Loop within a For Loop should sync things up

                    Dim originalDataArrayIndex = 0
                    For index = 0 To binToSortDataCount - 1
                        Do While binToSortDataIndices(index) > mDataIndices(originalDataArrayIndex)
                            originalDataArrayIndex += 1
                        Loop

                        If Math.Abs(binToSortAbundances(index) - mSkipDataPointFlag) < Single.Epsilon Then
                            If mDataIndices(originalDataArrayIndex) = binToSortDataIndices(index) Then
                                mDataValues(originalDataArrayIndex) = mSkipDataPointFlag
                            Else
                                ' This code shouldn't be reached
                            End If
                        End If
                        originalDataArrayIndex += 1

                        If binToSortDataCount < 1000 Or binToSortDataCount Mod 100 = 0 Then
                            UpdateProgress(CSng((3 + (index + 1) / CDbl(binToSortDataCount)) / SUBTASK_STEP_COUNT * 100.0#))
                        End If
                    Next
                End If
            Else
                useFullDataSort = True
            End If

            If useFullDataSort Then
                ' This shouldn't normally be necessary

                ' We have to sort all of the data; this can be quite slow
                SortAndMarkPointsToSkip(mDataValues, mDataIndices, mDataCount, mMaximumDataCountToKeep, SUBTASK_STEP_COUNT)
            End If

            UpdateProgress(4 / SUBTASK_STEP_COUNT * 100.0#)

            Exit Sub

        Catch ex As Exception
            Throw New Exception("Error in FilterDataByMaxDataCountToKeep: " & ex.Message, ex)
        End Try

    End Sub

    ' This is sub uses a full sort to filter the data
    ' This will be slow for large arrays and you should therefore use FilterDataByMaxDataCountToKeep if possible
    Private Sub SortAndMarkPointsToSkip(ByRef abundances() As Single, ByRef dataIndices() As Integer, dataCount As Integer, maximumDataCountInArraysToLoad As Integer, subtaskStepCount As Integer)

        Dim index As Integer

        If dataCount > 0 Then
            ' Sort abundances ascending, sorting dataIndices in parallel
            Array.Sort(abundances, dataIndices, 0, dataCount)

            UpdateProgress(CSng((2.333 / subtaskStepCount) * 100.0))

            ' Change the abundance values to mSkipDataPointFlag for data up to index dataCount-maximumDataCountInArraysToLoad-1
            For index = 0 To dataCount - maximumDataCountInArraysToLoad - 1
                abundances(index) = mSkipDataPointFlag
            Next

            UpdateProgress(CSng((2.666 / subtaskStepCount) * 100.0#))

            ' Re-sort, this time on dataIndices with abundances in parallel
            Array.Sort(dataIndices, abundances, 0, dataCount)

        End If

        UpdateProgress(CSng(3 / subtaskStepCount * 100.0#))

    End Sub

    Private Sub UpdateProgress(progressValue As Single)
        mProgress = progressValue

        RaiseEvent ProgressChanged(mProgress)
    End Sub
End Class


