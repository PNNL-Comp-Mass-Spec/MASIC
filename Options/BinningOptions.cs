namespace MASIC.Options
{
    /// <summary>
    /// Histogram binning options
    /// </summary>
    public class BinningOptions
    {
        /// <summary>
        /// X-value of the first bin
        /// </summary>
        public float StartX { get; set; }

        /// <summary>
        /// X-value for the last bin
        /// </summary>
        public float EndX { get; set; }

        /// <summary>
        /// Bin size
        /// </summary>
        public float BinSize
        {
            get => mBinSize;
            set
            {
                if (value <= 0)
                    value = 1;
                mBinSize = value;
            }
        }

        /// <summary>
        /// Intensity precision, as a value between 0 and 100
        /// </summary>
        public float IntensityPrecisionPercent
        {
            get => mIntensityPrecisionPercent;
            set
            {
                if (value is < 0 or > 100)
                    value = 1;
                mIntensityPrecisionPercent = value;
            }
        }

        /// <summary>
        /// When true, normalize the values
        /// </summary>
        public bool Normalize { get; set; }

        /// <summary>
        /// Sum all of the intensities for binned ions of the same bin together
        /// </summary>
        public bool SumAllIntensitiesForBin { get; set; }

        /// <summary>
        /// Maximum number of bins to allow
        /// </summary>
        /// <remarks>
        /// Bin count is auto-determined as (EndX - StartX) / BinSize
        /// </remarks>
        public int MaximumBinCount
        {
            get => mMaximumBinCount;
            set
            {
                if (value < 2)
                    value = 10;

                if (value > 1000000)
                    value = 1000000;
                mMaximumBinCount = value;
            }
        }

        private float mBinSize = 1;
        private float mIntensityPrecisionPercent = 1;
        private int mMaximumBinCount = 100000;

        /// <summary>
        /// Reset binning options to default
        /// </summary>
        public void Reset()
        {
            var defaultOptions = Correlation.GetDefaultBinningOptions();

            StartX = defaultOptions.StartX;
            EndX = defaultOptions.EndX;
            BinSize = defaultOptions.BinSize;
            IntensityPrecisionPercent = defaultOptions.IntensityPrecisionPercent;
            Normalize = defaultOptions.Normalize;
            SumAllIntensitiesForBin = defaultOptions.SumAllIntensitiesForBin;
            MaximumBinCount = defaultOptions.MaximumBinCount;
        }
    }
}
