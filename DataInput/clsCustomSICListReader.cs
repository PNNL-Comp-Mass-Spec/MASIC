Imports MASIC.clsMASIC

Namespace DataInput

    Public Class clsCustomSICListReader
        Inherits clsMasicEventNotifier

#Region "Constants and Enums"

        Public Const CUSTOM_SIC_COLUMN_MZ As String = "MZ"
        Public Const CUSTOM_SIC_COLUMN_MZ_TOLERANCE As String = "MZToleranceDa"
        Public Const CUSTOM_SIC_COLUMN_SCAN_CENTER As String = "ScanCenter"
        Public Const CUSTOM_SIC_COLUMN_SCAN_TOLERANCE As String = "ScanTolerance"
        Public Const CUSTOM_SIC_COLUMN_SCAN_TIME As String = "ScanTime"
        Public Const CUSTOM_SIC_COLUMN_TIME_TOLERANCE As String = "TimeTolerance"
        Public Const CUSTOM_SIC_COLUMN_COMMENT As String = "Comment"

        Private Enum eCustomSICFileColumns
            MZ = 0
            MZToleranceDa = 1
            ScanCenter = 2              ' Absolute scan or Relative Scan, or Acquisition Time
            ScanTolerance = 3           ' Absolute scan or Relative Scan, or Acquisition Time
            ScanTime = 4                ' Only used for acquisition Time
            TimeTolerance = 5           ' Only used for acquisition Time
            Comment = 6
        End Enum


#End Region

#Region "Classwide Variables"

        Private ReadOnly mCustomSICList As clsCustomSICList

#End Region

        Public Shared Function GetCustomMZFileColumnHeaders(
                                                            Optional cColDelimiter As String = ", ",
                                                            Optional includeAndBeforeLastItem As Boolean = True) As String

            Dim headerNames = New List(Of String) From {
                    CUSTOM_SIC_COLUMN_MZ,
                    CUSTOM_SIC_COLUMN_MZ_TOLERANCE,
                    CUSTOM_SIC_COLUMN_SCAN_CENTER,
                    CUSTOM_SIC_COLUMN_SCAN_TOLERANCE,
                    CUSTOM_SIC_COLUMN_SCAN_TIME,
                    CUSTOM_SIC_COLUMN_TIME_TOLERANCE
                    }

            If includeAndBeforeLastItem Then
                headerNames.Add("and " & CUSTOM_SIC_COLUMN_COMMENT)
            Else
                headerNames.Add(CUSTOM_SIC_COLUMN_COMMENT)
            End If

            Return String.Join(cColDelimiter, headerNames)

        End Function

        ''' <summary>
        ''' Constructor
        ''' </summary>
        Public Sub New(customSicList As clsCustomSICList)
            mCustomSICList = customSicList
        End Sub

        Public Function LoadCustomSICListFromFile(customSICValuesFileName As String) As Boolean


            Dim delimiterList = New Char() {ControlChars.Tab}
            Dim forceAcquisitionTimeMode As Boolean

            Try
                Dim mzHeaderFound = False
                Dim scanTimeHeaderFound = False
                Dim timeToleranceHeaderFound = False

                mCustomSICList.ResetMzSearchValues()

                ' eColumnMapping will be initialized when the headers are read
                Dim eColumnMapping() As Integer
                ReDim eColumnMapping(-1)

                If Not File.Exists(customSICValuesFileName) Then
                    ' Custom SIC file not found
                    Dim errorMessage = "Custom MZ List file not found: " & customSICValuesFileName
                    ReportError(errorMessage)
                    mCustomSICList.CustomMZSearchValues.Clear()
                    Return False
                End If

                Using reader = New StreamReader(New FileStream(customSICValuesFileName, FileMode.Open, FileAccess.Read, FileShare.Read))

                    Dim linesRead = 0
                    Do While Not reader.EndOfStream
                        Dim dataLine = reader.ReadLine
                        If dataLine Is Nothing Then Continue Do

                        If linesRead = 0 AndAlso Not dataLine.Contains(ControlChars.Tab) Then
                            ' Split on commas instead of tab characters
                            delimiterList = New Char() {","c}
                        End If

                        Dim dataCols = dataLine.Split(delimiterList)

                        If (dataCols Is Nothing) OrElse dataCols.Length <= 0 Then Continue Do

                        ' This is the first non-blank line
                        linesRead += 1

                        If linesRead = 1 Then

                            ' Initialize eColumnMapping, setting the value for each column to -1, indicating the column is not present
                            ReDim eColumnMapping(dataCols.Length - 1)
                            For colIndex = 0 To eColumnMapping.Length - 1
                                eColumnMapping(colIndex) = -1
                            Next

                            ' The first row must be the header row; parse the values
                            For colIndex = 0 To dataCols.Length - 1
                                Select Case dataCols(colIndex).ToUpper()
                                    Case CUSTOM_SIC_COLUMN_MZ.ToUpper()
                                        eColumnMapping(colIndex) = eCustomSICFileColumns.MZ
                                        mzHeaderFound = True

                                    Case CUSTOM_SIC_COLUMN_MZ_TOLERANCE.ToUpper()
                                        eColumnMapping(colIndex) = eCustomSICFileColumns.MZToleranceDa

                                    Case CUSTOM_SIC_COLUMN_SCAN_CENTER.ToUpper()
                                        eColumnMapping(colIndex) = eCustomSICFileColumns.ScanCenter

                                    Case CUSTOM_SIC_COLUMN_SCAN_TOLERANCE.ToUpper()
                                        eColumnMapping(colIndex) = eCustomSICFileColumns.ScanTolerance

                                    Case CUSTOM_SIC_COLUMN_SCAN_TIME.ToUpper()
                                        eColumnMapping(colIndex) = eCustomSICFileColumns.ScanTime
                                        scanTimeHeaderFound = True

                                    Case CUSTOM_SIC_COLUMN_TIME_TOLERANCE.ToUpper()
                                        eColumnMapping(colIndex) = eCustomSICFileColumns.TimeTolerance
                                        timeToleranceHeaderFound = True

                                    Case CUSTOM_SIC_COLUMN_COMMENT.ToUpper()
                                        eColumnMapping(colIndex) = eCustomSICFileColumns.Comment

                                    Case Else
                                        ' Unknown column name; ignore it
                                End Select
                            Next

                            ' Make sure that, at a minimum, the MZ column is present
                            If Not mzHeaderFound Then

                                Dim errorMessage = "Custom M/Z List file " & customSICValuesFileName & "does not have a column header named " & CUSTOM_SIC_COLUMN_MZ & " in the first row; this header is required (valid column headers are: " & GetCustomMZFileColumnHeaders() & ")"
                                ReportError(errorMessage)

                                mCustomSICList.CustomMZSearchValues.Clear()
                                Return False
                            End If

                            If scanTimeHeaderFound AndAlso timeToleranceHeaderFound Then
                                forceAcquisitionTimeMode = True
                                mCustomSICList.ScanToleranceType = clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime
                            Else
                                forceAcquisitionTimeMode = False
                            End If

                            Continue Do
                        End If

                        ' Parse this line's data if dataCols(0) is numeric
                        If Not clsUtilities.IsNumber(dataCols(0)) Then
                            Continue Do
                        End If

                        Dim mzSearchSpec = New clsCustomMZSearchSpec(0)

                        With mzSearchSpec

                            .MZToleranceDa = 0
                            .ScanOrAcqTimeCenter = 0
                            .ScanOrAcqTimeTolerance = 0

                            For colIndex = 0 To dataCols.Length - 1

                                If colIndex >= eColumnMapping.Length Then Exit For

                                Select Case eColumnMapping(colIndex)
                                    Case eCustomSICFileColumns.MZ
                                        If Not Double.TryParse(dataCols(colIndex), .MZ) Then
                                            Throw New InvalidCastException(
                                                    "Non-numeric value for the MZ column in row " & linesRead + 1 &
                                                    ", column " & colIndex + 1)
                                        End If

                                    Case eCustomSICFileColumns.MZToleranceDa
                                        If Not Double.TryParse(dataCols(colIndex), .MZToleranceDa) Then
                                            Throw New InvalidCastException(
                                                    "Non-numeric value for the MZToleranceDa column in row " &
                                                    linesRead + 1 & ", column " & colIndex + 1)
                                        End If

                                    Case eCustomSICFileColumns.ScanCenter
                                        ' Do not use this value if both the ScanTime and the TimeTolerance columns were present
                                        If Not forceAcquisitionTimeMode Then
                                            If Not Single.TryParse(dataCols(colIndex), .ScanOrAcqTimeCenter) Then
                                                Throw New InvalidCastException(
                                                        "Non-numeric value for the ScanCenter column in row " &
                                                        linesRead + 1 & ", column " & colIndex + 1)
                                            End If
                                        End If

                                    Case eCustomSICFileColumns.ScanTolerance
                                        ' Do not use this value if both the ScanTime and the TimeTolerance columns were present
                                        If Not forceAcquisitionTimeMode Then
                                            If mCustomSICList.ScanToleranceType = clsCustomSICList.eCustomSICScanTypeConstants.Absolute Then
                                                .ScanOrAcqTimeTolerance = CInt(dataCols(colIndex))
                                            Else
                                                ' Includes .Relative and .AcquisitionTime
                                                If Not Single.TryParse(dataCols(colIndex), .ScanOrAcqTimeTolerance) Then
                                                    Throw New InvalidCastException(
                                                            "Non-numeric value for the ScanTolerance column in row " &
                                                            linesRead + 1 & ", column " & colIndex + 1)
                                                End If
                                            End If
                                        End If

                                    Case eCustomSICFileColumns.ScanTime
                                        ' Only use this value if both the ScanTime and the TimeTolerance columns were present
                                        If forceAcquisitionTimeMode Then
                                            If Not Single.TryParse(dataCols(colIndex), .ScanOrAcqTimeCenter) Then
                                                Throw New InvalidCastException(
                                                        "Non-numeric value for the ScanTime column in row " &
                                                        linesRead + 1 & ", column " & colIndex + 1)
                                            End If
                                        End If

                                    Case eCustomSICFileColumns.TimeTolerance
                                        ' Only use this value if both the ScanTime and the TimeTolerance columns were present
                                        If forceAcquisitionTimeMode Then
                                            If Not Single.TryParse(dataCols(colIndex), .ScanOrAcqTimeTolerance) Then
                                                Throw New InvalidCastException(
                                                        "Non-numeric value for the TimeTolerance column in row " &
                                                        linesRead + 1 & ", column " & colIndex + 1)
                                            End If
                                        End If

                                    Case eCustomSICFileColumns.Comment
                                        .Comment = String.Copy(dataCols(colIndex))
                                    Case Else
                                        ' Unknown column code
                                End Select

                            Next

                        End With

                        mCustomSICList.AddMzSearchTarget(mzSearchSpec)

                    Loop

                End Using

            Catch ex As Exception
                ReportError("Error in LoadCustomSICListFromFile", ex, eMasicErrorCodes.InvalidCustomSICValues)
                mCustomSICList.CustomMZSearchValues.Clear()
                Return False
            End Try

            If Not forceAcquisitionTimeMode Then
                mCustomSICList.ValidateCustomSICList()
            End If

            Return True

        End Function

    End Class
End Namespace