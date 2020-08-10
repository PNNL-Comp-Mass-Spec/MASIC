using System.Collections.Generic;

namespace MASIC.DataOutput
{
    public class BoxPlotStats
    {
        public double FirstQuartile { get; set; }
        public double ThirdQuartile { get; set; }

        public double InterQuartileRange { get; set; }
        public double Median { get; set; }

        public double UpperWhisker  { get; set; }
        public double LowerWhisker { get; set; }

        public int NonZeroCount { get; set; }

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

        public override string ToString()
        {
            return string.Format("Median: {0:0}", Median);
        }
    }
}
