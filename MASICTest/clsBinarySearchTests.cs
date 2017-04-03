Imports System.IO
Imports MASIC
Imports NUnit.Framework

Public Class clsBinarySearchTests

    <TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint)>
    <TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint)>
    <TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnNextPoint)>
    Public Sub TestSearchFunctionsInt(eMissingDataMode As clsBinarySearch.eMissingDataModeConstants)

        Try
            ' Initialize a data list with 20 items
            Dim dataPoints = New List(Of Integer)
            Dim maxDataValue As Integer

            For intIndex = 0 To 19
                maxDataValue = intIndex + CInt(Math.Pow(intIndex, 1.5))
                dataPoints.Add(maxDataValue)
            Next intIndex

            dataPoints.Sort()

            ' Write the data to disk
            ' Note that when running NUnit with resharper the output file path will be of the form
            ' C:\Users\username\AppData\Local\JetBrains\Installations\ReSharperPlatformVs14\BinarySearch_Test_Int.txt

            Dim outputFileName = "BinarySearch_Test_Int" & eMissingDataMode.ToString() & ".txt"
            Using srOutFile = New StreamWriter(outputFileName, False)

                srOutFile.WriteLine("Data_Index" & ControlChars.Tab & "Data_Value")
                For intIndex = 0 To dataPoints.Count - 1
                    srOutFile.WriteLine(intIndex.ToString() & ControlChars.Tab & dataPoints(intIndex).ToString())
                Next intIndex

                srOutFile.WriteLine()

                ' Write the headers
                srOutFile.WriteLine("Search Value" & ControlChars.Tab & "Match Value" & ControlChars.Tab & "Match Index")

                Dim searchValueStart = -10
                Dim searchValueEnd = maxDataValue + 10

                ' Initialize intSearchResults
                ' Note that keys in searchresults will contain the search values
                ' and the values in searchresults will contain the search results
                Dim searchresults As New Dictionary(Of Integer, Integer)

                ' Search intDataList for each number between intSearchValueStart and intSearchValueEnd
                For dataPointToFind = searchValueStart To searchValueEnd
                    Dim indexMatch = clsBinarySearch.BinarySearchFindNearest(dataPoints.ToArray(), dataPointToFind, dataPoints.Count, eMissingDataMode)
                    searchresults.Add(dataPointToFind, dataPoints(indexMatch))

                    srOutFile.WriteLine(dataPointToFind & ControlChars.Tab & dataPoints(indexMatch) & ControlChars.Tab & indexMatch)
                Next

                ' Verify some of the results
                Select Case eMissingDataMode
                    Case clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint
                        Assert.AreEqual(searchresults(10), 8)
                        Assert.AreEqual(searchresults(11), 12)
                        Assert.AreEqual(searchresults(89), 87)
                        Assert.AreEqual(searchresults(90), 87)
                        Assert.AreEqual(searchresults(91), 94)

                    Case clsBinarySearch.eMissingDataModeConstants.ReturnNextPoint
                        Assert.AreEqual(searchresults(10), 12)
                        Assert.AreEqual(searchresults(11), 12)
                        Assert.AreEqual(searchresults(12), 12)
                        Assert.AreEqual(searchresults(13), 16)
                        Assert.AreEqual(searchresults(14), 16)


                    Case clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint
                        Assert.AreEqual(searchresults(10), 8)
                        Assert.AreEqual(searchresults(11), 8)
                        Assert.AreEqual(searchresults(12), 12)
                        Assert.AreEqual(searchresults(13), 12)
                        Assert.AreEqual(searchresults(14), 12)

                End Select

            End Using


        Catch ex As Exception
            Console.WriteLine("Error in clsBinarySearch->TestSearchFunctions: " & ex.Message)
        End Try

    End Sub

    <TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint)>
    <TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint)>
    <TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnNextPoint)>
    Public Sub TestSearchFunctionsDbl(eMissingDataMode As clsBinarySearch.eMissingDataModeConstants)
        Try
            ' Initialize a data list with 20 items
            Dim dataPoints = New List(Of Double)
            Dim maxDataValue As Double

            For intIndex = 0 To 19
                maxDataValue = intIndex + Math.Pow(intIndex, 1.5)
                dataPoints.Add(maxDataValue)
            Next intIndex

            dataPoints.Sort()

            ' Write the data to disk
            ' Note that when running NUnit with resharper the output file path will be of the form
            ' C:\Users\username\AppData\Local\JetBrains\Installations\ReSharperPlatformVs14\BinarySearch_Test_Int.txt

            Dim outputFileName = "BinarySearch_TestDouble" & eMissingDataMode.ToString() & ".txt"
            Using srOutFile = New StreamWriter(outputFileName, False)

                srOutFile.WriteLine("Data_Index" & ControlChars.Tab & "Data_Value")
                For intIndex = 0 To dataPoints.Count - 1
                    srOutFile.WriteLine(intIndex.ToString() & ControlChars.Tab & dataPoints(intIndex).ToString())
                Next intIndex

                srOutFile.WriteLine()

                ' Write the headers
                srOutFile.WriteLine("Search Value" & ControlChars.Tab & "Match Value" & ControlChars.Tab & "Match Index")

                Dim searchValueStart = -10
                Dim searchValueEnd = maxDataValue + 11

                ' Initialize intSearchResults
                ' Note that keys in searchresults will contain the search values
                ' and the values in searchresults will contain the search results
                Dim searchresults As New Dictionary(Of Double, Double)

                ' Search intDataList for each number between intSearchValueStart and intSearchValueEnd
                For intIndex = searchValueStart To searchValueEnd
                    Dim dataPointToFind = CDbl(intIndex) + intIndex / 10.0
                    Dim indexMatch = clsBinarySearch.BinarySearchFindNearest(dataPoints.ToArray(), dataPointToFind, dataPoints.Count, eMissingDataMode)
                    searchresults.Add(dataPointToFind, dataPoints(indexMatch))

                    srOutFile.WriteLine(dataPointToFind & ControlChars.Tab & dataPoints(indexMatch) & ControlChars.Tab & indexMatch)
                Next

                ' Verify some of the results
                Select Case eMissingDataMode
                    Case clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint
                        Assert.AreEqual(searchresults(23.1), 20.69693846, 0.000001)
                        Assert.AreEqual(searchresults(24.2), 25.52025918, 0.000001)
                        Assert.AreEqual(searchresults(25.3), 25.52025918, 0.000001)
                        Assert.AreEqual(searchresults(26.4), 25.52025918, 0.000001)
                        Assert.AreEqual(searchresults(27.5), 25.52025918, 0.000001)
                        Assert.AreEqual(searchresults(28.6), 30.627417, 0.000001)

                    Case clsBinarySearch.eMissingDataModeConstants.ReturnNextPoint
                        Assert.AreEqual(searchresults(23.1), 25.52025918, 0.000001)
                        Assert.AreEqual(searchresults(24.2), 25.52025918, 0.000001)
                        Assert.AreEqual(searchresults(25.3), 25.52025918, 0.000001)
                        Assert.AreEqual(searchresults(26.4), 30.627417, 0.000001)
                        Assert.AreEqual(searchresults(27.5), 30.627417, 0.000001)
                        Assert.AreEqual(searchresults(28.6), 30.627417, 0.000001)


                    Case clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint
                        Assert.AreEqual(searchresults(23.1), 20.69693846, 0.000001)
                        Assert.AreEqual(searchresults(24.2), 20.69693846, 0.000001)
                        Assert.AreEqual(searchresults(25.3), 20.69693846, 0.000001)
                        Assert.AreEqual(searchresults(26.4), 25.52025918, 0.000001)
                        Assert.AreEqual(searchresults(27.5), 25.52025918, 0.000001)
                        Assert.AreEqual(searchresults(28.6), 25.52025918, 0.000001)
                        Assert.AreEqual(searchresults(29.7), 25.52025918, 0.000001)
                        Assert.AreEqual(searchresults(30.8), 30.627416998, 0.000001)

                End Select

            End Using


        Catch ex As Exception
            Console.WriteLine("Error in clsBinarySearch->TestSearchFunctions: " & ex.Message)
        End Try
    End Sub

    Private Function GetMissingDataModeName(eMissingDataMode As clsBinarySearch.eMissingDataModeConstants) As String
        Select Case eMissingDataMode
            Case clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint
                Return "Return Closest Point"
            Case clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint
                Return "Return Previous Point"
            Case clsBinarySearch.eMissingDataModeConstants.ReturnNextPoint
                Return "Return Next Point"
            Case Else
                Return "Unknown mode"
        End Select
    End Function

End Class