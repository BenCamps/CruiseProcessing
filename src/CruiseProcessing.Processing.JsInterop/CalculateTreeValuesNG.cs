using CruiseDAL.DataObjects;
using CruiseProcessing.Data;
using CruiseProcessing.Interop;
using CruiseProcessing.Processing.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CruiseProcessing.Processing
{
    public class CalculateTreeValuesNG
    {


        protected CalculateNetVolumeNG NetVolumeCalculator { get; }
        public IVolumeLibrary VolLib { get; }
        public ILogger Log { get; }

        public CalculateTreeValuesNG(ILogger<CalculateTreeValuesNG> log)
        {
            NetVolumeCalculator = new CalculateNetVolumeNG();
            VolLib = new VolumeLibrary();
            Log = log;
        }


        public NgTreeVolume CalculateTreeVolume(NgCruiseInfo cruiseInfo, NgTreeInfo tree, NgUtilizationInfo utilizationValues, IReadOnlyCollection<NgLogInfo> logs, out IList<NgCalculationMessage> messages)
        {
            messages = new List<NgCalculationMessage>();

            var REGN = cruiseInfo.Region;
            var Forest = cruiseInfo.Forest;
            int IDIST = cruiseInfo.District;

            var volumeEquationNumber = utilizationValues.VolumeEquationNumber ??
                LookupVolumeEquationNumber(cruiseInfo.Region, cruiseInfo.Forest, cruiseInfo.District, tree.FiaCode, tree.PrimaryProduct);

            // Inputs
            double DBHOB = tree.DBH;
            double DRCOB = tree.DRC;

            double HTTOT = tree.TotalHeight;
            int HTLOG = tree.MerchHeightLogLength;
            double HT1PRD = tree.MerchHeightPrimary;
            double HT2PRD = tree.MerchHeightSecondary;
            double UPSHT1 = tree.UpperStemHeight;
            double UPSHT2 = 0.0f;
            int HTTFLL = (int)tree.HeightToFirstLiveLimb;

            double UPSD1 = tree.UpperStemDiameter;
            //float UPSD1 = td.UpperStemDOB;
            float UPSD2 = 0.0f;

            float AVGZ1 = 0.0f;
            float AVGZ2 = 0.0f;
            int HTREF = 0;

            // diameter at the top of the first log (used by R6 where most species use same VolEq)
            // in Regions that do multi stem (R2, R3, R5?) Form Class can be the number of stems on the tree
            int FCLASS = (int)tree.FormClass;
            //if (FCLASS == 0)
            //{
            //    FCLASS = (int)tdv.FormClass;
            //    AVGZ1 = tdv.AverageZ;
            //    HTREF = (int)tdv.ReferenceHeightPercent;
            //}

            double DBTBH = tree.DBHDoubleBarkThickness;
            double BTR = tree.BarkThicknessRatio;

            // CType of V is for Varable Log Length. This hasn't been used since V1
            // and at the time isn't settable by the user
            //if (CTYPE.ToString() == "V" && numLogs > 0)
            //{
            //    load LOGLEN with values or zeros
            //    for (int n = 0; n < numLogs; n++)
            //        LOGLEN[n] = (float)treeLogs[n].Length;

            //    for (int n = numLogs; n < VolumeLibraryInterop.I20; n++)
            //        LOGLEN[n] = 0;

            //    TLOGS = numLogs;
            //}


            var isSawProd = tree.PrimaryProduct == "01";
            var tcv = new NgTreeVolume { TreeID = tree.TreeID };

            //  Get top DIBs based on comparison of DIB on volume equation versus DIB on tree.
            //  If tree DIB is zero, use volume equation DIB.  Else use tree DIB
            double minTopDibPrimary = (isSawProd) ? utilizationValues.TopDibSaw : utilizationValues.TopDibNonSaw;// (tree.TopDIBPrimary <= 0) ? volEq.TopDIBPrimary : tree.TopDIBPrimary;
            double minTopDibSecondary = utilizationValues.TopDibNonSaw;// (tree.TopDIBSecondary <= 0) ? volEq.TopDIBSecondary : tree.TopDIBSecondary;

            double STUMP = (isSawProd) ? utilizationValues.StumpHeightSaw : utilizationValues.StumpHeightNonSaw;

            //  volume flags
            int CUTFLG = utilizationValues.CalcTotal ? 1 : 0;
            int BFPFLG = utilizationValues.CalcBoard ? 1 : 0;
            int CUPFLG = utilizationValues.CalcCubic ? 1 : 0;
            int CDPFLG = utilizationValues.CalcCord ? 1 : 0;
            int SPFLG = utilizationValues.CalcTopwood ? 1 : 0;

            var merchModeFlag = utilizationValues.MerchModFlag;
            int PMTFLG = (merchModeFlag == 2) ? (int)utilizationValues.MerchModFlag
                : 0;


            //  if merch rules have changed, pull those into mRules
            MRules mRules = (merchModeFlag == 2) ? new MRules(ev: utilizationValues.EvenOddSegment,
                op: utilizationValues.SegmentationLogic,
                mxln: (!isSawProd) ? (float)utilizationValues.MaxLogLengthNonSaw : 0.0f,
                mnln: (!isSawProd) ? (float)utilizationValues.MinLogLengthNonSaw : 0.0f,
                merln: (!isSawProd) ? (float)utilizationValues.MinMerchLengthNonSaw : 0.0f,
                mtpp: (float)minTopDibPrimary,
                mtps: (float)minTopDibSecondary,
                stmp: (float)STUMP,
                trm: (!isSawProd) ? (float)utilizationValues.TrimNonSaw : 0.0f)
                : new MRules(evod: 2, op: 11);

            int iFiaCode = int.Parse(tree.FiaCode); //int.Parse(volEq.VolumeEquationNumber.Substring(7, 3));

            // these variables are passed to VOLLIBCSNVB
            // but currently unused.
            float brkht = 0.0f;
            float brkhtd = 0.0f;
            float cr = 0.0f;
            float cull = 0.0f;
            int decaycd = 0;

            // unused variables
            int ba = 0;
            int si = 0;

            string CTYPE = "C";

            Log?.LogDebug("Calculating Volume For TreeID {TreeID} using VolEq: {VolumeEquationNumber}", tree.TreeID, volumeEquationNumber);

            VolLibNVBoutput volLibOutput = VolLib.CalculateVolumeNVB(
                REGN, Forest, volumeEquationNumber, minTopDibPrimary, minTopDibSecondary,
                STUMP, DBHOB, DRCOB, tree.MerchHeightType, HTTOT,
                HTLOG, HT1PRD, HT2PRD, UPSHT1, UPSHT2,
                UPSD1, UPSD2, HTREF, AVGZ1, AVGZ2,
                FCLASS, DBTBH, BTR, CUTFLG, BFPFLG, CUPFLG, CDPFLG,
                SPFLG, tree.ContractSpecies, tree.PrimaryProduct, HTTFLL, tree.LiveDead,
                ba, si, CTYPE, PMTFLG,
                mRules, IDIST,
                brkht, brkhtd, iFiaCode,
                cr, cull, decaycd);

            var outputLogVolumes = MakeCalculatedLogsList(logs, tree, volLibOutput).ToArray();
            //  Update log stock table with calculated values

            double hiddenDefect = (tree.LiveDead == "D") ? utilizationValues.HiddenDefectDead :
                (tree.PrimaryProduct == "01") ? utilizationValues.HiddenDefectSaw : 0.0;
            var cullDefect = utilizationValues.CullDefect;
            var seenDefect = tree.SeenDefectPrimary;
            var seenDefectSecondary = tree.SeenDefectSecondary;
            var recoverableDefect = tree.RecoverablePrimary;

            //  Next, calculate net volumes
            NetVolumeCalculator.CalculateNetVolume(CTYPE,
                REGN,
                tree.PrimaryProduct,
                volLibOutput,
                outputLogVolumes,
                tree,
                utilizationValues,
                hiddenDefect,
                cullDefect,
                seenDefect,
                seenDefectSecondary,
                minTopDibPrimary,
                out var volumesDirtyFlag);

            //  Update number of logs
            if (REGN != 10 && REGN != 6)
            {
                volLibOutput.TotalLogs = (int)Math.Ceiling(volLibOutput.NoLogsPrimary);
            }

            //  Update log defects -- except for BLM
            if (REGN != 7)
            {
                foreach(var n in Enumerable.Range(0, volLibOutput.TotalLogs))
                {
                    var logVol = volLibOutput.LogVolumes[n];
                    if (logVol.GrossCubicFoot > 0)
                    {
                        outputLogVolumes[n].SeenDefect =
                            Math.Round((logVol.GrossCubicFoot - logVol.NetCubicFoot)
                                / logVol.GrossCubicFoot * 100);
                    }
                }
            }

            if (utilizationValues.CalcBiomass)
            {
                // get the weight factor. This will also load the weight factor into the cache which will
                // be use later when writing the biomass equation report in the out file
                var sForest = Forest.ToString().PadLeft(2, '0');
                var wf =  VolLib.LookupWeightFactorsNVB(REGN, sForest, iFiaCode, tree.PrimaryProduct, tree.LiveDead);


                var secondaryWf = VolLib.LookupWeightFactorsNVB(REGN, sForest, iFiaCode, tree.SecondaryProduct, tree.LiveDead)
                    .IfZeroThen(wf);

                // volumes might have changed when calling CalculateNetVolume
                // if so we need to recalculate the main stem and top biomass values from the updated Gross Cuft
                if (volumesDirtyFlag)
                {
                    var volumes = volLibOutput.Volumes;

                    var newMainStemWt = volumes.GrossCuFt * wf;
                    var newTopWoodWt = volumes.GrossSecondaryCuFt * secondaryWf;

                    var bioValues = volLibOutput.GreenBio;
                    bioValues.SawWood = newMainStemWt;
                    bioValues.TopwoodWood = newTopWoodWt;
                    bioValues.SawBark = 0;
                    bioValues.TopwoodBark = 0;
                }
            }

            //  Store volumes in tree calculated values
            //  because of the change made to how these calculations work, the TreeCalculatedValues table is emptied
            //  prior to processing and rebuilt.
            //  Discovered when multiple volume equations are found for an individual tree, the save into the
            //  database fails.  The program needs to check for duplicate tree_CN in the list and update
            //  the record instead of adding another.  Otherwise, the tree_CN causes a constraint violations
            //  when it trys to save through the DAL.
            //  March 2014
            

            // idealy if one tree in a stratum has recoverable def then all should,
            // because ?
            var hasRecoverablePrimary = recoverableDefect > 0; 

            var percentRemoved = (utilizationValues.CalcBiomass) ? utilizationValues.PercentRemoved : 0;

            SetTreeVolumes(tcv, volLibOutput, utilizationValues,
                             hasRecoverablePrimary, cullDefect, hiddenDefect, seenDefect, recoverableDefect,
                             percentRemoved, REGN);

            //  Calculate recovered if needed
            if (hasRecoverablePrimary)
            {
                SetRecoveredVolume(tcv, cullDefect, hiddenDefect, seenDefect, recoverableDefect, REGN, messages);
            }

            if (utilizationValues.CalcBiomass)
            {
                SetTreeBiomass(tcv, volLibOutput.GreenBio, percentRemoved);
            }

            //  update volume calcs in logstock if log volume calculated
            foreach(var k in Enumerable.Range(0, volLibOutput.TotalLogs))
            {
                var logNumber = k + 1;
                var lv = outputLogVolumes.FirstOrDefault(x => x.LogNumber == logNumber);
                if (lv != null)
                {
                    var logVol = volLibOutput.LogVolumes[k];
                    if (BFPFLG == 1)
                    {
                        lv.GrossBdFt = logVol.GrossBoardFoot;
                        lv.GrossBdFtRemoved = logVol.GrossRemovedBoardFoot;
                        lv.NetBdFt = logVol.NetBoardFoot;
                    }   //  endif board foot
                    if (CUPFLG == 1)
                    {
                        lv.GrossCuFt = logVol.GrossCubicFoot;
                        lv.GrossCuFtRemoved = logVol.GrossRemovedCubicFoot;
                        lv.NetCuFt = logVol.NetCubicFoot;
                    }   //  endif cubic foot
                }
            }

            tcv.LogVolumes = outputLogVolumes;
            return tcv;
        }

        public static void SetTreeVolumes(NgTreeVolume tcv,
            VolLibNVBoutput volLibNVBoutput,
            NgUtilizationInfo utilizationInfo,
            bool hasRecoverablePrimary,
            double cullDef,
            double hidDef,
            double seenDef,
            double recvDef,
            double percentRemoved,
            int currRegion)
        {
            Volumes VOL = volLibNVBoutput.Volumes;
            int TLOGS = volLibNVBoutput.TotalLogs;
            IReadOnlyList<LogVolume> LOGVOL = volLibNVBoutput.LogVolumes;
            float numberOfLogsPrimary = volLibNVBoutput.NoLogsPrimary;
            float numberOfLogsSecondary = volLibNVBoutput.NoLogsSecondary;

            bool calcTotal = utilizationInfo.CalcTotal;
            bool calcBoard = utilizationInfo.CalcBoard;
            bool calcCubic = utilizationInfo.CalcCubic;
            bool calcCord = utilizationInfo.CalcCord;
            bool calcTopwood = utilizationInfo.CalcTopwood;


            //  updates tree record in tree calculated values list
            if (calcTotal) tcv.TotalCubicVolume = VOL[0];
            if (calcBoard)         //  board foot volume
            {
                tcv.GrossBDFTPrimary = VOL[1];
                tcv.NetBDFTPrimary = VOL[2];
            }   //  endif GBDFTflag

            if (calcCubic)         //  cubic foot volume
            {
                tcv.GrossCUFTPrimary = VOL[3];
                tcv.NetCUFTPrimary = VOL[4];
            }   //  endif GCUFTflag

            if (calcCord) tcv.CordsPrimary = VOL[5];

            if (calcTopwood)          //  secondary product was calculated
            {
                tcv.GrossCUFTSecondary = VOL[6];
                tcv.NetCUFTSecondary = VOL[7];
                tcv.CordsSecondary = VOL[8];
                tcv.GrossBDFTSecondary = VOL[11];
                tcv.NetBDFTSecondary = VOL[12];
            }
            else if (!calcTopwood)
            {
                //  reset secondary buckets to zero
                tcv.GrossCUFTSecondary = 0;
                tcv.NetCUFTSecondary = 0;
                tcv.CordsSecondary = 0;
                tcv.GrossBDFTSecondary = 0;
                tcv.NetBDFTSecondary = 0;
            }

            // save tipwood volume regardless of secondary flag status
            tcv.TipwoodVolume = VOL[14];

            //  save international calcs and number of logs
            tcv.GrossBDFTIntl = VOL[9];
            tcv.NetBDFTIntl = VOL[10];
            tcv.NumberlogsMainStem = numberOfLogsPrimary;

            //  Make sure secondary was not calculated to get correct number of logs for secondary
            if (VOL[6] != 0 || VOL[7] != 0 || VOL[8] != 0 || VOL[11] != 0 || VOL[12] != 0)
                tcv.NumberlogsTopWood = numberOfLogsSecondary;

            //  Sum removed volume into tree removed
            for (int n = 0; n < TLOGS; n++)
            {
                var logVolume = LOGVOL[n];
                tcv.GrossBDFTRemovedPrimary += logVolume.GrossRemovedBoardFoot;
                tcv.GrossCUFTRemovedPrimary += logVolume.GrossRemovedCubicFoot;
            }   //  end for n loop
        }

        public static void SetRecoveredVolume(NgTreeVolume treeVol, double cullDef, double hidDef,
                                            double seenDef, double recvDef, int region, IList<NgCalculationMessage> messages)
        {
            //  Check if recoverable defect is greater than the sum
            //  of the defects.  Is so, use the sum of the defects in place
            //  of the entered recoverable defect.  Issue a warning for this tree.
            //  This is for every region except 10
            double checkRecv = recvDef;
            if (recvDef > (cullDef + hidDef + seenDef) && region != 10)
            {
                checkRecv = cullDef + hidDef + seenDef;
                messages.Add(new NgCalculationMessage
                {
                    Level = "W",
                    RecordType = "Tree",
                    RecordID = treeVol.TreeID,
                    Message = "RECOVERABLE DEFECT GREATER THAN SUM OF DEFECTS -- SUM OF DEFECTS USED IN CALCULATION"
                });
            }   //  endif

            //  calculate recovered volume based on region
            if (region == 9)
            {
                //  No board foot recovered calculated for Region 9
                treeVol.GrossBDFTRecoveredPrimary = 0;
                treeVol.GrossCUFTRecoveredPrimary = treeVol.GrossCUFTPrimary * (checkRecv / 100);
            }
            else if (region == 10)
            {
                //  they use net instead of gross for the calc
                treeVol.GrossBDFTRecoveredPrimary = treeVol.NetBDFTPrimary * (checkRecv / 100);
                treeVol.GrossCUFTRecoveredPrimary = treeVol.NetCUFTPrimary * (checkRecv / 100);
            }
            else
            {
                treeVol.GrossBDFTRecoveredPrimary = treeVol.GrossBDFTPrimary * (checkRecv / 100);
                treeVol.GrossCUFTRecoveredPrimary = treeVol.GrossCUFTPrimary * (checkRecv / 100);
            }

            if (cullDef > 0 || hidDef > 0 || seenDef > 0)
            {
                treeVol.CordsRecoveredPrimary = treeVol.CordsPrimary / (cullDef + hidDef + seenDef) * checkRecv;
            }
        }



        protected static void SetTreeBiomass(NgTreeVolume tcv, VolLibNVBCalculatedBiomass greenBio, double percentRemoved)
        {
            var prFactor = percentRemoved / 100.0f;

            tcv.Biomasstotalstem = greenBio.AboveGroundTotal * prFactor;
            tcv.Biomasslivebranches = greenBio.Branches * prFactor;
            tcv.Biomassfoliage = greenBio.Foliage * prFactor;
            tcv.BiomassMainStemPrimary = (greenBio.SawWood + greenBio.SawBark) * prFactor;
            tcv.BiomassMainStemSecondary = (greenBio.TopwoodWood + greenBio.TopwoodBark) * prFactor;
            tcv.BiomassTip = (greenBio.TipWood + greenBio.TipBark) * prFactor;
        }

        protected static IEnumerable<NgLogVolume> MakeCalculatedLogsList(IReadOnlyCollection<NgLogInfo> treeLogs, NgTreeInfo tree,
                                    VolLibNVBoutput volOutput)
        {
            foreach (var i in Enumerable.Range(0, volOutput.TotalLogs))
            {
                var dia = volOutput.LogDiameters[i];
                var nextLogDia = volOutput.LogDiameters[i + 1];
                var vol = volOutput.LogVolumes[i];
                var logNumber = i + 1;
                NgLogVolume calcLog = new NgLogVolume()
                {
                    TreeID = tree.TreeID,
                    LogNumber = logNumber,
                    SmallEndDiameter = nextLogDia.DIB,
                    LargeEndDiameter = dia.DIB,
                    LogLength = (int)volOutput.LogLengths[i],
                    GrossBdFt = vol.GrossBoardFoot,
                    NetBdFt = vol.NetBoardFoot,
                    GrossBdFtRemoved = vol.GrossRemovedBoardFoot,
                    GrossCuFt = vol.GrossCubicFoot,
                    NetCuFt = vol.NetCubicFoot,
                    GrossCuFtRemoved = vol.GrossRemovedCubicFoot,
                    DibClass = (int)nextLogDia.ScallingDIB,
                };

                var treeLog = treeLogs.FirstOrDefault(tl => calcLog.LogNumber == tl.LogNumber);
                if (treeLog != null)
                {
                    calcLog.SeenDefect = treeLog.SeenDefect;
                    calcLog.PercentRecoverable = treeLog.PercentRecoverable;
                    calcLog.Grade = treeLog.Grade;
                }
                else
                {
                    calcLog.SeenDefect = 0;
                    calcLog.PercentRecoverable = 0;
                }

                yield return calcLog;
            }
        }

        public string? LookupVolumeEquationNumber(int region, int forest, int district, string fiaCode, string product)
        {
            var sForest = forest.ToString().PadLeft(2, '0');
            var sDistrict = district.ToString().PadLeft(2, '0');
            var iFiaCode = int.Parse(fiaCode);
            var volLibEq = VolLib.LookupVolumeEquation(region, sForest, sDistrict, iFiaCode, product, out var errorCode);

            if(errorCode > 0)
            {
                Log?.LogWarning("LookupVolumeEquation returned error code {errorCode}. Regn:{region} Forest:{sForest} District{sDistrict} FiaCode:{iFiaCode} Product:{product}"
                    , region, sForest, sDistrict, iFiaCode, product);
            }

            return volLibEq;

            //var result = volumeLists.GetVolumeEquationsByRegion(region.ToString().PadLeft(2, '0'))
            //  .Where(x => x.Forest == forest.ToString().PadLeft(2, '0') || x.Forest == "ALL")
            //  .Where(x => x.Equation.EndsWith(fiaCode))
            //  .OrderBy(x => x.Forest)
            //  .Select(x => x.Equation).FirstOrDefault();

            //if (result == null) throw new InvalidOperationException($"Could Not Find Volume Equation for Region{region} Forest{forest} FiaCode{fiaCode}");
            //return result;
        }
    }
}
