using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OxyPlot;
using OxyPlot.Series;
using PRISM;
using ColumnSeries = OxyPlot.Series.ColumnSeries;

namespace MASIC.Plots
{
    public class BarChartPlotter : EventNotifier
    {

        #region "Member variables"

        private readonly BarChartInfo mBarChart;

        private readonly bool mWriteDebug;

        #endregion

        #region "Properties"

        public bool AutoMinMaxY { get; set; }

        /// <summary>
        /// This name is used when creating the .png file
        /// </summary>
        public string PlotAbbrev { get; set; } = "BarChart";

        public string PlotTitle { get; set; }

        public string YAxisLabel { get; set; } = "Intensity";

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="plotTitle"></param>
        /// <param name="writeDebug"></param>
        public BarChartPlotter(string plotTitle, bool writeDebug = false)
        {
            PlotTitle = plotTitle;
            mBarChart = new BarChartInfo();
            mWriteDebug = writeDebug;
            Reset();
        }

        public void AddData(string label, double value)
        {
            mBarChart.AddPoint(label, value);
        }

        private void AddOxyPlotSeries(PlotModel myPlot, IReadOnlyCollection<ColumnItem> points)
        {
            var series = new ColumnSeries();

            if (points.Count <= 0)
            {
                return;
            }

            series.StrokeColor = OxyColors.Black;
            series.FillColor = OxyColors.Black;
            series.StrokeThickness = 1;

            series.Items.AddRange(points);

            myPlot.Series.Add(series);
        }

        private PlotContainer InitializePlot(BarChartInfo histogramInfo, string plotTitle, AxisInfo yAxisInfo)
        {
            return InitializeOxyPlot(histogramInfo, plotTitle, yAxisInfo);
        }

        /// <summary>
        /// Plots bar chart
        /// </summary>
        /// <param name="histogramInfo">Data to display</param>
        /// <param name="plotTitle">Title of the plot</param>
        /// <param name="yAxisInfo"></param>
        /// <returns>OxyPlot PlotContainer</returns>
        private PlotContainer InitializeOxyPlot(BarChartInfo histogramInfo, string plotTitle, AxisInfo yAxisInfo)
        {

            double maxIntensity = 0;

            // Instantiate parallel lists to track the data points
            var xAxisLabels = new List<string>();
            var points = new List<ColumnItem>();

            var colorMap = new Dictionary<int, OxyColor>();

            if (yAxisInfo.ColorPalette != null)
            {
                for (var i = 0; i < yAxisInfo.ColorPalette.Colors.Count; i++)
                {
                    colorMap.Add(i, yAxisInfo.ColorPalette.Colors[i]);
                }
            }

            foreach (var dataPoint in histogramInfo.DataPoints)
            {
                if (dataPoint.Value > maxIntensity)
                {
                    maxIntensity = dataPoint.Value;
                }
            }

            var yAxisMinimum = PlotUtilities.GetNumberOrDefault(yAxisInfo.Minimum, 0);
            var yAxisMaximum = PlotUtilities.GetNumberOrDefault(yAxisInfo.Maximum, maxIntensity);

            if (Math.Abs(yAxisMinimum) < float.Epsilon && Math.Abs(yAxisMaximum) < float.Epsilon && maxIntensity > 0)
            {
                yAxisMaximum = maxIntensity;
            }

            foreach (var dataPoint in histogramInfo.DataPoints)
            {
                xAxisLabels.Add(dataPoint.Label);

                var columnItem = new ColumnItem(dataPoint.Value);

                if (colorMap.Count > 0 && yAxisMaximum > 0)
                {
                    var colorIndex = (int)Math.Round(dataPoint.Value * colorMap.Count / yAxisMaximum, 0) - 1;
                    if (colorIndex < 0)
                    {
                        colorIndex = 0;
                    }

                    columnItem.Color = colorMap[colorIndex];
                }

                points.Add(columnItem);

                if (dataPoint.Value > maxIntensity)
                {
                    maxIntensity = dataPoint.Value;
                }
            }

            if (points.Count == 0)
            {
                // Nothing to plot
                var emptyContainer = new PlotContainer(new PlotModel(), mWriteDebug);
                emptyContainer.WriteDebugLog("points.Count == 0 in InitializeOxyPlot for plot " + plotTitle);
                return emptyContainer;
            }

            var myPlot = OxyPlotUtilities.GetBasicBarChartModel(plotTitle, xAxisLabels, yAxisInfo);

            AddOxyPlotSeries(myPlot, points);

            var yVals = (from item in points select item.Value).ToList();
            OxyPlotUtilities.UpdateAxisFormatCodeIfSmallValues(myPlot.Axes[1], yVals, false);

            var plotContainer = new PlotContainer(myPlot, mWriteDebug)
            {
                FontSizeBase = PlotContainer.DEFAULT_BASE_FONT_SIZE
            };

            plotContainer.WriteDebugLog(string.Format("Instantiated plotContainer for plot {0}: {1} data points", plotTitle, points.Count));

            // Override the auto-computed Y axis range
            if (yAxisInfo.AutoScale)
            {
                // Auto scale
            }
            else
            {
                myPlot.Axes[1].Minimum = yAxisMinimum;
                myPlot.Axes[1].Maximum = yAxisMaximum;
            }

            // Hide the legend
            myPlot.IsLegendVisible = false;

            return plotContainer;
        }

        /// <summary>
        /// Clear bar chart data
        /// </summary>
        public void Reset()
        {
            mBarChart.Initialize();
        }

        /// <summary>
        /// Save the plot to disk
        /// </summary>
        /// <param name="datasetName"></param>
        /// <param name="outputDirectory"></param>
        /// <returns>True if success, false if an error</returns>
        public bool SavePlotFile(string datasetName, string outputDirectory, int yAxisMinimum = 0)
        {

            try
            {
                const int COLOR_COUNT = 30;

                var colorGradients = new Dictionary<string, OxyPalette>
                {
                    //{"BlackWhiteRed30", OxyPalettes.BlackWhiteRed(COLOR_COUNT).Reverse()},
                    //{"BlueWhiteRed30", OxyPalettes.BlueWhiteRed(COLOR_COUNT).Reverse()},
                    //{"Cool30", OxyPalettes.Cool(COLOR_COUNT).Reverse()},
                    //{"Gray30", OxyPalettes.Gray(COLOR_COUNT).Reverse()},
                    {"Hot30", OxyPalettes.Hot(COLOR_COUNT).Reverse()},
                    //{"Hue30", OxyPalettes.Hue(COLOR_COUNT).Reverse()},
                    //{"HueDistinct30", OxyPalettes.HueDistinct(COLOR_COUNT).Reverse()},
                    //{"Jet30", OxyPalettes.Jet(COLOR_COUNT).Reverse()},
                    //{"Rainbow30", OxyPalettes.Rainbow(COLOR_COUNT).Reverse()}
                };

                var successCount = 0;
                foreach (var colorGradient in colorGradients)
                {
                    var yAxisInfo = new AxisInfo(YAxisLabel)
                    {
                        Title = YAxisLabel,
                        AutoScale = AutoMinMaxY,
                        Minimum = yAxisMinimum,
                        Maximum = 100,
                        AddColorAxis = true,
                        ColorPalette = colorGradient.Value,
                        TickLabelsArePercents = true
                    };

                    var barChart = InitializePlot(mBarChart, PlotTitle, yAxisInfo);
                    RegisterEvents(barChart);

                    if (barChart.SeriesCount == 0)
                    {
                        // We'll treat this as success
                        return true;
                    }

                    var colorLabel = colorGradients.Count > 1 ? "_Gradient_" + colorGradient.Key : string.Empty;

                    var pngFile = new FileInfo(Path.Combine(outputDirectory, datasetName + "_" + PlotAbbrev + colorLabel + ".png"));

                    var success = barChart.SaveToPNG(pngFile, 1024, 600, 96);
                    if (success)
                        successCount++;
                }

                return successCount == colorGradients.Count;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in SavePlotFile", ex);
                return false;
            }

        }

        private class BarChartDataPoint
        {
            public string Label { get; set; }
            public double Value { get; set; }

            public override string ToString()
            {
                return string.Format("{0}: {1:F2}", Label, Value);
            }
        }

        private class BarChartInfo
        {

            // ReSharper disable once MemberCanBePrivate.Local
            public int DataCount => mDataPoints.Count;

            public IEnumerable<BarChartDataPoint> DataPoints => mDataPoints;

            private readonly List<BarChartDataPoint> mDataPoints;

            private readonly SortedSet<string> mLabels;

            /// <summary>
            /// Constructor
            /// </summary>
            public BarChartInfo()
            {
                mDataPoints = new List<BarChartDataPoint>();
                mLabels = new SortedSet<string>();
            }

            public void AddPoint(string label, double value)
            {
                if (mLabels.Contains(label))
                {
                    throw new Exception("Label " + label + " has already been added; programming error");
                }

                var dataPoint = new BarChartDataPoint
                {
                    Label = label,
                    Value = value
                };

                mDataPoints.Add(dataPoint);
                mLabels.Add(label);
            }

            public BarChartDataPoint GetDataPoint(int index)
            {
                if (mDataPoints.Count == 0)
                {
                    throw new Exception("Bar chart data list is empty; cannot retrieve data point at index " + index);
                }
                if (index < 0 || index >= mDataPoints.Count)
                {
                    throw new Exception("Bar chart data index out of range: " + index + "; should be between 0 and " + (mDataPoints.Count - 1));
                }

                return mDataPoints[index];
            }

            /// <summary>
            /// Clear cached data
            /// </summary>
            public void Initialize()
            {
                mDataPoints.Clear();
                mLabels.Clear();
            }

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

                var labelsToRemove = new List<string>();
                var lastIndex = Math.Min(index + count, mDataPoints.Count) - 1;

                for (var i = index; i <= lastIndex; i++)
                {
                    labelsToRemove.Add(mDataPoints[i].Label);
                }

                mDataPoints.RemoveRange(index, count);

                foreach (var label in labelsToRemove)
                {
                    mLabels.Remove(label);
                }
            }

            public override string ToString()
            {
                return string.Format("BarChartInfo: {0} values", DataCount);
            }
        }

    }
}

