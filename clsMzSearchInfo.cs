using System.Collections.Generic;

namespace MASIC
{
    public class clsMzSearchInfo
    {
        public double SearchMZ { get; set; }
        public int MZIndexStart { get; set; }
        public int MZIndexEnd { get; set; }
        public int MZIndexMidpoint { get; set; }
        public double MZTolerance { get; set; }
        public bool MZToleranceIsPPM { get; set; }
        public double MaximumIntensity { get; set; }
        public int ScanIndexMax { get; set; }
        public List<MASICPeakFinder.clsBaselineNoiseStatsSegment> BaselineNoiseStatSegments { get; set; }

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