using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MASIC.Options;
using MASICPeakFinder;
using PRISM;
using DbUtils = PRISMDatabaseUtils.DataTableUtils;

namespace MASIC.DataOutput
{
    public class StatsSummarizer : EventNotifier
    {

        #region "Constants and Enums"

        private const string UNDEFINED_UNITS = "Undefined Units";

        private enum ReporterIonsColumns
        {
            DatasetID = 0,
            ScanNumber = 1,
            CollisionMode = 2,
            ParentIonMZ = 3,
            BasePeakIntensity = 4,
            BasePeakMZ = 5,
            ReporterIonIntensityMax = 6,
            WeightedAvgPctIntensityCorrection = 7
        }

        private enum ScanStatsColumns
        {
            DatasetID = 0,
            ScanNumber = 1,
            ScanTime = 2,
            ScanType = 3,
            TotalIonIntensity = 4,
            BasePeakIntensity = 5,
            BasePeakMZ = 6,
            BasePeakSignalToNoiseRatio = 7,
            IonCount = 8,
            IonCountRaw = 9,
            ScanTypeName = 10
        }

        private enum SICStatsColumns
        {
            DatasetID = 0,
            ParentIonIndex = 1,
            Mz = 2,
            SurveyScanNumber = 3,
            FragScanNumber = 4,
            OptimalPeakApexScanNumber = 5,
            PeakSignalToNoiseRatio = 6,
            FWHMinScans = 7,
            PeakArea = 8,
            ParentIonIntensity = 9
        }

        #endregion

        #region "Member variables"

        /// <summary>
        /// Parent ions loaded from the SICStats file
        /// </summary>
        /// <remarks>Keys are parent ion index, values are parent ion info</remarks>
        private readonly Dictionary<int, clsParentIonInfo> mParentIons;

        /// <summary>
        /// Scan data loaded from the ScanStats file
        /// </summary>
        /// <remarks>Keys are scan number, values are scan info</remarks>
        private readonly Dictionary<int, clsScanInfo> mScanList;

        /// <summary>
        /// Reporter ion columns in the ReporterIons file
        /// </summary>
        /// <remarks>Keys are column index, values are reporter ion info</remarks>
        private readonly Dictionary<int, clsReporterIonInfo> mReporterIonInfo;

        /// <summary>
        /// Reporter ion abundance data in the ReporterIons file
        /// </summary>
        /// <remarks>
        /// Keys are column index (corresponding to keys in <see cref="mReporterIonInfo"/>)
        /// Values are a dictionary of scan number and abundance
        /// </remarks>
        private readonly Dictionary<int, Dictionary<int, double>> mReporterIonAbundances;

        #endregion

        #region "Properties"

        /// <summary>
        /// MASIC Options
        /// </summary>
        public MASICOptions Options { get; }

        /// <summary>
        /// Peak area histogram
        /// </summary>
        /// <remarks>Keys are base-10 log for this bin, values are bin counts</remarks>
        public Dictionary<float, int> PeakAreaHistogram { get; }

        /// <summary>
        /// Peak width histogram
        /// </summary>
        /// <remarks>Keys are either peak width in seconds or scan number, values are bin counts</remarks>
        public Dictionary<float, int> PeakWidthHistogram { get; }

        /// <summary>
        /// X axis units for the peak width histogram
        /// </summary>
        public string PeakWidthHistogramUnits { get; private set; }

        /// <summary>
        /// Non-zero reporter ion values, by channel
        /// </summary>
        /// <remarks>
        /// Keys are column index (corresponding to <see cref="ReporterIonNames"/> and <see cref="mReporterIonInfo"/>)
        /// Values are reporter ion intensities
        /// </remarks>
        public Dictionary<int, List<double>> NonZeroReporterIons { get; }

        /// <summary>
        /// Non-zero reporter ion values, by channel, using data from the top N percent of the data (sorted by descending peak area)
        /// </summary>
        /// <remarks>
        /// Keys are column index (corresponding to <see cref="ReporterIonNames"/> and <see cref="mReporterIonInfo"/>)
        /// Values are reporter ion intensities
        /// </remarks>
        public Dictionary<int, List<double>> NonZeroReporterIonsHighAbundance { get; }

        /// <summary>
        /// Reporter ion column names
        /// </summary>
        /// <remarks>Keys are column index, values are reporter ion info</remarks>
        public Dictionary<int, string> ReporterIonNames { get; }

        /// <summary>
        /// Reporter ion observation rate, by channel
        /// </summary>
        /// <remarks>
        /// Keys are column index (corresponding to <see cref="ReporterIonNames"/> and <see cref="mReporterIonInfo"/>)
        /// Values the observation rate (value between 0 and 100)
        /// </remarks>
        public Dictionary<int, float> ReporterIonObservationRate { get; }

        /// <summary>
        /// Reporter ion observation rate, by channel, using data from the top N percent of the data (sorted by descending peak area)
        /// </summary>
        /// <remarks>
        /// Keys are column index (corresponding to <see cref="ReporterIonNames"/> and <see cref="mReporterIonInfo"/>)
        /// Values are the observation rate (value between 0 and 100)
        /// </remarks>
        public Dictionary<int, float> ReporterIonObservationRateHighAbundance { get; }

        /// <summary>
        /// Box plot statistics for each reporter ion channel
        /// </summary>
        /// Keys are column index (corresponding to <see cref="ReporterIonNames"/> and <see cref="mReporterIonInfo"/>)
        /// Values are box plot stats, by channel
        public Dictionary<int, BoxPlotStats> ReporterIonIntensityStats { get; set; }

        /// <summary>
        /// Box plot statistics for each reporter ion channel, using data from the top N percent of the data (sorted by descending peak area)
        /// </summary>
        /// Keys are column index (corresponding to <see cref="ReporterIonNames"/> and <see cref="mReporterIonInfo"/>)
        /// Values are box plot stats, by channel
        public Dictionary<int, BoxPlotStats> ReporterIonIntensityStatsHighAbundanceData { get; set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options">MASIC Options</param>
        public StatsSummarizer(MASICOptions options)
        {
            Options = options;

            mScanList = new Dictionary<int, clsScanInfo>();
            mParentIons = new Dictionary<int, clsParentIonInfo>();

            mReporterIonInfo = new Dictionary<int, clsReporterIonInfo>();
            mReporterIonAbundances = new Dictionary<int, Dictionary<int, double>>();

            PeakAreaHistogram = new Dictionary<float, int>();

            PeakWidthHistogram = new Dictionary<float, int>();
            PeakWidthHistogramUnits = UNDEFINED_UNITS;

            ReporterIonNames = new Dictionary<int, string>();

            NonZeroReporterIons = new Dictionary<int, List<double>>();
            NonZeroReporterIonsHighAbundance = new Dictionary<int, List<double>>();

            ReporterIonObservationRate = new Dictionary<int, float>();
            ReporterIonObservationRateHighAbundance = new Dictionary<int, float>();

            ReporterIonIntensityStats = new Dictionary<int, BoxPlotStats>();
            ReporterIonIntensityStatsHighAbundanceData = new Dictionary<int, BoxPlotStats>();
        }

        private void AddHeaderColumn<T>(Dictionary<T, SortedSet<string>> columnNamesByIdentifier, T columnId, string columnName)
        {
            DbUtils.AddColumnNamesForIdentifier(columnNamesByIdentifier, columnId, columnName);
        }

        private void ClearResults()
        {
            mScanList.Clear();
            mParentIons.Clear();

            mReporterIonInfo.Clear();
            mReporterIonAbundances.Clear();

            PeakAreaHistogram.Clear();

            PeakWidthHistogram.Clear();
            PeakWidthHistogramUnits = UNDEFINED_UNITS;

            ReporterIonNames.Clear();

            ReporterIonObservationRate.Clear();
            ReporterIonObservationRateHighAbundance.Clear();

            ReporterIonIntensityStats.Clear();
            ReporterIonIntensityStatsHighAbundanceData.Clear();
        }

        private bool ComputeReporterIonObservationRates()
        {
            try
            {
                NonZeroReporterIons.Clear();
                NonZeroReporterIonsHighAbundance.Clear();

                ReporterIonObservationRate.Clear();
                ReporterIonObservationRateHighAbundance.Clear();

                var reporterIonScans = new SortedSet<int>();

                // Construct a master list of scan numbers for which we have reporter ion data
                foreach (var reporterIon in mReporterIonAbundances)
                {
                    foreach (var scanNumber in reporterIon.Value.Keys.Where(scanNumber => !reporterIonScans.Contains(scanNumber)))
                    {
                        reporterIonScans.Add(scanNumber);
                    }
                }

                // Determine the peak area for each scan
                // Keys are scan number, values are peak area
                var peakAreaByScan = new Dictionary<int, double>();

                foreach (var parentIon in mParentIons)
                {
                    if (parentIon.Value.FragScanIndices.Count == 0)
                    {
                        OnWarningEvent(string.Format(
                            "Parent ion {0} does not have a frag scan defined", parentIon.Key));
                        continue;
                    }

                    var fragScanNumber = parentIon.Value.FragScanIndices[0];
                    var peakArea = parentIon.Value.SICStats.Peak.Area;

                    if (peakAreaByScan.ContainsKey(fragScanNumber))
                    {
                        // Multiple parent ions point to the same scan; this shouldn't happen, but we'll allow it
                        peakAreaByScan[fragScanNumber] = Math.Max(peakAreaByScan[fragScanNumber], peakArea);
                        continue;
                    }

                    peakAreaByScan.Add(fragScanNumber, peakArea);
                }

                int highAbundanceThreshold;

                if (Options.PlotOptions.ReporterIonObservationRateTopNPct > 100 ||
                    Options.PlotOptions.ReporterIonObservationRateTopNPct < 1)
                {
                    highAbundanceThreshold = 100;
                }
                else
                {
                    highAbundanceThreshold = Options.PlotOptions.ReporterIonObservationRateTopNPct;
                }

                var scansToUseForHighAbundanceHistogram = peakAreaByScan.Count * highAbundanceThreshold / 100.0;

                // Assure that peakAreaByScan has an entry for each reporter ion
                // This shouldn't typically be required, but it can happen if the first scan (or scans) of the instrument data file are MS2 scans instead of MS1 scans
                // In this case, these MS2 scans will have reporter ions, but there won't be any parent ions for them
                foreach (var reporterIon in mReporterIonAbundances)
                {
                    foreach (var scanNumber in reporterIon.Value.Keys.Where(scanNumber => !peakAreaByScan.ContainsKey(scanNumber)))
                    {
                        peakAreaByScan.Add(scanNumber, 0);
                    }
                }

                // Cache the non-zero reporter ion intensities in NonZeroReporterIons and NonZeroReporterIonsHighAbundance
                // Keys in these dictionaries correspond to the keys in mReporterIonAbundances (which correspond to keys in mReporterIonInfo)
                // Values are the count of scans with a non-zero reporter ion

                // Initialize the tracking dictionaries
                foreach (var reporterIonIndex in mReporterIonAbundances.Keys)
                {
                    NonZeroReporterIons.Add(reporterIonIndex, new List<double>());
                    NonZeroReporterIonsHighAbundance.Add(reporterIonIndex, new List<double>());
                }

                var scansUsed = 0;
                var scansUsedHighAbundance = 0;

                foreach (var scanNumber in (from item in peakAreaByScan orderby item.Value descending select item.Key))
                {
                    if (!reporterIonScans.Contains(scanNumber))
                        continue;

                    scansUsed++;

                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    foreach (var reporterIon in mReporterIonAbundances)
                    {
                        var reporterIonIndex = reporterIon.Key;
                        var reporterIonAbundance = reporterIon.Value[scanNumber];

                        if (reporterIonAbundance <= 0)
                            continue;

                        NonZeroReporterIons[reporterIonIndex].Add(reporterIonAbundance);

                        if (scansUsed > scansToUseForHighAbundanceHistogram)
                            continue;

                        NonZeroReporterIonsHighAbundance[reporterIonIndex].Add(reporterIonAbundance);
                    }

                    if (scansUsed > scansToUseForHighAbundanceHistogram)
                        continue;

                    scansUsedHighAbundance++;
                }

                if (scansUsed == 0)
                {
                    // reporterIonScans was empty; this is unexpected
                    return true;
                }

                // Compute observation rate
                foreach (var reporterIonIndex in mReporterIonAbundances.Keys)
                {
                    var observationRate = NonZeroReporterIons[reporterIonIndex].Count / (float)scansUsed * 100;
                    ReporterIonObservationRate.Add(reporterIonIndex, observationRate);

                    var observationRateHighAbundance = NonZeroReporterIonsHighAbundance[reporterIonIndex].Count / (float)scansUsedHighAbundance * 100;
                    ReporterIonObservationRateHighAbundance.Add(reporterIonIndex, observationRateHighAbundance);
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in ComputeReporterIonObservationRates", ex);
                return false;
            }
        }

        private bool GeneratePeakAreaHistogram()
        {
            try
            {
                PeakAreaHistogram.Clear();

                var peakAreaData = new List<double>();

                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (var parentIon in mParentIons.Values)
                {
                    var peakArea = parentIon.SICStats.Peak.Area;
                    if (peakArea <= 0)
                        continue;

                    var logPeakArea = Math.Log10(peakArea);

                    peakAreaData.Add(logPeakArea);
                }

                if (peakAreaData.Count == 0)
                {
                    OnWarningEvent("None of the parent ions has a non-zero peak area; cannot create a histogram of peak areas");
                    return true;
                }

                var numberOfBins = Options.PlotOptions.PeakAreaHistogramBinCount <= 0 ?
                                       40 :
                                       Options.PlotOptions.PeakAreaHistogramBinCount;

                var histogram = new MathNet.Numerics.Statistics.Histogram(peakAreaData, numberOfBins);

                for (var i = 0; i < histogram.BucketCount; i++)
                {
                    var binCount = histogram[i].Count;

                    if (double.IsNaN(binCount))
                        continue;

                    var midPoint = ((float)histogram[i].LowerBound + (float)histogram[i].UpperBound) / 2;
                    PeakAreaHistogram.Add(midPoint, (int)binCount);
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in GeneratePeakAreaHistogram", ex);
                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses not needed")]
        private bool GeneratePeakWidthHistogram()
        {
            try
            {
                PeakWidthHistogram.Clear();

                var scanNumbers = new List<int>();
                foreach (var scanNumber in (from item in mScanList.Keys orderby item select item))
                {
                    scanNumbers.Add(scanNumber);
                }

                var peakWidths = new List<double>();

                var timeBasedPeakWidths = false;

                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (var parentIon in mParentIons)
                {
                    var fwhmWidthScans = parentIon.Value.SICStats.Peak.FWHMScanWidth;
                    if (fwhmWidthScans <= 0)
                    {
                        continue;
                    }

                    if (scanNumbers.Count > 0)
                    {
                        // Track peak width in seconds
                        var scanIndexMatch = clsBinarySearch.BinarySearchFindNearest(
                            scanNumbers,
                            parentIon.Value.OptimalPeakApexScanNumber,
                            clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint);

                        var startScanIndex = (int)Math.Floor(scanIndexMatch - fwhmWidthScans / 2.0);
                        if (startScanIndex < 0)
                            startScanIndex = 0;

                        var endScanIndex = startScanIndex + fwhmWidthScans;
                        if (endScanIndex >= scanNumbers.Count)
                            endScanIndex = scanNumbers.Count - 1;

                        var startScan = scanNumbers[startScanIndex];
                        var endScan = scanNumbers[endScanIndex];

                        if (!mScanList.ContainsKey(startScan))
                        {
                            OnWarningEvent(string.Format(
                                "StartScan {0} not found in the _ScanStats file; cannot use parent ion index {1} for the peak width histogram",
                                startScan, parentIon.Key));

                            continue;
                        }

                        if (!mScanList.ContainsKey(endScan))
                        {
                            OnWarningEvent(string.Format(
                                "EndScan {0} not found in the _ScanStats file; cannot use parent ion index {1} for the peak width histogram",
                                endScan, parentIon.Key));

                            continue;
                        }

                        var peakWidth = (int)Math.Floor(mScanList[endScan].ScanTime * 60.0 - mScanList[startScan].ScanTime * 60.0);
                        peakWidths.Add(peakWidth);
                        timeBasedPeakWidths = true;
                        continue;
                    }

                    // Track peak width in scans
                    peakWidths.Add(fwhmWidthScans);
                }

                if (peakWidths.Count == 0)
                {
                    OnWarningEvent("None of the parent ions has a non-zero peak area; cannot create a histogram of peak width");
                    return true;
                }

                PeakWidthHistogramUnits = timeBasedPeakWidths ? "seconds" : "scans";

                // Make an initial histogram
                var numberOfBins = Options.PlotOptions.PeakWidthHistogramBinCount <= 0 ?
                                       40 :
                                       Options.PlotOptions.PeakWidthHistogramBinCount;

                var histogram = new MathNet.Numerics.Statistics.Histogram(peakWidths, numberOfBins);

                const double filterPercentile = 98;
                var filteredHistogramValues = GetFilteredHistogram(peakWidths, histogram, numberOfBins, filterPercentile);

                foreach (var dataPoint in (from item in filteredHistogramValues orderby item.Key select item))
                {
                    PeakWidthHistogram.Add(dataPoint.Key, dataPoint.Value);
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in GeneratePeakWidthHistogram", ex);
                return false;
            }
        }

        private Dictionary<float, int> GetFilteredHistogram(
            IReadOnlyCollection<double> peakWidths,
            MathNet.Numerics.Statistics.Histogram histogram,
            int numberOfBins,
            double percentile)
        {
            // Find the bin at which 98% of the peaks have been included in the histogram
            var binCountSum = 0;
            var maxPeakWidthToUse = -1;

            var threshold = percentile / 100.0;

            for (var i = 0; i < histogram.BucketCount; i++)
            {
                var binCount = histogram[i].Count;

                if (double.IsNaN(binCount))
                    continue;

                binCountSum += (int)binCount;

                if (binCountSum / (double)peakWidths.Count > threshold)
                {
                    var binStart = (int)Math.Round(histogram[i].UpperBound);
                    maxPeakWidthToUse = binStart;
                    break;
                }
            }

            // Populate a new list using the peak widths that are less than the 98th percentile
            var filteredPeakWidths = new List<double>();

            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var peakWidth in peakWidths)
            {
                if (maxPeakWidthToUse < 0 || peakWidth <= maxPeakWidthToUse)
                    filteredPeakWidths.Add(peakWidth);
            }

            // Make a new histogram using the filtered list
            var filteredHistogram = new MathNet.Numerics.Statistics.Histogram(filteredPeakWidths, numberOfBins);

            var interpolatedHistogram = ReplaceZeroesInHistogram(filteredHistogram);

            return interpolatedHistogram;
        }

        private bool LoadReporterIons(string reporterIonsFilePath)
        {
            try
            {
                mReporterIonInfo.Clear();
                mReporterIonAbundances.Clear();

                var reporterIonsFile = new FileInfo(reporterIonsFilePath);
                if (!reporterIonsFile.Exists)
                {
                    // ReporterIons file not found
                    // Cannot create any reporter-ion based plots; this is not an error
                    return false;
                }

                OnDebugEvent("Reading reporter ions from " + PathUtils.CompactPathString(reporterIonsFile.FullName, 110));

                // Keys in this dictionary are column identifier
                // Values are the index of this column in the tab-delimited text file (-1 if not present)
                var columnMap = new Dictionary<ReporterIonsColumns, int>();

                var columnNamesByIdentifier = new Dictionary<ReporterIonsColumns, SortedSet<string>>();
                AddHeaderColumn(columnNamesByIdentifier, ReporterIonsColumns.DatasetID, "Dataset");
                AddHeaderColumn(columnNamesByIdentifier, ReporterIonsColumns.ScanNumber, "ScanNumber");
                AddHeaderColumn(columnNamesByIdentifier, ReporterIonsColumns.CollisionMode, "Collision Mode");
                AddHeaderColumn(columnNamesByIdentifier, ReporterIonsColumns.ParentIonMZ, "ParentIonMZ");
                AddHeaderColumn(columnNamesByIdentifier, ReporterIonsColumns.BasePeakIntensity, "BasePeakIntensity");
                AddHeaderColumn(columnNamesByIdentifier, ReporterIonsColumns.BasePeakMZ, "BasePeakMZ");
                AddHeaderColumn(columnNamesByIdentifier, ReporterIonsColumns.ReporterIonIntensityMax, "ReporterIonIntensityMax");
                AddHeaderColumn(columnNamesByIdentifier, ReporterIonsColumns.WeightedAvgPctIntensityCorrection, "Weighted Avg Pct Intensity Correction");

                var requiredColumns = new List<ReporterIonsColumns>
                {
                    ReporterIonsColumns.ScanNumber
                };

                // This RegEx matches reporter ion abundance columns
                // Typically columns will have names like Ion_126.128 or Ion_116
                // However, the name could be followed by an underscore then an integer, thus the ?<ReporterIonIndex> group

                var reporterIonMzMatcher = new Regex(@"^Ion_(?<Mz>[0-9.]+)(?<ReporterIonIndex>_\d+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                using (var reader = new StreamReader(new FileStream(reporterIonsFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    var linesRead = 0;

                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        if (dataLine == null)
                            continue;

                        var dataValues = dataLine.Split('\t');

                        if (dataValues.Length == 0)
                            continue;

                        linesRead++;

                        if (linesRead == 1)
                        {
                            // This is the first non-blank line; parse the headers
                            DbUtils.GetColumnMappingFromHeaderLine(columnMap, dataLine, columnNamesByIdentifier);

                            // Assure that required columns are present
                            foreach (var columnId in requiredColumns)
                            {
                                if (DbUtils.GetColumnIndex(columnMap, columnId) < 0)
                                {
                                    OnWarningEvent(string.Format("File {0} is missing required column {1}", reporterIonsFile.FullName, columnId));
                                    return false;
                                }
                            }

                            // Determine the column indices of the reporter ion abundance columns

                            // Reporter ion column names are of the form:
                            // Ion_126.128 or Ion_116

                            // If there is a name conflict due to two reporter ions having the same rounded mass,
                            // a uniquifier will have been appended, e.g. Ion_116_2

                            for (var columnIndex = 0; columnIndex < dataValues.Length; columnIndex++)
                            {
                                var columnName = dataValues[columnIndex];

                                if (columnMap.ContainsValue(columnIndex) || !columnName.StartsWith(clsReporterIonProcessor.REPORTER_ION_COLUMN_PREFIX))
                                    continue;

                                var reporterIonMatch = reporterIonMzMatcher.Match(columnName);

                                if (!reporterIonMatch.Success)
                                {
                                    // Although this column starts with Ion_, it is not a reporter ion intensity column
                                    // Example columns that are skipped:
                                    // Ion_126.128_ObsMZ
                                    // Ion_126.128_OriginalIntensity
                                    // Ion_126.128_SignalToNoise
                                    // Ion_126.128_Resolution
                                    continue;
                                }

                                var reporterIonMz = double.Parse(reporterIonMatch.Groups["Mz"].Value);
                                var reporterIon = new clsReporterIonInfo(reporterIonMz);

                                mReporterIonInfo.Add(columnIndex, reporterIon);
                                mReporterIonAbundances.Add(columnIndex, new Dictionary<int, double>());

                                ReporterIonNames.Add(columnIndex, columnName);
                            }

                            continue;
                        }

                        var scanNumber = DbUtils.GetColumnValue(dataValues, columnMap, ReporterIonsColumns.ScanNumber, 0);
                        // Skip: var collisionMode = DbUtils.GetColumnValue(dataValues, columnMap, ReporterIonsColumns.CollisionMode);
                        // Skip: var parentIonMZ = DbUtils.GetColumnValue(dataValues, columnMap, ReporterIonsColumns.ParentIonMZ, 0.0);
                        // Skip: var basePeakIntensity = DbUtils.GetColumnValue(dataValues, columnMap, ReporterIonsColumns.BasePeakIntensity, 0.0);
                        // Skip: var basePeakMZ = DbUtils.GetColumnValue(dataValues, columnMap, ReporterIonsColumns.BasePeakMZ, 0.0);
                        // Skip: var reporterIonIntensityMax = DbUtils.GetColumnValue(dataValues, columnMap, ReporterIonsColumns.ReporterIonIntensityMax, 0.0);
                        // Skip: var weightedAvgPctIntensityCorrection = DbUtils.GetColumnValue(dataValues, columnMap, ReporterIonsColumns.WeightedAvgPctIntensityCorrection, 0.0);

                        foreach (var columnIndex in mReporterIonInfo.Keys)
                        {
                            if (double.TryParse(dataValues[columnIndex], out var reporterIonAbundance))
                            {
                                mReporterIonAbundances[columnIndex].Add(scanNumber, reporterIonAbundance);
                            }
                            else
                            {
                                mReporterIonAbundances[columnIndex].Add(scanNumber, 0);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in LoadReporterIons", ex);
                return false;
            }
        }

        private bool LoadScanStats(string scanStatsFilePath)
        {
            try
            {
                mScanList.Clear();

                var scanStatsFile = new FileInfo(scanStatsFilePath);
                if (!scanStatsFile.Exists)
                {
                    OnWarningEvent("ScanStats file not found, cannot convert FWHM in scans to minutes; file path:" + scanStatsFile.FullName);
                    return false;
                }

                OnDebugEvent("Reading scan info from " + PathUtils.CompactPathString(scanStatsFile.FullName, 110));

                // Keys in this dictionary are column identifier
                // Values are the index of this column in the tab-delimited text file (-1 if not present)
                var columnMap = new Dictionary<ScanStatsColumns, int>();

                var columnNamesByIdentifier = new Dictionary<ScanStatsColumns, SortedSet<string>>();
                AddHeaderColumn(columnNamesByIdentifier, ScanStatsColumns.DatasetID, "Dataset");
                AddHeaderColumn(columnNamesByIdentifier, ScanStatsColumns.ScanNumber, "ScanNumber");
                AddHeaderColumn(columnNamesByIdentifier, ScanStatsColumns.ScanTime, "ScanTime");
                AddHeaderColumn(columnNamesByIdentifier, ScanStatsColumns.ScanType, "ScanType");
                AddHeaderColumn(columnNamesByIdentifier, ScanStatsColumns.TotalIonIntensity, "TotalIonIntensity");
                AddHeaderColumn(columnNamesByIdentifier, ScanStatsColumns.BasePeakIntensity, "BasePeakIntensity");
                AddHeaderColumn(columnNamesByIdentifier, ScanStatsColumns.BasePeakMZ, "BasePeakMZ");
                AddHeaderColumn(columnNamesByIdentifier, ScanStatsColumns.BasePeakSignalToNoiseRatio, "BasePeakSignalToNoiseRatio");
                AddHeaderColumn(columnNamesByIdentifier, ScanStatsColumns.IonCount, "IonCount");
                AddHeaderColumn(columnNamesByIdentifier, ScanStatsColumns.IonCountRaw, "IonCountRaw");
                AddHeaderColumn(columnNamesByIdentifier, ScanStatsColumns.ScanTypeName, "ScanTypeName");

                var requiredColumns = new List<ScanStatsColumns>
                {
                    ScanStatsColumns.ScanNumber,
                    ScanStatsColumns.ScanTime,
                };

                using (var reader = new StreamReader(new FileStream(scanStatsFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    var linesRead = 0;

                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        if (dataLine == null)
                            continue;

                        var dataValues = dataLine.Split('\t');

                        if (dataValues.Length == 0)
                            continue;

                        linesRead++;

                        if (linesRead == 1)
                        {
                            // This is the first non-blank line; parse the headers
                            DbUtils.GetColumnMappingFromHeaderLine(columnMap, dataLine, columnNamesByIdentifier);

                            // Assure that required columns are present
                            foreach (var columnId in requiredColumns)
                            {
                                if (DbUtils.GetColumnIndex(columnMap, columnId) < 0)
                                {
                                    OnWarningEvent(string.Format("File {0} is missing required column {1}", scanStatsFile.FullName, columnId));
                                    return false;
                                }
                            }
                            continue;
                        }

                        var scanNumber = DbUtils.GetColumnValue(dataValues, columnMap, ScanStatsColumns.ScanNumber, 0);
                        var scanTime = DbUtils.GetColumnValue(dataValues, columnMap, ScanStatsColumns.ScanTime, 0.0);
                        // Skip: var scanType = DbUtils.GetColumnValue(dataValues, columnMap, ScanStatsColumns.ScanType, 0);
                        var totalIonIntensity = DbUtils.GetColumnValue(dataValues, columnMap, ScanStatsColumns.TotalIonIntensity, 0.0);
                        var basePeakIntensity = DbUtils.GetColumnValue(dataValues, columnMap, ScanStatsColumns.BasePeakIntensity, 0.0);
                        var basePeakMZ = DbUtils.GetColumnValue(dataValues, columnMap, ScanStatsColumns.BasePeakMZ, 0.0);
                        // Skip: var basePeakSignalToNoiseRatio = DbUtils.GetColumnValue(dataValues, columnMap, ScanStatsColumns.BasePeakSignalToNoiseRatio, 0.0);
                        var ionCount = DbUtils.GetColumnValue(dataValues, columnMap, ScanStatsColumns.IonCount, 0);
                        var ionCountRaw = DbUtils.GetColumnValue(dataValues, columnMap, ScanStatsColumns.IonCountRaw, 0);
                        var scanTypeName = DbUtils.GetColumnValue(dataValues, columnMap, ScanStatsColumns.ScanTypeName);

                        var scanInfo = new clsScanInfo()
                        {
                            ScanNumber = scanNumber,
                            ScanTime = (float)scanTime,
                            ScanTypeName = scanTypeName,
                            TotalIonIntensity = totalIonIntensity,
                            BasePeakIonIntensity = basePeakIntensity,
                            BasePeakIonMZ = basePeakMZ,
                            IonCount = ionCount,
                            IonCountRaw = ionCountRaw
                        };

                        if (mScanList.ContainsKey(scanNumber))
                        {
                            OnWarningEvent(string.Format("Ignoring duplicate scan {0} found in {1}", scanNumber, scanStatsFile.Name));
                            continue;
                        }

                        mScanList.Add(scanNumber, scanInfo);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in LoadScanStats", ex);
                return false;
            }
        }

        private bool LoadSICStats(string sicStatsFilePath)
        {
            try
            {
                mParentIons.Clear();

                var sicStatsFile = new FileInfo(sicStatsFilePath);
                if (!sicStatsFile.Exists)
                {
                    OnWarningEvent("File not found: " + sicStatsFile.FullName);
                    return false;
                }

                OnDebugEvent("Reading SIC data from " + PathUtils.CompactPathString(sicStatsFile.FullName, 110));

                // Keys in this dictionary are column identifier
                // Values are the index of this column in the tab-delimited text file (-1 if not present)
                var columnMap = new Dictionary<SICStatsColumns, int>();

                var columnNamesByIdentifier = new Dictionary<SICStatsColumns, SortedSet<string>>();
                AddHeaderColumn(columnNamesByIdentifier, SICStatsColumns.DatasetID, "Dataset");
                AddHeaderColumn(columnNamesByIdentifier, SICStatsColumns.ParentIonIndex, "ParentIonIndex");
                AddHeaderColumn(columnNamesByIdentifier, SICStatsColumns.Mz, "MZ");
                AddHeaderColumn(columnNamesByIdentifier, SICStatsColumns.SurveyScanNumber, "SurveyScanNumber");
                AddHeaderColumn(columnNamesByIdentifier, SICStatsColumns.FragScanNumber, "FragScanNumber");
                AddHeaderColumn(columnNamesByIdentifier, SICStatsColumns.OptimalPeakApexScanNumber, "OptimalPeakApexScanNumber");
                AddHeaderColumn(columnNamesByIdentifier, SICStatsColumns.PeakSignalToNoiseRatio, "PeakSignalToNoiseRatio");
                AddHeaderColumn(columnNamesByIdentifier, SICStatsColumns.FWHMinScans, "FWHMInScans");
                AddHeaderColumn(columnNamesByIdentifier, SICStatsColumns.PeakArea, "PeakArea");
                AddHeaderColumn(columnNamesByIdentifier, SICStatsColumns.ParentIonIntensity, "ParentIonIntensity");

                var requiredColumns = new List<SICStatsColumns>
                {
                    SICStatsColumns.OptimalPeakApexScanNumber,
                    SICStatsColumns.FWHMinScans,
                    SICStatsColumns.PeakArea
                };

                var duplicateParentIonIndices = new SortedSet<int>();

                using (var reader = new StreamReader(new FileStream(sicStatsFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    var linesRead = 0;

                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        if (dataLine == null)
                            continue;

                        var dataValues = dataLine.Split('\t');

                        if (dataValues.Length == 0)
                            continue;

                        linesRead++;

                        if (linesRead == 1)
                        {
                            // This is the first non-blank line; parse the headers
                            DbUtils.GetColumnMappingFromHeaderLine(columnMap, dataLine, columnNamesByIdentifier);

                            // Assure that required columns are present
                            foreach (var columnId in requiredColumns)
                            {
                                if (DbUtils.GetColumnIndex(columnMap, columnId) < 0)
                                {
                                    OnWarningEvent(string.Format("File {0} is missing required column {1}", sicStatsFile.FullName, columnId));
                                    return false;
                                }
                            }
                            continue;
                        }

                        var parentIonIndex = DbUtils.GetColumnValue(dataValues, columnMap, SICStatsColumns.ParentIonIndex, 0);
                        var parentIonMz = DbUtils.GetColumnValue(dataValues, columnMap, SICStatsColumns.Mz, 0.0);
                        var surveyScanNumber = DbUtils.GetColumnValue(dataValues, columnMap, SICStatsColumns.SurveyScanNumber, 0);
                        var fragScanNumber = DbUtils.GetColumnValue(dataValues, columnMap, SICStatsColumns.FragScanNumber, 0);
                        var optimalPeakApexScanNumber = DbUtils.GetColumnValue(dataValues, columnMap, SICStatsColumns.OptimalPeakApexScanNumber, 0);
                        var peakSignalToNoiseRatio = DbUtils.GetColumnValue(dataValues, columnMap, SICStatsColumns.PeakSignalToNoiseRatio, 0.0);
                        var fwhmInScans = DbUtils.GetColumnValue(dataValues, columnMap, SICStatsColumns.FWHMinScans, 0);
                        var peakArea = DbUtils.GetColumnValue(dataValues, columnMap, SICStatsColumns.PeakArea, 0.0);
                        var parentIonIntensity = DbUtils.GetColumnValue(dataValues, columnMap, SICStatsColumns.ParentIonIntensity, 0.0);

                        var parentIon = new clsParentIonInfo(parentIonMz)
                        {
                            OptimalPeakApexScanNumber = optimalPeakApexScanNumber
                        };

                        // MASIC typically tracks scans using ScanIndex values
                        // Instead, we're storing scan numbers here
                        parentIon.FragScanIndices.Add(fragScanNumber);
                        parentIon.SurveyScanIndex = surveyScanNumber;

                        parentIon.SICStats.Peak.ParentIonIntensity = parentIonIntensity;
                        parentIon.SICStats.Peak.SignalToNoiseRatio = peakSignalToNoiseRatio;
                        parentIon.SICStats.Peak.FWHMScanWidth = fwhmInScans;
                        parentIon.SICStats.Peak.Area = peakArea;

                        if (!mParentIons.ContainsKey(parentIonIndex))
                        {
                            mParentIons.Add(parentIonIndex, parentIon);
                            continue;
                        }

                        // Duplicate parent ion index
                        // This is not typically seen, but it is possible if the same parent ion is fragmented using multiple fragmentation modes
                        // For example, see Angiotensin_AllScans.raw at https://github.com/PNNL-Comp-Mass-Spec/MASIC/tree/master/Docs/Lumos_Example

                        if (duplicateParentIonIndices.Contains(parentIonIndex))
                            continue;

                        duplicateParentIonIndices.Add(parentIonIndex);
                        if (duplicateParentIonIndices.Count > 5)
                            continue;

                        OnWarningEvent(string.Format(
                            "Ignoring duplicate parent ion index {0} found in {1}", parentIonIndex, sicStatsFile.Name));
                    }

                    if (duplicateParentIonIndices.Count > 5)
                    {
                        OnWarningEvent(string.Format(
                            // ReSharper disable once StringLiteralTypo
                            "Ignored {0} lines in the _SICstats.txt file due to a duplicate parent ion index\n" +
                            "This typically indicates that the same parent ion was fragmented " +
                            "using multiple fragmentation modes, and is thus not an error", duplicateParentIonIndices.Count));
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in LoadSICStats", ex);
                return false;
            }
        }

        private Dictionary<float, int> ReplaceZeroesInHistogram(MathNet.Numerics.Statistics.Histogram histogram)
        {
            var interpolatedHistogram = new Dictionary<float, int>();

            var lastNonZeroBin = 0.0;
            var lastNonZeroCount = 0;

            var binsToAdd = new Queue<float>();

            for (var i = 0; i < histogram.BucketCount; i++)
            {
                var binCount = histogram[i].Count;

                if (double.IsNaN(binCount))
                    continue;

                var midPoint = ((float)histogram[i].LowerBound + (float)histogram[i].UpperBound) / 2;

                var binCountInteger = (int)binCount;

                if (binCountInteger == 0 && lastNonZeroCount > 0)
                {
                    // This bucket has a count of zero but the previous bucket has a non-zero count
                    // Store this bucket's x value in a queue that we will use later to interpolate this bucket's count
                    binsToAdd.Enqueue(midPoint);
                    continue;
                }

                if (binsToAdd.Count > 0)
                {
                    // Interpolate counts for buckets in binsToAdd

                    var points = new List<double> { lastNonZeroBin, midPoint };
                    var values = new List<double> { lastNonZeroCount, binCountInteger };
                    var interpolationTool = MathNet.Numerics.Interpolate.Linear(points, values);

                    while (binsToAdd.Count > 0)
                    {
                        var binToAdd = binsToAdd.Dequeue();
                        var interpolatedBinCount = (int)Math.Round(interpolationTool.Interpolate(binToAdd), 0);

                        interpolatedHistogram.Add(binToAdd, interpolatedBinCount);
                    }
                }

                interpolatedHistogram.Add(midPoint, binCountInteger);

                if (binCountInteger <= 0)
                    continue;

                lastNonZeroBin = midPoint;
                lastNonZeroCount = binCountInteger;
            }

            return interpolatedHistogram;
        }

        public bool SummarizeSICStats(string sicStatsFilePath)
        {
            try
            {
                bool peakAreaHistogramCreated;
                bool peakWidthHistogramCreated;
                bool observationRatesDetermined;

                ClearResults();

                var sicStatsLoaded = LoadSICStats(sicStatsFilePath);

                var scanStatsFilePath = clsUtilities.ReplaceSuffix(sicStatsFilePath, clsDataOutput.SIC_STATS_FILE_SUFFIX, clsDataOutput.SCAN_STATS_FILE_SUFFIX);

                // ReSharper disable once UnusedVariable
                var scanStatsLoaded = LoadScanStats(scanStatsFilePath);

                var reporterIonsFilePath = clsUtilities.ReplaceSuffix(sicStatsFilePath, clsDataOutput.SIC_STATS_FILE_SUFFIX, clsDataOutput.REPORTER_IONS_FILE_SUFFIX);
                var reporterIonsLoaded = LoadReporterIons(reporterIonsFilePath);

                if (sicStatsLoaded)
                {
                    peakAreaHistogramCreated = GeneratePeakAreaHistogram();
                    peakWidthHistogramCreated = GeneratePeakWidthHistogram();
                }
                else
                {
                    peakAreaHistogramCreated = false;
                    peakWidthHistogramCreated = false;
                }

                if (reporterIonsLoaded)
                {
                    observationRatesDetermined = ComputeReporterIonObservationRates();
                }
                else
                {
                    observationRatesDetermined = false;
                }

                return peakAreaHistogramCreated ||
                       peakWidthHistogramCreated ||
                       observationRatesDetermined;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in SummarizeSICStats", ex);
                return false;
            }
        }
    }
}
