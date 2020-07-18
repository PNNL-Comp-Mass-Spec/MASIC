using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MASIC.Options;
using PRISM;

namespace MASIC.DataOutput
{
    public class StatsSummarizer : EventNotifier
    {

        #region "Properties"

        /// <summary>
        /// MASIC Options
        /// </summary>
        public MASICOptions Options { get; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options">MASIC Options</param>
        public StatsSummarizer(MASICOptions options)
        {
            Options = options;
        }
        {
        }
    }
}
