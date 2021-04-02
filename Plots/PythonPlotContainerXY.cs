using System;
using System.Collections.Generic;
using System.IO;
using OxyPlot;

namespace MASIC.Plots
{
    /// <summary>
    /// Python data container for plotting scatter plots (x vs. y)
    /// </summary>
    internal class PythonPlotContainerXY : PythonPlotContainer
    {
        public List<DataPoint> Data { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="plotCategory"></param>
        /// <param name="plotTitle"></param>
        /// <param name="xAxisTitle"></param>
        /// <param name="yAxisTitle"></param>
        /// <param name="writeDebug"></param>
        /// <param name="dataSource"></param>
        public PythonPlotContainerXY(
            PlotCategories plotCategory,
            string plotTitle = "Undefined",
            string xAxisTitle = "X",
            string yAxisTitle = "Y",
            bool writeDebug = false,
            string dataSource = "") : base(plotCategory, plotTitle, xAxisTitle, yAxisTitle, writeDebug, dataSource)
        {
            Data = new List<DataPoint>();
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
                writer.WriteLine("{0}\t{1}", XAxisInfo.Title, YAxisInfo.Title);

                // Data
                foreach (var dataPoint in Data)
                {
                    writer.WriteLine("{0}\t{1}", dataPoint.X, dataPoint.Y);
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
                OnErrorEvent("Error creating XY plot with Python using " + exportFile.Name, ex);
                return false;
            }
        }

        public void ClearData()
        {
            Data.Clear();
            mSeriesCount = 0;
        }

        public void SetData(List<DataPoint> points)
        {
            if (points.Count == 0)
            {
                ClearData();
                return;
            }

            Data = points;
            mSeriesCount = 1;
        }
    }
}
