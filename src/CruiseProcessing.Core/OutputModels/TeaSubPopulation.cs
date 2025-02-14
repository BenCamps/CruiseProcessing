using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CruiseProcessing.OutputModels
{
    public class TeaSubPopulation
    {
        [Required]
        [JsonRequired]
        public string SpeciesFia {  get; set; }

        [Required]
        [JsonRequired]
        public string LiveDead {  get; set; }

        [Required]
        [JsonRequired]
        public string TreeGrade { get; set; }

        

        public double SumExpansionFactors { get; set; }
        public double EstNumberTrees { get; set; }
        public double SumDbhOb {  get; set; }

        public double SumDbhObSqrd { get; set; }

        // should we include average heights?
        // sometimes cruiser might cruise multiple heights
        // not all trees will use a given height
        // so therefore number of trees or ExpFact can't be use to get averages

        public double SumTotalHeight { get; set; }
        public double SumMerchHeight { get; set; }
        public double SumLogs {  get; set; }

        public double SumGrossBdFtRemv { get; set; }
        public double SumGrossCuFtRemv { get; set; }

        public IReadOnlyCollection<TeaProductVolume> Products { get; set; }

    }
}
