﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using CruiseDAL.DataObjects;
using CruiseDAL.Schema;

namespace CruiseProcessing
{
    public class OutputR2 : CreateTextFile
    {
        #region
        public string currentReport;
        private int[] fieldLengths;
        private ArrayList prtFields = new ArrayList();
        List<RegionalReports> listToOutput = new List<RegionalReports>();
        List<ReportSubtotal> subtotalList = new List<ReportSubtotal>();
        List<ReportSubtotal> totalToOutput = new List<ReportSubtotal>();
        private regionalReportHeaders rRH = new regionalReportHeaders();
        private List<StandTables> defectData = new List<StandTables>();
        private string[] columnHeader = new string[] { "" };
        private double weightFraction = 0;
        #endregion

        public void CreateR2Reports(StreamWriter strWriteOut, ref int pageNumb, reportHeaders rh)
        {
            //  fill report title array
            string currentTitle = fillReportTitle(currentReport);

            //  grab LCD list to see if there is data for the current report
            IEnumerable<LCDDO> lcdList = Global.BL.getLCD();
            switch (currentReport)
            {
                case "R201":        case "R206":
                    if (lcdList.Sum(l => l.SumNCUFT) == 0)
                    {
                        noDataForReport(strWriteOut, currentReport, " >>>>> No cubic foot volume for this report");
                        return;
                    }   //  endif
                    break;
                case "R207":        case "R202":        case "R203":
                case "R204":        case "R205":
                    if (lcdList.Sum(l => l.SumExpanFactor) == 0)
                    {
                        noDataForReport(strWriteOut, currentReport, " >>>>> No data for this report");
                        return;
                    }   //  endif
                    break;
            }   //  end switch on current report

            //  process data for each report requested
            switch (currentReport)
            {
                case "R201":
                    //  process by stratum for this report
                    numOlines = 0;
                    rh.createReportTitle(currentTitle, 6, 0, 0, "BY SPECIES AND STRATA", reportConstants.FCTO);
                    fieldLengths = new int[] { 3, 7, 12, 9, 9, 14, 11, 10, 13, 11, 15, 7 };
                    foreach (StratumDO s in Global.BL.getStratum())
                    {
                        //  need stratum acres
                        double STRacres = Utilities.AcresLookup((long)s.Stratum_CN, s.Code);
                        //  Accumulate by species, primary and secondary product
                        AccumulateStratum(s.Code, STRacres, lcdList, s.Method);
                        WriteCurrentGroup(strWriteOut, ref pageNumb, rh);
                        //  update subtotal and print
                        updateSubtotal(s.Code, STRacres, "");
                        outputSubtotal(strWriteOut, ref pageNumb, rh);
                        subtotalList.Clear();
                        //  update total
                        updateTotal(STRacres);
                        listToOutput.Clear();
                    }   //  end foreach loop on stratum
                    //  output total line
                    outputTotal(strWriteOut, ref pageNumb, rh);
                    break;
                case "R202":        case "R203":
                case "R204":        case "R205":
                    //  reports for defect -- either 1 or 2 inch diameter class; board foot or cubic foot
                    numOlines = 0;
                    StringBuilder secondLine = new StringBuilder();
                    List<LCDDO> justSpecies = Global.BL.getLCDOrdered("WHERE CutLeave = ? ", 
                                                            "GROUP BY Species", "C", "").ToList();
                    //  setup main header based on report
                    rh.createReportTitle(currentTitle, 4, 31, 32, reportConstants.FCTO_PPO,"");
                    //  create column headers for the species pulled
                    LoadColumnHeader(justSpecies);
                    //  accumulate defect by diameter class, volume type and species
                    switch (currentReport)
                    {
                        case "R202":
                            AccumulateDefect(1,"BDFT",justSpecies);
                            break;
                        case "R203":
                            AccumulateDefect(1,"CUFT",justSpecies);
                            break;
                        case "R204":
                            AccumulateDefect(2,"BDFT",justSpecies);
                            break;
                        case "R205":
                            AccumulateDefect(2,"CUFT",justSpecies);
                            break;
                    }   //  end switch on report
                    //  Write report
                    WriteDefectGroups(strWriteOut, ref pageNumb, rh, justSpecies.Count);
                    break;
                case "R206":
                    //  process by contract species in LCD
                    numOlines = 0;
                    rh.createReportTitle(currentTitle, 6, 0, 0, reportConstants.FPPO, reportConstants.FCTO);
                    fieldLengths = new int[] { 3, 10, 19, 13, 14, 16, 5 };
                    foreach (LCDDO jc in Global.BL.getLCDOrdered("WHERE CutLeave = ? GROUP BY ", "ContractSpecies", "C", ""))
                    {
                        //  find all species for current contract species
                        AccumulateByContractSpecies(Global.BL.getLCDOrdered("WHERE ContractSpecies = ? GROUP BY ", "Species", jc.ContractSpecies, ""), lcdList);
                        WriteSpeciesGroups(strWriteOut, ref pageNumb, rh);
                        updateSubtotal("", 0, "");
                        outputCSsubtotal(strWriteOut, ref pageNumb, rh);
                        listToOutput.Clear();
                        subtotalList.Clear();
                    }   //  end foreach loop
                    break;
                case "R207":
                    //  process by cutting unit for this report
                    numOlines = 0;
                    rh.createReportTitle(currentTitle, 6, 0, 0, "BY CUTTING UNIT BY STRATUM", reportConstants.FCTO);
                    fieldLengths = new int[] { 2, 11, 9, 8, 7, 13, 9, 11, 10, 5 };
                    List<TreeDO> tList = Global.BL.getTrees().ToList();
                    foreach(CuttingUnitDO c in Global.BL.getCuttingUnits())
                    {
                        int somethingToPrint = AccumulateGroups(c,tList);
                        if(somethingToPrint == 1) WriteUnitGroup(strWriteOut,ref pageNumb,rh);
                    }   //  end foreach loop on cutList
                    break;
                case "R208":
                    //  stewardship report
                    numOlines = 0;
                    rh.createReportTitle(currentTitle, 6, 0, 0, reportConstants.FCTO, "");
                    fieldLengths = new int[] { 11, 13, 13, 14, 16, 14, 12, 8 };
                    CreateR208(strWriteOut, ref pageNumb, rh);
                    break;
            }   //  end switch
            return;
        }   //  end CreateR2Reports


        private void CreateR208(StreamWriter strWriteOut, ref int pageNumb, reportHeaders rh)
        {
            List<StewProductCosts> stewList = new List<StewProductCosts>();
            int numRows = 0;
            //  first need to determine if StewProductCost exists in the file or is empty
            bool bResult = Global.BL.doesTableExist("StewProductCosts");
            if (bResult)
            {
                //  table exists so try to get data
                stewList = Global.BL.getStewCosts().ToList();
                numRows = stewList.Count;
            }
            else if(!bResult)
            {
                Global.BL.CreateNewTable("StewProductCosts");
                numRows = 0;
            }   //  endif
            if (numRows == 0)
            {
                StewardshipProductCosts spc = new StewardshipProductCosts();
                spc.stewList = stewList;
                spc.setupDialog();
                spc.ShowDialog();
                stewList = spc.stewList;
            }   //  endif

            //  find all species groups to include in the report
            List<StewProductCosts> justGroups = stewList.FindAll(
                delegate(StewProductCosts s)
                {
                    return s.includeInReport == "True";
                });
            if (justGroups.Count == 0)
            {
                MessageBox.Show("No species groups selected to include in the report.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (justGroups.Count > 0)
            {
                //  process through the groups
                foreach (StewProductCosts jg in justGroups)
                {
                    //  pull group from tree calculated values to sum up net volume
                    SumExpandedNetVolume(jg.costUnit, jg.costSpecies, jg.costProduct,
                        Global.BL.getStewardshipTrees(jg.costUnit, jg.costSpecies, jg.costProduct), jg.costPounds);
                }   //  end foreach loop

                WriteStewardshipGroups(strWriteOut,ref pageNumb,rh);
                //  write summary portion
                WriteStewardshipSummary(strWriteOut, ref pageNumb, rh, justGroups);
            }   //  endif no groups
            
            return;
        }   //  end createR208


        private void SumExpandedNetVolume(string currCU, string currSP, string currPP, 
                                            IEnumerable<TreeCalculatedValuesDO> justTrees, float currLBS)
        {
            //  R208
            double currAC = 0;
            //  sum expanded net volume in listToOutput along with current group
            RegionalReports rr = new RegionalReports();
            rr.value1 = currCU;
            rr.value2 = currSP;
            rr.value3 = currPP;
            rr.value8 = currLBS;
            foreach (TreeCalculatedValuesDO jt in justTrees)
            {
                //  get stratum acres
                currAC = Utilities.ReturnCorrectAcres(jt.Tree.Stratum.Code,(long)jt.Tree.Stratum_CN);
                rr.value7 += jt.NetCUFTPP * jt.Tree.ExpansionFactor * currAC;
            }   //  end foreach loop
            listToOutput.Add(rr);
            return;
        }   //  end SumExpandedNetVolume


        private void AccumulateStratum(string currST, double currAC, IEnumerable<LCDDO> lcdList, string currMeth)
        {
            //  R201
            //  need current stratum grouped by species, PP and SP
            //  loop by groups to get data to sum from LCD
            foreach (LCDDO jg in Global.BL.getLCDOrdered("WHERE CutLeave = ? AND Stratum = ? GROUP BY ",
                                                        "Species,PrimaryProduct,SecondaryProduct", "C", currST))
            {
                List<LCDDO> justSpecies = lcdList.Where(
                    l => l.Stratum == currST && l.Species == jg.Species && l.PrimaryProduct == jg.PrimaryProduct && 
                                    l.SecondaryProduct == jg.SecondaryProduct).ToList();
                //  load group information into listToOutput
                RegionalReports rr = new RegionalReports();
                rr.value1 = jg.Stratum;
                rr.value2 = jg.Species;
                rr.value3 = jg.PrimaryProduct;
                rr.value4 = jg.SecondaryProduct;
                //  estimated number of trees
                switch (currMeth)
                {
                    case "S3P":     case "3P":
                        rr.value7 = justSpecies.Sum(j => j.TalliedTrees);
                        if (currAC > 0)
                        {
                            //  trees per acre
                            rr.value8 = justSpecies.Sum(j=>j.TalliedTrees) / currAC;
                            //  primary per acre
                            rr.value10 = justSpecies.Sum(j => j.SumNCUFT) / currAC;
                            //  secondary per acre
                            rr.value12 = justSpecies.Sum(j => j.SumNCUFTtop) / currAC;
                        }
                        else if (currAC == 0)
                        {
                            rr.value8 = -1.0;
                            rr.value10 = -1.0;
                            rr.value12 = -1.0;
                        }   //  endif
                        //   primary product volume
                        rr.value9 = justSpecies.Sum(j => j.SumNCUFT);
                        //  secondary product
                        rr.value11 = justSpecies.Sum(j => j.SumNCUFTtop);
                        break;
                    case "STR":     case "100":
                        rr.value7 = justSpecies.Sum(j => j.SumExpanFactor);
                        if (currAC > 0)
                        {
                            //  trees per acre
                            rr.value8 = justSpecies.Sum(j=>j.SumExpanFactor) / currAC;
                            //  primary per acre
                            rr.value10 = justSpecies.Sum(j => j.SumNCUFT) / currAC;
                            //  secondary per acre
                            rr.value12 = justSpecies.Sum(j => j.SumNCUFTtop) / currAC;
                        }
                        else if (currAC == 0)
                        {
                            rr.value8 = -1.0;
                            rr.value9 = -1.0;
                            rr.value12 = -1.0;
                        }   //  endif
                        //   primary product volume
                        rr.value9 = justSpecies.Sum(j => j.SumNCUFT);
                        //  secondary product
                        rr.value11 = justSpecies.Sum(j => j.SumNCUFTtop);
                        break;
                    default:
                        rr.value7 = justSpecies.Sum(j => j.SumExpanFactor) * currAC;
                        if (currAC > 0)
                        {
                            //  trees per acre
                            rr.value8 = (justSpecies.Sum(j => j.SumExpanFactor) * currAC) / currAC;
                            //  primary per acre
                            rr.value10 = (justSpecies.Sum(j => j.SumNCUFT) * currAC) / currAC;
                            //  secondary per acre
                            rr.value12 = (justSpecies.Sum(j => j.SumNCUFTtop) * currAC) / currAC;
                        }
                        else if (currAC == 0)
                        {
                            rr.value8 = -1.0;
                            rr.value10 = -1.0;
                            rr.value12 = -1.0;
                        }   //  endif
                        //  primary product volume
                        rr.value9 = justSpecies.Sum(j => j.SumNCUFT) * currAC;
                        //  secondary product volume
                        rr.value11 = justSpecies.Sum(j => j.SumNCUFTtop) * currAC;
                        break;
                }   //  end switch on method
                rr.value13 = currAC;
                listToOutput.Add(rr);
            }   //  end foreach loop on justGroups
            return;
        }   //  end AccumulateStratum


        private int AccumulateGroups(CuttingUnitDO currentUnit, List<TreeDO> treeList)
        {
            //  R207
            int printIt = 0;
            //  process by stratum
            currentUnit.Strata.Populate();
            foreach (StratumDO stratum in currentUnit.Strata)
            {
                //  need number of plots and plot size for current stratum
                double numPlots = Global.BL.GetStrataPlots(stratum.Code).Count();
                double FPSvalue = stratum.FixedPlotSize;
                //  and correct strata acres
                double currAC = Utilities.ReturnCorrectAcres(stratum.Code,
                                                        (long)stratum.Stratum_CN);
                
                //  pull all trees for product codes 14 and 20 and each species         
                double totalStems = 0;
                double treesPerAcre = 0;
                double totalDRC = 0;
                double avgDRC = 0;
                List<TreeDO> justTrees = new List<TreeDO>();
                foreach (LCDDO js in Global.BL.getLCDOrdered("WHERE CutLeave = ? AND Stratum = ?",
                                                        "GROUP BY Species,PrimaryProduct", "C", stratum.Code, ""))
                {
                    if (js.PrimaryProduct == "14" || js.PrimaryProduct == "20")
                    {
                        //  there will be something to print?
                        justTrees = treeList.FindAll(
                            delegate(TreeDO t)
                            {
                                return t.CuttingUnit_CN == currentUnit.CuttingUnit_CN &&
                                        t.Stratum_CN == stratum.Stratum_CN &&
                                        t.CountOrMeasure == "M" && t.SampleGroup.CutLeave == "C" &&
                                        t.Species == js.Species &&
                                        t.SampleGroup.PrimaryProduct == js.PrimaryProduct;
                            });
                        //  sum values needed
                        if (justTrees.Count > 0)
                        {
                            //  sum total stems and calculate tpa
                            switch (stratum.Method)
                            {
                                case "STR":
                                case "3P":
                                case "S3P":
                                    //  find all counts

                                    List<CountTreeDO> justCounts = Global.BL.getCountTrees((long)stratum.Stratum_CN).Where(
                                        ct => ct.SampleGroup.Code == js.SampleGroup && ct.CuttingUnit_CN == currentUnit.CuttingUnit_CN).ToList();
                                    totalStems = justCounts.Sum(jc => jc.TreeCount);
                                    totalStems += justTrees.Sum(jt => jt.TreeCount);
                                    //  trees per acre
                                    if (currAC > 0) treesPerAcre = totalStems / currAC;
                                    break;
                                default:
                                    totalStems = justTrees.Sum(jt => jt.TreeCount);
                                    if(numPlots > 0) treesPerAcre = (totalStems / numPlots) * FPSvalue;
                                    break;
                            }   //  end switch on method
                            //  average DRC
                            totalDRC = justTrees.Sum(jt => jt.DRC);
                            if (totalStems > 0) avgDRC = totalDRC / totalStems;
                            //  load product  into listToOutput
                            RegionalReports rr = new RegionalReports();
                            rr.value1 = currentUnit.Code;
                            rr.value2 = stratum.Code;
                            rr.value3 = js.Species;
                            rr.value4 = js.PrimaryProduct;
                            rr.value7 = numPlots;
                            rr.value8 = FPSvalue;
                            rr.value9 = totalStems;
                            rr.value10 = treesPerAcre;
                            rr.value11 = avgDRC;
                            listToOutput.Add(rr);
                            printIt = 1; 
                        }   //  endif there are product  trees
                        //  zero totals
                        totalStems = 0;
                        treesPerAcre = 0;
                        avgDRC = 0;
                    }   //  endif on primary product of 14 or 20
                }   //  end foreach loop on justSpecies
            }   //  end for k loop on strata list
            return printIt;
        }   //  end AccumulateGroups


        private void AccumulateByContractSpecies(IEnumerable<LCDDO> justGroups, IEnumerable<LCDDO> lcdList)
        {
            //  R206
            string prevST = "*";
            double EFsum = 0;
            double DBH2sum = 0;
            double DBHsum = 0;
            double NBDFTsum = 0;
            double NCUFTsum = 0;
            double currAC = 0;
            List<StratumDO> sList = Global.BL.getStratum().ToList();
            foreach (LCDDO jg in justGroups)
            {
                IEnumerable<LCDDO> justSpecies = lcdList.Where(
                    l => l.CutLeave == "C" && l.ContractSpecies == jg.ContractSpecies &&
                    //                                    l.Species == jg.Species && l.PrimaryProduct == "01";
                    l.Species == jg.Species && l.PrimaryProduct == jg.PrimaryProduct);

                foreach (LCDDO js in justSpecies)
                {
                    //  get stratum acres
                    if (prevST != js.Stratum)
                    {
                        int jthRow = sList.FindIndex(
                            delegate(StratumDO s)
                            {
                                return s.Code == js.Stratum;
                            });
                        currAC = Utilities.ReturnCorrectAcres(js.Stratum, (long)sList[jthRow].Stratum_CN);
                        prevST = js.Stratum;
                    }   //  endif
                    EFsum += js.SumExpanFactor * currAC;
                    DBH2sum += js.SumDBHOBsqrd * currAC;
                    DBHsum += js.SumDBHOB * currAC;
                    NBDFTsum += js.SumNBDFT * currAC;
                    NCUFTsum += js.SumNCUFT * currAC;
                }   //  end foreach loop on species
                if (justSpecies.Any())
                {
                    //  load into listToOutput
                    RegionalReports rr = new RegionalReports();
                    if (jg.ContractSpecies == null)
                        rr.value1 = " ";
                    else rr.value1 = jg.ContractSpecies;
                    rr.value2 = jg.Species;
                    rr.value7 = EFsum;
                    rr.value8 = DBH2sum;
                    rr.value9 = DBHsum;
                    rr.value10 = NBDFTsum;
                    rr.value11 = NCUFTsum;
                    listToOutput.Add(rr);
                }   // endif
                EFsum = 0;
                DBH2sum = 0;
                DBHsum = 0;
                NBDFTsum = 0;
                NCUFTsum = 0;
            }   //  end foreach loop on justGroups
            return;
        }   //  end AccumulateByContractSpecies


        private void AccumulateDefect(int diamClass, string volType, IEnumerable<LCDDO> justSpecies)
        {
            //  R202, R203, R204, R205
            double calcValue = 0;
            int nthRow = 0;
            int nthColumn = 0;
            //  load DIB class
            List<TreeDO> justDIBs = Global.BL.getTreeDBH("C").ToList();
            LoadTreeDIBclasses(justDIBs[justDIBs.Count - 1].DBH, defectData, diamClass);
            List<StandTables> treeCounts = new List<StandTables>();
            LoadTreeDIBclasses(justDIBs[justDIBs.Count - 1].DBH, treeCounts, diamClass);

            //  process by species
            foreach (LCDDO js in justSpecies)
            {
                //  pull calculated tree values
                foreach (TreeCalculatedValuesDO jt in Global.BL.getTreeCalculatedValues(js.Species))
                {
                    //  calculate defect and store in appropriate spot
                    switch (volType)
                    {
                        case "BDFT":
                            if (jt.GrossBDFTPP > 0)
                                calcValue = ((jt.GrossBDFTPP - jt.NetBDFTPP) / jt.GrossBDFTPP) * 100;
                            break;
                        case "CUFT":
                            if (jt.GrossCUFTPP > 0)
                                calcValue = ((jt.GrossCUFTPP - jt.NetCUFTPP) / jt.GrossCUFTPP) * 100;
                            break;
                    }   //  end switch on volume type

                    //  find index in DIB class
                    nthRow = FindTreeDIBindex(defectData, jt.Tree.DBH, diamClass);
                    //  load into defectData and increment tree counts
                    LoadProperColumn(calcValue, nthColumn, nthRow, treeCounts);
                    calcValue = 0;
                }   //  end foreach loop
                nthColumn++;
            }   //  end foreach loop
            //  update averages including overall average at the bottom of the list
            updateAverages(treeCounts);
            return;
        }   //  end AccumulateDefect


        private void LoadProperColumn(double valueToLoad, int nthColumn, int nthRow, List<StandTables> treeCounts)
        {
            switch (nthColumn)
            {
                case 0:
                    defectData[nthRow].species1 += valueToLoad;
                    treeCounts[nthRow].species1++;
                    break;
                case 1:
                    defectData[nthRow].species2 += valueToLoad;
                    treeCounts[nthRow].species2++;
                    break;
                case 2:
                    defectData[nthRow].species3 += valueToLoad;
                    treeCounts[nthRow].species3++;
                    break;
                case 3:
                    defectData[nthRow].species4 += valueToLoad;
                    treeCounts[nthRow].species4++;
                    break;
                case 4:
                    defectData[nthRow].species5 += valueToLoad;
                    treeCounts[nthRow].species5++;
                    break;
                case 5:
                    defectData[nthRow].species6 += valueToLoad;
                    treeCounts[nthRow].species6++;
                    break;
                case 6:
                    defectData[nthRow].species7 += valueToLoad;
                    treeCounts[nthRow].species7++;
                    break;
                case 7:
                    defectData[nthRow].species8 += valueToLoad;
                    treeCounts[nthRow].species8++;
                    break;
                case 8:
                    defectData[nthRow].species9 += valueToLoad;
                    treeCounts[nthRow].species9++;
                    break;
            }   //  end switch on column
            return;
        }   //  end LoadProperColumn


        private void updateAverages(List<StandTables> treeCounts)
        {
            //  updates average for defect reports
            double columnTotal = 0;
            double treeTotal = 0;
            //  need to do overall average first or the total becomes the average for the individual line
            //  total columns and put overall average in defectData
            StandTables s = new StandTables();
            for (int j = 1; j < 9; j++)
            {
                switch (j)
                {
                    case 1:
                        columnTotal = defectData.Sum(d => d.species1);
                        treeTotal = treeCounts.Sum(t => t.species1);
                        if (treeTotal > 0)
                            s.species1 = columnTotal / treeTotal;
                        break;
                    case 2:
                        columnTotal = defectData.Sum(d => d.species2);
                        treeTotal = treeCounts.Sum(t => t.species2);
                        if (treeTotal > 0)
                            s.species2 = columnTotal / treeTotal;
                        break;
                    case 3:
                        columnTotal = defectData.Sum(d => d.species3);
                        treeTotal = treeCounts.Sum(t => t.species3);
                        if (treeTotal > 0)
                            s.species3 = columnTotal / treeTotal;
                        break;
                    case 4:
                        columnTotal = defectData.Sum(d => d.species4);
                        treeTotal = treeCounts.Sum(t => t.species4);
                        if (treeTotal > 0)
                            s.species4 = columnTotal / treeTotal;
                        break;
                    case 5:
                        columnTotal = defectData.Sum(d => d.species5);
                        treeTotal = treeCounts.Sum(t => t.species5);
                        if (treeTotal > 0)
                            s.species5 = columnTotal / treeTotal;
                        break;
                    case 6:
                        columnTotal = defectData.Sum(d => d.species6);
                        treeTotal = treeCounts.Sum(t => t.species6);
                        if (treeTotal > 0)
                            s.species6 = columnTotal / treeTotal;
                        break;
                    case 7:
                        columnTotal = defectData.Sum(d => d.species7);
                        treeTotal = treeCounts.Sum(t => t.species7);
                        if (treeTotal > 0)
                            s.species7 = columnTotal / treeTotal;
                        break;
                    case 8:
                        columnTotal = defectData.Sum(d => d.species8);
                        treeTotal = treeCounts.Sum(t => t.species8);
                        if (treeTotal > 0)
                            s.species8 = columnTotal / treeTotal;
                        break;
                    case 9:
                        columnTotal = defectData.Sum(d => d.species8);
                        treeTotal = treeCounts.Sum(t => t.species8);
                        if (treeTotal > 0)
                            s.species8 = columnTotal / treeTotal;
                        break;
                }   //  end switch
            }   //  end for j loop on columns
            s.dibClass = "AVERAGE";
            defectData.Add(s);

            for (int k = 0; k < defectData.Count-1; k++)
            {
                //  loops by rows
                //  then by each column
                for (int j = 1; j < 9; j++)
                {
                    switch (j)
                    {
                        case 1:
                            if (treeCounts[k].species1 > 0)
                                defectData[k].species1 = defectData[k].species1 / treeCounts[k].species1;
                            break;
                        case 2:
                            if (treeCounts[k].species2 > 0)
                                defectData[k].species2 = defectData[k].species2 / treeCounts[k].species2;
                            break;
                        case 3:
                            if (treeCounts[k].species3 > 0)
                                defectData[k].species3 = defectData[k].species3 / treeCounts[k].species3;
                            break;
                        case 4:
                            if (treeCounts[k].species4 > 0)
                                defectData[k].species4 = defectData[k].species4 / treeCounts[k].species4;
                            break;
                        case 5:
                            if (treeCounts[k].species5 > 0)
                                defectData[k].species5 = defectData[k].species5 / treeCounts[k].species5;
                            break;
                        case 6:
                            if (treeCounts[k].species6 > 0)
                                defectData[k].species6 = defectData[k].species6 / treeCounts[k].species6;
                            break;
                        case 7:
                            if (treeCounts[k].species7 > 0)
                                defectData[k].species7 = defectData[k].species7 / treeCounts[k].species7;
                            break;
                        case 8:
                            if (treeCounts[k].species8 > 0)
                                defectData[k].species8 = defectData[k].species8 / treeCounts[k].species8;
                            break;
                        case 9:
                            if (treeCounts[k].species9 > 0)
                                defectData[k].species9 = defectData[k].species9 / treeCounts[k].species9;
                            break;
                    }   //  end switch on column
                }   // end for j loop                
            }   //  end for k loop

            return;
        }   //  end updateAverages


        private void WriteCurrentGroup(StreamWriter strWriteOut, ref int pageNumb, reportHeaders rh)
        {
            //  R201
            int firstLine = 1;
            foreach (RegionalReports lto in listToOutput)
            {
                WriteReportHeading(strWriteOut, rh.reportTitles[0], rh.reportTitles[1], rh.reportTitles[2], 
                                    rRH.R201columns, 13, ref pageNumb, "");
                prtFields.Clear();
                prtFields.Add(" ");
                if (firstLine == 1)
                    prtFields.Add(lto.value1.PadLeft(2, ' '));
                else prtFields.Add("  ");
                prtFields.Add(lto.value2.PadRight(6, ' '));
                prtFields.Add(lto.value3.PadLeft(2, '0'));
                prtFields.Add(lto.value4.PadLeft(2, '0'));
                //  estimated trees and trees per acre -- check for -1.0 in case acres was zero
                prtFields.Add(Utilities.FormatField(lto.value7, "{0,7:F0}").ToString().PadLeft(7, ' '));
                if (lto.value8 == -1.0)
                    prtFields.Add("   ***");
                else if (lto.value8 < 1.0 && lto.value7 > 0)
                    prtFields.Add("    <1");
                else
                    prtFields.Add(Utilities.FormatField(lto.value8, "{0,6:F0}").ToString().PadLeft(6, ' '));

                //  primary product net volume
                prtFields.Add(Utilities.FormatField(lto.value9, "{0,6:F0}").ToString().PadLeft(6, ' '));
                if (lto.value10 == -1.0)
                    prtFields.Add("   ***");
                else if (lto.value10 < 1.0 && lto.value9 > 0)
                    prtFields.Add("     <1");
                else
                    prtFields.Add(Utilities.FormatField(lto.value10, "{0,7:F0}").ToString().PadLeft(7, ' '));
                
                //  secondary product net volume
                prtFields.Add(Utilities.FormatField(lto.value11, "{0,6:F0}").ToString().PadLeft(6, ' '));
                if (lto.value12 == -1.0)
                    prtFields.Add("   ***");
                else if (lto.value12 < 1.0 && lto.value11 > 0)
                    prtFields.Add("    <1");
                else
                    prtFields.Add(Utilities.FormatField(lto.value12, "{0,6:F0}").ToString().PadLeft(6, ' '));

                if (firstLine == 1)
                {
                    prtFields.Add(Utilities.FormatField(lto.value13, "{0,7:F0}").ToString().PadLeft(7, ' '));
                    firstLine = 0;
                }
                else prtFields.Add("       ");

                printOneRecord(fieldLengths, prtFields, strWriteOut);
            }   //  end foreach loop on listToOutput
            return;
        }   //  end WriteCurrentGroup


        private void WriteUnitGroup(StreamWriter strWriteOut, ref int pageNumb, reportHeaders rh)
        {
            //  R207
            foreach (RegionalReports lto in listToOutput)
            {
                WriteReportHeading(strWriteOut, rh.reportTitles[0], rh.reportTitles[1], rh.reportTitles[2], 
                                        rRH.R207columns, 13, ref pageNumb, "");
                prtFields.Clear();
                prtFields.Add(" ");
                prtFields.Add(lto.value1.PadLeft(3, ' '));
                prtFields.Add(lto.value2.PadLeft(2, ' '));
                // number of plots and FPS
                prtFields.Add(Utilities.FormatField(lto.value7, "{0,3:F0}").ToString().PadLeft(3, ' '));
                prtFields.Add(Utilities.FormatField(lto.value8, "{0,2:F0}").ToString().PadLeft(2, ' '));
                //  species and product
                prtFields.Add(lto.value3.PadRight(6, ' '));
                prtFields.Add(lto.value4.PadLeft(2, ' '));
                //  total stems, TPA and average DRC
                prtFields.Add(Utilities.FormatField(lto.value9, "{0,5:F0}").ToString().PadLeft(5, ' '));
                prtFields.Add(Utilities.FormatField(lto.value10, "{0,5:F0}").ToString().PadLeft(5, ' '));
                prtFields.Add(Utilities.FormatField(lto.value11, "{0,5:F1}").ToString().PadLeft(5, ' '));

                printOneRecord(fieldLengths, prtFields, strWriteOut);
            }   //  end foreach loop on listToOutput
            //  update subtotal here
            updateSubtotal("", 0, listToOutput[0].value4);
            outputProductSubtotal(strWriteOut, ref pageNumb, rh);
            listToOutput.Clear();
            subtotalList.Clear();
            return;
        }   //  end WriteUnitGroup


        private void WriteSpeciesGroups(StreamWriter strWriteOut, ref int pageNumb, reportHeaders rh)
        {
            //  R206
            double calcValue = 0;
            foreach (RegionalReports lto in listToOutput)
            {
                WriteReportHeading(strWriteOut, rh.reportTitles[0], rh.reportTitles[1], rh.reportTitles[2], 
                                    rRH.R206columns, 13, ref pageNumb, "");
                prtFields.Clear();
                prtFields.Add(" ");
                prtFields.Add(lto.value1.PadRight(4, ' '));
                prtFields.Add(lto.value2.PadRight(6, ' '));
                //   net CUFT volume
                prtFields.Add(Utilities.FormatField(lto.value11, "{0,6:F0}").ToString().PadLeft(6, ' '));
                //  bdft/cuft ratio
                calcValue = CommonEquations.BoardCubicRatio(lto.value10, lto.value11);
                prtFields.Add(Utilities.FormatField(calcValue, "{0,7:F4}").ToString().PadLeft(7, ' '));
                //  quad mean and mean
                if (lto.value7 > 0)
                {
                    calcValue = Math.Sqrt(lto.value8 / lto.value7);
                    prtFields.Add(Utilities.FormatField(calcValue, "{0,5:F1}").ToString().PadLeft(5, ' '));
                    calcValue = lto.value9 / lto.value7;
                    prtFields.Add(Utilities.FormatField(calcValue, "{0,5:F1}").ToString().PadLeft(5, ' '));
                }
                else
                {
                    prtFields.Add("  0.0");
                    prtFields.Add("  0.0");
                }   //  endif
                printOneRecord(fieldLengths, prtFields, strWriteOut);
            }   //  end foreach loop on listToOutput
            return;
        }   //  end WriteSpeciesGroups


        private void WriteDefectGroups(StreamWriter strWriteOut, ref int pageNumb, 
                                        reportHeaders rh, int numberSpecies)
        {
            //  R202, R203, R204, R205
            string verticalLine = " |";
            WriteReportHeading(strWriteOut, rh.reportTitles[0], rh.reportTitles[1], rh.reportTitles[2], 
                                    columnHeader, 9, ref pageNumb, "");
            foreach (StandTables dd in defectData)
            {
                prtFields.Clear();
                prtFields.Add(dd.dibClass.PadLeft(5, ' '));
                if (dd.dibClass == "AVERAGE")
                    prtFields.Add(" ");
                else prtFields.Add("   ");
                prtFields.Add(verticalLine);
                //  add species
                for (int j = 0; j < numberSpecies; j++)
                {
                    switch (j)
                    {
                        case 0:
                            prtFields.Add(Utilities.FormatField(dd.species1, "{0,8:F0}").ToString().PadLeft(8, ' '));
                            break;
                        case 1:
                            prtFields.Add(Utilities.FormatField(dd.species2, "{0,8:F0}").ToString().PadLeft(8, ' '));
                            break;
                        case 2:
                            prtFields.Add(Utilities.FormatField(dd.species3, "{0,8:F0}").ToString().PadLeft(8, ' '));
                            break;
                        case 3:
                            prtFields.Add(Utilities.FormatField(dd.species4, "{0,8:F0}").ToString().PadLeft(8, ' '));
                            break;
                        case 4:
                            prtFields.Add(Utilities.FormatField(dd.species5, "{0,8:F0}").ToString().PadLeft(8, ' '));
                            break;
                        case 5:
                            prtFields.Add(Utilities.FormatField(dd.species6, "{0,8:F0}").ToString().PadLeft(8, ' '));
                            break;
                        case 6:
                            prtFields.Add(Utilities.FormatField(dd.species7, "{0,8:F0}").ToString().PadLeft(8, ' '));
                            break;
                        case 7:
                            prtFields.Add(Utilities.FormatField(dd.species8, "{0,8:F0}").ToString().PadLeft(8, ' '));
                            break;
                        case 8:
                            prtFields.Add(Utilities.FormatField(dd.species9, "{0,8:F0}").ToString().PadLeft(8, ' '));
                            break;

                    }   //  end switch
                    prtFields.Add(verticalLine);
                }   //  end for loop
                //  write line
                printOneRecord(strWriteOut, prtFields);
            }   //  end foreach loop
            return;
        }   //  end WriteDefectGroups


        private void WriteStewardshipGroups(StreamWriter strWriteOut, ref int pageNumb, reportHeaders rh)
        {
            //  R208
            // outputs top portion of the report
            string[] headerPortion = new string[2];
            headerPortion[0] = rRH.R208columns[0];
            headerPortion[1] = rRH.R208columns[1];
            double totalVolume = listToOutput.Sum(l => l.value7);
            double percentNet = 0;
            double calcValue = 0;
            foreach (RegionalReports lto in listToOutput)
            {
                WriteReportHeading(strWriteOut, rh.reportTitles[0], rh.reportTitles[1], rh.reportTitles[2],
                                    headerPortion, 10, ref pageNumb, "");
                prtFields.Clear();
                prtFields.Add("");
                prtFields.Add(lto.value1.PadLeft(3, ' '));
                prtFields.Add(lto.value2.PadRight(6, ' '));
                prtFields.Add(lto.value3.PadLeft(2, ' '));
                prtFields.Add(Utilities.FormatField(lto.value7, "{0,6:F0}").ToString().PadLeft(6, ' '));
                //  % net volume
                if (totalVolume > 0)
                    calcValue = (lto.value7 / totalVolume) * 100;
                else calcValue = 0.0;
                prtFields.Add(Utilities.FormatField(calcValue, "{0,6:F2}").ToString().PadLeft(6, ' '));
                percentNet += calcValue;
                //  pounds per cubic foot
                prtFields.Add(Utilities.FormatField(lto.value8, "{0,6:F2}").ToString().PadLeft(6, ' '));
                //  weight fraction
                prtFields.Add(Utilities.FormatField(lto.value8 * (calcValue / 100), "{0,8:F2}").ToString().PadLeft(8, ' '));
                weightFraction += lto.value8 * (calcValue/100);
                printOneRecord(fieldLengths, prtFields, strWriteOut);
            }   //  end foreach loop
            //  print total line
            strWriteOut.WriteLine("                                                _________________________                _____________");
            strWriteOut.Write("                               TOTALS              ");
            strWriteOut.Write(Utilities.FormatField(totalVolume, "{0,6:F0}").ToString().PadLeft(6, ' '));
            strWriteOut.Write(Utilities.FormatField(percentNet, "{0,6:F2}").ToString().PadLeft(16, ' '));
            strWriteOut.WriteLine(Utilities.FormatField(weightFraction, "{0,8:F2}").ToString().PadLeft(28, ' '));
            strWriteOut.WriteLine("");
            strWriteOut.WriteLine("");
            return;
        }   //  end WriteStewardshipGroups


        private void WriteStewardshipSummary(StreamWriter strWriteOut, ref int pageNumb, reportHeaders rh,
                                                IEnumerable<StewProductCosts> justGroups)
        {
            //  R208 summary portion
            double calcValue = 0;
            fieldLengths = new int[] { 8, 15, 10, 15, 17, 9, 12, 10, 5 };
            //  write header
            strWriteOut.WriteLine(rRH.R208columns[2]);
            strWriteOut.WriteLine(rRH.R208columns[3]);
            strWriteOut.WriteLine(reportConstants.longLine);
            foreach (StewProductCosts jg in justGroups)
            {
                //  find all groups in listToOutput
                List<RegionalReports> justSpecies = listToOutput.FindAll(
                    delegate(RegionalReports r)
                    {
                        return r.value1 == jg.costUnit && r.value2 == jg.costSpecies && r.value3 == jg.costProduct;
                    });
                //  load species, product and scale defect PC
                prtFields.Clear();
                prtFields.Add("");
                prtFields.Add(jg.costSpecies.PadRight(6, ' '));
                prtFields.Add(jg.costProduct.PadLeft(2, ' '));
                prtFields.Add(Utilities.FormatField(jg.scalePC, "{0,4:F1}").ToString().PadLeft(4, ' '));
                //  adjusted weight factor
                prtFields.Add(Utilities.FormatField(jg.scalePC + weightFraction, "{0,5:F2}").ToString().PadLeft(5, ' '));
                //  pounds per cf -- sum for justSpecies
                calcValue = justSpecies.Sum(j => j.value8);
                prtFields.Add(Utilities.FormatField(calcValue, "{0,5:F2}").ToString().PadLeft(5, ' '));
                //  pounds per ccf
                prtFields.Add(Utilities.FormatField(calcValue * 100, "{0,6:F2}").ToString().PadLeft(6, ' '));
                //  ton per ccf
                prtFields.Add(Utilities.FormatField((calcValue * 100) / 2000, "{0,4:F1}").ToString().PadLeft(4, ' '));
                //  cost per ton
                calcValue = (((calcValue * 100) / 2000) * jg.costCost);
                string tempValue = "$" +  Utilities.FormatField(calcValue,"{0,6:F2}").ToString().PadLeft(6,' ');
                prtFields.Add(tempValue);

                printOneRecord(fieldLengths, prtFields, strWriteOut);

            }   //  end foreach loop
            return;
        }   //  end WriteStewarshipSummary


        private void LoadColumnHeader(List<LCDDO> justSpecies)
        {
            StringBuilder columnString = new StringBuilder();
            string verticalBar = " |  ";
            columnString.Append("SPECIES  |  ");
            foreach (LCDDO js in justSpecies)
            {
                columnString.Append(js.Species.PadRight(6, ' '));
                columnString.Append(verticalBar);
            }   //  end foreach loop
            columnHeader[0] = columnString.ToString();
            return;
        }   //  end LoadColumnHeader


        private void updateSubtotal(string currST, double currAC, string currPP)
        {
            switch (currentReport)
            {
                case "R201":
                    ReportSubtotal rs = new ReportSubtotal();
                    rs.Value1 = currST;
                    rs.Value2 = "TOTAL";
                    foreach (RegionalReports lto in listToOutput)
                    {
                        rs.Value3 += lto.value7;
                        if (lto.value8 > 0) rs.Value4 += lto.value8;
                        rs.Value5 += lto.value9;
                        if (lto.value10 > 0) rs.Value6 += lto.value10;
                        rs.Value7 += lto.value11;
                        if (lto.value12 > 0) rs.Value8 += lto.value12;
                    }   //  end foreach loop
                    rs.Value9 = currAC;
                    subtotalList.Add(rs);
                    break;
                case "R206":
                    ReportSubtotal rc = new ReportSubtotal();
                    rc.Value1 = listToOutput[0].value1;
                    foreach (RegionalReports lt in listToOutput)
                    {
                        rc.Value3 += lt.value7;
                        rc.Value4 += lt.value8;
                        rc.Value5 += lt.value9;
                        rc.Value6 += lt.value10;
                        rc.Value7 += lt.value11;
                    }   //  end foreach loop
                    subtotalList.Add(rc);
                    break;
                case "R207":
                    ReportSubtotal r = new ReportSubtotal();
                    r.Value1 = "                           PRODUCT SUBTOTAL       ";
                    foreach (RegionalReports lto in listToOutput)
                    {
                        r.Value2 = lto.value4;
                        r.Value3 += lto.value9;
                        r.Value4 += lto.value10;
                        r.Value5 += lto.value11;
                    }   //  end foreach loop 
                    subtotalList.Add(r);
                    break;
            }   //  end switch on report
            return;
        }   //  end updateSubtotal
        

        private void outputSubtotal(StreamWriter strWriteOut, ref int pageNumb, reportHeaders rh)
        {
            //  R201
            WriteReportHeading(strWriteOut, rh.reportTitles[0], rh.reportTitles[1], rh.reportTitles[2], 
                                rRH.R201columns, 13, ref pageNumb, "");
            strWriteOut.WriteLine("                                      _____________________________________________________________________________________");
            strWriteOut.Write(subtotalList[0].Value1.PadLeft(7, ' '));
            strWriteOut.Write(subtotalList[0].Value2.PadLeft(7, ' '));
            strWriteOut.Write("                          ");
            strWriteOut.Write(Utilities.FormatField(subtotalList[0].Value3, "{0,7:F0}").ToString().PadLeft(7, ' '));
            if (subtotalList[0].Value4 < 1 && subtotalList[0].Value3 > 0)
                strWriteOut.Write("           <1");
            else
                strWriteOut.Write(Utilities.FormatField(subtotalList[0].Value4, "{0,6:F0}").ToString().PadLeft(13, ' '));
            strWriteOut.Write(Utilities.FormatField(subtotalList[0].Value5, "{0,6:F0}").ToString().PadLeft(11, ' '));
            if (subtotalList[0].Value6 < 1 && subtotalList[0].Value5 > 0)
                strWriteOut.Write("         <1");
            else
                strWriteOut.Write(Utilities.FormatField(subtotalList[0].Value6, "{0,7:F0}").ToString().PadLeft(11, ' '));
            strWriteOut.Write(Utilities.FormatField(subtotalList[0].Value7, "{0,6:F0}").ToString().PadLeft(12, ' '));
            if (subtotalList[0].Value8 < 1 && subtotalList[0].Value7 > 0)
                strWriteOut.Write("         <1");
            else
                strWriteOut.Write(Utilities.FormatField(subtotalList[0].Value8, "{0,6:F0}").ToString().PadLeft(11, ' '));
            strWriteOut.WriteLine(Utilities.FormatField(subtotalList[0].Value9, "{0,7:F0}").ToString().PadLeft(16, ' '));
            strWriteOut.WriteLine(reportConstants.longLine);
            strWriteOut.WriteLine(reportConstants.longLine);
            numOlines += 4;
            return;
        }   //  end outputSubtotal


        private void outputProductSubtotal(StreamWriter strWriteOut, ref int pageNumb, reportHeaders rh)
        {
            //  R207
            WriteReportHeading(strWriteOut, rh.reportTitles[0], rh.reportTitles[1], rh.reportTitles[2], 
                            rRH.R207columns, 13, ref pageNumb, "");
            strWriteOut.WriteLine(reportConstants.longLine);
            strWriteOut.Write(subtotalList[0].Value1);
            strWriteOut.Write(subtotalList[0].Value2);
            strWriteOut.Write(Utilities.FormatField(subtotalList[0].Value3, "{0,5:F0}").ToString().PadLeft(12, ' '));
            strWriteOut.Write(Utilities.FormatField(subtotalList[0].Value4, "{0,5:F0}").ToString().PadLeft(11, ' '));
            strWriteOut.WriteLine(Utilities.FormatField(subtotalList[0].Value5, "{0,5:F1}").ToString().PadLeft(10, ' '));
            strWriteOut.WriteLine(" ");
            return;
        }   //  end outputProductSubtotal


        private void outputCSsubtotal(StreamWriter strWriteOut, ref int pageNumb, reportHeaders rh)
        {
            //  R206 - contract species
            double calcValue = 0;
            WriteReportHeading(strWriteOut, rh.reportTitles[0], rh.reportTitles[1], rh.reportTitles[2], 
                                        rRH.R206columns, 13, ref pageNumb, "");
            strWriteOut.WriteLine(" ");
            strWriteOut.Write(" CONTRACT SPECIES ");
            strWriteOut.Write(subtotalList[0].Value1.PadRight(4, ' '));
            strWriteOut.Write(" TOTAL    ");
            strWriteOut.Write(Utilities.FormatField(subtotalList[0].Value7, "{0,6:F0}").ToString().PadLeft(6, ' '));
            //  ratio
            calcValue = CommonEquations.BoardCubicRatio(subtotalList[0].Value6, subtotalList[0].Value7);
            strWriteOut.Write(Utilities.FormatField(calcValue, "{0,7:F4}").ToString().PadLeft(14, ' '));
            // means
            if (subtotalList[0].Value3 > 0)
            {
                calcValue = Math.Sqrt(subtotalList[0].Value4 / subtotalList[0].Value3);
                strWriteOut.Write(Utilities.FormatField(calcValue, "{0,5:F1}").ToString().PadLeft(12, ' '));
                calcValue = subtotalList[0].Value5 / subtotalList[0].Value3;
                strWriteOut.WriteLine(Utilities.FormatField(calcValue, "{0,5:F1}").ToString().PadLeft(16, ' '));
            }
            else{
                strWriteOut.Write("         0.0");
                strWriteOut.WriteLine("             0.0");
            }   //  endif
            strWriteOut.WriteLine(" ");
            return;
        }   //  end outputCSsubtotal


        private void updateTotal(double currAC)
        {
            //  R201
            if (totalToOutput.Count == 0)
            {
                ReportSubtotal rs = new ReportSubtotal();
                rs.Value1 = "TOTAL";
                foreach (RegionalReports lto in listToOutput)
                {
                    rs.Value3 += lto.value7;
                    if (lto.value8 > 0) rs.Value4 += lto.value8;
                    rs.Value5 += lto.value9;
                    if (lto.value10 > 0) rs.Value6 += lto.value10;
                    rs.Value7 += lto.value11;
                    if (lto.value12 > 0) rs.Value8 += lto.value12;
                }   //  end foreach loop on listToOutput
                rs.Value9 += currAC;
                totalToOutput.Add(rs);
            }
            else
            {
                foreach (RegionalReports lto in listToOutput)
                {
                    totalToOutput[0].Value3 += lto.value7;
                    if (lto.value8 > 0) totalToOutput[0].Value4 += lto.value8;
                    totalToOutput[0].Value5 += lto.value9;
                    if (lto.value10 > 0) totalToOutput[0].Value6 += lto.value10;
                    totalToOutput[0].Value7 += lto.value11;
                    if (lto.value12 > 0) totalToOutput[0].Value8 += lto.value12;
                }   //  end foreach loop on listToOutput
                totalToOutput[0].Value9 += currAC;
            }   //  endif
        }   //  end updateTotal


        private void outputTotal(StreamWriter strWriteOut, ref int pageNumb, reportHeaders rh)
        {
            //  R201
            WriteReportHeading(strWriteOut, rh.reportTitles[0], rh.reportTitles[1], rh.reportTitles[2], 
                                        rRH.R201columns, 13, ref pageNumb, "");
            strWriteOut.Write("                      ");
            strWriteOut.Write(totalToOutput[0].Value1);
            strWriteOut.Write("             ");
            strWriteOut.Write(Utilities.FormatField(totalToOutput[0].Value3, "{0,7:F0}").ToString().PadLeft(7, ' '));
            strWriteOut.Write(Utilities.FormatField(totalToOutput[0].Value4, "{0,6:F0}").ToString().PadLeft(13, ' '));
            strWriteOut.Write(Utilities.FormatField(totalToOutput[0].Value5, "{0,6:F0}").ToString().PadLeft(11, ' '));
            strWriteOut.Write(Utilities.FormatField(totalToOutput[0].Value6, "{0,7:F0}").ToString().PadLeft(11, ' '));
            strWriteOut.Write(Utilities.FormatField(totalToOutput[0].Value7, "{0,6:F0}").ToString().PadLeft(12, ' '));
            strWriteOut.Write(Utilities.FormatField(totalToOutput[0].Value8, "{0,6:F0}").ToString().PadLeft(11, ' '));
            strWriteOut.WriteLine(Utilities.FormatField(totalToOutput[0].Value9, "{0,7:F0}").ToString().PadLeft(16, ' '));
            return;
        }   //  end outputTotal

    }   
}
