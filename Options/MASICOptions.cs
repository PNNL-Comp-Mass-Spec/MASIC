using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PRISM;

namespace MASIC.Options
{
    public class MASICOptions : clsMasicEventNotifier
    {
        #region "Constants and Enums"

        public const string XML_SECTION_DATABASE_SETTINGS = "MasicDatabaseSettings";
        public const string XML_SECTION_IMPORT_OPTIONS = "MasicImportOptions";
        public const string XML_SECTION_EXPORT_OPTIONS = "MasicExportOptions";
        public const string XML_SECTION_PLOT_OPTIONS = "PlotOptions";
        public const string XML_SECTION_SIC_OPTIONS = "SICOptions";
        public const string XML_SECTION_BINNING_OPTIONS = "BinningOptions";
        public const string XML_SECTION_MEMORY_OPTIONS = "MemoryOptions";
        public const string XML_SECTION_CUSTOM_SIC_VALUES = "CustomSICValues";

        public const string DEFAULT_MASIC_STATUS_FILE_NAME = "MasicStatus.xml";

        #endregion

        #region "Classwide Variables"

        /// <summary>
        /// Set options through the Property Functions or by passing parameterFilePath to ProcessFile()
        /// </summary>
        public SICOptions SICOptions { get; }

        /// <summary>
        /// Binning options for MS/MS spectra; only applies to spectrum similarity testing
        /// </summary>
        public BinningOptions BinningOptions { get; }

        public clsCustomSICList CustomSICList { get; }

        public bool AbortProcessing { get; set; }

        public string DatabaseConnectionString { get; set; }
        public string DatasetInfoQuerySql { get; set; }
        public string DatasetLookupFilePath { get; set; } = string.Empty;

        public bool IncludeHeadersInExportFile { get; set; }
        public bool IncludeScanTimesInSICStatsFile { get; set; }
        public bool FastExistingXMLFileTest { get; set; }

        /// <summary>
        /// Using this will reduce memory usage, but not as much as when mSkipSICAndRawDataProcessing = True
        /// </summary>
        public bool SkipMSMSProcessing { get; set; }

        /// <summary>
        /// Using this will drastically reduce memory usage since raw mass spec data is not retained
        /// </summary>
        public bool SkipSICAndRawDataProcessing { get; set; }

        /// <summary>
        /// When True, then will not create any SICs; automatically set to false if mSkipSICAndRawDataProcessing = True
        /// </summary>
        public bool ExportRawDataOnly { get; set; }

        public string OutputDirectoryPath { get; set; }

        [Obsolete("Use OutputDirectoryPath")]
        public string OutputFolderPath
        {
            get => OutputDirectoryPath;
            set => OutputDirectoryPath = value;
        }

        public bool WriteDetailedSICDataFile { get; set; }
        public bool WriteMSMethodFile { get; set; }
        public bool WriteMSTuneFile { get; set; }

        public bool WriteExtendedStats { get; set; }

        /// <summary>
        /// When enabled, the scan filter text will also be included in the extended stats file
        /// (e.g. ITMS + c NSI Full ms [300.00-2000.00] or ITMS + c NSI d Full ms2 756.98@35.00 [195.00-2000.00])
        /// </summary>
        public bool WriteExtendedStatsIncludeScanFilterText { get; set; }

        /// <summary>
        /// Adds a large number of additional columns with information like voltage, current, temperature, pressure, and gas flow rate
        /// If StatusLogKeyNameFilterList contains any entries, only the entries matching the specs in StatusLogKeyNameFilterList will be saved
        /// </summary>
        public bool WriteExtendedStatsStatusLog { get; set; }

        public bool ConsolidateConstantExtendedHeaderValues { get; set; }

        public bool WriteMRMDataList { get; set; }
        public bool WriteMRMIntensityCrosstab { get; set; }

        /// <summary>
        /// If this is true, then an error will not be raised if the input file contains no parent ions or no survey scans
        /// </summary>
        public bool SuppressNoParentIonsError { get; set; }

        public RawDataExportOptions RawDataExportOptions { get; }

        public clsReporterIons ReporterIons { get; }

        public bool CDFTimeInSeconds { get; set; }
        public double ParentIonDecoyMassDa { get; set; }

        public bool UseBase64DataEncoding { get; set; }

        public SpectrumCacheOptions CacheOptions { get; }
        public string MASICStatusFilename { get; set; } = DEFAULT_MASIC_STATUS_FILE_NAME;

        public string MASICVersion { get; }
        public string PeakFinderVersion { get; }

        public DateTime LastParentIonProcessingLogTime { get; set; }

        #endregion

        #region "Properties"

        public string StatusMessage { get; set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MASICOptions(string masicVersionInfo, string peakFinderVersionInfo)
        {
            MASICVersion = masicVersionInfo;
            PeakFinderVersion = peakFinderVersionInfo;

            CacheOptions = new SpectrumCacheOptions();

            CustomSICList = new clsCustomSICList();
            RegisterEvents(CustomSICList);

            RawDataExportOptions = new RawDataExportOptions();

            ReporterIons = new clsReporterIons();

            BinningOptions = new BinningOptions();

            SICOptions = new SICOptions();

            StatusLogKeyNameFilterList = new SortedSet<string>();
        }

        public clsCustomSICList.eCustomSICScanTypeConstants GetScanToleranceTypeFromText(string scanType)
        {
            if (scanType == null)
                scanType = string.Empty;

            var scanTypeTrimmed = scanType.Trim();

            if (string.Equals(scanTypeTrimmed, clsCustomSICList.CUSTOM_SIC_TYPE_RELATIVE, StringComparison.InvariantCultureIgnoreCase))
            {
                return clsCustomSICList.eCustomSICScanTypeConstants.Relative;
            }

            if (string.Equals(scanTypeTrimmed, clsCustomSICList.CUSTOM_SIC_TYPE_ACQUISITION_TIME, StringComparison.InvariantCultureIgnoreCase))
            {
                return clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime;
            }

            // Assume absolute
            return clsCustomSICList.eCustomSICScanTypeConstants.Absolute;
        }

        /// <summary>
        /// When WriteExtendedStatsStatusLog is true, a file with extra info like voltage, current, temperature, pressure, etc. is created
        /// Use this property to filter the items written in the StatusLog file to only include the entries in this list
        /// </summary>
        /// <returns></returns>
        public SortedSet<string> StatusLogKeyNameFilterList { get; }

        /// <summary>
        /// Returns the contents of StatusLogKeyNameFilterList
        /// </summary>
        /// <param name="commaSeparatedList">When true, returns a comma separated list; when false, returns a Newline separated list</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public string GetStatusLogKeyNameFilterListAsText(bool commaSeparatedList)
        {
            var delimiter = commaSeparatedList ? ", " : Environment.NewLine;

            return string.Join(delimiter, StatusLogKeyNameFilterList);
        }

        public void InitializeVariables()
        {
            AbortProcessing = false;

            DatasetLookupFilePath = string.Empty;
            DatabaseConnectionString = string.Empty;
            DatasetInfoQuerySql = string.Empty;

            IncludeHeadersInExportFile = true;
            IncludeScanTimesInSICStatsFile = false;
            FastExistingXMLFileTest = false;

            SkipMSMSProcessing = false;
            SkipSICAndRawDataProcessing = false;
            ExportRawDataOnly = false;

            SuppressNoParentIonsError = false;

            WriteMSMethodFile = true;
            WriteMSTuneFile = false;

            WriteDetailedSICDataFile = false;
            WriteExtendedStats = true;
            WriteExtendedStatsIncludeScanFilterText = true;
            WriteExtendedStatsStatusLog = true;
            ConsolidateConstantExtendedHeaderValues = true;

            SetStatusLogKeyNameFilterList("Source", ',');

            WriteMRMDataList = false;
            WriteMRMIntensityCrosstab = true;

            RawDataExportOptions.Reset();

            CDFTimeInSeconds = true;
            ParentIonDecoyMassDa = 0;

            // Enabling this gives files of nearly equivalent size, but with the data arrays base-64 encoded; thus, no advantage
            UseBase64DataEncoding = false;

            SICOptions.Reset();

            BinningOptions.Reset();

            CacheOptions.Reset();

            CustomSICList.Reset();
        }

        /// <summary>
        /// Load MASIC settings from the parameter file
        /// </summary>
        /// <param name="parameterFilePath"></param>
        /// <param name="inputFilePath"></param>
        /// <returns></returns>
        public bool LoadParameterFileSettings(string parameterFilePath, string inputFilePath = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(parameterFilePath))
                {
                    // No parameter file specified; nothing to load
                    ReportMessage("Parameter file not specified -- will use default settings");
                    return true;
                }

                ReportMessage("Loading parameter file: " + parameterFilePath);

                if (!File.Exists(parameterFilePath))
                {
                    // See if parameterFilePath points to a file in the same directory as the application
                    parameterFilePath = Path.Combine(PRISM.FileProcessor.ProcessFilesOrDirectoriesBase.GetAppDirectoryPath(), Path.GetFileName(parameterFilePath));
                    if (!File.Exists(parameterFilePath))
                    {
                        if (!string.IsNullOrWhiteSpace(inputFilePath))
                        {
                            // Also look in the same directory as the instrument data file
                            var instrumentDataFile = new FileInfo(inputFilePath);
                            if (instrumentDataFile.DirectoryName != null)
                            {
                                // ReSharper disable once AssignNullToNotNullAttribute
                                parameterFilePath = Path.Combine(instrumentDataFile.DirectoryName, Path.GetFileName(parameterFilePath));
                            }
                        }

                        if (!File.Exists(parameterFilePath))
                        {
                            ReportError("Parameter file not found: " + parameterFilePath);
                            return false;
                        }
                    }
                }

                var reader = new XmlSettingsFileAccessor();

                // Pass False to .LoadSettings() here to turn off case sensitive matching
                if (!reader.LoadSettings(parameterFilePath, false))
                {
                    ReportError("Error calling XmlSettingsFileAccessor.LoadSettings for " + parameterFilePath, clsMASIC.eMasicErrorCodes.InputFileDataReadError);
                    return false;
                }

                if (!reader.SectionPresent(XML_SECTION_DATABASE_SETTINGS))
                {
                    // Database settings section not found; that's ok
                }
                else
                {
                    DatabaseConnectionString = reader.GetParam(
                        XML_SECTION_DATABASE_SETTINGS, "ConnectionString", DatabaseConnectionString);

                    DatasetInfoQuerySql = reader.GetParam(
                        XML_SECTION_DATABASE_SETTINGS, "DatasetInfoQuerySql", DatasetInfoQuerySql);
                }

                if (!reader.SectionPresent(XML_SECTION_IMPORT_OPTIONS))
                {
                    // Import options section not found; that's ok
                }
                else
                {
                    CDFTimeInSeconds = reader.GetParam(
                        XML_SECTION_IMPORT_OPTIONS, "CDFTimeInSeconds", CDFTimeInSeconds);

                    ParentIonDecoyMassDa = reader.GetParam(
                        XML_SECTION_IMPORT_OPTIONS, "ParentIonDecoyMassDa", ParentIonDecoyMassDa);
                }

                // MASIC Export Options
                if (!reader.SectionPresent(XML_SECTION_EXPORT_OPTIONS))
                {
                    // Export options section not found; that's ok
                }
                else
                {
                    IncludeHeadersInExportFile = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "IncludeHeaders", IncludeHeadersInExportFile);

                    IncludeScanTimesInSICStatsFile = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "IncludeScanTimesInSICStatsFile", IncludeScanTimesInSICStatsFile);

                    SkipMSMSProcessing = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "SkipMSMSProcessing", SkipMSMSProcessing);

                    // Check for both "SkipSICProcessing" and "SkipSICAndRawDataProcessing" in the XML file
                    // If either is true, then mExportRawDataOnly will be auto-set to false in function ProcessFiles
                    SkipSICAndRawDataProcessing = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "SkipSICProcessing", SkipSICAndRawDataProcessing);

                    SkipSICAndRawDataProcessing = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "SkipSICAndRawDataProcessing", SkipSICAndRawDataProcessing);

                    ExportRawDataOnly = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ExportRawDataOnly", ExportRawDataOnly);

                    SuppressNoParentIonsError = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "SuppressNoParentIonsError", SuppressNoParentIonsError);

                    WriteDetailedSICDataFile = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "WriteDetailedSICDataFile", WriteDetailedSICDataFile);

                    WriteMSMethodFile = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "WriteMSMethodFile", WriteMSMethodFile);

                    WriteMSTuneFile = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "WriteMSTuneFile", WriteMSTuneFile);

                    WriteExtendedStats = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "WriteExtendedStats", WriteExtendedStats);

                    WriteExtendedStatsIncludeScanFilterText = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "WriteExtendedStatsIncludeScanFilterText", WriteExtendedStatsIncludeScanFilterText);

                    WriteExtendedStatsStatusLog = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "WriteExtendedStatsStatusLog", WriteExtendedStatsStatusLog);

                    var filterList = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "StatusLogKeyNameFilterList", string.Empty);
                    if (!string.IsNullOrEmpty(filterList))
                    {
                        SetStatusLogKeyNameFilterList(filterList, ',');
                    }

                    ConsolidateConstantExtendedHeaderValues = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ConsolidateConstantExtendedHeaderValues", ConsolidateConstantExtendedHeaderValues);

                    WriteMRMDataList = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "WriteMRMDataList", WriteMRMDataList);

                    WriteMRMIntensityCrosstab = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "WriteMRMIntensityCrosstab", WriteMRMIntensityCrosstab);

                    FastExistingXMLFileTest = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "FastExistingXMLFileTest", FastExistingXMLFileTest);

                    ReporterIons.ReporterIonStatsEnabled = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ReporterIonStatsEnabled", ReporterIons.ReporterIonStatsEnabled);

                    var eReporterIonMassMode = (clsReporterIons.eReporterIonMassModeConstants)reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ReporterIonMassMode", (int)ReporterIons.ReporterIonMassMode);

                    ReporterIons.ReporterIonToleranceDaDefault = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ReporterIonToleranceDa", ReporterIons.ReporterIonToleranceDaDefault);

                    ReporterIons.ReporterIonApplyAbundanceCorrection = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ReporterIonApplyAbundanceCorrection", ReporterIons.ReporterIonApplyAbundanceCorrection);

                    ReporterIons.ReporterIonITraq4PlexCorrectionFactorType =
                        (clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex)reader.GetParam(
                            XML_SECTION_EXPORT_OPTIONS,
                            "ReporterIonITraq4PlexCorrectionFactorType",
                            (int)ReporterIons.ReporterIonITraq4PlexCorrectionFactorType);

                    ReporterIons.ReporterIonSaveObservedMasses = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ReporterIonSaveObservedMasses", ReporterIons.ReporterIonSaveObservedMasses);

                    ReporterIons.ReporterIonSaveUncorrectedIntensities = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ReporterIonSaveUncorrectedIntensities", ReporterIons.ReporterIonSaveUncorrectedIntensities);

                    ReporterIons.SetReporterIonMassMode(eReporterIonMassMode,
                                                        ReporterIons.ReporterIonToleranceDaDefault);

                    // Raw data export options
                    RawDataExportOptions.ExportEnabled = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ExportRawSpectraData", RawDataExportOptions.ExportEnabled);

                    RawDataExportOptions.FileFormat =
                        (RawDataExportOptions.eExportRawDataFileFormatConstants)reader.GetParam(
                            XML_SECTION_EXPORT_OPTIONS,
                            "ExportRawDataFileFormat",
                            (int)RawDataExportOptions.FileFormat);

                    RawDataExportOptions.IncludeMSMS = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ExportRawDataIncludeMSMS", RawDataExportOptions.IncludeMSMS);

                    RawDataExportOptions.RenumberScans = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ExportRawDataRenumberScans", RawDataExportOptions.RenumberScans);

                    RawDataExportOptions.MinimumSignalToNoiseRatio = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ExportRawDataMinimumSignalToNoiseRatio", RawDataExportOptions.MinimumSignalToNoiseRatio);

                    RawDataExportOptions.MaxIonCountPerScan = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ExportRawDataMaxIonCountPerScan", RawDataExportOptions.MaxIonCountPerScan);

                    RawDataExportOptions.IntensityMinimum = reader.GetParam(
                        XML_SECTION_EXPORT_OPTIONS, "ExportRawDataIntensityMinimum", RawDataExportOptions.IntensityMinimum);
                }

                if (!reader.SectionPresent(XML_SECTION_SIC_OPTIONS))
                {
                    var errorMessage = "The node '<section name=" + "\"" + XML_SECTION_SIC_OPTIONS +
                                       "\"" + "> was not found in the parameter file: " +
                                       parameterFilePath;
                    ReportError(errorMessage);
                    return false;
                }

                // SIC Options
                // Note: Skipping .DatasetID since this must be provided at the command line or through the Property Function interface

                // Preferentially use "SICTolerance", if it is present
                var sicTolerance = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "SICTolerance", SICOptions.GetSICTolerance(), out var notPresent);

                if (notPresent)
                {
                    // Check for "SICToleranceDa", which is a legacy setting
                    sicTolerance = reader.GetParam(
                        XML_SECTION_SIC_OPTIONS, "SICToleranceDa", SICOptions.SICToleranceDa, out notPresent);

                    if (!notPresent)
                    {
                        SICOptions.SetSICTolerance(sicTolerance, false);
                    }
                }
                else
                {
                    var sicToleranceIsPPM = reader.GetParam(XML_SECTION_SIC_OPTIONS, "SICToleranceIsPPM", false);

                    SICOptions.SetSICTolerance(sicTolerance, sicToleranceIsPPM);
                }

                SICOptions.RefineReportedParentIonMZ = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "RefineReportedParentIonMZ", SICOptions.RefineReportedParentIonMZ);

                SICOptions.ScanRangeStart = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "ScanRangeStart", SICOptions.ScanRangeStart);

                SICOptions.ScanRangeEnd = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "ScanRangeEnd", SICOptions.ScanRangeEnd);

                SICOptions.RTRangeStart = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "RTRangeStart", SICOptions.RTRangeStart);

                SICOptions.RTRangeEnd = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "RTRangeEnd", SICOptions.RTRangeEnd);

                SICOptions.CompressMSSpectraData = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "CompressMSSpectraData", SICOptions.CompressMSSpectraData);

                SICOptions.CompressMSMSSpectraData = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "CompressMSMSSpectraData", SICOptions.CompressMSMSSpectraData);

                SICOptions.CompressToleranceDivisorForDa = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "CompressToleranceDivisorForDa", SICOptions.CompressToleranceDivisorForDa);

                SICOptions.CompressToleranceDivisorForPPM = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "CompressToleranceDivisorForPPM", SICOptions.CompressToleranceDivisorForPPM);

                SICOptions.MaxSICPeakWidthMinutesBackward = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "MaxSICPeakWidthMinutesBackward", SICOptions.MaxSICPeakWidthMinutesBackward);

                SICOptions.MaxSICPeakWidthMinutesForward = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "MaxSICPeakWidthMinutesForward", SICOptions.MaxSICPeakWidthMinutesForward);

                SICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "IntensityThresholdFractionMax", SICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax);

                SICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "IntensityThresholdAbsoluteMinimum", SICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum);

                // Peak Finding Options
                var sicNoiseThresholdMode = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "SICNoiseThresholdMode", (int)SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode);

                SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode =
                    (MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)sicNoiseThresholdMode;

                SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "SICNoiseThresholdIntensity", SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute);

                SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "SICNoiseFractionLowIntensityDataToAverage", SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage);

                // This value isn't utilized by MASIC for SICs so we'll force it to always be zero
                SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = 0;

                SICOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "MaxDistanceScansNoOverlap", SICOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap);

                SICOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "MaxAllowedUpwardSpikeFractionMax", SICOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax);

                SICOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "InitialPeakWidthScansScaler", SICOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler);

                SICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "InitialPeakWidthScansMaximum", SICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum);

                SICOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "FindPeaksOnSmoothedData", SICOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData);

                SICOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "SmoothDataRegardlessOfMinimumPeakWidth", SICOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth);

                SICOptions.SICPeakFinderOptions.UseButterworthSmooth = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "UseButterworthSmooth", SICOptions.SICPeakFinderOptions.UseButterworthSmooth);

                SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequency = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "ButterworthSamplingFrequency", SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequency);

                SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "ButterworthSamplingFrequencyDoubledForSIMData", SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData);

                SICOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "UseSavitzkyGolaySmooth", SICOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth);

                SICOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "SavitzkyGolayFilterOrder", SICOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder);

                SICOptions.SaveSmoothedData = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "SaveSmoothedData", SICOptions.SaveSmoothedData);

                var massSpectraNoiseThresholdMode = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS,
                    "MassSpectraNoiseThresholdMode",
                    (int)SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode);

                SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode =
                    (MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)massSpectraNoiseThresholdMode;

                SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseThresholdIntensity", SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute);

                SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseFractionLowIntensityDataToAverage", SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage);

                SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseMinimumSignalToNoiseRatio ", SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio);

                SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "ReplaceSICZeroesWithMinimumPositiveValueFromMSData", SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData);

                // Similarity Options
                SICOptions.SimilarIonMZToleranceHalfWidth = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "SimilarIonMZToleranceHalfWidth", SICOptions.SimilarIonMZToleranceHalfWidth);

                SICOptions.SimilarIonToleranceHalfWidthMinutes = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "SimilarIonToleranceHalfWidthMinutes", SICOptions.SimilarIonToleranceHalfWidthMinutes);

                SICOptions.SpectrumSimilarityMinimum = reader.GetParam(
                    XML_SECTION_SIC_OPTIONS, "SpectrumSimilarityMinimum", SICOptions.SpectrumSimilarityMinimum);

                // Binning Options
                if (!reader.SectionPresent(XML_SECTION_BINNING_OPTIONS))
                {
                    var errorMessage = "The node '<section name=" + "\"" +
                                       XML_SECTION_BINNING_OPTIONS + "\"" +
                                       "> was not found in the parameter file: " + parameterFilePath;
                    ReportError(errorMessage);

                    SetBaseClassErrorCode(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes.InvalidParameterFile);
                    return false;
                }

                BinningOptions.StartX = reader.GetParam(
                    XML_SECTION_BINNING_OPTIONS, "BinStartX", BinningOptions.StartX);

                BinningOptions.EndX = reader.GetParam(
                    XML_SECTION_BINNING_OPTIONS, "BinEndX", BinningOptions.EndX);

                BinningOptions.BinSize = reader.GetParam(
                    XML_SECTION_BINNING_OPTIONS, "BinSize", BinningOptions.BinSize);

                BinningOptions.MaximumBinCount = reader.GetParam(
                    XML_SECTION_BINNING_OPTIONS, "MaximumBinCount", BinningOptions.MaximumBinCount);

                BinningOptions.IntensityPrecisionPercent = reader.GetParam(
                    XML_SECTION_BINNING_OPTIONS, "IntensityPrecisionPercent", BinningOptions.IntensityPrecisionPercent);

                BinningOptions.Normalize = reader.GetParam(
                    XML_SECTION_BINNING_OPTIONS, "Normalize", BinningOptions.Normalize);

                BinningOptions.SumAllIntensitiesForBin = reader.GetParam(
                    XML_SECTION_BINNING_OPTIONS, "SumAllIntensitiesForBin", BinningOptions.SumAllIntensitiesForBin);

                // Memory management options
                CacheOptions.DiskCachingAlwaysDisabled = reader.GetParam(
                    XML_SECTION_MEMORY_OPTIONS, "DiskCachingAlwaysDisabled", CacheOptions.DiskCachingAlwaysDisabled);

                CacheOptions.DirectoryPath = reader.GetParam(
                    XML_SECTION_MEMORY_OPTIONS, "CacheFolderPath", CacheOptions.DirectoryPath);

                CacheOptions.DirectoryPath = reader.GetParam(
                    XML_SECTION_MEMORY_OPTIONS, "CacheDirectoryPath", CacheOptions.DirectoryPath);

                CacheOptions.SpectraToRetainInMemory = reader.GetParam(
                    XML_SECTION_MEMORY_OPTIONS, "CacheSpectraToRetainInMemory", CacheOptions.SpectraToRetainInMemory);

                if (!reader.SectionPresent(XML_SECTION_CUSTOM_SIC_VALUES))
                {
                    // Custom SIC values section not found; that's ok
                    // No more settings to load so return true
                    return true;
                }

                CustomSICList.LimitSearchToCustomMZList = reader.GetParam(
                    XML_SECTION_CUSTOM_SIC_VALUES, "LimitSearchToCustomMZList", CustomSICList.LimitSearchToCustomMZList);

                var scanType = reader.GetParam(
                    XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", string.Empty);

                var scanTolerance = reader.GetParam(
                    XML_SECTION_CUSTOM_SIC_VALUES, "ScanTolerance", string.Empty);

                CustomSICList.ScanToleranceType = GetScanToleranceTypeFromText(scanType);

                if (scanTolerance.Length > 0 && clsUtilities.IsNumber(scanTolerance))
                {
                    if (CustomSICList.ScanToleranceType == clsCustomSICList.eCustomSICScanTypeConstants.Absolute)
                    {
                        CustomSICList.ScanOrAcqTimeTolerance = int.Parse(scanTolerance);
                    }
                    else
                    {
                        // Includes .Relative and .AcquisitionTime
                        CustomSICList.ScanOrAcqTimeTolerance = float.Parse(scanTolerance);
                    }
                }
                else
                {
                    CustomSICList.ScanOrAcqTimeTolerance = 0;
                }

                CustomSICList.CustomSICListFileName = reader.GetParam(
                        XML_SECTION_CUSTOM_SIC_VALUES, "CustomMZFile", string.Empty);

                if (CustomSICList.CustomSICListFileName.Length > 0)
                {
                    // Clear mCustomSICList; we'll read the data from the file when ProcessFile is called()

                    CustomSICList.ResetMzSearchValues();

                    return true;
                }

                var mzList = reader.GetParam(
                    XML_SECTION_CUSTOM_SIC_VALUES, "MZList", string.Empty);

                var mzToleranceDaList = reader.GetParam(
                    XML_SECTION_CUSTOM_SIC_VALUES, "MZToleranceDaList", string.Empty);

                var scanCenterList = reader.GetParam(
                    XML_SECTION_CUSTOM_SIC_VALUES, "ScanCenterList", string.Empty);

                var scanToleranceList = reader.GetParam(
                    XML_SECTION_CUSTOM_SIC_VALUES, "ScanToleranceList", string.Empty);

                var scanCommentList = reader.GetParam(
                    XML_SECTION_CUSTOM_SIC_VALUES, "ScanCommentList", string.Empty);

                var success = CustomSICList.ParseCustomSICList(mzList, mzToleranceDaList,
                    scanCenterList, scanToleranceList,
                    scanCommentList);

                return success;
            }
            catch (Exception ex)
            {
                ReportError("Error in LoadParameterFileSettings", ex, clsMASIC.eMasicErrorCodes.InputFileDataReadError);
                return false;
            }
        }

        public bool SaveParameterFileSettings(string parameterFilePath)
        {
            var writer = new XmlSettingsFileAccessor();

            try
            {
                if (string.IsNullOrEmpty(parameterFilePath))
                {
                    // No parameter file specified; unable to save
                    ReportError("Empty parameter file path sent to SaveParameterFileSettings");
                    return false;
                }

                // Pass True to .LoadSettings() here so that newly made Xml files will have the correct capitalization
                if (!writer.LoadSettings(parameterFilePath, true))
                {
                    ReportError("LoadSettings returned false while initializing " + parameterFilePath);
                    return false;
                }

                // Database settings
                writer.SetParam(XML_SECTION_DATABASE_SETTINGS, "ConnectionString", DatabaseConnectionString);
                writer.SetParam(XML_SECTION_DATABASE_SETTINGS, "DatasetInfoQuerySql", DatasetInfoQuerySql);

                // Import Options
                writer.SetParam(XML_SECTION_IMPORT_OPTIONS, "CDFTimeInSeconds", CDFTimeInSeconds);
                writer.SetParam(XML_SECTION_IMPORT_OPTIONS, "ParentIonDecoyMassDa", ParentIonDecoyMassDa);

                // MASIC Export Options
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "IncludeHeaders", IncludeHeadersInExportFile);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "IncludeScanTimesInSICStatsFile", IncludeScanTimesInSICStatsFile);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "SkipMSMSProcessing", SkipMSMSProcessing);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "SkipSICAndRawDataProcessing", SkipSICAndRawDataProcessing);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataOnly", ExportRawDataOnly);

                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "SuppressNoParentIonsError", SuppressNoParentIonsError);

                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteExtendedStats", WriteExtendedStats);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteExtendedStatsIncludeScanFilterText", WriteExtendedStatsIncludeScanFilterText);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteExtendedStatsStatusLog", WriteExtendedStatsStatusLog);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "StatusLogKeyNameFilterList", GetStatusLogKeyNameFilterListAsText(true));
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ConsolidateConstantExtendedHeaderValues", ConsolidateConstantExtendedHeaderValues);

                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteDetailedSICDataFile", WriteDetailedSICDataFile);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMSMethodFile", WriteMSMethodFile);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMSTuneFile", WriteMSTuneFile);

                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMRMDataList", WriteMRMDataList);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMRMIntensityCrosstab", WriteMRMIntensityCrosstab);

                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "FastExistingXMLFileTest", FastExistingXMLFileTest);

                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonStatsEnabled", ReporterIons.ReporterIonStatsEnabled);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonMassMode", (int)ReporterIons.ReporterIonMassMode);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonToleranceDa", ReporterIons.ReporterIonToleranceDaDefault);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonApplyAbundanceCorrection", ReporterIons.ReporterIonApplyAbundanceCorrection);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonITraq4PlexCorrectionFactorType", (int)ReporterIons.ReporterIonITraq4PlexCorrectionFactorType);

                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonSaveObservedMasses", ReporterIons.ReporterIonSaveObservedMasses);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonSaveUncorrectedIntensities", ReporterIons.ReporterIonSaveUncorrectedIntensities);

                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawSpectraData", RawDataExportOptions.ExportEnabled);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataFileFormat", (int)RawDataExportOptions.FileFormat);

                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataIncludeMSMS", RawDataExportOptions.IncludeMSMS);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataRenumberScans", RawDataExportOptions.RenumberScans);

                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataMinimumSignalToNoiseRatio", RawDataExportOptions.MinimumSignalToNoiseRatio);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataMaxIonCountPerScan", RawDataExportOptions.MaxIonCountPerScan);
                writer.SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataIntensityMinimum", RawDataExportOptions.IntensityMinimum);

                // SIC Options
                // Note: Skipping .DatasetID since this must be provided at the command line or through the Property Function interface

                // "SICToleranceDa" is a legacy parameter.  If the SIC tolerance is in PPM, then "SICToleranceDa" is the Da tolerance at 1000 m/z
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "SICToleranceDa", SICOptions.SICToleranceDa.ToString("0.0000"));

                var sicTolerance = SICOptions.GetSICTolerance(out var sicToleranceIsPPM);
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "SICTolerance", sicTolerance.ToString("0.0000"));
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "SICToleranceIsPPM", sicToleranceIsPPM.ToString());

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "RefineReportedParentIonMZ", SICOptions.RefineReportedParentIonMZ);
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "ScanRangeStart", SICOptions.ScanRangeStart);
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "ScanRangeEnd", SICOptions.ScanRangeEnd);
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "RTRangeStart", SICOptions.RTRangeStart);
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "RTRangeEnd", SICOptions.RTRangeEnd);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "CompressMSSpectraData", SICOptions.CompressMSSpectraData);
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "CompressMSMSSpectraData", SICOptions.CompressMSMSSpectraData);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "CompressToleranceDivisorForDa", SICOptions.CompressToleranceDivisorForDa);
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "CompressToleranceDivisorForPPM", SICOptions.CompressToleranceDivisorForPPM);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "MaxSICPeakWidthMinutesBackward", SICOptions.MaxSICPeakWidthMinutesBackward);
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "MaxSICPeakWidthMinutesForward", SICOptions.MaxSICPeakWidthMinutesForward);
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "IntensityThresholdFractionMax", StringUtilities.DblToString(SICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax, 5));
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "IntensityThresholdAbsoluteMinimum", SICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum);

                // Peak Finding Options
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseThresholdMode", (int)SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseThresholdIntensity", SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseFractionLowIntensityDataToAverage", StringUtilities.DblToString(SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, 5));

                // This value isn't utilized by MASIC for SICs so we'll force it to always be zero
                SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = 0;

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseMinimumSignalToNoiseRatio", SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "MaxDistanceScansNoOverlap", SICOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "MaxAllowedUpwardSpikeFractionMax", StringUtilities.DblToString(SICOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax, 5));

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "InitialPeakWidthScansScaler", SICOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "InitialPeakWidthScansMaximum", SICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "FindPeaksOnSmoothedData", SICOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "SmoothDataRegardlessOfMinimumPeakWidth", SICOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "UseButterworthSmooth", SICOptions.SICPeakFinderOptions.UseButterworthSmooth);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "ButterworthSamplingFrequency", StringUtilities.DblToString(SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequency, 5));

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "ButterworthSamplingFrequencyDoubledForSIMData", SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "UseSavitzkyGolaySmooth", SICOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "SavitzkyGolayFilterOrder", SICOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "SaveSmoothedData", SICOptions.SaveSmoothedData);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseThresholdMode", (int)SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseThresholdIntensity", SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseFractionLowIntensityDataToAverage", StringUtilities.DblToString(SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage, 5));

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseMinimumSignalToNoiseRatio", SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "ReplaceSICZeroesWithMinimumPositiveValueFromMSData", SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData);

                // Similarity Options
                writer.SetParam(XML_SECTION_SIC_OPTIONS, "SimilarIonMZToleranceHalfWidth", SICOptions.SimilarIonMZToleranceHalfWidth);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "SimilarIonToleranceHalfWidthMinutes", SICOptions.SimilarIonToleranceHalfWidthMinutes);

                writer.SetParam(XML_SECTION_SIC_OPTIONS, "SpectrumSimilarityMinimum", SICOptions.SpectrumSimilarityMinimum);

                // Binning Options
                writer.SetParam(XML_SECTION_BINNING_OPTIONS, "BinStartX", BinningOptions.StartX);
                writer.SetParam(XML_SECTION_BINNING_OPTIONS, "BinEndX", BinningOptions.EndX);
                writer.SetParam(XML_SECTION_BINNING_OPTIONS, "BinSize", BinningOptions.BinSize);
                writer.SetParam(XML_SECTION_BINNING_OPTIONS, "MaximumBinCount", BinningOptions.MaximumBinCount);

                writer.SetParam(XML_SECTION_BINNING_OPTIONS, "IntensityPrecisionPercent", BinningOptions.IntensityPrecisionPercent);
                writer.SetParam(XML_SECTION_BINNING_OPTIONS, "Normalize", BinningOptions.Normalize);
                writer.SetParam(XML_SECTION_BINNING_OPTIONS, "SumAllIntensitiesForBin", BinningOptions.SumAllIntensitiesForBin);

                // Memory management options
                writer.SetParam(XML_SECTION_MEMORY_OPTIONS, "DiskCachingAlwaysDisabled", CacheOptions.DiskCachingAlwaysDisabled);
                writer.SetParam(XML_SECTION_MEMORY_OPTIONS, "CacheDirectoryPath", CacheOptions.DirectoryPath);
                writer.SetParam(XML_SECTION_MEMORY_OPTIONS, "CacheSpectraToRetainInMemory", CacheOptions.SpectraToRetainInMemory);

                // Plot options
                writer.SetParam(XML_SECTION_PLOT_OPTIONS, "CreatePlots", CacheOptions.DiskCachingAlwaysDisabled);
                writer.SetParam(XML_SECTION_MEMORY_OPTIONS, "CacheDirectoryPath", CacheOptions.DirectoryPath);
                writer.SetParam(XML_SECTION_MEMORY_OPTIONS, "CacheSpectraToRetainInMemory", CacheOptions.SpectraToRetainInMemory);

                // Construct the rawText strings using mCustomSICList
                var scanCommentsDefined = false;

                var mzValues = new List<string>(CustomSICList.CustomMZSearchValues.Count);
                var mzTolerances = new List<string>(CustomSICList.CustomMZSearchValues.Count);
                var scanCenters = new List<string>(CustomSICList.CustomMZSearchValues.Count);
                var scanTolerances = new List<string>(CustomSICList.CustomMZSearchValues.Count);
                var comments = new List<string>(CustomSICList.CustomMZSearchValues.Count);

                foreach (var mzSearchValue in CustomSICList.CustomMZSearchValues)
                {
                    mzValues.Add(mzSearchValue.MZ.ToString(CultureInfo.InvariantCulture));
                    mzTolerances.Add(mzSearchValue.MZToleranceDa.ToString(CultureInfo.InvariantCulture));

                    scanCenters.Add(mzSearchValue.ScanOrAcqTimeCenter.ToString(CultureInfo.InvariantCulture));
                    scanTolerances.Add(mzSearchValue.ScanOrAcqTimeTolerance.ToString(CultureInfo.InvariantCulture));

                    if (mzSearchValue.Comment == null)
                    {
                        comments.Add(string.Empty);
                    }
                    else
                    {
                        if (mzSearchValue.Comment.Length > 0)
                        {
                            scanCommentsDefined = true;
                        }

                        comments.Add(mzSearchValue.Comment);
                    }
                }

                writer.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "MZList", string.Join("\t", mzValues));
                writer.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "MZToleranceDaList", string.Join("\t", mzTolerances));

                writer.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCenterList", string.Join("\t", scanCenters));
                writer.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanToleranceList", string.Join("\t", scanTolerances));

                if (scanCommentsDefined)
                {
                    writer.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCommentList", string.Join("\t", comments));
                }
                else
                {
                    writer.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCommentList", string.Empty);
                }

                writer.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanTolerance", CustomSICList.ScanOrAcqTimeTolerance.ToString(CultureInfo.InvariantCulture));

                switch (CustomSICList.ScanToleranceType)
                {
                    case clsCustomSICList.eCustomSICScanTypeConstants.Relative:
                        writer.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", clsCustomSICList.CUSTOM_SIC_TYPE_RELATIVE);
                        break;
                    case clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime:
                        writer.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", clsCustomSICList.CUSTOM_SIC_TYPE_ACQUISITION_TIME);
                        break;
                    default:
                        // Assume absolute
                        writer.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", clsCustomSICList.CUSTOM_SIC_TYPE_ABSOLUTE);
                        break;
                }

                writer.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "CustomMZFile", CustomSICList.CustomSICListFileName);

                writer.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "LimitSearchToCustomMZList", CustomSICList.LimitSearchToCustomMZList);

                writer.SaveSettings();
            }
            catch (Exception ex)
            {
                ReportError("Error in SaveParameterFileSettings", ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }

        /// <summary>
        /// When WriteExtendedStatsStatusLog is true, a file with extra info like voltage, current, temperature, pressure, etc. is created
        /// Use this method to filter the items written in the StatusLog file to only include the entries in matchSpecList
        /// </summary>
        /// <param name="matchSpecList"></param>
        public void SetStatusLogKeyNameFilterList(List<string> matchSpecList)
        {
            try
            {
                StatusLogKeyNameFilterList.Clear();

                if (matchSpecList != null)
                {
                    var query = (from item in matchSpecList select item).Distinct();
                    foreach (var item in query)
                        StatusLogKeyNameFilterList.Add(item);
                }
            }
            catch (Exception ex)
            {
                // Ignore errors here
            }
        }

        public void SetStatusLogKeyNameFilterList(string matchSpecList, char delimiter)
        {
            try
            {
                // Split on the user-specified delimiter, plus also CR and LF
                var items = matchSpecList.Split(delimiter, '\r', '\n').ToList();

                var validatedItems = new List<string>(items.Count);

                if (items.Count > 0)
                {
                    // Populate validatedItems using any non-blank entries in items
                    foreach (var item in items)
                    {
                        var trimmedItem = item.Trim();
                        if (trimmedItem.Length > 0)
                        {
                            validatedItems.Add(trimmedItem);
                        }
                    }

                    SetStatusLogKeyNameFilterList(validatedItems);
                }
            }
            catch (Exception ex)
            {
                // Error parsing matchSpecList
                // Ignore errors here
            }
        }
    }
}
