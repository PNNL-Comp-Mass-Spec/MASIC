Public Class clsRawDataExportOptions

#Region "Constants and Enums"
    Public Enum eExportRawDataFileFormatConstants
        PEKFile = 0
        CSVFile = 1
    End Enum
#End Region

    Public Property ExportEnabled As Boolean
    Public Property FileFormat As eExportRawDataFileFormatConstants

    Public Property IncludeMSMS As Boolean
    Public Property RenumberScans As Boolean

    Public Property MinimumSignalToNoiseRatio As Single
    Public Property MaxIonCountPerScan As Integer
    Public Property IntensityMinimum As Single

    Public Sub Reset()

        ExportEnabled = False

        FileFormat = eExportRawDataFileFormatConstants.CSVFile
        IncludeMSMS = False
        RenumberScans = False

        MinimumSignalToNoiseRatio = 1
        MaxIonCountPerScan = 200
        IntensityMinimum = 0

    End Sub

    Public Overrides Function ToString() As String
        If ExportEnabled Then
            Return "Export raw data as " & FileFormat.ToString()
        Else
            Return "Raw data export is disabled"
        End If
    End Function

End Class
