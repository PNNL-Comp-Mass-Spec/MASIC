Imports System.Collections.Generic

Public Class clsSmoothedYDataSubset
    Public ReadOnly Property DataCount As Integer
    Public ReadOnly Property Data As Single()
    Public ReadOnly Property DataStartIndex As Integer

    Public Sub New()
        DataCount = 0
        DataStartIndex = 0
        ReDim Data(0)
    End Sub

    Public Sub New(yData As IList(Of Double), startIndex As Integer, endIndex As Integer)

        If yData Is Nothing OrElse endIndex < startIndex OrElse startIndex < 0 Then
            DataCount = 0
            DataStartIndex = 0
            ReDim Data(0)
            Return
        End If

        DataStartIndex = startIndex

        DataCount = endIndex - startIndex + 1
        ReDim Data(DataCount)

        For intIndex = startIndex To endIndex
            Data(intIndex - startIndex) = CSng(Math.Min(yData(intIndex), Single.MaxValue))
        Next

    End Sub
End Class
