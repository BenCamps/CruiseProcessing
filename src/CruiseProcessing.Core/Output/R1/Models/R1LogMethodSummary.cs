using System.Collections.Generic;

namespace CruiseProcessing.Output.R1.Models
{
    public class R1LogMethodSummary
    {
        public IList<R1LogMethodGroup> LogMethGroups { get; set; }

        public R1LogMethTotals Totals { get; set; }
    }
}