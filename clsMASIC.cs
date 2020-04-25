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
using MASIC.DataOutput;
using MASIC.DatasetStats;
using PRISM;
using PRISM.Logging;

namespace MASIC
{
    public class clsMASIC : PRISM.FileProcessor.ProcessFilesBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public clsMASIC()
        {
            mFileDate = "April 24, 2020";

            mLocalErrorCode = eMasicErrorCodes.NoError;
            mStatusMessage = string.Empty;

            mProcessingStats = new clsProcessingStats();
            InitializeMemoryManagementOptions(mProcessingStats);

            mMASICPeakFinder = new MASICPeakFinder.clsMASICPeakFinder();
            RegisterEvents(mMASICPeakFinder);

            Options = new clsMASICOptions(FileVersion, mMASICPeakFinder.ProgramVersion);
            Options.InitializeVariables();
            RegisterEvents(Options);
        }

        #region "Constants and Enums"

        // Enabling this will result in SICs with less noise, which will hurt noise determination after finding the SICs
        public const bool DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD = false;

        // Disabling this will slow down the correlation process (slightly)
        public const bool DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD = true;

        private const int MINIMUM_STATUS_FILE_UPDATE_INTERVAL_SECONDS = 3;

        public enum eProcessingStepConstants
        {
            NewTask = 0,
            ReadDataFile = 1,
            SaveBPI = 2,
            CreateSICsAndFindPeaks = 3,
            FindSimilarParentIons = 4,
            SaveExtendedScanStatsFiles = 5,
            SaveSICStatsFlatFile = 6,
            CloseOpenFileHandles = 7,
            UpdateXMLFileWithNewOptimalPeakApexValues = 8,
            Cancelled = 99,
            Complete = 100
        }

        public enum eMasicErrorCodes
        {
            NoError = 0,
            InvalidDatasetLookupFilePath = 1,
            UnknownFileExtension = 2,            // This error code matches the identical code in clsFilterMsMsSpectra
            InputFileAccessError = 4,            // This error code matches the identical code in clsFilterMsMsSpectra
            InvalidDatasetID = 8,
            CreateSICsError = 16,
            FindSICPeaksError = 32,
            InvalidCustomSICValues = 64,
            NoParentIonsFoundInInputFile = 128,
            NoSurveyScansFoundInInputFile = 256,
            FindSimilarParentIonsError = 512,
            InputFileDataReadError = 1024,
            OutputFileWriteError = 2048,
            FileIOPermissionsError = 4096,
            ErrorCreatingSpectrumCacheDirectory = 8192,
            ErrorCachingSpectrum = 16384,
            ErrorUncachingSpectrum = 32768,
            ErrorDeletingCachedSpectrumFiles = 65536,
            UnspecifiedError = -1
        }

        #endregion

        #region "Classwide Variables"

        private bool mLoggedMASICVersion;

        private readonly MASICPeakFinder.clsMASICPeakFinder mMASICPeakFinder;

        private readonly clsProcessingStats mProcessingStats;

        /// <summary>
        /// Current processing step
        /// </summary>
        private eProcessingStepConstants mProcessingStep;

        /// <summary>
        /// Percent completion for the current sub task
        /// </summary>
        /// <remarks>Value between 0 and 100</remarks>
        private float mSubtaskProcessingStepPct;

        private string mSubtaskDescription = string.Empty;

        private eMasicErrorCodes mLocalErrorCode;
        private string mStatusMessage;

        #endregion

        #region "Events"
        /// <summary>
        /// Use RaiseEvent MyBase.ProgressChanged when updating the overall progress
        /// Use ProgressSubtaskChanged when updating the sub task progress
        /// </summary>
        public event ProgressSubtaskChangedEventHandler ProgressSubtaskChanged;

        public delegate void ProgressSubtaskChangedEventHandler();

        public event ProgressResetKeypressAbortEventHandler ProgressResetKeypressAbort;

        public delegate void ProgressResetKeypressAbortEventHandler();

        #endregion

        // ReSharper disable UnusedMember.Global

        #region "Processing Options and File Path Interface Functions"

        [Obsolete("Use Property Options")]
        public string DatabaseConnectionString
        {
            get => Options.DatabaseConnectionString;
            set => Options.DatabaseConnectionString = value;
        }

        [Obsolete("Use Property Options")]
        public string DatasetInfoQuerySql
        {
            get => Options.DatasetInfoQuerySql;
            set => Options.DatasetInfoQuerySql = value;
        }

        [Obsolete("Use Property Options")]
        public string DatasetLookupFilePath
        {
            get => Options.DatasetLookupFilePath;
            set => Options.DatasetLookupFilePath = value;
        }

        [Obsolete("Use Property Options")]
        public int DatasetNumber
        {
            get => Options.SICOptions.DatasetID;
            set => Options.SICOptions.DatasetID = value;
        }

        public eMasicErrorCodes LocalErrorCode => mLocalErrorCode;

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

        [Obsolete("Use Property Options")]
        public string MASICStatusFilename
        {
            get => Options.MASICStatusFilename;
            set
            {
                if (value == null || value.Trim().Length == 0)
                {
                    Options.MASICStatusFilename = clsMASICOptions.DEFAULT_MASIC_STATUS_FILE_NAME;
                }
                else
                {
                    Options.MASICStatusFilename = value;
                }
            }
        }

        public clsMASICOptions Options { get; }

        public eProcessingStepConstants ProcessStep => mProcessingStep;

        /// <summary>
        /// Subtask progress percent complete
        /// </summary>
        /// <returns></returns>
        /// <remarks>Value between 0 and 100</remarks>
        public float SubtaskProgressPercentComplete => mSubtaskProcessingStepPct;

        public string SubtaskDescription => mSubtaskDescription;

        public string StatusMessage => mStatusMessage;

        #endregion

        #region "SIC Options Interface Functions"
        [Obsolete("Use Property Options")]
        public bool CDFTimeInSeconds
        {
            get => Options.CDFTimeInSeconds;
            set => Options.CDFTimeInSeconds = value;
        }

        [Obsolete("Use Property Options")]
        public bool CompressMSSpectraData
        {
            get => Options.SICOptions.CompressMSSpectraData;
            set => Options.SICOptions.CompressMSSpectraData = value;
        }

        [Obsolete("Use Property Options")]
        public bool CompressMSMSSpectraData
        {
            get => Options.SICOptions.CompressMSMSSpectraData;
            set => Options.SICOptions.CompressMSMSSpectraData = value;
        }

        [Obsolete("Use Property Options")]
        public double CompressToleranceDivisorForDa
        {
            get => Options.SICOptions.CompressToleranceDivisorForDa;
            set => Options.SICOptions.CompressToleranceDivisorForDa = value;
        }

        [Obsolete("Use Property Options")]
        public double CompressToleranceDivisorForPPM
        {
            get => Options.SICOptions.CompressToleranceDivisorForPPM;
            set => Options.SICOptions.CompressToleranceDivisorForPPM = value;
        }

        [Obsolete("Use Property Options")]
        public bool ConsolidateConstantExtendedHeaderValues
        {
            get => Options.ConsolidateConstantExtendedHeaderValues;
            set => Options.ConsolidateConstantExtendedHeaderValues = value;
        }

        [Obsolete("Use Property Options")]
        public clsCustomSICList.eCustomSICScanTypeConstants CustomSICListScanType => Options.CustomSICList.ScanToleranceType;

        [Obsolete("Use Property Options")]
        public float CustomSICListScanTolerance => Options.CustomSICList.ScanOrAcqTimeTolerance;

        [Obsolete("Use Property Options")]
        public List<clsCustomMZSearchSpec> CustomSICListSearchValues => Options.CustomSICList.CustomMZSearchValues;

        [Obsolete("Use Property Options")]
        public string CustomSICListFileName
        {
            get => Options.CustomSICList.CustomSICListFileName;
            set => Options.CustomSICList.CustomSICListFileName = value;
        }

        [Obsolete("Use Property Options")]
        public bool ExportRawDataOnly
        {
            get => Options.ExportRawDataOnly;
            set => Options.ExportRawDataOnly = value;
        }

        [Obsolete("Use Property Options")]
        public bool FastExistingXMLFileTest
        {
            get => Options.FastExistingXMLFileTest;
            set => Options.FastExistingXMLFileTest = value;
        }

        [Obsolete("Use Property Options")]
        public bool IncludeHeadersInExportFile
        {
            get => Options.IncludeHeadersInExportFile;
            set => Options.IncludeHeadersInExportFile = value;
        }

        [Obsolete("Use Property Options")]
        public bool IncludeScanTimesInSICStatsFile
        {
            get => Options.IncludeScanTimesInSICStatsFile;
            set => Options.IncludeScanTimesInSICStatsFile = value;
        }

        [Obsolete("Use Property Options")]
        public bool LimitSearchToCustomMZList
        {
            get => Options.CustomSICList.LimitSearchToCustomMZList;
            set => Options.CustomSICList.LimitSearchToCustomMZList = value;
        }

        [Obsolete("Use Property Options")]
        public double ParentIonDecoyMassDa
        {
            get => Options.ParentIonDecoyMassDa;
            set => Options.ParentIonDecoyMassDa = value;
        }

        [Obsolete("Use Property Options")]
        public bool SkipMSMSProcessing
        {
            get => Options.SkipMSMSProcessing;
            set => Options.SkipMSMSProcessing = value;
        }

        [Obsolete("Use Property Options")]
        public bool SkipSICAndRawDataProcessing
        {
            get => Options.SkipSICAndRawDataProcessing;
            set => Options.SkipSICAndRawDataProcessing = value;
        }

        [Obsolete("Use Property Options")]
        public bool SuppressNoParentIonsError
        {
            get => Options.SuppressNoParentIonsError;
            set => Options.SuppressNoParentIonsError = value;
        }

        [Obsolete("No longer supported")]
        // ReSharper disable once IdentifierTypo
        public bool UseFinniganXRawAccessorFunctions
        {
            get => true;
            set {}
        }

        [Obsolete("Use Property Options")]
        public bool WriteDetailedSICDataFile
        {
            get => Options.WriteDetailedSICDataFile;
            set => Options.WriteDetailedSICDataFile = value;
        }

        [Obsolete("Use Property Options")]
        public bool WriteExtendedStats
        {
            get => Options.WriteExtendedStats;
            set => Options.WriteExtendedStats = value;
        }

        [Obsolete("Use Property Options")]
        public bool WriteExtendedStatsIncludeScanFilterText
        {
            get => Options.WriteExtendedStatsIncludeScanFilterText;
            set => Options.WriteExtendedStatsIncludeScanFilterText = value;
        }

        [Obsolete("Use Property Options")]
        public bool WriteExtendedStatsStatusLog
        {
            get => Options.WriteExtendedStatsStatusLog;
            set => Options.WriteExtendedStatsStatusLog = value;
        }

        [Obsolete("Use Property Options")]
        public bool WriteMSMethodFile
        {
            get => Options.WriteMSMethodFile;
            set => Options.WriteMSMethodFile = value;
        }

        [Obsolete("Use Property Options")]
        public bool WriteMSTuneFile
        {
            get => Options.WriteMSTuneFile;
            set => Options.WriteMSTuneFile = value;
        }

        /// <summary>
        /// This property is included for historical reasons since SIC tolerance can now be Da or PPM
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use Property Options.  Also, the SICToleranceDa setting should not be used; use SetSICTolerance and GetSICTolerance instead")]
        public double SICToleranceDa
        {
            get => Options.SICOptions.SICToleranceDa;
            set => Options.SICOptions.SICToleranceDa = value;
        }

        [Obsolete("Use Property Options.SICOptions.GetSICTolerance")]
        public double GetSICTolerance()
        {
            return Options.SICOptions.GetSICTolerance(out _);
        }

        [Obsolete("Use Property Options.SICOptions.GetSICTolerance")]
        public double GetSICTolerance(out bool toleranceIsPPM)
        {
            return Options.SICOptions.GetSICTolerance(out toleranceIsPPM);
        }

        [Obsolete("Use Property Options.SICOptions.SetSICTolerance")]
        public void SetSICTolerance(double sicTolerance, bool toleranceIsPPM)
        {
            Options.SICOptions.SetSICTolerance(sicTolerance, toleranceIsPPM);
        }

        [Obsolete("Use Property Options")]
        public bool SICToleranceIsPPM
        {
            get => Options.SICOptions.SICToleranceIsPPM;
            set => Options.SICOptions.SICToleranceIsPPM = value;
        }

        [Obsolete("Use Property Options")]
        public bool RefineReportedParentIonMZ
        {
            get => Options.SICOptions.RefineReportedParentIonMZ;
            set => Options.SICOptions.RefineReportedParentIonMZ = value;
        }

        [Obsolete("Use Property Options")]
        public float RTRangeEnd
        {
            get => Options.SICOptions.RTRangeEnd;
            set => Options.SICOptions.RTRangeEnd = value;
        }

        [Obsolete("Use Property Options")]
        public float RTRangeStart
        {
            get => Options.SICOptions.RTRangeStart;
            set => Options.SICOptions.RTRangeStart = value;
        }

        [Obsolete("Use Property Options")]
        public int ScanRangeEnd
        {
            get => Options.SICOptions.ScanRangeEnd;
            set => Options.SICOptions.ScanRangeEnd = value;
        }

        [Obsolete("Use Property Options")]
        public int ScanRangeStart
        {
            get => Options.SICOptions.ScanRangeStart;
            set => Options.SICOptions.ScanRangeStart = value;
        }

        [Obsolete("Use Property Options")]
        public float MaxSICPeakWidthMinutesBackward
        {
            get => Options.SICOptions.MaxSICPeakWidthMinutesBackward;
            set => Options.SICOptions.MaxSICPeakWidthMinutesBackward = value;
        }

        [Obsolete("Use Property Options")]
        public float MaxSICPeakWidthMinutesForward
        {
            get => Options.SICOptions.MaxSICPeakWidthMinutesForward;
            set => Options.SICOptions.MaxSICPeakWidthMinutesForward = value;
        }

        [Obsolete("Use Property Options")]
        public double SICNoiseFractionLowIntensityDataToAverage
        {
            get => Options.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage;
            set => Options.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage = value;
        }

        [Obsolete("Use Property Options")]
        public double SICNoiseMinimumSignalToNoiseRatio
        {
            get => Options.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio;
            // This value isn't utilized by MASIC for SICs so we'll force it to always be zero
            set => Options.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = 0;
        }

        [Obsolete("Use Property Options")]
        public double SICNoiseThresholdIntensity
        {
            get => Options.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute;
            set => Options.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute = value;
        }

        [Obsolete("Use Property Options")]
        public MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes SICNoiseThresholdMode
        {
            get => Options.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode;
            set => Options.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = value;
        }

        [Obsolete("Use Property Options")]
        public double MassSpectraNoiseFractionLowIntensityDataToAverage
        {
            get => Options.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage;
            set => Options.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = value;
        }

        [Obsolete("Use Property Options")]
        public double MassSpectraNoiseMinimumSignalToNoiseRatio
        {
            get => Options.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio;
            set => Options.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = value;
        }

        [Obsolete("Use Property Options")]
        public double MassSpectraNoiseThresholdIntensity
        {
            get => Options.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute;
            set
            {
                if (value < 0)
                    Options.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute = 0;
                else
                    Options.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute = value;
            }
        }

        [Obsolete("Use Property Options")]
        public MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes MassSpectraNoiseThresholdMode
        {
            get => Options.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode;
            set => Options.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = value;
        }

        [Obsolete("Use Property Options")]
        public bool ReplaceSICZeroesWithMinimumPositiveValueFromMSData
        {
            get => Options.SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData;
            set => Options.SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData = value;
        }
        #endregion

        #region "Raw Data Export Options"

        [Obsolete("Use Property Options")]
        public bool ExportRawDataIncludeMSMS
        {
            get => Options.RawDataExportOptions.IncludeMSMS;
            set => Options.RawDataExportOptions.IncludeMSMS = value;
        }

        [Obsolete("Use Property Options")]
        public bool ExportRawDataRenumberScans
        {
            get => Options.RawDataExportOptions.RenumberScans;
            set => Options.RawDataExportOptions.RenumberScans = value;
        }

        [Obsolete("Use Property Options")]
        public float ExportRawDataIntensityMinimum
        {
            get => Options.RawDataExportOptions.IntensityMinimum;
            set => Options.RawDataExportOptions.IntensityMinimum = value;
        }

        [Obsolete("Use Property Options")]
        public int ExportRawDataMaxIonCountPerScan
        {
            get => Options.RawDataExportOptions.MaxIonCountPerScan;
            set => Options.RawDataExportOptions.MaxIonCountPerScan = value;
        }

        [Obsolete("Use Property Options")]
        public clsRawDataExportOptions.eExportRawDataFileFormatConstants ExportRawDataFileFormat
        {
            get => Options.RawDataExportOptions.FileFormat;
            set => Options.RawDataExportOptions.FileFormat = value;
        }

        [Obsolete("Use Property Options")]
        public float ExportRawDataMinimumSignalToNoiseRatio
        {
            get => Options.RawDataExportOptions.MinimumSignalToNoiseRatio;
            set => Options.RawDataExportOptions.MinimumSignalToNoiseRatio = value;
        }

        [Obsolete("Use Property Options")]
        public bool ExportRawSpectraData
        {
            get => Options.RawDataExportOptions.ExportEnabled;
            set => Options.RawDataExportOptions.ExportEnabled = value;
        }

        #endregion

        #region "Peak Finding Options"
        [Obsolete("Use Property Options")]
        public double IntensityThresholdAbsoluteMinimum
        {
            get => Options.SICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum;
            set => Options.SICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum = value;
        }

        [Obsolete("Use Property Options")]
        public double IntensityThresholdFractionMax
        {
            get => Options.SICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax;
            set => Options.SICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax = value;
        }

        [Obsolete("Use Property Options")]
        public int MaxDistanceScansNoOverlap
        {
            get => Options.SICOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap;
            set => Options.SICOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap = value;
        }

        [Obsolete("Use Property Options")]
        public bool FindPeaksOnSmoothedData
        {
            get => Options.SICOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData;
            set => Options.SICOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData = value;
        }

        [Obsolete("Use Property Options")]
        public bool SmoothDataRegardlessOfMinimumPeakWidth
        {
            get => Options.SICOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth;
            set => Options.SICOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth = value;
        }

        [Obsolete("Use Property Options")]
        public bool UseButterworthSmooth
        {
            get => Options.SICOptions.SICPeakFinderOptions.UseButterworthSmooth;
            set => Options.SICOptions.SICPeakFinderOptions.UseButterworthSmooth = value;
        }

        [Obsolete("Use Property Options")]
        public double ButterworthSamplingFrequency
        {
            get => Options.SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequency;
            // Value should be between 0.01 and 0.99; this is checked for in the filter, so we don't need to check here
            set => Options.SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequency = value;
        }

        [Obsolete("Use Property Options")]
        public bool ButterworthSamplingFrequencyDoubledForSIMData
        {
            get => Options.SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData;
            set => Options.SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData = value;
        }

        [Obsolete("Use Property Options")]
        public bool UseSavitzkyGolaySmooth
        {
            get => Options.SICOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth;
            set => Options.SICOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth = value;
        }

        [Obsolete("Use Property Options")]
        public short SavitzkyGolayFilterOrder
        {
            get => Options.SICOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder;
            set => Options.SICOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder = value;
        }

        [Obsolete("Use Property Options")]
        public bool SaveSmoothedData
        {
            get => Options.SICOptions.SaveSmoothedData;
            set => Options.SICOptions.SaveSmoothedData = value;
        }

        [Obsolete("Use Property Options")]
        public double MaxAllowedUpwardSpikeFractionMax
        {
            get => Options.SICOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax;
            set => Options.SICOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax = value;
        }

        [Obsolete("Use Property Options")]
        public double InitialPeakWidthScansScaler
        {
            get => Options.SICOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler;
            set => Options.SICOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler = value;
        }

        [Obsolete("Use Property Options")]
        public int InitialPeakWidthScansMaximum
        {
            get => Options.SICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum;
            set => Options.SICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum = value;
        }
        #endregion

        #region "Spectrum Similarity Options"
        [Obsolete("Use Property Options")]
        public float SimilarIonMZToleranceHalfWidth
        {
            get => Options.SICOptions.SimilarIonMZToleranceHalfWidth;
            set => Options.SICOptions.SimilarIonMZToleranceHalfWidth = value;
        }

        [Obsolete("Use Property Options")]
        public float SimilarIonToleranceHalfWidthMinutes
        {
            get => Options.SICOptions.SimilarIonToleranceHalfWidthMinutes;
            set => Options.SICOptions.SimilarIonToleranceHalfWidthMinutes = value;
        }

        [Obsolete("Use Property Options")]
        public float SpectrumSimilarityMinimum
        {
            get => Options.SICOptions.SpectrumSimilarityMinimum;
            set => Options.SICOptions.SpectrumSimilarityMinimum = value;
        }
        #endregion

        #region "Binning Options Interface Functions"

        [Obsolete("Use Property Options")]
        public float BinStartX
        {
            get => Options.BinningOptions.StartX;
            set => Options.BinningOptions.StartX = value;
        }

        [Obsolete("Use Property Options")]
        public float BinEndX
        {
            get => Options.BinningOptions.EndX;
            set => Options.BinningOptions.EndX = value;
        }

        [Obsolete("Use Property Options")]
        public float BinSize
        {
            get => Options.BinningOptions.BinSize;
            set => Options.BinningOptions.BinSize = value;
        }

        [Obsolete("Use Property Options")]
        public float BinnedDataIntensityPrecisionPercent
        {
            get => Options.BinningOptions.IntensityPrecisionPercent;
            set => Options.BinningOptions.IntensityPrecisionPercent = value;
        }

        [Obsolete("Use Property Options")]
        public bool NormalizeBinnedData
        {
            get => Options.BinningOptions.Normalize;
            set => Options.BinningOptions.Normalize = value;
        }

        [Obsolete("Use Property Options")]
        public bool SumAllIntensitiesForBin
        {
            get => Options.BinningOptions.SumAllIntensitiesForBin;
            set => Options.BinningOptions.SumAllIntensitiesForBin = value;
        }

        [Obsolete("Use Property Options")]
        public int MaximumBinCount
        {
            get => Options.BinningOptions.MaximumBinCount;
            set => Options.BinningOptions.MaximumBinCount = value;
        }
        #endregion

        #region "Memory Options Interface Functions"

        [Obsolete("Use Property Options")]
        public bool DiskCachingAlwaysDisabled
        {
            get => Options.CacheOptions.DiskCachingAlwaysDisabled;
            set => Options.CacheOptions.DiskCachingAlwaysDisabled = value;
        }

        [Obsolete("Use Property Options")]
        public string CacheDirectoryPath
        {
            get => Options.CacheOptions.DirectoryPath;
            set => Options.CacheOptions.DirectoryPath = value;
        }

        [Obsolete("Legacy parameter; no longer used")]
        public float CacheMaximumMemoryUsageMB
        {
            get => Options.CacheOptions.MaximumMemoryUsageMB;
            set => Options.CacheOptions.MaximumMemoryUsageMB = value;
        }

        [Obsolete("Legacy parameter; no longer used")]
        public float CacheMinimumFreeMemoryMB
        {
            get => Options.CacheOptions.MinimumFreeMemoryMB;
            set
            {
                if (Options.CacheOptions.MinimumFreeMemoryMB < 10)
                {
                    Options.CacheOptions.MinimumFreeMemoryMB = 10;
                }

                Options.CacheOptions.MinimumFreeMemoryMB = value;
            }
        }

        [Obsolete("Legacy parameter; no longer used")]
        public int CacheSpectraToRetainInMemory
        {
            get => Options.CacheOptions.SpectraToRetainInMemory;
            set => Options.CacheOptions.SpectraToRetainInMemory = value;
        }

        #endregion
        // ReSharper restore UnusedMember.Global

        public override void AbortProcessingNow()
        {
            AbortProcessing = true;
            Options.AbortProcessing = true;
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
            DataInput.clsDataImport dataImporterBase)
        {
            var success = true;
            var inputFileName = Path.GetFileName(inputFilePathFull);
            var similarParentIonUpdateCount = 0;

            try
            {
                var bpiWriter = new clsBPIWriter();
                RegisterEvents(bpiWriter);

                var xmlResultsWriter = new clsXMLResultsWriter(Options);
                RegisterEvents(xmlResultsWriter);

                // ---------------------------------------------------------
                // Save the BPIs and TICs
                // ---------------------------------------------------------

                UpdateProcessingStep(eProcessingStepConstants.SaveBPI);
                UpdateOverallProgress("Processing Data for " + inputFileName);
                SetSubtaskProcessingStepPct(0, "Saving chromatograms to disk");
                UpdatePeakMemoryUsage();

                if (Options.SkipSICAndRawDataProcessing || !Options.ExportRawDataOnly)
                {
                    LogMessage("ProcessFile: Call SaveBPIs");
                    bpiWriter.SaveBPIs(scanList, spectraCache, inputFilePathFull, outputDirectoryPath);
                }

                // ---------------------------------------------------------
                // Close the ScanStats file handle
                // ---------------------------------------------------------
                try
                {
                    LogMessage("ProcessFile: Close outputFileHandles.ScanStats");

                    dataOutputHandler.OutputFileHandles.CloseScanStats();
                }
                catch (Exception ex)
                {
                    // Ignore errors here
                }

                // ---------------------------------------------------------
                // Create the DatasetInfo XML file
                // ---------------------------------------------------------

                LogMessage("ProcessFile: Create DatasetInfo File");
                dataOutputHandler.CreateDatasetInfoFile(inputFileName, outputDirectoryPath, scanTracking, datasetFileInfo);

                if (Options.SkipSICAndRawDataProcessing)
                {
                    LogMessage("ProcessFile: Skipping SIC Processing");

                    SetDefaultPeakLocValues(scanList);
                }
                else
                {
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
                        // Possibly create the Tab-separated values SIC details output file
                        // ---------------------------------------------------------
                        if (Options.WriteDetailedSICDataFile)
                        {
                            success = dataOutputHandler.InitializeSICDetailsTextFile(inputFilePathFull, outputDirectoryPath);
                            if (!success)
                            {
                                SetLocalErrorCode(eMasicErrorCodes.OutputFileWriteError);
                                return false;
                            }
                        }

                        // ---------------------------------------------------------
                        // Create the XML output file
                        // ---------------------------------------------------------
                        success = xmlResultsWriter.XMLOutputFileInitialize(inputFilePathFull, outputDirectoryPath, dataOutputHandler, scanList, spectraCache, Options.SICOptions, Options.BinningOptions);
                        if (!success)
                        {
                            SetLocalErrorCode(eMasicErrorCodes.OutputFileWriteError);
                            return false;
                        }

                        // ---------------------------------------------------------
                        // Create the selected ion chromatograms (SICs)
                        // For each one, find the peaks and make an entry to the XML output file
                        // ---------------------------------------------------------

                        UpdateProcessingStep(eProcessingStepConstants.CreateSICsAndFindPeaks);
                        SetSubtaskProcessingStepPct(0);
                        UpdatePeakMemoryUsage();

                        LogMessage("ProcessFile: Call CreateParentIonSICs");
                        var sicProcessor = new clsSICProcessing(mMASICPeakFinder, mrmProcessor);
                        RegisterEvents(sicProcessor);

                        success = sicProcessor.CreateParentIonSICs(scanList, spectraCache, Options, dataOutputHandler, sicProcessor, xmlResultsWriter);

                        if (!success)
                        {
                            SetLocalErrorCode(eMasicErrorCodes.CreateSICsError, true);
                            return false;
                        }
                    }

                    if (!(Options.SkipMSMSProcessing || Options.ExportRawDataOnly))
                    {
                        // ---------------------------------------------------------
                        // Find Similar Parent Ions
                        // ---------------------------------------------------------

                        UpdateProcessingStep(eProcessingStepConstants.FindSimilarParentIons);
                        SetSubtaskProcessingStepPct(0);
                        UpdatePeakMemoryUsage();

                        LogMessage("ProcessFile: Call FindSimilarParentIons");
                        success = parentIonProcessor.FindSimilarParentIons(scanList, spectraCache, Options, dataImporterBase, ref similarParentIonUpdateCount);

                        if (!success)
                        {
                            SetLocalErrorCode(eMasicErrorCodes.FindSimilarParentIonsError, true);
                            return false;
                        }
                    }
                }

                if (Options.WriteExtendedStats && !Options.ExportRawDataOnly)
                {
                    // ---------------------------------------------------------
                    // Save Extended Scan Stats Files
                    // ---------------------------------------------------------

                    UpdateProcessingStep(eProcessingStepConstants.SaveExtendedScanStatsFiles);
                    SetSubtaskProcessingStepPct(0);
                    UpdatePeakMemoryUsage();

                    LogMessage("ProcessFile: Call SaveExtendedScanStatsFiles");
                    success = dataOutputHandler.ExtendedStatsWriter.SaveExtendedScanStatsFiles(
                        scanList, inputFileName, outputDirectoryPath, Options.IncludeHeadersInExportFile);

                    if (!success)
                    {
                        SetLocalErrorCode(eMasicErrorCodes.OutputFileWriteError, true);
                        return false;
                    }
                }

                // ---------------------------------------------------------
                // Save SIC Stats Flat File
                // ---------------------------------------------------------

                UpdateProcessingStep(eProcessingStepConstants.SaveSICStatsFlatFile);
                SetSubtaskProcessingStepPct(0);
                UpdatePeakMemoryUsage();

                if (!Options.ExportRawDataOnly)
                {
                    var sicStatsWriter = new clsSICStatsWriter();
                    RegisterEvents(sicStatsWriter);

                    LogMessage("ProcessFile: Call SaveSICStatsFlatFile");
                    success = sicStatsWriter.SaveSICStatsFlatFile(scanList, inputFileName, outputDirectoryPath, Options, dataOutputHandler);

                    if (!success)
                    {
                        SetLocalErrorCode(eMasicErrorCodes.OutputFileWriteError, true);
                        return false;
                    }
                }

                UpdateProcessingStep(eProcessingStepConstants.CloseOpenFileHandles);
                SetSubtaskProcessingStepPct(0);
                UpdatePeakMemoryUsage();

                if (!(Options.SkipSICAndRawDataProcessing || Options.ExportRawDataOnly))
                {
                    // ---------------------------------------------------------
                    // Write processing stats to the XML output file
                    // ---------------------------------------------------------

                    LogMessage("ProcessFile: Call FinalizeXMLFile");
                    var processingTimeSec = GetTotalProcessingTimeSec();
                    success = xmlResultsWriter.XMLOutputFileFinalize(dataOutputHandler, scanList, spectraCache,
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
                    LogMessage("ProcessFile: Call SaveHeaderGlossary");
                    dataOutputHandler.SaveHeaderGlossary(scanList, inputFileName, outputDirectoryPath);
                }

                if (!(Options.SkipSICAndRawDataProcessing || Options.ExportRawDataOnly) && similarParentIonUpdateCount > 0)
                {
                    // ---------------------------------------------------------
                    // Reopen the XML file and update the entries for those ions in scanList that had their
                    // Optimal peak apex scan numbers updated
                    // ---------------------------------------------------------

                    UpdateProcessingStep(eProcessingStepConstants.UpdateXMLFileWithNewOptimalPeakApexValues);
                    SetSubtaskProcessingStepPct(0);
                    UpdatePeakMemoryUsage();

                    LogMessage("ProcessFile: Call XmlOutputFileUpdateEntries");
                    xmlResultsWriter.XmlOutputFileUpdateEntries(scanList, inputFileName, outputDirectoryPath);
                }
            }
            catch (Exception ex)
            {
                success = false;
                LogErrors("FindSICsAndWriteOutput", "Error saving results to: " + outputDirectoryPath, ex, eMasicErrorCodes.OutputFileWriteError);
            }

            return success;
        }

        public override IList<string> GetDefaultExtensionsToParse()
        {
            return DataInput.clsDataImport.GetDefaultExtensionsToParse();
        }

        public override string GetErrorMessage()
        {
            // Returns String.Empty if no error

            string errorMessage;

            if (ErrorCode == ProcessFilesErrorCodes.LocalizedError ||
                ErrorCode == ProcessFilesErrorCodes.NoError)
            {
                switch (mLocalErrorCode)
                {
                    case eMasicErrorCodes.NoError:
                        errorMessage = string.Empty;
                        break;
                    case eMasicErrorCodes.InvalidDatasetLookupFilePath:
                        errorMessage = "Invalid dataset lookup file path";
                        break;
                    case eMasicErrorCodes.UnknownFileExtension:
                        errorMessage = "Unknown file extension";
                        break;
                    case eMasicErrorCodes.InputFileAccessError:
                        errorMessage = "Input file access error";
                        break;
                    case eMasicErrorCodes.InvalidDatasetID:
                        errorMessage = "Invalid dataset number";
                        break;
                    case eMasicErrorCodes.CreateSICsError:
                        errorMessage = "Create SIC's error";
                        break;
                    case eMasicErrorCodes.FindSICPeaksError:
                        errorMessage = "Error finding SIC peaks";
                        break;
                    case eMasicErrorCodes.InvalidCustomSICValues:
                        errorMessage = "Invalid custom SIC values";
                        break;
                    case eMasicErrorCodes.NoParentIonsFoundInInputFile:
                        errorMessage = "No parent ions were found in the input file (additionally, no custom SIC values were defined)";
                        break;
                    case eMasicErrorCodes.NoSurveyScansFoundInInputFile:
                        errorMessage = "No survey scans were found in the input file (do you have a Scan Range filter defined?)";
                        break;
                    case eMasicErrorCodes.FindSimilarParentIonsError:
                        errorMessage = "Find similar parent ions error";
                        break;
                    case eMasicErrorCodes.InputFileDataReadError:
                        errorMessage = "Error reading data from input file";
                        break;
                    case eMasicErrorCodes.OutputFileWriteError:
                        errorMessage = "Error writing data to output file";
                        break;
                    case eMasicErrorCodes.FileIOPermissionsError:
                        errorMessage = "File IO Permissions Error";
                        break;
                    case eMasicErrorCodes.ErrorCreatingSpectrumCacheDirectory:
                        errorMessage = "Error creating spectrum cache directory";
                        break;
                    case eMasicErrorCodes.ErrorCachingSpectrum:
                        errorMessage = "Error caching spectrum";
                        break;
                    case eMasicErrorCodes.ErrorUncachingSpectrum:
                        errorMessage = "Error uncaching spectrum";
                        break;
                    case eMasicErrorCodes.ErrorDeletingCachedSpectrumFiles:
                        errorMessage = "Error deleting cached spectrum files";
                        break;
                    case eMasicErrorCodes.UnspecifiedError:
                        errorMessage = "Unspecified localized error";
                        break;
                    default:
                        // This shouldn't happen
                        errorMessage = "Unknown error state";
                        break;
                }
            }
            else
            {
                errorMessage = GetBaseClassErrorMessage();
            }

            return errorMessage;
        }

        private float GetFreeMemoryMB()
        {
            // Returns the amount of free memory, in MB

            var freeMemoryMB = SystemInfo.GetFreeMemoryMB();

            return freeMemoryMB;
        }

        private float GetProcessMemoryUsageMB()
        {
            // Obtain a handle to the current process
            var objProcess = Process.GetCurrentProcess();

            // The WorkingSet is the total physical memory usage
            return (float)(objProcess.WorkingSet64 / 1024.0 / 1024);
        }

        private float GetTotalProcessingTimeSec()
        {
            var objProcess = Process.GetCurrentProcess();

            return (float)(objProcess.TotalProcessorTime.TotalSeconds);
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
            string inputFilePathFull,
            string outputDirectoryPath,
            clsDataOutput dataOutputHandler,
            clsParentIonProcessing parentIonProcessor,
            clsScanTracking scanTracking,
            clsScanList scanList,
            clsSpectraCache spectraCache,
            out DataInput.clsDataImport dataImporterBase,
            out DatasetFileInfo datasetFileInfo)
        {
            bool success;
            datasetFileInfo = new DatasetFileInfo();

            try
            {
                // ---------------------------------------------------------
                // Define inputFileName (which is referenced several times below)
                // ---------------------------------------------------------
                var inputFileName = Path.GetFileName(inputFilePathFull);

                // ---------------------------------------------------------
                // Create the _ScanStats.txt file
                // ---------------------------------------------------------
                dataOutputHandler.OpenOutputFileHandles(inputFileName, outputDirectoryPath, Options.IncludeHeadersInExportFile);

                // ---------------------------------------------------------
                // Read the mass spectra from the input data file
                // ---------------------------------------------------------

                UpdateProcessingStep(eProcessingStepConstants.ReadDataFile);
                SetSubtaskProcessingStepPct(0);
                UpdatePeakMemoryUsage();
                mStatusMessage = string.Empty;

                if (Options.SkipSICAndRawDataProcessing)
                {
                    Options.ExportRawDataOnly = false;
                }

                var keepRawMSSpectra = !Options.SkipSICAndRawDataProcessing || Options.ExportRawDataOnly;

                Options.SICOptions.ValidateSICOptions();

                switch (Path.GetExtension(inputFileName).ToUpper())
                {
                    case DataInput.clsDataImport.THERMO_RAW_FILE_EXTENSION:
                        // Open the .Raw file and obtain the scan information

                        var dataImporter = new DataInput.clsDataImportThermoRaw(Options, mMASICPeakFinder, parentIonProcessor, scanTracking);
                        RegisterDataImportEvents(dataImporter);
                        dataImporterBase = dataImporter;

                        success = dataImporter.ExtractScanInfoFromXcaliburDataFile(
                            inputFilePathFull,
                            scanList, spectraCache, dataOutputHandler,
                            keepRawMSSpectra,
                            !Options.SkipMSMSProcessing);

                        datasetFileInfo = dataImporter.DatasetFileInfo;
                        break;

                    case DataInput.clsDataImport.MZ_ML_FILE_EXTENSION:
                        // Open the .mzML file and obtain the scan information

                        var dataImporterMzML = new DataInput.clsDataImportMSXml(Options, mMASICPeakFinder, parentIonProcessor, scanTracking);
                        RegisterDataImportEvents(dataImporterMzML);
                        dataImporterBase = dataImporterMzML;

                        success = dataImporterMzML.ExtractScanInfoFromMzMLDataFile(
                            inputFilePathFull,
                            scanList, spectraCache, dataOutputHandler,
                            keepRawMSSpectra,
                            !Options.SkipMSMSProcessing);

                        datasetFileInfo = dataImporterMzML.DatasetFileInfo;
                        break;

                    case DataInput.clsDataImport.MZ_XML_FILE_EXTENSION1:
                    case DataInput.clsDataImport.MZ_XML_FILE_EXTENSION2:
                        // Open the .mzXML file and obtain the scan information

                        var dataImporterMzXML = new DataInput.clsDataImportMSXml(Options, mMASICPeakFinder, parentIonProcessor, scanTracking);
                        RegisterDataImportEvents(dataImporterMzXML);
                        dataImporterBase = dataImporterMzXML;

                        success = dataImporterMzXML.ExtractScanInfoFromMzXMLDataFile(
                            inputFilePathFull,
                            scanList, spectraCache, dataOutputHandler,
                            keepRawMSSpectra,
                            !Options.SkipMSMSProcessing);

                        datasetFileInfo = dataImporterMzXML.DatasetFileInfo;
                        break;

                    case DataInput.clsDataImport.MZ_DATA_FILE_EXTENSION1:
                    case DataInput.clsDataImport.MZ_DATA_FILE_EXTENSION2:
                        // Open the .mzData file and obtain the scan information

                        var dataImporterMzData = new DataInput.clsDataImportMSXml(Options, mMASICPeakFinder, parentIonProcessor, scanTracking);
                        RegisterDataImportEvents(dataImporterMzData);
                        dataImporterBase = dataImporterMzData;

                        success = dataImporterMzData.ExtractScanInfoFromMzDataFile(
                            inputFilePathFull,
                            scanList, spectraCache, dataOutputHandler,
                            keepRawMSSpectra, !Options.SkipMSMSProcessing);

                        datasetFileInfo = dataImporterMzData.DatasetFileInfo;
                        break;

                    case DataInput.clsDataImport.AGILENT_MSMS_FILE_EXTENSION:
                    case DataInput.clsDataImport.AGILENT_MS_FILE_EXTENSION:
                        // Open the .MGF and .CDF files to obtain the scan information

                        var dataImporterMGF = new DataInput.clsDataImportMGFandCDF(Options, mMASICPeakFinder, parentIonProcessor, scanTracking);
                        RegisterDataImportEvents(dataImporterMGF);
                        dataImporterBase = dataImporterMGF;

                        success = dataImporterMGF.ExtractScanInfoFromMGFandCDF(
                            inputFilePathFull,
                            scanList, spectraCache, dataOutputHandler,
                            keepRawMSSpectra, !Options.SkipMSMSProcessing);

                        datasetFileInfo = dataImporterMGF.DatasetFileInfo;
                        break;

                    default:
                        mStatusMessage = "Unknown file extension: " + Path.GetExtension(inputFilePathFull);
                        SetLocalErrorCode(eMasicErrorCodes.UnknownFileExtension);
                        success = false;

                        // Instantiate this object to avoid a warning below about the object potentially not being initialized
                        // In reality, an Exit Try statement will be reached and the potentially problematic use will therefore not get encountered
                        datasetFileInfo = new DatasetFileInfo();
                        dataImporterBase = null;
                        break;
                }

                if (!success)
                {
                    if (mLocalErrorCode == eMasicErrorCodes.NoParentIonsFoundInInputFile && string.IsNullOrWhiteSpace(mStatusMessage))
                    {
                        mStatusMessage = "None of the spectra in the input file was within the specified scan number and/or scan time range";
                    }

                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError, true);
                }
            }
            catch (Exception ex)
            {
                success = false;
                LogErrors("ProcessFile", "Error accessing input data file: " + inputFilePathFull, ex, eMasicErrorCodes.InputFileDataReadError);
                dataImporterBase = null;
            }

            return success;
        }

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
            eMasicErrorCodes newErrorCode = eMasicErrorCodes.NoError)
        {
            Options.StatusMessage = message;

            var messageWithoutCRLF = Options.StatusMessage.Replace(Environment.NewLine, "; ");
            if (ex == null)
            {
                ex = new Exception("Error");
            }
            else if (!string.IsNullOrEmpty(ex.Message) && !message.Contains(ex.Message))
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

            Console.WriteLine(StackTraceFormatter.GetExceptionStackTraceMultiLine(ex));

            if (newErrorCode != eMasicErrorCodes.NoError)
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
        /// <returns></returns>
        public override bool ProcessFile(
            string inputFilePath,
            string outputDirectoryPath,
            string parameterFilePath,
            bool resetErrorCode)
        {
            bool success = default, doNotProcess = default;

            var inputFilePathFull = string.Empty;

            if (!mLoggedMASICVersion)
            {
                LogMessage("Starting MASIC v" + GetAppVersion(mFileDate));
                Console.WriteLine();
                mLoggedMASICVersion = true;
            }

            if (resetErrorCode)
            {
                SetLocalErrorCode(eMasicErrorCodes.NoError);
            }

            Options.OutputDirectoryPath = outputDirectoryPath;

            mSubtaskProcessingStepPct = 0;
            UpdateProcessingStep(eProcessingStepConstants.NewTask, true);
            ResetProgress("Starting calculations");

            mStatusMessage = string.Empty;

            UpdateStatusFile(true);

            if (!Options.LoadParameterFileSettings(parameterFilePath, inputFilePath))
            {
                SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidParameterFile);
                mStatusMessage = "Parameter file load error: " + parameterFilePath;

                ShowErrorMessage(mStatusMessage);

                if (ErrorCode == ProcessFilesErrorCodes.NoError)
                {
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidParameterFile);
                }

                UpdateProcessingStep(eProcessingStepConstants.Cancelled, true);

                LogMessage("Processing ended in error");
                return false;
            }

            var dataOutputHandler = new clsDataOutput(Options);
            RegisterEvents(dataOutputHandler);
            try
            {
                var keepProcessing = true;
                // If a Custom SICList file is defined, then load the custom SIC values now
                if (Options.CustomSICList.CustomSICListFileName.Length > 0)
                {
                    var sicListReader = new DataInput.clsCustomSICListReader(Options.CustomSICList);
                    RegisterEvents(sicListReader);

                    LogMessage("ProcessFile: Reading custom SIC values file: " + Options.CustomSICList.CustomSICListFileName);
                    success = sicListReader.LoadCustomSICListFromFile(Options.CustomSICList.CustomSICListFileName);
                    if (!success)
                    {
                        SetLocalErrorCode(eMasicErrorCodes.InvalidCustomSICValues);
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
                    mStatusMessage = "Parsing " + Path.GetFileName(inputFilePath);

                    success = CleanupFilePaths(ref inputFilePath, ref outputDirectoryPath);
                    Options.OutputDirectoryPath = outputDirectoryPath;

                    if (success)
                    {
                        var dbAccessor = new clsDatabaseAccess(Options);
                        RegisterEvents(dbAccessor);

                        Options.SICOptions.DatasetID = dbAccessor.LookupDatasetID(inputFilePath, Options.DatasetLookupFilePath, Options.SICOptions.DatasetID);

                        if (LocalErrorCode != eMasicErrorCodes.NoError)
                        {
                            if (LocalErrorCode == eMasicErrorCodes.InvalidDatasetID || LocalErrorCode == eMasicErrorCodes.InvalidDatasetLookupFilePath)
                            {
                                // Ignore this error
                                SetLocalErrorCode(eMasicErrorCodes.NoError);
                                success = true;
                            }
                            else
                            {
                                success = false;
                            }
                        }
                    }

                    if (!success)
                    {
                        if (mLocalErrorCode == eMasicErrorCodes.NoError)
                            SetBaseClassErrorCode(ProcessFilesErrorCodes.FilePathError);
                        keepProcessing = false;
                    }
                }

                if (keepProcessing)
                {
                    try
                    {
                        // ---------------------------------------------------------
                        // See if an output XML file already exists
                        // If it does, open it and read the parameters used
                        // If they match the current analysis parameters, and if the input file specs match the input file, then
                        // do not reprocess
                        // ---------------------------------------------------------

                        // Obtain the full path to the input file
                        var inputFileInfo = new FileInfo(inputFilePath);
                        inputFilePathFull = inputFileInfo.FullName;

                        LogMessage("Checking for existing results in the output path: " + outputDirectoryPath);

                        doNotProcess = dataOutputHandler.CheckForExistingResults(inputFilePathFull, outputDirectoryPath, Options);

                        if (doNotProcess)
                        {
                            LogMessage("Existing results found; data will not be reprocessed");
                        }
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        LogErrors("ProcessFile", "Error checking for existing results file", ex, eMasicErrorCodes.InputFileDataReadError);
                    }

                    if (doNotProcess)
                    {
                        success = true;
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

                        // The following should work for testing access permissions, but it doesn't
                        // Dim objFilePermissionTest As New Security.Permissions.FileIOPermission(Security.Permissions.FileIOPermissionAccess.AllAccess, outputDirectoryPath)
                        // ' The following should throw an exception if the current user doesn't have read/write access; however, no exception is thrown for me
                        // objFilePermissionTest.Demand()
                        // objFilePermissionTest.Assert()

                        LogMessage("Checking for write permission in the output path: " + outputDirectoryPath);

                        var outputFileTestPath = Path.Combine(outputDirectoryPath, "TestOutputFile" + DateTime.UtcNow.Ticks + ".tmp");

                        using (var fsOutFileTest = new StreamWriter(outputFileTestPath, false))
                        {
                            fsOutFileTest.WriteLine("Test");
                        }

                        // Wait 250 msec, then delete the file
                        System.Threading.Thread.Sleep(250);
                        File.Delete(outputFileTestPath);
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        LogErrors("ProcessFile", "The current user does not have write permission for the output directory: " + outputDirectoryPath, ex, eMasicErrorCodes.FileIOPermissionsError);
                    }

                    if (!success)
                    {
                        SetLocalErrorCode(eMasicErrorCodes.FileIOPermissionsError);
                    }
                    else
                    {
                        // ---------------------------------------------------------
                        // Reset the processing stats
                        // ---------------------------------------------------------

                        InitializeMemoryManagementOptions(mProcessingStats);

                        // ---------------------------------------------------------
                        // Instantiate the SpectraCache
                        // ---------------------------------------------------------

                        using (var spectraCache = new clsSpectraCache(Options.CacheOptions)
                        {
                            DiskCachingAlwaysDisabled = Options.CacheOptions.DiskCachingAlwaysDisabled,
                            CacheDirectoryPath = Options.CacheOptions.DirectoryPath,
                            CacheSpectraToRetainInMemory = Options.CacheOptions.SpectraToRetainInMemory
                        })
                        {
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

                            success = LoadData(inputFilePathFull,
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
                            if (!success)
                            {
                                if (string.IsNullOrEmpty(mStatusMessage))
                                {
                                    mStatusMessage = "Unable to parse file; unknown error";
                                }
                                else
                                {
                                    mStatusMessage = "Unable to parse file: " + mStatusMessage;
                                }

                                ShowErrorMessage(mStatusMessage);
                            }
                            else
                            {
                                // ---------------------------------------------------------
                                // Find the Selected Ion Chromatograms, reporter ions, etc. and write the results to disk
                                // ---------------------------------------------------------

                                success = FindSICsAndWriteOutput(
                                    inputFilePathFull, outputDirectoryPath,
                                    scanList, spectraCache, dataOutputHandler, scanTracking,
                                    datasetFileInfo, parentIonProcessor, dataImporterBase);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                LogErrors("ProcessFile", "Error in ProcessFile", ex, eMasicErrorCodes.UnspecifiedError);
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
                if (doNotProcess)
                {
                    mStatusMessage = "Existing valid results were found; processing was not repeated.";
                    ShowMessage(mStatusMessage);
                }
                else if (success)
                {
                    mStatusMessage = "Processing complete.  Results can be found in directory: " + outputDirectoryPath;
                    ShowMessage(mStatusMessage);
                }
                else if (LocalErrorCode == eMasicErrorCodes.NoError)
                {
                    mStatusMessage = "Error Code " + ErrorCode + ": " + GetErrorMessage();
                    ShowErrorMessage(mStatusMessage);
                }
                else
                {
                    mStatusMessage = "Error Code " + LocalErrorCode + ": " + GetErrorMessage();
                    ShowErrorMessage(mStatusMessage);
                }

                LogMessage(string.Format("ProcessingStats: Memory Usage At Start (MB) = {0:F2}", mProcessingStats.MemoryUsageMBAtStart));
                LogMessage(string.Format("ProcessingStats: Peak memory usage (MB) = {0:F2}", mProcessingStats.PeakMemoryUsageMB));

                LogMessage(string.Format("ProcessingStats: File Load Time (seconds) = {0:N0}", mProcessingStats.FileLoadEndTime.Subtract(mProcessingStats.FileLoadStartTime).TotalSeconds));
                LogMessage(string.Format("ProcessingStats: Memory Usage During Load (MB) = {0:F2}", mProcessingStats.MemoryUsageMBDuringLoad));

                LogMessage(string.Format("ProcessingStats: Processing Time (seconds) = {0:N0}", mProcessingStats.ProcessingEndTime.Subtract(mProcessingStats.ProcessingStartTime).TotalSeconds));
                LogMessage(string.Format("ProcessingStats: Memory Usage At End (MB) = {0:F2}", mProcessingStats.MemoryUsageMBAtEnd));

                LogMessage(string.Format("ProcessingStats: Cache Event Count = {0:N0}", mProcessingStats.CacheEventCount));
                LogMessage(string.Format("ProcessingStats: UncCache Event Count = {0:N0}", mProcessingStats.UnCacheEventCount));

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
                LogErrors("ProcessFile", "Error in ProcessFile (Cleanup)", ex, eMasicErrorCodes.UnspecifiedError);
            }

            if (success)
            {
                Options.SICOptions.DatasetID += 1;
            }

            if (success)
            {
                UpdateProcessingStep(eProcessingStepConstants.Complete, true);
            }
            else
            {
                UpdateProcessingStep(eProcessingStepConstants.Cancelled, true);
            }

            return success;
        }

        private void RegisterDataImportEvents(DataInput.clsDataImport dataImporter)
        {
            RegisterEvents(dataImporter);
            dataImporter.UpdateMemoryUsageEvent += UpdateMemoryUsageEventHandler;
        }

        private void RegisterEventsBase(IEventNotifier oClass)
        {
            oClass.StatusEvent += MessageEventHandler;
            oClass.ErrorEvent += ErrorEventHandler;
            oClass.WarningEvent += WarningEventHandler;
            oClass.ProgressUpdate += ProgressUpdateHandler;
        }

        private void RegisterEvents(clsMasicEventNotifier oClass)
        {
            RegisterEventsBase(oClass);

            oClass.UpdateCacheStatsEvent += UpdatedCacheStatsEventHandler;
            oClass.UpdateBaseClassErrorCodeEvent += UpdateBaseClassErrorCodeEventHandler;
            oClass.UpdateErrorCodeEvent += UpdateErrorCodeEventHandler;
        }

        // ReSharper disable UnusedMember.Global

        [Obsolete("Use Options.SaveParameterFileSettings")]
        public bool SaveParameterFileSettings(string parameterFilePath)
        {
            var success = Options.SaveParameterFileSettings(parameterFilePath);
            return success;
        }

        [Obsolete("Use Options.ReporterIons.SetReporterIons")]
        public void SetReporterIons(double[] reporterIonMZList)
        {
            Options.ReporterIons.SetReporterIons(reporterIonMZList);
        }

        [Obsolete("Use Options.ReporterIons.SetReporterIons")]
        public void SetReporterIons(double[] reporterIonMZList, double mzToleranceDa)
        {
            Options.ReporterIons.SetReporterIons(reporterIonMZList, mzToleranceDa);
        }

        [Obsolete("Use Options.ReporterIons.SetReporterIons")]
        public void SetReporterIons(List<clsReporterIonInfo> reporterIons)
        {
            Options.ReporterIons.SetReporterIons(reporterIons, true);
        }

        [Obsolete("Use Options.ReporterIons.SetReporterIonMassMode")]
        public void SetReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants eReporterIonMassMode)
        {
            Options.ReporterIons.SetReporterIonMassMode(eReporterIonMassMode);
        }

        [Obsolete("Use Options.ReporterIons.SetReporterIonMassMode")]
        public void SetReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants eReporterIonMassMode, double mzToleranceDa)
        {
            Options.ReporterIons.SetReporterIonMassMode(eReporterIonMassMode, mzToleranceDa);
        }

        // ReSharper restore UnusedMember.Global

        private void SetDefaultPeakLocValues(clsScanList scanList)
        {
            try
            {
                for (var parentIonIndex = 0; parentIonIndex <= scanList.ParentIons.Count - 1; parentIonIndex++)
                {
                    var parentIon = scanList.ParentIons[parentIonIndex];
                    var scanIndexObserved = parentIon.SurveyScanIndex;

                    var sicStats = parentIon.SICStats;
                    sicStats.ScanTypeForPeakIndices = clsScanList.eScanTypeConstants.SurveyScan;
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

        private void SetLocalErrorCode(eMasicErrorCodes newErrorCode, bool leaveExistingErrorCodeUnchanged = false)
        {
            if (leaveExistingErrorCodeUnchanged && mLocalErrorCode != eMasicErrorCodes.NoError)
            {
                // An error code is already defined; do not change it
            }
            else
            {
                mLocalErrorCode = newErrorCode;

                if (newErrorCode == eMasicErrorCodes.NoError)
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

            if (Math.Abs(subtaskPercentComplete - mSubtaskProcessingStepPct) > float.Epsilon)
            {
                raiseEventNow = true;
                mSubtaskProcessingStepPct = subtaskPercentComplete;
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
            mSubtaskDescription = message;
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
            // Cancelled = 99
            // Complete = 100

            float[] weightingFactors;

            if (Options.SkipMSMSProcessing)
            {
                // Step                    0  1      2       3  4  5       6       7       8
                weightingFactors = new[] { 0, 0.97F, 0.002F, 0, 0, 0.001F, 0.025F, 0.001F, 0.001F };            // The sum of these factors should be 1.00
            }
            else
            {
                // Step                    0  1       2       3      4      5       6       7       8
                weightingFactors = new[] { 0, 0.194F, 0.003F, 0.65F, 0.09F, 0.001F, 0.006F, 0.001F, 0.055F };   // The sum of these factors should be 1.00
            }

            try
            {
                var currentStep = (int)mProcessingStep;
                if (currentStep >= weightingFactors.Length)
                    currentStep = weightingFactors.Length - 1;

                float overallPctCompleted = 0;
                for (var index = 0; index <= currentStep - 1; index++)
                    overallPctCompleted += weightingFactors[index] * 100;

                overallPctCompleted += weightingFactors[currentStep] * mSubtaskProcessingStepPct;

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

        private void UpdateProcessingStep(eProcessingStepConstants eNewProcessingStep, bool forceStatusFileUpdate = false)
        {
            mProcessingStep = eNewProcessingStep;
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
                        writer.WriteElementString("ProcessingStep", mProcessingStep.ToString());
                        writer.WriteElementString("Progress", StringUtilities.DblToString(mProgressPercentComplete, 2));
                        writer.WriteElementString("Error", GetErrorMessage());
                        writer.WriteEndElement();

                        writer.WriteStartElement("Statistics");
                        writer.WriteElementString("FreeMemoryMB", StringUtilities.DblToString(GetFreeMemoryMB(), 1));
                        writer.WriteElementString("MemoryUsageMB", StringUtilities.DblToString(GetProcessMemoryUsageMB(), 1));
                        writer.WriteElementString("PeakMemoryUsageMB", StringUtilities.DblToString(mProcessingStats.PeakMemoryUsageMB, 1));

                        writer.WriteElementString("CacheEventCount", mProcessingStats.CacheEventCount.ToString());
                        writer.WriteElementString("UnCacheEventCount", mProcessingStats.UnCacheEventCount.ToString());

                        writer.WriteElementString("ProcessingTimeSec", StringUtilities.DblToString(GetTotalProcessingTimeSec(), 2));
                        writer.WriteEndElement();

                        writer.WriteEndElement();  // End the "Root" element.
                        writer.WriteEndDocument(); // End the document
                    }

                    // Copy the temporary file to the real one
                    File.Copy(tempPath, statusFilePath, true);
                    File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    // Ignore any errors
                }
            }
        }

        #region "Event Handlers"

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

        private void UpdatedCacheStatsEventHandler(int cacheEventCount, int unCacheEventCount)
        {
            mProcessingStats.CacheEventCount = cacheEventCount;
            mProcessingStats.UnCacheEventCount = unCacheEventCount;
        }

        private void UpdateBaseClassErrorCodeEventHandler(ProcessFilesErrorCodes eErrorCode)
        {
            SetBaseClassErrorCode(eErrorCode);
        }

        private void UpdateErrorCodeEventHandler(eMasicErrorCodes eErrorCode, bool leaveExistingErrorCodeUnchanged)
        {
            SetLocalErrorCode(eErrorCode, leaveExistingErrorCodeUnchanged);
        }

        private void UpdateMemoryUsageEventHandler()
        {
            // Record the current memory usage
            var memoryUsageMB = GetProcessMemoryUsageMB();
            if (memoryUsageMB > mProcessingStats.MemoryUsageMBDuringLoad)
            {
                mProcessingStats.MemoryUsageMBDuringLoad = memoryUsageMB;
            }
        }

        #endregion
    }
}
