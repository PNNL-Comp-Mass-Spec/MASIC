using System;
using System.Globalization;
using System.IO;
using System.Xml;
using MASIC.Data;
using MASIC.DatasetStats;
using MASIC.Options;
using PRISM;

namespace MASIC.DataOutput
{
    /// <summary>
    /// XML results file writer
    /// </summary>
    public class XMLResultsWriter : MasicEventNotifier
    {
        // ReSharper disable once CommentTypo
        // Ignore Spelling: frag, Da, UnCache, Butterworth, SavitzkyGolay, Zeroes, parention

        private readonly MASICOptions mOptions;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="masicOptions"></param>
        public XMLResultsWriter(MASICOptions masicOptions)
        {
            mOptions = masicOptions;
        }

        /// <summary>
        /// Examines the values in toleranceList
        /// If all empty and/or all 0, returns an empty string
        /// </summary>
        /// <param name="toleranceList">Comma separated list of values</param>
        private string CheckForEmptyToleranceList(string toleranceList)
        {
            var toleranceValues = toleranceList.Split(',');
            var valuesDefined = false;

            foreach (var value in toleranceValues)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (value.Trim() == "0")
                {
                    continue;
                }

                if (double.TryParse(value, out var parsedValue))
                {
                    if (Math.Abs(parsedValue) < double.Epsilon)
                    {
                        continue;
                    }
                }

                valuesDefined = true;
            }

            if (valuesDefined)
            {
                return toleranceList;
            }

            return string.Empty;
        }

        /// <summary>
        /// Save data to an XML file
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="parentIonIndex"></param>
        /// <param name="sicDetails"></param>
        /// <param name="smoothedYDataSubset"></param>
        /// <param name="dataOutputHandler"></param>
        public bool SaveDataToXML(
            ScanList scanList,
            int parentIonIndex,
            SICDetails sicDetails,
            MASICPeakFinder.clsSmoothedYDataSubset smoothedYDataSubset,
            DataOutput dataOutputHandler)
        {
            var lastGoodLoc = "Start";

            try
            {
                // Populate SICDataScanIntervals with the scan intervals between each of the data points in sicDetails.SICScanNumbers
                // The first scan number is given by SICScanIndices(0)

                byte[] SICDataScanIntervals;
                if (sicDetails.SICDataCount == 0)
                {
                    SICDataScanIntervals = new byte[1];
                }
                else
                {
                    SICDataScanIntervals = new byte[sicDetails.SICDataCount];
                    var sicScanNumbers = sicDetails.SICScanNumbers;

                    for (var scanIndex = 1; scanIndex < sicDetails.SICDataCount; scanIndex++)
                    {
                        var scanDelta = sicScanNumbers[scanIndex] - sicScanNumbers[scanIndex - 1];

                        // When storing in SICDataScanIntervals, make sure the Scan Interval is, at most, 255; it will typically be 1 or 4
                        // However, for MRM data, field size can be much larger
                        SICDataScanIntervals[scanIndex] = (byte)Math.Min(byte.MaxValue, scanDelta);
                    }
                }

                var writer = dataOutputHandler.OutputFileHandles.XMLFileForSICs;
                if (writer == null)
                    return false;

                // Initialize the StringBuilder objects
                var intensityDataList = new System.Text.StringBuilder();
                var massDataList = new System.Text.StringBuilder();
                var peakYDataSmoothed = new System.Text.StringBuilder();

                var sicScanIndices = sicDetails.SICScanIndices;

                // ReSharper disable once CommentTypo

                // Write the SIC's and computed peak stats and areas to the XML file for the given parent ion
                for (var fragScanIndex = 0; fragScanIndex < scanList.ParentIons[parentIonIndex].FragScanIndices.Count; fragScanIndex++)
                {
                    lastGoodLoc = "fragScanIndex=" + fragScanIndex;

                    writer.WriteStartElement("ParentIon");
                    writer.WriteAttributeString("Index", parentIonIndex.ToString());             // Parent ion Index
                    writer.WriteAttributeString("FragScanIndex", fragScanIndex.ToString());      // Frag Scan Index

                    lastGoodLoc = "currentParentIon = scanList.ParentIons(parentIonIndex)";
                    var currentParentIon = scanList.ParentIons[parentIonIndex];

                    writer.WriteElementString("MZ", StringUtilities.DblToString(currentParentIon.MZ, 4));

                    if (currentParentIon.SurveyScanIndex >= 0 && currentParentIon.SurveyScanIndex < scanList.SurveyScans.Count)
                    {
                        writer.WriteElementString("SurveyScanNumber", scanList.SurveyScans[currentParentIon.SurveyScanIndex].ScanNumber.ToString());
                    }
                    else
                    {
                        writer.WriteElementString("SurveyScanNumber", "-1");
                    }

                    lastGoodLoc = "Write FragScanNumber";

                    double interferenceScore;

                    if (fragScanIndex < scanList.FragScans.Count)
                    {
                        var currentFragScan = scanList.FragScans[currentParentIon.FragScanIndices[fragScanIndex]];
                        writer.WriteElementString("FragScanNumber", currentFragScan.ScanNumber.ToString());
                        writer.WriteElementString("FragScanTime", currentFragScan.ScanTime.ToString(CultureInfo.InvariantCulture));
                        interferenceScore = currentFragScan.FragScanInfo.InterferenceScore;
                    }
                    else
                    {
                        // Fragmentation scan does not exist
                        writer.WriteElementString("FragScanNumber", "0");
                        writer.WriteElementString("FragScanTime", "0");
                        interferenceScore = 0;
                    }

                    writer.WriteElementString("OptimalPeakApexScanNumber", currentParentIon.OptimalPeakApexScanNumber.ToString());
                    writer.WriteElementString("PeakApexOverrideParentIonIndex", currentParentIon.PeakApexOverrideParentIonIndex.ToString());
                    writer.WriteElementString("CustomSICPeak", currentParentIon.CustomSICPeak.ToString());

                    if (currentParentIon.CustomSICPeak)
                    {
                        writer.WriteElementString("CustomSICPeakComment", currentParentIon.CustomSICPeakComment);
                        writer.WriteElementString("CustomSICPeakMZToleranceDa", currentParentIon.CustomSICPeakMZToleranceDa.ToString(CultureInfo.InvariantCulture));
                        writer.WriteElementString("CustomSICPeakScanTolerance", currentParentIon.CustomSICPeakScanOrAcqTimeTolerance.ToString(CultureInfo.InvariantCulture));
                        writer.WriteElementString("CustomSICPeakScanToleranceType", mOptions.CustomSICList.ScanToleranceType.ToString());
                    }

                    lastGoodLoc = "sicStatsPeak = currentParentIon.SICStats.Peak";
                    var sicStatsPeak = currentParentIon.SICStats.Peak;

                    if (sicDetails.SICScanType == ScanList.ScanTypeConstants.FragScan)
                    {
                        writer.WriteElementString("SICScanType", "FragScan");
                        writer.WriteElementString("PeakScanStart", scanList.FragScans[sicScanIndices[sicStatsPeak.IndexBaseLeft]].ScanNumber.ToString());
                        writer.WriteElementString("PeakScanEnd", scanList.FragScans[sicScanIndices[sicStatsPeak.IndexBaseRight]].ScanNumber.ToString());
                        writer.WriteElementString("PeakScanMaxIntensity", scanList.FragScans[sicScanIndices[sicStatsPeak.IndexMax]].ScanNumber.ToString());
                    }
                    else
                    {
                        writer.WriteElementString("SICScanType", "SurveyScan");
                        writer.WriteElementString("PeakScanStart", scanList.SurveyScans[sicScanIndices[sicStatsPeak.IndexBaseLeft]].ScanNumber.ToString());
                        writer.WriteElementString("PeakScanEnd", scanList.SurveyScans[sicScanIndices[sicStatsPeak.IndexBaseRight]].ScanNumber.ToString());
                        writer.WriteElementString("PeakScanMaxIntensity", scanList.SurveyScans[sicScanIndices[sicStatsPeak.IndexMax]].ScanNumber.ToString());
                    }

                    writer.WriteElementString("PeakIntensity", StringUtilities.ValueToString(sicStatsPeak.MaxIntensityValue, 5));
                    writer.WriteElementString("PeakSignalToNoiseRatio", StringUtilities.ValueToString(sicStatsPeak.SignalToNoiseRatio, 4));
                    writer.WriteElementString("FWHMInScans", sicStatsPeak.FWHMScanWidth.ToString());
                    writer.WriteElementString("PeakArea", StringUtilities.ValueToString(sicStatsPeak.Area, 5));
                    writer.WriteElementString("ShoulderCount", sicStatsPeak.ShoulderCount.ToString());

                    writer.WriteElementString("ParentIonIntensity", StringUtilities.ValueToString(sicStatsPeak.ParentIonIntensity, 5));

                    var noiseStats = sicStatsPeak.BaselineNoiseStats;
                    writer.WriteElementString("PeakBaselineNoiseLevel", StringUtilities.ValueToString(noiseStats.NoiseLevel, 5));
                    writer.WriteElementString("PeakBaselineNoiseStDev", StringUtilities.ValueToString(noiseStats.NoiseStDev, 3));
                    writer.WriteElementString("PeakBaselinePointsUsed", noiseStats.PointsUsed.ToString());
                    writer.WriteElementString("NoiseThresholdModeUsed", ((int)noiseStats.NoiseThresholdModeUsed).ToString());

                    var statMoments = sicStatsPeak.StatisticalMoments;

                    writer.WriteElementString("StatMomentsArea", StringUtilities.ValueToString(statMoments.Area, 5));
                    writer.WriteElementString("CenterOfMassScan", statMoments.CenterOfMassScan.ToString());
                    writer.WriteElementString("PeakStDev", StringUtilities.ValueToString(statMoments.StDev, 3));
                    writer.WriteElementString("PeakSkew", StringUtilities.ValueToString(statMoments.Skew, 4));
                    writer.WriteElementString("PeakKSStat", StringUtilities.ValueToString(statMoments.KSStat, 4));
                    writer.WriteElementString("StatMomentsDataCountUsed", statMoments.DataCountUsed.ToString());

                    writer.WriteElementString("InterferenceScore", StringUtilities.ValueToString(interferenceScore, 4));

                    if (sicDetails.SICScanType == ScanList.ScanTypeConstants.FragScan)
                    {
                        writer.WriteElementString("SICScanStart", scanList.FragScans[sicScanIndices[0]].ScanNumber.ToString());
                    }
                    else
                    {
                        writer.WriteElementString("SICScanStart", scanList.SurveyScans[sicScanIndices[0]].ScanNumber.ToString());
                    }

                    if (mOptions.UseBase64DataEncoding)
                    {
                        // Save scan interval list as base-64 encoded strings
                        lastGoodLoc = "Call SaveDataToXMLEncodeArray with SICScanIntervals";
                        SaveDataToXMLEncodeArray(writer, "SICScanIntervals", SICDataScanIntervals);
                    }
                    else
                    {
                        // Save scan interval list as long list of numbers
                        // There are no tab delimiters, since we require that all
                        // of the SICDataScanInterval values be <= 61
                        // If the interval is <= 9, the interval is stored as a number
                        // For intervals between 10 and 35, uses letters A to Z
                        // For intervals between 36 and 61, uses letters A to Z

                        lastGoodLoc = "Populate scanIntervalList";
                        var scanIntervalList = string.Empty;
                        if (SICDataScanIntervals != null)
                        {
                            for (var scanIntervalIndex = 0; scanIntervalIndex < sicDetails.SICDataCount; scanIntervalIndex++)
                            {
                                if (SICDataScanIntervals[scanIntervalIndex] <= 9)
                                {
                                    scanIntervalList += SICDataScanIntervals[scanIntervalIndex].ToString();
                                }
                                else if (SICDataScanIntervals[scanIntervalIndex] <= 35)
                                {
                                    scanIntervalList += ((char)(SICDataScanIntervals[scanIntervalIndex] + 55)).ToString();     // 55 = -10 + 65
                                }
                                else if (SICDataScanIntervals[scanIntervalIndex] <= 61)
                                {
                                    scanIntervalList += ((char)(SICDataScanIntervals[scanIntervalIndex] + 61)).ToString();     // 61 = -36 + 97
                                }
                                else
                                {
                                    scanIntervalList += "z";
                                }
                            }
                        }

                        writer.WriteElementString("SICScanIntervals", scanIntervalList);
                    }

                    lastGoodLoc = "Write SICPeakIndexStart";
                    writer.WriteElementString("SICPeakIndexStart", currentParentIon.SICStats.Peak.IndexBaseLeft.ToString());
                    writer.WriteElementString("SICPeakIndexEnd", currentParentIon.SICStats.Peak.IndexBaseRight.ToString());
                    writer.WriteElementString("SICDataCount", sicDetails.SICDataCount.ToString());

                    if (mOptions.SICOptions.SaveSmoothedData)
                    {
                        writer.WriteElementString("SICSmoothedYDataIndexStart", smoothedYDataSubset.DataStartIndex.ToString());
                    }

                    if (mOptions.UseBase64DataEncoding)
                    {
                        // Save intensity and mass data lists as base-64 encoded strings
                        // Note that these field names are purposely different than the DataList names used below for comma separated lists
                        lastGoodLoc = "Call SaveDataToXMLEncodeArray with SICIntensityData";
                        SaveDataToXMLEncodeArray(writer, "SICIntensityData", sicDetails.SICIntensitiesAsFloat);

                        lastGoodLoc = "Call SaveDataToXMLEncodeArray with SICMassData";
                        SaveDataToXMLEncodeArray(writer, "SICMassData", sicDetails.SICMassesAsFloat);

                        if (mOptions.SICOptions.SaveSmoothedData)
                        {
                            // Need to copy the data into an array with the correct number of elements
                            var dataArray = new float[smoothedYDataSubset.DataCount];
                            Array.Copy(smoothedYDataSubset.Data, dataArray, smoothedYDataSubset.DataCount);

                            SaveDataToXMLEncodeArray(writer, "SICSmoothedYData", dataArray);
                        }
                    }
                    else
                    {
                        // Save intensity and mass data lists as tab-delimited text list

                        var intensityDataListWritten = false;
                        var massDataListWritten = false;
                        try
                        {
                            lastGoodLoc = "Populate intensityDataList";
                            intensityDataList.Length = 0;
                            massDataList.Length = 0;

                            if (sicDetails.SICDataCount > 0)
                            {
                                foreach (var dataPoint in sicDetails.SICData)
                                {
                                    if (dataPoint.Intensity > 0)
                                    {
                                        intensityDataList.Append(StringUtilities.DblToString(dataPoint.Intensity, 1) + ",");
                                    }
                                    else
                                    {
                                        // Do not output any number if the intensity is 0
                                        intensityDataList.Append(',');
                                    }

                                    if (dataPoint.Mass > 0)
                                    {
                                        massDataList.Append(StringUtilities.DblToString(dataPoint.Mass, 3) + ",");
                                    }
                                    else
                                    {
                                        // Do not output any number if the mass is 0
                                        massDataList.Append(',');
                                    }
                                }

                                // Trim the trailing comma
                                if (intensityDataList[intensityDataList.Length - 1] == ',')
                                {
                                    intensityDataList.Length--;
                                    massDataList.Length--;
                                }
                            }

                            writer.WriteElementString("IntensityDataList", intensityDataList.ToString());
                            intensityDataListWritten = true;

                            writer.WriteElementString("MassDataList", massDataList.ToString());
                            massDataListWritten = true;
                        }
                        catch (OutOfMemoryException)
                        {
                            // Ignore the exception if it is an Out of Memory exception

                            if (!intensityDataListWritten)
                            {
                                writer.WriteElementString("IntensityDataList", string.Empty);
                            }

                            if (!massDataListWritten)
                            {
                                writer.WriteElementString("MassDataList", string.Empty);
                            }
                        }

                        if (mOptions.SICOptions.SaveSmoothedData)
                        {
                            try
                            {
                                lastGoodLoc = "Populate peakYDataSmoothed";
                                peakYDataSmoothed.Length = 0;

                                if (smoothedYDataSubset.Data != null && smoothedYDataSubset.DataCount > 0)
                                {
                                    for (var index = 0; index < smoothedYDataSubset.DataCount; index++)
                                    {
                                        peakYDataSmoothed.Append(Math.Round(smoothedYDataSubset.Data[index]).ToString(CultureInfo.InvariantCulture) + ",");
                                    }

                                    // Trim the trailing comma
                                    peakYDataSmoothed.Length--;
                                }

                                writer.WriteElementString("SmoothedYDataList", peakYDataSmoothed.ToString());
                            }
                            catch (OutOfMemoryException)
                            {
                                // Ignore the exception if this is an Out of Memory exception
                                writer.WriteElementString("SmoothedYDataList", string.Empty);
                            }
                        }
                    }

                    writer.WriteEndElement();
                }
            }
            catch (Exception ex)
            {
                ReportError("Error writing the XML data to the output file; Last good location: " + lastGoodLoc, ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }

        private void SaveDataToXMLEncodeArray(
            XmlWriter writer,
            string elementName,
            byte[] dataArray)
        {
            var encodedValues = MSDataFileReader.clsBase64EncodeDecode.EncodeNumericArray(dataArray, out var precisionBits, out var dataTypeName);

            writer.WriteStartElement(elementName);
            writer.WriteAttributeString("precision", precisionBits.ToString());        // Store the precision, in bits
            writer.WriteAttributeString("type", dataTypeName);
            writer.WriteString(encodedValues);
            writer.WriteEndElement();
        }

        private void SaveDataToXMLEncodeArray(
            XmlWriter writer,
            string elementName,
            float[] dataArray)
        {
            var encodedValues = MSDataFileReader.clsBase64EncodeDecode.EncodeNumericArray(dataArray, out var precisionBits, out var dataTypeName);

            writer.WriteStartElement(elementName);
            writer.WriteAttributeString("precision", precisionBits.ToString());        // Store the precision, in bits
            writer.WriteAttributeString("type", dataTypeName);
            writer.WriteString(encodedValues);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Finalize the output file
        /// </summary>
        /// <param name="dataOutputHandler"></param>
        /// <param name="scanList"></param>
        /// <param name="spectraCache"></param>
        /// <param name="processingStats"></param>
        /// <param name="processingTimeSec"></param>
        public bool XMLOutputFileFinalize(
            DataOutput dataOutputHandler,
            ScanList scanList,
            SpectraCache spectraCache,
            ProcessingStats processingStats,
            float processingTimeSec)
        {
            var writer = dataOutputHandler.OutputFileHandles.XMLFileForSICs;
            if (writer == null)
                return false;

            try
            {
                writer.WriteStartElement("ProcessingStats");
                writer.WriteElementString("CacheEventCount", spectraCache.CacheEventCount.ToString());
                writer.WriteElementString("UnCacheEventCount", spectraCache.UnCacheEventCount.ToString());
                writer.WriteElementString("SpectraPoolHitEventCount", spectraCache.SpectraPoolHitEventCount.ToString());

                writer.WriteElementString("PeakMemoryUsageMB", StringUtilities.DblToString(processingStats.PeakMemoryUsageMB, 1));
                var effectiveSeconds = processingTimeSec - processingStats.TotalProcessingTimeAtStart;
                writer.WriteElementString("TotalProcessingTimeSeconds", StringUtilities.DblToString(effectiveSeconds, 1));

                writer.WriteEndElement();

                if (scanList.ProcessingIncomplete)
                {
                    writer.WriteElementString("ProcessingComplete", "False");
                }
                else
                {
                    writer.WriteElementString("ProcessingComplete", "True");
                }

                writer.WriteEndElement();     // Close out the <SICData> element
                writer.WriteEndDocument();
                writer.Close();
            }
            catch (Exception ex)
            {
                ReportError("Error finalizing the XML output file", ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Initialize the output file
        /// </summary>
        /// <param name="inputFilePathFull"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <param name="dataOutputHandler"></param>
        /// <param name="scanList"></param>
        /// <param name="spectraCache"></param>
        /// <param name="sicOptions"></param>
        /// <param name="binningOptions"></param>
        public bool XMLOutputFileInitialize(
            string inputFilePathFull,
            string outputDirectoryPath,
            DataOutput dataOutputHandler,
            ScanList scanList,
            SpectraCache spectraCache,
            SICOptions sicOptions,
            BinningOptions binningOptions)
        {
            var xmlOutputFilePath = string.Empty;

            try
            {
                xmlOutputFilePath = DataOutput.ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, DataOutput.OutputFileTypeConstants.XMLFile);

                dataOutputHandler.OutputFileHandles.XMLFileForSICs = new XmlTextWriter(xmlOutputFilePath, System.Text.Encoding.UTF8);
                var writer = dataOutputHandler.OutputFileHandles.XMLFileForSICs;
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 1;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("SICData");

                writer.WriteStartElement("ProcessingSummary");
                writer.WriteElementString("DatasetID", sicOptions.DatasetID.ToString());
                writer.WriteElementString("SourceFilePath", inputFilePathFull);

                string lastModTimeText;
                string fileSizeBytes;

                try
                {
                    var inputFileInfo = new FileInfo(inputFilePathFull);
                    var lastModTime = inputFileInfo.LastWriteTime;
                    lastModTimeText = lastModTime.ToShortDateString() + " " + lastModTime.ToShortTimeString();
                    fileSizeBytes = inputFileInfo.Length.ToString();
                }
                catch (Exception)
                {
                    lastModTimeText = string.Empty;
                    fileSizeBytes = "0";
                }

                writer.WriteElementString("SourceFileDateTime", lastModTimeText);
                writer.WriteElementString("SourceFileSizeBytes", fileSizeBytes);

                writer.WriteElementString("MASICProcessingDate", DateTime.Now.ToString(DatasetStatsSummarizer.DATE_TIME_FORMAT_STRING));
                writer.WriteElementString("MASICVersion", mOptions.MASICVersion);
                writer.WriteElementString("MASICPeakFinderDllVersion", mOptions.PeakFinderVersion);
                writer.WriteElementString("ScanCountTotal", scanList.MasterScanOrderCount.ToString());
                writer.WriteElementString("SurveyScanCount", scanList.SurveyScans.Count.ToString());
                writer.WriteElementString("FragScanCount", scanList.FragScans.Count.ToString());
                writer.WriteElementString("SkipMSMSProcessing", mOptions.SkipMSMSProcessing.ToString());

                writer.WriteElementString("ParentIonDecoyMassDa", mOptions.ParentIonDecoyMassDa.ToString("0.0000"));

                writer.WriteEndElement();

                writer.WriteStartElement("MemoryOptions");

                writer.WriteElementString("CacheAlwaysDisabled", spectraCache.DiskCachingAlwaysDisabled.ToString());
                writer.WriteElementString("CacheSpectraToRetainInMemory", spectraCache.CacheSpectraToRetainInMemory.ToString());

                writer.WriteEndElement();

                writer.WriteStartElement("SICOptions");

                // SIC Options

                // "SICToleranceDa" is a legacy parameter; If the SIC tolerance is in PPM, "SICToleranceDa" is the Da tolerance at 1000 m/z
                writer.WriteElementString("SICToleranceDa", ParentIonProcessing.GetParentIonToleranceDa(sicOptions, 1000).ToString("0.0000"));

                writer.WriteElementString("SICTolerance", sicOptions.SICTolerance.ToString("0.0000"));
                writer.WriteElementString("SICToleranceIsPPM", sicOptions.SICToleranceIsPPM.ToString());

                writer.WriteElementString("RefineReportedParentIonMZ", sicOptions.RefineReportedParentIonMZ.ToString());

                writer.WriteElementString("ScanRangeStart", sicOptions.ScanRangeStart.ToString());
                writer.WriteElementString("ScanRangeEnd", sicOptions.ScanRangeEnd.ToString());
                writer.WriteElementString("RTRangeStart", sicOptions.RTRangeStart.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("RTRangeEnd", sicOptions.RTRangeEnd.ToString(CultureInfo.InvariantCulture));

                writer.WriteElementString("CompressMSSpectraData", sicOptions.CompressMSSpectraData.ToString());
                writer.WriteElementString("CompressMSMSSpectraData", sicOptions.CompressMSMSSpectraData.ToString());

                writer.WriteElementString("CompressToleranceDivisorForDa", sicOptions.CompressToleranceDivisorForDa.ToString("0.0"));
                writer.WriteElementString("CompressToleranceDivisorForPPM", sicOptions.CompressToleranceDivisorForPPM.ToString("0.0"));

                writer.WriteElementString("MaxSICPeakWidthMinutesBackward", sicOptions.MaxSICPeakWidthMinutesBackward.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("MaxSICPeakWidthMinutesForward", sicOptions.MaxSICPeakWidthMinutesForward.ToString(CultureInfo.InvariantCulture));

                writer.WriteElementString("IntensityThresholdFractionMax", StringUtilities.DblToString(sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax, 5));
                writer.WriteElementString("IntensityThresholdAbsoluteMinimum", sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum.ToString(CultureInfo.InvariantCulture));

                // Peak Finding Options
                var baselineNoiseOptions = sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions;
                writer.WriteElementString("SICNoiseThresholdMode", baselineNoiseOptions.BaselineNoiseMode.ToString());
                writer.WriteElementString("SICNoiseThresholdIntensity", baselineNoiseOptions.BaselineNoiseLevelAbsolute.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("SICNoiseFractionLowIntensityDataToAverage", StringUtilities.DblToString(baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, 5));
                writer.WriteElementString("SICNoiseMinimumSignalToNoiseRatio", baselineNoiseOptions.MinimumSignalToNoiseRatio.ToString(CultureInfo.InvariantCulture));

                writer.WriteElementString("MaxDistanceScansNoOverlap", sicOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap.ToString());
                writer.WriteElementString("MaxAllowedUpwardSpikeFractionMax", StringUtilities.DblToString(sicOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax, 5));
                writer.WriteElementString("InitialPeakWidthScansScaler", sicOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("InitialPeakWidthScansMaximum", sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum.ToString());

                writer.WriteElementString("FindPeaksOnSmoothedData", sicOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData.ToString());
                writer.WriteElementString("SmoothDataRegardlessOfMinimumPeakWidth", sicOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth.ToString());
                writer.WriteElementString("UseButterworthSmooth", sicOptions.SICPeakFinderOptions.UseButterworthSmooth.ToString());
                writer.WriteElementString("ButterworthSamplingFrequency", StringUtilities.DblToString(sicOptions.SICPeakFinderOptions.ButterworthSamplingFrequency, 5));
                writer.WriteElementString("ButterworthSamplingFrequencyDoubledForSIMData", sicOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData.ToString());

                writer.WriteElementString("UseSavitzkyGolaySmooth", sicOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth.ToString());
                writer.WriteElementString("SavitzkyGolayFilterOrder", sicOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder.ToString());

                var noiseThresholdOptions = sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions;
                writer.WriteElementString("MassSpectraNoiseThresholdMode", noiseThresholdOptions.BaselineNoiseMode.ToString());
                writer.WriteElementString("MassSpectraNoiseThresholdIntensity", noiseThresholdOptions.BaselineNoiseLevelAbsolute.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("MassSpectraNoiseFractionLowIntensityDataToAverage", StringUtilities.DblToString(noiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage, 5));
                writer.WriteElementString("MassSpectraNoiseMinimumSignalToNoiseRatio", noiseThresholdOptions.MinimumSignalToNoiseRatio.ToString(CultureInfo.InvariantCulture));

                writer.WriteElementString("ReplaceSICZeroesWithMinimumPositiveValueFromMSData", sicOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData.ToString());
                writer.WriteElementString("SaveSmoothedData", sicOptions.SaveSmoothedData.ToString());

                // Similarity options
                writer.WriteElementString("SimilarIonMZToleranceHalfWidth", sicOptions.SimilarIonMZToleranceHalfWidth.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("SimilarIonToleranceHalfWidthMinutes", sicOptions.SimilarIonToleranceHalfWidthMinutes.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("SpectrumSimilarityMinimum", sicOptions.SpectrumSimilarityMinimum.ToString(CultureInfo.InvariantCulture));

                writer.WriteEndElement();

                writer.WriteStartElement("BinningOptions");

                writer.WriteElementString("BinStartX", binningOptions.StartX.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("BinEndX", binningOptions.EndX.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("BinSize", binningOptions.BinSize.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("MaximumBinCount", binningOptions.MaximumBinCount.ToString());

                writer.WriteElementString("IntensityPrecisionPercent", binningOptions.IntensityPrecisionPercent.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("Normalize", binningOptions.Normalize.ToString());
                writer.WriteElementString("SumAllIntensitiesForBin", binningOptions.SumAllIntensitiesForBin.ToString());

                writer.WriteEndElement();

                writer.WriteStartElement("CustomSICValues");

                writer.WriteElementString("MZList", mOptions.CustomSICList.RawTextMZList);
                writer.WriteElementString("MZToleranceDaList", CheckForEmptyToleranceList(mOptions.CustomSICList.RawTextMZToleranceDaList));
                writer.WriteElementString("ScanCenterList", mOptions.CustomSICList.RawTextScanOrAcqTimeCenterList);
                writer.WriteElementString("ScanToleranceList", CheckForEmptyToleranceList(mOptions.CustomSICList.RawTextScanOrAcqTimeToleranceList));
                writer.WriteElementString("ScanTolerance", mOptions.CustomSICList.ScanOrAcqTimeTolerance.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("ScanType", mOptions.CustomSICList.ScanToleranceType.ToString());
                writer.WriteElementString("LimitSearchToCustomMZList", mOptions.CustomSICList.LimitSearchToCustomMZList.ToString());

                writer.WriteEndElement();
            }
            catch (Exception ex)
            {
                ReportError("Error initializing the XML output file: " + xmlOutputFilePath, ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Replace a value in the XML results file
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="lineIn"></param>
        /// <param name="xmlElementName">This should be the properly capitalized element name and should not start with "&lt;"</param>
        /// <param name="newValueToSave"></param>
        private void XmlOutputFileReplaceSetting(
            TextWriter writer,
            string lineIn,
            string xmlElementName,
            int newValueToSave)
        {
            // Need to add two since xmlElementName doesn't include "<" at the beginning
            var work = lineIn.Trim().ToLower().Substring(xmlElementName.Length + 2);

            // Look for the "<" after the number
            var charIndex = work.IndexOf("<", StringComparison.Ordinal);
            if (charIndex > 0)
            {
                // Isolate the number
                work = work.Substring(0, charIndex);
                if (Utilities.IsNumber(work))
                {
                    var currentValue = int.Parse(work);

                    if (newValueToSave != currentValue)
                    {
                        lineIn = "  <" + xmlElementName + ">";
                        lineIn += newValueToSave.ToString();
                        lineIn += "</" + xmlElementName + ">";
                    }
                }
            }

            writer.WriteLine(lineIn);
        }

        /// <summary>
        /// Update inputFileName with optimal peak apex values
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="inputFileName"></param>
        /// <param name="outputDirectoryPath"></param>
        public bool XmlOutputFileUpdateEntries(
            ScanList scanList,
            string inputFileName,
            string outputDirectoryPath)
        {
            // ReSharper disable once StringLiteralTypo
            const string PARENT_ION_TAG_START_LOWER = "<parention";     // Note: this needs to be lowercase
            const string INDEX_ATTRIBUTE_LOWER = "index=";              // Note: this needs to be lowercase

            const string OPTIMAL_PEAK_APEX_TAG_NAME = "OptimalPeakApexScanNumber";
            const string PEAK_APEX_OVERRIDE_PARENT_ION_TAG_NAME = "PeakApexOverrideParentIonIndex";

            var xmlReadFilePath = DataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, DataOutput.OutputFileTypeConstants.XMLFile);

            var xmlOutputFilePath = Path.Combine(outputDirectoryPath, "__temp__MASICOutputFile.xml");

            try
            {
                // Wait 2 seconds before reopening the file, to make sure the handle is closed
                System.Threading.Thread.Sleep(2000);

                if (!File.Exists(xmlReadFilePath))
                {
                    // XML file not found, exit the function
                    return true;
                }

                using (var reader = new StreamReader(xmlReadFilePath))
                using (var writer = new StreamWriter(xmlOutputFilePath, false))
                {
                    UpdateProgress(0, "Updating XML file with optimal peak apex values");

                    var parentIonIndex = -1;
                    var parentIonsProcessed = 0;
                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        if (dataLine == null)
                            continue;

                        var dataLineLCase = dataLine.Trim().ToLower();

                        if (dataLineLCase.StartsWith(PARENT_ION_TAG_START_LOWER))
                        {
                            var charIndex = dataLineLCase.IndexOf(INDEX_ATTRIBUTE_LOWER, StringComparison.CurrentCultureIgnoreCase);
                            if (charIndex > 0)
                            {
                                var work = dataLineLCase.Substring(charIndex + INDEX_ATTRIBUTE_LOWER.Length + 1);
                                charIndex = work.IndexOf('"');
                                if (charIndex > 0)
                                {
                                    work = work.Substring(0, charIndex);
                                    if (Utilities.IsNumber(work))
                                    {
                                        parentIonIndex = int.Parse(work);
                                        parentIonsProcessed++;

                                        // Update progress
                                        if (scanList.ParentIons.Count > 1)
                                        {
                                            if (parentIonsProcessed % 100 == 0)
                                            {
                                                UpdateProgress((short)(parentIonsProcessed / (double)(scanList.ParentIons.Count - 1) * 100));
                                            }
                                        }
                                        else
                                        {
                                            UpdateProgress(0);
                                        }

                                        if (mOptions.AbortProcessing)
                                        {
                                            scanList.ProcessingIncomplete = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            writer.WriteLine(dataLine);
                        }
                        else if (dataLineLCase.StartsWith("<" + OPTIMAL_PEAK_APEX_TAG_NAME.ToLower()) && parentIonIndex >= 0)
                        {
                            if (parentIonIndex < scanList.ParentIons.Count)
                            {
                                XmlOutputFileReplaceSetting(writer, dataLine, OPTIMAL_PEAK_APEX_TAG_NAME, scanList.ParentIons[parentIonIndex].OptimalPeakApexScanNumber);
                            }
                        }
                        else if (dataLineLCase.StartsWith("<" + PEAK_APEX_OVERRIDE_PARENT_ION_TAG_NAME.ToLower()) && parentIonIndex >= 0)
                        {
                            if (parentIonIndex < scanList.ParentIons.Count)
                            {
                                XmlOutputFileReplaceSetting(writer, dataLine, PEAK_APEX_OVERRIDE_PARENT_ION_TAG_NAME, scanList.ParentIons[parentIonIndex].PeakApexOverrideParentIonIndex);
                            }
                        }
                        else
                        {
                            writer.WriteLine(dataLine);
                        }
                    }
                }

                try
                {
                    // Wait 2 seconds, then delete the original file and rename the temp one to the original one
                    System.Threading.Thread.Sleep(2000);

                    if (File.Exists(xmlOutputFilePath))
                    {
                        if (File.Exists(xmlReadFilePath))
                        {
                            File.Delete(xmlReadFilePath);
                            System.Threading.Thread.Sleep(500);
                        }

                        File.Move(xmlOutputFilePath, xmlReadFilePath);
                    }
                }
                catch (Exception ex)
                {
                    ReportError("Error renaming XML output file from temp name to: " + xmlReadFilePath, ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
                    return false;
                }

                UpdateProgress(100);

#if GUI
                System.Windows.Forms.Application.DoEvents();
#endif
            }
            catch (Exception ex)
            {
                ReportError("Error updating the XML output file: " + xmlReadFilePath, ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }
    }
}
