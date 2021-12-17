namespace MASICPeakFinder
{
    /// <summary>
    /// Class for tracking baseline noise stats
    /// </summary>
    public class BaselineNoiseStats
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

        /// <summary>
        /// Noise threshold mode used
        /// </summary>
        public clsMASICPeakFinder.NoiseThresholdModes NoiseThresholdModeUsed { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public BaselineNoiseStats()
        {
            NoiseThresholdModeUsed = clsMASICPeakFinder.NoiseThresholdModes.AbsoluteThreshold;
        }

        /// <summary>
        /// Clone an instance of this class
        /// </summary>
        public BaselineNoiseStats Clone()
        {
            return new BaselineNoiseStats
            {
                NoiseLevel = NoiseLevel,
                NoiseStDev = NoiseStDev,
                PointsUsed = PointsUsed,
                NoiseThresholdModeUsed = NoiseThresholdModeUsed
            };
        }
    }
}