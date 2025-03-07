using CruiseDAL;
using CruiseProcessing.Data;
using CruiseProcessing.Interop;
using CruiseProcessing.Processing;
using CruiseProcessing.ReferenceImplmentation;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace CruiseProcessing.Test.Processing
{
    public class CalculateTreeValues2_Test : CalculateTreeValuesTestBase
    {
        public CalculateTreeValues2_Test(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData("OgTest\\Region1\\R1_FrenchGulch.cruise")]
        [InlineData("OgTest\\Region2\\R2_Test.cruise")]
        [InlineData("OgTest\\Region2\\R2_Test_V3.process")]
        [InlineData("OgTest\\Region3\\R3_FCM_100.cruise")]
        [InlineData("OgTest\\Region3\\R3_PCM_FIXCNT.cruise")]
        [InlineData("OgTest\\Region3\\R3_PNT_FIXCNT.cruise")]
        [InlineData("OgTest\\Region4\\R4_McDougal.cruise")]
        [InlineData("OgTest\\Region5\\R5.cruise")]
        [InlineData("OgTest\\Region6\\R6.cruise")]
        [InlineData("OgTest\\Region8\\R8.cruise")]
        [InlineData("OgTest\\Region9\\R9.cruise")]
        [InlineData("OgTest\\Region10\\R10.cruise")]

        [InlineData("Version3Testing\\3P\\87654 test 3P TS.cruise")]
        [InlineData("Version3Testing\\3P\\87654_test 3P_Timber_Sale_26082021.process")]
        [InlineData("Version3Testing\\3P\\87654_test 3P_Timber_Sale_26082021_fixedTallyBySp.process")]
        [InlineData("Version3Testing\\3P\\87654_Test 3P_Timber_Sale_30092021.process")]

        [InlineData("Version3Testing\\FIX\\20301 Cold Springs Recon.cruise")]
        [InlineData("Version3Testing\\FIX\\20301_Cold Springs_Timber_Sale_29092021.process")]

        [InlineData("Version3Testing\\FIX and PNT\\99996_TestMeth_Timber_Sale_08072021.process")]

        [InlineData("Version3Testing\\PCM\\27504_Spruce East_TS.cruise")]

        [InlineData("Version3Testing\\PNT\\Exercise3_Dead_LP_Recon.cruise")]

        [InlineData("Version3Testing\\STR\\98765 test STR TS.cruise")]
        [InlineData("Version3Testing\\STR\\98765_test STR_Timber_Sale_26082021.process")]
        [InlineData("Version3Testing\\STR\\98765_test STR_Timber_Sale_30092021.process")]

        [InlineData("Version3Testing\\TestMeth\\99996_TestMeth_TS_202310040107_KC'sTabActive3-R9Q8.process")]

        [InlineData("Version3Testing\\27504PCM_Spruce East_Timber_Sale.cruise")]
        [InlineData("Version3Testing\\99996FIX_PNT_Timber_Sale_08242021.cruise")]
        [InlineData("Issues\\20383_Jiffy Stewardship_TS.04.30.24.process")]

        [InlineData("Issues\\20383_Jiffy Stewardship_TS.crz3")]
        public CpDataLayer ProcessTrees(string fileName)
        {
            var filePath = GetTestFile(fileName);
            var dataLayer = GetCpDataLayer(filePath);

            var host = CreateTestHost(sc =>
            {
                sc.AddTransient<CpDataLayer>(x => dataLayer);
                sc.AddTransient<RefCalculateTreeValues>();
                sc.AddTransient<CalculateTreeValues2>();
            });

            var ctv = host.Services.GetRequiredService<RefCalculateTreeValues>();
            var trees = dataLayer.getTrees();
            trees.All(x => x.TreeDefaultValue_CN != null && x.TreeDefaultValue_CN > 0)
                .Should().BeTrue();

            var ctv2 = host.Services.GetRequiredService<CalculateTreeValues2>();
            ReprocessCruise(dataLayer, ctv2);
            

            var tcvLookup = dataLayer.getTreeCalculatedValues().ToLookup(x => x.Tree_CN.Value);


            foreach (var t in trees.Where(x => x.CountOrMeasure == "M"))
            {
                tcvLookup.Contains(t.Tree_CN.Value).Should().BeTrue();
            }

            return dataLayer;
        }
    }
}
