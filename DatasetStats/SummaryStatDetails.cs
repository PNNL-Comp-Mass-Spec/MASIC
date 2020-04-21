namespace MASIC.DatasetStats
{
    public class SummaryStatDetails
    {
        /// <summary>
        /// Scan count
        /// </summary>
        public int ScanCount { get; set; }

        /// <summary>
        /// Max TIC
        /// </summary>
        public double TICMax { get; set; }

        /// <summary>
        /// Max BPI
        /// </summary>
        public double BPIMax { get; set; }

        /// <summary>
        /// Median TIC
        /// </summary>
        public double TICMedian { get; set; }

        /// <summary>
        /// Median BPI
        /// </summary>
        public double BPIMedian { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SummaryStatDetails()
        {
            Clear();
        }

        public void Clear()
        {
            ScanCount = 0;
            TICMax = 0;
            BPIMax = 0;
            TICMedian = 0;
            BPIMedian = 0;
        }

        public override string ToString()
        {
            return string.Format("ScanCount: {0}", ScanCount);
        }
    }
}
