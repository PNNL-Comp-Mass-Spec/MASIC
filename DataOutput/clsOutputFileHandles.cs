Namespace DataOutput
    Public Class clsOutputFileHandles
        Inherits clsMasicEventNotifier

        Public Property ScanStats As StreamWriter
        Public Property SICDataFile As StreamWriter
        Public Property XMLFileForSICs As Xml.XmlTextWriter
        Public Property MSMethodFilePathBase As String
        Public Property MSTuneFilePathBase As String

        Public Sub CloseScanStats()
            If Not ScanStats Is Nothing Then
                ScanStats.Close()
                ScanStats = Nothing
            End If
        End Sub

        Public Function CloseAll() As Boolean

            Try
                CloseScanStats()

                If Not SICDataFile Is Nothing Then
                    SICDataFile.Close()
                    SICDataFile = Nothing
                End If

                If Not XMLFileForSICs Is Nothing Then
                    XMLFileForSICs.Close()
                    XMLFileForSICs = Nothing
                End If

                Return True
            Catch ex As Exception
                ReportError("Error in CloseOutputFileHandles", ex)
                Return False
            End Try
        End Function
    End Class

End Namespace
