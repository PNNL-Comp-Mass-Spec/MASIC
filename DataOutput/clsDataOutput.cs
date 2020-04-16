using System;
using System.Collections.Generic;
using System.IO;
using MASIC.DatasetStats;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using PRISM;

namespace MASIC.DataOutput
{
    public class clsDataOutput : clsMasicEventNotifier
    {
        #region // TODO
        public enum eOutputFileTypeConstants
        {
            XMLFile = 0,
            ScanStatsFlatFile = 1,
            ScanStatsExtendedFlatFile = 2,
            ScanStatsExtendedConstantFlatFile = 3,
            SICStatsFlatFile = 4,
            BPIFile = 5,
            FragBPIFile = 6,
            TICFile = 7,
            ICRToolsFragTICChromatogramByScan = 8,
            ICRToolsBPIChromatogramByScan = 9,
            ICRToolsBPIChromatogramByTime = 10,
            ICRToolsTICChromatogramByScan = 11,
            PEKFile = 12,
            HeaderGlossary = 13,
            DeconToolsScansFile = 14,
            DeconToolsIsosFile = 15,
            DeconToolsMSChromatogramFile = 16,
            DeconToolsMSMSChromatogramFile = 17,
            MSMethodFile = 18,
            MSTuneFile = 19,
            ReporterIonsFile = 20,
            MRMSettingsFile = 21,
            MRMDatafile = 22,
            MRMCrosstabFile = 23,
            DatasetInfoFile = 24,
            SICDataFile = 25
        }
        #endregion
        #region // TODO
        private readonly clsMASICOptions mOptions;

        public clsOutputFileHandles OutputFileHandles { get; private set; }
        public clsExtendedStatsWriter ExtendedStatsWriter { get; private set; }

        #endregion
        /// <summary>
        /// Constructor
        /// </summary>
        public clsDataOutput(clsMASICOptions masicOptions)
        {
            mOptions = masicOptions;
            OutputFileHandles = new clsOutputFileHandles();
            RegisterEvents(OutputFileHandles);
            ExtendedStatsWriter = new clsExtendedStatsWriter(mOptions);
            RegisterEvents(ExtendedStatsWriter);
        }

        public bool CheckForExistingResults(string inputFilePathFull, string outputDirectoryPath, clsMASICOptions masicOptions)
        {
            // Returns True if existing results already exist for the given input file path, SIC Options, and Binning options

            string filePathToCheck;
            var sicOptionsCompare = new clsSICOptions();
            var binningOptionsCompare = new clsBinningOptions();
            bool validExistingResultsFound;
            var sourceFileSizeBytes = default(long);
            string sourceFilePathCheck = string.Empty;
            string masicVersion = string.Empty;
            string masicPeakFinderDllVersion = string.Empty;
            string sourceFileDateTimeCheck = string.Empty;
            DateTime sourceFileDateTime;
            var skipMSMSProcessing = default(bool);
            validExistingResultsFound = false;
            try
            {
                // Don't even look for the XML file if mSkipSICAndRawDataProcessing = True
                if (masicOptions.SkipSICAndRawDataProcessing)
                {
                    return false;
                }

                // Obtain the output XML filename
                filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.XMLFile);

                // See if the file exists
                if (File.Exists(filePathToCheck))
                {
                    if (masicOptions.FastExistingXMLFileTest)
                    {
                        // XML File found; do not check the settings or version to see if they match the current ones
                        return true;
                    }

                    // Open the XML file and look for the "ProcessingComplete" node
                    var xmlDoc = new System.Xml.XmlDocument();
                    try
                    {
                        xmlDoc.Load(filePathToCheck);
                    }
                    catch (Exception ex)
                    {
                        // Invalid XML file; do not continue
                        return false;
                    }

                    // If we get here, the file opened successfully
                    var rootElement = xmlDoc.DocumentElement;
                    if ((rootElement.Name ?? "") == "SICData")
                    {
                        // See if the ProcessingComplete node has a value of True
                        var matchingNodeList = rootElement.GetElementsByTagName("ProcessingComplete");
                        if (matchingNodeList is null || matchingNodeList.Count != 1)
                            break;
                        if ((matchingNodeList.Item(0).InnerText.ToLower() ?? "") != "true")
                            break;

                        // Read the ProcessingSummary and populate
                        matchingNodeList = rootElement.GetElementsByTagName("ProcessingSummary");
                        if (matchingNodeList is null || matchingNodeList.Count != 1)
                            break;
                        foreach (System.Xml.XmlNode valueNode in matchingNodeList[0].ChildNodes)
                        {
                            switch (valueNode.Name)
                            {
                                case "DatasetID":
                                    sicOptionsCompare.DatasetID = Conversions.ToInteger(valueNode.InnerText);
                                    break;
                                case "SourceFilePath":
                                    sourceFilePathCheck = valueNode.InnerText;
                                    break;
                                case "SourceFileDateTime":
                                    sourceFileDateTimeCheck = valueNode.InnerText;
                                    break;
                                case "SourceFileSizeBytes":
                                    sourceFileSizeBytes = Conversions.ToLong(valueNode.InnerText);
                                    break;
                                case "MASICVersion":
                                    masicVersion = valueNode.InnerText;
                                    break;
                                case "MASICPeakFinderDllVersion":
                                    masicPeakFinderDllVersion = valueNode.InnerText;
                                    break;
                                case "SkipMSMSProcessing":
                                    skipMSMSProcessing = Conversions.ToBoolean(valueNode.InnerText);
                                    break;
                            }
                        }

                        if (masicVersion is null)
                            masicVersion = string.Empty;
                        if (masicPeakFinderDllVersion is null)
                            masicPeakFinderDllVersion = string.Empty;

                        // Check if the MASIC version matches
                        if ((masicVersion ?? "") != (masicOptions.MASICVersion ?? ""))
                            break;
                        if ((masicPeakFinderDllVersion ?? "") != (masicOptions.PeakFinderVersion ?? ""))
                            break;

                        // Check the dataset number
                        if (sicOptionsCompare.DatasetID != masicOptions.SICOptions.DatasetID)
                            break;

                        // Check the filename in sourceFilePathCheck
                        if ((Path.GetFileName(sourceFilePathCheck) ?? "") != (Path.GetFileName(inputFilePathFull) ?? ""))
                            break;

                        // Check if the source file stats match
                        var inputFileInfo = new FileInfo(inputFilePathFull);
                        sourceFileDateTime = inputFileInfo.LastWriteTime;
                        if ((sourceFileDateTimeCheck ?? "") != (sourceFileDateTime.ToShortDateString() + " " + sourceFileDateTime.ToShortTimeString() ?? ""))
                            break;
                        if (sourceFileSizeBytes != inputFileInfo.Length)
                            break;

                        // Check that skipMSMSProcessing matches
                        if (skipMSMSProcessing != masicOptions.SkipMSMSProcessing)
                            break;

                        // Read the ProcessingOptions and populate
                        matchingNodeList = rootElement.GetElementsByTagName("ProcessingOptions");
                        if (matchingNodeList is null || matchingNodeList.Count != 1)
                            break;
                        foreach (System.Xml.XmlNode valueNode in matchingNodeList[0].ChildNodes)
                        {
                            switch (valueNode.Name)
                            {
                                case "SICToleranceDa":
                                    sicOptionsCompare.SICTolerance = Conversions.ToDouble(valueNode.InnerText);            // Legacy name
                                    break;
                                case "SICTolerance":
                                    sicOptionsCompare.SICTolerance = Conversions.ToDouble(valueNode.InnerText);
                                    break;
                                case "SICToleranceIsPPM":
                                    sicOptionsCompare.SICToleranceIsPPM = Conversions.ToBoolean(valueNode.InnerText);
                                    break;
                                case "RefineReportedParentIonMZ":
                                    sicOptionsCompare.RefineReportedParentIonMZ = Conversions.ToBoolean(valueNode.InnerText);
                                    break;
                                case "ScanRangeEnd":
                                    sicOptionsCompare.ScanRangeEnd = Conversions.ToInteger(valueNode.InnerText);
                                    break;
                                case "ScanRangeStart":
                                    sicOptionsCompare.ScanRangeStart = Conversions.ToInteger(valueNode.InnerText);
                                    break;
                                case "RTRangeEnd":
                                    sicOptionsCompare.RTRangeEnd = Conversions.ToSingle(valueNode.InnerText);
                                    break;
                                case "RTRangeStart":
                                    sicOptionsCompare.RTRangeStart = Conversions.ToSingle(valueNode.InnerText);
                                    break;
                                case "CompressMSSpectraData":
                                    sicOptionsCompare.CompressMSSpectraData = Conversions.ToBoolean(valueNode.InnerText);
                                    break;
                                case "CompressMSMSSpectraData":
                                    sicOptionsCompare.CompressMSMSSpectraData = Conversions.ToBoolean(valueNode.InnerText);
                                    break;
                                case "CompressToleranceDivisorForDa":
                                    sicOptionsCompare.CompressToleranceDivisorForDa = Conversions.ToDouble(valueNode.InnerText);
                                    break;
                                case "CompressToleranceDivisorForPPM":
                                    sicOptionsCompare.CompressToleranceDivisorForPPM = Conversions.ToDouble(valueNode.InnerText);
                                    break;
                                case "MaxSICPeakWidthMinutesBackward":
                                    sicOptionsCompare.MaxSICPeakWidthMinutesBackward = Conversions.ToSingle(valueNode.InnerText);
                                    break;
                                case "MaxSICPeakWidthMinutesForward":
                                    sicOptionsCompare.MaxSICPeakWidthMinutesForward = Conversions.ToSingle(valueNode.InnerText);
                                    break;
                                case "ReplaceSICZeroesWithMinimumPositiveValueFromMSData":
                                    sicOptionsCompare.ReplaceSICZeroesWithMinimumPositiveValueFromMSData = Conversions.ToBoolean(valueNode.InnerText);
                                    break;
                                case "SaveSmoothedData":
                                    sicOptionsCompare.SaveSmoothedData = Conversions.ToBoolean(valueNode.InnerText);
                                    break;
                                case "SimilarIonMZToleranceHalfWidth":
                                    sicOptionsCompare.SimilarIonMZToleranceHalfWidth = Conversions.ToSingle(valueNode.InnerText);
                                    break;
                                case "SimilarIonToleranceHalfWidthMinutes":
                                    sicOptionsCompare.SimilarIonToleranceHalfWidthMinutes = Conversions.ToSingle(valueNode.InnerText);
                                    break;
                                case "SpectrumSimilarityMinimum":
                                    sicOptionsCompare.SpectrumSimilarityMinimum = Conversions.ToSingle(valueNode.InnerText);
                                    break;
                                default:
                                    var withBlock = sicOptionsCompare.SICPeakFinderOptions;
                                    switch (valueNode.Name)
                                    {
                                        case "IntensityThresholdFractionMax":
                                            withBlock.IntensityThresholdFractionMax = Conversions.ToSingle(valueNode.InnerText);
                                            break;
                                        case "IntensityThresholdAbsoluteMinimum":
                                            withBlock.IntensityThresholdAbsoluteMinimum = Conversions.ToSingle(valueNode.InnerText);
                                            break;
                                        case "SICNoiseThresholdMode":
                                            withBlock.SICBaselineNoiseOptions.BaselineNoiseMode = (MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)Conversions.ToInteger(valueNode.InnerText);
                                            break;
                                        case "SICNoiseThresholdIntensity":
                                            withBlock.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute = Conversions.ToSingle(valueNode.InnerText);
                                            break;
                                        case "SICNoiseFractionLowIntensityDataToAverage":
                                            withBlock.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage = Conversions.ToSingle(valueNode.InnerText);
                                            break;
                                        case "SICNoiseMinimumSignalToNoiseRatio":
                                            withBlock.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = Conversions.ToSingle(valueNode.InnerText);
                                            break;
                                        case "MaxDistanceScansNoOverlap":
                                            withBlock.MaxDistanceScansNoOverlap = Conversions.ToInteger(valueNode.InnerText);
                                            break;
                                        case "MaxAllowedUpwardSpikeFractionMax":
                                            withBlock.MaxAllowedUpwardSpikeFractionMax = Conversions.ToSingle(valueNode.InnerText);
                                            break;
                                        case "InitialPeakWidthScansScaler":
                                            withBlock.InitialPeakWidthScansScaler = Conversions.ToSingle(valueNode.InnerText);
                                            break;
                                        case "InitialPeakWidthScansMaximum":
                                            withBlock.InitialPeakWidthScansMaximum = Conversions.ToInteger(valueNode.InnerText);
                                            break;
                                        case "FindPeaksOnSmoothedData":
                                            withBlock.FindPeaksOnSmoothedData = Conversions.ToBoolean(valueNode.InnerText);
                                            break;
                                        case "SmoothDataRegardlessOfMinimumPeakWidth":
                                            withBlock.SmoothDataRegardlessOfMinimumPeakWidth = Conversions.ToBoolean(valueNode.InnerText);
                                            break;
                                        case "UseButterworthSmooth":
                                            withBlock.UseButterworthSmooth = Conversions.ToBoolean(valueNode.InnerText);
                                            break;
                                        case "ButterworthSamplingFrequency":
                                            withBlock.ButterworthSamplingFrequency = Conversions.ToSingle(valueNode.InnerText);
                                            break;
                                        case "ButterworthSamplingFrequencyDoubledForSIMData":
                                            withBlock.ButterworthSamplingFrequencyDoubledForSIMData = Conversions.ToBoolean(valueNode.InnerText);
                                            break;
                                        case "UseSavitzkyGolaySmooth":
                                            withBlock.UseSavitzkyGolaySmooth = Conversions.ToBoolean(valueNode.InnerText);
                                            break;
                                        case "SavitzkyGolayFilterOrder":
                                            withBlock.SavitzkyGolayFilterOrder = Conversions.ToShort(valueNode.InnerText);
                                            break;
                                        case "MassSpectraNoiseThresholdMode":
                                            withBlock.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = (MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)Conversions.ToInteger(valueNode.InnerText);
                                            break;
                                        case "MassSpectraNoiseThresholdIntensity":
                                            withBlock.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute = Conversions.ToSingle(valueNode.InnerText);
                                            break;
                                        case "MassSpectraNoiseFractionLowIntensityDataToAverage":
                                            withBlock.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = Conversions.ToSingle(valueNode.InnerText);
                                            break;
                                        case "MassSpectraNoiseMinimumSignalToNoiseRatio":
                                            withBlock.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = Conversions.ToSingle(valueNode.InnerText);
                                            break;
                                    }

                                    break;
                            }
                        }

                        // Read the BinningOptions and populate
                        matchingNodeList = rootElement.GetElementsByTagName("BinningOptions");
                        if (matchingNodeList is null || matchingNodeList.Count != 1)
                            break;
                        foreach (System.Xml.XmlNode valueNode in matchingNodeList[0].ChildNodes)
                        {
                            switch (valueNode.Name)
                            {
                                case "BinStartX":
                                    binningOptionsCompare.StartX = Conversions.ToSingle(valueNode.InnerText);
                                    break;
                                case "BinEndX":
                                    binningOptionsCompare.EndX = Conversions.ToSingle(valueNode.InnerText);
                                    break;
                                case "BinSize":
                                    binningOptionsCompare.BinSize = Conversions.ToSingle(valueNode.InnerText);
                                    break;
                                case "MaximumBinCount":
                                    binningOptionsCompare.MaximumBinCount = Conversions.ToInteger(valueNode.InnerText);
                                    break;
                                case "IntensityPrecisionPercent":
                                    binningOptionsCompare.IntensityPrecisionPercent = Conversions.ToSingle(valueNode.InnerText);
                                    break;
                                case "Normalize":
                                    binningOptionsCompare.Normalize = Conversions.ToBoolean(valueNode.InnerText);
                                    break;
                                case "SumAllIntensitiesForBin":
                                    binningOptionsCompare.SumAllIntensitiesForBin = Conversions.ToBoolean(valueNode.InnerText);
                                    break;
                            }
                        }

                        // Read the CustomSICValues and populate

                        var customSICListCompare = new clsCustomSICList();
                        matchingNodeList = rootElement.GetElementsByTagName("CustomSICValues");
                        if (matchingNodeList is null || matchingNodeList.Count != 1)
                        {
                        }
                        // Custom values not defined; that's OK
                        else
                        {
                            foreach (System.Xml.XmlNode valueNode in matchingNodeList[0].ChildNodes)
                            {
                                switch (valueNode.Name)
                                {
                                    case "MZList":
                                        customSICListCompare.RawTextMZList = valueNode.InnerText;
                                        break;
                                    case "MZToleranceDaList":
                                        customSICListCompare.RawTextMZToleranceDaList = valueNode.InnerText;
                                        break;
                                    case "ScanCenterList":
                                        customSICListCompare.RawTextScanOrAcqTimeCenterList = valueNode.InnerText;
                                        break;
                                    case "ScanToleranceList":
                                        customSICListCompare.RawTextScanOrAcqTimeToleranceList = valueNode.InnerText;
                                        break;
                                    case "ScanTolerance":
                                        customSICListCompare.ScanOrAcqTimeTolerance = Conversions.ToSingle(valueNode.InnerText);
                                        break;
                                    case "ScanType":
                                        customSICListCompare.ScanToleranceType = masicOptions.GetScanToleranceTypeFromText(valueNode.InnerText);
                                        break;
                                }
                            }
                        }

                        var sicOptions = masicOptions.SICOptions;

                        // Check if the processing options match
                        if (clsUtilities.ValuesMatch(sicOptionsCompare.SICTolerance, sicOptions.SICTolerance, 3) && sicOptionsCompare.SICToleranceIsPPM == sicOptions.SICToleranceIsPPM && sicOptionsCompare.RefineReportedParentIonMZ == sicOptions.RefineReportedParentIonMZ && sicOptionsCompare.ScanRangeStart == sicOptions.ScanRangeStart && sicOptionsCompare.ScanRangeEnd == sicOptions.ScanRangeEnd && clsUtilities.ValuesMatch(sicOptionsCompare.RTRangeStart, sicOptions.RTRangeStart, 2) && clsUtilities.ValuesMatch(sicOptionsCompare.RTRangeEnd, sicOptions.RTRangeEnd, 2) && sicOptionsCompare.CompressMSSpectraData == sicOptions.CompressMSSpectraData && sicOptionsCompare.CompressMSMSSpectraData == sicOptions.CompressMSMSSpectraData && clsUtilities.ValuesMatch(sicOptionsCompare.CompressToleranceDivisorForDa, sicOptions.CompressToleranceDivisorForDa, 2) && clsUtilities.ValuesMatch(sicOptionsCompare.CompressToleranceDivisorForPPM, sicOptions.CompressToleranceDivisorForDa, 2) && clsUtilities.ValuesMatch(sicOptionsCompare.MaxSICPeakWidthMinutesBackward, sicOptions.MaxSICPeakWidthMinutesBackward, 2) && clsUtilities.ValuesMatch(sicOptionsCompare.MaxSICPeakWidthMinutesForward, sicOptions.MaxSICPeakWidthMinutesForward, 2) && sicOptionsCompare.ReplaceSICZeroesWithMinimumPositiveValueFromMSData == sicOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData && clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.IntensityThresholdFractionMax, sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax) && clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum, sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum) && sicOptionsCompare.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode == sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode && clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute, sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute) && clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage) && clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio, sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio) && sicOptionsCompare.SICPeakFinderOptions.MaxDistanceScansNoOverlap == sicOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap && clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax, sicOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax) && clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.InitialPeakWidthScansScaler, sicOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler) && sicOptionsCompare.SICPeakFinderOptions.InitialPeakWidthScansMaximum == sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum && sicOptionsCompare.SICPeakFinderOptions.FindPeaksOnSmoothedData == sicOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData && sicOptionsCompare.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth == sicOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth && sicOptionsCompare.SICPeakFinderOptions.UseButterworthSmooth == sicOptions.SICPeakFinderOptions.UseButterworthSmooth && clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.ButterworthSamplingFrequency, sicOptions.SICPeakFinderOptions.ButterworthSamplingFrequency) && sicOptionsCompare.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData == sicOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData && sicOptionsCompare.SICPeakFinderOptions.UseSavitzkyGolaySmooth == sicOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth && sicOptionsCompare.SICPeakFinderOptions.SavitzkyGolayFilterOrder == sicOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder && sicOptionsCompare.SaveSmoothedData == sicOptions.SaveSmoothedData && sicOptionsCompare.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode == sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode && clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute) && clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage) && clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio) && clsUtilities.ValuesMatch(sicOptionsCompare.SimilarIonMZToleranceHalfWidth, sicOptions.SimilarIonMZToleranceHalfWidth) && clsUtilities.ValuesMatch(sicOptionsCompare.SimilarIonToleranceHalfWidthMinutes, sicOptions.SimilarIonToleranceHalfWidthMinutes) && clsUtilities.ValuesMatch(sicOptionsCompare.SpectrumSimilarityMinimum, sicOptions.SpectrumSimilarityMinimum))
                        {
                            validExistingResultsFound = true;
                        }
                        else
                        {
                            validExistingResultsFound = false;
                        }

                        if (validExistingResultsFound)
                        {
                            // Check if the binning options match
                            var binningOptions = masicOptions.BinningOptions;
                            if (clsUtilities.ValuesMatch(binningOptionsCompare.StartX, binningOptions.StartX) && clsUtilities.ValuesMatch(binningOptionsCompare.EndX, binningOptions.EndX) && clsUtilities.ValuesMatch(binningOptionsCompare.BinSize, binningOptions.BinSize) && binningOptionsCompare.MaximumBinCount == binningOptions.MaximumBinCount && clsUtilities.ValuesMatch(binningOptionsCompare.IntensityPrecisionPercent, binningOptions.IntensityPrecisionPercent) && binningOptionsCompare.Normalize == binningOptions.Normalize && binningOptionsCompare.SumAllIntensitiesForBin == binningOptions.SumAllIntensitiesForBin)
                            {
                                validExistingResultsFound = true;
                            }
                            else
                            {
                                validExistingResultsFound = false;
                            }
                        }

                        if (validExistingResultsFound)
                        {
                            // Check if the Custom MZ options match
                            if ((customSICListCompare.RawTextMZList ?? "") == (masicOptions.CustomSICList.RawTextMZList ?? "") && (customSICListCompare.RawTextMZToleranceDaList ?? "") == (masicOptions.CustomSICList.RawTextMZToleranceDaList ?? "") && (customSICListCompare.RawTextScanOrAcqTimeCenterList ?? "") == (masicOptions.CustomSICList.RawTextScanOrAcqTimeCenterList ?? "") && (customSICListCompare.RawTextScanOrAcqTimeToleranceList ?? "") == (masicOptions.CustomSICList.RawTextScanOrAcqTimeToleranceList ?? "") && clsUtilities.ValuesMatch(customSICListCompare.ScanOrAcqTimeTolerance, masicOptions.CustomSICList.ScanOrAcqTimeTolerance) && customSICListCompare.ScanToleranceType == masicOptions.CustomSICList.ScanToleranceType)
                            {
                                validExistingResultsFound = true;
                            }
                            else
                            {
                                validExistingResultsFound = false;
                            }
                        }

                        if (validExistingResultsFound)
                        {
                            // All of the options match, make sure the other output files exist
                            validExistingResultsFound = false;
                            filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.ScanStatsFlatFile);
                            if (!File.Exists(filePathToCheck))
                                break;
                            filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.SICStatsFlatFile);
                            if (!File.Exists(filePathToCheck))
                                break;
                            filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.BPIFile);
                            if (!File.Exists(filePathToCheck))
                                break;
                            validExistingResultsFound = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError("There may be a programming error in CheckForExistingResults", ex);
                validExistingResultsFound = false;
            }

            return validExistingResultsFound;
        }

        public static string ConstructOutputFilePath(string inputFileName, string outputDirectoryPath, eOutputFileTypeConstants eFileType, int fragTypeNumber = 1)
        {
            string outputFilePath;
            outputFilePath = Path.Combine(outputDirectoryPath, Path.GetFileNameWithoutExtension(inputFileName));
            switch (eFileType)
            {
                case eOutputFileTypeConstants.XMLFile:
                    outputFilePath += "_SICs.xml";
                    break;
                case eOutputFileTypeConstants.ScanStatsFlatFile:
                    outputFilePath += "_ScanStats.txt";
                    break;
                case eOutputFileTypeConstants.ScanStatsExtendedFlatFile:
                    outputFilePath += "_ScanStatsEx.txt";
                    break;
                case eOutputFileTypeConstants.ScanStatsExtendedConstantFlatFile:
                    outputFilePath += "_ScanStatsConstant.txt";
                    break;
                case eOutputFileTypeConstants.SICStatsFlatFile:
                    // ReSharper disable once StringLiteralTypo
                    outputFilePath += "_SICstats.txt";
                    break;
                case eOutputFileTypeConstants.BPIFile:
                    outputFilePath += "_BPI.txt";
                    break;
                case eOutputFileTypeConstants.FragBPIFile:
                    outputFilePath += "_Frag" + fragTypeNumber.ToString() + "_BPI.txt";
                    break;
                case eOutputFileTypeConstants.TICFile:
                    outputFilePath += "_TIC.txt";
                    break;
                case eOutputFileTypeConstants.ICRToolsBPIChromatogramByScan:
                    outputFilePath += "_BPI_Scan.tic";
                    break;
                case eOutputFileTypeConstants.ICRToolsBPIChromatogramByTime:
                    outputFilePath += "_BPI_Time.tic";
                    break;
                case eOutputFileTypeConstants.ICRToolsTICChromatogramByScan:
                    outputFilePath += "_TIC_Scan.tic";
                    break;
                case eOutputFileTypeConstants.ICRToolsFragTICChromatogramByScan:
                    outputFilePath += "_TIC_MSMS_Scan.tic";
                    break;
                case eOutputFileTypeConstants.DeconToolsMSChromatogramFile:
                    outputFilePath += "_MS_scans.csv";
                    break;
                case eOutputFileTypeConstants.DeconToolsMSMSChromatogramFile:
                    outputFilePath += "_MSMS_scans.csv";
                    break;
                case eOutputFileTypeConstants.PEKFile:
                    outputFilePath += ".pek";
                    break;
                case eOutputFileTypeConstants.HeaderGlossary:
                    outputFilePath = Path.Combine(outputDirectoryPath, "Header_Glossary_Readme.txt");
                    break;
                case eOutputFileTypeConstants.DeconToolsIsosFile:
                    outputFilePath += "_isos.csv";
                    break;
                case eOutputFileTypeConstants.DeconToolsScansFile:
                    outputFilePath += "_scans.csv";
                    break;
                case eOutputFileTypeConstants.MSMethodFile:
                    outputFilePath += "_MSMethod";
                    break;
                case eOutputFileTypeConstants.MSTuneFile:
                    outputFilePath += "_MSTuneSettings";
                    break;
                case eOutputFileTypeConstants.ReporterIonsFile:
                    outputFilePath += "_ReporterIons.txt";
                    break;
                case eOutputFileTypeConstants.MRMSettingsFile:
                    outputFilePath += "_MRMSettings.txt";
                    break;
                case eOutputFileTypeConstants.MRMDatafile:
                    outputFilePath += "_MRMData.txt";
                    break;
                case eOutputFileTypeConstants.MRMCrosstabFile:
                    outputFilePath += "_MRMCrosstab.txt";
                    break;
                case eOutputFileTypeConstants.DatasetInfoFile:
                    outputFilePath += "_DatasetInfo.xml";
                    break;
                case eOutputFileTypeConstants.SICDataFile:
                    outputFilePath += "_SICdata.txt";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(eFileType), "Unknown Output File Type found in clsDataOutput.ConstructOutputFilePath");
                    break;
            }

            return outputFilePath;
        }

        public bool CreateDatasetInfoFile(string inputFileName, string outputDirectoryPath, clsScanTracking scanTracking, DatasetFileInfo datasetFileInfo)
        {
            var sampleInfo = new SampleInfo();
            try
            {
                string datasetName = Path.GetFileNameWithoutExtension(inputFileName);
                string datasetInfoFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.DatasetInfoFile);
                var datasetStatsSummarizer = new clsDatasetStatsSummarizer();
                bool success = datasetStatsSummarizer.CreateDatasetInfoFile(datasetName, datasetInfoFilePath, scanTracking.ScanStats, datasetFileInfo, sampleInfo);
                if (success)
                {
                    return true;
                }

                ReportError("datasetStatsSummarizer.CreateDatasetInfoFile, error from DataStatsSummarizer: " + datasetStatsSummarizer.ErrorMessage, new Exception("DataStatsSummarizer error " + datasetStatsSummarizer.ErrorMessage));
                return false;
            }
            catch (Exception ex)
            {
                ReportError("Error creating dataset info file", ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }
        }

        public string GetHeadersForOutputFile(clsScanList scanList, eOutputFileTypeConstants eOutputFileType)
        {
            return GetHeadersForOutputFile(scanList, eOutputFileType, ControlChars.Tab);
        }

        public string GetHeadersForOutputFile(clsScanList scanList, eOutputFileTypeConstants eOutputFileType, char cColDelimiter)
        {
            List<string> headerNames;
            switch (eOutputFileType)
            {
                case eOutputFileTypeConstants.ScanStatsFlatFile:
                    headerNames = new List<string>() { "Dataset", "ScanNumber", "ScanTime", "ScanType", "TotalIonIntensity", "BasePeakIntensity", "BasePeakMZ", "BasePeakSignalToNoiseRatio", "IonCount", "IonCountRaw", "ScanTypeName" };
                    break;
                case eOutputFileTypeConstants.ScanStatsExtendedFlatFile:
                    if (!(ExtendedStatsWriter.ExtendedHeaderNameCount > 0))
                    {
                        List<int> nonConstantHeaderIDs = null;

                        // Lookup extended stats values that are constants for all scans
                        // The following will also remove the constant header values from htExtendedHeaderInfo
                        ExtendedStatsWriter.ExtractConstantExtendedHeaderValues(out nonConstantHeaderIDs, scanList.SurveyScans, scanList.FragScans, cColDelimiter);
                        headerNames = ExtendedStatsWriter.ConstructExtendedStatsHeaders();
                    }
                    else
                    {
                        headerNames = new List<string>();
                    }

                    break;

                case eOutputFileTypeConstants.SICStatsFlatFile:
                    headerNames = new List<string>() { "Dataset", "ParentIonIndex", "MZ", "SurveyScanNumber", "FragScanNumber", "OptimalPeakApexScanNumber", "PeakApexOverrideParentIonIndex", "CustomSICPeak", "PeakScanStart", "PeakScanEnd", "PeakScanMaxIntensity", "PeakMaxIntensity", "PeakSignalToNoiseRatio", "FWHMInScans", "PeakArea", "ParentIonIntensity", "PeakBaselineNoiseLevel", "PeakBaselineNoiseStDev", "PeakBaselinePointsUsed", "StatMomentsArea", "CenterOfMassScan", "PeakStDev", "PeakSkew", "PeakKSStat", "StatMomentsDataCountUsed", "InterferenceScore" };
                    if (mOptions.IncludeScanTimesInSICStatsFile)
                    {
                        headerNames.Add("SurveyScanTime");
                        headerNames.Add("FragScanTime");
                        headerNames.Add("OptimalPeakApexScanTime");
                    }

                    break;

                case eOutputFileTypeConstants.MRMSettingsFile:
                    headerNames = new List<string>() { "Parent_Index", "Parent_MZ", "Daughter_MZ", "MZ_Start", "MZ_End", "Scan_Count" };
                    break;
                case eOutputFileTypeConstants.MRMDatafile:
                    headerNames = new List<string>() { "Scan", "MRM_Parent_MZ", "MRM_Daughter_MZ", "MRM_Daughter_Intensity" };
                    break;
                default:
                    headerNames = new List<string>() { "Unknown header column names" };
                    break;
            }

            return string.Join(Conversions.ToString(cColDelimiter), headerNames);
        }

        public bool InitializeSICDetailsTextFile(string inputFilePathFull, string outputDirectoryPath)
        {
            string outputFilePath = string.Empty;
            try
            {
                outputFilePath = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.SICDataFile);
                OutputFileHandles.SICDataFile = new StreamWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read));

                // Write the header line
                OutputFileHandles.SICDataFile.WriteLine("Dataset" + ControlChars.Tab + "ParentIonIndex" + ControlChars.Tab + "FragScanIndex" + ControlChars.Tab + "ParentIonMZ" + ControlChars.Tab + "Scan" + ControlChars.Tab + "MZ" + ControlChars.Tab + "Intensity");
            }
            catch (Exception ex)
            {
                ReportError("Error initializing the XML output file: " + outputFilePath, ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }

        public void OpenOutputFileHandles(string inputFileName, string outputDirectoryPath, bool writeHeaders)
        {
            string outputFilePath;

            var withBlock = OutputFileHandles;

            // Scan Stats file
            outputFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.ScanStatsFlatFile);
            withBlock.ScanStats = new StreamWriter(outputFilePath, false);
            if (writeHeaders)
                withBlock.ScanStats.WriteLine(GetHeadersForOutputFile(null, eOutputFileTypeConstants.ScanStatsFlatFile));
            withBlock.MSMethodFilePathBase = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.MSMethodFile);
            withBlock.MSTuneFilePathBase = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.MSTuneFile);
        }

        public bool SaveHeaderGlossary(clsScanList scanList, string inputFileName, string outputDirectoryPath)
        {
            string outputFilePath = "?UndefinedFile?";
            try
            {
                outputFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.HeaderGlossary);
                ReportMessage("Saving Header Glossary to " + Path.GetFileName(outputFilePath));
                using (var writer = new StreamWriter(outputFilePath, false))
                {
                    // ScanStats
                    writer.WriteLine(ConstructOutputFilePath(string.Empty, string.Empty, eOutputFileTypeConstants.ScanStatsFlatFile) + ":");
                    writer.WriteLine(GetHeadersForOutputFile(scanList, eOutputFileTypeConstants.ScanStatsFlatFile));
                    writer.WriteLine();

                    // SICStats
                    writer.WriteLine(ConstructOutputFilePath(string.Empty, string.Empty, eOutputFileTypeConstants.SICStatsFlatFile) + ":");
                    writer.WriteLine(GetHeadersForOutputFile(scanList, eOutputFileTypeConstants.SICStatsFlatFile));
                    writer.WriteLine();

                    // ScanStatsExtended
                    string headers = GetHeadersForOutputFile(scanList, eOutputFileTypeConstants.ScanStatsExtendedFlatFile);
                    if (!string.IsNullOrWhiteSpace(headers))
                    {
                        writer.WriteLine(ConstructOutputFilePath(string.Empty, string.Empty, eOutputFileTypeConstants.ScanStatsExtendedFlatFile) + ":");
                        writer.WriteLine(headers);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError("Error writing the Header Glossary to: " + outputFilePath, ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }

        public bool SaveSICDataToText(clsSICOptions sicOptions, clsScanList scanList, int parentIonIndex, clsSICDetails sicDetails)
        {
            int fragScanIndex;
            string prefix;
            try
            {
                if (OutputFileHandles.SICDataFile is null)
                {
                    return true;
                }

                // Write the detailed SIC values for the given parent ion to the text file

                for (fragScanIndex = 0; fragScanIndex <= scanList.ParentIons[parentIonIndex].FragScanIndices.Count - 1; fragScanIndex++)
                {
                    // "Dataset  ParentIonIndex  FragScanIndex  ParentIonMZ
                    prefix = sicOptions.DatasetID.ToString() + ControlChars.Tab + parentIonIndex.ToString() + ControlChars.Tab + fragScanIndex.ToString() + ControlChars.Tab + StringUtilities.DblToString(scanList.ParentIons[parentIonIndex].MZ, 4) + ControlChars.Tab;
                    if (sicDetails.SICDataCount == 0)
                    {
                        // Nothing to write
                        OutputFileHandles.SICDataFile.WriteLine(prefix + "0" + ControlChars.Tab + "0" + ControlChars.Tab + "0");
                    }
                    else
                    {
                        foreach (var dataPoint in sicDetails.SICData)
                            OutputFileHandles.SICDataFile.WriteLine(prefix + dataPoint.ScanNumber + ControlChars.Tab + dataPoint.Mass + ControlChars.Tab + dataPoint.Intensity);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError("Error writing to detailed SIC data text file", ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }
    }
}