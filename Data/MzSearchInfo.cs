using System.Collections.Generic;

namespace MASIC.Data
{
    /// <summary>
    /// Selected ion chromatogram search info
    /// </summary>
    public class MzSearchInfo
    {
        // Ignore Spelling: Da, MASIC

        /// <summary>
        /// m/z value to find
        /// </summary>
        public double SearchMZ { get; set; }

        /// <summary>
        /// Starting index in the binned list of m/z values (created by CreateMZLookupList)
        /// </summary>
        public int MZIndexStart { get; set; }

        /// <summary>
        /// Ending index in the binned list of m/z values (created by CreateMZLookupList)
        /// </summary>
        public int MZIndexEnd { get; set; }

        /// <summary>
        /// Midpoint index in the binned list of m/z values (created by CreateMZLookupList)
        /// </summary>
        public int MZIndexMidpoint { get; set; }

        /// <summary>
        /// Search tolerance, in Da or ppm
        /// </summary>
        public double MZTolerance { get; set; }

        /// <summary>
        /// When true, the search tolerance is ppm-based
        /// </summary>
        public bool MZToleranceIsPPM { get; set; }

        /// <summary>
        /// Maximum intensity of data found by this class
        /// </summary>
        public double MaximumIntensity { get; set; }

        /// <summary>
        /// Index of the scan with the maximum intensity
        /// </summary>
        public int ScanIndexMax { get; set; }

        /// <summary>
        /// List of baseline noise stat segments
        /// </summary>
        public List<MASICPeakFinder.BaselineNoiseStatsSegment> BaselineNoiseStatSegments { get; set; }

        /// <summary>
        /// Show the search m/z and maximum intensity
        /// </summary>
        public override string ToString()
        {
            return "m/z: " + SearchMZ.ToString("0.0") + ", Intensity: " + MaximumIntensity.ToString("0.0");
        }

        /// <summary>
        /// Reset MaximumIntensity and ScanIndexMax to defaults
        /// </summary>
        public void ResetMaxIntensity()
        {
            MaximumIntensity = 0;
            ScanIndexMax = 0;
        }
    }
}
