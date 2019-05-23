Public Class clsBinnedData

    Public Property BinnedDataStartX As Single
    Public Property BinSize As Single

    ''' <summary>
    ''' Number of bins in BinnedIntensities
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property BinCount As Integer
        Get
            Return BinnedIntensities.Count
        End Get
    End Property

    ''' <summary>
    ''' List of binned intensities; First bin starts at BinnedDataStartX
    ''' </summary>
    Public ReadOnly BinnedIntensities As List(Of Single)

    ''' <summary>
    ''' List of binned intensity offsets; First bin starts at BinnedDataStartX + BinSize/2
    ''' </summary>
    Public ReadOnly BinnedIntensitiesOffset As List(Of Single)

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        BinnedIntensities = New List(Of Single)
        BinnedIntensitiesOffset = New List(Of Single)
    End Sub

    Public Overrides Function ToString() As String
        Return "BinCount: " & BinCount & ", BinSize: " & BinSize.ToString("0.0") & ", StartX: " & BinnedDataStartX.ToString("0.0")
    End Function

End Class
