using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MASIC;
using NUnit.Framework;

namespace MASICTest
{
    [TestFixture]
    public class clsBinarySearchTests
    {

        [TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint)]
        [TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint)]
        [TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnNextPoint)]
        public void TestSearchFunctionsInt(clsBinarySearch.eMissingDataModeConstants eMissingDataMode)
        {
            try {
                // Initialize a data list with 20 items
                var dataPoints = new List<int>();
                var maxDataValue = 0;

                for (var index = 0; index <= 19; index++) {
                    maxDataValue = index + Convert.ToInt32(Math.Pow(index, 1.5));
                    dataPoints.Add(maxDataValue);
                }

                dataPoints.Sort();

                // Write the data to disk
                // Note that when running NUnit with resharper the output file path will be of the form
                // C:\Users\username\AppData\Local\JetBrains\Installations\ReSharperPlatformVs14\BinarySearch_Test_Int.txt

                var outputFileName = "BinarySearch_Test_Int" + eMissingDataMode + ".txt";
                using (var srOutFile = new StreamWriter(outputFileName, false)) {

                    srOutFile.WriteLine("Data_Index\tData_Value");
                    for (var index = 0; index <= dataPoints.Count - 1; index++) {
                        srOutFile.WriteLine(index.ToString() + '\t' + dataPoints[index]);
                    }

                    srOutFile.WriteLine();

                    // Write the headers
                    srOutFile.WriteLine("Search Value" + '\t' + "Match Value" + '\t' + "Match Index");

                    var searchValueStart = -10;
                    var searchValueEnd = maxDataValue + 10;

                    // Initialize intSearchResults
                    // Note that keys in searchresults will contain the search values
                    // and the values in searchresults will contain the search results
                    var searchresults = new Dictionary<int, int>();

                    // Search intDataList for each number between intSearchValueStart and intSearchValueEnd
                    for (var dataPointToFind = searchValueStart; dataPointToFind <= searchValueEnd; dataPointToFind++) {
                        var indexMatch = clsBinarySearch.BinarySearchFindNearest(dataPoints.ToArray(), dataPointToFind, dataPoints.Count, eMissingDataMode);
                        searchresults.Add(dataPointToFind, dataPoints[indexMatch]);

                        srOutFile.WriteLine(dataPointToFind + '\t' + dataPoints[indexMatch] + '\t' + indexMatch);
                    }

                    // Verify some of the results
                    switch (eMissingDataMode) {
                        case clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint:
                            Assert.AreEqual(searchresults[10], 8);
                            Assert.AreEqual(searchresults[11], 12);
                            Assert.AreEqual(searchresults[89], 87);
                            Assert.AreEqual(searchresults[90], 87);
                            Assert.AreEqual(searchresults[91], 94);

                            break;
                        case clsBinarySearch.eMissingDataModeConstants.ReturnNextPoint:
                            Assert.AreEqual(searchresults[10], 12);
                            Assert.AreEqual(searchresults[11], 12);
                            Assert.AreEqual(searchresults[12], 12);
                            Assert.AreEqual(searchresults[13], 16);
                            Assert.AreEqual(searchresults[14], 16);

                            break;

                        case clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint:
                            Assert.AreEqual(searchresults[10], 8);
                            Assert.AreEqual(searchresults[11], 8);
                            Assert.AreEqual(searchresults[12], 12);
                            Assert.AreEqual(searchresults[13], 12);
                            Assert.AreEqual(searchresults[14], 12);

                            break;
                    }

                }


            } catch (Exception ex) {
                Console.WriteLine("Error in clsBinarySearch->TestSearchFunctions: " + ex.Message);
            }

        }

        [TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint)]
        [TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint)]
        [TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnNextPoint)]
        public void TestSearchFunctionsDbl(clsBinarySearch.eMissingDataModeConstants eMissingDataMode)
        {
            try {
                // Initialize a data list with 20 items
                var dataPoints = new List<double>();
                double maxDataValue = 0;

                for (var index = 0; index <= 19; index++) {
                    maxDataValue = index + Math.Pow(index, 1.5);
                    dataPoints.Add(maxDataValue);
                }

                dataPoints.Sort();

                // Write the data to disk
                // Note that when running NUnit with resharper the output file path will be of the form
                // C:\Users\username\AppData\Local\JetBrains\Installations\ReSharperPlatformVs14\BinarySearch_Test_Int.txt

                var outputFileName = "BinarySearch_TestDouble" + eMissingDataMode.ToString() + ".txt";
                using (var srOutFile = new StreamWriter(outputFileName, false)) {

                    srOutFile.WriteLine("Data_Index" + '\t' + "Data_Value");
                    for (var index = 0; index <= dataPoints.Count - 1; index++) {
                        srOutFile.WriteLine(index.ToString() + '\t' + dataPoints[index].ToString(CultureInfo.InvariantCulture));
                    }

                    srOutFile.WriteLine();

                    // Write the headers
                    srOutFile.WriteLine("Search Value" + '\t' + "Match Value" + '\t' + "Match Index");

                    var searchValueStart = -10;
                    var searchValueEnd = maxDataValue + 11;

                    // Initialize intSearchResults
                    // Note that keys in searchresults will contain the search values
                    // and the values in searchresults will contain the search results
                    var searchresults = new Dictionary<double, double>();

                    // Search intDataList for each number between intSearchValueStart and intSearchValueEnd
                    for (var index = searchValueStart; index <= searchValueEnd; index++) {
                        var dataPointToFind = Convert.ToDouble(index) + index / 10.0;
                        var indexMatch = clsBinarySearch.BinarySearchFindNearest(dataPoints.ToArray(), dataPointToFind, dataPoints.Count, eMissingDataMode);
                        searchresults.Add(dataPointToFind, dataPoints[indexMatch]);

                        srOutFile.WriteLine(dataPointToFind + '\t' + dataPoints[indexMatch] + '\t' + indexMatch);
                    }

                    // Verify some of the results
                    switch (eMissingDataMode) {
                        case clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint:
                            Assert.AreEqual(searchresults[23.1], 20.69693846, 1E-06);
                            Assert.AreEqual(searchresults[24.2], 25.52025918, 1E-06);
                            Assert.AreEqual(searchresults[25.3], 25.52025918, 1E-06);
                            Assert.AreEqual(searchresults[26.4], 25.52025918, 1E-06);
                            Assert.AreEqual(searchresults[27.5], 25.52025918, 1E-06);
                            Assert.AreEqual(searchresults[28.6], 30.627417, 1E-06);

                            break;
                        case clsBinarySearch.eMissingDataModeConstants.ReturnNextPoint:
                            Assert.AreEqual(searchresults[23.1], 25.52025918, 1E-06);
                            Assert.AreEqual(searchresults[24.2], 25.52025918, 1E-06);
                            Assert.AreEqual(searchresults[25.3], 25.52025918, 1E-06);
                            Assert.AreEqual(searchresults[26.4], 30.627417, 1E-06);
                            Assert.AreEqual(searchresults[27.5], 30.627417, 1E-06);
                            Assert.AreEqual(searchresults[28.6], 30.627417, 1E-06);

                            break;

                        case clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint:
                            Assert.AreEqual(searchresults[23.1], 20.69693846, 1E-06);
                            Assert.AreEqual(searchresults[24.2], 20.69693846, 1E-06);
                            Assert.AreEqual(searchresults[25.3], 20.69693846, 1E-06);
                            Assert.AreEqual(searchresults[26.4], 25.52025918, 1E-06);
                            Assert.AreEqual(searchresults[27.5], 25.52025918, 1E-06);
                            Assert.AreEqual(searchresults[28.6], 25.52025918, 1E-06);
                            Assert.AreEqual(searchresults[29.7], 25.52025918, 1E-06);
                            Assert.AreEqual(searchresults[30.8], 30.627416998, 1E-06);

                            break;
                    }

                }


            } catch (Exception ex) {
                Console.WriteLine("Error in clsBinarySearch->TestSearchFunctions: " + ex.Message);
            }
        }

        private string GetMissingDataModeName(clsBinarySearch.eMissingDataModeConstants eMissingDataMode)
        {
            switch (eMissingDataMode) {
                case clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint:
                    return "Return Closest Point";
                case clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint:
                    return "Return Previous Point";
                case clsBinarySearch.eMissingDataModeConstants.ReturnNextPoint:
                    return "Return Next Point";
                default:
                    return "Unknown mode";
            }
        }

    }
}
