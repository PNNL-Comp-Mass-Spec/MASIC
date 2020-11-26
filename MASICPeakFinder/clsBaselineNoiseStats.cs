namespace MASICPeakFinder
{
    /// <summary>
    /// Class for tracking baseline noise stats
    /// </summary>
    public class clsBaselineNoiseStats
    {
        /// <summary>
        /// Typically the average of the data being sampled to determine the baseline noise estimate
        /// </summary>
        public double NoiseLevel { get; set; }

        /// <summary>
        /// Standard Deviation of the data used to compute the baseline estimate
        /// </summary>
        public double NoiseStDev { get; set; }

        /// <summary>
        /// Number of points used
        /// </summary>
        public int PointsUsed { get; set; }

        public clsMASICPeakFinder.eNoiseThresholdModes NoiseThresholdModeUsed { get; set; }
        /// <summary>
        /// Noise threshold mode used
        /// </summary>

        /// <summary>
        /// Constructor
        /// </summary>
        public clsBaselineNoiseStats()
        {
            NoiseThresholdModeUsed = clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold;
        }

        /// <summary>
        /// Clone an instance of this class
        /// </summary>
        /// <returns></returns>
        public clsBaselineNoiseStats Clone()
        {
            var clonedStats = new clsBaselineNoiseStats()
            {
                NoiseLevel = NoiseLevel,
                NoiseStDev = NoiseStDev,
                PointsUsed = PointsUsed,
                NoiseThresholdModeUsed = NoiseThresholdModeUsed
            };
            return clonedStats;
        }
    }
}