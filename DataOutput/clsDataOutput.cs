using System;
using System.Collections.Generic;
using System.IO;
using MASIC.DatasetStats;
using PRISM;

namespace MASIC.DataOutput
{
    public class clsDataOutput : clsMasicEventNotifier
    {
        #region "Constants and Enums"

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

        #region "Properties"
        private readonly clsMASICOptions mOptions;

        public clsOutputFileHandles OutputFileHandles { get; }

        public clsExtendedStatsWriter ExtendedStatsWriter { get; }

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

        public bool CheckForExistingResults(
            string inputFilePathFull,
            string outputDirectoryPath,
            clsMASICOptions masicOptions)
        {
            // Returns True if existing results already exist for the given input file path, SIC Options, and Binning options

            var sicOptionsCompare = new clsSICOptions();
            var binningOptionsCompare = new clsBinningOptions();

            long sourceFileSizeBytes = 0;
            var sourceFilePathCheck = string.Empty;
            var masicVersion = string.Empty;
            var masicPeakFinderDllVersion = string.Empty;
            var sourceFileDateTimeCheck = string.Empty;

            var skipMSMSProcessing = false;

            var validExistingResultsFound = false;
            try
            {
                // Don't even look for the XML file if mSkipSICAndRawDataProcessing = True
                if (masicOptions.SkipSICAndRawDataProcessing)
                {
                    return false;
                }

                // Obtain the output XML filename
                var filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.XMLFile);

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
                        if (matchingNodeList == null || matchingNodeList.Count != 1)
                            return false;
                        if ((matchingNodeList.Item(0).InnerText.ToLower() ?? "") != "true")
                            return false;

                        // Read the ProcessingSummary and populate
                        matchingNodeList = rootElement.GetElementsByTagName("ProcessingSummary");
                        if (matchingNodeList == null || matchingNodeList.Count != 1)
                            return false;

                        foreach (System.Xml.XmlNode valueNode in matchingNodeList[0].ChildNodes)
                        {
                            switch (valueNode.Name)
                            {
                                case "DatasetID":
                                    sicOptionsCompare.DatasetID = int.Parse(valueNode.InnerText);
                                    break;
                                case "SourceFilePath":
                                    sourceFilePathCheck = valueNode.InnerText;
                                    break;
                                case "SourceFileDateTime":
                                    sourceFileDateTimeCheck = valueNode.InnerText;
                                    break;
                                case "SourceFileSizeBytes":
                                    sourceFileSizeBytes = long.Parse(valueNode.InnerText);
                                    break;
                                case "MASICVersion":
                                    masicVersion = valueNode.InnerText;
                                    break;
                                case "MASICPeakFinderDllVersion":
                                    masicPeakFinderDllVersion = valueNode.InnerText;
                                    break;
                                case "SkipMSMSProcessing":
                                    skipMSMSProcessing = bool.Parse(valueNode.InnerText);
                                    break;
                            }
                        }

                        if (masicVersion == null)
                            masicVersion = string.Empty;
                        if (masicPeakFinderDllVersion == null)
                            masicPeakFinderDllVersion = string.Empty;

                        // Check if the MASIC version matches
                        if ((masicVersion ?? "") != (masicOptions.MASICVersion ?? ""))
                            return false;

                        if ((masicPeakFinderDllVersion ?? "") != (masicOptions.PeakFinderVersion ?? ""))
                            return false;

                        // Check the dataset number
                        if (sicOptionsCompare.DatasetID != masicOptions.SICOptions.DatasetID)
                            return false;

                        // Check the filename in sourceFilePathCheck
                        if ((Path.GetFileName(sourceFilePathCheck) ?? "") != (Path.GetFileName(inputFilePathFull) ?? ""))
                            return false;

                        // Check if the source file stats match
                        var inputFileInfo = new FileInfo(inputFilePathFull);
                        var sourceFileDateTime = inputFileInfo.LastWriteTime;
                        if ((sourceFileDateTimeCheck ?? "") != (sourceFileDateTime.ToShortDateString() + " " + sourceFileDateTime.ToShortTimeString() ?? ""))
                            return false;
                        if (sourceFileSizeBytes != inputFileInfo.Length)
                            return false;

                        // Check that skipMSMSProcessing matches
                        if (skipMSMSProcessing != masicOptions.SkipMSMSProcessing)
                            return false;

                        // Read the ProcessingOptions and populate
                        matchingNodeList = rootElement.GetElementsByTagName("ProcessingOptions");
                        if (matchingNodeList == null || matchingNodeList.Count != 1)
                            return false;

                        foreach (System.Xml.XmlNode valueNode in matchingNodeList[0].ChildNodes)
                        {
                            switch (valueNode.Name)
                            {
                                case "SICToleranceDa":
                                    sicOptionsCompare.SICTolerance = double.Parse(valueNode.InnerText);            // Legacy name
                                    break;
                                case "SICTolerance":
                                    sicOptionsCompare.SICTolerance = double.Parse(valueNode.InnerText);
                                    break;
                                case "SICToleranceIsPPM":
                                    sicOptionsCompare.SICToleranceIsPPM = bool.Parse(valueNode.InnerText);
                                    break;
                                case "RefineReportedParentIonMZ":
                                    sicOptionsCompare.RefineReportedParentIonMZ = bool.Parse(valueNode.InnerText);
                                    break;
                                case "ScanRangeEnd":
                                    sicOptionsCompare.ScanRangeEnd = int.Parse(valueNode.InnerText);
                                    break;
                                case "ScanRangeStart":
                                    sicOptionsCompare.ScanRangeStart = int.Parse(valueNode.InnerText);
                                    break;
                                case "RTRangeEnd":
                                    sicOptionsCompare.RTRangeEnd = float.Parse(valueNode.InnerText);
                                    break;
                                case "RTRangeStart":
                                    sicOptionsCompare.RTRangeStart = float.Parse(valueNode.InnerText);
                                    break;
                                case "CompressMSSpectraData":
                                    sicOptionsCompare.CompressMSSpectraData = bool.Parse(valueNode.InnerText);
                                    break;
                                case "CompressMSMSSpectraData":
                                    sicOptionsCompare.CompressMSMSSpectraData = bool.Parse(valueNode.InnerText);
                                    break;
                                case "CompressToleranceDivisorForDa":
                                    sicOptionsCompare.CompressToleranceDivisorForDa = double.Parse(valueNode.InnerText);
                                    break;
                                case "CompressToleranceDivisorForPPM":
                                    sicOptionsCompare.CompressToleranceDivisorForPPM = double.Parse(valueNode.InnerText);
                                    break;
                                case "MaxSICPeakWidthMinutesBackward":
                                    sicOptionsCompare.MaxSICPeakWidthMinutesBackward = float.Parse(valueNode.InnerText);
                                    break;
                                case "MaxSICPeakWidthMinutesForward":
                                    sicOptionsCompare.MaxSICPeakWidthMinutesForward = float.Parse(valueNode.InnerText);
                                    break;
                                case "ReplaceSICZeroesWithMinimumPositiveValueFromMSData":
                                    sicOptionsCompare.ReplaceSICZeroesWithMinimumPositiveValueFromMSData = bool.Parse(valueNode.InnerText);
                                    break;
                                case "SaveSmoothedData":
                                    sicOptionsCompare.SaveSmoothedData = bool.Parse(valueNode.InnerText);
                                    break;
                                case "SimilarIonMZToleranceHalfWidth":
                                    sicOptionsCompare.SimilarIonMZToleranceHalfWidth = float.Parse(valueNode.InnerText);
                                    break;
                                case "SimilarIonToleranceHalfWidthMinutes":
                                    sicOptionsCompare.SimilarIonToleranceHalfWidthMinutes = float.Parse(valueNode.InnerText);
                                    break;
                                case "SpectrumSimilarityMinimum":
                                    sicOptionsCompare.SpectrumSimilarityMinimum = float.Parse(valueNode.InnerText);
                                    break;
                                default:
                                    var peakFinderOptions = sicOptionsCompare.SICPeakFinderOptions;
                                    switch (valueNode.Name)
                                    {
                                        case "IntensityThresholdFractionMax":
                                            peakFinderOptions.IntensityThresholdFractionMax = float.Parse(valueNode.InnerText);
                                            break;
                                        case "IntensityThresholdAbsoluteMinimum":
                                            peakFinderOptions.IntensityThresholdAbsoluteMinimum = float.Parse(valueNode.InnerText);
                                            break;
                                        case "SICNoiseThresholdMode":
                                            peakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = (MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)int.Parse(valueNode.InnerText);
                                            break;
                                        case "SICNoiseThresholdIntensity":
                                            peakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute = float.Parse(valueNode.InnerText);
                                            break;
                                        case "SICNoiseFractionLowIntensityDataToAverage":
                                            peakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage = float.Parse(valueNode.InnerText);
                                            break;
                                        case "SICNoiseMinimumSignalToNoiseRatio":
                                            peakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = float.Parse(valueNode.InnerText);
                                            break;
                                        case "MaxDistanceScansNoOverlap":
                                            peakFinderOptions.MaxDistanceScansNoOverlap = int.Parse(valueNode.InnerText);
                                            break;
                                        case "MaxAllowedUpwardSpikeFractionMax":
                                            peakFinderOptions.MaxAllowedUpwardSpikeFractionMax = float.Parse(valueNode.InnerText);
                                            break;
                                        case "InitialPeakWidthScansScaler":
                                            peakFinderOptions.InitialPeakWidthScansScaler = float.Parse(valueNode.InnerText);
                                            break;
                                        case "InitialPeakWidthScansMaximum":
                                            peakFinderOptions.InitialPeakWidthScansMaximum = int.Parse(valueNode.InnerText);
                                            break;
                                        case "FindPeaksOnSmoothedData":
                                            peakFinderOptions.FindPeaksOnSmoothedData = bool.Parse(valueNode.InnerText);
                                            break;
                                        case "SmoothDataRegardlessOfMinimumPeakWidth":
                                            peakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth = bool.Parse(valueNode.InnerText);
                                            break;
                                        case "UseButterworthSmooth":
                                            peakFinderOptions.UseButterworthSmooth = bool.Parse(valueNode.InnerText);
                                            break;
                                        case "ButterworthSamplingFrequency":
                                            peakFinderOptions.ButterworthSamplingFrequency = float.Parse(valueNode.InnerText);
                                            break;
                                        case "ButterworthSamplingFrequencyDoubledForSIMData":
                                            peakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData = bool.Parse(valueNode.InnerText);
                                            break;
                                        case "UseSavitzkyGolaySmooth":
                                            peakFinderOptions.UseSavitzkyGolaySmooth = bool.Parse(valueNode.InnerText);
                                            break;
                                        case "SavitzkyGolayFilterOrder":
                                            peakFinderOptions.SavitzkyGolayFilterOrder = short.Parse(valueNode.InnerText);
                                            break;
                                        case "MassSpectraNoiseThresholdMode":
                                            peakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = (MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)int.Parse(valueNode.InnerText);
                                            break;
                                        case "MassSpectraNoiseThresholdIntensity":
                                            peakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute = float.Parse(valueNode.InnerText);
                                            break;
                                        case "MassSpectraNoiseFractionLowIntensityDataToAverage":
                                            peakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = float.Parse(valueNode.InnerText);
                                            break;
                                        case "MassSpectraNoiseMinimumSignalToNoiseRatio":
                                            peakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = float.Parse(valueNode.InnerText);
                                            break;
                                    }

                                    break;
                            }
                        }

                        // Read the BinningOptions and populate
                        matchingNodeList = rootElement.GetElementsByTagName("BinningOptions");
                        if (matchingNodeList == null || matchingNodeList.Count != 1)
                            return false;

                        foreach (System.Xml.XmlNode valueNode in matchingNodeList[0].ChildNodes)
                        {
                            switch (valueNode.Name)
                            {
                                case "BinStartX":
                                    binningOptionsCompare.StartX = float.Parse(valueNode.InnerText);
                                    break;
                                case "BinEndX":
                                    binningOptionsCompare.EndX = float.Parse(valueNode.InnerText);
                                    break;
                                case "BinSize":
                                    binningOptionsCompare.BinSize = float.Parse(valueNode.InnerText);
                                    break;
                                case "MaximumBinCount":
                                    binningOptionsCompare.MaximumBinCount = int.Parse(valueNode.InnerText);
                                    break;
                                case "IntensityPrecisionPercent":
                                    binningOptionsCompare.IntensityPrecisionPercent = float.Parse(valueNode.InnerText);
                                    break;
                                case "Normalize":
                                    binningOptionsCompare.Normalize = bool.Parse(valueNode.InnerText);
                                    break;
                                case "SumAllIntensitiesForBin":
                                    binningOptionsCompare.SumAllIntensitiesForBin = bool.Parse(valueNode.InnerText);
                                    break;
                            }
                        }

                        // Read the CustomSICValues and populate

                        var customSICListCompare = new clsCustomSICList();

                        matchingNodeList = rootElement.GetElementsByTagName("CustomSICValues");
                        if (matchingNodeList == null || matchingNodeList.Count != 1)
                        {
                            // Custom values not defined; that's OK
                        }
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
                                        customSICListCompare.ScanOrAcqTimeTolerance = float.Parse(valueNode.InnerText);
                                        break;
                                    case "ScanType":
                                        customSICListCompare.ScanToleranceType = masicOptions.GetScanToleranceTypeFromText(valueNode.InnerText);
                                        break;
                                }
                            }
                        }

                        var sicOptions = masicOptions.SICOptions;

                        // Check if the processing options match
                        if (clsUtilities.ValuesMatch(sicOptionsCompare.SICTolerance, sicOptions.SICTolerance, 3) &&
                            sicOptionsCompare.SICToleranceIsPPM == sicOptions.SICToleranceIsPPM &&
                            sicOptionsCompare.RefineReportedParentIonMZ == sicOptions.RefineReportedParentIonMZ &&
                            sicOptionsCompare.ScanRangeStart == sicOptions.ScanRangeStart &&
                            sicOptionsCompare.ScanRangeEnd == sicOptions.ScanRangeEnd &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.RTRangeStart, sicOptions.RTRangeStart, 2) &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.RTRangeEnd, sicOptions.RTRangeEnd, 2) &&
                            sicOptionsCompare.CompressMSSpectraData == sicOptions.CompressMSSpectraData &&
                            sicOptionsCompare.CompressMSMSSpectraData == sicOptions.CompressMSMSSpectraData &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.CompressToleranceDivisorForDa, sicOptions.CompressToleranceDivisorForDa, 2) &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.CompressToleranceDivisorForPPM, sicOptions.CompressToleranceDivisorForDa, 2) &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.MaxSICPeakWidthMinutesBackward, sicOptions.MaxSICPeakWidthMinutesBackward, 2) &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.MaxSICPeakWidthMinutesForward, sicOptions.MaxSICPeakWidthMinutesForward, 2) &&
                            sicOptionsCompare.ReplaceSICZeroesWithMinimumPositiveValueFromMSData == sicOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.IntensityThresholdFractionMax, sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax) &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum, sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum) &&
                            sicOptionsCompare.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode == sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute, sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute) &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage) &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio, sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio) &&
                            sicOptionsCompare.SICPeakFinderOptions.MaxDistanceScansNoOverlap == sicOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax, sicOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax) &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.InitialPeakWidthScansScaler, sicOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler) &&
                            sicOptionsCompare.SICPeakFinderOptions.InitialPeakWidthScansMaximum == sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum &&
                            sicOptionsCompare.SICPeakFinderOptions.FindPeaksOnSmoothedData == sicOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData &&
                            sicOptionsCompare.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth == sicOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth &&
                            sicOptionsCompare.SICPeakFinderOptions.UseButterworthSmooth == sicOptions.SICPeakFinderOptions.UseButterworthSmooth &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.ButterworthSamplingFrequency, sicOptions.SICPeakFinderOptions.ButterworthSamplingFrequency) &&
                            sicOptionsCompare.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData == sicOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData &&
                            sicOptionsCompare.SICPeakFinderOptions.UseSavitzkyGolaySmooth == sicOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth &&
                            sicOptionsCompare.SICPeakFinderOptions.SavitzkyGolayFilterOrder == sicOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder &&
                            sicOptionsCompare.SaveSmoothedData == sicOptions.SaveSmoothedData &&
                            sicOptionsCompare.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode == sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute) &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage) &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio) &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SimilarIonMZToleranceHalfWidth, sicOptions.SimilarIonMZToleranceHalfWidth) &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SimilarIonToleranceHalfWidthMinutes, sicOptions.SimilarIonToleranceHalfWidthMinutes) &&
                            clsUtilities.ValuesMatch(sicOptionsCompare.SpectrumSimilarityMinimum, sicOptions.SpectrumSimilarityMinimum))
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

                            if (clsUtilities.ValuesMatch(binningOptionsCompare.StartX, binningOptions.StartX) &&
                                clsUtilities.ValuesMatch(binningOptionsCompare.EndX, binningOptions.EndX) &&
                                clsUtilities.ValuesMatch(binningOptionsCompare.BinSize, binningOptions.BinSize) &&
                                binningOptionsCompare.MaximumBinCount == binningOptions.MaximumBinCount &&
                                clsUtilities.ValuesMatch(binningOptionsCompare.IntensityPrecisionPercent, binningOptions.IntensityPrecisionPercent) &&
                                binningOptionsCompare.Normalize == binningOptions.Normalize &&
                                binningOptionsCompare.SumAllIntensitiesForBin == binningOptions.SumAllIntensitiesForBin)
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
                            if ((customSICListCompare.RawTextMZList ?? "") == (masicOptions.CustomSICList.RawTextMZList ?? "") &&
                                (customSICListCompare.RawTextMZToleranceDaList ?? "") == (masicOptions.CustomSICList.RawTextMZToleranceDaList ?? "") &&
                                (customSICListCompare.RawTextScanOrAcqTimeCenterList ?? "") == (masicOptions.CustomSICList.RawTextScanOrAcqTimeCenterList ?? "") &&
                                (customSICListCompare.RawTextScanOrAcqTimeToleranceList ?? "") == (masicOptions.CustomSICList.RawTextScanOrAcqTimeToleranceList ?? "") &&
                                clsUtilities.ValuesMatch(customSICListCompare.ScanOrAcqTimeTolerance, masicOptions.CustomSICList.ScanOrAcqTimeTolerance) &&
                                customSICListCompare.ScanToleranceType == masicOptions.CustomSICList.ScanToleranceType)
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
                                return false;

                            filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.SICStatsFlatFile);
                            if (!File.Exists(filePathToCheck))
                                return false;

                            filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.BPIFile);
                            if (!File.Exists(filePathToCheck))
                                return false;

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

        public static string ConstructOutputFilePath(
            string inputFileName,
            string outputDirectoryPath,
            eOutputFileTypeConstants eFileType,
            int fragTypeNumber = 1)
        {
            var outputFilePath = Path.Combine(outputDirectoryPath, Path.GetFileNameWithoutExtension(inputFileName));
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
            }

            return outputFilePath;
        }

        public bool CreateDatasetInfoFile(
            string inputFileName,
            string outputDirectoryPath,
            clsScanTracking scanTracking,
            DatasetFileInfo datasetFileInfo)
        {
            var sampleInfo = new SampleInfo();
            try
            {
                var datasetName = Path.GetFileNameWithoutExtension(inputFileName);
                var datasetInfoFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.DatasetInfoFile);

                var datasetStatsSummarizer = new clsDatasetStatsSummarizer();

                var success = datasetStatsSummarizer.CreateDatasetInfoFile(
                    datasetName, datasetInfoFilePath,
                    scanTracking.ScanStats, datasetFileInfo, sampleInfo);

                if (success)
                {
                    return true;
                }

                ReportError("datasetStatsSummarizer.CreateDatasetInfoFile, error from DataStatsSummarizer: " + datasetStatsSummarizer.ErrorMessage,
                    new Exception("DataStatsSummarizer error " + datasetStatsSummarizer.ErrorMessage));

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
            return GetHeadersForOutputFile(scanList, eOutputFileType, '\t');
        }

        public string GetHeadersForOutputFile(
            clsScanList scanList, eOutputFileTypeConstants eOutputFileType, char delimiter)
        {
            List<string> headerNames;

            switch (eOutputFileType)
            {
                case eOutputFileTypeConstants.ScanStatsFlatFile:
                    headerNames = new List<string>()
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
                    break;
                case eOutputFileTypeConstants.ScanStatsExtendedFlatFile:
                    if (!(ExtendedStatsWriter.ExtendedHeaderNameCount > 0))
                    {
                        List<int> nonConstantHeaderIDs = null;

                        // Lookup extended stats values that are constants for all scans
                        // The following will also remove the constant header values from htExtendedHeaderInfo
                        ExtendedStatsWriter.ExtractConstantExtendedHeaderValues(out nonConstantHeaderIDs, scanList.SurveyScans, scanList.FragScans, delimiter);

                        headerNames = ExtendedStatsWriter.ConstructExtendedStatsHeaders();
                    }
                    else
                    {
                        headerNames = new List<string>();
                    }

                    break;

                case eOutputFileTypeConstants.SICStatsFlatFile:
                    headerNames = new List<string>()
                    {
                        "Dataset",
                        "ParentIonIndex",
                        "MZ",
                        "SurveyScanNumber",
                        "FragScanNumber",
                        "OptimalPeakApexScanNumber",
                        "PeakApexOverrideParentIonIndex",
                        "CustomSICPeak",
                        "PeakScanStart",
                        "PeakScanEnd",
                        "PeakScanMaxIntensity",
                        "PeakMaxIntensity",
                        "PeakSignalToNoiseRatio",
                        "FWHMInScans",
                        "PeakArea",
                        "ParentIonIntensity",
                        "PeakBaselineNoiseLevel",
                        "PeakBaselineNoiseStDev",
                        "PeakBaselinePointsUsed",
                        "StatMomentsArea",
                        "CenterOfMassScan",
                        "PeakStDev",
                        "PeakSkew",
                        "PeakKSStat",
                        "StatMomentsDataCountUsed",
                        "InterferenceScore"
                    };

                    if (mOptions.IncludeScanTimesInSICStatsFile)
                    {
                        headerNames.Add("SurveyScanTime");
                        headerNames.Add("FragScanTime");
                        headerNames.Add("OptimalPeakApexScanTime");
                    }

                    break;

                case eOutputFileTypeConstants.MRMSettingsFile:
                    headerNames = new List<string>()
                    {
                        "Parent_Index",
                        "Parent_MZ",
                        "Daughter_MZ",
                        "MZ_Start",
                        "MZ_End",
                        "Scan_Count"
                    };
                    break;
                case eOutputFileTypeConstants.MRMDatafile:
                    headerNames = new List<string>()
                    {
                        "Scan",
                        "MRM_Parent_MZ",
                        "MRM_Daughter_MZ",
                        "MRM_Daughter_Intensity"
                    };
                    break;
                default:
                    headerNames = new List<string>()
                    {
                        "Unknown header column names"
                    };
                    break;
            }

            return string.Join(delimiter.ToString(), headerNames);
        }

        public bool InitializeSICDetailsTextFile(
            string inputFilePathFull,
            string outputDirectoryPath)
        {
            var outputFilePath = string.Empty;

            try
            {
                outputFilePath = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.SICDataFile);

                OutputFileHandles.SICDataFile = new StreamWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read));

                // Write the header line
                OutputFileHandles.SICDataFile.WriteLine("Dataset" + "\t" +
                    "ParentIonIndex" + "\t" +
                    "FragScanIndex" + "\t" +
                    "ParentIonMZ" + "\t" +
                    "Scan" + "\t" +
                    "MZ" + "\t" +
                    "Intensity");
            }
            catch (Exception ex)
            {
                ReportError("Error initializing the XML output file: " + outputFilePath, ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }

        public void OpenOutputFileHandles(
            string inputFileName,
            string outputDirectoryPath,
            bool writeHeaders)
        {
            // Scan Stats file
            var outputFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.ScanStatsFlatFile);
            OutputFileHandles.ScanStats = new StreamWriter(outputFilePath, false);
            if (writeHeaders)
                OutputFileHandles.ScanStats.WriteLine(GetHeadersForOutputFile(null, eOutputFileTypeConstants.ScanStatsFlatFile));

            OutputFileHandles.MSMethodFilePathBase = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.MSMethodFile);
            OutputFileHandles.MSTuneFilePathBase = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.MSTuneFile);
        }

        public bool SaveHeaderGlossary(
            clsScanList scanList,
            string inputFileName,
            string outputDirectoryPath)
        {
            var outputFilePath = "?UndefinedFile?";

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
                    var headers = GetHeadersForOutputFile(scanList, eOutputFileTypeConstants.ScanStatsExtendedFlatFile);
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

        public bool SaveSICDataToText(
            clsSICOptions sicOptions,
            clsScanList scanList,
            int parentIonIndex,
            clsSICDetails sicDetails)
        {
            try
            {
                if (OutputFileHandles.SICDataFile == null)
                {
                    return true;
                }

                // Write the detailed SIC values for the given parent ion to the text file

                for (var fragScanIndex = 0; fragScanIndex <= scanList.ParentIons[parentIonIndex].FragScanIndices.Count - 1; fragScanIndex++)
                {
                    // "Dataset  ParentIonIndex  FragScanIndex  ParentIonMZ
                    var prefix = sicOptions.DatasetID.ToString() + "\t" +
                                 parentIonIndex.ToString() + "\t" +
                                 fragScanIndex.ToString() + "\t" +
                                 StringUtilities.DblToString(scanList.ParentIons[parentIonIndex].MZ, 4) + "\t";

                    if (sicDetails.SICDataCount == 0)
                    {
                        // Nothing to write
                        OutputFileHandles.SICDataFile.WriteLine(prefix + "0" + "\t" + "0" + "\t" + "0");
                    }
                    else
                    {
                        foreach (var dataPoint in sicDetails.SICData)
                            OutputFileHandles.SICDataFile.WriteLine(prefix +
                                                                    dataPoint.ScanNumber + "\t" +
                                                                    dataPoint.Mass + "\t" +
                                                                    dataPoint.Intensity);
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
