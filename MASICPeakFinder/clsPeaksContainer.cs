using System.Collections.Generic;

namespace MASICPeakFinder
{
    public class clsPeaksContainer
    {
        public int OriginalPeakLocationIndex { get; set;
         }
        public int SourceDataCount { get; set; }

        public double[] XData;
        public double[] YData;
        public double[] SmoothedYData;

        public List<clsPeakInfo> Peaks { get; set; }

        public int PeakWidthPointsMinimum { get; set; }
        public double MaxAllowedUpwardSpikeFractionMax { get; set; }
        public int BestPeakIndex { get; set; }
        public double BestPeakArea { get; set; }

        public clsPeaksContainer()
        {
            Peaks = new List<clsPeakInfo>();
        }

        public clsPeaksContainer Clone(bool skipSourceData = false)
        {
            var clonedContainer = new clsPeaksContainer()
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
                clonedContainer.Peaks.Add(sourcePeak.Clone());

            return clonedContainer;
        }
    }
}