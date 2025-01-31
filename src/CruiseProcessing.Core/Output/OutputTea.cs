using CruiseProcessing.Data;
using CruiseProcessing.OutputModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

            var teaReport = new TeaReport()
            {
                SaleName = sale.Name,
                SaleNumber = sale.SaleNumber,
                Region = int.Parse(sale.Region),
                Forest = int.Parse(sale.Forest),
                District = int.Parse(sale.District),
            };

            var teaUnits = new List<TeaCuttingUnit>();
            // build tea units
            

            var cuttingUnits = DataLayer.getCuttingUnits();

            foreach (var unit in cuttingUnits)
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

                    foreach(var sg in sampleGroups)
                    {
                        var teaSg = new TeaSampleGroup()
                        {
                            SampleGroupCode = sg.Code,
                            Product = sg.PrimaryProduct,
                            UOM = sg.UOM ?? sale.DefaultUOM,
                        };

                        var sgVolumes = new List<TeaAppraisalVolume>();


                        var lcds = DataLayer.GetLcds(st.Code, sg.Code);

                        var lcdGroups = lcds.GroupBy(x => (x.Species, x.LiveDead));

                        // lcds grouped by species, livedead
                        foreach (var group in lcdGroups)
                        {
                            var sp = group.Key.Species;
                            var ld = group.Key.LiveDead;
                            var fia = DataLayer.GetFIACode(sp);

                            var appraisalGroup = new TeaAppraisalVolume()
                            {
                                SpeciesFia = fia.ToString(),
                                LiveDead = ld,
                            };

                            // accumilate totals by stm
                            foreach (var lcd in group)
                            {
                                var pro = DataLayer.GetPro(unit.Code, lcd.Stratum, lcd.SampleGroup, lcd.STM);
                                var proFactor = pro.ProrationFactor;

                                appraisalGroup.SumExpansionFactors += lcd.SumExpanFactor * proFactor;
                                appraisalGroup.EstNumberTrees += pro.ProratedEstimatedTrees; // this is already prorated and uses existing processing logic that uses ExpFactors or TalliedTrees based on cruise method
                                appraisalGroup.SumDbhOb += lcd.SumDBHOB * proFactor;
                                appraisalGroup.SumDbhObSqrd += lcd.SumDBHOBsqrd * proFactor; // DbhObSqrd wasn't in the original json schema but I noticed it was missing so added it
                                appraisalGroup.SumTotalHeight += lcd.SumTotHgt * proFactor;
                                appraisalGroup.SumMerchHeight += lcd.SumMerchHgtPrim * proFactor;
                                appraisalGroup.SumLogs += lcd.SumLogsMS * proFactor;
                                appraisalGroup.SumGrossBdFt += (lcd.SumGBDFT + lcd.SumGBDFTtop) * proFactor;
                                appraisalGroup.SumNetBdFt += (lcd.SumNBDFT + lcd.SumNBDFTtop) * proFactor;
                                appraisalGroup.SumGrossBdFtRemv += lcd.SumGBDFTremv * proFactor;
                                appraisalGroup.SumGrossCuFt += (lcd.SumGCUFT + lcd.SumGCUFTtop) * proFactor;
                                appraisalGroup.SumNetCuFt += (lcd.SumNCUFT + lcd.SumNCUFTtop) * proFactor;
                                appraisalGroup.SumGrossCuFtRemv += lcd.SumGCUFTremv * proFactor;
                                appraisalGroup.SumCords += (lcd.SumCords + lcd.SumCordsTop) * proFactor;
                                appraisalGroup.SumWeight += (lcd.SumWgtMSP + lcd.SumWgtMSS) * proFactor; // do we want the total weight of tree or just main and secondary?
                            }

                            sgVolumes.Add(appraisalGroup);
                        }

                        teaSg.AppraisalVolumes = sgVolumes;
                        teaSgs.Add(teaSg);
                    }
                }

                teaUnit.SampleGroups = teaSgs;
                teaUnits.Add(teaUnit);
            }
            teaReport.CuttingUnits = teaUnits;


            var reportText = System.Text.Json.JsonSerializer.Serialize(teaReport);

            strWriteOut.Write(reportText);

            return 0;
        }
    }
}
