
Public Class clsSICStats
    Public Property Peak As MASICPeakFinder.clsSICStatsPeak

    Public Property SICPeakWidthFullScans As Integer

    ''' <summary>
    ''' Scan number of the peak apex
    ''' </summary>
    ''' <returns></returns>
    Public Property ScanNumberMaxIntensity As Integer

    Public Property SICPotentialAreaStatsForPeak As MASICPeakFinder.clsSICPotentialAreaStats

    Public Property SICSmoothedYData As List(Of Single)

    Public Property SICSmoothedYDataIndexStart As Integer

    Public Sub New()
        Peak = New MASICPeakFinder.clsSICStatsPeak()
        SICPotentialAreaStatsForPeak = New MASICPeakFinder.clsSICPotentialAreaStats()
        SICSmoothedYData = New List(Of Single)
    End Sub

    Public Overrides Function ToString() As String
        Return "Peak at index " & Peak.IndexMax & ", area " & Peak.Area
    End Function

End Class
