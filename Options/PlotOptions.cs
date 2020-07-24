
namespace MASIC.Options
{
    public class PlotOptions
    {

        #region "Properties"

        /// <summary>
        /// When true, create plots using the SIC data
        /// If reporter ion abundances were extracted, also create reporter ion observation rate plots
        /// </summary>
        public bool CreatePlots { get; set; } = true;

        /// <summary>
        /// Number of bins (buckets) to use for the histogram of log-10 transformed peak areas
        /// </summary>
        public int PeakAreaHistogramBinCount { get; set; } = 40;

        /// <summary>
        /// Number of bins (buckets) to use for the histogram of peak widths
        /// </summary>
        public int PeakWidthHistogramBinCount { get; set; } = 40;

        /// <summary>
        /// When compiling reporter ion observation rates, only look at data
        /// from this percentage of the peaks, sorted from highest to lowest abundance
        /// </summary>
        public int ReporterIonObservationRateTopNPct { get; set; } = 80;

        /// <summary>
        /// Minimum Y-axis value for the Top N Pct based reporter ion observation rate plot
        /// </summary>
        /// <remarks>Set this to a larger value to scale the y-axis to a range of this to 100</remarks>
        public int ReporterIonTopNPctObsRateYAxisMinimum { get; set; } = 0;

        #endregion

    }

}
