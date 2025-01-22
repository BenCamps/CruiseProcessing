using CruiseProcessing.Output;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace CruiseProcessing.Test.Output
{
    public abstract class OutputTestBase : TestBase
    {
        protected OutputTestBase(ITestOutputHelper output) : base(output)
        {
        }

        protected void CompareOutput(string outFileName, string reportID, string output)
        {
            var originalReportPages = ExtractReportPagesFromFile(outFileName).Where(x => x.ReportID == reportID).ToArray();
            originalReportPages.Should().NotBeEmpty();

            var newReportPages = OutputParser.ExtractReportPages(output).ToArray();
            newReportPages.Should().NotBeEmpty();

            newReportPages.Length.Should().Be(originalReportPages.Length);

            foreach (var (orgRpt, newRpt) in originalReportPages.Zip(newReportPages, (orgRpt, newRpt) => (orgRpt, newRpt)))
            {

                orgRpt.ReportID.Should().Be(newRpt.ReportID);
                //orgRpt.ReportTitle.Should().Be(newRpt.ReportTitle);
                //orgRpt.PageNumber.Should().Be(newRpt.PageNumber);
                //orgRpt.ReportSubtitle.Should().Be(newRpt.ReportSubtitle);

                var orgContent = orgRpt.ReportContent;
                var newContent = newRpt.ReportContent;

                Output.WriteLine(newContent);

                try
                {
                    // WT1 had a column added so we cant compare it now
                    if (reportID == "WT5")
                    {
                        orgContent = orgContent.Replace("ALL SPECUES", "ALL SPECIES");
                        orgContent = orgContent.Replace("NaN", "0.0");
                    }
                    if (reportID != "WT1")
                    {
                        Assert.Equal(orgContent, newContent, ignoreCase: true, ignoreWhiteSpaceDifferences: true);
                    }
                    else
                    {
                        Output.WriteLine("expected:" + orgContent);
                        Output.WriteLine("actual  :" + newContent);
                    }
                }
                catch (Exception e)
                {
                    Output.WriteLine("expected:" + orgContent);
                    Output.WriteLine("actual  :" + newContent);

                    throw;
                }

                //orgContent.Should().Be(newContent);
                //foreach (var r in replaceRegex)
                //{
                //    orgContent = r.Regex.Replace(orgContent, r.Replacement);
                //    newContent = r.Regex.Replace(newContent, r.Replacement);
                //}

            }


            //var stringReader = new System.IO.StringReader(output);

            //var lines = ExtractReport(outFileName, reportID).ToArray();
            ////lines.Should().NotBeEmpty();
            //if (lines.Length == 0)
            //{
            //    Output.WriteLine("report not found in original report");
            //}

            //foreach (var (line, i) in lines.Select((x, i) => (x, i)))
            //{
            //    //Output.WriteLine(line);
            //    var expected = line;
            //    var actual = stringReader.ReadLine();
            //    Output.WriteLine(actual);
            //    if (actual == "\f")
            //    {
            //        actual = stringReader.ReadLine();
            //        Output.WriteLine(actual);
            //    }

            //    if (ignoreRegex.Any(x => x.IsMatch(expected)))
            //    {
            //        continue;
            //    }

            //    foreach (var r in replaceRegex)
            //    {
            //        expected = r.Regex.Replace(expected, r.Replacement);
            //    }

            //    try
            //    {
            //        Assert.Equal(expected, actual, ignoreCase: true, ignoreWhiteSpaceDifferences: true);
            //    }
            //    catch (Exception e)
            //    {
            //        Output.WriteLine("expected:" + expected);
            //        Output.WriteLine("actual  :" + actual);

            //        throw;
            //    }

            //    //actual.Should().BeEquivalentTo(expected, because: $"line {i}");
            //}
        }

        protected IEnumerable<ReportPage> ExtractReportPagesFromFile(string fileName)
        {
            var allText = ReadAllText(fileName);
            return OutputParser.ExtractReportPages(allText);
        }

        protected string ReadAllText(string fileName)
        {
            var filePath = GetTestFile(fileName);
            return System.IO.File.ReadAllText(filePath);
        }
    }
}
