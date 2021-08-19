using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MASIC.DatasetStats;
using MASIC.Options;
using PRISM;

namespace MASIC.DataInput
{
    /// <summary>
    /// Base class for reading spectra from mass spec data files
    /// </summary>
    public abstract class clsDataImport : clsMasicEventNotifier
    {
        // Ignore Spelling: centroided

        /// <summary>
        /// Thermo .raw file extension
        /// </summary>
        /// <remarks>Needs to be all caps because of the switch statement in clsMASIC.LoadData</remarks>
        public const string THERMO_RAW_FILE_EXTENSION = ".RAW";

        /// <summary>
        /// .mzML file extension
        /// </summary>
        public const string MZ_ML_FILE_EXTENSION = ".MZML";

        /// <summary>
        /// .mzXML file extension
        /// </summary>
        public const string MZ_XML_FILE_EXTENSION1 = ".MZXML";

        /// <summary>
        /// Generic .mzXML file name
        /// </summary>
        public const string MZ_XML_FILE_EXTENSION2 = "MZXML.XML";

        /// <summary>
        /// .mzData file extension
        /// </summary>
        public const string MZ_DATA_FILE_EXTENSION1 = ".MZDATA";

        /// <summary>
        /// Generic .mzData file name
        /// </summary>
        public const string MZ_DATA_FILE_EXTENSION2 = "MZDATA.XML";

        /// <summary>
        /// .mgf file extension
        /// </summary>
        /// <remarks>
        /// Agilent files must have been exported to a .MGF and .CDF file pair prior to using MASIC
        /// </remarks>
        public const string AGILENT_MSMS_FILE_EXTENSION = ".MGF";

        /// <summary>
        /// .cdf file extension
        /// </summary>
        public const string AGILENT_MS_FILE_EXTENSION = ".CDF";

        /// <summary>
        /// .txt file extension
        /// </summary>
        public const string TEXT_FILE_EXTENSION = ".TXT";

        private const int ISOLATION_WIDTH_NOT_FOUND_WARNINGS_TO_SHOW = 5;

        /// <summary>
        /// Number of times a warning should be shown regarding a missing precursor
        /// </summary>
        protected const int PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW = 5;

        /// <summary>
        /// MASIC options
        /// </summary>
        protected readonly MASICOptions mOptions;

        /// <summary>
        /// Parent ion processor
        /// </summary>
        protected readonly clsParentIonProcessing mParentIonProcessor;

        /// <summary>
        /// MASIC peak finder
        /// </summary>
        protected readonly MASICPeakFinder.clsMASICPeakFinder mPeakFinder;

        /// <summary>
        /// Scan tracking
        /// </summary>
        protected readonly clsScanTracking mScanTracking;

        /// <summary>
        /// Dataset file info
        /// </summary>
        protected DatasetFileInfo mDatasetFileInfo;

        /// <summary>
        /// When true, store raw spectra in the spectrum cache
        /// When false, compute the noise level of each spectrum but do not store in the cache
        /// </summary>
        protected bool mKeepRawSpectra;

        /// <summary>
        /// When true, store MS/MS spectra in the spectrum cache
        /// </summary>
        protected bool mKeepMSMSSpectra;

        /// <summary>
        /// Scan index of the most recent survey scan
        /// </summary>
        protected int mLastSurveyScanIndexInMasterSeqOrder;

        /// <summary>
        /// Scan index of the most recent non-zoom survey scan
        /// </summary>
        protected int mLastNonZoomSurveyScanIndex;

        /// <summary>
        /// Last time a log entry was written
        /// </summary>
        protected DateTime mLastLogTime;

        private readonly InterDetect.InterferenceCalculator mInterferenceCalculator;

        private readonly List<InterDetect.Peak> mCachedPrecursorIons;

        /// <summary>
        /// Cached precursor scan
        /// </summary>
        protected int mCachedPrecursorScan;

        private int mIsolationWidthNotFoundCount;
        private int mPrecursorNotFoundCount;
        private int mNextPrecursorNotFoundCountThreshold;

        /// <summary>
        /// Number of scans outside the specified scan number and/or scan time range
        /// </summary>
        protected int mScansOutOfRange;

        /// <summary>
        /// Dataset file info
        /// </summary>
        public DatasetFileInfo DatasetFileInfo => mDatasetFileInfo;

        /// <summary>
        /// This event is used to signify to the calling class that it should update the status of the available memory usage
        /// </summary>
        public event UpdateMemoryUsageEventEventHandler UpdateMemoryUsageEvent;

        /// <summary>
        /// Delegate for the memory usage event handler
        /// </summary>
        public delegate void UpdateMemoryUsageEventEventHandler();

        /// <summary>
        /// Event raised when reporting the current memory usage
        /// </summary>
        protected void OnUpdateMemoryUsage()
        {
            UpdateMemoryUsageEvent?.Invoke();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="masicOptions"></param>
        /// <param name="peakFinder"></param>
        /// <param name="parentIonProcessor"></param>
        /// <param name="scanTracking"></param>
        protected clsDataImport(
            MASICOptions masicOptions,
            MASICPeakFinder.clsMASICPeakFinder peakFinder,
            clsParentIonProcessing parentIonProcessor,
            clsScanTracking scanTracking)
        {
            mOptions = masicOptions;
            mPeakFinder = peakFinder;
            mParentIonProcessor = parentIonProcessor;
            mScanTracking = scanTracking;

            mDatasetFileInfo = new DatasetFileInfo();

            mInterferenceCalculator = new InterDetect.InterferenceCalculator();

            mInterferenceCalculator.StatusEvent += OnStatusEvent;
            mInterferenceCalculator.ErrorEvent += OnErrorEvent;
            mInterferenceCalculator.WarningEvent += InterferenceWarningEventHandler;

            mCachedPrecursorIons = new List<InterDetect.Peak>();
            mCachedPrecursorScan = 0;

            mIsolationWidthNotFoundCount = 0;
            mPrecursorNotFoundCount = 0;
        }

        /// <summary>
        /// Compute the interference in the region centered around parentIonMz
        /// Before calling this method, call UpdateCachedPrecursorScan to store the m/z and intensity values of the precursor spectrum
        /// </summary>
        /// <param name="fragScanNumber">Used for reporting purposes</param>
        /// <param name="precursorScanNumber">Used for reporting purposes</param>
        /// <param name="parentIonMz"></param>
        /// <param name="isolationWidth"></param>
        /// <param name="chargeState"></param>
        /// <returns>
        /// Interference score: fraction of observed peaks that are from the precursor
        /// Larger is better, with a max of 1 and minimum of 0
        /// 1 means all peaks are from the precursor
        /// </returns>
        protected double ComputePrecursorInterference(
            int fragScanNumber,
            int precursorScanNumber,
            double parentIonMz,
            double isolationWidth,
            int chargeState)
        {
            var precursorInfo = new InterDetect.PrecursorIntense(parentIonMz, isolationWidth, chargeState)
            {
                PrecursorScanNumber = precursorScanNumber,
                ScanNumber = fragScanNumber
            };

            mInterferenceCalculator.Interference(precursorInfo, mCachedPrecursorIons);

            return precursorInfo.Interference;
        }

        /// <summary>
        /// Discard data below the noise threshold
        /// </summary>
        /// <param name="msSpectrum"></param>
        /// <param name="noiseThresholdIntensity"></param>
        /// <param name="mzIgnoreRangeStart"></param>
        /// <param name="mzIgnoreRangeEnd"></param>
        /// <param name="noiseThresholdOptions"></param>
        public void DiscardDataBelowNoiseThreshold(
            clsMSSpectrum msSpectrum,
            double noiseThresholdIntensity,
            double mzIgnoreRangeStart,
            double mzIgnoreRangeEnd,
            MASICPeakFinder.clsBaselineNoiseOptions noiseThresholdOptions)
        {
            var ionCountNew = 0;

            try
            {
                switch (noiseThresholdOptions.BaselineNoiseMode)
                {
                    case MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.AbsoluteThreshold:
                        if (noiseThresholdOptions.BaselineNoiseLevelAbsolute > 0)
                        {
                            ionCountNew = 0;
                            for (var ionIndex = 0; ionIndex < msSpectrum.IonCount; ionIndex++)
                            {
                                // Always keep points in the m/z ignore range
                                // If CheckPointInMZIgnoreRange returns true, set pointPassesFilter to true
                                var pointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ[ionIndex], mzIgnoreRangeStart, mzIgnoreRangeEnd);

                                if (!pointPassesFilter)
                                {
                                    // Check the point's intensity against .BaselineNoiseLevelAbsolute
                                    if (msSpectrum.IonsIntensity[ionIndex] >= noiseThresholdOptions.BaselineNoiseLevelAbsolute)
                                    {
                                        pointPassesFilter = true;
                                    }
                                }

                                if (pointPassesFilter)
                                {
                                    msSpectrum.IonsMZ[ionCountNew] = msSpectrum.IonsMZ[ionIndex];
                                    msSpectrum.IonsIntensity[ionCountNew] = msSpectrum.IonsIntensity[ionIndex];
                                    ionCountNew++;
                                }
                            }
                        }
                        else
                        {
                            ionCountNew = msSpectrum.IonCount;
                        }

                        break;
                    case MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMeanByAbundance:
                    case MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMeanByCount:
                    case MASICPeakFinder.clsMASICPeakFinder.NoiseThresholdModes.TrimmedMedianByAbundance:
                        if (noiseThresholdOptions.MinimumSignalToNoiseRatio > 0)
                        {
                            ionCountNew = 0;
                            for (var ionIndex = 0; ionIndex < msSpectrum.IonCount; ionIndex++)
                            {
                                // Always keep points in the m/z ignore range
                                // If CheckPointInMZIgnoreRange returns true, set pointPassesFilter to true
                                var pointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ[ionIndex], mzIgnoreRangeStart, mzIgnoreRangeEnd);

                                if (!pointPassesFilter)
                                {
                                    // Check the point's intensity against .BaselineNoiseLevelAbsolute
                                    if (MASICPeakFinder.clsMASICPeakFinder.ComputeSignalToNoise(msSpectrum.IonsIntensity[ionIndex], noiseThresholdIntensity) >= noiseThresholdOptions.MinimumSignalToNoiseRatio)
                                    {
                                        pointPassesFilter = true;
                                    }
                                }

                                if (pointPassesFilter)
                                {
                                    msSpectrum.IonsMZ[ionCountNew] = msSpectrum.IonsMZ[ionIndex];
                                    msSpectrum.IonsIntensity[ionCountNew] = msSpectrum.IonsIntensity[ionIndex];
                                    ionCountNew++;
                                }
                            }
                        }
                        else
                        {
                            ionCountNew = msSpectrum.IonCount;
                        }

                        break;
                    default:
                        ReportError("Unknown BaselineNoiseMode encountered in DiscardDataBelowNoiseThreshold: " +
                                    noiseThresholdOptions.BaselineNoiseMode);
                        break;
                }

                if (ionCountNew < msSpectrum.IonCount)
                {
                    msSpectrum.ShrinkArrays(ionCountNew);
                }
            }
            catch (Exception ex)
            {
                ReportError("Error discarding data below the noise threshold", ex, clsMASIC.MasicErrorCodes.UnspecifiedError);
            }
        }

        /// <summary>
        /// This method discards data from a spectrum to limit the number of data points
        /// </summary>
        /// <param name="msSpectrum"></param>
        /// <param name="mzIgnoreRangeStart"></param>
        /// <param name="mzIgnoreRangeEnd"></param>
        /// <param name="maxIonCountToRetain"></param>
        public void DiscardDataToLimitIonCount(
            clsMSSpectrum msSpectrum,
            double mzIgnoreRangeStart,
            double mzIgnoreRangeEnd,
            int maxIonCountToRetain)
        {
            // When this is true, will write a text file of the mass spectrum before and after it is filtered
            // Enable this for debugging
            var writeDebugData = false;
            StreamWriter writer = null;

            try
            {
                int ionCountNew;
                if (msSpectrum.IonCount > maxIonCountToRetain)
                {
                    var filterDataArray = new clsFilterDataArrayMaxCount
                    {
                        MaximumDataCountToKeep = maxIonCountToRetain
                    };

                    // ReSharper disable ConditionIsAlwaysTrueOrFalse
                    // ReSharper disable once RedundantAssignment
                    writeDebugData = false;
                    if (writeDebugData)
                    {
                        writer = new StreamWriter(new FileStream(Path.Combine(mOptions.OutputDirectoryPath, "DataDump_" + msSpectrum.ScanNumber + "_BeforeFilter.txt"), FileMode.Create, FileAccess.Write, FileShare.Read));
                        writer.WriteLine("{0}\t{1}", "m/z", "Intensity");
                    }

                    // Store the intensity values in filterDataArray
                    for (var ionIndex = 0; ionIndex < msSpectrum.IonCount; ionIndex++)
                    {
                        filterDataArray.AddDataPoint(msSpectrum.IonsIntensity[ionIndex], ionIndex);
                        if (writeDebugData)
                        {
                            writer.WriteLine("{0:F3}\t{1:F0}", msSpectrum.IonsMZ[ionIndex], msSpectrum.IonsIntensity[ionIndex]);
                        }
                    }

                    if (writeDebugData)
                    {
                        writer.Close();
                    }
                    // ReSharper restore ConditionIsAlwaysTrueOrFalse

                    // Call .FilterData, which will determine which data points to keep
                    filterDataArray.FilterData();

                    ionCountNew = 0;
                    for (var ionIndex = 0; ionIndex < msSpectrum.IonCount; ionIndex++)
                    {
                        // Always keep points in the m/z ignore range
                        // If CheckPointInMZIgnoreRange returns true, set pointPassesFilter to true
                        var pointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ[ionIndex], mzIgnoreRangeStart, mzIgnoreRangeEnd);

                        if (!pointPassesFilter)
                        {
                            // See if the point's intensity is negative
                            if (filterDataArray.GetAbundanceByIndex(ionIndex) >= 0)
                            {
                                pointPassesFilter = true;
                            }
                        }

                        if (pointPassesFilter)
                        {
                            msSpectrum.IonsMZ[ionCountNew] = msSpectrum.IonsMZ[ionIndex];
                            msSpectrum.IonsIntensity[ionCountNew] = msSpectrum.IonsIntensity[ionIndex];
                            ionCountNew++;
                        }
                    }
                }
                else
                {
                    ionCountNew = msSpectrum.IonCount;
                }

                if (ionCountNew < msSpectrum.IonCount)
                {
                    msSpectrum.ShrinkArrays(ionCountNew);
                }

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (writeDebugData)
                {
                    using var postFilterWriter = new StreamWriter(new FileStream(Path.Combine(mOptions.OutputDirectoryPath, "DataDump_" + msSpectrum.ScanNumber + "_PostFilter.txt"), FileMode.Create, FileAccess.Write, FileShare.Read));

                    postFilterWriter.WriteLine("{0}\t{1}", "m/z", "Intensity");

                    // Store the intensity values in filterDataArray
                    for (var ionIndex = 0; ionIndex < msSpectrum.IonCount; ionIndex++)
                    {
                        postFilterWriter.WriteLine("{0:F3}\t{1:F0}", msSpectrum.IonsMZ[ionIndex], msSpectrum.IonsIntensity[ionIndex]);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError("Error limiting the number of data points to " + maxIonCountToRetain, ex, clsMASIC.MasicErrorCodes.UnspecifiedError);
            }
        }

        /// <summary>
        /// <para>
        /// This method is called after all spectra have been read
        /// </para>
        /// <para>
        /// A warning is shown if no scans were stored
        /// </para>
        /// <para>
        /// A Warning is shown if precursor ion was not found in any of the spectra
        /// </para>
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="dataFile"></param>
        /// <returns>True if it at least one scan was stored, otherwise false</returns>
        protected bool FinalizeScanList(clsScanList scanList, FileSystemInfo dataFile)
        {
            if (scanList.MasterScanOrderCount <= 0)
            {
                // No scans found
                if (mScansOutOfRange > 0)
                {
                    ReportWarning("None of the spectra in the input file was within the specified scan number and/or scan time range: " + dataFile.FullName);
                    SetLocalErrorCode(clsMASIC.MasicErrorCodes.NoParentIonsFoundInInputFile);
                }
                else
                {
                    ReportError("No scans found in the input file: " + dataFile.FullName, clsMASIC.MasicErrorCodes.InputFileAccessError);
                    SetLocalErrorCode(clsMASIC.MasicErrorCodes.InputFileAccessError);
                }

                return false;
            }

            if (mPrecursorNotFoundCount > PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW)
            {
                var precursorMissingPct = 0.0;
                if (scanList.FragScans.Count > 0)
                {
                    precursorMissingPct = mPrecursorNotFoundCount / (double)scanList.FragScans.Count * 100;
                }

                OnWarningEvent(
                    string.Format("Could not find the precursor ion for {0:F1}% of the MS2 spectra ({1} / {2} scans). " +
                                  "These scans will have an Interference Score of 1 (to avoid accidentally filtering out low intensity results).",
                                  precursorMissingPct, mPrecursorNotFoundCount, scanList.FragScans.Count));
            }

            // Record the current memory usage
            OnUpdateMemoryUsage();
            return true;
        }

        /// <summary>
        /// Obtain the default list of file extensions to parse
        /// </summary>
        public static IList<string> GetDefaultExtensionsToParse()
        {
            var extensionsToParse = new List<string>()
            {
                THERMO_RAW_FILE_EXTENSION,
                MZ_XML_FILE_EXTENSION1,
                MZ_XML_FILE_EXTENSION2,
                MZ_DATA_FILE_EXTENSION1,
                MZ_DATA_FILE_EXTENSION2,
                AGILENT_MSMS_FILE_EXTENSION,
                TEXT_FILE_EXTENSION
            };

            return extensionsToParse;
        }

        /// <summary>
        /// Initialize the base options
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="keepRawSpectra"></param>
        /// <param name="keepMSMSSpectra"></param>
        protected void InitBaseOptions(clsScanList scanList, bool keepRawSpectra, bool keepMSMSSpectra)
        {
            mLastNonZoomSurveyScanIndex = -1;
            mScansOutOfRange = 0;

            scanList.SIMDataPresent = false;
            scanList.MRMDataPresent = false;

            mKeepRawSpectra = keepRawSpectra;
            mKeepMSMSSpectra = keepMSMSSpectra;

            mLastLogTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Append a line to the scan stats file
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="eScanType"></param>
        /// <param name="currentScan"></param>
        /// <param name="datasetID"></param>
        protected void SaveScanStatEntry(
            StreamWriter writer,
            clsScanList.ScanTypeConstants eScanType,
            clsScanInfo currentScan,
            int datasetID)
        {
            const char TAB_DELIMITER = '\t';

            var scanStatsEntry = new ScanStatsEntry()
            {
                ScanNumber = currentScan.ScanNumber
            };

            if (eScanType == clsScanList.ScanTypeConstants.SurveyScan)
            {
                scanStatsEntry.ScanType = 1;
                scanStatsEntry.ScanTypeName = string.Copy(currentScan.ScanTypeName);
            }
            else
            {
                if (currentScan.FragScanInfo.MSLevel <= 1)
                {
                    // This is a fragmentation scan, so it must have a scan type of at least 2
                    scanStatsEntry.ScanType = 2;
                }
                else
                {
                    // .MSLevel is 2 or higher, record the actual MSLevel value
                    scanStatsEntry.ScanType = currentScan.FragScanInfo.MSLevel;
                }

                scanStatsEntry.ScanTypeName = string.Copy(currentScan.ScanTypeName);
            }

            scanStatsEntry.ScanFilterText = currentScan.ScanHeaderText;

            scanStatsEntry.ElutionTime = currentScan.ScanTime.ToString("0.0000");
            scanStatsEntry.TotalIonIntensity = StringUtilities.ValueToString(currentScan.TotalIonIntensity, 5);
            scanStatsEntry.BasePeakIntensity = StringUtilities.ValueToString(currentScan.BasePeakIonIntensity, 5);
            scanStatsEntry.BasePeakMZ = StringUtilities.DblToString(currentScan.BasePeakIonMZ, 4);

            // Base peak signal to noise ratio
            scanStatsEntry.BasePeakSignalToNoiseRatio = StringUtilities.ValueToString(MASICPeakFinder.clsMASICPeakFinder.ComputeSignalToNoise(currentScan.BasePeakIonIntensity, currentScan.BaselineNoiseStats.NoiseLevel), 4);

            scanStatsEntry.IonCount = currentScan.IonCount;
            scanStatsEntry.IonCountRaw = currentScan.IonCountRaw;

            mScanTracking.ScanStats.Add(scanStatsEntry);

            var dataColumns = new List<string>()
            {
                datasetID.ToString(),                       // Dataset ID
                scanStatsEntry.ScanNumber.ToString(),       // Scan number
                scanStatsEntry.ElutionTime,                 // Scan time (minutes)
                scanStatsEntry.ScanType.ToString(),         // Scan type (1 for MS, 2 for MS2, etc.)
                scanStatsEntry.TotalIonIntensity,           // Total ion intensity
                scanStatsEntry.BasePeakIntensity,           // Base peak ion intensity
                scanStatsEntry.BasePeakMZ,                  // Base peak ion m/z
                scanStatsEntry.BasePeakSignalToNoiseRatio,  // Base peak signal to noise ratio
                scanStatsEntry.IonCount.ToString(),         // Number of peaks (aka ions) in the spectrum
                scanStatsEntry.IonCountRaw.ToString(),      // Number of peaks (aka ions) in the spectrum prior to any filtering
                scanStatsEntry.ScanTypeName                 // Scan type name
            };

            writer.WriteLine(string.Join(TAB_DELIMITER.ToString(), dataColumns));
        }

        /// <summary>
        /// Update the cached precursor scan
        /// </summary>
        /// <param name="precursorScanNumber"></param>
        /// <param name="centroidedIonsMz"></param>
        /// <param name="centroidedIonsIntensity"></param>
        protected void UpdateCachedPrecursorScan(
            int precursorScanNumber,
            double[] centroidedIonsMz,
            double[] centroidedIonsIntensity)
        {
            UpdateCachedPrecursorScan(precursorScanNumber, centroidedIonsMz.ToList(), centroidedIonsIntensity.ToList());
        }

        /// <summary>
        /// Update the cached precursor scan
        /// </summary>
        /// <param name="precursorScanNumber"></param>
        /// <param name="centroidedIonsMz"></param>
        /// <param name="centroidedIonsIntensity"></param>
        protected void UpdateCachedPrecursorScan(
            int precursorScanNumber,
            List<double> centroidedIonsMz,
            List<double> centroidedIonsIntensity)
        {
            mCachedPrecursorIons.Clear();
            mCachedPrecursorIons.Capacity = Math.Max(mCachedPrecursorIons.Capacity, centroidedIonsMz.Count);

            var ionCount = centroidedIonsMz.Count;
            for (var index = 0; index < ionCount; index++)
            {
                var newPeak = new InterDetect.Peak()
                {
                    Mz = centroidedIonsMz[index],
                    Abundance = centroidedIonsIntensity[index]
                };

                mCachedPrecursorIons.Add(newPeak);
            }

            mCachedPrecursorScan = precursorScanNumber;
        }

        /// <summary>
        /// Update dataset file stats
        /// </summary>
        /// <param name="dataFileInfo"></param>
        /// <param name="datasetID"></param>
        /// <returns>True if success, false if an error</returns>
        protected bool UpdateDatasetFileStats(
            FileInfo dataFileInfo,
            int datasetID)
        {
            try
            {
                if (!dataFileInfo.Exists)
                    return false;

                // Record the file size and Dataset ID
                mDatasetFileInfo.FileSystemCreationTime = dataFileInfo.CreationTime;
                mDatasetFileInfo.FileSystemModificationTime = dataFileInfo.LastWriteTime;

                mDatasetFileInfo.AcqTimeStart = mDatasetFileInfo.FileSystemModificationTime;
                mDatasetFileInfo.AcqTimeEnd = mDatasetFileInfo.FileSystemModificationTime;

                mDatasetFileInfo.DatasetID = datasetID;
                mDatasetFileInfo.DatasetName = Path.GetFileNameWithoutExtension(dataFileInfo.Name);
                mDatasetFileInfo.FileExtension = dataFileInfo.Extension;
                mDatasetFileInfo.FileSizeBytes = dataFileInfo.Length;

                mDatasetFileInfo.ScanCount = 0;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Show a warning that the MS2 isolation width could not be determined
        /// </summary>
        /// <param name="scanNumber"></param>
        /// <param name="warningMessage"></param>
        protected void WarnIsolationWidthNotFound(int scanNumber, string warningMessage)
        {
            mIsolationWidthNotFoundCount++;

            if (mIsolationWidthNotFoundCount <= ISOLATION_WIDTH_NOT_FOUND_WARNINGS_TO_SHOW)
            {
                ReportWarning(warningMessage + "; cannot compute interference for scan " + scanNumber);
            }
            else if (mIsolationWidthNotFoundCount % 5000 == 0)
            {
                ReportWarning("Could not determine the MS2 isolation width for " + mIsolationWidthNotFoundCount + " scans");
            }
        }

        private void InterferenceWarningEventHandler(string message)
        {
            if (message.StartsWith("Did not find the precursor for"))
            {
                // The precursor ion was not found in the centroided MS1 spectrum; this happens sometimes
                mPrecursorNotFoundCount++;
                if (mPrecursorNotFoundCount <= PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW || mPrecursorNotFoundCount > mNextPrecursorNotFoundCountThreshold)
                {
                    OnWarningEvent(message);

                    if (mNextPrecursorNotFoundCountThreshold <= 0)
                    {
                        mNextPrecursorNotFoundCountThreshold = PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW * 2;
                    }
                    else if (mPrecursorNotFoundCount > PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW)
                    {
                        mNextPrecursorNotFoundCountThreshold *= 2;
                    }
                }
            }
            else
            {
                OnWarningEvent(message);
            }
        }
    }
}
