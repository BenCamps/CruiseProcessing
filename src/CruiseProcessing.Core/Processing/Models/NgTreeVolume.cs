using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CruiseProcessing.Processing.Models
{
    public class NgTreeVolume
    {
        public IReadOnlyCollection<NgLogVolume> LogVolumes { get; set; }

        public string TreeID { get; set; }

        public double TotalCubicVolume { get; set; }
        public double GrossBDFTPrimary { get; set; }
        public double NetBDFTPrimary { get; set; }
        public double GrossCUFTPrimary { get; set; }
        public double NetCUFTPrimary { get; set;}
        public double CordsPrimary { get; set; }
        public double GrossBDFTRemovedPrimary { get; set; }
        public double GrossCUFTRemovedPrimary { get; set; }
        public double GrossBDFTSecondary {  get; set; }
        public double NetBDFTSecondary { get; set; }
        public double GrossCUFTSecondary { get; set; }
        public double NetCUFTSecondary { get; set; }
        public double CordsSecondary { get; set; }
        public double GrossCUFTRemovedSecondary { get; set; }
        public double NumberlogsMainStem { get; set; }
        public double NumberlogsTopWood { get; set; }
        public double GrossBDFTRecoveredPrimary { get; set; }
        public double GrossCUFTRecoveredPrimary { get; set; }
        public double CordsRecoveredPrimary { get; set; }
        public double GrossBDFTIntl { get; set; }
        public double NetBDFTIntl { get; set; }
        public double TipwoodVolume { get; set; }

        // green biomass
        public double BiomassMainStemPrimary { get; set; }
        public double BiomassMainStemSecondary { get; set; }
        public double BiomassProd {  get; set; }
        public double Biomasstotalstem {  get; set; }
        public double Biomasslivebranches { get; set; }
        public double Biomassdeadbranches { get; set; }
        public double Biomassfoliage {  get; set; }
        public double BiomassTip { get; set; }
        

        // value equation stuff i dont think we'll need
        //public double ValuePP { get; set; }
        //public double ValueSP { get; set; }
        //public double ValueRP { get; set; }
    }
}
