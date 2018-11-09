Option Strict On

''' <summary>
''' This class can be used to search a list of values for a given value, plus or minus a given tolerance
''' The input list need not be sorted, since mPointerIndices() will be populated when the data is loaded,
''' after which the data array will be sorted
'''
''' To prevent this behavior, and save memory by not populating mPointerIndices, set mUsePointerIndexArray = False
''' </summary>
Public Class clsSearchRange

    Public Sub New()
        InitializeLocalVariables()
    End Sub

#Region "Constants and Enums"
    Private Enum eDataTypeToUse
        NoDataPresent = 0
        IntegerType = 1
        SingleType = 2
        DoubleType = 3
    End Enum
#End Region

#Region "Classwide Variables"
    Private mDataType As eDataTypeToUse

    Private mDataInt() As Integer
    Private mDataSingle() As Single
    Private mDataDouble() As Double

    Private mPointerIndices() As Integer        ' Pointers to the original index of the data point in the source array

    Private mPointerArrayIsValid As Boolean
    Private mUsePointerIndexArray As Boolean    ' Set this to false to conserve memory usage

#End Region

#Region "Interface Functions"
    Public ReadOnly Property DataCount As Integer
        Get
            Select Case mDataType
                Case eDataTypeToUse.IntegerType
                    Return mDataInt.Length
                Case eDataTypeToUse.SingleType
                    Return mDataSingle.Length
                Case eDataTypeToUse.DoubleType
                    Return mDataDouble.Length
                Case eDataTypeToUse.NoDataPresent
                    Return 0
                Case Else
                    Throw New Exception("Unknown data type encountered: " & mDataType.ToString())
            End Select
        End Get
    End Property

    Public ReadOnly Property OriginalIndex(index As Integer) As Integer
        Get
            If mPointerArrayIsValid Then
                Try
                    If index < mPointerIndices.Length Then
                        Return mPointerIndices(index)
                    Else
                        Return -1
                    End If
                Catch ex As Exception
                    Return -1
                End Try
            Else
                Return -1
            End If
        End Get
    End Property

    Public Property UsePointerIndexArray As Boolean
        Get
            Return mUsePointerIndexArray
        End Get
        Set
            mUsePointerIndexArray = Value
        End Set
    End Property
#End Region

#Region "Binary Search Range"

    Private Sub BinarySearchRangeInt(searchValue As Integer, toleranceHalfWidth As Integer, ByRef matchIndexStart As Integer, ByRef matchIndexEnd As Integer)
        ' Recursive search function

        Dim indexMidpoint As Integer
        Dim leftDone As Boolean
        Dim rightDone As Boolean
        Dim leftIndex As Integer
        Dim rightIndex As Integer

        indexMidpoint = (matchIndexStart + matchIndexEnd) \ 2
        If indexMidpoint = matchIndexStart Then
            ' Min and Max are next to each other
            If Math.Abs(searchValue - mDataInt(matchIndexStart)) > toleranceHalfWidth Then matchIndexStart = matchIndexEnd
            If Math.Abs(searchValue - mDataInt(matchIndexEnd)) > toleranceHalfWidth Then matchIndexEnd = indexMidpoint
            Exit Sub
        End If

        If mDataInt(indexMidpoint) > searchValue + toleranceHalfWidth Then
            ' Out of range on the right
            matchIndexEnd = indexMidpoint
            BinarySearchRangeInt(searchValue, toleranceHalfWidth, matchIndexStart, matchIndexEnd)
        ElseIf mDataInt(indexMidpoint) < searchValue - toleranceHalfWidth Then
            ' Out of range on the left
            matchIndexStart = indexMidpoint
            BinarySearchRangeInt(searchValue, toleranceHalfWidth, matchIndexStart, matchIndexEnd)
        Else
            ' Inside range; figure out the borders
            leftIndex = indexMidpoint
            Do
                leftIndex = leftIndex - 1
                If leftIndex < matchIndexStart Then
                    leftDone = True
                Else
                    If Math.Abs(searchValue - mDataInt(leftIndex)) > toleranceHalfWidth Then leftDone = True
                End If
            Loop While Not leftDone
            rightIndex = indexMidpoint

            Do
                rightIndex = rightIndex + 1
                If rightIndex > matchIndexEnd Then
                    rightDone = True
                Else
                    If Math.Abs(searchValue - mDataInt(rightIndex)) > toleranceHalfWidth Then rightDone = True
                End If
            Loop While Not rightDone

            matchIndexStart = leftIndex + 1
            matchIndexEnd = rightIndex - 1
        End If

    End Sub

    Private Sub BinarySearchRangeSng(searchValue As Single, toleranceHalfWidth As Single, ByRef matchIndexStart As Integer, ByRef matchIndexEnd As Integer)
        ' Recursive search function

        Dim indexMidpoint As Integer
        Dim leftDone As Boolean
        Dim rightDone As Boolean
        Dim leftIndex As Integer
        Dim rightIndex As Integer

        indexMidpoint = (matchIndexStart + matchIndexEnd) \ 2
        If indexMidpoint = matchIndexStart Then
            ' Min and Max are next to each other
            If Math.Abs(searchValue - mDataSingle(matchIndexStart)) > toleranceHalfWidth Then matchIndexStart = matchIndexEnd
            If Math.Abs(searchValue - mDataSingle(matchIndexEnd)) > toleranceHalfWidth Then matchIndexEnd = indexMidpoint
            Exit Sub
        End If

        If mDataSingle(indexMidpoint) > searchValue + toleranceHalfWidth Then
            ' Out of range on the right
            matchIndexEnd = indexMidpoint
            BinarySearchRangeSng(searchValue, toleranceHalfWidth, matchIndexStart, matchIndexEnd)
        ElseIf mDataSingle(indexMidpoint) < searchValue - toleranceHalfWidth Then
            ' Out of range on the left
            matchIndexStart = indexMidpoint
            BinarySearchRangeSng(searchValue, toleranceHalfWidth, matchIndexStart, matchIndexEnd)
        Else
            ' Inside range; figure out the borders
            leftIndex = indexMidpoint
            Do
                leftIndex = leftIndex - 1
                If leftIndex < matchIndexStart Then
                    leftDone = True
                Else
                    If Math.Abs(searchValue - mDataSingle(leftIndex)) > toleranceHalfWidth Then leftDone = True
                End If
            Loop While Not leftDone
            rightIndex = indexMidpoint

            Do
                rightIndex = rightIndex + 1
                If rightIndex > matchIndexEnd Then
                    rightDone = True
                Else
                    If Math.Abs(searchValue - mDataSingle(rightIndex)) > toleranceHalfWidth Then rightDone = True
                End If
            Loop While Not rightDone

            matchIndexStart = leftIndex + 1
            matchIndexEnd = rightIndex - 1
        End If

    End Sub

    Private Sub BinarySearchRangeDbl(searchValue As Double, toleranceHalfWidth As Double, ByRef matchIndexStart As Integer, ByRef matchIndexEnd As Integer)
        ' Recursive search function

        Dim indexMidpoint As Integer
        Dim leftDone As Boolean
        Dim rightDone As Boolean
        Dim leftIndex As Integer
        Dim rightIndex As Integer

        indexMidpoint = (matchIndexStart + matchIndexEnd) \ 2
        If indexMidpoint = matchIndexStart Then
            ' Min and Max are next to each other
            If Math.Abs(searchValue - mDataDouble(matchIndexStart)) > toleranceHalfWidth Then matchIndexStart = matchIndexEnd
            If Math.Abs(searchValue - mDataDouble(matchIndexEnd)) > toleranceHalfWidth Then matchIndexEnd = indexMidpoint
            Exit Sub
        End If

        If mDataDouble(indexMidpoint) > searchValue + toleranceHalfWidth Then
            ' Out of range on the right
            matchIndexEnd = indexMidpoint
            BinarySearchRangeDbl(searchValue, toleranceHalfWidth, matchIndexStart, matchIndexEnd)
        ElseIf mDataDouble(indexMidpoint) < searchValue - toleranceHalfWidth Then
            ' Out of range on the left
            matchIndexStart = indexMidpoint
            BinarySearchRangeDbl(searchValue, toleranceHalfWidth, matchIndexStart, matchIndexEnd)
        Else
            ' Inside range; figure out the borders
            leftIndex = indexMidpoint
            Do
                leftIndex = leftIndex - 1
                If leftIndex < matchIndexStart Then
                    leftDone = True
                Else
                    If Math.Abs(searchValue - mDataDouble(leftIndex)) > toleranceHalfWidth Then leftDone = True
                End If
            Loop While Not leftDone
            rightIndex = indexMidpoint

            Do
                rightIndex = rightIndex + 1
                If rightIndex > matchIndexEnd Then
                    rightDone = True
                Else
                    If Math.Abs(searchValue - mDataDouble(rightIndex)) > toleranceHalfWidth Then rightDone = True
                End If
            Loop While Not rightDone

            matchIndexStart = leftIndex + 1
            matchIndexEnd = rightIndex - 1
        End If

    End Sub
#End Region

    Private Sub ClearUnusedData()
        If mDataType <> eDataTypeToUse.IntegerType Then ReDim mDataInt(-1)
        If mDataType <> eDataTypeToUse.SingleType Then ReDim mDataSingle(-1)
        If mDataType <> eDataTypeToUse.DoubleType Then ReDim mDataDouble(-1)

        If mDataType = eDataTypeToUse.NoDataPresent Then
            mPointerArrayIsValid = False
        End If
    End Sub

    Public Sub ClearData()
        mDataType = eDataTypeToUse.NoDataPresent
        ClearUnusedData()
    End Sub

#Region "Fill with Data"

    Public Function FillWithData(ByRef values() As Integer) As Boolean

        Dim success As Boolean

        Try
            If values Is Nothing OrElse values.Length = 0 Then
                success = False
            Else
                ReDim mDataInt(values.Length - 1)
                values.CopyTo(mDataInt, 0)

                If mUsePointerIndexArray Then
                    InitializePointerIndexArray(mDataInt.Length)
                    Array.Sort(mDataInt, mPointerIndices)
                Else
                    Array.Sort(mDataInt)
                    mPointerArrayIsValid = False
                End If

                mDataType = eDataTypeToUse.IntegerType
                success = True
            End If
        Catch ex As Exception
            success = False
        End Try

        If success Then ClearUnusedData()
        Return success
    End Function

    Public Function FillWithData(ByRef values() As Single) As Boolean

        Dim success As Boolean

        Try
            If values Is Nothing OrElse values.Length = 0 Then
                success = False
            Else
                ReDim mDataSingle(values.Length - 1)
                values.CopyTo(mDataSingle, 0)

                If mUsePointerIndexArray Then
                    InitializePointerIndexArray(mDataSingle.Length)
                    Array.Sort(mDataSingle, mPointerIndices)
                Else
                    Array.Sort(mDataSingle)
                    mPointerArrayIsValid = False
                End If

                mDataType = eDataTypeToUse.SingleType
                success = True
            End If
        Catch ex As Exception
            success = False
        End Try

        If success Then ClearUnusedData()
        Return success
    End Function

    Public Function FillWithData(ByRef values() As Double) As Boolean

        Dim success As Boolean

        Try
            If values Is Nothing OrElse values.Length = 0 Then
                success = False
            Else
                ReDim mDataDouble(values.Length - 1)
                values.CopyTo(mDataDouble, 0)

                If mUsePointerIndexArray Then
                    InitializePointerIndexArray(mDataDouble.Length)
                    Array.Sort(mDataDouble, mPointerIndices)
                Else
                    Array.Sort(mDataDouble)
                    mPointerArrayIsValid = False
                End If

                mDataType = eDataTypeToUse.DoubleType
                success = True
            End If
        Catch ex As Exception
            success = False
        End Try

        If success Then ClearUnusedData()
        Return success
    End Function
#End Region


#Region "Find Value Range"

    Public Function FindValueRange(searchValue As Integer, toleranceHalfWidth As Integer, Optional ByRef matchIndexStart As Integer = 0, Optional ByRef matchIndexEnd As Integer = 0) As Boolean
        ' Searches the loaded data for searchValue with a tolerance of +/-toleranceHalfWidth
        ' Returns True if a match is found; in addition, populates matchIndexStart and matchIndexEnd
        ' Otherwise, returns false

        Dim matchFound As Boolean

        If mDataType <> eDataTypeToUse.IntegerType Then
            Select Case mDataType
                Case eDataTypeToUse.SingleType
                    matchFound = FindValueRange(CSng(searchValue), CSng(toleranceHalfWidth), matchIndexStart, matchIndexEnd)
                Case eDataTypeToUse.DoubleType
                    matchFound = FindValueRange(CDbl(searchValue), CDbl(toleranceHalfWidth), matchIndexStart, matchIndexEnd)
                Case Else
                    matchFound = False
            End Select
        Else
            matchIndexStart = 0
            matchIndexEnd = mDataInt.Length - 1

            If mDataInt.Length = 0 Then
                matchIndexEnd = -1
            ElseIf mDataInt.Length = 1 Then
                If Math.Abs(searchValue - mDataInt(0)) > toleranceHalfWidth Then
                    ' Only one data point, and it is not within tolerance
                    matchIndexEnd = -1
                End If
            Else
                BinarySearchRangeInt(searchValue, toleranceHalfWidth, matchIndexStart, matchIndexEnd)
            End If

            If matchIndexStart > matchIndexEnd Then
                matchIndexStart = -1
                matchIndexEnd = -1
                matchFound = False
            Else
                matchFound = True
            End If
        End If

        Return matchFound
    End Function

    Public Function FindValueRange(searchValue As Double, toleranceHalfWidth As Double, Optional ByRef matchIndexStart As Integer = 0, Optional ByRef matchIndexEnd As Integer = 0) As Boolean
        ' Searches the loaded data for searchValue with a tolerance of +/-tolerance
        ' Returns True if a match is found; in addition, populates matchIndexStart and matchIndexEnd
        ' Otherwise, returns false

        Dim matchFound As Boolean

        If mDataType <> eDataTypeToUse.DoubleType Then
            Select Case mDataType
                Case eDataTypeToUse.IntegerType
                    matchFound = FindValueRange(CInt(searchValue), CInt(toleranceHalfWidth), matchIndexStart, matchIndexEnd)
                Case eDataTypeToUse.SingleType
                    matchFound = FindValueRange(CSng(searchValue), CSng(toleranceHalfWidth), matchIndexStart, matchIndexEnd)
                Case Else
                    matchFound = False
            End Select
        Else
            matchIndexStart = 0
            matchIndexEnd = mDataDouble.Length - 1

            If mDataDouble.Length = 0 Then
                matchIndexEnd = -1
            ElseIf mDataDouble.Length = 1 Then
                If Math.Abs(searchValue - mDataDouble(0)) > toleranceHalfWidth Then
                    ' Only one data point, and it is not within tolerance
                    matchIndexEnd = -1
                End If
            Else
                BinarySearchRangeDbl(searchValue, toleranceHalfWidth, matchIndexStart, matchIndexEnd)
            End If

            If matchIndexStart > matchIndexEnd Then
                matchIndexStart = -1
                matchIndexEnd = -1
                matchFound = False
            Else
                matchFound = True
            End If
        End If

        Return matchFound
    End Function

    Public Function FindValueRange(searchValue As Single, toleranceHalfWidth As Single, Optional ByRef matchIndexStart As Integer = 0, Optional ByRef matchIndexEnd As Integer = 0) As Boolean
        ' Searches the loaded data for searchValue with a tolerance of +/-tolerance
        ' Returns True if a match is found; in addition, populates matchIndexStart and matchIndexEnd
        ' Otherwise, returns false

        Dim matchFound As Boolean

        If mDataType <> eDataTypeToUse.SingleType Then
            Select Case mDataType
                Case eDataTypeToUse.IntegerType
                    matchFound = FindValueRange(CInt(searchValue), CInt(toleranceHalfWidth), matchIndexStart, matchIndexEnd)
                Case eDataTypeToUse.DoubleType
                    matchFound = FindValueRange(CDbl(searchValue), CDbl(toleranceHalfWidth), matchIndexStart, matchIndexEnd)
                Case Else
                    matchFound = False
            End Select
        Else
            matchIndexStart = 0
            matchIndexEnd = mDataSingle.Length - 1

            If mDataSingle.Length = 0 Then
                matchIndexEnd = -1
            ElseIf mDataSingle.Length = 1 Then
                If Math.Abs(searchValue - mDataSingle(0)) > toleranceHalfWidth Then
                    ' Only one data point, and it is not within tolerance
                    matchIndexEnd = -1
                End If
            Else
                BinarySearchRangeSng(searchValue, toleranceHalfWidth, matchIndexStart, matchIndexEnd)
            End If

            If matchIndexStart > matchIndexEnd Then
                matchIndexStart = -1
                matchIndexEnd = -1
                matchFound = False
            Else
                matchFound = True
            End If
        End If

        Return matchFound
    End Function
#End Region


#Region "Get Value by Index"
    Public Function GetValueByIndexInt(index As Integer) As Integer
        Try
            Return CInt(GetValueByIndex(index))
        Catch ex As Exception
            Return 0
        End Try
    End Function

    Public Function GetValueByIndex(index As Integer) As Double
        Try
            If mDataType = eDataTypeToUse.NoDataPresent Then
                Return 0
            Else
                Select Case mDataType
                    Case eDataTypeToUse.IntegerType
                        Return mDataInt(index)
                    Case eDataTypeToUse.SingleType
                        Return mDataSingle(index)
                    Case eDataTypeToUse.DoubleType
                        Return mDataDouble(index)
                End Select
            End If
        Catch ex As Exception
            ' index is probably out of range
            ' Ignore errors
        End Try

        Return 0

    End Function

    Public Function GetValueByIndexSng(index As Integer) As Single
        Try
            Return CSng(GetValueByIndex(index))
        Catch ex As Exception
            Return 0
        End Try
    End Function
#End Region


#Region "Get Value by Original Index"
    Public Function GetValueByOriginalIndexInt(index As Integer) As Integer
        Try
            Return CInt(GetValueByOriginalIndex(index))
        Catch ex As Exception
            Return 0
        End Try
    End Function

    Public Function GetValueByOriginalIndex(indexOriginal As Integer) As Double
        Dim index As Integer

        If Not mPointerArrayIsValid OrElse mDataType = eDataTypeToUse.NoDataPresent Then
            Return 0
        Else
            Try
                index = Array.IndexOf(mPointerIndices, indexOriginal)
                If index >= 0 Then
                    Select Case mDataType
                        Case eDataTypeToUse.IntegerType
                            Return mDataInt(mPointerIndices(index))
                        Case eDataTypeToUse.SingleType
                            Return mDataSingle(mPointerIndices(index))
                        Case eDataTypeToUse.DoubleType
                            Return mDataDouble(mPointerIndices(index))
                    End Select
                Else
                    Return 0
                End If
            Catch ex As Exception
                ' Ignore errors
            End Try
        End If

        Return 0
    End Function

    Public Function GetValueByOriginalIndexSng(index As Integer) As Single
        Try
            Return CSng(GetValueByOriginalIndex(index))
        Catch ex As Exception
            Return 0
        End Try
    End Function
#End Region

    Private Sub InitializeLocalVariables()
        mDataType = eDataTypeToUse.NoDataPresent
        ClearUnusedData()

        mUsePointerIndexArray = True
        InitializePointerIndexArray(0)

    End Sub

    Private Sub InitializePointerIndexArray(arrayLength As Integer)
        Dim index As Integer

        If arrayLength < 0 Then arrayLength = 0
        ReDim mPointerIndices(arrayLength - 1)

        For index = 0 To arrayLength - 1
            mPointerIndices(index) = index
        Next

        If arrayLength > 0 Then
            mPointerArrayIsValid = True
        Else
            mPointerArrayIsValid = False
        End If
    End Sub

End Class
