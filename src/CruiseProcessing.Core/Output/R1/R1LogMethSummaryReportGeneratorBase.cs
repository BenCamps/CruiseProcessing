#define NEW_LOGMETHUMMERY_LOGIC

using CruiseDAL.DataObjects;
using CruiseProcessing.Data;
using CruiseProcessing.Output.R1.Models;
using CruiseProcessing.OutputModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static CruiseProcessing.Output.ReportGeneratorBase;

namespace CruiseProcessing.Output.R1
{
    public abstract class R1LogMethSummaryReportGeneratorBase : OutputFileReportGeneratorBase, IReportGenerator
    {
        //  R102/R103 reports
        //  BDFT OR CUFT
        private readonly IReadOnlyList<string> R102R103columns = new string[7] {
                "  L  P",
                "  O  R",
                "  G  O",
                "     D",
                "  M  U                                                 GROSS       NET                              NET   16'LOGS/",
                "  T  C          GROSS    TOTAL    NET       ESTIM      XX/         XX/   TREES/   MEAN     MEAN     XX/   GROSS    AVG",
                "  H  T          XX       DEF%     XX        TREES      ACRE        ACRE  ACRE     DBH      HGT      TREE  XXX      SLOPE     ACRES"};

        private readonly IReadOnlyList<int> fieldLengths = new[] {
            3, // lm
            2, // prod
            2, // prod cat
            13, // gross
            9, // def
            7, //net
            12, // est trees
            11, // gross per acre
            10, // net per acre
            8, // trees per acre
            7, // mean dbh
            9, // mean ht
            9, //net per tree
            10, //logs per gross
            6, // avg slope
            10 }; // acres

        ILogger Log { get; }

        protected R1LogMethSummaryReportGeneratorBase(CpDataLayer dataLayer, string reportID, ILogger log) : base(dataLayer, reportID)
        {
            Log = log;
        }

        public int GenerateReport(TextWriter strWriteOut, HeaderFieldData headerData, int startPageNum)
        {
            HeaderData = headerData;
            var pageNumb = startPageNum;
            numOlines = 0;
            Log.LogInformation("Generating {CurrentReport} report", currentReport);
            string currentTitle = fillReportTitle(currentReport);

            List<LCDDO> lcdList = DataLayer.getLCD();
            

            string volTitle = currentReport switch
            {
                "R102" => reportConstants.volumeType.Replace("XXXXX", "BOARD"),
                "R103" => reportConstants.volumeType.Replace("XXXXX", "CUBIC"),
                _ => throw new InvalidOperationException("invalid report code, for current report generator")
            };

            switch (currentReport)
            {
                case "R102":
                    {
                        if (!lcdList.Any(l => l.SumGBDFT > 0))
                        {
                            noDataForReport(strWriteOut, currentReport, " >>>> No board foot volume for report");
                            return pageNumb;
                        }   //  endif on board foot



                        break;
                    }

                case "R103":
                    {
                        if (!lcdList.Any(l => l.SumGCUFT > 0))
                        {
                            noDataForReport(strWriteOut, currentReport, " >>>> No cubic foot volume for report");
                            return pageNumb;
                        }   //  endif on cubic foot
                        break;
                    }
            }

            SetReportTitles(currentTitle, 6, 0, 0, reportConstants.FCTO, volTitle);

            //  June 2017 -- these reports are by logging method so if blank or null
            //  cannot generate the reports
            var cuttingUnits = DataLayer.getCuttingUnits();
            if (cuttingUnits.Any(x => string.IsNullOrEmpty(x.LoggingMethod)))
            {
                noDataForReport(strWriteOut, currentReport, " >>>> One or more unit(s) missing logging method.  Cannot generate this report.");
                return pageNumb;
            }

            var loggingMethods = cuttingUnits.Select(x => x.LoggingMethod).Distinct().ToArray();

            //  determine which heights to use for mean height calculation
            List<TreeDO> tList = DataLayer.getTrees();
            whichHeightFields(out var hgtOne, out var _, tList);

            //  accumulate data by logging method
            var completeHeader = CreateCompleteHeader();
            var logMethGroups = AccumulateLogMethods(loggingMethods, hgtOne);
            WriteCurrentMethods(logMethGroups, strWriteOut, completeHeader, ref pageNumb);

            return pageNumb;

        }

        //  writes subtotal line for any subtotal in R101
        private string[] CreateCompleteHeader()
        {
            var finnishHeader = R102R103columns.ToArray();
            switch (currentReport)
            {
                case "R102":
                    finnishHeader[5] = R102R103columns[5].Replace("XX", "BF");
                    finnishHeader[6] = R102R103columns[6].Replace("XXX", "MBF").Replace("XX", "BF");
                    break;

                case "R103":
                    finnishHeader[5] = R102R103columns[5].Replace("XX", "CF");
                    finnishHeader[6] = R102R103columns[6].Replace("XXX", "CCF").Replace("XX", "CF");
                    break;
            }   //  end switch on current report
            return finnishHeader;
        }   //  end createCompleteHeader

        private R1LogMethodSummary AccumulateLogMethods(IReadOnlyCollection<string> loggingMethods, HeightFieldType hgtOne)
        {
            //  R102/R103
            var summary = new R1LogMethodSummary()
            {
                LogMethGroups = new List<R1LogMethodGroup>(),
                Totals = new R1LogMethTotals(),
            };

            var overallUnits = new List<CuttingUnitDO>();

            //  need unit list and PRO list
            List<CuttingUnitDO> cutList = DataLayer.getCuttingUnits();
            //  accumulate sums by logging method
            foreach (var logMethod in loggingMethods)
            {
                var lmUnits = new List<CuttingUnitDO>();

                var logMethodGroup = new R1LogMethodGroup()
                {
                    LoggingMethod = logMethod,
                    Products = new List<R1LoggingMethodProductGroup>(),
                    Totals = new R1LogMethTotals()
                };

                //  find all units for current method
                var methodUnits = cutList.Where(x => x.LoggingMethod == logMethod).ToArray();


                //string[] prodList = new string[6] { "01", "02", "06", "07", "08", "14" };


                // build a list of product codes in unit by looping through SGs
                var prodList = new List<string>();
                foreach (var unit in methodUnits)
                {
                    unit.Strata.Populate();
                    foreach(var st in unit.Strata)
                    {
                        var sgs = DataLayer.GetSampleGroups(st.Code);
                        foreach(var sg in sgs)
                        {
                            prodList.Add(sg.PrimaryProduct);
                            prodList.Add(sg.SecondaryProduct);
                        }
                    }
                }

                prodList = prodList.Distinct().OrderBy(x => x).ToList();

                // process by product code
                foreach (var product in prodList)
                {
                    var productGroup = new R1LoggingMethodProductGroup()
                    {
                        Product = product,
                        Items = new List<R1LogMethodSummeryItem>(),
                        Totals = new R1LogMethTotals(),
                    };

                    var primaryComponent = new R1LogMethodSummeryItem()
                    { ProductComponent = "P" };
                    var secondaryComponent = new R1LogMethodSummeryItem()
                    { ProductComponent = "S" };
                    var recoverableComponent = new R1LogMethodSummeryItem()
                    { ProductComponent = "R"};


                    foreach (CuttingUnitDO ju in methodUnits)
                    {
                        ju.Strata.Populate();

                        //  sum up each stratum
                        foreach (StratumDO stratum in ju.Strata)
                        {
                            var sampleGroups = DataLayer.GetSampleGroups(stratum.Code)
                                .Where(x => x.PrimaryProduct == product || x.SecondaryProduct == product).ToArray();

                            foreach (var sg in sampleGroups)
                            {
                                var lcds = DataLayer.GetLcds(stratum.Code, sg.Code)
                                    .Where(x => x.CutLeave == "C").ToArray();

                                foreach(var lcd in lcds)
                                {
                                    //  find proration factor for the group
                                    var pro = DataLayer.GetPro(ju.Code, lcd.Stratum, lcd.SampleGroup, lcd.STM);
                                    var proratFactor = pro?.ProrationFactor ?? 0;

                                    //  est number of trees is dependent on method
                                    var estNumTrees = (stratum.Method == "S3P" || stratum.Method == "3P") ?
                                        lcd.TalliedTrees *proratFactor : lcd.SumExpanFactor * proratFactor;

                                    
                                    if (lcd.PrimaryProduct == product)
                                    {
                                        if(currentReport == "R102" && lcd.SumGBDFT > 0)
                                        {
                                            primaryComponent.GrossVolume += lcd.SumGBDFT * proratFactor;
                                            primaryComponent.NetVolume += lcd.SumNBDFT * proratFactor;
                                        }
                                        else if(currentReport == "R103" && lcd.SumGCUFT > 0)
                                        {
                                            primaryComponent.GrossVolume += lcd.SumGCUFT * proratFactor;
                                            primaryComponent.NetVolume += lcd.SumNCUFT * proratFactor;
                                        }

                                        primaryComponent.EstNumbTrees += estNumTrees;

                                        // only sum dbh, heights, logs, ExpanFactors for primary prod
                                        primaryComponent.SumDBH += lcd.SumDBHOB * proratFactor;
                                        switch (hgtOne)
                                        {
                                            case HeightFieldType.Total:
                                                {
                                                    primaryComponent.SumHeight += lcd.SumTotHgt * proratFactor;
                                                    break;
                                                }

                                            case HeightFieldType.MerchPrimary:
                                                {
                                                    primaryComponent.SumHeight += lcd.SumMerchHgtPrim * proratFactor;
                                                    break;
                                                }

                                            case HeightFieldType.MerchSecondary:
                                                {
                                                    primaryComponent.SumHeight += lcd.SumMerchHgtSecond * proratFactor;
                                                    break;
                                                }

                                            case HeightFieldType.UpperStem:
                                                {
                                                    primaryComponent.SumHeight += lcd.SumHgtUpStem * proratFactor;
                                                    break;
                                                }
                                        }   //  end switch on height
                                        primaryComponent.NumberOfLogs += lcd.SumLogsMS * proratFactor;

                                        //productGroup.Totals.EstimatedNumbTrees += estNumTrees;
                                        //logMethodGroup.Totals.EstimatedNumbTrees += estNumTrees;
                                        //summary.Totals.EstimatedNumbTrees += estNumTrees;

                                    }
                                    else if (lcd.SecondaryProduct == product)
                                    {
                                        if (currentReport == "R102" && lcd.SumGBDFTtop > 0)
                                        {
                                            secondaryComponent.GrossVolume += lcd.SumGBDFTtop * proratFactor;
                                            secondaryComponent.NetVolume += lcd.SumNBDFTtop * proratFactor;

                                            recoverableComponent.NetVolume += lcd.SumBDFTrecv * proratFactor;

                                        }
                                        else if (currentReport == "R103" && lcd.SumGCUFTtop > 0)
                                        {
                                            secondaryComponent.GrossVolume += lcd.SumGCUFTtop * proratFactor;
                                            secondaryComponent.NetVolume += lcd.SumNCUFTtop * proratFactor;

                                            recoverableComponent.NetVolume += lcd.SumCUFTrecv * proratFactor;
                                        }
                                    }

                                    // each component will have its own tree count but we don't want to
                                    // double count trees for out total so we will add to the total separately
                                    //if (lcd.PrimaryProduct == product || lcd.SecondaryProduct == product
                                    //    )
                                    //{
                                    //    productGroup.Totals.EstimatedNumbTrees += estNumTrees;
                                    //    logMethodGroup.Totals.EstimatedNumbTrees += estNumTrees;
                                    //    summary.Totals.EstimatedNumbTrees += estNumTrees;
                                    //}
                                }
                            }


                            if (sampleGroups.Any())
                            {
                                //  Sum slope percent for this stratum
                                var justSlope = DataLayer.GetPlotsByStratum(stratum.Code)
                                        .Where(x => x.CuttingUnit_CN == ju.CuttingUnit_CN)
                                        .ToArray();

                                primaryComponent.SumSlope += justSlope.Sum(s => s.Slope);
                                primaryComponent.NumberOfPlots += justSlope.Count();
                            }
                        }

                        if (primaryComponent.GrossVolume > 0)
                        {
                            primaryComponent.UnitAcres += ju.Area;
                            productGroup.Totals.UnitAcres += ju.Area;
                            lmUnits.Add(ju); // to prevent double counting units in our product groups create a list of units used
                        }
                    }
                    //  load listToOutput with sums by product

                    if (primaryComponent.GrossVolume > 0)
                    {
                        productGroup.Items.Add(primaryComponent);

                        productGroup.Totals.UpdateVolumeTotals(primaryComponent);
                        logMethodGroup.Totals.UpdateVolumeTotals(primaryComponent);
                        summary.Totals.UpdateVolumeTotals(primaryComponent);
                    }
                    if (secondaryComponent.GrossVolume > 0)
                    {
                        productGroup.Items.Add(secondaryComponent);

                        productGroup.Totals.UpdateVolumeTotals(secondaryComponent);
                        logMethodGroup.Totals.UpdateVolumeTotals(secondaryComponent);
                        summary.Totals.UpdateVolumeTotals(secondaryComponent);
                    }
                    if (recoverableComponent.NetVolume > 0)
                    {
                        productGroup.Items.Add(recoverableComponent);

                        productGroup.Totals.UpdateVolumeTotals(recoverableComponent);
                        logMethodGroup.Totals.UpdateVolumeTotals(recoverableComponent);
                        summary.Totals.UpdateVolumeTotals(recoverableComponent);
                    }

                    if (productGroup.Items.Any())
                    {
                        logMethodGroup.Products.Add(productGroup);
                    }

                }

                lmUnits = lmUnits.DistinctBy(x => x.Code).ToList();
                logMethodGroup.Totals.UnitAcres = lmUnits.Sum(x => x.Area);
                overallUnits.AddRange(lmUnits);

                summary.LogMethGroups.Add(logMethodGroup);
            }

            overallUnits = overallUnits.DistinctBy(x => x.Code).ToList();
            summary.Totals.UnitAcres = overallUnits.Sum(x => x.Area);

            return summary;
        }

        private void WriteCurrentMethods(R1LogMethodSummary logMethSummaryData, TextWriter strWriteOut, IReadOnlyList<string> completeHeader, ref int pageNum)
        {
            //  R102/R103
            //  print by method and product
            foreach (var lmGroup in logMethSummaryData.LogMethGroups)
            {
                var loggingMethod = lmGroup.LoggingMethod;

                WriteReportHeading(strWriteOut, reportTitles[0], reportTitles[1], reportTitles[2],
                                    completeHeader, 15, ref pageNum, "");


                var prodGroups = lmGroup.Products;
                foreach (var prodGroup in prodGroups)
                {
                    var product = prodGroup.Product;

                    foreach (var listItem in prodGroup.Items)
                    {
                        var prtFields = new List<string>();

                        prtFields.Add(loggingMethod.PadLeft(3, ' ') + " ");
                        prtFields.Add(product.PadLeft(2, ' ') + " ");
                        prtFields.Add(listItem.ProductComponent);

                        //  gross volume
                        prtFields.Add(listItem.GrossVolume.ToString("F0").PadLeft(10, ' '));
                        //  defect percent
                        double defectPct = (listItem.ProductComponent == "P" && listItem.GrossVolume > 0) ?
                            (((listItem.GrossVolume - listItem.NetVolume) / listItem.GrossVolume) * 100) : 0.0d;
                        prtFields.Add(defectPct.ToString("F1").PadLeft(5, ' '));
                        //  net volume
                        prtFields.Add(listItem.NetVolume.ToString("F0").PadLeft(10, ' '));

                        // est numb trees
                        double estNumbTrees = (listItem.ProductComponent == "P") ? listItem.EstNumbTrees : 0;
                        prtFields.Add(estNumbTrees.ToString("F0").PadLeft(8, ' '));
                        //  gross and net volume per acre and trees per acre
                        double grossVolPerAcre = (listItem.UnitAcres > 0) ? listItem.GrossVolume / listItem.UnitAcres : 0.0;
                        prtFields.Add(grossVolPerAcre.ToString("F0").PadLeft(10, ' '));
                        double netVolPerAcre = (listItem.UnitAcres > 0) ? listItem.NetVolume / listItem.UnitAcres : 0.0;
                        prtFields.Add(netVolPerAcre.ToString("F0").PadLeft(10, ' '));

                        double estNumbTreesPerAcre = (listItem.ProductComponent == "P" && listItem.UnitAcres > 0) ?
                            listItem.EstNumbTrees / listItem.UnitAcres : 0.0;
                        prtFields.Add(estNumbTreesPerAcre.ToString("F1").PadLeft(6, ' '));

                        double meanDbh = (listItem.ProductComponent == "P") ? listItem.SumDBH / listItem.EstNumbTrees :
                            0.0;
                        prtFields.Add(meanDbh.ToString("F1").PadLeft(5, ' '));
                        double meanHeight = (listItem.ProductComponent == "P") ? listItem.SumHeight / listItem.EstNumbTrees
                            : 0.0;
                        prtFields.Add(meanHeight.ToString("F1").PadLeft(7, ' '));

                        double netPerTree = (listItem.ProductComponent == "P" && listItem.EstNumbTrees > 0) ? listItem.NetVolume / listItem.EstNumbTrees : 0.0;
                        prtFields.Add(netPerTree.ToString("F0").PadLeft(8, ' '));

                        double logsPerVolume = (listItem.ProductComponent == "P" && listItem.GrossVolume > 0) ?
                            currentReport switch
                            {
                                "R102" => listItem.NumberOfLogs / (listItem.GrossVolume / 1000),
                                "R103" => listItem.NumberOfLogs / (listItem.GrossVolume / 100),
                                _ => 0.0,
                            } : 0.0;
                        prtFields.Add(logsPerVolume.ToString("F1").PadLeft(6, ' '));
                        //  average slope
                        double avgSlope = (listItem.ProductComponent == "P" && listItem.NumberOfPlots > 0) ? listItem.SumSlope / listItem.NumberOfPlots : 0.0;
                        prtFields.Add(avgSlope.ToString("F1").PadLeft(5, ' '));

                        //  output acres
                        string unitAcres = (listItem.ProductComponent == "P") ? listItem.UnitAcres.ToString("F1")
                            : "N/A";
                        prtFields.Add(unitAcres.PadLeft(8, ' '));

                        printOneRecord(fieldLengths, prtFields, strWriteOut);
                    }

                    //  output product subtotal
                    OutputTotal(strWriteOut, ref pageNum, 1, product, prodGroup.Totals, completeHeader);
                }

                //  output logging method subtotal
                OutputTotal(strWriteOut, ref pageNum, 2, loggingMethod, lmGroup.Totals, completeHeader);
            }

            //  output overall total
            OutputTotal(strWriteOut, ref pageNum, 3, "", logMethSummaryData.Totals, completeHeader);
        }



        private void OutputTotal(TextWriter strWriteOut, ref int pageNum, int lineType,
                        string currTotal, R1LogMethTotals totalsLine, IReadOnlyList<string> completeHeader)
        {
            //  write subtotal or total line for any subtotal in R102/R103
            double calcValue = 0;
            WriteReportHeading(strWriteOut, reportTitles[0], reportTitles[1], reportTitles[2],
                                completeHeader, 13, ref pageNum, "");
            switch (lineType)
            {
                case 1:     //  product subtotal
                    strWriteOut.WriteLine("            _________________________________________________________________________________________________________________________");
                    strWriteOut.Write(currTotal.PadLeft(4, ' '));
                    strWriteOut.Write(" TOTAL  ");
                    break;

                case 2:     //  logging method subtotal
                    strWriteOut.WriteLine("            _________________________________________________________________________________________________________________________");
                    strWriteOut.Write(currTotal.PadLeft(4, ' '));
                    strWriteOut.Write(" TOTAL  ");
                    break;

                case 3:     //  overall total
                    strWriteOut.WriteLine(reportConstants.longLine);
                    strWriteOut.Write(" TOTALS/AVE ");
                    break;
            }   //  end switch on lineType

            //  rest of data is the same for all three subtotals and total
            //  gross volume
            strWriteOut.Write(totalsLine.GrossVolume.ToString("F0").PadLeft(10, ' '));
            //  defect percent
            if (totalsLine.GrossVolume > 0)
                calcValue = ((totalsLine.GrossVolume - totalsLine.NetVolume) / totalsLine.GrossVolume) * 100;
            strWriteOut.Write(calcValue.ToString("F1").PadLeft(8, ' '));
            //  net volume
            strWriteOut.Write(totalsLine.NetVolume.ToString("F0").PadLeft(11, ' '));
            //  estimated trees
            strWriteOut.Write(totalsLine.EstimatedNumbTrees.ToString("F0").PadLeft(9, ' '));
            //  gross and net volume per acre and trees per acre
            calcValue = totalsLine.GrossVolume / totalsLine.UnitAcres;
            strWriteOut.Write(calcValue.ToString("F0").PadLeft(11, ' '));
            calcValue = totalsLine.NetVolume / totalsLine.UnitAcres;
            strWriteOut.Write(calcValue.ToString("F0").PadLeft(11, ' '));
            calcValue = totalsLine.EstimatedNumbTrees / totalsLine.UnitAcres;
            strWriteOut.Write(calcValue.ToString("F1").PadLeft(7, ' '));
            //  mean DBH and mean height
            if (totalsLine.EstimatedNumbTrees > 0)
                calcValue = totalsLine.SumDBH / totalsLine.EstimatedNumbTrees;
            else calcValue = 0.0;
            strWriteOut.Write(calcValue.ToString("F1").PadLeft(8, ' '));
            if (totalsLine.EstimatedNumbTrees > 0)
                calcValue = totalsLine.SumHeight / totalsLine.EstimatedNumbTrees;
            else calcValue = 0.0;
            strWriteOut.Write(calcValue.ToString("F1").PadLeft(9, ' '));
            //  net per tree
            if (totalsLine.EstimatedNumbTrees > 0)
                calcValue = totalsLine.NetVolume / totalsLine.EstimatedNumbTrees;
            else calcValue = 0.0;
            strWriteOut.Write(calcValue.ToString("F0").PadLeft(9, ' '));
            //  logs per volume
            if (totalsLine.GrossVolume > 0)
            {
                if (currentReport == "R102")
                    calcValue = totalsLine.NumberOfLogs / (totalsLine.GrossVolume / 1000);
                else if (currentReport == "R103")
                    calcValue = totalsLine.NumberOfLogs / (totalsLine.GrossVolume / 100);
            }
            else calcValue = 0.0;
            strWriteOut.Write(calcValue.ToString("F1").PadLeft(7, ' '));
            //  average slope
            if (totalsLine.NumberOfPlots > 0)
                calcValue = totalsLine.SumSlope / totalsLine.NumberOfPlots;
            else calcValue = 0.0;
            strWriteOut.Write(calcValue.ToString("F1").PadLeft(8, ' '));
            //  output acres
            strWriteOut.WriteLine(totalsLine.UnitAcres.ToString("F1").PadLeft(10, ' '));
            strWriteOut.WriteLine("");
            if (lineType == 2)
            {
                strWriteOut.WriteLine(reportConstants.longLine);
                strWriteOut.WriteLine(reportConstants.longLine);
            }   //  endif
        }
    }
}
