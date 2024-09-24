using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MASIC.Data;
using MASIC.DataOutput;
using MASIC.Options;
using MASICPeakFinder;
using PRISM;
using ThermoRawFileReader;

namespace MASIC.DataInput
{
    /// <summary>
    /// Class for reading spectra from a Thermo .raw file
    /// </summary>
    public class DataImportThermoRaw : DataImport
    {
        // Ignore Spelling: MASIC

        private const string SCAN_EVENT_CHARGE_STATE = "Charge State";
        private const string SCAN_EVENT_MONOISOTOPIC_MZ = "Monoisotopic M/Z";
        private const string SCAN_EVENT_MS2_ISOLATION_WIDTH = "MS2 Isolation Width";

        private int mBpiUpdateCount;

        private readonly Dictionary<string, int> mSIMScanMapping = new();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="masicOptions"></param>
        /// <param name="peakFinder"></param>
        /// <param name="parentIonProcessor"></param>
        /// <param name="scanTracking"></param>
        public DataImportThermoRaw(
            MASICOptions masicOptions,
            clsMASICPeakFinder peakFinder,
            ParentIonProcessing parentIonProcessor,
            ScanTracking scanTracking)
            : base(masicOptions, peakFinder, parentIonProcessor, scanTracking)
        {
        }

        private double ComputeInterference(
            XRawFileIO rawFileReader,
            clsScanInfo scanInfo,
            int precursorScanNumber)
        {
            if (precursorScanNumber != mCachedPrecursorScan)
            {
                rawFileReader.GetScanData(precursorScanNumber, out var centroidedIonsMz, out var centroidedIonsIntensity, 0, true);

                UpdateCachedPrecursorScan(precursorScanNumber, centroidedIonsMz, centroidedIonsIntensity);
            }

            var chargeState = 0;

            scanInfo.TryGetScanEvent(SCAN_EVENT_CHARGE_STATE, out var chargeStateText, true);

            if (!string.IsNullOrWhiteSpace(chargeStateText))
            {
                if (!int.TryParse(chargeStateText, out chargeState))
                {
                    chargeState = 0;
                }
            }

            if (!scanInfo.TryGetScanEvent(SCAN_EVENT_MS2_ISOLATION_WIDTH, out var isolationWidthText, true))
            {
                if (scanInfo.MRMScanType == MRMScanTypeConstants.SRM)
                {
                    // SRM data files don't have the MS2 Isolation Width event
                    return 0;
                }

                WarnIsolationWidthNotFound(
                    scanInfo.ScanNumber,
                    "Could not determine the MS2 isolation width (" + SCAN_EVENT_MS2_ISOLATION_WIDTH + ")");

                return 0;
            }

            if (!double.TryParse(isolationWidthText, out var isolationWidth))
            {
                ReportWarning("MS2 isolation width (" + SCAN_EVENT_MS2_ISOLATION_WIDTH + ") was non-numeric (" + isolationWidthText + "); " +
                              "cannot compute interference for scan " + scanInfo.ScanNumber);
                return 0;
            }

            double parentIonMz;

            if (Math.Abs(scanInfo.ParentIonMZ) > 0)
            {
                parentIonMz = scanInfo.ParentIonMZ;
            }
            else
            {
                // ThermoRawFileReader could not determine the parent ion m/z value (this is highly unlikely)
                // Use scan event "Monoisotopic M/Z" instead
                if (!scanInfo.TryGetScanEvent(SCAN_EVENT_MONOISOTOPIC_MZ, out var monoMzText, true))
                {
                    ReportWarning("Could not determine the parent ion m/z value (" + SCAN_EVENT_MONOISOTOPIC_MZ + "); " +
                                  "cannot compute interference for scan " + scanInfo.ScanNumber);
                    return 0;
                }

                if (!double.TryParse(monoMzText, out var mz))
                {
                    OnWarningEvent("Skipping scan {0} since scan event {1} was not a number: {2}", scanInfo.ScanNumber, SCAN_EVENT_MONOISOTOPIC_MZ, monoMzText);
                    return 0;
                }

                parentIonMz = mz;
            }

            if (Math.Abs(parentIonMz) < float.Epsilon)
            {
                ReportWarning("Parent ion m/z is 0; cannot compute interference for scan " + scanInfo.ScanNumber);
                return 0;
            }

            return ComputePrecursorInterference(
                scanInfo.ScanNumber,
                precursorScanNumber, parentIonMz, isolationWidth, chargeState);
        }

        /// <summary>
        /// Read scan data and ions from a Thermo .raw file
        /// </summary>
        /// <remarks>Assumes filePath exists</remarks>
        /// <param name="filePath"></param>
        /// <param name="scanList"></param>
        /// <param name="spectraCache"></param>
        /// <param name="dataOutputHandler"></param>
        /// <param name="keepRawSpectra"></param>
        /// <param name="keepMSMSSpectra"></param>
        /// <returns>True if Success, False if failure</returns>
        public bool ExtractScanInfoFromThermoDataFile(
            string filePath,
            ScanList scanList,
            SpectraCache spectraCache,
            DataOutput.DataOutput dataOutputHandler,
            bool keepRawSpectra,
            bool keepMSMSSpectra)
        {
            // Use XrawFileIO to read the .Raw files (it uses ThermoFisher.CommonCore)

            var loadMSMethodInfo = mOptions.WriteMSMethodFile && !SystemInfo.IsLinux;

            if (SystemInfo.IsLinux && mOptions.WriteMSMethodFile)
            {
                ReportWarning(
                    "Ignoring WriteMSMethodFile=True in the parameter file since running on Linux; " +
                    "set WriteMSMethodFile to false to suppress this warning");
            }

            var readerOptions = new ThermoReaderOptions
            {
                LoadMSMethodInfo = loadMSMethodInfo,
                LoadMSTuneInfo = mOptions.WriteMSTuneFile
            };

            var rawFileReader = new XRawFileIO(readerOptions)
            {
                ScanInfoCacheMaxSize = 0    // Don't cache scanInfo objects
            };

            RegisterEvents(rawFileReader);

            mBpiUpdateCount = 0;

            // Assume success for now
            var success = true;

            try
            {
                Console.Write("Reading Thermo .raw file ");
                ReportMessage("Reading Thermo .raw file");

                UpdateProgress(0, "Opening data file:" + Environment.NewLine + Path.GetFileName(filePath));

                // Obtain the full path to the file
                var rawFileInfo = new FileInfo(filePath);
                var inputFileFullPath = rawFileInfo.FullName;

                // Open a handle to the data file
                if (!rawFileReader.OpenRawFile(inputFileFullPath))
                {
                    ReportError("Error opening input data file: " + inputFileFullPath + " (rawFileReader.OpenRawFile returned False)");
                    SetLocalErrorCode(clsMASIC.MasicErrorCodes.InputFileAccessError);
                    return false;
                }

                var datasetID = mOptions.SICOptions.DatasetID;

                success = UpdateDatasetFileStats(rawFileInfo, datasetID, rawFileReader);

                var metadataWriter = new ThermoMetadataWriter();
                RegisterEvents(metadataWriter);

                if (mOptions.WriteMSMethodFile)
                {
                    metadataWriter.SaveMSMethodFile(rawFileReader, dataOutputHandler);
                }

                if (mOptions.WriteMSTuneFile)
                {
                    metadataWriter.SaveMSTuneFile(rawFileReader, dataOutputHandler);
                }

                var scanCount = rawFileReader.GetNumScans();

                if (scanCount <= 0)
                {
                    // No scans found
                    ReportError("No scans found in the input file: " + filePath);
                    SetLocalErrorCode(clsMASIC.MasicErrorCodes.InputFileAccessError);
                    return false;
                }

                var scanStart = rawFileReader.ScanStart;
                var scanEnd = rawFileReader.ScanEnd;

                InitOptions(scanList, keepRawSpectra, keepMSMSSpectra);

                UpdateProgress(string.Format("Reading Thermo data ({0:N0} scans){1}", scanCount, Environment.NewLine + Path.GetFileName(filePath)));
                ReportMessage(string.Format("Reading Thermo data; Total scan count: {0:N0}", scanCount));

                var scanCountToRead = Math.Max(1, scanEnd - scanStart + 1);

                var scansEst = mOptions.SICOptions.ScanRangeCount;

                if (scansEst <= 0)
                {
                    scansEst = scanCountToRead;
                }

                scanList.ReserveListCapacity(scansEst);
                mScanTracking.ReserveListCapacity(scansEst);
                spectraCache.SpectrumCount = Math.Max(spectraCache.SpectrumCount, scansEst);

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    if (!mScanTracking.CheckScanInRange(scanNumber, mOptions.SICOptions))
                    {
                        mScansOutOfRange++;
                        continue;
                    }

                    success = rawFileReader.GetScanInfo(scanNumber, out var thermoScanInfo);

                    if (!success)
                    {
                        // GetScanInfo returned false
                        ReportWarning("rawFileReader.GetScanInfo returned false for scan " + scanNumber + "; aborting read");
                        break;
                    }

                    var percentComplete = scanList.MasterScanOrderCount / (double)(scansEst) * 100;
                    var extractSuccess = ExtractScanInfoCheckRange(rawFileReader, thermoScanInfo, scanList, spectraCache, dataOutputHandler, percentComplete);

                    if (!extractSuccess)
                    {
                        break;
                    }
                }

                Console.WriteLine();

                scanList.SetListCapacityToCount();
                mScanTracking.SetListCapacityToCount();

                // Shrink the memory usage of the scanList arrays
                success = FinalizeScanList(scanList, rawFileInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                ReportError("Error in ExtractScanInfoFromThermoDataFile", ex, clsMASIC.MasicErrorCodes.InputFileDataReadError);
            }

            // Close the handle to the data file
            rawFileReader.CloseRawFile();

            return success;
        }

        private bool ExtractScanInfoCheckRange(
            XRawFileIO rawFileReader,
            clsScanInfo thermoScanInfo,
            ScanList scanList,
            SpectraCache spectraCache,
            DataOutput.DataOutput dataOutputHandler,
            double percentComplete)
        {
            bool success;

            if (mScanTracking.CheckScanInRange(thermoScanInfo.ScanNumber, thermoScanInfo.RetentionTime, mOptions.SICOptions))
            {
                success = ExtractScanInfoWork(rawFileReader, scanList, spectraCache, dataOutputHandler,
                                              mOptions.SICOptions, thermoScanInfo);
            }
            else
            {
                mScansOutOfRange++;
                success = true;
            }

            UpdateProgress((short)Math.Round(percentComplete, 0));

            UpdateCacheStats(spectraCache);

            if (mOptions.AbortProcessing)
            {
                scanList.ProcessingIncomplete = true;
                return false;
            }

            if (DateTime.UtcNow.Subtract(mLastLogTime).TotalSeconds >= 10 ||
                thermoScanInfo.ScanNumber % 500 == 0 &&
                thermoScanInfo.ScanNumber >= mOptions.SICOptions.ScanRangeStart &&
                thermoScanInfo.ScanNumber <= mOptions.SICOptions.ScanRangeEnd)
            {
                ReportMessage("Reading scan: " + thermoScanInfo.ScanNumber);
                Console.Write(".");
                mLastLogTime = DateTime.UtcNow;
            }

            return success;
        }

        private bool ExtractScanInfoWork(
            XRawFileIO rawFileReader,
            ScanList scanList,
            SpectraCache spectraCache,
            DataOutput.DataOutput dataOutputHandler,
            SICOptions sicOptions,
            clsScanInfo thermoScanInfo)
        {
            if (thermoScanInfo.ParentIonMZ > 0 && Math.Abs(mOptions.ParentIonDecoyMassDa) > 0)
            {
                thermoScanInfo.ParentIonMZ += mOptions.ParentIonDecoyMassDa;
            }

            // Determine if this was an MS/MS scan
            // If yes, determine the scan number of the survey scan
            if (thermoScanInfo.MSLevel <= 1)
            {
                // Survey Scan
                return ExtractThermoSurveyScan(
                    rawFileReader, scanList, spectraCache, dataOutputHandler,
                    sicOptions, thermoScanInfo);
            }

            // Fragmentation Scan
            return ExtractThermoFragmentationScan(rawFileReader, scanList, spectraCache, dataOutputHandler,
                sicOptions, mOptions.BinningOptions, thermoScanInfo);
        }

        private bool ExtractThermoSurveyScan(
            XRawFileIO rawFileReader,
            ScanList scanList,
            SpectraCache spectraCache,
            DataOutput.DataOutput dataOutputHandler,
            SICOptions sicOptions,
            clsScanInfo thermoScanInfo)
        {
            var includeParentMZ = thermoScanInfo.IsDIA;

            var scanInfo = new ScanInfo
            {
                ScanNumber = thermoScanInfo.ScanNumber,
                ScanTime = (float)thermoScanInfo.RetentionTime,
                ScanHeaderText = XRawFileIO.MakeGenericThermoScanFilter(thermoScanInfo.FilterText, includeParentMZ),
                ScanTypeName = XRawFileIO.GetScanTypeNameFromThermoScanFilterText(thermoScanInfo.FilterText, thermoScanInfo.IsDIA),
                BasePeakIonMZ = thermoScanInfo.BasePeakMZ,
                BasePeakIonIntensity = thermoScanInfo.BasePeakIntensity,
                TotalIonIntensity = thermoScanInfo.TotalIonCurrent,
                MinimumPositiveIntensity = 0,        // This will be determined in LoadSpectraForThermoRawFile
                ZoomScan = thermoScanInfo.ZoomScan,
                SIMScan = thermoScanInfo.SIMScan,
                MRMScanType = thermoScanInfo.MRMScanType,
                LowMass = thermoScanInfo.LowMass,
                HighMass = thermoScanInfo.HighMass,
                IsDIA = false,
                IsHighResolution = thermoScanInfo.IsHighResolution,
                FragScanInfo =
                {
                    // Survey scans typically lead to multiple parent ions; we do not record them here
                    ParentIonInfoIndex = -1
                }
            };

            if (scanInfo.MRMScanType != MRMScanTypeConstants.NotMRM)
            {
                // This is an MRM scan
                scanList.MRMDataPresent = true;
            }

            if (scanInfo.SIMScan)
            {
                scanList.SIMDataPresent = true;
                var simKey = scanInfo.LowMass + "_" + scanInfo.HighMass;

                if (mSIMScanMapping.TryGetValue(simKey, out var simIndex))
                {
                    scanInfo.SIMIndex = simIndex;
                }
                else
                {
                    scanInfo.SIMIndex = mSIMScanMapping.Count;
                    mSIMScanMapping.Add(simKey, mSIMScanMapping.Count);
                }
            }

            // Store the ScanEvent values in .ExtendedHeaderInfo
            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, thermoScanInfo.ScanEvents);

            // Store the collision mode and possibly the scan filter text
            scanInfo.FragScanInfo.CollisionMode = thermoScanInfo.CollisionMode;
            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, ExtendedStatsWriter.EXTENDED_STATS_HEADER_COLLISION_MODE, thermoScanInfo.CollisionMode);

            if (mOptions.WriteExtendedStatsIncludeScanFilterText)
            {
                StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, ExtendedStatsWriter.EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, thermoScanInfo.FilterText);
            }

            if (mOptions.WriteExtendedStatsStatusLog)
            {
                // Store the StatusLog values in .ExtendedHeaderInfo
                StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, thermoScanInfo.StatusLog, mOptions.StatusLogKeyNameFilterList);
            }

            scanList.SurveyScans.Add(scanInfo);

            if (!scanInfo.ZoomScan)
            {
                mLastNonZoomSurveyScanIndex = scanList.SurveyScans.Count - 1;
            }

            scanList.AddMasterScanEntry(ScanList.ScanTypeConstants.SurveyScan, scanList.SurveyScans.Count - 1);

            double msDataResolution;

            if (sicOptions.SICToleranceIsPPM)
            {
                // Define MSDataResolution based on the tolerance value that will be used at the lowest m/z in this spectrum, divided by sicOptions.CompressToleranceDivisorForPPM
                // However, if the lowest m/z value is < 100, use 100 m/z
                if (thermoScanInfo.LowMass < 100)
                {
                    msDataResolution = ParentIonProcessing.GetParentIonToleranceDa(sicOptions, 100) /
                        sicOptions.CompressToleranceDivisorForPPM;
                }
                else
                {
                    msDataResolution = ParentIonProcessing.GetParentIonToleranceDa(sicOptions, thermoScanInfo.LowMass) /
                        sicOptions.CompressToleranceDivisorForPPM;
                }
            }
            else
            {
                msDataResolution = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForDa;
            }

            // Note: Even if mKeepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
            var success = LoadSpectraForThermoRawFile(
                rawFileReader,
                spectraCache,
                scanInfo,
                sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                clsMASIC.DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD,
                sicOptions.CompressMSSpectraData,
                msDataResolution,
                mKeepRawSpectra);

            if (!success)
                return false;

            SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats, ScanList.ScanTypeConstants.SurveyScan, scanInfo, sicOptions.DatasetID);

            return true;
        }

        private bool ExtractThermoFragmentationScan(
            XRawFileIO rawFileReader,
            ScanList scanList,
            SpectraCache spectraCache,
            DataOutput.DataOutput dataOutputHandler,
            SICOptions sicOptions,
            BinningOptions binningOptions,
            clsScanInfo thermoScanInfo)
        {
            // Note that MinimumPositiveIntensity will be determined in LoadSpectraForThermoRawFile

            var includeParentMZ = thermoScanInfo.IsDIA;

            var scanInfo = new ScanInfo(thermoScanInfo.ParentScan, thermoScanInfo.ParentIonMZ)
            {
                ScanNumber = thermoScanInfo.ScanNumber,
                ScanTime = (float)thermoScanInfo.RetentionTime,
                ScanHeaderText = XRawFileIO.MakeGenericThermoScanFilter(thermoScanInfo.FilterText, includeParentMZ),
                ScanTypeName = XRawFileIO.GetScanTypeNameFromThermoScanFilterText(thermoScanInfo.FilterText, thermoScanInfo.IsDIA),
                BasePeakIonMZ = thermoScanInfo.BasePeakMZ,
                BasePeakIonIntensity = thermoScanInfo.BasePeakIntensity,
                TotalIonIntensity = thermoScanInfo.TotalIonCurrent,
                MinimumPositiveIntensity = 0,
                ZoomScan = thermoScanInfo.ZoomScan,
                SIMScan = thermoScanInfo.SIMScan,
                MRMScanType = thermoScanInfo.MRMScanType,
                FragScanInfo =
                {
                    // Typically .EventNumber is 1 for the parent-ion scan; 2 for 1st fragmentation scan, 3 for 2nd fragmentation scan, etc.
                    // This resets for each new parent-ion scan
                    FragScanNumber = thermoScanInfo.EventNumber - 1
                }
            };

            // ReSharper disable once GrammarMistakeInComment

            // The .EventNumber value is sometimes wrong; need to check for this
            // For example, if the dataset only has MS2 scans and no parent-ion scan, .EventNumber will be 2 for every MS2 scan
            if (scanList.FragScans.Count > 0)
            {
                var prevFragScan = scanList.FragScans[scanList.FragScans.Count - 1];

                if (prevFragScan.ScanNumber == scanInfo.ScanNumber - 1)
                {
                    if (scanInfo.FragScanInfo.FragScanNumber <= prevFragScan.FragScanInfo.FragScanNumber)
                    {
                        scanInfo.FragScanInfo.FragScanNumber = prevFragScan.FragScanInfo.FragScanNumber + 1;
                    }
                }
            }

            scanInfo.FragScanInfo.MSLevel = thermoScanInfo.MSLevel;

            if (scanInfo.MRMScanType != MRMScanTypeConstants.NotMRM)
            {
                // This is an MRM scan
                scanList.MRMDataPresent = true;

                scanInfo.MRMScanInfo = MRMProcessing.DuplicateMRMInfo(thermoScanInfo.MRMInfo, thermoScanInfo.ParentIonMZ);

                if (scanList.SurveyScans.Count == 0)
                {
                    // Need to add a "fake" survey scan that we can map this parent ion to
                    mLastNonZoomSurveyScanIndex = scanList.AddFakeSurveyScan();
                }
            }
            else
            {
                scanInfo.MRMScanInfo.MRMMassCount = 0;
            }

            scanInfo.LowMass = thermoScanInfo.LowMass;
            scanInfo.HighMass = thermoScanInfo.HighMass;
            scanInfo.IsDIA = thermoScanInfo.IsDIA;
            scanInfo.IsHighResolution = thermoScanInfo.IsHighResolution;
            scanInfo.IsolationWindowWidthMZ = thermoScanInfo.IsolationWindowWidthMZ;

            // Store the ScanEvent values in .ExtendedHeaderInfo
            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, thermoScanInfo.ScanEvents);

            // Store the collision mode and possibly the scan filter text
            scanInfo.FragScanInfo.CollisionMode = thermoScanInfo.CollisionMode;
            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, ExtendedStatsWriter.EXTENDED_STATS_HEADER_COLLISION_MODE, thermoScanInfo.CollisionMode);

            if (mOptions.WriteExtendedStatsIncludeScanFilterText)
            {
                StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, ExtendedStatsWriter.EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, thermoScanInfo.FilterText);
            }

            if (mOptions.WriteExtendedStatsStatusLog)
            {
                // Store the StatusLog values in .ExtendedHeaderInfo
                StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, thermoScanInfo.StatusLog, mOptions.StatusLogKeyNameFilterList);
            }

            scanList.FragScans.Add(scanInfo);
            var fragScanIndex = scanList.FragScans.Count - 1;

            scanList.AddMasterScanEntry(ScanList.ScanTypeConstants.FragScan, fragScanIndex);

            // Note: Even if keepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
            var msDataResolution = binningOptions.BinSize / sicOptions.CompressToleranceDivisorForDa;

            var success = LoadSpectraForThermoRawFile(
                rawFileReader,
                spectraCache,
                scanInfo,
                sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                clsMASIC.DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD,
                sicOptions.CompressMSMSSpectraData,
                msDataResolution,
                mKeepRawSpectra && mKeepMSMSSpectra);

            if (!success)
                return false;

            SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats, ScanList.ScanTypeConstants.FragScan, scanInfo, sicOptions.DatasetID);

            if (thermoScanInfo.MRMScanType == MRMScanTypeConstants.NotMRM)
            {
                // This is not an MRM scan
                mParentIonProcessor.AddUpdateParentIons(
                    scanList, mLastNonZoomSurveyScanIndex, thermoScanInfo.ParentIonMZ,
                    fragScanIndex, spectraCache, sicOptions, thermoScanInfo.IsDIA);
            }
            else
            {
                // This is an MRM scan
                mParentIonProcessor.AddUpdateParentIons(
                    scanList, mLastNonZoomSurveyScanIndex, thermoScanInfo.ParentIonMZ,
                    scanInfo.MRMScanInfo, spectraCache, sicOptions);
            }

            if (mLastNonZoomSurveyScanIndex >= 0)
            {
                var precursorScanNumber = scanList.SurveyScans[mLastNonZoomSurveyScanIndex].ScanNumber;

                // Compute the interference of the parent ion in the MS1 spectrum for this fragmentation scan
                scanInfo.FragScanInfo.InterferenceScore = ComputeInterference(rawFileReader, thermoScanInfo, precursorScanNumber);
            }

            return true;
        }

        private void InitOptions(ScanList scanList,
                                 bool keepRawSpectra,
                                 bool keepMSMSSpectra)
        {
            if (mOptions.SICOptions.ScanRangeStart > 0 && mOptions.SICOptions.ScanRangeEnd == 0)
            {
                mOptions.SICOptions.ScanRangeEnd = int.MaxValue;
            }

            scanList.Initialize();

            mSIMScanMapping.Clear();

            InitBaseOptions(scanList, keepRawSpectra, keepMSMSSpectra);
        }

        private bool LoadSpectraForThermoRawFile(
            XRawFileIO rawFileReader,
            SpectraCache spectraCache,
            ScanInfo scanInfo,
            BaselineNoiseOptions noiseThresholdOptions,
            bool discardLowIntensityData,
            bool compressSpectraData,
            double msDataResolution,
            bool keepRawSpectrum)
        {
            var lastKnownLocation = "Start";

            try
            {
                // Load the ions for this scan

                lastKnownLocation = "rawFileReader.GetScanData for scan " + scanInfo.ScanNumber;

                // Retrieve the m/z and intensity values for the given scan
                // We retrieve the profile-mode data, since that's required for determining spectrum noise
                scanInfo.IonCountRaw = rawFileReader.GetScanData(scanInfo.ScanNumber, out var mzList, out var intensityList);

                if (scanInfo.IonCountRaw > 0)
                {
                    var ionCountVerified = VerifyDataSorted(scanInfo.ScanNumber, scanInfo.IonCountRaw, mzList, intensityList);

                    if (ionCountVerified != scanInfo.IonCountRaw)
                    {
                        scanInfo.IonCountRaw = ionCountVerified;
                    }
                }

                scanInfo.IonCount = scanInfo.IonCountRaw;

                lastKnownLocation = "Instantiate new clsMSSpectrum";

                var msSpectrum = new MSSpectrum(scanInfo.ScanNumber, mzList, intensityList);

                lastKnownLocation = "Manually determine the base peak m/z and base peak intensity";

                // ReSharper disable once CommentTypo

                // Regarding BPI, comparison of data read via the ThermoRawFileReader vs.
                // that read from the .mzML file for dataset QC_Shew_18_02-run1_02Mar19_Arwen_18-11-02
                // showed that 25% of the spectra had incorrect BPI values

                double totalIonIntensity = 0;
                double basePeakIntensity = 0;
                double basePeakMz = 0;

                for (var ionIndex = 0; ionIndex < scanInfo.IonCountRaw; ionIndex++)
                {
                    totalIonIntensity += intensityList[ionIndex];

                    if (intensityList[ionIndex] > basePeakIntensity)
                    {
                        basePeakIntensity = intensityList[ionIndex];
                        basePeakMz = mzList[ionIndex];
                    }
                }

                if (Math.Abs(scanInfo.BasePeakIonMZ - basePeakMz) > 0.1)
                {
                    mBpiUpdateCount++;

                    if (mBpiUpdateCount < 10)
                    {
                        ConsoleMsgUtils.ShowDebug(
                            "Updating BPI in scan {0} from {1:F3} m/z to {2:F3} m/z, and BPI Intensity from {3:F0} to {4:F0}",
                            scanInfo.ScanNumber, scanInfo.BasePeakIonMZ, basePeakMz, scanInfo.BasePeakIonIntensity, basePeakIntensity);
                    }

                    scanInfo.BasePeakIonMZ = basePeakMz;
                    scanInfo.BasePeakIonIntensity = basePeakIntensity;
                }

                // Determine the minimum positive intensity in this scan
                lastKnownLocation = "Call mMASICPeakFinder.FindMinimumPositiveValue";
                scanInfo.MinimumPositiveIntensity = mPeakFinder.FindMinimumPositiveValue(msSpectrum.IonsIntensity, 0);

                if (msSpectrum.IonCount > 0)
                {
                    if (scanInfo.TotalIonIntensity < float.Epsilon)
                    {
                        scanInfo.TotalIonIntensity = totalIonIntensity;
                    }
                }
                else
                {
                    scanInfo.TotalIonIntensity = 0;
                }

                bool discardLowIntensityDataWork;
                bool compressSpectraDataWork;

                if (scanInfo.MRMScanType == MRMScanTypeConstants.NotMRM)
                {
                    discardLowIntensityDataWork = discardLowIntensityData;
                    compressSpectraDataWork = compressSpectraData;
                }
                else
                {
                    discardLowIntensityDataWork = false;
                    compressSpectraDataWork = false;
                }

                lastKnownLocation = "Call ProcessAndStoreSpectrum";
                var spectrumStored = mScanTracking.ProcessAndStoreSpectrum(
                    scanInfo, this,
                    spectraCache, msSpectrum,
                    noiseThresholdOptions,
                    discardLowIntensityDataWork,
                    compressSpectraDataWork,
                    msDataResolution,
                    keepRawSpectrum);

                if (spectraCache.PageFileInitializationFailed)
                {
                    mOptions.AbortProcessing = true;
                    return false;
                }

                if (!spectrumStored)
                    return false;
            }
            catch (Exception ex)
            {
                ReportError("Error in LoadSpectraForThermoRawFile (LastKnownLocation: " + lastKnownLocation + ")", ex, clsMASIC.MasicErrorCodes.InputFileDataReadError);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Update dataset file stats
        /// </summary>
        /// <param name="rawFileInfo"></param>
        /// <param name="datasetID"></param>
        /// <param name="rawFileReader"></param>
        /// <returns>True if success, false if an error</returns>
        protected bool UpdateDatasetFileStats(
            FileInfo rawFileInfo,
            int datasetID,
            XRawFileIO rawFileReader)
        {
            // Read the file info from the file system
            var success = UpdateDatasetFileStats(rawFileInfo, datasetID);

            if (!success)
                return false;

            // Read the file info using the ThermoRawFileReader
            try
            {
                mDatasetFileInfo.AcqTimeStart = rawFileReader.FileInfo.CreationDate;
            }
            catch (Exception)
            {
                // Read error
                return false;
            }

            try
            {
                // Look up the end scan time then compute .AcqTimeEnd
                var scanEnd = rawFileReader.ScanEnd;
                rawFileReader.GetScanInfo(scanEnd, out var scanInfo);

                mDatasetFileInfo.AcqTimeEnd = mDatasetFileInfo.AcqTimeStart.AddMinutes(scanInfo.RetentionTime);
                mDatasetFileInfo.ScanCount = rawFileReader.GetNumScans();
            }
            catch (Exception)
            {
                // Error; use default values
                mDatasetFileInfo.AcqTimeEnd = mDatasetFileInfo.AcqTimeStart;
                mDatasetFileInfo.ScanCount = 0;
            }

            return true;
        }

        private void StoreExtendedHeaderInfo(
            DataOutput.DataOutput dataOutputHandler,
            ScanInfo scanInfo,
            string entryName,
            string entryValue)
        {
            entryValue ??= string.Empty;

            var statusEntries = new List<KeyValuePair<string, string>>
            {
                new(entryName, entryValue)
            };

            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, statusEntries);
        }

        private void StoreExtendedHeaderInfo(
            DataOutput.DataOutput dataOutputHandler,
            ScanInfo scanInfo,
            IReadOnlyCollection<KeyValuePair<string, string>> statusEntries)
        {
            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, statusEntries, new SortedSet<string>());
        }

        /// <summary>
        /// Store extended header info for a scan
        /// </summary>
        /// <param name="dataOutputHandler"></param>
        /// <param name="scanInfo"></param>
        /// <param name="statusEntries"></param>
        /// <param name="keyNameFilterList">List of header names to store; store all headers if this is an entry list</param>
        private void StoreExtendedHeaderInfo(
            DataOutput.DataOutput dataOutputHandler,
            ScanInfo scanInfo,
            IReadOnlyCollection<KeyValuePair<string, string>> statusEntries,
            IReadOnlyCollection<string> keyNameFilterList)
        {
            var filterItems = false;

            try
            {
                if (statusEntries == null)
                    return;

                if (keyNameFilterList?.Count > 0 && keyNameFilterList.Any(item => item.Length > 0))
                {
                    filterItems = true;
                }

                foreach (var statusEntry in statusEntries)
                {
                    if (string.IsNullOrWhiteSpace(statusEntry.Key))
                    {
                        // Empty entry name; do not add
                        continue;
                    }

                    bool saveItem;

                    if (filterItems)
                    {
                        saveItem = false;

                        foreach (var item in keyNameFilterList)
                        {
                            if (statusEntry.Key.IndexOf(item, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                saveItem = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        saveItem = true;
                    }

                    if (string.IsNullOrWhiteSpace(statusEntry.Key) || statusEntry.Key.Equals("1"))
                    {
                        // Name is null; skip it
                        saveItem = false;
                    }

                    if (saveItem)
                    {
                        var extendedHeaderID = dataOutputHandler.ExtendedStatsWriter.GetExtendedHeaderInfoIdByName(statusEntry.Key);

                        // Add or update the value for extendedHeaderID
                        scanInfo.ExtendedHeaderInfo[extendedHeaderID] = statusEntry.Value.Trim();
                    }
                }
            }
            catch (Exception)
            {
                // Ignore any errors here
            }
        }

        /// <summary>
        /// Verify that data in mzList is sorted ascending
        /// </summary>
        /// <param name="scanNumber">Scan number</param>
        /// <param name="ionCount">Expected length of mzList and intensityList</param>
        /// <param name="mzList"></param>
        /// <param name="intensityList"></param>
        /// <returns>Number of data points in mzList</returns>
        private int VerifyDataSorted(int scanNumber, int ionCount, double[] mzList, double[] intensityList)
        {
            if (ionCount != mzList.Length)
            {
                if (ionCount == 0)
                {
                    ReportWarning("Scan found with IonCount = 0, scan " + scanNumber);
                }
                else
                {
                    ReportWarning(string.Format(
                        "Scan found where IonCount <> mzList.Length, scan {0}: {1} vs. {2}",
                        scanNumber, ionCount, mzList.Length));
                }

                ionCount = Math.Min(ionCount, mzList.Length);
            }

            var sortRequired = false;

            for (var index = 1; index < ionCount; index++)
            {
                // Although the data returned by mXRawFile.GetMassListFromScanNum is generally sorted by m/z,
                // we have observed a few cases in certain scans of certain datasets that points with
                // similar m/z values are swapped and thus slightly out of order
                // The following if statement checks for this
                if (mzList[index] < mzList[index - 1])
                {
                    sortRequired = true;
                    break;
                }
            }

            if (sortRequired)
            {
                Array.Sort(mzList, intensityList);
            }

            return ionCount;
        }
    }
}
