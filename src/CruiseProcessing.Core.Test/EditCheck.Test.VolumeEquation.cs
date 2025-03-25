using CruiseDAL;
using CruiseDAL.DataObjects;
using CruiseProcessing.Data;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CruiseProcessing.Test
{
    public partial class EditCheckTest
    {
        [Fact]
        public void ValidateVolumeEquations_PrimaryVolFlags_Passing()
        {
            var init = new DatabaseInitializer_V2();

            // initialize the a database
            // we just want the sale info setup
            var db = init.CreateDatabase();
            var dataLayer = GetCpDataLayer(db);


            var volumeEquations = new[]
            {
                new VolumeEquationDO
                {
                    VolumeEquationNumber = "1234567890",
                    Species = "abc",
                    PrimaryProduct = "00",
                    CalcBoard = 1,
                },
                new VolumeEquationDO
                {
                    VolumeEquationNumber = "1234567890",
                    Species = "abc",
                    PrimaryProduct = "00",
                    CalcCubic = 1,
                },
                new VolumeEquationDO
                {
                    VolumeEquationNumber = "1234567890",
                    Species = "abc",
                    PrimaryProduct = "00",
                    CalcCord = 1,
                },
            };
            foreach (var item in volumeEquations)
            {
                db.Insert(item);
            }

            var errors = new ErrorLogCollection();
            EditChecks.ValidateVolumeEqs(isVLL: false, dataLayer, errors);


            errors.Should().BeEmpty();
        }


        [Fact]
        public void ValidateVolumeEquations_PrimaryVolFlags_Failing()
        {
            var init = new DatabaseInitializer_V2();

            // initialize the a database
            // we just want the sale info setup
            var db = init.CreateDatabase();
            var dataLayer = GetCpDataLayer(db);


            var volumeEquations = new[]
            {
                new VolumeEquationDO
                {
                    VolumeEquationNumber = "1234567890",
                    Species = "abc",
                    PrimaryProduct = "00",
                    CalcBoard = 0,
                    CalcCubic = 0,
                    CalcCord = 0,
                },
            };
            foreach (var item in volumeEquations)
            {
                db.Insert(item);
            }

            var errors = new ErrorLogCollection();
            EditChecks.ValidateVolumeEqs(isVLL: false, dataLayer, errors);


            errors.Should().HaveCount(1);
            var error = errors.Single();
            Output.WriteLine("Error Message:" + error.Message);
        }

    }
}
