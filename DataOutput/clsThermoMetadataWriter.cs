Imports MASIC.clsMASIC
Imports ThermoRawFileReader

Namespace DataOutput
    Public Class clsThermoMetadataWriter
        Inherits clsMasicEventNotifier

        Public Function SaveMSMethodFile(
          objXcaliburAccessor As XRawFileIO,
          dataOutputHandler As clsDataOutput) As Boolean

            Dim instMethodCount As Integer
            Dim outputFilePath = "?UndefinedFile?"

            Try
                instMethodCount = objXcaliburAccessor.FileInfo.InstMethods.Count
            Catch ex As Exception
                ReportError("Error looking up InstMethod length in objXcaliburAccessor.FileInfo", ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Try
                For index = 0 To instMethodCount - 1

                    Dim methodNum As String
                    If index = 0 And objXcaliburAccessor.FileInfo.InstMethods.Count = 1 Then
                        methodNum = String.Empty
                    Else
                        methodNum = (index + 1).ToString.Trim
                    End If

                    outputFilePath = dataOutputHandler.OutputFileHandles.MSMethodFilePathBase & methodNum & ".txt"

                    Using writer = New StreamWriter(outputFilePath, False)

                        With objXcaliburAccessor.FileInfo
                            writer.WriteLine("Instrument model: " & .InstModel)
                            writer.WriteLine("Instrument name: " & .InstName)
                            writer.WriteLine("Instrument description: " & .InstrumentDescription)
                            writer.WriteLine("Instrument serial number: " & .InstSerialNumber)
                            writer.WriteLine()

                            writer.WriteLine(objXcaliburAccessor.FileInfo.InstMethods(index))
                        End With

                    End Using
                Next

            Catch ex As Exception
                ReportError("Error writing the MS Method to: " & outputFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

        Public Function SaveMSTuneFile(
          objXcaliburAccessor As XRawFileIO,
          dataOutputHandler As clsDataOutput) As Boolean

            Const cColDelimiter As Char = ControlChars.Tab

            Dim tuneMethodCount As Integer
            Dim outputFilePath = "?UndefinedFile?"

            Try
                tuneMethodCount = objXcaliburAccessor.FileInfo.TuneMethods.Count
            Catch ex As Exception
                ReportError("Error looking up TuneMethods length in objXcaliburAccessor.FileInfo", ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Try
                For index = 0 To tuneMethodCount - 1

                    Dim tuneInfoNum As String
                    If index = 0 And objXcaliburAccessor.FileInfo.TuneMethods.Count = 1 Then
                        tuneInfoNum = String.Empty
                    Else
                        tuneInfoNum = (index + 1).ToString.Trim
                    End If
                    outputFilePath = dataOutputHandler.OutputFileHandles.MSTuneFilePathBase & tuneInfoNum & ".txt"

                    Using writer = New StreamWriter(outputFilePath, False)

                        writer.WriteLine("Category" & cColDelimiter & "Name" & cColDelimiter & "Value")

                        With objXcaliburAccessor.FileInfo.TuneMethods(index)
                            For Each setting As udtTuneMethodSetting In .Settings
                                writer.WriteLine(setting.Category & cColDelimiter & setting.Name & cColDelimiter & setting.Value)
                            Next
                            writer.WriteLine()
                        End With

                    End Using

                Next

            Catch ex As Exception
                ReportError("Error writing the MS Tune Settings to: " & outputFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True
        End Function


    End Class

End Namespace
