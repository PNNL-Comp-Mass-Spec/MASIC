using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MASIC.DataOutput;
using MASIC.Options;
using OxyPlot;
using OxyPlot.Series;
using PRISM;

namespace MASIC.Plots
{
    class BoxPlotPlotter : EventNotifier
    {

        #region "Member variables"

        private readonly BoxPlotInfo mBoxPlot;

        private readonly bool mWriteDebug;

        #endregion

        #region "Properties"

        /// <summary>
        /// Reporter ion column names
        /// </summary>
        /// <remarks>Keys are column index, values are reporter ion info</remarks>
        public Dictionary<int, string> ReporterIonNames => mBoxPlot.ReporterIonNames;

        /// <summary>
        /// Statistics for each box, by channel
        /// </summary>
        /// Keys are column index (corresponding to <see cref="ReporterIonNames"/>)
        /// Values are the stats for the given reporter ion
        public Dictionary<int, BoxPlotStats> BoxPlotStatistics => mBoxPlot.BoxPlotStatistics;

        /// <summary>
        /// This name is used when creating the .png file
        /// </summary>
        public string PlotAbbrev { get; set; } = "BoxPlot";

        /// <summary>
        /// Plot options
        /// </summary>
        public PlotOptions Options { get; }

        /// <summary>
        /// Y-axis label
        /// </summary>
        public string YAxisLabel { get; set; } = "Intensity";

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="plotTitle"></param>
        /// <param name="writeDebug"></param>
        public BoxPlotPlotter(PlotOptions options, string plotTitle, bool writeDebug = false)
        {
            Options = options;
            mBoxPlot = new BoxPlotInfo(plotTitle);
            mWriteDebug = writeDebug;
            Reset();
        }

        public void AddData(int labelIndex, string label, List<double> values)
        {
            mBoxPlot.AddPoints(labelIndex, label, values);
        }

        private void AddOxyPlotSeries(PlotModel myPlot, IEnumerable<double> points, out BoxPlotStats boxPlotStats)
        {
            boxPlotStats = new BoxPlotStats();

            var series = new BoxPlotSeries
            {
                Fill = OxyColor.FromRgb(0x1e, 0xb4, 0xda),
                StrokeThickness = 1.1,
                WhiskerWidth = 1
            };

            // Assure that the data is sorted
            var values = (from item in points orderby item select item).ToList();

            if (values.Count < 1)
            {
                boxPlotStats.Median = 0;
                boxPlotStats.FirstQuartile = 0;
                boxPlotStats.ThirdQuartile = 0;
            }
            else if (values.Count == 1)
            {
                boxPlotStats.Median = values[0];
                boxPlotStats.FirstQuartile = values[0];
                boxPlotStats.ThirdQuartile = values[0];
            }
            else
            {
                boxPlotStats.Median = GetMedian(values);

                var oddCountAddon = values.Count % 2;

                var firstQuartileCount = (values.Count + oddCountAddon) / 2;
                var thirdQuartilePreviousItemCount = (values.Count - oddCountAddon) / 2;

                // The first quartile (aka 25th percentile) is the median of the lower half of the dataset
                boxPlotStats.FirstQuartile = GetMedian(values.Take(firstQuartileCount).ToList());

                // The third quartile (aka the 75th percentile) is the is the median of the upper half of the dataset
                boxPlotStats.ThirdQuartile = GetMedian(values.Skip(thirdQuartilePreviousItemCount).ToList());
            }

            boxPlotStats.InterQuartileRange = boxPlotStats.ThirdQuartile - boxPlotStats.FirstQuartile;

            var whiskerAddon = 1.5 * boxPlotStats.InterQuartileRange;

            var upperWhiskerThreshold = boxPlotStats.ThirdQuartile + whiskerAddon;
            var upperValues = values.Where(item => item <= upperWhiskerThreshold).ToList();
            var upperWhisker = upperValues.Count == 0 ? 0 : upperValues.Max();

            var lowerWhiskerThreshold = boxPlotStats.FirstQuartile - whiskerAddon;
            var lowerValues = values.Where(item => item >= lowerWhiskerThreshold).ToList();
            var lowerWhisker = lowerValues.Count == 0 ? 0 : lowerValues.Min();

            var xValue = myPlot.Series.Count;

            var boxPlotItem = new BoxPlotItem(
                xValue,
                lowerWhisker,
                boxPlotStats.FirstQuartile,
                boxPlotStats.Median,
                boxPlotStats.ThirdQuartile,
                upperWhisker)
            {
                Outliers = values.Where(item => item > upperWhisker || item < lowerWhisker).ToList()
            };

            boxPlotStats.UpperWhisker = upperWhisker;
            boxPlotStats.LowerWhisker = lowerWhisker;

            boxPlotStats.NonZeroCount = values.Count(item => item > 0);
            boxPlotStats.NumberOfOutliers = boxPlotItem.Outliers.Count;

            series.Items.Add(boxPlotItem);

            myPlot.Series.Add(series);
        }

        /// <summary>
        /// Get data to plot
        /// </summary>
        /// <param name="boxPlotInfo"></param>
        /// <param name="pointsByBox">List of lists; each item is the list of values for the given box</param>
        /// <param name="maxIntensity"></param>
        /// <returns>Labels for the bars</returns>
        private Dictionary<int, string> GetDataToPlot(
            BoxPlotInfo boxPlotInfo,
            out List<List<double>> pointsByBox)
        {

            // Instantiate the list to track the data points
            pointsByBox = new List<List<double>>();
            var xAxisLabels = new Dictionary<int, string>();

            var maxIntensity = 0.0;

            foreach (var item in boxPlotInfo.DataPoints)
            {
                xAxisLabels.Add(item.Key, boxPlotInfo.ReporterIonNames[item.Key]);
                pointsByBox.Add(item.Value);

                if (item.Value.Count > 0)
                {
                    maxIntensity = Math.Max(maxIntensity, item.Value.Max());
                }
            }

            // Multiply maxIntensity by 2% then round up to the nearest integer
            boxPlotInfo.MaxIntensity = Math.Max(1, Math.Ceiling(maxIntensity * 1.02));

            return xAxisLabels;
        }

        private double GetMedian(ICollection<double> values, double valueIfEmptyList = 0)
        {
            if (values.Count == 0)
                return valueIfEmptyList;

            return MathNet.Numerics.Statistics.Statistics.Median(values);
        }

        private PlotContainerBase InitializePlot(
            BoxPlotInfo boxPlotInfo,
            string plotTitle,
            AxisInfo yAxisInfo)
        {
            if (Options.PlotWithPython)
            {
                return InitializePythonPlot(boxPlotInfo, plotTitle, yAxisInfo);
            }

            return InitializeOxyPlot(boxPlotInfo, plotTitle, yAxisInfo);
        }

        /// <summary>
        /// Initialize an OxyPlot plot container for a histogram
        /// </summary>
        /// <param name="boxPlotInfo">Data to display</param>
        /// <param name="plotTitle">Title of the plot</param>
        /// <param name="xAxisLabel"></param>
        /// <param name="yAxisInfo"></param>
        /// <returns>OxyPlot PlotContainer</returns>
        private PlotContainer InitializeOxyPlot(
            BoxPlotInfo boxPlotInfo,
            Dictionary<int, string> xAxisLabels,
            IReadOnlyList<List<double>> pointsByBox)
        {

            var xAxisLabels = GetDataToPlot(boxPlotInfo, out var pointsByBox, out var maxIntensity);

            if (pointsByBox.Count == 0)
            {
                // Nothing to plot
                var emptyContainer = new PlotContainer(PlotContainerBase.PlotTypes.BoxPlot, new PlotModel(), mWriteDebug);
                emptyContainer.WriteDebugLog("pointsByBox.Count == 0 in InitializeOxyPlot for plot " + boxPlotInfo.PlotTitle);
                return emptyContainer;
            }

            var myPlot = OxyPlotUtilities.GetBasicBoxPlotModel(boxPlotInfo.PlotTitle, xAxisLabels.Values, boxPlotInfo.YAxisInfo);

            var absoluteValueMin = double.MaxValue;
            var absoluteValueMax = double.MinValue;

            var xAxisLabelIndices = new List<int>();

            foreach (var labelIndex in (from item in xAxisLabels.Keys orderby item select item))
            {
                xAxisLabelIndices.Add(labelIndex);
            }

            for (var i = 0; i < pointsByBox.Count; i++)
            {
                var labelIndex = xAxisLabelIndices[i];
                var values = pointsByBox[i];

                foreach (var currentValAbs in from value in values select Math.Abs(value))
                {
                    absoluteValueMin = Math.Min(absoluteValueMin, currentValAbs);
                    absoluteValueMax = Math.Max(absoluteValueMax, currentValAbs);
                }

                AddOxyPlotSeries(myPlot, values, out var boxPlotStats);

                mBoxPlot.BoxPlotStatistics.Add(labelIndex, boxPlotStats);
            }

            if (!(absoluteValueMin < double.MaxValue))
                absoluteValueMin = 0;

            if (!(absoluteValueMax > double.MinValue))
                absoluteValueMax = 0;

            OxyPlotUtilities.UpdateAxisFormatCodeIfSmallValues(myPlot.Axes[1], absoluteValueMin, absoluteValueMax, false);

            var plotContainer = new PlotContainer(PlotContainerBase.PlotTypes.XY, myPlot, mWriteDebug)
            {
                FontSizeBase = PlotContainer.DEFAULT_BASE_FONT_SIZE
            };

            plotContainer.WriteDebugLog(string.Format("Instantiated plotContainer for plot {0}: {1} boxes", boxPlotInfo.PlotTitle, pointsByBox.Count));

            if (boxPlotInfo.YAxisInfo.AutoScale)
            {
                // Auto scale
            }
            else
            {
                // Override the auto-computed Y axis range
                myPlot.Axes[1].Minimum = 0;
                myPlot.Axes[1].Maximum = boxPlotInfo.MaxIntensity;
            }

            // Hide the legend
            myPlot.IsLegendVisible = false;

            return plotContainer;
        }

        /// <summary>
        /// Initialize a Python plot container for a histogram
        /// </summary>
        /// <param name="boxPlotInfo">Data to display</param>
        /// <param name="plotTitle">Title of the plot</param>
        /// <param name="yAxisInfo"></param>
        /// <returns>OxyPlot PlotContainer</returns>
        private PythonPlotContainer InitializePythonPlot(
            BoxPlotInfo boxPlotInfo,
            Dictionary<int, string> xAxisLabels,
            List<List<double>> pointsByBox)
        {

            var xAxisLabels = GetDataToPlot(boxPlotInfo, out var pointsByBox, out var maxIntensity);

            if (pointsByBox.Count == 0)
            {
                // Nothing to plot
                var emptyContainer = new PythonPlotContainerBoxPlot();
                emptyContainer.WriteDebugLog("points.Count == 0 in InitializePythonPlot for plot " + boxPlotInfo.PlotTitle);
                return emptyContainer;
            }

            var plotContainer = new PythonPlotContainerBoxPlot(boxPlotInfo.PlotTitle, string.Empty, boxPlotInfo.YAxisInfo.Title)
            {
                DeleteTempFiles = Options.DeleteTempFiles
            };
            RegisterEvents(plotContainer);

            var labelNames = (from item in xAxisLabels orderby item.Key select item.Value).ToList();

            plotContainer.SetData(labelNames, pointsByBox);

            if (yAxisInfo.AutoScale)
            {
                // Auto scale
            }
            else
            {
                // Override the auto-computed Y axis range
                plotContainer.YAxisInfo.SetRange(0, boxPlotInfo.MaxIntensity);
            }

            return plotContainer;
        }

        /// <summary>
        /// Clear box plot data
        /// </summary>
        public void Reset()
        {
            mBoxPlot.Initialize();
        }

        /// <summary>
        /// Save the plot to disk
        /// </summary>
        /// <param name="datasetName"></param>
        /// <param name="outputDirectory"></param>
        /// <param name="outputFilePath"></param>
        /// <param name="yAxisMinimum"></param>
        /// <returns>True if success, otherwise false</returns>
        public bool SavePlotFile(
            string datasetName,
            string outputDirectory,
            out string outputFilePath,
            bool logarithmicYAxis,
            int yAxisMinimum = 0)
        {
            outputFilePath = string.Empty;

            try
            {
                mBoxPlot.YAxisInfo.Title = YAxisLabel;
                mBoxPlot.YAxisInfo.Minimum = yAxisMinimum;
                mBoxPlot.YAxisInfo.TickLabelsArePercents = false;
                mBoxPlot.YAxisInfo.UseLogarithmicScale = logarithmicYAxis;
                mBoxPlot.YAxisInfo.StringFormat = "0E0";

                var boxPlot = InitializePlot(mBoxPlot);
                RegisterEvents(boxPlot);

                if (boxPlot.SeriesCount == 0)
                {
                    // We'll treat this as success
                    return true;
                }

                var pngFile = new FileInfo(Path.Combine(outputDirectory, datasetName + "_" + PlotAbbrev + ".png"));

                if (string.IsNullOrWhiteSpace(outputFilePath))
                {
                    outputFilePath = pngFile.FullName;
                }

                var success = boxPlot.SaveToPNG(pngFile, 1024, 600, 96);

                return success;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in BoxPlotPlotter.SavePlotFile", ex);
                return false;
            }

        }

        private class BoxPlotInfo
        {
            private int DataCount => DataPoints.Count;

            /// <summary>
            /// Statistics for each box, by channel
            /// </summary>
            /// Keys are column index (corresponding to <see cref="ReporterIonNames"/>)
            /// Values are the stats for the given reporter ion
            public Dictionary<int, BoxPlotStats> BoxPlotStatistics { get; }

            /// <summary>
            /// Reporter ion intensities, by channel
            /// </summary>
            /// Keys are column index (corresponding to <see cref="ReporterIonNames"/>)
            /// Values are the list of non-zero reporter ion intensities for the given reporter ion
            public Dictionary<int, List<double>> DataPoints { get; }

            public double MaxIntensity { get; set; }

            public string PlotTitle { get; }

            /// <summary>
            /// Reporter ion column names
            /// </summary>
            /// <remarks>Keys are column index, values are reporter ion info</remarks>
            public Dictionary<int, string> ReporterIonNames { get; }

            public AxisInfo YAxisInfo { get; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="plotTitle"></param>
            public BoxPlotInfo(string plotTitle)
            {
                BoxPlotStatistics = new Dictionary<int, BoxPlotStats>();
                DataPoints = new Dictionary<int, List<double>>();
                MaxIntensity = 0;
                PlotTitle = plotTitle;
                ReporterIonNames = new Dictionary<int, string>();
                YAxisInfo = new AxisInfo();
            }

            public void AddPoints(int labelIndex, string label, List<double> values)
            {
                DataPoints.Add(labelIndex, values);
                ReporterIonNames.Add(labelIndex, label);
            }

            /// <summary>
            /// Clear cached data
            /// </summary>
            public void Initialize()
            {
                DataPoints.Clear();
                ReporterIonNames.Clear();
            }

            // ReSharper disable once UnusedMember.Global
            // ReSharper disable once UnusedMember.Local
            public void RemoveAt(int index)
            {
                RemoveRange(index, 1);
            }

            // ReSharper disable once MemberCanBePrivate.Local
            public void RemoveRange(int index, int count)
            {
                if (index < 0 || index >= DataCount || count <= 0)
                    return;

                var lastIndex = Math.Min(index + count, DataPoints.Count) - 1;

                for (var i = index; i <= lastIndex; i++)
                {
                    DataPoints.Remove(index);
                    ReporterIonNames.Remove(index);
                }
            }

            public override string ToString()
            {
                return string.Format("BoxPlotInfo: {0} boxes", DataCount);
            }
        }
    }
}
