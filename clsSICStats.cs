namespace MASIC
{
    public class clsSICStats
    {
        public MASICPeakFinder.clsSICStatsPeak Peak { get; set; }
        public clsScanList.eScanTypeConstants ScanTypeForPeakIndices { get; set; }

        /// <summary>
        /// Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum
        /// </summary>
        /// <returns></returns>
        public int PeakScanIndexStart { get; set; }

        /// <summary>
        /// Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum
        /// </summary>
        /// <returns></returns>
        public int PeakScanIndexEnd { get; set; }

        /// <summary>
        /// Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum
        /// </summary>
        /// <returns></returns>
        public int PeakScanIndexMax { get; set; }
        public MASICPeakFinder.clsSICPotentialAreaStats SICPotentialAreaStatsForPeak { get; set; }

        public clsSICStats()
        {
            Peak = new MASICPeakFinder.clsSICStatsPeak();
        }

        public override string ToString()
        {
            return "Peak at index " + Peak.IndexMax + ", area " + Peak.Area;
        }
    }
}