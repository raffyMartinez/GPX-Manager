using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class CTXFileImportEventArgs:EventArgs
    {
        public string SourceFile { get; set; }
        public string ImportResultFile { get; set; }
    }
}
