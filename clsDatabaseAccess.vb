Imports MASIC.clsMASIC

Public Class clsDatabaseAccess
    Inherits clsMasicEventNotifier

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
        Dim strFileNameCompare = Path.GetFileNameWithoutExtension(strInputFilePath).ToUpper()
        Dim intNewDatasetNumber = intDefaultDatasetNumber

        Dim strAvoidErrorMessage = "To avoid seeing this message in the future, clear the 'SQL Server Connection String' and " &
            "'Dataset Info Query SQL' entries on the Advanced tab."

        Dim blnDatasetFoundInDB = False

        If Not mOptions.DatabaseConnectionString Is Nothing AndAlso mOptions.DatabaseConnectionString.Length > 0 Then
            ' Attempt to lookup the dataset number in the database
            Try
                Dim objDBTools = New PRISM.DBTools(mOptions.DatabaseConnectionString)

                Dim blnQueryingSingleDataset = False

                For iteration = 1 To 2

                    Dim sqlQuery = mOptions.DatasetInfoQuerySql

                    If String.IsNullOrEmpty(sqlQuery) Then
                        sqlQuery = "Select Dataset, ID FROM V_Dataset_Export"
                    End If

                    If sqlQuery.ToUpper().StartsWith("SELECT DATASET") Then
                        ' Add a where clause to the query
                        If iteration = 1 Then
                            sqlQuery &= " WHERE Dataset = '" & strFileNameCompare & "'"
                            blnQueryingSingleDataset = True
                        Else
                            sqlQuery &= " WHERE Dataset Like '" & strFileNameCompare & "%'"
                        End If
                    End If

                    Dim lstResults As List(Of List(Of String)) = Nothing

                    Dim success = objDBTools.GetQueryResults(sqlQuery, lstResults, "LookupDatasetNumber")
                    If success Then

                        ' Find the row in the datatable that matches strFileNameCompare
                        For Each datasetItem In lstResults
                            If String.Equals(datasetItem(0), strFileNameCompare, StringComparison.InvariantCultureIgnoreCase) Then
                                ' Match found
                                Try
                                    If Integer.TryParse(datasetItem(1), intNewDatasetNumber) Then
                                        blnDatasetFoundInDB = True
                                    End If

                                Catch ex As Exception
                                    Try
                                        ReportError("Error converting '" & datasetItem(1) & "' to a dataset ID", ex, eMasicErrorCodes.InvalidDatasetNumber)
                                    Catch ex2 As Exception
                                        ReportError("Error converting column 2 from the dataset report to a dataset ID", ex, eMasicErrorCodes.InvalidDatasetNumber)
                                    End Try
                                    blnDatasetFoundInDB = False
                                End Try
                                Exit For
                            End If
                        Next

                        If Not blnDatasetFoundInDB AndAlso lstResults.Count > 0 Then

                            Try
                                If blnQueryingSingleDataset OrElse lstResults.First().Item(0).StartsWith(strFileNameCompare) Then
                                    Integer.TryParse(lstResults.First().Item(1), intNewDatasetNumber)
                                    blnDatasetFoundInDB = True
                                End If

                            Catch ex As Exception
                                ' Ignore errors here
                            End Try

                        End If

                    End If

                    If blnDatasetFoundInDB Then
                        Exit For
                    End If
                Next

            Catch ex2 As NullReferenceException
                ReportError("Error connecting to database: " & mOptions.DatabaseConnectionString & ControlChars.NewLine & strAvoidErrorMessage, eMasicErrorCodes.InvalidDatasetNumber)
                blnDatasetFoundInDB = False
            Catch ex As Exception
                ReportError("Error connecting to database: " & mOptions.DatabaseConnectionString & ControlChars.NewLine & strAvoidErrorMessage, ex, eMasicErrorCodes.InvalidDatasetNumber)
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
                            ReportWarning("Invalid dataset number: " & strSplitLine(1))
                            Exit Do
                        End If

                    Loop
                End Using

            Catch ex As Exception
                ReportError("Error reading the dataset lookup file", ex, eMasicErrorCodes.InvalidDatasetLookupFilePath)
            End Try

        End If

        Return intNewDatasetNumber

    End Function



End Class
