using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CruiseProcessing.Test.Shared
{
    public class TestFilesHelper
    {
        public static string TestExecutionDirectory
        {
            get
            {
                var codeBase = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
                return Path.GetDirectoryName(codeBase);
            }
        }

        public static string TestFilesDirectory => Path.Combine(TestExecutionDirectory, "TestFiles");


        public static IEnumerable<string[]> GetTestFileNames()
        {
            var fileExtentions = new[] { ".crz3", ".cruise" };

            var testFileDir = TestFilesDirectory;
            var testCruiseFiles = Directory.EnumerateFiles(TestFilesDirectory, "*.*", SearchOption.AllDirectories)
                .Where(x => fileExtentions.Contains(Path.GetExtension(x)));

            foreach (var file in testCruiseFiles)
            {
                var fileName = Path.GetFileName(file);
                var directory = Path.GetDirectoryName(file);
                var outFileName = fileName + ".out";

                var outPath = Path.Combine(directory, outFileName);
                if (File.Exists(outPath))
                {
                    var filePathShort = file.Remove(0, testFileDir.Length);
                    var outPathShort = outPath.Remove(0, testFileDir.Length);

                    yield return new[] { filePathShort, outPathShort };
                }
            }
        }
    }
}
