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
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public bool CreateHTMLFile()
        {
            return CreateHTMLFile(string.Empty);
        }

        /// <summary>
        /// Create the index.html file in the specified directory
        /// </summary>
        /// <param name="outputDirectoryPath"></param>
        /// <returns></returns>
        public bool CreateHTMLFile(string outputDirectoryPath)
        {
            try
            {

                if (PlotFiles.Count == 0)
                {
                    OnWarningEvent("No plot files defined; cannot create the index.html file");
                    return false;
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

                using (var writer = new StreamWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    // Add HTML headers and <table>
                    AppendHTMLHeader(writer, DatasetName);

                    // Add the histograms (X vs. Y plots)
                    AppendPlots(writer, PlotContainerBase.PlotTypes.XY);

                    // Add the bar charts (if defined)
                    AppendPlots(writer, PlotContainerBase.PlotTypes.BarChart);

                    // Add the box plots (if defined)
                    AppendPlots(writer, PlotContainerBase.PlotTypes.BoxPlot);

                    // Append dataset info
                    AppendDatasetInfo(writer, DatasetName, outputDirectoryPath);

                    // Add </table> and HTML footers
                    AppendHTMLFooter(writer);
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in Plots.HTMLFileCreator.CreateHTMLFile", ex);
                return false;
            }

        }

        private void AppendPlots(TextWriter writer, PlotContainerBase.PlotTypes plotType)
        {
            var matchingPlotFiles = new List<PlotFileInfo>();

            foreach (var plotFile in PlotFiles)
            {
                if (plotFile.PlotType != plotType)
                    continue;
                matchingPlotFiles.Add(plotFile);
            }

            if (matchingPlotFiles.Count == 0)
                return;

            writer.WriteLine("    <tr>");

            foreach (var plotFile in matchingPlotFiles)
            {
                writer.WriteLine("      <td>" + GeneratePlotHTML(plotFile, 425) + "</td>");
            }

            writer.WriteLine("    </tr>");
            writer.WriteLine();
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
            writer.WriteLine("      border: 1px solid black;");
            writer.WriteLine("      border-collapse: collapse;");
            writer.WriteLine("    }");
            writer.WriteLine("    ");
            writer.WriteLine("    th.DataHead {");
            writer.WriteLine("      border: 1px solid black;");
            writer.WriteLine("      padding: 2px 4px 2px 2px; ");
            writer.WriteLine("      text-align: left;");
            writer.WriteLine("    }");
            writer.WriteLine("    ");
            writer.WriteLine("    td.DataCell {");
            writer.WriteLine("      border: 1px solid black;");
            writer.WriteLine("      padding: 2px 4px 2px 4px;");
            writer.WriteLine("    }");
            writer.WriteLine("        ");
            writer.WriteLine("    td.DataCentered {");
            writer.WriteLine("      border: 1px solid black;");
            writer.WriteLine("      padding: 2px 4px 2px 4px;");
            writer.WriteLine("      text-align: center;");
            writer.WriteLine("    }");
            writer.WriteLine("   </style>");
            writer.WriteLine("</head>");
            writer.WriteLine();
            writer.WriteLine("<body>");
            writer.WriteLine("  <h2>" + datasetName + "</h2>");
            writer.WriteLine();
            writer.WriteLine("  <table>");
        }

        private void AppendDatasetInfo(TextWriter writer, string datasetName, string outputDirectoryPath)
        {
            writer.WriteLine("    <tr>");
            writer.WriteLine("      <td align=\"center\">DMS <a href=\"http://dms2.pnl.gov/dataset/show/" + datasetName + "\">Dataset Detail Report</a></td>");

            // Link to the _RepIonObsRate.txt file
            var reporterIonObsRateDataFile = datasetName + "_" + StatsPlotter.REPORTER_ION_OBSERVATION_RATE_DATA_FILE_SUFFIX;
            if (File.Exists(Path.Combine(outputDirectoryPath, reporterIonObsRateDataFile)))
            {
                writer.WriteLine("      <td align=\"center\"><a href=\"" + reporterIonObsRateDataFile + "\">Reporter Ion Observation Rate data file</a></td>");
            }
            else
            {
                writer.WriteLine("      <td>&nbsp;</td>");
            }

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
                return "&nbsp;";
            }

            var hyperlink = string.Format(
                "<a href=\"{0}\"><img src=\"{0}\" width=\"{1}\" border=\"0\" alt={2}></a>",
                plotFile.PlotFile.Name,
                widthPixels,
                plotFile.FileDescription);

            return hyperlink;
        }

    }
}
