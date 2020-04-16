namespace MASIC.DatasetStats
{
    public class SampleInfo
    {
        public string SampleName { get; set; }
        public string Comment1 { get; set; }
        public string Comment2 { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SampleInfo()
        {
            Clear();
        }

        public void Clear()
        {
            SampleName = string.Empty;
            Comment1 = string.Empty;
            Comment2 = string.Empty;
        }

        public bool HasData()
        {
            if (!string.IsNullOrWhiteSpace(SampleName) || !string.IsNullOrWhiteSpace(Comment1) || !string.IsNullOrWhiteSpace(Comment2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return SampleName;
        }
    }
}