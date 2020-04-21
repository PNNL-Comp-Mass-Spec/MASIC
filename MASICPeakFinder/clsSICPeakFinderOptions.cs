namespace MASICPeakFinder
{
    public class clsSICPeakFinderOptions
    {
        #region "Properties"

        /// <summary>
        /// Intensity Threshold Fraction Max
        /// </summary>
        /// <returns></returns>
        /// <remarks>Value between 0 and 1; default: 0.01</remarks>
        public double IntensityThresholdFractionMax
        {
            get => mIntensityThresholdFractionMax;
            set
            {
                if (value < 0 || value > 1)
                    value = 0.01;
                mIntensityThresholdFractionMax = value;
            }
        }

        /// <summary>
        /// Intensity Threshold Absolute Minimum
        /// </summary>
        /// <returns></returns>
        /// <remarks>Default: 0</remarks>
        public double IntensityThresholdAbsoluteMinimum { get; set; }

        public clsBaselineNoiseOptions SICBaselineNoiseOptions { get; set; }

        /// <summary>
        /// Maximum distance that the edge of an identified peak can be away from the scan number that the parent ion was observed in if the identified peak does not contain the parent ion
        /// </summary>
        /// <returns></returns>
        /// <remarks>Default: 0</remarks>
        public int MaxDistanceScansNoOverlap
        {
            get => mMaxDistanceScansNoOverlap;
            set
            {
                if (value < 0 || value > 10000)
                    value = 0;
                mMaxDistanceScansNoOverlap = value;
            }
        }

        /// <summary>
        /// Maximum fraction of the peak maximum that an upward spike can be to be included in the peak
        /// </summary>
        /// <returns></returns>
        /// <remarks>Default: 0.20</remarks>
        public double MaxAllowedUpwardSpikeFractionMax
        {
            get => mMaxAllowedUpwardSpikeFractionMax;
            set
            {
                if (value < 0 || value > 1)
                    value = 0.2;
                mMaxAllowedUpwardSpikeFractionMax = value;
            }
        }

        /// <summary>
        /// Multiplied by scaled S/N for the given spectrum to determine the initial minimum peak width (in scans) to try.  Scaled "S/N" = Math.Log10(Math.Floor("S/N")) * 10
        /// </summary>
        /// <returns></returns>
        /// <remarks>Default: 0.5</remarks>
        public double InitialPeakWidthScansScaler
        {
            get => mInitialPeakWidthScansScaler;
            set
            {
                if (value < 0.001 || value > 1000)
                    value = 0.5;
                mInitialPeakWidthScansScaler = value;
            }
        }

        /// <summary>
        /// Maximum initial peak width to allow
        /// </summary>
        /// <returns></returns>
        /// <remarks>Default: 30</remarks>
        public int InitialPeakWidthScansMaximum
        {
            get => mInitialPeakWidthScansMaximum;
            set
            {
                if (value < 3 || value > 1000)
                    value = 6;
                mInitialPeakWidthScansMaximum = value;
            }
        }

        public bool FindPeaksOnSmoothedData { get; set; }

        public bool SmoothDataRegardlessOfMinimumPeakWidth { get; set; }

        /// <summary>
        /// Use Butterworth smoothing
        /// </summary>
        /// <returns></returns>
        /// <remarks>UseButterworthSmooth takes precedence over UseSavitzkyGolaySmooth</remarks>
        public bool UseButterworthSmooth { get; set; }

        public double ButterworthSamplingFrequency { get; set; }
        public bool ButterworthSamplingFrequencyDoubledForSIMData { get; set; }

        /// <summary>
        /// Use Savitzky Golay smoothing
        /// </summary>
        /// <returns></returns>
        public bool UseSavitzkyGolaySmooth { get; set; }

        /// <summary>
        /// Even number, 0 or greater; 0 means a moving average filter, 2 means a 2nd order Savitzky Golay filter
        /// </summary>
        /// <returns></returns>
        /// <remarks>Default: 0</remarks>
        public short SavitzkyGolayFilterOrder
        {
            get => mSavitzkyGolayFilterOrder;
            set
            {
                // Polynomial order should be between 0 and 6
                if (value < 0 || value > 6)
                    value = 0;

                // Polynomial order should be even
                if (value % 2 != 0)
                    value -= 1;
                if (value < 0)
                    value = 0;

                mSavitzkyGolayFilterOrder = value;
            }
        }

        public clsBaselineNoiseOptions MassSpectraNoiseThresholdOptions { get; set; }

        #endregion

        #region "Classwide variables"
        private int mInitialPeakWidthScansMaximum = 30;
        private double mInitialPeakWidthScansScaler = 0.5;
        private double mIntensityThresholdFractionMax = 0.01;
        private double mMaxAllowedUpwardSpikeFractionMax = 0.2;
        private int mMaxDistanceScansNoOverlap;
        private short mSavitzkyGolayFilterOrder;
        #endregion
    }
}