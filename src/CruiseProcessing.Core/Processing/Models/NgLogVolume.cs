using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CruiseProcessing.Processing.Models
{
    public class NgLogVolume
    {
        public string TreeID { get; set; }

        public int LogNumber { get; set; }

        // log dimentions
        public double SmallEndDiameter { get; set; }
        public double LargeEndDiameter { get; set; }
        public int LogLength { get; set; }
        public int DibClass { get; set; }

        // volumes
        public double GrossBdFt { get; set; }
        public double NetBdFt { get; set; }
        public double GrossBdFtRemoved { get; set; }
        public double GrossCuFt { get; set; }
        public double NetCuFt { get; set; }
        public double GrossCuFtRemoved { get;set; }

        public double SeenDefect {  get; set; }
        public double PercentRecoverable { get; set; }

        public string Grade {  get; set; }
    }
}
