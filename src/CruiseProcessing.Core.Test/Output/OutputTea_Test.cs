using CruiseProcessing.Data;
using CruiseProcessing.Output;
using CruiseProcessing.Processing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace CruiseProcessing.Test.Output
{
    public class OutputTea_Test : TestBase
    {
        public OutputTea_Test(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData("SteveTest\\Region8\\BreadCreek.M.cruise")]
        [InlineData("SteveTest\\Region8\\V3_02363_Bonnerdale DXP_TS_CRZ.crz3")]
        public void GenerateReport(string testFileName)
        {
            var filePath = GetTestFile(testFileName);

            var dataLayer = GetCpDataLayer(filePath);

            var host = CreateTestHost(sc =>
            {
                sc.AddOutputReportGenerators();
                sc.AddSingleton<CpDataLayer>(dataLayer);
                sc.AddTransient<ICruiseProcessor, CruiseProcessor3>();
            });

            var services = host.Services;
            var processor = services.GetRequiredService<ICruiseProcessor>();
            processor.ProcessCruise(null);

            var reprortGenerator = services.GetRequiredService<OutputTea>();

            var writer = new StringWriter();
            reprortGenerator.GenerateReport(writer, dataLayer.GetReportHeaderData(), startPageNum: 0);

            Output.WriteLine(writer.ToString());

        }
    }
}
