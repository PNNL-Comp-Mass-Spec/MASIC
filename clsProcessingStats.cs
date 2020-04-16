Public Class clsProcessingStats

    Public Property PeakMemoryUsageMB As Single
    Public Property TotalProcessingTimeAtStart As Single
    Public Property CacheEventCount As Integer
    Public Property UnCacheEventCount As Integer

    Public Property FileLoadStartTime As DateTime
    Public Property FileLoadEndTime As DateTime

    Public Property ProcessingStartTime As DateTime
    Public Property ProcessingEndTime As DateTime

    Public Property MemoryUsageMBAtStart As Single
    Public Property MemoryUsageMBDuringLoad As Single
    Public Property MemoryUsageMBAtEnd As Single

    Public Overrides Function ToString() As String
        Return "PeakMemoryUsageMB: " & PeakMemoryUsageMB.ToString("0.0") & ", " &
            "CacheEventCount: " & CacheEventCount & ", " &
            "UnCacheEventCount: " & UnCacheEventCount
    End Function

End Class
