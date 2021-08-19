// This class will read an LC-MS/MS data file and create selected ion chromatograms
//   for each of the parent ion masses chosen for fragmentation
// It will create several output files, including a BPI for the survey scan,
//   a BPI for the fragmentation scans, an XML file containing the SIC data
//   for each parent ion, and a "flat file" ready for import into the database
//   containing summaries of the SIC data statistics
// Supported file types are Thermo .Raw files (LCQ, LTQ, LTQ-FT),
//   Agilent Ion Trap (.MGF and .CDF files), and mzXML files

// -------------------------------------------------------------------------------
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
// Program started October 11, 2003
// Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
// Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
// -------------------------------------------------------------------------------
//
// Licensed under the 2-Clause BSD License; you may not use this file except
// in compliance with the License.  You may obtain a copy of the License at
// https://opensource.org/licenses/BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MASIC.DataInput;
using MASIC.DataOutput;
using MASIC.DatasetStats;
using MASIC.Options;
using PRISM;
using PRISM.Logging;

namespace MASIC
{
    /// <summary>
    /// MASIC processing class
    /// </summary>
    public class clsMASIC : PRISM.FileProcessor.ProcessFilesBase
    {
        // Ignore Spelling: Da, uncaching, OxyPlot, UnCache

        /// <summary>
        /// Constructor
        /// </summary>
        public clsMASIC()
        {
            mFileDate = "August 14, 2021";

            LocalErrorCode = MasicErrorCodes.NoError;
            StatusMessage = string.Empty;

            mProcessingStats = new clsProcessingStats();
            InitializeMemoryManagementOptions(mProcessingStats);

            mMASICPeakFinder = new MASICPeakFinder.clsMASICPeakFinder();
            RegisterEvents(mMASICPeakFinder);

            Options = new MASICOptions(FileVersion, mMASICPeakFinder.ProgramVersion);
            Options.InitializeVariables();
            RegisterEvents(Options);
        }

        /// <summary>
        /// Enabling this will result in SICs with less noise, which will hurt noise determination after finding the SICs
        /// </summary>
        public const bool DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD = false;

        /// <summary>
        /// Disabling this will slow down the correlation process (slightly)
        /// </summary>
        public const bool DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD = true;

        private const int MINIMUM_STATUS_FILE_UPDATE_INTERVAL_SECONDS = 3;

        /// <summary>
        /// Processing steps
        /// </summary>
        public enum ProcessingStepConstants
        {
            /// <summary>
            /// Starting a new analysis
            /// </summary>
            NewTask = 0,

            /// <summary>
            /// Reading the input file
            /// </summary>
            ReadDataFile = 1,

            /// <summary>
            /// Saving the base peak intensity chromatogram
            /// </summary>
            SaveBPI = 2,

            /// <summary>
            /// Creating selected ion chromatograms and finding peaks
            /// </summary>
            CreateSICsAndFindPeaks = 3,

            /// <summary>
            /// Finding similar parent ions
            /// </summary>
            FindSimilarParentIons = 4,

            /// <summary>
            /// Saving extended scan stats files
            /// </summary>
            SaveExtendedScanStatsFiles = 5,

            /// <summary>
            /// Saving the SIC stats files
            /// </summary>
            SaveSICStatsFlatFile = 6,

            /// <summary>
            /// Closing open file handles
            /// </summary>
            CloseOpenFileHandles = 7,

            /// <summary>
            /// Updating the XML file with optimal peak apex values
            /// </summary>
            UpdateXMLFileWithNewOptimalPeakApexValues = 8,

            /// <summary>
            /// Creating plots
            /// </summary>
            CreatePlots = 9,

            /// <summary>
            /// Processing canceled
            /// </summary>
            Cancelled = 99,

            /// <summary>
            /// Processing complete
            /// </summary>
            Complete = 100
        }

        /// <summary>
        /// MASIC Error Codes
        /// </summary>
        public enum MasicErrorCodes
        {
            /// <summary>
            /// No error
            /// </summary>
            NoError = 0,

            /// <summary>
            /// Invalid dataset lookup file path
            /// </summary>
            InvalidDatasetLookupFilePath = 1,

            /// <summary>
            /// Unknown file extension
            /// </summary>
            /// <remarks>
            /// This error code matches the identical code in clsFilterMsMsSpectra
            /// </remarks>
            UnknownFileExtension = 2,

            /// <summary>
            /// Input file access error
            /// </summary>
            /// <remarks>
            /// This error code matches the identical code in clsFilterMsMsSpectra
            /// </remarks>
            InputFileAccessError = 4,

            /// <summary>
            /// Invalid dataset ID
            /// </summary>
            InvalidDatasetID = 8,

            /// <summary>
            /// Error creating selected ion chromatograms
            /// </summary>
            CreateSICsError = 16,

            /// <summary>
            /// Error finding SIC peaks
            /// </summary>
            FindSICPeaksError = 32,

            /// <summary>
            /// Invalid custom SIC values
            /// </summary>
            InvalidCustomSICValues = 64,

            /// <summary>
            /// No parent ions were found in the input file
            /// </summary>
            NoParentIonsFoundInInputFile = 128,

            /// <summary>
            /// No survey scans were found in the input file
            /// </summary>
            NoSurveyScansFoundInInputFile = 256,

            /// <summary>
            /// Error finding similar parent ions
            /// </summary>
            FindSimilarParentIonsError = 512,

            /// <summary>
            /// Error reading the input file
            /// </summary>
            InputFileDataReadError = 1024,

            /// <summary>
            /// Error writing an output file
            /// </summary>
            OutputFileWriteError = 2048,

            /// <summary>
            /// File I/O permission error
            /// </summary>
            FileIOPermissionsError = 4096,

            /// <summary>
            /// Error creating the spectrum cache directory
            /// </summary>
            ErrorCreatingSpectrumCacheDirectory = 8192,

            /// <summary>
            /// Error caching a spectrum
            /// </summary>
            ErrorCachingSpectrum = 16384,

            /// <summary>
            /// Error uncaching a spectrum
            /// </summary>
            ErrorUncachingSpectrum = 32768,

            /// <summary>
            /// Error deleting cached spectrum files
            /// </summary>
            ErrorDeletingCachedSpectrumFiles = 65536,

            /// <summary>
            /// Unspecified error
            /// </summary>
            UnspecifiedError = -1
        }

        private bool mLoggedMASICVersion;

        private readonly MASICPeakFinder.clsMASICPeakFinder mMASICPeakFinder;

        private readonly clsProcessingStats mProcessingStats;

        /// <summary>
        /// Use RaiseEvent MyBase.ProgressChanged when updating the overall progress
        /// Use ProgressSubtaskChanged when updating the subtask progress
        /// </summary>
        public event ProgressSubtaskChangedEventHandler ProgressSubtaskChanged;

        /// <summary>
        /// Subtask changed event handler
        /// </summary>
        public delegate void ProgressSubtaskChangedEventHandler();

        /// <summary>
        /// Event to track that the user wants to abort processing
        /// </summary>
        public event ProgressResetKeypressAbortEventHandler ProgressResetKeypressAbort;

        /// <summary>
        /// Abort processing event handler
        /// </summary>
        public delegate void ProgressResetKeypressAbortEventHandler();

        /// <summary>
        /// Local error code
        /// </summary>
        public MasicErrorCodes LocalErrorCode { get; private set; }

        public string MASICPeakFinderDllVersion
        {
            get
            {
                if (mMASICPeakFinder != null)
                {
                    return mMASICPeakFinder.ProgramVersion;
                }

                return string.Empty;
            }
        }
        /// <summary>
        /// Returns the version of the MASIC peak finder DLL
        /// </summary>

        /// <summary>
        /// Processing options
        /// </summary>
        public MASICOptions Options { get; }

        /// <summary>
        /// Current processing step
        /// </summary>
        public ProcessingStepConstants ProcessStep { get; private set; }

        /// <summary>
        /// Status message
        /// </summary>
        public string StatusMessage { get; private set; }

        /// <summary>
        /// Subtask progress percent complete
        /// </summary>
        /// <remarks>Value between 0 and 100</remarks>
        public float SubtaskProgressPercentComplete { get; private set; }

        /// <summary>
        /// Subtask description
        /// </summary>
        public string SubtaskDescription { get; private set; } = string.Empty;

        /// <summary>
        /// Call this method to abort processing
        /// </summary>
        public override void AbortProcessingNow()
        {
            AbortProcessing = true;
            Options.AbortProcessing = true;
        }

        /// <summary>
        /// Create an example parameter file
        /// </summary>
        /// <param name="paramFilePath">File name or path; if an empty string, will use MASIC_ExampleSettings.xml</param>
        public void CreateExampleParameterFile(string paramFilePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(paramFilePath))
                {
                    paramFilePath = "MASIC_ExampleSettings.xml";
                }

                var outputFile = new FileInfo(paramFilePath);

                var action = outputFile.Exists ? "Overwriting " : "Creating ";
                Console.WriteLine(action + PathUtils.CompactPathString(outputFile.FullName, 100));

                Options.SaveParameterFileSettings(outputFile.FullName);

                Console.WriteLine("... done");
            }
            catch (Exception ex)
            {
                LogErrors("CreateExampleParameterFile", "Error creating an example parameter file", ex, MasicErrorCodes.OutputFileWriteError);
            }
        }

        private bool CreatePlots(string inputFilePath, string outputDirectoryPath)
        {
            bool success;
            var inputFile = new FileInfo(inputFilePath);

            if (inputFile.Name.EndsWith(clsDataOutput.SIC_STATS_FILE_SUFFIX, StringComparison.OrdinalIgnoreCase))
            {
                success = CreatePlots(inputFile, outputDirectoryPath);
            }
            else if (inputFilePath.EndsWith(clsDataOutput.SCAN_STATS_FILE_SUFFIX, StringComparison.OrdinalIgnoreCase))
            {
                // The user specified the Scan Stats file
                // Auto-switch to the SIC stats file
                var sicStatsFilePath = clsUtilities.ReplaceSuffix(inputFilePath, clsDataOutput.SCAN_STATS_FILE_SUFFIX, clsDataOutput.SIC_STATS_FILE_SUFFIX);
                success = CreatePlots(new FileInfo(sicStatsFilePath), outputDirectoryPath);
            }
            else if (inputFilePath.EndsWith(clsDataOutput.REPORTER_IONS_FILE_SUFFIX, StringComparison.OrdinalIgnoreCase))
            {
                // The user specified the Reporter Ion data file
                // Auto-switch to the SIC stats file

                var sicStatsFilePath = clsUtilities.ReplaceSuffix(inputFilePath, clsDataOutput.REPORTER_IONS_FILE_SUFFIX, clsDataOutput.SIC_STATS_FILE_SUFFIX);
                success = CreatePlots(new FileInfo(sicStatsFilePath), outputDirectoryPath);
            }
            else
            {
                StatusMessage = "Invalid input file path; cannot create plots using " + inputFilePath;
                ShowErrorMessage(StatusMessage);
                return false;
            }

            return success;
        }

        private bool CreatePlots(FileSystemInfo sicStatsFile, string outputDirectoryPath)
        {
            try
            {
                var statsPlotter = new StatsPlotter(Options);
                RegisterEvents(statsPlotter);

                var success = statsPlotter.ProcessFile(sicStatsFile.FullName, outputDirectoryPath);
                if (!success)
                {
                    SetLocalErrorCode(MasicErrorCodes.OutputFileWriteError);
                }

                return success;
            }
            catch (Exception ex)
            {
                LogErrors("CreatePlots", "Error summarizing stats and creating plots", ex, MasicErrorCodes.OutputFileWriteError);
                return false;
            }
        }

        private bool FindSICsAndWriteOutput(
            string inputFilePathFull,
            string outputDirectoryPath,
            clsScanList scanList,
            clsSpectraCache spectraCache,
            clsDataOutput dataOutputHandler,
            clsScanTracking scanTracking,
            DatasetFileInfo datasetFileInfo,
            clsParentIonProcessing parentIonProcessor,
            clsDataImport dataImporterBase)
        {
            var inputFileName = Path.GetFileName(inputFilePathFull);

            try
            {
                var bpiWriter = new clsBPIWriter();
                RegisterEvents(bpiWriter);

                var xmlResultsWriter = new clsXMLResultsWriter(Options);
                RegisterEvents(xmlResultsWriter);

                // ---------------------------------------------------------
                // Save the BPIs and TICs
                // ---------------------------------------------------------

                UpdateProcessingStep(ProcessingStepConstants.SaveBPI);
                UpdateOverallProgress("Processing Data for " + inputFileName);
                SetSubtaskProcessingStepPct(0, "Saving chromatograms to disk");
                UpdatePeakMemoryUsage();

                if (Options.SkipSICAndRawDataProcessing || !Options.ExportRawDataOnly)
                {
                    LogMessage("FindSICsAndWriteOutput: Call SaveBPIs", MessageTypeConstants.Debug);
                    bpiWriter.SaveBPIs(scanList, spectraCache, inputFilePathFull, outputDirectoryPath);
                }

                // ---------------------------------------------------------
                // Close the ScanStats file handle
                // ---------------------------------------------------------
                try
                {
                    LogMessage("FindSICsAndWriteOutput: Close outputFileHandles.ScanStats");

                    dataOutputHandler.OutputFileHandles.CloseScanStats();
                }
                catch (Exception)
                {
                    // Ignore errors here
                }

                // ---------------------------------------------------------
                // Create the DatasetInfo XML file
                // ---------------------------------------------------------

                LogMessage("FindSICsAndWriteOutput: Create DatasetInfo File");
                dataOutputHandler.CreateDatasetInfoFile(inputFileName, outputDirectoryPath, scanTracking, datasetFileInfo);

                int similarParentIonUpdateCount;
                if (Options.SkipSICAndRawDataProcessing)
                {
                    LogMessage("FindSICsAndWriteOutput: Skipping SIC Processing");

                    SetDefaultPeakLocValues(scanList);
                    similarParentIonUpdateCount = 0;
                }
                else
                {
                    var success = FindSICsWork(
                        inputFilePathFull,
                        outputDirectoryPath,
                        scanList,
                        spectraCache,
                        dataOutputHandler,
                        parentIonProcessor,
                        dataImporterBase,
                        bpiWriter,
                        xmlResultsWriter,
                        out similarParentIonUpdateCount);

                    if (!success)
                        return false;
                }

                if (Options.WriteExtendedStats && !Options.ExportRawDataOnly)
                {
                    // ---------------------------------------------------------
                    // Save Extended Scan Stats Files
                    // ---------------------------------------------------------

                    UpdateProcessingStep(ProcessingStepConstants.SaveExtendedScanStatsFiles);
                    SetSubtaskProcessingStepPct(0);
                    UpdatePeakMemoryUsage();

                    LogMessage("FindSICsAndWriteOutput: Call SaveExtendedScanStatsFiles", MessageTypeConstants.Debug);
                    var extendedStatsWritten = dataOutputHandler.ExtendedStatsWriter.SaveExtendedScanStatsFiles(
                        scanList, inputFileName, outputDirectoryPath, Options.IncludeHeadersInExportFile);

                    if (!extendedStatsWritten)
                    {
                        SetLocalErrorCode(MasicErrorCodes.OutputFileWriteError, true);
                        return false;
                    }
                }

                // ---------------------------------------------------------
                // Save SIC Stats Flat File
                // ---------------------------------------------------------

                UpdateProcessingStep(ProcessingStepConstants.SaveSICStatsFlatFile);
                SetSubtaskProcessingStepPct(0);
                UpdatePeakMemoryUsage();

                string sicStatsFilePath;
                if (!Options.ExportRawDataOnly)
                {
                    var sicStatsWriter = new clsSICStatsWriter();
                    RegisterEvents(sicStatsWriter);

                    LogMessage("FindSICsAndWriteOutput: Call SaveSICStatsFlatFile", MessageTypeConstants.Debug);
                    var sicStatsWritten = sicStatsWriter.SaveSICStatsFlatFile(scanList, inputFileName, outputDirectoryPath, Options, dataOutputHandler);

                    if (!sicStatsWritten)
                    {
                        SetLocalErrorCode(MasicErrorCodes.OutputFileWriteError, true);
                        return false;
                    }

                    sicStatsFilePath = sicStatsWriter.SICStatsFilePath;
                }
                else
                {
                    sicStatsFilePath = string.Empty;
                }

                UpdateProcessingStep(ProcessingStepConstants.CloseOpenFileHandles);
                SetSubtaskProcessingStepPct(0);
                UpdatePeakMemoryUsage();

                if (!(Options.SkipSICAndRawDataProcessing || Options.ExportRawDataOnly))
                {
                    // ---------------------------------------------------------
                    // Write processing stats to the XML output file
                    // ---------------------------------------------------------

                    LogMessage("FindSICsAndWriteOutput: Call FinalizeXMLFile", MessageTypeConstants.Debug);
                    var processingTimeSec = GetTotalProcessingTimeSec();
                    xmlResultsWriter.XMLOutputFileFinalize(
                        dataOutputHandler, scanList, spectraCache,
                        mProcessingStats, processingTimeSec);
                }

                // ---------------------------------------------------------
                // Close any open output files
                // ---------------------------------------------------------
                dataOutputHandler.OutputFileHandles.CloseAll();

                // ---------------------------------------------------------
                // Save a text file containing the headers used in the text files
                // ---------------------------------------------------------
                if (!Options.IncludeHeadersInExportFile)
                {
                    LogMessage("FindSICsAndWriteOutput: Call SaveHeaderGlossary", MessageTypeConstants.Debug);
                    dataOutputHandler.SaveHeaderGlossary(scanList, inputFileName, outputDirectoryPath);
                }

                if (!(Options.SkipSICAndRawDataProcessing || Options.ExportRawDataOnly) && similarParentIonUpdateCount > 0)
                {
                    // ---------------------------------------------------------
                    // Reopen the XML file and update the entries for those ions in scanList that had their
                    // Optimal peak apex scan numbers updated
                    // ---------------------------------------------------------

                    UpdateProcessingStep(ProcessingStepConstants.UpdateXMLFileWithNewOptimalPeakApexValues);
                    SetSubtaskProcessingStepPct(0);
                    UpdatePeakMemoryUsage();

                    LogMessage("FindSICsAndWriteOutput: Call XmlOutputFileUpdateEntries", MessageTypeConstants.Debug);
                    xmlResultsWriter.XmlOutputFileUpdateEntries(scanList, inputFileName, outputDirectoryPath);
                }

                if (string.IsNullOrEmpty(sicStatsFilePath) || !Options.PlotOptions.CreatePlots || scanList.ParentIons.Count == 0)
                {
                    return true;
                }

                UpdateProcessingStep(ProcessingStepConstants.CreatePlots);
                SetSubtaskProcessingStepPct(0);
                UpdateOverallProgress("Creating plots in " + outputDirectoryPath);

                var plotsCreated = CreatePlots(new FileInfo(sicStatsFilePath), outputDirectoryPath);
                return plotsCreated;
            }
            catch (Exception ex)
            {
                LogErrors("FindSICsAndWriteOutput", "Error saving results to: " + outputDirectoryPath, ex, MasicErrorCodes.OutputFileWriteError);
                return false;
            }
        }

        private bool FindSICsWork(
            string inputFilePathFull,
            string outputDirectoryPath,
            clsScanList scanList,
            clsSpectraCache spectraCache,
            clsDataOutput dataOutputHandler,
            clsParentIonProcessing parentIonProcessor,
            clsDataImport dataImporterBase,
            clsBPIWriter bpiWriter,
            clsXMLResultsWriter xmlResultsWriter,
            out int similarParentIonUpdateCount)
        {
            var inputFileName = Path.GetFileName(inputFilePathFull);
            similarParentIonUpdateCount = 0;

            // ---------------------------------------------------------
            // Optionally, export the raw mass spectra data
            // ---------------------------------------------------------

            if (Options.RawDataExportOptions.ExportEnabled)
            {
                var rawDataExporter = new clsSpectrumDataWriter(bpiWriter, Options);
                RegisterEvents(rawDataExporter);

                rawDataExporter.ExportRawDataToDisk(scanList, spectraCache, inputFileName, outputDirectoryPath);
            }

            if (Options.ReporterIons.ReporterIonStatsEnabled)
            {
                // Look for Reporter Ions in the Fragmentation spectra

                var reporterIonProcessor = new clsReporterIonProcessor(Options);
                RegisterEvents(reporterIonProcessor);
                reporterIonProcessor.FindReporterIons(scanList, spectraCache, inputFilePathFull, outputDirectoryPath);
            }

            var mrmProcessor = new clsMRMProcessing(Options, dataOutputHandler);
            RegisterEvents(mrmProcessor);

            // ---------------------------------------------------------
            // If MRM data is present, save the MRM values to disk
            // ---------------------------------------------------------
            if (scanList.MRMDataPresent)
            {
                mrmProcessor.ExportMRMDataToDisk(scanList, spectraCache, inputFileName, outputDirectoryPath);
            }

            if (!Options.ExportRawDataOnly)
            {
                // ---------------------------------------------------------
                // Add the custom SIC values to scanList
                // ---------------------------------------------------------
                Options.CustomSICList.AddCustomSICValues(scanList, Options.SICOptions.SICTolerance,
                    Options.SICOptions.SICToleranceIsPPM, Options.CustomSICList.ScanOrAcqTimeTolerance);

                // ---------------------------------------------------------
                // Possibly create the Tab-separated SIC details output file
                // ---------------------------------------------------------
                if (Options.WriteDetailedSICDataFile)
                {
                    var sicDetailsFileCreated = dataOutputHandler.InitializeSICDetailsTextFile(inputFilePathFull, outputDirectoryPath);
                    if (!sicDetailsFileCreated)
                    {
                        SetLocalErrorCode(MasicErrorCodes.OutputFileWriteError);
                        return false;
                    }
                }

                // ---------------------------------------------------------
                // Create the XML output file
                // ---------------------------------------------------------
                var xmlWriterInitialized = xmlResultsWriter.XMLOutputFileInitialize(inputFilePathFull, outputDirectoryPath, dataOutputHandler, scanList,
                    spectraCache, Options.SICOptions, Options.BinningOptions);
                if (!xmlWriterInitialized)
                {
                    SetLocalErrorCode(MasicErrorCodes.OutputFileWriteError);
                    return false;
                }

                // ---------------------------------------------------------
                // Create the selected ion chromatograms (SICs)
                // For each one, find the peaks and make an entry to the XML output file
                // ---------------------------------------------------------

                UpdateProcessingStep(ProcessingStepConstants.CreateSICsAndFindPeaks);
                SetSubtaskProcessingStepPct(0);
                UpdatePeakMemoryUsage();

                LogMessage("FindSICsWork: Call CreateParentIonSICs", MessageTypeConstants.Debug);
                var sicProcessor = new clsSICProcessing(mMASICPeakFinder, mrmProcessor);
                RegisterEvents(sicProcessor);

                var parentIonSICsCreated = sicProcessor.CreateParentIonSICs(
                    scanList, spectraCache, Options, dataOutputHandler, sicProcessor, xmlResultsWriter);

                if (!parentIonSICsCreated)
                {
                    SetLocalErrorCode(MasicErrorCodes.CreateSICsError, true);
                    return false;
                }
            }

            if (!(Options.SkipMSMSProcessing || Options.ExportRawDataOnly))
            {
                // ---------------------------------------------------------
                // Find Similar Parent Ions
                // ---------------------------------------------------------

                UpdateProcessingStep(ProcessingStepConstants.FindSimilarParentIons);
                SetSubtaskProcessingStepPct(0);
                UpdatePeakMemoryUsage();

                LogMessage("FindSICsWork: Call FindSimilarParentIons", MessageTypeConstants.Debug);
                var foundSimilarParentIons = parentIonProcessor.FindSimilarParentIons(
                    scanList, spectraCache, Options, dataImporterBase, out similarParentIonUpdateCount);

                if (!foundSimilarParentIons)
                {
                    SetLocalErrorCode(MasicErrorCodes.FindSimilarParentIonsError, true);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get supported file extensions
        /// </summary>
        public override IList<string> GetDefaultExtensionsToParse()
        {
            return clsDataImport.GetDefaultExtensionsToParse();
        }

        /// <summary>
        /// Get the error message, or an empty string if no error
        /// </summary>
        public override string GetErrorMessage()
        {
            if (ErrorCode == ProcessFilesErrorCodes.LocalizedError ||
                ErrorCode == ProcessFilesErrorCodes.NoError)
            {
                return LocalErrorCode switch
                {
                    MasicErrorCodes.NoError => string.Empty,
                    MasicErrorCodes.InvalidDatasetLookupFilePath => "Invalid dataset lookup file path",
                    MasicErrorCodes.UnknownFileExtension => "Unknown file extension",
                    MasicErrorCodes.InputFileAccessError => "Input file access error",
                    MasicErrorCodes.InvalidDatasetID => "Invalid dataset number",
                    // ReSharper disable once StringLiteralTypo
                    MasicErrorCodes.CreateSICsError => "Create SIC's error",
                    MasicErrorCodes.FindSICPeaksError => "Error finding SIC peaks",
                    MasicErrorCodes.InvalidCustomSICValues => "Invalid custom SIC values",
                    MasicErrorCodes.NoParentIonsFoundInInputFile => "No parent ions were found in the input file (additionally, no custom SIC values were defined)",
                    MasicErrorCodes.NoSurveyScansFoundInInputFile => "No survey scans were found in the input file (do you have a Scan Range filter defined?)",
                    MasicErrorCodes.FindSimilarParentIonsError => "Find similar parent ions error",
                    MasicErrorCodes.InputFileDataReadError => "Error reading data from input file",
                    MasicErrorCodes.OutputFileWriteError => "Error writing data to output file",
                    MasicErrorCodes.FileIOPermissionsError => "File IO Permissions Error",
                    MasicErrorCodes.ErrorCreatingSpectrumCacheDirectory => "Error creating spectrum cache directory",
                    MasicErrorCodes.ErrorCachingSpectrum => "Error caching spectrum",
                    MasicErrorCodes.ErrorUncachingSpectrum => "Error uncaching spectrum",
                    MasicErrorCodes.ErrorDeletingCachedSpectrumFiles => "Error deleting cached spectrum files",
                    MasicErrorCodes.UnspecifiedError => "Unspecified localized error",
                    _ => "Unknown error state"
                };
            }

            return GetBaseClassErrorMessage();
        }

        /// <summary>
        /// Get the amount of free memory, in MB
        /// </summary>
        private float GetFreeMemoryMB()
        {
            var freeMemoryMB = SystemInfo.GetFreeMemoryMB();

            return freeMemoryMB;
        }

        /// <summary>
        /// Get the amount of memory used by this process
        /// </summary>
        private float GetProcessMemoryUsageMB()
        {
            // Obtain a handle to the current process
            var currentProcess = Process.GetCurrentProcess();

            // The WorkingSet is the total physical memory usage
            return (float)(currentProcess.WorkingSet64 / 1024.0 / 1024);
        }

        /// <summary>
        /// Get total processing time, in seconds
        /// </summary>
        private float GetTotalProcessingTimeSec()
        {
            var currentProcess = Process.GetCurrentProcess();

            return (float)(currentProcess.TotalProcessorTime.TotalSeconds);
        }

        private void InitializeMemoryManagementOptions(clsProcessingStats processingStats)
        {
            processingStats.PeakMemoryUsageMB = GetProcessMemoryUsageMB();
            processingStats.TotalProcessingTimeAtStart = GetTotalProcessingTimeSec();
            processingStats.CacheEventCount = 0;
            processingStats.UnCacheEventCount = 0;

            processingStats.FileLoadStartTime = DateTime.UtcNow;
            processingStats.FileLoadEndTime = processingStats.FileLoadStartTime;

            processingStats.ProcessingStartTime = processingStats.FileLoadStartTime;
            processingStats.ProcessingEndTime = processingStats.FileLoadStartTime;

            processingStats.MemoryUsageMBAtStart = processingStats.PeakMemoryUsageMB;
            processingStats.MemoryUsageMBDuringLoad = processingStats.PeakMemoryUsageMB;
            processingStats.MemoryUsageMBAtEnd = processingStats.PeakMemoryUsageMB;
        }

        private bool LoadData(
            string inputFilePath,
            string outputDirectoryPath,
            clsDataOutput dataOutputHandler,
            clsParentIonProcessing parentIonProcessor,
            clsScanTracking scanTracking,
            clsScanList scanList,
            clsSpectraCache spectraCache,
            out clsDataImport dataImporterBase,
            out DatasetFileInfo datasetFileInfo)
        {
            bool success;
            datasetFileInfo = new DatasetFileInfo();

            try
            {
                // ---------------------------------------------------------
                // Define inputFileName (which is referenced several times below)
                // ---------------------------------------------------------
                var inputFileName = Path.GetFileName(inputFilePath);

                // ---------------------------------------------------------
                // Create the _ScanStats.txt file
                // ---------------------------------------------------------
                dataOutputHandler.OpenOutputFileHandles(inputFileName, outputDirectoryPath, Options.IncludeHeadersInExportFile);

                // ---------------------------------------------------------
                // Read the mass spectra from the input data file
                // ---------------------------------------------------------

                UpdateProcessingStep(ProcessingStepConstants.ReadDataFile);
                SetSubtaskProcessingStepPct(0);
                UpdatePeakMemoryUsage();
                StatusMessage = string.Empty;

                if (Options.SkipSICAndRawDataProcessing)
                {
                    Options.ExportRawDataOnly = false;
                }

                var keepRawMSSpectra = !Options.SkipSICAndRawDataProcessing || Options.ExportRawDataOnly;

                Options.SICOptions.ValidateSICOptions();

                switch (Path.GetExtension(inputFileName).ToUpper())
                {
                    case clsDataImport.THERMO_RAW_FILE_EXTENSION:
                        // Open the .Raw file and obtain the scan information

                        var dataImporter = new clsDataImportThermoRaw(Options, mMASICPeakFinder, parentIonProcessor, scanTracking);
                        RegisterDataImportEvents(dataImporter);
                        dataImporterBase = dataImporter;

                        success = dataImporter.ExtractScanInfoFromThermoDataFile(
                            inputFilePath,
                            scanList, spectraCache, dataOutputHandler,
                            keepRawMSSpectra,
                            !Options.SkipMSMSProcessing);

                        datasetFileInfo = dataImporter.DatasetFileInfo;
                        break;

                    case clsDataImport.MZ_ML_FILE_EXTENSION:
                        // Open the .mzML file and obtain the scan information

                        var dataImporterMzML = new clsDataImportMSXml(Options, mMASICPeakFinder, parentIonProcessor, scanTracking);
                        RegisterDataImportEvents(dataImporterMzML);
                        dataImporterBase = dataImporterMzML;

                        success = dataImporterMzML.ExtractScanInfoFromMzMLDataFile(
                            inputFilePath,
                            scanList, spectraCache, dataOutputHandler,
                            keepRawMSSpectra,
                            !Options.SkipMSMSProcessing);

                        datasetFileInfo = dataImporterMzML.DatasetFileInfo;
                        break;

                    case clsDataImport.MZ_XML_FILE_EXTENSION1:
                    case clsDataImport.MZ_XML_FILE_EXTENSION2:
                        // Open the .mzXML file and obtain the scan information

                        var dataImporterMzXML = new clsDataImportMSXml(Options, mMASICPeakFinder, parentIonProcessor, scanTracking);
                        RegisterDataImportEvents(dataImporterMzXML);
                        dataImporterBase = dataImporterMzXML;

                        success = dataImporterMzXML.ExtractScanInfoFromMzXMLDataFile(
                            inputFilePath,
                            scanList, spectraCache, dataOutputHandler,
                            keepRawMSSpectra,
                            !Options.SkipMSMSProcessing);

                        datasetFileInfo = dataImporterMzXML.DatasetFileInfo;
                        break;

                    case clsDataImport.MZ_DATA_FILE_EXTENSION1:
                    case clsDataImport.MZ_DATA_FILE_EXTENSION2:
                        // Open the .mzData file and obtain the scan information

                        var dataImporterMzData = new clsDataImportMSXml(Options, mMASICPeakFinder, parentIonProcessor, scanTracking);
                        RegisterDataImportEvents(dataImporterMzData);
                        dataImporterBase = dataImporterMzData;

                        success = dataImporterMzData.ExtractScanInfoFromMzDataFile(
                            inputFilePath,
                            scanList, spectraCache, dataOutputHandler,
                            keepRawMSSpectra, !Options.SkipMSMSProcessing);

                        datasetFileInfo = dataImporterMzData.DatasetFileInfo;
                        break;

                    case clsDataImport.AGILENT_MSMS_FILE_EXTENSION:
                    case clsDataImport.AGILENT_MS_FILE_EXTENSION:
                        // Open the .MGF and .CDF files to obtain the scan information

                        var dataImporterMGF = new clsDataImportMGFandCDF(Options, mMASICPeakFinder, parentIonProcessor, scanTracking);
                        RegisterDataImportEvents(dataImporterMGF);
                        dataImporterBase = dataImporterMGF;

                        success = dataImporterMGF.ExtractScanInfoFromMGFandCDF(
                            inputFilePath,
                            scanList, spectraCache, dataOutputHandler,
                            keepRawMSSpectra, !Options.SkipMSMSProcessing);

                        datasetFileInfo = dataImporterMGF.DatasetFileInfo;
                        break;

                    default:
                        StatusMessage = "Unknown file extension: " + Path.GetExtension(inputFilePath);
                        SetLocalErrorCode(MasicErrorCodes.UnknownFileExtension);
                        success = false;

                        // Instantiate this object to avoid a warning below about the object potentially not being initialized
                        // In reality, an Exit Try statement will be reached and the potentially problematic use will therefore not get encountered
                        datasetFileInfo = new DatasetFileInfo();
                        dataImporterBase = null;
                        break;
                }

                if (!success)
                {
                    if (LocalErrorCode == MasicErrorCodes.NoParentIonsFoundInInputFile && string.IsNullOrWhiteSpace(StatusMessage))
                    {
                        StatusMessage = "None of the spectra in the input file was within the specified scan number and/or scan time range";
                    }

                    SetLocalErrorCode(MasicErrorCodes.InputFileAccessError, true);
                }
            }
            catch (Exception ex)
            {
                success = false;
                LogErrors("LoadData", "Error accessing input data file: " + inputFilePath, ex, MasicErrorCodes.InputFileDataReadError);
                dataImporterBase = null;
            }

            scanList.SetListCapacityToCount();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            System.Threading.Thread.Sleep(50);

            return success;
        }

        /// <summary>
        /// Load settings from a parameter file
        /// </summary>
        /// <param name="parameterFilePath"></param>
        /// <returns>True if success, false if an error</returns>
        // ReSharper disable once UnusedMember.Global
        public bool LoadParameterFileSettings(string parameterFilePath)
        {
            var success = Options.LoadParameterFileSettings(parameterFilePath);
            return success;
        }

        private void LogErrors(
            string source,
            string message,
            Exception ex,
            MasicErrorCodes newErrorCode = MasicErrorCodes.NoError)
        {
            Options.StatusMessage = message;

            var messageWithoutCRLF = Options.StatusMessage.Replace(Environment.NewLine, "; ");
            if (ex != null && !string.IsNullOrEmpty(ex.Message) && !message.Contains(ex.Message))
            {
                messageWithoutCRLF += "; " + ex.Message;
            }

            // Show the message and log to the clsProcessFilesBaseClass logger
            if (string.IsNullOrEmpty(source))
            {
                ShowErrorMessage(messageWithoutCRLF);
            }
            else
            {
                ShowErrorMessage(source + ": " + messageWithoutCRLF);
            }

            if (ex != null)
            {
                Console.WriteLine(StackTraceFormatter.GetExceptionStackTraceMultiLine(ex));
            }

            if (newErrorCode != MasicErrorCodes.NoError)
            {
                SetLocalErrorCode(newErrorCode, true);
            }
        }

        /// <summary>
        /// Main processing function
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <param name="parameterFilePath"></param>
        /// <param name="resetErrorCode"></param>
        public override bool ProcessFile(
            string inputFilePath,
            string outputDirectoryPath,
            string parameterFilePath,
            bool resetErrorCode)
        {
            var success = false;

            if (!mLoggedMASICVersion)
            {
                LogMessage("Starting MASIC v" + GetAppVersion(mFileDate));
                Console.WriteLine();
                mLoggedMASICVersion = true;
            }

            if (resetErrorCode)
            {
                SetLocalErrorCode(MasicErrorCodes.NoError);
            }

            Options.OutputDirectoryPath = outputDirectoryPath;

            SubtaskProgressPercentComplete = 0;
            UpdateProcessingStep(ProcessingStepConstants.NewTask, true);
            ResetProgress("Starting calculations");

            StatusMessage = string.Empty;

            UpdateStatusFile(true);

            if (!Options.LoadParameterFileSettings(parameterFilePath, inputFilePath))
            {
                SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidParameterFile);
                StatusMessage = "Parameter file load error: " + parameterFilePath;

                ShowErrorMessage(StatusMessage);

                if (ErrorCode == ProcessFilesErrorCodes.NoError)
                {
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidParameterFile);
                }

                UpdateProcessingStep(ProcessingStepConstants.Cancelled, true);

                LogMessage("Processing ended in error");
                return false;
            }

            if (!Options.PlotOptions.PlotWithPython && SystemInfo.IsLinux)
            {
                OnWarningEvent("Plotting with OxyPlot is not supported on Linux; " +
                               "you should set PlotWithPython=True in the parameter file");
            }

            var dataOutputHandler = new clsDataOutput(Options);
            var existingResultsFound = false;

            RegisterEvents(dataOutputHandler);
            try
            {
                var keepProcessing = true;
                // If a Custom SICList file is defined, load the custom SIC values now
                if (Options.CustomSICList.CustomSICListFileName.Length > 0)
                {
                    var sicListReader = new clsCustomSICListReader(Options.CustomSICList);
                    RegisterEvents(sicListReader);

                    LogMessage("ProcessFile: Reading custom SIC values file: " + Options.CustomSICList.CustomSICListFileName);
                    success = sicListReader.LoadCustomSICListFromFile(Options.CustomSICList.CustomSICListFileName);
                    if (!success)
                    {
                        SetLocalErrorCode(MasicErrorCodes.InvalidCustomSICValues);
                        keepProcessing = false;
                    }
                }

                if (keepProcessing)
                {
                    Options.ReporterIons.UpdateMZIntensityFilterIgnoreRange();

                    LogMessage("Source data file: " + inputFilePath);

                    if (string.IsNullOrEmpty(inputFilePath))
                    {
                        ShowErrorMessage("Input file name is empty");
                        SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                        keepProcessing = false;
                    }
                }

                if (keepProcessing)
                {
                    StatusMessage = "Parsing " + Path.GetFileName(inputFilePath);

                    success = CleanupFilePaths(ref inputFilePath, ref outputDirectoryPath);
                    Options.OutputDirectoryPath = outputDirectoryPath;

                    if (success && !inputFilePath.EndsWith(clsDataImport.TEXT_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
                    {
                        var dbAccessor = new clsDatabaseAccess(Options);
                        RegisterEvents(dbAccessor);

                        Options.SICOptions.DatasetID = dbAccessor.LookupDatasetID(inputFilePath, Options.DatasetLookupFilePath, Options.SICOptions.DatasetID);

                        if (LocalErrorCode != MasicErrorCodes.NoError)
                        {
                            if (LocalErrorCode == MasicErrorCodes.InvalidDatasetID || LocalErrorCode == MasicErrorCodes.InvalidDatasetLookupFilePath)
                            {
                                // Ignore this error
                                SetLocalErrorCode(MasicErrorCodes.NoError);
                            }
                            else
                            {
                                success = false;
                            }
                        }
                    }

                    if (!success)
                    {
                        if (LocalErrorCode == MasicErrorCodes.NoError)
                            SetBaseClassErrorCode(ProcessFilesErrorCodes.FilePathError);
                        keepProcessing = false;
                    }
                }

                if (keepProcessing)
                {
                    try
                    {
                        // ---------------------------------------------------------
                        // Verify that we have write access to the output directory
                        // ---------------------------------------------------------

                        // The following use of .Demand() and .Assert() doesn't work for testing access permissions
                        // This is because System.Security.Permissions.FileIOPermission doesn't examine actual file system ACLs
                        // Instead, just create a dummy file, then delete it
                        // see also https://stackoverflow.com/a/21637409/1179467
                        //
                        // var filePermissionTest = new FileIOPermission(FileIOPermissionAccess.AllAccess, outputDirectoryPath);
                        // filePermissionTest.Demand();
                        // filePermissionTest.Assert();

                        LogMessage("Checking for write permission in the output path: " + outputDirectoryPath);

                        var outputFileTestPath = Path.Combine(outputDirectoryPath, "TestOutputFile" + DateTime.UtcNow.Ticks + ".tmp");

                        // ReSharper disable once RemoveRedundantBraces
                        using (var writer = new StreamWriter(outputFileTestPath, false))
                        {
                            writer.WriteLine("Test");
                        }

                        // Wait 250 msec, then delete the file
                        System.Threading.Thread.Sleep(250);
                        File.Delete(outputFileTestPath);
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        LogErrors("ProcessFile", "The current user does not have write permission for the output directory: " + outputDirectoryPath, ex, MasicErrorCodes.FileIOPermissionsError);
                    }

                    if (!success)
                    {
                        SetLocalErrorCode(MasicErrorCodes.FileIOPermissionsError);
                    }
                    else
                    {
                        // ---------------------------------------------------------
                        // Reset the processing stats
                        // ---------------------------------------------------------

                        InitializeMemoryManagementOptions(mProcessingStats);

                        if (inputFilePath.EndsWith(clsDataImport.TEXT_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
                        {
                            success = CreatePlots(inputFilePath, outputDirectoryPath);
                        }
                        else
                        {
                            success = ProcessInstrumentDataFile(inputFilePath, outputDirectoryPath, dataOutputHandler, out existingResultsFound);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                LogErrors("ProcessFile", "Error in ProcessFile", ex, MasicErrorCodes.UnspecifiedError);
            }
            finally
            {
                // Record the final processing stats (before the output file handles are closed)
                mProcessingStats.ProcessingEndTime = DateTime.UtcNow;
                mProcessingStats.MemoryUsageMBAtEnd = GetProcessMemoryUsageMB();

                // ---------------------------------------------------------
                // Make sure the output file handles are closed
                // ---------------------------------------------------------

                dataOutputHandler.OutputFileHandles.CloseAll();
            }

            try
            {
                // ---------------------------------------------------------
                // Cleanup after processing or error
                // ---------------------------------------------------------

                LogMessage("ProcessFile: Processing nearly complete");

                Console.WriteLine();
                if (existingResultsFound)
                {
                    StatusMessage = "Existing valid results were found; processing was not repeated.";
                    ShowMessage(StatusMessage);
                }
                else if (success)
                {
                    StatusMessage = "Processing complete.  Results can be found in directory: " + outputDirectoryPath;
                    ShowMessage(StatusMessage);
                }
                else if (LocalErrorCode == MasicErrorCodes.NoError)
                {
                    StatusMessage = "Error Code " + ErrorCode + ": " + GetErrorMessage();
                    ShowErrorMessage(StatusMessage);
                }
                else
                {
                    StatusMessage = "Error Code " + LocalErrorCode + ": " + GetErrorMessage();
                    ShowErrorMessage(StatusMessage);
                }

                Console.WriteLine();
                LogMessage(string.Format("ProcessingStats: Memory Usage At Start (MB) = {0:F2}", mProcessingStats.MemoryUsageMBAtStart));
                LogMessage(string.Format("ProcessingStats: Peak memory usage (MB) = {0:F2}", mProcessingStats.PeakMemoryUsageMB));

                LogMessage(string.Format("ProcessingStats: File Load Time (seconds) = {0:N0}", mProcessingStats.FileLoadEndTime.Subtract(mProcessingStats.FileLoadStartTime).TotalSeconds));
                LogMessage(string.Format("ProcessingStats: Memory Usage During Load (MB) = {0:F2}", mProcessingStats.MemoryUsageMBDuringLoad));

                LogMessage(string.Format("ProcessingStats: Processing Time (seconds) = {0:N0}", mProcessingStats.ProcessingEndTime.Subtract(mProcessingStats.ProcessingStartTime).TotalSeconds));
                LogMessage(string.Format("ProcessingStats: Memory Usage At End (MB) = {0:F2}", mProcessingStats.MemoryUsageMBAtEnd));

                LogMessage(string.Format("ProcessingStats: Cache Event Count = {0:N0}", mProcessingStats.CacheEventCount));
                LogMessage(string.Format("ProcessingStats: UnCache Event Count = {0:N0}", mProcessingStats.UnCacheEventCount));
                LogMessage(string.Format("ProcessingStats: SpectraPool Hit Event Count = {0:N0}", mProcessingStats.SpectraPoolHitEventCount));

                if (success)
                {
                    LogMessage("Processing complete");
                }
                else
                {
                    LogMessage("Processing ended in error");
                }
            }
            catch (Exception ex)
            {
                success = false;
                LogErrors("ProcessFile", "Error in ProcessFile (Cleanup)", ex, MasicErrorCodes.UnspecifiedError);
            }

            if (success)
            {
                Options.SICOptions.DatasetID++;
            }

            if (success)
            {
                UpdateProcessingStep(ProcessingStepConstants.Complete, true);
            }
            else
            {
                UpdateProcessingStep(ProcessingStepConstants.Cancelled, true);
            }

            return success;
        }

        private bool ProcessInstrumentDataFile(
            string inputFilePath,
            string outputDirectoryPath,
            clsDataOutput dataOutputHandler,
            out bool existingResultsFound)
        {
            try
            {
                // ---------------------------------------------------------
                // See if an output XML file already exists
                // If it does, open it and read the parameters used
                // If they match the current analysis parameters, and if the input file specs match the input file,
                // do not reprocess
                // ---------------------------------------------------------

                // Obtain the full path to the input file
                var inputFileInfo = new FileInfo(inputFilePath);

                LogMessage("Checking for existing results in the output path: " + outputDirectoryPath);

                existingResultsFound = dataOutputHandler.CheckForExistingResults(inputFileInfo.FullName, outputDirectoryPath, Options);

                if (existingResultsFound)
                {
                    LogMessage("Existing results found; data will not be reprocessed");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogErrors("ProcessInstrumentDataFile", "Error checking for existing results file", ex, MasicErrorCodes.InputFileDataReadError);
                existingResultsFound = false;
                return false;
            }

            try
            {
                // ---------------------------------------------------------
                // Instantiate the SpectraCache
                // ---------------------------------------------------------

                using var spectraCache = new clsSpectraCache(Options.CacheOptions)
                {
                    DiskCachingAlwaysDisabled = Options.CacheOptions.DiskCachingAlwaysDisabled,
                    CacheDirectoryPath = Options.CacheOptions.DirectoryPath,
                    CacheSpectraToRetainInMemory = Options.CacheOptions.SpectraToRetainInMemory
                };
                RegisterEvents(spectraCache);

                spectraCache.InitializeSpectraPool();

                var scanList = new clsScanList();
                RegisterEvents(scanList);

                var parentIonProcessor = new clsParentIonProcessing(Options.ReporterIons);
                RegisterEvents(parentIonProcessor);

                var scanTracking = new clsScanTracking(Options.ReporterIons, mMASICPeakFinder);
                RegisterEvents(scanTracking);

                // ---------------------------------------------------------
                // Load the mass spectral data
                // ---------------------------------------------------------

                var dataLoadSuccess = LoadData(inputFilePath,
                    outputDirectoryPath,
                    dataOutputHandler,
                    parentIonProcessor,
                    scanTracking,
                    scanList,
                    spectraCache,
                    out var dataImporterBase,
                    out var datasetFileInfo);

                // Record that the file is finished loading
                mProcessingStats.FileLoadEndTime = DateTime.UtcNow;
                if (!dataLoadSuccess)
                {
                    if (string.IsNullOrEmpty(StatusMessage))
                    {
                        StatusMessage = "Unable to parse file; unknown error";
                    }
                    else
                    {
                        StatusMessage = "Unable to parse file: " + StatusMessage;
                    }

                    ShowErrorMessage(StatusMessage);
                    return false;
                }

                // ---------------------------------------------------------
                // Find the Selected Ion Chromatograms, reporter ions, etc. and write the results to disk
                // ---------------------------------------------------------

                var success = FindSICsAndWriteOutput(
                    inputFilePath, outputDirectoryPath,
                    scanList, spectraCache, dataOutputHandler, scanTracking,
                    datasetFileInfo, parentIonProcessor, dataImporterBase);

                return success;
            }
            catch (Exception ex)
            {
                LogErrors("ProcessInstrumentDataFile", "Error processing", ex, MasicErrorCodes.InputFileDataReadError);
                return false;
            }
        }

        private void RegisterDataImportEvents(clsDataImport dataImporter)
        {
            RegisterEvents(dataImporter);
            dataImporter.UpdateMemoryUsageEvent += UpdateMemoryUsageEventHandler;
        }

        private void RegisterEventsBase(IEventNotifier sourceClass)
        {
            sourceClass.StatusEvent += MessageEventHandler;
            sourceClass.ErrorEvent += ErrorEventHandler;
            sourceClass.WarningEvent += WarningEventHandler;
            sourceClass.ProgressUpdate += ProgressUpdateHandler;
        }

        private void RegisterEvents(clsMasicEventNotifier sourceClass)
        {
            RegisterEventsBase(sourceClass);

            sourceClass.UpdateCacheStatsEvent += UpdatedCacheStatsEventHandler;
            sourceClass.UpdateBaseClassErrorCodeEvent += UpdateBaseClassErrorCodeEventHandler;
            sourceClass.UpdateErrorCodeEvent += UpdateErrorCodeEventHandler;
        }

        // ReSharper restore UnusedMember.Global

        private void SetDefaultPeakLocValues(clsScanList scanList)
        {
            try
            {
                foreach (var parentIon in scanList.ParentIons)
                {
                    var scanIndexObserved = parentIon.SurveyScanIndex;

                    var sicStats = parentIon.SICStats;
                    sicStats.ScanTypeForPeakIndices = clsScanList.ScanTypeConstants.SurveyScan;
                    sicStats.PeakScanIndexStart = scanIndexObserved;
                    sicStats.PeakScanIndexEnd = scanIndexObserved;
                    sicStats.PeakScanIndexMax = scanIndexObserved;
                }
            }
            catch (Exception ex)
            {
                LogErrors("SetDefaultPeakLocValues", "Error in clsMasic->SetDefaultPeakLocValues ", ex);
            }
        }

        private void SetLocalErrorCode(MasicErrorCodes newErrorCode, bool leaveExistingErrorCodeUnchanged = false)
        {
            if (leaveExistingErrorCodeUnchanged && LocalErrorCode != MasicErrorCodes.NoError)
            {
                // An error code is already defined; do not change it
            }
            else
            {
                LocalErrorCode = newErrorCode;

                if (newErrorCode == MasicErrorCodes.NoError)
                {
                    if (ErrorCode == ProcessFilesErrorCodes.LocalizedError)
                    {
                        SetBaseClassErrorCode(ProcessFilesErrorCodes.NoError);
                    }
                }
                else
                {
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.LocalizedError);
                }
            }
        }

        private DateTime SetSubtaskProcessingStepPctLastFileWriteTime = DateTime.UtcNow;

        /// <summary>
        /// Update subtask progress
        /// </summary>
        /// <param name="subtaskPercentComplete">Percent complete, between 0 and 100</param>
        /// <param name="forceUpdate"></param>
        private void SetSubtaskProcessingStepPct(float subtaskPercentComplete, bool forceUpdate = false)
        {
            const int MINIMUM_PROGRESS_UPDATE_INTERVAL_MILLISECONDS = 250;

            var raiseEventNow = false;

            if (Math.Abs(subtaskPercentComplete) < float.Epsilon)
            {
                AbortProcessing = false;
                ProgressResetKeypressAbort?.Invoke();
                raiseEventNow = true;
            }

            if (Math.Abs(subtaskPercentComplete - SubtaskProgressPercentComplete) > float.Epsilon)
            {
                raiseEventNow = true;
                SubtaskProgressPercentComplete = subtaskPercentComplete;
            }

            if (forceUpdate || raiseEventNow ||
                DateTime.UtcNow.Subtract(SetSubtaskProcessingStepPctLastFileWriteTime).TotalMilliseconds >= MINIMUM_PROGRESS_UPDATE_INTERVAL_MILLISECONDS)
            {
                SetSubtaskProcessingStepPctLastFileWriteTime = DateTime.UtcNow;

                UpdateOverallProgress();
                UpdateStatusFile();
                ProgressSubtaskChanged?.Invoke();
            }
        }

        /// <summary>
        /// Update subtask progress and description
        /// </summary>
        /// <param name="subtaskPercentComplete">Percent complete, between 0 and 100</param>
        /// <param name="message"></param>
        private void SetSubtaskProcessingStepPct(float subtaskPercentComplete, string message)
        {
            SubtaskDescription = message;
            SetSubtaskProcessingStepPct(subtaskPercentComplete, true);
        }

        private void UpdateOverallProgress()
        {
            UpdateOverallProgress(ProgressStepDescription);
        }

        private void UpdateOverallProgress(string message)
        {
            // Update the processing progress, storing the value in mProgressPercentComplete

            // NewTask = 0
            // ReadDataFile = 1
            // SaveBPI = 2
            // CreateSICsAndFindPeaks = 3
            // FindSimilarParentIons = 4
            // SaveExtendedScanStatsFiles = 5
            // SaveSICStatsFlatFile = 6
            // CloseOpenFileHandles = 7
            // UpdateXMLFileWithNewOptimalPeakApexValues = 8
            // CreatePlots = 9
            // Canceled = 99
            // Complete = 100

            float[] weightingFactors;

            if (Options.SkipMSMSProcessing)
            {
                // Step                    0  1      2       3  4  5       6       7       8       9
                weightingFactors = new[] { 0, 0.96F, 0.002F, 0, 0, 0.001F, 0.025F, 0.001F, 0.001F, 0.01F };           // The sum of these factors should be 1.00
            }
            else
            {
                // Step                    0  1       2       3      4      5       6       7       8       9
                weightingFactors = new[] { 0, 0.194F, 0.003F, 0.64F, 0.09F, 0.001F, 0.006F, 0.001F, 0.055F, 0.01F };   // The sum of these factors should be 1.00
            }

            try
            {
                var currentStep = (int)ProcessStep;
                if (currentStep >= weightingFactors.Length)
                    currentStep = weightingFactors.Length - 1;

                float overallPctCompleted = 0;
                for (var index = 0; index < currentStep; index++)
                {
                    overallPctCompleted += weightingFactors[index] * 100;
                }

                overallPctCompleted += weightingFactors[currentStep] * SubtaskProgressPercentComplete;

                mProgressPercentComplete = overallPctCompleted;
            }
            catch (Exception ex)
            {
                LogErrors("UpdateOverallProgress", "Bug in UpdateOverallProgress", ex);
            }

            UpdateProgress(message, mProgressPercentComplete);
        }

        private void UpdatePeakMemoryUsage()
        {
            var memoryUsageMB = GetProcessMemoryUsageMB();
            if (memoryUsageMB > mProcessingStats.PeakMemoryUsageMB)
            {
                mProcessingStats.PeakMemoryUsageMB = memoryUsageMB;
            }
        }

        private void UpdateProcessingStep(ProcessingStepConstants eNewProcessingStep, bool forceStatusFileUpdate = false)
        {
            ProcessStep = eNewProcessingStep;
            UpdateStatusFile(forceStatusFileUpdate);
        }

        private DateTime UpdateStatusFileLastFileWriteTime = DateTime.UtcNow;

        private void UpdateStatusFile(bool forceUpdate = false)
        {
            if (forceUpdate || DateTime.UtcNow.Subtract(UpdateStatusFileLastFileWriteTime).TotalSeconds >= MINIMUM_STATUS_FILE_UPDATE_INTERVAL_SECONDS)
            {
                UpdateStatusFileLastFileWriteTime = DateTime.UtcNow;

                try
                {
                    var tempPath = Path.Combine(GetAppDirectoryPath(), "Temp_" + Options.MASICStatusFilename);
                    var statusFilePath = Path.Combine(GetAppDirectoryPath(), Options.MASICStatusFilename);

                    using (var writer = new System.Xml.XmlTextWriter(tempPath, System.Text.Encoding.UTF8))
                    {
                        writer.Formatting = System.Xml.Formatting.Indented;
                        writer.Indentation = 2;

                        writer.WriteStartDocument(true);
                        writer.WriteComment("MASIC processing status");

                        // Write the beginning of the "Root" element.
                        writer.WriteStartElement("Root");

                        writer.WriteStartElement("General");
                        writer.WriteElementString("LastUpdate", DateTime.Now.ToString(clsDatasetStatsSummarizer.DATE_TIME_FORMAT_STRING));
                        writer.WriteElementString("ProcessingStep", ProcessStep.ToString());
                        writer.WriteElementString("Progress", StringUtilities.DblToString(mProgressPercentComplete, 2));
                        writer.WriteElementString("Error", GetErrorMessage());
                        writer.WriteEndElement();

                        writer.WriteStartElement("Statistics");
                        writer.WriteElementString("FreeMemoryMB", StringUtilities.DblToString(GetFreeMemoryMB(), 1));
                        writer.WriteElementString("MemoryUsageMB", StringUtilities.DblToString(GetProcessMemoryUsageMB(), 1));
                        writer.WriteElementString("PeakMemoryUsageMB", StringUtilities.DblToString(mProcessingStats.PeakMemoryUsageMB, 1));

                        writer.WriteElementString("CacheEventCount", mProcessingStats.CacheEventCount.ToString());
                        writer.WriteElementString("UnCacheEventCount", mProcessingStats.UnCacheEventCount.ToString());
                        writer.WriteElementString("SpectraPoolHitEventCount", mProcessingStats.SpectraPoolHitEventCount.ToString());

                        writer.WriteElementString("ProcessingTimeSec", StringUtilities.DblToString(GetTotalProcessingTimeSec(), 1));
                        writer.WriteEndElement();

                        writer.WriteEndElement();  // End the "Root" element.
                        writer.WriteEndDocument(); // End the document
                    }

                    // Copy the temporary file to the real one
                    File.Copy(tempPath, statusFilePath, true);
                    File.Delete(tempPath);
                }
                catch (Exception)
                {
                    // Ignore any errors
                }
            }
        }

        private void MessageEventHandler(string message)
        {
            LogMessage(message);
        }

        private void ErrorEventHandler(string message, Exception ex)
        {
            LogErrors(string.Empty, message, ex);
        }

        private void WarningEventHandler(string message)
        {
            LogMessage(message, MessageTypeConstants.Warning);
        }

        /// <summary>
        /// Update progress
        /// </summary>
        /// <param name="progressMessage">Progress message (can be empty)</param>
        /// <param name="percentComplete">Value between 0 and 100</param>
        private void ProgressUpdateHandler(string progressMessage, float percentComplete)
        {
            if (string.IsNullOrEmpty(progressMessage))
            {
                SetSubtaskProcessingStepPct(percentComplete);
            }
            else
            {
                SetSubtaskProcessingStepPct(percentComplete, progressMessage);
            }
        }

        private void UpdatedCacheStatsEventHandler(int cacheEventCount, int unCacheEventCount, int spectraPoolHitEventCount)
        {
            mProcessingStats.CacheEventCount = cacheEventCount;
            mProcessingStats.UnCacheEventCount = unCacheEventCount;
            mProcessingStats.SpectraPoolHitEventCount = spectraPoolHitEventCount;
        }

        private void UpdateBaseClassErrorCodeEventHandler(ProcessFilesErrorCodes eErrorCode)
        {
            SetBaseClassErrorCode(eErrorCode);
        }

        private void UpdateErrorCodeEventHandler(MasicErrorCodes eErrorCode, bool leaveExistingErrorCodeUnchanged)
        {
            SetLocalErrorCode(eErrorCode, leaveExistingErrorCodeUnchanged);
        }

        /// <summary>
        /// Update mProcessingStats.MemoryUsageMBDuringLoad based on the current memory usage
        /// </summary>
        private void UpdateMemoryUsageEventHandler()
        {
            var memoryUsageMB = GetProcessMemoryUsageMB();
            if (memoryUsageMB > mProcessingStats.MemoryUsageMBDuringLoad)
            {
                mProcessingStats.MemoryUsageMBDuringLoad = memoryUsageMB;
            }
        }
    }
}
