using System.Collections.Generic;

namespace CruiseProcessing.Output.R1.Models
{
    public class R1LoggingMethodProductGroup
    {
        public string Product { get; set; }

        public IList<R1LogMethodSummeryItem> Items { get; set; }

        public R1LogMethTotals Totals { get; set; }
    }
}