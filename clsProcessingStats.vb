Public Class clsProcessingStats

    Public PeakMemoryUsageMB As Single
    Public TotalProcessingTimeAtStart As Single
    Public CacheEventCount As Integer
    Public UnCacheEventCount As Integer

    Public FileLoadStartTime As DateTime
    Public FileLoadEndTime As DateTime

    Public ProcessingStartTime As DateTime
    Public ProcessingEndTime As DateTime

    Public MemoryUsageMBAtStart As Single
    Public MemoryUsageMBDuringLoad As Single
    Public MemoryUsageMBAtEnd As Single

    Public Overrides Function ToString() As String
        Return "PeakMemoryUsageMB: " & PeakMemoryUsageMB.ToString("0.0") & ", " &
            "CacheEventCount: " & CacheEventCount & ", " &
            "UnCacheEventCount: " & UnCacheEventCount
    End Function

End Class
