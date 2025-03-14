using CruiseDAL;
using CruiseProcessing.Data;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace CruiseProcessing.Test.Output
{
    public class TestHelpers : TestBase
    {
        public TestHelpers(ITestOutputHelper output) : base(output)
        {
        }

        private class ReportEntry
        { public string ReportID { get; set; } public bool IsSelected { get; set; } }

        [Fact]
        public void OutputReportMatrix()
        {
            var fileNameColumnWidth = 80 + 50;
            var reportMatrixColumnWidth = 5;

            var fileExtentions = new[] { ".crz3", ".cruise" };

            var testCruiseFiles = Directory.EnumerateFiles(TestFilesDirectory, "*.*", SearchOption.AllDirectories)
                .Where(x => fileExtentions.Contains(Path.GetExtension(x)));

            // output header
            Output.WriteLine("".PadRight(fileNameColumnWidth) +","+ string.Join(",", ReportsDataservice.REPORT_OBJECT_ARRAY.Select(x => x.ReportID.PadRight(reportMatrixColumnWidth)).ToArray()));

            List<string> errors = new List<string>();

            foreach (var file in testCruiseFiles)
            {
                

                var reportMatrix = ReportsDataservice.REPORT_OBJECT_ARRAY.Select(x => new ReportEntry { ReportID = x.ReportID, IsSelected = false })
                    .ToArray();

                var testFile = GetTestFileFromFullPath(file);

                var extention = Path.GetExtension(file);
                CpDataLayer dataLayer = null;
                try
                {
                    if (extention == ".cruise")
                    {
                        var db = new DAL(file);
                        dataLayer = new CpDataLayer(db, CreateLogger<CpDataLayer>(), biomassOptions: null);
                    }
                    else if (extention == ".crz3")
                    {
                        var migrator = new DownMigrator();
                        var v3Db = new CruiseDatastore_V3(testFile);
                        var v2Db = new DAL();
                        var cruiseID = v3Db.QueryScalar<string>("SELECT CruiseID FROM Cruise").First();
                        migrator.MigrateFromV3ToV2(cruiseID, v3Db, v2Db);

                        dataLayer = new CpDataLayer(v2Db, CreateLogger<CpDataLayer>(), biomassOptions: null);
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"Failed to open file: {file}::::{e.GetType().Name + e.Message.Substring(0, Math.Min(80, e.Message.Length))}");

                    //Output.WriteLine($"Failed to open file: {file}");
                    //Output.WriteLine(e.Message);
                    continue;
                }

                var selectedReports = dataLayer.GetSelectedReports();
                foreach (var report in selectedReports)
                {
                    reportMatrix.First(x => x.ReportID == report.ReportID).IsSelected = true;
                }

                var fileName = file.Remove(0, TestFilesDirectory.Length);
                //var fileName = Path.GetFileName(file);
                Output.WriteLine(fileName.PadRight(fileNameColumnWidth) + "," + string.Join(",", reportMatrix.Select(x => (x.IsSelected ? "X" : " ").PadRight(reportMatrixColumnWidth)).ToArray()));
            }

            foreach (var error in errors)
            {
                Output.WriteLine(error);
            }
        }

    }
}
