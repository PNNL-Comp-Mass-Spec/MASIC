using System.Collections.Generic;
using MASICPeakFinder;

namespace MASICBrowser
{
    /// <summary>
    /// Container for parent ion information
    /// </summary>
    public class clsParentIonStats
    {
        /// <summary>
        /// Scan types
        /// </summary>
        public enum eScanTypeConstants
        {
            /// <summary>
            /// Survey scan
            /// </summary>
            SurveyScan = 0,

            /// <summary>
            /// Fragmentation scan
            /// </summary>
            FragScan = 1
        }

        /// <summary>
        /// Parent ion index
        /// </summary>
        public int Index;

        /// <summary>
        /// Parent ion m/z
        /// </summary>
        public double MZ;

        /// <summary>
        /// Scan number of the survey scan
        /// </summary>
        public int SurveyScanNumber;

        /// <summary>
        /// Scan number of the fragmentation scan
        /// </summary>
        public int FragScanObserved;

        /// <summary>
        /// Fragmentation scan time, in minutes
        /// </summary>
        public float FragScanTime;

        /// <summary>
        /// Optimal peak apex scan number
        /// </summary>
        /// <remarks>
        /// This will be different than the survey scan number if the parent ion was combined with another parent ion due to similar m/z
        /// </remarks>
        public int OptimalPeakApexScanNumber { get; set; }

        /// <summary>
        /// Optimal peak apex scan time
        /// </summary>
        public float OptimalPeakApexTime { get; set; }

        /// <summary>
        /// True if this parent ion comes from a custom SIC
        /// </summary>
        public bool CustomSICPeak { get; set; }

        /// <summary>
        /// Custom SIC comment
        /// </summary>
        public string CustomSICPeakComment { get; set; }

        /// <summary>
        /// Parent ion scan type
        /// </summary>
        public eScanTypeConstants SICScanType { get; set; }

        /// <summary>
        /// Selected ion chromatogram data points
        /// </summary>
        public List<SICDataPoint> SICData { get; set; }

        /// <summary>
        /// Maximum intensity in SICData
        /// </summary>
        public double SICIntensityMax { get; set; }

        /// <summary>
        /// Contains the smoothed SIC data plus the details on the peak identified in the SIC (including its baseline noise stats)
        /// </summary>
        public clsSICStats SICStats { get; set; }

        /// <summary>
        /// List of scan numbers at which this m/z was chosen for fragmentation
        /// </summary>
        /// <remarks>
        /// The range of scans checked will be from SICScans[0] to SICScans[DataCount]
        /// </remarks>
        public List<SICDataPoint> SimilarFragScans { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsParentIonStats()
        {
            SICData = new List<SICDataPoint>();
            SICStats = new clsSICStats();
            SimilarFragScans = new List<SICDataPoint>();
        }
    }
}
