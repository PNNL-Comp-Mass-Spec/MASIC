Option Strict On

' The functions in this class can be used to decode a base-64 encoded array of numbers,
' or to encode an array of numbers into a base-64 string
'
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in November 2004
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.
'
' Last modified November 20, 2004

Public Class clsBase64EncodeDecode

    Private Shared Function B64Encode(ByVal BinaryData() As Byte) As String
        Return System.Convert.ToBase64String(BinaryData).Replace("=", "")
    End Function

    Public Shared Function DecodeNumericArray(ByVal strBase64EncodedText As String, ByRef dataArray() As System.Byte) As Boolean
        ' Extracts an array of Bytes from a base-64 encoded string

        dataArray = System.Convert.FromBase64String(strBase64EncodedText)
        Return True

    End Function

    Public Shared Function DecodeNumericArray(ByVal strBase64EncodedText As String, ByRef dataArray() As System.Int16) As Boolean
        ' Extracts an array of 16-bit integers from a base-64 encoded string

        Const DATA_TYPE_PRECISION_BYTES As System.Int32 = 2
        Dim bytArray() As System.Byte
        Dim bytArrayOneValue(DATA_TYPE_PRECISION_BYTES - 1) As System.Byte

        Dim intIndex As Integer

        bytArray = System.Convert.FromBase64String(strBase64EncodedText)

        If Not bytArray.Length Mod DATA_TYPE_PRECISION_BYTES = 0 Then
            ' Array is not divisible by DATA_TYPE_PRECISION_BYTES; not the correct length
            Return False
        End If

        ReDim dataArray(CInt(bytArray.Length / DATA_TYPE_PRECISION_BYTES) - 1)

        For intIndex = 0 To bytArray.Length - 1 Step DATA_TYPE_PRECISION_BYTES

            ' Swap bytes before converting from DATA_TYPE_PRECISION_BYTES bits to one 16-bit integer
            bytArrayOneValue(0) = bytArray(intIndex + 1)
            bytArrayOneValue(1) = bytArray(intIndex + 0)

            dataArray(CInt(intIndex / DATA_TYPE_PRECISION_BYTES)) = BitConverter.ToInt16(bytArrayOneValue, 0)
        Next intIndex

        Return True

    End Function

    Public Shared Function DecodeNumericArray(ByVal strBase64EncodedText As String, ByRef dataArray() As System.Int32) As Boolean
        ' Extracts an array of 32-bit integers from a base-64 encoded string

        Const DATA_TYPE_PRECISION_BYTES As System.Int32 = 4
        Dim bytArray() As System.Byte
        Dim bytArrayOneValue(DATA_TYPE_PRECISION_BYTES - 1) As System.Byte

        Dim intIndex As Integer

        bytArray = System.Convert.FromBase64String(strBase64EncodedText)

        If Not bytArray.Length Mod DATA_TYPE_PRECISION_BYTES = 0 Then
            ' Array is not divisible by DATA_TYPE_PRECISION_BYTES; not the correct length
            Return False
        End If

        ReDim dataArray(CInt(bytArray.Length / DATA_TYPE_PRECISION_BYTES) - 1)

        For intIndex = 0 To bytArray.Length - 1 Step DATA_TYPE_PRECISION_BYTES

            ' Swap bytes before converting from DATA_TYPE_PRECISION_BYTES bits to one 32-bit integer
            bytArrayOneValue(0) = bytArray(intIndex + 3)
            bytArrayOneValue(1) = bytArray(intIndex + 2)
            bytArrayOneValue(2) = bytArray(intIndex + 1)
            bytArrayOneValue(3) = bytArray(intIndex + 0)

            dataArray(CInt(intIndex / DATA_TYPE_PRECISION_BYTES)) = BitConverter.ToInt32(bytArrayOneValue, 0)
        Next intIndex

        Return True

    End Function

    Public Shared Function DecodeNumericArray(ByVal strBase64EncodedText As String, ByRef dataArray() As Single) As Boolean
        ' Extracts an array of Singles from a base-64 encoded string

        Const DATA_TYPE_PRECISION_BYTES As System.Int32 = 4
        Dim bytArray() As System.Byte
        Dim bytArrayOneValue(DATA_TYPE_PRECISION_BYTES - 1) As System.Byte

        Dim intIndex As Integer

        bytArray = System.Convert.FromBase64String(strBase64EncodedText)

        If Not bytArray.Length Mod DATA_TYPE_PRECISION_BYTES = 0 Then
            ' Array is not divisible by DATA_TYPE_PRECISION_BYTES; not the correct length
            Return False
        End If

        ReDim dataArray(CInt(bytArray.Length / DATA_TYPE_PRECISION_BYTES) - 1)

        For intIndex = 0 To bytArray.Length - 1 Step DATA_TYPE_PRECISION_BYTES

            ' Swap bytes before converting from DATA_TYPE_PRECISION_BYTES bits to one single
            bytArrayOneValue(0) = bytArray(intIndex + 3)
            bytArrayOneValue(1) = bytArray(intIndex + 2)
            bytArrayOneValue(2) = bytArray(intIndex + 1)
            bytArrayOneValue(3) = bytArray(intIndex + 0)

            dataArray(CInt(intIndex / DATA_TYPE_PRECISION_BYTES)) = BitConverter.ToSingle(bytArrayOneValue, 0)
        Next intIndex

        Return True

    End Function

    Public Shared Function DecodeNumericArray(ByVal strBase64EncodedText As String, ByRef dataArray() As Double) As Boolean
        ' Extracts an array of Doubles from a base-64 encoded string

        Const DATA_TYPE_PRECISION_BYTES As System.Int32 = 8
        Dim bytArray() As System.Byte
        Dim bytArrayOneValue(DATA_TYPE_PRECISION_BYTES - 1) As System.Byte

        Dim intIndex As Integer

        bytArray = System.Convert.FromBase64String(strBase64EncodedText)

        If Not bytArray.Length Mod DATA_TYPE_PRECISION_BYTES = 0 Then
            ' Array is not divisible by DATA_TYPE_PRECISION_BYTES; not the correct length
            Return False
        End If

        ReDim dataArray(CInt(bytArray.Length / DATA_TYPE_PRECISION_BYTES) - 1)

        For intIndex = 0 To bytArray.Length - 1 Step DATA_TYPE_PRECISION_BYTES

            ' Swap bytes before converting from DATA_TYPE_PRECISION_BYTES bits to one double
            bytArrayOneValue(0) = bytArray(intIndex + 7)
            bytArrayOneValue(1) = bytArray(intIndex + 6)
            bytArrayOneValue(2) = bytArray(intIndex + 5)
            bytArrayOneValue(3) = bytArray(intIndex + 4)
            bytArrayOneValue(4) = bytArray(intIndex + 3)
            bytArrayOneValue(5) = bytArray(intIndex + 2)
            bytArrayOneValue(6) = bytArray(intIndex + 1)
            bytArrayOneValue(7) = bytArray(intIndex)

            dataArray(CInt(intIndex / DATA_TYPE_PRECISION_BYTES)) = BitConverter.ToDouble(bytArrayOneValue, 0)
        Next intIndex

        Return True

    End Function

    Public Shared Function EncodeNumericArray(ByRef dataArray() As System.Byte, ByRef intPrecisionBitsReturn As System.Int32, ByRef strDataTypeNameReturn As String) As String
        ' Converts an array of Bytes to a base-64 encoded string
        ' In addition, returns the bits of precision and datatype name for the given data type

        Const DATA_TYPE_PRECISION_BYTES As System.Int32 = 1
        Const DATA_TYPE_NAME As String = "byte"

        intPrecisionBitsReturn = DATA_TYPE_PRECISION_BYTES * 8
        strDataTypeNameReturn = DATA_TYPE_NAME

        If dataArray Is Nothing OrElse dataArray.Length = -1 Then
            Return String.Empty
        Else
            Return B64Encode(dataArray)
        End If

    End Function

    Public Shared Function EncodeNumericArray(ByRef dataArray() As System.Int16, ByRef intPrecisionBitsReturn As System.Int32, ByRef strDataTypeNameReturn As String) As String
        ' Converts an array of 16-bit integers to a base-64 encoded string
        ' In addition, returns the bits of precision and datatype name for the given data type

        Const DATA_TYPE_PRECISION_BYTES As System.Int32 = 2
        Const DATA_TYPE_NAME As String = "int"

        Dim bytArray() As System.Byte
        Dim bytNewBytes(DATA_TYPE_PRECISION_BYTES - 1) As System.Byte

        Dim intIndex As Integer
        Dim intBaseIndex As Integer

        intPrecisionBitsReturn = DATA_TYPE_PRECISION_BYTES * 8
        strDataTypeNameReturn = DATA_TYPE_NAME

        If dataArray Is Nothing OrElse dataArray.Length = -1 Then
            Return String.Empty
        Else
            ReDim bytArray(dataArray.Length * DATA_TYPE_PRECISION_BYTES - 1)

            For intIndex = 0 To dataArray.Length - 1
                intBaseIndex = intIndex * DATA_TYPE_PRECISION_BYTES

                bytNewBytes = BitConverter.GetBytes(dataArray(intIndex))

                ' Swap bytes when copying into bytArray
                bytArray(intBaseIndex + 0) = bytNewBytes(1)
                bytArray(intBaseIndex + 1) = bytNewBytes(0)

            Next intIndex
            Return B64Encode(bytArray)
        End If

    End Function

    Public Shared Function EncodeNumericArray(ByRef dataArray() As System.Int32, ByRef intPrecisionBitsReturn As System.Int32, ByRef strDataTypeNameReturn As String) As String
        ' Converts an array of 32-bit integers to a base-64 encoded string
        ' In addition, returns the bits of precision and datatype name for the given data type

        Const DATA_TYPE_PRECISION_BYTES As System.Int32 = 4
        Const DATA_TYPE_NAME As String = "int"

        Dim bytArray() As System.Byte
        Dim bytNewBytes(DATA_TYPE_PRECISION_BYTES - 1) As System.Byte

        Dim intIndex As Integer
        Dim intBaseIndex As Integer

        intPrecisionBitsReturn = DATA_TYPE_PRECISION_BYTES * 8
        strDataTypeNameReturn = DATA_TYPE_NAME

        If dataArray Is Nothing OrElse dataArray.Length = -1 Then
            Return String.Empty
        Else
            ReDim bytArray(dataArray.Length * DATA_TYPE_PRECISION_BYTES - 1)

            For intIndex = 0 To dataArray.Length - 1
                intBaseIndex = intIndex * DATA_TYPE_PRECISION_BYTES

                bytNewBytes = BitConverter.GetBytes(dataArray(intIndex))

                ' Swap bytes when copying into bytArray
                bytArray(intBaseIndex + 0) = bytNewBytes(3)
                bytArray(intBaseIndex + 1) = bytNewBytes(2)
                bytArray(intBaseIndex + 2) = bytNewBytes(1)
                bytArray(intBaseIndex + 3) = bytNewBytes(0)

            Next intIndex
            Return B64Encode(bytArray)
        End If

    End Function

    Public Shared Function EncodeNumericArray(ByRef dataArray() As System.Single, ByRef intPrecisionBitsReturn As System.Int32, ByRef strDataTypeNameReturn As String) As String
        ' Converts an array of singles to a base-64 encoded string
        ' In addition, returns the bits of precision and datatype name for the given data type

        Const DATA_TYPE_PRECISION_BYTES As System.Int32 = 4
        Const DATA_TYPE_NAME As String = "float"

        Dim bytArray() As System.Byte
        Dim bytNewBytes(DATA_TYPE_PRECISION_BYTES - 1) As System.Byte

        Dim intIndex As Integer
        Dim intBaseIndex As Integer

        intPrecisionBitsReturn = DATA_TYPE_PRECISION_BYTES * 8
        strDataTypeNameReturn = DATA_TYPE_NAME

        If dataArray Is Nothing OrElse dataArray.Length = -1 Then
            Return String.Empty
        Else
            ReDim bytArray(dataArray.Length * DATA_TYPE_PRECISION_BYTES - 1)

            For intIndex = 0 To dataArray.Length - 1
                intBaseIndex = intIndex * DATA_TYPE_PRECISION_BYTES

                bytNewBytes = BitConverter.GetBytes(dataArray(intIndex))

                ' Swap bytes when copying into bytArray
                bytArray(intBaseIndex + 0) = bytNewBytes(3)
                bytArray(intBaseIndex + 1) = bytNewBytes(2)
                bytArray(intBaseIndex + 2) = bytNewBytes(1)
                bytArray(intBaseIndex + 3) = bytNewBytes(0)

            Next intIndex
            Return B64Encode(bytArray)
        End If

    End Function

    Public Shared Function EncodeNumericArray(ByRef dataArray() As System.Double, ByRef intPrecisionBitsReturn As System.Int32, ByRef strDataTypeNameReturn As String) As String
        ' Converts an array of doubles to a base-64 encoded string
        ' In addition, returns the bits of precision and datatype name for the given data type

        Const DATA_TYPE_PRECISION_BYTES As System.Int32 = 8
        Const DATA_TYPE_NAME As String = "float"

        Dim bytArray() As System.Byte
        Dim bytNewBytes(DATA_TYPE_PRECISION_BYTES - 1) As System.Byte

        Dim intIndex As Integer
        Dim intBaseIndex As Integer

        intPrecisionBitsReturn = DATA_TYPE_PRECISION_BYTES * 8
        strDataTypeNameReturn = DATA_TYPE_NAME

        If dataArray Is Nothing OrElse dataArray.Length = -1 Then
            Return String.Empty
        Else
            ReDim bytArray(dataArray.Length * DATA_TYPE_PRECISION_BYTES - 1)

            For intIndex = 0 To dataArray.Length - 1
                intBaseIndex = intIndex * DATA_TYPE_PRECISION_BYTES

                bytNewBytes = BitConverter.GetBytes(dataArray(intIndex))

                ' Swap bytes when copying into bytArray
                bytArray(intBaseIndex + 0) = bytNewBytes(7)
                bytArray(intBaseIndex + 1) = bytNewBytes(6)
                bytArray(intBaseIndex + 2) = bytNewBytes(5)
                bytArray(intBaseIndex + 3) = bytNewBytes(4)
                bytArray(intBaseIndex + 4) = bytNewBytes(3)
                bytArray(intBaseIndex + 5) = bytNewBytes(2)
                bytArray(intBaseIndex + 6) = bytNewBytes(1)
                bytArray(intBaseIndex + 7) = bytNewBytes(0)

            Next intIndex
            Return B64Encode(bytArray)
        End If

    End Function

End Class
