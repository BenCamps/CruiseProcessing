namespace CruiseProcessing.Output.R1.Models
{
    public class R1LogMethodSummeryItem
    {
        public string LoggingMethod { get; set; }
        public string Product { get; set; }
        public string ProductComponent { get; set; }
        public double GrossVolume { get; set; }
        public double NetVolume { get; set; }
        public double EstNumbTrees { get; set; }
        public double SumDBH { get; set; }
        public double SumHeight { get; set; }
        public double SumSlope { get; set; }
        public double NumberOfLogs { get; set; }
        public double UnitAcres { get; set; }
        public double NumberOfPlots { get; set; }
    }
}