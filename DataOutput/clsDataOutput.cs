using System;
using System.Collections.Generic;
using System.IO;
using MASIC.DatasetStats;
using MASIC.Options;
using PRISM;

namespace MASIC.DataOutput
{
    public class clsDataOutput : clsMasicEventNotifier
    {
        public const string SCAN_STATS_FILE_SUFFIX = "_ScanStats.txt";

        public const string SIC_STATS_FILE_SUFFIX = "_SICstats.txt";

        public const string REPORTER_IONS_FILE_SUFFIX = "_ReporterIons.txt";

        public enum OutputFileTypeConstants
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

        private readonly MASICOptions mOptions;

        public clsOutputFileHandles OutputFileHandles { get; }

        public clsExtendedStatsWriter ExtendedStatsWriter { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsDataOutput(MASICOptions masicOptions)
        {
            mOptions = masicOptions;

            OutputFileHandles = new clsOutputFileHandles();
            RegisterEvents(OutputFileHandles);

            ExtendedStatsWriter = new clsExtendedStatsWriter(mOptions);
            RegisterEvents(ExtendedStatsWriter);
        }

        /// <summary>
        /// Check for existing results
        /// </summary>
        /// <param name="inputFilePathFull"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <param name="masicOptions"></param>
        /// <returns>True if existing results already exist for the given input file path, SIC Options, and Binning options</returns>
        public bool CheckForExistingResults(
            string inputFilePathFull,
            string outputDirectoryPath,
            MASICOptions masicOptions)
        {
            var sicOptionsCompare = new SICOptions();
            var binningOptionsCompare = new BinningOptions();

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
                var filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, OutputFileTypeConstants.XMLFile);

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
                    catch (Exception)
                    {
                        // Invalid XML file; do not continue
                        return false;
                    }

                    // If we get here, the file opened successfully
                    var rootElement = xmlDoc.DocumentElement;
                    if (rootElement?.Name == "SICData")
                    {
                        // See if the ProcessingComplete node has a value of True
                        var matchingNodeList = rootElement.GetElementsByTagName("ProcessingComplete");
                        if (matchingNodeList.Count != 1)
                            return false;

                        if (matchingNodeList.Item(0)?.InnerText.ToLower() != "true")
                            return false;

                        // Read the ProcessingSummary and populate
                        matchingNodeList = rootElement.GetElementsByTagName("ProcessingSummary");
                        if (matchingNodeList.Count != 1)
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
                                            peakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = (MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes)int.Parse(valueNode.InnerText);
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
                                            peakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = (MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes)int.Parse(valueNode.InnerText);
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
                        validExistingResultsFound = clsUtilities.ValuesMatch(sicOptionsCompare.SICTolerance, sicOptions.SICTolerance, 3) &&
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
                            clsUtilities.ValuesMatch(sicOptionsCompare.SpectrumSimilarityMinimum, sicOptions.SpectrumSimilarityMinimum);

                        if (validExistingResultsFound)
                        {
                            // Check if the binning options match
                            var binningOptions = masicOptions.BinningOptions;

                            validExistingResultsFound = clsUtilities.ValuesMatch(binningOptionsCompare.StartX, binningOptions.StartX) &&
                                clsUtilities.ValuesMatch(binningOptionsCompare.EndX, binningOptions.EndX) &&
                                clsUtilities.ValuesMatch(binningOptionsCompare.BinSize, binningOptions.BinSize) &&
                                binningOptionsCompare.MaximumBinCount == binningOptions.MaximumBinCount &&
                                clsUtilities.ValuesMatch(binningOptionsCompare.IntensityPrecisionPercent, binningOptions.IntensityPrecisionPercent) &&
                                binningOptionsCompare.Normalize == binningOptions.Normalize &&
                                binningOptionsCompare.SumAllIntensitiesForBin == binningOptions.SumAllIntensitiesForBin;
                        }

                        if (validExistingResultsFound)
                        {
                            // Check if the Custom MZ options match
                            validExistingResultsFound =
                                (customSICListCompare.RawTextMZList ?? "") == (masicOptions.CustomSICList.RawTextMZList ?? "") &&
                                (customSICListCompare.RawTextMZToleranceDaList ?? "") == (masicOptions.CustomSICList.RawTextMZToleranceDaList ?? "") &&
                                (customSICListCompare.RawTextScanOrAcqTimeCenterList ?? "") == (masicOptions.CustomSICList.RawTextScanOrAcqTimeCenterList ?? "") &&
                                (customSICListCompare.RawTextScanOrAcqTimeToleranceList ?? "") == (masicOptions.CustomSICList.RawTextScanOrAcqTimeToleranceList ?? "") &&
                                clsUtilities.ValuesMatch(customSICListCompare.ScanOrAcqTimeTolerance, masicOptions.CustomSICList.ScanOrAcqTimeTolerance) &&
                                customSICListCompare.ScanToleranceType == masicOptions.CustomSICList.ScanToleranceType;
                        }

                        if (validExistingResultsFound)
                        {
                            // All of the options match, make sure the other output files exist

                            filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, OutputFileTypeConstants.ScanStatsFlatFile);
                            if (!File.Exists(filePathToCheck))
                                return false;

                            filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, OutputFileTypeConstants.SICStatsFlatFile);
                            if (!File.Exists(filePathToCheck))
                                return false;

                            filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, OutputFileTypeConstants.BPIFile);
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
            OutputFileTypeConstants eFileType,
            int fragTypeNumber = 1)
        {
            var outputFilePath = Path.Combine(outputDirectoryPath, Path.GetFileNameWithoutExtension(inputFileName));
            switch (eFileType)
            {
                case OutputFileTypeConstants.XMLFile:
                    outputFilePath += "_SICs.xml";
                    break;
                case OutputFileTypeConstants.ScanStatsFlatFile:
                    outputFilePath += SCAN_STATS_FILE_SUFFIX;
                    break;
                case OutputFileTypeConstants.ScanStatsExtendedFlatFile:
                    outputFilePath += "_ScanStatsEx.txt";
                    break;
                case OutputFileTypeConstants.ScanStatsExtendedConstantFlatFile:
                    outputFilePath += "_ScanStatsConstant.txt";
                    break;
                case OutputFileTypeConstants.SICStatsFlatFile:
                    outputFilePath += SIC_STATS_FILE_SUFFIX;
                    break;
                case OutputFileTypeConstants.BPIFile:
                    outputFilePath += "_BPI.txt";
                    break;
                case OutputFileTypeConstants.FragBPIFile:
                    outputFilePath += "_Frag" + fragTypeNumber + "_BPI.txt";
                    break;
                case OutputFileTypeConstants.TICFile:
                    outputFilePath += "_TIC.txt";
                    break;
                case OutputFileTypeConstants.ICRToolsBPIChromatogramByScan:
                    outputFilePath += "_BPI_Scan.tic";
                    break;
                case OutputFileTypeConstants.ICRToolsBPIChromatogramByTime:
                    outputFilePath += "_BPI_Time.tic";
                    break;
                case OutputFileTypeConstants.ICRToolsTICChromatogramByScan:
                    outputFilePath += "_TIC_Scan.tic";
                    break;
                case OutputFileTypeConstants.ICRToolsFragTICChromatogramByScan:
                    outputFilePath += "_TIC_MSMS_Scan.tic";
                    break;
                case OutputFileTypeConstants.DeconToolsMSChromatogramFile:
                    outputFilePath += "_MS_scans.csv";
                    break;
                case OutputFileTypeConstants.DeconToolsMSMSChromatogramFile:
                    outputFilePath += "_MSMS_scans.csv";
                    break;
                case OutputFileTypeConstants.PEKFile:
                    outputFilePath += ".pek";
                    break;
                case OutputFileTypeConstants.HeaderGlossary:
                    outputFilePath = Path.Combine(outputDirectoryPath, "Header_Glossary_Readme.txt");
                    break;
                case OutputFileTypeConstants.DeconToolsIsosFile:
                    outputFilePath += "_isos.csv";
                    break;
                case OutputFileTypeConstants.DeconToolsScansFile:
                    outputFilePath += "_scans.csv";
                    break;
                case OutputFileTypeConstants.MSMethodFile:
                    outputFilePath += "_MSMethod";
                    break;
                case OutputFileTypeConstants.MSTuneFile:
                    outputFilePath += "_MSTuneSettings";
                    break;
                case OutputFileTypeConstants.ReporterIonsFile:
                    outputFilePath += REPORTER_IONS_FILE_SUFFIX;
                    break;
                case OutputFileTypeConstants.MRMSettingsFile:
                    outputFilePath += "_MRMSettings.txt";
                    break;
                case OutputFileTypeConstants.MRMDatafile:
                    outputFilePath += "_MRMData.txt";
                    break;
                case OutputFileTypeConstants.MRMCrosstabFile:
                    outputFilePath += "_MRMCrosstab.txt";
                    break;
                case OutputFileTypeConstants.DatasetInfoFile:
                    outputFilePath += "_DatasetInfo.xml";
                    break;
                case OutputFileTypeConstants.SICDataFile:
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
                var datasetInfoFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, OutputFileTypeConstants.DatasetInfoFile);

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
                ReportError("Error creating dataset info file", ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
                return false;
            }
        }

        public string GetHeadersForOutputFile(clsScanList scanList, OutputFileTypeConstants eOutputFileType)
        {
            return GetHeadersForOutputFile(scanList, eOutputFileType, '\t');
        }

        public string GetHeadersForOutputFile(
            clsScanList scanList, OutputFileTypeConstants eOutputFileType, char delimiter)
        {
            List<string> headerNames;

            switch (eOutputFileType)
            {
                case OutputFileTypeConstants.ScanStatsFlatFile:
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
                case OutputFileTypeConstants.ScanStatsExtendedFlatFile:
                    if (ExtendedStatsWriter.ExtendedHeaderNameCount <= 0)
                    {
                        // Lookup extended stats values that are constants for all scans
                        // The following will also remove the constant header values from htExtendedHeaderInfo
                        ExtendedStatsWriter.ExtractConstantExtendedHeaderValues(out _, scanList.SurveyScans, scanList.FragScans, delimiter);

                        headerNames = ExtendedStatsWriter.ConstructExtendedStatsHeaders();
                    }
                    else
                    {
                        headerNames = new List<string>();
                    }

                    break;

                case OutputFileTypeConstants.SICStatsFlatFile:
                    headerNames = new List<string>(31)
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

                case OutputFileTypeConstants.MRMSettingsFile:
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
                case OutputFileTypeConstants.MRMDatafile:
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
                outputFilePath = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, OutputFileTypeConstants.SICDataFile);

                OutputFileHandles.SICDataFile = new StreamWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read));

                var headerNames = new List<string>
                {
                    "Dataset",
                    "ParentIonIndex",
                    "FragScanIndex",
                    "ParentIonMZ",
                    "Scan",
                    "MZ",
                    "Intensity"
                };

                // Write the header line
                OutputFileHandles.SICDataFile.WriteLine(string.Join("\t", headerNames));
            }
            catch (Exception ex)
            {
                ReportError("Error initializing the XML output file: " + outputFilePath, ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
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
            var outputFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, OutputFileTypeConstants.ScanStatsFlatFile);
            OutputFileHandles.ScanStats = new StreamWriter(outputFilePath, false);
            if (writeHeaders)
                OutputFileHandles.ScanStats.WriteLine(GetHeadersForOutputFile(null, OutputFileTypeConstants.ScanStatsFlatFile));

            OutputFileHandles.MSMethodFilePathBase = ConstructOutputFilePath(inputFileName, outputDirectoryPath, OutputFileTypeConstants.MSMethodFile);
            OutputFileHandles.MSTuneFilePathBase = ConstructOutputFilePath(inputFileName, outputDirectoryPath, OutputFileTypeConstants.MSTuneFile);
        }

        public bool SaveHeaderGlossary(
            clsScanList scanList,
            string inputFileName,
            string outputDirectoryPath)
        {
            var outputFilePath = "?UndefinedFile?";

            try
            {
                outputFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, OutputFileTypeConstants.HeaderGlossary);
                ReportMessage("Saving Header Glossary to " + Path.GetFileName(outputFilePath));

                using var writer = new StreamWriter(outputFilePath, false);

                // ScanStats
                writer.WriteLine(ConstructOutputFilePath(string.Empty, string.Empty, OutputFileTypeConstants.ScanStatsFlatFile) + ":");
                writer.WriteLine(GetHeadersForOutputFile(scanList, OutputFileTypeConstants.ScanStatsFlatFile));
                writer.WriteLine();

                // SICStats
                writer.WriteLine(ConstructOutputFilePath(string.Empty, string.Empty, OutputFileTypeConstants.SICStatsFlatFile) + ":");
                writer.WriteLine(GetHeadersForOutputFile(scanList, OutputFileTypeConstants.SICStatsFlatFile));
                writer.WriteLine();

                // ScanStatsExtended
                var headers = GetHeadersForOutputFile(scanList, OutputFileTypeConstants.ScanStatsExtendedFlatFile);
                if (!string.IsNullOrWhiteSpace(headers))
                {
                    writer.WriteLine(ConstructOutputFilePath(string.Empty, string.Empty, OutputFileTypeConstants.ScanStatsExtendedFlatFile) + ":");
                    writer.WriteLine(headers);
                }
            }
            catch (Exception ex)
            {
                ReportError("Error writing the Header Glossary to: " + outputFilePath, ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }

        public bool SaveSICDataToText(
            SICOptions sicOptions,
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

                for (var fragScanIndex = 0; fragScanIndex < scanList.ParentIons[parentIonIndex].FragScanIndices.Count; fragScanIndex++)
                {
                    // "Dataset  ParentIonIndex  FragScanIndex  ParentIonMZ
                    var prefix = string.Format("{0}\t{1}\t{2}\t{3}",
                        sicOptions.DatasetID,
                        parentIonIndex,
                        fragScanIndex,
                        StringUtilities.DblToString(scanList.ParentIons[parentIonIndex].MZ, 4));

                    if (sicDetails.SICDataCount == 0)
                    {
                        // Nothing to write
                        OutputFileHandles.SICDataFile.WriteLine("{0}\t{1}\t{2}\t{3}", prefix, "0", "0", "0");
                    }
                    else
                    {
                        foreach (var dataPoint in sicDetails.SICData)
                        {
                            OutputFileHandles.SICDataFile.WriteLine(
                                "{0}\t{1}\t{2}\t{3}",
                                prefix,
                                dataPoint.ScanNumber,
                                dataPoint.Mass,
                                dataPoint.Intensity);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError("Error writing to detailed SIC data text file", ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }
    }
}
