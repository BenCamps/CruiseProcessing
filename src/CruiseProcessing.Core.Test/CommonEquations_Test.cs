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
    }
}
