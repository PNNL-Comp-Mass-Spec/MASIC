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
        private const string OUTPUT_FOLDER = @"\\proto-2\UnitTest_Files\MASIC\Results";

        [Test]
        [TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint)]
        [TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint)]
        [TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnNextPoint)]
        public void TestSearchFunctionsInt(clsBinarySearch.eMissingDataModeConstants eMissingDataMode)
        {
            try
            {
                // Initialize a data list with 20 items
                var dataPoints = new List<int>();
                var maxDataValue = 0;

                for (var index = 0; index <= 19; index++)
                {
                    maxDataValue = index + Convert.ToInt32(Math.Pow(index, 1.5));
                    dataPoints.Add(maxDataValue);
                }

                dataPoints.Sort();

                // Write the data to disk
                // The output folder will be below \\proto-2\UnitTest_Files\ if that folder exists
                // Otherwise, it will be local, and when running NUnit with resharper the output file path will be of the form
                // C:\Users\username\AppData\Local\JetBrains\Installations\ReSharperPlatformVs15\BinarySearch_Test_Int.txt

                var outputFolderPath = GetOutputFolderPath();
                var outputFile = new FileInfo(Path.Combine(outputFolderPath, "BinarySearch_Test_Int_" + eMissingDataMode + ".txt"));

                using (var srOutFile = new StreamWriter(outputFile.FullName, false))
                {

                    srOutFile.WriteLine("Data_Index" + '\t' + "Data_Value");
                    for (var index = 0; index <= dataPoints.Count - 1; index++)
                    {
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
                    for (var dataPointToFind = searchValueStart; dataPointToFind <= searchValueEnd; dataPointToFind++)
                    {
                        var indexMatch = clsBinarySearch.BinarySearchFindNearest(dataPoints.ToArray(), dataPointToFind, dataPoints.Count, eMissingDataMode);
                        searchresults.Add(dataPointToFind, dataPoints[indexMatch]);

                        srOutFile.WriteLine(dataPointToFind + '\t' + dataPoints[indexMatch] + '\t' + indexMatch);
                    }

                    // Verify some of the results
                    switch (eMissingDataMode)
                    {
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

                Console.WriteLine("Test complete; see file " + outputFile.FullName);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in clsBinarySearch->TestSearchFunctions: " + ex.Message);
            }

        }

        [Test]
        [TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint)]
        [TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnPreviousPoint)]
        [TestCase(clsBinarySearch.eMissingDataModeConstants.ReturnNextPoint)]
        public void TestSearchFunctionsDbl(clsBinarySearch.eMissingDataModeConstants eMissingDataMode)
        {
            try
            {
                // Initialize a data list with 20 items
                var dataPoints = new List<double>();
                double maxDataValue = 0;

                for (var index = 0; index <= 19; index++)
                {
                    maxDataValue = index + Math.Pow(index, 1.5);
                    dataPoints.Add(maxDataValue);
                }

                dataPoints.Sort();

                // Write the data to disk
                // The output folder will be below \\proto-2\UnitTest_Files\ if that folder exists
                // Otherwise, it will be local, and when running NUnit with resharper the output file path will be of the form
                // C:\Users\username\AppData\Local\JetBrains\Installations\ReSharperPlatformVs15\BinarySearch_Test_Int.txt

                var outputFolderPath = GetOutputFolderPath();
                var outputFile = new FileInfo(Path.Combine(outputFolderPath, "BinarySearch_Test_Double_" + eMissingDataMode + ".txt"));

                using (var srOutFile = new StreamWriter(outputFile.FullName, false))
                {

                    srOutFile.WriteLine("Data_Index" + '\t' + "Data_Value");
                    for (var index = 0; index <= dataPoints.Count - 1; index++)
                    {
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
                    for (var index = searchValueStart; index <= searchValueEnd; index++)
                    {
                        var dataPointToFind = Convert.ToDouble(index) + index / 10.0;
                        var indexMatch = clsBinarySearch.BinarySearchFindNearest(dataPoints.ToArray(), dataPointToFind, dataPoints.Count, eMissingDataMode);
                        searchresults.Add(dataPointToFind, dataPoints[indexMatch]);

                        srOutFile.WriteLine(dataPointToFind + '\t' + dataPoints[indexMatch] + '\t' + indexMatch);
                    }

                    // Verify some of the results
                    switch (eMissingDataMode)
                    {
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

                Console.WriteLine("Test complete; see file " + outputFile.FullName);

                var entryAssembly = System.Reflection.Assembly.GetExecutingAssembly();

                Console.WriteLine(entryAssembly);
                Console.WriteLine(@"C:\Users\d3l243\AppData\Local\JetBrains\Installations");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in clsBinarySearch->TestSearchFunctions: " + ex.Message);
            }
        }

        private string GetOutputFolderPath()
        {
            try
            {
                var defaultOutputFolder = new DirectoryInfo(OUTPUT_FOLDER);

                if (defaultOutputFolder.Exists)
                    return defaultOutputFolder.FullName;

                if (defaultOutputFolder.Parent != null && defaultOutputFolder.Parent.Exists)
                {
                    defaultOutputFolder.Create();
                    return defaultOutputFolder.FullName;
                }

                return ".";

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception determining the best output folder path: " + ex.Message);
            }

            return string.Empty;
        }

    }
}
