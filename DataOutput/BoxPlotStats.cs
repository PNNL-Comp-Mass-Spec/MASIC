using System.Collections.Generic;

namespace MASIC.DataOutput
{
    /// <summary>
    /// Values for a single box plot data point
    /// </summary>
    public class BoxPlotStats
    {
        // Ignore Spelling: MASIC, outlier

        /// <summary>
        /// First quartile
        /// </summary>
        public double FirstQuartile { get; set; }

        /// <summary>
        /// Third quartile
        /// </summary>
        public double ThirdQuartile { get; set; }

        /// <summary>
        /// Interquartile range
        /// </summary>
        public double InterQuartileRange { get; set; }

        /// <summary>
        /// Median value
        /// </summary>
        public double Median { get; set; }

        /// <summary>
        /// Upper y-value for a box plot whisker
        /// </summary>
        public double UpperWhisker  { get; set; }

        /// <summary>
        /// Lower y-value for a box plot whisker
        /// </summary>
        public double LowerWhisker { get; set; }

        /// <summary>
        /// Number of non-zero items
        /// </summary>
        public int NonZeroCount { get; set; }

        /// <summary>
        /// Outlier values
        /// </summary>
        public List<double> Outliers { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public BoxPlotStats()
        {
            Outliers = new List<double>();
        }

        /// <summary>
        /// Store outlier points
        /// </summary>
        /// <param name="outliers"></param>
        public void StoreOutliers(IEnumerable<double> outliers)
        {
            Outliers.Clear();
            Outliers.AddRange(outliers);
        }

        /// <summary>
        /// Show the median value
        /// </summary>
        public override string ToString()
        {
            return string.Format("Median: {0:0}", Median);
        }
    }
}
