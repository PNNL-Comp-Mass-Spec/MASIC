using System.Collections.Generic;

namespace MASICPeakFinder
{
    /// <summary>
    /// Container for peak information
    /// </summary>
    public class clsPeaksContainer
    {
        /// <summary>
        /// Original peak location index
        /// </summary>
        public int OriginalPeakLocationIndex { get; set; }

        /// <summary>
        /// Source data count
        /// </summary>
        public int SourceDataCount { get; set; }

        /// <summary>
        /// X data
        /// </summary>
        public double[] XData;

        /// <summary>
        /// Y data
        /// </summary>
        public double[] YData;

        /// <summary>
        /// Smoothed Y data
        /// </summary>
        public double[] SmoothedYData;

        /// <summary>
        /// List of peaks found in the data
        /// </summary>
        public List<clsPeakInfo> Peaks { get; set; }

        /// <summary>
        /// Minimum peak width, in points
        /// </summary>
        public int PeakWidthPointsMinimum { get; set; }

        /// <summary>
        /// Maximum allowed upward spike, as a fraction of the maximum intensity
        /// </summary>
        public double MaxAllowedUpwardSpikeFractionMax { get; set; }

        /// <summary>
        /// Index of the best peak
        /// </summary>
        public int BestPeakIndex { get; set; }

        /// <summary>
        /// Area of the best peak
        /// </summary>
        public double BestPeakArea { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsPeaksContainer()
        {
            Peaks = new List<clsPeakInfo>();
        }

        /// <summary>
        /// Clone the peak
        /// </summary>
        /// <param name="skipSourceData"></param>
        public clsPeaksContainer Clone(bool skipSourceData = false)
        {
            var clonedContainer = new clsPeaksContainer
            {
                OriginalPeakLocationIndex = OriginalPeakLocationIndex,
                SourceDataCount = SourceDataCount,
                PeakWidthPointsMinimum = PeakWidthPointsMinimum,
                MaxAllowedUpwardSpikeFractionMax = MaxAllowedUpwardSpikeFractionMax,
                BestPeakIndex = BestPeakIndex,
                BestPeakArea = BestPeakArea
            };

            if (skipSourceData || SourceDataCount <= 0)
            {
                clonedContainer.SourceDataCount = 0;

                clonedContainer.XData = new double[0];
                clonedContainer.YData = new double[0];
                clonedContainer.SmoothedYData = new double[0];
            }
            else
            {
                clonedContainer.XData = new double[SourceDataCount + 1];
                clonedContainer.YData = new double[SourceDataCount + 1];
                clonedContainer.SmoothedYData = new double[SourceDataCount + 1];

                XData.CopyTo(clonedContainer.XData, 0);
                YData.CopyTo(clonedContainer.YData, 0);
                SmoothedYData.CopyTo(clonedContainer.SmoothedYData, 0);
            }

            clonedContainer.Peaks.Capacity = Peaks.Count;
            foreach (var sourcePeak in Peaks)
            {
                clonedContainer.Peaks.Add(sourcePeak.Clone());
            }

            return clonedContainer;
        }
    }
}