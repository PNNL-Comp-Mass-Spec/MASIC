using System;
using System.Collections.Generic;
using System.IO;
using MASIC.DataOutput;
using MASIC.Options;
using PRISM;
using ThermoRawFileReader;

namespace MASIC
{
    public class clsReporterIonProcessor : clsMasicEventNotifier
    {
        #region "Constants"

        public const string REPORTER_ION_COLUMN_PREFIX = "Ion_";

        #endregion

        #region "Class wide variables"

        private readonly MASICOptions mOptions;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="masicOptions"></param>
        public clsReporterIonProcessor(MASICOptions masicOptions)
        {
            mOptions = masicOptions;
        }

        /// <summary>
        /// Looks for the reporter ion peaks using FindReporterIonsWork
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="spectraCache"></param>
        /// <param name="inputFilePathFull">Full path to the input file</param>
        /// <param name="outputDirectoryPath"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool FindReporterIons(
            clsScanList scanList,
            clsSpectraCache spectraCache,
            string inputFilePathFull,
            string outputDirectoryPath)
        {
            const char TAB_DELIMITER = '\t';

            var outputFilePath = "??";

            try
            {
                // Use Xraw to read the .Raw files
                var readerOptions = new ThermoReaderOptions()
                {
                    LoadMSMethodInfo = false,
                    LoadMSTuneInfo = false
                };

                var rawFileReader = new XRawFileIO(readerOptions);
                RegisterEvents(rawFileReader);

                var includeFtmsColumns = false;

                if (inputFilePathFull.EndsWith(DataInput.clsDataImport.THERMO_RAW_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
                {
                    // Processing a thermo .Raw file
                    // Check whether any of the frag scans has IsFTMS true
                    for (var masterOrderIndex = 0; masterOrderIndex < scanList.MasterScanOrderCount; masterOrderIndex++)
                    {
                        var scanPointer = scanList.MasterScanOrder[masterOrderIndex].ScanIndexPointer;
                        if (scanList.MasterScanOrder[masterOrderIndex].ScanType == clsScanList.ScanTypeConstants.SurveyScan)
                        {
                            // Skip survey scans
                            continue;
                        }

                        if (scanList.FragScans[scanPointer].IsFTMS)
                        {
                            includeFtmsColumns = true;
                            break;
                        }
                    }

                    if (includeFtmsColumns)
                    {
                        rawFileReader.OpenRawFile(inputFilePathFull);
                    }
                }

                if (mOptions.ReporterIons.ReporterIonList.Count == 0)
                {
                    // No reporter ions defined; default to ITraq
                    mOptions.ReporterIons.SetReporterIonMassMode(clsReporterIons.ReporterIonMassModeConstants.ITraqFourMZ);
                }

                // Populate array reporterIons, which we will sort by m/z
                var reporterIons = new clsReporterIonInfo[mOptions.ReporterIons.ReporterIonList.Count];

                var index = 0;
                foreach (var reporterIon in mOptions.ReporterIons.ReporterIonList)
                {
                    reporterIons[index] = reporterIon;
                    index++;
                }

                Array.Sort(reporterIons, new clsReportIonInfoComparer());

                outputFilePath = clsDataOutput.ConstructOutputFilePath(
                    Path.GetFileName(inputFilePathFull),
                    outputDirectoryPath,
                    clsDataOutput.OutputFileTypeConstants.ReporterIonsFile);

                using var writer = new StreamWriter(outputFilePath);

                // Write the file headers
                var reporterIonMZsUnique = new SortedSet<string>();
                var headerColumns = new List<string>(7 + reporterIons.Length + 1)
                {
                    "Dataset",
                    "ScanNumber",
                    "Collision Mode",
                    "ParentIonMZ",
                    "BasePeakIntensity",
                    "BasePeakMZ",
                    "ReporterIonIntensityMax"
                };

                var obsMZHeaders = new List<string>(reporterIons.Length);
                var uncorrectedIntensityHeaders = new List<string>(reporterIons.Length);
                var ftmsSignalToNoise = new List<string>(reporterIons.Length);
                var ftmsResolution = new List<string>(reporterIons.Length);
                //var ftmsLabelDataMz = new List<string>(reporterIons.Length);

                var saveUncorrectedIntensities =
                    mOptions.ReporterIons.ReporterIonApplyAbundanceCorrection && mOptions.ReporterIons.ReporterIonSaveUncorrectedIntensities;

                var dataAggregation = new clsDataAggregation();
                RegisterEvents(dataAggregation);

                for (var reporterIonIndex= 0; reporterIonIndex < reporterIons.Length; reporterIonIndex++)
                {
                    var reporterIon = reporterIons[reporterIonIndex];

                    if (reporterIon.ContaminantIon && !saveUncorrectedIntensities)
                        continue;

                    // Construct the reporter ion intensity header
                    // We skip contaminant ions, unless saveUncorrectedIntensities is True, then we include them

                    string mzValue;
                    if (mOptions.ReporterIons.ReporterIonMassMode == clsReporterIons.ReporterIonMassModeConstants.TMTTenMZ ||
                        mOptions.ReporterIons.ReporterIonMassMode == clsReporterIons.ReporterIonMassModeConstants.TMTElevenMZ ||
                        mOptions.ReporterIons.ReporterIonMassMode == clsReporterIons.ReporterIonMassModeConstants.TMTSixteenMZ)
                    {
                        mzValue = reporterIon.MZ.ToString("#0.000");
                    }
                    else
                    {
                        mzValue = ((int)Math.Round(reporterIon.MZ, 0)).ToString();
                    }

                    if (reporterIonMZsUnique.Contains(mzValue))
                    {
                        // Uniquify the m/z value
                        mzValue += "_" + reporterIonIndex;
                    }

                    try
                    {
                        reporterIonMZsUnique.Add(mzValue);
                    }
                    catch (Exception)
                    {
                        // Error updating the SortedSet;
                        // this shouldn't happen based on the .ContainsKey test above
                    }

                    // Append the reporter ion intensity title to the headers
                    headerColumns.Add(REPORTER_ION_COLUMN_PREFIX + mzValue);

                    // This string will only be included in the header line if mOptions.ReporterIons.ReporterIonSaveObservedMasses is true
                    obsMZHeaders.Add(REPORTER_ION_COLUMN_PREFIX + mzValue + "_ObsMZ");

                    // This string will be included in the header line if saveUncorrectedIntensities is true
                    uncorrectedIntensityHeaders.Add(REPORTER_ION_COLUMN_PREFIX + mzValue + "_OriginalIntensity");

                    // This string will be included in the header line if includeFtmsColumns is true
                    ftmsSignalToNoise.Add(REPORTER_ION_COLUMN_PREFIX + mzValue + "_SignalToNoise");
                    ftmsResolution.Add(REPORTER_ION_COLUMN_PREFIX + mzValue + "_Resolution");

                    // Uncomment to include the label data m/z value in the _ReporterIons.txt file
                    // This string will only be included in the header line if mOptions.ReporterIons.ReporterIonSaveObservedMasses is true
                    //ftmsLabelDataMz.Add(REPORTER_ION_COLUMN_PREFIX + mzValue + "_LabelDataMZ");
                }

                headerColumns.Add("Weighted Avg Pct Intensity Correction");

                if (mOptions.ReporterIons.ReporterIonSaveObservedMasses)
                {
                    headerColumns.AddRange(obsMZHeaders);
                }

                if (saveUncorrectedIntensities)
                {
                    headerColumns.AddRange(uncorrectedIntensityHeaders);
                }

                if (includeFtmsColumns)
                {
                    headerColumns.AddRange(ftmsSignalToNoise);
                    headerColumns.AddRange(ftmsResolution);
                    // Uncomment to include the label data m/z value in the _ReporterIons.txt file
                    // If mOptions.ReporterIons.ReporterIonSaveObservedMasses Then
                    // headerColumns.AddRange(ftmsLabelDataMz)
                    // End If
                }

                // Write the headers to the output file, separated by tabs
                writer.WriteLine(string.Join(TAB_DELIMITER.ToString(), headerColumns));

                UpdateProgress(0, "Searching for reporter ions");

                for (var masterOrderIndex = 0; masterOrderIndex < scanList.MasterScanOrderCount; masterOrderIndex++)
                {
                    var scanPointer = scanList.MasterScanOrder[masterOrderIndex].ScanIndexPointer;
                    if (scanList.MasterScanOrder[masterOrderIndex].ScanType == clsScanList.ScanTypeConstants.SurveyScan)
                    {
                        // Skip Survey Scans
                        continue;
                    }

                    FindReporterIonsWork(
                        rawFileReader,
                        dataAggregation,
                        includeFtmsColumns,
                        mOptions.SICOptions,
                        scanList,
                        spectraCache,
                        scanList.FragScans[scanPointer],
                        writer,
                        reporterIons,
                        TAB_DELIMITER,
                        saveUncorrectedIntensities,
                        mOptions.ReporterIons.ReporterIonSaveObservedMasses);

                    if (scanList.MasterScanOrderCount > 1)
                    {
                        UpdateProgress((short)(masterOrderIndex / (double)(scanList.MasterScanOrderCount - 1) * 100));
                    }
                    else
                    {
                        UpdateProgress(0);
                    }

                    UpdateCacheStats(spectraCache);
                    if (mOptions.AbortProcessing)
                    {
                        break;
                    }
                }

                if (includeFtmsColumns)
                {
                    // Close the handle to the data file
                    rawFileReader.CloseRawFile();
                }

                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error writing the reporter ions to: " + outputFilePath, ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
                return false;
            }
        }

        private readonly clsITraqIntensityCorrection intensityCorrector = new clsITraqIntensityCorrection(
            clsReporterIons.ReporterIonMassModeConstants.CustomOrNone,
            clsITraqIntensityCorrection.CorrectionFactorsiTRAQ4Plex.ABSciex);

        /// <summary>
        /// Looks for the reporter ion m/z values, +/- a tolerance
        /// Calls AggregateIonsInRange with returnMax = True, meaning we're reporting the maximum ion abundance for each reporter ion m/z
        /// </summary>
        /// <param name="rawFileReader"></param>
        /// <param name="dataAggregation"></param>
        /// <param name="includeFtmsColumns"></param>
        /// <param name="sicOptions"></param>
        /// <param name="scanList"></param>
        /// <param name="spectraCache"></param>
        /// <param name="currentScan"></param>
        /// <param name="writer"></param>
        /// <param name="reporterIons"></param>
        /// <param name="delimiter"></param>
        /// <param name="saveUncorrectedIntensities"></param>
        /// <param name="saveObservedMasses"></param>
        /// <remarks></remarks>
        private void FindReporterIonsWork(
            XRawFileIO rawFileReader,
            clsDataAggregation dataAggregation,
            bool includeFtmsColumns,
            SICOptions sicOptions,
            clsScanList scanList,
            clsSpectraCache spectraCache,
            clsScanInfo currentScan,
            TextWriter writer,
            IList<clsReporterIonInfo> reporterIons,
            char delimiter,
            bool saveUncorrectedIntensities,
            bool saveObservedMasses)
        {
            const bool USE_MAX_ABUNDANCE_IN_WINDOW = true;

            // The following will be a value between 0 and 100
            // Using Absolute Value of percent change to avoid averaging both negative and positive values
            double parentIonMZ;

            if (currentScan.FragScanInfo.ParentIonInfoIndex >= 0 && currentScan.FragScanInfo.ParentIonInfoIndex < scanList.ParentIons.Count)
            {
                parentIonMZ = scanList.ParentIons[currentScan.FragScanInfo.ParentIonInfoIndex].MZ;
            }
            else
            {
                parentIonMZ = 0;
            }

            if (!spectraCache.GetSpectrum(currentScan.ScanNumber, out var spectrum, true))
            {
                SetLocalErrorCode(clsMASIC.MasicErrorCodes.ErrorUncachingSpectrum);
                return;
            }

            // Initialize the arrays used to track the observed reporter ion values
            var reporterIntensities = new double[reporterIons.Count];
            var reporterIntensitiesCorrected = new double[reporterIons.Count];
            var closestMZ = new double[reporterIons.Count];

            // Initialize the output variables
            var dataColumns = new List<string>()
            {
                sicOptions.DatasetID.ToString(),
                currentScan.ScanNumber.ToString(),
                currentScan.FragScanInfo.CollisionMode,
                StringUtilities.DblToString(parentIonMZ, 2),
                StringUtilities.DblToString(currentScan.BasePeakIonIntensity, 2),
                StringUtilities.DblToString(currentScan.BasePeakIonMZ, 4)
            };

            var reporterIntensityList = new List<string>(reporterIons.Count);
            var obsMZList = new List<string>(reporterIons.Count);
            var uncorrectedIntensityList = new List<string>(reporterIons.Count);

            var ftmsSignalToNoise = new List<string>(reporterIons.Count);
            var ftmsResolution = new List<string>(reporterIons.Count);
            //var ftmsLabelDataMz = new List<string>(reporterIons.Count);

            double reporterIntensityMax = 0;

            // Find the reporter ion intensities
            // Also keep track of the closest m/z for each reporter ion
            // Note that we're using the maximum intensity in the range (not the sum)
            for (var reporterIonIndex = 0; reporterIonIndex < reporterIons.Count; reporterIonIndex++)
            {
                var ion = reporterIons[reporterIonIndex];
                // Search for the reporter ion MZ in this mass spectrum
                reporterIntensities[reporterIonIndex] = dataAggregation.AggregateIonsInRange(
                    spectrum,
                    ion.MZ,
                    ion.MZToleranceDa,
                    out _,
                    out closestMZ[reporterIonIndex],
                    USE_MAX_ABUNDANCE_IN_WINDOW);

                ion.SignalToNoise = 0;
                ion.Resolution = 0;
                ion.LabelDataMZ = 0;
            }

            if (includeFtmsColumns && currentScan.IsFTMS)
            {
                // Retrieve the label data for this spectrum

                rawFileReader.GetScanLabelData(currentScan.ScanNumber, out var ftLabelData);

                // Find each reporter ion in ftLabelData

                foreach (var reporterIon in reporterIons)
                {
                    var mzToFind = reporterIon.MZ;
                    var mzToleranceDa = reporterIon.MZToleranceDa;
                    var highestIntensity = 0.0;
                    var udtBestMatch = new FTLabelInfoType();
                    var matchFound = false;

                    foreach (var labelItem in ftLabelData)
                    {
                        // Compare labelItem.Mass (which is m/z of the ion in labelItem) to the m/z of the current reporter ion
                        if (Math.Abs(mzToFind - labelItem.Mass) > mzToleranceDa)
                        {
                            continue;
                        }

                        // m/z is within range
                        if (labelItem.Intensity > highestIntensity)
                        {
                            udtBestMatch = labelItem;
                            highestIntensity = labelItem.Intensity;
                            matchFound = true;
                        }
                    }

                    if (matchFound)
                    {
                        reporterIon.SignalToNoise = udtBestMatch.SignalToNoise;
                        reporterIon.Resolution = udtBestMatch.Resolution;
                        reporterIon.LabelDataMZ = udtBestMatch.Mass;
                    }
                }
            }

            // Populate reporterIntensitiesCorrected with the data in reporterIntensities
            Array.Copy(reporterIntensities, reporterIntensitiesCorrected, reporterIntensities.Length);
            if (mOptions.ReporterIons.ReporterIonApplyAbundanceCorrection)
            {
                if (mOptions.ReporterIons.ReporterIonMassMode == clsReporterIons.ReporterIonMassModeConstants.ITraqFourMZ ||
                    mOptions.ReporterIons.ReporterIonMassMode == clsReporterIons.ReporterIonMassModeConstants.ITraqEightMZHighRes ||
                    mOptions.ReporterIons.ReporterIonMassMode == clsReporterIons.ReporterIonMassModeConstants.ITraqEightMZLowRes ||
                    mOptions.ReporterIons.ReporterIonMassMode == clsReporterIons.ReporterIonMassModeConstants.TMTTenMZ ||
                    mOptions.ReporterIons.ReporterIonMassMode == clsReporterIons.ReporterIonMassModeConstants.TMTElevenMZ ||
                    mOptions.ReporterIons.ReporterIonMassMode == clsReporterIons.ReporterIonMassModeConstants.TMTSixteenMZ)
                {
                    // Correct the reporter ion intensities using the Reporter Ion Intensity Corrector class

                    if (intensityCorrector.ReporterIonMode != mOptions.ReporterIons.ReporterIonMassMode ||
                        intensityCorrector.ITraq4PlexCorrectionFactorType != mOptions.ReporterIons.ReporterIonITraq4PlexCorrectionFactorType)
                    {
                        intensityCorrector.UpdateReporterIonMode(
                            mOptions.ReporterIons.ReporterIonMassMode,
                            mOptions.ReporterIons.ReporterIonITraq4PlexCorrectionFactorType);
                    }

                    // Count the number of non-zero data points in reporterIntensitiesCorrected()
                    var positiveCount = 0;
                    for (var reporterIonIndex = 0; reporterIonIndex < reporterIons.Count; reporterIonIndex++)
                    {
                        if (reporterIntensitiesCorrected[reporterIonIndex] > 0)
                        {
                            positiveCount++;
                        }
                    }

                    // Apply the correction if 2 or more points are non-zero
                    if (positiveCount >= 2)
                    {
                        intensityCorrector.ApplyCorrection(reporterIntensitiesCorrected);
                    }
                }
            }

            // Now construct the string of intensity values, delimited by delimiter
            // Will also compute the percent change in intensities

            // Initialize the variables used to compute the weighted average percent change
            double pctChangeSum = 0;
            double originalIntensitySum = 0;

            for (var reporterIonIndex = 0; reporterIonIndex < reporterIons.Count; reporterIonIndex++)
            {
                if (!reporterIons[reporterIonIndex].ContaminantIon)
                {
                    // Update the PctChange variables and the IntensityMax variable only if this is not a Contaminant Ion

                    originalIntensitySum += reporterIntensities[reporterIonIndex];

                    if (reporterIntensities[reporterIonIndex] > 0)
                    {
                        // Compute the percent change, then update pctChangeSum
                        var pctChange =
                            (reporterIntensitiesCorrected[reporterIonIndex] - reporterIntensities[reporterIonIndex]) /
                            reporterIntensities[reporterIonIndex];

                        // Using Absolute Value here to prevent negative changes from cancelling out positive changes
                        pctChangeSum += Math.Abs(pctChange * reporterIntensities[reporterIonIndex]);
                    }

                    if (reporterIntensitiesCorrected[reporterIonIndex] > reporterIntensityMax)
                    {
                        reporterIntensityMax = reporterIntensitiesCorrected[reporterIonIndex];
                    }
                }

                if (!reporterIons[reporterIonIndex].ContaminantIon || saveUncorrectedIntensities)
                {
                    // Append the reporter ion intensity to reporterIntensityList
                    // We skip contaminant ions, unless saveUncorrectedIntensities is True, then we include them

                    reporterIntensityList.Add(StringUtilities.DblToString(reporterIntensitiesCorrected[reporterIonIndex], 2));

                    if (saveObservedMasses)
                    {
                        // Append the observed reporter mass value to obsMZList
                        obsMZList.Add(StringUtilities.DblToString(closestMZ[reporterIonIndex], 3));
                    }

                    if (saveUncorrectedIntensities)
                    {
                        // Append the original, uncorrected intensity value
                        uncorrectedIntensityList.Add(StringUtilities.DblToString(reporterIntensities[reporterIonIndex], 2));
                    }

                    if (includeFtmsColumns)
                    {
                        if (Math.Abs(reporterIons[reporterIonIndex].SignalToNoise) < float.Epsilon &&
                            Math.Abs(reporterIons[reporterIonIndex].Resolution) < float.Epsilon &&
                            Math.Abs(reporterIons[reporterIonIndex].LabelDataMZ) < float.Epsilon)
                        {
                            // A match was not found in the label data; display blanks (not zeroes)
                            ftmsSignalToNoise.Add(string.Empty);
                            ftmsResolution.Add(string.Empty);
                            //ftmsLabelDataMz.Add(string.Empty);
                        }
                        else
                        {
                            ftmsSignalToNoise.Add(StringUtilities.DblToString(reporterIons[reporterIonIndex].SignalToNoise, 2));
                            ftmsResolution.Add(StringUtilities.DblToString(reporterIons[reporterIonIndex].Resolution, 2));
                            //ftmsLabelDataMz.Add(StringUtilities.DblToString(reporterIons(reporterIonIndex).LabelDataMZ, 4));
                        }
                    }
                }
            }

            // Compute the weighted average percent intensity correction value
            float weightedAvgPctIntensityCorrection;
            if (originalIntensitySum > 0)
            {
                weightedAvgPctIntensityCorrection = (float)(pctChangeSum / originalIntensitySum * 100);
            }
            else
            {
                weightedAvgPctIntensityCorrection = 0;
            }

            // Resize the target list capacity to large enough to hold all data.
            dataColumns.Capacity = reporterIntensityList.Count + 3 + obsMZList.Count + uncorrectedIntensityList.Count +
                                   ftmsSignalToNoise.Count + ftmsResolution.Count;
            // Append the maximum reporter ion intensity then the individual reporter ion intensities
            dataColumns.Add(StringUtilities.DblToString(reporterIntensityMax, 2));
            dataColumns.AddRange(reporterIntensityList);

            // Append the weighted average percent intensity correction
            if (weightedAvgPctIntensityCorrection < float.Epsilon)
            {
                dataColumns.Add("0");
            }
            else
            {
                dataColumns.Add(StringUtilities.DblToString(weightedAvgPctIntensityCorrection, 1));
            }

            if (saveObservedMasses)
            {
                dataColumns.AddRange(obsMZList);
            }

            if (saveUncorrectedIntensities)
            {
                dataColumns.AddRange(uncorrectedIntensityList);
            }

            if (includeFtmsColumns)
            {
                dataColumns.AddRange(ftmsSignalToNoise);
                dataColumns.AddRange(ftmsResolution);

                // Uncomment to include the label data m/z value in the _ReporterIons.txt file
                //if (saveObservedMasses)
                //    dataColumns.AddRange(ftmsLabelDataMz)
            }

            writer.WriteLine(string.Join(delimiter.ToString(), dataColumns));
        }

        protected class clsReportIonInfoComparer : IComparer<clsReporterIonInfo>
        {
            public int Compare(clsReporterIonInfo x, clsReporterIonInfo y)
            {
                var reporterIonInfoA = x;
                var reporterIonInfoB = y;

                if (reporterIonInfoA == null || reporterIonInfoB == null)
                    return 0;

                if (reporterIonInfoA.MZ > reporterIonInfoB.MZ)
                {
                    return 1;
                }

                if (reporterIonInfoA.MZ < reporterIonInfoB.MZ)
                {
                    return -1;
                }

                return 0;
            }
        }
    }
}
