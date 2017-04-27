Imports MASIC.clsMASIC

Namespace DataInput

    Public Class clsCustomSICListReader
        Inherits clsMasicEventNotifier

#Region "Constants and Enums"

        Public Const CUSTOM_SIC_COLUMN_MZ As String = "MZ"
        Public Const CUSTOM_SIC_COLUMN_MZ_TOLERANCE As String = "MZToleranceDa"
        Public Const CUSTOM_SIC_COLUMN_SCAN_CENTER As String = "ScanCenter"
        Public Const CUSTOM_SIC_COLUMN_SCAN_TOLERNACE As String = "ScanTolerance"
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
                                                            Optional blnIncludeAndBeforeLastItem As Boolean = True) As String

            Dim headerNames = New List(Of String) From {
                    CUSTOM_SIC_COLUMN_MZ,
                    CUSTOM_SIC_COLUMN_MZ_TOLERANCE,
                    CUSTOM_SIC_COLUMN_SCAN_CENTER,
                    CUSTOM_SIC_COLUMN_SCAN_TOLERNACE,
                    CUSTOM_SIC_COLUMN_SCAN_TIME,
                    CUSTOM_SIC_COLUMN_TIME_TOLERANCE
                    }

            If blnIncludeAndBeforeLastItem Then
                headerNames.Add(" and " & CUSTOM_SIC_COLUMN_COMMENT)
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

        Public Function LoadCustomSICListFromFile(strCustomSICValuesFileName As String) As Boolean


            Dim strDelimList = New Char() {ControlChars.Tab}
            Dim blnForceAcquisitionTimeMode As Boolean

            Try
                Dim blnMZHeaderFound = False
                Dim blnScanTimeHeaderFound = False
                Dim blnTimeToleranceHeaderFound = False

                mCustomSICList.ResetMzSearchValues()

                ' eColumnMapping will be initialized when the headers are read
                Dim eColumnMapping() As Integer
                ReDim eColumnMapping(-1)

                If Not File.Exists(strCustomSICValuesFileName) Then
                    ' Custom SIC file not found
                    Dim strErrorMessage = "Custom MZ List file not found: " & strCustomSICValuesFileName
                    ReportError(strErrorMessage)
                    mCustomSICList.CustomMZSearchValues.Clear()
                    Return False
                End If

                Using srInFile = New StreamReader(New FileStream(strCustomSICValuesFileName, FileMode.Open, FileAccess.Read, FileShare.Read))

                    Dim intLinesRead = 0
                    Do While Not srInFile.EndOfStream
                        Dim strLineIn = srInFile.ReadLine
                        If strLineIn Is Nothing Then Continue Do

                        If intLinesRead = 0 AndAlso Not strLineIn.Contains(ControlChars.Tab) Then
                            ' Split on commas instead of tab characters
                            strDelimList = New Char() {","c}
                        End If

                        Dim strSplitLine = strLineIn.Split(strDelimList)

                        If (strSplitLine Is Nothing) OrElse strSplitLine.Length <= 0 Then Continue Do

                        ' This is the first non-blank line
                        intLinesRead += 1

                        If intLinesRead = 1 Then

                            ' Initialize eColumnMapping, setting the value for each column to -1, indicating the column is not present
                            ReDim eColumnMapping(strSplitLine.Length - 1)
                            For intColIndex = 0 To eColumnMapping.Length - 1
                                eColumnMapping(intColIndex) = -1
                            Next

                            ' The first row must be the header row; parse the values
                            For intColIndex = 0 To strSplitLine.Length - 1
                                Select Case strSplitLine(intColIndex).ToUpper
                                    Case CUSTOM_SIC_COLUMN_MZ.ToUpper
                                        eColumnMapping(intColIndex) = eCustomSICFileColumns.MZ
                                        blnMZHeaderFound = True

                                    Case CUSTOM_SIC_COLUMN_MZ_TOLERANCE.ToUpper
                                        eColumnMapping(intColIndex) = eCustomSICFileColumns.MZToleranceDa

                                    Case CUSTOM_SIC_COLUMN_SCAN_CENTER.ToUpper
                                        eColumnMapping(intColIndex) = eCustomSICFileColumns.ScanCenter

                                    Case CUSTOM_SIC_COLUMN_SCAN_TOLERNACE.ToUpper
                                        eColumnMapping(intColIndex) = eCustomSICFileColumns.ScanTolerance

                                    Case CUSTOM_SIC_COLUMN_SCAN_TIME.ToUpper
                                        eColumnMapping(intColIndex) = eCustomSICFileColumns.ScanTime
                                        blnScanTimeHeaderFound = True

                                    Case CUSTOM_SIC_COLUMN_TIME_TOLERANCE.ToUpper
                                        eColumnMapping(intColIndex) = eCustomSICFileColumns.TimeTolerance
                                        blnTimeToleranceHeaderFound = True

                                    Case CUSTOM_SIC_COLUMN_COMMENT.ToUpper
                                        eColumnMapping(intColIndex) = eCustomSICFileColumns.Comment

                                    Case Else
                                        ' Unknown column name; ignore it
                                End Select
                            Next

                            ' Make sure that, at a minimum, the MZ column is present
                            If Not blnMZHeaderFound Then

                                Dim strErrorMessage = "Custom M/Z List file " & strCustomSICValuesFileName & "does not have a column header named " & CUSTOM_SIC_COLUMN_MZ & " in the first row; this header is required (valid column headers are: " & GetCustomMZFileColumnHeaders() & ")"
                                ReportError(strErrorMessage)

                                mCustomSICList.CustomMZSearchValues.Clear()
                                Return False
                            End If

                            If blnScanTimeHeaderFound AndAlso blnTimeToleranceHeaderFound Then
                                blnForceAcquisitionTimeMode = True
                                mCustomSICList.ScanToleranceType = clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime
                            Else
                                blnForceAcquisitionTimeMode = False
                            End If

                            Continue Do
                        End If

                        ' Parse this line's data if strSplitLine(0) is numeric
                        If Not clsUtilities.IsNumber(strSplitLine(0)) Then
                            Continue Do
                        End If

                        Dim mzSearchSpec = New clsCustomMZSearchSpec(0)

                        With mzSearchSpec

                            .MZToleranceDa = 0
                            .ScanOrAcqTimeCenter = 0
                            .ScanOrAcqTimeTolerance = 0

                            For intColIndex = 0 To strSplitLine.Length - 1

                                If intColIndex >= eColumnMapping.Length Then Exit For

                                Select Case eColumnMapping(intColIndex)
                                    Case eCustomSICFileColumns.MZ
                                        If Not Double.TryParse(strSplitLine(intColIndex), .MZ) Then
                                            Throw New InvalidCastException(
                                                    "Non-numeric value for the MZ column in row " & intLinesRead + 1 &
                                                    ", column " & intColIndex + 1)
                                        End If

                                    Case eCustomSICFileColumns.MZToleranceDa
                                        If Not Double.TryParse(strSplitLine(intColIndex), .MZToleranceDa) Then
                                            Throw New InvalidCastException(
                                                    "Non-numeric value for the MZToleranceDa column in row " &
                                                    intLinesRead + 1 & ", column " & intColIndex + 1)
                                        End If

                                    Case eCustomSICFileColumns.ScanCenter
                                        ' Do not use this value if both the ScanTime and the TimeTolerance columns were present
                                        If Not blnForceAcquisitionTimeMode Then
                                            If Not Single.TryParse(strSplitLine(intColIndex), .ScanOrAcqTimeCenter) Then
                                                Throw New InvalidCastException(
                                                        "Non-numeric value for the ScanCenter column in row " &
                                                        intLinesRead + 1 & ", column " & intColIndex + 1)
                                            End If
                                        End If

                                    Case eCustomSICFileColumns.ScanTolerance
                                        ' Do not use this value if both the ScanTime and the TimeTolerance columns were present
                                        If Not blnForceAcquisitionTimeMode Then
                                            If mCustomSICList.ScanToleranceType = clsCustomSICList.eCustomSICScanTypeConstants.Absolute Then
                                                .ScanOrAcqTimeTolerance = CInt(strSplitLine(intColIndex))
                                            Else
                                                ' Includes .Relative and .AcquisitionTime
                                                If Not Single.TryParse(strSplitLine(intColIndex), .ScanOrAcqTimeTolerance) Then
                                                    Throw New InvalidCastException(
                                                            "Non-numeric value for the ScanTolerance column in row " &
                                                            intLinesRead + 1 & ", column " & intColIndex + 1)
                                                End If
                                            End If
                                        End If

                                    Case eCustomSICFileColumns.ScanTime
                                        ' Only use this value if both the ScanTime and the TimeTolerance columns were present
                                        If blnForceAcquisitionTimeMode Then
                                            If Not Single.TryParse(strSplitLine(intColIndex), .ScanOrAcqTimeCenter) Then
                                                Throw New InvalidCastException(
                                                        "Non-numeric value for the ScanTime column in row " &
                                                        intLinesRead + 1 & ", column " & intColIndex + 1)
                                            End If
                                        End If

                                    Case eCustomSICFileColumns.TimeTolerance
                                        ' Only use this value if both the ScanTime and the TimeTolerance columns were present
                                        If blnForceAcquisitionTimeMode Then
                                            If Not Single.TryParse(strSplitLine(intColIndex), .ScanOrAcqTimeTolerance) Then
                                                Throw New InvalidCastException(
                                                        "Non-numeric value for the TimeTolerance column in row " &
                                                        intLinesRead + 1 & ", column " & intColIndex + 1)
                                            End If
                                        End If

                                    Case eCustomSICFileColumns.Comment
                                        .Comment = String.Copy(strSplitLine(intColIndex))
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

            If Not blnForceAcquisitionTimeMode Then
                mCustomSICList.ValidateCustomSICList()
            End If

            Return True

        End Function

    End Class
End Namespace