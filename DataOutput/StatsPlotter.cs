using System;
using System.Collections.Generic;
using System.IO;
using MASIC.Options;
using MASIC.Plots;
using PRISM;

namespace MASIC.DataOutput
{
    public class StatsPlotter : EventNotifier
    {
        private readonly StatsSummarizer mStatsSummarizer;

        public const string REPORTER_ION_OBSERVATION_RATE_DATA_FILE_SUFFIX = "RepIonObsRate.txt";

        public const string REPORTER_ION_INTENSITY_STATS_FILE_SUFFIX = "RepIonStats.txt";

        /// <summary>
        /// MASIC Options
        /// </summary>
        public MASICOptions Options { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options">MASIC Options</param>
        public StatsPlotter(MASICOptions options)
        {
            Options = options;

            mStatsSummarizer = new StatsSummarizer(Options);
            RegisterEvents(mStatsSummarizer);
        }

        /// <summary>
        /// Append a plot to the list of plot files saved to disk
        /// </summary>
        /// <param name="plotFiles"></param>
        /// <param name="outputFilePath"></param>
        /// <param name="plotCategory"></param>
        /// <param name="plotDescription"></param>
        private void AppendPlotFile(
            ICollection<PlotFileInfo> plotFiles,
            string outputFilePath,
            PlotContainerBase.PlotCategories plotCategory,
            string plotDescription)
        {
            if (string.IsNullOrWhiteSpace(outputFilePath))
                return;

            var outputFile = new FileInfo(outputFilePath);
            var plotFile = new PlotFileInfo(outputFile)
            {
                PlotCategory = plotCategory,
                FileDescription = plotDescription
            };

            plotFiles.Add(plotFile);
        }

        private void AppendReporterIonStats(
            ICollection<string> dataLine,
            IReadOnlyDictionary<int, BoxPlotStats> reporterIonStats,
            int reporterIonIndex,
            IReadOnlyDictionary<string, int> columnWidths,
            string statNameSuffix)
        {
            var boxPlotStats = reporterIonStats.ContainsKey(reporterIonIndex) ? reporterIonStats[reporterIonIndex] : new BoxPlotStats();

            dataLine.Add(string.Format("{0}", boxPlotStats.NonZeroCount).PadRight(columnWidths["NonZeroCount" + statNameSuffix]));
            dataLine.Add(string.Format("{0:0}", boxPlotStats.Median).PadRight(columnWidths["Median" + statNameSuffix]));
            dataLine.Add(string.Format("{0:0}", boxPlotStats.InterQuartileRange).PadRight(columnWidths["InterQuartileRange" + statNameSuffix]));
            dataLine.Add(string.Format("{0:0}", boxPlotStats.LowerWhisker).PadRight(columnWidths["LowerWhisker" + statNameSuffix]));
            dataLine.Add(string.Format("{0:0}", boxPlotStats.UpperWhisker).PadRight(columnWidths["UpperWhisker" + statNameSuffix]));
            dataLine.Add(string.Format("{0:0}", boxPlotStats.Outliers.Count).PadRight(columnWidths["NumberOfOutliers" + statNameSuffix]));
        }

        private bool CreateHistogram(
            Dictionary<float, int> histogramData,
            string datasetName,
            string outputDirectory,
            ICollection<PlotFileInfo> plotFiles,
            string plotTitle,
            PlotContainerBase.PlotCategories plotCategory,
            string plotDescription,
            string plotAbbreviation,
            string xAxisLabel,
            string yAxisLabel)
        {
            try
            {
                var histogramPlotter = new HistogramPlotter(Options.PlotOptions, plotTitle, plotCategory)
                {
                    PlotAbbrev = plotAbbreviation,
                    XAxisLabel = xAxisLabel,
                    YAxisLabel = yAxisLabel
                };

                RegisterEvents(histogramPlotter);

                foreach (var dataPoint in histogramData)
                {
                    histogramPlotter.AddData(dataPoint.Key, dataPoint.Value);
                }

                var success = histogramPlotter.SavePlotFile(datasetName, outputDirectory, out var outputFilePath);

                AppendPlotFile(plotFiles, outputFilePath, plotCategory, plotDescription);

                return success;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.CreateHistogram", ex);
                return false;
            }
        }

        private bool CreateHistograms(string datasetName, string outputDirectory, ICollection<PlotFileInfo> plotFiles)
        {
            try
            {
                var peakAreaSuccess = CreateHistogram(
                    mStatsSummarizer.PeakAreaHistogram,
                    datasetName,
                    outputDirectory,
                    plotFiles,
                    "Peak Area Histogram",
                    PlotContainerBase.PlotCategories.SelectedIonChromatogramPeakStats,
                    "Peak Areas",
                    "PeakAreaHistogram",
                    "Peak Area (Log 10)",
                    "Count");


                var peakWidthSuccess = CreateHistogram(
                    mStatsSummarizer.PeakWidthHistogram,
                    datasetName,
                    outputDirectory,
                    plotFiles,
                    "Peak Width Histogram",
                    PlotContainerBase.PlotCategories.SelectedIonChromatogramPeakStats,
                    "Peak Widths",
                    "PeakWidthHistogram",
                    string.Format("Peak Width ({0}), FWHM", mStatsSummarizer.PeakWidthHistogramUnits),
                    "Count");

                return peakAreaSuccess && peakWidthSuccess;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.CreateHistograms", ex);
                return false;
            }
        }

        private bool CreateBarChart(
            IReadOnlyDictionary<int, string> barChartLabelsByIndex,
            IReadOnlyDictionary<int, float> barChartDataByIndex,
            string datasetName,
            string outputDirectory,
            ICollection<PlotFileInfo> plotFiles,
            string plotTitle,
            PlotContainerBase.PlotCategories plotCategory,
            string plotDescription,
            string plotAbbreviation,
            string yAxisLabel,
            int yAxisMinimum = 0,
            int yAxisMaximum = 102)
        {
            try
            {
                var barChartPlotter = new BarChartPlotter(Options.PlotOptions, plotTitle, plotCategory)
                {
                    PlotAbbrev = plotAbbreviation,
                    YAxisLabel = yAxisLabel
                };

                RegisterEvents(barChartPlotter);

                foreach (var dataPoint in barChartDataByIndex)
                {
                    var label = barChartLabelsByIndex[dataPoint.Key];
                    barChartPlotter.AddData(label, dataPoint.Value);
                }

                var success = barChartPlotter.SavePlotFile(datasetName, outputDirectory, out var outputFilePath, yAxisMinimum, yAxisMaximum);

                AppendPlotFile(plotFiles, outputFilePath, plotCategory, plotDescription);

                return success;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.CreateBarChart", ex);
                return false;
            }
        }

        private bool CreateBarCharts(string datasetName, string outputDirectory, ICollection<PlotFileInfo> plotFiles)
        {
            try
            {
                var highAbundanceTitle = string.Format("Reporter Ion Observation Rate (top {0}%)", Options.PlotOptions.ReporterIonObservationRateTopNPct);
                var allSpectraTitle = "Reporter Ion Observation Rate";
                var observationCountsTitle = string.Format("Reporter Ion Observation Counts (top {0}%)", Options.PlotOptions.ReporterIonObservationRateTopNPct);

                var success1 = CreateBarChart(
                    mStatsSummarizer.ReporterIonNames,
                    mStatsSummarizer.ReporterIonObservationRateHighAbundance,
                    datasetName,
                    outputDirectory,
                    plotFiles,
                    highAbundanceTitle,
                    PlotContainerBase.PlotCategories.ReporterIonObservationRate,
                    "Observation rate, excluding low abundance spectra",
                    "RepIonObsRateHighAbundance",
                    "Observation Rate (%)",
                    Options.PlotOptions.ReporterIonTopNPctObsRateYAxisMinimum);

                var success2 = CreateBarChart(
                    mStatsSummarizer.ReporterIonNames,
                    mStatsSummarizer.ReporterIonObservationRate,
                    datasetName,
                    outputDirectory,
                    plotFiles,
                    allSpectraTitle,
                    PlotContainerBase.PlotCategories.ReporterIonObservationRate,
                    "Observation rate, all spectra",
                    Path.GetFileNameWithoutExtension(REPORTER_ION_OBSERVATION_RATE_DATA_FILE_SUFFIX),
                    "Observation Rate (%)");

                var reporterIonObservationCounts = new Dictionary<int, float>();
                foreach (var item in mStatsSummarizer.ReporterIonIntensityStatsHighAbundanceData)
                {
                    reporterIonObservationCounts.Add(item.Key, item.Value.NonZeroCount);
                }

                var obsCountMaximum = 1;
                foreach (var item in reporterIonObservationCounts.Values)
                {
                    var observationCount = (int)Math.Round(item);
                    if (observationCount > obsCountMaximum)
                    {
                        obsCountMaximum = observationCount;
                    }
                }

                obsCountMaximum = (int)Math.Round(obsCountMaximum * 1.02);

                var success3 = CreateBarChart(
                     mStatsSummarizer.ReporterIonNames,
                     reporterIonObservationCounts,
                     datasetName,
                     outputDirectory,
                     plotFiles,
                     observationCountsTitle,
                     PlotContainerBase.PlotCategories.ReporterIonObservationCount,
                     "Observation counts, excluding low abundance spectra",
                     "RepIonObsCounts",
                     "Number of Spectra",
                     yAxisMaximum: obsCountMaximum);

                return success1 && success2 && success3;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.CreateBarCharts", ex);
                return false;
            }
        }

        private bool CreateBoxPlots(string datasetName, string outputDirectory, ICollection<PlotFileInfo> plotFiles)
        {
            try
            {
                var highAbundanceTitle = string.Format("Reporter Ion Intensities (top {0}%)", Options.PlotOptions.ReporterIonObservationRateTopNPct);
                var allSpectraTitle = "Reporter Ion Intensities";

                var success1 = CreateBoxPlot(
                    mStatsSummarizer.ReporterIonNames,
                    mStatsSummarizer.NonZeroReporterIonsHighAbundance,
                    mStatsSummarizer.ReporterIonIntensityStatsHighAbundanceData,
                    datasetName,
                    outputDirectory,
                    plotFiles,
                    highAbundanceTitle,
                    PlotContainerBase.PlotCategories.ReporterIonIntensityStats,
                    "Reporter ion intensities, excluding low abundance spectra",
                    "RepIonStatsHighAbundance",
                    "Intensity");

                var success2 = CreateBoxPlot(
                    mStatsSummarizer.ReporterIonNames,
                    mStatsSummarizer.NonZeroReporterIons,
                    mStatsSummarizer.ReporterIonIntensityStats,
                    datasetName,
                    outputDirectory,
                    plotFiles,
                    allSpectraTitle,
                    PlotContainerBase.PlotCategories.ReporterIonIntensityStats,
                    "Reporter ion intensities, all spectra",
                    Path.GetFileNameWithoutExtension(REPORTER_ION_INTENSITY_STATS_FILE_SUFFIX),
                    "Intensity",
                    skipCreatingPngFile: true);

                return success1 && success2;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.CreateBoxPlots", ex);
                return false;
            }
        }

        private bool CreateBoxPlot(
            IReadOnlyDictionary<int, string> boxPlotLabelsByIndex,
            IReadOnlyDictionary<int, List<double>> boxPlotDataByIndex,
            IDictionary<int, BoxPlotStats> boxPlotStats,
            string datasetName,
            string outputDirectory,
            ICollection<PlotFileInfo> plotFiles,
            string plotTitle,
            PlotContainerBase.PlotCategories plotCategory,
            string plotDescription,
            string plotAbbreviation,
            string yAxisLabel,
            bool logarithmicYAxis = true,
            bool skipCreatingPngFile = false,
            int yAxisMinimum = 0)
        {
            boxPlotStats.Clear();

            try
            {
                var boxPlotPlotter = new BoxPlotPlotter(Options.PlotOptions, plotTitle, plotCategory)
                {
                    PlotAbbrev = plotAbbreviation,
                    YAxisLabel = yAxisLabel
                };

                RegisterEvents(boxPlotPlotter);

                foreach (var item in boxPlotDataByIndex)
                {
                    var label = boxPlotLabelsByIndex[item.Key];
                    boxPlotPlotter.AddData(item.Key, label, item.Value);
                }

                var success = boxPlotPlotter.SavePlotFile(
                    datasetName, outputDirectory, out var outputFilePath,
                    logarithmicYAxis, skipCreatingPngFile, yAxisMinimum);

                foreach (var item in boxPlotPlotter.BoxPlotStatistics)
                {
                    boxPlotStats.Add(item.Key, item.Value);
                }

                if (!skipCreatingPngFile)
                {
                    AppendPlotFile(plotFiles, outputFilePath, plotCategory, plotDescription);
                }

                return success;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.CreateBoxPlot", ex);
                return false;
            }
        }

        private bool CreatePlots(string datasetName, string outputDirectory, out List<PlotFileInfo> plotFiles)
        {
            plotFiles = new List<PlotFileInfo>();
            var histogramSuccess = CreateHistograms(datasetName, outputDirectory, plotFiles);

            // Note that CreateBoxPlots needs to be called before calling CreateBarCharts
            // This is required to populate mStatsSummarizer.ReporterIonIntensityStatsHighAbundanceData
            var boxPlotSuccess = CreateBoxPlots(datasetName, outputDirectory, plotFiles);

            var barChartSuccess = CreateBarCharts(datasetName, outputDirectory, plotFiles);

            return barChartSuccess && boxPlotSuccess && histogramSuccess;
        }

        /// <summary>
        /// Read the SIC stats file (and optionally reporter ions file)
        /// Generate stats, then create plots
        /// </summary>
        /// <returns>True if success, otherwise false</returns>
        public bool ProcessFile(string sicStatsFilePath, string outputDirectory)
        {
            try
            {
                var statsSummarized = mStatsSummarizer.SummarizeSICStats(sicStatsFilePath);
                if (!statsSummarized)
                    return false;

                var datasetName = clsUtilities.ReplaceSuffix(Path.GetFileName(sicStatsFilePath), clsDataOutput.SIC_STATS_FILE_SUFFIX, string.Empty);

                var plotsGenerated = CreatePlots(datasetName, outputDirectory, out var plotFiles);

                var plotDataSaved = SavePlotData(datasetName, outputDirectory);

                var htmlCreated = SaveIndexHTML(datasetName, outputDirectory, plotFiles);

                return plotsGenerated && plotDataSaved && htmlCreated;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.ProcessFile", ex);
                return false;
            }
        }

        private bool SaveIndexHTML(string datasetName, string outputDirectory, List<PlotFileInfo> plotFiles)
        {
            if (!Options.PlotOptions.SaveHtmlFile)
                return true;

            var htmlCreator = new HTMLFileCreator(datasetName, Options.PlotOptions, plotFiles);
            RegisterEvents(htmlCreator);

            var success = htmlCreator.CreateHTMLFile(outputDirectory);
            return success;
        }

        private bool SaveHistogramData(string datasetName, string outputDirectory)
        {

            var peakAreaSuccess = WriteHistogramData(
                mStatsSummarizer.PeakAreaHistogram,
                datasetName,
                outputDirectory,
                "PeakAreaHistogram",
                "PeakArea_Log10");

            var peakWidthSuccess = WriteHistogramData(
                mStatsSummarizer.PeakWidthHistogram,
                datasetName,
                outputDirectory,
                "PeakWidthHistogram",
                string.Format("PeakWidth_{0}", mStatsSummarizer.PeakWidthHistogramUnits));

            return peakAreaSuccess && peakWidthSuccess;
        }

        private bool SavePlotData(string datasetName, string outputDirectory)
        {
            bool histogramSuccess;
            bool intensityStatsSuccess;
            bool obsRateSuccess;

            if (Options.PlotOptions.SaveHistogramData)
            {
                histogramSuccess = SaveHistogramData(datasetName, outputDirectory);
            }
            else
            {
                histogramSuccess = true;
            }

            if (Options.PlotOptions.SaveReporterIonIntensityStatsData)
            {
                intensityStatsSuccess = SaveReporterIonIntensityStatsData(datasetName, outputDirectory);
            }
            else
            {
                intensityStatsSuccess = true;
            }

            if (Options.PlotOptions.SaveReporterIonObservationRateData)
            {
                obsRateSuccess = SaveReporterIonObservationRateData(datasetName, outputDirectory);
            }
            else
            {
                obsRateSuccess = true;
            }

            return histogramSuccess && intensityStatsSuccess && obsRateSuccess;
        }

        private bool SaveReporterIonIntensityStatsData(string datasetName, string outputDirectory)
        {
            if (mStatsSummarizer.ReporterIonNames.Keys.Count == 0)
                return true;

            var success = WriteReporterIonIntensityStatsData(
                mStatsSummarizer.ReporterIonNames,
                mStatsSummarizer.ReporterIonIntensityStats,
                mStatsSummarizer.ReporterIonIntensityStatsHighAbundanceData,
                datasetName,
                outputDirectory,
                REPORTER_ION_INTENSITY_STATS_FILE_SUFFIX);

            return success;
        }

        private bool SaveReporterIonObservationRateData(string datasetName, string outputDirectory)
        {
            if (mStatsSummarizer.ReporterIonNames.Keys.Count == 0)
                return true;

            var success = WriteReporterIonObservationRateData(
                mStatsSummarizer.ReporterIonNames,
                mStatsSummarizer.ReporterIonObservationRate,
                mStatsSummarizer.ReporterIonObservationRateHighAbundance,
                datasetName,
                outputDirectory,
                REPORTER_ION_OBSERVATION_RATE_DATA_FILE_SUFFIX);

            return success;
        }

        private bool WriteHistogramData(
            IReadOnlyDictionary<float, int> histogramData,
            string datasetName,
            string outputDirectory,
            string fileSuffix,
            string dataColumnHeader)
        {
            try
            {
                var outputFilePath = Path.Combine(outputDirectory, string.Format("{0}_{1}.txt", datasetName, fileSuffix));
                OnDebugEvent("Saving " + PathUtils.CompactPathString(outputFilePath, 120));

                using (var writer = new StreamWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    writer.WriteLine("{0}\t{1}", dataColumnHeader, "Count");
                    foreach (var dataPoint in histogramData)
                    {
                        writer.WriteLine("{0:0.0#}\t{1}", dataPoint.Key, dataPoint.Value);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in WriteHistogramData", ex);
                return false;
            }
        }

        private bool WriteReporterIonIntensityStatsData(
            IReadOnlyDictionary<int, string> reporterIonNames,
            IReadOnlyDictionary<int, BoxPlotStats> reporterIonIntensityStats,
            IReadOnlyDictionary<int, BoxPlotStats> reporterIonIntensityStatsHighAbundance,
            string datasetName,
            string outputDirectory,
            string fileSuffix)
        {
            try
            {
                var outputFilePath = Path.Combine(outputDirectory, string.Format("{0}_{1}", datasetName, fileSuffix));
                OnDebugEvent("Saving " + PathUtils.CompactPathString(outputFilePath, 120));

                var statNameSuffixTopNPct = string.Format("_Top{0}Pct", Options.PlotOptions.ReporterIonObservationRateTopNPct);

                // Define the column names and widths
                var statNames = new List<string>
                {
                    "NonZeroCount",
                    "Median",
                    "InterQuartileRange",
                    "LowerWhisker",
                    "UpperWhisker",
                    "NumberOfOutliers"
                };

                var columnWidths = new Dictionary<string, int>();
                foreach (var item in statNames)
                {
                    var columnWidth = Math.Max(10, (item + statNameSuffixTopNPct).Length);
                    columnWidths.Add(item, columnWidth);
                    columnWidths.Add(item + statNameSuffixTopNPct, columnWidth);
                }

                columnWidths.Add("Reporter_Ion", "Reporter_Ion".Length);

                using (var writer = new StreamWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    for (var i = 0; i < 2; i++)
                    {
                        if (i > 0)
                            writer.WriteLine();

                        // Write the header line
                        var dataLine = new List<string> {
                            "Reporter_Ion"
                        };

                        var statNameSuffix = i == 0 ? statNameSuffixTopNPct : string.Empty;

                        foreach (var statName in statNames)
                        {
                            var columnName = string.Format("{0}{1}", statName, statNameSuffix);
                            var totalWidth = columnWidths[columnName];
                            dataLine.Add(columnName.PadRight(totalWidth));
                        }
                        writer.WriteLine(string.Join("\t", dataLine));

                        foreach (var reporterIonIndex in reporterIonNames.Keys)
                        {
                            dataLine.Clear();
                            dataLine.Add(reporterIonNames[reporterIonIndex].PadRight("Reporter_Ion".Length));

                            AppendReporterIonStats(
                                dataLine,
                                i == 0 ? reporterIonIntensityStatsHighAbundance : reporterIonIntensityStats,
                                reporterIonIndex,
                                columnWidths,
                                statNameSuffixTopNPct);

                            writer.WriteLine(string.Join("\t", dataLine));
                        }

                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in WriteReporterIonObservationRateData", ex);
                return false;
            }
        }

        private bool WriteReporterIonObservationRateData(
            IReadOnlyDictionary<int, string> reporterIonNames,
            IReadOnlyDictionary<int, float> reporterIonObservationRateData,
            IReadOnlyDictionary<int, float> reporterIonObservationRateHighAbundanceData,
            string datasetName,
            string outputDirectory,
            string fileSuffix)
        {
            try
            {
                var outputFilePath = Path.Combine(outputDirectory, string.Format("{0}_{1}", datasetName, fileSuffix));
                OnDebugEvent("Saving " + PathUtils.CompactPathString(outputFilePath, 120));

                using (var writer = new StreamWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    var observationRateHighAbundance = string.Format("Observation_Rate_Top{0}Pct", Options.PlotOptions.ReporterIonObservationRateTopNPct);

                    writer.WriteLine("{0}\t{1}\t{2}", "Reporter_Ion", "Observation_Rate", observationRateHighAbundance);
                    foreach (var reporterIonIndex in reporterIonNames.Keys)
                    {
                        writer.WriteLine("{0,-12}\t{1,-16:0.0##}\t{2:0.0##}",
                            reporterIonNames[reporterIonIndex],
                            reporterIonObservationRateData[reporterIonIndex],
                            reporterIonObservationRateHighAbundanceData[reporterIonIndex]);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in WriteReporterIonObservationRateData", ex);
                return false;
            }
        }
    }
}
