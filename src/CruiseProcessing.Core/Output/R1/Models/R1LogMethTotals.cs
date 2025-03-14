namespace CruiseProcessing.Output.R1.Models
{
    public class R1LogMethTotals
    {
        public double GrossVolume { get; set; }
        public double NetVolume { get; set; }

        public double EstimatedNumbTrees { get; set; }

        // these values only used for the primary component
        public double SumDBH { get; set; }

        public double SumHeight { get; set; }
        public double SumSlope { get; set; }
        public double NumberOfLogs { get; set; }
        public double UnitAcres { get; set; }
        public double NumberOfPlots { get; set; }

        public void UpdateVolumeTotals(R1LogMethodSummeryItem component)
        {
            GrossVolume += component.GrossVolume;
            NetVolume += component.NetVolume;

            if (component.ProductComponent == "P")
            {
                SumDBH += component.SumDBH;
                SumHeight += component.SumHeight;
                SumSlope += component.SumSlope;
                NumberOfLogs += component.NumberOfLogs;
                //UnitAcres += component.UnitAcres;
                NumberOfPlots += component.NumberOfPlots;
                EstimatedNumbTrees += component.EstNumbTrees;
            }
        }
    }
}