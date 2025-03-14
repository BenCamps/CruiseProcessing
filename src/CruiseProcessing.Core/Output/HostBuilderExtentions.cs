using CruiseProcessing.Output.R1;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CruiseProcessing.Output
{
    public static class HostBuilderExtentions
    {
        public static IServiceCollection AddOutputReportGenerators(this IServiceCollection serviceProvider)
        {
            serviceProvider.AddTransient<BiomassEquationReportGenerator>();
            serviceProvider.AddTransient<OutputTea>();

            serviceProvider.AddKeyedTransient<IReportGenerator, Wt1ReportGenerator>("WT1");
            serviceProvider.AddKeyedTransient<IReportGenerator, Wt2ReportGenerator>("WT2");
            serviceProvider.AddKeyedTransient<IReportGenerator, Wt3ReportGenerator>("WT3");
            serviceProvider.AddKeyedTransient<IReportGenerator, Wt4ReportGenerator>("WT4");
            serviceProvider.AddKeyedTransient<IReportGenerator, Wt5ReportGenerator>("WT5");

            serviceProvider.AddKeyedTransient<IReportGenerator, R102ReportGenerator>("R102");
            serviceProvider.AddKeyedTransient<IReportGenerator, R103ReportGenerator>("R103");

            return serviceProvider;
        }
    }
}
