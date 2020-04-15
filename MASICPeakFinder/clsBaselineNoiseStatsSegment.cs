using System;

namespace MASICPeakFinder
{
    public class clsBaselineNoiseStatsSegment
    {
        public clsBaselineNoiseStats BaselineNoiseStats { get; set; }

        public int SegmentIndexStart { get; set; }

        public int SegmentIndexEnd { get; set; }

        [Obsolete("Use the constructor that takes an instance of clsBaselineNoiseStats")]
        public clsBaselineNoiseStatsSegment()
        {
            BaselineNoiseStats = new clsBaselineNoiseStats();
        }

        public clsBaselineNoiseStatsSegment(clsBaselineNoiseStats noiseStats)
        {
            BaselineNoiseStats = noiseStats;
        }
    }
}