Imports MASIC.clsMASIC

Public Class clsDatabaseAccess
    Inherits clsEventNotifier

#Region "Constants and Enums"

    Public Const DATABASE_CONNECTION_STRING_DEFAULT As String = "Data Source=Pogo;Initial Catalog=Prism_IFC;User=mtuser;Password=mt4fun"
    Public Const DATABASE_DATASET_INFO_QUERY_DEFAULT As String = "Select Dataset, ID FROM Prism_IFC..V_DMS_Dataset_Summary"

#End Region

#Region "Classwide Variables"
    Private ReadOnly mOptions As clsMASICOptions
#End Region

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="masicOptions"></param>
    Public Sub New(masicOptions As clsMASICOptions)
        mOptions = masicOptions
    End Sub

    Public Function LookupDatasetNumber(
      strInputFilePath As String,
      strDatasetLookupFilePath As String,
      intDefaultDatasetNumber As Integer) As Integer

        ' First tries to poll the database for the dataset number
        ' If this doesn't work, then looks for the dataset name in mDatasetLookupFilePath

        ' Initialize intNewDatasetNumber and strFileNameCompare
        Dim strFileNameCompare = Path.GetFileNameWithoutExtension(strInputFilePath).ToUpper
        Dim intNewDatasetNumber = intDefaultDatasetNumber

        Dim strAvoidErrorMessage = "To avoid seeing this message in the future, clear the 'SQL Server Connection String' and " &
            "'Dataset Info Query SQL' entries on the Advanced tab."

        Dim blnDatasetFoundInDB = False

        If Not mOptions.DatabaseConnectionString Is Nothing AndAlso mOptions.DatabaseConnectionString.Length > 0 Then
            ' Attempt to lookup the dataset number in the database
            Try
                Dim objDBTools = New PRISM.DataBase.clsDBTools(mOptions.DatabaseConnectionString)

                Dim intTextCol As Integer = -1
                Dim intDatasetIDCol As Integer = -1
                Dim blnQueryingSingleDataset = False

                Dim strQuery = String.Copy(mOptions.DatasetInfoQuerySql)
                If strQuery.ToUpper.StartsWith("SELECT DATASET") Then
                    ' Add a where clause to the query
                    strQuery &= " WHERE Dataset = '" & strFileNameCompare & "'"
                    blnQueryingSingleDataset = True
                End If

                Dim dsDatasetInfo As DataSet = Nothing
                Dim intRowCount As Integer

                If objDBTools.GetDiscDataSet(strQuery, dsDatasetInfo, intRowCount) Then
                    If intRowCount > 0 Then
                        With dsDatasetInfo.Tables(0)
                            If .Columns(0).DataType Is Type.GetType("System.String") Then
                                ' First column is text; make sure the second is a number
                                If Not .Columns(1).DataType Is Type.GetType("System.String") Then
                                    intTextCol = 0
                                    intDatasetIDCol = 1
                                End If
                            Else
                                ' First column is not text; make sure the second is text
                                If .Columns(1).DataType Is Type.GetType("System.String") Then
                                    intTextCol = 1
                                    intDatasetIDCol = 0
                                End If
                            End If
                        End With

                        If intTextCol >= 0 Then
                            ' Find the row in the datatable that matches strFileNameCompare
                            For Each objRow As DataRow In dsDatasetInfo.Tables(0).Rows
                                If CStr(objRow.Item(intTextCol)).ToUpper = strFileNameCompare Then
                                    ' Match found
                                    Try
                                        intNewDatasetNumber = CInt(objRow.Item(intDatasetIDCol))
                                        blnDatasetFoundInDB = True
                                    Catch ex As Exception
                                        Try
                                            ReportError("LookupDatasetNumber", "Error converting '" & objRow.Item(intDatasetIDCol).ToString() & "' to a dataset ID", ex, True, False, eMasicErrorCodes.InvalidDatasetNumber)
                                        Catch ex2 As Exception
                                            ReportError("LookupDatasetNumber", "Error converting column " & intDatasetIDCol.ToString() & " from the dataset report to a dataset ID", ex, True, False, eMasicErrorCodes.InvalidDatasetNumber)
                                        End Try
                                        blnDatasetFoundInDB = False
                                    End Try
                                    Exit For
                                End If
                            Next objRow
                        End If

                        If Not blnDatasetFoundInDB AndAlso blnQueryingSingleDataset Then
                            Try
                                Integer.TryParse(dsDatasetInfo.Tables(0).Rows(0).Item(1).ToString(), intNewDatasetNumber)
                                blnDatasetFoundInDB = True
                            Catch ex As Exception
                                ' Ignore errors here
                            End Try

                        End If
                    End If
                End If

            Catch ex2 As NullReferenceException
                ReportError("LookupDatasetNumber", "Error connecting to database: " & mOptions.DatabaseConnectionString & ControlChars.NewLine & strAvoidErrorMessage, Nothing, True, False, eMasicErrorCodes.InvalidDatasetNumber)
                blnDatasetFoundInDB = False
            Catch ex As Exception
                ReportError("LookupDatasetNumber", "Error connecting to database: " & mOptions.DatabaseConnectionString & ControlChars.NewLine & strAvoidErrorMessage, ex, True, False, eMasicErrorCodes.InvalidDatasetNumber)
                blnDatasetFoundInDB = False
            End Try
        End If

        If Not blnDatasetFoundInDB AndAlso Not String.IsNullOrWhiteSpace(strDatasetLookupFilePath) Then

            ' Lookup the dataset number in the dataset lookup file

            Dim strLineIn As String
            Dim strSplitLine() As String

            Dim strDelimList = New Char() {" "c, ","c, ControlChars.Tab}

            Try
                Using srInFile = New StreamReader(strDatasetLookupFilePath)
                    Do While Not srInFile.EndOfStream
                        strLineIn = srInFile.ReadLine
                        If strLineIn Is Nothing Then Continue Do

                        If strLineIn.Length < strFileNameCompare.Length Then Continue Do

                        If strLineIn.Substring(0, strFileNameCompare.Length).ToUpper() <> strFileNameCompare Then Continue Do

                        strSplitLine = strLineIn.Split(strDelimList)
                        If strSplitLine.Length < 2 Then Continue Do

                        If clsUtilities.IsNumber(strSplitLine(1)) Then
                            intNewDatasetNumber = CInt(strSplitLine(1))
                            Exit Do
                        Else
                            ReportWarning("LookupDatasetNumber", "Invalid dataset number: " & strSplitLine(1))
                            Exit Do
                        End If

                    Loop
                End Using

            Catch ex As Exception
                ReportError("LookupDatasetNumber", "Error reading the dataset lookup file", ex, True, True, eMasicErrorCodes.InvalidDatasetLookupFilePath)
            End Try

        End If

        Return intNewDatasetNumber

    End Function



End Class
