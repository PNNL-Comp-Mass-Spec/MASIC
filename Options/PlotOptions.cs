
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
        /// When true, delete temp files created while creating plots with Python
        /// </summary>
        public bool DeleteTempFiles { get; set; } = true;

        /// <summary>
        /// Number of bins (buckets) to use for the histogram of log-10 transformed peak areas
        /// </summary>
        public int PeakAreaHistogramBinCount { get; set; } = 40;

        /// <summary>
        /// Number of bins (buckets) to use for the histogram of peak widths
        /// </summary>
        public int PeakWidthHistogramBinCount { get; set; } = 40;

        /// <summary>
        /// When true, create plots using Python instead of OxyPlot
        /// </summary>
        /// <remarks>
        /// Looks for `python.exe` in directories that start with "Python3" or "Python 3" on Windows, searching below:
        /// - C:\Program Files
        /// - C:\Program Files(x86)
        /// - C:\Users\Username\AppData\Local\Programs
        /// - C:\ProgramData\Anaconda3
        /// - C:\
        /// Assumes Python is at `/usr/bin/python3` on Linux
        /// </remarks>
        public bool PlotWithPython { get; set; } = false;

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

        /// <summary>
        /// When true, create text files of the peak area and peak width histograms
        /// </summary>
        public bool SaveHistogramData { get; set; } = false;

        /// <summary>
        /// When true, create a index.html file that shows the plots and includes a link to DMS
        /// </summary>
        public bool SaveHtmlFile { get; set; } = true;

        /// <summary>
        /// When true, create a text file with reporter ion intensity statistics
        /// </summary>
        public bool SaveReporterIonIntensityStatsData { get; set; } = true;

        /// <summary>
        /// When true, create a text file with reporter ion observation rate stats
        /// </summary>
        public bool SaveReporterIonObservationRateData { get; set; } = true;

        #endregion

    }
}
