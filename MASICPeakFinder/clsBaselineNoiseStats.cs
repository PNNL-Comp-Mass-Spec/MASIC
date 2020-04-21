namespace MASICPeakFinder
{
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

        public int PointsUsed { get; set; }

        public clsMASICPeakFinder.eNoiseThresholdModes NoiseThresholdModeUsed { get; set; }

        public clsBaselineNoiseStats()
        {
            NoiseThresholdModeUsed = clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold;
        }

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