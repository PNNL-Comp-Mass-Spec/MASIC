using System;
using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Axes;

namespace MASIC.Plots
{
    /// <summary>
    /// Helper class for plotting data using OxyPlot
    /// </summary>
    public static class OxyPlotUtilities
    {
        // Ignore Spelling: Arial

        private static string AddAxes(PlotModel myPlot, Axis xAxis, Axis yAxis, AxisInfo yAxisInfo)
        {
            myPlot.Axes.Add(xAxis);

            myPlot.Axes.Add(yAxis);

            string yAxisFormatString;
            if (yAxisInfo.TickLabelsArePercents)
            {
                // The left y-axis ignores attempts to display values as a percentage
                // Furthermore, the values shown on the color axis are shown as values * 100, which is not what we want
                // Thus, will use DEFAULT_AXIS_LABEL_FORMAT instead of yAxisFormatString = "0%";
                yAxisFormatString = AxisInfo.DEFAULT_AXIS_LABEL_FORMAT;
            }
            else if (yAxisInfo.TickLabelsUseExponentialNotation)
            {
                yAxisFormatString = AxisInfo.EXPONENTIAL_FORMAT;
            }
            else
            {
                yAxisFormatString = string.IsNullOrWhiteSpace(yAxisInfo.StringFormat) ? AxisInfo.DEFAULT_AXIS_LABEL_FORMAT : yAxisInfo.StringFormat;
            }

            myPlot.Axes[1].StringFormat = yAxisFormatString;

            // Adjust the font sizes
            myPlot.Axes[0].FontSize = PlotContainer.DEFAULT_BASE_FONT_SIZE;
            myPlot.Axes[1].FontSize = PlotContainer.DEFAULT_BASE_FONT_SIZE;

            return yAxisFormatString;
        }

#pragma warning disable CS3002 // Return type is not CLS-compliant
        /// <summary>
        /// Obtain a bar chart PlotModel
        /// </summary>
        /// <param name="title"></param>
        /// <param name="xAxisLabels"></param>
        /// <param name="yAxisInfo"></param>
        public static PlotModel GetBasicBarChartModel(
            string title,
            IEnumerable<string> xAxisLabels,
            AxisInfo yAxisInfo)
#pragma warning restore CS3002 // Argument type is not CLS-compliant
        {
            var myPlot = GetPlotModel(title);

            var xAxisInfo = new AxisInfo(string.Empty)
            {
                AxisKey = "Category"
            };

            var xAxis = MakeCategoryAxis(AxisPosition.Bottom, xAxisInfo, PlotContainer.DEFAULT_BASE_FONT_SIZE);

            xAxis.Labels.AddRange(xAxisLabels);

            yAxisInfo.AxisKey = "Value";

            var yAxis = MakeYAxis(yAxisInfo, PlotContainer.DEFAULT_BASE_FONT_SIZE);

            var yAxisFormatString = AddAxes(myPlot, xAxis, yAxis, yAxisInfo);

            if (!yAxisInfo.AddColorAxis)
                return myPlot;

            // This will show a color gradient on the right side of the chart
            // This has no effect on bar colors; those must be manually set
            var colorAxis = new LinearColorAxis
            {
                Position = AxisPosition.Right,
                Title = string.Empty,
                TitleFontSize = PlotContainer.DEFAULT_BASE_FONT_SIZE + 2,
                TitleFontWeight = FontWeights.Normal,
                TitleFont = "Arial",
                AxisTitleDistance = 15,
                TickStyle = TickStyle.Crossing,
                AxislineColor = OxyColors.Black,
                AxislineStyle = LineStyle.Solid,
                MajorTickSize = 8,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                StringFormat = yAxisFormatString,
                Font = "Arial",
                FontSize = PlotContainer.DEFAULT_BASE_FONT_SIZE,

                Minimum = yAxisInfo.ColorScaleMinIntensity,
                Maximum = yAxisInfo.ColorScaleMaxIntensity,
                Palette = yAxisInfo.ColorPalette,
                IsAxisVisible = true
            };

            myPlot.Axes.Add(colorAxis);
            return myPlot;
        }

#pragma warning disable CS3002 // Return type is not CLS-compliant
        /// <summary>
        /// Obtain a box plot PlotModel
        /// </summary>
        /// <param name="title"></param>
        /// <param name="xAxisLabels"></param>
        /// <param name="yAxisInfo"></param>
        public static PlotModel GetBasicBoxPlotModel(
            string title,
            IEnumerable<string> xAxisLabels,
            AxisInfo yAxisInfo)
#pragma warning restore CS3002 // Argument type is not CLS-compliant
        {
            var myPlot = GetPlotModel(title);

            var xAxis = MakeCategoryAxis(AxisPosition.Bottom, yAxisInfo, PlotContainer.DEFAULT_BASE_FONT_SIZE);

            xAxis.Labels.AddRange(xAxisLabels);

            var yAxis = MakeYAxis(yAxisInfo, PlotContainer.DEFAULT_BASE_FONT_SIZE);

            AddAxes(myPlot, xAxis, yAxis, yAxisInfo);

            return myPlot;
        }

#pragma warning disable CS3002 // Return type is not CLS-compliant
        /// <summary>
        /// Obtain a basic PlotModel instance
        /// </summary>
        /// <param name="title"></param>
        /// <param name="xAxisLabel"></param>
        /// <param name="yAxisInfo"></param>
        public static PlotModel GetBasicPlotModel(
            string title,
            string xAxisLabel,
            AxisInfo yAxisInfo)
#pragma warning restore CS3002 // Argument type is not CLS-compliant
        {
            var myPlot = GetPlotModel(title);

            var xAxisInfo = new AxisInfo(xAxisLabel);
            var xAxis = MakeLinearAxis(AxisPosition.Bottom, xAxisInfo, PlotContainer.DEFAULT_BASE_FONT_SIZE);

            var yAxis = MakeYAxis(yAxisInfo, PlotContainer.DEFAULT_BASE_FONT_SIZE);

            AddAxes(myPlot, xAxis, yAxis, yAxisInfo);
            myPlot.Axes[0].Minimum = 0;

            return myPlot;
        }

        private static PlotModel GetPlotModel(string title)
        {
            var myPlot = new PlotModel
            {
                Title = title,
                TitleFont = "Arial",
                TitleFontSize = PlotContainer.DEFAULT_BASE_FONT_SIZE + 4,
                TitleFontWeight = FontWeights.Normal
            };

            myPlot.Padding = new OxyThickness(myPlot.Padding.Left, myPlot.Padding.Top, 30, myPlot.Padding.Bottom);

            // Set the background color
            myPlot.PlotAreaBackground = OxyColor.FromRgb(243, 243, 243);

            return myPlot;
        }

        private static void InitializeAxis(Axis axis, AxisPosition position, AxisInfo axisInfo, int baseFontSize, bool isCategoryAxis = false)
        {
            axis.Position = position;

            if (!isCategoryAxis)
            {
                axis.Title = axisInfo.Title ?? string.Empty;
            }

            axis.Key = axisInfo.AxisKey;
            axis.TitleFontSize = baseFontSize + 2;
            axis.TitleFontWeight = FontWeights.Normal;
            axis.TitleFont = "Arial";
            axis.AxisTitleDistance = 15;
            axis.TickStyle = TickStyle.Crossing;
            axis.AxislineColor = OxyColors.Black;
            axis.AxislineStyle = LineStyle.Solid;
            axis.MajorTickSize = 8;
            axis.MajorGridlineStyle = LineStyle.None;
            axis.MinorGridlineStyle = LineStyle.None;

            if (!isCategoryAxis)
            {
                var stringFormat = string.IsNullOrWhiteSpace(axisInfo.StringFormat) ? AxisInfo.DEFAULT_AXIS_LABEL_FORMAT : axisInfo.StringFormat;

                // Option 1:
                // axis.LabelFormatter = delegate (double value) { return value.ToString(stringFormat); };

                // Option 2: use a lambda expression
                axis.LabelFormatter = value => value.ToString(stringFormat);
            }

            axis.Font = "Arial";
        }

        private static CategoryAxis MakeCategoryAxis(AxisPosition position, AxisInfo axisInfo, int baseFontSize)
        {
            var categoryAxis = new CategoryAxis
            {
                Angle = 30
            };

            InitializeAxis(categoryAxis, position, axisInfo, baseFontSize, true);

            return categoryAxis;
        }

        private static LinearAxis MakeLinearAxis(AxisPosition position, AxisInfo axisInfo, int baseFontSize)
        {
            var linearAxis = new LinearAxis();
            InitializeAxis(linearAxis, position, axisInfo, baseFontSize);

            return linearAxis;
        }

        private static LogarithmicAxis MakeLogarithmicAxis(AxisPosition position, AxisInfo axisInfo, int baseFontSize)
        {
            var logAxis = new LogarithmicAxis();
            InitializeAxis(logAxis, position, axisInfo, baseFontSize);

            return logAxis;
        }

        private static Axis MakeYAxis(AxisInfo yAxisInfo, int defaultBaseFontSize)
        {
            if (yAxisInfo.UseLogarithmicScale)
            {
                return MakeLogarithmicAxis(AxisPosition.Left, yAxisInfo, defaultBaseFontSize);
            }

            return MakeLinearAxis(AxisPosition.Left, yAxisInfo, defaultBaseFontSize);
        }

        /// <summary>
        /// Examine the values in dataPoints to see if they are all less than 10 (or all less than 1)
        /// If they are, change the axis format code from the default of "#,##0" (see DEFAULT_AXIS_LABEL_FORMAT)
        /// </summary>
        /// <param name="currentAxis"></param>
        /// <param name="dataPoints"></param>
        /// <param name="integerData"></param>
#pragma warning disable CS3001 // Argument type is not CLS-compliant
        public static void UpdateAxisFormatCodeIfSmallValues(Axis currentAxis, List<double> dataPoints, bool integerData)
#pragma warning restore CS3001
        {
            if (dataPoints.Count == 0 || double.IsNaN(currentAxis.MajorStep))
                return;

            var axisInfo = new AxisInfo(currentAxis.MajorStep, currentAxis.MinorGridlineThickness, currentAxis.Title);
            PlotUtilities.GetAxisFormatInfo(dataPoints, integerData, axisInfo);

            currentAxis.StringFormat = axisInfo.StringFormat;
            currentAxis.MajorStep = axisInfo.MajorStep;
            currentAxis.MinorGridlineThickness = axisInfo.MinorGridLineThickness;
        }

#pragma warning disable CS3001 // Argument type is not CLS-compliant
        /// <summary>
        /// Update the axis format string if small values are present
        /// </summary>
        /// <param name="currentAxis"></param>
        /// <param name="absoluteValueMin"></param>
        /// <param name="absoluteValueMax"></param>
        /// <param name="integerData"></param>
        public static void UpdateAxisFormatCodeIfSmallValues(Axis currentAxis, double absoluteValueMin, double absoluteValueMax, bool integerData)
#pragma warning restore CS3001
        {
            var axisInfo = new AxisInfo(currentAxis.MajorStep, currentAxis.MinorGridlineThickness, currentAxis.Title);

            PlotUtilities.GetAxisFormatInfo(absoluteValueMin, absoluteValueMax, integerData, axisInfo);

            currentAxis.StringFormat = axisInfo.StringFormat;
            currentAxis.MajorStep = axisInfo.MajorStep;
            currentAxis.MinorGridlineThickness = axisInfo.MinorGridLineThickness;
        }

#pragma warning disable CS3001 // Argument type is not CLS-compliant
        /// <summary>
        /// Validate the major step size for tick marks
        /// </summary>
        /// <param name="currentAxis"></param>
        public static void ValidateMajorStep(Axis currentAxis)
#pragma warning restore CS3001
        {
            if (Math.Abs(currentAxis.ActualMajorStep) > float.Epsilon && currentAxis.ActualMajorStep < 1)
            {
                currentAxis.MajorStep = 1;
                currentAxis.MinorGridlineThickness = 0;
            }
        }
    }
}
