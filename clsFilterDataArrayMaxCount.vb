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

    Public Event ProgressChanged(ProgressVal As Single)

#Region "Properties"
    Public Property MaximumDataCountToLoad() As Integer
        Get
            Return mMaximumDataCountToKeep
        End Get
        Set(value As Integer)
            mMaximumDataCountToKeep = value
        End Set
    End Property

    Public ReadOnly Property Progress() As Single
        Get
            Return mProgress
        End Get
    End Property

    Public Property SkipDataPointFlag() As Single
        Get
            Return mSkipDataPointFlag
        End Get
        Set(value As Single)
            mSkipDataPointFlag = value
        End Set
    End Property

    Public Property TotalIntensityPercentageFilterEnabled() As Boolean
        Get
            Return mTotalIntensityPercentageFilterEnabled
        End Get
        Set(value As Boolean)
            mTotalIntensityPercentageFilterEnabled = value
        End Set
    End Property

    Public Property TotalIntensityPercentageFilter() As Single
        Get
            Return mTotalIntensityPercentageFilter
        End Get
        Set(value As Single)
            mTotalIntensityPercentageFilter = value
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

    Public Sub AddDataPoint(sngAbundance As Single, intDataPointIndex As Integer)

        If mDataCount >= mDataValues.Length Then
            ReDim Preserve mDataValues(CInt(Math.Floor(mDataValues.Length * 1.5)) - 1)
            ReDim Preserve mDataIndices(mDataValues.Length - 1)
        End If

        mDataValues(mDataCount) = sngAbundance
        mDataIndices(mDataCount) = intDataPointIndex

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

    Public Function GetAbundanceByIndex(intDataPointIndex As Integer) As Single
        If intDataPointIndex >= 0 And intDataPointIndex < mDataCount Then
            Return mDataValues(intDataPointIndex)
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

        Dim intIndex As Integer
        Dim intPointTotal As Integer
        Dim intBinCount As Integer
        Dim intTargetBin As Integer
        Dim intBinToSort As Integer
        Dim intOriginalDataArrayIndex As Integer

        Dim blnUseFullDataSort As Boolean

        Dim sngMaxAbundance As Single
        Dim dblBinSize As Double

        Dim intHistogramBinCounts() As Integer
        Dim dblHistogramBinStartIntensity() As Double

        Dim dblBinToSortAbundanceMinimum As Double
        Dim dblBinToSortAbundanceMaximum As Double

        Dim sngBinToSortAbundances() As Single
        Dim intBinToSortDataIndices() As Integer
        Dim intBinToSortDataCount As Integer
        Dim intDataCountImplicitlyIncluded As Integer

        Try

            ReDim sngBinToSortAbundances(9)
            ReDim intBinToSortDataIndices(9)

            UpdateProgress(0)

            blnUseFullDataSort = False
            If mDataCount = 0 Then
                ' No data loaded
            ElseIf mDataCount <= mMaximumDataCountToKeep Then
                ' Loaded less than mMaximumDataCountToKeep data points
                ' Nothing to filter
            Else

                ' In order to speed up the sorting, we're first going to make a histogram
                '  (aka frequency distribution) of the abundances in mDataValues

                ' First, determine the maximum abundance value in mDataValues
                sngMaxAbundance = Single.MinValue
                For intIndex = 0 To mDataCount - 1
                    If mDataValues(intIndex) > sngMaxAbundance Then
                        sngMaxAbundance = mDataValues(intIndex)
                    End If
                Next intIndex

                ' Round sngMaxAbundance up to the next highest integer
                sngMaxAbundance = CLng(Math.Ceiling(sngMaxAbundance))

                ' Now determine the histogram bin size
                dblBinSize = sngMaxAbundance / HISTOGRAM_BIN_COUNT
                If dblBinSize < 1 Then dblBinSize = 1

                ' Initialize intHistogramData
                intBinCount = CInt(sngMaxAbundance / dblBinSize) + 1
                ReDim intHistogramBinCounts(intBinCount - 1)
                ReDim dblHistogramBinStartIntensity(intBinCount - 1)

                For intIndex = 0 To intBinCount - 1
                    dblHistogramBinStartIntensity(intIndex) = intIndex * dblBinSize
                Next intIndex

                ' Parse mDataValues to populate intHistogramBinCounts
                For intIndex = 0 To mDataCount - 1
                    If mDataValues(intIndex) <= 0 Then
                        intTargetBin = 0
                    Else
                        intTargetBin = CInt(Math.Floor(mDataValues(intIndex) / dblBinSize))
                    End If

                    If intTargetBin < intBinCount - 1 Then
                        If mDataValues(intIndex) >= dblHistogramBinStartIntensity(intTargetBin + 1) Then
                            intTargetBin += 1
                        End If
                    End If

                    intHistogramBinCounts(intTargetBin) += 1

                    If mDataValues(intIndex) < dblHistogramBinStartIntensity(intTargetBin) Then
                        If mDataValues(intIndex) < dblHistogramBinStartIntensity(intTargetBin) - dblBinSize / 1000 Then
                            ' This is unexpected
                            mDataValues(intIndex) = mDataValues(intIndex)
                        End If
                    End If

                    If intIndex Mod 10000 = 0 Then
                        UpdateProgress(CSng((0 + (intIndex + 1) / CDbl(mDataCount)) / SUBTASK_STEP_COUNT * 100.0#))
                    End If
                Next intIndex

                ' Now examine the frequencies in intHistogramBinCounts() to determine the minimum value to consider when sorting
                intPointTotal = 0
                intBinToSort = -1
                For intIndex = intBinCount - 1 To 0 Step -1
                    intPointTotal = intPointTotal + intHistogramBinCounts(intIndex)
                    If intPointTotal >= mMaximumDataCountToKeep Then
                        intBinToSort = intIndex
                        Exit For
                    End If
                Next intIndex

                UpdateProgress(1 / SUBTASK_STEP_COUNT * 100.0#)

                If intBinToSort >= 0 Then
                    ' Find the data with intensity >= dblHistogramBinStartIntensity(intBinToSort)
                    ' We actually only need to sort the data in bin intBinToSort

                    dblBinToSortAbundanceMinimum = dblHistogramBinStartIntensity(intBinToSort)
                    dblBinToSortAbundanceMaximum = sngMaxAbundance + 1
                    If intBinToSort < intBinCount - 1 Then
                        dblBinToSortAbundanceMaximum = dblHistogramBinStartIntensity(intBinToSort + 1)
                    End If

                    If dblBinToSortAbundanceMaximum = dblBinToSortAbundanceMinimum Then
                        ' Is this code ever reached?
                        ' If yes, then the code below won't populate sngBinToSortAbundances() and intBinToSortDataIndices() with any data
                        blnUseFullDataSort = True
                    End If

                    If Not blnUseFullDataSort Then
                        intBinToSortDataCount = 0
                        If intHistogramBinCounts(intBinToSort) > 0 Then
                            ReDim sngBinToSortAbundances(intHistogramBinCounts(intBinToSort) - 1)
                            ReDim intBinToSortDataIndices(intHistogramBinCounts(intBinToSort) - 1)
                        Else
                            ' Is this code ever reached?
                            blnUseFullDataSort = True
                        End If
                    End If

                    If Not blnUseFullDataSort Then
                        intDataCountImplicitlyIncluded = 0
                        For intIndex = 0 To mDataCount - 1
                            If mDataValues(intIndex) < dblBinToSortAbundanceMinimum Then
                                ' Skip this data point when re-reading the input data file
                                mDataValues(intIndex) = mSkipDataPointFlag
                            ElseIf mDataValues(intIndex) < dblBinToSortAbundanceMaximum Then
                                ' Value is in the bin to sort; add to the BinToSort arrays

                                If intBinToSortDataCount >= sngBinToSortAbundances.Length Then
                                    ' Need to reserve more space (this is unexpected)
                                    ReDim Preserve sngBinToSortAbundances(sngBinToSortAbundances.Length * 2 - 1)
                                    ReDim Preserve intBinToSortDataIndices(sngBinToSortAbundances.Length - 1)
                                End If

                                sngBinToSortAbundances(intBinToSortDataCount) = mDataValues(intIndex)
                                intBinToSortDataIndices(intBinToSortDataCount) = mDataIndices(intIndex)
                                intBinToSortDataCount += 1
                            Else
                                intDataCountImplicitlyIncluded = intDataCountImplicitlyIncluded + 1
                            End If

                            If intIndex Mod 10000 = 0 Then
                                UpdateProgress(CSng((1 + (intIndex + 1) / CDbl(mDataCount)) / SUBTASK_STEP_COUNT * 100.0#))
                            End If
                        Next intIndex

                        If intBinToSortDataCount > 0 Then
                            If intBinToSortDataCount < sngBinToSortAbundances.Length Then
                                ReDim Preserve sngBinToSortAbundances(intBinToSortDataCount - 1)
                                ReDim Preserve intBinToSortDataIndices(intBinToSortDataCount - 1)
                            End If
                        Else
                            ' This code shouldn't be reached
                        End If

                        If mMaximumDataCountToKeep - intDataCountImplicitlyIncluded - intBinToSortDataCount = 0 Then
                            ' No need to sort and examine the data for BinToSort since we'll ultimately include all of it
                        Else
                            SortAndMarkPointsToSkip(sngBinToSortAbundances, intBinToSortDataIndices, intBinToSortDataCount, mMaximumDataCountToKeep - intDataCountImplicitlyIncluded, SUBTASK_STEP_COUNT)
                        End If

                        ' Synchronize the data in sngBinToSortAbundances and intBinToSortDataIndices with mDataValues and mDataValues
                        ' mDataValues and mDataIndices have not been sorted and therefore mDataIndices should currently be sorted ascending on "valid data point index"
                        ' intBinToSortDataIndices should also currently be sorted ascending on "valid data point index" so the following Do Loop within a For Loop should sync things up

                        intOriginalDataArrayIndex = 0
                        For intIndex = 0 To intBinToSortDataCount - 1
                            Do While intBinToSortDataIndices(intIndex) > mDataIndices(intOriginalDataArrayIndex)
                                intOriginalDataArrayIndex += 1
                            Loop

                            If sngBinToSortAbundances(intIndex) = mSkipDataPointFlag Then
                                If mDataIndices(intOriginalDataArrayIndex) = intBinToSortDataIndices(intIndex) Then
                                    mDataValues(intOriginalDataArrayIndex) = mSkipDataPointFlag
                                Else
                                    ' This code shouldn't be reached
                                End If
                            End If
                            intOriginalDataArrayIndex += 1

                            If intBinToSortDataCount < 1000 Or intBinToSortDataCount Mod 100 = 0 Then
                                UpdateProgress(CSng((3 + (intIndex + 1) / CDbl(intBinToSortDataCount)) / SUBTASK_STEP_COUNT * 100.0#))
                            End If
                        Next intIndex
                    End If
                Else
                    blnUseFullDataSort = True
                End If

                If blnUseFullDataSort Then
                    ' This shouldn't normally be necessary

                    ' We have to sort all of the data; this can be quite slow
                    SortAndMarkPointsToSkip(mDataValues, mDataIndices, mDataCount, mMaximumDataCountToKeep, SUBTASK_STEP_COUNT)
                End If

            End If

            UpdateProgress(4 / SUBTASK_STEP_COUNT * 100.0#)

            Exit Sub

        Catch ex As System.Exception
            Throw New System.Exception("Error in FilterDataByMaxDataCountToKeep: " & ex.Message, ex)
        End Try

    End Sub

    ' This is sub uses a full sort to filter the data
    ' This will be slow for large arrays and you should therefore use FilterDataByMaxDataCountToKeep if possible
    Private Sub SortAndMarkPointsToSkip(ByRef sngAbundances() As Single, ByRef intDataIndices() As Integer, intDataCount As Integer, intMaximumDataCountInArraysToLoad As Integer, intSubtaskStepCount As Integer)

        Dim intIndex As Integer

        If intDataCount > 0 Then
            ' Sort sngAbundances ascending, sorting intDataIndices in parallel
            Array.Sort(sngAbundances, intDataIndices, 0, intDataCount)

            UpdateProgress(CSng((2.333 / intSubtaskStepCount) * 100.0))

            ' Change the abundance values to mSkipDataPointFlag for data up to index intDataCount-intMaximumDataCountInArraysToLoad-1
            For intIndex = 0 To intDataCount - intMaximumDataCountInArraysToLoad - 1
                sngAbundances(intIndex) = mSkipDataPointFlag
            Next intIndex

            UpdateProgress(CSng((2.666 / intSubtaskStepCount) * 100.0#))

            ' Re-sort, this time on intDataIndices with sngAbundances in parallel
            Array.Sort(intDataIndices, sngAbundances, 0, intDataCount)

        End If

        UpdateProgress(CSng(3 / intSubtaskStepCount * 100.0#))

    End Sub

    Private Sub UpdateProgress(sngProgress As Single)
        mProgress = sngProgress

        RaiseEvent ProgressChanged(mProgress)
    End Sub
End Class


