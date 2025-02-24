using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CruiseProcessing.OutputModels
{
    public class TeaReport
    {
        [Required]
        [JsonRequired]
        // currently just a number however new web cruise application this will be numbers and dashes ('-')
        public string SaleNumber { get; set; }

        [Required]
        [JsonRequired]
        public string SaleName { get; set; }

        [Required]
        [JsonRequired]
        // guid. this would allow tracking individual cruise and would be supported in V3 and modernization
        // but we don't really have a cruise identifier in earlier versions
        public string CruiseID { get; set; }

        [Required]
        [JsonRequired]
        // i.e. timber sale, recon, ROW as a coded integer value
        public string Purpose { get; set; }

        [Required]
        [JsonRequired]
        public string Region { get; set; }

        [Required]
        [JsonRequired]
        public string Forest { get; set; }

        [Required]
        [JsonRequired]
        public string District {  get; set; }

        public IReadOnlyCollection<TeaCuttingUnit> CuttingUnits { get; set; }

        public IReadOnlyCollection<TeaSampleGroup> SampleGroups { get; set; }

    }
}
