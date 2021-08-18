using System;
using System.Collections.Generic;
using OxyPlot;

namespace MASIC.Plots
{
    public class AxisInfo
    {
        // Ignore Spelling: Autoscale

        public const string DEFAULT_AXIS_LABEL_FORMAT = "#,##0";

        public const string EXPONENTIAL_FORMAT = "0.00E+00";

        public bool AutoScale { get; set; }

        public bool AddColorAxis { get; set; }

#pragma warning disable CS3003 // Type is not CLS-compliant
        public OxyPalette ColorPalette { get; set; }
#pragma warning restore CS3003 // Type is not CLS-compliant

        public double ColorScaleMinIntensity { get; set; }

        public double ColorScaleMaxIntensity { get; set; }

        public double Minimum { get; set; }

        public double Maximum { get; set; }

        public double MajorStep { get; set; }

        public double MinorGridLineThickness { get; set; }

        public string StringFormat { get; set; }

        public bool TickLabelsArePercents { get; set; }

        public bool TickLabelsUseExponentialNotation { get; set; }

        public string Title { get; set; }

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
        /// <returns></returns>
        public string GetOptions()
        {
            return GetOptions(new List<string>());
        }

        /// <summary>
        /// Get options as a semi colon separated list of key-value pairs
        /// This is used when plotting data with Python
        /// </summary>
        /// <returns></returns>
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
