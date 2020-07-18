using System;
using MASIC.Options;
using PRISM;

namespace MASIC.DataOutput
{
    public class StatsPlotter : EventNotifier
    {
        private readonly StatsSummarizer mStatsSummarizer;

        /// <summary>
        /// MASIC Options
        /// </summary>
        public MASICOptions Options { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options">MASIC Options</param>
        public StatsPlotter(MASICOptions options)
        {
            Options = options;

            mStatsSummarizer = new StatsSummarizer(Options);
            RegisterEvents(mStatsSummarizer);
        }

        private bool CreatePlots(string outputDirectory)
        {
            return false;
        }

        /// <summary>
        /// Read the SIC stats file (and optionally reporter ions file)
        /// Generate stats, then create plots
        /// </summary>
        /// <returns></returns>
        public bool ProcessFile(string sicStatsFilePath, string outputDirectory)
        {
            try
            {
                var statsSummarized = mStatsSummarizer.SummarizeSICStats(sicStatsFilePath);
                if (!statsSummarized)
                    return false;

                var plotsGenerated = CreatePlots(outputDirectory);

                return plotsGenerated;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.GenerateAndPlotStats", ex);
                return false;
            }
        }
    }
}
