using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CruiseProcessing.OutputModels
{
    public class TeaSampleGroup
    {
        [Required]
        [JsonRequired]
        public string StratumCode { get; set; }

        [Required]
        [JsonRequired]
        public string SampleGroupCode { get; set; }

        //[Required]
        //[JsonRequired]
        //public string SampleGroupID { get; set; }

        [Required]
        [JsonRequired]
        // usualy defined at the sale level but for best to cover cases where it can be at SG we will report it at SG level
        public string UOM {  get; set; }

        [Required]
        [JsonRequired]
        public IReadOnlyCollection<TeaUnitSubpopulation> SubPopulations { get; set; }

    }
}
