using CruiseProcessing.Data;
using CruiseProcessing.Output;
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
        public void GenerateReport(string testFileName)
        {
            var filePath = GetTestFile(testFileName);

            var dataLayer = GetCpDataLayer(filePath);

            var host = CreateTestHost(sc =>
            {
                sc.AddOutputReportGenerators();
                sc.AddSingleton<CpDataLayer>(dataLayer);
            });

            var services = host.Services;
            var reprortGenerator = services.GetRequiredService<OutputTea>();

            var writer = new StringWriter();
            reprortGenerator.GenerateReport(writer, dataLayer.GetReportHeaderData(), startPageNum: 0);

            Output.WriteLine(writer.ToString());

        }
    }
}
