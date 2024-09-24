namespace MASIC.Data
{
    /// <summary>
    /// SIC Stats container
    /// </summary>
    public class SICStats
    {
        // Ignore Spelling: frag, MASIC

        /// <summary>
        /// SIC peak
        /// </summary>
        public MASICPeakFinder.SICStatsPeak Peak { get; set; }

        /// <summary>
        /// Scan type for peak indices (survey scan or frag scan)
        /// </summary>
        public ScanList.ScanTypeConstants ScanTypeForPeakIndices { get; set; }

        /// <summary>
        /// Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum
        /// </summary>
        public int PeakScanIndexStart { get; set; }

        /// <summary>
        /// Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum
        /// </summary>
        public int PeakScanIndexEnd { get; set; }

        /// <summary>
        /// Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum
        /// </summary>
        public int PeakScanIndexMax { get; set; }

        /// <summary>
        /// Potential area stats for the SIC peak
        /// </summary>
        public MASICPeakFinder.SICPotentialAreaStats SICPotentialAreaStatsForPeak { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SICStats()
        {
            Peak = new MASICPeakFinder.SICStatsPeak();
        }

        /// <summary>
        /// Show the SIC peak index and area
        /// </summary>
        public override string ToString()
        {
            return "Peak at index " + Peak.IndexMax + ", area " + Peak.Area;
        }
    }
}
