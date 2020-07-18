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

        /// <summary>
        /// Read the SIC stats file (and optionally reporter ions file)
        /// Generate stats, then create plots
        /// </summary>
        /// <returns></returns>
        public bool ProcessFile(string sicStatsFilePath, string outputDirectory)
        {
            try
            {

                // ToDo: Code This:

                //var sicDataLoaded = LoadSICStats(sicStatsFilePath);

                //var reporterIonsFilePath = clsUtilities.ReplaceSuffix(sicStatsFilePath, clsDataOutput.SIC_STATS_FILE_SUFFIX, clsDataOutput.REPORTER_IONS_FILE_SUFFIX);

                //var reporterIonsLoaded = LoadReporterIons(sicStatsFilePath);

                //mStatsSummarizer.SummarizeSICStats();

                //mStatsSummarizer.ExamineReporterIons();

                //CreatePlots();

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in StatsPlotter.GenerateAndPlotStats", ex);
                return false;
            }
        }
    }
}
