using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CruiseProcessing.Processing.Models
{
    public class NgLogInfo
    {
        public string TreeID { get; set; }
        public int LogNumber { get; set; }
        public string Grade {  get; set; }
        public double SeenDefect { get; set; }
        public double PercentRecoverable { get; set; }
        public int Length { get; set; }
        public double SmallEndDiameter { get; set; }
        public double LargeEndDiameter { get; set; }

        // these fields are if the tree is Fall Buck Scale
        // not sure if we are doing FBS 
        public double GrossBoardFoot { get; set; }
        public double NetBoardFoot { get; set; }
        public double GrossCubicFoot { get; set; }
        public double NetCubicFoot { get; set; }
        public double BoardFootRemoved { get; set; }
        public double CubicFootRemoved { get; set; }
        public double DIBClass {  get; set; }
        public double BarkThickness { get; set; }
    }
}
