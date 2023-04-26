// -------------------------------------------------------------------------------
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
// Program started May 7, 2009
// Ported from clsMASICScanStatsParser to clsDatasetStatsSummarizer in February 2010
//
// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
// Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics
// -------------------------------------------------------------------------------
//
// Licensed under the 2-Clause BSD License; you may not use this file except
// in compliance with the License.  You may obtain a copy of the License at
// https://opensource.org/licenses/BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using PRISM;

// ReSharper disable UnusedMember.Global

namespace MASIC.DatasetStats
{
    /// <summary>
    /// Dataset stats summarizer
    /// </summary>
    /// <remarks>
    /// This class computes aggregate stats for a dataset
    /// </remarks>
    public class DatasetStatsSummarizer : EventNotifier
    {
        // Ignore Spelling: AcqTime, yyyy-MM-dd, hh:mm:ss tt

        /// <summary>
        /// Scan type stats separation character
        /// </summary>
        public const string SCAN_TYPE_STATS_SEP_CHAR = "::###::";

        /// <summary>
        /// Dataset info file suffix
        /// </summary>
        public const string DATASET_INFO_FILE_SUFFIX = "_DatasetInfo.xml";

        /// <summary>
        /// Date/time format string
        /// </summary>
        public const string DATE_TIME_FORMAT_STRING = "yyyy-MM-dd hh:mm:ss tt";

        private struct SummaryStatsStatus
        {
            public bool UpToDate;
            public bool ScanFiltersIncludePrecursorMZValues;
        }

        private string mDatasetStatsSummaryFileName;

        private readonly List<ScanStatsEntry> mDatasetScanStats;

        private DatasetSummaryStats mDatasetSummaryStats;

        private SummaryStatsStatus mDatasetStatsSummaryStatus;

        /// <summary>
        /// Dataset stats summary filename
        /// </summary>
        public string DatasetStatsSummaryFileName
        {
            get => mDatasetStatsSummaryFileName;
            set
            {
                if (value != null)
                {
                    mDatasetStatsSummaryFileName = value;
                }
            }
        }

        /// <summary>
        /// Dataset file info
        /// </summary>
        public DatasetFileInfo DatasetFileInfo { get; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Dataset file modification time
        /// </summary>
        public string FileDate { get; }

        /// <summary>
        /// Sample info
        /// </summary>
        public SampleInfo SampleInfo { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public DatasetStatsSummarizer()
        {
            FileDate = "August 14, 2020";

            ErrorMessage = string.Empty;

            mDatasetScanStats = new List<ScanStatsEntry>();
            mDatasetSummaryStats = new DatasetSummaryStats();

            mDatasetStatsSummaryStatus.UpToDate = false;
            mDatasetStatsSummaryStatus.ScanFiltersIncludePrecursorMZValues = false;

            DatasetFileInfo = new DatasetFileInfo();
            SampleInfo = new SampleInfo();

            ClearCachedData();
        }

        /// <summary>
        /// Add a new scan
        /// </summary>
        /// <param name="scanStats"></param>
        public void AddDatasetScan(ScanStatsEntry scanStats)
        {
            mDatasetScanStats.Add(scanStats);
            mDatasetStatsSummaryStatus.UpToDate = false;
        }

        private double AssureNumeric(double value)
        {
            if (double.IsNaN(value))
                return 0;

            if (double.IsPositiveInfinity(value))
                return double.MaxValue;

            if (double.IsNegativeInfinity(value))
                return double.MinValue;

            return value;
        }

        /// <summary>
        /// Clear cached data
        /// </summary>
        public void ClearCachedData()
        {
            mDatasetScanStats.Clear();
            mDatasetSummaryStats.Clear();

            DatasetFileInfo.Clear();
            SampleInfo.Clear();

            mDatasetStatsSummaryStatus.UpToDate = false;
            mDatasetStatsSummaryStatus.ScanFiltersIncludePrecursorMZValues = false;
        }

        /// <summary>
        /// Summarizes the scan info in scanStats()
        /// </summary>
        /// <param name="scanStats">ScanStats data to parse</param>
        /// <param name="includePrecursorMZ">
        /// When true, include precursor m/z values in the generic scan filters
        /// When false, replace the actual precursor m/z with 0
        /// </param>
        /// <param name="summaryStats">Output: summarized scan stats</param>
        /// <returns>>True if success, false if error</returns>
        public bool ComputeScanStatsSummary(
            List<ScanStatsEntry> scanStats,
            bool includePrecursorMZ,
            out DatasetSummaryStats summaryStats)
        {
            summaryStats = new DatasetSummaryStats();

            try
            {
                if (scanStats == null)
                {
                    ReportError("scanStats is Nothing; unable to continue in ComputeScanStatsSummary");
                    return false;
                }

                ErrorMessage = string.Empty;

                var scanStatsCount = scanStats.Count;

                // Initialize the TIC and BPI Lists
                var ticListMS = new List<double>(scanStatsCount);
                var ticListMSn = new List<double>(scanStatsCount);

                var bpiListMS = new List<double>(scanStatsCount);
                var bpiListMSn = new List<double>(scanStatsCount);

                foreach (var statEntry in scanStats)
                {
                    var genericScanFilter = includePrecursorMZ
                        ? statEntry.ScanFilterText
                        : ThermoRawFileReader.XRawFileIO.GetScanFilterWithGenericPrecursorMZ(statEntry.ScanFilterText);

                    if (statEntry.ScanType > 1)
                    {
                        // MSn spectrum
                        ComputeScanStatsUpdateDetails(
                            statEntry,
                            summaryStats,
                            summaryStats.MSnStats,
                            ticListMSn,
                            bpiListMSn);
                    }
                    else
                    {
                        // MS spectrum
                        ComputeScanStatsUpdateDetails(
                            statEntry,
                            summaryStats,
                            summaryStats.MSStats,
                            ticListMS,
                            bpiListMS);
                    }

                    var scanTypeKey = statEntry.ScanTypeName + SCAN_TYPE_STATS_SEP_CHAR + statEntry.ScanFilterText;
                    if (summaryStats.ScanTypeStats.ContainsKey(scanTypeKey))
                    {
                        summaryStats.ScanTypeStats[scanTypeKey]++;
                    }
                    else
                    {
                        summaryStats.ScanTypeStats.Add(scanTypeKey, 1);
                    }

                    if (statEntry.IsDIA)
                    {
                        summaryStats.DIAScanCount++;
                    }
                }

                summaryStats.MSStats.TICMedian = AssureNumeric(MathNet.Numerics.Statistics.Statistics.Median(ticListMS));
                summaryStats.MSStats.BPIMedian = AssureNumeric(MathNet.Numerics.Statistics.Statistics.Median(bpiListMS));

                summaryStats.MSnStats.TICMedian = AssureNumeric(MathNet.Numerics.Statistics.Statistics.Median(ticListMSn));
                summaryStats.MSnStats.BPIMedian = AssureNumeric(MathNet.Numerics.Statistics.Statistics.Median(bpiListMSn));

                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error in ComputeScanStatsSummary", ex);
                return false;
            }
        }

        private void ComputeScanStatsUpdateDetails(
            ScanStatsEntry scanStats,
            DatasetSummaryStats summaryStats,
            SummaryStatDetails summaryStatDetails,
            ICollection<double> ticList,
            ICollection<double> bpiList)
        {
            if (!string.IsNullOrWhiteSpace(scanStats.ElutionTime))
            {
                if (double.TryParse(scanStats.ElutionTime, out var elutionTime))
                {
                    if (elutionTime > summaryStats.ElutionTimeMax)
                    {
                        summaryStats.ElutionTimeMax = elutionTime;
                    }
                }
            }

            if (double.TryParse(scanStats.TotalIonIntensity, out var totalIonCurrent))
            {
                if (totalIonCurrent > summaryStatDetails.TICMax)
                {
                    summaryStatDetails.TICMax = totalIonCurrent;
                }

                ticList.Add(totalIonCurrent);
            }

            if (double.TryParse(scanStats.BasePeakIntensity, out var basePeakIntensity))
            {
                if (basePeakIntensity > summaryStatDetails.BPIMax)
                {
                    summaryStatDetails.BPIMax = basePeakIntensity;
                }

                bpiList.Add(basePeakIntensity);
            }

            summaryStatDetails.ScanCount++;
        }

        /// <summary>
        /// Creates an XML file summarizing the data stored in this class (in mDatasetScanStats, this.DatasetFileInfo, and this.SampleInfo)
        /// </summary>
        /// <param name="datasetName">Dataset Name</param>
        /// <param name="datasetInfoFilePath">File path to write the XML to</param>
        /// <returns>True if success; False if failure</returns>
        public bool CreateDatasetInfoFile(string datasetName, string datasetInfoFilePath)
        {
            return CreateDatasetInfoFile(datasetName, datasetInfoFilePath, mDatasetScanStats, DatasetFileInfo, SampleInfo);
        }

        /// <summary>
        /// Creates an XML file summarizing the data in scanStats and datasetInfo
        /// </summary>
        /// <param name="datasetName">Dataset Name</param>
        /// <param name="datasetInfoFilePath">File path to write the XML to</param>
        /// <param name="scanStats">Scan stats to parse</param>
        /// <param name="datasetInfo">Dataset Info</param>
        /// <param name="sampleInfo">Sample Info</param>
        /// <returns>True if success; False if failure</returns>
        public bool CreateDatasetInfoFile(
            string datasetName,
            string datasetInfoFilePath,
            List<ScanStatsEntry> scanStats,
            DatasetFileInfo datasetInfo,
            SampleInfo sampleInfo)
        {
            try
            {
                if (scanStats == null)
                {
                    ReportError("scanStats is Nothing; unable to continue in CreateDatasetInfoFile");
                    return false;
                }

                ErrorMessage = string.Empty;

                // If CreateDatasetInfoXML() used a StringBuilder to cache the XML data, we would have to use System.Encoding.Unicode
                // However, CreateDatasetInfoXML() now uses a MemoryStream, so we're able to use UTF8
                using var writer = new StreamWriter(new FileStream(datasetInfoFilePath, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8);

                writer.WriteLine(CreateDatasetInfoXML(datasetName, scanStats, datasetInfo, sampleInfo));

                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error in CreateDatasetInfoFile", ex);
                return false;
            }
        }

        /// <summary>
        /// Creates XML summarizing the data stored in this class (in mDatasetScanStats, this.DatasetFileInfo, and this.SampleInfo)
        /// Auto-determines the dataset name using this.DatasetFileInfo.DatasetName
        /// </summary>
        /// <returns>XML (as string)</returns>
        public string CreateDatasetInfoXML()
        {
            return CreateDatasetInfoXML(DatasetFileInfo.DatasetName, mDatasetScanStats, DatasetFileInfo, SampleInfo);
        }

        /// <summary>
        /// Creates XML summarizing the data stored in this class (in mDatasetScanStats, this.DatasetFileInfo, and this.SampleInfo)
        /// </summary>
        /// <param name="datasetName">Dataset Name</param>
        /// <returns>XML (as string)</returns>
        public string CreateDatasetInfoXML(string datasetName)
        {
            return CreateDatasetInfoXML(datasetName, mDatasetScanStats, DatasetFileInfo, SampleInfo);
        }

        /// <summary>
        /// Creates XML summarizing the data in scanStats and datasetInfo
        /// Auto-determines the dataset name using datasetInfo.DatasetName
        /// </summary>
        /// <param name="scanStats">Scan stats to parse</param>
        /// <param name="datasetInfo">Dataset Info</param>
        /// <returns>XML (as string)</returns>
        public string CreateDatasetInfoXML(List<ScanStatsEntry> scanStats, DatasetFileInfo datasetInfo)
        {
            return CreateDatasetInfoXML(datasetInfo.DatasetName, scanStats, datasetInfo);
        }

        /// <summary>
        /// Creates XML summarizing the data in scanStats, datasetInfo, and sampleInfo
        /// Auto-determines the dataset name using datasetInfo.DatasetName
        /// </summary>
        /// <param name="scanStats">Scan stats to parse</param>
        /// <param name="datasetInfo">Dataset Info</param>
        /// <param name="sampleInfo">Sample Info</param>
        /// <returns>XML (as string)</returns>
        public string CreateDatasetInfoXML(
            List<ScanStatsEntry> scanStats,
            DatasetFileInfo datasetInfo,
            SampleInfo sampleInfo)
        {
            return CreateDatasetInfoXML(datasetInfo.DatasetName, scanStats, datasetInfo, sampleInfo);
        }

        /// <summary>
        /// Creates XML summarizing the data in scanStats and datasetInfo
        /// </summary>
        /// <param name="datasetName">Dataset Name</param>
        /// <param name="scanStats">Scan stats to parse</param>
        /// <param name="datasetInfo">Dataset Info</param>
        /// <returns>XML (as string)</returns>
        public string CreateDatasetInfoXML(
            string datasetName,
            List<ScanStatsEntry> scanStats,
            DatasetFileInfo datasetInfo)
        {
            return CreateDatasetInfoXML(datasetName, scanStats, datasetInfo, new SampleInfo());
        }

        /// <summary>
        /// Creates XML summarizing the data in scanStats and datasetInfo
        /// </summary>
        /// <param name="datasetName">Dataset Name</param>
        /// <param name="scanStats">Scan stats to parse</param>
        /// <param name="datasetInfo">Dataset Info</param>
        /// <param name="sampleInfo">Sample Info</param>
        /// <returns>XML (as string)</returns>
        public string CreateDatasetInfoXML(
            string datasetName,
            List<ScanStatsEntry> scanStats,
            DatasetFileInfo datasetInfo,
            SampleInfo sampleInfo)
        {
            try
            {
                if (scanStats == null)
                {
                    ReportError("scanStats is Nothing; unable to continue in CreateDatasetInfoXML");
                    return string.Empty;
                }

                ErrorMessage = string.Empty;

                DatasetSummaryStats summaryStats;

                // This is true in MASIC, but false in MS_File_Info_Scanner
                const bool includePrecursorMZ = true;

                if (scanStats == mDatasetScanStats)
                {
                    summaryStats = GetDatasetSummaryStats(includePrecursorMZ);
                }
                else
                {
                    // Parse the data in scanStats to compute the bulk values
                    ComputeScanStatsSummary(scanStats, includePrecursorMZ, out summaryStats);
                }

                var xmlSettings = new XmlWriterSettings
                {
                    CheckCharacters = true,
                    Indent = true,
                    IndentChars = "  ",
                    Encoding = Encoding.UTF8,
                    CloseOutput = false        // Do not close output automatically so that the MemoryStream can be read after the XmlWriter has been closed
                };

                // We could cache the text using a StringBuilder, like this:
                //
                // var datasetInfo = new StringBuilder();
                // var stringWriter = new StringWriter(datasetInfo);
                // var writer = new XmlTextWriter(stringWriter)
                // {
                //     Formatting = Formatting.Indented,
                //     Indentation = 2
                // };

                // However, when you send the output to a StringBuilder it is always encoded as Unicode (UTF-16)
                // since this is the only character encoding used in the .NET Framework for String values,
                // and thus you'll see the attribute encoding="UTF-16" in the opening XML declaration

                // The alternative is to use a MemoryStream.  Here, the stream encoding is set by the XmlWriter
                // and so you see the attribute encoding="UTF-8" in the opening XML declaration encoding
                // (since we used xmlSettings.Encoding = Encoding.UTF8)
                //
                var memStream = new MemoryStream();
                var writer = XmlWriter.Create(memStream, xmlSettings);

                writer.WriteStartDocument(true);

                // Write the beginning of the "Root" element.
                writer.WriteStartElement("DatasetInfo");

                writer.WriteElementString("Dataset", datasetName);

                writer.WriteStartElement("ScanTypes");

                foreach (var scanTypeEntry in summaryStats.ScanTypeStats)
                {
                    var scanCountForType = GetScanTypeAndFilter(scanTypeEntry, out var scanType, out var genericScanFilter);

                    writer.WriteStartElement("ScanType");
                    writer.WriteAttributeString("ScanCount", scanCountForType.ToString());
                    writer.WriteAttributeString("ScanFilterText", FixNull(genericScanFilter));
                    writer.WriteString(scanType);
                    writer.WriteEndElement();     // ScanType
                }

                writer.WriteEndElement();       // ScanTypes

                writer.WriteStartElement("AcquisitionInfo");

                var scanCountTotal = summaryStats.MSStats.ScanCount + summaryStats.MSnStats.ScanCount;

                if (scanCountTotal == 0 && datasetInfo.ScanCount > 0)
                {
                    scanCountTotal = datasetInfo.ScanCount;
                }

                writer.WriteElementString("ScanCount", scanCountTotal.ToString());

                writer.WriteElementString("ScanCountMS", summaryStats.MSStats.ScanCount.ToString());
                writer.WriteElementString("ScanCountMSn", summaryStats.MSnStats.ScanCount.ToString());
                writer.WriteElementString("ScanCountDIA", summaryStats.DIAScanCount.ToString());

                writer.WriteElementString("Elution_Time_Max", summaryStats.ElutionTimeMax.ToString("0.0###"));

                writer.WriteElementString("AcqTimeMinutes", datasetInfo.AcqTimeEnd.Subtract(datasetInfo.AcqTimeStart).TotalMinutes.ToString("0.00"));
                writer.WriteElementString("StartTime", datasetInfo.AcqTimeStart.ToString(DATE_TIME_FORMAT_STRING));
                writer.WriteElementString("EndTime", datasetInfo.AcqTimeEnd.ToString(DATE_TIME_FORMAT_STRING));

                writer.WriteElementString("FileSizeBytes", datasetInfo.FileSizeBytes.ToString());

                writer.WriteEndElement();       // AcquisitionInfo

                writer.WriteStartElement("TICInfo");
                writer.WriteElementString("TIC_Max_MS", ValueToString(summaryStats.MSStats.TICMax, 5));
                writer.WriteElementString("TIC_Max_MSn", ValueToString(summaryStats.MSnStats.TICMax, 5));
                writer.WriteElementString("BPI_Max_MS", ValueToString(summaryStats.MSStats.BPIMax, 5));
                writer.WriteElementString("BPI_Max_MSn", ValueToString(summaryStats.MSnStats.BPIMax, 5));
                writer.WriteElementString("TIC_Median_MS", ValueToString(summaryStats.MSStats.TICMedian, 5));
                writer.WriteElementString("TIC_Median_MSn", ValueToString(summaryStats.MSnStats.TICMedian, 5));
                writer.WriteElementString("BPI_Median_MS", ValueToString(summaryStats.MSStats.BPIMedian, 5));
                writer.WriteElementString("BPI_Median_MSn", ValueToString(summaryStats.MSnStats.BPIMedian, 5));
                writer.WriteEndElement();       // TICInfo

                // Only write the SampleInfo block if sampleInfo contains entries
                if (sampleInfo.HasData())
                {
                    writer.WriteStartElement("SampleInfo");
                    writer.WriteElementString("SampleName", FixNull(sampleInfo.SampleName));
                    writer.WriteElementString("Comment1", FixNull(sampleInfo.Comment1));
                    writer.WriteElementString("Comment2", FixNull(sampleInfo.Comment2));
                    writer.WriteEndElement();       // SampleInfo
                }

                writer.WriteEndElement();  // End the "Root" element (DatasetInfo)
                writer.WriteEndDocument(); // End the document

                writer.Close();

                // Now Rewind the memory stream and output as a string
                memStream.Position = 0;
                var reader = new StreamReader(memStream);

                // Return the XML as text
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                ReportError("Error in CreateDatasetInfoXML", ex);
            }

            // This code will only be reached if an exception occurs
            return string.Empty;
        }

        /// <summary>
        /// Creates a tab-delimited text file with details on each scan tracked by this class (stored in mDatasetScanStats)
        /// </summary>
        /// <param name="scanStatsFilePath">File path to write the text file to</param>
        /// <returns>True if success; False if failure</returns>
        public bool CreateScanStatsFile(string scanStatsFilePath)
        {
            return CreateScanStatsFile(scanStatsFilePath, mDatasetScanStats);
        }

        /// <summary>
        /// Creates a tab-delimited text file with details on each scan tracked by this class (stored in mDatasetScanStats)
        /// </summary>
        /// <param name="scanStatsFilePath">File path to write the text file to</param>
        /// <param name="scanStats">Scan stats to parse</param>
        /// <returns>True if success; False if failure</returns>
        public bool CreateScanStatsFile(
            string scanStatsFilePath,
            List<ScanStatsEntry> scanStats)
        {
            const int DATASET_ID = 0;

            try
            {
                if (scanStats == null)
                {
                    ReportError("scanStats is Nothing; unable to continue in CreateScanStatsFile");
                    return false;
                }

                ErrorMessage = string.Empty;

                using var scanStatsWriter = new StreamWriter(new FileStream(scanStatsFilePath, FileMode.Create, FileAccess.Write, FileShare.Read));

                // Write the headers
                var headerNames = new List<string>
                {
                    "Dataset",
                    "ScanNumber",
                    "ScanTime",
                    "ScanType",
                    "TotalIonIntensity",
                    "BasePeakIntensity",
                    "BasePeakMZ",
                    "BasePeakSignalToNoiseRatio",
                    "IonCount",
                    "IonCountRaw",
                    "ScanTypeName"
                };

                scanStatsWriter.WriteLine(string.Join("\t", headerNames));

                var dataValues = new List<string>(12);

                foreach (var scanStatsEntry in scanStats)
                {
                    dataValues.Clear();

                    // Dataset ID
                    dataValues.Add(DATASET_ID.ToString());

                    // Scan number
                    dataValues.Add(scanStatsEntry.ScanNumber.ToString());

                    // Scan time (minutes)
                    dataValues.Add(scanStatsEntry.ElutionTime);

                    // Scan type (1 for MS, 2 for MS2, etc.)
                    dataValues.Add(scanStatsEntry.ScanType.ToString());

                    // Total ion intensity
                    dataValues.Add(scanStatsEntry.TotalIonIntensity);

                    // Base peak ion intensity
                    dataValues.Add(scanStatsEntry.BasePeakIntensity);

                    // Base peak ion m/z
                    dataValues.Add(scanStatsEntry.BasePeakMZ);

                    // Base peak signal to noise ratio
                    dataValues.Add(scanStatsEntry.BasePeakSignalToNoiseRatio);

                    // Number of peaks (aka ions) in the spectrum
                    dataValues.Add(scanStatsEntry.IonCount.ToString());

                    // Number of peaks (aka ions) in the spectrum prior to any filtering
                    dataValues.Add(scanStatsEntry.IonCountRaw.ToString());

                    // Scan type name
                    dataValues.Add(scanStatsEntry.ScanTypeName);

                    scanStatsWriter.WriteLine(string.Join("\t", dataValues));
                }

                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error in CreateScanStatsFile", ex);
                return false;
            }
        }

        private string FixNull(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text;
        }

        /// <summary>
        /// Get dataset summary stats
        /// </summary>
        /// <param name="includePrecursorMZ">
        /// When true, include precursor m/z values in the generic scan filters
        /// When false, replace the actual precursor m/z with 0
        /// </param>
        public DatasetSummaryStats GetDatasetSummaryStats(bool includePrecursorMZ)
        {
            if (mDatasetStatsSummaryStatus.UpToDate && mDatasetStatsSummaryStatus.ScanFiltersIncludePrecursorMZValues == includePrecursorMZ)
                return mDatasetSummaryStats;

            ComputeScanStatsSummary(mDatasetScanStats, includePrecursorMZ, out mDatasetSummaryStats);

            mDatasetStatsSummaryStatus.UpToDate = true;
            mDatasetStatsSummaryStatus.ScanFiltersIncludePrecursorMZValues = includePrecursorMZ;

            return mDatasetSummaryStats;
        }

        /// <summary>
        /// Extract out the scan type and filter text from the key in scanTypeEntry
        /// </summary>
        /// <param name="scanTypeEntry">Key is scan type, value is number of scans with the given scan type</param>
        /// <param name="scanType">Scan Type, e.g. HMS or HCD-HMSn or DIA-HCD-HMSn</param>
        /// <param name="scanFilterText">Scan filter text, e.g. "FTMS + p NSI Full ms" or "FTMS + p NSI d Full ms2 0@hcd25.00" or "IMS"</param>
        /// <returns>Scan count for this scan type and filter string</returns>
        private int GetScanTypeAndFilter(
            KeyValuePair<string, int> scanTypeEntry,
            out string scanType,
            out string scanFilterText)
        {
            var scanTypeKey = scanTypeEntry.Key;
            var indexMatch = scanTypeKey.IndexOf(SCAN_TYPE_STATS_SEP_CHAR, StringComparison.Ordinal);

            if (indexMatch >= 0)
            {
                scanFilterText = scanTypeKey.Substring(indexMatch + SCAN_TYPE_STATS_SEP_CHAR.Length);

                if (indexMatch > 0)
                {
                    scanType = scanTypeKey.Substring(0, indexMatch);
                }
                else
                {
                    scanType = string.Empty;
                }
            }
            else
            {
                scanType = scanTypeKey;
                scanFilterText = string.Empty;
            }

            return scanTypeEntry.Value;
        }

        private void ReportError(string message, Exception ex = null)
        {
            if (ex is null)
            {
                ErrorMessage = message;
            }
            else
            {
                ErrorMessage = message + ": " + ex.Message;
            }

            OnErrorEvent(message, ex);
        }

        /// <summary>
        /// Updates the scan type information for the specified scan number
        /// </summary>
        /// <param name="scanNumber"></param>
        /// <param name="scanType"></param>
        /// <param name="scanTypeName"></param>
        /// <returns>True if the scan was found and updated; otherwise false</returns>
        public bool UpdateDatasetScanType(int scanNumber, int scanType, string scanTypeName)
        {
            var matchFound = false;

            // Look for scanNumber in mDatasetScanStats
            foreach (var scan in mDatasetScanStats)
            {
                if (scan.ScanNumber != scanNumber)
                    continue;

                scan.ScanType = scanType;
                scan.ScanTypeName = scanTypeName;
                mDatasetStatsSummaryStatus.UpToDate = false;

                matchFound = true;
                break;
            }

            return matchFound;
        }

        /// <summary>
        /// Updates a tab-delimited text file, adding a new line summarizing the data stored in this class (in mDatasetScanStats and this.DatasetFileInfo)
        /// </summary>
        /// <param name="datasetName">Dataset Name</param>
        /// <param name="datasetInfoFilePath">File path to write the XML to</param>
        /// <returns>True if success; False if failure</returns>
        public bool UpdateDatasetStatsTextFile(string datasetName, string datasetInfoFilePath)
        {
            return UpdateDatasetStatsTextFile(datasetName, datasetInfoFilePath, mDatasetScanStats, DatasetFileInfo, SampleInfo);
        }

        /// <summary>
        /// Updates a tab-delimited text file, adding a new line summarizing the data in scanStats and datasetInfo
        /// This method does not check for duplicate entries; it simply appends a new line
        /// </summary>
        /// <param name="datasetName">Dataset Name</param>
        /// <param name="datasetStatsFilePath">Tab-delimited file to create/update</param>
        /// <param name="scanStats">Scan stats to parse</param>
        /// <param name="datasetInfo">Dataset Info</param>
        /// <param name="sampleInfo">Sample Info</param>
        /// <returns>True if success; False if failure</returns>
        public bool UpdateDatasetStatsTextFile(
            string datasetName,
            string datasetStatsFilePath,
            List<ScanStatsEntry> scanStats,
            DatasetFileInfo datasetInfo,
            SampleInfo sampleInfo)
        {
            var writeHeaders = false;

            try
            {
                if (scanStats == null)
                {
                    ReportError("scanStats is Nothing; unable to continue in UpdateDatasetStatsTextFile");
                    return false;
                }

                ErrorMessage = string.Empty;

                DatasetSummaryStats summaryStats;

                const bool includePrecursorMZ = false;

                if (scanStats == mDatasetScanStats)
                {
                    summaryStats = GetDatasetSummaryStats(includePrecursorMZ);
                }
                else
                {
                    // Parse the data in scanStats to compute the bulk values
                    var summarySuccess = ComputeScanStatsSummary(scanStats, includePrecursorMZ, out summaryStats);

                    if (!summarySuccess)
                    {
                        ReportError("ComputeScanStatsSummary returned false; unable to continue in UpdateDatasetStatsTextFile");
                        return false;
                    }
                }

                if (!File.Exists(datasetStatsFilePath))
                {
                    writeHeaders = true;
                }

                OnDebugEvent("Updating {0}", datasetStatsFilePath);

                // Create or open the output file
                using var writer = new StreamWriter(new FileStream(datasetStatsFilePath, FileMode.Append, FileAccess.Write, FileShare.Read));

                if (writeHeaders)
                {
                    // Write the header line
                    var headerNames = new List<string>
                    {
                        "Dataset",
                        "ScanCount",
                        "ScanCountMS",
                        "ScanCountMSn",
                        "Elution_Time_Max",
                        "AcqTimeMinutes",
                        "StartTime",
                        "EndTime",
                        "FileSizeBytes",
                        "SampleName",
                        "Comment1",
                        "Comment2"
                    };

                    writer.WriteLine(string.Join("\t", headerNames));
                }

                var dataValues = new List<string>
                {
                    datasetName,
                    (summaryStats.MSStats.ScanCount + summaryStats.MSnStats.ScanCount).ToString(),
                    summaryStats.MSStats.ScanCount.ToString(),
                    summaryStats.MSnStats.ScanCount.ToString(),
                    summaryStats.ElutionTimeMax.ToString("0.00"),
                    datasetInfo.AcqTimeEnd.Subtract(datasetInfo.AcqTimeStart).TotalMinutes.ToString("0.00"),
                    datasetInfo.AcqTimeStart.ToString(DATE_TIME_FORMAT_STRING),
                    datasetInfo.AcqTimeEnd.ToString(DATE_TIME_FORMAT_STRING),
                    datasetInfo.FileSizeBytes.ToString(),
                    FixNull(sampleInfo.SampleName),
                    FixNull(sampleInfo.Comment1),
                    FixNull(sampleInfo.Comment2)
                };

                writer.WriteLine(string.Join("\t", dataValues));

                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error in UpdateDatasetStatsTextFile", ex);
                return false;
            }
        }

        private string ValueToString(double value, byte digitsOfPrecision)
        {
            if (double.IsNaN(value))
            {
                return 0.ToString();
            }

            if (double.IsNegativeInfinity(value))
            {
                return StringUtilities.ValueToString(double.MinValue, digitsOfPrecision);
            }

            if (double.IsPositiveInfinity(value))
            {
                return StringUtilities.ValueToString(double.MaxValue, digitsOfPrecision);
            }

            return StringUtilities.ValueToString(value, 5);
        }
    }
}
