using Microsoft.JavaScript.NodeApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CruiseProcessing.Processing.Models
{
    [JSExport]
    public class NgUtilizationInfo
    {
        // used in V2 and V3, I'll include this as an input for now but as an optional input.
        // it doesn't exist in the database. Generaly its based off of the region, forest, and FIA code
        // if null or blank Volume Library will use Tree FIA code to lookup VolumeEquation Number 
        public string VolumeEquationNumber { get; set; }

        // these flags arn't included in utilization table. my guess is they will all be set to true. 
        public bool CalcTotal {  get; set; }
        public bool CalcBoard {  get; set; }
        public bool CalcCubic { get; set; }
        public bool CalcCord { get; set; }

        // these flags are in the utilization table 
        public bool CalcTopwood { get; set; }
        public bool CalcBiomass { get; set; }

        // use for biomass calculation
        public int PercentRemoved { get; set; }


        public double CullDefect {  get; set; }
        public double HiddenDefectSaw { get; set; }
        public double HiddenDefectDead { get; set; }

        // merch rule fields 
        public double TopDibSaw { get; set; }
        public double TopDibNonSaw { get; set; }
        public double StumpHeightSaw { get; set; }
        public double StumpHeightNonSaw { get; set; }
        public double StumpHeightBiomass { get; set; }

        // more merch rules fields. currently these are't in the utilization table. ignore for now
        public int EvenOddSegment { get; set; }
        public int SegmentationLogic { get; set; }
        public double MaxLogLengthNonSaw { get; set; }
        public double MinLogLengthNonSaw { get; set; }
        public double MinMerchLengthNonSaw {  get; set; }
        public double TrimNonSaw {  get; set; }

        // additional un-used field
        public int MerchModFlag { get; set; }
    }
}
