using System.Collections.Generic;

namespace CruiseProcessing.Output.R1.Models
{
    public class R1LogMethodGroup
    {
        public string LoggingMethod { get; set; }

        public IList<R1LoggingMethodProductGroup> Products { get; set; }

        public R1LogMethTotals Totals { get; set; }
    }
}