using System;
using System.Collections.Generic;
using System.IO;
using PRISM;
using PRISM.FileProcessor;

namespace MASIC.Plots
{
    public abstract class PlotContainerBase : EventNotifier
    {
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
            /// /// <remarks>Type PlotTypes.BoxPlot</remarks>
            ReporterIonIntensityStats = 3
        }

        protected StreamWriter mLogWriter;

        #region "Properties"

        public string AnnotationBottomLeft { get; set; }

        public string AnnotationBottomRight { get; set; }

        public string PlotTitle { get; set; }

        public PlotCategories PlotCategory { get; protected set; }

        public abstract int SeriesCount { get; }

        #endregion

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

        protected PlotTypes GetPlotTypeForCategory(PlotCategories plotCategory)
        {
            switch (plotCategory)
            {
                case PlotCategories.Undefined:
                case PlotCategories.SelectedIonChromatogramPeakStats:
                    return PlotTypes.XY;

                case PlotCategories.ReporterIonObservationRate:
                    return PlotTypes.BarChart;

                case PlotCategories.ReporterIonIntensityStats:
                    return PlotTypes.BoxPlot;

                default:
                    throw new ArgumentOutOfRangeException(nameof(plotCategory), plotCategory, null);
            }
        }

        protected void OpenDebugFile(string dataSource)
        {
            var appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            var logDirectory = ProcessFilesOrDirectoriesBase.GetAppDataDirectoryPath(appName);

            string logFileName;
            if (string.IsNullOrWhiteSpace(dataSource))
                logFileName = "MASIC_Plotter_Debug.txt";
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

        public abstract bool SaveToPNG(FileInfo pngFile, int width, int height, int resolution);

        public void WriteDebugLog(string message)
        {
            mLogWriter?.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + ": " + message);
        }

    }
}