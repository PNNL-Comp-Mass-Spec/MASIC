using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MASIC.Options;
using OxyPlot;
using OxyPlot.Series;
using PRISM;

namespace MASIC.Plots
{
    public class HistogramPlotter : EventNotifier
    {

        #region "Member variables"

        private readonly HistogramInfo mHistogram;

        private readonly bool mWriteDebug;

        #endregion

        #region "Properties"

        /// <summary>
        /// When true, autoscale the Y axis
        /// </summary>
        public bool AutoMinMaxY { get; set; }

        /// <summary>
        /// This name is used when creating the .png file
        /// </summary>
        public string PlotAbbrev { get; set; } = "Histogram";

        /// <summary>
        /// Plot options
        /// </summary>
        public PlotOptions Options { get; }

        /// <summary>
        /// Plot title
        /// </summary>
        public string PlotTitle { get; set; }

        /// <summary>
        /// X-axis label
        /// </summary>
        public string XAxisLabel { get; set; } = "Bin";

        /// <summary>
        /// y-axis label
        /// </summary>
        public string YAxisLabel { get; set; } = "Bin Count";

        /// <summary>
        /// When true, remove zeroes from the start and end of the data
        /// </summary>
        public bool RemoveZeroesFromEnds { get; set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="plotTitle"></param>
        /// <param name="writeDebug"></param>
        public HistogramPlotter(PlotOptions options, string plotTitle, bool writeDebug = false)
        {
            Options = options;
            PlotTitle = plotTitle;
            mHistogram = new HistogramInfo();
            mWriteDebug = writeDebug;
            Reset();
        }

        public void AddData(double bin, int binCount)
        {
            mHistogram.AddPoint(bin, binCount);
        }

        private void AddOxyPlotSeries(PlotModel myPlot, IReadOnlyCollection<DataPoint> points)
        {
            // Generate a black curve with no symbols
            var series = new LineSeries();

            if (points.Count <= 0)
            {
                return;
            }

            var eSymbolType = MarkerType.None;
            if (points.Count == 1)
            {
                eSymbolType = MarkerType.Circle;
            }

            series.Color = OxyColors.Black;
            series.StrokeThickness = 2;
            series.MarkerType = eSymbolType;

            if (points.Count == 1)
            {
                series.MarkerSize = 8;
                series.MarkerFill = OxyColors.DarkRed;
            }

            series.Points.AddRange(points);

            myPlot.Series.Add(series);
        }
        private List<DataPoint> GetDataToPlot(
            HistogramInfo histogramInfo,
            out double minBin,
            out double maxBin,
            out double maxIntensity)
        {
            minBin = double.MaxValue;
            maxBin = double.MinValue;
            maxIntensity = 0;

            // Instantiate the list to track the data points
            var points = new List<DataPoint>();

            foreach (var dataPoint in histogramInfo.Data)
            {

                points.Add(new DataPoint(dataPoint.Bin, dataPoint.BinCount));

                if (dataPoint.Bin < minBin)
                {
                    minBin = dataPoint.Bin;
                }

                if (dataPoint.Bin > maxBin)
                {
                    maxBin = dataPoint.Bin;
                }

                if (dataPoint.BinCount > maxIntensity)
                {
                    maxIntensity = dataPoint.BinCount;
                }
            }

            // Round maxBin down to the nearest multiple of 10
            maxBin = (int)Math.Ceiling(maxBin / 10.0) * 10;

            // Multiply maxIntensity by 2% and then round up to the nearest integer
            maxIntensity = Math.Ceiling(maxIntensity * 1.02);

            return points;
        }

        private PlotContainerBase InitializePlot(
            HistogramInfo histogramInfo,
            string plotTitle,
            string xAxisLabel,
            AxisInfo yAxisInfo)
        {
            if (Options.PlotWithPython)
            {
                return InitializePythonPlot(histogramInfo, plotTitle, xAxisLabel, yAxisInfo);
            }

            return InitializeOxyPlot(histogramInfo, plotTitle, xAxisLabel, yAxisInfo);
        }

        /// <summary>
        /// Initialize an OxyPlot plot container for a histogram
        /// </summary>
        /// <param name="histogramInfo">Data to display</param>
        /// <param name="plotTitle">Title of the plot</param>
        /// <param name="xAxisLabel"></param>
        /// <param name="yAxisInfo"></param>
        /// <returns>OxyPlot PlotContainer</returns>
        private PlotContainer InitializeOxyPlot(
            HistogramInfo histogramInfo,
            string plotTitle,
            string xAxisLabel,
            AxisInfo yAxisInfo)
        {

            var points = GetDataToPlot(histogramInfo, out var minBin, out var maxBin, out var maxIntensity);

            if (points.Count == 0)
            {
                // Nothing to plot
                var emptyContainer = new PlotContainer(PlotContainerBase.PlotTypes.XY, new PlotModel(), mWriteDebug);
                emptyContainer.WriteDebugLog("points.Count == 0 in InitializeOxyPlot for plot " + plotTitle);
                return emptyContainer;
            }

            var myPlot = OxyPlotUtilities.GetBasicPlotModel(
                plotTitle, xAxisLabel, yAxisInfo);

            AddOxyPlotSeries(myPlot, points);

            // Update the axis format codes if the data values are small or the range of data is small
            var xVals = (from item in points select item.X).ToList();
            OxyPlotUtilities.UpdateAxisFormatCodeIfSmallValues(myPlot.Axes[0], xVals, true);

            var yVals = (from item in points select item.Y).ToList();
            OxyPlotUtilities.UpdateAxisFormatCodeIfSmallValues(myPlot.Axes[1], yVals, false);

            var plotContainer = new PlotContainer(PlotContainerBase.PlotTypes.XY, myPlot, mWriteDebug)
            {
                FontSizeBase = PlotContainer.DEFAULT_BASE_FONT_SIZE
            };

            plotContainer.WriteDebugLog(string.Format("Instantiated plotContainer for plot {0}: {1} data points", plotTitle, points.Count));

            // Override the auto-computed X axis range
            if (Math.Abs(minBin - maxBin) < float.Epsilon)
            {
                myPlot.Axes[0].Minimum = minBin - 1;
                myPlot.Axes[0].Maximum = minBin + 1;
            }
            else
            {
                myPlot.Axes[0].Minimum = 0;

                if (Math.Abs(maxBin) < float.Epsilon)
                {
                    myPlot.Axes[0].Maximum = 1;
                }
                else
                {
                    myPlot.Axes[0].Maximum = maxBin;
                }
            }

            // Assure that we don't see ticks between scan numbers
            OxyPlotUtilities.ValidateMajorStep(myPlot.Axes[0]);

            if (yAxisInfo.AutoScale)
            {
                // Auto scale
            }
            else
            {
                // Override the auto-computed Y axis range
                myPlot.Axes[1].Minimum = 0;
                myPlot.Axes[1].Maximum = maxIntensity;
            }

            // Hide the legend
            myPlot.IsLegendVisible = false;

            return plotContainer;
        }

        /// <summary>
        /// Initialize a Python plot container for a histogram
        /// </summary>
        /// <param name="histogramInfo">Data to display</param>
        /// <param name="plotTitle">Title of the plot</param>
        /// <param name="xAxisLabel"></param>
        /// <param name="yAxisInfo"></param>
        /// <returns>OxyPlot PlotContainer</returns>
        private PythonPlotContainer InitializePythonPlot(
            HistogramInfo histogramInfo,
            string plotTitle,
            string xAxisLabel,
            AxisInfo yAxisInfo)
        {

            var points = GetDataToPlot(histogramInfo, out _, out _, out var maxIntensity);

            if (points.Count == 0)
            {
                // Nothing to plot
                var emptyContainer = new PythonPlotContainerXY();
                emptyContainer.WriteDebugLog("points.Count == 0 in InitializeOxyPlot for plot " + plotTitle);
                return emptyContainer;
            }

            var plotContainer = new PythonPlotContainerXY(plotTitle, xAxisLabel, yAxisInfo.Title)
            {
                DeleteTempFiles = Options.DeleteTempFiles
            };

            plotContainer.SetData(points);

            if (yAxisInfo.AutoScale)
            {
                // Auto scale
            }
            else
            {
                // Override the auto-computed Y axis range
                plotContainer.XAxisInfo.SetRange(0, maxIntensity);
            }

            return plotContainer;
        }

        private void RemoveZeroesAtFrontAndBack(HistogramInfo histogramInfo)
        {
            const int MAX_POINTS_TO_CHECK = 100;
            var pointsChecked = 0;

            // See if the last few values are zero, but the data before them is non-zero
            // If this is the case, remove the final entries

            var indexNonZeroValue = -1;
            var zeroPointCount = 0;
            for (var index = histogramInfo.DataCount - 1; index >= 0; index += -1)
            {
                if (Math.Abs(histogramInfo.GetDataPoint(index).BinCount) < float.Epsilon)
                {
                    zeroPointCount += 1;
                }
                else
                {
                    indexNonZeroValue = index;
                    break;
                }
                pointsChecked += 1;
                if (pointsChecked >= MAX_POINTS_TO_CHECK)
                    break;
            }

            if (zeroPointCount > 0 && indexNonZeroValue >= 0)
            {
                histogramInfo.RemoveRange(indexNonZeroValue + 1, zeroPointCount);
            }

            // Now check the first few values
            indexNonZeroValue = -1;
            zeroPointCount = 0;
            for (var index = 0; index <= histogramInfo.DataCount - 1; index++)
            {
                if (Math.Abs(histogramInfo.GetDataPoint(index).BinCount) < float.Epsilon)
                {
                    zeroPointCount += 1;
                }
                else
                {
                    indexNonZeroValue = index;
                    break;
                }
                pointsChecked += 1;
                if (pointsChecked >= MAX_POINTS_TO_CHECK)
                    break;
            }

            if (zeroPointCount > 0 && indexNonZeroValue >= 0)
            {
                histogramInfo.RemoveRange(0, indexNonZeroValue);
            }

        }

        /// <summary>
        /// Clear histogram data
        /// </summary>
        public void Reset()
        {
            mHistogram.Initialize();
        }

        /// <summary>
        /// Save the plot to disk
        /// </summary>
        /// <param name="datasetName"></param>
        /// <param name="outputDirectory"></param>
        /// <returns>True if success, false if an error</returns>
        public bool SavePlotFile(string datasetName, string outputDirectory)
        {

            try
            {

                if (RemoveZeroesFromEnds)
                {
                    // Check whether the last few scans have values if 0; if they do, remove them
                    RemoveZeroesAtFrontAndBack(mHistogram);
                }

                var yAxisInfo = new AxisInfo(YAxisLabel)
                {
                    Title = YAxisLabel,
                    AutoScale = AutoMinMaxY
                };

                var histogramPlot = InitializePlot(mHistogram, PlotTitle, XAxisLabel, yAxisInfo);
                RegisterEvents(histogramPlot);

                if (histogramPlot.SeriesCount <= 0)
                {
                    // We'll treat this as success
                    return true;
                }

                var pngFile = new FileInfo(Path.Combine(outputDirectory, datasetName + "_" + PlotAbbrev + ".png"));

                var success = histogramPlot.SaveToPNG(pngFile, 1024, 600, 96);
                return success;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in SavePlotFile", ex);
                return false;
            }

        }

        private class HistogramDataPoint
        {
            public double Bin { get; set; }
            public int BinCount { get; set; }

            public override string ToString()
            {
                return string.Format("{0:F2}: {1}", Bin, BinCount);
            }
        }

        private class HistogramInfo
        {

            public int DataCount => mData.Count;

            public IEnumerable<HistogramDataPoint> Data => mData;

            private readonly List<HistogramDataPoint> mData;

            private readonly SortedSet<double> mBinValues;

            /// <summary>
            /// Constructor
            /// </summary>
            public HistogramInfo()
            {
                mData = new List<HistogramDataPoint>();
                mBinValues = new SortedSet<double>();
            }

            public void AddPoint(double bin, int binCount)
            {
                if (mBinValues.Contains(bin))
                {
                    throw new Exception("Bin " + bin + " has already been added; programming error");
                }

                var dataPoint = new HistogramDataPoint
                {
                    Bin = bin,
                    BinCount = binCount
                };

                mData.Add(dataPoint);
                mBinValues.Add(bin);
            }

            public HistogramDataPoint GetDataPoint(int index)
            {
                if (mData.Count == 0)
                {
                    throw new Exception("Histogram list is empty; cannot retrieve data point at index " + index);
                }
                if (index < 0 || index >= mData.Count)
                {
                    throw new Exception("Histogram index out of range: " + index + "; should be between 0 and " + (mData.Count - 1));
                }

                return mData[index];
            }

            /// <summary>
            /// Clear cached data
            /// </summary>
            public void Initialize()
            {
                mData.Clear();
            }

            // ReSharper disable once UnusedMember.Local
            public void RemoveAt(int index)
            {
                RemoveRange(index, 1);
            }

            public void RemoveRange(int index, int count)
            {
                if (index < 0 || index >= DataCount || count <= 0)
                    return;

                var binsToRemove = new List<double>();
                var lastIndex = Math.Min(index + count, mData.Count) - 1;

                for (var i = index; i <= lastIndex; i++)
                {
                    binsToRemove.Add(mData[i].Bin);
                }

                mData.RemoveRange(index, count);

                foreach (var scanNumber in binsToRemove)
                {
                    mBinValues.Remove(scanNumber);
                }
            }

            public override string ToString()
            {
                return string.Format("HistogramInfo: {0} points", DataCount);
            }
        }

    }
}

