namespace MASICPeakFinder
{
    /// <summary>
    /// Statistical moments options
    /// </summary>
    public class clsStatisticalMoments
    {
        /// <summary>
        /// Area; Zeroth central moment (m0)
        /// Using baseline-corrected intensities unless all of the data is below the baseline;
        /// if that's the case, use the 3 points surrounding the peak apex
        /// </summary>
        public double Area { get; set; }

        /// <summary>
        /// Center of Mass of the peak; First central moment (m1); reported as an absolute scan number
        /// </summary>
        public int CenterOfMassScan { get; set; }

        /// <summary>
        /// Standard Deviation; Sqrt(Variance) where Variance is the second central moment (m2)
        /// </summary>
        public double StDev { get; set; }

        /// <summary>
        /// Computed using the third central moment via m3 / sigma^3 where m3 is the third central moment and sigma^3 = (Sqrt(m2))^3
        /// </summary>
        public double Skew { get; set; }

        /// <summary>
        /// The Kolmogorov-Smirnov Goodness-of-Fit value (not officially a statistical moment, but we'll put it here anyway)
        /// </summary>
        public double KSStat { get; set; }

        /// <summary>
        /// Data count used
        /// </summary>
        public int DataCountUsed { get; set; }

        /// <summary>
        /// Clone the settings tracked by this class
        /// </summary>
        public clsStatisticalMoments Clone()
        {
            var clonedStats = new clsStatisticalMoments()
            {
                Area = Area,
                CenterOfMassScan = CenterOfMassScan,
                StDev = StDev,
                Skew = Skew,
                KSStat = KSStat,
                DataCountUsed = DataCountUsed
            };

            return clonedStats;
        }
    }
}