using System.Collections.Generic;
using System.Linq;

namespace MASICBrowser
{
    public class clsSICStats
    {
        public MASICPeakFinder.clsSICStatsPeak Peak { get; set; }

        public int SICPeakWidthFullScans { get; set; }

        /// <summary>
        /// Scan number of the peak apex
        /// </summary>
        public int ScanNumberMaxIntensity { get; set; }

        public MASICPeakFinder.clsSICPotentialAreaStats SICPotentialAreaStatsForPeak { get; set; }

        public List<double> SICSmoothedYData { get; set; }

        public int SICSmoothedYDataIndexStart { get; set; }

        public clsSICStats()
        {
            Peak = new MASICPeakFinder.clsSICStatsPeak();
            SICPotentialAreaStatsForPeak = new MASICPeakFinder.clsSICPotentialAreaStats();
            SICSmoothedYData = new List<double>();
        }

        public clsSICStats Clone()
        {
            var stats = new clsSICStats
            {
                Peak = Peak.Clone(),
                SICPeakWidthFullScans = SICPeakWidthFullScans,
                ScanNumberMaxIntensity = ScanNumberMaxIntensity,
                SICPotentialAreaStatsForPeak = new MASICPeakFinder.clsSICPotentialAreaStats
                {
                    MinimumPotentialPeakArea = SICPotentialAreaStatsForPeak.MinimumPotentialPeakArea,
                    PeakCountBasisForMinimumPotentialArea = SICPotentialAreaStatsForPeak.PeakCountBasisForMinimumPotentialArea,
                },
                SICSmoothedYData = SICSmoothedYData.ToList(),
                SICSmoothedYDataIndexStart = SICSmoothedYDataIndexStart,
            };

            return stats;
        }

        public override string ToString()
        {
            return "Peak at index " + Peak.IndexMax + ", area " + Peak.Area;
        }
    }
}
