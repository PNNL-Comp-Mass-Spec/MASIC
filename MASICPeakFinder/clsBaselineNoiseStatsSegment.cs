using System;

namespace MASICPeakFinder
{
    /// <summary>
    /// Baseline noise stats segment
    /// </summary>
    public class clsBaselineNoiseStatsSegment
    {
        /// <summary>
        /// Baseline noise stats
        /// </summary>
        public clsBaselineNoiseStats BaselineNoiseStats { get; set; }

        /// <summary>
        /// Segment start index
        /// </summary>
        public int SegmentIndexStart { get; set; }

        /// <summary>
        /// Segment end index
        /// </summary>
        public int SegmentIndexEnd { get; set; }

        /// <summary>
        /// Obsolete constructor
        /// </summary>
        [Obsolete("Use the constructor that takes an instance of clsBaselineNoiseStats")]
        public clsBaselineNoiseStatsSegment()
        {
            BaselineNoiseStats = new clsBaselineNoiseStats();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="noiseStats"></param>
        public clsBaselineNoiseStatsSegment(clsBaselineNoiseStats noiseStats)
        {
            BaselineNoiseStats = noiseStats;
        }
    }
}