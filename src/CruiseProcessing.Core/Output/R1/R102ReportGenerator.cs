using CruiseProcessing.Data;
using Microsoft.Extensions.Logging;

namespace CruiseProcessing.Output.R1
{
    public class R102ReportGenerator : R1LogMethSummaryReportGeneratorBase
    {
        public R102ReportGenerator(CpDataLayer dataLayer, ILogger<R102ReportGenerator> log) : base(dataLayer, "R102", log)
        {
        }
    }
}