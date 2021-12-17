using System;

namespace MASICPeakFinder
{
    /// <summary>
    /// Baseline noise stats segment
    /// </summary>
    public class BaselineNoiseStatsSegment
    {
        /// <summary>
        /// Baseline noise stats
        /// </summary>
        public BaselineNoiseStats BaselineNoiseStats { get; set; }

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
        public BaselineNoiseStatsSegment()
        {
            BaselineNoiseStats = new BaselineNoiseStats();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="noiseStats"></param>
        public BaselineNoiseStatsSegment(BaselineNoiseStats noiseStats)
        {
            BaselineNoiseStats = noiseStats;
        }
    }
}