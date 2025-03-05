using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#nullable enable

namespace CruiseProcessing
{
    //public record VolEqList(string Forest, string CommonName, string Equation, string ModelName);
    public class VolEqList
    {
        public VolEqList(string forest, string commonName, string equation, string modelName)
        {
            this.Forest = forest ?? throw new ArgumentNullException(nameof(forest));
            this.CommonName = commonName ?? throw new ArgumentNullException(nameof(commonName));
            this.Equation = equation ?? throw new ArgumentNullException(nameof(equation));
            this.ModelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
        }

        public string Forest { get; set; }
        public string CommonName { get; set; }
        public string Equation { get; set; }
        public string ModelName { get; set; }
    }
}
