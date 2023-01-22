using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;
using System.Runtime.Remoting.Messaging;

namespace FileImporter
{
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface ImporterInterop
    {
        [DispId(900)]
        string ImportGeomaxRAW(int outputFormat = 0);
        
    }


    

    
}
