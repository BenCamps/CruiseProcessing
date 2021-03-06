﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using CruiseDAL.DataObjects;
using CruiseDAL.Schema;


namespace CruiseProcessing
{
    public static class StratumMethods
    {
        //  edit check functions
        public static int IsEmpty(IEnumerable<StratumDO> strList)
        {
            return strList.Any() ? 0 : 25;
            //  checks for empty table
            //if (strList.Count == 0)
            //    return 25;
            //else return 0;
        }   //  end IsEmpty



        //  methods pertaining to stratum table
        public static double CheckMethod(IEnumerable<StratumDO> sList, string currST)
        {
            //  returns fixed plot size or basal area factor for the current stratum
            StratumDO sdo = sList.Where(st => st.Code == currST).FirstOrDefault();
            if (sdo != null)
            {
                if (sdo.Method == "F3P" || sdo.Method == "FIX" || 
                    sdo.Method == "FIXCNT" || sdo.Method == "FCM")
                    return Convert.ToDouble(sdo.FixedPlotSize);
                else if (sdo.Method == "P3P" || sdo.Method == "PNT" ||
                         sdo.Method == "PCMTRE" || sdo.Method == "PCM" ||
                          sdo.Method == "3PPNT")
                    return Convert.ToDouble(sdo.BasalAreaFactor);
                else return 1;
            }   //  endif rtrnList not null
            return 0;
        }   //  end CheckMethod


        public static StratumDO GetStratumCN(string currST, IEnumerable<StratumDO> sList)
        {
            return sList.FirstOrDefault(s => s.Code == currST);
        }   //  end GetStratumCN


        public static ArrayList buildPrintArray(StratumDO stl, string cruiseName, double totalAcres,
                                                    double numPlots)
        {
            //  parameter list will have two other fields -- strata acres and number of plots
            ArrayList stratumArray = new ArrayList();
            stratumArray.Add("   ");
            stratumArray.Add(cruiseName.PadRight(5, ' '));
            stratumArray.Add(stl.Code.PadLeft(2, ' '));

            stratumArray.Add(stl.Method.PadRight(6, ' '));
            stratumArray.Add(Utilities.FormatField(totalAcres, "{0,6:F2}").ToString().PadLeft(6, ' '));
            stratumArray.Add(Utilities.FormatField(stl.BasalAreaFactor, "{0,7:F2}").ToString().PadLeft(7, ' '));
            stratumArray.Add(Utilities.FormatField(stl.FixedPlotSize, "{0,4:F0}").ToString().PadLeft(3, ' '));
            stratumArray.Add(Utilities.FormatField(numPlots, "{0,3:F0}").ToString().PadLeft(3, ' '));
            stratumArray.Add(stl.Description ?? (" ").PadRight(25, ' '));
            stratumArray.Add(Utilities.FormatField(stl.Month, "{0,2:F0}").ToString());
            stratumArray.Add(Utilities.FormatField(stl.Year, "{0,4:F0}").ToString());

            return stratumArray;
        }   //  end buildPrintArray


        public static ArrayList buildPrintArray(TreeCalculatedValuesDO tcv, string currUOM, ref int firstLine)
        {
            //  currently for VSM4 (CP4) report only
            ArrayList prtArray = new ArrayList();

            prtArray.Add("");
            if (firstLine == 0)
            {
                prtArray.Add(tcv.Tree.Stratum.Code.PadLeft(2, ' '));
                prtArray.Add(tcv.Tree.SampleGroup.Code.PadLeft(2,' '));
                firstLine = 1;
            }
            else
            {
                prtArray.Add("  ");
                prtArray.Add("  ");
            }   //  endif firstLine

            prtArray.Add(tcv.Tree.CuttingUnit.Code.PadLeft(3, ' '));
            if(tcv.Tree != null && tcv.Tree.Plot != null)
                prtArray.Add(tcv.Tree.Plot.PlotNumber.ToString().PadLeft(4, ' '));
            else prtArray.Add("    ");
            prtArray.Add(tcv.Tree.TreeNumber.ToString().PadLeft(4, ' '));
            prtArray.Add(tcv.Tree.Species.PadRight(6, ' '));
            prtArray.Add(Utilities.FormatField(tcv.Tree.DBH,"{0,5:F1}").ToString().PadLeft(5, ' '));
            
            //  volumes used depend on current UOM
            double currVolume = 0;
            switch (currUOM)
            {
                case "03":
                    prtArray.Add(Utilities.FormatField(tcv.GrossCUFTPP, "{0,7:F1}"));
                    prtArray.Add(Utilities.FormatField(tcv.NetCUFTPP, "{0,7:F1}"));
                    currVolume = tcv.NetCUFTPP;
                    break;
                case "01":
                    prtArray.Add(Utilities.FormatField(tcv.GrossBDFTPP, "{0,7:F1}"));
                    prtArray.Add(Utilities.FormatField(tcv.NetBDFTPP, "{0,7:F1}"));
                    currVolume = tcv.NetBDFTPP;
                    break;
                case "05":
                    prtArray.Add(Utilities.FormatField(tcv.BiomassMainStemPrimary, "{0,7:F1}"));
                    prtArray.Add(Utilities.FormatField(tcv.BiomassMainStemPrimary, "{0,7:F1}"));
                    currVolume = tcv.BiomassMainStemPrimary;
                    break;
                case "02":
                    prtArray.Add(Utilities.FormatField(tcv.CordsPP, "{0,7:F1}"));
                    prtArray.Add(Utilities.FormatField(tcv.CordsPP, "{0,7:F1}"));
                    currVolume = tcv.CordsPP;
                    break;
            }   //  end switch on UOM

            prtArray.Add(Utilities.FormatField(tcv.Tree.ExpansionFactor, "{0,8:F2}"));
            prtArray.Add(tcv.Tree.KPI.ToString().PadLeft(5, ' '));
            //  April 2017 --  separate KPI from count table since it includes measured KPI
            prtArray.Add("     ");
            prtArray.Add(tcv.Tree.TreeCount.ToString().PadLeft(6, ' '));
            
            //  calculate ratio
            if (tcv.Tree.KPI > 0)
                prtArray.Add(Utilities.FormatField((currVolume / tcv.Tree.KPI), "{0,7:F3}"));
            else prtArray.Add("       ");
            //  and finally marker's initials
            if (tcv.Tree != null && tcv.Tree.Initials != null)
                prtArray.Add(tcv.Tree.Initials.PadRight(3, ' '));
            else prtArray.Add("   ");

            return prtArray;
        }   //  end buildPrintArray
    }
}
