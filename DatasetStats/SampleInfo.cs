namespace MASIC.DatasetStats
{
    /// <summary>
    /// Container for sample information
    /// </summary>
    public class SampleInfo
    {
        // Ignore Spelling: MASIC

        /// <summary>
        /// Sample name
        /// </summary>
        public string SampleName { get; set; }

        /// <summary>
        /// Primary comment
        /// </summary>
        public string Comment1 { get; set; }

        /// <summary>
        /// Secondary comment
        /// </summary>
        public string Comment2 { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SampleInfo()
        {
            Clear();
        }

        /// <summary>
        /// Clear the options
        /// </summary>
        public void Clear()
        {
            SampleName = string.Empty;
            Comment1 = string.Empty;
            Comment2 = string.Empty;
        }

        /// <summary>
        /// True if SampleName, Comment1, or Comment2 has text
        /// </summary>
        public bool HasData()
        {
            return !string.IsNullOrWhiteSpace(SampleName) ||
                   !string.IsNullOrWhiteSpace(Comment1) ||
                   !string.IsNullOrWhiteSpace(Comment2);
        }

        /// <summary>
        /// Show the sample name
        /// </summary>
        public override string ToString()
        {
            return SampleName;
        }
    }
}
