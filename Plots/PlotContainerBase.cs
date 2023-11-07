using System;
using System.Collections.Generic;
using System.IO;
using PRISM;

namespace MASIC.Plots
{
    /// <summary>
    /// Plot container base class
    /// </summary>
    public abstract class PlotContainerBase : EventNotifier
    {
        // Ignore Spelling: yyyy-MM-dd hh:mm:ss

        /// <summary>
        /// Plot types
        /// </summary>
        public enum PlotTypes
        {
            /// <summary>
            /// X vs. Y plots (aka histograms)
            /// </summary>
            XY = 0,

            /// <summary>
            /// Bar chart of reporter ion observation rates
            /// </summary>
            BarChart = 1,

            /// <summary>
            /// Box and whiskers plot of reporter ion intensities
            /// </summary>
            BoxPlot = 2
        }

        /// <summary>
        /// Plot categories
        /// </summary>
        public enum PlotCategories
        {
            /// <summary>
            /// Undefined
            /// </summary>
            Undefined = 0,

            /// <summary>
            /// X vs. Y plots (aka histograms)
            /// </summary>
            /// <remarks>Type PlotTypes.XY</remarks>
            SelectedIonChromatogramPeakStats = 1,

            /// <summary>
            /// Bar chart of reporter ion observation rates
            /// </summary>
            /// <remarks>Type PlotTypes.BarChart</remarks>
            ReporterIonObservationRate = 2,

            /// <summary>
            /// Box and whiskers plot of reporter ion intensities
            /// </summary>
            /// <remarks>Type PlotTypes.BoxPlot</remarks>
            ReporterIonIntensityStats = 3
        }

        /// <summary>
        /// Log writer
        /// </summary>
        protected StreamWriter mLogWriter;

        /// <summary>
        /// Bottom left annotation
        /// </summary>
        public string AnnotationBottomLeft { get; set; }

        /// <summary>
        /// Bottom right annotation
        /// </summary>
        public string AnnotationBottomRight { get; set; }

        /// <summary>
        /// Plot title
        /// </summary>
        public string PlotTitle { get; set; }

        /// <summary>
        /// Plot category
        /// </summary>
        public PlotCategories PlotCategory { get; protected set; }

        /// <summary>
        /// Series count
        /// </summary>
        public abstract int SeriesCount { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="writeDebug"></param>
        /// <param name="dataSource"></param>
        protected PlotContainerBase(bool writeDebug = false, string dataSource = "")
        {
            AnnotationBottomLeft = string.Empty;
            AnnotationBottomRight = string.Empty;
            PlotTitle = "Undefined Plot Title";
            PlotCategory = PlotCategories.Undefined;

            if (writeDebug)
            {
                OpenDebugFile(dataSource);
            }
        }

        /// <summary>
        /// Dispose of the log writer
        /// </summary>
        /// <param name="disposing"></param>
        // ReSharper disable once UnusedMember.Global
        protected virtual void Dispose(bool disposing)
        {
            mLogWriter?.Close();
        }

        /// <summary>
        /// Get semicolon separated list of plot options
        /// </summary>
        /// <remarks>
        /// Example options:
        /// PlotType=XY;Title=PlotTitle;Percentages=False;BottomLeft=Annotation1;BottomRight=Annotation2;
        /// </remarks>
        protected string GetPlotOptions()
        {
            var percentagesFlag = PlotCategory == PlotCategories.ReporterIonObservationRate ? "True" : "False";

            var plotOptions = new List<string> {
                "PlotType=" + GetPlotTypeForCategory(PlotCategory),
                "Title=" + PlotTitle,
                "Percentages=" + percentagesFlag,
                "BottomLeft=" + AnnotationBottomLeft,
                "BottomRight=" + AnnotationBottomRight
            };

            return string.Join(";", plotOptions);
        }

        /// <summary>
        /// Get the plot type based on the plot category
        /// </summary>
        /// <param name="plotCategory"></param>
        protected PlotTypes GetPlotTypeForCategory(PlotCategories plotCategory)
        {
            return plotCategory switch
            {
                PlotCategories.Undefined or PlotCategories.SelectedIonChromatogramPeakStats => PlotTypes.XY,
                PlotCategories.ReporterIonObservationRate => PlotTypes.BarChart,
                PlotCategories.ReporterIonIntensityStats => PlotTypes.BoxPlot,
                _ => throw new ArgumentOutOfRangeException(nameof(plotCategory), plotCategory, null),
            };
        }

        /// <summary>
        /// Open a debug log file
        /// </summary>
        /// <param name="dataSource"></param>
        protected void OpenDebugFile(string dataSource)
        {
            var appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            var logDirectory = AppUtils.GetAppDataDirectoryPath(appName);

            string logFileName;

            if (string.IsNullOrWhiteSpace(dataSource))
            {
                logFileName = "MASIC_Plotter_Debug.txt";
            }
            else
            {
                logFileName = dataSource + ".txt";
            }

            var logFile = new FileInfo(Path.Combine(logDirectory, logFileName));
            var addBlankLink = logFile.Exists;

            mLogWriter = new StreamWriter(new FileStream(logFile.FullName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                AutoFlush = true
            };

            if (addBlankLink)
                mLogWriter.WriteLine();
        }

        /// <summary>
        /// Abstract method for saving the plot as a PNG file
        /// </summary>
        /// <param name="pngFile"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="resolution"></param>
        public abstract bool SaveToPNG(FileInfo pngFile, int width, int height, int resolution);

        /// <summary>
        /// Append a line to the log file
        /// </summary>
        /// <param name="message"></param>
        public void WriteDebugLog(string message)
        {
            mLogWriter?.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + ": " + message);
        }
    }
}