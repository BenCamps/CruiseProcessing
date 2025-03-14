using CruiseProcessing.Interop;
using CruiseProcessing.Processing.Models;
using System.Diagnostics;

namespace CruiseProcessing.Processing
{
    public class CalculateNetVolumeNG// : CalculateNetVolume
    {
        const string LOG_GRADE_NONSAW = "8";
        const string LOG_GRADE_CULL = "9";

        public void CalculateNetVolume(string cType, int region, string pProd,
            VolLibNVBoutput volOutput, IReadOnlyList<NgLogVolume> outputLogVolumes, 
            NgTreeInfo tree, NgUtilizationInfo utilizationInfo, 
            double hiddenDefect, double cullDefect, double seenDefect, double seenDefectSecondary,
            double minTopDIBprimary, out bool volumesDirtyFlag)
        {
            volumesDirtyFlag = false;
            

            if (cType == "V")
            {
                VariableLogLength(volOutput, pProd, outputLogVolumes, cullDefect, hiddenDefect, seenDefect);
            }


            var noLogsPrimary = (int)Math.Round(volOutput.NoLogsPrimary, MidpointRounding.AwayFromZero);
            var noLogsSecondary = (int)Math.Ceiling(volOutput.NoLogsSecondary);

            double soundDefault = GetSoundDefault(region);

            //  call appropriate defect routine based on region --  default is VolumeTreeDefect
            //  some regions have multiple routines called
            if (region == 5 || region == 7 || region == 10)
            {
                if (!outputLogVolumes.Any() || volOutput.TotalLogs <= 0)
                {
                    VolumeTreeDefect(region, pProd, volOutput.Volumes,
                                     cullDefect,hiddenDefect, seenDefect, tree.SeenDefectSecondary);
                }
                else
                {
                    SetLogGrades(region, outputLogVolumes, tree.TreeGrade, noLogsPrimary, volOutput.TotalLogs);

                    VolumeLogDefect(region, volOutput.LogVolumes, volOutput.Volumes, outputLogVolumes,
                        cullDefect, hiddenDefect, seenDefect, seenDefectSecondary,
                        noLogsPrimary, noLogsSecondary, volOutput.TotalLogs, tree.RecoverablePrimary, soundDefault);

                    if (region == 7 || region == 10)
                    {
                        AdjustTreeVolumeFromLogGrades(region, pProd, volOutput, outputLogVolumes, noLogsPrimary, out volumesDirtyFlag);
                    }

                    if (region == 5 || region == 10)
                    {
                        SetDiameterClass(region, outputLogVolumes, volOutput.TotalLogs);
                    }
                }
            }
            else if (region == 6)
            {
                if (outputLogVolumes.Any())
                {
                    GetR6LogGrade(tree.TreeGrade, outputLogVolumes, volOutput.TotalLogs, noLogsPrimary, minTopDIBprimary);
                    VolumeLogDefect(region, volOutput.LogVolumes, volOutput.Volumes, outputLogVolumes,
                        cullDefect, hiddenDefect, seenDefect, seenDefectSecondary,
                        noLogsPrimary, noLogsSecondary, volOutput.TotalLogs,
                        tree.RecoverablePrimary, soundDefault);
                    AdjustTreeVolumeFromLogGrades(region, pProd, volOutput, outputLogVolumes, noLogsPrimary, out volumesDirtyFlag);
                    SetDiameterClass(region, outputLogVolumes, volOutput.TotalLogs);
                }
            }
            else
            {   //  default -- includes regions 1, 2, 3, 4, 8 and 9
                VolumeTreeDefect(region, pProd, volOutput.Volumes,
                                     cullDefect, hiddenDefect, seenDefect, seenDefectSecondary);
            }

            if (volOutput.TotalLogs > 0 && (region != 5 && region != 6 && region != 10))
            {
                SetDiameterClass(region, outputLogVolumes, volOutput.TotalLogs);
            }
        }

        protected static double GetSoundDefault(int region)
        {
            return region switch
            {
                5 => 0.25,
                6 => 0.02,
                7 or 10 => 0.33,
                _ => 0,
            };
        }

        protected static void GetR6LogGrade(string treeGrade, IReadOnlyList<NgLogVolume> logStockList, int TLOGS,
                                    int numPPlogs, double minTopDIBprimary)
        {
            /*  These comments are from the original GETLOG/GETR6GRADE in NatCRS
             *  New log grade rules:
             *      All log grades <= 6 become grade 1 if sed >= 5
             *      Log grades of 7 or 8 become 7
             *      if sed < 5, log grade becomes 7
             *      If log grade is 9 stays 9
             *      1st (if westside) dupe both grade & defect for both halfs of the six possible longlogs.
             *      2nd  standardize all cull logs.  A log is cull if grade is >= 8 or if defect is >= 7. Make all culls 8 over 9.
             *      3rd now fill in any missing grades.  start at top of tree and work down searching for any grade zero.
             *      Substitute grade of first non-cull log above.  if none default to min. grade for species.
             *      Last cull-out any (westside) logs above nine 32's.  (eastside) logs above twelve 16's.
             *      All logs end up graded.
             *      All cull logs end up with lgrd=8 and vdef=9
             *      All other logs end up with lgrd=(1-7) and vdef=(0=6)
             */
            //  NOTE:  per conversation with Chriss Roemer and Ken Cormier
            //  Zone 2 is no longer used in this routine -- January 2003

            for (int n = 0; n < TLOGS; n++)
            {
                if (logStockList[n].Grade == "0" || string.IsNullOrWhiteSpace(logStockList[n].Grade))
                    logStockList[n].Grade = treeGrade;

                string gradeTest = "0123456";
                //if (currTG.IndexOf(gradeTest) >= 0 || logStockList[n].Grade.IndexOf(gradeTest) >= 0)
                if (gradeTest.IndexOf(treeGrade) >= 0 || gradeTest.IndexOf(logStockList[n].Grade) >= 0)
                {
                    //  Per Jeff Penman, this check now uses top dib as recorded on volume equations
                    //  February 2008 -- bem
                    //  Per Jeff Penman, added setting seen defect to zero when this check happens
                    //  June 2015 -- bem
                    if (logStockList[n].SmallEndDiameter < minTopDIBprimary)
                    {
                        logStockList[n].Grade = "8";
                        logStockList[n].SeenDefect = 0;
                    }
                    //  per Jeff Penman, need to check any log grade of 8 to reset defect to zero -- June 2015
                    if (logStockList[n].Grade == "8" && logStockList[n].SeenDefect > 0)
                        logStockList[n].SeenDefect = 0;
                }   //  endif

                if (n > numPPlogs) logStockList[n].Grade = "8";

                if (logStockList[n].Grade == "9" || logStockList[n].SeenDefect > 98)
                {
                    logStockList[n].SeenDefect = 100;
                    logStockList[n].Grade = "9";
                }   //  endif
            }

            if (TLOGS >= 19)
            {
                for (int n = 19; n < TLOGS; n++)
                {
                    logStockList[n].Grade = "9";
                    logStockList[n].SeenDefect = 100;
                }   //  end for n loop
            }   //  endif

        }

        protected static void SetDiameterClass(int currRegn, IReadOnlyList<NgLogVolume> logStockList, int TLOGS)
        {
            for (int n = 0; n < TLOGS; n++)
            {
                //  used to have small end diameter scale used here.  Need to calculate it like the volume library
                //  now that only the calculated small end diameter is stored.
                int roundedSED;

                //  For Region 6, make adjustments as needed
                if (currRegn == 6)
                {
                    //  round diameter first so this matches the old program -- February 2014
                    roundedSED = (int)(Math.Round(logStockList[n].SmallEndDiameter, 0, MidpointRounding.AwayFromZero));
                    roundedSED = (int)(((roundedSED / 3.0) - 0.53) + 0.49);
                    if (roundedSED < 1)
                        logStockList[n].DibClass = 1;
                    else if (roundedSED > 28)
                        logStockList[n].DibClass = 28;
                    else
                        logStockList[n].DibClass = roundedSED;
                }
                else if (currRegn == 7)
                    logStockList[n].DibClass = (int)Math.Round(logStockList[n].SmallEndDiameter - 0.1);
                else
                    logStockList[n].DibClass = (int)Math.Round(logStockList[n].SmallEndDiameter);
            }
        }

        protected static void SetLogGrades(int currRegn, IReadOnlyList<NgLogVolume> outputLogVolumes, string treeGrade,
                          int numPPlogs, int TLOGS)
        {
            if(currRegn == 10 && TLOGS == 0)
            {
                if(treeGrade == "8" || treeGrade == "9")
                {
                    foreach (var outputLog in outputLogVolumes)
                    {
                        outputLog.SeenDefect = 100;
                    }
                }
                return;
            }

            //  skip first log record (0) -- loop starts with next log (1)
            for (int n = 0; n < TLOGS; n++)
            {
                var outputLog = outputLogVolumes[n];

                if (n < numPPlogs)
                {
                    //  For every log except the first one and log grade is blank
                    //  grade the grade from the previous log
                    //  also defect for Region 10
                    if (string.IsNullOrWhiteSpace(outputLog.Grade))
                    {
                        NgLogVolume prevLog = (n > 0) ? outputLogVolumes[n - 1] : null;
                        if (prevLog != null)
                        {
                            outputLog.Grade = prevLog.Grade;
                            if (currRegn == 10) outputLog.SeenDefect = prevLog.SeenDefect;
                        }
                        else
                        {
                            throw new InvalidOperationException("First log on tree missing log grade");
                        }
                    }

                    //  Make adjustments as needed for select regions
                    if (currRegn == 5)
                    {
                        if (outputLog.Grade == "0") outputLog.Grade = treeGrade;
                    }
                    else if (currRegn == 10)
                    {
                        //  July 2014
                        //  this no longer applies since R10 may record recoverable at the log level
                        //logStockList[n].PercentRecoverable = currDefRec;
                        //  January 2017 -- Region 10 no longer sells utility volume so
                        //  log grade 7 is recoded to 8 if the volume library spits out a grade 7
                        if (outputLog.Grade == "7")
                        {
                            outputLog.SeenDefect = 100;
                            outputLog.PercentRecoverable = 0;
                        }
                    }
                    else
                    {
                        if (outputLog.Grade == "0") outputLog.Grade = treeGrade;
                    }
                }
                else             //  topwood
                {
                    if (currRegn == 5)
                    {
                        outputLog.Grade = "T";
                        outputLog.SeenDefect = Math.Min(outputLog.SeenDefect, 100.0);
                    }
                    else if (currRegn == 7)
                    {
                        outputLog.Grade = "7";
                        outputLog.SeenDefect = Math.Min(outputLog.SeenDefect, 100.0);
                    }
                    else if (currRegn == 10)
                    {
                        //  January 2017 -- see above concerning use of grade 7
                        //logStockList[n].Grade = "7";
                        outputLog.Grade = "8";
                        outputLog.SeenDefect = Math.Min(outputLog.SeenDefect, 100.0);
                        outputLog.PercentRecoverable = 0;
                    }   //  endif current Region
                }   //  endif n

                if (currRegn == 10)
                {
                    if (string.IsNullOrWhiteSpace(outputLog.Grade)
                        && (treeGrade == "8" || treeGrade == "9")
                        || (outputLog.Grade == "8" || outputLog.Grade == "9"))
                    {
                        outputLog.SeenDefect = 100;
                        outputLog.PercentRecoverable = 0;
                    }
                }
            }   //  end for n loop


            // todo somethings not right here
            //  if no logs, should skip the loop to here
            if (outputLogVolumes.Count == 0)
            {
                for (int k = 0; k < TLOGS; k++)
                {
                    outputLogVolumes[k].Grade = treeGrade;
                    if (currRegn == 10 && (treeGrade == "8" || treeGrade == "9"))
                        outputLogVolumes[k].SeenDefect = 100;
                }
            }

            //  For Regions 7 (BLM) or 10, if the first log has a blank grade, reset it to zero.  Per KCormier June 2003 -- bem
            if ((currRegn == 7 || currRegn == 10)
                && string.IsNullOrWhiteSpace(outputLogVolumes.First().Grade))
            {
                outputLogVolumes[0].Grade = "0";
            }
        }

        private static void VariableLogLength(VolLibNVBoutput volOutput, string pProd, IReadOnlyList<NgLogVolume> calculatedLogVolumes,
                                                double cullDefect, double hiddenDefect, double seenDefect)
        {
            var newVolume = new Volumes();

            foreach (var i in Enumerable.Range(0, volOutput.TotalLogs))
            {
                var logStock = calculatedLogVolumes[i];
                if (logStock.Grade == "0")
                    logStock.SeenDefect = 100;

                var totalDefect = cullDefect + hiddenDefect + seenDefect + logStock.SeenDefect;

                var logVol = volOutput.LogVolumes[i];

                logVol.NetBoardFoot = (float)Math.Round(logVol.GrossBoardFoot * (1.0 - (totalDefect / 100.0)));
                logVol.NetCubicFoot = (float)Math.Round(logVol.GrossCubicFoot * (1.0 - (totalDefect / 100.0)));

                if (pProd == "01" && (new[] { "9", "A", "B" }.Contains(logStock.Grade)))
                {
                    newVolume.GrossSecondaryCuFt += logVol.GrossCubicFoot;
                    newVolume.NetSecondaryCuFt += logVol.NetCubicFoot;
                    newVolume.GrossSecondaryBdFt += logVol.GrossBoardFoot;
                    newVolume.NetSecondaryBdFt += logVol.NetBoardFoot;
                }
                else
                {
                    newVolume.GrossBdFt += logVol.GrossBoardFoot;
                    newVolume.NetBdFt += logVol.NetBoardFoot;
                    newVolume.GrossCuFt += logVol.GrossCubicFoot;
                    newVolume.NetCuFt += logVol.NetCubicFoot;
                }
            }

            volOutput.Volumes = newVolume;
        }

        private static void VolumeTreeDefect(int region, string primaryProduct, Volumes VOL,
                                             double cullDefect, double hiddenDefect, double seenDefPrimary, double seenDefSecondary)
        {
            // in new natcruise web application we just have a single cull and hidden defect value
            // which will be used for both primary and secondary volumes
            VolumeTreeDefect(region, primaryProduct, VOL,
                cullDefect, hiddenDefect, seenDefPrimary,
                cullDefect, hiddenDefect, seenDefSecondary);
        }

        private static void VolumeTreeDefect(int currRegn, string pProd, Volumes VOL,
                                             double cullDefPrimary, double hiddenDefPrimary, double seenDefPrimary,
                                             double cullDefSecondary, double hiddenDefSecondary, double seenDefSecondary)
        {
            // see https://usfs.app.box.com/file/233680987164 for defect calculation methods

            //float totalPrimDef, totalSecDef;
            int defectLogic = currRegn switch
            {
                2 or 4 or 7 => 2,
                8 => 3,
                10 => 4,
                _ => 1,
            };
            //  Calculate nets based on defect logic
            if (defectLogic == 1 || defectLogic == 3)
            {
                float totalPrimDef = (float)( 1 - (cullDefPrimary + hiddenDefPrimary + seenDefPrimary) / 100);
                float totalSecDef = (float)( 1 - (cullDefSecondary + hiddenDefSecondary + seenDefSecondary) / 100);
                if (totalPrimDef < 0) totalPrimDef = 0;
                if (totalSecDef < 0) totalSecDef = 0;

                VOL.NetCuFt = VOL.GrossCuFt * totalPrimDef;         //  Net CUFT primary volume
                VOL.NetSecondaryCuFt = VOL.GrossSecondaryCuFt * totalSecDef;          //  Net CUFT secondary volume
                VOL.NetSecondaryBdFt = VOL.GrossSecondaryBdFt * totalSecDef;        //  Net BDFT secondary volume
                VOL.NetBdFt = VOL.GrossBdFt * totalPrimDef;         //  Net BDFT primary volume
                VOL.NetInternationalBdFt = VOL.GrossInernationalBdFt * totalPrimDef;        //  Net International volume

                //  Region 9 applies defect weirdly.  Per MVanDyck, this is how it is to be applied.  June 2008 -- bem
                if (currRegn == 9 && pProd != "01")
                {
                    //  Apply secondary defect to primary products
                    VOL.NetCuFt = VOL.GrossCuFt * totalSecDef;      //  Net CUFT primary volume
                    VOL.NetBdFt = VOL.GrossBdFt * totalSecDef;      //  Net BDFT primary volume
                } 
            }
            else if (defectLogic == 2)
            {
                float totalPrimDef = (float)
                    ((1 - cullDefPrimary / 100)
                    * (1 - hiddenDefPrimary / 100)
                    * (1 - seenDefPrimary / 100));
                float totalSecDef = (float)
                    ((1 - cullDefSecondary / 100)
                    * (1 - hiddenDefSecondary / 100)
                    * (1 - seenDefSecondary / 100));

                VOL.NetBdFt = totalPrimDef * VOL.GrossBdFt;     //  Net BDFT primary volume
                VOL.NetCuFt = totalPrimDef * VOL.GrossCuFt;     //  Net CUFT primary volume
                VOL.NetInternationalBdFt = totalPrimDef * VOL.GrossInernationalBdFt;    //  Net International volume
                VOL.NetSecondaryCuFt = totalSecDef * VOL.GrossSecondaryCuFt;      //  Net CUFT secondary volume
                VOL.NetSecondaryBdFt = totalSecDef * VOL.GrossSecondaryBdFt;    //  Net BDFT secondary volume
            }
            else if (defectLogic == 4)
            {
                float breakageDef = (float) (1 - (cullDefPrimary / 100));
                float hiddenDef = (float) (1 - (hiddenDefPrimary / 100));
                float seenDef = (float) (1 - (seenDefPrimary / 100));
                VOL.NetBdFt = ((VOL.GrossBdFt * breakageDef) * seenDef) * hiddenDef;        //  Net BDFT primary volume
                VOL.NetCuFt = ((VOL.GrossCuFt * breakageDef) * seenDef) * hiddenDef;        //  Net CUFT primary volume
            }   //  endif defectLogic

            //  Round net volumes
            VOL.NetBdFt = (float)Math.Round(VOL.NetBdFt + 0.001, 0, MidpointRounding.AwayFromZero);
            VOL.NetCuFt = (float)Math.Round(VOL.NetCuFt + 0.001, 1, MidpointRounding.AwayFromZero);
            VOL.NetSecondaryCuFt = (float)Math.Round(VOL.NetSecondaryCuFt + 0.001, 1, MidpointRounding.AwayFromZero);
            VOL.NetInternationalBdFt = (float)Math.Round(VOL.NetInternationalBdFt + 0.001, 0, MidpointRounding.AwayFromZero);
            VOL.NetSecondaryBdFt = (float)Math.Round(VOL.NetSecondaryBdFt + 0.001, 0, MidpointRounding.AwayFromZero);
        }

        private static void VolumeLogDefect(int region, IReadOnlyList<LogVolume> LOGVOL, Volumes VOL, IReadOnlyList<NgLogVolume> logStockList,
                                            double cullDefect, double hiddenDefect, double seenDefPrimary, double seenDefSecondary,
                                            int numPPlogs, int numSPlogs, int TLOGS, double currentRec, double soundDefault)
        {
            VolumeLogDefect(region, LOGVOL, VOL, logStockList,
                cullDefect, hiddenDefect, seenDefPrimary,
                cullDefect, hiddenDefect, seenDefSecondary,
                numPPlogs, numSPlogs, TLOGS, currentRec, soundDefault);
        }

        private static void VolumeLogDefect(int currRegn, IReadOnlyList<LogVolume> LOGVOL, Volumes VOL, IReadOnlyList<NgLogVolume> logStockList,
                                            double cullDefPrimary, double hiddenDefPrimary, double seenDefPrimary,
                                            double cullDefSecondary, double hiddenDefSecondary, double seenDefSecondary,
                                            int numPPlogs, int numSPlogs, int TLOGS, double currentRec, double soundDefault)
        {
            Debug.Assert(numPPlogs + numSPlogs <= LOGVOL.Count);

            //  Calculate net log volume by region
            if (currRegn == 10)
            {
                VolumeLogDefect_R10(LOGVOL, logStockList, cullDefPrimary, hiddenDefPrimary, cullDefSecondary, hiddenDefSecondary, numPPlogs, TLOGS, currentRec);
            }
            else if (currRegn == 5)
            {
                VolumeLogDefect_R5(LOGVOL, logStockList, cullDefPrimary, hiddenDefPrimary, seenDefPrimary, cullDefSecondary, hiddenDefSecondary, seenDefSecondary, numPPlogs, TLOGS);
            }
            else if (currRegn == 7)
            {
                VolumeLogDefect_R7(LOGVOL, logStockList, cullDefPrimary, hiddenDefPrimary, seenDefPrimary, cullDefSecondary, hiddenDefSecondary, seenDefSecondary, numPPlogs, TLOGS);
            }
            else if (currRegn == 6)
            {
                VolumeLogDefect_R6(LOGVOL, logStockList, cullDefPrimary, hiddenDefPrimary, seenDefPrimary, cullDefSecondary, hiddenDefSecondary, seenDefSecondary, numPPlogs, TLOGS);
            }
            else                 //  all other regions
            {
                for (int n = 0; n < TLOGS; n++)
                {
                    double totalDefect;

                    if (n < numPPlogs)
                        totalDefect = cullDefPrimary + hiddenDefPrimary + seenDefPrimary + logStockList[n].SeenDefect;
                    else
                        totalDefect = cullDefSecondary + hiddenDefSecondary + seenDefSecondary + logStockList[n].SeenDefect;

                    if (totalDefect > 100) totalDefect = 100;

                    var logVol = LOGVOL[n];

                    //  Net board foot
                    logVol.NetBoardFoot = (float)Math.Round(logVol.GrossBoardFoot * (1.0 - (totalDefect / 100.0)), 0, MidpointRounding.AwayFromZero);
                    //  Net cubic foot
                    logVol.NetCubicFoot = (float)Math.Round(logVol.GrossCubicFoot * (1.0 - (totalDefect / 100.0)), 1, MidpointRounding.AwayFromZero);
                }   //  end for n loop
            }

            //  Check for sound log
            for (int n = 0; n < TLOGS; n++)
            {
                var logVol = LOGVOL[n];

                if (logVol[0] > 0)
                {
                    float percentSound = logVol[2] / logVol[0];
                    if (percentSound < soundDefault)
                    {
                        logVol.NetBoardFoot = 0;
                        if (currRegn == 6) logStockList[n].Grade = "9";
                    }
                }

                if (logVol[3] > 0)
                {
                    float percentSound = logVol[5] / logVol[3];
                    if (percentSound < soundDefault)
                    {
                        logVol.NetCubicFoot = 0;
                        if (currRegn == 6) logStockList[n].Grade = "9";
                    }
                }
            }

            //  Sum log volumes into tree volumes
            for (int n = 0; n < numPPlogs; n++)
            {
                var logVol = LOGVOL[n];
                VOL.NetBdFt += logVol.NetBoardFoot;
                VOL.NetCuFt += logVol.NetCubicFoot;
            }

            for (int n = 0; n < numSPlogs; n++)
            {
                var secondaryLogVol = LOGVOL[n + numPPlogs];

                VOL.NetSecondaryCuFt += secondaryLogVol.NetCubicFoot;
                VOL.NetSecondaryBdFt += secondaryLogVol.NetBoardFoot;
            }
        }

        private static void VolumeLogDefect_R5(IReadOnlyList<LogVolume> LOGVOL, IReadOnlyList<NgLogVolume> logStockList,
            double cullDefPrimary, double hiddenDefPrimary, double seenDefPrimary,
            double cullDefSecondary, double hiddenDefSecondary, double seenDefSecondary,
            int numPPlogs, int TLOGS)
        {
            for (int n = 0; n < TLOGS; n++)
            {
                double breakageDef, hiddenDef, seenDef;

                //  find seen defect from log record
                int logNumber = n + 1;
                double logSeenDef = 0;

                var logNumMatch = logStockList.FirstOrDefault(l => l.LogNumber == logNumber);
                if (logNumMatch != null)
                {
                    // this might be a bug
                    logSeenDef = logStockList[n].SeenDefect;
                }

                if (n < numPPlogs)
                {
                    breakageDef = 1.0 - (cullDefPrimary / 100.0);
                    hiddenDef = 1.0 - (hiddenDefPrimary / 100.0);
                    seenDef = 1.0 - ((logSeenDef + seenDefPrimary) / 100.0);
                }
                else
                {
                    breakageDef = 1.0 - (cullDefSecondary / 100.0);
                    hiddenDef = 1.0 - (hiddenDefSecondary / 100.0);
                    seenDef = 1.0 - ((logSeenDef + seenDefSecondary) / 100.0);
                }

                var logVol = LOGVOL[n];
                //  Board foot removed
                logVol.GrossRemovedBoardFoot = (float)Math.Round(logVol.GrossBoardFoot * breakageDef, 0, MidpointRounding.AwayFromZero);
                //  Cubic foot removed
                logVol.GrossRemovedCubicFoot = (float)Math.Round(logVol.GrossCubicFoot * breakageDef, 1, MidpointRounding.AwayFromZero);
                //  Net board foot
                logVol.NetBoardFoot = (float)Math.Round(((logVol.GrossBoardFoot * breakageDef) * seenDef) * hiddenDef, 0, MidpointRounding.AwayFromZero);
                //  Net cubic foot
                logVol.NetCubicFoot = (float)Math.Round((((logVol.GrossCubicFoot * breakageDef) * seenDef) * hiddenDef), 1, MidpointRounding.AwayFromZero);
            }
        }

        private static void VolumeLogDefect_R6(IReadOnlyList<LogVolume> LOGVOL, IReadOnlyList<NgLogVolume> logStockList,
            double cullDefPrimary, double hiddenDefPrimary, double seenDefPrimary,
            double cullDefSecondary, double hiddenDefSecondary, double seenDefSecondary,
            int numPPlogs, int TLOGS)
        {
            //  November 2016 -- need to check for non-saw logs in sawtimber logs
            //  and reset defect to zero
            
            for (int n = 0; n < TLOGS; n++)
            {
                double totalDefect;
                var logStock = logStockList[n];
                var logVol = LOGVOL[n];

                if (n < numPPlogs)
                {
                    //  check log grade to reset defect for non-saw logs
                    if (logStock.Grade == "8")
                        totalDefect = 0;
                    else totalDefect = cullDefPrimary + hiddenDefPrimary + seenDefPrimary + logStock.SeenDefect;
                }
                else totalDefect = cullDefSecondary + hiddenDefSecondary + seenDefSecondary + logStock.SeenDefect;

                if (totalDefect > 100) totalDefect = 100;

                //  Net board foot
                logVol.NetBoardFoot = (float)Math.Round(logVol.GrossBoardFoot * (1.0 - (totalDefect / 100.0)), 0, MidpointRounding.AwayFromZero);
                //  Net cubic foot
                logVol.NetCubicFoot = (float)Math.Round(logVol.GrossCubicFoot * (1.0 - (totalDefect / 100.0)), 1, MidpointRounding.AwayFromZero);
            }
        }

        private static void VolumeLogDefect_R7(IReadOnlyList<LogVolume> LOGVOL, IReadOnlyList<NgLogVolume> logStockList,
            double cullDefPrimary, double hiddenDefPrimary, double seenDefPrimary,
            double cullDefSecondary, double hiddenDefSecondary, double seenDefSecondary,
            int numPPlogs, int TLOGS)
        {
            for (int n = 0; n < TLOGS; n++)
            {
                var logStock = logStockList[n];
                var logVol = LOGVOL[n];

                //  Apply defect as recorded for every log and apply override exceptions below
                double totalDefect = (n < numPPlogs) ?
                      cullDefPrimary + hiddenDefPrimary + seenDefPrimary + logStock.SeenDefect
                    : cullDefSecondary + hiddenDefSecondary + seenDefSecondary + logStock.SeenDefect;

                if (totalDefect >= 100)
                {
                    //  Board foot removed
                    logVol.GrossRemovedBoardFoot = 0;
                    //  Cubic foot removed
                    logVol.GrossRemovedCubicFoot = 0;
                }
                else
                {
                    //  Board foot removed
                    logVol.GrossRemovedBoardFoot = logVol.GrossBoardFoot;
                    //  Cubic foot removed
                    logVol.GrossRemovedCubicFoot = logVol.GrossCubicFoot;
                }

                //  Calculate net volumes
                //  Net board foot
                logVol.NetBoardFoot = (float)Math.Round(logVol.GrossBoardFoot * (1.0 - (totalDefect / 100.0)), 0, MidpointRounding.AwayFromZero);
                //  Net cubic foot
                logVol.NetCubicFoot = (float)Math.Round(logVol.GrossCubicFoot * (1.0 - (totalDefect / 100.0)), 1, MidpointRounding.AwayFromZero);

                //  Now for the grades shown, override the volume calculated with zero
                if (logStock.Grade == "7" || logStock.Grade == "8")
                {
                    //  Net board foot
                    logVol.NetBoardFoot = 0;
                    //  Net cubic foot
                    logVol.NetCubicFoot = 0;

                    //  If seen defect is greater than 50%, this is a cull log.
                    //  Grade does not change but gross merch is reset to zero.
                    if (logStock.SeenDefect > 50)
                    {
                        logVol.GrossRemovedBoardFoot = 0;      //  BDFT
                        logVol.GrossRemovedCubicFoot = 0;      //  CUFT
                    }
                }
                else if (logStock.Grade == "9")
                {
                    //  reset gross removed to zero for BDFT and CUFT
                    logVol.GrossRemovedBoardFoot = 0;
                    logVol.GrossRemovedCubicFoot = 0;

                    //  reset net volume to zero for BDFT and CUFT
                    logVol.NetBoardFoot = 0;
                    logVol.NetCubicFoot = 0;
                }
            }
        }

        private static void VolumeLogDefect_R10(IReadOnlyList<LogVolume> LOGVOL, IReadOnlyList<NgLogVolume> logStockList,
            double cullDefPrimary, double hiddenDefPrimary, double cullDefSecondary,
            double hiddenDefSecondary,
            int numPPlogs, int TLOGS, double currentRec)
        {
            for (int n = 0; n < TLOGS; n++)
            {
                double breakageDef, hiddenDef, seenDef;
                var logStock = logStockList[n];
                var logStockGrade = logStock.Grade;

                if (n < numPPlogs)
                {
                    breakageDef = 1.0 - (cullDefPrimary / 100.0);
                    hiddenDef = 1.0 - (hiddenDefPrimary / 100.0);
                }
                else
                {
                    breakageDef = 1.0 - (cullDefSecondary / 100.0);
                    hiddenDef = 1.0 - (hiddenDefSecondary / 100.0);
                }   //  endif

                //  find seen defect from log record
                var currLN = n + 1;
                var logStockMatch = logStockList.FirstOrDefault(l => l.LogNumber == currLN);
                if (logStockMatch != null)
                { seenDef = 1.0 - (logStockMatch.SeenDefect / 100); }
                else seenDef = 1.0;

                var logVol = LOGVOL[n];
                //  Gross removed volumes -- (Grades 8-9 and breakage)
                if (logStockGrade == "8" || logStockGrade == "9")
                {
                    //  no gross removed volume
                    logVol.GrossRemovedBoardFoot = 0;
                    logVol.GrossRemovedCubicFoot = 0;
                }
                else
                {
                    //  Net board foot
                    logVol.NetBoardFoot = (float)Math.Floor(((logVol.GrossBoardFoot * breakageDef) * seenDef) * hiddenDef + 0.5);
                    //  Net cubic foot
                    logVol.NetCubicFoot = (float)(Math.Floor((((logVol.GrossCubicFoot * breakageDef) * seenDef) * hiddenDef) * 10 + 0.5) / 10.0);


                    //  then add tree breakage, tree hidden and seen from the log
                    double boardUtil = 0;
                    double cubicUtil = 0;


                    //  January 2017 -- they are no longer selling utility voulme
                    //  so calculation of utility is no longer needed
                    //float totalDef = cullDefPrimary + hiddenDefPrimary + logStock.SeenDefect;
                    //  add recoverable percent together (tree and log) and make sure it's not large than total defect
                    //  need just log grades 0-6
                    //float combinedRecPC = 0;
                    //if (logStockGrade == "0" || logStockGrade == "1" || logStockGrade == "2" ||
                    //    logStockGrade == "3" || logStockGrade == "4" || logStockGrade == "5" ||
                    //    logStockGrade == "6")
                    //{
                    //    combinedRecPC = logStock.PercentRecoverable;
                    //    combinedRecPC += currentRec;
                    //}   //  endif log grade 0-6
                    //if (combinedRecPC <= totalDef)
                    //{
                    //  Calculate utility volume using combined recoverable percent
                    //    boardUtil = Math.Floor(LOGVOL[n, 0] * combinedRecPC / 100 + 0.0499);
                    //    cubicUtil = Math.Floor((LOGVOL[n, 3] * combinedRecPC / 100) * 10 + 0.0499) / 10.0;
                    //logStockList[n].PercentRecoverable = combinedRecPC;
                    //}
                    //else
                    //{
                    //  Calculate utility volume using total defect instead
                    //    boardUtil = Math.Floor(LOGVOL[n, 0] * totalDef / 100 + 0.0499);
                    //    cubicUtil = Math.Floor((LOGVOL[n, 3] * totalDef / 100) * 10 + 0.0499) / 10.0;
                    //logStockList[n].PercentRecoverable = totalDef;
                    //}   //  endif on recoverable percent
                    //  store utility calculation in the log stock list
                    //logStock.BoardUtil = (float)boardUtil;
                    //logStock.CubicUtil = (float)cubicUtil;

                    //  Board foot removed
                    logVol.GrossRemovedBoardFoot = (float)(Math.Round(((logVol.GrossBoardFoot * breakageDef) - boardUtil), 0, MidpointRounding.AwayFromZero));
                    //  Cubic foot removed
                    logVol.GrossRemovedCubicFoot = (float)(Math.Round(((logVol.GrossCubicFoot * breakageDef) - cubicUtil), 1, MidpointRounding.AwayFromZero));

                }
            }
        }

        /// <summary>
        /// Recalculates tree volume, moving volume from primary to secondary if
        /// tree has grade 7 or 8 logs. 
        /// </summary>
        private static void AdjustTreeVolumeFromLogGrades(int region, string primaryProd, VolLibNVBoutput volOutput,
                                    IReadOnlyList<NgLogVolume> outputLogVolumes, int numLogsPrimary, out bool volumesDirtyFlag)
        {
            //volumesDirtyFlag = false;
            // HACK originaly I thought we would only set this flag in
            // the if statment inside the loop that loops the primary logs
            // because thats where we are moving volumes from primary to secondary
            // however that doesn't work, but setting it here does 🤷‍
            volumesDirtyFlag = true;


            Volumes VOL = volOutput.Volumes;
            //  Zero out volumes

            // leave TotalCubic unchanged
            VOL.GrossBdFt = 0;
            VOL.NetBdFt = 0;
            VOL.GrossCuFt = 0;
            VOL.NetCuFt = 0;
            // leave Merchantable Cords
            VOL.GrossSecondaryCuFt = 0;
            VOL.NetSecondaryCuFt = 0;
            VOL.SecondaryCords = 0;
            VOL.GrossInernationalBdFt = 0;
            VOL.NetInternationalBdFt = 0;
            VOL.GrossSecondaryBdFt = 0;
            VOL.NetSecondaryBdFt = 0;
            // because of change to profile models leave stump and tip unchanged

            IReadOnlyList<LogVolume> LOGVOL = volOutput.LogVolumes;
            int TLOGS = volOutput.TotalLogs;
            //  Primary volume
            for (int n = 0; n < numLogsPrimary; n++)
            {
                var logVol = LOGVOL[n];
                var logStockGrade = outputLogVolumes[n].Grade;

                // if there are any grade 7 or 8 logs we need to move the volume from them
                // into our secondary
                if (region == 6
                    && (logStockGrade == "7" || logStockGrade == "8")
                    && primaryProd == "01")
                //  January 2017 --  Region 10 no longer sells utility volume
                //  so no log grade 7 and this check doesn't apply
                // || (currRegn == "10" && logStockList[n].Grade == "7" && currPP == "01"))
                {
                    VOL.GrossSecondaryCuFt += logVol.GrossCubicFoot;
                    VOL.NetSecondaryCuFt += logVol.NetCubicFoot;
                    VOL.GrossSecondaryBdFt += logVol.GrossBoardFoot;
                    VOL.NetSecondaryBdFt += logVol.NetBoardFoot;

                    // set flag to indicate that volumes are being changed
                    //volumesDirtyFlag = true;
                }
                else
                {
                    VOL.GrossBdFt += logVol.GrossBoardFoot;
                    VOL.NetBdFt += logVol.NetBoardFoot;
                    VOL.GrossCuFt += logVol.GrossCubicFoot;
                    VOL.NetCuFt += logVol.NetCubicFoot;
                }
            }

            //  Secondary volume
            for (int n = numLogsPrimary; n < TLOGS; n++)
            {
                var logVol = LOGVOL[n];

                VOL.GrossSecondaryCuFt += logVol.GrossCubicFoot;
                VOL.NetSecondaryCuFt += logVol.NetCubicFoot;
                VOL.GrossSecondaryBdFt += logVol.GrossBoardFoot;
                VOL.NetSecondaryBdFt += logVol.NetBoardFoot;
            }
        }
    }
}

