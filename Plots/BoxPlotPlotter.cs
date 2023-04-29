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
    internal class BoxPlotPlotter : EventNotifier
    {
        private readonly BoxPlotInfo mBoxPlot;

        private readonly bool mWriteDebug;

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

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="plotTitle"></param>
        /// <param name="plotCategory"></param>
        /// <param name="writeDebug"></param>
        public BoxPlotPlotter(
            PlotOptions options,
            string plotTitle,
            PlotContainerBase.PlotCategories plotCategory,
            bool writeDebug = false)
        {
            Options = options;
            mBoxPlot = new BoxPlotInfo(plotTitle, plotCategory);
            mWriteDebug = writeDebug;
            Reset();
        }

        public void AddData(int labelIndex, string label, List<double> values)
        {
            mBoxPlot.AddPoints(labelIndex, label, values);
        }

        private void AddOxyPlotSeries(PlotModel myPlot, IEnumerable<double> points, out BoxPlotStats boxPlotStats)
        {
            var series = new BoxPlotSeries
            {
                Fill = OxyColor.FromRgb(0x1e, 0xb4, 0xda),
                StrokeThickness = 1.1,
                WhiskerWidth = 1
            };

            boxPlotStats = ComputeBoxStats(points);

            var xValue = myPlot.Series.Count;

            var boxPlotItem = new BoxPlotItem(
                xValue,
                boxPlotStats.LowerWhisker,
                boxPlotStats.FirstQuartile,
                boxPlotStats.Median,
                boxPlotStats.ThirdQuartile,
                boxPlotStats.UpperWhisker)
            {
                Outliers = boxPlotStats.Outliers
            };

            series.Items.Add(boxPlotItem);

            myPlot.Series.Add(series);
        }

        private BoxPlotStats ComputeBoxStats(IEnumerable<double> points)
        {
            var boxPlotStats = new BoxPlotStats();

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

            var outliers = values.Where(item => item > upperWhisker || item < lowerWhisker);
            boxPlotStats.StoreOutliers(outliers);

            boxPlotStats.UpperWhisker = upperWhisker;
            boxPlotStats.LowerWhisker = lowerWhisker;

            boxPlotStats.NonZeroCount = values.Count(item => item > 0);

            return boxPlotStats;
        }

        /// <summary>
        /// Get data to plot
        /// </summary>
        /// <param name="boxPlotInfo"></param>
        /// <param name="pointsByBox">List of lists; each item is the list of values for the given box</param>
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

        private List<int> GetXAxisLabelIndices(Dictionary<int, string> xAxisLabels)
        {
            return (from item in xAxisLabels.Keys orderby item select item).ToList();
        }

        private PlotContainerBase InitializePlot(BoxPlotInfo boxPlotInfo)
        {
            var xAxisLabels = GetDataToPlot(boxPlotInfo, out var pointsByBox);

            if (Options.PlotWithPython)
            {
                return InitializePythonPlot(boxPlotInfo, xAxisLabels, pointsByBox);
            }

            return InitializeOxyPlot(boxPlotInfo, xAxisLabels, pointsByBox);
        }

        /// <summary>
        /// Initialize an OxyPlot plot container for a histogram
        /// </summary>
        /// <param name="boxPlotInfo">Data to display</param>
        /// <param name="xAxisLabels"></param>
        /// <param name="pointsByBox"></param>
        /// <returns>OxyPlot PlotContainer</returns>
        private PlotContainer InitializeOxyPlot(
            BoxPlotInfo boxPlotInfo,
            Dictionary<int, string> xAxisLabels,
            IReadOnlyList<List<double>> pointsByBox)
        {
            if (pointsByBox.Count == 0)
            {
                // Nothing to plot
                var emptyContainer = new PlotContainer(boxPlotInfo.PlotCategory, new PlotModel(), mWriteDebug);
                emptyContainer.WriteDebugLog("pointsByBox.Count == 0 in InitializeOxyPlot for plot " + boxPlotInfo.PlotTitle);
                return emptyContainer;
            }

            var myPlot = OxyPlotUtilities.GetBasicBoxPlotModel(boxPlotInfo.PlotTitle, xAxisLabels.Values, boxPlotInfo.YAxisInfo);

            var absoluteValueMin = double.MaxValue;
            var absoluteValueMax = double.MinValue;

            var xAxisLabelIndices = GetXAxisLabelIndices(xAxisLabels);

            for (var i = 0; i < pointsByBox.Count; i++)
            {
                var labelIndex = xAxisLabelIndices[i];
                var points = pointsByBox[i];

                foreach (var currentValAbs in from value in points select Math.Abs(value))
                {
                    absoluteValueMin = Math.Min(absoluteValueMin, currentValAbs);
                    absoluteValueMax = Math.Max(absoluteValueMax, currentValAbs);
                }

                AddOxyPlotSeries(myPlot, points, out var boxPlotStats);

                mBoxPlot.BoxPlotStatistics.Add(labelIndex, boxPlotStats);
            }

            if (!(absoluteValueMin < double.MaxValue))
                absoluteValueMin = 0;

            if (!(absoluteValueMax > double.MinValue))
                absoluteValueMax = 0;

            OxyPlotUtilities.UpdateAxisFormatCodeIfSmallValues(myPlot.Axes[1], absoluteValueMin, absoluteValueMax, false);

            var plotContainer = new PlotContainer(boxPlotInfo.PlotCategory, myPlot, mWriteDebug)
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
        /// <param name="xAxisLabels">Title of the plot</param>
        /// <param name="pointsByBox"></param>
        /// <returns>OxyPlot PlotContainer</returns>
        private PythonPlotContainer InitializePythonPlot(
            BoxPlotInfo boxPlotInfo,
            Dictionary<int, string> xAxisLabels,
            List<List<double>> pointsByBox)
        {
            if (pointsByBox.Count == 0)
            {
                // Nothing to plot
                var emptyContainer = new PythonPlotContainerBoxPlot(boxPlotInfo.PlotCategory);
                emptyContainer.WriteDebugLog("points.Count == 0 in InitializePythonPlot for plot " + boxPlotInfo.PlotTitle);
                return emptyContainer;
            }

            var plotContainer = new PythonPlotContainerBoxPlot(boxPlotInfo.PlotCategory, boxPlotInfo.PlotTitle, string.Empty, boxPlotInfo.YAxisInfo.Title)
            {
                DeleteTempFiles = Options.DeleteTempFiles
            };

            RegisterEvents(plotContainer);

            var labelNames = (from item in xAxisLabels orderby item.Key select item.Value).ToList();

            plotContainer.SetData(labelNames, pointsByBox);

            var xAxisLabelIndices = GetXAxisLabelIndices(xAxisLabels);

            for (var i = 0; i < pointsByBox.Count; i++)
            {
                var labelIndex = xAxisLabelIndices[i];

                var boxPlotStats = ComputeBoxStats(pointsByBox[i]);

                mBoxPlot.BoxPlotStatistics.Add(labelIndex, boxPlotStats);
            }

            if (boxPlotInfo.YAxisInfo.AutoScale)
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
        /// <param name="outputFilePath">Output: the full path to the .png file created by this method</param>
        /// <param name="logarithmicYAxis">When true, use logarithmic scaling for the y axis</param>
        /// <param name="skipCreatingPngFile">When true, generate the plot in memory but do not actually save to disk</param>
        /// <param name="yAxisMinimum"></param>
        /// <returns>True if success, otherwise false</returns>
        public bool SavePlotFile(
            string datasetName,
            string outputDirectory,
            out string outputFilePath,
            bool logarithmicYAxis,
            bool skipCreatingPngFile = false,
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

                // This call will lead to a call to ComputeBoxStats
                var boxPlot = InitializePlot(mBoxPlot);
                RegisterEvents(boxPlot);

                if (boxPlot.SeriesCount == 0 || skipCreatingPngFile)
                {
                    // We'll treat this as success
                    return true;
                }

                var pngFile = new FileInfo(Path.Combine(outputDirectory, datasetName + "_" + PlotAbbrev + ".png"));

                outputFilePath = pngFile.FullName;

                return boxPlot.SaveToPNG(pngFile, 1024, 600, 96);
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

            /// <summary>
            /// Maximum intensity
            /// </summary>
            public double MaxIntensity { get; set; }

            /// <summary>
            /// Plot title
            /// </summary>
            public string PlotTitle { get; }

            /// <summary>
            /// Plot category
            /// </summary>
            public PlotContainerBase.PlotCategories PlotCategory { get; }

            /// <summary>
            /// Reporter ion column names
            /// </summary>
            /// <remarks>Keys are column index, values are reporter ion info</remarks>
            public Dictionary<int, string> ReporterIonNames { get; }

            /// <summary>
            /// Y axis info
            /// </summary>
            public AxisInfo YAxisInfo { get; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="plotTitle"></param>
            /// <param name="plotCategory"></param>
            public BoxPlotInfo(string plotTitle, PlotContainerBase.PlotCategories plotCategory)
            {
                BoxPlotStatistics = new Dictionary<int, BoxPlotStats>();
                DataPoints = new Dictionary<int, List<double>>();
                MaxIntensity = 0;
                PlotTitle = plotTitle;
                PlotCategory = plotCategory;
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
