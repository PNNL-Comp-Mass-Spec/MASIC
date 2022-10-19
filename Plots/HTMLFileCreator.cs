using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MASIC.DataOutput;
using MASIC.Options;
using PRISM;

namespace MASIC.Plots
{
    internal class HTMLFileCreator : EventNotifier
    {
        // Ignore Spelling: href, html

        public string DatasetName { get; }

        public PlotOptions Options { get; }

        public List<PlotFileInfo> PlotFiles { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="datasetName"></param>
        /// <param name="options"></param>
        /// <param name="plotFiles"></param>
        public HTMLFileCreator(string datasetName, PlotOptions options, List<PlotFileInfo> plotFiles)
        {
            DatasetName = datasetName;
            Options = options;
            PlotFiles = plotFiles;
        }

        /// <summary>
        /// Create the index.html file in the directory the same directory as the first item in PlotFiles
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public bool CreateHTMLFile()
        {
            return CreateHTMLFile(string.Empty);
        }

        /// <summary>
        /// Create the index.html file in the specified directory
        /// </summary>
        /// <param name="outputDirectoryPath"></param>
        public bool CreateHTMLFile(string outputDirectoryPath)
        {
            try
            {
                if (PlotFiles.Count == 0)
                {
                    OnWarningEvent("No plot files defined; cannot create the index.html file");

                    // Return true since this is not a fatal error
                    return true;
                }

                if (string.IsNullOrWhiteSpace(outputDirectoryPath))
                {
                    outputDirectoryPath = PlotFiles.First().PlotFile.DirectoryName;
                }

                var outputDirectory = new DirectoryInfo(outputDirectoryPath ?? ".");
                if (!outputDirectory.Exists)
                {
                    outputDirectory.Create();
                }

                var outputFilePath = Path.Combine(outputDirectory.FullName, "index.html");

                using var writer = new StreamWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read));

                // Add HTML headers and <table>
                AppendHTMLHeader(writer, DatasetName);

                // Add the SIC peak stats histograms (X vs. Y plots)
                AppendPlots(writer, PlotContainerBase.PlotCategories.SelectedIonChromatogramPeakStats);

                // Add the bar charts (if defined)
                AppendPlots(writer, PlotContainerBase.PlotCategories.ReporterIonObservationRate);

                // Add the reporter ion intensity stats box plot and histogram of observation count by channel (if defined)
                var intensityStatPlotCount = AppendPlots(
                    writer,
                    PlotContainerBase.PlotCategories.ReporterIonIntensityStats,
                    DatasetName,
                    outputDirectoryPath);

                // If this dataset has reporter ions, the ReporterIonIntensityStats row will include a plot on the left and a link to the dataset on the right
                // Otherwise, if there are no reporter ions, we need to append another row with a link to DMS
                if (intensityStatPlotCount == 0)
                {
                    AppendDatasetInfo(writer, DatasetName, outputDirectoryPath);
                }

                // Add </table> and HTML footers
                AppendHTMLFooter(writer);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in Plots.HTMLFileCreator.CreateHTMLFile", ex);
                return false;
            }
        }

        /// <summary>
        /// Append plots of the given type
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="plotCategory"></param>
        /// <param name="datasetName"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <returns>Number of plots appended</returns>
        private int AppendPlots(
            TextWriter writer,
            PlotContainerBase.PlotCategories plotCategory,
            string datasetName = "",
            string outputDirectoryPath = "")
        {
            var matchingPlotFiles = new List<PlotFileInfo>();

            foreach (var plotFile in PlotFiles)
            {
                if (plotFile.PlotCategory != plotCategory)
                    continue;

                matchingPlotFiles.Add(plotFile);
            }

            if (matchingPlotFiles.Count == 0)
                return 0;

            writer.WriteLine("    <tr>");

            foreach (var plotFile in matchingPlotFiles)
            {
                writer.WriteLine("      <td>" + GeneratePlotHTML(plotFile, 425) + "</td>");
            }

            if (plotCategory == PlotContainerBase.PlotCategories.ReporterIonIntensityStats)
            {
                var datasetDetailReportLink = GetDatasetDetailReportLink(datasetName);
                var reporterIonDataFileLinks = GetReporterIonDataFileLinks(datasetName, outputDirectoryPath);

                writer.Write("      <td class=\"Links\">" + datasetDetailReportLink);
                if (reporterIonDataFileLinks.Length > 0)
                {
                    writer.WriteLine("<br><br>" + reporterIonDataFileLinks);
                }
                else
                {
                    writer.WriteLine();
                }
            }

            writer.WriteLine("    </tr>");
            writer.WriteLine();

            return matchingPlotFiles.Count;
        }

        private void AppendHTMLHeader(TextWriter writer, string datasetName)
        {
            // ReSharper disable once StringLiteralTypo
            writer.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD HTML 3.2//EN\">");
            writer.WriteLine("<html>");
            writer.WriteLine("<head>");
            writer.WriteLine("  <title>" + datasetName + "</title>");
            writer.WriteLine("  <style>");
            writer.WriteLine("    table.DataTable {");
            writer.WriteLine("      margin: 10px 5px 5px 5px;");

            // Option 1: tight, with a border
            // writer.WriteLine("      border: 1px solid black;");
            // writer.WriteLine("      border-collapse: collapse;");

            // Option 2: no border, normal spacing
            writer.WriteLine("      border: none;");
            writer.WriteLine("      border-collapse: separate;");

            writer.WriteLine("    }");
            writer.WriteLine("    ");
            writer.WriteLine("    td.Links {");
            writer.WriteLine("      border: none;");
            writer.WriteLine("      padding: 2px 4px 2px 20px;");
            writer.WriteLine("    }");
            writer.WriteLine("        ");
            writer.WriteLine("    td.LinksCentered {");
            writer.WriteLine("      border: none;");
            writer.WriteLine("      padding: 2px 4px 2px 4px;");
            writer.WriteLine("      text-align: center;");
            writer.WriteLine("    }");
            writer.WriteLine("   </style>");
            writer.WriteLine("</head>");
            writer.WriteLine();
            writer.WriteLine("<body>");
            writer.WriteLine("  <h2>" + datasetName + "</h2>");
            writer.WriteLine();
            writer.WriteLine("  <table class=\"DataTable\">");
        }

        private void AppendDatasetInfo(TextWriter writer, string datasetName, string outputDirectoryPath)
        {
            var datasetDetailReportLink = GetDatasetDetailReportLink(datasetName);
            var reporterIonDataFileLinks = GetReporterIonDataFileLinks(datasetName, outputDirectoryPath);

            writer.WriteLine("    <tr>");
            writer.WriteLine("      <td class=\"LinksCentered\">{0}</td>", datasetDetailReportLink);
            writer.WriteLine("      <td class=\"Links\">{0}</td>", reporterIonDataFileLinks);
            writer.WriteLine("    </tr>");
        }

        private void AppendHTMLFooter(TextWriter writer)
        {
            writer.WriteLine();
            writer.WriteLine("  </table>");
            writer.WriteLine();
            writer.WriteLine("</body>");
            writer.WriteLine("</html>");
            writer.WriteLine();
        }

        private string GeneratePlotHTML(PlotFileInfo plotFile, int widthPixels)
        {
            if (plotFile.PlotFile == null)
            {
                return string.Empty;
            }

            return string.Format(
                "<a href=\"{0}\"><img src=\"{0}\" width=\"{1}\" border=\"0\" alt=\"{2}\"></a>",
                plotFile.PlotFile.Name,
                widthPixels,
                plotFile.FileDescription);
        }

        private string GetDatasetDetailReportLink(string datasetName)
        {
            return string.Format("DMS <a href=\"http://dms2.pnl.gov/dataset/show/{0}\">Dataset Detail Report</a>", datasetName);
        }

        private string GetFileUrlIfExists(string outputDirectoryPath, string fileName, string fileDescription)
        {
            var dataFile = new FileInfo(Path.Combine(outputDirectoryPath, fileName));

            return dataFile.Exists ?
                       string.Format("<a href=\"{0}\">{1}</a>", dataFile.Name, fileDescription) :
                       string.Empty;
        }

        private string GetReporterIonDataFileLinks(string datasetName, string outputDirectoryPath)
        {
            // Link to the tab-delimited text files
            var obsRateURL = GetFileUrlIfExists(
                outputDirectoryPath,
                datasetName + "_" + StatsPlotter.REPORTER_ION_OBSERVATION_RATE_DATA_FILE_SUFFIX,
                "Reporter Ion Observation Rate data file");

            var intensityStatsURL = GetFileUrlIfExists(
                outputDirectoryPath,
                datasetName + "_" + StatsPlotter.REPORTER_ION_INTENSITY_STATS_FILE_SUFFIX,
                "Reporter Ion Intensity Stats data file");

            if (obsRateURL.Length == 0 && intensityStatsURL.Length == 0)
                return string.Empty;

            string breakTag;
            if (obsRateURL.Length > 0 && intensityStatsURL.Length > 0)
                breakTag = "<br>";
            else
                breakTag = string.Empty;

            return obsRateURL + breakTag + intensityStatsURL;
        }
    }
}
