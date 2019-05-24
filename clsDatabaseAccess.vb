Imports System.IO
Imports MASIC.clsMASIC

Public Class clsDatabaseAccess
    Inherits clsMasicEventNotifier

#Region "Constants and Enums"

    ' frmMain uses these constants

    ' ReSharper disable UnusedMember.Global

    Public Const DATABASE_CONNECTION_STRING_DEFAULT As String = "Data Source=gigasax;Initial Catalog=DMS5;User=DMSReader;Password=dms4fun"
    Public Const DATABASE_DATASET_INFO_QUERY_DEFAULT As String = "Select Dataset, ID FROM V_Dataset_Export"

    ' ReSharper restore UnusedMember.Global

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


    ''' <summary>
    ''' Lookup the dataset ID given the dataset name
    ''' First contacts the database using the specified connection string and query
    ''' If not found, looks for the dataset name in the file specified by mDatasetLookupFilePath
    ''' </summary>
    ''' <param name="inputFilePath"></param>
    ''' <param name="datasetLookupFilePath"></param>
    ''' <param name="defaultDatasetID"></param>
    ''' <returns></returns>
    Public Function LookupDatasetID(
      inputFilePath As String,
      datasetLookupFilePath As String,
      defaultDatasetID As Integer) As Integer

        Dim datasetName = Path.GetFileNameWithoutExtension(inputFilePath)
        Dim newDatasetID = defaultDatasetID

        Dim avoidErrorMessage = "To avoid seeing this message in the future, clear the 'SQL Server Connection String' and " &
                                "'Dataset Info Query SQL' entries on the Advanced tab and save a new settings file. " &
                                "Alternatively, edit a MASIC parameter file to remove the text after the equals sign " &
                                "for parameters ConnectionString and DatasetInfoQuerySql."

        Dim datasetFoundInDB = False

        If Not mOptions.DatabaseConnectionString Is Nothing AndAlso mOptions.DatabaseConnectionString.Length > 0 Then
            ' Attempt to lookup the Dataset ID in the database
            Try
                Dim objDBTools = New PRISM.DBTools(mOptions.DatabaseConnectionString)

                Dim queryingSingleDataset = False

                For iteration = 1 To 2

                    Dim sqlQuery = mOptions.DatasetInfoQuerySql

                    If String.IsNullOrEmpty(sqlQuery) Then
                        sqlQuery = "Select Dataset, ID FROM V_Dataset_Export"
                    End If

                    If sqlQuery.StartsWith("SELECT Dataset", StringComparison.OrdinalIgnoreCase) Then
                        ' Add a where clause to the query
                        If iteration = 1 Then
                            sqlQuery &= " WHERE Dataset = '" & datasetName & "'"
                            queryingSingleDataset = True
                        Else
                            sqlQuery &= " WHERE Dataset Like '" & datasetName & "%'"
                        End If
                    End If

                    Dim lstResults As List(Of List(Of String)) = Nothing

                    Dim success = objDBTools.GetQueryResults(sqlQuery, lstResults, "LookupDatasetID")
                    If success Then

                        ' Find the row in the lstResults that matches fileNameCompare
                        For Each datasetItem In lstResults
                            If String.Equals(datasetItem(0), datasetName, StringComparison.OrdinalIgnoreCase) Then
                                ' Match found
                                If Integer.TryParse(datasetItem(1), newDatasetID) Then
                                    Return newDatasetID
                                Else
                                    ReportError("Error converting Dataset ID '" & datasetItem(1) & "' to an integer", eMasicErrorCodes.InvalidDatasetID)
                                End If
                                Exit For
                            End If
                        Next

                        If lstResults.Count > 0 Then

                            Try
                                If queryingSingleDataset OrElse lstResults.First().Item(0).StartsWith(datasetName) Then
                                    If Integer.TryParse(lstResults.First().Item(1), newDatasetID) Then
                                        Return newDatasetID
                                    End If
                                End If

                            Catch ex As Exception
                                ' Ignore errors here
                            End Try

                        End If

                    End If

                Next

            Catch ex2 As NullReferenceException
                ReportError("Error connecting to database: " & mOptions.DatabaseConnectionString & ControlChars.NewLine & avoidErrorMessage, clsMASIC.eMasicErrorCodes.InvalidDatasetID)
            Catch ex As Exception
                ReportError("Error connecting to database: " & mOptions.DatabaseConnectionString & ControlChars.NewLine & avoidErrorMessage, ex, clsMASIC.eMasicErrorCodes.InvalidDatasetID)
            End Try

        End If

        If Not String.IsNullOrWhiteSpace(datasetLookupFilePath) Then

            ' Lookup the Dataset ID in the dataset lookup file

            Dim delimiterList = New Char() {" "c, ","c, ControlChars.Tab}

            Try
                Using reader = New StreamReader(datasetLookupFilePath)
                    Do While Not reader.EndOfStream
                        Dim dataLine = reader.ReadLine()
                        If String.IsNullOrWhiteSpace(dataLine) Then
                            Continue Do
                        End If

                        If dataLine.Length < datasetName.Length Then
                            Continue Do
                        End If

                        Dim dataValues = dataLine.Split(delimiterList)
                        If dataValues.Length < 2 Then
                            Continue Do
                        End If

                        If Not String.Equals(dataValues(0), datasetName, StringComparison.OrdinalIgnoreCase) Then
                            Continue Do
                        End If

                        If Integer.TryParse(dataValues(1), newDatasetID) Then
                            Return newDatasetID
                        Else
                            ReportError("Error converting Dataset ID '" & dataValues(1) & "' to an integer", eMasicErrorCodes.InvalidDatasetID)
                        End If
                    Loop

                End Using

            Catch ex As Exception
                ReportError("Error reading the dataset lookup file", ex, clsMASIC.eMasicErrorCodes.InvalidDatasetLookupFilePath)
            End Try

        End If

        Return newDatasetID

    End Function

End Class
