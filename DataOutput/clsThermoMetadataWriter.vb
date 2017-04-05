Imports MASIC.clsMASIC
Imports ThermoRawFileReader

Namespace DataOutput
    Public Class clsThermoMetadataWriter
        Inherits clsEventNotifier

        Public Function SaveMSMethodFile(
          objXcaliburAccessor As XRawFileIO,
          dataOutputHandler As clsDataOutput) As Boolean

            Dim intInstMethodCount As Integer
            Dim strOutputFilePath = "?undefinedfile?"

            Try
                intInstMethodCount = objXcaliburAccessor.FileInfo.InstMethods.Count
            Catch ex As Exception
                ReportError("SaveMSMethodFile", "Error looking up InstMethod length in objXcaliburAccessor.FileInfo", ex, True, True, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Try
                For intIndex = 0 To intInstMethodCount - 1

                    Dim strMethodNum As String
                    If intIndex = 0 And objXcaliburAccessor.FileInfo.InstMethods.Count = 1 Then
                        strMethodNum = String.Empty
                    Else
                        strMethodNum = (intIndex + 1).ToString.Trim
                    End If

                    strOutputFilePath = dataOutputHandler.OutputFileHandles.MSMethodFilePathBase & strMethodNum & ".txt"

                    Using srOutfile = New StreamWriter(strOutputFilePath, False)

                        With objXcaliburAccessor.FileInfo
                            srOutfile.WriteLine("Instrument model: " & .InstModel)
                            srOutfile.WriteLine("Instrument name: " & .InstName)
                            srOutfile.WriteLine("Instrument description: " & .InstrumentDescription)
                            srOutfile.WriteLine("Instrument serial number: " & .InstSerialNumber)
                            srOutfile.WriteLine()

                            srOutfile.WriteLine(objXcaliburAccessor.FileInfo.InstMethods(intIndex))
                        End With

                    End Using
                Next

            Catch ex As Exception
                ReportError("SaveMSMethodFile", "Error writing the MS Method to: " & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

        Public Function SaveMSTuneFile(
          objXcaliburAccessor As XRawFileIO,
          dataOutputHandler As clsDataOutput) As Boolean

            Const cColDelimiter As Char = ControlChars.Tab

            Dim intTuneMethodCount As Integer
            Dim strOutputFilePath = "?undefinedfile?"

            Try
                intTuneMethodCount = objXcaliburAccessor.FileInfo.TuneMethods.Count
            Catch ex As Exception
                ReportError("SaveMSMethodFile", "Error looking up TuneMethods length in objXcaliburAccessor.FileInfo", ex, True, True, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Try
                For intIndex = 0 To intTuneMethodCount - 1

                    Dim strTuneInfoNum As String
                    If intIndex = 0 And objXcaliburAccessor.FileInfo.TuneMethods.Count = 1 Then
                        strTuneInfoNum = String.Empty
                    Else
                        strTuneInfoNum = (intIndex + 1).ToString.Trim
                    End If
                    strOutputFilePath = dataOutputHandler.OutputFileHandles.MSTuneFilePathBase & strTuneInfoNum & ".txt"

                    Using srOutfile = New StreamWriter(strOutputFilePath, False)

                        srOutfile.WriteLine("Category" & cColDelimiter & "Name" & cColDelimiter & "Value")

                        With objXcaliburAccessor.FileInfo.TuneMethods(intIndex)
                            For Each setting As udtTuneMethodSetting In .Settings
                                srOutfile.WriteLine(setting.Category & cColDelimiter & setting.Name & cColDelimiter & setting.Value)
                            Next
                            srOutfile.WriteLine()
                        End With

                    End Using

                Next

            Catch ex As Exception
                ReportError("SaveMSTuneFile", "Error writing the MS Tune Settings to: " & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True
        End Function


    End Class

End Namespace
