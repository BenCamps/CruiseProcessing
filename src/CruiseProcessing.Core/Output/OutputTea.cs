using CruiseDAL.Schema;
using CruiseDAL.V3.Models;
using CruiseProcessing.Data;
using CruiseProcessing.OutputModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CruiseProcessing.Output
{
    public class OutputTea : IReportGenerator
    {

        CpDataLayer DataLayer { get; set; }

        public OutputTea(CpDataLayer datalayer)
        {
            DataLayer = datalayer;
        }

        public int GenerateReport(TextWriter strWriteOut, HeaderFieldData headerData, int startPageNum)
        {
            // headerData and page number not needed

            // use datalayer to access data from cruise

            var sale = DataLayer.GetSale();

            string cruiseID = null;
            var v3dal = DataLayer.DAL_V3;
            if(v3dal != null)
            {
                cruiseID = DataLayer.CruiseID;
            }
            

            var teaReport = new TeaReport()
            {
                SaleName = sale.Name,
                SaleNumber = sale.SaleNumber,
                CruiseID = cruiseID,
                Region = sale.Region,
                Forest = sale.Forest,
                District = sale.District,
                Purpose = GetPurposeDomainValue(sale.Purpose),
            };

            
            

            var cuttingUnits = DataLayer.getCuttingUnits();
            var teaUnits = new List<TeaCuttingUnit>();
            foreach(var unit in cuttingUnits)
            {
                var teaUnit = new TeaCuttingUnit()
                {
                    CuttingUnitCode = unit.Code,
                    Area = unit.Area,
                    LoggingMethod = unit.LoggingMethod,
                };

                var teaSgs = new List<TeaSampleGroup>();
                // populate sample groups
                var strata = DataLayer.GetStrataByUnit(unit.Code);
                foreach (var st in strata)
                {
                    var sampleGroups = DataLayer.GetSampleGroups(st.Code);
                    
                    foreach (var sg in sampleGroups)
                    {
                        var teaSg = new TeaSampleGroup()
                        {
                            StratumCode = st.Code,
                            SampleGroupCode = sg.Code,
                            UOM = sg.UOM ?? sale.DefaultUOM,
                        };

                        var lcds = DataLayer.GetLcds(st.Code, sg.Code);
                        var lcdGroups = lcds.GroupBy(x => (x.Species, x.LiveDead, x.TreeGrade));

                        var teaSubpops = new List<TeaSubpopulation>();
                        // lcds grouped by species, livedead, tree grade
                        foreach (var group in lcdGroups)
                        {
                            var sp = group.Key.Species;
                            var ld = group.Key.LiveDead;
                            var treeGrade = group.Key.TreeGrade;
                            var fia = DataLayer.GetFIACode(sp);

                            var subPopulation = new TeaSubpopulation()
                            {
                                SpeciesFia = fia.ToString(),
                                LiveDead = ld,
                                TreeGrade = treeGrade,
                            };

                            // create containers for primary, secondary and biomass products if defined
                            // if secondary or biomass are not defined we will sum their respective values into
                            // one of the defined products
                            var products = new List<TeaProductVolume>();
                            TeaProductVolume primaryProduct = new TeaProductVolume { Product = sg.PrimaryProduct };
                            products.Add(primaryProduct);

                            TeaProductVolume secondaryProduct = null;
                            if (!string.IsNullOrEmpty(sg.SecondaryProduct) && primaryProduct.Product != sg.SecondaryProduct)
                            {
                                secondaryProduct = new TeaProductVolume { Product = sg.SecondaryProduct };
                                products.Add(secondaryProduct);
                            }
                            else
                            {
                                secondaryProduct = primaryProduct;
                            }

                            TeaProductVolume bioVolume = null;
                            if (!string.IsNullOrEmpty(sg.BiomassProduct) && sg.SecondaryProduct != sg.BiomassProduct)
                            {
                                bioVolume = new TeaProductVolume { Product = sg.BiomassProduct };
                                products.Add(bioVolume);
                            }
                            else
                            {
                                bioVolume = secondaryProduct;
                            }


                            // accumilate totals by stm
                            foreach (var lcd in group)
                            {
                                //Debug.Assert(lcd.SumHgtUpStem > 0
                                //    || lcd.SumMerchHgtPrim > 0
                                //    || lcd.SumMerchHgtSecond > 0
                                //    || lcd.SumTotHgt > 0);

                                var pro = DataLayer.GetPro(unit.Code, lcd.Stratum, lcd.SampleGroup, lcd.STM);
                                var proFactor = pro.ProrationFactor;

                                subPopulation.SumExpansionFactors += lcd.SumExpanFactor * proFactor;
                                subPopulation.EstNumberTrees += pro.ProratedEstimatedTrees; // this is already prorated and uses existing processing logic that uses ExpFactors or TalliedTrees based on cruise method
                                subPopulation.SumDbhOb += lcd.SumDBHOB * proFactor;
                                subPopulation.SumDbhObSqrd += lcd.SumDBHOBsqrd * proFactor; // DbhObSqrd wasn't in the original json schema but I noticed it was missing so added it
                                subPopulation.SumTotalHeight += lcd.SumTotHgt * proFactor;
                                subPopulation.SumMerchHeight += lcd.SumMerchHgtPrim * proFactor;
                                subPopulation.SumLogs += lcd.SumLogsMS * proFactor;
                                subPopulation.SumGrossCuFtRemv += lcd.SumGCUFTremv * proFactor;
                                subPopulation.SumGrossBdFtRemv += lcd.SumGBDFTremv * proFactor;

                                // accumilate primary and secondary products.
                                // if products are the same then primaryProduct and secondaryProduct
                                // reference the same object. 
                                primaryProduct.SumGrossBdFt += (lcd.SumGBDFT) * proFactor;
                                primaryProduct.SumNetBdFt += (lcd.SumNBDFT) * proFactor;

                                primaryProduct.SumGrossCuFt += (lcd.SumGCUFT) * proFactor;
                                primaryProduct.SumNetCuFt += (lcd.SumNCUFT) * proFactor;

                                primaryProduct.SumCords += (lcd.SumCords) * proFactor;
                                primaryProduct.SumWeight += (lcd.SumWgtMSP) * proFactor;

                                secondaryProduct.SumGrossBdFt += (lcd.SumGBDFTtop) * proFactor;
                                secondaryProduct.SumNetBdFt += (lcd.SumNBDFTtop) * proFactor;

                                secondaryProduct.SumGrossCuFt += (lcd.SumGCUFTtop) * proFactor;
                                secondaryProduct.SumNetCuFt += (lcd.SumNCUFTtop) * proFactor;

                                secondaryProduct.SumCords += (lcd.SumCordsTop + lcd.SumCordsRecv) * proFactor;
                                secondaryProduct.SumWeight += (lcd.SumWgtMSS) * proFactor;

                                bioVolume.SumWeight += (lcd.SumWgtBBD + lcd.SumWgtBBL + lcd.SumWgtBFT + lcd.SumWgtTip) * proFactor;

                                //foreach (var prod in products)
                                //{
                                //    if(prod.Product == "01")
                                //    {
                                //        prod.SumGrossBdFt += (lcd.SumGBDFT) * proFactor;
                                //        prod.SumNetBdFt += (lcd.SumNBDFT) * proFactor;

                                //        prod.SumGrossCuFt += (lcd.SumGCUFT) * proFactor;
                                //        prod.SumNetCuFt += (lcd.SumNCUFT) * proFactor;

                                //        prod.SumCords += (lcd.SumCords) * proFactor;
                                //        prod.SumWeight += (lcd.SumWgtMSP) * proFactor;
                                //    }
                                //    else
                                //    {
                                //        prod.SumGrossBdFt += (lcd.SumGBDFTtop) * proFactor;
                                //        prod.SumNetBdFt += (lcd.SumNBDFTtop) * proFactor;

                                //        prod.SumGrossCuFt += (lcd.SumGCUFTtop) * proFactor;
                                //        prod.SumNetCuFt += (lcd.SumNCUFTtop) * proFactor;

                                //        prod.SumCords += (lcd.SumCordsTop + lcd.SumCordsRecv) * proFactor;
                                //        prod.SumWeight += (lcd.SumWgtMSS) * proFactor;
                                //    }
                                //}

                            }
                            subPopulation.Products = products;

                            // assert that we are returning volumes
                            //Debug.Assert(primaryProduct.SumGrossCuFt > 0 || primaryProduct.SumGrossBdFt > 0 || primaryProduct.SumCords > 0);
                            //Debug.Assert(secondaryProduct.SumGrossCuFt > 0 || secondaryProduct.SumGrossBdFt > 0 || secondaryProduct.SumCords > 0);

                            teaSubpops.Add(subPopulation);
                        }

                        teaSg.SubPopulations = teaSubpops;
                        teaSgs.Add(teaSg);
                    }
                }

                teaUnit.SampleGroups = teaSgs;
                teaUnits.Add(teaUnit);
            }


            teaReport.CuttingUnits = teaUnits;

            var jsonOptions = new JsonSerializerOptions
            {
#if DEBUG
                WriteIndented = true,
#endif
            };
            var reportText = System.Text.Json.JsonSerializer.Serialize(teaReport, jsonOptions);

            strWriteOut.Write(reportText);

            return 0;
        }

        /// <summary>
        /// Maps old prupose values to modernization coded domain values
        /// </summary>
        /// <param name="purpose"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private string GetPurposeDomainValue(string purpose)
        {
            return purpose.ToLowerInvariant() switch
            {
                "timber sale" or "ts" => "1",
                "check cruise" or "check" => "2",
                "right of way" or "row" => "3",
                "recon" => "4",
                "other" => "5",
                _ => throw new ArgumentException("Invalid Sale Purpose Code," + purpose)
            };
        }
    }
}
