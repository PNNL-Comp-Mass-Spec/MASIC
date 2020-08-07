using System;
using System.Collections.Generic;
using System.IO;
using PRISM;
using PRISM.FileProcessor;

namespace MASIC.Plots
{
    internal abstract class PlotContainerBase : EventNotifier
    {
        public enum PlotTypes
        {
            XY = 0,
            BarChart = 1,
            BoxPlot = 2
        }

        protected StreamWriter mLogWriter;

        #region "Properties"

        public string AnnotationBottomLeft { get; set; }

        public string AnnotationBottomRight { get; set; }

        public string PlotTitle { get; set; }

        public PlotTypes PlotType { get; protected set; }

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
            PlotType = PlotTypes.XY;

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
        /// <returns></returns>
        protected string GetPlotOptions()
        {
            var plotOptions = new List<string> {
                "PlotType=" + PlotType,
                "Title=" + PlotTitle,
                "BottomLeft=" + AnnotationBottomLeft,
                "BottomRight=" + AnnotationBottomRight
            };

            return string.Join(";", plotOptions);
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