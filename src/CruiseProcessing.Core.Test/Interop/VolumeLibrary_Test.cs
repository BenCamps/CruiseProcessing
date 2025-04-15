using CruiseProcessing.Interop;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace CruiseProcessing.Test.Interop
{
    public class VolumeLibrary_Test : TestBase
    {
        const int VOLLIB_VERSION = 20250401;

        public VolumeLibrary_Test(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void GetVersionNumber()
        {
            var platform = Environment.Is64BitProcess ? "x64" : "x86";
            Output.WriteLine($"Platform {platform}");
            var volumeLibrary = new VolumeLibrary();

            var versionNum = volumeLibrary.GetVersionNumber();
            versionNum.Should().Be(VOLLIB_VERSION);

            var sVerNum = VolumeLibraryExtentions.VolLibVersionNumberToString(versionNum);
            
            Version.TryParse(sVerNum, out var ver).Should().BeTrue(); // ensure version number is properly formatted
            sVerNum.Should().NotBe("0.0.0.0");

        }

        [Theory]
        [InlineData(1, "01", "01", 101, "01")]
        public void LookupVolumeEquation(int region, string forest, string district, int fiaCode, string product)
        {

            var volLib = new VolumeLibrary();

            var volEq = volLib.LookupVolumeEquation(region, forest, district, fiaCode, product, out var error);
            Output.WriteLine($"VolumeEquation: {volEq}");
            error.Should().Be(0);
            volEq.Should().NotBeNull();
        }

        [Theory]
        [InlineData(1, "01", "01", 101)]
        public void LookupVolumeEquationNVB(int region, string forest, string district, int fiaCode)
        {
            var volLib = new VolumeLibrary();

            var volEq = volLib.LookupVolumeEquationNVB(region, forest, district, fiaCode, out var error);
            Output.WriteLine($"VolumeEquation: {volEq}");
            error.Should().Be(0);
            volEq.Should().NotBeNull();
        }
    }
}
