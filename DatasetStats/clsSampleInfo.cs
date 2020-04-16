Namespace DatasetStats
    Public Class SampleInfo
        Public Property SampleName As String
        Public Property Comment1 As String
        Public Property Comment2 As String

        ''' <summary>
        ''' Constructor
        ''' </summary>
        Public Sub New()
            Clear()
        End Sub

        Public Sub Clear()
            SampleName = String.Empty
            Comment1 = String.Empty
            Comment2 = String.Empty
        End Sub

        Public Function HasData() As Boolean
            If Not String.IsNullOrWhiteSpace(SampleName) OrElse
               Not String.IsNullOrWhiteSpace(Comment1) OrElse
               Not String.IsNullOrWhiteSpace(Comment2) Then
                Return True
            Else
                Return False
            End If
        End Function

        Public Overrides Function ToString() As String
            Return SampleName
        End Function
    End Class
End Namespace