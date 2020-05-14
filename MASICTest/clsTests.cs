using System;
using System.Collections.Generic;
using MASIC;
using NUnit.Framework;
using PRISM;
using ThermoRawFileReader;

namespace MASICTest
{
    [TestFixture]
    public class clsTests : EventNotifier
    {

        /// <summary>
        /// Test the FilterData method in clsFilterDataArrayMaxCount
        /// </summary>
        [Test]
        public void TestFilterData()
        {
            const int MAX_ALLOWABLE_ION_COUNT = 2000;

            var options = new clsMASICOptions(string.Empty, string.Empty);
            var peakFinder = new MASICPeakFinder.clsMASICPeakFinder();
            var reporterIons = new clsReporterIons();

            var parentIonProcessor = new clsParentIonProcessing(reporterIons);
            var scanTracking = new clsScanTracking(reporterIons, peakFinder);

            var dataImportUtilities = new MASIC.DataInput.clsDataImportThermoRaw(options, peakFinder, parentIonProcessor, scanTracking);

            var mReporterIons = new clsReporterIons();

            var msSpectrum = new clsMSSpectrum(1000);

            for (var mz = 500.0; mz < 1000; mz += 0.1)
            {
                msSpectrum.IonsMZ.Add(mz);
                msSpectrum.IonsIntensity.Add(100 + Math.Sin(mz / 10) * 50 - Math.Abs(750 - mz) / 10);
            }

            dataImportUtilities.DiscardDataToLimitIonCount(msSpectrum,
                mReporterIons.MZIntensityFilterIgnoreRangeStart,
                mReporterIons.MZIntensityFilterIgnoreRangeEnd,
                MAX_ALLOWABLE_ION_COUNT);

            var pointsToCheck = new Dictionary<int, KeyValuePair<double, double>>
            {
                {0, new KeyValuePair<double, double>(508.500, 103.43985)},
                {10, new KeyValuePair<double, double>(509.500, 107.56497)},
                {250, new KeyValuePair<double, double>(574.500, 121.65772)},
                {500, new KeyValuePair<double, double>(637.800, 129.39681)},
                {750, new KeyValuePair<double, double>(698.500, 128.37793)},
                {1000, new KeyValuePair<double, double>(756.800, 113.22312)},
                {1500, new KeyValuePair<double, double>(841.500, 122.01680)},
                {1999, new KeyValuePair<double, double>(968.600, 103.38958)}
            };

            foreach (var item in pointsToCheck)
            {
                var index = item.Key;
                Console.WriteLine(
                    "Data point {0,4}: {1:F4} m/z, {2:F4} counts",
                    item.Key, msSpectrum.IonsMZ[index], msSpectrum.IonsIntensity[index]);

                Assert.AreEqual(item.Value.Key, msSpectrum.IonsMZ[index], 0.00001);
                Assert.AreEqual(item.Value.Value, msSpectrum.IonsIntensity[index], 0.00001);
            }

        }

        [Test]
        public void TestScanConversions()
        {
            const double MZ_MINIMUM = 100;
            const float INTENSITY_MINIMUM = 10000;
            const float SCAN_TIME_SCALAR = 10;

            var scanList = new clsScanList();
            var oRand = new Random();

            var intLastSurveyScanIndexInMasterSeqOrder = -1;

            // Populate scanList with example scan data

            for (var scanNumber = 1; scanNumber <= 1750; scanNumber++)
            {
                if (scanNumber % 10 == 0)
                {
                    // Add a survey scan
                    // If this is a mzXML file that was processed with ReadW, .ScanHeaderText and .ScanTypeName will get updated by UpdateMSXMLScanType
                    var newSurveyScan = new MASIC.clsScanInfo
                    {
                        ScanNumber = scanNumber,
                        ScanTime = scanNumber / SCAN_TIME_SCALAR,
                        ScanHeaderText = string.Empty,
                        ScanTypeName = "MS",
                        BasePeakIonMZ = MZ_MINIMUM + oRand.NextDouble() * 1000,
                        BasePeakIonIntensity = INTENSITY_MINIMUM + (float)oRand.NextDouble() * 1000
                    };

                    // Survey scans typically lead to multiple parent ions; we do not record them here
                    newSurveyScan.FragScanInfo.ParentIonInfoIndex = -1;
                    newSurveyScan.TotalIonIntensity = newSurveyScan.BasePeakIonIntensity * (float)(0.25 + oRand.NextDouble() * 5);

                    // Determine the minimum positive intensity in this scan
                    newSurveyScan.MinimumPositiveIntensity = INTENSITY_MINIMUM;

                    // If this is a mzXML file that was processed with ReadW, then these values will get updated by UpdateMSXMLScanType
                    newSurveyScan.ZoomScan = false;
                    newSurveyScan.SIMScan = false;
                    newSurveyScan.MRMScanType = MRMScanTypeConstants.NotMRM;

                    newSurveyScan.LowMass = MZ_MINIMUM;
                    newSurveyScan.HighMass = Math.Max(newSurveyScan.BasePeakIonMZ * 1.1, MZ_MINIMUM * 10);
                    newSurveyScan.IsFTMS = false;

                    scanList.SurveyScans.Add(newSurveyScan);

                    var intLastSurveyScanIndex = scanList.SurveyScans.Count - 1;

                    scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan, intLastSurveyScanIndex);
                    intLastSurveyScanIndexInMasterSeqOrder = scanList.MasterScanOrderCount - 1;
                }
                else
                {
                    // If this is a mzXML file that was processed with ReadW, .ScanHeaderText and .ScanTypeName will get updated by UpdateMSXMLScanType
                    var newFragScan = new MASIC.clsScanInfo
                    {
                        ScanNumber = scanNumber,
                        ScanTime = scanNumber / SCAN_TIME_SCALAR,
                        ScanHeaderText = string.Empty,
                        ScanTypeName = "MSn",
                        BasePeakIonMZ = MZ_MINIMUM + oRand.NextDouble() * 1000,
                        BasePeakIonIntensity = INTENSITY_MINIMUM + (float)oRand.NextDouble() * 1000
                    };

                    // 1 for the first MS/MS scan after the survey scan, 2 for the second one, etc.
                    newFragScan.FragScanInfo.FragScanNumber = (scanList.MasterScanOrderCount - 1) - intLastSurveyScanIndexInMasterSeqOrder;
                    newFragScan.FragScanInfo.MSLevel = 2;

                    newFragScan.TotalIonIntensity = newFragScan.BasePeakIonIntensity * (float)(0.25 + oRand.NextDouble() * 2);

                    // Determine the minimum positive intensity in this scan
                    newFragScan.MinimumPositiveIntensity = INTENSITY_MINIMUM;

                    // If this is a mzXML file that was processed with ReadW, then these values will get updated by UpdateMSXMLScanType
                    newFragScan.ZoomScan = false;
                    newFragScan.SIMScan = false;
                    newFragScan.MRMScanType = MRMScanTypeConstants.NotMRM;

                    newFragScan.MRMScanInfo.MRMMassCount = 0;

                    newFragScan.LowMass = MZ_MINIMUM;
                    newFragScan.HighMass = Math.Max(newFragScan.BasePeakIonMZ * 1.1, MZ_MINIMUM * 10);
                    newFragScan.IsFTMS = false;

                    scanList.FragScans.Add(newFragScan);
                    scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.FragScan, scanList.FragScans.Count - 1);
                }
            }

            var scanNumScanConverter = new clsScanNumScanTimeConversion();
            RegisterEvents(scanNumScanConverter);

            // Convert absolute values
            // Scan 500, relative scan 0.5, and the scan at 30 minutes
            TestScanConversionToAbsolute(
                scanList, scanNumScanConverter,
                new KeyValuePair<int, int>(500, 500),
                new KeyValuePair<float, float>(0.5F, 876),
                new KeyValuePair<float, float>(30, 300));

            TestScanConversionToTime(
                scanList, scanNumScanConverter,
                new KeyValuePair<int, int>(500, 50),
                new KeyValuePair<float, float>(0.5F, 87.55F),
                new KeyValuePair<float, float>(30, 30));

            // Convert ranges
            // 50 scans wide, 10% of the run, and 5 minutes
            TestScanConversionToAbsolute(
                scanList, scanNumScanConverter,
                new KeyValuePair<int, int>(50, 50),
                new KeyValuePair<float, float>(0.1F, 176),
                new KeyValuePair<float, float>(5, 50));

            TestScanConversionToTime(
                scanList, scanNumScanConverter,
                new KeyValuePair<int, int>(50, 5),
                new KeyValuePair<float, float>(0.1F, 17.59F),
                new KeyValuePair<float, float>(5, 5));
        }

        /// <summary>
        /// Test ScanOrAcqTimeToAbsolute
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="scanNumScanConverter"></param>
        /// <param name="scanNumber">Absolute scan number</param>
        /// <param name="relativeTime">Relative scan (value between 0 and 1)</param>
        /// <param name="scanTime">Scan time</param>
        private void TestScanConversionToAbsolute(
            clsScanList scanList,
            clsScanNumScanTimeConversion scanNumScanConverter,
            KeyValuePair<int, int> scanNumber,
            KeyValuePair<float, float> relativeTime,
            KeyValuePair<float, float> scanTime)
        {
            try
            {
                // Find the scan number corresponding to each of these values
                float result1 = scanNumScanConverter.ScanOrAcqTimeToAbsolute(scanList, scanNumber.Key, clsCustomSICList.eCustomSICScanTypeConstants.Absolute, false);
                Console.WriteLine(scanNumber.Key + " -> " + result1);
                Assert.AreEqual(scanNumber.Value, result1, 1E-05);

                float result2 = scanNumScanConverter.ScanOrAcqTimeToAbsolute(scanList, relativeTime.Key, clsCustomSICList.eCustomSICScanTypeConstants.Relative, false);
                Console.WriteLine(relativeTime.Key + " -> " + result2);
                Assert.AreEqual(relativeTime.Value, result2, 1E-05);

                float result3 = scanNumScanConverter.ScanOrAcqTimeToAbsolute(scanList, scanTime.Key, clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime, false);
                Console.WriteLine(scanTime.Key + " -> " + result3);
                Assert.AreEqual(scanTime.Value, result3, 1E-05);

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error caught: " + ex.Message);
            }
        }

        /// <summary>
        /// Test ScanOrAcqTimeToAbsolute
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="scanNumScanConverter"></param>
        /// <param name="scanNumber">Absolute scan number</param>
        /// <param name="relativeTime">Relative scan (value between 0 and 1)</param>
        /// <param name="scanTime">Scan time</param>

        public void TestScanConversionToTime(clsScanList scanList, clsScanNumScanTimeConversion scanNumScanConverter, KeyValuePair<int, int> scanNumber, KeyValuePair<float, float> relativeTime, KeyValuePair<float, float> scanTime)
        {
            try
            {
                // Find the scan time corresponding to each of these values
                var result1 = scanNumScanConverter.ScanOrAcqTimeToScanTime(scanList, scanNumber.Key, clsCustomSICList.eCustomSICScanTypeConstants.Absolute, false);
                Console.WriteLine(scanNumber.Key + " -> " + result1 + " minutes");
                Assert.AreEqual(scanNumber.Value, result1, 1E-05);

                var result2 = scanNumScanConverter.ScanOrAcqTimeToScanTime(scanList, relativeTime.Key, clsCustomSICList.eCustomSICScanTypeConstants.Relative, false);
                Console.WriteLine(relativeTime.Key + " -> " + result2 + " minutes");
                Assert.AreEqual(relativeTime.Value, result2, 1E-05);

                var result3 = scanNumScanConverter.ScanOrAcqTimeToScanTime(scanList, scanTime.Key, clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime, false);
                Console.WriteLine(scanTime.Key + " -> " + result3 + " minutes");
                Assert.AreEqual(scanTime.Value, result3, 1E-05);

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error caught: " + ex.Message);
            }
        }

        [Test]
        [TestCase(1.2301, "1.23", 3, 100000)]
        [TestCase(1.2, "1.2", 3, 100000)]
        [TestCase(1.003, "1", 3, 100000)]
        [TestCase(999.995, "999.995", 9, 100000)]
        [TestCase(999.995, "999.995", 8, 100000)]
        [TestCase(999.995, "999.995", 7, 100000)]
        [TestCase(999.995, "999.995", 6, 100000)]
        [TestCase(999.995, "1000", 5, 100000)]
        [TestCase(999.995, "1000", 4, 100000)]
        [TestCase(1000.995, "1001", 3, 100000)]
        [TestCase(1000.995, "1001", 2, 100000)]
        [TestCase(1.003, "1.003", 5, 0)]
        [TestCase(1.23123, "1.2312", 5, 0)]
        [TestCase(12.3123, "12.312", 5, 0)]
        [TestCase(123.123, "123.12", 5, 0)]
        [TestCase(1231.23, "1231.2", 5, 0)]
        [TestCase(12312.3, "12312", 5, 0)]
        [TestCase(123123, "123123", 5, 0)]
        [TestCase(1231234, "1.2312E+06", 5, 0)]
        [TestCase(12312345, "1.2312E+07", 5, 0)]
        [TestCase(123123456, "1.2312E+08", 5, 0)]
        public void TestValueToString(double valueToConvert, string expectedResult, byte digitsOfPrecision, int scientificNotationThreshold)
        {
            string result;
            if (scientificNotationThreshold > 0)
            {
                result = StringUtilities.ValueToString(valueToConvert, digitsOfPrecision, scientificNotationThreshold);
            }
            else
            {
                result = StringUtilities.ValueToString(valueToConvert, digitsOfPrecision);
            }

            Console.WriteLine("{0,-12} -> {1,-12}", valueToConvert, result);
        }
    }
}
