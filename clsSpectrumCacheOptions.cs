Public Class clsSpectrumCacheOptions

#Region "Properties"

    ''' <summary>
    ''' If True, then spectra will never be cached to disk and the spectra pool will consequently be increased as needed
    ''' </summary>
    Public Property DiskCachingAlwaysDisabled As Boolean

    ''' <summary>
    ''' Path to the cache directory (can be relative or absolute, aka rooted); if empty, then the user's AppData directory is used
    ''' </summary>
    Public Property DirectoryPath As String

    Public Property SpectraToRetainInMemory As Integer
        Get
            Return mSpectraToRetainInMemory
        End Get
        Set
            If Value < 100 Then Value = 100
            mSpectraToRetainInMemory = Value
        End Set
    End Property

    <Obsolete("Legacy parameter; no longer used")>
    Public Property MinimumFreeMemoryMB As Single

    <Obsolete("Legacy parameter; no longer used")>
    Public Property MaximumMemoryUsageMB As Single

#End Region

#Region "Classwide variables"
    Private mSpectraToRetainInMemory As Integer = 1000
#End Region

    Public Sub Reset()
        Dim defaultOptions = clsSpectraCache.GetDefaultCacheOptions()

        With defaultOptions
            DiskCachingAlwaysDisabled = .DiskCachingAlwaysDisabled
            DirectoryPath = .DirectoryPath
            SpectraToRetainInMemory = .SpectraToRetainInMemory
        End With
    End Sub

    Public Overrides Function ToString() As String
        Return "Cache up to " & SpectraToRetainInMemory & " in directory " & DirectoryPath
    End Function

End Class
