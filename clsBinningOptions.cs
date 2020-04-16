
namespace MASIC
{
    public class clsBinningOptions
    {

        #region // TODO
        public float StartX { get; set; }
        public float EndX { get; set; }

        public float BinSize
        {
            get
            {
                return mBinSize;
            }

            set
            {
                if (value <= 0)
                    value = 1;
                mBinSize = value;
            }
        }

        public float IntensityPrecisionPercent
        {
            get
            {
                return mIntensityPrecisionPercent;
            }

            set
            {
                if (value < 0 | value > 100)
                    value = 1;
                mIntensityPrecisionPercent = value;
            }
        }

        public bool Normalize { get; set; }

        /// <summary>
        /// Sum all of the intensities for binned ions of the same bin together
        /// </summary>
        /// <returns></returns>
        public bool SumAllIntensitiesForBin { get; set; }

        public int MaximumBinCount
        {
            get
            {
                return mMaximumBinCount;
            }

            set
            {
                if (value < 2)
                    value = 10;
                if (value > 1000000)
                    value = 1000000;
                mMaximumBinCount = value;
            }
        }

        #endregion
        #region // TODO
        private float mBinSize = 1;
        private float mIntensityPrecisionPercent = 1;
        private int mMaximumBinCount = 100000;
        #endregion
        public void Reset()
        {
            var defaultOptions = clsCorrelation.GetDefaultBinningOptions();
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