using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MASIC.DataOutput;
using MASICPeakFinder;
using PRISM;
using ThermoRawFileReader;

namespace MASIC.DataInput
{
    public class clsDataImportThermoRaw : clsDataImport
    {
        private const string SCAN_EVENT_CHARGE_STATE = "Charge State";
        private const string SCAN_EVENT_MONOISOTOPIC_MZ = "Monoisotopic M/Z";
        private const string SCAN_EVENT_MS2_ISOLATION_WIDTH = "MS2 Isolation Width";

        private int mBpiUpdateCount;

        private readonly Dictionary<string, int> mSIMScanMapping = new Dictionary<string, int>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="masicOptions"></param>
        /// <param name="peakFinder"></param>
        /// <param name="parentIonProcessor"></param>
        /// <param name="scanTracking"></param>
        public clsDataImportThermoRaw(
            clsMASICOptions masicOptions,
            clsMASICPeakFinder peakFinder,
            clsParentIonProcessing parentIonProcessor,
            clsScanTracking scanTracking)
            : base(masicOptions, peakFinder, parentIonProcessor, scanTracking)
        {
        }

        private double ComputeInterference(
            XRawFileIO xcaliburAccessor,
            ThermoRawFileReader.clsScanInfo scanInfo,
            int precursorScanNumber)
        {
            if (precursorScanNumber != mCachedPrecursorScan)
            {
                var ionCount = xcaliburAccessor.GetScanData(precursorScanNumber, out var centroidedIonsMz, out var centroidedIonsIntensity, 0, true);

                UpdateCachedPrecursorScan(precursorScanNumber, centroidedIonsMz, centroidedIonsIntensity, ionCount);
            }

            var chargeState = 0;

            var chargeStateText = string.Empty;
            var isolationWidthText = string.Empty;

            scanInfo.TryGetScanEvent(SCAN_EVENT_CHARGE_STATE, out chargeStateText, true);
            if (!string.IsNullOrWhiteSpace(chargeStateText))
            {
                if (!int.TryParse(chargeStateText, out chargeState))
                {
                    chargeState = 0;
                }
            }

            if (!scanInfo.TryGetScanEvent(SCAN_EVENT_MS2_ISOLATION_WIDTH, out isolationWidthText, true))
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
                var monoMzText = string.Empty;
                if (!scanInfo.TryGetScanEvent(SCAN_EVENT_MONOISOTOPIC_MZ, out monoMzText, true))
                {
                    ReportWarning("Could not determine the parent ion m/z value (" + SCAN_EVENT_MONOISOTOPIC_MZ + "); " +
                                  "cannot compute interference for scan " + scanInfo.ScanNumber);
                    return 0;
                }

                if (!double.TryParse(monoMzText, out var mz))
                {
                    OnWarningEvent(string.Format("Skipping scan {0} since scan event {1} was not a number: {2}",
                                                 scanInfo.ScanNumber, SCAN_EVENT_MONOISOTOPIC_MZ, monoMzText));
                    return 0;
                }

                parentIonMz = mz;
            }

            if (Math.Abs(parentIonMz) < float.Epsilon)
            {
                ReportWarning("Parent ion m/z is 0; cannot compute interference for scan " + scanInfo.ScanNumber);
                return 0;
            }

            var precursorInterference = ComputePrecursorInterference(
                scanInfo.ScanNumber,
                precursorScanNumber, parentIonMz, isolationWidth, chargeState);

            return precursorInterference;
        }

        /// <summary>
        /// Read scan data and ions from a Thermo .raw file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="scanList"></param>
        /// <param name="spectraCache"></param>
        /// <param name="dataOutputHandler"></param>
        /// <param name="keepRawSpectra"></param>
        /// <param name="keepMSMSSpectra"></param>
        /// <returns>True if Success, False if failure</returns>
        /// <remarks>Assumes filePath exists</remarks>
        public bool ExtractScanInfoFromXcaliburDataFile(
            string filePath,
            clsScanList scanList,
            clsSpectraCache spectraCache,
            clsDataOutput dataOutputHandler,
            bool keepRawSpectra,
            bool keepMSMSSpectra)
        {
            // Use XrawFileIO to read the .Raw files (it uses ThermoFisher.CommonCore)

            var readerOptions = new ThermoReaderOptions()
            {
                LoadMSMethodInfo = mOptions.WriteMSMethodFile,
                LoadMSTuneInfo = mOptions.WriteMSTuneFile
            };

            var xcaliburAccessor = new XRawFileIO(readerOptions);
            RegisterEvents(xcaliburAccessor);

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
                if (!xcaliburAccessor.OpenRawFile(inputFileFullPath))
                {
                    ReportError("Error opening input data file: " + inputFileFullPath + " (xcaliburAccessor.OpenRawFile returned False)");
                    SetLocalErrorCode(clsMASIC.eMasicErrorCodes.InputFileAccessError);
                    return false;
                }

                if (xcaliburAccessor == null)
                {
                    ReportError("Error opening input data file: " + inputFileFullPath + " (xcaliburAccessor is Nothing)");
                    SetLocalErrorCode(clsMASIC.eMasicErrorCodes.InputFileAccessError);
                    return false;
                }

                var datasetID = mOptions.SICOptions.DatasetID;

                success = UpdateDatasetFileStats(rawFileInfo, datasetID, xcaliburAccessor);

                var metadataWriter = new clsThermoMetadataWriter();
                RegisterEvents(metadataWriter);

                if (mOptions.WriteMSMethodFile)
                {
                    metadataWriter.SaveMSMethodFile(xcaliburAccessor, dataOutputHandler);
                }

                if (mOptions.WriteMSTuneFile)
                {
                    metadataWriter.SaveMSTuneFile(xcaliburAccessor, dataOutputHandler);
                }

                var scanCount = xcaliburAccessor.GetNumScans();

                if (scanCount <= 0)
                {
                    // No scans found
                    ReportError("No scans found in the input file: " + filePath);
                    SetLocalErrorCode(clsMASIC.eMasicErrorCodes.InputFileAccessError);
                    return false;
                }

                var scanStart = xcaliburAccessor.ScanStart;
                var scanEnd = xcaliburAccessor.ScanEnd;

                InitOptions(scanList, keepRawSpectra, keepMSMSSpectra);

                UpdateProgress(string.Format("Reading Xcalibur data ({0:N0} scans){1}", scanCount, Environment.NewLine + Path.GetFileName(filePath)));
                ReportMessage(string.Format("Reading Xcalibur data; Total scan count: {0:N0}", scanCount));

                var scanCountToRead = scanEnd - scanStart + 1;
                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    if (!mScanTracking.CheckScanInRange(scanNumber, mOptions.SICOptions))
                    {
                        mScansOutOfRange += 1;
                        continue;
                    }

                    success = xcaliburAccessor.GetScanInfo(scanNumber, out ThermoRawFileReader.clsScanInfo thermoScanInfo);
                    if (!success)
                    {
                        // GetScanInfo returned false
                        ReportWarning("xcaliburAccessor.GetScanInfo returned false for scan " + scanNumber.ToString() + "; aborting read");
                        break;
                    }

                    var percentComplete = scanList.MasterScanOrderCount / (double)(scanCountToRead) * 100;
                    var extractSuccess = ExtractScanInfoCheckRange(xcaliburAccessor, thermoScanInfo, scanList, spectraCache, dataOutputHandler, percentComplete);

                    if (!extractSuccess)
                    {
                        break;
                    }
                }

                Console.WriteLine();

                // Shrink the memory usage of the scanList arrays
                success = FinalizeScanList(scanList, rawFileInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                ReportError("Error in ExtractScanInfoFromXcaliburDataFile", ex, clsMASIC.eMasicErrorCodes.InputFileDataReadError);
            }

            // Close the handle to the data file
            xcaliburAccessor.CloseRawFile();

            return success;
        }

        private bool ExtractScanInfoCheckRange(
            XRawFileIO xcaliburAccessor,
            ThermoRawFileReader.clsScanInfo thermoScanInfo,
            clsScanList scanList,
            clsSpectraCache spectraCache,
            clsDataOutput dataOutputHandler,
            double percentComplete)
        {
            bool success;

            if (mScanTracking.CheckScanInRange(thermoScanInfo.ScanNumber, thermoScanInfo.RetentionTime, mOptions.SICOptions))
            {
                success = ExtractScanInfoWork(xcaliburAccessor, scanList, spectraCache, dataOutputHandler,
                                              mOptions.SICOptions, thermoScanInfo);
            }
            else
            {
                mScansOutOfRange += 1;
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
                thermoScanInfo.ScanNumber % 500 == 0 && (
                    thermoScanInfo.ScanNumber >= mOptions.SICOptions.ScanRangeStart &&
                    thermoScanInfo.ScanNumber <= mOptions.SICOptions.ScanRangeEnd))
            {
                ReportMessage("Reading scan: " + thermoScanInfo.ScanNumber.ToString());
                Console.Write(".");
                mLastLogTime = DateTime.UtcNow;
            }

            if ((scanList.MasterScanOrderCount - 1) % 100 == 0)
            {
                // Call the garbage collector every 100 spectra
                GC.Collect();
                GC.WaitForPendingFinalizers();
                System.Threading.Thread.Sleep(50);
            }

            return success;
        }

        private bool ExtractScanInfoWork(
            XRawFileIO xcaliburAccessor,
            clsScanList scanList,
            clsSpectraCache spectraCache,
            clsDataOutput dataOutputHandler,
            clsSICOptions sicOptions,
            ThermoRawFileReader.clsScanInfo thermoScanInfo)
        {
            if (thermoScanInfo.ParentIonMZ > 0 && Math.Abs(mOptions.ParentIonDecoyMassDa) > 0)
            {
                thermoScanInfo.ParentIonMZ += mOptions.ParentIonDecoyMassDa;
            }

            bool success;

            // Determine if this was an MS/MS scan
            // If yes, determine the scan number of the survey scan
            if (thermoScanInfo.MSLevel <= 1)
            {
                // Survey Scan
                success = ExtractXcaliburSurveyScan(xcaliburAccessor, scanList, spectraCache, dataOutputHandler,
                                                    sicOptions, thermoScanInfo);
            }
            else
            {
                // Fragmentation Scan
                success = ExtractXcaliburFragmentationScan(xcaliburAccessor, scanList, spectraCache, dataOutputHandler,
                                                           sicOptions, mOptions.BinningOptions, thermoScanInfo);
            }

            return success;
        }

        private bool ExtractXcaliburSurveyScan(
            XRawFileIO xcaliburAccessor,
            clsScanList scanList,
            clsSpectraCache spectraCache,
            clsDataOutput dataOutputHandler,
            clsSICOptions sicOptions,
            ThermoRawFileReader.clsScanInfo thermoScanInfo)
        {
            var scanInfo = new clsScanInfo()
            {
                ScanNumber = thermoScanInfo.ScanNumber,
                ScanTime = (float)thermoScanInfo.RetentionTime,
                ScanHeaderText = XRawFileIO.MakeGenericThermoScanFilter(thermoScanInfo.FilterText),
                ScanTypeName = XRawFileIO.GetScanTypeNameFromThermoScanFilterText(thermoScanInfo.FilterText),
                BasePeakIonMZ = thermoScanInfo.BasePeakMZ,
                BasePeakIonIntensity = thermoScanInfo.BasePeakIntensity,
                TotalIonIntensity = thermoScanInfo.TotalIonCurrent,
                MinimumPositiveIntensity = 0,        // This will be determined in LoadSpectraForThermoRawFile
                ZoomScan = thermoScanInfo.ZoomScan,
                SIMScan = thermoScanInfo.SIMScan,
                MRMScanType = thermoScanInfo.MRMScanType,
                LowMass = thermoScanInfo.LowMass,
                HighMass = thermoScanInfo.HighMass,
                IsFTMS = thermoScanInfo.IsFTMS
            };

            // Survey scans typically lead to multiple parent ions; we do not record them here
            scanInfo.FragScanInfo.ParentIonInfoIndex = -1;

            if (!(scanInfo.MRMScanType == MRMScanTypeConstants.NotMRM))
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
            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, clsExtendedStatsWriter.EXTENDED_STATS_HEADER_COLLISION_MODE, thermoScanInfo.CollisionMode);
            if (mOptions.WriteExtendedStatsIncludeScanFilterText)
            {
                StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, clsExtendedStatsWriter.EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, thermoScanInfo.FilterText);
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

            scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan, scanList.SurveyScans.Count - 1);

            double msDataResolution;

            if (sicOptions.SICToleranceIsPPM)
            {
                // Define MSDataResolution based on the tolerance value that will be used at the lowest m/z in this spectrum, divided by sicOptions.CompressToleranceDivisorForPPM
                // However, if the lowest m/z value is < 100, then use 100 m/z
                if (thermoScanInfo.LowMass < 100)
                {
                    msDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, 100) /
                        sicOptions.CompressToleranceDivisorForPPM;
                }
                else
                {
                    msDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, thermoScanInfo.LowMass) /
                        sicOptions.CompressToleranceDivisorForPPM;
                }
            }
            else
            {
                msDataResolution = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForDa;
            }

            // Note: Even if mKeepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
            var success = LoadSpectraForThermoRawFile(
                xcaliburAccessor,
                spectraCache,
                scanInfo,
                sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                clsMASIC.DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD,
                sicOptions.CompressMSSpectraData,
                msDataResolution,
                mKeepRawSpectra);

            if (!success)
                return false;

            SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats, clsScanList.eScanTypeConstants.SurveyScan, scanInfo, sicOptions.DatasetID);

            return true;
        }

        private bool ExtractXcaliburFragmentationScan(
            XRawFileIO xcaliburAccessor,
            clsScanList scanList,
            clsSpectraCache spectraCache,
            clsDataOutput dataOutputHandler,
            clsSICOptions sicOptions,
            clsBinningOptions binningOptions,
            ThermoRawFileReader.clsScanInfo thermoScanInfo)
        {
            // Note that MinimumPositiveIntensity will be determined in LoadSpectraForThermoRawFile

            var scanInfo = new clsScanInfo(thermoScanInfo.ParentIonMZ)
            {
                ScanNumber = thermoScanInfo.ScanNumber,
                ScanTime = (float)thermoScanInfo.RetentionTime,
                ScanHeaderText = XRawFileIO.MakeGenericThermoScanFilter(thermoScanInfo.FilterText),
                ScanTypeName = XRawFileIO.GetScanTypeNameFromThermoScanFilterText(thermoScanInfo.FilterText),
                BasePeakIonMZ = thermoScanInfo.BasePeakMZ,
                BasePeakIonIntensity = thermoScanInfo.BasePeakIntensity,
                TotalIonIntensity = thermoScanInfo.TotalIonCurrent,
                MinimumPositiveIntensity = 0,
                ZoomScan = thermoScanInfo.ZoomScan,
                SIMScan = thermoScanInfo.SIMScan,
                MRMScanType = thermoScanInfo.MRMScanType
            };

            // Typically .EventNumber is 1 for the parent-ion scan; 2 for 1st frag scan, 3 for 2nd frag scan, etc.
            // This resets for each new parent-ion scan
            scanInfo.FragScanInfo.FragScanNumber = thermoScanInfo.EventNumber - 1;

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

            if (!(scanInfo.MRMScanType == MRMScanTypeConstants.NotMRM))
            {
                // This is an MRM scan
                scanList.MRMDataPresent = true;

                scanInfo.MRMScanInfo = clsMRMProcessing.DuplicateMRMInfo(thermoScanInfo.MRMInfo, thermoScanInfo.ParentIonMZ);

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
            scanInfo.IsFTMS = thermoScanInfo.IsFTMS;

            // Store the ScanEvent values in .ExtendedHeaderInfo
            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, thermoScanInfo.ScanEvents);

            // Store the collision mode and possibly the scan filter text
            scanInfo.FragScanInfo.CollisionMode = thermoScanInfo.CollisionMode;
            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, clsExtendedStatsWriter.EXTENDED_STATS_HEADER_COLLISION_MODE, thermoScanInfo.CollisionMode);
            if (mOptions.WriteExtendedStatsIncludeScanFilterText)
            {
                StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, clsExtendedStatsWriter.EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, thermoScanInfo.FilterText);
            }

            if (mOptions.WriteExtendedStatsStatusLog)
            {
                // Store the StatusLog values in .ExtendedHeaderInfo
                StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, thermoScanInfo.StatusLog, mOptions.StatusLogKeyNameFilterList);
            }

            scanList.FragScans.Add(scanInfo);
            var fragScanIndex = scanList.FragScans.Count - 1;

            scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.FragScan, fragScanIndex);

            // Note: Even if keepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
            var msDataResolution = binningOptions.BinSize / sicOptions.CompressToleranceDivisorForDa;

            var success = LoadSpectraForThermoRawFile(
                xcaliburAccessor,
                spectraCache,
                scanInfo,
                sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                clsMASIC.DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD,
                sicOptions.CompressMSMSSpectraData,
                msDataResolution,
                mKeepRawSpectra && mKeepMSMSSpectra);

            if (!success)
                return false;

            SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats, clsScanList.eScanTypeConstants.FragScan, scanInfo, sicOptions.DatasetID);

            if (thermoScanInfo.MRMScanType == MRMScanTypeConstants.NotMRM)
            {
                // This is not an MRM scan
                mParentIonProcessor.AddUpdateParentIons(scanList, mLastNonZoomSurveyScanIndex, thermoScanInfo.ParentIonMZ,
                                                        fragScanIndex, spectraCache, sicOptions);
            }
            else
            {
                // This is an MRM scan
                mParentIonProcessor.AddUpdateParentIons(scanList, mLastNonZoomSurveyScanIndex, thermoScanInfo.ParentIonMZ,
                                                        scanInfo.MRMScanInfo, spectraCache, sicOptions);
            }

            if (mLastNonZoomSurveyScanIndex >= 0)
            {
                var precursorScanNumber = scanList.SurveyScans[mLastNonZoomSurveyScanIndex].ScanNumber;

                // Compute the interference of the parent ion in the MS1 spectrum for this frag scan
                scanInfo.FragScanInfo.InterferenceScore = ComputeInterference(xcaliburAccessor, thermoScanInfo, precursorScanNumber);
            }

            return true;
        }

        private void InitOptions(clsScanList scanList,
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
            XRawFileIO xcaliburAccessor,
            clsSpectraCache spectraCache,
            clsScanInfo scanInfo,
            clsBaselineNoiseOptions noiseThresholdOptions,
            bool discardLowIntensityData,
            bool compressSpectraData,
            double msDataResolution,
            bool keepRawSpectrum)
        {
            var lastKnownLocation = "Start";

            try
            {
                // Load the ions for this scan

                lastKnownLocation = "xcaliburAccessor.GetScanData for scan " + scanInfo.ScanNumber;

                // Retrieve the m/z and intensity values for the given scan
                // We retrieve the profile-mode data, since that's required for determining spectrum noise
                scanInfo.IonCountRaw = xcaliburAccessor.GetScanData(scanInfo.ScanNumber, out var mzList, out var intensityList);

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

                var msSpectrum = new clsMSSpectrum(scanInfo.ScanNumber, mzList, intensityList, scanInfo.IonCountRaw);

                lastKnownLocation = "Manually determine the base peak m/z and base peak intensity";

                // ReSharper disable once CommentTypo

                // Regarding BPI, comparison of data read via the ThermoRawFileReader vs.
                // that read from the .mzML file for dataset QC_Shew_18_02-run1_02Mar19_Arwen_18-11-02
                // showed that 25% of the spectra had incorrect BPI values

                double totalIonIntensity = 0;
                double basePeakIntensity = 0;
                double basePeakMz = 0;

                for (var ionIndex = 0; ionIndex <= scanInfo.IonCountRaw - 1; ionIndex++)
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
                    mBpiUpdateCount += 1;

                    if (mBpiUpdateCount < 10)
                    {
                        ConsoleMsgUtils.ShowDebug("Updating BPI in scan {0} from {1:F3} m/z to {2:F3} m/z, and BPI Intensity from {3:F0} to {4:F0}",
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
                mScanTracking.ProcessAndStoreSpectrum(
                    scanInfo, this,
                    spectraCache, msSpectrum,
                    noiseThresholdOptions,
                    discardLowIntensityDataWork,
                    compressSpectraDataWork,
                    msDataResolution,
                    keepRawSpectrum);
            }
            catch (Exception ex)
            {
                ReportError("Error in LoadSpectraForThermoRawFile (LastKnownLocation: " + lastKnownLocation + ")", ex, clsMASIC.eMasicErrorCodes.InputFileDataReadError);
                return false;
            }

            return true;
        }

        protected bool UpdateDatasetFileStats(
            FileInfo rawFileInfo,
            int datasetID,
            XRawFileIO xcaliburAccessor)
        {
            var scanInfo = new ThermoRawFileReader.clsScanInfo(0);

            // Read the file info from the file system
            var success = UpdateDatasetFileStats(rawFileInfo, datasetID);

            if (!success)
                return false;

            // Read the file info using the Xcalibur Accessor
            try
            {
                mDatasetFileInfo.AcqTimeStart = xcaliburAccessor.FileInfo.CreationDate;
            }
            catch (Exception ex)
            {
                // Read error
                return false;
            }

            try
            {
                // Look up the end scan time then compute .AcqTimeEnd
                var scanEnd = xcaliburAccessor.ScanEnd;
                xcaliburAccessor.GetScanInfo(scanEnd, out scanInfo);

                mDatasetFileInfo.AcqTimeEnd = mDatasetFileInfo.AcqTimeStart.AddMinutes(scanInfo.RetentionTime);
                mDatasetFileInfo.ScanCount = xcaliburAccessor.GetNumScans();
            }
            catch (Exception ex)
            {
                // Error; use default values
                mDatasetFileInfo.AcqTimeEnd = mDatasetFileInfo.AcqTimeStart;
                mDatasetFileInfo.ScanCount = 0;
            }

            return true;
        }

        private void StoreExtendedHeaderInfo(
            clsDataOutput dataOutputHandler,
            clsScanInfo scanInfo,
            string entryName,
            string entryValue)
        {
            if (entryValue == null)
            {
                entryValue = string.Empty;
            }

            var statusEntries = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>(entryName, entryValue)
            };

            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, statusEntries);
        }

        private void StoreExtendedHeaderInfo(
            clsDataOutput dataOutputHandler,
            clsScanInfo scanInfo,
            IReadOnlyCollection<KeyValuePair<string, string>> statusEntries)
        {
            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, statusEntries, new SortedSet<string>());
        }

        private void StoreExtendedHeaderInfo(
            clsDataOutput dataOutputHandler,
            clsScanInfo scanInfo,
            IReadOnlyCollection<KeyValuePair<string, string>> statusEntries,
            IReadOnlyCollection<string> keyNameFilterList)
        {
            var filterItems = false;

            try
            {
                if (statusEntries == null)
                    return;
                if (keyNameFilterList != null && keyNameFilterList.Count > 0)
                {
                    if (keyNameFilterList.Any(item => item.Length > 0))
                    {
                        filterItems = true;
                    }
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
                            if (statusEntry.Key.ToLower().Contains(item.ToLower()))
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
            catch (Exception ex)
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

            for (var index = 1; index <= ionCount - 1; index++)
            {
                // Although the data returned by mXRawFile.GetMassListFromScanNum is generally sorted by m/z,
                // we have observed a few cases in certain scans of certain datasets that points with
                // similar m/z values are swapped and ths slightly out of order
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
