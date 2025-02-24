using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CruiseProcessing.OutputModels
{
    public class TeaProductVolume
    {
        public string Product { get; set; }

        public double SumGrossBdFt { get; set; }
        public double SumNetBdFt { get; set; }

        public double SumGrossCuFt { get; set; }
        public double SumNetCuFt { get; set; }

        public double SumCords { get; set; }
        public double SumWeight { get; set; }
    }
}
