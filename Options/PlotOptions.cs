
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
        /// When compiling reporter ion observation rates, only look at data
        /// from this percentage of the peaks, sorted from highest to lowest abundance
        /// </summary>
        public int ReporterIonObservationRateTopNPct { get; set; } = 80;

        /// <summary>
        /// Minimum Y-axis value for the Top N Pct based reporter ion observation rate plot
        /// </summary>
        public int ReporterIonTopNPctObsRateYAxisMinimum { get; set; } = 80;

        #endregion

    }

}
