using System;
using System.Collections.Generic;
using System.IO;
using MASIC.DatasetStats;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using PRISM;

namespace MASIC.DataInput
{
    public abstract class clsDataImport : clsMasicEventNotifier
    {

        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        public const string THERMO_RAW_FILE_EXTENSION = ".RAW";
        public const string MZ_ML_FILE_EXTENSION = ".MZML";
        public const string MZ_XML_FILE_EXTENSION1 = ".MZXML";
        public const string MZ_XML_FILE_EXTENSION2 = "MZXML.XML";
        public const string MZ_DATA_FILE_EXTENSION1 = ".MZDATA";
        public const string MZ_DATA_FILE_EXTENSION2 = "MZDATA.XML";
        public const string AGILENT_MSMS_FILE_EXTENSION = ".MGF";    // Agilent files must have been exported to a .MGF and .CDF file pair prior to using MASIC
        public const string AGILENT_MS_FILE_EXTENSION = ".CDF";
        private const int ISOLATION_WIDTH_NOT_FOUND_WARNINGS_TO_SHOW = 5;
        protected const int PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW = 5;

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        protected readonly clsMASICOptions mOptions;
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

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        public DatasetFileInfo DatasetFileInfo
        {
            get
            {
                return mDatasetFileInfo;
            }
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        /// <summary>
        /// This event is used to signify to the calling class that it should update the status of the available memory usage
        /// </summary>
        public event UpdateMemoryUsageEventEventHandler UpdateMemoryUsageEvent;

        public delegate void UpdateMemoryUsageEventEventHandler();

        protected void OnUpdateMemoryUsage()
        {
            UpdateMemoryUsageEvent?.Invoke();
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="masicOptions"></param>
        /// <param name="peakFinder"></param>
        /// <param name="parentIonProcessor"></param>
        public clsDataImport(clsMASICOptions masicOptions, MASICPeakFinder.clsMASICPeakFinder peakFinder, clsParentIonProcessing parentIonProcessor, clsScanTracking scanTracking)
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
        protected double ComputePrecursorInterference(int fragScanNumber, int precursorScanNumber, double parentIonMz, double isolationWidth, int chargeState)
        {
            var precursorInfo = new InterDetect.PrecursorIntense(parentIonMz, isolationWidth, chargeState)
            {
                PrecursorScanNumber = precursorScanNumber,
                ScanNumber = fragScanNumber
            };
            mInterferenceCalculator.Interference(precursorInfo, mCachedPrecursorIons);
            return precursorInfo.Interference;
        }

        public void DiscardDataBelowNoiseThreshold(clsMSSpectrum msSpectrum, double noiseThresholdIntensity, double mzIgnoreRangeStart, double mzIgnoreRangeEnd, MASICPeakFinder.clsBaselineNoiseOptions noiseThresholdOptions)
        {
            var ionCountNew = default(int);
            int ionIndex;
            bool pointPassesFilter;
            try
            {
                var switchExpr = noiseThresholdOptions.BaselineNoiseMode;
                switch (switchExpr)
                {
                    case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold:
                        {
                            if (noiseThresholdOptions.BaselineNoiseLevelAbsolute > 0)
                            {
                                ionCountNew = 0;
                                var loopTo = msSpectrum.IonCount - 1;
                                for (ionIndex = 0; ionIndex <= loopTo; ionIndex++)
                                {
                                    pointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ[ionIndex], mzIgnoreRangeStart, mzIgnoreRangeEnd);
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
                        }

                    case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByAbundance:
                    case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByCount:
                    case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance:
                        {
                            if (noiseThresholdOptions.MinimumSignalToNoiseRatio > 0)
                            {
                                ionCountNew = 0;
                                var loopTo1 = msSpectrum.IonCount - 1;
                                for (ionIndex = 0; ionIndex <= loopTo1; ionIndex++)
                                {
                                    pointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ[ionIndex], mzIgnoreRangeStart, mzIgnoreRangeEnd);
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
                        }

                    default:
                        {
                            ReportError("Unknown BaselineNoiseMode encountered in DiscardDataBelowNoiseThreshold: " + noiseThresholdOptions.BaselineNoiseMode.ToString());
                            break;
                        }
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

        public void DiscardDataToLimitIonCount(clsMSSpectrum msSpectrum, double mzIgnoreRangeStart, double mzIgnoreRangeEnd, int maxIonCountToRetain)
        {
            int ionCountNew;
            int ionIndex;
            bool pointPassesFilter;

            // When this is true, then will write a text file of the mass spectrum before and after it is filtered
            // Used for debugging
            var writeDebugData = default(bool);
            StreamWriter writer = null;
            try
            {
                if (msSpectrum.IonCount > maxIonCountToRetain)
                {
                    var objFilterDataArray = new clsFilterDataArrayMaxCount()
                    {
                        MaximumDataCountToLoad = maxIonCountToRetain,
                        TotalIntensityPercentageFilterEnabled = false
                    };
                    writeDebugData = false;
                    if (writeDebugData)
                    {
                        writer = new StreamWriter(new FileStream(Path.Combine(mOptions.OutputDirectoryPath, "DataDump_" + msSpectrum.ScanNumber.ToString() + "_BeforeFilter.txt"), FileMode.Create, FileAccess.Write, FileShare.Read));
                        writer.WriteLine("m/z" + ControlChars.Tab + "Intensity");
                    }

                    // Store the intensity values in objFilterDataArray
                    var loopTo = msSpectrum.IonCount - 1;
                    for (ionIndex = 0; ionIndex <= loopTo; ionIndex++)
                    {
                        objFilterDataArray.AddDataPoint(msSpectrum.IonsIntensity[ionIndex], ionIndex);
                        if (writeDebugData)
                        {
                            writer.WriteLine(msSpectrum.IonsMZ[ionIndex].ToString() + ControlChars.Tab + msSpectrum.IonsIntensity[ionIndex]);
                        }
                    }

                    if (writeDebugData)
                    {
                        writer.Close();
                    }


                    // Call .FilterData, which will determine which data points to keep
                    objFilterDataArray.FilterData();
                    ionCountNew = 0;
                    var loopTo1 = msSpectrum.IonCount - 1;
                    for (ionIndex = 0; ionIndex <= loopTo1; ionIndex++)
                    {
                        pointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ[ionIndex], mzIgnoreRangeStart, mzIgnoreRangeEnd);
                        if (!pointPassesFilter)
                        {
                            // See if the point's intensity is negative
                            if (objFilterDataArray.GetAbundanceByIndex(ionIndex) >= 0)
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

                if (writeDebugData)
                {
                    using (var postFilterWriter = new StreamWriter(new FileStream(Path.Combine(mOptions.OutputDirectoryPath, "DataDump_" + msSpectrum.ScanNumber.ToString() + "_PostFilter.txt"), FileMode.Create, FileAccess.Write, FileShare.Read)))
                    {
                        postFilterWriter.WriteLine("m/z" + ControlChars.Tab + "Intensity");

                        // Store the intensity values in objFilterDataArray
                        var loopTo2 = msSpectrum.IonCount - 1;
                        for (ionIndex = 0; ionIndex <= loopTo2; ionIndex++)
                            postFilterWriter.WriteLine(msSpectrum.IonsMZ[ionIndex].ToString() + ControlChars.Tab + msSpectrum.IonsIntensity[ionIndex]);
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
                    ReportError("No scans found in the input file: " + dataFile.FullName);
                    SetLocalErrorCode(clsMASIC.eMasicErrorCodes.InputFileAccessError);
                }

                return false;
            }

            if (mPrecursorNotFoundCount > PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW)
            {
                var precursorMissingPct = default(double);
                if (scanList.FragScans.Count > 0)
                {
                    precursorMissingPct = mPrecursorNotFoundCount / Conversions.ToDouble(scanList.FragScans.Count) * 100;
                }

                OnWarningEvent(string.Format("Could not find the precursor ion for {0:F1}% of the MS2 spectra ({1} / {2} scans). " + "These scans will have an Interference Score of 1 (to avoid accidentally filtering out low intensity results).", precursorMissingPct, mPrecursorNotFoundCount, scanList.FragScans.Count));
            }

            // Record the current memory usage
            OnUpdateMemoryUsage();
            return true;
        }

        public static IList<string> GetDefaultExtensionsToParse()
        {
            var extensionsToParse = new List<string>() { THERMO_RAW_FILE_EXTENSION, MZ_XML_FILE_EXTENSION1, MZ_XML_FILE_EXTENSION2, MZ_DATA_FILE_EXTENSION1, MZ_DATA_FILE_EXTENSION2, AGILENT_MSMS_FILE_EXTENSION };
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

        protected void SaveScanStatEntry(StreamWriter writer, clsScanList.eScanTypeConstants eScanType, clsScanInfo currentScan, int datasetID)
        {
            const char cColDelimiter = ControlChars.Tab;
            var scanStatsEntry = new ScanStatsEntry() { ScanNumber = currentScan.ScanNumber };
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
            var dataColumns = new List<string>() { datasetID.ToString(), scanStatsEntry.ScanNumber.ToString(), scanStatsEntry.ElutionTime, scanStatsEntry.ScanType.ToString(), scanStatsEntry.TotalIonIntensity, scanStatsEntry.BasePeakIntensity, scanStatsEntry.BasePeakMZ, scanStatsEntry.BasePeakSignalToNoiseRatio, scanStatsEntry.IonCount.ToString(), scanStatsEntry.IonCountRaw.ToString(), scanStatsEntry.ScanTypeName };                       // Dataset ID
                                                                                                                                                                                                                                                                                                                                                                                                                                                         // Scan number
                                                                                                                                                                                                                                                                                                                                                                                                                                                         // Scan time (minutes)
                                                                                                                                                                                                                                                                                                                                                                                                                                                         // Scan type (1 for MS, 2 for MS2, etc.)
                                                                                                                                                                                                                                                                                                                                                                                                                                                         // Total ion intensity
                                                                                                                                                                                                                                                                                                                                                                                                                                                         // Base peak ion intensity
                                                                                                                                                                                                                                                                                                                                                                                                                                                         // Base peak ion m/z
                                                                                                                                                                                                                                                                                                                                                                                                                                                         // Base peak signal to noise ratio
                                                                                                                                                                                                                                                                                                                                                                                                                                                         // Number of peaks (aka ions) in the spectrum
                                                                                                                                                                                                                                                                                                                                                                                                                                                         // Number of peaks (aka ions) in the spectrum prior to any filtering
                                                                                                                                                                                                                                                                                                                                                                                                                                                         // Scan type name
            writer.WriteLine(string.Join(Conversions.ToString(cColDelimiter), dataColumns));
        }

        protected void UpdateCachedPrecursorScan(int precursorScanNumber, double[] centroidedIonsMz, double[] centroidedIonsIntensity, int ionCount)
        {
            var mzList = new List<double>();
            var intensityList = new List<double>();
            for (int i = 0, loopTo = ionCount - 1; i <= loopTo; i++)
            {
                mzList.Add(centroidedIonsMz[i]);
                intensityList.Add(centroidedIonsIntensity[i]);
            }

            UpdateCachedPrecursorScan(precursorScanNumber, mzList, intensityList);
        }

        protected void UpdateCachedPrecursorScan(int precursorScanNumber, List<double> centroidedIonsMz, List<double> centroidedIonsIntensity)
        {
            mCachedPrecursorIons.Clear();
            int ionCount = centroidedIonsMz.Count;
            for (int index = 0, loopTo = ionCount - 1; index <= loopTo; index++)
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

        protected bool UpdateDatasetFileStats(FileInfo dataFileInfo, int datasetID)
        {
            try
            {
                if (!dataFileInfo.Exists)
                    return false;

                // Record the file size and Dataset ID
                {
                    var withBlock = mDatasetFileInfo;
                    withBlock.FileSystemCreationTime = dataFileInfo.CreationTime;
                    withBlock.FileSystemModificationTime = dataFileInfo.LastWriteTime;
                    withBlock.AcqTimeStart = withBlock.FileSystemModificationTime;
                    withBlock.AcqTimeEnd = withBlock.FileSystemModificationTime;
                    withBlock.DatasetID = datasetID;
                    withBlock.DatasetName = Path.GetFileNameWithoutExtension(dataFileInfo.Name);
                    withBlock.FileExtension = dataFileInfo.Extension;
                    withBlock.FileSizeBytes = dataFileInfo.Length;
                    withBlock.ScanCount = 0;
                }
            }
            catch (Exception ex)
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