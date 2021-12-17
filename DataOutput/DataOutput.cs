using System;
using System.Collections.Generic;
using System.IO;
using MASIC.Data;
using MASIC.DatasetStats;
using MASIC.Options;
using PRISM;

namespace MASIC.DataOutput
{
    /// <summary>
    /// Methods for writing data to the scan stats, SIC stats, and report ions .txt files
    /// </summary>
    public class DataOutput : MasicEventNotifier
    {
        // Ignore Spelling: crosstab

        /// <summary>
        /// Scan stats file suffix
        /// </summary>
        public const string SCAN_STATS_FILE_SUFFIX = "_ScanStats.txt";

        /// <summary>
        /// SIC stats file suffix
        /// </summary>
        public const string SIC_STATS_FILE_SUFFIX = "_SICstats.txt";

        /// <summary>
        /// Reporter ions file suffix
        /// </summary>
        public const string REPORTER_IONS_FILE_SUFFIX = "_ReporterIons.txt";

        /// <summary>
        /// Output file types
        /// </summary>
        public enum OutputFileTypeConstants
        {
            /// <summary>
            /// XML results file
            /// </summary>
            XMLFile = 0,

            /// <summary>
            /// Scan stats file, _ScanStats.txt
            /// </summary>
            ScanStatsFlatFile = 1,

            /// <summary>
            /// Extended scan stats file, _ScanStatsEx.txt
            /// </summary>
            ScanStatsExtendedFlatFile = 2,

            /// <summary>
            /// Consolidated extended scan stats file, _ScanStatsConstant.txt
            /// </summary>
            ScanStatsExtendedConstantFlatFile = 3,

            // ReSharper disable once CommentTypo

            /// <summary>
            /// SIC stats file, _SICstats.txt
            /// </summary>
            SICStatsFlatFile = 4,

            /// <summary>
            /// BPI plot that includes all scans, _BPI_MS.png
            /// </summary>
            BPIFile = 5,

            /// <summary>
            /// BPI plot that only includes MSn scans, _BPI_MSn.png
            /// </summary>
            FragBPIFile = 6,

            /// <summary>
            /// TIC plot, _TIC.png
            /// </summary>
            TICFile = 7,

            /// <summary>
            /// ICR-2LS compatible _TIC_MSMS_Scan.tic file
            /// </summary>
            ICRToolsFragTICChromatogramByScan = 8,

            /// <summary>
            /// ICR-2LS compatible _BPI_Scan.tic file
            /// </summary>
            ICRToolsBPIChromatogramByScan = 9,

            /// <summary>
            /// ICR-2LS compatible _BPI_Time.tic file
            /// </summary>
            ICRToolsBPIChromatogramByTime = 10,

            /// <summary>
            /// ICR-2LS compatible _TIC_Scan.tic file
            /// </summary>
            ICRToolsTICChromatogramByScan = 11,

            /// <summary>
            /// ICR-2LS compatible .pek file
            /// </summary>
            PEKFile = 12,

            /// <summary>
            /// Header glossary file
            /// </summary>
            HeaderGlossary = 13,

            /// <summary>
            /// DeconTools compatible _scans.csv file
            /// </summary>
            DeconToolsScansFile = 14,

            /// <summary>
            /// DeconTools compatible _isos.csv file
            /// </summary>
            DeconToolsIsosFile = 15,

            /// <summary>
            /// DeconTools compatible _MS_scans.csv file
            /// </summary>
            DeconToolsMSChromatogramFile = 16,

            /// <summary>
            /// DeconTools compatible _MSMS_scans.csv file
            /// </summary>
            DeconToolsMSMSChromatogramFile = 17,

            /// <summary>
            /// MS Method file, _MSMethod.txt
            /// </summary>
            MSMethodFile = 18,

            /// <summary>
            /// MS tune file, _MSTuneSettings.txt
            /// </summary>
            MSTuneFile = 19,

            /// <summary>
            /// Reporter ions file, _ReporterIons.txt
            /// </summary>
            ReporterIonsFile = 20,

            /// <summary>
            /// MRM settings file, _MRMSettings
            /// </summary>
            MRMSettingsFile = 21,

            /// <summary>
            /// MRM data file, _MRMData.txt
            /// </summary>
            MRMDatafile = 22,

            /// <summary>
            /// MRM crosstab file, _MRMCrosstab.txt
            /// </summary>
            MRMCrosstabFile = 23,

            /// <summary>
            /// Dataset info file, _DatasetInfo.xml
            /// </summary>
            DatasetInfoFile = 24,

            /// <summary>
            /// SIC data file, _SICdata.txt
            /// </summary>
            SICDataFile = 25
        }

        private readonly MASICOptions mOptions;

        /// <summary>
        /// Output file handles
        /// </summary>
        public OutputFileHandles OutputFileHandles { get; }

        /// <summary>
        /// Extended scan stats writer
        /// </summary>
        public ExtendedStatsWriter ExtendedStatsWriter { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public DataOutput(MASICOptions masicOptions)
        {
            mOptions = masicOptions;

            OutputFileHandles = new OutputFileHandles();
            RegisterEvents(OutputFileHandles);

            ExtendedStatsWriter = new ExtendedStatsWriter(mOptions);
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

                        masicVersion ??= string.Empty;
                        masicPeakFinderDllVersion ??= string.Empty;

                        // Check if the MASIC version matches
                        if ((masicVersion ?? string.Empty) != (masicOptions.MASICVersion ?? string.Empty))
                            return false;

                        if ((masicPeakFinderDllVersion ?? string.Empty) != (masicOptions.PeakFinderVersion ?? string.Empty))
                            return false;

                        // Check the dataset number
                        if (sicOptionsCompare.DatasetID != masicOptions.SICOptions.DatasetID)
                            return false;

                        // Check the filename in sourceFilePathCheck
                        if ((Path.GetFileName(sourceFilePathCheck) ?? string.Empty) != (Path.GetFileName(inputFilePathFull) ?? string.Empty))
                            return false;

                        // Check if the source file stats match
                        var inputFileInfo = new FileInfo(inputFilePathFull);
                        var sourceFileDateTime = inputFileInfo.LastWriteTime;
                        if ((sourceFileDateTimeCheck ?? string.Empty) != (sourceFileDateTime.ToShortDateString() + " " + sourceFileDateTime.ToShortTimeString() ?? string.Empty))
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

                        var customSICListCompare = new CustomSICList();

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
                        validExistingResultsFound = Utilities.ValuesMatch(sicOptionsCompare.SICTolerance, sicOptions.SICTolerance, 3) &&
                            sicOptionsCompare.SICToleranceIsPPM == sicOptions.SICToleranceIsPPM &&
                            sicOptionsCompare.RefineReportedParentIonMZ == sicOptions.RefineReportedParentIonMZ &&
                            sicOptionsCompare.ScanRangeStart == sicOptions.ScanRangeStart &&
                            sicOptionsCompare.ScanRangeEnd == sicOptions.ScanRangeEnd &&
                            Utilities.ValuesMatch(sicOptionsCompare.RTRangeStart, sicOptions.RTRangeStart, 2) &&
                            Utilities.ValuesMatch(sicOptionsCompare.RTRangeEnd, sicOptions.RTRangeEnd, 2) &&
                            sicOptionsCompare.CompressMSSpectraData == sicOptions.CompressMSSpectraData &&
                            sicOptionsCompare.CompressMSMSSpectraData == sicOptions.CompressMSMSSpectraData &&
                            Utilities.ValuesMatch(sicOptionsCompare.CompressToleranceDivisorForDa, sicOptions.CompressToleranceDivisorForDa, 2) &&
                            Utilities.ValuesMatch(sicOptionsCompare.CompressToleranceDivisorForPPM, sicOptions.CompressToleranceDivisorForDa, 2) &&
                            Utilities.ValuesMatch(sicOptionsCompare.MaxSICPeakWidthMinutesBackward, sicOptions.MaxSICPeakWidthMinutesBackward, 2) &&
                            Utilities.ValuesMatch(sicOptionsCompare.MaxSICPeakWidthMinutesForward, sicOptions.MaxSICPeakWidthMinutesForward, 2) &&
                            sicOptionsCompare.ReplaceSICZeroesWithMinimumPositiveValueFromMSData == sicOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData &&
                            Utilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.IntensityThresholdFractionMax, sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax) &&
                            Utilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum, sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum) &&
                            sicOptionsCompare.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode == sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode &&
                            Utilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute, sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute) &&
                            Utilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage) &&
                            Utilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio, sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio) &&
                            sicOptionsCompare.SICPeakFinderOptions.MaxDistanceScansNoOverlap == sicOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap &&
                            Utilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax, sicOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax) &&
                            Utilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.InitialPeakWidthScansScaler, sicOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler) &&
                            sicOptionsCompare.SICPeakFinderOptions.InitialPeakWidthScansMaximum == sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum &&
                            sicOptionsCompare.SICPeakFinderOptions.FindPeaksOnSmoothedData == sicOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData &&
                            sicOptionsCompare.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth == sicOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth &&
                            sicOptionsCompare.SICPeakFinderOptions.UseButterworthSmooth == sicOptions.SICPeakFinderOptions.UseButterworthSmooth &&
                            Utilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.ButterworthSamplingFrequency, sicOptions.SICPeakFinderOptions.ButterworthSamplingFrequency) &&
                            sicOptionsCompare.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData == sicOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData &&
                            sicOptionsCompare.SICPeakFinderOptions.UseSavitzkyGolaySmooth == sicOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth &&
                            sicOptionsCompare.SICPeakFinderOptions.SavitzkyGolayFilterOrder == sicOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder &&
                            sicOptionsCompare.SaveSmoothedData == sicOptions.SaveSmoothedData &&
                            sicOptionsCompare.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode == sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode &&
                            Utilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute) &&
                            Utilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage) &&
                            Utilities.ValuesMatch(sicOptionsCompare.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio) &&
                            Utilities.ValuesMatch(sicOptionsCompare.SimilarIonMZToleranceHalfWidth, sicOptions.SimilarIonMZToleranceHalfWidth) &&
                            Utilities.ValuesMatch(sicOptionsCompare.SimilarIonToleranceHalfWidthMinutes, sicOptions.SimilarIonToleranceHalfWidthMinutes) &&
                            Utilities.ValuesMatch(sicOptionsCompare.SpectrumSimilarityMinimum, sicOptions.SpectrumSimilarityMinimum);

                        if (validExistingResultsFound)
                        {
                            // Check if the binning options match
                            var binningOptions = masicOptions.BinningOptions;

                            validExistingResultsFound = Utilities.ValuesMatch(binningOptionsCompare.StartX, binningOptions.StartX) &&
                                Utilities.ValuesMatch(binningOptionsCompare.EndX, binningOptions.EndX) &&
                                Utilities.ValuesMatch(binningOptionsCompare.BinSize, binningOptions.BinSize) &&
                                binningOptionsCompare.MaximumBinCount == binningOptions.MaximumBinCount &&
                                Utilities.ValuesMatch(binningOptionsCompare.IntensityPrecisionPercent, binningOptions.IntensityPrecisionPercent) &&
                                binningOptionsCompare.Normalize == binningOptions.Normalize &&
                                binningOptionsCompare.SumAllIntensitiesForBin == binningOptions.SumAllIntensitiesForBin;
                        }

                        if (validExistingResultsFound)
                        {
                            // Check if the Custom MZ options match
                            validExistingResultsFound =
                                (customSICListCompare.RawTextMZList ?? string.Empty) == (masicOptions.CustomSICList.RawTextMZList ?? string.Empty) &&
                                (customSICListCompare.RawTextMZToleranceDaList ?? string.Empty) == (masicOptions.CustomSICList.RawTextMZToleranceDaList ?? string.Empty) &&
                                (customSICListCompare.RawTextScanOrAcqTimeCenterList ?? string.Empty) == (masicOptions.CustomSICList.RawTextScanOrAcqTimeCenterList ?? string.Empty) &&
                                (customSICListCompare.RawTextScanOrAcqTimeToleranceList ?? string.Empty) == (masicOptions.CustomSICList.RawTextScanOrAcqTimeToleranceList ?? string.Empty) &&
                                Utilities.ValuesMatch(customSICListCompare.ScanOrAcqTimeTolerance, masicOptions.CustomSICList.ScanOrAcqTimeTolerance) &&
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

        /// <summary>
        /// Obtain the output file path for the given file type
        /// </summary>
        /// <param name="inputFileName"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <param name="fileType"></param>
        /// <param name="fragTypeNumber"></param>
        public static string ConstructOutputFilePath(
            string inputFileName,
            string outputDirectoryPath,
            OutputFileTypeConstants fileType,
            int fragTypeNumber = 1)
        {
            var outputFilePath = Path.Combine(outputDirectoryPath, Path.GetFileNameWithoutExtension(inputFileName));
            switch (fileType)
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
                    throw new ArgumentOutOfRangeException(nameof(fileType), "Unknown Output File Type found in clsDataOutput.ConstructOutputFilePath");
            }

            return outputFilePath;
        }

        /// <summary>
        /// Create the dataset info file
        /// </summary>
        /// <param name="inputFileName"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <param name="scanTracking"></param>
        /// <param name="datasetFileInfo"></param>
        /// <returns>True if success, false if an error</returns>
        public bool CreateDatasetInfoFile(
            string inputFileName,
            string outputDirectoryPath,
            ScanTracking scanTracking,
            DatasetFileInfo datasetFileInfo)
        {
            var sampleInfo = new SampleInfo();
            try
            {
                var datasetName = Path.GetFileNameWithoutExtension(inputFileName);
                var datasetInfoFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, OutputFileTypeConstants.DatasetInfoFile);

                var datasetStatsSummarizer = new DatasetStatsSummarizer();

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

        /// <summary>
        /// Get the header line for the given file type, tab delimited
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="outputFileType"></param>
        public string GetHeadersForOutputFile(ScanList scanList, OutputFileTypeConstants outputFileType)
        {
            return GetHeadersForOutputFile(scanList, outputFileType, '\t');
        }

        /// <summary>
        /// Get the header line for the given file type, using the specified delimiter
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="outputFileType"></param>
        /// <param name="delimiter"></param>
        public string GetHeadersForOutputFile(
            ScanList scanList, OutputFileTypeConstants outputFileType, char delimiter)
        {
            List<string> headerNames;

            switch (outputFileType)
            {
                case OutputFileTypeConstants.ScanStatsFlatFile:
                    headerNames = new List<string>
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
                    headerNames = new List<string>
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
                    headerNames = new List<string>
                    {
                        "Scan",
                        "MRM_Parent_MZ",
                        "MRM_Daughter_MZ",
                        "MRM_Daughter_Intensity"
                    };
                    break;
                default:
                    headerNames = new List<string>
                    {
                        "Unknown header column names"
                    };
                    break;
            }

            return string.Join(delimiter.ToString(), headerNames);
        }

        /// <summary>
        /// Initialize the SIC details file, _SICdata.txt
        /// </summary>
        /// <param name="inputFilePathFull"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <returns>True if success, false if an error</returns>
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

        /// <summary>
        /// Open the output file handles
        /// </summary>
        /// <param name="inputFileName"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <param name="writeHeaders"></param>
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

        /// <summary>
        /// Save the header glossary
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="inputFileName"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <returns>True if success, false if an error</returns>
        public bool SaveHeaderGlossary(
            ScanList scanList,
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

        /// <summary>
        /// Save SIC data to the SIC data file
        /// </summary>
        /// <param name="sicOptions"></param>
        /// <param name="scanList"></param>
        /// <param name="parentIonIndex"></param>
        /// <param name="sicDetails"></param>
        /// <returns>True if success, false if an error</returns>
        public bool SaveSICDataToText(
            SICOptions sicOptions,
            ScanList scanList,
            int parentIonIndex,
            SICDetails sicDetails)
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
