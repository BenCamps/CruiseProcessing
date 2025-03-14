using CruiseProcessing.Data;
using Microsoft.Extensions.Logging;

namespace CruiseProcessing.Output.R1
{
    public class R103ReportGenerator : R1LogMethSummaryReportGeneratorBase
    {
        public R103ReportGenerator(CpDataLayer dataLayer, ILogger<R103ReportGenerator> log) : base(dataLayer, "R103", log)
        {
        }
    }
}