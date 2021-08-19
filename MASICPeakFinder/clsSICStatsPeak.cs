namespace MASICPeakFinder
{
    /// <summary>
    /// Container for a SIC peak
    /// </summary>
    public class clsSICStatsPeak
    {
        /// <summary>
        /// Index that the SIC peak officially starts; Pointer to entry in .SICData()
        /// </summary>
        public int IndexBaseLeft { get; set; }

        /// <summary>
        /// Index that the SIC peak officially ends; Pointer to entry in .SICData()
        /// </summary>
        public int IndexBaseRight { get; set; }

        /// <summary>
        /// Index of the maximum of the SIC peak; Pointer to entry in .SICData()
        /// </summary>
        public int IndexMax { get; set; }

        /// <summary>
        /// Index that the SIC peak was first observed in by the instrument (and thus caused it to be chosen for fragmentation)
        /// Pointer to entry in .SICData()
        /// </summary>
        public int IndexObserved { get; set; }

        /// <summary>
        /// Intensity of the parent ion in the scan just prior to the scan in which the peptide was fragmented
        /// If previous scan was not MS1, interpolates between MS1 scans bracketing the MS2 scan
        /// </summary>
        public double ParentIonIntensity { get; set; }

        /// <summary>
        /// Index of the FWHM point in the previous closest peak in the SIC
        /// Filtering to only include peaks with intensities >= BestPeak'sIntensity/3
        /// </summary>
        public int PreviousPeakFWHMPointRight { get; set; }

        /// <summary>
        /// Index of the FWHM point in the next closest peak in the SIC; filtering to only include peaks with intensities >= BestPeak'sIntensity/3
        /// </summary>
        public int NextPeakFWHMPointLeft { get; set; }

        /// <summary>
        /// Full width at half max, in scans
        /// </summary>
        public int FWHMScanWidth { get; set; }

        /// <summary>
        /// Maximum intensity of the SIC Peak -- not necessarily the maximum intensity in .SICData(); Not baseline corrected
        /// </summary>
        public double MaxIntensityValue { get; set; }

        /// <summary>
        /// Area of the SIC peak -- Equivalent to the zeroth statistical moment (m0); Not baseline corrected
        /// </summary>
        public double Area { get; set; }

        /// <summary>
        /// Number of small peaks that are contained by the peak
        /// </summary>
        public int ShoulderCount { get; set; }

        /// <summary>
        /// Signal to noise ratio of the SIC peak
        /// </summary>
        public double SignalToNoiseRatio { get; set; }

        /// <summary>
        /// Baseline noise stats
        /// </summary>
        public clsBaselineNoiseStats BaselineNoiseStats { get; set; }

        /// <summary>
        /// Statistical moments
        /// </summary>
        public clsStatisticalMoments StatisticalMoments { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsSICStatsPeak()
        {
            BaselineNoiseStats = new clsBaselineNoiseStats();
            StatisticalMoments = new clsStatisticalMoments();
        }

        /// <summary>
        /// Clone this SIC peak
        /// </summary>
        public clsSICStatsPeak Clone()
        {
            var clonedPeak = new clsSICStatsPeak()
            {
                IndexBaseLeft = IndexBaseLeft,
                IndexBaseRight = IndexBaseRight,
                IndexMax = IndexMax,
                IndexObserved = IndexObserved,
                ParentIonIntensity = ParentIonIntensity,
                PreviousPeakFWHMPointRight = PreviousPeakFWHMPointRight,
                NextPeakFWHMPointLeft = NextPeakFWHMPointLeft,
                FWHMScanWidth = FWHMScanWidth,
                MaxIntensityValue = MaxIntensityValue,
                Area = Area,
                ShoulderCount = ShoulderCount,
                SignalToNoiseRatio = SignalToNoiseRatio,
                BaselineNoiseStats = BaselineNoiseStats.Clone(),
                StatisticalMoments = StatisticalMoments.Clone()
            };
            return clonedPeak;
        }
    }
}