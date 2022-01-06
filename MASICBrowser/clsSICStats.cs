using System.Collections.Generic;
using System.Linq;

namespace MASICBrowser
{
    /// <summary>
    /// Container for the smoothed SIC data plus the details on the peak identified in the SIC (including its baseline noise stats)
    /// </summary>
    public class clsSICStats
    {
        /// <summary>
        /// Largest peak in the selected ion chromatogram
        /// </summary>
        public MASICPeakFinder.SICStatsPeak Peak { get; set; }

        /// <summary>
        /// Peak width, in scans
        /// </summary>
        public int SICPeakWidthFullScans { get; set; }

        /// <summary>
        /// Scan number of the peak apex
        /// </summary>
        public int ScanNumberMaxIntensity { get; set; }

        /// <summary>
        /// Area stats for the SIC peak
        /// </summary>
        public MASICPeakFinder.SICPotentialAreaStats  SICPotentialAreaStatsForPeak { get; set; }

        /// <summary>
        /// Smoothed intensity values
        /// </summary>
        public List<double> SICSmoothedYData { get; set; }

        /// <summary>
        /// Index in the original data array where the smoothed intensity data starts
        /// </summary>
        public int SICSmoothedYDataIndexStart { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsSICStats()
        {
            Peak = new MASICPeakFinder.SICStatsPeak();
            SICPotentialAreaStatsForPeak = new MASICPeakFinder.SICPotentialAreaStats ();
            SICSmoothedYData = new List<double>();
        }

        /// <summary>
        /// Duplicate the data stored in this class
        /// </summary>
        public clsSICStats Clone()
        {
            return new clsSICStats
            {
                Peak = Peak.Clone(),
                SICPeakWidthFullScans = SICPeakWidthFullScans,
                ScanNumberMaxIntensity = ScanNumberMaxIntensity,
                SICPotentialAreaStatsForPeak = new MASICPeakFinder.SICPotentialAreaStats
                {
                    MinimumPotentialPeakArea = SICPotentialAreaStatsForPeak.MinimumPotentialPeakArea,
                    PeakCountBasisForMinimumPotentialArea = SICPotentialAreaStatsForPeak.PeakCountBasisForMinimumPotentialArea,
                },
                SICSmoothedYData = SICSmoothedYData.ToList(),
                SICSmoothedYDataIndexStart = SICSmoothedYDataIndexStart,
            };
        }

        /// <summary>
        /// Show the peak index and area
        /// </summary>
        public override string ToString()
        {
            return "Peak at index " + Peak.IndexMax + ", area " + Peak.Area;
        }
    }
}
