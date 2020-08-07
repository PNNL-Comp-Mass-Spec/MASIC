namespace MASIC.DataOutput
{
    public class BoxPlotStats
    {
        public double FirstQuartile { get; set; }
        public double ThirdQuartile { get; set; }

        public double InterQuartileRange { get; set; }
        public double Median { get; set; }

        public double UpperWhisker  { get; set; }
        public double LowerWhisker { get; set; }

        public int NonZeroCount { get; set; }
        public int NumberOfOutliers { get; set; }
    }
}
