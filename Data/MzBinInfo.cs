namespace MASIC.Data
{
    /// <summary>
    /// Container for a binned m/z value
    /// </summary>
    public class MzBinInfo
    {
        // Ignore Spelling: Da, MASIC

        /// <summary>
        /// m/z value
        /// </summary>
        public double MZ { get; set; }

        /// <summary>
        /// Search tolerance to use when creating a selected ion chromatogram
        /// </summary>
        public double MZTolerance { get; set; }

        /// <summary>
        /// When true, MZTolerance is ppm-based
        /// </summary>
        public bool MZToleranceIsPPM { get; set; }

        /// <summary>
        /// Parent ion index for this binned m/z value
        /// </summary>
        public int ParentIonIndex { get; set; }

        /// <summary>
        /// Show the m/z value and search tolerance
        /// </summary>
        public override string ToString()
        {
            if (MZToleranceIsPPM)
            {
                return "m/z: " + MZ.ToString("0.0") + ", MZTolerance: " + MZTolerance.ToString("0.0") + " ppm";
            }

            return "m/z: " + MZ.ToString("0.0") + ", MZTolerance: " + MZTolerance.ToString("0.000") + " Da";
        }
    }
}
