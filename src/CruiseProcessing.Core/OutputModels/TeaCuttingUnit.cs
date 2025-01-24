using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CruiseProcessing.OutputModels
{
    public class TeaCuttingUnit
    {
        [Required]
        [JsonRequired]
        public string CuttingUnitCode { get; set; }

        // current system doesn't have a prescription field on unit
        public string Prescription {  get; set; }

        // might be blank 
        public string LoggingMethod { get; set; }

        [Required]
        [JsonRequired]
        // currently it doesn't look like we enforce that this is greater than 0
        public double Area { get; set; }

        [Required]
        [JsonRequired]
        public IReadOnlyCollection<TeaSampleGroup> SampleGroups { get; set; }

    }
}
