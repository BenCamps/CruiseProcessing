using CruiseDAL.DataObjects;
using CruiseProcessing.Data;
using CruiseProcessing.Interop;
using CruiseProcessing.Processing;
using CruiseProcessing.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace CruiseProcessing.Test.Output
{
    public class OutputLogStock_Test : OutputTestBase
    {
        public OutputLogStock_Test(ITestOutputHelper output) : base(output)
        {
        }

        //protected override void ConfigureServices(IServiceCollection services)
        //{
        //    base.ConfigureServices(services);

        //    services.AddTransient<ICruiseProcessor, CruiseProcessor>();

        //}

        [Theory]
        [InlineData("Version3Testing\\STR\\98765_test STR_Timber_Sale_30092021.crz3", "Version3Testing\\STR\\98765_test STR_Timber_Sale_30092021.out")]
        public void OutputL8Report(string testFileName, string outFileName)
        {
            var reportID = "L8";
            var reprocess = true;


            var filePath = GetTestFile(testFileName);

            using CpDataLayer dataLayer = GetCpDataLayer(filePath);

            // if comparing to original out file we should check to see if the report is selected
            // otherwise we can still try to generate the report
            if (outFileName != null)
            {
                dataLayer.GetSelectedReports().Select(x => x.ReportID).Should().Contain(reportID);
            }

            //if(updateBio)
            //{
            //    BiomassHelpers.UpdateBiomass(dataLayer);
            //}

            if (reprocess)
            {
                BiomassHelpers.UpdateBiomass(dataLayer);

                List<ErrorLogDO> fscList = dataLayer.getErrorMessages("E", "FScruiser");
                var errors = EditChecks.CheckErrors(dataLayer);

                if (fscList.Any())
                { throw new Exception("Skip - Cruise FSC errors"); }
                if (errors.Any())
                {
                    foreach (var error in errors)
                    {
                        Output.WriteLine($"Table: {error.TableName} Msg:{ErrorReport.GetErrorMessage(error.Message)}");
                    }

                    throw new Exception("Skip - Cruise errors");
                }

                var processor = new CruiseProcessor(dataLayer, Substitute.For<IDialogService>(), CreateLogger<CruiseProcessor>());
                processor.ProcessCruise(null);
            }

            var headerData = dataLayer.GetReportHeaderData();

            var reportGenerator = new OutputLogStock(dataLayer, headerData, reportID);

            var writer = new System.IO.StringWriter();
            int pageNum = 0;
            reportGenerator.CreateLogReports(writer, ref pageNum);

            var output = writer.ToString();
            output.Should().NotBeNullOrEmpty();
            if (outFileName != null)
            {
                CompareOutput(outFileName, reportID, output);
            }
            else
            {
                Output.WriteLine(output);
            }
        }
    }
}
