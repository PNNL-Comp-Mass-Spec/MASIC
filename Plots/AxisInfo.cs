using System;
using System.Collections.Generic;
using OxyPlot;

namespace MASIC.Plots
{
    /// <summary>
    /// Plot axis info
    /// </summary>
    public class AxisInfo
    {
        // Ignore Spelling: Autoscale

        /// <summary>
        /// Default axis label format
        /// </summary>
        public const string DEFAULT_AXIS_LABEL_FORMAT = "#,##0";

        /// <summary>
        /// Exponential label format
        /// </summary>
        public const string EXPONENTIAL_FORMAT = "0.00E+00";

        /// <summary>
        /// When true, autoscale
        /// </summary>
        public bool AutoScale { get; set; }

        /// <summary>
        /// When true, add a color axis
        /// </summary>
        public bool AddColorAxis { get; set; }

#pragma warning disable CS3003 // Type is not CLS-compliant

        /// <summary>
        /// Color palette
        /// </summary>
        public OxyPalette ColorPalette { get; set; }

#pragma warning restore CS3003 // Type is not CLS-compliant

        /// <summary>
        /// Minimum intensity for the color scale
        /// </summary>
        public double ColorScaleMinIntensity { get; set; }

        /// <summary>
        /// Maximum intensity for the color scale
        /// </summary>
        public double ColorScaleMaxIntensity { get; set; }

        /// <summary>
        /// Minimum value for the axis
        /// </summary>
        public double Minimum { get; set; }

        /// <summary>
        /// Maximum value for the axis
        /// </summary>
        public double Maximum { get; set; }

        /// <summary>
        /// Major step value
        /// </summary>
        public double MajorStep { get; set; }

        /// <summary>
        /// Minor grid line thickness
        /// </summary>
        public double MinorGridLineThickness { get; set; }

        /// <summary>
        /// String format for tick mark labels
        /// </summary>
        public string StringFormat { get; set; }

        /// <summary>
        /// True if tick mark labels are percents
        /// </summary>
        public bool TickLabelsArePercents { get; set; }

        /// <summary>
        /// True if tick mark labels should use exponential notation
        /// </summary>
        public bool TickLabelsUseExponentialNotation { get; set; }

        /// <summary>
        /// Axis title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// When true, use a logarithmic scale
        /// </summary>
        public bool UseLogarithmicScale { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public AxisInfo(string title = "Undefined") : this(double.NaN, 1, title)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public AxisInfo(double majorStep, double minorGridLineThickness, string title = "Undefined")
        {
            AutoScale = true;
            Minimum = double.NaN;
            Maximum = double.NaN;
            MajorStep = majorStep;
            MinorGridLineThickness = minorGridLineThickness;
            StringFormat = DEFAULT_AXIS_LABEL_FORMAT;
            Title = title;

            AddColorAxis = false;
            ColorPalette = null;
            ColorScaleMinIntensity = 0;
            ColorScaleMaxIntensity = 100;
        }

        /// <summary>
        /// Get options as a semi colon separated list of key-value pairs
        /// </summary>
        public string GetOptions()
        {
            return GetOptions(new List<string>());
        }

        /// <summary>
        /// Get options as a semi colon separated list of key-value pairs
        /// This is used when plotting data with Python
        /// </summary>
        public string GetOptions(List<string> additionalOptions)
        {
            var options = new List<string>();

            if (AutoScale)
            {
                options.Add("Autoscale=true");
            }
            else
            {
                options.Add("Autoscale=false");
                options.Add("Minimum=" + Minimum);
                options.Add("Maximum=" + Maximum);
            }

            options.Add("StringFormat=" + StringFormat);

            if (!double.IsNaN(MinorGridLineThickness))
                options.Add("MinorGridLineThickness=" + MinorGridLineThickness);

            if (!double.IsNaN(MajorStep))
                options.Add("MajorStep=" + MajorStep);

            // ReSharper disable once MergeIntoPattern
            if (additionalOptions != null && additionalOptions.Count > 0)
                options.AddRange(additionalOptions);

            return string.Join(";", options);
        }

        /// <summary>
        /// Set the axis range
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <remarks>Set min and max to 0 (or double.NaN) to enable auto scaling</remarks>
        public void SetRange(double min, double max)
        {
            if (double.IsNaN(min) ||
                double.IsNaN(max) ||
                Math.Abs(min) < float.Epsilon && Math.Abs(max) < float.Epsilon)
            {
                AutoScale = true;
                return;
            }

            AutoScale = false;

            Minimum = min;
            Maximum = max;
        }
    }
}
