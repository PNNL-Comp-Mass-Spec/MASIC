Public Class clsSimilarParentIonsData

    Public Property MZPointerArray As Integer()

    Public Property IonInUseCount As Integer

    Public ReadOnly Property IonUsed As Boolean()

    Public ReadOnly Property UniqueMZList As List(Of clsUniqueMZListItem)

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New(parentIonCount As Integer)
        ReDim MZPointerArray(parentIonCount - 1)
        ReDim IonUsed(parentIonCount - 1)

        UniqueMZList = New List(Of clsUniqueMZListItem)
    End Sub

    Public Overrides Function ToString() As String
        Return "IonInUseCount: " & IonInUseCount
    End Function

End Class
