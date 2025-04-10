using CruiseProcessing.Interop;
using CruiseProcessing.Processing;
using CruiseProcessing.Processing.Models;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CruiseProcessing.ProcessingNg.Test
{
    public class TreeVolumeCalculatorTest 
    {
        protected ITestOutputHelper Output { get; }

        public TreeVolumeCalculatorTest(ITestOutputHelper output) 
        {
            Output = output;
        }



        [Fact]
        public void CalculateTreeVolume()
        {
            var cruiseInfo = new NgCruiseInfo
            {
                CruiseID = Guid.Empty.ToString(),
                Region = 6,
                Forest = 7,
                District = 2,
            };

            var tree = new NgTreeInfo
            {
                TreeID = Guid.Empty.ToString(),
                TreeNumber = 1,
                CruiseMethod = "STR",
                DBH = 14,
                TotalHeight = 61,
                LiveDead = "L",
                FiaCode = "122",
                SpeciesCode = "122",
                PrimaryProduct = "01",
                TreeGrade = "0",
            };

            var utilizationValues = new NgUtilizationInfo
            {
                CalcCubic = true,
                CalcBoard = true,
                CalcBiomass = true,
                TopDibSaw = 6,
                TopDibNonSaw = 3,

            };

            var logs = new List<NgLogInfo>();



            var treeVolume = TreeVolumeCalculator.CalculateTreeVolume(cruiseInfo, tree, utilizationValues, logs);

            // assert that volume was calculated
            treeVolume.GrossCUFTPrimary.Should().BeGreaterThan(0);
        }
    }
}
