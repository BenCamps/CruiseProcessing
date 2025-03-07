using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

#nullable enable

namespace CruiseProcessing.Interop.Native
{
    public class VolumeLibraryNativeMethods
    {
        public VolumeLibraryDelegateTypes.BROWNCROWNFRACTION BROWNCROWNFRACTION { get; set; }
        public VolumeLibraryDelegateTypes.BROWNCULLCHUNK BROWNCULLCHUNK { get; set; }
        public VolumeLibraryDelegateTypes.BROWNCULLLOG BROWNCULLLOG { get; set; }
        public VolumeLibraryDelegateTypes.BROWNTOPWOOD BROWNTOPWOOD { get; set; }
        public VolumeLibraryDelegateTypes.CRZBIOMASSCS CRZBIOMASSCS { get; set; }
        public VolumeLibraryDelegateTypes.CRZSPDFTCS CRZSPDFTCS { get; set; }
        public VolumeLibraryDelegateTypes.GETNVBEQ GETNVBEQ { get; set; }
        public VolumeLibraryDelegateTypes.GETREGNWFCS GETREGNWFCS { get; set; }
        public VolumeLibraryDelegateTypes.GETVOLEQ3 GETVOLEQ3 { get; set; }
        public VolumeLibraryDelegateTypes.MRULESCS MRULESCS { get; set; }
        public VolumeLibraryDelegateTypes.VERNUM2 VERNUM2 { get; set; }
        public VolumeLibraryDelegateTypes.VOLLIBCSNVB VOLLIBCSNVB { get; set; }
    }
}
