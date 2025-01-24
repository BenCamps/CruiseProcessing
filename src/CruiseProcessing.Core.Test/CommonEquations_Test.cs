using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace CruiseProcessing.Test
{
    public class CommonEquations_Test : TestBase
    {
        public CommonEquations_Test(ITestOutputHelper output) : base(output)
        {
        }


        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(0.09, 0)]
        [InlineData(0.1, 0)]
        [InlineData(0.5, 0)]
        [InlineData(0.59, 0)]
        [InlineData(0.6, 1)]
        [InlineData(1.1, 1)]
        [InlineData(1.59, 1)]
        [InlineData(1.6, 2)]
        public void CalculateOneInchDiameterClass(double input, int expected)
        {
            CommonEquations.CalculateOneInchDiameterClass(input).Should().Be(expected);
        }


        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(0.09, 0)]
        [InlineData(0.1, 0)]
        [InlineData(0.5, 0)]
        [InlineData(0.59, 0)]
        [InlineData(0.6, 1)]
        [InlineData(1.1, 1)]
        [InlineData(1.5, 1)]
        [InlineData(1.59, 1)]
        [InlineData(1.6, 2)]
        public void CalculateOneInchDiameterClass_Old(double input, int expected)
        {
            var value = Math.Floor(input + 0.5);
            value.Should().Be(expected);
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(0.09, 0)]
        [InlineData(0.1, 0)]
        [InlineData(0.5, 0)]
        [InlineData(0.59, 0)]
        [InlineData(0.6, 1)]
        [InlineData(1.1, 1)]
        [InlineData(1.4, 1)]
        [InlineData(1.49, 1)]
        [InlineData(1.5, 1)]
        [InlineData(1.51, 1)]
        [InlineData(1.59, 1)]
        [InlineData(1.6, 2)]
        public void CalculateOneInchDiameterClass_Old2(double input, int expected)
        {
            var value = (int)(input + .49);
            value.Should().Be(expected);
        }


        [Theory]
        [InlineData(0, 0)]
        [InlineData(0.09, 0)]
        [InlineData(0.1, 0)]
        [InlineData(0.48, 0)]
        [InlineData(0.49, 0)]
        [InlineData(0.5, 1)]
        [InlineData(0.51, 1)]
        [InlineData(1.1, 1)]
        [InlineData(1.4, 1)]
        [InlineData(1.48, 1)]
        [InlineData(1.49, 1)]
        [InlineData(1.5, 2)]
        [InlineData(1.51, 2)]
        public void TestPoint51Rounding(double input, int expected)
        {
            var value = (int)Math.Floor(input + .51);
            value.Should().Be(expected);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0.09, 0)]
        [InlineData(0.1, 0)]
        [InlineData(0.48, 0)]
        [InlineData(0.49, 0)]
        [InlineData(0.5, 1)]
        [InlineData(0.51, 1)]
        [InlineData(1.1, 1)]
        [InlineData(1.4, 1)]
        [InlineData(1.48, 1)]
        [InlineData(1.49, 1)]
        [InlineData(1.5, 2)]
        [InlineData(1.51, 2)]
        public void TestAwayFromZeroRounding(double input, int expected)
        {
            var value = (int)Math.Round(input, MidpointRounding.AwayFromZero);
            value.Should().Be(expected);
        }
    }
}
