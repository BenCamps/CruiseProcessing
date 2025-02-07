using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CruiseProcessing.Processing.Models
{
    public class NgTreeInfo
    {
        // tree number and id arn't used for volume calculation but these identifying fields might be useful
        // for logging and tracking data in the output. 
        public long TreeNumber { get; set; }
        public string TreeID { get; set; }

        // sample group fields
        public string PrimaryProduct { get; set; }
        

        // species table fields
        public string FiaCode { get; set; }
        public string ContractSpecies { get; set; }


        public string SpeciesCode { get; set; }
        public string LiveDead { get; set; }


        // diameters
        public double DBH { get; set; }
        public double DRC { get; set; }
        public double UpperStemDiameter { get; set; }
        public double DBHDoubleBarkThickness { get; set; }

        // heights
        public double TotalHeight { get; set; }
        public double MerchHeightPrimary { get; set; }
        public double MerchHeightSecondary { get; set; }
        public double UpperStemHeight {  get; set; }
        public double HeightToFirstLiveLimb { get; set; }


        // defect
        public double SeenDefectPrimary { get; set; }
        public double SeenDefectSecondary { get; set; }


        public bool IsFallBuckScale { get; set; }

        // likely these will come from Tree Default Value table once we have it
        // but for now get we can get them from the tree table
        // do we not have a tree default value table?!?
        public string TreeGrade { get; set; }
        public double RecoverablePrimary { get; set; }
        public string FormClass { get; set; }


        // more likely Tree Default Value table fields
        // but these aren't in the new tree table. 
        public int MerchHeightLogLength { get; set; }
        public string MerchHeightType { get; set; }
        public double AverageZ { get; set; }
        public double ReferenceHeightPercent { get; set; }
        public double BarkThicknessRatio { get; set; }

        // fields that now come from utilizaton table 
        //public double CullPrimary {  get; set; }
        //public double DefaultHiddenPrimary { get; set; }
        //public double HiddenSecondary { get; set; }

        // hidden primary now comes from the utilization table
        //public double HiddenPrimary { get; set; }

        // top dibs now come from utilization table
        //public double TopDIBPrimary { get; set; }
        //public double TopDIBSecondary { get; set; }


        // tree fields not use for volume calculation 
        //public double PoleLength { get; set; }
        //public double ClearFace {  get; set; }
        //public double CrownRatio { get; set; }
        //public string DefectCode { get; set; }
        //public double DiameterAtDefect { get; set; }
        //public double VoidPercent { get; set; }
        //public string CountOrMeasure { get; set; }
        //public int TreeCount { get; set; }
        //public int KPI {  get; set; }
        //public bool STM { get; set; }
        //public double Slope { get; set; }
        //public double Aspect {  get; set; }
        //public double TreeFactor { get; set; }
        //public double PointFactor { get; set; }
        //public string Initials { get; set; }
    }
}
