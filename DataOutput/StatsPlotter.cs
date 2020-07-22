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

        private bool CreateHistogram(
            Dictionary<float, int> histogramData,
            string datasetName,
            string outputDirectory,
            string plotTitle,
            string plotAbbreviation,
            string xAxisLabel,
            string yAxisLabel)
        {
            try
            {
                var histogramPlotter = new HistogramPlotter(plotTitle)
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

                var success = histogramPlotter.SavePlotFile(datasetName, outputDirectory);

                return success;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.CreateHistogram", ex);
                return false;
            }
        }

        private bool CreateHistograms(string datasetName, string outputDirectory)
        {
            try
            {
                var peakAreaSuccess = CreateHistogram(
                    mStatsSummarizer.PeakAreaHistogram,
                    datasetName,
                    outputDirectory,
                    "Peak Area Histogram",
                    "PeakAreaStats",
                    "Peak Area (Log 10)",
                    "Count");


                var peakWidthSuccess = CreateHistogram(
                    mStatsSummarizer.PeakWidthHistogram,
                    datasetName,
                    outputDirectory,
                    "Peak Width Histogram",
                    "PeakWidthStats",
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
            string plotTitle,
            string plotAbbreviation,
            string yAxisLabel)
        {
            try
            {
                var barChartPlotter = new BarChartPlotter(plotTitle) {
                    PlotAbbrev = plotAbbreviation,
                    YAxisLabel = yAxisLabel};

                RegisterEvents(barChartPlotter);

                foreach (var dataPoint in barChartDataByIndex)
                {
                    var label = barChartLabelsByIndex[dataPoint.Key];
                    barChartPlotter.AddData(label, dataPoint.Value);
                }

                var success = barChartPlotter.SavePlotFile(datasetName, outputDirectory);

                return success;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.CreateBarChart", ex);
                return false;
            }
        }

        private bool CreateBarCharts(string datasetName, string outputDirectory)
        {
            try
            {
                var highAbundanceTitle = string.Format("Reporter Ion Observation Rate (top {0}%)", Options.PlotOptions.ReporterIonObservationRateTopNPct);
                var reporterIonObservationRatePlotter = new BarChartPlotter(highAbundanceTitle);

                var highAbundanceReporterIonObservationRatePlotter = new BarChartPlotter("Reporter Ion Observation Rate");

                RegisterEvents(reporterIonObservationRatePlotter);
                RegisterEvents(highAbundanceReporterIonObservationRatePlotter);

                var success1 = CreateBarChart(
                    mStatsSummarizer.ReporterIonNames,
                    mStatsSummarizer.ReporterIonObservationRateHighAbundance,
                    datasetName,
                    outputDirectory,
                    highAbundanceTitle,
                    "RepIonObsRateHighAbundance",
                    "Observation Rate (%)");

                var success2 = CreateBarChart(
                    mStatsSummarizer.ReporterIonNames,
                    mStatsSummarizer.ReporterIonObservationRate,
                    datasetName,
                    outputDirectory,
                    "Reporter Ion Observation Rate",
                    "RepIonObsRate",
                    "Observation Rate (%)");

                return success1 && success2;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.CreateBarCharts", ex);
                return false;
            }
        }

        private bool CreatePlots(string datasetName, string outputDirectory)
        {
            var barChartSuccess = CreateBarCharts(datasetName, outputDirectory);
            var histogramSuccess = CreateHistograms(datasetName, outputDirectory);

            return barChartSuccess && histogramSuccess;
        }

        /// <summary>
        /// Read the SIC stats file (and optionally reporter ions file)
        /// Generate stats, then create plots
        /// </summary>
        /// <returns></returns>
        public bool ProcessFile(string sicStatsFilePath, string outputDirectory)
        {
            try
            {
                var statsSummarized = mStatsSummarizer.SummarizeSICStats(sicStatsFilePath);
                if (!statsSummarized)
                    return false;

                var datasetName = clsUtilities.ReplaceSuffix(Path.GetFileName(sicStatsFilePath), clsDataOutput.SIC_STATS_FILE_SUFFIX, string.Empty);

                var plotsGenerated = CreatePlots(datasetName, outputDirectory);

                return plotsGenerated;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.ProcessFile", ex);
                return false;
            }
        }
    }
}
