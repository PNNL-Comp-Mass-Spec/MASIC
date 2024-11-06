using System;
using System.Collections.Generic;
using System.IO;
using MASIC.Data;
using MASIC.Options;
using MathNet.Numerics.Statistics;
using PRISM;
using ThermoRawFileReader;

namespace MASIC
{
    /// <summary>
    /// Class for finding reporter ions
    /// </summary>
    public class ReporterIonProcessor : MasicEventNotifier
    {
        // Ignore Spelling: MASIC, plex, Uniquify

        /// <summary>
        /// Column prefix for reporter ion data in the output file
        /// </summary>
        public const string REPORTER_ION_COLUMN_PREFIX = "Ion_";

        private readonly MASICOptions mOptions;

        private int mUnsupportedCorrectionWarningCount;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="masicOptions">MASIC options</param>
        public ReporterIonProcessor(MASICOptions masicOptions)
        {
            mOptions = masicOptions;
            mUnsupportedCorrectionWarningCount = 0;
        }

        /// <summary>
        /// Look for MS3 spectra
        /// If found, possibly copy the reporter ion data to parent MS2 spectra
        /// </summary>
        /// <remarks>
        /// If mOptions.ReporterIons.AlwaysUseMS3ReporterIonsForParents is true, always copy
        /// If false (the default), examine reporter ions in the parent MS2 spectra and copy if the median intensity is less than the MS3 scan's median intensity
        /// </remarks>
        /// <param name="cachedDataToWrite"></param>
        private void CopyReporterIonsToParentIfRequired(SortedDictionary<int, ReporterIonStats> cachedDataToWrite)
        {
            foreach (var item in cachedDataToWrite)
            {
                if (item.Value.MSLevel <= 2 || item.Value.ParentScan == 0)
                    continue;

                var targetScan = 0;

                if (mOptions.ReporterIons.AlwaysUseMS3ReporterIonsForParents)
                {
                    targetScan = item.Value.ParentScan;
                }
                else
                {
                    // Compute the median reporter ion intensity for this MS3 scan (both using all values and ignoring missing values and zeros)

                    var reporterIonCount = item.Value.ReporterIonIntensityEndIndex - item.Value.ReporterIonIntensityStartIndex + 1;

                    var medianMS3Intensity = GetMedianReporterIonIntensity(
                        string.Format("MS3 scan {0}", item.Key),
                        item.Value.DataColumns.GetRange(item.Value.ReporterIonIntensityStartIndex, reporterIonCount),
                        out var medianMS3IntensityIgnoringZeros);

                    // Examine the parent ion intensities of the parent MS2 scan

                    // If the median value is less than medianMS3Intensity,
                    // copy the reporter ion intensities from the MS3 scan to the MS2 scan

                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    foreach (var candidateScan in cachedDataToWrite)
                    {
                        if (candidateScan.Key != item.Value.ParentScan)
                            continue;

                        var medianMS2Intensity = GetMedianReporterIonIntensity(
                            string.Format("MS2 scan {0}", candidateScan.Value.ScanNumber),
                            candidateScan.Value.DataColumns.GetRange(item.Value.ReporterIonIntensityStartIndex, reporterIonCount),
                            out var medianMS2IntensityIgnoringZeros);

                        // First compare the median based on all values, since scans with numerous reporter ion intensities that are small (or 0) will have a smaller median
                        if (medianMS2Intensity < medianMS3Intensity)
                        {
                            targetScan = item.Value.ParentScan;
                        }
                        else if (medianMS2IntensityIgnoringZeros < medianMS3IntensityIgnoringZeros)
                        {
                            // If both the MS2 scan and the MS3 scan have several reporter ions with an intensity of 0, the median for each will be 0
                            // Compare the median of the non-zero values
                            targetScan = item.Value.ParentScan;
                        }

                        break;
                    }
                }

                if (targetScan == 0)
                {
                    continue;
                }

                foreach (var candidateScan in cachedDataToWrite)
                {
                    if (candidateScan.Key != targetScan)
                        continue;

                    var targetScanStats = candidateScan.Value;

                    // Copy the ReporterIonIntensityMax value
                    targetScanStats.DataColumns[item.Value.ReporterIonIntensityStartIndex - 1] =
                        item.Value.DataColumns[item.Value.ReporterIonIntensityStartIndex - 1];

                    // Copy the reporter ion data (starting at column index ReporterIonIntensityStartIndex)
                    for (var i = item.Value.ReporterIonIntensityStartIndex; i < item.Value.DataColumns.Count; i++)
                    {
                        targetScanStats.DataColumns[i] = item.Value.DataColumns[i];
                    }
                }
            }
        }

        /// <summary>
        /// Looks for the reporter ion peaks using FindReporterIonsWork
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="spectraCache"></param>
        /// <param name="inputFilePathFull">Full path to the input file</param>
        /// <param name="outputDirectoryPath"></param>
        public bool FindReporterIons(
            ScanList scanList,
            SpectraCache spectraCache,
            string inputFilePathFull,
            string outputDirectoryPath)
        {
            const char TAB_DELIMITER = '\t';

            var outputFilePath = "??";

            try
            {
                // Use XRawFileIO to read the .Raw files
                var readerOptions = new ThermoReaderOptions
                {
                    LoadMSMethodInfo = false,
                    LoadMSTuneInfo = false
                };

                var rawFileReader = new XRawFileIO(readerOptions);
                RegisterEvents(rawFileReader);

                var includeFtmsColumns = false;

                if (inputFilePathFull.EndsWith(DataInput.DataImport.THERMO_RAW_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
                {
                    // Processing a Thermo .Raw file
                    // Check whether any of the fragmentation scans has IsHighResolution true
                    for (var masterOrderIndex = 0; masterOrderIndex < scanList.MasterScanOrderCount; masterOrderIndex++)
                    {
                        var scanPointer = scanList.MasterScanOrder[masterOrderIndex].ScanIndexPointer;

                        if (scanList.MasterScanOrder[masterOrderIndex].ScanType == ScanList.ScanTypeConstants.SurveyScan)
                        {
                            // Skip survey scans
                            continue;
                        }

                        if (scanList.FragScans[scanPointer].IsHighResolution)
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
                    // No reporter ions defined; default to 11-plex TMT
                    mOptions.ReporterIons.SetReporterIonMassMode(ReporterIons.ReporterIonMassModeConstants.TMTElevenMZ);
                }

                // Populate array reporterIons, which we will sort by IonNumber then m/z
                var reporterIons = new ReporterIonInfo[mOptions.ReporterIons.ReporterIonList.Count];

                var index = 0;

                foreach (var reporterIon in mOptions.ReporterIons.ReporterIonList)
                {
                    reporterIons[index] = reporterIon.Value;
                    index++;
                }

                Array.Sort(reporterIons, new ReportIonInfoComparer());

                outputFilePath = DataOutput.DataOutput.ConstructOutputFilePath(
                    Path.GetFileName(inputFilePathFull),
                    outputDirectoryPath,
                    DataOutput.DataOutput.OutputFileTypeConstants.ReporterIonsFile);

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
                    "ParentScan",
                    "ReporterIonIntensityMax"
                };

                var obsMZHeaders = new List<string>(reporterIons.Length);
                var uncorrectedIntensityHeaders = new List<string>(reporterIons.Length);
                var ftmsSignalToNoise = new List<string>(reporterIons.Length);
                var ftmsResolution = new List<string>(reporterIons.Length);
                //var ftmsLabelDataMz = new List<string>(reporterIons.Length);

                var saveUncorrectedIntensities =
                    mOptions.ReporterIons.ReporterIonApplyAbundanceCorrection && mOptions.ReporterIons.ReporterIonSaveUncorrectedIntensities;

                var dataAggregation = new DataAggregation();
                RegisterEvents(dataAggregation);

                for (var reporterIonIndex = 0; reporterIonIndex < reporterIons.Length; reporterIonIndex++)
                {
                    var reporterIon = reporterIons[reporterIonIndex];

                    if (reporterIon.ContaminantIon && !saveUncorrectedIntensities)
                        continue;

                    // Construct the reporter ion intensity header
                    // We skip contaminant ions, unless saveUncorrectedIntensities is True, in which case we include them

                    string mzValue;

                    if (mOptions.ReporterIons.ReporterIonMassMode is
                        ReporterIons.ReporterIonMassModeConstants.TMTTenMZ or
                        ReporterIons.ReporterIonMassModeConstants.TMTElevenMZ or
                        ReporterIons.ReporterIonMassModeConstants.TMTSixteenMZ or
                        ReporterIons.ReporterIonMassModeConstants.TMTEighteenMZ)
                    {
                        // Round to three decimal places
                        mzValue = reporterIon.MZ.ToString("#0.000");
                    }
                    else if (mOptions.ReporterIons.ReporterIonMassMode is
                             ReporterIons.ReporterIonMassModeConstants.TMT32MZ or
                             ReporterIons.ReporterIonMassModeConstants.TMT35MZ)
                    {
                        // Round to four decimal places
                        mzValue = reporterIon.MZ.ToString("#0.0000");
                    }
                    else
                    {
                        // Round to the nearest integer
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
                    // if (mOptions.ReporterIons.ReporterIonSaveObservedMasses)
                    // {
                    //     headerColumns.AddRange(ftmsLabelDataMz);
                    // }
                }

                // Write the headers to the output file, separated by tabs
                writer.WriteLine(string.Join(TAB_DELIMITER.ToString(), headerColumns));

                UpdateProgress(0, "Searching for reporter ions");

                // Keys in this dictionary are scan number, values are the data columns for each scan
                var cachedDataToWrite = new SortedDictionary<int, ReporterIonStats>();

                for (var masterOrderIndex = 0; masterOrderIndex < scanList.MasterScanOrderCount; masterOrderIndex++)
                {
                    var scanPointer = scanList.MasterScanOrder[masterOrderIndex].ScanIndexPointer;

                    if (scanList.MasterScanOrder[masterOrderIndex].ScanType == ScanList.ScanTypeConstants.SurveyScan)
                    {
                        // Survey scan; write the cached data, then move on to the next scan
                        if (cachedDataToWrite.Count > 0)
                        {
                            WriteCachedReporterIons(writer, cachedDataToWrite, TAB_DELIMITER);
                            cachedDataToWrite.Clear();
                        }

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
                        cachedDataToWrite,
                        reporterIons,
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

                if (cachedDataToWrite.Count > 0)
                {
                    WriteCachedReporterIons(writer, cachedDataToWrite, TAB_DELIMITER);
                    cachedDataToWrite.Clear();
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

        private void WriteCachedReporterIons(
            TextWriter writer,
            SortedDictionary<int, ReporterIonStats> cachedDataToWrite,
            char delimiter)
        {
            if (mOptions.ReporterIons.UseMS3ReporterIonsForParentMS2Spectra)
            {
                CopyReporterIonsToParentIfRequired(cachedDataToWrite);
            }

            foreach (var cachedLine in cachedDataToWrite)
            {
                writer.WriteLine(string.Join(delimiter.ToString(), cachedLine.Value.DataColumns));
            }
        }

        private readonly ITraqIntensityCorrection intensityCorrector = new(
            ReporterIons.ReporterIonMassModeConstants.CustomOrNone,
            ITraqIntensityCorrection.CorrectionFactorsITRAQ4Plex.ABSciex);

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
        /// <param name="cachedDataToWrite"></param>
        /// <param name="reporterIons"></param>
        /// <param name="saveUncorrectedIntensities"></param>
        /// <param name="saveObservedMasses"></param>
        private void FindReporterIonsWork(
            XRawFileIO rawFileReader,
            DataAggregation dataAggregation,
            bool includeFtmsColumns,
            SICOptions sicOptions,
            ScanList scanList,
            SpectraCache spectraCache,
            ScanInfo currentScan,
            IDictionary<int, ReporterIonStats> cachedDataToWrite,
            IList<ReporterIonInfo> reporterIons,
            bool saveUncorrectedIntensities,
            bool saveObservedMasses)
        {
            const bool USE_MAX_ABUNDANCE_IN_WINDOW = true;

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

            var reporterIonStats = new ReporterIonStats(currentScan.ScanNumber);

            // Initialize the output data
            reporterIonStats.DataColumns.Add(sicOptions.DatasetID.ToString());
            reporterIonStats.DataColumns.Add(currentScan.ScanNumber.ToString());
            reporterIonStats.DataColumns.Add(currentScan.FragScanInfo.CollisionMode);
            reporterIonStats.DataColumns.Add(StringUtilities.DblToString(parentIonMZ, 2));
            reporterIonStats.DataColumns.Add(StringUtilities.DblToString(currentScan.BasePeakIonIntensity, 2));
            reporterIonStats.DataColumns.Add(StringUtilities.DblToString(currentScan.BasePeakIonMZ, 4));

            reporterIonStats.MSLevel = currentScan.FragScanInfo.MSLevel;
            reporterIonStats.ParentIonMz = currentScan.FragScanInfo.ParentIonMz;

            if (reporterIonStats.MSLevel <= 2)
            {
                reporterIonStats.ParentScan = currentScan.FragScanInfo.ParentScan;

                // Note that the parent scan is not necessarily the most recent survey scan (i.e., most recent MS1 scan)
            }
            else
            {
                reporterIonStats.ParentScan = currentScan.FragScanInfo.ParentScan;
            }

            reporterIonStats.DataColumns.Add(reporterIonStats.ParentScan.ToString());

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

            if (includeFtmsColumns && currentScan.IsHighResolution)
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
                // Correct the reporter ion intensities using the Reporter Ion Intensity Corrector class

                switch (mOptions.ReporterIons.ReporterIonMassMode)
                {
                    case ReporterIons.ReporterIonMassModeConstants.TMT32MZ:
                    case ReporterIons.ReporterIonMassModeConstants.TMT35MZ:
                        mUnsupportedCorrectionWarningCount++;

                        if (mUnsupportedCorrectionWarningCount < 10)
                        {
                            OnWarningEvent("Reporter ion correction is not yet supported for 32-plex or 35-plex TMT (scan {0})", currentScan.ScanNumber);
                        }

                        break;

                    case ReporterIons.ReporterIonMassModeConstants.ITraqFourMZ:
                    case ReporterIons.ReporterIonMassModeConstants.ITraqEightMZHighRes:
                    case ReporterIons.ReporterIonMassModeConstants.ITraqEightMZLowRes:
                    case ReporterIons.ReporterIonMassModeConstants.TMTTenMZ:
                    case ReporterIons.ReporterIonMassModeConstants.TMTElevenMZ:
                    case ReporterIons.ReporterIonMassModeConstants.TMTSixteenMZ:
                    case ReporterIons.ReporterIonMassModeConstants.TMTEighteenMZ:

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

                        break;

                    case ReporterIons.ReporterIonMassModeConstants.CustomOrNone:
                    case ReporterIons.ReporterIonMassModeConstants.ITraqETDThreeMZ:
                    case ReporterIons.ReporterIonMassModeConstants.TMTTwoMZ:
                    case ReporterIons.ReporterIonMassModeConstants.TMTSixMZ:
                    case ReporterIons.ReporterIonMassModeConstants.PCGalnaz:
                    case ReporterIons.ReporterIonMassModeConstants.HemeCFragment:
                    case ReporterIons.ReporterIonMassModeConstants.LycAcetFragment:
                    case ReporterIons.ReporterIonMassModeConstants.OGlcNAc:
                    case ReporterIons.ReporterIonMassModeConstants.FrackingAmine20160217:
                    case ReporterIons.ReporterIonMassModeConstants.FSFACustomCarbonyl:
                    case ReporterIons.ReporterIonMassModeConstants.FSFACustomCarboxylic:
                    case ReporterIons.ReporterIonMassModeConstants.FSFACustomHydroxyl:
                    case ReporterIons.ReporterIonMassModeConstants.Acetylation:
                    case ReporterIons.ReporterIonMassModeConstants.NativeOGlcNAc:
                    default:
                        // Reporter ion correction is not supported for these reporter ion mass modes
                        break;
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
                        // Compute the percent change, update pctChangeSum
                        var pctChange =
                            (reporterIntensitiesCorrected[reporterIonIndex] - reporterIntensities[reporterIonIndex]) /
                            reporterIntensities[reporterIonIndex];

                        // Using Absolute Value here to prevent negative changes from canceling out positive changes
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
                    // We skip contaminant ions, unless saveUncorrectedIntensities is True, in which case we include them

                    reporterIntensityList.Add(
                        reporterIntensitiesCorrected[reporterIonIndex] < float.Epsilon
                            ? "0"
                            : StringUtilities.DblToString(reporterIntensitiesCorrected[reporterIonIndex], 2));

                    if (saveObservedMasses)
                    {
                        // Append the observed reporter mass value to obsMZList
                        obsMZList.Add(StringUtilities.DblToString(closestMZ[reporterIonIndex], 3));
                    }

                    if (saveUncorrectedIntensities)
                    {
                        // Append the original, uncorrected intensity value
                        uncorrectedIntensityList.Add(
                            reporterIntensities[reporterIonIndex] < float.Epsilon
                                ? "0"
                                : StringUtilities.DblToString(reporterIntensities[reporterIonIndex], 2));
                    }

                    if (!includeFtmsColumns)
                        continue;

                    if (Math.Abs(reporterIons[reporterIonIndex].SignalToNoise) < float.Epsilon &&
                        Math.Abs(reporterIons[reporterIonIndex].Resolution) < float.Epsilon &&
                        Math.Abs(reporterIons[reporterIonIndex].LabelDataMZ) < float.Epsilon)
                    {
                        // A match was not found in the label data; display blanks (not zeros)
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

            // Compute the weighted average percent intensity correction value
            // This will be a value between 0 and 100

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
            reporterIonStats.DataColumns.Capacity =
                reporterIntensityList.Count + 3 +
                obsMZList.Count + uncorrectedIntensityList.Count +
                ftmsSignalToNoise.Count + ftmsResolution.Count;

            // Append the maximum reporter ion intensity, then the individual reporter ion intensities
            reporterIonStats.DataColumns.Add(StringUtilities.DblToString(reporterIntensityMax, 2));

            reporterIonStats.ReporterIonIntensityStartIndex = reporterIonStats.DataColumns.Count;
            reporterIonStats.ReporterIonIntensityEndIndex = reporterIonStats.ReporterIonIntensityStartIndex + reporterIntensityList.Count - 1;

            reporterIonStats.DataColumns.AddRange(reporterIntensityList);

            // Append the weighted average percent intensity correction
            reporterIonStats.DataColumns.Add(
                weightedAvgPctIntensityCorrection < float.Epsilon
                    ? "0"
                    : StringUtilities.DblToString(weightedAvgPctIntensityCorrection, 1));

            if (saveObservedMasses)
            {
                reporterIonStats.DataColumns.AddRange(obsMZList);
            }

            if (saveUncorrectedIntensities)
            {
                reporterIonStats.DataColumns.AddRange(uncorrectedIntensityList);
            }

            if (includeFtmsColumns)
            {
                reporterIonStats.DataColumns.AddRange(ftmsSignalToNoise);
                reporterIonStats.DataColumns.AddRange(ftmsResolution);

                // Uncomment to include the label data m/z value in the _ReporterIons.txt file
                //if (saveObservedMasses)
                //    reporterIonStats.DataColumns.AddRange(ftmsLabelDataMz)
            }

            cachedDataToWrite.Add(currentScan.ScanNumber, reporterIonStats);
        }

        /// <summary>
        /// Compute the median reporter ion value
        /// </summary>
        /// <param name="scanDescription"></param>
        /// <param name="reporterIonIntensities"></param>
        /// <param name="medianIgnoringZeros">Median value, ignoring zeros</param>
        /// <returns>Median value, including zeros</returns>
        private double GetMedianReporterIonIntensity(string scanDescription, List<string> reporterIonIntensities, out double medianIgnoringZeros)
        {
            var reporterIonIntensityValues = new List<double>();
            var nonZeroIntensities = new List<double>();

            var validNumbers = 0;

            foreach (var item in reporterIonIntensities)
            {
                if (!double.TryParse(item, out var reporterIonIntensity))
                {
                    reporterIonIntensityValues.Add(0);
                    continue;
                }

                validNumbers++;

                reporterIonIntensityValues.Add(reporterIonIntensity);

                if (!double.IsNaN(reporterIonIntensity) && reporterIonIntensity > 0)
                    nonZeroIntensities.Add(reporterIonIntensity);
            }

            if (validNumbers == 0)
            {
                OnWarningEvent("No valid reporter ions were found in {0}", scanDescription);
                medianIgnoringZeros = 0;
                return 0;
            }

            medianIgnoringZeros = GetMedianValue(scanDescription + " (non-zero values)", nonZeroIntensities);

            return GetMedianValue(scanDescription + " (all values)", reporterIonIntensityValues);
        }

        /// <summary>
        /// Compute the median value for a list of doubles
        /// </summary>
        /// <param name="scanDescription"></param>
        /// <param name="values"></param>
        /// <returns>Median value, or 0 if an empty list or the computed median is NaN</returns>
        private double GetMedianValue(string scanDescription, IReadOnlyCollection<double> values)
        {
            if (values.Count == 0)
                return 0;

            var medianValue = values.Median();

            if (!double.IsNaN(medianValue))
                return medianValue;

            OnWarningEvent("Median reporter ion intensity is NaN for {0}", scanDescription);
            return 0;
        }

        /// <summary>
        /// Reporter ion info comparison class
        /// </summary>
        /// <remarks>Compares m/z values</remarks>
        protected class ReportIonInfoComparer : IComparer<ReporterIonInfo>
        {
            /// <summary>
            /// Compare the Ion Number and m/z values of two reporter ions
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns>-1, 0, or 1</returns>
            public int Compare(ReporterIonInfo x, ReporterIonInfo y)
            {
                var reporterIonInfoA = x;
                var reporterIonInfoB = y;

                if (reporterIonInfoA == null || reporterIonInfoB == null)
                    return 0;

                if (reporterIonInfoA.IonNumber > reporterIonInfoB.IonNumber)
                {
                    return 1;
                }

                if (reporterIonInfoA.IonNumber < reporterIonInfoB.IonNumber)
                {
                    return -1;
                }

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
