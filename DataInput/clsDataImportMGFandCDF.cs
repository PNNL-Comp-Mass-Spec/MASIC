using System;
using System.Collections.Generic;
using System.IO;
using MASIC.Options;

namespace MASIC.DataInput
{
    // ReSharper disable once IdentifierTypo
    public class clsDataImportMGFandCDF : clsDataImport
    {
        // ReSharper disable once IdentifierTypo
        public clsDataImportMGFandCDF(
            MASICOptions masicOptions,
            MASICPeakFinder.clsMASICPeakFinder peakFinder,
            clsParentIonProcessing parentIonProcessor,
            clsScanTracking scanTracking)
            : base(masicOptions, peakFinder, parentIonProcessor, scanTracking)
        {
        }

        // ReSharper disable once IdentifierTypo
        public bool ExtractScanInfoFromMGFandCDF(
            string filePath,
            clsScanList scanList,
            clsSpectraCache spectraCache,
            DataOutput.clsDataOutput dataOutputHandler,
            bool keepRawSpectra,
            bool keepMSMSSpectra)
        {
            // Returns True if Success, False if failure
            // Note: This function assumes filePath exists
            //
            // This function can be used to read a pair of MGF and NetCDF files that contain MS/MS and MS-only parent ion scans, respectively
            // Typically, this will apply to LC-MS/MS analyses acquired using an Agilent mass spectrometer running DataAnalysis software
            // filePath can contain the path to the MGF or to the CDF file; the extension will be removed in order to determine the base file name,
            // then the two files will be looked for separately

            var scanTime = 0.0;

            var cdfReader = new NetCDFReader.clsMSNetCdf();
            var mgfReader = new MSDataFileReader.clsMGFFileReader();

            try
            {
                Console.Write("Reading CDF/MGF data files ");
                ReportMessage("Reading CDF/MGF data files");

                UpdateProgress(0, "Opening data file: " + Environment.NewLine + Path.GetFileName(filePath));

                // Obtain the full path to the file
                var mgfFileInfo = new FileInfo(filePath);
                var mgfInputFilePathFull = mgfFileInfo.FullName;

                // Make sure the extension for mgfInputFilePathFull is .MGF
                mgfInputFilePathFull = Path.ChangeExtension(mgfInputFilePathFull, AGILENT_MSMS_FILE_EXTENSION);
                var cdfInputFilePathFull = Path.ChangeExtension(mgfInputFilePathFull, AGILENT_MS_FILE_EXTENSION);

                var datasetID = mOptions.SICOptions.DatasetID;
                var sicOptions = mOptions.SICOptions;

                var success = UpdateDatasetFileStats(mgfFileInfo, datasetID);
                mDatasetFileInfo.ScanCount = 0;

                // Open a handle to each data file
                if (!cdfReader.OpenMSCdfFile(cdfInputFilePathFull))
                {
                    ReportError("Error opening input data file: " + cdfInputFilePathFull);
                    SetLocalErrorCode(clsMASIC.MasicErrorCodes.InputFileAccessError);
                    return false;
                }

                if (!mgfReader.OpenFile(mgfInputFilePathFull))
                {
                    ReportError("Error opening input data file: " + mgfInputFilePathFull);
                    SetLocalErrorCode(clsMASIC.MasicErrorCodes.InputFileAccessError);
                    return false;
                }

                var msScanCount = cdfReader.GetScanCount();
                mDatasetFileInfo.ScanCount = msScanCount;

                if (msScanCount <= 0)
                {
                    // No scans found
                    ReportError("No scans found in the input file: " + cdfInputFilePathFull);
                    SetLocalErrorCode(clsMASIC.MasicErrorCodes.InputFileAccessError);
                    return false;
                }

                // Reserve memory for all of the Survey Scan data
                scanList.Initialize();
                var scanCount = mOptions.SICOptions.ScanRangeCount;
                if (scanCount <= 0)
                {
                    scanCount = msScanCount * 3;
                }
                scanList.ReserveListCapacity(scanCount, Math.Min(msScanCount, scanCount / 3)); // Assume there are 2 frag scans for every survey scan
                mScanTracking.ReserveListCapacity(scanCount);
                spectraCache.SpectrumCount = Math.Max(spectraCache.SpectrumCount, scanCount);

                UpdateProgress("Reading CDF/MGF data (" + msScanCount.ToString() + " scans)" + Environment.NewLine + Path.GetFileName(filePath));
                ReportMessage("Reading CDF/MGF data; Total MS scan count: " + msScanCount.ToString());

                // Read all of the Survey scans from the CDF file
                // CDF files created by the Agilent XCT list the first scan number as 0; use scanNumberCorrection to correct for this
                var scanNumberCorrection = 0;
                for (var msScanIndex = 0; msScanIndex < msScanCount; msScanIndex++)
                {
                    success = cdfReader.GetScanInfo(msScanIndex, out var scanNumber, out var scanTotalIntensity, out scanTime, out _, out _);

                    if (msScanIndex == 0 && scanNumber == 0)
                    {
                        scanNumberCorrection = 1;
                    }

                    if (!success)
                    {
                        // Error reading CDF file
                        ReportError("Error obtaining data from CDF file: " + cdfInputFilePathFull);
                        SetLocalErrorCode(clsMASIC.MasicErrorCodes.InputFileDataReadError);
                        return false;
                    }

                    if (scanNumberCorrection > 0)
                        scanNumber += scanNumberCorrection;

                    if (mScanTracking.CheckScanInRange(scanNumber, scanTime, sicOptions))
                    {
                        var newSurveyScan = new clsScanInfo
                        {
                            ScanNumber = scanNumber,
                            TotalIonIntensity = (float)scanTotalIntensity,    // Copy the Total Scan Intensity to .TotalIonIntensity
                            ScanHeaderText = string.Empty,
                            ScanTypeName = "MS",
                        };

                        if (mOptions.CDFTimeInSeconds)
                        {
                            newSurveyScan.ScanTime = (float)(scanTime / 60);
                        }
                        else
                        {
                            newSurveyScan.ScanTime = (float)scanTime;
                        }

                        // Survey scans typically lead to multiple parent ions; we do not record them here
                        newSurveyScan.FragScanInfo.ParentIonInfoIndex = -1;

                        scanList.SurveyScans.Add(newSurveyScan);

                        success = cdfReader.GetMassSpectrum(msScanIndex, out var mzData,
                                                               out var intensityData,
                                                               out var intIonCount, out _);

                        if (success && intIonCount > 0)
                        {
                            var msSpectrum = new clsMSSpectrum(scanNumber, mzData, intensityData, intIonCount);

                            double msDataResolution;

                            newSurveyScan.IonCount = msSpectrum.IonCount;
                            newSurveyScan.IonCountRaw = newSurveyScan.IonCount;

                            // Find the base peak ion mass and intensity
                            newSurveyScan.BasePeakIonMZ = FindBasePeakIon(msSpectrum.IonsMZ,
                                msSpectrum.IonsIntensity,
                                out var basePeakIonIntensity,
                                out var mzMin, out _);
                            newSurveyScan.BasePeakIonIntensity = basePeakIonIntensity;

                            // Determine the minimum positive intensity in this scan
                            newSurveyScan.MinimumPositiveIntensity = mPeakFinder.FindMinimumPositiveValue(msSpectrum.IonsIntensity, 0);

                            if (sicOptions.SICToleranceIsPPM)
                            {
                                // Define MSDataResolution based on the tolerance value that will be used at the lowest m/z in this spectrum, divided by COMPRESS_TOLERANCE_DIVISOR
                                // However, if the lowest m/z value is < 100, then use 100 m/z
                                if (mzMin < 100)
                                {
                                    msDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, 100) /
                                        sicOptions.CompressToleranceDivisorForPPM;
                                }
                                else
                                {
                                    msDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, mzMin) /
                                        sicOptions.CompressToleranceDivisorForPPM;
                                }
                            }
                            else
                            {
                                msDataResolution = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForDa;
                            }

                            mScanTracking.ProcessAndStoreSpectrum(
                                newSurveyScan, this,
                                spectraCache, msSpectrum,
                                sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                                clsMASIC.DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD,
                                sicOptions.CompressMSSpectraData,
                                msDataResolution,
                                keepRawSpectra);
                        }
                        else
                        {
                            newSurveyScan.IonCount = 0;
                            newSurveyScan.IonCountRaw = 0;
                        }

                        // Note: Since we're reading all of the Survey Scan data, we cannot update .MasterScanOrder() at this time
                    }

                    // Note: We need to take msScanCount * 2 since we have to read two different files
                    if (msScanCount > 1)
                    {
                        UpdateProgress((short)(msScanIndex / (double)(msScanCount * 2 - 1) * 100));
                    }
                    else
                    {
                        UpdateProgress(0);
                    }

                    UpdateCacheStats(spectraCache);
                    if (mOptions.AbortProcessing)
                    {
                        scanList.ProcessingIncomplete = true;
                        break;
                    }

                    if (msScanIndex % 100 == 0)
                    {
                        ReportMessage("Reading MS scan index: " + msScanIndex.ToString());
                        Console.Write(".");
                    }
                }

                // Record the current memory usage (before we close the .CDF file)
                OnUpdateMemoryUsage();

                cdfReader.CloseMSCdfFile();

                // We loaded all of the survey scan data above
                // We can now initialize .MasterScanOrder()
                var lastSurveyScanIndex = 0;

                scanList.AddMasterScanEntry(clsScanList.ScanTypeConstants.SurveyScan, lastSurveyScanIndex);

                var surveyScansRecorded = new SortedSet<int>()
                {
                    lastSurveyScanIndex
                };

                // Reset scanNumberCorrection; we might also apply it to MS/MS data
                scanNumberCorrection = 0;

                // Now read the MS/MS data from the MGF file
                do
                {
                    var fragScanFound = mgfReader.ReadNextSpectrum(out var spectrumInfo);
                    if (!fragScanFound)
                        break;

                    mDatasetFileInfo.ScanCount++;

                    while (spectrumInfo.ScanNumber < scanList.SurveyScans[lastSurveyScanIndex].ScanNumber)
                    {
                        // The scan number for the current MS/MS spectrum is less than the last survey scan index scan number
                        // This can happen, due to oddities with combining scans when creating the .MGF file
                        // Need to decrement lastSurveyScanIndex until we find the appropriate survey scan

                        lastSurveyScanIndex--;
                        if (lastSurveyScanIndex == 0)
                            break;
                    }

                    if (scanNumberCorrection == 0)
                    {
                        // See if udtSpectrumHeaderInfo.ScanNumberStart is equivalent to one of the survey scan numbers, yielding conflicting scan numbers
                        // If it is, then there is an indexing error in the .MGF file; this error was present in .MGF files generated with
                        // an older version of Agilent Chemstation.  These files typically have lines like ###MSMS: #13-29 instead of ###MSMS: #13/29/
                        // If this indexing error is found, then we'll set scanNumberCorrection = 1 and apply it to all subsequent MS/MS scans;
                        // we'll also need to correct prior MS/MS scans
                        for (var surveyScanIndex = lastSurveyScanIndex; surveyScanIndex < scanList.SurveyScans.Count; surveyScanIndex++)
                        {
                            if (scanList.SurveyScans[surveyScanIndex].ScanNumber == spectrumInfo.ScanNumber)
                            {
                                // Conflicting scan numbers were found
                                scanNumberCorrection = 1;

                                // Need to update prior MS/MS scans
                                foreach (var fragScan in scanList.FragScans)
                                {
                                    fragScan.ScanNumber += scanNumberCorrection;
                                    var scanTimeInterpolated = InterpolateRTandFragScanNumber(
                                        scanList.SurveyScans, 0, fragScan.ScanNumber, out var fragScanIterationOut);
                                    fragScan.FragScanInfo.FragScanNumber = fragScanIterationOut;

                                    fragScan.ScanTime = scanTimeInterpolated;
                                }

                                break;
                            }

                            if (scanList.SurveyScans[surveyScanIndex].ScanNumber > spectrumInfo.ScanNumber)
                            {
                                break;
                            }
                        }
                    }

                    if (scanNumberCorrection > 0)
                    {
                        spectrumInfo.ScanNumber += scanNumberCorrection;
                        spectrumInfo.ScanNumberEnd += scanNumberCorrection;
                    }

                    scanTime = InterpolateRTandFragScanNumber(
                        scanList.SurveyScans, lastSurveyScanIndex, spectrumInfo.ScanNumber, out var fragScanIteration);

                    // Make sure this fragmentation scan isn't present yet in scanList.FragScans
                    // This can occur in Agilent .MGF files if the scan is listed both singly and grouped with other MS/MS scans
                    var validFragScan = true;
                    foreach (var fragScan in scanList.FragScans)
                    {
                        if (fragScan.ScanNumber == spectrumInfo.ScanNumber)
                        {
                            // Duplicate found
                            validFragScan = false;
                            break;
                        }
                    }

                    if (!(validFragScan && mScanTracking.CheckScanInRange(spectrumInfo.ScanNumber, scanTime, sicOptions)))
                    {
                        continue;
                    }

                    // See if lastSurveyScanIndex needs to be updated
                    // At the same time, populate .MasterScanOrder
                    while (lastSurveyScanIndex < scanList.SurveyScans.Count - 1 &&
                           spectrumInfo.ScanNumber > scanList.SurveyScans[lastSurveyScanIndex + 1].ScanNumber)
                    {
                        lastSurveyScanIndex++;

                        // Add the given SurveyScan to .MasterScanOrder, though only if it hasn't yet been added
                        if (!surveyScansRecorded.Contains(lastSurveyScanIndex))
                        {
                            surveyScansRecorded.Add(lastSurveyScanIndex);
                            scanList.AddMasterScanEntry(clsScanList.ScanTypeConstants.SurveyScan,
                                lastSurveyScanIndex);
                        }
                    }

                    scanList.AddMasterScanEntry(clsScanList.ScanTypeConstants.FragScan, scanList.FragScans.Count,
                        spectrumInfo.ScanNumber, (float)scanTime);

                    var newFragScan = new clsScanInfo
                    {
                        ScanNumber = spectrumInfo.ScanNumber,
                        ScanTime = (float) scanTime,
                        ScanHeaderText = string.Empty,
                        ScanTypeName = "MSn",
                    };

                    newFragScan.FragScanInfo.FragScanNumber = fragScanIteration;
                    newFragScan.FragScanInfo.MSLevel = 2;
                    newFragScan.MRMScanInfo.MRMMassCount = 0;

                    scanList.FragScans.Add(newFragScan);

                    var msSpectrum = new clsMSSpectrum(newFragScan.ScanNumber, spectrumInfo.MZList, spectrumInfo.IntensityList, spectrumInfo.DataCount);

                    if (spectrumInfo.DataCount > 0)
                    {
                        newFragScan.IonCount = msSpectrum.IonCount;
                        newFragScan.IonCountRaw = newFragScan.IonCount;

                        // Find the base peak ion mass and intensity
                        newFragScan.BasePeakIonMZ = FindBasePeakIon(msSpectrum.IonsMZ, msSpectrum.IonsIntensity,
                                                                    out var basePeakIonIntensity,
                                                                    out _, out _);

                        newFragScan.BasePeakIonIntensity = basePeakIonIntensity;

                        // Compute the total scan intensity
                        newFragScan.TotalIonIntensity = 0;
                        for (var ionIndex = 0; ionIndex < newFragScan.IonCount; ionIndex++)
                            newFragScan.TotalIonIntensity += msSpectrum.IonsIntensity[ionIndex];

                        // Determine the minimum positive intensity in this scan
                        newFragScan.MinimumPositiveIntensity = mPeakFinder.FindMinimumPositiveValue(msSpectrum.IonsIntensity, 0);

                        var msDataResolution = mOptions.BinningOptions.BinSize / sicOptions.CompressToleranceDivisorForDa;
                        var keepRawSpectrum = keepRawSpectra && keepMSMSSpectra;

                        mScanTracking.ProcessAndStoreSpectrum(
                            newFragScan, this,
                            spectraCache, msSpectrum,
                            sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                            clsMASIC.DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD,
                            sicOptions.CompressMSMSSpectraData,
                            msDataResolution,
                            keepRawSpectrum);
                    }
                    else
                    {
                        newFragScan.IonCount = 0;
                        newFragScan.IonCountRaw = 0;
                        newFragScan.TotalIonIntensity = 0;
                    }

                    mParentIonProcessor.AddUpdateParentIons(scanList, lastSurveyScanIndex, spectrumInfo.ParentIonMZ,
                                                            scanList.FragScans.Count - 1, spectraCache, sicOptions);

                    // Note: We need to take msScanCount * 2, in addition to adding msScanCount to lastSurveyScanIndex, since we have to read two different files
                    if (msScanCount > 1)
                    {
                        UpdateProgress((short)((lastSurveyScanIndex + msScanCount) / (double)(msScanCount * 2 - 1) * 100));
                    }
                    else
                    {
                        UpdateProgress(0);
                    }

                    UpdateCacheStats(spectraCache);
                    if (mOptions.AbortProcessing)
                    {
                        scanList.ProcessingIncomplete = true;
                        break;
                    }

                    if (scanList.FragScans.Count % 100 == 0)
                    {
                        ReportMessage("Reading MSMS scan index: " + scanList.FragScans.Count);
                        Console.Write(".");
                    }
                }
                while (true);

                // Record the current memory usage (before we close the .MGF file)
                OnUpdateMemoryUsage();

                mgfReader.CloseFile();

                // Check for any other survey scans that need to be added to MasterScanOrder

                // See if lastSurveyScanIndex needs to be updated
                // At the same time, populate .MasterScanOrder
                while (lastSurveyScanIndex < scanList.SurveyScans.Count - 1)
                {
                    lastSurveyScanIndex++;

                    // Note that scanTime is the scan time of the most recent survey scan processed in the above Do loop, so it's not accurate
                    if (mScanTracking.CheckScanInRange(scanList.SurveyScans[lastSurveyScanIndex].ScanNumber, scanTime, sicOptions))
                    {
                        // Add the given SurveyScan to .MasterScanOrder, though only if it hasn't yet been added
                        if (!surveyScansRecorded.Contains(lastSurveyScanIndex))
                        {
                            surveyScansRecorded.Add(lastSurveyScanIndex);

                            scanList.AddMasterScanEntry(clsScanList.ScanTypeConstants.SurveyScan, lastSurveyScanIndex);
                        }
                    }
                }

                // Make sure that MasterScanOrder really is sorted by scan number
                ValidateMasterScanOrderSorting(scanList);

                // Now that all of the data has been read, write out to the scan stats file, in order of scan number
                for (var scanIndex = 0; scanIndex < scanList.MasterScanOrderCount; scanIndex++)
                {
                    var eScanType = scanList.MasterScanOrder[scanIndex].ScanType;
                    clsScanInfo currentScan;

                    if (eScanType == clsScanList.ScanTypeConstants.SurveyScan)
                    {
                        // Survey scan
                        currentScan = scanList.SurveyScans[scanList.MasterScanOrder[scanIndex].ScanIndexPointer];
                    }
                    else
                    {
                        // Frag Scan
                        currentScan = scanList.FragScans[scanList.MasterScanOrder[scanIndex].ScanIndexPointer];
                    }

                    SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats, eScanType, currentScan, sicOptions.DatasetID);
                }

                Console.WriteLine();

                scanList.SetListCapacityToCount();
                mScanTracking.SetListCapacityToCount();

                return success;
            }
            catch (Exception ex)
            {
                // ReSharper disable once StringLiteralTypo
                ReportError("Error in ExtractScanInfoFromMGFandCDF", ex, clsMASIC.MasicErrorCodes.InputFileDataReadError);
                return false;
            }
        }

        private double FindBasePeakIon(
            IReadOnlyList<double> mzList,
            IReadOnlyList<double> ionIntensity,
            out double basePeakIonIntensity,
            out double mzMin,
            out double mzMax)
        {
            // Finds the base peak ion
            // Also determines the minimum and maximum m/z values in mzList

            mzMin = 0;
            mzMax = 0;

            try
            {
                mzMin = mzList[0];
                mzMax = mzList[0];

                var basePeakIndex = 0;
                for (var dataIndex = 0; dataIndex < mzList.Count; dataIndex++)
                {
                    if (ionIntensity[dataIndex] > ionIntensity[basePeakIndex])
                    {
                        basePeakIndex = dataIndex;
                    }

                    if (mzList[dataIndex] < mzMin)
                    {
                        mzMin = mzList[dataIndex];
                    }

                    if (mzList[dataIndex] > mzMax)
                    {
                        mzMax = mzList[dataIndex];
                    }
                }

                basePeakIonIntensity = ionIntensity[basePeakIndex];
                return mzList[basePeakIndex];
            }
            catch (Exception ex)
            {
                ReportError("Error in FindBasePeakIon", ex);
                basePeakIonIntensity = 0;
                return 0;
            }
        }

        /// <summary>
        /// Examine the scan numbers in surveyScans, starting at lastSurveyScanIndex, to find the survey scans on either side of fragScanNumber
        /// Interpolate the retention time that corresponds to fragScanNumber
        /// Determine fragScanNumber, which is generally 1, 2, or 3, indicating if this is the 1st, 2nd, or 3rd MS/MS scan after the survey scan
        /// </summary>
        /// <param name="surveyScans"></param>
        /// <param name="lastSurveyScanIndex"></param>
        /// <param name="fragScanNumber"></param>
        /// <param name="fragScanIteration"></param>
        /// <returns>Closest elution time</returns>
        // ReSharper disable once IdentifierTypo
        private float InterpolateRTandFragScanNumber(
            IList<clsScanInfo> surveyScans,
            int lastSurveyScanIndex,
            int fragScanNumber,
            out int fragScanIteration)
        {
            float elutionTime = 0;

            fragScanIteration = 1;

            try
            {
                // Decrement lastSurveyScanIndex if the corresponding SurveyScan's scan number is larger than fragScanNumber
                while (lastSurveyScanIndex > 0 && surveyScans[lastSurveyScanIndex].ScanNumber > fragScanNumber)
                    // This code will generally not be reached, provided the calling function passed the correct lastSurveyScanIndex value to this function
                    lastSurveyScanIndex--;

                // Increment lastSurveyScanIndex if the next SurveyScan's scan number is smaller than fragScanNumber
                while (lastSurveyScanIndex < surveyScans.Count - 1 && surveyScans[lastSurveyScanIndex + 1].ScanNumber < fragScanNumber)
                    // This code will generally not be reached, provided the calling function passed the correct lastSurveyScanIndex value to this function
                    lastSurveyScanIndex++;

                if (lastSurveyScanIndex >= surveyScans.Count - 1)
                {
                    // Cannot easily interpolate since FragScanNumber is greater than the last survey scan number
                    if (surveyScans.Count > 0)
                    {
                        if (surveyScans.Count >= 2)
                        {
                            // Use the scan numbers of the last 2 survey scans to extrapolate the scan number for this fragmentation scan

                            lastSurveyScanIndex = surveyScans.Count - 1;

                            var surveyScan = surveyScans[lastSurveyScanIndex];
                            var scanDiff = surveyScan.ScanNumber - surveyScans[lastSurveyScanIndex - 1].ScanNumber;
                            var prevScanElutionTime = surveyScans[lastSurveyScanIndex - 1].ScanTime;

                            // Compute fragScanIteration
                            fragScanIteration = fragScanNumber - surveyScan.ScanNumber;

                            if (scanDiff > 0 && fragScanIteration > 0)
                            {
                                elutionTime = (float)(surveyScan.ScanTime + fragScanIteration / (double)scanDiff * (surveyScan.ScanTime - prevScanElutionTime));
                            }
                            else
                            {
                                // Adjacent survey scans have the same scan number
                                // This shouldn't happen
                                elutionTime = surveyScans[lastSurveyScanIndex].ScanTime;
                            }

                            if (fragScanIteration < 1)
                                fragScanIteration = 1;
                        }
                        else
                        {
                            // Use the scan time of the highest survey scan in memory
                            elutionTime = surveyScans[surveyScans.Count - 1].ScanTime;
                        }
                    }
                    else
                    {
                        elutionTime = 0;
                    }
                }
                else
                {
                    // Interpolate retention time
                    var surveyScan = surveyScans[lastSurveyScanIndex];
                    var scanDiff = surveyScans[lastSurveyScanIndex + 1].ScanNumber - surveyScan.ScanNumber;
                    var nextScanElutionTime = surveyScans[lastSurveyScanIndex + 1].ScanTime;

                    // Compute fragScanIteration
                    fragScanIteration = fragScanNumber - surveyScan.ScanNumber;

                    if (scanDiff > 0 && fragScanIteration > 0)
                    {
                        elutionTime = (float)(surveyScan.ScanTime + fragScanIteration / (double)scanDiff * (nextScanElutionTime - surveyScan.ScanTime));
                    }
                    else
                    {
                        // Adjacent survey scans have the same scan number
                        // This shouldn't happen
                        elutionTime = surveyScan.ScanTime;
                    }

                    if (fragScanIteration < 1)
                        fragScanIteration = 1;
                }
            }
            catch (Exception ex)
            {
                // Report this error, but otherwise ignore it
                // ReSharper disable once StringLiteralTypo
                ReportError("Error in InterpolateRTandFragScanNumber", ex);
            }

            return elutionTime;
        }

        /// <summary>
        /// Validate that .MasterScanOrder() really is sorted by scan number
        /// </summary>
        /// <param name="scanList"></param>
        private void ValidateMasterScanOrderSorting(clsScanList scanList)
        {
            // Cannot use an IComparer because .MasterScanOrder points into other arrays

            var masterScanNumbers = new int[scanList.MasterScanOrderCount];
            var masterScanOrderIndices = new int[scanList.MasterScanOrderCount];

            for (var index = 0; index < scanList.MasterScanOrderCount; index++)
            {
                masterScanNumbers[index] = scanList.MasterScanNumList[index];
                masterScanOrderIndices[index] = index;
            }

            // Sort masterScanNumbers ascending, sorting the scan order indices array in parallel
            Array.Sort(masterScanNumbers, masterScanOrderIndices);

            // Check whether we need to re-populate the lists
            var needToSort = false;
            for (var index = 1; index < scanList.MasterScanOrderCount; index++)
            {
                if (masterScanOrderIndices[index] < masterScanOrderIndices[index - 1])
                {
                    needToSort = true;
                    break;
                }
            }

            if (needToSort)
            {
                // Reorder .MasterScanOrder, .MasterScanNumList, and .MasterScanTimeList

                var udtMasterScanOrderListCopy = new clsScanList.ScanOrderPointerType[scanList.MasterScanOrder.Count];
                var masterScanTimeListCopy = new float[scanList.MasterScanOrder.Count];

                Array.Copy(scanList.MasterScanOrder.ToArray(), udtMasterScanOrderListCopy, scanList.MasterScanOrderCount);
                Array.Copy(scanList.MasterScanTimeList.ToArray(), masterScanTimeListCopy, scanList.MasterScanOrderCount);

                for (var index = 0; index < scanList.MasterScanOrderCount; index++)
                {
                    scanList.MasterScanOrder[index] = udtMasterScanOrderListCopy[masterScanOrderIndices[index]];
                    scanList.MasterScanNumList[index] = masterScanNumbers[index];
                    scanList.MasterScanTimeList[index] = masterScanTimeListCopy[masterScanOrderIndices[index]];
                }
            }
        }
    }
}
