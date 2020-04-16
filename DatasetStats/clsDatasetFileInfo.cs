Namespace DatasetStats
    Public Class DatasetFileInfo
        Public Property FileSystemCreationTime As DateTime
        Public Property FileSystemModificationTime As DateTime
        Public Property DatasetID As Integer
        Public Property DatasetName As String
        Public Property FileExtension As String
        Public Property AcqTimeStart As DateTime
        Public Property AcqTimeEnd As DateTime
        Public Property ScanCount As Integer
        Public Property FileSizeBytes As Long

        ''' <summary>
        ''' Constructor
        ''' </summary>
        Public Sub New()
            Clear()
        End Sub

        Public Sub Clear()
            FileSystemCreationTime = DateTime.MinValue
            FileSystemModificationTime = DateTime.MinValue
            DatasetID = 0
            DatasetName = String.Empty
            FileExtension = String.Empty
            AcqTimeStart = DateTime.MinValue
            AcqTimeEnd = DateTime.MinValue
            ScanCount = 0
            FileSizeBytes = 0
        End Sub

        Public Overrides Function ToString() As String
            Return string.Format("Dataset {0}, ScanCount={1}", DatasetName, ScanCount)
        End Function
    End Class
End Namespace