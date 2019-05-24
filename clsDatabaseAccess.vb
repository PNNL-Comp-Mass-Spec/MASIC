Imports System.Runtime.InteropServices

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
        Dim newDatasetID As Integer

        ' ReSharper disable once CommentTypo
        ' Data Source=gigasax;Initial Catalog=DMS5;User=DMSReader;Password=...
        If Not String.IsNullOrWhiteSpace(mOptions.DatabaseConnectionString) Then
            Dim datasetFoundInDB = GetDatasetIDFromDatabase(mOptions, datasetName, newDatasetID)
            If datasetFoundInDB Then
                Return newDatasetID
            End If
        End If

        If Not String.IsNullOrWhiteSpace(datasetLookupFilePath) Then
            Dim datasetFoundInFile = GetDatasetIDFromFile(datasetLookupFilePath, datasetName, newDatasetID)
            If datasetFoundInFile Then
                Return newDatasetID
            End If
        End If

        Return defaultDatasetID

    End Function

    ''' <summary>
    ''' Attempt to lookup the Dataset ID in the database
    ''' </summary>
    ''' <param name="masicOptions"></param>
    ''' <param name="datasetName"></param>
    ''' <returns></returns>
    Private Function GetDatasetIDFromDatabase(masicOptions As clsMASICOptions, datasetName As String, <Out> ByRef newDatasetID As Integer) As Boolean

        Dim avoidErrorMessage = "To avoid seeing this message in the future, clear the 'SQL Server Connection String' and " &
                                "'Dataset Info Query SQL' entries on the Advanced tab and save a new settings file. " &
                                "Alternatively, edit a MASIC parameter file to remove the text after the equals sign " &
                                "for parameters ConnectionString and DatasetInfoQuerySql."

        newDatasetID = 0

        Try
            Dim objDBTools = New PRISM.DBTools(masicOptions.DatabaseConnectionString)

            Dim queryingSingleDataset = False

            For iteration = 1 To 2

                Dim sqlQuery = masicOptions.DatasetInfoQuerySql

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

                Dim success = objDBTools.GetQueryResults(sqlQuery, lstResults, "GetDatasetIDFromDatabase")
                If success Then

                    ' Find the row in the lstResults that matches fileNameCompare
                    For Each datasetItem In lstResults
                        If String.Equals(datasetItem(0), datasetName, StringComparison.OrdinalIgnoreCase) Then
                            ' Match found
                            If Integer.TryParse(datasetItem(1), newDatasetID) Then
                                Return True
                            Else
                                ReportError("Error converting Dataset ID '" & datasetItem(1) & "' to an integer", clsMASIC.eMasicErrorCodes.InvalidDatasetID)
                            End If
                            Exit For
                        End If
                    Next

                    If lstResults.Count > 0 Then

                        Try
                            If queryingSingleDataset OrElse lstResults.First().Item(0).StartsWith(datasetName) Then
                                If Integer.TryParse(lstResults.First().Item(1), newDatasetID) Then
                                    Return True
                                End If
                            End If

                        Catch ex As Exception
                            ' Ignore errors here
                        End Try

                    End If

                End If

            Next

            Return False

        Catch ex2 As NullReferenceException
            ReportError("Error connecting to database: " & masicOptions.DatabaseConnectionString & ControlChars.NewLine & avoidErrorMessage, clsMASIC.eMasicErrorCodes.InvalidDatasetID)
            Return False
        Catch ex As Exception
            ReportError("Error connecting to database: " & masicOptions.DatabaseConnectionString & ControlChars.NewLine & avoidErrorMessage, ex, clsMASIC.eMasicErrorCodes.InvalidDatasetID)
            Return False
        End Try

    End Function

    ''' <summary>
    ''' Lookup the dataset ID in the dataset lookup file
    ''' This is a comma, space, or tab delimited file with two columns: Dataset Name and Dataset ID
    ''' </summary>
    ''' <param name="datasetLookupFilePath"></param>
    ''' <param name="datasetName"></param>
    ''' <param name="newDatasetId"></param>
    ''' <returns></returns>
    Private Function GetDatasetIDFromFile(datasetLookupFilePath As String, datasetName As String, <Out> ByRef newDatasetId As Integer) As Boolean

        Dim delimiterList = New Char() {" "c, ","c, ControlChars.Tab}

        newDatasetId = 0

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

                    If Integer.TryParse(dataValues(1), newDatasetId) Then
                        Return True
                    Else
                        ReportError("Error converting Dataset ID '" & dataValues(1) & "' to an integer", clsMASIC.eMasicErrorCodes.InvalidDatasetID)
                    End If
                Loop

            End Using

            Return False

        Catch ex As Exception
            ReportError("Error reading the dataset lookup file", ex, clsMASIC.eMasicErrorCodes.InvalidDatasetLookupFilePath)
            Return False
        End Try

    End Function

End Class
