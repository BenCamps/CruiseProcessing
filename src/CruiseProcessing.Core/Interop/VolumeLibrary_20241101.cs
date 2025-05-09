﻿using CruiseProcessing.Services.Logging;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text;

namespace CruiseProcessing.Interop
{
    public class VolumeLibrary_20241101 : VolumeLibraryInterop, IVolumeLibrary
    {
        private const string DLL_NAME = "vollib_20241101.dll";

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void VERNUM2(out int a);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void VOLLIBCSNVB(ref int regn,
                    StringBuilder forst,
                    StringBuilder voleq,
                    ref float mtopp,
                    ref float mtops,

                    ref float stump,
                    ref float dbhob,
                    ref float drcob,
                    StringBuilder httype,
                    ref float httot,

                    ref int htlog,
                    ref float ht1prd,
                    ref float ht2prd,
                    ref float upsht1,
                    ref float upsht2,

                    ref float upsd1,
                    ref float upsd2,
                    ref int htref,
                    ref float avgz1,
                    ref float avgz2,

                    ref int fclass,
                    ref float dbtbh,
                    ref float btr,
                    float[] vol,
                    float[,] logvol,

                    float[,] logdia,
                    float[] loglen,
                    float[] bohlt,
                    ref int tlogs,
                    ref float nologp,

                    ref float nologs,
                    ref int cutflg,
                    ref int bfpflg,
                    ref int cupflg,
                    ref int cdpflg,

                    ref int spflg,
                    StringBuilder conspec,
                    StringBuilder prod,
                    ref int httfll,
                    StringBuilder live,

                    ref int ba,
                    ref int si,
                    StringBuilder ctype,
                    ref int errflg,
                    ref int pmtflg,

                    ref MRules mRules,
                    ref int dist,

                    ref float brkht,
                    ref float brkhtd,
                    ref int fiaspcd,
                    float[] drybio,
                    float[] grnbio,

                    ref float cr,
                    ref float cull,
                    ref int decaycd,

                    int ll1,
                    int ll2,
                    int ll3,
                    int ll4,
                    int ll5,
                    int ll6,
                    int ll7,
                    int charLen);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CRZBIOMASSCS(ref int regn,
                    StringBuilder forst,
                    ref int spcd,
                    ref float dbhob,
                    ref float drcob,
                    ref float httot,
                    ref int fclass,
                    float[] vol,
                    float[] wf,
                    float[] bms,
                    ref int errflg,
                    StringBuilder prod,
                    int i1,
                    int i2);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void CRZSPDFTCS(ref int regn, StringBuilder forst, ref int spcd, float[] wf, StringBuilder agteq, StringBuilder lbreq,
    StringBuilder dbreq, StringBuilder foleq, StringBuilder tipeq, StringBuilder wf1ref, StringBuilder wf2ref, StringBuilder mcref,
    StringBuilder agtref, StringBuilder lbrref, StringBuilder dbrref, StringBuilder folref, StringBuilder tipref,
    int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13, int i14);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void GETREGNWFCS(ref int regin, StringBuilder forest, ref int fiaCode, StringBuilder prod, out float greenWf, out float deadWf, int i1, int i2);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BROWNCROWNFRACTION(ref int SPCD, ref float DBH, ref float THT, ref float CR, float[] CFWT);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BROWNTOPWOOD(ref int SPN, ref float GCUFTS, ref float WT);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BROWNCULLLOG(ref int SPN, ref float GCUFTS, ref float WT);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BROWNCULLCHUNK(ref int SPN, ref float GCUFT, ref float NCUFT, ref float FLIW, ref float WT);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void MRULESCS(ref int regn, StringBuilder voleq, StringBuilder prod, out float trim,
                                     out float minlen, out float maxlen, out int opt, out float merchl,
                                     int l1, int l2);

        public VolumeLibrary_20241101()
            : base(LoggerProvider.CreateLogger<VolumeLibrary_20241101>())
        { }

        public override int GetVersionNumber()
        {
            VERNUM2(out var i);
            return i;
        }

        public override void CalculateVolumeNVB(
            int regn, string forst, string voleq, float mtopp, float mtops,
            float stump, float dbhob, float drcob, string httype, float httot,
            int htlog, float ht1prd, float ht2prd, float upsht1, float upsht2,
            float upsd1, float upsd2, int htref, float avgz1, float avgz2,
            int fclass, float dbtbh, float btr, out float[] vol, out float[,] logvol,
            out float[,] logdia, out float[] loglen, out float[] bolht, out int tlogs, out float nologp,
            out float nologs, int cutflg, int bfpflg, int cupflg, int cdpflg,
            int spflg, string conspec, string prod, int httfll, string live,
            int ba, int si, string ctype, out int errflg, int pmtflg,
            MRules mRules, int idist,
            float brkht, float brkhtd, int fiaspcd, out float[] drybio, out float[] grnbio,
            float cr, float cull, int decaycd)
        {
            tlogs = 0;
            nologp = 0f;
            nologs = 0f;
            errflg = 0;

            vol = new float[VOLLIBNVB_VOL_SIZE];
            logvol = new float[VOLLIBNVB_LOGVOL_SIZE_X, VOLLIBNVB_LOGVOL_SIZE_Y];
            logdia = new float[VOLLIBNVB_LOGDIA_SIZE_X, VOLLIBNVB_LOGDIA_SIZE_Y];
            loglen = new float[VOLLIBNVB_LOGLEN_SIZE];
            bolht = new float[VOLLIBNVB_BOLHT_SIZE];
            drybio = new float[DRYBIO_ARRAY_SIZE];
            grnbio = new float[GRNBIO_ARRAY_SIZE];

            StringBuilder FORST = new StringBuilder(STRING_BUFFER_SIZE).Append(forst);
            StringBuilder VOLEQ = new StringBuilder(STRING_BUFFER_SIZE).Append(voleq);
            StringBuilder CTYPE = new StringBuilder(STRING_BUFFER_SIZE).Append(ctype);

            StringBuilder CONSPEC = new StringBuilder(STRING_BUFFER_SIZE).Append(conspec);
            StringBuilder PROD = new StringBuilder(STRING_BUFFER_SIZE).Append(prod);
            StringBuilder LIVE = new StringBuilder(STRING_BUFFER_SIZE).Append(live);
            StringBuilder HTTYPE = new StringBuilder(STRING_BUFFER_SIZE).Append(httype);

            Log?.LogTrace("VOLLIBCSNVB Prams \r\n" +
                            "{regn}, {forst}, {voleq}, {mtopp}, {mtops}, " +
                            "{stump}, {dbhob}, {drcob}, {httype}, {httot}, " +
                            "{htlog}, {ht1prd}, {ht2prd}, {upsht1}, {upsht2}, " +
                            "{upsd1}, {upsd2}, {htref}, {avgz1}, {avgz2}, " +
                            "{fclass}, {dbtbh}, {btr}, " +
                            "{cutflg}, {bfpflg}, {cupflg}, {cdpflg}, " +
                            "{spflg}, {conspec}, {prod}, {httfll}, {live}, " +
                            "{ba}, {si}, {ctype}, {pmtflg}, " +
                            //"{mRules}, {idist}, " +
                            "{brkht}, {brkhtd}, {fiaspcd}, " +
                            "{cr}, {cull}, {decaycd}",
                            regn, forst, voleq, mtopp, mtops,
                            stump, dbhob, drcob, httype, httot,
                            htlog, ht1prd, ht2prd, upsht1, upsht2,
                            upsd1, upsd2, htref, avgz1, avgz2,
                            fclass, dbtbh, btr,
                            cutflg, bfpflg, cupflg, cdpflg,
                            spflg, conspec, prod, httfll, live,
                            ba, si, ctype, pmtflg,
                            //mRules, idist,
                            brkht, brkhtd, fiaspcd,
                            cr, cull, decaycd);

            VOLLIBCSNVB(ref regn, FORST, VOLEQ, ref mtopp, ref mtops,
               ref stump, ref dbhob, ref drcob, HTTYPE, ref httot,
               ref htlog, ref ht1prd, ref ht2prd, ref upsht1, ref upsht2,
               ref upsd1, ref upsd2, ref htref, ref avgz1, ref avgz2,
               ref fclass, ref dbtbh, ref btr, vol, logvol,
               logdia, loglen, bolht, ref tlogs, ref nologp,
               ref nologs, ref cutflg, ref bfpflg, ref cupflg, ref cdpflg,
               ref spflg, CONSPEC, PROD, ref httfll, LIVE,
               ref ba, ref si, CTYPE, ref errflg, ref pmtflg,
               ref mRules, ref idist,
               ref brkht, ref brkhtd, ref fiaspcd, drybio, grnbio,
               ref cr, ref cull, ref decaycd,
               STRING_BUFFER_SIZE, STRING_BUFFER_SIZE, STRING_BUFFER_SIZE, STRING_BUFFER_SIZE,
                    STRING_BUFFER_SIZE, STRING_BUFFER_SIZE, STRING_BUFFER_SIZE, CHARLEN);

            if (errflg > 0)
            {
                Log?.LogInformation($"VOLLIBCSNVB Error Flag {errflg} - " + ErrorReport.GetWarningMessage(errflg.ToString()));
            }
        }

        public override CrzBiomassResult CalculateBiomass(int regn, string forst, int spcd, float dbhob, float drcob, float httot, int fclass, float[] vol, float[] wf, out int errflg, string prod)
        {
            var calculatedBiomass = new float[CRZBIOMASSCS_BMS_SIZE];
            StringBuilder FORST = new StringBuilder(STRING_BUFFER_SIZE).Append(forst);
            StringBuilder PROD = new StringBuilder(STRING_BUFFER_SIZE).Append(prod);
            errflg = 0;

            CRZBIOMASSCS(ref regn, FORST, ref spcd, ref dbhob, ref drcob, ref httot, ref fclass, vol, wf,
                                    calculatedBiomass, ref errflg, PROD, STRING_BUFFER_SIZE, STRING_BUFFER_SIZE);

            if (errflg > 0)
            {
                Log?.LogInformation($"CRZBIOMASSCS Error Flag {errflg} - " + ErrorReport.GetWarningMessage(errflg.ToString()));
            }
            return CrzBiomassResult.FromArray(calculatedBiomass);
        }

        public override float[] LookupWeightFactorsCRZSPDFTRaw(int region, string forest, int fiaCode)
        {
            float[] WF = new float[3];
            var FORST = new StringBuilder(CRZSPDFTCS_STRINGLENGTH).Append(forest);
            var AGTEQ = new StringBuilder(CRZSPDFTCS_STRINGLENGTH);
            var LBREQ = new StringBuilder(CRZSPDFTCS_STRINGLENGTH);
            var DBREQ = new StringBuilder(CRZSPDFTCS_STRINGLENGTH);
            var FOLEQ = new StringBuilder(CRZSPDFTCS_STRINGLENGTH);
            var TIPEQ = new StringBuilder(CRZSPDFTCS_STRINGLENGTH);
            var WF1REF = new StringBuilder(CRZSPDFTCS_STRINGLENGTH);
            var WF2REF = new StringBuilder(CRZSPDFTCS_STRINGLENGTH);
            var MCREF = new StringBuilder(CRZSPDFTCS_STRINGLENGTH);
            var AGTREF = new StringBuilder(CRZSPDFTCS_STRINGLENGTH);
            var LBRREF = new StringBuilder(CRZSPDFTCS_STRINGLENGTH);
            var DBRREF = new StringBuilder(CRZSPDFTCS_STRINGLENGTH);
            var FOLREF = new StringBuilder(CRZSPDFTCS_STRINGLENGTH);
            var TIPREF = new StringBuilder(CRZSPDFTCS_STRINGLENGTH);
            int REGN = region;
            int SPCD = fiaCode;

            CRZSPDFTCS(ref REGN,
                       FORST,
                       ref fiaCode,
                       WF,
                       AGTEQ,
                       LBREQ,
                       DBREQ,
                       FOLEQ,
                       TIPEQ,
                       WF1REF,
                       WF2REF,
                       MCREF,
                       AGTREF,
                       LBRREF,
                       DBRREF,
                       FOLREF,
                       TIPREF,
                       CRZSPDFTCS_STRINGLENGTH,
                       CRZSPDFTCS_STRINGLENGTH,
                       CRZSPDFTCS_STRINGLENGTH,
                       CRZSPDFTCS_STRINGLENGTH,
                       CRZSPDFTCS_STRINGLENGTH,
                       CRZSPDFTCS_STRINGLENGTH,
                       CRZSPDFTCS_STRINGLENGTH,
                       CRZSPDFTCS_STRINGLENGTH,
                       CRZSPDFTCS_STRINGLENGTH,
                       CRZSPDFTCS_STRINGLENGTH,
                       CRZSPDFTCS_STRINGLENGTH,
                       CRZSPDFTCS_STRINGLENGTH,
                       CRZSPDFTCS_STRINGLENGTH,
                       CRZSPDFTCS_STRINGLENGTH);
            // note: fiaCode is both an input and an output variable

            return WF;
        }

        public override void LookupWeightFactorsNVB(int regin, string forest, int fiaCode, string prod, out float greenWf, out float deadWf)
        {
            var FORST = new StringBuilder(STRING_BUFFER_SIZE).Append(forest);
            var PROD = new StringBuilder(STRING_BUFFER_SIZE).Append(prod);

            GETREGNWFCS(ref regin, FORST, ref fiaCode, PROD, out greenWf, out deadWf, STRING_BUFFER_SIZE, STRING_BUFFER_SIZE);
        }

        public override void BrownCrownFraction(int fiaCode, float DBH, float THT, float CR, float[] crownFractionWGT)
        {
            BROWNCROWNFRACTION(ref fiaCode, ref DBH, ref THT, ref CR, crownFractionWGT);
        }

        public override CrownFractionWeight BrownCrownFraction(int fiaCode, float DBH, float THT, float CR)
        {
            var crownFractionWGT = new float[VolumeLibraryInterop.CROWN_FACTOR_WEIGHT_ARRAY_LENGTH];
            BROWNCROWNFRACTION(ref fiaCode, ref DBH, ref THT, ref CR, crownFractionWGT);
            return CrownFractionWeight.FromArray(crownFractionWGT);
        }

        public override void BrownTopwood(int fiaCode, float grsVol, out float topwoodWGT)
        {
            topwoodWGT = 0;
            BROWNTOPWOOD(ref fiaCode, ref grsVol, ref topwoodWGT);
        }

        public override void BrownCullLog(int fiaCode, float GCUFTS, out float cullLogWGT)
        {
            cullLogWGT = 0;
            BROWNCULLLOG(ref fiaCode, ref GCUFTS, ref cullLogWGT);
        }

        public override void BrownCullChunk(int fiaCode, float GCUFT, float NCUFT, float FLIW, out float cullChunkWGT)
        {
            cullChunkWGT = 0;
            BROWNCULLCHUNK(ref fiaCode, ref GCUFT, ref NCUFT, ref FLIW, ref cullChunkWGT);
        }

        public override MRules GetMRules(int region, string volEq, string product)
        {
            StringBuilder VOLEQ = new StringBuilder(STRING_BUFFER_SIZE).Append(volEq);
            StringBuilder PROD = new StringBuilder(STRING_BUFFER_SIZE).Append(product);

            MRULESCS(ref region, VOLEQ, PROD, out float trim,
                                    out float minlen, out float maxlen, out int opt, out float merchl,
                                    STRING_BUFFER_SIZE, STRING_BUFFER_SIZE);

            return new MRules()
            {
                trim = trim,
                minlen = minlen,
                maxlen = maxlen,
                opt = opt,
                merchl = merchl
            };
        }
    }
}