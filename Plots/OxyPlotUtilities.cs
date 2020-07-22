using System;
using System.Collections.Generic;
using System.Linq;
using OxyPlot;
using OxyPlot.Axes;

namespace MASIC.Plots
{
    public class OxyPlotUtilities
    {
#pragma warning disable CS3002 // Return type is not CLS-compliant
        public static PlotModel GetBasicBarChartModel(
            string title,
            IEnumerable<string> xAxisLabels,
            AxisInfo yAxisInfo)
#pragma warning restore CS3002 // Argument type is not CLS-compliant
        {
            var myPlot = GetPlotModel(title);

            var xAxis = MakeCategoryAxis(AxisPosition.Bottom, PlotContainer.DEFAULT_BASE_FONT_SIZE);

            foreach (var label in xAxisLabels)
            {
                xAxis.Labels.Add(label);
            }

            myPlot.Axes.Add(xAxis);

            var yAxis = MakeLinearAxis(AxisPosition.Left, yAxisInfo.Title, PlotContainer.DEFAULT_BASE_FONT_SIZE);
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
                yAxisFormatString = AxisInfo.DEFAULT_AXIS_LABEL_FORMAT;
            }

            myPlot.Axes[1].StringFormat = yAxisFormatString;

            // Adjust the font sizes
            myPlot.Axes[0].FontSize = PlotContainer.DEFAULT_BASE_FONT_SIZE;
            myPlot.Axes[1].FontSize = PlotContainer.DEFAULT_BASE_FONT_SIZE;

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
        public static PlotModel GetBasicPlotModel(
            string title,
            string xAxisLabel,
            AxisInfo yAxisInfo)
#pragma warning restore CS3002 // Argument type is not CLS-compliant
        {
            var myPlot = GetPlotModel(title);

            myPlot.Axes.Add(MakeLinearAxis(AxisPosition.Bottom, xAxisLabel, PlotContainer.DEFAULT_BASE_FONT_SIZE));
            myPlot.Axes[0].Minimum = 0;

            myPlot.Axes.Add(MakeLinearAxis(AxisPosition.Left, yAxisInfo.Title, PlotContainer.DEFAULT_BASE_FONT_SIZE));

            if (yAxisInfo.TickLabelsUseExponentialNotation)
            {
                myPlot.Axes[1].StringFormat = AxisInfo.EXPONENTIAL_FORMAT;
            }

            // Adjust the font sizes
            myPlot.Axes[0].FontSize = PlotContainer.DEFAULT_BASE_FONT_SIZE;
            myPlot.Axes[1].FontSize = PlotContainer.DEFAULT_BASE_FONT_SIZE;

            return myPlot;
        }

        private static PlotModel GetPlotModel(string title)
        {
            var myPlot = new PlotModel
            {
                Title = string.Copy(title),
                TitleFont = "Arial",
                TitleFontSize = PlotContainer.DEFAULT_BASE_FONT_SIZE + 4,
                TitleFontWeight = FontWeights.Normal
            };

            myPlot.Padding = new OxyThickness(myPlot.Padding.Left, myPlot.Padding.Top, 30, myPlot.Padding.Bottom);

            // Set the background color
            myPlot.PlotAreaBackground = OxyColor.FromRgb(243, 243, 243);

            return myPlot;
        }

        private static CategoryAxis MakeCategoryAxis(AxisPosition position, int baseFontSize)
        {
            var axis = new CategoryAxis
            {
                Position = position,
                TitleFontSize = baseFontSize + 2,
                TitleFontWeight = FontWeights.Normal,
                TitleFont = "Arial",
                AxisTitleDistance = 15,
                TickStyle = TickStyle.Crossing,
                AxislineColor = OxyColors.Black,
                AxislineStyle = LineStyle.Solid,
                MajorTickSize = 8,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                Font = "Arial",
                Angle = 30
            };

            return axis;
        }

        private static LinearAxis MakeLinearAxis(AxisPosition position, string axisTitle, int baseFontSize)
        {
            var axis = new LinearAxis
            {
                Position = position,
                Title = axisTitle,
                TitleFontSize = baseFontSize + 2,
                TitleFontWeight = FontWeights.Normal,
                TitleFont = "Arial",
                AxisTitleDistance = 15,
                TickStyle = TickStyle.Crossing,
                AxislineColor = OxyColors.Black,
                AxislineStyle = LineStyle.Solid,
                MajorTickSize = 8,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                StringFormat = AxisInfo.DEFAULT_AXIS_LABEL_FORMAT,
                Font = "Arial"
            };

            return axis;
        }

        /// <summary>
        /// Examine the values in dataPoints to see if they are all less than 10 (or all less than 1)
        /// If they are, change the axis format code from the default of "#,##0" (see DEFAULT_AXIS_LABEL_FORMAT)
        /// </summary>
        /// <param name="currentAxis"></param>
        /// <param name="dataPoints"></param>
        /// <param name="integerData"></param>
        /// <remarks></remarks>
#pragma warning disable CS3001 // Argument type is not CLS-compliant
        public static void UpdateAxisFormatCodeIfSmallValues(Axis currentAxis, List<double> dataPoints, bool integerData)
#pragma warning restore CS3001
        {
            if (!dataPoints.Any())
                return;

            var axisInfo = new AxisInfo(currentAxis.MajorStep, currentAxis.MinorGridlineThickness, currentAxis.Title);
            PlotUtilities.GetAxisFormatInfo(dataPoints, integerData, axisInfo);

            currentAxis.StringFormat = axisInfo.StringFormat;
            currentAxis.MajorStep = axisInfo.MajorStep;
            currentAxis.MinorGridlineThickness = axisInfo.MinorGridLineThickness;
        }

#pragma warning disable CS3001 // Argument type is not CLS-compliant
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
