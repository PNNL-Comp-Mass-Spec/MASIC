using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MASIC.DatasetStats;
using MASIC.Options;
using PRISM;

namespace MASIC.DataInput
{
    public abstract class clsDataImport : clsMasicEventNotifier
    {
        #region "Constants and Enums"

        /// <summary>
        /// Thermo .raw file extension
        /// </summary>
        /// <remarks>Needs to be all caps because of the switch statement in clsMASIC.LoadData</remarks>
        public const string THERMO_RAW_FILE_EXTENSION = ".RAW";

        public const string MZ_ML_FILE_EXTENSION = ".MZML";

        public const string MZ_XML_FILE_EXTENSION1 = ".MZXML";
        public const string MZ_XML_FILE_EXTENSION2 = "MZXML.XML";

        public const string MZ_DATA_FILE_EXTENSION1 = ".MZDATA";
        public const string MZ_DATA_FILE_EXTENSION2 = "MZDATA.XML";

        public const string AGILENT_MSMS_FILE_EXTENSION = ".MGF";    // Agilent files must have been exported to a .MGF and .CDF file pair prior to using MASIC
        public const string AGILENT_MS_FILE_EXTENSION = ".CDF";

        public const string TEXT_FILE_EXTENSION = ".TXT";

        private const int ISOLATION_WIDTH_NOT_FOUND_WARNINGS_TO_SHOW = 5;

        protected const int PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW = 5;

        #endregion

        #region "Classwide Variables"

        protected readonly MASICOptions mOptions;

        protected readonly clsParentIonProcessing mParentIonProcessor;

        protected readonly MASICPeakFinder.clsMASICPeakFinder mPeakFinder;

        protected readonly clsScanTracking mScanTracking;

        protected DatasetFileInfo mDatasetFileInfo;

        protected bool mKeepRawSpectra;
        protected bool mKeepMSMSSpectra;

        protected int mLastSurveyScanIndexInMasterSeqOrder;
        protected int mLastNonZoomSurveyScanIndex;
        protected DateTime mLastLogTime;

        private readonly InterDetect.InterferenceCalculator mInterferenceCalculator;

        private readonly List<InterDetect.Peak> mCachedPrecursorIons;
        protected int mCachedPrecursorScan;

        private int mIsolationWidthNotFoundCount;
        private int mPrecursorNotFoundCount;
        private int mNextPrecursorNotFoundCountThreshold;

        protected int mScansOutOfRange;

        #endregion

        #region "Properties"

        public DatasetFileInfo DatasetFileInfo => mDatasetFileInfo;

        #endregion

        #region "Events"

        /// <summary>
        /// This event is used to signify to the calling class that it should update the status of the available memory usage
        /// </summary>
        public event UpdateMemoryUsageEventEventHandler UpdateMemoryUsageEvent;

        public delegate void UpdateMemoryUsageEventEventHandler();

        protected void OnUpdateMemoryUsage()
        {
            UpdateMemoryUsageEvent?.Invoke();
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="masicOptions"></param>
        /// <param name="peakFinder"></param>
        /// <param name="parentIonProcessor"></param>
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
                    case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold:
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
                                    ionCountNew += 1;
                                }
                            }
                        }
                        else
                        {
                            ionCountNew = msSpectrum.IonCount;
                        }

                        break;
                    case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByAbundance:
                    case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByCount:
                    case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance:
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
                                    ionCountNew += 1;
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
                                    noiseThresholdOptions.BaselineNoiseMode.ToString());
                        break;
                }

                if (ionCountNew < msSpectrum.IonCount)
                {
                    msSpectrum.ShrinkArrays(ionCountNew);
                }
            }
            catch (Exception ex)
            {
                ReportError("Error discarding data below the noise threshold", ex, clsMASIC.eMasicErrorCodes.UnspecifiedError);
            }
        }

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
                        MaximumDataCountToKeep = maxIonCountToRetain,
                        TotalIntensityPercentageFilterEnabled = false
                    };

                    // ReSharper disable ConditionIsAlwaysTrueOrFalse
                    // ReSharper disable once RedundantAssignment
                    writeDebugData = false;
                    if (writeDebugData)
                    {
                        writer = new StreamWriter(new FileStream(Path.Combine(mOptions.OutputDirectoryPath, "DataDump_" + msSpectrum.ScanNumber.ToString() + "_BeforeFilter.txt"), FileMode.Create, FileAccess.Write, FileShare.Read));
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
                            ionCountNew += 1;
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
                    using (var postFilterWriter = new StreamWriter(new FileStream(Path.Combine(mOptions.OutputDirectoryPath, "DataDump_" + msSpectrum.ScanNumber.ToString() + "_PostFilter.txt"), FileMode.Create, FileAccess.Write, FileShare.Read)))
                    {
                        postFilterWriter.WriteLine("{0}\t{1}", "m/z", "Intensity");

                        // Store the intensity values in filterDataArray
                        for (var ionIndex = 0; ionIndex < msSpectrum.IonCount; ionIndex++)
                        {
                            postFilterWriter.WriteLine("{0:F3}\t{1:F0}", msSpectrum.IonsMZ[ionIndex], msSpectrum.IonsIntensity[ionIndex]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError("Error limiting the number of data points to " + maxIonCountToRetain, ex, clsMASIC.eMasicErrorCodes.UnspecifiedError);
            }
        }

        protected bool FinalizeScanList(clsScanList scanList, FileSystemInfo dataFile)
        {
            if (scanList.MasterScanOrderCount <= 0)
            {
                // No scans found
                if (mScansOutOfRange > 0)
                {
                    ReportWarning("None of the spectra in the input file was within the specified scan number and/or scan time range: " + dataFile.FullName);
                    SetLocalErrorCode(clsMASIC.eMasicErrorCodes.NoParentIonsFoundInInputFile);
                }
                else
                {
                    ReportError("No scans found in the input file: " + dataFile.FullName, clsMASIC.eMasicErrorCodes.InputFileAccessError);
                    SetLocalErrorCode(clsMASIC.eMasicErrorCodes.InputFileAccessError);
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

        protected void SaveScanStatEntry(
            StreamWriter writer,
            clsScanList.eScanTypeConstants eScanType,
            clsScanInfo currentScan,
            int datasetID)
        {
            const char TAB_DELIMITER = '\t';

            var scanStatsEntry = new ScanStatsEntry()
            {
                ScanNumber = currentScan.ScanNumber
            };

            if (eScanType == clsScanList.eScanTypeConstants.SurveyScan)
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

        protected void UpdateCachedPrecursorScan(
            int precursorScanNumber,
            double[] centroidedIonsMz,
            double[] centroidedIonsIntensity)
        {
            UpdateCachedPrecursorScan(precursorScanNumber, centroidedIonsMz.ToList(), centroidedIonsIntensity.ToList());
        }

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

        protected void WarnIsolationWidthNotFound(int scanNumber, string warningMessage)
        {
            mIsolationWidthNotFoundCount += 1;

            if (mIsolationWidthNotFoundCount <= ISOLATION_WIDTH_NOT_FOUND_WARNINGS_TO_SHOW)
            {
                ReportWarning(warningMessage + "; " + "cannot compute interference for scan " + scanNumber);
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
                mPrecursorNotFoundCount += 1;
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
