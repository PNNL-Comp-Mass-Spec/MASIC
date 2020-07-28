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
                var histogramPlotter = new HistogramPlotter(Options.PlotOptions, plotTitle)
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
                    "PeakAreaHistogram",
                    "Peak Area (Log 10)",
                    "Count");


                var peakWidthSuccess = CreateHistogram(
                    mStatsSummarizer.PeakWidthHistogram,
                    datasetName,
                    outputDirectory,
                    "Peak Width Histogram",
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
            string plotTitle,
            string plotAbbreviation,
            string yAxisLabel,
            int yAxisMinimum = 0)
        {
            try
            {
                var barChartPlotter = new BarChartPlotter(Options.PlotOptions, plotTitle) {
                    PlotAbbrev = plotAbbreviation,
                    YAxisLabel = yAxisLabel};

                RegisterEvents(barChartPlotter);

                foreach (var dataPoint in barChartDataByIndex)
                {
                    var label = barChartLabelsByIndex[dataPoint.Key];
                    barChartPlotter.AddData(label, dataPoint.Value);
                }

                var success = barChartPlotter.SavePlotFile(datasetName, outputDirectory, yAxisMinimum);

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
                var reporterIonObservationRatePlotter = new BarChartPlotter(Options.PlotOptions, highAbundanceTitle);

                var highAbundanceReporterIonObservationRatePlotter = new BarChartPlotter(Options.PlotOptions, "Reporter Ion Observation Rate");

                RegisterEvents(reporterIonObservationRatePlotter);
                RegisterEvents(highAbundanceReporterIonObservationRatePlotter);

                var success1 = CreateBarChart(
                    mStatsSummarizer.ReporterIonNames,
                    mStatsSummarizer.ReporterIonObservationRateHighAbundance,
                    datasetName,
                    outputDirectory,
                    highAbundanceTitle,
                    "RepIonObsRateHighAbundance",
                    "Observation Rate (%)",
                    Options.PlotOptions.ReporterIonTopNPctObsRateYAxisMinimum);

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

                var plotDataSaved = SavePlotData(datasetName, outputDirectory);

                return plotsGenerated && plotDataSaved;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.ProcessFile", ex);
                return false;
            }
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
            bool obsRateSuccess;

            if (Options.PlotOptions.SaveHistogramData)
            {
                histogramSuccess = SaveHistogramData(datasetName, outputDirectory);
            }
            else
            {
                histogramSuccess = true;
            }

            if (Options.PlotOptions.SaveReporterIonObservationRateData)
            {
                obsRateSuccess = SaveReporterIonObservationRateData(datasetName, outputDirectory);
            }
            else
            {
                obsRateSuccess = true;
            }

            return histogramSuccess && obsRateSuccess;
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
