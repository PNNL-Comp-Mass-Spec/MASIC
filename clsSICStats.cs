Public Class clsSICStats

    Public Property Peak As MASICPeakFinder.clsSICStatsPeak

    Public Property ScanTypeForPeakIndices As clsScanList.eScanTypeConstants

    ''' <summary>
    ''' Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum
    ''' </summary>
    ''' <returns></returns>
    Public Property PeakScanIndexStart As Integer

    ''' <summary>
    ''' Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum
    ''' </summary>
    ''' <returns></returns>
    Public Property PeakScanIndexEnd As Integer

    ''' <summary>
    ''' Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum
    ''' </summary>
    ''' <returns></returns>
    Public Property PeakScanIndexMax As Integer

    Public Property SICPotentialAreaStatsForPeak As MASICPeakFinder.clsSICPotentialAreaStats

    Public Sub New()
        Peak = New MASICPeakFinder.clsSICStatsPeak()
    End Sub

    Public Overrides Function ToString() As String
        Return "Peak at index " & Peak.IndexMax & ", area " & Peak.Area
    End Function

End Class
