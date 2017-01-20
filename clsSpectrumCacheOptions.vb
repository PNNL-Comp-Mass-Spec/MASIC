Public Class clsSpectrumCacheOptions

    ''' <summary>
    ''' If True, then spectra will never be cached to disk and the spectra pool will consequently be increased as needed
    ''' </summary>
    Public DiskCachingAlwaysDisabled As Boolean

    ''' <summary>
    ''' Path to the cache folder (can be relative or absolute, aka rooted); if empty, then the user's AppData folder is used
    ''' </summary>
    Public FolderPath As String

    Public SpectraToRetainInMemory As Integer

    <Obsolete("Legacy parameter; no longer used")>
    Public MinimumFreeMemoryMB As Single

    <Obsolete("Legacy parameter; no longer used")>
    Public MaximumMemoryUsageMB As Single

    Public Overrides Function ToString() As String
        Return "Cache up to " & SpectraToRetainInMemory & " in folder " & FolderPath
    End Function

End Class
