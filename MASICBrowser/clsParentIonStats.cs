using System.Collections.Generic;
using MASICPeakFinder;

namespace MASICBrowser
{
    public class clsParentIonStats
    {
        public enum eScanTypeConstants
        {
            SurveyScan = 0,
            FragScan = 1
        }

        public int Index;

        public double MZ;

        /// <summary>
        /// Scan number of the survey scan
        /// </summary>
        public int SurveyScanNumber;

        /// <summary>
        /// Scan number of the fragmentation scan
        /// </summary>
        public int FragScanObserved;
        public float FragScanTime;

        /// <summary>
        /// Optimal peak apex scan number (if parent ion was combined with another parent ion due to similar m/z)
        /// </summary>
        public int OptimalPeakApexScanNumber { get; set; }

        public float OptimalPeakApexTime { get; set; }
        public bool CustomSICPeak { get; set; }
        public string CustomSICPeakComment { get; set; }

        public eScanTypeConstants SICScanType { get; set; }

        public List<clsSICDataPoint> SICData { get; set; }

        // Maximum intensity in SICData
        public double SICIntensityMax { get; set; }

        // Contains the smoothed SIC data plus the details on the peak identified in the SIC (including its baseline noise stats)
        public clsSICStats SICStats { get; set; }

        // List of scan numbers at which this m/z was chosen for fragmentation; the range of scans checked will be from SICScans(0) to SICScans(DataCount)
        public List<clsSICDataPoint> SimilarFragScans { get; set; }

        public clsParentIonStats()
        {
            SICData = new List<clsSICDataPoint>();
            SICStats = new clsSICStats();
            SimilarFragScans = new List<clsSICDataPoint>();
        }
    }
}
