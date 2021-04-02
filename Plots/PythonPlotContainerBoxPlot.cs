using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MASIC.Plots
{
    internal class PythonPlotContainerBoxPlot : PythonPlotContainer
    {
        public List<string> XAxisLabels { get; private set; }

        public List<List<double>> Data { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="plotCategory"></param>
        /// <param name="plotTitle"></param>
        /// <param name="xAxisTitle"></param>
        /// <param name="yAxisTitle"></param>
        /// <param name="writeDebug"></param>
        /// <param name="dataSource"></param>
        public PythonPlotContainerBoxPlot(
            PlotCategories plotCategory,
            string plotTitle = "Undefined",
            string xAxisTitle = "X",
            string yAxisTitle = "Y",
            bool writeDebug = false,
            string dataSource = "") : base(plotCategory, plotTitle, xAxisTitle, yAxisTitle, writeDebug, dataSource)
        {
            XAxisLabels = new List<string>();
            Data = new List<List<double>>();
            ClearData();
        }

        /// <summary>
        /// Save the plot, along with any defined annotations, to a png file
        /// </summary>
        /// <param name="pngFile">Output PNG file</param>
        /// <param name="width">PNG file width, in pixels</param>
        /// <param name="height">PNG file height, in pixels</param>
        /// <param name="resolution">Image resolution, in dots per inch</param>
        /// <remarks></remarks>
        public override bool SaveToPNG(FileInfo pngFile, int width, int height, int resolution)
        {
            if (pngFile == null)
                throw new ArgumentNullException(nameof(pngFile), "PNG file instance cannot be blank");

            var exportFile = new FileInfo(Path.ChangeExtension(pngFile.FullName, null) + TMP_FILE_SUFFIX + ".txt");

            try
            {
                using var writer = new StreamWriter(new FileStream(exportFile.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));

                // Plot options: set of square brackets with semicolon separated key/value pairs
                writer.WriteLine("[" + GetPlotOptions() + "]");

                // Column options: semicolon separated key/value pairs for each column, with options for each column separated by a tab
                // Note: these options aren't actually used by the Python plotting library

                // Example XAxis options: Autoscale=false;Minimum=0;Maximum=12135006;StringFormat=#,##0;MinorGridlineThickness=1
                // Example YAxis options: Autoscale=true;StringFormat=0.00E+00;MinorGridlineThickness=1

                writer.WriteLine("{0}\t{1}", XAxisInfo.GetOptions(), YAxisInfo.GetOptions());

                // Column names
                var xAxisTitle = string.IsNullOrWhiteSpace(XAxisInfo.Title) ? "Label" : XAxisInfo.Title;
                writer.WriteLine("{0}\t{1}", xAxisTitle, YAxisInfo.Title);

                var intensityValues = new StringBuilder();

                for (var i = 0; i < XAxisLabels.Count; i++)
                {
                    intensityValues.Clear();

                    // Data: the first column is the box label; the second column is a comma separated list of intensities for the box
                    for (var j =0 ; j < Data[i].Count; j++)
                    {
                        if (j > 0)
                            intensityValues.Append(",");

                        intensityValues.Append(Data[i][j]);
                    }

                    writer.WriteLine("{0}\t{1}", XAxisLabels[i], intensityValues);
                }
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error exporting data in SaveToPNG", ex);
                return false;
            }

            if (string.IsNullOrWhiteSpace(PythonPath) && !PythonInstalled)
            {
                NotifyPythonNotFound("Cannot export plot data for PNG creation");
                return false;
            }

            try
            {
                var success = GeneratePlotsWithPython(exportFile, pngFile.Directory);

                if (DeleteTempFiles)
                {
                    exportFile.Delete();
                }

                return success;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error creating box plot with Python using " + exportFile.Name, ex);
                return false;
            }
        }

        public void ClearData()
        {
            XAxisLabels.Clear();
            Data.Clear();
            mSeriesCount = 0;
        }

        public void SetData(List<string> xAxisLabels, List<List<double>> pointsByBox)
        {
            if (pointsByBox.Count == 0)
            {
                ClearData();
                return;
            }

            XAxisLabels = xAxisLabels;
            Data = pointsByBox;
            mSeriesCount = 1;
        }
    }
}
