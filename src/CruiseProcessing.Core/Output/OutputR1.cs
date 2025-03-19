

using CruiseDAL.DataObjects;
using CruiseProcessing.Data;
using CruiseProcessing.Output;
using CruiseProcessing.OutputModels;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CruiseProcessing
{
    public class OutputR1 : OutputFileReportGeneratorBase
    {
        //  Region 1 reports
        //  R101 report
        private readonly string[] R101columns = new string[10] {"  C                      ******************************************** PRIMARY PRODUCT *********************************************",
                                                      "  O",
                                                      "  N                        AVGDEF",
                                                      "  T     S       P                       GROSS      NET",
                                                      "  R     P       R   U     %      %                         ************************** CONTRACT SPECIES  ***************************",
                                                      "        E       O                       BDFT       BDFT                                                             16'LOGS 16'LOGS",
                                                      "  S     C       D   O     B      C",
                                                      "  P     I       U   F     D      U      CUFT       CUFT       AVG       ** GROSS VOLUME **     **  NET VOLUME **     GROSS   GROSS",
                                                      "  E     E       C         F      F",
                                                      "  C     S       T   M     T      T      RATIO      RATIO      DBH         BDFT       CUFT       BDFT       CUFT       CCF     MBF"};

        //  R104 report
        private readonly string[] R104columns = new string[2] {"           CUTTING               SAMPLE    PRIMARY     CUT/      AVG      BASAL",

                                                     " STRATA    UNIT        SPECIES   GROUP     PRODUCT     LEAVE     DBH      AREA"};
        private readonly string[] R105sectionOne = new string[7] {"                    *************** SAWTIMBER **************   ************* NON-SAWTIMBER ****************",
                                                        "                             (PROD = 01   UM = 01, 03)            (PROD NOT = 01   UM = 01, 02, 03)",
                                                        "        A                                                     (AND ALL SECONDARY & RECOVERED PRODUCT VOLUMES)",
                                                        "  U     C      ",
                                                        "  N     R            ** GROSS VOLUME **    ** NET VOLUME **      ***   GROSS    ***   ***   NET    ***",
                                                        "  I     E    ",
                                                        "  T     S            BDFT       CUFT       BDFT       CUFT          BDFT      CUFT       BDFT      CUFT  "};
        private readonly string[] R105sectionTwo = new string[5] {" ",
                                                        " S U B T O T A L S    **************SAWTIMBER**************     ************NON-SAWTIMBER**************",
                                                        " ",
                                                        "                    ** GROSS VOLUME **     ** NET VOLUME **        GROSS     GROSS      NET       NET",
                                                        "          SPECIES     BDFT       CUFT       BDFT       CUFT        BDFT      CUFT       BDFT      CUFT "};
        private readonly string[] R105sectionThree = new string[5]{" ",
                                                           "T O T A L S",
                                                           "                      GROSS         NET             ************** VOLUME  ***************",
                                                           "           PRODUCT    BF/CF         BF/CF           ***** GROSS *****     *****  NET *****",
                                                           "  PRODUCT  SOURCE     RATIO         RATIO           BDFT         CUFT     BDFT        CUFT "};


        private int[] fieldLengths;
        private List<string> prtFields = new List<string>();
        private List<RegionalReports> listToOutput = new List<RegionalReports>();
        private List<RegionalReports> listTotalOutput = new List<RegionalReports>();
        private List<ReportSubtotal> firstSubtotal = new List<ReportSubtotal>();
        private List<ReportSubtotal> secondSubtotal = new List<ReportSubtotal>();
        private List<ReportSubtotal> thirdSubtotal = new List<ReportSubtotal>();
        private List<ReportSubtotal> totalToOutput = new List<ReportSubtotal>();
        private string[] completeHeader;


        public OutputR1(CpDataLayer dataLayer, HeaderFieldData headerData, string reportID) : base(dataLayer, headerData, reportID)
        {
        }

        public void CreateR1reports(TextWriter strWriteOut, ref int pageNum)
        {
            //  fill report title array
            string currentTitle = fillReportTitle(currentReport);
            List<LCDDO> lcdList = DataLayer.getLCD();

            //  is there any data for the report
            switch (currentReport)
            {
                case "R101":
                    if (!lcdList.Any(l => l.SumGBDFT > 0))
                    {
                        noDataForReport(strWriteOut, currentReport, " >>>> No board foot volume for report");
                        return;
                    }   //  endif on board foot
                    if (!lcdList.Any(l => l.SumGCUFT > 0))
                    {
                        noDataForReport(strWriteOut, currentReport, " >>>> No cubic foot volume for report");
                        return;
                    }   //  endif on cubic foot
                    break;



                case "R104":
                    if (lcdList.Count == 0)
                    {
                        noDataForReport(strWriteOut, currentReport, " >>>> No data for report");
                        return;
                    }   //  endif on no data
                    break;

                case "R105":
                    if (lcdList.Count == 0)
                    {
                        noDataForReport(strWriteOut, currentReport, ">>>>> No data for report");
                        return;
                    }   //  end if no data
                    break;
            }   //  end switch on report

            //  process base on report
            switch (currentReport)
            {
                case "R101":
                    numOlines = 0;
                    fieldLengths = new int[] { 2, 6, 8, 4, 4, 7, 8, 11, 12, 6, 12, 11, 11, 14, 10, 5 };
                    SetReportTitles(currentTitle, 6, 0, 0, reportConstants.FCTO, "");
                    //  pull groups from LCD
                    List<LCDDO> speciesList = DataLayer.getLCDOrdered("WHERE CutLeave = @p1 ", "GROUP BY UOM,PrimaryProduct,Species,ContractSpecies", "C", "");
                    createR101(speciesList, strWriteOut, ref pageNum, lcdList);
                    break;

                case "R104":
                    numOlines = 0;
                    fieldLengths = new[] { 3, 8, 12, 12, 11, 11, 8, 9, 5 };
                    SetReportTitles(currentTitle, 6, 0, 0, reportConstants.FCLT, "");
                    //  This report will be by stratum and cutting unit so get all stratum first
                    List<StratumDO> orderedStrata = DataLayer.GetStrata();
                    createR104(orderedStrata, strWriteOut, ref pageNum);
                    break;

                case "R105":
                    numOlines = 0;
                    // section one -- volume summary by ubit
                    fieldLengths = new[] { 4, 7, 10, 10, 11, 10, 11, 11, 9, 11 };
                    SetReportTitles(currentTitle, 6, 0, 0, reportConstants.FCTO, " ");
                    List<CuttingUnitDO> cList = DataLayer.getCuttingUnits();
                    List<LCDDO> ldList = DataLayer.getLCD();
                    List<TreeCalculatedValuesDO> tcvList = DataLayer.getTreeCalculatedValues();
                    AccumulateValuesAndPrint(strWriteOut, cList, ldList);
                    WriteSectionOne(strWriteOut, ref pageNum);

                    //  section two -- subtitaks by species only.
                    fieldLengths = new[] { 10, 7, 10, 12, 11, 11, 11, 11, 11, 11 };
                    //                    List<LCDDO> lList = bslyr.getLCD();
                    AccumulateSubtotalAndPrint(strWriteOut, cList, ldList);
                    WriteSectionTwo(strWriteOut);

                    //section three -- Totals by product
                    //AccumulateTotalsAndPrint(strWriteOut,ldList,tcvList);
                    WriteSectionThree(strWriteOut);

                    break;
            }   //  end switch
            return;
        }   //  end CreateR1reports

        private void createR101(List<LCDDO> speciesList, TextWriter strWriteOut, ref int pageNum,
                                List<LCDDO> lcdList)
        {
            //  generates R101 report
            string currCS = "*";
            string currPP = "*";
            foreach (LCDDO sl in speciesList)
            {
                if (currPP == "*")
                    currPP = sl.PrimaryProduct;
                if (currCS == "*")
                    currCS = sl.ContractSpecies;
                if (currPP != sl.PrimaryProduct)
                {
                    //  Output product subtotal
                    outputTotal(1, currPP, firstSubtotal, strWriteOut, ref pageNum);
                    firstSubtotal.Clear();
                    currPP = sl.PrimaryProduct;
                }
                if (currCS != sl.ContractSpecies)
                {
                    //  update overall total
                    updateTotal();
                    //  Output contract species total
                    outputTotal(2, currCS, secondSubtotal, strWriteOut, ref pageNum);
                    secondSubtotal.Clear();
                    currCS = sl.ContractSpecies;
                }   //  endif

                AccumulateVolumes(lcdList, sl.ContractSpecies, sl.Species, sl.PrimaryProduct);
                //  Update product subtotal
                updateSubtotal(firstSubtotal);
                //  update contract species subtotal
                updateSubtotal(secondSubtotal);
                WriteCurrentGroup(strWriteOut, ref pageNum);
                listToOutput.Clear();
            }   //  end foreach loop
            //  output last product and contract species total
            outputTotal(1, currPP, firstSubtotal, strWriteOut, ref pageNum);
            outputTotal(2, currCS, secondSubtotal, strWriteOut, ref pageNum);
            //  update overall total
            updateTotal();
            //  output overall contract species total
            outputTotal(3, "", totalToOutput, strWriteOut, ref pageNum);
            return;
        }   //  end createR101

        private void createR104(List<StratumDO> orderedStrata, TextWriter strWriteOut, ref int pageNum)
        {
            //  then process by stratum and cutting unit
            foreach (StratumDO os in orderedStrata)
            {
                os.CuttingUnits.Populate();
                ReportSubtotal r1 = new ReportSubtotal();
                r1.Value1 = "C";
                totalToOutput.Add(r1);
                ReportSubtotal r2 = new ReportSubtotal();
                r2.Value1 = "L";
                totalToOutput.Add(r2);
                foreach (CuttingUnitDO cu in os.CuttingUnits)
                {
                    // set two lines in totalToOutput for cut and leave
                    //  process cut trees first
                    AccumulateBasalArea(os.Code, cu.Code, cu.Area, "C");
                    WriteCurrentUnit(strWriteOut, ref pageNum);
                    //  update and output cut/leave subtotal
                    if (listToOutput.Count > 0)
                    {
                        updateUnitOrStrata(3, "C");
                        outputCutLeaveSubtotal("C", strWriteOut, ref pageNum);
                        firstSubtotal.Clear();
                        //                    }   //  endif
                        //  update unit subtotal
                        updateUnitOrStrata(1, "C");
                        //  update strata total
                        updateUnitOrStrata(2, "C");
                    }
                    else if (listToOutput.Count == 0)
                    {
                        StringBuilder msg = new StringBuilder();
                        msg.Append("CUTTING UNIT ");
                        msg.Append(cu.Code);
                        msg.Append(" has no cut tree data and is not included in this report. ");
                        msg.Append("Report is not complete.");
                        strWriteOut.WriteLine(msg);
                    }   //  endif

                    //  process leave trees
                    listToOutput.Clear();
                    AccumulateBasalArea(os.Code, cu.Code, cu.Area, "L");
                    if (listToOutput.Count > 0)
                    {
                        updateUnitOrStrata(3, "L");
                        outputCutLeaveSubtotal("L", strWriteOut, ref pageNum);
                        firstSubtotal.Clear();
                    }   //  endif
                    //  update and output unit subtotal
                    updateUnitOrStrata(1, "L");
                    //  update strata total
                    updateUnitOrStrata(2, "L");
                    outputUnitOrStrata(strWriteOut, ref pageNum, cu.Code, 1, secondSubtotal);
                    secondSubtotal.Clear();
                    listToOutput.Clear();
                }   //  end foreach loop on cutting unit
                //  output strata total
                outputUnitOrStrata(strWriteOut, ref pageNum, os.Code, 2, totalToOutput);
                totalToOutput.Clear();
            }   //  end foreach loop on stratum
            return;
        }   //  end createR104

        private void AccumulateVolumes(List<LCDDO> lcdList, string currCS, string currSP, string currPP)
        {
            //  R101
            double currGB = 0;
            double currNB = 0;
            double currGC = 0;
            double currNC = 0;
            string currMeth = "";
            string currUOM = "";
            double currLOGS = 0;
            double currDBH = 0;
            double currEF = 0;
            List<StratumDO> sList = DataLayer.GetStrata();
            //  pull current group from LCD
            List<LCDDO> justCurrentGroup = lcdList.FindAll(
                delegate (LCDDO l)
                {
                    return l.ContractSpecies == currCS && l.Species == currSP && l.PrimaryProduct == currPP;
                });

            //  going to need proration factor for sample group and unit on each LCD record
            List<PRODO> proList = DataLayer.getPRO();
            foreach (LCDDO jcg in justCurrentGroup)
            {
                currUOM = jcg.UOM;
                //  get all units for stratum and sample group
                List<PRODO> justUnits = proList.FindAll(
                    delegate (PRODO p)
                    {
                        return p.CutLeave == "C" && p.Stratum == jcg.Stratum && p.SampleGroup == jcg.SampleGroup;
                    });
                foreach (PRODO ju in justUnits)
                {
                    //  sum DBH
                    currDBH += jcg.SumDBHOB * ju.ProrationFactor;
                    //  sum board foot
                    currGB += jcg.SumGBDFT * ju.ProrationFactor;
                    currNB += jcg.SumNBDFT * ju.ProrationFactor;
                    //  sum cubic foot
                    currGC += jcg.SumGCUFT * ju.ProrationFactor;
                    currNC += jcg.SumNCUFT * ju.ProrationFactor;
                    //  total logs
                    currLOGS += jcg.SumLogsMS * ju.ProrationFactor;
                    //  total EF
                    //  need method to get appropriate EF total
                    int nthRow = sList.FindIndex(
                        delegate (StratumDO s)
                        {
                            return s.Code == jcg.Stratum;
                        });
                    if (nthRow >= 0)
                        currMeth = sList[nthRow].Method;
                    switch (currMeth)
                    {
                        case "S3P":
                        case "3P":
                            if (jcg.STM == "Y")
                                currEF += jcg.SumExpanFactor;
                            else if (jcg.STM == "N")
                                currEF += jcg.TalliedTrees;
                            break;

                        default:
                            currEF += jcg.SumExpanFactor * ju.ProrationFactor;
                            break;
                    }   //  end switch
                }   //  end foreach loop
            }   //  end foreach loop

            //  load into output list
            RegionalReports r = new RegionalReports();
            if (currCS == null)
                r.value1 = " ";
            else r.value1 = currCS;
            r.value2 = currSP;
            r.value3 = currPP;
            r.value4 = currUOM;
            r.value7 = currDBH;
            r.value8 = currGB;
            r.value9 = currGC;
            r.value10 = currNB;
            r.value11 = currNC;
            r.value12 = currLOGS;
            r.value13 = currEF;
            listToOutput.Add(r);

            return;
        }   //  end AccumulateVolumes

       

        private void AccumulateBasalArea(string currST, string currCU, float currAC, string currCL)
        {
            //  R104
            List<TreeDO> tList = DataLayer.getTrees();
            List<PRODO> proList = DataLayer.getPRO();

            //  get groups from LCD
            List<LCDDO> justGroups = DataLayer.GetLCDdata("WHERE Stratum = @p1 GROUP BY SampleGroup,Species,PrimaryProduct,CutLeave", currST);
            foreach (LCDDO jg in justGroups)
            {
                //  pull tree data for each cutting unit and group for average DBH
                //  then need all trees in the strata for the species/SG group for summed BA
                List<TreeDO> justTrees = tList.FindAll(
                      delegate (TreeDO t)
                      {
                          return t.Stratum.Code == currST && t.CuttingUnit.Code == currCU &&
                              t.Species == jg.Species && t.SampleGroup.Code == jg.SampleGroup &&
                              t.SampleGroup.PrimaryProduct == jg.PrimaryProduct &&
                              t.SampleGroup.CutLeave == currCL;
                      });
                List<TreeDO> allTrees = tList.FindAll(
                    delegate (TreeDO t)
                    {
                        return t.Stratum.Code == currST && t.Species == jg.Species &&
                            t.SampleGroup.Code == jg.SampleGroup &&
                            t.SampleGroup.PrimaryProduct == jg.PrimaryProduct &&
                            t.SampleGroup.CutLeave == currCL;
                    });
                //  are there trees in this strata and cutting unit to process?
                if (justTrees.Count > 0)
                {
                    //  sum up dbh and ef for average dbh
                    double currDBH = justTrees.Sum(j => j.DBH * j.ExpansionFactor);
                    //  calculate and sum basal area
                    double currBA = allTrees.Sum(j => (0.005454 * Math.Pow(j.DBH, 2.0)) * j.ExpansionFactor);
                    //  also need sum of EF
                    double currEF = justTrees.Sum(j => j.ExpansionFactor);

                    //  what's the proration factor for this group?
                    int nthRow = proList.FindIndex(
                        delegate (PRODO p)
                        {
                            return p.Stratum == currST && p.CuttingUnit == currCU &&
                                        p.SampleGroup == jg.SampleGroup &&
                                        p.PrimaryProduct == jg.PrimaryProduct &&
                                        p.CutLeave == jg.CutLeave;
                        });
                    //  store this group for printing
                    RegionalReports r = new RegionalReports();
                    r.value1 = currST;
                    r.value2 = currCU;
                    r.value3 = jg.Species;
                    r.value4 = jg.SampleGroup;
                    r.value5 = jg.PrimaryProduct;
                    r.value6 = currCL;
                    r.value7 = currAC;
                    if (nthRow >= 0)
                        r.value8 = proList[nthRow].ProrationFactor;
                    else r.value8 = 1.0;
                    r.value9 = currBA;
                    r.value10 = currDBH;
                    r.value11 = currEF;
                    listToOutput.Add(r);
                }   //  endif trees in this strata and cutting unit
            }   //  end foreach loop on groups

            return;
        }   //  end AccumulateBasalArea

        private void WriteCurrentGroup(TextWriter strWriteOut, ref int pageNum)
        {
            //  works for R101
            double calcValue = 0;
            foreach (RegionalReports lto in listToOutput)
            {
                WriteReportHeading(strWriteOut, reportTitles[0], reportTitles[1], reportTitles[2],
                                    R101columns, 13, ref pageNum, "");
                prtFields.Clear();
                prtFields.Add("");
                prtFields.Add(lto.value1.PadRight(4, ' '));
                prtFields.Add(lto.value2.PadRight(6, ' '));
                prtFields.Add(lto.value3.PadLeft(2, '0'));
                prtFields.Add(lto.value4.PadLeft(2, '0'));
                //  average defect
                if (lto.value8 > 0.0)
                    calcValue = ((lto.value8 - lto.value10) / lto.value8) * 100;
                else calcValue = 0.0;
                prtFields.Add(String.Format("{0,4:F0}", calcValue).PadLeft(4, ' '));
                if (lto.value9 > 0)
                    calcValue = ((lto.value9 - lto.value11) / lto.value9) * 100;
                else calcValue = 0.0;
                prtFields.Add(String.Format("{0,4:F0}", calcValue).PadLeft(4, ' '));
                //  Gross and net ratios
                if (lto.value9 > 0)
                    calcValue = lto.value8 / lto.value9;
                else calcValue = 0.0;
                prtFields.Add(String.Format("{0,7:F4}", calcValue).PadLeft(7, ' '));
                if (lto.value11 > 0)
                    calcValue = lto.value10 / lto.value11;
                prtFields.Add(String.Format("{0,7:F4}", calcValue).PadLeft(7, ' '));
                //  Average DBH
                if (lto.value13 > 0)
                    calcValue = lto.value7 / lto.value13;
                else calcValue = 0.0;
                prtFields.Add(String.Format("{0,5:F1}", calcValue).PadLeft(5, ' '));
                //  gross volume
                prtFields.Add(String.Format("{0,10:F0}", lto.value8).PadLeft(10, ' '));
                prtFields.Add(String.Format("{0,10:F0}", lto.value9).PadLeft(10, ' '));
                //  net volume
                prtFields.Add(String.Format("{0,10:F0}", lto.value10).PadLeft(10, ' '));
                prtFields.Add(String.Format("{0,10:F0}", lto.value11).PadLeft(10, ' '));
                //  16 foot log volume
                if (lto.value9 > 0)
                    calcValue = lto.value12 / (lto.value9 / 100);
                else calcValue = 0.0;
                prtFields.Add(String.Format("{0,5:F1}", calcValue).PadLeft(5, ' '));
                if (lto.value8 > 0)
                    calcValue = lto.value12 / (lto.value8 / 1000);
                prtFields.Add(String.Format("{0,5:F1}", calcValue).PadLeft(5, ' '));
                printOneRecord(fieldLengths, prtFields, strWriteOut);
            }   //  end foreach loop
            return;
        }   //  end WriteCurrentGroup

        

        private void WriteCurrentUnit(TextWriter strWriteOut, ref int pageNum)
        {
            //  R104
            double calcValue = 0;
            foreach (RegionalReports lto in listToOutput)
            {
                WriteReportHeading(strWriteOut, reportTitles[0], reportTitles[1], reportTitles[2],
                                    R104columns, 10, ref pageNum, "");
                prtFields.Clear();
                prtFields.Add("");
                prtFields.Add(lto.value1.PadLeft(2, ' '));
                prtFields.Add(lto.value2.PadLeft(3, ' '));
                prtFields.Add(lto.value3.PadLeft(6, ' '));
                prtFields.Add(lto.value4.PadLeft(2, ' '));
                prtFields.Add(lto.value5.PadLeft(2, ' '));
                prtFields.Add(lto.value6);
                //  average DBH
                if (lto.value11 > 0)
                    calcValue = lto.value10 / lto.value11;
                else calcValue = 0;
                prtFields.Add(String.Format("{0,4:F1}", calcValue).PadLeft(4, ' '));
                //  basal area
                if (lto.value7 > 0)
                    calcValue = (lto.value9 * lto.value8) / lto.value7;
                else calcValue = 0.0;
                prtFields.Add(String.Format("{0,5:F1}", calcValue).PadLeft(5, ' '));

                printOneRecord(fieldLengths, prtFields, strWriteOut);
            }   //  end foreach loop
            return;
        }   //  end WriteCurrentUnit

        private void updateSubtotal(List<ReportSubtotal> totalToUpdate)
        {
            //  R101
            if (totalToUpdate.Count > 0)
            {
                foreach (RegionalReports lto in listToOutput)
                {
                    totalToUpdate[0].Value7 += lto.value7;
                    totalToUpdate[0].Value8 += lto.value8;
                    totalToUpdate[0].Value9 += lto.value9;
                    totalToUpdate[0].Value10 += lto.value10;
                    totalToUpdate[0].Value11 += lto.value11;
                    totalToUpdate[0].Value12 += lto.value12;
                    totalToUpdate[0].Value13 += lto.value13;
                }   //  end foreach loop
            }
            else if (totalToUpdate.Count == 0)
            {
                foreach (RegionalReports lto in listToOutput)
                {
                    ReportSubtotal r = new ReportSubtotal();
                    r.Value7 = lto.value7;
                    r.Value8 = lto.value8;
                    r.Value9 = lto.value9;
                    r.Value10 = lto.value10;
                    r.Value11 = lto.value11;
                    r.Value12 = lto.value12;
                    r.Value13 = lto.value13;
                    totalToUpdate.Add(r);
                }   //  end foreach loop
            }   //  endif
            return;
        }   //  end updateSubtotal

        private void updateTotal()
        {
            //  R101
            if (totalToOutput.Count > 0)
            {
                totalToOutput[0].Value7 += secondSubtotal[0].Value7;
                totalToOutput[0].Value8 += secondSubtotal[0].Value8;
                totalToOutput[0].Value9 += secondSubtotal[0].Value9;
                totalToOutput[0].Value10 += secondSubtotal[0].Value10;
                totalToOutput[0].Value11 += secondSubtotal[0].Value11;
                totalToOutput[0].Value12 += secondSubtotal[0].Value12;
                totalToOutput[0].Value13 += secondSubtotal[0].Value13;
            }
            else if (totalToOutput.Count == 0)
            {
                ReportSubtotal r = new ReportSubtotal();
                r.Value7 = secondSubtotal[0].Value7;
                r.Value8 = secondSubtotal[0].Value8;
                r.Value9 = secondSubtotal[0].Value9;
                r.Value10 = secondSubtotal[0].Value10;
                r.Value11 = secondSubtotal[0].Value11;
                r.Value12 = secondSubtotal[0].Value12;
                r.Value13 = secondSubtotal[0].Value13;
                totalToOutput.Add(r);
            }   //  endif
            return;
        }   //  end updateTotal



        private void updateUnitOrStrata(int UnitOrStrata, string cutLeave)
        {
            //  R104
            switch (UnitOrStrata)
            {
                case 1:         //  update unit subtotal
                    if (secondSubtotal.Count > 0)
                    {
                        secondSubtotal[0].Value7 += listToOutput.Sum(l => l.value7);
                        secondSubtotal[0].Value8 += listToOutput.Sum(l => l.value8);
                        secondSubtotal[0].Value9 += listToOutput.Sum(l => l.value9);
                        secondSubtotal[0].Value10 += listToOutput.Sum(l => l.value10);
                        secondSubtotal[0].Value11 += listToOutput.Sum(l => l.value11);
                    }
                    else
                    {
                        ReportSubtotal r = new ReportSubtotal();
                        r.Value7 = listToOutput.Sum(l => l.value7);
                        r.Value8 = listToOutput.Sum(l => l.value8);
                        r.Value9 = listToOutput.Sum(l => l.value9);
                        r.Value10 = listToOutput.Sum(l => l.value10);
                        r.Value11 = listToOutput.Sum(l => l.value11);
                        secondSubtotal.Add(r);
                    }   //  endif
                    break;

                case 2:         //  update strata subtotal
                    if (totalToOutput.Count > 0)
                    {
                        if (totalToOutput[0].Value1 == cutLeave)
                        {
                            totalToOutput[0].Value7 += listToOutput[0].value7;
                            totalToOutput[0].Value9 += listToOutput.Sum(l => (l.value9 * l.value8));
                            totalToOutput[0].Value10 += listToOutput.Sum(l => l.value10);
                            totalToOutput[0].Value11 += listToOutput.Sum(l => l.value11);
                        }
                        else if (totalToOutput[1].Value1 == cutLeave)
                        {
                            if (listToOutput.Count > 0)
                            {
                                totalToOutput[1].Value7 += listToOutput[0].value7;
                                totalToOutput[1].Value9 += listToOutput.Sum(l => (l.value9 * l.value8));
                                totalToOutput[1].Value10 += listToOutput.Sum(l => l.value10);
                                totalToOutput[1].Value11 += listToOutput.Sum(l => l.value11);
                            }   //  endif
                        }   //  endif
                    }   //  endif
                    break;

                case 3:         //  update cut or leave subtotal
                    if (firstSubtotal.Count > 0)
                    {
                        firstSubtotal[0].Value7 += listToOutput.Sum(l => l.value7);
                        firstSubtotal[0].Value8 += listToOutput.Sum(l => l.value8);
                        firstSubtotal[0].Value9 += listToOutput.Sum(l => l.value9);
                        firstSubtotal[0].Value10 += listToOutput.Sum(l => l.value10);
                        firstSubtotal[0].Value11 += listToOutput.Sum(l => l.value11);
                    }
                    else
                    {
                        ReportSubtotal r = new ReportSubtotal();
                        r.Value7 = listToOutput.Sum(l => l.value7);
                        r.Value8 = listToOutput.Sum(l => l.value8);
                        r.Value9 = listToOutput.Sum(l => l.value9);
                        r.Value10 = listToOutput.Sum(l => l.value10);
                        r.Value11 = listToOutput.Sum(l => l.value11);
                        firstSubtotal.Add(r);
                    }   //  endif
                    break;
            }   //  end switch
            return;
        }   //  end updateUnitOrStrata

        private void outputTotal(int lineType, string currTotal, List<ReportSubtotal> totalsLine,
                                TextWriter strWriteOut, ref int pageNum)
        {
            //  writes subtotal line for any subtotal in R101
            double calcValue = 0;
            WriteReportHeading(strWriteOut, reportTitles[0], reportTitles[1], reportTitles[2],
                                    R101columns, 13, ref pageNum, "");
            switch (lineType)
            {
                case 1:         //  product subtotal

                    strWriteOut.WriteLine("                         __________________________________________________________________________________________________________");
                    strWriteOut.Write("        PRODUCT ");
                    strWriteOut.Write(currTotal.PadLeft(2, ' '));
                    strWriteOut.Write(" TOTAL");
                    break;

                case 2:         //  contract species subtotal
                    strWriteOut.WriteLine("                         __________________________________________________________________________________________________________");
                    strWriteOut.Write("   CONTR SPEC ");
                    strWriteOut.Write(currTotal.PadLeft(4, ' '));
                    strWriteOut.Write(" TOTAL");
                    break;

                case 3:         //  overall contract species total
                    strWriteOut.WriteLine(reportConstants.longLine);
                    strWriteOut.WriteLine(reportConstants.longLine);
                    strWriteOut.Write("CONTRACT SPECIES TOTALS ");
                    break;
            }   //  end switch on line type

            //  rest of fields are the same for all three total lines
            //  average defect
            if (totalsLine[0].Value8 > 0.0)
                calcValue = ((totalsLine[0].Value8 - totalsLine[0].Value10) / totalsLine[0].Value8) * 100;
            else calcValue = 0.0;
            strWriteOut.Write(String.Format("{0,4:F0}", calcValue).PadLeft(4, ' '));
            if (totalsLine[0].Value9 > 0.0)
                calcValue = ((totalsLine[0].Value9 - totalsLine[0].Value11) / totalsLine[0].Value9) * 100;
            else calcValue = 0.0;
            strWriteOut.Write(String.Format("{0,4:F0}", calcValue).PadLeft(7, ' '));
            //  Gross and net ratios
            if (totalsLine[0].Value9 > 0.0)
                calcValue = totalsLine[0].Value8 / totalsLine[0].Value9;
            else calcValue = 0.0;
            strWriteOut.Write(String.Format("{0,7:F4}", calcValue).PadLeft(11, ' '));
            if (totalsLine[0].Value11 > 0.0)
                calcValue = totalsLine[0].Value10 / totalsLine[0].Value11;
            else calcValue = 0.0;
            strWriteOut.Write(String.Format("{0,7:F4}", calcValue).PadLeft(11));
            //  Average DBH
            if (totalsLine[0].Value13 > 0)
                calcValue = totalsLine[0].Value7 / totalsLine[0].Value13;
            else calcValue = 0.0;
            strWriteOut.Write(String.Format("{0,5:F1}", calcValue).PadLeft(10, ' '));
            //  gross volume
            strWriteOut.Write(String.Format("{0,10:F0}", totalsLine[0].Value8).PadLeft(11, ' '));
            strWriteOut.Write(String.Format("{0,10:F0}", totalsLine[0].Value9).PadLeft(12, ' '));
            //  net volume
            strWriteOut.Write(String.Format("{0,10:F0}", totalsLine[0].Value10).PadLeft(11, ' '));
            strWriteOut.Write(String.Format("{0,10:F0}", totalsLine[0].Value11).PadLeft(11, ' '));
            //  16 foot log volume
            if (totalsLine[0].Value9 > 0.0)
                calcValue = totalsLine[0].Value12 / (totalsLine[0].Value9 / 100);
            else calcValue = 0.0;
            strWriteOut.Write(String.Format("{0,5:F1}", calcValue).PadLeft(9, ' '));
            if (totalsLine[0].Value8 > 0.0)
                calcValue = totalsLine[0].Value12 / (totalsLine[0].Value8 / 1000);
            strWriteOut.WriteLine(String.Format("{0,5:F1}", calcValue).PadLeft(10, ' '));
            strWriteOut.WriteLine("");
            return;
        }   //  end outputTotal



        private void AccumulateValuesAndPrint(TextWriter strWriteOut,
                                        List<CuttingUnitDO> cList,
                                        List<LCDDO> lcdList)
        {
            // R105
            List<PRODO> pList = DataLayer.getPRO();
            foreach (CuttingUnitDO cd in cList)
            {
                cd.Strata.Populate();
                string currCU = cd.Code;
                //  find  proration factors for each cutting unit
                int nthRow = pList.FindIndex(
                    delegate (PRODO pl)
                    {
                        return pl.CuttingUnit == currCU;
                    });

                //Accumulate value for each cutting unit within each stratum
                foreach (StratumDO stratum in cd.Strata)
                {
                    //  find all stratum in LCD for sawtimber
                    List<LCDDO> currSTR = lcdList.FindAll(
                        delegate (LCDDO ld)
                        {
                            return ld.Stratum == stratum.Code &&
                                   ld.PrimaryProduct == "01" &&
                                    ld.CutLeave == "C";
                        });

                    foreach (LCDDO cs in currSTR)
                    {
                        //  Is unit already in the list?
                        //  Accumulate  prorated values for current unit
                        int kRow = listToOutput.FindIndex(
                            delegate (RegionalReports lto)
                            {
                                return lto.value1 == currCU;
                            });

                        if (kRow >= 0)
                        {
                            listToOutput[kRow].value9 += cs.SumGBDFT * pList[nthRow].ProrationFactor;
                            listToOutput[kRow].value10 += cs.SumGCUFT * pList[nthRow].ProrationFactor;
                            listToOutput[kRow].value11 += cs.SumNBDFT * pList[nthRow].ProrationFactor;
                            listToOutput[kRow].value12 += cs.SumNCUFT * pList[nthRow].ProrationFactor;
                        }
                        else if (kRow < 0)
                        {
                            RegionalReports rr = new RegionalReports();
                            rr.value1 = cd.Code;
                            rr.value8 = cd.Area;
                            rr.value9 += cs.SumGBDFT * pList[nthRow].ProrationFactor;
                            rr.value10 += cs.SumGCUFT * pList[nthRow].ProrationFactor;
                            rr.value11 += cs.SumNBDFT * pList[nthRow].ProrationFactor;
                            rr.value12 += cs.SumNCUFT * pList[nthRow].ProrationFactor;
                            listToOutput.Add(rr);
                        }   //  ednif
                    }  //  end foreach
                }   //  end for j loop
                //  now need everything that's not sawtimber
                foreach (StratumDO stratum in cd.Strata)
                {
                    //  find all stratum in LCD for non-sawtimber
                    List<LCDDO> currSTR = lcdList.FindAll(
                        delegate (LCDDO ld)
                        {
                            return ld.Stratum == stratum.Code &&
                                   ld.PrimaryProduct != "01" &&
                                    ld.CutLeave == "C";
                        });

                    foreach (LCDDO cs in currSTR)
                    {
                        //  Is unit already in the list?
                        //  Accumulate  prorated values for current unit
                        int kRow = listToOutput.FindIndex(
                            delegate (RegionalReports lto)
                            {
                                return lto.value1 == currCU;
                            });

                        if (kRow >= 0)
                        {
                            listToOutput[kRow].value13 += cs.SumGBDFT * pList[nthRow].ProrationFactor;
                            listToOutput[kRow].value14 += cs.SumGCUFT * pList[nthRow].ProrationFactor;
                            listToOutput[kRow].value15 += cs.SumNBDFT * pList[nthRow].ProrationFactor;
                            listToOutput[kRow].value16 += cs.SumNCUFT * pList[nthRow].ProrationFactor;
                            listToOutput[kRow].value13 += cs.SumGBDFTtop * pList[nthRow].ProrationFactor;
                            listToOutput[kRow].value14 += cs.SumGCUFTtop * pList[nthRow].ProrationFactor;
                            listToOutput[kRow].value15 += cs.SumNBDFTtop * pList[nthRow].ProrationFactor;
                            listToOutput[kRow].value16 += cs.SumNCUFTtop * pList[nthRow].ProrationFactor;
                        }
                        else if (kRow < 0)
                        {
                            RegionalReports rr = new RegionalReports();
                            rr.value1 = cd.Code;
                            rr.value8 = cd.Area;
                            rr.value13 += cs.SumGBDFT * pList[nthRow].ProrationFactor;
                            rr.value14 += cs.SumGCUFT * pList[nthRow].ProrationFactor;
                            rr.value15 += cs.SumNBDFT * pList[nthRow].ProrationFactor;
                            rr.value16 += cs.SumNCUFT * pList[nthRow].ProrationFactor;
                            rr.value13 += cs.SumGBDFTtop * pList[nthRow].ProrationFactor;
                            rr.value14 += cs.SumGCUFTtop * pList[nthRow].ProrationFactor;
                            rr.value15 += cs.SumNBDFTtop * pList[nthRow].ProrationFactor;
                            rr.value16 += cs.SumNCUFTtop * pList[nthRow].ProrationFactor;
                            listToOutput.Add(rr);
                        }   //  ednif
                    }  //  end foreach
                }  //  end for j loop
            }   //  end foreach

            return;
        }   //end AccumulateValueAndPRint

        private void AccumulateSubtotalAndPrint(TextWriter strWriteOut, List<CuttingUnitDO> cList,
                                                    List<LCDDO> lList)
        {
            //  R105
            listToOutput.Clear();
            //  Get unique species from LCD
            string[,] justSpecies = DataLayer.GetUniqueSpeciesProduct();
            for (int k = 0; k < justSpecies.GetLength(0); k++)
            {
                List<LCDDO> currentUnit = new List<LCDDO>();
                if (justSpecies[k, 0] != null && justSpecies[k, 1] == "01")
                {
                    //  find all species in lcd list
                    currentUnit = lList.FindAll(
                        delegate (LCDDO t)
                        {
                            return t.Species == justSpecies[k, 0] &&
                                t.PrimaryProduct == "01" &&
                                t.CutLeave == "C";
                        });
                }   //endif
                RegionalReports rr = new RegionalReports();
                RegionalReports rrt = new RegionalReports();
                if (currentUnit.Count != 0)
                {
                    foreach (LCDDO cu in currentUnit)
                    {
                        //  What method is the stratum on this tree?
                        //  And what is the strata acres for plot based methods
                        List<StratumDO> sList = DataLayer.GetStrata();
                        int mthRow = sList.FindIndex(
                            delegate (StratumDO s)
                            {
                                return s.Code == cu.Stratum;
                            });
                        double strAcres = Utilities.ReturnCorrectAcres(cu.Stratum, DataLayer,
                                    (long)sList[mthRow].Stratum_CN);

                        //  is species already in the list?
                        int kRow = listToOutput.FindIndex(
                            delegate (RegionalReports lto)
                            {
                                return lto.value1 == justSpecies[k, 0];
                            });
                        if (kRow >= 0)
                        {
                            listToOutput[kRow].value9 += cu.SumGBDFT * strAcres;
                            listToOutput[kRow].value10 += cu.SumGCUFT * strAcres;
                            listToOutput[kRow].value11 += cu.SumNBDFT * strAcres;
                            listToOutput[kRow].value12 += cu.SumNCUFT * strAcres;
                            listToOutput[kRow].value13 += cu.SumGBDFTtop * strAcres;
                            listToOutput[kRow].value14 += cu.SumGCUFTtop * strAcres;
                            listToOutput[kRow].value15 += cu.SumNBDFTtop * strAcres;
                            listToOutput[kRow].value16 += cu.SumNCUFTtop * strAcres;
                        }
                        else if (kRow < 0)
                        {
                            rr.value1 = justSpecies[k, 0];
                            rr.value9 += cu.SumGBDFT * strAcres;
                            rr.value10 += cu.SumGCUFT * strAcres;
                            rr.value11 += cu.SumNBDFT * strAcres;
                            rr.value12 += cu.SumNCUFT * strAcres;
                            rr.value13 += cu.SumGBDFTtop * strAcres;
                            rr.value14 += cu.SumGCUFTtop * strAcres;
                            rr.value15 += cu.SumNBDFTtop * strAcres;
                            rr.value16 += cu.SumNCUFTtop * strAcres;
                            listToOutput.Add(rr);
                        }   //  endif
                            // add logic to total the volumes
                        int nthRow = listTotalOutput.FindIndex(
                        delegate (RegionalReports lto)
                        {
                            return lto.value1 == cu.PrimaryProduct;
                        });
                        if (nthRow >= 0)
                        {
                            listTotalOutput[nthRow].value9 += cu.SumGBDFT * strAcres;
                            listTotalOutput[nthRow].value10 += cu.SumGCUFT * strAcres;
                            listTotalOutput[nthRow].value11 += cu.SumNBDFT * strAcres;
                            listTotalOutput[nthRow].value12 += cu.SumNCUFT * strAcres;
                            //  also add any secondaary volume
                            listTotalOutput[nthRow].value13 += cu.SumGBDFTtop * strAcres;
                            listTotalOutput[nthRow].value14 += cu.SumGCUFTtop * strAcres;
                            listTotalOutput[nthRow].value15 += cu.SumNBDFTtop * strAcres;
                            listTotalOutput[nthRow].value16 += cu.SumNCUFTtop * strAcres;
                        }
                        else if (nthRow < 0)
                        {
                            rrt.value1 = cu.PrimaryProduct;
                            rrt.value2 = "P";
                            rrt.value3 = cu.SecondaryProduct;
                            rrt.value4 = "S";
                            rrt.value9 = cu.SumGBDFT * strAcres;
                            rrt.value10 = cu.SumGCUFT * strAcres;
                            rrt.value11 = cu.SumNBDFT * strAcres;
                            rrt.value12 = cu.SumNCUFT * strAcres;
                            rrt.value13 = cu.SumGBDFTtop * strAcres;
                            rrt.value14 = cu.SumGCUFTtop * strAcres;
                            rrt.value15 = cu.SumNBDFTtop * strAcres;
                            rrt.value16 = cu.SumNCUFTtop * strAcres;
                            listTotalOutput.Add(rrt);
                        }   //  endif
                    }   //  end foreach loop
                }   //  endif no records
            }   //  end for k loop

            //  accumulate non-sawtimber
            for (int k = 0; k < justSpecies.GetLength(0); k++)
            {
                List<LCDDO> currentUnit = new List<LCDDO>();
                if (justSpecies[k, 0] != null && justSpecies[k, 1] != "01")
                {
                    //  find all species in lcd list
                    currentUnit = lList.FindAll(
                        delegate (LCDDO t)
                        {
                            return t.Species == justSpecies[k, 0] &&
                                t.PrimaryProduct != "01" &&
                                t.CutLeave == "C";
                        });
                }   //endif
                RegionalReports rr = new RegionalReports();
                RegionalReports rrt = new RegionalReports();
                if (currentUnit.Count != 0)
                {
                    foreach (LCDDO cu in currentUnit)
                    {
                        //  What method is the stratum on this tree?
                        //  And what is the strata acres for plot based methods
                        List<StratumDO> sList = DataLayer.GetStrata();
                        int mthRow = sList.FindIndex(
                            delegate (StratumDO s)
                            {
                                return s.Code == cu.Stratum;
                            });
                        double strAcres = Utilities.ReturnCorrectAcres(cu.Stratum, DataLayer,
                                    (long)sList[mthRow].Stratum_CN);
                        //  is species already in the list?
                        int kRow = listToOutput.FindIndex(
                            delegate (RegionalReports lto)
                            {
                                return lto.value1 == justSpecies[k, 0];
                            });
                        if (kRow >= 0)
                        {
                            listToOutput[kRow].value13 += cu.SumGBDFT * strAcres;
                            listToOutput[kRow].value14 += cu.SumGCUFT * strAcres;
                            listToOutput[kRow].value15 += cu.SumNBDFT * strAcres;
                            listToOutput[kRow].value16 += cu.SumNCUFT * strAcres;
                        }
                        else if (kRow < 0)
                        {
                            rr.value1 = justSpecies[k, 0];
                            rr.value13 += cu.SumGBDFT * strAcres;
                            rr.value14 += cu.SumGCUFT * strAcres;
                            rr.value15 += cu.SumNBDFT * strAcres;
                            rr.value16 += cu.SumNCUFT * strAcres;
                            listToOutput.Add(rr);
                        }   //  enduf
                        int nthRow = listTotalOutput.FindIndex(
                        delegate (RegionalReports lto)
                        {
                            return lto.value1 == cu.PrimaryProduct;
                        });
                        if (nthRow >= 0)
                        {
                            listTotalOutput[nthRow].value9 += cu.SumGBDFT * strAcres;
                            listTotalOutput[nthRow].value10 += cu.SumGCUFT * strAcres;
                            listTotalOutput[nthRow].value11 += cu.SumNBDFT * strAcres;
                            listTotalOutput[nthRow].value12 += cu.SumNCUFT * strAcres;
                            //  also add any secondaary volume
                            listTotalOutput[nthRow].value13 += cu.SumGBDFTtop * strAcres;
                            listTotalOutput[nthRow].value14 += cu.SumGCUFTtop * strAcres;
                            listTotalOutput[nthRow].value15 += cu.SumNBDFTtop * strAcres;
                            listTotalOutput[nthRow].value16 += cu.SumNCUFTtop * strAcres;
                        }
                        else if (nthRow < 0)
                        {
                            rrt.value1 = cu.PrimaryProduct;
                            rrt.value2 = "P";
                            rrt.value3 = cu.SecondaryProduct;
                            rrt.value4 = "S";
                            rrt.value9 = cu.SumGBDFT * strAcres;
                            rrt.value10 = cu.SumGCUFT * strAcres;
                            rrt.value11 = cu.SumNBDFT * strAcres;
                            rrt.value12 = cu.SumNCUFT * strAcres;
                            rrt.value13 = cu.SumGBDFTtop * strAcres;
                            rrt.value14 = cu.SumGCUFTtop * strAcres;
                            rrt.value15 = cu.SumNBDFTtop * strAcres;
                            rrt.value16 = cu.SumNCUFTtop * strAcres;
                            listTotalOutput.Add(rrt);
                        }   //  endif
                    }   //  end foreach loop
                }  //  endif no records
            }   //  end for k loop
            return;
        }   // end  AccuulateSubtotals

        private void AccumulateTotalsAndPrint(TextWriter strWriteOut, List<LCDDO> lcdList,
                                              List<TreeCalculatedValuesDO> tcvList)
        {
            listToOutput.Clear();
            //List<LCDDO> productValues = LCDmethods.GetCutGroupedBy("","",9,bslyr);

            foreach (LCDDO pv in lcdList)
            {
                //  Is the product in the list to output
                int nthRow = listToOutput.FindIndex(lto => lto.value1 == pv.PrimaryProduct);
                if (nthRow >= 0)
                {
                    listToOutput[nthRow].value7 += pv.SumGBDFT; ;
                    listToOutput[nthRow].value8 += pv.SumGCUFT;
                    listToOutput[nthRow].value9 += pv.SumNBDFT;
                    listToOutput[nthRow].value10 += pv.SumNCUFT;
                    //  also add any secondaary volume
                    listToOutput[nthRow].value11 += pv.SumGBDFTtop;
                    listToOutput[nthRow].value12 += pv.SumGCUFTtop;
                    listToOutput[nthRow].value13 += pv.SumNBDFTtop;
                    listToOutput[nthRow].value14 += pv.SumNCUFTtop;
                }
                else if (nthRow < 0)
                {
                    RegionalReports rr = new RegionalReports();
                    rr.value1 = pv.PrimaryProduct;
                    rr.value2 = "P";
                    rr.value3 = pv.SecondaryProduct;
                    rr.value4 = "S";
                    rr.value7 = pv.SumGBDFT;
                    rr.value8 = pv.SumGCUFT;
                    rr.value9 = pv.SumNBDFT;
                    rr.value10 = pv.SumNCUFT;
                    rr.value11 = pv.SumGBDFTtop;
                    rr.value12 = pv.SumGCUFTtop;
                    rr.value13 = pv.SumNBDFTtop;
                    rr.value14 = pv.SumNCUFTtop;
                    listToOutput.Add(rr);
                }
            }       //  end foreaach
        }   // end Accumulate Totals

        private void WriteSectionOne(TextWriter strWriteOut, ref int pagenumber)
        {
            //  R105
            WriteReportHeading(strWriteOut, reportTitles[0], reportTitles[1],
                            reportTitles[2], R105sectionOne, 7, ref pagenumber, "");
            strWriteOut.WriteLine("_________________________________________________________________________________________________________");
            prtFields.Clear();
            foreach (RegionalReports lto in listToOutput)
            {
                prtFields.Add(lto.value1.PadRight(4, ' '));
                prtFields.Add(String.Format("{0,7:F0}", lto.value8).PadLeft(7, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value9).PadLeft(16, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value10).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value11).PadLeft(12, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value12).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value13).PadLeft(13, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value14).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value15).PadLeft(10, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value16).PadLeft(11, ' '));
                printOneRecord(fieldLengths, prtFields, strWriteOut);
                prtFields.Clear();
            }   //  end foreach loop
            strWriteOut.WriteLine("                  ______________________________________________________________________________________");

            return;
        }   //  end WriteSectionOne

        private void WriteSectionTwo(TextWriter strWriteOut)
        {
            //  R105
            //  write section two headings only
            for (int j = 0; j < 5; j++)
            {
                strWriteOut.WriteLine(R105sectionTwo[j]);
                numOlines++;
            }   //  end for j loop
            strWriteOut.WriteLine("_________________________________________________________________________________________________________");
            prtFields.Clear();
            foreach (RegionalReports lto in listToOutput)
            {
                prtFields.Add("");
                prtFields.Add(lto.value1);
                prtFields.Add(String.Format("{0,8:F0}", lto.value9).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value10).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value11).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value12).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value13).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value14).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value15).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value16).PadLeft(11, ' '));
                printOneRecord(fieldLengths, prtFields, strWriteOut);
                prtFields.Clear();
            }   //  end foreach loop
            strWriteOut.WriteLine("                  ______________________________________________________________________________________");
            listToOutput.Clear();
            return;
        }   //  end WRite Section two

        private void WriteSectionThree(TextWriter strWriteOut)
        {
            //  R105
            double calcValue = 0;
            double GBDFTtotal = 0;
            double GCUFTtotal = 0;
            double NBDFTtotal = 0;
            double NCUFTtotal = 0;
            //            WriteReportHeading(strWriteOut, rh.reportTitles[0], rh.reportTitles[1],
            //                           rh.reportTitles[2], rRH.R105sectionThree, 4, ref pagenumber, "");
            //  write section two headings only
            for (int j = 0; j < 5; j++)
            {
                strWriteOut.WriteLine(R105sectionThree[j]);
                numOlines++;
            }   //  end for j loop
            strWriteOut.WriteLine("_________________________________________________________________________________________________________");
            prtFields.Clear();
            foreach (RegionalReports lto in listTotalOutput)
            {
                fieldLengths = new[] { 5, 2, 8, 15, 11, 13, 11, 11, 10 };
                prtFields.Add("");
                //  Primary product
                prtFields.Add(lto.value1.PadRight(6, ' '));
                prtFields.Add(lto.value2.PadRight(3, ' '));
                //  ratio
                if (lto.value10 > 0)
                {
                    calcValue = lto.value9 / lto.value10;
                    prtFields.Add(String.Format("{0,8:F4}", calcValue));
                }
                else prtFields.Add("        ");
                if (lto.value12 > 0)
                {
                    calcValue = lto.value11 / lto.value12;
                    prtFields.Add(String.Format("{0,8:F4}", calcValue));
                }
                else prtFields.Add("        ");
                prtFields.Add(String.Format("{0,8:F0}", lto.value9).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value10).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value11).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value12).PadLeft(11, ' '));
                printOneRecord(fieldLengths, prtFields, strWriteOut);
                prtFields.Clear();
                GBDFTtotal += lto.value9;
                GCUFTtotal += lto.value10;
                NBDFTtotal += lto.value11;
                NCUFTtotal += lto.value12;

                //  Secondary product
                fieldLengths = new[] { 5, 6, 34, 13, 9, 11, 8 };
                prtFields.Add(" ");
                prtFields.Add(lto.value3.PadRight(4, ' '));
                prtFields.Add(lto.value4);
                prtFields.Add(String.Format("{0,8:F0}", lto.value13).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value14).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value15).PadLeft(11, ' '));
                prtFields.Add(String.Format("{0,8:F0}", lto.value16).PadLeft(11, ' '));
                printOneRecord(fieldLengths, prtFields, strWriteOut);
                prtFields.Clear();
                GBDFTtotal += lto.value13;
                GCUFTtotal += lto.value14;
                NBDFTtotal += lto.value15;
                NCUFTtotal += lto.value16;
            }   //  end foreach loop
            strWriteOut.WriteLine("                  ______________________________________________________________________________________");

            //  write overall totals
            //  ratio
            strWriteOut.Write("                   ");
            if (GCUFTtotal > 0)
            {
                calcValue = GBDFTtotal / GCUFTtotal;
                strWriteOut.Write(String.Format("{0,8:F4}", calcValue).PadRight(15, ' '));
            }
            else strWriteOut.Write("        ");
            if (NCUFTtotal > 0)
            {
                calcValue = NBDFTtotal / NCUFTtotal;
                strWriteOut.Write(String.Format("{0,8:F4}", calcValue).PadRight(14, ' '));
            }
            else strWriteOut.Write("        ");
            strWriteOut.Write(String.Format("{0,8:F0}", GBDFTtotal).PadRight(13, ' '));
            strWriteOut.Write(String.Format("{0,8:F0}", GCUFTtotal).PadRight(11, ' '));
            strWriteOut.Write(String.Format("{0,8:F0}", NBDFTtotal).PadRight(11, ' '));
            strWriteOut.WriteLine(String.Format("{0,8:F0}", NCUFTtotal).PadRight(11, ' '));

            return;
        }   //  end Write Section Three

        private void outputCutLeaveSubtotal(string currCL, TextWriter strWriteOut, ref int pageNum)
        {
            //  R104
            double calcValue = 0;
            WriteReportHeading(strWriteOut, reportTitles[0], reportTitles[1], reportTitles[2],
                                R104columns, 10, ref pageNum, "");
            strWriteOut.WriteLine("            _________________________________________________________________________________________________________________________");
            strWriteOut.Write("  SUBTOTAL                                               ");
            strWriteOut.Write(currCL);
            //  average DBH
            if (firstSubtotal[0].Value11 > 0)
                calcValue = firstSubtotal[0].Value10 / firstSubtotal[0].Value11;
            else calcValue = 0.0;
            strWriteOut.Write(String.Format("{0,4:F1}", calcValue).PadLeft(11, ' '));
            //  basal area
            if (firstSubtotal[0].Value7 > 0)
                calcValue = (firstSubtotal[0].Value8 * firstSubtotal[0].Value9) / firstSubtotal[0].Value7;
            else calcValue = 0.0;
            strWriteOut.WriteLine(String.Format("{0,5:F1}", calcValue).PadLeft(10, ' '));
            return;
        }   //  end outputCutLeaveSubtotal

        private void outputUnitOrStrata(TextWriter strWriteOut, ref int pageNum, string currCode,
                                        int whichSubtotal, List<ReportSubtotal> subtotalList)
        {
            //  R104
            double calcValue = 0;
            WriteReportHeading(strWriteOut, reportTitles[0], reportTitles[1], reportTitles[2],
                                R104columns, 10, ref pageNum, "");
            strWriteOut.WriteLine("            _________________________________________________________________________________________________________________________");

            switch (whichSubtotal)
            {
                case 1:         //  unit subtotal
                    strWriteOut.Write("   UNIT    ");
                    strWriteOut.Write(currCode.PadLeft(3, ' '));
                    strWriteOut.Write("  SUBTOTAL                                 ");
                    break;

                case 2:         //  strata subtotal
                    strWriteOut.Write("   STRATA  ");
                    strWriteOut.Write(currCode.PadLeft(3, ' '));
                    strWriteOut.Write("  SUBTOTAL                                 ");
                    strWriteOut.Write(subtotalList[0].Value1);
                    break;
            }   //  end switch
            foreach (ReportSubtotal sl in subtotalList)
            {
                //  output average DBH
                if (whichSubtotal == 1)
                {
                    if (sl.Value11 > 0)
                        calcValue = sl.Value10 / sl.Value11;
                    else calcValue = 0.0;
                    strWriteOut.Write(String.Format("{0,4:F1}", calcValue).PadLeft(12, ' '));
                    //  basal area
                    if (sl.Value7 > 0)
                        calcValue = (sl.Value8 * sl.Value9) / sl.Value7;
                    else calcValue = 0.0;
                    strWriteOut.WriteLine(String.Format("{0,5:F1}", calcValue).PadLeft(10, ' '));
                    strWriteOut.WriteLine("");
                }
                else if (whichSubtotal == 2)
                {
                    if (sl.Value1 == "C")
                        strWriteOut.Write("            ");
                    else if (sl.Value1 == "L")
                    {
                        strWriteOut.Write("                                                         ");
                        strWriteOut.Write(sl.Value1);
                        strWriteOut.Write("            ");
                    }   //  endif
                    //  basal area
                    if (sl.Value7 > 0)
                        calcValue = sl.Value9 / sl.Value7;
                    else calcValue = 0.0;
                    strWriteOut.WriteLine(String.Format("{0,5:F1}", calcValue).PadLeft(9, ' '));
                }   //  endif whichSubtotal
            }   //  end foreach loop
            strWriteOut.WriteLine(reportConstants.longLine);
            return;
        }   //  end outputUnitOrStrata


    }
}