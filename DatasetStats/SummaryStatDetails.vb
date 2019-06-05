
Namespace DatasetStats
    Public Class SummaryStatDetails
        ''' <summary>
        ''' Scan count
        ''' </summary>
        Public Property ScanCount As Integer

        ''' <summary>
        ''' Max TIC
        ''' </summary>
        Public Property TICMax As Double

        ''' <summary>
        ''' Max BPI
        ''' </summary>
        Public Property BPIMax As Double

        ''' <summary>
        ''' Median TIC
        ''' </summary>
        Public Property TICMedian As Double

        ''' <summary>
        ''' Median BPI
        ''' </summary>
        Public Property BPIMedian As Double

        ''' <summary>
        ''' Constructor
        ''' </summary>
        Public Sub New()
            Clear()
        End Sub

        Public Sub Clear()
            ScanCount = 0
            TICMax = 0
            BPIMax = 0
            TICMedian = 0
            BPIMedian = 0
        End Sub

        Public Overrides Function ToString() As String
            Return "ScanCount: " & ScanCount
        End Function
    End Class
End Namespace
