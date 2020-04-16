using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using MASIC.DatasetStats;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using PRISM;

namespace MASIC.DataOutput
{
    public class clsXMLResultsWriter : clsMasicEventNotifier
    {
        #region // TODO
        private readonly clsMASICOptions mOptions;
        #endregion
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="masicOptions"></param>
        public clsXMLResultsWriter(clsMASICOptions masicOptions)
        {
            mOptions = masicOptions;
        }

        /// <summary>
        /// Examines the values in toleranceList
        /// If all empty and/or all 0, returns an empty string
        /// </summary>
        /// <param name="toleranceList">Comma separated list of values</param>
        /// <returns></returns>
        private string CheckForEmptyToleranceList(string toleranceList)
        {
            var toleranceValues = toleranceList.Split(',');
            bool valuesDefined = false;
            foreach (var value in toleranceValues)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if ((value.Trim() ?? "") == "0")
                {
                    continue;
                }

                double parsedValue;
                if (double.TryParse(value, out parsedValue))
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
            else
            {
                return string.Empty;
            }
        }

        public bool SaveDataToXML(clsScanList scanList, int parentIonIndex, clsSICDetails sicDetails, MASICPeakFinder.clsSmoothedYDataSubset smoothedYDataSubset, clsDataOutput dataOutputHandler)
        {
            // Numbers between 0 and 255 that specify the distance (in scans) between each of the data points in SICData(); the first scan number is given by SICScanIndices(0)
            byte[] SICDataScanIntervals;
            string lastGoodLoc = "Start";
            bool intensityDataListWritten;
            bool massDataList;
            try
            {
                // Populate udtSICStats.SICDataScanIntervals with the scan intervals between each of the data points

                if (sicDetails.SICDataCount == 0)
                {
                    SICDataScanIntervals = new byte[1];
                }
                else
                {
                    SICDataScanIntervals = new byte[sicDetails.SICDataCount];
                    var sicScanNumbers = sicDetails.SICScanNumbers;
                    for (int scanIndex = 1; scanIndex <= sicDetails.SICDataCount - 1; scanIndex++)
                    {
                        int scanDelta = sicScanNumbers[scanIndex] - sicScanNumbers[scanIndex - 1];
                        // When storing in SICDataScanIntervals, make sure the Scan Interval is, at most, 255; it will typically be 1 or 4
                        // However, for MRM data, field size can be much larger
                        SICDataScanIntervals[scanIndex] = Conversions.ToByte(Math.Min(byte.MaxValue, scanDelta));
                    }
                }

                var writer = dataOutputHandler.OutputFileHandles.XMLFileForSICs;
                if (writer is null)
                    return false;

                // Initialize the StringBuilder objects
                var sbIntensityDataList = new System.Text.StringBuilder();
                var sbMassDataList = new System.Text.StringBuilder();
                var sbPeakYDataSmoothed = new System.Text.StringBuilder();
                var sicScanIndices = sicDetails.SICScanIndices;

                // Write the SIC's and computed peak stats and areas to the XML file for the given parent ion
                for (int fragScanIndex = 0; fragScanIndex <= scanList.ParentIons[parentIonIndex].FragScanIndices.Count - 1; fragScanIndex++)
                {
                    lastGoodLoc = "fragScanIndex=" + fragScanIndex.ToString();
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
                        writer.WriteElementString("FragScanTime", currentFragScan.ScanTime.ToString());
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
                        writer.WriteElementString("CustomSICPeakMZToleranceDa", currentParentIon.CustomSICPeakMZToleranceDa.ToString());
                        writer.WriteElementString("CustomSICPeakScanTolerance", currentParentIon.CustomSICPeakScanOrAcqTimeTolerance.ToString());
                        writer.WriteElementString("CustomSICPeakScanToleranceType", mOptions.CustomSICList.ScanToleranceType.ToString());
                    }

                    lastGoodLoc = "sicStatsPeak = currentParentIon.SICStats.Peak";
                    var sicStatsPeak = currentParentIon.SICStats.Peak;
                    if (sicDetails.SICScanType == clsScanList.eScanTypeConstants.FragScan)
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
                    writer.WriteElementString("NoiseThresholdModeUsed", Conversions.ToInteger(noiseStats.NoiseThresholdModeUsed).ToString());
                    var statMoments = sicStatsPeak.StatisticalMoments;
                    writer.WriteElementString("StatMomentsArea", StringUtilities.ValueToString(statMoments.Area, 5));
                    writer.WriteElementString("CenterOfMassScan", statMoments.CenterOfMassScan.ToString());
                    writer.WriteElementString("PeakStDev", StringUtilities.ValueToString(statMoments.StDev, 3));
                    writer.WriteElementString("PeakSkew", StringUtilities.ValueToString(statMoments.Skew, 4));
                    writer.WriteElementString("PeakKSStat", StringUtilities.ValueToString(statMoments.KSStat, 4));
                    writer.WriteElementString("StatMomentsDataCountUsed", statMoments.DataCountUsed.ToString());
                    writer.WriteElementString("InterferenceScore", StringUtilities.ValueToString(interferenceScore, 4));
                    if (sicDetails.SICScanType == clsScanList.eScanTypeConstants.FragScan)
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
                        // If the interval is <=9, then the interval is stored as a number
                        // For intervals between 10 and 35, uses letters A to Z
                        // For intervals between 36 and 61, uses letters A to Z

                        lastGoodLoc = "Populate scanIntervalList";
                        string scanIntervalList = string.Empty;
                        if (SICDataScanIntervals is object)
                        {
                            for (int scanIntervalIndex = 0; scanIntervalIndex <= sicDetails.SICDataCount - 1; scanIntervalIndex++)
                            {
                                if (SICDataScanIntervals[scanIntervalIndex] <= 9)
                                {
                                    scanIntervalList += SICDataScanIntervals[scanIntervalIndex].ToString();
                                }
                                else if (SICDataScanIntervals[scanIntervalIndex] <= 35)
                                {
                                    scanIntervalList += Conversions.ToString((char)(SICDataScanIntervals[scanIntervalIndex] + 55));     // 55 = -10 + 65
                                }
                                else if (SICDataScanIntervals[scanIntervalIndex] <= 61)
                                {
                                    scanIntervalList += Conversions.ToString((char)(SICDataScanIntervals[scanIntervalIndex] + 61));     // 61 = -36 + 97
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
                            float[] dataArray;
                            dataArray = new float[smoothedYDataSubset.DataCount];
                            Array.Copy(smoothedYDataSubset.Data, dataArray, smoothedYDataSubset.DataCount);
                            SaveDataToXMLEncodeArray(writer, "SICSmoothedYData", dataArray);
                        }
                    }
                    else
                    {
                        // Save intensity and mass data lists as tab-delimited text list

                        intensityDataListWritten = false;
                        massDataList = false;
                        try
                        {
                            lastGoodLoc = "Populate sbIntensityDataList";
                            sbIntensityDataList.Length = 0;
                            sbMassDataList.Length = 0;
                            if (sicDetails.SICDataCount > 0)
                            {
                                foreach (var dataPoint in sicDetails.SICData)
                                {
                                    if (dataPoint.Intensity > 0)
                                    {
                                        sbIntensityDataList.Append(StringUtilities.DblToString(dataPoint.Intensity, 1) + ",");
                                    }
                                    else
                                    {
                                        sbIntensityDataList.Append(',');
                                    }     // Do not output any number if the intensity is 0

                                    if (dataPoint.Mass > 0)
                                    {
                                        sbMassDataList.Append(StringUtilities.DblToString(dataPoint.Mass, 3) + ",");
                                    }
                                    else
                                    {
                                        sbMassDataList.Append(',');
                                    }     // Do not output any number if the mass is 0
                                }

                                // Trim the trailing comma
                                if (sbIntensityDataList[sbIntensityDataList.Length - 1] == ',')
                                {
                                    sbIntensityDataList.Length -= 1;
                                    sbMassDataList.Length -= 1;
                                }
                            }

                            writer.WriteElementString("IntensityDataList", sbIntensityDataList.ToString());
                            intensityDataListWritten = true;
                            writer.WriteElementString("MassDataList", sbMassDataList.ToString());
                            massDataList = true;
                        }
                        catch (OutOfMemoryException ex)
                        {
                            // Ignore the exception if this is an Out of Memory exception

                            if (!intensityDataListWritten)
                            {
                                writer.WriteElementString("IntensityDataList", string.Empty);
                            }

                            if (!massDataList)
                            {
                                writer.WriteElementString("MassDataList", string.Empty);
                            }
                        }

                        if (mOptions.SICOptions.SaveSmoothedData)
                        {
                            try
                            {
                                lastGoodLoc = "Populate sbPeakYDataSmoothed";
                                sbPeakYDataSmoothed.Length = 0;
                                if (smoothedYDataSubset.Data is object && smoothedYDataSubset.DataCount > 0)
                                {
                                    for (int index = 0; index <= smoothedYDataSubset.DataCount - 1; index++)
                                        sbPeakYDataSmoothed.Append(Math.Round(smoothedYDataSubset.Data[index]).ToString() + ",");

                                    // Trim the trailing comma
                                    sbPeakYDataSmoothed.Length -= 1;
                                }

                                writer.WriteElementString("SmoothedYDataList", sbPeakYDataSmoothed.ToString());
                            }
                            catch (OutOfMemoryException ex)
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
                ReportError("Error writing the XML data to the output file; Last good location: " + lastGoodLoc, ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }

        private void SaveDataToXMLEncodeArray(XmlWriter writer, string elementName, byte[] dataArray)
        {
            int precisionBits;
            string dataTypeName = string.Empty;
            string encodedValues = MSDataFileReader.clsBase64EncodeDecode.EncodeNumericArray(dataArray, out precisionBits, out dataTypeName);
            writer.WriteStartElement(elementName);
            writer.WriteAttributeString("precision", precisionBits.ToString());        // Store the precision, in bits
            writer.WriteAttributeString("type", dataTypeName);
            writer.WriteString(encodedValues);
            writer.WriteEndElement();
        }

        private void SaveDataToXMLEncodeArray(XmlWriter writer, string elementName, float[] dataArray)
        {
            int precisionBits;
            string dataTypeName = string.Empty;
            string encodedValues = MSDataFileReader.clsBase64EncodeDecode.EncodeNumericArray(dataArray, out precisionBits, out dataTypeName);
            writer.WriteStartElement(elementName);
            writer.WriteAttributeString("precision", precisionBits.ToString());        // Store the precision, in bits
            writer.WriteAttributeString("type", dataTypeName);
            writer.WriteString(encodedValues);
            writer.WriteEndElement();
        }

        public bool XMLOutputFileFinalize(clsDataOutput dataOutputHandler, clsScanList scanList, clsSpectraCache spectraCache, clsProcessingStats processingStats, float processingTimeSec)
        {
            var writer = dataOutputHandler.OutputFileHandles.XMLFileForSICs;
            if (writer is null)
                return false;
            try
            {
                writer.WriteStartElement("ProcessingStats");
                writer.WriteElementString("CacheEventCount", spectraCache.CacheEventCount.ToString());
                writer.WriteElementString("UnCacheEventCount", spectraCache.UnCacheEventCount.ToString());
                writer.WriteElementString("PeakMemoryUsageMB", StringUtilities.DblToString(processingStats.PeakMemoryUsageMB, 2));
                float effectiveSeconds = processingTimeSec - processingStats.TotalProcessingTimeAtStart;
                writer.WriteElementString("TotalProcessingTimeSeconds", StringUtilities.DblToString(effectiveSeconds, 2));
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
                ReportError("Error finalizing the XML output file", ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }

        public bool XMLOutputFileInitialize(string inputFilePathFull, string outputDirectoryPath, clsDataOutput dataOutputHandler, clsScanList scanList, clsSpectraCache spectraCache, clsSICOptions sicOptions, clsBinningOptions binningOptions)
        {
            string xmlOutputFilePath = string.Empty;
            try
            {
                xmlOutputFilePath = clsDataOutput.ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.XMLFile);
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
                catch (Exception ex)
                {
                    lastModTimeText = string.Empty;
                    fileSizeBytes = "0";
                }

                writer.WriteElementString("SourceFileDateTime", lastModTimeText);
                writer.WriteElementString("SourceFileSizeBytes", fileSizeBytes);
                writer.WriteElementString("MASICProcessingDate", DateTime.Now.ToString(clsDatasetStatsSummarizer.DATE_TIME_FORMAT_STRING));
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

                // "SICToleranceDa" is a legacy parameter; If the SIC tolerance is in PPM, then "SICToleranceDa" is the Da tolerance at 1000 m/z
                writer.WriteElementString("SICToleranceDa", clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, 1000).ToString("0.0000"));
                writer.WriteElementString("SICTolerance", sicOptions.SICTolerance.ToString("0.0000"));
                writer.WriteElementString("SICToleranceIsPPM", sicOptions.SICToleranceIsPPM.ToString());
                writer.WriteElementString("RefineReportedParentIonMZ", sicOptions.RefineReportedParentIonMZ.ToString());
                writer.WriteElementString("ScanRangeStart", sicOptions.ScanRangeStart.ToString());
                writer.WriteElementString("ScanRangeEnd", sicOptions.ScanRangeEnd.ToString());
                writer.WriteElementString("RTRangeStart", sicOptions.RTRangeStart.ToString());
                writer.WriteElementString("RTRangeEnd", sicOptions.RTRangeEnd.ToString());
                writer.WriteElementString("CompressMSSpectraData", sicOptions.CompressMSSpectraData.ToString());
                writer.WriteElementString("CompressMSMSSpectraData", sicOptions.CompressMSMSSpectraData.ToString());
                writer.WriteElementString("CompressToleranceDivisorForDa", sicOptions.CompressToleranceDivisorForDa.ToString("0.0"));
                writer.WriteElementString("CompressToleranceDivisorForPPM", sicOptions.CompressToleranceDivisorForPPM.ToString("0.0"));
                writer.WriteElementString("MaxSICPeakWidthMinutesBackward", sicOptions.MaxSICPeakWidthMinutesBackward.ToString());
                writer.WriteElementString("MaxSICPeakWidthMinutesForward", sicOptions.MaxSICPeakWidthMinutesForward.ToString());
                writer.WriteElementString("IntensityThresholdFractionMax", StringUtilities.DblToString(sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax, 5));
                writer.WriteElementString("IntensityThresholdAbsoluteMinimum", sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum.ToString());

                // Peak Finding Options
                var baselineNoiseOptions = sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions;
                writer.WriteElementString("SICNoiseThresholdMode", baselineNoiseOptions.BaselineNoiseMode.ToString());
                writer.WriteElementString("SICNoiseThresholdIntensity", baselineNoiseOptions.BaselineNoiseLevelAbsolute.ToString());
                writer.WriteElementString("SICNoiseFractionLowIntensityDataToAverage", StringUtilities.DblToString(baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, 5));
                writer.WriteElementString("SICNoiseMinimumSignalToNoiseRatio", baselineNoiseOptions.MinimumSignalToNoiseRatio.ToString());
                writer.WriteElementString("MaxDistanceScansNoOverlap", sicOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap.ToString());
                writer.WriteElementString("MaxAllowedUpwardSpikeFractionMax", StringUtilities.DblToString(sicOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax, 5));
                writer.WriteElementString("InitialPeakWidthScansScaler", sicOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler.ToString());
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
                writer.WriteElementString("MassSpectraNoiseThresholdIntensity", noiseThresholdOptions.BaselineNoiseLevelAbsolute.ToString());
                writer.WriteElementString("MassSpectraNoiseFractionLowIntensityDataToAverage", StringUtilities.DblToString(noiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage, 5));
                writer.WriteElementString("MassSpectraNoiseMinimumSignalToNoiseRatio", noiseThresholdOptions.MinimumSignalToNoiseRatio.ToString());
                writer.WriteElementString("ReplaceSICZeroesWithMinimumPositiveValueFromMSData", sicOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData.ToString());
                writer.WriteElementString("SaveSmoothedData", sicOptions.SaveSmoothedData.ToString());

                // Similarity options
                writer.WriteElementString("SimilarIonMZToleranceHalfWidth", sicOptions.SimilarIonMZToleranceHalfWidth.ToString());
                writer.WriteElementString("SimilarIonToleranceHalfWidthMinutes", sicOptions.SimilarIonToleranceHalfWidthMinutes.ToString());
                writer.WriteElementString("SpectrumSimilarityMinimum", sicOptions.SpectrumSimilarityMinimum.ToString());
                writer.WriteEndElement();
                writer.WriteStartElement("BinningOptions");
                writer.WriteElementString("BinStartX", binningOptions.StartX.ToString());
                writer.WriteElementString("BinEndX", binningOptions.EndX.ToString());
                writer.WriteElementString("BinSize", binningOptions.BinSize.ToString());
                writer.WriteElementString("MaximumBinCount", binningOptions.MaximumBinCount.ToString());
                writer.WriteElementString("IntensityPrecisionPercent", binningOptions.IntensityPrecisionPercent.ToString());
                writer.WriteElementString("Normalize", binningOptions.Normalize.ToString());
                writer.WriteElementString("SumAllIntensitiesForBin", binningOptions.SumAllIntensitiesForBin.ToString());
                writer.WriteEndElement();
                writer.WriteStartElement("CustomSICValues");
                writer.WriteElementString("MZList", mOptions.CustomSICList.RawTextMZList);
                writer.WriteElementString("MZToleranceDaList", CheckForEmptyToleranceList(mOptions.CustomSICList.RawTextMZToleranceDaList));
                writer.WriteElementString("ScanCenterList", mOptions.CustomSICList.RawTextScanOrAcqTimeCenterList);
                writer.WriteElementString("ScanToleranceList", CheckForEmptyToleranceList(mOptions.CustomSICList.RawTextScanOrAcqTimeToleranceList));
                writer.WriteElementString("ScanTolerance", mOptions.CustomSICList.ScanOrAcqTimeTolerance.ToString());
                writer.WriteElementString("ScanType", mOptions.CustomSICList.ScanToleranceType.ToString());
                writer.WriteElementString("LimitSearchToCustomMZList", mOptions.CustomSICList.LimitSearchToCustomMZList.ToString());
                writer.WriteEndElement();
            }
            catch (Exception ex)
            {
                ReportError("Error initializing the XML output file: " + xmlOutputFilePath, ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }

        private void XmlOutputFileReplaceSetting(TextWriter writer, string lineIn, string xmlElementName, int newValueToSave)
        {
            // xmlElementName should be the properly capitalized element name and should not start with "<"

            string work;
            int charIndex;
            int currentValue;

            // Need to add two since xmlElementName doesn't include "<" at the beginning
            work = lineIn.Trim().ToLower().Substring(xmlElementName.Length + 2);

            // Look for the "<" after the number
            charIndex = work.IndexOf("<", StringComparison.Ordinal);
            if (charIndex > 0)
            {
                // Isolate the number
                work = work.Substring(0, charIndex);
                if (clsUtilities.IsNumber(work))
                {
                    currentValue = Conversions.ToInteger(work);
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

        public bool XmlOutputFileUpdateEntries(clsScanList scanList, string inputFileName, string outputDirectoryPath)
        {
            // ReSharper disable once StringLiteralTypo
            const string PARENT_ION_TAG_START_LCASE = "<parention";     // Note: this needs to be lowercase
            const string INDEX_ATTRIBUTE_LCASE = "index=";              // Note: this needs to be lowercase
            const string OPTIMAL_PEAK_APEX_TAG_NAME = "OptimalPeakApexScanNumber";
            const string PEAK_APEX_OVERRIDE_PARENT_ION_TAG_NAME = "PeakApexOverrideParentIonIndex";
            string xmlReadFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.XMLFile);
            string xmlOutputFilePath = Path.Combine(outputDirectoryPath, "__temp__MASICOutputFile.xml");
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
                    int parentIonIndex = -1;
                    int parentIonsProcessed = 0;
                    while (!reader.EndOfStream)
                    {
                        string dataLine = reader.ReadLine();
                        if (dataLine is null)
                            continue;
                        string dataLineLCase = dataLine.Trim().ToLower();
                        if (dataLineLCase.StartsWith(PARENT_ION_TAG_START_LCASE))
                        {
                            int charIndex = dataLineLCase.IndexOf(INDEX_ATTRIBUTE_LCASE, StringComparison.CurrentCultureIgnoreCase);
                            if (charIndex > 0)
                            {
                                string work = dataLineLCase.Substring(charIndex + INDEX_ATTRIBUTE_LCASE.Length + 1);
                                charIndex = work.IndexOf(ControlChars.Quote);
                                if (charIndex > 0)
                                {
                                    work = work.Substring(0, charIndex);
                                    if (clsUtilities.IsNumber(work))
                                    {
                                        parentIonIndex = Conversions.ToInteger(work);
                                        parentIonsProcessed += 1;

                                        // Update progress
                                        if (scanList.ParentIons.Count > 1)
                                        {
                                            if (parentIonsProcessed % 100 == 0)
                                            {
                                                UpdateProgress(Conversions.ToShort(parentIonsProcessed / (double)(scanList.ParentIons.Count - 1) * 100));
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
                    ReportError("Error renaming XML output file from temp name to: " + xmlReadFilePath, ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                    return false;
                }

                UpdateProgress(100);

#if // TODO
                Application.DoEvents();
            }
#endif
            catch (Exception ex)
            {
                ReportError("Error updating the XML output file: " + xmlReadFilePath, ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }
    }
}