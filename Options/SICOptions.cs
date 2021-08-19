namespace MASIC.Options
{
    /// <summary>
    /// Selected ion chromatogram processing options
    /// </summary>
    public class SICOptions
    {
        // Ignore Spelling: Da, Daltons

        /// <summary>
        /// Default divisor to use when compressing spectra and sicOptions.SICToleranceIsPPM is false
        /// </summary>
        public const double DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_DA = 5;

        /// <summary>
        /// Default divisor to use when compressing spectra and sicOptions.SICToleranceIsPPM is true
        /// </summary>
        public const double DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_PPM = 3;

        /// <summary>
        /// Provided by the user at the command line or obtained from the database (if a connection string is defined)
        /// 0 if unknown
        /// </summary>
        public int DatasetID { get; set; }

        /// <summary>
        /// SIC tolerance; defaults to 10 ppm
        /// </summary>
        public double SICTolerance { get; set; }

        /// <summary>
        /// When true, SICTolerance is treated as a PPM value
        /// </summary>
        public bool SICToleranceIsPPM { get; set; }

        /// <summary>
        /// SIC tolerance, in Daltons
        /// </summary>
        public double SICToleranceDa
        {
            get
            {
                if (SICToleranceIsPPM)
                {
                    // Return the Da tolerance value that will result for the given ppm tolerance at 1000 m/z
                    return clsParentIonProcessing.GetParentIonToleranceDa(this, 1000);
                }

                return SICTolerance;
            }
            set => SetSICTolerance(value, false);
        }
        /// <summary>
        /// If True, will look through the m/z values in the parent ion spectrum data to find the closest match
        /// (within SICToleranceDa / sicOptions.CompressToleranceDivisorForDa); will update the reported m/z value to the one found
        /// </summary>
        public bool RefineReportedParentIonMZ { get; set; }

        /// <summary>
        /// Start scan to process
        /// </summary>
        /// <remarks>
        /// If both ScanRangeStart >=0 and ScanRangeEnd > 0, only process data between those scan numbers
        /// </remarks>
        public int ScanRangeStart { get; set; }

        /// <summary>
        /// End scan to process
        /// </summary>
        /// <remarks>
        /// If both ScanRangeStart >=0 and ScanRangeEnd > 0, only process data between those scan numbers
        /// </remarks>
        public int ScanRangeEnd { get; set; }

        /// <summary>
        /// Number of scans we will process, according the ScanRangeStart and ScanRangeEnd. '-1' means no limit.
        /// </summary>
        public int ScanRangeCount
        {
            get
            {
                if (ScanRangeStart >= 0 && ScanRangeEnd > ScanRangeStart)
                {
                    return ScanRangeEnd - ScanRangeStart + 1;
                }

                return -1;
            }
        }

        /// <summary>
        /// Elution time of the first scan to process
        /// </summary>
        /// <remarks>
        /// If both RTRangeStart >=0 and RTRangeEnd > RTRangeStart, only process data between those that scan range (in minutes)
        /// </remarks>
        public float RTRangeStart { get; set; }

        /// <summary>
        /// Elution time of the last scan to process
        /// </summary>
        /// <remarks>
        /// If both RTRangeStart >=0 and RTRangeEnd > RTRangeStart, only process data between those that scan range (in minutes)
        /// </remarks>
        public float RTRangeEnd { get; set; }

        /// <summary>
        /// If true, combines data points that have similar m/z values (within tolerance) when loading
        /// Tolerance is sicOptions.SICToleranceDa / sicOptions.CompressToleranceDivisorForDa
        /// (or divided by sicOptions.CompressToleranceDivisorForPPM if sicOptions.SICToleranceIsPPM=True)
        /// </summary>
        public bool CompressMSSpectraData { get; set; }

        /// <summary>
        /// If true, combines data points that have similar m/z values (within tolerance) when loading
        /// Tolerance is binningOptions.BinSize / sicOptions.CompressToleranceDivisorForDa
        /// </summary>
        public bool CompressMSMSSpectraData { get; set; }

        /// <summary>
        /// When compressing spectra, sicOptions.SICTolerance and binningOptions.BinSize will be divided by this value
        /// to determine the resolution to compress the data to
        /// </summary>
        public double CompressToleranceDivisorForDa { get; set; }

        /// <summary>
        /// If sicOptions.SICToleranceIsPPM is True, this divisor is used instead of CompressToleranceDivisorForDa
        /// </summary>
        public double CompressToleranceDivisorForPPM { get; set; }

        // The SIC is extended left and right until:
        // 1) the SIC intensity falls below IntensityThresholdAbsoluteMinimum,
        // 2) the SIC intensity falls below the maximum value observed times IntensityThresholdFractionMax,
        // or 3) the distance exceeds MaxSICPeakWidthMinutesBackward or MaxSICPeakWidthMinutesForward

        /// <summary>
        /// Maximum SIC peak width, in minutes, to the left of the peak center
        /// </summary>
        /// <remarks>
        /// Defaults to 5
        /// </remarks>
        public float MaxSICPeakWidthMinutesBackward
        {
            get => mMaxSICPeakWidthMinutesBackward;
            set
            {
                if (value is < 0 or > 10000)
                    value = 5;
                mMaxSICPeakWidthMinutesBackward = value;
            }
        }

        /// <summary>
        /// Maximum SIC peak width, in minutes, to the right of the peak center
        /// </summary>
        /// <remarks>
        /// Defaults to 5
        /// </remarks>
        public float MaxSICPeakWidthMinutesForward
        {
            get => mMaxSICPeakWidthMinutesForward;
            set
            {
                if (value is < 0 or > 10000)
                    value = 5;
                mMaxSICPeakWidthMinutesForward = value;
            }
        }

        /// <summary>
        /// SIC peak finder options
        /// </summary>
        public MASICPeakFinder.clsSICPeakFinderOptions SICPeakFinderOptions { get; set; }

        /// <summary>
        /// When true, replace zeros with the minimum positive value from MS data
        /// </summary>
        public bool ReplaceSICZeroesWithMinimumPositiveValueFromMSData { get; set; }

        /// <summary>
        /// When true, save smoothed data
        /// </summary>
        public bool SaveSmoothedData { get; set; }

        /// <summary>
        /// m/z Tolerance for finding similar parent ions; full tolerance is +/- this value
        /// </summary>
        /// <remarks>Defaults to 0.1</remarks>
        public float SimilarIonMZToleranceHalfWidth
        {
            get => mSimilarIonMZToleranceHalfWidth;
            set
            {
                if (value < 0.001 || value > 100)
                    value = 0.1F;
                mSimilarIonMZToleranceHalfWidth = value;
            }
        }

        /// <summary>
        /// Time Tolerance (in minutes) for finding similar parent ions; full tolerance is +/- this value
        /// </summary>
        /// <remarks>Defaults to 5</remarks>
        public float SimilarIonToleranceHalfWidthMinutes
        {
            get => mSimilarIonToleranceHalfWidthMinutes;
            set
            {
                if (value is < 0 or > 100000)
                    value = 5;
                mSimilarIonToleranceHalfWidthMinutes = value;
            }
        }

        /// <summary>
        /// Spectrum similarity threshold
        /// </summary>
        /// <remarks>
        /// Defaults to 0.8
        /// </remarks>
        public float SpectrumSimilarityMinimum
        {
            get => mSpectrumSimilarityMinimum;
            set
            {
                if (value is < 0 or > 1)
                    value = 0.8F;
                mSpectrumSimilarityMinimum = value;
            }
        }

        private float mMaxSICPeakWidthMinutesBackward;
        private float mMaxSICPeakWidthMinutesForward;

        /// <summary>
        /// Obtain the SIC tolerance
        /// </summary>
        public double GetSICTolerance()
        {
            return GetSICTolerance(out _);
        }

        /// <summary>
        /// Obtain the SIC tolerance, along with a flag that indicates if the tolerance is PPM-based
        /// </summary>
        /// <param name="toleranceIsPPM"></param>
        public double GetSICTolerance(out bool toleranceIsPPM)
        {
            toleranceIsPPM = SICToleranceIsPPM;
            return SICTolerance;
        }

        /// <summary>
        /// Reset options to default
        /// </summary>
        public void Reset()
        {
            SICTolerance = 10;
            SICToleranceIsPPM = true;

            // Typically only useful when using a small value for .SICTolerance
            RefineReportedParentIonMZ = false;

            ScanRangeStart = 0;
            ScanRangeEnd = 0;
            RTRangeStart = 0;
            RTRangeEnd = 0;

            CompressMSSpectraData = true;
            CompressMSMSSpectraData = true;

            CompressToleranceDivisorForDa = DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_DA;
            CompressToleranceDivisorForPPM = DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_PPM;

            MaxSICPeakWidthMinutesBackward = 5;
            MaxSICPeakWidthMinutesForward = 5;

            ReplaceSICZeroesWithMinimumPositiveValueFromMSData = true;

            SICPeakFinderOptions = MASICPeakFinder.clsMASICPeakFinder.GetDefaultSICPeakFinderOptions();

            SaveSmoothedData = false;

            // Note: When using narrow SIC tolerances, be sure to SimilarIonMZToleranceHalfWidth to a smaller value
            // However, with very small values, the SpectraCache file will be much larger
            // The default for SimilarIonMZToleranceHalfWidth is 0.1
            // Consider using 0.05 when using ppm-based SIC tolerances
            SimilarIonMZToleranceHalfWidth = 0.05F;

            // SimilarIonScanToleranceHalfWidth = 100
            SimilarIonToleranceHalfWidthMinutes = 5;
            SpectrumSimilarityMinimum = 0.8F;
        }

        /// <summary>
        /// Set the SIC tolerance
        /// </summary>
        /// <param name="toleranceValue"></param>
        /// <param name="toleranceIsPPM"></param>
        public void SetSICTolerance(double toleranceValue, bool toleranceIsPPM)
        {
            SICToleranceIsPPM = toleranceIsPPM;

            if (SICToleranceIsPPM)
            {
                if (toleranceValue is < 0 or > 1000000)
                    toleranceValue = 100;
            }
            else if (toleranceValue is < 0 or > 10000)
            {
                toleranceValue = 0.6;
            }

            SICTolerance = toleranceValue;
        }

        /// <summary>
        /// Validate SIC options
        /// </summary>
        public void ValidateSICOptions()
        {
            if (CompressToleranceDivisorForDa < 1)
            {
                CompressToleranceDivisorForDa = DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_DA;
            }

            if (CompressToleranceDivisorForPPM < 1)
            {
                CompressToleranceDivisorForPPM = DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_PPM;
            }
        }

        /// <summary>
        /// Show the SIC tolerance
        /// </summary>
        public override string ToString()
        {
            var toleranceUnits = SICToleranceIsPPM ? "ppm" : "Da";

            return string.Format("SIC Tolerance: {0:F2} {1}", SICTolerance, toleranceUnits);
        }

        private float mSimilarIonMZToleranceHalfWidth = 0.1F;
        private float mSimilarIonToleranceHalfWidthMinutes = 5;
        private float mSpectrumSimilarityMinimum = 0.8F;
    }
}
