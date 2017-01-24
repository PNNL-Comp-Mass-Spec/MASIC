Option Strict On

''' <summary>
''' This class can be used to search a list of values for a given value, plus or minus a given tolerance
''' The input list need not be sorted, since mPointerIndices() will be populated when the data is loaded,
''' afterwhich the data array will be sorted
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
    Public ReadOnly Property DataCount() As Integer
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

    Public ReadOnly Property OriginalIndex(intIndex As Integer) As Integer
        Get
            If mPointerArrayIsValid Then
                Try
                    If intIndex < mPointerIndices.Length Then
                        Return mPointerIndices(intIndex)
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

    Public Property UsePointerIndexArray() As Boolean
        Get
            Return mUsePointerIndexArray
        End Get
        Set(Value As Boolean)
            mUsePointerIndexArray = Value
        End Set
    End Property
#End Region

#Region "Binary Search Range"

    Private Sub BinarySearchRangeInt(intSearchValue As Integer, intToleranceHalfWidth As Integer, ByRef intMatchIndexStart As Integer, ByRef intMatchIndexEnd As Integer)
        ' Recursive search function

        Dim intIndexMidpoint As Integer
        Dim blnLeftDone As Boolean
        Dim blnRightDone As Boolean
        Dim intLeftIndex As Integer
        Dim intRightIndex As Integer

        intIndexMidpoint = (intMatchIndexStart + intMatchIndexEnd) \ 2
        If intIndexMidpoint = intMatchIndexStart Then
            ' Min and Max are next to each other
            If Math.Abs(intSearchValue - mDataInt(intMatchIndexStart)) > intToleranceHalfWidth Then intMatchIndexStart = intMatchIndexEnd
            If Math.Abs(intSearchValue - mDataInt(intMatchIndexEnd)) > intToleranceHalfWidth Then intMatchIndexEnd = intIndexMidpoint
            Exit Sub
        End If

        If mDataInt(intIndexMidpoint) > intSearchValue + intToleranceHalfWidth Then
            ' Out of range on the right
            intMatchIndexEnd = intIndexMidpoint
            BinarySearchRangeInt(intSearchValue, intToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
        ElseIf mDataInt(intIndexMidpoint) < intSearchValue - intToleranceHalfWidth Then
            ' Out of range on the left
            intMatchIndexStart = intIndexMidpoint
            BinarySearchRangeInt(intSearchValue, intToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
        Else
            ' Inside range; figure out the borders
            intLeftIndex = intIndexMidpoint
            Do
                intLeftIndex = intLeftIndex - 1
                If intLeftIndex < intMatchIndexStart Then
                    blnLeftDone = True
                Else
                    If Math.Abs(intSearchValue - mDataInt(intLeftIndex)) > intToleranceHalfWidth Then blnLeftDone = True
                End If
            Loop While Not blnLeftDone
            intRightIndex = intIndexMidpoint

            Do
                intRightIndex = intRightIndex + 1
                If intRightIndex > intMatchIndexEnd Then
                    blnRightDone = True
                Else
                    If Math.Abs(intSearchValue - mDataInt(intRightIndex)) > intToleranceHalfWidth Then blnRightDone = True
                End If
            Loop While Not blnRightDone

            intMatchIndexStart = intLeftIndex + 1
            intMatchIndexEnd = intRightIndex - 1
        End If

    End Sub

    Private Sub BinarySearchRangeSng(sngSearchValue As Single, sngToleranceHalfWidth As Single, ByRef intMatchIndexStart As Integer, ByRef intMatchIndexEnd As Integer)
        ' Recursive search function

        Dim intIndexMidpoint As Integer
        Dim blnLeftDone As Boolean
        Dim blnRightDone As Boolean
        Dim intLeftIndex As Integer
        Dim intRightIndex As Integer

        intIndexMidpoint = (intMatchIndexStart + intMatchIndexEnd) \ 2
        If intIndexMidpoint = intMatchIndexStart Then
            ' Min and Max are next to each other
            If Math.Abs(sngSearchValue - mDataSingle(intMatchIndexStart)) > sngToleranceHalfWidth Then intMatchIndexStart = intMatchIndexEnd
            If Math.Abs(sngSearchValue - mDataSingle(intMatchIndexEnd)) > sngToleranceHalfWidth Then intMatchIndexEnd = intIndexMidpoint
            Exit Sub
        End If

        If mDataSingle(intIndexMidpoint) > sngSearchValue + sngToleranceHalfWidth Then
            ' Out of range on the right
            intMatchIndexEnd = intIndexMidpoint
            BinarySearchRangeSng(sngSearchValue, sngToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
        ElseIf mDataSingle(intIndexMidpoint) < sngSearchValue - sngToleranceHalfWidth Then
            ' Out of range on the left
            intMatchIndexStart = intIndexMidpoint
            BinarySearchRangeSng(sngSearchValue, sngToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
        Else
            ' Inside range; figure out the borders
            intLeftIndex = intIndexMidpoint
            Do
                intLeftIndex = intLeftIndex - 1
                If intLeftIndex < intMatchIndexStart Then
                    blnLeftDone = True
                Else
                    If Math.Abs(sngSearchValue - mDataSingle(intLeftIndex)) > sngToleranceHalfWidth Then blnLeftDone = True
                End If
            Loop While Not blnLeftDone
            intRightIndex = intIndexMidpoint

            Do
                intRightIndex = intRightIndex + 1
                If intRightIndex > intMatchIndexEnd Then
                    blnRightDone = True
                Else
                    If Math.Abs(sngSearchValue - mDataSingle(intRightIndex)) > sngToleranceHalfWidth Then blnRightDone = True
                End If
            Loop While Not blnRightDone

            intMatchIndexStart = intLeftIndex + 1
            intMatchIndexEnd = intRightIndex - 1
        End If

    End Sub

    Private Sub BinarySearchRangeDbl(dblSearchValue As Double, dblToleranceHalfWidth As Double, ByRef intMatchIndexStart As Integer, ByRef intMatchIndexEnd As Integer)
        ' Recursive search function

        Dim intIndexMidpoint As Integer
        Dim blnLeftDone As Boolean
        Dim blnRightDone As Boolean
        Dim intLeftIndex As Integer
        Dim intRightIndex As Integer

        intIndexMidpoint = (intMatchIndexStart + intMatchIndexEnd) \ 2
        If intIndexMidpoint = intMatchIndexStart Then
            ' Min and Max are next to each other
            If Math.Abs(dblSearchValue - mDataDouble(intMatchIndexStart)) > dblToleranceHalfWidth Then intMatchIndexStart = intMatchIndexEnd
            If Math.Abs(dblSearchValue - mDataDouble(intMatchIndexEnd)) > dblToleranceHalfWidth Then intMatchIndexEnd = intIndexMidpoint
            Exit Sub
        End If

        If mDataDouble(intIndexMidpoint) > dblSearchValue + dblToleranceHalfWidth Then
            ' Out of range on the right
            intMatchIndexEnd = intIndexMidpoint
            BinarySearchRangeDbl(dblSearchValue, dblToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
        ElseIf mDataDouble(intIndexMidpoint) < dblSearchValue - dblToleranceHalfWidth Then
            ' Out of range on the left
            intMatchIndexStart = intIndexMidpoint
            BinarySearchRangeDbl(dblSearchValue, dblToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
        Else
            ' Inside range; figure out the borders
            intLeftIndex = intIndexMidpoint
            Do
                intLeftIndex = intLeftIndex - 1
                If intLeftIndex < intMatchIndexStart Then
                    blnLeftDone = True
                Else
                    If Math.Abs(dblSearchValue - mDataDouble(intLeftIndex)) > dblToleranceHalfWidth Then blnLeftDone = True
                End If
            Loop While Not blnLeftDone
            intRightIndex = intIndexMidpoint

            Do
                intRightIndex = intRightIndex + 1
                If intRightIndex > intMatchIndexEnd Then
                    blnRightDone = True
                Else
                    If Math.Abs(dblSearchValue - mDataDouble(intRightIndex)) > dblToleranceHalfWidth Then blnRightDone = True
                End If
            Loop While Not blnRightDone

            intMatchIndexStart = intLeftIndex + 1
            intMatchIndexEnd = intRightIndex - 1
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

    Public Function FillWithData(ByRef intValues() As Integer) As Boolean

        Dim blnSuccess As Boolean

        Try
            If intValues Is Nothing OrElse intValues.Length = 0 Then
                blnSuccess = False
            Else
                ReDim mDataInt(intValues.Length - 1)
                intValues.CopyTo(mDataInt, 0)

                If mUsePointerIndexArray Then
                    InitializePointerIndexArray(mDataInt.Length)
                    Array.Sort(mDataInt, mPointerIndices)
                Else
                    Array.Sort(mDataInt)
                    mPointerArrayIsValid = False
                End If

                mDataType = eDataTypeToUse.IntegerType
                blnSuccess = True
            End If
        Catch ex As Exception
            blnSuccess = False
        End Try

        If blnSuccess Then ClearUnusedData()
        Return blnSuccess
    End Function

    Public Function FillWithData(ByRef sngValues() As Single) As Boolean

        Dim blnSuccess As Boolean

        Try
            If sngValues Is Nothing OrElse sngValues.Length = 0 Then
                blnSuccess = False
            Else
                ReDim mDataSingle(sngValues.Length - 1)
                sngValues.CopyTo(mDataSingle, 0)

                If mUsePointerIndexArray Then
                    InitializePointerIndexArray(mDataSingle.Length)
                    Array.Sort(mDataSingle, mPointerIndices)
                Else
                    Array.Sort(mDataSingle)
                    mPointerArrayIsValid = False
                End If

                mDataType = eDataTypeToUse.SingleType
                blnSuccess = True
            End If
        Catch ex As Exception
            blnSuccess = False
        End Try

        If blnSuccess Then ClearUnusedData()
        Return blnSuccess
    End Function

    Public Function FillWithData(ByRef dblValues() As Double) As Boolean

        Dim blnSuccess As Boolean

        Try
            If dblValues Is Nothing OrElse dblValues.Length = 0 Then
                blnSuccess = False
            Else
                ReDim mDataDouble(dblValues.Length - 1)
                dblValues.CopyTo(mDataDouble, 0)

                If mUsePointerIndexArray Then
                    InitializePointerIndexArray(mDataDouble.Length)
                    Array.Sort(mDataDouble, mPointerIndices)
                Else
                    Array.Sort(mDataDouble)
                    mPointerArrayIsValid = False
                End If

                mDataType = eDataTypeToUse.DoubleType
                blnSuccess = True
            End If
        Catch ex As Exception
            blnSuccess = False
        End Try

        If blnSuccess Then ClearUnusedData()
        Return blnSuccess
    End Function
#End Region


#Region "Find Value Range"

    Public Function FindValueRange(intSearchValue As Integer, intToleranceHalfWidth As Integer, Optional ByRef intMatchIndexStart As Integer = 0, Optional ByRef intMatchIndexEnd As Integer = 0) As Boolean
        ' Searches the loaded data for intSearchValue with a tolerance of +/-intToleranceHalfWidth
        ' Returns True if a match is found; in addition, populates intMatchIndexStart and intMatchIndexEnd
        ' Otherwise, returns false

        Dim blnMatchFound As Boolean

        If mDataType <> eDataTypeToUse.IntegerType Then
            Select Case mDataType
                Case eDataTypeToUse.SingleType
                    blnMatchFound = FindValueRange(CSng(intSearchValue), CSng(intToleranceHalfWidth), intMatchIndexStart, intMatchIndexEnd)
                Case eDataTypeToUse.DoubleType
                    blnMatchFound = FindValueRange(CDbl(intSearchValue), CDbl(intToleranceHalfWidth), intMatchIndexStart, intMatchIndexEnd)
                Case Else
                    blnMatchFound = False
            End Select
        Else
            intMatchIndexStart = 0
            intMatchIndexEnd = mDataInt.Length - 1

            If mDataInt.Length = 0 Then
                intMatchIndexEnd = -1
            ElseIf mDataInt.Length = 1 Then
                If Math.Abs(intSearchValue - mDataInt(0)) > intToleranceHalfWidth Then
                    ' Only one data point, and it is not within tolerance
                    intMatchIndexEnd = -1
                End If
            Else
                BinarySearchRangeInt(intSearchValue, intToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
            End If

            If intMatchIndexStart > intMatchIndexEnd Then
                intMatchIndexStart = -1
                intMatchIndexEnd = -1
                blnMatchFound = False
            Else
                blnMatchFound = True
            End If
        End If

        Return blnMatchFound
    End Function

    Public Function FindValueRange(dblSearchValue As Double, dblToleranceHalfWidth As Double, Optional ByRef intMatchIndexStart As Integer = 0, Optional ByRef intMatchIndexEnd As Integer = 0) As Boolean
        ' Searches the loaded data for dblSearchValue with a tolerance of +/-dblTolerance
        ' Returns True if a match is found; in addition, populates intMatchIndexStart and intMatchIndexEnd
        ' Otherwise, returns false

        Dim blnMatchFound As Boolean

        If mDataType <> eDataTypeToUse.DoubleType Then
            Select Case mDataType
                Case eDataTypeToUse.IntegerType
                    blnMatchFound = FindValueRange(CInt(dblSearchValue), CInt(dblToleranceHalfWidth), intMatchIndexStart, intMatchIndexEnd)
                Case eDataTypeToUse.SingleType
                    blnMatchFound = FindValueRange(CSng(dblSearchValue), CSng(dblToleranceHalfWidth), intMatchIndexStart, intMatchIndexEnd)
                Case Else
                    blnMatchFound = False
            End Select
        Else
            intMatchIndexStart = 0
            intMatchIndexEnd = mDataDouble.Length - 1

            If mDataDouble.Length = 0 Then
                intMatchIndexEnd = -1
            ElseIf mDataDouble.Length = 1 Then
                If Math.Abs(dblSearchValue - mDataDouble(0)) > dblToleranceHalfWidth Then
                    ' Only one data point, and it is not within tolerance
                    intMatchIndexEnd = -1
                End If
            Else
                BinarySearchRangeDbl(dblSearchValue, dblToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
            End If

            If intMatchIndexStart > intMatchIndexEnd Then
                intMatchIndexStart = -1
                intMatchIndexEnd = -1
                blnMatchFound = False
            Else
                blnMatchFound = True
            End If
        End If

        Return blnMatchFound
    End Function

    Public Function FindValueRange(sngSearchValue As Single, sngToleranceHalfWidth As Single, Optional ByRef intMatchIndexStart As Integer = 0, Optional ByRef intMatchIndexEnd As Integer = 0) As Boolean
        ' Searches the loaded data for sngSearchValue with a tolerance of +/-sngTolerance
        ' Returns True if a match is found; in addition, populates intMatchIndexStart and intMatchIndexEnd
        ' Otherwise, returns false

        Dim blnMatchFound As Boolean

        If mDataType <> eDataTypeToUse.SingleType Then
            Select Case mDataType
                Case eDataTypeToUse.IntegerType
                    blnMatchFound = FindValueRange(CInt(sngSearchValue), CInt(sngToleranceHalfWidth), intMatchIndexStart, intMatchIndexEnd)
                Case eDataTypeToUse.DoubleType
                    blnMatchFound = FindValueRange(CDbl(sngSearchValue), CDbl(sngToleranceHalfWidth), intMatchIndexStart, intMatchIndexEnd)
                Case Else
                    blnMatchFound = False
            End Select
        Else
            intMatchIndexStart = 0
            intMatchIndexEnd = mDataSingle.Length - 1

            If mDataSingle.Length = 0 Then
                intMatchIndexEnd = -1
            ElseIf mDataSingle.Length = 1 Then
                If Math.Abs(sngSearchValue - mDataSingle(0)) > sngToleranceHalfWidth Then
                    ' Only one data point, and it is not within tolerance
                    intMatchIndexEnd = -1
                End If
            Else
                BinarySearchRangeSng(sngSearchValue, sngToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
            End If

            If intMatchIndexStart > intMatchIndexEnd Then
                intMatchIndexStart = -1
                intMatchIndexEnd = -1
                blnMatchFound = False
            Else
                blnMatchFound = True
            End If
        End If

        Return blnMatchFound
    End Function
#End Region


#Region "Get Value by Index"
    Public Function GetValueByIndexInt(intIndex As Integer) As Integer
        Try
            Return CInt(GetValueByIndex(intIndex))
        Catch ex As Exception
            Return 0
        End Try
    End Function

    Public Function GetValueByIndex(intIndex As Integer) As Double
        Try
            If mDataType = eDataTypeToUse.NoDataPresent Then
                Return 0
            Else
                Select Case mDataType
                    Case eDataTypeToUse.IntegerType
                        Return mDataInt(intIndex)
                    Case eDataTypeToUse.SingleType
                        Return mDataSingle(intIndex)
                    Case eDataTypeToUse.DoubleType
                        Return mDataDouble(intIndex)
                End Select
            End If
        Catch ex As Exception
            ' intIndex is probably out of range
            ' Ignore errors
        End Try

        Return 0

    End Function

    Public Function GetValueByIndexSng(intIndex As Integer) As Single
        Try
            Return CSng(GetValueByIndex(intIndex))
        Catch ex As Exception
            Return 0
        End Try
    End Function
#End Region


#Region "Get Value by Original Index"
    Public Function GetValueByOriginalIndexInt(intIndex As Integer) As Integer
        Try
            Return CInt(GetValueByOriginalIndex(intIndex))
        Catch ex As Exception
            Return 0
        End Try
    End Function

    Public Function GetValueByOriginalIndex(intIndexOriginal As Integer) As Double
        Dim intIndex As Integer

        If Not mPointerArrayIsValid OrElse mDataType = eDataTypeToUse.NoDataPresent Then
            Return 0
        Else
            Try
                intIndex = Array.IndexOf(mPointerIndices, intIndexOriginal)
                If intIndex >= 0 Then
                    Select Case mDataType
                        Case eDataTypeToUse.IntegerType
                            Return mDataInt(mPointerIndices(intIndex))
                        Case eDataTypeToUse.SingleType
                            Return mDataSingle(mPointerIndices(intIndex))
                        Case eDataTypeToUse.DoubleType
                            Return mDataDouble(mPointerIndices(intIndex))
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

    Public Function GetValueByOriginalIndexSng(intIndex As Integer) As Single
        Try
            Return CSng(GetValueByOriginalIndex(intIndex))
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

    Private Sub InitializePointerIndexArray(intLength As Integer)
        Dim intIndex As Integer

        If intLength < 0 Then intLength = 0
        ReDim mPointerIndices(intLength - 1)

        For intIndex = 0 To intLength - 1
            mPointerIndices(intIndex) = intIndex
        Next intIndex

        If intLength > 0 Then
            mPointerArrayIsValid = True
        Else
            mPointerArrayIsValid = False
        End If
    End Sub

End Class
