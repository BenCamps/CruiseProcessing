using CruiseDAL.DataObjects;
using CruiseProcessing.Data;
using CruiseProcessing.OutputModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CruiseProcessing.Output
{
    public class Wt4ReportGenerator : OutputFileReportGeneratorBase, IReportGenerator
    {
        //  WT4 report
        private readonly string[] WT4columns = new string[3] {"                                           SAWTIMBER         NON-SAWTIMBER        NON-SAWTIMBER",
                                                    "  CUTTING                                  PRIM PROD = 01    OTHER PRIM PROD      SECOND PROD ONLY",
                                                    "   UNIT         ACRES       SPECIES        GREEN TONS        GREEN TONS           GREEN TONS"};

        //private List<string> prtFields;
        private ILogger Log { get; }

        private readonly IReadOnlyList<int> _fieldLengths;

        public Wt4ReportGenerator(CpDataLayer dataLayer, ILogger<Wt4ReportGenerator> logger)
            : base(dataLayer, "WT4")
        {
            Log = logger;
            _fieldLengths = new int[] { 3, 11, 13, 17, 18, 19, 5 };

            string currentTitle = fillReportTitle(currentReport);
            SetReportTitles(currentTitle, 5, 0, 0, reportConstants.BCUFS, "");
            reportTitles[2] = reportConstants.FCTO;
        }

        public int GenerateReport(TextWriter strWriteOut, HeaderFieldData headerData, int startPageNum)
        {
            HeaderData = headerData;
            var pageNumb = startPageNum;
            numOlines = 0;
            Log.LogInformation("Generating WT4 report");

            if (!CheckForCubicFootVolumeAndWeights(strWriteOut))
            {
                return startPageNum;
            }

            var cList = DataLayer.getCuttingUnits();
            processUnits(strWriteOut, cList, ref pageNumb);

            Log.LogInformation("WT4 report generation complete");

            return pageNumb;
        }

        private void processUnits(TextWriter strWriteOut, List<CuttingUnitDO> cList, ref int pageNumb)
        {
            //  WT4
            double grandTotalSaw = 0;
            double grandTotalNonsawPP = 0;
            double grandTotalNonsawSP = 0;
            List<PRODO> proList = DataLayer.getPRO();
            List<LCDDO> lcdList = DataLayer.getLCD();

            foreach (CuttingUnitDO cdo in cList)
            {
                int firstLine = 1;
                double totalUnitSaw = 0;
                double totalUnitNonsawPP = 0;
                double totalUnitNonsawSP = 0;

                cdo.Strata.Populate();
                //  get species groups from LCD
                var speciesGroups = DataLayer.GetLCDgroup("", 5, "C")
                    .Select(x=> x.Species)
                    .ToArray();
                foreach (var species in speciesGroups)
                {
                    double unitSaw = 0;
                    double unitNonsawPP = 0;
                    double unitNonsawSP = 0;

                    //  loop through stratum in the current unit
                    foreach (StratumDO st in cdo.Strata)
                    {
                        var currMeth = DataLayer.GetCruiseMethod(st.Code);

                        //  get group data
                        List<LCDDO> groupData = LCDmethods.GetCutOrLeave(lcdList, "C", species, st.Code, "");
                        foreach (LCDDO gd in groupData)
                        {
                            if (currMeth == "100" || gd.STM == "Y")
                            {
                                //pull all trees for current unit
                                List<TreeCalculatedValuesDO> currentGroup = new List<TreeCalculatedValuesDO>();
                                List<TreeCalculatedValuesDO> justUnitTrees = DataLayer.getTreeCalculatedValues((int)st.Stratum_CN, (int)cdo.CuttingUnit_CN);
                                if (gd.STM == "Y")
                                {
                                    //  pull sure-to-measure trees for current unit
                                    currentGroup = justUnitTrees.FindAll(
                                        delegate (TreeCalculatedValuesDO jut)
                                        {
                                            return jut.Tree.STM == "Y";
                                        });
                                }
                                else if (currMeth == "100")
                                {
                                    //  pull all trees for current unit
                                    currentGroup = justUnitTrees.FindAll(
                                        delegate (TreeCalculatedValuesDO jut)
                                        {
                                            return jut.Tree.Species == gd.Species;
                                        });
                                }   //  endif
                                //  sum up weights based on product
                                foreach (TreeCalculatedValuesDO cg in currentGroup)
                                {
                                    switch (cg.Tree.SampleGroup.PrimaryProduct)
                                    {
                                        case "01":
                                            unitSaw += cg.BiomassMainStemPrimary * cg.Tree.ExpansionFactor;
                                            break;

                                        default:
                                            unitNonsawPP = cg.BiomassMainStemPrimary * cg.Tree.ExpansionFactor;
                                            break;
                                    }   //  end switch on product
                                    unitNonsawSP = cg.BiomassMainStemSecondary * cg.Tree.ExpansionFactor;
                                }   //  end foreach loop
                            }
                            else
                            {
                                //  find proration factor for current group
                                List<PRODO> pList = PROmethods.GetMultipleData(proList, "C", gd.Stratum,
                                                                cdo.Code, gd.SampleGroup, "", "", gd.STM, 1);
                                double prorateFactor = (pList.Count == 1) ? (float)pList[0].ProrationFactor
                                    : 0.0;

                                //  Sum up weights by product
                                switch (gd.PrimaryProduct)
                                {
                                    case "01":
                                        unitSaw += gd.SumWgtMSP * prorateFactor;
                                        break;

                                    default:
                                        unitNonsawPP += gd.SumWgtMSP * prorateFactor;
                                        break;
                                } 
                                unitNonsawSP += gd.SumWgtMSS * prorateFactor;
                            }
                        }
                    }

                    if (unitSaw > 0 || unitNonsawPP > 0)
                        WriteCurrentGroupWT4(strWriteOut, ref pageNumb, ref firstLine, unitSaw, unitNonsawPP, unitNonsawSP, cdo.Area,
                                        cdo.Code, species);

                    //  Update subtotals
                    totalUnitSaw += unitSaw / 2000;
                    totalUnitNonsawPP += unitNonsawPP / 2000;
                    totalUnitNonsawSP += unitNonsawSP / 2000;
                    
                    
                }

                if (totalUnitSaw > 0 || totalUnitNonsawPP > 0 || totalUnitNonsawSP > 0)
                {
                    OutputTotalLine(strWriteOut, ref pageNumb, totalUnitSaw, totalUnitNonsawPP, totalUnitNonsawSP, 1);
                }

                //  update grand total
                grandTotalSaw += totalUnitSaw;
                grandTotalNonsawPP += totalUnitNonsawPP;
                grandTotalNonsawSP += totalUnitNonsawSP;

                
            }

            OutputTotalLine(strWriteOut, ref pageNumb, grandTotalSaw, grandTotalNonsawPP, grandTotalNonsawSP, 2);
        }

        private void WriteCurrentGroupWT4(TextWriter strWriteOut, ref int pageNumb, ref int firstLine, double unitSaw,
                                double unitNonsawPP, double unitNonsawSP, double unitAcres,
                                string unitCode, string currSP)
        {
            var prtFields = new List<string>();
            //  WT4 only
            WriteReportHeading(strWriteOut, reportTitles[0], reportTitles[1], reportTitles[2],
                                    WT4columns, 10, ref pageNumb, "");
            prtFields.Add("");
            if (firstLine == 1)
            {
                prtFields.Add(unitCode.PadLeft(3, ' '));
                prtFields.Add(Math.Round(unitAcres, 1, MidpointRounding.AwayFromZero).ToString().PadLeft(5, ' '));
                firstLine = 0;
            }
            else
            {
                prtFields.Add("   ");
                prtFields.Add("     ");
            }   //  endif it's the first line
            prtFields.Add(currSP.PadLeft(6, ' '));
            prtFields.Add(String.Format("{0,5:F2}", unitSaw / 2000).PadLeft(8, ' '));
            prtFields.Add(String.Format("{0,5:F2}", unitNonsawPP / 2000).PadLeft(8, ' '));
            prtFields.Add(String.Format("{0,5:F2}", unitNonsawSP / 2000).PadLeft(8, ' '));
            printOneRecord(_fieldLengths, prtFields, strWriteOut);
        } 

        private void OutputTotalLine(TextWriter strWriteOut, ref int pageNumb, double totalValue1, double totalValue2,
                            double totalValue3, int whichTotal)
        {
            //  WT4
            WriteReportHeading(strWriteOut, reportTitles[0], reportTitles[1], reportTitles[2],
                                    WT4columns, 10, ref pageNumb, "");
            strWriteOut.WriteLine("                                            __________________________________________________");
            if (whichTotal == 1)
            {
                strWriteOut.Write("        SUBTOTAL                            ");
                strWriteOut.Write("{0,8:F2}", totalValue1);
                strWriteOut.Write("          ");
                strWriteOut.Write("{0,8:F2}", totalValue2);
                strWriteOut.Write("             ");
                strWriteOut.WriteLine("{0,6:F2}", totalValue3);
                strWriteOut.WriteLine("");
                numOlines += 3;
            }
            else if (whichTotal == 2)
            {
                strWriteOut.Write("        GRAND TOTAL                     ");
                strWriteOut.Write("{0,12:F2}", totalValue1);
                strWriteOut.Write("         ");
                strWriteOut.Write("{0,9:F2}", totalValue2);
                strWriteOut.Write("          ");
                strWriteOut.WriteLine("{0,9:F2}", totalValue3);
            } 

        }
    }
}